using System;
using Enlisted.Features.Enlistment.Behaviors;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace Enlisted.Features.Activities
{
    /// <summary>
    /// Centralized helper for awarding "Free Time" training XP in a way that:
    /// - Stays relevant across Enlisted tiers (tier-scaled baseline)
    /// - Respects native learning mechanics (focus/attribute learning rate)
    /// - Avoids becoming useless at high skill levels (diminishing returns)
    ///
    /// IMPORTANT:
    /// We return "raw XP" to feed into native <see cref="Hero.AddSkillXp"/>. Native code will still apply:
    /// - GenericXpModel multiplier
    /// - Learning rate (focus factor)
    /// - Skill level threshold checks + skill level-ups
    ///
    /// Our only intervention around learning rate is a mild compensation clamp so training does not become
    /// completely irrelevant when the player is far beyond their learning limit, while still not bypassing
    /// learning limits entirely.
    /// </summary>
    public static class TrainingXpScaler
    {
        // Baseline tuning knobs (intentionally constants for now; can be moved to config later if needed).
        private const float BaseRawXp = 40f;
        private const float RawXpPerEnlistmentTier = 10f;

        // Diminishing returns: training is less effective at very high skill values.
        // SkillDecay = clamp(1 - skill/400, MinSkillDecay..1).
        private const float SkillDecayDivisor = 400f;
        private const float MinSkillDecay = 0.35f;

        // Learning rate compensation:
        // Native applies learning rate internally. We only clamp the *effective* learning rate range so
        // training doesn't feel dead at very low learning rates or wildly strong at very high rates.
        //
        // We do NOT fully "fix" low learning rates; we only compensate down to MinCompensationSourceRate.
        private const float MinEffectiveLearningRate = 0.20f;
        private const float MaxEffectiveLearningRate = 1.50f;
        private const float MinCompensationSourceRate = 0.10f; // caps maximum compensation at 2.0x

        /// <summary>
        /// Calculates the raw XP to pass to <see cref="Hero.AddSkillXp"/> for a Free Time training action.
        /// </summary>
        /// <param name="hero">Target hero (usually Hero.MainHero).</param>
        /// <param name="skill">Skill to train.</param>
        /// <param name="enlistmentTier">Enlisted tier (T1..T9). Values &lt; 1 are treated as 1.</param>
        /// <returns>Raw XP amount (float) to pass to native AddSkillXp.</returns>
        public static float CalculateRawTrainingXp(Hero hero, SkillObject skill, int enlistmentTier)
        {
            try
            {
                if (hero == null || skill == null || Campaign.Current?.Models?.CharacterDevelopmentModel == null)
                {
                    return 0f;
                }

                // Tier scaling keeps training relevant as the player advances.
                var tier = Math.Max(1, enlistmentTier);
                var tierScaledBase = BaseRawXp + (tier * RawXpPerEnlistmentTier);

                // Diminishing returns by current skill value.
                var skillValue = hero.GetSkillValue(skill);
                var decay = 1f - (skillValue / SkillDecayDivisor);
                decay = MathF.Clamp(decay, MinSkillDecay, 1f);

                // Mild learning-rate compensation to keep training from becoming completely irrelevant when capped.
                // Native applies the learning rate internally; we compensate by adjusting raw XP *before* passing it in.
                var focusFactor = GetNativeLearningRate(hero, skill);
                var effectiveTarget = MathF.Clamp(focusFactor, MinEffectiveLearningRate, MaxEffectiveLearningRate);

                // Limit how much we compensate when focusFactor is extremely low.
                var safeSource = Math.Max(focusFactor, MinCompensationSourceRate);
                var compensation = safeSource > 0f ? (effectiveTarget / safeSource) : 1f;

                var raw = tierScaledBase * decay * compensation;
                return Math.Max(0f, raw);
            }
            catch
            {
                return 0f;
            }
        }

        /// <summary>
        /// Convenience helper to grant scaled training XP using native progression.
        /// </summary>
        public static void GrantScaledTrainingXp(Hero hero, SkillObject skill, int enlistmentTier, bool notify = true)
        {
            if (hero == null || skill == null)
            {
                return;
            }

            var raw = CalculateRawTrainingXp(hero, skill, enlistmentTier);
            if (raw <= 0f)
            {
                return;
            }

            // Call through HeroDeveloper so we can control notification behavior.
            // This applies learning rate, skill thresholds, and increments HeroDeveloper._totalXp
            // (driving character level progression).
            hero.HeroDeveloper?.AddSkillXp(skill, raw, isAffectedByFocusFactor: true, shouldNotify: notify);

            // Award enlistment XP for rank progression (what shows in muster reports)
            if (hero == Hero.MainHero)
            {
                EnlistmentBehavior.Instance?.AddEnlistmentXP((int)raw, $"Training: {skill.Name}");
            }
        }

        private static float GetNativeLearningRate(Hero hero, SkillObject skill)
        {
            try
            {
                // HeroDeveloper.GetFocusFactor(skill) is effectively the native learning rate:
                // CharacterDevelopmentModel.CalculateLearningRate(...).ResultNumber
                var developer = hero?.HeroDeveloper;
                if (developer == null)
                {
                    return 1f;
                }

                var rate = developer.GetFocusFactor(skill);
                if (float.IsNaN(rate) || float.IsInfinity(rate))
                {
                    return 1f;
                }

                // Safety clamp (native also clamps to >= 0, but we avoid weird values).
                return Math.Max(0f, rate);
            }
            catch
            {
                return 1f;
            }
        }
    }
}


