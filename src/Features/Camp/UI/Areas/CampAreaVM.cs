using System;
using System.Collections.Generic;
using System.Linq;
using Enlisted.Features.Activities;
using Enlisted.Features.Assignments.Behaviors;
using Enlisted.Features.Camp.UI.Hub;
using Enlisted.Features.Conditions;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Features.Escalation;
using Enlisted.Mod.Core.Logging;
using Enlisted.Mod.Core.Triggers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace Enlisted.Features.Camp.UI.Areas
{
    /// <summary>
    /// ViewModel for the Camp Area screen - displays activities for a specific location.
    /// Phase 2: Refactored from CampActivitiesVM to support location-based filtering.
    /// </summary>
    public class CampAreaVM : ViewModel
    {
        private readonly string _locationId;
        private readonly Action _onClose;
        private readonly EnlistmentBehavior _enlistment;
        private readonly CampActivitiesBehavior _activitiesBehavior;
        
        private MBBindingList<ActivityCardVM> _activities;
        private MBBindingList<ActivityCardRowVM> _activityRows;
        private string _currentTimeText;
        private string _headerTitle;
        private int _availableCount;
        private string _availableCountText;
        private int _totalCount;
        
        // Status bar properties
        private int _fatigue;
        private int _maxFatigue;
        private string _fatigueText;
        private string _fatigueColor;
        private int _heat;
        private string _heatText;
        private string _heatColor;
        private int _lanceRep;
        private string _lanceRepText;
        private string _lanceRepColor;
        private int _payOwed;
        private string _payOwedText;
        
        public CampAreaVM(string locationId, Action onClose)
        {
            _locationId = locationId;
            _onClose = onClose;
            _enlistment = EnlistmentBehavior.Instance;
            _activitiesBehavior = CampActivitiesBehavior.Instance;
            
            Activities = new MBBindingList<ActivityCardVM>();
            ActivityRows = new MBBindingList<ActivityCardRowVM>();
            
            // Set header based on location
            HeaderTitle = CampLocations.GetLocationHeaderTitle(locationId);
            
            RefreshActivities();
            RefreshStatusBar();
            UpdateCurrentTime();
        }
        
        #region Properties
        
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
        public MBBindingList<ActivityCardRowVM> ActivityRows
        {
            get => _activityRows;
            set
            {
                if (_activityRows != value)
                {
                    _activityRows = value;
                    OnPropertyChangedWithValue(value, nameof(ActivityRows));
                }
            }
        }
        
        [DataSourceProperty]
        public string HeaderTitle
        {
            get => _headerTitle;
            set
            {
                if (_headerTitle != value)
                {
                    _headerTitle = value;
                    OnPropertyChangedWithValue(value, nameof(HeaderTitle));
                }
            }
        }
        
        [DataSourceProperty]
        public string CurrentTimeText
        {
            get => _currentTimeText;
            set
            {
                if (_currentTimeText != value)
                {
                    _currentTimeText = value;
                    OnPropertyChangedWithValue(value, nameof(CurrentTimeText));
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
        public string AvailableCountText
        {
            get => _availableCountText;
            set
            {
                if (_availableCountText != value)
                {
                    _availableCountText = value;
                    OnPropertyChangedWithValue(value, nameof(AvailableCountText));
                }
            }
        }
        
        [DataSourceProperty]
        public int TotalCount
        {
            get => _totalCount;
            set
            {
                if (_totalCount != value)
                {
                    _totalCount = value;
                    OnPropertyChangedWithValue(value, nameof(TotalCount));
                }
            }
        }
        
        [DataSourceProperty]
        public int Fatigue
        {
            get => _fatigue;
            set
            {
                if (_fatigue != value)
                {
                    _fatigue = value;
                    OnPropertyChangedWithValue(value, nameof(Fatigue));
                }
            }
        }
        
        [DataSourceProperty]
        public int MaxFatigue
        {
            get => _maxFatigue;
            set
            {
                if (_maxFatigue != value)
                {
                    _maxFatigue = value;
                    OnPropertyChangedWithValue(value, nameof(MaxFatigue));
                }
            }
        }
        
        [DataSourceProperty]
        public string FatigueText
        {
            get => _fatigueText;
            set
            {
                if (_fatigueText != value)
                {
                    _fatigueText = value;
                    OnPropertyChangedWithValue(value, nameof(FatigueText));
                }
            }
        }
        
        [DataSourceProperty]
        public string FatigueColor
        {
            get => _fatigueColor;
            set
            {
                if (_fatigueColor != value)
                {
                    _fatigueColor = value;
                    OnPropertyChangedWithValue(value, nameof(FatigueColor));
                }
            }
        }
        
        [DataSourceProperty]
        public int Heat
        {
            get => _heat;
            set
            {
                if (_heat != value)
                {
                    _heat = value;
                    OnPropertyChangedWithValue(value, nameof(Heat));
                }
            }
        }
        
        [DataSourceProperty]
        public string HeatText
        {
            get => _heatText;
            set
            {
                if (_heatText != value)
                {
                    _heatText = value;
                    OnPropertyChangedWithValue(value, nameof(HeatText));
                }
            }
        }
        
        [DataSourceProperty]
        public string HeatColor
        {
            get => _heatColor;
            set
            {
                if (_heatColor != value)
                {
                    _heatColor = value;
                    OnPropertyChangedWithValue(value, nameof(HeatColor));
                }
            }
        }
        
        [DataSourceProperty]
        public int LanceRep
        {
            get => _lanceRep;
            set
            {
                if (_lanceRep != value)
                {
                    _lanceRep = value;
                    OnPropertyChangedWithValue(value, nameof(LanceRep));
                }
            }
        }
        
        [DataSourceProperty]
        public string LanceRepText
        {
            get => _lanceRepText;
            set
            {
                if (_lanceRepText != value)
                {
                    _lanceRepText = value;
                    OnPropertyChangedWithValue(value, nameof(LanceRepText));
                }
            }
        }
        
        [DataSourceProperty]
        public string LanceRepColor
        {
            get => _lanceRepColor;
            set
            {
                if (_lanceRepColor != value)
                {
                    _lanceRepColor = value;
                    OnPropertyChangedWithValue(value, nameof(LanceRepColor));
                }
            }
        }
        
        [DataSourceProperty]
        public int PayOwed
        {
            get => _payOwed;
            set
            {
                if (_payOwed != value)
                {
                    _payOwed = value;
                    OnPropertyChangedWithValue(value, nameof(PayOwed));
                }
            }
        }
        
        [DataSourceProperty]
        public string PayOwedText
        {
            get => _payOwedText;
            set
            {
                if (_payOwedText != value)
                {
                    _payOwedText = value;
                    OnPropertyChangedWithValue(value, nameof(PayOwedText));
                }
            }
        }
        
        #endregion
        
        #region Commands
        
        public void ExecuteClose()
        {
            _onClose?.Invoke();
        }
        
        public void ExecuteSelectActivity(ActivityCardVM activityCard)
        {
            if (activityCard == null)
            {
                return;
            }
            
            var activity = activityCard.GetActivity();
            if (activity == null)
            {
                return;
            }
            
            if (!activityCard.GetIsAvailable())
            {
                // Show why it's unavailable
                InformationManager.DisplayMessage(
                    new InformationMessage(activityCard.AvailabilityText, new Color(0.8f, 0.3f, 0.3f)));
                return;
            }
            
            // Execute the activity
            try
            {
                var success = _activitiesBehavior.TryExecuteActivity(activity, out var failureReason);
                
                if (success)
                {
                    // Success! Show feedback
                    InformationManager.DisplayMessage(
                        new InformationMessage($"✓ {activityCard.Title} completed!", new Color(0.3f, 0.8f, 0.3f)));
                    
                    // Refresh the screen to show new cooldowns/availability
                    RefreshActivities();
                    RefreshStatusBar();
                }
                else
                {
                    // Failed - show reason
                    InformationManager.DisplayMessage(
                        new InformationMessage(failureReason ?? "Activity unavailable", new Color(0.8f, 0.3f, 0.3f)));
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("CampAreaUI", $"Failed to execute activity {activity.Id}: {ex.Message}", ex);
                InformationManager.DisplayMessage(
                    new InformationMessage("Failed to complete activity", new Color(0.8f, 0.3f, 0.3f)));
            }
        }
        
        #endregion
        
        #region Refresh Methods
        
        private void RefreshActivities()
        {
            try
            {
                Activities.Clear();
                
                if (_activitiesBehavior == null || !_activitiesBehavior.IsEnabled())
                {
                    ModLogger.Warn("CampAreaUI", "Activities system not enabled");
                    return;
                }
                
                var allActivities = _activitiesBehavior.GetAllActivities();
                if (allActivities == null || !allActivities.Any())
                {
                    ModLogger.Warn("CampAreaUI", "No activities loaded");
                    return;
                }
                
                // FILTER BY LOCATION - This is the key difference from CampActivitiesVM
                var activitiesAtLocation = allActivities
                    .Where(a => a.Location == _locationId)
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
                TotalCount = Activities.Count;
                AvailableCountText = $"{AvailableCount} Available";
                
                // Organize cards into rows of 3 for grid layout
                OrganizeCardsIntoRows();
                
                ModLogger.Debug("CampAreaUI", $"Loaded {TotalCount} activities ({AvailableCount} available) at {_locationId} in {ActivityRows.Count} rows");
            }
            catch (Exception ex)
            {
                ModLogger.Error("CampAreaUI", $"Failed to refresh activities: {ex.Message}", ex);
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
        
        private void RefreshStatusBar()
        {
            try
            {
                // Fatigue - calculate pixel width for 200px bar
                var fatigueCurrent = _enlistment?.FatigueCurrent ?? 0;
                var fatigueMax = _enlistment?.FatigueMax ?? 24;
                MaxFatigue = fatigueMax;
                Fatigue = (int)(200.0f * fatigueCurrent / Math.Max(1, fatigueMax));
                FatigueText = $"{fatigueCurrent} / {fatigueMax}";
                FatigueColor = GetFatigueColor(fatigueCurrent, fatigueMax);
                
                // Heat - calculate pixel width for 200px bar (max 10)
                var escalation = EscalationManager.Instance;
                var heatValue = escalation?.State?.Heat ?? 0;
                Heat = (int)(200.0f * heatValue / 10.0f);
                HeatText = $"{heatValue} / 10";
                HeatColor = GetHeatColor(heatValue);
                
                // Lance Rep - calculate pixel width for 200px bar (range -50 to +50, normalize to 0-200)
                var repValue = escalation?.State?.LanceReputation ?? 0;
                var repNormalized = (repValue + 50) / 100.0f; // Map -50..50 to 0..1
                LanceRep = (int)(200.0f * Math.Max(0f, Math.Min(1f, repNormalized)));
                LanceRepText = $"{repValue:+#;-#;0}";
                LanceRepColor = GetLanceRepColor(repValue);
                
                // Pay
                PayOwed = _enlistment?.PendingMusterPay ?? 0;
                PayOwedText = $"{PayOwed} denars";
            }
            catch (Exception ex)
            {
                ModLogger.Error("CampAreaUI", $"Failed to refresh status bar: {ex.Message}", ex);
            }
        }
        
        private void OrganizeCardsIntoRows()
        {
            ActivityRows.Clear();
            
            ActivityCardRowVM currentRow = null;
            
            foreach (var card in Activities)
            {
                if (currentRow == null || currentRow.IsFull)
                {
                    currentRow = new ActivityCardRowVM();
                    ActivityRows.Add(currentRow);
                }
                
                currentRow.AddCard(card);
            }
        }
        
        private void UpdateCurrentTime()
        {
            try
            {
                var hour = CampaignTime.Now.CurrentHourInDay;
                var dayPartName = GetDayPartName();
                var hourInt = (int)hour;
                var minuteInt = (int)((hour - hourInt) * 60);
                
                CurrentTimeText = $"🕐 {dayPartName}, {hourInt:00}:{minuteInt:00}";
            }
            catch (Exception ex)
            {
                ModLogger.Error("CampAreaUI", $"Failed to update time: {ex.Message}", ex);
                CurrentTimeText = "Time Unknown";
            }
        }
        
        #endregion
        
        #region Helper Methods
        
        private string GetDayPartName()
        {
            var dayPart = CampaignTriggerTrackerBehavior.Instance?.GetDayPart();
            return dayPart?.ToString() ?? "Day";
        }
        
        private string GetFatigueColor(int fatigue, int maxFatigue)
        {
            var ratio = (float)fatigue / maxFatigue;
            
            if (ratio >= 0.8f)
            {
                return "#DD4444FF"; // Red - exhausted
            }
            if (ratio >= 0.6f)
            {
                return "#FFAA33FF"; // Orange - tired
            }
            
            return "#FFFFFFFF"; // White - okay
        }
        
        private string GetHeatColor(int heat)
        {
            if (heat >= 7)
            {
                return "#DD4444FF"; // Red - danger
            }
            if (heat >= 5)
            {
                return "#FFAA33FF"; // Orange - warning
            }
            if (heat >= 3)
            {
                return "#FFCC44FF"; // Yellow - caution
            }
            
            return "#FFFFFFFF"; // White - safe
        }
        
        private string GetLanceRepColor(int rep)
        {
            if (rep >= 20)
            {
                return "#44FF88FF"; // Bright green - trusted
            }
            if (rep >= 0)
            {
                return "#FFFFFFFF"; // White - neutral
            }
            if (rep >= -20)
            {
                return "#FFAA33FF"; // Orange - strained
            }
            
            return "#DD4444FF"; // Red - hostile
        }
        
        public override void OnFinalize()
        {
            // Cleanup if needed
        }
        
        #endregion
    }
}

