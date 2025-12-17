using System;
using System.Collections.Generic;
using System.Linq;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Features.Lances.Personas;
using Enlisted.Features.Ranks;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Library;

namespace Enlisted.Features.Camp.UI.Management.Tabs
{
    /// <summary>
    /// Lance tab ViewModel - Shows lances in lord's party (Clans-style layout).
    /// Left panel: List of lances, Right panel: Selected lance members.
    /// </summary>
    public class CampLanceVM : ViewModel
    {
        private bool _show;
        private MBBindingList<LanceItemVM> _lances;
        private LanceItemVM _selectedLance;
        private string _bannerText;
        private string _typeText;
        private string _nameText;
        private string _membersText;
        private string _fallenText;
        private string _membersHeaderText;
        
        public CampLanceVM()
        {
            Lances = new MBBindingList<LanceItemVM>();
            RefreshValues();
        }
        
        public override void RefreshValues()
        {
            base.RefreshValues();
            BannerText = TaleWorlds.Core.GameTexts.FindText("str_banner").ToString();
            TypeText = "Type";
            NameText = TaleWorlds.Core.GameTexts.FindText("str_scoreboard_header", "name").ToString();
            MembersText = TaleWorlds.Core.GameTexts.FindText("str_members").ToString();
            FallenText = "Fallen";
            MembersHeaderText = "Lance Members";
            
            RefreshLanceList();
        }
        
