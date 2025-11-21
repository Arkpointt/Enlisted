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
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Library;
using Enlisted.Mod.GameAdapters.Patches;
using TaleWorlds.Localization;
using Helpers;
using Enlisted.Features.Assignments.Behaviors;
using Enlisted.Features.Assignments.Core;
using Enlisted.Features.Interface.Behaviors;
using Enlisted.Mod.Core.Logging;
using Enlisted.Mod.Entry;

namespace Enlisted.Features.Enlistment.Behaviors
{
	/// <summary>
	/// Core behavior managing the player's enlistment in a lord's military service.
	/// 
	/// This behavior tracks the enlisted lord, manages party following and battle participation,
	/// handles equipment backup/restoration, processes XP and promotions, and manages party
	/// activity state to prevent unwanted encounters while allowing battle participation.
	/// 
	/// The system works by attaching the player's party to the lord's party, making the player
	/// invisible and inactive during normal travel (preventing random encounters), but activating
	/// the player when the lord enters battles so they can participate.
	/// </summary>
	public sealed class EnlistmentBehavior : CampaignBehaviorBase
	{
		/// <summary>
		/// Checks if a party is currently involved in a battle, siege, or besieging a settlement.
		/// Used to prevent finishing player encounters during critical battle/siege operations,
		/// which can cause assertion failures in the siege system that expects encounters to
		/// remain active during these operations.
		/// </summary>
		/// <param name="party">The party to check for battle/siege state.</param>
		/// <returns>True if the party is in battle, siege, or besieging a settlement; false otherwise.</returns>
		private static bool InBattleOrSiege(MobileParty party) =>
			party?.Party.MapEvent != null || party?.Party.SiegeEvent != null || party?.BesiegedSettlement != null;

		public static EnlistmentBehavior Instance { get; private set; }

			/// <summary>
		/// The lord the player is currently serving under, or null if not enlisted.
		/// </summary>
		private Hero _enlistedLord;
		
		/// <summary>
		/// Current military rank tier (1-6), where 1 is the lowest rank and 6 is the highest.
		/// Determined by accumulated enlistment XP and tier thresholds from progression_config.json.
		/// </summary>
		private int _enlistmentTier = 1;
		
		/// <summary>
		/// Total experience points accumulated during military service.
		/// Gained from daily service, battle participation, duty performance, and special events.
		/// Used to determine tier promotions based on thresholds in progression_config.json.
		/// </summary>
		private int _enlistmentXP = 0;
	
		/// <summary>
		/// Whether the player's personal equipment has been backed up before enlistment.
		/// Equipment is backed up once at the start of service and restored when service ends.
		/// </summary>
		private bool _hasBackedUpEquipment = false;
		
		/// <summary>
		/// Backup of the player's battle equipment before enlistment.
		/// Restored when the player ends their service.
		/// </summary>
		private TaleWorlds.Core.Equipment _personalBattleEquipment;
		
		/// <summary>
		/// Backup of the player's civilian equipment before enlistment.
		/// Restored when the player ends their service.
		/// </summary>
		private TaleWorlds.Core.Equipment _personalCivilianEquipment;
		
		/// <summary>
		/// Backup of the player's inventory items before enlistment.
		/// Restored when the player ends their service.
		/// </summary>
		private ItemRoster _personalInventory = new ItemRoster();
	
		/// <summary>
		/// Tracks whether an army was created specifically for battle participation.
		/// If true, the army should be disbanded after the battle completes to prevent
		/// the player from remaining in an army when not needed.
		/// </summary>
		private bool _disbandArmyAfterBattle = false;
		
		/// <summary>
		/// Campaign time when the player first enlisted with the current lord.
		/// Used for calculating service duration and veteran status.
		/// </summary>
		private CampaignTime _enlistmentDate = CampaignTime.Zero;
	
		/// <summary>
		/// Whether the player is currently on temporary leave from service.
		/// When on leave, the player is not actively enlisted but can return without
		/// going through the full enlistment process again.
		/// </summary>
		private bool _isOnLeave = false;
		
		/// <summary>
		/// Campaign time when the player started their current leave period.
		/// Used for tracking leave duration.
		/// </summary>
		private CampaignTime _leaveStartDate = CampaignTime.Zero;
	
		/// <summary>
		/// Last campaign time when the real-time tick update was processed.
		/// Used with _realtimeUpdateIntervalSeconds to throttle update frequency.
		/// </summary>
		private CampaignTime _lastRealtimeUpdate = CampaignTime.Zero;
		
		/// <summary>
		/// Minimum time interval between real-time tick updates, in seconds.
		/// Updates are throttled to every 100ms to prevent overwhelming the game's
		/// rendering system with too-frequent state changes, which can cause assertion failures.
		/// </summary>
		private readonly float _realtimeUpdateIntervalSeconds = 0.1f;
	
		/// <summary>
		/// Currently selected duty assignment ID (e.g., "enlisted", "forager", "sentry").
		/// Duties provide daily skill XP bonuses and may include wage multipliers.
		/// Changed via the duty selection menu.
		/// </summary>
		private string _selectedDuty = "enlisted";
		
		/// <summary>
		/// Currently selected profession ID (e.g., "field_medic", "quartermaster_aide").
		/// Professions unlock at tier 3 and provide specialized skill XP bonuses.
		/// Can be set to "none" if no profession is selected.
		/// </summary>
		private string _selectedProfession = "none";
	
		/// <summary>
		/// The player's original kingdom before enlisting with the lord.
		/// Stored so it can be restored when service ends. Null if the player was independent.
		/// </summary>
		private Kingdom _originalKingdom;
		
		/// <summary>
		/// Whether the player's clan was independent (no kingdom) before enlistment.
		/// Used to determine whether to restore independence or rejoin the original kingdom
		/// when service ends.
		/// </summary>
		private bool _wasIndependentClan;
			public bool IsEnlisted => _enlistedLord != null && !_isOnLeave;
	public bool IsOnLeave => _isOnLeave;
	public Hero CurrentLord => _enlistedLord;
	public int EnlistmentTier => _enlistmentTier;
	public int EnlistmentXP => _enlistmentXP;
	public string SelectedDuty => _selectedDuty;
	public string SelectedProfession => _selectedProfession;

		/// <summary>
		/// Initializes the enlistment behavior and sets up singleton access.
		/// The singleton pattern allows other behaviors (like dialog managers) to access
		/// enlistment state without needing a direct reference.
		/// </summary>
		public EnlistmentBehavior()
		{
			Instance = this;
		}

		/// <summary>
		/// Registers event listeners that respond to game events throughout the campaign.
		/// These events fire at various intervals and trigger specific behaviors like
		/// party state updates, battle detection, lord status changes, and XP processing.
		/// </summary>
		public override void RegisterEvents()
		{
			// Real-time tick runs every frame, used for continuous party state management.
			// Includes time validation to prevent executing updates during zero-delta-time frames,
			// which can cause assertion failures in the rendering system.
			CampaignEvents.TickEvent.AddNonSerializedListener(this, OnRealtimeTick);
			
			// Hourly tick runs once per in-game hour, used for periodic checks like
			// battle detection, menu state validation, and promotion eligibility.
			CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, OnHourlyTick);
			
			// Daily tick runs once per in-game day, used for wage calculation,
			// XP accumulation, and daily duty processing.
			CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
		
			// Battle detection and participation are handled in the real-time tick
			// by checking MapEvent status, rather than relying on battle start/end events,
			// to ensure immediate response and compatibility across game versions.
		
			// Event-driven handlers for lord status changes that affect enlistment:
			// These events fire when the enlisted lord is killed, defeated, taken prisoner,
			// or when their army is dispersed, allowing us to handle service termination.
			CampaignEvents.HeroKilledEvent.AddNonSerializedListener(this, OnHeroKilled);
			CampaignEvents.CharacterDefeated.AddNonSerializedListener(this, OnCharacterDefeated);
			CampaignEvents.ArmyDispersed.AddNonSerializedListener(this, OnArmyDispersed);
			CampaignEvents.HeroPrisonerTaken.AddNonSerializedListener(this, OnHeroPrisonerTaken);
	
			// Battle start/end events provide notification when battles begin and complete.
			// Used for logging and state tracking, though battle participation is primarily
			// managed through real-time MapEvent detection for immediate response.
			CampaignEvents.MapEventStarted.AddNonSerializedListener(this, OnMapEventStarted);
			CampaignEvents.MapEventEnded.AddNonSerializedListener(this, OnMapEventEnded);
	
			// Player battle end event fires when the player's battle completes.
			// Used for tracking battle completion and handling post-battle state transitions.
			CampaignEvents.OnPlayerBattleEndEvent.AddNonSerializedListener(this, OnPlayerBattleEnd);
	
