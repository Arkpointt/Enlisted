using System;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Party;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Mod.Core.Config;
using Enlisted.Mod.Core.Logging;

namespace Enlisted.Mod.GameAdapters.Patches
{
    /// <summary>
    /// Helper method to check if a party is in battle or siege.
    /// This prevents PlayerSiege assertion failures by ensuring we don't finish encounters during sieges.
    /// </summary>
    public static class EncounterHelpers
    {
        public static bool InBattleOrSiege(MobileParty party) =>
            party?.Party.MapEvent != null || party?.Party.SiegeEvent != null || party?.BesiegedSettlement != null;
    }

    /// <summary>
    /// Harmony patch that prevents unwanted encounters from being created when the player is enlisted.
    /// Intercepts encounter creation and checks if it involves legitimate battles with the lord's party.
    /// This prevents random encounters while allowing battle participation when the lord fights.
    /// </summary>
    [HarmonyPatch(typeof(EncounterManager), "StartPartyEncounter")]
    public class EncounterSuppressionPatch
    {
        /// <summary>
        /// Prefix method that runs before EncounterManager.StartPartyEncounter.
        /// Checks if the encounter involves the enlisted player and whether it's a legitimate battle.
        /// Returns false to prevent unwanted encounters, true to allow legitimate battles.
        /// </summary>
        /// <param name="attackerParty">The attacking party in the encounter.</param>
        /// <param name="defenderParty">The defending party in the encounter.</param>
        /// <returns>True to allow the encounter, false to prevent it.</returns>
        static bool Prefix(PartyBase attackerParty, PartyBase defenderParty)
        {
            try
            {
                // Check if player is enlisted
                var enlistment = EnlistmentBehavior.Instance;
                if (enlistment?.IsEnlisted != true)
                {
                    return true; // Allow normal encounter creation
                }

                // Check if this encounter involves the main party
                var mainParty = MobileParty.MainParty?.Party;
                if (mainParty == null)
                {
                    return true; // Allow normal encounter creation
                }

                // Check if the main party is involved in this encounter
                bool mainPartyInvolved = (attackerParty == mainParty || defenderParty == mainParty);
                if (!mainPartyInvolved)
                {
                    return true; // Allow normal encounter creation
                }

                // CRITICAL: Prevent encounters with the lord's party itself
                // When the player is positioned at the lord's location and activated,
                // the game can create an unwanted encounter with the lord's party
                var enlistedLord = enlistment.CurrentLord;
                var lordParty = enlistedLord?.PartyBelongedTo?.Party;
                
                if (lordParty != null)
                {
                    // Check if this encounter is between the player and the lord's party (not a battle)
                    bool isEncounterWithLord = (attackerParty == mainParty && defenderParty == lordParty) ||
                                               (attackerParty == lordParty && defenderParty == mainParty);
                    
                    if (isEncounterWithLord)
                    {
                        // Check if the lord is in a battle - if so, this might be legitimate
                        bool lordInBattle = lordParty.MapEvent != null;
                        if (!lordInBattle)
                        {
                            // This is an unwanted encounter with the lord's party (not a battle)
                            // Suppress it to prevent "You have encountered a lord's army" menu
                            ModLogger.Debug("EncounterSuppression", $"Suppressed unwanted encounter with lord's party (Lord: {enlistedLord?.Name}, Battle: {lordInBattle})");
                            return false;
                        }
                    }
                    
                    // Check if this is a legitimate battle involving the lord's party or army
                    bool lordInvolved = (attackerParty == lordParty || defenderParty == lordParty);
                    
                    if (lordInvolved && lordParty.MapEvent != null)
                    {
                        // This is a legitimate battle involving the lord's party
                        // Allow it so the player can participate in the battle
                        ModLogger.Debug("EncounterSuppression", "Allowing legitimate battle involving enlisted lord's party");
                        return true;
                    }
                    
                    // Check if the lord's army is involved in this encounter
                    var lordArmy = enlistedLord.PartyBelongedTo?.Army;
                    if (lordArmy != null)
                    {
                        bool armyInvolved = (attackerParty == lordArmy.LeaderParty?.Party || defenderParty == lordArmy.LeaderParty?.Party);
                        if (armyInvolved && lordArmy.LeaderParty?.Party?.MapEvent != null)
                        {
                            ModLogger.Debug("EncounterSuppression", "Allowing legitimate battle involving enlisted lord's army");
                            return true;
                        }
                    }
                }

                // Check if the main party is currently active and in a battle (indicating battle participation)
                // If the party is active and has a MapEvent, it means a battle is starting
                if (MobileParty.MainParty?.IsActive == true && mainParty.MapEvent != null)
                {
                    ModLogger.Debug("EncounterSuppression", "Allowing encounter - main party is active and in battle");
                    return true;
                }

                // This appears to be an unwanted encounter (not a legitimate battle)
                // Suppress it to prevent the encounter menu from appearing
                ModLogger.Debug("EncounterSuppression", "Suppressed unwanted encounter involving enlisted player party");
                return false;
            }
            catch (Exception ex)
            {
                ModLogger.Error("EncounterSuppression", $"Error in encounter suppression patch: {ex.Message}");
                // Fail open - allow encounter if we can't determine state
                return true;
            }

#if false
            // Original logic (disabled - unreachable code):
            // Check if player is enlisted
            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment?.IsEnlisted != true)
            {
                return true; // Allow normal encounter creation
            }

