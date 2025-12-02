using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;
using Enlisted.Features.Assignments.Core;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Mod.Core.Logging;

namespace Enlisted.Features.Assignments.Behaviors
{
    /// <summary>
    /// Modern configuration-driven duties system with formation specializations and officer role integration.
    ///
    /// This system provides meaningful military assignments that affect both player progression
    /// and party effectiveness. Officer duties integrate with the party's effective roles,
    /// while basic duties provide skill training and experience.
    /// </summary>
    public sealed class EnlistedDutiesBehavior : CampaignBehaviorBase
    {
        public static EnlistedDutiesBehavior Instance { get; private set; }

        private DutiesSystemConfig _config;
        private List<string> _activeDuties = new List<string>();
        private string _playerFormation;
        private Dictionary<string, CampaignTime> _dutyStartTimes = new Dictionary<string, CampaignTime>();
        private int _saveVersion = 1;

        // Officer role state for Harmony patches (optional enhancement)
        private bool _officerRolesEnabled = true;

        public bool IsInitialized => _config != null;
        public List<string> ActiveDuties => [.. _activeDuties];
        public string PlayerFormation => _playerFormation;

        public EnlistedDutiesBehavior()
        {
            Instance = this;
        }

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("_saveVersion", ref _saveVersion);
            dataStore.SyncData("_activeDuties", ref _activeDuties);
            dataStore.SyncData("_playerFormation", ref _playerFormation);
            dataStore.SyncData("_dutyStartTimes", ref _dutyStartTimes);
            dataStore.SyncData("_officerRolesEnabled", ref _officerRolesEnabled);

            // Initialize config after loading if not already done
            if (dataStore.IsLoading && _config == null)
            {
                InitializeConfig();
            }
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            InitializeConfig();
        }

