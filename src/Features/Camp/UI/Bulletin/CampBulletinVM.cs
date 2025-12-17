using System;
using System.Linq;
using Enlisted.Features.Activities;
using Enlisted.Features.Enlistment;
using Enlisted.Mod.Core.Triggers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ViewModelCollection.Input;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;

namespace Enlisted.Features.Camp.UI.Bulletin
{
    /// <summary>
    /// Main ViewModel for the Camp Bulletin Board screen.
    /// Based on native TownManagementVM pattern - three-column layout with dynamic center content.
    /// </summary>
    public class CampBulletinVM : ViewModel
    {
        // Sub-ViewModels (like TownManagementVM's ProjectSelection, GovernorSelection, etc.)
        private LanceLeaderVM _lanceLeader;
        private CampBulletinFeedVM _bulletinFeed;
        private CampPlayerStatusVM _playerStatus;
        private CampDailyScheduleVM _dailySchedule;
        private CampLanceLocationVM _lanceLocation;

        // Separate news feeds for better organization
        private MBBindingList<CampBulletinNewsItemVM> _lanceNews;
        private MBBindingList<CampBulletinNewsItemVM> _kingdomDispatches;

        // Location buttons (like TownManagementVM's Shops list)
        private MBBindingList<CampLocationButtonVM> _locations;

        // Current activities (dynamic based on selected location)
        private MBBindingList<ActivityIconVM> _currentLocationActivities;

        // Selected activity details (like TownManagementVM's CurrentProject)
        private ActivityDetailVM _selectedActivity;
        private bool _hasSelectedActivity;
        private bool _hasNoSelectedActivity;

        // State
        private bool _show;
        private bool _showingBulletin;  // true = bulletin feed, false = activities
        private bool _showingLance;     // true = lance view (roster, schedule, needs)
        private string _currentLocationId;
        private bool _showingActivities;

        // Text labels
        private string _titleText;
        private string _doneText;
        private string _currentTimeText;
        private string _locationsText;

        // Input
        private InputKeyItemVM _doneInputKey;

        public CampBulletinVM()
        {
            // Initialize sub-ViewModels
            LanceLeader = new LanceLeaderVM();
            BulletinFeed = new CampBulletinFeedVM();
            PlayerStatus = new CampPlayerStatusVM();
            DailySchedule = new CampDailyScheduleVM();
            LanceLocation = new CampLanceLocationVM();

            // Initialize separate news feeds
            LanceNews = new MBBindingList<CampBulletinNewsItemVM>();
            KingdomDispatches = new MBBindingList<CampBulletinNewsItemVM>();

            // Initialize location buttons (like settlement's shop icons)
            Locations = new MBBindingList<CampLocationButtonVM>();
            // Lance location - roster, schedule, and needs (first position - most important)
            Locations.Add(new CampLocationButtonVM("lance", "My Lance", "LANCE", ExecuteShowLocation));
            // Reports location - bulletin feed and daily schedule
            Locations.Add(new CampLocationButtonVM("reports", "Reports", "RPT", ExecuteShowLocation));
            // Other locations with activities
            Locations.Add(new CampLocationButtonVM("medical_tent", "Medical Tent", "MED", ExecuteShowLocation));
            Locations.Add(new CampLocationButtonVM("training_grounds", "Training Grounds", "TRN", ExecuteShowLocation));
            Locations.Add(new CampLocationButtonVM("quartermaster", "Quartermaster", "QM", ExecuteShowLocation));

            // Start with Reports visible (default view)
            ShowingBulletin = true;  // Show bulletin by default
            ShowingLance = false;
            CurrentLocationId = "reports";
            HasSelectedActivity = false;
            HasNoSelectedActivity = true;
            
            // Mark reports as initially selected
            var reportsLocation = Locations.FirstOrDefault(l => l.LocationId == "reports");
            if (reportsLocation != null)
            {
                reportsLocation.IsSelected = true;
            }

            // Initialize empty activity list
            CurrentLocationActivities = new MBBindingList<ActivityIconVM>();

            // Initialize Lance view data
            LanceLocation?.RefreshValues();

            RefreshValues();
        }

