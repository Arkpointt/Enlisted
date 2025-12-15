using System;
using TaleWorlds.Library;

namespace Enlisted.Features.Camp.UI.Hub
{
    /// <summary>
    /// ViewModel for a single location button in the Camp Hub.
    /// Each button represents one of the 6 camp locations (Medical Tent, Training Grounds, etc.).
    /// </summary>
    public class LocationButtonVM : ViewModel
    {
        private readonly string _locationId;
        private readonly Action<string> _onSelected;
        
        private string _locationName;
        private string _locationIcon;
        private string _locationColor;
        private int _totalActivities;
        private int _availableActivities;
        private string _availabilityText;
        private string _activityCountText;
        
        public LocationButtonVM(string locationId, int total, int available, Action<string> onSelected)
        {
            _locationId = locationId;
            _onSelected = onSelected;
            
            LocationName = CampLocations.GetLocationName(locationId);
            LocationIcon = CampLocations.GetLocationIcon(locationId);
            LocationColor = CampLocations.GetLocationColor(locationId);
            TotalActivities = total;
            AvailableActivities = available;
            ActivityCountText = $"{total} Activities";
            AvailabilityText = $"{available} Available";
        }
        
        #region Properties
        
        [DataSourceProperty]
        public string LocationName
        {
            get => _locationName;
            set
            {
                if (_locationName != value)
                {
                    _locationName = value;
                    OnPropertyChangedWithValue(value, nameof(LocationName));
                }
            }
        }
        
        [DataSourceProperty]
        public string LocationIcon
        {
            get => _locationIcon;
            set
            {
                if (_locationIcon != value)
                {
                    _locationIcon = value;
                    OnPropertyChangedWithValue(value, nameof(LocationIcon));
                }
            }
        }
        
        [DataSourceProperty]
        public string LocationColor
        {
            get => _locationColor;
            set
            {
                if (_locationColor != value)
                {
                    _locationColor = value;
                    OnPropertyChangedWithValue(value, nameof(LocationColor));
                }
            }
        }
        
        [DataSourceProperty]
        public int TotalActivities
        {
            get => _totalActivities;
            set
            {
                if (_totalActivities != value)
                {
                    _totalActivities = value;
                    OnPropertyChangedWithValue(value, nameof(TotalActivities));
                }
            }
        }
        
        [DataSourceProperty]
        public int AvailableActivities
        {
            get => _availableActivities;
            set
            {
                if (_availableActivities != value)
                {
                    _availableActivities = value;
                    OnPropertyChangedWithValue(value, nameof(AvailableActivities));
                }
            }
        }
        
        [DataSourceProperty]
        public string AvailabilityText
        {
            get => _availabilityText;
            set
            {
                if (_availabilityText != value)
                {
                    _availabilityText = value;
                    OnPropertyChangedWithValue(value, nameof(AvailabilityText));
                }
            }
        }
        
        [DataSourceProperty]
        public string ActivityCountText
        {
            get => _activityCountText;
            set
            {
                if (_activityCountText != value)
                {
                    _activityCountText = value;
                    OnPropertyChangedWithValue(value, nameof(ActivityCountText));
                }
            }
        }
        
        #endregion
        
        #region Commands
        
        /// <summary>
        /// Called when the location button is clicked.
        /// Invokes the callback to open the area detail screen for this location.
        /// </summary>
        public void ExecuteSelect()
        {
            _onSelected?.Invoke(_locationId);
        }
        
        #endregion
    }
}

