using System;
using HarmonyLib;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Party;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Mod.Core;
using Enlisted.Mod.Core.Logging;

namespace Enlisted.Mod.GameAdapters.Patches
{
    /// <summary>
    /// Contains patches to handle the "Virtual Food Link" system.
    /// This system allows the enlisted player to share the Lord's food supply without
    /// physically duplicating items into the player's inventory (which caused exploits).
    /// </summary>
    public static class FoodSystemPatches
    {
        /// <summary>
        /// Patch 1: Virtual Food Link (UI/Getter)
        /// Links the player's food count to the lord's party when enlisted.
        ///
        /// Purpose:
        /// - Ensures UI displays the correct "shared" food amount.
        /// - Satisfies Morale checks that read TotalFoodAtInventory.
        /// - Prevents the "Sell to Lord" exploit by keeping the actual ItemRoster empty.
        /// </summary>
        [HarmonyPatch(typeof(MobileParty), "TotalFoodAtInventory", MethodType.Getter)]
        public static class VirtualFoodLinkPatch
        {
            /// <summary>
            /// Postfix method that runs after MobileParty.TotalFoodAtInventory getter.
            /// Called by Harmony via reflection.
            /// </summary>
            [HarmonyPostfix]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Harmony convention: __instance and __result are special injected parameters")]
            public static void Postfix(MobileParty __instance, ref int __result)
            {
                try
                {
                    if (!EnlistedActivation.EnsureActive())
                    {
                        return;
                    }

                    // Only modify for the player's party
                    if (__instance == null || !__instance.IsMainParty)
                    {
                        return;
                    }

                    var enlistment = EnlistmentBehavior.Instance;
                    if (enlistment == null || !enlistment.IsEnlisted)
                    {
                        return;
                    }

                    var lord = enlistment.EnlistedLord;
                    var lordParty = lord?.PartyBelongedTo;

                    if (lordParty == null)
                    {
                        return;
                    }

                    // Determine the food source: Army Leader or direct Lord
                    // Soldiers feed from the Army stocks if available
                    var foodSource = lordParty.Army?.LeaderParty ?? lordParty;

                    // Return the food count of the source party
                    // This acts as a "view" of the lord's supplies
                    __result = foodSource.TotalFoodAtInventory;
                }
                catch (Exception ex)
                {
                    ModLogger.ErrorCode("Food", "E-PATCH-022", "Error in VirtualFoodLink", ex);
                }
            }
        }

        /// <summary>
        /// Patch 2: Shared Food Consumption (Logic)
        /// Intercepts the daily food consumption logic to allow the player to "eat" from the Lord's supply.
        ///
        /// Purpose:
        /// - Prevents the "Starvation" state when the player has no items in their own inventory.
        /// - Vanilla code iterates the ItemRoster to find food; since ours is empty, it would default to starvation.
        /// - This patch checks the Lord's supply and, if available, marks the player as "Fed" (RemainingFoodPercentage = 100).
        /// </summary>
        [HarmonyPatch(typeof(FoodConsumptionBehavior), "PartyConsumeFood")]
        public static class SharedFoodConsumptionPatch
        {
            /// <summary>
            /// Prefix method that runs before FoodConsumptionBehavior.PartyConsumeFood.
            /// Called by Harmony via reflection.
            /// </summary>
            [HarmonyPrefix]
            public static bool Prefix(MobileParty mobileParty)
            {
                try
                {
                    if (!EnlistedActivation.EnsureActive())
                    {
                        return true;
                    }

                    // Only intercept for the player's party
                    if (mobileParty == null || !mobileParty.IsMainParty)
                    {
                        return true;
                    }

                    var enlistment = EnlistmentBehavior.Instance;
                    if (enlistment == null || !enlistment.IsEnlisted)
                    {
                        return true;
                    }

                    var lord = enlistment.EnlistedLord;
                    var lordParty = lord?.PartyBelongedTo;

                    // If Lord is invalid, let vanilla handle it (likely starve)
                    if (lordParty == null)
                    {
                        return true;
                    }

                    // Check if Lord (or Army) has food available
                    // We use TotalFoodAtInventory which aggregates all food items
                    var foodSource = lordParty.Army?.LeaderParty ?? lordParty;

                    if (foodSource.TotalFoodAtInventory > 0)
                    {
                        // Lord has food -> Player eats "virtually"

                        // 1. Reset starvation flags to indicate we are fully fed
                        mobileParty.Party.RemainingFoodPercentage = 100;
                        mobileParty.Party.OnConsumedFood();

                        // 2. Log occasionally (debug)
                        // ModLogger.Debug("Food", "Enlisted player consumed shared food from Lord");

                        // 3. Return FALSE to skip the vanilla method
                        // Skipping vanilla prevents it from scanning our empty inventory and declaring starvation
                        return false;
                    }

                    // If Lord has NO food, fall back to vanilla logic
                    // This will result in starvation, which is correct (Lord can't feed us)
                    return true;
                }
                catch (Exception ex)
                {
                    ModLogger.ErrorCode("Food", "E-PATCH-023", "Error in SharedFoodConsumptionPatch", ex);
                    return true; // Fail safe: run original
                }
            }
        }
    }
}
