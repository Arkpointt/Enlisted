namespace Enlisted.Services.Abstractions
{
    using TaleWorlds.CampaignSystem;

    /// <summary>
    /// Contract for the visual party illusion used while enlisted.
    /// Implementations hide the player party, set camera follow to the commander,
    /// and restore state when leaving service.
    /// </summary>
    public interface IPartyIllusionService
    {
        /// <summary>
        /// Hide the player party and make the camera follow the commander.
        /// </summary>
        void HidePlayerPartyAndFollowCommander(Hero commander);

        /// <summary>
        /// Restore the player party visibility and return camera control.
        /// </summary>
        void RestorePlayerPartyVisibility();

        /// <summary>
        /// Maintain illusion during OnTick (idempotent and lightweight).
        /// </summary>
        void MaintainIllusion(Hero commander);

        /// <summary>
        /// Read the cached visibility state captured before hiding.
        /// </summary>
        bool GetOriginalVisibilityState();

        /// <summary>
        /// Set the cached visibility state (used during SyncData).
        /// </summary>
        void SetOriginalVisibilityState(bool wasVisible);
    }
}
