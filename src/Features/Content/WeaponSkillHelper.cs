using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace Enlisted.Features.Content
{
    /// <summary>
    /// Helper for determining skill XP based on equipped weapons.
    /// Used by training events to award appropriate skill XP.
    /// </summary>
    public static class WeaponSkillHelper
    {
        /// <summary>
        /// Gets the primary combat skill for the hero's currently equipped weapon.
        /// Checks all weapon slots and returns the first valid weapon skill.
        /// Falls back to Athletics if no weapon is equipped.
        /// </summary>
        public static SkillObject GetEquippedWeaponSkill(Hero hero)
        {
            if (hero?.BattleEquipment == null)
            {
                return DefaultSkills.Athletics;
            }

            // Check weapon slots (0-3) for combat weapons
            for (int i = 0; i < 4; i++)
            {
                var element = hero.BattleEquipment[i];
                if (element.Item != null && IsWeaponType(element.Item.Type))
                {
                    var skill = element.Item.RelevantSkill;
                    if (skill != null)
                    {
                        return skill;
                    }
                }
            }

            return DefaultSkills.Athletics;
        }

        /// <summary>
        /// Gets the display name of the hero's primary equipped weapon.
        /// Returns "fists" if no weapon is equipped.
        /// </summary>
        public static string GetEquippedWeaponName(Hero hero)
        {
            if (hero?.BattleEquipment == null)
            {
                return "fists";
            }

            for (int i = 0; i < 4; i++)
            {
                var element = hero.BattleEquipment[i];
                if (element.Item != null && IsWeaponType(element.Item.Type))
                {
                    return element.Item.Name?.ToString() ?? "weapon";
                }
            }

            return "fists";
        }

        /// <summary>
        /// Gets the hero's weakest combat skill for focused training.
        /// Only considers main combat skills (OneHanded, TwoHanded, Polearm, Bow, Crossbow, Throwing).
        /// </summary>
        public static SkillObject GetWeakestCombatSkill(Hero hero)
        {
            if (hero == null)
            {
                return DefaultSkills.OneHanded;
            }

            var combatSkills = new[]
            {
                DefaultSkills.OneHanded,
                DefaultSkills.TwoHanded,
                DefaultSkills.Polearm,
                DefaultSkills.Bow,
                DefaultSkills.Crossbow,
                DefaultSkills.Throwing
            };

            SkillObject weakest = combatSkills[0];
            int lowestValue = hero.GetSkillValue(weakest);

            foreach (var skill in combatSkills)
            {
                int value = hero.GetSkillValue(skill);
                if (value < lowestValue)
                {
                    lowestValue = value;
                    weakest = skill;
                }
            }

            return weakest;
        }

        /// <summary>
        /// Checks if the hero has any combat weapon equipped.
        /// </summary>
        public static bool HasWeaponEquipped(Hero hero)
        {
            if (hero?.BattleEquipment == null)
            {
                return false;
            }

            for (int i = 0; i < 4; i++)
            {
                var element = hero.BattleEquipment[i];
                if (element.Item != null && IsWeaponType(element.Item.Type))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if an item type is a combat weapon.
        /// </summary>
        private static bool IsWeaponType(ItemObject.ItemTypeEnum type)
        {
            return type == ItemObject.ItemTypeEnum.OneHandedWeapon ||
                   type == ItemObject.ItemTypeEnum.TwoHandedWeapon ||
                   type == ItemObject.ItemTypeEnum.Polearm ||
                   type == ItemObject.ItemTypeEnum.Bow ||
                   type == ItemObject.ItemTypeEnum.Crossbow ||
                   type == ItemObject.ItemTypeEnum.Thrown ||
                   type == ItemObject.ItemTypeEnum.Sling;
        }
    }
}

