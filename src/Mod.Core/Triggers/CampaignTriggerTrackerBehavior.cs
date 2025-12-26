using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using Enlisted.Mod.Core.Logging;
namespace Enlisted.Mod.Core.Triggers
{
    /// <summary>
    /// Shared, lightweight campaign signal tracker.
    ///
    /// This tracker provides a consistent vocabulary across systems without heavy
    /// scanning loops. It records a small amount of recent history as timestamps
    /// and IDs, allowing features to check for events such as entering a town or
    /// leaving a battle.
    ///
    /// This data is intentionally minimal to avoid save bloat.
    /// </summary>
    public sealed class CampaignTriggerTrackerBehavior : CampaignBehaviorBase
    {
        private const string LogCategory = "Triggers";

        /// <summary>
        /// Singleton access for lightweight, shared trigger state.
        /// This avoids relying on Campaign.GetCampaignBehavior APIs (which vary across versions).
        /// </summary>
        public static CampaignTriggerTrackerBehavior Instance { get; private set; }

        private CampaignTime _lastSettlementEnteredTime = CampaignTime.Zero;
        private string _lastSettlementEnteredId = string.Empty;
        private CampaignTime _lastSettlementLeftTime = CampaignTime.Zero;
        private string _lastSettlementLeftId = string.Empty;

        private CampaignTime _lastTownEnteredTime = CampaignTime.Zero;
        private CampaignTime _lastCastleEnteredTime = CampaignTime.Zero;
        private CampaignTime _lastVillageEnteredTime = CampaignTime.Zero;

        private CampaignTime _lastMapEventStartedTime = CampaignTime.Zero;
        private CampaignTime _lastMapEventEndedTime = CampaignTime.Zero;
        private string _lastMapEventType = string.Empty;

        public CampaignTime LastSettlementEnteredTime => _lastSettlementEnteredTime;
        public string LastSettlementEnteredId => _lastSettlementEnteredId ?? string.Empty;
        public CampaignTime LastSettlementLeftTime => _lastSettlementLeftTime;
        public string LastSettlementLeftId => _lastSettlementLeftId ?? string.Empty;

        public CampaignTime LastTownEnteredTime => _lastTownEnteredTime;
        public CampaignTime LastCastleEnteredTime => _lastCastleEnteredTime;
        public CampaignTime LastVillageEnteredTime => _lastVillageEnteredTime;

        public CampaignTime LastMapEventStartedTime => _lastMapEventStartedTime;
        public CampaignTime LastMapEventEndedTime => _lastMapEventEndedTime;
        public string LastMapEventType => _lastMapEventType ?? string.Empty;

        public CampaignTriggerTrackerBehavior()
        {
            Instance = this;
        }

        public override void RegisterEvents()
        {
            CampaignEvents.SettlementEntered.AddNonSerializedListener(this, OnSettlementEntered);
            CampaignEvents.OnSettlementLeftEvent.AddNonSerializedListener(this, OnSettlementLeft);
            CampaignEvents.MapEventStarted.AddNonSerializedListener(this, OnMapEventStarted);
            CampaignEvents.MapEventEnded.AddNonSerializedListener(this, OnMapEventEnded);
        }

        public override void SyncData(IDataStore dataStore)
        {
            SaveLoadDiagnostics.SafeSyncData(this, dataStore, () =>
            {
                // Settlements
                dataStore.SyncData("tr_lastSettlementEnteredTime", ref _lastSettlementEnteredTime);
                dataStore.SyncData("tr_lastSettlementEnteredId", ref _lastSettlementEnteredId);
                dataStore.SyncData("tr_lastSettlementLeftTime", ref _lastSettlementLeftTime);
                dataStore.SyncData("tr_lastSettlementLeftId", ref _lastSettlementLeftId);

                dataStore.SyncData("tr_lastTownEnteredTime", ref _lastTownEnteredTime);
                dataStore.SyncData("tr_lastCastleEnteredTime", ref _lastCastleEnteredTime);
                dataStore.SyncData("tr_lastVillageEnteredTime", ref _lastVillageEnteredTime);

                // Battles / map events
                dataStore.SyncData("tr_lastMapEventStartedTime", ref _lastMapEventStartedTime);
                dataStore.SyncData("tr_lastMapEventEndedTime", ref _lastMapEventEndedTime);
                dataStore.SyncData("tr_lastMapEventType", ref _lastMapEventType);
            });
        }

