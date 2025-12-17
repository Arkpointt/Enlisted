using TaleWorlds.Library;

namespace Enlisted.Features.Camp.UI.Bulletin
{
    /// <summary>
    /// One entry in the camp "Queue" panel showing a schedule block.
    /// Track A Phase 3: Enhanced with Track B schedule integration.
    /// </summary>
    public class CampScheduleQueueItemVM : ViewModel
    {
        private string _timeText;
        private string _taskText;
        private bool _isActive;
        private bool _isCompleted;

        public CampScheduleQueueItemVM(string timeText, string taskText, bool isActive = false, bool isCompleted = false)
        {
            TimeText = timeText;
            TaskText = taskText;
            IsActive = isActive;
            IsCompleted = isCompleted;
        }

        [DataSourceProperty]
        public string TimeText
        {
            get => _timeText;
            set
            {
                if (value == _timeText) return;
                _timeText = value;
                OnPropertyChangedWithValue(value, nameof(TimeText));
            }
        }

        [DataSourceProperty]
        public string TaskText
        {
            get => _taskText;
            set
            {
                if (value == _taskText) return;
                _taskText = value;
                OnPropertyChangedWithValue(value, nameof(TaskText));
            }
        }

        /// <summary>Whether this is the currently active schedule block (Phase 3)</summary>
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

        /// <summary>Whether this schedule block has been completed (Phase 3)</summary>
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
    }
}


