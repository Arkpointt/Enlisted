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

        /// <summary>
        ///     Returns a deterministic set of lances representing the lord's party composition for UI/simulation purposes.
        ///     This is intentionally lightweight and does not attempt to infer "real" troop composition from the party roster.
        ///
        ///     Rules:
        ///     - Calculates up to <paramref name="maxLances"/> lances from the lord's culture-mapped style.
        ///     - First pass: Picks one lance per role (infantry → ranged → cavalry → horsearcher).
        ///     - Subsequent passes: Cycles through roles again to fill remaining slots (e.g., "2nd Infantry", "2nd Ranged").
        ///     - Ensures the player's current lance is included when provided (without duplication).
        ///
        ///     This replaces the previous "demo lances" placeholder in Camp UI and gives us stable per-lord lances
        ///     that can be simulated independently (personas, banners, etc.).
        /// </summary>
        public static List<LanceAssignment> GetLordPartyLances(Hero lord, string playerCurrentLanceId, int maxLances = 3)
        {
            var featureConfig = ConfigurationManager.LoadLancesConfig();
            if (featureConfig?.LancesEnabled != true || lord == null)
            {
                return new List<LanceAssignment>();
            }

            var catalog = ConfigurationManager.LoadLanceCatalog();
            if (catalog?.StyleDefinitions == null || catalog.StyleDefinitions.Count == 0)
            {
                return new List<LanceAssignment>();
            }

            var cultureId = GetCultureId(lord);
            var styleId = ResolveStyleId(catalog, cultureId);
            var style = catalog.StyleDefinitions.FirstOrDefault(s =>
                string.Equals(s.Id, styleId, StringComparison.OrdinalIgnoreCase));

            var lances = style?.Lances?.Where(l => l != null).ToList() ?? new List<LanceDefinition>();
            if (lances.Count == 0)
            {
                return new List<LanceAssignment>();
            }

            var result = new List<LanceAssignment>();
            var usedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var cap = Math.Max(1, maxLances);

            // Build role buckets from the style (each role may have multiple lance definitions).
            var byRole = new Dictionary<string, List<LanceDefinition>>(StringComparer.OrdinalIgnoreCase);
            foreach (var lance in lances)
            {
                var role = NormalizeRole(lance.RoleHint);
                if (!byRole.TryGetValue(role, out var list))
                {
                    list = new List<LanceDefinition>();
                    byRole[role] = list;
                }
                list.Add(lance);
            }

            // Ensure player's current lance is included first (if it resolves).
            if (!string.IsNullOrWhiteSpace(playerCurrentLanceId))
            {
                var playerAssignment = ResolveLanceById(playerCurrentLanceId);
                if (playerAssignment != null && usedIds.Add(playerAssignment.Id))
                {
                    result.Add(playerAssignment);
                }
            }

            // Role cycling order - we'll loop through these repeatedly to fill lances.
            var preferredRoles = new[] { "infantry", "ranged", "cavalry", "horsearcher" };
            
            // Track how many lances we've picked from each role bucket for deterministic selection.
            var rolePickIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var role in preferredRoles)
            {
                rolePickIndex[role] = 0;
            }

            // Keep cycling through roles until we have enough lances.
            // This gives us balanced distribution: Infantry1, Ranged1, Cavalry1, Horse1, Infantry2, Ranged2, etc.
            var cycleCount = 0;
            var maxCycles = cap; // Safety limit to prevent infinite loops
            
            while (result.Count < cap && cycleCount < maxCycles)
            {
                var addedThisCycle = false;
                
                foreach (var role in preferredRoles)
                {
                    if (result.Count >= cap)
                    {
                        break;
                    }

                    if (!byRole.TryGetValue(role, out var bucket) || bucket == null || bucket.Count == 0)
                    {
                        continue;
                    }

                    // Sort bucket deterministically for stable picking
                    var sortedBucket = bucket
                        .OrderBy(l => l.Id ?? l.Name ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                        .ToList();

                    var pickIdx = rolePickIndex[role];
                    
                    // Try to pick the next lance from this role that we haven't used yet
                    for (var attempt = 0; attempt < sortedBucket.Count; attempt++)
                    {
                        var candidateIdx = (pickIdx + attempt) % sortedBucket.Count;
                        var candidate = sortedBucket[candidateIdx];
                        var assignment = BuildAssignment(candidate, styleId, cultureId, usedFallback: false);
                        
                        if (usedIds.Add(assignment.Id))
                        {
                            result.Add(assignment);
                            rolePickIndex[role] = candidateIdx + 1;
                            addedThisCycle = true;
                            break;
                        }
                    }
                }
                
                // If we couldn't add any lances this cycle, all unique lances are exhausted.
                // Generate synthetic lances with ordinal suffixes for larger parties.
                if (!addedThisCycle)
                {
                    // Create synthetic lances by re-using definitions with ordinal suffixes
                    var ordinal = cycleCount + 2; // "2nd", "3rd", etc.
                    foreach (var role in preferredRoles)
                    {
                        if (result.Count >= cap)
                        {
                            break;
                        }

                        if (!byRole.TryGetValue(role, out var bucket) || bucket == null || bucket.Count == 0)
                        {
                            continue;
                        }

                        // Pick deterministically based on lord + role + ordinal
                        var sortedBucket = bucket
                            .OrderBy(l => l.Id ?? l.Name ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                            .ToList();
                        var idx = Math.Abs(StableHash32($"{lord.StringId}|{styleId}|{role}|{ordinal}")) % sortedBucket.Count;
                        var baseLance = sortedBucket[idx];
                        
                        // Create a synthetic lance with ordinal suffix
                        var syntheticId = $"{baseLance.Id}_{ordinal}";
                        if (usedIds.Add(syntheticId))
                        {
                            var syntheticName = $"{baseLance.Name} ({GetOrdinalSuffix(ordinal)})";
                            result.Add(new LanceAssignment
                            {
                                Id = syntheticId,
                                Name = syntheticName,
                                StyleId = styleId,
                                StoryId = baseLance.StoryId ?? string.Empty,
                                RoleHint = NormalizeRole(baseLance.RoleHint),
                                SourceCultureId = string.IsNullOrWhiteSpace(cultureId) ? "unknown" : cultureId,
                                UsedFallback = false
                            });
                        }
                    }
                }
                
                cycleCount++;
            }

            // Hard cap to maxLances.
            if (result.Count > cap)
            {
                result = result.Take(cap).ToList();
            }

            return result;
        }
        
        /// <summary>
        /// Get ordinal suffix for lance numbering (2nd, 3rd, 4th, etc.).
        /// </summary>
        private static string GetOrdinalSuffix(int number)
        {
            if (number <= 0) return number.ToString();
            
            // Special cases for 11th, 12th, 13th
            var lastTwoDigits = number % 100;
            if (lastTwoDigits >= 11 && lastTwoDigits <= 13)
            {
                return $"{number}th";
            }
            
            return (number % 10) switch
            {
                1 => $"{number}st",
                2 => $"{number}nd",
                3 => $"{number}rd",
                _ => $"{number}th"
            };
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

        private static LanceDefinition PickDeterministic(List<LanceDefinition> candidates, string seedKey)
        {
            if (candidates == null || candidates.Count == 0)
            {
                return null;
            }

            candidates.Sort((a, b) =>
                string.Compare(a?.Id ?? a?.Name ?? string.Empty, b?.Id ?? b?.Name ?? string.Empty, StringComparison.OrdinalIgnoreCase));

            var seed = StableHash32(seedKey ?? string.Empty);
            var idx = Math.Abs(seed) % candidates.Count;
            return candidates[idx];
        }

        /// <summary>
        /// Stable, cross-process 32-bit hash (FNV-1a) for deterministic selections.
        /// Do NOT use string.GetHashCode() here (it is randomized across processes in modern .NET).
        /// </summary>
        private static int StableHash32(string input)
        {
            unchecked
            {
                const int fnvPrime = 16777619;
                var hash = (int)2166136261;
                if (!string.IsNullOrEmpty(input))
                {
                    for (var i = 0; i < input.Length; i++)
                    {
                        hash ^= input[i];
                        hash *= fnvPrime;
                    }
                }

                return hash;
            }
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

