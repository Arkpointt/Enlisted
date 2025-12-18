using System;
using System.Collections.Generic;
using System.Linq;
using Enlisted.Features.Assignments.Core;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Features.Escalation;
using Enlisted.Features.Ranks;
using Enlisted.Mod.Core;
using Enlisted.Mod.Core.Logging;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace Enlisted.Features.Assignments.Behaviors
{
    /// <summary>
    ///     Phase 7: Result of a duty change request.
    /// </summary>
    public sealed class DutyRequestResult
    {
        public bool Approved { get; set; }
        public string Reason { get; set; }
        public string DutyId { get; set; }
    }

    /// <summary>
    ///     Modern configuration-driven duties system with formation specializations and officer role integration.
    ///     This system provides meaningful military assignments that affect both player progression
    ///     and party effectiveness. Officer duties integrate with the party's effective roles,
    ///     while basic duties provide skill training and experience.
    /// </summary>
    public sealed class EnlistedDutiesBehavior : CampaignBehaviorBase
    {
        private List<string> _activeDuties = new();

        private DutiesSystemConfig _config;
        private Dictionary<string, CampaignTime> _dutyStartTimes = new();

        // Officer role state for Harmony patches (optional enhancement)
        private bool _officerRolesEnabled = true;
        private string _playerFormation;
        private int _saveVersion = 1;

        /// <summary>
        ///     Phase 7: Last time the player requested a duty change. Used for cooldown.
        /// </summary>
        private CampaignTime _lastDutyChangeRequest = CampaignTime.Zero;

        /// <summary>
        ///     Phase 7: Cooldown period between duty change requests (in days).
        /// </summary>
        private const int DutyChangeCooldownDays = 14;

        /// <summary>
        ///     Phase 7: Minimum lance reputation required to request duty change.
        /// </summary>
        private const int MinLanceReputationForDutyChange = 10;

        public EnlistedDutiesBehavior()
        {
            Instance = this;
        }

        public static EnlistedDutiesBehavior Instance { get; private set; }

        public bool IsInitialized => _config != null;
        public List<string> ActiveDuties => [.. _activeDuties];
        public string PlayerFormation => _playerFormation;

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
        }

        public override void SyncData(IDataStore dataStore)
        {
            SaveLoadDiagnostics.SafeSyncData(this, dataStore, () =>
            {
                dataStore.SyncData("_saveVersion", ref _saveVersion);
                dataStore.SyncData("_activeDuties", ref _activeDuties);
                dataStore.SyncData("_playerFormation", ref _playerFormation);
                dataStore.SyncData("_dutyStartTimes", ref _dutyStartTimes);
                dataStore.SyncData("_officerRolesEnabled", ref _officerRolesEnabled);

                // Phase 7: Duty request cooldown tracking
                dataStore.SyncData("_lastDutyChangeRequest", ref _lastDutyChangeRequest);

                // Initialize config after loading if not already done
                if (dataStore.IsLoading && _config == null)
                {
                    InitializeConfig();
                }

                // Phase 7: Migration for existing saves - assign starter duty if none exists
                if (dataStore.IsLoading)
                {
                    MigratePhase7Data();
                }
            });
        }
        
        /// <summary>
        ///     Phase 7: Migrate existing save data to new Phase 7 structures.
        ///     - Assigns starter duty if player is T2+ with no duties
        ///     - Detects formation from equipment if not set
        /// </summary>
        private void MigratePhase7Data()
        {
            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment == null || !enlistment.IsEnlisted)
            {
                return;
            }
            
            try
            {
                // If player has no formation, detect from equipment or troop selection
                if (string.IsNullOrEmpty(_playerFormation))
                {
                    // Try to derive from existing troop selection
                    var troopId = Equipment.Behaviors.TroopSelectionManager.Instance?.LastSelectedTroopId;
                    if (!string.IsNullOrEmpty(troopId))
                    {
                        var troop = TaleWorlds.ObjectSystem.MBObjectManager.Instance?.GetObject<TaleWorlds.CampaignSystem.CharacterObject>(troopId);
                        if (troop != null)
                        {
                            _playerFormation = DeriveFormationFromTroop(troop);
                            ModLogger.Info("Duties", $"Migration: Derived formation '{_playerFormation}' from troop '{troopId}'");
                        }
                    }
                    
                    // Fallback to equipment detection
                    if (string.IsNullOrEmpty(_playerFormation))
                    {
                        _playerFormation = DetectPlayerFormation();
                        ModLogger.Info("Duties", $"Migration: Detected formation '{_playerFormation}' from equipment");
                    }
                }
                
                // If player is T2+ with no duties, assign starter duty for their formation
                if (enlistment.EnlistmentTier >= 2 && _activeDuties.Count == 0)
                {
                    var starterDuty = GetStarterDutyForFormation(_playerFormation ?? "infantry");
                    if (!string.IsNullOrEmpty(starterDuty))
                    {
                        _activeDuties.Add(starterDuty);
                        _dutyStartTimes[starterDuty] = CampaignTime.Now;
                        ModLogger.Info("Duties", $"Migration: Assigned starter duty '{starterDuty}' for formation '{_playerFormation}'");
                    }
                }
            }
            catch (Exception ex)
            {
                ModLogger.ErrorCode("Duties", "E-DUTIES-001", "Phase 7 migration failed", ex);
            }
        }
        
        /// <summary>
        ///     Phase 7: Derive formation from a troop's properties.
        /// </summary>
        private static string DeriveFormationFromTroop(TaleWorlds.CampaignSystem.CharacterObject troop)
        {
            if (troop == null) return "infantry";
            
            var isMounted = troop.IsMounted;
            var isRanged = troop.IsRanged;
            
            if (isMounted && isRanged) return "horsearcher";
            if (isMounted) return "cavalry";
            if (isRanged) return "archer";
            return "infantry";
        }
        
        /// <summary>
        ///     Phase 7: Get the default starter duty for a formation.
        /// </summary>
        public static string GetStarterDutyForFormation(string formation)
        {
            return formation?.ToLower() switch
            {
                "infantry" => "runner",
                "archer" => "lookout",
                "cavalry" => "messenger",
                "horsearcher" => "scout",
                "naval" => "boatswain",
                _ => "runner"
            };
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            InitializeConfig();
        }

        /// <summary>
        ///     Initialize duties system configuration with comprehensive error handling.
        /// </summary>
        private void InitializeConfig()
        {
            try
            {
                _config = ConfigurationManager.LoadDutiesConfig();

                if (!_config.Enabled)
                {
                    ModLogger.Info("Duties", "Duties system disabled by configuration");
                    return;
                }

                // Detect player formation if not set
                if (string.IsNullOrEmpty(_playerFormation) && EnlistmentBehavior.Instance?.IsEnlisted == true)
                {
                    _playerFormation = DetectPlayerFormation();
                    ModLogger.Info("Duties", $"Auto-detected player formation: {_playerFormation}");
                }

                ModLogger.Info("Duties", "Duties system initialized successfully");
            }
            catch (Exception ex)
            {
                ModLogger.ErrorCode("Duties", "E-DUTIES-002", "Failed to initialize duties system", ex);
                // Create a fallback configuration with default values
                _config = new DutiesSystemConfig { Enabled = false };
            }
        }

        /// <summary>
        ///     Detects the player's military formation (Infantry/Cavalry/Archer/Horse Archer) based on equipment.
        ///     Analyzes the player's equipped items to determine their military specialization,
        ///     which determines which skills receive daily XP from formation training.
        /// </summary>
        private string DetectPlayerFormation()
        {
            try
            {
                var hero = Hero.MainHero;
                var characterObject = hero?.CharacterObject;

                if (characterObject == null)
                {
                    return "infantry"; // Default fallback
                }

                // Use Bannerlord's built-in formation classification
                switch (characterObject.DefaultFormationClass)
                {
                    case FormationClass.Infantry:
                        return "infantry";
                    case FormationClass.Ranged:
                        return "archer";
                    case FormationClass.Cavalry:
                        return "cavalry";
                    case FormationClass.HorseArcher:
                        return "horsearcher";
                    case FormationClass.Unset:
                    default:
                        return "infantry"; // Default if unset
                }
            }
            catch (Exception ex)
            {
                ModLogger.ErrorCode("Duties", "E-DUTIES-003", "Error detecting player formation", ex);
                return "infantry";
            }
        }

        /// <summary>
        ///     Process daily duties: skill XP, wage bonuses, and officer role management.
        /// </summary>
        private void OnDailyTick()
        {
            if (!EnlistedActivation.EnsureActive())
            {
                return;
            }

            var enlistment = EnlistmentBehavior.Instance;
            if (!IsInitialized || !_config.Enabled || enlistment == null)
            {
                return;
            }

            // Must have a valid lord
            if (enlistment.CurrentLord == null || !enlistment.CurrentLord.IsAlive)
            {
                return;
            }

            try
            {
                // Process duties only when actively enlisted (not on leave)
                if (enlistment.IsEnlisted)
                {
                    ProcessDailyDuties();
                    UpdateOfficerRoles();
                }

                // Process formation training only while actively enlisted.
                //
                // Rationale:
                // - We want "time in the army" to feel productive, but we also want battles and player-initiated training
                //   (Camp Activities, training events, etc.) to matter over a long career.
                // - Continuing automatic daily training while on leave made skill growth feel overly passive.
                if (enlistment.IsEnlisted)
                {
                    ProcessFormationTraining();
                }
            }
            catch (Exception ex)
            {
                ModLogger.ErrorCode("Duties", "E-DUTIES-004", "Error during daily duties processing", ex);
            }
        }

        /// <summary>
        ///     Helper method to get duty definition from the Duties dictionary.
        /// </summary>
        private bool TryGetDuty(string dutyId, out DutyDefinition dutyDef)
        {
            dutyDef = null;

            if (_config.Duties != null && _config.Duties.TryGetValue(dutyId, out dutyDef))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        ///     Process daily benefits from active duties.
        /// </summary>
        private void ProcessDailyDuties()
        {
            // Intentionally operate directly on Hero.MainHero and EnlistmentBehavior.Instance where needed

            foreach (var dutyId in _activeDuties.ToList()) // ToList to prevent modification during iteration
            {
                // Fix: Check both Duties and Professions dictionaries
                if (!TryGetDuty(dutyId, out var dutyDef))
                {
                    ModLogger.LogOnce($"duties_unknown_active_{dutyId}", "Duties",
                        $"[E-DUTIES-005] Unknown duty/profession in active duties: {dutyId}", LogLevel.Error);
                    _activeDuties.Remove(dutyId);
                    continue;
                }

                // Apply multi-skill XP if specified (new system)
                if (dutyDef.MultiSkillXp is { Count: > 0 })
                {
                    var applied = new List<string>();
                    foreach (var skillEntry in dutyDef.MultiSkillXp)
                    {
                        var skill = GetSkillFromName(skillEntry.Key);
                        if (skill != null && skillEntry.Value > 0)
                        {
                            Hero.MainHero.AddSkillXp(skill, skillEntry.Value);
                            if (applied.Count < 6)
                            {
                                applied.Add($"{skillEntry.Key}+{skillEntry.Value}");
                            }
                        }
                    }

                    // Non-spammy: one line per duty, not one line per skill.
                    if (applied.Count > 0)
                    {
                        ModLogger.Debug("Duties", $"Applied daily duty XP ({dutyId}): {string.Join(", ", applied)}");
                    }
                }
                else if (dutyDef.SkillXpDaily > 0 && !string.IsNullOrEmpty(dutyDef.TargetSkill))
                {
                    // Handle legacy configuration format if present
                    var skill = GetSkillFromName(dutyDef.TargetSkill);
                    if (skill != null)
                    {
                        Hero.MainHero.AddSkillXp(skill, dutyDef.SkillXpDaily);
                        ModLogger.Debug("Duties", $"Applied daily duty XP ({dutyId}): {dutyDef.TargetSkill}+{dutyDef.SkillXpDaily}");
                    }
                }

                // Duties provide daily skill XP bonuses, not military tier progression XP
                // Military tier progression XP comes from daily service (25 XP/day) and battle participation (75 XP)
            }
        }

        /// <summary>
        ///     Process formation-based daily skill training.
        ///     Provides automatic skill XP based on the player's military formation assignment.
        /// </summary>
        private void ProcessFormationTraining()
        {
            if (!_config.FormationTraining.Enabled)
            {
                return;
            }

            try
            {
                var playerFormation = GetPlayerFormationType();

                if (!_config.FormationTraining.Formations.TryGetValue(playerFormation, out var formationConfig))
                {
                    ModLogger.LogOnce($"duties_no_formation_training_{playerFormation}", "Duties",
                        $"[E-DUTIES-006] No formation training configuration for: {playerFormation}", LogLevel.Error);
                    return;
                }

                foreach (var skillEntry in formationConfig.Skills)
                {
                    var skillName = skillEntry.Key;
                    var xpAmount = skillEntry.Value;

                    var skill = GetSkillFromName(skillName);
                    if (skill != null)
                    {
                        // Apply skill XP bonuses from the duty/profession to the player's skills
                        // This provides daily skill training based on the player's military assignment
                        Hero.MainHero.AddSkillXp(skill, xpAmount);
                    }
                    else
                    {
                        ModLogger.LogOnce($"duties_unknown_training_skill_{skillName}", "Duties",
                            $"[E-DUTIES-007] Unknown skill in formation training: {skillName}", LogLevel.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                ModLogger.ErrorCode("Duties", "E-DUTIES-008", "Error processing formation training", ex);
            }
        }


        /// <summary>
        ///     Update officer roles based on active duties.
        ///     This manages both public API assignments and prepares data for optional Harmony patches.
        /// </summary>
        private void UpdateOfficerRoles()
        {
            if (!_officerRolesEnabled || EnlistmentBehavior.Instance?.CurrentLord?.PartyBelongedTo == null)
            {
                return;
            }

            var lordParty = EnlistmentBehavior.Instance.CurrentLord.PartyBelongedTo;
            var playerHero = Hero.MainHero;

            // Public API approach: Direct officer assignment
            AssignOfficerRolesViaPublicApi(lordParty, playerHero);
        }

        /// <summary>
        ///     Simple and reliable, but player shows as 'official' officer.
        /// </summary>
        private void AssignOfficerRolesViaPublicApi(MobileParty lordParty, Hero playerHero)
        {
            try
            {
                // Clear current assignments first
                // Previous assignments are cleared when assigning new officer roles

                // Assign based on active duties
                foreach (var dutyId in _activeDuties)
                {
                    // Fix: Check both Duties and Professions dictionaries
                    if (!TryGetDuty(dutyId, out var dutyDef) ||
                        string.IsNullOrEmpty(dutyDef.OfficerRole))
                    {
                        continue;
                    }

                    switch (dutyDef.OfficerRole.ToLower())
                    {
                        case "engineer":
                            lordParty.SetPartyEngineer(playerHero);
                            break;
                        case "scout":
                            lordParty.SetPartyScout(playerHero);
                            break;
                        case "quartermaster":
                            lordParty.SetPartyQuartermaster(playerHero);
                            break;
                        case "surgeon":
                            lordParty.SetPartySurgeon(playerHero);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                ModLogger.ErrorCode("Duties", "E-DUTIES-009", "Error assigning officer roles via public API", ex);
            }
        }

        /// <summary>
        ///     Helper method for Harmony patches to check if the player has active duty with a specific officer role.
        ///     Assigns officer roles to the player based on their active duties/professions.
        ///     This method provides a public API that can be used by other systems to assign
        ///     officer roles (Scout, Quartermaster, etc.) based on the player's current assignments.
        /// </summary>
        public bool HasActiveDutyWithRole(string officerRole)
        {
            if (!IsInitialized || !_officerRolesEnabled)
            {
                return false;
            }

            return _activeDuties.Any(dutyId =>
                TryGetDuty(dutyId, out var dutyDef) &&
                string.Equals(dutyDef.OfficerRole, officerRole, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Phase 4.5: Public API for event systems to check whether a specific duty ID is active.
        /// Uses the persisted ID list (strings) for save compatibility.
        /// </summary>
        public bool HasActiveDuty(string dutyId)
        {
            if (!IsInitialized || string.IsNullOrWhiteSpace(dutyId))
            {
                return false;
            }

            return _activeDuties.Any(d => string.Equals(d, dutyId.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        ///     Assign duty to the player if requirements are met.
        /// </summary>
        public bool AssignDuty(string dutyId)
        {
            if (!IsInitialized || !_config.Enabled)
            {
                ModLogger.LogOnce("duties_not_initialized", "Duties",
                    "[E-DUTIES-010] Duties system not initialized", LogLevel.Error);
                return false;
            }

            // Fix: Check both Duties and Professions dictionaries
            if (!TryGetDuty(dutyId, out var dutyDef))
            {
                ModLogger.LogOnce($"duties_unknown_assign_{dutyId}", "Duties",
                    $"[E-DUTIES-011] Unknown duty/profession: {dutyId}", LogLevel.Error);
                return false;
            }

            // Check if the player meets requirements
            if (!CanPlayerPerformDuty(dutyDef))
            {
                return false;
            }

            // Check duty slot limits
            var currentTier = EnlistmentBehavior.Instance?.EnlistmentTier ?? 1;
            var maxSlots = GetDutySlotsForTier(currentTier);

            if (_activeDuties.Count >= maxSlots)
            {
                ModLogger.Info("Duties",
                    $"Cannot assign duty {dutyId}: duty slot limit reached ({_activeDuties.Count}/{maxSlots})");
                return false;
            }

            // Add duty
            _activeDuties.Add(dutyId);
            _dutyStartTimes[dutyId] = CampaignTime.Now;

            ModLogger.Info("Duties", $"Assigned duty: {dutyDef.DisplayName}");
            return true;
        }

        /// <summary>
        ///     Phase 7: Request a duty change, subject to approval.
        ///     This replaces direct duty selection for T2+ players.
        /// </summary>
        /// <param name="newDutyId">The duty to request transfer to.</param>
        /// <returns>Result containing approval status and reason.</returns>
        public DutyRequestResult RequestDutyChange(string newDutyId)
        {
            var enlistment = EnlistmentBehavior.Instance;
            if (!IsInitialized || !_config.Enabled || enlistment?.IsEnlisted != true)
            {
                return new DutyRequestResult
                {
                    Approved = false,
                    Reason = "Duty system is not available.",
                    DutyId = newDutyId
                };
            }

            // Get duty definition
            if (!TryGetDuty(newDutyId, out var dutyDef))
            {
                return new DutyRequestResult
                {
                    Approved = false,
                    Reason = "Unknown duty.",
                    DutyId = newDutyId
                };
            }

            // Check if already assigned to this duty
            if (_activeDuties.Contains(newDutyId))
            {
                return new DutyRequestResult
                {
                    Approved = false,
                    Reason = $"You are already assigned to {dutyDef.DisplayName}.",
                    DutyId = newDutyId
                };
            }

            // Check cooldown (14 days between requests)
            if (_lastDutyChangeRequest != CampaignTime.Zero)
            {
                var daysSinceLastRequest = (CampaignTime.Now - _lastDutyChangeRequest).ToDays;
                if (daysSinceLastRequest < DutyChangeCooldownDays)
                {
                    var daysRemaining = DutyChangeCooldownDays - (int)daysSinceLastRequest;
                    return new DutyRequestResult
                    {
                        Approved = false,
                        Reason = $"You must wait {daysRemaining} more days before requesting another duty change.",
                        DutyId = newDutyId
                    };
                }
            }

            // Check lance reputation (require at least 10)
            var escalation = EscalationManager.Instance;
            if (escalation?.IsEnabled() == true)
            {
                var lanceRep = escalation.State?.LanceReputation ?? 0;
                if (lanceRep < MinLanceReputationForDutyChange)
                {
                    // Get lance leader name for personalized message
                    var lanceLeaderName = Lances.Text.LanceLifeTextVariables.GetLanceLeaderShortName(enlistment);
                    return new DutyRequestResult
                    {
                        Approved = false,
                        Reason = $"{lanceLeaderName} doesn't think you've earned a transfer yet. (Lance reputation: {lanceRep}/{MinLanceReputationForDutyChange})",
                        DutyId = newDutyId
                    };
                }
            }

            // Check tier requirements
            var currentTier = enlistment.EnlistmentTier;
            var requiredTier = dutyDef.MinTier > 0 ? dutyDef.MinTier : 1;
            if (currentTier < requiredTier)
            {
                var cultureId = enlistment.EnlistedLord?.Culture?.StringId ?? "mercenary";
                var requiredRank = RankHelper.GetRankTitle(requiredTier, cultureId);
                return new DutyRequestResult
                {
                    Approved = false,
                    Reason = $"{dutyDef.DisplayName} requires rank {requiredRank} or higher.",
                    DutyId = newDutyId
                };
            }

            // Check formation requirements
            if (!CanPlayerPerformDuty(dutyDef))
            {
                return new DutyRequestResult
                {
                    Approved = false,
                    Reason = $"You do not meet the requirements for {dutyDef.DisplayName}.",
                    DutyId = newDutyId
                };
            }

            // Check duty slot limits
            var maxSlots = GetDutySlotsForTier(currentTier);
            if (_activeDuties.Count >= maxSlots)
            {
                // Need to remove a duty first - for now, just inform the player
                return new DutyRequestResult
                {
                    Approved = false,
                    Reason = $"You have no open duty slots. ({_activeDuties.Count}/{maxSlots})",
                    DutyId = newDutyId
                };
            }

            // Approved! Record the request time and assign the duty
            _lastDutyChangeRequest = CampaignTime.Now;
            _activeDuties.Add(newDutyId);
            _dutyStartTimes[newDutyId] = CampaignTime.Now;

            var lanceLeader = Lances.Text.LanceLifeTextVariables.GetLanceLeaderShortName(enlistment);
            ModLogger.Info("Duties", $"Duty request approved: {dutyDef.DisplayName}");

            return new DutyRequestResult
            {
                Approved = true,
                Reason = $"{lanceLeader} approves your transfer to {dutyDef.DisplayName}.",
                DutyId = newDutyId
            };
        }

        /// <summary>
        ///     Phase 7: Check if duty request is on cooldown.
        /// </summary>
        public bool IsDutyRequestOnCooldown()
        {
            if (_lastDutyChangeRequest == CampaignTime.Zero)
            {
                return false;
            }
            var daysSinceLastRequest = (CampaignTime.Now - _lastDutyChangeRequest).ToDays;
            return daysSinceLastRequest < DutyChangeCooldownDays;
        }

        /// <summary>
        ///     Phase 7: Get days remaining on duty request cooldown.
        /// </summary>
        public int GetDutyRequestCooldownRemaining()
        {
            if (_lastDutyChangeRequest == CampaignTime.Zero)
            {
                return 0;
            }
            var daysSinceLastRequest = (int)(CampaignTime.Now - _lastDutyChangeRequest).ToDays;
            return Math.Max(0, DutyChangeCooldownDays - daysSinceLastRequest);
        }

        /// <summary>
        ///     Remove duty from the player.
        /// </summary>
        public bool RemoveDuty(string dutyId)
        {
            if (_activeDuties.Remove(dutyId))
            {
                _dutyStartTimes.Remove(dutyId);
                ModLogger.Info("Duties", $"Removed duty: {dutyId}");

                // Update officer roles after removing duty
                if (EnlistmentBehavior.Instance?.IsEnlisted == true)
                {
                    UpdateOfficerRoles();
                }

                return true;
            }

            return false;
        }

        /// <summary>
        ///     Get available duties for the current player state.
        /// </summary>
        public List<DutyDefinition> GetAvailableDuties()
        {
            if (!IsInitialized)
            {
                return new List<DutyDefinition>();
            }

            var currentTier = EnlistmentBehavior.Instance?.EnlistmentTier ?? 1;
            var availableDuties = new List<DutyDefinition>();

            foreach (var duty in _config.Duties.Values)
            {
                if (duty.MinTier <= currentTier &&
                    CanPlayerPerformDuty(duty) &&
                    !_activeDuties.Contains(duty.Id))
                {
                    availableDuties.Add(duty);
                }
            }

            return availableDuties;
        }

        /// <summary>
        ///     Get all duties from config, respecting expansion requirements.
        ///     Returns all duties regardless of formation - UI should grey out incompatible ones.
        /// </summary>
        public List<DutyDefinition> GetAllDuties()
        {
            if (!IsInitialized || _config?.Duties == null)
            {
                return new List<DutyDefinition>();
            }

            var result = new List<DutyDefinition>();

            foreach (var duty in _config.Duties.Values)
            {
                // Check expansion requirement (e.g., war_sails for naval duties)
                if (!string.IsNullOrEmpty(duty.RequiresExpansion))
                {
                    if (!IsExpansionActive(duty.RequiresExpansion))
                    {
                        continue; // Expansion not active
                    }
                }

                result.Add(duty);
            }

            return result;
        }

        /// <summary>
        ///     Check if a duty is compatible with the player's current formation.
        /// </summary>
        public bool IsDutyCompatibleWithFormation(DutyDefinition duty)
        {
            if (duty == null)
            {
                return false;
            }

            // If no formation requirements, duty is compatible with all formations
            if (duty.RequiredFormations == null || duty.RequiredFormations.Count == 0)
            {
                return true;
            }

            var formation = GetPlayerFormationType();
            return duty.RequiredFormations.Any(f => 
                string.Equals(f, formation, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        ///     Get all duties from config that are valid for the player's current formation.
        ///     Includes tier-locked duties (for graying out) and respects expansion requirements.
        ///     DEPRECATED: Use GetAllDuties() and IsDutyCompatibleWithFormation() instead.
        /// </summary>
        public List<DutyDefinition> GetDutiesForCurrentFormation()
        {
            if (!IsInitialized || _config?.Duties == null)
            {
                return new List<DutyDefinition>();
            }

            var formation = GetPlayerFormationType();
            var result = new List<DutyDefinition>();

            foreach (var duty in _config.Duties.Values)
            {
                // Check formation requirement
                if (duty.RequiredFormations != null && duty.RequiredFormations.Count > 0)
                {
                    if (!duty.RequiredFormations.Any(f => 
                        string.Equals(f, formation, StringComparison.OrdinalIgnoreCase)))
                    {
                        continue; // Not available for this formation
                    }
                }

                // Check expansion requirement (e.g., war_sails for naval duties)
                if (!string.IsNullOrEmpty(duty.RequiresExpansion))
                {
                    if (!IsExpansionActive(duty.RequiresExpansion))
                    {
                        continue; // Expansion not active
                    }
                }

                result.Add(duty);
            }

            return result;
        }

        /// <summary>
        ///     Check if a specific expansion/DLC is active.
        /// </summary>
        public bool IsExpansionActive(string expansionId)
        {
            if (string.IsNullOrWhiteSpace(expansionId))
            {
                return true; // No expansion required
            }

            // Check for War Sails / Naval War DLC
            if (string.Equals(expansionId, "war_sails", StringComparison.OrdinalIgnoreCase))
            {
                return IsWarSailsActive();
            }

            return false; // Unknown expansion
        }

        /// <summary>
        ///     Check if War Sails (Naval War) expansion is active.
        /// </summary>
        private bool IsWarSailsActive()
        {
            try
            {
                // Check if any Naval War related submodules are loaded
                var subModules = TaleWorlds.MountAndBlade.Module.CurrentModule?.CollectSubModules();
                if (subModules == null || subModules.Count == 0)
                {
                    return false;
                }

                var active = subModules.Any(m =>
                {
                    var name = m.GetType().FullName ?? "";
                    return name.IndexOf("NavalWar", StringComparison.OrdinalIgnoreCase) >= 0 ||
                           name.IndexOf("WarSails", StringComparison.OrdinalIgnoreCase) >= 0;
                });

                if (!active)
                {
                    // Non-spammy: only once per session, and only when we actually check for War Sails.
                    ModLogger.WarnCodeOnce("warsails_missing_duties_behavior_check", "Duties", "W-DLC-002",
                        "War Sails (NavalDLC) not detected; naval duties/features will be disabled.");
                }

                return active;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        ///     Check if a duty can be selected (meets tier and other requirements).
        /// </summary>
        public bool IsDutySelectableByPlayer(DutyDefinition duty)
        {
            if (duty == null || !IsInitialized)
            {
                return false;
            }

            var currentTier = EnlistmentBehavior.Instance?.EnlistmentTier ?? 1;
            
            // Check tier requirement
            if (duty.MinTier > currentTier)
            {
                return false;
            }

            return CanPlayerPerformDuty(duty);
        }

        /// <summary>
        ///     Get a duty definition by ID.
        /// </summary>
        public DutyDefinition GetDutyById(string dutyId)
        {
            if (!IsInitialized || string.IsNullOrWhiteSpace(dutyId))
            {
                return null;
            }

            if (TryGetDuty(dutyId, out var duty))
            {
                return duty;
            }

            return null;
        }

        /// <summary>
        ///     Check if the player can perform a specific duty.
        /// </summary>
        private bool CanPlayerPerformDuty(DutyDefinition duty)
        {
            // Check tier requirement
            var currentTier = EnlistmentBehavior.Instance?.EnlistmentTier ?? 1;
            if (duty.MinTier > currentTier)
            {
                return false;
            }

            // Check formation requirement
            if (duty.RequiredFormations.Count > 0 &&
                !string.IsNullOrEmpty(_playerFormation) &&
                !duty.RequiredFormations.Contains(_playerFormation))
            {
                return false;
            }

            // Check relationship requirement
            var lordRelation = EnlistmentBehavior.Instance?.CurrentLord != null
                ? Hero.MainHero.GetRelation(EnlistmentBehavior.Instance.CurrentLord)
                : 0;

            if (duty.UnlockConditions.RelationshipRequired > lordRelation)
            {
                return false;
            }

            // Check skill requirement
            if (duty.UnlockConditions.SkillRequired > 0 && !string.IsNullOrEmpty(duty.TargetSkill))
            {
                var skill = GetSkillFromName(duty.TargetSkill);
                if (skill != null && Hero.MainHero.GetSkillValue(skill) < duty.UnlockConditions.SkillRequired)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        ///     Get the number of duty slots available for a tier.
        /// </summary>
        private int GetDutySlotsForTier(int tier)
        {
            var key = $"tier_{tier}";
            return _config.DutySlots.TryGetValue(key, out var slots) ? slots : 1;
        }

        /// <summary>
        ///     Convert skill name string to SkillObject.
        /// </summary>
        private SkillObject GetSkillFromName(string skillName)
        {
            try
            {
                return MBObjectManager.Instance.GetObject<SkillObject>(skillName);
            }
            catch
            {
                ModLogger.LogOnce($"duties_unknown_skill_{skillName}", "Duties",
                    $"[E-DUTIES-012] Unknown skill: {skillName}", LogLevel.Error);
                return null;
            }
        }

        /// <summary>
        ///     Set player formation and update available duties.
        ///     Called when the player chooses formation at Tier 2.
        /// </summary>
        public void SetPlayerFormation(string formation)
        {
            _playerFormation = formation;
            ModLogger.Info("Duties", $"Player formation set to: {formation}");

            // Remove any duties that are no longer compatible
            // Fix: Check both Duties and Professions dictionaries
            var incompatibleDuties = _activeDuties.Where(dutyId =>
                TryGetDuty(dutyId, out var duty) &&
                duty.RequiredFormations.Count > 0 &&
                !duty.RequiredFormations.Contains(formation)).ToList();

            foreach (var dutyId in incompatibleDuties)
            {
                RemoveDuty(dutyId);
                ModLogger.Info("Duties", $"Removed incompatible duty: {dutyId} (formation changed to {formation})");
            }
        }

        /// <summary>
        ///     Calculate wage multiplier from active duties.
        ///     Calculates the combined wage multiplier from all active duties and professions.
        ///     Different duties and professions provide different wage bonuses, allowing players
        ///     to earn more gold per day based on their assignments.
        /// </summary>
        public float GetWageMultiplierForActiveDuties()
        {
            if (!IsInitialized || _activeDuties.Count == 0)
            {
                return 1.0f;
            }

            try
            {
                // Calculate average wage multiplier from active duties
                var totalMultiplier = 0f;
                var validDuties = 0;

                foreach (var dutyId in _activeDuties)
                {
                    // Fix: Check both Duties and Professions dictionaries
                    if (TryGetDuty(dutyId, out var dutyDef))
                    {
                        totalMultiplier += dutyDef.WageMultiplier;
                        validDuties++;
                    }
                }

                return validDuties > 0 ? totalMultiplier / validDuties : 1.0f;
            }
            catch (Exception ex)
            {
                ModLogger.ErrorCode("Duties", "E-DUTIES-013", "Error calculating wage multiplier", ex);
                return 1.0f;
            }
        }

        /// <summary>
        ///     Get formation display name for the current culture.
        ///     Used for UI display in formation selection.
        /// </summary>
        private string GetFormationDisplayName(string formation)
        {
            // This would integrate with enlisted_config.json formations data
            // Return the formation name with proper casing for display
            return formation switch
            {
                "infantry" => "Infantry",
                "archer" => "Archer",
                "cavalry" => "Cavalry",
                "horsearcher" => "Horse Archer",
                _ => formation
            };
        }

        #region Menu Support Methods

        /// <summary>
        ///     Get formatted display text for active duties.
        /// </summary>
        public string GetActiveDutiesDisplay()
        {
            if (_activeDuties.Count == 0)
            {
                return "None assigned";
            }

            // Fix: Check both Duties and Professions dictionaries
            var duties =
                _activeDuties.Select(id => TryGetDuty(id, out var dutyDef) ? dutyDef.DisplayName : id);
            var maxSlots = GetMaxDutySlots();

            return $"{string.Join(", ", duties)} ({_activeDuties.Count}/{maxSlots})";
        }

        /// <summary>
        ///     Get the current officer role for menu display.
        /// </summary>
        public string GetCurrentOfficerRole()
        {
            foreach (var dutyId in _activeDuties)
            {
                // Fix: Check both Duties and Professions dictionaries
                if (TryGetDuty(dutyId, out var dutyDef) && !string.IsNullOrEmpty(dutyDef.OfficerRole))
                {
                    return dutyDef.OfficerRole;
                }
            }

            return "";
        }

        /// <summary>
        ///     Get the player's formation type for menu display.
        ///     Based on the chosen troop type, not current equipment.
        /// </summary>
        public string GetPlayerFormationType()
        {
            return _playerFormation?.ToLower() ?? "infantry";
        }

        /// <summary>
        ///     Get current wage multiplier from active duties.
        /// </summary>
        public float GetCurrentWageMultiplier()
        {
            var multiplier = 1.0f;

            foreach (var dutyId in _activeDuties)
            {
                if (_config?.Duties.ContainsKey(dutyId) == true)
                {
                    multiplier *= _config.Duties[dutyId].WageMultiplier;
                }
            }

            return multiplier;
        }


        /// <summary>
        ///     Get maximum duty slots based on tier.
        /// </summary>
        private int GetMaxDutySlots()
        {
            var tier = EnlistmentBehavior.Instance?.EnlistmentTier ?? 1;
            return tier switch
            {
                >= 5 => 3, // Senior tiers get 3 slots
                >= 3 => 2, // Mid tiers get 2 slots
                _ => 1 // Junior tiers get 1 slot
            };
        }

        /// <summary>
        ///     Get current formation training information for menu display.
        /// </summary>
        public string GetFormationTrainingDisplay()
        {
            if (!(_config?.FormationTraining?.Enabled ?? false))
            {
                return "Formation training disabled";
            }

            try
            {
                var playerFormation = GetPlayerFormationType();
                var formationDisplay = GetFormationDisplayName(playerFormation);

                var formations = _config?.FormationTraining?.Formations;
                if (formations != null && formations.TryGetValue(playerFormation, out var formationConfig))
                {
                    var skillList = formationConfig.Skills.Select(kv => $"{kv.Key} (+{kv.Value})").ToArray();
                    return $"{formationDisplay}: {string.Join(", ", skillList)}";
                }

                return $"{formationDisplay}: No training configured";
            }
            catch (Exception ex)
            {
                ModLogger.ErrorCode("Duties", "E-DUTIES-014", "Error getting formation training display", ex);
                return "Error loading formation training";
            }
        }

        /// <summary>
        ///     Get formation training status for the current player.
        /// </summary>
        public string GetCurrentFormation()
        {
            try
            {
                var playerFormation = GetPlayerFormationType();
                return GetFormationDisplayName(playerFormation);
            }
            catch
            {
                return "Unknown";
            }
        }

        /// <summary>
        ///     Get skill XP configuration for a specific formation.
        ///     Used by the menu system for dynamic highlighting.
        /// </summary>
        public Dictionary<string, int> GetFormationSkillXp(string formation)
        {
            try
            {
                if (_config?.FormationTraining?.Formations.TryGetValue(formation, out var formationConfig) == true)
                {
                    return formationConfig.Skills;
                }
            }
            catch (Exception ex)
            {
                ModLogger.ErrorCode("Duties", "E-DUTIES-015", "Error getting formation skill XP configuration", ex);
            }

            return new Dictionary<string, int>();
        }

        #endregion
    }
}
