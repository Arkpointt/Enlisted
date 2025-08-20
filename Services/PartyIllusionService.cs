using System;
using SandBox.View.Map;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Library;
using Enlisted.Utils;

namespace Enlisted.Services
{
    /// <summary>
    /// Manages the visual "party illusion" while enlisted: hides the player party,
    /// keeps camera on the commander, and restores state on discharge.
    /// </summary>
    public static class PartyIllusionService
    {
        private static bool _originalVisibilityState = true;

        /// <summary>
        /// Hide the player party and set camera to follow the commander.
        /// Uses PartyVisualManager reflection to fully hide entities (with a basic fallback).
        /// </summary>
        public static void HidePlayerPartyAndFollowCommander(Hero commander)
        {
            var main = MobileParty.MainParty;
            var commanderParty = commander?.PartyBelongedTo;
            
            if (main == null || commanderParty == null) return;

            try
            {
                // Remember original visibility state
                _originalVisibilityState = main.IsVisible;

                // Advanced hiding method - reflection-based
                HidePlayerPartyAdvanced();

                // Set camera to follow commander's party
                commanderParty.Party.SetAsCameraFollowParty();

                InformationManager.DisplayMessage(new InformationMessage(Constants.Messages.FOLLOWING_COMMANDER));
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage(string.Format(Constants.Messages.COULD_NOT_HIDE_PARTY, ex.Message)));
            }
        }

        /// <summary>
        /// Restore the player party and return camera control to the player party.
        /// </summary>
        public static void RestorePlayerPartyVisibility()
        {
            var main = MobileParty.MainParty;
            if (main == null) return;

            try
            {
                ShowPlayerPartyAdvanced();
                main.Party.SetAsCameraFollowParty();
                InformationManager.DisplayMessage(new InformationMessage(Constants.Messages.RESTORED_COMMAND));
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage(string.Format(Constants.Messages.COULD_NOT_RESTORE_PARTY, ex.Message)));
            }
        }

        // Advanced hiding using PartyVisualManager reflection helpers.
        private static void HidePlayerPartyAdvanced()
        {
            try
            {
                var main = MobileParty.MainParty;
                main.IsVisible = false; // basic hiding first
                
                var partyVisualManager = PartyVisualManager.Current;
                if (partyVisualManager != null)
                {
                    var partyVisual = partyVisualManager.GetVisualOfParty(main.Party);
                    if (partyVisual != null)
                    {
                        var partyVisualType = partyVisual.GetType();
                        var humanVisuals = partyVisualType.GetProperty("HumanAgentVisuals")?.GetValue(partyVisual);
                        if (humanVisuals != null)
                        {
                            var entity = humanVisuals.GetType().GetMethod("GetEntity")?.Invoke(humanVisuals, null);
                            entity?.GetType().GetMethod("SetVisibilityExcludeParents")?.Invoke(entity, new object[] { false });
                        }

                        var mountVisuals = partyVisualType.GetProperty("MountAgentVisuals")?.GetValue(partyVisual);
                        if (mountVisuals != null)
                        {
                            var entity = mountVisuals.GetType().GetMethod("GetEntity")?.Invoke(mountVisuals, null);
                            entity?.GetType().GetMethod("SetVisibilityExcludeParents")?.Invoke(entity, new object[] { false });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Fallback to basic hiding
                MobileParty.MainParty.IsVisible = false;
                InformationManager.DisplayMessage(new InformationMessage($"[Enlisted] Using basic party hiding: {ex.Message}"));
            }
        }

        // Advanced restoration using PartyVisualManager reflection helpers.
        private static void ShowPlayerPartyAdvanced()
        {
            try
            {
                var main = MobileParty.MainParty;
                main.IsVisible = _originalVisibilityState;
                
                var partyVisualManager = PartyVisualManager.Current;
                if (partyVisualManager != null)
                {
                    var partyVisual = partyVisualManager.GetVisualOfParty(main.Party);
                    if (partyVisual != null)
                    {
                        var partyVisualType = partyVisual.GetType();
                        var humanVisuals = partyVisualType.GetProperty("HumanAgentVisuals")?.GetValue(partyVisual);
                        if (humanVisuals != null)
                        {
                            var entity = humanVisuals.GetType().GetMethod("GetEntity")?.Invoke(humanVisuals, null);
                            entity?.GetType().GetMethod("SetVisibilityExcludeParents")?.Invoke(entity, new object[] { true });
                        }

                        var mountVisuals = partyVisualType.GetProperty("MountAgentVisuals")?.GetValue(partyVisual);
                        if (mountVisuals != null)
                        {
                            var entity = mountVisuals.GetType().GetMethod("GetEntity")?.Invoke(mountVisuals, null);
                            entity?.GetType().GetMethod("SetVisibilityExcludeParents")?.Invoke(entity, new object[] { true });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MobileParty.MainParty.IsVisible = _originalVisibilityState;
                InformationManager.DisplayMessage(new InformationMessage($"[Enlisted] Using basic party restoration: {ex.Message}"));
            }
        }

        /// <summary>
        /// Idempotent maintenance step that keeps the player party hidden and camera on the commander.
        /// </summary>
        public static void MaintainIllusion(Hero commander)
        {
            var main = MobileParty.MainParty;
            if (main != null && main.IsVisible && commander?.PartyBelongedTo != null)
            {
                HidePlayerPartyAdvanced();
                commander.PartyBelongedTo.Party.SetAsCameraFollowParty();
            }
        }

        /// <summary>
        /// Read cached visibility state captured before hiding.
        /// </summary>
        public static bool GetOriginalVisibilityState() => _originalVisibilityState;

        /// <summary>
        /// Set cached visibility state (used during save/load SyncData).
        /// </summary>
        public static void SetOriginalVisibilityState(bool wasVisible) => _originalVisibilityState = wasVisible;
    }
}
