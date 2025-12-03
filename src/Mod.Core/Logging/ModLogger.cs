using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Enlisted.Mod.Core.Logging
{
	/// <summary>
	/// Log levels for controlling verbosity per category.
	/// Higher values include all lower levels (e.g., Info includes Warn and Error).
	/// </summary>
	public enum LogLevel
	{
		Off = 0,    // No logging
		Error = 1,  // Only errors
		Warn = 2,   // Errors and warnings
		Info = 3,   // Normal operational messages
		Debug = 4,  // Detailed debugging info
		Trace = 5   // Very verbose, for deep debugging
	}

	/// <summary>
	/// File logger for the mod with category-based log levels, throttling, and anti-spam features.
	/// Writes to Modules/Enlisted/Debugging/enlisted.log and category-specific log files.
	/// 
	/// Features:
	/// - Per-category log levels (configure in settings.json)
	/// - Message throttling to prevent spam from repeated messages
	/// - LogOnce() for one-time messages per session
	/// - LogSummary() for periodic summary reports
	/// </summary>
	public static class ModLogger
	{
		private static readonly object Sync = new object();
		private static string _logFilePath = null; // Will be set in Initialize()
		private static string _sessionId = Guid.NewGuid().ToString("N").Substring(0, 8);

		// Per-category log levels (default to Info)
		private static Dictionary<string, LogLevel> _categoryLevels = new Dictionary<string, LogLevel>(StringComparer.OrdinalIgnoreCase);
		private static LogLevel _defaultLevel = LogLevel.Info;

		// Throttling system - tracks recent messages to prevent spam
		private static readonly Dictionary<string, ThrottleEntry> _throttleCache = new Dictionary<string, ThrottleEntry>();
		private static int _throttleSeconds = 5;

		// LogOnce tracking - messages that should only appear once per session
		private static readonly HashSet<string> _loggedOnceKeys = new HashSet<string>();

		// Summary tracking - accumulates counts for periodic summaries
		private static readonly Dictionary<string, SummaryEntry> _summaryData = new Dictionary<string, SummaryEntry>();

		/// <summary>
		/// Tracks repeated messages for throttling.
		/// </summary>
		private class ThrottleEntry
		{
			public DateTime LastLogTime { get; set; }
			public int SuppressedCount { get; set; }
			public string Level { get; set; }
			public string Category { get; set; }
		}

		/// <summary>
		/// Tracks data for summary logging.
		/// </summary>
		private class SummaryEntry
		{
			public int Count { get; set; }
			public int TotalValue { get; set; }
			public DateTime LastUpdate { get; set; }
		}

		/// <summary>
		/// Initialize the logger and configure log levels from settings.
		/// </summary>
		public static void Initialize(string customPath = null)
		{
			try
			{
				lock (Sync)
				{
					_logFilePath = string.IsNullOrWhiteSpace(customPath) ? ResolveDefaultLogPath() : customPath;
					
					// Ensure we have a valid path
					if (string.IsNullOrWhiteSpace(_logFilePath))
					{
						_logFilePath = ResolveDocumentsPath("enlisted.log");
					}
					
					var logDir = Path.GetDirectoryName(_logFilePath);
					EnsureDirectoryExists(logDir);
					ClearPreviousSessionFiles(logDir);
					
					// Clear session-specific tracking
					_loggedOnceKeys.Clear();
					_throttleCache.Clear();
					_summaryData.Clear();
					
					// Write initialization message - this will test if logging works
					WriteInternal("INFO", "Init", $"Logger initialized (session: {_sessionId}, path: {_logFilePath})");
				}
			}
			catch (Exception ex)
			{
				// Log to debug output so we can see what went wrong during initialization
				System.Diagnostics.Debug.WriteLine($"[Enlisted] Logger initialization failed: {ex.Message}\n{ex.StackTrace}");
				// Try to set a fallback path
				try
				{
					_logFilePath = ResolveDocumentsPath("enlisted.log");
					var logDir = Path.GetDirectoryName(_logFilePath);
					EnsureDirectoryExists(logDir);
				}
				catch
				{
					// If even fallback fails, we'll use Debug output only
				}
			}
		}

		/// <summary>
		/// Configure log levels per category. Call after Initialize() and after loading settings.
		/// </summary>
		/// <param name="categoryLevels">Dictionary mapping category names to log levels</param>
		/// <param name="defaultLevel">Default level for categories not explicitly configured</param>
		/// <param name="throttleSeconds">Seconds to suppress repeated identical messages (0 to disable)</param>
		public static void ConfigureLevels(Dictionary<string, LogLevel> categoryLevels, LogLevel defaultLevel = LogLevel.Info, int throttleSeconds = 5)
		{
			lock (Sync)
			{
				_categoryLevels = categoryLevels ?? new Dictionary<string, LogLevel>(StringComparer.OrdinalIgnoreCase);
				_defaultLevel = defaultLevel;
				_throttleSeconds = Math.Max(0, throttleSeconds);
				
				WriteInternal("INFO", "Init", $"Log levels configured: default={defaultLevel}, throttle={_throttleSeconds}s, categories={_categoryLevels.Count}");
			}
		}

		/// <summary>
		/// Get the effective log level for a category.
		/// </summary>
		public static LogLevel GetCategoryLevel(string category)
		{
			if (string.IsNullOrEmpty(category))
			{
				return _defaultLevel;
			}
				
			lock (Sync)
			{
				if (_categoryLevels.TryGetValue(category, out var level))
				{
					return level;
				}
				return _defaultLevel;
			}
		}

		/// <summary>
		/// Check if a message at the given level would be logged for the category.
		/// </summary>
		public static bool IsEnabled(string category, LogLevel level)
		{
			return level <= GetCategoryLevel(category);
		}

		#region Standard Logging Methods

		/// <summary>
		/// Log a trace-level message (most verbose).
		/// </summary>
		public static void Trace(string category, string message)
		{
			if (IsEnabled(category, LogLevel.Trace))
			{
				LogWithThrottle("TRACE", category, message);
			}
		}

		/// <summary>
		/// Log a debug-level message.
		/// </summary>
		public static void Debug(string category, string message)
		{
			if (IsEnabled(category, LogLevel.Debug))
			{
				LogWithThrottle("DEBUG", category, message);
			}
		}

		/// <summary>
		/// Log an info-level message.
		/// </summary>
		public static void Info(string category, string message)
		{
			if (IsEnabled(category, LogLevel.Info))
			{
				LogWithThrottle("INFO", category, message);
			}
		}

		/// <summary>
		/// Log a warning-level message.
		/// </summary>
		public static void Warn(string category, string message)
		{
			if (IsEnabled(category, LogLevel.Warn))
			{
				LogWithThrottle("WARN", category, message);
			}
		}

		/// <summary>
		/// Log an error-level message with optional exception details.
		/// Errors are never throttled.
		/// </summary>
		public static void Error(string category, string message, Exception ex = null)
		{
			if (!IsEnabled(category, LogLevel.Error))
			{
				return;
			}
				
			var text = ex == null ? message : $"{message} | Exception: {ex.GetType().Name}: {ex.Message}";
			WriteInternal("ERROR", category, text);
			
			// Also log stack trace at debug level if exception provided
			if (ex != null && IsEnabled(category, LogLevel.Debug))
			{
				WriteInternal("DEBUG", category, $"Stack trace: {ex.StackTrace}");
			}
		}

		#endregion

		#region Special Logging Methods

		/// <summary>
		/// Log a message only once per session. Useful for startup messages or first-time events.
		/// </summary>
		/// <param name="key">Unique key to identify this message (e.g., "battle_system_init")</param>
		/// <param name="category">Log category</param>
		/// <param name="message">Message to log</param>
		/// <param name="level">Log level (default Info)</param>
		public static void LogOnce(string key, string category, string message, LogLevel level = LogLevel.Info)
		{
			if (!IsEnabled(category, level))
			{
				return;
			}

			lock (Sync)
			{
				if (_loggedOnceKeys.Contains(key))
				{
					return;
				}
					
				_loggedOnceKeys.Add(key);
			}
			
			var levelStr = level.ToString().ToUpper();
			WriteInternal(levelStr, category, message);
		}

		/// <summary>
		/// Increment a summary counter. Use with FlushSummary() to log aggregated data.
		/// Useful for tracking things like "X battles participated" or "Y gold earned".
		/// </summary>
		/// <param name="key">Unique key for this summary (e.g., "battles_won")</param>
		/// <param name="incrementCount">Amount to add to count (default 1)</param>
		/// <param name="incrementValue">Optional value to accumulate (e.g., gold amount)</param>
		public static void IncrementSummary(string key, int incrementCount = 1, int incrementValue = 0)
		{
			lock (Sync)
			{
				if (!_summaryData.TryGetValue(key, out var entry))
				{
					entry = new SummaryEntry();
					_summaryData[key] = entry;
				}
				entry.Count += incrementCount;
				entry.TotalValue += incrementValue;
				entry.LastUpdate = DateTime.Now;
			}
		}

		/// <summary>
		/// Log and reset a summary counter.
		/// </summary>
		/// <param name="key">Summary key to flush</param>
		/// <param name="category">Log category</param>
		/// <param name="messageFormat">Message format string with {0} for count and {1} for total value</param>
		/// <param name="level">Log level (default Info)</param>
		public static void FlushSummary(string key, string category, string messageFormat, LogLevel level = LogLevel.Info)
		{
			if (!IsEnabled(category, level))
			{
				return;
			}

			SummaryEntry entry;
			lock (Sync)
			{
				if (!_summaryData.TryGetValue(key, out entry) || entry.Count == 0)
				{
					return;
				}
					
				_summaryData.Remove(key);
			}

			var message = string.Format(messageFormat, entry.Count, entry.TotalValue);
			var levelStr = level.ToString().ToUpper();
			WriteInternal(levelStr, category, message);
		}

		/// <summary>
		/// Log a state transition. Useful for tracking game state changes.
		/// </summary>
		public static void StateChange(string category, string fromState, string toState, string details = null)
		{
			if (!IsEnabled(category, LogLevel.Info))
			{
				return;
			}

			var message = $"State: [{fromState}] -> [{toState}]";
			if (!string.IsNullOrEmpty(details))
			{
				message += $" | {details}";
			}
				
			LogWithThrottle("INFO", category, message);
		}

		/// <summary>
		/// Log an action result. Useful for tracking success/failure of operations.
		/// </summary>
		public static void ActionResult(string category, string action, bool success, string details = null)
		{
			var level = success ? LogLevel.Info : LogLevel.Warn;
			if (!IsEnabled(category, level))
			{
				return;
			}

			var status = success ? "OK" : "FAILED";
			var message = $"{action}: {status}";
			if (!string.IsNullOrEmpty(details))
			{
				message += $" | {details}";
			}
				
			var levelStr = level.ToString().ToUpper();
			LogWithThrottle(levelStr, category, message);
		}

		#endregion

		#region Internal Methods

		/// <summary>
		/// Log with throttling to prevent spam from repeated identical messages.
		/// </summary>
		private static void LogWithThrottle(string level, string category, string message)
		{
			if (_throttleSeconds <= 0)
			{
				WriteInternal(level, category, message);
				return;
			}

			var key = $"{category}|{message}";
			var now = DateTime.Now;

			lock (Sync)
			{
				if (_throttleCache.TryGetValue(key, out var entry))
				{
					var elapsed = (now - entry.LastLogTime).TotalSeconds;
					if (elapsed < _throttleSeconds)
					{
						// Still within throttle window - suppress and count
						entry.SuppressedCount++;
						return;
					}
					
					// Throttle window expired - log with suppression count if any
					if (entry.SuppressedCount > 0)
					{
						WriteInternal(level, category, $"{message} (repeated {entry.SuppressedCount}x)");
					}
					else
					{
						WriteInternal(level, category, message);
					}
					
					entry.LastLogTime = now;
					entry.SuppressedCount = 0;
				}
				else
				{
					// First time seeing this message
					_throttleCache[key] = new ThrottleEntry
					{
						LastLogTime = now,
						SuppressedCount = 0,
						Level = level,
						Category = category
					};
					WriteInternal(level, category, message);
				}
			}
			
			// Periodically clean old entries to prevent memory growth
			CleanThrottleCache();
		}

		/// <summary>
		/// Remove old throttle cache entries to prevent unbounded memory growth.
		/// </summary>
		private static void CleanThrottleCache()
		{
			// Only clean occasionally to avoid overhead
			if (_throttleCache.Count < 100)
			{
				return;
			}

			var now = DateTime.Now;
			var expiredKeys = new List<string>();

			lock (Sync)
			{
				foreach (var kvp in _throttleCache)
				{
					if ((now - kvp.Value.LastLogTime).TotalSeconds > _throttleSeconds * 10)
					{
						expiredKeys.Add(kvp.Key);
					}
				}

				foreach (var key in expiredKeys)
				{
					_throttleCache.Remove(key);
				}
			}
		}

		private static void WriteInternal(string level, string category, string message)
		{
			// Ensure path is set (in case Initialize wasn't called)
			if (string.IsNullOrWhiteSpace(_logFilePath))
			{
				_logFilePath = ResolveDefaultLogPath();
				if (string.IsNullOrWhiteSpace(_logFilePath))
				{
					_logFilePath = ResolveDocumentsPath("enlisted.log");
				}
			}
			
			try
			{
				lock (Sync)
				{
					var logDir = Path.GetDirectoryName(_logFilePath);
					EnsureDirectoryExists(logDir);
					var line = FormatLine(level, category, message);
					File.AppendAllText(_logFilePath, line, Encoding.UTF8);
				}
			}
			catch (Exception ex)
			{
				// Fallback to Documents path
				try
				{
					var fallback = ResolveDocumentsPath("enlisted.log");
					var logDir = Path.GetDirectoryName(fallback);
					EnsureDirectoryExists(logDir);
					var line = FormatLine(level, category, message);
					File.AppendAllText(fallback, line, Encoding.UTF8);
					
					// If we had to use fallback, update the main path for future writes
					if (_logFilePath != fallback)
					{
						System.Diagnostics.Debug.WriteLine($"[Enlisted] Primary log path failed, using fallback: {fallback} (Error: {ex.Message})");
						_logFilePath = fallback;
					}
				}
				catch (Exception fallbackEx)
				{
					// Last resort: Debug output
					System.Diagnostics.Debug.WriteLine($"[Enlisted][{level}][{category}] {message}");
					System.Diagnostics.Debug.WriteLine($"[Enlisted] Both primary and fallback log paths failed. Primary: {ex.Message}, Fallback: {fallbackEx.Message}");
				}
			}
		}

		private static string FormatLine(string level, string category, string message)
		{
			var timestamp = DateTime.Now.ToString("HH:mm:ss");
			// User-friendly format: [time] [LEVEL] [Category] Message
			return $"[{timestamp}] [{level,-5}] [{category}] {message}\r\n";
		}

		#endregion

		#region Path Resolution

		private static string ResolveDefaultLogPath()
		{
			try
			{
				var assembly = typeof(ModLogger).Assembly;
				var assemblyLocation = assembly.Location;
				
				// Assembly.Location can be null or empty in some scenarios (e.g., dynamic loading, single-file apps)
				if (string.IsNullOrWhiteSpace(assemblyLocation))
				{
					// Fallback: use Documents folder if we can't determine assembly location
					System.Diagnostics.Debug.WriteLine("[Enlisted] Assembly.Location is null/empty, using Documents fallback");
					return ResolveDocumentsPath("enlisted.log");
				}
				
				var dllDir = Path.GetDirectoryName(assemblyLocation);
				if (string.IsNullOrWhiteSpace(dllDir))
				{
					System.Diagnostics.Debug.WriteLine("[Enlisted] Could not get directory from assembly location, using Documents fallback");
					return ResolveDocumentsPath("enlisted.log");
				}
				
				// Navigate from bin/Win64_Shipping_Client/Enlisted.dll up to Modules/Enlisted/Debugging/
				var binDir = Directory.GetParent(dllDir);
				if (binDir == null)
				{
					System.Diagnostics.Debug.WriteLine("[Enlisted] Could not get parent directory, using Documents fallback");
					return ResolveDocumentsPath("enlisted.log");
				}
				
				var enlistedRoot = binDir.Parent;
				if (enlistedRoot == null)
				{
					System.Diagnostics.Debug.WriteLine("[Enlisted] Could not get module root directory, using Documents fallback");
					return ResolveDocumentsPath("enlisted.log");
				}
				
				var dir = Path.Combine(enlistedRoot.FullName, "Debugging");
				var logPath = Path.Combine(dir, "enlisted.log");
				
				// Verify the path looks reasonable
				if (logPath.Contains("Enlisted") && logPath.Contains("Debugging"))
				{
					return logPath;
				}
				
				// Path doesn't look right, use fallback
				System.Diagnostics.Debug.WriteLine($"[Enlisted] Resolved path doesn't look correct: {logPath}, using Documents fallback");
				return ResolveDocumentsPath("enlisted.log");
			}
			catch (Exception ex)
			{
				// Log to debug output so we can see what went wrong
				System.Diagnostics.Debug.WriteLine($"[Enlisted] Failed to resolve log path: {ex.Message}\n{ex.StackTrace}");
				return ResolveDocumentsPath("enlisted.log");
			}
		}

		private static string ResolveDocumentsPath(string fileName)
		{
			var docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
			var dir = Path.Combine(docs, "Mount and Blade II Bannerlord", "Logs", "Enlisted");
			return Path.Combine(dir, fileName);
		}

		private static void EnsureDirectoryExists(string directory)
		{
			if (string.IsNullOrWhiteSpace(directory))
			{
				return;
			}
			if (!Directory.Exists(directory))
			{
				Directory.CreateDirectory(directory);
			}
		}

		private static void ClearPreviousSessionFiles(string directory)
		{
			try
			{
				if (string.IsNullOrWhiteSpace(directory))
				{
					return;
				}
				if (!Directory.Exists(directory))
				{
					return;
				}
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

		#endregion
	}
}
