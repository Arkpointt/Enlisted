using System;
using Enlisted.Features.Camp.UI.Management.Tabs;
using TaleWorlds.CampaignSystem.ViewModelCollection.Input;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace Enlisted.Features.Camp.UI.Management
{
    /// <summary>
    /// Main ViewModel for the Camp Management screen.
    /// Uses tab controller pattern from native KingdomManagementVM.
    /// Tabs: Lance, Orders, Duties, Reports, Army
    /// Note: Camp Activities are in the Game Menu (one-click access), not here.
    ///       Duties are low-frequency configuration and belong in this management screen.
    /// </summary>
    public class CampManagementVM : ViewModel
    {
        private readonly Action _onClose;
        private readonly int _categoryCount = 5;
        private int _currentCategory;
        
        // Tab VMs
        private CampLanceVM _lance;
        private CampScheduleVM _schedule;
        private CampDutiesVM _duties;
        private CampReportsVM _reports;
        private CampArmyVM _army;
        
        // State
        private bool _canSwitchTabs = true;
        private string _titleText;
        private string _doneText;
        
        // Tab labels
        private string _lanceText;
        private string _scheduleText;
        private string _dutiesText;
        private string _reportsText;
        private string _armyText;
        
        // Input keys
        private InputKeyItemVM _doneInputKey;
        private InputKeyItemVM _previousTabInputKey;
        private InputKeyItemVM _nextTabInputKey;
        
        public CampManagementVM(Action onClose)
        {
            _onClose = onClose;
            
            // Initialize tab VMs
            Lance = new CampLanceVM();
            Schedule = new CampScheduleVM();
            Duties = new CampDutiesVM();
            Reports = new CampReportsVM();
            Army = new CampArmyVM();
            
            // Default to Schedule tab (index 1)
            SetSelectedCategory(1);
            RefreshValues();
        }
        
        public override void RefreshValues()
        {
            base.RefreshValues();
            
            TitleText = new TextObject("{=enl_camp_mgmt_title}CAMP MANAGEMENT").ToString();
            DoneText = new TextObject("{=enl_ui_done}Done").ToString();
            LanceText = new TextObject("{=enl_camp_tab_lance}Lance").ToString();
            ScheduleText = new TextObject("{=enl_camp_tab_orders}Orders").ToString();
            DutiesText = new TextObject("{=enl_camp_tab_duties}Duties").ToString();
            ReportsText = new TextObject("{=enl_camp_tab_reports}Reports").ToString();
            ArmyText = new TextObject("{=enl_camp_tab_army}Army").ToString();
            
            // Refresh sub-VMs
            Lance?.RefreshValues();
            Schedule?.RefreshValues();
            Duties?.RefreshValues();
            Reports?.RefreshValues();
            Army?.RefreshValues();
        }
        
        /// <summary>
        /// Per-frame update (matches Kingdom pattern).
        /// </summary>
        public void OnFrameTick()
        {
            // Process any per-frame updates here (decisions, etc.)
        }
        
        /// <summary>
        /// Set the selected tab category (mirrors KingdomManagementVM.SetSelectedCategory).
        /// </summary>
        public void SetSelectedCategory(int index)
        {
            // Hide all panels
            Lance.Show = false;
            Schedule.Show = false;
            Duties.Show = false;
            Reports.Show = false;
            Army.Show = false;
            
            _currentCategory = index;
            
            // Show selected panel
            switch (index)
            {
                case 0:
                    Lance.Show = true;
                    break;
                case 1:
                    Schedule.Show = true;
                    break;
                case 2:
                    Duties.Show = true;
                    break;
                case 3:
                    Reports.Show = true;
                    break;
                default:
                    _currentCategory = 4;
                    Army.Show = true;
                    break;
            }
        }
        
        public void SelectPreviousCategory()
        {
            SetSelectedCategory(_currentCategory == 0 ? _categoryCount - 1 : _currentCategory - 1);
        }
        
        public void SelectNextCategory()
        {
            SetSelectedCategory((_currentCategory + 1) % _categoryCount);
        }
        
        // Tab commands (called from XML buttons)
        public void ExecuteShowLance() => SetSelectedCategory(0);
        public void ExecuteShowSchedule() => SetSelectedCategory(1);
        public void ExecuteShowDuties() => SetSelectedCategory(2);
        public void ExecuteShowReports() => SetSelectedCategory(3);
        public void ExecuteShowArmy() => SetSelectedCategory(4);
        
        public void ExecuteClose() => _onClose?.Invoke();
        
        // Input key setters
        public void SetDoneInputKey(HotKey hotKey)
        {
            DoneInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, true);
        }
        
        public void SetPreviousTabInputKey(HotKey hotKey)
        {
            PreviousTabInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, true);
        }
        
        public void SetNextTabInputKey(HotKey hotKey)
        {
            NextTabInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, true);
        }
        
        public override void OnFinalize()
        {
            base.OnFinalize();
            DoneInputKey?.OnFinalize();
            PreviousTabInputKey?.OnFinalize();
            NextTabInputKey?.OnFinalize();
            Lance?.OnFinalize();
            Schedule?.OnFinalize();
            Duties?.OnFinalize();
            Reports?.OnFinalize();
            Army?.OnFinalize();
        }
        
        // ===== Properties =====
        
        [DataSourceProperty]
        public bool CanSwitchTabs
        {
            get => _canSwitchTabs;
            set
            {
                if (value == _canSwitchTabs) return;
                _canSwitchTabs = value;
                OnPropertyChangedWithValue(value, nameof(CanSwitchTabs));
            }
        }
        
        [DataSourceProperty]
        public string TitleText
        {
            get => _titleText;
            set
            {
                if (value == _titleText) return;
                _titleText = value;
                OnPropertyChangedWithValue(value, nameof(TitleText));
            }
        }
        
        [DataSourceProperty]
        public string DoneText
        {
            get => _doneText;
            set
            {
                if (value == _doneText) return;
                _doneText = value;
                OnPropertyChangedWithValue(value, nameof(DoneText));
            }
        }
        
        [DataSourceProperty]
        public string LanceText
        {
            get => _lanceText;
            set
            {
                if (value == _lanceText) return;
                _lanceText = value;
                OnPropertyChangedWithValue(value, nameof(LanceText));
            }
        }
        
        [DataSourceProperty]
        public string ScheduleText
        {
            get => _scheduleText;
            set
            {
                if (value == _scheduleText) return;
                _scheduleText = value;
                OnPropertyChangedWithValue(value, nameof(ScheduleText));
            }
        }
        
        [DataSourceProperty]
        public string DutiesText
        {
            get => _dutiesText;
            set
            {
                if (value == _dutiesText) return;
                _dutiesText = value;
                OnPropertyChangedWithValue(value, nameof(DutiesText));
            }
        }
        
        [DataSourceProperty]
        public string ReportsText
        {
            get => _reportsText;
            set
            {
                if (value == _reportsText) return;
                _reportsText = value;
                OnPropertyChangedWithValue(value, nameof(ReportsText));
            }
        }
        
        [DataSourceProperty]
        public string ArmyText
        {
            get => _armyText;
            set
            {
                if (value == _armyText) return;
                _armyText = value;
                OnPropertyChangedWithValue(value, nameof(ArmyText));
            }
        }
        
        [DataSourceProperty]
        public CampLanceVM Lance
        {
            get => _lance;
            set
            {
                if (value == _lance) return;
                _lance = value;
                OnPropertyChangedWithValue(value, nameof(Lance));
            }
        }
        
        [DataSourceProperty]
        public CampScheduleVM Schedule
        {
            get => _schedule;
            set
            {
                if (value == _schedule) return;
                _schedule = value;
                OnPropertyChangedWithValue(value, nameof(Schedule));
            }
        }
        
        [DataSourceProperty]
        public CampDutiesVM Duties
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
        public CampReportsVM Reports
        {
            get => _reports;
            set
            {
                if (value == _reports) return;
                _reports = value;
                OnPropertyChangedWithValue(value, nameof(Reports));
            }
        }
        
        [DataSourceProperty]
        public CampArmyVM Army
        {
            get => _army;
            set
            {
                if (value == _army) return;
                _army = value;
                OnPropertyChangedWithValue(value, nameof(Army));
            }
        }
        
        [DataSourceProperty]
        public InputKeyItemVM DoneInputKey
        {
            get => _doneInputKey;
            set
            {
                if (value == _doneInputKey) return;
                _doneInputKey = value;
                OnPropertyChangedWithValue(value, nameof(DoneInputKey));
            }
        }
        
        [DataSourceProperty]
        public InputKeyItemVM PreviousTabInputKey
        {
            get => _previousTabInputKey;
            set
            {
                if (value == _previousTabInputKey) return;
                _previousTabInputKey = value;
                OnPropertyChangedWithValue(value, nameof(PreviousTabInputKey));
            }
        }
        
        [DataSourceProperty]
        public InputKeyItemVM NextTabInputKey
        {
            get => _nextTabInputKey;
            set
            {
                if (value == _nextTabInputKey) return;
                _nextTabInputKey = value;
                OnPropertyChangedWithValue(value, nameof(NextTabInputKey));
            }
        }
    }
}