        public override void RefreshValues()
        {
            base.RefreshValues();
            // Player-facing title: keep it simple and consistent with native settlement screens.
            TitleText = ShowingBulletin ? "CAMP" : GetLocationTitle(CurrentLocationId);
            DoneText = "Done";
            CurrentTimeText = GetFormattedTime();
            LocationsText = "Camp Locations";

            UpdateLocationActivityCounts();

            // Refresh sub-ViewModels
            LanceLeader?.RefreshValues();
            BulletinFeed?.RefreshValues();
            PlayerStatus?.RefreshValues();
            DailySchedule?.RefreshValues();
            LanceLocation?.RefreshValues();
        }

        /// <summary>
        /// Command: Navigate to a specific camp location and show its content.
        /// Called when player clicks a location button.
        /// </summary>
        public void ExecuteShowLocation(string locationId)
        {
            CurrentLocationId = locationId;
            HasSelectedActivity = false;
            HasNoSelectedActivity = true;

            // Special locations with custom views
            if (string.Equals(locationId, "reports", StringComparison.OrdinalIgnoreCase))
            {
                ShowingBulletin = true;
                ShowingLance = false;
                // Refresh bulletin feed with current state
                BulletinFeed?.RefreshValues();
                CampBulletinIntegration.RefreshNewsForActiveBulletin();
            }
            else if (string.Equals(locationId, "lance", StringComparison.OrdinalIgnoreCase))
            {
                ShowingBulletin = false;
                ShowingLance = true;
                // Refresh lance data when showing lance view
                LanceLocation?.RefreshValues();
            }
            else
            {
                // Regular location with activities
                ShowingBulletin = false;
                ShowingLance = false;
                LoadActivitiesForLocation(locationId);
            }

            RefreshValues();

            // Update player status to show current location
            int activityCount = ShowingBulletin || ShowingLance ? 0 : CurrentLocationActivities.Count;
            PlayerStatus?.UpdateCurrentLocation(locationId, activityCount);

            // Update selection state
            foreach (var loc in Locations)
            {
                loc.IsSelected = string.Equals(loc.LocationId, locationId, StringComparison.OrdinalIgnoreCase);
            }
        }

        /// <summary>
        /// Command: Return to the bulletin board (news feed).
        /// </summary>
        public void ExecuteShowBulletin()
        {
            // #region agent log
            System.IO.File.AppendAllText(
                @"c:\Dev\Enlisted\Enlisted\.cursor\debug.log",
                $"{{\"location\":\"CampBulletinVM.cs:ExecuteShowBulletin\",\"message\":\"Return to bulletin\",\"timestamp\":{DateTime.UtcNow.Ticks},\"sessionId\":\"debug-session\",\"runId\":\"debug5\",\"hypothesisId\":\"Loc\"}}\n");
            // #endregion

            ShowingBulletin = true;
            CurrentLocationId = string.Empty;
            HasSelectedActivity = false;
            HasNoSelectedActivity = true;
            RefreshValues();

            foreach (var loc in Locations)
            {
                loc.IsSelected = false;
            }
        }

        private void UpdateLocationActivityCounts()
        {
            var behavior = CampActivitiesBehavior.Instance;
            if (behavior == null)
            {
                foreach (var loc in Locations)
                {
                    loc.UpdateActivityCount(0);
                }

                return;
            }

            var defs = behavior.GetAllActivities();
            foreach (var loc in Locations)
            {
                var count = defs.Count(d =>
                    string.Equals(d.Location, loc.LocationId, StringComparison.OrdinalIgnoreCase));
                loc.UpdateActivityCount(count);
            }
        }

