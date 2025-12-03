using System;
using System.Linq;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Mod.Core.Logging;

namespace Enlisted.Mod.GameAdapters.Patches
{
    /// <summary>
    /// Harmony patch that automatically joins enlisted players to their lord's battle,
    /// bypassing the native "join_encounter" menu that asks "Help X's Party / Don't get involved".
    /// 
    /// When an enlisted soldier's lord attacks enemies (like looters) without being in an army,
    /// the native game shows a choice menu. This patch intercepts that menu and automatically
    /// joins the battle on the lord's side, then shows the standard encounter menu with
    /// Attack/Send Troops/Wait options instead.
    /// </summary>
    [HarmonyPatch(typeof(TaleWorlds.CampaignSystem.CampaignBehaviors.EncounterGameMenuBehavior), "game_menu_join_encounter_on_init")]
    public static class JoinEncounterAutoSelectPatch
    {
        /// <summary>
        /// Prefix patch for game_menu_join_encounter_on_init.
        /// Intercepts the "join_encounter" menu initialization and auto-joins for enlisted players.
        /// Returns false to skip native on_init when auto-joining (player never sees the menu).
        /// </summary>
        [HarmonyPrefix]
        private static bool Prefix(MenuCallbackArgs args)
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
                    ModLogger.Debug("JoinEncounter", "Enlisted but lord party is null - allowing native menu");
                    return true;
                }

                // Get the encountered battle
                var battle = PlayerEncounter.EncounteredBattle;
                if (battle == null)
                {
                    ModLogger.Debug("JoinEncounter", "No encountered battle - allowing native menu");
                    return true;
                }

                // Check if our lord is involved in this battle
                bool lordIsAttacker = battle.AttackerSide.Parties.Any(p => p.Party == lordParty);
                bool lordIsDefender = battle.DefenderSide.Parties.Any(p => p.Party == lordParty);

                if (!lordIsAttacker && !lordIsDefender)
                {
                    // Lord is not in this battle - this might be a different encounter
                    // Let the native menu show so player can decide
                    ModLogger.Debug("JoinEncounter", "Lord not in this battle - allowing native menu");
                    return true;
                }

                // Determine which side to join (same side as our lord)
                BattleSideEnum lordSide = lordIsAttacker ? BattleSideEnum.Attacker : BattleSideEnum.Defender;

                // Check if we can actually join on the lord's side
                if (!battle.CanPartyJoinBattle(PartyBase.MainParty, lordSide))
                {
                    ModLogger.Warn("JoinEncounter", $"Cannot join battle on lord's side ({lordSide}) - allowing native menu");
                    return true;
                }

                // Auto-join the battle on the lord's side
                ModLogger.Info("JoinEncounter", $"Auto-joining lord's battle as {lordSide} (Lord: {lord.Name}, Battle: {battle.EventType})");

                try
                {
                    // Join the battle - this is what the native "Help X" option does
                    PlayerEncounter.JoinBattle(lordSide);

                    // Switch to the standard encounter menu (Attack/Send Troops/Wait options)
                    // This replaces the "join_encounter" menu with the actual battle options
                    if (battle.DefenderSide.TroopCount > 0)
                    {
                        GameMenu.SwitchToMenu("encounter");
                    }
                    else
                    {
                        // No defenders left (battle already won) - handle like native does
                        if (MobileParty.MainParty.Army != null)
                        {
                            if (MobileParty.MainParty.Army.LeaderParty != MobileParty.MainParty)
                            {
                                GameMenu.SwitchToMenu("army_wait");
                            }
                            else
                            {
                                GameMenu.SwitchToMenu("encounter");
                            }
                        }
                        else
                        {
                            GameMenu.SwitchToMenu("encounter");
                        }
                    }

                    ModLogger.Info("JoinEncounter", "Successfully auto-joined battle and switched to encounter menu");

                    // Return false to skip native on_init - the join_encounter menu never displays
                    return false;
                }
                catch (Exception joinEx)
                {
                    // JoinBattle can fail for parties with no troops
                    // Fall back to letting native menu show
                    ModLogger.Error("JoinEncounter", $"Failed to auto-join battle: {joinEx.Message}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("JoinEncounter", $"Error in join_encounter patch: {ex.Message}");
                // Fail open - let native menu show
                return true;
            }
        }
    }
}

