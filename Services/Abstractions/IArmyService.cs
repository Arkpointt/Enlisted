namespace Enlisted.Services.Abstractions
{
    using TaleWorlds.CampaignSystem;

    /// <summary>
    /// Contract for army-related operations while enlisted.
    /// Implementations encapsulate Bannerlord API calls for creating/joining/leaving
    /// commander armies and for maintaining escort AI safely.
    /// </summary>
    public interface IArmyService
    {
        /// <summary>
        /// Attempts to begin service by following the specified commander.
        /// Implementations should avoid heavy game state changes and prefer
        /// safe AI escort operations.
        /// </summary>
        /// <param name="commander">Commander hero to serve under.</param>
        /// <returns>True if escort/join succeeded; otherwise false.</returns>
        bool TryJoinCommandersArmy(Hero commander);

        /// <summary>
        /// Leaves current service and restores independent party AI.
        /// </summary>
        void LeaveCurrentArmy();

        /// <summary>
        /// Detaches from any follow/escort behavior in a fail-safe way.
        /// Should never throw; used during emergency cleanups.
        /// </summary>
        void SafeDetach();
    }
}
