using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Library;
using Enlisted.Utils;

namespace Enlisted.Services
{
    /// <summary>
    /// Concrete army-related helpers. Kept static to simplify call sites and to match
    /// existing code paths. Interface adapters in Services.Impl provide DI-friendly wrappers.
    /// </summary>
    public static class ArmyService
    {
        /// <summary>
        /// Sets up simple escort following behavior with the commander.
        /// No army integration - just escort AI and UX messaging.
        /// </summary>
        /// <param name="commander">Commander hero to follow.</param>
        /// <returns>True if follow behavior was configured.</returns>
        public static bool TryJoinCommandersArmy(Hero commander)
        {
            var main = MobileParty.MainParty;
            var commanderParty = commander?.PartyBelongedTo;
            
            if (main == null || commanderParty == null)
            {
                InformationManager.DisplayMessage(new InformationMessage(Constants.Messages.COULD_NOT_FIND_PARTIES));
                return false;
            }

            // Set escort behavior; do not alter army structures
            main.Ai.SetMoveEscortParty(commanderParty);
            
            InformationManager.DisplayMessage(new InformationMessage(string.Format(Constants.Messages.ENLISTED_SUCCESS, commander.Name)));
            return true;
        }

        /// <summary>
        /// Leave escort service by resetting AI to a neutral state.
        /// </summary>
        public static void LeaveCurrentArmy()
        {
            var main = MobileParty.MainParty;
            if (main != null)
            {
                // Reset AI behavior to independent operation
                main.Ai.SetInitiative(1f, 1f, 24f);
                main.Ai.SetMoveModeHold();
            }
        }

        /// <summary>
        /// Fail-safe detachment used by cleanups and hourly checks.
        /// </summary>
        public static void SafeDetach()
        {
            var main = MobileParty.MainParty;
            if (main != null)
            {
                main.Ai.SetInitiative(1f, 1f, 24f);
                main.Ai.SetMoveModeHold();
            }
        }
    }
}
