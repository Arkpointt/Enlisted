using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Overlay;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using static TaleWorlds.CampaignSystem.GameMenus.GameMenu;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Features.Assignments.Behaviors;
using Enlisted.Mod.Core.Logging;

namespace Enlisted.Features.Interface.Behaviors
{
    /// <summary>
    /// Enhanced menu system for enlisted military service providing comprehensive status display,
    /// interactive duty management, and professional military interface.
    /// 
    /// This system provides rich, real-time information about military service status including
    /// detailed progression tracking, army information, duties management, and service records.
    /// </summary>
    public sealed class EnlistedMenuBehavior : CampaignBehaviorBase
    {
        public static EnlistedMenuBehavior Instance { get; private set; }
        
        // Menu update tracking
        private CampaignTime _lastMenuUpdate = CampaignTime.Zero;
        private readonly float _updateIntervalSeconds = 1.0f; // Update every second for real-time feel
        
        // Menu state tracking
        private string _currentMenuId = "";
        private bool _menuNeedsRefresh = false;

        public EnlistedMenuBehavior()
        {
            Instance = this;
        }

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.TickEvent.AddNonSerializedListener(this, OnTick);
            CampaignEvents.GameMenuOpened.AddNonSerializedListener(this, OnMenuOpened);
        }

        public override void SyncData(IDataStore dataStore)
        {
            // Menu behavior has no persistent state - all data comes from other behaviors
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            AddEnhancedEnlistedMenus(starter);
            ModLogger.Info("Interface", "Enhanced enlisted menu system initialized");
        }

        private void OnTick(float dt)
        {
            // Real-time menu updates for dynamic information
            if (CampaignTime.Now - _lastMenuUpdate > CampaignTime.Hours(_updateIntervalSeconds / 3600f))
            {
                if (_currentMenuId.StartsWith("enlisted_") && _menuNeedsRefresh)
                {
                    RefreshCurrentMenu();
                    _lastMenuUpdate = CampaignTime.Now;
                    _menuNeedsRefresh = false;
                }
            }
        }

        private void OnMenuOpened(MenuCallbackArgs args)
        {
            _currentMenuId = args.MenuContext.GameMenu.StringId;
            _menuNeedsRefresh = true;
        }

        /// <summary>
        /// Add all enhanced enlisted menus with comprehensive information display and interactivity.
        /// </summary>
        private void AddEnhancedEnlistedMenus(CampaignGameStarter starter)
        {
            AddMainEnlistedStatusMenu(starter);
            AddDutiesManagementMenu(starter);
            AddServiceRecordMenu(starter);
            AddTroopSelectionMenu(starter);
        }

        /// <summary>
        /// Enhanced main enlisted status menu with comprehensive military service information.
        /// </summary>
        private void AddMainEnlistedStatusMenu(CampaignGameStarter starter)
        {
            starter.AddWaitGameMenu("enlisted_status", 
                "Enlisted Status\n{ENLISTED_STATUS_TEXT}",
                new OnInitDelegate(OnEnlistedStatusInit),
                new OnConditionDelegate(OnEnlistedStatusCondition),
                null, // No consequence for wait menu
                null, // No tick handler for now
                GameMenu.MenuAndOptionType.WaitMenuShowOnlyProgressOption,
                GameOverlays.MenuOverlayType.None,
                1f, // Target hours (small value for immediate)
                GameMenu.MenuFlags.None,
                null);

            // Field medical treatment
            starter.AddGameMenuOption("enlisted_status", "enlisted_field_medical",
                new TextObject("{=field_medical_option}Request field medical treatment").ToString(),
                IsFieldMedicalAvailable,
                OnFieldMedicalSelected,
                false, 1);

            // Duties management
            starter.AddGameMenuOption("enlisted_status", "enlisted_duties_management",
                new TextObject("{=duties_management_option}Manage military duties ({ACTIVE_DUTIES_COUNT}/{MAX_DUTIES})").ToString(),
                IsDutiesManagementAvailable,
                OnDutiesManagementSelected,
                false, 2);

            // Equipment and advancement
            starter.AddGameMenuOption("enlisted_status", "enlisted_advancement",
                new TextObject("{=advancement_option}Equipment & advancement").ToString(),
                IsAdvancementAvailable,
                OnAdvancementSelected,
                false, 3);

            // Service record
            starter.AddGameMenuOption("enlisted_status", "enlisted_service_record",
                new TextObject("{=service_record_option}View detailed service record").ToString(),
                IsServiceRecordAvailable,
                OnServiceRecordSelected,
                false, 4);

            // Retirement (if eligible)
            starter.AddGameMenuOption("enlisted_status", "enlisted_retirement",
                new TextObject("{=retirement_option}Request honorable retirement").ToString(),
                IsRetirementAvailable,
                OnRetirementSelected,
                false, 5);

            // No "return to duties" option needed - player IS doing duties by being in this menu
        }

