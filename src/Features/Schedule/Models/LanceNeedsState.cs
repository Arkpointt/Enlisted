using System;
using TaleWorlds.Library;

namespace Enlisted.Features.Schedule.Models
{
    /// <summary>
    /// Tracks the current state of all five lance needs.
    /// Degrades daily based on activities and recovers through appropriate assignments.
    /// Used primarily for T5-T6 lance management gameplay.
    /// NOTE: Serialization is handled manually in ScheduleBehavior.SyncData()
    /// </summary>
    public class LanceNeedsState
    {
        /// <summary>Combat readiness (0-100)</summary>
        public int Readiness { get; set; }
        
        /// <summary>Equipment condition (0-100)</summary>
        public int Equipment { get; set; }
        
        /// <summary>Morale level (0-100)</summary>
        public int Morale { get; set; }
        
        /// <summary>Rest level (0-100)</summary>
        public int Rest { get; set; }
        
        /// <summary>Supplies level (0-100)</summary>
        public int Supplies { get; set; }

        /// <summary>Thresholds for need levels</summary>
        public const int ExcellentThreshold = 80;
        public const int GoodThreshold = 60;
        public const int FairThreshold = 40;
        public const int PoorThreshold = 30;
        public const int CriticalThreshold = 20;

        public LanceNeedsState()
        {
            // Initialize all needs at "Good" level (60%)
            Readiness = 60;
            Equipment = 60;
            Morale = 60;
            Rest = 60;
            Supplies = 60;
        }

        /// <summary>
        /// Get the value of a specific need.
        /// </summary>
        public int GetNeed(LanceNeed need)
        {
            return need switch
            {
                LanceNeed.Readiness => Readiness,
                LanceNeed.Equipment => Equipment,
                LanceNeed.Morale => Morale,
                LanceNeed.Rest => Rest,
                LanceNeed.Supplies => Supplies,
                _ => 0
            };
        }

        /// <summary>
        /// Set the value of a specific need (clamped to 0-100).
        /// </summary>
        public void SetNeed(LanceNeed need, int value)
        {
            value = (int)MathF.Clamp(value, 0, 100);
            
            switch (need)
            {
                case LanceNeed.Readiness:
                    Readiness = value;
                    break;
                case LanceNeed.Equipment:
                    Equipment = value;
                    break;
                case LanceNeed.Morale:
                    Morale = value;
                    break;
                case LanceNeed.Rest:
                    Rest = value;
                    break;
                case LanceNeed.Supplies:
                    Supplies = value;
                    break;
            }
        }

        /// <summary>
        /// Modify a need by a delta amount (clamped to 0-100).
        /// </summary>
        public void ModifyNeed(LanceNeed need, int delta)
        {
            int current = GetNeed(need);
            SetNeed(need, current + delta);
        }

        /// <summary>
        /// Get the status level for a need (Excellent, Good, Fair, Poor, Critical).
        /// </summary>
        public string GetNeedStatus(LanceNeed need)
        {
            int value = GetNeed(need);
            
            if (value >= ExcellentThreshold)
                return "Excellent";
            else if (value >= GoodThreshold)
                return "Good";
            else if (value >= FairThreshold)
                return "Fair";
            else if (value >= PoorThreshold)
                return "Poor";
            else
                return "Critical";
        }

        /// <summary>
        /// Check if any need is at critical level (below 20%).
        /// </summary>
        public bool HasCriticalNeeds()
        {
            return Readiness < CriticalThreshold ||
                   Equipment < CriticalThreshold ||
                   Morale < CriticalThreshold ||
                   Rest < CriticalThreshold ||
                   Supplies < CriticalThreshold;
        }

        /// <summary>
        /// Get the most critical need (lowest value).
        /// </summary>
        public LanceNeed GetMostCriticalNeed()
        {
            int minValue = 100;
            LanceNeed criticalNeed = LanceNeed.Readiness;
            
            foreach (LanceNeed need in Enum.GetValues(typeof(LanceNeed)))
            {
                int value = GetNeed(need);
                if (value < minValue)
                {
                    minValue = value;
                    criticalNeed = need;
                }
            }
            
            return criticalNeed;
        }

        /// <summary>
        /// Get overall lance health score (average of all needs, 0-100).
        /// </summary>
        public int GetOverallHealth()
        {
            return (Readiness + Equipment + Morale + Rest + Supplies) / 5;
        }
    }
}

