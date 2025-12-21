using System;
using Enlisted.Features.Retinue.Core;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Mod.Core.Logging;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace Enlisted.Features.Retinue.Systems
{
    /// <summary>
    /// Handles the free, slow replenishment of retinue soldiers via daily tick.
    /// Every 2-3 days, adds one soldier to the player's retinue at no cost.
    /// 
    /// V2.0: Now requires Commander rank (T7+) for trickle to activate.
    /// Recruits match player's formation and lord's culture.
    /// Includes overfill protection for both tier capacity and party size limits.
    /// </summary>
    public sealed class RetinueTrickleSystem : CampaignBehaviorBase
    {
        private const string LogCategory = "Trickle";

        // Configurable trickle parameters
        private const int TrickleMinDays = 2;
        private const int TrickleMaxDays = 3;
        private const int SoldiersPerTrickle = 1;

        // Random interval for this session (set once per eligibility)
        private int _currentTrickleInterval;

        public static RetinueTrickleSystem Instance { get; private set; }

        public RetinueTrickleSystem()
        {
            Instance = this;
        }

        public override void RegisterEvents()
        {
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
            ModLogger.Debug(LogCategory, "RetinueTrickleSystem registered for daily tick");
        }

        public override void SyncData(IDataStore dataStore)
        {
            // Trickle state is stored in RetinueState, managed by RetinueManager
            // No additional serialization needed here
        }

        /// <summary>
        /// Daily tick handler - checks eligibility and potentially adds a soldier.
        /// </summary>
        private void OnDailyTick()
        {
            try
            {
                if (!CanTrickleReplenish(out var reason))
                {
                    // Only log if there's a specific blocking reason (not just "no type selected")
                    if (!string.IsNullOrEmpty(reason) && !reason.Contains("no type"))
                    {
                        ModLogger.Debug(LogCategory, $"Trickle skipped: {reason}");
                    }
                    return;
                }

                var manager = RetinueManager.Instance;
                var state = manager?.State;
                if (state == null)
                {
                    return;
                }

                // Increment days since last trickle
                state.DaysSinceLastTrickle++;

                // Check if we've reached the trickle interval
                if (state.DaysSinceLastTrickle < GetTrickleInterval())
                {
                    ModLogger.Debug(LogCategory,
                        $"Trickle waiting: day {state.DaysSinceLastTrickle}/{GetTrickleInterval()}");
                    return;
                }

                // Reset counter and attempt to add a soldier
                state.DaysSinceLastTrickle = 0;
                RollNewTrickleInterval();

                // Calculate safe add count with overfill protection
                var safeCount = CalculateSafeAddCount(SoldiersPerTrickle);
                if (safeCount <= 0)
                {
                    ModLogger.Debug(LogCategory, "Trickle skipped: already at capacity");
                    return;
                }

                // Add the soldier
                if (manager.TryAddSoldiers(safeCount, state.SelectedTypeId, out var added, out var message))
                {
                    var currentCount = state.TotalSoldiers;
                    var enlistment = EnlistmentBehavior.Instance;
                    var tierCapacity = RetinueManager.GetTierCapacity(enlistment?.EnlistmentTier ?? RetinueManager.CommanderTier1);

                    ModLogger.Info(LogCategory,
                        $"Trickle added {added} soldier ({currentCount}/{tierCapacity})");

                    // Show subtle notification to player
                    ShowTrickleNotification();
                }
                else
                {
                    ModLogger.Debug(LogCategory, $"Trickle failed: {message}");
                }
            }
            catch (Exception ex)
            {
                ModLogger.ErrorCode(LogCategory, "E-RETINUE-001", "Error in daily trickle tick", ex);
            }
        }

        /// <summary>
        /// Checks if trickle replenishment can occur.
        /// V2.0: Requires Commander rank (T7+) for retinue trickle.
        /// </summary>
        /// <param name="reason">Out: reason if cannot trickle</param>
        /// <returns>True if trickle is allowed</returns>
        private static bool CanTrickleReplenish(out string reason)
        {
            reason = null;

            // Must be enlisted at Commander tier (T7+)
            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment == null || !enlistment.IsEnlisted)
            {
                reason = "not enlisted";
                return false;
            }

            if (enlistment.EnlistmentTier < RetinueManager.CommanderTier1)
            {
                reason = $"tier {enlistment.EnlistmentTier} < {RetinueManager.CommanderTier1} (Commander rank required)";
                return false;
            }

            // Must have a retinue type selected
            var manager = RetinueManager.Instance;
            if (manager?.State == null)
            {
                reason = "no retinue manager";
                return false;
            }

            if (!manager.State.HasTypeSelected)
            {
                reason = "no type selected";
                return false;
            }

            // Must have room for more soldiers (tier capacity)
            var tierCapacity = RetinueManager.GetTierCapacity(enlistment.EnlistmentTier);
            var currentSoldiers = manager.State.TotalSoldiers;
            if (currentSoldiers >= tierCapacity)
            {
                reason = $"tier capacity full ({currentSoldiers}/{tierCapacity})";
                return false;
            }

            // Must have party space available
            var party = PartyBase.MainParty;
            if (party == null)
            {
                reason = "no party";
                return false;
            }

            var partySpace = party.PartySizeLimit - party.NumberOfAllMembers;
            if (partySpace <= 0)
            {
                reason = "party full";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Calculates how many soldiers can safely be added without exceeding limits.
        /// Uses the shared capacity check: Math.Min(tierCapacity - current, partyLimit - partyMembers)
        /// </summary>
        /// <param name="requested">Number of soldiers requested</param>
        /// <returns>Safe number to add (0 or more)</returns>
        private static int CalculateSafeAddCount(int requested)
        {
            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment == null)
            {
                return 0;
            }

            var manager = RetinueManager.Instance;
            if (manager?.State == null)
            {
                return 0;
            }

            // Tier capacity check
            var tierCapacity = RetinueManager.GetTierCapacity(enlistment.EnlistmentTier);
            var currentSoldiers = manager.State.TotalSoldiers;
            var tierAvailable = tierCapacity - currentSoldiers;

            // Party size check
            var party = PartyBase.MainParty;
            if (party == null)
            {
                return 0;
            }

            var partySpace = party.PartySizeLimit - party.NumberOfAllMembers;

            // Take the more restrictive limit
            var maxCanAdd = Math.Min(tierAvailable, partySpace);
            var safeCount = Math.Min(requested, maxCanAdd);

            if (safeCount < requested)
            {
                ModLogger.Debug(LogCategory,
                    $"Overfill protection: requested {requested}, allowing {safeCount} " +
                    $"(tier: {tierAvailable}, party: {partySpace})");
            }

            return Math.Max(0, safeCount);
        }

        /// <summary>
        /// Gets the current trickle interval in days. Rolls a new interval if not set.
        /// </summary>
        private int GetTrickleInterval()
        {
            if (_currentTrickleInterval < TrickleMinDays)
            {
                RollNewTrickleInterval();
            }
            return _currentTrickleInterval;
        }

        /// <summary>
        /// Rolls a new random trickle interval between min and max days.
        /// </summary>
        private void RollNewTrickleInterval()
        {
            // MBRandom is Bannerlord's random generator, thread-safe and seeded appropriately
            _currentTrickleInterval = MBRandom.RandomInt(TrickleMinDays, TrickleMaxDays + 1);
            ModLogger.Debug(LogCategory, $"New trickle interval: {_currentTrickleInterval} days");
        }

        /// <summary>
        /// Shows a subtle notification when a soldier is added via trickle.
        /// </summary>
        private static void ShowTrickleNotification()
        {
            var msg = new TextObject("{=ct_trickle_added}A new soldier has reported for duty.");
            MBInformationManager.AddQuickInformation(msg, soundEventPath: "event:/ui/notification/quest_update");
        }
    }
}

