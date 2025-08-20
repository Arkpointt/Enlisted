using Enlisted.Services.Abstractions;

namespace Enlisted.Services
{
    /// <summary>
    /// Simple locator for service interfaces so Behaviors and patches can reach services
    /// without depending on concrete implementations. Configured from SubModule.
    /// </summary>
    internal static class ServiceLocator
    {
        public static IArmyService Army { get; private set; }
        public static IPartyIllusionService PartyIllusion { get; private set; }
        public static IDialogService Dialog { get; private set; }
        public static IPromotionRules PromotionRules { get; private set; }

        public static void Configure(
            IArmyService army,
            IPartyIllusionService partyIllusion,
            IDialogService dialog,
            IPromotionRules promotionRules)
        {
            Army = army;
            PartyIllusion = partyIllusion;
            Dialog = dialog;
            PromotionRules = promotionRules;
        }
    }
}
