using System;
using System.Collections.Generic;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Mod.Core.Logging;

namespace Enlisted.Mod.GameAdapters.Patches
{
    /// <summary>
    /// Harmony patch that prevents mounted players from joining siege assaults.
    /// Intercepts EncounterGameMenuBehavior.game_menu_encounter_attack_on_consequence to require
    /// dismounting before siege participation, preventing mounted players from joining sieges.
    /// </summary>
    [HarmonyPatch(typeof(EncounterGameMenuBehavior), "game_menu_encounter_attack_on_consequence")]
    public class NoHorseSiegePatch
    {
        /// <summary>
        /// Prefix method that runs before the encounter attack consequence handler.
        /// Checks if the player is mounted and attempting to join a siege, and prevents it if so.
        /// </summary>
        /// <param name="args">Menu callback arguments containing menu state and context.</param>
        /// <returns>False to prevent siege participation, true to allow it.</returns>
        static bool Prefix(MenuCallbackArgs args)
        {
            try
            {
                var enlistment = EnlistmentBehavior.Instance;
                if (enlistment?.IsEnlisted != true)
                {
                    return true; // Allow normal behavior when not enlisted
                }

                var lord = enlistment.CurrentLord;
                var lordParty = lord?.PartyBelongedTo;
                
                // Check if the lord is in a siege and the player is mounted
                // If so, prevent siege participation until the player dismounts
                // NULL-SAFE: MapEvent may not exist yet during siege setup, so check SiegeEvent first
                // CORRECT API: Use Party.SiegeEvent and Party.MapEvent (not direct on MobileParty)
                if (lordParty?.Party.SiegeEvent != null && 
                    Hero.MainHero.CharacterObject.Equipment[10].Item != null && // Equipment slot 10 is the horse slot
                    lordParty.Party.MapEvent != null &&
                    ContainsParty(lordParty.Party.MapEvent.PartiesOnSide(BattleSideEnum.Attacker), lordParty))
                {
                    InformationManager.DisplayMessage(new InformationMessage("Dismount from horse first before joining siege"));
                    return false; // Prevent siege participation while mounted
                }
                
                return true; // Allow normal behavior if not mounted or not in siege
            }
            catch (System.Exception ex)
            {
                ModLogger.Error("NoHorseSiege", "Error in horse siege patch", ex);
                return true; // Allow normal behavior on error
            }
        }

        /// <summary>
        /// Helper method that checks if a specific party is included in a list of battle parties.
        /// Used to determine which side of a battle a party is fighting on by checking
        /// if the party appears in the attacker or defender side's party list.
        /// </summary>
        /// <param name="parties">The list of battle parties to check against.</param>
        /// <param name="party">The party to search for in the list.</param>
        /// <returns>True if the party is found in the list, false otherwise.</returns>
        public static bool ContainsParty(IReadOnlyList<MapEventParty> parties, MobileParty party)
        {
            if (parties == null || party == null)
                return false;
                
            foreach (var mapEventParty in parties)
            {
                if (mapEventParty.Party.MobileParty == party)
                    return true;
            }
            return false;
        }
    }
}
