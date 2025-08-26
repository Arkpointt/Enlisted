using TaleWorlds.SaveSystem;

namespace Enlisted.Core.Models
{
    /// <summary>
    /// Tracks promotion progress including tier level and experience points.
    /// Handles XP accumulation and tier advancement state.
    /// </summary>
    public class PromotionState
    {
        [SaveableField(1)] private int _tier = 1;
        [SaveableField(2)] private int _currentXp = 0;
        [SaveableField(3)] private int _nextTierXp = 600; // default first tier requirement

        public int Tier => _tier;
        public int CurrentXp => _currentXp;
        public int NextTierXp => _nextTierXp;

        /// <summary>Ensure state is properly initialized with valid defaults.</summary>
        public void EnsureInitialized()
        {
            if (_nextTierXp <= 0) _nextTierXp = 600;
            if (_tier <= 0) _tier = 1;
            if (_currentXp < 0) _currentXp = 0;
        }

        /// <summary>Add experience points to current total.</summary>
        public void AddExperience(int amount)
        {
            if (amount > 0) _currentXp += amount;
        }

        /// <summary>Advance to next tier and consume required XP.</summary>
        public void AdvanceTier(int newTierRequirement)
        {
            _currentXp -= _nextTierXp;
            _tier++;
            _nextTierXp = newTierRequirement;
        }
    }
}
