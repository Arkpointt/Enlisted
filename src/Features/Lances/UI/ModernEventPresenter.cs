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
        private static double _eventShowingStartHour = -1.0;

        /// <summary>
        /// Returns true if an event is currently being displayed.
        /// Includes safety timeout to prevent permanent blocking.
        /// </summary>
        public static bool IsEventShowing
        {
            get
            {
                // Safety: if the flag has been set for more than 30 minutes (game time), reset it
                // This prevents permanent blocking if something goes wrong
                if (_isEventShowing && Campaign.Current != null && _eventShowingStartHour >= 0)
                {
                    try
                    {
                        var elapsedHours = CampaignTime.Now.ToHours - _eventShowingStartHour;
                        if (elapsedHours > 0.5) // 30 minutes game time
                        {
                            ModLogger.Warn(LogCategory, "Event showing flag timeout - resetting to prevent permanent block");
                            _isEventShowing = false;
                            _eventShowingStartHour = -1.0;
                        }
                    }
                    catch
                    {
                        // If time comparison fails, reset to be safe
                        _isEventShowing = false;
                        _eventShowingStartHour = -1.0;
                    }
                }
                return _isEventShowing;
            }
        }

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

                ModLogger.Info(LogCategory, $"Attempting to open modern event screen: {evt.Id} (category: {evt.Category})");

                // Set flag immediately to prevent double-queuing during the same tick
                _isEventShowing = true;
                _eventShowingStartHour = CampaignTime.Now.ToHours;

                // Open the modern event screen (deferred to next frame)
                LanceLifeEventScreen.Open(evt, enlistment, onClosed: () =>
                {
                    _isEventShowing = false;
                    _eventShowingStartHour = -1.0;
                    ModLogger.Debug(LogCategory, $"Event screen closed: {evt.Id}");
                });

                ModLogger.Info(LogCategory, $"Modern event screen queued successfully: {evt.Id}");
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
