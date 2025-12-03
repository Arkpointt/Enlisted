using Enlisted.Features.CommandTent.Core;
using Enlisted.Mod.Core.Logging;
using TaleWorlds.CampaignSystem;

namespace Enlisted.Features.Combat.Stats.Behaviors
{
    /// <summary>
    /// Tracks player kill counts and manages combat reward payouts.
    /// Total kills persist across all enlistments. Unrewarded kills accumulate
    /// and are paid out during daily wage tick (15 gold + 5 XP per kill).
    /// </summary>
    public sealed class CombatStatsBehavior : CampaignBehaviorBase
    {
        public static CombatStatsBehavior Instance { get; private set; }

        private int _totalKills;
        private int _unrewardedKills;

        public int TotalKills => _totalKills;
        public int UnrewardedKills => _unrewardedKills;

        public CombatStatsBehavior()
        {
            Instance = this;
        }

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("_totalKills", ref _totalKills);
            dataStore.SyncData("_unrewardedKills", ref _unrewardedKills);
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            ModLogger.Info("CombatStats", "Combat statistics system initialized");
        }

        /// <summary>
        /// Records kills from a completed battle.
        /// Called by MissionBehavior when battle ends.
        /// Adds kills to both total (lifetime) and unrewarded (pending payment) counts.
        /// </summary>
        public void RecordBattleKills(int killCount)
        {
            if (killCount <= 0)
            {
                return;
            }

            _totalKills += killCount;
            _unrewardedKills += killCount;

            ServiceRecordManager.Instance?.OnKillsRecorded(killCount);
        }

        /// <summary>
        /// Gets pending kill rewards (gold and XP) for unrewarded kills.
        /// Returns (gold, xp) tuple where gold = kills * 15, xp = kills * 5.
        /// </summary>
        public (int gold, int xp) GetPendingKillRewards()
        {
            if (_unrewardedKills <= 0)
            {
                return (0, 0);
            }

            int gold = _unrewardedKills * 15;
            int xp = _unrewardedKills * 5;

            return (gold, xp);
        }

        /// <summary>
        /// Pays out kill rewards and clears the unrewarded kill count.
        /// Called after rewards have been awarded to the player.
        /// </summary>
        public void PayOutKillRewards()
        {
            if (_unrewardedKills <= 0)
            {
                return;
            }

            _unrewardedKills = 0;

            // Silent success - no logging for normal operation
        }

        /// <summary>
        /// Resets enlistment-specific kill counter (not total kills).
        /// Called when enlistment ends. Total kills persist across enlistments.
        /// </summary>
        public void ResetEnlistmentKills()
        {
            // Note: We don't reset _totalKills - it persists across enlistments
            // Unrewarded kills also persist - they'll be paid if player re-enlists
            // Silent success - no logging for normal operation
        }
    }
}

