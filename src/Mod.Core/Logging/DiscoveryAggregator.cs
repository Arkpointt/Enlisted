using System;
using System.Collections.Generic;
using System.IO;

namespace Enlisted.Mod.Core.Logging
{
	internal static class DiscoveryAggregator
	{
		private static readonly HashSet<string> MenuIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		private static readonly HashSet<string> DialogTokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		private static readonly object Sync = new object();

		public static void RecordMenu(string id)
		{
			if (string.IsNullOrWhiteSpace(id)) return;
			lock (Sync)
			{
				if (MenuIds.Add(id)) WriteMenuIndex();
			}
		}

		public static void RecordDialogToken(string token)
		{
			if (string.IsNullOrWhiteSpace(token)) return;
			lock (Sync)
			{
				if (DialogTokens.Add(token)) WriteDialogIndex();
			}
		}

		private static void WriteMenuIndex()
		{
			try
			{
				var path = ResolvePath("attributed_menus.txt");
				EnsureDir(Path.GetDirectoryName(path));
				File.WriteAllLines(path, MenuIds);
			}
			catch { }
		}

		private static void WriteDialogIndex()
		{
			try
			{
				var path = ResolvePath("dialog_tokens.txt");
				EnsureDir(Path.GetDirectoryName(path));
				File.WriteAllLines(path, DialogTokens);
			}
			catch { }
		}

		private static string ResolvePath(string fileName)
		{
			var dllDir = Path.GetDirectoryName(typeof(DiscoveryAggregator).Assembly.Location);
			var binDir = Directory.GetParent(dllDir); // bin
			var enlistedRoot = binDir?.Parent; // Enlisted module root
			var dir = enlistedRoot != null ? Path.Combine(enlistedRoot.FullName, "Debugging") : "Debugging";
			return Path.Combine(dir, fileName);
		}

		private static void EnsureDir(string dir)
		{
			if (string.IsNullOrWhiteSpace(dir)) return;
			if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
		}
	}
}


