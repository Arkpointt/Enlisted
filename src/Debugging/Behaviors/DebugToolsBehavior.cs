using System;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Features.Lances.Events;
using Enlisted.Features.Lances.UI;
using Enlisted.Mod.Core.Logging;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace Enlisted.Debugging.Behaviors
{
    /// <summary>
    ///     Lightweight in-game debug helpers for QA: grant gold and enlistment XP.
    ///     Keeps state changes minimal and logged for traceability.
    /// </summary>
    public static class DebugToolsBehavior
    {
        private const int GoldPerClick = 1000;
        private const int XpPerClick = 2000;

        public static void GiveGold()
        {
            var hero = Hero.MainHero;
            if (hero == null)
            {
                return;
            }

            hero.Gold += GoldPerClick;
            var msg = new TextObject("{=dbg_gold_added}+{G} gold granted (debug).");
            msg.SetTextVariable("G", GoldPerClick);
            InformationManager.DisplayMessage(new InformationMessage(msg.ToString()));
            SessionDiagnostics.LogEvent("Debug", "GiveGold", $"gold={GoldPerClick}, total={hero.Gold}");
        }

        public static void GiveEnlistmentXp()
        {
            var enlist = EnlistmentBehavior.Instance;
            if (enlist?.IsEnlisted != true)
            {
                var warn = new TextObject("{=dbg_xp_not_enlisted}Cannot grant XP while not enlisted.");
                InformationManager.DisplayMessage(new InformationMessage(warn.ToString()));
                return;
            }

            enlist.AddEnlistmentXP(XpPerClick, "Debug");
            var msg = new TextObject("{=dbg_xp_added}+{XP} enlistment XP granted (debug).");
            msg.SetTextVariable("XP", XpPerClick);
            InformationManager.DisplayMessage(new InformationMessage(msg.ToString()));
            SessionDiagnostics.LogEvent("Debug", "GiveXP",
                $"xp={XpPerClick}, total={enlist.EnlistmentXP}, tier={enlist.EnlistmentTier}");
        }

        /// <summary>
        /// Test the custom onboarding event screen with a sample event.
        /// Creates a test event and displays it using the modern Gauntlet UI.
        /// </summary>
        public static void TestOnboardingScreen()
        {
            var enlist = EnlistmentBehavior.Instance;
            if (enlist == null)
            {
                var warn = new TextObject("{=dbg_test_no_enlist}Cannot test event screen - EnlistmentBehavior not found.");
                InformationManager.DisplayMessage(new InformationMessage(warn.ToString()));
                ModLogger.Warn("Debug", "TestOnboardingScreen: EnlistmentBehavior.Instance is null");
                return;
            }

            ModLogger.Info("Debug", "TestOnboardingScreen: Creating test event...");

            // Create a test onboarding event
            var testEvent = new LanceLifeEventDefinition
            {
                Id = "debug_test_onboarding",
                Category = "onboarding",
                Track = "enlisted",
                TitleFallback = "Test Event - Welcome to Enlisted",
                SetupFallback = "This is a test of the custom Gauntlet event screen.\n\nThe modern UI should display:\n- This story text\n- Character visualization (if configured)\n- Your escalation tracks (heat, discipline, reputation)\n- Multiple choice buttons below\n\nThis is a debug test to verify the UI is working correctly.",
                Options = new System.Collections.Generic.List<LanceLifeEventOptionDefinition>
                {
                    new LanceLifeEventOptionDefinition
                    {
                        Id = "test_option_1",
                        TextFallback = "This looks great! Close the screen.",
                        Tooltip = "Test option 1",
                        Risk = "safe",
                        OutcomeTextFallback = "Screen test successful! The custom Gauntlet UI is working.\n\nCheck the logs for detailed information about the screen opening process.",
                        Effects = new LanceLifeEventEscalationEffects
                        {
                            Heat = 0,
                            Discipline = 0,
                            LanceReputation = 0
                        }
                    },
                    new LanceLifeEventOptionDefinition
                    {
                        Id = "test_option_2",
                        TextFallback = "Test a different choice",
                        Tooltip = "Test option 2",
                        Risk = "safe",
                        OutcomeTextFallback = "You selected the second option. Everything is working as expected!",
                        Effects = new LanceLifeEventEscalationEffects
                        {
                            Heat = 1,
                            Discipline = -1,
                            LanceReputation = 0
                        }
                    }
                }
            };

            ModLogger.Info("Debug", $"TestOnboardingScreen: Attempting to show test event directly (bypass bag check)");

            // Temporarily clear bag check flag for testing
            var wasPending = enlist.IsBagCheckPending;
            if (wasPending)
            {
                ModLogger.Info("Debug", "TestOnboardingScreen: Temporarily bypassing bag check for test");
                // Force clear the pending flag for testing
                try
                {
                    var field = typeof(EnlistmentBehavior).GetField("_isBagCheckPending", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (field != null)
                    {
                        field.SetValue(enlist, false);
                    }
                }
                catch (Exception ex)
                {
                    ModLogger.Warn("Debug", $"Could not bypass bag check: {ex.Message}");
                }
            }

            // Try to show using the modern presenter
            var shown = ModernEventPresenter.TryShowWithFallback(testEvent, enlist, useModernUI: true);

            // Restore bag check flag
            if (wasPending)
            {
                try
                {
                    var field = typeof(EnlistmentBehavior).GetField("_isBagCheckPending", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (field != null)
                    {
                        field.SetValue(enlist, wasPending);
                    }
                }
                catch { }
            }

            if (shown)
            {
                ModLogger.Info("Debug", "TestOnboardingScreen: Event screen queued successfully");
                var msg = new TextObject("{=dbg_test_event_shown}Test event screen queued. Check for the popup!");
                InformationManager.DisplayMessage(new InformationMessage(msg.ToString()));
            }
            else
            {
                ModLogger.Warn("Debug", "TestOnboardingScreen: Failed to show event screen");
                var warn = new TextObject("{=dbg_test_event_failed}Failed to show test event screen. Check logs.");
                InformationManager.DisplayMessage(new InformationMessage(warn.ToString()));
            }

            SessionDiagnostics.LogEvent("Debug", "TestOnboardingScreen", $"shown={shown}");
        }
    }
}