        /// <summary>
        /// Refresh list of lances from enlistment and persona data.
        /// Currently shows player's lance + demo lances (until AI Lord Lance Simulation is implemented).
        /// </summary>
        private void RefreshLanceList()
        {
            Lances.Clear();
            
            var enlistment = Enlisted.Features.Enlistment.Behaviors.EnlistmentBehavior.Instance;
            if (enlistment == null || !enlistment.IsEnlisted)
            {
                // Not enlisted - show placeholder with empty banner
                var placeholderBanner = new BannerImageIdentifierVM(new TaleWorlds.Core.Banner());
                var placeholder = new LanceItemVM("none", "Not Enlisted", 0, 0, 0, placeholderBanner, OnLanceSelect);
                Lances.Add(placeholder);
                return;
            }
            
            var personaBehavior = LancePersonaBehavior.Instance;
            var lord = enlistment.EnlistedLord;
            var playerLanceId = enlistment.CurrentLanceId;
            
            // Check if player is the lance leader
            var scheduleBehavior = Enlisted.Features.Schedule.Behaviors.ScheduleBehavior.Instance;
            bool isPlayerLanceLeader = scheduleBehavior?.CanUseManualManagement() ?? false;
            
            // Get persistent banner from the banner manager
            var bannerManager = Enlisted.Features.Lances.Behaviors.LanceBannerManager.Instance;
            var playerLanceBanner = bannerManager != null
                ? new BannerImageIdentifierVM(bannerManager.GetOrCreateLanceBanner(lord, playerLanceId, isPlayerLanceLeader))
                : new BannerImageIdentifierVM(lord.ClanBanner);
            
            // Get actual lance name from lance registry
            var lanceAssignment = Enlisted.Features.Assignments.Core.LanceRegistry.ResolveLanceById(playerLanceId);
            var lanceName = lanceAssignment?.Name ?? "The Red Chevron"; // Fallback
            
            // Get player's lance roster
            var playerRoster = personaBehavior?.GetRosterFor(lord, playerLanceId);
            
            if (playerRoster != null)
            {
                var playerLance = new LanceItemVM(
                    playerRoster.LanceKey,
                    lanceName,
                    0, // Infantry
                    playerRoster.Members.Count(m => m.IsAlive),
                    playerRoster.Members.Count(m => !m.IsAlive),
                    playerLanceBanner,
                    OnLanceSelect
                );
                
                // Load members from roster
                foreach (var member in playerRoster.Members)
                {
                    // Use persona's culture-specific rank, or fallback to position-based rank
                    var displayRank = !string.IsNullOrEmpty(member.RankTitleFallback)
                        ? member.RankTitleFallback
                        : GetLancePositionRank(member.Position, lord?.Culture?.StringId ?? "empire");
                    
                    playerLance.AddMember(new LanceMemberItemVM(
                        member.FirstName + (string.IsNullOrEmpty(member.Epithet) ? "" : $" {member.Epithet}"),
                        displayRank,
                        (int)member.Position,
                        member.IsAlive
                    ));
                }
                
                Lances.Add(playerLance);
            }
            else
            {
                // Fallback: Show basic player lance without personas
                // Uses culture-specific rank names from lance persona system
                var lordCulture = lord?.Culture?.StringId ?? "empire";
                var playerLance = new LanceItemVM("player_lance", lanceName, 0, 6, 0, playerLanceBanner, OnLanceSelect);
                playerLance.AddMember(new LanceMemberItemVM("Aldric", GetLancePositionRank(LancePosition.Leader, lordCulture), 0, true));
                playerLance.AddMember(new LanceMemberItemVM("Megenhelda", GetLancePositionRank(LancePosition.Second, lordCulture), 1, true));
                playerLance.AddMember(new LanceMemberItemVM("Elthild", GetLancePositionRank(LancePosition.Veteran, lordCulture), 2, true));
                playerLance.AddMember(new LanceMemberItemVM("Furnhard", GetLancePositionRank(LancePosition.Veteran, lordCulture), 3, true));
                playerLance.AddMember(new LanceMemberItemVM("Liena", GetLancePositionRank(LancePosition.Soldier, lordCulture), 4, true));
                playerLance.AddMember(new LanceMemberItemVM("You (Player)", GetLancePositionRank(LancePosition.Recruit, lordCulture), 5, true));
                Lances.Add(playerLance);
            }
            
            // Demo lances (until AI Lord Lance Simulation is implemented)
            // Each lance gets a persistent unique banner
            var catalog = Enlisted.Features.Assignments.Core.ConfigurationManager.LoadLanceCatalog();
            var availableLances = catalog?.StyleDefinitions?.FirstOrDefault()?.Lances ?? new List<Enlisted.Features.Assignments.Core.LanceDefinition>();
            
            var lance2Name = availableLances.Count > 1 ? availableLances[1].Name : "The Iron Column";
            var lance3Name = availableLances.Count > 2 ? availableLances[2].Name : "The Eagle's Talons";
            
            // Use unique demo lance IDs that won't collide with real lance assignments
            // Format: "demo_{lordStringId}_2" ensures uniqueness per lord and won't match player's lance ID
            var lance2Id = $"demo_{lord.StringId}_2";
            var lance3Id = $"demo_{lord.StringId}_3";
            
            Enlisted.Mod.Core.Logging.ModLogger.Debug("CampLance", $"Player lance ID: {playerLanceId}");
            Enlisted.Mod.Core.Logging.ModLogger.Debug("CampLance", $"Demo lance 2 ID: {lance2Id}");
            Enlisted.Mod.Core.Logging.ModLogger.Debug("CampLance", $"Demo lance 3 ID: {lance3Id}");
            
            var lance2Banner = bannerManager != null
                ? new BannerImageIdentifierVM(bannerManager.GetOrCreateLanceBanner(lord, lance2Id, false))
                : new BannerImageIdentifierVM(lord.ClanBanner);
            var lance3Banner = bannerManager != null
                ? new BannerImageIdentifierVM(bannerManager.GetOrCreateLanceBanner(lord, lance3Id, false))
                : new BannerImageIdentifierVM(lord.ClanBanner);
            
            // Demo lances use the lord's culture for rank names
            var demoCulture = lord?.Culture?.StringId ?? "empire";
            
            // Demo lance 2 - uses culture-specific ranks
            var lance2 = new LanceItemVM("demo_2", lance2Name, 1, 7, 2, lance2Banner, OnLanceSelect); // Cavalry, 2 fallen
            lance2.AddMember(new LanceMemberItemVM("Draconis", GetLancePositionRank(LancePosition.Leader, demoCulture), 0, true));
            lance2.AddMember(new LanceMemberItemVM("Valerius", GetLancePositionRank(LancePosition.Second, demoCulture), 1, true));
            lance2.AddMember(new LanceMemberItemVM("Cassius", GetLancePositionRank(LancePosition.Veteran, demoCulture), 2, true));
            lance2.AddMember(new LanceMemberItemVM("Maximus", GetLancePositionRank(LancePosition.Soldier, demoCulture), 4, true));
            lance2.AddMember(new LanceMemberItemVM("Brutus", GetLancePositionRank(LancePosition.Soldier, demoCulture), 4, true));
            lance2.AddMember(new LanceMemberItemVM("Titus", GetLancePositionRank(LancePosition.Soldier, demoCulture), 4, true));
            lance2.AddMember(new LanceMemberItemVM("Gaius", GetLancePositionRank(LancePosition.Recruit, demoCulture), 5, true));
            Lances.Add(lance2);
            
            // Demo lance 3 - uses culture-specific ranks
            var lance3 = new LanceItemVM("demo_3", lance3Name, 2, 6, 1, lance3Banner, OnLanceSelect); // Archer, 1 fallen
            lance3.AddMember(new LanceMemberItemVM("Aelric", GetLancePositionRank(LancePosition.Leader, demoCulture), 0, true));
            lance3.AddMember(new LanceMemberItemVM("Wulfric", GetLancePositionRank(LancePosition.Second, demoCulture), 1, true));
            lance3.AddMember(new LanceMemberItemVM("Eadric", GetLancePositionRank(LancePosition.Veteran, demoCulture), 2, true));
            lance3.AddMember(new LanceMemberItemVM("Leofric", GetLancePositionRank(LancePosition.Soldier, demoCulture), 4, true));
            lance3.AddMember(new LanceMemberItemVM("Beorn", GetLancePositionRank(LancePosition.Soldier, demoCulture), 4, true));
            lance3.AddMember(new LanceMemberItemVM("Wulfgar", GetLancePositionRank(LancePosition.Recruit, demoCulture), 5, true));
            Lances.Add(lance3);
            
            // Select player's lance by default
            if (Lances.Count > 0)
            {
                OnLanceSelect(Lances[0]);
            }
        }
        
