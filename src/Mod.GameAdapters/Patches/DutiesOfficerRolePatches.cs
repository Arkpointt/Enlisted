using System;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using Enlisted.Features.Assignments.Behaviors;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Mod.Core.Logging;

namespace Enlisted.Mod.GameAdapters.Patches
{
    /// <summary>
    /// Optional enhancement patches for natural officer role integration.
    /// 
    /// These patches provide Option B from the design document - making the player's skills 
    /// naturally affect party performance when assigned to officer duties, without changing
    /// the official party officer assignments.
    /// 
    /// </summary>
    
    // Harmony Patch
    // Target: TaleWorlds.CampaignSystem.Party.MobileParty.EffectiveEngineer { get; }
    // Why: Make player the effective engineer when assigned to Siegewright's Aide duty for natural skill benefits
    // Safety: Campaign-only; checks enlistment state; validates duty assignment; only affects enlisted lord's party
    // Notes: Property getter patch; high priority to ensure our substitution runs first; part of duties system officer role integration
    [HarmonyPatch(typeof(MobileParty), "EffectiveEngineer", MethodType.Getter)]
    [HarmonyPriority(999)] // Run before other Harmony layers
    public class DutiesEffectiveEngineerPatch
    {
        static bool Prefix(MobileParty __instance, ref Hero __result)
        {
            try
            {
                // Guard: Verify all required objects exist
                if (EnlistmentBehavior.Instance?.IsEnlisted != true || 
                    __instance == null || 
                    EnlistmentBehavior.Instance.CurrentLord?.PartyBelongedTo != __instance)
                {
                    return true; // Use original behavior
                }
                
                // Guard: Verify duty assignment
                if (EnlistedDutiesBehavior.Instance?.HasActiveDutyWithRole("Engineer") != true)
                {
                    return true; // Use original behavior
                }
                
                // Substitute player as effective engineer
                __result = Hero.MainHero;
                return false; // Skip original method - player's Engineering skill now affects party
            }
            catch (Exception ex)
            {
                ModLogger.Error("Patches", $"EffectiveEngineer patch error: {ex.Message}");
                return true; // Fail safe - use original behavior
            }
        }
    }
    
    // Harmony Patch
    // Target: TaleWorlds.CampaignSystem.Party.MobileParty.EffectiveScout { get; }
    // Why: Make player the effective scout when assigned to Pathfinder duty for natural skill benefits
    // Safety: Campaign-only; checks enlistment state; validates duty assignment; only affects enlisted lord's party
    // Notes: Property getter patch; high priority so the substitution happens before downstream code
    [HarmonyPatch(typeof(MobileParty), "EffectiveScout", MethodType.Getter)]
    [HarmonyPriority(999)]
    public class DutiesEffectiveScoutPatch
    {
        static bool Prefix(MobileParty __instance, ref Hero __result)
        {
            try
            {
                if (EnlistmentBehavior.Instance?.IsEnlisted != true || 
                    __instance == null || 
                    EnlistmentBehavior.Instance.CurrentLord?.PartyBelongedTo != __instance)
                {
                    return true; // Use original behavior
                }
                
                if (EnlistedDutiesBehavior.Instance?.HasActiveDutyWithRole("Scout") != true)
                {
                    return true; // Use original behavior
                }
                
                __result = Hero.MainHero;
                return false; // Player's Scouting skill drives party speed/detection
            }
            catch (Exception ex)
            {
                ModLogger.Error("Patches", $"EffectiveScout patch error: {ex.Message}");
                return true; // Fail safe
            }
        }
    }
    
    // Harmony Patch
    // Target: TaleWorlds.CampaignSystem.Party.MobileParty.EffectiveQuartermaster { get; }
    // Why: Make player the effective quartermaster when assigned to Provisioner duty for natural skill benefits
    // Safety: Campaign-only; checks enlistment state; validates duty assignment; only affects enlisted lord's party  
    // Notes: Property getter patch; priority keeps the player's steward role in place before other logic checks the value
    [HarmonyPatch(typeof(MobileParty), "EffectiveQuartermaster", MethodType.Getter)]
    [HarmonyPriority(999)]
    public class DutiesEffectiveQuartermasterPatch
    {
        static bool Prefix(MobileParty __instance, ref Hero __result)
        {
            try
            {
                if (EnlistmentBehavior.Instance?.IsEnlisted != true || 
                    __instance == null || 
                    EnlistmentBehavior.Instance.CurrentLord?.PartyBelongedTo != __instance)
                {
                    return true; // Use original behavior
                }
                
                if (EnlistedDutiesBehavior.Instance?.HasActiveDutyWithRole("Quartermaster") != true)
                {
                    return true; // Use original behavior
                }
                
                __result = Hero.MainHero;
                return false; // Player's Steward skill drives carry capacity/efficiency
            }
            catch (Exception ex)
            {
                ModLogger.Error("Patches", $"EffectiveQuartermaster patch error: {ex.Message}");
                return true; // Fail safe
            }
        }
    }
    
    // Harmony Patch
    // Target: TaleWorlds.CampaignSystem.Party.MobileParty.EffectiveSurgeon { get; }
    // Why: Make player the effective surgeon when assigned to Field Medic duty for natural skill benefits
    // Safety: Campaign-only; checks enlistment state; validates duty assignment; only affects enlisted lord's party
    // Notes: Property getter patch; priority ensures the player's medicine skill is used whenever this getter runs
    [HarmonyPatch(typeof(MobileParty), "EffectiveSurgeon", MethodType.Getter)]
    [HarmonyPriority(999)]
    public class DutiesEffectiveSurgeonPatch
    {
        static bool Prefix(MobileParty __instance, ref Hero __result)
        {
            try
            {
                if (EnlistmentBehavior.Instance?.IsEnlisted != true || 
                    __instance == null || 
                    EnlistmentBehavior.Instance.CurrentLord?.PartyBelongedTo != __instance)
                {
                    return true; // Use original behavior
                }
                
                if (EnlistedDutiesBehavior.Instance?.HasActiveDutyWithRole("Surgeon") != true)
                {
                    return true; // Use original behavior
                }
                
                __result = Hero.MainHero;
                return false; // Player's Medicine skill drives party healing
            }
            catch (Exception ex)
            {
                ModLogger.Error("Patches", $"EffectiveSurgeon patch error: {ex.Message}");
                return true; // Fail safe
            }
        }
    }
}
