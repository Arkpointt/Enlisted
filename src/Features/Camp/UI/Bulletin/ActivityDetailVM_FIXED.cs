using System.Linq;
using Enlisted.Features.Activities;
using TaleWorlds.Library;

namespace Enlisted.Features.Camp.UI.Bulletin
{
    /// <summary>
    /// ViewModel for the selected activity details panel (like current project in settlement management).
    /// </summary>
    public class ActivityDetailVM : ViewModel
    {
        private string _activityTitle;
        private string _activityDescription;
        private string _rewardsText;
        private string _costsText;
        private string _performButtonText;
        private CampActivityDefinition _activity;

        public ActivityDetailVM(CampActivityDefinition activity)
        {
            _activity = activity;
            RefreshValues();
        }

        public override void RefreshValues()
        {
            base.RefreshValues();

            // Use TextFallback for display name
            ActivityTitle = GetActivityTitle(_activity);
            ActivityDescription = _activity.HintFallback ?? "No description available.";
            RewardsText = BuildRewardsText();
            CostsText = BuildCostsText();
            PerformButtonText = "⚡ PERFORM ACTIVITY ⚡";
        }

        /// <summary>
        /// Extract activity name from TextFallback (remove XP/fatigue brackets).
        /// </summary>
        private string GetActivityTitle(CampActivityDefinition activity)
        {
            var fullText = activity.TextFallback ?? "Unknown Activity";
            var bracketIndex = fullText.IndexOf('[');
            return bracketIndex > 0 ? fullText.Substring(0, bracketIndex).Trim() : fullText;
        }

        /// <summary>
        /// Build the rewards text summary.
        /// </summary>
        private string BuildRewardsText()
        {
            var rewards = new System.Collections.Generic.List<string>();

            if (_activity.FatigueRelief > 0)
                rewards.Add($"+{_activity.FatigueRelief} Energy");

            if (_activity.SkillXp != null && _activity.SkillXp.Any())
            {
                var totalXp = _activity.SkillXp.Values.Sum();
                var skills = string.Join(", ", _activity.SkillXp.Keys);
                rewards.Add($"+{totalXp} XP ({skills})");
            }

            return rewards.Any() ? string.Join(", ", rewards) : "No rewards";
        }

        /// <summary>
        /// Build the costs text summary.
        /// </summary>
        private string BuildCostsText()
        {
            var costs = new System.Collections.Generic.List<string>();

            if (_activity.FatigueCost > 0)
                costs.Add($"{_activity.FatigueCost} Energy");

            // Note: Duration is not stored in CampActivityDefinition, removed for now

            return costs.Any() ? string.Join(", ", costs) : "No costs";
        }

        /// <summary>
        /// Get a summary of rewards for bulletin board news items.
        /// </summary>
        public string GetRewardSummary()
        {
            return RewardsText;
        }

        [DataSourceProperty]
        public CampActivityDefinition Activity => _activity;

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
        public string RewardsText
        {
            get => _rewardsText;
            set
            {
                if (value == _rewardsText) return;
                _rewardsText = value;
                OnPropertyChangedWithValue(value, nameof(RewardsText));
            }
        }

        [DataSourceProperty]
        public string CostsText
        {
            get => _costsText;
            set
            {
                if (value == _costsText) return;
                _costsText = value;
                OnPropertyChangedWithValue(value, nameof(CostsText));
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
    }
}