        /// <summary>
        /// Command: Select an activity to view its details.
        /// Called when player clicks an activity icon.
        /// </summary>
        public void ExecuteSelectActivity(ActivityIconVM activityIcon)
        {
            if (activityIcon == null) return;

            // #region agent log
            System.IO.File.AppendAllText(
                @"c:\Dev\Enlisted\Enlisted\.cursor\debug.log",
                $"{{\"location\":\"CampBulletinVM.cs:ExecuteSelectActivity\",\"message\":\"Selected activity\",\"activity\":\"{activityIcon.ActivityName}\",\"timestamp\":{DateTime.UtcNow.Ticks},\"sessionId\":\"debug-session\",\"runId\":\"debug7\",\"hypothesisId\":\"Act\"}}\n");
            // #endregion

            SelectedActivity = new ActivityDetailVM(activityIcon.Activity);
            HasSelectedActivity = true;

            // Update selection state for circular icon visuals.
            foreach (var icon in CurrentLocationActivities)
            {
                icon.IsSelected = ReferenceEquals(icon, activityIcon);
            }
        }

        /// <summary>
        /// Command: Execute the currently selected activity.
        /// </summary>
        public void ExecutePerformActivity()
        {
            if (SelectedActivity == null) return;

            // Execute the activity through the behavior
            var behavior = CampActivitiesBehavior.Instance;
            if (behavior == null) return;

            behavior.TryExecuteActivity(SelectedActivity.Activity, out _);

            // Add result to bulletin feed
            var rewardSummary = SelectedActivity.GetRewardSummary();
            BulletinFeed.AddNewsItem(new CampBulletinNewsItemVM(
                "activity_complete",
                $"Completed: {SelectedActivity.ActivityTitle}",
                $"You've completed {SelectedActivity.ActivityTitle}. {rewardSummary}",
                CampaignTime.Now
            ));

            // Return to bulletin board to see the result
            ExecuteShowBulletin();
        }

        /// <summary>
        /// Command: Close the screen.
        /// </summary>
        public void ExecuteDone()
        {
            // #region agent log
            System.IO.File.AppendAllText(
                @"c:\Dev\Enlisted\Enlisted\.cursor\debug.log",
                $"{{\"location\":\"CampBulletinVM.cs:ExecuteDone\",\"message\":\"ExecuteDone called\",\"showBefore\":{Show.ToString().ToLower()},\"timestamp\":{DateTime.UtcNow.Ticks},\"sessionId\":\"debug-session\",\"runId\":\"debug3\",\"hypothesisId\":\"C\"}}\n");
            // #endregion

            Show = false;

            // #region agent log
            System.IO.File.AppendAllText(
                @"c:\Dev\Enlisted\Enlisted\.cursor\debug.log",
                $"{{\"location\":\"CampBulletinVM.cs:ExecuteDone\",\"message\":\"ExecuteDone finished\",\"showAfter\":{Show.ToString().ToLower()},\"timestamp\":{DateTime.UtcNow.Ticks},\"sessionId\":\"debug-session\",\"runId\":\"debug3\",\"hypothesisId\":\"C\"}}\n");
            // #endregion

            // TownManagement pattern is "Show=false then view closes itself".
            // In our overlay, campaign ticks may be suspended while the menu is open, so we also close directly.
            // #region agent log
            System.IO.File.AppendAllText(
                @"c:\Dev\Enlisted\Enlisted\.cursor\debug.log",
                $"{{\"location\":\"CampBulletinVM.cs:ExecuteDone\",\"message\":\"Calling CampBulletinScreen.Close()\",\"timestamp\":{DateTime.UtcNow.Ticks},\"sessionId\":\"debug-session\",\"runId\":\"debug6\",\"hypothesisId\":\"Close\"}}\n");
            // #endregion

            CampBulletinScreen.Close();
        }

