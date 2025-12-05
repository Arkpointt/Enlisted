using System;

namespace Enlisted.Features.CommandTent.Data
{
    /// <summary>
    /// Per-faction service history for the player. Each faction maintains separate records.
    /// </summary>
    [Serializable]
    public class FactionServiceRecord
    {
        /// <summary>Unique key: "kingdom_{StringId}", "minor_{StringId}", "merc_{StringId}", or "clan_{StringId}".</summary>
        public string FactionId { get; set; }

        /// <summary>Category: "kingdom", "minor", "merc", "clan", or "unknown".</summary>
        public string FactionType { get; set; }

        /// <summary>Display name for UI (e.g., "Kingdom of Vlandia").</summary>
        public string FactionDisplayName { get; set; }

        /// <summary>Full contract terms completed (not early discharge/desertion).</summary>
        public int TermsCompleted { get; set; }

        /// <summary>Total days served across all enlistments with this faction.</summary>
        public int TotalDaysServed { get; set; }

        /// <summary>Peak tier (1-6) achieved with this faction.</summary>
        public int HighestTier { get; set; }

        /// <summary>Battles fought while serving this faction.</summary>
        public int BattlesFought { get; set; }

        /// <summary>Unique lords served under in this faction.</summary>
        public int LordsServed { get; set; }

        /// <summary>Times enlisted with this faction.</summary>
        public int Enlistments { get; set; }

        /// <summary>Enemies killed while serving this faction.</summary>
        public int TotalKills { get; set; }

        public FactionServiceRecord()
        {
            FactionId = string.Empty;
            FactionType = "unknown";
            FactionDisplayName = string.Empty;
        }

        public FactionServiceRecord(string factionId, string factionType, string displayName)
        {
            FactionId = factionId ?? string.Empty;
            FactionType = factionType ?? "unknown";
            FactionDisplayName = displayName ?? string.Empty;
        }

        /// <summary>Updates highest tier if <paramref name="currentTier"/> exceeds current record.</summary>
        public void UpdateHighestTier(int currentTier)
        {
            if (currentTier > HighestTier)
            {
                HighestTier = currentTier;
            }
        }

        public override string ToString()
        {
            return $"[{FactionId}] Terms:{TermsCompleted} Days:{TotalDaysServed} " +
                   $"MaxTier:{HighestTier} Battles:{BattlesFought} Kills:{TotalKills}";
        }
    }
}
