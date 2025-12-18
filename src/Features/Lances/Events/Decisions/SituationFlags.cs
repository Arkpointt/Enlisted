namespace Enlisted.Features.Lances.Events.Decisions
{
    /// <summary>
    /// Small, stable set of “situation flags” derived from the Daily Report snapshot + live state.
    /// These flags are used to drive decision availability without putting custom logic into UI code.
    /// </summary>
    public sealed class SituationFlags
    {
        public bool CompanyFoodCritical { get; set; }
        public bool CompanyThreatHigh { get; set; }
        public bool LanceFeverSpike { get; set; }
        public bool LanceShortHanded { get; set; }
        public bool BattleImminent { get; set; }
    }
}


