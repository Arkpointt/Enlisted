using System;
using System.Collections.Generic;
using System.Linq;
using EnlistedConfig = Enlisted.Features.Assignments.Core.ConfigurationManager;
using Enlisted.Features.Activities;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Features.Interface.Behaviors;
using Enlisted.Features.Lances.Events;
using Enlisted.Features.Schedule.Models;
using Enlisted.Mod.Core.Logging;
using Enlisted.Mod.Core.Triggers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace Enlisted.Features.Lances.Events.Decisions
{
    /// <summary>
    /// Decision Events behavior: CK3-style event delivery system.
    /// Evaluates eligible decision events on specific hours, respects pacing limits, and fires events through the
    /// Lance Life Events infrastructure. This behavior handles the "pushed" automatic decision events.
    /// Player-initiated decisions are surfaced through the main menu.
    /// </summary>
    public sealed class DecisionEventBehavior : CampaignBehaviorBase
    {
        private const string LogCategory = "DecisionEvents";
        private const int FreeTimeQueueTimeoutHours = 48;

        public static DecisionEventBehavior Instance { get; private set; }

        private readonly DecisionEventEvaluator _evaluator = new DecisionEventEvaluator();

        // Persisted state (cooldowns, flags, counters)
        private DecisionEventState _state = new DecisionEventState();

        /// <summary>
        /// Public accessor for decision event state (used by menu tooltips).
        /// </summary>
        public DecisionEventState State => _state;

        // Cached config (reloaded periodically)
        private DecisionEventConfig _config;
        private int _lastConfigLoadHour = -1;

        // Queued event for firing at safe moment
        private string _queuedEventId = string.Empty;
        private int _queuedAtHour = -1;

        // Tracking
        private int _lastEvaluationHour = -1;
        private bool _initialized;

        // Situation flag refresh gate.
        private int _lastSituationUpdateHourNumber = -1;

        public override void RegisterEvents()
        {
            Instance = this;

            CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, OnHourlyTick);
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
        }

        public override void SyncData(IDataStore dataStore)
        {
            SaveLoadDiagnostics.SafeSyncData(this, dataStore, () =>
            {
                // Sync the state object
                _state ??= new DecisionEventState();
                _state.SyncData(dataStore);

                // Sync queue state
                dataStore.SyncData("de_queuedEventId", ref _queuedEventId);
                dataStore.SyncData("de_queuedAtHour", ref _queuedAtHour);
                dataStore.SyncData("de_lastEvalHour", ref _lastEvaluationHour);
                dataStore.SyncData("de_initialized", ref _initialized);

                if (dataStore.IsLoading)
                {
                    _queuedEventId ??= string.Empty;
                    if (_queuedAtHour < -1)
                    {
                        _queuedAtHour = -1;
                    }
                }
            });
        }

        /// <summary>
        /// Gets available player-initiated decisions for the Main Menu.
        /// </summary>
        public List<LanceLifeEventDefinition> GetAvailablePlayerDecisions()
        {
            var config = GetConfig();
            if (config == null || !config.Enabled || !config.Menu.Enabled)
            {
                return new List<LanceLifeEventDefinition>();
            }

            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment?.IsEnlisted != true)
            {
                return new List<LanceLifeEventDefinition>();
            }

            // Keep situation flags fresh before evaluating availability.
            UpdateSituationFlagsIfNeeded(enlistment);

            var catalog = LanceLifeEventRuntime.GetCatalog();
            var allEvents = catalog?.Events ?? new List<LanceLifeEventDefinition>();

            return _evaluator.GetAvailablePlayerDecisions(allEvents, _state, config, enlistment);
        }

        public IReadOnlyList<QueuedFreeTimeDecision> GetQueuedFreeTimeDecisions()
        {
            return _state?.GetQueuedFreeTimeDecisions() ?? new List<QueuedFreeTimeDecision>();
        }

        public bool TryQueueFreeTimeDecision(
            FreeTimeDecisionKind kind,
            string id,
            FreeTimeDecisionWindow window,
            int desiredFatigueCost,
            out TextObject resultText)
        {
            resultText = new TextObject(string.Empty);

            try
            {
                var enlistment = EnlistmentBehavior.Instance;
                if (enlistment?.IsEnlisted != true)
                {
                    resultText = new TextObject("{=enlisted_decision_not_enlisted}You are not currently enlisted.");
                    return false;
                }

                if (string.IsNullOrWhiteSpace(id))
                {
                    resultText = new TextObject("{=enlisted_decision_invalid}Invalid decision.");
                    return false;
                }

                desiredFatigueCost = Math.Max(0, desiredFatigueCost);
                if (desiredFatigueCost > 0 && enlistment.FatigueCurrent < desiredFatigueCost)
                {
                    resultText = new TextObject("{=enlisted_decision_too_tired}You are too exhausted for that right now.");
                    return false;
                }

                var queuedAtHour = GetHourNumber();
                var earliestHour = ComputeEarliestHourForWindow(window, queuedAtHour);

                var record = new QueuedFreeTimeDecision
                {
                    Kind = kind,
                    Window = window,
                    Id = id,
                    DesiredFatigueCost = desiredFatigueCost,
                    EarliestHourNumber = earliestHour,
                    QueuedAtHourNumber = queuedAtHour
                };

                if (_state?.TryQueueFreeTimeDecision(record) != true)
                {
                    resultText = new TextObject("{=enlisted_decision_already_queued}That is already queued.");
                    return false;
                }

                resultText = new TextObject("{=enlisted_decision_queued}Queued.");
                return true;
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, "Failed to queue Free Time decision", ex);
                resultText = new TextObject("{=enlisted_decision_queue_error}Failed to queue decision.");
                return false;
            }
        }

        public bool TryCancelQueuedFreeTimeDecision(string id, out TextObject resultText)
        {
            resultText = new TextObject(string.Empty);

            try
            {
                if (_state?.TryRemoveQueuedFreeTimeDecision(id) == true)
                {
                    resultText = new TextObject("{=enlisted_decision_cancelled}Cancelled.");
                    return true;
                }

                resultText = new TextObject("{=enlisted_decision_cancel_failed}Nothing to cancel.");
                return false;
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, "Failed to cancel queued Free Time decision", ex);
                resultText = new TextObject("{=enlisted_decision_cancel_error}Failed to cancel queued decision.");
                return false;
            }
        }

        /// <summary>
        /// Fires a player-initiated decision. Called from Main Menu.
        /// </summary>
        public bool FirePlayerDecision(string eventId)
        {
            return FirePlayerDecision(eventId, onEventClosed: null);
        }

        /// <summary>
        /// Fires a player-initiated decision with an optional callback when the event screen closes.
        /// </summary>
        public bool FirePlayerDecision(string eventId, System.Action onEventClosed)
        {
            var config = GetConfig();
            if (config == null || !config.Enabled)
            {
                return false;
            }

            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment?.IsEnlisted != true)
            {
                return false;
            }

            var catalog = LanceLifeEventRuntime.GetCatalog();
            var evt = catalog?.Events?.FirstOrDefault(e => e.Id == eventId);

            if (evt == null)
            {
                ModLogger.Warn(LogCategory, $"Player decision not found: {eventId}");
                return false;
            }

            // Fire through existing infrastructure
            return FireEvent(evt, enlistment, onEventClosed);
        }

        /// <summary>
        /// Sets a story flag with optional expiry.
        /// </summary>
        public void SetFlag(string flag, float expiryDays = 0f)
        {
            _state.SetFlag(flag, expiryDays);
            ModLogger.Debug(LogCategory, $"Flag set: {flag} (expires in {expiryDays} days)");
        }

        /// <summary>
        /// Clears a story flag.
        /// </summary>
        public void ClearFlag(string flag)
        {
            _state.ClearFlag(flag);
            ModLogger.Debug(LogCategory, $"Flag cleared: {flag}");
        }

        /// <summary>
        /// Queues a chain event to fire after a delay.
        /// </summary>
        public void QueueChainEvent(string eventId, float delayHours = 0f)
        {
            _state.QueueChainEvent(eventId, delayHours);
            ModLogger.Debug(LogCategory, $"Chain event queued: {eventId} (delay {delayHours}h)");
        }

        /// <summary>
        /// Resets per-term counters. Called when player re-enlists.
        /// </summary>
        public void OnPlayerReenlisted()
        {
            _state.ResetTermCounters();
            ModLogger.Debug(LogCategory, "Per-term counters reset for new enlistment");
        }

        private void OnHourlyTick()
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

                // Update situation flags for decision availability (derived from Daily Report snapshot + live state).
                UpdateSituationFlagsIfNeeded(enlistment);

                // Try to fire queued event first
                if (!string.IsNullOrEmpty(_queuedEventId))
                {
                    TryFireQueued(enlistment);
                    return;
                }

                // Try to execute one queued Free Time decision when its window is reached.
                if (TryFireQueuedFreeTimeDecision(enlistment))
                {
                    return;
                }

                // Check if this is an evaluation hour
                var currentHour = (int)CampaignTime.Now.CurrentHourInDay;
                var config = GetConfig();

                if (!IsEvaluationHour(currentHour, config))
                {
                    return;
                }

                // Don't re-evaluate same hour
                var hourNumber = GetHourNumber();
                if (_lastEvaluationHour == hourNumber)
                {
                    return;
                }

                _lastEvaluationHour = hourNumber;

                // Try to select and queue an event
                TryEvaluateAndQueue(enlistment, config);

                // If we queued something and it's safe to fire, fire now
                if (!string.IsNullOrEmpty(_queuedEventId))
                {
                    TryFireQueued(enlistment);
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, "Decision Events hourly tick failed", ex);
            }
        }

        private bool TryFireQueuedFreeTimeDecision(EnlistmentBehavior enlistment)
        {
            var queue = _state?.GetQueuedFreeTimeDecisions();
            if (queue == null || queue.Count == 0)
            {
                return false;
            }

            var nowHour = GetHourNumber();
            var currentBlock = CampaignTriggerTrackerBehavior.Instance?.GetTimeBlock() ?? TimeBlock.Morning;

            // Drop timed-out entries (keep queue lean and predictable).
            var changed = false;
            var normalized = queue.ToList();
            for (var i = normalized.Count - 1; i >= 0; i--)
            {
                var age = nowHour - normalized[i].QueuedAtHourNumber;
                if (age > FreeTimeQueueTimeoutHours)
                {
                    ModLogger.Warn(LogCategory, $"Dropping queued Free Time decision due to timeout: {normalized[i].Id}");
                    normalized.RemoveAt(i);
                    changed = true;
                }
            }

            if (changed)
            {
                _state.ReplaceQueuedFreeTimeDecisions(normalized);
            }

            // Find the first eligible entry.
            var next = normalized.FirstOrDefault(d =>
                d != null &&
                nowHour >= d.EarliestHourNumber &&
                IsTimeBlockAllowed(currentBlock, d.Window));

            if (next == null)
            {
                return false;
            }

            // For event-backed decisions, use the same safety rules as automatic events.
            // For training actions, we also respect these to avoid executing while the game is in a sensitive state.
            if (!IsSafeToShowEvent())
            {
                return false;
            }

            var executed = false;
            if (next.Kind == FreeTimeDecisionKind.TrainingAction)
            {
                executed = ExecuteTrainingAction(next, enlistment);
            }
            else
            {
                executed = ExecuteQueuedEventDecision(next, enlistment);
            }

            if (!executed)
            {
                return false;
            }

            // Remove from queue.
            normalized.RemoveAll(d => d != null && string.Equals(d.Id, next.Id, StringComparison.OrdinalIgnoreCase));
            _state.ReplaceQueuedFreeTimeDecisions(normalized);
            return true;
        }

        private bool ExecuteQueuedEventDecision(QueuedFreeTimeDecision queued, EnlistmentBehavior enlistment)
        {
            try
            {
                var catalog = LanceLifeEventRuntime.GetCatalog();
                var evt = catalog?.Events?.FirstOrDefault(e => e.Id == queued.Id);
                if (evt == null)
                {
                    ModLogger.Warn(LogCategory, $"Queued Free Time event not found in catalog: {queued.Id}");
                    return true; // treat as consumed; it will be removed by caller
                }

                // Ensure the action is "expensive" as a free-time spend: we top up fatigue cost to at least DesiredFatigueCost.
                var minFatigue = GetMinFatigueCost(evt);
                var additional = Math.Max(0, queued.DesiredFatigueCost - minFatigue);
                if (additional > 0 && !enlistment.TryConsumeFatigue(additional, $"free_time:{queued.Id}"))
                {
                    InformationManager.DisplayMessage(new InformationMessage(
                        new TextObject("{=enlisted_decision_cancel_exhausted}You are too exhausted. Your queued decision is cancelled.").ToString()));
                    return true; // treat as consumed; it will be removed by caller
                }

                return FireEvent(evt, enlistment);
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, $"Failed to execute queued Free Time event: {queued?.Id}", ex);
                return false;
            }
        }

        private bool ExecuteTrainingAction(QueuedFreeTimeDecision queued, EnlistmentBehavior enlistment)
        {
            try
            {
                var hero = Hero.MainHero;
                if (hero == null)
                {
                    return true;
                }

                if (queued.DesiredFatigueCost > 0 && !enlistment.TryConsumeFatigue(queued.DesiredFatigueCost, $"training:{queued.Id}"))
                {
                    InformationManager.DisplayMessage(new InformationMessage(
                        new TextObject("{=enlisted_decision_cancel_exhausted}You are too exhausted. Your queued decision is cancelled.").ToString()));
                    return true; // treat as consumed; it will be removed by caller
                }

                var tier = enlistment.EnlistmentTier <= 0 ? 1 : enlistment.EnlistmentTier;

                switch (queued.Id)
                {
                    case "ft_training_formation":
                        GrantTrainingXp(hero, DefaultSkills.Athletics, tier, 1.0f);
                        if (HasMountEquipped(hero))
                        {
                            GrantTrainingXp(hero, DefaultSkills.Riding, tier, 0.8f);
                        }
                        InformationManager.DisplayMessage(new InformationMessage(
                            new TextObject("{=enlisted_training_done_formation}You complete a formation drill.").ToString()));
                        return true;

                    case "ft_training_combat":
                        var weaponSkill = TryGetPrimaryWeaponSkill(hero) ?? DefaultSkills.OneHanded;
                        GrantTrainingXp(hero, weaponSkill, tier, 1.0f);
                        GrantTrainingXp(hero, DefaultSkills.Athletics, tier, 0.4f);
                        InformationManager.DisplayMessage(new InformationMessage(
                            new TextObject("{=enlisted_training_done_combat}You complete a combat drill.").ToString()));
                        return true;

                    case "ft_training_specialist":
                        // Placeholder "specialist" training until we have duty-role mapping.
                        // We keep this useful by awarding Leadership (drill discipline) plus a small Athletics component.
                        GrantTrainingXp(hero, DefaultSkills.Leadership, tier, 0.8f);
                        GrantTrainingXp(hero, DefaultSkills.Athletics, tier, 0.3f);
                        InformationManager.DisplayMessage(new InformationMessage(
                            new TextObject("{=enlisted_training_done_specialist}You complete specialist practice.").ToString()));
                        return true;
                }

                // Unknown action id: treat as consumed so it doesn't get stuck.
                ModLogger.Warn(LogCategory, $"Unknown training action id in queue: {queued.Id}");
                return true;
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, $"Failed to execute training action: {queued?.Id}", ex);
                return false;
            }
        }

        private static int GetMinFatigueCost(LanceLifeEventDefinition evt)
        {
            try
            {
                var min = 0;
                var first = true;
                foreach (var opt in evt?.Options ?? new List<LanceLifeEventOptionDefinition>())
                {
                    var fat = opt?.Costs?.Fatigue ?? 0;
                    if (first)
                    {
                        min = fat;
                        first = false;
                        continue;
                    }
                    min = Math.Min(min, fat);
                }
                return Math.Max(0, min);
            }
            catch
            {
                return 0;
            }
        }

        private static void GrantTrainingXp(Hero hero, SkillObject skill, int tier, float multiplier)
        {
            try
            {
                if (hero?.HeroDeveloper == null || skill == null)
                {
                    return;
                }

                multiplier = MathF.Clamp(multiplier, 0f, 5f);
                if (multiplier <= 0f)
                {
                    return;
                }

                var raw = TrainingXpScaler.CalculateRawTrainingXp(hero, skill, tier) * multiplier;
                if (raw <= 0f)
                {
                    return;
                }

                hero.HeroDeveloper.AddSkillXp(skill, raw, isAffectedByFocusFactor: true, shouldNotify: true);
            }
            catch
            {
                // Intentionally swallow: training XP should never hard-fail a campaign tick.
            }
        }

        private static bool HasMountEquipped(Hero hero)
        {
            try
            {
                var horse = hero?.BattleEquipment[EquipmentIndex.Horse].Item;
                return horse != null;
            }
            catch
            {
                return false;
            }
        }

        private static SkillObject TryGetPrimaryWeaponSkill(Hero hero)
        {
            try
            {
                if (hero == null || Campaign.Current?.Models?.CombatXpModel == null)
                {
                    return null;
                }

                var equipment = hero.BattleEquipment;
                var weaponSlots = new[] { EquipmentIndex.Weapon0, EquipmentIndex.Weapon1, EquipmentIndex.Weapon2, EquipmentIndex.Weapon3 };

                foreach (var slot in weaponSlots)
                {
                    var item = equipment[slot].Item;
                    if (item?.PrimaryWeapon == null)
                    {
                        continue;
                    }

                    var weapon = item.GetWeaponWithUsageIndex(0);
                    if (weapon == null)
                    {
                        continue;
                    }

                    return Campaign.Current.Models.CombatXpModel.GetSkillForWeapon(weapon, isSiegeEngineHit: false);
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        private static bool IsTimeBlockAllowed(TimeBlock current, FreeTimeDecisionWindow window)
        {
            switch (window)
            {
                case FreeTimeDecisionWindow.Any:
                    return true;
                case FreeTimeDecisionWindow.Training:
                    return current == TimeBlock.Morning || current == TimeBlock.Afternoon;
                case FreeTimeDecisionWindow.Social:
                    return current == TimeBlock.Dusk || current == TimeBlock.Night;
                default:
                    return false;
            }
        }

        private static int ComputeEarliestHourForWindow(FreeTimeDecisionWindow window, int currentHourNumber)
        {
            // We schedule to "now" if currently inside the window; otherwise to the next window start hour.
            // This is intentionally coarse (hour-based) since processing is driven by HourlyTickEvent.
            var hourOfDay = ((currentHourNumber % 24) + 24) % 24;

            int[] starts;
            switch (window)
            {
                case FreeTimeDecisionWindow.Training:
                    // Morning/Afternoon starts (approx): 6, 12
                    starts = new[] { 6, 12 };
                    break;
                case FreeTimeDecisionWindow.Social:
                    // Dusk/Night starts (approx): 18, 22
                    starts = new[] { 18, 22 };
                    break;
                case FreeTimeDecisionWindow.Any:
                default:
                    return currentHourNumber;
            }

            // If inside the window already, allow firing as soon as safe.
            if (window == FreeTimeDecisionWindow.Training && (hourOfDay >= 6 && hourOfDay < 18))
            {
                return currentHourNumber;
            }

            if (window == FreeTimeDecisionWindow.Social && (hourOfDay >= 18 || hourOfDay < 6))
            {
                return currentHourNumber;
            }

            // Otherwise, compute the next start hour >= now.
            var bestDelta = int.MaxValue;
            foreach (var start in starts)
            {
                var delta = start - hourOfDay;
                if (delta <= 0)
                {
                    delta += 24;
                }
                bestDelta = Math.Min(bestDelta, delta);
            }

            if (bestDelta == int.MaxValue)
            {
                bestDelta = 0;
            }

            return currentHourNumber + bestDelta;
        }

        private void OnDailyTick()
        {
            try
            {
                // Reset daily counter
                _state.ResetDailyCounter();

                // Reset weekly counter if week changed
                _state.ResetWeeklyCounter();

                // Expire old flags
                _state.ExpireFlags();

                // Refresh situation flags at least once per day (safe fallback).
                UpdateSituationFlagsIfNeeded(EnlistmentBehavior.Instance, force: true);

                ModLogger.Debug(LogCategory, $"Daily reset complete. Fired today: {_state.FiredToday}, this week: {_state.FiredThisWeek}");
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, "Decision Events daily tick failed", ex);
            }
        }

        private void UpdateSituationFlagsIfNeeded(EnlistmentBehavior enlistment, bool force = false)
        {
            try
            {
                if (enlistment?.IsEnlisted != true)
                {
                    return;
                }

                var hourNumber = GetHourNumber();
                if (!force && _lastSituationUpdateHourNumber == hourNumber)
                {
                    return;
                }

                _lastSituationUpdateHourNumber = hourNumber;

                var flags = SituationFlagsProvider.Compute(enlistment);

                ApplySituationFlag("company_food_critical", flags.CompanyFoodCritical);
                ApplySituationFlag("company_threat_high", flags.CompanyThreatHigh);
                ApplySituationFlag("lance_fever_spike", flags.LanceFeverSpike);
                ApplySituationFlag("lance_short_handed", flags.LanceShortHanded);
                ApplySituationFlag("battle_imminent", flags.BattleImminent);
            }
            catch (Exception ex)
            {
                ModLogger.ErrorCode(LogCategory, "E-DECISIONS-FLAGS-001", "Failed to update situation flags", ex);
            }
        }

        private void ApplySituationFlag(string name, bool active)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return;
            }

            _state ??= new DecisionEventState();

            var currently = _state.HasActiveFlag(name);
            if (active && !currently)
            {
                // Use the state directly to avoid log spam (these update frequently).
                _state.SetFlag(name, expiryDays: 0f);
            }
            else if (!active && currently)
            {
                _state.ClearFlag(name);
            }
        }

        /// <summary>
        /// Record a resolved decision outcome (for Reports + Daily Report grounding) and post a short dispatch
        /// follow-up. Called from the centralized effects applier to ensure all delivery paths are covered.
        /// </summary>
        public void RecordDecisionOutcome(LanceLifeEventDefinition evt, LanceLifeEventOptionDefinition option, string resultText)
        {
            try
            {
                if (evt == null || option == null)
                {
                    return;
                }

                if (!string.Equals(evt.Category, "decision", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                _state ??= new DecisionEventState();

                var txt = (resultText ?? string.Empty).Replace("\r", " ").Replace("\n", " ").Trim();
                if (txt.Length > 240)
                {
                    txt = txt.Substring(0, 240).TrimEnd() + "...";
                }

                _state.RecordOutcome(evt.Id, option.Id, txt);

                // High-signal dispatch follow-up (personal feed).
                var enlistment = EnlistmentBehavior.Instance;
                var title = LanceLifeEventText.Resolve(evt.TitleId, evt.TitleFallback, evt.Id, enlistment);
                var headline = string.IsNullOrWhiteSpace(txt) ? title : $"{title}: {txt}";

                var dayNumber = (int)CampaignTime.Now.ToDays;
                var storyKey = $"decision_outcome:{evt.Id}:{dayNumber}";

                EnlistedNewsBehavior.Instance?.PostPersonalDispatchText("decision", headline, storyKey, minDisplayDays: 2);
            }
            catch (Exception ex)
            {
                ModLogger.ErrorCode(LogCategory, "E-DECISIONS-OUTCOME-001", $"Failed to record decision outcome for {evt?.Id}", ex);
            }
        }

        private bool IsEnabled()
        {
            var config = GetConfig();
            return config?.Enabled == true;
        }

        private bool IsEvaluationHour(int currentHour, DecisionEventConfig config)
        {
            if (config?.Pacing?.EvaluationHours == null || config.Pacing.EvaluationHours.Count == 0)
            {
                // Default: 8 AM, 2 PM, 8 PM
                return currentHour == 8 || currentHour == 14 || currentHour == 20;
            }

            return config.Pacing.EvaluationHours.Contains((int)currentHour);
        }

        private void TryEvaluateAndQueue(EnlistmentBehavior enlistment, DecisionEventConfig config)
        {
            var catalog = LanceLifeEventRuntime.GetCatalog();
            var allEvents = catalog?.Events ?? new List<LanceLifeEventDefinition>();

            // Filter to decision events only
            var decisionEvents = allEvents
                .Where(e => e.Category == "decision" && e.Delivery?.Method == "automatic")
                .ToList();

            if (decisionEvents.Count == 0)
            {
                ModLogger.Debug(LogCategory, "No decision events in catalog");
                return;
            }

            // Select an event using the evaluator
            var selected = _evaluator.SelectEvent(decisionEvents, _state, config, enlistment);

            if (selected == null)
            {
                ModLogger.Debug(LogCategory, "No eligible decision event selected");
                return;
            }

            // Queue for firing
            _queuedEventId = selected.Id;
            _queuedAtHour = GetHourNumber();

            ModLogger.Info(LogCategory, $"Queued decision event: {selected.Id}");
        }

        private void TryFireQueued(EnlistmentBehavior enlistment)
        {
            if (string.IsNullOrEmpty(_queuedEventId))
            {
                return;
            }

            // Check timeout (24 hours max queue time)
            var hoursSinceQueued = GetHourNumber() - _queuedAtHour;
            if (hoursSinceQueued > 24)
            {
                ModLogger.Warn(LogCategory, $"Dropping queued decision event due to timeout: {_queuedEventId}");
                ClearQueue();
                return;
            }

            // Check if safe to fire (not in combat, not in dialogue, etc.)
            if (!IsSafeToShowEvent())
            {
                return;
            }

            // Find the event
            var catalog = LanceLifeEventRuntime.GetCatalog();
            var evt = catalog?.Events?.FirstOrDefault(e => e.Id == _queuedEventId);

            if (evt == null)
            {
                ModLogger.Warn(LogCategory, $"Queued event not found in catalog: {_queuedEventId}");
                ClearQueue();
                return;
            }

            // Fire it
            if (FireEvent(evt, enlistment))
            {
                ModLogger.Info(LogCategory, $"Fired decision event: {evt.Id}");
            }

            ClearQueue();
        }

        private bool FireEvent(LanceLifeEventDefinition evt, EnlistmentBehavior enlistment, System.Action onEventClosed = null)
        {
            try
            {
                // Use the modern event presenter (matches existing infrastructure)
                var shown = Enlisted.Features.Lances.UI.ModernEventPresenter.TryShow(evt, enlistment, onEventClosed);

                if (shown)
                {
                    // Record in state for cooldown and pacing tracking
                    _state.RecordEventFired(
                        evt.Id,
                        evt.Category,
                        evt.Timing?.OneTime == true,
                        hasMaxPerTerm: (evt.Timing?.MaxPerTerm ?? 0) > 0
                    );
                }

                return shown;
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, $"Failed to fire decision event: {evt.Id}", ex);
                return false;
            }
        }

        private void ClearQueue()
        {
            _queuedEventId = string.Empty;
            _queuedAtHour = -1;
        }

        private bool IsSafeToShowEvent()
        {
            if (Campaign.Current == null)
            {
                return false;
            }

            // Don't stack decision popups on top of each other (modern UI sets its own global guard).
            if (Enlisted.Features.Lances.UI.ModernEventPresenter.IsEventShowing)
            {
                return false;
            }

            // Use the shared "ai_safe" guardrails to avoid firing during encounters/conversations/map events/prisoner state.
            if (!LanceLifeEventTriggerEvaluator.IsAiSafe())
            {
                return false;
            }

            // Not safe during menus (conversation, trade, etc.) - keep this as a belt-and-suspenders check.
            if (Campaign.Current.ConversationManager?.IsConversationInProgress == true)
            {
                return false;
            }

            // Not safe if game is paused for other reasons
            if (Campaign.Current.TimeControlMode == CampaignTimeControlMode.Stop ||
                Campaign.Current.TimeControlMode == CampaignTimeControlMode.UnstoppableFastForwardForPartyWaitTime)
            {
                return false;
            }

            return true;
        }

        private DecisionEventConfig GetConfig()
        {
            var currentHour = GetHourNumber();

            // Reload config every hour
            if (_config == null || currentHour != _lastConfigLoadHour)
            {
                _config = EnlistedConfig.LoadDecisionEventsConfig() ?? new DecisionEventConfig();
                _lastConfigLoadHour = currentHour;
            }

            return _config;
        }

        private static int GetHourNumber()
        {
            return (int)Math.Floor(CampaignTime.Now.ToHours);
        }
    }
}

