using System;

namespace Enlisted.Features.Promotion.Domain
{
    /// <summary>
    /// Central thresholds and calculators for the promotion system.
    /// Keep all tuning here so behaviors remain simple and stable.
    /// Config-driven approach following blueprint principles.
    /// </summary>
    public static class PromotionRules
    {
        // XP curve: 600, 900, 1350, 2025, ... (1.5x growth)
        public static int GetRequiredXpForTier(int tier)
        {
            const int baseXp = 600;
            const double growth = 1.5;
            return (int)Math.Round(baseXp * Math.Pow(growth, Math.Max(0, tier)));
        }

        /// <summary>
        /// Compute battle XP based on participation and performance.
        /// Ready for Phase 2 integration with battle systems.
        /// </summary>
        public static int ComputeBattleXp(bool victory, int playerKills, int playerAssists, int enemyCount, int friendlyCount, bool playerKnockedOut)
        {
            int baseXp = 100; // participation
            int sizeFactor = Math.Max(0, (enemyCount + friendlyCount)) / 50; // +10 per ~50 total troops
            int victoryBonus = victory ? 50 : 0;
            int performance = (playerKills * 15) + (playerAssists * 8);
            int survival = playerKnockedOut ? -25 : 25;

            int xp = baseXp + (sizeFactor * 10) + victoryBonus + performance + survival;
            return Math.Max(0, xp);
        }
    }
}
