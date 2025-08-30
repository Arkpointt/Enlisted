using HarmonyLib;
using System;
using System.Reflection;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using System.Collections.Generic;
using Enlisted.Mod.Core.Logging;

namespace Enlisted.Mod.GameAdapters.Patches
{
	// Harmony Patch
	// Target: SandBox.ViewModelCollection.Map.*Tracker*VM (methods like RefreshTrackedObjects/RefreshList/Initialize)
	// Why: Redirect map tracker while enlisted to track commander instead of MainParty (hidden/escorting)
	// Safety: Campaign/UI only; exits when not enlisted; reflection-based and null-guarded; no persistence touched
	// Notes: Uses broad discovery across versioned VM type names; Postfix adds commander tracking and removes MainParty
	[HarmonyPatch]
	internal static class MobilePartyTracker_Refresh_Patch
	{
		private static List<MethodBase> _targets;
		private static DateTime _lastDebugLogUtc;

		static bool Prepare()
		{
			_targets = ResolveTargets();
			if (_targets.Count == 0)
			{
				LoggingService.Info("TrackerPatch", "No tracker VM methods found; patch will not apply.");
				return false;
			}
			LoggingService.Info("TrackerPatch", $"Patching tracker methods: {string.Join(", ", _targets)}");
			return true;
		}

		static IEnumerable<MethodBase> TargetMethods()
		{
			return _targets ?? (IEnumerable<MethodBase>)ResolveTargets();
		}

		private static List<MethodBase> ResolveTargets()
		{
			var found = new List<MethodBase>();
			// Try multiple possible type names across versions
			var typeNames = new[]
			{
				// 1.2.x primary
				"SandBox.ViewModelCollection.Map.MapMobilePartyTrackerVM",
				"SandBox.ViewModelCollection.Map.MapTrackersVM",
				// fallbacks
				"SandBox.ViewModelCollection.Map.MobilePartyTrackerVM",
				"TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapMobilePartyTrackerVM",
				"TaleWorlds.CampaignSystem.ViewModelCollection.Map.MobilePartyTrackerVM",
				"TaleWorlds.MountAndBlade.ViewModelCollection.Map.MobilePartyTrackerVM"
			};
			var methodNames = new[]
			{
				"RefreshTrackedObjects",
				"RefreshTrackedParties",
				"RefreshList",
				"Refresh",
				"OnRefresh",
				"OnMobilePartyCreated",
				"OnMobilePartyRemoved",
				"Initialize"
			};
			foreach (var name in typeNames)
			{
				try
				{
					var t = AccessTools.TypeByName(name);
					if (t != null)
					{
						foreach (var mn in methodNames)
						{
							var m = AccessTools.Method(t, mn);
							if (m != null && !found.Contains(m))
							{
								found.Add(m);
							}
						}
						if (found.Count > 0)
						{
							LoggingService.Info("TrackerPatch", $"Found VM type by name: {t.FullName}");
						}
					}
				}
				catch { }
			}
			if (found.Count == 0)
			{
				// Fallback: scan all loaded assemblies for any Map *Tracker* VM
				try
				{
					foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
					{
						Type t = null;
						try { t = asm.GetTypes().FirstOrDefault(x => x.FullName != null && x.FullName.Contains(".ViewModelCollection.Map.") && x.Name.Contains("Tracker") && x.Name.EndsWith("VM")); } catch { }
						if (t == null)
						{
							continue;
						}
						LoggingService.Info("TrackerPatch", $"Discovered VM type via scan: {t.Assembly.GetName().Name}:{t.FullName}");
						foreach (var mn in methodNames)
						{
							var m = AccessTools.Method(t, mn);
							if (m != null && !found.Contains(m))
							{
								found.Add(m);
							}
						}
					}
				}
				catch { }
			}
			return found;
		}

