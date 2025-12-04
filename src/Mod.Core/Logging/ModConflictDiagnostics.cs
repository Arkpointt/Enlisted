using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using HarmonyLib;

namespace Enlisted.Mod.Core.Logging
{
    /// <summary>
    ///     Centralized diagnostics for detecting mod conflicts and logging environment info.
    ///     Writes to a dedicated conflicts.log file in the Debugging folder for easy user access.
    ///     
    ///     This system is designed to be lightweight - it only runs at startup and when
    ///     deferred patches are applied. No per-frame or per-tick overhead.
    ///     
    ///     Usage:
    ///     - Call RunStartupDiagnostics() from OnSubModuleLoad after initial Harmony.PatchAll()
    ///     - Call RefreshDeferredPatches() from NextFrameDispatcherPatch after deferred patches apply
    /// </summary>
    public static class ModConflictDiagnostics
    {
        // Harmony IDs used by Enlisted - must match SubModule.cs
        private const string EnlistedHarmonyId = "com.enlisted.mod";
        private const string EnlistedDeferredHarmonyId = "com.enlisted.mod.deferred";

        private static readonly object Sync = new();
        private static string _conflictLogPath;
        private static bool _hasRunStartup;
        private static bool _hasDeferredPatchInfo;

        /// <summary>
        ///     Runs initial startup diagnostics and writes to Debugging/conflicts.log.
        ///     Call this once from OnSubModuleLoad after the main Harmony.PatchAll() completes.
        ///     
        ///     NOTE: This runs before deferred patches are applied, so those won't appear yet.
        ///     Call RefreshDeferredPatches() later to append deferred patch info.
        /// </summary>
        /// <param name="harmony">The main Harmony instance used by Enlisted</param>
        public static void RunStartupDiagnostics(Harmony harmony)
        {
            if (_hasRunStartup)
            {
                return;
            }

            try
            {
                _hasRunStartup = true;
                InitializeLogPath();
                ClearPreviousLog();

                WriteHeader();
                WriteEnvironmentInfo();
                WriteLoadedModules();
                WritePatchConflicts(harmony, "MAIN PATCHES", EnlistedHarmonyId);
                WriteEnlistedPatchList(harmony, "Main");
                WriteNoteAboutDeferredPatches();
                WriteFooter(isPartial: true);

                ModLogger.Info("Diagnostics", $"Initial conflict diagnostics written to: {_conflictLogPath}");
            }
            catch (Exception ex)
            {
                ModLogger.Error("Diagnostics", $"Failed to run startup diagnostics: {ex.Message}");
            }
        }

        /// <summary>
        ///     Appends deferred patch information to the existing diagnostics log.
        ///     Call this from NextFrameDispatcherPatch.Postfix after deferred patches are applied.
        ///     
        ///     This is separate from RunStartupDiagnostics because deferred patches are applied
        ///     on the first Campaign.Tick() to avoid TypeInitializationException on Proton/Linux.
        /// </summary>
        /// <param name="deferredHarmony">The deferred Harmony instance</param>
        public static void RefreshDeferredPatches(Harmony deferredHarmony)
        {
            if (_hasDeferredPatchInfo || string.IsNullOrWhiteSpace(_conflictLogPath))
            {
                return;
            }

            try
            {
                _hasDeferredPatchInfo = true;
                
                WriteLine();
                WriteLine(new string('=', 72));
                WriteLine("              DEFERRED PATCHES (Applied on Campaign Start)");
                WriteLine(new string('=', 72));
                WriteLine();
                WriteLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                WriteLine();

                WritePatchConflicts(deferredHarmony, "DEFERRED PATCHES", EnlistedDeferredHarmonyId);
                WriteEnlistedPatchList(deferredHarmony, "Deferred");
                WriteCombinedConflictSummary();
                WriteFooter(isPartial: false);

                ModLogger.Info("Diagnostics", "Deferred patch diagnostics appended to conflicts.log");
            }
            catch (Exception ex)
            {
                ModLogger.Error("Diagnostics", $"Failed to refresh deferred patch diagnostics: {ex.Message}");
            }
        }

