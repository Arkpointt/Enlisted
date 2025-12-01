using System;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Core;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Features.Assignments.Behaviors;
using Enlisted.Mod.Core.Logging;

namespace Enlisted.Features.Combat.Behaviors
{
    /// <summary>
    /// Mission behavior that automatically assigns enlisted players to their designated formation
    /// (Infantry, Archer, Cavalry, Horse Archer) when a battle starts.
    ///
    /// Without this, the player would float as an independent agent without being part of any formation,
    /// missing out on formation orders and not being sorted with their fellow soldiers.
    /// </summary>
    public class EnlistedFormationAssignmentBehavior : MissionBehavior
    {
        private Agent _assignedAgent = null;

        public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;

        /// <summary>
        /// Called after the mission starts. We try to assign formation here,
        /// but the player agent might not exist yet.
        /// Note: This may not fire if we're added after mission start.
        /// </summary>
        public override void AfterStart()
        {
            try
            {
                base.AfterStart();
                TryAssignPlayerToFormation("AfterStart");
            }
            catch (Exception ex)
            {
                ModLogger.Error("FormationAssignment", $"Error in AfterStart: {ex.Message}");
            }
        }

        /// <summary>
        /// Called when deployment finishes. This is a reliable point where
        /// the player agent should exist.
        /// </summary>
        public override void OnDeploymentFinished()
        {
            try
            {
                base.OnDeploymentFinished();
                TryAssignPlayerToFormation("OnDeploymentFinished");
            }
            catch (Exception ex)
            {
                ModLogger.Error("FormationAssignment", $"Error in OnDeploymentFinished: {ex.Message}");
            }
        }

        /// <summary>
        /// Called when an agent is built.
        /// This catches late joins, respawns, and reinforcements immediately.
        /// </summary>
        public override void OnAgentBuild(Agent agent, Banner banner)
        {
            try
            {
                base.OnAgentBuild(agent, banner);
                if (agent.IsMainAgent)
                {
                    TryAssignPlayerToFormation("OnAgentBuild", agent);
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("FormationAssignment", $"Error in OnAgentBuild: {ex.Message}");
            }
        }

        /// <summary>
        /// Called each mission tick. We use this as a fallback in case
        /// the player agent wasn't available in earlier callbacks.
        /// </summary>
        public override void OnMissionTick(float dt)
        {
            try
            {
                base.OnMissionTick(dt);
                TryAssignPlayerToFormation("OnMissionTick");
            }
            catch (Exception ex)
            {
                ModLogger.LogOnce("formation_tick_error", "FormationAssignment", $"Error in OnMissionTick: {ex.Message}");
            }
        }

        /// <summary>
        /// Attempts to assign the player to their designated formation.
        /// Safe to call multiple times - will only assign once per agent instance.
        /// </summary>
        private void TryAssignPlayerToFormation(string caller, Agent specificAgent = null)
        {
            // Get the player agent (either specific or Main)
            var playerAgent = specificAgent ?? Agent.Main;

            // If no agent or not main agent, skip
            if (playerAgent == null || !playerAgent.IsMainAgent)
            {
                return;
            }

            // Skip if already assigned for this specific agent instance
            if (playerAgent == _assignedAgent)
            {
                return;
            }

            // Check if player is enlisted
            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment?.IsEnlisted != true || enlistment.IsOnLeave)
            {
                // Not enlisted or on leave - don't force formation assignment
                _assignedAgent = playerAgent; // Mark as handled so we don't keep checking
                return;
            }

            // Ensure mission is valid
            if (Mission.Current == null)
            {
                return;
            }

            // Check if agent is active (with null safety)
            try
            {
                if (!playerAgent.IsActive())
                {
                    return;
                }
            }
            catch
            {
                // Agent might be in invalid state - skip
                return;
            }

            // Get the player's team
            var playerTeam = playerAgent.Team;
            if (playerTeam == null)
            {
                // Team might not be assigned yet (especially in OnAgentBuild)
                // We'll try again next tick
                return;
            }

            // Get the designated formation from duties system
            var duties = EnlistedDutiesBehavior.Instance;
            var formationString = duties?.PlayerFormation ?? "infantry";

            // Convert string to FormationClass
            var formationClass = GetFormationClassFromString(formationString);

            // Get the formation from the team
            var targetFormation = playerTeam.GetFormation(formationClass);
            if (targetFormation == null)
            {
                ModLogger.Debug("FormationAssignment", $"[{caller}] Could not get {formationClass} formation from team");
                return;
            }

            // Check if player is already in this formation
            if (playerAgent.Formation == targetFormation)
            {
                ModLogger.Debug("FormationAssignment", $"[{caller}] Player already in {formationClass} formation");
                SuppressPlayerCommand(playerAgent);
                _assignedAgent = playerAgent;
                return;
            }

            // Assign the player to the formation
            try
            {
                playerAgent.Formation = targetFormation;
                SuppressPlayerCommand(playerAgent);
                _assignedAgent = playerAgent;

                ModLogger.Info("FormationAssignment", $"[{caller}] Assigned enlisted player to {formationClass} formation (index: {targetFormation.Index})");
            }
            catch (Exception ex)
            {
                ModLogger.LogOnce($"assign_error_{playerAgent.Index}", "FormationAssignment", $"[{caller}] Failed to assign player to formation: {ex.Message}");
                // Mark as assigned to prevent spamming this error for this agent
                _assignedAgent = playerAgent;
            }
        }

