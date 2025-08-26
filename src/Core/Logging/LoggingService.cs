using System;
using TaleWorlds.Library;
using Enlisted.Core.Config;

namespace Enlisted.Core.Logging
{
    /// <summary>
    /// Centralized logging service following blueprint observability principles.
    /// Provides structured logging with stable categories and correlation support.
    /// Replaces direct InformationManager calls and TODO logging comments throughout the codebase.
    /// </summary>
    public interface ILoggingService
    {
        /// <summary>Log debug information for development and troubleshooting.</summary>
        void LogDebug(string category, string message, params object[] args);
        
        /// <summary>Log general information about mod operations.</summary>
        void LogInfo(string category, string message, params object[] args);
        
        /// <summary>Log warnings about potentially problematic situations.</summary>
        void LogWarning(string category, string message, params object[] args);
        
        /// <summary>Log errors that affect functionality but don't crash the game.</summary>
        void LogError(string category, string message, Exception exception = null);
        
        /// <summary>Display user-visible message in game UI.</summary>
        void ShowPlayerMessage(string message, bool isImportant = false);
        
        /// <summary>Log performance metrics and timing information.</summary>
        void LogPerformance(string category, string operation, TimeSpan duration);
    }

    /// <summary>
    /// Production implementation of logging service.
    /// Routes messages to appropriate TaleWorlds logging systems based on severity and configuration.
    /// Respects log level thresholds and performance settings from ModSettings.
    /// </summary>
    public class LoggingService : ILoggingService
    {
        private readonly ModSettings _settings;
        private readonly string _sessionId;

        public LoggingService(ModSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _sessionId = GenerateSessionId();
        }

        public void LogDebug(string category, string message, params object[] args)
        {
            if (!_settings.EnableDebugLogging || _settings.LogLevel > 0) return;

            var formattedMessage = FormatMessage("DEBUG", category, message, args);
            Debug.Print($"[Enlisted] {formattedMessage}");
        }

        public void LogInfo(string category, string message, params object[] args)
        {
            if (_settings.LogLevel > 1) return;

            var formattedMessage = FormatMessage("INFO", category, message, args);
            Debug.Print($"[Enlisted] {formattedMessage}");
        }

        public void LogWarning(string category, string message, params object[] args)
        {
            if (_settings.LogLevel > 2) return;

            var formattedMessage = FormatMessage("WARN", category, message, args);
            Debug.Print($"[Enlisted] {formattedMessage}");
            
            // Show important warnings to player if configured
            if (_settings.ShowVerboseMessages)
            {
                ShowPlayerMessage($"Warning: {string.Format(message, args)}", false);
            }
        }

        public void LogError(string category, string message, Exception exception = null)
        {
            if (_settings.LogLevel > 3) return;

            var formattedMessage = FormatMessage("ERROR", category, message);
            if (exception != null)
            {
                formattedMessage += $" Exception: {exception.Message}";
            }
            
            Debug.Print($"[Enlisted] {formattedMessage}");
            
            // Always show errors to player for support purposes
            ShowPlayerMessage($"Error: {message}", true);
        }

        public void ShowPlayerMessage(string message, bool isImportant = false)
        {
            var color = isImportant ? Colors.Red : Colors.White;
            InformationManager.DisplayMessage(new InformationMessage(message, color));
        }

        public void LogPerformance(string category, string operation, TimeSpan duration)
        {
            if (!_settings.EnablePerformanceLogging) return;
            
            LogDebug(category, "Performance: {0} took {1}ms", operation, duration.TotalMilliseconds);
        }

        private string FormatMessage(string level, string category, string message, params object[] args)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            var formattedMessage = args.Length > 0 ? string.Format(message, args) : message;
            return $"{timestamp} [{level}] [{category}] [Session:{_sessionId}] {formattedMessage}";
        }

        private static string GenerateSessionId()
        {
            return DateTime.Now.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N")[..8];
        }
    }

    /// <summary>
    /// Stable logging categories following blueprint observability principles.
    /// Use these constants to ensure consistent category naming across the mod.
    /// </summary>
    public static class LogCategories
    {
        public const string Enlistment = "Enlistment";
        public const string Promotion = "Promotion";
        public const string Wages = "Wages";
        public const string GameAdapters = "GameAdapters";
        public const string Configuration = "Configuration";
        public const string Persistence = "Persistence";
        public const string Performance = "Performance";
        public const string Initialization = "Initialization";
    }
}
