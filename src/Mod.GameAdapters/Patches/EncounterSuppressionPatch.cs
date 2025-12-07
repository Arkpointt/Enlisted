using System;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Party;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Mod.Core;
using Enlisted.Mod.Core.Logging;
using EnlistedEncounterBehavior = Enlisted.Features.Combat.Behaviors.EnlistedEncounterBehavior;

namespace Enlisted.Mod.GameAdapters.Patches
{
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
        /// Called by Harmony via reflection.
        /// </summary>
        /// <param name="attackerParty">The attacking party in the encounter.</param>
        /// <param name="defenderParty">The defending party in the encounter.</param>
        /// <returns>True to allow the encounter, false to prevent it.</returns>
        [HarmonyPrefix]
        public static bool Prefix(PartyBase attackerParty, PartyBase defenderParty)
        {
            try
            {
                if (!EnlistedActivation.EnsureActive())
                {
                    return true;
                }

                // Check if player is enlisted or just ended enlistment (grace period)
                var enlistment = EnlistmentBehavior.Instance;
                var isEnlisted = enlistment?.IsEnlisted == true;
                var hasGraceProtection = enlistment?.HasActiveGraceProtection == true;
                
                // Check if player just ended enlistment and is still in a MapEvent/Encounter
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

                var mainPartyInvolved = attackerParty == mainParty || defenderParty == mainParty;
                if (!mainPartyInvolved)
                {
                    return true; // Allow normal encounter creation
                }
                
                // If player is already a prisoner, allow encounters (captor movement)
                if (Hero.MainHero?.IsPrisoner == true)
                {
                    return true;
                }
                
                // Block encounters when waiting in reserve AND still enlisted
                // Only block if actively enlisted - if enlistment ended, clear stale flag and allow
                var isInReserve = EnlistedEncounterBehavior.IsWaitingInReserve;
                if (isInReserve && isEnlisted)
                {
                    ModLogger.Debug("EncounterSuppression", "Suppressed player encounter - waiting in reserve");
                    return false;
                }
                
                // If reserve flag is stale (enlistment ended but flag wasn't cleared), clear it
                if (isInReserve && !isEnlisted)
                {
                    ModLogger.Debug("EncounterSuppression", "Clearing stale reserve flag during encounter creation");
                    EnlistedEncounterBehavior.ClearReserveState();
                }
                
                // Suppress encounters if enlistment just ended and player is still in MapEvent/Encounter
                if (justEndedEnlistment)
                {
                    ModLogger.Warn("Encounter", $"Suppressed encounter - enlistment just ended and player still in battle state (MapEvent: {playerInMapEvent}, Encounter: {playerInEncounter})");
                    return false;
                }

                if (hasGraceProtection)
                {
                    ModLogger.Debug("Encounter", "Suppressed encounter - grace protection window active");
                    return false;
                }
                
                // If not enlisted and not in grace period, allow normal encounters
                if (!isEnlisted)
                {
                    return true;
                }

                // CRITICAL: Prevent encounters with the lord's party itself
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
                            // Allow encounters when player party is visible/active - indicates player-initiated conversation
                            var mainMobileParty = MobileParty.MainParty;
                            var partyVisible = mainMobileParty?.IsVisible == true;
                            var partyActive = mainMobileParty?.IsActive == true;
                            var isPlayerInitiated = partyVisible || partyActive;
                            
                            if (isPlayerInitiated)
                            {
                                ModLogger.Debug("EncounterSuppression", $"Allowing player-initiated encounter with lord's party (Lord: {enlistedLord!.Name})");
                                return true;
                            }
                            
                            // Party is invisible and inactive - suppress unwanted automatic encounter
                            ModLogger.Debug("EncounterSuppression", $"Suppressed unwanted encounter with lord's party (Lord: {enlistedLord.Name})");
                            return false;
                        }
                    }
                    
                    // Check if this is a legitimate battle involving the lord's party or army
                    var lordInvolved = attackerParty == lordParty || defenderParty == lordParty;
                    
                    if (lordInvolved && lordParty.MapEvent != null)
                    {
                        ModLogger.Debug("EncounterSuppression", "Allowing legitimate battle involving enlisted lord's party");
                        return true;
                    }
                    
                    // Check if the lord's army is involved in this encounter
                    var lordArmy = enlistedLord.PartyBelongedTo?.Army;
                    if (lordArmy != null)
                    {
                        var armyInvolved = attackerParty == lordArmy.LeaderParty?.Party || defenderParty == lordArmy.LeaderParty?.Party;
                        if (armyInvolved && lordArmy.LeaderParty?.Party?.MapEvent != null)
                        {
                            ModLogger.Debug("EncounterSuppression", "Allowing legitimate battle involving enlisted lord's army");
                            return true;
                        }
                    }
                }

                // Check if the main party is currently active and in a battle
                if (MobileParty.MainParty?.IsActive == true && mainParty.MapEvent != null)
                {
                    ModLogger.Debug("EncounterSuppression", "Allowing encounter - main party is active and in battle");
                    return true;
                }

                // Suppress unwanted encounter
                ModLogger.Debug("EncounterSuppression", "Suppressed unwanted encounter involving enlisted player party");
                return false;
            }
            catch (Exception ex)
            {
                ModLogger.Error("EncounterSuppression", $"Error in encounter suppression patch: {ex.Message}");
                return true; // Fail open - allow encounter if we can't determine state
            }
        }
    }
}
