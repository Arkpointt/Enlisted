using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace Enlisted.Features.Camp.UI.Bulletin
{
    /// <summary>
    /// ViewModel for the bulletin board news feed (center panel default view).
    /// </summary>
    public class CampBulletinFeedVM : ViewModel
    {
        private string _bulletinHeaderText;
        private MBBindingList<CampBulletinNewsItemVM> _newsItems;
        private const int MaxNewsItems = 20;

        public CampBulletinFeedVM()
        {
            NewsItems = new MBBindingList<CampBulletinNewsItemVM>();
            RefreshValues();
            LoadInitialNews();
        }

        public override void RefreshValues()
        {
            base.RefreshValues();
            BulletinHeaderText = "═══ REPORTS ═══";
        }

        /// <summary>
        /// Load initial news items (examples for now).
        /// </summary>
        private void LoadInitialNews()
        {
            // Add some example news items
            AddNewsItem(new CampBulletinNewsItemVM(
                "welcome",
                "Welcome to Camp",
                "Check the bulletin board daily for important updates, duty assignments, and camp events.",
                CampaignTime.Now
            ));

            AddNewsItem(new CampBulletinNewsItemVM(
                "locations",
                "Camp Locations",
                "Click on location buttons above to visit different areas of the camp and perform activities.",
                CampaignTime.Now
            ));

            AddNewsItem(new CampBulletinNewsItemVM(
                "activities",
                "⚡ Activities",
                "Each location offers unique activities. Complete them to improve your skills, restore energy, and earn rewards.",
                CampaignTime.Now
            ));
        }

        /// <summary>
        /// Add a news item to the feed (newest at top).
        /// </summary>
        public void AddNewsItem(CampBulletinNewsItemVM newsItem)
        {
            // Insert at the top (newest first)
            NewsItems.Insert(0, newsItem);

            // Trim to max items
            while (NewsItems.Count > MaxNewsItems)
            {
                NewsItems.RemoveAt(NewsItems.Count - 1);
            }
        }

        /// <summary>
        /// Remove old news items (called when exiting camp or on new day).
        /// </summary>
        public void ClearOldNews()
        {
            var currentTime = CampaignTime.Now;
            var itemsToRemove = NewsItems
                .Where(item => currentTime.ToDays - item.Timestamp.ToDays > 3) // Older than 3 days
                .ToList();

            foreach (var item in itemsToRemove)
            {
                NewsItems.Remove(item);
            }
        }

        /// <summary>
        /// Clear all news items.
        /// </summary>
        public void ClearAllNews()
        {
            NewsItems.Clear();
        }

        [DataSourceProperty]
        public string BulletinHeaderText
        {
            get => _bulletinHeaderText;
            set
            {
                if (value == _bulletinHeaderText) return;
                _bulletinHeaderText = value;
                OnPropertyChangedWithValue(value, nameof(BulletinHeaderText));
            }
        }

        [DataSourceProperty]
        public MBBindingList<CampBulletinNewsItemVM> NewsItems
        {
            get => _newsItems;
            set
            {
                if (value == _newsItems) return;
                _newsItems = value;
                OnPropertyChangedWithValue(value, nameof(NewsItems));
            }
        }
    }
}

