using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Mod.Core.Logging;
using Enlisted.Mod.Core.Util;
using HarmonyLib;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.GameMenus;

namespace Enlisted.Mod.GameAdapters.Patches
{
    /// <summary>
    /// Prevents enlisted soldiers from seeing the native "Abandon army" options
    /// in the army_wait and raiding_village menus. Applied during normal patching.
    /// 
    /// Note: EncounterGameMenuBehavior patches are in a separate deferred class
    /// because that class has static fields that cause TypeInitializationException
    /// if patched before the localization system is ready.
    /// </summary>
    [HarmonyPatch]
    internal static class AbandonArmyBlockPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayerArmyWaitBehavior), "wait_menu_army_abandon_on_condition")]
        [UsedImplicitly]
        private static bool HideArmyWaitAbandon(ref bool __result)
        {
            if (IsPlayerEnlisted())
            {
                __result = false;
                ModLogger.Debug("Encounter", "Hid army_wait abandon army for enlisted player");
                return false;
            }

            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(VillageHostileActionCampaignBehavior),
            "wait_menu_end_raiding_at_army_by_abandoning_on_condition")]
        [UsedImplicitly]
        private static bool HideRaidAbandon(ref bool __result)
        {
            if (IsPlayerEnlisted())
            {
                __result = false;
                ModLogger.Debug("Encounter", "Hid raid abandon army for enlisted player");
                return false;
            }

            return true;
        }

        private static bool IsPlayerEnlisted()
        {
            return EnlistmentBehavior.Instance?.IsEnlisted == true;
        }
    }

    /// <summary>
    /// Deferred patch for join_encounter abandon option.
    /// Must be applied after campaign starts because EncounterGameMenuBehavior
    /// has static fields that call GameTexts.FindText() before localization is ready.
    /// </summary>
    [HarmonyPatch(typeof(EncounterGameMenuBehavior), "game_menu_join_encounter_abandon_army_on_condition")]
    internal static class EncounterAbandonArmyBlockPatch
    {
        [HarmonyPrefix]
        [UsedImplicitly]
        public static bool Prefix(MenuCallbackArgs args, ref bool __result)
        {
            if (EnlistmentBehavior.Instance?.IsEnlisted == true)
            {
                __result = false;
                ModLogger.Debug("Encounter", "Hid join_encounter abandon army for enlisted player");
                return false;
            }

            return true;
        }
    }

    /// <summary>
    /// Deferred patch for encounter abandon option.
    /// Must be applied after campaign starts because EncounterGameMenuBehavior
    /// has static fields that call GameTexts.FindText() before localization is ready.
    /// </summary>
    [HarmonyPatch(typeof(EncounterGameMenuBehavior), "game_menu_encounter_abandon_army_on_condition")]
    internal static class EncounterAbandonArmyBlockPatch2
    {
        [HarmonyPrefix]
        [UsedImplicitly]
        public static bool Prefix(MenuCallbackArgs args, ref bool __result)
        {
            if (EnlistmentBehavior.Instance?.IsEnlisted == true)
            {
                __result = false;
                ModLogger.Debug("Encounter", "Hid encounter abandon army for enlisted player");
                return false;
            }

            return true;
        }
    }
}

