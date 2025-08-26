using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.GameMenus;

namespace Enlisted.Features.Enlistment.Infrastructure
{
    /// <summary>
    /// Infrastructure service handling dialog registration for enlist/leave conversations.
    /// Manages TaleWorlds conversation system integration while isolating presentation
    /// logic from domain concerns following blueprint separation.
    /// </summary>
    public static class DialogService
    {
        /// <summary>
        /// Registers enlist and leave dialogs on the specified conversation hubs.
        /// Provides clean integration with game's conversation system.
        /// </summary>
        public static void RegisterDialogs(CampaignGameStarter starter, string[] hubs, 
            ConversationSentence.OnConditionDelegate canEnlist,
            ConversationSentence.OnConditionDelegate canLeave,
            Action onEnlist,
            Action onLeave)
        {
            foreach (var hub in hubs)
            {
                RegisterEnlistDialog(starter, hub, canEnlist, onEnlist);
                RegisterLeaveDialog(starter, hub, canLeave, onLeave);
            }

            // Register party wait menu for potential future use
            RegisterPartyWaitMenu(starter);
        }

        private static void RegisterEnlistDialog(CampaignGameStarter starter, string hub,
            ConversationSentence.OnConditionDelegate canEnlist, Action onEnlist)
        {
            const string dialogIdPrefix = "enlisted";
            const int dialogPriority = 120;

            // Player request to enlist
            starter.AddPlayerLine(
                $"{dialogIdPrefix}_enlist_ask__{hub}",
                hub,
                $"{dialogIdPrefix}_enlist_confirm__{hub}",
                DialogTexts.ENLIST_REQUEST,
                canEnlist,
                null,
                dialogPriority
            );

            // NPC response confirming enlistment
            starter.AddDialogLine(
                $"{dialogIdPrefix}_enlist_confirm__{hub}",
                $"{dialogIdPrefix}_enlist_confirm__{hub}",
                "close_window",
                DialogTexts.ENLIST_RESPONSE,
                null,
                () => onEnlist?.Invoke(),
                dialogPriority
            );
        }

        private static void RegisterLeaveDialog(CampaignGameStarter starter, string hub,
            ConversationSentence.OnConditionDelegate canLeave, Action onLeave)
        {
            const string dialogIdPrefix = "enlisted";
            const int dialogPriority = 120;

            // Player request to leave service
            starter.AddPlayerLine(
                $"{dialogIdPrefix}_leave_ask__{hub}",
                hub,
                $"{dialogIdPrefix}_leave_confirm__{hub}",
                DialogTexts.LEAVE_REQUEST,
                canLeave,
                null,
                dialogPriority
            );

            // NPC response confirming discharge
            starter.AddDialogLine(
                $"{dialogIdPrefix}_leave_confirm__{hub}",
                $"{dialogIdPrefix}_leave_confirm__{hub}",
                "close_window",
                DialogTexts.LEAVE_RESPONSE,
                null,
                () => onLeave?.Invoke(),
                dialogPriority
            );
        }

        /// <summary>
        /// Registers party wait menu for manual use when following commander.
        /// Available for future features but not automatically triggered.
        /// </summary>
        private static void RegisterPartyWaitMenu(CampaignGameStarter starter)
        {
            starter.AddWaitGameMenu(
                "party_wait", 
                "Following {COMMANDER_NAME}.\n{PARTY_STATUS}",
                (args) => {
                    // Menu setup for manual activation
                },
                (args) => true, // Always available when manually called
                null,
                (args, dt) => {
                    // Wait time progression handling
                },
                GameMenu.MenuAndOptionType.WaitMenuShowOnlyProgressOption
            );

            // Option to leave service from wait menu
            starter.AddGameMenuOption(
                "party_wait",
                "party_wait_leave_service",
                "Leave service",
                (args) => {
                    args.optionLeaveType = GameMenuOption.LeaveType.Leave;
                    return true;
                },
                (args) => {
                    GameMenu.ExitToLast();
                }
            );
        }
    }

    /// <summary>
    /// Dialog text constants for conversation system.
    /// Centralized for consistency and future localization.
    /// </summary>
    public static class DialogTexts
    {
        public const string ENLIST_REQUEST = "I wish to enlist in your army.";
        public const string ENLIST_RESPONSE = "Very well. Fall in with my party.";
        public const string LEAVE_REQUEST = "I'd like to leave your service.";
        public const string LEAVE_RESPONSE = "As you wish. You are dismissed.";
    }
}
