using System;
using System.Collections.Generic;
using Enlisted.Features.CommandTent.Data;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Mod.Core.Logging;
using Enlisted.Mod.Core.Util;

namespace Enlisted.Features.CommandTent.Core
{
    /// <summary>
    ///     Manages service records for all factions and the player's retinue state.
    ///     Handles CRUD operations, event-driven updates, and serialization.
    /// </summary>
    public sealed class ServiceRecordManager : CampaignBehaviorBase
    {
        private const string LogCategory = "ServiceRecords";

        // Retinue system state
        private string _currentFactionId;
        private string _currentLordId;

        private int _currentTermBattles;
        private int _currentTermKills;

        private Dictionary<string, FactionServiceRecord> _factionRecords = new();

        // Track if we've shown the Tier 4 leadership notification this session
        private bool _shownLeadershipNotification;

        public ServiceRecordManager()
        {
            Instance = this;
            RetinueManager = new RetinueManager(RetinueState);
        }

        public static ServiceRecordManager Instance { get; private set; }

        /// <summary>Lifetime service totals across all factions.</summary>
        [UsedImplicitly]
        public LifetimeServiceRecord LifetimeRecord { get; } = new();

        /// <summary>Battles fought in current enlistment term.</summary>
        [UsedImplicitly]
        public int CurrentTermBattles => _currentTermBattles;

        /// <summary>Kills in current enlistment term.</summary>
        [UsedImplicitly]
        public int CurrentTermKills => _currentTermKills;

        /// <summary>Gets the retinue manager for soldier management operations.</summary>
        public RetinueManager RetinueManager { get; private set; }

        /// <summary>Gets the current retinue state for UI display.</summary>
        public RetinueState RetinueState { get; } = new();

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
            CampaignEvents.MapEventEnded.AddNonSerializedListener(this, OnMapEventEnded);

            EnlistmentBehavior.OnEnlisted += HandleEnlistmentStarted;
            EnlistmentBehavior.OnDischarged += HandleEnlistmentEnded;
            EnlistmentBehavior.OnPromoted += HandlePromotion;
        }

        public override void SyncData(IDataStore dataStore)
        {
            var lifetimeKills = LifetimeRecord.LifetimeKills;
            var totalDays = LifetimeRecord.TotalDaysServed;
            var termsComplete = LifetimeRecord.TermsCompleted;
            var totalEnlist = LifetimeRecord.TotalEnlistments;
            var totalBattles = LifetimeRecord.TotalBattlesFought;

            dataStore.SyncData("svc_lifetimeKills", ref lifetimeKills);
            dataStore.SyncData("svc_totalDaysServed", ref totalDays);
            dataStore.SyncData("svc_termsCompleted", ref termsComplete);
            dataStore.SyncData("svc_totalEnlistments", ref totalEnlist);
            dataStore.SyncData("svc_totalBattles", ref totalBattles);

            if (dataStore.IsLoading)
            {
                LifetimeRecord.LifetimeKills = lifetimeKills;
                LifetimeRecord.TotalDaysServed = totalDays;
                LifetimeRecord.TermsCompleted = termsComplete;
                LifetimeRecord.TotalEnlistments = totalEnlist;
                LifetimeRecord.TotalBattlesFought = totalBattles;
            }

            var factionsServedList = LifetimeRecord.FactionsServed ?? new List<string>();
            var factionCount = factionsServedList.Count;
            dataStore.SyncData("svc_factionServedCount", ref factionCount);

            if (dataStore.IsLoading)
            {
                factionsServedList.Clear();
                for (var i = 0; i < factionCount; i++)
                {
                    var factionId = string.Empty;
                    dataStore.SyncData($"svc_factionServed_{i}", ref factionId);
                    if (!string.IsNullOrEmpty(factionId))
                    {
                        factionsServedList.Add(factionId);
                    }
                }

                LifetimeRecord.FactionsServed = factionsServedList;
            }
            else
            {
                for (var i = 0; i < factionCount; i++)
                {
                    var factionId = factionsServedList[i];
                    dataStore.SyncData($"svc_factionServed_{i}", ref factionId);
                }
            }

            dataStore.SyncData("svc_currentTermBattles", ref _currentTermBattles);
            dataStore.SyncData("svc_currentTermKills", ref _currentTermKills);
            dataStore.SyncData("svc_currentFactionId", ref _currentFactionId);
            dataStore.SyncData("svc_currentLordId", ref _currentLordId);

            SerializeFactionRecords(dataStore);
            SerializeRetinueState(dataStore);

            if (dataStore.IsLoading)
            {
                // Reinitialize retinue manager with loaded state
                RetinueManager = new RetinueManager(RetinueState);
                ModLogger.Debug(LogCategory,
                    $"Loaded {_factionRecords.Count} faction records, lifetime: {LifetimeRecord}");
                ModLogger.Debug("Retinue", $"Loaded retinue state: {RetinueState}");
            }
        }

