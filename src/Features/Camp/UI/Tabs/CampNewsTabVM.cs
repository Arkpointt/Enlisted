using System;
using TaleWorlds.Library;

namespace Enlisted.Features.Camp.UI.Tabs
{
    /// <summary>
    /// ViewModel for the News/Alerts tab in the Camp screen.
    /// Shows notifications, cover requests, duty alerts, and recent activity results.
    /// </summary>
    public class CampNewsTabVM : ViewModel
    {
        private readonly Action _onRefresh;
        private bool _isSelected;
        private MBBindingList<CampNewsItemVM> _newsItems;
        private string _noNewsText;

        [DataSourceProperty]
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChangedWithValue(value, nameof(IsSelected));
                    if (value)
                    {
                        RefreshNews();
                    }
                }
            }
        }

        [DataSourceProperty]
        public MBBindingList<CampNewsItemVM> NewsItems
        {
            get => _newsItems;
            set
            {
                if (_newsItems != value)
                {
                    _newsItems = value;
                    OnPropertyChangedWithValue(value, nameof(NewsItems));
                }
            }
        }

        [DataSourceProperty]
        public bool HasNews => NewsItems.Count > 0;

        [DataSourceProperty]
        public string NoNewsText
        {
            get => _noNewsText;
            set
            {
                if (_noNewsText != value)
                {
                    _noNewsText = value;
                    OnPropertyChangedWithValue(value, nameof(NoNewsText));
                }
            }
        }

        public CampNewsTabVM(Action onRefresh)
        {
            _onRefresh = onRefresh;
            _newsItems = new MBBindingList<CampNewsItemVM>();
            _noNewsText = "All quiet in camp. No reports or alerts.";
        }

        public void RefreshNews()
        {
            NewsItems.Clear();

            // Add placeholder news items for now
            // These will be populated by Lance Life Simulation and AI Schedule in Phase 4
            AddWelcomeMessage();

            OnPropertyChangedWithValue(HasNews, nameof(HasNews));
        }

        private void AddWelcomeMessage()
        {
            NewsItems.Add(new CampNewsItemVM
            {
                Title = "Camp Reports",
                Description = "Select a location tab above to view available activities.",
                // Avoid emoji glyphs (render as ? on some Bannerlord fonts). Use plain text until we switch to sprite icons.
                Icon = "RPT",
                Priority = 0,
                Timestamp = "Now"
            });
        }

        public void AddNewsItem(string title, string description, string icon, int priority = 0)
        {
            NewsItems.Insert(0, new CampNewsItemVM
            {
                Title = title,
                Description = description,
                Icon = icon,
                Priority = priority,
                Timestamp = "Now"
            });

            // Keep only last 20 items
            while (NewsItems.Count > 20)
            {
                NewsItems.RemoveAt(NewsItems.Count - 1);
            }

            OnPropertyChangedWithValue(HasNews, nameof(HasNews));
        }

        public override void OnFinalize()
        {
            base.OnFinalize();
            NewsItems.Clear();
        }
    }

    /// <summary>
    /// Individual news/alert item in the News tab.
    /// </summary>
    public class CampNewsItemVM : ViewModel
    {
        private string _title;
        private string _description;
        private string _icon;
        private int _priority;
        private string _timestamp;

        [DataSourceProperty]
        public string Title
        {
            get => _title;
            set
            {
                if (_title != value)
                {
                    _title = value;
                    OnPropertyChangedWithValue(value, nameof(Title));
                }
            }
        }

        [DataSourceProperty]
        public string Description
        {
            get => _description;
            set
            {
                if (_description != value)
                {
                    _description = value;
                    OnPropertyChangedWithValue(value, nameof(Description));
                }
            }
        }

        [DataSourceProperty]
        public string Icon
        {
            get => _icon;
            set
            {
                if (_icon != value)
                {
                    _icon = value;
                    OnPropertyChangedWithValue(value, nameof(Icon));
                }
            }
        }

        [DataSourceProperty]
        public int Priority
        {
            get => _priority;
            set
            {
                if (_priority != value)
                {
                    _priority = value;
                    OnPropertyChangedWithValue(value, nameof(Priority));
                }
            }
        }

        [DataSourceProperty]
        public string Timestamp
        {
            get => _timestamp;
            set
            {
                if (_timestamp != value)
                {
                    _timestamp = value;
                    OnPropertyChangedWithValue(value, nameof(Timestamp));
                }
            }
        }

        [DataSourceProperty]
        public bool IsHighPriority => Priority >= 2;

        [DataSourceProperty]
        public bool IsMediumPriority => Priority == 1;
    }
}

