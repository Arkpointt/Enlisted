using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Mod.Core.Logging;
using HarmonyLib;
using TaleWorlds.CampaignSystem.GameMenus;

namespace Enlisted.Mod.GameAdapters.Patches
{
    /// <summary>
    /// Prevents enlisted soldiers from seeing the native "Abandon army" options
    /// in encounter-related menus. When enlisted, we short-circuit the native
    /// on_condition callbacks so the option is hidden entirely.
    /// </summary>
    [HarmonyPatch]
    internal static class AbandonArmyBlockPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(TaleWorlds.CampaignSystem.CampaignBehaviors.EncounterGameMenuBehavior),
            "game_menu_join_encounter_abandon_army_on_condition")]
        private static bool HideJoinEncounterAbandon(MenuCallbackArgs args, ref bool __result)
        {
            if (IsPlayerEnlisted())
            {
                __result = false; // option is removed from the menu
                ModLogger.Debug("Encounter", "Hid join_encounter abandon army for enlisted player");
                return false;
            }

            return true; // fall through to native condition
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(TaleWorlds.CampaignSystem.CampaignBehaviors.EncounterGameMenuBehavior),
            "game_menu_encounter_abandon_army_on_condition")]
        private static bool HideEncounterAbandon(MenuCallbackArgs args, ref bool __result)
        {
            if (IsPlayerEnlisted())
            {
                __result = false; // option is removed from the menu
                ModLogger.Debug("Encounter", "Hid encounter abandon army for enlisted player");
                return false;
            }

            return true; // fall through to native condition
        }

        private static bool IsPlayerEnlisted()
        {
            return EnlistmentBehavior.Instance?.IsEnlisted == true;
        }
    }
}

