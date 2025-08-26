using HarmonyLib;
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
    // Why: Auto-join the commander’s battles when the player is enlisted, maintaining enlistment narrative flow
    // Safety: Campaign-only; null-check commander/player parties; skip if player already in encounter; fail closed on exceptions
    // Notes: Logs at Info; no allocations on hot path beyond message formatting; gated by enlistment state
    [HarmonyPatch]
    public static class BattleParticipationPatch
    {
        public static System.Reflection.MethodBase TargetMethod()
        {
            var campaignEventDispatcherType = typeof(CampaignEventDispatcher);
            return campaignEventDispatcherType.GetMethod("OnMapEventStarted", 
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        }

        /// <summary>
        /// Intercepts battle start events to automatically join commander's battles.
        /// Uses game's encounter system to properly add player to the battle.
        /// Fails safely if encounter setup cannot complete.
        /// </summary>
        private static void Postfix(MapEvent mapEvent, PartyBase attackerParty, PartyBase defenderParty)
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

            try
            {
                LogBattleJoin(commanderIsAttacker ? "attacking" : "defending");

                // Join battle on commander's side using game's encounter system
                if (commanderIsAttacker)
                {
                    EncounterManager.StartPartyEncounter(playerParty.Party, defenderParty);
                    ShowBattleMessage($"Attacking with {enlistmentService.Commander.Name}!",
                        LogCategories.GameAdapters);
                }
                else
                {
                    EncounterManager.StartPartyEncounter(attackerParty, playerParty.Party);
                    ShowBattleMessage($"Defending with {enlistmentService.Commander.Name}!",
                        LogCategories.GameAdapters);
                }
            }
            catch (System.Exception ex)
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
            if (ServiceLocator.TryGetService<ILoggingService>(out var logger))
            {
                logger.LogInfo(LogCategories.GameAdapters, "Auto-joining commander's battle as {0}", role);
            }
        }

        /// <summary>
        /// Log battle participation errors using centralized logging service.
        /// </summary>
        private static void LogBattleError(string message, System.Exception ex)
        {
            if (ServiceLocator.TryGetService<ILoggingService>(out var logger))
            {
                logger.LogError(LogCategories.GameAdapters, message, ex);
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
        private static void ShowBattleMessage(string message, string category)
        {
            if (ServiceLocator.TryGetService<ILoggingService>(out var logger))
            {
                logger.ShowPlayerMessage($"[Enlisted] {message}");
            }
            else
            {
                // Fallback during transition period
                InformationManager.DisplayMessage(new InformationMessage($"[Enlisted] {message}"));
            }
        }
    }
}
