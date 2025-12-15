namespace Enlisted.Features.Camp.UI.Hub
{
    /// <summary>
    /// Constants for the 6 camp location IDs.
    /// Phase 2: Camp Hub & Location System.
    /// </summary>
    public static class CampLocations
    {
        public const string MedicalTent = "medical_tent";
        public const string TrainingGrounds = "training_grounds";
        public const string LordsTent = "lords_tent";
        public const string Quartermaster = "quartermaster";
        public const string PersonalQuarters = "personal_quarters";
        public const string CampFire = "camp_fire";

        /// <summary>
        /// Returns display name for location ID.
        /// </summary>
        public static string GetLocationName(string locationId)
        {
            return locationId switch
            {
                MedicalTent => "Medical Tent",
                TrainingGrounds => "Training Grounds",
                LordsTent => "Lord's Tent",
                Quartermaster => "Quartermaster",
                PersonalQuarters => "Personal Quarters",
                CampFire => "Camp Fire",
                _ => "Unknown Location"
            };
        }

        /// <summary>
        /// Returns icon (Unicode emoji) for location ID.
        /// </summary>
        public static string GetLocationIcon(string locationId)
        {
            return locationId switch
            {
                MedicalTent => "🏥",
                TrainingGrounds => "⚔️",
                LordsTent => "🎪",
                Quartermaster => "📦",
                PersonalQuarters => "🛏️",
                CampFire => "🔥",
                _ => "📍"
            };
        }

        /// <summary>
        /// Returns color (8-digit hex with alpha) for location ID.
        /// </summary>
        public static string GetLocationColor(string locationId)
        {
            return locationId switch
            {
                MedicalTent => "#DD0000FF",      // Red
                TrainingGrounds => "#FFAA33FF",   // Orange
                LordsTent => "#4444AAFF",         // Blue
                Quartermaster => "#44AA44FF",      // Green
                PersonalQuarters => "#AA44AAFF",  // Purple
                CampFire => "#FF6622FF",          // Bright Orange
                _ => "#FFFFFFFF"                     // White
            };
        }

        /// <summary>
        /// Returns formatted header title for location screen.
        /// </summary>
        public static string GetLocationHeaderTitle(string locationId)
        {
            return locationId switch
            {
                MedicalTent => "🏥 MEDICAL TENT",
                TrainingGrounds => "⚔️ TRAINING GROUNDS",
                LordsTent => "🎪 LORD'S TENT",
                Quartermaster => "📦 QUARTERMASTER",
                PersonalQuarters => "🛏️ PERSONAL QUARTERS",
                CampFire => "🔥 CAMP FIRE",
                _ => "⚔ CAMP ACTIVITIES"
            };
        }

        /// <summary>
        /// Returns all valid location IDs in preferred display order.
        /// </summary>
        public static string[] GetAllLocationIds()
        {
            return new[]
            {
                MedicalTent,
                TrainingGrounds,
                LordsTent,
                Quartermaster,
                PersonalQuarters,
                CampFire
            };
        }
    }
}
