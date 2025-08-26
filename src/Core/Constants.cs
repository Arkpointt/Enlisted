namespace Enlisted.Core.Constants
{
    /// <summary>
    /// Application-wide constants following blueprint config-over-code principle.
    /// Centralized configuration to avoid magic strings throughout codebase.
    /// </summary>
    public static class Constants
    {
        // Dialog system configuration
        public const string DIALOG_ID_PREFIX = "enlisted";
        public const int DIALOG_PRIORITY = 120;
        
        // Main conversation hubs where enlistment dialogs are available
        public static readonly string[] MAIN_HUBS = 
        {
            "hero_main_options",
            "lord_talk_speak_diplomacy_2"
        };

        /// <summary>
        /// User-facing messages for game events.
        /// Centralized for consistency and future localization support.
        /// </summary>
        public static class Messages
        {
            public const string ENLISTED_SUCCESS = "Enlisted under {0}. Follow their orders.";
            public const string LEFT_SERVICE = "You have left military service.";
            public const string SERVICE_ENDED = "Your military service has ended.";
            public const string FOLLOWING_COMMANDER = "Following your commander...";
            public const string FOLLOWING_INTO_SETTLEMENT = "Following {0} into {1}...";
            public const string RESTORED_COMMAND = "Command restored to your party.";
            public const string COULD_NOT_FIND_PARTIES = "Could not locate required parties for enlistment.";
            public const string COULD_NOT_HIDE_PARTY = "Could not hide party properly: {0}";
            public const string COULD_NOT_RESTORE_PARTY = "Could not restore party visibility: {0}";
        }
    }
}