        /// <summary>
        /// Initialize enlisted status menu with current service information.
        /// </summary>
        private void OnEnlistedStatusInit(MenuCallbackArgs args)
        {
            try
            {
                args.MenuContext.GameMenu.StartWait();
                RefreshEnlistedStatusDisplay();
                _menuNeedsRefresh = true;
            }
            catch (Exception ex)
            {
                ModLogger.Error("Interface", $"Error initializing enlisted status menu: {ex.Message}");
            }
        }

        /// <summary>
        /// Refresh the enlisted status display with current information.
        /// </summary>
        private void RefreshEnlistedStatusDisplay()
        {
            try
            {
                var enlistment = EnlistmentBehavior.Instance;
                if (!enlistment?.IsEnlisted == true)
                {
                    MBTextManager.SetTextVariable("ENLISTED_STATUS_TEXT", "You are not currently enlisted.");
                    return;
                }

                var lord = enlistment.CurrentLord;
                var faction = lord?.MapFaction?.Name?.ToString() ?? "Unknown";
                var serviceDays = CalculateServiceDays(enlistment);
                var rank = $"Tier {enlistment.EnlistmentTier}/7";
                var experience = $"{enlistment.EnlistmentXP} XP";
                
                // Build status text
                var statusText = $"Lord: {lord?.Name?.ToString() ?? "Unknown"}\n";
                statusText += $"Faction: {faction}\n";
                statusText += $"Rank: {rank}\n";
                statusText += $"Experience: {experience}\n";
                statusText += $"Service Duration: {serviceDays} days\n\n";
                statusText += "Following your lord's commands...";

                MBTextManager.SetTextVariable("ENLISTED_STATUS_TEXT", statusText);
                
                ModLogger.Debug("Interface", $"Status refreshed - {lord?.Name} ({faction}) - {rank}");
            }
            catch (Exception ex)
            {
                ModLogger.Error("Interface", "Error refreshing enlisted status", ex);
                MBTextManager.SetTextVariable("ENLISTED_STATUS_TEXT", "Error loading status information.");
            }
        }

        /// <summary>
        /// Calculate service days from enlistment start.
        /// </summary>
        private int CalculateServiceDays(EnlistmentBehavior enlistment)
        {
            // Use XP as approximation for now (about 50 XP per day)
            return Math.Max(1, enlistment.EnlistmentXP / 50);
        }

