using System;
using System.Collections.Generic;
using System.Linq;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Features.Schedule.Behaviors;
using Enlisted.Features.Schedule.Models;
using Enlisted.Mod.Core.Logging;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.SaveSystem;

namespace Enlisted.Features.Lances.Simulation
{
    /// <summary>
    /// Escalation path for Lance Leader vacancy creation.
    /// </summary>
    public enum EscalationPath
    {
        None = 0,
        Promotion = 1,   // Lance Leader promoted to higher position
        Transfer = 2,    // Lance Leader reassigned elsewhere
        Injury = 3,      // Lance Leader incapacitated
        Death = 4,       // Lance Leader dies
        Retirement = 5   // Lance Leader retires from service
    }

    /// <summary>
    /// Core behavior for Lance Life Simulation system.
    /// Track C1: Manages lance member states, injuries, deaths, cover requests, and promotions.
    /// Implements ILanceScheduleModifier for AI Schedule integration.
    /// </summary>
    public class LanceLifeSimulationBehavior : CampaignBehaviorBase, ILanceScheduleModifier
    {
        private const string LogCategory = "LanceLife";
        
        // Base probabilities (per day)
        private const float BaseInjuryChance = 0.001f;   // 0.1% daily base
        private const float BaseFatalAccidentChance = 0.0001f; // 0.01% daily
        
        public static LanceLifeSimulationBehavior Instance { get; private set; }

        // Roster of lance members (synced via SyncData, not SaveableField)
        private List<LanceMemberState> _lanceMembers;

        // Player's escalation state (synced via SyncData, not SaveableField)
        private bool _playerReadyForPromotion;
        private int _weeksWaitingForVacancy;
        private EscalationPath _selectedEscalationPath;
        private CampaignTime _escalationStartTime;
        private bool _escalationInProgress;
        private CampaignTime _lastDailyProcessTime;
        private int _pendingCoverRequestMemberId;
        private bool _memorialPending;
        private string _pendingMemorialMemberId;
        private bool _isInitialized;

        /// <summary>All current lance members (including dead for history)</summary>
        public IReadOnlyList<LanceMemberState> LanceMembers => _lanceMembers;

        /// <summary>Active lance members (alive and not permanently detached)</summary>
        public IEnumerable<LanceMemberState> ActiveMembers => 
            _lanceMembers.Where(m => m.HealthState != MemberHealthState.Dead);

        /// <summary>Members available for duty</summary>
        public IEnumerable<LanceMemberState> AvailableMembers => 
            _lanceMembers.Where(m => m.IsAvailableForDuty());

        /// <summary>Current lance leader (if any)</summary>
        public LanceMemberState CurrentLanceLeader => 
            _lanceMembers.FirstOrDefault(m => m.IsLanceLeader && m.HealthState != MemberHealthState.Dead);

        /// <summary>Is player ready for lance leader promotion?</summary>
        public bool IsPlayerReadyForPromotion => _playerReadyForPromotion;

        /// <summary>Is vacancy escalation in progress?</summary>
        public bool IsEscalationInProgress => _escalationInProgress;

        /// <summary>Selected escalation path (if any)</summary>
        public EscalationPath SelectedEscalation => _selectedEscalationPath;

        public LanceLifeSimulationBehavior()
        {
            Instance = this;
            _lanceMembers = new List<LanceMemberState>();
        }

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
            CampaignEvents.MapEventEnded.AddNonSerializedListener(this, OnMapEventEnded);
        }

        public override void SyncData(IDataStore dataStore)
        {
            SaveLoadDiagnostics.SafeSyncData(this, dataStore, () =>
            {
                dataStore.SyncData("lls_members", ref _lanceMembers);
                dataStore.SyncData("lls_playerReady", ref _playerReadyForPromotion);
                dataStore.SyncData("lls_weeksWaiting", ref _weeksWaitingForVacancy);
                dataStore.SyncData("lls_escalationPath", ref _selectedEscalationPath);
                dataStore.SyncData("lls_escalationStart", ref _escalationStartTime);
                dataStore.SyncData("lls_escalationActive", ref _escalationInProgress);
                dataStore.SyncData("lls_lastDailyProcess", ref _lastDailyProcessTime);
                dataStore.SyncData("lls_pendingCover", ref _pendingCoverRequestMemberId);
                dataStore.SyncData("lls_memorialPending", ref _memorialPending);
                dataStore.SyncData("lls_memorialMember", ref _pendingMemorialMemberId);
                dataStore.SyncData("lls_initialized", ref _isInitialized);

                _lanceMembers ??= new List<LanceMemberState>();
            });
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            if (!_isInitialized)
            {
                Initialize();
            }
        }

