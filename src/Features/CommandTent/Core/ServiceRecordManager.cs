using System;
using System.Collections.Generic;
using System.Linq;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Features.CommandTent.Data;
using Enlisted.Mod.Core.Logging;
using Enlisted.Mod.Core.Util;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;

namespace Enlisted.Features.CommandTent.Core
{
    /// <summary>
    /// Manages service records for all factions and the player's retinue state.
    /// Handles CRUD operations, event-driven updates, and serialization.
    /// </summary>
    public sealed class ServiceRecordManager : CampaignBehaviorBase
    {
        private const string LogCategory = "ServiceRecords";

        public static ServiceRecordManager Instance { get; private set; }

        private Dictionary<string, FactionServiceRecord> _factionRecords = new Dictionary<string, FactionServiceRecord>();
        private readonly LifetimeServiceRecord _lifetimeRecord = new LifetimeServiceRecord();
        private ReservistRecord _reservistRecord = new ReservistRecord();

        // Retinue system state
        private readonly RetinueState _retinueState = new RetinueState();
        private RetinueManager _retinueManager;

        // Track if we've shown the Tier 4 leadership notification this session
        private bool _shownLeadershipNotification;

        private int _currentTermBattles;
        private int _currentTermKills;
        private string _currentFactionId;
        private string _currentLordId;

        /// <summary>Lifetime service totals across all factions.</summary>
        [UsedImplicitly]
        public LifetimeServiceRecord LifetimeRecord => _lifetimeRecord;

        /// <summary>Battles fought in current enlistment term.</summary>
        [UsedImplicitly]
        public int CurrentTermBattles => _currentTermBattles;

        /// <summary>Kills in current enlistment term.</summary>
        [UsedImplicitly]
        public int CurrentTermKills => _currentTermKills;

        /// <summary>Gets the retinue manager for soldier management operations.</summary>
        public RetinueManager RetinueManager => _retinueManager;

        /// <summary>Gets the current retinue state for UI display.</summary>
        public RetinueState RetinueState => _retinueState;

        public ReservistRecord ReservistRecord => _reservistRecord;

        public ServiceRecordManager()
        {
            Instance = this;
            _retinueManager = new RetinueManager(_retinueState);
        }

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
            var lifetimeKills = _lifetimeRecord.LifetimeKills;
            var totalDays = _lifetimeRecord.TotalDaysServed;
            var termsComplete = _lifetimeRecord.TermsCompleted;
            var totalEnlist = _lifetimeRecord.TotalEnlistments;
            var totalBattles = _lifetimeRecord.TotalBattlesFought;

            dataStore.SyncData("svc_lifetimeKills", ref lifetimeKills);
            dataStore.SyncData("svc_totalDaysServed", ref totalDays);
            dataStore.SyncData("svc_termsCompleted", ref termsComplete);
            dataStore.SyncData("svc_totalEnlistments", ref totalEnlist);
            dataStore.SyncData("svc_totalBattles", ref totalBattles);

            if (dataStore.IsLoading)
            {
                _lifetimeRecord.LifetimeKills = lifetimeKills;
                _lifetimeRecord.TotalDaysServed = totalDays;
                _lifetimeRecord.TermsCompleted = termsComplete;
                _lifetimeRecord.TotalEnlistments = totalEnlist;
                _lifetimeRecord.TotalBattlesFought = totalBattles;
            }

            var factionsServedList = _lifetimeRecord.FactionsServed ?? new List<string>();
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
                _lifetimeRecord.FactionsServed = factionsServedList;
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

            // Reservist snapshot
            var resLord = _reservistRecord.LastLordId;
            var resFaction = _reservistRecord.LastFactionId;
            var resDays = _reservistRecord.DaysServed;
            var resTier = _reservistRecord.TierAtExit;
            var resXp = _reservistRecord.XpAtExit;
            var resBand = _reservistRecord.DischargeBand;
            var resRel = _reservistRecord.RelationAtExit;
            var resAt = _reservistRecord.RecordedAt;
            var resConsumed = _reservistRecord.Consumed;
            var resGrantedProbation = _reservistRecord.GrantedProbation;

