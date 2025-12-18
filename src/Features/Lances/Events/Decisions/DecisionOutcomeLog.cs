using System;
using TaleWorlds.CampaignSystem;

namespace Enlisted.Features.Lances.Events.Decisions
{
    /// <summary>
    /// Save-safe ring buffer of recent resolved decision outcomes (strings/ids only).
    /// Used by Reports (“Recent Outcomes”) and to ground the next day’s Daily Report.
    /// </summary>
    public sealed class DecisionOutcomeLog
    {
        public int Capacity { get; set; } = 20;
        public int HeadIndex { get; set; }
        public DecisionOutcomeRecord[] Items { get; set; } = Array.Empty<DecisionOutcomeRecord>();

        public void EnsureInitialized()
        {
            Capacity = Math.Max(1, Math.Min(Capacity, 60));

            if (Items == null || Items.Length != Capacity)
            {
                var next = new DecisionOutcomeRecord[Capacity];
                var copyCount = Items == null ? 0 : Math.Min(Items.Length, next.Length);
                if (copyCount > 0)
                {
                    Array.Copy(Items, 0, next, 0, copyCount);
                }

                Items = next;
                HeadIndex = ClampIndex(HeadIndex, Capacity);
            }

            for (var i = 0; i < Items.Length; i++)
            {
                Items[i] ??= new DecisionOutcomeRecord();
            }
        }

        public void Append(DecisionOutcomeRecord record)
        {
            if (record == null)
            {
                return;
            }

            EnsureInitialized();
            record.Normalize();

            Items[HeadIndex] = record;
            HeadIndex = (HeadIndex + 1) % Items.Length;
        }

        public void SyncData(IDataStore dataStore, string prefix)
        {
            EnsureInitialized();

            // IDataStore.SyncData requires a ref to a variable, not a property.
            var cap = Capacity;
            dataStore.SyncData($"{prefix}_cap", ref cap);
            Capacity = cap;

            var head = HeadIndex;
            dataStore.SyncData($"{prefix}_head", ref head);
            HeadIndex = head;

            EnsureInitialized();

            for (var i = 0; i < Capacity; i++)
            {
                var item = Items[i] ?? new DecisionOutcomeRecord();
                item.SyncData(dataStore, $"{prefix}_{i}");
                Items[i] = item;
            }

            EnsureInitialized();
        }

        private static int ClampIndex(int idx, int capacity)
        {
            if (capacity <= 0)
            {
                return 0;
            }

            if (idx < 0)
            {
                return 0;
            }

            return idx >= capacity ? idx % capacity : idx;
        }
    }

    public sealed class DecisionOutcomeRecord
    {
        public int DayNumber { get; set; } = -1;
        public string EventId { get; set; } = string.Empty;
        public string OptionId { get; set; } = string.Empty;
        public string ResultText { get; set; } = string.Empty;

        public void Normalize()
        {
            EventId ??= string.Empty;
            OptionId ??= string.Empty;
            ResultText ??= string.Empty;
            DayNumber = Math.Max(-1, DayNumber);
        }

        public void SyncData(IDataStore dataStore, string prefix)
        {
            var day = DayNumber;
            var evt = EventId ?? string.Empty;
            var opt = OptionId ?? string.Empty;
            var txt = ResultText ?? string.Empty;

            dataStore.SyncData($"{prefix}_day", ref day);
            dataStore.SyncData($"{prefix}_evt", ref evt);
            dataStore.SyncData($"{prefix}_opt", ref opt);
            dataStore.SyncData($"{prefix}_txt", ref txt);

            DayNumber = day;
            EventId = evt ?? string.Empty;
            OptionId = opt ?? string.Empty;
            ResultText = txt ?? string.Empty;
        }
    }
}


