using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Mod.Core.Logging;

namespace Enlisted.Mod.GameAdapters.Patches
{
    /// <summary>
    /// Prevents loot from being assigned to the player's party during battle result processing.
    /// 
    /// Problem: Even though we block the loot screen, items/prisoners/troops are still assigned
    /// to temporary rosters during MapEvent.LootDefeatedPartyMembers (before the loot screen).
    /// This can cause "capacity exceeded" warnings and other issues.
    /// 
    /// Solution: Return null from the RosterToReceiveLoot* properties when the player is enlisted.
    /// The loot distribution code already handles null by simply not adding items.
    /// 
    /// This ensures enlisted soldiers receive NO loot at all - consistent with the roleplay
    /// that spoils of war go to the lord, not individual soldiers.
    /// </summary>
    public static class LootAssignmentBlockPatch
    {
        /// <summary>
        /// Patch for MapEventParty.RosterToReceiveLootItems property getter.
        /// Returns null for enlisted players, preventing item loot assignment.
        /// </summary>
        [HarmonyPatch(typeof(MapEventParty), nameof(MapEventParty.RosterToReceiveLootItems), MethodType.Getter)]
        public static class ItemLootPatch
        {
            static bool Prefix(MapEventParty __instance, ref ItemRoster __result)
            {
                // Only affect the player's party
                if (__instance.Party != PartyBase.MainParty)
                    return true;
                
                var enlistment = EnlistmentBehavior.Instance;
                if (enlistment?.IsEnlisted != true || enlistment.IsOnLeave || enlistment.IsInDesertionGracePeriod)
                    return true; // Allow loot when not actively serving
                
                // Actively serving - return null to block loot assignment
                __result = null;
                return false;
            }
        }

        /// <summary>
        /// Patch for MapEventParty.RosterToReceiveLootMembers property getter.
        /// Returns null for enlisted players, preventing troop loot assignment.
        /// </summary>
        [HarmonyPatch(typeof(MapEventParty), nameof(MapEventParty.RosterToReceiveLootMembers), MethodType.Getter)]
        public static class MemberLootPatch
        {
            static bool Prefix(MapEventParty __instance, ref TroopRoster __result)
            {
                if (__instance.Party != PartyBase.MainParty)
                    return true;
                
                var enlistment = EnlistmentBehavior.Instance;
                if (enlistment?.IsEnlisted != true || enlistment.IsOnLeave || enlistment.IsInDesertionGracePeriod)
                    return true; // Allow loot when not actively serving
                
                __result = null;
                return false;
            }
        }

        /// <summary>
        /// Patch for MapEventParty.RosterToReceiveLootPrisoners property getter.
        /// Returns null for enlisted players, preventing prisoner loot assignment.
        /// </summary>
        [HarmonyPatch(typeof(MapEventParty), nameof(MapEventParty.RosterToReceiveLootPrisoners), MethodType.Getter)]
        public static class PrisonerLootPatch
        {
            static bool Prefix(MapEventParty __instance, ref TroopRoster __result)
            {
                if (__instance.Party != PartyBase.MainParty)
                    return true;
                
                var enlistment = EnlistmentBehavior.Instance;
                if (enlistment?.IsEnlisted != true || enlistment.IsOnLeave || enlistment.IsInDesertionGracePeriod)
                    return true; // Allow loot when not actively serving
                
                __result = null;
                return false;
            }
        }
    }
}

