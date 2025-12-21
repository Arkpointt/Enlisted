using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;

namespace Enlisted.Features.Retinue.Data
{
    /// <summary>
    /// Tracks the player's personal retinue state including soldier type, counts, and replenishment tracking.
    /// This is the single source of truth for all retinue data.
    /// </summary>
    [Serializable]
    public class RetinueState
    {
        /// <summary>
        /// The soldier type the player selected ("infantry", "archers", "cavalry", "horse_archers").
        /// Null or empty if no type selected yet.
        /// </summary>
        public string SelectedTypeId { get; set; }

        /// <summary>
        /// Tracks which specific CharacterObjects we added and how many.
        /// Key = CharacterObject.StringId, Value = count.
        /// This allows us to distinguish retinue soldiers from other troops in the party.
        /// </summary>
        public Dictionary<string, int> TroopCounts { get; set; }

        /// <summary>
        /// Days elapsed since last free trickle replenishment.
        /// Resets to 0 when a soldier is added via trickle.
        /// </summary>
        public int DaysSinceLastTrickle { get; set; }

        /// <summary>
        /// Campaign time when instant requisition cooldown ends.
        /// Player can requisition again when CampaignTime.Now >= this value.
        /// </summary>
        public CampaignTime RequisitionCooldownEnd { get; set; }

        /// <summary>
        /// Total number of soldiers currently in the retinue.
        /// Computed from TroopCounts dictionary.
        /// </summary>
        public int TotalSoldiers => TroopCounts?.Values.Sum() ?? 0;

        /// <summary>
        /// Returns true if the player has an active retinue (type selected and soldiers present).
        /// </summary>
        public bool HasRetinue => !string.IsNullOrEmpty(SelectedTypeId) && TotalSoldiers > 0;

        /// <summary>
        /// Returns true if a soldier type has been selected, even if no soldiers are present.
        /// Used to determine if trickle replenishment should occur.
        /// </summary>
        public bool HasTypeSelected => !string.IsNullOrEmpty(SelectedTypeId);

        public RetinueState()
        {
            TroopCounts = new Dictionary<string, int>();
            RequisitionCooldownEnd = CampaignTime.Zero;
        }

        /// <summary>
        /// Clears all retinue state. Called on capture, enlistment end, army defeat, or type change.
        /// </summary>
        public void Clear()
        {
            TroopCounts?.Clear();
            SelectedTypeId = null;
            DaysSinceLastTrickle = 0;
            // Note: RequisitionCooldownEnd is preserved across clears to prevent exploit
        }

        /// <summary>
        /// Updates troop count for a specific character. Removes entry if count reaches zero.
        /// </summary>
        /// <param name="characterId">The CharacterObject.StringId</param>
        /// <param name="delta">Amount to add (positive) or remove (negative)</param>
        /// <returns>New count for this character type</returns>
        public int UpdateTroopCount(string characterId, int delta)
        {
            if (string.IsNullOrEmpty(characterId))
            {
                return 0;
            }

            TroopCounts ??= new Dictionary<string, int>();

            if (!TroopCounts.TryGetValue(characterId, out var current))
            {
                current = 0;
            }

            var newCount = Math.Max(0, current + delta);

            if (newCount > 0)
            {
                TroopCounts[characterId] = newCount;
            }
            else
            {
                TroopCounts.Remove(characterId);
            }

            return newCount;
        }

        /// <summary>
        /// Gets the count of a specific troop type in the retinue.
        /// </summary>
        public int GetTroopCount(string characterId)
        {
            if (string.IsNullOrEmpty(characterId) || TroopCounts == null)
            {
                return 0;
            }

            return TroopCounts.TryGetValue(characterId, out var count) ? count : 0;
        }

        /// <summary>
        /// Checks if the requisition cooldown has elapsed.
        /// </summary>
        public bool IsRequisitionAvailable()
        {
            return RequisitionCooldownEnd.IsPast || RequisitionCooldownEnd == CampaignTime.Zero;
        }

        /// <summary>
        /// Gets remaining days until requisition is available.
        /// </summary>
        public int GetRequisitionCooldownDays()
        {
            if (IsRequisitionAvailable())
            {
                return 0;
            }

            var remaining = RequisitionCooldownEnd.RemainingDaysFromNow;
            return (int)Math.Ceiling(remaining);
        }

        public override string ToString()
        {
            return $"Retinue[Type={SelectedTypeId ?? "none"}, Soldiers={TotalSoldiers}, " +
                   $"TrickleDays={DaysSinceLastTrickle}, ReqCooldown={GetRequisitionCooldownDays()}d]";
        }
    }
}

