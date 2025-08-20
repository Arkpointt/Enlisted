using Enlisted.Services.Abstractions;

namespace Enlisted.Services.Impl
{
    internal sealed class PromotionRulesImpl : IPromotionRules
    {
        public int GetRequiredXpForTier(int tier) => PromotionRules.GetRequiredXpForTier(tier);
        public int ComputeBattleXp(bool victory, int playerKills, int playerAssists, int enemyCount, int friendlyCount, bool playerKnockedOut)
            => PromotionRules.ComputeBattleXp(victory, playerKills, playerAssists, enemyCount, friendlyCount, playerKnockedOut);
    }
}
