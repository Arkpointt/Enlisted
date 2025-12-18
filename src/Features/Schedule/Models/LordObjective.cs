namespace Enlisted.Features.Schedule.Models
{
    /// <summary>
    /// Represents the lord's current military objective.
    /// Determines what types of duties are prioritized in schedule generation.
    /// </summary>
    public enum LordObjective
    {
        /// <summary>Unknown or default state</summary>
        Unknown,
        
        /// <summary>Patrolling territory, border watch</summary>
        Patrolling,
        
        /// <summary>Besieging a settlement</summary>
        Besieging,
        
        /// <summary>Preparing for imminent battle</summary>
        PreparingBattle,
        
        /// <summary>Traveling between locations</summary>
        Traveling,
        
        /// <summary>Resting in friendly territory</summary>
        Resting,
        
        /// <summary>Defending a settlement</summary>
        Defending,
        
        /// <summary>Raiding enemy territory</summary>
        Raiding,
        
        /// <summary>Fleeing from enemy</summary>
        Fleeing
    }

    /// <summary>
    /// Priority level for lord's orders.
    /// Affects how schedule balances lord's needs vs lance needs.
    /// </summary>
    public enum LordOrderPriority
    {
        /// <summary>Low priority - lance needs take precedence</summary>
        Low,
        
        /// <summary>Medium priority - balanced approach</summary>
        Medium,
        
        /// <summary>High priority - lord's orders take precedence</summary>
        High,
        
        /// <summary>Critical priority - must follow orders regardless of lance needs</summary>
        Critical
    }
}