        /// <summary>
        /// Load activities for the specified location from the behavior.
        /// Filters by player tier and Lance Leader status.
        /// </summary>
        private void LoadActivitiesForLocation(string locationId)
        {
            CurrentLocationActivities.Clear();

            var behavior = CampActivitiesBehavior.Instance;
            if (behavior == null)
            {
                return;
            }

            var enlistment = Enlistment.Behaviors.EnlistmentBehavior.Instance;
            if (enlistment == null || !enlistment.IsEnlisted)
            {
                return;
            }

            // Get player authority info
            int playerTier = enlistment.EnlistmentTier;
            var scheduleBehavior = Schedule.Behaviors.ScheduleBehavior.Instance;
            bool isLanceLeader = scheduleBehavior?.CanUseManualManagement() ?? false;

            // Get all activities for this location and filter by tier/role
            var allActivities = behavior.GetAllActivities();
            var locationActivities = allActivities
                .Where(a => string.Equals(a.Location, locationId, StringComparison.OrdinalIgnoreCase))
                .Where(a => playerTier >= a.MinTier) // Check minimum tier
                .Where(a => a.MaxTier == 0 || playerTier <= a.MaxTier) // Check maximum tier (0 = no limit)
                .Where(a => !a.RequiresLanceLeader || isLanceLeader) // Check Lance Leader requirement
                .ToList();

            foreach (var activity in locationActivities)
            {
                var icon = new ActivityIconVM(activity, ExecuteSelectActivity)
                {
                    IsSelected = false
                };
                CurrentLocationActivities.Add(icon);
            }

            HasSelectedActivity = false;
            SelectedActivity = null;
            HasNoSelectedActivity = true;
        }

        /// <summary>
        /// Get the display title for a location ID.
        /// </summary>
        private string GetLocationTitle(string locationId)
        {
            var location = Locations.FirstOrDefault(l => 
                string.Equals(l.LocationId, locationId, StringComparison.OrdinalIgnoreCase)
            );
            return location?.LocationName ?? "Camp";
        }

        /// <summary>
        /// Get formatted time string for display (e.g., "Evening, 18:00").
        /// </summary>
        private string GetFormattedTime()
        {
            var dayPart = CampaignTriggerTrackerBehavior.Instance?.GetDayPart() ?? DayPart.Morning;
            var hour = (int)CampaignTime.Now.CurrentHourInDay;
            return $"{dayPart}, {hour:00}:00";
        }

        /// <summary>
        /// Set the input key for the Done button.
        /// </summary>
        public void SetDoneInputKey(HotKey hotKey)
        {
            DoneInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, true);
        }

        public override void OnFinalize()
        {
            base.OnFinalize();
            DoneInputKey?.OnFinalize();
        }

        // ===== DATA SOURCE PROPERTIES =====

        [DataSourceProperty]
        public bool Show
        {
            get => _show;
            set
            {
                if (value == _show) return;
                _show = value;
                OnPropertyChangedWithValue(value, nameof(Show));
            }
        }

        [DataSourceProperty]
        public bool ShowingBulletin
        {
            get => _showingBulletin;
            set
            {
                if (value == _showingBulletin) return;
                _showingBulletin = value;
                OnPropertyChangedWithValue(value, nameof(ShowingBulletin));

                // Mirror property for XML simplicity (no inverse binding needed).
                ShowingActivities = !value;
            }
        }

        [DataSourceProperty]
        public bool ShowingActivities
        {
            get => _showingActivities;
            private set
            {
                if (value == _showingActivities) return;
                _showingActivities = value;
                OnPropertyChangedWithValue(value, nameof(ShowingActivities));
            }
        }

        [DataSourceProperty]
        public bool ShowingLance
        {
            get => _showingLance;
            set
            {
                if (value == _showingLance) return;
                _showingLance = value;
                OnPropertyChangedWithValue(value, nameof(ShowingLance));
            }
        }

        [DataSourceProperty]
        public string CurrentLocationId
        {
            get => _currentLocationId;
            set
            {
                if (value == _currentLocationId) return;
                _currentLocationId = value;
                OnPropertyChangedWithValue(value, nameof(CurrentLocationId));
            }
        }

        [DataSourceProperty]
        public string TitleText
        {
            get => _titleText;
            set
            {
                if (value == _titleText) return;
                _titleText = value;
                OnPropertyChangedWithValue(value, nameof(TitleText));
            }
        }

        [DataSourceProperty]
        public string DoneText
        {
            get => _doneText;
            set
            {
                if (value == _doneText) return;
                _doneText = value;
                OnPropertyChangedWithValue(value, nameof(DoneText));
            }
        }

