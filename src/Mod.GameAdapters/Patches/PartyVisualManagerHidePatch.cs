using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem.Party;
using Enlisted.Mod.Core.Logging;

namespace Enlisted.Mod.GameAdapters.Patches
{
    // Prevent the game from (re)creating the MainParty's map visual while enlisted
    // This mirrors SAS-style behavior where the player's party appears visually merged with the commander
    [HarmonyPatch]
    internal static class PartyVisualManager_HideMainParty_Patch
    {
        private static List<MethodBase> _targets;

        static bool Prepare()
        {
            _targets = ResolveTargets();
            if (_targets.Count == 0)
            {
                LoggingService.Info("PartyVisualHidePatch", "No PartyVisualManager add/create methods found; hide patch will not apply.");
                return false;
            }
            LoggingService.Info("PartyVisualHidePatch", $"Patching PartyVisualManager methods: {string.Join(", ", _targets.Select(m => m.DeclaringType?.Name + "." + m.Name))}");
            return true;
        }

        static IEnumerable<MethodBase> TargetMethods() => _targets ?? (IEnumerable<MethodBase>)ResolveTargets();

        private static List<MethodBase> ResolveTargets()
        {
            var found = new List<MethodBase>();
            // Known/likely PartyVisualManager type names across versions
            var typeNames = new[]
            {
                "SandBox.View.Map.PartyVisualManager",
                "SandBox.View.PartyVisualManager",
                "SandBox.ViewModelCollection.Map.PartyVisualManager"
            };
            // Methods that create/add/recreate visuals
            var methodNames = new[]
            {
                "AddVisualOfParty",
                "AddParty",
                "CreatePartyVisuals",
                "OnMobilePartyCreated",
                "OnPartyCreated",
                "RegisterParty",
                // extra catches
                "RefreshParty",
                "RefreshVisualOfParty",
                "OnMobilePartyRemoved",
                "OnPartyRemoved"
            };
            foreach (var tn in typeNames)
            {
                var t = AccessTools.TypeByName(tn);
                if (t == null) continue;
                foreach (var mn in methodNames)
                {
                    var m = AccessTools.Method(t, mn);
                    if (m != null) found.Add(m);
                }
            }
            return found;
        }

        // Prefix: if this is about MainParty and player is enlisted, skip visual creation
        static bool Prefix(object __instance, params object[] __args)
        {
            try
            {
                if (!Enlisted.Features.Enlistment.Application.EnlistmentBehavior.IsPlayerEnlisted)
                {
                    return true;
                }
                // Try to find a MobileParty or PartyBase parameter among args
                MobileParty party = null;
                foreach (var arg in __args)
                {
                    if (arg is MobileParty mp)
                    {
                        party = mp; break;
                    }
                    if (arg is PartyBase pb && pb?.MobileParty != null)
                    {
                        party = pb.MobileParty; break;
                    }
                }
                if (party != null && party == MobileParty.MainParty)
                {
                    LoggingService.Debug("PartyVisualHidePatch", $"Suppressing visual creation for MainParty while enlisted (method={__instance.GetType().Name}).");
                    return false; // skip original
                }
            }
            catch { }
            return true;
        }
    }
}


