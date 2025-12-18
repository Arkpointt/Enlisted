using Enlisted.Features.Interface.News.Models;

namespace Enlisted.Features.Interface.News.Generation.Producers
{
    /// <summary>
    /// Producer pattern for Daily Report facts.
    /// Each producer observes current campaign state and contributes small, save-safe facts
    /// into a <see cref="DailyReportSnapshot"/> and its associated generation context.
    /// </summary>
    public interface IDailyReportFactProducer
    {
        void Contribute(DailyReportSnapshot snapshot, CampNewsContext context);
    }
}


