using System;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace Enlisted.Mod.Core.Logging
{
    /// <summary>
    /// Archives previous session logs before they're cleared by ModLogger.Initialize().
    /// Creates timestamped zip files in the Debugging folder for easy user bug reporting.
    /// Keeps only the last N archives to prevent disk bloat.
    /// </summary>
    public static class SessionArchiver
    {
        private const int MaxArchivesToKeep = 2;
        private const string ArchivePrefix = "session_";
        
        // Log files to include in archives
        private static readonly string[] LogFileNames = 
        {
            "enlisted.log",
            "conflicts.log",
            "discovery.log",
            "dialog.log",
            "api.log"
        };

        /// <summary>
        /// Archives the previous session's logs before they're cleared.
        /// Call this BEFORE ModLogger.Initialize() to capture the previous session.
        /// </summary>
        public static void ArchivePreviousSession()
        {
            try
            {
                var debugDir = ResolveDebuggingPath();
                if (string.IsNullOrEmpty(debugDir) || !Directory.Exists(debugDir))
                {
                    return; // No debugging folder yet, nothing to archive
                }

                // Check if there are any log files worth archiving
                var logsToArchive = LogFileNames
                    .Select(name => Path.Combine(debugDir, name))
                    .Where(path => File.Exists(path) && new FileInfo(path).Length > 0)
                    .ToList();

                if (logsToArchive.Count == 0)
                {
                    return; // No logs to archive
                }

                // Create the archive
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HHmmss");
                var archiveName = $"{ArchivePrefix}{timestamp}.zip";
                var archivePath = Path.Combine(debugDir, archiveName);

                CreateArchive(archivePath, logsToArchive);

                // Clean up old archives, keeping only the most recent ones
                CleanupOldArchives(debugDir);
            }
            catch
            {
                // Archiving should never crash the game - fail silently
                // We can't even log this error because the logger isn't initialized yet
            }
        }

        /// <summary>
        /// Creates a zip archive containing the specified log files.
        /// </summary>
        private static void CreateArchive(string archivePath, System.Collections.Generic.List<string> logFiles)
        {
            try
            {
                // Delete existing archive with same name if it exists (shouldn't happen with timestamps)
                if (File.Exists(archivePath))
                {
                    File.Delete(archivePath);
                }

                using (var archive = ZipFile.Open(archivePath, ZipArchiveMode.Create))
                {
                    foreach (var logPath in logFiles)
                    {
                        var fileName = Path.GetFileName(logPath);
                        
                        // Read the file content and write to archive
                        // We read first to avoid file locking issues
                        try
                        {
                            var content = File.ReadAllBytes(logPath);
                            var entry = archive.CreateEntry(fileName, CompressionLevel.Optimal);
                            using (var entryStream = entry.Open())
                            {
                                entryStream.Write(content, 0, content.Length);
                            }
                        }
                        catch
                        {
                            // Skip files we can't read (might be locked)
                        }
                    }
                }
            }
            catch
            {
                // If archive creation fails, just continue without it
            }
        }

        /// <summary>
        /// Removes old session archives, keeping only the most recent ones.
        /// </summary>
        private static void CleanupOldArchives(string debugDir)
        {
            try
            {
                var archives = Directory.GetFiles(debugDir, $"{ArchivePrefix}*.zip")
                    .Select(path => new FileInfo(path))
                    .OrderByDescending(fi => fi.CreationTime)
                    .ToList();

                // Delete archives beyond the limit
                foreach (var oldArchive in archives.Skip(MaxArchivesToKeep))
                {
                    try
                    {
                        oldArchive.Delete();
                    }
                    catch
                    {
                        // Best effort deletion
                    }
                }
            }
            catch
            {
                // Cleanup failure is not critical
            }
        }

        /// <summary>
        /// Resolves the path to the Debugging folder alongside the installed module.
        /// </summary>
        private static string ResolveDebuggingPath()
        {
            try
            {
                var dllDir = Path.GetDirectoryName(typeof(SessionArchiver).Assembly.Location);
                var binDir = Directory.GetParent(dllDir);
                var enlistedRoot = binDir?.Parent;
                
                if (enlistedRoot != null)
                {
                    return Path.Combine(enlistedRoot.FullName, "Debugging");
                }
            }
            catch
            {
                // Path resolution failed
            }
            
            return null;
        }
    }
}

