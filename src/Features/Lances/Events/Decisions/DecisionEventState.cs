using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.SaveSystem;

namespace Enlisted.Features.Lances.Events.Decisions
{
    /// <summary>
    /// Persisted state for the Decision Events system.
    /// Tracks cooldowns, one-time flags, per-term counts, and story flags
    /// to implement CK3-style pacing protections.
    /// </summary>
    public sealed class DecisionEventState
    {
        // Per-event tracking: when each event was last fired (stored as day number for simplicity)
        private Dictionary<string, int> _eventLastFiredDay = new Dictionary<string, int>();

        // One-time events that have already fired (never fire again)
        private HashSet<string> _oneTimeFired = new HashSet<string>();

        // Per-term counts: how many times each event fired this enlistment term
        private Dictionary<string, int> _firedThisTerm = new Dictionary<string, int>();

        // Per-category tracking: when each category was last fired (day number)
        private Dictionary<string, int> _categoryLastFiredDay = new Dictionary<string, int>();

        // Global tracking
        private int _firedToday;
        private int _firedThisWeek;
        private int _lastEventFiredHour = -1;
        private int _lastFiredDayNumber = -1;
        private int _lastFiredWeekNumber = -1;

        // Story flags for narrative state blocking
        private HashSet<string> _activeFlags = new HashSet<string>();

        // Flag expiry tracking (day number when flag expires)
        private Dictionary<string, int> _flagExpiryDay = new Dictionary<string, int>();

        // Chain event queue (event IDs waiting to fire as follow-ups)
        private List<string> _chainQueue = new List<string>();

        // Chain delay tracking (hour number when each queued chain event becomes eligible)
        private Dictionary<string, int> _chainDelayHour = new Dictionary<string, int>();

        // Property accessors for cleaner external use

        public int FiredToday => _firedToday;
        public int FiredThisWeek => _firedThisWeek;

        /// <summary>
        /// Gets hours since last event fired.
        /// </summary>
        public int HoursSinceLastEvent()
        {
            if (_lastEventFiredHour < 0)
            {
                return int.MaxValue; // Never fired
            }

            var currentHour = GetHourNumber();
            return currentHour - _lastEventFiredHour;
        }

        /// <summary>
        /// Checks if an event has already been fired as a one-time event.
        /// </summary>
        public bool HasFiredOneTime(string eventId)
        {
            return _oneTimeFired.Contains(eventId);
        }

        /// <summary>
        /// Gets how many times an event has fired this term.
        /// </summary>
        public int GetFiredThisTerm(string eventId)
        {
            return _firedThisTerm.TryGetValue(eventId, out var count) ? count : 0;
        }

        /// <summary>
        /// Gets days since an event was last fired, or int.MaxValue if never fired.
        /// </summary>
        public int GetDaysSinceEventFired(string eventId)
        {
            if (_eventLastFiredDay.TryGetValue(eventId, out var lastDay))
            {
                return GetDayNumber() - lastDay;
            }
            return int.MaxValue;
        }

        /// <summary>
        /// Gets days since a category was last fired, or int.MaxValue if never fired.
        /// </summary>
        public int GetDaysSinceCategoryFired(string category)
        {
            if (_categoryLastFiredDay.TryGetValue(category, out var lastDay))
            {
                return GetDayNumber() - lastDay;
            }
            return int.MaxValue;
        }

        /// <summary>
        /// Checks if a story flag is currently active.
        /// </summary>
        public bool HasActiveFlag(string flag)
        {
            return _activeFlags.Contains(flag);
        }

        /// <summary>
        /// Gets the next chain event to fire, if any is ready.
        /// </summary>
        public string GetNextChainEvent()
        {
            if (_chainQueue.Count == 0)
            {
                return null;
            }

            var nextId = _chainQueue[0];
            
            // Check if delay has passed
            if (_chainDelayHour.TryGetValue(nextId, out var readyHour))
            {
                if (GetHourNumber() < readyHour)
                {
                    return null; // Not ready yet
                }
            }

            return nextId;
        }

