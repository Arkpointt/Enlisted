using System;
using System.Linq;
using Enlisted.Features.Content;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Features.Escalation;
using Enlisted.Mod.Core.Logging;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace Enlisted.Debugging.Behaviors
{
    /// <summary>
    ///     Lightweight in-game debug helpers for QA: grant gold and enlistment XP.
    ///     Keeps state changes minimal and logged for traceability.
    /// </summary>
    public static class DebugToolsBehavior
    {
        private const int GoldPerClick = 1000;
        private const int XpPerClick = 2000;

        public static void GiveGold()
        {
            var hero = Hero.MainHero;
            if (hero == null)
            {
                return;
            }

            hero.Gold += GoldPerClick;
            var msg = new TextObject("{=dbg_gold_added}+{G} gold granted (debug).");
            msg.SetTextVariable("G", GoldPerClick);
            InformationManager.DisplayMessage(new InformationMessage(msg.ToString()));
            SessionDiagnostics.LogEvent("Debug", "GiveGold", $"gold={GoldPerClick}, total={hero.Gold}");
        }

        public static void GiveEnlistmentXp()
        {
            var enlist = EnlistmentBehavior.Instance;
            if (enlist?.IsEnlisted != true)
            {
                var warn = new TextObject("{=dbg_xp_not_enlisted}Cannot grant XP while not enlisted.");
                InformationManager.DisplayMessage(new InformationMessage(warn.ToString()));
                return;
            }

            enlist.AddEnlistmentXP(XpPerClick, "Debug");
            var msg = new TextObject("{=dbg_xp_added}+{XP} enlistment XP granted (debug).");
            msg.SetTextVariable("XP", XpPerClick);
            InformationManager.DisplayMessage(new InformationMessage(msg.ToString()));
            SessionDiagnostics.LogEvent("Debug", "GiveXP",
                $"xp={XpPerClick}, total={enlist.EnlistmentXP}, tier={enlist.EnlistmentTier}");
        }

        /// <summary>
        /// Test the custom onboarding event screen with a sample event.
        /// Creates a test event and displays it using the modern Gauntlet UI.
        /// </summary>
        public static void TestOnboardingScreen()
        {
            var enlist = EnlistmentBehavior.Instance;
            if (enlist == null)
            {
                var warn = new TextObject("{=dbg_test_no_enlist}Cannot test event screen - EnlistmentBehavior not found.");
                InformationManager.DisplayMessage(new InformationMessage(warn.ToString()));
                ModLogger.Warn("Debug", "TestOnboardingScreen: EnlistmentBehavior.Instance is null");
                return;
            }

            // Event screen testing is currently inactive.
            ModLogger.Info("Debug", "TestOnboardingScreen: This feature is currently disabled.");
            return;

            /* Original test code has been removed. */
        }

        /// <summary>
        /// Forces an immediate event selection attempt, bypassing the pacing window.
        /// Useful for testing event selection and delivery without waiting 3-5 days.
        /// </summary>
        public static void ForceEventSelection()
        {
            var pacingManager = EventPacingManager.Instance;
            if (pacingManager == null)
            {
                var warn = new TextObject("Cannot force event - EventPacingManager not found.");
                InformationManager.DisplayMessage(new InformationMessage(warn.ToString()));
                ModLogger.Warn("Debug", "ForceEventSelection: EventPacingManager.Instance is null");
                return;
            }

            var enlist = EnlistmentBehavior.Instance;
            if (enlist?.IsEnlisted != true)
            {
                var warn = new TextObject("Cannot force event - not enlisted.");
                InformationManager.DisplayMessage(new InformationMessage(warn.ToString()));
                return;
            }

            pacingManager.ForceEventAttempt();
            var msg = new TextObject("Event selection forced (debug). Check if popup appears.");
            InformationManager.DisplayMessage(new InformationMessage(msg.ToString()));
            SessionDiagnostics.LogEvent("Debug", "ForceEventSelection", "Forced immediate event attempt");
        }

        /// <summary>
        /// Resets the event pacing window to now, allowing the next daily tick to fire an event.
        /// </summary>
        public static void ResetEventWindow()
        {
            var pacingManager = EventPacingManager.Instance;
            if (pacingManager == null)
            {
                var warn = new TextObject("Cannot reset window - EventPacingManager not found.");
                InformationManager.DisplayMessage(new InformationMessage(warn.ToString()));
                return;
            }

            pacingManager.ResetPacingWindow();
            var msg = new TextObject("Event window reset to now. Next daily tick will attempt event selection.");
            InformationManager.DisplayMessage(new InformationMessage(msg.ToString()));
            SessionDiagnostics.LogEvent("Debug", "ResetEventWindow", "Pacing window reset");
        }

        /// <summary>
        /// Lists all currently eligible events (meet requirements, not on cooldown).
        /// </summary>
        public static void ListEligibleEvents()
        {
            var eligibleCount = EventSelector.GetEligibleEventCount();
            var totalCount = EventCatalog.EventCount;

            var msg = new TextObject("{C} eligible events out of {T} total. Check log for details.");
            msg.SetTextVariable("C", eligibleCount);
            msg.SetTextVariable("T", totalCount);
            InformationManager.DisplayMessage(new InformationMessage(msg.ToString()));

            ModLogger.Info("Debug", $"Eligible events: {eligibleCount}/{totalCount}");
            SessionDiagnostics.LogEvent("Debug", "ListEligibleEvents", $"eligible={eligibleCount}, total={totalCount}");
        }

        /// <summary>
        /// Clears all event cooldowns, allowing any event to fire again.
        /// Useful for testing event variety without waiting for cooldowns.
        /// </summary>
        public static void ClearEventCooldowns()
        {
            var escalationState = EscalationManager.Instance?.State;
            if (escalationState == null)
            {
                var warn = new TextObject("Cannot clear cooldowns - EscalationManager not found.");
                InformationManager.DisplayMessage(new InformationMessage(warn.ToString()));
                return;
            }

            var clearedCount = escalationState.EventLastFired?.Count ?? 0;
            escalationState.EventLastFired?.Clear();

            var msg = new TextObject("Cleared {C} event cooldowns. All events can fire again.");
            msg.SetTextVariable("C", clearedCount);
            InformationManager.DisplayMessage(new InformationMessage(msg.ToString()));
            SessionDiagnostics.LogEvent("Debug", "ClearEventCooldowns", $"cleared={clearedCount}");
        }

        /// <summary>
        /// Shows information about the next event window and pacing state.
        /// </summary>
        public static void ShowEventPacingInfo()
        {
            var pacingManager = EventPacingManager.Instance;
            var escalationState = EscalationManager.Instance?.State;

            if (pacingManager == null || escalationState == null)
            {
                var warn = new TextObject("Cannot show pacing info - managers not found.");
                InformationManager.DisplayMessage(new InformationMessage(warn.ToString()));
                return;
            }

            var daysUntilNext = pacingManager.GetDaysUntilNextWindow();
            var lastEventTime = escalationState.LastNarrativeEventTime;
            var nextWindow = escalationState.NextNarrativeEventWindow;

            var msg = daysUntilNext >= 0
                ? new TextObject("Next event in {D} days. Last event: {L} days ago.")
                : new TextObject("Event window not initialized yet.");

            if (daysUntilNext >= 0)
            {
                msg.SetTextVariable("D", $"{daysUntilNext:F1}");
                var daysSinceLast = (CampaignTime.Now - lastEventTime).ToDays;
                msg.SetTextVariable("L", $"{daysSinceLast:F1}");
            }

            InformationManager.DisplayMessage(new InformationMessage(msg.ToString()));
            ModLogger.Info("Debug", $"Next event window: {daysUntilNext:F1} days, Last: {(CampaignTime.Now - lastEventTime).ToDays:F1} days ago");
        }

        /// <summary>
        /// Fires a specific event by ID, bypassing selection and requirements.
        /// Useful for testing specific event content.
        /// </summary>
        public static void FireSpecificEvent(string eventId)
        {
            if (string.IsNullOrEmpty(eventId))
            {
                var warn = new TextObject("No event ID provided. Usage: enlisted.fire_event <event_id>");
                InformationManager.DisplayMessage(new InformationMessage(warn.ToString()));
                return;
            }

            var evt = EventCatalog.GetEvent(eventId);
            if (evt == null)
            {
                var warn = new TextObject("Event '{ID}' not found in catalog.");
                warn.SetTextVariable("ID", eventId);
                InformationManager.DisplayMessage(new InformationMessage(warn.ToString()));
                return;
            }

            var deliveryManager = EventDeliveryManager.Instance;
            if (deliveryManager == null)
            {
                var warn = new TextObject("Cannot fire event - EventDeliveryManager not found.");
                InformationManager.DisplayMessage(new InformationMessage(warn.ToString()));
                return;
            }

            deliveryManager.QueueEvent(evt);
            var msg = new TextObject("Queued event '{ID}' for delivery.");
            msg.SetTextVariable("ID", eventId);
            InformationManager.DisplayMessage(new InformationMessage(msg.ToString()));
            SessionDiagnostics.LogEvent("Debug", "FireSpecificEvent", $"eventId={eventId}");
        }

        /// <summary>
        /// Triggers an immediate pay muster, bypassing the 12-day cycle.
        /// Useful for testing the muster system flow, pay options, inspections, and promotions.
        /// </summary>
        public static void TriggerMuster()
        {
            var enlist = EnlistmentBehavior.Instance;
            if (enlist?.IsEnlisted != true)
            {
                var warn = new TextObject("{=dbg_muster_not_enlisted}Cannot trigger muster while not enlisted.");
                InformationManager.DisplayMessage(new InformationMessage(warn.ToString()));
                ModLogger.Warn("Debug", "TriggerMuster: Player not enlisted");
                return;
            }

            // Find the MusterMenuHandler (registered as a campaign behavior)
            var musterHandler = Campaign.Current?.CampaignBehaviorManager?.GetBehavior<MusterMenuHandler>();
            if (musterHandler == null)
            {
                var error = new TextObject("{=dbg_muster_handler_missing}Cannot trigger muster - MusterMenuHandler not found.");
                InformationManager.DisplayMessage(new InformationMessage(error.ToString()));
                ModLogger.Error("Debug", "TriggerMuster: MusterMenuHandler not registered as campaign behavior");
                return;
            }

            // Call the muster handler directly to open the 8-stage muster sequence
            musterHandler.BeginMusterSequence();
            
            var msg = new TextObject("{=dbg_muster_started}Muster sequence started (debug). Opening intro stage...");
            InformationManager.DisplayMessage(new InformationMessage(msg.ToString()));
            SessionDiagnostics.LogEvent("Debug", "TriggerMuster", 
                $"tier={enlist.EnlistmentTier}, xp={enlist.EnlistmentXP}, pay_owed={enlist.PendingMusterPay}");
        }
    }
}

