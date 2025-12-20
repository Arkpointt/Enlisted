using System;
using System.Linq;
// Removed: CampBulletinIntegration no longer used - bulletin screen deleted
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Features.Schedule.Config;
using Enlisted.Features.Schedule.Core;
using Enlisted.Features.Schedule.Models;
using Enlisted.Mod.Core.Logging;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.SaveSystem;

namespace Enlisted.Features.Schedule.Behaviors
{
    /// <summary>
    /// Defines the level of authority the player has over schedule management.
    /// Authority is based on tier and Lance Leader promotion status.
    /// </summary>
    public enum ScheduleAuthorityLevel
    {
        /// <summary>T1-T2: Can only view schedule, cannot modify</summary>
        ViewOnly,
        
        /// <summary>T3-T4: Can request changes with approval roll</summary>
        CanRequest,
        
        /// <summary>T5-T6 (not Lance Leader): AI recommends, player decides</summary>
        GuidedControl,
        
        /// <summary>Lance Leader (T5-T6): Full control, player sets schedule</summary>
        FullControl
    }
    
    /// <summary>
    /// Core behavior for the AI Camp Schedule system.
    /// Manages schedule generation, execution, and state persistence.
    /// </summary>
    public class ScheduleBehavior : CampaignBehaviorBase
    {
        private const string LogCategory = "Schedule";

        // Singleton instance for easy access
        public static ScheduleBehavior Instance { get; private set; }

        // Configuration (loaded once at initialization)
        private ScheduleConfig _config;
        
        // Schedule generator
        private ScheduleGenerator _generator;

        // Current schedule state (synced via SyncData, not SaveableField)
        private DailySchedule _currentSchedule;
        private int _currentCycleDay;

        // LanceNeedsState is not directly saveable - synced manually in SyncData()
        private LanceNeedsState _lanceNeeds;

        private CampaignTime _lastScheduleGeneration;
        private bool _isInitialized;
        private CampaignTime _lastDegradationTime;
        private TimeBlock _previousTimeBlock;
        private int _cachedPlayerTier;
        private bool _isManualScheduleMode;
        private SchedulePerformanceTracker _performanceTracker;
        private CampaignTime _cycleStartTime;
        private CampaignTime _nextMusterTime;
        private bool _combatInterrupted;

        /// <summary>Current daily schedule (null if not generated yet)</summary>
        public DailySchedule CurrentSchedule => _currentSchedule;

        /// <summary>Current day in the 12-day cycle (1-12)</summary>
        public int CurrentCycleDay => _currentCycleDay;

        /// <summary>Current lance needs state</summary>
        public LanceNeedsState LanceNeeds => _lanceNeeds;

        /// <summary>Schedule configuration</summary>
        public ScheduleConfig Config => _config;

        /// <summary>Player's current tier (1-6), cached for performance</summary>
        public int PlayerTier => _cachedPlayerTier;

        /// <summary>Whether player is manually controlling schedule (T6 Lance Leader)</summary>
        public bool IsManualScheduleMode => _isManualScheduleMode;

        /// <summary>Performance tracker for T5-T6 consequence system.</summary>
        public SchedulePerformanceTracker PerformanceTracker => _performanceTracker;

        /// <summary>Start time of current 12-day cycle.</summary>
        public CampaignTime CycleStartTime => _cycleStartTime;

        /// <summary>Expected time of next Pay Muster.</summary>
        public CampaignTime NextMusterTime => _nextMusterTime;

        /// <summary>Days remaining until next muster.</summary>
        public int DaysUntilMuster
        {
            get
            {
                if (_nextMusterTime == CampaignTime.Zero)
                    return 12 - _currentCycleDay + 1;
                
                var daysRemaining = (int)(_nextMusterTime.ToDays - CampaignTime.Now.ToDays);
                return Math.Max(0, daysRemaining);
            }
        }

        /// <summary>Whether schedule was interrupted by combat.</summary>
        public bool CombatInterrupted => _combatInterrupted;

        public ScheduleBehavior()
        {
            Instance = this;
            _currentCycleDay = 1;
            _lanceNeeds = new LanceNeedsState();
            _performanceTracker = new SchedulePerformanceTracker();
            _isInitialized = false;
        }

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, OnHourlyTick);
            
