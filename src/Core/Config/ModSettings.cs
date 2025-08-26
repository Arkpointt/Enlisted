using System.IO;
using System.Xml.Serialization;

namespace Enlisted.Core.Config
{
    /// <summary>
    /// Centralized mod configuration following blueprint config-over-code principle.
    /// Provides strongly-typed settings with validation and safe defaults.
    /// Supports file-based configuration with fallback to defaults.
    /// 
    /// Updated to support centralized logging configuration.
    /// </summary>
    public class ModSettings
    {
        /// <summary>Daily wage paid to enlisted players in gold pieces.</summary>
        public int DailyWage { get; set; } = 10;

        /// <summary>Enable debug logging for development builds and troubleshooting.</summary>
        public bool EnableDebugLogging { get; set; } = false;

        /// <summary>Feature flag for advanced party hiding mechanics.</summary>
        public bool UseAdvancedPartyHiding { get; set; } = true;

        /// <summary>Enable performance logging to monitor frame-time impact.</summary>
        public bool EnablePerformanceLogging { get; set; } = false;

        /// <summary>Show detailed logging messages to players (for debugging).</summary>
        public bool ShowVerboseMessages { get; set; } = false;

        /// <summary>Log level threshold: 0=Debug, 1=Info, 2=Warning, 3=Error.</summary>
        public int LogLevel { get; set; } = 1; // Default to Info level

        private static ModSettings _instance;
        public static ModSettings Instance => _instance ??= Load();

        /// <summary>
        /// Load settings from XML file with safe fallback to defaults.
        /// Follows blueprint principle of fail-closed configuration loading.
        /// </summary>
        private static ModSettings Load()
        {
            var moduleDir = System.IO.Path.Combine(
                System.IO.Path.GetDirectoryName(typeof(ModSettings).Assembly.Location) ?? "",
                "..", ".."); // Navigate from bin\Win64_Shipping_Client\ back to Modules\Enlisted\
            var path = Path.GetFullPath(Path.Combine(moduleDir, "settings.xml"));

            try
            {
                if (File.Exists(path))
                {
                    var serializer = new XmlSerializer(typeof(ModSettings));
                    using var fileStream = File.OpenRead(path);
                    var settings = (ModSettings)serializer.Deserialize(fileStream);
                    
                    // Validate loaded settings
                    return ValidateSettings(settings);
                }
            }
            catch
            {
                // Fail closed: Return defaults if loading fails
                // Note: Can't use centralized logging here as this runs before logging initialization
            }

            return new ModSettings();
        }

        /// <summary>
        /// Validate settings values and apply safe bounds.
        /// Follows blueprint principle of fail-fast with actionable errors.
        /// </summary>
        private static ModSettings ValidateSettings(ModSettings settings)
        {
            // Ensure wage is within reasonable bounds
            if (settings.DailyWage < 0 || settings.DailyWage > 1000)
            {
                settings.DailyWage = 10; // Reset to default
            }

            // Validate log level
            if (settings.LogLevel < 0 || settings.LogLevel > 3)
            {
                settings.LogLevel = 1; // Reset to Info
            }

            return settings;
        }

        /// <summary>
        /// Save current settings to XML file.
        /// Used by future in-game configuration UI.
        /// </summary>
        public void Save()
        {
            try
            {
                var moduleDir = System.IO.Path.Combine(
                    System.IO.Path.GetDirectoryName(typeof(ModSettings).Assembly.Location) ?? "",
                    "..", "..");
                var path = Path.GetFullPath(Path.Combine(moduleDir, "settings.xml"));

                var serializer = new XmlSerializer(typeof(ModSettings));
                using var fileStream = File.Create(path);
                serializer.Serialize(fileStream, this);
            }
            catch
            {
                // Fail silently - saving is not critical
            }
        }
    }
}
