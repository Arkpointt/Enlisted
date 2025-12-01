using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Features.Equipment.Behaviors;
using Enlisted.Features.Assignments.Behaviors;
using Enlisted.Features.Assignments.Core;
using Enlisted.Mod.Core.Logging;

namespace Enlisted.Features.Ranks.Behaviors
{
    /// <summary>
    /// Promotion and advancement system for military service progression.
    ///
    /// This system handles tier advancement, promotion notifications, and integration
    /// with the troop selection system. Implements 1-year military progression with
    /// meaningful advancement milestones and realistic military economics.
    /// </summary>
    public sealed class PromotionBehavior : CampaignBehaviorBase
    {
        public static PromotionBehavior Instance { get; private set; }

        // Promotion tracking
        private CampaignTime _lastPromotionCheck = CampaignTime.Zero;
        private bool _formationSelectionPending = false;

        public PromotionBehavior()
        {
            Instance = this;
        }

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
        /// Hourly tick handler that checks for promotion eligibility once per in-game hour.
        /// This provides responsive promotion detection without checking every frame,
        /// allowing players to see promotions shortly after reaching XP thresholds.
        /// </summary>
        private void OnHourlyTick()
        {
            // Check every hour for promotion eligibility
            // This provides immediate response when XP thresholds are reached
            _lastPromotionCheck = CampaignTime.Now;
            CheckForPromotion();
        }

