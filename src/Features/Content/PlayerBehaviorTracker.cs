using System.Collections.Generic;
using Enlisted.Features.Content.Models;
using Enlisted.Mod.Core.Logging;

namespace Enlisted.Features.Content
{
    /// <summary>
    /// Tracks player behavior and choices to learn content preferences.
    /// Used by the Content Orchestrator to personalize event selection.
    /// </summary>
    public static class PlayerBehaviorTracker
    {
        private const string LogCategory = "BehaviorTracker";

        // Behavior counts by tag (e.g., "combat", "social", "risky", "safe", "loyal", "selfish")
        private static Dictionary<string, int> _behaviorCounts = new Dictionary<string, int>();

        // Content engagement tracking (event ID -> times engaged/dismissed)
        private static Dictionary<string, int> _contentEngagement = new Dictionary<string, int>();

        // Cached preferences (recalculated when choices change)
        private static PlayerPreferences _cachedPreferences;
        private static bool _preferencesDirty = true;

        /// <summary>
        /// Records a player choice with the given tag.
        /// Tags should be consistent: "combat", "social", "risky", "safe", "loyal", "selfish".
        /// </summary>
        public static void RecordChoice(string choiceTag)
        {
            if (string.IsNullOrEmpty(choiceTag))
            {
                return;
            }

            var tag = choiceTag.ToLowerInvariant();
            if (!_behaviorCounts.ContainsKey(tag))
            {
                _behaviorCounts[tag] = 0;
            }

            _behaviorCounts[tag]++;
            _preferencesDirty = true;

            ModLogger.Debug(LogCategory, $"Recorded choice: {tag} (total: {_behaviorCounts[tag]})");
        }

        /// <summary>
        /// Records that content was delivered to the player.
        /// Used for tracking engagement patterns.
        /// </summary>
        public static void RecordContentDelivered(string contentId)
        {
            if (string.IsNullOrEmpty(contentId))
            {
                return;
            }

            if (!_contentEngagement.ContainsKey(contentId))
            {
                _contentEngagement[contentId] = 0;
            }

            _contentEngagement[contentId]++;
            ModLogger.Debug(LogCategory, $"Content delivered: {contentId}");
        }

        /// <summary>
        /// Gets the current player preferences based on recorded choices.
        /// </summary>
        public static PlayerPreferences GetPreferences()
        {
            if (_preferencesDirty || _cachedPreferences == null)
            {
                _cachedPreferences = CalculatePreferences();
                _preferencesDirty = false;
            }

            return _cachedPreferences;
        }

        /// <summary>
        /// Resets all tracked behavior data.
        /// Called when starting a new game or for testing.
        /// </summary>
        public static void Reset()
        {
            _behaviorCounts.Clear();
            _contentEngagement.Clear();
            _cachedPreferences = null;
            _preferencesDirty = true;
            ModLogger.Info(LogCategory, "Behavior tracking reset");
        }

        /// <summary>
        /// Loads behavior data from saved state.
        /// </summary>
        public static void LoadState(Dictionary<string, int> behaviorCounts, Dictionary<string, int> contentEngagement)
        {
            _behaviorCounts = behaviorCounts ?? new Dictionary<string, int>();
            _contentEngagement = contentEngagement ?? new Dictionary<string, int>();
            _preferencesDirty = true;
            ModLogger.Debug(LogCategory, $"Loaded state: {_behaviorCounts.Count} behavior tags, {_contentEngagement.Count} content entries");
        }

        /// <summary>
        /// Gets behavior counts for saving.
        /// </summary>
        public static Dictionary<string, int> GetBehaviorCountsForSave()
        {
            return new Dictionary<string, int>(_behaviorCounts);
        }

        /// <summary>
        /// Gets content engagement for saving.
        /// </summary>
        public static Dictionary<string, int> GetContentEngagementForSave()
        {
            return new Dictionary<string, int>(_contentEngagement);
        }

        private static PlayerPreferences CalculatePreferences()
        {
            var prefs = new PlayerPreferences();

            // Count choices by category
            int combatChoices = GetCount("combat");
            int socialChoices = GetCount("social");
            int riskyChoices = GetCount("risky");
            int safeChoices = GetCount("safe");
            int loyalChoices = GetCount("loyal");
            int selfishChoices = GetCount("selfish");

            // Calculate combat vs social preference (0 = social, 1 = combat, 0.5 = balanced)
            int combatSocialTotal = combatChoices + socialChoices;
            prefs.CombatVsSocial = combatSocialTotal > 0
                ? (float)combatChoices / combatSocialTotal
                : 0.5f;

            // Calculate risky vs safe preference (0 = safe, 1 = risky)
            int riskTotal = riskyChoices + safeChoices;
            prefs.RiskyVsSafe = riskTotal > 0
                ? (float)riskyChoices / riskTotal
                : 0.5f;

            // Calculate loyal vs self-serving preference (0 = selfish, 1 = loyal)
            int loyaltyTotal = loyalChoices + selfishChoices;
            prefs.LoyalVsSelfServing = loyaltyTotal > 0
                ? (float)loyalChoices / loyaltyTotal
                : 0.5f;

            // Store raw counts for debugging
            prefs.TotalChoicesMade = combatSocialTotal + riskTotal + loyaltyTotal;
            prefs.CombatChoices = combatChoices;
            prefs.SocialChoices = socialChoices;
            prefs.RiskyChoices = riskyChoices;
            prefs.SafeChoices = safeChoices;

            return prefs;
        }

        private static int GetCount(string tag)
        {
            return _behaviorCounts.TryGetValue(tag, out var count) ? count : 0;
        }
    }
}