            dataStore.SyncData("svc_reservist_lastLord", ref resLord);
            dataStore.SyncData("svc_reservist_lastFaction", ref resFaction);
            dataStore.SyncData("svc_reservist_daysServed", ref resDays);
            dataStore.SyncData("svc_reservist_tier", ref resTier);
            dataStore.SyncData("svc_reservist_xp", ref resXp);
            dataStore.SyncData("svc_reservist_band", ref resBand);
            dataStore.SyncData("svc_reservist_relation", ref resRel);
            dataStore.SyncData("svc_reservist_recordedAt", ref resAt);
            dataStore.SyncData("svc_reservist_consumed", ref resConsumed);
            dataStore.SyncData("svc_reservist_grantedProbation", ref resGrantedProbation);

            if (dataStore.IsLoading)
            {
                _reservistRecord.LastLordId = resLord;
                _reservistRecord.LastFactionId = resFaction;
                _reservistRecord.DaysServed = resDays;
                _reservistRecord.TierAtExit = resTier;
                _reservistRecord.XpAtExit = resXp;
                _reservistRecord.DischargeBand = resBand;
                _reservistRecord.RelationAtExit = resRel;
                _reservistRecord.RecordedAt = resAt;
                _reservistRecord.Consumed = resConsumed;
                _reservistRecord.GrantedProbation = resGrantedProbation;
            }

            SerializeFactionRecords(dataStore);
            SerializeRetinueState(dataStore);

            if (dataStore.IsLoading)
            {
                // Reinitialize retinue manager with loaded state
                _retinueManager = new RetinueManager(_retinueState);
                ModLogger.Debug(LogCategory, $"Loaded {_factionRecords.Count} faction records, lifetime: {_lifetimeRecord}");
                ModLogger.Debug("Retinue", $"Loaded retinue state: {_retinueState}");
            }
        }

