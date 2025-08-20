using System;
using Enlisted.Services.Abstractions;
using TaleWorlds.CampaignSystem;

namespace Enlisted.Services.Impl
{
    internal sealed class ArmyServiceImpl : IArmyService
    {
        public bool TryJoinCommandersArmy(Hero commander) => ArmyService.TryJoinCommandersArmy(commander);
        public void LeaveCurrentArmy() => ArmyService.LeaveCurrentArmy();
        public void SafeDetach() => ArmyService.SafeDetach();
    }
}
