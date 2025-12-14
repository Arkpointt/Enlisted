using System;
using System.Linq;
using Enlisted.Features.Activities;
using Enlisted.Features.Enlistment.Behaviors;
using TaleWorlds.Library;

namespace Enlisted.Features.Camp.UI
{
    /// <summary>
    /// ViewModel for a single activity card in the Camp Activities menu.
    /// Displays activity information, availability status, and visual styling.
    /// </summary>
    public class ActivityCardVM : ViewModel
    {
        private readonly CampActivityDefinition _activity;
        private readonly bool _isAvailable;
        private readonly string _unavailableReason;
        
        private string _activityId;
        private string _title;
        private string _description;
        private string _categoryIcon;
        private string _categoryColor;
        private string _categoryBadgeText;
        private string _availabilityText;
        private string _availabilityColor;
        private string _rewardsText;
        private string _costsText;
        private bool _isEnabled;
        private bool _isHighlighted;
        private bool _showPulse;
        private int _sortOrder;

        public ActivityCardVM(CampActivityDefinition activity, bool isAvailable, string unavailableReason, EnlistmentBehavior enlistment)
        {
            _activity = activity;
            _isAvailable = isAvailable;
            _unavailableReason = unavailableReason;

            ActivityId = activity.Id;
            Title = GetActivityTitle(activity);
            Description = activity.HintFallback ?? "No description available";
            
            // Visual styling based on category
            CategoryIcon = GetCategoryIcon(activity.Category);
            CategoryColor = GetCategoryColor(activity.Category);
            CategoryBadgeText = FormatCategoryBadge(activity.Category);
            
            // Availability display
            IsEnabled = isAvailable;
            IsHighlighted = isAvailable && IsSpecialActivity(activity);
            ShowPulse = IsHighlighted;
            
            if (isAvailable)
            {
                AvailabilityText = "⭐ AVAILABLE NOW";
                AvailabilityColor = "#44FF88FF"; // Bright green
            }
            else
            {
                AvailabilityText = unavailableReason ?? "Unavailable";
                AvailabilityColor = "#888888FF"; // Gray
            }
            
            // Rewards and costs summary
            RewardsText = FormatRewards(activity);
            CostsText = FormatCosts(activity);
            
            // Sort order (available activities first)
            SortOrder = isAvailable ? 0 : 1;
        }

        #region Properties

        [DataSourceProperty]
        public string ActivityId
        {
            get => _activityId;
            set
            {
                if (_activityId != value)
                {
                    _activityId = value;
                    OnPropertyChangedWithValue(value, nameof(ActivityId));
                }
            }
        }

        [DataSourceProperty]
        public string Title
        {
            get => _title;
            set
            {
                if (_title != value)
                {
                    _title = value;
                    OnPropertyChangedWithValue(value, nameof(Title));
                }
            }
        }

        [DataSourceProperty]
        public string Description
        {
            get => _description;
            set
            {
                if (_description != value)
                {
                    _description = value;
                    OnPropertyChangedWithValue(value, nameof(Description));
                }
            }
        }

        [DataSourceProperty]
        public string CategoryIcon
        {
            get => _categoryIcon;
            set
            {
                if (_categoryIcon != value)
                {
                    _categoryIcon = value;
                    OnPropertyChangedWithValue(value, nameof(CategoryIcon));
                }
            }
        }

        [DataSourceProperty]
        public string CategoryColor
        {
            get => _categoryColor;
            set
            {
                if (_categoryColor != value)
                {
                    _categoryColor = value;
                    OnPropertyChangedWithValue(value, nameof(CategoryColor));
                }
            }
        }

        [DataSourceProperty]
        public string CategoryBadgeText
        {
            get => _categoryBadgeText;
            set
            {
                if (_categoryBadgeText != value)
                {
                    _categoryBadgeText = value;
                    OnPropertyChangedWithValue(value, nameof(CategoryBadgeText));
                }
            }
        }

        [DataSourceProperty]
        public string AvailabilityText
        {
            get => _availabilityText;
            set
            {
                if (_availabilityText != value)
                {
                    _availabilityText = value;
                    OnPropertyChangedWithValue(value, nameof(AvailabilityText));
                }
            }
        }

        [DataSourceProperty]
        public string AvailabilityColor
        {
            get => _availabilityColor;
            set
            {
                if (_availabilityColor != value)
                {
                    _availabilityColor = value;
                    OnPropertyChangedWithValue(value, nameof(AvailabilityColor));
                }
            }
        }

        [DataSourceProperty]
        public string RewardsText
        {
            get => _rewardsText;
            set
            {
                if (_rewardsText != value)
                {
                    _rewardsText = value;
                    OnPropertyChangedWithValue(value, nameof(RewardsText));
                }
            }
        }

