using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;

namespace Enlisted.Features.Identity
{
    /// <summary>
    ///     Utility methods for working with the native trait system.
    ///     Provides simplified access to trait levels, XP awards, and specialization detection.
    ///     Traits represent hidden character development (0-20 scale) that unlock specialist roles
    ///     and affect event outcomes.
    /// </summary>
    public static class TraitHelper
    {
        /// <summary>
        ///     Gets the current level of a trait for a hero.
        ///     Personality traits range from -2 to +2, hidden traits range from 0 to 20.
        /// </summary>
        /// <param name="hero">The hero to check</param>
        /// <param name="trait">The trait to query</param>
        /// <returns>Current trait level</returns>
        public static int GetTraitLevel(Hero hero, TraitObject trait)
        {
            return hero.GetTraitLevel(trait);
        }

        /// <summary>
        ///     Awards trait XP to a hero, potentially leveling up the trait.
        ///     Uses the native trait leveling system which handles XP accumulation and level progression.
        ///     This is a wrapper around TraitLevelingHelper for consistency with the mod's architecture.
        /// </summary>
        /// <param name="hero">The hero receiving XP (currently only supports MainHero)</param>
        /// <param name="trait">The trait to award XP to</param>
        /// <param name="xp">Amount of XP to award (can be negative for penalties)</param>
        public static void AwardTraitXP(Hero hero, TraitObject trait, int xp)
        {
            // Native API only supports player trait XP through TraitLevelingHelper
            // which internally uses Campaign.Current.PlayerTraitDeveloper
            if (hero == Hero.MainHero)
            {
                TraitLevelingHelper.OnIncidentResolved(trait, xp);
            }
        }

        /// <summary>
        ///     Determines the hero's primary specialization based on their highest trait level.
        ///     Returns the specialization name if any trait is at level 10 or higher,
        ///     otherwise returns "Soldier" as the default.
        /// </summary>
        /// <param name="hero">The hero to evaluate</param>
        /// <returns>Primary specialization name: Commander, Surgeon, Scout, Rogue, Sergeant, Engineer, or Soldier</returns>
        public static string GetPrimarySpecialization(Hero hero)
        {
            var traits = new Dictionary<string, int>
            {
                ["Commander"] = GetTraitLevel(hero, DefaultTraits.Commander),
                ["Surgeon"] = GetTraitLevel(hero, DefaultTraits.Surgery),
                ["Scout"] = GetTraitLevel(hero, DefaultTraits.ScoutSkills),
                ["Rogue"] = GetTraitLevel(hero, DefaultTraits.RogueSkills),
                ["Sergeant"] = GetTraitLevel(hero, DefaultTraits.SergeantCommandSkills),
                ["Engineer"] = GetTraitLevel(hero, DefaultTraits.Siegecraft)
            };

            var highest = traits.OrderByDescending(x => x.Value).First();

            if (highest.Value >= 10)
            {
                return highest.Key;
            }

            return "Soldier"; // Default for unspecialized troops
        }
    }
}

