using System;
using SandBox.View.Map;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Library;
using Enlisted.Utils;

namespace Enlisted.Services
{
    /// <summary>
    /// Manages the party illusion system - hiding player party and following commander.
    /// Creates the appearance of a merged party when enlisted.
    /// Uses advanced visual hiding to prevent conflicts with army visibility.
    /// </summary>
    public static class PartyIllusionService
    {
        private static bool _originalVisibilityState = true;

        /// <summary>
        /// Hides the player party and sets camera to follow the commander.
        /// Uses advanced visual hiding to prevent army visibility conflicts.
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

                // Advanced hiding method - same as original ServeAsSoldier mod
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
        /// Restores the player party visibility and returns camera control.
        /// </summary>
        public static void RestorePlayerPartyVisibility()
        {
            var main = MobileParty.MainParty;
            if (main == null) return;

            try
            {
                // Advanced restoration method
                ShowPlayerPartyAdvanced();

                // Return camera to player party
                main.Party.SetAsCameraFollowParty();

                InformationManager.DisplayMessage(new InformationMessage(Constants.Messages.RESTORED_COMMAND));
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage(string.Format(Constants.Messages.COULD_NOT_RESTORE_PARTY, ex.Message)));
            }
        }

        /// <summary>
        /// Advanced party hiding using PartyVisualManager.
        /// This method completely hides the visual entities to prevent army visibility conflicts.
        /// Uses reflection for safe access to avoid assembly reference issues.
        /// </summary>
        private static void HidePlayerPartyAdvanced()
        {
            try
            {
                var main = MobileParty.MainParty;
                
                // Basic visibility hiding
                main.IsVisible = false;
                
                // Advanced visual entity hiding using PartyVisualManager with reflection
                var partyVisualManager = PartyVisualManager.Current;
                if (partyVisualManager != null)
                {
                    var partyVisual = partyVisualManager.GetVisualOfParty(main.Party);
                    if (partyVisual != null)
                    {
                        var partyVisualType = partyVisual.GetType();
                        
                        // Hide human agent visuals using reflection
                        var humanVisualsProperty = partyVisualType.GetProperty("HumanAgentVisuals");
                        var humanVisuals = humanVisualsProperty?.GetValue(partyVisual);
                        if (humanVisuals != null)
                        {
                            var getEntityMethod = humanVisuals.GetType().GetMethod("GetEntity");
                            var entity = getEntityMethod?.Invoke(humanVisuals, null);
                            if (entity != null)
                            {
                                var setVisibilityMethod = entity.GetType().GetMethod("SetVisibilityExcludeParents");
                                setVisibilityMethod?.Invoke(entity, new object[] { false });
                            }
                        }
                        
                        // Hide mount agent visuals using reflection
                        var mountVisualsProperty = partyVisualType.GetProperty("MountAgentVisuals");
                        var mountVisuals = mountVisualsProperty?.GetValue(partyVisual);
                        if (mountVisuals != null)
                        {
                            var getEntityMethod = mountVisuals.GetType().GetMethod("GetEntity");
                            var entity = getEntityMethod?.Invoke(mountVisuals, null);
                            if (entity != null)
                            {
                                var setVisibilityMethod = entity.GetType().GetMethod("SetVisibilityExcludeParents");
                                setVisibilityMethod?.Invoke(entity, new object[] { false });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Fallback to basic hiding if advanced method fails
                MobileParty.MainParty.IsVisible = false;
                InformationManager.DisplayMessage(new InformationMessage($"[Enlisted] Using basic party hiding: {ex.Message}"));
            }
        }

        /// <summary>
        /// Advanced party restoration using PartyVisualManager.
        /// This method restores the visual entities properly using reflection.
        /// </summary>
        private static void ShowPlayerPartyAdvanced()
        {
            try
            {
                var main = MobileParty.MainParty;
                
                // Basic visibility restoration
                main.IsVisible = _originalVisibilityState;
                
                // Advanced visual entity restoration using PartyVisualManager with reflection
                var partyVisualManager = PartyVisualManager.Current;
                if (partyVisualManager != null)
                {
                    var partyVisual = partyVisualManager.GetVisualOfParty(main.Party);
                    if (partyVisual != null)
                    {
                        var partyVisualType = partyVisual.GetType();
                        
                        // Restore human agent visuals using reflection
                        var humanVisualsProperty = partyVisualType.GetProperty("HumanAgentVisuals");
                        var humanVisuals = humanVisualsProperty?.GetValue(partyVisual);
                        if (humanVisuals != null)
                        {
                            var getEntityMethod = humanVisuals.GetType().GetMethod("GetEntity");
                            var entity = getEntityMethod?.Invoke(humanVisuals, null);
                            if (entity != null)
                            {
                                var setVisibilityMethod = entity.GetType().GetMethod("SetVisibilityExcludeParents");
                                setVisibilityMethod?.Invoke(entity, new object[] { true });
                            }
                        }
                        
                        // Restore mount agent visuals using reflection
                        var mountVisualsProperty = partyVisualType.GetProperty("MountAgentVisuals");
                        var mountVisuals = mountVisualsProperty?.GetValue(partyVisual);
                        if (mountVisuals != null)
                        {
                            var getEntityMethod = mountVisuals.GetType().GetMethod("GetEntity");
                            var entity = getEntityMethod?.Invoke(mountVisuals, null);
                            if (entity != null)
                            {
                                var setVisibilityMethod = entity.GetType().GetMethod("SetVisibilityExcludeParents");
                                setVisibilityMethod?.Invoke(entity, new object[] { true });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Fallback to basic restoration if advanced method fails
                MobileParty.MainParty.IsVisible = _originalVisibilityState;
                InformationManager.DisplayMessage(new InformationMessage($"[Enlisted] Using basic party restoration: {ex.Message}"));
            }
        }

        /// <summary>
        /// Maintains the illusion by ensuring the player party stays hidden.
        /// Call this during OnTick to maintain the effect.
        /// </summary>
        public static void MaintainIllusion(Hero commander)
        {
            var main = MobileParty.MainParty;
            if (main != null && main.IsVisible && commander?.PartyBelongedTo != null)
            {
                HidePlayerPartyAdvanced();
                
                // Ensure camera stays on commander
                commander.PartyBelongedTo.Party.SetAsCameraFollowParty();
            }
        }

        /// <summary>
        /// Gets the original visibility state before hiding.
        /// </summary>
        public static bool GetOriginalVisibilityState()
        {
            return _originalVisibilityState;
        }

        /// <summary>
        /// Sets the original visibility state (used during save/load).
        /// </summary>
        public static void SetOriginalVisibilityState(bool wasVisible)
        {
            _originalVisibilityState = wasVisible;
        }
    }
}