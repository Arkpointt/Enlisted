using System;
using System.Collections.Generic;
using System.Linq;
using Enlisted.Features.Retinue.Core;
using Enlisted.Features.Retinue.Data;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Mod.Core.Logging;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;

namespace Enlisted.Features.Retinue.Systems
{
    /// <summary>
    /// Tracks retinue casualties during battles. Takes a snapshot of troop counts before battle,
    /// then compares against the actual roster after battle to sync our tracking state.
    /// Native casualty handling updates the roster automatically; we just reconcile our state.
    /// </summary>
    public sealed class RetinueCasualtyTracker : CampaignBehaviorBase
    {
        private const string LogCategory = "CasualtyTracker";

        // Pre-battle snapshot of retinue troop counts
        private Dictionary<string, int> _preBattleCounts;

        // Track if we're currently in a battle
        private bool _isInBattle;

        public static RetinueCasualtyTracker Instance { get; private set; }

        public RetinueCasualtyTracker()
        {
            Instance = this;
            _preBattleCounts = new Dictionary<string, int>();
        }

        public override void RegisterEvents()
        {
            // Hook into battle start/end events
            CampaignEvents.MapEventStarted.AddNonSerializedListener(this, OnMapEventStarted);
            CampaignEvents.MapEventEnded.AddNonSerializedListener(this, OnMapEventEnded);

            // Also hook into player battle end for additional tracking
            CampaignEvents.OnPlayerBattleEndEvent.AddNonSerializedListener(this, OnPlayerBattleEnd);

            // Daily sync to catch wounded soldiers who die from wounds between battles
            // This ensures trickle can activate sooner when deaths occur outside of combat
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);

            ModLogger.Debug(LogCategory, "RetinueCasualtyTracker registered for battle events and daily sync");
        }

        public override void SyncData(IDataStore dataStore)
        {
            // Pre-battle counts are transient and don't need to be saved
            // They're only valid during an active battle session
        }

        #region Battle Event Handlers

        /// <summary>
        /// Takes a snapshot of retinue counts when battle starts. Naval battles logged with dismount note.
        /// </summary>
        private void OnMapEventStarted(MapEvent mapEvent, PartyBase attackerParty, PartyBase defenderParty)
        {
            try
            {
                if (!IsPlayerInvolvedInMapEvent(mapEvent))
                {
                    return;
                }

                var manager = RetinueManager.Instance;
                if (manager?.State == null || !manager.State.HasRetinue)
                {
                    return;
                }

                TakePreBattleSnapshot(manager.State);
                _isInBattle = true;

                var retinueCount = manager.State.TotalSoldiers;
                var retinueType = manager.State.SelectedTypeId ?? "unknown";
                var isNaval = mapEvent.IsNavalMapEvent;
                
                if (isNaval)
                {
                    var isMounted = retinueType is "cavalry" or "horse_archers";
                    var note = isMounted ? " [dismounted]" : "";
                    ModLogger.Info(LogCategory, $"NAVAL battle snapshot: {retinueCount} {retinueType}{note}");
                }
                else
                {
                    ModLogger.Info(LogCategory, $"Battle snapshot: {retinueCount} retinue soldiers");
                }
            }
            catch (Exception ex)
            {
                ModLogger.ErrorCode(LogCategory, "E-CASUALTY-001", "Error in OnMapEventStarted", ex);
            }
        }

        /// <summary>
        /// Called when a map event (battle) ends. Reconciles casualties with our tracking state.
        /// </summary>
        private void OnMapEventEnded(MapEvent mapEvent)
        {
            try
            {
                // Only process if we took a snapshot for this battle
                if (!_isInBattle || _preBattleCounts == null || _preBattleCounts.Count == 0)
                {
                    return;
                }

                // Check if player was involved
                if (!IsPlayerInvolvedInMapEvent(mapEvent))
                {
                    return;
                }

                // Reconcile casualties
                ReconcileCasualties();
                _isInBattle = false;
            }
            catch (Exception ex)
            {
                ModLogger.ErrorCode(LogCategory, "E-CASUALTY-002", "Error in OnMapEventEnded", ex);
                _isInBattle = false;
            }
        }

        /// <summary>
        /// Called when a player battle specifically ends. Provides additional casualty tracking opportunity.
        /// </summary>
        private void OnPlayerBattleEnd(MapEvent mapEvent)
        {
            try
            {
                // Only process if we have a pending snapshot
                if (!_isInBattle || _preBattleCounts == null || _preBattleCounts.Count == 0)
                {
                    return;
                }

                // Reconcile if not already done by OnMapEventEnded
                ReconcileCasualties();
                _isInBattle = false;
            }
            catch (Exception ex)
            {
                ModLogger.ErrorCode(LogCategory, "E-CASUALTY-003", "Error in OnPlayerBattleEnd", ex);
                _isInBattle = false;
            }
        }

