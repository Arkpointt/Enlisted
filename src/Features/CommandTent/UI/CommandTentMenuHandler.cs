using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Enlisted.Features.CommandTent.Core;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Mod.Core.Logging;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using EnlistedConfig = Enlisted.Features.Assignments.Core.ConfigurationManager;

namespace Enlisted.Features.CommandTent.UI
{
    /// <summary>
    /// Handles the Command Tent menu system for service records display.
    /// Provides menus for viewing current posting, faction history, and lifetime statistics.
    /// Integrates with the existing enlisted status menu by adding a "Command Tent" option.
    /// </summary>
    public sealed class CommandTentMenuHandler : CampaignBehaviorBase
    {
        private const string LogCategory = "CommandTent";

        // Menu IDs
        private const string CommandTentMenuId = "command_tent";
        private const string ServiceRecordsMenuId = "command_tent_service_records";
        private const string CurrentPostingMenuId = "command_tent_current_posting";
        private const string FactionRecordsMenuId = "command_tent_faction_records";
        private const string FactionDetailMenuId = "command_tent_faction_detail";
        private const string LifetimeSummaryMenuId = "command_tent_lifetime_summary";

        // Retinue Menu IDs
        private const string RetinueMenuId = "command_tent_retinue";
        private const string RetinuePurchaseMenuId = "command_tent_retinue_purchase";
        private const string RetinueDismissMenuId = "command_tent_retinue_dismiss";
        private const string RetinueRequisitionMenuId = "command_tent_retinue_requisition";

        // Companion Assignment Menu IDs
        private const string CompanionAssignmentsMenuId = "command_tent_companions";

        // Track selected faction for detail view
        private string _selectedFactionKey;

        public static CommandTentMenuHandler Instance { get; private set; }

        public CommandTentMenuHandler()
        {
            Instance = this;
        }

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        }

