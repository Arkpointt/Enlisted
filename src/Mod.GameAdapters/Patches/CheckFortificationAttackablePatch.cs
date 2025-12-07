using HarmonyLib;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using Enlisted.Mod.Core.Logging;
using Enlisted.Mod.Core;

namespace Enlisted.Mod.GameAdapters.Patches
{
    /// <summary>
    /// Harmony patch that guards against a vanilla null reference bug in CheckFortificationAttackableHonorably.
    /// 
    /// The vanilla method assumes PlayerEncounter.EncounterSettlement is always set when the town/castle
    /// menu is displayed, but our mod sometimes finishes PlayerEncounter on settlement entry to prevent
    /// InsideSettlement assertion failures. This creates a brief window where the menu refreshes but
    /// EncounterSettlement is null, causing a NullReferenceException when vanilla tries to access
    /// EncounterSettlement.MapFaction.
    /// 
    /// This patch safely disables the besiege option and skips the vanilla method when no settlement
    /// context exists, preventing the crash while maintaining correct menu behavior.
    /// </summary>
    [HarmonyPatch(typeof(EncounterGameMenuBehavior), "CheckFortificationAttackableHonorably")]
    public static class CheckFortificationAttackablePatch
    {
        /// <summary>
        /// Prefix that guards against null EncounterSettlement before vanilla code tries to access it.
        /// When settlement context is missing, we disable the option and skip the vanilla method entirely.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("ReSharper", "UnusedMember.Local", 
            Justification = "Called by Harmony via reflection")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("ReSharper", "InconsistentNaming", 
            Justification = "Harmony convention: parameter names must match original method")]
        private static bool Prefix(MenuCallbackArgs args)
        {
            // Skip when the mod is inactive; only protect when Enlisted systems are running
            if (!EnlistedActivation.IsActive)
            {
                return true; // allow vanilla logic untouched
            }

            // Guard against vanilla null reference bug when EncounterSettlement is null
            // This happens when PlayerEncounter is finished right as the menu refreshes
            if (PlayerEncounter.EncounterSettlement == null)
            {
                // Disable the besiege option since we have no settlement to evaluate
                // This is safer than leaving it enabled during the transient null window
                args.IsEnabled = false;
                
                ModLogger.Debug("EncounterPatch", 
                    "Guarded null EncounterSettlement in CheckFortificationAttackableHonorably - disabled besiege option");
                
                return false; // Skip vanilla method to prevent NullReferenceException
            }

            return true; // Settlement exists, let vanilla method run normally
        }
    }
}

