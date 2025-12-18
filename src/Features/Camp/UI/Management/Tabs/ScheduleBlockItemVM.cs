using System;
using Enlisted.Features.Schedule.Models;
using TaleWorlds.Library;
using TaleWorlds.Localization;

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
                Title = new TextObject("{=enl_schedule_unknown}Unknown").ToString();
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
                    ScheduleBlockType.FreeTime => new TextObject("{=enl_sched_block_free_time}Free Time").ToString(),
                    ScheduleBlockType.SentryDuty => new TextObject("{=enl_sched_block_sentry_duty}Sentry Duty").ToString(),
                    ScheduleBlockType.PatrolDuty => new TextObject("{=enl_sched_block_patrol_duty}Patrol Duty").ToString(),
                    ScheduleBlockType.ScoutingMission => new TextObject("{=enl_sched_block_scouting_mission}Scouting Mission").ToString(),
                    ScheduleBlockType.ForagingDuty => new TextObject("{=enl_sched_block_foraging_duty}Foraging Duty").ToString(),
                    _ => _block.BlockType.ToString()
                };
            }
            
            Title = $"{TimeBlockText}: {activityName}";
            
            if (!string.IsNullOrWhiteSpace(_block.Description))
            {
                Description = _block.Description;
            }
            else
            {
                var t = new TextObject("{=enl_sched_block_desc_fallback}Activity for {TIME_BLOCK}");
                t.SetTextVariable("TIME_BLOCK", TimeBlockText ?? string.Empty);
                Description = t.ToString();
            }
            IsActive = _block.IsActive;
            IsCompleted = _block.IsCompleted;
            
            // Load effects
            Effects.Clear();

            var fatigue = new TextObject("{=enl_sched_effect_fatigue}Fatigue Cost: {COST}");
            fatigue.SetTextVariable("COST", _block.FatigueCost);
            Effects.Add(new EffectItemVM(fatigue.ToString()));

            var xp = new TextObject("{=enl_sched_effect_xp}XP Reward: {XP}");
            xp.SetTextVariable("XP", _block.XPReward);
            Effects.Add(new EffectItemVM(xp.ToString()));
            
            if (_block.IsCompleted)
            {
                Effects.Add(new EffectItemVM(new TextObject("{=enl_sched_status_completed}Status: Completed").ToString()));
            }
            else if (_block.IsActive)
            {
                Effects.Add(new EffectItemVM(new TextObject("{=enl_sched_status_in_progress}Status: In Progress").ToString()));
            }
            else
            {
                Effects.Add(new EffectItemVM(new TextObject("{=enl_sched_status_pending}Status: Pending").ToString()));
            }
        }
        
        private string GetTimeBlockDisplayText(TimeBlock timeBlock)
        {
            return timeBlock switch
            {
                TimeBlock.Morning => new TextObject("{=enl_timeblock_morning}Morning").ToString(),
                TimeBlock.Afternoon => new TextObject("{=enl_timeblock_afternoon}Afternoon").ToString(),
                TimeBlock.Dusk => new TextObject("{=enl_timeblock_dusk}Dusk").ToString(),
                TimeBlock.Night => new TextObject("{=enl_timeblock_night}Night").ToString(),
                _ => new TextObject("{=enl_schedule_unknown}Unknown").ToString()
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