        /// <summary>
        ///     Logs registered campaign behaviors for troubleshooting.
        ///     Call this from OnGameStart after all behaviors are registered.
        /// </summary>
        /// <param name="behaviorNames">List of behavior type names that were registered</param>
        public static void LogRegisteredBehaviors(IEnumerable<string> behaviorNames)
        {
            if (string.IsNullOrWhiteSpace(_conflictLogPath))
            {
                return;
            }

            try
            {
                var behaviors = behaviorNames?.ToList() ?? new List<string>();
                
                WriteLine();
                WriteLine("-- REGISTERED CAMPAIGN BEHAVIORS --");
                WriteLine();
                WriteLine($"  Total: {behaviors.Count} behaviors");
                WriteLine();

                foreach (var name in behaviors.OrderBy(n => n))
                {
                    WriteLine($"    {name}");
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("Diagnostics", $"Failed to log behaviors: {ex.Message}");
            }
        }

        #region Path Management

        /// <summary>
        ///     Resolves the path to conflicts.log in the Debugging folder alongside the module.
        /// </summary>
        private static void InitializeLogPath()
        {
            try
            {
                var assembly = typeof(ModConflictDiagnostics).Assembly;
                var assemblyLocation = assembly.Location;

                if (string.IsNullOrWhiteSpace(assemblyLocation))
                {
                    throw new InvalidOperationException("Assembly.Location is null or empty");
                }

                var dllDir = Path.GetDirectoryName(assemblyLocation);
                if (string.IsNullOrWhiteSpace(dllDir))
                {
                    throw new InvalidOperationException("Could not get directory from assembly location");
                }

                var binDir = Directory.GetParent(dllDir);
                if (binDir == null)
                {
                    throw new InvalidOperationException("Could not get parent directory");
                }

                var enlistedRoot = binDir.Parent;
                var debugDir = enlistedRoot != null
                    ? Path.Combine(enlistedRoot.FullName, "Debugging")
                    : "Debugging";

                if (!Directory.Exists(debugDir))
                {
                    Directory.CreateDirectory(debugDir);
                }

                _conflictLogPath = Path.Combine(debugDir, "conflicts.log");
            }
            catch (Exception ex)
            {
                // Fallback to Documents if module path resolution fails
                try
                {
                    var docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    var fallbackDir = Path.Combine(docs, "Mount and Blade II Bannerlord", "Logs", "Enlisted");
                    Directory.CreateDirectory(fallbackDir);
                    _conflictLogPath = Path.Combine(fallbackDir, "conflicts.log");
                    System.Diagnostics.Debug.WriteLine(
                        $"[Enlisted] Conflict log using Documents fallback (Error: {ex.Message})");
                }
                catch (Exception fallbackEx)
                {
                    // Last resort: use a temp path
                    _conflictLogPath = Path.Combine(Path.GetTempPath(), "Enlisted", "conflicts.log");
                    var tempDir = Path.GetDirectoryName(_conflictLogPath);
                    if (tempDir != null && !Directory.Exists(tempDir))
                    {
                        Directory.CreateDirectory(tempDir);
                    }

                    System.Diagnostics.Debug.WriteLine(
                        $"[Enlisted] Conflict log using temp fallback (Error: {fallbackEx.Message})");
                }
            }
        }

        /// <summary>
        ///     Clears the previous session's conflict log so only current session data is present.
        /// </summary>
        private static void ClearPreviousLog()
        {
            try
            {
                if (File.Exists(_conflictLogPath))
                {
                    File.Delete(_conflictLogPath);
                }
            }
            catch
            {
                // Best effort - don't crash over log cleanup
            }
        }

        /// <summary>
        ///     Writes a line to the conflicts.log file.
        /// </summary>
        private static void WriteLine(string text = "")
        {
            try
            {
                lock (Sync)
                {
                    File.AppendAllText(_conflictLogPath, text + Environment.NewLine, Encoding.UTF8);
                }
            }
            catch
            {
                // Silent fail - logging should never crash the game
            }
        }

        #endregion

        #region Header and Footer

        private static void WriteHeader()
        {
            WriteLine("========================================================================");
            WriteLine("              ENLISTED MOD - CONFLICT DIAGNOSTICS");
            WriteLine("========================================================================");
            WriteLine();
            WriteLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            WriteLine();
            WriteLine("This file helps diagnose mod conflicts. Share it when reporting issues.");
            WriteLine("NOTE: This mod uses two Harmony instances for stability:");
            WriteLine($"  - {EnlistedHarmonyId} (patches applied at mod load)");
            WriteLine($"  - {EnlistedDeferredHarmonyId} (patches applied when campaign starts)");
            WriteLine(new string('-', 72));
        }

        private static void WriteNoteAboutDeferredPatches()
        {
            WriteLine();
            WriteLine("-- NOTE --");
            WriteLine();
            WriteLine("  Deferred patches will be added below when the campaign starts.");
            WriteLine("  These patches are delayed to avoid crashes on Linux/Proton.");
            WriteLine("  If you only see this message, load a save to see full diagnostics.");
        }

        private static void WriteFooter(bool isPartial)
        {
            WriteLine();
            WriteLine(new string('-', 72));
            
            if (isPartial)
            {
                WriteLine("END OF INITIAL DIAGNOSTICS (deferred patches pending)");
            }
            else
            {
                WriteLine("END OF FULL DIAGNOSTICS");
            }
            
            WriteLine();
            WriteLine("If you're experiencing issues:");
            WriteLine("  1. Check the POTENTIAL CONFLICTS sections above");
            WriteLine("  2. Try disabling mods listed as sharing patches with Enlisted");
            WriteLine("  3. Include this file when reporting bugs on Nexus/GitHub");
            WriteLine();
            WriteLine("For support, visit the Enlisted mod page or GitHub repository.");
        }

        #endregion

        #region Environment Info

        /// <summary>
        ///     Logs game version, mod version, and runtime info for support tickets.
        /// </summary>
        private static void WriteEnvironmentInfo()
        {
            WriteLine();
            WriteLine("-- ENVIRONMENT --");
            WriteLine();

            string gameVersion;
            try
            {
                gameVersion = TaleWorlds.Library.ApplicationVersion.FromParametersFile().ToString();
            }
            catch
            {
                gameVersion = "Unknown";
            }

            // Pull version from SessionDiagnostics to avoid duplication
            var modVersion = SessionDiagnostics.ModVersion;
            var targetVersion = SessionDiagnostics.TargetGameVersion;

            WriteLine($"  Game Version:       {gameVersion}");
            WriteLine($"  Enlisted Version:   {modVersion}");
            WriteLine($"  Target Game:        {targetVersion}");
            WriteLine($"  CLR Version:        {Environment.Version}");
            WriteLine($"  OS:                 {Environment.OSVersion}");
            WriteLine($"  64-bit Process:     {Environment.Is64BitProcess}");
        }

        /// <summary>
        ///     Logs all loaded submodules so we know what mods were active when an issue occurred.
        /// </summary>
        private static void WriteLoadedModules()
        {
            WriteLine();
            WriteLine("-- LOADED MODULES --");
            WriteLine();

            try
            {
                // 1.3.4+ API: SubModules property replaced with CollectSubModules() method
                var subModules = TaleWorlds.MountAndBlade.Module.CurrentModule?.CollectSubModules();
                if (subModules == null || subModules.Count == 0)
                {
                    WriteLine("  (No modules detected - early load stage)");
                    return;
                }

                WriteLine($"  Total: {subModules.Count} modules");
                WriteLine();

                var index = 1;
                foreach (var subModule in subModules)
                {
                    var typeName = subModule?.GetType().FullName ?? "Unknown";
                    var isEnlisted = typeName.Contains("Enlisted") ? " <-- THIS MOD" : "";
                    WriteLine($"  {index,2}. {typeName}{isEnlisted}");
                    index++;
                }
            }
            catch (Exception ex)
            {
                WriteLine($"  ERROR: Failed to enumerate modules - {ex.Message}");
            }
        }

        #endregion

        #region Conflict Detection

        /// <summary>
        ///     Detects when other mods patch the same methods as Enlisted and logs potential conflicts.
        ///     This is the key diagnostic for mod interference issues.
        /// </summary>
        private static void WritePatchConflicts(Harmony harmony, string sectionTitle, string ourHarmonyId)
        {
            WriteLine();
            WriteLine($"-- HARMONY PATCH CONFLICT ANALYSIS ({sectionTitle}) --");
            WriteLine();

            try
            {
                var ourMethods = harmony.GetPatchedMethods().ToList();
                WriteLine($"  Enlisted patches ({sectionTitle.ToLower()}): {ourMethods.Count} methods");
                WriteLine();

                var conflicts = new List<(string method, string declaringType, List<string> otherMods)>();

                foreach (var method in ourMethods)
                {
                    var patchInfo = Harmony.GetPatchInfo(method);
                    if (patchInfo == null)
                    {
                        continue;
                    }

                    // Gather all mod owners that touch this method
                    var owners = new HashSet<string>();
                    foreach (var p in patchInfo.Prefixes)
                    {
                        owners.Add(p.owner);
                    }

                    foreach (var p in patchInfo.Postfixes)
                    {
                        owners.Add(p.owner);
                    }

                    foreach (var p in patchInfo.Transpilers)
                    {
                        owners.Add(p.owner);
                    }

                    foreach (var p in patchInfo.Finalizers)
                    {
                        owners.Add(p.owner);
                    }

                    // If more than just Enlisted patches this method, we have a potential conflict
                    // Check both our Harmony IDs since we use two instances
                    var ourIds = new HashSet<string> { EnlistedHarmonyId, EnlistedDeferredHarmonyId, ourHarmonyId };
                    var otherMods = owners.Where(o => !ourIds.Contains(o)).ToList();
                    
                    if (otherMods.Count > 0)
                    {
                        var methodName = method.Name;
                        var declaringType = method.DeclaringType?.FullName ?? "Unknown";
                        conflicts.Add((methodName, declaringType, otherMods));
                    }
                }

                if (conflicts.Count == 0)
                {
                    WriteLine("  [OK] NO CONFLICTS DETECTED");
                    WriteLine();
                    WriteLine("  All Enlisted patches are exclusive - no other mods are patching");
                    WriteLine("  the same game methods. If you're having issues, the problem is");
                    WriteLine("  likely not a mod conflict.");
                }
                else
                {
                    WriteLine($"  [!] POTENTIAL CONFLICTS: {conflicts.Count}");
                    WriteLine();
                    WriteLine("  The following methods are patched by both Enlisted and other mods.");
                    WriteLine("  This doesn't always cause problems, but may explain unexpected behavior.");
                    WriteLine();

                    // Group by other mod for easier reading
                    var byMod = conflicts
                        .SelectMany(c => c.otherMods.Select(m => new { Mod = m, c.declaringType, c.method }))
                        .GroupBy(x => x.Mod)
                        .OrderBy(g => g.Key);

                    foreach (var modGroup in byMod)
                    {
                        WriteLine($"  Mod: {modGroup.Key}");
                        WriteLine($"    Shared patches: {modGroup.Count()}");
                        foreach (var patch in modGroup.Take(10)) // Limit to first 10 to avoid spam
                        {
                            WriteLine($"      - {patch.declaringType}.{patch.method}");
                        }

                        if (modGroup.Count() > 10)
                        {
                            WriteLine($"      ... and {modGroup.Count() - 10} more");
                        }

                        WriteLine();
                    }

                    // Write detailed patch order for conflicts (limited to avoid huge logs)
                    if (conflicts.Count <= 20)
                    {
                        WritePatchExecutionOrder(harmony, ourMethods, conflicts, ourHarmonyId);
                    }
                    else
                    {
                        WriteLine("  (Patch execution order skipped - too many conflicts to display)");
                        WriteLine("  Try disabling other mods one at a time to identify the source.");
                    }
                }
            }
            catch (Exception ex)
            {
                WriteLine($"  ERROR: Conflict analysis failed - {ex.Message}");
            }
        }

        /// <summary>
        ///     Logs the execution order of patches on conflicting methods to understand who runs first.
        ///     Patch order matters: prefixes can skip the original method, postfixes can modify results.
        /// </summary>
        private static void WritePatchExecutionOrder(
            Harmony harmony, 
            List<MethodBase> ourMethods,
            List<(string method, string declaringType, List<string> otherMods)> conflicts,
            string ourHarmonyId)
        {
            WriteLine("  -- Patch Execution Order --");
            WriteLine();
            WriteLine("  (Higher priority prefixes run first and can skip original method)");
            WriteLine("  (Lower priority postfixes run first after the original method)");
            WriteLine();

            var ourIds = new HashSet<string> { EnlistedHarmonyId, EnlistedDeferredHarmonyId, ourHarmonyId };

            foreach (var method in ourMethods)
            {
                var patchInfo = Harmony.GetPatchInfo(method);
                if (patchInfo == null)
                {
                    continue;
                }

                var methodName = method.Name;
                var declaringType = method.DeclaringType?.FullName ?? "Unknown";
                
                if (!conflicts.Any(c => c.method == methodName && c.declaringType == declaringType))
                {
                    continue;
                }

                WriteLine($"  {declaringType}.{methodName}:");

                if (patchInfo.Prefixes.Count > 0)
                {
                    WriteLine("    Prefixes (run before original, highest priority first):");
                    foreach (var p in patchInfo.Prefixes.OrderByDescending(x => x.priority))
                    {
                        var marker = ourIds.Contains(p.owner) ? "<- Enlisted" : "";
                        WriteLine($"      [{p.priority,4}] {p.owner} {marker}");
                    }
                }

                if (patchInfo.Postfixes.Count > 0)
                {
                    WriteLine("    Postfixes (run after original, lowest priority first):");
                    foreach (var p in patchInfo.Postfixes.OrderBy(x => x.priority))
                    {
                        var marker = ourIds.Contains(p.owner) ? "<- Enlisted" : "";
                        WriteLine($"      [{p.priority,4}] {p.owner} {marker}");
                    }
                }

                if (patchInfo.Transpilers.Count > 0)
                {
                    WriteLine("    Transpilers (modify IL code):");
                    foreach (var p in patchInfo.Transpilers)
                    {
                        var marker = ourIds.Contains(p.owner) ? "<- Enlisted" : "";
                        WriteLine($"      {p.owner} {marker}");
                    }
                }

                WriteLine();
            }
        }

        /// <summary>
        ///     Writes a combined summary of all conflicts from both Harmony instances.
        /// </summary>
        private static void WriteCombinedConflictSummary()
        {
            WriteLine();
            WriteLine("-- COMBINED CONFLICT SUMMARY --");
            WriteLine();

            try
            {
                // Gather all patched methods from both Harmony instances
                var mainHarmony = new Harmony(EnlistedHarmonyId);
                var deferredHarmony = new Harmony(EnlistedDeferredHarmonyId);

                var allMethods = mainHarmony.GetPatchedMethods()
                    .Union(deferredHarmony.GetPatchedMethods())
                    .Distinct()
                    .ToList();

                var ourIds = new HashSet<string> { EnlistedHarmonyId, EnlistedDeferredHarmonyId };
                var allOtherMods = new HashSet<string>();

                foreach (var method in allMethods)
                {
                    var patchInfo = Harmony.GetPatchInfo(method);
                    if (patchInfo == null)
                    {
                        continue;
                    }

                    foreach (var p in patchInfo.Prefixes.Where(p => !ourIds.Contains(p.owner)))
                    {
                        allOtherMods.Add(p.owner);
                    }

                    foreach (var p in patchInfo.Postfixes.Where(p => !ourIds.Contains(p.owner)))
                    {
                        allOtherMods.Add(p.owner);
                    }

                    foreach (var p in patchInfo.Transpilers.Where(p => !ourIds.Contains(p.owner)))
                    {
                        allOtherMods.Add(p.owner);
                    }

                    foreach (var p in patchInfo.Finalizers.Where(p => !ourIds.Contains(p.owner)))
                    {
                        allOtherMods.Add(p.owner);
                    }
                }

                WriteLine($"  Total Enlisted patches: {allMethods.Count} methods");
                WriteLine($"  Other mods sharing patches: {allOtherMods.Count}");
                WriteLine();

                if (allOtherMods.Count == 0)
                {
                    WriteLine("  [OK] No conflicts detected across all patches.");
                }
                else
                {
                    WriteLine("  Mods with shared patches (potential conflict sources):");
                    foreach (var mod in allOtherMods.OrderBy(m => m))
                    {
                        WriteLine($"    - {mod}");
                    }
                }
            }
            catch (Exception ex)
            {
                WriteLine($"  ERROR: Could not generate combined summary - {ex.Message}");
            }
        }

        #endregion

        #region Patch Listing

        /// <summary>
        ///     Lists all methods that Enlisted patches, categorized by purpose.
        ///     Useful for users to understand what game behavior the mod touches.
        /// </summary>
        private static void WriteEnlistedPatchList(Harmony harmony, string label)
        {
            WriteLine();
            WriteLine($"-- ENLISTED PATCH LIST ({label}) --");
            WriteLine();

            try
            {
                var patchedMethods = harmony.GetPatchedMethods().ToList();

                // Categorize patches for easier reading
                var categories = new Dictionary<string, List<string>>
                {
                    { "Army/Party", new List<string>() },
                    { "Encounter", new List<string>() },
                    { "Kingdom/Clan", new List<string>() },
                    { "Finance", new List<string>() },
                    { "UI/Menu", new List<string>() },
                    { "Combat", new List<string>() },
                    { "Other", new List<string>() }
                };

                foreach (var method in patchedMethods.OrderBy(m => m.DeclaringType?.Name))
                {
                    var typeName = method.DeclaringType?.Name ?? "Unknown";
                    var fullName = $"{typeName}.{method.Name}";

                    // Simple categorization based on type name
                    if (typeName.Contains("Army") || typeName.Contains("Party") || typeName.Contains("Mobile"))
                    {
                        categories["Army/Party"].Add(fullName);
                    }
                    else if (typeName.Contains("Encounter") || typeName.Contains("Map"))
                    {
                        categories["Encounter"].Add(fullName);
                    }
                    else if (typeName.Contains("Kingdom") || typeName.Contains("Clan") || typeName.Contains("Relation"))
                    {
                        categories["Kingdom/Clan"].Add(fullName);
                    }
                    else if (typeName.Contains("Finance") || typeName.Contains("Gold") || typeName.Contains("Income") ||
                             typeName.Contains("Expense"))
                    {
                        categories["Finance"].Add(fullName);
                    }
                    else if (typeName.Contains("Menu") || typeName.Contains("UI") || typeName.Contains("Screen") ||
                             typeName.Contains("Message"))
                    {
                        categories["UI/Menu"].Add(fullName);
                    }
                    else if (typeName.Contains("Battle") || typeName.Contains("Combat") || typeName.Contains("Mission"))
                    {
                        categories["Combat"].Add(fullName);
                    }
                    else
                    {
                        categories["Other"].Add(fullName);
                    }
                }

                WriteLine($"  Total: {patchedMethods.Count} methods patched");
                WriteLine();

                foreach (var category in categories.Where(c => c.Value.Count > 0))
                {
                    WriteLine($"  [{category.Key}] ({category.Value.Count})");
                    foreach (var method in category.Value)
                    {
                        WriteLine($"    - {method}");
                    }

                    WriteLine();
                }
            }
            catch (Exception ex)
            {
                WriteLine($"  ERROR: Failed to list patches - {ex.Message}");
            }
        }

        #endregion
    }
}
