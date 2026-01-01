using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Enlisted.Features.Camp.Models;
using Enlisted.Features.Company;
using Enlisted.Features.Content;
using Enlisted.Features.Content.Models;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Features.Escalation;
using Enlisted.Features.Orders.Behaviors;
using Enlisted.Features.Ranks;
using Enlisted.Mod.Core.Logging;
using Enlisted.Mod.Core.SaveSystem;
using Newtonsoft.Json.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace Enlisted.Features.Camp
{
    /// <summary>
    /// Generates camp life opportunities based on world state, camp context, player state, and history.
    /// The heart of the living camp simulation. Opportunities are filtered, scored, and selected
    /// to create a realistic camp experience that adapts to the player.
    /// </summary>
    public sealed class CampOpportunityGenerator : CampaignBehaviorBase
    {
        private const string LogCategory = "CampLife";
        private const int FitnessThreshold = 40;
        private const int MaxOpportunitiesDefault = 3;

        /// <summary>Singleton instance for external access.</summary>
        public static CampOpportunityGenerator Instance { get; private set; }

        // Opportunity definitions loaded from JSON
        private List<CampOpportunity> _opportunityDefinitions = new List<CampOpportunity>();

        // History tracking
        private OpportunityHistory _history = new OpportunityHistory();

        // Player commitments
        private PlayerCommitments _commitments = new PlayerCommitments();

        // Cached opportunities for current phase
        private List<CampOpportunity> _cachedOpportunities;
        private DayPhase _cachePhase = DayPhase.Night;

        public CampOpportunityGenerator()
        {
            Instance = this;
        }

        /// <summary>Current player commitments.</summary>
        public PlayerCommitments Commitments => _commitments;

        /// <summary>Opportunity history for learning.</summary>
        public OpportunityHistory History => _history;

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, OnHourlyTick);
            ModLogger.Info(LogCategory, "CampOpportunityGenerator registered");
        }

        /// <summary>
        /// Hourly tick handler to check for scheduled commitments that should fire.
        /// Checks at phase boundaries: Dawn (6am), Midday (12pm), Dusk (6pm), Night (12am).
        /// </summary>
        private void OnHourlyTick()
        {
            try
            {
                var currentHour = CampaignTime.Now.GetHourOfDay;
                var currentDay = (int)CampaignTime.Now.ToDays;

                // Check at phase boundaries only
                if (currentHour != 6 && currentHour != 12 && currentHour != 18 && currentHour != 0)
                {
                    return;
                }

                var currentPhase = currentHour switch
                {
                    6 => "Dawn",
                    12 => "Midday",
                    18 => "Dusk",
                    0 or 24 => "Night",
                    _ => null
                };

                if (currentPhase == null)
                {
                    return;
                }

                FireScheduledCommitments(currentPhase, currentDay);
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, "Error in OnHourlyTick checking commitments", ex);
            }
        }

        /// <summary>
        /// Fires all commitments scheduled for the given phase and day.
        /// </summary>
        private void FireScheduledCommitments(string phase, int day)
        {
            var toFire = _commitments.GetCommitmentsForPhase(phase, day);

            if (toFire.Count == 0)
            {
                return;
            }

            ModLogger.Info(LogCategory, $"Firing {toFire.Count} scheduled commitments for {phase} on day {day}");

            foreach (var commitment in toFire)
            {
                try
                {
                    FireCommitment(commitment);
                }
                catch (Exception ex)
                {
                    ModLogger.Error(LogCategory, $"Error firing commitment {commitment.OpportunityId}", ex);
                }
                finally
                {
                    _commitments.RemoveCommitment(commitment.OpportunityId);
                }
            }
        }

        /// <summary>
        /// Fires a single commitment by delivering the target decision as an event.
        /// </summary>
        private void FireCommitment(ScheduledCommitment commitment)
        {
            // Get the decision definition
            var decision = DecisionCatalog.GetDecision(commitment.TargetDecisionId);
            if (decision == null)
            {
                ModLogger.Warn(LogCategory, $"Commitment target decision not found: {commitment.TargetDecisionId}");
                InformationManager.DisplayMessage(new InformationMessage(
                    $"The scheduled activity ({commitment.Title}) is no longer available.",
                    Colors.Yellow));
                return;
            }

            // Convert to event and queue for delivery
            var eventDef = new EventDefinition
            {
                Id = decision.Id,
                TitleId = decision.TitleId,
                TitleFallback = decision.TitleFallback,
                SetupId = decision.SetupId,
                SetupFallback = decision.SetupFallback,
                Category = decision.Category,
                Requirements = decision.Requirements,
                Timing = decision.Timing,
                Options = decision.Options
            };

            var deliveryManager = EventDeliveryManager.Instance;
            if (deliveryManager != null)
            {
                deliveryManager.QueueEvent(eventDef);
                ModLogger.Info(LogCategory, $"Fired scheduled commitment: {commitment.OpportunityId} -> {commitment.TargetDecisionId}");

                // Show notification that scheduled activity is starting
                var phaseText = commitment.ScheduledPhase.ToLower();
                InformationManager.DisplayMessage(new InformationMessage(
                    $"It's {phaseText}. Time for {commitment.Title.ToLower()}.",
                    Colors.Cyan));
            }
            else
            {
                ModLogger.Warn(LogCategory, "EventDeliveryManager not available for scheduled commitment");
            }
        }

        /// <summary>
        /// Commits the player to an opportunity at its scheduled phase.
        /// The opportunity will grey out in the menu and fire automatically at the scheduled time.
        /// </summary>
        public void CommitToOpportunity(CampOpportunity opportunity)
        {
            if (opportunity == null)
            {
                return;
            }

            var scheduledPhase = opportunity.GetEffectiveScheduledPhase();
            var scheduledDay = PlayerCommitments.CalculateScheduledDay(scheduledPhase);
            var hoursUntil = _commitments.GetHoursUntilCommitment(new ScheduledCommitment
            {
                ScheduledPhase = scheduledPhase,
                ScheduledDay = scheduledDay
            });

            // Generate display text
            var phaseText = scheduledPhase.ToLower();
            var displayText = hoursUntil <= 6
                ? $"You've committed to {opportunity.TitleFallback?.ToLower() ?? opportunity.Id} this {phaseText}."
                : $"You've committed to {opportunity.TitleFallback?.ToLower() ?? opportunity.Id} tomorrow at {phaseText}.";

            _commitments.AddCommitment(
                opportunity.Id,
                opportunity.TargetDecisionId,
                opportunity.TitleFallback ?? opportunity.Id,
                scheduledPhase,
                scheduledDay,
                displayText
            );

            // Record in history as engaged
            RecordEngagement(opportunity.Id, opportunity.Type);

            // Clear cache to force menu refresh
            _cachedOpportunities = null;

            ModLogger.Info(LogCategory, $"Committed to {opportunity.Id} for {scheduledPhase} (day {scheduledDay}, ~{hoursUntil:F0}h)");

            // Show confirmation message
            var message = new TextObject("{=enlisted_commitment_scheduled}You've made plans for {ACTIVITY} at {PHASE}.");
            message.SetTextVariable("ACTIVITY", opportunity.TitleFallback ?? opportunity.Id);
            message.SetTextVariable("PHASE", phaseText);
            InformationManager.DisplayMessage(new InformationMessage(message.ToString(), Colors.Cyan));
        }

        /// <summary>
        /// Cancels a commitment with a minor fatigue penalty.
        /// </summary>
        public void CancelCommitment(string opportunityId)
        {
            var commitment = _commitments.GetCommitment(opportunityId);
            if (commitment == null)
            {
                return;
            }

            _commitments.RemoveCommitment(opportunityId);

            // Apply small fatigue penalty for canceling plans (restless from changing plans)
            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment != null)
            {
                // Positive delta = spend fatigue, negative = restore
                enlistment.ModifyFatigue(2);
            }

            // Clear cache to force menu refresh
            _cachedOpportunities = null;

            ModLogger.Info(LogCategory, $"Cancelled commitment: {opportunityId}");

            InformationManager.DisplayMessage(new InformationMessage(
                "{=enlisted_commitment_cancelled}Commitment cancelled. You feel restless from changing plans.",
                Colors.Yellow));
        }

        /// <summary>
        /// Checks if the player is committed to a specific opportunity.
        /// </summary>
        public bool IsCommittedTo(string opportunityId)
        {
            return _commitments.IsCommittedTo(opportunityId);
        }

        /// <summary>
        /// Gets all active commitments for display in the Your Status section.
        /// </summary>
        public IReadOnlyList<ScheduledCommitment> GetActiveCommitments()
        {
            return _commitments.Commitments;
        }

        /// <summary>
        /// Gets the next commitment to fire (closest in time).
        /// </summary>
        public ScheduledCommitment GetNextCommitment()
        {
            return _commitments.GetNextCommitment();
        }

        /// <summary>
        /// Gets hours until a commitment fires.
        /// </summary>
        public float GetHoursUntilCommitment(ScheduledCommitment commitment)
        {
            return _commitments.GetHoursUntilCommitment(commitment);
        }

        public override void SyncData(IDataStore dataStore)
        {
            SaveLoadDiagnostics.SafeSyncData(this, dataStore, () =>
            {
                // Serialize history as dictionaries - use local variables for ref passing
                var lastPresented = _history.LastPresentedHours;
                var timesSeen = _history.TimesSeen;
                var timesEngaged = _history.TimesEngaged;
                var timesIgnored = _history.TimesIgnored;

                dataStore.SyncData("camp_lastPresented", ref lastPresented);
                dataStore.SyncData("camp_timesSeen", ref timesSeen);
                dataStore.SyncData("camp_timesEngaged", ref timesEngaged);
                dataStore.SyncData("camp_timesIgnored", ref timesIgnored);

                // Serialize commitments (new multi-commitment format)
                var commitmentCount = dataStore.IsSaving ? _commitments.Commitments.Count : 0;
                dataStore.SyncData("camp_commitmentCount", ref commitmentCount);

                // Serialize each commitment
                if (dataStore.IsSaving)
                {
                    for (var i = 0; i < _commitments.Commitments.Count; i++)
                    {
                        var c = _commitments.Commitments[i];
                        var oppId = c.OpportunityId ?? "";
                        var decId = c.TargetDecisionId ?? "";
                        var title = c.Title ?? "";
                        var phase = c.ScheduledPhase ?? "";
                        var day = c.ScheduledDay;
                        var commitTime = c.CommitTimeHours;
                        var display = c.DisplayText ?? "";

                        dataStore.SyncData($"camp_commit_{i}_oppId", ref oppId);
                        dataStore.SyncData($"camp_commit_{i}_decId", ref decId);
                        dataStore.SyncData($"camp_commit_{i}_title", ref title);
                        dataStore.SyncData($"camp_commit_{i}_phase", ref phase);
                        dataStore.SyncData($"camp_commit_{i}_day", ref day);
                        dataStore.SyncData($"camp_commit_{i}_time", ref commitTime);
                        dataStore.SyncData($"camp_commit_{i}_display", ref display);
                    }
                }

                // Legacy compatibility: still sync old format for backwards compatibility
                var legacyId = "";
                var legacyTime = 0f;
                var legacyText = "";
                dataStore.SyncData("camp_commitmentId", ref legacyId);
                dataStore.SyncData("camp_commitmentTime", ref legacyTime);
                dataStore.SyncData("camp_commitmentText", ref legacyText);

                // Serialize learning system data (opportunity engagement tracking)
                var oppPresented = dataStore.IsSaving
                    ? PlayerBehaviorTracker.GetOpportunityPresentedForSave()
                    : new Dictionary<string, int>();
                var oppEngaged = dataStore.IsSaving
                    ? PlayerBehaviorTracker.GetOpportunityEngagedForSave()
                    : new Dictionary<string, int>();

                dataStore.SyncData("camp_oppPresented", ref oppPresented);
                dataStore.SyncData("camp_oppEngaged", ref oppEngaged);

                if (dataStore.IsLoading)
                {
                    // Restore history from loaded values
                    _history = new OpportunityHistory
                    {
                        LastPresentedHours = lastPresented ?? new Dictionary<string, float>(),
                        TimesSeen = timesSeen ?? new Dictionary<string, int>(),
                        TimesEngaged = timesEngaged ?? new Dictionary<string, int>(),
                        TimesIgnored = timesIgnored ?? new Dictionary<string, int>()
                    };

                    // Restore commitments
                    _commitments = new PlayerCommitments();

                    // Try to load new format first
                    for (var i = 0; i < commitmentCount; i++)
                    {
                        var oppId = "";
                        var decId = "";
                        var title = "";
                        var phase = "";
                        var day = 0;
                        var commitTime = 0f;
                        var display = "";

                        dataStore.SyncData($"camp_commit_{i}_oppId", ref oppId);
                        dataStore.SyncData($"camp_commit_{i}_decId", ref decId);
                        dataStore.SyncData($"camp_commit_{i}_title", ref title);
                        dataStore.SyncData($"camp_commit_{i}_phase", ref phase);
                        dataStore.SyncData($"camp_commit_{i}_day", ref day);
                        dataStore.SyncData($"camp_commit_{i}_time", ref commitTime);
                        dataStore.SyncData($"camp_commit_{i}_display", ref display);

                        if (!string.IsNullOrEmpty(oppId))
                        {
                            _commitments.Commitments.Add(new ScheduledCommitment
                            {
                                OpportunityId = oppId,
                                TargetDecisionId = decId,
                                Title = title,
                                ScheduledPhase = phase,
                                ScheduledDay = day,
                                CommitTimeHours = commitTime,
                                DisplayText = display
                            });
                        }
                    }

                    // Fall back to legacy format if no new commitments loaded
                    if (_commitments.Commitments.Count == 0 && !string.IsNullOrEmpty(legacyId))
                    {
#pragma warning disable CS0618 // Intentionally using obsolete for migration
                        _commitments.ScheduledActivityId = legacyId;
                        _commitments.ScheduledTimeHours = legacyTime > 0 ? legacyTime : null;
                        _commitments.CommitmentDisplayText = legacyText;
#pragma warning restore CS0618
                    }

                    // Restore learning system data
                    PlayerBehaviorTracker.LoadOpportunityState(oppPresented, oppEngaged);

                    ModLogger.Debug(LogCategory, $"Loaded opportunity history: {_history.TimesSeen.Count} types seen, {_commitments.Commitments.Count} commitments, {oppPresented?.Count ?? 0} learning entries");
                }
            });
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            LoadOpportunityDefinitions();
            InitializeScheduleManager();
        }

        /// <summary>
        /// Initializes the camp schedule manager for routine tracking.
        /// Creates a schedule manager instance if one doesn't exist.
        /// </summary>
        private void InitializeScheduleManager()
        {
            if (CampScheduleManager.Instance == null)
            {
                _ = new CampScheduleManager();
                ModLogger.Info(LogCategory, "CampScheduleManager initialized");
            }
        }

        /// <summary>
        /// Main entry point: generates camp life opportunities for the current context.
        /// Returns 0-3 opportunities based on world state, budget, and player state.
        /// </summary>
        public List<CampOpportunity> GenerateCampLife()
        {
            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment?.IsEnlisted != true)
            {
                return new List<CampOpportunity>();
            }

            // Build context from all layers
            var campContext = AnalyzeCampContext();

            // Check for edge cases that block opportunities entirely
            if (ShouldBlockAllOpportunities(campContext, out string blockReason))
            {
                ModLogger.Debug(LogCategory, $"Opportunities blocked: {blockReason}");
                return new List<CampOpportunity>();
            }

            // Check cache validity
            if (_cachedOpportunities != null && _cachePhase == campContext.DayPhase)
            {
                return _cachedOpportunities;
            }

            // Get world situation
            var worldSituation = ContentOrchestrator.Instance?.GetCurrentWorldSituation()
                ?? WorldStateAnalyzer.AnalyzeSituation();

            // Get player preferences
            var playerPrefs = PlayerBehaviorTracker.GetPreferences();

            // Determine opportunity budget
            int budget = DetermineOpportunityBudget(worldSituation, campContext);
            ModLogger.Debug(LogCategory, $"Opportunity budget: {budget} (phase: {campContext.DayPhase}, activity: {campContext.ActivityLevel})");

            if (budget <= 0)
            {
                _cachedOpportunities = new List<CampOpportunity>();
                _cachePhase = campContext.DayPhase;
                return _cachedOpportunities;
            }

            // Generate candidates
            var candidates = GenerateCandidates(worldSituation, campContext, playerPrefs);
            ModLogger.Debug(LogCategory, $"Generated {candidates.Count} candidates");

            // Score each candidate
            foreach (var candidate in candidates)
            {
                candidate.FitnessScore = CalculateFitness(candidate, worldSituation, campContext, playerPrefs, _history);
            }

            // Select best fitting opportunities
            var selected = SelectTopN(candidates, budget);

            // Record presentations for history and learning system
            foreach (var opp in selected)
            {
                _history.RecordPresented(opp.Id, opp.Type.ToString());
                PlayerBehaviorTracker.RecordOpportunityPresented(opp.Type.ToString());
            }

            // Cache results
            _cachedOpportunities = selected;
            _cachePhase = campContext.DayPhase;

            ModLogger.Info(LogCategory, $"Selected {selected.Count} opportunities: [{string.Join(", ", selected.Select(o => o.Id))}]");
            return selected;
        }

        /// <summary>
        /// Called when day phase changes. Invalidates the opportunity cache and updates the schedule.
        /// </summary>
        public void OnPhaseChanged(DayPhase newPhase)
        {
            _cachedOpportunities = null;
            
            // Update the schedule manager for the new phase
            CampScheduleManager.Instance?.OnPhaseChanged(newPhase);
            
            ModLogger.Debug(LogCategory, $"Cache invalidated for phase change to {newPhase}");
        }

        /// <summary>
        /// Analyzes current camp context from game state.
        /// </summary>
        public CampContext AnalyzeCampContext()
        {
            var enlistment = EnlistmentBehavior.Instance;
            var worldSituation = WorldStateAnalyzer.AnalyzeSituation();
            var simulation = CompanySimulationBehavior.Instance;

            var context = new CampContext
            {
                DayPhase = worldSituation.CurrentDayPhase,
                LordSituation = worldSituation.LordIs,
                ActivityLevel = worldSituation.ExpectedActivity
            };

            // Muster cycle position: calculate from LastMusterDay
            int currentDay = (int)CampaignTime.Now.ToDays;
            int lastMusterDay = enlistment?.LastMusterDay ?? currentDay;
            context.DaysSinceLastMuster = currentDay - lastMusterDay;
            context.IsMusterDay = context.DaysSinceLastMuster >= 12; // Muster every ~12 days

            // Camp mood derived from recent events
            context.CurrentMood = DeriveCampMood(simulation);

            // Player state
            context.PlayerOnDuty = IsPlayerOnDuty();
            context.PlayerFatigue = 12; // Default fatigue - will integrate with fatigue system when available
            context.PlayerGold = Hero.MainHero.Gold;
            context.PlayerInjured = Hero.MainHero.HitPoints < Hero.MainHero.MaxHitPoints * 0.5f;

            // Company state
            var needs = enlistment?.CompanyNeeds;
            context.SupplyLevel = needs?.Supplies ?? 50;

            // Edge case flags
            context.IsNewEnlistmentGrace = enlistment != null && enlistment.DaysServed < 3;
            context.IsOnProbation = enlistment?.IsOnProbation ?? false;
            context.InBaggageWindow = IsBaggageWindowActive(enlistment);

            return context;
        }

        /// <summary>
        /// Checks if opportunities should be completely blocked due to edge cases.
        /// </summary>
        private bool ShouldBlockAllOpportunities(CampContext context, out string reason)
        {
            // New enlistment grace period
            if (context.IsNewEnlistmentGrace)
            {
                reason = "New enlistment grace period (first 3 days)";
                return true;
            }

            // Muster day
            if (context.IsMusterDay)
            {
                reason = "Muster day - structured sequence takes over";
                return true;
            }

            // Player on duty (will show filtered opportunities instead)
            // Actually, we don't block - we filter. So this is not a full block.

            // Critical supply shortage
            if (context.SupplyLevel < 20)
            {
                // Budget reduced, not blocked
                reason = null;
                return false;
            }

            // Lord captured/grace period would be handled by EnlistmentBehavior

            reason = null;
            return false;
        }

        /// <summary>
        /// Determines how many opportunities to show based on context.
        /// </summary>
        private int DetermineOpportunityBudget(WorldSituation world, CampContext camp)
        {
            int budget;

            // Base budget by lord situation and day phase
            budget = (world.LordIs, camp.DayPhase) switch
            {
                // Garrison: high activity, especially mornings and evenings
                (LordSituation.PeacetimeGarrison, DayPhase.Dawn) => 3,
                (LordSituation.PeacetimeGarrison, DayPhase.Midday) => 2,
                (LordSituation.PeacetimeGarrison, DayPhase.Dusk) => 3,
                (LordSituation.PeacetimeGarrison, DayPhase.Night) => 1,

                // Siege: very limited opportunities
                (LordSituation.SiegeAttacking, _) => 1,
                (LordSituation.SiegeDefending, _) => 0,

                // Campaign: moderate, mostly evening
                (LordSituation.WarMarching, DayPhase.Dawn) => 1,
                (LordSituation.WarMarching, DayPhase.Midday) => 0,
                (LordSituation.WarMarching, DayPhase.Dusk) => 2,
                (LordSituation.WarMarching, DayPhase.Night) => 0,

                (LordSituation.WarActiveCampaign, DayPhase.Dusk) => 2,
                (LordSituation.WarActiveCampaign, _) => 1,

                // Default
                _ => 2
            };

            // Modifiers
            if (camp.IsOnProbation)
            {
                budget = Math.Max(0, budget - 1);
            }

            if (camp.SupplyLevel < 30)
            {
                budget = Math.Max(0, budget - 1);
            }

            if (camp.SupplyLevel < 20)
            {
                budget = 1; // Survival mode
            }

            if (camp.PlayerOnDuty)
            {
                // On duty: reduced but not zero - risky opportunities still available
                budget = Math.Max(1, budget / 2);
            }

            return Math.Min(budget, MaxOpportunitiesDefault);
        }

        /// <summary>
        /// Generates candidate opportunities based on current context.
        /// </summary>
        private List<CampOpportunity> GenerateCandidates(WorldSituation world, CampContext camp, PlayerPreferences prefs)
        {
            var candidates = new List<CampOpportunity>();
            var tier = EnlistmentBehavior.Instance?.EnlistmentTier ?? 1;

            foreach (var opp in _opportunityDefinitions)
            {
                // Tier check
                if (tier < opp.MinTier)
                {
                    continue;
                }

                if (opp.MaxTier > 0 && tier > opp.MaxTier)
                {
                    continue;
                }

                // Cooldown check
                if (_history.WasRecentlyShown(opp.Id, opp.CooldownHours))
                {
                    continue;
                }

                // Day phase check
                if (opp.ValidPhases.Count > 0 && !opp.ValidPhases.Contains(camp.DayPhase.ToString()))
                {
                    continue;
                }

                // Sea/land context check
                var enlistment = EnlistmentBehavior.Instance;
                var isAtSea = enlistment?.CurrentLord?.PartyBelongedTo?.IsCurrentlyAtSea ?? false;
                
                if (opp.NotAtSea && isAtSea)
                {
                    continue; // Land-only opportunity but party is at sea
                }
                
                if (opp.AtSea && !isAtSea)
                {
                    continue; // Sea-only opportunity but party is on land
                }

                // Order compatibility check when on duty
                if (camp.PlayerOnDuty)
                {
                    var currentOrderType = GetCurrentOrderType();
                    var compat = opp.GetOrderCompatibility(currentOrderType);
                    if (compat == "blocked")
                    {
                        continue;
                    }
                }

                // Injury filter: no training if injured
                if (camp.PlayerInjured && opp.Type == OpportunityType.Training)
                {
                    continue;
                }

                // Gold filter: no gambling if broke
                if (camp.PlayerGold < 20 && opp.Type == OpportunityType.Economic && opp.Id.Contains("gambl"))
                {
                    continue;
                }

                // Create a copy with instance-specific data
                var candidate = CloneOpportunity(opp);
                candidates.Add(candidate);
            }

            return candidates;
        }

        /// <summary>
        /// Calculates fitness score for an opportunity using all 4 intelligence layers plus schedule awareness.
        /// </summary>
        private float CalculateFitness(CampOpportunity opp, WorldSituation world, CampContext camp, 
            PlayerPreferences prefs, OpportunityHistory history)
        {
            float score = opp.BaseFitness;

            // LAYER 1: World State (Macro)
            score += CalculateWorldStateModifier(opp, world);

            // LAYER 2: Camp Context (Meso)
            score += CalculateCampContextModifier(opp, camp);

            // LAYER 3: Player State (Micro)
            score += CalculatePlayerStateModifier(opp, camp, prefs);

            // LAYER 4: History (Meta)
            score += CalculateHistoryModifier(opp, history);

            // LAYER 5: Schedule Awareness (Routine)
            score = ApplyScheduleBoost(opp, score, camp);

            return Math.Max(0f, Math.Min(100f, score));
        }

        /// <summary>
        /// Applies schedule boost to opportunities that match the current routine schedule.
        /// Opportunities matching scheduled activities get a fitness boost, making them more likely to appear.
        /// </summary>
        private float ApplyScheduleBoost(CampOpportunity opp, float currentScore, CampContext camp)
        {
            var scheduleManager = CampScheduleManager.Instance;
            if (scheduleManager == null)
            {
                return currentScore;
            }

            var currentSchedule = scheduleManager.GetScheduleForPhase(camp.DayPhase);
            if (currentSchedule == null)
            {
                return currentScore;
            }

            // Check if this opportunity matches a scheduled activity category
            if (scheduleManager.IsScheduledCategory(opp.Type, currentSchedule))
            {
                // Apply schedule boost multiplier
                float boostedScore = currentScore * scheduleManager.ScheduleBoostMultiplier;
                
                // Mark the opportunity as scheduled for UI display
                opp.IsScheduled = true;
                
                ModLogger.Debug(LogCategory, 
                    $"Schedule boost applied to {opp.Id}: {currentScore:F1} -> {boostedScore:F1}");
                
                return boostedScore;
            }

            return currentScore;
        }

        private float CalculateWorldStateModifier(CampOpportunity opp, WorldSituation world)
        {
            float mod = 0f;

            // Training fits garrison
            if (opp.Type == OpportunityType.Training && world.LordIs == LordSituation.PeacetimeGarrison)
            {
                mod += 15f;
            }

            // Social is odd during siege
            if (opp.Type == OpportunityType.Social && 
                (world.LordIs == LordSituation.SiegeAttacking || world.LordIs == LordSituation.SiegeDefending))
            {
                mod -= 20f;
            }

            // Recovery valuable during intense periods
            if (opp.Type == OpportunityType.Recovery && world.ExpectedActivity == ActivityLevel.Intense)
            {
                mod += 25f;
            }

            return mod;
        }

        private float CalculateCampContextModifier(CampOpportunity opp, CampContext camp)
        {
            float mod = 0f;

            // Day phase preferences
            if (opp.Type == OpportunityType.Training && camp.DayPhase == DayPhase.Dawn)
            {
                mod += 10f;
            }

            if (opp.Type == OpportunityType.Social && camp.DayPhase == DayPhase.Dusk)
            {
                mod += 15f;
            }

            if (opp.Type == OpportunityType.Economic && camp.DayPhase == DayPhase.Night)
            {
                mod -= 30f;
            }

            // Camp mood effects
            if (camp.CurrentMood == CampMood.Celebration && opp.Type == OpportunityType.Social)
            {
                mod += 20f;
            }

            if (camp.CurrentMood == CampMood.Mourning && opp.Type == OpportunityType.Social)
            {
                mod -= 15f;
            }

            if (camp.CurrentMood == CampMood.Tense && opp.Type == OpportunityType.Recovery)
            {
                mod += 10f;
            }

            // Weekly rhythm: near muster = economic focus
            if (camp.DaysSinceLastMuster >= 9 && opp.Type == OpportunityType.Economic)
            {
                mod += 10f;
            }

            return mod;
        }

        private float CalculatePlayerStateModifier(CampOpportunity opp, CampContext camp, PlayerPreferences prefs)
        {
            float mod = 0f;

            // Fatigue affects training
            if (opp.Type == OpportunityType.Training && camp.PlayerFatigue < 5)
            {
                mod -= 25f;
            }

            // Recovery when injured
            if (opp.Type == OpportunityType.Recovery && camp.PlayerInjured)
            {
                mod += 30f;
            }

            // Economic when poor
            if (opp.Type == OpportunityType.Economic && camp.PlayerGold < 50)
            {
                mod += 20f;
            }

            // Player preference bonus (learned over time)
            if (opp.Type == OpportunityType.Training && prefs.CombatVsSocial > 0.6f)
            {
                mod += 10f;
            }

            if (opp.Type == OpportunityType.Social && prefs.CombatVsSocial < 0.4f)
            {
                mod += 10f;
            }

            return mod;
        }

        private float CalculateHistoryModifier(CampOpportunity opp, OpportunityHistory history)
        {
            float mod = 0f;
            string typeKey = opp.Type.ToString();

            // Don't repeat too soon
            float hoursSince = history.HoursSincePresented(typeKey);
            if (hoursSince < 12f)
            {
                mod -= 40f;
            }
            else if (hoursSince < 24f)
            {
                mod -= 20f;
            }

            // Learning system: apply learned preference modifier with 70/30 split
            // 70% learned preference, 30% variety (built into GetLearningModifier)
            float learningMod = PlayerBehaviorTracker.GetLearningModifier(typeKey);
            mod += learningMod;

            if (Math.Abs(learningMod) > 0.1f)
            {
                ModLogger.Debug(LogCategory, $"Learning modifier for {typeKey}: {learningMod:+0.0;-0.0;0}");
            }

            // Novelty bonus for unseen types (maintains the 30% variety)
            if (!history.TimesSeen.ContainsKey(typeKey))
            {
                mod += 8f; // Slight bonus to encourage trying new types
            }

            // If a type has been ignored many times, occasionally still show it (variety rule)
            // This prevents the system from completely hiding ignored types
            float engagementRate = PlayerBehaviorTracker.GetOpportunityEngagementRate(typeKey);
            if (engagementRate < 0.3f)
            {
                // Apply random variety chance: 30% of the time, reduce the penalty
                float currentHour = (float)CampaignTime.Now.ToHours;
                int variationSeed = (int)(currentHour / 6) + opp.Id.GetHashCode();
                if (variationSeed % 10 < 3) // 30% variety window
                {
                    mod += 5f; // Reduce penalty slightly to maintain variety
                }
            }

            return mod;
        }

        /// <summary>
        /// Selects the top N opportunities by fitness score.
        /// </summary>
        private List<CampOpportunity> SelectTopN(List<CampOpportunity> candidates, int n)
        {
            return candidates
                .Where(c => c.FitnessScore >= FitnessThreshold)
                .OrderByDescending(c => c.FitnessScore)
                .Take(n)
                .ToList();
        }

        /// <summary>
        /// Records that the player engaged with an opportunity.
        /// </summary>
        public void RecordEngagement(string opportunityId, OpportunityType type)
        {
            string typeKey = type.ToString();
            _history.RecordEngaged(opportunityId, typeKey);

            // Track for learning system
            PlayerBehaviorTracker.RecordOpportunityEngagement(typeKey, true);
            PlayerBehaviorTracker.RecordChoice(typeKey.ToLower());

            ModLogger.Debug(LogCategory, $"Recorded engagement: {opportunityId} ({typeKey})");
        }

        /// <summary>
        /// Records that the player ignored an opportunity (menu closed without engaging).
        /// </summary>
        public void RecordIgnored(string opportunityId, OpportunityType type)
        {
            string typeKey = type.ToString();
            _history.RecordIgnored(opportunityId, typeKey);

            // Track for learning system (presented but not engaged)
            PlayerBehaviorTracker.RecordOpportunityEngagement(typeKey, false);

            ModLogger.Debug(LogCategory, $"Recorded ignored: {opportunityId} ({typeKey})");
        }

        /// <summary>
        /// Performs detection check for risky opportunities while player is on duty.
        /// Returns true if player gets away with it, false if caught.
        /// Applies consequences automatically if caught.
        /// </summary>
        public bool AttemptRiskyOpportunity(CampOpportunity opportunity)
        {
            if (opportunity?.Detection == null)
            {
                return true; // No detection settings = always succeeds
            }

            var context = AnalyzeCampContext();
            if (!context.PlayerOnDuty)
            {
                return true; // Not on duty = no risk
            }

            // Calculate detection chance
            float chance = opportunity.Detection.BaseChance;

            // Night modifier (harder to detect at night)
            if (context.DayPhase == DayPhase.Night)
            {
                chance += opportunity.Detection.NightModifier;
            }

            // High reputation modifier (trusted soldiers get away with more)
            var reputation = EscalationManager.Instance?.State?.OfficerReputation ?? 50;
            if (reputation > 70)
            {
                chance += opportunity.Detection.HighRepModifier;
            }

            // Clamp to valid range
            chance = Math.Max(0f, Math.Min(1f, chance));

            ModLogger.Debug(LogCategory, $"Detection check for {opportunity.Id}: {chance * 100:F0}% chance");

            // Roll for detection
            bool caught = MBRandom.RandomFloat < chance;

            if (caught)
            {
                ApplyCaughtConsequences(opportunity);
                ModLogger.Info(LogCategory, $"Player caught doing {opportunity.Id} while on duty!");
                return false;
            }

            ModLogger.Debug(LogCategory, $"Player got away with {opportunity.Id}");
            return true;
        }

        /// <summary>
        /// Applies consequences when player is caught doing risky activity while on duty.
        /// </summary>
        private void ApplyCaughtConsequences(CampOpportunity opportunity)
        {
            var consequences = opportunity.CaughtConsequences;
            if (consequences == null)
            {
                return;
            }

            var escalation = EscalationManager.Instance;
            if (escalation != null)
            {
                // Apply officer reputation penalty
                if (consequences.OfficerRep != 0)
                {
                    escalation.ModifyOfficerReputation(consequences.OfficerRep, "Caught away from post");
                    ModLogger.Debug(LogCategory, $"Officer rep penalty: {consequences.OfficerRep}");
                }

                // Apply discipline increase
                if (consequences.Discipline != 0)
                {
                    escalation.ModifyDiscipline(consequences.Discipline, "Absent from duty");
                    ModLogger.Debug(LogCategory, $"Discipline increase: {consequences.Discipline}");
                }
            }

            // Check for order failure - for now just add extra discipline penalty
            // Future: integrate with order progression system when order completion tracking is added
            if (consequences.OrderFailureRisk > 0)
            {
                bool orderImpacted = MBRandom.RandomFloat < consequences.OrderFailureRisk;
                if (orderImpacted && escalation != null)
                {
                    var currentOrder = OrderManager.Instance?.GetCurrentOrder();
                    if (currentOrder != null)
                    {
                        ModLogger.Info(LogCategory, $"Order {currentOrder.Id} impacted by absence - extra discipline penalty");
                        // Add extra discipline as consequence for potentially failing the order
                        escalation.ModifyDiscipline(1, "Order compromised by absence");
                    }
                }
            }

            // Show notification to player
            var title = new TextObject("{=opp_caught_title}Dereliction of Duty");
            var message = new TextObject("{=opp_caught_notification}Your absence was noticed. The {NCO_TITLE} will hear of this.");

            // Set NCO title based on culture
            var cultureId = EnlistmentBehavior.Instance?.EnlistedLord?.Culture?.StringId ?? "empire";
            var ncoTitle = RankHelper.GetNCOTitle(cultureId);
            message.SetTextVariable("NCO_TITLE", ncoTitle);

            InformationManager.ShowInquiry(
                new InquiryData(
                    title.ToString(),
                    message.ToString(),
                    isAffirmativeOptionShown: true,
                    isNegativeOptionShown: false,
                    affirmativeText: new TextObject("{=str_understood}Understood").ToString(),
                    negativeText: null,
                    affirmativeAction: null,
                    negativeAction: null
                ), true);
        }

        /// <summary>
        /// Commits the player to a scheduled activity (legacy method).
        /// Use CommitToOpportunity instead for the new scheduling system.
        /// </summary>
        [Obsolete("Use CommitToOpportunity instead for the new scheduling system")]
        public void CommitToActivity(string activityId, float hoursFromNow, string displayText)
        {
#pragma warning disable CS0618 // Intentional: legacy compatibility method
            _commitments.CommitTo(activityId, hoursFromNow, displayText);
#pragma warning restore CS0618
            ModLogger.Info(LogCategory, $"Player committed to {activityId} in {hoursFromNow:F1} hours");
        }

        /// <summary>
        /// Gets contextually appropriate empty state message.
        /// </summary>
        public string GetEmptyStateMessage(CampContext context)
        {
            if (context.PlayerOnDuty)
            {
                return new TextObject("{=camp_empty_duty}You're on duty.").ToString();
            }

            if (context.LordSituation == LordSituation.WarMarching)
            {
                return new TextObject("{=camp_empty_march}The army is on the march. No time for leisure.").ToString();
            }

            if (context.LordSituation == LordSituation.SiegeAttacking || context.LordSituation == LordSituation.SiegeDefending)
            {
                return new TextObject("{=camp_empty_siege}The siege consumes all attention.").ToString();
            }

            if (context.IsNewEnlistmentGrace)
            {
                return new TextObject("{=camp_empty_new}You're still finding your place. The camp will open up once you've settled in.").ToString();
            }

            return new TextObject("{=camp_empty_quiet}A quiet moment in camp. Rest while you can.").ToString();
        }

        #region Helper Methods

        private CampMood DeriveCampMood(CompanySimulationBehavior simulation)
        {
            if (simulation == null)
            {
                return CampMood.Routine;
            }

            var needs = EnlistmentBehavior.Instance?.CompanyNeeds;

            // Recent deaths = mourning
            if (simulation.Pressure.RecentDesertions > 2 || simulation.Roster?.CasualtyRate > 0.15f)
            {
                return CampMood.Mourning;
            }

            // Low morale or supplies = tense
            if (needs != null && (needs.Morale < 30 || needs.Supplies < 30))
            {
                return CampMood.Tense;
            }

            // High morale = celebration
            if (needs != null && needs.Morale > 70)
            {
                return CampMood.Celebration;
            }

            return CampMood.Routine;
        }

        private bool IsPlayerOnDuty()
        {
            // TODO: Check OrderProgressionBehavior for active order
            // For now, return false - will be integrated with Order system
            return false;
        }

        private string GetCurrentOrderType()
        {
            // TODO: Get from OrderProgressionBehavior
            return "";
        }

        private bool IsBaggageWindowActive(EnlistmentBehavior enlistment)
        {
            if (enlistment == null)
            {
                return false;
            }

            // Baggage window is 6 hours after muster
            // TODO: Integrate with muster timing
            return false;
        }

        private CampOpportunity CloneOpportunity(CampOpportunity source)
        {
            return new CampOpportunity
            {
                Id = source.Id,
                Type = source.Type,
                TitleId = source.TitleId,
                TitleFallback = source.TitleFallback,
                DescriptionId = source.DescriptionId,
                DescriptionFallback = source.DescriptionFallback,
                ActionId = source.ActionId,
                ActionFallback = source.ActionFallback,
                TargetDecisionId = source.TargetDecisionId,
                MinTier = source.MinTier,
                MaxTier = source.MaxTier,
                CooldownHours = source.CooldownHours,
                BaseFitness = source.BaseFitness,
                ValidPhases = source.ValidPhases,
                OrderCompatibility = source.OrderCompatibility,
                Detection = source.Detection,
                CaughtConsequences = source.CaughtConsequences,
                TooltipRiskyId = source.TooltipRiskyId,
                TooltipRiskyFallback = source.TooltipRiskyFallback,
                RequiredFlags = source.RequiredFlags,
                BlockedByFlags = source.BlockedByFlags,
                ScheduledTime = source.ScheduledTime
            };
        }

        #endregion

        #region JSON Loading

        private void LoadOpportunityDefinitions()
        {
            try
            {
                var configPath = Path.Combine(BasePath.Name, "Modules", "Enlisted", "ModuleData", "Enlisted", "Decisions", "camp_opportunities.json");
                if (!File.Exists(configPath))
                {
                    ModLogger.Warn(LogCategory, "camp_opportunities.json not found, using empty definitions");
                    _opportunityDefinitions = new List<CampOpportunity>();
                    return;
                }

                var json = File.ReadAllText(configPath);
                var jObject = JObject.Parse(json);
                var opportunities = jObject["opportunities"] as JArray;

                if (opportunities == null)
                {
                    ModLogger.Warn(LogCategory, "No 'opportunities' array found in camp_opportunities.json");
                    return;
                }

                _opportunityDefinitions = new List<CampOpportunity>();

                foreach (var item in opportunities)
                {
                    var opp = ParseOpportunity(item);
                    if (opp != null)
                    {
                        _opportunityDefinitions.Add(opp);
                    }
                }

                ModLogger.Info(LogCategory, $"Loaded {_opportunityDefinitions.Count} opportunity definitions");
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, "Failed to load camp_opportunities.json", ex);
                _opportunityDefinitions = new List<CampOpportunity>();
            }
        }

        private CampOpportunity ParseOpportunity(JToken item)
        {
            try
            {
                var opp = new CampOpportunity
                {
                    Id = item["id"]?.Value<string>(),
                    Type = ParseOpportunityType(item["type"]?.Value<string>()),
                    TitleId = item["titleId"]?.Value<string>(),
                    TitleFallback = item["title"]?.Value<string>(),
                    DescriptionId = item["descriptionId"]?.Value<string>(),
                    DescriptionFallback = item["description"]?.Value<string>(),
                    ActionId = item["actionId"]?.Value<string>(),
                    ActionFallback = item["action"]?.Value<string>(),
                    TargetDecisionId = item["targetDecision"]?.Value<string>(),
                    MinTier = item["minTier"]?.Value<int>() ?? 1,
                    MaxTier = item["maxTier"]?.Value<int>() ?? 0,
                    CooldownHours = item["cooldownHours"]?.Value<int>() ?? 12,
                    BaseFitness = item["baseFitness"]?.Value<int>() ?? 50,
                    ScheduledTime = item["scheduledTime"]?.Value<string>(),
                    NotAtSea = item["notAtSea"]?.Value<bool>() ?? false,
                    AtSea = item["atSea"]?.Value<bool>() ?? false
                };

                // Valid phases
                var phases = item["validPhases"] as JArray;
                if (phases != null)
                {
                    opp.ValidPhases = phases.Select(p => p.Value<string>()).ToList();
                }

                // Order compatibility
                var orderCompat = item["orderCompatibility"] as JObject;
                if (orderCompat != null)
                {
                    opp.OrderCompatibility = new Dictionary<string, string>();
                    foreach (var prop in orderCompat.Properties())
                    {
                        opp.OrderCompatibility[prop.Name] = prop.Value.Value<string>();
                    }
                }

                // Detection settings
                var detection = item["detection"] as JObject;
                if (detection != null)
                {
                    opp.Detection = new DetectionSettings
                    {
                        BaseChance = detection["baseChance"]?.Value<float>() ?? 0.25f,
                        NightModifier = detection["nightModifier"]?.Value<float>() ?? -0.15f,
                        HighRepModifier = detection["highRepModifier"]?.Value<float>() ?? -0.10f
                    };
                }

                // Caught consequences
                var caught = item["caughtConsequences"] as JObject;
                if (caught != null)
                {
                    opp.CaughtConsequences = new CaughtConsequences
                    {
                        OfficerRep = caught["officerRep"]?.Value<int>() ?? -15,
                        Discipline = caught["discipline"]?.Value<int>() ?? 2,
                        OrderFailureRisk = caught["orderFailureRisk"]?.Value<float>() ?? 0.20f
                    };
                }

                opp.TooltipRiskyId = item["tooltipRiskyId"]?.Value<string>();
                opp.TooltipRiskyFallback = item["tooltipRisky"]?.Value<string>();

                // Required/blocked flags
                var requiredFlags = item["requiredFlags"] as JArray;
                if (requiredFlags != null)
                {
                    opp.RequiredFlags = requiredFlags.Select(f => f.Value<string>()).ToList();
                }

                var blockedFlags = item["blockedByFlags"] as JArray;
                if (blockedFlags != null)
                {
                    opp.BlockedByFlags = blockedFlags.Select(f => f.Value<string>()).ToList();
                }

                if (string.IsNullOrEmpty(opp.Id))
                {
                    return null;
                }

                return opp;
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, $"Failed to parse opportunity: {ex.Message}");
                return null;
            }
        }

        private OpportunityType ParseOpportunityType(string type)
        {
            return type?.ToLower() switch
            {
                "training" => OpportunityType.Training,
                "social" => OpportunityType.Social,
                "economic" => OpportunityType.Economic,
                "recovery" => OpportunityType.Recovery,
                "special" => OpportunityType.Special,
                _ => OpportunityType.Social
            };
        }

        #endregion
    }
}
