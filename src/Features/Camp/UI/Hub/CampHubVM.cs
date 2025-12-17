using System;
using System.Linq;
using Enlisted.Features.Activities;
using Enlisted.Features.Assignments.Behaviors;
using Enlisted.Features.Conditions;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Features.Escalation;
using Enlisted.Mod.Core.Logging;
using Enlisted.Mod.Core.Triggers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace Enlisted.Features.Camp.UI.Hub
{
    /// <summary>
    /// Main ViewModel for the Camp Hub screen.
    /// Displays 6 location buttons for navigating to different camp areas.
    /// Phase 2: Camp Hub & Location System.
    /// </summary>
    public class CampHubVM : ViewModel
    {
        private readonly Action _onClose;
        private readonly EnlistmentBehavior _enlistment;
        private readonly CampActivitiesBehavior _activitiesBehavior;
        
        private MBBindingList<LocationButtonRowVM> _locationRows;
        private string _headerTitle;
        private string _currentTimeText;
        
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
        
        public CampHubVM(Action onClose)
        {
            _onClose = onClose;
            _enlistment = EnlistmentBehavior.Instance;
            _activitiesBehavior = CampActivitiesBehavior.Instance;
            
            LocationRows = new MBBindingList<LocationButtonRowVM>();
            HeaderTitle = "CAMP OVERVIEW";
            
            RefreshLocations();
            RefreshStatusBar();
            UpdateCurrentTime();
        }
        
        #region Properties
        
        [DataSourceProperty]
        public MBBindingList<LocationButtonRowVM> LocationRows
        {
            get => _locationRows;
            set
            {
                if (_locationRows != value)
                {
                    _locationRows = value;
                    OnPropertyChangedWithValue(value, nameof(LocationRows));
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
        
        /// <summary>
        /// Closes the Camp Hub screen.
        /// </summary>
        public void ExecuteClose()
        {
            _onClose?.Invoke();
        }
        
        #endregion
        
        #region Refresh Methods
        
        /// <summary>
        /// Refresh location buttons with current activity counts.
        /// </summary>
        private void RefreshLocations()
        {
            try
            {
                LocationRows.Clear();
                
                if (_activitiesBehavior == null || !_activitiesBehavior.IsEnabled())
                {
                    ModLogger.Warn("CampHubUI", "Activities system not enabled");
                    return;
                }
                
                var allActivities = _activitiesBehavior.GetAllActivities();
                if (allActivities == null)
                {
                    ModLogger.Warn("CampHubUI", "No activities loaded");
                    return;
                }
                
                // Get current context for filtering
                var currentDay = (int)CampaignTime.Now.ToDays;
                var dayPart = CampaignTriggerTrackerBehavior.Instance?.GetDayPart();
                var dayPartToken = dayPart?.ToString().ToLowerInvariant() ?? "day";
                var formation = EnlistedDutiesBehavior.Instance?.GetPlayerFormationType()?.ToLowerInvariant() ?? "infantry";
                
                // Create location buttons for each of the 6 locations
                var locationIds = CampLocations.GetAllLocationIds();
                LocationButtonRowVM currentRow = null;
                
                foreach (var locationId in locationIds)
                {
                    // Get activities for this location
                    var activitiesAtLocation = allActivities
                        .Where(a => a.Location == locationId)
                        .ToList();
                    
                    // Filter by visibility (formation, time of day, rank)
                    var visibleActivities = activitiesAtLocation
                        .Where(a => CampActivitiesBehavior.IsActivityVisibleFor(a, _enlistment, formation, dayPartToken))
                        .ToList();
                    
                    // Count available activities (not on cooldown, not too fatigued)
                    var availableCount = visibleActivities
                        .Count(a => IsActivityAvailable(a, currentDay));
                    
                    // Create location button
                    var button = new LocationButtonVM(
                        locationId,
                        visibleActivities.Count,
                        availableCount,
                        OnLocationSelected
                    );
                    
                    // Organize into rows of 3
                    if (currentRow == null || currentRow.IsFull)
                    {
                        currentRow = new LocationButtonRowVM();
                        LocationRows.Add(currentRow);
                    }
                    
                    currentRow.AddButton(button);
                }
                
                ModLogger.Debug("CampHubUI", $"Loaded {locationIds.Length} locations in {LocationRows.Count} rows");
            }
            catch (Exception ex)
            {
                ModLogger.Error("CampHubUI", $"Failed to refresh locations: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// Check if an activity is currently available (not on cooldown, not too fatigued).
        /// </summary>
        private bool IsActivityAvailable(CampActivityDefinition activity, int currentDay)
        {
            if (activity == null || _enlistment == null)
                return false;
            
            // Check severe conditions
            if (activity.BlockOnSevereCondition)
            {
                var conditionState = PlayerConditionBehavior.Instance?.State;
                if (conditionState != null && (conditionState.InjuryDaysRemaining > 7 || conditionState.IllnessDaysRemaining > 7))
                {
                    return false;
                }
            }
            
            // Check fatigue
            if (activity.FatigueCost > 0)
            {
                var currentFatigue = _enlistment.FatigueCurrent;
                var maxFatigue = _enlistment.FatigueMax;
                
                if (currentFatigue + activity.FatigueCost > maxFatigue)
                {
                    return false;
                }
            }
            
            // Check cooldown
            if (_activitiesBehavior.TryGetCooldownDaysRemaining(activity, currentDay, out _))
            {
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// Called when a location button is clicked.
        /// Opens the area detail screen for the selected location.
        /// </summary>
        private void OnLocationSelected(string locationId)
        {
            try
            {
                ModLogger.Debug("CampHubUI", $"Opening area screen for location: {locationId}");
                
                // Open the area screen for this location (will be created in next step)
                // For now, this will open the existing CampActivitiesScreen
                // We'll refactor it to CampAreaScreen next
                UI.Areas.CampAreaScreen.Open(locationId, () =>
                {
                    // Refresh location counts when returning from area screen
                    RefreshLocations();
                });
            }
            catch (Exception ex)
            {
                ModLogger.Error("CampHubUI", $"Failed to open area screen: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// Refresh the status bar with current player state.
        /// </summary>
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
                ModLogger.Error("CampHubUI", $"Failed to refresh status bar: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// Update the current time display.
        /// </summary>
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
                ModLogger.Error("CampHubUI", $"Failed to update time: {ex.Message}", ex);
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
                return "#DD4444FF"; // Red - exhausted
            if (ratio >= 0.6f)
                return "#FFAA33FF"; // Orange - tired
            
            return "#FFFFFFFF"; // White - okay
        }
        
        private string GetHeatColor(int heat)
        {
            if (heat >= 7)
                return "#DD4444FF"; // Red - danger
            if (heat >= 5)
                return "#FFAA33FF"; // Orange - warning
            if (heat >= 3)
                return "#FFCC44FF"; // Yellow - caution
            
            return "#FFFFFFFF"; // White - safe
        }
        
        private string GetLanceRepColor(int rep)
        {
            if (rep >= 20)
                return "#44FF88FF"; // Bright green - trusted
            if (rep >= 0)
                return "#FFFFFFFF"; // White - neutral
            if (rep >= -20)
                return "#FFAA33FF"; // Orange - strained
            
            return "#DD4444FF"; // Red - hostile
        }
        
        #endregion
    }
}

