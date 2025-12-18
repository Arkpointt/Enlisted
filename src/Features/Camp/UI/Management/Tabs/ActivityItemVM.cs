using System;
using Enlisted.Features.Schedule.Config;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace Enlisted.Features.Camp.UI.Management
{
    /// <summary>
    /// ViewModel for an available activity in the schedule list.
    /// Used for the "Other Policies" equivalent - activities you can assign.
    /// </summary>
    public class ActivityItemVM : ViewModel
    {
        private readonly Action<ActivityItemVM> _onSelect;
        private readonly ScheduleActivityDefinition _activity;
        
        private string _name;
        private string _description;
        private bool _isSelected;
        
        public ScheduleActivityDefinition Activity => _activity;
        
        public ActivityItemVM(ScheduleActivityDefinition activity, Action<ActivityItemVM> onSelect)
        {
            _activity = activity;
            _onSelect = onSelect;
            RefreshValues();
        }
        
        public override void RefreshValues()
        {
            base.RefreshValues();
            
            if (_activity == null)
            {
                Name = new TextObject("{=enl_schedule_unknown_activity}Unknown Activity").ToString();
                Description = "";
                return;
            }
            
            // Use Title if available, fallback to TitleKey, then Id
            Name = _activity.Title ?? _activity.TitleKey ?? _activity.Id;
            Description = _activity.Description ?? _activity.DescriptionKey ?? "";
        }
        
        /// <summary>
        /// Called when item is clicked in the list (native pattern).
        /// </summary>
        public void OnSelect()
        {
            _onSelect?.Invoke(this);
        }
        
        /// <summary>
        /// Alias for OnSelect (alternate pattern used by some native UI).
        /// </summary>
        public void ExecuteSelect()
        {
            OnSelect();
        }
        
        // ===== Properties =====
        
        [DataSourceProperty]
        public string Name
        {
            get => _name;
            set
            {
                if (value == _name) return;
                _name = value;
                OnPropertyChangedWithValue(value, nameof(Name));
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
    }
}

