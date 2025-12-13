using System;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Features.Lances.Personas;
using Enlisted.Features.Ranks;
using Enlisted.Mod.Core.Triggers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace Enlisted.Features.Lances.Text
{
    internal static class LanceLifeTextVariables
    {
        /// <summary>
        /// Apply common Lance Life text variables used by both story packs and data-driven events.
        /// Keep this best-effort: placeholder resolution should never crash inquiry rendering.
        /// </summary>
        public static void ApplyCommon(TextObject text, EnlistmentBehavior enlistment)
        {
            if (text == null)
            {
                return;
            }

            text.SetTextVariable("PLAYER_NAME", Hero.MainHero?.Name ?? new TextObject(string.Empty));
            text.SetTextVariable("LORD_NAME", enlistment?.EnlistedLord?.Name ?? new TextObject("{=enlist_fallback_army}the army"));
            text.SetTextVariable("LANCE_NAME",
                !string.IsNullOrWhiteSpace(enlistment?.CurrentLanceName) ? new TextObject(enlistment.CurrentLanceName) : new TextObject(string.Empty));

            var factionName = enlistment?.EnlistedLord?.MapFaction?.Name ?? new TextObject("{=Enlisted_Term_ThisFaction}this faction");
            text.SetTextVariable("FACTION_NAME", factionName);

            var kingdomName = (enlistment?.EnlistedLord?.MapFaction as Kingdom)?.Name ?? new TextObject("{=Enlisted_Term_YourKingdom}your kingdom");
            text.SetTextVariable("KINGDOM_NAME", kingdomName);

            // Culture-specific player rank placeholders
            ApplyRankVariables(text, enlistment);

            var lastSettlementName = new TextObject("{=ll_term_camp}camp");
            try
            {
                var tracker = CampaignTriggerTrackerBehavior.Instance;
                var lastId = tracker?.LastSettlementEnteredId;
                if (!string.IsNullOrWhiteSpace(lastId))
                {
                    var settlement = Settlement.Find(lastId);
                    if (settlement?.Name != null)
                    {
                        lastSettlementName = settlement.Name;
                    }
                }
            }
            catch
            {
                // Best effort; do not fail story text rendering due to placeholder resolution.
            }

            text.SetTextVariable("LAST_SETTLEMENT", lastSettlementName);

            // Phase 5: Named lance role personas (text-only roster). Safe fallback when disabled/unavailable.
            try
            {
                var personas = LancePersonaBehavior.Instance;
                if (personas?.IsEnabled() == true)
                {
                    var roster = personas.GetRosterFor(enlistment?.CurrentLord, enlistment?.CurrentLanceId);
                    if (roster?.Members != null && roster.Members.Count > 0)
                    {
                        var leader = roster.Members.Find(m => m != null && m.Position == LancePosition.Leader && m.IsAlive);
                        var second = roster.Members.Find(m => m != null && m.Position == LancePosition.Second && m.IsAlive);

                        var vet1 = roster.Members.Find(m => m != null && (m.Position == LancePosition.SeniorVeteran) && m.IsAlive);
                        var vet2 = roster.Members.Find(m => m != null && (m.Position == LancePosition.Veteran) && m.IsAlive);

                        if (leader != null)
                        {
                            text.SetTextVariable("LANCE_LEADER_RANK", LancePersonaBehavior.BuildRankTitleText(leader.RankTitleId, leader.RankTitleFallback));
                            text.SetTextVariable("LANCE_LEADER_NAME", LancePersonaBehavior.BuildFullNameText(leader));
                            text.SetTextVariable("LANCE_LEADER_SHORT", LancePersonaBehavior.BuildShortNameText(leader));
                        }
                        if (second != null)
                        {
                            text.SetTextVariable("SECOND_RANK", LancePersonaBehavior.BuildRankTitleText(second.RankTitleId, second.RankTitleFallback));
                            text.SetTextVariable("SECOND_NAME", LancePersonaBehavior.BuildFullNameText(second));
                            text.SetTextVariable("SECOND_SHORT", LancePersonaBehavior.BuildShortNameText(second));
                        }
                        if (vet1 != null)
                        {
                            text.SetTextVariable("VETERAN_1_NAME", LancePersonaBehavior.BuildFullNameText(vet1));
                        }
                        if (vet2 != null)
                        {
                            text.SetTextVariable("VETERAN_2_NAME", LancePersonaBehavior.BuildFullNameText(vet2));
                        }

                        var living = roster.Members.FindAll(m => m != null && m.IsAlive);
                        if (living.Count > 0)
                        {
                            var randomMate = living[MBRandom.RandomInt(living.Count)];
                            text.SetTextVariable("LANCE_MATE_NAME", LancePersonaBehavior.BuildFullNameText(randomMate));
                        }

                        var soldiers = roster.Members.FindAll(m => m != null && m.IsAlive && m.Position == LancePosition.Soldier);
                        if (soldiers.Count > 0)
                        {
                            var soldier = soldiers[MBRandom.RandomInt(soldiers.Count)];
                            text.SetTextVariable("SOLDIER_NAME", LancePersonaBehavior.BuildFullNameText(soldier));
                        }

                        var recruits = roster.Members.FindAll(m => m != null && m.IsAlive && m.Position == LancePosition.Recruit);
                        if (recruits.Count > 0)
                        {
                            var recruit = recruits[MBRandom.RandomInt(recruits.Count)];
                            text.SetTextVariable("RECRUIT_NAME", LancePersonaBehavior.BuildFullNameText(recruit));
                        }
                    }
                }
            }
            catch
            {
                // Best effort only.
            }
        }

        /// <summary>
        /// Apply culture-specific rank placeholders for event text.
        /// </summary>
        private static void ApplyRankVariables(TextObject text, EnlistmentBehavior enlistment)
        {
            if (text == null)
            {
                return;
            }

            try
            {
                var cultureId = RankHelper.GetCultureId(enlistment);
                var tier = enlistment?.EnlistmentTier ?? 1;

                // Current and next rank
                text.SetTextVariable("PLAYER_RANK", RankHelper.GetRankTitle(tier, cultureId));
                text.SetTextVariable("NEXT_RANK", RankHelper.GetRankTitle(Math.Min(tier + 1, 9), cultureId));

                // All tier ranks for flexibility in event text
                text.SetTextVariable("T1_RANK", RankHelper.GetRankTitle(1, cultureId));
                text.SetTextVariable("T2_RANK", RankHelper.GetRankTitle(2, cultureId));
                text.SetTextVariable("T3_RANK", RankHelper.GetRankTitle(3, cultureId));
                text.SetTextVariable("T4_RANK", RankHelper.GetRankTitle(4, cultureId));
                text.SetTextVariable("T5_RANK", RankHelper.GetRankTitle(5, cultureId));
                text.SetTextVariable("T6_RANK", RankHelper.GetRankTitle(6, cultureId));
                text.SetTextVariable("T7_RANK", RankHelper.GetRankTitle(7, cultureId));
                text.SetTextVariable("T8_RANK", RankHelper.GetRankTitle(8, cultureId));
                text.SetTextVariable("T9_RANK", RankHelper.GetRankTitle(9, cultureId));

                // Commander/officer title for this culture
                text.SetTextVariable("COMMANDER_TITLE", RankHelper.GetCommanderTitle(cultureId));
            }
            catch
            {
                // Best effort - don't crash event rendering on placeholder resolution failure
            }
        }

        /// <summary>
        /// Phase 7: Get the lance leader's short name for duty request messages.
        /// Returns a sensible fallback if roster is not available.
        /// </summary>
        public static string GetLanceLeaderShortName(EnlistmentBehavior enlistment)
        {
            try
            {
                var personaBehavior = LancePersonaBehavior.Instance;
                if (personaBehavior != null && enlistment != null)
                {
                    var roster = personaBehavior.GetRosterFor(enlistment.EnlistedLord, enlistment.CurrentLanceId);
                    if (roster?.Members?.Count > 0)
                    {
                        var leader = roster.Members[0];
                        return LancePersonaBehavior.BuildShortNameText(leader)?.ToString() ?? "Sergeant";
                    }
                }
            }
            catch
            {
                // Fall through to default
            }

            return "The Sergeant";
        }
    }
}


