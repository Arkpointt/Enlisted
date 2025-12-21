using System.Collections.Generic;

namespace Enlisted.Features.Orders.Models
{
    /// <summary>
    /// Represents the consequences of an order outcome (success, failure, or decline).
    /// Affects skills, traits, reputation, company needs, resources, and provides narrative feedback.
    /// </summary>
    public class OrderOutcome
    {
        /// <summary>
        /// Skill XP awards for this outcome.
        /// Key is skill name (e.g., "Scouting", "Leadership"), value is XP amount.
        /// Can be negative for penalties.
        /// </summary>
        public Dictionary<string, int> SkillXP { get; set; }

        /// <summary>
        /// Trait XP awards for this outcome.
        /// Key is trait name (e.g., "Scout", "Commander"), value is XP amount.
        /// Can be negative for penalties.
        /// </summary>
        public Dictionary<string, int> TraitXP { get; set; }

        /// <summary>
        /// Reputation changes with chain of command.
        /// Keys: "lord", "officer", "soldier".
        /// Values: reputation delta (positive or negative).
        /// </summary>
        public Dictionary<string, int> Reputation { get; set; }

        /// <summary>
        /// Company need modifications.
        /// Keys: "Readiness", "Morale", "Supplies", "Equipment", "Rest".
        /// Values: delta to apply (positive improves, negative degrades).
        /// </summary>
        public Dictionary<string, int> CompanyNeeds { get; set; }

        /// <summary>
        /// Escalation track modifications.
        /// Keys: "heat", "discipline".
        /// Values: delta to apply.
        /// </summary>
        public Dictionary<string, int> Escalation { get; set; }

        /// <summary>
        /// Denar reward or penalty for this outcome.
        /// </summary>
        public int Denars { get; set; }

        /// <summary>
        /// Renown reward or penalty for this outcome.
        /// </summary>
        public int Renown { get; set; }

        /// <summary>
        /// Narrative text describing this outcome.
        /// Shown to player in UI after order completion.
        /// </summary>
        public string Text { get; set; }

        public OrderOutcome()
        {
            SkillXP = new Dictionary<string, int>();
            TraitXP = new Dictionary<string, int>();
            Reputation = new Dictionary<string, int>();
            CompanyNeeds = new Dictionary<string, int>();
            Escalation = new Dictionary<string, int>();
            Denars = 0;
            Renown = 0;
            Text = string.Empty;
        }
    }
}

