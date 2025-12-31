using System.Collections.Generic;
using Enlisted.Features.Content.Models;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Features.Escalation;
using Enlisted.Mod.Core.Logging;
using Enlisted.Mod.Core.SaveSystem;
using TaleWorlds.CampaignSystem;

namespace Enlisted.Features.Content
{
    /// <summary>
    /// Central coordinator for content pacing in the Enlisted mod.
    /// Analyzes world state and provides activity levels to OrderProgressionBehavior.
    /// Generates forecasts for UI and updates camp opportunities for player decisions.
    /// Does NOT fire automatic events - content delivery happens through order events (during duty) 
    /// and player-initiated camp decisions (DECISIONS menu).
    /// </summary>
    public class ContentOrchestrator : CampaignBehaviorBase
    {
        private const string LogCategory = "Orchestrator";

        /// <summary>Singleton instance for global access.</summary>
        public static ContentOrchestrator Instance { get; private set; }

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
        /// Logs the transition and notifies dependent systems.
        /// </summary>
        private void OnDayPhaseChanged(DayPhase newPhase)
        {
            ModLogger.Debug(LogCategory, $"Day phase changed to {newPhase}");

            // Notify camp life systems to refresh for new phase
            Camp.CampOpportunityGenerator.Instance?.OnPhaseChanged(newPhase);

            // TODO: Notify other dependent systems when implemented:
            // OrderProgressionBehavior.Instance?.OnPhaseChanged(newPhase);
        }

        /// <summary>
        /// Main daily tick. Runs once per in-game day.
        /// Orchestrator provides activity levels to OrderProgressionBehavior and generates forecasts.
        /// Does NOT fire automatic events - content delivery happens through orders and player decisions.
        /// </summary>
        private void OnDailyTick()
        {
            if (!IsActive())
            {
                return;
            }

            // Analyze world situation
            var worldSituation = WorldStateAnalyzer.AnalyzeSituation();
            var activityLevel = worldSituation.ExpectedActivity;

            ModLogger.Debug(LogCategory,
                $"World State: Lord={worldSituation.LordIs}, Phase={worldSituation.CurrentPhase}, Activity={activityLevel}");

            // Check if orchestrator is enabled
            var config = Mod.Core.Config.ConfigurationManager.LoadOrchestratorConfig();
            if (config?.Enabled == true)
            {
                ModLogger.Info(LogCategory, "=== Orchestrator Active ===");
                ModLogger.Info(LogCategory, $"Activity Level: {activityLevel} → OrderProgressionBehavior");

                // Activity level is provided to OrderProgressionBehavior via GetCurrentWorldSituation()
                // OrderProgressionBehavior uses it to modify order event slot probabilities

                // Set quiet day based on world state
                SetQuietDayFromWorldState(worldSituation);

                // Generate forecasts for UI (Main Menu NOW and AHEAD sections)
                // This ensures forecast data is fresh for when player opens menu
                GenerateForecastData(worldSituation);

                // Update camp opportunities availability
                RefreshCampOpportunities(worldSituation);

                // TODO Phase 10: Add order planning with 24h/8h/2h warnings
                // PlanNext24Hours(worldSituation);
            }
            else
            {
                // Orchestrator disabled - run diagnostic analysis only
                ModLogger.Debug(LogCategory, "Orchestrator disabled - analysis mode only");

                // Calculate simulation pressure
                var pressure = SimulationPressureCalculator.CalculatePressure();
                var sourcesText = pressure.Sources.Count > 0
                    ? string.Join(", ", pressure.Sources)
                    : "None";
                ModLogger.Debug(LogCategory, $"Pressure: {pressure.Value:F0}/100 from [{sourcesText}]");

                // Determine realistic frequency
                var frequency = DetermineRealisticFrequency(worldSituation, pressure);
                ModLogger.Info(LogCategory, $"Realistic frequency: {frequency:F2} events/week");

                // Log context for future content selection
                var eventContext = WorldStateAnalyzer.GetEventContext(worldSituation);
                var orderContext = WorldStateAnalyzer.GetOrderEventWorldState(worldSituation);
                ModLogger.Debug(LogCategory, $"Event context: {eventContext}, Order context: {orderContext}");

                // Test content selection with fitness scoring (logging only, no delivery)
                TestContentSelection(worldSituation);
            }

            // Update behavior tracking data for next save
            _behaviorCounts = PlayerBehaviorTracker.GetBehaviorCountsForSave();
            _contentEngagement = PlayerBehaviorTracker.GetContentEngagementForSave();
        }

        /// <summary>
        /// Tests content selection with fitness scoring and logs comparisons.
        /// Logs what WOULD be selected without affecting the live system.
        /// </summary>
        private void TestContentSelection(WorldSituation worldSituation)
        {
            ModLogger.Info(LogCategory, "=== Content Selection Test ===");

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

            ModLogger.Info(LogCategory, "=== End Selection Test ===");
        }

        /// <summary>
        /// Generates forecast data for UI display.
        /// Forecasts include player status (duty, health), upcoming events, and company state.
        /// ForecastGenerator.BuildPlayerStatus() is called by UI on-demand using this data.
        /// </summary>
        private void GenerateForecastData(WorldSituation worldSituation)
        {
            // Forecast data is cached in various systems for UI consumption:
            // - OrderManager tracks current/upcoming orders
            // - CompanySimulationBehavior tracks needs/pressure
            // - EscalationManager tracks tension levels
            // - CampOpportunityGenerator tracks commitments
            
            // The daily tick ensures all these systems have current data
            // UI calls ForecastGenerator.BuildPlayerStatus() which reads from these systems
            
            ModLogger.Debug(LogCategory, "Forecast data ready for UI");
        }