		static void Postfix(object __instance)
		{
			try
			{
				var now = DateTime.UtcNow;
				if ((now - _lastDebugLogUtc).TotalSeconds > 5)
				{
					LoggingService.Debug("TrackerPatch", $"Postfix hit: {__instance.GetType().FullName}");
					_lastDebugLogUtc = now;
				}
				if (!Enlisted.Features.Enlistment.Application.EnlistmentBehavior.IsPlayerEnlisted)
				{
					return;
				}

				MobileParty commander = Enlisted.Features.Enlistment.Application.EnlistmentBehavior.CurrentCommanderParty;
				if (commander == null)
				{
					return;
				}

				// Use reflection to call TrackParty/UntrackParty if present
				var instType = __instance.GetType();
				var track = AccessTools.Method(instType, "TrackParty");
				var untrack = AccessTools.Method(instType, "UntrackParty");
				if (track != null)
				{
					track.Invoke(__instance, new object[] { commander });
					if ((DateTime.UtcNow - _lastDebugLogUtc).TotalSeconds > 5)
					{
						LoggingService.Debug("TrackerPatch", $"TrackParty(commander) invoked");
						_lastDebugLogUtc = DateTime.UtcNow;
					}
				}

				var main = Campaign.Current?.MainParty;
				if (main != null && untrack != null)
				{
					untrack.Invoke(__instance, new object[] { main });
					if ((DateTime.UtcNow - _lastDebugLogUtc).TotalSeconds > 5)
					{
						LoggingService.Debug("TrackerPatch", $"UntrackParty(main) invoked");
						_lastDebugLogUtc = DateTime.UtcNow;
					}
				}

				// Fallback: aggressively remove MainParty from any tracked collections inside the VM
				try
				{
					if (main != null)
					{
						foreach (var f in instType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
						{
							var val = f.GetValue(__instance);
							TryRemovePartyFromCollection(val, main);
						}
						foreach (var p in instType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
						{
							if (!p.CanRead)
							{
								continue;
							}
							var val = p.GetValue(__instance, null);
							TryRemovePartyFromCollection(val, main);
						}
					}
				}
				catch { }
			}
			catch
			{
				// UI-only safeguard; ignore
			}
		}

		private static void TryRemovePartyFromCollection(object collectionObj, MobileParty main)
		{
			if (collectionObj == null)
			{
				return;
			}
			try
			{
				// Handle IList collections of MobileParty or PartyBase wrappers
				var iListType = collectionObj.GetType().GetInterfaces().FirstOrDefault(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(System.Collections.Generic.IList<>));
				if (iListType != null)
				{
					var elemType = iListType.GetGenericArguments()[0];
					var list = collectionObj as System.Collections.IList;
					if (list == null)
					{
						return;
					}
					var removeTargets = new System.Collections.ArrayList();
					foreach (var item in list)
					{
						if (item == null)
						{
							continue;
						}
						if (item is MobileParty mp && mp == main)
						{
							removeTargets.Add(item);
							continue;
						}
						var partyProp = item.GetType().GetProperty("Party") ?? item.GetType().GetProperty("MobileParty") ?? item.GetType().GetProperty("Target");
						var partyVal = partyProp?.GetValue(item);
						if (partyVal is MobileParty mp2 && mp2 == main)
						{
							removeTargets.Add(item);
						}
					}
					foreach (var it in removeTargets)
					{
						list.Remove(it);
					}
					if (removeTargets.Count > 0)
					{
						if ((DateTime.UtcNow - _lastDebugLogUtc).TotalSeconds > 5)
						{
							LoggingService.Debug("TrackerPatch", $"Removed {removeTargets.Count} tracker entries for MainParty");
							_lastDebugLogUtc = DateTime.UtcNow;
						}
					}
				}
			}
			catch { }
		}
	}

	// Harmony Patch
	// Target: *.Trackers*VM.TrackParty(object party)
	// Why: Block tracking of MainParty while enlisted so the shield/banner does not reappear
	// Safety: Campaign/UI only; returns true when not enlisted; null-guarded; safe skip of original
	// Notes: Resilient to parameter being MobileParty or PartyBase; exits quickly when not matched
	[HarmonyPatch]
	internal static class MobilePartyTracker_TrackParty_BlockMain_Patch
	{
		private static System.Collections.Generic.List<MethodBase> _targets;

		static bool Prepare()
		{
			_targets = ResolveTargets();
			if (_targets.Count == 0)
			{
				LoggingService.Info("TrackerPatch", "No TrackParty methods found; block patch will not apply.");
				return false;
			}
			return true;
		}

		static System.Collections.Generic.IEnumerable<MethodBase> TargetMethods() => _targets;

		private static System.Collections.Generic.List<MethodBase> ResolveTargets()
		{
			var found = new System.Collections.Generic.List<MethodBase>();
			var typeNames = new[]
			{
				"SandBox.ViewModelCollection.Map.MapMobilePartyTrackerVM",
				"SandBox.ViewModelCollection.Map.MapTrackersVM",
				"SandBox.ViewModelCollection.Map.MobilePartyTrackerVM",
				"TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapMobilePartyTrackerVM",
				"TaleWorlds.CampaignSystem.ViewModelCollection.Map.MobilePartyTrackerVM",
				"TaleWorlds.MountAndBlade.ViewModelCollection.Map.MobilePartyTrackerVM"
			};
			foreach (var tn in typeNames)
			{
				var t = AccessTools.TypeByName(tn);
				if (t == null)
				{
					continue;
				}
				var m = AccessTools.Method(t, "TrackParty");
				if (m != null)
				{
					found.Add(m);
				}
			}
			return found;
		}

		static bool Prefix(object __instance, object party)
		{
			try
			{
				if (!Enlisted.Features.Enlistment.Application.EnlistmentBehavior.IsPlayerEnlisted)
				{
					return true;
				}
				MobileParty mp = null;
				if (party is MobileParty mp1)
				{
					mp = mp1;
				}
				else if (party is TaleWorlds.CampaignSystem.Party.PartyBase pb && pb.MobileParty != null)
				{
					mp = pb.MobileParty;
				}
				if (mp != null && mp == Campaign.Current?.MainParty)
				{
					LoggingService.Debug("TrackerPatch", "Blocked TrackParty for MainParty while enlisted.");
					return false; // skip original -> do not track MainParty
				}
			}
			catch { }
			return true;
		}
	}
}


