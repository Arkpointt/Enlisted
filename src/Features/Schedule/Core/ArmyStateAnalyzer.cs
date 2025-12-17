using Enlisted.Features.Schedule.Models;
using Enlisted.Mod.Core.Logging;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;

namespace Enlisted.Features.Schedule.Core
{
    /// <summary>
    /// Analyzes army state to determine lord's current objective and priority.
    /// Used by schedule generator to assign appropriate duties.
    /// </summary>
    public static class ArmyStateAnalyzer
    {
        private const string LogCategory = "Schedule";

        /// <summary>
        /// Determine the lord's current objective based on army state.
        /// </summary>
        public static LordObjective GetLordObjective(MobileParty army)
        {
            if (army == null)
            {
                ModLogger.Warn(LogCategory, "GetLordObjective called with null army");
                return LordObjective.Unknown;
            }

            // Check if besieging
            if (army.BesiegerCamp != null)
            {
                return LordObjective.Besieging;
            }

            // Check if defending (in settlement)
            if (army.CurrentSettlement != null && army.CurrentSettlement.IsUnderSiege)
            {
                return LordObjective.Defending;
            }

            // Check if fleeing (low morale, being chased)
            if (army.Morale < 30 && army.IsActive && army.TargetParty != null)
            {
                // If target party is stronger and we're moving away, we're fleeing
                if (army.TargetParty.Party.NumberOfAllMembers > army.Party.NumberOfAllMembers * 1.5f)
                {
                    return LordObjective.Fleeing;
                }
            }

            // Check army AI behavior
            if (army.Army != null)
            {
                var armyLeader = army.Army.LeaderParty;
                if (armyLeader != null)
                {
                    // Check if raiding (in enemy territory with hostile intent)
                    if (armyLeader.MapFaction != null && 
                        armyLeader.CurrentSettlement != null &&
                        armyLeader.CurrentSettlement.MapFaction != null &&
                        armyLeader.MapFaction.IsAtWarWith(armyLeader.CurrentSettlement.MapFaction))
                    {
                        return LordObjective.Raiding;
                    }

                    // Check if patrolling (moving in own territory)
                    if (armyLeader.DefaultBehavior == AiBehavior.PatrolAroundPoint ||
                        armyLeader.DefaultBehavior == AiBehavior.EscortParty)
                    {
                        return LordObjective.Patrolling;
                    }
                }
            }

            // Check if resting (in friendly settlement, not under siege)
            if (army.CurrentSettlement != null && 
                !army.CurrentSettlement.IsUnderSiege &&
                army.CurrentSettlement.MapFaction == army.MapFaction)
            {
                return LordObjective.Resting;
            }

            // Check if traveling (moving between locations)
            if (army.TargetSettlement != null || army.TargetParty != null)
            {
                return LordObjective.Traveling;
            }

            // Default to patrolling if no specific state detected
            return LordObjective.Patrolling;
        }

        /// <summary>
        /// Determine priority level for the lord's current objective.
        /// </summary>
        public static LordOrderPriority GetObjectivePriority(LordObjective objective, MobileParty army)
        {
            switch (objective)
            {
                case LordObjective.PreparingBattle:
                case LordObjective.Fleeing:
                    // Battle imminent or fleeing - critical priority
                    return LordOrderPriority.Critical;

                case LordObjective.Besieging:
                case LordObjective.Defending:
                    // Active siege operations - high priority
                    return LordOrderPriority.High;

                case LordObjective.Raiding:
                case LordObjective.Patrolling:
                    // Active operations - medium priority
                    return LordOrderPriority.Medium;

                case LordObjective.Traveling:
                case LordObjective.Resting:
                    // Peaceful activities - low priority
                    return LordOrderPriority.Low;

                default:
                    return LordOrderPriority.Medium;
            }
        }

        /// <summary>
        /// Get a human-readable description of the lord's orders.
        /// </summary>
        public static string GetObjectiveDescription(LordObjective objective)
        {
            return objective switch
            {
                LordObjective.Patrolling => "Patrol the territory and watch for threats",
                LordObjective.Besieging => "Maintain siege operations and secure the perimeter",
                LordObjective.PreparingBattle => "Prepare for imminent battle",
                LordObjective.Traveling => "March to destination and maintain formation",
                LordObjective.Resting => "Rest and recover, maintain camp discipline",
                LordObjective.Defending => "Defend the settlement from siege",
                LordObjective.Raiding => "Raid enemy territory and gather supplies",
                LordObjective.Fleeing => "Retreat and preserve the army",
                _ => "Maintain readiness and follow orders"
            };
        }
    }
}