        [DataSourceProperty]
        public string CostsText
        {
            get => _costsText;
            set
            {
                if (_costsText != value)
                {
                    _costsText = value;
                    OnPropertyChangedWithValue(value, nameof(CostsText));
                }
            }
        }

        [DataSourceProperty]
        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (_isEnabled != value)
                {
                    _isEnabled = value;
                    OnPropertyChangedWithValue(value, nameof(IsEnabled));
                }
            }
        }

        [DataSourceProperty]
        public bool IsHighlighted
        {
            get => _isHighlighted;
            set
            {
                if (_isHighlighted != value)
                {
                    _isHighlighted = value;
                    OnPropertyChangedWithValue(value, nameof(IsHighlighted));
                }
            }
        }

        [DataSourceProperty]
        public bool ShowPulse
        {
            get => _showPulse;
            set
            {
                if (_showPulse != value)
                {
                    _showPulse = value;
                    OnPropertyChangedWithValue(value, nameof(ShowPulse));
                }
            }
        }

        [DataSourceProperty]
        public int SortOrder
        {
            get => _sortOrder;
            set
            {
                if (_sortOrder != value)
                {
                    _sortOrder = value;
                    OnPropertyChangedWithValue(value, nameof(SortOrder));
                }
            }
        }

        #endregion

        #region Helper Methods

        private string GetActivityTitle(CampActivityDefinition activity)
        {
            // Extract just the activity name from the full text (remove XP/Fatigue info)
            var fullText = activity.TextFallback ?? "Unknown Activity";
            var bracketIndex = fullText.IndexOf('[');
            if (bracketIndex > 0)
            {
                return fullText.Substring(0, bracketIndex).Trim();
            }
            return fullText;
        }

        private string GetCategoryIcon(string category)
        {
            // CRITICAL: All hex colors must be 8-digit (#RRGGBBAA) as required by TaleWorlds.Library.Color.ConvertStringToColor()
            return category?.ToLowerInvariant() switch
            {
                "training" => "⚔",
                "tasks" => "📋",
                "social" => "🔥",
                "duty" => "⚙",
                "medical" => "❤",
                "leisure" => "🎲",
                "lance" => "⛺",
                _ => "•"
            };
        }

        private string GetCategoryColor(string category)
        {
            // CRITICAL: All hex colors must be 8-digit (#RRGGBBAA) as required by TaleWorlds.Library.Color.ConvertStringToColor()
            return category?.ToLowerInvariant() switch
            {
                "training" => "#FFAA33FF",    // Orange - combat/training activities
                "tasks" => "#4488FFFF",       // Blue - duty and task activities
                "social" => "#44AA44FF",      // Green - social/morale activities
                "duty" => "#8844FFFF",        // Purple - assigned duties
                "medical" => "#DD4444FF",     // Red - medical activities
                "leisure" => "#FFCC44FF",     // Yellow - leisure/gambling
                "lance" => "#44AAFFFF",       // Light blue - lance activities
                _ => "#FFFFFFFF"              // White - default
            };
        }

        private string FormatCategoryBadge(string category)
        {
            return category?.ToUpperInvariant() switch
            {
                "TRAINING" => "TRAINING",
                "TASKS" => "DUTIES",
                "SOCIAL" => "SOCIAL",
                "DUTY" => "DUTY",
                "MEDICAL" => "MEDICAL",
                "LEISURE" => "LEISURE",
                "LANCE" => "LANCE",
                _ => "ACTIVITY"
            };
        }

        private string FormatRewards(CampActivityDefinition activity)
        {
            if (activity.SkillXp == null || !activity.SkillXp.Any())
            {
                return activity.FatigueRelief > 0 
                    ? $"Restores {activity.FatigueRelief} Fatigue" 
                    : "—";
            }

            var totalXp = activity.SkillXp.Values.Sum();
            var skills = string.Join(", ", activity.SkillXp.Keys.Take(2));
            
            if (activity.SkillXp.Count > 2)
            {
                return $"+{totalXp} XP ({skills}, +{activity.SkillXp.Count - 2} more)";
            }
            
            return $"+{totalXp} XP ({skills})";
        }

        private string FormatCosts(CampActivityDefinition activity)
        {
            if (activity.FatigueCost <= 0)
            {
                return "No Cost";
            }
            
            return $"{activity.FatigueCost} Fatigue";
        }

        private bool IsSpecialActivity(CampActivityDefinition activity)
        {
            // Highlight certain special activities that players should notice
            var specialIds = new[]
            {
                "social.campfire_stories",
                "social.dice_game",
                "training.formation_drill"
            };
            
            return specialIds.Contains(activity.Id, StringComparer.OrdinalIgnoreCase);
        }

        #endregion

        public CampActivityDefinition GetActivity() => _activity;
        
        public bool GetIsAvailable() => _isAvailable;
    }
}
