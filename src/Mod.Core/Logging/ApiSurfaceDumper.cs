using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Enlisted.Mod.Core.Logging
{
	internal static class ApiSurfaceDumper
	{
		public static void Dump()
		{
			try
			{
				var path = ResolvePath();
				EnsureDir(Path.GetDirectoryName(path));
				using (var w = new StreamWriter(path, false))
				{
					DumpType(w, "TaleWorlds.CampaignSystem.CampaignGameStarter");
					DumpType(w, "TaleWorlds.CampaignSystem.GameMenus.GameMenu");
					DumpType(w, "TaleWorlds.CampaignSystem.GameMenus.GameMenuManager");
					DumpType(w, "TaleWorlds.CampaignSystem.Conversation.ConversationManager");
					DumpType(w, "TaleWorlds.CampaignSystem.Armies.Army");
					DumpType(w, "TaleWorlds.CampaignSystem.Party.MobileParty");
				}
			}
			catch
			{
				// swallow; discovery best-effort
			}
		}

		private static void DumpType(StreamWriter w, string typeName)
		{
			var t = AccessTools_TypeByName(typeName);
			if (t == null)
			{
				w.WriteLine($"# {typeName}: <missing>");
				return;
			}
			w.WriteLine($"# {t.FullName}");
			foreach (var m in t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly).OrderBy(x => x.Name))
			{
				var pars = string.Join(", ", m.GetParameters().Select(p => p.ParameterType.Name + " " + p.Name));
				w.WriteLine($"- {m.ReturnType.Name} {m.Name}({pars})");
			}
			w.WriteLine();
		}

		private static Type AccessTools_TypeByName(string name)
		{
			return AppDomain.CurrentDomain.GetAssemblies()
				.Select(a => a.GetType(name, false))
				.FirstOrDefault(t => t != null);
		}

		private static string ResolvePath()
		{
			var dllDir = Path.GetDirectoryName(typeof(ApiSurfaceDumper).Assembly.Location);
			var binDir = Directory.GetParent(dllDir); // bin
			var enlistedRoot = binDir?.Parent; // Enlisted module root
			var dir = enlistedRoot != null ? Path.Combine(enlistedRoot.FullName, "Debugging") : "Debugging";
			return Path.Combine(dir, "api_surface.txt");
		}

		private static void EnsureDir(string dir)
		{
			if (string.IsNullOrWhiteSpace(dir)) return;
			if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
		}
	}
}