        /// <summary>
        /// Daily tick handler - syncs retinue tracking with actual roster to catch
        /// wounded soldiers who died from wounds between battles.
        /// </summary>
        private void OnDailyTick()
        {
            try
            {
                // Skip sync during active battles
                if (_isInBattle)
                {
                    return;
                }

                var manager = RetinueManager.Instance;
                if (manager?.State == null || !manager.State.HasRetinue)
                {
                    return;
                }

                // Perform a quiet sync - only log if corrections are needed
                SyncWithRosterQuiet();
            }
            catch (Exception ex)
            {
                ModLogger.ErrorCode(LogCategory, "E-CASUALTY-004", "Error in daily sync", ex);
            }
        }

        /// <summary>
        /// Quietly syncs retinue tracking with roster, logging only when corrections occur.
        /// Used by daily tick to catch wounded soldiers who died from wounds.
        /// </summary>
        private void SyncWithRosterQuiet()
        {
            var manager = RetinueManager.Instance;
            if (manager?.State == null)
            {
                return;
            }

            var roster = MobileParty.MainParty?.MemberRoster;
            if (roster == null)
            {
                return;
            }

            var state = manager.State;
            var totalLost = 0;
            var lossLog = new List<string>();

            foreach (var troopId in state.TroopCounts.Keys.ToList())
            {
                var character = CharacterObject.Find(troopId);
                if (character == null)
                {
                    continue;
                }

                var actualCount = roster.GetTroopCount(character);
                var trackedCount = state.GetTroopCount(troopId);

                // Only care about losses (wounded dying from wounds)
                if (actualCount < trackedCount)
                {
                    var lost = trackedCount - actualCount;
                    state.UpdateTroopCount(troopId, -lost);
                    totalLost += lost;
                    lossLog.Add($"{lost}x {character.Name}");
                }
            }

            // Clean up any zero-count entries
            CleanupZeroCountEntries(state);

            // Log if we found soldiers who died from wounds
            if (totalLost > 0)
            {
                ModLogger.Info(LogCategory,
                    $"Wounded casualties: {totalLost} soldiers succumbed to wounds ({string.Join(", ", lossLog)}), " +
                    $"{state.TotalSoldiers} remaining");
            }
        }

        #endregion

        #region Snapshot and Reconciliation

        /// <summary>
        /// Takes a snapshot of the current retinue troop counts before battle.
        /// </summary>
        private void TakePreBattleSnapshot(RetinueState state)
        {
            _preBattleCounts = new Dictionary<string, int>();

            if (state?.TroopCounts == null)
            {
                return;
            }

            foreach (var kvp in state.TroopCounts)
            {
                if (kvp.Value > 0)
                {
                    _preBattleCounts[kvp.Key] = kvp.Value;
                }
            }

            ModLogger.Debug(LogCategory,
                $"Pre-battle snapshot: {_preBattleCounts.Count} troop types, {_preBattleCounts.Values.Sum()} total soldiers");
        }

        /// <summary>
        /// Reconciles the post-battle roster with our tracked counts.
        /// Native casualty handling already updated the roster; we sync our tracking state.
        /// </summary>
        private void ReconcileCasualties()
        {
            var manager = RetinueManager.Instance;
            if (manager?.State == null)
            {
                ClearSnapshot();
                return;
            }

            var roster = MobileParty.MainParty?.MemberRoster;
            if (roster == null)
            {
                ClearSnapshot();
                return;
            }

            var state = manager.State;
            var totalCasualties = 0;
            var casualtyLog = new List<string>();

            // Compare each tracked troop type against actual roster counts
            foreach (var kvp in _preBattleCounts.ToList())
            {
                var troopId = kvp.Key;
                var preBattleCount = kvp.Value;

                var character = CharacterObject.Find(troopId);
                if (character == null)
                {
                    ModLogger.Warn(LogCategory, $"Could not find character for troop ID: {troopId}");
                    continue;
                }

                // Get the actual count from the roster (source of truth after battle)
                var actualRosterCount = roster.GetTroopCount(character);

                // Calculate losses based on pre-battle vs actual roster
                // Note: The roster is the source of truth after battle
                var casualties = preBattleCount - actualRosterCount;

                if (casualties > 0)
                {
                    // Update our tracking to match roster
                    state.UpdateTroopCount(troopId, -casualties);
                    totalCasualties += casualties;
                    casualtyLog.Add($"{casualties}x {character.Name}");

                    ModLogger.Debug(LogCategory,
                        $"Casualty: {casualties}x {character.Name} (pre: {preBattleCount}, post: {actualRosterCount})");
                }
                else if (casualties < 0)
                {
                    // Somehow we gained troops? This shouldn't happen but log it
                    ModLogger.Warn(LogCategory,
                        $"Unexpected troop gain: {character.Name} pre={preBattleCount}, post={actualRosterCount}");
                }
            }

            // Clean up entries with zero troops
            CleanupZeroCountEntries(state);

            // Log summary
            if (totalCasualties > 0)
            {
                ModLogger.Info(LogCategory,
                    $"Battle casualties: {totalCasualties} soldiers lost ({string.Join(", ", casualtyLog)}), " +
                    $"{state.TotalSoldiers} remaining");
            }
            else
            {
                ModLogger.Debug(LogCategory, $"Battle ended with no retinue casualties, {state.TotalSoldiers} remaining");
            }

            ClearSnapshot();
        }

