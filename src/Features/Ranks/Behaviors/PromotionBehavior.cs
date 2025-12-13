using System;
using System.Collections.Generic;
using Enlisted.Features.Assignments.Behaviors;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Features.Equipment.Behaviors;
using Enlisted.Features.Escalation;
using Enlisted.Mod.Core.Logging;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace Enlisted.Features.Ranks.Behaviors
{
    /// <summary>
    ///     Phase 7: Requirements for promotion to each tier.
    ///     Based on the Phase 7 promotion requirements table.
    /// </summary>
    public sealed class PromotionRequirements
    {
        public int XP { get; set; }
        public int DaysInRank { get; set; }
        public int EventsRequired { get; set; }
        public int BattlesRequired { get; set; }
        public int MinLanceReputation { get; set; }
        public int MinLeaderRelation { get; set; }
        public int MaxDiscipline { get; set; }

        /// <summary>
        ///     Get promotion requirements for a specific tier transition.
        /// </summary>
        public static PromotionRequirements GetForTier(int targetTier)
        {
            // Phase 7 promotion requirements table:
            // | Promotion | XP | Days | Events | Battles | Lance Rep | Leader Rel | Max Disc |
            // |-----------|-----|------|--------|---------|-----------|------------|----------|
            // | T1→T2 | 700 | 14 | 5 | 2 | ≥0 | ≥0 | <8 |
            // | T2→T3 | 2,200 | 35 | 12 | 6 | ≥10 | ≥10 | <7 |
            // | T3→T4 | 4,400 | 56 | 25 | 12 | ≥20 | ≥20 | <6 |
            // | T4→T5 | 6,600 | 56 | 40 | 20 | ≥30 | ≥30 | <5 |
            // | T5→T6 | 8,800 | 56 | 55 | 30 | ≥40 | ≥15 | <4 |

            return targetTier switch
            {
                2 => new PromotionRequirements { XP = 700, DaysInRank = 14, EventsRequired = 5, BattlesRequired = 2, MinLanceReputation = 0, MinLeaderRelation = 0, MaxDiscipline = 8 },
                3 => new PromotionRequirements { XP = 2200, DaysInRank = 35, EventsRequired = 12, BattlesRequired = 6, MinLanceReputation = 10, MinLeaderRelation = 10, MaxDiscipline = 7 },
                4 => new PromotionRequirements { XP = 4400, DaysInRank = 56, EventsRequired = 25, BattlesRequired = 12, MinLanceReputation = 20, MinLeaderRelation = 20, MaxDiscipline = 6 },
                5 => new PromotionRequirements { XP = 6600, DaysInRank = 56, EventsRequired = 40, BattlesRequired = 20, MinLanceReputation = 30, MinLeaderRelation = 30, MaxDiscipline = 5 },
                6 => new PromotionRequirements { XP = 8800, DaysInRank = 56, EventsRequired = 55, BattlesRequired = 30, MinLanceReputation = 40, MinLeaderRelation = 15, MaxDiscipline = 4 },
                _ => new PromotionRequirements { XP = int.MaxValue, DaysInRank = 999, EventsRequired = 999, BattlesRequired = 999, MinLanceReputation = 999, MinLeaderRelation = 999, MaxDiscipline = 0 }
            };
        }
    }

    /// <summary>
    ///     Promotion and advancement system for military service progression.
    ///     This system handles tier advancement, promotion notifications, and integration
    ///     with the troop selection system. Implements 1-year military progression with
    ///     meaningful advancement milestones and realistic military economics.
    /// </summary>
    public sealed class PromotionBehavior : CampaignBehaviorBase
    {
        private bool _formationSelectionPending;

        // Promotion tracking
        private CampaignTime _lastPromotionCheck = CampaignTime.Zero;

        /// <summary>
        ///     Phase 7: Track pending promotion tier to prevent event spam.
        ///     Reset when promotion completes or is cancelled.
        /// </summary>
        private int _pendingPromotionTier;

        public PromotionBehavior()
        {
            Instance = this;
        }

        public static PromotionBehavior Instance { get; private set; }

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, OnHourlyTick);
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("_lastPromotionCheck", ref _lastPromotionCheck);
            dataStore.SyncData("_formationSelectionPending", ref _formationSelectionPending);
            dataStore.SyncData("_pendingPromotionTier", ref _pendingPromotionTier);
        }

        /// <summary>
        ///     Phase 7: Get the proving event ID for a tier transition.
        /// </summary>
        private static string GetProvingEventId(int fromTier, int toTier)
        {
            // Event IDs follow the pattern: promotion_t{from}_t{to}_*
            return fromTier switch
            {
                1 => "promotion_t1_t2_finding_your_place",
                2 => "promotion_t2_t3_sergeants_test",
                3 => "promotion_t3_t4_crisis_of_command",
                4 => "promotion_t4_t5_lance_vote",
                5 => "promotion_t5_t6_lord_audience",
                _ => $"promotion_t{fromTier}_t{toTier}" // Fallback pattern
            };
        }

        /// <summary>
        ///     Phase 7: Clear the pending promotion flag (called when event completes).
        /// </summary>
        public void ClearPendingPromotion()
        {
            _pendingPromotionTier = 0;
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            ModLogger.Info("Promotion", "Promotion system initialized");
        }

        /// <summary>
        ///     Phase 7: Check if player meets all requirements for promotion to the next tier.
        ///     Returns a tuple of (canPromote, failureReasons).
        /// </summary>
        public (bool CanPromote, List<string> FailureReasons) CanPromote()
        {
            var reasons = new List<string>();
            var enlistment = EnlistmentBehavior.Instance;
            
            if (enlistment?.IsEnlisted != true)
            {
                reasons.Add("Not enlisted");
                return (false, reasons);
            }

            var currentTier = enlistment.EnlistmentTier;
            if (currentTier >= 6)
            {
                reasons.Add("Already at maximum tier");
                return (false, reasons);
            }

            var targetTier = currentTier + 1;
            var req = PromotionRequirements.GetForTier(targetTier);
            var escalation = EscalationManager.Instance;

            // Check XP threshold
            if (enlistment.EnlistmentXP < req.XP)
            {
                reasons.Add($"XP: {enlistment.EnlistmentXP}/{req.XP}");
            }

            // Check days in rank
            if (enlistment.DaysInRank < req.DaysInRank)
            {
                reasons.Add($"Days in rank: {enlistment.DaysInRank}/{req.DaysInRank}");
            }

            // Check events completed
            if (enlistment.EventsCompleted < req.EventsRequired)
            {
                reasons.Add($"Events: {enlistment.EventsCompleted}/{req.EventsRequired}");
            }

            // Check battles survived
            if (enlistment.BattlesSurvived < req.BattlesRequired)
            {
                reasons.Add($"Battles: {enlistment.BattlesSurvived}/{req.BattlesRequired}");
            }

            // Check lance reputation (escalation system)
            if (escalation?.IsEnabled() == true)
            {
                var lanceRep = escalation.State?.LanceReputation ?? 0;
                if (lanceRep < req.MinLanceReputation)
                {
                    reasons.Add($"Lance reputation: {lanceRep}/{req.MinLanceReputation}");
                }

                var discipline = escalation.State?.Discipline ?? 0;
                if (discipline >= req.MaxDiscipline)
                {
                    reasons.Add($"Discipline too high: {discipline} (max: {req.MaxDiscipline - 1})");
                }
            }

            // Check leader relation
            if (enlistment.EnlistedLord != null)
            {
                var relation = enlistment.EnlistedLord.GetRelationWithPlayer();
                if (relation < req.MinLeaderRelation)
                {
                    reasons.Add($"Leader relation: {relation}/{req.MinLeaderRelation}");
                }
            }

            var canPromote = reasons.Count == 0;
            
            if (!canPromote)
            {
                ModLogger.Debug("Promotion", $"Cannot promote to T{targetTier}: {string.Join(", ", reasons)}");
            }

            return (canPromote, reasons);
        }

        /// <summary>
        ///     Phase 7: Get promotion progress as a percentage (0-100).
        ///     Useful for UI display.
        /// </summary>
        public int GetPromotionProgress()
        {
            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment?.IsEnlisted != true || enlistment.EnlistmentTier >= 6)
            {
                return 100;
            }

            var req = PromotionRequirements.GetForTier(enlistment.EnlistmentTier + 1);
            var escalation = EscalationManager.Instance;

            // Calculate progress for each requirement
            var xpProgress = Math.Min(100, enlistment.EnlistmentXP * 100 / Math.Max(1, req.XP));
            var daysProgress = Math.Min(100, enlistment.DaysInRank * 100 / Math.Max(1, req.DaysInRank));
            var eventsProgress = Math.Min(100, enlistment.EventsCompleted * 100 / Math.Max(1, req.EventsRequired));
            var battlesProgress = Math.Min(100, enlistment.BattlesSurvived * 100 / Math.Max(1, req.BattlesRequired));

            // Average progress (can weight differently if desired)
            var average = (xpProgress + daysProgress + eventsProgress + battlesProgress) / 4;
            return average;
        }

        /// <summary>
        ///     Hourly tick handler that checks for promotion eligibility once per in-game hour.
        ///     This provides responsive promotion detection without checking every frame,
        ///     allowing players to see promotions shortly after reaching XP thresholds.
        /// </summary>
        private void OnHourlyTick()
        {
            // Check every hour for promotion eligibility
            // This provides immediate response when XP thresholds are reached
            _lastPromotionCheck = CampaignTime.Now;
            CheckForPromotion();
        }

        /// <summary>
        ///     Check if player has reached promotion thresholds.
        ///     Phase 7: Now checks all promotion requirements (XP, days, events, battles, reputation).
        /// </summary>
        private void CheckForPromotion()
        {
            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment?.IsEnlisted != true)
            {
                return;
            }

            try
            {
                var currentTier = enlistment.EnlistmentTier;
                var previousTier = currentTier;
                var maxTier = Assignments.Core.ConfigurationManager.GetMaxTier();

                // Already at max tier
                if (currentTier >= maxTier)
                {
                    return;
                }

                // Phase 7: Check all promotion requirements
                var (canPromote, failureReasons) = CanPromote();
                
                if (!canPromote)
                {
                    // Log once per session when close to promotion but blocked
                    var progress = GetPromotionProgress();
                    if (progress >= 75)
                    {
                        ModLogger.LogOnce($"promo_blocked_t{currentTier + 1}", "Promotion",
                            $"Promotion to T{currentTier + 1} blocked ({progress}% progress): {string.Join(", ", failureReasons)}");
                    }
                    return;
                }

                // Phase 7: Player meets all requirements - trigger proving event instead of auto-promoting
                // The event's `promotes: true` effect will handle the actual tier advancement
                var targetTier = currentTier + 1;
                var eventId = GetProvingEventId(currentTier, targetTier);

                // Check if we've already triggered this promotion event recently (prevent spam)
                if (_pendingPromotionTier == targetTier)
                {
                    return; // Already pending
                }

                _pendingPromotionTier = targetTier;

                // Try to show the proving event
                if (Lances.Events.LanceLifeEventRuntime.TryShowEventById(eventId))
                {
                    ModLogger.Info("Promotion", $"Triggered proving event for T{currentTier}→T{targetTier}: {eventId}");
                    
                    // Prompt lance selection for T1→T2
                    if (currentTier == 1)
                    {
                        enlistment.TryPromptLanceSelection();
                    }
                }
                else
                {
                    // Fallback: If event not found or failed, do direct promotion
                    ModLogger.Warn("Promotion", $"Proving event {eventId} not found, using fallback promotion");
                    _pendingPromotionTier = 0;
                    
                    enlistment.SetTier(targetTier);
                    Features.Equipment.Behaviors.QuartermasterManager.Instance?.UpdateNewlyUnlockedItems();
                    TriggerPromotionNotification(targetTier);
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("Promotion", "Error checking for promotion", ex);
            }
        }

        /// <summary>
        ///     Trigger formation selection at Tier 2 (specialization choice).
        /// </summary>
        private void TriggerFormationSelection(int newTier)
        {
            try
            {
                _formationSelectionPending = true;

                var message = new TextObject("{=formation_select_msg}The serjeant surveys the recruits. 'Time to find your place in the line, soldier. What are you trained for?'");
                InformationManager.DisplayMessage(new InformationMessage(message.ToString(), Colors.Cyan));

                // Open formation selection (can be enhanced with custom menu later)
                ShowFormationSelectionOptions();

                ModLogger.Info("Promotion", $"Formation selection triggered for Tier {newTier}");
            }
            catch (Exception ex)
            {
                ModLogger.Error("Promotion", "Error triggering formation selection", ex);
            }
        }

        /// <summary>
        ///     Trigger promotion notification with immersive roleplay text.
        ///     Each tier has unique narrative describing what the promotion means.
        /// </summary>
        private void TriggerPromotionNotification(int newTier)
        {
            try
            {
                var rankName = GetRankName(newTier);
                var playerName = Hero.MainHero?.Name?.ToString() ?? "Soldier";
                
                // Get tier-specific localized title and message
                var titleText = GetPromotionTitle(newTier);
                var popupMessage = GetPromotionMessage(newTier);
                popupMessage.SetTextVariable("PLAYER_NAME", playerName);
                popupMessage.SetTextVariable("RANK", rankName);

                // Show short notification in chat
                var chatMessage = GetPromotionChatMessage(newTier);
                InformationManager.DisplayMessage(new InformationMessage(chatMessage.ToString(), Colors.Green));

                // Phase 7: Show quartermaster prompt after promotion (no auto-equip)
                var qmPrompt = new TextObject("{=promo_qm_prompt}Report to the Quartermaster for your new kit.");
                InformationManager.DisplayMessage(new InformationMessage(qmPrompt.ToString(), Colors.Cyan));

                // Get appropriate button text based on tier
                var buttonText = newTier >= 4 
                    ? new TextObject("{=promo_btn_command}To the Command Tent").ToString()
                    : new TextObject("{=promo_btn_understood}Understood").ToString();

                var data = new InquiryData(
                    titleText.ToString(),
                    popupMessage.ToString(),
                    true,
                    false,
                    buttonText,
                    "",
                    () => { },
                    null
                );

                // pauseGameActiveState = false so notifications don't freeze game time
                InformationManager.ShowInquiry(data, false);

                ModLogger.Info("Promotion", $"Promotion notification triggered for {rankName} (Tier {newTier})");
            }
            catch (Exception ex)
            {
                ModLogger.Error("Promotion", "Error showing promotion notification", ex);
            }
        }
        
        /// <summary>
        ///     Get tier-specific promotion popup title with culture-specific rank.
        /// </summary>
        private TextObject GetPromotionTitle(int tier)
        {
            var enlistment = EnlistmentBehavior.Instance;
            var rankName = RankHelper.GetRankTitle(tier, enlistment?.EnlistedLord?.Culture?.StringId ?? "mercenary");

            var title = tier switch
            {
                2 => new TextObject("{=promo_title_2}Recognized as {RANK}"),
                3 => new TextObject("{=promo_title_3}Rise to {RANK}"),
                4 => new TextObject("{=promo_title_4}Sworn as {RANK}"),
                5 => new TextObject("{=promo_title_5}Elevated to {RANK}"),
                6 => new TextObject("{=promo_title_6}Welcomed as {RANK}"),
                7 => new TextObject("{=promo_title_7}Appointed as {RANK}"),
                8 => new TextObject("{=promo_title_8}Commissioned as {RANK}"),
                9 => new TextObject("{=promo_title_9}Honored as {RANK}"),
                _ => new TextObject("{=promo_title_default}Promoted to {RANK}")
            };

            title.SetTextVariable("RANK", rankName);
            return title;
        }
        
        /// <summary>
        ///     Get tier-specific immersive promotion message from localization.
        /// </summary>
        private TextObject GetPromotionMessage(int tier)
        {
            return tier switch
            {
                2 => new TextObject("{=promo_msg_2}"),
                3 => new TextObject("{=promo_msg_3}"),
                4 => new TextObject("{=promo_msg_4}"),
                5 => new TextObject("{=promo_msg_5}"),
                6 => new TextObject("{=promo_msg_6}"),
                _ => new TextObject("{=promo_msg_default}You have been promoted to {RANK}.")
            };
        }
        
        /// <summary>
        ///     Get tier-specific short chat notification from localization.
        /// </summary>
        private TextObject GetPromotionChatMessage(int tier)
        {
            return tier switch
            {
                2 => new TextObject("{=promo_chat_2}"),
                3 => new TextObject("{=promo_chat_3}"),
                4 => new TextObject("{=promo_chat_4}"),
                5 => new TextObject("{=promo_chat_5}"),
                6 => new TextObject("{=promo_chat_6}"),
                _ => new TextObject("{=promo_chat_default}You have been promoted!")
            };
        }

        /// <summary>
        ///     Show formation selection options (simplified approach).
        ///     Can be enhanced with custom UI later.
        /// </summary>
        private void ShowFormationSelectionOptions()
        {
            try
            {
                // For now, auto-detect formation from current equipment
                // This can be enhanced with an interactive selection menu later
                var currentFormation = DetectPlayerFormation();

                var duties = EnlistedDutiesBehavior.Instance;
                duties?.SetPlayerFormation(currentFormation.ToString().ToLower());

                var formationName = GetFormationDisplayName(currentFormation);
                var message = new TextObject("{=formation_assigned}Formation assigned: {FORMATION}. Your training and equipment will reflect your role.");
                message.SetTextVariable("FORMATION", formationName);

                InformationManager.DisplayMessage(new InformationMessage(message.ToString(), Colors.Cyan));

                // Phase 7: Prompt to visit Quartermaster for formation-specific gear (no auto-equip)
                var qmPrompt = new TextObject("{=formation_qm_prompt}Report to the Quartermaster for your {FORMATION} kit.");
                qmPrompt.SetTextVariable("FORMATION", formationName);
                InformationManager.DisplayMessage(new InformationMessage(qmPrompt.ToString(), Colors.Cyan));

                _formationSelectionPending = false;

                // Now trigger regular promotion for Tier 2
                TriggerPromotionNotification(2);
            }
            catch (Exception ex)
            {
                ModLogger.Error("Promotion", "Error in formation selection", ex);
                _formationSelectionPending = false;
            }
        }

        /// <summary>
        ///     Detect player's current formation from equipment.
        /// </summary>
        private FormationType DetectPlayerFormation()
        {
            try
            {
                var hero = Hero.MainHero;
                var characterObject = hero?.CharacterObject;

                if (characterObject == null)
                {
                    return FormationType.Infantry; // Default fallback
                }

                // Detect military formation based on equipment characteristics
                // The formation type is determined by whether the character uses ranged weapons
                // and whether they are mounted, creating four distinct categories:
                // - Horse Archer: Ranged weapon + Mount
                // - Cavalry: Melee weapon + Mount
                // - Archer: Ranged weapon + No mount
                // - Infantry: Melee weapon + No mount (default)
                if (characterObject.IsRanged && characterObject.IsMounted)
                {
                    return FormationType.HorseArcher;
                }

                if (characterObject.IsMounted)
                {
                    return FormationType.Cavalry;
                }

                if (characterObject.IsRanged)
                {
                    return FormationType.Archer;
                }

                return FormationType.Infantry;
            }
            catch
            {
                return FormationType.Infantry; // Safe fallback
            }
        }

        /// <summary>
        ///     Get culture-specific rank name for tier.
        /// </summary>
        private string GetRankName(int tier)
        {
            // Use culture-specific ranks from RankHelper
            var enlistment = EnlistmentBehavior.Instance;
            return RankHelper.GetRankTitle(tier, enlistment?.EnlistedLord?.Culture?.StringId ?? "mercenary");
        }

        /// <summary>
        ///     Get culture-specific formation display name.
        /// </summary>
        private string GetFormationDisplayName(FormationType formation)
        {
            var enlistment = EnlistmentBehavior.Instance;
            var culture = enlistment?.CurrentLord?.Culture?.StringId ?? "empire";

            var formationNames = new Dictionary<string, Dictionary<FormationType, string>>
            {
                ["empire"] =
                    new()
                    {
                        [FormationType.Infantry] = "Imperial Legionary",
                        [FormationType.Archer] = "Imperial Sagittarius",
                        [FormationType.Cavalry] = "Imperial Equites",
                        [FormationType.HorseArcher] = "Imperial Equites Sagittarii"
                    },
                ["aserai"] =
                    new()
                    {
                        [FormationType.Infantry] = "Aserai Footman",
                        [FormationType.Archer] = "Aserai Marksman",
                        [FormationType.Cavalry] = "Aserai Mameluke",
                        [FormationType.HorseArcher] = "Aserai Desert Horse Archer"
                    },
                ["khuzait"] =
                    new()
                    {
                        [FormationType.Infantry] = "Khuzait Spearman",
                        [FormationType.Archer] = "Khuzait Hunter",
                        [FormationType.Cavalry] = "Khuzait Lancer",
                        [FormationType.HorseArcher] = "Khuzait Horse Archer"
                    },
                ["vlandia"] =
                    new()
                    {
                        [FormationType.Infantry] = "Vlandian Man-at-Arms",
                        [FormationType.Archer] = "Vlandian Crossbowman",
                        [FormationType.Cavalry] = "Vlandian Knight",
                        [FormationType.HorseArcher] = "Vlandian Mounted Crossbowman"
                    },
                ["sturgia"] =
                    new()
                    {
                        [FormationType.Infantry] = "Sturgian Warrior",
                        [FormationType.Archer] = "Sturgian Bowman",
                        [FormationType.Cavalry] = "Sturgian Druzhnik",
                        [FormationType.HorseArcher] = "Sturgian Mounted Archer"
                    },
                ["battania"] = new()
                {
                    [FormationType.Infantry] = "Battanian Clansman",
                    [FormationType.Archer] = "Battanian Skirmisher",
                    [FormationType.Cavalry] = "Battanian Mounted Warrior",
                    [FormationType.HorseArcher] = "Battanian Mounted Skirmisher"
                }
            };

            if (formationNames.ContainsKey(culture) && formationNames[culture].ContainsKey(formation))
            {
                return formationNames[culture][formation];
            }

            return formation.ToString(); // Fallback to enum name
        }
    }
}
