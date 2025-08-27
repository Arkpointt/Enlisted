using HarmonyLib;
using System;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.Core;
using TaleWorlds.Library;
using Enlisted.Features.Enlistment.Application;
using Enlisted.Core.Logging;
using Enlisted.Core.DependencyInjection;

namespace Enlisted.GameAdapters.Patches
{
    // Harmony Patch
    // Target: TaleWorlds.CampaignSystem.CampaignEventDispatcher.OnMapEventStarted(MapEvent, PartyBase, PartyBase)
    // Why: Auto-join the commander's battles when the player is enlisted, maintaining enlistment narrative flow
    // Safety: Campaign-only; null-check commander/player parties; skip if player already in encounter; fail closed on exceptions
    // Notes: Logs at Info; no allocations on hot path beyond message formatting; gated by enlistment state
    [HarmonyPatch]
    public static class BattleParticipationPatch
    {
        private static ILoggingService _logger;

        static BattleParticipationPatch()
        {
            // Try to get logger during static initialization
            ServiceLocator.TryGetService<ILoggingService>(out _logger);
        }

        public static MethodBase TargetMethod()
        {
            try
            {
                var type = AccessTools.TypeByName("TaleWorlds.CampaignSystem.CampaignEventDispatcher");
                if (type == null)
                {
                    LogPatchError("Could not find CampaignEventDispatcher type");
                    return null;
                }

                // The method is an instance override method with specific signature
                var method = AccessTools.Method(type, "OnMapEventStarted", new[] { typeof(MapEvent), typeof(PartyBase), typeof(PartyBase) });
                if (method == null)
                {
                    LogPatchError("Could not find OnMapEventStarted method with correct signature");
                    return null;
                }

                LogPatchSuccess("Successfully found CampaignEventDispatcher.OnMapEventStarted");
                return method;
            }
            catch (Exception ex)
            {
                LogPatchError($"Exception finding BattleParticipation patch target: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Intercepts battle start events to automatically join commander's battles.
        /// Uses game's encounter system to properly add player to the battle.
        /// Fails safely if encounter setup cannot complete.
        /// </summary>
        [HarmonyPostfix]
        private static void Postfix(MapEvent mapEvent, PartyBase attackerParty, PartyBase defenderParty)
        {
            try
            {
                // Get enlistment service through dependency injection
                if (!TryGetEnlistmentService(out var enlistmentService) || 
                    !enlistmentService.IsEnlisted || 
                    enlistmentService.Commander == null)
                    return;

                var commanderParty = enlistmentService.Commander.PartyBelongedTo?.Party;
                if (commanderParty == null) return;

                // Determine commander's role in this battle
                bool commanderIsAttacker = (attackerParty == commanderParty);
                bool commanderIsDefender = (defenderParty == commanderParty);
                if (!commanderIsAttacker && !commanderIsDefender) return;

                var playerParty = MobileParty.MainParty;
                // Safety: Don't interfere if player already in battle or encounter
                if (playerParty.MapEvent != null || PlayerEncounter.Current != null) return;

                LogBattleJoin(commanderIsAttacker ? "attacking" : "defending");

                // Join battle on commander's side using game's encounter system
                if (commanderIsAttacker)
                {
                    EncounterManager.StartPartyEncounter(playerParty.Party, defenderParty);
                    ShowBattleMessage($"Attacking with {enlistmentService.Commander.Name}!");
                }
                else
                {
                    EncounterManager.StartPartyEncounter(attackerParty, playerParty.Party);
                    ShowBattleMessage($"Defending with {enlistmentService.Commander.Name}!");
                }
            }
            catch (Exception ex)
            {
                // Fail closed: Log error but don't crash the game
                LogBattleError("Could not join battle", ex);
            }
        }

        /// <summary>
        /// Helper method to get enlistment service through dependency injection.
        /// Falls back to static instance during transition period.
        /// </summary>
        private static bool TryGetEnlistmentService(out IEnlistmentService enlistmentService)
        {
            // Try dependency injection first (ADR-004 pattern)
            if (ServiceLocator.TryGetService<IEnlistmentService>(out enlistmentService))
            {
                return true;
            }

            // Fallback to static instance during transition
            if (EnlistmentBehavior.Instance != null)
            {
                enlistmentService = EnlistmentBehavior.Instance;
                return true;
            }

            enlistmentService = null;
            return false;
        }

        /// <summary>
        /// Log battle participation using centralized logging service.
        /// </summary>
        private static void LogBattleJoin(string role)
        {
            if (_logger != null)
            {
                _logger.LogInfo(LogCategories.GameAdapters, "Auto-joining commander's battle as {0}", role);
            }
            else
            {
                Debug.Print($"[Enlisted] Auto-joining commander's battle as {role}");
            }
        }

        /// <summary>
        /// Log battle participation errors using centralized logging service.
        /// </summary>
        private static void LogBattleError(string message, Exception ex)
        {
            if (_logger != null)
            {
                _logger.LogError(LogCategories.GameAdapters, message, ex);
            }
            else
            {
                // Fallback during transition period
                Debug.Print($"[Enlisted] {message}: {ex.Message}");
            }
        }

        /// <summary>
        /// Show battle participation message to player.
        /// </summary>
        private static void ShowBattleMessage(string message)
        {
            if (_logger != null)
            {
                _logger.ShowPlayerMessage($"[Enlisted] {message}");
            }
            else
            {
                // Fallback during transition period
                InformationManager.DisplayMessage(new InformationMessage($"[Enlisted] {message}"));
            }
        }

        /// <summary>
        /// Log patch success using centralized logging service.
        /// </summary>
        private static void LogPatchSuccess(string message)
        {
            if (_logger != null)
            {
                _logger.LogInfo(LogCategories.GameAdapters, message);
            }
            else
            {
                Debug.Print($"[Enlisted] PATCH SUCCESS: {message}");
            }
        }

        /// <summary>
        /// Log patch errors using centralized logging service.
        /// </summary>
        private static void LogPatchError(string message)
        {
            if (_logger != null)
            {
                _logger.LogError(LogCategories.GameAdapters, message, null);
            }
            else
            {
                Debug.Print($"[Enlisted] PATCH ERROR: {message}");
            }
        }
    }
}
