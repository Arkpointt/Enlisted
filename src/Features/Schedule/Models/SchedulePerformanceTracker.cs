using System.Collections.Generic;
using Enlisted.Features.Schedule.Models;
using TaleWorlds.SaveSystem;

namespace Enlisted.Features.Schedule.Models
{
    /// <summary>
    /// Tracks player's schedule management performance for T5-T6 leaders.
    /// Used to calculate consequences and generate feedback at Pay Muster.
    /// Phase 5: Basic tracking structure for future consequence system.
    /// </summary>
    public class SchedulePerformanceTracker
    {
        /// <summary>Number of days lord's orders were fully met</summary>
        [SaveableProperty(1)]
        public int LordOrdersMetCount { get; set; }

        /// <summary>Number of days lord's orders were ignored/failed</summary>
        [SaveableProperty(2)]
        public int LordOrdersFailedCount { get; set; }

        /// <summary>Number of days a critical need was addressed</summary>
        [SaveableProperty(3)]
        public int CriticalNeedsAddressedCount { get; set; }

        /// <summary>Number of days a critical need was left unaddressed</summary>
        [SaveableProperty(4)]
        public int CriticalNeedsIgnoredCount { get; set; }

        /// <summary>Total fatigue accumulated by lance over cycle</summary>
        [SaveableProperty(5)]
        public int TotalFatigueAccumulated { get; set; }

        /// <summary>Number of successful duty completions</summary>
        [SaveableProperty(6)]
        public int SuccessfulDutiesCount { get; set; }

        /// <summary>Number of failed/skipped duties</summary>
        [SaveableProperty(7)]
        public int FailedDutiesCount { get; set; }

        /// <summary>Daily snapshots of lance needs (for trend analysis)</summary>
        [SaveableProperty(8)]
        public List<LanceNeedsSnapshot> DailyNeedsSnapshots { get; set; }

        /// <summary>Current cycle day being tracked (1-12)</summary>
        [SaveableProperty(9)]
        public int CurrentCycleDay { get; set; }

        public SchedulePerformanceTracker()
        {
            Reset();
        }

        /// <summary>
        /// Reset all tracking for new 12-day cycle.
        /// </summary>
        public void Reset()
        {
            LordOrdersMetCount = 0;
            LordOrdersFailedCount = 0;
            CriticalNeedsAddressedCount = 0;
            CriticalNeedsIgnoredCount = 0;
            TotalFatigueAccumulated = 0;
            SuccessfulDutiesCount = 0;
            FailedDutiesCount = 0;
            DailyNeedsSnapshots = new List<LanceNeedsSnapshot>();
            CurrentCycleDay = 1;
        }

        /// <summary>
        /// Record a daily needs snapshot for trend analysis.
        /// </summary>
        public void RecordDailySnapshot(int day, LanceNeedsState needs, LordObjective lordObjective)
        {
            if (needs == null)
                return;

            var snapshot = new LanceNeedsSnapshot
            {
                Day = day,
                Readiness = needs.Readiness,
                Equipment = needs.Equipment,
                Morale = needs.Morale,
                Rest = needs.Rest,
                Supplies = needs.Supplies,
                LordObjective = lordObjective.ToString()
            };

            DailyNeedsSnapshots.Add(snapshot);
        }

        /// <summary>
        /// Calculate overall performance score (0-100).
        /// Phase 5: Basic scoring algorithm.
        /// </summary>
        public int CalculatePerformanceScore()
        {
            // Phase 5: Simple weighted scoring
            // Future: More sophisticated calculation based on faction, campaign difficulty, etc.
            
            int score = 50; // Start neutral

            // Lord satisfaction (40% of score)
            int totalLordOrders = LordOrdersMetCount + LordOrdersFailedCount;
            if (totalLordOrders > 0)
            {
                float lordSatisfaction = (float)LordOrdersMetCount / totalLordOrders;
                score += (int)(lordSatisfaction * 40) - 20; // Range: -20 to +20
            }

            // Needs management (30% of score)
            int totalCriticalNeedEvents = CriticalNeedsAddressedCount + CriticalNeedsIgnoredCount;
            if (totalCriticalNeedEvents > 0)
            {
                float needsManagement = (float)CriticalNeedsAddressedCount / totalCriticalNeedEvents;
                score += (int)(needsManagement * 30) - 15; // Range: -15 to +15
            }

            // Duty completion (30% of score)
            int totalDuties = SuccessfulDutiesCount + FailedDutiesCount;
            if (totalDuties > 0)
            {
                float dutySuccess = (float)SuccessfulDutiesCount / totalDuties;
                score += (int)(dutySuccess * 30) - 15; // Range: -15 to +15
            }

            // Clamp to 0-100
            if (score < 0) score = 0;
            if (score > 100) score = 100;

            return score;
        }

        /// <summary>
        /// Get performance rating text based on score.
        /// </summary>
        public string GetPerformanceRating(int score)
        {
            if (score >= 90) return "Exemplary";
            if (score >= 75) return "Excellent";
            if (score >= 60) return "Good";
            if (score >= 45) return "Satisfactory";
            if (score >= 30) return "Poor";
            return "Unacceptable";
        }

        /// <summary>
        /// Check if performance warrants consequences (positive or negative).
        /// </summary>
        public bool ShouldTriggerConsequences(int score)
        {
            // Trigger consequences for exceptional (>75) or poor (<45) performance
            return score > 75 || score < 45;
        }
    }

    /// <summary>
    /// Snapshot of lance needs at a specific point in time.
    /// Used for performance trend analysis.
    /// </summary>
    public class LanceNeedsSnapshot
    {
        [SaveableProperty(1)] public int Day { get; set; }
        [SaveableProperty(2)] public int Readiness { get; set; }
        [SaveableProperty(3)] public int Equipment { get; set; }
        [SaveableProperty(4)] public int Morale { get; set; }
        [SaveableProperty(5)] public int Rest { get; set; }
        [SaveableProperty(6)] public int Supplies { get; set; }
        [SaveableProperty(7)] public string LordObjective { get; set; }
    }
}

