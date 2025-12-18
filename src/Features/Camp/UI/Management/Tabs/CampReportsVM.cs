using System;
using System.Collections.Generic;
using System.Linq;
using Enlisted.Features.Camp.UI.Bulletin;
using Enlisted.Features.Camp.UI.Management;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Features.Interface.Behaviors;
using Enlisted.Features.Lances.Events;
using Enlisted.Features.Lances.Events.Decisions;
using Enlisted.Features.Schedule.Behaviors;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.LogEntries;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Library;

namespace Enlisted.Features.Camp.UI.Management.Tabs
{
    /// <summary>
    /// Reports tab ViewModel - Organized news feed with three categories:
    /// Lance News, Company News, and Kingdom News.
    /// </summary>
    public class CampReportsVM : ViewModel
    {
        private bool _show;
        
        // Left list header (like Diplomacy "At War" / "At Peace")
        private string _reportsText;
        private string _numReportsText;

        // Section headers and counts (used as category titles)
        private string _lanceNewsText;
        private string _companyNewsText;
        private string _kingdomNewsText;
        private string _numLanceNewsText;
        private string _numCompanyNewsText;
        private string _numKingdomNewsText;

        // Phase 5.5: decision-driven report categories
        private string _opportunitiesText;
        private string _recentOutcomesText;
        private string _numOpportunitiesText;
        private string _numRecentOutcomesText;
        
        // News item lists
        private MBBindingList<ReportItemVM> _lanceNews;
        private MBBindingList<ReportItemVM> _companyNews;
        private MBBindingList<ReportItemVM> _kingdomNews;
        private MBBindingList<ReportItemVM> _generalOrders;
        private MBBindingList<ReportItemVM> _opportunities;
        private MBBindingList<ReportItemVM> _recentOutcomes;
        
        // General Orders header
        private string _generalOrdersText;
        private string _numGeneralOrdersText;

        // Category buttons (the things you actually click, like Nord/Sturgia)
        private MBBindingList<ReportCategoryItemVM> _reportCategories;
        private ReportCategoryType _selectedCategoryType;
        
        // Selected item
        private ReportItemVM _currentSelectedReport;
        private bool _isReportSelected;
        
        // Direct properties for selected report display (avoids nested DataSource issues)
        private string _selectedReportTitle;
        private string _selectedReportCategory;
        private string _selectedReportDescription;
        
        // Category selection (like Diplomacy item selection)
        private bool _isCategorySelected;
        private string _selectedCategoryTitle;
        private MBBindingList<ReportItemVM> _selectedCategoryNews;
        
        public CampReportsVM()
        {
            LanceNews = new MBBindingList<ReportItemVM>();
            CompanyNews = new MBBindingList<ReportItemVM>();
            KingdomNews = new MBBindingList<ReportItemVM>();
            GeneralOrders = new MBBindingList<ReportItemVM>();
            Opportunities = new MBBindingList<ReportItemVM>();
            RecentOutcomes = new MBBindingList<ReportItemVM>();
            ReportCategories = new MBBindingList<ReportCategoryItemVM>();
            SelectedCategoryNews = new MBBindingList<ReportItemVM>();
            _selectedCategoryType = ReportCategoryType.GeneralOrders;
            RefreshValues();
        }
        
        public override void RefreshValues()
        {
            base.RefreshValues();

            ReportsText = "Reports";
            GeneralOrdersText = "General Orders";
            OpportunitiesText = "Opportunities";
            RecentOutcomesText = "Recent Outcomes";
            LanceNewsText = "Lance Reports";
            CompanyNewsText = "Company Reports";
            KingdomNewsText = "Kingdom Reports";
            
            RefreshAllNews();
        }
        
