using System;
using System.Collections.Generic;
using System.Linq;
using Enlisted.Features.Assignments.Core;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Features.Lances.Events;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace Enlisted.Features.Lances.UI
{
    /// <summary>
    /// ViewModel for individual event choice buttons.
    /// Displays choice text, costs, rewards, and risk indicators.
    /// </summary>
    public class EventChoiceVM : ViewModel
    {
        private readonly LanceLifeEventOptionDefinition _option;
        private readonly EnlistmentBehavior _enlistment;
        private readonly int _index;
        private readonly Action<EventChoiceVM> _onSelected;

        private string _choiceText;
        private string _choiceIconPath;
        private string _riskColor;
        private bool _isRisky;
        private string _rewardsText;
        private string _costsText;
        private string _textColor;
        private bool _isEnabled;
        private bool _isDisabled;
        private string _disabledReasonText;

        public LanceLifeEventOptionDefinition Option => _option;

        public EventChoiceVM(
            LanceLifeEventOptionDefinition option, 
            EnlistmentBehavior enlistment, 
            int index, 
            Action<EventChoiceVM> onSelected)
        {
            _option = option;
            _enlistment = enlistment;
            _index = index;
            _onSelected = onSelected;

            Initialize();
        }

        private void Initialize()
        {
            // Choice text
            ChoiceText = LanceLifeEventText.Resolve(
                _option.TextId, 
                _option.TextFallback, 
                $"Choice {_index + 1}", 
                _enlistment);

            // Icon based on option type
            ChoiceIconPath = GetChoiceIcon();

            // Risk assessment
            AssessRisk();

            // Build rewards/costs display
            BuildEffectsText();

            // Check if choice is available
            CheckAvailability();

            // Text color
            TextColor = IsEnabled ? "#FFFFFF" : "#888888";
        }

        private string GetChoiceIcon()
        {
            // Determine icon based on option characteristics
            var risk = _option.Risk?.ToLowerInvariant() ?? "";
            
            if (risk.Contains("safe") || _option.SuccessChance >= 0.9f)
            {
                return "General\\Icons\\icon_check"; // Safe choice
            }
            else if (risk.Contains("risky") || _option.SuccessChance < 0.7f)
            {
                return "General\\Icons\\icon_warning"; // Risky choice
            }
            else if (_option.Costs?.Gold > 0 || _option.Effects?.Heat > 0)
            {
                return "General\\Icons\\icon_coin"; // Costly choice
            }
            else if (_option.Rewards?.SkillXp != null && _option.Rewards.SkillXp.Count > 0)
            {
                return "General\\Icons\\icon_experience"; // Learning choice
            }
            else
            {
                return "General\\Icons\\icon_arrow_right"; // Default
            }
        }

        private void AssessRisk()
        {
            var risk = _option.Risk?.ToLowerInvariant() ?? "";
            var hasFailureChance = _option.SuccessChance.HasValue && _option.SuccessChance < 1.0f;
            var hasRiskChance = _option.RiskChance.HasValue && _option.RiskChance > 0;

            IsRisky = risk.Contains("risky") || hasFailureChance || hasRiskChance;

            // Set risk color
            if (IsRisky)
            {
                if (_option.SuccessChance < 0.5f || _option.RiskChance > 0.5f)
                {
                    RiskColor = "#DD3333"; // High risk - red
                }
                else if (_option.SuccessChance < 0.7f || _option.RiskChance > 0.3f)
                {
                    RiskColor = "#FFAA33"; // Medium risk - orange
                }
                else
                {
                    RiskColor = "#FFDD44"; // Low risk - yellow
                }
            }
            else
            {
                RiskColor = "#44AA44"; // Safe - green
            }
        }

        private void BuildEffectsText()
        {
            var rewards = new List<string>();
            var costs = new List<string>();

            // Rewards
            if (_option.Rewards != null)
            {
                if (_option.Rewards.Gold > 0)
                    rewards.Add($"+{_option.Rewards.Gold}🪙");

                if (_option.Rewards.FatigueRelief > 0)
                    rewards.Add($"-{_option.Rewards.FatigueRelief} fatigue");

                if (_option.Rewards.SkillXp != null && _option.Rewards.SkillXp.Count > 0)
                {
                    var totalXp = _option.Rewards.SkillXp.Values.Sum();
                    var skills = string.Join(", ", _option.Rewards.SkillXp.Keys.Take(2));
                    rewards.Add($"+{totalXp} {skills} XP");
                }
            }

            // Effects (reputation, etc.)
            if (_option.Effects != null)
            {
                if (_option.Effects.LanceReputation > 0)
                    rewards.Add($"+{_option.Effects.LanceReputation} Rep");
                else if (_option.Effects.LanceReputation < 0)
                    costs.Add($"{_option.Effects.LanceReputation} Rep");
            }

            // Costs
            if (_option.Costs != null)
            {
                if (_option.Costs.Gold > 0)
                    costs.Add($"-{_option.Costs.Gold}🪙");

                if (_option.Costs.Fatigue > 0)
                    costs.Add($"+{_option.Costs.Fatigue} fatigue");

                if (_option.Costs.Heat > 0)
                    costs.Add($"+{_option.Costs.Heat} Heat");
            }

            // Set text
            RewardsText = rewards.Count > 0 ? string.Join(" • ", rewards) : "";
            CostsText = costs.Count > 0 ? string.Join(" • ", costs) : "";
        }

        private void CheckAvailability()
        {
            var hero = Hero.MainHero;
            var reasons = new List<string>();

            // Gold check
            if (_option.Costs?.Gold > 0 && hero.Gold < _option.Costs.Gold)
            {
                reasons.Add($"Need {_option.Costs.Gold} gold");
            }

            // Fatigue check (FatigueCurrent is REMAINING fatigue points - 24 = fresh, 0 = exhausted)
            if (_option.Costs?.Fatigue > 0 && _enlistment != null)
            {
                if (_enlistment.FatigueCurrent < _option.Costs.Fatigue)
                {
                    reasons.Add("Too fatigued");
                }
            }

            // Condition check (if specified in option)
            if (!string.IsNullOrWhiteSpace(_option.Condition))
            {
                var evaluator = new LanceLifeEventTriggerEvaluator();
                if (!evaluator.IsConditionTrue(_option.Condition, _enlistment))
                {
                    reasons.Add("Requirements not met");
                }
            }

            // Set availability
            IsEnabled = reasons.Count == 0;
            IsDisabled = !IsEnabled;
            DisabledReasonText = reasons.Count > 0 ? string.Join(" • ", reasons) : "";
        }

        public void ExecuteSelectChoice()
        {
            if (IsEnabled)
            {
                _onSelected?.Invoke(this);
            }
        }

        // Properties for data binding
        [DataSourceProperty]
        public string ChoiceText
        {
            get => _choiceText;
            set
            {
                if (_choiceText != value)
                {
                    _choiceText = value;
                    OnPropertyChangedWithValue(value, nameof(ChoiceText));
                }
            }
        }

        [DataSourceProperty]
        public string ChoiceIconPath
        {
            get => _choiceIconPath;
            set
            {
                if (_choiceIconPath != value)
                {
                    _choiceIconPath = value;
                    OnPropertyChangedWithValue(value, nameof(ChoiceIconPath));
                }
            }
        }

        [DataSourceProperty]
        public string RiskColor
        {
            get => _riskColor;
            set
            {
                if (_riskColor != value)
                {
                    _riskColor = value;
                    OnPropertyChangedWithValue(value, nameof(RiskColor));
                }
            }
        }

        [DataSourceProperty]
        public bool IsRisky
        {
            get => _isRisky;
            set
            {
                if (_isRisky != value)
                {
                    _isRisky = value;
                    OnPropertyChangedWithValue(value, nameof(IsRisky));
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
        public string TextColor
        {
            get => _textColor;
            set
            {
                if (_textColor != value)
                {
                    _textColor = value;
                    OnPropertyChangedWithValue(value, nameof(TextColor));
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
        public bool IsDisabled
        {
            get => _isDisabled;
            set
            {
                if (_isDisabled != value)
                {
                    _isDisabled = value;
                    OnPropertyChangedWithValue(value, nameof(IsDisabled));
                }
            }
        }

        [DataSourceProperty]
        public string DisabledReasonText
        {
            get => _disabledReasonText;
            set
            {
                if (_disabledReasonText != value)
                {
                    _disabledReasonText = value;
                    OnPropertyChangedWithValue(value, nameof(DisabledReasonText));
                }
            }
        }
    }
}
