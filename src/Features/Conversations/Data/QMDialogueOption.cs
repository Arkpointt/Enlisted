using System.Collections.Generic;

namespace Enlisted.Features.Conversations.Data
{
    /// <summary>
    /// Represents a player dialogue option in the quartermaster conversation.
    /// </summary>
    public class QMDialogueOption
    {
        /// <summary>
        /// Unique identifier for this option within the node.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// XML localization key for the option text.
        /// </summary>
        public string TextId { get; set; }

        /// <summary>
        /// Fallback text if localization is missing.
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Tooltip explaining what this option does.
        /// </summary>
        public string Tooltip { get; set; }

        /// <summary>
        /// ID of the next dialogue node to transition to.
        /// </summary>
        public string NextNode { get; set; }

        /// <summary>
        /// System action to trigger when this option is selected.
        /// </summary>
        public string Action { get; set; }

        /// <summary>
        /// Additional data for the action (e.g., { "style": "direct", "rep_change": -2 }).
        /// </summary>
        public Dictionary<string, object> ActionData { get; set; }

        /// <summary>
        /// Context requirements for this option to be visible.
        /// If requirements don't match, the option is hidden entirely.
        /// </summary>
        public QMDialogueContext Requirements { get; set; }

        /// <summary>
        /// Gate condition - if this condition isn't met, redirect to gate_node instead of next_node.
        /// Used for RP responses when player doesn't meet requirements (e.g., not cavalry, wrong tier).
        /// </summary>
        public GateCondition Gate { get; set; }
    }

    /// <summary>
    /// Gate condition for redirecting to an RP response node.
    /// </summary>
    public class GateCondition
    {
        /// <summary>
        /// Condition string (e.g., "is_cavalry", "tier_min_7").
        /// </summary>
        public string Condition { get; set; }

        /// <summary>
        /// Node ID to redirect to if condition fails.
        /// </summary>
        public string GateNode { get; set; }
    }
}

