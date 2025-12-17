using System.Linq;
using Enlisted.Features.Enlistment;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Features.Lances.Personas;
using Enlisted.Features.Schedule.Behaviors;
using Enlisted.Features.Schedule.Models;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace Enlisted.Features.Camp.UI.Bulletin
{
    /// <summary>
    /// ViewModel for the Lance location in the Camp hub.
    /// Shows lance roster, current schedule/duties, and lance needs.
    /// T1-T4: View-only | T5: View with hints | T6: Can manage assignments
    /// </summary>
    public class CampLanceLocationVM : ViewModel
    {
        private string _headerText;
        private string _lanceNameText;
        private string _tierText;
        private int _playerTier;
        private bool _canManageLance; // T6+
        
        // Lance Roster
        private MBBindingList<LanceMemberVM> _lanceMembers;
        
        // Lance Needs
        private string _readinessText;
        private string _equipmentText;
        private string _moraleText;
        private string _restText;
        private string _suppliesText;
        private int _readinessValue;
        private int _equipmentValue;
        private int _moraleValue;
        private int _restValue;
        private int _suppliesValue;
        
        // Current Schedule/Duties
        private string _currentActivityText;
        private string _nextActivityText;

        public CampLanceLocationVM()
        {
            LanceMembers = new MBBindingList<LanceMemberVM>();
            
            // Initialize default values
            _headerText = "My Lance";
            _lanceNameText = "Loading...";
            _tierText = "T1 - Recruit";
            _currentActivityText = "Current: Loading...";
            _nextActivityText = "Next: N/A";
            _readinessText = "Readiness: 70%";
            _equipmentText = "Equipment: 70%";
            _moraleText = "Morale: 70%";
            _restText = "Rest: 70%";
            _suppliesText = "Supplies: 70%";
            
            RefreshValues();
        }

        public override void RefreshValues()
        {
            base.RefreshValues();
            
            Enlisted.Mod.Core.Logging.ModLogger.Info("CampLanceLocationVM", "RefreshValues called");
            
            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment == null || !enlistment.IsEnlisted)
            {
                Enlisted.Mod.Core.Logging.ModLogger.Warn("CampLanceLocationVM", "Not enlisted or no enlistment behavior");
                HeaderText = "Not Enlisted";
                LanceNameText = "N/A";
                TierText = "N/A";
                CanManageLance = false;
                return;
            }
            
            Enlisted.Mod.Core.Logging.ModLogger.Info("CampLanceLocationVM", $"Enlisted - Tier: {enlistment.EnlistmentTier}, Lance: {enlistment.CurrentLanceName}");

            // Player info
            PlayerTier = enlistment.EnlistmentTier;
            LanceNameText = enlistment.CurrentLanceName ?? "Unassigned";
            TierText = $"T{PlayerTier} - {GetTierName(PlayerTier)}";
            CanManageLance = PlayerTier >= 6; // T6 Lance Leader can manage
            HeaderText = CanManageLance ? "My Lance - Management" : "My Lance - Roster";

            // Refresh roster
            RefreshLanceRoster(enlistment);

            // Refresh lance needs
            RefreshLanceNeeds();

            // Refresh current duties
            RefreshCurrentDuties();
        }

        private void RefreshLanceRoster(EnlistmentBehavior enlistment)
        {
            LanceMembers.Clear();

            // Add player as first member
            LanceMembers.Add(new LanceMemberVM(
                Hero.MainHero?.Name?.ToString() ?? "You",
                GetPlayerRoleText(PlayerTier),
                "Active",
                true // isPlayer
            ));

            // Add placeholder lance mates (Phase 5 persona system will fill these properly)
            LanceMembers.Add(new LanceMemberVM("Lance Member 2", "Lance Member", "Active", false));
            LanceMembers.Add(new LanceMemberVM("Lance Member 3", "Lance Member", "Active", false));
            LanceMembers.Add(new LanceMemberVM("Lance Member 4", "Lance Member", "Active", false));
        }

        private string GetPlayerRoleText(int tier)
        {
            return tier >= 6 ? "Lance Leader" : tier >= 5 ? "Lance Second" : "Lance Member";
        }

        private void RefreshLanceNeeds()
        {
            var scheduleBehavior = ScheduleBehavior.Instance;
            if (scheduleBehavior != null)
            {
                var needs = scheduleBehavior.LanceNeeds;
                ReadinessValue = needs.Readiness;
                EquipmentValue = needs.Equipment;
                MoraleValue = needs.Morale;
                RestValue = needs.Rest;
                SuppliesValue = needs.Supplies;

                ReadinessText = $"Readiness: {needs.Readiness}%";
                EquipmentText = $"Equipment: {needs.Equipment}%";
                MoraleText = $"Morale: {needs.Morale}%";
                RestText = $"Rest: {needs.Rest}%";
                SuppliesText = $"Supplies: {needs.Supplies}%";
            }
            else
            {
                // Fallback values
                ReadinessValue = 70;
                EquipmentValue = 70;
                MoraleValue = 70;
                RestValue = 70;
                SuppliesValue = 70;

                ReadinessText = "Readiness: 70%";
                EquipmentText = "Equipment: 70%";
                MoraleText = "Morale: 70%";
                RestText = "Rest: 70%";
                SuppliesText = "Supplies: 70%";
            }
        }

        private void RefreshCurrentDuties()
        {
            var scheduleBehavior = ScheduleBehavior.Instance;
            if (scheduleBehavior != null && scheduleBehavior.CurrentSchedule != null)
            {
                var currentBlock = scheduleBehavior.CurrentSchedule.GetActiveBlock();
                if (currentBlock != null)
                {
                    CurrentActivityText = $"Current: {currentBlock.Title}";
                }
                else
                {
                    CurrentActivityText = "Current: Free Time";
                }

                // Get next block
                var allBlocks = scheduleBehavior.CurrentSchedule.Blocks.OrderBy(b => (int)b.TimeBlock).ToList();
                var currentHour = (int)CampaignTime.Now.CurrentHourInDay;
                var currentTimeBlock = GetTimeBlockFromHour(currentHour);
                var nextBlock = allBlocks.FirstOrDefault(b => (int)b.TimeBlock > (int)currentTimeBlock);
                if (nextBlock != null)
                {
                    NextActivityText = $"Next: {nextBlock.Title}";
                }
                else
                {
                    NextActivityText = "Next: (New Day)";
                }
            }
            else
            {
                CurrentActivityText = "Current: No Schedule";
                NextActivityText = "Next: N/A";
            }
        }

        private TimeBlock GetTimeBlockFromHour(int hour)
        {
            // Simplified to 4 blocks: Morning, Afternoon, Dusk, Night
            if (hour >= 6 && hour < 12) return TimeBlock.Morning;
            if (hour >= 12 && hour < 18) return TimeBlock.Afternoon;
            if (hour >= 18 && hour < 22) return TimeBlock.Dusk;
            return TimeBlock.Night; // 22-6
        }

        private string GetTierName(int tier)
        {
            return tier switch
            {
                1 => "Recruit",
                2 => "Soldier",
                3 => "Veteran",
                4 => "Elite",
                5 => "Lance Second",
                6 => "Lance Leader",
                _ => "Unknown"
            };
        }

        private string GetRoleText(int slotIndex)
        {
            // Slot 0 is typically the leader, but player might not always be slot 0
            return slotIndex == 0 ? "Lance Leader" : "Lance Member";
        }

        // Properties
        [DataSourceProperty]
        public string HeaderText
        {
            get => _headerText;
            set
            {
                if (value == _headerText) return;
                _headerText = value;
                OnPropertyChangedWithValue(value, nameof(HeaderText));
            }
        }

        [DataSourceProperty]
        public string LanceNameText
        {
            get => _lanceNameText;
            set
            {
                if (value == _lanceNameText) return;
                _lanceNameText = value;
                OnPropertyChangedWithValue(value, nameof(LanceNameText));
            }
        }

        [DataSourceProperty]
        public string TierText
        {
            get => _tierText;
            set
            {
                if (value == _tierText) return;
                _tierText = value;
                OnPropertyChangedWithValue(value, nameof(TierText));
            }
        }

        [DataSourceProperty]
        public int PlayerTier
        {
            get => _playerTier;
            set
            {
                if (value == _playerTier) return;
                _playerTier = value;
                OnPropertyChangedWithValue(value, nameof(PlayerTier));
            }
        }

        [DataSourceProperty]
        public bool CanManageLance
        {
            get => _canManageLance;
            set
            {
                if (value == _canManageLance) return;
                _canManageLance = value;
                OnPropertyChangedWithValue(value, nameof(CanManageLance));
            }
        }

        [DataSourceProperty]
        public MBBindingList<LanceMemberVM> LanceMembers
        {
            get => _lanceMembers;
            set
            {
                if (value == _lanceMembers) return;
                _lanceMembers = value;
                OnPropertyChangedWithValue(value, nameof(LanceMembers));
            }
        }

        [DataSourceProperty]
        public string ReadinessText
        {
            get => _readinessText;
            set
            {
                if (value == _readinessText) return;
                _readinessText = value;
                OnPropertyChangedWithValue(value, nameof(ReadinessText));
            }
        }

        [DataSourceProperty]
        public string EquipmentText
        {
            get => _equipmentText;
            set
            {
                if (value == _equipmentText) return;
                _equipmentText = value;
                OnPropertyChangedWithValue(value, nameof(EquipmentText));
            }
        }

        [DataSourceProperty]
        public string MoraleText
        {
            get => _moraleText;
            set
            {
                if (value == _moraleText) return;
                _moraleText = value;
                OnPropertyChangedWithValue(value, nameof(MoraleText));
            }
        }

        [DataSourceProperty]
        public string RestText
        {
            get => _restText;
            set
            {
                if (value == _restText) return;
                _restText = value;
                OnPropertyChangedWithValue(value, nameof(RestText));
            }
        }

        [DataSourceProperty]
        public string SuppliesText
        {
            get => _suppliesText;
            set
            {
                if (value == _suppliesText) return;
                _suppliesText = value;
                OnPropertyChangedWithValue(value, nameof(SuppliesText));
            }
        }

        [DataSourceProperty]
        public int ReadinessValue
        {
            get => _readinessValue;
            set
            {
                if (value == _readinessValue) return;
                _readinessValue = value;
                OnPropertyChangedWithValue(value, nameof(ReadinessValue));
            }
        }

        [DataSourceProperty]
        public int EquipmentValue
        {
            get => _equipmentValue;
            set
            {
                if (value == _equipmentValue) return;
                _equipmentValue = value;
                OnPropertyChangedWithValue(value, nameof(EquipmentValue));
            }
        }

        [DataSourceProperty]
        public int MoraleValue
        {
            get => _moraleValue;
            set
            {
                if (value == _moraleValue) return;
                _moraleValue = value;
                OnPropertyChangedWithValue(value, nameof(MoraleValue));
            }
        }

        [DataSourceProperty]
        public int RestValue
        {
            get => _restValue;
            set
            {
                if (value == _restValue) return;
                _restValue = value;
                OnPropertyChangedWithValue(value, nameof(RestValue));
            }
        }

        [DataSourceProperty]
        public int SuppliesValue
        {
            get => _suppliesValue;
            set
            {
                if (value == _suppliesValue) return;
                _suppliesValue = value;
                OnPropertyChangedWithValue(value, nameof(SuppliesValue));
            }
        }

        [DataSourceProperty]
        public string CurrentActivityText
        {
            get => _currentActivityText;
            set
            {
                if (value == _currentActivityText) return;
                _currentActivityText = value;
                OnPropertyChangedWithValue(value, nameof(CurrentActivityText));
            }
        }

        [DataSourceProperty]
        public string NextActivityText
        {
            get => _nextActivityText;
            set
            {
                if (value == _nextActivityText) return;
                _nextActivityText = value;
                OnPropertyChangedWithValue(value, nameof(NextActivityText));
            }
        }
    }

    /// <summary>
    /// ViewModel for an individual lance member in the roster.
    /// </summary>
    public class LanceMemberVM : ViewModel
    {
        private string _name;
        private string _role;
        private string _status;
        private bool _isPlayer;

        public LanceMemberVM(string name, string role, string status, bool isPlayer)
        {
            Name = name;
            Role = role;
            Status = status;
            IsPlayer = isPlayer;
        }

        [DataSourceProperty]
        public string Name
        {
            get => _name;
            set
            {
                if (value == _name) return;
                _name = value;
                OnPropertyChangedWithValue(value, nameof(Name));
            }
        }

        [DataSourceProperty]
        public string Role
        {
            get => _role;
            set
            {
                if (value == _role) return;
                _role = value;
                OnPropertyChangedWithValue(value, nameof(Role));
            }
        }

        [DataSourceProperty]
        public string Status
        {
            get => _status;
            set
            {
                if (value == _status) return;
                _status = value;
                OnPropertyChangedWithValue(value, nameof(Status));
            }
        }

        [DataSourceProperty]
        public bool IsPlayer
        {
            get => _isPlayer;
            set
            {
                if (value == _isPlayer) return;
                _isPlayer = value;
                OnPropertyChangedWithValue(value, nameof(IsPlayer));
            }
        }
    }
}

