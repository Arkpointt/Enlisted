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
    /// Harmony patch to automatically join battles when the enlisted commander enters combat.
    /// When enlisted, the player should automatically participate in any battle their commander is involved in,
    /// simulating being part of their military formation.
    /// 
    /// This patch monitors battle events and automatically creates an encounter to join the commander's battles.
    /// </summary>
    [HarmonyPatch]
    public static class BattleParticipationPatch
    {
        /// <summary>
        /// Target the MapEventStarted event dispatcher method.
        /// This allows us to intercept when battles begin.
        /// </summary>
        public static System.Reflection.MethodBase TargetMethod()
        {
            // Find the method that fires MapEventStarted events
            var campaignEventDispatcherType = typeof(CampaignEventDispatcher);
            return campaignEventDispatcherType.GetMethod("OnMapEventStarted", 
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        }

        /// <summary>
        /// Postfix patch to join battles after they start involving our commander.
        /// Uses EncounterManager.StartPartyEncounter to join existing battles.
        /// </summary>
        /// <param name="mapEvent">The battle that started</param>
        /// <param name="attackerParty">The attacking party</param>
        /// <param name="defenderParty">The defending party</param>
        private static void Postfix(MapEvent mapEvent, PartyBase attackerParty, PartyBase defenderParty)
        {
            // Check if we're enlisted
            var enlistmentBehavior = EnlistmentBehavior.Instance;
            if (enlistmentBehavior == null || !enlistmentBehavior.IsEnlisted || enlistmentBehavior.Commander == null)
                return;

            var commanderParty = enlistmentBehavior.Commander.PartyBelongedTo?.Party;
            if (commanderParty == null) return;

            // Check if our commander is involved in this battle
            bool commanderIsAttacker = (attackerParty == commanderParty);
            bool commanderIsDefender = (defenderParty == commanderParty);
            
            if (!commanderIsAttacker && !commanderIsDefender) return;

            // Check if we're already in a battle or encounter
            var playerParty = MobileParty.MainParty;
            if (playerParty.MapEvent != null || PlayerEncounter.Current != null) return;

            try
            {
                // Start an encounter that will automatically join us to the existing battle
                // The game's encounter system will handle adding us to the correct side
                if (commanderIsAttacker)
                {
                    // Commander is attacking, so we also attack the defender
                    EncounterManager.StartPartyEncounter(playerParty.Party, defenderParty);
                    InformationManager.DisplayMessage(new InformationMessage(
                        $"[Enlisted] Attacking with {enlistmentBehavior.Commander.Name}!"));
                }
                else
                {
                    // Commander is defending, so we help defend against the attacker
                    // This is trickier - we need to be on the defender's side
                    EncounterManager.StartPartyEncounter(attackerParty, playerParty.Party);
                    InformationManager.DisplayMessage(new InformationMessage(
                        $"[Enlisted] Defending with {enlistmentBehavior.Commander.Name}!"));
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