using System;

namespace Enlisted
{
    /// <summary>
    /// Central thresholds and calculators for the promotion system.
    /// Keep all tuning here so behaviors remain simple and stable.
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

        // Phase 2 helper: compute battle XP (not wired in Phase 1)
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
