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
    /// Central coordinator for all content delivery in the Enlisted mod.
    /// Analyzes world state, calculates appropriate content frequency, and coordinates
    /// timing across narrative events, orders, and camp life systems.
    /// When disabled, logs analysis for diagnostics. When enabled, delivers content.
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
        /// When orchestrator is enabled, fires content based on world state.
        /// Otherwise, runs diagnostic analysis and logging only.
        /// </summary>
        private void OnDailyTick()
        {
            if (!IsActive())
            {
                return;
            }

            // Reset weekly counter if needed
            ResetWeeklyCounterIfNeeded();

            // Analyze world situation
            var worldSituation = WorldStateAnalyzer.AnalyzeSituation();
            ModLogger.Debug(LogCategory,
                $"World State: Lord={worldSituation.LordIs}, Phase={worldSituation.CurrentPhase}, Activity={worldSituation.ExpectedActivity}");

            // Check if orchestrator is enabled
            var config = Mod.Core.Config.ConfigurationManager.LoadOrchestratorConfig();
            if (config?.Enabled == true)
            {
                // Orchestrator enabled - fire content based on world state
                ModLogger.Info(LogCategory, "=== Orchestrator Active ===");

                // Set quiet day based on world state
                SetQuietDayFromWorldState(worldSituation);

                // Get frequency for current situation
                var frequencyTable = GetFrequencyForSituation(worldSituation);
                ModLogger.Debug(LogCategory, $"Frequency for {worldSituation.LordIs}: {frequencyTable.Base:F2} events/day");

                // Decide if content should fire today
                if (ShouldFireContent(frequencyTable))
                {
                    // Select content based on world situation
                    var content = SelectContent(worldSituation);

                    if (content != null)
                    {
                        // Deliver the content
                        DeliverContent(content);
                    }
                    else
                    {
                        ModLogger.Debug(LogCategory, "No eligible content to deliver");
                    }
                }
                else
                {
                    ModLogger.Debug(LogCategory, "Content firing skipped (probabilistic check)");
                }
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

        /// <summary>
        /// Gets the frequency configuration for the current world situation.
        /// Reads from enlisted_config.json orchestrator.frequency_tables.
        /// </summary>
        private Mod.Core.Config.FrequencyTable GetFrequencyForSituation(WorldSituation situation)
        {
            var config = Mod.Core.Config.ConfigurationManager.LoadOrchestratorConfig();
            if (config?.FrequencyTables == null)
            {
                ModLogger.Warn(LogCategory, "Orchestrator frequency tables not configured, using defaults");
                return new Mod.Core.Config.FrequencyTable { Base = 0.4f, Min = 0.3f, Max = 0.5f };
            }

            // Map world situation to frequency table key
            string key;
            if (situation.LordIs == LordSituation.SiegeAttacking || situation.LordIs == LordSituation.SiegeDefending)
            {
                key = "siege";
            }
            else if (situation.LordIs == LordSituation.WarMarching || situation.LordIs == LordSituation.WarActiveCampaign)
            {
                key = "campaign";
            }
            else if (situation.LordIs == LordSituation.Defeated || situation.LordIs == LordSituation.Captured)
            {
                key = "battle"; // Recovery/captured = suppressed activity
            }
            else
            {
                key = "garrison"; // PeacetimeGarrison, PeacetimeRecruiting
            }

            if (config.FrequencyTables.TryGetValue(key, out var table))
            {
                return table;
            }

            ModLogger.Warn(LogCategory, $"Frequency table '{key}' not found, using garrison defaults");
            return new Mod.Core.Config.FrequencyTable { Base = 0.4f, Min = 0.3f, Max = 0.5f };
        }

        /// <summary>
        /// Determines if content should fire today based on frequency.
        /// Uses probabilistic firing: frequency of 1.5 = 1.5 events per day on average.
        /// </summary>
        private bool ShouldFireContent(Mod.Core.Config.FrequencyTable frequency)
        {
            // Frequency is events per day
            // If frequency >= 1.0, fire with certainty and possibly multiple times
            // If frequency < 1.0, fire probabilistically

            var roll = TaleWorlds.Core.MBRandom.RandomFloat;
            var shouldFire = roll < frequency.Base;

            ModLogger.Debug(LogCategory, $"Fire check: roll={roll:F2}, frequency={frequency.Base:F2}, result={shouldFire}");
            return shouldFire;
        }

        /// <summary>
        /// Selects content based on world situation using EventSelector.
        /// </summary>
        private EventDefinition SelectContent(WorldSituation situation)
        {
            var selected = EventSelector.SelectEvent(situation);
            if (selected != null)
            {
                ModLogger.Info(LogCategory, $"Selected content: {selected.Id} (category: {selected.Category})");
            }
            else
            {
                ModLogger.Debug(LogCategory, "No eligible content available");
            }
            return selected;
        }

        /// <summary>
        /// Delivers content via EventDeliveryManager and records it with GlobalEventPacer.
        /// </summary>
        private void DeliverContent(EventDefinition content)
        {
            var deliveryManager = EventDeliveryManager.Instance;
            if (deliveryManager == null)
            {
                ModLogger.Warn(LogCategory, "EventDeliveryManager not available, cannot deliver content");
                return;
            }

            // Check GlobalEventPacer limits before firing
            if (!GlobalEventPacer.CanFireAutoEvent(content.Id, content.Category, out var blockReason))
            {
                ModLogger.Info(LogCategory, $"Content blocked by pacing limits: {blockReason}");
                return;
            }

            // Queue the event
            deliveryManager.QueueEvent(content);
            ModLogger.Info(LogCategory, $"Delivered content: {content.Id}");

            // Record with GlobalEventPacer
            GlobalEventPacer.RecordAutoEvent(content.Id, content.Category);

            // Update weekly counter
            _eventsThisWeek++;
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