        /// <summary>
        /// Initialize duties system configuration with comprehensive error handling.
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
                ModLogger.Error("Duties", "Failed to initialize duties system", ex);
                // Create a fallback configuration with default values
                _config = new DutiesSystemConfig { Enabled = false };
            }
        }

        /// <summary>
        /// Detects the player's military formation (Infantry/Cavalry/Archer/Horse Archer) based on equipment.
        /// Analyzes the player's equipped items to determine their military specialization,
        /// which determines which skills receive daily XP from formation training.
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
                ModLogger.Error("Duties", "Error detecting player formation", ex);
                return "infantry";
            }
        }

        /// <summary>
        /// Process daily duties: skill XP, wage bonuses, and officer role management.
        /// </summary>
        private void OnDailyTick()
        {
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

                // Process formation training for both enlisted and leave status
                // (Military training continues even during temporary leave)
                if (enlistment.IsEnlisted || enlistment.IsOnLeave)
                {
                    ProcessFormationTraining();
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("Duties", "Error during daily duties processing", ex);
            }
        }

        /// <summary>
        /// Helper method to get duty/profession definition from either Duties or Professions dictionaries.
        /// Fix: Professions are stored separately but need to be processed the same way as duties.
        /// </summary>
        private bool TryGetDutyOrProfession(string dutyId, out DutyDefinition dutyDef)
        {
            dutyDef = null;

            // Check Duties first (most common)
            if (_config.Duties != null && _config.Duties.TryGetValue(dutyId, out dutyDef))
            {
                return true;
            }

            // Check Professions if not found in Duties
            if (_config.Professions != null && _config.Professions.TryGetValue(dutyId, out dutyDef))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Process daily benefits from active duties.
        /// </summary>
        private void ProcessDailyDuties()
        {
            // Intentionally operate directly on Hero.MainHero and EnlistmentBehavior.Instance where needed

            foreach (var dutyId in _activeDuties.ToList()) // ToList to prevent modification during iteration
            {
                // Fix: Check both Duties and Professions dictionaries
                if (!TryGetDutyOrProfession(dutyId, out var dutyDef))
                {
                    ModLogger.Error("Duties", $"Unknown duty/profession in active duties: {dutyId}");
                    _activeDuties.Remove(dutyId);
                    continue;
                }

                // Apply multi-skill XP if specified (new system)
                if (dutyDef.MultiSkillXp is { Count: > 0 })
                {
                    foreach (var skillEntry in dutyDef.MultiSkillXp)
                    {
                        var skill = GetSkillFromName(skillEntry.Key);
                        if (skill != null && skillEntry.Value > 0)
                        {
                            Hero.MainHero.AddSkillXp(skill, skillEntry.Value);
                            ModLogger.Info("Duties", $"Applied {skillEntry.Value} duty XP to {skillEntry.Key}");
                        }
                    }
                }
                else if (dutyDef.SkillXpDaily > 0 && !string.IsNullOrEmpty(dutyDef.TargetSkill))
                {
                    // Handle legacy configuration format if present
                    var skill = GetSkillFromName(dutyDef.TargetSkill);
                    if (skill != null)
                    {
                        Hero.MainHero.AddSkillXp(skill, dutyDef.SkillXpDaily);
                        ModLogger.Info("Duties", $"Applied {dutyDef.SkillXpDaily} duty XP to {dutyDef.TargetSkill}");
                    }
                }

                // Duties provide daily skill XP bonuses, not military tier progression XP
                // Military tier progression XP comes from daily service (25 XP/day) and battle participation (75 XP)
            }
        }

        /// <summary>
        /// Process formation-based daily skill training.
        /// Provides automatic skill XP based on the player's military formation assignment.
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
                    ModLogger.Error("Duties", $"No formation training configuration for: {playerFormation}");
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
                        ModLogger.Error("Duties", $"Unknown skill in formation training: {skillName}");
                    }
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("Duties", "Error processing formation training", ex);
            }
        }


        /// <summary>
        /// Update officer roles based on active duties.
        /// This manages both public API assignments and prepares data for optional Harmony patches.
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
        /// Assigning officer roles through public APIs (as per Option A in the design) is a straightforward and dependable method, though the player will be presented as an "official" officer.
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
                    if (!TryGetDutyOrProfession(dutyId, out var dutyDef) ||
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
                ModLogger.Error("Duties", "Error assigning officer roles via public API", ex);
            }
        }

        /// <summary>
        /// Helper method for Harmony patches to check if the player has active duty with a specific officer role.
        /// Assigns officer roles to the player based on their active duties/professions.
        /// This method provides a public API that can be used by other systems to assign
        /// officer roles (Scout, Quartermaster, etc.) based on the player's current assignments.
        /// </summary>
        public bool HasActiveDutyWithRole(string officerRole)
        {
            if (!IsInitialized || !_officerRolesEnabled)
            {
                return false;
            }

            return _activeDuties.Any(dutyId =>
                TryGetDutyOrProfession(dutyId, out var dutyDef) &&
                string.Equals(dutyDef.OfficerRole, officerRole, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Assign duty to the player if requirements are met.
        /// </summary>
        public bool AssignDuty(string dutyId)
        {
            if (!IsInitialized || !_config.Enabled)
            {
                ModLogger.Error("Duties", "Duties system not initialized");
                return false;
            }

            // Fix: Check both Duties and Professions dictionaries
            if (!TryGetDutyOrProfession(dutyId, out var dutyDef))
            {
                ModLogger.Error("Duties", $"Unknown duty/profession: {dutyId}");
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
                ModLogger.Info("Duties", $"Cannot assign duty {dutyId}: duty slot limit reached ({_activeDuties.Count}/{maxSlots})");
                return false;
            }

            // Add duty
            _activeDuties.Add(dutyId);
            _dutyStartTimes[dutyId] = CampaignTime.Now;

            ModLogger.Info("Duties", $"Assigned duty: {dutyDef.DisplayName}");
            return true;
        }

        /// <summary>
        /// Remove duty from the player.
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
        /// Get available duties for the current player state.
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
        /// Check if the player can perform a specific duty.
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
            var lordRelation = EnlistmentBehavior.Instance?.CurrentLord != null ?
                Hero.MainHero.GetRelation(EnlistmentBehavior.Instance.CurrentLord) : 0;

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
        /// Get the number of duty slots available for a tier.
        /// </summary>
        private int GetDutySlotsForTier(int tier)
        {
            var key = $"tier_{tier}";
            return _config.DutySlots.TryGetValue(key, out var slots) ? slots : 1;
        }

        /// <summary>
        /// Convert skill name string to SkillObject.
        /// </summary>
        private SkillObject GetSkillFromName(string skillName)
        {
            try
            {
                return MBObjectManager.Instance.GetObject<SkillObject>(skillName);
            }
            catch
            {
                ModLogger.Error("Duties", $"Unknown skill: {skillName}");
                return null;
            }
        }

        /// <summary>
        /// Set player formation and update available duties.
        /// Called when the player chooses formation at Tier 2.
        /// </summary>
        public void SetPlayerFormation(string formation)
        {
            _playerFormation = formation;
            ModLogger.Info("Duties", $"Player formation set to: {formation}");

            // Remove any duties that are no longer compatible
            // Fix: Check both Duties and Professions dictionaries
            var incompatibleDuties = _activeDuties.Where(dutyId =>
                TryGetDutyOrProfession(dutyId, out var duty) &&
                duty.RequiredFormations.Count > 0 &&
                !duty.RequiredFormations.Contains(formation)).ToList();

            foreach (var dutyId in incompatibleDuties)
            {
                RemoveDuty(dutyId);
                ModLogger.Info("Duties", $"Removed incompatible duty: {dutyId} (formation changed to {formation})");
            }
        }

        /// <summary>
        /// Calculate wage multiplier from active duties.
        /// Calculates the combined wage multiplier from all active duties and professions.
        /// Different duties and professions provide different wage bonuses, allowing players
        /// to earn more gold per day based on their assignments.
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
                    if (TryGetDutyOrProfession(dutyId, out var dutyDef))
                    {
                        totalMultiplier += dutyDef.WageMultiplier;
                        validDuties++;
                    }
                }

                return validDuties > 0 ? totalMultiplier / validDuties : 1.0f;
            }
            catch (Exception ex)
            {
                ModLogger.Error("Duties", "Error calculating wage multiplier", ex);
                return 1.0f;
            }
        }

        /// <summary>
        /// Get formation display name for the current culture.
        /// Used for UI display in formation selection.
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
        /// Get formatted display text for active duties.
        /// </summary>
        public string GetActiveDutiesDisplay()
        {
            if (_activeDuties.Count == 0)
            {
                return "None assigned";
            }

            // Fix: Check both Duties and Professions dictionaries
            var duties = _activeDuties.Select(id => TryGetDutyOrProfession(id, out var dutyDef) ? dutyDef.DisplayName : id);
            var maxSlots = GetMaxDutySlots();

            return $"{string.Join(", ", duties)} ({_activeDuties.Count}/{maxSlots})";
        }

        /// <summary>
        /// Get the current officer role for menu display.
        /// </summary>
        public string GetCurrentOfficerRole()
        {
            foreach (var dutyId in _activeDuties)
            {
                // Fix: Check both Duties and Professions dictionaries
                if (TryGetDutyOrProfession(dutyId, out var dutyDef) && !string.IsNullOrEmpty(dutyDef.OfficerRole))
                {
                    return dutyDef.OfficerRole;
                }
            }
            return "";
        }

        /// <summary>
        /// Get the player's formation type for menu display.
        /// Based on the chosen troop type, not current equipment.
        /// </summary>
        public string GetPlayerFormationType()
        {
            return _playerFormation?.ToLower() ?? "infantry";
        }

        /// <summary>
        /// Get current wage multiplier from active duties.
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
        /// Get maximum duty slots based on tier.
        /// </summary>
        private int GetMaxDutySlots()
        {
            var tier = EnlistmentBehavior.Instance?.EnlistmentTier ?? 1;
            return tier switch
            {
                >= 5 => 3, // Senior tiers get 3 slots
                >= 3 => 2, // Mid tiers get 2 slots
                _ => 1     // Junior tiers get 1 slot
            };
        }

        /// <summary>
        /// Get current formation training information for menu display.
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
                ModLogger.Error("Duties", "Error getting formation training display", ex);
                return "Error loading formation training";
            }
        }

        /// <summary>
        /// Get formation training status for the current player.
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
        /// Get skill XP configuration for a specific formation.
        /// Used by the menu system for dynamic highlighting.
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
                ModLogger.Error("Duties", "Error getting formation skill XP configuration", ex);
            }

            return new Dictionary<string, int>();
        }

        #endregion
    }
}