        /// <summary>
        /// Get rank name for display based on tier and formation.
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
        /// Get formation display information with culture-specific names.
        /// </summary>
        private string GetFormationDisplayInfo(EnlistedDutiesBehavior duties)
        {
            if (duties == null) return "Infantry (Basic)";
            
            var formation = duties.GetPlayerFormationType();
            var culture = EnlistmentBehavior.Instance?.CurrentLord?.Culture?.StringId ?? "empire";
            
            var formationNames = new Dictionary<string, Dictionary<string, string>>
            {
                ["infantry"] = new Dictionary<string, string>
                {
                    ["empire"] = "Legionary", ["aserai"] = "Footman", ["khuzait"] = "Spearman",
                    ["vlandia"] = "Man-at-Arms", ["sturgia"] = "Warrior", ["battania"] = "Clansman"
                },
                ["archer"] = new Dictionary<string, string>
                {
                    ["empire"] = "Sagittarius", ["aserai"] = "Marksman", ["khuzait"] = "Hunter",
                    ["vlandia"] = "Crossbowman", ["sturgia"] = "Bowman", ["battania"] = "Skirmisher"
                },
                ["cavalry"] = new Dictionary<string, string>
                {
                    ["empire"] = "Equites", ["aserai"] = "Mameluke", ["khuzait"] = "Lancer", 
                    ["vlandia"] = "Knight", ["sturgia"] = "Druzhnik", ["battania"] = "Mounted Warrior"
                },
                ["horsearcher"] = new Dictionary<string, string>
                {
                    ["empire"] = "Equites Sagittarii", ["aserai"] = "Desert Horse Archer", ["khuzait"] = "Horse Archer",
                    ["vlandia"] = "Mounted Crossbowman", ["sturgia"] = "Mounted Archer", ["battania"] = "Mounted Skirmisher"
                }
            };

            if (formationNames.ContainsKey(formation) && formationNames[formation].ContainsKey(culture))
            {
                return $"{formationNames[formation][culture]} ({formation.ToTitleCase()})";
            }

            return $"{formation.ToTitleCase()} (Basic)";
        }

        /// <summary>
        /// Calculate service days from enlistment date.
        /// </summary>
        private int GetServiceDays(EnlistmentBehavior enlistment)
        {
            // This would need to be implemented in EnlistmentBehavior
            // For now, calculate based on XP (approximate)
            return enlistment.EnlistmentXP / 50; // ~50 XP per day average
        }

        /// <summary>
        /// Get retirement countdown display.
        /// </summary>
        private string GetRetirementCountdown(int serviceDays)
        {
            const int retirementDays = 365;
            var remaining = retirementDays - serviceDays;
            
            if (remaining <= 0)
            {
                return "Eligible for retirement";
            }
            
            return $"{remaining} days to retirement";
        }

        /// <summary>
        /// Get next tier XP requirement.
        /// </summary>
        private int GetNextTierXPRequirement(int currentTier)
        {
            var requirements = new int[] { 0, 500, 1500, 3500, 7000, 12000, 18000 };
            return currentTier < 7 ? requirements[currentTier] : 18000;
        }

        /// <summary>
        /// Calculate current daily wage with bonuses.
        /// </summary>
        private int CalculateCurrentDailyWage()
        {
            var enlistment = EnlistmentBehavior.Instance;
            var duties = EnlistedDutiesBehavior.Instance;
            
            if (!enlistment?.IsEnlisted == true) return 0;
            
            // Base wage calculation (from progression_config.json logic)
            var baseWage = 10 + Hero.MainHero.Level + (enlistment.EnlistmentTier * 5) + (enlistment.EnlistmentXP / 200);
            
            // Duty multiplier
            var dutyMultiplier = duties?.GetCurrentWageMultiplier() ?? 1.0f;
            
            // Army bonus
            var armyBonus = enlistment.CurrentLord?.PartyBelongedTo?.Army != null ? 1.2f : 1.0f;
            
            var totalWage = (int)(baseWage * dutyMultiplier * armyBonus);
            return Math.Min(totalWage, 150); // Cap at 150 as per realistic economics
        }

