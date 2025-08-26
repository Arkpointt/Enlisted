using TaleWorlds.CampaignSystem;

namespace Enlisted.Features.Enlistment.Application
{
    /// <summary>
    /// Interface for enlistment service following ADR-004 dependency injection pattern.
    /// Replaces static EnlistmentBehavior.Instance access with proper abstraction.
    /// Enables testing and reduces coupling between game adapters and domain logic.
    /// </summary>
    public interface IEnlistmentService
    {
        /// <summary>Current enlistment status of the player.</summary>
        bool IsEnlisted { get; }
        
        /// <summary>The hero the player is currently serving under, if enlisted.</summary>
        Hero Commander { get; }
        
        /// <summary>Check if the current commander is still valid (alive, has party, etc).</summary>
        bool IsCommanderValid();
    }
}
