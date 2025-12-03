using System;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Mod.Core.Logging;

namespace Enlisted.Mod.GameAdapters.Patches
{
    /// <summary>
    ///     Harmony patch that prevents unwanted encounters from being created when the player is enlisted.
    ///     Intercepts encounter creation and checks if it involves legitimate battles with the lord's party.
    ///     This prevents random encounters while allowing battle participation when the lord fights.
    /// </summary>
    [HarmonyPatch(typeof(EncounterManager), "StartPartyEncounter")]
    public class EncounterSuppressionPatch
    {
        /// <summary>
        ///     Prefix method that runs before EncounterManager.StartPartyEncounter.
        ///     Checks if the encounter involves the enlisted player and whether it's a legitimate battle.
        ///     Returns false to prevent unwanted encounters, true to allow legitimate battles.
        ///     Called by Harmony via reflection.
        /// </summary>
        /// <param name="attackerParty">The attacking party in the encounter.</param>
        /// <param name="defenderParty">The defending party in the encounter.</param>
        /// <returns>True to allow the encounter, false to prevent it.</returns>
        [HarmonyPrefix]
        public static bool Prefix(PartyBase attackerParty, PartyBase defenderParty)
        {
            try
            {
                // Check if player is enlisted or just ended enlistment (grace period)
                var enlistment = EnlistmentBehavior.Instance;
                var isEnlisted = enlistment?.IsEnlisted == true;
                var hasGraceProtection = enlistment?.HasActiveGraceProtection == true;

                // CRITICAL: Also suppress encounters if player just ended enlistment and is still in a MapEvent OR PlayerEncounter
                // This prevents crashes when player becomes "attackable" right after army defeat
                // Player might be in a surrender menu (PlayerEncounter) even if MapEvent is null
                var mainParty = MobileParty.MainParty?.Party;
                var playerInMapEvent = mainParty?.MapEvent != null;
                var playerInEncounter = PlayerEncounter.Current != null;
                var playerInBattleState = playerInMapEvent || playerInEncounter;
                var justEndedEnlistment = !isEnlisted && playerInBattleState;

                // Check if this encounter involves the main party
                if (mainParty == null)
                {
                    return true; // Allow normal encounter creation
                }

                // Check if the main party is involved in this encounter
                var mainPartyInvolved = attackerParty == mainParty || defenderParty == mainParty;
                if (!mainPartyInvolved)
                {
                    return true; // Allow normal encounter creation
                }

                // CRITICAL: Suppress encounters if enlistment just ended and player is still in MapEvent/Encounter
                // This prevents crashes when clicking "Surrender" after army defeat
                if (justEndedEnlistment)
                {
                    ModLogger.Warn("Encounter",
                        $"Suppressed encounter - enlistment just ended and player still in battle state (MapEvent: {playerInMapEvent}, Encounter: {playerInEncounter})");
                    return false; // Prevent encounter until battle state clears
                }

                if (hasGraceProtection)
                {
                    ModLogger.Debug("Encounter", "Suppressed encounter - grace protection window active");
                    return false;
                }

                // If not enlisted and not in grace period, allow normal encounters
                if (!isEnlisted)
                {
                    return true; // Allow normal encounter creation
                }

                // CRITICAL: Prevent encounters with the lord's party itself
                // When the player is positioned at the lord's location and activated,
                // the game can create an unwanted encounter with the lord's party
                var enlistedLord = enlistment!.CurrentLord;
                var lordParty = enlistedLord?.PartyBelongedTo?.Party;

                if (lordParty != null)
                {
                    // Check if this encounter is between the player and the lord's party (not a battle)
                    var isEncounterWithLord = (attackerParty == mainParty && defenderParty == lordParty) ||
                                              (attackerParty == lordParty && defenderParty == mainParty);

                    if (isEncounterWithLord)
                    {
                        // Check if the lord is in a battle - if so, this might be legitimate
                        var lordInBattle = lordParty.MapEvent != null;
                        if (!lordInBattle)
                        {
                            // CRITICAL: Allow encounters when player party is visible/active - indicates player-initiated conversation
                            // When party is invisible and attached, it's an unwanted automatic encounter
                            // When party is visible or not attached, it's likely a player-initiated conversation
                            var mainMobileParty = MobileParty.MainParty;
                            var partyVisible = mainMobileParty?.IsVisible == true;
                            var partyActive = mainMobileParty?.IsActive == true;
                            var isPlayerInitiated = partyVisible || partyActive;

                            if (isPlayerInitiated)
                            {
                                // Player party is visible/active (or the player explicitly triggered the interaction)
                                // Allow the encounter so the player can talk to the lord while remaining hidden on the map.
                                ModLogger.Debug("EncounterSuppression",
                                    $"Allowing player-initiated encounter with lord's party (Lord: {enlistedLord!.Name}, Visible: {partyVisible}, Active: {partyActive})");
                                return true;
                            }

                            // Party is invisible and inactive - this is an unwanted automatic encounter
                            // Suppress it to prevent "You have encountered a lord's army" menu
                            ModLogger.Debug("EncounterSuppression",
                                $"Suppressed unwanted encounter with lord's party (Lord: {enlistedLord.Name})");
                            return false;
                        }
                    }

                    // Check if this is a legitimate battle involving the lord's party or army
                    var lordInvolved = attackerParty == lordParty || defenderParty == lordParty;

                    if (lordInvolved && lordParty.MapEvent != null)
                    {
                        // This is a legitimate battle involving the lord's party
                        // Allow it so the player can participate in the battle
                        ModLogger.Debug("EncounterSuppression",
                            "Allowing legitimate battle involving enlisted lord's party");
                        return true;
                    }

                    // Check if the lord's army is involved in this encounter
                    var lordArmy = enlistedLord.PartyBelongedTo?.Army;
                    if (lordArmy != null)
                    {
                        var armyInvolved = attackerParty == lordArmy.LeaderParty?.Party ||
                                           defenderParty == lordArmy.LeaderParty?.Party;
                        if (armyInvolved && lordArmy.LeaderParty?.Party?.MapEvent != null)
                        {
                            ModLogger.Debug("EncounterSuppression",
                                "Allowing legitimate battle involving enlisted lord's army");
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
                ModLogger.Debug("EncounterSuppression",
                    "Suppressed unwanted encounter involving enlisted player party");
                return false;
            }
            catch (Exception ex)
            {
                ModLogger.Error("EncounterSuppression", $"Error in encounter suppression patch: {ex.Message}");
                // Fail open - allow encounter if we can't determine state
                return true;
            }
        }
    }
}
