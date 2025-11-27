using System;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Encounters;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Mod.Core;
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
        // Force-hide state: when true, blocks ALL visibility changes (overrides everything)
        // This is set after forced settlement exit to prevent race conditions
        private static bool _forceHidden = false;
        private static float _forceHiddenUntil = 0f;
        
        /// <summary>
        /// Call this immediately BEFORE forcing player out of a settlement.
        /// Ensures visibility is blocked even during the transition frames.
        /// </summary>
        public static void BeginForceHidden()
        {
            _forceHidden = true;
            // Force hidden for 2 seconds to cover any transition delays
            _forceHiddenUntil = Campaign.CurrentTime + (2f / 24f / 60f); // 2 seconds in campaign time
            ModLogger.Debug("VisibilityEnforcement", "Force hidden mode ENABLED for settlement exit");
        }
        
        /// <summary>
        /// Call this after hiding is complete and escort is re-established.
        /// </summary>
        public static void EndForceHidden()
        {
            _forceHidden = false;
            ModLogger.Debug("VisibilityEnforcement", "Force hidden mode DISABLED");
        }
        
        /// <summary>
        /// Prefix that prevents visibility from being set to true for enlisted players.
        /// Returns false to block the setter, true to allow normal visibility.
        /// </summary>
        static bool Prefix(MobileParty __instance, bool value)
        {
            try
            {
                // Skip during character creation - use safe guard to prevent crashes
                var mainParty = CampaignSafetyGuard.SafeMainParty;
                if (mainParty == null)
                {
                    return true;
                }
                
                // Only check for main party
                if (__instance != mainParty)
                {
                    return true; // Allow normal visibility for other parties
                }
                
                // Only check when trying to make visible (value = true)
                if (!value)
                {
                    return true; // Always allow making invisible
                }
                
                // FORCE HIDDEN CHECK: Overrides everything during settlement exit transition
                // This prevents race conditions where CurrentSettlement/PlayerEncounter aren't cleared yet
                if (_forceHidden)
                {
                    // Auto-expire after 2 seconds in case EndForceHidden() wasn't called
                    if (Campaign.CurrentTime > _forceHiddenUntil)
                    {
                        _forceHidden = false;
                        ModLogger.Debug("VisibilityEnforcement", "Force hidden mode auto-expired");
                    }
                    else
                    {
                        ModLogger.Debug("VisibilityEnforcement", "BLOCKED by force hidden mode (settlement exit in progress)");
                        return false;
                    }
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

                bool playerEncounter = PlayerEncounter.Current != null;
                bool playerBattle = mainParty.Party?.MapEvent != null || mainParty.Party?.SiegeEvent != null;
                bool embeddedWithLord = enlistment.IsEmbeddedWithLord();
                
                // Check if lord or their army is in battle - if so, we MUST allow visibility for encounter system
                var lordParty = enlistment.CurrentLord?.PartyBelongedTo;
                bool lordInBattle = lordParty?.Party?.MapEvent != null || lordParty?.Party?.SiegeEvent != null;
                // Army battles: the army leader's party holds the MapEvent, not necessarily each member's party
                bool armyInBattle = lordParty?.Army?.LeaderParty?.Party?.MapEvent != null || lordParty?.Army?.LeaderParty?.Party?.SiegeEvent != null;
                bool anyBattleActive = lordInBattle || armyInBattle;
                
                // Check if player is in a settlement - if so, allow visibility for town/castle menus
                bool playerInSettlement = mainParty.CurrentSettlement != null;

                // When the native game needs us visible (battle, encounter, in settlement) we must allow it,
                // otherwise the encounter system loops and eventually asserts (rglSkeleton.cpp:1197).
                // CRITICAL: Also allow visibility when LORD or ARMY is in battle - native needs player visible to show encounter menu
                // NOTE: We removed "!embeddedWithLord" check because IsEmbeddedWithLord() can return false temporarily
                // during transitions (e.g., after settlement exit before TargetParty is set). 
                // If enlisted and not in battle/encounter/settlement, ALWAYS block visibility.
                if (playerEncounter || playerBattle || anyBattleActive || playerInSettlement)
                {
                    if (anyBattleActive && !playerBattle)
                    {
                        ModLogger.Debug("VisibilityEnforcement", $"Allowing visibility - battle active (lord:{lordInBattle}, army:{armyInBattle}), player needs encounter menu");
                    }
                    else if (playerInSettlement)
                    {
                        ModLogger.Debug("VisibilityEnforcement", "Allowing visibility - player in settlement");
                    }
                    else
                    {
                        ModLogger.Debug("VisibilityEnforcement", "Allowing visibility - native encounter/battle requires it");
                    }
                    return true;
                }

                // Otherwise we're still tucked inside the lord's escort, keep the party hidden.
                // Note: This fires frequently so we don't log it to avoid spam
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

