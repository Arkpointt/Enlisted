using System;
using Enlisted.Features.Assignments.Core;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Mod.Core.Logging;

namespace Enlisted.Features.Lances.Events
{
    /// <summary>
    /// Phase 1 runtime entry point for showing a specific Lance Life Event by ID.
    ///
    /// Note: Phase 1 does not schedule events. This is intentionally "pull-based" so we can
    /// wire it into menus/triggers later without changing presentation/effect code.
    /// </summary>
    internal static class LanceLifeEventRuntime
    {
        private const string LogCategory = "LanceLifeEvents";

        private static LanceLifeEventCatalog _cachedCatalog;

        public static bool IsEnabled()
        {
            return ConfigurationManager.LoadLanceLifeEventsConfig()?.Enabled == true;
        }

        public static LanceLifeEventCatalog GetCatalog()
        {
            _cachedCatalog ??= LanceLifeEventCatalogLoader.LoadCatalog();
            return _cachedCatalog;
        }

        public static void RefreshCatalog()
        {
            _cachedCatalog = null;
        }

        public static bool TryShowEventById(string eventId)
        {
            try
            {
                if (!IsEnabled())
                {
                    return false;
                }

                var enlistment = EnlistmentBehavior.Instance;
                if (enlistment?.IsEnlisted != true)
                {
                    return false;
                }

                var catalog = GetCatalog();
                var evt = catalog?.FindById(eventId);
                if (evt == null)
                {
                    ModLogger.Warn(LogCategory, $"Event not found: {eventId}");
                    return false;
                }

                return LanceLifeEventInquiryPresenter.TryShow(evt, enlistment);
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, "TryShowEventById failed", ex);
                return false;
            }
        }
    }
}


