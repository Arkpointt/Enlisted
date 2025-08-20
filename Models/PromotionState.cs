using TaleWorlds.SaveSystem;

namespace Enlisted.Models
{
    /// <summary>
    /// Saveable enlisted progression state (Phase 1: XP-only).
    /// </summary>
    public class PromotionState
    {
        // 0 = Recruit tier by default
        [SaveableField(1)] public int Tier;
        [SaveableField(2)] public int CurrentXp;
        [SaveableField(3)] public int NextTierXp;

        public void EnsureInitialized()
        {
            if (NextTierXp <= 0)
            {
                NextTierXp = Enlisted.PromotionRules.GetRequiredXpForTier(Tier);
            }
        }
    }
}
