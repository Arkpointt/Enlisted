using System;
using System.Collections.Generic;
using System.Linq;
using Enlisted.Mod.Core.Logging;

namespace Enlisted.Features.Content
{
    /// <summary>
    /// Catalog of player decisions loaded from the Event JSON files.
    /// Decisions are events with category "decision" and have additional delivery metadata.
    /// Filters and organizes decisions by delivery method (player-initiated vs automatic)
    /// and menu section (training, social, camp_life, logistics).
    /// </summary>
    public static class DecisionCatalog
    {
        private const string LogCategory = "DecisionCatalog";
        private const string DecisionCategory = "decision";

        private static readonly List<DecisionDefinition> AllDecisions = [];
        private static readonly Dictionary<string, DecisionDefinition> DecisionsById = new(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, List<DecisionDefinition>> DecisionsBySection = new(StringComparer.OrdinalIgnoreCase);
        private static readonly List<DecisionDefinition> PlayerInitiatedDecisions = [];
        private static readonly List<DecisionDefinition> AutomaticDecisions = [];
        private static bool _initialized;

        /// <summary>
        /// Total number of decisions loaded.
        /// </summary>
        public static int DecisionCount => AllDecisions.Count;

        /// <summary>
        /// Number of player-initiated decisions (shown in menu).
        /// </summary>
        public static int PlayerInitiatedCount => PlayerInitiatedDecisions.Count;

        /// <summary>
        /// Number of automatic decisions (triggered by context).
        /// </summary>
        public static int AutomaticCount => AutomaticDecisions.Count;

        /// <summary>
        /// Initializes the decision catalog by filtering decisions from EventCatalog.
        /// Must be called after EventCatalog.Initialize().
        /// </summary>
        public static void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            AllDecisions.Clear();
            DecisionsById.Clear();
            DecisionsBySection.Clear();
            PlayerInitiatedDecisions.Clear();
            AutomaticDecisions.Clear();

            // Ensure EventCatalog is initialized first
            EventCatalog.Initialize();

            // Get all events with category "decision"
            var decisionEvents = EventCatalog.GetEventsByCategory(DecisionCategory);

            foreach (var eventDef in decisionEvents)
            {
                try
                {
                    var decision = ConvertToDecision(eventDef);
                    if (decision != null)
                    {
                        AllDecisions.Add(decision);
                        DecisionsById[decision.Id] = decision;

                        // Categorize by delivery method
                        if (decision.IsPlayerInitiated)
                        {
                            PlayerInitiatedDecisions.Add(decision);
                        }
                        else
                        {
                            AutomaticDecisions.Add(decision);
                        }

                        // Group by menu section
                        var section = decision.MenuSection ?? "other";
                        if (!DecisionsBySection.TryGetValue(section, out var sectionList))
                        {
                            sectionList = [];
                            DecisionsBySection[section] = sectionList;
                        }
                        sectionList.Add(decision);
                    }
                }
                catch (Exception ex)
                {
                    ModLogger.Error(LogCategory, $"Failed to convert event '{eventDef.Id}' to decision", ex);
                }
            }

            _initialized = true;

            ModLogger.Info(LogCategory, $"Loaded {AllDecisions.Count} decisions " +
                $"({PlayerInitiatedDecisions.Count} player-initiated, {AutomaticDecisions.Count} automatic)");

            // Log section breakdown
            foreach (var kvp in DecisionsBySection)
            {
                ModLogger.Debug(LogCategory, $"  Section '{kvp.Key}': {kvp.Value.Count} decisions");
            }
        }

        /// <summary>
        /// Gets a decision by its unique ID.
        /// </summary>
        public static DecisionDefinition GetDecision(string decisionId)
        {
            EnsureInitialized();
            DecisionsById.TryGetValue(decisionId, out var decision);
            return decision;
        }

        /// <summary>
        /// Gets all player-initiated decisions (ones shown in the menu).
        /// </summary>
        public static IReadOnlyList<DecisionDefinition> GetPlayerInitiatedDecisions()
        {
            EnsureInitialized();
            return PlayerInitiatedDecisions;
        }

        /// <summary>
        /// Gets all automatic decisions (triggered by context/time).
        /// </summary>
        public static IReadOnlyList<DecisionDefinition> GetAutomaticDecisions()
        {
            EnsureInitialized();
            return AutomaticDecisions;
        }

        /// <summary>
        /// Gets player-initiated decisions for a specific menu section.
        /// </summary>
        public static IReadOnlyList<DecisionDefinition> GetDecisionsBySection(string section)
        {
            EnsureInitialized();

            if (DecisionsBySection.TryGetValue(section, out var sectionList))
            {
                return sectionList.Where(d => d.IsPlayerInitiated).ToList();
            }

            return Array.Empty<DecisionDefinition>();
        }

