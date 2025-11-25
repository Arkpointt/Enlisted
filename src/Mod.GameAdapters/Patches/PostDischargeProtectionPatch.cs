using System;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Party;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Mod.Core.Logging;

namespace Enlisted.Mod.GameAdapters.Patches
{
    /// <summary>
    /// Prevents party activation after enlistment discharge when in a vulnerable state.
    /// This prevents crashes from encounters being created immediately after army defeat.
    /// When the lord's army is defeated, the player party is in a battle/encounter state,
    /// and activating it causes the game to create a "Attack or Surrender" encounter which crashes.
    /// This patch blocks activation until the battle/encounter state is fully cleared.
    /// </summary>
    [HarmonyPatch(typeof(MobileParty), "IsActive", MethodType.Setter)]
    public class PostDischargeProtectionPatch
    {
        /// <summary>
        /// Prefix that prevents activation if enlistment just ended and player is in battle state.
        /// Returns false to prevent activation, true to allow normal activation.
        /// </summary>
        static bool Prefix(MobileParty __instance, bool value)
        {
            try
            {
                var campaign = Campaign.Current;
                var mainPartyMobile = campaign?.MainParty;

                // Only check for main party once the campaign is initialized
                if (mainPartyMobile == null || __instance != mainPartyMobile)
                {
                    return true; // Allow normal activation for other parties
                }
                
                // Only check when trying to activate (value = true)
                if (!value)
                {
                    return true; // Always allow deactivation
                }
                
                var enlistment = EnlistmentBehavior.Instance;
                if (enlistment == null)
                {
                    return true;
                }

                // Only intervene when the player has just been discharged and is still in their grace/cleanup window.
                if (enlistment.IsEnlisted || !enlistment.IsInDesertionGracePeriod)
                {
                    return true;
                }

                // Check if player is in a vulnerable state (in battle/encounter after discharge)
                var mainParty = mainPartyMobile.Party;
                bool playerInMapEvent = mainParty?.MapEvent != null;

                bool playerInEncounter = campaign?.PlayerEncounter != null;
                bool inVulnerableState = playerInMapEvent || playerInEncounter;
                
                // If in vulnerable state after discharge, prevent activation
                if (inVulnerableState)
                {
                    ModLogger.Info("PostDischargeProtection", $"Prevented party activation - discharged but still in battle state (MapEvent: {playerInMapEvent}, Encounter: {playerInEncounter})");
                    return false; // Prevent activation - this will block IsActive = true
                }
                
                return true; // Allow activation - no vulnerable state detected
            }
            catch (Exception ex)
            {
                ModLogger.Error("PostDischargeProtection", $"Error in activation protection patch: {ex.Message}");
                return true; // Fail open - allow activation if we can't determine state
            }
        }
    }
}

