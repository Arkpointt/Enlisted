using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Enlisted.Features.Camp;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Features.Interface.News.Generation;
using Enlisted.Features.Interface.News.Generation.Producers;
using Enlisted.Features.Interface.News.Models;
using Enlisted.Features.Interface.News.State;
using Enlisted.Features.Schedule.Behaviors;
using Enlisted.Features.Lances.Events;
using Enlisted.Features.Lances.Events.Decisions;
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
    /// Read-only observer of campaign events; generates localized headlines.
    ///
    /// Phase 0: Infrastructure and persistence structure.
    /// Phase 1+: Event wiring and news generation.
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
        /// Stored as raw values (without "Company:"/"Lance:"/"Kingdom:" prefixes) so UIs can format as needed.
        /// </summary>
        private string _dailyBriefCompany = string.Empty;
        private string _dailyBriefLance = string.Empty;
        private string _dailyBriefKingdom = string.Empty;

        /// <summary>
        /// Cached battle initial strengths for pyrrhic detection.
        /// Key: MapEvent hash code as string.
        /// </summary>
        private Dictionary<string, BattleSnapshot> _battleSnapshots = new Dictionary<string, BattleSnapshot>();

        /// <summary>
        /// Tracks if the lord had an army yesterday (for army formation detection).
        /// </summary>
        private bool _lordHadArmyYesterday;

        // Camp News (Phase 2): persisted Daily Report + rolling ledger.
        private CampNewsState _campNewsState = new CampNewsState();

        // Phase 5: expose the last generated snapshot to other systems (Decisions) without forcing recomputation.
        // This snapshot contains primitives only; it is safe to cache and share as read-only.
        private DailyReportSnapshot _lastDailyReportSnapshot;
        private int _lastDailyReportSnapshotDayNumber = -1;

        // Phase 4: producer pattern to populate snapshot facts without sprawl.
        private static readonly IDailyReportFactProducer[] DailyReportFactProducers =
        {
            new CompanyMovementObjectiveProducer(),
            new LanceStatusFactProducer(),
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

            ModLogger.Info(LogCategory, "News behavior registered (Phase 5: personal news feed)");
        }

        public override void SyncData(IDataStore dataStore)
        {
            SaveLoadDiagnostics.SafeSyncData(this, dataStore, () =>
            {
                // Sync kingdom feed as individual primitives (lists of complex types require manual handling)
                var kingdomCount = _kingdomFeed?.Count ?? 0;
                dataStore.SyncData("en_news_kingdomCount", ref kingdomCount);

                if (dataStore.IsLoading)
                {
                    _kingdomFeed = new List<DispatchItem>();
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
                var personalCount = _personalFeed?.Count ?? 0;
                dataStore.SyncData("en_news_personalCount", ref personalCount);

                if (dataStore.IsLoading)
                {
                    _personalFeed = new List<DispatchItem>();
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
                _dailyBriefLance ??= string.Empty;
                _dailyBriefKingdom ??= string.Empty;

                dataStore.SyncData("en_news_dailyBriefDay", ref _lastDailyBriefDayNumber);
                dataStore.SyncData("en_news_dailyBriefCompany", ref _dailyBriefCompany);
                dataStore.SyncData("en_news_dailyBriefLance", ref _dailyBriefLance);
                dataStore.SyncData("en_news_dailyBriefKingdom", ref _dailyBriefKingdom);

                // Sync battle snapshots (for pyrrhic detection across save/load)
                var snapshotCount = _battleSnapshots?.Count ?? 0;
                dataStore.SyncData("en_news_snapshotCount", ref snapshotCount);

                if (dataStore.IsLoading)
                {
                    _battleSnapshots = new Dictionary<string, BattleSnapshot>();
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

                // Phase 2: Camp News state (Daily Report archive + ledger)
                _campNewsState ??= new CampNewsState();
                _campNewsState.SyncData(dataStore);

                // Safe initialization for null collections after load
                _kingdomFeed ??= new List<DispatchItem>();
                _personalFeed ??= new List<DispatchItem>();
                _battleSnapshots ??= new Dictionary<string, BattleSnapshot>();

                _dailyBriefCompany ??= string.Empty;
                _dailyBriefLance ??= string.Empty;
                _dailyBriefKingdom ??= string.Empty;

                // Trim feeds to max size
                TrimFeeds();

                ModLogger.Debug(LogCategory, $"News data synced: kingdom={_kingdomFeed.Count}, personal={_personalFeed.Count}");
            });
        }

        #region Public API (Menu Integration)

        /// <summary>
        /// Builds the once-per-day Daily Brief section (Company/Lance/Kingdom).
        /// This replaces the old "Kingdom News" block on the main enlisted menu.
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

                // If generation produced no content (should be rare), hide the section rather than showing empties.
                if (string.IsNullOrWhiteSpace(_dailyBriefCompany) &&
                    string.IsNullOrWhiteSpace(_dailyBriefLance) &&
                    string.IsNullOrWhiteSpace(_dailyBriefKingdom))
                {
                    return string.Empty;
                }

                var sb = new StringBuilder();
                sb.AppendLine(new TextObject("{=en_daily_brief_header}--- Daily Brief (Today) ---").ToString());

                if (!string.IsNullOrWhiteSpace(_dailyBriefCompany))
                {
                    sb.AppendLine($"Company: {_dailyBriefCompany}");
                }

                if (!string.IsNullOrWhiteSpace(_dailyBriefLance))
                {
                    sb.AppendLine($"Lance:   {_dailyBriefLance}");
                }

                if (!string.IsNullOrWhiteSpace(_dailyBriefKingdom))
                {
                    sb.AppendLine($"Kingdom: {_dailyBriefKingdom}");
                }

                return sb.ToString().TrimEnd();
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, "Error building Daily Brief section", ex);
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

                // Select which items are "currently visible" based on priority and backlog rules.
                // - Every item must stay visible for at least 1 day once shown.
                // - Important items stay visible for 2 days (MinDisplayDays = 2).
                // - Items can be replaced by newer updates with the same StoryKey.
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

        #region Event Handlers (Phase 1 Stubs)

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

                // Phase 2: Generate the Daily Report once per in-game day (persisted, stable).
                EnsureDailyReportGenerated();

                CheckForArmyFormation();
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, "Error in OnDailyTick", ex);
            }
        }

        /// <summary>
        /// Phase 2: Ensure we have a persisted Daily Report for the current day.
        /// This does not change UI yet; Phase 3 consumes the record to show an excerpt on the main menu.
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

                // Cache snapshot for other systems (Phase 5 situation flags).
                _lastDailyReportSnapshot = snapshot;
                _lastDailyReportSnapshotDayNumber = dayNumber;

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
        /// Phase 5: provides the current day’s Daily Report snapshot (facts), ensuring it exists first.
        /// Intended for systems like Decisions that need the same “day facts” without duplicating producer logic.
        /// </summary>
        public DailyReportSnapshot GetTodayDailyReportSnapshot()
        {
            try
            {
                EnsureDailyReportGenerated();

                var today = (int)CampaignTime.Now.ToDays;
                if (_lastDailyReportSnapshot != null && _lastDailyReportSnapshotDayNumber == today)
                {
                    return _lastDailyReportSnapshot;
                }

                return _lastDailyReportSnapshot;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Phase 5: posts a short high-signal personal dispatch line (used by decision outcomes).
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
                    Schedule = ScheduleBehavior.Instance,
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
        /// Phase 2 helper: get the latest Daily Report lines (already templated) if available.
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
        /// Phase 2 helper: returns a compact excerpt suitable for a main-menu paragraph.
        /// Phase 3 will switch the enlisted_status display over to this.
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

                // Phase 5.5: append one short "Opportunities" sentence when player decisions are available.
                // This is deliberately dynamic (not persisted) so it reflects resolved decisions without regenerating the Daily Report.
                try
                {
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
                        { "LORD", lord?.Name?.ToString() ?? new TextObject("{=enl_ui_unknown}Unknown").ToString() }
                    };

                    AddPersonalNews("army", "News_ArmyForming", placeholders, $"army:{lord?.StringId}", 2);
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
                     !string.IsNullOrWhiteSpace(_dailyBriefLance) ||
                     !string.IsNullOrWhiteSpace(_dailyBriefKingdom)))
                {
                    return;
                }

                var enlistment = EnlistmentBehavior.Instance;
                if (enlistment?.IsEnlisted != true)
                {
                    _lastDailyBriefDayNumber = currentDay;
                    _dailyBriefCompany = string.Empty;
                    _dailyBriefLance = string.Empty;
                    _dailyBriefKingdom = string.Empty;
                    return;
                }

                _lastDailyBriefDayNumber = currentDay;
                _dailyBriefCompany = BuildDailyCompanyLine(enlistment);
                _dailyBriefLance = BuildDailyLanceLine(enlistment);
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

        private static string BuildDailyLanceLine(EnlistmentBehavior enlistment)
        {
            try
            {
                var lanceName = enlistment?.CurrentLanceName;
                if (string.IsNullOrWhiteSpace(lanceName))
                {
                    return "No lance assigned.";
                }

                // Keep this light and RP-flavored; detailed stats belong in Camp screens.
                var cond = Enlisted.Features.Conditions.PlayerConditionBehavior.Instance;
                if (cond?.IsEnabled() == true && cond.State?.HasAnyCondition == true)
                {
                    if (cond.State.HasInjury)
                    {
                        return $"{lanceName} keeps a slower pace while you recover.";
                    }
                    if (cond.State.HasIllness)
                    {
                        return $"{lanceName} covers for you while sickness runs its course.";
                    }
                }

                if (enlistment != null && enlistment.FatigueMax > 0 && enlistment.FatigueCurrent <= enlistment.FatigueMax / 4)
                {
                    return $"{lanceName} looks worn down after long days.";
                }

                return $"{lanceName} holds steady and in good order.";
            }
            catch
            {
                return "Your lance holds steady.";
            }
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
}
