using Enlisted.Services.Abstractions;
using TaleWorlds.CampaignSystem;

namespace Enlisted.Services.Impl
{
    internal sealed class PartyIllusionServiceImpl : IPartyIllusionService
    {
        public void HidePlayerPartyAndFollowCommander(Hero commander) => PartyIllusionService.HidePlayerPartyAndFollowCommander(commander);
        public void RestorePlayerPartyVisibility() => PartyIllusionService.RestorePlayerPartyVisibility();
        public void MaintainIllusion(Hero commander) => PartyIllusionService.MaintainIllusion(commander);
        public bool GetOriginalVisibilityState() => PartyIllusionService.GetOriginalVisibilityState();
        public void SetOriginalVisibilityState(bool wasVisible) => PartyIllusionService.SetOriginalVisibilityState(wasVisible);
    }
}
