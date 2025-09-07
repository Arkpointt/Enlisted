using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using Helpers;
using Enlisted.Features.Assignments.Behaviors;
using Enlisted.Mod.Core.Logging;

namespace Enlisted.Features.Enlistment.Behaviors
{
	/// <summary>
	/// Party-first enlistment behavior: attach to a lord's party and mirror/follow.
	/// No army membership is read or modified by this behavior.
	/// </summary>
	public sealed class EnlistmentBehavior : CampaignBehaviorBase
	{
		public static EnlistmentBehavior Instance { get; private set; }

			private Hero _enlistedLord;
	private int _enlistmentTier = 1;
	private int _enlistmentXP = 0;
	
	// Equipment backup system
	private bool _hasBackedUpEquipment = false;
	private TaleWorlds.Core.Equipment _personalBattleEquipment;
	private TaleWorlds.Core.Equipment _personalCivilianEquipment;
	private ItemRoster _personalInventory = new ItemRoster();
	
	// Army management state
	private bool _disbandArmyAfterBattle = false;  // SAS pattern: track if we created army for battle
	private CampaignTime _enlistmentDate = CampaignTime.Zero;
	
	// Temporary leave system
	private bool _isOnLeave = false;
	private CampaignTime _leaveStartDate = CampaignTime.Zero;
			public bool IsEnlisted => _enlistedLord != null && !_isOnLeave;
	public bool IsOnLeave => _isOnLeave;
	public Hero CurrentLord => _enlistedLord;
	public int EnlistmentTier => _enlistmentTier;
	public int EnlistmentXP => _enlistmentXP;

		public EnlistmentBehavior()
		{
			// Singleton-style access for dialog behaviors.
			Instance = this;
		}

			public override void RegisterEvents()
	{
		// SAS CRITICAL: Use TickEvent (real-time) for continuous IsActive management
		CampaignEvents.TickEvent.AddNonSerializedListener(this, OnRealtimeTick);
		CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, OnHourlyTick);
		CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
		
			// CRITICAL: Battle participation logic - handle in real-time tick instead of events
	// Note: MapEvent events may not exist in current version, using real-time detection instead
	
	// CRITICAL: Event-driven lord death/army defeat handling (corrected signatures)
	CampaignEvents.HeroKilledEvent.AddNonSerializedListener(this, OnHeroKilled);
	CampaignEvents.CharacterDefeated.AddNonSerializedListener(this, OnCharacterDefeated);
	CampaignEvents.ArmyDispersed.AddNonSerializedListener(this, OnArmyDispersed);
	CampaignEvents.HeroPrisonerTaken.AddNonSerializedListener(this, OnHeroPrisonerTaken);
	}

			public override void SyncData(IDataStore dataStore)
	{
			// SAS APPROACH: Ensure party is active on load (prevents stuck inactive state)
	MobileParty.MainParty.IsActive = true;
	
	// Phase 1B: Complete military service save data
	dataStore.SyncData("_enlistedLord", ref _enlistedLord);
	dataStore.SyncData("_enlistmentTier", ref _enlistmentTier);
	dataStore.SyncData("_enlistmentXP", ref _enlistmentXP);
	dataStore.SyncData("_hasBackedUpEquipment", ref _hasBackedUpEquipment);
	dataStore.SyncData("_personalBattleEquipment", ref _personalBattleEquipment);
	dataStore.SyncData("_personalCivilianEquipment", ref _personalCivilianEquipment);
	dataStore.SyncData("_personalInventory", ref _personalInventory);
	dataStore.SyncData("_disbandArmyAfterBattle", ref _disbandArmyAfterBattle);
	dataStore.SyncData("_enlistmentDate", ref _enlistmentDate);
	dataStore.SyncData("_isOnLeave", ref _isOnLeave);
	dataStore.SyncData("_leaveStartDate", ref _leaveStartDate);
	
	// Post-load validation
	if (dataStore.IsLoading)
	{
		ValidateLoadedState();
	}
	}

