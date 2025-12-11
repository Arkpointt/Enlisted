using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;

namespace Enlisted.Features.Assignments.Core
{
    /// <summary>
    ///     Helper for resolving lance assignments from configuration.
    ///     Provides provisional selection based on culture/style mapping.
    /// </summary>
    public static class LanceRegistry
    {
        private static readonly Random _rng = new Random();

        public static bool IsFeatureEnabled()
        {
            var config = ConfigurationManager.LoadLancesConfig();
            return config?.LancesEnabled == true;
        }

        /// <summary>
        ///     Resolve a lance definition by id across all styles. Returns null if not found.
        /// </summary>
        public static LanceAssignment ResolveLanceById(string lanceId)
        {
            if (string.IsNullOrWhiteSpace(lanceId))
            {
                return null;
            }

            var catalog = ConfigurationManager.LoadLanceCatalog();
            if (catalog?.StyleDefinitions == null || catalog.StyleDefinitions.Count == 0)
            {
                return null;
            }

            foreach (var style in catalog.StyleDefinitions)
            {
                if (style?.Lances == null)
                {
                    continue;
                }

                var match = style.Lances.FirstOrDefault(l =>
                    string.Equals(l.Id, lanceId, StringComparison.OrdinalIgnoreCase));

                if (match != null)
                {
                    return BuildAssignment(match, style.Id, null, false);
                }
            }

            return null;
        }

        /// <summary>
        ///     Select a provisional lance for the player based on the lord's culture and optional formation hint.
        /// </summary>
        public static LanceAssignment GenerateProvisionalLance(Hero lord, string playerFormation, string overrideStyleId = null)
        {
            var featureConfig = ConfigurationManager.LoadLancesConfig();
            if (featureConfig?.LancesEnabled != true)
            {
                return null;
            }

            var (style, candidates, usedFallback, cultureId, styleId, catalog) =
                GetCandidates(lord, playerFormation, overrideStyleId);

            if (candidates.Count == 0 || catalog == null)
            {
                return null;
            }

            var pick = candidates[_rng.Next(candidates.Count)];
            var resolvedStyle = style?.Id ?? styleId ?? catalog.StyleDefinitions.First().Id;

            return BuildAssignment(pick, resolvedStyle, cultureId, usedFallback);
        }

        /// <summary>
        ///     Build a set of candidate lances for a final selection menu.
        /// </summary>
        public static List<LanceAssignment> GetCandidateLances(Hero lord, string playerFormation, int selectionCount,
            string overrideStyleId = null)
        {
            var featureConfig = ConfigurationManager.LoadLancesConfig();
            if (featureConfig?.LancesEnabled != true)
            {
                return new List<LanceAssignment>();
            }

            var (style, candidates, usedFallback, cultureId, styleId, catalog) =
                GetCandidates(lord, playerFormation, overrideStyleId);

            if (catalog == null || candidates.Count == 0)
            {
                return new List<LanceAssignment>();
            }

            var takeCount = Math.Max(1, selectionCount);

            // Shuffle by random picking without replacement
            var pool = candidates.ToList();
            var picked = new List<LanceDefinition>();
            while (pool.Count > 0 && picked.Count < takeCount)
            {
                var idx = _rng.Next(pool.Count);
                picked.Add(pool[idx]);
                pool.RemoveAt(idx);
            }

            var resolvedStyle = style?.Id ?? styleId ?? catalog.StyleDefinitions.First().Id;

            return picked
                .Select(p => BuildAssignment(p, resolvedStyle, cultureId, usedFallback))
                .ToList();
        }

        /// <summary>
        ///     Resolve latest data for a saved lance. Returns null if id is empty; returns assignment with latest name/story
        ///     if id is found; otherwise returns null indicating legacy/missing.
        /// </summary>
        public static LanceAssignment ResolveFromSaved(string savedLanceId)
        {
            if (string.IsNullOrWhiteSpace(savedLanceId))
            {
                return null;
            }

            return ResolveLanceById(savedLanceId);
        }

        /// <summary>
        ///     Returns lances matching culture/style and role for external consumers (e.g., story/camp).
        /// </summary>
        public static List<LanceAssignment> GetLancesForCultureAndRole(string cultureId, string roleHint)
        {
            var catalog = ConfigurationManager.LoadLanceCatalog();
            if (catalog?.StyleDefinitions == null || catalog.StyleDefinitions.Count == 0)
            {
                return new List<LanceAssignment>();
            }

            var styleId = ResolveStyleId(catalog, cultureId);
            var normalizedRole = NormalizeRole(roleHint);
            var style = catalog.StyleDefinitions.FirstOrDefault(s =>
                string.Equals(s.Id, styleId, StringComparison.OrdinalIgnoreCase));
            var candidates = FilterByRole(style?.Lances, normalizedRole);
            if (candidates.Count == 0)
            {
                return new List<LanceAssignment>();
            }

            return candidates
                .Select(p => BuildAssignment(p, styleId, cultureId, false))
                .ToList();
        }

