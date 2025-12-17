using System;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Features.Escalation;
using Enlisted.Mod.Core.Triggers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace Enlisted.Features.Camp.UI.Bulletin
{
    /// <summary>
    /// ViewModel for the player status panel (right side, like settlement stats).
    /// </summary>
    public class CampPlayerStatusVM : ViewModel
    {
        private string _playerStatusHeaderText;
        private string _playerNameRankText;
        private string _fatigueText;
        private float _fatiguePercent;
        private string _heatText;
        private float _heatPercent;
        private string _foodText;
        private string _reputationText;
        private string _payOwedText;
        private string _incomingPayTitleText;
        private int _incomingPayAmount;
        private string _incomingPaySubText;
        private string _currentTimeFullText;
        private string _currentLocationText;

        public CampPlayerStatusVM()
        {
            RefreshValues();
        }

        public override void RefreshValues()
        {
            base.RefreshValues();

            try
            {
                PlayerStatusHeaderText = "YOUR STATUS";

                var playerHero = Hero.MainHero;
                var enlistment = EnlistmentBehavior.Instance;
                var escalation = EscalationManager.Instance;

                // Player name and rank
                var playerName = playerHero?.Name?.ToString() ?? "Unknown";
                var tierNum = enlistment?.EnlistmentTier ?? 1;
                var rank = $"Tier {tierNum}";
                PlayerNameRankText = $"{rank} {playerName}";

                // Fatigue
                var fatigue = enlistment?.FatigueCurrent ?? 0;
                var fatigueMax = enlistment?.FatigueMax ?? 24;
                FatigueText = $"Fatigue: {fatigue}/{fatigueMax}";
                FatiguePercent = fatigueMax > 0 ? (float)fatigue / fatigueMax : 0f;

                // Heat
                var heat = escalation?.State?.Heat ?? 0;
                var heatMax = 10;
                HeatText = $"Heat: {heat}/{heatMax}";
                HeatPercent = heatMax > 0 ? (float)heat / heatMax : 0f;

                // Food
                var party = MobileParty.MainParty;
                if (party != null)
                {
                    FoodText = $"Food: {party.Food:F1} ({party.FoodChange:+0.00;-0.00;0.00}/day)";
                }
                else
                {
                    FoodText = "Food: N/A";
                }

                // Reputation
                var rep = escalation?.State?.LanceReputation ?? 0;
                string repLevel;
                if (rep >= 50) repLevel = "Excellent";
                else if (rep >= 25) repLevel = "Good";
                else if (rep >= 0) repLevel = "Neutral";
                else if (rep >= -25) repLevel = "Poor";
                else repLevel = "Terrible";
                ReputationText = $"Lance Rep: {rep:+0;-#} ({repLevel})";

                // Pay owed
                var payOwed = enlistment?.PendingMusterPay ?? 0;
                PayOwedText = $"Incoming Pay: {payOwed} denars";
                IncomingPayTitleText = "Incoming Pay";
                IncomingPayAmount = payOwed;
                IncomingPaySubText = "Paid at next muster";

                // Current time
                var dayPart = CampaignTriggerTrackerBehavior.Instance?.GetDayPart() ?? DayPart.Morning;
                var hour = Campaign.Current != null ? (int)CampaignTime.Now.CurrentHourInDay : 0;
                CurrentTimeFullText = $"{dayPart}, {hour:00}:00";

                // Current location
                CurrentLocationText = "Bulletin Board";
            }
            catch (Exception ex)
            {
                // Fallback to safe defaults
                InformationManager.DisplayMessage(new InformationMessage($"Error refreshing player status: {ex.Message}"));
                PlayerStatusHeaderText = "YOUR STATUS";
                PlayerNameRankText = "Unknown";
                FatigueText = "Fatigue: 0/24";
                HeatText = "Heat: 0/10";
                FoodText = "Food: N/A";
                ReputationText = "Lance Rep: 0";
                PayOwedText = "Pay Owed: 0";
                IncomingPayTitleText = "Incoming Pay";
                IncomingPayAmount = 0;
                IncomingPaySubText = "";
                CurrentTimeFullText = "Morning, 00:00";
                CurrentLocationText = "Bulletin Board";
            }
        }

        /// <summary>
        /// Update the current location text when player navigates.
        /// </summary>
        public void UpdateCurrentLocation(string locationId, int activityCount)
        {
            var locationName = GetLocationDisplayName(locationId);
            var countText = activityCount > 0 ? $" ({activityCount} activities)" : " (no activities)";
            CurrentLocationText = $"At: {locationName}{countText}";
        }

        private string GetLocationDisplayName(string locationId)
        {
            return locationId switch
            {
                "medical_tent" => "Medical Tent",
                "training_grounds" => "Training Grounds",
                "lords_tent" => "Lord's Tent",
                "quartermaster" => "Quartermaster",
                "personal_quarters" => "Personal Quarters",
                "camp_fire" => "Camp Fire",
                _ => "Camp"
            };
        }

        [DataSourceProperty]
        public string PlayerStatusHeaderText
        {
            get => _playerStatusHeaderText;
            set
            {
                if (value == _playerStatusHeaderText) return;
                _playerStatusHeaderText = value;
                OnPropertyChangedWithValue(value, nameof(PlayerStatusHeaderText));
            }
        }

        [DataSourceProperty]
        public string PlayerNameRankText
        {
            get => _playerNameRankText;
            set
            {
                if (value == _playerNameRankText) return;
                _playerNameRankText = value;
                OnPropertyChangedWithValue(value, nameof(PlayerNameRankText));
            }
        }

        [DataSourceProperty]
        public string FatigueText
        {
            get => _fatigueText;
            set
            {
                if (value == _fatigueText) return;
                _fatigueText = value;
                OnPropertyChangedWithValue(value, nameof(FatigueText));
            }
        }

        [DataSourceProperty]
        public float FatiguePercent
        {
            get => _fatiguePercent;
            set
            {
                if (Math.Abs(value - _fatiguePercent) < 0.001f) return;
                _fatiguePercent = value;
                OnPropertyChangedWithValue(value, nameof(FatiguePercent));
            }
        }

        [DataSourceProperty]
        public string HeatText
        {
            get => _heatText;
            set
            {
                if (value == _heatText) return;
                _heatText = value;
                OnPropertyChangedWithValue(value, nameof(HeatText));
            }
        }

        [DataSourceProperty]
        public float HeatPercent
        {
            get => _heatPercent;
            set
            {
                if (Math.Abs(value - _heatPercent) < 0.001f) return;
                _heatPercent = value;
                OnPropertyChangedWithValue(value, nameof(HeatPercent));
            }
        }

        [DataSourceProperty]
        public string FoodText
        {
            get => _foodText;
            set
            {
                if (value == _foodText) return;
                _foodText = value;
                OnPropertyChangedWithValue(value, nameof(FoodText));
            }
        }

        [DataSourceProperty]
        public string ReputationText
        {
            get => _reputationText;
            set
            {
                if (value == _reputationText) return;
                _reputationText = value;
                OnPropertyChangedWithValue(value, nameof(ReputationText));
            }
        }

        [DataSourceProperty]
        public string PayOwedText
        {
            get => _payOwedText;
            set
            {
                if (value == _payOwedText) return;
                _payOwedText = value;
                OnPropertyChangedWithValue(value, nameof(PayOwedText));
            }
        }

        [DataSourceProperty]
        public string IncomingPayTitleText
        {
            get => _incomingPayTitleText;
            set
            {
                if (value == _incomingPayTitleText) return;
                _incomingPayTitleText = value;
                OnPropertyChangedWithValue(value, nameof(IncomingPayTitleText));
            }
        }

        [DataSourceProperty]
        public int IncomingPayAmount
        {
            get => _incomingPayAmount;
            set
            {
                if (value == _incomingPayAmount) return;
                _incomingPayAmount = value;
                OnPropertyChangedWithValue(value, nameof(IncomingPayAmount));
            }
        }

        [DataSourceProperty]
        public string IncomingPaySubText
        {
            get => _incomingPaySubText;
            set
            {
                if (value == _incomingPaySubText) return;
                _incomingPaySubText = value;
                OnPropertyChangedWithValue(value, nameof(IncomingPaySubText));
            }
        }

        [DataSourceProperty]
        public string CurrentTimeFullText
        {
            get => _currentTimeFullText;
            set
            {
                if (value == _currentTimeFullText) return;
                _currentTimeFullText = value;
                OnPropertyChangedWithValue(value, nameof(CurrentTimeFullText));
            }
        }

        [DataSourceProperty]
        public string CurrentLocationText
        {
            get => _currentLocationText;
            set
            {
                if (value == _currentLocationText) return;
                _currentLocationText = value;
                OnPropertyChangedWithValue(value, nameof(CurrentLocationText));
            }
        }
    }
}

