using System;
using System.Linq;
using Enlisted.Features.Escalation;
using Enlisted.Mod.Core.Config;
using Enlisted.Mod.Core.Logging;
using TaleWorlds.CampaignSystem;

namespace Enlisted.Features.Content
{
    /// <summary>
    /// Enforces global event pacing limits across all automatic event sources.
    /// Both EventPacingManager and MapIncidentManager check this before firing events.
    /// Reads all limits from enlisted_config.json → decision_events.pacing section.
    /// 
    /// Enforced limits:
    /// - max_per_day: Maximum automatic events per day
    /// - max_per_week: Maximum automatic events per week
    /// - min_hours_between: Minimum hours between any automatic events
    /// - evaluation_hours: Specific hours when events can fire (empty = any hour)
    /// - per_category_cooldown_days: Days between events of same category
    /// - quiet_day_chance: Random chance to skip events for a day
    /// </summary>
    public static class GlobalEventPacer
    {
        private const string LogCategory = "GlobalPacing";

        // Cached config to avoid repeated file reads
        private static EventPacingConfig _cachedConfig;
        private static bool _configLoaded;

        /// <summary>
        /// Gets the pacing config, loading it once and caching for performance.
        /// </summary>
        private static EventPacingConfig Config
        {
            get
            {
                if (!_configLoaded)
                {
                    _cachedConfig = ConfigurationManager.LoadEventPacingConfig();
                    _configLoaded = true;
                    ModLogger.Debug(LogCategory, 
                        $"Loaded pacing config: max_per_day={_cachedConfig.MaxPerDay}, " +
                        $"max_per_week={_cachedConfig.MaxPerWeek}, min_hours_between={_cachedConfig.MinHoursBetween}, " +
                        $"window={_cachedConfig.EventWindowMinDays}-{_cachedConfig.EventWindowMaxDays} days");
                }
                return _cachedConfig;
            }
        }

        /// <summary>
        /// Checks if an automatic event is allowed to fire right now.
        /// Enforces all config limits: max_per_day, max_per_week, min_hours_between,
        /// evaluation_hours, per_category_cooldown, and quiet_day_chance.
        /// </summary>
        /// <param name="eventId">The event ID (for logging)</param>
        /// <param name="reason">Output: why the event was blocked, if any</param>
        /// <returns>True if the event can fire, false if blocked by pacing limits</returns>
        public static bool CanFireAutoEvent(string eventId, out string reason)
        {
            return CanFireAutoEvent(eventId, null, out reason);
        }

        /// <summary>
        /// Checks if an automatic event is allowed to fire right now with category check.
        /// </summary>
        /// <param name="eventId">The event ID (for logging)</param>
        /// <param name="category">Optional category for per-category cooldown</param>
        /// <param name="reason">Output: why the event was blocked, if any</param>
        /// <returns>True if the event can fire, false if blocked by pacing limits</returns>
        public static bool CanFireAutoEvent(string eventId, string category, out string reason)
        {
            return CanFireAutoEvent(eventId, category, skipEvaluationHours: false, out reason);
        }

        /// <summary>
        /// Checks if an automatic event is allowed to fire right now with full options.
        /// </summary>
        /// <param name="eventId">The event ID (for logging)</param>
        /// <param name="category">Optional category for per-category cooldown</param>
        /// <param name="skipEvaluationHours">If true, skips evaluation_hours check (for context-triggered events like map incidents)</param>
        /// <param name="reason">Output: why the event was blocked, if any</param>
        /// <returns>True if the event can fire, false if blocked by pacing limits</returns>
        public static bool CanFireAutoEvent(string eventId, string category, bool skipEvaluationHours, out string reason)
        {
            reason = null;

            var state = EscalationManager.Instance?.State;
            if (state == null)
            {
                reason = "EscalationState not available";
                return false;
            }

            var config = Config;
            var now = CampaignTime.Now;
            var currentDay = (int)now.ToDays;
            var currentWeek = currentDay / 7;
            var currentHour = (int)now.CurrentHourInDay;

            // Roll over daily count and quiet day flag if it's a new day
            if (state.AutoEventDayNumber != currentDay)
            {
                state.AutoEventDayNumber = currentDay;
                state.AutoEventsToday = 0;
                state.IsQuietDay = false; // Reset quiet day flag for new day
            }

            // Roll over weekly count if it's a new week
            if (state.AutoEventWeekNumber != currentWeek)
            {
                state.AutoEventWeekNumber = currentWeek;
                state.AutoEventsThisWeek = 0;
            }

            // Check quiet day first (only roll once per day when first event is attempted)
            if (config.AllowQuietDays && state.AutoEventsToday == 0 && !state.IsQuietDay)
            {
                var quietRoll = TaleWorlds.Core.MBRandom.RandomFloat;
                if (quietRoll < config.QuietDayChance)
                {
                    state.IsQuietDay = true;
                    ModLogger.Info(LogCategory, $"Today is a quiet day (roll={quietRoll:F2} < {config.QuietDayChance})");
                }
            }

            if (state.IsQuietDay)
            {
                reason = "Quiet day - no automatic events today";
                ModLogger.Debug(LogCategory, $"Blocking {eventId}: {reason}");
                return false;
            }

            // Check evaluation hours (if specified and not skipped for context-triggered events)
            if (!skipEvaluationHours && config.EvaluationHours != null && config.EvaluationHours.Count > 0)
            {
                if (!config.EvaluationHours.Contains(currentHour))
                {
                    reason = $"Not an evaluation hour (current: {currentHour}, allowed: {string.Join(",", config.EvaluationHours)})";
                    ModLogger.Debug(LogCategory, $"Blocking {eventId}: {reason}");
                    return false;
                }
            }

            // Check daily limit
            if (state.AutoEventsToday >= config.MaxPerDay)
            {
                reason = $"Daily limit reached ({state.AutoEventsToday}/{config.MaxPerDay})";
                ModLogger.Debug(LogCategory, $"Blocking {eventId}: {reason}");
                return false;
            }

            // Check weekly limit
            if (state.AutoEventsThisWeek >= config.MaxPerWeek)
            {
                reason = $"Weekly limit reached ({state.AutoEventsThisWeek}/{config.MaxPerWeek})";
                ModLogger.Debug(LogCategory, $"Blocking {eventId}: {reason}");
                return false;
            }

            // Check minimum hours between events
            if (state.LastAutoEventTime != CampaignTime.Zero)
            {
                var hoursSinceLastEvent = (now - state.LastAutoEventTime).ToHours;
                if (hoursSinceLastEvent < config.MinHoursBetween)
                {
                    reason = $"Too soon since last event ({hoursSinceLastEvent:F1}h elapsed, need {config.MinHoursBetween}h)";
                    ModLogger.Debug(LogCategory, $"Blocking {eventId}: {reason}");
                    return false;
                }
            }

            // Check per-category cooldown (if category provided and cooldown > 0)
            if (!string.IsNullOrEmpty(category) && config.PerCategoryCooldownDays > 0)
            {
                if (IsCategoryOnCooldown(state, category, config.PerCategoryCooldownDays))
                {
                    reason = $"Category '{category}' on cooldown ({config.PerCategoryCooldownDays} day limit)";
                    ModLogger.Debug(LogCategory, $"Blocking {eventId}: {reason}");
                    return false;
                }
            }

            ModLogger.Debug(LogCategory, 
                $"Allowing {eventId}: today={state.AutoEventsToday}/{config.MaxPerDay}, " +
                $"week={state.AutoEventsThisWeek}/{config.MaxPerWeek}, hour={currentHour}");
            return true;
        }

