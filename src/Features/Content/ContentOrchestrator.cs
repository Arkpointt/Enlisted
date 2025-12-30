using System.Collections.Generic;
using Enlisted.Features.Content.Models;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Mod.Core.Logging;
using Enlisted.Mod.Core.SaveSystem;
using TaleWorlds.CampaignSystem;

namespace Enlisted.Features.Content
{
    /// <summary>
    /// Central coordinator for all content delivery in the Enlisted mod.
    /// Analyzes world state, calculates appropriate content frequency, and coordinates
    /// timing across narrative events, orders, and camp life systems.
    ///
    /// Phase 1: Foundation - logs analysis without affecting live systems.
    /// Future phases will integrate with EventPacingManager, OrderManager, etc.
    /// </summary>
    public class ContentOrchestrator : CampaignBehaviorBase
    {
        private const string LogCategory = "Orchestrator";

        /// <summary>Singleton instance for global access.</summary>
        public static ContentOrchestrator Instance { get; private set; }

        // Track events delivered this week for frequency management
        private int _eventsThisWeek;
        private CampaignTime _lastWeekReset = CampaignTime.Zero;

        // Track last day phase for phase change detection
        private DayPhase _lastPhase = DayPhase.Night;

        // Behavior tracking data for save/load
        private Dictionary<string, int> _behaviorCounts = new Dictionary<string, int>();
        private Dictionary<string, int> _contentEngagement = new Dictionary<string, int>();

        public ContentOrchestrator()
        {
            Instance = this;
        }

        public override void RegisterEvents()
        {
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
            CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, OnHourlyTick);
            ModLogger.Info(LogCategory, "Content Orchestrator registered for daily and hourly ticks");
        }

        public override void SyncData(IDataStore dataStore)
        {
            SaveLoadDiagnostics.SafeSyncData(this, dataStore, () =>
            {
                dataStore.SyncData("orchestrator_eventsThisWeek", ref _eventsThisWeek);

                // Save/load behavior tracking data
                dataStore.SyncData("orchestrator_behaviorCounts", ref _behaviorCounts);
                dataStore.SyncData("orchestrator_contentEngagement", ref _contentEngagement);

                // Sync last phase as int
                int lastPhaseInt = (int)_lastPhase;
                dataStore.SyncData("orchestrator_lastPhase", ref lastPhaseInt);
                _lastPhase = (DayPhase)lastPhaseInt;

                // After loading, restore PlayerBehaviorTracker state
                if (dataStore.IsLoading)
                {
                    PlayerBehaviorTracker.LoadState(_behaviorCounts, _contentEngagement);
                    ModLogger.Debug(LogCategory, "Restored behavior tracking state from save");
                }
            });
        }

        /// <summary>
        /// Called every in-game hour. Checks for day phase transitions.
        /// </summary>
        private void OnHourlyTick()
        {
            if (!IsActive())
            {
                return;
            }

            CheckPhaseTransition();
        }

        /// <summary>
        /// Checks if the day phase has changed and fires OnDayPhaseChanged if so.
        /// </summary>
        private void CheckPhaseTransition()
        {
            var currentHour = CampaignTime.Now.GetHourOfDay;
            var currentPhase = WorldStateAnalyzer.GetDayPhaseFromHour(currentHour);

            if (currentPhase != _lastPhase)
            {
                _lastPhase = currentPhase;
                OnDayPhaseChanged(currentPhase);
            }
        }

        /// <summary>
        /// Fires when military day phase changes (4x per day).
        /// Phase 1: Logs the transition only.
        /// Future phases will sync Orders, Camp Life, and Progression systems.
        /// </summary>
        private void OnDayPhaseChanged(DayPhase newPhase)
        {
            ModLogger.Debug(LogCategory, $"Day phase changed to {newPhase}");

            // Phase 1: Logging only
            // Future phases will add:
            // OrderProgressionBehavior.Instance?.OnPhaseChanged(newPhase);
            // CampLifeManager.Instance?.OnPhaseChanged(newPhase);
            // MainMenuNewsCache.Instance?.OnPhaseChanged(newPhase);
        }

