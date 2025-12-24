using System;
using System.Collections.Generic;
using System.Linq;
using Enlisted.Features.Company;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Features.Escalation;
using Enlisted.Features.Identity;
using Enlisted.Features.Interface.Behaviors;
using Enlisted.Features.Logistics;
using Enlisted.Features.Ranks;
using Enlisted.Features.Retinue.Core;
using Enlisted.Mod.Core.Logging;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace Enlisted.Features.Content
{
    /// <summary>
    /// Delivers events to the player via UI popups using MultiSelectionInquiryData.
    /// Manages event queue, checks option requirements, and applies effects when options are selected.
    /// Supports tier, role, skill, and trait requirements for event options.
    /// </summary>
    public class EventDeliveryManager : CampaignBehaviorBase
    {
        private const string LogCategory = "EventDelivery";

        public static EventDeliveryManager Instance { get; private set; }

        private readonly Queue<EventDefinition> _pendingEvents = new();
        private bool _isShowingEvent;
        private EventDefinition _currentEvent;
        private RewardChoices _pendingSubChoice;

        public EventDeliveryManager()
        {
            Instance = this;
        }

        public override void RegisterEvents()
        {
            // No campaign events needed - events are queued programmatically
        }

        public override void SyncData(IDataStore dataStore)
        {
            // Event queue is transient - doesn't persist across saves
            // Events will be re-triggered by conditions when save is loaded
        }

        /// <summary>
        /// Queues an event for delivery to the player.
        /// Events are shown one at a time in FIFO order.
        /// </summary>
        public void QueueEvent(EventDefinition evt)
        {
            if (evt == null)
            {
                ModLogger.Warn(LogCategory, "Attempted to queue null event");
                return;
            }

            _pendingEvents.Enqueue(evt);
            ModLogger.Info(LogCategory, $"Queued event: {evt.Id} (queue size: {_pendingEvents.Count})");

            // Try to deliver immediately if no event is currently showing
            if (!_isShowingEvent)
            {
                TryDeliverNextEvent();
            }
        }

        /// <summary>
        /// Attempts to deliver the next event in the queue.
        /// Only delivers if no event is currently showing and queue is not empty.
        /// </summary>
        private void TryDeliverNextEvent()
        {
            if (_isShowingEvent)
            {
                return;
            }

            if (_pendingEvents.Count == 0)
            {
                return;
            }

            // Check if player is enlisted and not in invalid states
            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment?.IsEnlisted != true)
            {
                ModLogger.Debug(LogCategory, "Skipping event delivery - not enlisted");
                _pendingEvents.Clear(); // Clear queue when not enlisted
                return;
            }

            _currentEvent = _pendingEvents.Dequeue();
            _isShowingEvent = true;

            ModLogger.Info(LogCategory, $"Delivering event: {_currentEvent.Id}");

            ShowEventPopup(_currentEvent);
        }

        /// <summary>
        /// Shows the event popup using MultiSelectionInquiryData.
        /// </summary>
        private void ShowEventPopup(EventDefinition evt)
        {
            var options = BuildOptions(evt);

            if (options.Count == 0)
            {
                ModLogger.Warn(LogCategory, $"Event {evt.Id} has no valid options");
                OnEventClosed();
                return;
            }

            // Resolve localized text with inline fallback for when XML lookup fails
            var titleText = ResolveText(evt.TitleId, evt.TitleFallback);
            var descriptionText = ResolveText(evt.SetupId, evt.SetupFallback);

            var inquiry = new MultiSelectionInquiryData(
                titleText: titleText,
                descriptionText: descriptionText,
                inquiryElements: options,
                isExitShown: false, // Force player to choose (no escape)
                minSelectableOptionCount: 1,
                maxSelectableOptionCount: 1,
                affirmativeText: new TextObject("{=str_ok}Confirm").ToString(),
                negativeText: null,
                affirmativeAction: OnOptionSelected,
                negativeAction: null
            );

            MBInformationManager.ShowMultiSelectionInquiry(inquiry, true);
        }

        /// <summary>
        /// Builds the list of inquiry elements from event options.
        /// Checks requirements and disables options that don't meet them.
        /// </summary>
        private List<InquiryElement> BuildOptions(EventDefinition evt)
        {
            var options = new List<InquiryElement>();
            var hero = Hero.MainHero;

            foreach (var option in evt.Options)
            {
                var meetsRequirements = MeetsRequirements(option.Requirements, hero);
                var hint = GetOptionHint(option, hero);
                var optionText = ResolveText(option.TextId, option.TextFallback);

                options.Add(new InquiryElement(
                    identifier: option, // Store the EventOption object
                    title: optionText,
                    imageIdentifier: null,
                    isEnabled: meetsRequirements,
                    hint: hint
                ));
            }

            return options;
        }

        /// <summary>
        /// Checks if a hero meets the requirements for an event option.
        /// </summary>
        private bool MeetsRequirements(EventOptionRequirements requirements, Hero hero)
        {
            if (requirements == null)
            {
                return true; // No requirements = always available
            }

            // Check tier requirement
            if (requirements.MinTier.HasValue)
            {
                var playerTier = EnlistmentBehavior.Instance?.EnlistmentTier ?? 1;
                if (playerTier < requirements.MinTier.Value)
                {
                    return false;
                }
            }

            // Check role requirement against player's current specialization
            if (!string.IsNullOrEmpty(requirements.Role) &&
                !requirements.Role.Equals("Any", StringComparison.OrdinalIgnoreCase))
            {
                var playerRole = EnlistmentBehavior.Instance?.CurrentSpecialization ?? "Soldier";
                if (!playerRole.Equals(requirements.Role, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            // Check skill requirements
            if (requirements.MinSkills != null)
            {
                foreach (var skillReq in requirements.MinSkills)
                {
                    var skill = SkillCheckHelper.GetSkillByName(skillReq.Key);
                    if (skill != null && !SkillCheckHelper.MeetsSkillThreshold(hero, skill, skillReq.Value))
                    {
                        return false;
                    }
                }
            }

            // Check trait requirements
            if (requirements.MinTraits != null)
            {
                foreach (var traitReq in requirements.MinTraits)
                {
                    var trait = SkillCheckHelper.GetTraitByName(traitReq.Key);
                    if (trait != null && !SkillCheckHelper.MeetsTraitThreshold(hero, trait, traitReq.Value))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Gets the tooltip hint for an option explaining requirements or consequences.
        /// </summary>
        private string GetOptionHint(EventOption option, Hero hero)
        {
            if (option.Requirements == null)
            {
                // No requirements - show tooltip if provided
                return option.Tooltip ?? string.Empty;
            }

            // Build requirements hint
            var hints = new List<string>();

            // Tier requirement
            if (option.Requirements.MinTier.HasValue)
            {
                var playerTier = EnlistmentBehavior.Instance?.EnlistmentTier ?? 1;
                var tierReq = option.Requirements.MinTier.Value;
                if (playerTier < tierReq)
                {
                    hints.Add($"Requires Rank {tierReq}+");
                }
            }

            // Role requirement - show hint if player doesn't have the required specialization
            if (!string.IsNullOrEmpty(option.Requirements.Role) &&
                !option.Requirements.Role.Equals("Any", StringComparison.OrdinalIgnoreCase))
            {
                var playerRole = EnlistmentBehavior.Instance?.CurrentSpecialization ?? "Soldier";
                if (!playerRole.Equals(option.Requirements.Role, StringComparison.OrdinalIgnoreCase))
                {
                    hints.Add($"Requires {option.Requirements.Role} specialization");
                }
            }

            // Skill requirements
            if (option.Requirements.MinSkills != null)
            {
                foreach (var skillReq in option.Requirements.MinSkills)
                {
                    var skill = SkillCheckHelper.GetSkillByName(skillReq.Key);
                    if (skill != null)
                    {
                        var currentLevel = hero.GetSkillValue(skill);
                        if (currentLevel < skillReq.Value)
                        {
                            hints.Add($"Requires {skillReq.Key} {skillReq.Value}+ (You have {currentLevel})");
                        }
                    }
                }
            }

            // Trait requirements
            if (option.Requirements.MinTraits != null)
            {
                foreach (var traitReq in option.Requirements.MinTraits)
                {
                    var trait = SkillCheckHelper.GetTraitByName(traitReq.Key);
                    if (trait != null)
                    {
                        var currentLevel = hero.GetTraitLevel(trait);
                        if (currentLevel < traitReq.Value)
                        {
                            hints.Add($"Requires {traitReq.Key} trait level {traitReq.Value}+ (You have {currentLevel})");
                        }
                    }
                }
            }

            // If requirements are met, show tooltip
            if (hints.Count == 0 && !string.IsNullOrEmpty(option.Tooltip))
            {
                return option.Tooltip;
            }

            return hints.Count > 0 ? string.Join("\n", hints) : string.Empty;
        }

        /// <summary>
        /// Called when the player selects an option.
        /// Applies effects and shows result text.
        /// </summary>
        private void OnOptionSelected(List<InquiryElement> selected)
        {
            if (selected == null || selected.Count == 0)
            {
                ModLogger.Warn(LogCategory, "No option selected");
                OnEventClosed();
                return;
            }

            var selectedElement = selected[0];
            var option = selectedElement.Identifier as EventOption;

            if (option == null)
            {
                ModLogger.Error(LogCategory, "Selected option identifier is not an EventOption");
                OnEventClosed();
                return;
            }

            ModLogger.Info(LogCategory, $"Option selected: {option.Id}");

            // Record cooldown for player-initiated decisions ONLY if they commit to an action
            // (not if they select a cancel/decline option)
            if (_currentEvent != null && _currentEvent.Category != null && 
                _currentEvent.Category.Equals("decision", StringComparison.OrdinalIgnoreCase))
            {
                // Check if this is a cancel option (common patterns: cancel, nevermind, not_now, decline, skip, back)
                var isCancelOption = option.Id.IndexOf("cancel", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                     option.Id.IndexOf("nevermind", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                     option.Id.IndexOf("not_now", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                     option.Id.IndexOf("skip", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                     option.Id.IndexOf("back", StringComparison.OrdinalIgnoreCase) >= 0;

                if (!isCancelOption)
                {
                    // Player committed to an action - record cooldown
                    DecisionManager.Instance?.RecordDecisionSelected(_currentEvent.Id);
                    ModLogger.Info(LogCategory, $"Decision cooldown recorded for: {_currentEvent.Id}");
                }
                else
                {
                    ModLogger.Debug(LogCategory, $"Cancel option selected - no cooldown recorded");
                }
            }

            // Track declined promotions (player chose "not ready" or "decline" in proving event)
            if (_currentEvent != null && _currentEvent.Id.StartsWith("promotion_", StringComparison.OrdinalIgnoreCase))
            {
                if (option.Id.IndexOf("not_ready", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    option.Id.IndexOf("decline", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    // Extract target tier from event ID (e.g., "promotion_t6_t7_commanders_commission" -> tier 7)
                    var parts = _currentEvent.Id.Split('_');
                    if (parts.Length >= 3 && parts[2].StartsWith("t", StringComparison.OrdinalIgnoreCase))
                    {
                        var tierStr = parts[2].Substring(1);
                        if (int.TryParse(tierStr, out var targetTier))
                        {
                            EscalationManager.Instance?.RecordDeclinedPromotion(targetTier);
                            ModLogger.Info(LogCategory, $"Player declined promotion to tier {targetTier} - must request via dialog");
                        }
                    }
                }
            }

            // Determine if this is a risky option and resolve success/failure
            var isRisky = option.RiskChance.HasValue && option.RiskChance.Value > 0 && option.RiskChance.Value < 100;
            var success = true;

            if (isRisky)
            {
                var roll = MBRandom.RandomInt(100);
                success = roll < option.RiskChance.Value;
                ModLogger.Info(LogCategory, $"Risky option roll: {roll} vs {option.RiskChance.Value}% -> {(success ? "SUCCESS" : "FAILURE")}");
            }

            // Apply base effects first (always applied)
            ApplyEffects(option.Effects);

            // Apply success or failure effects based on outcome
            if (isRisky)
            {
                if (success && option.EffectsSuccess != null)
                {
                    ApplyEffects(option.EffectsSuccess);
                }
                else if (!success && option.EffectsFailure != null)
                {
                    ApplyEffects(option.EffectsFailure);
                }
            }

            // Apply flag changes
            ApplyFlagChanges(option);

            // Notify news system of event outcome
            NotifyNewsOfEventOutcome(option);

            // Notify news of pending chain event if applicable
            NotifyNewsOfPendingChainEvent(option);

            // Show result text (use failure text if risky and failed)
            ShowResultText(option, isRisky && !success);

            // Handle immediate chain events (backwards compatibility)
            if (!string.IsNullOrEmpty(option.Effects?.ChainEventId))
            {
                var chainEvent = EventCatalog.GetEvent(option.Effects.ChainEventId);
                if (chainEvent != null)
                {
                    ModLogger.Info(LogCategory, $"Queuing immediate chain event: {option.Effects.ChainEventId}");
                    QueueEvent(chainEvent);
                }
                else
                {
                    ModLogger.Warn(LogCategory, $"Immediate chain event not found: {option.Effects.ChainEventId}");
                }
            }

            // Handle delayed chain events
            if (!string.IsNullOrEmpty(option.ChainsTo) && option.ChainDelayHours > 0)
            {
                var escalationState = EscalationManager.Instance?.State;
                if (escalationState != null)
                {
                    escalationState.ScheduleChainEvent(option.ChainsTo, option.ChainDelayHours);
                    ModLogger.Info(LogCategory, 
                        $"Scheduled chain event: {option.ChainsTo} in {option.ChainDelayHours} hours ({option.ChainDelayHours / 24f:F1} days)");
                }
                else
                {
                    ModLogger.Warn(LogCategory, "Cannot schedule chain event - EscalationManager not available");
                }
            }

            // Handle baggage check special events
            HandleBaggageCheckOutcome(_currentEvent, option, isRisky, success);

            OnEventClosed();
        }

        /// <summary>
        /// Handles baggage check event outcomes (contraband inspection at muster).
        /// Calls EnlistmentBehavior methods to process confiscation, bribes, and smuggling.
        /// </summary>
        private void HandleBaggageCheckOutcome(EventDefinition evt, EventOption option, bool isRisky, bool success)
        {
            // isRisky passed from caller but not used here; success determines risky outcome
            _ = isRisky;
            
            if (evt == null)
            {
                return;
            }
            
            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment == null)
            {
                return;
            }

            switch (evt.Id)
            {
                case "evt_baggage_lookaway":
                    enlistment.HandleBaggageLookAway();
                    break;
                    
                case "evt_baggage_bribe":
                    HandleBribeBaggageEvent(option, success, enlistment);
                    break;
                    
                case "evt_baggage_confiscate":
                    HandleConfiscateBaggageEvent(option, success, enlistment);
                    break;
            }
        }

        /// <summary>
        /// Handles bribe event outcomes (QM rep 35-64).
        /// Uses Charm skill to determine bribe success: 60+ auto-success, &lt;30 auto-fail, 30-59 roll.
        /// </summary>
        private void HandleBribeBaggageEvent(EventOption option, bool success, EnlistmentBehavior enlistment)
        {
            var contraband = enlistment.PendingContrabandCheck;
            if (contraband == null)
            {
                return;
            }

            switch (option.Id)
            {
                case "bribe":
                    // Calculate bribe amount first
                    int bribeAmount = ContrabandChecker.CalculateBribeAmount(contraband.TotalValue);
                    
                    // Check if player can afford the bribe
                    if (Hero.MainHero.Gold < bribeAmount)
                    {
                        // Can't afford bribe - confiscation instead
                        enlistment.HandleBaggageConfiscation();
                        
                        InformationManager.DisplayMessage(new InformationMessage(
                            $"You can't afford the {bribeAmount} gold bribe. Contraband confiscated.",
                            Colors.Red));
                        break;
                    }
                    
                    // Apply Charm skill check (60+ auto-success, <30 auto-fail, 30-59 roll)
                    int charmSkill = Hero.MainHero.GetSkillValue(DefaultSkills.Charm);
                    bool bribeSuccess = EvaluateCharmCheck(charmSkill);
                    
                    if (bribeSuccess)
                    {
                        // Successful bribe - pay and keep item
                        enlistment.HandleBaggageBribeSuccess(bribeAmount);
                        
                        InformationManager.DisplayMessage(new InformationMessage(
                            $"Paid {bribeAmount} gold to the quartermaster. Contraband overlooked.",
                            Colors.Yellow));
                    }
                    else
                    {
                        // Failed bribe negotiation - confiscation
                        enlistment.HandleBaggageConfiscation();
                        
                        InformationManager.DisplayMessage(new InformationMessage(
                            "Bribe rejected. Contraband confiscated.",
                            Colors.Red));
                    }
                    break;
                    
                case "smuggle":
                    if (success)
                    {
                        enlistment.HandleBaggageSmuggleSuccess();
                        
                        InformationManager.DisplayMessage(new InformationMessage(
                            "Contraband hidden successfully. +15 Roguery XP.",
                            Colors.Green));
                    }
                    else
                    {
                        enlistment.HandleBaggageSmuggleFailure();
                        
                        var mostValuable = contraband.MostValuable;
                        string itemName = mostValuable?.Item?.Name?.ToString() ?? "item";
                        
                        InformationManager.DisplayMessage(new InformationMessage(
                            $"Caught smuggling! {itemName} confiscated. +4 Scrutiny, -10 QM reputation.",
                            Colors.Red));
                    }
                    break;
                    
                case "surrender":
                    // Voluntary surrender - confiscation with less penalty
                    enlistment.HandleBaggageConfiscation();
                    
                    var surrenderedItem = contraband.MostValuable;
                    string surrenderedItemName = surrenderedItem?.Item?.Name?.ToString() ?? "item";
                    int fine = ContrabandChecker.CalculateFineAmount(surrenderedItem?.Value ?? 0);
                    
                    InformationManager.DisplayMessage(new InformationMessage(
                        $"{surrenderedItemName} surrendered. Fine: {fine} gold.",
                        Colors.Yellow));
                    break;
            }
        }

        /// <summary>
        /// Evaluates a Charm skill check for bribe negotiation.
        /// Charm 60+: auto-success. Charm &lt;30: auto-fail. Charm 30-59: random roll.
        /// </summary>
        private bool EvaluateCharmCheck(int charmSkill)
        {
            if (charmSkill >= 60)
            {
                ModLogger.Debug(LogCategory, $"Charm {charmSkill} >= 60: auto-success");
                return true;
            }
            
            if (charmSkill < 30)
            {
                ModLogger.Debug(LogCategory, $"Charm {charmSkill} < 30: auto-fail");
                return false;
            }
            
            // Charm 30-59: roll based on skill
            // Scale from 20% at Charm 30 to 80% at Charm 59
            float successChance = 0.20f + ((charmSkill - 30) / 29f) * 0.60f;
            bool success = MBRandom.RandomFloat < successChance;
            
            ModLogger.Debug(LogCategory, $"Charm {charmSkill}: roll {successChance:P0} -> {(success ? "success" : "fail")}");
            return success;
        }

        /// <summary>
        /// Handles confiscation event outcomes (QM rep &lt; 35).
        /// </summary>
        private void HandleConfiscateBaggageEvent(EventOption option, bool success, EnlistmentBehavior enlistment)
        {
            var contraband = enlistment.PendingContrabandCheck;
            if (contraband == null)
            {
                return;
            }

            switch (option.Id)
            {
                case "accept":
                    enlistment.HandleBaggageConfiscation();
                    
                    var confiscatedItem = contraband.MostValuable;
                    string itemName = confiscatedItem?.Item?.Name?.ToString() ?? "item";
                    int fine = ContrabandChecker.CalculateFineAmount(confiscatedItem?.Value ?? 0);
                    
                    InformationManager.DisplayMessage(new InformationMessage(
                        $"{itemName} confiscated. Fine: {fine} gold. +2 Scrutiny.",
                        Colors.Red));
                    break;
                    
                case "protest":
                    if (success)
                    {
                        // Rare success - keep item but owe a favor
                        enlistment.ClearPendingContrabandCheck();
                        
                        InformationManager.DisplayMessage(new InformationMessage(
                            "Protest accepted. You keep your item, but you owe the quartermaster.",
                            Colors.Yellow));
                    }
                    else
                    {
                        // Made things worse
                        enlistment.HandleBaggageConfiscation();
                        
                        // Extra discipline penalty already in JSON effects_failure
                        InformationManager.DisplayMessage(new InformationMessage(
                            "Protest backfired. Item confiscated with extra penalties.",
                            Colors.Red));
                    }
                    break;
            }
        }

        /// <summary>
        /// Applies the effects of a selected event option.
        /// </summary>
        private void ApplyEffects(EventEffects effects)
        {
            if (effects == null)
            {
                return;
            }

            var hero = Hero.MainHero;
            var enlistment = EnlistmentBehavior.Instance;
            var escalation = EscalationManager.Instance;

            // Apply skill XP
            if (effects.SkillXp != null)
            {
                foreach (var skillXp in effects.SkillXp)
                {
                    var skill = SkillCheckHelper.GetSkillByName(skillXp.Key);
                    if (skill != null)
                    {
                        hero.AddSkillXp(skill, skillXp.Value);
                        ModLogger.Debug(LogCategory, $"Applied {skillXp.Value} XP to {skillXp.Key}");
                    }
                }
            }

            // Apply dynamic skill XP from effects
            if (effects.DynamicSkillXp != null && effects.DynamicSkillXp.Count > 0)
            {
                foreach (var dynamicXp in effects.DynamicSkillXp)
                {
                    if (dynamicXp.Value <= 0)
                    {
                        ModLogger.Warn(LogCategory, $"Invalid dynamic XP value for {dynamicXp.Key}: {dynamicXp.Value}");
                        continue;
                    }
                    
                    SkillObject targetSkill = null;
                    
                    switch (dynamicXp.Key.ToLowerInvariant())
                    {
                        case "equipped_weapon":
                            targetSkill = WeaponSkillHelper.GetEquippedWeaponSkill(hero);
                            break;
                        case "weakest_combat":
                            targetSkill = WeaponSkillHelper.GetWeakestCombatSkill(hero);
                            break;
                        default:
                            ModLogger.Warn(LogCategory, $"Unknown dynamic XP key in effects: {dynamicXp.Key}");
                            continue;
                    }
                    
                    if (targetSkill != null)
                    {
                        hero.AddSkillXp(targetSkill, dynamicXp.Value);
                        ModLogger.Debug(LogCategory, 
                            $"Applied dynamic {dynamicXp.Key} XP from effects: +{dynamicXp.Value} {targetSkill.Name}");
                    }
                    else
                    {
                        ModLogger.Warn(LogCategory, $"Could not resolve dynamic skill for {dynamicXp.Key}");
                    }
                }
            }

            // Apply trait XP
            if (effects.TraitXp != null)
            {
                foreach (var traitXp in effects.TraitXp)
                {
                    var trait = SkillCheckHelper.GetTraitByName(traitXp.Key);
                    if (trait != null)
                    {
                        TraitHelper.AwardTraitXp(hero, trait, traitXp.Value);
                        ModLogger.Debug(LogCategory, $"Applied {traitXp.Value} trait XP to {traitXp.Key}");
                    }
                }
            }

            // Apply reputation changes
            if (effects.LordRep.HasValue && escalation != null)
            {
                escalation.ModifyLordReputation(effects.LordRep.Value, "event");
                ModLogger.Debug(LogCategory, $"Modified lord reputation by {effects.LordRep.Value}");
            }

            if (effects.OfficerRep.HasValue && escalation != null)
            {
                escalation.ModifyOfficerReputation(effects.OfficerRep.Value, "event");
                ModLogger.Debug(LogCategory, $"Modified officer reputation by {effects.OfficerRep.Value}");
            }

            if (effects.SoldierRep.HasValue && escalation != null)
            {
                escalation.ModifySoldierReputation(effects.SoldierRep.Value, "event");
                ModLogger.Debug(LogCategory, $"Modified soldier reputation by {effects.SoldierRep.Value}");
            }

            // Apply escalation changes
            if (effects.Scrutiny.HasValue && escalation != null)
            {
                escalation.ModifyScrutiny(effects.Scrutiny.Value, "event");
                ModLogger.Debug(LogCategory, $"Modified scrutiny by {effects.Scrutiny.Value}");
            }

            if (effects.Discipline.HasValue && escalation != null)
            {
                escalation.ModifyDiscipline(effects.Discipline.Value, "event");
                ModLogger.Debug(LogCategory, $"Modified discipline by {effects.Discipline.Value}");
            }

            if (effects.MedicalRisk.HasValue && escalation != null)
            {
                escalation.ModifyMedicalRisk(effects.MedicalRisk.Value, "event");
                ModLogger.Debug(LogCategory, $"Modified medical risk by {effects.MedicalRisk.Value}");
            }

            // Apply gold
            if (effects.Gold.HasValue && effects.Gold.Value != 0)
            {
                if (effects.Gold.Value > 0)
                {
                    GiveGoldAction.ApplyBetweenCharacters(null, hero, effects.Gold.Value);
                }
                else
                {
                    GiveGoldAction.ApplyBetweenCharacters(hero, null, -effects.Gold.Value);
                }
                ModLogger.Debug(LogCategory, $"Applied gold change: {effects.Gold.Value}");
            }

            // Apply HP change
            if (effects.HpChange.HasValue && effects.HpChange.Value != 0)
            {
                hero.HitPoints += effects.HpChange.Value;
                // Clamp to valid range
                if (hero.HitPoints > hero.CharacterObject.MaxHitPoints())
                {
                    hero.HitPoints = hero.CharacterObject.MaxHitPoints();
                }
                if (hero.HitPoints < 1)
                {
                    hero.HitPoints = 1; // Prevent death
                }
                ModLogger.Debug(LogCategory, $"Applied HP change: {effects.HpChange.Value}");
            }

            // Apply troop loss
            if (effects.TroopLoss.HasValue && effects.TroopLoss.Value > 0)
            {
                var party = MobileParty.MainParty;
                if (party != null)
                {
                    ApplyTroopLoss(party, effects.TroopLoss.Value);
                    ModLogger.Debug(LogCategory, $"Applied troop loss: {effects.TroopLoss.Value}");
                }
            }

            // Apply troop wounded
            if (effects.TroopWounded.HasValue && effects.TroopWounded.Value > 0)
            {
                var party = MobileParty.MainParty;
                if (party != null)
                {
                    ApplyTroopWounded(party, effects.TroopWounded.Value);
                    ModLogger.Debug(LogCategory, $"Applied troop wounded: {effects.TroopWounded.Value}");
                }
            }

            // Apply food loss
            if (effects.FoodLoss.HasValue && effects.FoodLoss.Value > 0)
            {
                var party = MobileParty.MainParty;
                if (party != null)
                {
                    ApplyFoodLoss(party, effects.FoodLoss.Value);
                    ModLogger.Debug(LogCategory, $"Applied food loss: {effects.FoodLoss.Value}");
                }
            }

            // Apply troop XP
            if (effects.TroopXp.HasValue && effects.TroopXp.Value > 0)
            {
                ApplyTroopXp(effects.TroopXp.Value);
                ModLogger.Debug(LogCategory, $"Applied troop XP: {effects.TroopXp.Value}");
            }

            // Apply company needs changes
            if (effects.CompanyNeeds != null && enlistment?.CompanyNeeds != null)
            {
                foreach (var needChange in effects.CompanyNeeds)
                {
                    if (Enum.TryParse<CompanyNeed>(needChange.Key, true, out var need))
                    {
                        var oldValue = enlistment.CompanyNeeds.GetNeed(need);
                        var newValue = oldValue + needChange.Value;
                        enlistment.CompanyNeeds.SetNeed(need, newValue);
                        ModLogger.Debug(LogCategory, $"Modified {need}: {oldValue} -> {newValue}");
                    }
                }
            }

            // Apply renown
            if (effects.Renown.HasValue && effects.Renown.Value != 0)
            {
                hero.Clan.AddRenown(effects.Renown.Value);
                ModLogger.Debug(LogCategory, $"Applied renown change: {effects.Renown.Value}");
            }

            // Apply retinue gain - adds soldiers to Commander's retinue
            if (effects.RetinueGain.HasValue && effects.RetinueGain.Value > 0)
            {
                ApplyRetinueGain(effects.RetinueGain.Value);
            }

            // Apply retinue loss - removes soldiers from Commander's retinue
            if (effects.RetinueLoss.HasValue && effects.RetinueLoss.Value > 0)
            {
                ApplyRetinueLoss(effects.RetinueLoss.Value);
            }

            // Apply retinue wounded - wounds soldiers in Commander's retinue
            if (effects.RetinueWounded.HasValue && effects.RetinueWounded.Value > 0)
            {
                ApplyRetinueWounded(effects.RetinueWounded.Value);
            }

            // Apply retinue loyalty change
            if (effects.RetinueLoyalty.HasValue && effects.RetinueLoyalty.Value != 0)
            {
                ApplyRetinueLoyalty(effects.RetinueLoyalty.Value);
            }

            // Apply discharge if specified. Ends the player's enlistment with the given band.
            if (!string.IsNullOrEmpty(effects.TriggersDischarge))
            {
                ApplyDischargeEffect(effects.TriggersDischarge);
            }

            // Apply promotion if specified. Used by proving events to grant tier advancement.
            if (effects.Promotes.HasValue && effects.Promotes.Value > 0)
            {
                ApplyPromotesEffect(effects.Promotes.Value);
            }
        }

        /// <summary>
        /// Adds soldiers to the player's retinue from event effects.
        /// Only works if player has Commander rank (T7+) and has selected a retinue type.
        /// </summary>
        private void ApplyRetinueGain(int count)
        {
            var manager = RetinueManager.Instance;
            if (manager?.State == null || !manager.State.HasTypeSelected)
            {
                ModLogger.Warn(LogCategory, $"ApplyRetinueGain: Cannot add {count} soldiers - no retinue type selected");
                return;
            }

            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment == null || enlistment.EnlistmentTier < RetinueManager.CommanderTier1)
            {
                ModLogger.Warn(LogCategory, $"ApplyRetinueGain: Cannot add {count} soldiers - not at Commander rank");
                return;
            }

            if (manager.TryAddSoldiers(count, manager.State.SelectedTypeId, out var added, out var message))
            {
                ModLogger.Info(LogCategory, $"ApplyRetinueGain: Added {added} soldiers via event");

                var notification = new TextObject("{=evt_ret_gain_notification}{COUNT} soldiers have joined your retinue.");
                notification.SetTextVariable("COUNT", added);
                InformationManager.DisplayMessage(new InformationMessage(notification.ToString(), Colors.Green));
            }
            else
            {
                ModLogger.Warn(LogCategory, $"ApplyRetinueGain failed: {message}");
            }
        }

        /// <summary>
        /// Removes soldiers from the player's retinue specifically.
        /// Only removes troops tracked in RetinueState (does not affect lord's main force).
        /// Used for dramatic events like ambush, betrayal, or desertion.
        /// </summary>
        private void ApplyRetinueLoss(int count)
        {
            var manager = RetinueManager.Instance;
            var state = manager?.State;
            if (state == null || !state.HasRetinue)
            {
                ModLogger.Warn(LogCategory, $"ApplyRetinueLoss: Cannot remove {count} soldiers - no active retinue");
                return;
            }

            var party = MobileParty.MainParty;
            if (party?.MemberRoster == null)
            {
                ModLogger.Warn(LogCategory, "ApplyRetinueLoss: No party roster available");
                return;
            }

            var removed = 0;
            var roster = party.MemberRoster;

            // Remove only troops tracked in RetinueState.TroopCounts
            foreach (var kvp in state.TroopCounts.ToList())
            {
                if (removed >= count)
                {
                    break;
                }

                var characterId = kvp.Key;
                var trackedCount = kvp.Value;

                var character = CharacterObject.Find(characterId);
                if (character == null)
                {
                    continue;
                }

                var rosterCount = roster.GetTroopCount(character);
                var toRemove = Math.Min(Math.Min(trackedCount, rosterCount), count - removed);

                if (toRemove > 0)
                {
                    roster.AddToCounts(character, -toRemove, removeDepleted: true);
                    state.UpdateTroopCount(characterId, -toRemove);
                    removed += toRemove;
                }
            }

            if (removed > 0)
            {
                ModLogger.Info(LogCategory, $"ApplyRetinueLoss: Removed {removed} retinue soldiers");

                var notification = new TextObject("{=evt_ret_loss_notification}{COUNT} soldiers have been lost from your retinue.");
                notification.SetTextVariable("COUNT", removed);
                InformationManager.DisplayMessage(new InformationMessage(notification.ToString(), Colors.Red));
            }
            else
            {
                ModLogger.Warn(LogCategory, $"ApplyRetinueLoss: No soldiers could be removed (requested {count})");
            }
        }

        /// <summary>
        /// Wounds soldiers in the player's retinue specifically.
        /// Only wounds troops tracked in RetinueState (moves healthy to wounded roster).
        /// Used for events involving skirmishes, accidents, or hardship.
        /// </summary>
        private void ApplyRetinueWounded(int count)
        {
            var manager = RetinueManager.Instance;
            var state = manager?.State;
            if (state == null || !state.HasRetinue)
            {
                ModLogger.Warn(LogCategory, $"ApplyRetinueWounded: Cannot wound {count} soldiers - no active retinue");
                return;
            }

            var party = MobileParty.MainParty;
            if (party?.MemberRoster == null)
            {
                ModLogger.Warn(LogCategory, "ApplyRetinueWounded: No party roster available");
                return;
            }

            var wounded = 0;
            var roster = party.MemberRoster;

            // Wound only troops tracked in RetinueState.TroopCounts
            foreach (var kvp in state.TroopCounts.ToList())
            {
                if (wounded >= count)
                {
                    break;
                }

                var characterId = kvp.Key;
                var character = CharacterObject.Find(characterId);
                if (character == null)
                {
                    continue;
                }

                // Get current roster state for this troop
                var rosterIndex = roster.FindIndexOfTroop(character);
                if (rosterIndex < 0)
                {
                    continue;
                }

                var element = roster.GetElementCopyAtIndex(rosterIndex);
                var healthyCount = element.Number - element.WoundedNumber;

                if (healthyCount <= 0)
                {
                    continue; // All already wounded
                }

                var toWound = Math.Min(healthyCount, count - wounded);
                if (toWound > 0)
                {
                    roster.AddToCounts(character, 0, woundedCount: toWound);
                    wounded += toWound;
                }
            }

            if (wounded > 0)
            {
                ModLogger.Info(LogCategory, $"ApplyRetinueWounded: Wounded {wounded} retinue soldiers");

                var notification = new TextObject("{=evt_ret_wounded_notification}{COUNT} of your soldiers have been wounded.");
                notification.SetTextVariable("COUNT", wounded);
                InformationManager.DisplayMessage(new InformationMessage(notification.ToString(), Colors.Yellow));
            }
            else
            {
                ModLogger.Warn(LogCategory, $"ApplyRetinueWounded: No soldiers could be wounded (requested {count})");
            }
        }

        /// <summary>
        /// Applies a loyalty change to the player's retinue.
        /// Loyalty affects morale, desertion risk, and combat effectiveness.
        /// Only applies if player has Commander rank (T7+) and has a retinue.
        /// Automatically checks for threshold crossings after applying the change.
        /// </summary>
        private void ApplyRetinueLoyalty(int delta)
        {
            var manager = RetinueManager.Instance;
            var state = manager?.State;
            if (state == null || !state.HasRetinue)
            {
                ModLogger.Debug(LogCategory, $"ApplyRetinueLoyalty: Skipping {delta:+#;-#;0} - no active retinue");
                return;
            }

            var oldLoyalty = state.RetinueLoyalty;
            var newValue = state.RetinueLoyalty + delta;
            state.RetinueLoyalty = newValue < 0 ? 0 : (newValue > 100 ? 100 : newValue);
            var newLoyalty = state.RetinueLoyalty;

            ModLogger.Info(LogCategory, $"ApplyRetinueLoyalty: {oldLoyalty} -> {newLoyalty} ({delta:+#;-#;0})");

            // Record loyalty change for news feed
            var dayNumber = (int)(CampaignTime.Now.ToDays);
            var message = delta > 0 ? "Improved morale from event" : "Morale impact from event";
            EnlistedNewsBehavior.Instance?.AddReputationChange("Retinue", delta, newLoyalty, message, dayNumber);

            // Show notification if loyalty changed significantly
            if (Math.Abs(delta) >= 5)
            {
                var loyaltyText = newLoyalty switch
                {
                    >= 80 => "high",
                    >= 60 => "good",
                    >= 40 => "fair",
                    >= 20 => "low",
                    _ => "critical"
                };

                var notification = new TextObject("{=evt_ret_loyalty_notification}Retinue loyalty {CHANGE}. Current: {LEVEL}");
                notification.SetTextVariable("CHANGE", delta > 0 ? "increased" : "decreased");
                notification.SetTextVariable("LEVEL", loyaltyText);

                var color = delta > 0 ? Colors.Green : Colors.Red;
                InformationManager.DisplayMessage(new InformationMessage(notification.ToString(), color));
            }

            // Check for threshold crossings after applying loyalty change
            manager.CheckLoyaltyThresholds();
        }

        /// <summary>
        /// Applies a promotion effect, advancing the player to the specified tier.
        /// This is called when an event option with promotes is selected (proving events).
        /// Triggers retinue grant for T7/T8/T9 promotions via SetTier().
        /// A targetTier of -1 means "promote to next tier" (resolved at runtime).
        /// </summary>
        private void ApplyPromotesEffect(int targetTier)
        {
            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment == null || !enlistment.IsEnlisted)
            {
                ModLogger.Warn(LogCategory, $"Cannot apply promotion (tier {targetTier}) - not enlisted");
                return;
            }

            var currentTier = enlistment.EnlistmentTier;
            
            // Handle sentinel value -1: promote to next tier
            // This allows JSON to use "promotes": true without specifying explicit tier
            if (targetTier == -1)
            {
                targetTier = currentTier + 1;
                ModLogger.Debug(LogCategory, $"Resolved 'promotes: true' to target tier {targetTier}");
            }
            
            if (targetTier <= currentTier)
            {
                ModLogger.Warn(LogCategory, $"Cannot promote to tier {targetTier} - already at tier {currentTier}");
                return;
            }

            ModLogger.Info(LogCategory, $"Applying promotion from event: T{currentTier} to T{targetTier}");

            // SetTier handles retinue grant for T7/T8/T9 promotions
            enlistment.SetTier(targetTier);
            Features.Equipment.Behaviors.QuartermasterManager.Instance?.UpdateNewlyUnlockedItems();

            // Clear pending promotion flag in PromotionBehavior
            Features.Ranks.Behaviors.PromotionBehavior.Instance?.ClearPendingPromotion();
            
            // Trigger promotion notification with culture-specific rank and immersive text
            Features.Ranks.Behaviors.PromotionBehavior.Instance?.TriggerPromotionNotificationPublic(targetTier);

            ModLogger.Info(LogCategory, $"Promotion complete: now tier {targetTier}");
        }

        /// <summary>
        /// Applies a discharge effect, ending the player's enlistment with the specified band.
        /// This is called when an event option with triggers_discharge is selected.
        /// </summary>
        private void ApplyDischargeEffect(string dischargeBand)
        {
            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment == null || !enlistment.IsEnlisted)
            {
                ModLogger.Warn(LogCategory, $"Cannot apply discharge ({dischargeBand}) - not enlisted");
                return;
            }

            var bandLower = dischargeBand.ToLowerInvariant();
            var isHonorable = bandLower == "honorable" || bandLower == "veteran";

            ModLogger.Info(LogCategory, $"Applying discharge from event: band={dischargeBand}, honorable={isHonorable}");

            // Record the discharge in the service record.
            // ServiceRecordManager.RecordReservist handles setting LastDischargeBand and ReenlistmentBlockedUntil.
            var lord = enlistment.EnlistedLord;
            var daysServed = enlistment.EnlistmentDate != CampaignTime.Zero
                ? (int)(CampaignTime.Now - enlistment.EnlistmentDate).ToDays
                : 0;
            Retinue.Core.ServiceRecordManager.Instance?.RecordReservist(
                bandLower,
                daysServed,
                enlistment.EnlistmentTier,
                enlistment.EnlistmentXP,
                lord);

            enlistment.StopEnlist($"Discharged: {dischargeBand}", isHonorable);
        }

        /// <summary>
        /// Applies troop loss by removing troops from the party roster.
        /// </summary>
        private void ApplyTroopLoss(MobileParty party, int count)
        {
            var roster = party.MemberRoster;
            var troopsRemoved = 0;

            // Remove from regular troops first
            for (var i = roster.Count - 1; i >= 0 && troopsRemoved < count; i--)
            {
                var element = roster.GetElementCopyAtIndex(i);
                if (element.Character != null && !element.Character.IsHero)
                {
                    var toRemove = Math.Min(element.Number, count - troopsRemoved);
                    roster.AddToCounts(element.Character, -toRemove, removeDepleted: true);
                    troopsRemoved += toRemove;
                }
            }
        }

        /// <summary>
        /// Applies troop wounded by moving troops from healthy to wounded.
        /// </summary>
        private void ApplyTroopWounded(MobileParty party, int count)
        {
            var roster = party.MemberRoster;
            var troopsWounded = 0;

            // Wound regular troops
            for (var i = roster.Count - 1; i >= 0 && troopsWounded < count; i--)
            {
                var element = roster.GetElementCopyAtIndex(i);
                if (element.Character != null && !element.Character.IsHero && element.Number > element.WoundedNumber)
                {
                    var healthyCount = element.Number - element.WoundedNumber;
                    var toWound = Math.Min(healthyCount, count - troopsWounded);
                    roster.AddToCounts(element.Character, 0, woundedCount: toWound, removeDepleted: true);
                    troopsWounded += toWound;
                }
            }
        }

        /// <summary>
        /// Applies food loss by removing food items from the party inventory.
        /// </summary>
        private void ApplyFoodLoss(MobileParty party, int amount)
        {
            var itemRoster = party.ItemRoster;
            var foodRemaining = amount;

            // Remove food items
            for (var i = itemRoster.Count - 1; i >= 0 && foodRemaining > 0; i--)
            {
                var element = itemRoster.GetElementCopyAtIndex(i);
                if (element.EquipmentElement.Item.IsFood)
                {
                    var toRemove = Math.Min(element.Amount, foodRemaining);
                    itemRoster.AddToCounts(element.EquipmentElement.Item, -toRemove);
                    foodRemaining -= toRemove;
                }
            }
        }

        /// <summary>
        /// Applies XP to T1-T3 troops in the lord's party (NCO training).
        /// Validates troops can gain XP before awarding.
        /// Reports training results to the news system.
        /// </summary>
        private void ApplyTroopXp(int xpAmount)
        {
            if (xpAmount <= 0)
            {
                ModLogger.Warn(LogCategory, "Invalid XP amount for troop training");
                return;
            }
            
            // Get lord's party (player is enlisted, not leading their own party)
            var enlistment = EnlistmentBehavior.Instance;
            var lord = enlistment?.EnlistedLord;
            var lordParty = lord?.PartyBelongedTo;
            
            if (lordParty == null || !lordParty.IsActive)
            {
                ModLogger.Warn(LogCategory, "Cannot train troops: lord party not available");
                return;
            }
            
            // Don't train if lord is captured
            if (lord.IsPrisoner)
            {
                ModLogger.Info(LogCategory, "Skipping training: lord is prisoner");
                return;
            }
            
            var roster = lordParty.MemberRoster;
            if (roster == null || roster.TotalManCount == 0)
            {
                ModLogger.Info(LogCategory, "No troops to train");
                return;
            }
            
            // Snapshot roster to avoid concurrent modification
            var troopsToTrain = new List<(CharacterObject Character, int Count)>();
            
            for (int i = 0; i < roster.Count; i++)
            {
                var element = roster.GetElementCopyAtIndex(i);
                if (element.Character == null || element.Character.IsHero)
                {
                    continue;
                }
                
                // Only train T1-T3 troops
                if (element.Character.Tier < 1 || element.Character.Tier > 3)
                {
                    continue;
                }
                
                if (element.Number <= 0)
                {
                    continue;
                }
                
                troopsToTrain.Add((element.Character, element.Number));
            }
            
            if (troopsToTrain.Count == 0)
            {
                ModLogger.Info(LogCategory, "No eligible troops for training (all max tier or heroes)");
                return;
            }
            
            // Limit to 10 troop types per training session
            troopsToTrain = troopsToTrain.Take(10).ToList();
            
            int trainedTypes = 0;
            int totalTrained = 0;
            int promotionReady = 0;
            
            foreach (var (character, count) in troopsToTrain)
            {
                try
                {
                    // Use native check: can this troop actually gain XP?
                    if (!MobilePartyHelper.CanTroopGainXp(lordParty.Party, character, out int maxGainableXp))
                    {
                        // Troop at max tier, can't gain XP
                        continue;
                    }
                    
                    // Cap total XP to prevent over-leveling (integer division could round to 0, so ensure minimum of 1 XP per troop)
                    int totalXpForTroop = Math.Min(xpAmount * count, maxGainableXp);
                    
                    // Skip if no XP can be awarded (very close to max level)
                    if (totalXpForTroop <= 0)
                    {
                        continue;
                    }
                    
                    roster.AddXpToTroop(character, totalXpForTroop);
                    
                    trainedTypes++;
                    totalTrained += count;
                    
                    // Check if troops might be ready for promotion after training (approximate)
                    var upgradeTarget = character.UpgradeTargets?.FirstOrDefault();
                    if (upgradeTarget != null)
                    {
                        // Get current XP and calculate if close to promotion threshold
                        int indexOfTroop = roster.FindIndexOfTroop(character);
                        if (indexOfTroop >= 0)
                        {
                            int currentXp = roster.GetElementXp(indexOfTroop);
                            int xpNeeded = Campaign.Current.Models.PartyTroopUpgradeModel
                                .GetXpCostForUpgrade(lordParty.Party, character, upgradeTarget);
                            int totalXpNeeded = xpNeeded * count;
                            
                            // Consider ready if within 90% of promotion after this training
                            if (xpNeeded > 0 && currentXp >= totalXpNeeded * 0.9f)
                            {
                                promotionReady += count;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    ModLogger.Error(LogCategory, $"Failed to train {character.Name}: {ex.Message}");
                }
            }
            
            if (trainedTypes > 0)
            {
                ModLogger.Info(LogCategory, 
                    $"Trained {trainedTypes} troop types ({totalTrained} soldiers, +{xpAmount} XP each)");
                
                // Report to news system
                ReportTrainingToNews(totalTrained, promotionReady, xpAmount);
            }
        }

        /// <summary>
        /// Reports training results to the camp news system.
        /// </summary>
        private void ReportTrainingToNews(int soldiersTrained, int promotionReady, int xpGiven)
        {
            if (EnlistedNewsBehavior.Instance == null)
            {
                return;
            }
            
            string summary;
            if (promotionReady > 0)
            {
                summary = $"{soldiersTrained} soldiers trained (+{xpGiven} XP), {promotionReady} ready for promotion";
            }
            else
            {
                summary = $"{soldiersTrained} soldiers trained (+{xpGiven} XP)";
            }
            
            // Use generic EventOutcome for training results
            var outcome = new EventOutcomeRecord
            {
                EventId = "training_session",
                EventTitle = "Training Session",
                OptionChosen = "NCO Training",
                OutcomeSummary = summary,
                DayNumber = (int)CampaignTime.Now.ToDays,
                EffectsApplied = new Dictionary<string, int>
                {
                    { "Soldiers Trained", soldiersTrained },
                    { "XP Awarded", xpGiven },
                    { "Ready for Promotion", promotionReady }
                }
            };
            
            EnlistedNewsBehavior.Instance.AddEventOutcome(outcome);
            ModLogger.Debug(LogCategory, $"Reported training to news: {summary}");
        }

        /// <summary>
        /// Applies flag changes from an event option.
        /// Sets or clears flags in the escalation state.
        /// </summary>
        private void ApplyFlagChanges(EventOption option)
        {
            var state = EscalationManager.Instance?.State;
            if (state == null)
            {
                return;
            }

            // Set flags
            if (option.SetFlags != null && option.SetFlags.Count > 0)
            {
                foreach (var flag in option.SetFlags)
                {
                    state.SetFlag(flag, option.FlagDurationDays);
                    ModLogger.Info(LogCategory, $"Set flag: {flag} (duration: {option.FlagDurationDays} days)");
                }
            }

            // Clear flags
            if (option.ClearFlags != null && option.ClearFlags.Count > 0)
            {
                foreach (var flag in option.ClearFlags)
                {
                    state.ClearFlag(flag);
                    ModLogger.Info(LogCategory, $"Cleared flag: {flag}");
                }
            }
            
            // Advance onboarding if option triggers it
            if (option.AdvancesOnboarding && state.IsOnboardingActive)
            {
                var oldStage = state.OnboardingStage;
                state.AdvanceOnboardingStage();
                var newStage = state.OnboardingStage;
                
                if (newStage == 0)
                {
                    ModLogger.Info(LogCategory, $"Onboarding complete (was stage {oldStage})");
                }
                else
                {
                    ModLogger.Info(LogCategory, $"Onboarding advanced: stage {oldStage} to {newStage}");
                }
            }
        }

        /// <summary>
        /// Notifies the news system of an event outcome after an option is selected.
        /// Adds the outcome to Personal Feed with a formatted headline.
        /// </summary>
        private void NotifyNewsOfEventOutcome(EventOption option)
        {
            if (EnlistedNewsBehavior.Instance == null)
            {
                return;
            }

            if (_currentEvent == null)
            {
                return;
            }

            try
            {
                var outcome = new EventOutcomeRecord
                {
                    EventId = _currentEvent.Id,
                    EventTitle = ResolveText(_currentEvent.TitleId, _currentEvent.TitleFallback),
                    OptionChosen = ResolveText(option.TextId, option.TextFallback),
                    OutcomeSummary = BuildOutcomeSummary(option.Effects),
                    DayNumber = (int)CampaignTime.Now.ToDays,
                    EffectsApplied = BuildEffectsDictionary(option.Effects)
                };

                EnlistedNewsBehavior.Instance.AddEventOutcome(outcome);
                ModLogger.Debug(LogCategory, $"Notified news of event outcome: {_currentEvent.Id}");
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, "Failed to notify news of event outcome", ex);
            }
        }

        /// <summary>
        /// Builds a human-readable summary of event effects for display in headlines.
        /// </summary>
        private static string BuildOutcomeSummary(EventEffects effects)
        {
            if (effects == null)
            {
                return string.Empty;
            }

            var parts = new List<string>();

            // Gold
            if (effects.Gold.HasValue && effects.Gold.Value != 0)
            {
                parts.Add(effects.Gold.Value > 0
                    ? $"+{effects.Gold.Value} gold"
                    : $"{effects.Gold.Value} gold");
            }

            // Skill XP - show top 3 skills by XP amount with truncated names
            if (effects.SkillXp != null)
            {
                var positiveSkillXp = effects.SkillXp
                    .Where(kv => kv.Value > 0)
                    .OrderByDescending(kv => kv.Value)
                    .ToList();
                
                var topThree = positiveSkillXp.Take(3);
                foreach (var skillXp in topThree)
                {
                    var skillName = skillXp.Key.Length > 12 
                        ? skillXp.Key.Substring(0, 10) + ".." 
                        : skillXp.Key;
                    parts.Add($"+{skillXp.Value} {skillName} XP");
                }
                
                // Show remaining count if more than 3 skills gained
                var remaining = positiveSkillXp.Count - 3;
                if (remaining > 0)
                {
                    parts.Add($"+{remaining} more skills");
                }
            }

            // Dynamic skill XP - resolve to actual skill names for display
            if (effects.DynamicSkillXp != null && effects.DynamicSkillXp.Count > 0)
            {
                foreach (var dynamicXp in effects.DynamicSkillXp.Where(kv => kv.Value > 0))
                {
                    SkillObject targetSkill = null;
                    switch (dynamicXp.Key.ToLowerInvariant())
                    {
                        case "equipped_weapon":
                            targetSkill = WeaponSkillHelper.GetEquippedWeaponSkill(Hero.MainHero);
                            break;
                        case "weakest_combat":
                            targetSkill = WeaponSkillHelper.GetWeakestCombatSkill(Hero.MainHero);
                            break;
                    }
                    
                    if (targetSkill != null)
                    {
                        var skillName = targetSkill.Name.ToString();
                        if (skillName.Length > 12)
                        {
                            skillName = skillName.Substring(0, 10) + "..";
                        }
                        parts.Add($"+{dynamicXp.Value} {skillName} XP");
                    }
                }
            }

            // Reputations
            if (effects.LordRep.HasValue && effects.LordRep.Value != 0)
            {
                parts.Add($"{effects.LordRep.Value:+#;-#;0} Lord Rep");
            }

            if (effects.OfficerRep.HasValue && effects.OfficerRep.Value != 0)
            {
                parts.Add($"{effects.OfficerRep.Value:+#;-#;0} Officer Rep");
            }

            if (effects.SoldierRep.HasValue && effects.SoldierRep.Value != 0)
            {
                parts.Add($"{effects.SoldierRep.Value:+#;-#;0} Soldier Rep");
            }

            // Escalation
            if (effects.Scrutiny.HasValue && effects.Scrutiny.Value != 0)
            {
                parts.Add($"{effects.Scrutiny.Value:+#;-#;0} Scrutiny");
            }

            if (effects.Discipline.HasValue && effects.Discipline.Value != 0)
            {
                parts.Add($"{effects.Discipline.Value:+#;-#;0} Discipline");
            }

            // HP
            if (effects.HpChange.HasValue && effects.HpChange.Value != 0)
            {
                parts.Add(effects.HpChange.Value > 0
                    ? $"+{effects.HpChange.Value} HP"
                    : $"{effects.HpChange.Value} HP");
            }

            // Renown
            if (effects.Renown.HasValue && effects.Renown.Value != 0)
            {
                parts.Add($"{effects.Renown.Value:+#;-#;0} Renown");
            }

            return parts.Count > 0 ? $"({string.Join(", ", parts)})" : string.Empty;
        }

        /// <summary>
        /// Builds a dictionary of effect values for storage and lookup.
        /// </summary>
        private static Dictionary<string, int> BuildEffectsDictionary(EventEffects effects)
        {
            var dict = new Dictionary<string, int>();

            if (effects == null)
            {
                return dict;
            }

            if (effects.Gold.HasValue && effects.Gold.Value != 0)
            {
                dict["Gold"] = effects.Gold.Value;
            }

            if (effects.LordRep.HasValue && effects.LordRep.Value != 0)
            {
                dict["LordRep"] = effects.LordRep.Value;
            }

            if (effects.OfficerRep.HasValue && effects.OfficerRep.Value != 0)
            {
                dict["OfficerRep"] = effects.OfficerRep.Value;
            }

            if (effects.SoldierRep.HasValue && effects.SoldierRep.Value != 0)
            {
                dict["SoldierRep"] = effects.SoldierRep.Value;
            }

            if (effects.Scrutiny.HasValue && effects.Scrutiny.Value != 0)
            {
                dict["Scrutiny"] = effects.Scrutiny.Value;
            }

            if (effects.Discipline.HasValue && effects.Discipline.Value != 0)
            {
                dict["Discipline"] = effects.Discipline.Value;
            }

            if (effects.SkillXp != null)
            {
                foreach (var skillXp in effects.SkillXp)
                {
                    dict[$"{skillXp.Key}XP"] = skillXp.Value;
                }
            }

            if (effects.DynamicSkillXp != null)
            {
                foreach (var dynamicXp in effects.DynamicSkillXp)
                {
                    // Resolve dynamic skill to actual skill name for display
                    SkillObject targetSkill = null;
                    switch (dynamicXp.Key.ToLowerInvariant())
                    {
                        case "equipped_weapon":
                            targetSkill = WeaponSkillHelper.GetEquippedWeaponSkill(Hero.MainHero);
                            break;
                        case "weakest_combat":
                            targetSkill = WeaponSkillHelper.GetWeakestCombatSkill(Hero.MainHero);
                            break;
                    }
                    
                    if (targetSkill != null)
                    {
                        dict[$"{targetSkill.Name}XP"] = dynamicXp.Value;
                    }
                }
            }

            return dict;
        }

        /// <summary>
        /// Notifies the news system of a pending chain event for context display.
        /// Shows hints like "A comrade owes you money" in the Daily Brief.
        /// </summary>
        private void NotifyNewsOfPendingChainEvent(EventOption option)
        {
            if (EnlistedNewsBehavior.Instance == null)
            {
                return;
            }

            if (_currentEvent == null)
            {
                return;
            }

            // Only notify for delayed chain events
            if (string.IsNullOrEmpty(option.ChainsTo) || option.ChainDelayHours <= 0)
            {
                return;
            }

            try
            {
                var hint = BuildChainEventContextHint(option.ChainsTo);
                var delayDays = Math.Max(1, option.ChainDelayHours / 24);

                EnlistedNewsBehavior.Instance.AddPendingEvent(
                    _currentEvent.Id,
                    option.ChainsTo,
                    hint,
                    delayDays);

                ModLogger.Debug(LogCategory, $"Notified news of pending chain event: {option.ChainsTo} in {delayDays} days");
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, "Failed to notify news of pending chain event", ex);
            }
        }

        /// <summary>
        /// Builds a context hint for a pending chain event based on its ID.
        /// </summary>
        private static string BuildChainEventContextHint(string chainEventId)
        {
            var id = chainEventId?.ToLowerInvariant() ?? string.Empty;

            // Loan/debt repayment
            if (id.Contains("repay") || id.Contains("return") || id.Contains("debt"))
            {
                return "A comrade owes you money.";
            }

            // Gratitude/favor
            if (id.Contains("gratitude") || id.Contains("thank") || id.Contains("favor"))
            {
                return "Someone remembers your kindness.";
            }

            // Revenge/grudge
            if (id.Contains("revenge") || id.Contains("grudge") || id.Contains("payback"))
            {
                return "Someone holds a grudge.";
            }

            // Training follow-up
            if (id.Contains("training") || id.Contains("practice"))
            {
                return "Your training regimen continues.";
            }

            // Investigation/scrutiny follow-up
            if (id.Contains("investigation") || id.Contains("scrutiny"))
            {
                return "Officers are watching closely.";
            }

            // Default
            return "Something is brewing.";
        }

        /// <summary>
        /// Shows the result text after an option is selected.
        /// If the option has reward_choices, stores them for showing after result popup closes.
        /// </summary>
        /// <param name="option">The selected option.</param>
        /// <param name="showFailure">True to show failure text for risky options that failed.</param>
        private void ShowResultText(EventOption option, bool showFailure = false)
        {
            // Store pending sub-choice if option has reward_choices (only on success)
            if (!showFailure && option.RewardChoices != null && option.RewardChoices.Options.Count > 0)
            {
                _pendingSubChoice = option.RewardChoices;
                ModLogger.Debug(LogCategory, $"Stored pending sub-choice: {option.RewardChoices.Type} with {option.RewardChoices.Options.Count} options");
            }
            else
            {
                _pendingSubChoice = null;
            }

            // Use failure text if showing failure outcome
            var resultText = showFailure
                ? ResolveText(option.ResultTextFailureId, option.ResultTextFailureFallback)
                : ResolveText(option.ResultTextId, option.ResultTextFallback);

            if (string.IsNullOrEmpty(resultText))
            {
                // No result text - show sub-choice directly if pending
                if (_pendingSubChoice != null)
                {
                    ShowSubChoicePopup(_pendingSubChoice);
                }
                return;
            }

            var inquiry = new InquiryData(
                titleText: ResolveText(_currentEvent.TitleId, _currentEvent.TitleFallback),
                text: resultText,
                isAffirmativeOptionShown: true,
                isNegativeOptionShown: false,
                affirmativeText: new TextObject("{=str_ok}Continue").ToString(),
                negativeText: null,
                affirmativeAction: OnResultTextClosed,
                negativeAction: null
            );

            InformationManager.ShowInquiry(inquiry, true);
        }

        /// <summary>
        /// Called when the result text popup is closed.
        /// If there's a pending sub-choice, shows the sub-choice popup.
        /// </summary>
        private void OnResultTextClosed()
        {
            if (_pendingSubChoice != null)
            {
                ShowSubChoicePopup(_pendingSubChoice);
            }
        }

        /// <summary>
        /// Shows the sub-choice popup for branching rewards.
        /// Filters options based on conditions and presents available choices.
        /// </summary>
        private void ShowSubChoicePopup(RewardChoices choices)
        {
            var options = new List<InquiryElement>();

            foreach (var subOption in choices.Options)
            {
                // Check condition if present - hide option if condition fails
                if (!CheckSubChoiceCondition(subOption.Condition))
                {
                    ModLogger.Debug(LogCategory, $"Sub-option '{subOption.Id}' hidden - condition '{subOption.Condition}' not met");
                    continue;
                }

                options.Add(new InquiryElement(
                    identifier: subOption,
                    title: subOption.Text,
                    imageIdentifier: null,
                    isEnabled: true,
                    hint: subOption.Tooltip ?? string.Empty
                ));
            }

            if (options.Count == 0)
            {
                ModLogger.Warn(LogCategory, $"Sub-choice popup '{choices.Type}' has no valid options after condition filtering");
                _pendingSubChoice = null;
                return;
            }

            // Format the title from the type (capitalize first letter)
            var titleText = !string.IsNullOrEmpty(choices.Type)
                ? char.ToUpper(choices.Type[0]) + choices.Type.Substring(1).Replace('_', ' ')
                : "Choose";

            var inquiry = new MultiSelectionInquiryData(
                titleText: titleText,
                descriptionText: choices.Prompt,
                inquiryElements: options,
                isExitShown: false,
                minSelectableOptionCount: 1,
                maxSelectableOptionCount: 1,
                affirmativeText: new TextObject("{=str_ok}Confirm").ToString(),
                negativeText: null,
                affirmativeAction: OnSubChoiceSelected,
                negativeAction: null
            );

            MBInformationManager.ShowMultiSelectionInquiry(inquiry, true);
        }

        /// <summary>
        /// Called when a sub-choice is selected from the reward choices popup.
        /// Applies costs, rewards, and effects from the selected sub-option.
        /// </summary>
        private void OnSubChoiceSelected(List<InquiryElement> selected)
        {
            _pendingSubChoice = null;

            if (selected == null || selected.Count == 0)
            {
                ModLogger.Warn(LogCategory, "No sub-choice selected");
                return;
            }

            var subOption = selected[0].Identifier as RewardChoiceOption;
            if (subOption == null)
            {
                ModLogger.Error(LogCategory, "Selected sub-choice identifier is not a RewardChoiceOption");
                return;
            }

            ModLogger.Info(LogCategory, $"Sub-choice selected: {subOption.Id}");

            // Apply costs first
            ApplyCosts(subOption.Costs);

            // Apply rewards
            ApplyRewards(subOption.Rewards);

            // Apply effects (reuse existing method)
            ApplyEffects(subOption.Effects);
        }

        /// <summary>
        /// Checks if a sub-choice condition is met.
        /// Supports formation:X, flag:X, and role:X conditions.
        /// </summary>
        private bool CheckSubChoiceCondition(string condition)
        {
            if (string.IsNullOrEmpty(condition))
            {
                return true; // No condition means always available
            }

            // Check for formation: prefix (e.g., "formation:ranged")
            if (condition.StartsWith("formation:", StringComparison.OrdinalIgnoreCase))
            {
                var formation = condition.Substring(10).Trim();
                var playerRole = EnlistmentBehavior.Instance?.CurrentSpecialization ?? "Soldier";

                // Map formations to roles that match
                return formation.ToLowerInvariant() switch
                {
                    "ranged" => playerRole.Equals("Scout", StringComparison.OrdinalIgnoreCase) ||
                                playerRole.Equals("Archer", StringComparison.OrdinalIgnoreCase),
                    "infantry" => playerRole.Equals("Soldier", StringComparison.OrdinalIgnoreCase),
                    "cavalry" => playerRole.Equals("Soldier", StringComparison.OrdinalIgnoreCase) ||
                                 playerRole.Equals("Scout", StringComparison.OrdinalIgnoreCase),
                    _ => true // Unknown formation - allow by default
                };
            }

            // Check for role: prefix (e.g., "role:Scout")
            if (condition.StartsWith("role:", StringComparison.OrdinalIgnoreCase))
            {
                var requiredRole = condition.Substring(5).Trim();
                var playerRole = EnlistmentBehavior.Instance?.CurrentSpecialization ?? "Soldier";
                return playerRole.Equals(requiredRole, StringComparison.OrdinalIgnoreCase);
            }

            // Check for flag: prefix (e.g., "flag:has_bow")
            if (condition.StartsWith("flag:", StringComparison.OrdinalIgnoreCase))
            {
                var flagName = condition.Substring(5).Trim();
                return EscalationManager.Instance?.State?.HasFlag(flagName) ?? false;
            }

            // Check for has_flag: prefix
            if (condition.StartsWith("has_flag:", StringComparison.OrdinalIgnoreCase))
            {
                var flagName = condition.Substring(9).Trim();
                return EscalationManager.Instance?.State?.HasFlag(flagName) ?? false;
            }

            // Check for has_weapon_equipped condition
            if (condition.Equals("has_weapon_equipped", StringComparison.OrdinalIgnoreCase))
            {
                return WeaponSkillHelper.HasWeaponEquipped(Hero.MainHero);
            }

            // Unknown condition type - log and allow
            ModLogger.Debug(LogCategory, $"Unknown sub-choice condition type: {condition}");
            return true;
        }

        /// <summary>
        /// Applies costs from a sub-choice option.
        /// Deducts gold, applies fatigue, etc.
        /// </summary>
        private void ApplyCosts(EventCosts costs)
        {
            if (costs == null)
            {
                return;
            }

            var hero = Hero.MainHero;

            // Apply gold cost
            if (costs.Gold.HasValue && costs.Gold.Value > 0)
            {
                GiveGoldAction.ApplyBetweenCharacters(hero, null, costs.Gold.Value);
                ModLogger.Debug(LogCategory, $"Applied gold cost: -{costs.Gold.Value}");
            }

            // Apply fatigue cost (affects company Rest need if available)
            if (costs.Fatigue.HasValue && costs.Fatigue.Value > 0)
            {
                var enlistment = EnlistmentBehavior.Instance;
                if (enlistment?.CompanyNeeds != null)
                {
                    var currentRest = enlistment.CompanyNeeds.GetNeed(CompanyNeed.Rest);
                    enlistment.CompanyNeeds.SetNeed(CompanyNeed.Rest, currentRest - costs.Fatigue.Value);
                    ModLogger.Debug(LogCategory, $"Applied fatigue cost: {costs.Fatigue.Value} (reduced Rest)");
                }
            }

            // Time costs could affect scheduling, but for now just log
            if (costs.TimeHours.HasValue && costs.TimeHours.Value > 0)
            {
                ModLogger.Debug(LogCategory, $"Time cost noted: {costs.TimeHours.Value} hours (not yet implemented)");
            }
        }

        /// <summary>
        /// Applies rewards from a sub-choice option.
        /// Grants gold, fatigue relief, skill XP, etc.
        /// </summary>
        private void ApplyRewards(EventRewards rewards)
        {
            if (rewards == null)
            {
                return;
            }

            var hero = Hero.MainHero;

            // Apply gold reward
            if (rewards.Gold.HasValue && rewards.Gold.Value > 0)
            {
                GiveGoldAction.ApplyBetweenCharacters(null, hero, rewards.Gold.Value);
                ModLogger.Debug(LogCategory, $"Applied gold reward: +{rewards.Gold.Value}");
            }

            // Apply fatigue relief (increases company Rest need)
            if (rewards.FatigueRelief.HasValue && rewards.FatigueRelief.Value > 0)
            {
                var enlistment = EnlistmentBehavior.Instance;
                if (enlistment?.CompanyNeeds != null)
                {
                    var currentRest = enlistment.CompanyNeeds.GetNeed(CompanyNeed.Rest);
                    enlistment.CompanyNeeds.SetNeed(CompanyNeed.Rest, currentRest + rewards.FatigueRelief.Value);
                    ModLogger.Debug(LogCategory, $"Applied fatigue relief: +{rewards.FatigueRelief.Value} Rest");
                }
            }

            // Apply skill XP rewards with experience track modifier
            if (rewards.SkillXp != null && rewards.SkillXp.Count > 0)
            {
                // Get training XP modifier based on hero's experience level
                // Green recruits get +20%, seasoned soldiers get normal XP, veterans get -10%
                float xpModifier = ExperienceTrackHelper.GetTrainingXpModifier(hero);
                
                foreach (var skillXp in rewards.SkillXp)
                {
                    var skill = SkillCheckHelper.GetSkillByName(skillXp.Key);
                    if (skill != null)
                    {
                        // Apply experience track modifier to training XP
                        int baseXp = skillXp.Value;
                        int modifiedXp = (int)Math.Round(baseXp * xpModifier);
                        
                        // Ensure minimum 1 XP if base XP was positive to prevent rounding to zero
                        if (baseXp > 0 && modifiedXp < 1)
                        {
                            modifiedXp = 1;
                        }
                        
                        hero.AddSkillXp(skill, modifiedXp);
                        
                        // Log with modifier details when modifier is not neutral
                        if (Math.Abs(xpModifier - 1.0f) > 0.001f)
                        {
                            ModLogger.Debug(LogCategory, $"Applied skill XP reward: +{modifiedXp} {skillXp.Key} (base {baseXp} × {xpModifier:F2})");
                        }
                        else
                        {
                            ModLogger.Debug(LogCategory, $"Applied skill XP reward: +{modifiedXp} {skillXp.Key}");
                        }
                    }
                    else
                    {
                        ModLogger.Warn(LogCategory, $"Unknown skill in reward: {skillXp.Key}");
                    }
                }
            }

            // Apply dynamic skill XP rewards
            if (rewards.DynamicSkillXp != null && rewards.DynamicSkillXp.Count > 0)
            {
                foreach (var dynamicXp in rewards.DynamicSkillXp)
                {
                    // Validate XP value
                    if (dynamicXp.Value <= 0)
                    {
                        ModLogger.Warn(LogCategory, $"Invalid dynamic XP value for {dynamicXp.Key}: {dynamicXp.Value}");
                        continue;
                    }
                    
                    SkillObject targetSkill;
                    
                    switch (dynamicXp.Key.ToLowerInvariant())
                    {
                        case "equipped_weapon":
                            targetSkill = WeaponSkillHelper.GetEquippedWeaponSkill(hero);
                            break;
                        case "weakest_combat":
                            targetSkill = WeaponSkillHelper.GetWeakestCombatSkill(hero);
                            break;
                        default:
                            ModLogger.Warn(LogCategory, $"Unknown dynamic XP key: {dynamicXp.Key}");
                            continue;
                    }
                    
                    if (targetSkill != null)
                    {
                        hero.AddSkillXp(targetSkill, dynamicXp.Value);
                        ModLogger.Debug(LogCategory, 
                            $"Applied dynamic {dynamicXp.Key} XP: +{dynamicXp.Value} {targetSkill.Name}");
                    }
                }
            }

            // Log general XP (for future tracking systems)
            if (rewards.Xp != null && rewards.Xp.Count > 0)
            {
                foreach (var xp in rewards.Xp)
                {
                    ModLogger.Debug(LogCategory, $"General XP reward logged: +{xp.Value} {xp.Key} (tracking not yet implemented)");
                }
            }
        }

        /// <summary>
        /// Called when an event is closed (after result shown or when no options available).
        /// </summary>
        private void OnEventClosed()
        {
            _isShowingEvent = false;
            _currentEvent = null;

            // Try to deliver next event in queue
            if (_pendingEvents.Count > 0)
            {
                TryDeliverNextEvent();
            }
        }

        /// <summary>
        /// Resolves a text ID to localized text from the XML strings file.
        /// Uses Bannerlord's TextObject system with the format {=stringId}Fallback.
        /// If the string is not found in XML, uses the provided inline fallback text.
        /// </summary>
        /// <param name="textId">The XML string ID to look up.</param>
        /// <param name="fallbackText">The inline fallback text from JSON if XML lookup fails.</param>
        private string ResolveText(string textId, string fallbackText = null)
        {
            // Determine the best fallback: use inline text if available, otherwise use the ID
            var effectiveFallback = !string.IsNullOrEmpty(fallbackText) ? fallbackText : textId;

            if (string.IsNullOrEmpty(textId))
            {
                // No XML ID provided, just return the fallback
                return effectiveFallback ?? string.Empty;
            }

            // Use TextObject to look up the string from XML
            // Format: "{=stringId}Fallback" - if string not found, uses the fallback text
            var textObject = new TextObject($"{{={textId}}}{effectiveFallback}");
            
            // Set NCO and soldier name text variables for personalized event dialogue
            SetEventTextVariables(textObject);
            
            var resolved = textObject.ToString();

            // If resolution returned the raw {=...} pattern, the lookup failed - use fallback directly
            if (resolved.StartsWith("{="))
            {
                ModLogger.Debug(LogCategory, $"XML lookup failed for '{textId}', using fallback");
                return effectiveFallback ?? string.Empty;
            }

            return resolved;
        }

        /// <summary>
        /// Sets text variables for event personalization (NCO names, soldier names, etc.).
        /// Called before resolving event text to populate placeholders like {SERGEANT}, {COMRADE_NAME}.
        /// </summary>
        private void SetEventTextVariables(TextObject textObject)
        {
            try
            {
                var enlistment = EnlistmentBehavior.Instance;
                if (enlistment == null) return;

                // NCO/Sergeant names
                var ncoFullName = enlistment.NcoFullName ?? "the Sergeant";
                var ncoRank = enlistment.NcoRank ?? "Sergeant";
                textObject.SetTextVariable("SERGEANT", ncoFullName);
                textObject.SetTextVariable("SERGEANT_NAME", ncoFullName);
                textObject.SetTextVariable("NCO_NAME", ncoFullName);
                textObject.SetTextVariable("NCO_RANK", ncoRank);

                // Soldier names for multi-character events
                var soldierName = enlistment.GetRandomSoldierName();
                var veteran1Name = enlistment.GetRandomSoldierName();
                var veteran2Name = enlistment.GetRandomSoldierName();
                var recruitName = enlistment.GetRandomSoldierName();
                textObject.SetTextVariable("COMRADE_NAME", soldierName);
                textObject.SetTextVariable("SOLDIER_NAME", soldierName);
                textObject.SetTextVariable("VETERAN_1_NAME", veteran1Name);
                textObject.SetTextVariable("VETERAN_2_NAME", veteran2Name);
                textObject.SetTextVariable("RECRUIT_NAME", recruitName);
                textObject.SetTextVariable("SECOND_SHORT", veteran1Name);

                // Officer names
                textObject.SetTextVariable("OFFICER_NAME", ncoFullName);
                textObject.SetTextVariable("CAPTAIN_NAME", "the Captain");
                
                // Naval crew names (for War Sails DLC events)
                textObject.SetTextVariable("BOATSWAIN_NAME", "the Boatswain");
                textObject.SetTextVariable("NAVIGATOR_NAME", "the Navigator");
                textObject.SetTextVariable("FIELD_MEDIC_NAME", "the Field Medic");

                // Company/Party info
                var companyName = enlistment.EnlistedLord?.PartyBelongedTo?.Name?.ToString() ?? "the company";
                textObject.SetTextVariable("COMPANY_NAME", companyName);

                // Player info
                if (Hero.MainHero != null)
                {
                    textObject.SetTextVariable("PLAYER_NAME", Hero.MainHero.FirstName?.ToString() ?? "Soldier");
                    textObject.SetTextVariable("PLAYER_RANK", RankHelper.GetCurrentRank(enlistment));
                }

                // Lord info
                if (enlistment.EnlistedLord != null)
                {
                    var lord = enlistment.EnlistedLord;
                    textObject.SetTextVariable("LORD_NAME", lord.Name?.ToString() ?? "the Lord");
                    textObject.SetTextVariable("LORD_TITLE", lord.IsFemale ? "Lady" : "Lord");
                    
                    // Faction and kingdom names
                    if (lord.MapFaction != null)
                    {
                        textObject.SetTextVariable("FACTION_NAME", lord.MapFaction.Name?.ToString() ?? "the faction");
                        
                        if (lord.MapFaction.IsKingdomFaction && lord.MapFaction is Kingdom kingdom)
                        {
                            textObject.SetTextVariable("KINGDOM_NAME", kingdom.Name?.ToString() ?? "the kingdom");
                        }
                        else
                        {
                            textObject.SetTextVariable("KINGDOM_NAME", lord.MapFaction.Name?.ToString() ?? "the realm");
                        }
                    }
                }

                // Rank progression variables
                var currentRank = RankHelper.GetCurrentRank(enlistment);
                textObject.SetTextVariable("NEXT_RANK", "the next rank");
                textObject.SetTextVariable("SECOND_RANK", currentRank);

                // Location variables
                var party = enlistment.EnlistedLord?.PartyBelongedTo;
                if (party?.CurrentSettlement != null)
                {
                    textObject.SetTextVariable("SETTLEMENT_NAME", party.CurrentSettlement.Name?.ToString() ?? "the settlement");
                }
                else
                {
                    textObject.SetTextVariable("SETTLEMENT_NAME", "the settlement");
                }

                // Previous lord (for transfer events)
                textObject.SetTextVariable("PREVIOUS_LORD", "your previous lord");
                textObject.SetTextVariable("ALLIED_LORD", "an allied lord");

                // Enemy faction info
                textObject.SetTextVariable("ENEMY_FACTION_ADJECTIVE", "enemy");

                // Medical event variables
                textObject.SetTextVariable("CONDITION_TYPE", "illness");
                textObject.SetTextVariable("CONDITION_LOCATION", "your arm");
                textObject.SetTextVariable("COMPLICATION_NAME", "infection");
                textObject.SetTextVariable("REMEDY_NAME", "medicine");

                // Naval event variables (for War Sails DLC)
                textObject.SetTextVariable("SHIP_NAME", companyName);
                textObject.SetTextVariable("DESTINATION_PORT", "port");
                textObject.SetTextVariable("DAYS_AT_SEA", "7");

                // Troop count for command events
                if (party != null)
                {
                    textObject.SetTextVariable("TROOP_COUNT", party.MemberRoster.TotalManCount.ToString());
                }
                else
                {
                    textObject.SetTextVariable("TROOP_COUNT", "20");
                }
            }
            catch (Exception ex)
            {
                ModLogger.Debug(LogCategory, $"Failed to set event text variables: {ex.Message}");
            }
        }
    }
}

