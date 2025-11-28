using TaleWorlds.MountAndBlade;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Mod.Core.Logging;

namespace Enlisted.Features.Combat.Stats.Behaviors
{
    /// <summary>
    /// Tracks player kills during battles in real-time.
    /// Only tracks when player is enlisted. Transfers kill count to campaign layer when battle ends.
    /// </summary>
    public class CombatStatsMissionBehavior : MissionBehavior
    {
        private int _killsAtBattleStart = 0;
        private bool _trackingActive = false;
        private int _retryAttempts = 0;
        private const int MaxRetryAttempts = 30; // Limit retries to ~0.5 seconds at 60fps

        public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;

        /// <summary>
        /// Called after mission starts. Try to start tracking if player agent is available.
        /// </summary>
        public override void AfterStart()
        {
            TryStartTracking();
        }

        /// <summary>
        /// Called when player agent controller is set. This is a reliable signal that MainAgent is ready.
        /// </summary>
        public override void OnAgentControllerSetToPlayer(Agent agent)
        {
            if (agent != null && agent == Mission?.MainAgent)
            {
                TryStartTracking();
            }
        }

        public override void OnMissionTick(float dt)
        {
            // Retry starting tracking if not started yet and MainAgent becomes available
            // Limit retries to prevent endless loops if something goes wrong
            if (!_trackingActive && _retryAttempts < MaxRetryAttempts && Mission?.MainAgent != null)
            {
                TryStartTracking();
            }
        }

        protected override void OnEndMission()
        {
            if (!_trackingActive || Mission?.MainAgent == null)
            {
                return;
            }

            // Calculate kills this battle
            int killsThisBattle = Mission.MainAgent.KillCount - _killsAtBattleStart;

            if (killsThisBattle > 0)
            {
                // Only record if player is still enlisted
                var enlistment = EnlistmentBehavior.Instance;
                if (enlistment?.IsEnlisted == true)
                {
                    // Transfer to campaign layer
                    var combatStats = CombatStatsBehavior.Instance;
                    combatStats?.RecordBattleKills(killsThisBattle);

                    // Silent success - no logging for normal operation
                }
                // Edge case: player not enlisted - no logging needed (expected behavior)
            }

            _trackingActive = false;
        }

        /// <summary>
        /// Starts tracking kills for this battle.
        /// Records the starting KillCount so we can calculate kills gained during battle.
        /// Can be called multiple times safely - only starts tracking once.
        /// </summary>
        public void StartTracking()
        {
            TryStartTracking();
        }

        /// <summary>
        /// Internal method to attempt starting tracking. Safe to call multiple times.
        /// </summary>
        private void TryStartTracking()
        {
            // Don't retry if already tracking
            if (_trackingActive)
            {
                return;
            }

            // Increment retry counter to prevent endless loops
            _retryAttempts++;

            // Check if player is enlisted before tracking
            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment?.IsEnlisted != true)
            {
                return;
            }

            if (Mission?.MainAgent != null)
            {
                _killsAtBattleStart = Mission.MainAgent.KillCount;
                _trackingActive = true;
                // Silent success - no logging for normal operation
            }
            // If MainAgent is still null, OnMissionTick will retry (up to MaxRetryAttempts)
        }
    }
}