			// Settlement entry event fires when the player enters a town or castle.
			// Used to detect when the player enters settlements with their lord and
			// trigger appropriate menu refreshes or state updates.
			CampaignEvents.SettlementEntered.AddNonSerializedListener(this, OnSettlementEntered);
		}

	/// <summary>
	/// Serializes and deserializes enlistment state for save/load operations.
	/// Called automatically by Bannerlord's save system when saving or loading games.
	/// After loading, validates state and restores proper party activity state.
	/// </summary>
	/// <param name="dataStore">The save/load data store that handles serialization.</param>
	public override void SyncData(IDataStore dataStore)
	{
		// Serialize all enlistment state variables
		// These values are saved to the game's save file and restored when loading
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
		dataStore.SyncData("_selectedDuty", ref _selectedDuty);
		dataStore.SyncData("_selectedProfession", ref _selectedProfession);
		
		// Serialize kingdom state so we can restore the player's original kingdom/clan status
		dataStore.SyncData("_originalKingdom", ref _originalKingdom);
		dataStore.SyncData("_wasIndependentClan", ref _wasIndependentClan);
	
		// After loading, validate state and restore proper party activity
		// This is important because the save system doesn't preserve IsActive state,
		// and we need to ensure enlisted players start inactive to prevent random encounters
		if (dataStore.IsLoading)
		{
			// Validate tier and XP values are within acceptable ranges
			ValidateLoadedState();
		
			// Restore proper party activity state after loading
			// Non-enlisted players should be active to allow normal gameplay
			if (!IsEnlisted && !Hero.MainHero.IsPrisoner)
			{
				MobileParty.MainParty.IsActive = true;
				ModLogger.Debug("SaveLoad", "Party activated - not enlisted");
			}
			else if (IsEnlisted)
			{
				// Enlisted players start inactive to prevent random encounters
				// The real-time tick will activate them if the lord enters battle
				MobileParty.MainParty.IsActive = false;
				ModLogger.Debug("SaveLoad", $"Party kept inactive - enlisted state (Lord: {_enlistedLord?.Name}, Army: {_enlistedLord?.PartyBelongedTo?.Army != null})");
			
				// Ensure the party can join battles when the lord fights
				TrySetShouldJoinPlayerBattles(MobileParty.MainParty, true);
			
				// Check if we're loading into a save where a battle is already in progress
				// If so, activate the party immediately so they can participate
				if (_enlistedLord?.PartyBelongedTo != null)
				{
					var lordParty = _enlistedLord.PartyBelongedTo;
					var lordArmy = lordParty.Army;
					
					// The MapEvent property exists on Party, not directly on MobileParty
					// This is the correct API structure for checking battle state
					bool lordInBattle = lordParty.Party.MapEvent != null;
					bool armyInBattle = lordArmy?.LeaderParty?.Party.MapEvent != null;
				
					if (lordInBattle || armyInBattle)
					{
						ModLogger.Info("SaveLoad", $"Loaded into active battle! Lord battle: {lordInBattle}, Army battle: {armyInBattle}");
						// Immediately activate for battle participation
						MobileParty.MainParty.IsActive = true;
						ModLogger.Info("SaveLoad", "Party activated immediately due to ongoing battle");
					}
				}
			}
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
				// Backup player's personal equipment before service begins
			// This ensures the player gets their original equipment back when service ends
			// Equipment is backed up once at the start to prevent losing items during service
			if (!_hasBackedUpEquipment)
			{
				BackupPlayerEquipment();
				_hasBackedUpEquipment = true;
			}
				
			// Initialize enlistment state with default values
			_enlistedLord = lord;
			_enlistmentTier = 1;  // Start at the lowest tier (recruit/levy)
			_enlistmentXP = 0;  // Start with zero XP
			_enlistmentDate = CampaignTime.Now;  // Record when service started
			_selectedDuty = "enlisted";  // Default to basic enlisted duty for daily XP
			_selectedProfession = "none";  // No profession initially (unlocks at tier 3)
			
			// Register the default "enlisted" duty with the duties system
			// This ensures daily XP processing begins immediately for the basic duty
			var dutiesBehavior = Features.Assignments.Behaviors.EnlistedDutiesBehavior.Instance;
			if (dutiesBehavior != null)
			{
				dutiesBehavior.AssignDuty("enlisted");
			}
			
			// Store the player's original kingdom/clan state before joining the lord's faction
			// This information is needed to restore the player's original kingdom when service ends
			var playerClan = Clan.PlayerClan;
			_originalKingdom = playerClan?.Kingdom;
			_wasIndependentClan = (playerClan?.Kingdom == null);
			
			// Join lord's kingdom if they have one and player isn't already in it
			var lordKingdom = lord.MapFaction as Kingdom;
			if (lordKingdom != null && playerClan != null)
			{
				if (playerClan.Kingdom != lordKingdom)
				{
					try
					{
						// Join the lord's kingdom as a vassal
						ChangeKingdomAction.ApplyByJoinToKingdom(playerClan, lordKingdom, false);
						ModLogger.Info("Enlistment", $"Joined {lordKingdom.Name} as vassal while enlisted with {lord.Name}");
					}
					catch (Exception ex)
					{
						ModLogger.Error("Enlistment", $"Error joining lord's kingdom: {ex.Message}");
					}
				}
				else
				{
					ModLogger.Debug("Enlistment", "Player already in lord's kingdom");
				}
			}
			else if (lordKingdom == null)
			{
				ModLogger.Debug("Enlistment", "Lord has no kingdom - player clan remains independent");
			}
				
			// Transfer any existing companions and troops from the player's party to the lord's party
			// This prevents the player from having their own troops while enlisted, as they're
			// now part of the lord's military force. Troops will be restored when service ends.
			TransferPlayerTroopsToLord();
				
			// Assign initial recruit equipment based on the lord's culture and tier 1 rank
			// New recruits start with basic equipment appropriate to their lord's faction
			AssignInitialEquipment();
			
			// Set initial military formation (Infantry/Cavalry/Archer/Horse Archer) for new recruits
			// Formation can be manually selected later when the player reaches tier 2
			SetInitialFormation();
			
			// Attach the player's party to the lord's party using position matching
			// This ensures the player follows the lord during travel
			EncounterGuard.TryAttachOrEscort(lord);
			
			// Configure party state to prevent random encounters while allowing battle participation
			var main = MobileParty.MainParty;
			if (main != null)
			{
				// Make the player party invisible on the map (they're part of the lord's force now)
				main.IsVisible = false;
				
				// Disable party activity to prevent random encounters with bandits or other parties
				// The party will be reactivated automatically when the lord enters battles
				main.IsActive = false;
				
				// Enable battle participation so the player joins battles when the lord fights
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
			
			// Finish any active player encounter before ending service
			// This prevents assertion failures that can occur when ending encounters during
			// settlement entry/exit or other state transitions
			if (PlayerEncounter.Current != null)
			{
				// If inside a settlement, leave it first before finishing the encounter
				// This ensures clean encounter state cleanup
				if (PlayerEncounter.InsideSettlement)
				{
					PlayerEncounter.LeaveSettlement();
				}
				PlayerEncounter.Finish(true);
			}

			// Restore normal party state now that service is ending
			var main = MobileParty.MainParty;
			if (main != null)
			{
				main.IsVisible = true;  // Make the party visible on the map again
				main.IsActive = true;  // Re-enable party activity to allow normal encounters
				TrySetShouldJoinPlayerBattles(main, false);  // Disable automatic battle joining
				TryReleaseEscort(main);  // Release attachment to the lord's party
			}
			
			// Restore the player's personal equipment that was backed up at enlistment start
			if (_hasBackedUpEquipment)
			{
				RestorePersonalEquipment();
				_hasBackedUpEquipment = false;
			}
		
			// Restore companions and troops to the player's party
			// These were transferred to the lord's party when service started
			RestoreCompanionsToPlayer();
		
			// Restore the player's original kingdom/clan state
		var playerClan = Clan.PlayerClan;
		if (playerClan != null)
		{
			try
			{
				if (_wasIndependentClan && playerClan.Kingdom != null)
				{
					// Player was independent before - leave current kingdom
					ChangeKingdomAction.ApplyByLeaveKingdom(playerClan, false);
					ModLogger.Info("Enlistment", "Restored player clan to independent status");
				}
				else if (_originalKingdom != null && playerClan.Kingdom != _originalKingdom)
				{
					// Player was in a different kingdom - restore it
					ChangeKingdomAction.ApplyByJoinToKingdom(playerClan, _originalKingdom, false);
					ModLogger.Info("Enlistment", $"Restored player clan to {_originalKingdom.Name}");
				}
				else
				{
					ModLogger.Debug("Enlistment", "Player clan kingdom unchanged");
				}
			}
			catch (Exception ex)
			{
				ModLogger.Error("Enlistment", $"Error restoring original kingdom: {ex.Message}");
			}
		}
		
		// Clear kingdom state tracking
		_originalKingdom = null;
		_wasIndependentClan = false;
		
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

		/// <summary>
		/// Hourly tick handler that runs once per in-game hour while the player is enlisted.
		/// Maintains party following, visibility, and encounter prevention state.
		/// Called automatically by the game every hour.
		/// </summary>
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

			// Check if the lord's party still exists
			var counterpartParty = _enlistedLord?.PartyBelongedTo;
			if (counterpartParty == null)
			{
				// The lord's party no longer exists - automatically end service
				StopEnlist("Target party invalid");
				return;
			}

			// Maintain party attachment to the lord's party
			// This ensures the player continues to follow the lord during travel
			EncounterGuard.TryAttachOrEscort(_enlistedLord);
		
			// Keep the player party invisible on the map (they're part of the lord's force)
			main.IsVisible = false;
			
			// Ensure the player can join battles when the lord fights
			TrySetShouldJoinPlayerBattles(main, true);
			
			// Ignore other parties for 0.5 hours to prevent random encounters
			// This prevents unwanted encounters while maintaining battle participation
			main.IgnoreByOtherPartiesTill(CampaignTime.Now + CampaignTime.Hours(0.5f));
		}

		/// <summary>
		/// Attaches the player's party to the lord's party for following.
		/// Uses direct position matching to ensure the player follows the lord during travel.
		/// Delegates to EncounterGuard.TryAttachOrEscort for the actual implementation.
		/// </summary>
		/// <param name="lord">The lord whose party the player should follow.</param>
		internal static void ApplyEscort(Hero lord)
		{
			// Use direct position matching instead of AI escort system
			// This avoids complications with the AI system and ensures reliable following
			ModLogger.Debug("Following", "ApplyEscort called - delegating to EncounterGuard.TryAttachOrEscort");
			EncounterGuard.TryAttachOrEscort(lord);
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
				// If the hold method cannot be found, the party will continue with its current movement state
			}
		}

		/// <summary>
		/// Sets the ShouldJoinPlayerBattles property on a party, using direct access first
		/// and falling back to reflection if direct access fails.
		/// 
		/// This property controls whether the party automatically joins battles.
		/// For enlisted players, this should be true so they can participate in battles.
		/// The method handles both modern API (direct property) and older versions (reflection).
		/// </summary>
		/// <param name="party">The party to modify.</param>
		/// <param name="value">True to enable automatic battle joining, false to disable.</param>
		private static void TrySetShouldJoinPlayerBattles(MobileParty party, bool value)
		{
			try
			{
				// Try direct property access first (modern API in current Bannerlord versions)
				party.ShouldJoinPlayerBattles = value;
				ModLogger.Debug("Battle", $"ShouldJoinPlayerBattles set to {value} via direct property access");
			}
			catch (Exception ex1)
			{
				try
				{
					// Use reflection for game versions where the property is not directly accessible
					// might not be directly accessible
					var prop = party.GetType().GetProperty("ShouldJoinPlayerBattles", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
					if (prop != null)
					{
						prop.SetValue(party, value, null);
						ModLogger.Debug("Battle", $"ShouldJoinPlayerBattles set to {value} via reflection");
					}
					else
					{
						ModLogger.Error("Battle", "ShouldJoinPlayerBattles property not found via reflection");
					}
				}
				catch (Exception ex2)
				{
					ModLogger.Error("Battle", $"Failed to set ShouldJoinPlayerBattles: Direct={ex1.Message}, Reflection={ex2.Message}");
				}
			}
		}
		
		/// <summary>
		/// Processes daily military service benefits: wages and XP progression.
		/// Called once per in-game day while the player is enlisted.
		/// Integrates with the duties system to provide additional wage multipliers.
		/// </summary>
		private void OnDailyTick()
		{
			if (!IsEnlisted || _enlistedLord?.IsAlive != true)
			{
				return;
			}
			
			try
			{
				// Calculate and pay daily wage based on tier, level, and duties
				// Wage increases with tier progression and includes bonuses from duties
				var wage = CalculateDailyWage();
				GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, wage, false);
				
				// Award daily XP for military tier progression
				// This is separate from skill XP, which is handled by formation training and duties
				// Base daily XP: 25 points. Additional XP comes from battle participation (75) and duties (15)
				var dailyXP = 25;
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
		/// Gets the wage multiplier from active duties and professions.
		/// Different duties and professions provide different wage bonuses,
		/// allowing players to earn more gold per day based on their assignments.
		/// </summary>
		/// <returns>Wage multiplier (1.0 = no bonus, higher values = bonus).</returns>
		private float GetDutiesWageMultiplier()
		{
			try
			{
				// Get the duties behavior instance to access active duties
				var dutiesBehavior = EnlistedDutiesBehavior.Instance;
				if (dutiesBehavior?.IsInitialized != true)
				{
					return 1.0f; // Return no multiplier if duties system isn't initialized
				}
				
				// Get the combined wage multiplier from all active duties and professions
				return dutiesBehavior.GetWageMultiplierForActiveDuties();
			}
			catch
			{
				// Return neutral multiplier if any error occurs
				return 1.0f;
			}
		}
		
		/// <summary>
		/// Check for promotion based on XP thresholds.
		/// </summary>
		private void CheckForPromotion()
		{
			// Load tier XP requirements from progression_config.json
			var tierXPRequirements = Assignments.Core.ConfigurationManager.GetTierXPRequirements();
			
			// Get max tier from config (array uses 1-based indexing, so Length - 1 = max tier)
			// With 6 tiers configured, array size is 7 (indices 0-6), max tier is 6
			int maxTier = tierXPRequirements.Length > 1 ? tierXPRequirements.Length - 1 : 1;
			
			bool promoted = false;
			
			// Check if player has enough XP for next tier
			// Fix: Prevent promoting beyond max tier (6) enforced in SetTier
			while (_enlistmentTier < maxTier && _enlistmentXP >= tierXPRequirements[_enlistmentTier])
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
			// Fix: Update max tier from 7 to 6 to match progression_config.json and SetTier validation
			if (_enlistmentTier < 1) { _enlistmentTier = 1; }
			if (_enlistmentTier > 6) { _enlistmentTier = 6; }
			if (_enlistmentXP < 0) { _enlistmentXP = 0; }
			
			ModLogger.Info("SaveLoad", $"Validated enlistment state - Tier: {_enlistmentTier}, XP: {_enlistmentXP}");
		}
		
		/// <summary>
		/// Real-time tick handler that runs every game frame while the player is enlisted.
		/// Manages party state (active/inactive), battle participation, army membership,
		/// and position following in real-time to ensure smooth gameplay.
		/// 
		/// This method is throttled to run every 100ms to prevent overwhelming the game's
		/// rendering system with too-frequent state changes.
		/// </summary>
		/// <param name="deltaTime">Time elapsed since last frame, in seconds. Must be positive.</param>
		private void OnRealtimeTick(float deltaTime)
		{
			// Skip all processing if the player is not currently enlisted
			// This avoids unnecessary computation when the system isn't active
			if (!IsEnlisted)
			{
				return;
			}
			
			// Validate that deltaTime is positive to prevent zero-delta-time updates
			// Zero-delta-time updates can cause assertion failures in the rendering system
			if (deltaTime <= 0)
			{
				return;
			}
			
			// Throttle updates to prevent overwhelming the game's rendering system
			// Updates are limited to once every 100 milliseconds (0.1 seconds)
			// This prevents too-frequent state changes that can cause assertion failures
			int intervalMilliseconds = (int)(_realtimeUpdateIntervalSeconds * 1000);
			if (CampaignTime.Now - _lastRealtimeUpdate < CampaignTime.Milliseconds(intervalMilliseconds))
			{
				return;
			}
			
			_lastRealtimeUpdate = CampaignTime.Now;
			
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
				// Get the lord's party to check battle and army status
				var lordParty = _enlistedLord.PartyBelongedTo;
				if (lordParty != null)
				{
					// Check battle and siege state for both lord and player
					// The MapEvent property exists on Party, not directly on MobileParty
					// This is the correct API structure for checking battle state
					var mainParty = MobileParty.MainParty;
					bool lordHasMapEvent = lordParty.Party.MapEvent != null;
					bool playerHasMapEvent = mainParty?.Party.MapEvent != null;
					bool lordHasBesiegedSettlement = lordParty.BesiegedSettlement != null;
					bool playerInArmy = mainParty?.Army != null;
					bool lordInArmy = lordParty.Army != null;
					
					// Ensure the player is in the lord's army when the lord is in an army
					// The game's battle collection system requires parties to be in the army
					// before the MapEvent is created, so we add the player proactively
					// This ensures the player will be collected into battles automatically
					if (lordInArmy && (mainParty.Army == null || mainParty.Army != lordParty.Army))
					{
						try
						{
							// Add the player's party to the lord's army
							lordParty.Army.AddPartyToMergedParties(mainParty);
							mainParty.Army = lordParty.Army;
							ModLogger.Debug("Battle", $"Ensured player is in lord's army (Army Leader: {lordParty.Army.LeaderParty?.LeaderHero?.Name})");
						}
						catch (Exception ex)
						{
							ModLogger.Error("Battle", $"Error ensuring player is in army: {ex.Message}");
						}
					}
					
					// Log battle state changes when significant events occur
					// This helps debug battle participation issues and track state transitions
					if (lordHasMapEvent || lordHasBesiegedSettlement || playerInArmy != (mainParty?.Army == lordParty?.Army))
					{
						ModLogger.Info("Battle", $"=== BATTLE STATE CHANGE ===");
						ModLogger.Info("Battle", $"Lord: {_enlistedLord.Name}");
						ModLogger.Info("Battle", $"Lord MapEvent: {lordHasMapEvent}, Player MapEvent: {playerHasMapEvent}");
						ModLogger.Info("Battle", $"Lord Besieging: {(lordParty.BesiegedSettlement?.Name?.ToString() ?? "null")}");
						ModLogger.Info("Battle", $"Player in Army: {playerInArmy} (Army: {(mainParty?.Army?.LeaderParty?.LeaderHero?.Name?.ToString() ?? "null")})");
						ModLogger.Info("Battle", $"Lord in Army: {lordInArmy} (Army Leader: {(lordParty.Army?.LeaderParty?.LeaderHero?.Name?.ToString() ?? "null")})");
						ModLogger.Info("Battle", $"Current Menu: {Campaign.Current?.CurrentMenuContext?.GameMenu?.StringId ?? "null"}");
						ModLogger.Info("Battle", $"PlayerEncounter: Active={PlayerEncounter.IsActive}, Current={PlayerEncounter.Current != null}");
					}
					
					// Verify player is already in the correct army for automatic battle participation
					if (playerInArmy && mainParty?.Army == lordParty?.Army)
					{
						ModLogger.Info("Battle", $"ARMY STATUS: Player already in lord's army - should participate automatically");
					}
					
					// Monitor siege state and prepare party for siege encounter creation
					// This ensures the player can participate in sieges properly
					SiegeWatchdogTick(mainParty, lordParty);
				
					// Handle battle participation when the lord enters battle but the player hasn't yet
					// This ensures the player joins the battle even if they weren't initially collected
					if (lordHasMapEvent && !playerHasMapEvent)
					{
						// Only attempt to add the player to the army if they're not already in it
						// If they're already in the correct army, the native system should collect them automatically
						if (mainParty.Army == null || mainParty.Army != lordParty.Army)
						{
							ModLogger.Info("Battle", $"Lord entered battle - ensuring player can participate");
							ModLogger.Info("Battle", $"Pre-state: Player Army={mainParty?.Army?.LeaderParty?.LeaderHero?.Name?.ToString() ?? "null"}");
							
							try
							{
								// Don't clear menus if the lord is in battle or siege
								// Clearing menus during these operations can cause assertion failures
								if (InBattleOrSiege(lordParty))
								{
									ModLogger.Info("Battle", "Lord in battle/siege - skipping menu clearing to prevent assertion failures");
								}
								else
								{
									// Clear menus before army management to ensure clean state transitions
									// This prevents menu state conflicts during battle setup
									ModLogger.Info("Battle", "Clearing menus before army management");
									while (Campaign.Current.CurrentMenuContext != null)
									{
										GameMenu.ExitToLast();
									}
								}
								
								// Ensure the lord has an army (create one if needed)
								// The player needs to join an army to participate in battles
								if (lordParty.Army == null)
								{
									ModLogger.Info("Battle", $"Creating army for {lordParty.LeaderHero.Name}");
									var kingdom = lordParty.ActualClan?.Kingdom;
									if (kingdom != null)
									{
										kingdom.CreateArmy(lordParty.LeaderHero, Hero.MainHero.HomeSettlement, Army.ArmyTypes.Patrolling);
										ModLogger.Info("Battle", $"Army created for {lordParty.LeaderHero.Name}");
									}
								}
								
								// Add the player to the lord's army so they can participate in battles
								if (lordParty.Army != null)
								{
									ModLogger.Info("Battle", $"Adding player to lord's army for battle participation");
									ModLogger.Info("Battle", $"Lord's army: {lordParty.Army.LeaderParty.LeaderHero.Name}");
								
									// Add the player's party to the army's merged parties list
									// This makes the player part of the army for battle purposes
									lordParty.Army.AddPartyToMergedParties(mainParty);
									mainParty.Army = lordParty.Army;
									
									// Activate the player's party so they can participate
									mainParty.IsActive = true;
									
									// Position the player's party at the lord's location
									// Using direct position assignment avoids AI escort complications
									// and ensures the player is in the right place for battle
									mainParty.Position2D = lordParty.Position2D;
								
									// Make the player visible and enable battle participation
									mainParty.IsVisible = true;
									mainParty.IgnoreByOtherPartiesTill(CampaignTime.Now);
									
									// Set additional properties to ensure battle participation
									mainParty.Party.SetAsCameraFollowParty();
									mainParty.ShouldJoinPlayerBattles = true;
								
									ModLogger.Info("Battle", $"SUCCESS: Player now in army - Army Leader: {mainParty.Army?.LeaderParty?.LeaderHero?.Name?.ToString() ?? "null"}");
								}
								else
								{
									ModLogger.Info("Battle", "Lord has no army - cannot add player to army");
								}
							}
							catch (Exception ex)
							{
								ModLogger.Error("Battle", $"Error in battle participation setup: {ex.Message}");
							}
						}
						else
						{
							// Player is already in the correct army - no action needed
							// The native system should collect them into the battle automatically
							ModLogger.Debug("Battle", "Player already in lord's army - no action needed");
						}
					}
				// Check battle state for both the lord individually and their army
				// The MapEvent property exists on Party, not directly on MobileParty
				// This is the correct API structure for checking battle state
				bool lordInBattle = lordParty.Party.MapEvent != null;
				bool armyInBattle = lordParty.Army?.LeaderParty?.Party.MapEvent != null;
				bool anyBattleActive = lordInBattle || armyInBattle;
				
				// Check if the player is already in a battle
				bool playerInBattle = mainParty?.Party.MapEvent != null;
				
				// Check for siege state (both BesiegedSettlement and SiegeEvent)
				// During sieges, the player needs to be active/visible even before the assault starts
				// to allow siege menus to appear
				bool lordInSiege = lordHasBesiegedSettlement || lordParty.Party.SiegeEvent != null;
				bool playerInSiege = mainParty?.BesiegedSettlement != null || mainParty?.Party.SiegeEvent != null;
				
				// Keep the player's party position matched to the lord's position
				// This ensures the player follows the lord during travel
				// Using direct position assignment avoids AI escort complications
				mainParty.Position2D = lordParty.Position2D;
				
				// Determine if the player needs to be active for battle participation
				// The player should be activated when a battle is starting but they haven't joined yet
				// The native system collects nearby active parties when a battle starts
				bool playerNeedsToBeActiveForBattle = anyBattleActive && !playerInBattle;
				
				// During sieges, keep the player active and visible so siege menus can appear
				if (lordInSiege || playerInSiege)
				{
					// Siege active - keep player active and visible for siege menus
					if (!mainParty.IsActive)
					{
						mainParty.IsActive = true;
					}
					mainParty.IsVisible = true;
					
					// Ensure player is in the army if lord is in an army (for siege menus)
					if (lordParty.Army != null && (mainParty.Army == null || mainParty.Army != lordParty.Army))
					{
						try
						{
							lordParty.Army.AddPartyToMergedParties(mainParty);
							mainParty.Army = lordParty.Army;
							ModLogger.Debug("Siege", $"Added player to army for siege participation (Army Leader: {lordParty.Army.LeaderParty?.LeaderHero?.Name})");
						}
						catch (Exception ex)
						{
							ModLogger.Error("Siege", $"Error adding player to army during siege: {ex.Message}");
						}
					}
					
					// If the player enters a settlement while having an active PlayerEncounter, finish it immediately
					// This prevents assertion failures that can occur when encounters persist after settlement entry
					// Check both InsideSettlement and CurrentSettlement to ensure we catch all cases
					if (PlayerEncounter.Current != null && !PlayerEncounter.InsideSettlement)
					{
						if (mainParty?.CurrentSettlement != null)
						{
							try
							{
								ModLogger.Info("Siege", "Player entered settlement with active PlayerEncounter - finishing to prevent assertion");
								PlayerEncounter.Finish(true);
							}
							catch (Exception ex)
							{
								ModLogger.Error("Siege", $"Error finishing PlayerEncounter when entering settlement: {ex.Message}");
							}
						}
					}
					
					// Create a PlayerEncounter for sieges if one doesn't exist yet
					// This allows siege menus to appear and the player to participate in sieges
					// Don't create encounters while inside a settlement, as this causes assertion failures
					// Check both InsideSettlement and CurrentSettlement to be safe
					if (PlayerEncounter.Current == null && lordInSiege && 
					    mainParty?.CurrentSettlement == null && !PlayerEncounter.InsideSettlement)
					{
						try
						{
							var encounteredParty = lordParty.Army?.LeaderParty?.Party ?? lordParty.Party;
							EncounterManager.StartPartyEncounter(mainParty.Party, encounteredParty);
							ModLogger.Info("Siege", $"Created PlayerEncounter for siege menu (Encountered: {encounteredParty?.MobileParty?.LeaderHero?.Name})");
						}
						catch (Exception ex)
						{
							ModLogger.Error("Siege", $"Error creating PlayerEncounter for siege: {ex.Message}");
						}
					}
					else if (mainParty?.CurrentSettlement != null)
					{
						ModLogger.Debug("Siege", "Skipped PlayerEncounter creation - player inside settlement");
					}
					
					ModLogger.Debug("Siege", $"Player active/visible for siege at {(lordParty.BesiegedSettlement?.Name?.ToString() ?? "unknown")}");
				}
				else if (!playerNeedsToBeActiveForBattle)
				{
					// No battle active, or player already in battle
					if (playerInBattle)
					{
						// Player already in battle - ensure visible/active but don't change state
						mainParty.IsVisible = true;
						ModLogger.Debug("Battle", "Player already in battle - maintaining active state");
					}
					else
					{
						// When the lord is in an army, the player must be active to be collected into battles
						// The game's battle collection system checks for active parties before or during MapEvent creation
						// If the player is inactive, they won't be included in the battle even if they're in the army
						// Army membership prevents random encounters, but the player needs to be active for battle collection
						if (lordInArmy && mainParty.Army == lordParty.Army)
						{
							// Lord is in an army and the player is in the same army
							// Keep the player active and visible so they can be collected into battles
							// This ensures the native system can collect the player when battles start
							if (!mainParty.IsActive)
							{
								mainParty.IsActive = true;
								mainParty.IsVisible = true;
								mainParty.Party.SetAsCameraFollowParty();
								TrySetShouldJoinPlayerBattles(mainParty, true);
								ModLogger.Debug("Battle", $"Activated player party for battle collection (lord in army, player in same army)");
							}
							else if (!mainParty.IsVisible)
							{
								// Already active, just ensure visible
								mainParty.IsVisible = true;
							}
						}
						else
						{
							// Lord is not in an army - deactivate the player to prevent random encounters
							// Normal hidden state when not in battle or siege
							mainParty.IsVisible = false;
							if (mainParty.IsActive)
							{
								mainParty.IsActive = false;
							}
						}
					}
				}
				else
				{
					// Lord is in battle but the player hasn't joined yet
					// Activate the player so the native system can collect them into the battle
					// This works whether the player is in an army or just following the lord
					mainParty.IsVisible = true;
					
					// Ensure the player is in the army if the lord is in an army
					// The native system needs the player to be in the army to show the "join battle" option
					if (lordParty.Army != null && (mainParty.Army == null || mainParty.Army != lordParty.Army))
					{
						try
						{
							lordParty.Army.AddPartyToMergedParties(mainParty);
							mainParty.Army = lordParty.Army;
							ModLogger.Debug("Battle", $"Added player to army for battle participation (Army Leader: {lordParty.Army.LeaderParty?.LeaderHero?.Name})");
						}
						catch (Exception ex)
						{
							ModLogger.Error("Battle", $"Error adding player to army in realtime tick: {ex.Message}");
						}
					}
					
					// Only activate if the party doesn't already have a MapEvent (safe to activate)
					// The MapEvent property exists on Party, not directly on MobileParty
					// This is the correct API structure for checking battle state
					if (mainParty.Party.MapEvent == null && !mainParty.IsActive)
					{
						mainParty.IsActive = true;
						// Ensure battle participation flags are set so the player can join battles
						mainParty.Party.SetAsCameraFollowParty();
						TrySetShouldJoinPlayerBattles(mainParty, true);
						ModLogger.Debug("Battle", $"Activated party for battle collection (lord in battle, player in army: {playerInArmy})");
					}
				}
			
				// Configure party ignoring behavior to prevent unwanted encounters
				// The party should ignore other parties when not in battle, but allow encounters
				// when a battle involving the lord is active (so the player can be collected)
				// The MapEvent property exists on Party, not directly on MobileParty
				bool lordHasEvent = lordParty?.Party.MapEvent != null || lordParty?.Army?.LeaderParty?.Party.MapEvent != null;
				if (!lordHasEvent)
				{
					// No battle active - ignore other parties for 0.5 hours to prevent random encounters
					mainParty.IgnoreByOtherPartiesTill(CampaignTime.Now + CampaignTime.Hours(0.5f));
					ModLogger.Debug("Realtime", "Ignoring other parties - no lord battles active");
				}
				else
				{
					// Battle active - don't ignore parties so the player can be collected into the battle
					ModLogger.Debug("Realtime", "NOT ignoring parties - lord battle active, allowing encounter collection");
				}
			}
		}
	}
		
		#region Event Handlers for Lord Status Changes
		
		/// <summary>
		/// Handles the case when the enlisted lord is killed in battle or other circumstances.
		/// Automatically discharges the player from service and restores their normal state.
		/// Called by the game when any hero is killed.
		/// </summary>
		/// <param name="victim">The hero that was killed.</param>
		/// <param name="killer">The hero that killed the victim (may be null).</param>
		/// <param name="detail">Details about how the hero was killed.</param>
		/// <param name="showNotification">Whether to show a notification to the player.</param>
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
		/// Handles the case when the enlisted lord is defeated in battle.
		/// Checks if the lord was killed or captured and discharges the player accordingly.
		/// Called by the game when a character is defeated in battle.
		/// </summary>
		/// <param name="defeatedHero">The hero that was defeated.</param>
		/// <param name="victorHero">The hero that defeated the victim (may be null).</param>
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
		/// Set tier directly (called by PromotionBehavior for immediate promotion).
		/// </summary>
		public void SetTier(int tier)
		{
			if (!IsEnlisted || tier < 1 || tier > 6)
			{
				return;
			}
			
			_enlistmentTier = tier;
		}

		/// <summary>
		/// Change selected duty (called from duty selection menu).
		/// </summary>
		public void SetSelectedDuty(string dutyId)
		{
			if (!IsEnlisted || string.IsNullOrEmpty(dutyId))
			{
				return;
			}
			
			// Remove previous duty if different
			if (_selectedDuty != dutyId && !string.IsNullOrEmpty(_selectedDuty))
			{
				var duties = Features.Assignments.Behaviors.EnlistedDutiesBehavior.Instance;
				if (duties != null)
				{
					duties.RemoveDuty(_selectedDuty);
				}
			}
			
			_selectedDuty = dutyId;
			
			// Add the new duty to active duties for daily XP processing
			var dutiesBehavior = Features.Assignments.Behaviors.EnlistedDutiesBehavior.Instance;
			if (dutiesBehavior != null)
			{
				dutiesBehavior.AssignDuty(dutyId);
			}
			
			ModLogger.Info("Duties", $"Changed duty to: {dutyId}");
		}

		/// <summary>
		/// Change selected profession (called from duty selection menu).
		/// </summary>
		public void SetSelectedProfession(string professionId)
		{
			if (!IsEnlisted || string.IsNullOrEmpty(professionId))
			{
				return;
			}
			
			// Remove previous profession if different and not "none"
			if (_selectedProfession != professionId && _selectedProfession != "none" && !string.IsNullOrEmpty(_selectedProfession))
			{
				var duties = Features.Assignments.Behaviors.EnlistedDutiesBehavior.Instance;
				if (duties != null)
				{
					duties.RemoveDuty(_selectedProfession);
				}
			}
			
			_selectedProfession = professionId;
			
			// Add the new profession to active duties for daily XP processing (skip "none")
			// "none" remains as default internal value but isn't an active duty
			if (professionId != "none")
			{
				var dutiesBehavior = Features.Assignments.Behaviors.EnlistedDutiesBehavior.Instance;
				if (dutiesBehavior != null)
				{
					dutiesBehavior.AssignDuty(professionId);
				}
			}
			
			ModLogger.Info("Duties", $"Changed profession to: {professionId}");
		}

		/// <summary>
		/// Check if promotion notification should be triggered after XP gain.
		/// Now integrates with Phase 2B troop selection system.
		/// </summary>
		private void CheckPromotionNotification(int previousXP, int currentXP)
		{
			try
			{
				// Load tier XP requirements from progression_config.json
				var tierXPRequirements = Assignments.Core.ConfigurationManager.GetTierXPRequirements();
				
				// Bounds check: ensure we don't go beyond array limits
				if (_enlistmentTier < 0 || _enlistmentTier >= tierXPRequirements.Length)
				{
					ModLogger.Debug("Progression", $"Skipping promotion check - tier {_enlistmentTier} out of bounds");
					return;
				}
				
				// Get max tier from config to prevent exceeding maximum
				int maxTier = tierXPRequirements.Length > 1 ? tierXPRequirements.Length - 1 : 1;
				
				// Check if we crossed any promotion threshold
				// Fix: Prevent checking beyond max tier to maintain consistency with SetTier validation
				for (int tier = _enlistmentTier; tier < maxTier && tier >= 0; tier++)
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
		/// Set initial formation for new recruits based on their assigned equipment.
		/// </summary>
		private void SetInitialFormation()
		{
			try
			{
				if (_enlistedLord?.Culture?.BasicTroop == null)
				{
					// Fallback: Set to infantry for all new recruits
					EnlistedDutiesBehavior.Instance?.SetPlayerFormation("infantry");
					ModLogger.Info("Enlistment", "Set initial formation to infantry (fallback)");
					return;
				}
				
				// Analyze the basic troop to determine formation
				var basicTroop = _enlistedLord.Culture.BasicTroop;
				string initialFormation = "infantry"; // Default for recruits
				
				// Most culture basic troops are infantry, but check just in case
				if (basicTroop.IsRanged && basicTroop.IsMounted)
				{
					initialFormation = "horsearcher";
				}
				else if (basicTroop.IsMounted)
				{
					initialFormation = "cavalry";
				}
				else if (basicTroop.IsRanged)
				{
					initialFormation = "archer";
				}
				else
				{
					initialFormation = "infantry";
				}
				
				// Set the formation in duties system
				EnlistedDutiesBehavior.Instance?.SetPlayerFormation(initialFormation);
				
				ModLogger.Info("Enlistment", $"Set initial formation to {initialFormation} based on {basicTroop.Name}");
			}
			catch (Exception ex)
			{
				// Fallback to infantry on any error
				ModLogger.Error("Enlistment", "Error setting initial formation", ex);
				EnlistedDutiesBehavior.Instance?.SetPlayerFormation("infantry");
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
				// CORRECT API: Use Party.MapEvent (not direct on MobileParty)
				bool lordInBattle = lordParty.Party.MapEvent != null;
				bool playerInBattle = main.Party.MapEvent != null;
				
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
					main.IsActive = false;  // Disable party activity to prevent random encounters
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
				
			// Transfer any new companions/troops to lord's party
			TransferPlayerTroopsToLord();
			
			// Finish any active encounter first (prevents assertion crashes during state transitions)
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
				
				// Activate enlisted status menu immediately to prevent encounter gaps
				Enlisted.Features.Interface.Behaviors.EnlistedMenuBehavior.SafeActivateEnlistedMenu();
				ModLogger.Info("Enlistment", "Service resumed - attempted enlisted status menu activation (with global guard)");
			}
			catch (Exception ex)
			{
				ModLogger.Error("Enlistment", "Error returning from leave", ex);
			}
		}
		
		/// <summary>
		/// Transfer player companions and troops to lord's party.
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
				
				// Transfer all non-player troops to lord's party
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
				
				// Perform the transfer using native party roster methods
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

		/// <summary>
		/// Army-aware battle participation logic.
		/// Handles both individual lord battles and army battles with proper state management.
		/// </summary>
		private void HandleArmyBattleParticipation(MobileParty main, MobileParty lordParty)
		{
			try
			{
				// Check if lord is part of an army
				var lordArmy = lordParty.Army;
				var armyLeader = lordArmy?.LeaderParty;
				
				// Determine the effective battle party (army leader or individual lord)
				var battleParty = armyLeader ?? lordParty;
				// CORRECT API: Use Party.MapEvent (not direct on MobileParty)
				var isBattlePartyInCombat = battleParty.Party.MapEvent != null;
				var isPlayerInBattle = main.Party.MapEvent != null;

				if (isBattlePartyInCombat && !isPlayerInBattle)
				{
					// Battle detected, activate player party for participation
					if (lordArmy != null)
					{
						ModLogger.Info("Battle", $"ARMY BATTLE DETECTED - Army Leader: {armyLeader?.LeaderHero?.Name}, Lord: {_enlistedLord.Name}");
						ModLogger.Debug("Battle", $"Army details - Army size: {lordArmy.Parties.Count}, Battle party: {battleParty.LeaderHero?.Name}");
						HandleArmyBattle(main, lordParty, lordArmy);
					}
					else
					{
						ModLogger.Info("Battle", $"Individual lord battle detected - Lord: {_enlistedLord.Name}");
						HandleIndividualBattle(main, lordParty);
					}
				}
				else if (!isBattlePartyInCombat && isPlayerInBattle)
				{
					// Battle ended, clean up
					ModLogger.Info("Battle", "Battle ended, returning to enlisted state");
					HandlePostBattleCleanup(main, lordParty);
				}
			}
			catch (Exception ex)
			{
				ModLogger.Error("Battle", $"Error in army battle participation: {ex.Message}");
			}
		}

		/// <summary>
		/// Handle army battle participation.
		/// Uses army membership for battle participation, keeps player party inactive to avoid conflicts.
		/// </summary>
		private void HandleArmyBattle(MobileParty main, MobileParty lordParty, Army lordArmy)
		{
			try
			{
				// Don't activate player party - use army membership for battle participation
				// The player participates through being part of the lord's army, not as independent party
				ModLogger.Info("Battle", "Army battle detected, player participates through army membership");
				
				// Ensure player is positioned with army for battle camera/interface
				main.Position2D = lordParty.Position2D;
			}
			catch (Exception ex)
			{
				ModLogger.Error("Battle", $"Error handling army battle: {ex.Message}");
			}
		}

		/// <summary>
		/// Handle individual lord battle participation.
		/// Creates temporary army for individual battles when needed.
		/// </summary>
		private void HandleIndividualBattle(MobileParty main, MobileParty lordParty)
		{
			try
			{
				// For individual lord battles, create temporary army if needed
				if (lordParty.Army == null)
				{
					ModLogger.Info("Battle", "Creating temporary army for individual lord battle");
					// Create temporary army so player can participate through army mechanics
					var kingdom = lordParty.ActualClan?.Kingdom;
					if (kingdom != null)
					{
						kingdom.CreateArmy(lordParty.LeaderHero, lordParty.HomeSettlement, Army.ArmyTypes.Patrolling);
						_disbandArmyAfterBattle = true; // Track that we created this army
						ModLogger.Debug("Battle", "Temporary army created for battle participation");
					}
				}
				
				// Keep player positioned with lord
				main.Position2D = lordParty.Position2D;
			}
			catch (Exception ex)
			{
				ModLogger.Error("Battle", $"Error handling individual battle: {ex.Message}");
			}
		}

		/// <summary>
		/// Clean up after battle ends.
		/// Disbands temporary armies if we created them.
		/// </summary>
		private void HandlePostBattleCleanup(MobileParty main, MobileParty lordParty)
		{
			try
			{
				// Disband temporary army if we created it
				if (_disbandArmyAfterBattle && lordParty.Army != null)
				{
					ModLogger.Info("Battle", "Disbanding temporary army after battle");
					var army = lordParty.Army;
					DisbandArmyAction.ApplyByUnknownReason(army);
					_disbandArmyAfterBattle = false;
					ModLogger.Debug("Battle", "Temporary army disbanded");
				}
				
				// Return to standard hidden enlisted state (always inactive/invisible)
				main.IsActive = false;
				main.IsVisible = false;
				ModLogger.Info("Battle", "Player returned to hidden enlisted state after battle");
			}
			catch (Exception ex)
			{
				ModLogger.Error("Battle", $"Error in post-battle cleanup: {ex.Message}");
			}
		}

		/// <summary>
		/// MINIMAL: Settlement entry detection for menu refresh only.
		/// Just refreshes the menu when lord enters settlements - no complex logic.
		/// </summary>
		private void OnSettlementEntered(MobileParty party, Settlement settlement, Hero hero)
		{
			try
			{
				if (!IsEnlisted || _isOnLeave)
				{
					return;
				}

		// Only react when OUR enlisted lord enters a settlement
		if (hero == _enlistedLord)
		{
			ModLogger.Info("Settlement", $"Lord {hero.Name} entered {settlement.Name} ({settlement.StringId})");
			
			// CRITICAL: Finish any active PlayerEncounter before entering settlement
			// This prevents InsideSettlement assertion failures when the army enters a settlement
			// Party encounters cannot exist while inside settlements
			// According to API: StartPartyEncounter cannot be called when InsideSettlement is true
			// We should finish party encounters when entering settlements to prevent assertion at line 2080
			var mainParty = MobileParty.MainParty;
			if (PlayerEncounter.Current != null && !PlayerEncounter.InsideSettlement)
			{
				// Settlement entry event means we're entering - finish party encounter immediately
				// This prevents the assertion that checks InsideSettlement at PlayerEncounter.cs:2080
				try
				{
					ModLogger.Info("Settlement", "Finishing PlayerEncounter before entering settlement to prevent InsideSettlement assertion");
					PlayerEncounter.Finish(true);
				}
				catch (Exception ex)
				{
					ModLogger.Error("Settlement", $"Error finishing PlayerEncounter before settlement entry: {ex.Message}");
				}
			}
			
			// Only pause/activate menu for towns and castles, not villages
			// Villages should allow continuous time flow while following the lord
			if (settlement.IsTown || settlement.IsCastle)
			{
			// CRITICAL GUARD: Don't interfere with battle/siege encounter menus
			// CORRECT API: Use Party.MapEvent (not direct on MobileParty)
			bool inBattleOrSiege = (MobileParty.MainParty?.Party.MapEvent != null) ||
			                      (PlayerEncounter.Current != null) ||
			                      (_enlistedLord?.PartyBelongedTo?.BesiegedSettlement != null);
				                      
				if (!inBattleOrSiege)
				{
					// Safe to show enlisted menu - no battles/sieges active
					EnlistedMenuBehavior.SafeActivateEnlistedMenu();
					ModLogger.Debug("Settlement", $"Activated enlisted menu for {settlement.Name} (town/castle)");
				}
				else
				{
					ModLogger.Info("Settlement", $"GUARDED: Skipped enlisted menu activation - battle/siege active ({settlement.Name})");
				}
			}
			else
			{
				// Villages: just log but don't pause or activate menu
				ModLogger.Debug("Settlement", $"Lord entered village {settlement.Name} - continuing time flow");
			}
		}
			}
			catch (Exception ex)
			{
				ModLogger.Error("Settlement", $"Error in settlement entry detection: {ex.Message}");
			}
		}
		
		/// <summary>
		/// REVERT: Handle battle start events to inject player into lord's battles.
		/// Back to MapEventStarted since BattleStarted doesn't seem to exist in v1.2.12.
		/// </summary>
		private void OnMapEventStarted(MapEvent mapEvent, PartyBase attackerParty, PartyBase defenderParty)
		{
			try
			{
				if (!IsEnlisted || _isOnLeave || _enlistedLord == null)
				{
					return;
				}
				
				var lordParty = _enlistedLord.PartyBelongedTo;
				if (lordParty == null)
				{
					return;
				}
				
				// DEBUG: Log battle details to understand detection failure
				ModLogger.Debug("Battle", $"BattleStarted - Attacker: {attackerParty?.MobileParty?.LeaderHero?.Name}, Defender: {defenderParty?.MobileParty?.LeaderHero?.Name}");
				ModLogger.Debug("Battle", $"Our Lord: {_enlistedLord?.Name}, Lord Party: {lordParty?.LeaderHero?.Name}");
				ModLogger.Debug("Battle", $"Lord Army: {lordParty?.Army?.LeaderParty?.LeaderHero?.Name}, Army Size: {lordParty?.Army?.Parties?.Count ?? 0}");
				
				// Check if our lord is involved in this battle
				// Use Party property for comparison (PartyBase.MobileParty vs MobileParty)
				bool lordIsAttacker = attackerParty?.MobileParty == lordParty || attackerParty == lordParty?.Party;
				bool lordIsDefender = defenderParty?.MobileParty == lordParty || defenderParty == lordParty?.Party;
				
				// ENHANCED: Comprehensive army detection for all scenarios
				bool lordInArmy = false;
				if (lordParty.Army != null)
				{
					var army = lordParty.Army;
					var armyLeader = army.LeaderParty;
					
					// Check 1: Is our army leader involved as attacker or defender?
					bool armyLeaderInvolved = (attackerParty?.MobileParty == armyLeader) || (defenderParty?.MobileParty == armyLeader) ||
					                          (attackerParty == armyLeader?.Party) || (defenderParty == armyLeader?.Party);
					
					// Check 2: Is our lord directly involved (even if not army leader)?  
					bool lordDirectlyInvolved = (attackerParty?.MobileParty == lordParty) || (defenderParty?.MobileParty == lordParty) ||
					                            (attackerParty == lordParty?.Party) || (defenderParty == lordParty?.Party);
					
					// Check 3: Are any army members involved?
					bool anyArmyMemberInvolved = false;
					if (army.Parties != null)
					{
						foreach (var armyParty in army.Parties)
						{
							if (armyParty == attackerParty?.MobileParty || armyParty == defenderParty?.MobileParty ||
							    armyParty?.Party == attackerParty || armyParty?.Party == defenderParty)
							{
								anyArmyMemberInvolved = true;
								break;
							}
						}
					}
					
					// Check 4: Check MapEvent parties directly (most reliable)
					if (mapEvent != null)
					{
						try
						{
							// Use PartiesOnSide method which is more reliable
							var attackerParties = mapEvent.PartiesOnSide(BattleSideEnum.Attacker);
							var defenderParties = mapEvent.PartiesOnSide(BattleSideEnum.Defender);
							
							if (attackerParties != null)
							{
								foreach (var eventParty in attackerParties)
								{
									if (eventParty?.Party?.MobileParty == lordParty || eventParty?.Party == lordParty?.Party)
									{
										anyArmyMemberInvolved = true;
										break;
									}
								}
							}
							if (!anyArmyMemberInvolved && defenderParties != null)
							{
								foreach (var eventParty in defenderParties)
								{
									if (eventParty?.Party?.MobileParty == lordParty || eventParty?.Party == lordParty?.Party)
									{
										anyArmyMemberInvolved = true;
										break;
									}
								}
							}
						}
						catch (Exception ex)
						{
							ModLogger.Debug("Battle", $"Error checking MapEvent parties: {ex.Message}");
						}
					}
					
					lordInArmy = armyLeaderInvolved || lordDirectlyInvolved || anyArmyMemberInvolved;
					
					ModLogger.Debug("Battle", $"Army analysis - Army Leader: {armyLeader?.LeaderHero?.Name}");
					ModLogger.Debug("Battle", $"Army checks - Leader involved: {armyLeaderInvolved}, Lord direct: {lordDirectlyInvolved}, Any member: {anyArmyMemberInvolved}");
					ModLogger.Debug("Battle", $"Final army result: {lordInArmy}");
				}
				
				// CRITICAL: Check if this is a siege battle
				bool isSiegeBattle = mapEvent?.IsSiegeAssault == true || mapEvent?.EventType == MapEvent.BattleTypes.Siege;
				
				// CRITICAL: Check if this is a village raid (player shouldn't join these, just observe)
				// Village raids have special encounter handling and shouldn't get PlayerEncounter created
				bool isVillageRaid = mapEvent?.EventType == MapEvent.BattleTypes.Raid || 
				                     (mapEvent?.EventType != null && mapEvent.EventType.ToString().Contains("Raid"));
				
				ModLogger.Debug("Battle", $"Detection results - Attacker: {lordIsAttacker}, Defender: {lordIsDefender}, Army: {lordInArmy}, Siege: {isSiegeBattle}, VillageRaid: {isVillageRaid}");
				
				// CRITICAL: Skip village raids - they have their own encounter system and shouldn't be interfered with
				if (isVillageRaid)
				{
					ModLogger.Info("Battle", "Village raid detected - skipping PlayerEncounter creation (native system handles village raid encounters)");
					return; // Let native system handle village raids completely
				}
				
				if (lordIsAttacker || lordIsDefender || lordInArmy)
				{
					ModLogger.Info("Battle", $"MAPEVENTS PATTERN: Lord battle starting, preparing player for collection (Attacker: {lordIsAttacker}, Defender: {lordIsDefender}, Army: {lordInArmy}, Siege: {isSiegeBattle})");
					
					// CRITICAL: Exit custom menus so native army_wait/siege menu can appear
					// This must happen BEFORE we configure the party
					// BUT: Don't exit siege menus - let the native system handle siege menu transitions
					var currentMenu = Campaign.Current?.CurrentMenuContext?.GameMenu?.StringId;
					if (currentMenu != null && currentMenu.StartsWith("enlisted_"))
					{
						// Only exit custom menus, not native siege menus
						if (!currentMenu.Contains("siege") && !isSiegeBattle)
						{
							ModLogger.Info("Battle", "Exiting custom menu to allow native army_wait menu");
							try
							{
								GameMenu.ExitToLast();
							}
							catch (Exception ex)
							{
								ModLogger.Debug("Battle", $"Error exiting menu: {ex.Message}");
							}
						}
						else
						{
							ModLogger.Debug("Battle", "Preserving siege menu - not exiting custom menu");
						}
					}
					
					var main = MobileParty.MainParty;
					if (main != null)
					{
						// CRITICAL: Handle both army and non-army cases
						// If lord has an army: add player to army (for army_wait menu)
						// If lord has NO army: still activate player so native system can collect them into battle
						var targetArmy = lordParty.Army;
						if (targetArmy != null && (main.Army == null || main.Army != targetArmy))
						{
							ModLogger.Info("Battle", "Adding player to lord's army for battle menu");
							try
							{
								targetArmy.AddPartyToMergedParties(main);
								main.Army = targetArmy;
								ModLogger.Debug("Battle", $"Player added to army for battle menu (Army Leader: {targetArmy.LeaderParty?.LeaderHero?.Name})");
							}
							catch (Exception ex)
							{
								ModLogger.Error("Battle", $"Error adding player to army: {ex.Message}");
							}
						}
						else if (targetArmy == null)
						{
							// Lord has no army - player is just following
							// Still need to activate so native battle collection can pull them in
							ModLogger.Info("Battle", "Lord has no army - activating player for direct battle collection");
						}
						
						// CRITICAL: Create PlayerEncounter so GetGenericStateMenu() can detect the battle
						// This allows the "encounter" menu (with "join battle" option) to appear instead of just "army_wait"
						// This works for both regular battles and siege battles
						// CRITICAL: Don't create encounters while inside a settlement (causes assertion failures)
						// Check both InsideSettlement and CurrentSettlement to be safe
						if (PlayerEncounter.Current == null && mapEvent != null && 
						    main?.CurrentSettlement == null && !PlayerEncounter.InsideSettlement)
						{
							try
							{
								// Determine the encountered party: army leader if in army, otherwise the lord
								var encounteredParty = targetArmy?.LeaderParty?.Party ?? lordParty.Party;
								
								// Create PlayerEncounter with the army leader/lord as encountered party
								// When that party has a MapEvent, PlayerEncounter.EncounteredBattle will return it
								// This allows DefaultEncounterGameMenuModel.GetGenericStateMenu() to detect the battle
								EncounterManager.StartPartyEncounter(main.Party, encounteredParty);
								ModLogger.Info("Battle", $"Created PlayerEncounter for {(isSiegeBattle ? "siege" : "army")} battle menu (Encountered: {encounteredParty?.MobileParty?.LeaderHero?.Name})");
							}
							catch (Exception ex)
							{
								ModLogger.Error("Battle", $"Error creating PlayerEncounter: {ex.Message}");
							}
						}
						else if (main?.CurrentSettlement != null)
						{
							ModLogger.Debug("Battle", "Skipped PlayerEncounter creation - player inside settlement");
						}
						
						// Cancel any ignore window so we can be collected by the battle sweep
						main.IgnoreByOtherPartiesTill(CampaignTime.Now); // effectively clear
						ModLogger.Debug("Battle", "Cleared ignore window for battle collection");
						
						// Make sure we're not attached and not escorting
						if (main.AttachedTo != null) 
						{
							main.AttachedTo = null;
							ModLogger.Debug("Battle", "Cleared attachment for battle participation");
						}
						TryReleaseEscort(main);
						
						// Position at lord's position (within join radius of 3.0f)
						main.Position2D = lordParty.Position2D;
						main.IsVisible = true;
						
						// CRITICAL: Only activate if player party doesn't have MapEvent yet (safe to activate)
						// This allows native system to show army_wait menu
						// Setting IsActive while MapEvent exists causes MobilePartyAi assertion failures
						if (main.Party.MapEvent == null && !main.IsActive)
						{
							main.IsActive = true;
							ModLogger.Debug("Battle", "Activated party for battle menu (no MapEvent yet, safe to activate)");
						}
						else if (main.Party.MapEvent != null)
						{
							ModLogger.Debug("Battle", "Party already has MapEvent - skipping activation to prevent assertion");
						}
						
						// Ensure battle participation flags are set
						main.Party.SetAsCameraFollowParty();
						TrySetShouldJoinPlayerBattles(main, true);
						
						ModLogger.Debug("Battle", $"Positioned at lord party, made visible and active for battle menu");
						ModLogger.Info("Battle", "MAPEVENTS TIMING: Player party prepared for battle participation and menu");
					}
				}
				else
				{
					ModLogger.Debug("Battle", "Not our lord's battle, ignoring");
				}
			}
			catch (Exception ex)
			{
				ModLogger.Error("Battle", $"Error in BattleStarted handler: {ex.Message}");
			}
		}
		
		/// <summary>
		/// Handle battle end events to return player to hidden state.
		/// </summary>
		private void OnMapEventEnded(MapEvent mapEvent)
		{
			try
			{
				if (!IsEnlisted || _isOnLeave || _enlistedLord == null)
				{
					return;
				}
				
				var lordParty = _enlistedLord.PartyBelongedTo;
				if (lordParty == null)
				{
					return;
				}
				
				// NULL-SAFE: Check if this was our lord's battle that ended
				if (mapEvent?.InvolvedParties != null && 
					mapEvent.InvolvedParties.Any(p => p?.MobileParty == lordParty ||
						(p?.MobileParty?.Army != null && p.MobileParty.Army == lordParty.Army)))
				{
					ModLogger.Info("Battle", "Lord battle ended, returning to hidden state");
					
					// Return to hidden enlisted state
					var main = MobileParty.MainParty;
					main.IsActive = false;
					main.IsVisible = false;
					
					// Disband temporary army if we created one
					if (_disbandArmyAfterBattle && lordParty.Army != null)
					{
						var army = lordParty.Army;
						DisbandArmyAction.ApplyByUnknownReason(army);
						_disbandArmyAfterBattle = false;
						ModLogger.Debug("Battle", "Disbanded temporary army");
					}
					
					ModLogger.Info("Battle", "Player returned to hidden enlisted state");
				}
			}
			catch (Exception ex)
			{
				ModLogger.Error("Battle", $"Error in MapEvent end handler: {ex.Message}");
			}
		}
		
		/// <summary>
		/// Debug tracking for PlayerBattleEnd events.
		/// </summary>
		private void OnPlayerBattleEnd(MapEvent mapEvent)
		{
			try
			{
				ModLogger.Info("Battle", $"=== PLAYER BATTLE END ===");
				ModLogger.Info("Battle", $"Battle Type: {mapEvent?.EventType}, Was Siege: {mapEvent?.IsSiegeAssault}");
				ModLogger.Info("Battle", $"Player was in army: {(MobileParty.MainParty?.Army?.LeaderParty?.LeaderHero?.Name?.ToString() ?? "null")}");
				ModLogger.Info("Battle", $"PlayerEncounter state: Active={PlayerEncounter.IsActive}, Current={PlayerEncounter.Current != null}");
			}
			catch (Exception ex)
			{
				ModLogger.Error("Battle", $"Error in OnPlayerBattleEnd: {ex.Message}");
			}
		}
		
		/// <summary>
		/// Siege watchdog to prepare party for vanilla encounter creation when siege begins.
		/// This is the missing piece - making party encounter-eligible so vanilla siege menus can appear.
		/// </summary>
		private void SiegeWatchdogTick(MobileParty mainParty, MobileParty lordParty)
		{
			try
			{
				if (mainParty == null || lordParty == null) return;
				
				// This is the pre-assault state: arriving / laying siege
				bool siegeForming = lordParty.BesiegedSettlement != null || lordParty.Party.SiegeEvent != null;
				
				if (!siegeForming) return;
				
				// If we're already in an encounter/menu, do nothing
				if (PlayerEncounter.Current != null || mainParty.Party.MapEvent != null) return;
				
				ModLogger.Info("Siege", $"=== SIEGE WATCHDOG: Siege forming at {(lordParty.BesiegedSettlement?.Name?.ToString() ?? "unknown")} ===");
				
				// Prepare for vanilla to collect us
				TryReleaseEscort(mainParty);
				mainParty.IgnoreByOtherPartiesTill(CampaignTime.Now);    // clear ignore window
				mainParty.Position2D = lordParty.Position2D;             // be in radius
				mainParty.IsVisible = true;
				mainParty.IsActive = true;
				TrySetShouldJoinPlayerBattles(mainParty, true);
				
				// IMPORTANT: do NOT push our own menu here. Let vanilla push army_wait / siege menu.
				ModLogger.Info("Siege", "Prepared player for siege encounter (active/visible & co-located) - vanilla should create encounter menu");
			}
			catch (Exception ex)
			{
				ModLogger.Error("Siege", $"Error in siege watchdog: {ex.Message}");
			}
		}
		
		#endregion
	}
}


