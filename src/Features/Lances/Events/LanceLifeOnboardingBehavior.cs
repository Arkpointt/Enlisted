using System;
using Enlisted.Features.Assignments.Core;
using Enlisted.Features.CommandTent.Core;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Mod.Core.Logging;
using TaleWorlds.CampaignSystem;

namespace Enlisted.Features.Lances.Events
{
    /// <summary>
    /// Onboarding state machine (stage/track/variant).
    ///
    /// This behavior does not deliver content itself. It provides:
    /// - Stage flags (onboarding_stage_1/2/3/complete)
    /// - Track selection (enlisted/officer/commander)
    /// - Variant selection (first_time/transfer/return)
    /// - days_since_enlistment and days_since_promotion values
    ///
    /// Safe persistence: only primitives + strings via SyncData.
    /// </summary>
    public sealed class LanceLifeOnboardingBehavior : CampaignBehaviorBase
    {
        private const string LogCategory = "LanceLifeEvents";

        public static LanceLifeOnboardingBehavior Instance { get; private set; }

        // Persisted state
        private int _stage = 0; // 0=uninitialized, 1..N stages, N+1=complete
        private string _track = string.Empty;
        private string _variant = string.Empty;
        private int _enlistDay = -1;
        private int _promotionDay = -1;

        private string _entryLordId = string.Empty;

        public LanceLifeOnboardingBehavior()
        {
            Instance = this;
        }

        public override void RegisterEvents()
        {
            EnlistmentBehavior.OnEnlisted += OnEnlisted;
            EnlistmentBehavior.OnDischarged += OnDischarged;
            EnlistmentBehavior.OnPromoted += OnPromoted;
        }

        public override void SyncData(IDataStore dataStore)
        {
            SaveLoadDiagnostics.SafeSyncData(this, dataStore, () =>
            {
                dataStore.SyncData("ll_on_stage", ref _stage);
                dataStore.SyncData("ll_on_track", ref _track);
                dataStore.SyncData("ll_on_variant", ref _variant);
                dataStore.SyncData("ll_on_enlistDay", ref _enlistDay);
                dataStore.SyncData("ll_on_promoDay", ref _promotionDay);
                dataStore.SyncData("ll_on_entryLord", ref _entryLordId);

                if (dataStore.IsLoading)
                {
                    NormalizeLoadedState();
                }
            });
        }

        public bool IsEnabled()
        {
            var cfg = ConfigurationManager.LoadLanceLifeEventsConfig();
            if (cfg?.Enabled != true)
            {
                return false;
            }

            return cfg.Onboarding?.Enabled == true;
        }

        public int Stage => _stage;
        public string Track => _track ?? string.Empty;
        public string Variant => _variant ?? string.Empty;

        public bool IsComplete
        {
            get
            {
                var stageCount = GetStageCount();
                return _stage >= stageCount + 1;
            }
        }

        public int DaysSinceEnlistment
        {
            get
            {
                if (_enlistDay < 0)
                {
                    return int.MaxValue;
                }

                return Math.Max(0, GetDayNumber() - _enlistDay);
            }
        }

        public int DaysSincePromotion
        {
            get
            {
                if (_promotionDay < 0)
                {
                    return int.MaxValue;
                }

                return Math.Max(0, GetDayNumber() - _promotionDay);
            }
        }

        public bool IsStageActive(int stage)
        {
            return stage > 0 && _stage == stage && !IsComplete;
        }

        public bool IsCompleteToken => IsComplete;

        /// <summary>
        /// Advance onboarding stage by 1, clamped to [1..stageCount+1].
        /// </summary>
        public void AdvanceStage(string reason)
        {
            if (!IsEnabled())
            {
                return;
            }

            var stageCount = GetStageCount();
            if (stageCount <= 0)
            {
                _stage = 1;
                return;
            }

            if (_stage <= 0)
            {
                _stage = 1;
                return;
            }

            var next = Math.Min(stageCount + 1, _stage + 1);
            if (next != _stage)
            {
                ModLogger.Info(LogCategory, $"Onboarding stage advanced: {_stage} -> {next} (why={reason ?? "unknown"})");
            }
            _stage = next;
        }

