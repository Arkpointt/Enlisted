using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Overlay;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Conversation;
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
        /// Add SAS-style enlisted menu with exact format from screenshot.
        /// </summary>
        private void AddEnhancedEnlistedMenus(CampaignGameStarter starter)
        {
            AddMainEnlistedStatusMenu(starter);
            // SAS-style single menu approach - all functionality in one menu
        }

        /// <summary>
        /// Enhanced main enlisted status menu with comprehensive military service information.
        /// </summary>
        private void AddMainEnlistedStatusMenu(CampaignGameStarter starter)
        {
            // SAS-style wait menu (restore time controls but fix UI boxes differently)
            starter.AddWaitGameMenu("enlisted_status", 
                "Party Leader: {PARTY_LEADER}\n{PARTY_TEXT}",  // SAS format
                new OnInitDelegate(OnEnlistedStatusInit),
                new OnConditionDelegate(OnEnlistedStatusCondition),
                null, // No consequence for wait menu
                new OnTickDelegate(OnEnlistedStatusTick), // SAS tick handler
                GameMenu.MenuAndOptionType.WaitMenuHideProgressAndHoursOption, // SAS template - fixes UI boxes!
                GameOverlays.MenuOverlayType.None,
                0f, // No wait time - immediate display
                GameMenu.MenuFlags.None,
                null);

            // SAS EXACT MENU OPTIONS from screenshot
            
            // Master at Arms (promotion selection / troop equipment selection)
            starter.AddGameMenuOption("enlisted_status", "enlisted_master_at_arms",
                "Master at Arms",
                IsMasterAtArmsAvailable,
                OnMasterAtArmsSelected,
                false, 1);

            // Visit Quartermaster (SAS option 1 - Enhanced with equipment variants)
            starter.AddGameMenuOption("enlisted_status", "enlisted_quartermaster",
                "Visit Quartermaster",
                IsQuartermasterAvailable,
                OnQuartermasterSelected,
                false, 2);

            // My Lord... (SAS option 3 - renamed for clarity)
            starter.AddGameMenuOption("enlisted_status", "enlisted_talk_to",
                "My Lord...",
                IsTalkToAvailable,
                OnTalkToSelected,
                false, 3);

            // Report for Duty (NEW - duty and profession selection)
            starter.AddGameMenuOption("enlisted_status", "enlisted_report_duty",
                "Report for Duty",
                IsReportDutyAvailable,
                OnReportDutySelected,
                false, 4);

            // Ask commander for leave (moved to bottom)
            starter.AddGameMenuOption("enlisted_status", "enlisted_ask_leave",
                "Ask commander for leave",
                IsAskLeaveAvailable,
                OnAskLeaveSelected,
                false, 5);

            // No "return to duties" option needed - player IS doing duties by being in this menu
            
            // Add duty selection menu
            AddDutySelectionMenu(starter);
        }
        
        /// <summary>
        /// Add duty selection menu for choosing duties and professions.
        /// </summary>
        private void AddDutySelectionMenu(CampaignGameStarter starter)
        {
            // Use same wait menu format as main enlisted menu for consistency
            starter.AddWaitGameMenu("enlisted_duty_selection", 
                "Duty Selection: {DUTY_STATUS}\n{DUTY_TEXT}",
                new OnInitDelegate(OnDutySelectionInit),
                new OnConditionDelegate(OnDutySelectionCondition),
                null, // No consequence for wait menu
                new OnTickDelegate(OnDutySelectionTick),
                GameMenu.MenuAndOptionType.WaitMenuHideProgressAndHoursOption, // Same as main menu
                GameOverlays.MenuOverlayType.None,
                0f, // No wait time - immediate display
                GameMenu.MenuFlags.None,
                null);

            // BACK OPTION (first, like main menu style)
            starter.AddGameMenuOption("enlisted_duty_selection", "duty_back",
                "Back to enlisted status",
                args => true,
                OnDutyBackSelected,
                false, 1);

            // DUTIES HEADER
            starter.AddGameMenuOption("enlisted_duty_selection", "duties_header",
                "─── DUTIES ───",
                args => true, // Show but make it a display-only option
                args => 
                {
                    // Show message when clicked to indicate it's just a header
                    InformationManager.DisplayMessage(new InformationMessage(
                        new TextObject("This is a section header. Select duties below.").ToString()));
                },
                false, 2);

            // DUTY OPTIONS - Dynamic text based on current selection
            starter.AddGameMenuOption("enlisted_duty_selection", "duty_enlisted",
                "{DUTY_ENLISTED_TEXT}",
                IsDutyEnlistedAvailable,
                OnDutyEnlistedSelected,
                false, 3);
                
            starter.AddGameMenuOption("enlisted_duty_selection", "duty_forager",
                "{DUTY_FORAGER_TEXT}",
                IsDutyForagerAvailable,
                OnDutyForagerSelected,
                false, 4);
                
            starter.AddGameMenuOption("enlisted_duty_selection", "duty_sentry",
                "{DUTY_SENTRY_TEXT}",
                IsDutySentryAvailable,
                OnDutySentrySelected,
                false, 5);
                
            starter.AddGameMenuOption("enlisted_duty_selection", "duty_messenger",
                "{DUTY_MESSENGER_TEXT}",
                IsDutyMessengerAvailable,
                OnDutyMessengerSelected,
                false, 6);
                
            starter.AddGameMenuOption("enlisted_duty_selection", "duty_pioneer",
                "{DUTY_PIONEER_TEXT}",
                IsDutyPioneerAvailable,
                OnDutyPioneerSelected,
                false, 7);

            // SPACER between duties and professions
            starter.AddGameMenuOption("enlisted_duty_selection", "section_spacer",
                " ",
                args => true, // Show as visible separator
                args => { }, // No action when clicked
                true, 8); // Disabled = true makes it gray and non-clickable

            // PROFESSIONS HEADER  
            starter.AddGameMenuOption("enlisted_duty_selection", "professions_header",
                "─── PROFESSIONS ───",
                args => true, // Show but make it a display-only option
                args => 
                {
                    // Show message when clicked to indicate it's just a header
                    InformationManager.DisplayMessage(new InformationMessage(
                        new TextObject("This is a section header. Select professions below.").ToString()));
                },
                false, 9);

            // PROFESSION OPTIONS (T3+) - Dynamic text based on current selection
            // Remove "None" profession as requested by user
            
            starter.AddGameMenuOption("enlisted_duty_selection", "prof_quarterhand",
                "{PROF_QUARTERHAND_TEXT}",
                IsProfQuarterhandAvailable,
                OnProfQuarterhandSelected,
                false, 10);
                
            starter.AddGameMenuOption("enlisted_duty_selection", "prof_field_medic",
                "{PROF_FIELD_MEDIC_TEXT}",
                IsProfFieldMedicAvailable,
                OnProfFieldMedicSelected,
                false, 11);
                
            starter.AddGameMenuOption("enlisted_duty_selection", "prof_siegewright",
                "{PROF_SIEGEWRIGHT_TEXT}",
                IsProfSiegewrightAvailable,
                OnProfSiegewrightSelected,
                false, 12);
                
            starter.AddGameMenuOption("enlisted_duty_selection", "prof_drillmaster",
                "{PROF_DRILLMASTER_TEXT}",
                IsProfDrillmasterAvailable,
                OnProfDrillmasterSelected,
                false, 13);
                
            starter.AddGameMenuOption("enlisted_duty_selection", "prof_saboteur",
                "{PROF_SABOTEUR_TEXT}",
                IsProfSaboteurAvailable,
                OnProfSaboteurSelected,
                false, 14);
        }

        /// <summary>
        /// Initialize enlisted status menu with current service information.
        /// </summary>
        private void OnEnlistedStatusInit(MenuCallbackArgs args)
        {
            try
            {
                // CRITICAL: Start wait to enable time controls (from BLUEPRINT)
                args.MenuContext.GameMenu.StartWait();
                
                // Set time control mode to allow unpausing (from BLUEPRINT)
                Campaign.Current.SetTimeControlModeLock(false);
                Campaign.Current.TimeControlMode = CampaignTimeControlMode.StoppablePlay;
                
                RefreshEnlistedStatusDisplay();
                _menuNeedsRefresh = true;
            }
            catch (Exception ex)
            {
                ModLogger.Error("Interface", $"Error initializing enlisted status menu: {ex.Message}");
            }
        }

        /// <summary>
        /// Refresh the enlisted status display with enhanced military styling.
        /// Professional militaristic format with colors, symbols, and improved readability.
        /// </summary>
        private void RefreshEnlistedStatusDisplay(MenuCallbackArgs args = null)
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
                var duties = EnlistedDutiesBehavior.Instance;
                
                if (lord == null)
                {
                    MBTextManager.SetTextVariable("ENLISTED_STATUS_TEXT", "Error: No enlisted lord found.");
                    return;
                }

                // Build simple SAS-style status display using their proven format
                var statusContent = "";
                
                try
                {
                    // SAS EXACT FORMAT - Simple label : value pairs
                    
                    // Party Objective (SAS format)
                    var objective = GetCurrentObjectiveDisplay(lord);
                    statusContent += $"Party Objective : {objective}\n";
                    
                    // Enlistment Time (SAS format)
                    var enlistmentTime = GetSASEnlistmentTimeDisplay(enlistment);
                    statusContent += $"Enlistment Time : {enlistmentTime}\n";
                    
                    // Enlistment Tier (SAS exact format)
                    statusContent += $"Enlistment Tier : {enlistment.EnlistmentTier}\n";
                    
                    // Formation (SAS exact format)
                    string formationName = "Infantry"; // Default
                    try
                    {
                        if (duties?.IsInitialized == true)
                        {
                            var playerFormation = duties.GetPlayerFormationType();
                            formationName = playerFormation?.ToTitleCase() ?? "Infantry";
                        }
                    }
                    catch { /* Use default */ }
                    statusContent += $"Formation : {formationName}\n";
                    
                    
                    // Wage (SAS exact format with coin icon)
                    var dailyWage = CalculateCurrentDailyWage();
                    statusContent += $"Wage : {dailyWage}<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">\n";
                    
                    // Current Experience (SAS exact format)
                    statusContent += $"Current Experience : {enlistment.EnlistmentXP}\n";
                    
                    // Next Level Experience (SAS exact format)
                    if (enlistment.EnlistmentTier < 6)
                    {
                        var nextTierXP = GetNextTierXPRequirement(enlistment.EnlistmentTier + 1);
                        statusContent += $"Next Level Experience : {nextTierXP}\n";
                    }
                    
                    // Formation training description (explains daily skill development)
                    var formationDesc = GetFormationTrainingDescription();
                    statusContent += formationDesc;
                }
                catch
                {
                    // Fallback to simple display on any error
                    statusContent = $"Lord : {lord?.Name?.ToString() ?? "Unknown"}\n";
                    statusContent += $"Enlistment Tier : {enlistment.EnlistmentTier}\n";
                    statusContent += $"Current Experience : {enlistment.EnlistmentXP}";
                }

                // SAS STEP 3: Use SAS text variable format (exact replication)
                var lordName = lord?.EncyclopediaLinkWithName?.ToString() ?? lord?.Name?.ToString() ?? "Unknown";
                
                // Use SAS approach - MenuContext.GameMenu.GetText() and set variables
                var menuContext = args?.MenuContext ?? Campaign.Current.CurrentMenuContext;
                if (menuContext != null)
                {
                    var text = menuContext.GameMenu.GetText();
                    text.SetTextVariable("PARTY_LEADER", lordName);
                    text.SetTextVariable("PARTY_TEXT", statusContent);
                }
                else
                {
                    // Fallback for compatibility
                    MBTextManager.SetTextVariable("PARTY_LEADER", lordName);
                    MBTextManager.SetTextVariable("PARTY_TEXT", statusContent);
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("Interface", "Error refreshing enlisted status", ex);
                
                // Error fallback
                var menuContext = args?.MenuContext ?? Campaign.Current.CurrentMenuContext;
                if (menuContext != null)
                {
                    var text = menuContext.GameMenu.GetText();
                    text.SetTextVariable("PARTY_LEADER", "Error");
                    text.SetTextVariable("PARTY_TEXT", "Status information unavailable.");
                }
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
            if (duties == null) 
            {
                return "Infantry (Basic)";
            }
            
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
            var requirements = new int[] { 0, 500, 2000, 5000, 10000, 18000 };
            return currentTier < 6 ? requirements[currentTier] : 18000;
        }

        /// <summary>
        /// Get SAS-style enlistment time display with proper date formatting.
        /// </summary>
        private string GetSASEnlistmentTimeDisplay(EnlistmentBehavior enlistment)
        {
            try
            {
                // Calculate approximate enlistment date from XP (since we don't store actual date yet)
                var daysServed = enlistment.EnlistmentXP / 50; // ~50 XP per day
                var enlistmentDate = CampaignTime.Now - CampaignTime.Days(daysServed);
                
                // Format like SAS: "Summer 15, 1084"
                return enlistmentDate.ToString();
            }
            catch
            {
                return "Unknown";
            }
        }

        /// <summary>
        /// Get SAS-style wage display with bonuses shown separately.
        /// </summary>
        private string GetSASWageDisplay()
        {
            try
            {
                var baseWage = CalculateBaseDailyWage();
                var totalWage = CalculateCurrentDailyWage();
                var bonus = totalWage - baseWage;
                
                // SAS format: "145(+25)" when bonus applies, otherwise just "145"  
                if (bonus > 0)
                {
                    return $"{baseWage}(+{bonus})";
                }
                else
                {
                    return baseWage.ToString();
                }
            }
            catch
            {
                return "Unknown";
            }
        }

        /// <summary>
        /// Calculate base daily wage without bonuses.
        /// </summary>
        private int CalculateBaseDailyWage()
        {
            var enlistment = EnlistmentBehavior.Instance;
            if (!enlistment?.IsEnlisted == true) 
            {
                return 0;
            }
            
            // Base wage formula: 10 + (Level × 1) + (Tier × 5) + (XP ÷ 200)
            var baseWage = 10 + Hero.MainHero.Level + (enlistment.EnlistmentTier * 5) + (enlistment.EnlistmentXP / 200);
            return Math.Min(Math.Max(baseWage, 24), 150); // Cap between 24-150
        }

        /// <summary>
        /// Get SAS-style assignment description (exact format from screenshot).
        /// </summary>
        private string GetFormationTrainingDescription()
        {
            try
            {
                var duties = EnlistedDutiesBehavior.Instance;
                
                if (duties?.IsInitialized != true)
                {
                    return "You perform basic military duties and training.";
                }
                
                // Get player's formation and build dynamic description with highlighted skills
                var playerFormation = duties.GetPlayerFormationType();
                return BuildFormationDescriptionWithHighlights(playerFormation, duties);
            }
            catch (Exception ex)
            {
                ModLogger.Error("Interface", "Error getting formation training description", ex);
                return "You perform basic military duties and training.";
            }
        }
        
        /// <summary>
        /// Get formation description with manually highlighted skills and XP amounts.
        /// </summary>
        private string BuildFormationDescriptionWithHighlights(string formation, EnlistedDutiesBehavior duties)
        {
            switch (formation.ToLower())
            {
                case "infantry":
                    return "As an Infantryman, you march in formation, drill the shieldwall, and spar in camp, becoming stronger through Athletics, deadly with One-Handed and Two-Handed blades, disciplined with the Polearm, and practiced in Throwing weapons.";
                    
                case "cavalry":
                    return "Serving as a Cavalryman, you ride endless drills to master Riding, lower your Polearm for the charge, cut close with One-Handed steel, practice Two-Handed arms for brute force, and keep your Athletics sharp when dismounted.";
                    
                case "horsearcher":
                    return "As a Horse Archer, you train daily at mounted archery, honing Riding to control your horse, perfecting the draw of the Bow, casting Throwing weapons at the gallop, keeping a One-Handed sword at your side, and building Athletics on foot.";
                    
                case "archer":
                    return "As an Archer, you loose countless shafts with Bow and Crossbow, strengthen your stride through Athletics, and sharpen your edge with a One-Handed blade for when the line closes.";
                    
                default:
                    return "You perform basic military duties and training as assigned.";
            }
        }

        /// <summary>
        /// Calculate current daily wage with bonuses.
        /// </summary>
        private int CalculateCurrentDailyWage()
        {
            var enlistment = EnlistmentBehavior.Instance;
            var duties = EnlistedDutiesBehavior.Instance;
            
            if (!enlistment?.IsEnlisted == true) 
            {
                return 0;
            }
            
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
            if (lordParty == null) 
            {
                return "";
            }

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
                var cooldownStatus = "Available"; // Simplified for SAS-style menu
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
            {
                return false;
            }

            var nextTierXP = GetNextTierXPRequirement(enlistment.EnlistmentTier);
            return enlistment.EnlistmentXP >= nextTierXP;
        }

        // GetMedicalCooldownStatus method removed - not used in SAS-style menu

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
            var isEnlisted = EnlistmentBehavior.Instance?.IsEnlisted == true;
            
            if (isEnlisted)
            {
                // Refresh the display when condition is checked
                RefreshEnlistedStatusDisplay(args);
            }
            
            return isEnlisted;
        }

        // SAS Menu Option Conditions and Actions

        private bool IsMasterAtArmsAvailable(MenuCallbackArgs args)
        {
            return EnlistmentBehavior.Instance?.IsEnlisted == true;
        }

        private void OnMasterAtArmsSelected(MenuCallbackArgs args)
        {
            try
            {
                var manager = Features.Equipment.Behaviors.TroopSelectionManager.Instance;
                if (manager == null)
                {
                    InformationManager.DisplayMessage(new InformationMessage(
                        new TextObject("Master at Arms system is temporarily unavailable.").ToString()));
                    return;
                }

                manager.ShowMasterAtArmsPopup();
            }
            catch (Exception ex)
            {
                ModLogger.Error("Interface", $"Error opening Master at Arms: {ex.Message}");
            }
        }

        private bool IsQuartermasterAvailable(MenuCallbackArgs args)
        {
            return EnlistmentBehavior.Instance?.IsEnlisted == true;
        }

        private void OnQuartermasterSelected(MenuCallbackArgs args)
        {
            try
            {
                var enlistment = EnlistmentBehavior.Instance;
                if (!enlistment?.IsEnlisted == true)
                {
                    InformationManager.DisplayMessage(new InformationMessage(
                        new TextObject("You must be enlisted to access quartermaster services.").ToString()));
                    return;
                }

                // Connect to new Quartermaster system
                var quartermasterManager = Features.Equipment.Behaviors.QuartermasterManager.Instance;
                if (quartermasterManager != null)
                {
                    // Show equipment variants for current troop selection
                    GameMenu.ActivateGameMenu("quartermaster_equipment");
                }
                else
                {
                    InformationManager.DisplayMessage(new InformationMessage(
                        new TextObject("Quartermaster services temporarily unavailable.").ToString()));
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("Interface", "Error accessing quartermaster services", ex);
                InformationManager.DisplayMessage(new InformationMessage(
                    new TextObject("Quartermaster system error. Please report this issue.").ToString()));
            }
        }


        private bool IsTalkToAvailable(MenuCallbackArgs args)
        {
            return EnlistmentBehavior.Instance?.IsEnlisted == true;
        }

        private void OnTalkToSelected(MenuCallbackArgs args)
        {
            try
            {
                var enlistment = EnlistmentBehavior.Instance;
                if (!enlistment?.IsEnlisted == true)
                {
                    InformationManager.DisplayMessage(new InformationMessage(
                        new TextObject("You must be enlisted to speak with lords.").ToString()));
                    return;
                }

                // Find nearby lords for conversation
                var nearbyLords = GetNearbyLordsForConversation();
                if (nearbyLords.Count == 0)
                {
                    InformationManager.DisplayMessage(new InformationMessage(
                        new TextObject("No lords are available for conversation at this location.").ToString()));
                    return;
                }

                // Show lord selection inquiry
                ShowLordSelectionInquiry(nearbyLords);
            }
            catch (Exception ex)
            {
                ModLogger.Error("Interface", $"Error in Talk to My Lord: {ex.Message}");
            }
        }

        /// <summary>
        /// Find nearby lords available for conversation using current TaleWorlds APIs.
        /// </summary>
        private List<Hero> GetNearbyLordsForConversation()
        {
            var nearbyLords = new List<Hero>();
            try
            {
                var mainParty = MobileParty.MainParty;
                if (mainParty == null)
                {
                    return nearbyLords;
                }

                // Check all mobile parties using verified API
                foreach (var party in MobileParty.All)
                {
                    if (party == null || party == mainParty || !party.IsActive)
                    {
                        continue;
                    }

                    // Check if party is close enough for conversation (same position or very close)
                    var distance = mainParty.Position2D.Distance(party.Position2D);
                    if (distance > 2.0f) // Reasonable conversation distance
                    {
                        continue;
                    }

                    var lord = party.LeaderHero;
                    if (lord != null && lord.IsLord && lord.IsAlive && !lord.IsPrisoner)
                    {
                        nearbyLords.Add(lord);
                    }
                }

                // Always include your enlisted lord if available
                var enlistment = EnlistmentBehavior.Instance;
                if (enlistment?.CurrentLord != null && !nearbyLords.Contains(enlistment.CurrentLord))
                {
                    nearbyLords.Insert(0, enlistment.CurrentLord); // Put enlisted lord first
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("Interface", $"Error finding nearby lords: {ex.Message}");
            }

            return nearbyLords;
        }

        /// <summary>
        /// Show lord selection inquiry with portraits.
        /// </summary>
        private void ShowLordSelectionInquiry(List<Hero> lords)
        {
            try
            {
                var options = new List<InquiryElement>();
                foreach (var lord in lords)
                {
                    var name = lord.Name?.ToString() ?? "Unknown Lord";
                    var portrait = new ImageIdentifier(CharacterCode.CreateFrom(lord.CharacterObject));
                    var description = $"{lord.Clan?.Name?.ToString() ?? "Unknown Clan"}\n{lord.MapFaction?.Name?.ToString() ?? "Unknown Faction"}";
                    
                    options.Add(new InquiryElement(lord, name, portrait, true, description));
                }

                var data = new MultiSelectionInquiryData(
                    titleText: "Select lord to speak with",
                    descriptionText: string.Empty,
                    inquiryElements: options,
                    isExitShown: true,
                    minSelectableOptionCount: 1,
                    maxSelectableOptionCount: 1,
                    affirmativeText: "Talk",
                    negativeText: "Cancel",
                    affirmativeAction: selected =>
                    {
                        try
                        {
                            var chosenLord = selected?.FirstOrDefault()?.Identifier as Hero;
                            if (chosenLord != null)
                            {
                                StartConversationWithLord(chosenLord);
                            }
                        }
                        catch (Exception ex)
                        {
                            ModLogger.Error("Interface", $"Error starting lord conversation: {ex.Message}");
                        }
                    },
                    negativeAction: _ =>
                    {
                        // Return to enlisted status menu
                        if (Campaign.Current?.CurrentMenuContext != null)
                        {
                            GameMenu.ActivateGameMenu("enlisted_status");
                        }
                    },
                    soundEventPath: string.Empty);

                MBInformationManager.ShowMultiSelectionInquiry(data);
            }
            catch (Exception ex)
            {
                ModLogger.Error("Interface", $"Error showing lord selection: {ex.Message}");
            }
        }

        /// <summary>
        /// Start conversation with selected lord using verified TaleWorlds APIs.
        /// </summary>
        private void StartConversationWithLord(Hero lord)
        {
            try
            {
                if (lord?.PartyBelongedTo == null)
                {
                    InformationManager.DisplayMessage(new InformationMessage(
                        new TextObject("Lord is not available for conversation.").ToString()));
                    return;
                }

                // Use the same conversation system our dialogs use
                CampaignMapConversation.OpenConversation(new ConversationCharacterData(CharacterObject.PlayerCharacter, PartyBase.MainParty), 
                                                        new ConversationCharacterData(lord.CharacterObject, lord.PartyBelongedTo.Party));
            }
            catch (Exception ex)
            {
                ModLogger.Error("Interface", $"Error opening conversation with {lord?.Name}: {ex.Message}");
                InformationManager.DisplayMessage(new InformationMessage(
                    new TextObject("Unable to start conversation. Please try again.").ToString()));
            }
        }


        private bool IsAskLeaveAvailable(MenuCallbackArgs args)
        {
            return EnlistmentBehavior.Instance?.IsEnlisted == true;
        }

        private void OnAskLeaveSelected(MenuCallbackArgs args)
        {
            try
            {
                var enlistment = EnlistmentBehavior.Instance;
                if (!enlistment?.IsEnlisted == true)
                {
                    return;
                }

                // Show leave request confirmation
                var titleText = "Request Leave from Commander";
                var descriptionText = "Request temporary leave from military service. You will regain independent movement but forfeit daily wages and duties until you return.";
                
                var confirmData = new InquiryData(
                    titleText,
                    descriptionText,
                    isAffirmativeOptionShown: true,
                    isNegativeOptionShown: true,
                    affirmativeText: "Request Leave",
                    negativeText: "Cancel",
                    affirmativeAction: () =>
                    {
                        try
                        {
                            RequestTemporaryLeave();
                        }
                        catch (Exception ex)
                        {
                            ModLogger.Error("Interface", $"Error requesting leave: {ex.Message}");
                        }
                    },
                    negativeAction: () =>
                    {
                        // Return to enlisted status menu
                        if (Campaign.Current?.CurrentMenuContext != null)
                        {
                            GameMenu.ActivateGameMenu("enlisted_status");
                        }
                    });

                InformationManager.ShowInquiry(confirmData);
            }
            catch (Exception ex)
            {
                ModLogger.Error("Interface", $"Error in Ask for Leave: {ex.Message}");
            }
        }

        /// <summary>
        /// Request temporary leave from service using our established EnlistmentBehavior patterns.
        /// </summary>
        private void RequestTemporaryLeave()
        {
            try
            {
                var enlistment = EnlistmentBehavior.Instance;
                if (!enlistment?.IsEnlisted == true)
                {
                    return;
                }

                // Use temporary leave instead of permanent discharge
                enlistment.StartTemporaryLeave();

                var message = new TextObject("Leave granted. You are temporarily released from service. Speak with your lord when ready to return to duty.");
                InformationManager.DisplayMessage(new InformationMessage(message.ToString()));

                // Exit menu to campaign map
                while (Campaign.Current.CurrentMenuContext != null)
                {
                    GameMenu.ExitToLast();
                }

                ModLogger.Info("Interface", "Temporary leave granted using proper StopEnlist method");
            }
            catch (Exception ex)
            {
                ModLogger.Error("Interface", $"Error granting temporary leave: {ex.Message}");
            }
        }


        /// <summary>
        /// SAS-style tick handler for real-time menu updates.
        /// Updates information continuously like the original SAS mod.
        /// </summary>
        private void OnEnlistedStatusTick(MenuCallbackArgs args, CampaignTime dt)
        {
            try
            {
                // SAS pattern - refresh every tick for real-time information
                RefreshEnlistedStatusDisplay(args);
                
                // SAS safety - auto-exit if not enlisted
                if (!EnlistmentBehavior.Instance?.IsEnlisted == true)
                {
                    GameMenu.ExitToLast();
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("Interface", $"Error during enlisted status tick: {ex.Message}");
            }
        }

        // Old field medical methods removed - replaced with SAS-style options

        // Old menu methods removed - replaced with SAS-style options

        /// <summary>
        /// Check if Report for Duty option should be available.
        /// </summary>
        private bool IsReportDutyAvailable(MenuCallbackArgs args)
        {
            var enlistment = EnlistmentBehavior.Instance;
            return enlistment?.IsEnlisted == true;
        }
        
        /// <summary>
        /// Handle Report for Duty selection - open duty selection menu.
        /// </summary>
        private void OnReportDutySelected(MenuCallbackArgs args)
        {
            try
            {
                GameMenu.SwitchToMenu("enlisted_duty_selection");
            }
            catch (Exception ex)
            {
                ModLogger.Error("Interface", $"Error opening Report for Duty: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Initialize duty selection menu with consistent SAS-style formatting.
        /// </summary>
        private void OnDutySelectionInit(MenuCallbackArgs args)
        {
            try
            {
                // CRITICAL: Start wait to enable time controls (same as main menu)
                args.MenuContext.GameMenu.StartWait();
                
                // Set time control mode to allow unpausing (same as main menu)
                Campaign.Current.SetTimeControlModeLock(false);
                Campaign.Current.TimeControlMode = CampaignTimeControlMode.StoppablePlay;
                
                // Initialize dynamic menu text on load
                var enlistment = EnlistmentBehavior.Instance;
                if (enlistment?.IsEnlisted == true)
                {
                    SetDynamicMenuText(enlistment);
                }
                
                RefreshDutySelectionDisplay();
                _menuNeedsRefresh = true;
            }
            catch (Exception ex)
            {
                ModLogger.Error("Interface", $"Error initializing duty selection menu: {ex.Message}");
            }
        }

        /// <summary>
        /// Condition check for duty selection menu (same pattern as main menu).
        /// </summary>
        private bool OnDutySelectionCondition(MenuCallbackArgs args)
        {
            var isEnlisted = EnlistmentBehavior.Instance?.IsEnlisted == true;
            
            if (isEnlisted)
            {
                // Refresh the display when condition is checked
                RefreshDutySelectionDisplay(args);
            }
            
            return isEnlisted;
        }

        /// <summary>
        /// Tick handler for duty selection menu (same pattern as main menu).
        /// </summary>
        private void OnDutySelectionTick(MenuCallbackArgs args, CampaignTime dt)
        {
            try
            {
                // Refresh every tick for real-time information (same as main menu)
                RefreshDutySelectionDisplay(args);
                
                // Auto-exit if not enlisted
                if (!EnlistmentBehavior.Instance?.IsEnlisted == true)
                {
                    GameMenu.ExitToLast();
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("Interface", $"Error during duty selection tick: {ex.Message}");
            }
        }

        /// <summary>
        /// Refresh duty selection display with dynamic checkmarks for current selections.
        /// </summary>
        private void RefreshDutySelectionDisplay(MenuCallbackArgs args = null)
        {
            try
            {
                var enlistment = EnlistmentBehavior.Instance;
                if (!enlistment?.IsEnlisted == true)
                {
                    return;
                }

                // Build status content using the same clean format as main menu
                var statusContent = "";
                
                // Current assignments with descriptions
                var currentDuty = GetDutyDisplayName(enlistment.SelectedDuty);
                var currentProfession = enlistment.SelectedProfession == "none" ? "None" : GetProfessionDisplayName(enlistment.SelectedProfession);
                
                statusContent += $"Current Duty : {currentDuty}\n";
                statusContent += $"Current Profession : {currentProfession}\n\n";
                
                // Add detailed descriptions for current assignments
                var dutyDescription = GetDutyDescription(enlistment.SelectedDuty);
                var professionDescription = GetProfessionDescription(enlistment.SelectedProfession);
                
                statusContent += $"DUTY ASSIGNMENT: {dutyDescription}\n\n";
                if (enlistment.SelectedProfession != "none")
                {
                    statusContent += $"PROFESSION: {professionDescription}\n\n";
                }
                
                // Show the selected profession description instead of instructions
                if (enlistment.SelectedProfession == "none")
                {
                    statusContent += "None";
                }
                else
                {
                    statusContent += GetProfessionDescription(enlistment.SelectedProfession);
                }

                // Set dynamic text variables for menu options with correct checkmarks
                SetDynamicMenuText(enlistment);

                // Use same text variable format as main menu
                var menuContext = args?.MenuContext ?? Campaign.Current.CurrentMenuContext;
                if (menuContext != null)
                {
                    var text = menuContext.GameMenu.GetText();
                    text.SetTextVariable("DUTY_STATUS", "Report for Duty");
                    text.SetTextVariable("DUTY_TEXT", statusContent);
                }
                else
                {
                    // Fallback for compatibility
                    MBTextManager.SetTextVariable("DUTY_STATUS", "Report for Duty");
                    MBTextManager.SetTextVariable("DUTY_TEXT", statusContent);
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("Interface", "Error refreshing duty selection display", ex);
                
                // Error fallback
                var menuContext = args?.MenuContext ?? Campaign.Current.CurrentMenuContext;
                if (menuContext != null)
                {
                    var text = menuContext.GameMenu.GetText();
                    text.SetTextVariable("DUTY_STATUS", "Error");
                    text.SetTextVariable("DUTY_TEXT", "Assignment information unavailable.");
                }
            }
        }

        /// <summary>
        /// Set dynamic text variables for menu options based on current selections.
        /// </summary>
        private void SetDynamicMenuText(EnlistmentBehavior enlistment)
        {
            var selectedDuty = enlistment.SelectedDuty;
            var selectedProfession = enlistment.SelectedProfession;

            // DUTY TEXT VARIABLES - Show checkmark for selected, circle for others (clean names only)
            MBTextManager.SetTextVariable("DUTY_ENLISTED_TEXT", 
                selectedDuty == "enlisted" ? "✓ Enlisted" : "○ Enlisted");
            
            MBTextManager.SetTextVariable("DUTY_FORAGER_TEXT", 
                selectedDuty == "forager" ? "✓ Forager" : "○ Forager");
            
            MBTextManager.SetTextVariable("DUTY_SENTRY_TEXT", 
                selectedDuty == "sentry" ? "✓ Sentry" : "○ Sentry");
            
            MBTextManager.SetTextVariable("DUTY_MESSENGER_TEXT", 
                selectedDuty == "messenger" ? "✓ Messenger" : "○ Messenger");
            
            MBTextManager.SetTextVariable("DUTY_PIONEER_TEXT", 
                selectedDuty == "pioneer" ? "✓ Pioneer" : "○ Pioneer");

            // PROFESSION TEXT VARIABLES - Show checkmark for selected, circle for others (clean names only)
            // "None" is default but invisible, show checkmark only when actual profession selected
            
            MBTextManager.SetTextVariable("PROF_QUARTERHAND_TEXT", 
                selectedProfession == "quarterhand" ? "✓ Quarterhand" : "○ Quarterhand");
            
            MBTextManager.SetTextVariable("PROF_FIELD_MEDIC_TEXT", 
                selectedProfession == "field_medic" ? "✓ Field Medic" : "○ Field Medic");
            
            MBTextManager.SetTextVariable("PROF_SIEGEWRIGHT_TEXT", 
                selectedProfession == "siegewright_aide" ? "✓ Siegewright's Aide" : "○ Siegewright's Aide");
            
            MBTextManager.SetTextVariable("PROF_DRILLMASTER_TEXT", 
                selectedProfession == "drillmaster" ? "✓ Drillmaster" : "○ Drillmaster");
            
            MBTextManager.SetTextVariable("PROF_SABOTEUR_TEXT", 
                selectedProfession == "saboteur" ? "✓ Saboteur" : "○ Saboteur");
        }

        #region Duty Selection Conditions and Actions
        
        // DUTY CONDITIONS (show option as available - ALL duties are always available)
        private bool IsDutyEnlistedAvailable(MenuCallbackArgs args) => true;
        private bool IsDutyForagerAvailable(MenuCallbackArgs args) => true;  
        private bool IsDutySentryAvailable(MenuCallbackArgs args) => true;
        private bool IsDutyMessengerAvailable(MenuCallbackArgs args) => true;
        private bool IsDutyPioneerAvailable(MenuCallbackArgs args) => true;

        // PROFESSION CONDITIONS (show option as available - Always visible, tier check in action)
        // Removed IsProfNoneAvailable as we removed the "None" option
            
        private bool IsProfQuarterhandAvailable(MenuCallbackArgs args) => true;
        private bool IsProfFieldMedicAvailable(MenuCallbackArgs args) => true;
        private bool IsProfSiegewrightAvailable(MenuCallbackArgs args) => true;
        private bool IsProfDrillmasterAvailable(MenuCallbackArgs args) => true;
        private bool IsProfSaboteurAvailable(MenuCallbackArgs args) => true;

        // DUTY ACTIONS
        private void OnDutyEnlistedSelected(MenuCallbackArgs args) => 
            SelectDuty("enlisted", "Enlisted");
            
        private void OnDutyForagerSelected(MenuCallbackArgs args) => 
            SelectDuty("forager", "Forager");
            
        private void OnDutySentrySelected(MenuCallbackArgs args) => 
            SelectDuty("sentry", "Sentry");
            
        private void OnDutyMessengerSelected(MenuCallbackArgs args) => 
            SelectDuty("messenger", "Messenger");
            
        private void OnDutyPioneerSelected(MenuCallbackArgs args) => 
            SelectDuty("pioneer", "Pioneer");

        // PROFESSION ACTIONS (with tier checking)
        // Removed OnProfNoneSelected as we removed the "None" option
            
        private void OnProfQuarterhandSelected(MenuCallbackArgs args) => 
            SelectProfessionWithTierCheck("quarterhand", "Quarterhand");
            
        private void OnProfFieldMedicSelected(MenuCallbackArgs args) => 
            SelectProfessionWithTierCheck("field_medic", "Field Medic");
            
        private void OnProfSiegewrightSelected(MenuCallbackArgs args) => 
            SelectProfessionWithTierCheck("siegewright_aide", "Siegewright's Aide");
            
        private void OnProfDrillmasterSelected(MenuCallbackArgs args) => 
            SelectProfessionWithTierCheck("drillmaster", "Drillmaster");
            
        private void OnProfSaboteurSelected(MenuCallbackArgs args) => 
            SelectProfessionWithTierCheck("saboteur", "Saboteur");

        private void OnDutyBackSelected(MenuCallbackArgs args)
        {
            GameMenu.SwitchToMenu("enlisted_status");
        }

        #endregion
        
        #region Duty Selection Helper Methods
        
        /// <summary>
        /// Select a new duty and show confirmation.
        /// </summary>
        private void SelectDuty(string dutyId, string dutyName)
        {
            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment?.IsEnlisted == true)
            {
                enlistment.SetSelectedDuty(dutyId);
                
                var message = new TextObject("Duty changed to {DUTY}. Your new daily skill training has begun.");
                message.SetTextVariable("DUTY", dutyName);
                InformationManager.DisplayMessage(new InformationMessage(message.ToString()));
                
                GameMenu.SwitchToMenu("enlisted_duty_selection"); // Refresh menu
            }
        }
        
        /// <summary>
        /// Select a new profession and show confirmation.
        /// </summary>
        private void SelectProfession(string professionId, string professionName)
        {
            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment?.IsEnlisted == true)
            {
                enlistment.SetSelectedProfession(professionId);
                
                var message = new TextObject("Profession changed to {PROFESSION}. Your specialized training has begun.");
                message.SetTextVariable("PROFESSION", professionName);
                InformationManager.DisplayMessage(new InformationMessage(message.ToString()));
                
                GameMenu.SwitchToMenu("enlisted_duty_selection"); // Refresh menu
            }
        }

        /// <summary>
        /// Select profession with tier requirement check.
        /// </summary>
        private void SelectProfessionWithTierCheck(string professionId, string professionName)
        {
            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment?.IsEnlisted != true)
            {
                return;
            }

            // Check tier requirement
            if (enlistment.EnlistmentTier < 3)
            {
                var message = new TextObject("You must reach Tier 3 before selecting professions. Continue your service to unlock specialized roles.");
                InformationManager.DisplayMessage(new InformationMessage(message.ToString()));
                return;
            }

            // Tier 3+, allow selection
            SelectProfession(professionId, professionName);
        }
        
        /// <summary>
        /// Get display name for duty ID.
        /// </summary>
        private string GetDutyDisplayName(string dutyId)
        {
            return dutyId switch
            {
                "enlisted" => "Enlisted",
                "forager" => "Forager",
                "sentry" => "Sentry", 
                "messenger" => "Messenger",
                "pioneer" => "Pioneer",
                _ => "Unknown"
            };
        }

        /// <summary>
        /// Get detailed description for duty ID.
        /// </summary>
        private string GetDutyDescription(string dutyId)
        {
            return dutyId switch
            {
                "enlisted" => "You handle the everyday soldier work: picket shifts, camp chores, hauling, drill, short patrols. (+4 XP for non-formation skills)",
                "forager" => "Work nearby farms/hamlets to keep rations coming—barter, levy, or quietly procure supplies. (Skills: Charm, Roguery, Trade)",
                "sentry" => "Man the picket posts, patrol around the entrenchments and palisade, and call the alarm early. (Skills: Scouting, Tactics)",
                "messenger" => "Run dispatches between the command tent, outposts, and allied banners; get through checkpoints and return with written replies. (Skills: Scouting, Charm, Trade)",
                "pioneer" => "Cut timber and dig; drain around tents, shore up breastworks, lay corduroy over mud, and keep tools and wagons serviceable. (Skills: Engineering, Steward, Smithing)",
                _ => "Military service duties."
            };
        }
        
        /// <summary>
        /// Get display name for profession ID.
        /// </summary>
        private string GetProfessionDisplayName(string professionId)
        {
            return professionId switch
            {
                "none" => "None", // Default but invisible in menu
                "quarterhand" => "Quartermaster's Aide",
                "field_medic" => "Field Medic",
                "siegewright_aide" => "Siegewright's Aide",
                "drillmaster" => "Drillmaster",
                "saboteur" => "Saboteur",
                _ => "Unknown"
            };
        }

        /// <summary>
        /// Get detailed description for profession ID.
        /// </summary>
        private string GetProfessionDescription(string professionId)
        {
            return professionId switch
            {
                "none" => "No specialized profession assigned.",
                "quarterhand" => "Post billet lists, route carts around trenches, book barns/inns, and settle accounts. (Skills: Steward, Trade)",
                "field_medic" => "Run the aid tent by the stockade; clean and dress wounds, set bones, and keep salves stocked. (Skill: Medicine)",
                "siegewright_aide" => "Work the siege park; shape beams, lash ladders and gabions, and patch engines between bombardments. (Skills: Engineering, Smithing)",
                "drillmaster" => "Run morning drill on the parade ground; dress ranks, time volleys, rehearse signals, and sharpen maneuvers. (Skills: Leadership, Tactics)",
                "saboteur" => "Specialized reconnaissance and sabotage operations behind enemy lines. (Skills: Roguery, Engineering, Smithing)",
                _ => "Specialized military profession."
            };
        }

        #endregion

        #region Military Styling Helper Methods

        /// <summary>
        /// Get military symbol for formation type using ASCII characters.
        /// </summary>
        private string GetFormationSymbol(string formationName)
        {
            return formationName?.ToLower() switch
            {
                "infantry" => "[INF]",
                "archer" => "[ARC]", 
                "cavalry" => "[CAV]",
                "horsearcher" => "[H.ARC]",
                _ => "[MIL]"
            };
        }

        /// <summary>
        /// Create visual progress bar for XP progression using ASCII characters.
        /// </summary>
        private string GetProgressBar(int percent)
        {
            var totalBars = 20;
            var filledBars = (int)(percent / 100.0 * totalBars);
            var emptyBars = totalBars - filledBars;
            
            var progressBar = new StringBuilder();
            progressBar.Append("[<color=#90EE90>");
            for (int i = 0; i < filledBars; i++)
            {
                progressBar.Append("=");
            }
            progressBar.Append("</color><color=#696969>");
            for (int i = 0; i < emptyBars; i++)
            {
                progressBar.Append("-");
            }
            progressBar.Append("</color>]");
            
            return progressBar.ToString();
        }

        /// <summary>
        /// Get SAS-style assignment description.
        /// </summary>

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
            {
                return input;
            }
            
            return char.ToUpper(input[0]) + input.Substring(1).ToLower();
        }

        #endregion
    }
}
