using System.Linq;
using Enlisted.Features.Schedule.Behaviors;
using Enlisted.Features.Schedule.Models;
using Enlisted.Mod.Core.Logging;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Localization;

namespace Enlisted.Features.Camp.UI.Bulletin
{
    /// <summary>
    /// Integration layer between Camp Bulletin UI and game systems.
    /// Track A Phase 3: Automatically generates news from schedule events.
    /// </summary>
    public static class CampBulletinIntegration
    {
        private const string LogCategory = "CampBulletinIntegration";
        private static CampBulletinVM _activeBulletin;
        private static int _lastCycleDay = -1;
        private static LordObjective _lastLordObjective = LordObjective.Unknown;

        /// <summary>
        /// Register the active bulletin VM (called when bulletin screen opens).
        /// </summary>
        public static void RegisterActiveBulletin(CampBulletinVM bulletin)
        {
            _activeBulletin = bulletin;
            Enlisted.Mod.Core.Logging.ModLogger.Info("CampBulletinIntegration", "Active bulletin registered, generating initial news...");

            // Generate initial news on registration
            RefreshNewsFromState();
        }

        /// <summary>
        /// Unregister bulletin (called when bulletin screen closes).
        /// </summary>
        public static void UnregisterActiveBulletin()
        {
            _activeBulletin = null;
        }

        /// <summary>
        /// Manually trigger news refresh (called when switching to Reports view).
        /// </summary>
        public static void RefreshNewsForActiveBulletin()
        {
            if (_activeBulletin != null)
            {
                RefreshNewsFromState();
            }
        }

        /// <summary>
        /// Post news item to active bulletin (if open).
        /// Routes to appropriate section based on category.
        /// </summary>
        public static void PostNews(CampBulletinNewsItemVM newsItem)
        {
            if (_activeBulletin == null || newsItem == null)
            {
                if (newsItem == null)
                {
                    Enlisted.Mod.Core.Logging.ModLogger.Warn("CampBulletinIntegration", "Cannot post news - newsItem is null");
                }
                return;
            }

            Enlisted.Mod.Core.Logging.ModLogger.Info("CampBulletinIntegration", $"Posting news: {newsItem.Title} (Category: {newsItem.Category})");

            // Route news to appropriate section based on category
            switch (newsItem.Category?.ToLowerInvariant())
            {
                case "lance":
                case "schedule":
                case "duty":
                case "performance":
                case "command":
                case "activity":
                case "alert":
                    _activeBulletin.LanceNews.Add(newsItem);
                    break;

                case "battle":
                case "siege":
                case "prisoner":
                case "war":
                case "kingdom":
                case "settlement":
                case "combat":
                    _activeBulletin.KingdomDispatches.Add(newsItem);
                    break;

                default:
                    // General news goes to kingdom dispatches
                    _activeBulletin.KingdomDispatches.Add(newsItem);
                    break;
            }

            // Also post into the main Reports feed (center panel) so players can read details there.
            // The Dispatch Board is meant to be a headline strip; the feed is the detailed board.
            _activeBulletin.PostNews(newsItem);
        }

        /// <summary>
        /// Called when new schedule is generated.
        /// </summary>
        public static void OnScheduleGenerated(DailySchedule schedule)
        {
            if (schedule == null)
                return;

            // Post schedule news
            var newsItem = CampNewsGenerator.GenerateScheduleNews(schedule);
            PostNews(newsItem);

            // Check for cycle day change
            if (_lastCycleDay > 0 && schedule.CycleDay != _lastCycleDay)
            {
                // New day started
                var welcomeNews = CampNewsGenerator.GenerateDailyWelcomeNews(schedule.CycleDay);
                PostNews(welcomeNews);

                // Check for cycle completion (day 1 after day 12)
                if (_lastCycleDay == 12 && schedule.CycleDay == 1)
                {
                    var cycleCompleteNews = CampNewsGenerator.GenerateCycleCompleteNews();
                    PostNews(cycleCompleteNews);
                }
            }

            _lastCycleDay = schedule.CycleDay;
        }

        /// <summary>
        /// Called when schedule block starts.
        /// </summary>
        public static void OnBlockStart(ScheduledBlock block)
        {
            if (block == null)
                return;

            var newsItem = CampNewsGenerator.GenerateBlockStartNews(block);
            PostNews(newsItem);
        }

        /// <summary>
        /// Called when schedule block completes.
        /// </summary>
        public static void OnBlockComplete(ScheduledBlock block)
        {
            if (block == null)
                return;

            var newsItem = CampNewsGenerator.GenerateBlockCompleteNews(block);
            PostNews(newsItem);
        }

        /// <summary>
        /// Called when critical lance needs are detected.
        /// </summary>
        public static void OnCriticalNeedsDetected(LanceNeedsState needs)
        {
            if (needs == null)
                return;

            var warningNews = CampNewsGenerator.GenerateLanceNeedsWarnings(needs);
            foreach (var newsItem in warningNews)
            {
                PostNews(newsItem);
            }
        }