        /// <summary>
        /// Check if player has reached promotion thresholds.
        /// Implements 1-year progression system with meaningful milestones.
        /// </summary>
        private void CheckForPromotion()
        {
            var enlistment = EnlistmentBehavior.Instance;
            if (!enlistment?.IsEnlisted == true)
            {
                return;
            }

            try
            {
                var currentTier = enlistment.EnlistmentTier;
                var currentXP = enlistment.EnlistmentXP;

                bool promoted = false;

                // Load tier XP requirements from progression_config.json
                // The requirements array contains XP thresholds needed to promote from each tier to the next
                var tierXPRequirements = Assignments.Core.ConfigurationManager.GetTierXPRequirements();

                // Get the maximum tier allowed to prevent promoting beyond tier 6
                int maxTier = tierXPRequirements.Length > 1 ? tierXPRequirements.Length - 1 : 1;

                // Check if the player has enough XP for promotion, and continue promoting
                // if they've accumulated enough XP for multiple tiers at once
                // This ensures players get all promotions they've earned immediately
                while (currentTier < maxTier && currentXP >= tierXPRequirements[currentTier])
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
        /// Trigger formation selection at Tier 2 (specialization choice).
        /// </summary>
        private void TriggerFormationSelection(int newTier)
        {
            try
            {
                _formationSelectionPending = true;

                var message = new TextObject("Specialization available! Choose your military formation to unlock specialized duties and equipment.");
                InformationManager.DisplayMessage(new InformationMessage(message.ToString()));

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
        /// Trigger promotion notification and troop selection.
        /// </summary>
        private void TriggerPromotionNotification(int newTier)
        {
            try
            {
                var rankName = GetRankName(newTier);
                var message = new TextObject("Promotion available! You can advance to {RANK} (Tier {TIER}). Visit the quartermaster to choose your equipment.");
                message.SetTextVariable("RANK", rankName);
                message.SetTextVariable("TIER", newTier.ToString());

                InformationManager.DisplayMessage(new InformationMessage(message.ToString()));

                // Show a popup inquiry to ensure the player notices the promotion
                var titleText = new TextObject("Promotion!");
                var popupMessage = new TextObject("Congratulations! You have been promoted to {RANK} (Tier {TIER}).\n\nVisit the quartermaster to update your equipment.");
                popupMessage.SetTextVariable("RANK", rankName);
                popupMessage.SetTextVariable("TIER", newTier.ToString());

                var data = new InquiryData(
                    titleText.ToString(),
                    popupMessage.ToString(),
                    true,
                    false,
                    "OK",
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
        /// Show formation selection options (simplified approach).
        /// Can be enhanced with custom UI later.
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
                var message = new TextObject("Formation specialized: {FORMATION}. New duties and equipment now available.");
                message.SetTextVariable("FORMATION", formationName);

                InformationManager.DisplayMessage(new InformationMessage(message.ToString()));

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
        /// Detect player's current formation from equipment.
        /// </summary>
        private Equipment.Behaviors.FormationType DetectPlayerFormation()
        {
            try
            {
                var hero = Hero.MainHero;
                var characterObject = hero?.CharacterObject;

                if (characterObject == null)
                {
                    return Equipment.Behaviors.FormationType.Infantry; // Default fallback
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
                    return Equipment.Behaviors.FormationType.HorseArcher;
                }
                else if (characterObject.IsMounted)
                {
                    return Equipment.Behaviors.FormationType.Cavalry;
                }
                else if (characterObject.IsRanged)
                {
                    return Equipment.Behaviors.FormationType.Archer;
                }
                else
                {
                    return Equipment.Behaviors.FormationType.Infantry;
                }
            }
            catch
            {
                return Equipment.Behaviors.FormationType.Infantry; // Safe fallback
            }
        }

        /// <summary>
        /// Get rank name for tier.
        /// </summary>
        private string GetRankName(int tier)
        {
            var rankNames = new Dictionary<int, string>
            {
                {1, "Levy"},
                {2, "Footman"},
                {3, "Serjeant"},
                {4, "Man-at-Arms"},
                {5, "Banner Sergeant"},
                {6, "Household Guard"}
            };

            return rankNames.ContainsKey(tier) ? rankNames[tier] : $"Tier {tier}";
        }

        /// <summary>
        /// Get culture-specific formation display name.
        /// </summary>
        private string GetFormationDisplayName(Equipment.Behaviors.FormationType formation)
        {
            var enlistment = EnlistmentBehavior.Instance;
            var culture = enlistment?.CurrentLord?.Culture?.StringId ?? "empire";

            var formationNames = new Dictionary<string, Dictionary<Equipment.Behaviors.FormationType, string>>
            {
                ["empire"] = new Dictionary<Equipment.Behaviors.FormationType, string>
                {
                    [Equipment.Behaviors.FormationType.Infantry] = "Imperial Legionary",
                    [Equipment.Behaviors.FormationType.Archer] = "Imperial Sagittarius",
                    [Equipment.Behaviors.FormationType.Cavalry] = "Imperial Equites",
                    [Equipment.Behaviors.FormationType.HorseArcher] = "Imperial Equites Sagittarii"
                },
                ["aserai"] = new Dictionary<Equipment.Behaviors.FormationType, string>
                {
                    [Equipment.Behaviors.FormationType.Infantry] = "Aserai Footman",
                    [Equipment.Behaviors.FormationType.Archer] = "Aserai Marksman",
                    [Equipment.Behaviors.FormationType.Cavalry] = "Aserai Mameluke",
                    [Equipment.Behaviors.FormationType.HorseArcher] = "Aserai Desert Horse Archer"
                },
                ["khuzait"] = new Dictionary<Equipment.Behaviors.FormationType, string>
                {
                    [Equipment.Behaviors.FormationType.Infantry] = "Khuzait Spearman",
                    [Equipment.Behaviors.FormationType.Archer] = "Khuzait Hunter",
                    [Equipment.Behaviors.FormationType.Cavalry] = "Khuzait Lancer",
                    [Equipment.Behaviors.FormationType.HorseArcher] = "Khuzait Horse Archer"
                },
                ["vlandia"] = new Dictionary<Equipment.Behaviors.FormationType, string>
                {
                    [Equipment.Behaviors.FormationType.Infantry] = "Vlandian Man-at-Arms",
                    [Equipment.Behaviors.FormationType.Archer] = "Vlandian Crossbowman",
                    [Equipment.Behaviors.FormationType.Cavalry] = "Vlandian Knight",
                    [Equipment.Behaviors.FormationType.HorseArcher] = "Vlandian Mounted Crossbowman"
                },
                ["sturgia"] = new Dictionary<Equipment.Behaviors.FormationType, string>
                {
                    [Equipment.Behaviors.FormationType.Infantry] = "Sturgian Warrior",
                    [Equipment.Behaviors.FormationType.Archer] = "Sturgian Bowman",
                    [Equipment.Behaviors.FormationType.Cavalry] = "Sturgian Druzhnik",
                    [Equipment.Behaviors.FormationType.HorseArcher] = "Sturgian Mounted Archer"
                },
                ["battania"] = new Dictionary<Equipment.Behaviors.FormationType, string>
                {
                    [Equipment.Behaviors.FormationType.Infantry] = "Battanian Clansman",
                    [Equipment.Behaviors.FormationType.Archer] = "Battanian Skirmisher",
                    [Equipment.Behaviors.FormationType.Cavalry] = "Battanian Mounted Warrior",
                    [Equipment.Behaviors.FormationType.HorseArcher] = "Battanian Mounted Skirmisher"
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
