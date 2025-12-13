using System;
using System.Collections.Generic;
using Enlisted.Features.Enlistment.Behaviors;

namespace Enlisted.Features.Ranks
{
    /// <summary>
    ///     Provides culture-specific military rank titles based on tier and faction.
    ///     Each faction has unique rank names that replace generic "Tier X" references.
    /// </summary>
    public static class RankHelper
    {
        // Culture-specific rank titles for all 9 tiers
        // T1-4: Enlisted ranks, T5-6: Officer ranks, T7-9: Commander ranks
        private static readonly Dictionary<string, Dictionary<int, string>> CultureRanks = new Dictionary<string, Dictionary<int, string>>
        {
            ["empire"] = new Dictionary<int, string>
            {
                [1] = "Tiro",
                [2] = "Miles",
                [3] = "Immunes",
                [4] = "Principalis",
                [5] = "Evocatus",
                [6] = "Centurion",
                [7] = "Primus Pilus",
                [8] = "Tribune",
                [9] = "Legate"
            },
            ["vlandia"] = new Dictionary<int, string>
            {
                [1] = "Peasant",
                [2] = "Levy",
                [3] = "Footman",
                [4] = "Man-at-Arms",
                [5] = "Sergeant",
                [6] = "Knight Bachelor",
                [7] = "Cavalier",
                [8] = "Banneret",
                [9] = "Castellan"
            },
            ["sturgia"] = new Dictionary<int, string>
            {
                [1] = "Thrall",
                [2] = "Ceorl",
                [3] = "Fyrdman",
                [4] = "Drengr",
                [5] = "Huskarl",
                [6] = "Varangian",
                [7] = "Champion",
                [8] = "Thane",
                [9] = "High Warlord"
            },
            ["khuzait"] = new Dictionary<int, string>
            {
                [1] = "Outsider",
                [2] = "Nomad",
                [3] = "Noker",
                [4] = "Warrior",
                [5] = "Veteran",
                [6] = "Bahadur",
                [7] = "Arban",
                [8] = "Zuun",
                [9] = "Noyan"
            },
            ["battania"] = new Dictionary<int, string>
            {
                [1] = "Woodrunner",
                [2] = "Clan Warrior",
                [3] = "Skirmisher",
                [4] = "Raider",
                [5] = "Oathsworn",
                [6] = "Fian",
                [7] = "Highland Champion",
                [8] = "Clan Chief",
                [9] = "High King's Guard"
            },
            ["aserai"] = new Dictionary<int, string>
            {
                [1] = "Tribesman",
                [2] = "Skirmisher",
                [3] = "Footman",
                [4] = "Veteran",
                [5] = "Guard",
                [6] = "Faris",
                [7] = "Emir's Chosen",
                [8] = "Sheikh",
                [9] = "Grand Vizier"
            },
            ["mercenary"] = new Dictionary<int, string>
            {
                [1] = "Follower",
                [2] = "Recruit",
                [3] = "Free Sword",
                [4] = "Veteran",
                [5] = "Blade",
                [6] = "Chosen",
                [7] = "Captain",
                [8] = "Commander",
                [9] = "Marshal"
            }
        };

        /// <summary>
        ///     Get the culture-specific rank title for a given tier.
        /// </summary>
        /// <param name="tier">Military tier (1-9)</param>
        /// <param name="cultureId">Culture string ID (e.g., "empire", "vlandia")</param>
        /// <returns>Localized rank title, or fallback to Mercenary ranks</returns>
        public static string GetRankTitle(int tier, string cultureId)
        {
            // Clamp tier to valid range
            tier = Math.Max(1, Math.Min(9, tier));

            var culture = (cultureId ?? "mercenary").ToLowerInvariant();

            // Try exact culture match
            if (CultureRanks.TryGetValue(culture, out var ranks))
            {
                if (ranks.TryGetValue(tier, out var title))
                {
                    return title;
                }
            }

            // Fallback to mercenary ranks for unknown cultures
            if (CultureRanks["mercenary"].TryGetValue(tier, out var fallback))
            {
                return fallback;
            }

            return "Soldier";
        }

        /// <summary>
        ///     Get the player's current culture-specific rank title.
        /// </summary>
        public static string GetCurrentRank(EnlistmentBehavior enlistment)
        {
            if (enlistment == null)
            {
                return "Soldier";
            }

            var tier = enlistment.EnlistmentTier;
            var culture = enlistment.EnlistedLord?.Culture?.StringId ?? "mercenary";
            return GetRankTitle(tier, culture);
        }

        /// <summary>
        ///     Get the player's next rank title (for promotion previews).
        /// </summary>
        public static string GetNextRank(EnlistmentBehavior enlistment)
        {
            if (enlistment == null)
            {
                return "Soldier";
            }

            var tier = Math.Min(enlistment.EnlistmentTier + 1, 9);
            var culture = enlistment.EnlistedLord?.Culture?.StringId ?? "mercenary";
            return GetRankTitle(tier, culture);
        }

        /// <summary>
        ///     Get the culture-specific commander/officer title (used in some events).
        /// </summary>
        public static string GetCommanderTitle(string cultureId)
        {
            var culture = (cultureId ?? "mercenary").ToLowerInvariant();

            return culture switch
            {
                "empire" => "Tribune",
                "vlandia" => "Captain",
                "sturgia" => "Thane",
                "khuzait" => "Noyan",
                "battania" => "Clan Chief",
                "aserai" => "Sheikh",
                _ => "Captain"
            };
        }

        /// <summary>
        ///     Get the culture ID from an enlistment, with fallback.
        /// </summary>
        public static string GetCultureId(EnlistmentBehavior enlistment)
        {
            return enlistment?.EnlistedLord?.Culture?.StringId ?? "mercenary";
        }
    }
}