        /// <summary>
        /// Called when lord objective changes.
        /// </summary>
        public static void OnLordObjectiveChanged(LordObjective objective, string description)
        {
            // Only post news if objective actually changed
            if (objective != _lastLordObjective && objective != LordObjective.Unknown)
            {
                var newsItem = CampNewsGenerator.GenerateLordObjectiveNews(objective, description);
                PostNews(newsItem);
                _lastLordObjective = objective;
            }
        }

        /// <summary>
        /// Called when player gets promoted.
        /// </summary>
        public static void OnTierPromotion(int newTier)
        {
            // Only post for significant promotions (T5, T6+)
            if (newTier == 5 || newTier == 6)
            {
                var newsItem = CampNewsGenerator.GeneratePromotionNews(newTier);
                PostNews(newsItem);
            }
        }

        /// <summary>
        /// Called at Pay Muster for performance feedback.
        /// </summary>
        public static void OnPerformanceReview(int score, string rating)
        {
            var newsItem = CampNewsGenerator.GeneratePerformanceFeedbackNews(score, rating);
            PostNews(newsItem);
        }

        /// <summary>
        /// Refresh news from current game state (called on bulletin open).
        /// Pulls from EnlistedNewsBehavior event feeds + adds current status.
        /// </summary>
        private static void RefreshNewsFromState()
        {
            if (_activeBulletin == null)
                return;

            ModLogger.Info(LogCategory, "RefreshNewsFromState - clearing old news and loading from event feeds");
            
            // Clear old news to prevent spam
            ClearAllNews();
            
            var enlistment = Enlistment.Behaviors.EnlistmentBehavior.Instance;
            if (enlistment == null || !enlistment.IsEnlisted)
            {
                // Not enlisted - post basic welcome
                PostNews(CampNewsGenerator.GenerateWelcomeNews());
                PostNews(CampNewsGenerator.GenerateCampStatusNews());
                return;
            }

            // Post current status news
            PostNews(CampNewsGenerator.GenerateEnlistmentStatusNews(enlistment));
            
            // Pull from EnlistedNewsBehavior event-driven feeds
            var newsBehavior = Interface.Behaviors.EnlistedNewsBehavior.Instance;
            if (newsBehavior != null)
            {
                // Post recent kingdom events (battles, sieges, prisoners, etc.)
                var visibleKingdomFeed = newsBehavior.GetVisibleKingdomFeedItems(5);
                if (visibleKingdomFeed != null && visibleKingdomFeed.Count > 0)
                {
                    foreach (var dispatch in visibleKingdomFeed)
                    {
                        PostNews(ConvertDispatchToNews(dispatch));
                    }
                }

                // Post personal/army events
                var visiblePersonalFeed = newsBehavior.GetVisiblePersonalFeedItems(3);
                if (visiblePersonalFeed != null && visiblePersonalFeed.Count > 0)
                {
                    foreach (var dispatch in visiblePersonalFeed)
                    {
                        PostNews(ConvertDispatchToNews(dispatch));
                    }
                }
            }

            // Add current lord status
            var commandingLord = enlistment.EnlistedLord;
            if (commandingLord != null)
            {
                PostNews(CampNewsGenerator.GenerateLordStatusNews(commandingLord));
            }

            // Add current kingdom status
            var hero = TaleWorlds.CampaignSystem.Hero.MainHero;
            var kingdom = hero?.MapFaction as TaleWorlds.CampaignSystem.Kingdom;
            if (kingdom != null)
            {
                PostNews(CampNewsGenerator.GenerateKingdomStatusNews(kingdom));
            }

            // Post general camp status
            PostNews(CampNewsGenerator.GenerateCampStatusNews());
        }

        /// <summary>
        /// Convert a DispatchItem from EnlistedNewsBehavior to CampBulletinNewsItemVM.
        /// </summary>
        private static CampBulletinNewsItemVM ConvertDispatchToNews(Interface.Behaviors.DispatchItem dispatch)
        {
            // Use the same formatting rules as the original enlisted menus, so we don't show raw keys like "News_Inconclusive".
            var headline = Interface.Behaviors.EnlistedNewsBehavior.FormatDispatchForDisplay(dispatch);
            if (string.IsNullOrWhiteSpace(headline))
            {
                headline = dispatch.HeadlineKey ?? "Dispatch";
            }

            // Use story key as ID
            string newsId = dispatch.StoryKey ?? $"dispatch_{dispatch.DayCreated}";
            
            // Calculate priority based on minimum display days (higher = more important)
            int priority = dispatch.MinDisplayDays >= 2 ? 3 : 2;

            return new CampBulletinNewsItemVM(
                newsId,
                headline,
                headline, // Use headline as description for now
                CampaignTime.Days(dispatch.DayCreated),
                dispatch.Category,
                priority
            );
        }

        /// <summary>
        /// Clear all news feeds.
        /// </summary>
        private static void ClearAllNews()
        {
            if (_activeBulletin == null)
                return;

            _activeBulletin.LanceNews.Clear();
            _activeBulletin.KingdomDispatches.Clear();
            _activeBulletin.BulletinFeed?.ClearAllNews();
        }
    }
}