        /// <summary>
        /// Handle lance selection.
        /// </summary>
        private void OnLanceSelect(LanceItemVM lance)
        {
            if (SelectedLance == lance)
                return;
            
            // Deselect previous
            if (SelectedLance != null)
            {
                SelectedLance.IsSelected = false;
            }
            
            // Select new
            SelectedLance = lance;
            if (SelectedLance != null)
            {
                SelectedLance.IsSelected = true;
            }
        }
        
        private string GetFormationName(string cultureId)
        {
            // Simple heuristic - in real implementation, read from lance config
            return "Infantry";
        }
        
        /// <summary>
        /// Get culture-specific rank name for a lance position.
        /// Maps lance positions to tier equivalents and uses RankHelper for localization.
        /// </summary>
        private string GetPositionName(LancePosition position)
        {
            // Get culture from enlisted lord
            var enlistment = EnlistmentBehavior.Instance;
            var cultureId = enlistment?.EnlistedLord?.Culture?.StringId ?? "mercenary";
            
            // Map lance position to approximate tier for rank lookup
            // Leader = T6, Second = T5, SeniorVet = T4, Veteran = T3, Soldier = T2, Recruit = T1
            int tier = position switch
            {
                LancePosition.Leader => 6,
                LancePosition.Second => 5,
                LancePosition.SeniorVeteran => 4,
                LancePosition.Veteran => 3,
                LancePosition.Soldier => 2,
                LancePosition.Recruit => 1,
                _ => 1
            };
            
            return RankHelper.GetRankTitle(tier, cultureId);
        }
        
