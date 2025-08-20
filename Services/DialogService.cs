using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.GameMenus;
using Enlisted.Utils;

namespace Enlisted.Services
{
    /// <summary>
    /// Handles dialog registration for enlist/leave conversations.
    /// Centralizes all conversation logic and text management.
    /// </summary>
    public static class DialogService
    {
        /// <summary>
        /// Registers enlist and leave dialogs on the specified hubs.
        /// The party_wait menu is registered but only used when explicitly needed.
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

            // Register the party wait menu - kept for potential future use
            // but no longer automatically triggered on settlement exits
            RegisterPartyWaitMenu(starter);
        }

        private static void RegisterEnlistDialog(CampaignGameStarter starter, string hub,
            ConversationSentence.OnConditionDelegate canEnlist, Action onEnlist)
        {
            // ENLIST
            starter.AddPlayerLine(
                $"{Constants.DIALOG_ID_PREFIX}_enlist_ask__{hub}",
                hub,
                $"{Constants.DIALOG_ID_PREFIX}_enlist_confirm__{hub}",
                DialogTexts.ENLIST_REQUEST,
                canEnlist,
                null,
                Constants.DIALOG_PRIORITY
            );

            starter.AddDialogLine(
                $"{Constants.DIALOG_ID_PREFIX}_enlist_confirm__{hub}",
                $"{Constants.DIALOG_ID_PREFIX}_enlist_confirm__{hub}",
                "close_window",
                DialogTexts.ENLIST_RESPONSE,
                null,
                () => onEnlist?.Invoke(),
                Constants.DIALOG_PRIORITY
            );
        }

        private static void RegisterLeaveDialog(CampaignGameStarter starter, string hub,
            ConversationSentence.OnConditionDelegate canLeave, Action onLeave)
        {
            // LEAVE
            starter.AddPlayerLine(
                $"{Constants.DIALOG_ID_PREFIX}_leave_ask__{hub}",
                hub,
                $"{Constants.DIALOG_ID_PREFIX}_leave_confirm__{hub}",
                DialogTexts.LEAVE_REQUEST,
                canLeave,
                null,
                Constants.DIALOG_PRIORITY
            );

            starter.AddDialogLine(
                $"{Constants.DIALOG_ID_PREFIX}_leave_confirm__{hub}",
                $"{Constants.DIALOG_ID_PREFIX}_leave_confirm__{hub}",
                "close_window",
                DialogTexts.LEAVE_RESPONSE,
                null,
                () => onLeave?.Invoke(),
                Constants.DIALOG_PRIORITY
            );
        }

        /// <summary>
        /// Registers the party_wait menu for manual use when following the commander.
        /// This menu is no longer automatically triggered but kept for potential future features.
        /// </summary>
        private static void RegisterPartyWaitMenu(CampaignGameStarter starter)
        {
            starter.AddWaitGameMenu(
                "party_wait", 
                "Following {COMMANDER_NAME}.\n{PARTY_STATUS}",
                (args) => {
                    // Set menu title and status when manually activated
                    // This menu is now only used for explicit wait commands, not automatic settlement transitions
                },
                (args) => true, // Always available when manually called
                null,
                (args, dt) => {
                    // Menu tick - handles wait time progression
                },
                TaleWorlds.CampaignSystem.GameMenus.GameMenu.MenuAndOptionType.WaitMenuShowOnlyProgressOption
            );

            // Add option to leave service from the wait menu
            // This is now only accessible through manual menu activation
            starter.AddGameMenuOption(
                "party_wait",
                "party_wait_leave_service",
                "Leave service",
                (args) => {
                    args.optionLeaveType = TaleWorlds.CampaignSystem.GameMenus.GameMenuOption.LeaveType.Leave;
                    return true;
                },
                (args) => {
                    // Exit the menu gracefully - behavior will handle the actual leaving
                    GameMenu.ExitToLast();
                }
            );
        }
    }

    /// <summary>
    /// Contains all dialog text constants.
    /// </summary>
    public static class DialogTexts
    {
        public const string ENLIST_REQUEST = "I wish to enlist in your army.";
        public const string ENLIST_RESPONSE = "Very well. Fall in with my party.";
        public const string LEAVE_REQUEST = "I'd like to leave your service.";
        public const string LEAVE_RESPONSE = "As you wish. You are dismissed.";
    }
}