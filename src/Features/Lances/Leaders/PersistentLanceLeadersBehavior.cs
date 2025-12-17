using System;
using System.Collections.Generic;
using System.Linq;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Features.Escalation;
using Enlisted.Features.Lances.Simulation;
using Enlisted.Mod.Core.Logging;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.SaveSystem;

namespace Enlisted.Features.Lances.Leaders
{
    /// <summary>
    /// Core behavior for Persistent Lance Leaders system.
    /// Track C2: Manages unique lance leaders per lord with memory and personality.
    /// </summary>
    public class PersistentLanceLeadersBehavior : CampaignBehaviorBase
    {
        private const string LogCategory = "LanceLeaders";
        private const int MaxMemories = 15;
        private const int MemoryDecayDays = 30;

        public static PersistentLanceLeadersBehavior Instance { get; private set; }

        // Main storage: LordId → LanceLeader (synced via SyncData, not SaveableField)
        private Dictionary<string, PersistentLanceLeader> _lanceLeadersByLord;

        // Quick lookup: Which lord is player currently serving?
        private string _currentLordId;

        // Cache for current lance leader (not serialized - rebuilt on load)
        private PersistentLanceLeader _currentLanceLeader;

        // Initialization flag
        private bool _isInitialized;

        /// <summary>Current lance leader (cached)</summary>
        public PersistentLanceLeader CurrentLeader => _currentLanceLeader;

        /// <summary>All lance leaders by lord ID</summary>
        public IReadOnlyDictionary<string, PersistentLanceLeader> AllLeaders => _lanceLeadersByLord;

        public PersistentLanceLeadersBehavior()
        {
            Instance = this;
            _lanceLeadersByLord = new Dictionary<string, PersistentLanceLeader>();
        }

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("pll_leadersByLord", ref _lanceLeadersByLord);
            dataStore.SyncData("pll_currentLordId", ref _currentLordId);
            dataStore.SyncData("pll_initialized", ref _isInitialized);

            _lanceLeadersByLord ??= new Dictionary<string, PersistentLanceLeader>();

            // Rebuild cache after load
            if (dataStore.IsLoading && !string.IsNullOrEmpty(_currentLordId))
            {
                _lanceLeadersByLord.TryGetValue(_currentLordId, out _currentLanceLeader);
            }
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            if (!_isInitialized)
            {
                Initialize();
            }

