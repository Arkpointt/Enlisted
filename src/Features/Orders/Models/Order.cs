using System.Collections.Generic;
using TaleWorlds.CampaignSystem;

namespace Enlisted.Features.Orders.Models
{
    /// <summary>
    /// Order lifecycle states for forecasting and progression.
    /// </summary>
    public enum OrderState
    {
        /// <summary>
        /// Order is imminent - advance warning given 4-8 hours before issue.
        /// Player sees forecast in daily brief but cannot yet accept/decline.
        /// </summary>
        Imminent,

        /// <summary>
        /// Order has been issued - shows in Orders menu for accept/decline.
        /// Mandatory orders (T1-T3) skip to Active state automatically.
        /// </summary>
        Pending,

        /// <summary>
        /// Order has been accepted - progressing through phases automatically.
        /// Managed by OrderProgressionBehavior.
        /// </summary>
        Active,

        /// <summary>
        /// Order is complete - results shown in Recent Activity.
        /// Historical record only, not actively tracked.
        /// </summary>
        Complete
    }

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
        /// Falls back to context-variant title if available.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Detailed description of the order and its objectives.
        /// Falls back to context-variant description if available.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Who issued this order (e.g., "Sergeant", "Lieutenant", "Captain", "Lord", or a specific name).
        /// Can be "auto" for runtime determination based on rank structure.
        /// </summary>
        public string Issuer { get; set; }

        /// <summary>
        /// Current state in the order lifecycle (Imminent → Pending → Active → Complete).
        /// </summary>
        public OrderState State { get; set; } = OrderState.Pending;

        /// <summary>
        /// Campaign time when imminent warning began (State = Imminent).
        /// Used to calculate hours until issue.
        /// </summary>
        public CampaignTime ImminentTime { get; set; }

        /// <summary>
        /// Campaign time when this order will be issued (transition Imminent → Pending).
        /// Set to ImminentTime + 4-8 hours when forecast is created.
        /// </summary>
        public CampaignTime IssueTime { get; set; }

        /// <summary>
        /// Campaign time when this order was actually issued (State = Pending/Active).
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

        /// <summary>
        /// Context-variant text for sea/land awareness.
        /// Keys: "land", "sea". Each contains Title and Description overrides.
        /// If the current travel context matches a variant, that text is used.
        /// </summary>
        public Dictionary<string, OrderTextVariant> ContextVariants { get; set; }
    }

    /// <summary>
    /// Context-specific title and description for an order.
    /// Used to provide sea/land flavor text variants.
    /// </summary>
    public class OrderTextVariant
    {
        /// <summary>Context-specific title (e.g., "Deck Watch" for sea, "Guard Post" for land).</summary>
        public string Title { get; set; }

        /// <summary>Localization ID for title (e.g., "order_guard_post_sea_title").</summary>
        public string TitleId { get; set; }

        /// <summary>Context-specific description with appropriate flavor text.</summary>
        public string Description { get; set; }

        /// <summary>Localization ID for description.</summary>
        public string DescriptionId { get; set; }
    }
}

