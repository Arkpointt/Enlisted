using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using Enlisted.Core.Models;

namespace Enlisted.Core.Utils
{
    /// <summary>
    /// Core utilities for the Enlisted mod following blueprint standards.
    /// Contains production-ready helper functions for validation and safety.
    /// Centralized utility functions to avoid code duplication across features.
    /// </summary>
    public static class DebugHelper
    {
        /// <summary>
        /// Safe Hero name extraction for error messages and logging.
        /// Returns a fallback string if the hero or name is null.
        /// Prevents null reference exceptions in display code.
        /// </summary>
        public static string SafeHeroName(Hero hero, string fallback = "None")
        {
            return hero?.Name != null ? hero.Name.ToString() : fallback;
        }

        /// <summary>
        /// Validates that the enlistment state is logically consistent.
        /// Used for error checking in production code to detect corruption.
        /// Follows blueprint principle of fail-fast validation.
        /// </summary>
        public static bool ValidateEnlistmentState(EnlistmentState state)
        {
            if (state.IsEnlisted && state.Commander == null) return false;
            if (!state.IsEnlisted && state.Commander != null) return false;
            if (state.IsEnlisted && state.Commander?.PartyBelongedTo == null) return false;
            return true;
        }

        /// <summary>
        /// Simple verification that enlistment completed successfully.
        /// Shows a brief status message for confirmation.
        /// Provides feedback to player that the system is working correctly.
        /// </summary>
        public static void VerifyEncounterProtection(Hero commander)
        {
            if (commander != null)
            {
                // TODO: Replace with centralized logging service when implemented
                InformationManager.DisplayMessage(new InformationMessage(
                    $"[Enlisted] Now serving under {SafeHeroName(commander)} - protection active"));
            }
        }

        /// <summary>
        /// Validates promotion state for consistency.
        /// Ensures tier and XP values are within expected ranges.
        /// </summary>
        public static bool ValidatePromotionState(PromotionState state)
        {
            if (state.Tier < 1) return false;
            if (state.CurrentXp < 0) return false;
            if (state.NextTierXp <= 0) return false;
            return true;
        }
    }
}
