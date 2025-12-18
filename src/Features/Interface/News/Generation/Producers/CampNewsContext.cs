using Enlisted.Features.Camp;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Features.Interface.Behaviors;
using Enlisted.Features.Interface.News.State;
using Enlisted.Features.Schedule.Behaviors;
using Enlisted.Mod.Core.Triggers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;

namespace Enlisted.Features.Interface.News.Generation.Producers
{
    /// <summary>
    /// Live, non-persisted context for Daily Report fact producers.
    /// This can reference engine objects and behaviors; it is not stored in saves.
    /// </summary>
    public sealed class CampNewsContext
    {
        public int DayNumber { get; set; }

        // Live engine objects / behaviors (NOT persisted).
        public EnlistmentBehavior Enlistment { get; set; }
        public Hero Lord { get; set; }
        public MobileParty LordParty { get; set; }

        public CampaignTriggerTrackerBehavior TriggerTracker { get; set; }
        public CampLifeBehavior CampLife { get; set; }
        public ScheduleBehavior Schedule { get; set; }

        // Camp News state + owning behavior.
        public CampNewsState NewsState { get; set; }
        public EnlistedNewsBehavior NewsBehavior { get; set; }

        // Output for templating.
        public DailyReportGenerationContext Generation { get; set; }
    }
}