        /// <summary>
        /// Records that an event was fired. Updates all tracking state.
        /// </summary>
        public void RecordEventFired(string eventId, string category, bool isOneTime, bool hasMaxPerTerm)
        {
            var currentDay = GetDayNumber();
            var currentWeek = GetWeekNumber();
            var currentHour = GetHourNumber();

            // Update per-event tracking
            _eventLastFiredDay[eventId] = currentDay;
            _lastEventFiredHour = currentHour;

            // Update per-category tracking
            if (!string.IsNullOrEmpty(category))
            {
                _categoryLastFiredDay[category] = currentDay;
            }

            // Update one-time tracking
            if (isOneTime)
            {
                _oneTimeFired.Add(eventId);
            }

            // Update per-term tracking
            if (hasMaxPerTerm)
            {
                _firedThisTerm.TryGetValue(eventId, out var count);
                _firedThisTerm[eventId] = count + 1;
            }

            // Update daily counter (reset if new day)
            if (_lastFiredDayNumber != currentDay)
            {
                _firedToday = 0;
                _lastFiredDayNumber = currentDay;
            }
            _firedToday++;

            // Update weekly counter (reset if new week)
            if (_lastFiredWeekNumber != currentWeek)
            {
                _firedThisWeek = 0;
                _lastFiredWeekNumber = currentWeek;
            }
            _firedThisWeek++;

            // Remove from chain queue if this was a chain event
            if (_chainQueue.Contains(eventId))
            {
                _chainQueue.Remove(eventId);
                _chainDelayHour.Remove(eventId);
            }
        }

        /// <summary>
        /// Adds a chain event to the queue with an optional delay.
        /// </summary>
        public void QueueChainEvent(string eventId, float delayHours = 0f)
        {
            if (_chainQueue.Contains(eventId))
            {
                return; // Already queued
            }

            _chainQueue.Add(eventId);

            if (delayHours > 0f)
            {
                var readyHour = GetHourNumber() + (int)Math.Ceiling(delayHours);
                _chainDelayHour[eventId] = readyHour;
            }
        }

        /// <summary>
        /// Sets a story flag with optional expiry.
        /// </summary>
        public void SetFlag(string flag, float expiryDays = 0f)
        {
            _activeFlags.Add(flag);

            if (expiryDays > 0f)
            {
                var expiryDay = GetDayNumber() + (int)Math.Ceiling(expiryDays);
                _flagExpiryDay[flag] = expiryDay;
            }
            else
            {
                // No expiry - remove any existing expiry
                _flagExpiryDay.Remove(flag);
            }
        }

        /// <summary>
        /// Removes a story flag.
        /// </summary>
        public void ClearFlag(string flag)
        {
            _activeFlags.Remove(flag);
            _flagExpiryDay.Remove(flag);
        }

        /// <summary>
        /// Expires old flags based on current time. Called daily.
        /// </summary>
        public void ExpireFlags()
        {
            var currentDay = GetDayNumber();
            var toRemove = new List<string>();

            foreach (var kvp in _flagExpiryDay)
            {
                if (kvp.Value <= currentDay)
                {
                    toRemove.Add(kvp.Key);
                }
            }

            foreach (var flag in toRemove)
            {
                ClearFlag(flag);
            }
        }

        /// <summary>
        /// Resets per-term counters. Called when player re-enlists.
        /// </summary>
        public void ResetTermCounters()
        {
            _firedThisTerm.Clear();
        }

        /// <summary>
        /// Resets daily counter. Called on day tick.
        /// </summary>
        public void ResetDailyCounter()
        {
            var currentDay = GetDayNumber();
            if (_lastFiredDayNumber != currentDay)
            {
                _firedToday = 0;
                _lastFiredDayNumber = currentDay;
            }
        }

        /// <summary>
        /// Resets weekly counter. Called when week changes.
        /// </summary>
        public void ResetWeeklyCounter()
        {
            var currentWeek = GetWeekNumber();
            if (_lastFiredWeekNumber != currentWeek)
            {
                _firedThisWeek = 0;
                _lastFiredWeekNumber = currentWeek;
            }
        }

        private static int GetDayNumber()
        {
            return (int)CampaignTime.Now.ToDays;
        }

        private static int GetWeekNumber()
        {
            return (int)(CampaignTime.Now.ToDays / 7);
        }

        private static int GetHourNumber()
        {
            return (int)Math.Floor(CampaignTime.Now.ToHours);
        }

