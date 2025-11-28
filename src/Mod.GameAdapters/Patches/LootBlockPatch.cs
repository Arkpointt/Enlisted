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
    /// 1. Data level: Blocks loot assignment to player's inventory during battle processing
    /// 2. UI level: Skips the loot screen entirely
    /// 
    /// Loot IS allowed during leave or grace periods when operating independently.
    /// </summary>
    public static class LootBlockPatch
    {
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

        #region Data Level - Block Loot Assignment

        /// <summary>
        /// Returns null for item roster, preventing item loot assignment.
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
                
                __result = null;
                return false;
            }
        }

        /// <summary>
        /// Returns null for troop roster, preventing troop loot assignment.
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
                
                __result = null;
                return false;
            }
        }

        /// <summary>
        /// Returns null for prisoner roster, preventing prisoner loot assignment.
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
                
                __result = null;
                return false;
            }
        }

        #endregion

        #region UI Level - Skip Loot Screen

        /// <summary>
        /// Skips the loot screen and jumps directly to End state.
        /// This prevents crashes from banner/figurehead loot screens.
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

