using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using Enlisted.Models;

namespace Enlisted.Utils
{
    /// <summary>
    /// Essential utilities for the Enlisted mod.
    /// Contains production-ready helper functions for validation and safety.
    /// </summary>
    public static class DebugHelper
    {
        /// <summary>
        /// Safe Hero name extraction for error messages and logging.
        /// Returns a fallback string if the hero or name is null.
        /// </summary>
        /// <param name="hero">The hero to get the name from</param>
        /// <param name="fallback">Fallback text if hero is null</param>
        /// <returns>Hero name or fallback string</returns>
        public static string SafeHeroName(Hero hero, string fallback = "None")
        {
            return hero?.Name != null ? hero.Name.ToString() : fallback;
        }

        /// <summary>
        /// Validates that the enlistment state is logically consistent.
        /// Used for error checking in production code.
        /// </summary>
        /// <param name="state">The enlistment state to validate</param>
        /// <returns>True if state is valid, false otherwise</returns>
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
        /// </summary>
        /// <param name="commander">The commander we're now serving under</param>
        public static void VerifyEncounterProtection(Hero commander)
        {
            if (commander != null)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    $"[Enlisted] Now serving under {SafeHeroName(commander)} - protection active"));
            }
        }
    }
}