        /// <summary>
        ///     Serializes retinue state to/from save data.
        /// </summary>
        private void SerializeRetinueState(IDataStore dataStore)
        {
            // Selected type
            var typeId = RetinueState.SelectedTypeId ?? string.Empty;
            dataStore.SyncData("ret_selectedType", ref typeId);

            // Trickle tracking
            var daysSinceTrickle = RetinueState.DaysSinceLastTrickle;
            dataStore.SyncData("ret_trickleDays", ref daysSinceTrickle);

            // Requisition cooldown - CampaignTime can be serialized directly
            var cooldownEnd = RetinueState.RequisitionCooldownEnd;
            dataStore.SyncData("ret_reqCooldown", ref cooldownEnd);

            // Troop counts dictionary
            var troopCount = RetinueState.TroopCounts?.Count ?? 0;
            dataStore.SyncData("ret_troopCount", ref troopCount);

            if (dataStore.IsLoading)
            {
                RetinueState.SelectedTypeId = string.IsNullOrEmpty(typeId) ? null : typeId;
                RetinueState.DaysSinceLastTrickle = daysSinceTrickle;
                RetinueState.RequisitionCooldownEnd = cooldownEnd;
                RetinueState.TroopCounts = new Dictionary<string, int>();

                for (var i = 0; i < troopCount; i++)
                {
                    var troopId = string.Empty;
                    var count = 0;
                    dataStore.SyncData($"ret_troop_{i}_id", ref troopId);
                    dataStore.SyncData($"ret_troop_{i}_count", ref count);

                    if (!string.IsNullOrEmpty(troopId) && count > 0)
                    {
                        RetinueState.TroopCounts[troopId] = count;
                    }
                }
            }
            else if (RetinueState.TroopCounts != null)
            {
                var idx = 0;
                foreach (var kvp in RetinueState.TroopCounts)
                {
                    var troopId = kvp.Key;
                    var count = kvp.Value;
                    dataStore.SyncData($"ret_troop_{idx}_id", ref troopId);
                    dataStore.SyncData($"ret_troop_{idx}_count", ref count);
                    idx++;
                }
            }
        }