		public bool CanEnlistWithParty(Hero lord, out TextObject reason)
		{
			reason = TextObject.Empty;
			if (IsEnlisted)
			{
				reason = new TextObject("You are already in service.");
				return false;
			}
			if (lord == null || !lord.IsLord)
			{
				reason = new TextObject("We must speak to a noble to enlist.");
				return false;
			}
			var main = MobileParty.MainParty;
			if (main == null)
			{
				reason = new TextObject("No main party found.");
				return false;
			}
			var counterpartParty = MobileParty.ConversationParty ?? lord.PartyBelongedTo;
			if (counterpartParty == null)
			{
				reason = new TextObject("The lord has no party at present.");
				return false;
			}
			return true;
		}

		public void StartEnlist(Hero lord)
		{
			if (lord == null)
			{
				return;
			}
			
			try
			{
				// CRITICAL: Backup player equipment before service (prevents equipment loss)
				if (!_hasBackedUpEquipment)
				{
					BackupPlayerEquipment();
					_hasBackedUpEquipment = true;
				}
				
							_enlistedLord = lord;
			_enlistmentTier = 1;
			_enlistmentXP = 0;
			_enlistmentDate = CampaignTime.Now;
				
							// Transfer any existing companions/troops to lord's party (SAS pattern)
				TransferPlayerTroopsToLord();
				
				// Assign initial recruit equipment based on lord's culture
			AssignInitialEquipment();
			
					// ORIGINAL WORKING VERSION: Simple escort setup
		EncounterGuard.TryAttachOrEscort(lord);
				var main = MobileParty.MainParty;
				if (main != null)
				{
					main.IsVisible = false;
					main.IsActive = false; // SAS APPROACH: Disable party activity to prevent encounters
					TrySetShouldJoinPlayerBattles(main, true);
				}
				
				ModLogger.Info("Enlistment", $"Successfully enlisted with {lord.Name} - Tier 1 recruit");
			}
			catch (Exception ex)
			{
				ModLogger.Error("Enlistment", "Failed to start enlistment", ex);
				// Restore equipment if backup was created
				if (_hasBackedUpEquipment) { RestorePersonalEquipment(); }
			}
		}

			public void StopEnlist(string reason)
	{
		try
		{
			ModLogger.Info("Enlistment", $"Service ended: {reason}");
			
			// SAS CRITICAL: Finish any active encounter first (prevents assertion crashes)
			if (PlayerEncounter.Current != null)
			{
				// Ensure we're not inside a settlement before finishing encounter
				if (PlayerEncounter.InsideSettlement)
				{
					PlayerEncounter.LeaveSettlement();
				}
				PlayerEncounter.Finish(true);
			}

			var main = MobileParty.MainParty;
			if (main != null)
			{
				main.IsVisible = true;
				main.IsActive = true;  // Re-enable party activity to allow encounters
				TrySetShouldJoinPlayerBattles(main, false);
				TryReleaseEscort(main);
			}
			
					// Phase 2: Equipment restoration
		if (_hasBackedUpEquipment)
		{
			RestorePersonalEquipment();
			_hasBackedUpEquipment = false;
		}
		
		// Restore companions to player party before ending service
		RestoreCompanionsToPlayer();
		
		// Clear enlistment state
		_enlistedLord = null;
		_enlistmentTier = 1;
		_enlistmentXP = 0;
		_enlistmentDate = CampaignTime.Zero;
		_disbandArmyAfterBattle = false; // Clear any pending army operations
	}
	catch (Exception ex)
	{
		ModLogger.Error("Enlistment", "Error ending service", ex);
		// Ensure critical state is cleared even if restoration fails
		_enlistedLord = null;
		_hasBackedUpEquipment = false;
		_disbandArmyAfterBattle = false;
	}
}

		private void OnHourlyTick()
		{
			var main = MobileParty.MainParty;
			if (main == null)
			{
				return;
			}
			if (!IsEnlisted)
			{
				return;
			}

			var counterpartParty = _enlistedLord?.PartyBelongedTo;
			if (counterpartParty == null)
			{
				// Auto-stop if target party became invalid.
				StopEnlist("Target party invalid");
				return;
			}

					// Maintain attach/escort and stealth while enlisted (ORIGINAL WORKING VERSION)
		EncounterGuard.TryAttachOrEscort(_enlistedLord);
		
		main.IsVisible = false;
		TrySetShouldJoinPlayerBattles(main, true);
		main.IgnoreByOtherPartiesTill(CampaignTime.Now + CampaignTime.Hours(0.5f));
		}