        /// <summary>
        /// Get culture-specific rank name for a lance position using the persona naming system.
        /// These are the in-lance rank titles (e.g., "Decanus", "Miles", "Tiro" for Empire).
        /// </summary>
        private string GetLancePositionRank(LancePosition position, string cultureId)
        {
            // Culture-specific lance position ranks (matching name_pools.json)
            // These represent roles within the lance, not the player's progression tier
            var cultureRanks = new Dictionary<string, Dictionary<LancePosition, string>>
            {
                ["empire"] = new()
                {
                    [LancePosition.Leader] = "Decanus",
                    [LancePosition.Second] = "Tesserarius",
                    [LancePosition.SeniorVeteran] = "Veteranus Primus",
                    [LancePosition.Veteran] = "Veteranus",
                    [LancePosition.Soldier] = "Miles",
                    [LancePosition.Recruit] = "Tiro"
                },
                ["vlandia"] = new()
                {
                    [LancePosition.Leader] = "Sergeant",
                    [LancePosition.Second] = "Corporal",
                    [LancePosition.SeniorVeteran] = "Senior Man-at-Arms",
                    [LancePosition.Veteran] = "Man-at-Arms",
                    [LancePosition.Soldier] = "Soldier",
                    [LancePosition.Recruit] = "Recruit"
                },
                ["sturgia"] = new()
                {
                    [LancePosition.Leader] = "Húskarl-Leader",
                    [LancePosition.Second] = "Second Húskarl",
                    [LancePosition.SeniorVeteran] = "Veteran Drengr",
                    [LancePosition.Veteran] = "Drengr",
                    [LancePosition.Soldier] = "Karl",
                    [LancePosition.Recruit] = "Recruit"
                },
                ["battania"] = new()
                {
                    [LancePosition.Leader] = "Cennaire",
                    [LancePosition.Second] = "Tánaise",
                    [LancePosition.SeniorVeteran] = "Laoch Mór",
                    [LancePosition.Veteran] = "Laoch",
                    [LancePosition.Soldier] = "Fénnid",
                    [LancePosition.Recruit] = "Dalta"
                },
                ["khuzait"] = new()
                {
                    [LancePosition.Leader] = "Arban-u Darga",
                    [LancePosition.Second] = "Baghatur",
                    [LancePosition.SeniorVeteran] = "Akh Duu",
                    [LancePosition.Veteran] = "Duu",
                    [LancePosition.Soldier] = "Cherbi",
                    [LancePosition.Recruit] = "Koke"
                },
                ["aserai"] = new()
                {
                    [LancePosition.Leader] = "Arif",
                    [LancePosition.Second] = "Naqib",
                    [LancePosition.SeniorVeteran] = "Muqatil Kabir",
                    [LancePosition.Veteran] = "Muqatil",
                    [LancePosition.Soldier] = "Jundi",
                    [LancePosition.Recruit] = "Mubdi"
                }
            };
            
            // Default/generic ranks for fallback
            var defaultRanks = new Dictionary<LancePosition, string>
            {
                [LancePosition.Leader] = "Lance Leader",
                [LancePosition.Second] = "Lance Second",
                [LancePosition.SeniorVeteran] = "Senior Veteran",
                [LancePosition.Veteran] = "Veteran",
                [LancePosition.Soldier] = "Soldier",
                [LancePosition.Recruit] = "Recruit"
            };
            
            var normalizedCulture = cultureId?.ToLowerInvariant() ?? "empire";
            
            // Try to find culture-specific rank
            if (cultureRanks.TryGetValue(normalizedCulture, out var positionRanks) &&
                positionRanks.TryGetValue(position, out var rank))
            {
                return rank;
            }
            
            // Fallback to default ranks
            return defaultRanks.TryGetValue(position, out var defaultRank) ? defaultRank : "Soldier";
        }
        
        
        [DataSourceProperty]
        public bool Show
        {
            get => _show;
            set
            {
                if (value == _show) return;
                _show = value;
                OnPropertyChangedWithValue(value, nameof(Show));
                
                // Refresh when tab becomes visible
                if (value)
                {
                    RefreshLanceList();
                    RefreshNeedsDisplay();
                }
            }
        }
        
