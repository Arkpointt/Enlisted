using System;
using System.Collections.Generic;
using Enlisted.Features.Assignments.Behaviors;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Features.Equipment.Behaviors;
using Enlisted.Mod.Core.Logging;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace Enlisted.Features.Ranks.Behaviors
{
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
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            ModLogger.Info("Promotion", "Promotion system initialized");
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
        ///     Implements 1-year progression system with meaningful milestones.
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
                var currentXp = enlistment.EnlistmentXP;

                var promoted = false;

                // Load tier XP requirements from progression_config.json
                // The requirements array contains XP thresholds needed to promote from each tier to the next
                var tierXpRequirements = Assignments.Core.ConfigurationManager.GetTierXpRequirements();

                // Get actual max tier from config (e.g., 6 for tiers 1-6)
                var maxTier = Assignments.Core.ConfigurationManager.GetMaxTier();

                // Check if the player has enough XP for promotion, and continue promoting
                // if they've accumulated enough XP for multiple tiers at once
                // This ensures players get all promotions they've earned immediately
                while (currentTier < maxTier && currentXp >= tierXpRequirements[currentTier])
                {
                    currentTier++;
                    promoted = true;

                    // Update enlistment tier immediately to reflect the promotion
                    enlistment.SetTier(currentTier);

                    ModLogger.Info("Promotion", $"Promoted to Tier {currentTier}");
                }

                // Handle promotion notifications
                if (promoted)
                {
                    // Special handling for Tier 2 (formation selection)
                    if (currentTier == 2 && !_formationSelectionPending)
                    {
                        TriggerFormationSelection(currentTier);
                    }
                    else
                    {
                        TriggerPromotionNotification(currentTier);
                    }
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

                InformationManager.ShowInquiry(data, true);

                ModLogger.Info("Promotion", $"Promotion notification triggered for {rankName} (Tier {newTier})");
            }
            catch (Exception ex)
            {
                ModLogger.Error("Promotion", "Error showing promotion notification", ex);
            }
        }
        
        /// <summary>
        ///     Get tier-specific promotion popup title from localization.
        /// </summary>
        private TextObject GetPromotionTitle(int tier)
        {
            return tier switch
            {
                2 => new TextObject("{=promo_title_2}Recognized as a Soldier"),
                3 => new TextObject("{=promo_title_3}Rise to Serjeant"),
                4 => new TextObject("{=promo_title_4}Sworn as Man-at-Arms"),
                5 => new TextObject("{=promo_title_5}Entrusted with the Banner"),
                6 => new TextObject("{=promo_title_6}Welcomed to the Household"),
                _ => new TextObject("{=promo_title_default}Promotion!")
            };
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
        ///     Get rank name for tier.
        /// </summary>
        private string GetRankName(int tier)
        {
            var rankNames = new Dictionary<int, string>
            {
                { 1, "Levy" },
                { 2, "Footman" },
                { 3, "Serjeant" },
                { 4, "Man-at-Arms" },
                { 5, "Banner Serjeant" },
                { 6, "Household Guard" }
            };

            return rankNames.TryGetValue(tier, out var rankName) ? rankName : $"Tier {tier}";
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
