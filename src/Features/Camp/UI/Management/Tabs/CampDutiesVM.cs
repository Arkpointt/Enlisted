using System;
using System.Linq;
using Enlisted.Features.Assignments.Behaviors;
using Enlisted.Features.Assignments.Core;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Features.Escalation;
using Enlisted.Mod.Core.Logging;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace Enlisted.Features.Camp.UI.Management.Tabs
{
    /// <summary>
    /// Duties tab ViewModel for Camp Management.
    /// Uses policy-style selection pattern (like Kingdom Policies):
    /// - Left panel: two lists (Available / Inactive) with collapsible headers
    /// - Right panel: selected duty details (description, effects, requirements)
    /// Selecting a duty only updates the details panel; the player must press the request button to change duty.
    /// (This mirrors the Orders tab: select, then confirm/request.)
    /// </summary>
    public class CampDutiesVM : ViewModel
    {
        private const string LogCategory = "CampDutiesVM";

        // Duty lists (left panel)
        private MBBindingList<DutyItemVM> _availableDuties;
        private MBBindingList<DutyItemVM> _inactiveDuties;
        private DutyItemVM _selectedDuty;
        private bool _hasSelectedDuty;

        // Duty details (right panel)
        private string _dutyTitle;
        private string _dutyDescription;
        private MBBindingList<DutyEffectItemVM> _dutyEffects;
        private string _dutyRequirements;
        private bool _canRequestDuty;
        private string _requestButtonText;
        private string _requestButtonHint;

        // Current duty info (top)
        private string _currentDutyText;
        private string _currentFormationText;
        private bool _hasCurrentDuty;

        // Lance Rep bar (bottom, like Lord Approval in Schedule)
        private int _lanceRepValue;
        private int _lanceRepNormalized;  // 0-100 for slider display (converts -50..+50 to 0..100)
        private int _lanceRepMin;
        private int _lanceRepMax;
        private string _lanceRepText;
        private string _lanceRepLabel;
        private bool _showLanceRepBar;

        // State
        private bool _show;

        // Text strings
        private string _dutiesHeaderText;
        private string _availableDutiesText;
        private string _inactiveDutiesText;
        private string _numAvailableDutiesText;
        private string _numInactiveDutiesText;
        private string _noDutiesText;
        private string _selectDutyText;
        private bool _hasDuties;

        public CampDutiesVM()
        {
            AvailableDuties = new MBBindingList<DutyItemVM>();
            InactiveDuties = new MBBindingList<DutyItemVM>();
            DutyEffects = new MBBindingList<DutyEffectItemVM>();
            RefreshValues();
        }

        public override void RefreshValues()
        {
            base.RefreshValues();

            DutiesHeaderText = new TextObject("{=enl_camp_duties_header}Duties").ToString();
            AvailableDutiesText = new TextObject("{=enl_camp_duties_available_header}Available Duties").ToString();
            InactiveDutiesText = new TextObject("{=enl_camp_duties_inactive_header}Inactive Duties").ToString();
            NoDutiesText = new TextObject("{=enl_camp_duties_none_for_rank}No duties available for your rank.").ToString();
            SelectDutyText = new TextObject("{=enl_camp_duties_select_prompt}Select a duty to view details and request assignment.").ToString();
            RequestButtonText = new TextObject("{=enl_camp_duties_request_assignment}Request Assignment").ToString();
            LanceRepLabel = new TextObject("{=enl_camp_duties_lance_rep_label}Lance Reputation").ToString();

            RefreshCurrentDuty();
            RefreshDutyList();
            RefreshLanceRep();

            // Auto-select current duty if none selected (show what player is currently doing)
            if (SelectedDuty == null && (AvailableDuties.Count + InactiveDuties.Count) > 0)
            {
                var currentDuty = AvailableDuties.FirstOrDefault(d => d.IsCurrentDuty);
                if (currentDuty != null)
                {
                    UpdateSelectedDutyDisplay(currentDuty);
                }
                else if (AvailableDuties.Count > 0)
                {
                    UpdateSelectedDutyDisplay(AvailableDuties[0]);
                }
                else
                {
                    UpdateSelectedDutyDisplay(InactiveDuties[0]);
                }
            }
        }

        /// <summary>
        /// Refresh current duty display at the top of the panel.
        /// </summary>
        private void RefreshCurrentDuty()
        {
            var dutiesBehavior = EnlistedDutiesBehavior.Instance;
            var enlistment = EnlistmentBehavior.Instance;

            if (dutiesBehavior == null || enlistment == null || !enlistment.IsEnlisted)
            {
                CurrentDutyText = new TextObject("{=enl_camp_duties_not_enlisted}Not enlisted").ToString();
                CurrentFormationText = "";
                HasCurrentDuty = false;
                return;
            }

            // Get active duties (players can have multiple)
            var activeDuties = dutiesBehavior.ActiveDuties;
            if (activeDuties != null && activeDuties.Count > 0)
            {
                var t = new TextObject("{=enl_camp_duties_current_prefix}Current: {DUTIES}");
                t.SetTextVariable("DUTIES", dutiesBehavior.GetActiveDutiesDisplay() ?? string.Empty);
                CurrentDutyText = t.ToString();
                HasCurrentDuty = true;
            }
            else
            {
                CurrentDutyText = new TextObject("{=enl_camp_duties_current_none}Current: None assigned").ToString();
                HasCurrentDuty = false;
            }

            // Get current formation and tier
            var formation = dutiesBehavior.GetPlayerFormationType();
            var tier = enlistment.EnlistmentTier;
            var formationDisplay = !string.IsNullOrWhiteSpace(formation)
                ? FormatFormationName(formation)
                : new TextObject("{=enl_formation_infantry}Infantry").ToString();
            
            // Show transfer availability status
            var transferStatus = "";
            if (tier < 2)
            {
                transferStatus = new TextObject("{=enl_camp_duties_transfer_unlock} (Transfers unlock at T2)").ToString();
            }
            else if (dutiesBehavior.IsDutyRequestOnCooldown())
            {
                var daysRemaining = dutiesBehavior.GetDutyRequestCooldownRemaining();
                var t = new TextObject("{=enl_camp_duties_transfer_cooldown} (Transfer cooldown: {DAYS}d)");
                t.SetTextVariable("DAYS", daysRemaining);
                transferStatus = t.ToString();
            }
            else
            {
                transferStatus = new TextObject("{=enl_camp_duties_transfer_available} (Transfer available)").ToString();
            }
            
            var line = new TextObject("{=enl_camp_duties_formation_line}Formation: {FORMATION} | Tier {TIER}{TRANSFER_STATUS}");
            line.SetTextVariable("FORMATION", formationDisplay ?? string.Empty);
            line.SetTextVariable("TIER", tier);
            line.SetTextVariable("TRANSFER_STATUS", transferStatus ?? string.Empty);
            CurrentFormationText = line.ToString();
        }

        /// <summary>
        /// Refresh the list of available duties.
        /// </summary>
        private void RefreshDutyList()
        {
            AvailableDuties.Clear();
            InactiveDuties.Clear();

            var dutiesBehavior = EnlistedDutiesBehavior.Instance;
            var enlistment = EnlistmentBehavior.Instance;

            if (dutiesBehavior == null || enlistment == null || !enlistment.IsEnlisted)
            {
                return;
            }

            try
            {
                // Get all duties (we'll show unavailable ones greyed out)
                var allDuties = dutiesBehavior.GetAllDuties();
                if (allDuties == null || !allDuties.Any())
                {
                    return;
                }

                var currentTier = enlistment.EnlistmentTier;
                var activeDuties = dutiesBehavior.ActiveDuties ?? new System.Collections.Generic.List<string>();

                // Sort duties: current first, then eligible, then inactive
                var sortedDuties = allDuties
                    .OrderByDescending(d => activeDuties.Contains(d.Id))
                    .ThenByDescending(d => dutiesBehavior.IsDutySelectableByPlayer(d) && dutiesBehavior.IsDutyCompatibleWithFormation(d))
                    .ThenBy(d => d.MinTier)
                    .ThenBy(d => d.DisplayName)
                    .ToList();

                foreach (var duty in sortedDuties)
                {
                    bool isCurrentDuty = activeDuties.Contains(duty.Id);
                    bool meetsTierAndOtherRules = dutiesBehavior.IsDutySelectableByPlayer(duty);
                    bool isCompatible = dutiesBehavior.IsDutyCompatibleWithFormation(duty);
                    bool isSelectable = meetsTierAndOtherRules && isCompatible;

                    // Build unavailability reason
                    string unavailableReason = null;
                    if (!isSelectable)
                    {
                        if (duty.MinTier > currentTier)
                        {
                            var t = new TextObject("{=enl_duty_requires_tier}Requires Tier {TIER}");
                            t.SetTextVariable("TIER", duty.MinTier);
                            unavailableReason = t.ToString();
                        }
                        else if (!isCompatible)
                        {
                            unavailableReason = new TextObject("{=enl_duty_incompatible_formation}Incompatible with your formation").ToString();
                        }
                        else
                        {
                            unavailableReason = new TextObject("{=enl_duty_not_available}Not available").ToString();
                        }
                    }

                    var item = new DutyItemVM(
                        duty,
                        isCurrentDuty,
                        isSelectable,
                        unavailableReason,
                        OnDutySelect
                    );

                    if (isSelectable || isCurrentDuty)
                    {
                        AvailableDuties.Add(item);
                    }
                    else
                    {
                        InactiveDuties.Add(item);
                    }
                }

                NumAvailableDutiesText = $"({AvailableDuties.Count})";
                NumInactiveDutiesText = $"({InactiveDuties.Count})";
                HasDuties = (AvailableDuties.Count + InactiveDuties.Count) > 0;
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, "Failed to load duties list", ex);
            }
        }

        /// <summary>
        /// Refresh the Lance Reputation bar from the escalation system.
        /// Shows the player's current standing with their lance comrades.
        /// </summary>
        private void RefreshLanceRep()
        {
            var escalation = EscalationManager.Instance;
            
            // Hide bar if escalation not available or disabled (normal cases, no logging needed)
            if (escalation == null || !escalation.IsEnabled())
            {
                HideLanceRepBar();
                return;
            }

            // State should never be null if Instance exists, but catch it if it happens
            if (escalation.State == null)
            {
                ModLogger.Error(LogCategory, "EscalationManager.State is unexpectedly null");
                HideLanceRepBar();
                return;
            }

            // Lance Rep ranges from -50 to +50
            LanceRepMin = EscalationState.LanceReputationMin;  // -50
            LanceRepMax = EscalationState.LanceReputationMax;  // +50
            LanceRepValue = escalation.State.LanceReputation;

            // Normalize to 0-100 for slider display (-50 → 0, 0 → 50, +50 → 100)
            LanceRepNormalized = LanceRepValue + 50;

            // Show the current value with a descriptive label
            var repLevel = GetLanceRepLevel(LanceRepValue);
            LanceRepText = $"{LanceRepValue} ({repLevel})";
            ShowLanceRepBar = true;
        }

        /// <summary>
        /// Hide the Lance Rep bar and reset to default values.
        /// </summary>
        private void HideLanceRepBar()
        {
            ShowLanceRepBar = false;
            LanceRepValue = 0;
            LanceRepNormalized = 50;  // Center position
            LanceRepText = "";
        }

        /// <summary>
        /// Get a descriptive level for Lance Rep value.
        /// </summary>
        private string GetLanceRepLevel(int rep)
        {
            return rep switch
            {
                >= 40 => "Legendary",
                >= 30 => "Respected",
                >= 20 => "Trusted",
                >= 10 => "Reliable",
                >= 0 => "Neutral",
                >= -10 => "Questionable",
                >= -20 => "Distrusted",
                >= -30 => "Pariah",
                _ => "Despised"
            };
        }

        /// <summary>
        /// Handle duty selection from the list.
        /// Policy-style: clicking a duty shows its details in the right panel.
        /// Player must click the "Request Assignment" button to actually change duties.
        /// This matches the native Kingdom/Policies pattern where you select then confirm.
        /// </summary>
        private void OnDutySelect(DutyItemVM dutyItem)
        {
            if (dutyItem == null)
                return;

            ModLogger.Debug(LogCategory, $"OnDutySelect called for {dutyItem.Name}");

            // Update the details panel to show the clicked duty's info
            // Does NOT assign the duty - player must click the button
            UpdateSelectedDutyDisplay(dutyItem);
        }

        /// <summary>
        /// Update the details panel to show the selected duty.
        /// Does NOT change assignment - just updates the display.
        /// </summary>
        private void UpdateSelectedDutyDisplay(DutyItemVM dutyItem)
        {
            if (dutyItem == null)
                return;

            // Deselect previous visual selection
            if (SelectedDuty != null)
            {
                SelectedDuty.IsSelected = false;
            }

            // Select new for display
            SelectedDuty = dutyItem;
            SelectedDuty.IsSelected = true;
            HasSelectedDuty = true;

            // Populate details panel
            PopulateDutyDetails(dutyItem);
        }

        /// <summary>
        /// Populate the duty details panel.
        /// </summary>
        private void PopulateDutyDetails(DutyItemVM dutyItem)
        {
            if (dutyItem?.Duty == null)
            {
                ModLogger.Error(LogCategory, "PopulateDutyDetails called with null duty - UI state may be corrupted");
                return;
            }

            var duty = dutyItem.Duty;
            var dutiesBehavior = EnlistedDutiesBehavior.Instance;

            // Title and description
            DutyTitle = duty.DisplayName ?? duty.Id;
            DutyDescription = duty.Description ?? new TextObject("{=enl_duty_no_description}No description available.").ToString();

            // Build effects list
            DutyEffects.Clear();

            // Skill XP gains
            if (!string.IsNullOrWhiteSpace(duty.TargetSkill) && duty.SkillXpDaily > 0)
            {
                DutyEffects.Add(new DutyEffectItemVM($"+{duty.SkillXpDaily} {duty.TargetSkill} XP/day", true));
            }

            // Multi-skill XP
            if (duty.MultiSkillXp != null)
            {
                foreach (var xp in duty.MultiSkillXp.Where(x => x.Value > 0))
                {
                    DutyEffects.Add(new DutyEffectItemVM($"+{xp.Value} {xp.Key} XP/day", true));
                }
            }

            // Wage multiplier
            if (Math.Abs(duty.WageMultiplier - 1.0f) > 0.01f)
            {
                var mult = duty.WageMultiplier;
                if (mult > 1.0f)
                {
                    DutyEffects.Add(new DutyEffectItemVM($"+{(int)((mult - 1) * 100)}% Wage", true));
                }
                else
                {
                    DutyEffects.Add(new DutyEffectItemVM($"-{(int)((1 - mult) * 100)}% Wage", false));
                }
            }

            // Passive effects
            if (duty.PassiveEffects != null)
            {
                foreach (var effect in duty.PassiveEffects)
                {
                    var sign = effect.Value >= 0 ? "+" : "";
                    DutyEffects.Add(new DutyEffectItemVM($"{sign}{effect.Value} {effect.Key}", effect.Value >= 0));
                }
            }

            // Special abilities
            if (duty.SpecialAbilities != null && duty.SpecialAbilities.Any())
            {
                foreach (var ability in duty.SpecialAbilities)
                {
                    DutyEffects.Add(new DutyEffectItemVM($"• {FormatAbilityName(ability)}", true));
                }
            }

            // Build requirements text
            var requirements = new System.Text.StringBuilder();

            if (duty.MinTier > 1)
            {
                requirements.Append($"Tier {duty.MinTier}+ ");
            }

            if (duty.RequiredFormations != null && duty.RequiredFormations.Count > 0)
            {
                var formationNames = duty.RequiredFormations.Select(FormatFormationName);
                requirements.Append($"Formations: {string.Join(", ", formationNames)} ");
            }

            if (duty.UnlockConditions != null)
            {
                if (duty.UnlockConditions.RelationshipRequired > 0)
                {
                    requirements.Append($"Relation {duty.UnlockConditions.RelationshipRequired}+ ");
                }
                if (duty.UnlockConditions.SkillRequired > 0)
                {
                    requirements.Append($"Skill {duty.UnlockConditions.SkillRequired}+ ");
                }
            }

            // Include the label in the bound string so the UI can render it as a single line
            // (Gauntlet XML bindings are simplest when the Text attribute is purely a binding).
            if (requirements.Length > 0)
            {
                var t = new TextObject("{=enl_duty_requirements_line}Requirements: {REQS}");
                t.SetTextVariable("REQS", requirements.ToString().Trim());
                DutyRequirements = t.ToString();
            }
            else
            {
                DutyRequirements = new TextObject("{=enl_duty_requirements_none}Requirements: No special requirements").ToString();
            }

            // Update status display (orders-screen style: persistent assignments with request system)
            var enlistment = EnlistmentBehavior.Instance;
            var currentTier = enlistment?.EnlistmentTier ?? 1;
            
            if (dutyItem.IsCurrentDuty)
            {
                CanRequestDuty = false;
                RequestButtonHint = new TextObject("{=enl_duty_current_hint}This is your current duty assignment. It persists until you request a change.").ToString();
                RequestButtonText = new TextObject("{=enl_duty_current_label}Current Duty").ToString();
            }
            else if (!dutyItem.IsSelectable)
            {
                CanRequestDuty = false;
                RequestButtonHint = dutyItem.UnavailableReason ?? new TextObject("{=enl_duty_not_available_dot}Not available.").ToString();
                RequestButtonText = new TextObject("{=enl_duty_locked_label}Locked").ToString();
            }
            else if (currentTier >= 2 && dutiesBehavior?.IsDutyRequestOnCooldown() == true)
            {
                // Show cooldown status
                var daysRemaining = dutiesBehavior.GetDutyRequestCooldownRemaining();
                CanRequestDuty = false;
                var hint = new TextObject("{=enl_duty_cooldown_hint}Duty change requests are on cooldown. {DAYS} days remaining.");
                hint.SetTextVariable("DAYS", daysRemaining);
                RequestButtonHint = hint.ToString();
                RequestButtonText = new TextObject("{=enl_duty_on_cooldown_label}On Cooldown").ToString();
            }
            else
            {
                // Eligible duty - clicking will request assignment
                CanRequestDuty = true;
                if (currentTier >= 2)
                {
                    RequestButtonHint = new TextObject("{=enl_duty_request_hint}Request assignment to this duty. Requires lance leader approval.").ToString();
                    RequestButtonText = new TextObject("{=enl_camp_duties_request_assignment}Request Assignment").ToString();
                }
                else
                {
                    RequestButtonHint = new TextObject("{=enl_duty_assign_hint}Assign this duty (T1 soldiers can change duties freely).").ToString();
                    RequestButtonText = new TextObject("{=enl_duty_assign_label}Assign Duty").ToString();
                }
            }
        }

        /// <summary>
        /// Execute: Request assignment to the selected duty.
        /// </summary>
        public void ExecuteRequestDuty()
        {
            if (SelectedDuty == null || !CanRequestDuty)
                return;

            var dutiesBehavior = EnlistedDutiesBehavior.Instance;
            if (dutiesBehavior == null)
            {
                ModLogger.Error(LogCategory, "Cannot request duty: EnlistedDutiesBehavior.Instance is null");
                return;
            }

            try
            {
                var result = dutiesBehavior.RequestDutyChange(SelectedDuty.Duty.Id);

                if (result.Approved)
                {
                    // Show success message to player
                    var t = new TextObject("{=enl_duty_assigned_to}Assigned to: {DUTY_TITLE}");
                    t.SetTextVariable("DUTY_TITLE", DutyTitle ?? string.Empty);
                    InformationManager.DisplayMessage(new InformationMessage(
                        t.ToString(),
                        Color.FromUint(0xFF88FF88)));

                    // Refresh the entire panel
                    RefreshValues();
                }
                else
                {
                    // Show denial message to player
                    var reasonText = GetDenialReasonText(result.Reason);
                    InformationManager.DisplayMessage(new InformationMessage(
                        reasonText,
                        Color.FromUint(0xFFFF8888)
                    ));
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, $"Failed to request duty change to '{SelectedDuty.Duty.Id}'", ex);
                InformationManager.DisplayMessage(new InformationMessage(
                    new TextObject("{=enl_duty_change_failed}Duty change failed unexpectedly. Check mod log for details.").ToString(),
                    Color.FromUint(0xFFFF8888)
                ));
            }
        }

        /// <summary>
        /// Convert denial reason code to human-readable text.
        /// </summary>
        private string GetDenialReasonText(string reasonCode)
        {
            return reasonCode switch
            {
                "duty_not_found" => new TextObject("{=enl_duty_not_found}Duty not found.").ToString(),
                "duty_locked_tier" => new TextObject("{=enl_duty_locked_tier}Your rank is too low for this duty.").ToString(),
                "duty_wrong_formation" => new TextObject("{=enl_duty_wrong_formation}This duty requires a different formation.").ToString(),
                "duty_on_cooldown" => new TextObject("{=enl_duty_on_cooldown}You recently changed duties. Wait before requesting again.").ToString(),
                "duty_same" => new TextObject("{=enl_duty_same}You already have this duty.").ToString(),
                "duty_not_selectable" => new TextObject("{=enl_duty_not_selectable}This duty is not available to you.").ToString(),
                _ => !string.IsNullOrWhiteSpace(reasonCode)
                    ? reasonCode
                    : new TextObject("{=enl_duty_request_denied}Request denied.").ToString()
            };
        }

        /// <summary>
        /// Format a formation type name for display.
        /// </summary>
        private string FormatFormationName(string formation)
        {
            if (string.IsNullOrWhiteSpace(formation))
                return formation;

            // Capitalize first letter
            return char.ToUpper(formation[0]) + formation.Substring(1).ToLower();
        }

        /// <summary>
        /// Format an ability name for display.
        /// </summary>
        private string FormatAbilityName(string ability)
        {
            if (string.IsNullOrWhiteSpace(ability))
                return ability;

            // Replace underscores with spaces and title case
            return string.Join(" ", ability.Split('_')
                .Select(w => char.ToUpper(w[0]) + w.Substring(1).ToLower()));
        }

        // ===== Properties =====

        [DataSourceProperty]
        public bool Show
        {
            get => _show;
            set
            {
                if (value == _show) return;
                _show = value;
                OnPropertyChangedWithValue(value, nameof(Show));

                if (value)
                {
                    RefreshValues();
                }
            }
        }

        [DataSourceProperty]
        public string DutiesHeaderText
        {
            get => _dutiesHeaderText;
            set
            {
                if (value == _dutiesHeaderText) return;
                _dutiesHeaderText = value;
                OnPropertyChangedWithValue(value, nameof(DutiesHeaderText));
            }
        }

        [DataSourceProperty]
        public string NoDutiesText
        {
            get => _noDutiesText;
            set
            {
                if (value == _noDutiesText) return;
                _noDutiesText = value;
                OnPropertyChangedWithValue(value, nameof(NoDutiesText));
            }
        }

        [DataSourceProperty]
        public bool HasDuties
        {
            get => _hasDuties;
            set
            {
                if (value == _hasDuties) return;
                _hasDuties = value;
                OnPropertyChangedWithValue(value, nameof(HasDuties));
            }
        }

        [DataSourceProperty]
        public string SelectDutyText
        {
            get => _selectDutyText;
            set
            {
                if (value == _selectDutyText) return;
                _selectDutyText = value;
                OnPropertyChangedWithValue(value, nameof(SelectDutyText));
            }
        }

        [DataSourceProperty]
        public string CurrentDutyText
        {
            get => _currentDutyText;
            set
            {
                if (value == _currentDutyText) return;
                _currentDutyText = value;
                OnPropertyChangedWithValue(value, nameof(CurrentDutyText));
            }
        }

        [DataSourceProperty]
        public string CurrentFormationText
        {
            get => _currentFormationText;
            set
            {
                if (value == _currentFormationText) return;
                _currentFormationText = value;
                OnPropertyChangedWithValue(value, nameof(CurrentFormationText));
            }
        }

        [DataSourceProperty]
        public bool HasCurrentDuty
        {
            get => _hasCurrentDuty;
            set
            {
                if (value == _hasCurrentDuty) return;
                _hasCurrentDuty = value;
                OnPropertyChangedWithValue(value, nameof(HasCurrentDuty));
            }
        }

        [DataSourceProperty]
        public MBBindingList<DutyItemVM> AvailableDuties
        {
            get => _availableDuties;
            set
            {
                if (value == _availableDuties) return;
                _availableDuties = value;
                OnPropertyChangedWithValue(value, nameof(AvailableDuties));
            }
        }

        [DataSourceProperty]
        public MBBindingList<DutyItemVM> InactiveDuties
        {
            get => _inactiveDuties;
            set
            {
                if (value == _inactiveDuties) return;
                _inactiveDuties = value;
                OnPropertyChangedWithValue(value, nameof(InactiveDuties));
            }
        }

        [DataSourceProperty]
        public string AvailableDutiesText
        {
            get => _availableDutiesText;
            set
            {
                if (value == _availableDutiesText) return;
                _availableDutiesText = value;
                OnPropertyChangedWithValue(value, nameof(AvailableDutiesText));
            }
        }

        [DataSourceProperty]
        public string InactiveDutiesText
        {
            get => _inactiveDutiesText;
            set
            {
                if (value == _inactiveDutiesText) return;
                _inactiveDutiesText = value;
                OnPropertyChangedWithValue(value, nameof(InactiveDutiesText));
            }
        }

        [DataSourceProperty]
        public string NumAvailableDutiesText
        {
            get => _numAvailableDutiesText;
            set
            {
                if (value == _numAvailableDutiesText) return;
                _numAvailableDutiesText = value;
                OnPropertyChangedWithValue(value, nameof(NumAvailableDutiesText));
            }
        }

        [DataSourceProperty]
        public string NumInactiveDutiesText
        {
            get => _numInactiveDutiesText;
            set
            {
                if (value == _numInactiveDutiesText) return;
                _numInactiveDutiesText = value;
                OnPropertyChangedWithValue(value, nameof(NumInactiveDutiesText));
            }
        }

        [DataSourceProperty]
        public DutyItemVM SelectedDuty
        {
            get => _selectedDuty;
            set
            {
                if (value == _selectedDuty) return;
                _selectedDuty = value;
                OnPropertyChangedWithValue(value, nameof(SelectedDuty));
            }
        }

        [DataSourceProperty]
        public bool HasSelectedDuty
        {
            get => _hasSelectedDuty;
            set
            {
                if (value == _hasSelectedDuty) return;
                _hasSelectedDuty = value;
                OnPropertyChangedWithValue(value, nameof(HasSelectedDuty));
            }
        }

        [DataSourceProperty]
        public string DutyTitle
        {
            get => _dutyTitle;
            set
            {
                if (value == _dutyTitle) return;
                _dutyTitle = value;
                OnPropertyChangedWithValue(value, nameof(DutyTitle));
            }
        }

        [DataSourceProperty]
        public string DutyDescription
        {
            get => _dutyDescription;
            set
            {
                if (value == _dutyDescription) return;
                _dutyDescription = value;
                OnPropertyChangedWithValue(value, nameof(DutyDescription));
            }
        }

        [DataSourceProperty]
        public MBBindingList<DutyEffectItemVM> DutyEffects
        {
            get => _dutyEffects;
            set
            {
                if (value == _dutyEffects) return;
                _dutyEffects = value;
                OnPropertyChangedWithValue(value, nameof(DutyEffects));
            }
        }

        [DataSourceProperty]
        public string DutyRequirements
        {
            get => _dutyRequirements;
            set
            {
                if (value == _dutyRequirements) return;
                _dutyRequirements = value;
                OnPropertyChangedWithValue(value, nameof(DutyRequirements));
            }
        }

        [DataSourceProperty]
        public bool CanRequestDuty
        {
            get => _canRequestDuty;
            set
            {
                if (value == _canRequestDuty) return;
                _canRequestDuty = value;
                OnPropertyChangedWithValue(value, nameof(CanRequestDuty));
            }
        }

        [DataSourceProperty]
        public string RequestButtonText
        {
            get => _requestButtonText;
            set
            {
                if (value == _requestButtonText) return;
                _requestButtonText = value;
                OnPropertyChangedWithValue(value, nameof(RequestButtonText));
            }
        }

        [DataSourceProperty]
        public string RequestButtonHint
        {
            get => _requestButtonHint;
            set
            {
                if (value == _requestButtonHint) return;
                _requestButtonHint = value;
                OnPropertyChangedWithValue(value, nameof(RequestButtonHint));
            }
        }

        // ===== Lance Rep Bar Properties =====

        [DataSourceProperty]
        public int LanceRepValue
        {
            get => _lanceRepValue;
            set
            {
                if (value == _lanceRepValue) return;
                _lanceRepValue = value;
                OnPropertyChangedWithValue(value, nameof(LanceRepValue));
            }
        }

        /// <summary>
        /// Lance Rep normalized to 0-100 for slider display.
        /// -50 becomes 0, 0 becomes 50, +50 becomes 100.
        /// </summary>
        [DataSourceProperty]
        public int LanceRepNormalized
        {
            get => _lanceRepNormalized;
            set
            {
                if (value == _lanceRepNormalized) return;
                _lanceRepNormalized = value;
                OnPropertyChangedWithValue(value, nameof(LanceRepNormalized));
            }
        }

        [DataSourceProperty]
        public int LanceRepMin
        {
            get => _lanceRepMin;
            set
            {
                if (value == _lanceRepMin) return;
                _lanceRepMin = value;
                OnPropertyChangedWithValue(value, nameof(LanceRepMin));
            }
        }

        [DataSourceProperty]
        public int LanceRepMax
        {
            get => _lanceRepMax;
            set
            {
                if (value == _lanceRepMax) return;
                _lanceRepMax = value;
                OnPropertyChangedWithValue(value, nameof(LanceRepMax));
            }
        }

        [DataSourceProperty]
        public string LanceRepText
        {
            get => _lanceRepText;
            set
            {
                if (value == _lanceRepText) return;
                _lanceRepText = value;
                OnPropertyChangedWithValue(value, nameof(LanceRepText));
            }
        }

        [DataSourceProperty]
        public string LanceRepLabel
        {
            get => _lanceRepLabel;
            set
            {
                if (value == _lanceRepLabel) return;
                _lanceRepLabel = value;
                OnPropertyChangedWithValue(value, nameof(LanceRepLabel));
            }
        }

        [DataSourceProperty]
        public bool ShowLanceRepBar
        {
            get => _showLanceRepBar;
            set
            {
                if (value == _showLanceRepBar) return;
                _showLanceRepBar = value;
                OnPropertyChangedWithValue(value, nameof(ShowLanceRepBar));
            }
        }
    }

    /// <summary>
    /// ViewModel for a duty item in the duties list.
    /// </summary>
    public class DutyItemVM : ViewModel
    {
        private readonly Action<DutyItemVM> _onSelect;
        private bool _isSelected;

        public DutyDefinition Duty { get; }
        public bool IsCurrentDuty { get; }
        public bool IsSelectable { get; }
        public string UnavailableReason { get; }

        public string DisplayText => Duty?.DisplayName ?? Duty?.Id ?? new TextObject("{=enl_ui_unknown}Unknown").ToString();
        public string StatusText => IsCurrentDuty ? "(Current)" : IsSelectable ? "" : "(Locked)";
        public bool ShowStatus => IsCurrentDuty || !IsSelectable;

        /// <summary>
        /// Native tuple convention: list items expose a <c>Name</c> string and an <c>OnSelect</c> click handler.
        /// This lets the Duties panel reuse the same Kingdom/Policies tuple widgets as the Orders tab.
        /// </summary>
        [DataSourceProperty]
        public string Name
        {
            get
            {
                // Display status inline to keep the list readable without needing a custom tuple UI.
                // Example: "Runner (Current)" / "Engineer (Locked)"
                return string.IsNullOrWhiteSpace(StatusText)
                    ? DisplayText
                    : $"{DisplayText} {StatusText}";
            }
        }

        public DutyItemVM(DutyDefinition duty, bool isCurrentDuty, bool isSelectable, string unavailableReason, Action<DutyItemVM> onSelect)
        {
            Duty = duty;
            IsCurrentDuty = isCurrentDuty;
            IsSelectable = isSelectable;
            UnavailableReason = unavailableReason;
            _onSelect = onSelect;
        }

        public void ExecuteSelect()
        {
            _onSelect?.Invoke(this);
        }

        public void OnSelect()
        {
            ExecuteSelect();
        }

        [DataSourceProperty]
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (value == _isSelected) return;
                _isSelected = value;
                OnPropertyChangedWithValue(value, nameof(IsSelected));
            }
        }
    }

    /// <summary>
    /// ViewModel for an effect line in the duty details panel.
    /// </summary>
    public class DutyEffectItemVM : ViewModel
    {
        public string EffectText { get; }
        public bool IsPositive { get; }

        public DutyEffectItemVM(string effectText, bool isPositive)
        {
            EffectText = effectText;
            IsPositive = isPositive;
        }

        /// <summary>
        /// Alias for <see cref="EffectText"/> that matches the schedule UI's effect list template (<c>@Text</c>).
        /// </summary>
        [DataSourceProperty]
        public string Text => EffectText;
    }
}

