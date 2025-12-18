using System.Linq;
using Enlisted.Features.Interface.Behaviors;
using Enlisted.Features.Interface.News.Models;
using TaleWorlds.CampaignSystem;

namespace Enlisted.Features.Interface.News.Generation.Producers
{
    /// <summary>
    /// Kingdom facts: reuse the existing dispatch feed headlines as context for today’s report.
    /// </summary>
    public sealed class KingdomHeadlineFactProducer : IDailyReportFactProducer
    {
        public void Contribute(DailyReportSnapshot snapshot, CampNewsContext context)
        {
            if (snapshot == null || context?.Generation == null)
            {
                return;
            }

            try
            {
                var news = context.NewsBehavior;
                if (news != null)
                {
                    var latest = news.GetVisibleKingdomFeedItems(1).FirstOrDefault();
                    if (latest.StoryKey != null || latest.DayCreated != 0 || latest.MinDisplayDays != 0)
                    {
                        var headline = EnlistedNewsBehavior.FormatDispatchForDisplay(latest);
                        if (!string.IsNullOrWhiteSpace(headline))
                        {
                            context.Generation.KingdomHeadline = headline.Trim();
                            return;
                        }
                    }
                }

                // Fallback: simple kingdom presence line.
                var kingdom = context.Lord?.MapFaction as Kingdom;
                if (kingdom?.Name != null)
                {
                    context.Generation.KingdomHeadline = $"The banners of {kingdom.Name} remain in the field.";
                }
            }
            catch
            {
                // Best-effort only.
            }
        }
    }
}


