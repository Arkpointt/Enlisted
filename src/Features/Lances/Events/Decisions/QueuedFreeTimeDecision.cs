using System;

namespace Enlisted.Features.Lances.Events.Decisions
{
    public enum FreeTimeDecisionKind
    {
        Event = 0,
        TrainingAction = 1
    }

    public enum FreeTimeDecisionWindow
    {
        Any = 0,
        Training = 1, // Morning/Afternoon
        Social = 2 // Dusk/Night
    }

    /// <summary>
    /// Persistable queued "Free Time" decision record.
    /// Stored in save as a compact string via <see cref="ToSaveString"/>.
    /// </summary>
    public sealed class QueuedFreeTimeDecision
    {
        public FreeTimeDecisionKind Kind { get; set; }
        public FreeTimeDecisionWindow Window { get; set; }

        /// <summary>
        /// Event id (when Kind == Event) or action id (when Kind == TrainingAction).
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Desired minimum total fatigue cost for this decision (some event options already consume fatigue).
        /// We may consume additional fatigue upfront to ensure this minimum is met.
        /// </summary>
        public int DesiredFatigueCost { get; set; }

        /// <summary>
        /// Absolute campaign hour number when this decision becomes eligible to execute.
        /// </summary>
        public int EarliestHourNumber { get; set; }

        /// <summary>
        /// Absolute campaign hour number when this decision was queued.
        /// Used for timeouts and diagnostics.
        /// </summary>
        public int QueuedAtHourNumber { get; set; }

        public string ToSaveString()
        {
            // Format:
            // kind|window|desiredFatigue|earliestHour|queuedAtHour|id
            return $"{(int)Kind}|{(int)Window}|{DesiredFatigueCost}|{EarliestHourNumber}|{QueuedAtHourNumber}|{Id ?? string.Empty}";
        }

        public static bool TryParse(string raw, out QueuedFreeTimeDecision decision)
        {
            decision = null;

            if (string.IsNullOrWhiteSpace(raw))
            {
                return false;
            }

            var parts = raw.Split(new[] { '|' }, 6, StringSplitOptions.None);
            if (parts.Length != 6)
            {
                return false;
            }

            if (!int.TryParse(parts[0], out var kindInt) ||
                !int.TryParse(parts[1], out var windowInt) ||
                !int.TryParse(parts[2], out var desiredFatigue) ||
                !int.TryParse(parts[3], out var earliestHour) ||
                !int.TryParse(parts[4], out var queuedAtHour))
            {
                return false;
            }

            var id = parts[5] ?? string.Empty;
            if (string.IsNullOrWhiteSpace(id))
            {
                return false;
            }

            decision = new QueuedFreeTimeDecision
            {
                Kind = (FreeTimeDecisionKind)kindInt,
                Window = (FreeTimeDecisionWindow)windowInt,
                DesiredFatigueCost = Math.Max(0, desiredFatigue),
                EarliestHourNumber = earliestHour,
                QueuedAtHourNumber = queuedAtHour,
                Id = id
            };

            return true;
        }
    }
}