        // Synchronization for save/load
        public void SyncData(IDataStore dataStore)
        {
            SyncDictionaryInt(dataStore, "de_eventLastFired", ref _eventLastFiredDay);
            SyncHashSet(dataStore, "de_oneTimeFired", ref _oneTimeFired);
            SyncDictionaryInt(dataStore, "de_firedThisTerm", ref _firedThisTerm);
            SyncDictionaryInt(dataStore, "de_categoryLastFired", ref _categoryLastFiredDay);

            dataStore.SyncData("de_firedToday", ref _firedToday);
            dataStore.SyncData("de_firedThisWeek", ref _firedThisWeek);
            dataStore.SyncData("de_lastEventFiredHour", ref _lastEventFiredHour);
            dataStore.SyncData("de_lastFiredDayNumber", ref _lastFiredDayNumber);
            dataStore.SyncData("de_lastFiredWeekNumber", ref _lastFiredWeekNumber);

            SyncHashSet(dataStore, "de_activeFlags", ref _activeFlags);
            SyncDictionaryInt(dataStore, "de_flagExpiry", ref _flagExpiryDay);

            SyncList(dataStore, "de_chainQueue", ref _chainQueue);
            SyncDictionaryInt(dataStore, "de_chainDelay", ref _chainDelayHour);

            // Ensure collections are not null after loading
            _eventLastFiredDay ??= new Dictionary<string, int>();
            _oneTimeFired ??= new HashSet<string>();
            _firedThisTerm ??= new Dictionary<string, int>();
            _categoryLastFiredDay ??= new Dictionary<string, int>();
            _activeFlags ??= new HashSet<string>();
            _flagExpiryDay ??= new Dictionary<string, int>();
            _chainQueue ??= new List<string>();
            _chainDelayHour ??= new Dictionary<string, int>();
        }

        private static void SyncDictionaryInt(IDataStore dataStore, string prefix, ref Dictionary<string, int> dict)
        {
            dict ??= new Dictionary<string, int>();
            var keys = new List<string>(dict.Keys);
            var count = keys.Count;
            dataStore.SyncData($"{prefix}_count", ref count);

            if (dataStore.IsLoading)
            {
                dict.Clear();
                for (var i = 0; i < count; i++)
                {
                    var key = string.Empty;
                    var value = 0;
                    dataStore.SyncData($"{prefix}_{i}_key", ref key);
                    dataStore.SyncData($"{prefix}_{i}_val", ref value);
                    if (!string.IsNullOrEmpty(key))
                    {
                        dict[key] = value;
                    }
                }
            }
            else
            {
                for (var i = 0; i < count; i++)
                {
                    var key = keys[i];
                    var value = dict[key];
                    dataStore.SyncData($"{prefix}_{i}_key", ref key);
                    dataStore.SyncData($"{prefix}_{i}_val", ref value);
                }
            }
        }

        private static void SyncHashSet(IDataStore dataStore, string prefix, ref HashSet<string> set)
        {
            set ??= new HashSet<string>();
            var list = new List<string>(set);
            var count = list.Count;
            dataStore.SyncData($"{prefix}_count", ref count);

            if (dataStore.IsLoading)
            {
                set.Clear();
                for (var i = 0; i < count; i++)
                {
                    var val = string.Empty;
                    dataStore.SyncData($"{prefix}_{i}", ref val);
                    if (!string.IsNullOrEmpty(val))
                    {
                        set.Add(val);
                    }
                }
            }
            else
            {
                for (var i = 0; i < count; i++)
                {
                    var val = list[i];
                    dataStore.SyncData($"{prefix}_{i}", ref val);
                }
            }
        }

        private static void SyncList(IDataStore dataStore, string prefix, ref List<string> list)
        {
            list ??= new List<string>();
            var count = list.Count;
            dataStore.SyncData($"{prefix}_count", ref count);

            if (dataStore.IsLoading)
            {
                list.Clear();
                for (var i = 0; i < count; i++)
                {
                    var val = string.Empty;
                    dataStore.SyncData($"{prefix}_{i}", ref val);
                    if (!string.IsNullOrEmpty(val))
                    {
                        list.Add(val);
                    }
                }
            }
            else
            {
                for (var i = 0; i < count; i++)
                {
                    var val = list[i];
                    dataStore.SyncData($"{prefix}_{i}", ref val);
                }
            }
        }
    }
}
