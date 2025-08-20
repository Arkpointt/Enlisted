using System;
using Enlisted.Services.Abstractions;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Conversation;

namespace Enlisted.Services.Impl
{
    internal sealed class DialogServiceImpl : IDialogService
    {
        public void RegisterDialogs(CampaignGameStarter starter, string[] hubs, ConversationSentence.OnConditionDelegate canEnlist, ConversationSentence.OnConditionDelegate canLeave, Action onEnlist, Action onLeave)
            => DialogService.RegisterDialogs(starter, hubs, canEnlist, canLeave, onEnlist, onLeave);
    }
}
