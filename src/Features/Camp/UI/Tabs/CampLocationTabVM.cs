using System;
using System.Collections.Generic;
using System.Linq;
using Enlisted.Features.Activities;
using Enlisted.Features.Assignments.Behaviors;
using Enlisted.Features.Conditions;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Mod.Core.Logging;
using Enlisted.Mod.Core.Triggers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace Enlisted.Features.Camp.UI.Tabs
{
    /// <summary>
    /// Base ViewModel for a location tab in the Camp screen.
    /// Each location (Medical Tent, Training Grounds, etc.) has its own tab
    /// that displays activities filtered to that location.
    /// </summary>
    public class CampLocationTabVM : ViewModel
    {
        private readonly string _locationId;
        private readonly Action _onRefresh;
        private readonly EnlistmentBehavior _enlistment;
        private readonly CampActivitiesBehavior _activitiesBehavior;
        
        private bool _isSelected;
        private MBBindingList<ActivityCardVM> _activities;
        private string _tabName;
        private int _activityCount;
        private int _availableCount;

        public string LocationId => _locationId;

        [DataSourceProperty]
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChangedWithValue(value, nameof(IsSelected));
                    if (value)
                    {
                        RefreshActivities();
                    }
                }
            }
        }

        [DataSourceProperty]
        public MBBindingList<ActivityCardVM> Activities
        {
            get => _activities;
            set
            {
                if (_activities != value)
                {
                    _activities = value;
                    OnPropertyChangedWithValue(value, nameof(Activities));
                }
            }
        }

        [DataSourceProperty]
        public string TabName
        {
            get => _tabName;
            set
            {
                if (_tabName != value)
                {
                    _tabName = value;
                    OnPropertyChangedWithValue(value, nameof(TabName));
                }
            }
        }

        [DataSourceProperty]
        public int ActivityCount
        {
            get => _activityCount;
            set
            {
                if (_activityCount != value)
                {
                    _activityCount = value;
                    OnPropertyChangedWithValue(value, nameof(ActivityCount));
                }
            }
        }

        [DataSourceProperty]
        public int AvailableCount
        {
            get => _availableCount;
            set
            {
                if (_availableCount != value)
                {
                    _availableCount = value;
                    OnPropertyChangedWithValue(value, nameof(AvailableCount));
                }
            }
        }

        [DataSourceProperty]
        public string CountText => $"({AvailableCount}/{ActivityCount})";

        [DataSourceProperty]
        public bool HasActivities => ActivityCount > 0;

        public CampLocationTabVM(string locationId, string tabName, Action onRefresh)
        {
            _locationId = locationId;
            _tabName = tabName;
            _onRefresh = onRefresh;
            _enlistment = EnlistmentBehavior.Instance;
            _activitiesBehavior = CampActivitiesBehavior.Instance;
            _activities = new MBBindingList<ActivityCardVM>();
        }

        public void RefreshActivities()
        {
            try
            {
                Activities.Clear();

                if (_activitiesBehavior == null || !_activitiesBehavior.IsEnabled())
                {
                    ActivityCount = 0;
                    AvailableCount = 0;
                    return;
                }

                var allActivities = _activitiesBehavior.GetAllActivities();
                if (allActivities == null || !allActivities.Any())
                {
                    ActivityCount = 0;
                    AvailableCount = 0;
                    return;
                }

                // Filter by location
                var activitiesAtLocation = allActivities
                    .Where(a => string.Equals(a.Location, _locationId, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                var currentDay = (int)CampaignTime.Now.ToDays;
                var dayPart = CampaignTriggerTrackerBehavior.Instance?.GetDayPart();
                var dayPartToken = dayPart?.ToString().ToLowerInvariant() ?? "day";
                var formation = EnlistedDutiesBehavior.Instance?.GetPlayerFormationType()?.ToLowerInvariant() ?? "infantry";

                var availableActivities = new List<ActivityCardVM>();
                var unavailableActivities = new List<ActivityCardVM>();

                foreach (var activity in activitiesAtLocation)
                {
                    // Check visibility (formation, time of day, rank)
                    if (!CampActivitiesBehavior.IsActivityVisibleFor(activity, _enlistment, formation, dayPartToken))
                    {
                        continue;
                    }

                    // Check availability (fatigue, cooldown, conditions)
                    var isEnabled = IsActivityEnabled(activity, currentDay, out var unavailableReason);

                    var card = new ActivityCardVM(activity, isEnabled, unavailableReason, _enlistment);

                    if (isEnabled)
                    {
                        availableActivities.Add(card);
                    }
                    else
                    {
                        unavailableActivities.Add(card);
                    }
                }

                // Add available activities first (sorted by category)
                foreach (var card in availableActivities.OrderBy(a => a.CategoryBadgeText))
                {
                    Activities.Add(card);
                }

                // Then unavailable activities
                foreach (var card in unavailableActivities.OrderBy(a => a.CategoryBadgeText))
                {
                    Activities.Add(card);
                }

                AvailableCount = availableActivities.Count;
                ActivityCount = Activities.Count;

                OnPropertyChangedWithValue(CountText, nameof(CountText));
                OnPropertyChangedWithValue(HasActivities, nameof(HasActivities));
            }
            catch (Exception ex)
            {
                ModLogger.Error("CampLocationTab", $"Failed to refresh activities: {ex.Message}", ex);
            }
        }

        private bool IsActivityEnabled(CampActivityDefinition activity, int currentDay, out string unavailableReason)
        {
            unavailableReason = null;

            // Check severe conditions
            if (activity.BlockOnSevereCondition)
            {
                var conditionState = PlayerConditionBehavior.Instance?.State;
                if (conditionState != null && (conditionState.InjuryDaysRemaining > 7 || conditionState.IllnessDaysRemaining > 7))
                {
                    unavailableReason = "Too injured (severe condition)";
                    return false;
                }
            }

            // Check fatigue
            if (activity.FatigueCost > 0)
            {
                var currentFatigue = _enlistment?.FatigueCurrent ?? 0;
                var maxFatigue = _enlistment?.FatigueMax ?? 24;

                if (currentFatigue + activity.FatigueCost > maxFatigue)
                {
                    unavailableReason = $"Too tired (need {activity.FatigueCost} fatigue)";
                    return false;
                }
            }

            // Check cooldown
            if (_activitiesBehavior.TryGetCooldownDaysRemaining(activity, currentDay, out var daysRemaining))
            {
                unavailableReason = daysRemaining == 1
                    ? "Cooldown: 1 day"
                    : $"Cooldown: {daysRemaining} days";
                return false;
            }

            return true;
        }

        public override void OnFinalize()
        {
            base.OnFinalize();
            Activities.Clear();
        }
    }
}