        /// <summary>
        /// Gets all menu sections that have player-initiated decisions.
        /// </summary>
        public static IEnumerable<string> GetActiveSections()
        {
            EnsureInitialized();

            return DecisionsBySection
                .Where(kvp => kvp.Value.Any(d => d.IsPlayerInitiated))
                .Select(kvp => kvp.Key);
        }

        private static void EnsureInitialized()
        {
            if (!_initialized)
            {
                Initialize();
            }
        }

        /// <summary>
        /// Converts an EventDefinition to a DecisionDefinition by extracting
        /// decision-specific fields from the underlying JSON data.
        /// </summary>
        private static DecisionDefinition ConvertToDecision(EventDefinition eventDef)
        {
            if (eventDef == null)
            {
                return null;
            }

            var decision = new DecisionDefinition
            {
                Id = eventDef.Id,
                TitleId = eventDef.TitleId,
                SetupId = eventDef.SetupId,
                Category = eventDef.Category,
                Requirements = eventDef.Requirements,
                Timing = eventDef.Timing,
                Options = eventDef.Options
            };

            // Log what we're getting from EventDefinition
            ModLogger.Info(LogCategory, $"ConvertToDecision: id={eventDef.Id}, titleId='{eventDef.TitleId}', setupId='{eventDef.SetupId}'");

            // Re-parse the original JSON to extract decision-specific delivery fields
            // Since we can't access the raw JSON here, we'll use naming conventions
            ExtractDeliveryInfo(decision);

            return decision;
        }

        /// <summary>
        /// Extracts delivery info from the decision ID naming convention.
        /// Player-initiated decisions typically start with "player_".
        /// Menu section is inferred from the ID pattern or defaults based on content.
        /// </summary>
        private static void ExtractDeliveryInfo(DecisionDefinition decision)
        {
            var id = decision.Id?.ToLowerInvariant() ?? string.Empty;

            // Determine if player-initiated based on ID prefix
            decision.IsPlayerInitiated = id.StartsWith("player_");

            // Infer menu section from ID pattern
            if (id.Contains("train") || id.Contains("drill") || id.Contains("spar"))
            {
                decision.MenuSection = "training";
            }
            else if (id.Contains("wound") || id.Contains("rest") || id.Contains("camp") ||
                     id.Contains("gear") || id.Contains("medic") || id.Contains("medical"))
            {
                decision.MenuSection = "camp_life";
            }
            else if (id.Contains("quarter") || id.Contains("supply") || id.Contains("qm") ||
                     id.Contains("logistics") || id.Contains("audit"))
            {
                decision.MenuSection = "logistics";
            }
            else if (id.Contains("dice") || id.Contains("letter") || id.Contains("petition") || 
                     id.Contains("join") || id.Contains("drink") || id.Contains("social") ||
                     id.Contains("favor") || id.Contains("hunt"))
            {
                decision.MenuSection = "social";
            }
            else if (id.Contains("scout") || id.Contains("intel") || id.Contains("rumor"))
            {
                decision.MenuSection = "intel";
            }
            else
            {
                decision.MenuSection = "other";
            }
        }

        /// <summary>
        /// Clears the catalog and resets initialization state. Used for testing.
        /// </summary>
        public static void Reset()
        {
            AllDecisions.Clear();
            DecisionsById.Clear();
            DecisionsBySection.Clear();
            PlayerInitiatedDecisions.Clear();
            AutomaticDecisions.Clear();
            _initialized = false;
        }
    }

    /// <summary>
    /// Extended decision definition with delivery metadata.
    /// Wraps EventDefinition with additional fields for menu integration.
    /// </summary>
    public class DecisionDefinition
    {
        public string Id { get; set; } = string.Empty;
        public string TitleId { get; set; } = string.Empty;
        public string SetupId { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public EventRequirements Requirements { get; set; } = new();
        public EventTiming Timing { get; set; } = new();
        public List<EventOption> Options { get; set; } = [];

        /// <summary>
        /// True if the player can initiate this decision from the menu.
        /// False if it's triggered automatically by context.
        /// </summary>
        public bool IsPlayerInitiated { get; set; }

        /// <summary>
        /// Menu section for player-initiated decisions: "training", "social", "camp_life", "logistics", "intel".
        /// </summary>
        public string MenuSection { get; set; } = "other";

        /// <summary>
        /// Required time of day for this decision (null = any time).
        /// </summary>
        public List<string> TimeOfDay { get; set; }

        /// <summary>
        /// Required flags for this decision to appear (null = no flag requirements).
        /// </summary>
        public List<string> RequiredFlags { get; set; }

        /// <summary>
        /// Flags that block this decision from appearing.
        /// </summary>
        public List<string> BlockingFlags { get; set; }
    }
}

