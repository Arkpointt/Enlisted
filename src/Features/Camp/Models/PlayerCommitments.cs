using System;
using TaleWorlds.CampaignSystem;

namespace Enlisted.Features.Camp.Models
{
    /// <summary>
    /// Tracks player's scheduled activity commitments.
    /// Only one active commitment at a time. Persists across save/load.
    /// </summary>
    [Serializable]
    public class PlayerCommitments
    {
        /// <summary>ID of the scheduled activity (null if no commitment).</summary>
        public string ScheduledActivityId { get; set; }

        /// <summary>Time when the activity is scheduled (null if no commitment).</summary>
        public float? ScheduledTimeHours { get; set; }

        /// <summary>Display text for the YOU section (e.g., "You've agreed to join the card game tonight").</summary>
        public string CommitmentDisplayText { get; set; }

        /// <summary>Whether the player has an active commitment.</summary>
        public bool HasCommitment => !string.IsNullOrEmpty(ScheduledActivityId);

        /// <summary>
        /// Commits the player to a scheduled activity.
        /// </summary>
        public void CommitTo(string activityId, float hoursFromNow, string displayText)
        {
            ScheduledActivityId = activityId;
            ScheduledTimeHours = (float)CampaignTime.Now.ToHours + hoursFromNow;
            CommitmentDisplayText = displayText;
        }

        /// <summary>
        /// Clears the current commitment.
        /// </summary>
        public void ClearCommitment()
        {
            ScheduledActivityId = null;
            ScheduledTimeHours = null;
            CommitmentDisplayText = null;
        }

        /// <summary>
        /// Checks if the scheduled time has passed.
        /// </summary>
        public bool IsTimeToFire()
        {
            if (!HasCommitment || !ScheduledTimeHours.HasValue)
            {
                return false;
            }

            float currentHour = (float)CampaignTime.Now.ToHours;
            return currentHour >= ScheduledTimeHours.Value;
        }

        /// <summary>
        /// Gets hours until the scheduled activity fires.
        /// </summary>
        public float GetHoursUntilActivity()
        {
            if (!HasCommitment || !ScheduledTimeHours.HasValue)
            {
                return 0f;
            }

            float currentHour = (float)CampaignTime.Now.ToHours;
            return Math.Max(0, ScheduledTimeHours.Value - currentHour);
        }
    }
}