        private static string GetCultureId(Hero lord)
        {
            var cultureId = lord?.MapFaction?.Culture?.StringId;
            if (string.IsNullOrWhiteSpace(cultureId))
            {
                cultureId = lord?.Culture?.StringId;
            }

            return cultureId;
        }

        private static (LanceStyleDefinition style,
            List<LanceDefinition> candidates,
            bool usedFallback,
            string cultureId,
            string styleId,
            LanceCatalogConfig catalog) GetCandidates(Hero lord, string playerFormation, string overrideStyleId = null)
        {
            var catalog = ConfigurationManager.LoadLanceCatalog();
            if (catalog?.StyleDefinitions == null || catalog.StyleDefinitions.Count == 0)
            {
                return (null, new List<LanceDefinition>(), false, null, null, null);
            }

            var cultureId = GetCultureId(lord);
            var styleId = !string.IsNullOrWhiteSpace(overrideStyleId)
                ? overrideStyleId
                : ResolveStyleId(catalog, cultureId);
            var normalizedRole = NormalizeRole(playerFormation);

            var style = catalog.StyleDefinitions.FirstOrDefault(s =>
                string.Equals(s.Id, styleId, StringComparison.OrdinalIgnoreCase));

            var candidates = FilterByRole(style?.Lances, normalizedRole);
            var usedFallback = false;

            if (candidates.Count == 0)
            {
                usedFallback = true;
                var fallbackStyle = catalog.StyleDefinitions.FirstOrDefault(s =>
                    string.Equals(s.Id, "style_mercenary", StringComparison.OrdinalIgnoreCase)) ??
                                    catalog.StyleDefinitions.FirstOrDefault();

                candidates = FilterByRole(fallbackStyle?.Lances, normalizedRole);
                style ??= fallbackStyle;

                if (candidates.Count == 0)
                {
                    // Last resort: any lance in the catalog
                    candidates = catalog.StyleDefinitions
                        .Where(s => s.Lances != null)
                        .SelectMany(s => s.Lances)
                        .ToList();
                }
            }

            return (style, candidates, usedFallback, cultureId, styleId, catalog);
        }

        private static string ResolveStyleId(LanceCatalogConfig catalog, string cultureId)
        {
            if (!string.IsNullOrWhiteSpace(cultureId) &&
                catalog.CultureMap != null &&
                catalog.CultureMap.TryGetValue(cultureId, out var mappedStyle) &&
                !string.IsNullOrWhiteSpace(mappedStyle))
            {
                return mappedStyle;
            }

            // Fallback to mercenary if present, otherwise first available
            var merc = catalog.StyleDefinitions.FirstOrDefault(s =>
                string.Equals(s.Id, "style_mercenary", StringComparison.OrdinalIgnoreCase));
            return merc?.Id ?? catalog.StyleDefinitions.First().Id;
        }

        private static List<LanceDefinition> FilterByRole(IEnumerable<LanceDefinition> lances, string normalizedRole)
        {
            if (lances == null)
            {
                return new List<LanceDefinition>();
            }

            var lanceList = lances as IList<LanceDefinition> ?? lances.ToList();

            var matching = lanceList
                .Where(l => string.Equals(NormalizeRole(l.RoleHint), normalizedRole, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (matching.Count > 0)
            {
                return matching;
            }

            return lanceList.ToList();
        }

        private static LanceAssignment BuildAssignment(LanceDefinition pick, string resolvedStyle, string cultureId,
            bool usedFallback)
        {
            return new LanceAssignment
            {
                Id = string.IsNullOrWhiteSpace(pick.Id) ? pick.Name ?? "lance" : pick.Id,
                Name = string.IsNullOrWhiteSpace(pick.Name) ? pick.Id ?? "Lance" : pick.Name,
                StyleId = resolvedStyle,
                StoryId = pick.StoryId ?? string.Empty,
                RoleHint = NormalizeRole(pick.RoleHint),
                SourceCultureId = string.IsNullOrWhiteSpace(cultureId) ? "unknown" : cultureId,
                UsedFallback = usedFallback
            };
        }

        private static string NormalizeRole(string role)
        {
            if (string.IsNullOrWhiteSpace(role))
            {
                return "infantry";
            }

            var normalized = role.Trim().ToLowerInvariant();
            return normalized switch
            {
                "archer" => "ranged",
                "ranged" => "ranged",
                "cavalry" => "cavalry",
                "horse_archer" => "horsearcher",
                "horsearcher" => "horsearcher",
                _ => "infantry"
            };
        }
    }

    /// <summary>
    ///     Resolved lance selection result.
    /// </summary>
    public class LanceAssignment
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string StyleId { get; set; }
        public string StoryId { get; set; }
        public string RoleHint { get; set; }
        public string SourceCultureId { get; set; }
        public bool UsedFallback { get; set; }
    }
}

