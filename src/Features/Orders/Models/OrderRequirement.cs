using System.Collections.Generic;

namespace Enlisted.Features.Orders.Models
{
    /// <summary>
    /// Defines requirements for accepting or being assigned an order.
    /// Orders are tier-gated and may require specific skills or traits.
    /// </summary>
    public class OrderRequirement
    {
        /// <summary>
        /// Minimum enlistment tier required (1-9).
        /// </summary>
        public int TierMin { get; set; } = 1;

        /// <summary>
        /// Maximum enlistment tier for this order (1-9).
        /// Higher-tier soldiers may not receive low-tier orders.
        /// </summary>
        public int TierMax { get; set; } = 9;

        /// <summary>
        /// Minimum skill levels required to receive or succeed at this order.
        /// Key is skill name (e.g., "Scouting", "Medicine"), value is minimum level.
        /// Used in success calculation and order selection.
        /// </summary>
        public Dictionary<string, int> MinSkills { get; set; } = [];

        /// <summary>
        /// Minimum trait levels required to receive this order.
        /// Key is trait name (e.g., "Scout", "Surgeon"), value is minimum level.
        /// Used to gate specialist orders to appropriate roles.
        /// </summary>
        public Dictionary<string, int> MinTraits { get; set; } = [];

        /// <summary>
        /// If true, this order can only be issued on land and will be cancelled if the party goes to sea.
        /// Used for orders that require leaving camp (scouting, foraging, patrolling).
        /// </summary>
        public bool NotAtSea { get; set; }
    }
}

