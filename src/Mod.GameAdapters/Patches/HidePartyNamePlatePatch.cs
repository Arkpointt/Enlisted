using System;
using System.Reflection;
using HarmonyLib;
using SandBox.ViewModelCollection.Nameplate;
using TaleWorlds.CampaignSystem.Party;
using Enlisted.Features.Enlistment.Application;
using Enlisted.Mod.Core.Logging;

namespace Enlisted.Mod.GameAdapters.Patches
{
    // Harmony Patch
    // Target: SandBox.ViewModelCollection.Nameplate.PartyNameplateVM.RefreshBinding()
    // Why: Hide main party nameplate/shield while enlisted to maintain illusion of attachment to commander
    // Safety: Campaign/UI only; affect only MainParty; exit immediately when not enlisted; full null checks
    // Notes: Uses reflection to set IsMainParty/IsVisibleOnMap if present; avoids hard dependency on specific game build
    [HarmonyPatch]
    internal static class HidePartyNamePlatePatch
    {
        private static bool _loggedHideOnce;
        private static DateTime _lastResetCheckUtc;

        public static MethodBase TargetMethod()
        {
            try
            {
                var vmType = typeof(PartyNameplateVM);
                var method = AccessTools.Method(vmType, "RefreshBinding");
                if (method == null)
                {
                    LoggingService.Info("NameplatePatch", "PartyNameplateVM.RefreshBinding not found; patch will not apply.");
                }
                else
                {
                    LoggingService.Info("NameplatePatch", "Patching PartyNameplateVM.RefreshBinding for main party nameplate hide.");
                }
                return method;
            }
            catch (Exception ex)
            {
                LoggingService.Exception("NameplatePatch", ex, "Resolve TargetMethod");
                return null;
            }
        }

        [HarmonyPostfix]
        private static void Postfix(PartyNameplateVM __instance)
        {
            try
            {
                // Reset one-shot log flag when not enlisted (rate-limited)
                if (!EnlistmentBehavior.IsPlayerEnlisted)
                {
                    var now = DateTime.UtcNow;
                    if ((now - _lastResetCheckUtc).TotalSeconds > 5)
                    {
                        _loggedHideOnce = false;
                        _lastResetCheckUtc = now;
                    }
                    return;
                }

                if (__instance == null)
                {
                    return;
                }

                // Only affect the player's main party nameplate
                if (__instance.Party == MobileParty.MainParty)
                {
                    // Use reflection to avoid hard dependency on specific property names
                    // Prefer setting IsMainParty=false and IsVisibleOnMap=false when available
                    var type = __instance.GetType();
                    try
                    {
                        var isMainProp = type.GetProperty("IsMainParty", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        isMainProp?.SetValue(__instance, false);
                    }
                    catch { }

                    try
                    {
                        var isVisibleProp = type.GetProperty("IsVisibleOnMap", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        isVisibleProp?.SetValue(__instance, false);
                    }
                    catch { }

                    if (!_loggedHideOnce)
                    {
                        LoggingService.Debug("NameplatePatch", "Hidden main party nameplate while enlisted");
                        _loggedHideOnce = true; // log only once per enlistment session
                    }
                }
            }
            catch (Exception ex)
            {
                // UI-only safeguard
                LoggingService.Exception("NameplatePatch", ex, "Postfix");
            }
        }
    }
}


