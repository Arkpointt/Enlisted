using System;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Encounters;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Mod.Core.Logging;

namespace Enlisted.Mod.GameAdapters.Patches
{
    /// <summary>
    /// Prevents the native game from making the player party visible when enlisted.
    /// When other NPCs approach, the game sets IsVisible = true for encounters,
    /// but enlisted players should stay invisible (except during battles/sieges).
    /// This patch intercepts visibility changes and blocks them for enlisted players.
    /// </summary>
    [HarmonyPatch(typeof(MobileParty), "IsVisible", MethodType.Setter)]
    public class VisibilityEnforcementPatch
    {
        /// <summary>
        /// Prefix that prevents visibility from being set to true for enlisted players.
        /// Returns false to block the setter, true to allow normal visibility.
        /// </summary>
        static bool Prefix(MobileParty __instance, bool value)
        {
            try
            {
                // Only check for main party
                if (__instance != MobileParty.MainParty)
                {
                    return true; // Allow normal visibility for other parties
                }
                
                // Only check when trying to make visible (value = true)
                if (!value)
                {
                    return true; // Always allow making invisible
                }
                
                var enlistment = EnlistmentBehavior.Instance;
                
                bool isEnlisted = enlistment?.IsEnlisted == true;
                bool onLeave = enlistment?.IsOnLeave == true;
                bool inGrace = enlistment?.IsInDesertionGracePeriod == true;

                // Fresh campaigns, leave, or grace periods should behave like vanilla.
                if (!isEnlisted || onLeave || inGrace)
                {
                    ModLogger.Debug("VisibilityEnforcement", "Allowing visibility - service paused (not enlisted / on leave / grace period)");
                    return true;
                }

                var mainParty = MobileParty.MainParty;
                if (mainParty == null)
                {
                    return true;
                }

                bool playerEncounter = PlayerEncounter.Current != null;
                bool playerBattle = mainParty.Party?.MapEvent != null || mainParty.Party?.SiegeEvent != null;
                bool playerActive = mainParty.IsActive;
                bool embeddedWithLord = enlistment.IsEmbeddedWithLord();

                // When the native game needs us visible (battle, encounter, detached travel) we must allow it,
                // otherwise the encounter system loops and eventually asserts (rglSkeleton.cpp:1197).
                if (playerEncounter || playerBattle || !embeddedWithLord || playerActive && mainParty.AttachedTo == null)
                {
                    ModLogger.Debug("VisibilityEnforcement", "Allowing visibility - native encounter/battle requires it");
                    return true;
                }

                // Otherwise we're still tucked inside the lord's escort, keep the party hidden.
                ModLogger.Debug("VisibilityEnforcement", "Blocked visibility change - player remains embedded with lord");
                return false;
            }
            catch (Exception ex)
            {
                ModLogger.Error("VisibilityEnforcement", $"Error in visibility enforcement patch: {ex.Message}");
                return true; // Fail open - allow visibility if we can't determine state
            }
        }
    }
}

