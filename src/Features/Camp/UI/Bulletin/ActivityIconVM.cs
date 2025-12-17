using System;
using System.Linq;
using Enlisted.Features.Activities;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Mod.Core.Triggers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace Enlisted.Features.Camp.UI.Bulletin
{
    /// <summary>
    /// ViewModel for an activity icon in the grid (like project icons in settlement management).
    /// </summary>
    public class ActivityIconVM : ViewModel
    {
        private readonly Action<ActivityIconVM> _onSelectAction;

        private string _activityName;
        private string _activityShortName;
        private string _durationText;
        private string _iconPath;
        private bool _isAvailable;
        private float _availabilityAlpha;
        private string _unavailableReason;
        private CampActivityDefinition _activity;
        private bool _isSelected;

        public ActivityIconVM(CampActivityDefinition activity, Action<ActivityIconVM> onSelectAction)
        {
            _activity = activity;
            _onSelectAction = onSelectAction;
            RefreshValues();
        }

        public override void RefreshValues()
        {
            base.RefreshValues();

            // Extract activity name from TextFallback
            var fullText = _activity.TextFallback ?? "Unknown Activity";
            var bracketIndex = fullText.IndexOf('[');
            ActivityName = bracketIndex > 0 ? fullText.Substring(0, bracketIndex).Trim() : fullText;
            ActivityShortName = GetShortName(ActivityName);
            DurationText = $"({_activity.FatigueCost} energy)";
            IconPath = GetIconPath(_activity);

            // Check availability
            IsAvailable = CheckAvailability(out var reason);
            UnavailableReason = reason;
            AvailabilityAlpha = IsAvailable ? 1f : 0.35f;
        }

        /// <summary>
        /// Check if the activity is currently available to the player.
        /// Includes tier gating and Lance Leader role requirements.
        /// </summary>
        private bool CheckAvailability(out string reason)
        {
            reason = string.Empty;

            var enlistment = EnlistmentBehavior.Instance;
            var behavior = CampActivitiesBehavior.Instance;

            if (enlistment == null || behavior == null)
            {
                reason = "System not initialized";
                return false;
            }

            int playerTier = enlistment.EnlistmentTier;

            // Check minimum tier requirement
            if (_activity.MinTier > playerTier)
            {
                reason = $"Requires Tier {_activity.MinTier}+";
                return false;
            }

            // Check maximum tier requirement (0 = no limit)
            if (_activity.MaxTier > 0 && playerTier > _activity.MaxTier)
            {
                reason = $"Only for Tier {_activity.MaxTier} and below";
                return false;
            }

            // Check Lance Leader requirement
            if (_activity.RequiresLanceLeader)
            {
                var scheduleBehavior = Schedule.Behaviors.ScheduleBehavior.Instance;
                bool isLanceLeader = scheduleBehavior?.CanUseManualManagement() ?? false;
                if (!isLanceLeader)
                {
                    reason = "Requires Lance Leader promotion";
                    return false;
                }
            }

            // Check time period requirement (DayParts is a list)
            if (_activity.DayParts != null && _activity.DayParts.Any())
            {
                var currentPeriod = CampaignTriggerTrackerBehavior.Instance?.GetDayPart().ToString().ToLowerInvariant() ?? "morning";
                if (!_activity.DayParts.Any(dp => string.Equals(dp, currentPeriod, StringComparison.OrdinalIgnoreCase)) &&
                    !_activity.DayParts.Any(dp => string.Equals(dp, "anytime", StringComparison.OrdinalIgnoreCase)))
                {
                    var required = string.Join("/", _activity.DayParts);
                    reason = $"Only during {required}";
                    return false;
                }
            }

            // Check cooldown
            var currentDay = (int)CampaignTime.Now.ToDays;
            if (behavior.TryGetCooldownDaysRemaining(_activity, currentDay, out var daysRemaining))
            {
                reason = $"On cooldown ({daysRemaining}d)";
                return false;
            }

            // Check fatigue cost
            if (_activity.FatigueCost > 0 && enlistment.FatigueCurrent + _activity.FatigueCost > enlistment.FatigueMax)
            {
                reason = "Not enough energy";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Get icon path for the activity (simplified for now).
        /// </summary>
        private string GetIconPath(CampActivityDefinition activity)
        {
            // Use sprites we know exist in the native TownManagement sprite category (ui_town_management).
            // This avoids "blank icon" circles which look wrong compared to vanilla.
            return activity.Location switch
            {
                "medical_tent" => "SPGeneral\\TownManagement\\VillageIcons\\grain",
                "training_grounds" => "SPGeneral\\TownManagement\\production_icon",
                "lords_tent" => "SPGeneral\\TownManagement\\project_popup_hammer_icon",
                "quartermaster" => "General\\Icons\\Coin@2x",
                "personal_quarters" => "SPGeneral\\TownManagement\\VillageIcons\\grape",
                "camp_fire" => "SPGeneral\\TownManagement\\VillageIcons\\hard_wood",
                _ => "SPGeneral\\TownManagement\\production_icon"
            };
        }

        private string GetShortName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "?";

            // Keep it readable inside a circular icon.
            // Prefer first word for long names; fall back to first 10 characters.
            var firstWord = name.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(firstWord) && firstWord.Length <= 10)
            {
                return firstWord;
            }

            return name.Length <= 10 ? name : name.Substring(0, 10);
        }

        /// <summary>
        /// Command: Select this activity to view details.
        /// </summary>
        public void ExecuteSelect()
        {
            // #region agent log
            System.IO.File.AppendAllText(
                @"c:\Dev\Enlisted\Enlisted\.cursor\debug.log",
                $"{{\"location\":\"ActivityIconVM.cs:ExecuteSelect\",\"message\":\"Activity clicked\",\"activity\":\"{ActivityName}\",\"timestamp\":{DateTime.UtcNow.Ticks},\"sessionId\":\"debug-session\",\"runId\":\"debug7\",\"hypothesisId\":\"Act\"}}\n");
            // #endregion

            _onSelectAction?.Invoke(this);
        }

        [DataSourceProperty]
        public CampActivityDefinition Activity => _activity;

        [DataSourceProperty]
        public string ActivityName
        {
            get => _activityName;
            set
            {
                if (value == _activityName) return;
                _activityName = value;
                OnPropertyChangedWithValue(value, nameof(ActivityName));
            }
        }

        [DataSourceProperty]
        public string ActivityShortName
        {
            get => _activityShortName;
            set
            {
                if (value == _activityShortName) return;
                _activityShortName = value;
                OnPropertyChangedWithValue(value, nameof(ActivityShortName));
            }
        }

        [DataSourceProperty]
        public string DurationText
        {
            get => _durationText;
            set
            {
                if (value == _durationText) return;
                _durationText = value;
                OnPropertyChangedWithValue(value, nameof(DurationText));
            }
        }

        [DataSourceProperty]
        public string IconPath
        {
            get => _iconPath;
            set
            {
                if (value == _iconPath) return;
                _iconPath = value;
                OnPropertyChangedWithValue(value, nameof(IconPath));
            }
        }

        [DataSourceProperty]
        public bool IsAvailable
        {
            get => _isAvailable;
            set
            {
                if (value == _isAvailable) return;
                _isAvailable = value;
                OnPropertyChangedWithValue(value, nameof(IsAvailable));
            }
        }

        [DataSourceProperty]
        public float AvailabilityAlpha
        {
            get => _availabilityAlpha;
            set
            {
                if (Math.Abs(value - _availabilityAlpha) < 0.001f) return;
                _availabilityAlpha = value;
                OnPropertyChangedWithValue(value, nameof(AvailabilityAlpha));
            }
        }

        [DataSourceProperty]
        public string UnavailableReason
        {
            get => _unavailableReason;
            set
            {
                if (value == _unavailableReason) return;
                _unavailableReason = value;
                OnPropertyChangedWithValue(value, nameof(UnavailableReason));
            }
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
}