        /// <summary>
        /// Main daily analysis. Runs once per in-game day.
        /// Phase 1: Analyzes world state and logs results without affecting live systems.
        /// </summary>
        private void OnDailyTick()
        {
            if (!IsActive())
            {
                return;
            }

            // Reset weekly counter if needed
            ResetWeeklyCounterIfNeeded();

            // 1. Analyze world situation
            var worldSituation = WorldStateAnalyzer.AnalyzeSituation();
            ModLogger.Debug(LogCategory,
                $"World State: Lord={worldSituation.LordIs}, Phase={worldSituation.CurrentPhase}, Activity={worldSituation.ExpectedActivity}");

            // 2. Calculate simulation pressure
            var pressure = SimulationPressureCalculator.CalculatePressure();
            var sourcesText = pressure.Sources.Count > 0
                ? string.Join(", ", pressure.Sources)
                : "None";
            ModLogger.Debug(LogCategory, $"Pressure: {pressure.Value:F0}/100 from [{sourcesText}]");

            // 3. Determine realistic frequency
            var frequency = DetermineRealisticFrequency(worldSituation, pressure);
            ModLogger.Info(LogCategory, $"Realistic frequency: {frequency:F2} events/week");

            // 4. Log context for future content selection
            var eventContext = WorldStateAnalyzer.GetEventContext(worldSituation);
            var orderContext = WorldStateAnalyzer.GetOrderEventWorldState(worldSituation);
            ModLogger.Debug(LogCategory, $"Event context: {eventContext}, Order context: {orderContext}");

            // Phase 2: Test content selection with fitness scoring (logging only, no delivery)
            TestContentSelection(worldSituation);

            // Update behavior tracking data for next save
            _behaviorCounts = PlayerBehaviorTracker.GetBehaviorCountsForSave();
            _contentEngagement = PlayerBehaviorTracker.GetContentEngagementForSave();
        }

        /// <summary>
        /// Tests content selection with fitness scoring and logs comparisons.
        /// Phase 2: Logs what WOULD be selected without affecting live system.
        /// </summary>
        private void TestContentSelection(WorldSituation worldSituation)
        {
            ModLogger.Info(LogCategory, "=== Phase 2: Content Selection Test ===");

            // Select with OLD system (no world situation)
            var oldSelection = EventSelector.SelectEvent(null);
            if (oldSelection != null)
            {
                ModLogger.Info(LogCategory, $"OLD system would select: {oldSelection.Id} (category: {oldSelection.Category})");
            }
            else
            {
                ModLogger.Debug(LogCategory, "OLD system: No eligible events");
            }

            // Select with NEW system (with world situation and fitness scoring)
            var newSelection = EventSelector.SelectEvent(worldSituation);
            if (newSelection != null)
            {
                ModLogger.Info(LogCategory, $"NEW system would select: {newSelection.Id} (category: {newSelection.Category})");

                // Log fitness reasoning
                var playerPrefs = PlayerBehaviorTracker.GetPreferences();
                ModLogger.Debug(LogCategory,
                    $"Player preferences: Combat={playerPrefs.CombatVsSocial:F2}, Risky={playerPrefs.RiskyVsSafe:F2}, Loyal={playerPrefs.LoyalVsSelfServing:F2} (from {playerPrefs.TotalChoicesMade} choices)");
            }
            else
            {
                ModLogger.Debug(LogCategory, "NEW system: No eligible events");
            }

            // Compare selections
            if (oldSelection != null && newSelection != null)
            {
                if (oldSelection.Id == newSelection.Id)
                {
                    ModLogger.Info(LogCategory, "✓ Both systems selected same event");
                }
                else
                {
                    ModLogger.Info(LogCategory, $"✗ Systems differ: OLD={oldSelection.Id}, NEW={newSelection.Id}");
                    ModLogger.Debug(LogCategory, $"Difference reason: Fitness scoring adjusted weights based on activity={worldSituation.ExpectedActivity}");
                }
            }

            ModLogger.Info(LogCategory, "=== End Content Selection Test ===");
        }

        /// <summary>
        /// Determines realistic event frequency based on world situation and pressure.
        /// </summary>
        private float DetermineRealisticFrequency(WorldSituation situation, SimulationPressure pressure)
        {
            // Start with base frequency from world analysis
            var baseFrequency = situation.RealisticEventFrequency;

            // Apply pressure modifier
            var pressureModifier = pressure.GetFrequencyModifier();

            return baseFrequency * pressureModifier;
        }

        /// <summary>
        /// Resets the weekly event counter if a week has passed.
        /// </summary>
        private void ResetWeeklyCounterIfNeeded()
        {
            if (_lastWeekReset == CampaignTime.Zero)
            {
                _lastWeekReset = CampaignTime.Now;
                return;
            }

            var daysSinceReset = (CampaignTime.Now - _lastWeekReset).ToDays;
            if (daysSinceReset >= 7)
            {
                ModLogger.Debug(LogCategory, $"Weekly reset: {_eventsThisWeek} events last week");
                _eventsThisWeek = 0;
                _lastWeekReset = CampaignTime.Now;
            }
        }

        /// <summary>
        /// Checks if the orchestrator should be active.
        /// </summary>
        private bool IsActive()
        {
            return EnlistmentBehavior.Instance?.IsEnlisted == true;
        }
    }
}
