using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using Helpers;
using Enlisted.Features.Assignments.Behaviors;

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
	private Equipment _personalBattleEquipment;
	private Equipment _personalCivilianEquipment;
	private ItemRoster _personalInventory = new ItemRoster();
	
	// Army management state
	private bool _disbandArmyAfterBattle = false;  // SAS pattern: track if we created army for battle
	private CampaignTime _enlistmentDate = CampaignTime.Zero;
			public bool IsEnlisted => _enlistedLord != null;
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
				
				System.Console.WriteLine($"[ENLISTMENT] Successfully enlisted with {lord.Name} - Tier 1 recruit");
			}
			catch (Exception ex)
			{
				System.Console.WriteLine($"[ENLISTMENT] Failed to start enlistment: {ex.Message}");
				// Restore equipment if backup was created
				if (_hasBackedUpEquipment) { RestorePersonalEquipment(); }
			}
		}

			public void StopEnlist(string reason)
	{
		try
		{
			System.Console.WriteLine($"[ENLISTMENT] Service ended: {reason}");
			
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
		
		// Clear enlistment state
		_enlistedLord = null;
		_enlistmentTier = 1;
		_enlistmentXP = 0;
		_enlistmentDate = CampaignTime.Zero;
		_disbandArmyAfterBattle = false; // Clear any pending army operations
	}
	catch (Exception ex)
	{
		System.Console.WriteLine($"[ENLISTMENT] Error ending service: {ex.Message}");
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
				
				System.Console.WriteLine($"[DAILY_SERVICE] Paid wage: {wage} gold, gained {dailyXP} XP");
			}
			catch (Exception ex)
			{
				System.Console.WriteLine($"[DAILY_SERVICE] Error: {ex.Message}");
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
				System.Console.WriteLine($"[PROGRESSION] Promoted to Tier {_enlistmentTier}");
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
			
			System.Console.WriteLine($"[SAVE_LOAD] Validated enlistment state - Tier: {_enlistmentTier}, XP: {_enlistmentXP}");
		}
		
		/// <summary>
		/// SAS CRITICAL APPROACH: Real-time tick (runs even during paused encounters)
		/// This is the KEY to SAS's encounter prevention - continuous IsActive management
		/// </summary>
		private void OnRealtimeTick(float deltaTime)
		{
			var main = MobileParty.MainParty;
			if (main == null) { return; }
			
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
					System.Console.WriteLine("[SAS_REALTIME] Re-enabled party activity - not enlisted");
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
				System.Console.WriteLine($"[EVENT_SAFETY] Lord {victim.Name} killed - automatic discharge");
				
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
				System.Console.WriteLine($"[EVENT_SAFETY] Lord {defeatedHero.Name} defeated");
				
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
				System.Console.WriteLine($"[EVENT_SAFETY] Army dispersed - reason: {reason}");
				
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
				System.Console.WriteLine($"[EVENT_SAFETY] Lord {prisoner.Name} captured - service ended");
				
				var message = new TextObject("Your lord has been captured. Your service has ended.");
				InformationManager.DisplayMessage(new InformationMessage(message.ToString()));
				
				StopEnlist("Lord captured");
			}
		}
		
		/// <summary>
		/// Add XP to player's military progression.
		/// Called by duties system and other progression sources.
		/// </summary>
		public void AddEnlistmentXP(int xp, string source = "General")
		{
			if (!IsEnlisted || xp <= 0)
			{
				return;
			}
			
			_enlistmentXP += xp;
			System.Console.WriteLine($"[XP] +{xp} from {source} (Total: {_enlistmentXP})");
		}
		
		/// <summary>
		/// Backup player equipment before service to prevent loss.
		/// </summary>
		private void BackupPlayerEquipment()
		{
			try
			{
				// Backup equipment using verified APIs
				_personalBattleEquipment = Hero.MainHero.BattleEquipment.Clone(false);
				_personalCivilianEquipment = Hero.MainHero.CivilianEquipment.Clone(false);
				
				// CRITICAL: Quest-safe inventory backup (prevents quest item loss)
				var itemsToBackup = new List<ItemRosterElement>();
				foreach (var elem in MobileParty.MainParty.ItemRoster)
				{
					// GUARD: Skip quest items - they must stay with player
					if (elem.EquipmentElement.IsQuestItem) { continue; }
					
					var item = elem.EquipmentElement.Item;
					// GUARD: Skip special items (simplified check for current version)
					if (item == null) { continue; }
						
					// Safe to backup this item
					itemsToBackup.Add(elem);
				}
				
				// Backup safe items only
				foreach (var elem in itemsToBackup)
				{
					_personalInventory.AddToCounts(elem.EquipmentElement, elem.Amount);
					MobileParty.MainParty.ItemRoster.AddToCounts(elem.EquipmentElement, -elem.Amount);
				}
				
				System.Console.WriteLine("[EQUIPMENT] Backed up personal equipment and safe inventory");
			}
			catch (Exception ex)
			{
				System.Console.WriteLine($"[EQUIPMENT] Error backing up equipment: {ex.Message}");
				throw;
			}
		}
		
		/// <summary>
		/// Restore personal equipment from backup.
		/// </summary>
		private void RestorePersonalEquipment()
		{
			try
			{
				if (_personalBattleEquipment != null)
				{
					EquipmentHelper.AssignHeroEquipmentFromEquipment(Hero.MainHero, _personalBattleEquipment);
				}
				if (_personalCivilianEquipment != null)
				{
					Hero.MainHero.CivilianEquipment.FillFrom(_personalCivilianEquipment, false);
				}
				
				// Restore safe inventory items
				foreach (var item in _personalInventory)
				{
					MobileParty.MainParty.ItemRoster.AddToCounts(item.EquipmentElement, item.Amount);
				}
				_personalInventory.Clear();
				
				System.Console.WriteLine("[EQUIPMENT] Restored personal equipment and inventory");
			}
			catch (Exception ex)
			{
				System.Console.WriteLine($"[EQUIPMENT] Error restoring equipment: {ex.Message}");
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
				
				System.Console.WriteLine($"[EQUIPMENT] Assigned initial {_enlistedLord.Culture.Name} recruit equipment");
			}
			catch (Exception ex)
			{
				System.Console.WriteLine($"[EQUIPMENT] Error assigning initial equipment: {ex.Message}");
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
					System.Console.WriteLine("[BATTLE] Lord entering combat - enabling battle participation");
					
					// CRITICAL: Enable player for battle participation
					main.IsActive = true;  // This should trigger encounter menu
					
					// Try to force battle participation through positioning
					main.Position2D = lordParty.Position2D;
					
					var message = new TextObject("Following your lord into battle!");
					InformationManager.DisplayMessage(new InformationMessage(message.ToString()));
				}
				else if (!lordInBattle && playerInBattle)
				{
					System.Console.WriteLine("[BATTLE] Battle ended - restoring normal service");
					
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
				System.Console.WriteLine($"[BATTLE] Error in battle participation handling: {ex.Message}");
				// Fallback: maintain normal enlisted state
				main.IsActive = false;
			}
		}
		
		#endregion
	}
}


