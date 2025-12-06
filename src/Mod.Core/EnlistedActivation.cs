using Enlisted.Mod.Core.Logging;

namespace Enlisted.Mod.Core
{
    /// <summary>
    ///     Central activation gate. When inactive, Enlisted systems should behave as no-ops.
    ///     Only enlistment dialog/activation detection should run while inactive.
    /// </summary>
    public static class EnlistedActivation
    {
        /// <summary>
        ///     True only when enlistment is active (player enlisted or in enlistment flow).
        /// </summary>
        public static bool IsActive { get; private set; }

        /// <summary>
        ///     Set activation state explicitly.
        /// </summary>
        public static void SetActive(bool active, string reason = null)
        {
            IsActive = active;
            ModLogger.Info("Activation", $"Enlisted activation set to {active} ({reason ?? "unspecified"})");
        }

        /// <summary>
        ///     Helper to sync activation state from current enlistment data.
        /// </summary>
        public static void SyncFromEnlistment(bool isEnlisted, string reason = null)
        {
            SetActive(isEnlisted, reason ?? "sync");
        }

        /// <summary>
        ///     Fast guard for handlers: returns false when inactive to allow early exit.
        /// </summary>
        public static bool EnsureActive()
        {
            if (!IsActive)
            {
                ModLogger.LogOnce("activation_inactive", "Activation", "Enlisted inactive; skipping behavior/patch.");
                return false;
            }

            return true;
        }
    }
}
