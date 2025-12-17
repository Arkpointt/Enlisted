using System;
using System.Linq;
using Enlisted.Features.Activities;
using Enlisted.Features.Camp.UI.Bulletin;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Features.Schedule.Behaviors;
using Enlisted.Mod.Core.Logging;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace Enlisted.Features.Camp.UI.Management
{
    /// <summary>
    /// Activities tab ViewModel - Location-based activity selection.
    /// Kingdom-style tab interface matching the Camp Management screen pattern.
    /// Phase 4: Full implementation with location navigation, activity selection, and execution.
    /// </summary>
    public class CampActivitiesVM : ViewModel
    {
        private const string LogCategory = "CampActivitiesVM";

        // Location selection (left panel)
        private MBBindingList<ActivityLocationItemVM> _locations;
        private ActivityLocationItemVM _selectedLocation;

        // Activity list (center panel)
        private MBBindingList<CampActivityItemVM> _activities;
        private CampActivityItemVM _selectedActivity;
        private bool _hasSelectedActivity;

        // Activity details (right panel)
        private string _activityTitle;
        private string _activityDescription;
        private MBBindingList<ActivityEffectItemVM> _activityEffects;
        private string _activityRequirements;
        private bool _canPerformActivity;
        private string _performButtonText;
        private string _performButtonHint;

        // State
        private bool _show;
        private string _currentLocationId;

        // Text strings
        private string _locationsHeaderText;
        private string _activitiesHeaderText;
        private string _noActivitiesText;
        private string _selectActivityText;

        public CampActivitiesVM()
        {
            Locations = new MBBindingList<ActivityLocationItemVM>();
            Activities = new MBBindingList<CampActivityItemVM>();
            ActivityEffects = new MBBindingList<ActivityEffectItemVM>();

            InitializeLocations();
            RefreshValues();
        }

        public override void RefreshValues()
        {
            base.RefreshValues();

            LocationsHeaderText = "Locations";
            ActivitiesHeaderText = "Available Activities";
            NoActivitiesText = "No activities available at this location.";
            SelectActivityText = "Select an activity to view details.";
            PerformButtonText = "Perform Activity";

            // Refresh location activity counts
            RefreshLocationCounts();

            // Select first location by default if none selected
            if (SelectedLocation == null && Locations.Count > 0)
            {
                SelectLocation(Locations[0]);
            }
        }

        /// <summary>
        /// Initialize the camp location list.
        /// </summary>
        private void InitializeLocations()
        {
            Locations.Clear();

            // Add locations in order of priority/thematic grouping
            Locations.Add(new ActivityLocationItemVM("training_grounds", "Training Grounds", "Practice combat skills and formations", OnLocationSelect));
            Locations.Add(new ActivityLocationItemVM("medical_tent", "Medical Tent", "Heal, rest, and help the wounded", OnLocationSelect));
            Locations.Add(new ActivityLocationItemVM("quartermaster", "Quartermaster", "Equipment, supplies, and work details", OnLocationSelect));
            Locations.Add(new ActivityLocationItemVM("camp_fire", "Camp Fire", "Socialize and rest with comrades", OnLocationSelect));
            Locations.Add(new ActivityLocationItemVM("personal_quarters", "Personal Quarters", "Private time and personal tasks", OnLocationSelect));
            Locations.Add(new ActivityLocationItemVM("lords_tent", "Lord's Tent", "Meet with officers and leadership", OnLocationSelect));
        }

        /// <summary>
        /// Refresh activity counts for each location.
        /// </summary>
        private void RefreshLocationCounts()
        {
            var behavior = CampActivitiesBehavior.Instance;
            if (behavior == null || !behavior.IsEnabled())
            {
                foreach (var loc in Locations)
                {
                    loc.ActivityCount = 0;
                }
                return;
            }

            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment == null || !enlistment.IsEnlisted)
            {
                foreach (var loc in Locations)
                {
                    loc.ActivityCount = 0;
                }
                return;
            }

            int playerTier = enlistment.EnlistmentTier;
            var scheduleBehavior = ScheduleBehavior.Instance;
            bool isLanceLeader = scheduleBehavior?.CanUseManualManagement() ?? false;

            var allActivities = behavior.GetAllActivities();

            foreach (var loc in Locations)
            {
                var count = allActivities
                    .Where(a => string.Equals(a.Location, loc.LocationId, StringComparison.OrdinalIgnoreCase))
                    .Where(a => playerTier >= a.MinTier)
                    .Where(a => a.MaxTier == 0 || playerTier <= a.MaxTier)
                    .Where(a => !a.RequiresLanceLeader || isLanceLeader)
                    .Count();

                loc.ActivityCount = count;
            }
        }

        /// <summary>
        /// Handle location selection.
        /// </summary>
        private void OnLocationSelect(ActivityLocationItemVM location)
        {
            SelectLocation(location);
        }

        /// <summary>
        /// Select a location and load its activities.
        /// </summary>
        private void SelectLocation(ActivityLocationItemVM location)
        {
            if (location == null)
                return;

            // Deselect previous
            if (SelectedLocation != null)
            {
                SelectedLocation.IsSelected = false;
            }

            // Select new
            SelectedLocation = location;
            SelectedLocation.IsSelected = true;
            _currentLocationId = location.LocationId;

            // Load activities for this location
            LoadActivitiesForLocation(location.LocationId);

            ModLogger.Debug(LogCategory, $"Selected location: {location.LocationName} ({location.ActivityCount} activities)");
        }

        /// <summary>
        /// Load activities for the specified location.
        /// </summary>
        private void LoadActivitiesForLocation(string locationId)
        {
            Activities.Clear();
            SelectedActivity = null;
            HasSelectedActivity = false;
            ClearActivityDetails();

            var behavior = CampActivitiesBehavior.Instance;
            if (behavior == null || !behavior.IsEnabled())
            {
                ModLogger.Debug(LogCategory, "Activities behavior not available");
                return;
            }

            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment == null || !enlistment.IsEnlisted)
            {
                ModLogger.Debug(LogCategory, "Player not enlisted");
                return;
            }

            int playerTier = enlistment.EnlistmentTier;
            var scheduleBehavior = ScheduleBehavior.Instance;
            bool isLanceLeader = scheduleBehavior?.CanUseManualManagement() ?? false;
            int currentDay = (int)CampaignTime.Now.ToDays;

            var allActivities = behavior.GetAllActivities();
            var locationActivities = allActivities
                .Where(a => string.Equals(a.Location, locationId, StringComparison.OrdinalIgnoreCase))
                .Where(a => playerTier >= a.MinTier)
                .Where(a => a.MaxTier == 0 || playerTier <= a.MaxTier)
                .Where(a => !a.RequiresLanceLeader || isLanceLeader)
                .OrderBy(a => a.Category)
                .ThenBy(a => a.MinTier)
                .ToList();

            foreach (var activity in locationActivities)
            {
                // Check if activity is on cooldown
                bool onCooldown = behavior.TryGetCooldownDaysRemaining(activity, currentDay, out int daysRemaining);

                // Get activity text
                var textObj = behavior.GetActivityText(activity);
                string displayText = textObj?.ToString() ?? activity.TextFallback ?? activity.Id;

                var item = new CampActivityItemVM(
                    activity,
                    displayText,
                    onCooldown,
                    daysRemaining,
                    OnActivitySelect
                );

                Activities.Add(item);
            }

            ModLogger.Debug(LogCategory, $"Loaded {Activities.Count} activities for {locationId}");

            // Auto-select first if available
            if (Activities.Count > 0)
            {
                SelectActivity(Activities[0]);
            }
        }

        /// <summary>
        /// Handle activity selection.
        /// </summary>
        private void OnActivitySelect(CampActivityItemVM activity)
        {
            SelectActivity(activity);
        }

        /// <summary>
        /// Select an activity and display its details.
        /// </summary>
        private void SelectActivity(CampActivityItemVM activity)
        {
            if (activity == null)
                return;

            // Deselect previous
            if (SelectedActivity != null)
            {
                SelectedActivity.IsSelected = false;
            }

            // Select new
            SelectedActivity = activity;
            SelectedActivity.IsSelected = true;
            HasSelectedActivity = true;

            // Populate details panel
            PopulateActivityDetails(activity);
        }

        /// <summary>
        /// Populate the activity details panel.
        /// </summary>
        private void PopulateActivityDetails(CampActivityItemVM activityItem)
        {
            var activity = activityItem.Activity;
            var behavior = CampActivitiesBehavior.Instance;

            // Title and description
            var titleObj = behavior?.GetActivityText(activity);
            ActivityTitle = titleObj?.ToString() ?? activity.TextFallback ?? activity.Id;

            var hintObj = behavior?.GetActivityHintText(activity);
            ActivityDescription = hintObj?.ToString() ?? activity.HintFallback ?? "No description available.";

            // Build effects list
            ActivityEffects.Clear();

            // XP rewards
            if (activity.SkillXp != null && activity.SkillXp.Count > 0)
            {
                foreach (var xp in activity.SkillXp)
                {
                    if (xp.Value > 0)
                    {
                        ActivityEffects.Add(new ActivityEffectItemVM($"+{xp.Value} {xp.Key} XP", true));
                    }
                }
            }

            // Fatigue cost
            if (activity.FatigueCost > 0)
            {
                ActivityEffects.Add(new ActivityEffectItemVM($"-{activity.FatigueCost} Fatigue", false));
            }

            // Fatigue relief
            if (activity.FatigueRelief > 0)
            {
                ActivityEffects.Add(new ActivityEffectItemVM($"+{activity.FatigueRelief} Fatigue Relief", true));
            }

            // Cooldown
            if (activity.CooldownDays > 0)
            {
                ActivityEffects.Add(new ActivityEffectItemVM($"{activity.CooldownDays} day cooldown", false));
            }

            // Build requirements text
            var requirements = new System.Text.StringBuilder();

            if (activity.MinTier > 1)
            {
                requirements.Append($"Tier {activity.MinTier}+ ");
            }

            if (activity.MaxTier > 0)
            {
                requirements.Append($"(Max T{activity.MaxTier}) ");
            }

            if (activity.RequiresLanceLeader)
            {
                requirements.Append("Lance Leader Only ");
            }

            if (activity.Formations != null && activity.Formations.Count > 0)
            {
                requirements.Append($"Formations: {string.Join(", ", activity.Formations)} ");
            }

            if (activity.DayParts != null && activity.DayParts.Count > 0)
            {
                requirements.Append($"Time: {string.Join(", ", activity.DayParts)}");
            }

            ActivityRequirements = requirements.Length > 0 ? requirements.ToString().Trim() : "No special requirements";

            // Check if activity can be performed
            var enlistment = EnlistmentBehavior.Instance;
            int currentDay = (int)CampaignTime.Now.ToDays;

            if (activityItem.IsOnCooldown)
            {
                CanPerformActivity = false;
                PerformButtonHint = $"On cooldown for {activityItem.CooldownDaysRemaining} more day(s).";
            }
            else if (enlistment != null && activity.FatigueCost > enlistment.FatigueCurrent)
            {
                CanPerformActivity = false;
                PerformButtonHint = "Not enough fatigue to perform this activity.";
            }
            else
            {
                CanPerformActivity = true;
                PerformButtonHint = "Click to perform this activity.";
            }
        }

        /// <summary>
        /// Clear the activity details panel.
        /// </summary>
        private void ClearActivityDetails()
        {
            ActivityTitle = "";
            ActivityDescription = SelectActivityText;
            ActivityEffects.Clear();
            ActivityRequirements = "";
            CanPerformActivity = false;
            PerformButtonHint = "";
        }

        /// <summary>
        /// Execute: Perform the selected activity.
        /// </summary>
        public void ExecutePerformActivity()
        {
            if (SelectedActivity == null || !CanPerformActivity)
                return;

            var behavior = CampActivitiesBehavior.Instance;
            if (behavior == null)
                return;

            bool success = behavior.TryExecuteActivity(SelectedActivity.Activity, out string failureReason);

            if (success)
            {
                ModLogger.Info(LogCategory, $"Activity completed: {SelectedActivity.Activity.Id}");

                // Show success message
                InformationManager.DisplayMessage(new InformationMessage(
                    $"Completed: {ActivityTitle}",
                    Color.FromUint(0xFF88FF88)
                ));

                // Refresh the activity list to update cooldowns
                LoadActivitiesForLocation(_currentLocationId);
                RefreshLocationCounts();

                // Post to bulletin if available
                CampBulletinIntegration.PostNews(new CampBulletinNewsItemVM(
                    $"activity_{SelectedActivity.Activity.Id}_{DateTime.Now.Ticks}",
                    $"Activity Completed: {ActivityTitle}",
                    $"You completed {ActivityTitle} at the {SelectedLocation?.LocationName ?? "camp"}.",
                    CampaignTime.Now,
                    "Activity",
                    1
                ));
            }
            else
            {
                ModLogger.Warn(LogCategory, $"Activity failed: {SelectedActivity.Activity.Id} - {failureReason}");

                // Show failure message
                var failureText = GetFailureReasonText(failureReason);
                InformationManager.DisplayMessage(new InformationMessage(
                    failureText,
                    Color.FromUint(0xFFFF8888)
                ));
            }
        }

        /// <summary>
        /// Get human-readable failure reason text.
        /// </summary>
        private string GetFailureReasonText(string reasonId)
        {
            return reasonId switch
            {
                "act_fail_cooldown" => "Activity is on cooldown.",
                "act_fail_too_fatigued" => "You are too fatigued for this activity.",
                "act_fail_condition" => "Your current condition prevents this activity.",
                "act_fail_not_enlisted" => "You must be enlisted to perform activities.",
                "act_fail_disabled" => "Activities are currently disabled.",
                _ => "Unable to perform this activity."
            };
        }

        // ===== Properties =====

        [DataSourceProperty]
        public bool Show
        {
            get => _show;
            set
            {
                if (value == _show) return;
                _show = value;
                OnPropertyChangedWithValue(value, nameof(Show));

                if (value)
                {
                    RefreshValues();
                }
            }
        }

        [DataSourceProperty]
        public string LocationsHeaderText
        {
            get => _locationsHeaderText;
            set
            {
                if (value == _locationsHeaderText) return;
                _locationsHeaderText = value;
                OnPropertyChangedWithValue(value, nameof(LocationsHeaderText));
            }
        }

        [DataSourceProperty]
        public string ActivitiesHeaderText
        {
            get => _activitiesHeaderText;
            set
            {
                if (value == _activitiesHeaderText) return;
                _activitiesHeaderText = value;
                OnPropertyChangedWithValue(value, nameof(ActivitiesHeaderText));
            }
        }

        [DataSourceProperty]
        public string NoActivitiesText
        {
            get => _noActivitiesText;
            set
            {
                if (value == _noActivitiesText) return;
                _noActivitiesText = value;
                OnPropertyChangedWithValue(value, nameof(NoActivitiesText));
            }
        }

        [DataSourceProperty]
        public string SelectActivityText
        {
            get => _selectActivityText;
            set
            {
                if (value == _selectActivityText) return;
                _selectActivityText = value;
                OnPropertyChangedWithValue(value, nameof(SelectActivityText));
            }
        }

        [DataSourceProperty]
        public MBBindingList<ActivityLocationItemVM> Locations
        {
            get => _locations;
            set
            {
                if (value == _locations) return;
                _locations = value;
                OnPropertyChangedWithValue(value, nameof(Locations));
            }
        }

        [DataSourceProperty]
        public ActivityLocationItemVM SelectedLocation
        {
            get => _selectedLocation;
            set
            {
                if (value == _selectedLocation) return;
                _selectedLocation = value;
                OnPropertyChangedWithValue(value, nameof(SelectedLocation));
            }
        }

        [DataSourceProperty]
        public MBBindingList<CampActivityItemVM> Activities
        {
            get => _activities;
            set
            {
                if (value == _activities) return;
                _activities = value;
                OnPropertyChangedWithValue(value, nameof(Activities));
            }
        }

        [DataSourceProperty]
        public CampActivityItemVM SelectedActivity
        {
            get => _selectedActivity;
            set
            {
                if (value == _selectedActivity) return;
                _selectedActivity = value;
                OnPropertyChangedWithValue(value, nameof(SelectedActivity));
            }
        }

        [DataSourceProperty]
        public bool HasSelectedActivity
        {
            get => _hasSelectedActivity;
            set
            {
                if (value == _hasSelectedActivity) return;
                _hasSelectedActivity = value;
                OnPropertyChangedWithValue(value, nameof(HasSelectedActivity));
            }
        }

        [DataSourceProperty]
        public string ActivityTitle
        {
            get => _activityTitle;
            set
            {
                if (value == _activityTitle) return;
                _activityTitle = value;
                OnPropertyChangedWithValue(value, nameof(ActivityTitle));
            }
        }

        [DataSourceProperty]
        public string ActivityDescription
        {
            get => _activityDescription;
            set
            {
                if (value == _activityDescription) return;
                _activityDescription = value;
                OnPropertyChangedWithValue(value, nameof(ActivityDescription));
            }
        }

        [DataSourceProperty]
        public MBBindingList<ActivityEffectItemVM> ActivityEffects
        {
            get => _activityEffects;
            set
            {
                if (value == _activityEffects) return;
                _activityEffects = value;
                OnPropertyChangedWithValue(value, nameof(ActivityEffects));
            }
        }

        [DataSourceProperty]
        public string ActivityRequirements
        {
            get => _activityRequirements;
            set
            {
                if (value == _activityRequirements) return;
                _activityRequirements = value;
                OnPropertyChangedWithValue(value, nameof(ActivityRequirements));
            }
        }

        [DataSourceProperty]
        public bool CanPerformActivity
        {
            get => _canPerformActivity;
            set
            {
                if (value == _canPerformActivity) return;
                _canPerformActivity = value;
                OnPropertyChangedWithValue(value, nameof(CanPerformActivity));
            }
        }

        [DataSourceProperty]
        public string PerformButtonText
        {
            get => _performButtonText;
            set
            {
                if (value == _performButtonText) return;
                _performButtonText = value;
                OnPropertyChangedWithValue(value, nameof(PerformButtonText));
            }
        }

        [DataSourceProperty]
        public string PerformButtonHint
        {
            get => _performButtonHint;
            set
            {
                if (value == _performButtonHint) return;
                _performButtonHint = value;
                OnPropertyChangedWithValue(value, nameof(PerformButtonHint));
            }
        }
    }

    /// <summary>
    /// ViewModel for a camp location in the activities tab.
    /// </summary>
    public class ActivityLocationItemVM : ViewModel
    {
        private readonly Action<ActivityLocationItemVM> _onSelect;
        private bool _isSelected;
        private int _activityCount;
        private string _activityCountText;

        public string LocationId { get; }
        public string LocationName { get; }
        public string LocationDescription { get; }

        public ActivityLocationItemVM(string locationId, string name, string description, Action<ActivityLocationItemVM> onSelect)
        {
            LocationId = locationId;
            LocationName = name;
            LocationDescription = description;
            _onSelect = onSelect;
            _activityCount = 0;
            _activityCountText = "(0)";
        }

        public void ExecuteSelect()
        {
            _onSelect?.Invoke(this);
        }

        [DataSourceProperty]
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (value == _isSelected) return;
                _isSelected = value;
                OnPropertyChangedWithValue(value, nameof(IsSelected));
            }
        }

        [DataSourceProperty]
        public int ActivityCount
        {
            get => _activityCount;
            set
            {
                if (value == _activityCount) return;
                _activityCount = value;
                OnPropertyChangedWithValue(value, nameof(ActivityCount));
                ActivityCountText = $"({value})";
            }
        }

        [DataSourceProperty]
        public string ActivityCountText
        {
            get => _activityCountText;
            set
            {
                if (value == _activityCountText) return;
                _activityCountText = value;
                OnPropertyChangedWithValue(value, nameof(ActivityCountText));
            }
        }
    }

    /// <summary>
    /// ViewModel for an activity item in the activities list.
    /// </summary>
    public class CampActivityItemVM : ViewModel
    {
        private readonly Action<CampActivityItemVM> _onSelect;
        private bool _isSelected;

        public CampActivityDefinition Activity { get; }
        public string DisplayText { get; }
        public bool IsOnCooldown { get; }
        public int CooldownDaysRemaining { get; }
        public string CooldownText { get; }

        public CampActivityItemVM(CampActivityDefinition activity, string displayText, bool onCooldown, int cooldownDays, Action<CampActivityItemVM> onSelect)
        {
            Activity = activity;
            DisplayText = displayText;
            IsOnCooldown = onCooldown;
            CooldownDaysRemaining = cooldownDays;
            CooldownText = onCooldown ? $"({cooldownDays}d)" : "";
            _onSelect = onSelect;
        }

        public void ExecuteSelect()
        {
            _onSelect?.Invoke(this);
        }

        [DataSourceProperty]
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (value == _isSelected) return;
                _isSelected = value;
                OnPropertyChangedWithValue(value, nameof(IsSelected));
            }
        }
    }

    /// <summary>
    /// ViewModel for an effect line in the activity details panel.
    /// </summary>
    public class ActivityEffectItemVM : ViewModel
    {
        public string EffectText { get; }
        public bool IsPositive { get; }

        public ActivityEffectItemVM(string effectText, bool isPositive)
        {
            EffectText = effectText;
            IsPositive = isPositive;
        }
    }
}
