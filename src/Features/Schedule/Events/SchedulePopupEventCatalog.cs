using System.Collections.Generic;
using Newtonsoft.Json;

namespace Enlisted.Features.Schedule.Events
{
    /// <summary>
    /// Data-driven catalog for the Schedule system's "Continue-only" popup events.
    ///
    /// These are intentionally lightweight, separate from the canonical StoryBlocks Events system.
    /// The goal is to let you add/adjust schedule flavor popups without touching code.
    /// </summary>
    internal sealed class SchedulePopupEventCatalog
    {
        [JsonProperty("schemaVersion")] public int SchemaVersion { get; set; } = 1;

        [JsonProperty("events")]
        public List<SchedulePopupEventDefinition> Events { get; set; } =
            new List<SchedulePopupEventDefinition>();
    }

    internal sealed class SchedulePopupEventDefinition
    {
        /// <summary>Stable identifier used for logs and debugging.</summary>
        [JsonProperty("id")] public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Optional: Match a specific schedule activity id (e.g. "work_detail" from schedule_config.json).
        /// If present, this takes priority over block_type matching.
        /// </summary>
        [JsonProperty("activity_id")] public string ActivityId { get; set; } = string.Empty;

        /// <summary>
        /// Optional: Match a schedule block type (e.g. "WorkDetail").
        /// Used when activity_id is empty or no activity match exists.
        /// </summary>
        [JsonProperty("block_type")] public string BlockType { get; set; } = string.Empty;

        // Text (optional localization ids + fallback strings).
        [JsonProperty("titleId")] public string TitleId { get; set; } = string.Empty;
        [JsonProperty("title")] public string TitleFallback { get; set; } = string.Empty;

        [JsonProperty("bodyId")] public string BodyId { get; set; } = string.Empty;
        [JsonProperty("body")] public string BodyFallback { get; set; } = string.Empty;

        /// <summary>
        /// Skill id (Bannerlord SkillObject StringId), or a common alias like "engineering" / "one_handed".
        /// </summary>
        [JsonProperty("skill")] public string Skill { get; set; } = string.Empty;

        /// <summary>Skill XP amount. If 0 or negative, the popup becomes "flavor only".</summary>
        [JsonProperty("xp")] public int Xp { get; set; }

        /// <summary>
        /// Weight for random selection among other matching events (default 1.0).
        /// </summary>
        [JsonProperty("weight")] public float Weight { get; set; } = 1f;
    }
}


