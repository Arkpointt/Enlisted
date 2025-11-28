using System;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Mod.Core.Logging;

namespace Enlisted.Mod.GameAdapters.Patches
{
    /// <summary>
    /// Prevents enlisted soldiers from receiving any loot.
    /// 
    /// As a soldier in someone else's army, the player does not receive personal loot.
    /// All spoils of war go to the lord. The soldier is compensated through wages instead.
    /// 
    /// Uses a two-layer approach:
    /// 1. Data level: Redirects loot to empty/dummy rosters during battle processing
    /// 2. UI level: Skips the loot screen entirely
    /// 
    /// Loot IS allowed during leave or grace periods when operating independently.
    /// 
    /// CRITICAL: We return EMPTY rosters, not null. Returning null causes crashes in
    /// MapEvent.LootDefeatedPartyItems() which doesn't handle null rosters gracefully.
    /// </summary>
    public static class LootBlockPatch
    {
        // Dummy rosters that receive loot but are never used
        // These prevent crashes while still blocking loot from going to player
        private static ItemRoster _dummyItemRoster = new ItemRoster();
        private static TroopRoster _dummyMemberRoster = TroopRoster.CreateDummyTroopRoster();
        private static TroopRoster _dummyPrisonerRoster = TroopRoster.CreateDummyTroopRoster();
        
        private static bool ShouldBlockLoot()
        {
            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment?.IsEnlisted != true)
            {
                return false; // Not enlisted - allow loot
            }
            
            // Allow loot when on leave or in grace period - player is operating independently
            if (enlistment.IsOnLeave || enlistment.IsInDesertionGracePeriod)
            {
                return false;
            }
            
            return true; // Actively serving - block loot
        }
        
        /// <summary>
        /// Clears the dummy rosters periodically to prevent memory buildup.
        /// Called after battles complete.
        /// </summary>
        public static void ClearDummyRosters()
        {
            try
            {
                _dummyItemRoster?.Clear();
                _dummyMemberRoster?.Clear();
                _dummyPrisonerRoster?.Clear();
            }
            catch
            {
                // Ignore errors during cleanup
            }
        }

        #region Data Level - Block Loot Assignment

        /// <summary>
        /// Returns empty item roster, preventing item loot from reaching player.
        /// CRITICAL: Returns empty roster, not null - null causes crashes in LootDefeatedPartyItems.
        /// </summary>
        [HarmonyPatch(typeof(MapEventParty), nameof(MapEventParty.RosterToReceiveLootItems), MethodType.Getter)]
        public static class ItemLootPatch
        {
            static bool Prefix(MapEventParty __instance, ref ItemRoster __result)
            {
                if (__instance.Party != PartyBase.MainParty)
                {
                    return true;
                }
                
                if (!ShouldBlockLoot())
                {
                    return true;
                }
                
                // Return empty dummy roster instead of null to prevent crashes
                __result = _dummyItemRoster;
                return false;
            }
        }

        /// <summary>
        /// Returns empty troop roster, preventing troop loot from reaching player.
        /// CRITICAL: Returns empty roster, not null - null causes crashes in LootDefeatedPartyItems.
        /// </summary>
        [HarmonyPatch(typeof(MapEventParty), nameof(MapEventParty.RosterToReceiveLootMembers), MethodType.Getter)]
        public static class MemberLootPatch
        {
            static bool Prefix(MapEventParty __instance, ref TroopRoster __result)
            {
                if (__instance.Party != PartyBase.MainParty)
                {
                    return true;
                }
                
                if (!ShouldBlockLoot())
                {
                    return true;
                }
                
                // Return empty dummy roster instead of null to prevent crashes
                __result = _dummyMemberRoster;
                return false;
            }
        }

        /// <summary>
        /// Returns empty prisoner roster, preventing prisoner loot from reaching player.
        /// CRITICAL: Returns empty roster, not null - null causes crashes in LootDefeatedPartyItems.
        /// </summary>
        [HarmonyPatch(typeof(MapEventParty), nameof(MapEventParty.RosterToReceiveLootPrisoners), MethodType.Getter)]
        public static class PrisonerLootPatch
        {
            static bool Prefix(MapEventParty __instance, ref TroopRoster __result)
            {
                if (__instance.Party != PartyBase.MainParty)
                {
                    return true;
                }
                
                if (!ShouldBlockLoot())
                {
                    return true;
                }
                
                // Return empty dummy roster instead of null to prevent crashes
                __result = _dummyPrisonerRoster;
                return false;
            }
        }

        #endregion

        #region UI Level - Skip Loot Screen

        /// <summary>
        /// Skips the loot screen and jumps directly to End state.
        /// This prevents crashes from banner/figurehead loot screens.
        /// Also clears dummy rosters to prevent memory buildup.
        /// </summary>
        [HarmonyPatch(typeof(PlayerEncounter), "DoLootParty")]
        public static class LootScreenPatch
        {
            static bool Prefix(PlayerEncounter __instance)
            {
                try
                {
                    if (!ShouldBlockLoot())
                    {
                        return true;
                    }

                    ModLogger.Info("LootBlock", "Skipping loot screen - enlisted soldiers don't receive personal loot");
                    
                    // Clear dummy rosters to prevent memory buildup
                    ClearDummyRosters();
                    
                    // Skip ALL loot states (party, inventory, ships/figureheads) and go directly to End
                    var mapEventStateField = typeof(PlayerEncounter).GetField("_mapEventState",
                        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                    if (mapEventStateField != null)
                    {
                        mapEventStateField.SetValue(__instance, PlayerEncounterState.End);
                    }
                    return false;
                }
                catch (Exception ex)
                {
                    ModLogger.Error("LootBlock", $"Error in loot screen patch: {ex.Message}", ex);
                    return true; // Allow loot on error to prevent breaking gameplay
                }
            }
        }

        #endregion
    }
}