        /// <summary>
        /// Get officer skill value for display.
        /// </summary>
        private int GetOfficerSkillValue(string officerRole)
        {
            return officerRole switch
            {
                "Engineer" => Hero.MainHero.GetSkillValue(DefaultSkills.Engineering),
                "Scout" => Hero.MainHero.GetSkillValue(DefaultSkills.Scouting),
                "Quartermaster" => Hero.MainHero.GetSkillValue(DefaultSkills.Steward),
                "Surgeon" => Hero.MainHero.GetSkillValue(DefaultSkills.Medicine),
                _ => 0
            };
        }

        /// <summary>
        /// Get army status display with hierarchy information.
        /// </summary>
        private string GetArmyStatusDisplay(Hero lord)
        {
            var lordParty = lord?.PartyBelongedTo;
            if (lordParty?.Army == null)
            {
                return "Independent operations";
            }

            var army = lordParty.Army;
            var leaderName = army.LeaderParty?.LeaderHero?.Name?.ToString() ?? "Unknown";
            var totalStrength = army.TotalStrength;
            var cohesion = (int)(army.Cohesion * 100);

            return $"Following [{army.Name}] (Leader: {leaderName})\n" +
                   $"Army Strength: {totalStrength} troops | Cohesion: {cohesion}%";
        }

        /// <summary>
        /// Get current objective display based on lord's activities.
        /// </summary>
        private string GetCurrentObjectiveDisplay(Hero lord)
        {
            var lordParty = lord?.PartyBelongedTo;
            if (lordParty == null) return "";

            if (lordParty.Ai.DoNotMakeNewDecisions)
            {
                return "Following direct orders";
            }

            if (lordParty.IsActive && lordParty.MapEvent != null)
            {
                return $"Engaged in battle at {lordParty.MapEvent.MapEventSettlement?.Name?.ToString() ?? "field"}";
            }

            if (lordParty.CurrentSettlement != null)
            {
                var settlement = lordParty.CurrentSettlement;
                return $"Stationed at {settlement.Name}";
            }

            if (lordParty.Army != null)
            {
                return "Army operations";
            }

            return "Patrol duties";
        }

        /// <summary>
        /// Get dynamic status messages based on current conditions.
        /// </summary>
        private List<string> GetDynamicStatusMessages()
        {
            var messages = new List<string>();
            var enlistment = EnlistmentBehavior.Instance;
            var duties = EnlistedDutiesBehavior.Instance;

            // Promotion available
            if (CanPromote())
            {
                messages.Add("Promotion available! Press 'P' to advance your rank.");
            }

            // Medical treatment available
            if (Hero.MainHero.HitPoints < Hero.MainHero.MaxHitPoints)
            {
                var cooldownStatus = GetMedicalCooldownStatus();
                if (cooldownStatus == "Available")
                {
                    messages.Add("Medical treatment available to heal wounds.");
                }
                else
                {
                    messages.Add($"Medical supplies restocking ({cooldownStatus}).");
                }
            }

            // Officer duties active
            var officerRole = duties?.GetCurrentOfficerRole();
            if (!string.IsNullOrEmpty(officerRole))
            {
                messages.Add($"Serving as party {officerRole.ToLower()} - your {GetOfficerSkillName(officerRole)} skill affects the party.");
            }

            // Retirement eligibility
            if (GetServiceDays(enlistment) >= 365)
            {
                messages.Add("Eligible for honorable retirement with veteran benefits.");
            }

            return messages;
        }

        /// <summary>
        /// Check if promotion is available.
        /// </summary>
        private bool CanPromote()
        {
            var enlistment = EnlistmentBehavior.Instance;
            if (!enlistment?.IsEnlisted == true || enlistment.EnlistmentTier >= 7)
                return false;

            var nextTierXP = GetNextTierXPRequirement(enlistment.EnlistmentTier);
            return enlistment.EnlistmentXP >= nextTierXP;
        }

        /// <summary>
        /// Get medical cooldown status.
        /// </summary>
        private string GetMedicalCooldownStatus()
        {
            // This would need implementation in a medical system
            // For now, return available
            return "Available";
        }

