using System.Collections.Generic;
using TaleWorlds.CampaignSystem;

namespace Enlisted.Features.Orders.Models
{
    /// <summary>
    /// Represents a military order from the chain of command.
    /// Orders are issued periodically (~3 days), can be accepted or declined, and have consequences.
    /// </summary>
    public class Order
    {
        /// <summary>
        /// Unique identifier for this order (e.g., "order_scout_route").
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Display title shown in UI (e.g., "Reconnaissance Patrol").
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Detailed description of the order and its objectives.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Who issued this order (e.g., "Sergeant", "Lieutenant", "Captain", "Lord", or a specific name).
        /// Can be "auto" for runtime determination based on rank structure.
        /// </summary>
        public string Issuer { get; set; }

        /// <summary>
        /// Campaign time when this order was issued.
        /// </summary>
        public CampaignTime IssuedTime { get; set; }

        /// <summary>
        /// Campaign time when this order expires (if not completed).
        /// </summary>
        public CampaignTime ExpirationTime { get; set; }

        /// <summary>
        /// Requirements that must be met to be eligible for this order.
        /// </summary>
        public OrderRequirement Requirements { get; set; } = new OrderRequirement();

        /// <summary>
        /// Outcomes for success, failure, decline, and critical failure scenarios.
        /// </summary>
        public OrderConsequence Consequences { get; set; } = new OrderConsequence();

        /// <summary>
        /// Tags for order categorization (role, context, etc.).
        /// Examples: "scout", "medic", "siege", "patrol", "strategic".
        /// </summary>
        public List<string> Tags { get; set; } = [];

        /// <summary>
        /// If true, this order is automatically accepted when issued (no player choice).
        /// Mandatory orders represent basic duties that soldiers cannot decline.
        /// Examples: guard duty, muster, latrine duty, basic patrols.
        /// </summary>
        public bool Mandatory { get; set; } = false;
    }
}

