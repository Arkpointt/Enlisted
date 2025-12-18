using System;
using System.Linq;
using Enlisted.Features.Assignments.Behaviors;
using Enlisted.Features.Assignments.Core;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Mod.Core.Logging;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace Enlisted.Features.Camp.UI.Management.Tabs
{
    /// <summary>
    /// Duties tab ViewModel for Camp Management.
    /// Replaces the Activities tab - Duties are low-frequency configuration items,
    /// not high-frequency actions (those belong in Camp Activities via the Game Menu).
    /// </summary>
    public class CampDutiesVM : ViewModel
    {
        private const string LogCategory = "CampDutiesVM";

        // Duty list (left panel)
        private MBBindingList<DutyItemVM> _duties;
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

        // State
        private bool _show;

        // Text strings
        private string _dutiesHeaderText;
        private string _noDutiesText;
        private string _selectDutyText;

        public CampDutiesVM()
        {
            Duties = new MBBindingList<DutyItemVM>();
            DutyEffects = new MBBindingList<DutyEffectItemVM>();
            RefreshValues();
        }

        public override void RefreshValues()
        {
            base.RefreshValues();

            DutiesHeaderText = "Available Duties";
            NoDutiesText = "No duties available for your rank.";
            SelectDutyText = "Select a duty to view details.";
            RequestButtonText = "Request Assignment";

            RefreshCurrentDuty();
            RefreshDutyList();

            // Auto-select first duty if none selected
            if (SelectedDuty == null && Duties.Count > 0)
            {
                SelectDuty(Duties[0]);
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
                CurrentDutyText = "Not enlisted";
                CurrentFormationText = "";
                HasCurrentDuty = false;
                return;
            }

            // Get active duties (players can have multiple)
            var activeDuties = dutiesBehavior.ActiveDuties;
            if (activeDuties != null && activeDuties.Count > 0)
            {
                CurrentDutyText = dutiesBehavior.GetActiveDutiesDisplay();
                HasCurrentDuty = true;
            }
            else
            {
                CurrentDutyText = "None assigned";
                HasCurrentDuty = false;
            }

            // Get current formation
            var formation = dutiesBehavior.GetPlayerFormationType();
            CurrentFormationText = !string.IsNullOrWhiteSpace(formation)
                ? $"Formation: {FormatFormationName(formation)}"
                : "";
        }

        /// <summary>
        /// Refresh the list of available duties.
        /// </summary>
        private void RefreshDutyList()
        {
            Duties.Clear();

            var dutiesBehavior = EnlistedDutiesBehavior.Instance;
            var enlistment = EnlistmentBehavior.Instance;

            if (dutiesBehavior == null || enlistment == null || !enlistment.IsEnlisted)
            {
                ModLogger.Debug(LogCategory, "Duties behavior not available or player not enlisted");
                return;
            }

            // Get all duties (we'll show unavailable ones greyed out)
            var allDuties = dutiesBehavior.GetAllDuties();
            if (allDuties == null || !allDuties.Any())
            {
                ModLogger.Debug(LogCategory, "No duties found in config");
                return;
            }

            var currentTier = enlistment.EnlistmentTier;
            var activeDuties = dutiesBehavior.ActiveDuties ?? new System.Collections.Generic.List<string>();

            // Sort duties: current first, then available, then locked
            var sortedDuties = allDuties
                .OrderByDescending(d => activeDuties.Contains(d.Id))
                .ThenByDescending(d => dutiesBehavior.IsDutySelectableByPlayer(d))
                .ThenBy(d => d.MinTier)
                .ThenBy(d => d.DisplayName)
                .ToList();

            foreach (var duty in sortedDuties)
            {
                bool isCurrentDuty = activeDuties.Contains(duty.Id);
                bool isSelectable = dutiesBehavior.IsDutySelectableByPlayer(duty);
                bool isCompatible = dutiesBehavior.IsDutyCompatibleWithFormation(duty);

                // Build unavailability reason
                string unavailableReason = null;
                if (!isSelectable)
                {
                    if (duty.MinTier > currentTier)
                    {
                        unavailableReason = $"Requires Tier {duty.MinTier}";
                    }
                    else if (!isCompatible)
                    {
                        unavailableReason = "Incompatible with your formation";
                    }
                    else
                    {
                        unavailableReason = "Not available";
                    }
                }

                var item = new DutyItemVM(
                    duty,
                    isCurrentDuty,
                    isSelectable,
                    unavailableReason,
                    OnDutySelect
                );

                Duties.Add(item);
            }

            ModLogger.Debug(LogCategory, $"Loaded {Duties.Count} duties");
        }

        /// <summary>
        /// Handle duty selection from the list.
        /// </summary>
        private void OnDutySelect(DutyItemVM dutyItem)
        {
            SelectDuty(dutyItem);
        }

        /// <summary>
        /// Select a duty and show its details.
        /// </summary>
        private void SelectDuty(DutyItemVM dutyItem)
        {
            if (dutyItem == null)
                return;

            // Deselect previous
            if (SelectedDuty != null)
            {
                SelectedDuty.IsSelected = false;
            }

            // Select new
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
            var duty = dutyItem.Duty;
            var dutiesBehavior = EnlistedDutiesBehavior.Instance;

            // Title and description
            DutyTitle = duty.DisplayName ?? duty.Id;
            DutyDescription = duty.Description ?? "No description available.";

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

            DutyRequirements = requirements.Length > 0 ? requirements.ToString().Trim() : "No special requirements";

            // Check if duty can be requested
            if (dutyItem.IsCurrentDuty)
            {
                CanRequestDuty = false;
                RequestButtonHint = "This is your current duty.";
                RequestButtonText = "Current Duty";
            }
            else if (!dutyItem.IsSelectable)
            {
                CanRequestDuty = false;
                RequestButtonHint = dutyItem.UnavailableReason ?? "Not available.";
                RequestButtonText = "Request Assignment";
            }
            else
            {
                CanRequestDuty = true;
                RequestButtonHint = "Request to be assigned to this duty.";
                RequestButtonText = "Request Assignment";
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
                return;

            var result = dutiesBehavior.RequestDutyChange(SelectedDuty.Duty.Id);

            if (result.Approved)
            {
                ModLogger.Info(LogCategory, $"Duty change approved: {SelectedDuty.Duty.Id}");

                // Show success message
                InformationManager.DisplayMessage(new InformationMessage(
                    $"Assigned to: {DutyTitle}",
                    Color.FromUint(0xFF88FF88)
                ));

                // Refresh the entire panel
                RefreshValues();
            }
            else
            {
                ModLogger.Info(LogCategory, $"Duty change denied: {result.Reason}");

                // Show denial message
                var reasonText = GetDenialReasonText(result.Reason);
                InformationManager.DisplayMessage(new InformationMessage(
                    reasonText,
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
                "duty_not_found" => "Duty not found.",
                "duty_locked_tier" => "Your rank is too low for this duty.",
                "duty_wrong_formation" => "This duty requires a different formation.",
                "duty_on_cooldown" => "You recently changed duties. Wait before requesting again.",
                "duty_same" => "You already have this duty.",
                "duty_not_selectable" => "This duty is not available to you.",
                _ => reasonCode ?? "Request denied."
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
        public MBBindingList<DutyItemVM> Duties
        {
            get => _duties;
            set
            {
                if (value == _duties) return;
                _duties = value;
                OnPropertyChangedWithValue(value, nameof(Duties));
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

        public string DisplayText => Duty?.DisplayName ?? Duty?.Id ?? "Unknown";
        public string StatusText => IsCurrentDuty ? "(Current)" : IsSelectable ? "" : "(Locked)";
        public bool ShowStatus => IsCurrentDuty || !IsSelectable;

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
    }
}

