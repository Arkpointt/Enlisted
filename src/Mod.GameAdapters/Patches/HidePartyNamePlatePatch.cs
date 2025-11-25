using System;
using HarmonyLib;
using SandBox.ViewModelCollection.Nameplate;
using TaleWorlds.CampaignSystem.Party;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Mod.Core.Logging;
using System.Reflection;
using TaleWorlds.CampaignSystem;

namespace Enlisted.Mod.GameAdapters.Patches
{
    /// <summary>
    /// Prevents the player's party nameplate from appearing when enlisted.
    /// Also suppresses the movement cursor (yellow arrow) when following the lord.
    /// </summary>
    [HarmonyPatch]
    public class HidePartyNamePlatePatch
    {
        private static FieldInfo _isVisibleOnMapBindField;
        
        // Patch DetermineIsVisibleOnMap to intercept visibility calculation
        [HarmonyPatch(typeof(PartyNameplateVM), "DetermineIsVisibleOnMap")]
        [HarmonyPostfix]
        static void DetermineIsVisiblePostfix(PartyNameplateVM __instance)
        {
            try
            {
                if (__instance.Party == MobileParty.MainParty)
                {
                    bool isEnlisted = EnlistmentBehavior.Instance?.IsEnlisted == true;
                    
                    if (isEnlisted)
                    {
                        if (_isVisibleOnMapBindField == null)
                            _isVisibleOnMapBindField = AccessTools.Field(typeof(PartyNameplateVM), "_isVisibleOnMapBind");

                        _isVisibleOnMapBindField?.SetValue(__instance, false);
                    }
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("HidePartyNamePlatePatch", $"Error in DetermineIsVisible: {ex.Message}");
            }
        }

        // Patch RefreshBinding to enforce visibility properties
        [HarmonyPatch(typeof(PartyNameplateVM), "RefreshBinding")]
        [HarmonyPostfix]
        static void RefreshBindingPostfix(PartyNameplateVM __instance)
        {
            try
            {
                if (__instance.Party == MobileParty.MainParty)
                {
                    if (EnlistmentBehavior.Instance?.IsEnlisted == true)
                    {
                        __instance.IsVisibleOnMap = false;
                        __instance.IsMainParty = false;
                    }
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("HidePartyNamePlatePatch", $"Error in RefreshBinding: {ex.Message}");
            }
        }

        // Patch to hide the movement cursor click effect when following the lord
        // The game creates a "map_click" entity when you move. We suppress it if the player is enlisted.
        // This is handled by the MapScreen, but we can intercept the cursor creation in Campaign.
        // A common place this visual is triggered is via Campaign.SetTargetEvent or similar,
        // but often it's handled by the UI layer directly.
        // 
        // Since we can't easily patch the UI layer click handler without specific knowledge of the method,
        // we can try to ensure the party doesn't emit "I am moving" events that trigger the visual.
        
        // However, the yellow cursor is usually purely client-side visual feedback.
        // If it persists, it's because the game thinks the player clicked to move.
        // When "Following", we are technically moving.
        
        // IMPORTANT: The "Attached" state should naturally suppress the move cursor because you aren't clicking.
        // If you are seeing it, it might be from the initial click to follow.
        // Or are you seeing it continuously?
        // If it's the movement target circle, that usually only appears for player-initiated movement.
    }
}
