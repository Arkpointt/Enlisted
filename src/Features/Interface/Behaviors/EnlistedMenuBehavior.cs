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
            
            // Visit Quartermaster (SAS option 1 - Enhanced with equipment variants)
            starter.AddGameMenuOption("enlisted_status", "enlisted_quartermaster",
                "Visit Quartermaster",
                IsQuartermasterAvailable,
                OnQuartermasterSelected,
                false, 1);

            // Battle Commands (SAS option 2)
            starter.AddGameMenuOption("enlisted_status", "enlisted_battle_commands",
                "Battle Commands: Player Formation Only",
                IsBattleCommandsAvailable,
                OnBattleCommandsSelected,
                false, 2);

            // Talk to... (SAS option 3)
            starter.AddGameMenuOption("enlisted_status", "enlisted_talk_to",
                "Talk to...",
                IsTalkToAvailable,
                OnTalkToSelected,
                false, 3);

            // Show reputation with factions (SAS option 4)
            starter.AddGameMenuOption("enlisted_status", "enlisted_reputation",
                "Show reputation with factions",
                IsReputationAvailable,
                OnReputationSelected,
                false, 4);

            // Ask commander for leave (SAS option 5)
            starter.AddGameMenuOption("enlisted_status", "enlisted_ask_leave",
                "Ask commander for leave",
                IsAskLeaveAvailable,
                OnAskLeaveSelected,
                false, 5);

            // Ask for a different assignment (SAS option 6)
            starter.AddGameMenuOption("enlisted_status", "enlisted_different_assignment",
                "Ask for a different assignment",
                IsDifferentAssignmentAvailable,
                OnDifferentAssignmentSelected,
                false, 6);

            // No "return to duties" option needed - player IS doing duties by being in this menu
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
        /// Refresh the enlisted status display with SAS-exact format.
        /// Uses SAS text variable approach for perfect replication.
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

                // Build comprehensive SAS-style status display with error handling
                var statusBuilder = new StringBuilder();
                
                try
                {
                    // EXACT SAS FORMAT from screenshot
                    statusBuilder.AppendLine($"Party Leader: {lord.Name?.ToString()}");
                    
                    // Party Objective (SAS format)
                    var objective = GetCurrentObjectiveDisplay(lord);
                    statusBuilder.AppendLine($"Party Objective: {objective}");
                    
                    // Enlistment Time (SAS format with season)
                    var enlistmentTime = GetSASEnlistmentTimeDisplay(enlistment);
                    statusBuilder.AppendLine($"Enlistment Time: {enlistmentTime}");
                    
                    // Enlistment Tier (SAS exact format)
                    statusBuilder.AppendLine($"Enlistment Tier: {enlistment.EnlistmentTier}");
                    
                    // Formation (SAS exact format)
                    string formationName = "Infantry"; // Default like SAS
                    try
                    {
                        if (duties?.IsInitialized == true)
                        {
                            var playerFormation = duties.GetPlayerFormationType();
                            formationName = playerFormation?.ToTitleCase() ?? "Infantry";
                        }
                    }
                    catch { /* Use default */ }
                    statusBuilder.AppendLine($"Formation: {formationName}");
                    
                    // Wage (SAS exact format with gold symbol)
                    var dailyWage = CalculateCurrentDailyWage();
                    statusBuilder.AppendLine($"Wage: {dailyWage} denars");
                    
                    // Current Experience (SAS exact format)
                    statusBuilder.AppendLine($"Current Experience: {enlistment.EnlistmentXP}");
                    
                    // Next Level Experience (SAS exact format)
                    if (enlistment.EnlistmentTier < 7)
                    {
                        var nextTierXP = GetNextTierXPRequirement(enlistment.EnlistmentTier + 1);
                        statusBuilder.AppendLine($"Next Level Experience: {nextTierXP}");
                    }
                    
                    // Assignment description (SAS exact format)
                    try
                    {
                        var assignmentDesc = GetSASAssignmentDescription();
                        statusBuilder.AppendLine($"When not fighting: {assignmentDesc}");
                    }
                    catch
                    {
                        statusBuilder.AppendLine("When not fighting: You are currently assigned to perform grunt work. Most tasks are unpleasant, tiring or involve menial labor. (Passive Daily Athletics XP)");
                    }
                }
                catch
                {
                // Fallback to simple display on any error
                statusBuilder.Clear();
                statusBuilder.AppendLine($"Lord: {lord?.Name?.ToString() ?? "Unknown"}");
                statusBuilder.AppendLine($"Rank: Tier {enlistment.EnlistmentTier}");
                statusBuilder.AppendLine($"Experience: {enlistment.EnlistmentXP} XP");
                }

                // SAS STEP 3: Use SAS text variable format (exact replication)
                var lordName = lord?.EncyclopediaLinkWithName?.ToString() ?? lord?.Name?.ToString() ?? "Unknown";
                var statusContent = statusBuilder.ToString();
                
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
            var requirements = new int[] { 0, 500, 1500, 3500, 7000, 12000, 18000 };
            return currentTier < 7 ? requirements[currentTier] : 18000;
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
        private string GetSASAssignmentDescription()
        {
            try
            {
                var duties = EnlistedDutiesBehavior.Instance;
                
                // For now, return the exact SAS grunt work description
                // This can be enhanced later to integrate with our duties system
                if (duties?.IsInitialized == true)
                {
                    var activeDuties = duties.GetActiveDutiesDisplay();
                    if (activeDuties != "None assigned")
                    {
                        // Enhanced description for when duties are active
                        return $"You are currently assigned to {activeDuties.ToLower()}. (Passive Daily XP from duties)";
                    }
                }
                
                // Default SAS-style grunt work description (matches screenshot exactly)
                return "You are currently assigned to perform grunt work. Most tasks are unpleasant, tiring or involve menial labor. (Passive Daily Athletics XP)";
            }
            catch
            {
                return "You are currently assigned to perform grunt work. Most tasks are unpleasant, tiring or involve menial labor. (Passive Daily Athletics XP)";
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

        private bool IsBattleCommandsAvailable(MenuCallbackArgs args)
        {
            return EnlistmentBehavior.Instance?.IsEnlisted == true;
        }

        private void OnBattleCommandsSelected(MenuCallbackArgs args)
        {
            // TODO: Implement battle commands toggle
            InformationManager.DisplayMessage(new InformationMessage(
                new TextObject("Battle commands system coming soon.").ToString()));
        }

        private bool IsTalkToAvailable(MenuCallbackArgs args)
        {
            return EnlistmentBehavior.Instance?.IsEnlisted == true;
        }

        private void OnTalkToSelected(MenuCallbackArgs args)
        {
            // TODO: Implement party member conversations
            InformationManager.DisplayMessage(new InformationMessage(
                new TextObject("Party conversation system coming soon.").ToString()));
        }

        private bool IsReputationAvailable(MenuCallbackArgs args)
        {
            return EnlistmentBehavior.Instance?.IsEnlisted == true;
        }

        private void OnReputationSelected(MenuCallbackArgs args)
        {
            // TODO: Implement reputation display
            InformationManager.DisplayMessage(new InformationMessage(
                new TextObject("Reputation system coming soon.").ToString()));
        }

        private bool IsAskLeaveAvailable(MenuCallbackArgs args)
        {
            return EnlistmentBehavior.Instance?.IsEnlisted == true;
        }

        private void OnAskLeaveSelected(MenuCallbackArgs args)
        {
            // TODO: Implement leave request dialog
            InformationManager.DisplayMessage(new InformationMessage(
                new TextObject("Leave request system coming soon.").ToString()));
        }

        private bool IsDifferentAssignmentAvailable(MenuCallbackArgs args)
        {
            return EnlistmentBehavior.Instance?.IsEnlisted == true;
        }

        private void OnDifferentAssignmentSelected(MenuCallbackArgs args)
        {
            // TODO: Implement assignment change dialog
            InformationManager.DisplayMessage(new InformationMessage(
                new TextObject("Assignment change system coming soon.").ToString()));
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

        #endregion

        // SAS-style single menu approach - all functionality integrated into main enlisted_status menu
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
    }
}
