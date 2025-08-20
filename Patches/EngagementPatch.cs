using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Library;
using Enlisted.Behaviors;

namespace Enlisted.Patches
{
    /// <summary>
    /// Third layer of protection - preventing mobile parties from engaging the player directly.
    /// This patches the core method that determines if a mobile party should interact with another party.
    /// Based on the "Serve as a Soldier" mod's comprehensive approach.
    /// </summary>
    [HarmonyPatch(typeof(MobileParty), "get_IsCurrentlyEngagingParty")]
    public static class MobilePartyEngagementPatch
    {
        /// <summary>
        /// Postfix patch that prevents hostile parties from targeting the enlisted player.
        /// This works by modifying the engagement status after the original method runs.
        /// </summary>
        /// <param name="__instance">The mobile party instance</param>
        /// <param name="__result">The original result of IsCurrentlyEngagingParty</param>
        public static void Postfix(MobileParty __instance, ref bool __result)
        {
            try
            {
                // Only process if the party was originally going to engage
                if (!__result) return;

                // Get enlistment status
                var enlistmentBehavior = EnlistmentBehavior.Instance;
                if (enlistmentBehavior == null || !enlistmentBehavior.IsEnlisted) return;

                // Check if this party is trying to engage the main party
                if (__instance.ShortTermTargetParty != MobileParty.MainParty) return;

                // Check if this is a hostile party that should be blocked
                if (BanditEncounterPatch.IsHostileToEnlistedPlayer(__instance.Party, enlistmentBehavior.Commander))
                {
                    // Block the engagement
                    __result = false;
                    
                    // Optional: Clear their target so they stop trying
                    if (__instance.Ai != null)
                    {
                        // Make them hold position instead of pursuing
                        __instance.Ai.SetMoveModeHold();
                    }
                }
            }
            catch (System.Exception ex)
            {
                // If anything goes wrong, don't break the game
                InformationManager.DisplayMessage(new InformationMessage(
                    $"[Enlisted] Error in MobilePartyEngagementPatch: {ex.Message}", Colors.Red));
            }
        }
    }

    /// <summary>
    /// Additional protection targeting the AI behavior that makes parties pursue targets.
    /// This prevents hostile parties from even attempting to chase the enlisted player.
    /// </summary>
    [HarmonyPatch(typeof(MobileParty), "OnPartyInteraction")]
    public static class PartyInteractionPatch
    {
        /// <summary>
        /// Prefix patch that prevents hostile party interactions when enlisted.
        /// </summary>
        /// <param name="__instance">The party being interacted with</param>
        /// <param name="mobileParty">The party initiating the interaction</param>
        /// <returns>False to cancel interaction, true to proceed</returns>
        public static bool Prefix(MobileParty __instance, MobileParty mobileParty)
        {
            try
            {
                // Get enlistment status
                var enlistmentBehavior = EnlistmentBehavior.Instance;
                if (enlistmentBehavior == null || !enlistmentBehavior.IsEnlisted) return true;

                // Check if this involves the main party being targeted
                if (__instance != MobileParty.MainParty) return true;

                // Check if the interacting party is hostile
                if (BanditEncounterPatch.IsHostileToEnlistedPlayer(mobileParty.Party, enlistmentBehavior.Commander))
                {
                    // Block the interaction
                    return false;
                }

                return true;
            }
            catch (System.Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    $"[Enlisted] Error in PartyInteractionPatch: {ex.Message}", Colors.Red));
                return true;
            }
        }
    }
}