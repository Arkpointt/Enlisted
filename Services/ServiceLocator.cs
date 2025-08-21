using TaleWorlds.CampaignSystem;

namespace Enlisted.Services
{
    /// <summary>
    /// Minimal service locator that forwards to existing static service classes.
    /// Avoids interface/implementation wiring to keep the project simple and stable.
    /// </summary>
    internal static class ServiceLocator
    {
        public static ArmyFacade Army { get; } = new ArmyFacade();
        public static PartyIllusionFacade PartyIllusion { get; } = new PartyIllusionFacade();
        public static DialogFacade Dialog { get; } = new DialogFacade();
        public static PromotionRulesFacade PromotionRules { get; } = new PromotionRulesFacade();

        internal sealed class ArmyFacade
        {
            public bool TryJoinCommandersArmy(Hero commander) => ArmyService.TryJoinCommandersArmy(commander);
            public void LeaveCurrentArmy() => ArmyService.LeaveCurrentArmy();
            public void SafeDetach() => ArmyService.SafeDetach();
        }

        internal sealed class PartyIllusionFacade
        {
            public void HidePlayerPartyAndFollowCommander(Hero commander) => PartyIllusionService.HidePlayerPartyAndFollowCommander(commander);
            public void RestorePlayerPartyVisibility() => PartyIllusionService.RestorePlayerPartyVisibility();
            public void MaintainIllusion(Hero commander) => PartyIllusionService.MaintainIllusion(commander);
            public bool GetOriginalVisibilityState() => PartyIllusionService.GetOriginalVisibilityState();
            public void SetOriginalVisibilityState(bool wasVisible) => PartyIllusionService.SetOriginalVisibilityState(wasVisible);
        }

        internal sealed class DialogFacade
        {
            public void RegisterDialogs(CampaignGameStarter starter, string[] hubs,
                TaleWorlds.CampaignSystem.Conversation.ConversationSentence.OnConditionDelegate canEnlist,
                TaleWorlds.CampaignSystem.Conversation.ConversationSentence.OnConditionDelegate canLeave,
                System.Action onEnlist,
                System.Action onLeave)
            {
                DialogService.RegisterDialogs(starter, hubs, canEnlist, canLeave, onEnlist, onLeave);
            }
        }

        internal sealed class PromotionRulesFacade
        {
            public int GetRequiredXpForTier(int tier) => Enlisted.PromotionRules.GetRequiredXpForTier(tier);
        }
    }
}
