using System;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Mod.Core.Config;
using TaleWorlds.Localization;

namespace Enlisted.Features.Ranks
{
    /// <summary>
    ///     Provides culture-specific military rank titles based on tier and faction.
    ///     Reads rank names from progression_config.json for localization and easy customization.
    /// </summary>
    public static class RankHelper
    {
        /// <summary>
        ///     Get the culture-specific rank title for a given tier.
        ///     Reads from progression_config.json culture_ranks section for proper localization.
        /// </summary>
        /// <param name="tier">Military tier (1-9)</param>
        /// <param name="cultureId">Culture string ID (e.g., "empire", "vlandia")</param>
        /// <returns>Localized rank title, or fallback to Mercenary ranks</returns>
        public static string GetRankTitle(int tier, string cultureId)
        {
            // Clamp tier to valid range
            tier = Math.Max(1, Math.Min(9, tier));

            // Get the raw rank string from config (may contain localization ID like "{=id}Text")
            var rawRank = ConfigurationManager.GetCultureRankTitle(tier, cultureId);

            // Process through TextObject to resolve localization IDs
            if (!string.IsNullOrEmpty(rawRank) && rawRank.Contains("{="))
            {
                try
                {
                    var textObj = new TextObject(rawRank);
                    return textObj.ToString();
                }
                catch
                {
                    // If localization fails, return the raw text (strip the ID part)
                    var closeBrace = rawRank.IndexOf('}');
                    if (closeBrace >= 0 && closeBrace < rawRank.Length - 1)
                    {
                        return rawRank.Substring(closeBrace + 1);
                    }
                }
            }

            return rawRank ?? "Soldier";
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
        ///     Uses Tier 8 rank from progression_config.json for consistency.
        /// </summary>
        public static string GetCommanderTitle(string cultureId)
        {
            // Use tier 8 (Commander track) as the "commander title"
            return GetRankTitle(8, cultureId);
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