        [DataSourceProperty]
        public MBBindingList<LanceItemVM> Lances
        {
            get => _lances;
            set
            {
                if (value == _lances) return;
                _lances = value;
                OnPropertyChangedWithValue(value, nameof(Lances));
            }
        }
        
        [DataSourceProperty]
        public LanceItemVM SelectedLance
        {
            get => _selectedLance;
            set
            {
                if (value == _selectedLance) return;
                _selectedLance = value;
                OnPropertyChangedWithValue(value, nameof(SelectedLance));
                OnPropertyChanged(nameof(HasSelectedLance));
            }
        }
        
        [DataSourceProperty]
        public bool HasSelectedLance => SelectedLance != null;
        
        [DataSourceProperty]
        public string BannerText
        {
            get => _bannerText;
            set
            {
                if (value == _bannerText) return;
                _bannerText = value;
                OnPropertyChangedWithValue(value, nameof(BannerText));
            }
        }
        
        [DataSourceProperty]
        public string TypeText
        {
            get => _typeText;
            set
            {
                if (value == _typeText) return;
                _typeText = value;
                OnPropertyChangedWithValue(value, nameof(TypeText));
            }
        }
        
        [DataSourceProperty]
        public string NameText
        {
            get => _nameText;
            set
            {
                if (value == _nameText) return;
                _nameText = value;
                OnPropertyChangedWithValue(value, nameof(NameText));
            }
        }
        
        [DataSourceProperty]
        public string MembersText
        {
            get => _membersText;
            set
            {
                if (value == _membersText) return;
                _membersText = value;
                OnPropertyChangedWithValue(value, nameof(MembersText));
            }
        }
        
        [DataSourceProperty]
        public string FallenText
        {
            get => _fallenText;
            set
            {
                if (value == _fallenText) return;
                _fallenText = value;
                OnPropertyChangedWithValue(value, nameof(FallenText));
            }
        }
        
        [DataSourceProperty]
        public string MembersHeaderText
        {
            get => _membersHeaderText;
            set
            {
                if (value == _membersHeaderText) return;
                _membersHeaderText = value;
                OnPropertyChangedWithValue(value, nameof(MembersHeaderText));
            }
        }
        
        // ============ Lance Needs Display ============
        
        /// <summary>
        /// Refresh needs display from ScheduleBehavior.
        /// Called when tab becomes visible and when schedule changes.
        /// </summary>
        private void RefreshNeedsDisplay()
        {
            var scheduleBehavior = Enlisted.Features.Schedule.Behaviors.ScheduleBehavior.Instance;
            var needs = scheduleBehavior?.LanceNeeds;
            
            if (needs == null)
            {
                // Default values if no schedule behavior
                NeedReadiness = 60;
                NeedEquipment = 60;
                NeedMorale = 60;
                NeedRest = 60;
                NeedSupplies = 60;
            }
            else
            {
                NeedReadiness = needs.Readiness;
                NeedEquipment = needs.Equipment;
                NeedMorale = needs.Morale;
                NeedRest = needs.Rest;
                NeedSupplies = needs.Supplies;
            }
        }
        
        private int _needReadiness = 60;
        private int _needEquipment = 60;
        private int _needMorale = 60;
        private int _needRest = 60;
        private int _needSupplies = 60;
        
        [DataSourceProperty]
        public int NeedReadiness
        {
            get => _needReadiness;
            set
            {
                if (value == _needReadiness) return;
                _needReadiness = value;
                OnPropertyChangedWithValue(value, nameof(NeedReadiness));
                OnPropertyChanged(nameof(NeedReadinessBar));
                OnPropertyChanged(nameof(NeedReadinessText));
                OnPropertyChanged(nameof(NeedReadinessColor));
            }
        }
        
        [DataSourceProperty]
        public int NeedEquipment
        {
            get => _needEquipment;
            set
            {
                if (value == _needEquipment) return;
                _needEquipment = value;
                OnPropertyChangedWithValue(value, nameof(NeedEquipment));
                OnPropertyChanged(nameof(NeedEquipmentBar));
                OnPropertyChanged(nameof(NeedEquipmentText));
                OnPropertyChanged(nameof(NeedEquipmentColor));
            }
        }
        
