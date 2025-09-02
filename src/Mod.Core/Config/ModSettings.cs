using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace Enlisted.Mod.Core.Config
{
	/// <summary>
	/// Strongly-typed mod settings loaded from ModuleData/Enlisted/settings.json.
	/// Defaults are safe and minimal. Fails closed if file is missing or invalid.
	/// </summary>
	[DataContract]
	public sealed class ModSettings
	{
		[DataMember(Name = "LogMenus")]
		public bool LogMenus { get; set; } = true;

		[DataMember(Name = "LogDialogs")]
		public bool LogDialogs { get; set; } = true;

		[DataMember(Name = "LogCampaignEvents")]
		public bool LogCampaignEvents { get; set; } = true;
		[DataMember(Name = "DiscoveryPlayerOnly")]
		public bool DiscoveryPlayerOnly { get; set; } = true;


		[DataMember(Name = "DiscoveryStackTraces")]
		public bool DiscoveryStackTraces { get; set; } = false;

		[DataMember(Name = "LogApiCalls")]
		public bool LogApiCalls { get; set; } = false;

		/// <summary>
		/// API call detail level. Supported: "summary" or "verbose" (case-insensitive).
		/// </summary>
		[DataMember(Name = "ApiCallDetail")]
		public string ApiCallDetail { get; set; } = "summary";

		/// <summary>
		/// SAS settings for party-first enlistment/attachment.
		/// </summary>
		[DataMember(Name = "SAS")]
		public SASSettings SAS { get; set; } = new SASSettings();

		[DataContract]
		public sealed class SASSettings
		{
			[DataMember(Name = "AttachWhenClose")]
			public bool AttachWhenClose { get; set; } = true;

			[DataMember(Name = "AttachRange")]
			public double AttachRange { get; set; } = 0.6;

			[DataMember(Name = "TrailDistance")]
			public double TrailDistance { get; set; } = 1.2;

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


