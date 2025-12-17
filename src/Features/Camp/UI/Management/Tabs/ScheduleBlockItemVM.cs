using System;
using Enlisted.Features.Schedule.Models;
using TaleWorlds.Library;

namespace Enlisted.Features.Camp.UI.Management
{
    /// <summary>
    /// ViewModel for a single schedule block in the list.
    /// Mirrors native KingdomPolicyItemVM pattern.
    /// </summary>
    public class ScheduleBlockItemVM : ViewModel
    {
        private readonly Action<ScheduleBlockItemVM> _onSelect;
        private readonly ScheduledBlock _block;
        
        private string _title;
        private string _timeBlockText;
        private string _description;
        private bool _isSelected;
        private bool _isActive;
        private bool _isCompleted;
        private MBBindingList<EffectItemVM> _effects;
        
        /// <summary>
        /// Expose the underlying ScheduledBlock for schedule modification operations.
        /// </summary>
        public ScheduledBlock Block => _block;
        
        public ScheduleBlockItemVM(ScheduledBlock block, Action<ScheduleBlockItemVM> onSelect)
        {
            _block = block;
            _onSelect = onSelect;
            Effects = new MBBindingList<EffectItemVM>();
            RefreshValues();
        }
        
        public override void RefreshValues()
        {
            base.RefreshValues();
            
            if (_block == null)
            {
                Title = "Unknown";
                TimeBlockText = "--";
                Description = "";
                return;
            }
            
            TimeBlockText = GetTimeBlockDisplayText(_block.TimeBlock);
            
            // Format title: "Dawn: Rest Period" or use BlockType if no Title
            string activityName = _block.Title;
            
            // If no title provided, use the block type
            if (string.IsNullOrEmpty(activityName))
            {
                // Convert BlockType enum to friendly text
                activityName = _block.BlockType switch
                {
                    ScheduleBlockType.FreeTime => "Free Time",
                    ScheduleBlockType.SentryDuty => "Sentry Duty",
                    ScheduleBlockType.PatrolDuty => "Patrol Duty",
                    ScheduleBlockType.ScoutingMission => "Scouting Mission",
                    ScheduleBlockType.ForagingDuty => "Foraging Duty",
                    _ => _block.BlockType.ToString()
                };
            }
            
            Title = $"{TimeBlockText}: {activityName}";
            
            Description = _block.Description ?? $"Activity for {TimeBlockText}";
            IsActive = _block.IsActive;
            IsCompleted = _block.IsCompleted;
            
            // Load effects
            Effects.Clear();
            Effects.Add(new EffectItemVM($"Fatigue Cost: {_block.FatigueCost}"));
            Effects.Add(new EffectItemVM($"XP Reward: {_block.XPReward}"));
            
            if (_block.IsCompleted)
            {
                Effects.Add(new EffectItemVM("Status: Completed"));
            }
            else if (_block.IsActive)
            {
                Effects.Add(new EffectItemVM("Status: In Progress"));
            }
            else
            {
                Effects.Add(new EffectItemVM("Status: Pending"));
            }
        }
        
        private string GetTimeBlockDisplayText(TimeBlock timeBlock)
        {
            return timeBlock switch
            {
                TimeBlock.Morning => "Morning",
                TimeBlock.Afternoon => "Afternoon",
                TimeBlock.Dusk => "Dusk",
                TimeBlock.Night => "Night",
                _ => "Unknown"
            };
        }
        
        /// <summary>
        /// Called when item is clicked in the list (native pattern).
        /// </summary>
        public void OnSelect()
        {
            _onSelect?.Invoke(this);
        }
        
        /// <summary>
        /// Alias for OnSelect (alternate pattern).
        /// </summary>
        public void ExecuteSelect()
        {
            OnSelect();
        }
        
        // ===== Properties =====
        
        [DataSourceProperty]
        public string Title
        {
            get => _title;
            set
            {
                if (value == _title) return;
                _title = value;
                OnPropertyChangedWithValue(value, nameof(Title));
                OnPropertyChanged(nameof(Name)); // Also notify Name since it's derived
            }
        }
        
        /// <summary>
        /// Alias for Title - matches native naming convention.
        /// </summary>
        [DataSourceProperty]
        public string Name => _title;
        
        [DataSourceProperty]
        public string TimeBlockText
        {
            get => _timeBlockText;
            set
            {
                if (value == _timeBlockText) return;
                _timeBlockText = value;
                OnPropertyChangedWithValue(value, nameof(TimeBlockText));
            }
        }
        
        [DataSourceProperty]
        public string Description
        {
            get => _description;
            set
            {
                if (value == _description) return;
                _description = value;
                OnPropertyChangedWithValue(value, nameof(Description));
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
        
        [DataSourceProperty]
        public bool IsActive
        {
            get => _isActive;
            set
            {
                if (value == _isActive) return;
                _isActive = value;
                OnPropertyChangedWithValue(value, nameof(IsActive));
            }
        }
        
        [DataSourceProperty]
        public bool IsCompleted
        {
            get => _isCompleted;
            set
            {
                if (value == _isCompleted) return;
                _isCompleted = value;
                OnPropertyChangedWithValue(value, nameof(IsCompleted));
            }
        }
        
        [DataSourceProperty]
        public MBBindingList<EffectItemVM> Effects
        {
            get => _effects;
            set
            {
                if (value == _effects) return;
                _effects = value;
                OnPropertyChangedWithValue(value, nameof(Effects));
            }
        }
    }
    
    /// <summary>
    /// Simple effect text item (like StringItemWithHintVM in native).
    /// </summary>
    public class EffectItemVM : ViewModel
    {
        private string _text;
        
        public EffectItemVM(string text)
        {
            Text = text;
        }
        
        [DataSourceProperty]
        public string Text
        {
            get => _text;
            set
            {
                if (value == _text) return;
                _text = value;
                OnPropertyChangedWithValue(value, nameof(Text));
            }
        }
    }
}

