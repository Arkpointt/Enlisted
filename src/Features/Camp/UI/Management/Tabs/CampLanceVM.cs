using System;
using System.Collections.Generic;
using System.Linq;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Features.Lances.Personas;
using Enlisted.Features.Ranks;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Library;
using TaleWorlds.Localization;

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
            TypeText = new TextObject("{=enl_camp_lance_type}Type").ToString();
            NameText = TaleWorlds.Core.GameTexts.FindText("str_scoreboard_header", "name").ToString();
            MembersText = TaleWorlds.Core.GameTexts.FindText("str_members").ToString();
            FallenText = new TextObject("{=enl_camp_lance_fallen}Fallen").ToString();
            MembersHeaderText = new TextObject("{=enl_camp_lance_members_header}Lance Members").ToString();
            
            RefreshLanceList();
        }
        
        /// <summary>
        /// Refresh list of lances from enlistment and persona data.
        /// Shows a deterministic set of lances for the enlisted lord (player lance + other party lances).
        /// Lance count is calculated dynamically: 1 lance per ~10 living members, rounded up.
        /// </summary>
        private void RefreshLanceList()
        {
            Lances.Clear();
            
            var enlistment = Enlisted.Features.Enlistment.Behaviors.EnlistmentBehavior.Instance;
            if (enlistment == null || !enlistment.IsEnlisted)
            {
                // Not enlisted - show placeholder with empty banner
                var placeholderBanner = new BannerImageIdentifierVM(new TaleWorlds.Core.Banner());
                var placeholder = new LanceItemVM("none", new TextObject("{=enl_camp_not_enlisted}Not Enlisted").ToString(), 0, 0, 0, placeholderBanner, OnLanceSelect);
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
            
            // Calculate lance count dynamically based on party size.
            // Rule: 1 lance per 10 living members, rounded up (e.g., 87 troops = 9 lances).
            int lanceCount = CalculateLanceCount(lord);
            
            // Determine the lord's party lances (stable per lord).
            var partyLances = Enlisted.Features.Assignments.Core.LanceRegistry.GetLordPartyLances(
                lord,
                playerCurrentLanceId: playerLanceId,
                maxLances: lanceCount);

            // Calculate member distribution across lances based on actual party size
            // This ensures rosters are created/updated with correct member counts
            var memberDistribution = CalculateMemberDistributionForUI(lord, partyLances.Count);

            for (int i = 0; i < partyLances.Count; i++)
            {
                var lance = partyLances[i];
                if (lance == null || string.IsNullOrWhiteSpace(lance.Id))
                {
                    continue;
                }

                // Formation type mapping for UI (matches ClanType-like layout):
                // 0=Infantry, 1=Cavalry (incl. horsearcher), 2=Archer/Ranged
                var role = (lance.RoleHint ?? string.Empty).ToLowerInvariant();
                var formationType = role == "ranged" ? 2 : (role == "cavalry" || role == "horsearcher" ? 1 : 0);

                var isThisPlayerLance = string.Equals(lance.Id, playerLanceId, StringComparison.OrdinalIgnoreCase);
                var banner = bannerManager != null
                    ? new BannerImageIdentifierVM(bannerManager.GetOrCreateLanceBanner(lord, lance.Id, isThisPlayerLance && isPlayerLanceLeader))
                    : new BannerImageIdentifierVM(lord.ClanBanner);

                // Ensure roster exists with correct member count before retrieving it
                var targetMemberCount = i < memberDistribution.Count ? memberDistribution[i] : 10;
                personaBehavior?.EnsureRosterWithMemberCount(lord, lance.Id, targetMemberCount);
                
                var roster = personaBehavior?.GetRosterFor(lord, lance.Id);
                var memberCount = roster?.Members?.Count(m => m.IsAlive) ?? 0;
                var fallenCount = roster?.Members?.Count(m => !m.IsAlive) ?? 0;

                var item = new LanceItemVM(
                    lanceKey: roster?.LanceKey ?? $"{lord.StringId}:{lance.Id}",
                    name: lance.Name ?? lance.Id,
                    formationType: formationType,
                    memberCount: memberCount,
                    fallenCount: fallenCount,
                    banner: banner,
                    onSelect: OnLanceSelect);

                // Populate members if we have personas; otherwise keep list empty (still shows counts/banners).
                if (roster?.Members != null)
                {
                    var cultureId = lord?.Culture?.StringId ?? "empire";
                    foreach (var member in roster.Members)
                    {
                        var displayRank = !string.IsNullOrEmpty(member.RankTitleFallback)
                            ? member.RankTitleFallback
                            : GetLancePositionRank(member.Position, cultureId);

                        item.AddMember(new LanceMemberItemVM(
                            member.FirstName + (string.IsNullOrEmpty(member.Epithet) ? "" : $" {member.Epithet}"),
                            displayRank,
                            (int)member.Position,
                            member.IsAlive));
                    }
                }

                Lances.Add(item);
            }
            
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
        /// Calculate the number of lances based on party size.
        /// Rule: 1 lance per 10 living members, rounded up.
        /// Example: 87 troops = 9 lances (8 full + 1 for the 7 remainder).
        /// Minimum of 1 lance, maximum capped at 15 for performance.
        /// </summary>
        private int CalculateLanceCount(TaleWorlds.CampaignSystem.Hero lord)
        {
            const int MembersPerLance = 10;
            const int MaxLances = 15;
            const int MinLances = 1;
            
            if (lord?.PartyBelongedTo == null)
            {
                return MinLances;
            }
            
            // Get living (healthy) members count from the lord's party
            int livingMembers = lord.PartyBelongedTo.Party.NumberOfHealthyMembers;
            
            if (livingMembers <= 0)
            {
                return MinLances;
            }
            
            // Calculate: ceiling division (members / 10, rounded up)
            int lanceCount = (livingMembers + MembersPerLance - 1) / MembersPerLance;
            
            // Clamp to reasonable bounds
            return Math.Max(MinLances, Math.Min(MaxLances, lanceCount));
        }

        /// <summary>
        /// Calculate how to distribute party members across lances for UI display.
        /// Example: 87 members with 9 lances = [10,10,10,10,10,10,10,10,7]
        /// </summary>
        private List<int> CalculateMemberDistributionForUI(TaleWorlds.CampaignSystem.Hero lord, int lanceCount)
        {
            var distribution = new List<int>();
            
            if (lord?.PartyBelongedTo == null || lanceCount <= 0)
            {
                // Fallback: standard 10-member lances
                for (int i = 0; i < Math.Max(1, lanceCount); i++)
                {
                    distribution.Add(10);
                }
                return distribution;
            }

            int totalMembers = lord.PartyBelongedTo.Party.NumberOfHealthyMembers;
            if (totalMembers <= 0)
            {
                // No members - give each lance 1 member (the leader placeholder)
                for (int i = 0; i < lanceCount; i++)
                {
                    distribution.Add(1);
                }
                return distribution;
            }

            // Distribute members evenly, with remainder going to earlier lances
            int baseCount = totalMembers / lanceCount;
            int remainder = totalMembers % lanceCount;

            for (int i = 0; i < lanceCount; i++)
            {
                int memberCount = baseCount + (i < remainder ? 1 : 0);
                // Ensure at least 1 member per lance, max 20 for sanity
                memberCount = Math.Max(1, Math.Min(20, memberCount));
                distribution.Add(memberCount);
            }

            return distribution;
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

