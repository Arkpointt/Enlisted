using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace Enlisted.Features.Camp.UI.Bulletin
{
    /// <summary>
    /// ViewModel for an individual news item in the bulletin board feed.
    /// Phase 3: Enhanced with category classification and priority system.
    /// </summary>
    public class CampBulletinNewsItemVM : ViewModel
    {
        private string _newsId;
        private string _title;
        private string _description;
        private string _timestampText;
        private string _iconPath;
        private string _category;  // Phase 3: News category (Activity, Command, Alert, etc.)
        private string _categoryColor;  // Phase 3: Color badge for category
        private int _priority;  // Phase 3: 0=low, 1=normal, 2=high, 3=critical
        private bool _hasDetails;
        private CampaignTime _timestamp;

        public CampBulletinNewsItemVM(string newsId, string title, string description, CampaignTime timestamp, 
            string category = null, int priority = 1)
        {
            _newsId = newsId;
            _title = title;
            _description = description;
            _timestamp = timestamp;
            _priority = priority;
            _category = category ?? DetermineCategory(newsId);
            _categoryColor = GetCategoryColor(_category);
            _hasDetails = false; // Can expand later for clickable news
            RefreshValues();
        }

        public override void RefreshValues()
        {
            base.RefreshValues();
            TimestampText = GetTimestampText();
            IconPath = GetIconPathForNewsType(_newsId);
        }

        /// <summary>
        /// Get human-readable timestamp (e.g., "2 hours ago").
        /// </summary>
        private string GetTimestampText()
        {
            var hoursAgo = (CampaignTime.Now.ToHours - _timestamp.ToHours);

            if (hoursAgo < 1)
                return "Just now";
            else if (hoursAgo < 2)
                return "1 hour ago";
            else if (hoursAgo < 24)
                return $"{(int)hoursAgo} hours ago";
            else if (hoursAgo < 48)
                return "1 day ago";
            else
                return $"{(int)(hoursAgo / 24)} days ago";
        }

        /// <summary>
        /// Determine category from news ID (Phase 3 enhancement).
        /// </summary>
        private string DetermineCategory(string newsId)
        {
            if (newsId.Contains("activity"))
                return "Activity";
            else if (newsId.Contains("duty") || newsId.Contains("assignment") || newsId.Contains("command"))
                return "Command";
            else if (newsId.Contains("alert") || newsId.Contains("warning") || newsId.Contains("urgent"))
                return "Alert";
            else if (newsId.Contains("lance") || newsId.Contains("party"))
                return "Lance";
            else if (newsId.Contains("equipment") || newsId.Contains("gear") || newsId.Contains("supply"))
                return "Equipment";
            else if (newsId.Contains("battle") || newsId.Contains("combat") || newsId.Contains("fight"))
                return "Combat";
            else if (newsId.Contains("social") || newsId.Contains("morale") || newsId.Contains("campfire"))
                return "Social";
            else
                return "General";
        }

        /// <summary>
        /// Get color for category badge (Phase 3 enhancement).
        /// </summary>
        private string GetCategoryColor(string category)
        {
            return category switch
            {
                "Report" => "#607D8BFF",        // Blue-gray - daily report / briefing
                "Activity" => "#4CAF50FF",      // Green - completed activities
                "Command" => "#FFB74DFF",       // Orange - orders/duties
                "Alert" => "#F44336FF",         // Red - important alerts
                "Lance" => "#2196F3FF",         // Blue - lance/party info
                "Equipment" => "#9C27B0FF",     // Purple - gear/supplies
                "Combat" => "#D32F2FFF",        // Dark red - combat reports
                "Social" => "#FFC107FF",        // Yellow - social/morale
                _ => "#9E9E9EFF"                // Gray - general/misc
            };
        }

        /// <summary>
        /// Get icon path based on news type.
        /// </summary>
        private string GetIconPathForNewsType(string newsId)
        {
            // Simple mapping for now - can expand with more specific icons
            if (newsId.Contains("battle") || newsId.Contains("combat"))
                return "GeneralStandard\\icon_strength";
            else if (newsId.Contains("medical") || newsId.Contains("health"))
                return "GeneralStandard\\icon_health";
            else if (newsId.Contains("duty") || newsId.Contains("assignment"))
                return "GeneralStandard\\icon_leadership";
            else if (newsId.Contains("event") || newsId.Contains("social"))
                return "GeneralStandard\\icon_social";
            else if (newsId.Contains("activity"))
                return "GeneralStandard\\icon_morale";
            else
                return "GeneralStandard\\icon_generic";
        }

        /// <summary>
        /// Command: Show details for this news item (if applicable).
        /// </summary>
        public void ExecuteShowDetails()
        {
            // TODO: Expand for clickable news items that show more detail
            InformationManager.DisplayMessage(new InformationMessage($"News details: {Title}"));
        }

        [DataSourceProperty]
        public CampaignTime Timestamp => _timestamp;

        [DataSourceProperty]
        public string NewsId
        {
            get => _newsId;
            set
            {
                if (value == _newsId) return;
                _newsId = value;
                OnPropertyChangedWithValue(value, nameof(NewsId));
            }
        }

        [DataSourceProperty]
        public string Title
        {
            get => _title;
            set
            {
                if (value == _title) return;
                _title = value;
                OnPropertyChangedWithValue(value, nameof(Title));
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
        public string TimestampText
        {
            get => _timestampText;
            set
            {
                if (value == _timestampText) return;
                _timestampText = value;
                OnPropertyChangedWithValue(value, nameof(TimestampText));
            }
        }

        [DataSourceProperty]
        public string IconPath
        {
            get => _iconPath;
            set
            {
                if (value == _iconPath) return;
                _iconPath = value;
                OnPropertyChangedWithValue(value, nameof(IconPath));
            }
        }

        [DataSourceProperty]
        public string Category
        {
            get => _category;
            set
            {
                if (value == _category) return;
                _category = value;
                OnPropertyChangedWithValue(value, nameof(Category));
            }
        }

        [DataSourceProperty]
        public string CategoryColor
        {
            get => _categoryColor;
            set
            {
                if (value == _categoryColor) return;
                _categoryColor = value;
                OnPropertyChangedWithValue(value, nameof(CategoryColor));
            }
        }

        [DataSourceProperty]
        public int Priority
        {
            get => _priority;
            set
            {
                if (value == _priority) return;
                _priority = value;
                OnPropertyChangedWithValue(value, nameof(Priority));
            }
        }

        [DataSourceProperty]
        public bool HasDetails
        {
            get => _hasDetails;
            set
            {
                if (value == _hasDetails) return;
                _hasDetails = value;
                OnPropertyChangedWithValue(value, nameof(HasDetails));
            }
        }
    }
}

