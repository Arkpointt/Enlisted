using System;
using System.Collections.Generic;
using System.IO;
using Enlisted.Features.Company;
using Enlisted.Features.Content.Models;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Features.Escalation;
using Enlisted.Mod.Core.Logging;
using Enlisted.Mod.Core.SaveSystem;
using Newtonsoft.Json.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

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

        // Override system tracking
        private int _lastVarietyInjectionDay = -10;
        private int _varietyInjectionsThisWeek;
        private int _weekStartDay;
        private JObject _overrideConfig;
        private bool _overrideConfigLoaded;
        private OrchestratorOverride _currentOverride;
        private DayPhase _currentOverridePhase = DayPhase.Night;

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
                // Orchestrator is active - manage world state and forecasts silently
                // Activity level is provided to OrderProgressionBehavior via GetCurrentWorldSituation()
                // OrderProgressionBehavior uses it to modify order event slot probabilities

                // Set quiet day based on world state
                SetQuietDayFromWorldState(worldSituation);

                // Generate forecasts for UI (Main Menu NOW and AHEAD sections)
                // This ensures forecast data is fresh for when player opens menu
                GenerateForecastData(worldSituation);

                // Update camp opportunities availability
                RefreshCampOpportunities(worldSituation);

                // Debug logging (only appears when Debug level is enabled for this category)
                ModLogger.Debug(LogCategory, $"Orchestrator active: Activity={activityLevel}, Phase={worldSituation.CurrentPhase}");

                // TODO Phase 10: Add order planning with 24h/8h/2h warnings
                // PlanNext24Hours(worldSituation);
            }

            // Update behavior tracking data for next save
            _behaviorCounts = PlayerBehaviorTracker.GetBehaviorCountsForSave();
            _contentEngagement = PlayerBehaviorTracker.GetContentEngagementForSave();
        }

        /// <summary>
        /// Tests content selection with fitness scoring and logs comparisons.
        /// Logs what WOULD be selected without affecting the live system.
        /// Debug-level logging only - not shown to end users.
        /// </summary>
        private void TestContentSelection(WorldSituation worldSituation)
        {
            // All diagnostic logging is at Debug level to avoid spamming end-user logs
            if (!ModLogger.IsEnabled(LogCategory, LogLevel.Debug))
            {
                return; // Skip expensive selection tests if Debug logging is disabled
            }

            ModLogger.Debug(LogCategory, "=== Content Selection Test ===");

            // Select with OLD system (no world situation)
            var oldSelection = EventSelector.SelectEvent(null);
            if (oldSelection != null)
            {
                ModLogger.Debug(LogCategory, $"OLD system would select: {oldSelection.Id} (category: {oldSelection.Category})");
            }
            else
            {
                ModLogger.Debug(LogCategory, "OLD system: No eligible events");
            }

            // Select with NEW system (with world situation and fitness scoring)
            var newSelection = EventSelector.SelectEvent(worldSituation);
            if (newSelection != null)
            {
                ModLogger.Debug(LogCategory, $"NEW system would select: {newSelection.Id} (category: {newSelection.Category})");

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
                    ModLogger.Debug(LogCategory, "✓ Both systems selected same event");
                }
                else
                {
                    ModLogger.Debug(LogCategory, $"✗ Systems differ: OLD={oldSelection.Id}, NEW={newSelection.Id}");
                    ModLogger.Debug(LogCategory, $"Difference reason: Fitness scoring adjusted weights based on activity={worldSituation.ExpectedActivity}");
                }
            }

            ModLogger.Debug(LogCategory, "=== End Selection Test ===");
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

        #region Schedule Override System

        /// <summary>
        /// Checks if an orchestrator override should apply to the given phase.
        /// First checks for need-based overrides (critical supplies, exhaustion, etc.),
        /// then checks for variety injections if no need-based override applies.
        /// Returns null if normal schedule should be used.
        /// </summary>
        public OrchestratorOverride CheckForScheduleOverride(DayPhase phase)
        {
            EnsureOverrideConfigLoaded();

            // Check if we already have an override for this phase
            if (_currentOverride != null && _currentOverridePhase == phase)
            {
                return _currentOverride;
            }

            // Get company needs for need-based checks
            var companyNeeds = EnlistmentBehavior.Instance?.CompanyNeeds;
            if (companyNeeds == null)
            {
                return null;
            }

            // Check for need-based overrides first (highest priority)
            var needOverride = CheckNeedBasedOverrides(phase, companyNeeds);
            if (needOverride != null)
            {
                _currentOverride = needOverride;
                _currentOverridePhase = phase;
                ModLogger.Info(LogCategory, 
                    $"Need-based override activated: {needOverride.ActivityName} ({needOverride.Reason})");
                return needOverride;
            }

            // Check for variety injection
            if (ShouldInjectVariety())
            {
                var varietyOverride = SelectVarietyInjection(phase);
                if (varietyOverride != null)
                {
                    _currentOverride = varietyOverride;
                    _currentOverridePhase = phase;
                    _lastVarietyInjectionDay = (int)CampaignTime.Now.ToDays;
                    _varietyInjectionsThisWeek++;
                    ModLogger.Info(LogCategory, 
                        $"Variety injection activated: {varietyOverride.ActivityName}");
                    return varietyOverride;
                }
            }

            // No override - clear cached override if phase changed
            if (_currentOverridePhase != phase)
            {
                _currentOverride = null;
                _currentOverridePhase = phase;
            }

            return null;
        }

        /// <summary>
        /// Checks all need-based override triggers and returns the highest priority override
        /// if conditions are met.
        /// </summary>
        private OrchestratorOverride CheckNeedBasedOverrides(DayPhase phase, CompanyNeedsState needs)
        {
            if (_overrideConfig == null)
            {
                return null;
            }

            OrchestratorOverride highestPriorityOverride = null;
            int highestPriority = -1;

            try
            {
                var needOverrides = _overrideConfig["needBasedOverrides"] as JObject;
                if (needOverrides == null)
                {
                    return null;
                }

                foreach (var overrideDef in needOverrides.Properties())
                {
                    var config = overrideDef.Value;
                    var trigger = config["trigger"];
                    var overrideData = config["override"];

                    if (trigger == null || overrideData == null)
                    {
                        continue;
                    }

                    // Check if trigger condition is met
                    var needName = trigger["need"]?.Value<string>();
                    var threshold = trigger["threshold"]?.Value<int>() ?? 30;
                    var comparison = trigger["comparison"]?.Value<string>() ?? "lessThan";

                    if (string.IsNullOrEmpty(needName))
                    {
                        continue;
                    }

                    // Get the need value
                    int needValue = GetNeedValue(needs, needName);
                    
                    // Check comparison
                    bool triggered = comparison == "lessThan" 
                        ? needValue < threshold 
                        : needValue > threshold;

                    if (!triggered)
                    {
                        continue;
                    }

                    // Check if override applies to this phase
                    var affectedPhases = overrideData["affectedPhases"]?.ToObject<List<string>>();
                    if (affectedPhases != null && affectedPhases.Count > 0)
                    {
                        if (!affectedPhases.Contains(phase.ToString()))
                        {
                            continue;
                        }
                    }

                    // Check priority
                    var priority = overrideData["priority"]?.Value<int>() ?? 50;
                    if (priority <= highestPriority)
                    {
                        continue;
                    }

                    // Create override
                    highestPriorityOverride = new OrchestratorOverride
                    {
                        Type = OverrideType.NeedBased,
                        ActivityCategory = overrideData["category"]?.Value<string>() ?? "foraging",
                        ActivityName = overrideData["name"]?.Value<string>() ?? "Override Activity",
                        Description = overrideData["description"]?.Value<string>() ?? "",
                        Reason = overrideData["reason"]?.Value<string>() ?? "Need critical",
                        Priority = priority,
                        AddressesNeed = overrideData["addressesNeed"]?.Value<string>(),
                        ReplaceBothSlots = overrideData["replaceBothSlots"]?.Value<bool>() ?? true,
                        SkillAffected = overrideData["skill"]?.Value<string>(),
                        BaseXpMin = overrideData["baseXpMin"]?.Value<int>() ?? 5,
                        BaseXpMax = overrideData["baseXpMax"]?.Value<int>() ?? 15,
                        ActivationText = config["activationText"]?.Value<string>()
                    };
                    highestPriority = priority;
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, "Error checking need-based overrides", ex);
            }

            return highestPriorityOverride;
        }

        /// <summary>
        /// Gets the value of a company need by name string.
        /// </summary>
        private int GetNeedValue(CompanyNeedsState needs, string needName)
        {
            return needName.ToLowerInvariant() switch
            {
                "supplies" => needs.Supplies,
                "rest" => needs.Rest,
                "morale" => needs.Morale,
                "readiness" => needs.Readiness,
                "equipment" => needs.Equipment,
                _ => 100
            };
        }

        /// <summary>
        /// Checks if it's time to inject a variety activity into the schedule.
        /// Based on days since last injection and weekly limits.
        /// </summary>
        public bool ShouldInjectVariety()
        {
            EnsureOverrideConfigLoaded();

            // Check world state - skip variety during intense activity or siege
            var worldSituation = WorldStateAnalyzer.AnalyzeSituation();
            if (worldSituation.ExpectedActivity == ActivityLevel.Intense)
            {
                var settings = _overrideConfig?["varietySettings"];
                if (settings?["skipDuringIntense"]?.Value<bool>() == true)
                {
                    return false;
                }
            }

            if (worldSituation.LordIs == LordSituation.SiegeAttacking || 
                worldSituation.LordIs == LordSituation.SiegeDefending)
            {
                var settings = _overrideConfig?["varietySettings"];
                if (settings?["skipDuringSiege"]?.Value<bool>() == true)
                {
                    return false;
                }
            }

            var currentDay = (int)CampaignTime.Now.ToDays;

            // Reset weekly counter if new week
            if (currentDay - _weekStartDay >= 7)
            {
                _weekStartDay = currentDay;
                _varietyInjectionsThisWeek = 0;
            }

            // Check weekly limit
            var maxPerWeek = _overrideConfig?["varietySettings"]?["maxInjectionsPerWeek"]?.Value<int>() ?? 2;
            if (_varietyInjectionsThisWeek >= maxPerWeek)
            {
                return false;
            }

            // Check minimum days between injections
            var minDays = _overrideConfig?["varietySettings"]?["minDaysBetweenInjections"]?.Value<int>() ?? 3;
            var daysSinceLastInjection = currentDay - _lastVarietyInjectionDay;
            
            if (daysSinceLastInjection < minDays)
            {
                return false;
            }

            // Check if we should roll for injection
            var maxDays = _overrideConfig?["varietySettings"]?["maxDaysBetweenInjections"]?.Value<int>() ?? 5;
            
            // After max days, always inject
            if (daysSinceLastInjection >= maxDays)
            {
                return true;
            }

            // Between min and max, use probability
            var injectionChance = _overrideConfig?["varietySettings"]?["injectionChancePerDay"]?.Value<float>() ?? 0.35f;
            var roll = TaleWorlds.Core.MBRandom.RandomFloat;
            
            return roll < injectionChance;
        }

        /// <summary>
        /// Selects a variety injection appropriate for the given phase using weighted random selection.
        /// </summary>
        public OrchestratorOverride SelectVarietyInjection(DayPhase phase)
        {
            EnsureOverrideConfigLoaded();

            if (_overrideConfig == null)
            {
                return null;
            }

            try
            {
                var varieties = _overrideConfig["varietyInjections"] as JObject;
                if (varieties == null)
                {
                    return null;
                }

                // Build weighted pool of eligible varieties for this phase
                var eligible = new List<(string id, JToken config, int weight)>();
                int totalWeight = 0;

                foreach (var variety in varieties.Properties())
                {
                    var config = variety.Value;
                    var preferredPhases = config["preferredPhases"]?.ToObject<List<string>>();

                    // Check if this variety applies to current phase
                    if (preferredPhases != null && preferredPhases.Count > 0)
                    {
                        if (!preferredPhases.Contains(phase.ToString()))
                        {
                            continue;
                        }
                    }

                    var weight = config["weight"]?.Value<int>() ?? 10;
                    eligible.Add((variety.Name, config, weight));
                    totalWeight += weight;
                }

                if (eligible.Count == 0 || totalWeight == 0)
                {
                    return null;
                }

                // Weighted random selection
                var roll = TaleWorlds.Core.MBRandom.RandomInt(totalWeight);
                int cumulative = 0;
                
                foreach (var (id, config, weight) in eligible)
                {
                    cumulative += weight;
                    if (roll < cumulative)
                    {
                        return new OrchestratorOverride
                        {
                            Type = OverrideType.VarietyInjection,
                            ActivityCategory = config["category"]?.Value<string>() ?? "patrol",
                            ActivityName = config["name"]?.Value<string>() ?? "Special Assignment",
                            Description = config["description"]?.Value<string>() ?? "",
                            Reason = "Variety assignment",
                            Priority = 50,
                            ReplaceBothSlots = false,
                            AffectedPhases = new[] { phase },
                            SkillAffected = config["skill"]?.Value<string>(),
                            BaseXpMin = config["baseXpMin"]?.Value<int>() ?? 4,
                            BaseXpMax = config["baseXpMax"]?.Value<int>() ?? 10,
                            ActivationText = config["activationText"]?.Value<string>()
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, "Error selecting variety injection", ex);
            }

            return null;
        }

        /// <summary>
        /// Gets the currently active override for the specified phase, if any.
        /// Returns null if no override is active.
        /// </summary>
        public OrchestratorOverride GetActiveOverride(DayPhase phase)
        {
            if (_currentOverride != null && _currentOverridePhase == phase)
            {
                return _currentOverride;
            }
            return null;
        }

        /// <summary>
        /// Clears the current override. Called when phase transitions or override is consumed.
        /// </summary>
        public void ClearCurrentOverride()
        {
            _currentOverride = null;
        }

        /// <summary>
        /// Ensures the override configuration is loaded from JSON.
        /// </summary>
        private void EnsureOverrideConfigLoaded()
        {
            if (_overrideConfigLoaded)
            {
                return;
            }

            try
            {
                var configPath = Path.Combine(BasePath.Name, "Modules", "Enlisted", "ModuleData",
                    "Enlisted", "Config", "orchestrator_overrides.json");

                if (!File.Exists(configPath))
                {
                    ModLogger.Warn(LogCategory, $"Override config not found at {configPath}");
                    _overrideConfigLoaded = true;
                    return;
                }

                var json = File.ReadAllText(configPath);
                _overrideConfig = JObject.Parse(json);
                _overrideConfigLoaded = true;
                ModLogger.Info(LogCategory, "Orchestrator override config loaded");
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, "Failed to load orchestrator override config", ex);
                _overrideConfigLoaded = true;
            }
        }

        #endregion
    }
}