        private void OnEnlisted(Hero lord)
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

                var svc = ServiceRecordManager.Instance;
                var totalEnlistments = svc?.LifetimeRecord?.TotalEnlistments ?? 0;
                var isFirstTime = totalEnlistments <= 1;

                var cfg = ConfigurationManager.LoadLanceLifeEventsConfig()?.Onboarding ?? new LanceLifeEventsOnboardingConfig();
                if (cfg.SkipForVeterans && !isFirstTime)
                {
                    InitializeBaseState(enlistment, lord, track: PickTrack(enlistment.EnlistmentTier), variant: PickVariant(enlistment, lord, isFirstTime));
                    // Mark complete immediately.
                    _stage = GetStageCount() + 1;
                    ModLogger.Info(LogCategory, "Onboarding skipped for veteran enlistment (skip_for_veterans=true)");
                    return;
                }

                InitializeBaseState(enlistment, lord, track: PickTrack(enlistment.EnlistmentTier), variant: PickVariant(enlistment, lord, isFirstTime));
                _stage = 1;

                ModLogger.Info(LogCategory, $"Onboarding initialized: stage={_stage}, track={_track}, variant={_variant}");
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, "Onboarding OnEnlisted failed", ex);
            }
        }

        private void OnDischarged(string reason)
        {
            try
            {
                // Reset state between enlistments.
                _stage = 0;
                _track = string.Empty;
                _variant = string.Empty;
                _enlistDay = -1;
                _promotionDay = -1;
                _entryLordId = string.Empty;
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, "Onboarding OnDischarged failed", ex);
            }
        }

        private void OnPromoted(int newTier)
        {
            try
            {
                if (!IsEnabled())
                {
                    return;
                }

                if (EnlistmentBehavior.Instance?.IsEnlisted != true)
                {
                    return;
                }

                _promotionDay = GetDayNumber();
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, "Onboarding OnPromoted failed", ex);
            }
        }

        private void InitializeBaseState(EnlistmentBehavior enlistment, Hero lord, string track, string variant)
        {
            _track = track ?? string.Empty;
            _variant = variant ?? string.Empty;
            _enlistDay = GetDayNumber();
            _promotionDay = _enlistDay;
            _entryLordId = lord?.StringId ?? string.Empty;
        }

        private static string PickTrack(int entryTier)
        {
            if (entryTier <= 4)
            {
                return "enlisted";
            }
            if (entryTier <= 6)
            {
                return "officer";
            }
            return "commander";
        }

        private static string PickVariant(EnlistmentBehavior enlistment, Hero currentLord, bool isFirstTime)
        {
            if (isFirstTime)
            {
                return "first_time";
            }

            var svc = ServiceRecordManager.Instance;
            var lastLordId = svc?.ReservistRecord?.LastLordId ?? string.Empty;
            var currentLordId = currentLord?.StringId ?? enlistment?.CurrentLord?.StringId ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(lastLordId) && !string.Equals(lastLordId, currentLordId, StringComparison.OrdinalIgnoreCase))
            {
                return "transfer";
            }

            return "return";
        }

        private int GetStageCount()
        {
            var cfg = ConfigurationManager.LoadLanceLifeEventsConfig()?.Onboarding;
            return Math.Max(1, cfg?.StageCount ?? 3);
        }

        private static int GetDayNumber()
        {
            return (int)Math.Floor(CampaignTime.Now.ToDays);
        }

        private void NormalizeLoadedState()
        {
            if (_stage < 0)
            {
                _stage = 0;
            }

            _track ??= string.Empty;
            _variant ??= string.Empty;
            _entryLordId ??= string.Empty;

            if (_enlistDay < -1)
            {
                _enlistDay = -1;
            }

            if (_promotionDay < -1)
            {
                _promotionDay = -1;
            }
        }
    }
}


