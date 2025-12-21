using System;
using Enlisted.Features.Enlistment.Behaviors;
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

            // Event screen testing is currently inactive.
            ModLogger.Info("Debug", "TestOnboardingScreen: This feature is currently disabled.");
            return;

            /* Original test code has been removed. */
        }
    }
}

