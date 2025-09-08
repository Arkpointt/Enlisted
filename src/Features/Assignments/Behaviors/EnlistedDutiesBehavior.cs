using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
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
        public List<string> ActiveDuties => new List<string>(_activeDuties);
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
                _config = Core.ConfigurationManager.LoadDutiesConfig();
                
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
                // Create minimal fallback config
                _config = new DutiesSystemConfig { Enabled = false };
            }
        }
        
        /// <summary>
        /// Detect player formation from equipment using SAS-style formation detection.
        /// Matches the original SAS logic for consistency.
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
                    case TaleWorlds.Core.FormationClass.Infantry:
                        return "infantry";
                    case TaleWorlds.Core.FormationClass.Ranged:
                        return "archer";
                    case TaleWorlds.Core.FormationClass.Cavalry:
                        return "cavalry";
                    case TaleWorlds.Core.FormationClass.HorseArcher:
                        return "horsearcher";
                    case TaleWorlds.Core.FormationClass.Unset:
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
            
            // Must have valid lord
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
        /// Process daily benefits from active duties.
        /// </summary>
        private void ProcessDailyDuties()
        {
            var hero = Hero.MainHero;
            var enlistmentBehavior = EnlistmentBehavior.Instance;
            
            foreach (var dutyId in _activeDuties.ToList()) // ToList to prevent modification during iteration
            {
                if (!_config.Duties.TryGetValue(dutyId, out var dutyDef))
                {
                    ModLogger.Error("Duties", $"Unknown duty in active duties: {dutyId}");
                    _activeDuties.Remove(dutyId);
                    continue;
                }
                
                // Apply skill XP if specified  
                if (dutyDef.SkillXpDaily > 0 && !string.IsNullOrEmpty(dutyDef.TargetSkill))
                {
                    var skill = GetSkillFromName(dutyDef.TargetSkill);
                    if (skill != null)
                    {
                        // Use SAS method for consistency
                        Hero.MainHero.AddSkillXp(skill, dutyDef.SkillXpDaily);
                        ModLogger.Info("Duties", $"Applied {dutyDef.SkillXpDaily} duty XP to {dutyDef.TargetSkill}");
                    }
                }
                
                // Apply daily experience bonus
                if (_config.XpSources.TryGetValue("duty_performance", out var dutyXp))
                {
                    // Add XP through EnlistmentBehavior for proper tier progression tracking
                    EnlistmentBehavior.Instance?.AddEnlistmentXP(dutyXp, $"Duty: {dutyDef.DisplayName}");
                }
            }
        }
        
        /// <summary>
        /// Process formation-based daily skill training.
        /// Provides automatic skill XP based on player's military formation assignment.
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
                
                var hero = Hero.MainHero;
                
                foreach (var skillEntry in formationConfig.Skills)
                {
                    var skillName = skillEntry.Key;
                    var xpAmount = skillEntry.Value;
                    
                    var skill = GetSkillFromName(skillName);
                    if (skill != null)
                    {
                        // Use SAS method for skill XP application
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
        /// Detect player's formation type using equipment analysis (more reliable than DefaultFormationClass).
        /// </summary>
        private string DetectPlayerFormationType()
        {
            try
            {
                var hero = Hero.MainHero;
                var characterObject = hero?.CharacterObject;
                
                if (characterObject == null)
                {
                    return "infantry"; // Default fallback
                }
                
                // Equipment-based detection (more reliable than DefaultFormationClass)
                bool isRanged = characterObject.IsRanged;
                bool isMounted = characterObject.IsMounted;
                
                if (isRanged && isMounted)
                {
                    return "horsearcher";   // Bow + Horse
                }
                else if (isMounted)
                {
                    return "cavalry";       // Horse + Melee  
                }
                else if (isRanged)
                {
                    return "archer";        // Bow + No Horse
                }
                else
                {
                    return "infantry";      // Melee + No Horse (default)
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("Duties", "Error detecting player formation type", ex);
                return "infantry";
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
            AssignOfficerRolesViaPublicAPI(lordParty, playerHero);
        }
        
        /// <summary>
        /// Assign officer roles using public APIs (Option A from design).
        /// Simple and reliable, but player shows as "official" officer.
        /// </summary>
        private void AssignOfficerRolesViaPublicAPI(MobileParty lordParty, Hero playerHero)
        {
            try
            {
                // Clear current assignments first
                // (We could track previous assignments to restore, but for now keep it simple)
                
                // Assign based on active duties
                foreach (var dutyId in _activeDuties)
                {
                    if (!_config.Duties.TryGetValue(dutyId, out var dutyDef) || 
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
        /// Helper method for Harmony patches to check if player has active duty with specific officer role.
        /// Used by optional enhancement patches in Phase 2.
        /// </summary>
        public bool HasActiveDutyWithRole(string officerRole)
        {
            if (!IsInitialized || !_officerRolesEnabled)
            {
                return false;
            }
                
            return _activeDuties.Any(dutyId => 
                _config.Duties.TryGetValue(dutyId, out var dutyDef) &&
                string.Equals(dutyDef.OfficerRole, officerRole, StringComparison.OrdinalIgnoreCase));
        }
        
        /// <summary>
        /// Assign duty to player if requirements are met.
        /// </summary>
        public bool AssignDuty(string dutyId)
        {
            if (!IsInitialized || !_config.Enabled)
            {
                ModLogger.Error("Duties", "Duties system not initialized");
                return false;
            }
            
            if (!_config.Duties.TryGetValue(dutyId, out var dutyDef))
            {
                ModLogger.Error("Duties", $"Unknown duty: {dutyId}");
                return false;
            }
            
            // Check if player meets requirements
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
        /// Remove duty from player.
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
        /// Check if player can perform a specific duty.
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
        /// Get number of duty slots available for a tier.
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
        /// Called when player chooses formation at Tier 2.
        /// </summary>
        public void SetPlayerFormation(string formation)
        {
            _playerFormation = formation;
            ModLogger.Info("Duties", $"Player formation set to: {formation}");
            
            // Remove any duties that are no longer compatible
            var incompatibleDuties = _activeDuties.Where(dutyId => 
                _config.Duties.TryGetValue(dutyId, out var duty) &&
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
        /// Used by EnlistmentBehavior for enhanced wage calculation.
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
                float totalMultiplier = 0f;
                int validDuties = 0;
                
                foreach (var dutyId in _activeDuties)
                {
                    if (_config.Duties.TryGetValue(dutyId, out var dutyDef))
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
        public string GetFormationDisplayName(string formation)
        {
            // This would integrate with enlisted_config.json formations data
            // For now, return the formation name with proper casing
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
                return "None assigned";

            var duties = _activeDuties.Select(id => _config?.Duties.ContainsKey(id) == true ? _config.Duties[id].DisplayName : id);
            var maxSlots = GetMaxDutySlots();
            
            return $"{string.Join(", ", duties)} ({_activeDuties.Count}/{maxSlots})";
        }

        /// <summary>
        /// Get current officer role for menu display.
        /// </summary>
        public string GetCurrentOfficerRole()
        {
            foreach (var dutyId in _activeDuties)
            {
                if (_config?.Duties.ContainsKey(dutyId) == true && !string.IsNullOrEmpty(_config.Duties[dutyId].OfficerRole))
                {
                    return _config.Duties[dutyId].OfficerRole;
                }
            }
            return "";
        }

        /// <summary>
        /// Get player's formation type for menu display.
        /// Based on chosen troop type, not current equipment.
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
            float multiplier = 1.0f;
            
            foreach (var dutyId in _activeDuties)
            {
                if (_config?.Duties.ContainsKey(dutyId) == true)
                {
                    multiplier *= _config.Duties[dutyId].WageMultiplier;
                }
            }
            
            return multiplier;
        }

        // HasActiveDutyWithRole method already exists above - removed duplicate

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
            if (!_config?.FormationTraining?.Enabled == true)
                return "Formation training disabled";

            try
            {
                var playerFormation = GetPlayerFormationType();
                var formationDisplay = GetFormationDisplayName(playerFormation);
                
                if (_config.FormationTraining.Formations.TryGetValue(playerFormation, out var formationConfig))
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
        /// Get formation training status for current player.
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
        /// Used by menu system for dynamic highlighting.
        /// </summary>
        public Dictionary<string, int> GetFormationSkillXP(string formation)
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
