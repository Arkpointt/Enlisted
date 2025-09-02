using System;
using System.Diagnostics;
using Enlisted.Mod.Core.Config;

namespace Enlisted.Mod.Core.Logging
{
	/// <summary>
	/// Helper for structured API call logging with timing and detail control.
	/// </summary>
	public static class ApiLog
	{
		public static IDisposable Scope(string targetDescription, string argsSummary)
		{
			return new ApiScope(targetDescription, argsSummary);
		}

		private sealed class ApiScope : IDisposable
		{
			private readonly string _target;
			private readonly string _args;
			private readonly Stopwatch _sw;

			public ApiScope(string target, string args)
			{
				_target = target;
				_args = args;
				_sw = Stopwatch.StartNew();
				if (ModConfig.Settings.LogApiCalls)
				{
					ModLogger.Api("INFO", $"CALL {_target} args={_args}");
				}
			}

			public void Dispose()
			{
				_sw.Stop();
				if (ModConfig.Settings.LogApiCalls)
				{
					ModLogger.Api("INFO", $"RET  {_target} durationMs={_sw.ElapsedMilliseconds}");
				}
			}

			public void Exception(Exception ex)
			{
				if (ModConfig.Settings.LogApiCalls)
				{
					var detail = string.Equals(ModConfig.Settings.ApiCallDetail, "verbose", StringComparison.OrdinalIgnoreCase)
						? ex.ToString()
						: ex.Message;
					ModLogger.Api("ERROR", $"THROW {_target} {detail}");
				}
			}
		}
	}
}


