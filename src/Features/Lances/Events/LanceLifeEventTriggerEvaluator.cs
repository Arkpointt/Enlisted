using System;
using System.Collections.Generic;
using System.Linq;
using Enlisted.Features.Assignments.Behaviors;
using Enlisted.Features.Camp;
using Enlisted.Features.Conditions;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Features.Escalation;
using Enlisted.Mod.Core.Triggers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace Enlisted.Features.Lances.Events
{
    internal sealed class LanceLifeEventTriggerEvaluator
    {
        private const float RecentWindowDays = 1f;

        public bool AreTriggersSatisfied(LanceLifeEventDefinition evt, EnlistmentBehavior enlistment)
        {
            if (evt == null)
            {
                return false;
            }

            // Time-of-day is modeled as its own list in the event schema.
            if (!IsTimeOfDaySatisfied(evt.Triggers?.TimeOfDay))
            {
                return false;
            }

            // Phase 5a: escalation range requirements (min/max)
            if (!AreEscalationRangesSatisfied(evt.Triggers?.EscalationRequirements))
            {
                return false;
            }

            var all = evt.Triggers?.All ?? new List<string>();
            var any = evt.Triggers?.Any ?? new List<string>();

            foreach (var token in all)
            {
                if (!IsTokenTrue(token, enlistment))
                {
                    return false;
                }
            }

            if (any.Count > 0)
            {
                return any.Any(t => IsTokenTrue(t, enlistment));
            }

            return true;
        }

        public bool IsConditionTrue(string token, EnlistmentBehavior enlistment)
        {
            // Phase 5a: option.condition uses the same token grammar as triggers.
            return IsTokenTrue(token, enlistment);
        }

        private static bool AreEscalationRangesSatisfied(LanceLifeEventEscalationRequirements req)
        {
            if (req == null)
            {
                return true;
            }

            // If escalation is disabled, only allow events that don't specify constraints.
            var esc = EscalationManager.Instance;
            var enabled = esc?.IsEnabled() == true;

            var heatOk = IsRangeSatisfied(enabled, esc?.State.Heat ?? 0, req.Heat);
            var discOk = IsRangeSatisfied(enabled, esc?.State.Discipline ?? 0, req.Discipline);
            var repOk = IsRangeSatisfied(enabled, esc?.State.LanceReputation ?? 0, req.LanceReputation);
            var medOk = IsRangeSatisfied(enabled, esc?.State.MedicalRisk ?? 0, req.MedicalRisk);
            
            // Pay tension threshold check (Phase 3 Pay System)
            // Pay tension is always enabled since it's part of the core pay system, not escalation
            var payTensionOk = IsPayTensionSatisfied(req.PayTensionMin, req.PayTensionMax);

            return heatOk && discOk && repOk && medOk && payTensionOk;
        }
        
        private static bool IsPayTensionSatisfied(int? min, int? max)
        {
            if (!min.HasValue && !max.HasValue)
            {
                return true;
            }
            
            var enlistment = Enlistment.Behaviors.EnlistmentBehavior.Instance;
            if (enlistment == null)
            {
                return false;
            }
            
            var payTension = enlistment.PayTension;
            
            if (min.HasValue && payTension < min.Value)
            {
                return false;
            }
            
            if (max.HasValue && payTension > max.Value)
            {
                return false;
            }
            
            return true;
        }

        private static bool IsRangeSatisfied(bool escalationEnabled, int value, LanceLifeIntRange range)
        {
            if (range == null || (!range.Min.HasValue && !range.Max.HasValue))
            {
                return true;
            }

            if (!escalationEnabled)
            {
                return false;
            }

            if (range.Min.HasValue && value < range.Min.Value)
            {
                return false;
            }

            if (range.Max.HasValue && value > range.Max.Value)
            {
                return false;
            }

            return true;
        }

        private bool IsTimeOfDaySatisfied(List<string> timeTokens)
        {
            if (timeTokens == null || timeTokens.Count == 0)
            {
                return true;
            }

            if (timeTokens.Any(t => string.Equals(t?.Trim(), CampaignTriggerTokens.AnyTime, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            var tracker = CampaignTriggerTrackerBehavior.Instance;
            if (tracker == null)
            {
                return false;
            }

            var dayPart = tracker.GetDayPart();
            foreach (var t in timeTokens)
            {
                if (string.IsNullOrWhiteSpace(t))
                {
                    continue;
                }

                var token = t.Trim().ToLowerInvariant();
                
                // 6-period camp schedule - direct mappings
                if (token == CampaignTriggerTokens.Dawn && dayPart == DayPart.Dawn)
                {
                    return true;
                }
                if (token == CampaignTriggerTokens.Morning && dayPart == DayPart.Morning)
                {
                    return true;
                }
                if (token == CampaignTriggerTokens.Afternoon && dayPart == DayPart.Afternoon)
                {
                    return true;
                }
                if (token == CampaignTriggerTokens.Evening && dayPart == DayPart.Evening)
                {
                    return true;
                }
                if (token == CampaignTriggerTokens.Dusk && dayPart == DayPart.Dusk)
                {
                    return true;
                }
                if (token == CampaignTriggerTokens.Night && dayPart == DayPart.Night)
                {
                    return true;
                }
                
                // Legacy "day" token - map to all daytime periods for backwards compatibility
                if (token == CampaignTriggerTokens.Day && 
                    (dayPart == DayPart.Morning || dayPart == DayPart.Afternoon))
                {
                    return true;
                }
                
                // "late_night" - map to night period
                if (token == CampaignTriggerTokens.LateNight && dayPart == DayPart.Night)
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsTokenTrue(string token, EnlistmentBehavior enlistment)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return false;
            }

            var t = token.Trim();

            // Comparison-style tokens: e.g. "days_since_enlistment < 1"
            if (TryEvaluateDaysComparison(t, out var daysResult))
            {
                return daysResult;
            }

            if (TryEvaluateNumericComparison(t, out var numericResult))
            {
                return numericResult;
            }

            // Prefix tokens
            if (t.StartsWith(CampaignTriggerTokens.HasDutyPrefix, StringComparison.OrdinalIgnoreCase))
            {
                var dutyId = t.Substring(CampaignTriggerTokens.HasDutyPrefix.Length).Trim();
                if (string.IsNullOrWhiteSpace(dutyId))
                {
                    return false;
                }

                var duties = EnlistedDutiesBehavior.Instance;
                if (duties == null)
                {
                    return false;
                }

                // Use reflection to avoid hard coupling on the method symbol in environments where analyzers lag.
                // (The implementation exists and is validated by build/test.)
                try
                {
                    var m = duties.GetType().GetMethod("HasActiveDuty", new[] { typeof(string) });
                    if (m != null)
                    {
                        return m.Invoke(duties, new object[] { dutyId }) as bool? == true;
                    }
                }
                catch
                {
                    // Fall through to list-based check
                }

                return duties.ActiveDuties?.Any(d => string.Equals(d, dutyId, StringComparison.OrdinalIgnoreCase)) == true;
            }

            var tracker = CampaignTriggerTrackerBehavior.Instance;
            if (tracker == null)
            {
                return false;
            }

            switch (t.ToLowerInvariant())
            {
                case CampaignTriggerTokens.IsEnlisted:
                    return enlistment?.IsEnlisted == true;

                case CampaignTriggerTokens.AiSafe:
                    return IsAiSafe();

                // Coarse cadence tokens (used as hints in metadata tables).
                // These are intentionally lightweight; we rely on cooldowns + per-day limits to prevent spam.
                case CampaignTriggerTokens.DailyTick:
                case CampaignTriggerTokens.WeeklyTick:
                    return true;

                case CampaignTriggerTokens.RandomChance:
                case CampaignTriggerTokens.Chance:
                    return MBRandom.RandomFloat < 0.25f;

                case CampaignTriggerTokens.OnboardingStage1:
                    return LanceLifeOnboardingBehavior.Instance?.IsStageActive(1) == true;
                case CampaignTriggerTokens.OnboardingStage2:
                    return LanceLifeOnboardingBehavior.Instance?.IsStageActive(2) == true;
                case CampaignTriggerTokens.OnboardingStage3:
                    return LanceLifeOnboardingBehavior.Instance?.IsStageActive(3) == true;
                case CampaignTriggerTokens.OnboardingComplete:
                    return LanceLifeOnboardingBehavior.Instance?.IsCompleteToken == true;

                case CampaignTriggerTokens.InSettlement:
                    return MobileParty.MainParty?.CurrentSettlement != null;

                case CampaignTriggerTokens.NotInSettlement:
                    return MobileParty.MainParty?.CurrentSettlement == null;

                case CampaignTriggerTokens.ArmyMoving:
                    return MobileParty.MainParty?.IsMoving == true;

                case CampaignTriggerTokens.CampEstablished:
                    return MobileParty.MainParty?.CurrentSettlement == null && MobileParty.MainParty?.IsMoving != true;

                case CampaignTriggerTokens.WoundedInCamp:
                    return MobileParty.MainParty?.MemberRoster?.TotalWounded > 0;

                case CampaignTriggerTokens.AtSea:
                    return MobileParty.MainParty?.IsCurrentlyAtSea == true;

                case CampaignTriggerTokens.AfterBattle:
                    return tracker.IsWithinDays(tracker.LastMapEventEndedTime, 3f);

                case CampaignTriggerTokens.BeforeBattle:
                    // Best-effort: “we are moving and enemies are near” approximates imminent contact without relying on internal map AI state.
                    return MobileParty.MainParty?.IsMoving == true && IsEnemyNearbyFast();

                case CampaignTriggerTokens.EnemyNearby:
                    return IsEnemyNearbyFast();

                // Dayparts (6-period camp schedule)
                case CampaignTriggerTokens.Dawn:
                    return tracker.GetDayPart() == DayPart.Dawn;
                case CampaignTriggerTokens.Morning:
                    return tracker.GetDayPart() == DayPart.Morning;
                case CampaignTriggerTokens.Afternoon:
                    return tracker.GetDayPart() == DayPart.Afternoon;
                case CampaignTriggerTokens.Evening:
                    return tracker.GetDayPart() == DayPart.Evening;
                case CampaignTriggerTokens.Dusk:
                    return tracker.GetDayPart() == DayPart.Dusk;
                case CampaignTriggerTokens.Night:
                    return tracker.GetDayPart() == DayPart.Night;
                
                // Legacy "day" token - map to all daytime periods for backwards compatibility
                case CampaignTriggerTokens.Day:
                {
                    var part = tracker.GetDayPart();
                    return part == DayPart.Morning || part == DayPart.Afternoon;
                }

                // Settlement events (recent)
                case CampaignTriggerTokens.EnteredSettlement:
                    return tracker.IsWithinDays(tracker.LastSettlementEnteredTime, RecentWindowDays);
                case CampaignTriggerTokens.EnteredTown:
                    return tracker.IsWithinDays(tracker.LastTownEnteredTime, RecentWindowDays);
                case CampaignTriggerTokens.EnteredCastle:
                    return tracker.IsWithinDays(tracker.LastCastleEnteredTime, RecentWindowDays);
                case CampaignTriggerTokens.EnteredVillage:
                    return tracker.IsWithinDays(tracker.LastVillageEnteredTime, RecentWindowDays);
                case CampaignTriggerTokens.LeftSettlement:
                    return tracker.IsWithinDays(tracker.LastSettlementLeftTime, RecentWindowDays);

                // Battle ended recently (proxy)
                case CampaignTriggerTokens.LeavingBattle:
                    return tracker.IsWithinDays(tracker.LastMapEventEndedTime, RecentWindowDays);

                // Camp life snapshot thresholds
                case CampaignTriggerTokens.LogisticsHigh:
                    return CampLifeBehavior.Instance?.IsLogisticsHigh() == true;
                case CampaignTriggerTokens.MoraleLow:
                    return CampLifeBehavior.Instance?.IsMoraleLow() == true;
                case CampaignTriggerTokens.PayTensionHigh:
                    return CampLifeBehavior.Instance?.IsPayTensionHigh() == true;
                case CampaignTriggerTokens.HeatHigh:
                    return CampLifeBehavior.Instance?.IsHeatHigh() == true;

                // Escalation thresholds
                case CampaignTriggerTokens.Heat3:
                    return EscalationManager.Instance?.IsEnabled() == true && EscalationManager.Instance.State.Heat >= 3;
                case CampaignTriggerTokens.Heat5:
                    return EscalationManager.Instance?.IsEnabled() == true && EscalationManager.Instance.State.Heat >= 5;
                case CampaignTriggerTokens.Heat7:
                    return EscalationManager.Instance?.IsEnabled() == true && EscalationManager.Instance.State.Heat >= 7;
                case CampaignTriggerTokens.Heat10:
                    return EscalationManager.Instance?.IsEnabled() == true && EscalationManager.Instance.State.Heat >= 10;

                case CampaignTriggerTokens.Discipline3:
                    return EscalationManager.Instance?.IsEnabled() == true && EscalationManager.Instance.State.Discipline >= 3;
                case CampaignTriggerTokens.Discipline2:
                    return EscalationManager.Instance?.IsEnabled() == true && EscalationManager.Instance.State.Discipline >= 2;
                case CampaignTriggerTokens.Discipline5:
                    return EscalationManager.Instance?.IsEnabled() == true && EscalationManager.Instance.State.Discipline >= 5;
                case CampaignTriggerTokens.Discipline7:
                    return EscalationManager.Instance?.IsEnabled() == true && EscalationManager.Instance.State.Discipline >= 7;
                case CampaignTriggerTokens.Discipline10:
                    return EscalationManager.Instance?.IsEnabled() == true && EscalationManager.Instance.State.Discipline >= 10;

                case CampaignTriggerTokens.LanceRep20:
                    return EscalationManager.Instance?.IsEnabled() == true && EscalationManager.Instance.State.LanceReputation >= 20;
                case CampaignTriggerTokens.LanceRep40:
                    return EscalationManager.Instance?.IsEnabled() == true && EscalationManager.Instance.State.LanceReputation >= 40;
                case CampaignTriggerTokens.LanceRepNeg20:
                    return EscalationManager.Instance?.IsEnabled() == true && EscalationManager.Instance.State.LanceReputation <= -20;
                case CampaignTriggerTokens.LanceRepNeg40:
                    return EscalationManager.Instance?.IsEnabled() == true && EscalationManager.Instance.State.LanceReputation <= -40;

                case CampaignTriggerTokens.Medical3:
                    return EscalationManager.Instance?.IsEnabled() == true && EscalationManager.Instance.State.MedicalRisk >= 3;
                case CampaignTriggerTokens.Medical4:
                    return EscalationManager.Instance?.IsEnabled() == true && EscalationManager.Instance.State.MedicalRisk >= 4;
                case CampaignTriggerTokens.Medical5:
                    return EscalationManager.Instance?.IsEnabled() == true && EscalationManager.Instance.State.MedicalRisk >= 5;

                // Player conditions
                case CampaignTriggerTokens.HasInjury:
                    return PlayerConditionBehavior.Instance?.IsEnabled() == true && PlayerConditionBehavior.Instance.State.HasInjury;
                case CampaignTriggerTokens.HasIllness:
                    return PlayerConditionBehavior.Instance?.IsEnabled() == true && PlayerConditionBehavior.Instance.State.HasIllness;
                case CampaignTriggerTokens.HasCondition:
                    return PlayerConditionBehavior.Instance?.IsEnabled() == true && PlayerConditionBehavior.Instance.State.HasAnyCondition;

                // Phase 7: Faction capability tokens
                case CampaignTriggerTokens.FactionHasHorseArchers:
                    return FactionHasHorseArcherTradition(enlistment);

                default:
                    // Recognized-but-unimplemented tokens are treated as false (loader warns once).
                    return false;
            }
        }

        private static bool IsEnemyNearbyFast()
        {
            try
            {
                var main = MobileParty.MainParty;
                if (main == null)
                {
                    return false;
                }

                // Avoid scanning unless the campaign exists.
                if (Campaign.Current == null)
                {
                    return false;
                }

                var faction = main.MapFaction;
                if (faction == null)
                {
                    return false;
                }

                // Small radius, early-exit scan. This runs only when a token explicitly asks for it.
                const float radius = 8f;
                var pos2D = main.GetPosition2D;

                foreach (var p in MobileParty.All)
                {
                    if (p == null || p == main)
                    {
                        continue;
                    }

                    if (!p.IsLordParty || p.MapFaction == null)
                    {
                        continue;
                    }

                    if (!faction.IsAtWarWith(p.MapFaction))
                    {
                        continue;
                    }

                    var distance = pos2D.Distance(p.GetPosition2D);
                    if (distance <= radius)
                    {
                        return true;
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Phase 7: Check if the player's enlisted faction has a horse archer tradition.
        /// Khuzait and Aserai cultures have strong horse archer traditions.
        /// </summary>
        private static bool FactionHasHorseArcherTradition(EnlistmentBehavior enlistment)
        {
            if (enlistment?.EnlistedLord?.Culture == null)
            {
                return false;
            }

            var cultureId = enlistment.EnlistedLord.Culture.StringId?.ToLowerInvariant() ?? "";

            // Khuzait and Aserai are the primary horse archer cultures
            return cultureId == "khuzait" || cultureId == "aserai";
        }

        private static bool TryEvaluateDaysComparison(string token, out bool result)
        {
            result = false;
            if (string.IsNullOrWhiteSpace(token))
            {
                return false;
            }

            // Accept:
            // - days_since_enlistment < 1
            // - days_since_enlistment<=1
            // - days_since_promotion >= 3
            // - days_enlisted > 14   (alias for days_since_enlistment)
            var t = token.Trim();

            if (!t.StartsWith(CampaignTriggerTokens.DaysSinceEnlistment, StringComparison.OrdinalIgnoreCase) &&
                !t.StartsWith(CampaignTriggerTokens.DaysSincePromotion, StringComparison.OrdinalIgnoreCase) &&
                !t.StartsWith(CampaignTriggerTokens.DaysEnlisted, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var daysValue = t.StartsWith(CampaignTriggerTokens.DaysSinceEnlistment, StringComparison.OrdinalIgnoreCase) ||
                            t.StartsWith(CampaignTriggerTokens.DaysEnlisted, StringComparison.OrdinalIgnoreCase)
                ? (LanceLifeOnboardingBehavior.Instance?.DaysSinceEnlistment ?? int.MaxValue)
                : (LanceLifeOnboardingBehavior.Instance?.DaysSincePromotion ?? int.MaxValue);

            // Remove prefix, parse operator + number.
            string rhs;
            if (t.StartsWith(CampaignTriggerTokens.DaysSinceEnlistment, StringComparison.OrdinalIgnoreCase))
            {
                rhs = t.Substring(CampaignTriggerTokens.DaysSinceEnlistment.Length);
            }
            else if (t.StartsWith(CampaignTriggerTokens.DaysEnlisted, StringComparison.OrdinalIgnoreCase))
            {
                rhs = t.Substring(CampaignTriggerTokens.DaysEnlisted.Length);
            }
            else
            {
                rhs = t.Substring(CampaignTriggerTokens.DaysSincePromotion.Length);
            }

            rhs = rhs.Trim();
            if (string.IsNullOrWhiteSpace(rhs))
            {
                return false;
            }

            // Normalize by inserting spaces around operators for simple split.
            rhs = rhs.Replace("<=", " <= ")
                     .Replace(">=", " >= ")
                     .Replace("==", " == ")
                     .Replace("!=", " != ")
                     .Replace("<", " < ")
                     .Replace(">", " > ")
                     .Replace("=", " = ");

            var parts = rhs.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
            {
                return false;
            }

            var op = parts[0];
            if (!int.TryParse(parts[1], out var target))
            {
                return false;
            }

            result = op switch
            {
                "<" => daysValue < target,
                "<=" => daysValue <= target,
                ">" => daysValue > target,
                ">=" => daysValue >= target,
                "==" => daysValue == target,
                "=" => daysValue == target,
                "!=" => daysValue != target,
                _ => false
            };

            return true;
        }

        private static bool TryEvaluateNumericComparison(string token, out bool result)
        {
            result = false;
            if (string.IsNullOrWhiteSpace(token))
            {
                return false;
            }

            var t = token.Trim();

            // Supported numeric sources:
            // - days_from_town
            // - logistics_strain
            // - morale_shock
            // - pay_tension
            // - heat / discipline / lance_reputation / medical_risk
            var source = GetNumericSourceValue(t, out var sourceValue);
            if (string.IsNullOrWhiteSpace(source))
            {
                return false;
            }

            var rhs = t.Substring(source.Length).Trim();
            if (string.IsNullOrWhiteSpace(rhs))
            {
                return false;
            }

            rhs = rhs.Replace("<=", " <= ")
                     .Replace(">=", " >= ")
                     .Replace("==", " == ")
                     .Replace("!=", " != ")
                     .Replace("<", " < ")
                     .Replace(">", " > ")
                     .Replace("=", " = ");

            var parts = rhs.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
            {
                return false;
            }

            var op = parts[0];
            if (!float.TryParse(parts[1], out var target))
            {
                return false;
            }

            result = op switch
            {
                "<" => sourceValue < target,
                "<=" => sourceValue <= target,
                ">" => sourceValue > target,
                ">=" => sourceValue >= target,
                "==" => Math.Abs(sourceValue - target) < 0.001f,
                "=" => Math.Abs(sourceValue - target) < 0.001f,
                "!=" => Math.Abs(sourceValue - target) >= 0.001f,
                _ => false
            };

            return true;
        }

        private static string GetNumericSourceValue(string token, out float value)
        {
            value = 0f;
            if (string.IsNullOrWhiteSpace(token))
            {
                return string.Empty;
            }

            if (token.StartsWith(CampaignTriggerTokens.DaysFromTown, StringComparison.OrdinalIgnoreCase))
            {
                var tracker = CampaignTriggerTrackerBehavior.Instance;
                if (tracker != null && tracker.LastTownEnteredTime != CampaignTime.Zero)
                {
                    value = (float)Math.Max(0d, CampaignTime.Now.ToDays - tracker.LastTownEnteredTime.ToDays);
                }
                else
                {
                    value = 999f;
                }
                return CampaignTriggerTokens.DaysFromTown;
            }

            var camp = CampLifeBehavior.Instance;
            if (token.StartsWith(CampaignTriggerTokens.LogisticsStrain, StringComparison.OrdinalIgnoreCase))
            {
                value = camp?.LogisticsStrain ?? 0f;
                return CampaignTriggerTokens.LogisticsStrain;
            }
            if (token.StartsWith(CampaignTriggerTokens.MoraleShock, StringComparison.OrdinalIgnoreCase))
            {
                value = camp?.MoraleShock ?? 0f;
                return CampaignTriggerTokens.MoraleShock;
            }
            if (token.StartsWith(CampaignTriggerTokens.PayTension, StringComparison.OrdinalIgnoreCase))
            {
                value = camp?.PayTension ?? 0f;
                return CampaignTriggerTokens.PayTension;
            }

            var esc = EscalationManager.Instance;
            if (token.StartsWith(CampaignTriggerTokens.Heat, StringComparison.OrdinalIgnoreCase))
            {
                value = esc?.State.Heat ?? 0;
                return CampaignTriggerTokens.Heat;
            }
            if (token.StartsWith(CampaignTriggerTokens.Discipline, StringComparison.OrdinalIgnoreCase))
            {
                value = esc?.State.Discipline ?? 0;
                return CampaignTriggerTokens.Discipline;
            }
            if (token.StartsWith(CampaignTriggerTokens.LanceReputation, StringComparison.OrdinalIgnoreCase))
            {
                value = esc?.State.LanceReputation ?? 0;
                return CampaignTriggerTokens.LanceReputation;
            }
            if (token.StartsWith(CampaignTriggerTokens.MedicalRisk, StringComparison.OrdinalIgnoreCase))
            {
                value = esc?.State.MedicalRisk ?? 0;
                return CampaignTriggerTokens.MedicalRisk;
            }

            return string.Empty;
        }

        public static bool IsAiSafe()
        {
            // Conservative "safe moment" gating:
            // - not in battle/map event
            // - not in active PlayerEncounter
            // - not in conversation
            // - not prisoner
            var hero = Hero.MainHero;
            if (hero?.IsPrisoner == true)
            {
                return false;
            }

            if (Hero.OneToOneConversationHero != null)
            {
                return false;
            }

            if (PlayerEncounter.Current != null)
            {
                return false;
            }

            var main = MobileParty.MainParty;
            if (main?.Party?.MapEvent != null)
            {
                return false;
            }

            return true;
        }
    }
}


