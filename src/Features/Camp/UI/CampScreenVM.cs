using System;
using Enlisted.Features.Camp.UI.Tabs;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Features.Escalation;
using Enlisted.Mod.Core.Triggers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace Enlisted.Features.Camp.UI
{
    /// <summary>
    /// Main ViewModel for the Camp screen.
    /// Follows the native ClanManagementVM pattern with tab-based navigation.
    /// </summary>
    public class CampScreenVM : ViewModel
    {
        private readonly Action _onClose;
        private readonly int _categoryCount = 7; // News + 6 locations
        private int _currentCategory;

        // Tab ViewModels
        private CampNewsTabVM _newsTab;
        private CampLocationTabVM _medicalTentTab;
        private CampLocationTabVM _trainingGroundsTab;
        private CampLocationTabVM _lordsTentTab;
        private CampLocationTabVM _quartermasterTab;
        private CampLocationTabVM _personalQuartersTab;
        private CampLocationTabVM _campFireTab;

        // Tab selection state
        private bool _isNewsSelected;
        private bool _isMedicalTentSelected;
        private bool _isTrainingGroundsSelected;
        private bool _isLordsTentSelected;
        private bool _isQuartermasterSelected;
        private bool _isPersonalQuartersSelected;
        private bool _isCampFireSelected;

        // Status bar
        private int _fatigue;
        private int _fatigueMax;
        private string _fatigueText;
        private int _heat;
        private string _heatText;
        private int _lanceRep;
        private string _lanceRepText;
        private string _currentTimeText;
        private string _headerTitle;

        private bool _canSwitchTabs = true;

        #region Properties

        [DataSourceProperty]
        public bool CanSwitchTabs
        {
            get => _canSwitchTabs;
            set
            {
                if (_canSwitchTabs != value)
                {
                    _canSwitchTabs = value;
                    OnPropertyChangedWithValue(value, nameof(CanSwitchTabs));
                }
            }
        }

        [DataSourceProperty]
        public string HeaderTitle
        {
            get => _headerTitle;
            set
            {
                if (_headerTitle != value)
                {
                    _headerTitle = value;
                    OnPropertyChangedWithValue(value, nameof(HeaderTitle));
                }
            }
        }

        // Tab VMs
        [DataSourceProperty]
        public CampNewsTabVM NewsTab
        {
            get => _newsTab;
            set
            {
                if (_newsTab != value)
                {
                    _newsTab = value;
                    OnPropertyChangedWithValue(value, nameof(NewsTab));
                }
            }
        }

        [DataSourceProperty]
        public CampLocationTabVM MedicalTentTab
        {
            get => _medicalTentTab;
            set
            {
                if (_medicalTentTab != value)
                {
                    _medicalTentTab = value;
                    OnPropertyChangedWithValue(value, nameof(MedicalTentTab));
                }
            }
        }

        [DataSourceProperty]
        public CampLocationTabVM TrainingGroundsTab
        {
            get => _trainingGroundsTab;
            set
            {
                if (_trainingGroundsTab != value)
                {
                    _trainingGroundsTab = value;
                    OnPropertyChangedWithValue(value, nameof(TrainingGroundsTab));
                }
            }
        }

        [DataSourceProperty]
        public CampLocationTabVM LordsTentTab
        {
            get => _lordsTentTab;
            set
            {
                if (_lordsTentTab != value)
                {
                    _lordsTentTab = value;
                    OnPropertyChangedWithValue(value, nameof(LordsTentTab));
                }
            }
        }

        [DataSourceProperty]
        public CampLocationTabVM QuartermasterTab
        {
            get => _quartermasterTab;
            set
            {
                if (_quartermasterTab != value)
                {
                    _quartermasterTab = value;
                    OnPropertyChangedWithValue(value, nameof(QuartermasterTab));
                }
            }
        }

        [DataSourceProperty]
        public CampLocationTabVM PersonalQuartersTab
        {
            get => _personalQuartersTab;
            set
            {
                if (_personalQuartersTab != value)
                {
                    _personalQuartersTab = value;
                    OnPropertyChangedWithValue(value, nameof(PersonalQuartersTab));
                }
            }
        }

        [DataSourceProperty]
        public CampLocationTabVM CampFireTab
        {
            get => _campFireTab;
            set
            {
                if (_campFireTab != value)
                {
                    _campFireTab = value;
                    OnPropertyChangedWithValue(value, nameof(CampFireTab));
                }
            }
        }

        // Tab selection states
        [DataSourceProperty]
        public bool IsNewsSelected
        {
            get => _isNewsSelected;
            set
            {
                if (_isNewsSelected != value)
                {
                    _isNewsSelected = value;
                    OnPropertyChangedWithValue(value, nameof(IsNewsSelected));
                }
            }
        }

        [DataSourceProperty]
        public bool IsMedicalTentSelected
        {
            get => _isMedicalTentSelected;
            set
            {
                if (_isMedicalTentSelected != value)
                {
                    _isMedicalTentSelected = value;
                    OnPropertyChangedWithValue(value, nameof(IsMedicalTentSelected));
                }
            }
        }

        [DataSourceProperty]
        public bool IsTrainingGroundsSelected
        {
            get => _isTrainingGroundsSelected;
            set
            {
                if (_isTrainingGroundsSelected != value)
                {
                    _isTrainingGroundsSelected = value;
                    OnPropertyChangedWithValue(value, nameof(IsTrainingGroundsSelected));
                }
            }
        }

        [DataSourceProperty]
        public bool IsLordsTentSelected
        {
            get => _isLordsTentSelected;
            set
            {
                if (_isLordsTentSelected != value)
                {
                    _isLordsTentSelected = value;
                    OnPropertyChangedWithValue(value, nameof(IsLordsTentSelected));
                }
            }
        }

        [DataSourceProperty]
        public bool IsQuartermasterSelected
        {
            get => _isQuartermasterSelected;
            set
            {
                if (_isQuartermasterSelected != value)
                {
                    _isQuartermasterSelected = value;
                    OnPropertyChangedWithValue(value, nameof(IsQuartermasterSelected));
                }
            }
        }

        [DataSourceProperty]
        public bool IsPersonalQuartersSelected
        {
            get => _isPersonalQuartersSelected;
            set
            {
                if (_isPersonalQuartersSelected != value)
                {
                    _isPersonalQuartersSelected = value;
                    OnPropertyChangedWithValue(value, nameof(IsPersonalQuartersSelected));
                }
            }
        }

        [DataSourceProperty]
        public bool IsCampFireSelected
        {
            get => _isCampFireSelected;
            set
            {
                if (_isCampFireSelected != value)
                {
                    _isCampFireSelected = value;
                    OnPropertyChangedWithValue(value, nameof(IsCampFireSelected));
                }
            }
        }

        // Status bar
        [DataSourceProperty]
        public int Fatigue
        {
            get => _fatigue;
            set
            {
                if (_fatigue != value)
                {
                    _fatigue = value;
                    OnPropertyChangedWithValue(value, nameof(Fatigue));
                }
            }
        }

        [DataSourceProperty]
        public int FatigueMax
        {
            get => _fatigueMax;
            set
            {
                if (_fatigueMax != value)
                {
                    _fatigueMax = value;
                    OnPropertyChangedWithValue(value, nameof(FatigueMax));
                }
            }
        }

        [DataSourceProperty]
        public string FatigueText
        {
            get => _fatigueText;
            set
            {
                if (_fatigueText != value)
                {
                    _fatigueText = value;
                    OnPropertyChangedWithValue(value, nameof(FatigueText));
                }
            }
        }

        [DataSourceProperty]
        public int Heat
        {
            get => _heat;
            set
            {
                if (_heat != value)
                {
                    _heat = value;
                    OnPropertyChangedWithValue(value, nameof(Heat));
                }
            }
        }

        [DataSourceProperty]
        public string HeatText
        {
            get => _heatText;
            set
            {
                if (_heatText != value)
                {
                    _heatText = value;
                    OnPropertyChangedWithValue(value, nameof(HeatText));
                }
            }
        }

        [DataSourceProperty]
        public int LanceRep
        {
            get => _lanceRep;
            set
            {
                if (_lanceRep != value)
                {
                    _lanceRep = value;
                    OnPropertyChangedWithValue(value, nameof(LanceRep));
                }
            }
        }

        [DataSourceProperty]
        public string LanceRepText
        {
            get => _lanceRepText;
            set
            {
                if (_lanceRepText != value)
                {
                    _lanceRepText = value;
                    OnPropertyChangedWithValue(value, nameof(LanceRepText));
                }
            }
        }

        [DataSourceProperty]
        public string CurrentTimeText
        {
            get => _currentTimeText;
            set
            {
                if (_currentTimeText != value)
                {
                    _currentTimeText = value;
                    OnPropertyChangedWithValue(value, nameof(CurrentTimeText));
                }
            }
        }

        #endregion

        public CampScreenVM(Action onClose)
        {
            _onClose = onClose;
            HeaderTitle = "Camp";

            // Initialize all tab VMs
            NewsTab = new CampNewsTabVM(RefreshCategoryValues);
            MedicalTentTab = new CampLocationTabVM("medical_tent", "Medical", RefreshCategoryValues);
            TrainingGroundsTab = new CampLocationTabVM("training_grounds", "Training", RefreshCategoryValues);
            LordsTentTab = new CampLocationTabVM("lords_tent", "Lord's Tent", RefreshCategoryValues);
            QuartermasterTab = new CampLocationTabVM("quartermaster", "Quartermaster", RefreshCategoryValues);
            PersonalQuartersTab = new CampLocationTabVM("personal_quarters", "Quarters", RefreshCategoryValues);
            CampFireTab = new CampLocationTabVM("camp_fire", "Camp Fire", RefreshCategoryValues);

            // Initialize all tabs with their activity counts
            MedicalTentTab.RefreshActivities();
            TrainingGroundsTab.RefreshActivities();
            LordsTentTab.RefreshActivities();
            QuartermasterTab.RefreshActivities();
            PersonalQuartersTab.RefreshActivities();
            CampFireTab.RefreshActivities();

            // Default to News tab
            SetSelectedCategory(0);
            RefreshStatusBar();
        }

        public void SelectPreviousCategory()
        {
            SetSelectedCategory(_currentCategory == 0
                ? _categoryCount - 1
                : _currentCategory - 1);
        }

        public void SelectNextCategory()
        {
            SetSelectedCategory((_currentCategory + 1) % _categoryCount);
        }

        public void SetSelectedCategory(int index)
        {
            // Clear all selections
            NewsTab.IsSelected = false;
            MedicalTentTab.IsSelected = false;
            TrainingGroundsTab.IsSelected = false;
            LordsTentTab.IsSelected = false;
            QuartermasterTab.IsSelected = false;
            PersonalQuartersTab.IsSelected = false;
            CampFireTab.IsSelected = false;

            _currentCategory = index;

            // Set active tab
            switch (index)
            {
                case 0:
                    NewsTab.IsSelected = true;
                    HeaderTitle = "Camp Reports";
                    break;
                case 1:
                    MedicalTentTab.IsSelected = true;
                    HeaderTitle = "Medical Tent";
                    break;
                case 2:
                    TrainingGroundsTab.IsSelected = true;
                    HeaderTitle = "Training Grounds";
                    break;
                case 3:
                    LordsTentTab.IsSelected = true;
                    HeaderTitle = "Lord's Tent";
                    break;
                case 4:
                    QuartermasterTab.IsSelected = true;
                    HeaderTitle = "Quartermaster";
                    break;
                case 5:
                    PersonalQuartersTab.IsSelected = true;
                    HeaderTitle = "Personal Quarters";
                    break;
                case 6:
                    CampFireTab.IsSelected = true;
                    HeaderTitle = "Camp Fire";
                    break;
            }

            // Update bindable selection states
            IsNewsSelected = NewsTab.IsSelected;
            IsMedicalTentSelected = MedicalTentTab.IsSelected;
            IsTrainingGroundsSelected = TrainingGroundsTab.IsSelected;
            IsLordsTentSelected = LordsTentTab.IsSelected;
            IsQuartermasterSelected = QuartermasterTab.IsSelected;
            IsPersonalQuartersSelected = PersonalQuartersTab.IsSelected;
            IsCampFireSelected = CampFireTab.IsSelected;
        }

        // Commands for tab buttons
        public void ExecuteSelectNews() => SetSelectedCategory(0);
        public void ExecuteSelectMedicalTent() => SetSelectedCategory(1);
        public void ExecuteSelectTrainingGrounds() => SetSelectedCategory(2);
        public void ExecuteSelectLordsTent() => SetSelectedCategory(3);
        public void ExecuteSelectQuartermaster() => SetSelectedCategory(4);
        public void ExecuteSelectPersonalQuarters() => SetSelectedCategory(5);
        public void ExecuteSelectCampFire() => SetSelectedCategory(6);

        public void ExecuteClose() => _onClose?.Invoke();

        public void RefreshCategoryValues()
        {
            // Refresh counts on all tabs
            MedicalTentTab.RefreshActivities();
            TrainingGroundsTab.RefreshActivities();
            LordsTentTab.RefreshActivities();
            QuartermasterTab.RefreshActivities();
            PersonalQuartersTab.RefreshActivities();
            CampFireTab.RefreshActivities();
            RefreshStatusBar();
        }

        private void RefreshStatusBar()
        {
            var enlistment = EnlistmentBehavior.Instance;

            // Fatigue
            var currentFatigue = enlistment?.FatigueCurrent ?? 0;
            var maxFatigue = enlistment?.FatigueMax ?? 24;
            Fatigue = (int)(200.0f * currentFatigue / Math.Max(1, maxFatigue));
            FatigueMax = 200;
            FatigueText = $"Fatigue: {currentFatigue} / {maxFatigue}";

            // Heat - get from EscalationManager
            var escalation = EscalationManager.Instance;
            var heatValue = escalation?.State?.Heat ?? 0;
            Heat = (int)(200.0f * heatValue / 10.0f);
            HeatText = $"Heat: {heatValue} / 10";

            // Lance Rep - get from EscalationManager
            var repValue = escalation?.State?.LanceReputation ?? 0;
            LanceRep = Math.Max(0, (int)((repValue + 50) / 100.0f * 200));
            LanceRepText = repValue >= 0 ? $"Lance Rep: +{repValue}" : $"Lance Rep: {repValue}";

            // Time
            CurrentTimeText = GetCurrentTimeText();
        }

        private string GetCurrentTimeText()
        {
            try
            {
                var dayPart = CampaignTriggerTrackerBehavior.Instance?.GetDayPart();
                var hour = CampaignTime.Now.GetHourOfDay;
                var displayPart = dayPart?.ToString() ?? "Day";
                return $"{displayPart}, {hour:00}:00";
            }
            catch
            {
                return "Unknown";
            }
        }

        public override void OnFinalize()
        {
            base.OnFinalize();
            NewsTab?.OnFinalize();
            MedicalTentTab?.OnFinalize();
            TrainingGroundsTab?.OnFinalize();
            LordsTentTab?.OnFinalize();
            QuartermasterTab?.OnFinalize();
            PersonalQuartersTab?.OnFinalize();
            CampFireTab?.OnFinalize();
        }
    }
}