        [DataSourceProperty]
        public int NeedMorale
        {
            get => _needMorale;
            set
            {
                if (value == _needMorale) return;
                _needMorale = value;
                OnPropertyChangedWithValue(value, nameof(NeedMorale));
                OnPropertyChanged(nameof(NeedMoraleBar));
                OnPropertyChanged(nameof(NeedMoraleText));
                OnPropertyChanged(nameof(NeedMoraleColor));
            }
        }
        
        [DataSourceProperty]
        public int NeedRest
        {
            get => _needRest;
            set
            {
                if (value == _needRest) return;
                _needRest = value;
                OnPropertyChangedWithValue(value, nameof(NeedRest));
                OnPropertyChanged(nameof(NeedRestBar));
                OnPropertyChanged(nameof(NeedRestText));
                OnPropertyChanged(nameof(NeedRestColor));
            }
        }
        
        [DataSourceProperty]
        public int NeedSupplies
        {
            get => _needSupplies;
            set
            {
                if (value == _needSupplies) return;
                _needSupplies = value;
                OnPropertyChangedWithValue(value, nameof(NeedSupplies));
                OnPropertyChanged(nameof(NeedSuppliesBar));
                OnPropertyChanged(nameof(NeedSuppliesText));
                OnPropertyChanged(nameof(NeedSuppliesColor));
            }
        }
        
        // Scaled values for 300px wide bars (value * 3)
        [DataSourceProperty]
        public int NeedReadinessBar => (int)(_needReadiness * 3);
        
        [DataSourceProperty]
        public int NeedEquipmentBar => (int)(_needEquipment * 3);
        
        [DataSourceProperty]
        public int NeedMoraleBar => (int)(_needMorale * 3);
        
        [DataSourceProperty]
        public int NeedRestBar => (int)(_needRest * 3);
        
        [DataSourceProperty]
        public int NeedSuppliesBar => (int)(_needSupplies * 3);
        
        // Status text (Excellent/Good/Fair/Poor/Critical)
        [DataSourceProperty]
        public string NeedReadinessText => GetNeedStatusText(_needReadiness);
        
        [DataSourceProperty]
        public string NeedEquipmentText => GetNeedStatusText(_needEquipment);
        
        [DataSourceProperty]
        public string NeedMoraleText => GetNeedStatusText(_needMorale);
        
        [DataSourceProperty]
        public string NeedRestText => GetNeedStatusText(_needRest);
        
        [DataSourceProperty]
        public string NeedSuppliesText => GetNeedStatusText(_needSupplies);
        
        // Colors based on value (hex color strings for XML binding)
        [DataSourceProperty]
        public string NeedReadinessColor => GetNeedColor(_needReadiness);
        
        [DataSourceProperty]
        public string NeedEquipmentColor => GetNeedColor(_needEquipment);
        
        [DataSourceProperty]
        public string NeedMoraleColor => GetNeedColor(_needMorale);
        
        [DataSourceProperty]
        public string NeedRestColor => GetNeedColor(_needRest);
        
        [DataSourceProperty]
        public string NeedSuppliesColor => GetNeedColor(_needSupplies);
        
        private string GetNeedStatusText(int value)
        {
            if (value >= 80) return "Excellent";
            if (value >= 60) return "Good";
            if (value >= 40) return "Fair";
            if (value >= 20) return "Poor";
            return "Critical";
        }
        
        private string GetNeedColor(int value)
        {
            if (value >= 80) return "#4CAF50FF"; // Green
            if (value >= 60) return "#8BC34AFF"; // Light green
            if (value >= 40) return "#FFC107FF"; // Yellow/Amber
            if (value >= 20) return "#FF9800FF"; // Orange
            return "#F44336FF"; // Red
        }
    }
    
    /// <summary>
    /// ViewModel for a single lance in the list (like KingdomClanItemVM).
    /// </summary>
    public class LanceItemVM : ViewModel
    {
        private readonly string _lanceKey;
        private readonly Action<LanceItemVM> _onSelect;
        