        private void SerializeFactionRecords(IDataStore dataStore)
        {
            var recordCount = _factionRecords?.Count ?? 0;
            dataStore.SyncData("svc_recordCount", ref recordCount);

            if (dataStore.IsLoading)
            {
                _factionRecords = new Dictionary<string, FactionServiceRecord>();
                for (var i = 0; i < recordCount; i++)
                {
                    var factionId = string.Empty;
                    var factionType = string.Empty;
                    var displayName = string.Empty;
                    var terms = 0;
                    var days = 0;
                    var tier = 0;
                    var battles = 0;
                    var lords = 0;
                    var enlistments = 0;
                    var kills = 0;

                    dataStore.SyncData($"svc_rec_{i}_id", ref factionId);
                    dataStore.SyncData($"svc_rec_{i}_type", ref factionType);
                    dataStore.SyncData($"svc_rec_{i}_name", ref displayName);
                    dataStore.SyncData($"svc_rec_{i}_terms", ref terms);
                    dataStore.SyncData($"svc_rec_{i}_days", ref days);
                    dataStore.SyncData($"svc_rec_{i}_tier", ref tier);
                    dataStore.SyncData($"svc_rec_{i}_battles", ref battles);
                    dataStore.SyncData($"svc_rec_{i}_lords", ref lords);
                    dataStore.SyncData($"svc_rec_{i}_enlist", ref enlistments);
                    dataStore.SyncData($"svc_rec_{i}_kills", ref kills);

                    if (!string.IsNullOrEmpty(factionId))
                    {
                        _factionRecords[factionId] = new FactionServiceRecord(factionId, factionType, displayName)
                        {
                            TermsCompleted = terms,
                            TotalDaysServed = days,
                            HighestTier = tier,
                            BattlesFought = battles,
                            LordsServed = lords,
                            Enlistments = enlistments,
                            TotalKills = kills
                        };
                    }
                }
            }
            else if (_factionRecords != null)
            {
                var idx = 0;
                foreach (var kvp in _factionRecords)
                {
                    var rec = kvp.Value;
                    var factionId = rec.FactionId;
                    var factionType = rec.FactionType;
                    var displayName = rec.FactionDisplayName;
                    var terms = rec.TermsCompleted;
                    var days = rec.TotalDaysServed;
                    var tier = rec.HighestTier;
                    var battles = rec.BattlesFought;
                    var lords = rec.LordsServed;
                    var enlistments = rec.Enlistments;
                    var kills = rec.TotalKills;

                    dataStore.SyncData($"svc_rec_{idx}_id", ref factionId);
                    dataStore.SyncData($"svc_rec_{idx}_type", ref factionType);
                    dataStore.SyncData($"svc_rec_{idx}_name", ref displayName);
                    dataStore.SyncData($"svc_rec_{idx}_terms", ref terms);
                    dataStore.SyncData($"svc_rec_{idx}_days", ref days);
                    dataStore.SyncData($"svc_rec_{idx}_tier", ref tier);
                    dataStore.SyncData($"svc_rec_{idx}_battles", ref battles);
                    dataStore.SyncData($"svc_rec_{idx}_lords", ref lords);
                    dataStore.SyncData($"svc_rec_{idx}_enlist", ref enlistments);
                    dataStore.SyncData($"svc_rec_{idx}_kills", ref kills);
                    idx++;
                }
            }
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            ModLogger.Info(LogCategory, $"Service records initialized ({_factionRecords.Count} faction records)");
        }

        private void OnDailyTick()
        {
            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment?.IsEnlisted != true)
            {
                return;
            }

