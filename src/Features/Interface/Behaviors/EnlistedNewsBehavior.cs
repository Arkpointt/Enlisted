using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Enlisted.Features.Camp;
using Enlisted.Features.Company;
using Enlisted.Features.Conditions;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Features.Escalation;
using Enlisted.Features.Interface.News.Generation;
using Enlisted.Features.Interface.News.Generation.Producers;
using Enlisted.Features.Interface.News.Models;
using Enlisted.Features.Interface.News.State;
using Enlisted.Mod.Core.Logging;
using Enlisted.Mod.Core.Triggers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace Enlisted.Features.Interface.Behaviors
{
    /// <summary>
    /// Manages kingdom-wide and personal news/dispatch feeds.
    /// Read-only observer of campaign events that generates localized headlines.
    /// </summary>
    public sealed class EnlistedNewsBehavior : CampaignBehaviorBase
    {
        private const string LogCategory = "News";
        private const int MaxKingdomFeedItems = 60;
        private const int MaxPersonalFeedItems = 20;

        /// <summary>
        /// Singleton instance for external access (menu integration, etc.).
        /// </summary>
        public static EnlistedNewsBehavior Instance { get; private set; }

        /// <summary>
        /// Kingdom-wide news feed (wars, battles, settlements, prisoner fates, politics).
        /// Displayed in the enlisted_status menu.
        /// </summary>
        private List<DispatchItem> _kingdomFeed = new List<DispatchItem>();

        /// <summary>
        /// Personal/army news feed (your lord's army, participation, direct orders).
        /// Displayed in the enlisted_activities (camp) menu.
        /// </summary>
        private List<DispatchItem> _personalFeed = new List<DispatchItem>();

        /// <summary>
        /// Day number when the last bulletin was generated (2-day cadence).
        /// </summary>
        private int _lastBulletinDayNumber = -1;

        /// <summary>
        /// Day number when the current Daily Brief was generated.
        /// The brief is intentionally stable for the whole in-game day (light RP flavor).
        /// </summary>
        private int _lastDailyBriefDayNumber = -1;

        /// <summary>
        /// Daily Brief lines (stable for the day).
        /// Stored as raw values (without "Company:"/"Unit:"/"Kingdom:" prefixes) so UIs can format as needed.
        /// </summary>
        private string _dailyBriefCompany = string.Empty;
        private string _dailyBriefUnit = string.Empty;
        private string _dailyBriefKingdom = string.Empty;

        /// <summary>
        /// Tracks when the player last participated in a battle (for "battle aftermath" flavor).
        /// </summary>
        private CampaignTime _lastPlayerBattleTime = CampaignTime.Zero;
        
        /// <summary>
        /// Type of the last battle: "bandit", "army", "siege", or empty if none.
        /// </summary>
        private string _lastPlayerBattleType = string.Empty;
        
        /// <summary>
        /// Whether the player won their last battle.
        /// </summary>
        private bool _lastPlayerBattleWon;

        /// <summary>
        /// Cached battle initial strengths for pyrrhic detection.
        /// Key: MapEvent hash code as string.
        /// </summary>
        private Dictionary<string, BattleSnapshot> _battleSnapshots = new Dictionary<string, BattleSnapshot>();

        /// <summary>
        /// Tracks if the lord had an army yesterday (for army formation detection).
        /// </summary>
        private bool _lordHadArmyYesterday;

        /// <summary>
        /// Last time skill progress was checked (cached for 6 hours to avoid expensive recalculation).
        /// </summary>
        private CampaignTime _lastSkillProgressCheck = CampaignTime.Zero;
        
        /// <summary>
        /// Cached skill progress line result (empty string if no skills near level-up).
        /// </summary>
        private string _cachedSkillProgressLine = string.Empty;

        // Persisted Daily Report and rolling ledger state.
        private CampNewsState _campNewsState = new CampNewsState();

        // "Since last muster" counters for camp news display. Reset at each muster cycle.
        private int _lostSinceLastMuster;
        private int _sickSinceLastMuster;

        /// <summary>
        /// Track order outcomes for display in daily brief and detailed reports.
        /// </summary>
        private List<OrderOutcomeRecord> _orderOutcomes = new List<OrderOutcomeRecord>();

        /// <summary>
        /// Track reputation changes for display in reports.
        /// </summary>
        private List<ReputationChangeRecord> _reputationChanges = new List<ReputationChangeRecord>();

        /// <summary>
        /// Track company need changes for display in reports.
        /// </summary>
        private List<CompanyNeedChangeRecord> _companyNeedChanges = new List<CompanyNeedChangeRecord>();

        /// <summary>
        /// Track event outcomes for Personal Feed headlines and Daily Brief context.
        /// Shows what events fired, choices made, and effects applied.
        /// </summary>
        private List<EventOutcomeRecord> _eventOutcomes = new List<EventOutcomeRecord>();

        /// <summary>
        /// Track pending chain events for Daily Brief context hints.
        /// Shows reminders like "A comrade owes you money" before the follow-up event fires.
        /// </summary>
        private List<PendingEventRecord> _pendingEvents = new List<PendingEventRecord>();

        /// <summary>
        /// Track muster outcomes for camp news display and daily brief.
        /// One entry per muster cycle, showing pay, rations, and unit status.
        /// </summary>
        private List<MusterOutcomeRecord> _musterOutcomes = new List<MusterOutcomeRecord>();

        // Expose the last generated snapshot to other systems (for example, Decisions) without forcing a
        // recomputation. The snapshot contains primitives only, so it is safe to cache and share as read-only.
        private DailyReportSnapshot _lastDailyReportSnapshot;

        // Producer set used to populate snapshot facts without scattering logic across the behavior.
        private static readonly IDailyReportFactProducer[] DailyReportFactProducers =
        {
            new CompanyMovementObjectiveProducer(),
            new UnitStatusFactProducer(),
            new KingdomHeadlineFactProducer()
        };

        public EnlistedNewsBehavior()
        {
            Instance = this;
        }

        /// <summary>
        /// Current kingdom feed for read-only inspection (debugging, UI).
        /// </summary>
        public IReadOnlyList<DispatchItem> KingdomFeed => _kingdomFeed;

        /// <summary>
        /// Current personal feed for read-only inspection (debugging, UI).
        /// </summary>
        public IReadOnlyList<DispatchItem> PersonalFeed => _personalFeed;

        public override void RegisterEvents()
        {
            // Daily tick for bulletin generation and army formation detection
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);

            // Battle events for victory/defeat/pyrrhic detection
            CampaignEvents.MapEventStarted.AddNonSerializedListener(this, OnMapEventStarted);
            CampaignEvents.MapEventEnded.AddNonSerializedListener(this, OnMapEventEnded);

            // Siege events
            CampaignEvents.OnSiegeEventStartedEvent.AddNonSerializedListener(this, OnSiegeStarted);

            // Prisoner events (captures, releases, escapes)
            CampaignEvents.HeroPrisonerTaken.AddNonSerializedListener(this, OnHeroPrisonerTaken);
            CampaignEvents.HeroPrisonerReleased.AddNonSerializedListener(this, OnHeroPrisonerReleased);

            // Hero death/execution
            CampaignEvents.HeroKilledEvent.AddNonSerializedListener(this, OnHeroKilled);

            // Settlement ownership changes
            CampaignEvents.OnSettlementOwnerChangedEvent.AddNonSerializedListener(this, OnSettlementOwnerChanged);

            // Army dispersal (personal feed)
            CampaignEvents.ArmyDispersed.AddNonSerializedListener(this, OnArmyDispersed);

            // Village raids
            CampaignEvents.VillageBeingRaided.AddNonSerializedListener(this, OnVillageRaided);

            // War/peace declarations
            CampaignEvents.WarDeclared.AddNonSerializedListener(this, OnWarDeclared);
            CampaignEvents.MakePeace.AddNonSerializedListener(this, OnPeaceMade);

            ModLogger.Info(LogCategory, "News behavior registered");
        }

        public override void SyncData(IDataStore dataStore)
        {
            SaveLoadDiagnostics.SafeSyncData(this, dataStore, () =>
            {
                // Sync kingdom feed as individual primitives (lists of complex types require manual handling)
                _kingdomFeed ??= new List<DispatchItem>();
                var kingdomCount = _kingdomFeed.Count;
                dataStore.SyncData("en_news_kingdomCount", ref kingdomCount);

                if (dataStore.IsLoading)
                {
                    _kingdomFeed.Clear();
                    for (var i = 0; i < kingdomCount; i++)
                    {
                        var item = LoadDispatchItem(dataStore, $"en_news_k_{i}");
                        _kingdomFeed.Add(item);
                    }
                }
                else
                {
                    for (var i = 0; i < kingdomCount; i++)
                    {
                        var item = _kingdomFeed[i];
                        SaveDispatchItem(dataStore, $"en_news_k_{i}", item);
                    }
                }

                // Sync personal feed
                _personalFeed ??= new List<DispatchItem>();
                var personalCount = _personalFeed.Count;
                dataStore.SyncData("en_news_personalCount", ref personalCount);

                if (dataStore.IsLoading)
                {
                    _personalFeed.Clear();
                    for (var i = 0; i < personalCount; i++)
                    {
                        var item = LoadDispatchItem(dataStore, $"en_news_p_{i}");
                        _personalFeed.Add(item);
                    }
                }
                else
                {
                    for (var i = 0; i < personalCount; i++)
                    {
                        var item = _personalFeed[i];
                        SaveDispatchItem(dataStore, $"en_news_p_{i}", item);
                    }
                }

                // Sync bulletin tracking
                dataStore.SyncData("en_news_lastBulletinDay", ref _lastBulletinDayNumber);

                // Sync Daily Brief (once-per-day RP digest)
                // Ensure strings are never null before syncing (Bannerlord SyncData requires non-null refs)
                _dailyBriefCompany ??= string.Empty;
                _dailyBriefUnit ??= string.Empty;
                _dailyBriefKingdom ??= string.Empty;

                dataStore.SyncData("en_news_dailyBriefDay", ref _lastDailyBriefDayNumber);
                dataStore.SyncData("en_news_dailyBriefCompany", ref _dailyBriefCompany);
                dataStore.SyncData("en_news_dailyBriefUnit", ref _dailyBriefUnit);
                dataStore.SyncData("en_news_dailyBriefKingdom", ref _dailyBriefKingdom);
                
                // Battle aftermath tracking for daily brief flavor
                _lastPlayerBattleType ??= string.Empty;
                dataStore.SyncData("en_news_lastBattleTime", ref _lastPlayerBattleTime);
                dataStore.SyncData("en_news_lastBattleType", ref _lastPlayerBattleType);
                dataStore.SyncData("en_news_lastBattleWon", ref _lastPlayerBattleWon);

                // "Since last muster" counters for camp news display
                dataStore.SyncData("en_news_lostSinceMuster", ref _lostSinceLastMuster);
                dataStore.SyncData("en_news_sickSinceMuster", ref _sickSinceLastMuster);

                // Sync battle snapshots (for pyrrhic detection across save/load)
                _battleSnapshots ??= new Dictionary<string, BattleSnapshot>();
                var snapshotCount = _battleSnapshots.Count;
                dataStore.SyncData("en_news_snapshotCount", ref snapshotCount);

                if (dataStore.IsLoading)
                {
                    _battleSnapshots.Clear();
                    for (var i = 0; i < snapshotCount; i++)
                    {
                        var snapshot = LoadBattleSnapshot(dataStore, $"en_news_bs_{i}");
                        if (!string.IsNullOrEmpty(snapshot.MapEventId))
                        {
                            _battleSnapshots[snapshot.MapEventId] = snapshot;
                        }
                    }
                }
                else
                {
                    var snapshotList = _battleSnapshots.Values.ToList();
                    for (var i = 0; i < snapshotCount; i++)
                    {
                        SaveBattleSnapshot(dataStore, $"en_news_bs_{i}", snapshotList[i]);
                    }
                }

                // Sync army tracking state
                dataStore.SyncData("en_news_lordHadArmy", ref _lordHadArmyYesterday);

                // Persisted camp news state (Daily Report archive + ledger).
                _campNewsState ??= new CampNewsState();
                _campNewsState.SyncData(dataStore);

                // Sync order outcomes tracking
                _orderOutcomes ??= new List<OrderOutcomeRecord>();
                var orderOutcomesCount = _orderOutcomes.Count;
                dataStore.SyncData("en_news_orderOutcomesCount", ref orderOutcomesCount);

                if (dataStore.IsLoading)
                {
                    _orderOutcomes.Clear();
                    for (var i = 0; i < orderOutcomesCount; i++)
                    {
                        var record = LoadOrderOutcomeRecord(dataStore, $"en_news_order_{i}");
                        _orderOutcomes.Add(record);
                    }
                }
                else
                {
                    for (var i = 0; i < orderOutcomesCount; i++)
                    {
                        SaveOrderOutcomeRecord(dataStore, $"en_news_order_{i}", _orderOutcomes[i]);
                    }
                }

                // Sync reputation changes tracking
                _reputationChanges ??= new List<ReputationChangeRecord>();
                var reputationChangesCount = _reputationChanges.Count;
                dataStore.SyncData("en_news_reputationChangesCount", ref reputationChangesCount);

                if (dataStore.IsLoading)
                {
                    _reputationChanges.Clear();
                    for (var i = 0; i < reputationChangesCount; i++)
                    {
                        var record = LoadReputationChangeRecord(dataStore, $"en_news_rep_{i}");
                        _reputationChanges.Add(record);
                    }
                }
                else
                {
                    for (var i = 0; i < reputationChangesCount; i++)
                    {
                        SaveReputationChangeRecord(dataStore, $"en_news_rep_{i}", _reputationChanges[i]);
                    }
                }

                // Sync company need changes tracking
                _companyNeedChanges ??= new List<CompanyNeedChangeRecord>();
                var needChangesCount = _companyNeedChanges.Count;
                dataStore.SyncData("en_news_needChangesCount", ref needChangesCount);

                if (dataStore.IsLoading)
                {
                    _companyNeedChanges.Clear();
                    for (var i = 0; i < needChangesCount; i++)
                    {
                        var record = LoadCompanyNeedChangeRecord(dataStore, $"en_news_need_{i}");
                        _companyNeedChanges.Add(record);
                    }
                }
                else
                {
                    for (var i = 0; i < needChangesCount; i++)
                    {
                        SaveCompanyNeedChangeRecord(dataStore, $"en_news_need_{i}", _companyNeedChanges[i]);
                    }
                }

                // Sync event outcomes tracking
                _eventOutcomes ??= new List<EventOutcomeRecord>();
                var eventOutcomesCount = _eventOutcomes.Count;
                dataStore.SyncData("en_news_eventOutcomesCount", ref eventOutcomesCount);

                if (dataStore.IsLoading)
                {
                    _eventOutcomes.Clear();
                    for (var i = 0; i < eventOutcomesCount; i++)
                    {
                        var record = LoadEventOutcomeRecord(dataStore, $"en_news_evt_{i}");
                        _eventOutcomes.Add(record);
                    }
                }
                else
                {
                    for (var i = 0; i < eventOutcomesCount; i++)
                    {
                        SaveEventOutcomeRecord(dataStore, $"en_news_evt_{i}", _eventOutcomes[i]);
                    }
                }

                // Sync pending events tracking
                _pendingEvents ??= new List<PendingEventRecord>();
                var pendingEventsCount = _pendingEvents.Count;
                dataStore.SyncData("en_news_pendingEventsCount", ref pendingEventsCount);

                if (dataStore.IsLoading)
                {
                    _pendingEvents.Clear();
                    for (var i = 0; i < pendingEventsCount; i++)
                    {
                        var record = LoadPendingEventRecord(dataStore, $"en_news_pend_{i}");
                        _pendingEvents.Add(record);
                    }
                }
                else
                {
                    for (var i = 0; i < pendingEventsCount; i++)
                    {
                        SavePendingEventRecord(dataStore, $"en_news_pend_{i}", _pendingEvents[i]);
                    }
                }

                // Safe initialization for null collections after load
                _kingdomFeed ??= new List<DispatchItem>();
                _personalFeed ??= new List<DispatchItem>();
                _battleSnapshots ??= new Dictionary<string, BattleSnapshot>();
                _orderOutcomes ??= new List<OrderOutcomeRecord>();
                _reputationChanges ??= new List<ReputationChangeRecord>();
                _companyNeedChanges ??= new List<CompanyNeedChangeRecord>();
                _eventOutcomes ??= new List<EventOutcomeRecord>();
                _pendingEvents ??= new List<PendingEventRecord>();

                _dailyBriefCompany ??= string.Empty;
                _dailyBriefUnit ??= string.Empty;
                _dailyBriefKingdom ??= string.Empty;

                // Trim feeds to max size
                TrimFeeds();

                ModLogger.Debug(LogCategory, $"News data synced: kingdom={_kingdomFeed.Count}, personal={_personalFeed.Count}");
            });
        }

        #region Public API (Menu Integration)

        /// <summary>
        /// Builds the player status RP flavor line for use in menus.
        /// Returns tier-appropriate, context-aware text about the player's current condition.
        /// </summary>
        public string BuildPlayerStatusLine(EnlistmentBehavior enlistment)
        {
            return BuildDailyUnitLine(enlistment ?? EnlistmentBehavior.Instance);
        }

        /// <summary>
        /// Returns the last battle the player participated in (if any), for contextual menu flavor.
        /// </summary>
        public bool TryGetLastPlayerBattleSummary(out CampaignTime battleTime, out bool playerWon)
        {
            battleTime = _lastPlayerBattleTime;
            playerWon = _lastPlayerBattleWon;
            return battleTime != CampaignTime.Zero;
        }

        /// <summary>
        /// Builds the once-per-day Daily Brief as a single flowing RP narrative paragraph.
        /// Combines company situation, casualties, player status, and kingdom news into immersive text.
        /// </summary>
        public string BuildDailyBriefSection()
        {
            try
            {
                if (!IsEnlisted())
                {
                    return string.Empty;
                }

                EnsureDailyBriefGenerated();

                // If generation produced no content, hide the section
                if (string.IsNullOrWhiteSpace(_dailyBriefCompany) &&
                    string.IsNullOrWhiteSpace(_dailyBriefUnit) &&
                    string.IsNullOrWhiteSpace(_dailyBriefKingdom))
                {
                    return string.Empty;
                }

                // Build a flowing narrative paragraph instead of labeled lines
                var parts = new List<string>();
                
                if (!string.IsNullOrWhiteSpace(_dailyBriefCompany))
                {
                    parts.Add(_dailyBriefCompany);
                }

                // Add supply context when relevant (< 70%)
                var supplyContext = BuildSupplyContextLine();
                if (!string.IsNullOrWhiteSpace(supplyContext))
                {
                    parts.Add(supplyContext);
                }

                // Add casualty report with RP flavor (losses and wounded since last muster)
                var casualtyLine = BuildCasualtyReportLine();
                if (!string.IsNullOrWhiteSpace(casualtyLine))
                {
                    parts.Add(casualtyLine);
                }

                // Add recent event aftermath (events in last 24 hours)
                var recentEventLine = BuildRecentEventLine();
                if (!string.IsNullOrWhiteSpace(recentEventLine))
                {
                    parts.Add(recentEventLine);
                }

                // Add pending chain event hints
                var pendingLine = BuildPendingEventsLine();
                if (!string.IsNullOrWhiteSpace(pendingLine))
                {
                    parts.Add(pendingLine);
                }

                // Add flag-based personality context
                var flagContext = BuildFlagContextLine();
                if (!string.IsNullOrWhiteSpace(flagContext))
                {
                    parts.Add(flagContext);
                }

                // Add skill progress hint if any skill is close to level-up
                var skillProgress = BuildSkillProgressLine();
                if (!string.IsNullOrWhiteSpace(skillProgress))
                {
                    parts.Add(skillProgress);
                }
                    
                if (!string.IsNullOrWhiteSpace(_dailyBriefUnit))
                {
                    parts.Add(_dailyBriefUnit);
                }
                    
                if (!string.IsNullOrWhiteSpace(_dailyBriefKingdom))
                {
                    parts.Add(_dailyBriefKingdom);
                }

                // Join sentences into a flowing paragraph
                var paragraph = string.Join(" ", parts);
                
                return paragraph;
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, "Error building Daily Brief section", ex);
                return string.Empty;
            }
        }

        /// <summary>
        /// Builds an RP-flavored casualty report line for the daily brief.
        /// Shows losses and wounded since last muster in narrative style.
        /// </summary>
        private string BuildCasualtyReportLine()
        {
            try
            {
                var enlistment = EnlistmentBehavior.Instance;
                var lord = enlistment?.CurrentLord;
                var lordParty = lord?.PartyBelongedTo;
                
                var lostCount = _lostSinceLastMuster;
                var woundedCount = lordParty?.MemberRoster?.TotalWounded ?? 0;
                
                // No casualties to report
                if (lostCount <= 0 && woundedCount <= 0)
                {
                    return string.Empty;
                }

                // Heavy losses - somber tone
                if (lostCount >= 10)
                {
                    return $"The company has paid dearly — {lostCount} souls lost since last muster.";
                }
                
                // Moderate losses with wounded
                if (lostCount >= 5)
                {
                    if (woundedCount >= 10)
                    {
                        return $"Hard fighting has cost us {lostCount} dead and left {woundedCount} wounded.";
                    }
                    return $"We've lost {lostCount} good soldiers since the last muster.";
                }

                // Some losses
                if (lostCount >= 2)
                {
                    if (woundedCount >= 15)
                    {
                        return $"The wounded fill the medical tents. {lostCount} didn't make it.";
                    }
                    if (woundedCount >= 5)
                    {
                        return $"{lostCount} fallen, {woundedCount} wounded — the cost of the march.";
                    }
                    return $"{lostCount} soldiers have fallen since last muster.";
                }

                // Single loss
                if (lostCount == 1)
                {
                    if (woundedCount >= 10)
                    {
                        return $"One soldier lost, {woundedCount} nursing wounds.";
                    }
                    return "One of ours didn't make it through.";
                }

                // No deaths but significant wounded
                if (woundedCount >= 20)
                {
                    return $"The surgeons are busy — {woundedCount} wounded in the company.";
                }
                if (woundedCount >= 10)
                {
                    return $"{woundedCount} soldiers recovering from their wounds.";
                }
                if (woundedCount >= 5)
                {
                    return $"A few wounded among us, {woundedCount} in all.";
                }

                return string.Empty;
            }
            catch (Exception ex)
            {
                ModLogger.Debug(LogCategory, $"Error building casualty report: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Builds a supply context line for the Daily Brief.
        /// Shows warnings when supply levels are concerning.
        /// </summary>
        private string BuildSupplyContextLine()
        {
            try
            {
                var supply = EnlistmentBehavior.Instance?.CompanyNeeds?.Supplies ?? 100;

                if (supply < 30)
                {
                    return "The company is nearly out of supplies. Equipment changes are restricted.";
                }
                if (supply < 50)
                {
                    return "Supplies are running thin. The quartermaster looks worried.";
                }
                if (supply < 70)
                {
                    return "Supplies are holding, but careful management is needed.";
                }

                // Good supply levels don't need mention
                return string.Empty;
            }
            catch (Exception ex)
            {
                ModLogger.Debug(LogCategory, $"Error building supply context: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Builds a recent event aftermath line for the Daily Brief.
        /// Shows flavor text for events that fired in the last 24 hours.
        /// </summary>
        private string BuildRecentEventLine()
        {
            try
            {
                _eventOutcomes ??= new List<EventOutcomeRecord>();

                var currentDay = (int)CampaignTime.Now.ToDays;
                var recentEvents = _eventOutcomes
                    .Where(e => currentDay - e.DayNumber <= 1)
                    .OrderByDescending(e => e.DayNumber)
                    .Take(1)
                    .ToList();

                if (recentEvents.Count == 0)
                {
                    return string.Empty;
                }

                var recent = recentEvents[0];
                var eventId = recent.EventId?.ToLowerInvariant() ?? string.Empty;

                // Dice/gambling aftermath
                if (eventId.Contains("dice") || eventId.Contains("gambling") || eventId.Contains("wager"))
                {
                    return "The men still talk about your dice game.";
                }

                // Training aftermath
                if (eventId.Contains("training") || eventId.Contains("drill") || eventId.Contains("practice"))
                {
                    return "Yesterday's training session left your arms sore, but your skills improved.";
                }

                // Hunt aftermath
                if (eventId.Contains("hunt"))
                {
                    return "Fresh game from the hunt has improved everyone's mood.";
                }

                // Loan aftermath
                if (eventId.Contains("lend") || eventId.Contains("loan"))
                {
                    if (eventId.Contains("repay") || eventId.Contains("return"))
                    {
                        return string.Empty; // Repayment doesn't need aftermath
                    }
                    return "A comrade owes you a debt.";
                }

                // Tavern aftermath
                if (eventId.Contains("tavern") || eventId.Contains("drinking"))
                {
                    return "Last night's drinking still echoes in your head.";
                }

                // Battle loot aftermath
                if (eventId.Contains("loot") || eventId.Contains("battlefield"))
                {
                    return "The spoils of battle weigh on your thoughts.";
                }

                return string.Empty;
            }
            catch (Exception ex)
            {
                ModLogger.Debug(LogCategory, $"Error building recent event line: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Builds a pending chain events line for the Daily Brief.
        /// Shows context hints for upcoming events.
        /// </summary>
        private string BuildPendingEventsLine()
        {
            try
            {
                _pendingEvents ??= new List<PendingEventRecord>();

                if (_pendingEvents.Count == 0)
                {
                    return string.Empty;
                }

                var currentDay = (int)CampaignTime.Now.ToDays;
                var lines = new List<string>();

                foreach (var pending in _pendingEvents)
                {
                    var daysRemaining = pending.ScheduledDay - currentDay;
                    var chainId = pending.ChainEventId?.ToLowerInvariant() ?? string.Empty;

                    // Skip stale pending events (more than 7 days overdue)
                    if (daysRemaining < -7)
                    {
                        continue;
                    }

                    // Loan repayment pending
                    if (chainId.Contains("repay") || chainId.Contains("return") || chainId.Contains("debt"))
                    {
                        if (daysRemaining <= 1)
                        {
                            lines.Add("A comrade promised to repay you today.");
                        }
                        else
                        {
                            // Clamp to minimum 1 to avoid negative or zero display
                            var daysSince = Math.Max(1, currentDay - pending.CreatedDay);
                            lines.Add($"A comrade owes you money. It's been {daysSince} days.");
                        }
                    }
                    // Gratitude pending
                    else if (chainId.Contains("gratitude") || chainId.Contains("thank") || chainId.Contains("favor"))
                    {
                        lines.Add("Someone remembers your kindness.");
                    }
                    // Revenge/grudge pending
                    else if (chainId.Contains("revenge") || chainId.Contains("grudge"))
                    {
                        lines.Add("Someone holds a grudge against you.");
                    }
                    // Generic hint from the record
                    else if (!string.IsNullOrEmpty(pending.ContextHint))
                    {
                        lines.Add(pending.ContextHint);
                    }
                }

                // Return only the most relevant (first) to avoid spam
                return lines.Count > 0 ? lines[0] : string.Empty;
            }
            catch (Exception ex)
            {
                ModLogger.Debug(LogCategory, $"Error building pending events line: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Builds a flag-based context line for the Daily Brief.
        /// Shows personality hints based on active escalation flags.
        /// </summary>
        private string BuildFlagContextLine()
        {
            try
            {
                var state = EscalationManager.Instance?.State;
                if (state == null)
                {
                    return string.Empty;
                }

                var lines = new List<string>();

                if (state.HasFlag("has_helped_comrade") || state.HasFlag("helped_comrade"))
                {
                    lines.Add("You're known for helping your comrades.");
                }
                if (state.HasFlag("dice_winner") || state.HasFlag("gambling_winner"))
                {
                    lines.Add("Your luck at dice is remembered.");
                }
                if (state.HasFlag("shared_winnings") || state.HasFlag("generous"))
                {
                    lines.Add("The men appreciate your generosity.");
                }
                if (state.HasFlag("officer_attention") || state.HasFlag("noticed_by_officers"))
                {
                    lines.Add("Officers have taken notice of you lately.");
                }
                if (state.HasFlag("training_focused") || state.HasFlag("dedicated_training"))
                {
                    lines.Add("Your dedication to training has been noted.");
                }
                if (state.HasFlag("good_hunter") || state.HasFlag("skilled_hunter"))
                {
                    lines.Add("Your hunting skills are well regarded.");
                }
                if (state.HasFlag("drinks_with_soldiers") || state.HasFlag("tavern_regular"))
                {
                    lines.Add("The soldiers enjoy drinking with you.");
                }

                // Return one random context to avoid spam and add variety
                if (lines.Count > 0)
                {
                    var index = MBRandom.RandomInt(lines.Count);
                    return lines[index];
                }

                return string.Empty;
            }
            catch (Exception ex)
            {
                ModLogger.Debug(LogCategory, $"Error building flag context line: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Checks main combat skills for level-up progress and returns a hint if any skill is within 20% of leveling.
        /// Results are cached for 6 hours to avoid expensive recalculation.
        /// </summary>
        private string BuildSkillProgressLine()
        {
            try
            {
                // Use cached result if checked within last 6 hours
                var hoursSinceCheck = CampaignTime.Now.ToHours - _lastSkillProgressCheck.ToHours;
                if (hoursSinceCheck < 6.0f && !string.IsNullOrEmpty(_cachedSkillProgressLine))
                {
                    return _cachedSkillProgressLine;
                }

                var hero = Hero.MainHero;
                if (hero?.HeroDeveloper == null)
                {
                    _lastSkillProgressCheck = CampaignTime.Now;
                    _cachedSkillProgressLine = string.Empty;
                    return string.Empty;
                }

                var model = Campaign.Current?.Models?.CharacterDevelopmentModel;
                if (model == null)
                {
                    _lastSkillProgressCheck = CampaignTime.Now;
                    _cachedSkillProgressLine = string.Empty;
                    return string.Empty;
                }

                // Check main combat skills for level-up progress
                var combatSkills = new[]
                {
                    DefaultSkills.OneHanded,
                    DefaultSkills.TwoHanded,
                    DefaultSkills.Polearm,
                    DefaultSkills.Bow,
                    DefaultSkills.Crossbow
                };

                foreach (var skill in combatSkills)
                {
                    try
                    {
                        var skillValue = hero.GetSkillValue(skill);
                        
                        // Skip skills at max level (330)
                        if (skillValue >= 330)
                        {
                            continue;
                        }

                        var xpProgress = hero.HeroDeveloper.GetSkillXpProgress(skill);
                        var xpNeeded = model.GetXpRequiredForSkillLevel(skillValue + 1) - model.GetXpRequiredForSkillLevel(skillValue);

                        // Skip if XP calculation is invalid (shouldn't happen in vanilla, but guards against mod edge cases)
                        if (xpNeeded <= 0)
                        {
                            continue;
                        }

                        // Check if within 20% of next level
                        if (xpProgress > xpNeeded * 0.8f)
                        {
                            _lastSkillProgressCheck = CampaignTime.Now;
                            _cachedSkillProgressLine = $"Your {skill.Name.ToString()} skill is nearly ready to advance.";
                            return _cachedSkillProgressLine;
                        }
                    }
                    catch (Exception ex)
                    {
                        ModLogger.Debug(LogCategory, $"Error checking skill progress for {(skill != null ? skill.Name.ToString() : "unknown")}: {ex.Message}");
                        continue;
                    }
                }

                // No skills close to level-up
                _lastSkillProgressCheck = CampaignTime.Now;
                _cachedSkillProgressLine = string.Empty;
                return string.Empty;
            }
            catch (Exception ex)
            {
                ModLogger.Debug(LogCategory, $"Error building skill progress line: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Returns the current "visible" kingdom dispatch items using the same rules as the enlisted_status menu.
        /// This is useful for other UIs (e.g. Camp Bulletin) that want the same cadence and dedupe behavior.
        /// </summary>
        public List<DispatchItem> GetVisibleKingdomFeedItems(int maxItems = 3)
        {
            try
            {
                if (_kingdomFeed == null || _kingdomFeed.Count == 0)
                {
                    return new List<DispatchItem>();
                }

                var currentDay = (int)CampaignTime.Now.ToDays;
                var candidates = _kingdomFeed
                    .Where(x =>
                        x.FirstShownDay < 0 ||
                        currentDay - x.FirstShownDay < Math.Max(1, x.MinDisplayDays))
                    .OrderByDescending(x => x.FirstShownDay >= 0 && currentDay - x.FirstShownDay < Math.Max(1, x.MinDisplayDays)) // sticky
                    .ThenByDescending(x => Math.Max(1, x.MinDisplayDays)) // important first
                    .ThenByDescending(x => x.DayCreated)
                    .ToList();

                var recentItems = candidates.Take(maxItems).ToList();

                // Mark newly-shown items so they remain visible for their minimum display window.
                for (var i = 0; i < recentItems.Count; i++)
                {
                    var item = recentItems[i];
                    if (item.FirstShownDay < 0 && !string.IsNullOrEmpty(item.StoryKey))
                    {
                        var idx = _kingdomFeed.FindIndex(x => x.StoryKey == item.StoryKey);
                        if (idx >= 0)
                        {
                            var updated = _kingdomFeed[idx];
                            updated.FirstShownDay = currentDay;
                            _kingdomFeed[idx] = updated;
                        }
                    }
                }

                return recentItems;
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, "Error selecting visible kingdom feed items", ex);
                return new List<DispatchItem>();
            }
        }

        /// <summary>
        /// Returns the current "visible" personal dispatch items using the same rules as the enlisted_activities menu.
        /// </summary>
        public List<DispatchItem> GetVisiblePersonalFeedItems(int maxItems = 2)
        {
            try
            {
                if (_personalFeed == null || _personalFeed.Count == 0)
                {
                    return new List<DispatchItem>();
                }

                var currentDay = (int)CampaignTime.Now.ToDays;
                var candidates = _personalFeed
                    .Where(x =>
                        x.FirstShownDay < 0 ||
                        currentDay - x.FirstShownDay < Math.Max(1, x.MinDisplayDays))
                    .OrderByDescending(x => x.FirstShownDay >= 0 && currentDay - x.FirstShownDay < Math.Max(1, x.MinDisplayDays)) // sticky
                    .ThenByDescending(x => Math.Max(1, x.MinDisplayDays))
                    .ThenByDescending(x => x.DayCreated)
                    .ToList();

                var recentItems = candidates.Take(maxItems).ToList();

                // Mark newly-shown items so they remain visible for their minimum display window.
                for (var i = 0; i < recentItems.Count; i++)
                {
                    var item = recentItems[i];
                    if (item.FirstShownDay < 0 && !string.IsNullOrEmpty(item.StoryKey))
                    {
                        var idx = _personalFeed.FindIndex(x => x.StoryKey == item.StoryKey);
                        if (idx >= 0)
                        {
                            var updated = _personalFeed[idx];
                            updated.FirstShownDay = currentDay;
                            _personalFeed[idx] = updated;
                        }
                    }
                }

                return recentItems;
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, "Error selecting visible personal feed items", ex);
                return new List<DispatchItem>();
            }
        }

        /// <summary>
        /// Formats a dispatch item into display text using the same localization/template rules used by the menus.
        /// </summary>
        public static string FormatDispatchForDisplay(DispatchItem item)
        {
            return FormatDispatchItem(item);
        }

        /// <summary>
        /// Builds the kingdom news section for display in enlisted_status menu.
        /// </summary>
        /// <param name="maxItems">Maximum headlines to display (default 3).</param>
        /// <returns>Formatted news section text, or empty string if no news.</returns>
        public string BuildKingdomNewsSection(int maxItems = 3)
        {
            try
            {
                if (_kingdomFeed == null || _kingdomFeed.Count == 0)
                {
                    return string.Empty;
                }

                // Select which items are "currently visible" based on priority and backlog rules. Every item stays
                // visible for at least one day once shown. Important items stay visible for two days. An item can be
                // replaced by a newer update with the same StoryKey.
                var currentDay = (int)CampaignTime.Now.ToDays;
                var candidates = _kingdomFeed
                    .Where(x =>
                        x.FirstShownDay < 0 ||
                        currentDay - x.FirstShownDay < Math.Max(1, x.MinDisplayDays))
                    .OrderByDescending(x => x.FirstShownDay >= 0 && currentDay - x.FirstShownDay < Math.Max(1, x.MinDisplayDays)) // sticky
                    .ThenByDescending(x => Math.Max(1, x.MinDisplayDays)) // important first
                    .ThenByDescending(x => x.DayCreated)
                    .ToList();

                var recentItems = candidates.Take(maxItems).ToList();

                if (recentItems.Count == 0)
                {
                    return string.Empty;
                }

                // Mark newly-shown items so they remain visible for their minimum display window.
                for (var i = 0; i < recentItems.Count; i++)
                {
                    var item = recentItems[i];
                    if (item.FirstShownDay < 0 && !string.IsNullOrEmpty(item.StoryKey))
                    {
                        var idx = _kingdomFeed.FindIndex(x => x.StoryKey == item.StoryKey);
                        if (idx >= 0)
                        {
                            var updated = _kingdomFeed[idx];
                            updated.FirstShownDay = currentDay;
                            _kingdomFeed[idx] = updated;
                        }
                    }
                }

                var sb = new StringBuilder();
                sb.AppendLine();
                sb.AppendLine(new TextObject("{=News_SectionHeader_Kingdom}--- Kingdom News ---").ToString());

                foreach (var item in recentItems)
                {
                    var headline = FormatDispatchItem(item);
                    if (!string.IsNullOrEmpty(headline))
                    {
                        sb.AppendLine($"- {headline}");
                    }
                }

                return sb.ToString();
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, "Error building kingdom news section", ex);
                return string.Empty;
            }
        }

        /// <summary>
        /// Builds the personal news section for display in enlisted_activities menu.
        /// </summary>
        /// <param name="maxItems">Maximum headlines to display (default 2).</param>
        /// <returns>Formatted news section text, or empty string if no news.</returns>
        public string BuildPersonalNewsSection(int maxItems = 2)
        {
            try
            {
                if (_personalFeed == null || _personalFeed.Count == 0)
                {
                    return string.Empty;
                }

                var currentDay = (int)CampaignTime.Now.ToDays;
                var candidates = _personalFeed
                    .Where(x =>
                        x.FirstShownDay < 0 ||
                        currentDay - x.FirstShownDay < Math.Max(1, x.MinDisplayDays))
                    .OrderByDescending(x => x.FirstShownDay >= 0 && currentDay - x.FirstShownDay < Math.Max(1, x.MinDisplayDays)) // sticky
                    .ThenByDescending(x => Math.Max(1, x.MinDisplayDays))
                    .ThenByDescending(x => x.DayCreated)
                    .ToList();

                var recentItems = candidates.Take(maxItems).ToList();

                if (recentItems.Count == 0)
                {
                    return string.Empty;
                }

                // Mark newly-shown items.
                for (var i = 0; i < recentItems.Count; i++)
                {
                    var item = recentItems[i];
                    if (item.FirstShownDay < 0 && !string.IsNullOrEmpty(item.StoryKey))
                    {
                        var idx = _personalFeed.FindIndex(x => x.StoryKey == item.StoryKey);
                        if (idx >= 0)
                        {
                            var updated = _personalFeed[idx];
                            updated.FirstShownDay = currentDay;
                            _personalFeed[idx] = updated;
                        }
                    }
                }

                var sb = new StringBuilder();
                sb.AppendLine(new TextObject("{=News_SectionHeader_Personal}--- Army Orders ---").ToString());

                foreach (var item in recentItems)
                {
                    var headline = FormatDispatchItem(item);
                    if (!string.IsNullOrEmpty(headline))
                    {
                        sb.AppendLine($"- {headline}");
                    }
                }

                sb.AppendLine(); // Extra line before camp activities

                return sb.ToString();
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, "Error building personal news section", ex);
                return string.Empty;
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Daily tick: check for bulletin generation and army formation detection.
        /// </summary>
        private void OnDailyTick()
        {
            try
            {
                if (!IsEnlisted())
                {
                    // Reset army tracking when not enlisted
                    _lordHadArmyYesterday = false;
                    return;
                }

                // Generate the daily brief once per day while enlisted so the main menu can display it without jitter.
                EnsureDailyBriefGenerated();

                // Generate the Daily Report once per in-game day (persisted, stable).
                EnsureDailyReportGenerated();

                // Clean up stale pending events that never fired
                CleanupStalePendingEvents();

                CheckForArmyFormation();
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, "Error in OnDailyTick", ex);
            }
        }

        /// <summary>
        /// Removes pending events that are more than 7 days overdue.
        /// Handles cases where chain events were scheduled but never fired
        /// (e.g., event definition removed, mod conflict, or game state issue).
        /// </summary>
        private void CleanupStalePendingEvents()
        {
            try
            {
                _pendingEvents ??= new List<PendingEventRecord>();

                if (_pendingEvents.Count == 0)
                {
                    return;
                }

                var currentDay = (int)CampaignTime.Now.ToDays;
                var staleThreshold = 7; // Remove events more than 7 days overdue

                var removed = _pendingEvents.RemoveAll(p => currentDay - p.ScheduledDay > staleThreshold);

                if (removed > 0)
                {
                    ModLogger.Info(LogCategory, $"Cleaned up {removed} stale pending event(s)");
                }
            }
            catch (Exception ex)
            {
                ModLogger.Debug(LogCategory, $"Error cleaning up stale pending events: {ex.Message}");
            }
        }

        /// <summary>
        /// Ensure we have a persisted Daily Report for the current day.
        /// </summary>
        private void EnsureDailyReportGenerated()
        {
            try
            {
                var dayNumber = (int)CampaignTime.Now.ToDays;

                _campNewsState ??= new CampNewsState();
                _campNewsState.EnsureInitialized();

                if (_campNewsState.LastGeneratedDayNumber == dayNumber &&
                    _campNewsState.TryGetReportForDay(dayNumber) != null)
                {
                    return;
                }

                var enlistment = EnlistmentBehavior.Instance;
                if (enlistment?.IsEnlisted != true)
                {
                    _campNewsState.MarkGenerated(dayNumber);
                    return;
                }

                var snapshot = BuildDailyReportSnapshot(enlistment, dayNumber, out var aggregate, out var context);

                // Cache snapshot for other systems that need day facts.
                _lastDailyReportSnapshot = snapshot;

                // Generate final report lines (templated strings) and persist them.
                var lines = DailyReportGenerator.Generate(snapshot, context, maxLines: 8);

                var record = new DailyReportRecord
                {
                    HasValue = true,
                    DayNumber = dayNumber,
                    Lines = lines?.ToArray() ?? Array.Empty<string>()
                };

                _campNewsState.AppendReport(record);
                _campNewsState.MarkGenerated(dayNumber);

                // Update ledger (best-effort; mostly zero until later producers).
                if (aggregate.HasValue)
                {
                    _campNewsState.Ledger.RecordDay(aggregate);
                }

                ModLogger.Debug(LogCategory, $"Daily Report generated for day {dayNumber} (lines={record.Lines.Length})");
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, "EnsureDailyReportGenerated failed", ex);
            }
        }

        /// <summary>
        /// Provides the current day's Daily Report snapshot (facts), ensuring it exists first.
        /// Intended for systems that need the same "day facts" without duplicating producer logic.
        /// </summary>
        public DailyReportSnapshot GetTodayDailyReportSnapshot()
        {
            try
            {
                EnsureDailyReportGenerated();
                return _lastDailyReportSnapshot;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Returns rolling totals from the camp life ledger for the specified window.
        /// Useful for displaying recent losses, wounded, sick over time.
        /// </summary>
        public CampLifeRollingTotals GetRollingTotals(int windowDays = 7)
        {
            try
            {
                _campNewsState?.EnsureInitialized();
                return _campNewsState?.Ledger?.GetRollingTotals(windowDays) 
                       ?? new CampLifeRollingTotals(0, 0, 0, 0, 0, 0);
            }
            catch
            {
                return new CampLifeRollingTotals(0, 0, 0, 0, 0, 0);
            }
        }

        /// <summary>
        /// Soldiers lost since the last muster cycle. Resets when muster completes.
        /// </summary>
        public int LostSinceLastMuster => _lostSinceLastMuster;

        /// <summary>
        /// Soldiers who fell sick since the last muster cycle. Resets when muster completes.
        /// </summary>
        public int SickSinceLastMuster => _sickSinceLastMuster;

        /// <summary>
        /// Resets the "since last muster" counters. Called by EnlistmentBehavior.OnMusterCycleComplete().
        /// </summary>
        public void ResetMusterCounters()
        {
            _lostSinceLastMuster = 0;
            _sickSinceLastMuster = 0;
            ModLogger.Debug(LogCategory, "Muster counters reset");
        }

        /// <summary>
        /// Records an order outcome for display in daily brief and detailed reports.
        /// </summary>
        public void AddOrderOutcome(string orderTitle, bool success, string briefSummary,
            string detailedSummary, string issuer, int dayNumber)
        {
            try
            {
                _orderOutcomes ??= new List<OrderOutcomeRecord>();
                
                _orderOutcomes.Add(new OrderOutcomeRecord
                {
                    OrderTitle = orderTitle ?? string.Empty,
                    Success = success,
                    BriefSummary = briefSummary ?? string.Empty,
                    DetailedSummary = detailedSummary ?? string.Empty,
                    Issuer = issuer ?? string.Empty,
                    DayNumber = dayNumber
                });

                // Keep only last 10 order outcomes
                if (_orderOutcomes.Count > 10)
                {
                    _orderOutcomes.RemoveAt(0);
                }

                ModLogger.Debug(LogCategory, $"Order outcome recorded: {orderTitle} (success={success}, day={dayNumber})");
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, "Failed to add order outcome", ex);
            }
        }

        /// <summary>
        /// Records a reputation change for display in reports.
        /// </summary>
        public void AddReputationChange(string target, int delta, int newValue,
            string message, int dayNumber)
        {
            try
            {
                _reputationChanges ??= new List<ReputationChangeRecord>();
                
                _reputationChanges.Add(new ReputationChangeRecord
                {
                    Target = target ?? string.Empty,
                    Delta = delta,
                    NewValue = newValue,
                    Message = message ?? string.Empty,
                    DayNumber = dayNumber
                });

                // Keep only last 10 reputation changes
                if (_reputationChanges.Count > 10)
                {
                    _reputationChanges.RemoveAt(0);
                }

                ModLogger.Debug(LogCategory, $"Reputation change recorded: {target} {delta:+#;-#;0} (day={dayNumber})");
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, "Failed to add reputation change", ex);
            }
        }

        /// <summary>
        /// Records a company need change for display in reports.
        /// </summary>
        public void AddCompanyNeedChange(string need, int delta, int oldValue, int newValue,
            string message, int dayNumber)
        {
            try
            {
                _companyNeedChanges ??= new List<CompanyNeedChangeRecord>();
                
                _companyNeedChanges.Add(new CompanyNeedChangeRecord
                {
                    Need = need ?? string.Empty,
                    Delta = delta,
                    OldValue = oldValue,
                    NewValue = newValue,
                    Message = message ?? string.Empty,
                    DayNumber = dayNumber
                });

                // Keep only last 10 need changes
                if (_companyNeedChanges.Count > 10)
                {
                    _companyNeedChanges.RemoveAt(0);
                }

                ModLogger.Debug(LogCategory, $"Company need change recorded: {need} {delta:+#;-#;0} (day={dayNumber})");
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, "Failed to add company need change", ex);
            }
        }

        /// <summary>
        /// Returns recent order outcomes within the specified number of days.
        /// </summary>
        public List<OrderOutcomeRecord> GetRecentOrderOutcomes(int maxDaysOld = 3)
        {
            try
            {
                _orderOutcomes ??= new List<OrderOutcomeRecord>();
                
                var currentDay = (int)CampaignTime.Now.ToDays;
                return _orderOutcomes
                    .Where(o => currentDay - o.DayNumber <= maxDaysOld)
                    .OrderByDescending(o => o.DayNumber)
                    .ToList();
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, "Failed to get recent order outcomes", ex);
                return new List<OrderOutcomeRecord>();
            }
        }

        /// <summary>
        /// Returns recent reputation changes within the specified number of days.
        /// </summary>
        public List<ReputationChangeRecord> GetRecentReputationChanges(int maxDaysOld = 3)
        {
            try
            {
                _reputationChanges ??= new List<ReputationChangeRecord>();
                
                var currentDay = (int)CampaignTime.Now.ToDays;
                return _reputationChanges
                    .Where(r => currentDay - r.DayNumber <= maxDaysOld)
                    .OrderByDescending(r => r.DayNumber)
                    .ToList();
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, "Failed to get recent reputation changes", ex);
                return new List<ReputationChangeRecord>();
            }
        }

        /// <summary>
        /// Returns recent company need changes within the specified number of days.
        /// </summary>
        public List<CompanyNeedChangeRecord> GetRecentCompanyNeedChanges(int maxDaysOld = 3)
        {
            try
            {
                _companyNeedChanges ??= new List<CompanyNeedChangeRecord>();
                
                var currentDay = (int)CampaignTime.Now.ToDays;
                return _companyNeedChanges
                    .Where(c => currentDay - c.DayNumber <= maxDaysOld)
                    .OrderByDescending(c => c.DayNumber)
                    .ToList();
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, "Failed to get recent company need changes", ex);
                return new List<CompanyNeedChangeRecord>();
            }
        }

        /// <summary>
        /// Records an event outcome after an event popup is resolved.
        /// Adds to Personal Feed with a formatted headline showing effects.
        /// </summary>
        public void AddEventOutcome(EventOutcomeRecord outcome)
        {
            try
            {
                if (outcome == null)
                {
                    ModLogger.Warn(LogCategory, "Attempted to add null event outcome");
                    return;
                }

                _eventOutcomes ??= new List<EventOutcomeRecord>();

                // Check for duplicate (same event on same day)
                var existingIndex = _eventOutcomes.FindIndex(e =>
                    e.EventId == outcome.EventId && e.DayNumber == outcome.DayNumber);
                if (existingIndex >= 0)
                {
                    ModLogger.Debug(LogCategory, $"Event outcome already recorded: {outcome.EventId} day {outcome.DayNumber}");
                    return;
                }

                _eventOutcomes.Add(outcome);

                // Keep only last 20 event outcomes
                if (_eventOutcomes.Count > 20)
                {
                    _eventOutcomes.RemoveAt(0);
                }

                // Add to personal feed with formatted headline
                var headline = BuildEventHeadline(outcome);
                var placeholders = new Dictionary<string, string>
                {
                    { "EVENT_TITLE", outcome.EventTitle ?? outcome.EventId },
                    { "OPTION", outcome.OptionChosen ?? string.Empty },
                    { "EFFECTS", outcome.OutcomeSummary ?? string.Empty }
                };

                // Replace placeholders in headline
                foreach (var kvp in placeholders)
                {
                    headline = headline.Replace($"{{{kvp.Key}}}", kvp.Value);
                }

                AddPersonalNews("event", headline, placeholders, $"event:{outcome.EventId}:{outcome.DayNumber}", 2);

                ModLogger.Info(LogCategory, $"Event outcome recorded: {outcome.EventId} - {outcome.OptionChosen}");
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, "Failed to add event outcome", ex);
            }
        }

        /// <summary>
        /// Schedules a pending chain event for context display in Daily Brief.
        /// Shows hints like "A comrade owes you money" before the follow-up fires.
        /// </summary>
        public void AddPendingEvent(string sourceId, string chainId, string hint, int delayDays)
        {
            try
            {
                if (string.IsNullOrEmpty(chainId))
                {
                    return;
                }

                // Validate delay is at least 1 day
                if (delayDays < 1)
                {
                    ModLogger.Debug(LogCategory, $"Pending event skipped - delay too short: {chainId} ({delayDays} days)");
                    return;
                }

                _pendingEvents ??= new List<PendingEventRecord>();

                // Remove any existing pending event with the same chain ID
                _pendingEvents.RemoveAll(p => p.ChainEventId == chainId);

                var currentDay = (int)CampaignTime.Now.ToDays;
                _pendingEvents.Add(new PendingEventRecord
                {
                    SourceEventId = sourceId ?? string.Empty,
                    ChainEventId = chainId,
                    ContextHint = hint ?? string.Empty,
                    CreatedDay = currentDay,
                    ScheduledDay = currentDay + delayDays
                });

                // Capacity limit: keep only the 10 most recent pending events
                if (_pendingEvents.Count > 10)
                {
                    // Remove oldest (first added) events
                    _pendingEvents.RemoveAt(0);
                    ModLogger.Debug(LogCategory, "Pending events trimmed to capacity limit of 10");
                }

                ModLogger.Debug(LogCategory, $"Pending event added: {chainId} scheduled for day {currentDay + delayDays}");
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, "Failed to add pending event", ex);
            }
        }

        /// <summary>
        /// Clears a pending chain event when it fires.
        /// Called when the chain event is delivered.
        /// </summary>
        public void ClearPendingEvent(string chainEventId)
        {
            try
            {
                if (string.IsNullOrEmpty(chainEventId))
                {
                    return;
                }

                _pendingEvents ??= new List<PendingEventRecord>();
                var removed = _pendingEvents.RemoveAll(p => p.ChainEventId == chainEventId);

                if (removed > 0)
                {
                    ModLogger.Debug(LogCategory, $"Pending event cleared: {chainEventId}");
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, "Failed to clear pending event", ex);
            }
        }

        /// <summary>
        /// Records a muster outcome for camp news display.
        /// Called at the end of each muster cycle to summarize pay, rations, and unit status.
        /// </summary>
        public void AddMusterOutcome(MusterOutcomeRecord record)
        {
            try
            {
                if (record == null)
                {
                    return;
                }

                _musterOutcomes ??= new List<MusterOutcomeRecord>();
                _musterOutcomes.Add(record);

                // Keep only the last 5 muster records (roughly 2 months of game time)
                while (_musterOutcomes.Count > 5)
                {
                    _musterOutcomes.RemoveAt(0);
                }

                ModLogger.Debug(LogCategory, $"Muster outcome recorded: pay={record.PayOutcome}, ration={record.RationOutcome}, supply={record.SupplyLevel}%");

                // Add to personal feed with formatted summary
                var headline = BuildMusterHeadline(record);
                if (!string.IsNullOrEmpty(headline))
                {
                    AddToPersonalFeed(headline, record.DayNumber);
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, "Failed to add muster outcome", ex);
            }
        }

        /// <summary>
        /// Returns the most recent muster outcome, or null if none recorded.
        /// </summary>
        public MusterOutcomeRecord GetLastMusterOutcome()
        {
            try
            {
                _musterOutcomes ??= new List<MusterOutcomeRecord>();
                return _musterOutcomes.Count > 0 ? _musterOutcomes[_musterOutcomes.Count - 1] : null;
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, "Failed to get last muster outcome", ex);
                return null;
            }
        }

        /// <summary>
        /// Returns a formatted muster summary for display in menus.
        /// Uses straightforward military language appropriate for the setting.
        /// </summary>
        public string GetLastMusterSummary()
        {
            try
            {
                var record = GetLastMusterOutcome();
                if (record == null)
                {
                    return "No muster on record.";
                }

                var sb = new StringBuilder();
                sb.AppendLine($"Last Muster (Day {record.DayNumber})");

                // Pay line
                var payText = record.PayOutcome switch
                {
                    "paid" => $"Pay: {record.PayAmount} denars received",
                    "partial" => $"Pay: {record.PayAmount} denars (partial, backpay still owed)",
                    "delayed" => "Pay: Delayed — lord short on funds",
                    "promissory" => $"Pay: Promissory note for {record.PayAmount} denars",
                    "corruption" => $"Pay: {record.PayAmount} denars (with certain arrangements)",
                    "side_deal" => $"Pay: {record.PayAmount} denars (side deal)",
                    _ => $"Pay: {record.PayOutcome}"
                };
                sb.AppendLine(payText);

                // Ration line
                var rationText = record.RationOutcome switch
                {
                    "issued" => $"Rations: {GetRationDisplayName(record.RationItemId)} issued",
                    "none_low_supply" => "Rations: None issued — supplies too low",
                    "none_critical" => "Rations: None issued — supplies critically low",
                    "officer_exempt" => "Rations: Not applicable (officer provision)",
                    "commander_exempt" => "Rations: Not applicable (commander provision)",
                    _ => $"Rations: {record.RationOutcome}"
                };
                sb.AppendLine(rationText);

                // Supply status
                var supplyStatus = record.SupplyLevel switch
                {
                    >= 80 => "well-stocked",
                    >= 60 => "adequate",
                    >= 40 => "limited",
                    >= 30 => "low",
                    _ => "critical"
                };
                sb.AppendLine($"Supplies: {record.SupplyLevel}% ({supplyStatus})");

                // Unit status (only if there were losses or sick)
                if (record.LostSinceLast > 0 || record.SickSinceLast > 0)
                {
                    var unitParts = new List<string>();
                    if (record.LostSinceLast > 0)
                    {
                        unitParts.Add($"{record.LostSinceLast} lost");
                    }
                    if (record.SickSinceLast > 0)
                    {
                        unitParts.Add($"{record.SickSinceLast} sick");
                    }
                    sb.AppendLine($"Unit: {string.Join(", ", unitParts)} since last muster");
                }

                return sb.ToString().TrimEnd();
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, "Failed to get muster summary", ex);
                return "Muster records unavailable.";
            }
        }

        /// <summary>
        /// Builds a headline for the personal feed from a muster outcome.
        /// </summary>
        private string BuildMusterHeadline(MusterOutcomeRecord record)
        {
            try
            {
                // Lead with the most notable aspect of the muster
                if (record.PayOutcome == "delayed")
                {
                    return "Muster: Pay delayed this cycle.";
                }
                if (record.RationOutcome == "none_critical")
                {
                    return "Muster: No rations — supplies critically low.";
                }
                if (record.RationOutcome == "none_low_supply")
                {
                    return "Muster: No rations issued due to supply shortage.";
                }
                if (record.LostSinceLast >= 5)
                {
                    return $"Muster: Heavy losses this cycle — {record.LostSinceLast} men gone.";
                }
                if (record.PayOutcome == "corruption" || record.PayOutcome == "side_deal")
                {
                    return $"Muster: Pay collected with certain arrangements.";
                }

                // Standard muster
                if (record.RationOutcome == "issued" && !string.IsNullOrEmpty(record.RationItemId))
                {
                    return $"Muster: {record.PayAmount} denars paid, {GetRationDisplayName(record.RationItemId)} issued.";
                }

                return $"Muster: {record.PayAmount} denars paid.";
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, "Failed to build muster headline", ex);
                return "Muster completed.";
            }
        }

        /// <summary>
        /// Converts a ration item ID to a display-friendly name.
        /// </summary>
        private static string GetRationDisplayName(string itemId)
        {
            return itemId?.ToLowerInvariant() switch
            {
                "meat" => "meat ration",
                "cheese" => "cheese ration",
                "butter" => "butter ration",
                "grain" => "grain ration",
                _ => itemId ?? "ration"
            };
        }

        /// <summary>
        /// Adds an item to the personal feed with the specified headline and day.
        /// Uses a direct headline key for muster-related messages.
        /// </summary>
        private void AddToPersonalFeed(string headline, int dayNumber)
        {
            try
            {
                _personalFeed ??= new List<DispatchItem>();
                _personalFeed.Add(new DispatchItem
                {
                    HeadlineKey = headline, // Direct text for muster headlines
                    DayCreated = dayNumber,
                    Category = "muster",
                    Type = DispatchType.Report,
                    Confidence = 100,
                    MinDisplayDays = 1,
                    FirstShownDay = -1
                });

                // Trim to capacity
                while (_personalFeed.Count > MaxPersonalFeedItems)
                {
                    _personalFeed.RemoveAt(0);
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, "Failed to add to personal feed", ex);
            }
        }

        /// <summary>
        /// Returns recent event outcomes within the specified number of days.
        /// </summary>
        public List<EventOutcomeRecord> GetRecentEventOutcomes(int maxDaysOld = 3)
        {
            try
            {
                _eventOutcomes ??= new List<EventOutcomeRecord>();

                var currentDay = (int)CampaignTime.Now.ToDays;
                return _eventOutcomes
                    .Where(e => currentDay - e.DayNumber <= maxDaysOld)
                    .OrderByDescending(e => e.DayNumber)
                    .ToList();
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, "Failed to get recent event outcomes", ex);
                return new List<EventOutcomeRecord>();
            }
        }

        /// <summary>
        /// Builds a context-appropriate headline for an event outcome.
        /// Uses event type patterns to generate immersive headlines.
        /// </summary>
        private string BuildEventHeadline(EventOutcomeRecord outcome)
        {
            var eventId = outcome.EventId?.ToLowerInvariant() ?? string.Empty;
            var effects = outcome.OutcomeSummary ?? string.Empty;

            // Dice/gambling events
            if (eventId.Contains("dice") || eventId.Contains("gambling") || eventId.Contains("wager"))
            {
                return string.IsNullOrEmpty(effects)
                    ? "Dice game in camp"
                    : $"Dice game in camp — {effects}";
            }

            // Training events
            if (eventId.Contains("training") || eventId.Contains("drill") || eventId.Contains("practice"))
            {
                return string.IsNullOrEmpty(effects)
                    ? "Training session"
                    : $"Training session — {effects}";
            }

            // Hunt events
            if (eventId.Contains("hunt"))
            {
                return string.IsNullOrEmpty(effects)
                    ? "Hunt with the lord"
                    : $"Hunt with the lord — {effects}";
            }

            // Lending/loan events
            if (eventId.Contains("lend") || eventId.Contains("loan"))
            {
                if (eventId.Contains("repay") || eventId.Contains("return"))
                {
                    return string.IsNullOrEmpty(effects)
                        ? "Debt repaid"
                        : $"Debt repaid — {effects}";
                }
                return "Lent money to a comrade";
            }

            // Scrutiny/attention events
            if (eventId.Contains("scrutiny") || eventId.Contains("attention") || eventId.Contains("noticed"))
            {
                return string.IsNullOrEmpty(effects)
                    ? "Unwanted attention"
                    : $"Unwanted attention — {effects}";
            }

            // Discipline events
            if (eventId.Contains("discipline") || eventId.Contains("punishment") || eventId.Contains("infraction"))
            {
                return string.IsNullOrEmpty(effects)
                    ? "Disciplinary matter"
                    : $"Disciplinary matter — {effects}";
            }

            // Loot/battlefield events
            if (eventId.Contains("loot") || eventId.Contains("battlefield") || eventId.Contains("corpse"))
            {
                return string.IsNullOrEmpty(effects)
                    ? "Battlefield opportunity"
                    : $"Battlefield opportunity — {effects}";
            }

            // Tavern/settlement events
            if (eventId.Contains("tavern") || eventId.Contains("drinking"))
            {
                return string.IsNullOrEmpty(effects)
                    ? "Evening at the tavern"
                    : $"Evening at the tavern — {effects}";
            }

            // Generic fallback with title
            var title = outcome.EventTitle ?? outcome.EventId ?? "Event";
            return string.IsNullOrEmpty(effects)
                ? title
                : $"{title} — {effects}";
        }

        /// <summary>
        /// Posts a short high-signal personal dispatch line (used by decision outcomes).
        /// </summary>
        public void PostPersonalDispatchText(string category, string text, string storyKey, int minDisplayDays = 2)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(text))
                {
                    return;
                }

                AddPersonalNews(category ?? "personal", text.Trim(), new Dictionary<string, string>(), storyKey, minDisplayDays);
                TrimFeeds();
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, "Failed to post personal dispatch text", ex);
            }
        }

        private DailyReportSnapshot BuildDailyReportSnapshot(
            EnlistmentBehavior enlistment,
            int dayNumber,
            out DailyAggregate aggregate,
            out DailyReportGenerationContext context)
        {
            aggregate = default;
            context = new DailyReportGenerationContext { DayNumber = dayNumber };

            var snapshot = new DailyReportSnapshot
            {
                DayNumber = dayNumber
            };

            try
            {
                var ctx = new CampNewsContext
                {
                    DayNumber = dayNumber,
                    Enlistment = enlistment,
                    Lord = enlistment?.CurrentLord,
                    LordParty = enlistment?.CurrentLord?.PartyBelongedTo,
                    TriggerTracker = CampaignTriggerTrackerBehavior.Instance,
                    CampLife = CampLifeBehavior.Instance,
                    // The unit schedule is managed through direct orders.
                    NewsState = _campNewsState,
                    NewsBehavior = this,
                    Generation = context
                };

                for (var i = 0; i < DailyReportFactProducers.Length; i++)
                {
                    try
                    {
                        DailyReportFactProducers[i]?.Contribute(snapshot, ctx);
                    }
                    catch (Exception ex)
                    {
                        ModLogger.Error(LogCategory, $"Daily report producer failed: {DailyReportFactProducers[i]?.GetType().Name}", ex);
                    }
                }

                // Ledger aggregate for the day (best-effort).
                aggregate = DailyAggregate.CreateEmpty(dayNumber);
                aggregate.LostToday = snapshot.DeadDelta;
                aggregate.WoundedToday = Math.Max(0, snapshot.WoundedDelta);
                aggregate.SickToday = Math.Max(0, snapshot.SickDelta);
                // TrainingIncidents/DecisionsResolved/Dispatches tracked in later phases.

                // Accumulate "since last muster" counters (reset at each muster).
                if (snapshot.DeadDelta > 0)
                {
                    _lostSinceLastMuster += snapshot.DeadDelta;
                }
                if (snapshot.SickDelta > 0)
                {
                    _sickSinceLastMuster += snapshot.SickDelta;
                }

                snapshot.Normalize();
                return snapshot;
            }
            catch
            {
                snapshot.Normalize();
                return snapshot;
            }
        }

        /// <summary>
        /// Helper: get the latest Daily Report lines (already templated) if available.
        /// </summary>
        public IReadOnlyList<string> GetLatestDailyReportLines()
        {
            try
            {
                // If UI asks before the daily tick fires (e.g., right after load), generate lazily but safely.
                EnsureDailyReportGenerated();

                _campNewsState ??= new CampNewsState();

                var today = (int)CampaignTime.Now.ToDays;
                var record = _campNewsState.TryGetReportForDay(today) ?? _campNewsState.TryGetLatestReport();
                return record?.Lines ?? Array.Empty<string>();
            }
            catch
            {
                return Array.Empty<string>();
            }
        }

        /// <summary>
        /// Helper: returns a compact excerpt suitable for a main-menu paragraph.
        /// </summary>
        public string GetLatestDailyReportExcerpt(int maxLines = 3, int maxChars = 260)
        {
            try
            {
                // Ensure we have a record for the current day before formatting an excerpt.
                EnsureDailyReportGenerated();

                maxLines = Math.Max(1, Math.Min(maxLines, 6));
                maxChars = Math.Max(80, Math.Min(maxChars, 600));

                var lines = GetLatestDailyReportLines();
                if (lines == null || lines.Count == 0)
                {
                    return string.Empty;
                }

                var parts = lines
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => x.Trim())
                    .Take(maxLines)
                    .ToList();

                if (parts.Count == 0)
                {
                    return string.Empty;
                }

                var excerpt = string.Join(" ", parts).Trim();

                // Check for any urgent matters that require the soldier's attention.
                try
                {
                    /*
                    var decisionBehavior = DecisionEventBehavior.Instance;
                    var available = decisionBehavior?.GetAvailablePlayerDecisions();
                    var count = available?.Count ?? 0;

                    if (count > 0 && available != null)
                    {
                        var enlistment = EnlistmentBehavior.Instance;
                        var top = available[0];
                        var topTitle = LanceLifeEventText.Resolve(top?.TitleId, top?.TitleFallback, top?.Id, enlistment).Trim();

                        // Keep this ONE sentence in the paragraph (use an em dash rather than a second sentence).
                        var oppLong = !string.IsNullOrWhiteSpace(topTitle)
                            ? $"Opportunities: {count} matters await your decision — most pressing: {topTitle}."
                            : $"Opportunities: {count} matters await your decision.";

                        var oppShort = $"Opportunities: {count} awaiting.";

                        // Prefer the richer sentence when it fits; otherwise use the short one.
                        var opp = $"{excerpt} {oppLong}".Trim().Length <= maxChars ? oppLong : oppShort;

                        // Ensure the Opportunities sentence is included by trimming the base excerpt if needed.
                        // (Main menu excerpt maxChars is typically generous, but we keep this robust.)
                        var reserved = opp.Length + 1; // space + sentence
                        if (reserved < maxChars)
                        {
                            var allowedBase = Math.Max(0, maxChars - reserved);
                            var baseText = excerpt;

                            if (baseText.Length > allowedBase)
                            {
                                // Trim base text to make room and add ellipsis to avoid an abrupt cut.
                                var trimmed = baseText.Substring(0, allowedBase).TrimEnd();
                                if (!string.IsNullOrWhiteSpace(trimmed))
                                {
                                    // Reserve room for ellipsis if possible.
                                    if (trimmed.Length > 3)
                                    {
                                        trimmed = trimmed.Substring(0, trimmed.Length - 3).TrimEnd() + "...";
                                    }
                                    else
                                    {
                                        trimmed = "...";
                                    }
                                }

                                baseText = trimmed;
                            }

                            excerpt = $"{baseText} {opp}".Trim();
                        }
                        else
                        {
                            // Degenerate case: maxChars too small - fall back to the opportunities sentence only.
                            excerpt = opp.Length <= maxChars
                                ? opp
                                : opp.Substring(0, maxChars).TrimEnd() + "...";
                        }
                    }
                    */
                }
                catch
                {
                    // Best-effort only; never break menu rendering due to decision availability checks.
                }

                if (excerpt.Length <= maxChars)
                {
                    return excerpt;
                }

                // Trim with a hard cap, preserving a clean ending.
                var cut = excerpt.Substring(0, maxChars).TrimEnd();
                return cut.EndsWith(".", StringComparison.Ordinal) ? cut : cut + "...";
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Checks if lord's army has just formed (for personal feed).
        /// </summary>
        private void CheckForArmyFormation()
        {
            try
            {
                var enlistment = EnlistmentBehavior.Instance;
                if (enlistment == null || !enlistment.IsEnlisted)
                {
                    return;
                }

                var lord = enlistment.EnlistedLord;
                var hasArmyNow = lord?.PartyBelongedTo?.Army != null;

                // Army just formed (wasn't there yesterday, is there now)
                if (hasArmyNow && !_lordHadArmyYesterday)
                {
                    var placeholders = new Dictionary<string, string>
                    {
                        { "LORD", lord.Name.ToString() }
                    };

                    AddPersonalNews("army", "News_ArmyForming", placeholders, $"army:{lord.StringId}", 2);
                }

                _lordHadArmyYesterday = hasArmyNow;
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, "Error checking army formation", ex);
            }
        }

        /// <summary>
        /// Ensures the Daily Brief is generated for the current in-game day.
        /// Stable for the day; regenerated at most once per day.
        /// </summary>
        private void EnsureDailyBriefGenerated()
        {
            try
            {
                var currentDay = (int)CampaignTime.Now.ToDays;
                if (_lastDailyBriefDayNumber == currentDay &&
                    (!string.IsNullOrWhiteSpace(_dailyBriefCompany) ||
                     !string.IsNullOrWhiteSpace(_dailyBriefUnit) ||
                     !string.IsNullOrWhiteSpace(_dailyBriefKingdom)))
                {
                    return;
                }

                var enlistment = EnlistmentBehavior.Instance;
                if (enlistment?.IsEnlisted != true)
                {
                    _lastDailyBriefDayNumber = currentDay;
                    _dailyBriefCompany = string.Empty;
                    _dailyBriefUnit = string.Empty;
                    _dailyBriefKingdom = string.Empty;
                    return;
                }

                _lastDailyBriefDayNumber = currentDay;
                _dailyBriefCompany = BuildDailyCompanyLine(enlistment);
                _dailyBriefUnit = BuildDailyUnitLine(enlistment);
                _dailyBriefKingdom = BuildDailyKingdomLine(enlistment);
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, "Error generating Daily Brief", ex);
            }
        }

        private static string BuildDailyCompanyLine(EnlistmentBehavior enlistment)
        {
            try
            {
                var lord = enlistment?.CurrentLord;
                var party = lord?.PartyBelongedTo;
                if (party == null)
                {
                    return "No company details available.";
                }

                if (party.Party?.MapEvent != null)
                {
                    return "Engaged in battle.";
                }

                if (party.Party?.SiegeEvent != null)
                {
                    return "Committed to siege operations.";
                }

                if (party.CurrentSettlement != null)
                {
                    return $"Camped at {party.CurrentSettlement.Name}.";
                }

                if (party.Army != null)
                {
                    var leader = party.Army.LeaderParty?.LeaderHero?.Name?.ToString() ?? "the army leader";
                    return $"Marching with the army of {leader}.";
                }

                if (party.TargetSettlement != null)
                {
                    return $"Marching toward {party.TargetSettlement.Name}.";
                }

                return "On the march.";
            }
            catch
            {
                return "On the march.";
            }
        }

        private static readonly Random FlavorRng = new Random();
        
        private string BuildDailyUnitLine(EnlistmentBehavior enlistment)
        {
            try
            {
                var tier = enlistment?.EnlistmentTier ?? 1;
                var hour = CampaignTime.Now.GetHourOfDay;
                var tierKey = GetTierKey(tier);
                
                // Priority 1: Recent battle aftermath (within 1 day)
                if (_lastPlayerBattleTime != CampaignTime.Zero)
                {
                    var hoursSinceBattle = (CampaignTime.Now - _lastPlayerBattleTime).ToHours;
                    if (hoursSinceBattle < 24 && !string.IsNullOrEmpty(_lastPlayerBattleType))
                    {
                        var outcomeKey = _lastPlayerBattleWon ? "won" : "lost";
                        var prefix = $"brief_{_lastPlayerBattleType}_{outcomeKey}_{tierKey}_";
                        var text = PickRandomLocalizedString(prefix, 3);
                        if (!string.IsNullOrEmpty(text))
                        {
                            return text;
                        }
                    }
                }
                
                // Priority 2: Injuries/illness
                var cond = PlayerConditionBehavior.Instance;
                if (cond?.IsEnabled() == true && cond.State?.HasAnyCondition == true)
                {
                    if (cond.State.HasInjury)
                    {
                        var prefix = $"brief_injury_{tierKey}_";
                        return PickRandomLocalizedString(prefix, 3, "brief_fallback_injury");
                    }
                    if (cond.State.HasIllness)
                    {
                        var prefix = $"brief_illness_{tierKey}_";
                        return PickRandomLocalizedString(prefix, 3, "brief_fallback_illness");
                    }
                }

                // Priority 3: Fatigue
                if (enlistment != null && enlistment.FatigueMax > 0)
                {
                    var fatiguePct = (float)enlistment.FatigueCurrent / enlistment.FatigueMax;
                    
                    if (fatiguePct <= 0.25f)
                    {
                        var prefix = $"brief_exhausted_{tierKey}_";
                        return PickRandomLocalizedString(prefix, 3, "brief_fallback_exhausted");
                    }
                    if (fatiguePct <= 0.5f)
                    {
                        var prefix = $"brief_tired_{tierKey}_";
                        return PickRandomLocalizedString(prefix, 3, "brief_fallback_tired");
                    }
                }

                // Priority 4: Good condition - vary by tier and time of day
                var timeKey = GetTimeKey(hour);
                var prefix2 = $"brief_good_{tierKey}_{timeKey}_";
                return PickRandomLocalizedString(prefix2, 3, "brief_fallback_default");
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, "Error building daily unit line", ex);
                return new TextObject("{=brief_fallback_default}You're ready for whatever comes.").ToString();
            }
        }
        
        private static string GetTierKey(int tier)
        {
            if (tier <= 2)
            {
                return "recruit";
            }
            if (tier <= 4)
            {
                return "soldier";
            }
            if (tier <= 6)
            {
                return "nco";
            }
            return "veteran";
        }
        
        private static string GetTimeKey(int hour)
        {
            if (hour < 6 || hour >= 20)
            {
                return "night";
            }
            if (hour < 10)
            {
                return "morning";
            }
            if (hour >= 17)
            {
                return "evening";
            }
            return "day";
        }
        
        private static string PickRandomLocalizedString(string prefix, int count, string fallbackId = null)
        {
            var index = FlavorRng.Next(1, count + 1);
            var id = $"{prefix}{index}";
            var text = new TextObject($"{{={id}}}").ToString();
            
            // If TextObject didn't resolve (returned the ID), try fallback
            if (text.Contains($"{{={id}}}") || text == id)
            {
                if (!string.IsNullOrEmpty(fallbackId))
                {
                    return new TextObject($"{{={fallbackId}}}").ToString();
                }
                return string.Empty;
            }
            
            return text;
        }

        private string BuildDailyKingdomLine(EnlistmentBehavior enlistment)
        {
            try
            {
                // Prefer a real headline if we have one (keeps the brief grounded in actual events).
                if (_kingdomFeed != null && _kingdomFeed.Count > 0)
                {
                    var latest = _kingdomFeed.OrderByDescending(x => x.DayCreated).FirstOrDefault();
                    var headline = FormatDispatchItem(latest);
                    if (!string.IsNullOrWhiteSpace(headline))
                    {
                        return headline;
                    }
                }

                var kingdom = enlistment?.CurrentLord?.MapFaction as Kingdom;
                if (kingdom?.Name != null)
                {
                    return $"The banners of {kingdom.Name} remain in the field.";
                }

                return "The realm is quiet, for now.";
            }
            catch
            {
                return "The realm is quiet, for now.";
            }
        }

        /// <summary>
        /// Battle started: cache initial troop strengths for pyrrhic detection.
        /// </summary>
        private void OnMapEventStarted(MapEvent mapEvent, PartyBase attackerParty, PartyBase defenderParty)
        {
            try
            {
                if (mapEvent == null)
                {
                    return;
                }

                var playerKingdom = GetPlayerEnlistedKingdom();
                if (playerKingdom == null)
                {
                    return;
                }

                // Only track battles involving player's kingdom
                var attackerFaction = attackerParty?.MapFaction;
                var defenderFaction = defenderParty?.MapFaction;

                if (attackerFaction != playerKingdom && defenderFaction != playerKingdom)
                {
                    return;
                }

                // Cache initial troop strengths for pyrrhic victory detection
                var snapshot = new BattleSnapshot
                {
                    MapEventId = mapEvent.GetHashCode().ToString(),
                    AttackerInitialStrength = mapEvent.AttackerSide?.TroopCount ?? 0,
                    DefenderInitialStrength = mapEvent.DefenderSide?.TroopCount ?? 0
                };

                _battleSnapshots[snapshot.MapEventId] = snapshot;
                ModLogger.Debug(LogCategory, $"Battle started: A={snapshot.AttackerInitialStrength}, D={snapshot.DefenderInitialStrength}");
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, "Error in OnMapEventStarted", ex);
            }
        }

        /// <summary>
        /// Battle ended: generate battle news with winner/loser and pyrrhic classification.
        /// </summary>
        private void OnMapEventEnded(MapEvent mapEvent)
        {
            try
            {
                if (mapEvent == null)
                {
                    return;
                }

                var playerKingdom = GetPlayerEnlistedKingdom();
                if (playerKingdom == null)
                {
                    return;
                }

                // Only report battles involving player's kingdom
                var attackerFaction = mapEvent.AttackerSide?.LeaderParty?.MapFaction;
                var defenderFaction = mapEvent.DefenderSide?.LeaderParty?.MapFaction;

                if (attackerFaction != playerKingdom && defenderFaction != playerKingdom)
                {
                    // Clean up any orphaned snapshot
                    _battleSnapshots.Remove(mapEvent.GetHashCode().ToString());
                    return;
                }

                // Skip bandit/looter battles - only report kingdom vs kingdom conflicts
                bool isAttackerBandit = attackerFaction == null || attackerFaction.IsBanditFaction;
                bool isDefenderBandit = defenderFaction == null || defenderFaction.IsBanditFaction;
                
                if (isAttackerBandit || isDefenderBandit)
                {
                    // Don't report bandit skirmishes - they're too minor for kingdom news
                    _battleSnapshots.Remove(mapEvent.GetHashCode().ToString());
                    return;
                }

                // Determine winner
                BattleSideEnum? winnerSide = null;
                if (mapEvent.BattleState == BattleState.AttackerVictory)
                {
                    winnerSide = BattleSideEnum.Attacker;
                }
                else if (mapEvent.BattleState == BattleState.DefenderVictory)
                {
                    winnerSide = BattleSideEnum.Defender;
                }

                if (winnerSide == null)
                {
                    // Inconclusive battle
                    AddBattleNews(mapEvent, "News_Inconclusive", null);
                    _battleSnapshots.Remove(mapEvent.GetHashCode().ToString());
                    return;
                }

                // Get snapshot for pyrrhic detection
                _battleSnapshots.TryGetValue(mapEvent.GetHashCode().ToString(), out var snapshot);
                var classification = ClassifyBattle(mapEvent, snapshot, winnerSide.Value);

                AddBattleNews(mapEvent, classification, winnerSide.Value);

                // Clean up snapshot
                _battleSnapshots.Remove(mapEvent.GetHashCode().ToString());

                // Check for player participation (Personal feed)
                CheckPlayerBattleParticipation(mapEvent, winnerSide.Value);
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, "Error in OnMapEventEnded", ex);
            }
        }

        /// <summary>
        /// Checks if player participated in the battle and generates personal news.
        /// Also records battle details for daily brief flavor.
        /// </summary>
        private void CheckPlayerBattleParticipation(MapEvent mapEvent, BattleSideEnum winnerSide)
        {
            try
            {
                if (!IsEnlisted() || mapEvent == null)
                {
                    return;
                }

                var playerParty = MobileParty.MainParty;
                if (playerParty?.Party == null)
                {
                    return;
                }

                // Check if player was involved in the battle
                var playerInvolved = mapEvent.InvolvedParties?.Contains(playerParty.Party) == true;
                if (!playerInvolved)
                {
                    return;
                }

                var place = mapEvent.MapEventSettlement?.Name?.ToString() ?? "the battlefield";
                var placeholders = new Dictionary<string, string>
                {
                    { "PLACE", place }
                };

                // Determine if player won or lost
                var playerOnAttacker = mapEvent.AttackerSide?.Parties?.Any(
                    p => p.Party == playerParty.Party) == true;
                var playerOnDefender = mapEvent.DefenderSide?.Parties?.Any(
                    p => p.Party == playerParty.Party) == true;

                var playerWon = (winnerSide == BattleSideEnum.Attacker && playerOnAttacker)
                             || (winnerSide == BattleSideEnum.Defender && playerOnDefender);

                // Record battle details for daily brief flavor
                _lastPlayerBattleTime = CampaignTime.Now;
                _lastPlayerBattleWon = playerWon;
                
                // Determine battle type
                var attackerFaction = mapEvent.AttackerSide?.LeaderParty?.MapFaction;
                var defenderFaction = mapEvent.DefenderSide?.LeaderParty?.MapFaction;
                var isBandit = (attackerFaction?.IsBanditFaction == true) || (defenderFaction?.IsBanditFaction == true);
                var isSiege = mapEvent.IsSiegeAssault || mapEvent.MapEventSettlement?.IsFortification == true;
                
                if (isSiege)
                {
                    _lastPlayerBattleType = "siege";
                }
                else if (isBandit)
                {
                    _lastPlayerBattleType = "bandit";
                }
                else
                {
                    _lastPlayerBattleType = "army";
                }

                var headlineKey = playerWon ? "News_PlayerBattle" : "News_PlayerDefeat";
                AddPersonalNews("participation", headlineKey, placeholders);
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, "Error checking player participation", ex);
            }
        }

        /// <summary>
        /// Siege started: generate siege news for kingdom settlements.
        /// </summary>
        private void OnSiegeStarted(SiegeEvent siegeEvent)
        {
            try
            {
                var playerKingdom = GetPlayerEnlistedKingdom();
                if (playerKingdom == null || siegeEvent == null)
                {
                    return;
                }

                var settlement = siegeEvent.BesiegedSettlement;
                if (settlement == null)
                {
                    return;
                }

                // Report if it's our kingdom's settlement being besieged OR we're the attackers
                var isOurSettlement = settlement.MapFaction == playerKingdom;
                var isOurSiege = siegeEvent.BesiegerCamp?.LeaderParty?.LeaderHero?.MapFaction == playerKingdom;

                if (!isOurSettlement && !isOurSiege)
                {
                    return;
                }

                var placeholders = new Dictionary<string, string>
                {
                    { "SETTLEMENT", settlement.Name?.ToString() ?? new TextObject("{=enl_ui_unknown}Unknown").ToString() }
                };

                AddKingdomNews("siege", "News_Siege", placeholders, $"siege:{settlement.StringId}", 2);
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, "Error in OnSiegeStarted", ex);
            }
        }

        /// <summary>
        /// Hero captured: generate prisoner news for kingdom lords.
        /// </summary>
        private void OnHeroPrisonerTaken(PartyBase captorParty, Hero prisoner)
        {
            try
            {
                var playerKingdom = GetPlayerEnlistedKingdom();
                if (playerKingdom == null || prisoner == null)
                {
                    return;
                }

                // Only report kingdom lords being captured
                if (prisoner.MapFaction != playerKingdom)
                {
                    return;
                }

                var placeholders = new Dictionary<string, string>
                {
                    { "LORD", prisoner.Name?.ToString() ?? new TextObject("{=enl_ui_unknown}Unknown").ToString() }
                };

                if (captorParty?.LeaderHero != null)
                {
                    placeholders["CAPTOR"] = captorParty.LeaderHero.Name?.ToString() ?? new TextObject("{=enl_ui_unknown}Unknown").ToString();
                    AddKingdomNews("prisoner", "News_PrisonerCapturedBy", placeholders, $"prisoner:{prisoner.StringId}");
                }
                else
                {
                    AddKingdomNews("prisoner", "News_PrisonerTaken", placeholders, $"prisoner:{prisoner.StringId}");
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, "Error in OnHeroPrisonerTaken", ex);
            }
        }

        /// <summary>
        /// Hero released/escaped: generate prisoner release news.
        /// </summary>
        private void OnHeroPrisonerReleased(Hero prisoner, PartyBase party, IFaction captor, EndCaptivityDetail detail, bool isTransfer)
        {
            try
            {
                var playerKingdom = GetPlayerEnlistedKingdom();
                if (playerKingdom == null || prisoner == null)
                {
                    return;
                }

                // Only report kingdom lords being released
                if (prisoner.MapFaction != playerKingdom)
                {
                    return;
                }

                // Don't report transfers as releases
                if (isTransfer)
                {
                    return;
                }

                var placeholders = new Dictionary<string, string>
                {
                    { "LORD", prisoner.Name?.ToString() ?? new TextObject("{=enl_ui_unknown}Unknown").ToString() }
                };

                // Choose headline based on how they got free
                // Note: EndCaptivityDetail values vary by game version; use Released as default
                var headlineKey = detail == EndCaptivityDetail.ReleasedAfterEscape
                    ? "News_PrisonerEscaped"
                    : "News_PrisonerReleased";
                AddKingdomNews("prisoner", headlineKey, placeholders, $"prisoner:{prisoner.StringId}");
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, "Error in OnHeroPrisonerReleased", ex);
            }
        }

        /// <summary>
        /// Hero killed: generate execution news for kingdom lords.
        /// </summary>
        private void OnHeroKilled(Hero victim, Hero killer, KillCharacterAction.KillCharacterActionDetail detail, bool showNotification)
        {
            try
            {
                var playerKingdom = GetPlayerEnlistedKingdom();
                if (playerKingdom == null || victim == null)
                {
                    return;
                }

                // Only report kingdom lords being executed
                if (victim.MapFaction != playerKingdom)
                {
                    return;
                }

                // Only report executions, not natural deaths or battle deaths
                if (detail != KillCharacterAction.KillCharacterActionDetail.Executed)
                {
                    return;
                }

                var placeholders = new Dictionary<string, string>
                {
                    { "LORD", victim.Name?.ToString() ?? new TextObject("{=enl_ui_unknown}Unknown").ToString() }
                };

                if (killer != null)
                {
                    placeholders["EXECUTOR"] = killer.Name?.ToString() ?? new TextObject("{=enl_ui_unknown}Unknown").ToString();
                    AddKingdomNews("execution", "News_Executed", placeholders, $"executed:{victim.StringId}", 2);
                }
                else
                {
                    AddKingdomNews("execution", "News_ExecutedNoExecutor", placeholders, $"executed:{victim.StringId}", 2);
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, "Error in OnHeroKilled", ex);
            }
        }

        /// <summary>
        /// Settlement ownership changed: generate capture/fallen news.
        /// </summary>
        private void OnSettlementOwnerChanged(
            Settlement settlement,
            bool openToClaim,
            Hero newOwner,
            Hero oldOwner,
            Hero capturerHero,
            ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
        {
            try
            {
                var playerKingdom = GetPlayerEnlistedKingdom();
                if (playerKingdom == null || settlement == null)
                {
                    return;
                }

                // Report if it was our kingdom's settlement (lost) or we captured it
                var wasOurs = oldOwner?.MapFaction == playerKingdom;
                var isNowOurs = newOwner?.MapFaction == playerKingdom;

                if (!wasOurs && !isNowOurs)
                {
                    return;
                }

                var placeholders = new Dictionary<string, string>
                {
                    { "SETTLEMENT", settlement.Name?.ToString() ?? new TextObject("{=enl_ui_unknown}Unknown").ToString() },
                    { "KINGDOM", newOwner?.MapFaction?.Name?.ToString() ?? new TextObject("{=enl_ui_unknown}Unknown").ToString() }
                };

                if (isNowOurs)
                {
                    AddKingdomNews("settlement", "News_Captured", placeholders, $"settlement_captured:{settlement.StringId}", 2);
                }
                else
                {
                    AddKingdomNews("settlement", "News_Fallen", placeholders, $"settlement_fallen:{settlement.StringId}", 2);
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, "Error in OnSettlementOwnerChanged", ex);
            }
        }

        /// <summary>
        /// Army dispersed: generate army news for personal feed.
        /// </summary>
        private void OnArmyDispersed(Army army, Army.ArmyDispersionReason reason, bool isLeaderPartyRemoved)
        {
            try
            {
                if (army == null)
                {
                    return;
                }

                var enlistment = EnlistmentBehavior.Instance;
                if (enlistment == null || !enlistment.IsEnlisted)
                {
                    return;
                }

                var lord = enlistment.EnlistedLord;
                var lordParty = lord?.PartyBelongedTo;

                // Only report player's lord's army dispersing
                if (army.LeaderParty != lordParty)
                {
                    return;
                }

                var placeholders = new Dictionary<string, string>
                {
                    { "LORD", lord?.Name?.ToString() ?? new TextObject("{=enl_ui_unknown}Unknown").ToString() }
                };

                AddPersonalNews("army", "News_ArmyDisbanded", placeholders, $"army:{lord?.StringId}", 2);
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, "Error in OnArmyDispersed", ex);
            }
        }

        /// <summary>
        /// Village being raided: generate raid news for kingdom villages.
        /// </summary>
        private void OnVillageRaided(Village village)
        {
            try
            {
                var playerKingdom = GetPlayerEnlistedKingdom();
                if (playerKingdom == null || village == null)
                {
                    return;
                }

                // Only report our kingdom's villages being raided
                if (village.Settlement?.MapFaction != playerKingdom)
                {
                    return;
                }

                var placeholders = new Dictionary<string, string>
                {
                    { "SETTLEMENT", village.Name?.ToString() ?? new TextObject("{=enl_ui_unknown}Unknown").ToString() }
                };

                // Use village settlement StringId for deduplication
                var villageId = village.Settlement?.StringId ?? village.Name?.ToString() ?? "unknown";
                AddKingdomNews("raid", "News_Raid", placeholders, $"raid:{villageId}:{(int)CampaignTime.Now.ToDays}");
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, "Error in OnVillageRaided", ex);
            }
        }

        /// <summary>
        /// War declared: generate war news if player's kingdom is involved.
        /// </summary>
        private void OnWarDeclared(IFaction faction1, IFaction faction2, DeclareWarAction.DeclareWarDetail detail)
        {
            try
            {
                var playerKingdom = GetPlayerEnlistedKingdom();
                if (playerKingdom == null)
                {
                    return;
                }

                // Only report if player's kingdom is involved
                if (faction1 != playerKingdom && faction2 != playerKingdom)
                {
                    return;
                }

                var placeholders = new Dictionary<string, string>
                {
                    { "KINGDOM_A", faction1?.Name?.ToString() ?? new TextObject("{=enl_ui_unknown}Unknown").ToString() },
                    { "KINGDOM_B", faction2?.Name?.ToString() ?? new TextObject("{=enl_ui_unknown}Unknown").ToString() }
                };

                AddKingdomNews("war", "News_War", placeholders, $"war:{faction1?.StringId}:{faction2?.StringId}", 2);
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, "Error in OnWarDeclared", ex);
            }
        }

        /// <summary>
        /// Peace made: generate peace news if player's kingdom is involved.
        /// </summary>
        private void OnPeaceMade(IFaction faction1, IFaction faction2, MakePeaceAction.MakePeaceDetail detail)
        {
            try
            {
                var playerKingdom = GetPlayerEnlistedKingdom();
                if (playerKingdom == null)
                {
                    return;
                }

                // Only report if player's kingdom is involved
                if (faction1 != playerKingdom && faction2 != playerKingdom)
                {
                    return;
                }

                // Show the OTHER kingdom (the one we made peace with)
                var otherKingdom = faction1 == playerKingdom ? faction2 : faction1;
                var placeholders = new Dictionary<string, string>
                {
                    { "KINGDOM", otherKingdom?.Name?.ToString() ?? new TextObject("{=enl_ui_unknown}Unknown").ToString() }
                };

                AddKingdomNews("peace", "News_Peace", placeholders, $"peace:{faction1?.StringId}:{faction2?.StringId}", 2);
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, "Error in OnPeaceMade", ex);
            }
        }

        #endregion

        #region News Generation Helpers

        /// <summary>
        /// Adds a news item to the kingdom feed with deduplication.
        /// </summary>
        /// <param name="category">Category for filtering (e.g., "war", "battle", "prisoner").</param>
        /// <param name="headlineKey">Localization string ID.</param>
        /// <param name="placeholders">Placeholder values for the headline.</param>
        /// <param name="storyKey">Optional key for deduplication.</param>
        /// <param name="minDisplayDays">Minimum number of whole days this item must remain visible once shown.</param>
        private void AddKingdomNews(
            string category,
            string headlineKey,
            Dictionary<string, string> placeholders,
            string storyKey = null,
            int minDisplayDays = 1)
        {
            if (!IsEnlisted())
            {
                return;
            }

            var effectiveStoryKey = storyKey ?? $"{category}:{Guid.NewGuid()}";

            // Story updates:
            // If we already have an item with this storyKey, update it (this simulates "until something else happens").
            if (!string.IsNullOrEmpty(storyKey))
            {
                var existingIndex = _kingdomFeed.FindIndex(x => x.StoryKey == storyKey);
                if (existingIndex >= 0)
                {
                    var updated = _kingdomFeed[existingIndex];
                    updated.DayCreated = (int)CampaignTime.Now.ToDays;
                    updated.Category = category;
                    updated.HeadlineKey = headlineKey;
                    updated.PlaceholderValues = placeholders ?? new Dictionary<string, string>();
                    updated.StoryKey = effectiveStoryKey;
                    updated.Type = DispatchType.Report;
                    updated.Confidence = 100;
                    updated.MinDisplayDays = Math.Max(1, minDisplayDays);
                    updated.FirstShownDay = -1; // reset visibility window

                    _kingdomFeed[existingIndex] = updated;
                    ModLogger.Info(LogCategory, $"Kingdom news updated: {headlineKey} ({category})");
                    return;
                }
            }

            var item = new DispatchItem
            {
                DayCreated = (int)CampaignTime.Now.ToDays,
                Category = category,
                HeadlineKey = headlineKey,
                PlaceholderValues = placeholders ?? new Dictionary<string, string>(),
                StoryKey = effectiveStoryKey,
                Type = DispatchType.Report,
                Confidence = 100,
                MinDisplayDays = Math.Max(1, minDisplayDays),
                FirstShownDay = -1
            };

            _kingdomFeed.Add(item);
            ModLogger.Info(LogCategory, $"Kingdom news: {headlineKey} ({category})");
        }

        /// <summary>
        /// Classifies a battle outcome based on casualty rates.
        /// </summary>
        /// <param name="mapEvent">The battle map event.</param>
        /// <param name="snapshot">Cached initial troop strengths.</param>
        /// <param name="winnerSide">Which side won the battle.</param>
        /// <returns>Localization key for the appropriate headline.</returns>
        private static string ClassifyBattle(MapEvent mapEvent, BattleSnapshot snapshot, BattleSideEnum winnerSide)
        {
            try
            {
                var winnerIsAttacker = winnerSide == BattleSideEnum.Attacker;
                var winnerFinal = winnerIsAttacker
                    ? mapEvent.AttackerSide?.TroopCount ?? 0
                    : mapEvent.DefenderSide?.TroopCount ?? 0;
                var loserFinal = winnerIsAttacker
                    ? mapEvent.DefenderSide?.TroopCount ?? 0
                    : mapEvent.AttackerSide?.TroopCount ?? 0;

                var winnerInitial = winnerIsAttacker
                    ? snapshot.AttackerInitialStrength
                    : snapshot.DefenderInitialStrength;
                var loserInitial = winnerIsAttacker
                    ? snapshot.DefenderInitialStrength
                    : snapshot.AttackerInitialStrength;

                // If we don't have initial data, default to clean victory
                if (winnerInitial == 0 || loserInitial == 0)
                {
                    return "News_Victory";
                }

                var winnerLosses = winnerInitial - winnerFinal;
                var loserLosses = loserInitial - loserFinal;

                var winnerLossRate = (float)winnerLosses / Math.Max(1, winnerInitial);
                var loserLossRate = (float)loserLosses / Math.Max(1, loserInitial);

                // Mutual ruin: Both sides lost 60%+ troops
                if (winnerLossRate >= 0.60f && loserLossRate >= 0.60f)
                {
                    return "News_Butchery";
                }

                // Pyrrhic victory: Winner lost 45%+ and loser lost 55%+
                if (winnerLossRate >= 0.45f && loserLossRate >= 0.55f)
                {
                    return "News_Pyrrhic";
                }

                // Costly victory: Winner lost 20-45%
                if (winnerLossRate >= 0.20f && winnerLossRate < 0.45f)
                {
                    return "News_Costly";
                }

                // Clean victory: Winner lost <20%
                return "News_Victory";
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, "Error classifying battle", ex);
                return "News_Victory";
            }
        }

        /// <summary>
        /// Adds battle news with winner, loser, and place information.
        /// </summary>
        /// <param name="mapEvent">The battle map event.</param>
        /// <param name="headlineKey">The localization key for the headline.</param>
        /// <param name="winnerSide">Which side won (null for inconclusive).</param>
        private void AddBattleNews(MapEvent mapEvent, string headlineKey, BattleSideEnum? winnerSide)
        {
            try
            {
                var placeholders = new Dictionary<string, string>();

                if (winnerSide.HasValue)
                {
                    // Get the winning and losing sides based on battle outcome
                    var winnerSideData = winnerSide.Value == BattleSideEnum.Attacker
                        ? mapEvent.AttackerSide
                        : mapEvent.DefenderSide;
                    var loserSideData = winnerSide.Value == BattleSideEnum.Attacker
                        ? mapEvent.DefenderSide
                        : mapEvent.AttackerSide;

                    // Use hero name if available, otherwise fall back to party name (handles bandits, looters, etc.)
                    placeholders["WINNER"] = winnerSideData?.LeaderParty?.LeaderHero?.Name?.ToString()
                                             ?? winnerSideData?.LeaderParty?.Name?.ToString()
                                             ?? new TextObject("{=enl_news_unknown_forces}unknown forces").ToString();
                    placeholders["LOSER"] = loserSideData?.LeaderParty?.LeaderHero?.Name?.ToString()
                                            ?? loserSideData?.LeaderParty?.Name?.ToString()
                                            ?? new TextObject("{=enl_news_unknown_forces}unknown forces").ToString();
                }

                // Get place name (settlement or region)
                var place = new TextObject("{=enl_news_countryside}the countryside").ToString();
                if (mapEvent.MapEventSettlement != null)
                {
                    place = mapEvent.MapEventSettlement.Name?.ToString() ?? place;
                }

                placeholders["PLACE"] = place;

                // Battle news doesn't dedupe - each battle is unique
                // Battles are high-signal: keep visible for 2 days.
                AddKingdomNews("battle", headlineKey, placeholders, null, 2);
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, "Error adding battle news", ex);
            }
        }

        /// <summary>
        /// Adds a news item to the personal feed with deduplication.
        /// </summary>
        /// <param name="category">Category for filtering (e.g., "army", "participation").</param>
        /// <param name="headlineKey">Localization string ID.</param>
        /// <param name="placeholders">Placeholder values for the headline.</param>
        /// <param name="storyKey">Optional key for deduplication.</param>
        /// <param name="minDisplayDays">Minimum number of whole days this item must remain visible once shown.</param>
        private void AddPersonalNews(
            string category,
            string headlineKey,
            Dictionary<string, string> placeholders,
            string storyKey = null,
            int minDisplayDays = 1)
        {
            if (!IsEnlisted())
            {
                return;
            }

            var effectiveStoryKey = storyKey ?? $"{category}:{Guid.NewGuid()}";

            // Story updates (see AddKingdomNews).
            if (!string.IsNullOrEmpty(storyKey))
            {
                var existingIndex = _personalFeed.FindIndex(x => x.StoryKey == storyKey);
                if (existingIndex >= 0)
                {
                    var updated = _personalFeed[existingIndex];
                    updated.DayCreated = (int)CampaignTime.Now.ToDays;
                    updated.Category = category;
                    updated.HeadlineKey = headlineKey;
                    updated.PlaceholderValues = placeholders ?? new Dictionary<string, string>();
                    updated.StoryKey = effectiveStoryKey;
                    updated.Type = DispatchType.Report;
                    updated.Confidence = 100;
                    updated.MinDisplayDays = Math.Max(1, minDisplayDays);
                    updated.FirstShownDay = -1;

                    _personalFeed[existingIndex] = updated;
                    ModLogger.Info(LogCategory, $"Personal news updated: {headlineKey} ({category})");
                    return;
                }
            }

            var item = new DispatchItem
            {
                DayCreated = (int)CampaignTime.Now.ToDays,
                Category = category,
                HeadlineKey = headlineKey,
                PlaceholderValues = placeholders ?? new Dictionary<string, string>(),
                StoryKey = effectiveStoryKey,
                Type = DispatchType.Report,
                Confidence = 100,
                MinDisplayDays = Math.Max(1, minDisplayDays),
                FirstShownDay = -1
            };

            _personalFeed.Add(item);
            ModLogger.Info(LogCategory, $"Personal news: {headlineKey} ({category})");
        }

        #endregion

        #region Internal Helpers

        /// <summary>
        /// Checks if the player is currently enlisted.
        /// </summary>
        private static bool IsEnlisted()
        {
            return EnlistmentBehavior.Instance?.IsEnlisted == true;
        }

        /// <summary>
        /// Gets the player's enlisted kingdom (if any).
        /// </summary>
        private static Kingdom GetPlayerEnlistedKingdom()
        {
            var lord = EnlistmentBehavior.Instance?.EnlistedLord;
            return lord?.MapFaction as Kingdom;
        }

        /// <summary>
        /// Formats a dispatch item into display text using localization.
        /// </summary>
        private static string FormatDispatchItem(DispatchItem item)
        {
            try
            {
                if (string.IsNullOrEmpty(item.HeadlineKey))
                {
                    return string.Empty;
                }

                // WORKAROUND: Use hardcoded templates instead of localization IDs
                // Bannerlord's LocalizedTextManager doesn't find our enlisted_strings.xml entries,
                // possibly because the XML isn't being loaded into the translation system correctly.
                // For now, use direct template strings with placeholders.
                string template = GetNewsTemplate(item.HeadlineKey);
                var textObj = new TextObject(template);

                // Apply placeholder values
                if (item.PlaceholderValues != null)
                {
                    foreach (var kvp in item.PlaceholderValues)
                    {
                        textObj.SetTextVariable(kvp.Key, kvp.Value);
                    }
                }

                var resolved = textObj.ToString();
                
                return string.IsNullOrWhiteSpace(resolved) ? item.HeadlineKey : resolved;
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, $"Error formatting dispatch item {item.HeadlineKey}", ex);
                return string.Empty;
            }
        }

        /// <summary>
        /// Gets the hardcoded news template for a given headline key.
        /// WORKAROUND: Bannerlord's localization system doesn't load our XML strings properly.
        /// </summary>
        private static string GetNewsTemplate(string headlineKey)
        {
            return headlineKey switch
            {
                // Kingdom battles
                "News_Victory" => "{WINNER} defeated {LOSER} near {PLACE}.",
                "News_Costly" => "{WINNER} won a costly battle against {LOSER} at {PLACE}.",
                "News_Pyrrhic" => "Pyrrhic victory at {PLACE}: {WINNER} beat {LOSER}, but at great cost.",
                "News_Butchery" => "Butchery at {PLACE}: {WINNER} and {LOSER} bled each other dry.",
                "News_Inconclusive" => "Inconclusive fighting near {PLACE}.",

                // Kingdom diplomacy
                "News_War" => "War declared between {KINGDOM_A} and {KINGDOM_B}.",
                "News_Peace" => "Peace concluded with {KINGDOM}.",

                // Kingdom sieges / settlements / raids
                "News_Siege" => "Siege underway at {SETTLEMENT}.",
                "News_Captured" => "{SETTLEMENT} captured by {KINGDOM}.",
                "News_Fallen" => "{SETTLEMENT} fell to {KINGDOM}.",
                "News_Raid" => "{SETTLEMENT} was raided.",

                // Kingdom prisoners / executions
                "News_PrisonerTaken" => "{LORD} was taken prisoner.",
                "News_PrisonerCapturedBy" => "{LORD} was captured by {CAPTOR}.",
                "News_PrisonerReleased" => "{LORD} was released from captivity.",
                "News_PrisonerEscaped" => "{LORD} escaped captivity.",
                "News_Executed" => "{LORD} was executed by {EXECUTOR}.",
                "News_ExecutedNoExecutor" => "{LORD} was executed.",

                // Personal feed
                "News_PlayerBattle" => "We helped secure victory at {PLACE}.",
                "News_PlayerDefeat" => "We were driven from {PLACE}.",
                "News_ArmyForming" => "{LORD} is gathering an army.",
                "News_ArmyDisbanded" => "{LORD}'s army dispersed.",

                // Fallback
                _ => headlineKey
            };
        }

        /// <summary>
        /// Trims both feeds to their maximum allowed sizes.
        /// </summary>
        private void TrimFeeds()
        {
            if (_kingdomFeed != null && _kingdomFeed.Count > MaxKingdomFeedItems)
            {
                _kingdomFeed = _kingdomFeed
                    .OrderByDescending(x => x.DayCreated)
                    .Take(MaxKingdomFeedItems)
                    .ToList();
            }

            if (_personalFeed != null && _personalFeed.Count > MaxPersonalFeedItems)
            {
                _personalFeed = _personalFeed
                    .OrderByDescending(x => x.DayCreated)
                    .Take(MaxPersonalFeedItems)
                    .ToList();
            }
        }

        #endregion

        #region Save/Load Helpers

        /// <summary>
        /// Saves a single dispatch item to the data store.
        /// </summary>
        private static void SaveDispatchItem(IDataStore dataStore, string prefix, DispatchItem item)
        {
            var dayCreated = item.DayCreated;
            var category = item.Category ?? string.Empty;
            var headlineKey = item.HeadlineKey ?? string.Empty;
            var storyKey = item.StoryKey ?? string.Empty;
            var type = (int)item.Type;
            var confidence = item.Confidence;
            var minDisplayDays = Math.Max(1, item.MinDisplayDays);
            var firstShownDay = item.FirstShownDay;

            dataStore.SyncData($"{prefix}_day", ref dayCreated);
            dataStore.SyncData($"{prefix}_cat", ref category);
            dataStore.SyncData($"{prefix}_key", ref headlineKey);
            dataStore.SyncData($"{prefix}_story", ref storyKey);
            dataStore.SyncData($"{prefix}_type", ref type);
            dataStore.SyncData($"{prefix}_conf", ref confidence);
            dataStore.SyncData($"{prefix}_minDays", ref minDisplayDays);
            dataStore.SyncData($"{prefix}_shownDay", ref firstShownDay);

            // Save placeholder values as a count + individual key-value pairs
            var placeholderCount = item.PlaceholderValues?.Count ?? 0;
            dataStore.SyncData($"{prefix}_phCount", ref placeholderCount);

            if (item.PlaceholderValues != null)
            {
                var placeholderList = item.PlaceholderValues.ToList();
                for (var i = 0; i < placeholderCount; i++)
                {
                    var phKey = placeholderList[i].Key;
                    var phVal = placeholderList[i].Value ?? string.Empty;
                    dataStore.SyncData($"{prefix}_ph_{i}_k", ref phKey);
                    dataStore.SyncData($"{prefix}_ph_{i}_v", ref phVal);
                }
            }
        }

        /// <summary>
        /// Loads a single dispatch item from the data store.
        /// </summary>
        private static DispatchItem LoadDispatchItem(IDataStore dataStore, string prefix)
        {
            var dayCreated = 0;
            var category = string.Empty;
            var headlineKey = string.Empty;
            var storyKey = string.Empty;
            var type = 0;
            var confidence = 100;
            var minDisplayDays = 1;
            var firstShownDay = -1;

            dataStore.SyncData($"{prefix}_day", ref dayCreated);
            dataStore.SyncData($"{prefix}_cat", ref category);
            dataStore.SyncData($"{prefix}_key", ref headlineKey);
            dataStore.SyncData($"{prefix}_story", ref storyKey);
            dataStore.SyncData($"{prefix}_type", ref type);
            dataStore.SyncData($"{prefix}_conf", ref confidence);
            dataStore.SyncData($"{prefix}_minDays", ref minDisplayDays);
            dataStore.SyncData($"{prefix}_shownDay", ref firstShownDay);

            // Load placeholder values
            var placeholderCount = 0;
            dataStore.SyncData($"{prefix}_phCount", ref placeholderCount);

            var placeholders = new Dictionary<string, string>();
            for (var i = 0; i < placeholderCount; i++)
            {
                var phKey = string.Empty;
                var phVal = string.Empty;
                dataStore.SyncData($"{prefix}_ph_{i}_k", ref phKey);
                dataStore.SyncData($"{prefix}_ph_{i}_v", ref phVal);
                if (!string.IsNullOrEmpty(phKey))
                {
                    placeholders[phKey] = phVal;
                }
            }

            return new DispatchItem
            {
                DayCreated = dayCreated,
                Category = category,
                HeadlineKey = headlineKey,
                PlaceholderValues = placeholders,
                StoryKey = storyKey,
                Type = (DispatchType)type,
                Confidence = confidence,
                MinDisplayDays = Math.Max(1, minDisplayDays),
                FirstShownDay = firstShownDay
            };
        }

        /// <summary>
        /// Saves a battle snapshot to the data store.
        /// </summary>
        private static void SaveBattleSnapshot(IDataStore dataStore, string prefix, BattleSnapshot snapshot)
        {
            var mapEventId = snapshot.MapEventId ?? string.Empty;
            var attackerStrength = snapshot.AttackerInitialStrength;
            var defenderStrength = snapshot.DefenderInitialStrength;

            dataStore.SyncData($"{prefix}_id", ref mapEventId);
            dataStore.SyncData($"{prefix}_atk", ref attackerStrength);
            dataStore.SyncData($"{prefix}_def", ref defenderStrength);
        }

        /// <summary>
        /// Loads a battle snapshot from the data store.
        /// </summary>
        private static BattleSnapshot LoadBattleSnapshot(IDataStore dataStore, string prefix)
        {
            var mapEventId = string.Empty;
            var attackerStrength = 0;
            var defenderStrength = 0;

            dataStore.SyncData($"{prefix}_id", ref mapEventId);
            dataStore.SyncData($"{prefix}_atk", ref attackerStrength);
            dataStore.SyncData($"{prefix}_def", ref defenderStrength);

            return new BattleSnapshot
            {
                MapEventId = mapEventId,
                AttackerInitialStrength = attackerStrength,
                DefenderInitialStrength = defenderStrength
            };
        }

        /// <summary>
        /// Saves an order outcome record to the data store.
        /// </summary>
        private static void SaveOrderOutcomeRecord(IDataStore dataStore, string prefix, OrderOutcomeRecord record)
        {
            var orderTitle = record.OrderTitle ?? string.Empty;
            var success = record.Success;
            var briefSummary = record.BriefSummary ?? string.Empty;
            var detailedSummary = record.DetailedSummary ?? string.Empty;
            var issuer = record.Issuer ?? string.Empty;
            var dayNumber = record.DayNumber;

            dataStore.SyncData($"{prefix}_title", ref orderTitle);
            dataStore.SyncData($"{prefix}_success", ref success);
            dataStore.SyncData($"{prefix}_brief", ref briefSummary);
            dataStore.SyncData($"{prefix}_detail", ref detailedSummary);
            dataStore.SyncData($"{prefix}_issuer", ref issuer);
            dataStore.SyncData($"{prefix}_day", ref dayNumber);
        }

        /// <summary>
        /// Loads an order outcome record from the data store.
        /// </summary>
        private static OrderOutcomeRecord LoadOrderOutcomeRecord(IDataStore dataStore, string prefix)
        {
            var orderTitle = string.Empty;
            var success = false;
            var briefSummary = string.Empty;
            var detailedSummary = string.Empty;
            var issuer = string.Empty;
            var dayNumber = 0;

            dataStore.SyncData($"{prefix}_title", ref orderTitle);
            dataStore.SyncData($"{prefix}_success", ref success);
            dataStore.SyncData($"{prefix}_brief", ref briefSummary);
            dataStore.SyncData($"{prefix}_detail", ref detailedSummary);
            dataStore.SyncData($"{prefix}_issuer", ref issuer);
            dataStore.SyncData($"{prefix}_day", ref dayNumber);

            return new OrderOutcomeRecord
            {
                OrderTitle = orderTitle,
                Success = success,
                BriefSummary = briefSummary,
                DetailedSummary = detailedSummary,
                Issuer = issuer,
                DayNumber = dayNumber
            };
        }

        /// <summary>
        /// Saves a reputation change record to the data store.
        /// </summary>
        private static void SaveReputationChangeRecord(IDataStore dataStore, string prefix, ReputationChangeRecord record)
        {
            var target = record.Target ?? string.Empty;
            var delta = record.Delta;
            var newValue = record.NewValue;
            var message = record.Message ?? string.Empty;
            var dayNumber = record.DayNumber;

            dataStore.SyncData($"{prefix}_target", ref target);
            dataStore.SyncData($"{prefix}_delta", ref delta);
            dataStore.SyncData($"{prefix}_newVal", ref newValue);
            dataStore.SyncData($"{prefix}_msg", ref message);
            dataStore.SyncData($"{prefix}_day", ref dayNumber);
        }

        /// <summary>
        /// Loads a reputation change record from the data store.
        /// </summary>
        private static ReputationChangeRecord LoadReputationChangeRecord(IDataStore dataStore, string prefix)
        {
            var target = string.Empty;
            var delta = 0;
            var newValue = 0;
            var message = string.Empty;
            var dayNumber = 0;

            dataStore.SyncData($"{prefix}_target", ref target);
            dataStore.SyncData($"{prefix}_delta", ref delta);
            dataStore.SyncData($"{prefix}_newVal", ref newValue);
            dataStore.SyncData($"{prefix}_msg", ref message);
            dataStore.SyncData($"{prefix}_day", ref dayNumber);

            return new ReputationChangeRecord
            {
                Target = target,
                Delta = delta,
                NewValue = newValue,
                Message = message,
                DayNumber = dayNumber
            };
        }

        /// <summary>
        /// Saves a company need change record to the data store.
        /// </summary>
        private static void SaveCompanyNeedChangeRecord(IDataStore dataStore, string prefix, CompanyNeedChangeRecord record)
        {
            var need = record.Need ?? string.Empty;
            var delta = record.Delta;
            var oldValue = record.OldValue;
            var newValue = record.NewValue;
            var message = record.Message ?? string.Empty;
            var dayNumber = record.DayNumber;

            dataStore.SyncData($"{prefix}_need", ref need);
            dataStore.SyncData($"{prefix}_delta", ref delta);
            dataStore.SyncData($"{prefix}_oldVal", ref oldValue);
            dataStore.SyncData($"{prefix}_newVal", ref newValue);
            dataStore.SyncData($"{prefix}_msg", ref message);
            dataStore.SyncData($"{prefix}_day", ref dayNumber);
        }

        /// <summary>
        /// Loads a company need change record from the data store.
        /// </summary>
        private static CompanyNeedChangeRecord LoadCompanyNeedChangeRecord(IDataStore dataStore, string prefix)
        {
            var need = string.Empty;
            var delta = 0;
            var oldValue = 0;
            var newValue = 0;
            var message = string.Empty;
            var dayNumber = 0;

            dataStore.SyncData($"{prefix}_need", ref need);
            dataStore.SyncData($"{prefix}_delta", ref delta);
            dataStore.SyncData($"{prefix}_oldVal", ref oldValue);
            dataStore.SyncData($"{prefix}_newVal", ref newValue);
            dataStore.SyncData($"{prefix}_msg", ref message);
            dataStore.SyncData($"{prefix}_day", ref dayNumber);

            return new CompanyNeedChangeRecord
            {
                Need = need,
                Delta = delta,
                OldValue = oldValue,
                NewValue = newValue,
                Message = message,
                DayNumber = dayNumber
            };
        }

        /// <summary>
        /// Saves an event outcome record to the data store.
        /// </summary>
        private static void SaveEventOutcomeRecord(IDataStore dataStore, string prefix, EventOutcomeRecord record)
        {
            var eventId = record.EventId ?? string.Empty;
            var eventTitle = record.EventTitle ?? string.Empty;
            var optionChosen = record.OptionChosen ?? string.Empty;
            var outcomeSummary = record.OutcomeSummary ?? string.Empty;
            var dayNumber = record.DayNumber;

            dataStore.SyncData($"{prefix}_eventId", ref eventId);
            dataStore.SyncData($"{prefix}_title", ref eventTitle);
            dataStore.SyncData($"{prefix}_option", ref optionChosen);
            dataStore.SyncData($"{prefix}_summary", ref outcomeSummary);
            dataStore.SyncData($"{prefix}_day", ref dayNumber);

            // Save effects dictionary as key-value pairs
            var effectsCount = record.EffectsApplied?.Count ?? 0;
            dataStore.SyncData($"{prefix}_effectsCount", ref effectsCount);

            if (record.EffectsApplied != null)
            {
                var i = 0;
                foreach (var kvp in record.EffectsApplied)
                {
                    var key = kvp.Key;
                    var value = kvp.Value;
                    dataStore.SyncData($"{prefix}_eff_{i}_k", ref key);
                    dataStore.SyncData($"{prefix}_eff_{i}_v", ref value);
                    i++;
                }
            }
        }

        /// <summary>
        /// Loads an event outcome record from the data store.
        /// </summary>
        private static EventOutcomeRecord LoadEventOutcomeRecord(IDataStore dataStore, string prefix)
        {
            var eventId = string.Empty;
            var eventTitle = string.Empty;
            var optionChosen = string.Empty;
            var outcomeSummary = string.Empty;
            var dayNumber = 0;

            dataStore.SyncData($"{prefix}_eventId", ref eventId);
            dataStore.SyncData($"{prefix}_title", ref eventTitle);
            dataStore.SyncData($"{prefix}_option", ref optionChosen);
            dataStore.SyncData($"{prefix}_summary", ref outcomeSummary);
            dataStore.SyncData($"{prefix}_day", ref dayNumber);

            // Load effects dictionary
            var effectsCount = 0;
            dataStore.SyncData($"{prefix}_effectsCount", ref effectsCount);

            var effects = new Dictionary<string, int>();
            for (var i = 0; i < effectsCount; i++)
            {
                var key = string.Empty;
                var value = 0;
                dataStore.SyncData($"{prefix}_eff_{i}_k", ref key);
                dataStore.SyncData($"{prefix}_eff_{i}_v", ref value);
                if (!string.IsNullOrEmpty(key))
                {
                    effects[key] = value;
                }
            }

            return new EventOutcomeRecord
            {
                EventId = eventId,
                EventTitle = eventTitle,
                OptionChosen = optionChosen,
                OutcomeSummary = outcomeSummary,
                DayNumber = dayNumber,
                EffectsApplied = effects
            };
        }

        /// <summary>
        /// Saves a pending event record to the data store.
        /// </summary>
        private static void SavePendingEventRecord(IDataStore dataStore, string prefix, PendingEventRecord record)
        {
            var sourceEventId = record.SourceEventId ?? string.Empty;
            var chainEventId = record.ChainEventId ?? string.Empty;
            var contextHint = record.ContextHint ?? string.Empty;
            var scheduledDay = record.ScheduledDay;
            var createdDay = record.CreatedDay;

            dataStore.SyncData($"{prefix}_sourceId", ref sourceEventId);
            dataStore.SyncData($"{prefix}_chainId", ref chainEventId);
            dataStore.SyncData($"{prefix}_hint", ref contextHint);
            dataStore.SyncData($"{prefix}_scheduled", ref scheduledDay);
            dataStore.SyncData($"{prefix}_created", ref createdDay);
        }

        /// <summary>
        /// Loads a pending event record from the data store.
        /// </summary>
        private static PendingEventRecord LoadPendingEventRecord(IDataStore dataStore, string prefix)
        {
            var sourceEventId = string.Empty;
            var chainEventId = string.Empty;
            var contextHint = string.Empty;
            var scheduledDay = 0;
            var createdDay = 0;

            dataStore.SyncData($"{prefix}_sourceId", ref sourceEventId);
            dataStore.SyncData($"{prefix}_chainId", ref chainEventId);
            dataStore.SyncData($"{prefix}_hint", ref contextHint);
            dataStore.SyncData($"{prefix}_scheduled", ref scheduledDay);
            dataStore.SyncData($"{prefix}_created", ref createdDay);

            return new PendingEventRecord
            {
                SourceEventId = sourceEventId,
                ChainEventId = chainEventId,
                ContextHint = contextHint,
                ScheduledDay = scheduledDay,
                CreatedDay = createdDay
            };
        }

        #endregion
    }

    /// <summary>
    /// Represents a single news dispatch item.
    /// Uses primitive-friendly structure for save/load compatibility.
    /// </summary>
    public struct DispatchItem
    {
        /// <summary>
        /// Campaign day when this item was created.
        /// </summary>
        public int DayCreated { get; set; }

        /// <summary>
        /// Category for filtering/grouping (e.g., "war", "battle", "prisoner", "siege").
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// Localization string ID (e.g., "News_Victory", "News_Peace").
        /// </summary>
        public string HeadlineKey { get; set; }

        /// <summary>
        /// Placeholder values for localization (e.g., {"WINNER": "Derthert", "PLACE": "Marunath"}).
        /// </summary>
        public Dictionary<string, string> PlaceholderValues { get; set; }

        /// <summary>
        /// Story key for deduplication (e.g., "siege:town_1", "prisoner:hero_123").
        /// </summary>
        public string StoryKey { get; set; }

        /// <summary>
        /// Type of dispatch (Report, Rumor, Bulletin).
        /// </summary>
        public DispatchType Type { get; set; }

        /// <summary>
        /// Confidence level 0-100 (primarily for rumors).
        /// </summary>
        public int Confidence { get; set; }

        /// <summary>
        /// Minimum number of whole days this item must stay visible once it is shown.
        /// 1 = at least today; 2 = today and tomorrow.
        /// </summary>
        public int MinDisplayDays { get; set; }

        /// <summary>
        /// The first campaign day this item was displayed in a menu.
        /// -1 means it has not been shown yet (backlogged).
        /// </summary>
        public int FirstShownDay { get; set; }
    }

    /// <summary>
    /// Type of news dispatch.
    /// </summary>
    public enum DispatchType
    {
        /// <summary>
        /// Factual report derived from a campaign event.
        /// </summary>
        Report = 0,

        /// <summary>
        /// Unconfirmed rumor that may be confirmed/denied later.
        /// </summary>
        Rumor = 1,

        /// <summary>
        /// Periodic summary bulletin (every 2 days).
        /// </summary>
        Bulletin = 2
    }

    /// <summary>
    /// Cached battle state for pyrrhic victory detection.
    /// Stores initial troop strengths to calculate losses after battle ends.
    /// </summary>
    public struct BattleSnapshot
    {
        /// <summary>
        /// Unique identifier for the map event (hash code as string).
        /// </summary>
        public string MapEventId { get; set; }

        /// <summary>
        /// Attacker side troop count at battle start.
        /// </summary>
        public int AttackerInitialStrength { get; set; }

        /// <summary>
        /// Defender side troop count at battle start.
        /// </summary>
        public int DefenderInitialStrength { get; set; }
    }

    /// <summary>
    /// Records an order outcome for display in reports and daily brief.
    /// </summary>
    public sealed class OrderOutcomeRecord
    {
        public string OrderTitle { get; set; }
        public bool Success { get; set; }
        public string BriefSummary { get; set; }
        public string DetailedSummary { get; set; }
        public string Issuer { get; set; }
        public int DayNumber { get; set; }
    }

    /// <summary>
    /// Records a reputation change for display in reports.
    /// </summary>
    public sealed class ReputationChangeRecord
    {
        public string Target { get; set; } // "Lord", "Officer", "Soldier"
        public int Delta { get; set; }
        public int NewValue { get; set; }
        public string Message { get; set; }
        public int DayNumber { get; set; }
    }

    /// <summary>
    /// Records a company need change for display in reports.
    /// </summary>
    public sealed class CompanyNeedChangeRecord
    {
        public string Need { get; set; }
        public int Delta { get; set; }
        public int OldValue { get; set; }
        public int NewValue { get; set; }
        public string Message { get; set; }
        public int DayNumber { get; set; }
    }

    /// <summary>
    /// Records an event outcome for display in Personal Feed and reports.
    /// Tracks what event fired, which option was chosen, and effects applied.
    /// </summary>
    public sealed class EventOutcomeRecord
    {
        public string EventId { get; set; }
        public string EventTitle { get; set; }
        public string OptionChosen { get; set; }
        public string OutcomeSummary { get; set; }
        public int DayNumber { get; set; }

        /// <summary>
        /// Individual effect values applied (for headline formatting).
        /// Keys: "Gold", "SoldierRep", "OfficerRep", "LordRep", "Scrutiny", skill names for XP.
        /// </summary>
        public Dictionary<string, int> EffectsApplied { get; set; }
    }

    /// <summary>
    /// Tracks a pending chain event for context display in Daily Brief.
    /// Shows hints like "A comrade owes you money" before the event fires.
    /// </summary>
    public sealed class PendingEventRecord
    {
        public string SourceEventId { get; set; }
        public string ChainEventId { get; set; }
        public string ContextHint { get; set; }
        public int ScheduledDay { get; set; }
        public int CreatedDay { get; set; }
    }

    /// <summary>
    /// Records muster outcomes for display in camp news and daily brief.
    /// Each muster cycle generates one record summarizing pay, rations, and unit status.
    /// </summary>
    public sealed class MusterOutcomeRecord
    {
        /// <summary>Day number when this muster occurred.</summary>
        public int DayNumber { get; set; }

        /// <summary>Pay outcome: "paid", "partial", "delayed", "promissory", "corruption", etc.</summary>
        public string PayOutcome { get; set; } = string.Empty;

        /// <summary>Amount of gold received (0 if none).</summary>
        public int PayAmount { get; set; }

        /// <summary>Ration outcome: "issued", "none_low_supply", "none_critical", "officer_exempt".</summary>
        public string RationOutcome { get; set; } = string.Empty;

        /// <summary>Item ID of ration issued (empty if none).</summary>
        public string RationItemId { get; set; } = string.Empty;

        /// <summary>QM reputation at time of issue (affects ration quality).</summary>
        public int QmReputation { get; set; }

        /// <summary>Company supply level at muster (0-100).</summary>
        public int SupplyLevel { get; set; }

        /// <summary>Number of soldiers lost since previous muster.</summary>
        public int LostSinceLast { get; set; }

        /// <summary>Number of soldiers sick since previous muster.</summary>
        public int SickSinceLast { get; set; }

        /// <summary>Flavor text shown to player about the ration.</summary>
        public string RationFlavorText { get; set; } = string.Empty;
    }
}