        /// <summary>
        /// Refresh all news categories from current game state.
        /// Uses native game log pattern for proper display.
        /// </summary>
        private void RefreshAllNews()
        {
            LanceNews.Clear();
            CompanyNews.Clear();
            KingdomNews.Clear();
            GeneralOrders.Clear();
            Opportunities.Clear();
            RecentOutcomes.Clear();
            
            var enlistment = EnlistmentBehavior.Instance;
            var schedule = ScheduleBehavior.Instance;
            var newsBehavior = EnlistedNewsBehavior.Instance;
            
            // Get faction banner for reports
            Banner factionBanner = enlistment?.EnlistedLord?.ClanBanner;
            
            // ----- LANCE NEWS -----
            // Schedule info
            if (schedule?.CurrentSchedule != null)
            {
                var cycleDay = schedule.CurrentSchedule.CycleDay;
                var timeText = CampaignTime.Now.ToString();
                
                var scheduleTitle = $"Day {cycleDay}/12 - {schedule.CurrentSchedule.LordOrders ?? "Follow your duties."}";
                LanceNews.Add(new ReportItemVM(
                    scheduleTitle,
                    timeText,
                    "Schedule",
                    OnReportSelect,
                    factionBanner,
                    detailText: schedule.CurrentSchedule.LordOrders ?? "Follow your duties."
                ));
                
                // Current time block activity
                var currentBlock = schedule.GetCurrentActiveBlock();
                if (currentBlock != null)
                {
                    var status = currentBlock.IsCompleted ? "Completed" : "In Progress";
                    LanceNews.Add(new ReportItemVM(
                        $"{currentBlock.BlockType}: {currentBlock.Title} ({status})",
                        CampaignTime.Now.ToString(),
                        "Activity",
                        OnReportSelect,
                        factionBanner,
                        detailText: currentBlock.Description
                    ));
                }
                
                // Lance needs warnings
                if (schedule.LanceNeeds != null)
                {
                    AddLanceNeedsWarnings(schedule.LanceNeeds, factionBanner);
                }
            }
            
            // Lance roster info
            if (enlistment?.IsEnlisted == true)
            {
                var lanceName = enlistment.CurrentLanceName ?? "Your Lance";
                var rankText = Ranks.RankHelper.GetCurrentRank(enlistment);
                
                LanceNews.Add(new ReportItemVM(
                    $"Assigned to {lanceName} as {rankText}",
                    CampaignTime.Now.ToString(),
                    "Assignment",
                    OnReportSelect,
                    factionBanner,
                    detailText: $"You are assigned to {lanceName} as {rankText}."
                ));
            }
            
            // Add placeholder if no lance news
            if (LanceNews.Count == 0)
            {
                LanceNews.Add(new ReportItemVM(
                    "No lance reports at this time.",
                    "",
                    "Info",
                    OnReportSelect,
                    detailText: "No lance reports at this time."
                ));
            }
            
            // ----- COMPANY NEWS -----
            if (enlistment?.IsEnlisted == true && enlistment.EnlistedLord != null)
            {
                var lord = enlistment.EnlistedLord;
                var party = lord.PartyBelongedTo;
                var lordBanner = lord.ClanBanner;
                
                // Lord status
                string lordStatus;
                if (party?.CurrentSettlement != null)
                {
                    lordStatus = $"Lord {lord.Name} stationed at {party.CurrentSettlement.Name}";
                }
                else if (party?.TargetSettlement != null)
                {
                    lordStatus = $"Lord {lord.Name} marching to {party.TargetSettlement.Name}";
                }
                else if (party?.Army != null)
                {
                    lordStatus = $"Lord {lord.Name} joined army under {party.Army.LeaderParty?.LeaderHero?.Name}";
                }
                else
                {
                    lordStatus = $"Lord {lord.Name} on patrol";
                }
                
                CompanyNews.Add(new ReportItemVM(
                    lordStatus,
                    CampaignTime.Now.ToString(),
                    "Command",
                    OnReportSelect,
                    lordBanner,
                    detailText: lordStatus
                ));
                
                // Army info
                if (party?.Army != null)
                {
                    var armySize = 0;
                    foreach (var p in party.Army.Parties)
                    {
                        if (p != null)
                            armySize += (int)p.Party.NumberOfAllMembers;
                    }
                    
                    CompanyNews.Add(new ReportItemVM(
                        $"Army assembled: {party.Army.Parties.Count} lords, {armySize} troops",
                        CampaignTime.Now.ToString(),
                        "Army",
                        OnReportSelect,
                        party.Army.LeaderParty?.LeaderHero?.ClanBanner ?? lordBanner,
                        detailText: $"An army has been assembled with {party.Army.Parties.Count} lords and {armySize} troops."
                    ));
                }
                
                // Party strength
                if (party != null)
                {
                    CompanyNews.Add(new ReportItemVM(
                        $"Party strength: {(int)party.Party.NumberOfAllMembers} troops",
                        CampaignTime.Now.ToString(),
                        "Strength",
                        OnReportSelect,
                        lordBanner,
                        detailText: $"Current party strength: {(int)party.Party.NumberOfAllMembers} troops."
                    ));
                }
            }
            
            // Add placeholder if no company news
            if (CompanyNews.Count == 0)
            {
                CompanyNews.Add(new ReportItemVM(
                    "No company reports at this time.",
                    "",
                    "Info",
                    OnReportSelect,
                    detailText: "No company reports at this time."
                ));
            }
            
            // ----- KINGDOM NEWS -----
            // Pull directly from native game log system - this is the key part
            PopulateKingdomNewsFromGameLogs(enlistment);
            
            // Add placeholder if no kingdom news
            if (KingdomNews.Count == 0)
            {
                KingdomNews.Add(new ReportItemVM(
                    "No kingdom reports at this time.",
                    "",
                    "Info",
                    OnReportSelect,
                    detailText: "No kingdom reports at this time."
                ));
            }
            
            // ----- GENERAL ORDERS -----
            // Generate RP-style narrative summaries of recent events
            GenerateGeneralOrders(enlistment, factionBanner);

            // ----- DECISIONS (Phase 5.5) -----
            var opportunitiesCount = PopulateDecisionOpportunities(enlistment, factionBanner);
            var recentOutcomesCount = PopulateRecentDecisionOutcomes(enlistment, factionBanner);
            
            // Update counts
            NumGeneralOrdersText = $"({GeneralOrders.Count})";
            NumOpportunitiesText = $"({opportunitiesCount})";
            NumRecentOutcomesText = $"({recentOutcomesCount})";
            NumLanceNewsText = $"({LanceNews.Count})";
            NumCompanyNewsText = $"({CompanyNews.Count})";
            NumKingdomNewsText = $"({KingdomNews.Count})";

            RefreshReportCategories(enlistment, factionBanner);
            EnsureValidCategorySelection();
            
            Mod.Core.Logging.ModLogger.Debug("CampReportsVM", 
                $"Refreshed news: GeneralOrders={GeneralOrders.Count}, Lance={LanceNews.Count}, Company={CompanyNews.Count}, Kingdom={KingdomNews.Count}");
        }

        private void RefreshReportCategories(EnlistmentBehavior enlistment, Banner fallbackBanner)
        {
            ReportCategories.Clear();

            Banner lordBanner = enlistment?.EnlistedLord?.ClanBanner ?? fallbackBanner;
            Banner kingdomBanner = enlistment?.EnlistedLord?.Clan?.Kingdom?.Banner ?? lordBanner;

            // These are the *clickable buttons* under the Reports toggle.
            ReportCategories.Add(new ReportCategoryItemVM(
                ReportCategoryType.GeneralOrders,
                $"{GeneralOrdersText} {NumGeneralOrdersText}",
                GeneralOrders.Count,
                SelectCategory,
                lordBanner
            ));
            ReportCategories.Add(new ReportCategoryItemVM(
                ReportCategoryType.Opportunities,
                $"{OpportunitiesText} {NumOpportunitiesText}",
                Opportunities?.Count ?? 0,
                SelectCategory,
                lordBanner
            ));
            ReportCategories.Add(new ReportCategoryItemVM(
                ReportCategoryType.RecentOutcomes,
                $"{RecentOutcomesText} {NumRecentOutcomesText}",
                RecentOutcomes?.Count ?? 0,
                SelectCategory,
                lordBanner
            ));
            ReportCategories.Add(new ReportCategoryItemVM(
                ReportCategoryType.LanceReports,
                $"{LanceNewsText} {NumLanceNewsText}",
                LanceNews.Count,
                SelectCategory,
                lordBanner
            ));
            ReportCategories.Add(new ReportCategoryItemVM(
                ReportCategoryType.CompanyReports,
                $"{CompanyNewsText} {NumCompanyNewsText}",
                CompanyNews.Count,
                SelectCategory,
                lordBanner
            ));
            ReportCategories.Add(new ReportCategoryItemVM(
                ReportCategoryType.KingdomReports,
                $"{KingdomNewsText} {NumKingdomNewsText}",
                KingdomNews.Count,
                SelectCategory,
                kingdomBanner
            ));

            NumReportsText = $"({ReportCategories.Count})";
        }

        private void EnsureValidCategorySelection()
        {
            // Keep selection stable across refreshes. If nothing selected yet, default to General Orders.
            if (ReportCategories == null || ReportCategories.Count == 0)
            {
                IsCategorySelected = false;
                SelectedCategoryTitle = "";
                SelectedCategoryNews = new MBBindingList<ReportItemVM>();
                return;
            }

            SelectCategory(_selectedCategoryType);
        }

