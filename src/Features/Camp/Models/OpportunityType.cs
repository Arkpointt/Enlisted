namespace Enlisted.Features.Camp.Models
{
    /// <summary>
    /// Categories of camp opportunities that can be generated.
    /// Used for filtering, fitness scoring, and learning player preferences.
    /// </summary>
    public enum OpportunityType
    {
        /// <summary>Skill improvement activities (drilling, sparring, equipment maintenance).</summary>
        Training,

        /// <summary>Social interactions (card games, fire circles, storytelling).</summary>
        Social,

        /// <summary>Money-making activities (gambling, side work, trade).</summary>
        Economic,

        /// <summary>Rest and healing activities (sleep, medical treatment).</summary>
        Recovery,

        /// <summary>Context-specific opportunities (lord audience, town leave, baggage access).</summary>
        Special
    }
}
