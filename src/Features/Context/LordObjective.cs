namespace Enlisted.Features.Context
{
    /// <summary>
    /// Lord's current strategic objective (determines party behavior and context for orders/events).
    /// </summary>
    public enum LordObjective
    {
        Unknown,
        Patrolling,
        Besieging,
        PreparingBattle,
        Traveling,
        Resting,
        Defending,
        Raiding,
        Fleeing
    }

    /// <summary>
    /// Priority level for lord's orders (affects frequency and urgency).
    /// </summary>
    public enum LordOrderPriority
    {
        Low,
        Medium,
        High,
        Critical
    }
}

