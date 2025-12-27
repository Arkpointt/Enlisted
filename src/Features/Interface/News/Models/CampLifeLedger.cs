using System;

namespace Enlisted.Features.Interface.News.Models
{
    /// <summary>
    /// One-day aggregate used by <see cref="CampLifeLedger"/>.
    /// Stored as primitives only (save-safe).
    /// </summary>
    public struct DailyAggregate
    {
        public bool HasValue;
        public int DayNumber;

        public int LostToday;
        public int WoundedToday;
        public int SickToday;
        public int TrainingIncidentsToday;
        public int DecisionsResolvedToday;
        public int HighSignalDispatchesToday;

        public static DailyAggregate CreateEmpty(int dayNumber)
        {
            return new DailyAggregate
            {
                HasValue = true,
                DayNumber = dayNumber,
                LostToday = 0,
                WoundedToday = 0,
                SickToday = 0,
                TrainingIncidentsToday = 0,
                DecisionsResolvedToday = 0,
                HighSignalDispatchesToday = 0
            };
        }
    }

    public readonly struct CampLifeRollingTotals
    {
        public CampLifeRollingTotals(
            int lost,
            int wounded,
            int sick,
            int trainingIncidents,
            int decisionsResolved,
            int highSignalDispatches)
        {
            Lost = lost;
            Wounded = wounded;
            Sick = sick;
            TrainingIncidents = trainingIncidents;
            DecisionsResolved = decisionsResolved;
            HighSignalDispatches = highSignalDispatches;
        }

        public int Lost { get; }
        public int Wounded { get; }
        public int Sick { get; }
        public int TrainingIncidents { get; }
        public int DecisionsResolved { get; }
        public int HighSignalDispatches { get; }
    }

    /// <summary>
    /// Rolling day ledger (ring buffer) for Camp Life stats.
    /// - Cadence: one entry per in-game day
    /// - Size: fixed (default 30) with overwrite of oldest
    /// - Storage: primitives only (save-safe)
    /// </summary>
    public sealed class CampLifeLedger
    {
        public int Capacity { get; set; } = 30;
        public int HeadIndex { get; set; }
        public DailyAggregate[] Days { get; set; } = Array.Empty<DailyAggregate>();

        public void EnsureInitialized()
        {
            Capacity = Math.Max(1, Capacity);

            if (Days == null || Days.Length != Capacity)
            {
                var newDays = new DailyAggregate[Capacity];
                var copyCount = Days == null ? 0 : Math.Min(Days.Length, newDays.Length);
                if (copyCount > 0 && Days != null)
                {
                    Array.Copy(Days, 0, newDays, 0, copyCount);
                }

                Days = newDays;
                HeadIndex = ClampIndex(HeadIndex);
            }
        }

        public void RecordDay(DailyAggregate aggregate)
        {
            EnsureInitialized();

            if (Days.Length == 0)
            {
                return;
            }

            aggregate.HasValue = true;
            Days[HeadIndex] = aggregate;
            HeadIndex = (HeadIndex + 1) % Days.Length;
        }

        public bool TryGetDay(int dayNumber, out DailyAggregate aggregate)
        {
            EnsureInitialized();

            if (Days.Length == 0)
            {
                aggregate = default;
                return false;
            }

            for (var i = 0; i < Days.Length; i++)
            {
                var item = Days[i];
                if (item.HasValue && item.DayNumber == dayNumber)
                {
                    aggregate = item;
                    return true;
                }
            }

            aggregate = default;
            return false;
        }

        public CampLifeRollingTotals GetRollingTotals(int windowDays)
        {
            EnsureInitialized();

            if (Days.Length == 0)
            {
                return new CampLifeRollingTotals(0, 0, 0, 0, 0, 0);
            }

            windowDays = Math.Max(1, Math.Min(windowDays, Days.Length));

            var lost = 0;
            var wounded = 0;
            var sick = 0;
            var incidents = 0;
            var decisions = 0;
            var dispatches = 0;

            // HeadIndex points to the next write slot. The newest item is the previous slot.
            for (var offset = 1; offset <= windowDays; offset++)
            {
                var idx = HeadIndex - offset;
                if (idx < 0)
                {
                    idx += Days.Length;
                }

                var item = Days[idx];
                if (!item.HasValue)
                {
                    continue;
                }

                lost += item.LostToday;
                wounded += item.WoundedToday;
                sick += item.SickToday;
                incidents += item.TrainingIncidentsToday;
                decisions += item.DecisionsResolvedToday;
                dispatches += item.HighSignalDispatchesToday;
            }

            return new CampLifeRollingTotals(lost, wounded, sick, incidents, decisions, dispatches);
        }

        private int ClampIndex(int idx)
        {
            if (Days == null || Days.Length == 0)
            {
                return 0;
            }

            if (idx < 0)
            {
                return 0;
            }

            return idx >= Days.Length ? idx % Days.Length : idx;
        }
    }
}


