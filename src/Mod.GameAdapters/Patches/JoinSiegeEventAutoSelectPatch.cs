using System;
using System.Linq;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Mod.Core.Logging;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace Enlisted.Mod.GameAdapters.Patches
{
    /// <summary>
    /// Harmony patch that automatically handles the "join_siege_event" menu for enlisted players.
    /// This menu appears when encountering a settlement under siege, showing options like
    /// "Assault the siege camp", "Break in to help the defenders", or "Don't get involved".
    /// When enlisted, the player should automatically follow their lord's position in the siege
    /// rather than making independent choices.
    /// </summary>
    [HarmonyPatch(typeof(TaleWorlds.CampaignSystem.CampaignBehaviors.EncounterGameMenuBehavior),
        "game_menu_join_siege_event_on_init")]
    public static class JoinSiegeEventAutoSelectPatch
    {
        /// <summary>
        /// Prefix patch for game_menu_join_siege_event_on_init.
        /// Intercepts the "join_siege_event" menu initialization and auto-joins for enlisted players.
        /// Returns false to skip native on_init when auto-joining (player never sees the menu).
        /// Called by Harmony via reflection.
        /// </summary>
        [HarmonyPrefix]
        public static bool Prefix()
        {
            try
            {
                // Check if player is enlisted
                var enlistment = EnlistmentBehavior.Instance;
                if (enlistment?.IsEnlisted != true)
                {
                    // Not enlisted - let native menu show normally
                    return true;
                }

                var lord = enlistment.CurrentLord;
                var lordParty = lord?.PartyBelongedTo?.Party;
                if (lordParty == null)
                {
                    ModLogger.Debug("JoinSiegeEvent", "Enlisted but lord party is null - allowing native menu");
                    return true;
                }

                // Get the encounter settlement and check if it's under siege
                var settlement = PlayerEncounter.EncounterSettlement;
                if (settlement == null || settlement.SiegeEvent == null)
                {
                    ModLogger.Debug("JoinSiegeEvent", "No settlement or siege event - allowing native menu");
                    return true;
                }

                var siegeEvent = settlement.SiegeEvent;

                // Determine which side the lord is on
                // Check if lord is a besieger (attacking the settlement)
                var lordIsBesieger = siegeEvent.BesiegerCamp?.GetInvolvedPartiesForEventType().Any(p => p == lordParty) == true;

                // Check if lord is a defender (inside the settlement)
                var lordIsDefender = settlement.Parties.Any(p => p.Party == lordParty);

                if (!lordIsBesieger && !lordIsDefender)
                {
                    // Lord is not involved in this siege - let player make their own choice
                    ModLogger.Debug("JoinSiegeEvent",
                        $"Lord {lord.Name} not involved in siege of {settlement.Name} - allowing native menu");
                    return true;
                }

                // Get the siege battle if one exists
                var battle = PlayerEncounter.EncounteredBattle;
                if (battle == null)
                {
                    // No active battle - this is a "join siege event" but not a battle yet
                    // Player might be approaching a siege in progress
                    // Let them choose since there's no immediate battle
                    ModLogger.Debug("JoinSiegeEvent", "No active battle at siege - allowing native menu");
                    return true;
                }

                // Determine which side to join based on lord's position
                BattleSideEnum lordSide;
                string lordRole;

                if (lordIsBesieger)
                {
                    lordSide = BattleSideEnum.Attacker;
                    lordRole = "besieger";
                }
                else
                {
                    // lordIsDefender is guaranteed true here due to the check at line 69
                    lordSide = BattleSideEnum.Defender;
                    lordRole = "defender";
                }

                // Verify lord is actually in this battle
                var lordIsInBattle = battle.AttackerSide.Parties.Any(p => p.Party == lordParty) ||
                                     battle.DefenderSide.Parties.Any(p => p.Party == lordParty);

                if (!lordIsInBattle)
                {
                    ModLogger.Debug("JoinSiegeEvent", "Lord not in this specific battle - allowing native menu");
                    return true;
                }

                // Check if we can join on the lord's side
                bool canJoinNatively = battle.CanPartyJoinBattle(PartyBase.MainParty, lordSide);

                // Check if lord is in a non-Kingdom faction
                bool isNonKingdomFactionLord = lord.MapFaction != null &&
                                               !(lord.MapFaction is Kingdom) &&
                                               (lord.MapFaction.IsMinorFaction || lord.MapFaction.IsBanditFaction);

                if (!canJoinNatively && !isNonKingdomFactionLord)
                {
                    // Kingdom lord but faction check failed - let native handle it
                    ModLogger.Warn("JoinSiegeEvent",
                        $"Faction check failed for Kingdom lord as {lordRole} - allowing native menu");
                    return true;
                }

                // Auto-join the battle on the lord's side
                ModLogger.Info("Battle",
                    $"Auto-joining {lord.Name}'s siege battle as {lordRole} at {settlement.Name}");

                try
                {
                    // If player is inside the settlement and needs to leave to join attackers
                    if (PlayerEncounter.InsideSettlement && lordSide == BattleSideEnum.Attacker)
                    {
                        PlayerEncounter.LeaveSettlement();
                    }

                    // Join the battle on the lord's side
                    PlayerEncounter.JoinBattle(lordSide);

                    // Determine which menu to switch to based on battle state and party status
                    var enemySide = lordSide == BattleSideEnum.Attacker ? battle.DefenderSide : battle.AttackerSide;

                    if (enemySide.TroopCount > 0)
                    {
                        // Battle is ongoing - show encounter menu with Attack/Send Troops/Wait options
                        GameMenu.SwitchToMenu("encounter");
                    }
                    else
                    {
                        // Battle is won - handle like native does
                        var inArmy = MobileParty.MainParty.Army != null;
                        var isArmyLeader = inArmy && MobileParty.MainParty.Army.LeaderParty == MobileParty.MainParty;

                        if (inArmy && !isArmyLeader)
                        {
                            // In army but not leader - wait menu
                            GameMenu.SwitchToMenu("army_wait");
                        }
                        else if (lordSide == BattleSideEnum.Attacker)
                        {
                            // Attacker side - go to siege strategies menu
                            GameMenu.SwitchToMenu("menu_siege_strategies");
                            MobileParty.MainParty.SetMoveModeHold();
                        }
                        else
                        {
                            // Defender side or default - encounter menu
                            GameMenu.SwitchToMenu("encounter");
                        }
                    }

                    ModLogger.Info("JoinSiegeEvent",
                        $"Successfully auto-joined siege battle as {lordRole} and switched menu");

                    // Return false to skip native on_init - the join_siege_event menu never displays
                    return false;
                }
                catch (Exception joinEx)
                {
                    // JoinBattle can fail - fall back to letting native menu show
                    ModLogger.ErrorCode("JoinSiegeEvent", "E-PATCH-015",
                        "Failed to auto-join siege battle", joinEx);
                    return true;
                }
            }
            catch (Exception ex)
            {
                ModLogger.ErrorCode("JoinSiegeEvent", "E-PATCH-016",
                    "Error in join_siege_event patch", ex);
                // Fail open - let native menu show
                return true;
            }
        }
    }
}