            // Reconnect to current lord if enlisted
            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment?.IsEnlisted == true && enlistment.EnlistedLord != null)
            {
                OnPlayerEnlisted(enlistment.EnlistedLord);
            }
        }

        private void Initialize()
        {
            ModLogger.Info(LogCategory, "Initializing Persistent Lance Leaders system");
            _isInitialized = true;
        }

        private void OnDailyTick()
        {
            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment == null || !enlistment.IsEnlisted)
                return;

            // Increment days served for current leader
            if (_currentLanceLeader != null && _currentLanceLeader.IsAlive)
            {
                _currentLanceLeader.IncrementDaysServed();
                _currentLanceLeader.ProcessMemoryDecay();
            }
        }

        // ===== Public API =====

        /// <summary>
        /// Called when player enlists with a lord.
        /// </summary>
        public void OnPlayerEnlisted(Hero lord)
        {
            if (lord == null)
                return;

            _currentLordId = lord.StringId;
            _currentLanceLeader = GetOrCreateLanceLeader(lord);

            // Update tracking for re-enlistment
            if (_currentLanceLeader.FirstMetDate != CampaignTime.Zero)
            {
                // Returning soldier
                _currentLanceLeader.AddMemory(MemoryEntry.Create(
                    MemoryType.ReEnlisted,
                    "Player re-enlisted after leaving",
                    2 // Small positive impact
                ));
                
                ModLogger.Info(LogCategory, $"Player re-enlisted under {_currentLanceLeader.FullName}");
            }
            else
            {
                // First time meeting
                _currentLanceLeader.FirstMetDate = CampaignTime.Now;
                ModLogger.Info(LogCategory, $"Player first meeting with {_currentLanceLeader.FullName}");
            }

            _currentLanceLeader.DaysServedUnder = 0;
        }

        /// <summary>
        /// Called when player leaves service.
        /// </summary>
        public void OnPlayerDischarged()
        {
            if (_currentLanceLeader != null)
            {
                _currentLanceLeader.AddMemory(MemoryEntry.Create(
                    MemoryType.Discharged,
                    $"Player left service after {_currentLanceLeader.DaysServedUnder} days",
                    -1 // Slight negative
                ));
                
                ModLogger.Info(LogCategory, $"Player discharged from {_currentLanceLeader.FullName}'s lance");
            }

            _currentLordId = null;
            _currentLanceLeader = null;
        }

        /// <summary>
        /// Get current lance leader (creates if needed).
        /// </summary>
        public PersistentLanceLeader GetCurrentLanceLeader()
        {
            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment == null || !enlistment.IsEnlisted)
                return null;

            var currentLord = enlistment.EnlistedLord;
            if (currentLord == null)
                return null;

            return GetOrCreateLanceLeader(currentLord);
        }

        /// <summary>
        /// Get or create lance leader for specific lord.
        /// </summary>
        public PersistentLanceLeader GetOrCreateLanceLeader(Hero lord)
        {
            if (lord == null)
                return null;

            string lordId = lord.StringId;

            // Check if we already have a lance leader for this lord
            if (_lanceLeadersByLord.TryGetValue(lordId, out var existing))
            {
                // Check if they're still alive
                if (existing.IsAlive)
                {
                    if (_currentLordId == lordId)
                        _currentLanceLeader = existing;
                    return existing;
                }

                // Dead - need to generate replacement
                ModLogger.Info(LogCategory, $"Lance leader {existing.FullName} is dead. Generating replacement.");
            }

            // Generate new lance leader
            var newLeader = GenerateLanceLeader(lord);
            _lanceLeadersByLord[lordId] = newLeader;

            ModLogger.Info(LogCategory, $"Generated new lance leader: {newLeader.FullName} for {lord.Name}");

            if (_currentLordId == lordId)
                _currentLanceLeader = newLeader;

            return newLeader;
        }

        /// <summary>
        /// Record an event choice in the current leader's memory.
        /// </summary>
        public void RecordEventChoice(string eventId, string choiceId, int impactScore, string description = null)
        {
            if (_currentLanceLeader == null || !_currentLanceLeader.IsAlive)
                return;

            var memory = MemoryEntry.FromEventChoice(
                eventId,
                choiceId,
                description ?? $"Player chose {choiceId} for {eventId}",
                impactScore
            );

            // Determine memory type based on impact and keywords
            memory.Type = DetermineMemoryType(choiceId, impactScore);

            _currentLanceLeader.AddMemory(memory);

            ModLogger.Info(LogCategory, $"{_currentLanceLeader.FirstName} remembers: {memory.Description} (Impact: {impactScore})");
        }

        /// <summary>
        /// Record a battle performance.
        /// </summary>
        public void RecordBattlePerformance(int killCount, bool wasVictory, bool wasHeroic)
        {
            if (_currentLanceLeader == null || !_currentLanceLeader.IsAlive)
                return;

            int impact = wasVictory ? 2 : -1;
            if (wasHeroic) impact += 5;
            if (killCount >= 5) impact += 2;

            var memory = MemoryEntry.Create(
                MemoryType.BattlePerformance,
                wasHeroic 
                    ? $"Player fought heroically, killing {killCount} enemies" 
                    : $"Player participated in {(wasVictory ? "victorious" : "lost")} battle",
                impact
            );

            _currentLanceLeader.AddMemory(memory);
        }

        /// <summary>
        /// Record player promotion.
        /// </summary>
        public void RecordPromotion(int newTier)
        {
            if (_currentLanceLeader == null || !_currentLanceLeader.IsAlive)
                return;

            var memory = MemoryEntry.Create(
                MemoryType.PromotionMoment,
                $"Player promoted to tier {newTier}",
                5 // Significant positive
            );

            _currentLanceLeader.AddMemory(memory);
        }

        /// <summary>
        /// Get reaction from current lance leader based on context.
        /// </summary>
        public LanceLeaderReaction GetReaction(LanceLeaderContext context = null)
        {
            if (_currentLanceLeader == null || !_currentLanceLeader.IsAlive)
                return null;

            context ??= BuildCurrentContext();

            var reaction = new LanceLeaderReaction
            {
                Tone = DetermineTone(_currentLanceLeader, context),
                Opening = GenerateOpening(_currentLanceLeader, context),
                ReferencesPastEvent = false,
                WarningGiven = false
            };

            // Check for warnings
            if (context.HeatLevel >= 5)
            {
                reaction.WarningGiven = true;
                reaction.WarningText = GenerateHeatWarning(_currentLanceLeader);
            }
            else if (context.DisciplineLevel >= 5)
            {
                reaction.WarningGiven = true;
                reaction.WarningText = GenerateDisciplineWarning(_currentLanceLeader);
            }

            // Check for past event reference
            var recentMemory = _currentLanceLeader.RecentMemories
                .Where(m => m.DaysSinceCreated < 7 && Math.Abs(m.ImpactScore) >= 5)
                .OrderByDescending(m => Math.Abs(m.ImpactScore))
                .FirstOrDefault();

            if (recentMemory != null)
            {
                reaction.ReferencesPastEvent = true;
                reaction.ReferencedMemory = recentMemory;
            }

            return reaction;
        }

        /// <summary>
        /// Handle lance leader death (called from LanceLifeSimulation).
        /// </summary>
        public void OnLanceLeaderDeath(string lordId, string cause)
        {
            if (!_lanceLeadersByLord.TryGetValue(lordId, out var leader))
                return;

            if (!leader.IsAlive)
                return;

            leader.MarkDead(cause);

            ModLogger.Info(LogCategory, $"Lance leader {leader.FullName} has died: {cause}");

            InformationManager.DisplayMessage(new InformationMessage(
                $"{leader.FormalTitle} has fallen ({cause}).",
                Color.FromUint(0xFFFF4444)
            ));

            // Clear current cache if this was current leader
            if (_currentLordId == lordId)
            {
                _currentLanceLeader = null;
            }
        }

        /// <summary>
        /// Handle lance leader vacancy (offer player promotion or generate new leader).
        /// </summary>
        public void OnLeaderVacancy(string lordId, EscalationPath reason)
        {
            var lanceLife = LanceLifeSimulationBehavior.Instance;
            
            // Check if player should be offered promotion
            if (lanceLife?.IsPlayerReadyForPromotion == true)
            {
                OfferPlayerPromotion(lordId);
            }
            else
            {
                // Generate new NPC leader
                var lord = Hero.FindFirst(h => h.StringId == lordId);
                if (lord != null)
                {
                    var newLeader = GetOrCreateLanceLeader(lord);
                    TriggerIntroductionEvent(newLeader);
                }
            }
        }

        // ===== Private Methods =====

        /// <summary>
        /// Generate a new lance leader for a lord.
        /// </summary>
        private PersistentLanceLeader GenerateLanceLeader(Hero lord)
        {
            var random = new Random(lord.StringId.GetHashCode() + (int)CampaignTime.Now.ToHours);
            
            // Determine culture (90% lord's culture, 10% allied)
            string culture = lord.Culture?.StringId ?? "empire";

            // Generate name
            var (firstName, epithet, isFemale) = GenerateName(culture, random);

            // Generate personality
            var (primary, secondary) = GeneratePersonality(random);

            // Create leader
            var leader = new PersistentLanceLeader
            {
                LordId = lord.StringId,
                FirstName = firstName,
                Epithet = epithet,
                Name = string.IsNullOrEmpty(epithet) ? firstName : $"{firstName} \"{epithet}\"",
                Culture = culture,
                RankTitle = GetRankTitle(culture),
                IsFemale = isFemale,
                PrimaryTrait = primary,
                SecondaryTraitValue = secondary,
                PersonalitySeed = random.Next(int.MaxValue),
                IsAlive = true
            };

            // Set attributes based on personality
            InitializeAttributes(leader, random);

            // Generate background
            leader.Background = GenerateBackground(culture, random);

            return leader;
        }

        /// <summary>
        /// Generate a culture-appropriate name.
        /// </summary>
        private (string firstName, string epithet, bool isFemale) GenerateName(string culture, Random random)
        {
            // Female chance varies by culture (simplified)
            bool isFemale = random.NextDouble() < GetFemaleLanceLeaderChance(culture);

            string firstName = isFemale 
                ? GetRandomFemaleName(culture, random) 
                : GetRandomMaleName(culture, random);

            // 70% chance of epithet (veterans earn epithets)
            string epithet = random.NextDouble() < 0.7 
                ? GetRandomEpithet(culture, random) 
                : null;

            return (firstName, epithet, isFemale);
        }

        /// <summary>
        /// Generate personality traits.
        /// </summary>
        private (PersonalityTrait primary, SecondaryTrait secondary) GeneratePersonality(Random random)
        {
            // Weighted primary trait selection
            int roll = random.Next(100);
            PersonalityTrait primary;
            if (roll < 20) primary = PersonalityTrait.Stern;
            else if (roll < 45) primary = PersonalityTrait.Fair;
            else if (roll < 65) primary = PersonalityTrait.Pragmatic;
            else if (roll < 80) primary = PersonalityTrait.Fatherly;
            else if (roll < 90) primary = PersonalityTrait.Ambitious;
            else primary = PersonalityTrait.Cynical;

            // Compatible secondary traits
            var secondaryOptions = GetCompatibleSecondaryTraits(primary);
            var secondary = secondaryOptions[random.Next(secondaryOptions.Length)];

            return (primary, secondary);
        }

        /// <summary>
        /// Initialize attributes based on personality.
        /// </summary>
        private void InitializeAttributes(PersistentLanceLeader leader, Random random)
        {
            switch (leader.PrimaryTrait)
            {
                case PersonalityTrait.Stern:
                    leader.Strictness = random.Next(70, 96);
                    leader.Pragmatism = random.Next(40, 61);
                    leader.Loyalty = random.Next(60, 81);
                    break;
                case PersonalityTrait.Fair:
                    leader.Strictness = random.Next(50, 71);
                    leader.Pragmatism = random.Next(50, 71);
                    leader.Loyalty = random.Next(60, 81);
                    break;
                case PersonalityTrait.Pragmatic:
                    leader.Strictness = random.Next(30, 51);
                    leader.Pragmatism = random.Next(75, 96);
                    leader.Loyalty = random.Next(40, 61);
                    break;
                case PersonalityTrait.Fatherly:
                    leader.Strictness = random.Next(30, 51);
                    leader.Pragmatism = random.Next(55, 76);
                    leader.Loyalty = random.Next(70, 91);
                    break;
                case PersonalityTrait.Ambitious:
                    leader.Strictness = random.Next(45, 66);
                    leader.Pragmatism = random.Next(70, 91);
                    leader.Loyalty = random.Next(50, 71);
                    break;
                case PersonalityTrait.Cynical:
                    leader.Strictness = random.Next(40, 61);
                    leader.Pragmatism = random.Next(65, 86);
                    leader.Loyalty = random.Next(30, 51);
                    break;
            }

            // Initialize memory
            leader.RelationshipScore = 0;
            leader.TrustLevel = 25;
        }

        /// <summary>
        /// Generate background story.
        /// </summary>
        private LanceLeaderBackground GenerateBackground(string culture, Random random)
        {
            var background = new LanceLeaderBackground
            {
                YearsOfService = random.Next(8, 26),
                FormerRole = GetRandomRole(culture, random),
                IsVeteran = random.NextDouble() < 0.6
            };

            // Generate battle scars (0-2)
            int scarCount = random.Next(3);
            string[] scarOptions = { "missing_finger", "eye_scar", "sword_scar_arm", "arrow_scar_shoulder", "burn_scar" };
            for (int i = 0; i < scarCount; i++)
            {
                string scar = scarOptions[random.Next(scarOptions.Length)];
                if (!background.BattleScars.Contains(scar))
                    background.BattleScars.Add(scar);
            }

            // Special quality
            string[] qualities = { "tactical_genius", "brutal", "inspiring", "steady", "fearless", "cunning" };
            background.SpecialQuality = qualities[random.Next(qualities.Length)];

            return background;
        }

        /// <summary>
        /// Determine the dialog tone based on relationship and personality.
        /// </summary>
        private DialogTone DetermineTone(PersistentLanceLeader leader, LanceLeaderContext context)
        {
            int relationship = leader.RelationshipScore;

            switch (leader.PrimaryTrait)
            {
                case PersonalityTrait.Stern:
                    return relationship > 50 ? DialogTone.RespectfullyFormal : DialogTone.ColdlyFormal;

                case PersonalityTrait.Fatherly:
                    return relationship > 30 ? DialogTone.WarmFamilial : DialogTone.PatientlyFormal;

                case PersonalityTrait.Pragmatic:
                    return relationship > 40 ? DialogTone.CasuallyProfessional : DialogTone.BusinessLike;

                case PersonalityTrait.Fair:
                    return relationship > 40 ? DialogTone.FriendlyProfessional : DialogTone.NeutralProfessional;

                case PersonalityTrait.Ambitious:
                    return relationship > 50 ? DialogTone.CasuallyProfessional : DialogTone.BusinessLike;

                case PersonalityTrait.Cynical:
                    return relationship > 60 ? DialogTone.CasuallyProfessional : DialogTone.ColdlyFormal;

                default:
                    return DialogTone.NeutralProfessional;
            }
        }

        /// <summary>
        /// Generate opening dialogue.
        /// </summary>
        private string GenerateOpening(PersistentLanceLeader leader, LanceLeaderContext context)
        {
            int relationship = leader.RelationshipScore;
            int trust = leader.TrustLevel;

            // Check for immediate issues
            if (context.HeatLevel >= 5)
                return GetHeatOpeningLine(leader);

            if (context.DisciplineLevel >= 5)
                return GetDisciplineOpeningLine(leader);

            // Standard greetings based on relationship and personality
            if (relationship >= 60 && trust >= 60)
                return GetHighTrustGreeting(leader);
            
            if (relationship >= 30)
                return GetModerateTrustGreeting(leader);
            
            if (relationship <= -20)
                return GetLowTrustGreeting(leader);

            return GetNeutralGreeting(leader);
        }

        private string GetHeatOpeningLine(PersistentLanceLeader leader)
        {
            switch (leader.PrimaryTrait)
            {
                case PersonalityTrait.Stern:
                    return "I've heard rumors. They better not be true.";
                case PersonalityTrait.Pragmatic:
                    return "Word is you've been cutting corners. Be careful.";
                case PersonalityTrait.Fatherly:
                    return "We need to talk about some concerning reports.";
                default:
                    return "I've heard some things. We should discuss them.";
            }
        }

        private string GetDisciplineOpeningLine(PersistentLanceLeader leader)
        {
            switch (leader.PrimaryTrait)
            {
                case PersonalityTrait.Stern:
                    return "Your conduct has been unacceptable.";
                case PersonalityTrait.Pragmatic:
                    return "Your discipline record is becoming a problem.";
                case PersonalityTrait.Fatherly:
                    return "I'm disappointed in your recent behavior.";
                default:
                    return "We need to address your discipline issues.";
            }
        }

        private string GetHighTrustGreeting(PersistentLanceLeader leader)
        {
            switch (leader.PrimaryTrait)
            {
                case PersonalityTrait.Stern:
                    return "Good to see you. You've earned my respect.";
                case PersonalityTrait.Fatherly:
                    return "Ah, there you are. How's my best soldier?";
                case PersonalityTrait.Pragmatic:
                    return "Just who I needed. You always deliver.";
                default:
                    return "Welcome. It's good to have you.";
            }
        }

        private string GetModerateTrustGreeting(PersistentLanceLeader leader)
        {
            switch (leader.PrimaryTrait)
            {
                case PersonalityTrait.Stern:
                    return "Soldier. Report.";
                case PersonalityTrait.Fatherly:
                    return "Ah, good. Come in.";
                case PersonalityTrait.Pragmatic:
                    return "What's the situation?";
                default:
                    return "At ease. What do you need?";
            }
        }

        private string GetLowTrustGreeting(PersistentLanceLeader leader)
        {
            switch (leader.PrimaryTrait)
            {
                case PersonalityTrait.Stern:
                    return "You. Make it quick.";
                case PersonalityTrait.Fatherly:
                    return "You again. I hope you've improved.";
                case PersonalityTrait.Pragmatic:
                    return "What do you want?";
                default:
                    return "State your business.";
            }
        }

        private string GetNeutralGreeting(PersistentLanceLeader leader)
        {
            switch (leader.PrimaryTrait)
            {
                case PersonalityTrait.Stern:
                    return "Report.";
                case PersonalityTrait.Fatherly:
                    return "Come in, come in.";
                case PersonalityTrait.Pragmatic:
                    return "Yes?";
                default:
                    return "What is it?";
            }
        }

        private string GenerateHeatWarning(PersistentLanceLeader leader)
        {
            switch (leader.PrimaryTrait)
            {
                case PersonalityTrait.Stern:
                    return "Your activities have attracted attention. Stop immediately or face consequences.";
                case PersonalityTrait.Pragmatic:
                    return "You're generating heat. Either get smarter about it or stop.";
                case PersonalityTrait.Fatherly:
                    return "I'm worried about you. These rumors could destroy your career.";
                case PersonalityTrait.Fair:
                    return "I will not tolerate corruption in my lance. Consider this your final warning.";
                default:
                    return "You're drawing attention to yourself. Be careful.";
            }
        }

        private string GenerateDisciplineWarning(PersistentLanceLeader leader)
        {
            switch (leader.PrimaryTrait)
            {
                case PersonalityTrait.Stern:
                    return "One more infraction and I'll have you on punishment detail for a month.";
                case PersonalityTrait.Pragmatic:
                    return "Your discipline is affecting the lance's performance. Fix it.";
                case PersonalityTrait.Fatherly:
                    return "You're better than this. What's going on with you?";
                default:
                    return "Your discipline record is becoming a serious issue.";
            }
        }

        private MemoryType DetermineMemoryType(string choiceId, int impact)
        {
            if (choiceId.Contains("corrupt") || choiceId.Contains("bribe") || choiceId.Contains("steal"))
                return MemoryType.CorruptionChoice;

            if (impact >= 8)
                return MemoryType.HeroicAction;

            if (impact <= -5)
                return MemoryType.Failure;

            return MemoryType.DutyEvent;
        }

        private LanceLeaderContext BuildCurrentContext()
        {
            var escalation = EscalationManager.Instance;
            var enlistment = EnlistmentBehavior.Instance;

            return new LanceLeaderContext
            {
                HeatLevel = escalation?.State?.Heat ?? 0,
                DisciplineLevel = escalation?.State?.Discipline ?? 0,
                LanceReputation = escalation?.State?.LanceReputation ?? 0,
                PlayerTier = enlistment?.EnlistmentTier ?? 1,
                IsInBattle = false,
                IsOnLeave = enlistment?.IsOnLeave ?? false
            };
        }

        private void OfferPlayerPromotion(string lordId)
        {
            ModLogger.Info(LogCategory, "Offering player Lance Leader promotion");

            InformationManager.DisplayMessage(new InformationMessage(
                "🎖️ You have been offered the position of Lance Leader!",
                Color.FromUint(0xFFFFD700)
            ));
        }

        private void TriggerIntroductionEvent(PersistentLanceLeader newLeader)
        {
            ModLogger.Info(LogCategory, $"New Lance Leader introduction: {newLeader.FullName}");

            InformationManager.DisplayMessage(new InformationMessage(
                $"A new Lance Leader has arrived: {newLeader.FormalTitle}",
                Color.FromUint(0xFFFFFF88)
            ));
        }

        // ===== Name/Culture Data =====

        private double GetFemaleLanceLeaderChance(string culture)
        {
            // Varies by culture
            switch (culture.ToLowerInvariant())
            {
                case "khuzait": return 0.25;
                case "battania": return 0.20;
                case "empire": return 0.15;
                case "sturgia": return 0.15;
                case "vlandia": return 0.10;
                case "aserai": return 0.08;
                default: return 0.15;
            }
        }

        private string GetRandomMaleName(string culture, Random random)
        {
            string[] names = culture.ToLowerInvariant() switch
            {
                "empire" => new[] { "Gaius", "Marcus", "Lucius", "Titus", "Quintus", "Decimus", "Publius", "Gnaeus", "Aulus", "Spurius" },
                "battania" => new[] { "Erwan", "Cormac", "Edern", "Cadell", "Aldric", "Brynn", "Dermot", "Finnian", "Galen", "Kevan" },
                "sturgia" => new[] { "Erik", "Bjorn", "Harald", "Ragnar", "Olaf", "Ingvar", "Sigurd", "Thorkell", "Ulf", "Varg" },
                "vlandia" => new[] { "Baldwin", "Conrad", "Dietrich", "Edmund", "Gerard", "Hugo", "Lambert", "Raymond", "Wolfram", "Aldric" },
                "khuzait" => new[] { "Sübedei", "Jebe", "Temüjin", "Borte", "Kublai", "Möngke", "Ögedei", "Chagatai", "Tolui", "Batu" },
                "aserai" => new[] { "Rashid", "Tariq", "Farid", "Khalid", "Samir", "Jamil", "Nasir", "Omar", "Yusuf", "Zafir" },
                _ => new[] { "Marcus", "Erik", "Baldwin", "Erwan", "Rashid" }
            };
            return names[random.Next(names.Length)];
        }

        private string GetRandomFemaleName(string culture, Random random)
        {
            string[] names = culture.ToLowerInvariant() switch
            {
                "empire" => new[] { "Livia", "Julia", "Claudia", "Cornelia", "Valeria", "Aurelia", "Flavia", "Lucilla", "Octavia", "Servilia" },
                "battania" => new[] { "Aisling", "Brigid", "Caoimhe", "Deirdre", "Eithne", "Fiona", "Grainne", "Niamh", "Orlaith", "Siobhan" },
                "sturgia" => new[] { "Astrid", "Freya", "Helga", "Ingrid", "Sigrid", "Thora", "Yrsa", "Gudrun", "Ragnhild", "Svanhild" },
                "vlandia" => new[] { "Adelaide", "Beatrice", "Constance", "Eleanor", "Matilda", "Philippa", "Rosamund", "Sybilla", "Yvonne", "Adelais" },
                "khuzait" => new[] { "Altani", "Borte", "Khulan", "Sorghaghtani", "Toregene", "Yisui", "Oghul", "Alaqai", "Checheyigen", "Ibaqa" },
                "aserai" => new[] { "Fatima", "Layla", "Nadia", "Rashida", "Safiya", "Yasmin", "Zahra", "Amira", "Jamila", "Leila" },
                _ => new[] { "Julia", "Astrid", "Adelaide", "Brigid", "Fatima" }
            };
            return names[random.Next(names.Length)];
        }

        private string GetRandomEpithet(string culture, Random random)
        {
            string[] epithets = 
            {
                "Ironarm", "One-Eye", "Bloodaxe", "Fairhair", "the Grim", "the Bold", "the Steady",
                "Wolfborn", "Scarface", "the Old", "the Young", "Blackbeard", "Redhands", "the Wise",
                "Stoneheart", "Ironfist", "the Silent", "Arrow-Eye", "the Swift", "the Patient"
            };
            return epithets[random.Next(epithets.Length)];
        }

        private string GetRankTitle(string culture)
        {
            return culture.ToLowerInvariant() switch
            {
                "empire" => "Decanus",
                "battania" => "Cennaire",
                "sturgia" => "Desyatnik",
                "vlandia" => "Sergeant",
                "khuzait" => "Arban-u Darga",
                "aserai" => "Arif",
                _ => "Sergeant"
            };
        }

        private string GetRandomRole(string culture, Random random)
        {
            string[] roles = { "infantry", "cavalry", "archer", "scout", "engineer", "veteran" };
            return roles[random.Next(roles.Length)];
        }

        private SecondaryTrait[] GetCompatibleSecondaryTraits(PersonalityTrait primary)
        {
            return primary switch
            {
                PersonalityTrait.Stern => new[] { SecondaryTrait.Honorable, SecondaryTrait.Brutal, SecondaryTrait.Cautious, SecondaryTrait.Vengeful },
                PersonalityTrait.Fair => new[] { SecondaryTrait.Honorable, SecondaryTrait.Wise, SecondaryTrait.Inspiring, SecondaryTrait.Protective },
                PersonalityTrait.Pragmatic => new[] { SecondaryTrait.Cunning, SecondaryTrait.Cautious, SecondaryTrait.Inspiring },
                PersonalityTrait.Fatherly => new[] { SecondaryTrait.Protective, SecondaryTrait.Wise, SecondaryTrait.Inspiring },
                PersonalityTrait.Ambitious => new[] { SecondaryTrait.Cunning, SecondaryTrait.Inspiring, SecondaryTrait.Brutal },
                PersonalityTrait.Cynical => new[] { SecondaryTrait.Cunning, SecondaryTrait.Wise, SecondaryTrait.Vengeful },
                _ => new[] { SecondaryTrait.Honorable, SecondaryTrait.Wise }
            };
        }
    }
}

