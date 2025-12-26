using System;

namespace Enlisted.Features.Conversations.Data
{
    /// <summary>
    /// Context conditions for selecting quartermaster dialogue nodes.
    /// All fields are nullable - null means "any value matches".
    /// Used for both matching actual game state and specifying requirements in JSON.
    /// </summary>
    public class QMDialogueContext
    {
        // Supply state from CompanySupplyManager
        public string SupplyLevel { get; set; } // excellent/good/fair/low/critical

        // QM personality and relationship from QuartermasterManager
        public string Archetype { get; set; } // veteran/merchant/bookkeeper/scoundrel/believer/eccentric
        public string ReputationTier { get; set; } // hostile/wary/neutral/friendly/trusted
        public string PlayerStyle { get; set; } // direct/military/friendly/flattering
        public bool? IsIntroduced { get; set; } // First meeting flag

        // Player tier and role from EnlistmentBehavior
        public int? PlayerTier { get; set; } // Current player tier (1-9) in actual context
        public int? TierMin { get; set; } // Minimum tier requirement in node context
        public int? TierMax { get; set; } // Maximum tier requirement in node context
        public string TierCategory { get; set; } // enlisted/nco/officer
        public string Formation { get; set; } // infantry/cavalry/archers
        public bool? IsCavalry { get; set; }
        public bool? IsOfficer { get; set; }

        // Recent events from EnlistedNewsBehavior
        public string RecentEvent { get; set; } // battle/siege/march/resupply/none
        public bool? HasRecentBattle { get; set; }
        public bool? HighCasualties { get; set; }

        // Escalation flags from EscalationManager
        public string HasFlag { get; set; }

        // Personal tracking
        public int? DaysEnlisted { get; set; }
        public bool? RecentlyPromoted { get; set; }
        public string LastPurchaseCategory { get; set; } // weapons/armor/provisions

        // Baggage access context
        public string BaggageRequestType { get; set; } // emergency/locked/none

        /// <summary>
        /// Checks if this context matches an actual game context.
        /// Returns true if all non-null fields in this context match the corresponding fields in the actual context.
        /// This implements specificity-based matching: more conditions = more specific = higher priority.
        /// </summary>
        public bool Matches(QMDialogueContext actual)
        {
            if (actual == null)
            {
                return false;
            }

            // Check each field - null in this context means "any value matches"
            if (SupplyLevel != null && !SupplyLevel.Equals(actual.SupplyLevel, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (Archetype != null && !Archetype.Equals(actual.Archetype, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (ReputationTier != null && !ReputationTier.Equals(actual.ReputationTier, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (PlayerStyle != null && !PlayerStyle.Equals(actual.PlayerStyle, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (IsIntroduced.HasValue && IsIntroduced.Value != actual.IsIntroduced.GetValueOrDefault())
            {
                return false;
            }

            // Tier range check - node context defines a range, actual context has player's current tier
            var actualTier = actual.PlayerTier ?? 1; // Default to tier 1 if not set

            if (TierMin.HasValue && actualTier < TierMin.Value)
            {
                return false;
            }

            if (TierMax.HasValue && actualTier > TierMax.Value)
            {
                return false;
            }

            if (TierCategory != null && !TierCategory.Equals(actual.TierCategory, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (Formation != null && !Formation.Equals(actual.Formation, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (IsCavalry.HasValue && IsCavalry.Value != actual.IsCavalry.GetValueOrDefault())
            {
                return false;
            }

            if (IsOfficer.HasValue && IsOfficer.Value != actual.IsOfficer.GetValueOrDefault())
            {
                return false;
            }

            if (RecentEvent != null && !RecentEvent.Equals(actual.RecentEvent, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (HasRecentBattle.HasValue && HasRecentBattle.Value != actual.HasRecentBattle.GetValueOrDefault())
            {
                return false;
            }

            if (HighCasualties.HasValue && HighCasualties.Value != actual.HighCasualties.GetValueOrDefault())
            {
                return false;
            }

            if (HasFlag != null && !HasFlag.Equals(actual.HasFlag, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (DaysEnlisted.HasValue)
            {
                var actualDays = actual.DaysEnlisted.GetValueOrDefault(0);
                if (actualDays < DaysEnlisted.Value)
                {
                    return false;
                }
            }

            if (RecentlyPromoted.HasValue && RecentlyPromoted.Value != actual.RecentlyPromoted.GetValueOrDefault())
            {
                return false;
            }

            if (LastPurchaseCategory != null && !LastPurchaseCategory.Equals(actual.LastPurchaseCategory, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (BaggageRequestType != null && !BaggageRequestType.Equals(actual.BaggageRequestType, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // All checks passed
            return true;
        }

        /// <summary>
        /// Counts how many non-null fields this context has.
        /// Used for sorting by specificity - more conditions = more specific = higher priority.
        /// </summary>
        public int GetSpecificity()
        {
            var count = 0;

            if (SupplyLevel != null)
            {
                count++;
            }
            if (Archetype != null)
            {
                count++;
            }
            if (ReputationTier != null)
            {
                count++;
            }
            if (PlayerStyle != null)
            {
                count++;
            }
            if (IsIntroduced.HasValue)
            {
                count++;
            }
            if (PlayerTier.HasValue)
            {
                count++;
            }
            if (TierMin.HasValue)
            {
                count++;
            }
            if (TierMax.HasValue)
            {
                count++;
            }
            if (TierCategory != null)
            {
                count++;
            }
            if (Formation != null)
            {
                count++;
            }
            if (IsCavalry.HasValue)
            {
                count++;
            }
            if (IsOfficer.HasValue)
            {
                count++;
            }
            if (RecentEvent != null)
            {
                count++;
            }
            if (HasRecentBattle.HasValue)
            {
                count++;
            }
            if (HighCasualties.HasValue)
            {
                count++;
            }
            if (HasFlag != null)
            {
                count++;
            }
            if (DaysEnlisted.HasValue)
            {
                count++;
            }
            if (RecentlyPromoted.HasValue)
            {
                count++;
            }
            if (LastPurchaseCategory != null)
            {
                count++;
            }
            if (BaggageRequestType != null)
            {
                count++;
            }

            return count;
        }
    }
}

