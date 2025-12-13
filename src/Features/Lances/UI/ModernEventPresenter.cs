using System;
using Enlisted.Features.Assignments.Core;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Features.Lances.Events;
using Enlisted.Mod.Core.Logging;
using TaleWorlds.CampaignSystem;

namespace Enlisted.Features.Lances.UI
{
    /// <summary>
    /// Modern event presenter that uses custom Gauntlet UI instead of basic inquiry popups.
    /// Drop-in replacement for LanceLifeEventInquiryPresenter with improved visuals.
    /// </summary>
    public static class ModernEventPresenter
    {
        private const string LogCategory = "LanceLifeUI";

        // Global flag to prevent multiple events from stacking
        private static bool _isEventShowing;

        /// <summary>
        /// Returns true if an event is currently being displayed.
        /// </summary>
        public static bool IsEventShowing => _isEventShowing;

        /// <summary>
        /// Show an event using the modern custom UI screen.
        /// This is the main entry point to replace ShowInquiry calls.
        /// </summary>
        public static bool TryShow(LanceLifeEventDefinition evt, EnlistmentBehavior enlistment)
        {
            try
            {
                // Check if events are enabled
                var cfg = ConfigurationManager.LoadLanceLifeEventsConfig() ?? new LanceLifeEventsConfig();
                if (!cfg.Enabled)
                {
                    return false;
                }

                if (evt == null)
                {
                    ModLogger.Warn(LogCategory, "Cannot show event - event definition is null");
                    return false;
                }

                // Prevent multiple popups from stacking
                if (_isEventShowing)
                {
                    ModLogger.Info(LogCategory, $"Skipping event {evt.Id} - another event is already showing");
                    return false;
                }

                // Wait for bag check to complete
                if (enlistment?.IsBagCheckPending == true)
                {
                    ModLogger.Info(LogCategory, $"Deferring event {evt.Id} - bag check pending");
                    return false;
                }

                // Ensure we're in a valid campaign
                if (Campaign.Current == null)
                {
                    return false;
                }

                // Mark as showing
                _isEventShowing = true;

                // Open the modern event screen
                LanceLifeEventScreen.Open(evt, enlistment, onClosed: () =>
                {
                    _isEventShowing = false;
                    ModLogger.Debug(LogCategory, $"Event screen closed: {evt.Id}");
                });

                ModLogger.Info(LogCategory, $"Opened modern event screen: {evt.Id} (category: {evt.Category})");
                return true;
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, $"Error showing modern event screen: {ex.Message}", ex);
                _isEventShowing = false;
                return false;
            }
        }

        /// <summary>
        /// Fallback to basic inquiry popup if modern UI fails or is disabled.
        /// </summary>
        public static bool TryShowFallback(LanceLifeEventDefinition evt, EnlistmentBehavior enlistment)
        {
            // Use the original inquiry presenter as fallback
            return Events.LanceLifeEventInquiryPresenter.TryShow(evt, enlistment);
        }

        /// <summary>
        /// Try modern UI first, fallback to inquiry if it fails.
        /// </summary>
        public static bool TryShowWithFallback(LanceLifeEventDefinition evt, EnlistmentBehavior enlistment, bool useModernUI = true)
        {
            if (useModernUI)
            {
                var success = TryShow(evt, enlistment);
                if (success)
                {
                    return true;
                }

                // Fallback to basic inquiry if modern UI failed
                ModLogger.Warn(LogCategory, $"Modern UI failed for event {evt?.Id}, falling back to inquiry popup");
            }

            return TryShowFallback(evt, enlistment);
        }
    }
}