            // Listen for combat events to interrupt schedule.
            CampaignEvents.MapEventStarted.AddNonSerializedListener(this, OnMapEventStarted);
            CampaignEvents.MapEventEnded.AddNonSerializedListener(this, OnMapEventEnded);
        }

        public override void SyncData(IDataStore dataStore)
        {
            SaveLoadDiagnostics.SafeSyncData(this, dataStore, () =>
            {
                // Manual serialization via SyncData - do NOT use [SaveableField] attributes
                // as mixing both causes save corruption
                dataStore.SyncData("_currentSchedule", ref _currentSchedule);
                dataStore.SyncData("_currentCycleDay", ref _currentCycleDay);

                // Manually serialize LanceNeedsState properties since it's a custom class
                if (dataStore.IsLoading)
                {
                    _lanceNeeds = new LanceNeedsState();
                    int readiness = 60, equipment = 60, morale = 60, rest = 60, supplies = 60;
                    dataStore.SyncData("_lanceNeeds_Readiness", ref readiness);
                    dataStore.SyncData("_lanceNeeds_Equipment", ref equipment);
                    dataStore.SyncData("_lanceNeeds_Morale", ref morale);
                    dataStore.SyncData("_lanceNeeds_Rest", ref rest);
                    dataStore.SyncData("_lanceNeeds_Supplies", ref supplies);

                    _lanceNeeds.Readiness = readiness;
                    _lanceNeeds.Equipment = equipment;
                    _lanceNeeds.Morale = morale;
                    _lanceNeeds.Rest = rest;
                    _lanceNeeds.Supplies = supplies;
                }
                else
                {
                    // Saving
                    if (_lanceNeeds == null) _lanceNeeds = new LanceNeedsState();
                    int readiness = _lanceNeeds.Readiness;
                    int equipment = _lanceNeeds.Equipment;
                    int morale = _lanceNeeds.Morale;
                    int rest = _lanceNeeds.Rest;
                    int supplies = _lanceNeeds.Supplies;

                    dataStore.SyncData("_lanceNeeds_Readiness", ref readiness);
                    dataStore.SyncData("_lanceNeeds_Equipment", ref equipment);
                    dataStore.SyncData("_lanceNeeds_Morale", ref morale);
                    dataStore.SyncData("_lanceNeeds_Rest", ref rest);
                    dataStore.SyncData("_lanceNeeds_Supplies", ref supplies);
                }

                dataStore.SyncData("_lastScheduleGeneration", ref _lastScheduleGeneration);
                dataStore.SyncData("_isInitialized", ref _isInitialized);
                dataStore.SyncData("_lastDegradationTime", ref _lastDegradationTime);
                dataStore.SyncData("_previousTimeBlock", ref _previousTimeBlock);
                dataStore.SyncData("_cachedPlayerTier", ref _cachedPlayerTier);
                dataStore.SyncData("_isManualScheduleMode", ref _isManualScheduleMode);
                dataStore.SyncData("_performanceTracker", ref _performanceTracker);
            });
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            // Always load config and reinitialize runtime objects on session launch
            // Config is not serialized, so we need to reload it after loading a save
            if (_config == null)
            {
                _config = ScheduleConfigLoader.LoadConfig();
                if (_config == null)
                {
                    ModLogger.Error(LogCategory, "Failed to load schedule configuration on session launch");
                }
            }
            
            // Create generator if needed (not serialized)
            if (_generator == null && _config != null)
            {
                _generator = new ScheduleGenerator(_config);
            }
            
            // Full initialization on first run
            if (!_isInitialized)
            {
                Initialize();
            }
        }

        /// <summary>
        /// Initialize the schedule system.
        /// Loads configuration and sets up initial state.
        /// </summary>
        private void Initialize()
        {
            try
            {
                ModLogger.Info(LogCategory, "Initializing AI Camp Schedule system");

                // Load configuration from JSON
                _config = ScheduleConfigLoader.LoadConfig();
                if (_config == null)
                {
                    ModLogger.Error(LogCategory, "Failed to load schedule configuration");
                    return;
                }

                // Initialize lance needs if not already set
                if (_lanceNeeds == null)
                {
                    _lanceNeeds = new LanceNeedsState();
                }

                // Initialize performance tracker.
                if (_performanceTracker == null)
                {
                    _performanceTracker = new SchedulePerformanceTracker();
                }

                // Set cycle day to 1 if not set
                if (_currentCycleDay < 1)
                {
                    _currentCycleDay = 1;
                }

                // Create schedule generator
                _generator = new ScheduleGenerator(_config);

                // Initialize player tier.
                UpdatePlayerTierAndMode();

                // Initialize cycle tracking.
                if (_cycleStartTime == CampaignTime.Zero)
                {
                    _cycleStartTime = CampaignTime.Now;
                    _nextMusterTime = CampaignTime.Now + CampaignTime.Days(12);
                }
                SyncMusterTimeWithEnlistment();

                _isInitialized = true;

                ModLogger.Info(LogCategory, $"Schedule system initialized successfully");
                ModLogger.Info(LogCategory, $"  - Activities loaded: {_config.Activities.Count}");
                ModLogger.Info(LogCategory, $"  - Cycle length: {_config.CycleLengthDays} days");
                ModLogger.Info(LogCategory, $"  - Schedule enabled: {_config.EnableSchedule}");
                ModLogger.Info(LogCategory, $"  - Current cycle day: {_currentCycleDay}/12");
                ModLogger.Info(LogCategory, $"  - Player tier: {_cachedPlayerTier}");

                // Log lance needs state
                ModLogger.Debug(LogCategory, $"Lance Needs - Readiness: {_lanceNeeds.Readiness}%, " +
                    $"Equipment: {_lanceNeeds.Equipment}%, Morale: {_lanceNeeds.Morale}%, " +
                    $"Rest: {_lanceNeeds.Rest}%, Supplies: {_lanceNeeds.Supplies}%");

                // Generate initial schedule
                GenerateNewSchedule();
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, "Failed to initialize schedule system", ex);
            }
        }

        /// <summary>
        /// Hourly tick. Checks for a new day, generates schedules, and advances schedule state.
        /// Also processes daily degradation at midnight, auto-starts blocks on transitions, syncs with the muster
        /// cycle, and handles large time skips (fast-forward).
        /// </summary>
        private void OnHourlyTick()
        {
            // Guard against null config (can happen if hourly tick fires before session launch completes)
            if (!_isInitialized || _config == null || !_config.EnableSchedule)
                return;

            // Check if player is enlisted
            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment == null || !enlistment.IsEnlisted)
                return;

            // Skip schedule processing during combat.
            if (_combatInterrupted)
            {
                ModLogger.Debug(LogCategory, "Schedule processing paused - combat in progress");
                return;
            }

            int currentHour = (int)CampaignTime.Now.CurrentHourInDay;

            // Process daily degradation at midnight (hour 0).
            if (currentHour == 0 && _config.LanceNeeds.EnableDegradation)
            {
                // Check if we already processed degradation today
                if (_lastDegradationTime != null)
                {
                    double hoursSinceLastDeg = CampaignTime.Now.ToHours - _lastDegradationTime.ToHours;
                    if (hoursSinceLastDeg < 12) // Less than half a day
                    {
                        // Skip to next check
                    }
                    else
                    {
                        ProcessMidnightTasks(enlistment);
                    }
                }
                else
                {
                    ProcessMidnightTasks(enlistment);
                }
            }

            // Check if it's dawn (hour 6) - time to generate new schedule
            if (currentHour == 6)
            {
                // Check if we already generated today
                if (_lastScheduleGeneration != null)
                {
                    double hoursSinceLastGen = CampaignTime.Now.ToHours - _lastScheduleGeneration.ToHours;
                    if (hoursSinceLastGen < 12) // Less than half a day
                    {
                        // Skip generation
                    }
                    else
                    {
                        GenerateNewSchedule();
                    }
                }
                else
                {
                    GenerateNewSchedule();
                }
            }

            // Check for time block transitions and auto-start blocks.
            TimeBlock currentTimeBlock = GetCurrentTimeBlock();
            if (currentTimeBlock != _previousTimeBlock)
            {
                ModLogger.Debug(LogCategory, $"Time block changed: {_previousTimeBlock} -> {currentTimeBlock}");
                
                // Check if we skipped time blocks (fast forward).
                int skippedBlocks = (int)currentTimeBlock - (int)_previousTimeBlock;
                if (skippedBlocks > 1)
                {
                    ModLogger.Info(LogCategory, $"Time skip detected - {skippedBlocks - 1} blocks skipped");
                    HandleTimeSkipRecovery();
                }
                
                _previousTimeBlock = currentTimeBlock;

                // Auto-start the new block if we have a schedule
                if (_currentSchedule != null && _currentSchedule.Blocks != null && _currentSchedule.Blocks.Count > 0)
                {
                    ScheduleExecutor.AutoStartCurrentBlock();
                }
            }

            // Check if Pay Muster is pending (sync with enlistment).
            if (enlistment.IsPayMusterPending && _currentCycleDay >= 11)
            {
                // Muster is imminent - the actual reset happens in OnPayMusterCompleted
                ModLogger.Debug(LogCategory, "Pay Muster pending - schedule cycle will reset upon resolution");
            }
        }

        /// <summary>
        /// Process midnight tasks: degradation, performance recording, cycle advancement.
        /// </summary>
        private void ProcessMidnightTasks(EnlistmentBehavior enlistment)
        {
            // Get lord's party for context-aware degradation
            var lord = enlistment.EnlistedLord;
            var lordParty = lord?.PartyBelongedTo;

            ModLogger.Debug(LogCategory, "Midnight - processing daily tasks");
            
            // Record daily performance before resetting
            RecordDailyPerformance();
            
            // Process lance needs degradation
            LanceNeedsManager.ProcessDailyDegradation(_lanceNeeds, lordParty);
            _lastDegradationTime = CampaignTime.Now;

            // Check for critical needs and log warnings
            var criticalNeeds = LanceNeedsManager.CheckCriticalNeeds(_lanceNeeds);
            if (criticalNeeds.Count > 0)
            {
                foreach (var warning in criticalNeeds)
                {
                    ModLogger.Warn(LogCategory, warning.Value);
                }
                
                // Critical needs logged above - bulletin integration removed
            }
        }

        /// <summary>
        /// Generate a new daily schedule.
        /// Respects manual mode for T6 players.
        /// </summary>
        private void GenerateNewSchedule()
        {
            try
            {
                var enlistment = EnlistmentBehavior.Instance;
                if (enlistment == null || !enlistment.IsEnlisted)
                {
                    ModLogger.Debug(LogCategory, "Skipping schedule generation - player not enlisted");
                    return;
                }

                // Update tier status.
                UpdatePlayerTierAndMode();

                // Skip AI generation if in manual mode (T6 player controls schedule).
                if (_isManualScheduleMode)
                {
                    ModLogger.Info(LogCategory, $"Day {_currentCycleDay}/12 - Manual mode active, waiting for player to set schedule");
                    // Player must explicitly create and set their schedule
                    // For now, just advance the cycle day
                    _currentCycleDay++;
                    if (_currentCycleDay > _config.CycleLengthDays)
                    {
                        _currentCycleDay = 1;
                        ModLogger.Info(LogCategory, "12-day cycle complete, starting new cycle");
                    }
                    return;
                }

                // Get lord's party (which may be in an army)
                var lord = enlistment.EnlistedLord;
                if (lord == null)
                {
                    ModLogger.Warn(LogCategory, "Cannot generate schedule - no enlisted lord");
                    return;
                }

                var lordParty = lord.PartyBelongedTo;
                if (lordParty == null)
                {
                    ModLogger.Warn(LogCategory, "Cannot generate schedule - lord has no party");
                    return;
                }

                ModLogger.Info(LogCategory, $"Generating new AI schedule for Day {_currentCycleDay}/12");

                // Generate schedule
                _currentSchedule = _generator.GenerateSchedule(lordParty, _currentCycleDay);
                _lastScheduleGeneration = CampaignTime.Now;

                // Schedule generated - bulletin integration removed

                // Advance cycle day
                _currentCycleDay++;
                if (_currentCycleDay > _config.CycleLengthDays)
                {
                    _currentCycleDay = 1;
                    ModLogger.Info(LogCategory, "12-day cycle complete, starting new cycle");
                }

                ModLogger.LogOnce("Schedule", "first_schedule_generated", "First schedule generated successfully");
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, "Failed to generate schedule", ex);
            }
        }

        /// <summary>
        /// Get an activity definition by ID.
        /// </summary>
        public ScheduleActivityDefinition GetActivityById(string activityId)
        {
            if (_config == null || _config.Activities == null)
            {
                return null;
            }

            return _config.Activities.Find(a => a.Id == activityId);
        }

        /// <summary>
        /// Get an activity definition by block type.
        /// </summary>
        public ScheduleActivityDefinition GetActivityByType(ScheduleBlockType blockType)
        {
            if (_config == null || _config.Activities == null)
            {
                return null;
            }

            return _config.Activities.Find(a => a.BlockType == blockType);
        }

        /// <summary>
        /// Mark a schedule block as completed and process need recovery.
        /// </summary>
        public void CompleteScheduleBlock(ScheduledBlock block)
        {
            if (block == null)
            {
                ModLogger.Warn(LogCategory, "Cannot complete null schedule block");
                return;
            }

            if (block.IsCompleted)
            {
                ModLogger.Debug(LogCategory, $"Block {block.Title} already completed, skipping");
                return;
            }

            ModLogger.Info(LogCategory, $"Completing schedule block: {block.Title} ({block.BlockType})");

            // Mark as completed
            block.IsCompleted = true;
            block.IsActive = false;

            // Process need recovery
            LanceNeedsManager.ProcessActivityRecovery(_lanceNeeds, block);

            // Track completion in performance tracker.
            if (_performanceTracker != null)
            {
                _performanceTracker.SuccessfulDutiesCount++;
                _performanceTracker.TotalFatigueAccumulated += Math.Max(0, block.FatigueCost);
            }

            // Block completed - bulletin integration removed

            // Log final needs state
            ModLogger.Debug(LogCategory, $"Needs after completion - Readiness: {_lanceNeeds.Readiness}%, " +
                $"Equipment: {_lanceNeeds.Equipment}%, Morale: {_lanceNeeds.Morale}%, " +
                $"Rest: {_lanceNeeds.Rest}%, Supplies: {_lanceNeeds.Supplies}%");
        }

        /// <summary>
        /// Get the current active schedule block based on time of day.
        /// </summary>
        public ScheduledBlock GetCurrentActiveBlock()
        {
            if (_currentSchedule == null || _currentSchedule.Blocks == null || _currentSchedule.Blocks.Count == 0)
            {
                return null;
            }

            // Get current time block based on hour
            TimeBlock currentTimeBlock = GetCurrentTimeBlock();

            // Find the block for this time period
            return _currentSchedule.Blocks.Find(b => b.TimeBlock == currentTimeBlock);
        }

        /// <summary>
        /// Determine which time block we're currently in based on hour of day.
        /// Simplified to 4 blocks: Morning, Afternoon, Dusk, Night.
        /// </summary>
        private TimeBlock GetCurrentTimeBlock()
        {
            int hour = (int)CampaignTime.Now.CurrentHourInDay;

            // Time block mapping (6-hour blocks):
            // Morning: 6-12, Afternoon: 12-18, Dusk: 18-22, Night: 22-6
            if (hour >= 6 && hour < 12) return TimeBlock.Morning;
            if (hour >= 12 && hour < 18) return TimeBlock.Afternoon;
            if (hour >= 18 && hour < 22) return TimeBlock.Dusk;
            return TimeBlock.Night; // 22-6
        }

        // ===== T5-T6 Leadership System =====

        /// <summary>
        /// Update cached player tier and adjust schedule mode accordingly.
        /// T5 uses guided mode, and T6 can use full management mode.
        /// </summary>
        private void UpdatePlayerTierAndMode()
        {
            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment == null)
            {
                _cachedPlayerTier = 1;
                _isManualScheduleMode = false;
                return;
            }

            int newTier = enlistment.EnlistmentTier;
            if (newTier != _cachedPlayerTier)
            {
                int oldTier = _cachedPlayerTier;
                _cachedPlayerTier = newTier;

                ModLogger.Info(LogCategory, $"Player tier changed: {oldTier} -> {newTier}");

                // Tier-specific mode changes.
                if (newTier == 5)
                {
                    // T5 "Lance Second" - tutorial mode
                    ModLogger.Info(LogCategory, "Player promoted to Lance Second (T5) - tutorial mode active");
                    _isManualScheduleMode = false; // Still AI-controlled, but with input
                }
                else if (newTier >= 6)
                {
                    // T6 "Lance Leader" - full management mode unlocked
                    ModLogger.Info(LogCategory, "Player promoted to Lance Leader (T6+) - manual schedule mode available");
                    // Player can choose to enable manual mode via menu
                    // For now, default to AI mode until player explicitly enables manual mode
                }
                else
                {
                    // T1-T4 - standard enlisted mode
                    _isManualScheduleMode = false;
                }
            }
        }

        /// <summary>
        /// Enable manual schedule mode (T6+ only).
        /// Player takes full control of schedule assignments.
        /// </summary>
        public void EnableManualScheduleMode()
        {
            if (_cachedPlayerTier < 6)
            {
                ModLogger.Warn(LogCategory, $"Cannot enable manual mode - player tier {_cachedPlayerTier} < 6");
                return;
            }

            _isManualScheduleMode = true;
            ModLogger.Info(LogCategory, "Manual schedule mode enabled - player controls assignments");
        }

        /// <summary>
        /// Disable manual schedule mode and return to AI-controlled scheduling.
        /// </summary>
        public void DisableManualScheduleMode()
        {
            _isManualScheduleMode = false;
            ModLogger.Info(LogCategory, "Manual schedule mode disabled - AI controls assignments");
        }

        /// <summary>
        /// Set a custom player-created schedule (Lance Leader only).
        /// Schedule persists and AI will not regenerate until reverted.
        /// </summary>
        public void SetManualSchedule(DailySchedule customSchedule)
        {
            if (!CanSetScheduleFully())
            {
                ModLogger.Warn(LogCategory, "Player attempted to set manual schedule without Lance Leader authority");
                return;
            }

            _currentSchedule = customSchedule;
            _isManualScheduleMode = true;
            ModLogger.Info(LogCategory, $"Player set manual schedule for cycle day {_currentCycleDay}");
        }

        /// <summary>
        /// Revert to AI-controlled schedule (allow AI to regenerate daily).
        /// </summary>
        public void RevertToAutoSchedule()
        {
            _isManualScheduleMode = false;
            GenerateNewSchedule(); // AI takes over again
            ModLogger.Info(LogCategory, "Reverted to AI-controlled schedule");
        }

        /// <summary>
        /// Get AI recommendations for schedule (used in T5 tutorial and T6 "auto-assign").
        /// </summary>
        public DailySchedule GetAIRecommendedSchedule()
        {
            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment == null || !enlistment.IsEnlisted)
            {
                ModLogger.Warn(LogCategory, "Cannot get AI recommendations - player not enlisted");
                return null;
            }

            var lord = enlistment.EnlistedLord;
            var lordParty = lord?.PartyBelongedTo;

            if (lordParty == null)
            {
                ModLogger.Warn(LogCategory, "Cannot get AI recommendations - no lord party");
                return null;
            }

            // Generate schedule using AI logic
            var recommendedSchedule = _generator.GenerateSchedule(lordParty, _currentCycleDay);
            ModLogger.Debug(LogCategory, "Generated AI-recommended schedule");

            return recommendedSchedule;
        }

        /// <summary>
        /// Check if player should see T5 tutorial prompts.
        /// </summary>
        public bool ShouldShowT5Tutorial()
        {
            return _cachedPlayerTier == 5;
        }

        /// <summary>
        /// Check if player can use manual schedule management.
        /// Returns true if player is promoted to Lance Leader (T5-T6 role).
        /// NOTE: Currently checks tier >= 6 as placeholder until Lance Leader promotion system is implemented.
        /// TODO: Update this to check actual Lance Leader role when promotion system is complete.
        /// </summary>
        public bool CanUseManualManagement()
        {
            // Placeholder: Check tier >= 6
            // Future: Check EnlistmentBehavior.Instance.IsLanceLeader or similar role flag
            return _cachedPlayerTier >= 6;
        }
        
        /// <summary>
        /// Get the player's current schedule authority level.
        /// Determines what actions the player can take with the schedule.
        /// </summary>
        public ScheduleAuthorityLevel GetPlayerScheduleAuthority()
        {
            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment == null || !enlistment.IsEnlisted)
            {
                return ScheduleAuthorityLevel.ViewOnly;
            }
            
            int tier = _cachedPlayerTier;
            bool isLanceLeader = CanUseManualManagement();
            
            // Lance Leader has full control
            if (isLanceLeader && tier >= 5)
            {
                return ScheduleAuthorityLevel.FullControl;
            }
            
            // T5-T6 (not Lance Leader) have guided control
            if (tier >= 5)
            {
                return ScheduleAuthorityLevel.GuidedControl;
            }
            
            // T3-T4 can request changes
            if (tier >= 3)
            {
                return ScheduleAuthorityLevel.CanRequest;
            }
            
            // T1-T2 view only
            return ScheduleAuthorityLevel.ViewOnly;
        }
        
        /// <summary>
        /// Check if player can view the schedule (all enlisted players can).
        /// </summary>
        public bool CanViewSchedule()
        {
            return EnlistmentBehavior.Instance?.IsEnlisted == true;
        }
        
        /// <summary>
        /// Check if player can request schedule changes (T3-T4 with approval roll).
        /// </summary>
        public bool CanRequestScheduleChange()
        {
            var authority = GetPlayerScheduleAuthority();
            return authority == ScheduleAuthorityLevel.CanRequest 
                || authority == ScheduleAuthorityLevel.GuidedControl;
        }
        
        /// <summary>
        /// Check if player can set schedule with AI guidance (T5-T6 non-Leader).
        /// </summary>
        public bool CanSetScheduleWithGuidance()
        {
            var authority = GetPlayerScheduleAuthority();
            return authority == ScheduleAuthorityLevel.GuidedControl 
                || authority == ScheduleAuthorityLevel.FullControl;
        }
        
        /// <summary>
        /// Check if player can set schedule fully without restrictions (Lance Leader).
        /// </summary>
        public bool CanSetScheduleFully()
        {
            return GetPlayerScheduleAuthority() == ScheduleAuthorityLevel.FullControl;
        }
        
        /// <summary>
        /// Calculate the approval likelihood for a schedule change request (T3-T4).
        /// Returns percentage (0-100) chance that the lance leader/lord will approve.
        /// </summary>
        public int CalculateApprovalLikelihood(ScheduledBlock currentBlock, ScheduleActivityDefinition newActivity)
        {
            if (currentBlock == null || newActivity == null)
            {
                return 0;
            }
            
            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment == null || !enlistment.IsEnlisted)
            {
                return 0;
            }
            
            int likelihood = 50; // Base chance
            
            // Factor 1: Lord's opinion of player (+/- relation)
            var lord = enlistment.EnlistedLord;
            if (lord != null)
            {
                int relation = lord.GetRelation(TaleWorlds.CampaignSystem.Hero.MainHero);
                likelihood += (int)(relation * 0.3f); // -30 to +30 based on relation
            }
            
            // Factor 2: Activity appropriateness (fits lord's current objective?)
            var lordParty = lord?.PartyBelongedTo;
            if (lordParty != null && newActivity.FavoredByObjectives != null)
            {
                var objective = ArmyStateAnalyzer.GetLordObjective(lordParty);
                string objectiveStr = objective.ToString();
                if (newActivity.FavoredByObjectives.Contains(objectiveStr))
                {
                    likelihood += 20; // Activity fits the current situation
                }
            }
            
            // Factor 3: Time appropriateness (is this activity preferred for this time?)
            if (newActivity.PreferredTimeBlocks != null && newActivity.PreferredTimeBlocks.Contains(currentBlock.TimeBlock))
            {
                likelihood += 10; // Good time for this activity
            }
            
            // Factor 4: Lance needs (does this help critical needs?)
            if (_lanceNeeds != null && newActivity.NeedRecovery != null)
            {
                // Check if any critical needs would be helped
                foreach (var needPair in newActivity.NeedRecovery)
                {
                    string needType = needPair.Key.ToLowerInvariant();
                    int recoveryAmount = needPair.Value;
                    
                    // Check if this need is critical (< 40%)
                    bool isCritical = false;
                    switch (needType)
                    {
                        case "readiness":
                            isCritical = _lanceNeeds.Readiness < 40;
                            break;
                        case "equipment":
                            isCritical = _lanceNeeds.Equipment < 40;
                            break;
                        case "morale":
                            isCritical = _lanceNeeds.Morale < 40;
                            break;
                        case "rest":
                            isCritical = _lanceNeeds.Rest < 40;
                            break;
                        case "supplies":
                            isCritical = _lanceNeeds.Supplies < 40;
                            break;
                    }
                    
                    if (isCritical && recoveryAmount > 0)
                    {
                        likelihood += 15; // Helps with critical need
                        break; // Only count once
                    }
                }
            }
            
            // Factor 5: Player tier (higher tier = more respect for opinions)
            if (_cachedPlayerTier >= 4)
            {
                likelihood += 5; // Senior soldiers get slightly more consideration
            }
            
            // Clamp to reasonable range (never 0% or 100%)
            return System.Math.Max(5, System.Math.Min(95, likelihood));
        }
        
        /// <summary>
        /// Request a schedule change for a specific block (T3-T4 only).
        /// Returns true if approved, false if denied.
        /// </summary>
        public bool RequestScheduleChange(ScheduledBlock block, ScheduleActivityDefinition newActivity, out int approvalChance)
        {
            approvalChance = 0;
            
            if (!CanRequestScheduleChange())
            {
                ModLogger.Warn(LogCategory, "Player attempted to request schedule change without authority");
                return false;
            }
            
            if (block == null || newActivity == null)
            {
                ModLogger.Warn(LogCategory, "Cannot request schedule change - null block or activity");
                return false;
            }
            
            // Calculate approval likelihood
            approvalChance = CalculateApprovalLikelihood(block, newActivity);
            
            // Roll for approval
            int roll = MBRandom.RandomInt(0, 100);
            bool approved = roll < approvalChance;
            
            if (approved)
            {
                // Update the block's activity
                var activityDef = _config.Activities.FirstOrDefault(a => a.Id == newActivity.Id);
                if (activityDef != null)
                {
                    block.BlockType = activityDef.BlockType;
                    block.Title = activityDef.Title ?? string.Empty;
                    block.Description = activityDef.Description ?? string.Empty;
                    block.FatigueCost = activityDef.FatigueCost;
                    block.XPReward = activityDef.XPReward;
                    
                    ModLogger.Info(LogCategory, $"Schedule change request APPROVED: {newActivity.Id} at {block.TimeBlock} ({approvalChance}% chance, rolled {roll})");
                }
            }
            else
            {
                ModLogger.Info(LogCategory, $"Schedule change request DENIED: {newActivity.Id} at {block.TimeBlock} ({approvalChance}% chance, rolled {roll})");
            }
            
            return approved;
        }

        // ===== Pay Muster Integration =====

        /// <summary>
        /// Called when Pay Muster is completed to reset the 12-day schedule cycle.
        /// Generates performance feedback and starts a fresh cycle.
        /// </summary>
        public void OnPayMusterCompleted()
        {
            ModLogger.Info(LogCategory, "Pay Muster completed - resetting schedule cycle");

            // Generate and log performance feedback for T5-T6 players.
            if (_cachedPlayerTier >= 5 && _performanceTracker != null)
            {
                int score = _performanceTracker.CalculatePerformanceScore();
                string rating = _performanceTracker.GetPerformanceRating(score);

                ModLogger.Info(LogCategory, $"Performance Review: Score={score}, Rating={rating}");

                // Performance feedback - bulletin integration removed

                // Show feedback message to player
                string feedbackMessage = GetPerformanceFeedbackMessage(score, rating);
                InformationManager.DisplayMessage(new InformationMessage(
                    feedbackMessage,
                    score >= 60 ? Color.FromUint(0xFF88FF88) : Color.FromUint(0xFFFF8888)));

                // Apply consequences if warranted
                if (_performanceTracker.ShouldTriggerConsequences(score))
                {
                    ApplyPerformanceConsequences(score, rating);
                }
            }

            // Reset cycle
            _currentCycleDay = 1;
            _cycleStartTime = CampaignTime.Now;
            _nextMusterTime = CampaignTime.Now + CampaignTime.Days(12);

            // Reset performance tracker for new cycle
            _performanceTracker?.Reset();
            _performanceTracker.CurrentCycleDay = 1;

            // Generate fresh schedule for new cycle (unless in manual mode)
            if (!_isManualScheduleMode)
            {
                GenerateNewSchedule();
            }

            ModLogger.Info(LogCategory, $"New 12-day cycle started. Next muster expected: {_nextMusterTime}");
        }

        /// <summary>
        /// Sync next muster time with EnlistmentBehavior's pay schedule.
        /// Called during initialization and when pay schedule changes.
        /// </summary>
        public void SyncMusterTimeWithEnlistment()
        {
            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment == null)
                return;

            // Get the next payday from enlistment behavior
            // EnlistmentBehavior tracks _nextPayday internally
            // For now, we estimate based on 12-day cycle
            if (_nextMusterTime == CampaignTime.Zero || _cycleStartTime == CampaignTime.Zero)
            {
                _cycleStartTime = CampaignTime.Now;
                _nextMusterTime = CampaignTime.Now + CampaignTime.Days(12);
            }

            ModLogger.Debug(LogCategory, $"Muster time synced: CycleStart={_cycleStartTime}, NextMuster={_nextMusterTime}");
        }

        /// <summary>
        /// Get a narrative performance feedback message based on score.
        /// </summary>
        private string GetPerformanceFeedbackMessage(int score, string rating)
        {
            if (score >= 90)
                return $"Your lance leadership this cycle was {rating}. The lord has taken notice.";
            if (score >= 75)
                return $"Your performance this cycle was {rating}. Well done, soldier.";
            if (score >= 60)
                return $"Your service this cycle was {rating}. Keep up the work.";
            if (score >= 45)
                return $"Your performance was {rating}. There is room for improvement.";
            if (score >= 30)
                return $"Your leadership this cycle was {rating}. The lance suffered for it.";
            return $"Your performance was {rating}. The lord is not pleased.";
        }

        /// <summary>
        /// Apply consequences for exceptional or poor performance.
        /// Basic consequence system for T5-T6 leaders.
        /// </summary>
        private void ApplyPerformanceConsequences(int score, string rating)
        {
            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment == null)
                return;

            var lord = enlistment.EnlistedLord;
            if (lord == null)
                return;

            if (score >= 75)
            {
                // Positive consequences: improved relation with lord, bonus XP
                int relationBonus = (score - 75) / 10; // 0-2 relation points
                if (relationBonus > 0)
                {
                    lord.SetPersonalRelation(Hero.MainHero, lord.GetRelation(Hero.MainHero) + relationBonus);
                    ModLogger.Info(LogCategory, $"Performance bonus: +{relationBonus} relation with lord");
                }

                // Bonus XP for excellent management
                int bonusXP = (score - 75) * 2;
                Hero.MainHero.AddSkillXp(DefaultSkills.Leadership, bonusXP);
                ModLogger.Info(LogCategory, $"Performance bonus: +{bonusXP} Leadership XP");
            }
            else if (score < 45)
            {
                // Negative consequences: reduced relation with lord
                int relationPenalty = (45 - score) / 15; // 1-3 relation penalty
                if (relationPenalty > 0)
                {
                    lord.SetPersonalRelation(Hero.MainHero, lord.GetRelation(Hero.MainHero) - relationPenalty);
                    ModLogger.Info(LogCategory, $"Performance penalty: -{relationPenalty} relation with lord");

                    // Inform player of consequence
                    InformationManager.DisplayMessage(new InformationMessage(
                        $"Your poor leadership has cost you standing with {lord.Name}.",
                        Color.FromUint(0xFFFF6666)));
                }
            }
        }

        // ===== Combat Interrupt Handling =====

        /// <summary>
        /// Handle map event start (combat, siege, etc.) - interrupts current schedule block.
        /// </summary>
        private void OnMapEventStarted(MapEvent mapEvent, PartyBase attackerParty, PartyBase defenderParty)
        {
            // Check if player is involved
            if (Hero.MainHero?.PartyBelongedTo == null)
                return;

            bool playerInvolved = attackerParty?.MobileParty == Hero.MainHero.PartyBelongedTo ||
                                  defenderParty?.MobileParty == Hero.MainHero.PartyBelongedTo ||
                                  attackerParty?.MobileParty == Hero.MainHero.PartyBelongedTo?.Army?.LeaderParty ||
                                  defenderParty?.MobileParty == Hero.MainHero.PartyBelongedTo?.Army?.LeaderParty;

            if (!playerInvolved)
                return;

            _combatInterrupted = true;
            ModLogger.Info(LogCategory, "Schedule interrupted by combat");

            // Mark current block as interrupted (not failed, just paused)
            var currentBlock = GetCurrentActiveBlock();
            if (currentBlock != null && !currentBlock.IsCompleted)
            {
                ModLogger.Debug(LogCategory, $"Block '{currentBlock.Title}' paused for combat");
            }
        }

        /// <summary>
        /// Handle map event end - resume schedule after combat.
        /// </summary>
        private void OnMapEventEnded(MapEvent mapEvent)
        {
            if (!_combatInterrupted)
                return;

            _combatInterrupted = false;
            ModLogger.Info(LogCategory, "Combat ended - schedule resuming");

            // Check if we need to catch up on missed blocks due to time skip during combat
            HandleTimeSkipRecovery();
        }

        /// <summary>
        /// Handle time skips (fast forward, waiting, combat time compression).
        /// Called during hourly tick if significant time has passed.
        /// </summary>
        private void HandleTimeSkipRecovery()
        {
            if (_currentSchedule == null || _currentSchedule.Blocks == null)
                return;

            var currentTimeBlock = GetCurrentTimeBlock();

            // Find any blocks that should have completed but weren't marked
            foreach (var block in _currentSchedule.Blocks)
            {
                if (block.IsCompleted)
                    continue;

                // If this block's time has passed and it wasn't completed, auto-complete it
                if ((int)block.TimeBlock < (int)currentTimeBlock)
                {
                    ModLogger.Debug(LogCategory, $"Auto-completing skipped block: {block.Title}");
                    block.IsCompleted = true;

                    // Still apply recovery/degradation but reduce effectiveness (player missed the activity)
                    // Partial credit: 50% of normal effect
                    var halfRecoveryBlock = new ScheduledBlock(
                        block.TimeBlock,
                        block.BlockType,
                        block.Title,
                        block.Description,
                        block.FatigueCost / 2,
                        block.XPReward / 2,
                        0, // No event chance for skipped blocks
                        block.ScheduledTime
                    );
                    LanceNeedsManager.ProcessActivityRecovery(_lanceNeeds, halfRecoveryBlock);

                    // Track as partial completion in performance tracker
                    if (_performanceTracker != null)
                    {
                        _performanceTracker.SuccessfulDutiesCount++; // Credit for auto-complete
                    }
                }
            }
        }

        /// <summary>
        /// Record daily performance snapshot for the tracker.
        /// Called at end of each day.
        /// </summary>
        public void RecordDailyPerformance()
        {
            if (_performanceTracker == null || _lanceNeeds == null)
                return;

            // Get current lord objective
            var enlistment = EnlistmentBehavior.Instance;
            var lordParty = enlistment?.EnlistedLord?.PartyBelongedTo;
            var objective = lordParty != null ? ArmyStateAnalyzer.GetLordObjective(lordParty) : LordObjective.Unknown;

            // Record snapshot
            _performanceTracker.RecordDailySnapshot(_currentCycleDay, _lanceNeeds, objective);
            _performanceTracker.CurrentCycleDay = _currentCycleDay;

            // Check if lord's orders were met today (based on schedule adherence)
            bool ordersFollowed = CheckIfLordOrdersFollowed();
            if (ordersFollowed)
            {
                _performanceTracker.LordOrdersMetCount++;
            }
            else
            {
                _performanceTracker.LordOrdersFailedCount++;
            }

            // Check if critical needs were addressed
            bool hasCriticalNeed = _lanceNeeds.Readiness < 30 || _lanceNeeds.Equipment < 30 ||
                                   _lanceNeeds.Morale < 30 || _lanceNeeds.Rest < 30 || _lanceNeeds.Supplies < 30;

            if (hasCriticalNeed)
            {
                // Check if schedule addressed the critical need
                bool addressedCritical = CheckIfCriticalNeedsAddressed();
                if (addressedCritical)
                {
                    _performanceTracker.CriticalNeedsAddressedCount++;
                }
                else
                {
                    _performanceTracker.CriticalNeedsIgnoredCount++;
                }
            }

            ModLogger.Debug(LogCategory, $"Daily performance recorded: Day {_currentCycleDay}, " +
                $"LordOrders: {_performanceTracker.LordOrdersMetCount}/{_performanceTracker.LordOrdersFailedCount}, " +
                $"CriticalNeeds: {_performanceTracker.CriticalNeedsAddressedCount}/{_performanceTracker.CriticalNeedsIgnoredCount}");
        }

        /// <summary>
        /// Check if today's schedule followed the lord's orders.
        /// </summary>
        private bool CheckIfLordOrdersFollowed()
        {
            if (_currentSchedule == null)
                return true; // No schedule = no violation

            // Check if schedule blocks aligned with lord's objective
            var enlistment = EnlistmentBehavior.Instance;
            var lordParty = enlistment?.EnlistedLord?.PartyBelongedTo;
            if (lordParty == null)
                return true;

            var objective = ArmyStateAnalyzer.GetLordObjective(lordParty);
            var priority = ArmyStateAnalyzer.GetObjectivePriority(objective, lordParty);

            // High priority orders = stricter check
            if (priority == LordOrderPriority.Critical || priority == LordOrderPriority.High)
            {
                // At least half of blocks should align with objective
                int alignedBlocks = _currentSchedule.Blocks.Count(b => b.IsCompleted);
                return alignedBlocks >= _currentSchedule.Blocks.Count / 2;
            }

            // Normal/low priority = more lenient
            return true;
        }

        /// <summary>
        /// Check if today's schedule addressed critical needs.
        /// </summary>
        private bool CheckIfCriticalNeedsAddressed()
        {
            if (_currentSchedule == null || _lanceNeeds == null)
                return false;

            // Check if any completed blocks had recovery for critical needs
            foreach (var block in _currentSchedule.Blocks.Where(b => b.IsCompleted))
            {
                var activity = GetActivityById(block.ActivityId);
                if (activity?.NeedRecovery == null)
                    continue;

                // Check if activity addresses a critical need
                if (_lanceNeeds.Rest < 30 && activity.NeedRecovery.ContainsKey("Rest"))
                    return true;
                if (_lanceNeeds.Morale < 30 && activity.NeedRecovery.ContainsKey("Morale"))
                    return true;
                if (_lanceNeeds.Equipment < 30 && activity.NeedRecovery.ContainsKey("Equipment"))
                    return true;
                if (_lanceNeeds.Readiness < 30 && activity.NeedRecovery.ContainsKey("Readiness"))
                    return true;
                if (_lanceNeeds.Supplies < 30 && activity.NeedRecovery.ContainsKey("Supplies"))
                    return true;
            }

            return false;
        }
    }
}

