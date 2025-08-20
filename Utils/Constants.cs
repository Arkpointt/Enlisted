namespace Enlisted.Utils
{
    /// <summary>
    /// Constants used throughout the Enlisted mod.
    /// Prevents magic strings and centralizes configuration.
    /// </summary>
    public static class Constants
    {
        // Dialog Configuration
        public static readonly string[] MAIN_HUBS = { "lord_talk", "hero_main_options" };
        public const string DIALOG_ID_PREFIX = "enlisted";
        public const int DIALOG_PRIORITY = 110;

        // Message Prefixes
        public const string MESSAGE_PREFIX = "[Enlisted]";

        // Save Field IDs (for SaveableField attributes)
        public const int SAVE_FIELD_IS_ENLISTED = 1;
        public const int SAVE_FIELD_COMMANDER = 2;
        public const int SAVE_FIELD_PENDING_DETACH = 3;
        public const int SAVE_FIELD_PLAYER_PARTY_WAS_VISIBLE = 4;

        // Game Messages
        public static class Messages
        {
            public const string ENLISTED_SUCCESS = MESSAGE_PREFIX + " You have enlisted under {0}.";
            public const string LEFT_SERVICE = MESSAGE_PREFIX + " You have left service.";
            public const string SERVICE_ENDED = MESSAGE_PREFIX + " Service ended (commander unavailable).";
            public const string FOLLOWING_COMMANDER = MESSAGE_PREFIX + " Following your commander's party.";
            public const string RESTORED_COMMAND = MESSAGE_PREFIX + " Restored independent command.";
            public const string JOINED_ARMY = MESSAGE_PREFIX + " You have joined {0}'s army.";
            public const string COULD_NOT_FIND_PARTIES = MESSAGE_PREFIX + " Could not find parties to join.";
            public const string COULD_NOT_HIDE_PARTY = MESSAGE_PREFIX + " Could not hide party: {0}";
            public const string COULD_NOT_RESTORE_PARTY = MESSAGE_PREFIX + " Could not restore party: {0}";
            public const string FOLLOWING_INTO_SETTLEMENT = MESSAGE_PREFIX + " Following {0} into {1}.";
        }

        // Settings (for future use)
        public static class Settings
        {
            public const bool DEFAULT_HIDE_PARTY_ON_ENLIST = true;
            public const bool DEFAULT_FOLLOW_CAMERA_ON_ENLIST = true;
            public const bool DEFAULT_AUTO_FOLLOW_INTO_SETTLEMENTS = true;
        }
    }
}