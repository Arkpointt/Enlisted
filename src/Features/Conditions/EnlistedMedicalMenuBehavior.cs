using System;
using System.Text;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Features.Equipment.Behaviors;
using Enlisted.Mod.Core.Logging;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using EnlistedConfig = Enlisted.Mod.Core.Config.ConfigurationManager;

namespace Enlisted.Features.Conditions
{
    /// <summary>
    /// Handles the Medical Attention menu - treatment and recovery options.
    /// This menu only appears when the player has an injury, illness, or exhaustion.
    /// Provides treatment options with different costs and recovery rates.
    /// </summary>
    public sealed class EnlistedMedicalMenuBehavior : CampaignBehaviorBase
    {
        private const string LogCategory = "MedicalMenu";
        private const string MedicalMenuId = "enlisted_medical";

        public static EnlistedMedicalMenuBehavior Instance { get; private set; }

        public EnlistedMedicalMenuBehavior()
        {
            Instance = this;
        }

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        }

        public override void SyncData(IDataStore dataStore)
        {
            // No persistent state - condition data comes from PlayerConditionBehavior
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            try
            {
                AddMedicalMenu(starter);
                ModLogger.Info(LogCategory, "Medical menu registered");
            }
            catch (Exception ex)
            {
                ModLogger.ErrorCode(LogCategory, "E-MEDICAL-001", "Failed to register medical menu", ex);
            }
        }

        /// <summary>
        /// Adds the Medical Attention option to the enlisted status menu.
        /// Only visible when player has a condition.
        /// </summary>
        public void AddMedicalOptionToEnlistedMenu(CampaignGameStarter starter)
        {
            try
            {
                starter.AddGameMenuOption(
                    "enlisted_status",
                    "enlisted_seek_medical",
                    "{=medical_menu_option}Seek Medical Attention",
                    IsMedicalMenuAvailable,
                    OnMedicalMenuSelected,
                    false,
                    5); // After Camp Activities, before My Lord

                ModLogger.Debug(LogCategory, "Added Medical option to enlisted_status menu");
            }
            catch (Exception ex)
            {
                ModLogger.ErrorCode(LogCategory, "E-MEDICAL-002", "Failed to add medical menu option", ex);
            }
        }

        private bool IsMedicalMenuAvailable(MenuCallbackArgs args)
        {
            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment?.IsEnlisted != true)
            {
                return false;
            }

            // Only show if condition system is enabled and player has a condition
            var cond = PlayerConditionBehavior.Instance;
            if (cond?.IsEnabled() != true || cond.State?.HasAnyCondition != true)
            {
                return false;
            }

            args.optionLeaveType = GameMenuOption.LeaveType.Manage;

            // Urgent indicator if condition is worsening
            if (IsConditionWorsening())
            {
                args.Tooltip = new TextObject("{=medical_urgent}[!] Your condition requires attention.");
            }
            else
            {
                args.Tooltip = new TextObject("{=medical_tooltip}Visit the surgeon's tent.");
            }