        /// <summary>
        /// Strips command authority from the enlisted player.
        /// Prevents them from being captain or general.
        /// </summary>
        private void SuppressPlayerCommand(Agent playerAgent)
        {
            if (playerAgent == null || EnlistmentBehavior.Instance?.IsEnlisted != true) return;

            try
            {
                if (playerAgent.Team == null) return;

                bool captaincyStripped = false;
                bool roleStripped = false;
                string commandTransferredTo = null;

                // 1. Strip Captaincy
                if (playerAgent.Formation != null && playerAgent.Formation.Captain == playerAgent)
                {
                    playerAgent.Formation.Captain = null;
                    captaincyStripped = true;
                }

                // 2. Strip Generalship Role (Force AI Control on all formations)
                // This ensures the game logic knows the player is not the general.
                if (playerAgent.Team.IsPlayerGeneral || playerAgent.Team.IsPlayerSergeant)
                {
                    playerAgent.Team.SetPlayerRole(false, false);
                    roleStripped = true;
                }

                // 3. Strip Generalship (OrderController Owner)
                // We also explicitly transfer the controller ownership to ensure UI/Keys don't target the player.
                if (playerAgent.Team.PlayerOrderController?.Owner == playerAgent)
                {
                     // Try to find the Lord to give command to
                     var lord = EnlistmentBehavior.Instance.CurrentLord;
                     Agent lordAgent = null;

                     if (lord != null && playerAgent.Team.ActiveAgents != null)
                     {
                         // Find the lord agent in the team
                         foreach (var agent in playerAgent.Team.ActiveAgents)
                         {
                             if (agent.Character == lord.CharacterObject)
                             {
                                 lordAgent = agent;
                                 break;
                             }
                         }
                     }

                     if (lordAgent != null)
                     {
                         playerAgent.Team.PlayerOrderController.Owner = lordAgent;
                         commandTransferredTo = $"Lord {lord.Name}";
                     }
                     else
                     {
                         // If Lord not found, try to transfer to the Team General (if not player)
                         var general = playerAgent.Team.GeneralAgent;
                         if (general != null && general != playerAgent)
                         {
                             playerAgent.Team.PlayerOrderController.Owner = general;
                             commandTransferredTo = "Team General";
                         }
                         else
                         {
                             // Fallback: Find any other hero or agent to take command
                             Agent otherAgent = null;
                             if (playerAgent.Team.ActiveAgents != null)
                             {
                                 foreach (var agent in playerAgent.Team.ActiveAgents)
                                 {
                                     if (agent != playerAgent && agent.IsHero)
                                     {
                                         otherAgent = agent;
                                         break;
                                     }
                                 }
                             }

                             if (otherAgent != null)
                             {
                                  playerAgent.Team.PlayerOrderController.Owner = otherAgent;
                                  commandTransferredTo = "Other Hero";
                             }
                         }
                     }
                }

                // 4. Verification & Logging
                bool isStillCaptain = playerAgent.Formation?.Captain == playerAgent;
                bool isStillGeneral = playerAgent.Team.PlayerOrderController?.Owner == playerAgent;
                bool isStillRoleGeneral = playerAgent.Team.IsPlayerGeneral;

                if (captaincyStripped || roleStripped || commandTransferredTo != null)
                {
                    ModLogger.Info("FormationAssignment", $"Command Suppression: Captaincy Stripped={captaincyStripped}, Role Stripped={roleStripped}, Command Transferred To={commandTransferredTo ?? "None"}");
                }

                if (isStillCaptain || isStillGeneral || isStillRoleGeneral)
                {
                    ModLogger.Warn("FormationAssignment", $"Command Suppression Incomplete! Player is still: {(isStillCaptain ? "Captain " : "")}{(isStillGeneral ? "OrderControllerOwner " : "")}{(isStillRoleGeneral ? "GeneralRole" : "")}");
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("FormationAssignment", $"Error suppressing player command: {ex.Message}");
            }
        }

        /// <summary>
        /// Converts our formation string (from duties system) to a FormationClass enum.
        /// </summary>
        private FormationClass GetFormationClassFromString(string formation)
        {
            return formation?.ToLower() switch
            {
                "infantry" => FormationClass.Infantry,
                "archer" => FormationClass.Ranged,
                "ranged" => FormationClass.Ranged,
                "cavalry" => FormationClass.Cavalry,
                "horsearcher" => FormationClass.HorseArcher,
                "horse_archer" => FormationClass.HorseArcher,
                _ => FormationClass.Infantry // Default fallback
            };
        }

        protected override void OnEndMission()
        {
            try
            {
                base.OnEndMission();
                _assignedAgent = null; // Reset for next mission
            }
            catch (Exception ex)
            {
                ModLogger.Error("FormationAssignment", $"Error in OnEndMission: {ex.Message}");
            }
        }
    }
}