        /// <summary>
        /// Checks if a category is on cooldown based on last event time.
        /// </summary>
        private static bool IsCategoryOnCooldown(EscalationState state, string category, int cooldownDays)
        {
            if (state.CategoryLastFired == null)
            {
                return false;
            }

            if (!state.CategoryLastFired.TryGetValue(category, out var lastFired))
            {
                return false;
            }

            var daysSinceLastFired = (CampaignTime.Now - lastFired).ToDays;
            return daysSinceLastFired < cooldownDays;
        }

        /// <summary>
        /// Records that an automatic event was fired, updating counts and timestamps.
        /// Call this after successfully queuing an event.
        /// </summary>
        /// <param name="eventId">The event ID (for logging)</param>
        public static void RecordAutoEvent(string eventId)
        {
            RecordAutoEvent(eventId, null);
        }

        /// <summary>
        /// Records that an automatic event was fired with category tracking.
        /// </summary>
        /// <param name="eventId">The event ID (for logging)</param>
        /// <param name="category">Optional category for per-category cooldown tracking</param>
        public static void RecordAutoEvent(string eventId, string category)
        {
            var state = EscalationManager.Instance?.State;
            if (state == null)
            {
                ModLogger.Warn(LogCategory, "Cannot record event - EscalationState not available");
                return;
            }

            var now = CampaignTime.Now;
            var currentDay = (int)now.ToDays;
            var currentWeek = currentDay / 7;

            // Ensure counters are for current day/week
            if (state.AutoEventDayNumber != currentDay)
            {
                state.AutoEventDayNumber = currentDay;
                state.AutoEventsToday = 0;
                state.IsQuietDay = false;
            }
            if (state.AutoEventWeekNumber != currentWeek)
            {
                state.AutoEventWeekNumber = currentWeek;
                state.AutoEventsThisWeek = 0;
            }

            // Update tracking
            state.LastAutoEventTime = now;
            state.AutoEventsToday++;
            state.AutoEventsThisWeek++;

            // Track category if provided
            if (!string.IsNullOrEmpty(category))
            {
                state.RecordCategoryFired(category);
            }

            ModLogger.Info(LogCategory, 
                $"Recorded auto event: {eventId} (today={state.AutoEventsToday}, week={state.AutoEventsThisWeek}" +
                (string.IsNullOrEmpty(category) ? ")" : $", category={category})"));
        }

        /// <summary>
        /// Gets diagnostic info about current pacing state for debug display.
        /// </summary>
        public static string GetPacingDiagnostics()
        {
            var state = EscalationManager.Instance?.State;
            if (state == null)
            {
                return "Pacing: No state available";
            }

            var config = Config;
            var now = CampaignTime.Now;
            var hoursSinceLast = state.LastAutoEventTime != CampaignTime.Zero
                ? (now - state.LastAutoEventTime).ToHours
                : -1;

            return $"Pacing: {state.AutoEventsToday}/{config.MaxPerDay} today, " +
                   $"{state.AutoEventsThisWeek}/{config.MaxPerWeek} this week, " +
                   $"{hoursSinceLast:F1}h since last (min {config.MinHoursBetween}h)";
        }

        /// <summary>
        /// Resets cached config (for hot-reload during development).
        /// </summary>
        public static void ReloadConfig()
        {
            _configLoaded = false;
            _cachedConfig = null;
            ModLogger.Info(LogCategory, "Pacing config cache cleared");
        }
    }
}