        /// <summary>
        /// Initialize the lance life simulation system.
        /// </summary>
        private void Initialize()
        {
            ModLogger.Info(LogCategory, "Initializing Lance Life Simulation system");

            // Generate initial lance roster if not already populated
            if (_lanceMembers.Count == 0)
            {
                GenerateInitialRoster();
            }

            _isInitialized = true;
            ModLogger.Info(LogCategory, $"Lance Life initialized with {_lanceMembers.Count} members");
        }

        /// <summary>
        /// Generate an initial lance roster with procedural members.
        /// </summary>
        private void GenerateInitialRoster()
        {
            ModLogger.Info(LogCategory, "Generating initial lance roster");

            // Get player's current formation/culture for name generation
            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment == null || !enlistment.IsEnlisted)
            {
                ModLogger.Debug(LogCategory, "Player not enlisted - deferring roster generation");
                return;
            }

            // Generate 8-12 lance members (typical lance size)
            int lanceSize = MBRandom.RandomInt(8, 12);
            var random = new Random();

            // Always have a lance leader first
            var leader = LanceMemberState.Create(
                GenerateName(random),
                5, // T5 leader
                GetRandomFormation(random),
                isLeader: true
            );
            leader.DaysInService = random.Next(180, 365); // Experienced
            leader.BattlesParticipated = random.Next(5, 15);
            _lanceMembers.Add(leader);

            // Generate regular members
            for (int i = 1; i < lanceSize; i++)
            {
                int tier = GetRandomTier(random);
                var member = LanceMemberState.Create(
                    GenerateName(random),
                    tier,
                    GetRandomFormation(random)
                );
                member.DaysInService = random.Next(0, 180);
                member.BattlesParticipated = random.Next(0, tier * 2);
                _lanceMembers.Add(member);
            }