        private void SelectCategory(ReportCategoryType categoryType)
        {
            _selectedCategoryType = categoryType;

            foreach (var c in ReportCategories)
                c.IsSelected = c.CategoryType == categoryType;

            IsCategorySelected = true;

            switch (categoryType)
            {
                case ReportCategoryType.GeneralOrders:
                    SelectedCategoryTitle = GeneralOrdersText;
                    SelectedCategoryNews = GeneralOrders;
                    break;
                case ReportCategoryType.Opportunities:
                    SelectedCategoryTitle = OpportunitiesText;
                    SelectedCategoryNews = Opportunities;
                    break;
                case ReportCategoryType.RecentOutcomes:
                    SelectedCategoryTitle = RecentOutcomesText;
                    SelectedCategoryNews = RecentOutcomes;
                    break;
                case ReportCategoryType.LanceReports:
                    SelectedCategoryTitle = LanceNewsText;
                    SelectedCategoryNews = LanceNews;
                    break;
                case ReportCategoryType.CompanyReports:
                    SelectedCategoryTitle = CompanyNewsText;
                    SelectedCategoryNews = CompanyNews;
                    break;
                case ReportCategoryType.KingdomReports:
                    SelectedCategoryTitle = KingdomNewsText;
                    SelectedCategoryNews = KingdomNews;
                    break;
                default:
                    SelectedCategoryTitle = ReportsText;
                    SelectedCategoryNews = new MBBindingList<ReportItemVM>();
                    break;
            }

            // In native screens, selecting an item always populates the right panel.
            // We'll auto-select the newest/top item in the feed for a responsive UX.
            // For Opportunities (Phase 5.5) we DO NOT auto-select, because selecting an opportunity routes out to the Decisions menu.
            if (categoryType == ReportCategoryType.Opportunities)
            {
                OnReportSelect(null);
                return;
            }

            if (SelectedCategoryNews != null && SelectedCategoryNews.Count > 0)
            {
                OnReportSelect(SelectedCategoryNews[0]);
            }
            else
            {
                OnReportSelect(null);
            }
        }

        private int PopulateDecisionOpportunities(EnlistmentBehavior enlistment, Banner factionBanner)
        {
            Opportunities.Clear();

            if (enlistment?.IsEnlisted != true)
            {
                Opportunities.Add(new ReportItemVM(
                    "No opportunities while not enlisted.",
                    "",
                    "Opportunity",
                    OnReportSelect,
                    factionBanner,
                    detailText: "Opportunities appear when you are enlisted and there are player-initiated decisions available."
                ));

                return 0;
            }

            var decisionBehavior = DecisionEventBehavior.Instance;
            var available = decisionBehavior?.GetAvailablePlayerDecisions() ?? new List<LanceLifeEventDefinition>();
            var availableCount = available.Count;

            if (availableCount == 0)
            {
                Opportunities.Add(new ReportItemVM(
                    "No matters await your decision today.",
                    "",
                    "Opportunity",
                    OnReportSelect,
                    factionBanner,
                    detailText: "No player-initiated decisions are currently available."
                ));

                return 0;
            }

            // Keep it short and high-signal.
            var maxToShow = Math.Min(availableCount, 8);
            for (var i = 0; i < maxToShow; i++)
            {
                var evt = available[i];
                if (evt == null)
                {
                    continue;
                }

                var title = LanceLifeEventText.Resolve(evt.TitleId, evt.TitleFallback, evt.Id, enlistment);
                var setup = LanceLifeEventText.Resolve(evt.SetupId, evt.SetupFallback, string.Empty, enlistment);
                var stakes = BuildDecisionStakesHint(evt);

                var detail = string.IsNullOrWhiteSpace(setup) ? title : setup;
                if (!string.IsNullOrWhiteSpace(stakes))
                {
                    detail = $"{detail}\n\n{stakes}";
                }

                Opportunities.Add(new ReportItemVM(
                    logText: ShortenSingleLine(title, 110),
                    logTimeText: "Available",
                    category: "Opportunity",
                    onSelect: OnReportSelect,
                    banner: factionBanner,
                    detailText: detail,
                    decisionEventId: evt.Id
                ));
            }

            return availableCount;
        }

        private int PopulateRecentDecisionOutcomes(EnlistmentBehavior enlistment, Banner factionBanner)
        {
            RecentOutcomes.Clear();

            var decisionBehavior = DecisionEventBehavior.Instance;
            var log = decisionBehavior?.State?.OutcomeLog;
            log?.EnsureInitialized();

            var items = log?.Items;
            if (items == null || items.Length == 0)
            {
                RecentOutcomes.Add(new ReportItemVM(
                    "No recent outcomes.",
                    "",
                    "Outcome",
                    OnReportSelect,
                    factionBanner,
                    detailText: "Resolved decision outcomes will appear here."
                ));

                return 0;
            }

            var records = items
                .Where(r => r != null &&
                            r.DayNumber >= 0 &&
                            !string.IsNullOrWhiteSpace(r.EventId) &&
                            !string.IsNullOrWhiteSpace(r.ResultText))
                .ToList();

            records.Sort((a, b) => b.DayNumber.CompareTo(a.DayNumber));

            if (records.Count == 0)
            {
                RecentOutcomes.Add(new ReportItemVM(
                    "No recent outcomes.",
                    "",
                    "Outcome",
                    OnReportSelect,
                    factionBanner,
                    detailText: "Resolved decision outcomes will appear here."
                ));

                return 0;
            }

            var catalog = LanceLifeEventRuntime.GetCatalog();

            var maxToShow = Math.Min(records.Count, 10);
            for (var i = 0; i < maxToShow; i++)
            {
                var rec = records[i];
                if (rec == null)
                {
                    continue;
                }

                var evt = catalog?.Events?.FirstOrDefault(e => string.Equals(e?.Id, rec.EventId, StringComparison.OrdinalIgnoreCase));

                var title = evt != null
                    ? LanceLifeEventText.Resolve(evt.TitleId, evt.TitleFallback, evt.Id, enlistment)
                    : rec.EventId;

                var optText = rec.OptionId ?? string.Empty;
                if (evt?.Options != null && !string.IsNullOrWhiteSpace(rec.OptionId))
                {
                    var opt = evt.Options.FirstOrDefault(o => string.Equals(o?.Id, rec.OptionId, StringComparison.OrdinalIgnoreCase));
                    if (opt != null)
                    {
                        optText = LanceLifeEventText.Resolve(opt.TextId, opt.TextFallback, opt.Id, enlistment);
                    }
                }

                optText = ShortenSingleLine(optText, 70);

                var heading = string.IsNullOrWhiteSpace(optText)
                    ? title
                    : $"{title} — {optText}";

                heading = ShortenSingleLine(heading, 120);

                var timeText = rec.DayNumber >= 0 ? CampaignTime.Days(rec.DayNumber).ToString() : string.Empty;

                var detail = ShortenWithNewlines(rec.ResultText ?? string.Empty, 500);
                if (!string.IsNullOrWhiteSpace(optText))
                {
                    detail = $"Chosen: {optText}\n\n{detail}";
                }

                RecentOutcomes.Add(new ReportItemVM(
                    logText: heading,
                    logTimeText: timeText,
                    category: "Outcome",
                    onSelect: OnReportSelect,
                    banner: factionBanner,
                    detailText: detail
                ));
            }

            return records.Count;
        }

        private static string BuildDecisionStakesHint(LanceLifeEventDefinition evt)
        {
            if (evt?.Options == null || evt.Options.Count == 0)
            {
                return string.Empty;
            }

            var tags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var opt in evt.Options)
            {
                if (opt == null)
                {
                    continue;
                }

                if (opt.Costs?.Fatigue > 0)
                {
                    tags.Add("fatigue");
                }
                if (opt.Costs?.Gold > 0 || (opt.Rewards?.Gold ?? 0) > 0)
                {
                    tags.Add("coin");
                }

                var heat = (opt.Costs?.Heat ?? 0) + (opt.Effects?.Heat ?? 0);
                if (heat != 0)
                {
                    tags.Add("heat");
                }

                var disc = (opt.Costs?.Discipline ?? 0) + (opt.Effects?.Discipline ?? 0);
                if (disc != 0)
                {
                    tags.Add("discipline");
                }

                if ((opt.Effects?.LanceReputation ?? 0) != 0)
                {
                    tags.Add("reputation");
                }

                if ((opt.Rewards?.FatigueRelief ?? 0) > 0)
                {
                    tags.Add("rest");
                }
            }