        [DataSourceProperty]
        public string CurrentTimeText
        {
            get => _currentTimeText;
            set
            {
                if (value == _currentTimeText) return;
                _currentTimeText = value;
                OnPropertyChangedWithValue(value, nameof(CurrentTimeText));
            }
        }

        [DataSourceProperty]
        public string LocationsText
        {
            get => _locationsText;
            set
            {
                if (value == _locationsText) return;
                _locationsText = value;
                OnPropertyChangedWithValue(value, nameof(LocationsText));
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

                HasNoSelectedActivity = !value;
            }
        }

        [DataSourceProperty]
        public bool HasNoSelectedActivity
        {
            get => _hasNoSelectedActivity;
            private set
            {
                if (value == _hasNoSelectedActivity) return;
                _hasNoSelectedActivity = value;
                OnPropertyChangedWithValue(value, nameof(HasNoSelectedActivity));
            }
        }

        [DataSourceProperty]
        public LanceLeaderVM LanceLeader
        {
            get => _lanceLeader;
            set
            {
                if (value == _lanceLeader) return;
                _lanceLeader = value;
                OnPropertyChangedWithValue(value, nameof(LanceLeader));
            }
        }

        [DataSourceProperty]
        public CampBulletinFeedVM BulletinFeed
        {
            get => _bulletinFeed;
            set
            {
                if (value == _bulletinFeed) return;
                _bulletinFeed = value;
                OnPropertyChangedWithValue(value, nameof(BulletinFeed));
            }
        }

        /// <summary>
        /// Post a news item to the bulletin feed (Track A Phase 3).
        /// Used by schedule system and other features to add news.
        /// </summary>
        public void PostNews(CampBulletinNewsItemVM newsItem)
        {
            if (newsItem != null && _bulletinFeed != null)
            {
                _bulletinFeed.AddNewsItem(newsItem);
            }
        }

        [DataSourceProperty]
        public CampPlayerStatusVM PlayerStatus
        {
            get => _playerStatus;
            set
            {
                if (value == _playerStatus) return;
                _playerStatus = value;
                OnPropertyChangedWithValue(value, nameof(PlayerStatus));
            }
        }

        [DataSourceProperty]
        public CampDailyScheduleVM DailySchedule
        {
            get => _dailySchedule;
            set
            {
                if (value == _dailySchedule) return;
                _dailySchedule = value;
                OnPropertyChangedWithValue(value, nameof(DailySchedule));
            }
        }

        [DataSourceProperty]
        public CampLanceLocationVM LanceLocation
        {
            get => _lanceLocation;
            set
            {
                if (value == _lanceLocation) return;
                _lanceLocation = value;
                OnPropertyChangedWithValue(value, nameof(LanceLocation));
            }
        }

        [DataSourceProperty]
        public MBBindingList<CampBulletinNewsItemVM> LanceNews
        {
            get => _lanceNews;
            set
            {
                if (value == _lanceNews) return;
                _lanceNews = value;
                OnPropertyChangedWithValue(value, nameof(LanceNews));
            }
        }

        [DataSourceProperty]
        public MBBindingList<CampBulletinNewsItemVM> KingdomDispatches
        {
            get => _kingdomDispatches;
            set
            {
                if (value == _kingdomDispatches) return;
                _kingdomDispatches = value;
                OnPropertyChangedWithValue(value, nameof(KingdomDispatches));
            }
        }

        [DataSourceProperty]
        public MBBindingList<CampLocationButtonVM> Locations
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
        public MBBindingList<ActivityIconVM> CurrentLocationActivities
        {
            get => _currentLocationActivities;
            set
            {
                if (value == _currentLocationActivities) return;
                _currentLocationActivities = value;
                OnPropertyChangedWithValue(value, nameof(CurrentLocationActivities));
            }
        }

        [DataSourceProperty]
        public ActivityDetailVM SelectedActivity
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
        public InputKeyItemVM DoneInputKey
        {
            get => _doneInputKey;
            set
            {
                if (value == _doneInputKey) return;
                _doneInputKey = value;
                OnPropertyChangedWithValue(value, nameof(DoneInputKey));
            }
        }
    }
}