            return true;
        }

        private void OnMedicalMenuSelected(MenuCallbackArgs args)
        {
            try
            {
                if (Campaign.Current != null && !QuartermasterManager.CapturedTimeMode.HasValue)
                {
                    QuartermasterManager.CapturedTimeMode = Campaign.Current.TimeControlMode;
                }

                GameMenu.SwitchToMenu(MedicalMenuId);
                ModLogger.Debug(LogCategory, "Player entered Medical menu");
            }
            catch (Exception ex)
            {
                ModLogger.ErrorCode(LogCategory, "E-MEDICAL-003", "Failed to switch to medical menu", ex);
            }
        }

        /// <summary>
        /// Creates the Medical Attention menu with treatment options.
        /// </summary>
        private void AddMedicalMenu(CampaignGameStarter starter)
        {
            starter.AddWaitGameMenu(
                MedicalMenuId,
                "{MEDICAL_MENU_TEXT}",
                OnMedicalMenuInit,
                _ => true,
                null,
                OnMedicalMenuTick,
                GameMenu.MenuAndOptionType.WaitMenuHideProgressAndHoursOption);

            // Request treatment from surgeon
            starter.AddGameMenuOption(
                MedicalMenuId,
                "medical_surgeon",
                "{=medical_surgeon}Request Treatment from Surgeon",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Manage;
                    args.Tooltip = new TextObject("{=medical_surgeon_hint}Surgeon will treat your condition. Recovery rate +100%. Costs 2 fatigue.");
                    return HasTreatableCondition();
                },
                _ => ApplyTreatment("surgeon"),
                false, 1);

            // Self-treat (if Field Medic profession)
            starter.AddGameMenuOption(
                MedicalMenuId,
                "medical_self_treat",
                "{=medical_self_treat}Treat Yourself (Field Medic)",
                IsSelfTreatAvailable,
                _ => ApplyTreatment("self"),
                false, 2);

            // Purchase herbal remedy
            starter.AddGameMenuOption(
                MedicalMenuId,
                "medical_herbal",
                "{HERBAL_OPTION_TEXT}",
                IsHerbalAvailable,
                _ => ApplyTreatment("herbal"),
                false, 3);

            // Rest in camp (light duty)
            starter.AddGameMenuOption(
                MedicalMenuId,
                "medical_rest",
                "{=medical_rest}Rest in Camp",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Wait;
                    args.Tooltip = new TextObject("{=medical_rest_hint}Skip activities for today. Recovery rate +50%. No duty events while resting.");
                    return true;
                },
                _ => ApplyRest(),
                false, 4);

            // View detailed status
            starter.AddGameMenuOption(
                MedicalMenuId,
                "medical_status",
                "{=medical_status}View Detailed Status",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
                    return true;
                },
                _ => ShowDetailedConditionPopup(),
                false, 5);

            // Back
            starter.AddGameMenuOption(
                MedicalMenuId,
                "medical_back",
                "{=medical_back}Back",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Leave;
                    return true;
                },
                _ => GameMenu.SwitchToMenu("enlisted_status"),
                true, 99);
        }

        /// <summary>
        /// Menu background initialization.
        /// </summary>
        [GameMenuInitializationHandler(MedicalMenuId)]
        public static void OnMedicalMenuBackgroundInit(MenuCallbackArgs args)
        {
            // Medical attention is a grounded "camp support" activity.
            // Use a more civilian/common backdrop instead of a combat encounter background.
            args.MenuContext.SetBackgroundMeshName("encounter_peasant");
            args.MenuContext.SetAmbientSound("event:/map/ambient/node/settlements/2d/camp_army");
        }

        private void OnMedicalMenuInit(MenuCallbackArgs args)
        {
            try
            {
                RefreshMedicalMenuText();
                SetHerbalOptionText();

                args.MenuContext.GameMenu.StartWait();
                Campaign.Current.SetTimeControlModeLock(false);

                var captured = QuartermasterManager.CapturedTimeMode ?? Campaign.Current.TimeControlMode;
                var normalized = QuartermasterManager.NormalizeToStoppable(captured);
                Campaign.Current.TimeControlMode = normalized;
            }
            catch (Exception ex)
            {
                ModLogger.ErrorCode(LogCategory, "E-MEDICAL-004", "Error initializing medical menu", ex);
            }
        }

        private void OnMedicalMenuTick(MenuCallbackArgs args, CampaignTime dt)
        {
            // No per-tick updates needed
        }

        private void RefreshMedicalMenuText()
        {
            var cond = PlayerConditionBehavior.Instance;

            var sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine(new TextObject("{=medical_header}— MEDICAL ATTENTION —").ToString());
            sb.AppendLine();

            // Condition details
            sb.AppendLine(new TextObject("{=medical_condition_header}— YOUR CONDITION —").ToString());

            if (cond?.State == null || !cond.State.HasAnyCondition)
            {
                sb.AppendLine(new TextObject("{=medical_healthy}Healthy").ToString());
            }
            else
            {
                if (cond.State.HasInjury)
                {
                    var injuryLine = new TextObject("{=medical_injury}Injury: {DAYS} days remaining");
                    injuryLine.SetTextVariable("DAYS", cond.State.InjuryDaysRemaining);
                    sb.AppendLine(injuryLine.ToString());

                    sb.AppendLine(new TextObject("{=medical_injury_effect}  Fatigue Pool: -25%").ToString());
                    sb.AppendLine(new TextObject("{=medical_injury_restrict}  Restrictions: No training").ToString());
                }

                if (cond.State.HasIllness)
                {
                    var illnessLine = new TextObject("{=medical_illness}Illness: {DAYS} days remaining");
                    illnessLine.SetTextVariable("DAYS", cond.State.IllnessDaysRemaining);
                    sb.AppendLine(illnessLine.ToString());

                    if (IsConditionWorsening())
                    {
                        sb.AppendLine(new TextObject("{=medical_illness_warn}  Warning: May worsen if untreated").ToString());
                    }
                }

                if (cond.State.UnderMedicalCare)
                {
                    sb.AppendLine();
                    sb.AppendLine(new TextObject("{=medical_under_care}Currently under medical care.").ToString());
                }
            }

            sb.AppendLine();

            // Recovery estimate
            var estimate = GetRecoveryEstimate();
            var estimateLine = new TextObject("{=medical_recovery}Recovery: {ESTIMATE}");
            estimateLine.SetTextVariable("ESTIMATE", estimate);
            sb.AppendLine(estimateLine.ToString());

            // Treatment status
            var treatmentStatus = cond?.State?.UnderMedicalCare == true
                ? new TextObject("{=medical_treatment_active}Active").ToString()
                : new TextObject("{=medical_treatment_none}None").ToString();
            var treatmentLine = new TextObject("{=medical_treatment_status}Treatment: {STATUS}");
            treatmentLine.SetTextVariable("STATUS", treatmentStatus);
            sb.AppendLine(treatmentLine.ToString());

            MBTextManager.SetTextVariable("MEDICAL_MENU_TEXT", sb.ToString());
        }

        private void SetHerbalOptionText()
        {
            var cost = GetHerbalCost();
            var optionText = new TextObject("{=medical_herbal_option}Purchase Herbal Remedy ({COST}{GOLD_ICON})");
            optionText.SetTextVariable("COST", cost);
            MBTextManager.SetTextVariable("HERBAL_OPTION_TEXT", optionText.ToString());
        }

        private bool IsSelfTreatAvailable(MenuCallbackArgs args)
        {
            var cond = PlayerConditionBehavior.Instance;
            if (cond?.State == null || !cond.State.HasAnyCondition)
            {
                return false;
            }

            // Placeholder: currently no Field Medic profession check implemented
            // return false;

            // Can only self-treat minor/moderate conditions
            var isSevere = cond.State.InjuryDaysRemaining > 7 || cond.State.IllnessDaysRemaining > 7;
            if (isSevere)
            {
                args.IsEnabled = false;
                args.Tooltip = new TextObject("{=medical_self_severe}Condition too severe for self-treatment.");
                return true;
            }

            args.optionLeaveType = GameMenuOption.LeaveType.Manage;
            args.Tooltip = new TextObject("{=medical_self_hint}Use your medical knowledge. Recovery rate +50%. Gain Medicine XP.");
            return false; // Disabled until Field Medic profession is implemented
        }

        private bool IsHerbalAvailable(MenuCallbackArgs args)
        {
            var cost = GetHerbalCost();
            var playerGold = Hero.MainHero?.Gold ?? 0;

            if (playerGold < cost)
            {
                args.IsEnabled = false;
                var tooltip = new TextObject("{=medical_herbal_poor}You need {COST} denars.");
                tooltip.SetTextVariable("COST", cost);
                args.Tooltip = tooltip;
                return true;
            }

            args.optionLeaveType = GameMenuOption.LeaveType.Trade;
            args.Tooltip = new TextObject("{=medical_herbal_hint}Buy medicine from camp followers. Recovery rate +75%.");
            return true;
        }

        private static bool HasTreatableCondition()
        {
            var cond = PlayerConditionBehavior.Instance;
            return cond?.State?.HasAnyCondition == true;
        }

        private static bool IsConditionWorsening()
        {
            var cond = PlayerConditionBehavior.Instance;
            // Illness without treatment may worsen
            return cond?.State?.HasIllness == true && cond.State.UnderMedicalCare != true;
        }

        private static string GetRecoveryEstimate()
        {
            var cond = PlayerConditionBehavior.Instance;
            if (cond?.State == null || !cond.State.HasAnyCondition)
            {
                return new TextObject("{=medical_no_recovery}N/A").ToString();
            }

            var days = Math.Max(cond.State.InjuryDaysRemaining, cond.State.IllnessDaysRemaining);
            if (days <= 0)
            {
                return new TextObject("{=medical_recovered}Recovered").ToString();
            }

            var withTreatment = Math.Max(1, days / 2);
            var estimate = new TextObject("{=medical_estimate}{DAYS} days (or {TREATED} with treatment)");
            estimate.SetTextVariable("DAYS", days);
            estimate.SetTextVariable("TREATED", withTreatment);
            return estimate.ToString();
        }

        private static int GetHerbalCost()
        {
            // Base cost, could scale with tier or condition severity
            return 50;
        }

        private void ApplyTreatment(string treatmentType)
        {
            var cond = PlayerConditionBehavior.Instance;
            var enlistment = EnlistmentBehavior.Instance;
            var cfg = EnlistedConfig.LoadPlayerConditionsConfig() ?? new Mod.Core.Config.PlayerConditionsConfig();

            if (cond?.IsEnabled() != true)
            {
                return;
            }

            switch (treatmentType)
            {
                case "surgeon":
                    enlistment?.TryConsumeFatigue(2, "surgeon_treatment");
                    cond.ApplyTreatment(cfg.ThoroughTreatmentMultiplier, "surgeon_treatment");
                    InformationManager.DisplayMessage(new InformationMessage(
                        new TextObject("{=medical_surgeon_done}The surgeon has tended to your wounds.").ToString(),
                        Colors.Green));
                    break;

                case "self":
                    cond.ApplyTreatment(cfg.BasicTreatmentMultiplier * 1.5f, "self_treatment");
                    // Grant medicine XP
                    Hero.MainHero?.AddSkillXp(DefaultSkills.Medicine, 25);
                    InformationManager.DisplayMessage(new InformationMessage(
                        new TextObject("{=medical_self_done}You treat yourself using your medical knowledge.").ToString(),
                        Colors.Green));
                    break;

                case "herbal":
                    var cost = GetHerbalCost();
                    if (Hero.MainHero.Gold >= cost)
                    {
                        GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, cost);
                        cond.ApplyTreatment(cfg.HerbalTreatmentMultiplier, "herbal_treatment");
                        InformationManager.DisplayMessage(new InformationMessage(
                            new TextObject("{=medical_herbal_done}You purchase and apply herbal remedies.").ToString(),
                            Colors.Green));
                    }
                    else
                    {
                        InformationManager.DisplayMessage(new InformationMessage(
                            new TextObject("{=medical_no_coin}You don't have enough coin.").ToString(),
                            Colors.Red));
                        return;
                    }
                    break;
            }

            // Refresh menu
            GameMenu.SwitchToMenu(MedicalMenuId);
        }

        private void ApplyRest()
        {
            var cond = PlayerConditionBehavior.Instance;
            var cfg = EnlistedConfig.LoadPlayerConditionsConfig() ?? new Mod.Core.Config.PlayerConditionsConfig();

            if (cond?.IsEnabled() == true)
            {
                cond.ApplyTreatment(cfg.BasicTreatmentMultiplier * 0.5f, "camp_rest");
            }

            InformationManager.DisplayMessage(new InformationMessage(
                new TextObject("{=medical_rest_done}You rest for the day, focusing on recovery.").ToString(),
                Colors.White));

            GameMenu.SwitchToMenu("enlisted_status");
        }

        private void ShowDetailedConditionPopup()
        {
            var cond = PlayerConditionBehavior.Instance;

            var sb = new StringBuilder();
            sb.AppendLine(new TextObject("{=medical_detail_header}Detailed Condition Report").ToString());
            sb.AppendLine();

            if (cond?.State == null || !cond.State.HasAnyCondition)
            {
                sb.AppendLine(new TextObject("{=medical_detail_healthy}You are in good health.").ToString());
            }
            else
            {
                if (cond.State.HasInjury)
                {
                    sb.AppendLine(new TextObject("{=medical_detail_injury_header}— INJURY —").ToString());
                    var injuryLine = new TextObject("{=medical_detail_injury}Days Remaining: {DAYS}");
                    injuryLine.SetTextVariable("DAYS", cond.State.InjuryDaysRemaining);
                    sb.AppendLine(injuryLine.ToString());
                    sb.AppendLine(new TextObject("{=medical_detail_injury_effect}Effect: -25% Fatigue Pool, No training allowed").ToString());
                    sb.AppendLine();
                }

                if (cond.State.HasIllness)
                {
                    sb.AppendLine(new TextObject("{=medical_detail_illness_header}— ILLNESS —").ToString());
                    var illnessLine = new TextObject("{=medical_detail_illness}Days Remaining: {DAYS}");
                    illnessLine.SetTextVariable("DAYS", cond.State.IllnessDaysRemaining);
                    sb.AppendLine(illnessLine.ToString());
                    if (!cond.State.UnderMedicalCare)
                    {
                        sb.AppendLine(new TextObject("{=medical_detail_illness_warn}Warning: Untreated illness may worsen").ToString());
                    }
                    sb.AppendLine();
                }

                if (cond.State.UnderMedicalCare)
                {
                    sb.AppendLine(new TextObject("{=medical_detail_care}Status: Under medical care (recovery accelerated)").ToString());
                }
            }

            InformationManager.ShowInquiry(
                new InquiryData(
                    new TextObject("{=medical_detail_title}Condition Report").ToString(),
                    sb.ToString(),
                    true,
                    false,
                    new TextObject("{=medical_detail_close}Close").ToString(),
                    string.Empty,
                    () => { },
                    null));
        }
    }
}

