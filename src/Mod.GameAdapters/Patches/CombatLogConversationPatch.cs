using HarmonyLib;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.ScreenSystem;
using Enlisted.Features.Interface.Behaviors;
using Enlisted.Mod.Core.Logging;

namespace Enlisted.Mod.GameAdapters.Patches
{
    /// <summary>
    /// Patches MapScreen's conversation handling to suspend/resume the combat log layer.
    /// This hooks into the same conversation lifecycle that native MapViews use.
    /// </summary>
    public static class CombatLogConversationPatch
    {
        /// <summary>
        /// Patch the IMapStateHandler.OnMapConversationStarts implementation in MapScreen.
        /// This is called when a map conversation (portrait style) begins.
        /// </summary>
        [HarmonyPatch("SandBox.View.Map.MapScreen", "HandleMapConversationInit")]
        public static class MapConversationStartPatch
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                try
                {
                    EnlistedCombatLogBehavior.Instance?.SuspendLayer();
                    ModLogger.Debug("Interface", "Combat log suspended via MapScreen conversation patch");
                }
                catch (System.Exception ex)
                {
                    ModLogger.Error("Interface", "Error in CombatLogConversationPatch.Start", ex);
                }
            }
        }
        
        /// <summary>
        /// Patch the IMapStateHandler.OnMapConversationOver implementation in MapScreen.
        /// This is called when a map conversation ends.
        /// </summary>
        [HarmonyPatch("SandBox.View.Map.MapScreen", "OnMapConversationOver")]
        public static class MapConversationEndPatch
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                try
                {
                    EnlistedCombatLogBehavior.Instance?.ResumeLayer();
                    ModLogger.Debug("Interface", "Combat log resumed via MapScreen conversation patch");
                }
                catch (System.Exception ex)
                {
                    ModLogger.Error("Interface", "Error in CombatLogConversationPatch.End", ex);
                }
            }
        }
    }
}
