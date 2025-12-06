using Enlisted.Mod.Core.Logging;
using TaleWorlds.CampaignSystem;

namespace Enlisted.Mod.Core
{
    /// <summary>
    /// Central activation gate. When inactive, Enlisted systems should behave as no-ops.
    /// Only enlistment dialog/activation detection should run while inactive.
    /// </summary>
    public static class EnlistedActivation
    {
        private static bool _isActive;

        /// <summary>
        /// True only when enlistment is active (player enlisted or in enlistment flow).
        /// </summary>
        public static bool IsActive => _isActive;

        /// <summary>
        /// Set activation state explicitly.
        /// </summary>
        public static void SetActive(bool active, string reason = null)
        {
            _isActive = active;
            ModLogger.Info("Activation", $"Enlisted activation set to {active} ({reason ?? "unspecified"})");
        }

        /// <summary>
        /// Helper to sync activation state from current enlistment data.
        /// </summary>
        public static void SyncFromEnlistment(bool isEnlisted, string reason = null)
        {
            SetActive(isEnlisted, reason ?? "sync");
        }

        /// <summary>
        /// Fast guard for handlers: returns false when inactive to allow early exit.
        /// </summary>
        public static bool EnsureActive()
        {
            if (!_isActive)
            {
                ModLogger.LogOnce("activation_inactive", "Activation", "Enlisted inactive; skipping behavior/patch.");
                return false;
            }
            return true;
        }
    }
}