            ModLogger.Info(LogCategory, $"Generated {_lanceMembers.Count} lance members");
        }

        /// <summary>
        /// Generate a random name for a lance member.
        /// </summary>
        private string GenerateName(Random random)
        {
            // Simple name pool - in full implementation, use culture-specific names
            string[] firstNames = { "Marcus", "Lucius", "Gaius", "Titus", "Decimus", 
                                    "Quintus", "Publius", "Gnaeus", "Aulus", "Spurius",
                                    "Erik", "Bjorn", "Harald", "Ragnar", "Olaf",
                                    "Aldric", "Baldwin", "Conrad", "Dietrich", "Edmund" };
            
            string[] lastNames = { "Maximus", "Aurelius", "Brutus", "Cassius", "Flavius",
                                   "Ironside", "Bloodaxe", "Fairhair", "Lothbrok", "Skullsplitter",
                                   "Blackwood", "Stoneheart", "Ironfist", "Greybeard", "Swiftblade" };

            return $"{firstNames[random.Next(firstNames.Length)]} {lastNames[random.Next(lastNames.Length)]}";
        }

        /// <summary>
        /// Get a random formation type.
        /// </summary>
        private string GetRandomFormation(Random random)
        {
            string[] formations = { "infantry", "infantry", "infantry", "archer", "cavalry", "horsearcher" };
            return formations[random.Next(formations.Length)];
        }

        /// <summary>
        /// Get a random tier (weighted toward lower tiers).
        /// </summary>
        private int GetRandomTier(Random random)
        {
            int roll = random.Next(100);
            if (roll < 40) return 1;  // 40% T1
            if (roll < 70) return 2;  // 30% T2
            if (roll < 85) return 3;  // 15% T3
            if (roll < 95) return 4;  // 10% T4
            return 5;                  // 5% T5
        }

        /// <summary>
        /// Daily processing tick.
        /// </summary>
        private void OnDailyTick()
        {
            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment == null || !enlistment.IsEnlisted)
                return;

            // Process each lance member
            foreach (var member in _lanceMembers)
            {
                if (member.HealthState == MemberHealthState.Dead)
                    continue;

                // Increment service time
                member.IncrementService();

                // Process recovery
                member.ProcessRecovery();

                // Random injury checks
                ProcessInjuryCheck(member);

                // Check for cover request opportunity
                ProcessCoverRequestOpportunity(member);
            }

            // Check player promotion readiness
            UpdatePlayerReadiness();

            // Process escalation if player is waiting for vacancy
            if (_playerReadyForPromotion && CurrentLanceLeader != null)
            {
                ProcessEscalation();
            }

            // Process pending memorials
            if (_memorialPending)
            {
                ProcessMemorial();
            }

            _lastDailyProcessTime = CampaignTime.Now;
        }

        /// <summary>
        /// Process injury check for a member.
        /// </summary>
        private void ProcessInjuryCheck(LanceMemberState member)
        {
            if (member.HealthState >= MemberHealthState.MajorInjury)
                return; // Already injured

            float injuryChance = CalculateInjuryChance(member);
            float roll = MBRandom.RandomFloat;

            if (roll < injuryChance)
            {
                // Determine severity
                MemberHealthState severity;
                int recoveryDays;
                
                float severityRoll = MBRandom.RandomFloat;
                if (severityRoll < 0.7f)
                {
                    severity = MemberHealthState.MinorInjury;
                    recoveryDays = MBRandom.RandomInt(1, 4);
                }
                else if (severityRoll < 0.95f)
                {
                    severity = MemberHealthState.MajorInjury;
                    recoveryDays = MBRandom.RandomInt(7, 14);
                }
                else
                {
                    severity = MemberHealthState.Incapacitated;
                    recoveryDays = MBRandom.RandomInt(14, 30);
                }

                member.ApplyInjury(severity, recoveryDays);
                
                ModLogger.Info(LogCategory, $"Lance member {member.Name} injured ({severity}), recovery: {recoveryDays} days");

                // Display notification
                InformationManager.DisplayMessage(new InformationMessage(
                    $"{member.Name} has been injured ({severity}).",
                    Color.FromUint(0xFFFFAA00)
                ));

                // Check for death from severe injury
                if (severity == MemberHealthState.Incapacitated && member.MedicalRisk >= 4)
                {
                    float deathChance = 0.05f + (member.MedicalRisk * 0.02f);
                    if (MBRandom.RandomFloat < deathChance)
                    {
                        ProcessMemberDeath(member, "complications from injury");
                    }
                }
            }
        }

        /// <summary>
        /// Calculate injury chance for a member based on context.
        /// </summary>
        private float CalculateInjuryChance(LanceMemberState member)
        {
            float chance = BaseInjuryChance;

            // Modifiers based on activity state
            if (member.ActivityState == MemberActivityState.OnDuty)
            {
                chance += 0.001f; // +0.1% on active duty
            }

            // Check current schedule activity
            var schedule = ScheduleBehavior.Instance;
            if (schedule?.CurrentSchedule != null)
            {
                var currentBlock = schedule.GetCurrentActiveBlock();
                if (currentBlock != null)
                {
                    // Training activities increase injury risk
                    if (currentBlock.BlockType == ScheduleBlockType.TrainingDrill)
                    {
                        chance += 0.005f; // +0.5% during training
                    }
                }
            }

            // Lance needs affect injury chance
            var lanceNeeds = schedule?.LanceNeeds;
            if (lanceNeeds != null)
            {
                // Low rest increases injury chance
                if (lanceNeeds.Rest < 40)
                {
                    chance += 0.002f;
                }
                // Poor equipment increases injury chance
                if (lanceNeeds.Equipment < 40)
                {
                    chance += 0.002f;
                }
            }

            return chance;
        }

        /// <summary>
        /// Process potential cover request opportunity.
        /// </summary>
        private void ProcessCoverRequestOpportunity(LanceMemberState member)
        {
            // Don't spam requests
            if (member.LastCoverRequestTime != null && 
                (CampaignTime.Now - member.LastCoverRequestTime).ToDays < 3)
                return;

            // Check if member might need cover
            float requestChance = 0.02f; // 2% daily base chance

            // Injured members more likely to need cover
            if (member.HealthState == MemberHealthState.MinorInjury)
            {
                requestChance += 0.05f;
            }

            // Friendly members more likely to ask
            if (member.GetRelationshipLevel() == MemberRelationship.Friendly)
            {
                requestChance += 0.02f;
            }

            // Members who owe player favors less likely to ask more
            if (member.FavorsOwedToPlayer > 2)
            {
                requestChance -= 0.03f;
            }

            if (MBRandom.RandomFloat < requestChance)
            {
                // Queue a cover request event (will be processed by event system)
                member.LastCoverRequestTime = CampaignTime.Now;
                member.CoverRequestsMade++;
                
                ModLogger.Debug(LogCategory, $"Cover request queued from {member.Name}");
                // The actual event delivery is handled by LanceLifeEventsAutomaticBehavior
            }
        }

        /// <summary>
        /// Process member death.
        /// </summary>
        public void ProcessMemberDeath(LanceMemberState member, string cause)
        {
            member.MarkDead(cause);

            ModLogger.Info(LogCategory, $"Lance member {member.Name} has died: {cause}");

            // Display notification
            InformationManager.DisplayMessage(new InformationMessage(
                $"{member.Name} has died ({cause}).",
                Color.FromUint(0xFFFF4444)
            ));

            // Queue memorial service
            _memorialPending = true;
            _pendingMemorialMemberId = member.MemberId;

            // If this was the lance leader, trigger vacancy
            if (member.IsLanceLeader)
            {
                OnLanceLeaderVacancy(EscalationPath.Death);
            }

            // Apply morale impact via schedule system
            var schedule = ScheduleBehavior.Instance;
            if (schedule?.LanceNeeds != null)
            {
                schedule.LanceNeeds.Morale = Math.Max(0, schedule.LanceNeeds.Morale - 10);
            }
        }

        /// <summary>
        /// Process pending memorial service.
        /// </summary>
        private void ProcessMemorial()
        {
            if (!_memorialPending)
                return;

            var deceased = _lanceMembers.FirstOrDefault(m => m.MemberId == _pendingMemorialMemberId);
            if (deceased == null)
            {
                _memorialPending = false;
                return;
            }

            // Memorial handled after 2 days
            if (deceased.HealthState == MemberHealthState.Dead)
            {
                ModLogger.Info(LogCategory, $"Memorial service for {deceased.Name}");
                
                // Display memorial message
                InformationManager.DisplayMessage(new InformationMessage(
                    $"A memorial was held for {deceased.Name}. Rest in peace.",
                    Color.FromUint(0xFFAAAAAA)
                ));

                _memorialPending = false;
                _pendingMemorialMemberId = null;
            }
        }

        /// <summary>
        /// Update player's readiness for lance leader promotion.
        /// </summary>
        private void UpdatePlayerReadiness()
        {
            bool wasReady = _playerReadyForPromotion;
            _playerReadyForPromotion = CheckPlayerReadinessForLanceLeader();

            if (_playerReadyForPromotion && !wasReady)
            {
                ModLogger.Info(LogCategory, "Player is now ready for Lance Leader promotion");
                InformationManager.DisplayMessage(new InformationMessage(
                    "You are ready for Lance Leader promotion.",
                    Color.FromUint(0xFF88FF88)
                ));

                // Start escalation timer
                _escalationStartTime = CampaignTime.Now;
                _weeksWaitingForVacancy = 0;
            }
        }

        /// <summary>
        /// Check if player meets all requirements for lance leader.
        /// </summary>
        public bool CheckPlayerReadinessForLanceLeader()
        {
            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment == null || !enlistment.IsEnlisted)
                return false;

            // Minimum tier requirement (T4 = Corporal-equivalent)
            if (enlistment.EnlistmentTier < 4)
                return false;

            // Time in service requirement (90 days minimum)
            if (enlistment.DaysServed < 90)
                return false;

            // Skills requirement (Leadership or relevant combat skill)
            var hero = Hero.MainHero;
            if (hero.GetSkillValue(DefaultSkills.Leadership) < 50 &&
                hero.GetSkillValue(DefaultSkills.OneHanded) < 75)
                return false;

            return true;
        }

        /// <summary>
        /// Process escalation mechanics when player is ready but vacancy doesn't exist.
        /// </summary>
        private void ProcessEscalation()
        {
            if (CurrentLanceLeader == null)
            {
                // Vacancy already exists
                OnLanceLeaderVacancy(EscalationPath.None);
                return;
            }

            // Calculate weeks waiting
            int daysWaiting = (int)(CampaignTime.Now - _escalationStartTime).ToDays;
            int newWeeksWaiting = daysWaiting / 7;

            if (newWeeksWaiting > _weeksWaitingForVacancy)
            {
                _weeksWaitingForVacancy = newWeeksWaiting;

                // Each week increases escalation probability by 10%
                float escalationChance = 0.05f + (_weeksWaitingForVacancy * 0.10f);
                escalationChance = Math.Min(0.95f, escalationChance);

                ModLogger.Debug(LogCategory, $"Escalation check: Week {_weeksWaitingForVacancy}, chance: {escalationChance:P0}");

                if (MBRandom.RandomFloat < escalationChance)
                {
                    // Select and trigger escalation path
                    SelectAndTriggerEscalation();
                }
            }
        }

        /// <summary>
        /// Select an escalation path based on context.
        /// </summary>
        private void SelectAndTriggerEscalation()
        {
            var leader = CurrentLanceLeader;
            if (leader == null)
                return;

            var weights = new Dictionary<EscalationPath, float>
            {
                { EscalationPath.Promotion, 0.40f },
                { EscalationPath.Transfer, 0.30f },
                { EscalationPath.Injury, 0.15f },
                { EscalationPath.Death, 0.10f },
                { EscalationPath.Retirement, 0.05f }
            };

            // Adjust based on leader characteristics
            if (leader.DaysInService > 300)
                weights[EscalationPath.Retirement] *= 2.0f;

            // Wartime adjustments - check if the faction has any active wars
            var enlistment = EnlistmentBehavior.Instance;
            var faction = enlistment?.EnlistedLord?.MapFaction;
            if (faction != null)
            {
                // Check if at war by looking for enemy factions
                bool atWar = Campaign.Current.Factions
                    .Any(f => f != faction && f.IsAtWarWith(faction));
                
                if (atWar)
                {
                    weights[EscalationPath.Injury] *= 1.5f;
                    weights[EscalationPath.Death] *= 1.3f;
                }
            }

            // High skill leaders more likely to get promoted
            if (leader.RankTier >= 5)
                weights[EscalationPath.Promotion] *= 1.5f;

            // Normalize and select
            _selectedEscalationPath = WeightedRandomSelect(weights);
            _escalationInProgress = true;

            ModLogger.Info(LogCategory, $"Escalation triggered: {_selectedEscalationPath}");

            // Execute escalation
            ExecuteEscalation();
        }

        /// <summary>
        /// Execute the selected escalation path.
        /// </summary>
        private void ExecuteEscalation()
        {
            var leader = CurrentLanceLeader;
            if (leader == null)
                return;

            switch (_selectedEscalationPath)
            {
                case EscalationPath.Promotion:
                    InformationManager.DisplayMessage(new InformationMessage(
                        $"{leader.Name} has been promoted to a higher command!",
                        Color.FromUint(0xFF88FF88)
                    ));
                    leader.IsLanceLeader = false;
                    leader.RankTier = 6; // Promoted out
                    OnLanceLeaderVacancy(EscalationPath.Promotion);
                    break;

                case EscalationPath.Transfer:
                    InformationManager.DisplayMessage(new InformationMessage(
                        $"{leader.Name} has been transferred to another unit.",
                        Color.FromUint(0xFFFFFF88)
                    ));
                    leader.IsLanceLeader = false;
                    leader.ActivityState = MemberActivityState.Detached;
                    OnLanceLeaderVacancy(EscalationPath.Transfer);
                    break;

                case EscalationPath.Injury:
                    InformationManager.DisplayMessage(new InformationMessage(
                        $"{leader.Name} has been seriously injured and cannot continue as leader.",
                        Color.FromUint(0xFFFFAA00)
                    ));
                    leader.ApplyInjury(MemberHealthState.Incapacitated, 30);
                    leader.IsLanceLeader = false;
                    OnLanceLeaderVacancy(EscalationPath.Injury);
                    break;

                case EscalationPath.Death:
                    ProcessMemberDeath(leader, "combat wounds");
                    // Vacancy triggered in ProcessMemberDeath
                    break;

                case EscalationPath.Retirement:
                    InformationManager.DisplayMessage(new InformationMessage(
                        $"{leader.Name} has retired from military service.",
                        Color.FromUint(0xFFAAAAAA)
                    ));
                    leader.IsLanceLeader = false;
                    leader.ActivityState = MemberActivityState.Detached;
                    OnLanceLeaderVacancy(EscalationPath.Retirement);
                    break;
            }

            _escalationInProgress = false;
        }

        /// <summary>
        /// Called when lance leader position becomes vacant.
        /// </summary>
        private void OnLanceLeaderVacancy(EscalationPath reason)
        {
            ModLogger.Info(LogCategory, $"Lance Leader vacancy created via {reason}");

            // Check if player should be offered promotion
            if (_playerReadyForPromotion)
            {
                OfferPlayerPromotion();
            }
            else
            {
                // Generate new NPC leader
                GenerateNewLeader();
            }
        }

        /// <summary>
        /// Offer player the lance leader promotion.
        /// </summary>
        private void OfferPlayerPromotion()
        {
            ModLogger.Info(LogCategory, "Offering player Lance Leader promotion");

            InformationManager.DisplayMessage(new InformationMessage(
                "🎖️ You have been offered the position of Lance Leader!",
                Color.FromUint(0xFFFFD700)
            ));

            // In full implementation, this triggers a promotion event/inquiry
            // For now, auto-accept
            AcceptPromotion();
        }

        /// <summary>
        /// Player accepts lance leader promotion.
        /// </summary>
        public void AcceptPromotion()
        {
            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment == null)
                return;

            ModLogger.Info(LogCategory, "Player accepted Lance Leader promotion");

            InformationManager.DisplayMessage(new InformationMessage(
                "🎖️ Congratulations! You are now the Lance Leader.",
                Color.FromUint(0xFF88FF88)
            ));

            // Reset escalation state
            _playerReadyForPromotion = false;
            _weeksWaitingForVacancy = 0;
            _escalationInProgress = false;
            _selectedEscalationPath = EscalationPath.None;
        }

        /// <summary>
        /// Generate a new NPC lance leader when vacancy occurs and player isn't ready.
        /// </summary>
        private void GenerateNewLeader()
        {
            var random = new Random();
            var newLeader = LanceMemberState.Create(
                GenerateName(random),
                5,
                GetRandomFormation(random),
                isLeader: true
            );
            newLeader.DaysInService = random.Next(90, 180);
            newLeader.BattlesParticipated = random.Next(3, 10);

            _lanceMembers.Add(newLeader);

            ModLogger.Info(LogCategory, $"New Lance Leader generated: {newLeader.Name}");

            InformationManager.DisplayMessage(new InformationMessage(
                $"A new Lance Leader has arrived: {newLeader.Name}",
                Color.FromUint(0xFFFFFF88)
            ));
        }

        /// <summary>
        /// Handle post-battle processing for lance members.
        /// </summary>
        private void OnMapEventEnded(MapEvent mapEvent)
        {
            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment == null || !enlistment.IsEnlisted)
                return;

            // Check if player's lord was involved
            var lordParty = enlistment.EnlistedLord?.PartyBelongedTo;
            if (lordParty == null)
                return;

            bool wasInBattle = mapEvent.InvolvedParties.Any(p => p.MobileParty == lordParty);
            if (!wasInBattle)
                return;

            ModLogger.Debug(LogCategory, "Processing post-battle for lance members");

            // Record battle participation for all active members
            foreach (var member in ActiveMembers)
            {
                if (member.IsAvailableForDuty())
                {
                    member.RecordBattle();
                }
            }

            // Post-battle injury/death checks
            float battleSeverity = CalculateBattleSeverity(mapEvent);
            
            foreach (var member in ActiveMembers.Where(m => m.IsAvailableForDuty()).ToList())
            {
                // Injury chance based on battle severity
                float injuryChance = 0.15f * battleSeverity;
                if (MBRandom.RandomFloat < injuryChance)
                {
                    var severity = MBRandom.RandomFloat < 0.7f 
                        ? MemberHealthState.MinorInjury 
                        : MemberHealthState.MajorInjury;
                    
                    int recoveryDays = severity == MemberHealthState.MinorInjury 
                        ? MBRandom.RandomInt(2, 5) 
                        : MBRandom.RandomInt(7, 14);
                    
                    member.ApplyInjury(severity, recoveryDays);
                    
                    InformationManager.DisplayMessage(new InformationMessage(
                        $"{member.Name} was wounded in the battle.",
                        Color.FromUint(0xFFFFAA00)
                    ));
                }

                // Death chance (rare)
                float deathChance = 0.02f * battleSeverity;
                if (MBRandom.RandomFloat < deathChance)
                {
                    ProcessMemberDeath(member, "killed in battle");
                }
            }
        }

        /// <summary>
        /// Calculate battle severity (0-1 scale).
        /// </summary>
        private float CalculateBattleSeverity(MapEvent mapEvent)
        {
            // Basic severity based on battle type and outcome
            // Without Casualties property, use a simpler heuristic
            int totalParticipants = mapEvent.AttackerSide.TroopCount + mapEvent.DefenderSide.TroopCount;
            
            if (totalParticipants == 0)
                return 0.5f;

            // Estimate severity based on battle type
            float baseSeverity = 0.3f;
            
            // Sieges are more severe
            if (mapEvent.IsSiegeAssault)
                baseSeverity = 0.7f;
            else if (mapEvent.IsSiegeOutside)
                baseSeverity = 0.5f;
            else if (mapEvent.IsFieldBattle)
                baseSeverity = 0.4f;
            
            // Add randomness
            baseSeverity += MBRandom.RandomFloat * 0.3f;
            
            return Math.Min(1f, baseSeverity);
        }

        /// <summary>
        /// Weighted random selection from a dictionary of weights.
        /// </summary>
        private T WeightedRandomSelect<T>(Dictionary<T, float> weights)
        {
            float total = weights.Values.Sum();
            float roll = MBRandom.RandomFloat * total;
            float cumulative = 0;

            foreach (var kvp in weights)
            {
                cumulative += kvp.Value;
                if (roll < cumulative)
                    return kvp.Key;
            }

            return weights.Keys.First();
        }

        // ===== Public API for other systems =====

        /// <summary>
        /// Get a lance member by ID.
        /// </summary>
        public LanceMemberState GetMemberById(string memberId)
        {
            return _lanceMembers.FirstOrDefault(m => m.MemberId == memberId);
        }

        /// <summary>
        /// Get count of available members for duty.
        /// </summary>
        public int GetAvailableMemberCount()
        {
            return AvailableMembers.Count();
        }

        /// <summary>
        /// Get count of members needing recovery.
        /// </summary>
        public int GetInjuredMemberCount()
        {
            return _lanceMembers.Count(m => m.NeedsRecovery());
        }

        /// <summary>
        /// Record a cover request response from player.
        /// </summary>
        public void RecordCoverRequestResponse(string memberId, bool accepted)
        {
            var member = GetMemberById(memberId);
            if (member == null)
                return;

            if (accepted)
            {
                member.CoverRequestsAccepted++;
                member.ModifyRelation(10);
                member.FavorsOwedToPlayer++;
                
                ModLogger.Info(LogCategory, $"Player accepted cover request from {member.Name}");
            }
            else
            {
                member.ModifyRelation(-5);
                ModLogger.Info(LogCategory, $"Player refused cover request from {member.Name}");
            }
        }

        /// <summary>
        /// Get roster summary for UI display.
        /// </summary>
        public (int total, int available, int injured, int onLeave) GetRosterSummary()
        {
            int total = ActiveMembers.Count();
            int available = AvailableMembers.Count();
            int injured = _lanceMembers.Count(m => m.NeedsRecovery());
            int onLeave = _lanceMembers.Count(m => m.ActivityState == MemberActivityState.OnLeave);

            return (total, available, injured, onLeave);
        }

        // ===== ILanceScheduleModifier Implementation =====

        /// <inheritdoc/>
        public float GetLanceAvailabilityRatio()
        {
            int total = ActiveMembers.Count();
            if (total == 0) return 1.0f;

            int available = AvailableMembers.Count();
            return (float)available / total;
        }

        /// <inheritdoc/>
        public IEnumerable<string> GetUnavailableMemberIds()
        {
            return _lanceMembers
                .Where(m => !m.IsAvailableForDuty() && m.HealthState != MemberHealthState.Dead)
                .Select(m => m.MemberId);
        }

        /// <inheritdoc/>
        public bool CanCoverDuty(string dutyId)
        {
            // Check if there are available members to cover
            return AvailableMembers.Any();
        }

        /// <inheritdoc/>
        public ScheduleModificationRequest RequestModification(string memberId, string reason)
        {
            var member = GetMemberById(memberId);
            if (member == null)
                return null;

            var request = new ScheduleModificationRequest
            {
                MemberId = memberId,
                Reason = reason,
                Priority = member.IsLanceLeader ? 3 : 1
            };

            // Determine modification type based on reason and availability
            if (!member.IsAvailableForDuty())
            {
                // Try to find replacement
                var replacement = AvailableMembers
                    .Where(m => m.MemberId != memberId)
                    .OrderByDescending(m => m.RankTier)
                    .FirstOrDefault();

                if (replacement != null)
                {
                    request.Type = ModificationType.AssignReplacement;
                    request.ReplacementMemberId = replacement.MemberId;
                }
                else
                {
                    // No replacement available - suggest player cover
                    request.Type = ModificationType.PlayerCover;
                }
            }
            else
            {
                request.Type = ModificationType.RemoveFromDuty;
            }

            return request;
        }

        /// <inheritdoc/>
        public void NotifyUnfulfilledDuty(string dutyId, string reason)
        {
            ModLogger.Info(LogCategory, $"Duty unfulfilled: {dutyId} - {reason}");

            // This could impact lance reputation or discipline
            var schedule = ScheduleBehavior.Instance;
            if (schedule?.LanceNeeds != null)
            {
                // Small morale hit for unfulfilled duty
                schedule.LanceNeeds.Morale = Math.Max(0, schedule.LanceNeeds.Morale - 2);
            }
        }

        /// <inheritdoc/>
        public DutyReassignment GetReassignmentSuggestion(string originalMemberId, string dutyId)
        {
            var originalMember = GetMemberById(originalMemberId);
            if (originalMember == null)
                return null;

            // Find best replacement based on formation and availability
            var candidates = AvailableMembers
                .Where(m => m.MemberId != originalMemberId)
                .OrderByDescending(m => m.FormationType == originalMember.FormationType ? 1 : 0)
                .ThenByDescending(m => m.RankTier)
                .ToList();

            if (candidates.Count == 0)
            {
                // No candidates - suggest player cover
                return new DutyReassignment
                {
                    OriginalMemberId = originalMemberId,
                    DutyId = dutyId,
                    SuggestPlayerCover = true,
                    Confidence = 0.8f
                };
            }

            var replacement = candidates.First();
            return new DutyReassignment
            {
                OriginalMemberId = originalMemberId,
                DutyId = dutyId,
                ReplacementMemberId = replacement.MemberId,
                ReplacementName = replacement.Name,
                SuggestPlayerCover = false,
                Confidence = replacement.FormationType == originalMember.FormationType ? 0.9f : 0.6f
            };
        }
    }

    /// <summary>
    /// Save container definitions for LanceLifeSimulationBehavior fields.
    /// </summary>
    public class LanceLifeSimulationSaveDefiner : SaveableTypeDefiner
    {
        public LanceLifeSimulationSaveDefiner() : base(735311) { }

        protected override void DefineEnumTypes()
        {
            AddEnumDefinition(typeof(EscalationPath), 1);
        }
    }
}