        /// <summary>
        /// Removes entries from TroopCounts that have zero soldiers.
        /// </summary>
        private static void CleanupZeroCountEntries(RetinueState state)
        {
            if (state?.TroopCounts == null)
            {
                return;
            }

            var toRemove = state.TroopCounts
                .Where(kvp => kvp.Value <= 0)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in toRemove)
            {
                state.TroopCounts.Remove(key);
            }

            if (toRemove.Count > 0)
            {
                ModLogger.Debug(LogCategory, $"Cleaned up {toRemove.Count} empty troop entries");
            }
        }

        /// <summary>
        /// Clears the pre-battle snapshot.
        /// </summary>
        private void ClearSnapshot()
        {
            _preBattleCounts?.Clear();
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Checks if the player is involved in a map event.
        /// </summary>
        private static bool IsPlayerInvolvedInMapEvent(MapEvent mapEvent)
        {
            if (mapEvent == null)
            {
                return false;
            }

            // Check if player's party is directly involved
            var mainParty = PartyBase.MainParty;
            if (mainParty == null)
            {
                return false;
            }

            // Check if main party is in the event
            if (mapEvent.IsPlayerMapEvent)
            {
                return true;
            }

            // Check if player's enlisted lord's party is in the event
            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment is { IsEnlisted: true, CurrentLord.PartyBelongedTo: not null })
            {
                var lordParty = enlistment.CurrentLord.PartyBelongedTo.Party;

                // Check attacker side
                if (mapEvent.AttackerSide?.Parties != null)
                {
                    foreach (var eventParty in mapEvent.AttackerSide.Parties)
                    {
                        if (eventParty?.Party == lordParty || eventParty?.Party == mainParty)
                        {
                            return true;
                        }
                    }
                }

                // Check defender side
                if (mapEvent.DefenderSide?.Parties != null)
                {
                    foreach (var eventParty in mapEvent.DefenderSide.Parties)
                    {
                        if (eventParty?.Party == lordParty || eventParty?.Party == mainParty)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Gets the total casualties from the last battle (for UI display).
        /// </summary>
        public int GetLastBattleCasualties()
        {
            // This would require storing the last battle's casualties
            // For now, return 0 as the casualties are logged but not stored
            return 0;
        }

        /// <summary>
        /// Force-syncs the retinue state with the actual roster.
        /// Use this if tracking gets out of sync for any reason.
        /// </summary>
        public void ForceSyncWithRoster()
        {
            var manager = RetinueManager.Instance;
            if (manager?.State == null)
            {
                return;
            }

            var roster = MobileParty.MainParty?.MemberRoster;
            if (roster == null)
            {
                return;
            }

            var state = manager.State;
            var corrections = 0;

            foreach (var troopId in state.TroopCounts.Keys.ToList())
            {
                var character = CharacterObject.Find(troopId);
                if (character == null)
                {
                    continue;
                }

                var actualCount = roster.GetTroopCount(character);
                var trackedCount = state.GetTroopCount(troopId);

                if (actualCount != trackedCount)
                {
                    var delta = actualCount - trackedCount;
                    state.UpdateTroopCount(troopId, delta);
                    corrections++;

                    ModLogger.Debug(LogCategory,
                        $"Sync correction: {character.Name} tracked={trackedCount}, actual={actualCount}");
                }
            }

            if (corrections > 0)
            {
                ModLogger.Info(LogCategory, $"Force sync completed with {corrections} corrections");
            }
        }

        #endregion
    }
}

