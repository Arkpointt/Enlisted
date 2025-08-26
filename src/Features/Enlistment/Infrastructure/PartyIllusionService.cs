using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Library;
using Enlisted.Core.Constants;

namespace Enlisted.Features.Enlistment.Infrastructure
{
    /// <summary>
    /// Infrastructure service managing the visual "party illusion" while enlisted.
    /// Hides player party from map, maintains camera on commander, and handles
    /// state restoration. Uses basic visibility controls with safe fallbacks.
    /// 
    /// Note: Advanced reflection-based hiding removed to avoid SandBox dependencies.
    /// Uses core TaleWorlds APIs that are stable across game versions.
    /// </summary>
    public static class PartyIllusionService
    {
        private static bool _originalVisibilityState = true;

        /// <summary>
        /// Hide the player party and set camera to follow the commander.
        /// Uses basic visibility controls for maximum compatibility.
        /// Ensures smooth visual transition to enlisted state.
        /// </summary>
        public static void HidePlayerPartyAndFollowCommander(Hero commander)
        {
            var main = MobileParty.MainParty;
            var commanderParty = commander?.PartyBelongedTo;
            
            if (main == null || commanderParty == null) return;

            try
            {
                // Remember original visibility state for restoration
                _originalVisibilityState = main.IsVisible;

                // Use basic hiding approach for stability
                main.IsVisible = false;

                // Transfer camera control to commander
                commanderParty.Party.SetAsCameraFollowParty();

                InformationManager.DisplayMessage(new InformationMessage(Constants.Messages.FOLLOWING_COMMANDER));
            }
            catch (Exception ex)
            {
                // TODO: Replace with centralized logging service
                InformationManager.DisplayMessage(new InformationMessage(
                    string.Format(Constants.Messages.COULD_NOT_HIDE_PARTY, ex.Message)));
            }
        }

        /// <summary>
        /// Restore the player party visibility and return camera control.
        /// Reverses the illusion effects to return to normal gameplay state.
        /// </summary>
        public static void RestorePlayerPartyVisibility()
        {
            var main = MobileParty.MainParty;
            if (main == null) return;

            try
            {
                main.IsVisible = _originalVisibilityState;
                main.Party.SetAsCameraFollowParty();
                InformationManager.DisplayMessage(new InformationMessage(Constants.Messages.RESTORED_COMMAND));
            }
            catch (Exception ex)
            {
                // TODO: Replace with centralized logging service
                InformationManager.DisplayMessage(new InformationMessage(
                    string.Format(Constants.Messages.COULD_NOT_RESTORE_PARTY, ex.Message)));
            }
        }

        /// <summary>
        /// Idempotent maintenance that keeps the illusion active.
        /// Called during tick to ensure visual consistency is maintained.
        /// </summary>
        public static void MaintainIllusion(Hero commander)
        {
            var main = MobileParty.MainParty;
            if (main != null && main.IsVisible && commander?.PartyBelongedTo != null)
            {
                main.IsVisible = false;
                commander.PartyBelongedTo.Party.SetAsCameraFollowParty();
            }
        }

        /// <summary>Get cached original visibility state for save persistence.</summary>
        public static bool GetOriginalVisibilityState() => _originalVisibilityState;

        /// <summary>Set cached visibility state during save/load operations.</summary>
        public static void SetOriginalVisibilityState(bool wasVisible) => _originalVisibilityState = wasVisible;
    }
}
