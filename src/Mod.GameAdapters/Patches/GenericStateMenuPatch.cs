using System;
using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameComponents;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Mod.Core;
using Enlisted.Mod.Core.Logging;
using EnlistedEncounterBehavior = Enlisted.Features.Combat.Behaviors.EnlistedEncounterBehavior;

namespace Enlisted.Mod.GameAdapters.Patches
{
    /// <summary>
    /// Patches the native GetGenericStateMenu() to return our custom enlisted menu when appropriate.
    /// 
    /// Root cause: When enlisted, the native system thinks player should be in "army_wait" or
    /// "army_wait_at_settlement" because mainParty.AttachedTo != null. Various native systems
    /// call GetGenericStateMenu() and switch menus accordingly, causing unwanted menu switches
    /// even when the player just wants to stay in the enlisted menu.
    /// 
    /// Fix: When player is enlisted and hasn't explicitly chosen to visit a settlement,
    /// override settlement/army menus to stay on the enlisted menu instead.
    /// </summary>
    [HarmonyPatch(typeof(DefaultEncounterGameMenuModel), nameof(DefaultEncounterGameMenuModel.GetGenericStateMenu))]
    public class GenericStateMenuPatch
    {
        /// <summary>
        /// Postfix that overrides the result when player is enlisted.
        /// Returns "enlisted_status" for non-combat situations and "enlisted_battle_wait" for combat.
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
                
                // Check if actually enlisted
                var enlistment = EnlistmentBehavior.Instance;
                var isEnlisted = enlistment?.IsEnlisted == true;
                
                if (!isEnlisted)
                {
                    return;
                }
                
                // If player is waiting in reserve during battle, use the battle wait menu
                if (EnlistedEncounterBehavior.IsWaitingInReserve)
                {
                    var lordParty = enlistment?.CurrentLord?.PartyBelongedTo;
                    var mapEvent = lordParty?.Party?.MapEvent;
                    
                    // Battle is over when: no MapEvent, OR MapEvent has a winner
                    var battleOver = mapEvent == null || mapEvent.HasWinner;
                    
                    if (!battleOver)
                    {
                        // Battle is ongoing - use battle wait menu
                        if (__result == "army_wait" || __result == "army_wait_at_settlement" || __result == "encounter")
                        {
                            ModLogger.Debug("Battle", $"Menu override: {__result} -> enlisted_battle_wait (player in reserve)");
                            __result = "enlisted_battle_wait";
                        }
                        return;
                    }
                    
                    // Battle is over - clear stale reserve flag and continue to normal enlisted menu logic
                    ModLogger.Debug("GenericStateMenuPatch", "Clearing stale reserve flag - battle is over");
                    EnlistedEncounterBehavior.ClearReserveState();
                }
                
                // Check if player has explicitly clicked "Visit Settlement" from the enlisted menu.
                // If so, don't override - let them use the native settlement menus.
                // We can't rely solely on PlayerEncounter.InsideSettlement because it can be true
                // even when the lord just enters a settlement (not when player clicks Visit).
                var hasExplicitlyVisited = Enlisted.Features.Interface.Behaviors.EnlistedMenuBehavior.HasExplicitlyVisitedSettlement;
                
                if (hasExplicitlyVisited)
                {
                    // Player has explicitly clicked "Visit Settlement" - don't override
                    return;
                }
                
                // Check if there's an active settlement encounter (e.g., player paused near a castle).
                // If so, let the native settlement menu show - don't force the enlisted menu.
                // This fixes the bug where pausing at a castle shows enlisted menu instead of castle menu.
                var hasSettlementEncounter = PlayerEncounter.Current != null && 
                                            PlayerEncounter.EncounterSettlement != null;
                
                if (hasSettlementEncounter)
                {
                    // Player is in a settlement encounter (paused at castle/town) - don't override
                    // Let the native castle/town menu show
                    ModLogger.Debug("Menu", $"Settlement encounter active - allowing native menu: {__result}");
                    return;
                }
                
                // Check if lord is in a battle or siege - if so, don't override combat-related menus
                var lordPartyCheck = enlistment?.CurrentLord?.PartyBelongedTo;
                var lordInBattle = lordPartyCheck?.Party?.MapEvent != null;
                
                if (lordInBattle && __result == "encounter")
                {
                    // Lord is in battle and native wants to show encounter menu - don't override
                    // This will be handled by EnlistedMenuBehavior's battle activation logic
                    return;
                }
                
                // Override army_wait and army_wait_at_settlement to stay on enlisted menu
                // This prevents unwanted menu switches when not at a settlement
                if (__result == "army_wait" || __result == "army_wait_at_settlement")
                {
                    ModLogger.Debug("Menu", $"Menu override: {__result} -> enlisted_status (keeping enlisted menu)");
                    __result = "enlisted_status";
                }
            }
            catch (Exception ex)
            {
                ModLogger.ErrorCode("GenericStateMenuPatch", "E-PATCH-001", "Error in GetGenericStateMenu patch", ex);
            }
        }
    }
}
