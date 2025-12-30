using System;
using System.Collections.Generic;
using Enlisted.Features.Company;
using Enlisted.Features.Content.Models;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Features.Escalation;
using Enlisted.Mod.Core;
using TaleWorlds.CampaignSystem;

namespace Enlisted.Features.Content
{
    /// <summary>
    /// Calculates simulation pressure from company and player state.
    /// High pressure indicates stressful situations that may warrant more frequent events.
    /// </summary>
    public static class SimulationPressureCalculator
    {
        /// <summary>
        /// Calculates the current simulation pressure based on company needs,
        /// escalation state, player health, and location.
        /// </summary>
        /// <returns>SimulationPressure with value (0-100) and source descriptions.</returns>
        public static SimulationPressure CalculatePressure()
        {
            float pressure = 0;
            var sources = new List<string>();

            // Check company needs
            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment?.CompanyNeeds != null)
            {
                var needs = enlistment.CompanyNeeds;

                // Low supplies adds pressure
                var supplies = needs.GetNeedValue(CompanyNeed.Supplies);
                if (supplies < 30)
                {
                    pressure += 20;
                    sources.Add("Low Supplies");
                }

                // Low rest/exhaustion adds pressure
                var rest = needs.GetNeedValue(CompanyNeed.Rest);
                if (rest < 30)
                {
                    pressure += 15;
                    sources.Add("Exhausted Company");
                }

                // Low morale adds pressure
                var morale = needs.GetNeedValue(CompanyNeed.Morale);
                if (morale < 30)
                {
                    pressure += 15;
                    sources.Add("Low Morale");
                }
            }

            // Check escalation state
            var escalation = EscalationManager.Instance?.State;
            if (escalation != null)
            {
                // High discipline (strict enforcement) adds pressure
                if (escalation.Discipline > 7)
                {
                    pressure += 25;
                    sources.Add("High Discipline");
                }

                // High scrutiny (being watched) adds pressure
                if (escalation.Scrutiny > 7)
                {
                    pressure += 20;
                    sources.Add("Under Scrutiny");
                }

                // High medical risk adds pressure
                if (escalation.MedicalRisk > 3)
                {
                    pressure += 15;
                    sources.Add("Medical Risk");
                }
            }

            // Check player health
            var hero = CampaignSafetyGuard.SafeMainHero;
            if (hero != null && hero.HitPoints < hero.MaxHitPoints * 0.5f)
            {
                pressure += 15;
                sources.Add("Wounded");
            }

            // Check if in enemy territory
            var situation = WorldStateAnalyzer.AnalyzeSituation();
            if (situation.InEnemyTerritory)
            {
                pressure += 15;
                sources.Add("Enemy Territory");
            }

            return new SimulationPressure
            {
                Value = Math.Min(100, pressure),
                Sources = sources
            };
        }
    }
}
