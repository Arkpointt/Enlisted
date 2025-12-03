using System;
using Enlisted.Features.Assignments.Behaviors;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Mod.Core.Logging;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Enlisted.Features.Combat.Behaviors
{
    /// <summary>
    ///     Mission behavior that automatically assigns enlisted players to their designated formation
    ///     (Infantry, Archer, Cavalry, Horse Archer) when a battle starts.
    ///     Without this, the player would float as an independent agent without being part of any formation,
    ///     missing out on formation orders and not being sorted with their fellow soldiers.
    ///     FIX: Also teleports the player to the correct position within their formation to handle
    ///     cases where the player's map party was slightly behind the lord when battle started,
    ///     causing them to spawn in the wrong position (behind the formation instead of in it).
    /// </summary>
    public class EnlistedFormationAssignmentBehavior : MissionBehavior
    {
        private const int MaxPositionFixAttempts = 30; // Try for about 0.5 seconds at 60fps
        private Agent _assignedAgent;

        // Track if we've logged the behavior initialization
        private bool _hasLoggedInit;

        // Track whether we need to teleport the player to their formation position
        // This handles the case where the player spawned late or in wrong position
        private bool _needsPositionFix;
        private int _positionFixAttempts;

        public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;

        /// <summary>
        ///     Called after the mission starts. We try to assign formation here,
        ///     but the player agent might not exist yet.
        ///     Note: This may not fire if we're added after mission start.
        /// </summary>
        public override void AfterStart()
        {
            try
            {
                base.AfterStart();

                // Log that the behavior has been initialized (once per mission)
                if (!_hasLoggedInit)
                {
                    _hasLoggedInit = true;
                    var enlistment = EnlistmentBehavior.Instance;
                    ModLogger.Info("FormationAssignment",
                        $"=== BEHAVIOR ACTIVE === Mission: {Mission.Current?.Mode}, " +
                        $"Enlisted: {enlistment?.IsEnlisted}, OnLeave: {enlistment?.IsOnLeave}, " +
                        $"Agent.Main exists: {Agent.Main != null}");
                }

                TryAssignPlayerToFormation("AfterStart");
            }
            catch (Exception ex)
            {
                ModLogger.Error("FormationAssignment", $"Error in AfterStart: {ex.Message}");
            }
        }

        /// <summary>
        ///     Called when deployment finishes. This is a reliable point where
        ///     the player agent should exist.
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
        ///     Called when an agent is built.
        ///     This catches late joins, respawns, and reinforcements immediately.
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
        ///     Called each mission tick. We use this as a fallback in case
        ///     the player agent wasn't available in earlier callbacks.
        ///     Also handles delayed position correction for players who spawned late.
        /// </summary>
        public override void OnMissionTick(float dt)
        {
            try
            {
                base.OnMissionTick(dt);
                TryAssignPlayerToFormation("OnMissionTick");

                // Handle position fix for players who may have spawned in wrong location
                // This happens when the player's map party lagged behind the lord
                if (_needsPositionFix && _positionFixAttempts < MaxPositionFixAttempts)
                {
                    _positionFixAttempts++;
                    TryTeleportPlayerToFormationPosition();
                }
            }
            catch (Exception ex)
            {
                ModLogger.LogOnce("formation_tick_error", "FormationAssignment",
                    $"Error in OnMissionTick: {ex.Message}");
            }
        }

        /// <summary>
        ///     Attempts to assign the player to their designated formation.
        ///     Safe to call multiple times - will only assign once per agent instance.
        /// </summary>
        private void TryAssignPlayerToFormation(string caller, Agent specificAgent = null)
        {
            // Get the player agent (either specific or Main)
            var playerAgent = specificAgent ?? Agent.Main;

            // If no agent or not main agent, skip
            if (playerAgent == null || !playerAgent.IsMainAgent)
            {
                // Only log once per callback to avoid spam
                if (caller == "AfterStart" || caller == "OnDeploymentFinished")
                {
                    ModLogger.Debug("FormationAssignment",
                        $"[{caller}] No player agent yet (null: {playerAgent == null})");
                }

                return;
            }

            // Skip if already assigned for this specific agent instance
            if (playerAgent == _assignedAgent)
            {
                return;
            }

            // Skip in naval battles - the Naval DLC has its own ship-based spawn system
            // Our ground-based formation teleporting would interfere with ship positioning
            if (Mission.Current?.IsNavalBattle == true)
            {
                ModLogger.LogOnce("formation_naval_skip", "FormationAssignment",
                    $"[{caller}] Skipping formation assignment - naval battle uses ship-based spawning");
                return;
            }

            // Check if player is enlisted
            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment?.IsEnlisted != true || enlistment.IsOnLeave)
            {
                // Not enlisted or on leave - don't force formation assignment
                // Log this at Info level since it's an important state decision
                ModLogger.LogOnce("formation_not_enlisted", "FormationAssignment",
                    $"[{caller}] Skipping formation assignment - not enlisted or on leave " +
                    $"(Instance: {enlistment != null}, Enlisted: {enlistment?.IsEnlisted}, OnLeave: {enlistment?.IsOnLeave})");
                _assignedAgent = playerAgent; // Mark as handled so we don't keep checking
                return;
            }

            // Ensure mission is valid
            if (Mission.Current == null)
            {
                ModLogger.Debug("FormationAssignment", $"[{caller}] Mission.Current is null");
                return;
            }

            // Check if agent is active (with null safety)
            try
            {
                if (!playerAgent.IsActive())
                {
                    ModLogger.Debug("FormationAssignment", $"[{caller}] Player agent not active yet");
                    return;
                }
            }
            catch
            {
                // Agent might be in invalid state - skip
                ModLogger.Debug("FormationAssignment", $"[{caller}] Player agent in invalid state");
                return;
            }

            // Get the player's team
            var playerTeam = playerAgent.Team;
            if (playerTeam == null)
            {
                // Team might not be assigned yet (especially in OnAgentBuild)
                // We'll try again next tick
                ModLogger.Debug("FormationAssignment", $"[{caller}] Player team not assigned yet");
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
                ModLogger.Debug("FormationAssignment",
                    $"[{caller}] Could not get {formationClass} formation from team");
                return;
            }

            // Check if player is already in this formation
            if (playerAgent.Formation == targetFormation)
            {
                ModLogger.Info("FormationAssignment",
                    $"[{caller}] Player already in {formationClass} formation - will still check position");
                SuppressPlayerCommand(playerAgent);
                _assignedAgent = playerAgent;

                // FIX: Still need to check position even if already in correct formation
                // The game may have auto-assigned the player but spawned them in wrong location
                if (!_needsPositionFix)
                {
                    _needsPositionFix = true;
                    _positionFixAttempts = 0;
                }

                return;
            }

            // Assign the player to the formation
            try
            {
                playerAgent.Formation = targetFormation;
                SuppressPlayerCommand(playerAgent);
                _assignedAgent = playerAgent;

                // FIX: Mark that we need to teleport the player to their formation position
                // This handles cases where the player spawned late or in wrong location
                // because their map party was slightly behind the lord when battle started
                _needsPositionFix = true;
                _positionFixAttempts = 0;

                ModLogger.Info("FormationAssignment",
                    $"[{caller}] Assigned enlisted player to {formationClass} formation (index: {targetFormation.Index}) - will attempt position fix");
            }
            catch (Exception ex)
            {
                ModLogger.LogOnce($"assign_error_{playerAgent.Index}", "FormationAssignment",
                    $"[{caller}] Failed to assign player to formation: {ex.Message}");
                // Mark as assigned to prevent spamming this error for this agent
                _assignedAgent = playerAgent;
            }
        }

        /// <summary>
        ///     Attempts to teleport the player to the correct position within their assigned formation.
        ///     This fixes the issue where players spawn behind their formation when their map party
        ///     was slightly behind the lord when battle started.
        ///     The teleport only happens once, after formation assignment, and only if the player
        ///     is significantly far from their formation's position.
        /// </summary>
        private void TryTeleportPlayerToFormationPosition()
        {
            try
            {
                var playerAgent = Agent.Main;
                if (playerAgent == null || !playerAgent.IsMainAgent || !playerAgent.IsActive())
                {
                    return;
                }

                var enlistment = EnlistmentBehavior.Instance;
                if (enlistment?.IsEnlisted != true || enlistment.IsOnLeave)
                {
                    _needsPositionFix = false;
                    return;
                }

                var formation = playerAgent.Formation;
                if (formation == null)
                {
                    // Formation not assigned yet - keep trying
                    return;
                }

                // Check if the formation has a valid position
                // Use CachedMedianPosition which is on Formation directly (not FormationQuerySystem)
                var formationPosition = formation.CachedMedianPosition;
                if (!formationPosition.IsValid)
                {
                    // Formation doesn't have valid position yet - keep trying
                    ModLogger.Debug("FormationAssignment", "Formation position not valid yet, will retry position fix");
                    return;
                }

                var playerPosition = playerAgent.Position;
                var targetPosition = formationPosition.GetGroundVec3();

                // Calculate distance to formation center
                var distanceToFormation = playerPosition.Distance(targetPosition);

                // Only teleport if player is significantly far from formation (more than 15 meters)
                // This prevents unnecessary teleports for players who spawned correctly
                const float minTeleportDistance = 15f;

                if (distanceToFormation > minTeleportDistance)
                {
                    // PRIMARY: Teleport to formation's CURRENT median position
                    // This is where the formation actually IS right now in battle,
                    // not the deployment spawn frame which may be far behind
                    playerAgent.TeleportToPosition(targetPosition);

                    // Try to face the same direction as the formation
                    if (formation.Direction.IsValid)
                    {
                        playerAgent.SetMovementDirection(formation.Direction);
                        playerAgent.LookDirection = formation.Direction.ToVec3();
                    }

                    ModLogger.Info("FormationAssignment",
                        $"Teleported player to formation's current position (was {distanceToFormation:F1}m away, formation: {formation.PhysicalClass})");

                    // Force the agent to update its cached values
                    playerAgent.ForceUpdateCachedAndFormationValues(true, false);
                }
                else
                {
                    ModLogger.Debug("FormationAssignment",
                        $"Player already near formation ({distanceToFormation:F1}m) - no teleport needed");
                }

                // Position fix complete - stop trying
                _needsPositionFix = false;
            }
            catch (Exception ex)
            {
                ModLogger.Error("FormationAssignment", $"Error teleporting player to formation: {ex.Message}");
                _needsPositionFix = false; // Stop trying on error
            }
        }

        /// <summary>
        ///     Strips command authority from the enlisted player.
        ///     Prevents them from being captain or general.
        /// </summary>
        private void SuppressPlayerCommand(Agent playerAgent)
        {
            if (playerAgent == null || EnlistmentBehavior.Instance?.IsEnlisted != true)
            {
                return;
            }

            try
            {
                if (playerAgent.Team == null)
                {
                    return;
                }

                var captaincyStripped = false;
                var roleStripped = false;
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
                var isStillCaptain = playerAgent.Formation?.Captain == playerAgent;
                var isStillGeneral = playerAgent.Team.PlayerOrderController?.Owner == playerAgent;
                var isStillRoleGeneral = playerAgent.Team.IsPlayerGeneral;

                if (captaincyStripped || roleStripped || commandTransferredTo != null)
                {
                    ModLogger.Info("FormationAssignment",
                        $"Command Suppression: Captaincy Stripped={captaincyStripped}, Role Stripped={roleStripped}, Command Transferred To={commandTransferredTo ?? "None"}");
                }

                if (isStillCaptain || isStillGeneral || isStillRoleGeneral)
                {
                    ModLogger.Warn("FormationAssignment",
                        $"Command Suppression Incomplete! Player is still: {(isStillCaptain ? "Captain " : "")}{(isStillGeneral ? "OrderControllerOwner " : "")}{(isStillRoleGeneral ? "GeneralRole" : "")}");
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("FormationAssignment", $"Error suppressing player command: {ex.Message}");
            }
        }

        /// <summary>
        ///     Converts our formation string (from duties system) to a FormationClass enum.
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
                _needsPositionFix = false;
                _positionFixAttempts = 0;
            }
            catch (Exception ex)
            {
                ModLogger.Error("FormationAssignment", $"Error in OnEndMission: {ex.Message}");
            }
        }
    }
}
