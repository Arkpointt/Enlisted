using System;
using System.Collections.Generic;
using System.Linq;
using Enlisted.Features.Company;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Features.Escalation;
using Enlisted.Features.Identity;
using Enlisted.Mod.Core.Logging;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
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

            // Apply effects
            ApplyEffects(option.Effects);

            // Show result text
            ShowResultText(option);

            // Handle chain events
            if (!string.IsNullOrEmpty(option.Effects?.ChainEventId))
            {
                var chainEvent = EventCatalog.GetEvent(option.Effects.ChainEventId);
                if (chainEvent != null)
                {
                    ModLogger.Info(LogCategory, $"Queuing chain event: {option.Effects.ChainEventId}");
                    QueueEvent(chainEvent);
                }
                else
                {
                    ModLogger.Warn(LogCategory, $"Chain event not found: {option.Effects.ChainEventId}");
                }
            }

            OnEventClosed();
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
                    GiveGoldAction.ApplyBetweenCharacters(null, hero, effects.Gold.Value, false);
                }
                else
                {
                    GiveGoldAction.ApplyBetweenCharacters(hero, null, -effects.Gold.Value, false);
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
                var party = MobileParty.MainParty;
                if (party != null)
                {
                    ApplyTroopXp(party, effects.TroopXp.Value);
                    ModLogger.Debug(LogCategory, $"Applied troop XP: {effects.TroopXp.Value}");
                }
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
                    roster.AddToCounts(element.Character, -toRemove, false, 0, 0, true);
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
                    roster.AddToCounts(element.Character, 0, false, toWound, 0, true);
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
        /// Applies XP to T1-T3 troops in the party (NCO training).
        /// </summary>
        private void ApplyTroopXp(MobileParty party, int xpAmount)
        {
            var roster = party.MemberRoster;

            for (var i = 0; i < roster.Count; i++)
            {
                var element = roster.GetElementCopyAtIndex(i);
                if (element.Character != null && !element.Character.IsHero)
                {
                    var tier = element.Character.Tier;
                    if (tier >= 1 && tier <= 3)
                    {
                        roster.AddXpToTroop(element.Character, element.Number * xpAmount);
                    }
                }
            }
        }

        /// <summary>
        /// Shows the result text after an option is selected.
        /// </summary>
        private void ShowResultText(EventOption option)
        {
            var resultText = ResolveText(option.ResultTextId, option.ResultTextFallback);

            if (string.IsNullOrEmpty(resultText))
            {
                return; // No result text to show
            }

            var inquiry = new InquiryData(
                titleText: ResolveText(_currentEvent.TitleId, _currentEvent.TitleFallback),
                text: resultText,
                isAffirmativeOptionShown: true,
                isNegativeOptionShown: false,
                affirmativeText: new TextObject("{=str_ok}Continue").ToString(),
                negativeText: null,
                affirmativeAction: null,
                negativeAction: null
            );

            InformationManager.ShowInquiry(inquiry, true);
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
            var resolved = textObject.ToString();

            // If resolution returned the raw {=...} pattern, the lookup failed - use fallback directly
            if (resolved.StartsWith("{="))
            {
                ModLogger.Debug(LogCategory, $"XML lookup failed for '{textId}', using fallback");
                return effectiveFallback ?? string.Empty;
            }

            return resolved;
        }
    }
}