        /// <summary>
        /// Returns true when an event timestamp is within the given window (in days).
        /// </summary>
        public bool IsWithinDays(CampaignTime when, float windowDays)
        {
            if (when == CampaignTime.Zero)
            {
                return false;
            }

            if (windowDays <= 0)
            {
                return false;
            }

            // CampaignTime arithmetic varies between versions; use ToDays for a stable check.
            var deltaDays = CampaignTime.Now.ToDays - when.ToDays;
            return deltaDays >= 0f && deltaDays <= windowDays;
        }

        /// <summary>
        /// Returns the current time block (4-block system: Morning, Afternoon, Dusk, Night).
        /// This is the preferred method for schedule and activity filtering.
        /// </summary>
        public TimeBlock GetTimeBlock()
        {
            var hourOfDay = (int)Math.Floor((CampaignTime.Now.ToDays * 24f) % 24f);
            if (hourOfDay < 0)
            {
                hourOfDay = 0;
            }

            // 4-block schedule by hour: Morning is 6–12 (training, patrols, primary duties). Afternoon is 12–18
            // (work details, secondary duties). Dusk is 18–22 (free time, social activities). Night is 22–6 (rest,
            // watch rotation).
            if (hourOfDay >= 6 && hourOfDay < 12)
            {
                return TimeBlock.Morning;
            }
            if (hourOfDay >= 12 && hourOfDay < 18)
            {
                return TimeBlock.Afternoon;
            }
            if (hourOfDay >= 18 && hourOfDay < 22)
            {
                return TimeBlock.Dusk;
            }
            return TimeBlock.Night;
        }

        private void OnSettlementEntered(MobileParty party, Settlement settlement, Hero hero)
        {
            try
            {
                if (party == null || party != MobileParty.MainParty)
                {
                    return;
                }

                _lastSettlementEnteredTime = CampaignTime.Now;
                _lastSettlementEnteredId = settlement?.StringId ?? string.Empty;

                if (settlement?.IsTown == true)
                {
                    _lastTownEnteredTime = CampaignTime.Now;
                }
                if (settlement?.IsCastle == true)
                {
                    _lastCastleEnteredTime = CampaignTime.Now;
                }
                if (settlement?.IsVillage == true)
                {
                    _lastVillageEnteredTime = CampaignTime.Now;
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, "Error tracking SettlementEntered", ex);
            }
        }

        private void OnSettlementLeft(MobileParty party, Settlement settlement)
        {
            try
            {
                if (party == null || party != MobileParty.MainParty)
                {
                    return;
                }

                _lastSettlementLeftTime = CampaignTime.Now;
                _lastSettlementLeftId = settlement?.StringId ?? string.Empty;
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, "Error tracking SettlementLeft", ex);
            }
        }

        private void OnMapEventStarted(MapEvent mapEvent, PartyBase attackerParty, PartyBase defenderParty)
        {
            try
            {
                // Map events can be noisy; only track ones involving the player party.
                if (!IsMapEventInvolvingMainParty(mapEvent))
                {
                    return;
                }

                _lastMapEventStartedTime = CampaignTime.Now;
                _lastMapEventType = mapEvent?.EventType.ToString() ?? string.Empty;
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, "Error tracking MapEventStarted", ex);
            }
        }

        private void OnMapEventEnded(MapEvent mapEvent)
        {
            try
            {
                if (!IsMapEventInvolvingMainParty(mapEvent))
                {
                    return;
                }

                _lastMapEventEndedTime = CampaignTime.Now;
                _lastMapEventType = mapEvent?.EventType.ToString() ?? _lastMapEventType;
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, "Error tracking MapEventEnded", ex);
            }
        }

        private static bool IsMapEventInvolvingMainParty(MapEvent mapEvent)
        {
            try
            {
                if (mapEvent == null)
                {
                    return false;
                }

                var main = MobileParty.MainParty;
                if (main == null)
                {
                    return false;
                }

                // InvolvedParties is the most stable, version-friendly way to check membership.
                // (Some internal party collections are MBReadOnlyList and don't always expose LINQ extensions cleanly.)
                return mapEvent.InvolvedParties?.Any(p => p?.MobileParty == main) == true;
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>
    /// 4-block time system for daily activities: Morning (6-12), Afternoon (12-18), Dusk (18-22), Night (22-6).
    /// </summary>
    public enum TimeBlock
    {
        Morning,
        Afternoon,
        Dusk,
        Night
    }
}


