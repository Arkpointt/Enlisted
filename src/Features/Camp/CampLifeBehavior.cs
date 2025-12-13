using System;
using System.Linq;
using Enlisted.Features.Assignments.Core;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Mod.Core.Logging;
using Enlisted.Mod.Core.Triggers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace Enlisted.Features.Camp
{
    /// <summary>
    /// Phase 3 (Roadmap): Daily camp-conditions snapshot + lightweight integrations.
    ///
    /// This is an internal simulation layer:
    /// - Only runs while enlisted
    /// - Stores small numeric meters (0–100) updated once per day
    /// - Feeds Enlisted-owned systems (Quartermaster pricing/mood, Pay Muster text/options, Lance Life triggers)
    ///
    /// It avoids world scanning by using CampaignEvents + a few small rolling counters.
    /// </summary>
    public sealed class CampLifeBehavior : CampaignBehaviorBase
    {
        private const string LogCategory = "CampLife";

        public static CampLifeBehavior Instance { get; private set; }

        // Snapshot meters (0–100)
        private float _logisticsStrain;
        private float _moraleShock;
        private float _territoryPressure;
        private float _payTension;
        private float _contrabandHeat;

        // Daily update gate
        private int _lastSnapshotDayNumber = -1;

        // Simple "recent history" counters (weekly)
        private int _weekNumber;
        private int _battlesThisWeek;
        private int _villagesLootedThisWeek;

        // Quartermaster mood derived from the snapshot (stable for the day)
        private QuartermasterMoodTier _quartermasterMoodTier = QuartermasterMoodTier.Fine;

        public CampLifeBehavior()
        {
            Instance = this;
        }

        public float LogisticsStrain => _logisticsStrain;
        public float MoraleShock => _moraleShock;
        public float TerritoryPressure => _territoryPressure;
        public float PayTension => _payTension;
        public float ContrabandHeat => _contrabandHeat;
        public QuartermasterMoodTier QuartermasterMoodTier => _quartermasterMoodTier;

        public override void RegisterEvents()
        {
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
            CampaignEvents.MapEventEnded.AddNonSerializedListener(this, OnMapEventEnded);
            CampaignEvents.VillageLooted.AddNonSerializedListener(this, OnVillageLooted);
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("cl_logisticsStrain", ref _logisticsStrain);
            dataStore.SyncData("cl_moraleShock", ref _moraleShock);
            dataStore.SyncData("cl_territoryPressure", ref _territoryPressure);
            dataStore.SyncData("cl_payTension", ref _payTension);
            dataStore.SyncData("cl_contrabandHeat", ref _contrabandHeat);

            var mood = (int)_quartermasterMoodTier;
            dataStore.SyncData("cl_qmMoodTier", ref mood);
            _quartermasterMoodTier = (QuartermasterMoodTier)mood;

            dataStore.SyncData("cl_lastSnapshotDayNumber", ref _lastSnapshotDayNumber);
            dataStore.SyncData("cl_weekNumber", ref _weekNumber);
            dataStore.SyncData("cl_battlesThisWeek", ref _battlesThisWeek);
            dataStore.SyncData("cl_villagesLootedThisWeek", ref _villagesLootedThisWeek);
        }

        public bool IsEnabled()
        {
            return ConfigurationManager.LoadCampLifeConfig()?.Enabled == true;
        }

        public bool IsActiveWhileEnlisted()
        {
            return IsEnabled() && EnlistmentBehavior.Instance?.IsEnlisted == true;
        }

        public bool IsLogisticsHigh()
        {
            var cfg = ConfigurationManager.LoadCampLifeConfig();
            return IsActiveWhileEnlisted() && _logisticsStrain >= (cfg?.LogisticsHighThreshold ?? 70f);
        }

        public bool IsMoraleLow()
        {
            var cfg = ConfigurationManager.LoadCampLifeConfig();
            // MoraleShock is an inverse-morale meter (higher shock == lower morale).
            return IsActiveWhileEnlisted() && _moraleShock >= (cfg?.MoraleLowThreshold ?? 70f);
        }

        public bool IsPayTensionHigh()
        {
            var cfg = ConfigurationManager.LoadCampLifeConfig();
            return IsActiveWhileEnlisted() && _payTension >= (cfg?.PayTensionHighThreshold ?? 70f);
        }

        public bool IsHeatHigh()
        {
            var cfg = ConfigurationManager.LoadCampLifeConfig();
            return IsActiveWhileEnlisted() && _contrabandHeat >= (cfg?.HeatHighThreshold ?? 70f);
        }

        public float GetQuartermasterPurchaseMultiplier()
        {
            if (!IsActiveWhileEnlisted())
            {
                return 1.0f;
            }

            var cfg = ConfigurationManager.LoadCampLifeConfig() ?? new CampLifeConfig();
            switch (_quartermasterMoodTier)
            {
                case QuartermasterMoodTier.Fine:
                    return cfg.QuartermasterPurchaseFine;
                case QuartermasterMoodTier.Tense:
                    return cfg.QuartermasterPurchaseTense;
                case QuartermasterMoodTier.Sour:
                    return cfg.QuartermasterPurchaseSour;
                case QuartermasterMoodTier.Predatory:
                    return cfg.QuartermasterPurchasePredatory;
                default:
                    return 1.0f;
            }
        }

        public float GetQuartermasterBuybackMultiplier()
        {
            if (!IsActiveWhileEnlisted())
            {
                return 1.0f;
            }

            var cfg = ConfigurationManager.LoadCampLifeConfig() ?? new CampLifeConfig();
            switch (_quartermasterMoodTier)
            {
                case QuartermasterMoodTier.Fine:
                    return cfg.QuartermasterBuybackFine;
                case QuartermasterMoodTier.Tense:
                    return cfg.QuartermasterBuybackTense;
                case QuartermasterMoodTier.Sour:
                    return cfg.QuartermasterBuybackSour;
                case QuartermasterMoodTier.Predatory:
                    return cfg.QuartermasterBuybackPredatory;
                default:
                    return 1.0f;
            }
        }

        private void OnDailyTick()
        {
            try
            {
                if (!IsEnabled())
                {
                    return;
                }

                var enlistment = EnlistmentBehavior.Instance;
                if (enlistment?.IsEnlisted != true)
                {
                    return;
                }

                // Weekly counters reset
                var weekNumber = (int)(CampaignTime.Now.ToDays / 7f);
                if (weekNumber != _weekNumber)
                {
                    _weekNumber = weekNumber;
                    _battlesThisWeek = 0;
                    _villagesLootedThisWeek = 0;
                }

                // Daily snapshot update gate
                var dayNumber = (int)CampaignTime.Now.ToDays;
                if (dayNumber == _lastSnapshotDayNumber)
                {
                    return;
                }

                _lastSnapshotDayNumber = dayNumber;

                // Compute the snapshot in a stable, low-cost way.
                var tracker = CampaignTriggerTrackerBehavior.Instance;
                var daysSinceTown = 5f;
                if (tracker != null && tracker.LastTownEnteredTime != CampaignTime.Zero)
                {
                    daysSinceTown = (float)Math.Max(0d, CampaignTime.Now.ToDays - tracker.LastTownEnteredTime.ToDays);
                }

                // Logistics strain: long stretches away from towns, plus recent disruption (raids, heavy battle cadence).
                var logistics = (daysSinceTown * 12f) + (_villagesLootedThisWeek * 10f) + (_battlesThisWeek * 4f);
                _logisticsStrain = Clamp01Hundred(logistics);

                // Morale shock: spikes after recent battles, then decays slowly each day.
                var battleEndedRecently = tracker != null && tracker.IsWithinDays(tracker.LastMapEventEndedTime, 1f);
                if (battleEndedRecently)
                {
                    _moraleShock = Math.Max(_moraleShock, 70f);
                }
                else
                {
                    _moraleShock = Math.Max(0f, _moraleShock - 8f);
                }
                _moraleShock = Clamp01Hundred(_moraleShock + (_battlesThisWeek > 2 ? 5f : 0f));

                // Territory pressure: light placeholder for now (expanded in later phases).
                _territoryPressure = Clamp01Hundred(_villagesLootedThisWeek * 12f);

                // Pay tension: read from EnlistmentBehavior pay system (Phase 1-2 implementation)
                // This is the authoritative source - tracks actual pay delays, backpay, and tension escalation
                _payTension = enlistment.PayTension;

                // Contraband heat: owned by later phases. Keep it stable for now.
                _contrabandHeat = Clamp01Hundred(_contrabandHeat * 0.95f);

                _quartermasterMoodTier = ComputeQuartermasterMoodTier(_logisticsStrain, _moraleShock, _payTension);

                ModLogger.Info(LogCategory,
                    $"Snapshot updated (logistics={_logisticsStrain:0}, moraleShock={_moraleShock:0}, payTension={_payTension:0}, mood={_quartermasterMoodTier})");
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, "CampLife daily tick failed", ex);
            }
        }

        private void OnVillageLooted(Village village)
        {
            try
            {
                if (!IsActiveWhileEnlisted())
                {
                    return;
                }

                _villagesLootedThisWeek = Math.Max(0, _villagesLootedThisWeek + 1);
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, "VillageLooted handler failed", ex);
            }
        }

        private void OnMapEventEnded(MapEvent mapEvent)
        {
            try
            {
                if (!IsActiveWhileEnlisted())
                {
                    return;
                }

                if (!IsMapEventInvolvingMainParty(mapEvent))
                {
                    return;
                }

                _battlesThisWeek = Math.Max(0, _battlesThisWeek + 1);
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, "MapEventEnded handler failed", ex);
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

                return mapEvent.InvolvedParties?.Any(p => p?.MobileParty == main) == true;
            }
            catch
            {
                return false;
            }
        }

        private static QuartermasterMoodTier ComputeQuartermasterMoodTier(float logisticsStrain, float moraleShock, float payTension)
        {
            // A small stable blend (0–100) that matches the design intent:
            // - logistics and pay drive the "predatory clerk" feel the most
            // - morale shock adds a lighter pressure toward sour mood
            var score = (logisticsStrain * 0.55f) + (payTension * 0.35f) + (moraleShock * 0.15f);
            score = Clamp01Hundred(score);

            if (score < 25f)
            {
                return QuartermasterMoodTier.Fine;
            }
            if (score < 50f)
            {
                return QuartermasterMoodTier.Tense;
            }
            if (score < 75f)
            {
                return QuartermasterMoodTier.Sour;
            }
            return QuartermasterMoodTier.Predatory;
        }

        private static float Clamp01Hundred(float value)
        {
            if (value < 0f)
            {
                return 0f;
            }
            if (value > 100f)
            {
                return 100f;
            }
            return value;
        }
    }

    public enum QuartermasterMoodTier
    {
        Fine = 0,
        Tense = 1,
        Sour = 2,
        Predatory = 3
    }
}