        private string _name;
        private int _formationType; // 0=Infantry, 1=Cavalry, 2=Archer (matches native ClanType pattern)
        private int _memberCount;
        private int _fallenCount;
        private bool _isSelected;
        private BannerImageIdentifierVM _banner;
        private BannerImageIdentifierVM _banner_9;
        private MBBindingList<LanceMemberItemVM> _members;
        
        public LanceItemVM(string lanceKey, string name, int formationType, int memberCount, int fallenCount, BannerImageIdentifierVM banner, Action<LanceItemVM> onSelect)
        {
            _lanceKey = lanceKey;
            _name = name;
            _formationType = formationType;
            _memberCount = memberCount;
            _fallenCount = fallenCount;
            _onSelect = onSelect;
            _banner = banner;
            // BannerImageIdentifierVM constructor takes (Banner, bool is9Slice)
            // We'll create our own 9-slice version from the same banner
            if (banner != null)
            {
                // Access the internal banner through reflection or create new one
                // For now, just assign the same one (XML will handle 9-slice via Brush)
                _banner_9 = banner;
            }
            Members = new MBBindingList<LanceMemberItemVM>();
        }
        
        public void AddMember(LanceMemberItemVM member)
        {
            Members.Add(member);
        }
        
        public void ExecuteSelect()
        {
            _onSelect?.Invoke(this);
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
        public int FormationType
        {
            get => _formationType;
            set
            {
                if (value == _formationType) return;
                _formationType = value;
                OnPropertyChangedWithValue(value, nameof(FormationType));
            }
        }
        
        [DataSourceProperty]
        public int NumOfMembers
        {
            get => _memberCount;
            set
            {
                if (value == _memberCount) return;
                _memberCount = value;
                OnPropertyChangedWithValue(value, nameof(NumOfMembers));
            }
        }
        
        [DataSourceProperty]
        public int NumOfFallen
        {
            get => _fallenCount;
            set
            {
                if (value == _fallenCount) return;
                _fallenCount = value;
                OnPropertyChangedWithValue(value, nameof(NumOfFallen));
            }
        }
        
        [DataSourceProperty]
        public BannerImageIdentifierVM Banner
        {
            get => _banner;
            set
            {
                if (value == _banner) return;
                _banner = value;
                OnPropertyChangedWithValue(value, nameof(Banner));
            }
        }
        
        [DataSourceProperty]
        public BannerImageIdentifierVM Banner_9
        {
            get => _banner_9;
            set
            {
                if (value == _banner_9) return;
                _banner_9 = value;
                OnPropertyChangedWithValue(value, nameof(Banner_9));
            }
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
        
        [DataSourceProperty]
        public MBBindingList<LanceMemberItemVM> Members
        {
            get => _members;
            set
            {
                if (value == _members) return;
                _members = value;
                OnPropertyChangedWithValue(value, nameof(Members));
            }
        }
    }
    
    /// <summary>
    /// ViewModel for a single lance member (like clan member in native).
    /// </summary>
    public class LanceMemberItemVM : ViewModel
    {
        private string _name;
        private string _rank;
        private int _tier;
        private bool _isAlive;
        
        public LanceMemberItemVM(string name, string rank, int tier, bool isAlive)
        {
            _name = name;
            _rank = rank;
            _tier = tier;
            _isAlive = isAlive;
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
        public string Rank
        {
            get => _rank;
            set
            {
                if (value == _rank) return;
                _rank = value;
                OnPropertyChangedWithValue(value, nameof(Rank));
            }
        }
        
        [DataSourceProperty]
        public int Tier
        {
            get => _tier;
            set
            {
                if (value == _tier) return;
                _tier = value;
                OnPropertyChangedWithValue(value, nameof(Tier));
            }
        }
        
        [DataSourceProperty]
        public bool IsAlive
        {
            get => _isAlive;
            set
            {
                if (value == _isAlive) return;
                _isAlive = value;
                OnPropertyChangedWithValue(value, nameof(IsAlive));
            }
        }
    }
}

