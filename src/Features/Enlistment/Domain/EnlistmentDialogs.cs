using System.Collections.Generic;
using TaleWorlds.Localization;

namespace Enlisted.Features.Enlistment.Domain
{
    /// <summary>
    /// Centralized dialog configuration for the enlistment system.
    /// All conversation text is stored here for easy modification and localization.
    /// </summary>
    public static class EnlistmentDialogs
    {
        // Enlistment Request Flow
        public static class EnlistmentRequest
        {
            public static string PlayerRequest => "I would like to join your army as a soldier.";
            public static string LordAccept => "I would be honored to have you serve in my army. Welcome aboard!";
            public static string LordDecline => "I'm sorry, but I cannot accept you into my army at this time.";
            public static string PlayerConfirm => "Thank you, my lord. I will serve you faithfully.";
        }

        // Leave Request Flow
        public static class LeaveRequest
        {
            public static string PlayerRequest => "I would like to request leave from your army.";
            public static string CommanderAccept => "I understand. You have served well. You are free to go.";
            public static string CommanderDecline => "I cannot grant you leave at this time. We need every able soldier.";
            public static string PlayerConfirm => "Thank you for the opportunity to serve, my lord.";
        }

        // Menu Text
        public static class Menus
        {
            public static string SoldierStatusTitle => "Soldier Status - {COMMANDER_NAME}";
            public static string SoldierStatusDescription => "You are currently serving as a soldier in {COMMANDER_NAME}'s army.";
            public static string AskLeaveOption => "Ask for leave from the army";
            public static string ReturnToCampaignOption => "Return to campaign";
        }

        // Information Messages
        public static class Messages
        {
            public static string EnlistmentSuccess => "You have successfully enlisted in {COMMANDER_NAME}'s army!";
            public static string LeaveSuccess => "You have left the army and returned to your own party.";
            public static string LeaveDenied => "Your leave request has been denied.";
            public static string PromotionMessage => "You have been promoted to tier {TIER}!";
        }

        // Dialog IDs (for conversation flow)
        public static class DialogIds
        {
            // Enlistment flow
            public const string Start = "enlistment_start";
            public const string AskToJoin = "enlistment_ask_to_join";
            public const string Accepted = "enlistment_accepted";
            public const string Confirm = "enlistment_confirm";

            // Leave flow
            public const string AskLeave = "enlistment_ask_leave";
            public const string LeaveRequest = "enlistment_leave_request";
            public const string LeaveAccepted = "enlistment_leave_accepted";
            public const string LeaveConfirm = "enlistment_leave_confirm";

            // Menu IDs
            public const string SoldierStatus = "enlisted_soldier_status";
            public const string AskLeaveOption = "enlisted_ask_leave";
            public const string ReturnCampaignOption = "enlisted_return_campaign";
        }

        /// <summary>
        /// Gets a formatted dialog text with variable substitution
        /// </summary>
        public static string FormatDialog(string template, Dictionary<string, string> variables)
        {
            string result = template;
            foreach (var variable in variables)
            {
                result = result.Replace($"{{{variable.Key}}}", variable.Value);
            }
            return result;
        }

        /// <summary>
        /// Gets enlistment success message with commander name
        /// </summary>
        public static string GetEnlistmentSuccessMessage(string commanderName)
        {
            return FormatDialog(Messages.EnlistmentSuccess, new Dictionary<string, string>
            {
                { "COMMANDER_NAME", commanderName }
            });
        }

        /// <summary>
        /// Gets promotion message with tier number
        /// </summary>
        public static string GetPromotionMessage(int tier)
        {
            return FormatDialog(Messages.PromotionMessage, new Dictionary<string, string>
            {
                { "TIER", tier.ToString() }
            });
        }

        /// <summary>
        /// Gets soldier status title with commander name
        /// </summary>
        public static string GetSoldierStatusTitle(string commanderName)
        {
            return FormatDialog(Menus.SoldierStatusTitle, new Dictionary<string, string>
            {
                { "COMMANDER_NAME", commanderName }
            });
        }

        /// <summary>
        /// Gets soldier status description with commander name
        /// </summary>
        public static string GetSoldierStatusDescription(string commanderName)
        {
            return FormatDialog(Menus.SoldierStatusDescription, new Dictionary<string, string>
            {
                { "COMMANDER_NAME", commanderName }
            });
        }
    }
}
