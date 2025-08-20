using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.Core;
using TaleWorlds.Library;
using Enlisted.Behaviors;

namespace Enlisted.Patches
{
    /// <summary>
    /// Auto-join commander battles. Patch listens to OnMapEventStarted and, if enlisted,
    /// triggers an encounter that adds the player on the correct side.
    /// Keep logic thin and wrap in try/catch to avoid crashing the game.
    /// </summary>
    [HarmonyPatch]
    public static class BattleParticipationPatch
    {
        public static System.Reflection.MethodBase TargetMethod()
        {
            var campaignEventDispatcherType = typeof(CampaignEventDispatcher);
            return campaignEventDispatcherType.GetMethod("OnMapEventStarted", 
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        }

        private static void Postfix(MapEvent mapEvent, PartyBase attackerParty, PartyBase defenderParty)
        {
            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment == null || !enlistment.IsEnlisted || enlistment.Commander == null)
                return;

            var commanderParty = enlistment.Commander.PartyBelongedTo?.Party;
            if (commanderParty == null) return;

            bool commanderIsAttacker = (attackerParty == commanderParty);
            bool commanderIsDefender = (defenderParty == commanderParty);
            if (!commanderIsAttacker && !commanderIsDefender) return;

            var playerParty = MobileParty.MainParty;
            if (playerParty.MapEvent != null || PlayerEncounter.Current != null) return;

            try
            {
                if (commanderIsAttacker)
                {
                    EncounterManager.StartPartyEncounter(playerParty.Party, defenderParty);
                    InformationManager.DisplayMessage(new InformationMessage(
                        $"[Enlisted] Attacking with {enlistment.Commander.Name}!"));
                }
                else
                {
                    EncounterManager.StartPartyEncounter(attackerParty, playerParty.Party);
                    InformationManager.DisplayMessage(new InformationMessage(
                        $"[Enlisted] Defending with {enlistment.Commander.Name}!"));
                }
            }
            catch (System.Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    $"[Enlisted] Could not join battle: {ex.Message}"));
            }
        }
    }
}
