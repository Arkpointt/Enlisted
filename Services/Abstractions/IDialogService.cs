namespace Enlisted.Services.Abstractions
{
    using System;
    using TaleWorlds.CampaignSystem;
    using TaleWorlds.CampaignSystem.Conversation;

    /// <summary>
    /// Contract for registering enlist/leave dialogs on conversation hubs.
    /// Keeps dialog wiring separate from behavior orchestration.
    /// </summary>
    public interface IDialogService
    {
        /// <summary>
        /// Register enlist/leave dialogs and wire their conditions and actions.
        /// </summary>
        /// <param name="starter">Campaign starter provided by the game.</param>
        /// <param name="hubs">Conversation hub ids (e.g., lord_talk, hero_main_options).</param>
        /// <param name="canEnlist">Condition delegate for showing enlist option.</param>
        /// <param name="canLeave">Condition delegate for showing leave option.</param>
        /// <param name="onEnlist">Action when enlist is confirmed.</param>
        /// <param name="onLeave">Action when leave is confirmed.</param>
        void RegisterDialogs(
            CampaignGameStarter starter,
            string[] hubs,
            ConversationSentence.OnConditionDelegate canEnlist,
            ConversationSentence.OnConditionDelegate canLeave,
            Action onEnlist,
            Action onLeave);
    }
}
