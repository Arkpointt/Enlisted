using System;
using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using TaleWorlds.CampaignSystem.GameComponents;
using Enlisted.Features.Combat.Behaviors;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Mod.Core;
using Enlisted.Mod.Core.Logging;

namespace Enlisted.Mod.GameAdapters.Patches
{
    /// <summary>
    /// Patches the native GetGenericStateMenu() to return our custom menu when player is in reserve.
    /// 
    /// Root cause: When in reserve mode, the native system thinks player should be in "army_wait"
    /// because mainParty.AttachedTo != null. Various native systems call GetGenericStateMenu() and
    /// switch menus accordingly, causing visual stutter as menus flip between army_wait and enlisted_battle_wait.
    /// 
    /// Fix: If player is waiting in reserve, override the result to return "enlisted_battle_wait".
    /// This prevents ALL native systems from trying to switch away from our menu.
    /// </summary>
    [HarmonyPatch(typeof(DefaultEncounterGameMenuModel), nameof(DefaultEncounterGameMenuModel.GetGenericStateMenu))]
    [SuppressMessage("ReSharper", "UnusedType.Global", Justification = "Harmony patch - applied via attribute")]
    public class GenericStateMenuPatch
    {
        /// <summary>
        /// Postfix that overrides the result when player is waiting in reserve.
        /// Returns "enlisted_battle_wait" instead of "army_wait" to prevent menu switching stutter.
        /// </summary>
        [HarmonyPostfix]
        [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Harmony convention: __result is a special injected parameter")]
        [SuppressMessage("CodeQuality", "IDE0051", Justification = "Called by Harmony via reflection")]
        [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Called by Harmony via reflection")]
        public static void Postfix(ref string __result)
        {
            try
            {
                // Check activation gate first - mod may be disabled
                if (!EnlistedActivation.EnsureActive())
                {
                    return;
                }
                
                // Only intercept when player is waiting in reserve
                if (!EnlistedEncounterBehavior.IsWaitingInReserve)
                {
                    return;
                }
                
                // Check if actually enlisted - if not, reserve flag is stale
                var enlistment = EnlistmentBehavior.Instance;
                var isEnlisted = enlistment?.IsEnlisted == true;
                
                if (!isEnlisted)
                {
                    // Reserve flag is stale - clear it and don't intercept
                    ModLogger.Debug("GenericStateMenuPatch", "Clearing stale reserve flag during menu check");
                    EnlistedEncounterBehavior.ClearReserveState();
                    return;
                }
                
                // Check if battle is actually over - if so, DON'T override
                // This allows the tick handler to detect battle end via genericStateMenu
                var lordParty = enlistment?.CurrentLord?.PartyBelongedTo;
                var mapEvent = lordParty?.Party?.MapEvent;
                
                // Battle is over when: no MapEvent, OR MapEvent has a winner, OR lord party is gone
                var battleOver = mapEvent == null || mapEvent.HasWinner || lordParty == null || !lordParty.IsActive;
                
                if (battleOver)
                {
                    // Battle is over - don't override, let native menu system take over
                    return;
                }
                
                // When in reserve AND battle is ongoing, override to our custom menu
                if (__result == "army_wait" || __result == "army_wait_at_settlement" || __result == "encounter")
                {
                    __result = "enlisted_battle_wait";
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("GenericStateMenuPatch", $"Error in GetGenericStateMenu patch: {ex.Message}");
            }
        }
    }
}