        /// <summary>
        /// Serializes retinue state to/from save data.
        /// </summary>
        private void SerializeRetinueState(IDataStore dataStore)
        {
            // Selected type
            var typeId = _retinueState.SelectedTypeId ?? string.Empty;
            dataStore.SyncData("ret_selectedType", ref typeId);

            // Trickle tracking
            var daysSinceTrickle = _retinueState.DaysSinceLastTrickle;
            dataStore.SyncData("ret_trickleDays", ref daysSinceTrickle);

            // Requisition cooldown - CampaignTime can be serialized directly
            var cooldownEnd = _retinueState.RequisitionCooldownEnd;
            dataStore.SyncData("ret_reqCooldown", ref cooldownEnd);

            // Troop counts dictionary
            var troopCount = _retinueState.TroopCounts?.Count ?? 0;
            dataStore.SyncData("ret_troopCount", ref troopCount);

            if (dataStore.IsLoading)
            {
                _retinueState.SelectedTypeId = string.IsNullOrEmpty(typeId) ? null : typeId;
                _retinueState.DaysSinceLastTrickle = daysSinceTrickle;
                _retinueState.RequisitionCooldownEnd = cooldownEnd;
                _retinueState.TroopCounts = new Dictionary<string, int>();

                for (var i = 0; i < troopCount; i++)
                {
                    var troopId = string.Empty;
                    var count = 0;
                    dataStore.SyncData($"ret_troop_{i}_id", ref troopId);
                    dataStore.SyncData($"ret_troop_{i}_count", ref count);

                    if (!string.IsNullOrEmpty(troopId) && count > 0)
                    {
                        _retinueState.TroopCounts[troopId] = count;
                    }
                }
            }
            else if (_retinueState.TroopCounts != null)
            {
                var idx = 0;
                foreach (var kvp in _retinueState.TroopCounts)
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

        public void RecordReservist(string dischargeBand, int daysServed, int tier, int xp, Hero lord)
        {
            try
            {
                _reservistRecord.DischargeBand = dischargeBand;
                _reservistRecord.DaysServed = daysServed;
                _reservistRecord.TierAtExit = tier;
                _reservistRecord.XpAtExit = xp;
                _reservistRecord.LastLordId = lord?.StringId;
                _reservistRecord.LastFactionId = lord?.MapFaction?.StringId;
                _reservistRecord.RelationAtExit = lord != null ? Hero.MainHero.GetRelation(lord) : 0;
                _reservistRecord.RecordedAt = CampaignTime.Now;
                _reservistRecord.Consumed = false;

                ModLogger.Info(LogCategory,
                    $"Reservist snapshot recorded: band={dischargeBand}, days={daysServed}, tier={tier}, lord={lord?.Name}");
            }
            catch (Exception ex)
            {
                ModLogger.Warn(LogCategory, $"Failed to record reservist snapshot: {ex.Message}");
            }
        }

        public bool TryConsumeReservistForFaction(IFaction faction, out int targetTier, out int bonusXp, out int relationBonus, out string band, out bool probation)
        {
            targetTier = 0;
            bonusXp = 0;
            relationBonus = 0;
            band = "none";
            probation = false;

            try
            {
                if (_reservistRecord == null || _reservistRecord.Consumed)
                {
                    return false;
                }

                if (faction == null || string.IsNullOrWhiteSpace(faction.StringId))
                {
                    return false;
                }

                if (!string.Equals(_reservistRecord.LastFactionId, faction.StringId, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                band = _reservistRecord.DischargeBand?.ToLowerInvariant() ?? "none";

                switch (band)
                {
                    case "washout":
                    case "deserter":
                        targetTier = 1;
                        bonusXp = 0;
                        relationBonus = 0;
                        probation = true;
                        break;
                    case "grace":
                        // BUG FIX: "grace" band means player's lord died/captured but they're re-enlisting
                        // Restore their previous tier and partial XP as a veteran returning to service
                        targetTier = Math.Max(1, _reservistRecord.TierAtExit);
                        bonusXp = _reservistRecord.XpAtExit / 2; // Half XP retained
                        relationBonus = 3;
                        probation = false;
                        ModLogger.Info(LogCategory, 
                            $"Grace re-entry: restoring tier {targetTier} with {bonusXp} bonus XP");
                        break;
                    case "honorable":
                        targetTier = 3;
                        bonusXp = 500;
                        relationBonus = 5;
                        break;
                    case "veteran":
                    case "heroic":
                        targetTier = 4;
                        bonusXp = 1000;
                        relationBonus = 10;
                        break;
                    default:
                        return false;
                }

                _reservistRecord.Consumed = true;
                _reservistRecord.GrantedProbation = probation;
                ModLogger.Info(LogCategory,
                    $"Reservist offer consumed for faction {faction.Name} (band={band}, targetTier={targetTier}, bonusXp={bonusXp}, relBonus={relationBonus}, probation={probation})");
                return true;
            }
            catch (Exception ex)
            {
                ModLogger.Warn(LogCategory, $"Error consuming reservist record: {ex.Message}");
                return false;
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
                _retinueState?.Clear();
                ModLogger.Info(LogCategory, "Retinue tracking cleared - troops retained as regular party members");
                
                // Reset the flag after use
                EnlistmentBehavior.RetainTroopsOnRetirement = false;
            }
            else
            {
                // Clear retinue when enlistment ends (default behavior)
                _retinueManager?.ClearRetinueTroops("enlistment_end");
            }

            OnEnlistmentEnded();
        }

        /// <summary>
        /// Called when player receives a promotion. Shows leadership notification at Tier 4.
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

            _lifetimeRecord.TotalEnlistments++;
            _lifetimeRecord.AddFactionServed(record.FactionId);

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
            _lifetimeRecord.TermsCompleted++;

            ModLogger.Info(LogCategory, $"Term completed: {record.FactionDisplayName}, total terms={record.TermsCompleted}");
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
            _lifetimeRecord.TotalDaysServed++;
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
            _lifetimeRecord.TotalBattlesFought++;

            ModLogger.Debug(LogCategory, $"Battle recorded: term={_currentTermBattles}, faction total={record.BattlesFought}");
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
            _lifetimeRecord.LifetimeKills += killCount;

            ModLogger.Debug(LogCategory, $"Kills recorded: {killCount}, term total={_currentTermKills}, " +
                                         $"lifetime={_lifetimeRecord.LifetimeKills}");
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