            if (tags.Count == 0)
            {
                return string.Empty;
            }

            return $"Stakes: {string.Join(", ", tags)}.";
        }

        private static string ShortenSingleLine(string text, int maxChars)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            var t = text.Replace("\r", " ").Replace("\n", " ").Trim();
            if (t.Length <= maxChars)
            {
                return t;
            }

            return t.Substring(0, maxChars).TrimEnd() + "...";
        }

        private static string ShortenWithNewlines(string text, int maxChars)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            var t = text.Trim();
            if (t.Length <= maxChars)
            {
                return t;
            }

            return t.Substring(0, maxChars).TrimEnd() + "...";
        }

        private void OpenDecisionsMenuFromReports(string decisionEventId)
        {
            if (string.IsNullOrWhiteSpace(decisionEventId))
            {
                return;
            }

            try
            {
                // Close the Camp Management overlay first to avoid input restrictions, then switch to the Decisions menu.
                Enlisted.Mod.Entry.NextFrameDispatcher.RunNextFrame(() =>
                {
                    try
                    {
                        CampManagementScreen.Close();
                    }
                    catch
                    {
                        // Ignore
                    }

                    Enlisted.Mod.Entry.NextFrameDispatcher.RunNextFrame(() =>
                    {
                        try
                        {
                            GameMenu.SwitchToMenu("enlisted_decisions");
                        }
                        catch
                        {
                            // Ignore
                        }
                    });
                });
            }
            catch
            {
                // Ignore
            }
        }
        
        /// <summary>
        /// Generate RP-style narrative summaries for General Orders.
        /// Creates interesting military dispatches based on recent events.
        /// </summary>
        private void GenerateGeneralOrders(EnlistmentBehavior enlistment, Banner factionBanner)
        {
            if (enlistment?.IsEnlisted != true) return;
            
            var lord = enlistment.EnlistedLord;
            var kingdom = lord?.Clan?.Kingdom;
            var party = lord?.PartyBelongedTo;
            var schedule = ScheduleBehavior.Instance;
            
            // Calculate which 3-day period we're in
            int dayOfCampaign = (int)CampaignTime.Now.ToDays;
            int periodNumber = dayOfCampaign / 3;
            string periodName = GetPeriodName(periodNumber);
            
            // Generate the main dispatch header
            string dispatchDate = CampaignTime.Now.ToString();
            string lordTitle = lord != null ? $"Lord {lord.Name}" : "Command";
            
            // Build the narrative based on current state
            var narrative = new System.Text.StringBuilder();
            
            // Opening - Military style
            narrative.AppendLine($"GENERAL ORDERS - {periodName}");
            narrative.AppendLine($"From the desk of {lordTitle}");
            narrative.AppendLine();
            
            // Current situation summary
            if (party?.CurrentSettlement != null)
            {
                narrative.AppendLine($"The company remains encamped at {party.CurrentSettlement.Name}. ");
                narrative.AppendLine(GetSettlementFlavorText(party.CurrentSettlement));
            }
            else if (party?.Army != null)
            {
                narrative.AppendLine($"We march with the army of {party.Army.LeaderParty?.LeaderHero?.Name}. ");
                narrative.AppendLine("All soldiers are to maintain formation and follow their assigned duties.");
            }
            else
            {
                narrative.AppendLine("The company is on the march. Stay vigilant and maintain discipline.");
            }
            narrative.AppendLine();
            
            // War status
            if (kingdom != null)
            {
                int warCount = 0;
                string enemyNames = "";
                foreach (var otherKingdom in Kingdom.All)
                {
                    if (otherKingdom != kingdom && kingdom.IsAtWarWith(otherKingdom))
                    {
                        warCount++;
                        if (warCount <= 2)
                            enemyNames += (enemyNames.Length > 0 ? " and " : "") + otherKingdom.Name.ToString();
                    }
                }
                
                if (warCount > 0)
                {
                    narrative.AppendLine($"WAR STATUS: {kingdom.Name} remains at war with {enemyNames}.");
                    narrative.AppendLine(GetWarFlavorText(warCount));
                }
                else
                {
                    narrative.AppendLine($"PEACE: {kingdom.Name} is currently at peace.");
                    narrative.AppendLine("Use this time to train and resupply.");
                }
            }
            narrative.AppendLine();
            
            // Lance status
            if (schedule?.LanceNeeds != null)
            {
                var needs = schedule.LanceNeeds;
                narrative.AppendLine("LANCE STATUS:");
                
                if (needs.Rest < 40)
                    narrative.AppendLine("- The men are exhausted. Rest is ordered when possible.");
                if (needs.Morale < 40)
                    narrative.AppendLine("- Morale is low. Officers are to maintain spirits.");
                if (needs.Supplies < 40)
                    narrative.AppendLine("- Supplies are running low. Forage when able.");
                if (needs.Readiness > 70)
                    narrative.AppendLine("- Combat readiness is good. Maintain current training.");
                    
                if (needs.Rest >= 40 && needs.Morale >= 40 && needs.Supplies >= 40)
                    narrative.AppendLine("- All needs are satisfactory. Continue normal operations.");
            }
            narrative.AppendLine();
            
            // Closing
            narrative.AppendLine("---");
            narrative.AppendLine($"Signed, {lordTitle}");
            narrative.AppendLine($"{dispatchDate}");
            
            // Add the main general order
            GeneralOrders.Add(new ReportItemVM(
                $"General Orders - {periodName}",
                dispatchDate,
                "Dispatch",
                OnReportSelect,
                factionBanner,
                detailText: narrative.ToString()
            ));
            
            // Add recent battle summary if any
            AddBattleSummary(enlistment, factionBanner);
            
            // Add standing orders
            AddStandingOrders(enlistment, schedule, factionBanner);
        }
        
        /// <summary>
        /// Get a period name for the 3-day cycle.
        /// </summary>
        private string GetPeriodName(int periodNumber)
        {
            string[] periods = { "First Watch", "Second Watch", "Third Watch", "Fourth Watch" };
            int periodInMonth = periodNumber % 4;
            return periods[periodInMonth];
        }
        
        /// <summary>
        /// Get flavor text based on settlement type.
        /// </summary>
        private string GetSettlementFlavorText(Settlement settlement)
        {
            if (settlement.IsTown)
                return "The town provides good opportunity for resupply and rest. Soldiers are reminded to conduct themselves with honor.";
            else if (settlement.IsCastle)
                return "The castle garrison has made room for our company. Maintain vigilance on the walls.";
            else if (settlement.IsVillage)
                return "We take shelter in the village. Be respectful of the locals - they are our people.";
            return "We make camp and rest.";
        }
        
        /// <summary>
        /// Get flavor text based on war status.
        /// </summary>
        private string GetWarFlavorText(int warCount)
        {
            if (warCount >= 3)
                return "Multiple fronts demand our attention. Every soldier must be ready.";
            else if (warCount == 2)
                return "Two enemies threaten our borders. Stay sharp and follow orders.";
            else
                return "Battle may come at any time. Keep your weapons ready and your spirits high.";
        }
        
        /// <summary>
        /// Add battle summary if recent battles occurred.
        /// </summary>
        private void AddBattleSummary(EnlistmentBehavior enlistment, Banner factionBanner)
        {
            try
            {
                if (Campaign.Current?.LogEntryHistory?.GameActionLogs == null) return;
                
                var logs = Campaign.Current.LogEntryHistory.GameActionLogs;
                var cutoffTime = CampaignTime.Now - CampaignTime.Days(3);
                int battleCount = 0;
                int siegeCount = 0;
                
                for (int i = logs.Count - 1; i >= 0 && i > logs.Count - 50; i--)
                {
                    var logEntry = logs[i];
                    if (logEntry.GameTime < cutoffTime) break;
                    
                    if (logEntry is BattleStartedLogEntry)
                        battleCount++;
                    else if (logEntry is BesiegeSettlementLogEntry)
                        siegeCount++;
                }
                
                if (battleCount > 0 || siegeCount > 0)
                {
                    var summary = new System.Text.StringBuilder();
                    summary.AppendLine("RECENT ENGAGEMENTS:");
                    
                    if (battleCount > 0)
                        summary.AppendLine($"- {battleCount} field battle(s) fought in the region");
                    if (siegeCount > 0)
                        summary.AppendLine($"- {siegeCount} siege(s) underway or completed");
                        
                    summary.AppendLine();
                    summary.AppendLine("All soldiers who participated are to report any injuries to the medical tent.");
                    
                    GeneralOrders.Add(new ReportItemVM(
                        "Battle Report",
                        CampaignTime.Now.ToString(),
                        "Combat",
                        OnReportSelect,
                        factionBanner,
                        detailText: summary.ToString()
                    ));
                }
            }
            catch (System.Exception ex)
            {
                Mod.Core.Logging.ModLogger.Warn("CampReportsVM", $"Error generating battle summary: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Add standing orders based on current schedule.
        /// </summary>
        private void AddStandingOrders(EnlistmentBehavior enlistment, ScheduleBehavior schedule, Banner factionBanner)
        {
            if (schedule?.CurrentSchedule == null) return;
            
            var orders = new System.Text.StringBuilder();
            orders.AppendLine("STANDING ORDERS:");
            orders.AppendLine();
            orders.AppendLine($"Day {schedule.CurrentSchedule.CycleDay} of the current duty cycle.");
            orders.AppendLine();
            
            if (!string.IsNullOrEmpty(schedule.CurrentSchedule.LordOrders))
            {
                orders.AppendLine($"Commander's Intent: {schedule.CurrentSchedule.LordOrders}");
            }
            else
            {
                orders.AppendLine("Commander's Intent: Follow standard duties and maintain readiness.");
            }
            
            orders.AppendLine();
            orders.AppendLine("All soldiers are to report to their assigned duties on time.");
            orders.AppendLine("Failure to comply will result in disciplinary action.");
            
            GeneralOrders.Add(new ReportItemVM(
                "Standing Orders",
                CampaignTime.Now.ToString(),
                "Orders",
                OnReportSelect,
                factionBanner,
                detailText: orders.ToString()
            ));
        }
        
        /// <summary>
        /// Add warnings for critical lance needs.
        /// </summary>
        private void AddLanceNeedsWarnings(Schedule.Models.LanceNeedsState needs, Banner banner)
        {
            if (needs.Readiness < 30)
            {
                LanceNews.Add(new ReportItemVM(
                    $"Low Readiness ({needs.Readiness}%) - Schedule training or drills",
                    CampaignTime.Now.ToString(),
                    "Warning",
                    OnReportSelect,
                    banner,
                    detailText: "Readiness is low. Schedule training or drills to improve discipline and preparedness."
                ));
            }
            
            if (needs.Morale < 30)
            {
                LanceNews.Add(new ReportItemVM(
                    $"Low Morale ({needs.Morale}%) - Allow rest or social activities",
                    CampaignTime.Now.ToString(),
                    "Warning",
                    OnReportSelect,
                    banner,
                    detailText: "Morale is low. Grant rest or social time to steady the men."
                ));
            }
            
            if (needs.Rest < 30)
            {
                LanceNews.Add(new ReportItemVM(
                    $"Lance Exhausted ({needs.Rest}%) - The men need rest",
                    CampaignTime.Now.ToString(),
                    "Warning",
                    OnReportSelect,
                    banner,
                    detailText: "The lance is exhausted. Schedule rest as soon as possible."
                ));
            }
            
            if (needs.Supplies < 30)
            {
                LanceNews.Add(new ReportItemVM(
                    $"Low Supplies ({needs.Supplies}%) - Resupply needed soon",
                    CampaignTime.Now.ToString(),
                    "Warning",
                    OnReportSelect,
                    banner,
                    detailText: "Supplies are running low. Resupply, forage, or requisition stores."
                ));
            }
        }
        
        /// <summary>
        /// Populate Kingdom News directly from native game log system.
        /// Uses Campaign.Current.LogEntryHistory.GameActionLogs like native KingdomWarLogItemVM.
        /// </summary>
        private void PopulateKingdomNewsFromGameLogs(EnlistmentBehavior enlistment)
        {
            if (enlistment?.IsEnlisted != true) return;
            
            var kingdom = enlistment.EnlistedLord?.Clan?.Kingdom;
            var playerFaction = enlistment.EnlistedLord?.MapFaction;
            Banner kingdomBanner = kingdom?.Banner ?? enlistment.EnlistedLord?.ClanBanner;
            
            // Add kingdom status summary
            if (kingdom != null)
            {
                var activeWars = 0;
                var enemyName = "";
                foreach (var otherKingdom in Kingdom.All)
                {
                    if (otherKingdom != kingdom && kingdom.IsAtWarWith(otherKingdom))
                    {
                        activeWars++;
                        if (activeWars == 1)
                            enemyName = otherKingdom.Name.ToString();
                    }
                }
                
                string warStatus;
                if (activeWars == 0)
                    warStatus = $"{kingdom.Name} is at peace. {kingdom.Fiefs.Count} fiefs controlled.";
                else if (activeWars == 1)
                    warStatus = $"{kingdom.Name} at war with {enemyName}. {kingdom.Fiefs.Count} fiefs.";
                else
                    warStatus = $"{kingdom.Name} at war with {activeWars} factions.";
                
                KingdomNews.Add(new ReportItemVM(
                    warStatus,
                    CampaignTime.Now.ToString(),
                    "Kingdom",
                    OnReportSelect,
                    kingdomBanner,
                    detailText: warStatus
                ));
            }
            
            // Pull recent events from native game log system - like native KingdomWarLogItemVM does
            try
            {
                if (Campaign.Current?.LogEntryHistory?.GameActionLogs == null) return;
                
                var logs = Campaign.Current.LogEntryHistory.GameActionLogs;
                var cutoffTime = CampaignTime.Now - CampaignTime.Days(30);
                int addedCount = 0;
                
                // Iterate from newest to oldest
                for (int i = logs.Count - 1; i >= 0 && addedCount < 15; i--)
                {
                    var logEntry = logs[i];
                    
                    // Only process logs that implement IEncyclopediaLog and are recent
                    if (logEntry is IEncyclopediaLog encLog && logEntry.GameTime >= cutoffTime)
                    {
                        // Get the faction associated with this log for the banner
                        var effectorFaction = GetEffectorFaction(logEntry, playerFaction);
                        
                        // Only show logs relevant to player's faction
                        if (effectorFaction != null && IsLogRelevantToFaction(logEntry, playerFaction))
                        {
                            // Native pattern: GetEncyclopediaText() for text, GameTime for time
                            var logText = encLog.GetEncyclopediaText().ToString();
                            var timeText = logEntry.GameTime.ToString();
                            var category = GetLogCategory(logEntry);
                            
                            KingdomNews.Add(new ReportItemVM(
                                logText,
                                timeText,
                                category,
                                OnReportSelect,
                                effectorFaction.Banner,
                                detailText: logText
                            ));
                            
                            addedCount++;
                        }
                    }
                }
                
                Mod.Core.Logging.ModLogger.Debug("CampReportsVM", 
                    $"Added {addedCount} kingdom news from game logs");
            }
            catch (System.Exception ex)
            {
                Mod.Core.Logging.ModLogger.Warn("CampReportsVM", $"Error reading game logs: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Get the faction that caused this log entry (for banner display).
        /// Uses reflection since native log entries store data in private fields.
        /// </summary>
        private IFaction GetEffectorFaction(LogEntry log, IFaction playerFaction)
        {
            try
            {
                // Use reflection to find faction/character references in the log
                var logType = log.GetType();
                var fields = logType.GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                foreach (var field in fields)
                {
                    var value = field.GetValue(log);
                    
                    // Direct faction reference
                    if (value is IFaction faction)
                        return faction;
                    
                    // Hero -> their faction
                    if (value is Hero hero && hero.MapFaction != null)
                        return hero.MapFaction;
                    
                    // Character -> hero -> faction
                    if (value is CharacterObject character && character.HeroObject?.MapFaction != null)
                        return character.HeroObject.MapFaction;
                    
                    // Settlement -> owner faction
                    if (value is Settlement settlement && settlement.MapFaction != null)
                        return settlement.MapFaction;
                    
                    // Clan -> kingdom or self
                    if (value is Clan clan)
                        return clan.Kingdom ?? (IFaction)clan;
                }
                
                // Default to player's faction
                return playerFaction;
            }
            catch
            {
                return playerFaction;
            }
        }
        
        /// <summary>
        /// Get category string for a log entry.
        /// </summary>
        private string GetLogCategory(LogEntry log)
        {
            return log switch
            {
                DeclareWarLogEntry => "War",
                MakePeaceLogEntry => "Peace",
                BattleStartedLogEntry => "Battle",
                BesiegeSettlementLogEntry => "Siege",
                ChangeSettlementOwnerLogEntry => "Conquest",
                ArmyCreationLogEntry => "Army",
                ArmyDispersionLogEntry => "Army",
                CharacterKilledLogEntry => "Death",
                TakePrisonerLogEntry => "Prisoner",
                _ => "News"
            };
        }
        
        /// <summary>
        /// Check if a log entry is relevant to the player's faction.
        /// Uses reflection or type-specific checks.
        /// </summary>
        private bool IsLogRelevantToFaction(LogEntry log, IFaction playerFaction)
        {
            if (playerFaction == null) return false;
            
            try
            {
                // Use reflection to find faction references in the log
                var logType = log.GetType();
                
                // Check all fields for faction references
                foreach (var field in logType.GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance))
                {
                    var value = field.GetValue(log);
                    
                    if (value is IFaction faction && faction == playerFaction)
                        return true;
                    
                    if (value is Hero hero && hero.MapFaction == playerFaction)
                        return true;
                    
                    if (value is CharacterObject character && character.HeroObject?.MapFaction == playerFaction)
                        return true;
                    
                    if (value is Settlement settlement && settlement.MapFaction == playerFaction)
                        return true;
                    
                    if (value is Clan clan && clan.Kingdom == playerFaction)
                        return true;
                }
                
                return false;
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// Get title from dispatch item.
        /// </summary>
        private string GetDispatchTitle(DispatchItem dispatch)
        {
            if (!string.IsNullOrEmpty(dispatch.HeadlineKey))
            {
                var textObj = new TaleWorlds.Localization.TextObject("{=" + dispatch.HeadlineKey + "}");
                if (dispatch.PlaceholderValues != null)
                {
                    foreach (var kv in dispatch.PlaceholderValues)
                    {
                        textObj.SetTextVariable(kv.Key, kv.Value);
                    }
                }
                return textObj.ToString();
            }
            return dispatch.Category ?? "News";
        }
        
        /// <summary>
        /// Get description from dispatch item.
        /// </summary>
        private string GetDispatchDescription(DispatchItem dispatch)
        {
            if (!string.IsNullOrEmpty(dispatch.StoryKey))
            {
                var textObj = new TaleWorlds.Localization.TextObject("{=" + dispatch.StoryKey + "}");
                if (dispatch.PlaceholderValues != null)
                {
                    foreach (var kv in dispatch.PlaceholderValues)
                    {
                        textObj.SetTextVariable(kv.Key, kv.Value);
                    }
                }
                return textObj.ToString();
            }
            return "";
        }
        
        /// <summary>
        /// Handle report item selection.
        /// </summary>
        private void OnReportSelect(ReportItemVM report)
        {
            // Deselect all
            foreach (var item in GeneralOrders) item.IsSelected = false;
            foreach (var item in Opportunities) item.IsSelected = false;
            foreach (var item in RecentOutcomes) item.IsSelected = false;
            foreach (var item in LanceNews) item.IsSelected = false;
            foreach (var item in CompanyNews) item.IsSelected = false;
            foreach (var item in KingdomNews) item.IsSelected = false;
            
            // Select this one
            if (report != null)
            {
                report.IsSelected = true;
                CurrentSelectedReport = report;
                IsReportSelected = true;
                
                // Update direct properties for UI binding (avoids nested DataSource issues)
                SelectedReportTitle = report.Title;
                SelectedReportCategory = report.Category;
                SelectedReportDescription = report.Description;

                // Phase 5.5: selecting an opportunity routes the player to the Decisions surface.
                if (_selectedCategoryType == ReportCategoryType.Opportunities &&
                    !string.IsNullOrWhiteSpace(report.DecisionEventId))
                {
                    OpenDecisionsMenuFromReports(report.DecisionEventId);
                }
            }
            else
            {
                CurrentSelectedReport = null;
                IsReportSelected = false;
                SelectedReportTitle = "";
                SelectedReportCategory = "";
                SelectedReportDescription = "";
            }
        }

        // ===== Properties =====
        
        [DataSourceProperty]
        public bool Show
        {
            get => _show;
            set
            {
                if (value == _show) return;
                _show = value;
                OnPropertyChangedWithValue(value, nameof(Show));
                
                if (value)
                {
                    RefreshAllNews();
                }
            }
        }

        [DataSourceProperty]
        public string ReportsText
        {
            get => _reportsText;
            set
            {
                if (value == _reportsText) return;
                _reportsText = value;
                OnPropertyChangedWithValue(value, nameof(ReportsText));
            }
        }

        [DataSourceProperty]
        public string NumReportsText
        {
            get => _numReportsText;
            set
            {
                if (value == _numReportsText) return;
                _numReportsText = value;
                OnPropertyChangedWithValue(value, nameof(NumReportsText));
            }
        }
        
        [DataSourceProperty]
        public string LanceNewsText
        {
            get => _lanceNewsText;
            set
            {
                if (value == _lanceNewsText) return;
                _lanceNewsText = value;
                OnPropertyChangedWithValue(value, nameof(LanceNewsText));
            }
        }
        
        [DataSourceProperty]
        public string CompanyNewsText
        {
            get => _companyNewsText;
            set
            {
                if (value == _companyNewsText) return;
                _companyNewsText = value;
                OnPropertyChangedWithValue(value, nameof(CompanyNewsText));
            }
        }
        
        [DataSourceProperty]
        public string KingdomNewsText
        {
            get => _kingdomNewsText;
            set
            {
                if (value == _kingdomNewsText) return;
                _kingdomNewsText = value;
                OnPropertyChangedWithValue(value, nameof(KingdomNewsText));
            }
        }
        
        [DataSourceProperty]
        public string NumLanceNewsText
        {
            get => _numLanceNewsText;
            set
            {
                if (value == _numLanceNewsText) return;
                _numLanceNewsText = value;
                OnPropertyChangedWithValue(value, nameof(NumLanceNewsText));
            }
        }
        
        [DataSourceProperty]
        public string NumCompanyNewsText
        {
            get => _numCompanyNewsText;
            set
            {
                if (value == _numCompanyNewsText) return;
                _numCompanyNewsText = value;
                OnPropertyChangedWithValue(value, nameof(NumCompanyNewsText));
            }
        }
        
        [DataSourceProperty]
        public string NumKingdomNewsText
        {
            get => _numKingdomNewsText;
            set
            {
                if (value == _numKingdomNewsText) return;
                _numKingdomNewsText = value;
                OnPropertyChangedWithValue(value, nameof(NumKingdomNewsText));
            }
        }
        
        [DataSourceProperty]
        public MBBindingList<ReportItemVM> LanceNews
        {
            get => _lanceNews;
            set
            {
                if (value == _lanceNews) return;
                _lanceNews = value;
                OnPropertyChangedWithValue(value, nameof(LanceNews));
            }
        }
        
        [DataSourceProperty]
        public MBBindingList<ReportItemVM> CompanyNews
        {
            get => _companyNews;
            set
            {
                if (value == _companyNews) return;
                _companyNews = value;
                OnPropertyChangedWithValue(value, nameof(CompanyNews));
            }
        }
        
        [DataSourceProperty]
        public MBBindingList<ReportItemVM> KingdomNews
        {
            get => _kingdomNews;
            set
            {
                if (value == _kingdomNews) return;
                _kingdomNews = value;
                OnPropertyChangedWithValue(value, nameof(KingdomNews));
            }
        }
        
        [DataSourceProperty]
        public MBBindingList<ReportItemVM> GeneralOrders
        {
            get => _generalOrders;
            set
            {
                if (value == _generalOrders) return;
                _generalOrders = value;
                OnPropertyChangedWithValue(value, nameof(GeneralOrders));
            }
        }
        
        [DataSourceProperty]
        public string GeneralOrdersText
        {
            get => _generalOrdersText;
            set
            {
                if (value == _generalOrdersText) return;
                _generalOrdersText = value;
                OnPropertyChangedWithValue(value, nameof(GeneralOrdersText));
            }
        }
        
        [DataSourceProperty]
        public string NumGeneralOrdersText
        {
            get => _numGeneralOrdersText;
            set
            {
                if (value == _numGeneralOrdersText) return;
                _numGeneralOrdersText = value;
                OnPropertyChangedWithValue(value, nameof(NumGeneralOrdersText));
            }
        }

        [DataSourceProperty]
        public MBBindingList<ReportItemVM> Opportunities
        {
            get => _opportunities;
            set
            {
                if (value == _opportunities) return;
                _opportunities = value;
                OnPropertyChangedWithValue(value, nameof(Opportunities));
            }
        }

        [DataSourceProperty]
        public MBBindingList<ReportItemVM> RecentOutcomes
        {
            get => _recentOutcomes;
            set
            {
                if (value == _recentOutcomes) return;
                _recentOutcomes = value;
                OnPropertyChangedWithValue(value, nameof(RecentOutcomes));
            }
        }

        [DataSourceProperty]
        public string OpportunitiesText
        {
            get => _opportunitiesText;
            set
            {
                if (value == _opportunitiesText) return;
                _opportunitiesText = value;
                OnPropertyChangedWithValue(value, nameof(OpportunitiesText));
            }
        }

        [DataSourceProperty]
        public string NumOpportunitiesText
        {
            get => _numOpportunitiesText;
            set
            {
                if (value == _numOpportunitiesText) return;
                _numOpportunitiesText = value;
                OnPropertyChangedWithValue(value, nameof(NumOpportunitiesText));
            }
        }

        [DataSourceProperty]
        public string RecentOutcomesText
        {
            get => _recentOutcomesText;
            set
            {
                if (value == _recentOutcomesText) return;
                _recentOutcomesText = value;
                OnPropertyChangedWithValue(value, nameof(RecentOutcomesText));
            }
        }

        [DataSourceProperty]
        public string NumRecentOutcomesText
        {
            get => _numRecentOutcomesText;
            set
            {
                if (value == _numRecentOutcomesText) return;
                _numRecentOutcomesText = value;
                OnPropertyChangedWithValue(value, nameof(NumRecentOutcomesText));
            }
        }
        
        [DataSourceProperty]
        public ReportItemVM CurrentSelectedReport
        {
            get => _currentSelectedReport;
            set
            {
                if (value == _currentSelectedReport) return;
                _currentSelectedReport = value;
                OnPropertyChangedWithValue(value, nameof(CurrentSelectedReport));
            }
        }
        
        [DataSourceProperty]
        public bool IsReportSelected
        {
            get => _isReportSelected;
            set
            {
                if (value == _isReportSelected) return;
                _isReportSelected = value;
                OnPropertyChangedWithValue(value, nameof(IsReportSelected));
            }
        }
        
        [DataSourceProperty]
        public string SelectedReportTitle
        {
            get => _selectedReportTitle;
            set
            {
                if (value == _selectedReportTitle) return;
                _selectedReportTitle = value;
                OnPropertyChangedWithValue(value, nameof(SelectedReportTitle));
            }
        }
        
        [DataSourceProperty]
        public string SelectedReportCategory
        {
            get => _selectedReportCategory;
            set
            {
                if (value == _selectedReportCategory) return;
                _selectedReportCategory = value;
                OnPropertyChangedWithValue(value, nameof(SelectedReportCategory));
            }
        }
        
        [DataSourceProperty]
        public string SelectedReportDescription
        {
            get => _selectedReportDescription;
            set
            {
                if (value == _selectedReportDescription) return;
                _selectedReportDescription = value;
                OnPropertyChangedWithValue(value, nameof(SelectedReportDescription));
            }
        }
        
        [DataSourceProperty]
        public MBBindingList<ReportCategoryItemVM> ReportCategories
        {
            get => _reportCategories;
            set
            {
                if (value == _reportCategories) return;
                _reportCategories = value;
                OnPropertyChangedWithValue(value, nameof(ReportCategories));
            }
        }
        
        [DataSourceProperty]
        public bool IsCategorySelected
        {
            get => _isCategorySelected;
            set
            {
                if (value == _isCategorySelected) return;
                _isCategorySelected = value;
                OnPropertyChangedWithValue(value, nameof(IsCategorySelected));
            }
        }
        
        [DataSourceProperty]
        public string SelectedCategoryTitle
        {
            get => _selectedCategoryTitle;
            set
            {
                if (value == _selectedCategoryTitle) return;
                _selectedCategoryTitle = value;
                OnPropertyChangedWithValue(value, nameof(SelectedCategoryTitle));
            }
        }
        
        [DataSourceProperty]
        public MBBindingList<ReportItemVM> SelectedCategoryNews
        {
            get => _selectedCategoryNews;
            set
            {
                if (value == _selectedCategoryNews) return;
                _selectedCategoryNews = value;
                OnPropertyChangedWithValue(value, nameof(SelectedCategoryNews));
            }
        }
    }

    public enum ReportCategoryType
    {
        GeneralOrders = 0,
        LanceReports = 1,
        CompanyReports = 2,
        KingdomReports = 3,

        // Phase 5.5: decision-driven report categories
        Opportunities = 4,
        RecentOutcomes = 5
    }

    /// <summary>
    /// The clickable items under the Reports toggle (like Nord/Sturgia).
    /// </summary>
    public class ReportCategoryItemVM : ViewModel
    {
        private readonly System.Action<ReportCategoryType> _onSelect;
        private string _name;
        private bool _isSelected;
        private BannerImageIdentifierVM _banner;

        public ReportCategoryItemVM(
            ReportCategoryType categoryType,
            string name,
            int _,
            System.Action<ReportCategoryType> onSelect,
            Banner banner = null)
        {
            CategoryType = categoryType;
            _name = name;
            _onSelect = onSelect;
            Banner = banner != null ? new BannerImageIdentifierVM(banner, true) : new BannerImageIdentifierVM((Banner)null);
        }

        public ReportCategoryType CategoryType { get; }

        public void OnSelect()
        {
            _onSelect?.Invoke(CategoryType);
        }

        [DataSourceProperty]
        public string Name
        {
            get => _name;
            set
            {
                if (value == _name) return;
                _name = value;
                OnPropertyChangedWithValue(value, nameof(Name));
            }
        }

        [DataSourceProperty]
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (value == _isSelected) return;
                _isSelected = value;
                OnPropertyChangedWithValue(value, nameof(IsSelected));
            }
        }

        [DataSourceProperty]
        public BannerImageIdentifierVM Banner
        {
            get => _banner;
            set
            {
                if (value == _banner) return;
                _banner = value;
                OnPropertyChangedWithValue(value, nameof(Banner));
            }
        }
    }
    
    /// <summary>
    /// ViewModel for a single report/news item.
    /// Matches native KingdomWarLogItemVM pattern EXACTLY with same property names.
    /// </summary>
    public class ReportItemVM : ViewModel
    {
        private readonly System.Action<ReportItemVM> _onSelect;
        private readonly string _decisionEventId;
        
        // Use EXACT same property names as native KingdomWarLogItemVM
        private string _warLogText;      // Main text (like "Grykka created an army.")
        private string _warLogTimeText;  // Time text (like "Summer 2, 1084")
        private string _detailText;      // Long text shown in our details panel
        private string _category;
        private bool _isSelected;
        private BannerImageIdentifierVM _banner;
        
        public ReportItemVM(
            string logText,
            string logTimeText,
            string category,
            System.Action<ReportItemVM> onSelect,
            Banner banner = null,
            string detailText = null,
            string decisionEventId = null)
        {
            _warLogText = logText;
            _warLogTimeText = logTimeText;
            _detailText = string.IsNullOrWhiteSpace(detailText) ? logText : detailText;
            _category = category;
            _onSelect = onSelect;
            _decisionEventId = decisionEventId ?? string.Empty;
            
            // Use banner if provided - native pattern: new BannerImageIdentifierVM(banner, true)
            if (banner != null)
            {
                Banner = new BannerImageIdentifierVM(banner, true);
            }
            else
            {
                // Create empty banner
                Banner = new BannerImageIdentifierVM((Banner)null);
            }
        }
        
        /// <summary>
        /// Called when this item is clicked - matches native KingdomWarItemVM.OnSelect pattern.
        /// </summary>
        public void OnSelect()
        {
            _onSelect?.Invoke(this);
        }
        
        // Keep for backward compatibility
        public void ExecuteSelect() => OnSelect();
        
        // Native pattern: ExecuteLink for clickable text in RichTextWidget
        private void ExecuteLink(string link)
        {
            Campaign.Current?.EncyclopediaManager?.GoToLink(link);
        }
        
        // NATIVE PROPERTY NAME - must match DiplomacyWarLogElement.xml
        [DataSourceProperty]
        public string WarLogText
        {
            get => _warLogText;
            set
            {
                if (value == _warLogText) return;
                _warLogText = value;
                OnPropertyChangedWithValue(value, nameof(WarLogText));
            }
        }
        
        // NATIVE PROPERTY NAME - must match DiplomacyWarLogElement.xml
        [DataSourceProperty]
        public string WarLogTimeText
        {
            get => _warLogTimeText;
            set
            {
                if (value == _warLogTimeText) return;
                _warLogTimeText = value;
                OnPropertyChangedWithValue(value, nameof(WarLogTimeText));
            }
        }
        
        // Aliases for our code
        [DataSourceProperty]
        public string Title => _warLogText;
        
        [DataSourceProperty]
        public string Description => _detailText;

        public string DecisionEventId => _decisionEventId;
        
        [DataSourceProperty]
        public string Category
        {
            get => _category;
            set
            {
                if (value == _category) return;
                _category = value;
                OnPropertyChangedWithValue(value, nameof(Category));
            }
        }
        
        [DataSourceProperty]
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (value == _isSelected) return;
                _isSelected = value;
                OnPropertyChangedWithValue(value, nameof(IsSelected));
            }
        }
        
        // NATIVE PROPERTY NAME - must match DiplomacyWarLogElement.xml
        [DataSourceProperty]
        public BannerImageIdentifierVM Banner
        {
            get => _banner;
            set
            {
                if (value == _banner) return;
                _banner = value;
                OnPropertyChangedWithValue(value, nameof(Banner));
            }
        }
    }
}
