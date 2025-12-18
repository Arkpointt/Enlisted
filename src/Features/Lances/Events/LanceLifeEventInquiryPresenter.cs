using System;
using System.Collections.Generic;
using System.Linq;
using Enlisted.Features.Assignments.Core;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Features.Equipment.Behaviors;
using EnlistedConfig = Enlisted.Features.Assignments.Core.ConfigurationManager;
using Enlisted.Mod.Core.Logging;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace Enlisted.Features.Lances.Events
{
    internal static class LanceLifeEventInquiryPresenter
    {
        private const string LogCategory = "LanceLifeEvents";

        // Global flag to prevent multiple popups from stacking
        private static bool _isEventShowing;
        
        // Captured time control mode to restore after popup closes
        private static CampaignTimeControlMode? _capturedTimeMode;
        
        /// <summary>
        /// Returns true if an event popup is currently being displayed.
        /// Used to prevent overlapping popups.
        /// </summary>
        public static bool IsEventShowing => _isEventShowing;
        
        /// <summary>
        /// Restore the time control mode that was active before the popup was shown.
        /// Uses stoppable equivalents to ensure player can still pause/unpause freely.
        /// </summary>
        private static void RestoreTimeControlMode()
        {
            try
            {
                if (Campaign.Current == null || !_capturedTimeMode.HasValue)
                {
                    return;
                }
                
                // Normalize to stoppable variants so player retains control
                var restored = QuartermasterManager.NormalizeToStoppable(_capturedTimeMode.Value);
                Campaign.Current.TimeControlMode = restored;
                ModLogger.Debug(LogCategory, $"Restored time mode after event popup: {restored} (was {_capturedTimeMode})");
                _capturedTimeMode = null;
            }
            catch (Exception ex)
            {
                ModLogger.Warn(LogCategory, $"Failed to restore time control mode: {ex.Message}");
            }
        }

        public static bool TryShow(LanceLifeEventDefinition evt, EnlistmentBehavior enlistment)
        {
            try
            {
                var cfg = EnlistedConfig.LoadLanceLifeEventsConfig() ?? new LanceLifeEventsConfig();
                if (!cfg.Enabled)
                {
                    return false;
                }

                if (evt == null)
                {
                    return false;
                }

                // Prevent multiple popups from stacking
                if (_isEventShowing)
                {
                    ModLogger.Info(LogCategory, $"Skipping event {evt.Id} - another event is already showing");
                    return false;
                }

                // Wait for bag check to complete before showing other events
                if (enlistment?.IsBagCheckPending == true)
                {
                    ModLogger.Info(LogCategory, $"Deferring event {evt.Id} - bag check pending");
                    return false;
                }

                // Phase 1 is only a presentation + effects layer. Scheduling/ai_safe checks occur in Phase 2+.
                // Still, we avoid showing anything if the player isn't in a campaign.
                if (Campaign.Current == null)
                {
                    return false;
                }

                var title = LanceLifeEventText.Resolve(evt.TitleId, evt.TitleFallback, "{=ll_default_title}Lance Activity", enlistment);
                var body = ResolveEffectiveSetupText(evt, enlistment);
                
                // Format body text for better readability in the popup
                // Break long paragraphs and add spacing for a cleaner, narrower layout
                body = FormatPopupText(body);

                var options = new List<InquiryElement>();
                foreach (var opt in ResolveEffectiveOptions(evt))
                {
                    if (opt == null)
                    {
                        continue;
                    }

                    var enabled = IsOptionEnabled(opt, Hero.MainHero, enlistment);
                    var optionText = LanceLifeEventText.Resolve(opt.TextId, opt.TextFallback, "{=ll_default_continue}Continue", enlistment);
                    
                    // Build hint text from costs/rewards for tooltip
                    var hintText = BuildOptionHint(opt);
                    
                    options.Add(new InquiryElement(opt, optionText, null, enabled, hintText));
                }

                if (options.Count == 0)
                {
                    return false;
                }

                // Mark event as showing to prevent stacking
                _isEventShowing = true;
                
                // Capture current time control mode so we can restore it when popup closes
                // This preserves the player's speed setting (paused, 1x, 2x, 3x)
                _capturedTimeMode = Campaign.Current?.TimeControlMode;
                ModLogger.Debug(LogCategory, $"Captured time mode before event popup: {_capturedTimeMode}");
                
                var inquiry = new MultiSelectionInquiryData(
                    titleText: title,
                    descriptionText: body,
                    inquiryElements: options,
                    isExitShown: false, // Hide exit button - player should choose an option
                    minSelectableOptionCount: 1,
                    maxSelectableOptionCount: 1,
                    affirmativeText: new TextObject("{=ll_inquiry_choose}Proceed").ToString(),
                    negativeText: new TextObject("{=ll_inquiry_leave}Back").ToString(),
                    affirmativeAction: selected =>
                    {
                        try
                        {
                            var chosen = selected?.FirstOrDefault()?.Identifier as LanceLifeEventOptionDefinition;
                            if (chosen != null)
                            {
                                // Check if this option has reward choices
                                if (chosen.RewardChoices != null && chosen.RewardChoices.Options.Count > 0)
                                {
                                    // Show outcome narrative first
                                    var outcome = GetOutcomeText(chosen, enlistment);
                                    if (!string.IsNullOrWhiteSpace(outcome))
                                    {
                                        InformationManager.DisplayMessage(new InformationMessage(outcome, Colors.White));
                                    }

                                    // Then show reward choice dialog
                                    LanceLifeRewardChoiceInquiryScreen.Show(evt, chosen, enlistment, resultText =>
                                    {
                                        // Reward choice complete
                                        _isEventShowing = false;
                                        RestoreTimeControlMode();
                                    });
                                }
                                else
                                {
                                    // No reward choices - apply directly
                                    LanceLifeEventEffectsApplier.Apply(evt, chosen, enlistment);
                                    _isEventShowing = false;
                                    RestoreTimeControlMode();
                                }
                            }
                            else
                            {
                                _isEventShowing = false;
                                RestoreTimeControlMode();
                            }
                        }
                        catch (Exception ex)
                        {
                            ModLogger.Error(LogCategory, "Error applying Lance Life Event option", ex);
                            _isEventShowing = false;
                            RestoreTimeControlMode();
                        }
                    },
                    negativeAction: _ => 
                    { 
                        _isEventShowing = false;
                        RestoreTimeControlMode();
                    },
                    soundEventPath: string.Empty);

                // Show inquiry with transparent background (pauseGameActiveState: false allows map to be visible)
                // We manually control time via _capturedTimeMode capture/restore
                MBInformationManager.ShowMultiSelectionInquiry(inquiry, pauseGameActiveState: false);
                
                // Manually pause time if it's not already paused
                if (Campaign.Current.TimeControlMode != CampaignTimeControlMode.Stop)
                {
                    Campaign.Current.TimeControlMode = CampaignTimeControlMode.Stop;
                }
                
                return true;
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, "Error showing Lance Life Event inquiry", ex);
                return false;
            }
        }

        private static bool IsOptionEnabled(LanceLifeEventOptionDefinition option, Hero hero, EnlistmentBehavior enlistment)
        {
            if (option == null || hero == null)
            {
                return false;
            }

            // Gold gating
            if (option.Costs?.Gold > 0 && hero.Gold < option.Costs.Gold)
            {
                return false;
            }

            // Fatigue gating (we require the consume call to succeed; we don't expose exact current fatigue here).
            if (option.Costs?.Fatigue > 0 && enlistment?.IsEnlisted == true)
            {
                // If fatigue is already 0, consume would no-op and return false.
                // We mimic that cheaply by attempting a 0-cost check: avoid calling TryConsumeFatigue here.
                // Phase 2+ will provide better menu gating. For Phase 1, keep minimal.
            }

            return true;
        }

        private static string ResolveEffectiveSetupText(LanceLifeEventDefinition evt, EnlistmentBehavior enlistment)
        {
            if (evt == null)
            {
                return string.Empty;
            }

            // Phase 5a: onboarding variant overrides setup/options.
            var onboarding = LanceLifeOnboardingBehavior.Instance;
            if (string.Equals(evt.Category, "onboarding", StringComparison.OrdinalIgnoreCase) &&
                onboarding != null &&
                !string.IsNullOrWhiteSpace(onboarding.Variant) &&
                evt.Variants != null &&
                evt.Variants.TryGetValue(onboarding.Variant, out var variant) &&
                variant != null)
            {
                var variantSetup = LanceLifeEventText.Resolve(variant.SetupId, variant.SetupFallback, string.Empty, enlistment);
                if (!string.IsNullOrWhiteSpace(variantSetup))
                {
                    return variantSetup;
                }
            }

            return LanceLifeEventText.Resolve(evt.SetupId, evt.SetupFallback, string.Empty, enlistment);
        }

        private static List<LanceLifeEventOptionDefinition> ResolveEffectiveOptions(LanceLifeEventDefinition evt)
        {
            if (evt == null)
            {
                return new List<LanceLifeEventOptionDefinition>();
            }

            var enlistment = EnlistmentBehavior.Instance;
            var baseOptions = evt.Options ?? new List<LanceLifeEventOptionDefinition>();

            // Phase 5a: onboarding variant override
            var onboarding = LanceLifeOnboardingBehavior.Instance;
            if (string.Equals(evt.Category, "onboarding", StringComparison.OrdinalIgnoreCase) &&
                onboarding != null &&
                !string.IsNullOrWhiteSpace(onboarding.Variant) &&
                evt.Variants != null &&
                evt.Variants.TryGetValue(onboarding.Variant, out var variant) &&
                variant?.Options != null &&
                variant.Options.Count > 0)
            {
                baseOptions = variant.Options;
            }

            // Phase 5a: option.condition filtering
            var eval = new LanceLifeEventTriggerEvaluator();
            var result = new List<LanceLifeEventOptionDefinition>();
            foreach (var opt in baseOptions)
            {
                if (opt == null)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(opt.Condition))
                {
                    result.Add(opt);
                    continue;
                }

                if (enlistment == null)
                {
                    continue;
                }

                if (eval.IsConditionTrue(opt.Condition, enlistment))
                {
                    result.Add(opt);
                }
            }

            return result;
        }

        /// <summary>
        /// Format popup text for better readability - prevents overly wide popups.
        /// Inserts line breaks at natural points to create a narrower, taller layout.
        /// </summary>
        private static string FormatPopupText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            // Replace double newlines with paragraph markers
            text = text.Replace("\r\n", "\n");
            
            // Add visual paragraph breaks for better readability
            // Replace single newlines with double for spacing
            text = System.Text.RegularExpressions.Regex.Replace(text, @"\n{2,}", "\n\n");
            
            // If text is very long with no breaks, insert breaks at sentence boundaries
            // This prevents the popup from being extremely wide
            if (!text.Contains("\n") && text.Length > 200)
            {
                // Insert breaks after sentences (. or ? or !) followed by space
                text = System.Text.RegularExpressions.Regex.Replace(
                    text, 
                    @"([.!?])\s+(?=[A-Z])", 
                    "$1\n\n");
            }

            return text.Trim();
        }

        /// <summary>
        /// Build hint text for option tooltip showing costs, rewards, and risks.
        /// Displayed when hovering over option in the inquiry.
        /// </summary>
        private static string BuildOptionHint(LanceLifeEventOptionDefinition opt)
        {
            if (opt == null)
            {
                return string.Empty;
            }

            var lines = new List<string>();

            // Start with the author-provided tooltip if available
            if (!string.IsNullOrWhiteSpace(opt.Tooltip))
            {
                lines.Add(opt.Tooltip);
            }

            // Risk warning (risky options with failure chance)
            if (opt.SuccessChance.HasValue && opt.SuccessChance < 1.0f)
            {
                var failChance = (int)((1.0f - opt.SuccessChance.Value) * 100);
                lines.Add($"Risk: {failChance}% chance of failure");
            }
            else if (opt.RiskChance.HasValue && opt.RiskChance > 0)
            {
                lines.Add($"Risk: {opt.RiskChance}% chance of failure");
            }
            else if (!string.IsNullOrWhiteSpace(opt.Risk) && opt.Risk.ToLowerInvariant() == "risky")
            {
                lines.Add("Risky action");
            }

            var effectParts = new List<string>();

            // Costs
            if (opt.Costs != null)
            {
                if (opt.Costs.Gold > 0)
                    effectParts.Add($"-{opt.Costs.Gold} gold");
                if (opt.Costs.Fatigue > 0)
                    effectParts.Add($"-{opt.Costs.Fatigue} fatigue");
                if (opt.Costs.Heat != 0)
                    effectParts.Add($"+{opt.Costs.Heat} Heat");
            }

            // Rewards
            if (opt.Rewards != null)
            {
                if (opt.Rewards.Gold > 0)
                    effectParts.Add($"+{opt.Rewards.Gold} gold");
                if (opt.Rewards.FatigueRelief > 0)
                    effectParts.Add($"+{opt.Rewards.FatigueRelief} rest");
                if (opt.Rewards.SkillXp != null && opt.Rewards.SkillXp.Count > 0)
                {
                    foreach (var kvp in opt.Rewards.SkillXp)
                    {
                        effectParts.Add($"+{kvp.Value} {kvp.Key} XP");
                    }
                }
            }

            // Effects (lance reputation, discipline, heat, medical risk)
            if (opt.Effects != null)
            {
                if (opt.Effects.LanceReputation != 0)
                {
                    var sign = opt.Effects.LanceReputation > 0 ? "+" : "";
                    effectParts.Add($"{sign}{opt.Effects.LanceReputation} Rep");
                }
                if (opt.Effects.Discipline != 0)
                {
                    var sign = opt.Effects.Discipline > 0 ? "+" : "";
                    effectParts.Add($"{sign}{opt.Effects.Discipline} Discipline");
                }
                if (opt.Effects.Heat != 0)
                {
                    var sign = opt.Effects.Heat > 0 ? "+" : "";
                    effectParts.Add($"{sign}{opt.Effects.Heat} Heat");
                }
                if (opt.Effects.MedicalRisk > 0)
                {
                    var pct = (int)(opt.Effects.MedicalRisk * 100);
                    effectParts.Add($"Injury risk: {pct}%");
                }
            }

            // Injury/Illness risks from dedicated fields
            if (opt.Injury != null && opt.Injury.Chance > 0)
            {
                var pct = (int)(opt.Injury.Chance * 100);
                effectParts.Add($"Injury risk: {pct}%");
            }
            if (opt.InjuryRisk != null && opt.InjuryRisk.Chance > 0)
            {
                var pct = (int)(opt.InjuryRisk.Chance * 100);
                effectParts.Add($"Injury risk: {pct}%");
            }

            // Add effects line if we have any
            if (effectParts.Count > 0)
            {
                lines.Add(string.Join(" | ", effectParts));
            }

            return lines.Count > 0 ? string.Join("\n", lines) : string.Empty;
        }

        /// <summary>
        /// Get the outcome text for an event option (for displaying before reward choices)
        /// </summary>
        private static string GetOutcomeText(LanceLifeEventOptionDefinition option, EnlistmentBehavior enlistment)
        {
            if (option == null)
            {
                return string.Empty;
            }

            // Try schema outcome first
            if (!string.IsNullOrWhiteSpace(option.SchemaOutcome))
            {
                return LanceLifeEventText.Resolve(string.Empty, option.SchemaOutcome, string.Empty, enlistment);
            }

            // Fall back to result text
            if (!string.IsNullOrWhiteSpace(option.OutcomeTextId) || !string.IsNullOrWhiteSpace(option.OutcomeTextFallback))
            {
                return LanceLifeEventText.Resolve(option.OutcomeTextId, option.OutcomeTextFallback, string.Empty, enlistment);
            }

            return string.Empty;
        }
    }
}


