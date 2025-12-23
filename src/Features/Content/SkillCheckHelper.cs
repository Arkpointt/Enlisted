using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.Core;

namespace Enlisted.Features.Content
{
    /// <summary>
    /// Centralized skill check logic used by events, orders, and decisions.
    /// Provides consistent success chance calculations and skill threshold checks.
    /// </summary>
    public static class SkillCheckHelper
    {
        /// <summary>
        /// Calculates the success chance for a skill-based action.
        /// Formula: Base% + (Skill / 3)
        /// 
        /// Examples:
        /// - Skill 0, Base 50%  → 50%
        /// - Skill 60, Base 50% → 70%
        /// - Skill 120, Base 50% → 90%
        /// </summary>
        /// <param name="hero">The hero performing the check.</param>
        /// <param name="skill">The skill being tested.</param>
        /// <param name="baseChance">Base success chance (0.0 to 1.0). Default is 0.5 (50%).</param>
        /// <returns>Success chance as a float between 0.0 and 1.0.</returns>
        public static float CalculateSuccessChance(Hero hero, SkillObject skill, float baseChance = 0.5f)
        {
            if (hero == null || skill == null)
            {
                return baseChance;
            }

            var skillLevel = hero.GetSkillValue(skill);
            var skillBonus = skillLevel / 3f / 100f;

            return Math.Min(1f, Math.Max(0f, baseChance + skillBonus));
        }

        /// <summary>
        /// Performs a skill check and returns whether it succeeded.
        /// Uses CalculateSuccessChance internally and rolls against MBRandom.
        /// </summary>
        /// <param name="hero">The hero performing the check.</param>
        /// <param name="skill">The skill being tested.</param>
        /// <param name="baseChance">Base success chance (0.0 to 1.0). Default is 0.5 (50%).</param>
        /// <returns>True if the check succeeded, false otherwise.</returns>
        public static bool CheckSkill(Hero hero, SkillObject skill, float baseChance = 0.5f)
        {
            var chance = CalculateSuccessChance(hero, skill, baseChance);
            return MBRandom.RandomFloat < chance;
        }

        /// <summary>
        /// Checks if a hero meets a minimum skill threshold.
        /// Used to gate event options or unlock special outcomes.
        /// </summary>
        /// <param name="hero">The hero to check.</param>
        /// <param name="skill">The skill being checked.</param>
        /// <param name="threshold">Minimum skill level required.</param>
        /// <returns>True if the hero's skill level meets or exceeds the threshold.</returns>
        public static bool MeetsSkillThreshold(Hero hero, SkillObject skill, int threshold)
        {
            if (hero == null || skill == null)
            {
                return false;
            }

            return hero.GetSkillValue(skill) >= threshold;
        }

        /// <summary>
        /// Checks if a hero meets a minimum trait level threshold.
        /// </summary>
        /// <param name="hero">The hero to check.</param>
        /// <param name="trait">The trait being checked.</param>
        /// <param name="threshold">Minimum trait level required.</param>
        /// <returns>True if the hero's trait level meets or exceeds the threshold.</returns>
        public static bool MeetsTraitThreshold(Hero hero, TraitObject trait, int threshold)
        {
            if (hero == null || trait == null)
            {
                return false;
            }

            return hero.GetTraitLevel(trait) >= threshold;
        }

        /// <summary>
        /// Gets the skill bonus percentage for UI display.
        /// Shows how much a skill improves success chance above base.
        /// </summary>
        /// <param name="hero">The hero to check.</param>
        /// <param name="skill">The skill being checked.</param>
        /// <returns>Bonus percentage as an integer (e.g., 20 for +20%).</returns>
        public static int GetSkillBonusPercent(Hero hero, SkillObject skill)
        {
            if (hero == null || skill == null)
            {
                return 0;
            }

            var skillLevel = hero.GetSkillValue(skill);
            return skillLevel / 3;
        }

