using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace Enlisted.Mod.Core.Config
{
	/// <summary>
	/// Strongly-typed mod settings loaded from ModuleData/Enlisted/settings.json.
	/// Contains configuration options for logging, discovery, and encounter management.
	/// Defaults are safe and minimal. Fails closed if file is missing or invalid.
	/// </summary>
	[DataContract]
	public sealed class ModSettings
	{
		/// <summary>
		/// Whether to log menu open/close events for debugging.
		/// </summary>
		[DataMember(Name = "LogMenus")]
		public bool LogMenus { get; set; } = true;

		/// <summary>
		/// Whether to log dialog/conversation events for debugging.
		/// </summary>
		[DataMember(Name = "LogDialogs")]
		public bool LogDialogs { get; set; } = true;

		/// <summary>
		/// Whether to log campaign events (hourly/daily ticks, etc.) for debugging.
		/// </summary>
		[DataMember(Name = "LogCampaignEvents")]
		public bool LogCampaignEvents { get; set; } = true;
		
		/// <summary>
		/// Whether discovery logging should only track player-related events.
		/// When true, only logs events involving the player party.
		/// </summary>
		[DataMember(Name = "DiscoveryPlayerOnly")]
		public bool DiscoveryPlayerOnly { get; set; } = true;

		/// <summary>
		/// Whether discovery logging should include stack traces for detailed debugging.
		/// When true, includes full stack traces which can be very verbose.
		/// </summary>
		[DataMember(Name = "DiscoveryStackTraces")]
		public bool DiscoveryStackTraces { get; set; } = false;

		/// <summary>
		/// Whether to log API calls for debugging Bannerlord API usage.
		/// When true, logs method calls and parameters to the API log file.
		/// </summary>
		[DataMember(Name = "LogApiCalls")]
		public bool LogApiCalls { get; set; } = false;

		/// <summary>
		/// API call detail level. Supported: "summary" or "verbose" (case-insensitive).
		/// Summary provides basic call information, verbose includes detailed parameters.
		/// </summary>
		[DataMember(Name = "ApiCallDetail")]
		public string ApiCallDetail { get; set; } = "summary";

		/// <summary>
		/// Whether to run mod conflict diagnostics at startup and write to conflicts.log.
		/// Useful for diagnosing issues caused by other mods patching the same game methods.
		/// When disabled, the conflicts.log file will not be generated.
		/// </summary>
		[DataMember(Name = "LogModConflicts")]
		public bool LogModConflicts { get; set; } = true;

		/// <summary>
		/// Settings for encounter suppression and party attachment behavior.
		/// Controls when encounters are prevented and how party following works.
		/// </summary>
		[DataMember(Name = "Encounter")]
		public EncounterSettings Encounter { get; set; } = new EncounterSettings();

		/// <summary>
		/// Settings for encounter suppression and party attachment behavior.
		/// Controls when encounters are prevented and how party following works
		/// when the player is enlisted with a lord.
		/// </summary>
		[DataContract]
		public sealed class EncounterSettings
		{
			/// <summary>
			/// Whether to attach the player's party to the lord's party when close.
			/// When true, the player party follows the lord more closely.
			/// </summary>
			[DataMember(Name = "AttachWhenClose")]
			public bool AttachWhenClose { get; set; } = true;

			/// <summary>
			/// Distance threshold for attaching to the lord's party, in game units.
			/// When the player is within this distance, party attachment is attempted.
			/// </summary>
			[DataMember(Name = "AttachRange")]
			public double AttachRange { get; set; } = 0.6;

			/// <summary>
			/// Distance for trailing behind the lord's party, in game units.
			/// When following, the player party maintains this distance behind the lord.
			/// </summary>
			[DataMember(Name = "TrailDistance")]
			public double TrailDistance { get; set; } = 1.2;

			/// <summary>
			/// Whether to suppress unwanted player encounters while enlisted.
			/// When true, random encounters are prevented while still allowing battle participation.
			/// </summary>
			[DataMember(Name = "SuppressPlayerEncounter")]
			public bool SuppressPlayerEncounter { get; set; } = true;
		}

		/// <summary>
		/// Attempts to load settings from the installed module's ModuleData folder.
		/// On failure, returns defaults and does not throw.
		/// </summary>
		public static ModSettings LoadFromModule()
		{
			try
			{
				var settingsPath = ResolveSettingsPath();
				if (!File.Exists(settingsPath))
				{
					return new ModSettings();
				}

				using (var stream = File.OpenRead(settingsPath))
				{
					var serializer = new DataContractJsonSerializer(typeof(ModSettings));
					var loaded = serializer.ReadObject(stream) as ModSettings;
					return loaded ?? new ModSettings();
				}
			}
			catch
			{
				// Fail closed; avoid throwing during module load
				return new ModSettings();
			}
		}

		/// <summary>
		/// Resolves absolute path to ModuleData/Enlisted/settings.json from the executing assembly location.
		/// </summary>
		private static string ResolveSettingsPath()
		{
			// Executing DLL is at: .../Modules/Enlisted/bin/Win64_*/Enlisted.dll
			var dllDir = Path.GetDirectoryName(typeof(ModSettings).Assembly.Location);
			if (string.IsNullOrWhiteSpace(dllDir))
			{
				return "settings.json"; // harmless fallback; will not exist
			}

			var moduleRoot = Directory.GetParent(dllDir); // Win64_*
			moduleRoot = moduleRoot?.Parent; // bin
			moduleRoot = moduleRoot?.Parent; // Enlisted (module root)

			var path = moduleRoot != null
				? Path.Combine(moduleRoot.FullName, "ModuleData", "Enlisted", "settings.json")
				: "settings.json";
			return path;
		}
	}
}