            // Check if this encounter involves the main party
            var mainParty = MobileParty.MainParty?.Party;
            if (mainParty == null)
            {
                return true; // Allow normal encounter creation
            }

            // Check if the main party is involved in this encounter
            bool mainPartyInvolved = (attackerParty == mainParty || defenderParty == mainParty);
            if (!mainPartyInvolved)
            {
                return true; // Allow normal encounter creation
            }

            // Check if this is a legitimate battle involving the lord's party
            // Legitimate battles should be allowed so the player can participate
            var enlistedLord = enlistment.CurrentLord;
            var lordParty = enlistedLord?.PartyBelongedTo?.Party;
            
            if (lordParty != null)
            {
                // Check if the lord's party is involved in this encounter
                // If so, this is a legitimate battle and should be allowed
                bool lordInvolved = (attackerParty == lordParty || defenderParty == lordParty);
                
                if (lordInvolved)
                {
                    // This is a legitimate battle involving the lord's party
                    // Allow it so the player can participate in the battle
                    ModLogger.Debug("EncounterSuppression", "Allowing legitimate battle involving enlisted lord's party");
                    return true;
                }
                
                // Check if the lord's army is involved in this encounter
                // If the lord is in an army, army battles should also be allowed
                var lordArmy = enlistedLord.PartyBelongedTo?.Army;
                if (lordArmy != null)
                {
                    bool armyInvolved = (attackerParty == lordArmy.LeaderParty?.Party || defenderParty == lordArmy.LeaderParty?.Party);
                    if (armyInvolved)
                    {
                        ModLogger.Debug("EncounterSuppression", "Allowing legitimate battle involving enlisted lord's army");
                        return true;
                    }
                }
            }

            // Check if the main party is currently active (indicating it should participate in battles)
            // If the party is active, it means a battle is starting and the player should be included
            if (MobileParty.MainParty?.IsActive == true)
            {
                ModLogger.Debug("EncounterSuppression", "Allowing encounter - main party is active (battle mode)");
                return true;
            }

            // This appears to be an unwanted encounter (not a legitimate battle)
            // Suppress it to prevent the encounter menu from appearing
            // This prevents random encounters while enlisted while allowing battle participation
            ModLogger.Debug("EncounterSuppression", "Suppressed unwanted encounter involving enlisted player party");
            return false;
#endif
        }
    }
}
