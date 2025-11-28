using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using HarmonyLib;
using TaleWorlds.MountAndBlade;

namespace Enlisted.Mod.Core.Logging
{
    /// <summary>
    /// Centralized diagnostics for detecting mod conflicts and logging environment info.
    /// Writes to a dedicated conflicts.log file in the Debugging folder for easy user access.
    /// Called once at startup from SubModule - no changes needed to patches or behaviors.
    /// </summary>
    public static class ModConflictDiagnostics
    {
        private const string EnlistedHarmonyId = "com.enlisted.mod";
        private const string EnlistedVersion = "v0.3.0";
        
        private static readonly object Sync = new object();
        private static string _conflictLogPath;

        /// <summary>
        /// Runs all startup diagnostics and writes to Debugging/conflicts.log.
        /// Call this once from OnSubModuleLoad after Harmony.PatchAll().
        /// </summary>
        /// <param name="harmony">The Harmony instance used by Enlisted</param>
        public static void RunStartupDiagnostics(Harmony harmony)
        {
            try
            {
                InitializeLogPath();
                ClearPreviousLog();
                
                WriteHeader();
                WriteEnvironmentInfo();
                WriteLoadedModules();
                WritePatchConflicts(harmony);
                WriteEnlistedPatchList(harmony);
                WriteFooter();
                
                // Log a summary to main log so users know where to find conflict info
                ModLogger.Info("Diagnostics", $"Conflict diagnostics written to: {_conflictLogPath}");
            }
            catch (Exception ex)
            {
                ModLogger.Error("Diagnostics", $"Failed to run startup diagnostics: {ex.Message}");
            }
        }

        /// <summary>
        /// Resolves the path to conflicts.log in the Debugging folder alongside the module.
        /// </summary>
        private static void InitializeLogPath()
        {
            try
            {
                var dllDir = Path.GetDirectoryName(typeof(ModConflictDiagnostics).Assembly.Location);
                var binDir = Directory.GetParent(dllDir);
                var enlistedRoot = binDir?.Parent;
                var debugDir = enlistedRoot != null 
                    ? Path.Combine(enlistedRoot.FullName, "Debugging") 
                    : "Debugging";
                
                if (!Directory.Exists(debugDir))
                {
                    Directory.CreateDirectory(debugDir);
                }
                
                _conflictLogPath = Path.Combine(debugDir, "conflicts.log");
            }
            catch
            {
                // Fallback to Documents if module path resolution fails
                var docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                var fallbackDir = Path.Combine(docs, "Mount and Blade II Bannerlord", "Logs", "Enlisted");
                Directory.CreateDirectory(fallbackDir);
                _conflictLogPath = Path.Combine(fallbackDir, "conflicts.log");
            }
        }

        /// <summary>
        /// Clears the previous session's conflict log so only current session data is present.
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
        /// Writes a line to the conflicts.log file.
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

        private static void WriteHeader()
        {
            WriteLine("========================================================================");
            WriteLine("              ENLISTED MOD - CONFLICT DIAGNOSTICS");
            WriteLine("========================================================================");
            WriteLine();
            WriteLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            WriteLine();
            WriteLine("This file helps diagnose mod conflicts. Share it when reporting issues.");
            WriteLine(new string('-', 72));
        }

        private static void WriteFooter()
        {
            WriteLine();
            WriteLine(new string('-', 72));
            WriteLine("END OF DIAGNOSTICS");
            WriteLine();
            WriteLine("If you're experiencing issues:");
            WriteLine("  1. Check the POTENTIAL CONFLICTS section above");
            WriteLine("  2. Try disabling mods listed as sharing patches with Enlisted");
            WriteLine("  3. Include this file when reporting bugs on Nexus/GitHub");
        }

        /// <summary>
        /// Logs game version, mod version, and runtime info for support tickets.
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

            WriteLine($"  Game Version:     {gameVersion}");
            WriteLine($"  Enlisted Version: {EnlistedVersion}");
            WriteLine($"  CLR Version:      {Environment.Version}");
            WriteLine($"  OS:               {Environment.OSVersion}");
        }

