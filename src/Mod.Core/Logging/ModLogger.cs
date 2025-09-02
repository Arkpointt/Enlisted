using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Enlisted.Mod.Core.Logging
{
	/// <summary>
	/// Simple file logger for the mod with categories and severity levels.
	/// Writes to Documents\Mount and Blade II Bannerlord\Logs\Enlisted\enlisted.log
	/// </summary>
	public static class ModLogger
	{
		private static readonly object Sync = new object();
		private static string _logFilePath = ResolveDefaultLogPath();
		private static string _apiLogPath = ResolveApiLogPath();
		private static string _discoveryLogPath = ResolveDiscoveryLogPath();
		private static string _dialogLogPath = ResolveDialogLogPath();
		private static string _sessionId = Guid.NewGuid().ToString("N");

		public static void Initialize(string customPath = null)
		{
			try
			{
				lock (Sync)
				{
					_logFilePath = string.IsNullOrWhiteSpace(customPath) ? ResolveDefaultLogPath() : customPath;
					var logDir = Path.GetDirectoryName(_logFilePath);
					EnsureDirectoryExists(logDir);
					EnsureDirectoryExists(Path.GetDirectoryName(_apiLogPath));
					EnsureDirectoryExists(Path.GetDirectoryName(_discoveryLogPath));
					EnsureDirectoryExists(Path.GetDirectoryName(_dialogLogPath));
					ClearPreviousSessionFiles(logDir);
					// Pre-create category logs with a header so they exist even before first entry
					WriteToFile("INFO", "Api", "API log started", _apiLogPath);
					WriteToFile("INFO", "Discovery", "Discovery log started", _discoveryLogPath);
					WriteToFile("INFO", "Dialog", "Dialog log started", _dialogLogPath);
					WriteInternal("INFO", "Init", "Logger initialized");
				}
			}
			catch
			{
				// Do not throw from init; fall back to Debug output inside WriteInternal paths
			}
		}

		public static void Info(string category, string message)
		{
			WriteInternal("INFO", category, message);
		}

		public static void Debug(string category, string message)
		{
			WriteInternal("DEBUG", category, message);
		}

		public static void Error(string category, string message, Exception ex = null)
		{
			var text = ex == null ? message : message + " | " + ex;
			WriteInternal("ERROR", category, text);
		}

		public static void Discovery(string level, string message)
		{
			WriteToFile(level, "Discovery", message, _discoveryLogPath);
		}

		public static void Api(string level, string message)
		{
			WriteToFile(level, "Api", message, _apiLogPath);
		}

		public static void Dialog(string level, string message)
		{
			WriteToFile(level, "Dialog", message, _dialogLogPath);
		}

		private static void WriteInternal(string level, string category, string message)
		{
			try
			{
				lock (Sync)
				{
					EnsureDirectoryExists(Path.GetDirectoryName(_logFilePath));
					var line = FormatLine(level, category, message);
					File.AppendAllText(_logFilePath, line, Encoding.UTF8);
				}
			}
			catch
			{
				// Fallback to Documents path
				try
				{
					var fallback = ResolveDocumentsPath("enlisted.log");
					EnsureDirectoryExists(Path.GetDirectoryName(fallback));
					var line = FormatLine(level, category, message);
					File.AppendAllText(fallback, line, Encoding.UTF8);
				}
				catch
				{
					System.Diagnostics.Debug.WriteLine($"[Enlisted][{level}][{category}] {message}");
				}
			}
		}

		private static void WriteToFile(string level, string category, string message, string path)
		{
			try
			{
				lock (Sync)
				{
					EnsureDirectoryExists(Path.GetDirectoryName(path));
					var line = FormatLine(level, category, message);
					File.AppendAllText(path, line, Encoding.UTF8);
				}
			}
			catch
			{
				// Fallback to Documents path
				try
				{
					var fallback = ResolveDocumentsPath(Path.GetFileName(path));
					EnsureDirectoryExists(Path.GetDirectoryName(fallback));
					var line = FormatLine(level, category, message);
					File.AppendAllText(fallback, line, Encoding.UTF8);
				}
				catch
				{
					System.Diagnostics.Debug.WriteLine($"[Enlisted][{level}][{category}] {message}");
				}
			}
		}

		private static string FormatLine(string level, string category, string message)
		{
			var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
			return $"{timestamp} [{_sessionId}] [{level}] [{category}] {message}\r\n";
		}

		private static string ResolveDefaultLogPath()
		{
			// Place logs alongside the installed module: <Modules>\Enlisted\Debugging\enlisted.log
			try
			{
				var dllDir = Path.GetDirectoryName(typeof(ModLogger).Assembly.Location);
				var binDir = Directory.GetParent(dllDir); // bin
				var enlistedRoot = binDir?.Parent; // Enlisted module root
				var dir = enlistedRoot != null ? Path.Combine(enlistedRoot.FullName, "Debugging") : "Debugging";
				return Path.Combine(dir, "enlisted.log");
			}
			catch { return Path.Combine("Debugging", "enlisted.log"); }
		}

		private static string ResolveApiLogPath()
		{
			try
			{
				var dllDir = Path.GetDirectoryName(typeof(ModLogger).Assembly.Location);
				var binDir = Directory.GetParent(dllDir); // bin
				var enlistedRoot = binDir?.Parent; // Enlisted module root
				var dir = enlistedRoot != null ? Path.Combine(enlistedRoot.FullName, "Debugging") : "Debugging";
				return Path.Combine(dir, "api.log");
			}
			catch { return Path.Combine("Debugging", "api.log"); }
		}

		private static string ResolveDiscoveryLogPath()
		{
			try
			{
				var dllDir = Path.GetDirectoryName(typeof(ModLogger).Assembly.Location);
				var binDir = Directory.GetParent(dllDir); // bin
				var enlistedRoot = binDir?.Parent; // Enlisted module root
				var dir = enlistedRoot != null ? Path.Combine(enlistedRoot.FullName, "Debugging") : "Debugging";
				return Path.Combine(dir, "discovery.log");
			}
			catch { return Path.Combine("Debugging", "discovery.log"); }
		}

		private static string ResolveDialogLogPath()
		{
			try
			{
				var dllDir = Path.GetDirectoryName(typeof(ModLogger).Assembly.Location);
				var binDir = Directory.GetParent(dllDir); // bin
				var enlistedRoot = binDir?.Parent; // Enlisted module root
				var dir = enlistedRoot != null ? Path.Combine(enlistedRoot.FullName, "Debugging") : "Debugging";
				return Path.Combine(dir, "dialog.log");
			}
			catch { return Path.Combine("Debugging", "dialog.log"); }
		}

		private static string ResolveDocumentsPath(string fileName)
		{
			var docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
			var dir = Path.Combine(docs, "Mount and Blade II Bannerlord", "Logs", "Enlisted");
			return Path.Combine(dir, fileName);
		}

		private static void EnsureDirectoryExists(string directory)
		{
			if (string.IsNullOrWhiteSpace(directory)) return;
			if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
		}

		private static void ClearPreviousSessionFiles(string directory)
		{
			try
			{
				if (string.IsNullOrWhiteSpace(directory)) return;
				if (!Directory.Exists(directory)) return;
				foreach (var file in Directory.GetFiles(directory))
				{
					try { File.Delete(file); } catch { /* best effort */ }
				}
			}
			catch
			{
				// best effort cleanup; ignore
			}
		}
	}
}