        /// <summary>
        /// Refreshes available camp opportunities based on world state.
        /// CampOpportunityGenerator filters opportunities by phase, context, and eligibility.
        /// </summary>
        private void RefreshCampOpportunities(WorldSituation worldSituation)
        {
            // Camp opportunities refresh automatically on phase changes via OnPhaseChanged()
            // This daily tick can trigger additional logic for opportunity planning
            
            var opportunityGen = Camp.CampOpportunityGenerator.Instance;
            if (opportunityGen != null)
            {
                // Opportunities are generated on-demand when player opens DECISIONS menu
                // CampOpportunityGenerator.GenerateCampLife() uses current world state
                ModLogger.Debug(LogCategory, "Camp opportunity system active");
            }
        }

        /// <summary>
        /// Determines realistic event frequency based on world situation and pressure.
        /// Used for diagnostic analysis when orchestrator is disabled.
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
        /// Checks if the orchestrator should be active.
        /// </summary>
        private bool IsActive()
        {
            return EnlistmentBehavior.Instance?.IsEnlisted == true;
        }

        /// <summary>
        /// Sets quiet day flag based on world state.
        /// Garrison = potentially quiet, Battle = always quiet, Campaign/Siege = never quiet.
        /// </summary>
        private void SetQuietDayFromWorldState(WorldSituation situation)
        {
            bool isQuiet = false;

            // Defeated/Captured = always quiet (recovery/imprisonment)
            if (situation.LordIs == LordSituation.Defeated || situation.LordIs == LordSituation.Captured)
            {
                isQuiet = true;
            }
            // Peacetime garrison = potentially quiet (low activity)
            else if (situation.LordIs == LordSituation.PeacetimeGarrison)
            {
                // Use a small chance for quiet days in garrison (10%)
                var roll = TaleWorlds.Core.MBRandom.RandomFloat;
                isQuiet = roll < 0.10f;
            }
            // Campaign/Siege/War = never quiet (high activity)
            else
            {
                isQuiet = false;
            }

            GlobalEventPacer.SetQuietDay(isQuiet);

            if (isQuiet)
            {
                ModLogger.Debug(LogCategory, $"Set quiet day: true (situation: {situation.LordIs})");
            }
        }

        /// <summary>
        /// Checks if OrderManager can issue a new order now based on world state timing.
        /// Coordinates order issuance frequency with activity level.
        /// </summary>
        public bool CanIssueOrderNow()
        {
            if (!IsActive())
            {
                return true; // Orchestrator disabled, allow normal order flow
            }

            var worldSituation = WorldStateAnalyzer.AnalyzeSituation();
            var activityLevel = worldSituation.ExpectedActivity;

            // Determine minimum days between orders based on activity level
            float minDaysBetweenOrders = activityLevel switch
            {
                ActivityLevel.Quiet => 3.0f,      // Garrison: 3-5 days
                ActivityLevel.Routine => 2.0f,    // Peacetime: 2-3 days
                ActivityLevel.Active => 1.0f,     // Campaign: 1-2 days
                ActivityLevel.Intense => 0.5f,    // Siege: 0.5-1 day
                _ => 2.0f
            };

            // Check if enough time has passed (OrderManager tracks _lastOrderTime)
            // For now, always return true to allow OrderManager's existing timing logic
            // TODO: OrderManager will coordinate with this method when order integration is added
            ModLogger.Debug(LogCategory,
                $"Order issuance check: Activity={activityLevel}, MinDays={minDaysBetweenOrders}");

            return true;
        }

        /// <summary>
        /// Exposes current world situation for OrderProgressionBehavior event weighting.
        /// Used to modify slot event chances based on activity level.
        /// </summary>
        public WorldSituation GetCurrentWorldSituation()
        {
            return WorldStateAnalyzer.AnalyzeSituation();
        }

        /// <summary>
        /// Queues a crisis event for high-priority delivery.
        /// Called by CompanySimulationBehavior when pressure thresholds are exceeded.
        /// Crisis events bypass normal frequency limits and fire at the next opportunity.
        /// </summary>
        /// <param name="eventId">Event ID to queue (e.g., "evt_supply_crisis", "evt_morale_collapse").</param>
        public void QueueCrisisEvent(string eventId)
        {
            if (string.IsNullOrEmpty(eventId))
            {
                return;
            }

            ModLogger.Warn(LogCategory, $"Crisis event queued: {eventId}");

            // Load the event definition
            var eventDef = EventCatalog.GetEvent(eventId);
            if (eventDef == null)
            {
                ModLogger.Warn(LogCategory, $"Crisis event '{eventId}' not found in definitions - creating placeholder");

                // Crisis events should exist, but if not, log for content authors to add them
                // The simulation should still function even if specific crisis events aren't defined yet
                return;
            }

            // Queue via EventDeliveryManager
            var deliveryManager = EventDeliveryManager.Instance;
            if (deliveryManager != null)
            {
                deliveryManager.QueueEvent(eventDef);
                ModLogger.Info(LogCategory, $"Crisis event queued for delivery: {eventId}");
            }
            else
            {
                ModLogger.Warn(LogCategory, "EventDeliveryManager not available for crisis event delivery");
            }
        }
    }
}