        /// <summary>
        /// Get officer skill name for display.
        /// </summary>
        private string GetOfficerSkillName(string officerRole)
        {
            return officerRole switch
            {
                "Engineer" => "Engineering",
                "Scout" => "Scouting", 
                "Quartermaster" => "Steward",
                "Surgeon" => "Medicine",
                _ => "Unknown"
            };
        }

        /// <summary>
        /// Refresh current menu with updated information.
        /// </summary>
        private void RefreshCurrentMenu()
        {
            try
            {
                // Trigger menu refresh by updating text variables
                if (Campaign.Current?.GameMenuManager != null)
                {
                    // The menu text will be regenerated on next display
                    _menuNeedsRefresh = false;
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("Interface", $"Error refreshing menu: {ex.Message}");
            }
        }

        #region Menu Condition and Action Methods

        private bool OnEnlistedStatusCondition(MenuCallbackArgs args)
        {
            return EnlistmentBehavior.Instance?.IsEnlisted == true;
        }

        private void OnEnlistedStatusTick(MenuCallbackArgs args)
        {
            try
            {
                RefreshEnlistedStatusDisplay();
            }
            catch (Exception ex)
            {
                ModLogger.Error("Interface", $"Error during enlisted status tick: {ex.Message}");
            }
        }

        private bool IsFieldMedicalAvailable(MenuCallbackArgs args)
        {
            return EnlistmentBehavior.Instance?.IsEnlisted == true && 
                   Hero.MainHero.HitPoints < Hero.MainHero.MaxHitPoints;
        }

        private void OnFieldMedicalSelected(MenuCallbackArgs args)
        {
            // Implement field medical treatment
            var healAmount = 10; // Basic healing
            Hero.MainHero.Heal(healAmount);
            
            InformationManager.DisplayMessage(new InformationMessage(
                new TextObject("{=field_medical_complete}Army medics have treated your wounds.").ToString()));
                
            GameMenu.ActivateGameMenu("enlisted_status");
        }

        private bool IsDutiesManagementAvailable(MenuCallbackArgs args)
        {
            return EnlistmentBehavior.Instance?.IsEnlisted == true;
        }

        private void OnDutiesManagementSelected(MenuCallbackArgs args)
        {
            GameMenu.ActivateGameMenu("enlisted_duties_management");
        }

        private bool IsAdvancementAvailable(MenuCallbackArgs args)
        {
            return EnlistmentBehavior.Instance?.IsEnlisted == true;
        }

        private void OnAdvancementSelected(MenuCallbackArgs args)
        {
            GameMenu.ActivateGameMenu("enlisted_troop_selection");
        }

        private bool IsServiceRecordAvailable(MenuCallbackArgs args)
        {
            return EnlistmentBehavior.Instance?.IsEnlisted == true;
        }

        private void OnServiceRecordSelected(MenuCallbackArgs args)
        {
            GameMenu.ActivateGameMenu("enlisted_service_record");
        }

        private bool IsRetirementAvailable(MenuCallbackArgs args)
        {
            var enlistment = EnlistmentBehavior.Instance;
            return enlistment?.IsEnlisted == true && GetServiceDays(enlistment) >= 365;
        }

        private void OnRetirementSelected(MenuCallbackArgs args)
        {
            // Implement retirement dialog
            InformationManager.DisplayMessage(new InformationMessage(
                new TextObject("{=retirement_available}Retirement system coming in future update.").ToString()));
        }

        // "Return to duties" button removed - player IS doing duties by staying in enlisted menu

        #endregion

        #region Placeholder Menu Methods (To Be Implemented)

        private void AddDutiesManagementMenu(CampaignGameStarter starter)
        {
            // Placeholder for duties management menu
            starter.AddWaitGameMenu("enlisted_duties_management",
                "Duties Management\n{DUTIES_MANAGEMENT_TEXT}",
                new OnInitDelegate(OnDutiesManagementInit),
                new OnConditionDelegate(OnEnlistedStatusCondition),
                null,
                null,
                GameMenu.MenuAndOptionType.WaitMenuShowOnlyProgressOption,
                GameOverlays.MenuOverlayType.None,
                1f,
                GameMenu.MenuFlags.None,
                null);
                
            starter.AddGameMenuOption("enlisted_duties_management", "back_to_status",
                "Back to enlisted status",
                args => true,
                args => GameMenu.ActivateGameMenu("enlisted_status"));
        }

        private void AddServiceRecordMenu(CampaignGameStarter starter)
        {
            // Placeholder for service record menu
            starter.AddWaitGameMenu("enlisted_service_record", 
                "Service Record\n{SERVICE_RECORD_TEXT}",
                new OnInitDelegate(OnServiceRecordInit),
                new OnConditionDelegate(OnEnlistedStatusCondition),
                null,
                null,
                GameMenu.MenuAndOptionType.WaitMenuShowOnlyProgressOption,
                GameOverlays.MenuOverlayType.None,
                1f,
                GameMenu.MenuFlags.None,
                null);
                
            starter.AddGameMenuOption("enlisted_service_record", "back_to_status",
                "Back to enlisted status", 
                args => true,
                args => GameMenu.ActivateGameMenu("enlisted_status"));
        }

        private void AddTroopSelectionMenu(CampaignGameStarter starter)
        {
            // Placeholder for troop selection menu
            starter.AddWaitGameMenu("enlisted_troop_selection",
                "Equipment & Advancement\n{TROOP_SELECTION_TEXT}",
                new OnInitDelegate(OnTroopSelectionInit),
                new OnConditionDelegate(OnEnlistedStatusCondition),
                null,
                null,
                GameMenu.MenuAndOptionType.WaitMenuShowOnlyProgressOption,
                GameOverlays.MenuOverlayType.None,
                1f,
                GameMenu.MenuFlags.None,
                null);
                
            starter.AddGameMenuOption("enlisted_troop_selection", "back_to_status",
                "Back to enlisted status",
                args => true, 
                args => GameMenu.ActivateGameMenu("enlisted_status"));
        }

        private void OnDutiesManagementInit(MenuCallbackArgs args)
        {
            try
            {
                args.MenuContext.GameMenu.StartWait();
                MBTextManager.SetTextVariable("DUTIES_MANAGEMENT_TEXT", "Duties management menu will be implemented here.\n\nAvailable duties, assignment slots, and officer roles will be displayed.");
            }
            catch (Exception ex)
            {
                ModLogger.Error("Interface", $"Error initializing duties management menu: {ex.Message}");
            }
        }

        private void OnServiceRecordInit(MenuCallbackArgs args)
        {
            try
            {
                args.MenuContext.GameMenu.StartWait();
                MBTextManager.SetTextVariable("SERVICE_RECORD_TEXT", "Service record menu will be implemented here.\n\nBattle history, relationships, and progression details will be displayed.");
            }
            catch (Exception ex)
            {
                ModLogger.Error("Interface", $"Error initializing service record menu: {ex.Message}");
            }
        }

        private void OnTroopSelectionInit(MenuCallbackArgs args)
        {
            try
            {
                args.MenuContext.GameMenu.StartWait();
                MBTextManager.SetTextVariable("TROOP_SELECTION_TEXT", "Equipment and advancement menu will be implemented here.\n\nTroop selection, promotion system, and equipment management will be available.");
            }
            catch (Exception ex)
            {
                ModLogger.Error("Interface", $"Error initializing troop selection menu: {ex.Message}");
            }
        }

        #endregion
    }

    /// <summary>
    /// Extension methods for string formatting.
    /// </summary>
    public static class StringExtensions
    {
        public static string ToTitleCase(this string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;
            
            return char.ToUpper(input[0]) + input.Substring(1).ToLower();
        }
    }
}
