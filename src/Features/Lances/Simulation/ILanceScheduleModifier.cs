using System.Collections.Generic;
using Enlisted.Features.Schedule.Models;

namespace Enlisted.Features.Lances.Simulation
{
    /// <summary>
    /// Interface for systems that can modify the AI Camp Schedule based on lance member states.
    /// Track C1: Integration point between Lance Life Simulation and AI Schedule.
    /// </summary>
    public interface ILanceScheduleModifier
    {
        /// <summary>
        /// Get the current availability ratio for lance members (0-1).
        /// Used by AI Schedule to adjust duty requirements.
        /// </summary>
        float GetLanceAvailabilityRatio();

        /// <summary>
        /// Get list of members who cannot perform duties.
        /// </summary>
        IEnumerable<string> GetUnavailableMemberIds();

        /// <summary>
        /// Check if a specific duty can be covered given current lance state.
        /// </summary>
        bool CanCoverDuty(string dutyId);

        /// <summary>
        /// Request schedule modification due to member unavailability.
        /// </summary>
        ScheduleModificationRequest RequestModification(string memberId, string reason);

        /// <summary>
        /// Notify the lance simulation that a duty was unfulfilled.
        /// </summary>
        void NotifyUnfulfilledDuty(string dutyId, string reason);

        /// <summary>
        /// Get suggested duty reassignment when a member becomes unavailable.
        /// </summary>
        DutyReassignment GetReassignmentSuggestion(string originalMemberId, string dutyId);
    }

    /// <summary>
    /// Request to modify the schedule due to lance life events.
    /// </summary>
    public class ScheduleModificationRequest
    {
        /// <summary>ID of the member affected</summary>
        public string MemberId { get; set; }

        /// <summary>Reason for the modification</summary>
        public string Reason { get; set; }

        /// <summary>Type of modification requested</summary>
        public ModificationType Type { get; set; }

        /// <summary>Suggested replacement member ID (if applicable)</summary>
        public string ReplacementMemberId { get; set; }

        /// <summary>Duration of the modification in hours</summary>
        public int DurationHours { get; set; }

        /// <summary>Priority level (higher = more urgent)</summary>
        public int Priority { get; set; }
    }

    /// <summary>
    /// Type of schedule modification.
    /// </summary>
    public enum ModificationType
    {
        /// <summary>Remove member from current duty</summary>
        RemoveFromDuty,

        /// <summary>Assign replacement member</summary>
        AssignReplacement,

        /// <summary>Skip duty entirely</summary>
        SkipDuty,

        /// <summary>Redistribute duty among remaining members</summary>
        RedistributeDuty,

        /// <summary>Add player to cover duty</summary>
        PlayerCover
    }

    /// <summary>
    /// Suggested duty reassignment.
    /// </summary>
    public class DutyReassignment
    {
        /// <summary>ID of the original member who can't perform duty</summary>
        public string OriginalMemberId { get; set; }

        /// <summary>ID of the duty that needs coverage</summary>
        public string DutyId { get; set; }

        /// <summary>ID of suggested replacement member</summary>
        public string ReplacementMemberId { get; set; }

        /// <summary>Name of suggested replacement</summary>
        public string ReplacementName { get; set; }

        /// <summary>Whether player should be asked to cover</summary>
        public bool SuggestPlayerCover { get; set; }

        /// <summary>Confidence level of the suggestion (0-1)</summary>
        public float Confidence { get; set; }
    }
}

