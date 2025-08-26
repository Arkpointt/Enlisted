using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Library;
using Enlisted.Core.Constants;

namespace Enlisted.Features.Enlistment.Infrastructure
{
    /// <summary>
    /// Infrastructure service for army integration operations.
    /// Handles TaleWorlds army mechanics while isolating game API interactions.
    /// Provides safe escort behavior without complex army integration.
    /// </summary>
    public static class ArmyIntegrationService
    {
        /// <summary>
        /// Sets up escort following behavior with the commander.
        /// Uses simple escort AI without complex army integration to avoid conflicts.
        /// Safer approach that maintains narrative without touching army structures.
        /// </summary>
        public static bool TryJoinCommandersArmy(Hero commander)
        {
            var main = MobileParty.MainParty;
            var commanderParty = commander?.PartyBelongedTo;
            
            if (main == null || commanderParty == null)
            {
                InformationManager.DisplayMessage(new InformationMessage(Constants.Messages.COULD_NOT_FIND_PARTIES));
                return false;
            }

            // Set escort behavior; safer than army manipulation
            main.Ai.SetMoveEscortParty(commanderParty);
            
            InformationManager.DisplayMessage(new InformationMessage(
                string.Format(Constants.Messages.ENLISTED_SUCCESS, commander.Name)));
            return true;
        }

        /// <summary>
        /// Leave escort service by resetting AI to neutral state.
        /// Clean separation from commander without army complications.
        /// </summary>
        public static void LeaveCurrentArmy()
        {
            var main = MobileParty.MainParty;
            if (main != null)
            {
                // Reset AI to independent operation
                main.Ai.SetInitiative(1f, 1f, 24f);
                main.Ai.SetMoveModeHold();
            }
        }

        /// <summary>
        /// Emergency detachment for cleanup and error recovery.
        /// Ensures player party can operate independently.
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
