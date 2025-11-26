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
        private bool _hasAssignedFormation = false;
        
        public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;
        
        /// <summary>
        /// Called after the mission starts. We try to assign formation here,
        /// but the player agent might not exist yet.
        /// </summary>
        public override void AfterStart()
        {
            base.AfterStart();
            TryAssignPlayerToFormation("AfterStart");
        }
        
        /// <summary>
        /// Called when deployment finishes. This is a reliable point where
        /// the player agent should exist.
        /// </summary>
        public override void OnDeploymentFinished()
        {
            base.OnDeploymentFinished();
            TryAssignPlayerToFormation("OnDeploymentFinished");
        }
        
        /// <summary>
        /// Called each mission tick. We use this as a fallback in case
        /// the player agent wasn't available in earlier callbacks.
        /// </summary>
        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);
            
            // Only try once per mission
            if (!_hasAssignedFormation)
            {
                TryAssignPlayerToFormation("OnMissionTick");
            }
        }
        
        /// <summary>
        /// Attempts to assign the player to their designated formation.
        /// Safe to call multiple times - will only assign once.
        /// </summary>
        private void TryAssignPlayerToFormation(string caller)
        {
            // Skip if already assigned
            if (_hasAssignedFormation)
            {
                return;
            }
            
            // Check if player is enlisted
            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment?.IsEnlisted != true || enlistment.IsOnLeave)
            {
                // Not enlisted or on leave - don't force formation assignment
                _hasAssignedFormation = true; // Mark as done so we don't keep checking
                return;
            }
            
            // Get the player agent
            var playerAgent = Agent.Main;
            if (playerAgent == null || !playerAgent.IsActive())
            {
                // Player not spawned yet - will try again
                return;
            }
            
            // Get the player's team
            var playerTeam = playerAgent.Team;
            if (playerTeam == null)
            {
                ModLogger.Debug("FormationAssignment", $"[{caller}] Player has no team - skipping formation assignment");
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
                _hasAssignedFormation = true;
                return;
            }
            
            // Assign the player to the formation
            try
            {
                playerAgent.Formation = targetFormation;
                _hasAssignedFormation = true;
                
                ModLogger.Info("FormationAssignment", $"[{caller}] Assigned enlisted player to {formationClass} formation (index: {targetFormation.Index})");
            }
            catch (Exception ex)
            {
                ModLogger.Error("FormationAssignment", $"[{caller}] Failed to assign player to formation: {ex.Message}");
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
            base.OnEndMission();
            _hasAssignedFormation = false; // Reset for next mission
        }
    }
}

