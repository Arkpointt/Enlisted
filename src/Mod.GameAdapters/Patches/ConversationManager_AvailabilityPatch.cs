using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Enlisted.Mod.Core.Config;
using Enlisted.Mod.Core.Logging;

namespace Enlisted.Mod.GameAdapters.Patches
{
	// Harmony Patch
	// Target: ConversationManager methods likely rebuilding options (best-effort reflection)
	// This dumps candidate tokens/option ids without requiring clicks.
	[HarmonyPatch]
	internal static class ConversationManager_AvailabilityPatch
	{
		static IEnumerable<MethodBase> TargetMethods()
		{
			var cmType = AccessTools.TypeByName("TaleWorlds.CampaignSystem.Conversation.ConversationManager");
			if (cmType == null) yield break;
			foreach (var m in AccessTools.GetDeclaredMethods(cmType))
			{
				var n = m.Name;
				if (n.IndexOf("Option", StringComparison.OrdinalIgnoreCase) >= 0 ||
					n.IndexOf("Build", StringComparison.OrdinalIgnoreCase) >= 0 ||
					n.IndexOf("Refresh", StringComparison.OrdinalIgnoreCase) >= 0 ||
					n.IndexOf("Update", StringComparison.OrdinalIgnoreCase) >= 0)
				{
					yield return m;
				}
			}
		}

		static void Postfix(object __instance)
		{
			if (!ModConfig.Settings.LogDialogs) return;
			try
			{
				var tokens = new List<string>();
				var type = __instance.GetType();
				foreach (var f in AccessTools.GetDeclaredFields(type))
				{
					var val = f.GetValue(__instance);
					if (val == null) continue;
					// String fields containing token/id
					if (val is string s)
					{
						if (ContainsTokenish(s)) tokens.Add($"{f.Name}={s}");
						continue;
					}
					// Collections with strings
					if (val is IEnumerable enumerable)
					{
						int count = 0;
						foreach (var item in enumerable)
						{
							if (item == null) continue;
							if (item is string si)
							{
								if (ContainsTokenish(si)) tokens.Add($"{f.Name}[]={si}");
							}
							else
							{
								// Try common string properties like Id/Token/Text
								var sp = item.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
								foreach (var p in sp)
								{
									if (p.PropertyType != typeof(string)) continue;
									string sv;
									try { sv = p.GetValue(item) as string; }
									catch { continue; }
									if (string.IsNullOrEmpty(sv)) continue;
									if (ContainsTokenish(sv)) tokens.Add($"{f.Name}.{p.Name}={sv}");
								}
							}
							if (++count > 50) break; // cap scanning
						}
					}
				}
				if (tokens.Count > 0)
				{
					var sample = string.Join(", ", tokens.Take(10));
					ModLogger.Dialog("INFO", $"conversation availability: {sample}");
				}
			}
			catch
			{
				// best-effort only
			}
		}

		private static bool ContainsTokenish(string value)
		{
			if (string.IsNullOrWhiteSpace(value)) return false;
			// Heuristics: tokens/ids are simple lowercase with underscores, avoid long texts
			if (value.Length > 100) return false;
			var v = value.Trim();
			return v.Any(char.IsLetter) && v.Contains("_");
		}
	}
}


