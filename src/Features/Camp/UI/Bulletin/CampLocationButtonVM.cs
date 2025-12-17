using System;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace Enlisted.Features.Camp.UI.Bulletin
{
    /// <summary>
    /// ViewModel for a location button in the top row (like shop icons in settlement management).
    /// Phase 3: Uses proper sprite icons instead of text labels.
    /// </summary>
    public class CampLocationButtonVM : ViewModel
    {
        private readonly Action<string> _onSelect;
        private string _locationId;
        private string _locationName;
        private string _iconText;  // Fallback text for sprites
        private string _iconSprite;  // Sprite path for visual icon
        private string _hintText;    // Tooltip/hint text
        private int _activityCount;
        private string _activityCountText;
        private bool _isSelected;

        public CampLocationButtonVM(string locationId, string locationName, string iconText, Action<string> onSelect)
        {
            _onSelect = onSelect;
            LocationId = locationId;
            LocationName = locationName;
            IconText = iconText;  // Keep as fallback
            IconSprite = GetLocationIconSprite(locationId);
            HintText = GetLocationHint(locationId);
            ActivityCount = 0;
            IsSelected = false;
            RefreshValues();
        }

        public override void RefreshValues()
        {
            base.RefreshValues();
            ActivityCountText = ActivityCount > 0 ? $"({ActivityCount})" : string.Empty;
        }

        /// <summary>
        /// Update the activity count for this location.
        /// </summary>
        public void UpdateActivityCount(int count)
        {
            ActivityCount = count;
            RefreshValues();
        }

        /// <summary>
        /// Get appropriate sprite icon for each location.
        /// Uses native TownManagement and Encyclopedia sprites that are already loaded.
        /// </summary>
        private string GetLocationIconSprite(string locationId)
        {
            return locationId switch
            {
                // Lance - use garrison/troops icon
                "lance" => "SPGeneral\\TownManagement\\garrison_icon",
                // Reports - use consumption/notification icon
                "reports" => "SPGeneral\\TownManagement\\Consumption_Icon",
                // Medical Tent - use grain/food icon (represents supplies/care)
                "medical_tent" => "SPGeneral\\TownManagement\\VillageIcons\\grain",
                // Training Grounds - use production/building icon (represents work/training)
                "training_grounds" => "SPGeneral\\TownManagement\\production_icon",
                // Lord's Tent - use hammer/command icon (represents leadership/authority)
                "lords_tent" => "SPGeneral\\TownManagement\\project_popup_hammer_icon",
                // Quartermaster - use loyalty icon (represents management/supplies)
                "quartermaster" => "SPGeneral\\TownManagement\\loyalty_icon",
                // Personal Quarters - use grape/rest icon (represents comfort/personal space)
                "personal_quarters" => "SPGeneral\\TownManagement\\VillageIcons\\grape",
                // Camp Fire - use hardwood icon (represents gathering/warmth)
                "camp_fire" => "SPGeneral\\TownManagement\\VillageIcons\\hard_wood",
                _ => "SPGeneral\\TownManagement\\production_icon"
            };
        }

        /// <summary>
        /// Get hint text (tooltip) for each location.
        /// Provides context when player hovers over the button.
        /// </summary>
        private string GetLocationHint(string locationId)
        {
            return locationId switch
            {
                "lance" => "View your lance roster, current duties, and lance readiness",
                "reports" => "View camp news, daily schedule, and bulletin board",
                "medical_tent" => "Visit the Medical Tent to recover from injuries and restore health",
                "training_grounds" => "Visit the Training Grounds to improve skills and practice combat",
                "lords_tent" => "Visit the Lord's Tent for command duties and strategic planning",
                "quartermaster" => "Visit the Quartermaster to manage equipment and supplies",
                "personal_quarters" => "Visit your Personal Quarters to rest and manage personal affairs",
                "camp_fire" => "Visit the Camp Fire to socialize and boost morale",
                _ => "Visit this camp location"
            };
        }

        /// <summary>
        /// Command: select this location (invokes parent VM callback).
        /// </summary>
        public void ExecuteSelect()
        {
            _onSelect?.Invoke(LocationId);
        }

        [DataSourceProperty]
        public string LocationId
        {
            get => _locationId;
            set
            {
                if (value == _locationId) return;
                _locationId = value;
                OnPropertyChangedWithValue(value, nameof(LocationId));
            }
        }

        [DataSourceProperty]
        public string LocationName
        {
            get => _locationName;
            set
            {
                if (value == _locationName) return;
                _locationName = value;
                OnPropertyChangedWithValue(value, nameof(LocationName));
            }
        }

        [DataSourceProperty]
        public string IconText
        {
            get => _iconText;
            set
            {
                if (value == _iconText) return;
                _iconText = value;
                OnPropertyChangedWithValue(value, nameof(IconText));
            }
        }

        [DataSourceProperty]
        public int ActivityCount
        {
            get => _activityCount;
            set
            {
                if (value == _activityCount) return;
                _activityCount = value;
                OnPropertyChangedWithValue(value, nameof(ActivityCount));
            }
        }

        [DataSourceProperty]
        public string ActivityCountText
        {
            get => _activityCountText;
            set
            {
                if (value == _activityCountText) return;
                _activityCountText = value;
                OnPropertyChangedWithValue(value, nameof(ActivityCountText));
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
        public string HintText
        {
            get => _hintText;
            set
            {
                if (value == _hintText) return;
                _hintText = value;
                OnPropertyChangedWithValue(value, nameof(HintText));
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