            OnDayServed(enlistment.EnlistmentTier);
        }

        private void OnMapEventEnded(MapEvent mapEvent)
        {
            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment?.IsEnlisted != true)
            {
                return;
            }

            var mainParty = MobileParty.MainParty?.Party;
            if (mainParty == null)
            {
                return;
            }

            var playerParticipated = mapEvent.InvolvedParties.Contains(mainParty);
            if (playerParticipated)
            {
                OnBattleFought();
            }
        }

        private void HandleEnlistmentStarted(Hero lord)
        {
            if (lord == null)
            {
                return;
            }

            var enlistment = EnlistmentBehavior.Instance;
            var tier = enlistment?.EnlistmentTier ?? 1;
            OnEnlistmentStarted(lord, tier);
        }

        private void HandleEnlistmentEnded(string reason)
        {
            var isTermComplete = reason != null &&
                                 (reason.Contains("retirement") ||
                                  reason.Contains("honorable") ||
                                  reason.Contains("completed"));

            if (isTermComplete)
            {
                OnTermCompleted();
            }

            // Check if player chose to keep their troops on retirement
            if (EnlistmentBehavior.RetainTroopsOnRetirement)
            {
                // Clear only the tracking state - troops stay in party as regular members
                RetinueState?.Clear();
                ModLogger.Info(LogCategory, "Retinue tracking cleared - troops retained as regular party members");

                // Reset the flag after use
                EnlistmentBehavior.RetainTroopsOnRetirement = false;
            }
            else
            {
                // Clear retinue when enlistment ends (default behavior)
                RetinueManager?.ClearRetinueTroops("enlistment_end");
            }

            OnEnlistmentEnded();
        }

        /// <summary>
        ///     Called when player receives a promotion. Shows leadership notification at Tier 4.
        /// </summary>
        private void HandlePromotion(int newTier)
        {
            try
            {
                // Show Tier 4 leadership notification only once per session
                if (newTier >= RetinueManager.LanceTier && !_shownLeadershipNotification)
                {
                    _shownLeadershipNotification = true;
                    RetinueManager.ShowLeadershipNotification();
                    ModLogger.Info(LogCategory, $"Leadership notification shown for Tier {newTier}");
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, $"Error handling promotion for retinue: {ex.Message}");
            }
        }

        #region Faction Key Generation

        /// <summary>Generates unique record key from faction. Uses StringId with type prefix.</summary>
        public static string GetFactionRecordKey(IFaction faction)
        {
            if (faction == null)
            {
                return "unknown_null";
            }

            if (faction is Kingdom kingdom)
            {
                return $"kingdom_{kingdom.StringId}";
            }

            if (faction is Clan clan)
            {
                if (clan.IsBanditFaction)
                {
                    return $"bandit_{clan.StringId}";
                }

                if (clan.IsClanTypeMercenary)
                {
                    return $"merc_{clan.StringId}";
                }

                if (clan.IsMinorFaction)
                {
                    return $"minor_{clan.StringId}";
                }

                return $"clan_{clan.StringId}";
            }

            return $"unknown_{faction.StringId}";
        }

        /// <summary>Gets faction type string from faction.</summary>
        public static string GetFactionType(IFaction faction)
        {
            if (faction == null)
            {
                return "unknown";
            }

            if (faction is Kingdom)
            {
                return "kingdom";
            }

            if (faction is Clan clan)
            {
                if (clan.IsBanditFaction)
                {
                    return "bandit";
                }

                if (clan.IsClanTypeMercenary)
                {
                    return "merc";
                }

                if (clan.IsMinorFaction)
                {
                    return "minor";
                }

                return "clan";
            }

            return "unknown";
        }

        /// <summary>Gets display name from faction.</summary>
        public static string GetFactionDisplayName(IFaction faction)
        {
            return faction?.Name?.ToString() ?? "Unknown Faction";
        }

        #endregion

        #region Record Access

        /// <summary>Gets or creates a faction record.</summary>
        public FactionServiceRecord GetOrCreateRecord(IFaction faction)
        {
            if (faction == null)
            {
                return null;
            }

            var key = GetFactionRecordKey(faction);
            if (_factionRecords.TryGetValue(key, out var existing))
            {
                return existing;
            }

            var record = new FactionServiceRecord(key, GetFactionType(faction), GetFactionDisplayName(faction));
            _factionRecords[key] = record;
            ModLogger.Debug(LogCategory, $"Created new faction record: {key}");
            return record;
        }

        /// <summary>Gets faction record by key, or null if not found.</summary>
        public FactionServiceRecord GetRecord(string factionKey)
        {
            return _factionRecords.TryGetValue(factionKey, out var record) ? record : null;
        }

        /// <summary>Gets all faction records for UI display.</summary>
        [UsedImplicitly]
        public IReadOnlyDictionary<string, FactionServiceRecord> GetAllRecords()
        {
            return _factionRecords;
        }

        /// <summary>Gets current faction record if enlisted.</summary>
        public FactionServiceRecord GetCurrentFactionRecord()
        {
            return string.IsNullOrEmpty(_currentFactionId) ? null : GetRecord(_currentFactionId);
        }

        #endregion

        #region Event Handlers (Called by EnlistmentBehavior)

        /// <summary>Called when player starts a new enlistment.</summary>
        public void OnEnlistmentStarted(Hero lord, int startingTier)
        {
            if (lord?.Clan?.MapFaction == null)
            {
                ModLogger.Warn(LogCategory, "OnEnlistmentStarted: Invalid lord or faction");
                return;
            }

            var faction = lord.Clan.MapFaction;
            var record = GetOrCreateRecord(faction);
            var lordId = lord.StringId;

            var isNewLord = _currentLordId != lordId;
            if (isNewLord && !string.IsNullOrEmpty(lordId))
            {
                record.LordsServed++;
            }

            record.Enlistments++;
            record.UpdateHighestTier(startingTier);

            LifetimeRecord.TotalEnlistments++;
            LifetimeRecord.AddFactionServed(record.FactionId);

            _currentTermBattles = 0;
            _currentTermKills = 0;
            _currentFactionId = record.FactionId;
            _currentLordId = lordId;

            ModLogger.Info(LogCategory, $"Enlistment started: {record.FactionDisplayName}, lord={lord.Name}, " +
                                        $"enlistments={record.Enlistments}, lords={record.LordsServed}");
        }

        /// <summary>Called when player completes a full term (honorable discharge).</summary>
        public void OnTermCompleted()
        {
            var record = GetCurrentFactionRecord();
            if (record == null)
            {
                return;
            }

            record.TermsCompleted++;
            LifetimeRecord.TermsCompleted++;

            ModLogger.Info(LogCategory,
                $"Term completed: {record.FactionDisplayName}, total terms={record.TermsCompleted}");
        }

        /// <summary>Called when enlistment ends (any reason).</summary>
        public void OnEnlistmentEnded()
        {
            _currentFactionId = null;
            _currentLordId = null;
            ModLogger.Debug(LogCategory, "Enlistment ended, current faction cleared");
        }

        /// <summary>Called on daily tick while enlisted.</summary>
        public void OnDayServed(int currentTier)
        {
            var record = GetCurrentFactionRecord();
            if (record == null)
            {
                return;
            }

            record.TotalDaysServed++;
            record.UpdateHighestTier(currentTier);
            LifetimeRecord.TotalDaysServed++;
        }

        /// <summary>Called when player participates in a battle.</summary>
        public void OnBattleFought()
        {
            var record = GetCurrentFactionRecord();
            if (record == null)
            {
                return;
            }

            record.BattlesFought++;
            _currentTermBattles++;
            LifetimeRecord.TotalBattlesFought++;

            ModLogger.Debug(LogCategory,
                $"Battle recorded: term={_currentTermBattles}, faction total={record.BattlesFought}");
        }

        /// <summary>Called when player gets kills in battle.</summary>
        public void OnKillsRecorded(int killCount)
        {
            if (killCount <= 0)
            {
                return;
            }

            var record = GetCurrentFactionRecord();
            if (record != null)
            {
                record.TotalKills += killCount;
            }

            _currentTermKills += killCount;
            LifetimeRecord.LifetimeKills += killCount;

            ModLogger.Debug(LogCategory, $"Kills recorded: {killCount}, term total={_currentTermKills}, " +
                                         $"lifetime={LifetimeRecord.LifetimeKills}");
        }

        /// <summary>Called when player's tier changes (for updating highest tier).</summary>
        [UsedImplicitly]
        public void OnTierChanged(int newTier)
        {
            var record = GetCurrentFactionRecord();
            record?.UpdateHighestTier(newTier);
        }

        #endregion
    }
}
