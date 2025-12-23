using System.Collections.Generic;

namespace Enlisted.Features.Conversations.Data
{
    /// <summary>
    /// Represents a dialogue node in the quartermaster conversation tree.
    /// Multiple nodes can share the same ID with different context conditions for conditional responses.
    /// </summary>
    public class QMDialogueNode
    {
        /// <summary>
        /// Node identifier. Multiple nodes with the same ID represent contextual variants.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Who is speaking: "quartermaster" or "player".
        /// </summary>
        public string Speaker { get; set; }

        /// <summary>
        /// XML localization key for the dialogue text.
        /// </summary>
        public string TextId { get; set; }

        /// <summary>
        /// Fallback text if localization is missing.
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Context conditions for this node variant.
        /// When selecting a node, the system picks the most specific matching variant.
        /// </summary>
        public QMDialogueContext Context { get; set; }

        /// <summary>
        /// Player response options (for quartermaster nodes).
        /// Empty for player nodes (which just transition to next_node).
        /// </summary>
        public List<QMDialogueOption> Options { get; set; }

        public QMDialogueNode()
        {
            Options = new List<QMDialogueOption>();
            Context = new QMDialogueContext();
        }
    }
}