			internal static void ApplyEscort(Hero lord)
	{
		var main = MobileParty.MainParty;
		var counterpartParty = MobileParty.ConversationParty ?? lord?.PartyBelongedTo;
		if (main == null || counterpartParty == null)
		{
			return;
		}
		
		// ORIGINAL WORKING VERSION: Simple direct escort
		main.Ai.SetMoveEscortParty(counterpartParty);
	}

		internal static void TryReleaseEscort(MobileParty main)
		{
			try
			{
				var ai = main.Ai;
				var hold = ai.GetType().GetMethod("SetMoveModeHold", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
				hold?.Invoke(ai, null);
			}
			catch
			{
				// Best-effort: if we can't find a clear/hold method, do nothing.
			}
		}

		private static void TrySetShouldJoinPlayerBattles(MobileParty party, bool value)
		{
			try
			{
				var prop = party.GetType().GetProperty("ShouldJoinPlayerBattles", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				prop?.SetValue(party, value, null);
			}
			catch
			{
				// Best-effort: property may not exist in some versions; ignore failures.
			}
		}
		
		/// <summary>
		/// Daily service processing for wages and progression.
		/// Phase 1B: Complete wage and XP system with duties integration.
		/// </summary>
		private void OnDailyTick()
		{
			if (!IsEnlisted || _enlistedLord?.IsAlive != true)
			{
				return;
			}
			
			try
			{
				// Daily wage calculation (enhanced for 1-year progression)
				var wage = CalculateDailyWage();
				GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, wage, false);
				
				// Daily XP gain (1-year progression system)
				var dailyXP = 25; // Base XP - duties system can add more
				AddEnlistmentXP(dailyXP, "Daily Service");
				
				ModLogger.Info("DailyService", $"Paid wage: {wage} gold, gained {dailyXP} XP");
			}
			catch (Exception ex)
			{
				ModLogger.Error("DailyService", "Daily service processing failed", ex);
			}
		}
		
		/// <summary>
		/// Calculate daily wage based on tier, level, and duties system bonuses.
		/// Uses realistic military progression wages (24-150 gold/day range).
		/// </summary>
		private int CalculateDailyWage()
		{
			// Base wage formula: 10 + (Level × 1) + (Tier × 5) + (XP ÷ 200)
			var baseWage = 10 + Hero.MainHero.Level + (_enlistmentTier * 5) + (_enlistmentXP / 200);
			
			// Army bonus (+20% when in active army)
			var lordParty = _enlistedLord.PartyBelongedTo;
			var armyMultiplier = (lordParty?.Army != null) ? 1.2f : 1.0f;
			
			// Duties system wage multiplier (will be implemented when duties system is integrated)
			var dutiesMultiplier = GetDutiesWageMultiplier();
			
			// Apply multipliers and cap
			var finalWage = Math.Min((int)(baseWage * armyMultiplier * dutiesMultiplier), 150);
			return Math.Max(finalWage, 24); // Minimum 24 gold/day
		}
		
		/// <summary>
		/// Get wage multiplier from active duties.
		/// Integrates with the duties system for enhanced wages.
		/// </summary>
		private float GetDutiesWageMultiplier()
		{
			try
			{
				// Safely attempt to get duties behavior
				var dutiesBehavior = EnlistedDutiesBehavior.Instance;
				if (dutiesBehavior?.IsInitialized != true)
				{
					return 1.0f; // No duties system bonus
				}
				
				// Get multiplier from active duties
				return dutiesBehavior.GetWageMultiplierForActiveDuties();
			}
			catch
			{
				return 1.0f; // Fallback on any error
			}
		}
		
		/// <summary>
		/// Check for promotion based on XP thresholds.
		/// </summary>
		private void CheckForPromotion()
		{
			// 1-year progression system (SAS enhanced)
			var tierXPRequirements = new int[] { 0, 500, 1500, 3500, 7000, 12000, 18000 };
			
			bool promoted = false;
			
			// Check if player has enough XP for next tier
			while (_enlistmentTier < 7 && _enlistmentXP >= tierXPRequirements[_enlistmentTier])
			{
				_enlistmentTier++;
				promoted = true;
				ModLogger.Info("Progression", $"Promoted to Tier {_enlistmentTier}");
			}
			
			// Show promotion notification
			if (promoted)
			{
				var message = new TextObject("Promotion achieved! Your service and dedication have been recognized.");
				InformationManager.DisplayMessage(new InformationMessage(message.ToString()));
			}
		}
		
		/// <summary>
		/// Validate loaded state and handle any corruption.
		/// </summary>
		private void ValidateLoadedState()
		{
			// Ensure XP progression is valid
			if (_enlistmentTier < 1) { _enlistmentTier = 1; }
			if (_enlistmentTier > 7) { _enlistmentTier = 7; }
			if (_enlistmentXP < 0) { _enlistmentXP = 0; }
			
			ModLogger.Info("SaveLoad", $"Validated enlistment state - Tier: {_enlistmentTier}, XP: {_enlistmentXP}");
		}
		
		/// <summary>
		/// SAS CRITICAL APPROACH: Real-time tick (runs even during paused encounters)
		/// This is the KEY to SAS's encounter prevention - continuous IsActive management
		/// </summary>
		private void OnRealtimeTick(float deltaTime)
		{
			var main = MobileParty.MainParty;
			if (main == null) { return; }
			
			// Skip all enlistment logic when on leave - let vanilla behavior take over
			if (_isOnLeave)
			{
				// Ensure vanilla state when on leave
				if (!main.IsActive && !Hero.MainHero.IsPrisoner)
				{
					main.IsActive = true;
					main.IsVisible = true;
				}
				return;
			}
			
			if (IsEnlisted && _enlistedLord != null)
		{
			// Enhanced SAS logic - army-aware following and battle participation
			var lordParty = _enlistedLord.PartyBelongedTo;
			if (lordParty != null)
			{
				// CRITICAL: Battle participation detection (real-time)
				HandleBattleParticipation(main, lordParty);
				
				// SIMPLE: Original working escort logic (no complex army management)
				EncounterGuard.TryAttachOrEscort(_enlistedLord);
				
				// Basic state management
				main.Position2D = lordParty.Position2D;
				main.IsVisible = false;
				TrySetShouldJoinPlayerBattles(main, true);
				
				// VERIFIED: Ignore by other parties to prevent targeting
				main.IgnoreByOtherPartiesTill(CampaignTime.Now + CampaignTime.Hours(0.5f));
			}
		}
			else if (!IsEnlisted)
			{
				// SAS APPROACH: Ensure party is active when not enlisted
				if (!main.IsActive && !Hero.MainHero.IsPrisoner)
				{
					main.IsActive = true;
					ModLogger.Debug("Realtime", "Re-enabled party activity - not enlisted");
				}
			}
		}
		
	
	#region Critical Event Handlers for Lord Death/Army Defeat
		
		/// <summary>
		/// Handle lord death scenarios with immediate discharge.
		/// Called when lord is killed in battle or other circumstances.
		/// </summary>
		private void OnHeroKilled(Hero victim, Hero killer, KillCharacterAction.KillCharacterActionDetail detail, bool showNotification)
		{
			if (IsEnlisted && victim == _enlistedLord)
			{
				ModLogger.Info("EventSafety", $"Lord {victim.Name} killed - automatic discharge");
				
				var message = new TextObject("Your lord has been killed in battle. You have been honorably discharged.");
				InformationManager.DisplayMessage(new InformationMessage(message.ToString()));
				
				StopEnlist("Lord killed in battle");
			}
		}
		
			/// <summary>
	/// Handle lord defeat scenarios (may or may not be killed).
	/// Called when lord is defeated in battle - check if alive or captured.
	/// </summary>
	private void OnCharacterDefeated(Hero defeatedHero, Hero victorHero)
		{
			if (IsEnlisted && defeatedHero == _enlistedLord)
			{
				ModLogger.Info("EventSafety", $"Lord {defeatedHero.Name} defeated");
				
				// Check lord status after defeat
				if (!_enlistedLord.IsAlive)
				{
					var message = new TextObject("Your lord has fallen in battle. You have been honorably discharged.");
					InformationManager.DisplayMessage(new InformationMessage(message.ToString()));
					StopEnlist("Lord died in battle");
				}
				else if (_enlistedLord.IsPrisoner)
				{
					var message = new TextObject("Your lord has been captured. Your service has ended.");
					InformationManager.DisplayMessage(new InformationMessage(message.ToString()));
					StopEnlist("Lord captured in battle");
				}
			}
		}
		
		/// <summary>
		/// Handle army dispersal/defeat scenarios.
		/// Called when lord's army is disbanded or defeated.
		/// </summary>
		private void OnArmyDispersed(Army army, Army.ArmyDispersionReason reason, bool isLeaderPartyRemoved)
		{
			if (IsEnlisted && army?.LeaderParty?.LeaderHero == _enlistedLord)
			{
				ModLogger.Info("EventSafety", $"Army dispersed - reason: {reason}");
				
				// Check if lord still exists after army defeat
				if (_enlistedLord == null || !_enlistedLord.IsAlive || _enlistedLord.PartyBelongedTo == null)
				{
					var message = new TextObject("Your lord's army has been defeated. Your service has ended.");
					InformationManager.DisplayMessage(new InformationMessage(message.ToString()));
					StopEnlist("Army defeated - lord lost");
				}
				else
				{
					var message = new TextObject("Your lord's army has been dispersed. Continuing service.");
					InformationManager.DisplayMessage(new InformationMessage(message.ToString()));
					// Continue service with individual lord
				}
			}
		}
		
			/// <summary>
	/// Handle lord capture scenarios.
	/// Called when lord is taken prisoner.
	/// </summary>
	private void OnHeroPrisonerTaken(PartyBase capturingParty, Hero prisoner)
		{
			if (IsEnlisted && prisoner == _enlistedLord)
			{
				ModLogger.Info("EventSafety", $"Lord {prisoner.Name} captured - service ended");
				
				var message = new TextObject("Your lord has been captured. Your service has ended.");
				InformationManager.DisplayMessage(new InformationMessage(message.ToString()));
				
				StopEnlist("Lord captured");
			}
		}
		
		/// <summary>
		/// Add XP to player's military progression.
		/// Called by duties system and other progression sources.
		/// Triggers promotion notifications when XP thresholds are reached.
		/// </summary>
		public void AddEnlistmentXP(int xp, string source = "General")
		{
			if (!IsEnlisted || xp <= 0)
			{
				return;
			}
			
			var previousXP = _enlistmentXP;
			_enlistmentXP += xp;
			ModLogger.Info("XP", $"+{xp} from {source} (Total: {_enlistmentXP})");
			
			// Check if we crossed a promotion threshold
			CheckPromotionNotification(previousXP, _enlistmentXP);
		}

		/// <summary>
		/// Check if promotion notification should be triggered after XP gain.
		/// Now integrates with Phase 2B troop selection system.
		/// </summary>
		private void CheckPromotionNotification(int previousXP, int currentXP)
		{
			try
			{
				// 1-year progression system thresholds
				var tierXPRequirements = new int[] { 0, 500, 1500, 3500, 7000, 12000, 18000 };
				
				// Check if we crossed any promotion threshold
				for (int tier = _enlistmentTier; tier < 7; tier++)
				{
					var requiredXP = tierXPRequirements[tier];
					
					// If we crossed from below to above a threshold
					if (previousXP < requiredXP && currentXP >= requiredXP)
					{
						// Phase 2B: Trigger troop selection system
						var troopSelectionManager = Features.Equipment.Behaviors.TroopSelectionManager.Instance;
						if (troopSelectionManager != null)
						{
							troopSelectionManager.ShowTroopSelectionMenu(tier + 1);
						}
						else
						{
							// Fallback: Direct promotion notification
							ShowPromotionNotification(tier + 1);
						}
						break; // Only notify for the first threshold crossed
					}
				}
			}
			catch (Exception ex)
			{
				ModLogger.Error("Progression", $"Error checking promotion notification: {ex.Message}");
			}
		}

		/// <summary>
		/// Apply basic promotion without equipment change (fallback method).
		/// Used when troop selection system is unavailable.
		/// </summary>
		public void ApplyBasicPromotion(int newTier)
		{
			try
			{
				_enlistmentTier = newTier;
				var rankName = GetRankName(newTier);
				
				var message = new TextObject("Promoted to {RANK} (Tier {TIER})! Visit the quartermaster for equipment upgrades.");
				message.SetTextVariable("RANK", rankName);
				message.SetTextVariable("TIER", newTier.ToString());
				
				InformationManager.DisplayMessage(new InformationMessage(message.ToString()));
				
				ModLogger.Info("Promotion", $"Basic promotion applied to {rankName} (Tier {newTier})");
			}
			catch (Exception ex)
			{
				ModLogger.Error("Promotion", "Error applying basic promotion", ex);
			}
		}

		/// <summary>
		/// Show promotion notification directly to player.
		/// </summary>
		private void ShowPromotionNotification(int availableTier)
		{
			try
			{
				var rankName = GetRankName(availableTier);
				var message = new TextObject($"Promotion available! You can advance to {rankName} (Tier {availableTier}). Press 'P' to choose your advancement!");
				
				InformationManager.DisplayMessage(new InformationMessage(message.ToString()));
				ModLogger.Info("Progression", $"Promotion notification shown for Tier {availableTier}");
			}
			catch (Exception ex)
			{
				ModLogger.Error("Progression", $"Error showing promotion notification: {ex.Message}");
			}
		}

		/// <summary>
		/// Get rank name for tier.
		/// </summary>
		private string GetRankName(int tier)
		{
			var rankNames = new Dictionary<int, string>
			{
				{1, "Recruit"},
				{2, "Private"}, 
				{3, "Corporal"},
				{4, "Sergeant"},
				{5, "Staff Sergeant"},
				{6, "Master Sergeant"},
				{7, "Veteran"}
			};
			
			return rankNames.ContainsKey(tier) ? rankNames[tier] : $"Tier {tier}";
		}
		
		/// <summary>
		/// Backup player equipment before service to prevent loss.
		/// Now delegated to EquipmentManager for centralized handling.
		/// </summary>
		private void BackupPlayerEquipment()
		{
			try
			{
				// Phase 2B: Use centralized equipment management
				var equipmentManager = Features.Equipment.Behaviors.EquipmentManager.Instance;
				if (equipmentManager != null)
				{
					equipmentManager.BackupPersonalEquipment();
				}
				else
				{
					// Fallback: Basic backup if equipment manager not available
					_personalBattleEquipment = Hero.MainHero.BattleEquipment.Clone(false);
					_personalCivilianEquipment = Hero.MainHero.CivilianEquipment.Clone(false);
				}
				
				ModLogger.Info("Equipment", "Personal equipment backed up");
			}
			catch (Exception ex)
			{
				ModLogger.Error("Equipment", "Error backing up equipment", ex);
				throw;
			}
		}
		
		/// <summary>
		/// Restore personal equipment from backup.
		/// Now delegated to EquipmentManager for centralized handling.
		/// </summary>
		private void RestorePersonalEquipment()
		{
			try
			{
				// Phase 2B: Use centralized equipment management
				var equipmentManager = Features.Equipment.Behaviors.EquipmentManager.Instance;
				if (equipmentManager != null)
				{
					equipmentManager.RestorePersonalEquipment();
				}
				else
				{
					// Fallback: Basic restoration if equipment manager not available
					if (_personalBattleEquipment != null)
					{
						EquipmentHelper.AssignHeroEquipmentFromEquipment(Hero.MainHero, _personalBattleEquipment);
					}
					if (_personalCivilianEquipment != null)
					{
						Hero.MainHero.CivilianEquipment.FillFrom(_personalCivilianEquipment, false);
					}
				}
				
				ModLogger.Info("Equipment", "Personal equipment restored");
			}
			catch (Exception ex)
			{
				ModLogger.Error("Equipment", "Error restoring equipment", ex);
			}
		}
		
		/// <summary>
		/// Assign initial recruit equipment based on lord's culture.
		/// </summary>
		private void AssignInitialEquipment()
		{
			try
			{
				if (_enlistedLord?.Culture?.BasicTroop?.Equipment == null) { return; }
				
				// Use lord's culture basic troop equipment
				var basicTroopEquipment = _enlistedLord.Culture.BasicTroop.Equipment;
				EquipmentHelper.AssignHeroEquipmentFromEquipment(Hero.MainHero, basicTroopEquipment);
				
				ModLogger.Info("Equipment", $"Assigned initial {_enlistedLord.Culture.Name} recruit equipment");
			}
			catch (Exception ex)
			{
				ModLogger.Error("Equipment", "Error assigning initial equipment", ex);
			}
		}
		
		
		/// <summary>
		/// Handle battle participation detection and automatic joining.
		/// Detects when lord enters combat and enables player battle participation.
		/// </summary>
		private void HandleBattleParticipation(MobileParty main, MobileParty lordParty)
		{
			try
			{
				// Check if lord is currently in battle
				bool lordInBattle = lordParty.MapEvent != null;
				bool playerInBattle = main.MapEvent != null;
				
				if (lordInBattle && !playerInBattle)
				{
					ModLogger.Info("Battle", "Lord entering combat - enabling battle participation");
					
					// CRITICAL: Enable player for battle participation
					main.IsActive = true;  // This should trigger encounter menu
					
					// Try to force battle participation through positioning
					main.Position2D = lordParty.Position2D;
					
					var message = new TextObject("Following your lord into battle!");
					InformationManager.DisplayMessage(new InformationMessage(message.ToString()));
				}
				else if (!lordInBattle && playerInBattle)
				{
					ModLogger.Info("Battle", "Battle ended - restoring normal service");
					
					// Restore normal enlisted state
					main.IsActive = false;  // Return to normal encounter prevention
					main.IsVisible = false; // Return to invisible state
				}
				else if (!lordInBattle)
				{
					// Normal state - not in battle
					main.IsActive = false;  // SAS APPROACH: Disable party activity to prevent encounters
				}
			}
			catch (Exception ex)
			{
				ModLogger.Error("Battle", "Error in battle participation handling", ex);
				// Fallback: maintain normal enlisted state
				main.IsActive = false;
			}
		}
		
		#endregion
		
		#region Temporary Leave System
		
		/// <summary>
		/// Start temporary leave - suspend enlistment without ending service permanently.
		/// </summary>
		public void StartTemporaryLeave()
		{
			try
			{
				if (!IsEnlisted)
				{
					return;
				}
				
				ModLogger.Info("Enlistment", "Starting temporary leave - suspending service");
				
				// Clean up any active encounters first
				if (PlayerEncounter.Current != null)
				{
					if (PlayerEncounter.InsideSettlement)
					{
						PlayerEncounter.LeaveSettlement();
					}
					PlayerEncounter.Finish(true);
				}
				
				// Restore vanilla player state
				var main = MobileParty.MainParty;
				if (main != null)
				{
					main.IsVisible = true;
					main.IsActive = true;
					TrySetShouldJoinPlayerBattles(main, false);
					TryReleaseEscort(main);
				}
				
				// Set leave state (preserve all service data)
				_isOnLeave = true;
				_leaveStartDate = CampaignTime.Now;
				
				ModLogger.Info("Enlistment", "Temporary leave started - player restored to vanilla behavior");
			}
			catch (Exception ex)
			{
				ModLogger.Error("Enlistment", "Error starting temporary leave", ex);
			}
		}
		
		/// <summary>
		/// Return from temporary leave - resume enlistment with preserved data.
		/// </summary>
		public void ReturnFromLeave()
		{
			try
			{
				if (!_isOnLeave || _enlistedLord == null)
				{
					return;
				}
				
				ModLogger.Info("Enlistment", "Returning from temporary leave - resuming service");
				
				// Clear leave state
				_isOnLeave = false;
				_leaveStartDate = CampaignTime.Zero;
				
			// Transfer any new companions/troops to lord's party (SAS pattern)
			TransferPlayerTroopsToLord();
			
			// SAS CRITICAL: Finish any active encounter first (prevents assertion crashes)
			// Same logic as StartEnlist() - must clean up encounters before setting IsActive = false
			if (PlayerEncounter.Current != null)
			{
				// Ensure we're not inside a settlement before finishing encounter
				if (PlayerEncounter.InsideSettlement)
				{
					PlayerEncounter.LeaveSettlement();
				}
				PlayerEncounter.Finish(true);
			}
			
			// Resume enlistment behavior (will be handled by next real-time tick)
			var main = MobileParty.MainParty;
			if (main != null)
			{
				main.IsVisible = false;
				main.IsActive = false; // Now safe to disable after encounter cleanup
				TrySetShouldJoinPlayerBattles(main, true);
			}
				
				// SAS APPROACH: Activate enlisted status menu (zero gap implementation)
			GameMenu.ActivateGameMenu("enlisted_status");
			ModLogger.Info("Enlistment", "Service resumed - enlisted status menu activated");
			}
			catch (Exception ex)
			{
				ModLogger.Error("Enlistment", "Error returning from leave", ex);
			}
		}
		
		/// <summary>
		/// Transfer player companions and troops to lord's party (SAS pattern).
		/// Called when returning from leave or initial enlistment.
		/// </summary>
		private void TransferPlayerTroopsToLord()
		{
			try
			{
				var main = MobileParty.MainParty;
				var lordParty = _enlistedLord?.PartyBelongedTo;
				
				if (main == null || lordParty == null)
				{
					return;
				}
				
				var transferCount = 0;
				var companionCount = 0;
				
				// Transfer all non-player troops to lord's party (exact SAS pattern)
				var troopsToTransfer = new List<TroopRosterElement>();
				foreach (var troop in main.MemberRoster.GetTroopRoster())
				{
					// Skip the player character
					if (troop.Character == CharacterObject.PlayerCharacter)
					{
						continue;
					}
					
					if (troop.Number > 0)
					{
						troopsToTransfer.Add(troop);
						transferCount += troop.Number;
						if (troop.Character.IsHero)
						{
							companionCount++;
						}
					}
				}
				
				// Perform the transfer using verified SAS pattern
				foreach (var troop in troopsToTransfer)
				{
					// Add to lord's party
					lordParty.MemberRoster.AddToCounts(troop.Character, troop.Number, false, 0, 0, true, -1);
					// Remove from player party
					main.MemberRoster.AddToCounts(troop.Character, -1 * troop.Number, false, 0, 0, true, -1);
				}
				
				if (transferCount > 0)
				{
					var message = new TextObject("Your {TROOP_COUNT} troops{COMPANION_INFO} have joined your lord's party for the duration of service.");
					message.SetTextVariable("TROOP_COUNT", transferCount.ToString());
					message.SetTextVariable("COMPANION_INFO", companionCount > 0 ? $" (including {companionCount} companions)" : "");
					InformationManager.DisplayMessage(new InformationMessage(message.ToString()));
					
					ModLogger.Info("Enlistment", $"Transferred {transferCount} troops ({companionCount} companions) to lord's party");
				}
			}
			catch (Exception ex)
			{
				ModLogger.Error("Enlistment", "Error transferring troops to lord", ex);
			}
		}
		
		/// <summary>
		/// Restore companions to player party on retirement (regular troops stay with lord).
		/// Companions should be available for the player again after service ends.
		/// </summary>
		private void RestoreCompanionsToPlayer()
		{
			try
			{
				var main = MobileParty.MainParty;
				var lordParty = _enlistedLord?.PartyBelongedTo;
				
				if (main == null || lordParty == null)
				{
					return;
				}
				
				var companionsRestored = 0;
				var companionsToRestore = new List<TroopRosterElement>();
				
				// Find player's companions in lord's party
				foreach (var troop in lordParty.MemberRoster.GetTroopRoster())
				{
					// Only restore hero companions, not regular troops
					if (troop.Character.IsHero && troop.Character != CharacterObject.PlayerCharacter)
					{
						// Check if this companion belongs to player's clan
						var hero = troop.Character.HeroObject;
						if (hero != null && hero.Clan == Clan.PlayerClan)
						{
							companionsToRestore.Add(troop);
							companionsRestored += troop.Number;
						}
					}
				}
				
				// Transfer companions back to player using verified API pattern
				foreach (var companion in companionsToRestore)
				{
					// Remove from lord's party
					lordParty.MemberRoster.AddToCounts(companion.Character, -1 * companion.Number, false, 0, 0, true, -1);
					// Add back to player party
					main.MemberRoster.AddToCounts(companion.Character, companion.Number, false, 0, 0, true, -1);
				}
				
				if (companionsRestored > 0)
				{
					var message = new TextObject("Your {COMPANION_COUNT} companions have rejoined your party upon retirement.");
					message.SetTextVariable("COMPANION_COUNT", companionsRestored.ToString());
					InformationManager.DisplayMessage(new InformationMessage(message.ToString()));
					
					ModLogger.Info("Enlistment", $"Restored {companionsRestored} companions to player party on retirement");
				}
				
				// Note: Regular troops stay with the lord as they've become part of the military unit
			}
			catch (Exception ex)
			{
				ModLogger.Error("Enlistment", "Error restoring companions on retirement", ex);
			}
		}
		
		#endregion
	}
}