        /// <summary>
        /// Logs all loaded submodules so we know what mods were active when an issue occurred.
        /// </summary>
        private static void WriteLoadedModules()
        {
            WriteLine();
            WriteLine("-- LOADED MODULES --");
            WriteLine();
            
            try
            {
                // 1.3.4 API: SubModules property replaced with CollectSubModules() method
                var subModules = Module.CurrentModule?.CollectSubModules();
                if (subModules == null || subModules.Count == 0)
                {
                    WriteLine("  (No modules detected - early load stage)");
                    return;
                }

                WriteLine($"  Total: {subModules.Count} modules");
                WriteLine();
                
                int index = 1;
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

        /// <summary>
        /// Detects when other mods patch the same methods as Enlisted and logs potential conflicts.
        /// This is the key diagnostic for mod interference issues.
        /// </summary>
        private static void WritePatchConflicts(Harmony harmony)
        {
            WriteLine();
            WriteLine("-- HARMONY PATCH CONFLICT ANALYSIS --");
            WriteLine();
            
            try
            {
                var ourMethods = harmony.GetPatchedMethods().ToList();
                WriteLine($"  Enlisted patches: {ourMethods.Count} methods");
                WriteLine();
                
                var conflicts = new List<(string method, List<string> otherMods)>();

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
                    if (owners.Count > 1)
                    {
                        var methodName = $"{method.DeclaringType?.Name}.{method.Name}";
                        var otherMods = owners.Where(o => o != EnlistedHarmonyId).ToList();
                        conflicts.Add((methodName, otherMods));
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
                    
                    foreach (var (methodName, otherMods) in conflicts)
                    {
                        WriteLine($"  Method: {methodName}");
                        WriteLine($"    Also patched by: {string.Join(", ", otherMods)}");
                        WriteLine();
                    }
                    
                    // Write detailed patch order for conflicts
                    WritePatchExecutionOrder(harmony, ourMethods, conflicts);
                }
            }
            catch (Exception ex)
            {
                WriteLine($"  ERROR: Conflict analysis failed - {ex.Message}");
            }
        }

        /// <summary>
        /// Logs the execution order of patches on conflicting methods to understand who runs first.
        /// Patch order matters: prefixes can skip the original method, postfixes can modify results.
        /// </summary>
        private static void WritePatchExecutionOrder(Harmony harmony, List<System.Reflection.MethodBase> ourMethods, 
            List<(string method, List<string> otherMods)> conflicts)
        {
            WriteLine("  -- Patch Execution Order --");
            WriteLine();
            WriteLine("  (Higher priority prefixes run first and can skip original method)");
            WriteLine("  (Lower priority postfixes run first after the original method)");
            WriteLine();
            
            foreach (var method in ourMethods)
            {
                var patchInfo = Harmony.GetPatchInfo(method);
                if (patchInfo == null)
                {
                    continue;
                }
                
                var methodName = $"{method.DeclaringType?.Name}.{method.Name}";
                if (!conflicts.Any(c => c.method == methodName))
                {
                    continue;
                }
                
                WriteLine($"  {methodName}:");
                
                if (patchInfo.Prefixes.Count > 0)
                {
                    WriteLine("    Prefixes (run before original, highest priority first):");
                    foreach (var p in patchInfo.Prefixes.OrderByDescending(x => x.priority))
                    {
                        var marker = p.owner == EnlistedHarmonyId ? "<-" : "  ";
                        WriteLine($"      {marker} [{p.priority,4}] {p.owner}");
                    }
                }
                
                if (patchInfo.Postfixes.Count > 0)
                {
                    WriteLine("    Postfixes (run after original, lowest priority first):");
                    foreach (var p in patchInfo.Postfixes.OrderBy(x => x.priority))
                    {
                        var marker = p.owner == EnlistedHarmonyId ? "<-" : "  ";
                        WriteLine($"      {marker} [{p.priority,4}] {p.owner}");
                    }
                }
                
                if (patchInfo.Transpilers.Count > 0)
                {
                    WriteLine("    Transpilers (modify IL code):");
                    foreach (var p in patchInfo.Transpilers)
                    {
                        var marker = p.owner == EnlistedHarmonyId ? "<-" : "  ";
                        WriteLine($"      {marker} {p.owner}");
                    }
                }
                
                WriteLine();
            }
        }

        /// <summary>
        /// Lists all methods that Enlisted patches for reference.
        /// Useful for users to understand what game behavior the mod touches.
        /// </summary>
        private static void WriteEnlistedPatchList(Harmony harmony)
        {
            WriteLine();
            WriteLine("-- ENLISTED PATCH LIST --");
            WriteLine();
            WriteLine("  Methods patched by Enlisted (for reference):");
            WriteLine();
            
            try
            {
                var patchedMethods = harmony.GetPatchedMethods().ToList();
                foreach (var method in patchedMethods.OrderBy(m => m.DeclaringType?.Name))
                {
                    var methodName = $"{method.DeclaringType?.Name}.{method.Name}";
                    WriteLine($"    {methodName}");
                }
            }
            catch (Exception ex)
            {
                WriteLine($"  ERROR: Failed to list patches - {ex.Message}");
            }
        }
    }
}

