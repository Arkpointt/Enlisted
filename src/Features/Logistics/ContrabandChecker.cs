using System;
using System.Collections.Generic;
using Enlisted.Mod.Core.Logging;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace Enlisted.Features.Logistics
{
    /// <summary>
    /// Checks player inventory for contraband items at muster.
    /// Contraband includes items that violate tier restrictions, role restrictions, or luxury rules.
    /// </summary>
    public static class ContrabandChecker
    {
        private const string LogCategory = "Contraband";

        /// <summary>
        /// Result of a contraband check containing all contraband items found.
        /// </summary>
        public class ContrabandCheckResult
        {
            public List<ContrabandItem> Items { get; } = new();
            public int TotalValue => CalculateTotalValue();
            public bool HasContraband => Items.Count > 0;

            /// <summary>
            /// Returns the most valuable contraband item for confiscation.
            /// </summary>
            public ContrabandItem MostValuable
            {
                get
                {
                    if (Items.Count == 0)
                    {
                        return null;
                    }

                    ContrabandItem best = Items[0];
                    foreach (var item in Items)
                    {
                        if (item.Value > best.Value)
                        {
                            best = item;
                        }
                    }
                    return best;
                }
            }

            private int CalculateTotalValue()
            {
                int total = 0;
                foreach (var item in Items)
                {
                    total += item.Value;
                }
                return total;
            }
        }

        /// <summary>
        /// Represents a single contraband item with violation details.
        /// </summary>
        public class ContrabandItem
        {
            public ItemObject Item { get; set; }
            public int Amount { get; set; }
            public int Value { get; set; }
            public string ViolationType { get; set; }
            public string Description { get; set; }
        }

        /// <summary>
        /// Scans the player's inventory for contraband items.
        /// </summary>
        /// <param name="playerTier">Player's current enlistment tier (1-9).</param>
        /// <param name="playerRole">Player's current role from EnlistedStatusManager.</param>
        /// <returns>Result containing all contraband items found.</returns>
        public static ContrabandCheckResult ScanInventory(int playerTier, string playerRole)
        {
            var result = new ContrabandCheckResult();

            try
            {
                var party = MobileParty.MainParty;
                if (party?.ItemRoster == null)
                {
                    ModLogger.Debug(LogCategory, "No inventory to scan - party or roster is null");
                    return result;
                }

                var itemRoster = party.ItemRoster;

                for (int i = 0; i < itemRoster.Count; i++)
                {
                    var element = itemRoster.GetElementCopyAtIndex(i);
                    if (element.EquipmentElement.Item == null)
                    {
                        continue;
                    }

                    var item = element.EquipmentElement.Item;
                    var amount = element.Amount;

                    var violation = CheckItem(item, playerTier, playerRole);
                    if (violation != null)
                    {
                        result.Items.Add(new ContrabandItem
                        {
                            Item = item,
                            Amount = amount,
                            Value = item.Value * amount,
                            ViolationType = violation.Item1,
                            Description = violation.Item2
                        });
                    }
                }

                if (result.HasContraband)
                {
                    ModLogger.Debug(LogCategory,
                        $"Found {result.Items.Count} contraband item(s) worth {result.TotalValue} gold");
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, "Error scanning inventory for contraband", ex);
            }

            return result;
        }

        /// <summary>
        /// Checks if a specific item is contraband.
        /// Returns null if the item is allowed, or a tuple of (violationType, description) if contraband.
        /// </summary>
        private static Tuple<string, string> CheckItem(ItemObject item, int playerTier, string playerRole)
        {
            if (item == null)
            {
                return null;
            }

            // Skip food items - never contraband
            if (item.IsFood)
            {
                return null;
            }

            // Skip quest items - cannot be confiscated due to quest requirements
            if (IsQuestItem(item))
            {
                return null;
            }

            // Tier violation: Item tier exceeds player tier + 1
            var tierViolation = CheckTierViolation(item, playerTier);
            if (tierViolation != null)
            {
                return tierViolation;
            }

            // Role violation: Weapon doesn't match player's assigned role
            var roleViolation = CheckRoleViolation(item, playerRole);
            if (roleViolation != null)
            {
                return roleViolation;
            }

            // Luxury violation: Non-essential luxury items
            var luxuryViolation = CheckLuxuryViolation(item);
            if (luxuryViolation != null)
            {
                return luxuryViolation;
            }

            return null;
        }

        /// <summary>
        /// Checks if an item violates tier restrictions.
        /// Tier violations occur when equipment is too high-quality for the player's rank.
        /// </summary>
        private static Tuple<string, string> CheckTierViolation(ItemObject item, int playerTier)
        {
            // Get item tier (Tier1 = 0, Tier2 = 1, etc. in the enum, but we use 1-based)
            int itemTierValue = GetItemTierNumeric(item);

            // Player is allowed items up to their tier + 1
            // e.g., T2 player can have T1, T2, T3 items
            int maxAllowedTier = playerTier + 1;

            if (itemTierValue > maxAllowedTier)
            {
                return new Tuple<string, string>(
                    "tier",
                    $"Equipment above your station (T{itemTierValue} item, you're authorized for T{maxAllowedTier})");
            }

            return null;
        }

        /// <summary>
        /// Checks if a weapon violates role restrictions.
        /// Certain weapons are restricted to specific roles.
        /// </summary>
        private static Tuple<string, string> CheckRoleViolation(ItemObject item, string playerRole)
        {
            // Only check weapon items
            if (!item.HasWeaponComponent)
            {
                return null;
            }

            var primaryWeapon = item.PrimaryWeapon;
            if (primaryWeapon == null)
            {
                return null;
            }

            var weaponClass = primaryWeapon.WeaponClass;

            // Crossbows are ranged specialist equipment
            if (weaponClass == WeaponClass.Crossbow)
            {
                if (!IsRangedRole(playerRole))
                {
                    return new Tuple<string, string>(
                        "role",
                        "Crossbow requires ranged specialist assignment");
                }
            }

            // Bows are also ranged equipment
            if (weaponClass == WeaponClass.Bow)
            {
                if (!IsRangedRole(playerRole))
                {
                    return new Tuple<string, string>(
                        "role",
                        "Bow requires ranged specialist assignment");
                }
            }

            // Two-handed polearms for cavalry/mounted roles
            if (weaponClass == WeaponClass.TwoHandedPolearm ||
                weaponClass == WeaponClass.LowGripPolearm)
            {
                // Lance-type weapons for cavalry
                if (primaryWeapon.WeaponFlags.HasAnyFlag(WeaponFlags.PenaltyWithShield))
                {
                    if (!IsCavalryRole(playerRole) && !IsOfficerRole(playerRole))
                    {
                        return new Tuple<string, string>(
                            "role",
                            "Lance weapons require cavalry or officer assignment");
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Checks if an item is a luxury item that should not be carried during campaign.
        /// </summary>
        private static Tuple<string, string> CheckLuxuryViolation(ItemObject item)
        {
            // Trade goods marked as luxury
            // Note: Bannerlord doesn't have a direct ItemTypeEnum.Jewelry
            // Instead, we check for high-value goods that aren't food/weapons/armor

            if (item.Type == ItemObject.ItemTypeEnum.Goods && !item.IsFood)
            {
                // High-value trade goods (jewelry, velvet, etc.) are contraband
                // Threshold: Items worth more than 500 gold that aren't military supplies
                if (item.Value > 500)
                {
                    // Check if it's clearly a luxury good by name/id pattern
                    var itemId = item.StringId?.ToLowerInvariant() ?? "";

                    if (itemId.Contains("jewelry") ||
                        itemId.Contains("silver") ||
                        itemId.Contains("gold") ||
                        itemId.Contains("velvet") ||
                        itemId.Contains("fur") ||
                        itemId.Contains("silk"))
                    {
                        return new Tuple<string, string>(
                            "luxury",
                            "Luxury goods not permitted for enlisted personnel");
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Gets the numeric tier value for an item (1-7 scale, matching player tier scale).
        /// </summary>
        private static int GetItemTierNumeric(ItemObject item)
        {
            // ItemObject.Tier returns ItemTiers enum: Tier1=0, Tier2=1, ..., Tier6=5
            // Convert to 1-based: Tier1=1, Tier2=2, ..., Tier6=6
            // Items without tiers or with Tier1 default to 1

            int enumValue = (int)item.Tier;

            // Tier1=0 in enum, but we want 1-based
            return enumValue + 1;
        }

        /// <summary>
        /// Determines if a role is a ranged specialist role.
        /// </summary>
        private static bool IsRangedRole(string role)
        {
            if (string.IsNullOrEmpty(role))
            {
                return false;
            }

            var lowerRole = role.ToLowerInvariant();
            return lowerRole == "scout" ||
                   lowerRole == "ranged" ||
                   lowerRole == "archer" ||
                   lowerRole == "marksman";
        }

        /// <summary>
        /// Determines if a role is a cavalry role.
        /// </summary>
        private static bool IsCavalryRole(string role)
        {
            if (string.IsNullOrEmpty(role))
            {
                return false;
            }

            var lowerRole = role.ToLowerInvariant();
            return lowerRole == "cavalry" ||
                   lowerRole == "mounted" ||
                   lowerRole == "lancer" ||
                   lowerRole == "rider";
        }

        /// <summary>
        /// Determines if a role is an officer/commander role (exempt from some restrictions).
        /// </summary>
        private static bool IsOfficerRole(string role)
        {
            if (string.IsNullOrEmpty(role))
            {
                return false;
            }

            var lowerRole = role.ToLowerInvariant();
            return lowerRole == "officer" ||
                   lowerRole == "commander" ||
                   lowerRole == "nco" ||
                   lowerRole == "sergeant";
        }

        /// <summary>
        /// Checks if an item is a quest item that cannot be confiscated.
        /// Quest items are marked with the NotMerchandise flag and typically have special handling.
        /// </summary>
        private static bool IsQuestItem(ItemObject item)
        {
            if (item == null)
            {
                return false;
            }

            // Items with NotMerchandise flag are typically quest-related or special
            // However, this can also include some regular items, so we add additional checks
            if (item.NotMerchandise)
            {
                // Banner items are special - always protected
                if (item.Type == ItemObject.ItemTypeEnum.Banner)
                {
                    return true;
                }

                // Books can be quest items
                if (item.Type == ItemObject.ItemTypeEnum.Book)
                {
                    return true;
                }

                // Log for debugging but don't auto-exclude all NotMerchandise
                ModLogger.Debug(LogCategory, $"Item {item.StringId} has NotMerchandise flag - checking further");
            }

            // Check if item ID suggests it's a quest item
            var itemId = item.StringId?.ToLowerInvariant() ?? "";
            if (itemId.Contains("quest_") || itemId.Contains("special_") || itemId.Contains("unique_"))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Calculates the bribe amount for a contraband item (50-75% of value).
        /// </summary>
        /// <param name="itemValue">The item's value in gold.</param>
        /// <returns>Bribe amount in gold.</returns>
        public static int CalculateBribeAmount(int itemValue)
        {
            // Random between 50% and 75%
            float percentage = 0.50f + (MBRandom.RandomFloat * 0.25f);
            return (int)(itemValue * percentage);
        }

        /// <summary>
        /// Calculates the fine amount for confiscation (typically 25-50% of item value).
        /// </summary>
        /// <param name="itemValue">The item's value in gold.</param>
        /// <returns>Fine amount in gold.</returns>
        public static int CalculateFineAmount(int itemValue)
        {
            // Random between 25% and 50%
            float percentage = 0.25f + (MBRandom.RandomFloat * 0.25f);
            return (int)(itemValue * percentage);
        }

        /// <summary>
        /// Removes a contraband item from the player's inventory.
        /// </summary>
        /// <param name="item">The item to confiscate.</param>
        /// <returns>True if successfully removed.</returns>
        public static bool ConfiscateItem(ItemObject item)
        {
            try
            {
                var party = MobileParty.MainParty;
                if (party?.ItemRoster == null)
                {
                    return false;
                }

                int currentCount = party.ItemRoster.GetItemNumber(item);
                if (currentCount <= 0)
                {
                    return false;
                }

                // Remove one copy of the item
                party.ItemRoster.AddToCounts(item, -1);

                ModLogger.Info(LogCategory, $"Confiscated item: {item.Name}");
                return true;
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, $"Failed to confiscate item: {item?.Name}", ex);
                return false;
            }
        }
    }
}

