using System.Linq;
using Enlisted.Features.Activities;
using Enlisted.Features.Enlistment.Behaviors;
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
        private string _iconSprite;
        private string _rewardsText;
        private string _costsText;
        private string _energyAvailableText;
        private string _energyCostText;
        private string _performButtonText;
        private string _requirementsText;
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
            IconSprite = GetIconSprite(_activity);
            RewardsText = BuildRewardsText();
            CostsText = BuildCostsText();
            EnergyCostText = $"Energy Cost: {_activity.FatigueCost}";
            EnergyAvailableText = BuildEnergyAvailableText();
            RequirementsText = BuildRequirementsText();
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

        private string BuildEnergyAvailableText()
        {
            var enlistment = EnlistmentBehavior.Instance;
            var max = enlistment?.FatigueMax ?? 24;
            var used = enlistment?.FatigueCurrent ?? 0;
            var available = max - used;
            if (available < 0) available = 0;
            return $"Energy Available: {available}/{max}";
        }

        /// <summary>
        /// Build the requirements text showing tier and role requirements.
        /// </summary>
        private string BuildRequirementsText()
        {
            var requirements = new System.Collections.Generic.List<string>();

            // Tier requirements
            if (_activity.MaxTier > 0)
            {
                requirements.Add($"Tier {_activity.MinTier}-{_activity.MaxTier}");
            }
            else if (_activity.MinTier > 1)
            {
                requirements.Add($"Tier {_activity.MinTier}+");
            }

            // Lance Leader requirement
            if (_activity.RequiresLanceLeader)
            {
                requirements.Add("Lance Leader");
            }

            // Formation requirements
            if (_activity.Formations != null && _activity.Formations.Any())
            {
                var formationNames = string.Join("/", _activity.Formations.Select(f =>
                {
                    return f switch
                    {
                        "infantry" => "Infantry",
                        "archer" => "Archer",
                        "cavalry" => "Cavalry",
                        "horsearcher" => "Horse Archer",
                        _ => f
                    };
                }));
                requirements.Add(formationNames);
            }

            // Day part requirements
            if (_activity.DayParts != null && _activity.DayParts.Any() &&
                !_activity.DayParts.Any(dp => string.Equals(dp, "anytime", System.StringComparison.OrdinalIgnoreCase)))
            {
                var dayPartNames = string.Join("/", _activity.DayParts.Select(dp =>
                {
                    return dp switch
                    {
                        "dawn" => "Dawn",
                        "morning" => "Morning",
                        "afternoon" => "Afternoon",
                        "evening" => "Evening",
                        "dusk" => "Dusk",
                        "night" => "Night",
                        _ => dp
                    };
                }));
                requirements.Add(dayPartNames);
            }

            return requirements.Any() ? string.Join(" | ", requirements) : "No requirements";
        }

        private string GetIconSprite(CampActivityDefinition activity)
        {
            // Keep this consistent with ActivityIconVM until activities have proper art assets.
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
        public string IconSprite
        {
            get => _iconSprite;
            set
            {
                if (value == _iconSprite) return;
                _iconSprite = value;
                OnPropertyChangedWithValue(value, nameof(IconSprite));
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
        public string EnergyAvailableText
        {
            get => _energyAvailableText;
            set
            {
                if (value == _energyAvailableText) return;
                _energyAvailableText = value;
                OnPropertyChangedWithValue(value, nameof(EnergyAvailableText));
            }
        }

        [DataSourceProperty]
        public string EnergyCostText
        {
            get => _energyCostText;
            set
            {
                if (value == _energyCostText) return;
                _energyCostText = value;
                OnPropertyChangedWithValue(value, nameof(EnergyCostText));
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
        public string RequirementsText
        {
            get => _requirementsText;
            set
            {
                if (value == _requirementsText) return;
                _requirementsText = value;
                OnPropertyChangedWithValue(value, nameof(RequirementsText));
            }
        }
    }
}

