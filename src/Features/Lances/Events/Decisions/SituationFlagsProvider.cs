using Enlisted.Features.Conditions;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Features.Interface.Behaviors;
using Enlisted.Features.Interface.News.Models;
using Enlisted.Mod.Core.Triggers;

namespace Enlisted.Features.Lances.Events.Decisions
{
    /// <summary>
    /// Computes situation flags from the Phase 4 Daily Report snapshot (preferred) plus a few “right now” live checks.
    /// The output should remain small and stable to keep content authoring predictable.
    /// </summary>
    public static class SituationFlagsProvider
    {
        public static SituationFlags Compute(EnlistmentBehavior enlistment)
        {
            var flags = new SituationFlags();

            try
            {
                var news = EnlistedNewsBehavior.Instance;
                var snapshot = news?.GetTodayDailyReportSnapshot();

                if (snapshot != null)
                {
                    flags.CompanyFoodCritical = snapshot.Food == FoodBand.Critical;
                    flags.CompanyThreatHigh = snapshot.Threat == ThreatBand.High;

                    // “Short-handed” is intentionally coarse: this is about pressure, not perfect accounting.
                    // We treat “lost men without replacements” or large wounded spikes as short-handed signals.
                    var dead = snapshot.DeadDelta;
                    var wounded = snapshot.WoundedDelta;
                    var replacements = snapshot.ReplacementsDelta;
                    flags.LanceShortHanded =
                        (dead >= 1 && replacements <= 0) ||
                        dead >= 2 ||
                        wounded >= 5;

                    // Fever spike (placeholder until troop sickness modelling exists).
                    flags.LanceFeverSpike = snapshot.SickDelta >= 2;
                }

                // Live “right now” signals (can change within a day).
                // Battle imminent is a deliberate approximation: “moving + enemy nearby” (see token provider).
                var eval = new LanceLifeEventTriggerEvaluator();
                flags.BattleImminent = eval.IsConditionTrue(CampaignTriggerTokens.BeforeBattle, enlistment);

                // Fever spike fallback (player illness as a proxy until troop sickness is modelled).
                if (!flags.LanceFeverSpike)
                {
                    flags.LanceFeverSpike = PlayerConditionBehavior.Instance?.IsEnabled() == true &&
                                            PlayerConditionBehavior.Instance.State?.HasIllness == true;
                }
            }
            catch
            {
                // Best-effort only. If we cannot compute flags, decisions still rely on their normal tokens/requirements.
            }

            return flags;
        }
    }
}


