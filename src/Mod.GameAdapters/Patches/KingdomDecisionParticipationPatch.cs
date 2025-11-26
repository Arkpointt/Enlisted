using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Election;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Mod.Core.Logging;

namespace Enlisted.Mod.GameAdapters.Patches
{
    /// <summary>
    /// Keeps enlisted players out of kingdom decision participation while they are embedded with a lord.
    /// This mirrors vanilla behavior for mercenaries so kingdom menus never show up during service.
    /// </summary>
    public static class KingdomDecisionParticipationPatch
    {
        private static bool ShouldSuppressParticipation()
        {
            // Skip during character creation when campaign isn't initialized
            if (Campaign.Current == null)
            {
                return false;
            }
            
            var enlistment = EnlistmentBehavior.Instance;
            return enlistment?.IsEmbeddedWithLord() == true && enlistment?.IsOnLeave != true;
        }

        [HarmonyPatch(typeof(KingdomDecision), "get_IsPlayerParticipant")]
        private static class IsPlayerParticipantPatch
        {
            private static void Postfix(ref bool __result)
            {
                if (__result && ShouldSuppressParticipation())
                {
                    __result = false;
                    ModLogger.Debug("Voting", "Suppressed player participation flag while enlisted.");
                }
            }
        }

        [HarmonyPatch(typeof(KingdomDecision), nameof(KingdomDecision.DetermineSupporters))]
        private static class DetermineSupportersPatch
        {
            private static void Postfix(ref IEnumerable<Supporter> __result)
            {
                if (!ShouldSuppressParticipation() || __result == null)
                {
                    return;
                }
                
                // Skip during character creation when player clan isn't initialized
                if (Clan.PlayerClan == null)
                {
                    return;
                }

                __result = __result.Where(s => s?.Clan != Clan.PlayerClan);
            }
        }
    }
}

