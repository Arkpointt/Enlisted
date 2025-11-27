using System;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Core;
using Enlisted.Mod.Core.Logging;

namespace Enlisted.Features.Combat.Behaviors
{
    /// <summary>
    /// Mission behavior that tracks player kills during battles.
    /// Kill count is used to award bonus XP for tier progression.
    /// Resets at mission start and reports to EnlistmentBehavior at mission end.
    /// </summary>
    public class EnlistedKillTrackerBehavior : MissionBehavior
    {
        /// <summary>
        /// Singleton instance for access from EnlistmentBehavior.
        /// </summary>
        public static EnlistedKillTrackerBehavior Instance { get; private set; }
        
        /// <summary>
        /// Number of enemies killed by the player in the current mission.
        /// </summary>
        public int KillCount { get; private set; } = 0;
        
        /// <summary>
        /// Whether the player participated in this battle (was present and active).
        /// </summary>
        public bool DidParticipate { get; private set; } = false;
        
        public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;
        
        public EnlistedKillTrackerBehavior()
        {
            Instance = this;
        }
        
        public override void AfterStart()
        {
            try
            {
                base.AfterStart();
                
                // Reset kill count for new mission
                KillCount = 0;
                DidParticipate = false;
                
                ModLogger.Debug("KillTracker", "Kill tracker initialized for new mission");
            }
            catch (Exception ex)
            {
                ModLogger.Error("KillTracker", $"Error in AfterStart: {ex.Message}");
            }
        }
        
        public override void OnMissionTick(float dt)
        {
            try
            {
                base.OnMissionTick(dt);
                
                // Mark participation if player agent exists and is active
                if (!DidParticipate && Agent.Main != null)
                {
                    try
                    {
                        if (Agent.Main.IsActive())
                        {
                            DidParticipate = true;
                            ModLogger.Debug("KillTracker", "Player participation confirmed");
                        }
                    }
                    catch
                    {
                        // Agent in invalid state
                    }
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("KillTracker", $"Error in OnMissionTick: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Called when any agent is removed from the mission (killed, fled, etc).
        /// We check if it was killed by the player and increment the counter.
        /// </summary>
        public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
        {
            try
            {
                base.OnAgentRemoved(affectedAgent, affectorAgent, agentState, blow);
                
                // Only count kills (not retreats, etc)
                if (agentState != AgentState.Killed && agentState != AgentState.Unconscious)
                {
                    return;
                }
                
                // Check if the killer was the player
                if (affectorAgent == null || affectorAgent != Agent.Main)
                {
                    return;
                }
                
                // Check if the victim was an enemy
                if (affectedAgent == null || affectedAgent.Team == null || Agent.Main?.Team == null)
                {
                    return;
                }
                
                // Don't count friendly fire
                if (affectedAgent.Team == Agent.Main.Team)
                {
                    return;
                }
                
                // Increment kill counter
                KillCount++;
                
                // Log every 5 kills to avoid spam
                if (KillCount % 5 == 0 || KillCount == 1)
                {
                    ModLogger.Debug("KillTracker", $"Player kill count: {KillCount}");
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("KillTracker", $"Error in OnAgentRemoved: {ex.Message}");
            }
        }
        
        protected override void OnEndMission()
        {
            try
            {
                base.OnEndMission();
                
                if (DidParticipate)
                {
                    ModLogger.Info("KillTracker", $"Mission ended - Player kills: {KillCount}, Participated: {DidParticipate}");
                }
                
                // Note: Don't reset here - EnlistmentBehavior will read and then reset via GetAndResetKillCount()
            }
            catch (Exception ex)
            {
                ModLogger.Error("KillTracker", $"Error in OnEndMission: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Gets the current kill count and resets it for the next battle.
        /// Called by EnlistmentBehavior when awarding battle XP.
        /// </summary>
        public int GetAndResetKillCount()
        {
            var kills = KillCount;
            KillCount = 0;
            return kills;
        }
        
        /// <summary>
        /// Gets participation status and resets it for the next battle.
        /// </summary>
        public bool GetAndResetParticipation()
        {
            var participated = DidParticipate;
            DidParticipate = false;
            return participated;
        }
    }
}

