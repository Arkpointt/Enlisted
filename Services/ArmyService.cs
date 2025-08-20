using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Library;
using Enlisted.Utils;

namespace Enlisted.Services
{
    /// <summary>
    /// Handles escort operations for following a commander while remaining invisible.
    /// Creates the illusion of being part of their party without actual army integration.
    /// This is purely about following behavior, not joining armies.
    /// </summary>
    public static class ArmyService
    {
        /// <summary>
        /// Sets up simple escort following behavior with the commander.
        /// No army integration - just pure following illusion.
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

            // Simply set escort behavior - no army joining at all
            main.Ai.SetMoveEscortParty(commanderParty);
            
            InformationManager.DisplayMessage(new InformationMessage(string.Format(Constants.Messages.ENLISTED_SUCCESS, commander.Name)));
            return true;
        }

        /// <summary>
        /// Leaves escort service by resetting AI to normal behavior.
        /// No army operations needed since we never joined an army.
        /// </summary>
        public static void LeaveCurrentArmy()
        {
            var main = MobileParty.MainParty;
            if (main != null)
            {
                // Reset AI behavior to normal independent operation
                main.Ai.SetInitiative(1f, 1f, 24f);
                main.Ai.SetMoveModeHold();
            }
        }

        /// <summary>
        /// Safely detaches from escort behavior.
        /// Only resets AI since we're not in any actual army.
        /// </summary>
        public static void SafeDetach()
        {
            var main = MobileParty.MainParty;
            if (main != null)
            {
                // Reset AI behavior to independent operation
                main.Ai.SetInitiative(1f, 1f, 24f);
                main.Ai.SetMoveModeHold();
            }
        }
    }
}