namespace Enlisted.Features.Orders.Models
{
    /// <summary>
    /// Defines the possible outcomes for an order: success, failure, or decline.
    /// Each outcome has different consequences for skills, reputation, company needs, etc.
    /// </summary>
    public class OrderConsequence
    {
        /// <summary>
        /// Consequences if the order succeeds (determined by skill/trait checks).
        /// Generally positive effects on reputation, XP, and company needs.
        /// </summary>
        public OrderOutcome Success { get; set; }

        /// <summary>
        /// Consequences if the order fails (determined by skill/trait checks).
        /// Generally negative effects on reputation and company needs.
        /// </summary>
        public OrderOutcome Failure { get; set; }

        /// <summary>
        /// Consequences if the player declines the order.
        /// Typically reputation penalties with the issuer and officers.
        /// </summary>
        public OrderOutcome Decline { get; set; }

        public OrderConsequence()
        {
            Success = new OrderOutcome();
            Failure = new OrderOutcome();
            Decline = new OrderOutcome();
        }
    }
}