        /// <summary>
        /// Gets a skill object by its name string.
        /// Matches against both StringId and display Name.
        /// </summary>
        /// <param name="skillName">The name of the skill (e.g., "Scouting", "Medicine").</param>
        /// <returns>The SkillObject if found, null otherwise.</returns>
        public static SkillObject GetSkillByName(string skillName)
        {
            if (string.IsNullOrEmpty(skillName))
            {
                return null;
            }

            return Skills.All.FirstOrDefault(s =>
                s.StringId.Equals(skillName, StringComparison.OrdinalIgnoreCase) ||
                s.Name.ToString().Equals(skillName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Gets a trait object by its name string.
        /// Supports both native traits and personality traits.
        /// </summary>
        /// <param name="traitName">The name of the trait (e.g., "ScoutSkills", "Commander").</param>
        /// <returns>The TraitObject if found, null otherwise.</returns>
        public static TraitObject GetTraitByName(string traitName)
        {
            if (string.IsNullOrEmpty(traitName))
            {
                return null;
            }

            // Check role/profession traits
            if (traitName.Equals("Surgery", StringComparison.OrdinalIgnoreCase))
            {
                return DefaultTraits.Surgery;
            }

            if (traitName.Equals("ScoutSkills", StringComparison.OrdinalIgnoreCase))
            {
                return DefaultTraits.ScoutSkills;
            }

            if (traitName.Equals("RogueSkills", StringComparison.OrdinalIgnoreCase))
            {
                return DefaultTraits.RogueSkills;
            }

            if (traitName.Equals("Siegecraft", StringComparison.OrdinalIgnoreCase))
            {
                return DefaultTraits.Siegecraft;
            }

            if (traitName.Equals("Commander", StringComparison.OrdinalIgnoreCase))
            {
                return DefaultTraits.Commander;
            }

            if (traitName.Equals("SergeantCommandSkills", StringComparison.OrdinalIgnoreCase))
            {
                return DefaultTraits.SergeantCommandSkills;
            }

            // Check personality traits (Mercy, Valor, Honor, Generosity, Calculating)
            foreach (var trait in DefaultTraits.Personality)
            {
                if (trait.StringId.Equals(traitName, StringComparison.OrdinalIgnoreCase))
                {
                    return trait;
                }
            }

            return null;
        }

        /// <summary>
        /// Calculates the effective success chance for a skill check with multiple skills.
        /// Uses the highest relevant skill bonus.
        /// </summary>
        /// <param name="hero">The hero performing the check.</param>
        /// <param name="skills">Array of skills that could apply.</param>
        /// <param name="baseChance">Base success chance (0.0 to 1.0).</param>
        /// <returns>Success chance as a float between 0.0 and 1.0.</returns>
        public static float CalculateBestSkillChance(Hero hero, SkillObject[] skills, float baseChance = 0.5f)
        {
            if (hero == null || skills == null || skills.Length == 0)
            {
                return baseChance;
            }

            var bestBonus = 0f;
            foreach (var skill in skills)
            {
                if (skill == null)
                {
                    continue;
                }

                var skillLevel = hero.GetSkillValue(skill);
                var bonus = skillLevel / 3f / 100f;
                if (bonus > bestBonus)
                {
                    bestBonus = bonus;
                }
            }

            return Math.Min(1f, Math.Max(0f, baseChance + bestBonus));
        }

        /// <summary>
        /// Gets a hint string describing the skill requirement for an option.
        /// Used in UI tooltips to explain why an option is locked.
        /// </summary>
        /// <param name="skillName">The required skill name.</param>
        /// <param name="threshold">The required skill level.</param>
        /// <param name="currentLevel">The player's current skill level.</param>
        /// <returns>A formatted hint string.</returns>
        public static string GetSkillRequirementHint(string skillName, int threshold, int currentLevel)
        {
            if (currentLevel >= threshold)
            {
                return $"{skillName} {threshold}+ (You have {currentLevel})";
            }

            return $"Requires {skillName} {threshold}+ (You have {currentLevel})";
        }
    }
}

