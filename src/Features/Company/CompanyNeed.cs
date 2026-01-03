namespace Enlisted.Features.Company
{
    /// <summary>
    /// The four core needs that must be balanced for company health and effectiveness.
    /// Represents the enlisted lord's party needs (readiness, morale, rest, supplies).
    /// Each need ranges from 0-100, with thresholds at 30 (Poor) and 20 (Critical).
    /// </summary>
    public enum CompanyNeed
    {
        /// <summary>Combat readiness and training level (0-100)</summary>
        Readiness,
        
        /// <summary>Morale and unit cohesion (0-100)</summary>
        Morale,
        
        /// <summary>Rest and fatigue management (0-100)</summary>
        Rest,
        
        /// <summary>Food, water, and supplies (0-100)</summary>
        Supplies
    }
}

