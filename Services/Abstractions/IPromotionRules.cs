namespace Enlisted.Services.Abstractions
{
    /// <summary>
    /// Contract exposing promotion thresholds and XP computation.
    /// A wrapper abstraction around the static rules so behaviors depend on an interface.
    /// </summary>
    public interface IPromotionRules
    {
        /// <summary>
        /// Get the XP required to reach the specified tier from the previous tier.
        /// </summary>
        int GetRequiredXpForTier(int tier);

        /// <summary>
        /// Compute XP for a battle given result and performance. Inputs should be cheap to obtain.
        /// </summary>
        int ComputeBattleXp(bool victory, int playerKills, int playerAssists, int enemyCount, int friendlyCount, bool playerKnockedOut);
    }
}
