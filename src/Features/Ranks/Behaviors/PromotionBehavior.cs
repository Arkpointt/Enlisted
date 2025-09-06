using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Features.Equipment.Behaviors;
using Enlisted.Features.Assignments.Behaviors;
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
        
        // 1-year progression system (SAS enhanced)
        private readonly int[] _tierXPRequirements = { 0, 500, 1500, 3500, 7000, 12000, 18000 };
        
        public PromotionBehavior()
        {
            Instance = this;
        }
        
        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
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
        /// Daily tick to check for promotion eligibility.
        /// </summary>
        private void OnDailyTick()
        {
            // Only check once per day
            if (CampaignTime.Now - _lastPromotionCheck < CampaignTime.Days(1))
            {
                return;
            }
            
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
                
                // Check if eligible for next tier
                if (currentTier < 7 && currentXP >= _tierXPRequirements[currentTier])
                {
                    var newTier = currentTier + 1;
                    
                    // Special handling for Tier 2 (formation selection)
                    if (newTier == 2 && !_formationSelectionPending)
                    {
                        TriggerFormationSelection(newTier);
                    }
                    else
                    {
                        TriggerPromotionNotification(newTier);
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
                
                // SAS formation detection logic
                if (characterObject.IsRanged && characterObject.IsMounted)
                {
                    return Equipment.Behaviors.FormationType.HorseArcher;   // Bow + Horse
                }
                else if (characterObject.IsMounted)
                {
                    return Equipment.Behaviors.FormationType.Cavalry;       // Sword + Horse  
                }
                else if (characterObject.IsRanged)
                {
                    return Equipment.Behaviors.FormationType.Archer;        // Bow + No Horse
                }
                else
                {
                    return Equipment.Behaviors.FormationType.Infantry;      // Sword + No Horse (default)
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
                {1, "Recruit"},
                {2, "Private"}, 
                {3, "Corporal"},
                {4, "Sergeant"},
                {5, "Staff Sergeant"},
                {6, "Master Sergeant"},
                {7, "Veteran"}
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
