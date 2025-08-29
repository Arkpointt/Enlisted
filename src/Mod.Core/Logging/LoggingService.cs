using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using TaleWorlds.Library;

namespace Enlisted.Mod.Core.Logging
{
    /// <summary>
    /// Centralized logging service for the Enlisted mod.
    /// Outputs logs to the modules debugging folder with session management.
    /// </summary>
    public static class LoggingService
    {
        private static readonly string LogDirectory = Path.Combine(
            "C:\\Program Files (x86)\\Steam\\steamapps\\common\\Mount & Blade II Bannerlord\\Modules\\Enlisted\\debugging");
        
        private static readonly string UserLogFile = Path.Combine(LogDirectory, "user_log.txt");
        private static readonly string ApiLogFile = Path.Combine(LogDirectory, "api_calls.txt");
        private static readonly string SessionLogFile = Path.Combine(LogDirectory, "session_log.txt");
        
        private static readonly object LogLock = new object();
        private static readonly Queue<string> SessionLogs = new Queue<string>();
        private static readonly int MaxSessionLogs = 1000; // Keep last 1000 entries
        
        private static bool _isInitialized = false;
        private static string _sessionId;

        /// <summary>
        /// Initialize the logging service
        /// </summary>
        public static void Initialize()
        {
            if (_isInitialized) return;
            
            // Always show initialization in game console for debugging
            InformationManager.DisplayMessage(new InformationMessage(
                "Enlisted: Initializing logging service...", 
                Color.FromUint(0xFF00FF00)));
            
            try
            {
                // Create log directory if it doesn't exist
                if (!Directory.Exists(LogDirectory))
                {
                    Directory.CreateDirectory(LogDirectory);
                    InformationManager.DisplayMessage(new InformationMessage(
                        $"Enlisted: Created log directory: {LogDirectory}", 
                        Color.FromUint(0xFF00FF00)));
                }

                // Generate session ID
                _sessionId = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                
                // Clean up old log files (keep only 3 sessions)
                CleanupOldLogs();
                
                // Write session start
                WriteToSessionLog($"=== ENLISTED MOD SESSION STARTED: {_sessionId} ===");
                WriteToSessionLog($"Mod Version: 1.0.0");
                WriteToSessionLog($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                WriteToSessionLog("================================================");
                
                _isInitialized = true;
                
                // Log initialization success
                WriteToUserLog("INFO", "LoggingService", "Logging service initialized successfully");
                
                // Show success in game console
                InformationManager.DisplayMessage(new InformationMessage(
                    "Enlisted: Logging service initialized successfully!", 
                    Color.FromUint(0xFF00FF00)));
            }
            catch (Exception ex)
            {
                // Fallback to game's logging if our logging fails
                InformationManager.DisplayMessage(new InformationMessage(
                    $"Enlisted Logging Error: {ex.Message}", 
                    Color.FromUint(0xFFFF0000)));
            }
        }

        /// <summary>
        /// Log user-facing messages for troubleshooting
        /// </summary>
        public static void WriteToUserLog(string level, string category, string message)
        {
            if (!_isInitialized) Initialize();
            
            var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] [{category}] {message}";
            
            lock (LogLock)
            {
                try
                {
                    File.AppendAllText(UserLogFile, logEntry + Environment.NewLine);
                }
                catch (Exception ex)
                {
                    // Fallback to game logging
                    InformationManager.DisplayMessage(new InformationMessage(
                        $"Log Write Error: {ex.Message}", 
                        Color.FromUint(0xFFFF0000)));
                }
            }
        }

        /// <summary>
        /// Log API calls for debugging (will be removed in release)
        /// </summary>
        public static void LogApiCall(string apiName, string methodName, object[] parameters = null, object result = null)
        {
            if (!_isInitialized) Initialize();
            
            var sb = new StringBuilder();
            sb.AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] API CALL:");
            sb.AppendLine($"  API: {apiName}");
            sb.AppendLine($"  Method: {methodName}");
            
            if (parameters != null && parameters.Length > 0)
            {
                sb.AppendLine("  Parameters:");
                for (int i = 0; i < parameters.Length; i++)
                {
                    var param = parameters[i];
                    sb.AppendLine($"    [{i}]: {param?.GetType().Name} = {param}");
                }
            }
            
            if (result != null)
            {
                sb.AppendLine($"  Result: {result.GetType().Name} = {result}");
            }
            
            sb.AppendLine("---");
            
            lock (LogLock)
            {
                try
                {
                    File.AppendAllText(ApiLogFile, sb.ToString());
                }
                catch (Exception ex)
                {
                    WriteToUserLog("ERROR", "LoggingService", $"Failed to write API log: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Write to session log (keeps last N entries in memory)
        /// </summary>
        private static void WriteToSessionLog(string message)
        {
            var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
            
            lock (LogLock)
            {
                SessionLogs.Enqueue(logEntry);
                
                // Keep only the last MaxSessionLogs entries
                while (SessionLogs.Count > MaxSessionLogs)
                {
                    SessionLogs.Dequeue();
                }
                
                try
                {
                    // Write all current session logs to file
                    File.WriteAllLines(SessionLogFile, SessionLogs.ToArray());
                }
                catch (Exception ex)
                {
                    WriteToUserLog("ERROR", "LoggingService", $"Failed to write session log: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Clean up old log files (keep only 3 sessions)
        /// </summary>
        private static void CleanupOldLogs()
        {
            try
            {
                var logFiles = Directory.GetFiles(LogDirectory, "*.txt");
                var fileInfos = new List<FileInfo>();
                
                foreach (var file in logFiles)
                {
                    fileInfos.Add(new FileInfo(file));
                }
                
                // Sort by creation time (oldest first)
                fileInfos.Sort((a, b) => a.CreationTime.CompareTo(b.CreationTime));
                
                // Remove oldest files if we have more than 3 sessions
                while (fileInfos.Count > 3)
                {
                    var oldestFile = fileInfos[0];
                    try
                    {
                        File.Delete(oldestFile.FullName);
                        fileInfos.RemoveAt(0);
                    }
                    catch (Exception ex)
                    {
                        WriteToUserLog("WARNING", "LoggingService", $"Failed to delete old log file {oldestFile.Name}: {ex.Message}");
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                WriteToUserLog("ERROR", "LoggingService", $"Failed to cleanup old logs: {ex.Message}");
            }
        }

        /// <summary>
        /// Log a debug message
        /// </summary>
        public static void Debug(string category, string message)
        {
            WriteToUserLog("DEBUG", category, message);
        }

        /// <summary>
        /// Log an info message
        /// </summary>
        public static void Info(string category, string message)
        {
            WriteToUserLog("INFO", category, message);
        }

        /// <summary>
        /// Log a warning message
        /// </summary>
        public static void Warning(string category, string message)
        {
            WriteToUserLog("WARNING", category, message);
        }

        /// <summary>
        /// Log an error message
        /// </summary>
        public static void Error(string category, string message)
        {
            WriteToUserLog("ERROR", category, message);
        }

        /// <summary>
        /// Log an exception
        /// </summary>
        public static void Exception(string category, Exception ex, string context = "")
        {
            var message = string.IsNullOrEmpty(context) ? ex.ToString() : $"{context}: {ex}";
            WriteToUserLog("EXCEPTION", category, message);
        }
    }
}