        public override void SyncData(IDataStore dataStore)
        {
            // No persistent state needed for menu handler
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            try
            {
                AddCommandTentMenus(starter);
                ModLogger.Info(LogCategory, "Command Tent menus registered successfully");
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, $"Failed to register Command Tent menus: {ex.Message}");
            }
        }

        /// <summary>
        /// Registers all Command Tent menus and options with the game.
        /// </summary>
        private void AddCommandTentMenus(CampaignGameStarter starter)
        {
            // Add "Command Tent" option to enlisted_status menu
            AddCommandTentOptionToEnlistedMenu(starter);

            // Main Command Tent menu
            AddMainCommandTentMenu(starter);

            // Service Records submenu
            AddServiceRecordsMenu(starter);

            // Current Posting display
            AddCurrentPostingMenu(starter);

            // Faction Records list
            AddFactionRecordsMenu(starter);

            // Faction Detail view
            AddFactionDetailMenu(starter);

            // Lifetime Summary display
            AddLifetimeSummaryMenu(starter);

            // Retinue menus (Phase 4)
            AddRetinueMenu(starter);
            AddRetinuePurchaseMenu(starter);
            AddRetinueDismissMenu(starter);

            // Requisition menu (Phase 5)
            AddRetinueRequisitionMenu(starter);

            // Companion Assignments menu (Phase 8)
            AddCompanionAssignmentsMenu(starter);
        }

        #region Enlisted Status Integration

        /// <summary>
        /// Adds the Command Tent option to the main enlisted status menu.
        /// </summary>
        private void AddCommandTentOptionToEnlistedMenu(CampaignGameStarter starter)
        {
            try
            {
                starter.AddGameMenuOption(
                    "enlisted_status",
                    "enlisted_command_tent",
                    "{=ct_menu_enter}Enter the Command Tent",
                    IsCommandTentAvailable,
                    OnCommandTentSelected,
                    false,
                    4); // Position after Report for Duty

                ModLogger.Debug(LogCategory, "Added Command Tent option to enlisted_status menu");
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, $"Failed to add Command Tent option: {ex.Message}");
            }
        }

        /// <summary>
        /// Checks if the Command Tent option should be available (player must be enlisted).
        /// </summary>
        private bool IsCommandTentAvailable(MenuCallbackArgs args)
        {
            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment?.IsEnlisted != true)
            {
                return false;
            }

            args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
            return true;
        }

        /// <summary>
        /// Opens the Command Tent main menu.
        /// </summary>
        private void OnCommandTentSelected(MenuCallbackArgs args)
        {
            try
            {
                GameMenu.SwitchToMenu(CommandTentMenuId);
                ModLogger.Debug(LogCategory, "Player entered Command Tent");
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, $"Failed to switch to Command Tent menu: {ex.Message}");
            }
        }

        #endregion

        #region Main Command Tent Menu

        /// <summary>
        /// Creates the main Command Tent menu with introduction text.
        /// </summary>
        private void AddMainCommandTentMenu(CampaignGameStarter starter)
        {
            starter.AddGameMenu(
                CommandTentMenuId,
                "{=ct_menu_intro}The canvas flaps in the breeze. Maps and tallies cover a makeshift table. Your small corner of the army's camp.",
                OnCommandTentInit); // Default overlay is None

            // Service Records option
            starter.AddGameMenuOption(
                CommandTentMenuId,
                "ct_service_records",
                "{=ct_option_records}Review service records",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
                    return true;
                },
                _ => GameMenu.SwitchToMenu(ServiceRecordsMenuId),
                false,
                1);

            // Personal Retinue option (Tier 4+ only)
            starter.AddGameMenuOption(
                CommandTentMenuId,
                "ct_retinue",
                "{=ct_option_retinue}Muster personal retinue",
                IsRetinueAvailable,
                _ => GameMenu.SwitchToMenu(RetinueMenuId),
                false,
                2);

            // Companion Assignments option (Tier 4+ only)
            starter.AddGameMenuOption(
                CommandTentMenuId,
                "ct_companions",
                "{=ct_option_companions}Companion assignments",
                IsCompanionAssignmentsAvailable,
                _ => GameMenu.SwitchToMenu(CompanionAssignmentsMenuId),
                false,
                3);

            // Back to camp option
            starter.AddGameMenuOption(
                CommandTentMenuId,
                "ct_back",
                "{=ct_option_back}Return to camp",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Leave;
                    return true;
                },
                _ => GameMenu.SwitchToMenu("enlisted_status"),
                true,
                100);
        }

        /// <summary>
        /// Initializes the main Command Tent menu.
        /// </summary>
        private void OnCommandTentInit(MenuCallbackArgs args)
        {
            // args is required by the delegate signature but not used for initialization
            _ = args;
            ModLogger.Debug(LogCategory, "Command Tent menu initialized");
        }

        /// <summary>
        /// Checks if retinue option is available (Tier 4+ only).
        /// </summary>
        private bool IsRetinueAvailable(MenuCallbackArgs args)
        {
            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment?.IsEnlisted != true)
            {
                return false;
            }

            var tier = enlistment.EnlistmentTier;

            if (tier < 4)
            {
                args.IsEnabled = false;
                args.Tooltip = new TextObject("{=ct_warn_tier_locked}You must reach Tier 4 to command soldiers.");
            }

            args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
            return true;
        }

        #endregion

        #region Service Records Menu

        /// <summary>
        /// Creates the Service Records submenu with options for different record views.
        /// </summary>
        private void AddServiceRecordsMenu(CampaignGameStarter starter)
        {
            starter.AddGameMenu(
                ServiceRecordsMenuId,
                "{=ct_records_intro}Your service history and military records are catalogued here.",
                OnServiceRecordsInit); // Default overlay is None

            // Current Posting option
            starter.AddGameMenuOption(
                ServiceRecordsMenuId,
                "ct_current_posting",
                "{=ct_option_current}Current Posting",
                args =>
                {
                    var enlistment = EnlistmentBehavior.Instance;
                    args.IsEnabled = enlistment?.IsEnlisted == true;
                    if (!args.IsEnabled)
                    {
                        args.Tooltip = new TextObject("{=ct_not_enlisted}You are not currently enlisted.");
                    }
                    args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
                    return true;
                },
                _ => GameMenu.SwitchToMenu(CurrentPostingMenuId),
                false,
                1);

            // Faction Records option
            starter.AddGameMenuOption(
                ServiceRecordsMenuId,
                "ct_faction_records",
                "{=ct_option_faction}Faction Records",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
                    return true;
                },
                _ => GameMenu.SwitchToMenu(FactionRecordsMenuId),
                false,
                2);

            // Lifetime Summary option
            starter.AddGameMenuOption(
                ServiceRecordsMenuId,
                "ct_lifetime_summary",
                "{=ct_option_lifetime}Lifetime Summary",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
                    return true;
                },
                _ => GameMenu.SwitchToMenu(LifetimeSummaryMenuId),
                false,
                3);

            // Back option
            starter.AddGameMenuOption(
                ServiceRecordsMenuId,
                "ct_records_back",
                "{=ct_back_tent}Back to Command Tent",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Leave;
                    return true;
                },
                _ => GameMenu.SwitchToMenu(CommandTentMenuId),
                true,
                100);
        }

        private void OnServiceRecordsInit(MenuCallbackArgs args)
        {
            _ = args;
            ModLogger.Debug(LogCategory, "Service Records menu initialized");
        }

        #endregion

        #region Current Posting Menu

        /// <summary>
        /// Creates the Current Posting display showing current enlistment details.
        /// </summary>
        private void AddCurrentPostingMenu(CampaignGameStarter starter)
        {
            starter.AddGameMenu(
                CurrentPostingMenuId,
                "{CURRENT_POSTING_TEXT}",
                OnCurrentPostingInit); // Default overlay is None

            // Back option
            starter.AddGameMenuOption(
                CurrentPostingMenuId,
                "ct_posting_back",
                "{=ct_back_records}Back to Service Records",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Leave;
                    return true;
                },
                _ => GameMenu.SwitchToMenu(ServiceRecordsMenuId),
                true,
                100);
        }

        /// <summary>
        /// Initializes the Current Posting display with live data.
        /// </summary>
        private void OnCurrentPostingInit(MenuCallbackArgs args)
        {
            _ = args;
            try
            {
                var text = BuildCurrentPostingText();
                MBTextManager.SetTextVariable("CURRENT_POSTING_TEXT", text);
                ModLogger.Debug(LogCategory, "Current Posting display initialized");
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, $"Error initializing Current Posting: {ex.Message}");
                MBTextManager.SetTextVariable("CURRENT_POSTING_TEXT", "Error loading current posting data.");
            }
        }

        /// <summary>
        /// Builds the formatted text for current posting display.
        /// </summary>
        private string BuildCurrentPostingText()
        {
            var sb = new StringBuilder();
            var enlistment = EnlistmentBehavior.Instance;
            var recordManager = ServiceRecordManager.Instance;

            if (enlistment?.IsEnlisted != true)
            {
                return "You are not currently enlisted.";
            }

            var lord = enlistment.CurrentLord;
            var faction = lord?.Clan?.MapFaction;
            var tier = enlistment.EnlistmentTier;
            var daysServed = (int)enlistment.DaysServed;
            var daysRemaining = GetDaysRemaining(enlistment);

            // Header
            sb.AppendLine("═══════════════════════════════════════════");
            sb.AppendLine("         CURRENT SERVICE RECORD");
            sb.AppendLine("═══════════════════════════════════════════");
            sb.AppendLine();

            // Posting info
            var factionName = faction?.Name?.ToString() ?? "Unknown";
            var lordName = lord?.Name?.ToString() ?? "Unknown";
            var rankName = GetRankName(tier);

            sb.AppendLine($"  Posting: Army of {factionName}");
            sb.AppendLine($"  Lord: {lordName}");
            sb.AppendLine($"  Rank: {rankName} (Tier {tier})");
            sb.AppendLine();
            sb.AppendLine($"  Days Served: {daysServed}");
            sb.AppendLine($"  Contract Remaining: {daysRemaining}");
            sb.AppendLine();

            // Current term stats
            sb.AppendLine("───────────────────────────────────────────");
            sb.AppendLine("            THIS TERM");
            sb.AppendLine("───────────────────────────────────────────");
            sb.AppendLine();

            var termBattles = recordManager?.CurrentTermBattles ?? 0;
            var termKills = recordManager?.CurrentTermKills ?? 0;

            sb.AppendLine($"  Battles: {termBattles}");
            sb.AppendLine($"  Enemies Slain: {termKills}");
            sb.AppendLine();
            sb.AppendLine("═══════════════════════════════════════════");

            return sb.ToString();
        }

        /// <summary>
        /// Calculates days remaining in current term based on term type (first term vs renewal).
        /// </summary>
        private static string GetDaysRemaining(EnlistmentBehavior enlistment)
        {
            try
            {
                var remainingDays = 0;
                var faction = enlistment.CurrentLord?.MapFaction;

                if (faction != null)
                {
                    // Check if in renewal term first
                    var record = enlistment.GetFactionVeteranRecord(faction);
                    if (record is { IsInRenewalTerm: true } && record.CurrentTermEnd != CampaignTime.Zero)
                    {
                        remainingDays = (int)(record.CurrentTermEnd - CampaignTime.Now).ToDays;
                    }
                    else
                    {
                        // First term calculation
                        var retirementConfig = EnlistedConfig.LoadRetirementConfig();
                        var termEnd = enlistment.EnlistmentDate + CampaignTime.Days(retirementConfig.FirstTermDays);
                        remainingDays = (int)(termEnd - CampaignTime.Now).ToDays;
                    }
                }

                if (remainingDays <= 0)
                {
                    return "Term complete";
                }

                return $"{remainingDays} days";
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, $"Error calculating days remaining: {ex.Message}");
                return "Unknown";
            }
        }

        #endregion

        #region Faction Records Menu

        /// <summary>
        /// Creates the Faction Records list showing all factions served.
        /// </summary>
        private void AddFactionRecordsMenu(CampaignGameStarter starter)
        {
            starter.AddGameMenu(
                FactionRecordsMenuId,
                "{FACTION_RECORDS_TEXT}",
                OnFactionRecordsInit); // Default overlay is None

            // Dynamic faction options will be added during init
            // For now, add a static back option
            starter.AddGameMenuOption(
                FactionRecordsMenuId,
                "ct_factions_back",
                "{=ct_back_records}Back to Service Records",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Leave;
                    return true;
                },
                _ => GameMenu.SwitchToMenu(ServiceRecordsMenuId),
                true,
                100);
        }

        /// <summary>
        /// Initializes the Faction Records list.
        /// </summary>
        private void OnFactionRecordsInit(MenuCallbackArgs args)
        {
            _ = args;
            try
            {
                var text = BuildFactionRecordsListText();
                MBTextManager.SetTextVariable("FACTION_RECORDS_TEXT", text);
                ModLogger.Debug(LogCategory, "Faction Records list initialized");
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, $"Error initializing Faction Records: {ex.Message}");
                MBTextManager.SetTextVariable("FACTION_RECORDS_TEXT", "Error loading faction records.");
            }
        }

        /// <summary>
        /// Builds the faction records list text showing all factions served with summary stats.
        /// </summary>
        private string BuildFactionRecordsListText()
        {
            var sb = new StringBuilder();
            var recordManager = ServiceRecordManager.Instance;

            sb.AppendLine("═══════════════════════════════════════════");
            sb.AppendLine("         FACTION SERVICE RECORDS");
            sb.AppendLine("═══════════════════════════════════════════");
            sb.AppendLine();

            var records = recordManager?.GetAllRecords();
            if (records == null || records.Count == 0)
            {
                sb.AppendLine("  No faction service records found.");
                sb.AppendLine();
                sb.AppendLine("  Enlist with a lord to begin building");
                sb.AppendLine("  your military service history.");
            }
            else
            {
                sb.AppendLine("  Factions Served:");
                sb.AppendLine();

                foreach (var record in records.Values.OrderByDescending(r => r.TotalDaysServed))
                {
                    var factionType = FormatFactionType(record.FactionType);
                    sb.AppendLine($"  • {record.FactionDisplayName} ({factionType})");
                    sb.AppendLine($"    Terms: {record.TermsCompleted} | Days: {record.TotalDaysServed} | Kills: {record.TotalKills}");
                    sb.AppendLine();
                }
            }

            sb.AppendLine("═══════════════════════════════════════════");

            return sb.ToString();
        }

        /// <summary>
        /// Formats the faction type for display.
        /// </summary>
        private static string FormatFactionType(string factionType)
        {
            return factionType switch
            {
                "kingdom" => "Kingdom",
                "minor" => "Minor Faction",
                "merc" => "Mercenary Company",
                "clan" => "Noble Clan",
                "bandit" => "Bandit Clan",
                _ => "Unknown"
            };
        }

        #endregion

        #region Faction Detail Menu

        /// <summary>
        /// Creates the Faction Detail view for a specific faction.
        /// </summary>
        private void AddFactionDetailMenu(CampaignGameStarter starter)
        {
            starter.AddGameMenu(
                FactionDetailMenuId,
                "{FACTION_DETAIL_TEXT}",
                OnFactionDetailInit); // Default overlay is None

            // Back option
            starter.AddGameMenuOption(
                FactionDetailMenuId,
                "ct_detail_back",
                "{=ct_back_factions}Back to Faction Records",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Leave;
                    return true;
                },
                _ => GameMenu.SwitchToMenu(FactionRecordsMenuId),
                true,
                100);
        }

        /// <summary>
        /// Initializes the Faction Detail display.
        /// </summary>
        private void OnFactionDetailInit(MenuCallbackArgs args)
        {
            try
            {
                var text = BuildFactionDetailText(_selectedFactionKey);
                MBTextManager.SetTextVariable("FACTION_DETAIL_TEXT", text);
                ModLogger.Debug(LogCategory, $"Faction Detail initialized for: {_selectedFactionKey}");
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, $"Error initializing Faction Detail: {ex.Message}");
                MBTextManager.SetTextVariable("FACTION_DETAIL_TEXT", "Error loading faction detail.");
            }
        }

        /// <summary>
        /// Builds detailed faction record text.
        /// </summary>
        private string BuildFactionDetailText(string factionKey)
        {
            var sb = new StringBuilder();
            var recordManager = ServiceRecordManager.Instance;
            var record = recordManager?.GetRecord(factionKey);

            if (record == null)
            {
                return "No record found for this faction.";
            }

            var highestRank = GetRankName(record.HighestTier);

            sb.AppendLine("═══════════════════════════════════════════");
            sb.AppendLine($"       SERVICE RECORD: {record.FactionDisplayName.ToUpperInvariant()}");
            sb.AppendLine("═══════════════════════════════════════════");
            sb.AppendLine();
            sb.AppendLine($"  Total Enlistments: {record.Enlistments}");
            sb.AppendLine($"  Terms Completed: {record.TermsCompleted}");
            sb.AppendLine($"  Days Served: {record.TotalDaysServed}");
            sb.AppendLine($"  Highest Rank: {highestRank} (Tier {record.HighestTier})");
            sb.AppendLine();
            sb.AppendLine($"  Battles Fought: {record.BattlesFought}");
            sb.AppendLine($"  Enemies Slain: {record.TotalKills}");
            sb.AppendLine($"  Lords Served: {record.LordsServed}");
            sb.AppendLine();
            sb.AppendLine("═══════════════════════════════════════════");

            return sb.ToString();
        }

        /// <summary>
        /// Sets the selected faction for detail view and navigates to detail menu.
        /// </summary>
        public void ViewFactionDetail(string factionKey)
        {
            _selectedFactionKey = factionKey;
            GameMenu.SwitchToMenu(FactionDetailMenuId);
        }

        #endregion

        #region Lifetime Summary Menu

        /// <summary>
        /// Creates the Lifetime Summary display showing cross-faction totals.
        /// </summary>
        private void AddLifetimeSummaryMenu(CampaignGameStarter starter)
        {
            starter.AddGameMenu(
                LifetimeSummaryMenuId,
                "{LIFETIME_SUMMARY_TEXT}",
                OnLifetimeSummaryInit); // Default overlay is None

            // Back option
            starter.AddGameMenuOption(
                LifetimeSummaryMenuId,
                "ct_lifetime_back",
                "{=ct_back_records}Back to Service Records",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Leave;
                    return true;
                },
                _ => GameMenu.SwitchToMenu(ServiceRecordsMenuId),
                true,
                100);
        }

        /// <summary>
        /// Initializes the Lifetime Summary display.
        /// </summary>
        private void OnLifetimeSummaryInit(MenuCallbackArgs args)
        {
            _ = args;
            try
            {
                var text = BuildLifetimeSummaryText();
                MBTextManager.SetTextVariable("LIFETIME_SUMMARY_TEXT", text);
                ModLogger.Debug(LogCategory, "Lifetime Summary initialized");
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, $"Error initializing Lifetime Summary: {ex.Message}");
                MBTextManager.SetTextVariable("LIFETIME_SUMMARY_TEXT", "Error loading lifetime summary.");
            }
        }

        /// <summary>
        /// Builds the lifetime summary text showing cross-faction statistics.
        /// </summary>
        private string BuildLifetimeSummaryText()
        {
            var sb = new StringBuilder();
            var recordManager = ServiceRecordManager.Instance;
            var lifetime = recordManager?.LifetimeRecord;

            sb.AppendLine("═══════════════════════════════════════════");
            sb.AppendLine("         LIFETIME SERVICE SUMMARY");
            sb.AppendLine("═══════════════════════════════════════════");
            sb.AppendLine();

            if (lifetime == null)
            {
                sb.AppendLine("  No lifetime service records found.");
                sb.AppendLine();
                sb.AppendLine("  Your military career has not yet begun.");
            }
            else
            {
                // Calculate years and months from total days
                var (years, months) = lifetime.GetServiceDuration();
                var timeString = years > 0
                    ? $"{years} year{(years > 1 ? "s" : "")}, {months} month{(months != 1 ? "s" : "")}"
                    : $"{months} month{(months != 1 ? "s" : "")}";

                if (years == 0 && months == 0)
                {
                    timeString = $"{lifetime.TotalDaysServed} days";
                }

                sb.AppendLine($"  Years in Service: {timeString}");
                sb.AppendLine($"  Total Enlistments: {lifetime.TotalEnlistments}");
                sb.AppendLine($"  Terms Completed: {lifetime.TermsCompleted}");
                sb.AppendLine();

                // List factions served
                if (lifetime.FactionsServed != null && lifetime.FactionsServed.Count > 0)
                {
                    sb.AppendLine("  Factions Served:");

                    var allRecords = recordManager.GetAllRecords();
                    foreach (var factionId in lifetime.FactionsServed)
                    {
                        if (allRecords.TryGetValue(factionId, out var record))
                        {
                            var terms = record.TermsCompleted > 0
                                ? $"({record.TermsCompleted} term{(record.TermsCompleted > 1 ? "s" : "")})"
                                : "(in progress)";
                            sb.AppendLine($"    • {record.FactionDisplayName} {terms}");
                        }
                    }
                    sb.AppendLine();
                }

                sb.AppendLine($"  Total Battles Fought: {lifetime.TotalBattlesFought}");
                sb.AppendLine($"  Total Enemies Slain: {lifetime.LifetimeKills}");
            }

            sb.AppendLine();
            sb.AppendLine("═══════════════════════════════════════════");

            return sb.ToString();
        }

        #endregion

        #region Retinue Menu

        /// <summary>
        /// Creates the Personal Retinue submenu showing current muster status and options.
        /// </summary>
        private void AddRetinueMenu(CampaignGameStarter starter)
        {
            starter.AddGameMenu(
                RetinueMenuId,
                "{RETINUE_STATUS_TEXT}",
                OnRetinueMenuInit);

            // Current Muster option (view current soldiers if any)
            starter.AddGameMenuOption(
                RetinueMenuId,
                "ct_retinue_muster",
                "{=ct_retinue_current_muster}Current Muster",
                args =>
                {
                    var manager = RetinueManager.Instance;
                    var hasRetinue = manager?.State?.HasRetinue == true;
                    args.IsEnabled = hasRetinue;
                    if (!hasRetinue)
                    {
                        args.Tooltip = new TextObject("{=ct_retinue_no_soldiers}You have no soldiers mustered.");
                    }
                    args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
                    return true;
                },
                _ =>
                {
                    // Show current muster breakdown - just refresh the menu for now
                    GameMenu.SwitchToMenu(RetinueMenuId);
                },
                false,
                1);

            // Purchase Soldiers option
            starter.AddGameMenuOption(
                RetinueMenuId,
                "ct_retinue_purchase",
                "{=ct_retinue_purchase}Purchase Soldiers",
                args =>
                {
                    var manager = RetinueManager.Instance;
                    var enlistment = EnlistmentBehavior.Instance;

                    // Check if already at capacity
                    if (manager != null && enlistment != null)
                    {
                        var tierCapacity = RetinueManager.GetTierCapacity(enlistment.EnlistmentTier);
                        var currentSoldiers = manager.State?.TotalSoldiers ?? 0;
                        var partySpace = GetAvailablePartySpace();

                        if (currentSoldiers >= tierCapacity)
                        {
                            args.IsEnabled = false;
                            args.Tooltip = new TextObject("{=ct_warn_full_retinue}Your retinue is at full strength.");
                        }
                        else if (partySpace <= 0)
                        {
                            args.IsEnabled = false;
                            args.Tooltip = new TextObject("{=ct_warn_party_full}Your party is full. Dismiss troops or increase party size.");
                        }
                    }

                    args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
                    return true;
                },
                _ => GameMenu.SwitchToMenu(RetinuePurchaseMenuId),
                false,
                2);

            // Requisition Soldiers option (instant fill for gold, with cooldown)
            starter.AddGameMenuOption(
                RetinueMenuId,
                "ct_retinue_requisition",
                "{REQUISITION_OPTION_TEXT}",
                IsRequisitionAvailable,
                _ => GameMenu.SwitchToMenu(RetinueRequisitionMenuId),
                false,
                3);

            // Dismiss Soldiers option
            starter.AddGameMenuOption(
                RetinueMenuId,
                "ct_retinue_dismiss",
                "{=ct_retinue_dismiss}Dismiss Soldiers",
                args =>
                {
                    var manager = RetinueManager.Instance;
                    var hasRetinue = manager?.State?.HasRetinue == true;
                    args.IsEnabled = hasRetinue;
                    if (!hasRetinue)
                    {
                        args.Tooltip = new TextObject("{=ct_retinue_no_soldiers}You have no soldiers to dismiss.");
                    }
                    args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
                    return true;
                },
                _ => GameMenu.SwitchToMenu(RetinueDismissMenuId),
                false,
                4);

            // Back option
            starter.AddGameMenuOption(
                RetinueMenuId,
                "ct_retinue_back",
                "{=ct_back_tent}Back to Command Tent",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Leave;
                    return true;
                },
                _ => GameMenu.SwitchToMenu(CommandTentMenuId),
                true,
                100);
        }

        /// <summary>
        /// Initializes the Retinue menu with current status.
        /// </summary>
        private void OnRetinueMenuInit(MenuCallbackArgs args)
        {
            _ = args;
            try
            {
                var text = BuildRetinueStatusText();
                MBTextManager.SetTextVariable("RETINUE_STATUS_TEXT", text);

                // Set requisition option text with cost display
                SetRequisitionOptionText();

                ModLogger.Debug(LogCategory, "Retinue menu initialized");
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, $"Error initializing Retinue menu: {ex.Message}");
                MBTextManager.SetTextVariable("RETINUE_STATUS_TEXT", "Error loading retinue status.");
            }
        }

        /// <summary>
        /// Sets the requisition menu option text with cost and cooldown info.
        /// </summary>
        private static void SetRequisitionOptionText()
        {
            var manager = RetinueManager.Instance;
            if (manager == null)
            {
                MBTextManager.SetTextVariable("REQUISITION_OPTION_TEXT", "Requisition Men");
                return;
            }

            var cost = manager.CalculateRequisitionCost();
            var missing = manager.GetMissingSoldierCount();
            var cooldownDays = manager.GetRequisitionCooldownDays();

            string optionText;
            if (cooldownDays > 0)
            {
                optionText = $"Requisition Men ({cooldownDays}d cooldown)";
            }
            else if (missing <= 0)
            {
                optionText = "Requisition Men (at capacity)";
            }
            else
            {
                optionText = $"Requisition Men ({cost} denars)";
            }

            MBTextManager.SetTextVariable("REQUISITION_OPTION_TEXT", optionText);
        }

        /// <summary>
        /// Builds the retinue status text showing current muster and capacity.
        /// </summary>
        private string BuildRetinueStatusText()
        {
            var sb = new StringBuilder();
            var enlistment = EnlistmentBehavior.Instance;
            var manager = RetinueManager.Instance;

            if (enlistment?.IsEnlisted != true)
            {
                return "You are not currently enlisted.";
            }

            var tier = enlistment.EnlistmentTier;
            var rankName = GetRankName(tier);
            var unitName = GetUnitNameForTier(tier);
            var tierCapacity = RetinueManager.GetTierCapacity(tier);
            var currentSoldiers = manager?.State?.TotalSoldiers ?? 0;
            var selectedType = manager?.State?.SelectedTypeId;
            var partySpace = GetAvailablePartySpace();
            var dailyUpkeep = currentSoldiers * 2; // 2 gold per soldier per day

            sb.AppendLine("═══════════════════════════════════════════");
            sb.AppendLine("         PERSONAL RETINUE");
            sb.AppendLine("═══════════════════════════════════════════");
            sb.AppendLine();

            // Show rank and capacity
            sb.AppendLine($"  Rank: {rankName} (Tier {tier})");
            sb.AppendLine($"  Command Limit: {unitName}");
            sb.AppendLine();

            // Show current muster
            sb.AppendLine("───────────────────────────────────────────");
            sb.AppendLine("            CURRENT MUSTER");
            sb.AppendLine("───────────────────────────────────────────");
            sb.AppendLine();

            if (string.IsNullOrEmpty(selectedType))
            {
                sb.AppendLine("  No soldiers mustered.");
                sb.AppendLine("  Select a soldier type to begin.");
            }
            else
            {
                var typeName = GetSoldierTypeName(selectedType, enlistment.CurrentLord?.Culture);
                sb.AppendLine($"  Type: {typeName}");
                sb.AppendLine($"  Soldiers: {currentSoldiers}/{tierCapacity}");
                sb.AppendLine($"  Daily Upkeep: {dailyUpkeep} denars");
            }

            sb.AppendLine();

            // Show party size status
            sb.AppendLine("───────────────────────────────────────────");
            sb.AppendLine("            PARTY CAPACITY");
            sb.AppendLine("───────────────────────────────────────────");
            sb.AppendLine();

            var partyLimit = PartyBase.MainParty?.PartySizeLimit ?? 0;
            var currentMembers = PartyBase.MainParty?.NumberOfAllMembers ?? 0;
            sb.AppendLine($"  Party Limit: {partyLimit}");
            sb.AppendLine($"  Current Members: {currentMembers}");
            sb.AppendLine($"  Available Space: {partySpace}");

            if (partySpace < tierCapacity - currentSoldiers)
            {
                sb.AppendLine();
                sb.AppendLine("  ⚠ Party size limits your retinue.");
            }

            sb.AppendLine();
            sb.AppendLine("═══════════════════════════════════════════");

            return sb.ToString();
        }

        #endregion

        #region Retinue Purchase Menu

        /// <summary>
        /// Creates the soldier type selection and purchase menu.
        /// </summary>
        private void AddRetinuePurchaseMenu(CampaignGameStarter starter)
        {
            starter.AddGameMenu(
                RetinuePurchaseMenuId,
                "{RETINUE_PURCHASE_TEXT}",
                OnRetinuePurchaseInit);

            // Infantry option - Men-at-Arms
            starter.AddGameMenuOption(
                RetinuePurchaseMenuId,
                "ct_purchase_infantry",
                "{INFANTRY_OPTION_TEXT}",
                args => IsSoldierTypeAvailable(args, "infantry"),
                _ => OnSoldierTypePurchase("infantry"),
                false,
                1);

            // Archers option - Bowmen
            starter.AddGameMenuOption(
                RetinuePurchaseMenuId,
                "ct_purchase_archers",
                "{ARCHERS_OPTION_TEXT}",
                args => IsSoldierTypeAvailable(args, "archers"),
                _ => OnSoldierTypePurchase("archers"),
                false,
                2);

            // Cavalry option - Mounted Lancers
            starter.AddGameMenuOption(
                RetinuePurchaseMenuId,
                "ct_purchase_cavalry",
                "{CAVALRY_OPTION_TEXT}",
                args => IsSoldierTypeAvailable(args, "cavalry"),
                _ => OnSoldierTypePurchase("cavalry"),
                false,
                3);

            // Horse Archers option - Mounted Bowmen (faction restricted)
            starter.AddGameMenuOption(
                RetinuePurchaseMenuId,
                "ct_purchase_horse_archers",
                "{HORSE_ARCHERS_OPTION_TEXT}",
                args => IsSoldierTypeAvailable(args, "horse_archers"),
                _ => OnSoldierTypePurchase("horse_archers"),
                false,
                4);

            // Back option
            starter.AddGameMenuOption(
                RetinuePurchaseMenuId,
                "ct_purchase_back",
                "{=ct_back_retinue}Back to Retinue",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Leave;
                    return true;
                },
                _ => GameMenu.SwitchToMenu(RetinueMenuId),
                true,
                100);
        }

        /// <summary>
        /// Initializes the purchase menu. Shows gold, soldier options, and naval dismount note for mounted types.
        /// </summary>
        private void OnRetinuePurchaseInit(MenuCallbackArgs args)
        {
            _ = args;
            try
            {
                var enlistment = EnlistmentBehavior.Instance;
                var culture = enlistment?.CurrentLord?.Culture;
                var tier = enlistment?.EnlistmentTier ?? 4;

                // Build header text with naval note for mounted types
                var sb = new StringBuilder();
                sb.AppendLine("═══════════════════════════════════════════");
                sb.AppendLine("         PURCHASE SOLDIERS");
                sb.AppendLine("═══════════════════════════════════════════");
                sb.AppendLine();
                sb.AppendLine("  Select the type of soldiers to muster.");
                sb.AppendLine($"  Your Gold: {Hero.MainHero?.Gold ?? 0} denars");
                sb.AppendLine();
                sb.AppendLine("  * Mounted troops fight on foot in naval battles.");
                sb.AppendLine();
                sb.AppendLine("═══════════════════════════════════════════");

                MBTextManager.SetTextVariable("RETINUE_PURCHASE_TEXT", sb.ToString());

                // Set option text variables with costs
                SetSoldierTypeOptionText("infantry", culture, tier);
                SetSoldierTypeOptionText("archers", culture, tier);
                SetSoldierTypeOptionText("cavalry", culture, tier);
                SetSoldierTypeOptionText("horse_archers", culture, tier);

                ModLogger.Debug(LogCategory, "Retinue purchase menu initialized");
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, $"Error initializing purchase menu: {ex.Message}");
                MBTextManager.SetTextVariable("RETINUE_PURCHASE_TEXT", "Error loading purchase options.");
            }
        }

        /// <summary>
        /// Sets the menu option text for a soldier type. Mounted types marked with * for naval warning.
        /// </summary>
        private void SetSoldierTypeOptionText(string typeId, CultureObject culture, int playerTier)
        {
            var typeName = GetSoldierTypeName(typeId, culture);
            var cost = CalculateRecruitmentCost(typeId, culture, playerTier);
            var variableName = typeId.ToUpperInvariant() + "_OPTION_TEXT";

            var optionText = $"{typeName} ({cost} denars)";

            if (IsMountedType(typeId))
            {
                optionText += " *";
            }

            MBTextManager.SetTextVariable(variableName, optionText);
        }

        /// <summary>
        /// Returns true for cavalry/horse_archers which fight dismounted in naval battles.
        /// </summary>
        private static bool IsMountedType(string typeId)
        {
            return typeId is "cavalry" or "horse_archers";
        }

        /// <summary>
        /// Validates soldier type availability. Checks faction restrictions, affordability, and shows naval tooltip for mounted types.
        /// </summary>
        private bool IsSoldierTypeAvailable(MenuCallbackArgs args, string typeId)
        {
            var enlistment = EnlistmentBehavior.Instance;
            var culture = enlistment?.CurrentLord?.Culture;

            if (culture == null)
            {
                args.IsEnabled = false;
                return true;
            }

            // Faction restriction check (horse archers not available for some factions)
            if (!RetinueManager.IsSoldierTypeAvailable(typeId, culture))
            {
                args.IsEnabled = false;
                var factionName = culture.Name?.ToString() ?? "This faction";
                var tooltip = new TextObject("{=ct_warn_faction_unavailable}{FACTION_NAME} does not field mounted archers.");
                tooltip.SetTextVariable("FACTION_NAME", factionName);
                args.Tooltip = tooltip;
                return true;
            }

            // Affordability check
            var cost = CalculateRecruitmentCost(typeId, culture, enlistment.EnlistmentTier);
            var playerGold = Hero.MainHero?.Gold ?? 0;

            if (playerGold < cost)
            {
                args.IsEnabled = false;
                var tooltip = new TextObject("{=ct_warn_cannot_afford}You cannot afford this ({COST} denars required).");
                tooltip.SetTextVariable("COST", cost);
                args.Tooltip = tooltip;
                return true;
            }

            // Type change requires dismissing current retinue
            var manager = RetinueManager.Instance;
            if (manager?.State?.HasRetinue == true && manager.State.SelectedTypeId != typeId)
            {
                args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
            }
            else
            {
                args.optionLeaveType = GameMenuOption.LeaveType.Recruit;
            }

            // Naval dismount info tooltip for mounted types
            if (IsMountedType(typeId))
            {
                var tooltipId = typeId == "cavalry" 
                    ? "{=ct_naval_cavalry_tooltip}Cavalry will dismount and fight as infantry during naval engagements. Their horses cannot be brought aboard ships."
                    : "{=ct_naval_horse_archer_tooltip}Horse archers will dismount and fight as foot archers during naval engagements. Their horses cannot be brought aboard ships.";
                args.Tooltip = new TextObject(tooltipId);
            }

            return true;
        }

        /// <summary>
        /// Handles soldier type purchase selection.
        /// </summary>
        private void OnSoldierTypePurchase(string typeId)
        {
            var manager = RetinueManager.Instance;
            var enlistment = EnlistmentBehavior.Instance;

            if (manager == null || enlistment == null)
            {
                ModLogger.Warn(LogCategory, "Cannot purchase: manager or enlistment null");
                return;
            }

            var culture = enlistment.CurrentLord?.Culture;
            if (culture == null)
            {
                ModLogger.Warn(LogCategory, "Cannot purchase: no culture");
                return;
            }

            // Check if changing type with existing retinue
            if (manager.State?.HasRetinue == true && manager.State.SelectedTypeId != typeId)
            {
                // Show confirmation to dismiss existing retinue
                ShowTypeChangeConfirmation(typeId);
                return;
            }

            // Calculate how many soldiers to purchase
            var tierCapacity = RetinueManager.GetTierCapacity(enlistment.EnlistmentTier);
            var currentSoldiers = manager.State?.TotalSoldiers ?? 0;
            var partySpace = GetAvailablePartySpace();
            var canPurchase = Math.Min(tierCapacity - currentSoldiers, partySpace);

            if (canPurchase <= 0)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    "Cannot purchase soldiers: at capacity or party full.", Colors.Red));
                return;
            }

            // Calculate total cost
            var costPerSoldier = CalculateRecruitmentCost(typeId, culture, enlistment.EnlistmentTier);
            var playerGold = Hero.MainHero?.Gold ?? 0;
            var maxAffordable = playerGold / Math.Max(1, costPerSoldier);
            var purchaseCount = Math.Min(canPurchase, maxAffordable);

            if (purchaseCount <= 0)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    "You cannot afford any soldiers.", Colors.Red));
                return;
            }

            // Show purchase confirmation
            ShowPurchaseConfirmation(typeId, purchaseCount, costPerSoldier * purchaseCount);
        }

        /// <summary>
        /// Shows purchase confirmation dialog.
        /// </summary>
        private void ShowPurchaseConfirmation(string typeId, int count, int totalCost)
        {
            var enlistment = EnlistmentBehavior.Instance;
            var culture = enlistment?.CurrentLord?.Culture;
            var typeName = GetSoldierTypeName(typeId, culture);

            var title = new TextObject("{=ct_purchase_confirm_title}Confirm Purchase");
            var message = new TextObject("{=ct_purchase_confirm_msg}Purchase {COUNT} {TYPE_NAME} for {COST} denars?\n\nDaily upkeep: {UPKEEP} denars");
            message.SetTextVariable("COUNT", count);
            message.SetTextVariable("TYPE_NAME", typeName);
            message.SetTextVariable("COST", totalCost);
            message.SetTextVariable("UPKEEP", count * 2);

            InformationManager.ShowInquiry(
                new InquiryData(
                    title.ToString(),
                    message.ToString(),
                    true,
                    true,
                    new TextObject("{=ct_confirm_yes}Purchase").ToString(),
                    new TextObject("{=ct_confirm_no}Cancel").ToString(),
                    () => ExecutePurchase(typeId, count, totalCost),
                    () => GameMenu.SwitchToMenu(RetinuePurchaseMenuId)),
                true);
        }

        /// <summary>
        /// Executes the actual purchase of soldiers.
        /// </summary>
        private void ExecutePurchase(string typeId, int count, int totalCost)
        {
            var manager = RetinueManager.Instance;

            if (manager == null)
            {
                ModLogger.Error(LogCategory, "ExecutePurchase: manager null");
                return;
            }

            // Deduct gold
            Hero.MainHero?.ChangeHeroGold(-totalCost);

            // Add soldiers
            if (manager.TryAddSoldiers(count, typeId, out var actuallyAdded, out var message))
            {
                ModLogger.Info(LogCategory, $"Purchased {actuallyAdded} {typeId} soldiers for {totalCost} gold");

                var successMsg = new TextObject("{=ct_purchase_success}{COUNT} soldiers have been mustered to your retinue.");
                successMsg.SetTextVariable("COUNT", actuallyAdded);
                InformationManager.DisplayMessage(new InformationMessage(successMsg.ToString(), Colors.Green));
            }
            else
            {
                // Refund gold if failed
                Hero.MainHero?.ChangeHeroGold(totalCost);
                ModLogger.Warn(LogCategory, $"Purchase failed: {message}");
                InformationManager.DisplayMessage(new InformationMessage(message, Colors.Red));
            }

            // Return to retinue menu
            GameMenu.SwitchToMenu(RetinueMenuId);
        }

        /// <summary>
        /// Shows confirmation dialog when changing soldier type with existing retinue.
        /// </summary>
        private void ShowTypeChangeConfirmation(string newTypeId)
        {
            var manager = RetinueManager.Instance;
            var currentCount = manager?.State?.TotalSoldiers ?? 0;

            var title = new TextObject("{=ct_type_change_title}Change Soldier Type");
            var message = new TextObject("{=ct_type_change_msg}You currently have {COUNT} soldiers. Changing type will dismiss them. Continue?");
            message.SetTextVariable("COUNT", currentCount);

            InformationManager.ShowInquiry(
                new InquiryData(
                    title.ToString(),
                    message.ToString(),
                    true,
                    true,
                    new TextObject("{=ct_confirm_yes}Dismiss & Continue").ToString(),
                    new TextObject("{=ct_confirm_no}Cancel").ToString(),
                    () =>
                    {
                        // Dismiss current retinue
                        manager?.ClearRetinueTroops("type_change");
                        ModLogger.Info(LogCategory, "Dismissed retinue for type change");

                        var dismissMsg = new TextObject("{=ct_type_change_dismiss}Your current soldiers have been dismissed to make way for new recruits.");
                        InformationManager.DisplayMessage(new InformationMessage(dismissMsg.ToString()));

                        // Now proceed with new type purchase
                        OnSoldierTypePurchase(newTypeId);
                    },
                    () => GameMenu.SwitchToMenu(RetinuePurchaseMenuId)),
                true);
        }

        #endregion

        #region Retinue Dismiss Menu

        /// <summary>
        /// Creates the dismiss soldiers confirmation menu.
        /// </summary>
        private void AddRetinueDismissMenu(CampaignGameStarter starter)
        {
            starter.AddGameMenu(
                RetinueDismissMenuId,
                "{RETINUE_DISMISS_TEXT}",
                OnRetinueDismissInit);

            // Confirm dismiss option
            starter.AddGameMenuOption(
                RetinueDismissMenuId,
                "ct_dismiss_confirm",
                "{=ct_dismiss_confirm_btn}Dismiss all soldiers",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Surrender;
                    return true;
                },
                _ => ExecuteDismiss(),
                false,
                1);

            // Cancel option
            starter.AddGameMenuOption(
                RetinueDismissMenuId,
                "ct_dismiss_cancel",
                "{=ct_dismiss_cancel}Keep my soldiers",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Leave;
                    return true;
                },
                _ => GameMenu.SwitchToMenu(RetinueMenuId),
                true,
                100);
        }

        /// <summary>
        /// Initializes the dismiss menu.
        /// </summary>
        private void OnRetinueDismissInit(MenuCallbackArgs args)
        {
            _ = args;
            try
            {
                var manager = RetinueManager.Instance;
                var currentCount = manager?.State?.TotalSoldiers ?? 0;

                var sb = new StringBuilder();
                sb.AppendLine("═══════════════════════════════════════════");
                sb.AppendLine("         DISMISS SOLDIERS");
                sb.AppendLine("═══════════════════════════════════════════");
                sb.AppendLine();
                sb.AppendLine("  Are you certain?");
                sb.AppendLine();
                sb.AppendLine($"  Your {currentCount} soldiers will return");
                sb.AppendLine("  to the army ranks.");
                sb.AppendLine();
                sb.AppendLine("  This action cannot be undone.");
                sb.AppendLine();
                sb.AppendLine("═══════════════════════════════════════════");

                MBTextManager.SetTextVariable("RETINUE_DISMISS_TEXT", sb.ToString());
                ModLogger.Debug(LogCategory, "Dismiss menu initialized");
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, $"Error initializing dismiss menu: {ex.Message}");
                MBTextManager.SetTextVariable("RETINUE_DISMISS_TEXT", "Error loading dismiss options.");
            }
        }

        /// <summary>
        /// Executes the dismissal of all retinue soldiers.
        /// </summary>
        private void ExecuteDismiss()
        {
            var manager = RetinueManager.Instance;
            var dismissedCount = manager?.State?.TotalSoldiers ?? 0;

            manager?.ClearRetinueTroops("player_dismiss");

            ModLogger.Info(LogCategory, $"Player dismissed {dismissedCount} soldiers");

            var msg = new TextObject("{=ct_dismiss_success}Your soldiers have been dismissed.");
            InformationManager.DisplayMessage(new InformationMessage(msg.ToString()));

            GameMenu.SwitchToMenu(RetinueMenuId);
        }

        #endregion

        #region Requisition Menu

        /// <summary>
        /// Checks if the requisition menu option should be available.
        /// Shows disabled state with tooltip when on cooldown or at capacity.
        /// </summary>
        private static bool IsRequisitionAvailable(MenuCallbackArgs args)
        {
            var manager = RetinueManager.Instance;

            // Must have a retinue type selected
            if (manager?.State?.HasTypeSelected != true)
            {
                args.IsEnabled = false;
                args.Tooltip = new TextObject("{=ct_requisition_no_type}You must select a soldier type first.");
                args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
                return true;
            }

            // Check cooldown
            if (!manager.IsRequisitionAvailable())
            {
                args.IsEnabled = false;
                var days = manager.GetRequisitionCooldownDays();
                var tooltip = new TextObject("{=ct_requisition_cooldown}Requisition on cooldown: {DAYS} days remaining.");
                tooltip.SetTextVariable("DAYS", days);
                args.Tooltip = tooltip;
                args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
                return true;
            }

            // Check if already at capacity
            var missing = manager.GetMissingSoldierCount();
            if (missing <= 0)
            {
                args.IsEnabled = false;
                args.Tooltip = new TextObject("{=ct_warn_full_retinue}Your retinue is at full strength.");
                args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
                return true;
            }

            // Check affordability
            var cost = manager.CalculateRequisitionCost();
            var playerGold = Hero.MainHero?.Gold ?? 0;
            if (playerGold < cost)
            {
                args.IsEnabled = false;
                var tooltip = new TextObject("{=ct_warn_cannot_afford}You cannot afford this ({COST} denars required).");
                tooltip.SetTextVariable("COST", cost);
                args.Tooltip = tooltip;
                args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
                return true;
            }

            args.optionLeaveType = GameMenuOption.LeaveType.Trade;
            return true;
        }

        /// <summary>
        /// Creates the Requisition confirmation menu with cost breakdown.
        /// </summary>
        private void AddRetinueRequisitionMenu(CampaignGameStarter starter)
        {
            starter.AddGameMenu(
                RetinueRequisitionMenuId,
                "{REQUISITION_MENU_TEXT}",
                OnRetinueRequisitionInit);

            // Confirm requisition option
            starter.AddGameMenuOption(
                RetinueRequisitionMenuId,
                "ct_requisition_confirm",
                "{REQUISITION_CONFIRM_TEXT}",
                args =>
                {
                    var manager = RetinueManager.Instance;
                    if (manager == null || !manager.CanRequisition(out _))
                    {
                        args.IsEnabled = false;
                        return true;
                    }

                    args.optionLeaveType = GameMenuOption.LeaveType.Trade;
                    return true;
                },
                _ => ExecuteRequisition(),
                false,
                1);

            // Cancel option
            starter.AddGameMenuOption(
                RetinueRequisitionMenuId,
                "ct_requisition_cancel",
                "{=ct_requisition_cancel}Return without requisition",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Leave;
                    return true;
                },
                _ => GameMenu.SwitchToMenu(RetinueMenuId),
                true,
                100);
        }

        /// <summary>
        /// Initializes the requisition menu with cost breakdown.
        /// </summary>
        private static void OnRetinueRequisitionInit(MenuCallbackArgs args)
        {
            _ = args;
            try
            {
                var manager = RetinueManager.Instance;
                var missing = manager?.GetMissingSoldierCount() ?? 0;
                var cost = manager?.CalculateRequisitionCost() ?? 0;
                var perSoldier = missing > 0 ? cost / missing : 0;
                var playerGold = Hero.MainHero?.Gold ?? 0;
                var cooldownDays = manager?.GetRequisitionCooldownDays() ?? 0;

                var sb = new StringBuilder();
                sb.AppendLine("═══════════════════════════════════════════");
                sb.AppendLine("         REQUISITION MEN");
                sb.AppendLine("═══════════════════════════════════════════");
                sb.AppendLine();
                sb.AppendLine("  A word to the right quartermaster, a few");
                sb.AppendLine("  coins changing hands, and fresh soldiers");
                sb.AppendLine("  report for duty.");
                sb.AppendLine();
                sb.AppendLine("───────────────────────────────────────────");
                sb.AppendLine();
                sb.AppendLine($"  Missing Soldiers: {missing}");
                sb.AppendLine($"  Cost per Soldier: {perSoldier} denars");
                sb.AppendLine($"  Total Cost: {cost} denars");
                sb.AppendLine();
                sb.AppendLine($"  Your Gold: {playerGold} denars");
                sb.AppendLine();

                if (cooldownDays > 0)
                {
                    sb.AppendLine($"  ⏱ Cooldown: {cooldownDays} days remaining");
                }
                else
                {
                    sb.AppendLine("  ✓ Requisition Available NOW");
                }

                sb.AppendLine();
                sb.AppendLine("  After requisition: 14 day cooldown");
                sb.AppendLine();
                sb.AppendLine("═══════════════════════════════════════════");

                MBTextManager.SetTextVariable("REQUISITION_MENU_TEXT", sb.ToString());

                // Set confirm button text
                var confirmText = $"Requisition {missing} soldiers for {cost} denars";
                MBTextManager.SetTextVariable("REQUISITION_CONFIRM_TEXT", confirmText);

                ModLogger.Debug(LogCategory, "Requisition menu initialized");
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, $"Error initializing requisition menu: {ex.Message}");
                MBTextManager.SetTextVariable("REQUISITION_MENU_TEXT", "Error loading requisition details.");
                MBTextManager.SetTextVariable("REQUISITION_CONFIRM_TEXT", "Requisition");
            }
        }

        /// <summary>
        /// Executes the instant requisition of soldiers.
        /// </summary>
        private void ExecuteRequisition()
        {
            var manager = RetinueManager.Instance;
            if (manager == null)
            {
                ModLogger.Error(LogCategory, "ExecuteRequisition: manager null");
                GameMenu.SwitchToMenu(RetinueMenuId);
                return;
            }

            if (manager.TryRequisition(out var message))
            {
                var successMsg = new TextObject("{=ct_requisition_success}{MESSAGE}");
                successMsg.SetTextVariable("MESSAGE", message);
                InformationManager.DisplayMessage(new InformationMessage(successMsg.ToString(), Colors.Green));

                ModLogger.Info(LogCategory, $"Requisition complete: {message}");
            }
            else
            {
                InformationManager.DisplayMessage(new InformationMessage(message, Colors.Red));
                ModLogger.Warn(LogCategory, $"Requisition failed: {message}");
            }

            GameMenu.SwitchToMenu(RetinueMenuId);
        }

        #endregion

        #region Companion Assignments Menu

        /// <summary>
        /// Checks if companion assignments option is available (Tier 4+ and has companions).
        /// </summary>
        private static bool IsCompanionAssignmentsAvailable(MenuCallbackArgs args)
        {
            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment?.IsEnlisted != true)
            {
                return false;
            }

            var tier = enlistment.EnlistmentTier;

            if (tier < 4)
            {
                args.IsEnabled = false;
                args.Tooltip = new TextObject("{=ct_companions_tier_locked}You must reach Tier 4 to manage companion assignments.");
            }
            else
            {
                var manager = CompanionAssignmentManager.Instance;
                var companions = manager?.GetAssignableCompanions() ?? new List<Hero>();
                if (companions.Count == 0)
                {
                    args.IsEnabled = false;
                    args.Tooltip = new TextObject("{=ct_companions_none}No companions in your command.");
                }
            }

            args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
            return true;
        }

        /// <summary>
        /// Creates the Companion Assignments submenu showing each companion with toggle options.
        /// </summary>
        private void AddCompanionAssignmentsMenu(CampaignGameStarter starter)
        {
            starter.AddGameMenu(
                CompanionAssignmentsMenuId,
                "{COMPANION_ASSIGNMENTS_TEXT}",
                OnCompanionAssignmentsInit);

            // Dynamic companion options are added via menu init text since GameMenu doesn't support 
            // truly dynamic options. Instead we show the list in the description and use toggle buttons.
            // Add 8 companion slots (should cover most parties)
            for (var i = 0; i < 8; i++)
            {
                var index = i;
                starter.AddGameMenuOption(
                    CompanionAssignmentsMenuId,
                    $"ct_companion_{i}",
                    $"{{COMPANION_{i}_TEXT}}",
                    args => IsCompanionSlotVisible(args, index),
                    _ => OnCompanionToggle(index),
                    false,
                    i + 1);
            }

            // Back option
            starter.AddGameMenuOption(
                CompanionAssignmentsMenuId,
                "ct_companions_back",
                "{=ct_back_tent}Back to Command Tent",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Leave;
                    return true;
                },
                _ => GameMenu.SwitchToMenu(CommandTentMenuId),
                true,
                100);
        }

        // Cache companions for the current menu session to maintain consistent indices
        private List<Hero> _cachedCompanions;

        /// <summary>
        /// Initializes the companion assignments menu with current companion list.
        /// </summary>
        private void OnCompanionAssignmentsInit(MenuCallbackArgs args)
        {
            _ = args;
            try
            {
                var manager = CompanionAssignmentManager.Instance;
                _cachedCompanions = manager?.GetAssignableCompanions() ?? new List<Hero>();

                var sb = new StringBuilder();
                sb.AppendLine("═══════════════════════════════════════════");
                sb.AppendLine("         COMPANION ASSIGNMENTS");
                sb.AppendLine("═══════════════════════════════════════════");
                sb.AppendLine();
                sb.AppendLine("  Companions set to 'Stay Back' will not");
                sb.AppendLine("  spawn in battle. They remain safe in your");
                sb.AppendLine("  roster, immune to death, wounds, or capture.");
                sb.AppendLine();

                if (_cachedCompanions.Count == 0 || manager == null)
                {
                    sb.AppendLine("  No companions in your command.");
                }
                else
                {
                    var fightCount = manager.GetFightingCompanionCount();
                    var stayBackCount = manager.GetStayBackCompanionCount();
                    sb.AppendLine($"  Fighting: {fightCount}  |  Staying Back: {stayBackCount}");
                }

                sb.AppendLine();
                sb.AppendLine("═══════════════════════════════════════════");

                MBTextManager.SetTextVariable("COMPANION_ASSIGNMENTS_TEXT", sb.ToString());

                // Set text variables for each companion slot
                for (var i = 0; i < 8; i++)
                {
                    if (i < _cachedCompanions.Count && manager != null)
                    {
                        var companion = _cachedCompanions[i];
                        var shouldFight = manager.ShouldCompanionFight(companion);
                        var statusIcon = shouldFight ? "⚔" : "🏕";
                        var statusText = shouldFight ? "Will fight" : "Stay back";
                        MBTextManager.SetTextVariable($"COMPANION_{i}_TEXT", $"{statusIcon} {companion.Name} - {statusText}");
                    }
                    else
                    {
                        MBTextManager.SetTextVariable($"COMPANION_{i}_TEXT", string.Empty);
                    }
                }

                ModLogger.Debug(LogCategory, $"Companion assignments menu initialized with {_cachedCompanions.Count} companions");
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, $"Error initializing companion assignments: {ex.Message}");
                MBTextManager.SetTextVariable("COMPANION_ASSIGNMENTS_TEXT", "Error loading companion data.");
            }
        }

        /// <summary>
        /// Checks if a companion slot should be visible based on cached companion list.
        /// </summary>
        private bool IsCompanionSlotVisible(MenuCallbackArgs args, int index)
        {
            if (_cachedCompanions == null || index >= _cachedCompanions.Count)
            {
                return false;
            }

            args.optionLeaveType = GameMenuOption.LeaveType.Continue;
            return true;
        }

        /// <summary>
        /// Toggles a companion's battle participation when their menu option is selected.
        /// </summary>
        private void OnCompanionToggle(int index)
        {
            if (_cachedCompanions == null || index >= _cachedCompanions.Count)
            {
                return;
            }

            var companion = _cachedCompanions[index];
            var manager = CompanionAssignmentManager.Instance;
            if (manager == null || companion == null)
            {
                return;
            }

            manager.ToggleCompanionParticipation(companion);
            var newStatus = manager.ShouldCompanionFight(companion) ? "Fight" : "Stay Back";
            
            var message = new TextObject("{=ct_companion_toggled}{COMPANION_NAME} set to: {STATUS}");
            message.SetTextVariable("COMPANION_NAME", companion.Name);
            message.SetTextVariable("STATUS", newStatus);
            InformationManager.DisplayMessage(new InformationMessage(message.ToString()));

            ModLogger.Info(LogCategory, $"Toggled {companion.Name} to {newStatus}");

            // Refresh the menu
            GameMenu.SwitchToMenu(CompanionAssignmentsMenuId);
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Gets the rank name for a given tier.
        /// </summary>
        private static string GetRankName(int tier)
        {
            return tier switch
            {
                1 => "Levy",
                2 => "Footman",
                3 => "Serjeant",
                4 => "Man-at-Arms",
                5 => "Banner Serjeant",
                6 => "Household Guard",
                _ => $"Tier {tier}"
            };
        }

        /// <summary>
        /// Gets the unit name for a tier (lance/squad/retinue).
        /// </summary>
        private static string GetUnitNameForTier(int tier)
        {
            return tier switch
            {
                >= 6 => "Retinue of Twenty",
                5 => "Squad of Ten",
                4 => "Lance of Five",
                _ => "None"
            };
        }

        /// <summary>
        /// Gets the display name for a soldier type, with faction-specific overrides.
        /// </summary>
        private static string GetSoldierTypeName(string typeId, CultureObject culture)
        {
            var cultureId = culture?.StringId?.ToLowerInvariant() ?? "";

            return typeId switch
            {
                "infantry" => "Men-at-Arms",
                "archers" => cultureId switch
                {
                    "vlandia" => "Crossbowmen",
                    "battania" => "Fian Archers",
                    _ => "Bowmen"
                },
                "cavalry" => "Mounted Lancers",
                "horse_archers" => cultureId switch
                {
                    "khuzait" => "Steppe Riders",
                    "aserai" => "Mameluke Cavalry",
                    _ => "Mounted Bowmen"
                },
                _ => typeId
            };
        }

        /// <summary>
        /// Gets available party space for new soldiers.
        /// </summary>
        private static int GetAvailablePartySpace()
        {
            var party = PartyBase.MainParty;
            if (party == null)
            {
                return 0;
            }

            return Math.Max(0, party.PartySizeLimit - party.NumberOfAllMembers);
        }

        /// <summary>
        /// Calculates the recruitment cost for one soldier of the given type.
        /// Uses the native PartyWageModel.GetTroopRecruitmentCost formula.
        /// </summary>
        private static int CalculateRecruitmentCost(string typeId, CultureObject culture, int playerTier)
        {
            // Get a representative troop to calculate cost
            var troops = RetinueManager.GetAvailableTroops(typeId, culture, playerTier);
            if (troops == null || troops.Count == 0)
            {
                // Fallback cost based on type
                return typeId switch
                {
                    "cavalry" or "horse_archers" => 200, // Mounted troops cost more
                    _ => 100
                };
            }

            // Calculate average cost across available troops
            var totalCost = 0;
            var count = 0;

            foreach (var troop in troops)
            {
                var cost = Campaign.Current?.Models?.PartyWageModel?
                    .GetTroopRecruitmentCost(troop, Hero.MainHero)
                    .RoundedResultNumber ?? 100;
                totalCost += cost;
                count++;
            }

            return count > 0 ? totalCost / count : 100;
        }

        #endregion
    }
}

