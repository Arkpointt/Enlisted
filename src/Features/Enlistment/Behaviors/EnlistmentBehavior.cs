using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Enlisted.Features.Assignments.Behaviors;
using Enlisted.Features.Assignments.Core;
using Enlisted.Features.Combat.Behaviors;
using Enlisted.Features.CommandTent.Core;
using Enlisted.Features.Equipment.Behaviors;
using Enlisted.Features.Interface.Behaviors;
using Enlisted.Mod.Core;
using Enlisted.Mod.Core.Logging;
using Enlisted.Mod.Entry;
using Enlisted.Mod.GameAdapters.Patches;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using EnlistedConfig = Enlisted.Features.Assignments.Core.ConfigurationManager;

namespace Enlisted.Features.Enlistment.Behaviors
{
    /// <summary>
    ///     Detailed breakdown of daily wage components for tooltip display.
    ///     Each field represents a separate line item in the finance tooltip.
    /// </summary>
    public class WageBreakdown
    {
        /// <summary>Base soldier's pay (from config base_wage).</summary>
        public int BasePay { get; set; }

        /// <summary>Bonus from player's character level.</summary>
        public int LevelBonus { get; set; }

        /// <summary>Bonus from military rank/tier.</summary>
        public int TierBonus { get; set; }

        /// <summary>Bonus from accumulated service time (XP-based seniority).</summary>
        public int ServiceBonus { get; set; }

        /// <summary>Bonus for serving in an active army campaign.</summary>
        public int ArmyBonus { get; set; }

        /// <summary>Bonus from active duty assignment.</summary>
        public int DutyBonus { get; set; }

        /// <summary>Whether currently in an army.</summary>
        public bool IsInArmy { get; set; }

        /// <summary>Name of active duty for display (null if none).</summary>
        public string ActiveDuty { get; set; }

        /// <summary>Final total wage after all bonuses and caps.</summary>
        public int Total { get; set; }
    }

    /// <summary>
    ///     Core behavior managing the player's enlistment in a lord's military service.
    ///     This behavior tracks the enlisted lord, manages party following and battle participation,
    ///     handles equipment backup/restoration, processes XP and promotions, and manages party
    ///     activity state to prevent unwanted encounters while allowing battle participation.
    ///     The system works by attaching the player's party to the lord's party, making the player
    ///     invisible and inactive during normal travel (preventing random encounters), but activating
    ///     the player when the lord enters battles so they can participate.
    /// </summary>
    public sealed class EnlistmentBehavior : CampaignBehaviorBase
    {
        /// <summary>
        ///     Minimum time interval between real-time tick updates, in seconds.
        ///     Updates are throttled to every 100ms to prevent overwhelming the game's
        ///     rendering system with too-frequent state changes, which can cause assertion failures.
        /// </summary>
        private readonly float _realtimeUpdateIntervalSeconds = 0.1f;

        /// <summary>
        ///     Tracks whether battle XP has been awarded for the current battle.
        ///     Prevents double XP awards when both OnPlayerBattleEnd and OnMapEventEnded fire.
        ///     Reset when a new battle starts.
        /// </summary>
        private bool _battleXPAwardedThisBattle;

        private MapEvent _cachedLordMapEvent;

        /// <summary>
        ///     Tracks whether a PlayerEncounter has been created for the current lord's battle.
        ///     Prevents repeated PlayerEncounter creation during the same battle (which causes loops).
        ///     Reset when the battle ends (OnMapEventEnded) or when the lord's MapEvent changes.
        /// </summary>
        private bool _playerEncounterCreatedForBattle;

        /// <summary>
        ///     Campaign time when the desertion grace period ends.
        ///     If current time exceeds this and player hasn't rejoined, desertion penalties apply.
        ///     Set when army is defeated or lord is captured, giving player 14 days to rejoin another lord in the same kingdom.
        /// </summary>
        private CampaignTime _desertionGracePeriodEnd = CampaignTime.Zero;

        /// <summary>
        ///     Tracks whether an army was created specifically for battle participation.
        ///     If true, the army should be disbanded after the battle completes to prevent
        ///     the player from remaining in an army when not needed.
        /// </summary>
        private bool _disbandArmyAfterBattle;

        /// <summary>
        ///     The lord the player is currently serving under, or null if not enlisted.
        /// </summary>
        private Hero _enlistedLord;

        /// <summary>
        ///     Campaign time when the player first enlisted with the current lord.
        ///     Used for calculating service duration and veteran status.
        /// </summary>
        private CampaignTime _enlistmentDate = CampaignTime.Zero;

        /// <summary>
        ///     Current military rank tier (1-6), where 1 is the lowest rank and 6 is the highest.
        ///     Determined by accumulated enlistment XP and tier thresholds from progression_config.json.
        /// </summary>
        private int _enlistmentTier = 1;

        /// <summary>
        ///     Total experience points accumulated during military service.
        ///     Gained from daily service, battle participation, duty performance, and special events.
        ///     Used to determine tier promotions based on thresholds in progression_config.json.
        /// </summary>
        private int _enlistmentXP;

        private CampaignTime _graceProtectionEnds = CampaignTime.Zero;

        /// <summary>
        ///     Whether the player's personal equipment has been backed up before enlistment.
        ///     Equipment is backed up once at the start of service and restored when service ends.
        /// </summary>
        private bool _hasBackedUpEquipment;

        /// <summary>
        ///     Counter to skip initial ticks during loading. The game's save system runs
        ///     before our tick handler is ready, so we wait a few ticks for it to settle.
        /// </summary>
        private int _initializationTicksRemaining = 5;

        /// <summary>
        ///     Whether the player is currently on temporary leave from service.
        ///     When on leave, the player is not actively enlisted but can return without
        ///     going through the full enlistment process again.
        /// </summary>
        private bool _isOnLeave;

        /// <summary>
        ///     Flag to block all IsActive modifications until post-load initialization is complete.
        ///     This prevents the realtime tick from setting IsActive during the loading process.
        ///     Defaults to false (safe) - set true only after proper initialization.
        /// </summary>
        private bool _isPartyStateInitialized;

        /// <summary>
        ///     Tracks whether the siege watchdog already prepared the player for the current siege.
        ///     Prevents the watchdog from reapplying visibility/activation every frame while time is paused.
        /// </summary>
        private bool _isSiegePreparationLatched;

        /// <summary>
        ///     Last campaign time when the real-time tick update was processed.
        ///     Used with _realtimeUpdateIntervalSeconds to throttle update frequency.
        /// </summary>
        private CampaignTime _lastRealtimeUpdate = CampaignTime.Zero;

        /// <summary>
        ///     Last campaign time when a siege PlayerEncounter was created.
        ///     Used to prevent rapid recreation of encounters that causes zero-delta-time assertion failures.
        /// </summary>
        private CampaignTime _lastSiegeEncounterCreation = CampaignTime.Zero;

        /// <summary>
        ///     Settlement for which the siege watchdog last latched preparation logic.
        ///     Used to determine when a new siege begins so the latch can be reset.
        /// </summary>
        private Settlement _latchedSiegeSettlement;

        /// <summary>
        ///     Campaign time when the player started their current leave period.
        ///     Used for tracking leave duration.
        /// </summary>
        private CampaignTime _leaveStartDate = CampaignTime.Zero;

        /// <summary>
        ///     Campaign time when the player can next request leave.
        ///     Set when returning from leave to enforce a cooldown.
        /// </summary>
        private CampaignTime _leaveCooldownEnds = CampaignTime.Zero;

        /// <summary>
        ///     Flag to indicate party state needs to be restored after save loading.
        ///     We can't set IsActive during SyncData because the game asserts !IsActive during load.
        ///     This flag triggers restoration on the first campaign tick after loading.
        /// </summary>
        private bool _needsPostLoadStateRestore;

        /// <summary>
        ///     The player's original kingdom before enlisting with the lord.
        ///     Stored so it can be restored when service ends. Null if the player was independent.
        /// </summary>
        private Kingdom _originalKingdom;

        /// <summary>
        ///     Kingdom that the player needs to rejoin during grace period to avoid desertion.
        ///     Set when army is defeated or lord is captured. Player can rejoin any lord in this kingdom
        ///     during the grace period to avoid penalties.
        /// </summary>
        private Kingdom _pendingDesertionKingdom = null;

        /// <summary>
        ///     Tracks pending capture cleanup when the player is taken prisoner during an encounter.
        ///     We defer enlistment teardown until the surrender/encounter menus fully close.
        /// </summary>
        private Kingdom _pendingPlayerCaptureKingdom;

        private string _pendingPlayerCaptureReason;
        private bool _pendingVisibilityRestore;

        /// <summary>
        ///     Backup of the player's battle equipment before enlistment.
        ///     Restored when the player ends their service.
        /// </summary>
        private TaleWorlds.Core.Equipment _personalBattleEquipment;

        /// <summary>
        ///     Backup of the player's civilian equipment before enlistment.
        ///     Restored when the player ends their service.
        /// </summary>
        private TaleWorlds.Core.Equipment _personalCivilianEquipment;

        /// <summary>
        ///     Backup of the player's inventory items before enlistment.
        ///     Restored when the player ends their service.
        /// </summary>
        private ItemRoster _personalInventory = new ItemRoster();

        private bool _playerCaptureCleanupScheduled;
        private CampaignTime _savedGraceEnlistmentDate = CampaignTime.Zero;
        private Hero _savedGraceLord;
        private int _savedGraceTier = -1;
        private string _savedGraceTroopId;
        private int _savedGraceXP;

        /// <summary>
        ///     Currently selected duty assignment ID (e.g., "enlisted", "forager", "sentry").
        ///     Duties provide daily skill XP bonuses and may include wage multipliers.
        ///     Changed via the duty selection menu.
        /// </summary>
        private string _selectedDuty = "enlisted";

        /// <summary>
        ///     Currently selected profession ID (e.g., "field_medic", "quartermaster_aide").
        ///     Professions unlock at tier 3 and provide specialized skill XP bonuses.
        ///     Can be set to "none" if no profession is selected.
        /// </summary>
        private string _selectedProfession = "none";

        /// <summary>
        ///     Whether the player's clan was independent (no kingdom) before enlistment.
        ///     Used to determine whether to restore independence or rejoin the original kingdom
        ///     when service ends.
        /// </summary>
        private bool _wasIndependentClan;

        /// <summary>
        ///     Tracks faction StringIds for war relations we created when enlisting with a minor faction lord.
        ///     When the player enlists with a minor faction (not a Kingdom), they don't automatically get
        ///     faction relationships. We mirror the lord's faction war relations to the player clan so that
        ///     nameplates show correct ally/enemy colors. These are restored to neutral when service ends.
        /// </summary>
        private List<string> _minorFactionWarRelations = new List<string>();

        /// <summary>
        ///     Tracks desertion cooldowns for minor factions. When a player deserts from a minor faction lord,
        ///     they cannot re-enlist with any lord in that faction for 90 days. Unlike Kingdoms, minor factions
        ///     have no crime rating system, so we use this cooldown-based blocking instead.
        ///     Key: Minor faction StringId, Value: Cooldown end time
        /// </summary>
        private Dictionary<string, CampaignTime> _minorFactionDesertionCooldowns = new Dictionary<string, CampaignTime>();

        /// <summary>
        ///     Initializes the enlistment behavior and sets up singleton access.
        ///     The singleton pattern allows other behaviors (like dialog managers) to access
        ///     enlistment state without needing a direct reference.
        /// </summary>
        public EnlistmentBehavior()
        {
            Instance = this;
        }

        /// <summary>
        ///     Singleton instance for accessing enlistment state from other mods.
        ///     Available after campaign starts. Safe to check in any CampaignBehavior.
        /// </summary>
        public static EnlistmentBehavior Instance { get; private set; }

        /// <summary>
        ///     True if player is actively enlisted and not on leave.
        ///     Use this to check if enlistment mechanics should apply.
        /// </summary>
        public bool IsEnlisted => _enlistedLord != null && !_isOnLeave;

        /// <summary>
        ///     The lord the player is currently serving under.
        /// </summary>
        public Hero EnlistedLord => _enlistedLord;

        /// <summary>
        ///     True if player is on temporary leave from service.
        ///     While on leave, IsEnlisted returns false but enlistment state is preserved.
        /// </summary>
        public bool IsOnLeave => _isOnLeave;

        /// <summary>
        ///     Campaign time when the current leave started.
        /// </summary>
        public CampaignTime LeaveStartDate => _leaveStartDate;

        /// <summary>
        ///     Whether the player is currently in a desertion grace period.
        ///     During this time, they can rejoin any lord in the same kingdom to avoid desertion penalties.
        /// </summary>
        public bool IsInDesertionGracePeriod =>
            _pendingDesertionKingdom != null &&
            CampaignTime.Now < _desertionGracePeriodEnd;

        private bool ShouldActivationBeOn()
        {
            return IsEnlisted || _isOnLeave || IsInDesertionGracePeriod;
        }

        private void SyncActivationState(string reason)
        {
            var shouldBeActive = ShouldActivationBeOn();
            if (shouldBeActive != EnlistedActivation.IsActive)
            {
                EnlistedActivation.SetActive(shouldBeActive, reason);
            }
        }

        /// <summary>
        ///     Kingdom that the player needs to rejoin during grace period to avoid desertion.
        ///     Returns null if not in grace period.
        /// </summary>
        public Kingdom PendingDesertionKingdom => _pendingDesertionKingdom;

        /// <summary>
        ///     The lord the player is currently serving under.
        ///     Returns null if not enlisted.
        /// </summary>
        public Hero CurrentLord => _enlistedLord;

        /// <summary>
        ///     Current military rank tier (1-6). Higher tiers unlock better equipment and duties.
        /// </summary>
        public int EnlistmentTier => _enlistmentTier;

        /// <summary>
        ///     Total XP accumulated in current service. Used for promotion calculations.
        /// </summary>
        public int EnlistmentXP => _enlistmentXP;

        /// <summary>
        ///     Currently assigned duty (e.g., "enlisted", "pathfinder", "field_medic").
        /// </summary>
        public string SelectedDuty => _selectedDuty;

        /// <summary>
        ///     Currently selected profession track (e.g., "infantry", "cavalry"). "none" if not selected.
        /// </summary>
        public string SelectedProfession => _selectedProfession;

        public bool HasActiveGraceProtection => CampaignTime.Now < _graceProtectionEnds;

        /// <summary>
        ///     Checks if a party is currently involved in a battle, siege, or besieging a settlement.
        ///     Used to prevent finishing player encounters during critical battle/siege operations,
        ///     which can cause assertion failures in the siege system that expects encounters to
        ///     remain active during these operations.
        /// </summary>
        /// <param name="party">The party to check for battle/siege state.</param>
        /// <returns>True if the party is in battle, siege, or besieging a settlement; false otherwise.</returns>
        private static bool InBattleOrSiege(MobileParty party)
        {
            return party?.Party.MapEvent != null || party?.Party.SiegeEvent != null ||
                   party?.BesiegedSettlement != null;
        }

        private void EnsurePlayerSharesArmy(MobileParty lordParty)
        {
            var main = MobileParty.MainParty;
            if (main == null || lordParty == null)
            {
                ModLogger.Debug("Battle",
                    $"EnsurePlayerSharesArmy: Skipped - main={main != null}, lordParty={lordParty != null}");
                return;
            }

            // Log current state for diagnostics
            var lordMapEvent = lordParty.Party?.MapEvent;
            var playerMapEvent = main.Party?.MapEvent;
            float distanceToLord = main.GetPosition2D.Distance(lordParty.GetPosition2D);

            ModLogger.Info("Battle", "=== BATTLE PARTICIPATION CHECK ===");
            ModLogger.Info("Battle",
                $"Lord: {lordParty.LeaderHero?.Name?.ToString() ?? "unknown"}, HasArmy: {lordParty.Army != null}, InMapEvent: {lordMapEvent != null}");
            ModLogger.Info("Battle",
                $"Player: InMapEvent: {playerMapEvent != null}, IsActive: {main.IsActive}, IsVisible: {main.IsVisible}");
            ModLogger.Info("Battle",
                $"Distance to lord: {distanceToLord:F2}, PlayerArmy: {main.Army?.LeaderParty?.LeaderHero?.Name?.ToString() ?? "none"}");
            if (lordMapEvent != null)
            {
                ModLogger.Info("Battle",
                    $"Lord MapEvent Type: {lordMapEvent.EventType}, Attacker: {lordMapEvent.AttackerSide?.LeaderParty?.LeaderHero?.Name?.ToString() ?? "unknown"}, Defender: {lordMapEvent.DefenderSide?.LeaderParty?.LeaderHero?.Name?.ToString() ?? "unknown"}");
            }

            // Army joining is now handled in OnRealtimeTick when lord merges with army
            // If already in army, the native encounter system handles battle participation
            if (lordParty.Army != null && main.Army == lordParty.Army)
            {
                ModLogger.Debug("Battle", "Already in lord's army - native encounter system handles participation");
                return;
            }

            // Lord has no army but in battle - player participates through native encounter system
            // The native game will show encounter menu when player is active and near battle
            if (lordMapEvent != null && playerMapEvent == null)
            {
                ModLogger.Info("Battle",
                    "LORD IN INDIVIDUAL BATTLE (no army) - Attempting native encounter collection");
                ModLogger.Info("Battle",
                    $"Pre-activation state: IsActive={main.IsActive}, ShouldJoinPlayerBattles={main.ShouldJoinPlayerBattles}");

                // Ensure player is active, visible enough to be collected, and near the lord
                main.IsActive = true;
                main.IgnoreByOtherPartiesTill(CampaignTime.Now); // Clear ignore window so we can be collected
                main.ShouldJoinPlayerBattles = true;

                ModLogger.Info("Battle",
                    $"Post-activation state: IsActive={main.IsActive}, ShouldJoinPlayerBattles={main.ShouldJoinPlayerBattles}");
                ModLogger.Info("Battle",
                    "Player party ACTIVATED for individual lord battle - waiting for native encounter menu");
            }
            else if (lordMapEvent == null)
            {
                ModLogger.Debug("Battle", "Lord not in MapEvent - no battle action needed");
            }
            else if (playerMapEvent != null)
            {
                ModLogger.Debug("Battle", "Player already in MapEvent - no action needed");
            }
        }

        private void PreparePartyForNativeBattle(MobileParty main)
        {
            if (main == null)
            {
                return;
            }

            if (!main.IsActive)
            {
                main.IsActive = true;
            }

            main.IsVisible = false;
            main.IgnoreByOtherPartiesTill(CampaignTime.Now);
            main.ShouldJoinPlayerBattles = true;

            // CRITICAL: Follow the LORD's party with the camera, not the player's invisible party
            // This prevents the game from pausing when the lord enters battle while waiting for
            // the player's party (camera target) to arrive at the battle location
            var lordParty = _enlistedLord?.PartyBelongedTo;
            if (lordParty != null)
            {
                lordParty.Party.SetAsCameraFollowParty();
            }

            TrySetShouldJoinPlayerBattles(main, true);
        }

        /// <summary>
        ///     Registers event listeners that respond to game events throughout the campaign.
        ///     These events fire at various intervals and trigger specific behaviors like
        ///     party state updates, battle detection, lord status changes, and XP processing.
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

            // Settlement entry/exit events fire when parties enter or leave settlements.
            // We monitor both to keep the enlisted party hidden except during menus.
            CampaignEvents.SettlementEntered.AddNonSerializedListener(this, OnSettlementEntered);
            CampaignEvents.OnSettlementLeftEvent.AddNonSerializedListener(this, OnSettlementLeft);

            // Edge case handlers for grace period:
            // Clear grace period if player changes kingdoms or if kingdom is destroyed
            CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this,
                new Action<Clan, Kingdom, Kingdom, ChangeKingdomAction.ChangeKingdomActionDetail, bool>(
                    OnClanChangedKingdom));
            CampaignEvents.KingdomDestroyedEvent.AddNonSerializedListener(this, OnKingdomDestroyed);

            // Mission started event - used to add our formation assignment behavior
            // This ensures enlisted players are sorted into their designated formation (Infantry/Archer/etc)
            CampaignEvents.AfterMissionStarted.AddNonSerializedListener(this, OnAfterMissionStarted);
        }

        /// <summary>
        ///     Serializes and deserializes enlistment state for save/load operations.
        ///     Called automatically by Bannerlord's save system when saving or loading games.
        ///     After loading, validates state and restores proper party activity state.
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
            dataStore.SyncData("_leaveCooldownEnds", ref _leaveCooldownEnds);
            dataStore.SyncData("_desertionGracePeriodEnd", ref _desertionGracePeriodEnd);
            dataStore.SyncData("_pendingDesertionKingdom", ref _pendingDesertionKingdom);
            dataStore.SyncData("_savedGraceTier", ref _savedGraceTier);
            dataStore.SyncData("_savedGraceLord", ref _savedGraceLord);
            dataStore.SyncData("_savedGraceXP", ref _savedGraceXP);
            dataStore.SyncData("_savedGraceTroopId", ref _savedGraceTroopId);
            dataStore.SyncData("_savedGraceEnlistmentDate", ref _savedGraceEnlistmentDate);
            dataStore.SyncData("_graceProtectionEnds", ref _graceProtectionEnds);
            dataStore.SyncData("_selectedDuty", ref _selectedDuty);
            dataStore.SyncData("_selectedProfession", ref _selectedProfession);

            // Serialize kingdom state so we can restore the player's original kingdom/clan status
            dataStore.SyncData("_originalKingdom", ref _originalKingdom);
            dataStore.SyncData("_wasIndependentClan", ref _wasIndependentClan);

            // Serialize minor faction war relations - these are created when enlisting with non-Kingdom lords
            // and need to be restored to neutral when service ends
            dataStore.SyncData("_minorFactionWarRelations", ref _minorFactionWarRelations);

            // Serialize minor faction desertion cooldowns - manual serialization for Dictionary<string, CampaignTime>
            // Bannerlord's save system can serialize this dictionary directly since both types are primitives
            SerializeMinorFactionDesertionCooldowns(dataStore);

            // Veteran retirement system state - manual serialization for dictionary
            // Bannerlord's save system can't serialize custom class dictionaries directly
            dataStore.SyncData("_retirementNotificationShown", ref _retirementNotificationShown);
            dataStore.SyncData("_currentTermKills", ref _currentTermKills);

            // Manual serialization of _veteranRecords dictionary
            SerializeVeteranRecords(dataStore);

            // CRITICAL: Ensure proper party activity state for both new games and loaded games
            // This is important because the save system doesn't preserve IsActive state,
            // and we need to ensure enlisted players start inactive to prevent random encounters
            // For new campaigns, non-enlisted players must be active to allow normal gameplay

            // Validate tier and XP values after loading
            if (dataStore.IsLoading)
            {
                ModLogger.Info("SaveLoad",
                    $"Loading enlistment state - Lord: {_enlistedLord?.Name?.ToString() ?? "null"}, Tier: {_enlistmentTier}, XP: {_enlistmentXP}, OnLeave: {_isOnLeave}, GracePeriod: {IsInDesertionGracePeriod}");
                ValidateLoadedState();
                ModLogger.Info("SaveLoad", "Enlistment state validated and restored");

                // Sync activation based on loaded state
                SyncActivationState("sync_load");
            }
            else
            {
                ModLogger.Debug("SaveLoad",
                    $"Saving enlistment state - Lord: {_enlistedLord?.Name?.ToString() ?? "null"}, Tier: {_enlistmentTier}, XP: {_enlistmentXP}");
            }

            // IMPORTANT: Do NOT set IsActive here during SyncData!
            // The game asserts that IsActive must be false during save load process.
            // Party state will be restored on the first campaign tick via _needsPostLoadStateRestore flag.
            if (dataStore.IsLoading)
            {
                _needsPostLoadStateRestore = true;
                _isPartyStateInitialized = false; // Block all IsActive modifications until post-load
                _initializationTicksRemaining = 10; // Reset countdown to ensure we wait for load completion
                ModLogger.Debug("SaveLoad", "Deferred party state restoration to first campaign tick");
            }
        }

        /// <summary>
        ///     Manually serialize/deserialize veteran records since Bannerlord can't handle custom class dictionaries.
        ///     Uses a simple count + individual field approach for maximum compatibility.
        /// </summary>
        private void SerializeVeteranRecords(IDataStore dataStore)
        {
            try
            {
                // Store/load the count first
                var recordCount = _veteranRecords?.Count ?? 0;
                dataStore.SyncData("_vetRec_count", ref recordCount);

                if (!dataStore.IsLoading)
                {
                    // Saving: serialize each record individually
                    var index = 0;
                    foreach (var kvp in _veteranRecords)
                    {
                        var kingdomId = kvp.Key;
                        var firstTerm = kvp.Value.FirstTermCompleted;
                        var tier = kvp.Value.PreservedTier;
                        var kills = kvp.Value.TotalKills;
                        CampaignTime cooldown = kvp.Value.CooldownEnds;
                        CampaignTime termEnd = kvp.Value.CurrentTermEnd;
                        var renewal = kvp.Value.IsInRenewalTerm;
                        var renewalCount = kvp.Value.RenewalTermsCompleted;

                        dataStore.SyncData($"_vetRec_{index}_id", ref kingdomId);
                        dataStore.SyncData($"_vetRec_{index}_firstTerm", ref firstTerm);
                        dataStore.SyncData($"_vetRec_{index}_tier", ref tier);
                        dataStore.SyncData($"_vetRec_{index}_kills", ref kills);
                        dataStore.SyncData($"_vetRec_{index}_cooldown", ref cooldown);
                        dataStore.SyncData($"_vetRec_{index}_termEnd", ref termEnd);
                        dataStore.SyncData($"_vetRec_{index}_renewal", ref renewal);
                        dataStore.SyncData($"_vetRec_{index}_renewalCount", ref renewalCount);
                        index++;
                    }
                }
                else
                {
                    // Loading: reconstruct dictionary from individual fields
                    _veteranRecords = new Dictionary<string, FactionVeteranRecord>();

                    for (var i = 0; i < recordCount; i++)
                    {
                        var kingdomId = "";
                        var firstTerm = false;
                        var tier = 1;
                        var kills = 0;
                        CampaignTime cooldown = CampaignTime.Zero;
                        CampaignTime termEnd = CampaignTime.Zero;
                        var renewal = false;
                        var renewalCount = 0;

                        dataStore.SyncData($"_vetRec_{i}_id", ref kingdomId);
                        dataStore.SyncData($"_vetRec_{i}_firstTerm", ref firstTerm);
                        dataStore.SyncData($"_vetRec_{i}_tier", ref tier);
                        dataStore.SyncData($"_vetRec_{i}_kills", ref kills);
                        dataStore.SyncData($"_vetRec_{i}_cooldown", ref cooldown);
                        dataStore.SyncData($"_vetRec_{i}_termEnd", ref termEnd);
                        dataStore.SyncData($"_vetRec_{i}_renewal", ref renewal);
                        dataStore.SyncData($"_vetRec_{i}_renewalCount", ref renewalCount);

                        if (!string.IsNullOrEmpty(kingdomId))
                        {
                            _veteranRecords[kingdomId] = new FactionVeteranRecord
                            {
                                FirstTermCompleted = firstTerm,
                                PreservedTier = tier,
                                TotalKills = kills,
                                CooldownEnds = cooldown,
                                CurrentTermEnd = termEnd,
                                IsInRenewalTerm = renewal,
                                RenewalTermsCompleted = renewalCount
                            };
                        }
                    }

                    ModLogger.Debug("SaveLoad", $"Loaded {_veteranRecords.Count} veteran records");
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("SaveLoad", $"Error serializing veteran records: {ex.Message}");
                // Ensure dictionary exists even on error
                if (_veteranRecords == null)
                {
                    _veteranRecords = new Dictionary<string, FactionVeteranRecord>();
                }
            }
        }

        /// <summary>
        ///     Manually serialize/deserialize minor faction desertion cooldowns.
        ///     Uses a simple count + individual field approach for maximum compatibility.
        /// </summary>
        private void SerializeMinorFactionDesertionCooldowns(IDataStore dataStore)
        {
            try
            {
                var cooldownCount = _minorFactionDesertionCooldowns?.Count ?? 0;
                dataStore.SyncData("_minorFacDesertion_count", ref cooldownCount);

                if (!dataStore.IsLoading)
                {
                    // Saving: serialize each cooldown entry individually
                    var index = 0;
                    foreach (var kvp in _minorFactionDesertionCooldowns)
                    {
                        var factionId = kvp.Key;
                        var cooldownEnd = kvp.Value;

                        dataStore.SyncData($"_minorFacDesertion_{index}_id", ref factionId);
                        dataStore.SyncData($"_minorFacDesertion_{index}_end", ref cooldownEnd);
                        index++;
                    }
                }
                else
                {
                    // Loading: reconstruct dictionary from individual fields
                    _minorFactionDesertionCooldowns = new Dictionary<string, CampaignTime>();

                    for (var i = 0; i < cooldownCount; i++)
                    {
                        var factionId = "";
                        var cooldownEnd = CampaignTime.Zero;

                        dataStore.SyncData($"_minorFacDesertion_{i}_id", ref factionId);
                        dataStore.SyncData($"_minorFacDesertion_{i}_end", ref cooldownEnd);

                        if (!string.IsNullOrEmpty(factionId))
                        {
                            _minorFactionDesertionCooldowns[factionId] = cooldownEnd;
                        }
                    }

                    ModLogger.Debug("SaveLoad", $"Loaded {_minorFactionDesertionCooldowns.Count} minor faction desertion cooldowns");
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("SaveLoad", $"Error serializing minor faction desertion cooldowns: {ex.Message}");
                // Ensure dictionary exists even on error
                if (_minorFactionDesertionCooldowns == null)
                {
                    _minorFactionDesertionCooldowns = new Dictionary<string, CampaignTime>();
                }
            }
        }

        public bool CanEnlistWithParty(Hero lord, out TextObject reason)
        {
            reason = TextObject.GetEmpty(); // 1.3.4 API: Empty is now GetEmpty() method
            if (IsEnlisted)
            {
                reason = new TextObject("{=Enlisted_Message_AlreadyInService}You are already in service.");
                return false;
            }

            if (lord == null || !lord.IsLord)
            {
                reason = new TextObject("{=Enlisted_Message_MustSpeakToNoble}We must speak to a noble to enlist.");
                return false;
            }

            var main = MobileParty.MainParty;
            if (main == null)
            {
                reason = new TextObject("{=Enlisted_Message_NoMainParty}No main party found.");
                return false;
            }

            // Block enlistment if player is leading their own army - army must be disbanded first
            // This prevents crashes from army members being left in an undefined state
            if (main.Army != null && main.Army.LeaderParty == main)
            {
                reason = new TextObject("{=Enlisted_Message_MustDisbandArmy}You are leading an army. Disband your army before enlisting in another lord's service.");
                return false;
            }

            // Handle players on leave - allow same-faction transfers but block cross-faction enlistment
            if (_isOnLeave)
            {
                if (_enlistedLord != null && lord != _enlistedLord)
                {
                    // Allow transfer to other lords in the SAME faction/kingdom
                    var currentLordKingdom = _enlistedLord.MapFaction as Kingdom;
                    var targetLordKingdom = lord.MapFaction as Kingdom;

                    if (currentLordKingdom != null && targetLordKingdom == currentLordKingdom)
                    {
                        // Same faction - allow transfer (handled by TransferServiceToLord)
                        ModLogger.Debug("Enlistment",
                            $"Allowing same-faction transfer check from {_enlistedLord.Name} to {lord.Name}");
                        // Continue to other checks (party validity, etc.)
                    }
                    else
                    {
                        // Different faction - block
                        reason = new TextObject(
                            "{=Enlisted_Message_OnLeaveCannotJoin}You are currently on leave from {LORD}. You cannot join a different faction without resigning first.");
                        reason.SetTextVariable("LORD", _enlistedLord.Name ?? TextObject.GetEmpty());
                        return false;
                    }
                }

                if (_enlistedLord == null)
                {
                    reason = new TextObject(
                        "{=Enlisted_Message_OnLeaveReportBack}You are on leave and cannot enlist elsewhere until you report back.");
                    return false;
                }
            }

            // During grace period, enforce kingdom loyalty
            if (IsInDesertionGracePeriod && _pendingDesertionKingdom != null)
            {
                var lordKingdom = lord.MapFaction as Kingdom;
                if (lordKingdom != _pendingDesertionKingdom)
                {
                    reason = new TextObject(
                        "{=Enlisted_Message_BoundByGrace}You are still bound to {KINGDOM} by your grace orders. Other lords cannot enlist you yet.");
                    reason.SetTextVariable("KINGDOM",
                        _pendingDesertionKingdom.Name ?? TextObject.GetEmpty()); // 1.3.4 API
                    return false;
                }
            }

            // Block re-enlistment with minor factions if player deserted from them
            // Minor factions have no crime rating system, so we use a cooldown-based block instead
            if (lord.MapFaction != null && !(lord.MapFaction is Kingdom))
            {
                if (IsBlockedFromMinorFaction(lord.MapFaction, out var remainingDays))
                {
                    reason = new TextObject(
                        "{=Enlisted_Message_MinorFactionCooldown}{FACTION} will not accept you back for another {DAYS} days due to your past desertion.");
                    reason.SetTextVariable("FACTION", lord.MapFaction.Name);
                    reason.SetTextVariable("DAYS", remainingDays);
                    return false;
                }
            }

            var counterpartParty = MobileParty.ConversationParty ?? lord.PartyBelongedTo;

            // Fallback: If in a settlement, PartyBelongedTo might be null even if the lord has a party.
            // Attempt to find the lord's party via clan components.
            if (counterpartParty == null && lord.Clan != null)
            {
                foreach (var component in lord.Clan.WarPartyComponents)
                {
                    if (component.MobileParty != null && component.MobileParty.LeaderHero == lord)
                    {
                        counterpartParty = component.MobileParty;
                        break;
                    }
                }
            }

            if (counterpartParty == null)
            {
                reason = new TextObject("{=Enlisted_Message_LordNoParty}The lord has no party at present.");
                ModLogger.Debug("Enlistment", $"CanEnlistWithParty: {lord.Name} has no party");
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
                var rejoiningKingdom = lord.MapFaction as Kingdom;
                _graceProtectionEnds = CampaignTime.Zero;
                var resumingGraceService = IsInDesertionGracePeriod && rejoiningKingdom == _pendingDesertionKingdom &&
                                           _savedGraceTier > 0;
                var graceTier = _savedGraceTier;
                var graceXP = _savedGraceXP;
                var graceTroopId = _savedGraceTroopId;

                // Backup player's personal equipment before service begins
                // This ensures the player gets their original equipment back when service ends
                // Equipment is backed up once at the start to prevent losing items during service
                if (!_hasBackedUpEquipment)
                {
                    BackupPlayerEquipment();
                    _hasBackedUpEquipment = true;
                }

                // Protect player's ships from damage during enlistment
                // Ships remain with the player but cannot take damage while serving under a lord
                SetPlayerShipsInvulnerable();

                // Initialize enlistment state with default values
                _enlistedLord = lord;
                if (_enlistedLord != null)
                {
                    Campaign.Current.VisualTrackerManager.RemoveTrackedObject(_enlistedLord);
                }

                SyncActivationState("start_enlist");

                var resumedFromGrace = resumingGraceService;
                if (resumedFromGrace)
                {
                    _enlistmentTier = Math.Max(1, graceTier);
                    _enlistmentXP = Math.Max(0, graceXP);
                }
                else
                {
                    _enlistmentTier = 1; // Start at the lowest tier (recruit/levy)
                    _enlistmentXP = 0; // Start with zero XP
                }

                _enlistmentDate = resumedFromGrace && _savedGraceEnlistmentDate != CampaignTime.Zero
                    ? _savedGraceEnlistmentDate
                    : CampaignTime.Now; // Record when service started (or resume previous enlistment date)
                _selectedDuty = "enlisted"; // Default to basic enlisted duty for daily XP
                _selectedProfession = "none"; // No profession initially (unlocks at tier 3)

                // Register the default "enlisted" duty with the duties system
                // This ensures daily XP processing begins immediately for the basic duty
                var dutiesBehavior = EnlistedDutiesBehavior.Instance;
                if (dutiesBehavior != null)
                {
                    dutiesBehavior.AssignDuty("enlisted");
                }

                // Log enlistment start for diagnostics
                SessionDiagnostics.LogStateTransition("Enlistment", "Civilian", "Enlisted",
                    $"Lord: {lord.Name}, Kingdom: {lord.MapFaction?.Name?.ToString() ?? "None"}, Tier: {_enlistmentTier}, ResumedGrace: {resumedFromGrace}");

                // Store the player's original kingdom/clan state before joining the lord's faction
                // This information is needed to restore the player's original kingdom when service ends
                var playerClan = Clan.PlayerClan;
                _originalKingdom = playerClan?.Kingdom;
                _wasIndependentClan = playerClan?.Kingdom == null;

                // Join lord's kingdom if they have one and player isn't already in it
                var lordKingdom = lord.MapFaction as Kingdom;
                if (lordKingdom != null && playerClan != null)
                {
                    if (playerClan.Kingdom != lordKingdom)
                    {
                        try
                        {
                            // Join the lord's kingdom as a mercenary (enlisted soldiers are mercenaries, not vassals)
                            // The awardMultiplier parameter controls the mercenary wage rate (default 50)
                            ChangeKingdomAction.ApplyByJoinFactionAsMercenary(playerClan, lordKingdom,
                                default(CampaignTime), 0, false);
                            ModLogger.Info("Enlistment",
                                $"Joined {lordKingdom.Name} as mercenary while enlisted with {lord.Name}");
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
                    // MINOR FACTION FIX: Mirror the lord's faction war relations to the player clan
                    // Without this, nameplates don't show green/red colors and battle participation fails
                    // because the player has no faction relationships with enemies/allies.
                    MirrorMinorFactionWarRelations(lord);
                }

                // Transfer any existing companions and troops from the player's party to the lord's party
                // This prevents the player from having their own troops while enlisted, as they're
                // now part of the lord's military force. Troops will be restored when service ends.
                TransferPlayerTroopsToLord();

                var appliedGraceEquipment = TryApplyGraceEquipment(resumedFromGrace, graceTroopId);
                if (!appliedGraceEquipment)
                {
                    AssignInitialEquipment();
                    SetInitialFormation();
                }
                else
                {
                    ModLogger.Info("Enlistment", "Grace enlistment equipment restored for new service");
                }

                if (resumedFromGrace)
                {
                    var message =
                        new TextObject(
                            "{=Enlisted_Message_RejoinedKingdom}You have rejoined {KINGDOM}. Your grace period has been cleared.");
                    var kingdomName = rejoiningKingdom?.Name ??
                                      new TextObject("{=Enlisted_Term_YourKingdom}your kingdom");
                    message.SetTextVariable("KINGDOM", kingdomName);
                    InformationManager.DisplayMessage(new InformationMessage(message.ToString()));
                    ClearDesertionGracePeriod();
                }

                // CRITICAL: Finish any active PlayerEncounter before starting enlistment
                // This prevents unwanted encounters with the lord's party when we position the player
                // at the lord's location. If there's an active encounter, finishing it ensures clean state.
                if (PlayerEncounter.Current != null)
                {
                    try
                    {
                        // If inside a settlement, leave it first before finishing the encounter
                        // This ensures clean encounter state cleanup
                        if (PlayerEncounter.InsideSettlement)
                        {
                            PlayerEncounter.LeaveSettlement();
                        }

                        PlayerEncounter.Finish(true);
                        ModLogger.Debug("Enlistment", "Finished active PlayerEncounter before starting enlistment");
                    }
                    catch (Exception ex)
                    {
                        ModLogger.Error("Enlistment",
                            $"Error finishing PlayerEncounter before enlistment: {ex.Message}");
                    }
                }

                // Attach the player's party to the lord's party using natural attachment system
                // This ensures the player follows the lord during travel and integrates with armies
                // IMPORTANT: Do this AFTER finishing any active encounters to avoid creating
                // an encounter with the lord's party itself
                // Expense sharing is prevented by EnlistmentExpenseIsolationPatch
                EncounterGuard.TryAttachOrEscort(lord);

                // Configure party state to prevent random encounters while allowing battle participation
                var main = MobileParty.MainParty;
                var lordParty = lord.PartyBelongedTo;
                if (main != null)
                {
                    // Make the player party invisible on the map (they're part of the lord's force now)
                    main.IsVisible = false;

                    // Also hide the 3D visual entity (separate from nameplate VM)
                    EncounterGuard.HidePlayerPartyVisual();

                    // CRITICAL: Keep party ACTIVE so escort AI works for following
                    // Use IgnoreByOtherPartiesTill to prevent random encounters instead of IsActive = false
                    // This allows SetMoveEscortParty to function properly
                    main.IsActive = true;
                    main.IgnoreByOtherPartiesTill(CampaignTime.Now +
                                                  CampaignTime.Days(365f)); // Ignore all parties for 1 year

                    // Set escort AI to follow the lord - this only works when party is active
                    if (lordParty != null)
                    {
                        main.SetMoveEscortParty(lordParty, MobileParty.NavigationType.Default, false);
                        lordParty.Party.SetAsCameraFollowParty();
                        ModLogger.Debug("Enlistment", $"Set escort AI to follow {lord.Name}");
                    }

                    // Enable battle participation so the player joins battles when the lord fights
                    TrySetShouldJoinPlayerBattles(main, true);
                }


                ModLogger.Info("Enlistment",
                    $"Successfully enlisted with {lord.Name} - Tier {_enlistmentTier}, XP: {_enlistmentXP}, Kingdom: {lord.MapFaction?.Name?.ToString() ?? "Independent"}, Culture: {lord.Culture?.StringId ?? "unknown"}");
                ModLogger.Info("Enlistment",
                    $"Enlistment date: {_enlistmentDate}, Equipment backed up: {_hasBackedUpEquipment}, Grace resume: {resumedFromGrace}");

                // Fire event for other mods
                OnEnlisted?.Invoke(lord);
            }
            catch (Exception ex)
            {
                ModLogger.Error("Enlistment",
                    $"Failed to start enlistment with {lord?.Name?.ToString() ?? "null"} - {ex.Message}", ex);
                // Restore equipment if backup was created
                if (_hasBackedUpEquipment)
                {
                    RestorePersonalEquipment();
                }
            }
        }

        /// <summary>
        ///     Discharge the player from enlistment. By default, returns the player to independent status.
        ///     Exception: If honorably discharged after completing full service period (252 days),
        ///     the player is returned to their original kingdom.
        /// </summary>
        /// <param name="reason">Reason for discharge (for logging).</param>
        /// <param name="isHonorableDischarge">Whether this is an honorable discharge (e.g., lord died). Defaults to false.</param>
        public void StopEnlist(string reason, bool isHonorableDischarge = false, bool retainKingdomDuringGrace = false)
        {
            try
            {
                ModLogger.Info("Enlistment", $"Service ended: {reason} (Honorable: {isHonorableDischarge})");
                
                // EQUIPMENT ACCOUNTABILITY: Check for missing gear and charge the soldier before discharge
                // EXCEPTION: Skip for honorable discharge (retirement) - player keeps all gear as reward
                if (!isHonorableDischarge)
                {
                    ProcessEquipmentAccountabilityOnDischarge();
                }
                else
                {
                    // Clear issued equipment tracking without charging - retirement perk
                    var troopSelection = TroopSelectionManager.Instance;
                    troopSelection?.ClearIssuedEquipment();
                    ModLogger.Info("Enlistment", "Retirement: skipping equipment accountability (keeping all gear)");
                }

                var main = MobileParty.MainParty;
                var playerInBattleState = false;

                // CRITICAL: Clean up all battle-related state BEFORE finishing encounters or making party active
                // This prevents crashes when the player is still in a MapEvent or Army after army defeat
                if (main != null)
                {
                    // Remove from army if still in one (army might be dispersed but party still references it)
                    if (main.Army != null)
                    {
                        try
                        {
                            ModLogger.Info("Enlistment",
                                $"Removing player from army before ending service (Army: {main.Army.LeaderParty?.LeaderHero?.Name})");
                            // Clear army reference - the army may already be dispersed
                            main.Army = null;
                            // Note: We can't call RemovePartyFromMergedParties on a dispersed army, so just clear the reference
                        }
                        catch (Exception ex)
                        {
                            ModLogger.Error("Enlistment", $"Error removing player from army: {ex.Message}");
                            // Force clear army reference even if removal fails
                            main.Army = null;
                        }
                    }

                    // Release attachment to lord's party BEFORE finishing encounters
                    // CRITICAL: Clear attachment (clearAttachment = true) to prevent player from being
                    // considered part of defeated force, avoiding immediate "Attack or Surrender" encounters
                    TryReleaseEscort(main, true);

                    // Disable battle joining BEFORE state changes
                    TrySetShouldJoinPlayerBattles(main, false);

                    // CRITICAL: Check if player is still in a MapEvent OR PlayerEncounter
                    // If the player is in either state, we must NOT activate to prevent crashes
                    // This is especially important when the lord is captured/defeated while the player
                    // is in a surrender menu - activating while in an encounter causes assertion failures
                    bool playerInMapEvent = main.Party.MapEvent != null;
                    bool playerInEncounter = PlayerEncounter.Current != null;
                    playerInBattleState = playerInMapEvent || playerInEncounter;

                    if (playerInBattleState)
                    {
                        ModLogger.Info("Enlistment",
                            $"Player still in battle state when ending service (MapEvent: {playerInMapEvent}, Encounter: {playerInEncounter}) - deactivating to prevent crashes");
                        // CRITICAL: Deactivate party to prevent them from becoming "attackable"
                        // while still in battle/encounter state - this prevents crashes when clicking "Surrender"
                        main.IsActive = false;
                        main.IsVisible = false;
                        // Don't finish PlayerEncounter or MapEvent yet - wait for them to end naturally
                        // The OnMapEventEnded handler will handle cleanup when battle ends
                        // This prevents crashes when clicking "Surrender" while in invalid state
                        SchedulePostEncounterVisibilityRestore();
                    }
                    else
                    {
                        // No active MapEvent or PlayerEncounter - safe to activate
                        // Restore normal party state now that service is ending
                        main.IsVisible = true; // Make the party visible on the map again
                        main.IsActive = true; // Re-enable party activity to allow normal encounters

                        // Also show the 3D visual entity (separate from nameplate VM)
                        EncounterGuard.ShowPlayerPartyVisual();

                        ModLogger.Info("Enlistment", "Party activated and made visible (no active battle state)");
                    }

                    // NAVAL FIX: Handle sea stranding - if player is at sea without naval capability, teleport to nearest port
                    // This can happen when the army disbands at sea and service ends (e.g., lord captured at sea)
                    // Without this fix, the player becomes permanently stranded on the water
                    if (main.IsCurrentlyAtSea && !main.HasNavalNavigationCapability && !retainKingdomDuringGrace)
                    {
                        ModLogger.Info("Naval",
                            "Player stranded at sea after service ended - teleporting to nearest port");
                        TryTeleportToNearestPort(main);
                    }
                }

                // Restore the player's personal equipment that was backed up at enlistment start
                // EXCEPTION: During grace period (retainKingdomDuringGrace=true), keep enlisted equipment
                // RETIREMENT REWARD: Honorable discharge = keep military gear, get old stuff back in inventory
                if (_hasBackedUpEquipment && !retainKingdomDuringGrace)
                {
                    if (isHonorableDischarge)
                    {
                        // RETIREMENT: Player keeps military gear AND gets personal stuff back in inventory
                        RestorePersonalEquipmentToInventory();
                        _hasBackedUpEquipment = false;
                        ModLogger.Info("Equipment", "Retirement reward: keeping military gear, personal items to inventory");
                    }
                    else
                    {
                        // REGULAR DISCHARGE: Replace military gear with original personal equipment
                        RestorePersonalEquipment();
                        _hasBackedUpEquipment = false;
                        ModLogger.Info("Equipment", "Personal equipment restored - full discharge");
                    }
                }
                else if (retainKingdomDuringGrace)
                {
                    ModLogger.Info("Equipment", "Keeping enlisted equipment during grace period");
                }

                // Restore player's ships to normal vulnerability after full discharge
                // During grace period, ships remain protected in case player re-enlists
                if (!retainKingdomDuringGrace)
                {
                    RestorePlayerShipsVulnerability();
                }
                else
                {
                    ModLogger.Debug("Naval", "Keeping ships protected during grace period");
                }

                // Restore companions and troops to the player's party
                // These were transferred to the lord's party when service started
                RestoreCompanionsToPlayer();

                // MINOR FACTION CLEANUP: Restore war relations that were mirrored when enlisting with a minor faction
                // During grace period, keep war relations in case player re-enlists with another lord in same faction
                if (!retainKingdomDuringGrace)
                {
                    RestoreMinorFactionWarRelations();
                }
                else
                {
                    ModLogger.Debug("FactionRelations", "Keeping mirrored war relations during grace period");
                }

                // Determine if player completed full enlistment period (252 days / 3 Bannerlord years)
                // This is required for honorable discharge to restore original kingdom
                var completedFullService = false;
                if (_enlistmentDate != CampaignTime.Zero)
                {
                    var serviceDuration = CampaignTime.Now - _enlistmentDate;
                    var fullServicePeriod = CampaignTime.Days(252f); // 3 Bannerlord years (from config)
                    completedFullService = serviceDuration >= fullServicePeriod;

                    var daysServed = serviceDuration.ToDays;
                    ModLogger.Info("Enlistment",
                        $"Service duration: {daysServed:F1} days (Full period: 252 days, Completed: {completedFullService})");
                }

                // Determine target kingdom based on discharge type
                // Default: Return to independent status
                // Exception: Honorably discharged after full service → restore original kingdom
                var playerClan = Clan.PlayerClan;
                if (playerClan != null)
                {
                    try
                    {
                        Kingdom targetKingdom = null;
                        var keepCurrentKingdom = false;

                        // Check if this qualifies for restoration to original kingdom
                        // Must be: honorable discharge AND completed full service period
                        if (isHonorableDischarge && completedFullService && _originalKingdom != null)
                        {
                            // Honorably discharged after full service - restore to original kingdom
                            if (playerClan.Kingdom != _originalKingdom)
                            {
                                targetKingdom = _originalKingdom;
                                ModLogger.Info("Enlistment",
                                    $"Honorable discharge after full service - restoring to {_originalKingdom.Name} without penalties");
                            }
                            else
                            {
                                ModLogger.Debug("Enlistment", "Honorable discharge - already in original kingdom");
                            }
                        }
                        else
                        {
                            // Default: Return to independent status (leave current kingdom if in one)
                            if (playerClan.Kingdom != null)
                            {
                                if (retainKingdomDuringGrace)
                                {
                                    keepCurrentKingdom = true;
                                    ModLogger.Info("Enlistment",
                                        $"Grace discharge - retaining membership in {playerClan.Kingdom.Name}");
                                }
                                else
                                {
                                    targetKingdom = null; // Independent
                                    if (isHonorableDischarge && !completedFullService)
                                    {
                                        ModLogger.Info("Enlistment",
                                            "Honorable discharge but incomplete service - returning to independent status");
                                    }
                                    else
                                    {
                                        ModLogger.Info("Enlistment", "Discharge - returning to independent status");
                                    }
                                }
                            }
                            else
                            {
                                ModLogger.Debug("Enlistment", "Already independent - no kingdom change needed");
                            }
                        }

                        // Use helper method to restore kingdom without penalties
                        if (keepCurrentKingdom)
                        {
                            ModLogger.Debug("Enlistment", "Skipped kingdom change due to active grace period");
                        }
                        else if (targetKingdom == null && playerClan.Kingdom != null)
                        {
                            // Need to leave kingdom to become independent
                            DischargeHelper.RestoreKingdomWithoutPenalties(playerClan, null);
                        }
                        else if (targetKingdom != null && playerClan.Kingdom != targetKingdom)
                        {
                            // Need to join original kingdom (honorable discharge after full service)
                            DischargeHelper.RestoreKingdomWithoutPenalties(playerClan, targetKingdom);
                        }
                    }
                    catch (Exception ex)
                    {
                        ModLogger.Error("Enlistment", $"Error restoring kingdom: {ex.Message}");
                    }
                }

                // Clear kingdom state tracking
                _originalKingdom = null;
                _wasIndependentClan = false;
                if (!retainKingdomDuringGrace)
                {
                    _graceProtectionEnds = CampaignTime.Zero;
                }

                // If retaining kingdom for grace period, save progression state BEFORE clearing it
                // This preserves tier/XP so player can resume at their previous rank when re-enlisting
                // CRITICAL: Must happen before _enlistmentTier and _enlistmentXP are reset below
                if (retainKingdomDuringGrace && _enlistedLord != null)
                {
                    _savedGraceTier = _enlistmentTier;
                    _savedGraceXP = _enlistmentXP;
                    _savedGraceTroopId = TroopSelectionManager.Instance?.LastSelectedTroopId;
                    _savedGraceEnlistmentDate = _enlistmentDate;
                    ModLogger.Info("Enlistment",
                        $"Saved grace progression state: Tier={_savedGraceTier}, XP={_savedGraceXP}");
                }

                // Clear enlistment state
                if (_enlistedLord != null)
                {
                    if (retainKingdomDuringGrace)
                    {
                        _savedGraceLord = _enlistedLord;
                        Campaign.Current.VisualTrackerManager.RegisterObject(_enlistedLord);
                        var trackerMsg =
                            new TextObject("{=Enlisted_Message_LordTracked}Your lord has been marked on the map.");
                        InformationManager.DisplayMessage(new InformationMessage(trackerMsg.ToString()));
                        ModLogger.Debug("Enlistment", "Added map tracker for grace period");
                    }
                    else
                    {
                        Campaign.Current.VisualTrackerManager.RemoveTrackedObject(_enlistedLord);
                    }
                }

                _enlistedLord = null;
                _enlistmentTier = 1;
                _enlistmentXP = 0;
                _enlistmentDate = CampaignTime.Zero;
                _disbandArmyAfterBattle = false; // Clear any pending army operations
                if (!playerInBattleState)
                {
                    ForceFinishLingeringEncounter("StopEnlist");
                }
                else
                {
                    ModLogger.Debug("EncounterCleanup",
                        "Skipping encounter force-finish because player is still in battle state");
                }

                // Log discharge for diagnostics
                SessionDiagnostics.LogStateTransition("Enlistment", "Enlisted", "Civilian",
                    $"Reason: {reason}, Honorable: {isHonorableDischarge}");

                // Fire event for other mods
                OnDischarged?.Invoke(reason);

                SyncActivationState("stop_enlist");
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
        /// Process equipment accountability when soldier is discharged or transfers service.
        /// Checks for missing issued gear and deducts the cost from the soldier's pay.
        /// </summary>
        private void ProcessEquipmentAccountabilityOnDischarge()
        {
            try
            {
                var troopSelection = TroopSelectionManager.Instance;
                if (troopSelection == null)
                {
                    return;
                }
                
                var (missingItems, totalDebt) = troopSelection.CheckMissingEquipment();
                
                if (missingItems.Count == 0)
                {
                    // All equipment accounted for
                    troopSelection.ClearIssuedEquipment();
                    ModLogger.Info("Enlistment", "Equipment accountability check passed - all gear returned");
                    return;
                }
                
                // Deduct cost of missing equipment
                var hero = Hero.MainHero;
                if (hero != null && totalDebt > 0)
                {
                    hero.Gold = Math.Max(0, hero.Gold - totalDebt);
                    
                    // Build notification using localized strings
                    var sb = new System.Text.StringBuilder();
                    var headerText = new TextObject("{=qm_missing_discharge_header}Equipment missing at discharge:");
                    sb.AppendLine(headerText.ToString());
                    foreach (var item in missingItems)
                    {
                        sb.AppendLine($"  • {item.ItemName} ({item.ItemValue} denars)");
                    }
                    var totalText = new TextObject("{=qm_missing_discharge_total}Total deducted from final pay: {AMOUNT} denars");
                    totalText.SetTextVariable("AMOUNT", totalDebt);
                    sb.AppendLine(totalText.ToString());
                    
                    var chargeMsg = new TextObject("{=qm_missing_discharge_charge}Missing equipment: {AMOUNT} denars deducted from final pay.");
                    chargeMsg.SetTextVariable("AMOUNT", totalDebt);
                    InformationManager.DisplayMessage(new InformationMessage(chargeMsg.ToString(), Colors.Red));
                    
                    // Show popup for significant amounts
                    if (totalDebt >= 100)
                    {
                        var titleText = new TextObject("{=qm_missing_equipment_title}Equipment Accountability");
                        var btnText = new TextObject("{=qm_btn_understood}Understood");
                        InformationManager.ShowInquiry(new InquiryData(
                            titleText.ToString(),
                            sb.ToString(),
                            true,
                            false,
                            btnText.ToString(),
                            string.Empty,
                            null,
                            null));
                    }
                    
                    ModLogger.Info("Enlistment", $"Equipment accountability: {totalDebt} denars charged for {missingItems.Count} missing items");
                }
                
                // Clear tracking
                troopSelection.ClearIssuedEquipment();
            }
            catch (Exception ex)
            {
                ModLogger.Error("Enlistment", "Error processing equipment accountability on discharge", ex);
            }
        }

        /// <summary>
        ///     Start desertion grace period when army is defeated or lord is captured.
        ///     Player has 14 days to rejoin another lord in the same kingdom before being branded a deserter.
        /// </summary>
        /// <param name="kingdom">Kingdom that the player needs to rejoin during grace period.</param>
        private void StartDesertionGracePeriod(Kingdom kingdom)
        {
            try
            {
                if (kingdom == null)
                {
                    ModLogger.Error("Desertion", "Cannot start grace period - kingdom is null");
                    return;
                }

                _pendingDesertionKingdom = kingdom;
                var graceDays = EnlistedConfig.LoadGameplayConfig().DesertionGracePeriodDays;
                _desertionGracePeriodEnd = CampaignTime.Now + CampaignTime.Days(graceDays);

                // Only save progression state if not already saved by StopEnlist()
                // StopEnlist saves these values when retainKingdomDuringGrace=true, before clearing
                // If _savedGraceTier is already > 0, the values were pre-saved and shouldn't be overwritten
                if (_savedGraceTier <= 0)
                {
                    _savedGraceTier = _enlistmentTier;
                    _savedGraceXP = _enlistmentXP;
                    _savedGraceTroopId = TroopSelectionManager.Instance?.LastSelectedTroopId;
                    _savedGraceEnlistmentDate = _enlistmentDate;
                    ModLogger.Debug("Desertion",
                        $"Saved grace state in StartDesertionGracePeriod: Tier={_savedGraceTier}, XP={_savedGraceXP}");
                }
                else
                {
                    ModLogger.Debug("Desertion",
                        $"Using pre-saved grace state: Tier={_savedGraceTier}, XP={_savedGraceXP}");
                }

                ModLogger.Info("Desertion", $"Started {graceDays}-day grace period to rejoin {kingdom.Name}");

                var message =
                    new TextObject(
                        "{=Enlisted_Message_ServiceEndedGrace}Your service has ended. You have {DAYS} days to find another lord in {KINGDOM} before you are branded a deserter.");
                message.SetTextVariable("DAYS", graceDays);
                message.SetTextVariable("KINGDOM", kingdom.Name);
                InformationManager.DisplayMessage(new InformationMessage(message.ToString()));

                // Fire event for other mods
                OnGracePeriodStarted?.Invoke();

                SyncActivationState("start_grace_period");
            }
            catch (Exception ex)
            {
                ModLogger.Error("Desertion", "Error starting grace period", ex);
            }
        }

        /// <summary>
        ///     Apply desertion penalties when grace period expires and player hasn't rejoined.
        ///     Applies -50 relation with all lords in kingdom, +50 crime rating, and removes player from kingdom.
        /// </summary>
        private void ApplyDesertionPenalties()
        {
            try
            {
                if (_pendingDesertionKingdom == null)
                {
                    ModLogger.Debug("Desertion", "No pending desertion kingdom - clearing grace period");
                    _desertionGracePeriodEnd = CampaignTime.Zero;
                    return;
                }

                ModLogger.Info("Desertion",
                    $"Grace period expired - applying desertion penalties for {_pendingDesertionKingdom.Name}");

                var playerClan = Clan.PlayerClan;
                if (playerClan == null)
                {
                    ModLogger.Error("Desertion", "Cannot apply penalties - player clan is null");
                    ClearDesertionGracePeriod();
                    return;
                }

                // Check if kingdom still exists (edge case: kingdom destroyed during grace period)
                if (_pendingDesertionKingdom.IsEliminated)
                {
                    ModLogger.Info("Desertion",
                        $"Kingdom {_pendingDesertionKingdom.Name} was destroyed - clearing grace period without penalties");
                    ClearDesertionGracePeriod();
                    return;
                }

                // Check if player already left kingdom (edge case: manual kingdom leave)
                if (playerClan.Kingdom != _pendingDesertionKingdom)
                {
                    ModLogger.Info("Desertion",
                        "Player already left kingdom - clearing grace period without penalties");
                    ClearDesertionGracePeriod();
                    return;
                }

                // -50 relation with all lords in kingdom
                var lordsPenalized = 0;
                foreach (Clan clan in _pendingDesertionKingdom.Clans)
                {
                    if (clan.Leader != null && clan.Leader != Hero.MainHero && clan.Leader.IsAlive)
                    {
                        ChangeRelationAction.ApplyPlayerRelation(clan.Leader, -50, true, true);
                        lordsPenalized++;
                    }
                }

                // Apply moderate crime rating (50 points = moderate)
                ChangeCrimeRatingAction.Apply(_pendingDesertionKingdom, 50f, true);

                // Remove from kingdom (become independent)
                if (playerClan.Kingdom == _pendingDesertionKingdom)
                {
                    ChangeKingdomAction.ApplyByLeaveKingdom(playerClan, true);
                }

                // Display notification
                var message =
                    new TextObject(
                        "{=Enlisted_Message_BrandedDeserter}You have been branded a deserter. Your relationship with {KINGDOM} has suffered.");
                message.SetTextVariable("KINGDOM", _pendingDesertionKingdom.Name);
                InformationManager.DisplayMessage(new InformationMessage(message.ToString()));

                ModLogger.Info("Desertion",
                    $"Applied desertion penalties: -50 relation with {lordsPenalized} lords, +50 crime rating, removed from kingdom");

                // Clear grace period state
                ClearDesertionGracePeriod();
            }
            catch (Exception ex)
            {
                ModLogger.Error("Desertion", "Error applying desertion penalties", ex);
                ClearDesertionGracePeriod(); // Always clear state on error
            }
        }

        /// <summary>
        ///     Clear desertion grace period state.
        ///     Called when grace period expires (after penalties) or when player rejoins.
        /// </summary>
        private void ClearDesertionGracePeriod()
        {
            _pendingDesertionKingdom = null;
            _desertionGracePeriodEnd = CampaignTime.Zero;

            if (_savedGraceLord != null)
            {
                Campaign.Current.VisualTrackerManager.RemoveTrackedObject(_savedGraceLord);
            }

            _savedGraceLord = null;

            _savedGraceTier = -1;
            _savedGraceXP = 0;
            _savedGraceTroopId = null;
            _savedGraceEnlistmentDate = CampaignTime.Zero;
            _graceProtectionEnds = CampaignTime.Zero;

            SyncActivationState("clear_grace_period");
        }

        /// <summary>
        ///     Apply desertion penalties for minor faction lords.
        ///     Minor factions don't have a crime rating system, so we apply relation penalties
        ///     with the lord and their clan members, plus a 90-day re-enlistment cooldown.
        /// </summary>
        /// <param name="lord">The minor faction lord the player is deserting from.</param>
        private void ApplyMinorFactionDesertionPenalties(Hero lord)
        {
            try
            {
                if (lord == null)
                {
                    ModLogger.Debug("Desertion", "Cannot apply minor faction penalties - lord is null");
                    return;
                }

                var faction = lord.MapFaction;
                if (faction == null)
                {
                    ModLogger.Debug("Desertion", "Cannot apply minor faction penalties - faction is null");
                    return;
                }

                ModLogger.Info("Desertion", 
                    $"Applying minor faction desertion penalties for {lord.Name} ({faction.Name})");

                // Apply -50 relation with the lord
                var relationPenalty = -50;
                var herosPenalized = 0;

                if (lord.IsAlive && lord != Hero.MainHero)
                {
                    ChangeRelationAction.ApplyPlayerRelation(lord, relationPenalty, true, true);
                    herosPenalized++;
                    ModLogger.Debug("Desertion", $"Applied {relationPenalty} relation with lord {lord.Name}");
                }

                // Apply -50 relation with all clan members
                var lordClan = lord.Clan;
                if (lordClan != null)
                {
                    foreach (var clanMember in lordClan.Heroes)
                    {
                        // Skip the lord (already penalized), dead heroes, and the player
                        if (clanMember == lord || !clanMember.IsAlive || clanMember == Hero.MainHero)
                        {
                            continue;
                        }

                        ChangeRelationAction.ApplyPlayerRelation(clanMember, relationPenalty, true, true);
                        herosPenalized++;
                    }
                }

                // Add 90-day cooldown for this faction
                var cooldownDays = 90;
                var cooldownEnd = CampaignTime.Now + CampaignTime.Days(cooldownDays);
                _minorFactionDesertionCooldowns[faction.StringId] = cooldownEnd;

                // Display notification
                var message = new TextObject(
                    "{=Enlisted_Message_MinorFactionDeserter}You have deserted {LORD}'s company. Your reputation with {FACTION} has suffered, and they will not accept you back for some time.");
                message.SetTextVariable("LORD", lord.Name);
                message.SetTextVariable("FACTION", faction.Name);
                InformationManager.DisplayMessage(new InformationMessage(message.ToString(), Colors.Red));

                ModLogger.Info("Desertion",
                    $"Minor faction desertion penalties applied: {relationPenalty} relation with {herosPenalized} heroes, {cooldownDays}-day cooldown for {faction.Name}");
            }
            catch (Exception ex)
            {
                ModLogger.Error("Desertion", $"Error applying minor faction desertion penalties: {ex.Message}");
            }
        }

        /// <summary>
        ///     Checks if the player is blocked from re-enlisting with a minor faction due to desertion.
        /// </summary>
        /// <param name="faction">The minor faction to check.</param>
        /// <param name="remainingDays">Output: days remaining in cooldown (0 if not blocked).</param>
        /// <returns>True if blocked, false if can enlist.</returns>
        public bool IsBlockedFromMinorFaction(IFaction faction, out int remainingDays)
        {
            remainingDays = 0;

            if (faction == null || _minorFactionDesertionCooldowns == null)
            {
                return false;
            }

            if (_minorFactionDesertionCooldowns.TryGetValue(faction.StringId, out var cooldownEnd))
            {
                if (CampaignTime.Now < cooldownEnd)
                {
                    remainingDays = (int)(cooldownEnd - CampaignTime.Now).ToDays;
                    return true;
                }
                else
                {
                    // Cooldown expired, remove it
                    _minorFactionDesertionCooldowns.Remove(faction.StringId);
                }
            }

            return false;
        }

        /// <summary>
        ///     Voluntarily desert from the army. The player keeps their current equipment but
        ///     receives desertion penalties: -50 relation with all lords in the kingdom and
        ///     +50 crime rating. After deserting, the player is free to enlist with other factions.
        /// </summary>
        public void DesertArmy()
        {
            try
            {
                if (!IsEnlisted)
                {
                    ModLogger.Warn("Desertion", "Cannot desert - not currently enlisted");
                    return;
                }

                var enlistedKingdom = _enlistedLord?.MapFaction as Kingdom;
                var kingdomName = enlistedKingdom?.Name?.ToString() ?? "the army";

                ModLogger.Info("Desertion", $"Player voluntarily deserting from {kingdomName}");

                var playerClan = Clan.PlayerClan;
                if (playerClan == null)
                {
                    ModLogger.Error("Desertion", "Cannot desert - player clan is null");
                    return;
                }

                // Store kingdom reference before clearing enlistment state
                var targetKingdom = enlistedKingdom;

                // === STEP 1: End enlistment WITHOUT restoring equipment ===
                // Player keeps their enlisted gear as "stolen" equipment
                var main = MobileParty.MainParty;
                if (main != null)
                {
                    // Remove from army if in one
                    if (main.Army != null)
                    {
                        try
                        {
                            main.Army = null;
                        }
                        catch (Exception ex)
                        {
                            ModLogger.Error("Desertion", $"Error removing from army: {ex.Message}");
                            main.Army = null;
                        }
                    }

                    // Release escort
                    TryReleaseEscort(main, true);
                    TrySetShouldJoinPlayerBattles(main, false);

                    // Restore party visibility
                    main.IsVisible = true;
                    main.IsActive = true;
                    EncounterGuard.ShowPlayerPartyVisual();
                }

                // Restore companions (but NOT equipment - player keeps enlisted gear)
                RestoreCompanionsToPlayer();

                // Clear the equipment backup flag WITHOUT restoring - player keeps their gear
                // This effectively "forfeits" the backed up equipment
                _hasBackedUpEquipment = false;
                ModLogger.Info("Desertion", "Equipment backup cleared - player keeps enlisted gear");

                // Clear enlistment state
                var previousLord = _enlistedLord;
                _enlistedLord = null;
                _enlistmentTier = 1;
                _enlistmentXP = 0;
                _enlistmentDate = CampaignTime.Zero;
                _disbandArmyAfterBattle = false;

                // Clear any active grace period state
                ClearDesertionGracePeriod();

                // === STEP 2: Apply desertion penalties ===
                if (targetKingdom != null && !targetKingdom.IsEliminated)
                {
                    // Kingdom desertion: -50 relation with all lords in kingdom + crime rating
                    var lordsPenalized = 0;
                    foreach (Clan clan in targetKingdom.Clans)
                    {
                        if (clan.Leader != null && clan.Leader != Hero.MainHero && clan.Leader.IsAlive)
                        {
                            ChangeRelationAction.ApplyPlayerRelation(clan.Leader, -50, true, true);
                            lordsPenalized++;
                        }
                    }

                    // Apply crime rating (+50)
                    ChangeCrimeRatingAction.Apply(targetKingdom, 50f, true);

                    ModLogger.Info("Desertion",
                        $"Applied desertion penalties: -50 relation with {lordsPenalized} lords, +50 crime rating");
                }
                else if (previousLord != null && previousLord.MapFaction != null && !(previousLord.MapFaction is Kingdom))
                {
                    // Minor faction desertion: no crime rating (they have no judicial system),
                    // but apply relation penalties and cooldown
                    ApplyMinorFactionDesertionPenalties(previousLord);
                }

                // === STEP 3: Leave the kingdom ===
                if (playerClan.Kingdom != null)
                {
                    try
                    {
                        ChangeKingdomAction.ApplyByLeaveKingdomAsMercenary(playerClan, true);
                        ModLogger.Info("Desertion", "Left kingdom as deserter");
                    }
                    catch (Exception ex)
                    {
                        ModLogger.Error("Desertion", $"Error leaving kingdom: {ex.Message}");
                    }
                }

                // Clear kingdom tracking
                _originalKingdom = null;
                _wasIndependentClan = false;

                // Display notification
                var message =
                    new TextObject(
                        "You have deserted from {KINGDOM}. You are now branded a deserter and your reputation has suffered greatly.");
                message.SetTextVariable("KINGDOM", targetKingdom?.Name ?? new TextObject("{=enlist_fallback_army}the army"));
                InformationManager.DisplayMessage(new InformationMessage(message.ToString()));

                // Log state transition
                SessionDiagnostics.LogStateTransition("Enlistment", "Enlisted", "Deserted",
                    $"Kingdom: {kingdomName}, Lord: {previousLord?.Name?.ToString() ?? "unknown"}");

                // Fire discharge event
                OnDischarged?.Invoke("Voluntary desertion");
            }
            catch (Exception ex)
            {
                ModLogger.Error("Desertion", "Error during voluntary desertion", ex);
                // Ensure critical state is cleared
                _enlistedLord = null;
                _hasBackedUpEquipment = false;
            }
        }

        private bool TryApplyGraceEquipment(bool resumedFromGrace, string preferredTroopId)
        {
            if (!resumedFromGrace || _enlistedLord?.Culture == null)
            {
                return false;
            }

            var manager = TroopSelectionManager.Instance;
            if (manager == null)
            {
                ModLogger.Error("Enlistment", "TroopSelectionManager unavailable - cannot restore saved equipment");
                return false;
            }

            CharacterObject selectedTroop = null;
            if (!string.IsNullOrEmpty(preferredTroopId))
            {
                try
                {
                    selectedTroop = MBObjectManager.Instance.GetObject<CharacterObject>(preferredTroopId);
                    if (selectedTroop != null && selectedTroop.Culture != _enlistedLord.Culture)
                    {
                        selectedTroop = null;
                    }
                }
                catch
                {
                    selectedTroop = null;
                }
            }

            if (selectedTroop == null)
            {
                var unlocked = manager.GetUnlockedTroopsForCurrentTier(_enlistedLord.Culture.StringId, _enlistmentTier);
                selectedTroop = unlocked?.FirstOrDefault();
            }

            if (selectedTroop == null)
            {
                ModLogger.Info("Enlistment", "Grace enlistment could not find matching troop; using default kit");
                return false;
            }

            manager.ApplySelectedTroopEquipment(Hero.MainHero, selectedTroop);
            return true;
        }

        /// <summary>
        ///     Hourly tick handler that runs once per in-game hour while the player is enlisted.
        ///     Maintains party following, visibility, and encounter prevention state.
        ///     Called automatically by the game every hour.
        /// </summary>
        private void OnHourlyTick()
        {
            // Check if grace period expired (even if not enlisted)
            if (IsInDesertionGracePeriod && CampaignTime.Now >= _desertionGracePeriodEnd)
            {
                ApplyDesertionPenalties();
                return;
            }

            SyncActivationState("hourly_tick");
            if (!EnlistedActivation.IsActive)
            {
                return;
            }

            var main = MobileParty.MainParty;
            if (main == null)
            {
                return;
            }

            // CRITICAL: Enforce map tracker persistence
            // VisualTrackerManager can sometimes lose tracking state (e.g. after load or party changes)
            // We re-apply it here to ensure the user can always find their lord during leave/grace
            if (_isOnLeave && _enlistedLord != null && _enlistedLord.IsAlive)
            {
                if (!Campaign.Current.VisualTrackerManager.CheckTracked(_enlistedLord))
                {
                    Campaign.Current.VisualTrackerManager.RegisterObject(_enlistedLord);
                    ModLogger.Info("Tracker", "Re-applied map tracker for on-leave lord (was missing)");
                }

                var lordParty = _enlistedLord.PartyBelongedTo;
                if (lordParty != null)
                {
                    // Backup: Ensure the party is physically visible
                    if (!lordParty.IsVisible)
                    {
                        lordParty.IsVisible = true;
                        ModLogger.Info("Tracker", "Forced Lord party to be visible (was hidden) - Hourly Check");
                    }
                }
            }
            else if (IsInDesertionGracePeriod && _savedGraceLord != null && _savedGraceLord.IsAlive)
            {
                if (!Campaign.Current.VisualTrackerManager.CheckTracked(_savedGraceLord))
                {
                    Campaign.Current.VisualTrackerManager.RegisterObject(_savedGraceLord);
                    ModLogger.Info("Tracker", "Re-applied map tracker for grace period lord (was missing)");
                }
            }

            // CRITICAL: Ensure non-enlisted players always stay visible and active
            // This is a fallback check in case the realtime tick misses something
            // Hourly tick runs once per in-game hour, providing periodic enforcement
            if (!IsEnlisted)
            {
                if (!Hero.MainHero.IsPrisoner)
                {
                    // Enforce visibility and activity for non-enlisted players as a fallback
                    // This catches any cases where realtime tick might have missed something
                    bool needsActivation = !main.IsActive;
                    bool needsVisibility = !main.IsVisible;

                    if (needsActivation || needsVisibility)
                    {
                        // Log when we're fixing visibility/activity to help diagnose issues
                        if (needsVisibility)
                        {
                            ModLogger.Info("Hourly",
                                "Enforcing visibility for non-enlisted player (was invisible - hourly fallback check)");
                        }

                        if (needsActivation)
                        {
                            ModLogger.Info("Hourly",
                                "Enforcing activity for non-enlisted player (was inactive - hourly fallback check)");
                        }

                        main.IsActive = true;
                        main.IsVisible = true;
                    }
                }

                return; // Skip enlistment logic for non-enlisted players
            }

            // Check if the lord's party still exists
            var counterpartParty = _enlistedLord?.PartyBelongedTo;
            if (counterpartParty == null)
            {
                // The lord's party no longer exists
                // Check if the lord is still alive and in a faction (e.g., joined a garrison, changed kingdoms)
                // This prevents XP/Rank reset when the lord moves to a garrison or changes realms
                if (_enlistedLord != null && _enlistedLord.IsAlive && _enlistedLord.MapFaction is Kingdom lordKingdom)
                {
                    ModLogger.Info("Enlistment",
                        $"Lord's party disbanded (Lord: {_enlistedLord.Name}, Kingdom: {lordKingdom.Name}) - starting grace period");

                    // Stop enlistment but retain state for grace period
                    // This saves the current tier/XP to _savedGraceTier/_savedGraceXP
                    StopEnlist("Party disbanded - awaiting transfer", retainKingdomDuringGrace: true);

                    // Start grace period for the lord's CURRENT kingdom
                    StartDesertionGracePeriod(lordKingdom);

                    // Notify player specifically about the disbanding
                    var message =
                        new TextObject(
                            "{=Enlisted_Message_PartyDisbanded}Your commander's party has disbanded. Locate them to resume service.");
                    InformationManager.DisplayMessage(new InformationMessage(message.ToString()));
                }
                else
                {
                    // The lord's party no longer exists - automatically end service
                    StopEnlist("Target party invalid");
                }

                return;
            }

            // Maintain party attachment to the lord's party using natural attachment system
            // This ensures the player continues to follow the lord during travel and integrates with armies
            // Expense sharing is prevented by EnlistmentExpenseIsolationPatch
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
        ///     Attaches the player's party to the lord's party for following.
        ///     Uses direct position matching to ensure the player follows the lord during travel.
        ///     Delegates to EncounterGuard.TryAttachOrEscort for the actual implementation.
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
            TryReleaseEscort(main, false);
        }

        /// <summary>
        ///     Releases escort AI behavior by setting the party to hold mode.
        /// </summary>
        /// <param name="main">The party to release escort for.</param>
        /// <param name="clearAttachment">
        ///     Legacy parameter - no longer used. We don't set AttachedTo
        ///     because it causes GetGenericStateMenu() to crash when player isn't in an army.
        /// </param>
        internal static void TryReleaseEscort(MobileParty main, bool clearAttachment)
        {
            try
            {
                // Set AI to hold mode to stop following behavior
                // NOTE: We no longer clear AttachedTo because we never set it (causes crashes)
                // 1.3.4 API: SetMoveModeHold is on MobileParty directly
                main.SetMoveModeHold();
                ModLogger.Debug("Following", "Released escort - set hold mode");
            }
            catch (Exception ex)
            {
                ModLogger.Error("Following", $"Error releasing escort: {ex.Message}");
            }
        }

        /// <summary>
        ///     Sets the ShouldJoinPlayerBattles property on a party, using direct access first
        ///     and falling back to reflection if direct access fails.
        ///     This property controls whether the party automatically joins battles.
        ///     For enlisted players, this should be true so they can participate in battles.
        ///     The method handles both modern API (direct property) and older versions (reflection).
        /// </summary>
        /// <param name="party">The party to modify.</param>
        /// <param name="value">True to enable automatic battle joining, false to disable.</param>
        private static void TrySetShouldJoinPlayerBattles(MobileParty party, bool value)
        {
            try
            {
                // Try direct property access first (modern API in current Bannerlord versions)
                // Note: No debug logging here as this is called frequently during normal operation
                party.ShouldJoinPlayerBattles = value;
            }
            catch (Exception ex1)
            {
                try
                {
                    // Use reflection for game versions where the property is not directly accessible
                    var prop = party.GetType().GetProperty("ShouldJoinPlayerBattles",
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (prop != null)
                    {
                        prop.SetValue(party, value, null);
                    }
                    else
                    {
                        ModLogger.Error("Battle", "ShouldJoinPlayerBattles property not found via reflection");
                    }
                }
                catch (Exception ex2)
                {
                    ModLogger.Error("Battle",
                        $"Failed to set ShouldJoinPlayerBattles: Direct={ex1.Message}, Reflection={ex2.Message}");
                }
            }
        }

        /// <summary>
        ///     Processes daily military service benefits: wages and XP progression.
        ///     Called once per in-game day while the player is enlisted.
        ///     Integrates with the duties system to provide additional wage multipliers.
        ///     Also checks for leave expiration and grace period expiration.
        /// </summary>
        private void OnDailyTick()
        {
            // Always run captivity + leave maintenance even when inactive so grace-period escapes
            // and leave expirations are processed for players who temporarily lost activation.
            CheckPlayerCaptivityDuration();

            // Check for leave expiration (14 days max)
            // This runs even when "not enlisted" because IsOnLeave makes IsEnlisted return false
            CheckLeaveExpiration();

            SyncActivationState("daily_tick");
            if (!EnlistedActivation.IsActive)
            {
                return;
            }

            if (!IsEnlisted || _enlistedLord?.IsAlive != true)
            {
                return;
            }

            try
            {
                // Calculate daily wage based on tier, level, and duties
                var wage = CalculateDailyWage();

                // Wage payment is handled by ClanFinanceEnlistmentIncomePatch + ClanFinanceEnlistmentGoldChangePatch
                // which feed into the native daily gold change system.
                // We only log and track stats here to avoid double payment.
                if (wage > 0)
                {
                    // Log wage payment with breakdown context
                    var breakdown = GetWageBreakdown();
                    ModLogger.Info("Gold",
                        $"Wage paid: {wage} denars (base {breakdown.BasePay} + tier {breakdown.TierBonus} + level {breakdown.LevelBonus} + service {breakdown.ServiceBonus} + army {breakdown.ArmyBonus} + duty {breakdown.DutyBonus})");

                    // Track total wages for summary
                    ModLogger.IncrementSummary("wages_earned", 1, wage);

                    // Fire event for other mods
                    OnWagePaid?.Invoke(wage);
                }

                // Award daily XP for military tier progression
                // This is separate from skill XP, which is handled by formation training and duties
                // Base daily XP: 25 points. Additional XP comes from battle participation (75) and duties (15)
                var dailyXP = 25;
                AddEnlistmentXP(dailyXP, "Daily Service");

                ModLogger.Debug("Enlistment", $"Daily service completed: {wage} gold paid, {dailyXP} XP gained");

                // Check for retirement eligibility notification (first term complete)
                CheckRetirementEligibility();

                // Check for renewal term completion
                CheckRenewalTermCompletion();
            }
            catch (Exception ex)
            {
                ModLogger.Error("Enlistment", "Daily service processing failed", ex);
            }
        }

        /// <summary>
        ///     Calculate daily wage based on tier, level, and duties system bonuses.
        ///     Uses config from enlisted_config.json finance section.
        /// </summary>
        private int CalculateDailyWage()
        {
            // Load wage formula from config
            var financeConfig = EnlistedConfig.LoadFinanceConfig();
            var formula = financeConfig.WageFormula;

            // Base wage formula from config: base + (level*levelMult) + (tier*tierMult) + (xp/xpDiv)
            var xpDivisor = formula.XpDivisor > 0 ? formula.XpDivisor : 200;
            var baseWage = formula.BaseWage +
                           Hero.MainHero.Level * formula.LevelMultiplier +
                           _enlistmentTier * formula.TierMultiplier +
                           _enlistmentXP / xpDivisor;

            // Army bonus from config (default +20% when in active army)
            var lordParty = _enlistedLord.PartyBelongedTo;
            var armyMultiplier = lordParty?.Army != null ? formula.ArmyBonusMultiplier : 1.0f;

            // Duties system wage multiplier
            var dutiesMultiplier = GetDutiesWageMultiplier();

            // Apply multipliers and cap
            var finalWage = Math.Min((int)(baseWage * armyMultiplier * dutiesMultiplier), 150);
            return Math.Max(finalWage, 24); // Minimum 24 gold/day
        }

        internal bool TryGetProjectedDailyWage(out int wage)
        {
            wage = 0;

            if (!IsEnlisted || _enlistedLord?.IsAlive != true)
            {
                return false;
            }

            wage = CalculateDailyWage();
            return wage > 0;
        }

        /// <summary>
        ///     Gets a detailed breakdown of daily wage components for tooltip display.
        ///     Returns individual amounts for base pay, tier bonus, army bonus, duty bonus, etc.
        /// </summary>
        internal WageBreakdown GetWageBreakdown()
        {
            var breakdown = new WageBreakdown();

            if (!IsEnlisted || _enlistedLord?.IsAlive != true)
            {
                return breakdown;
            }

            try
            {
                var financeConfig = EnlistedConfig.LoadFinanceConfig();
                var formula = financeConfig.WageFormula;

                // Base soldier's pay
                breakdown.BasePay = formula.BaseWage;

                // Level bonus (experience as a fighter)
                breakdown.LevelBonus = Hero.MainHero.Level * formula.LevelMultiplier;

                // Rank/tier bonus (military rank pay increase)
                breakdown.TierBonus = _enlistmentTier * formula.TierMultiplier;

                // Service bonus (XP accumulated = seniority)
                var xpDivisor = formula.XpDivisor > 0 ? formula.XpDivisor : 200;
                breakdown.ServiceBonus = _enlistmentXP / xpDivisor;

                // Calculate subtotal before multipliers
                var subtotal = breakdown.BasePay + breakdown.LevelBonus + breakdown.TierBonus + breakdown.ServiceBonus;

                // Army campaign bonus (+20% when in active army)
                var lordParty = _enlistedLord?.PartyBelongedTo;
                bool inArmy = lordParty?.Army != null;
                if (inArmy)
                {
                    breakdown.ArmyBonus = (int)(subtotal * (formula.ArmyBonusMultiplier - 1.0f));
                    breakdown.IsInArmy = true;
                }

                // Duty assignment bonus
                var dutiesMultiplier = GetDutiesWageMultiplier();
                if (dutiesMultiplier > 1.0f)
                {
                    var afterArmy = subtotal + breakdown.ArmyBonus;
                    breakdown.DutyBonus = (int)(afterArmy * (dutiesMultiplier - 1.0f));
                    breakdown.ActiveDuty = GetActiveDutyName();
                }

                // Calculate total (with cap)
                breakdown.Total = Math.Min(
                    breakdown.BasePay + breakdown.LevelBonus + breakdown.TierBonus +
                    breakdown.ServiceBonus + breakdown.ArmyBonus + breakdown.DutyBonus,
                    150);
                breakdown.Total = Math.Max(breakdown.Total, 24); // Minimum wage
            }
            catch (Exception ex)
            {
                ModLogger.Error("Wage", $"Failed to calculate wage breakdown: {ex.Message}");
                breakdown.BasePay = 24;
                breakdown.Total = 24;
            }

            return breakdown;
        }

        /// <summary>
        ///     Gets the display name of the current active duty for wage tooltip.
        /// </summary>
        private string GetActiveDutyName()
        {
            try
            {
                var dutiesBehavior = EnlistedDutiesBehavior.Instance;
                if (dutiesBehavior?.IsInitialized != true)
                {
                    return null;
                }

                // Get the primary active duty name
                var activeDuties = dutiesBehavior.ActiveDuties;
                if (activeDuties != null && activeDuties.Count > 0)
                {
                    // Return the first duty that has a wage modifier
                    foreach (var duty in activeDuties)
                    {
                        if (duty != "enlisted") // Skip default duty
                        {
                            return FormatDutyName(duty);
                        }
                    }
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        ///     Formats a duty ID into a display-friendly name.
        /// </summary>
        private string FormatDutyName(string dutyId)
        {
            if (string.IsNullOrEmpty(dutyId))
            {
                return null;
            }

            // Convert snake_case to Title Case
            var words = dutyId.Split('_');
            for (var i = 0; i < words.Length; i++)
            {
                if (words[i].Length > 0)
                {
                    words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1).ToLower();
                }
            }

            return string.Join(" ", words);
        }

        /// <summary>
        ///     Gets the wage multiplier from active duties and professions.
        ///     Different duties and professions provide different wage bonuses,
        ///     allowing players to earn more gold per day based on their assignments.
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
        ///     Check for promotion based on XP thresholds.
        /// </summary>
        private void CheckForPromotion()
        {
            // Load tier XP requirements from progression_config.json
            var tierXPRequirements = EnlistedConfig.GetTierXpRequirements();

            // Get actual max tier from config (e.g., 6 for tiers 1-6)
            var maxTier = EnlistedConfig.GetMaxTier();

            var promoted = false;

            // Check if player has enough XP for next tier (up to max tier)
            while (_enlistmentTier < maxTier && _enlistmentXP >= tierXPRequirements[_enlistmentTier])
            {
                _enlistmentTier++;
                promoted = true;
                ModLogger.Info("Progression", $"Promoted to Tier {_enlistmentTier}");
            }

            // Show promotion notification
            if (promoted)
            {
                var rankNameStr = GetRankName(_enlistmentTier);
                var rankName = new TextObject(rankNameStr);

                var message =
                    new TextObject(
                        "{=Enlisted_Message_PromotionAchieved}Promotion achieved! Your service and dedication have been recognized. You are now a {RANK}.");
                message.SetTextVariable("RANK", rankName);
                InformationManager.DisplayMessage(new InformationMessage(message.ToString()));
            }
        }

        /// <summary>
        ///     Validate loaded state and handle any corruption.
        /// </summary>
        private void ValidateLoadedState()
        {
            // Ensure XP progression is valid
            // Fix: Update max tier from 7 to 6 to match progression_config.json and SetTier validation
            if (_enlistmentTier < 1)
            {
                _enlistmentTier = 1;
            }

            if (_enlistmentTier > 6)
            {
                _enlistmentTier = 6;
            }

            if (_enlistmentXP < 0)
            {
                _enlistmentXP = 0;
            }

            ModLogger.Info("SaveLoad", $"Validated enlistment state - Tier: {_enlistmentTier}, XP: {_enlistmentXP}");
        }

        /// <summary>
        ///     Restores proper party activity state after a save is loaded.
        ///     Called from the first campaign tick after loading, NOT from SyncData.
        ///     The game asserts !IsActive during save load, so we must defer this.
        /// </summary>
        private void RestorePartyStateAfterLoad()
        {
            try
            {
                var main = MobileParty.MainParty;
                if (main == null)
                {
                    ModLogger.Debug("SaveLoad", "RestorePartyStateAfterLoad: MainParty is null, skipping");
                    return;
                }

                if (!IsEnlisted && !Hero.MainHero.IsPrisoner)
                {
                    // Non-enlisted players should be active and visible
                    main.IsActive = true;
                    main.IsVisible = true;
                    ModLogger.Info("SaveLoad", "Post-load: Party activated - not enlisted");
                }
                else if (IsEnlisted)
                {
                    // Enlisted players: check if lord is in battle
                    ModLogger.Info("SaveLoad",
                        $"Post-load: Restoring enlisted state (Lord: {_enlistedLord?.Name}, OnLeave: {_isOnLeave})");

                    // Re-apply ship protection after loading a save while enlisted
                    // This ensures ships remain invulnerable even if the flag wasn't persisted
                    SetPlayerShipsInvulnerable();

                    // Ensure the party can join battles when the lord fights
                    TrySetShouldJoinPlayerBattles(main, true);

                    // Check if we're loading into a save where a battle is already in progress
                    if (_enlistedLord?.PartyBelongedTo != null)
                    {
                        var lordParty = _enlistedLord.PartyBelongedTo;
                        var lordArmy = lordParty.Army;

                        bool lordInBattle = lordParty.Party.MapEvent != null;
                        bool armyInBattle = lordArmy?.LeaderParty?.Party.MapEvent != null;

                        // CRITICAL: Don't override reserve mode settings
                        // If player is waiting in reserve, keep them inactive and out of the MapEvent
                        if (EnlistedEncounterBehavior.IsWaitingInReserve)
                        {
                            ModLogger.Info("SaveLoad",
                                "Post-load: Player is in reserve mode - keeping party inactive");
                            main.IsActive = false;
                            main.IsVisible = false;
                            main.MapEventSide = null;
                        }
                        else if (lordInBattle || armyInBattle)
                        {
                            ModLogger.Info("SaveLoad",
                                $"Post-load: Loaded into active battle! Lord battle: {lordInBattle}, Army battle: {armyInBattle}");
                            main.IsActive = true;
                            main.IsVisible = true;
                        }
                        else
                        {
                            // Not in battle - keep hidden but active for escort AI
                            main.IsActive = true;
                            main.IsVisible = false;
                            main.IgnoreByOtherPartiesTill(CampaignTime.Now + CampaignTime.Hours(1f));
                            main.SetMoveEscortParty(lordParty, MobileParty.NavigationType.Default, false);
                            ModLogger.Info("SaveLoad", "Post-load: Escort AI restored for enlisted player");
                        }
                    }
                    else
                    {
                        // No lord party - might be in grace period or error state
                        main.IsActive = true;
                        main.IsVisible = true;
                        ModLogger.Info("SaveLoad", "Post-load: Lord party not found, keeping player visible");
                    }

                    // At Tier 4+, ensure companions are with the player for the retinue system
                    // This handles saves where companions are stuck in lord's party from earlier enlistment
                    if (_enlistmentTier >= 4)
                    {
                        ReclaimCompanionsFromLord();
                    }
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("SaveLoad", $"Error in RestorePartyStateAfterLoad: {ex.Message}");
            }
        }

        /// <summary>
        ///     Checks if the player has been a prisoner for too long while in enlistment grace period.
        ///     If captured > 3 days, we force an escape/release so the player can actually use their grace period.
        ///     NAVAL SAFETY: Does NOT force escape if the captor party is currently at sea.
        ///     The native EndCaptivityInternal only teleports the player if the captor is on land.
        ///     Forcing escape at sea would strand the player without a ship. We wait until the
        ///     captor returns to land before triggering the escape.
        /// </summary>
        private void CheckPlayerCaptivityDuration()
        {
            if (Hero.MainHero.IsPrisoner && IsInDesertionGracePeriod)
            {
                // Check duration
                var daysInCaptivity = (float)(CampaignTime.Now - Hero.MainHero.CaptivityStartTime).ToDays;
                if (daysInCaptivity > 3f)
                {
                    // NAVAL SAFETY: Check if captor party is at sea
                    // If at sea, do NOT force escape - player would be stranded without a ship
                    // The native EndCaptivityInternal checks IsCurrentlyAtSea before teleporting,
                    // but if we trigger escape while at sea, player ends up at the captor's sea position
                    // with no way to move. Wait until captor reaches land.
                    var captorParty = Hero.MainHero.PartyBelongedToAsPrisoner?.MobileParty;
                    if (captorParty != null && captorParty.IsCurrentlyAtSea)
                    {
                        // Log only occasionally to avoid spam (check if we haven't logged recently)
                        ModLogger.Debug("EventSafety",
                            $"Player held prisoner for {daysInCaptivity:F1} days but captor is at sea - waiting for land before forcing escape");
                        return;
                    }

                    ModLogger.Info("EventSafety",
                        $"Player held prisoner for {daysInCaptivity:F1} days during enlistment grace period - forcing release");

                    // Attempt to force release/escape
                    // Use EndCaptivityAction via reflection since it's internal or we just use the public static ApplyByEscape
                    // Public API: EndCaptivityAction.ApplyByEscape(Hero character, Hero facilitator = null)
                    try
                    {
                        // TaleWorlds.CampaignSystem.Actions.EndCaptivityAction
                        var endCaptivityType = typeof(TaleWorlds.CampaignSystem.Actions.EndCaptivityAction);
                        var applyMethod = endCaptivityType.GetMethod("ApplyByEscape",
                            BindingFlags.Static | BindingFlags.Public);

                        if (applyMethod != null)
                        {
                            applyMethod.Invoke(null, new object[] { Hero.MainHero, null });
                            InformationManager.DisplayMessage(new InformationMessage(
                                "You have managed to escape captivity! Return to your kingdom quickly!"));
                        }
                        else
                        {
                            // Fallback if method not found (unlikely)
                            ModLogger.Error("EventSafety", "Could not find EndCaptivityAction.ApplyByEscape");
                        }
                    }
                    catch (Exception ex)
                    {
                        ModLogger.Error("EventSafety", $"Error forcing player escape: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        ///     Checks if the player's leave has expired and applies desertion penalties if so.
        ///     Also displays a daily reminder of remaining leave days.
        ///     Leave duration is configurable via enlisted_config.json gameplay.leave_max_days.
        /// </summary>
        private void CheckLeaveExpiration()
        {
            // Only check if actually on leave with a valid start date
            if (!_isOnLeave || _leaveStartDate == CampaignTime.Zero || _enlistedLord == null)
            {
                return;
            }

            var daysOnLeave = (CampaignTime.Now - _leaveStartDate).ToDays;
            var maxLeaveDays = EnlistedConfig.LoadGameplayConfig().LeaveMaxDays;
            var remainingDays = maxLeaveDays - (int)daysOnLeave;

            if (daysOnLeave > maxLeaveDays)
            {
                // Get the kingdom for desertion penalties
                var lordKingdom = _enlistedLord.MapFaction as Kingdom;

                // BUG FIX: Check if player has become a VASSAL of the same faction
                // Only vassals get honorable discharge - they elevated their service.
                // Mercenaries who didn't return to the army are still deserters.
                var playerClan = Clan.PlayerClan;
                if (playerClan?.Kingdom != null && lordKingdom != null && 
                    playerClan.Kingdom == lordKingdom && !playerClan.IsUnderMercenaryService)
                {
                    ModLogger.Info("Leave",
                        $"Leave expired but player is now a vassal of {lordKingdom.Name} - clearing leave without penalties");

                    // Clear leave state
                    _isOnLeave = false;
                    _leaveStartDate = CampaignTime.Zero;

                    // Fire event for other mods
                    OnLeaveEnded?.Invoke();

                    // End enlistment cleanly since player is now a vassal
                    StopEnlist("Player became vassal of same faction", isHonorableDischarge: true);

                    var vassalMessage =
                        new TextObject(
                            "{=Enlisted_Message_LeaveExpiredVassal}Your leave has expired, but as a member of {KINGDOM}, your service ends honorably.");
                    vassalMessage.SetTextVariable("KINGDOM", lordKingdom.Name);
                    InformationManager.DisplayMessage(new InformationMessage(vassalMessage.ToString()));
                    return;
                }

                ModLogger.Info("Leave", $"Leave expired after {daysOnLeave:F1} days - applying desertion penalties");

                // Clear leave state first
                _isOnLeave = false;
                _leaveStartDate = CampaignTime.Zero;

                // Apply desertion penalties if player was in a kingdom
                if (lordKingdom != null)
                {
                    // Set pending desertion kingdom so ApplyDesertionPenalties knows which kingdom to penalize
                    _pendingDesertionKingdom = lordKingdom;
                    ApplyDesertionPenalties();
                }

                // Fire event for other mods (before StopEnlist clears state)
                OnLeaveEnded?.Invoke();

                StopEnlist("Leave expired - desertion");

                var message =
                    new TextObject(
                        "{=Enlisted_Message_LeaveExpiredDeserter}Your leave has expired. You have been branded a deserter.");
                InformationManager.DisplayMessage(new InformationMessage(message.ToString()));
            }
            else if (remainingDays <= 7 && remainingDays > 0)
            {
                // Daily warning when leave is running out
                var message =
                    new TextObject("{=Enlisted_Message_LeaveRemaining}Leave: {DAYS} days remaining before desertion.");
                message.SetTextVariable("DAYS", remainingDays);
                InformationManager.DisplayMessage(new InformationMessage(message.ToString()));
            }
        }

        /// <summary>
        ///     Real-time tick handler that runs every game frame while the player is enlisted.
        ///     Manages party state (active/inactive), battle participation, army membership,
        ///     and position following in real-time to ensure smooth gameplay.
        ///     This method is throttled to run every 100ms to prevent overwhelming the game's
        ///     rendering system with too-frequent state changes.
        /// </summary>
        /// <param name="deltaTime">Time elapsed since last frame, in seconds. Must be positive.</param>
        private void OnRealtimeTick(float deltaTime)
        {
            // CRITICAL: Skip during character creation when campaign isn't initialized
            // Accessing Hero.MainHero or MobileParty.MainParty during character creation throws exceptions
            if (!CampaignSafetyGuard.IsCampaignReady)
            {
                return;
            }

            SyncActivationState("realtime_tick");

            // When inactive, do nothing at all (mod effectively disabled).
            if (!EnlistedActivation.EnsureActive())
            {
                return;
            }

            // Wait for game initialization to complete before modifying party state
            // This prevents assertion failures during save loading when IsActive must be false
            if (_initializationTicksRemaining > 0)
            {
                _initializationTicksRemaining--;
                return;
            }

            // Handle party state initialization after the startup delay
            // For loaded games: restore after SyncData sets _needsPostLoadStateRestore
            // For new games: just enable modifications once campaign is ready
            if (!_isPartyStateInitialized)
            {
                if (_needsPostLoadStateRestore)
                {
                    // Loaded game - do the full restoration
                    _needsPostLoadStateRestore = false;
                    RestorePartyStateAfterLoad();
                    _isPartyStateInitialized = true;
                    ModLogger.Info("SaveLoad", "Loaded game: Party state initialized after restore");
                }
                else
                {
                    // New game or already restored - just enable modifications
                    _isPartyStateInitialized = true;
                    ModLogger.Info("SaveLoad", "New game: Party state initialized");
                }
            }

            // CRITICAL: Ensure non-enlisted players always stay visible and active
            // Native game systems might change visibility (e.g., night events or scripted encounters)
            // This check runs BEFORE throttling to ensure it's checked every frame
            // We need to enforce visibility for non-enlisted players during gameplay
            if (!IsEnlisted)
            {
                var mainParty = CampaignSafetyGuard.SafeMainParty;
                var mainHero = CampaignSafetyGuard.SafeMainHero;
                if (mainParty != null && mainHero != null && !mainHero.IsPrisoner)
                {
                    // Enforce visibility and activity for non-enlisted players
                    // This ensures the player remains visible even if native systems switch it off
                    // Check both IsActive and IsVisible to catch any state changes
                    bool needsActivation = !mainParty.IsActive;
                    bool needsVisibility = !mainParty.IsVisible;

                    if (needsActivation || needsVisibility)
                    {
                        // Log when we're fixing visibility/activity to help diagnose issues
                        if (needsVisibility)
                        {
                            ModLogger.Info("Realtime",
                                "Enforcing visibility for non-enlisted player (was invisible - possibly native night visibility)");
                        }

                        if (needsActivation)
                        {
                            ModLogger.Info("Realtime", "Enforcing activity for non-enlisted player (was inactive)");
                        }

                        mainParty.IsActive = true;
                        mainParty.IsVisible = true;
                    }
                }

                return; // Skip enlistment logic for non-enlisted players
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
            var intervalMilliseconds = (int)(_realtimeUpdateIntervalSeconds * 1000);
            if (CampaignTime.Now - _lastRealtimeUpdate < CampaignTime.Milliseconds(intervalMilliseconds))
            {
                return;
            }

            _lastRealtimeUpdate = CampaignTime.Now;

            var main = MobileParty.MainParty;
            if (main == null)
            {
                return;
            }

            // If the player is inside any settlement, let native menus handle state.
            // Keep the party active/visible temporarily for UI, but skip enlistment logic.
            if (main.CurrentSettlement != null)
            {
                if (!main.IsActive)
                {
                    main.IsActive = true;
                }

                return;
            }

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
                    bool playerInArmy = mainParty?.Army != null;
                    bool lordInArmy = lordParty.Army != null;
                    var lordInSiege = IsPartyInActiveSiege(lordParty);
                    var playerInSiege = IsPartyInActiveSiege(mainParty);

                    if (!playerInSiege && mainParty.BesiegerCamp != null)
                    {
                        mainParty.BesiegerCamp = null;
                        ModLogger.Debug("Siege",
                            "Cleared stale player besieger camp reference (no active siege detected)");
                    }

                    // Ensure the player is in the lord's army when the lord is in an army
                    // Join seamlessly when the LORD physically merges with the army (not when first assigned)
                    // This prevents teleporting the player across the map - we wait for our lord to arrive first
                    if (lordInArmy && (mainParty.Army == null || mainParty.Army != lordParty.Army))
                    {
                        var targetArmy = lordParty.Army;
                        var armyLeader = targetArmy?.LeaderParty;

                        if (targetArmy == null || armyLeader == null)
                        {
                            ModLogger.Debug("Battle",
                                "Lord army reference invalid (null leader) - skipping automatic join this tick");
                        }
                        else
                        {
                            // Check if LORD has physically merged with the army
                            // Lord is merged when: they ARE the leader, OR they're very close to the leader
                            bool lordIsArmyLeader = lordParty == armyLeader;
                            var lordDistanceToLeader = lordIsArmyLeader
                                ? 0f
                                : lordParty.GetPosition2D.Distance(armyLeader.GetPosition2D);
                            var lordHasMerged = lordIsArmyLeader || lordDistanceToLeader < 3.0f;

                            // Also allow immediate join for urgent battles if lord is reasonably close
                            var urgentBattleNeed = (lordHasMapEvent || lordInSiege) && lordDistanceToLeader < 50.0f;

                            if (lordHasMerged || urgentBattleNeed)
                            {
                                try
                                {
                                    // CRITICAL: Set Army property FIRST, then call AddPartyToMergedParties
                                    // Native code does it in this order - the UI update in AddPartyToMergedParties
                                    // reads MainParty.Army to show the Army HUD, so it must be set first
                                    mainParty.Army = targetArmy;
                                    targetArmy.AddPartyToMergedParties(mainParty);

                                    // If the army is currently besieging, align the player's besieger camp
                                    TrySyncBesiegerCamp(mainParty, lordParty);

                                    if (urgentBattleNeed && !lordHasMerged)
                                    {
                                        ModLogger.Info("Battle",
                                            $"URGENT: Joined army for active battle/siege (Army: {armyLeader?.LeaderHero?.Name})");
                                    }
                                    else
                                    {
                                        ModLogger.Info("Battle",
                                            $"Lord merged with army - player joining (Army: {armyLeader?.LeaderHero?.Name})");
                                    }
                                }
                                catch (Exception ex)
                                {
                                    ModLogger.Error("Battle", $"Error joining lord's army: {ex.Message}");
                                }
                            }
                        }
                    }

                    // NOTE: Removed spammy battle state logging that was causing infinite loop during sieges
                    // These logs were firing every tick, flooding the log and causing freezes

                    // Monitor siege state and prepare party for siege encounter creation
                    // This ensures the player can participate in sieges properly
                    SiegeWatchdogTick(mainParty, lordParty);

                    // Handle battle participation when the lord enters battle but the player hasn't yet
                    // This ensures the player joins the battle even if they weren't initially collected
                    // CRITICAL: Skip if player is deliberately waiting in reserve - don't interrupt that menu
                    string currentMenuId = Campaign.Current?.CurrentMenuContext?.GameMenu?.StringId;
                    var isWaitingInReserve = currentMenuId == "enlisted_battle_wait";

                    if (lordHasMapEvent && !playerHasMapEvent && !isWaitingInReserve)
                    {
                        // Only attempt to add the player to the army if they're not already in it
                        // If they're already in the correct army, the native system should collect them automatically
                        if (mainParty.Army == null || mainParty.Army != lordParty.Army)
                        {
                            ModLogger.Info("Battle", "Lord entered battle - ensuring player can participate");
                            ModLogger.Info("Battle",
                                $"Pre-state: Player Army={mainParty?.Army?.LeaderParty?.LeaderHero?.Name?.ToString() ?? "null"}");

                            try
                            {
                                // Don't clear menus if the lord is in battle or siege
                                // Clearing menus during these operations can cause assertion failures
                                if (InBattleOrSiege(lordParty))
                                {
                                    ModLogger.Info("Battle",
                                        "Lord in battle/siege - skipping menu clearing to prevent assertion failures");
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

                                // Army joining is now handled in OnRealtimeTick when lord merges with army
                                // If already in lord's army, just ensure battle participation flags are set
                                if (lordParty.Army != null && mainParty.Army == lordParty.Army)
                                {
                                    ModLogger.Debug("Battle",
                                        "Already in lord's army - setting battle participation flags");
                                    mainParty.IsActive = true;
                                    mainParty.IsVisible = false;
                                    mainParty.IgnoreByOtherPartiesTill(CampaignTime.Now);
                                    lordParty.Party.SetAsCameraFollowParty();
                                    mainParty.ShouldJoinPlayerBattles = true;
                                }
                                else if (lordParty.Army == null)
                                {
                                    // Lord has no army but is in battle - create a PlayerEncounter to join directly
                                    // PlayerEncounter.Init() will automatically join the existing MapEvent
                                    ModLogger.Info("Battle",
                                        "Lord has no army - attempting direct PlayerEncounter join");

                                    var lordMapEvent = lordParty.Party.MapEvent;
                                    if (lordMapEvent != null)
                                    {
                                        // Determine which side the lord is on
                                        bool lordIsAttacker =
                                            lordMapEvent.AttackerSide.Parties.Any(p => p.Party == lordParty.Party);
                                        BattleSideEnum lordSide = lordIsAttacker
                                            ? BattleSideEnum.Attacker
                                            : BattleSideEnum.Defender;

                                        // Check if we can join on the lord's side using native faction checks
                                        bool canJoinNatively = lordMapEvent.CanPartyJoinBattle(mainParty.Party, lordSide);
                                        
                                        // Check if lord is in a minor/bandit faction (not a Kingdom)
                                        // Minor/bandit faction lords don't trigger the mercenary join logic, leaving the player's
                                        // faction state independent. This causes CanPartyJoinBattle to fail because the
                                        // player isn't formally "at war" with bandits/enemies.
                                        bool isNonKingdomFactionLord = _enlistedLord?.MapFaction != null && 
                                                                       !(_enlistedLord.MapFaction is Kingdom) &&
                                                                       (_enlistedLord.MapFaction.IsMinorFaction || 
                                                                        _enlistedLord.MapFaction.IsBanditFaction);
                                        
                                        // Determine if we should bypass the native faction check
                                        // Only bypass for minor/bandit faction lords where we KNOW the faction logic doesn't work
                                        bool shouldBypassFactionCheck = !canJoinNatively && isNonKingdomFactionLord;
                                        
                                        if (canJoinNatively)
                                        {
                                            ModLogger.Debug("Battle",
                                                $"Can join battle on lord's side ({lordSide}) - preparing for battle participation");
                                        }
                                        else if (shouldBypassFactionCheck)
                                        {
                                            // NON-KINGDOM FACTION FIX: Bypass the native faction check for minor/bandit faction lords.
                                            // CanPartyJoinBattle requires player.MapFaction.IsAtWarWith(enemy), but enlisted
                                            // players with non-Kingdom lords aren't joined to the faction (no Kingdom to join).
                                            // As an enlisted soldier, we logically belong to the lord's side regardless of
                                            // formal faction state. Skip MapEventSide (which crashes without faction compat)
                                            // but create PlayerEncounter so the hero can participate.
                                            ModLogger.Debug("Battle",
                                                $"Non-Kingdom faction lord battle - bypassing native faction check for {lordSide} side");
                                        }
                                        else
                                        {
                                            // Kingdom lord but faction check failed - this is unexpected
                                            // Log warning but don't force bypass - let native system handle it
                                            ModLogger.Warn("Battle",
                                                $"Faction check failed for Kingdom lord battle ({lordSide} side) - player may not be able to join. Please report this if battle participation fails.");
                                            // Don't return here - still try to set up PlayerEncounter as fallback
                                        }

                                        // Make the player active for the encounter but keep them INVISIBLE
                                        // CRITICAL: If IsVisible = true, the native system creates a "Help [Party]" encounter menu
                                        // instead of directly joining the lord's battle. Keeping invisible ensures seamless battle entry.
                                        mainParty.IsActive = true;
                                        mainParty.IsVisible = false;
                                        mainParty.ShouldJoinPlayerBattles = true;

                                        // Clear any ignore window so we can be collected
                                        mainParty.IgnoreByOtherPartiesTill(CampaignTime.Now);

                                        // NAVAL BATTLE FIX: Check if this is a naval battle
                                        // Naval battles require ships - enlisted players have no ships (they're passengers on lord's ship)
                                        // Skip direct MapEvent join for naval battles to prevent spawn crashes
                                        // The player will still participate via PlayerEncounter as crew member on lord's ship
                                        bool isNavalBattle = lordMapEvent.IsNavalMapEvent;

                                        // Determine if we should set MapEventSide to join the battle
                                        // NOTE: The old code avoided setting MapEventSide for parties with 0 troops,
                                        // fearing a crash in ApplySimulatedHitRewardToSelectedTroop during auto-sim.
                                        // However, this prevented enlisted players from joining small battles entirely!
                                        // The auto-sim crash only occurs if the player uses "Send Troops" with 0 troops,
                                        // which won't happen in practice - players will use "Attack" for manual combat.
                                        // By setting MapEventSide, the player properly joins the MapEvent and can participate.
                                        var targetSide = lordSide == BattleSideEnum.Attacker
                                            ? lordMapEvent.AttackerSide
                                            : lordMapEvent.DefenderSide;

                                        if (isNavalBattle)
                                        {
                                            // Naval battles - join MapEventSide so Init() works, Naval DLC handles ship assignment
                                            mainParty.Party.MapEventSide = targetSide;
                                            ModLogger.Info("Naval",
                                                $"Naval battle detected - joined MapEventSide on {lordSide} side (Naval DLC will assign ship)");
                                        }
                                        else if (canJoinNatively || shouldBypassFactionCheck)
                                        {
                                            // Land battle - join the MapEvent on lord's side
                                            mainParty.Party.MapEventSide = targetSide;
                                            int partyTroopCount = mainParty.Party.NumberOfRegularMembers;
                                            ModLogger.Info("Battle",
                                                $"Joined MapEvent on {lordSide} side (troops: {partyTroopCount}, bypass: {shouldBypassFactionCheck})");
                                        }
                                        else
                                        {
                                            // Kingdom lord but faction check failed - try anyway, but warn
                                            mainParty.Party.MapEventSide = targetSide;
                                            ModLogger.Warn("Battle",
                                                $"Faction check failed but joining MapEvent anyway on {lordSide} side - report if battle doesn't work");
                                        }

                                        // CRITICAL: Create and initialize a PlayerEncounter to show encounter menu.
                                        // Now that MapEventSide is set, MobileParty.MainParty.MapEvent returns the battle.
                                        // The public static PlayerEncounter.Init() will properly initialize the encounter.
                                        // Guard flag prevents repeated creation during the same battle (causes loops).
                                        if (PlayerEncounter.Current == null && !_playerEncounterCreatedForBattle)
                                        {
                                            _playerEncounterCreatedForBattle = true;
                                            
                                            // Start the encounter, then Init() uses MainParty.MapEvent
                                            PlayerEncounter.Start();
                                            PlayerEncounter.Init();
                                            
                                            var attackerName = lordMapEvent.AttackerSide?.LeaderParty?.Name?.ToString() ?? "unknown";
                                            var defenderName = lordMapEvent.DefenderSide?.LeaderParty?.Name?.ToString() ?? "unknown";
                                            ModLogger.Info("Battle", 
                                                $"Initialized PlayerEncounter (attacker: {attackerName}, defender: {defenderName})");
                                        }
                                        else if (_playerEncounterCreatedForBattle)
                                        {
                                            // Already created for this battle, don't spam logs
                                        }
                                    }
                                    else
                                    {
                                        ModLogger.Info("Battle",
                                            "Lord has MapEvent flag but MapEvent is null - cannot join directly");
                                    }
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
                    var anyBattleActive = lordInBattle || armyInBattle;

                    // Check if the player is already in a battle
                    bool playerInBattle = mainParty?.Party.MapEvent != null;

                    // Check for siege state (both BesiegedSettlement and SiegeEvent)
                    // During sieges, the player needs to be active/visible even before the assault starts
                    // to allow siege menus to appear

                    // Keep the player's party position matched to the lord's position
                    // This ensures the player follows the lord during travel
                    // Position is handled by Escort AI (SetMoveEscortParty)

                    // Determine if the player needs to be active for battle participation
                    // The player should be activated when a battle is starting but they haven't joined yet
                    // The native system collects nearby active parties when a battle starts
                    var playerNeedsToBeActiveForBattle = anyBattleActive && !playerInBattle;

                    // During sieges, keep the player active and visible so siege menus can appear
                    if (lordInSiege || playerInSiege)
                    {
                        // CRITICAL: Only activate party if an assault (MapEvent) is actually in progress.
                        // If we activate during the "waiting" phase (siege prep/camp), the native engine
                        // constantly triggers the "Settlement is under siege" menu loop.
                        // As an enlisted soldier, we only need to act when the Lord attacks/defends (Assault).
                        var isAssault = lordParty.Party.MapEvent != null || mainParty.Party.MapEvent != null;

                        if (isAssault)
                        {
                            // Assault in progress - activate so we can join the battle/encounter
                            if (!mainParty.IsActive)
                            {
                                mainParty.IsActive = true;
                                ModLogger.Debug("Siege",
                                    "Assault started - activating player for battle participation");
                            }
                        }
                        else
                        {
                            // No assault yet (just siege prep/waiting) - keep inactive to prevent menu loops
                            // EXCEPTION: If the siege watchdog just prepared the player (latch is set),
                            // don't deactivate - the watchdog expects the player to stay active for vanilla to create encounters
                            if (mainParty.IsActive && !_isSiegePreparationLatched)
                            {
                                mainParty.IsActive = false;
                                ModLogger.Debug("Siege",
                                    "Siege waiting phase - deactivating player to prevent menu loop");
                            }
                        }

                        // Player banner should stay hidden even during siege prep
                        mainParty.IsVisible = false;

                        // Army joining is now handled in OnRealtimeTick when lord merges
                        // If in lord's army, siege menus work automatically

                        // If the player enters a settlement while having an active PlayerEncounter, finish it immediately
                        // This prevents assertion failures that can occur when encounters persist after settlement entry
                        // Check both InsideSettlement and CurrentSettlement to ensure we catch all cases
                        if (PlayerEncounter.Current != null && !PlayerEncounter.InsideSettlement)
                        {
                            if (mainParty?.CurrentSettlement != null)
                            {
                                try
                                {
                                    ModLogger.Info("Siege",
                                        "Player entered settlement with active PlayerEncounter - finishing to prevent assertion");
                                    PlayerEncounter.Finish(true);
                                }
                                catch (Exception ex)
                                {
                                    ModLogger.Error("Siege",
                                        $"Error finishing PlayerEncounter when entering settlement: {ex.Message}");
                                }
                            }
                        }

                        // CRITICAL: Do NOT create PlayerEncounter in realtime tick - let the native system handle it
                        // Creating encounters in realtime tick causes rapid loops and zero-delta-time assertion failures
                        // The OnMapEventStarted handler will create the encounter when the siege battle starts
                        // For siege menus before assault, the native system creates encounters naturally
                        // We only need to ensure the party is active/visible, which we already did above
                        // NOTE: Removed spammy debug log here that fired every tick
                    }
                    else if (!playerNeedsToBeActiveForBattle)
                    {
                        // No battle active, or player already in battle
                        if (playerInBattle)
                        {
                            // Player already in battle - keep invisible (banner should be hidden during enlisted service)
                            // Visibility only needed for battle UI menus before joining, not during active battle
                            mainParty.IsVisible = false;
                            ModLogger.Debug("Battle",
                                "Player already in battle - keeping invisible (enlisted service)");
                        }
                        else
                        {
                            // When the lord is in an army, the player must be active to be collected into battles
                            // The game's battle collection system checks for active parties before or during MapEvent creation
                            // If the player is inactive, they won't be included in the battle even if they're in the army
                            // Army membership prevents random encounters, but the player needs to be active for battle collection
                            // CRITICAL: Keep party INVISIBLE even when active - prevents icon rendering
                            // Making it visible causes party icon to appear with troop count and map marker issues
                            // Battle collection uses IsActive, not IsVisible - visibility only needed for UI menus (already handled separately)
                            if (lordInArmy && mainParty.Army == lordParty.Army)
                            {
                                // Lord is in an army and the player is in the same army
                                // Keep the player active for battle collection and escort AI, but INVISIBLE
                                if (!mainParty.IsActive)
                                {
                                    mainParty.IsActive = true;
                                    TrySetShouldJoinPlayerBattles(mainParty, true);
                                    ModLogger.Debug("Battle", "Activated player party for army following");
                                }

                                // Check if lord is at sea - use direct position sync instead of escort AI
                                // Naval War Expansion: Escort AI doesn't work when target is at sea without ships
                                if (!TrySyncNavalPosition(mainParty, lordParty))
                                {
                                    // Lord is on land - sync player's sea state if they're still at sea (disembark)
                                    if (mainParty.IsCurrentlyAtSea && !lordParty.IsCurrentlyAtSea)
                                    {
                                        mainParty.IsCurrentlyAtSea = false;
                                        mainParty.Position = lordParty.Position;
                                        ModLogger.Debug("Naval",
                                            "Synced player sea state to land (disembarked with lord in army)");
                                    }

                                    // Lord is on land - use normal escort AI
                                    mainParty.SetMoveEscortParty(lordParty, MobileParty.NavigationType.Default, false);
                                    lordParty.Party.SetAsCameraFollowParty();

                                    // FIX: Aggressive position sync when player is lagging behind
                                    // This prevents the issue where the player spawns behind their formation in battle
                                    // because their map party was slightly behind the lord when battle started
                                    TryAggressivePositionSync(mainParty, lordParty, "army");
                                }

                                // Keep invisible to prevent banner/icon appearing
                                mainParty.IsVisible = false;
                            }
                            else
                            {
                                // Lord is not in an army - keep party active for escort AI to work
                                // Use IgnoreByOtherPartiesTill to prevent random encounters instead of IsActive = false
                                mainParty.IsVisible = false;

                                // CRITICAL: Keep party ACTIVE so escort AI works for following
                                if (!mainParty.IsActive)
                                {
                                    mainParty.IsActive = true;
                                }

                                // Refresh ignore window to prevent random encounters
                                mainParty.IgnoreByOtherPartiesTill(CampaignTime.Now + CampaignTime.Hours(1f));

                                // Check if lord is at sea - use direct position sync instead of escort AI
                                // Naval War Expansion: Escort AI doesn't work when target is at sea without ships
                                if (!TrySyncNavalPosition(mainParty, lordParty))
                                {
                                    // Lord is on land - sync player's sea state if they're still at sea (disembark)
                                    if (mainParty.IsCurrentlyAtSea && !lordParty.IsCurrentlyAtSea)
                                    {
                                        mainParty.IsCurrentlyAtSea = false;
                                        mainParty.Position = lordParty.Position;
                                        ModLogger.Debug("Naval",
                                            "Synced player sea state to land (disembarked with lord)");
                                    }

                                    // Lord is on land - use normal escort AI
                                    mainParty.SetMoveEscortParty(lordParty, MobileParty.NavigationType.Default, false);
                                    lordParty.Party.SetAsCameraFollowParty();

                                    // FIX: Aggressive position sync when player is lagging behind
                                    // This prevents the issue where the player spawns behind their formation in battle
                                    // because their map party was slightly behind the lord when battle started
                                    TryAggressivePositionSync(mainParty, lordParty, "solo");
                                }
                            }
                        }
                    }
                    else
                    {
                        // Lord is in battle but the player hasn't joined yet
                        // Army joining is now handled when lord merges - just activate for battle collection

                        // Only activate if the party doesn't already have a MapEvent (safe to activate)
                        // The MapEvent property exists on Party, not directly on MobileParty
                        // This is the correct API structure for checking battle state
                        if (mainParty.Party.MapEvent == null && !mainParty.IsActive)
                        {
                            mainParty.IsActive = true;
                            // Ensure battle participation flags are set so the player can join battles
                            // Follow the LORD's party with camera to prevent pausing when lord enters battle
                            lordParty.Party.SetAsCameraFollowParty();
                            TrySetShouldJoinPlayerBattles(mainParty, true);
                            ModLogger.Debug("Battle",
                                $"Activated party for battle collection (lord in battle, player in army: {playerInArmy})");
                        }

                        // CRITICAL: After battle UI is shown, reset visibility to false if player joined the battle
                        // This ensures banner doesn't remain visible after battle participation
                        if (playerInBattle)
                        {
                            mainParty.IsVisible = false;
                            ModLogger.Debug("Battle", "Reset visibility to false - player joined battle");
                        }
                    }

                    // Configure party ignoring behavior to prevent unwanted encounters
                    // The party should ignore other parties when not in battle, but allow encounters
                    // when a battle involving the lord is active (so the player can be collected)
                    // The MapEvent property exists on Party, not directly on MobileParty
                    var lordHasEvent = lordParty?.Party.MapEvent != null ||
                                       lordParty?.Army?.LeaderParty?.Party.MapEvent != null;
                    if (!lordHasEvent)
                    {
                        // No battle active - ignore other parties for 0.5 hours to prevent random encounters
                        mainParty.IgnoreByOtherPartiesTill(CampaignTime.Now + CampaignTime.Hours(0.5f));
                        // NOTE: Removed spammy debug log here that fired every tick
                    }
                    else
                    {
                        // Battle active - don't ignore parties so the player can be collected into the battle
                        ModLogger.Debug("Realtime",
                            "NOT ignoring parties - lord battle active, allowing encounter collection");
                    }
                }
            }
        }

        private void TrySyncBesiegerCamp(MobileParty mainParty, MobileParty lordParty)
        {
            try
            {
                var siegeEvent = lordParty?.Party?.SiegeEvent
                                 ?? lordParty?.BesiegedSettlement?.SiegeEvent
                                 ?? lordParty?.Army?.LeaderParty?.Party?.SiegeEvent;

                var targetCamp = siegeEvent?.BesiegerCamp ?? lordParty?.BesiegedSettlement?.SiegeEvent?.BesiegerCamp;

                if (targetCamp == null)
                {
                    if (mainParty?.BesiegerCamp != null)
                    {
                        ModLogger.Debug("Battle", "Clearing player besieger camp - no active siege event detected");
                        mainParty.BesiegerCamp = null;
                    }
                    else
                    {
                        ModLogger.Debug("Battle", "No siege event/camp available while joining army (safe)");
                    }

                    return;
                }

                if (mainParty?.BesiegerCamp == targetCamp)
                {
                    ModLogger.Debug("Battle",
                        $"Player besieger camp already synced ({targetCamp.SiegeEvent?.BesiegedSettlement?.Name?.ToString() ?? "unknown"})");
                    return;
                }

                mainParty.BesiegerCamp = targetCamp;
                ModLogger.Info("Battle",
                    $"Synced player besieger camp with lord's siege at {targetCamp.SiegeEvent?.BesiegedSettlement?.Name?.ToString() ?? "unknown"}");
            }
            catch (Exception ex)
            {
                ModLogger.Error("Battle", $"Failed to sync besieger camp: {ex.Message}");
            }
        }

        /// <summary>
        ///     Aggressively syncs the player's position with the lord's position to ensure the player
        ///     is always at the battle location when combat starts.
        ///     This fixes the issue where players spawn behind their formation in battle because
        ///     their map party was slightly behind the lord when the battle MapEvent was created.
        ///     The sync happens when:
        ///     - Player is more than 3 units behind the lord (normal lag)
        ///     - Lord is approaching an enemy party (pre-battle sync)
        ///     - Lord's army is about to engage (army battle sync)
        /// </summary>
        /// <param name="mainParty">The player's party.</param>
        /// <param name="lordParty">The lord's party that the player is following.</param>
        /// <param name="context">Context string for logging (e.g., "army", "solo").</param>
        private void TryAggressivePositionSync(MobileParty mainParty, MobileParty lordParty, string context)
        {
            try
            {
                if (mainParty == null || lordParty == null)
                {
                    return;
                }

                // Skip if player is in a settlement (handled separately)
                if (mainParty.CurrentSettlement != null)
                {
                    return;
                }

                var playerPos = mainParty.GetPosition2D;
                var lordPos = lordParty.GetPosition2D;
                var distance = playerPos.Distance(lordPos);

                // Always sync if player is more than 3 units behind
                // This is aggressive to ensure player is ALWAYS at lord's location for battle spawning
                const float MaxAllowedDistance = 3f;

                // Also check if lord is near an enemy (potential combat) - be even more aggressive
                var lordNearEnemy = IsLordNearEnemy(lordParty);
                var syncDistance = lordNearEnemy ? 1f : MaxAllowedDistance; // Even tighter sync near combat

                if (distance > syncDistance)
                {
                    mainParty.Position = lordParty.Position;

                    // Only log when the distance was significant (to avoid log spam)
                    if (distance > 5f || lordNearEnemy)
                    {
                        ModLogger.Debug("PositionSync",
                            $"[{context}] Synced player position to lord (was {distance:F1} units behind" +
                            (lordNearEnemy ? ", lord near enemy)" : ")"));
                    }
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("PositionSync", $"Error in aggressive position sync: {ex.Message}");
            }
        }

        /// <summary>
        ///     Checks if the lord's party is near an enemy party that could result in battle.
        ///     Used to trigger more aggressive position syncing before combat.
        /// </summary>
        /// <param name="lordParty">The lord's party to check.</param>
        /// <returns>True if the lord is near an enemy and combat is likely.</returns>
        private bool IsLordNearEnemy(MobileParty lordParty)
        {
            try
            {
                if (lordParty == null)
                {
                    return false;
                }

                // Check if lord is already in battle (most obvious case)
                if (lordParty.Party.MapEvent != null)
                {
                    return true;
                }

                // Check if lord's army is in battle
                if (lordParty.Army?.LeaderParty?.Party.MapEvent != null)
                {
                    return true;
                }

                // Check for nearby hostile parties using the lord's current target
                var targetParty = lordParty.TargetParty;
                if (targetParty != null)
                {
                    // Check if targeting a hostile party
                    bool isHostile = FactionManager.IsAtWarAgainstFaction(lordParty.MapFaction, targetParty.MapFaction);
                    if (isHostile)
                    {
                        float distanceToTarget = lordParty.GetPosition2D.Distance(targetParty.GetPosition2D);
                        // If within 10 units of an enemy, combat is imminent
                        return distanceToTarget < 10f;
                    }
                }

                // Check if lord is in an army that's targeting something
                var armyTarget = lordParty.Army?.AiBehaviorObject;
                if (armyTarget != null && armyTarget is MobileParty armyTargetParty)
                {
                    bool isHostile =
                        FactionManager.IsAtWarAgainstFaction(lordParty.MapFaction, armyTargetParty.MapFaction);
                    if (isHostile)
                    {
                        var armyLeader = lordParty.Army.LeaderParty;
                        if (armyLeader != null)
                        {
                            float distanceToTarget = armyLeader.GetPosition2D.Distance(armyTargetParty.GetPosition2D);
                            return distanceToTarget < 15f; // Slightly larger radius for army engagements
                        }
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                ModLogger.Debug("PositionSync", $"Error checking if lord near enemy: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        ///     Syncs the player's position and sea state with the lord when the lord is at sea.
        ///     This is needed because the normal escort AI doesn't work when the target is at sea
        ///     and the follower doesn't have naval navigation capability (ships).
        ///     Instead, we directly teleport the player to the lord's position and sync their sea state.
        /// </summary>
        /// <param name="mainParty">The player's party.</param>
        /// <param name="lordParty">The lord's party that the player is following.</param>
        /// <returns>True if the lord is at sea and position was synced, false otherwise.</returns>
        private bool TrySyncNavalPosition(MobileParty mainParty, MobileParty lordParty)
        {
            try
            {
                if (mainParty == null || lordParty == null)
                {
                    return false;
                }

                // Check if the lord is currently at sea (sailing on a ship)
                if (!lordParty.IsCurrentlyAtSea)
                {
                    return false;
                }

                // Lord is at sea - sync position and sea state directly
                // The escort AI won't work across land/sea boundaries without ships
                var lordPosition = lordParty.Position;
                var playerPosition = mainParty.Position;

                // Only sync if positions differ significantly (avoid unnecessary updates)
                var distance = playerPosition.Distance(lordPosition);
                if (distance > 0.5f || mainParty.IsCurrentlyAtSea != lordParty.IsCurrentlyAtSea)
                {
                    // Sync sea state first (must match for position to be valid)
                    if (mainParty.IsCurrentlyAtSea != lordParty.IsCurrentlyAtSea)
                    {
                        mainParty.IsCurrentlyAtSea = lordParty.IsCurrentlyAtSea;
                        ModLogger.Debug("Naval", $"Synced player sea state to {lordParty.IsCurrentlyAtSea}");
                    }

                    // Teleport player to lord's position
                    mainParty.Position = lordPosition;
                    ModLogger.Debug("Naval", $"Synced player position to lord at sea (distance was {distance:F2})");
                }

                // Set hold mode to prevent AI from trying to pathfind (which would fail on water)
                mainParty.SetMoveModeHold();

                // Camera should follow the lord for visual continuity
                lordParty.Party.SetAsCameraFollowParty();

                return true;
            }
            catch (Exception ex)
            {
                ModLogger.Error("Naval", $"Failed to sync naval position: {ex.Message}");
                return false;
            }
        }


        /// <summary>
        ///     Teleports the player to the nearest settlement with a port when stranded at sea.
        ///     Called when service ends and player has no naval navigation capability.
        ///     This prevents players from being permanently stranded on the water.
        /// </summary>
        /// <param name="mainParty">The player's party to teleport.</param>
        private void TryTeleportToNearestPort(MobileParty mainParty)
        {
            try
            {
                if (mainParty == null)
                {
                    ModLogger.Warn("Naval", "TryTeleportToNearestPort called with null party");
                    return;
                }

                var playerPosition = mainParty.Position;
                Settlement nearestPort = null;
                var nearestDistance = float.MaxValue;

                ModLogger.Debug("Naval", "Searching for nearest port from player position");

                // Find the nearest settlement with a port
                foreach (var settlement in Settlement.All)
                {
                    if (settlement == null || settlement.IsHideout)
                    {
                        continue;
                    }

                    // Check if settlement has a port (towns with ports, coastal castles)
                    if (!settlement.HasPort)
                    {
                        continue;
                    }

                    var portPosition = settlement.PortPosition;
                    var distance = playerPosition.Distance(portPosition);

                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearestPort = settlement;
                    }
                }

                if (nearestPort != null)
                {
                    // Clear sea state and teleport to the settlement's gate position (on land)
                    mainParty.IsCurrentlyAtSea = false;
                    mainParty.Position = nearestPort.GatePosition;

                    // Set hold mode so player can decide where to go
                    mainParty.SetMoveModeHold();

                    var message =
                        new TextObject("{=Enlisted_Message_WashedAshore}You have washed ashore near {SETTLEMENT}.");
                    message.SetTextVariable("SETTLEMENT", nearestPort.Name);
                    InformationManager.DisplayMessage(new InformationMessage(message.ToString()));

                    ModLogger.Info("Naval",
                        $"Teleported stranded player to {nearestPort.Name} (distance was {nearestDistance:F2})");
                }
                else
                {
                    // Fallback: just clear sea state and hope for the best
                    // This shouldn't happen as there should always be ports in the game
                    mainParty.IsCurrentlyAtSea = false;
                    mainParty.SetMoveModeHold();

                    ModLogger.Warn("Naval", "No port found - cleared sea state but player may still be in water");

                    var message = new TextObject("{=Enlisted_Message_MadeItToShore}You have made it to shore.");
                    InformationManager.DisplayMessage(new InformationMessage(message.ToString()));
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("Naval", $"Failed to teleport stranded player: {ex.Message}");

                // Emergency fallback - at least try to clear sea state
                try
                {
                    if (mainParty != null)
                    {
                        mainParty.IsCurrentlyAtSea = false;
                    }
                }
                catch
                {
                    // Ignore - we tried our best
                }
            }
        }

        #region Veteran Retirement System

        /// <summary>
        ///     Per-faction veteran service records. Tracks service history, cooldowns, and preserved tier.
        ///     Dictionary keyed by Kingdom string ID for save/load compatibility.
        /// </summary>
        private Dictionary<string, FactionVeteranRecord> _veteranRecords = new();

        /// <summary>
        ///     Whether the player has been notified about retirement eligibility for this term.
        ///     Resets when starting new term.
        /// </summary>
        private bool _retirementNotificationShown;

        /// <summary>
        ///     Total kills accumulated during current service term.
        ///     Added to faction's TotalKills on retirement/re-enlistment.
        /// </summary>
        private int _currentTermKills;

        #endregion

        #region Public Events (for mod integration)

        /// <summary>
        ///     Fired when player enlists with a lord. Passes the lord Hero.
        /// </summary>
        public static event Action<Hero> OnEnlisted;

        /// <summary>
        ///     Fired when player is discharged. Passes the reason string.
        /// </summary>
        public static event Action<string> OnDischarged;

        /// <summary>
        ///     Fired when player is promoted. Passes the new tier number.
        /// </summary>
        public static event Action<int> OnPromoted;

        /// <summary>
        ///     Fired when player starts temporary leave.
        /// </summary>
        public static event Action OnLeaveStarted;

        /// <summary>
        ///     Fired when leave ends (either by returning or desertion).
        /// </summary>
        public static event Action OnLeaveEnded;

        /// <summary>
        ///     Fired when grace period begins (lord death, capture, army defeat).
        /// </summary>
        public static event Action OnGracePeriodStarted;

        /// <summary>
        ///     Fired when XP is gained. Passes amount and source description.
        /// </summary>
        public static event Action<int, string> OnXPGained;

        /// <summary>
        ///     Flag indicating the player chose to keep their retinue troops on retirement.
        ///     Set by dialog, checked by RetinueLifecycleHandler and ServiceRecordManager,
        ///     then reset after processing. Troops become regular party members.
        /// </summary>
        public static bool RetainTroopsOnRetirement { get; set; }

        /// <summary>
        ///     Fired when daily wage is paid. Passes the wage amount.
        /// </summary>
        public static event Action<int> OnWagePaid;

        #endregion

        #region Veteran System Properties

        /// <summary>
        ///     Date when the current enlistment started.
        /// </summary>
        public CampaignTime EnlistmentDate => _enlistmentDate;

        /// <summary>
        ///     Days served in current enlistment term.
        /// </summary>
        public float DaysServed => _enlistmentDate != CampaignTime.Zero
            ? (float)(CampaignTime.Now - _enlistmentDate).ToDays
            : 0f;

        /// <summary>
        ///     Whether the player has served the minimum 252 days (3 years) required for first-term retirement.
        /// </summary>
        public bool IsEligibleForRetirement => IsEnlisted && DaysServed >= 252f && !IsInRenewalTerm;

        /// <summary>
        ///     Whether the player is currently in a renewal term (post-first-term).
        /// </summary>
        public bool IsInRenewalTerm
        {
            get
            {
                if (_enlistedLord == null)
                {
                    return false;
                }

                var record = GetFactionVeteranRecord(_enlistedLord.MapFaction);
                return record?.IsInRenewalTerm ?? false;
            }
        }

        /// <summary>
        ///     Whether the current renewal term has expired and player can retire/continue.
        /// </summary>
        public bool IsRenewalTermComplete
        {
            get
            {
                if (_enlistedLord == null || !IsInRenewalTerm)
                {
                    return false;
                }

                var record = GetFactionVeteranRecord(_enlistedLord.MapFaction);
                return record?.CurrentTermEnd != CampaignTime.Zero && CampaignTime.Now >= record.CurrentTermEnd;
            }
        }

        /// <summary>
        ///     Gets the veteran record for a faction. Creates new record if none exists.
        ///     Supports both Kingdoms and Clans (minor factions).
        /// </summary>
        public FactionVeteranRecord GetFactionVeteranRecord(IFaction faction)
        {
            if (faction == null)
            {
                return null;
            }

            var factionId = faction.StringId;
            if (!_veteranRecords.ContainsKey(factionId))
            {
                _veteranRecords[factionId] = new FactionVeteranRecord();
            }

            return _veteranRecords[factionId];
        }

        /// <summary>
        ///     Checks if the player is in cooldown period for a faction.
        /// </summary>
        public bool IsInFactionCooldown(IFaction faction)
        {
            if (faction == null)
            {
                return false;
            }

            var record = GetFactionVeteranRecord(faction);
            return record.CooldownEnds != CampaignTime.Zero && CampaignTime.Now < record.CooldownEnds;
        }

        /// <summary>
        ///     Checks if the player can re-enlist with a faction after cooldown.
        /// </summary>
        public bool CanReEnlistAfterCooldown(IFaction faction)
        {
            if (faction == null)
            {
                return false;
            }

            var record = GetFactionVeteranRecord(faction);
            // Must have completed first term and be past cooldown
            return record.FirstTermCompleted &&
                   record.CooldownEnds != CampaignTime.Zero &&
                   CampaignTime.Now >= record.CooldownEnds;
        }

        #endregion

        #region Veteran Retirement System Methods

        /// <summary>
        ///     Checks if the player has reached first-term retirement eligibility (252 days).
        ///     Shows a one-time notification when first eligible.
        /// </summary>
        private void CheckRetirementEligibility()
        {
            if (_retirementNotificationShown || !IsEnlisted || IsInRenewalTerm)
            {
                return;
            }

            var config = EnlistedConfig.LoadRetirementConfig();
            if (DaysServed >= config.FirstTermDays)
            {
                _retirementNotificationShown = true;

                var message =
                    new TextObject(
                        "{=Enlisted_Message_TermCompleted}You have completed your term of service! Speak with {LORD} to discuss retirement or re-enlistment.");
                message.SetTextVariable("LORD", _enlistedLord.Name);
                InformationManager.DisplayMessage(new InformationMessage(message.ToString(), Colors.Green));

                ModLogger.Info("Retirement", $"Player eligible for retirement after {DaysServed:F1} days");
            }
        }

        /// <summary>
        ///     Checks if the current renewal term has completed.
        ///     Shows notification when term expires.
        /// </summary>
        private void CheckRenewalTermCompletion()
        {
            if (!IsEnlisted || !IsInRenewalTerm)
            {
                return;
            }

            var faction = _enlistedLord.MapFaction;
            var record = GetFactionVeteranRecord(faction);

            if (record?.CurrentTermEnd != CampaignTime.Zero && CampaignTime.Now >= record.CurrentTermEnd)
            {
                var message =
                    new TextObject(
                        "{=Enlisted_Message_TermEndedDischarge}Your service term has ended. Speak with {LORD} to receive your discharge bonus or continue service.");
                message.SetTextVariable("LORD", _enlistedLord.Name);
                InformationManager.DisplayMessage(new InformationMessage(message.ToString(), Colors.Green));

                ModLogger.Info("Retirement", "Renewal term complete - player should speak with lord");
            }
        }

        /// <summary>
        ///     Process first-term retirement: apply full benefits and start cooldown.
        ///     Called when player chooses to retire after first term.
        /// </summary>
        public void ProcessFirstTermRetirement()
        {
            if (!IsEnlisted || !IsEligibleForRetirement)
            {
                ModLogger.Error("Retirement", "Cannot process first-term retirement - not eligible");
                return;
            }

            var config = EnlistedConfig.LoadRetirementConfig();
            var faction = _enlistedLord.MapFaction;
            var record = GetFactionVeteranRecord(faction);

            // Mark first term as completed
            record.FirstTermCompleted = true;
            record.PreservedTier = _enlistmentTier;
            record.TotalKills += _currentTermKills;

            // Start 6-month cooldown (42 days)
            record.CooldownEnds = CampaignTime.Now + CampaignTime.Days(config.CooldownDays);

            // Award retirement gold
            GiveGoldAction.ApplyForCharacterToParty(null, MobileParty.MainParty.Party, config.FirstTermGold, true);
            ModLogger.Info("Gold", $"Retirement bonus: {config.FirstTermGold} denars (first term completion)");

            // Apply relation bonuses
            ApplyVeteranRelationBonuses(config);

            // Award 50 renown for first term retirement
            GainRenownAction.Apply(Hero.MainHero, 50f);
            ModLogger.Info("Renown", "First term retirement: +50 renown");

            // End enlistment
            StopEnlist("Honorable retirement - first term", true);

            var message =
                new TextObject(
                    "{=Enlisted_Message_RetiredHonor}You have retired with honor. {GOLD} gold received. You may re-enlist with {KINGDOM} after the cooldown period.");
            message.SetTextVariable("GOLD", config.FirstTermGold);
            message.SetTextVariable("KINGDOM",
                faction?.Name ?? new TextObject("{=Enlisted_Term_ThisFaction}this faction"));
            InformationManager.DisplayMessage(new InformationMessage(message.ToString(), Colors.Green));

            ModLogger.Info("Enlistment",
                $"First term retirement processed: {config.FirstTermGold}g, cooldown ends {record.CooldownEnds}");
        }

        /// <summary>
        ///     Process renewal term retirement: award discharge gold and start cooldown.
        ///     Called when player chooses to retire after a renewal term.
        /// </summary>
        public void ProcessRenewalRetirement()
        {
            if (!IsEnlisted || !IsInRenewalTerm)
            {
                ModLogger.Error("Retirement", "Cannot process renewal retirement - not in renewal term");
                return;
            }

            var config = EnlistedConfig.LoadRetirementConfig();
            var faction = _enlistedLord.MapFaction;
            var record = GetFactionVeteranRecord(faction);

            // Update record
            record.PreservedTier = _enlistmentTier;
            record.TotalKills += _currentTermKills;
            record.IsInRenewalTerm = false;
            record.RenewalTermsCompleted++;

            // Start cooldown
            record.CooldownEnds = CampaignTime.Now + CampaignTime.Days(config.CooldownDays);

            // Award discharge gold
            GiveGoldAction.ApplyForCharacterToParty(null, MobileParty.MainParty.Party, config.RenewalDischargeGold,
                true);
            ModLogger.Info("Gold", $"Discharge bonus: {config.RenewalDischargeGold} denars (renewal term completion)");

            // End enlistment
            StopEnlist("Honorable discharge - renewal term", true);

            var message =
                new TextObject(
                    "{=Enlisted_Message_Discharged}You have been discharged. {GOLD} gold received. You may re-enlist with {KINGDOM} after {DAYS} days.");
            message.SetTextVariable("GOLD", config.RenewalDischargeGold);
            message.SetTextVariable("KINGDOM",
                faction?.Name ?? new TextObject("{=Enlisted_Term_ThisFaction}this faction"));
            message.SetTextVariable("DAYS", config.CooldownDays);
            InformationManager.DisplayMessage(new InformationMessage(message.ToString()));

            ModLogger.Info("Enlistment",
                $"Renewal term discharge: {config.RenewalDischargeGold}g, cooldown {config.CooldownDays} days");
        }

        /// <summary>
        ///     Start a renewal term with bonus payment.
        ///     Called when player chooses to continue service.
        ///     First re-enlistment grants full veteran bonuses (30/30/15 relation, 50 renown).
        ///     Subsequent re-enlistments grant smaller bonuses (10/10/10 relation, 10 renown).
        /// </summary>
        public void StartRenewalTerm(int bonus)
        {
            if (!IsEnlisted)
            {
                ModLogger.Error("Retirement", "Cannot start renewal term - not enlisted");
                return;
            }

            var config = EnlistedConfig.LoadRetirementConfig();
            var faction = _enlistedLord.MapFaction;
            var record = GetFactionVeteranRecord(faction);

            // Check if this is the first re-enlistment (no renewal terms completed yet)
            var isFirstReenlistment = record.RenewalTermsCompleted == 0;

            // Update record for renewal
            if (!record.FirstTermCompleted)
            {
                record.FirstTermCompleted = true;
            }

            record.IsInRenewalTerm = true;
            record.CurrentTermEnd = CampaignTime.Now + CampaignTime.Days(config.RenewalTermDays);

            // Pay the bonus
            if (bonus > 0)
            {
                GiveGoldAction.ApplyForCharacterToParty(null, MobileParty.MainParty.Party, bonus, true);
            }

            // Apply relation and renown bonuses based on whether this is first or subsequent re-enlistment
            if (isFirstReenlistment)
            {
                // First re-enlistment: same bonuses as first retirement (30/30/15 relation, 50 renown)
                ApplyVeteranRelationBonuses(config);
                GainRenownAction.Apply(Hero.MainHero, 50f);
                ModLogger.Info("Renown", "First re-enlistment: +50 renown, full relation bonuses applied");
            }
            else
            {
                // Subsequent re-enlistments: smaller bonuses (10/10/10 relation, 10 renown)
                ApplySubsequentReenlistmentBonuses();
                GainRenownAction.Apply(Hero.MainHero, 10f);
                ModLogger.Info("Renown", "Subsequent re-enlistment: +10 renown, reduced relation bonuses applied");
            }

            // Reset notification flag for this term
            _retirementNotificationShown = true; // Don't show first-term notification again

            var message =
                new TextObject(
                    "{=Enlisted_Message_ReEnlistedBonus}You have re-enlisted for another term. {GOLD} gold bonus received. Term ends in {DAYS} days.");
            message.SetTextVariable("GOLD", bonus);
            message.SetTextVariable("DAYS", config.RenewalTermDays);
            InformationManager.DisplayMessage(new InformationMessage(message.ToString()));

            ModLogger.Info("Retirement",
                $"Renewal term started: {bonus}g bonus, ends in {config.RenewalTermDays} days, first={isFirstReenlistment}");
        }

        /// <summary>
        ///     Re-enlist with a faction after cooldown period.
        ///     Restores preserved tier and starts new 1-year term.
        /// </summary>
        public void ReEnlistAfterCooldown(Hero lord)
        {
            var faction = lord.MapFaction;
            if (faction == null || !CanReEnlistAfterCooldown(faction))
            {
                ModLogger.Error("Retirement", "Cannot re-enlist - not eligible or wrong faction");
                return;
            }

            var config = EnlistedConfig.LoadRetirementConfig();
            var record = GetFactionVeteranRecord(faction);

            // Restore tier from record
            var preservedTier = record.PreservedTier;

            // Clear cooldown
            record.CooldownEnds = CampaignTime.Zero;

            // Start enlistment normally
            StartEnlist(lord);

            // Restore tier
            _enlistmentTier = preservedTier;
            _enlistmentXP = GetMinXPForTier(preservedTier);

            // Start in renewal mode
            record.IsInRenewalTerm = true;
            record.CurrentTermEnd = CampaignTime.Now + CampaignTime.Days(config.RenewalTermDays);

            _retirementNotificationShown = true;
            _currentTermKills = 0;

            var message =
                new TextObject(
                    "{=Enlisted_Message_ReEnlistedRank}You have re-enlisted with {KINGDOM}. Your rank of Tier {TIER} has been restored. Term: {DAYS} days.");
            message.SetTextVariable("KINGDOM", faction.Name);
            message.SetTextVariable("TIER", preservedTier);
            message.SetTextVariable("DAYS", config.RenewalTermDays);
            InformationManager.DisplayMessage(new InformationMessage(message.ToString()));

            ModLogger.Info("Retirement",
                $"Re-enlisted after cooldown: tier {preservedTier} restored, {config.RenewalTermDays} day term");
        }

        /// <summary>
        ///     Get the minimum XP required for a given tier.
        /// </summary>
        private int GetMinXPForTier(int tier)
        {
            var requirements = EnlistedConfig.GetTierXpRequirements();
            if (tier > 0 && tier <= requirements.Length)
            {
                return requirements[tier - 1];
            }

            return 0;
        }

        /// <summary>
        ///     Apply veteran relation bonuses for first-term retirement.
        ///     +30 with current lord, +30 with faction, +15 with other lords (if rep > 50).
        /// </summary>
        private void ApplyVeteranRelationBonuses(RetirementConfig config)
        {
            try
            {
                var faction = _enlistedLord?.MapFaction;
                if (faction == null)
                {
                    ModLogger.Error("Retirement", "Cannot apply relation bonuses - no faction");
                    return;
                }

                // +30 with current lord
                if (_enlistedLord != null)
                {
                    ChangeRelationAction.ApplyPlayerRelation(_enlistedLord, config.LordRelationBonus, true, true);
                    ModLogger.Info("Retirement", $"+{config.LordRelationBonus} relation with {_enlistedLord.Name}");
                }

                // +30 with faction (clan reputation affects kingdom standing)
                // Use leader as proxy for faction reputation
                if (faction.Leader != null && faction.Leader != _enlistedLord)
                {
                    ChangeRelationAction.ApplyPlayerRelation(faction.Leader, config.FactionReputationBonus, true, true);
                    ModLogger.Info("Retirement",
                        $"+{config.FactionReputationBonus} relation with {faction.Name} (via leader)");
                }

                // +15 with other lords in faction IF player has > 50 relation with them
                if (faction is Kingdom kingdom)
                {
                    foreach (var clan in kingdom.Clans)
                    {
                        if (clan == Clan.PlayerClan)
                        {
                            continue;
                        }

                        foreach (var hero in clan.Heroes)
                        {
                            // Only apply to lords (not companions)
                            if (!hero.IsLord || hero == _enlistedLord || hero == kingdom.Leader || !hero.IsAlive)
                            {
                                continue;
                            }

                            var currentRelation = Hero.MainHero.GetRelation(hero);
                            if (currentRelation > config.OtherLordsMinRelation)
                            {
                                ChangeRelationAction.ApplyPlayerRelation(hero, config.OtherLordsRelationBonus, true,
                                    false);
                                ModLogger.Debug("Retirement",
                                    $"+{config.OtherLordsRelationBonus} relation with {hero.Name} (had {currentRelation})");
                            }
                        }
                    }
                }
                else if (faction is Clan clan)
                {
                    // Minor faction handling
                    foreach (var hero in clan.Heroes)
                    {
                        if (!hero.IsLord || hero == _enlistedLord || hero == clan.Leader || !hero.IsAlive)
                        {
                            continue;
                        }

                        var currentRelation = Hero.MainHero.GetRelation(hero);
                        if (currentRelation > config.OtherLordsMinRelation)
                        {
                            ChangeRelationAction.ApplyPlayerRelation(hero, config.OtherLordsRelationBonus, true, false);
                            ModLogger.Debug("Retirement",
                                $"+{config.OtherLordsRelationBonus} relation with {hero.Name} (had {currentRelation})");
                        }
                    }
                }

                ModLogger.Info("Retirement", "Veteran relation bonuses applied");
            }
            catch (Exception ex)
            {
                ModLogger.Error("Retirement", $"Error applying relation bonuses: {ex.Message}");
            }
        }

        /// <summary>
        ///     Apply smaller relation bonuses for subsequent re-enlistments.
        ///     +10 with current lord, +10 with faction leader, +10 with other lords.
        /// </summary>
        private void ApplySubsequentReenlistmentBonuses()
        {
            const int subsequentBonus = 10;

            try
            {
                var faction = _enlistedLord?.MapFaction;
                if (faction == null)
                {
                    ModLogger.Error("Retirement", "Cannot apply subsequent bonuses - no faction");
                    return;
                }

                // +10 with current lord
                if (_enlistedLord != null)
                {
                    ChangeRelationAction.ApplyPlayerRelation(_enlistedLord, subsequentBonus, true, true);
                    ModLogger.Info("Retirement", $"+{subsequentBonus} relation with {_enlistedLord.Name} (subsequent re-enlistment)");
                }

                // +10 with faction leader
                if (faction.Leader != null && faction.Leader != _enlistedLord)
                {
                    ChangeRelationAction.ApplyPlayerRelation(faction.Leader, subsequentBonus, true, true);
                    ModLogger.Info("Retirement", $"+{subsequentBonus} relation with {faction.Name} leader (subsequent re-enlistment)");
                }

                // +10 with other lords in faction (no minimum relation requirement for subsequent)
                if (faction is Kingdom kingdom)
                {
                    foreach (var clan in kingdom.Clans)
                    {
                        if (clan == Clan.PlayerClan)
                        {
                            continue;
                        }

                        foreach (var hero in clan.Heroes)
                        {
                            if (!hero.IsLord || hero == _enlistedLord || hero == kingdom.Leader || !hero.IsAlive)
                            {
                                continue;
                            }

                            ChangeRelationAction.ApplyPlayerRelation(hero, subsequentBonus, true, false);
                        }
                    }
                }
                else if (faction is Clan clan)
                {
                    foreach (var hero in clan.Heroes)
                    {
                        if (!hero.IsLord || hero == _enlistedLord || hero == clan.Leader || !hero.IsAlive)
                        {
                            continue;
                        }

                        ChangeRelationAction.ApplyPlayerRelation(hero, subsequentBonus, true, false);
                    }
                }

                ModLogger.Info("Retirement", $"Subsequent re-enlistment bonuses applied (+{subsequentBonus} relation)");
            }
            catch (Exception ex)
            {
                ModLogger.Error("Retirement", $"Error applying subsequent bonuses: {ex.Message}");
            }
        }

        #endregion

        #region Event Handlers for Lord Status Changes

        /// <summary>
        ///     Handles the case when the enlisted lord is killed in battle or other circumstances.
        ///     Starts a 14-day grace period to re-enlist with another lord in the same faction.
        ///     Called by the game when any hero is killed.
        /// </summary>
        /// <param name="victim">The hero that was killed.</param>
        /// <param name="killer">The hero that killed the victim (may be null).</param>
        /// <param name="detail">Details about how the hero was killed.</param>
        /// <param name="showNotification">Whether to show a notification to the player.</param>
        private void OnHeroKilled(Hero victim, Hero killer, KillCharacterAction.KillCharacterActionDetail detail,
            bool showNotification)
        {
            if (IsEnlisted && victim == _enlistedLord)
            {
                ModLogger.Info("EventSafety", $"Lord {victim.Name} killed - starting grace period");

                // Start grace period instead of immediate discharge
                // Player has 14 days to re-enlist with another lord in the same faction
                var lordKingdom = _enlistedLord.MapFaction as Kingdom;
                if (lordKingdom != null)
                {
                    var message =
                        new TextObject(
                            "{=Enlisted_Message_LordFallenGrace}Your lord has fallen. You have 14 days to find a new commander in {KINGDOM} before desertion.");
                    message.SetTextVariable("KINGDOM", lordKingdom.Name);
                    InformationManager.DisplayMessage(new InformationMessage(message.ToString()));

                    StopEnlist("Lord killed in battle", false, true);
                    StartDesertionGracePeriod(lordKingdom);
                }
                else
                {
                    // No kingdom - immediate discharge without penalties
                    var message =
                        new TextObject(
                            "{=Enlisted_Message_LordFallenEnded}Your lord has fallen. Your service has ended.");
                    InformationManager.DisplayMessage(new InformationMessage(message.ToString()));
                    StopEnlist("Lord killed - no kingdom", true);
                }
            }
        }

        /// <summary>
        ///     Handles the case when the enlisted lord is defeated in battle.
        ///     Checks if the lord was killed or captured and discharges the player accordingly.
        ///     Called by the game when a character is defeated in battle.
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
                    var message =
                        new TextObject(
                            "{=Enlisted_Message_LordFallenDischarged}Your lord has fallen in battle. You have been honorably discharged.");
                    InformationManager.DisplayMessage(new InformationMessage(message.ToString()));
                    StopEnlist("Lord died in battle", true);
                }
                else if (_enlistedLord.IsPrisoner)
                {
                    // Start grace period instead of immediate discharge
                    // Player has 14 days to rejoin another lord in the same kingdom
                    var lordKingdom = _enlistedLord.MapFaction as Kingdom;
                    if (lordKingdom != null)
                    {
                        // End enlistment but start grace period
                        StopEnlist("Lord captured in battle", false, true);
                        StartDesertionGracePeriod(lordKingdom);
                    }
                    else
                    {
                        // No kingdom - immediate discharge
                        var message = new TextObject("{=enlist_lord_captured_ended}Your lord has been captured. Your service has ended.");
                        InformationManager.DisplayMessage(new InformationMessage(message.ToString()));
                        StopEnlist("Lord captured in battle");
                    }
                }
            }
        }

        /// <summary>
        ///     Handle army dispersal/defeat scenarios.
        ///     Called when lord's army is disbanded or defeated.
        /// </summary>
        private void OnArmyDispersed(Army army, Army.ArmyDispersionReason reason, bool isLeaderPartyRemoved)
        {
            if (IsEnlisted && army?.LeaderParty?.LeaderHero == _enlistedLord)
            {
                ModLogger.Info("EventSafety", $"Army dispersed - reason: {reason}");

                // Check if lord still exists after army defeat
                if (_enlistedLord == null || !_enlistedLord.IsAlive || _enlistedLord.PartyBelongedTo == null)
                {
                    // Start grace period instead of immediate discharge
                    // Player has 14 days to rejoin another lord in the same kingdom
                    // Capture kingdom before StopEnlist clears _enlistedLord
                    var lordKingdom = _enlistedLord?.MapFaction as Kingdom ??
                                      army?.LeaderParty?.MapFaction as Kingdom;

                    if (lordKingdom != null)
                    {
                        // End enlistment but start grace period
                        StopEnlist("Army defeated - lord lost", false, true);
                        StartDesertionGracePeriod(lordKingdom);
                    }
                    else
                    {
                        // No kingdom - immediate discharge
                        var message = new TextObject("{=enlist_army_defeated_ended}Your lord's army has been defeated. Your service has ended.");
                        InformationManager.DisplayMessage(new InformationMessage(message.ToString()));
                        StopEnlist("Army defeated - lord lost");
                    }
                }
                else
                {
                    var message = new TextObject("{=enlist_army_dispersed}Your lord's army has been dispersed. Continuing service.");
                    InformationManager.DisplayMessage(new InformationMessage(message.ToString()));

                    // NAVAL FIX: Re-establish following when army disbands at sea
                    // Without this fix, players get stranded because DisperseInternal skips
                    // repositioning for parties without naval navigation capability
                    var mainParty = MobileParty.MainParty;
                    var lordParty = _enlistedLord.PartyBelongedTo;

                    if (mainParty != null && lordParty != null)
                    {
                        // Check if lord is at sea - need to re-sync position
                        if (lordParty.IsCurrentlyAtSea)
                        {
                            ModLogger.Info("Naval",
                                $"Army disbanded at sea - re-syncing player position with lord {_enlistedLord.Name}");

                            // Sync sea state and position
                            bool wasAtSea = mainParty.IsCurrentlyAtSea;
                            mainParty.IsCurrentlyAtSea = lordParty.IsCurrentlyAtSea;
                            mainParty.Position = lordParty.Position;

                            // Set hold mode to prevent AI from trying to pathfind on water
                            mainParty.SetMoveModeHold();

                            // Camera should follow the lord
                            lordParty.Party.SetAsCameraFollowParty();

                            var navalMessage = new TextObject("{=enlist_naval_remain}You remain aboard with {LORD}'s party.");
                            navalMessage.SetTextVariable("LORD", _enlistedLord.Name);
                            InformationManager.DisplayMessage(new InformationMessage(navalMessage.ToString()));

                            ModLogger.Debug("Naval",
                                $"Naval sync complete: wasAtSea={wasAtSea}, nowAtSea={mainParty.IsCurrentlyAtSea}, position synced to lord");
                        }
                        else
                        {
                            // Lord is on land - use normal escort AI
                            ModLogger.Debug("Naval", "Army disbanded on land - setting up normal escort AI");
                            mainParty.SetMoveEscortParty(lordParty, MobileParty.NavigationType.Default, false);
                            lordParty.Party.SetAsCameraFollowParty();
                        }
                    }
                    else
                    {
                        ModLogger.Warn("Naval",
                            $"Cannot re-sync after army disband: mainParty={mainParty != null}, lordParty={lordParty != null}");
                    }

                    // Menu restoration is now handled by ArmyDispersedMenuPatch which intercepts
                    // the native army_dispersed menu's "Continue" button to show enlisted_status
                }
            }
        }

        /// <summary>
        ///     Handle lord or player capture scenarios.
        ///     Called when lord or player is taken prisoner.
        /// </summary>
        private void OnHeroPrisonerTaken(PartyBase capturingParty, Hero prisoner)
        {
            if (!IsEnlisted)
            {
                return;
            }

            // Case 1: Player captured
            if (prisoner == Hero.MainHero)
            {
                ModLogger.Info("EventSafety", "Player captured - deferring enlistment teardown until encounter closes");

                var lordKingdom = _enlistedLord?.MapFaction as Kingdom;
                if (lordKingdom != null)
                {
                    var message =
                        new TextObject(
                            "You have been taken prisoner. You have 14 days after escape to rejoin your kingdom.");
                    InformationManager.DisplayMessage(new InformationMessage(message.ToString()));
                }
                else
                {
                    var message = new TextObject("{=enlist_player_captured}You have been taken prisoner. Your service has ended.");
                    InformationManager.DisplayMessage(new InformationMessage(message.ToString()));
                }

                SchedulePlayerCaptureCleanup(lordKingdom);
                return;
            }

            // Case 2: Lord captured
            if (prisoner == _enlistedLord)
            {
                ModLogger.Info("EventSafety", $"Lord {prisoner.Name} captured - starting grace period");

                var playerCapturedWithLord = TryCapturePlayerAlongsideLord(capturingParty);
                if (playerCapturedWithLord)
                {
                    ModLogger.Info("Battle", "Player captured alongside lord due to mirrored capture logic");
                }

                // Start grace period instead of immediate discharge
                // Player has 14 days to rejoin another lord in the same kingdom
                var lordKingdom = _enlistedLord.MapFaction as Kingdom;
                if (lordKingdom != null)
                {
                    if (playerCapturedWithLord || _playerCaptureCleanupScheduled)
                    {
                        ModLogger.Info("EventSafety",
                            "Deferring lord capture discharge because player capture cleanup is pending");
                        SchedulePlayerCaptureCleanup(lordKingdom);
                    }
                    else
                    {
                        StopEnlist("Lord captured", false, true);
                        StartDesertionGracePeriod(lordKingdom);
                    }
                }
                else
                {
                    // No kingdom - immediate discharge
                    var message = new TextObject("{=enlist_lord_captured_ended}Your lord has been captured. Your service has ended.");
                    InformationManager.DisplayMessage(new InformationMessage(message.ToString()));
                    if (playerCapturedWithLord || _playerCaptureCleanupScheduled)
                    {
                        SchedulePlayerCaptureCleanup(null);
                    }
                    else
                    {
                        StopEnlist("Lord captured");
                    }
                }
            }
        }

        /// <summary>
        ///     Handle clan changing kingdoms - both player clan and lord's clan.
        ///     Edge cases:
        ///     1. If player changes kingdoms during grace period, clear grace period.
        ///     2. If player becomes vassal/mercenary while actively enlisted, end enlistment honorably.
        ///     3. If lord's minor faction clan joins a Kingdom, auto-join player as mercenary.
        ///     Player can no longer rejoin the original kingdom, so grace period is invalid.
        /// </summary>
        private void OnClanChangedKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom,
            ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification)
        {
            // EDGE CASE: Lord's clan (minor faction) joins a Kingdom as mercenary
            // When enlisted with a minor faction lord, the player isn't joined to any faction.
            // If the lord's clan joins a Kingdom, we should auto-join the player too so
            // battle participation works correctly (CanPartyJoinBattle requires faction alignment).
            if (IsEnlisted && !_isOnLeave && clan == _enlistedLord?.Clan && clan != Clan.PlayerClan)
            {
                if (newKingdom != null && oldKingdom == null)
                {
                    // Lord's clan joined a Kingdom - auto-join player as mercenary
                    var playerClan = Clan.PlayerClan;
                    if (playerClan != null && playerClan.Kingdom != newKingdom)
                    {
                        try
                        {
                            ChangeKingdomAction.ApplyByJoinFactionAsMercenary(playerClan, newKingdom,
                                default(CampaignTime), 0, false);
                            ModLogger.Info("Enlistment",
                                $"Lord's clan joined {newKingdom.Name} - auto-joined player as mercenary to maintain battle eligibility");
                        }
                        catch (Exception ex)
                        {
                            ModLogger.Error("Enlistment",
                                $"Failed to auto-join player as mercenary when lord's clan joined {newKingdom.Name}: {ex.Message}");
                        }
                    }
                }
                return; // Don't process further - this was the lord's clan, not player's
            }
            
            if (clan != Clan.PlayerClan)
            {
                return; // Not the player's clan and not lord's clan
            }

            // Handle player leaving the kingdom entirely (quitting mercenary service) while enlisted or on leave
            // This is desertion - they abandoned their service commitment
            if ((IsEnlisted || _isOnLeave) && newKingdom == null && oldKingdom != null)
            {
                var lordKingdom = _enlistedLord?.MapFaction as Kingdom;
                
                // Only process if they were in the lord's kingdom (not some random kingdom change)
                if (lordKingdom != null && oldKingdom == lordKingdom)
                {
                    ModLogger.Info("Desertion",
                        $"Player quit mercenary service with {oldKingdom.Name} while {(IsEnlisted ? "enlisted" : "on leave")} - treating as desertion");

                    // Clear leave state if on leave
                    if (_isOnLeave)
                    {
                        _isOnLeave = false;
                        _leaveStartDate = CampaignTime.Zero;
                    }

                    // Set pending desertion kingdom to the original lord's kingdom
                    _pendingDesertionKingdom = lordKingdom;
                    
                    // Apply desertion penalties
                    ApplyDesertionPenalties();

                    // Fire event for other mods (before StopEnlist clears state)
                    OnLeaveEnded?.Invoke();

                    // End enlistment as desertion
                    StopEnlist($"Player quit mercenary service with {oldKingdom.Name} - desertion", isHonorableDischarge: false);

                    var desertionMessage = new TextObject(
                        "{=Enlisted_Message_QuitMercenaryDesertion}You have abandoned your mercenary service with {KINGDOM}. You have been branded a deserter.");
                    desertionMessage.SetTextVariable("KINGDOM", oldKingdom.Name);
                    InformationManager.DisplayMessage(new InformationMessage(desertionMessage.ToString()));
                    
                    return; // Early return - don't process grace period logic
                }
            }

            // BUG FIX: If player is enlisted (active or on leave) and kingdom status changes, handle appropriately
            // When actively enlisted, player is already a mercenary in the lord's kingdom.
            // This handles:
            // 1. Active enlistment: mercenary -> vassal promotion (same kingdom, oldKingdom == newKingdom)
            // 2. Active enlistment: joins different kingdom (oldKingdom != newKingdom)
            // 3. On leave: joins any kingdom as mercenary/vassal (may have left kingdom during leave)
            // NOTE: We must NOT end enlistment when they first join as mercenary during StartEnlist()
            if ((IsEnlisted || _isOnLeave) && newKingdom != null)
            {
                var playerClan = Clan.PlayerClan;
                if (playerClan != null && playerClan.Kingdom == newKingdom)
                {
                    var lordKingdom = _enlistedLord?.MapFaction as Kingdom;
                    
                    // Determine if this is a meaningful status change that should end enlistment:
                    // 1. Promotion to vassal: same kingdom, became vassal (was mercenary, now vassal) = honorable
                    // 2. Kingdom transfer: joined different kingdom than lord's kingdom = desertion
                    // 3. On leave + same kingdom: honorable discharge
                    // 4. On leave + different kingdom: desertion
                    // 
                    // EXCLUDE: Initial enlistment (joining lord's kingdom as mercenary during StartEnlist)
                    // This happens when: oldKingdom != lordKingdom && newKingdom == lordKingdom && is mercenary
                    var isPromotionToVassal = oldKingdom == newKingdom && !playerClan.IsUnderMercenaryService;
                    var isKingdomTransfer = lordKingdom != null && lordKingdom != newKingdom;
                    var wasOnLeave = _isOnLeave;
                    var isInitialEnlistment = lordKingdom != null && lordKingdom == newKingdom && 
                                             oldKingdom != lordKingdom && playerClan.IsUnderMercenaryService;
                    // Only vassals of the same kingdom get honorable discharge
                    // Mercenaries who join same kingdom while on leave are still deserters (they broke their service commitment)
                    var isOnLeaveBecameVassal = wasOnLeave && lordKingdom != null && lordKingdom == newKingdom && !playerClan.IsUnderMercenaryService;
                    var isOnLeaveDifferentKingdom = wasOnLeave && lordKingdom != null && lordKingdom != newKingdom;
                    var isOnLeaveSameKingdomAsMercenary = wasOnLeave && lordKingdom != null && lordKingdom == newKingdom && playerClan.IsUnderMercenaryService;
                    
                    // Skip initial enlistment (joining lord's kingdom as mercenary during StartEnlist)
                    if (isInitialEnlistment)
                    {
                        return; // Don't process - this is normal enlistment flow
                    }
                    
                    // Handle honorable discharge cases:
                    // 1. Promoted to vassal in same kingdom (active enlistment)
                    // 2. On leave and became vassal of same kingdom
                    if (isPromotionToVassal || isOnLeaveBecameVassal)
                    {
                        ModLogger.Info("Enlistment",
                            $"Player became vassal of {newKingdom.Name} while {(IsEnlisted ? "enlisted" : "on leave")} - ending enlistment honorably");

                        // Clear leave state if on leave
                        if (_isOnLeave)
                        {
                            _isOnLeave = false;
                            _leaveStartDate = CampaignTime.Zero;
                        }

                        // End enlistment honorably since player elevated their service
                        StopEnlist($"Player became vassal of {newKingdom.Name}", isHonorableDischarge: true);

                        var message = new TextObject(
                            "{=Enlisted_Message_BecameVassal}You have become a vassal of {KINGDOM}. Your enlistment has ended honorably.");
                        message.SetTextVariable("KINGDOM", newKingdom.Name);
                        InformationManager.DisplayMessage(new InformationMessage(message.ToString()));
                        
                        return; // Early return - don't process grace period logic
                    }
                    
                    // Handle desertion cases:
                    // 1. Active enlistment + joined different kingdom
                    // 2. On leave + joined different kingdom
                    // 3. On leave + joined same kingdom as mercenary (they left and came back, breaking service commitment)
                    if (isKingdomTransfer || isOnLeaveDifferentKingdom || isOnLeaveSameKingdomAsMercenary)
                    {
                        ModLogger.Info("Desertion",
                            $"Player joined different kingdom {newKingdom.Name} while {(IsEnlisted ? "enlisted" : "on leave")} - treating as desertion");

                        // Clear leave state if on leave
                        if (_isOnLeave)
                        {
                            _isOnLeave = false;
                            _leaveStartDate = CampaignTime.Zero;
                        }

                        // Set pending desertion kingdom to the original lord's kingdom
                        if (lordKingdom != null)
                        {
                            _pendingDesertionKingdom = lordKingdom;
                            
                            // Apply desertion penalties
                            ApplyDesertionPenalties();
                        }

                        // Fire event for other mods (before StopEnlist clears state)
                        OnLeaveEnded?.Invoke();

                        // End enlistment as desertion
                        StopEnlist($"Player joined different kingdom {newKingdom.Name} while {(IsEnlisted ? "enlisted" : "on leave")} - desertion", isHonorableDischarge: false);

                        var desertionMessage = new TextObject(
                            "{=Enlisted_Message_DesertedToOtherKingdom}You have deserted {ORIGINAL_KINGDOM} by joining {NEW_KINGDOM}. You have been branded a deserter.");
                        desertionMessage.SetTextVariable("ORIGINAL_KINGDOM", lordKingdom?.Name ?? new TextObject("{=enlist_fallback_army}your lord's army"));
                        desertionMessage.SetTextVariable("NEW_KINGDOM", newKingdom.Name);
                        InformationManager.DisplayMessage(new InformationMessage(desertionMessage.ToString()));
                        
                        return; // Early return - don't process grace period logic
                    }
                }
            }

            // Edge case: If player changes kingdoms during grace period, clear it
            // Player can only rejoin the pending desertion kingdom during grace period
            // Any kingdom change (join different kingdom, become independent) invalidates grace period
            if (IsInDesertionGracePeriod)
            {
                // If player left the grace period kingdom (became independent or joined different kingdom), clear grace period
                if (oldKingdom == _pendingDesertionKingdom && newKingdom != _pendingDesertionKingdom)
                {
                    if (newKingdom == null)
                    {
                        ModLogger.Info("Desertion", "Player left kingdom during grace period - clearing grace period");
                    }
                    else
                    {
                        ModLogger.Info("Desertion",
                            $"Player changed kingdoms during grace period - clearing grace period (left: {oldKingdom?.Name}, joined: {newKingdom?.Name})");
                    }

                    ClearDesertionGracePeriod();
                }
                // If player is joining the grace period kingdom from a different kingdom, that's okay (rejoin during grace period)
                // This is handled in StartEnlist() which clears the grace period when rejoining
            }
        }

        /// <summary>
        ///     Handle kingdom destruction.
        ///     Edge case: If pending desertion kingdom is destroyed during grace period, clear grace period.
        ///     Player can no longer rejoin a destroyed kingdom, so grace period is invalid.
        /// </summary>
        private void OnKingdomDestroyed(Kingdom kingdom)
        {
            if (IsInDesertionGracePeriod && kingdom == _pendingDesertionKingdom)
            {
                ModLogger.Info("Desertion",
                    $"Kingdom {kingdom.Name} destroyed during grace period - clearing grace period");
                ClearDesertionGracePeriod();

                // Notify player that grace period is cancelled due to kingdom destruction
                var message =
                    new TextObject("{=enlist_kingdom_fallen}The kingdom you served has fallen. Your grace period has been cancelled.");
                InformationManager.DisplayMessage(new InformationMessage(message.ToString()));
            }
        }

        /// <summary>
        ///     Add XP to player's military progression.
        ///     Called by duties system and other progression sources.
        ///     Triggers promotion notifications when XP thresholds are reached.
        /// </summary>
        public void AddEnlistmentXP(int xp, string source = "General")
        {
            if (!IsEnlisted || xp <= 0)
            {
                return;
            }

            var previousXP = _enlistmentXP;
            _enlistmentXP += xp;

            // Get tier requirements to show progress
            var tierXP = Enlisted.Features.Assignments.Core.ConfigurationManager.GetTierXpRequirements();
            var nextTierXP = _enlistmentTier < tierXP.Length ? tierXP[_enlistmentTier] : tierXP[tierXP.Length - 1];
            var progressPercent = nextTierXP > 0 ? _enlistmentXP * 100 / nextTierXP : 100;

            ModLogger.Info("XP",
                $"+{xp} XP from {source} | Total: {_enlistmentXP}/{nextTierXP} ({progressPercent}% to Tier {_enlistmentTier + 1})");

            // Track XP for summary
            ModLogger.IncrementSummary("xp_earned", 1, xp);

            // Fire event for other mods
            OnXPGained?.Invoke(xp, source);

            // Check if we crossed a promotion threshold
            CheckPromotionNotification(previousXP, _enlistmentXP);
        }

        /// <summary>
        ///     Awards battle participation XP after a battle ends (called from OnMapEventEnded).
        ///     Reads values from progression_config.json for configurability.
        ///     Note: This is a fallback - OnPlayerBattleEnd awards XP first if it fires.
        /// </summary>
        /// <param name="participated">Whether the player actively participated in the battle.</param>
        private void AwardBattleXP(bool participated)
        {
            try
            {
                if (!IsEnlisted)
                {
                    return;
                }

                // Don't award XP if player is waiting in reserve - they're sitting out all battles
                if (EnlistedEncounterBehavior.IsWaitingInReserve)
                {
                    ModLogger.Debug("Battle", "Skipping XP award - player is waiting in reserve");
                    return;
                }

                // Prevent double XP awards - OnPlayerBattleEnd may have already awarded XP
                if (_battleXPAwardedThisBattle)
                {
                    ModLogger.Debug("Battle", "Skipping XP award - already awarded via OnPlayerBattleEnd");
                    return;
                }

                if (!participated)
                {
                    ModLogger.Debug("Battle", "No battle XP awarded - player did not participate");
                    return;
                }

                // Get XP values from config
                var battleXP = Enlisted.Features.Assignments.Core.ConfigurationManager.GetBattleParticipationXp();

                if (battleXP > 0)
                {
                    AddEnlistmentXP(battleXP, "Battle Participation");
                    _battleXPAwardedThisBattle = true;

                    // Show notification to player
                    var message = $"Battle completed! +{battleXP} XP";
                    InformationManager.DisplayMessage(new InformationMessage(message));

                    ModLogger.Info("Battle", $"Battle XP awarded: {battleXP} (participation) via OnMapEventEnded");
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("Battle", $"Error awarding battle XP: {ex.Message}");
            }
        }

        /// <summary>
        ///     Set tier directly (called by PromotionBehavior for immediate promotion).
        ///     When crossing into Tier 4, reclaims companions from lord's party for retinue system.
        /// </summary>
        public void SetTier(int tier)
        {
            if (!IsEnlisted || tier < 1 || tier > 6)
            {
                ModLogger.Debug("Enlistment", $"SetTier rejected - Enlisted: {IsEnlisted}, Tier: {tier}");
                return;
            }

            var previousTier = _enlistmentTier;
            _enlistmentTier = tier;
            ModLogger.Info("Enlistment", $"Tier changed: {previousTier} → {tier} (XP: {_enlistmentXP})");

            // When crossing into Tier 4, reclaim companions from lord's party
            // This enables the Companion Assignments feature in the Command Tent
            if (previousTier < 4 && tier >= 4)
            {
                ReclaimCompanionsFromLord();
            }
        }

        /// <summary>
        ///     Change selected duty (called from duty selection menu).
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
                var duties = EnlistedDutiesBehavior.Instance;
                if (duties != null)
                {
                    duties.RemoveDuty(_selectedDuty);
                }
            }

            _selectedDuty = dutyId;

            // Add the new duty to active duties for daily XP processing
            var dutiesBehavior = EnlistedDutiesBehavior.Instance;
            if (dutiesBehavior != null)
            {
                dutiesBehavior.AssignDuty(dutyId);
            }

            ModLogger.Info("Duties", $"Changed duty to: {dutyId}");
        }

        /// <summary>
        ///     Change selected profession (called from duty selection menu).
        /// </summary>
        public void SetSelectedProfession(string professionId)
        {
            if (!IsEnlisted || string.IsNullOrEmpty(professionId))
            {
                return;
            }

            // Remove previous profession if different and not "none"
            if (_selectedProfession != professionId && _selectedProfession != "none" &&
                !string.IsNullOrEmpty(_selectedProfession))
            {
                var duties = EnlistedDutiesBehavior.Instance;
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
                var dutiesBehavior = EnlistedDutiesBehavior.Instance;
                if (dutiesBehavior != null)
                {
                    dutiesBehavior.AssignDuty(professionId);
                }
            }

            ModLogger.Info("Duties", $"Changed profession to: {professionId}");
        }

        /// <summary>
        ///     Check if promotion notification should be triggered after XP gain.
        ///     Now integrates with Phase 2B troop selection system.
        /// </summary>
        private void CheckPromotionNotification(int previousXP, int currentXP)
        {
            try
            {
                // Load tier XP requirements from progression_config.json
                var tierXPRequirements = EnlistedConfig.GetTierXpRequirements();

                // Get actual max tier from config (e.g., 6 for tiers 1-6)
                var maxTier = EnlistedConfig.GetMaxTier();

                // Bounds check: ensure we don't go beyond array limits
                if (_enlistmentTier < 0 || _enlistmentTier > maxTier)
                {
                    ModLogger.Debug("Progression",
                        $"Skipping promotion check - tier {_enlistmentTier} out of bounds (max: {maxTier})");
                    return;
                }

                // Check if we crossed any promotion threshold up to max tier
                for (var tier = _enlistmentTier; tier < maxTier && tier >= 0; tier++)
                {
                    var requiredXP = tierXPRequirements[tier];

                    // If we crossed from below to above a threshold
                    if (previousXP < requiredXP && currentXP >= requiredXP)
                    {
                        // Phase 2B: Trigger troop selection system
                        var troopSelectionManager = TroopSelectionManager.Instance;
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
        ///     Apply basic promotion without equipment change (fallback method).
        ///     Used when troop selection system is unavailable.
        /// </summary>
        public void ApplyBasicPromotion(int newTier)
        {
            try
            {
                _enlistmentTier = newTier;
                var rankName = GetRankName(newTier);

                var message =
                    new TextObject("{=enlist_promotion_quartermaster}Promoted to {RANK} (Tier {TIER})! Visit the quartermaster for equipment upgrades.");
                message.SetTextVariable("RANK", rankName);
                message.SetTextVariable("TIER", newTier.ToString());

                InformationManager.DisplayMessage(new InformationMessage(message.ToString()));

                ModLogger.Info("Promotion", $"Basic promotion applied to {rankName} (Tier {newTier})");

                // Fire event for other mods
                OnPromoted?.Invoke(newTier);
            }
            catch (Exception ex)
            {
                ModLogger.Error("Promotion", "Error applying basic promotion", ex);
            }
        }

        /// <summary>
        ///     Show promotion notification directly to player.
        /// </summary>
        private void ShowPromotionNotification(int availableTier)
        {
            try
            {
                var rankNameStr = GetRankName(availableTier);
                var rankName = new TextObject(rankNameStr);

                var message =
                    new TextObject(
                        "{=Enlisted_Promotion_Available}Promotion available! You can advance to {RANK} (Tier {TIER}). Press 'P' to choose your advancement!");
                message.SetTextVariable("RANK", rankName);
                message.SetTextVariable("TIER", availableTier);

                InformationManager.DisplayMessage(new InformationMessage(message.ToString()));
                ModLogger.Info("Progression", $"Promotion notification shown for Tier {availableTier}");
            }
            catch (Exception ex)
            {
                ModLogger.Error("Progression", $"Error showing promotion notification: {ex.Message}");
            }
        }

        /// <summary>
        ///     Get rank name for tier. Uses names from progression_config.json.
        /// </summary>
        public string GetRankName(int tier)
        {
            // Use configured tier names from progression_config.json
            return Enlisted.Features.Assignments.Core.ConfigurationManager.GetTierName(tier);
        }

        #region Minor Faction War Relations

        /// <summary>
        ///     Mirrors the lord's faction war relations to the player clan when enlisting with a minor faction.
        ///     This enables proper nameplate coloring (green for allies, red for enemies) and battle participation.
        ///     Uses FactionManager.DeclareWar directly (not DeclareWarAction) to avoid firing campaign events.
        /// </summary>
        /// <param name="lord">The minor faction lord the player is enlisting with.</param>
        private void MirrorMinorFactionWarRelations(Hero lord)
        {
            try
            {
                var lordFaction = lord.MapFaction;
                var playerClan = Clan.PlayerClan;

                if (lordFaction == null || playerClan == null)
                {
                    ModLogger.Debug("Enlistment", "Cannot mirror war relations - lord faction or player clan is null");
                    return;
                }

                // Clear any previous tracking in case of re-enlistment
                _minorFactionWarRelations.Clear();

                // Get all factions the lord's faction is at war with
                var enemyFactions = lordFaction.FactionsAtWarWith?.ToList() ?? new List<IFaction>();

                if (enemyFactions.Count == 0)
                {
                    ModLogger.Info("FactionRelations", 
                        $"Minor faction lord {lord.Name} has no enemies - no war relations to mirror");
                    return;
                }

                ModLogger.Info("FactionRelations", 
                    $"Mirroring {enemyFactions.Count} war relations from {lordFaction.Name} to player clan");

                foreach (var enemyFaction in enemyFactions)
                {
                    // Skip if already at war (player might have their own existing wars)
                    if (FactionManager.IsAtWarAgainstFaction(playerClan, enemyFaction))
                    {
                        ModLogger.Debug("FactionRelations", 
                            $"Already at war with {enemyFaction.Name} - skipping");
                        continue;
                    }

                    // Skip constant-war factions (bandits, etc.) - diplomacy model handles these automatically
                    if (Campaign.Current.Models.DiplomacyModel.IsAtConstantWar(playerClan, enemyFaction))
                    {
                        ModLogger.Debug("FactionRelations", 
                            $"Constant war with {enemyFaction.Name} (bandits/outlaws) - handled by diplomacy model");
                        continue;
                    }

                    // Use low-level FactionManager.DeclareWar - does NOT fire OnWarDeclared events
                    FactionManager.DeclareWar(playerClan, enemyFaction);
                    
                    // Track this relation for cleanup when service ends
                    _minorFactionWarRelations.Add(enemyFaction.StringId);

                    ModLogger.Info("FactionRelations", 
                        $"Declared war with {enemyFaction.Name} (mirrored from {lordFaction.Name})");
                }

                // Refresh visuals so nameplate colors update immediately
                RefreshFactionVisuals();

                ModLogger.Info("FactionRelations", 
                    $"Mirrored {_minorFactionWarRelations.Count} war relations for minor faction enlistment");
            }
            catch (Exception ex)
            {
                ModLogger.Error("FactionRelations", $"Error mirroring war relations: {ex.Message}");
            }
        }

        /// <summary>
        ///     Restores faction relations to neutral that were created when enlisting with a minor faction.
        ///     Called when enlistment ends to return the player to their original diplomatic state.
        ///     Uses FactionManager.SetNeutral directly (not MakePeaceAction) to avoid firing campaign events.
        /// </summary>
        private void RestoreMinorFactionWarRelations()
        {
            try
            {
                if (_minorFactionWarRelations == null || _minorFactionWarRelations.Count == 0)
                {
                    return;
                }

                var playerClan = Clan.PlayerClan;
                if (playerClan == null)
                {
                    ModLogger.Debug("FactionRelations", "Cannot restore relations - player clan is null");
                    _minorFactionWarRelations.Clear();
                    return;
                }

                ModLogger.Info("FactionRelations", 
                    $"Restoring {_minorFactionWarRelations.Count} war relations to neutral");

                var restoredCount = 0;
                foreach (var factionId in _minorFactionWarRelations.ToList())
                {
                    // Look up the faction by StringId
                    var faction = Campaign.Current?.Factions?.FirstOrDefault(f => f.StringId == factionId);
                    if (faction == null)
                    {
                        ModLogger.Debug("FactionRelations", 
                            $"Faction {factionId} not found (may have been destroyed) - skipping");
                        continue;
                    }

                    // Skip constant-war factions - can't make peace with them
                    if (Campaign.Current.Models.DiplomacyModel.IsAtConstantWar(playerClan, faction))
                    {
                        ModLogger.Debug("FactionRelations", 
                            $"Cannot make peace with {faction.Name} (constant war) - skipping");
                        continue;
                    }

                    // Use low-level FactionManager.SetNeutral - does NOT fire OnMakePeace events
                    FactionManager.SetNeutral(playerClan, faction);
                    restoredCount++;

                    ModLogger.Info("FactionRelations", 
                        $"Restored neutral relations with {faction.Name}");
                }

                // Clear tracking
                _minorFactionWarRelations.Clear();

                // Refresh visuals so nameplate colors update
                RefreshFactionVisuals();

                ModLogger.Info("FactionRelations", 
                    $"Restored {restoredCount} faction relations to neutral");
            }
            catch (Exception ex)
            {
                ModLogger.Error("FactionRelations", $"Error restoring war relations: {ex.Message}");
                // Clear tracking even on error to prevent stale state
                _minorFactionWarRelations?.Clear();
            }
        }

        /// <summary>
        ///     Refreshes party visuals to update nameplate colors after faction relation changes.
        ///     This marks enemy faction parties as "dirty" so their nameplates redraw with correct colors.
        /// </summary>
        private void RefreshFactionVisuals()
        {
            try
            {
                var playerFaction = Hero.MainHero?.MapFaction;
                if (playerFaction == null) return;

                // Mark all visible parties from factions at war with player as dirty
                // This triggers nameplate color refresh (same pattern as DeclareWarAction.ApplyInternal)
                foreach (var party in MobileParty.All)
                {
                    if (!party.IsVisible) continue;
                    
                    var partyFaction = party.MapFaction;
                    if (partyFaction == null) continue;

                    // Check if this party's faction is at war with player
                    if (FactionManager.IsAtWarAgainstFaction(playerFaction, partyFaction))
                    {
                        party.Party.SetVisualAsDirty();
                    }
                }

                // Also mark settlements
                foreach (var settlement in Settlement.All)
                {
                    if (!settlement.IsVisible) continue;
                    
                    var settlementFaction = settlement.MapFaction;
                    if (settlementFaction == null) continue;

                    if (FactionManager.IsAtWarAgainstFaction(playerFaction, settlementFaction))
                    {
                        settlement.Party.SetVisualAsDirty();
                    }
                }

                ModLogger.Debug("FactionRelations", "Refreshed faction visuals after relation change");
            }
            catch (Exception ex)
            {
                ModLogger.Error("FactionRelations", $"Error refreshing faction visuals: {ex.Message}");
            }
        }

        #endregion

        /// <summary>
        ///     Backup player equipment before service to prevent loss.
        ///     Now delegated to EquipmentManager for centralized handling.
        /// </summary>
        private void BackupPlayerEquipment()
        {
            try
            {
                // Phase 2B: Use centralized equipment management
                var equipmentManager = EquipmentManager.Instance;
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
        ///     Restore personal equipment from backup.
        ///     Now delegated to EquipmentManager for centralized handling.
        /// </summary>
        private void RestorePersonalEquipment()
        {
            try
            {
                // Phase 2B: Use centralized equipment management
                var equipmentManager = EquipmentManager.Instance;
                if (equipmentManager != null)
                {
                    equipmentManager.RestorePersonalEquipment();
                }
                else
                {
                    // Fallback: Basic restoration if equipment manager not available
                    if (_personalBattleEquipment != null)
                    {
                        Helpers.EquipmentHelper.AssignHeroEquipmentFromEquipment(Hero.MainHero, _personalBattleEquipment);
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
        /// Restore personal equipment to INVENTORY for retirement.
        /// Player keeps military gear AND gets old stuff back in inventory.
        /// This is a reward for completing honorable service.
        /// </summary>
        private void RestorePersonalEquipmentToInventory()
        {
            try
            {
                var equipmentManager = EquipmentManager.Instance;
                if (equipmentManager != null)
                {
                    equipmentManager.RestorePersonalEquipmentToInventory();
                }
                else
                {
                    // Fallback: Add backed up items to inventory
                    var itemRoster = MobileParty.MainParty.ItemRoster;
                    
                    if (_personalBattleEquipment != null)
                    {
                        for (var slot = EquipmentIndex.Weapon0; slot <= EquipmentIndex.HorseHarness; slot++)
                        {
                            var item = _personalBattleEquipment[slot].Item;
                            if (item != null)
                            {
                                itemRoster.AddToCounts(new EquipmentElement(item), 1);
                            }
                        }
                    }
                    
                    ModLogger.Info("Equipment", "Retirement: personal equipment added to inventory (fallback)");
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("Equipment", "Error restoring equipment to inventory for retirement", ex);
            }
        }

        /// <summary>
        ///     Make player's ships invulnerable to prevent damage while enlisted.
        ///     Ships remain with the player but cannot take damage during military service.
        ///     This protects player investment in ships while serving under a lord.
        /// </summary>
        private void SetPlayerShipsInvulnerable()
        {
            try
            {
                var mainParty = MobileParty.MainParty;
                if (mainParty?.Party?.Ships == null)
                {
                    ModLogger.Debug("Naval", "No ships to protect - player has no ships");
                    return;
                }

                var protectedCount = 0;
                foreach (Ship ship in mainParty.Party.Ships)
                {
                    if (ship != null && !ship.IsInvulnerable)
                    {
                        ship.IsInvulnerable = true;
                        protectedCount++;
                        ModLogger.Debug("Naval",
                            $"Protected ship: {ship.Name} (HP: {ship.HitPoints}/{ship.MaxHitPoints})");
                    }
                }

                if (protectedCount > 0)
                {
                    ModLogger.Info("Naval", $"Protected {protectedCount} player ship(s) from damage during enlistment");
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("Naval", "Error protecting player ships", ex);
            }
        }

        /// <summary>
        ///     Restore player's ships to normal vulnerability after discharge.
        ///     Called when player is fully discharged (not during grace period).
        /// </summary>
        private void RestorePlayerShipsVulnerability()
        {
            try
            {
                var mainParty = MobileParty.MainParty;
                if (mainParty?.Party?.Ships == null)
                {
                    ModLogger.Debug("Naval", "No ships to restore - player has no ships");
                    return;
                }

                var restoredCount = 0;
                foreach (Ship ship in mainParty.Party.Ships)
                {
                    if (ship != null && ship.IsInvulnerable)
                    {
                        ship.IsInvulnerable = false;
                        restoredCount++;
                        ModLogger.Debug("Naval",
                            $"Restored ship vulnerability: {ship.Name} (HP: {ship.HitPoints}/{ship.MaxHitPoints})");
                    }
                }

                if (restoredCount > 0)
                {
                    ModLogger.Info("Naval",
                        $"Restored vulnerability to {restoredCount} player ship(s) after discharge");
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("Naval", "Error restoring ship vulnerability", ex);
            }
        }

        /// <summary>
        ///     Assign initial recruit equipment based on lord's culture.
        /// </summary>
        private void AssignInitialEquipment()
        {
            try
            {
                if (_enlistedLord?.Culture?.BasicTroop?.Equipment == null)
                {
                    return;
                }

                // Use lord's culture basic troop equipment
                var basicTroopEquipment = _enlistedLord.Culture.BasicTroop.Equipment;
                Helpers.EquipmentHelper.AssignHeroEquipmentFromEquipment(Hero.MainHero, basicTroopEquipment);

                ModLogger.Info("Equipment", $"Assigned initial {_enlistedLord.Culture.Name} recruit equipment");
            }
            catch (Exception ex)
            {
                ModLogger.Error("Equipment", "Error assigning initial equipment", ex);
            }
        }

        /// <summary>
        ///     Set initial formation for new recruits based on their assigned equipment.
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
                var initialFormation = "infantry"; // Default for recruits

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
        ///     Handle battle participation detection and automatic joining.
        ///     Detects when lord enters combat and enables player battle participation.
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
                    main.IsActive = true; // This should trigger encounter menu

                    // Try to force battle participation through positioning
                    // Position is handled by Escort AI (SetMoveEscortParty)

                    var message = new TextObject("{=enlist_following_battle}Following your lord into battle!");
                    InformationManager.DisplayMessage(new InformationMessage(message.ToString()));
                }
                else if (!lordInBattle && playerInBattle)
                {
                    ModLogger.Info("Battle", "Battle ended - restoring normal service");

                    // Restore normal enlisted state
                    main.IsActive = false; // Return to normal encounter prevention
                    main.IsVisible = false; // Return to invisible state
                }
                else if (!lordInBattle)
                {
                    // Normal state - not in battle
                    main.IsActive = false; // Disable party activity to prevent random encounters
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
        ///     Start temporary leave - suspend enlistment without ending service permanently.
        /// </summary>
        public void StartTemporaryLeave()
        {
            try
            {
                if (!IsEnlisted)
                {
                    return;
                }

                if (IsLeaveOnCooldown(out var cooldownDays))
                {
                    var cooldownMsg = new TextObject("{=Enlisted_Leave_Cooldown}Leave is on cooldown. {DAYS} days remain before you can request leave again.");
                    cooldownMsg.SetTextVariable("DAYS", cooldownDays);
                    InformationManager.DisplayMessage(new InformationMessage(cooldownMsg.ToString(), Colors.Red));
                    ModLogger.Info("Enlistment", $"Leave request blocked by cooldown ({cooldownDays} days remaining)");
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

                // Notify player of leave timer
                var maxLeaveDays = EnlistedConfig.LoadGameplayConfig().LeaveMaxDays;
                var message =
                    new TextObject(
                        "Leave granted. You have {DAYS} days to return to your lord or face desertion penalties.");
                message.SetTextVariable("DAYS", maxLeaveDays);
                InformationManager.DisplayMessage(new InformationMessage(message.ToString()));

                ModLogger.Info("Enlistment", $"Temporary leave started - {maxLeaveDays} days before desertion");

                if (_enlistedLord != null)
                {
                    Campaign.Current.VisualTrackerManager.RegisterObject(_enlistedLord);
                    var trackerMsg =
                        new TextObject("{=Enlisted_Message_LordTracked}Your lord has been marked on the map.");
                    InformationManager.DisplayMessage(new InformationMessage(trackerMsg.ToString()));
                }

                // Fire event for other mods
                OnLeaveStarted?.Invoke();
            }
            catch (Exception ex)
            {
                ModLogger.Error("Enlistment", "Error starting temporary leave", ex);
            }
        }

        /// <summary>
        ///     Return from temporary leave - resume enlistment with preserved data.
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

                if (_enlistedLord != null)
                {
                    Campaign.Current.VisualTrackerManager.RemoveTrackedObject(_enlistedLord);
                }

                // Clear leave state and start cooldown before next leave
                _isOnLeave = false;
                _leaveStartDate = CampaignTime.Zero;
                _leaveCooldownEnds = CampaignTime.Now + CampaignTime.Days(30);

                // NOTE: We intentionally do NOT transfer troops here. Unlike initial enlistment, 
                // troops recruited during leave are the player's personal force and should stay
                // with them when returning to service. The player earned these troops independently.

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
                EnlistedMenuBehavior.SafeActivateEnlistedMenu();
                ModLogger.Info("Enlistment",
                    "Service resumed - attempted enlisted status menu activation (with global guard)");

                // Fire event for other mods
                OnLeaveEnded?.Invoke();
            }
            catch (Exception ex)
            {
                ModLogger.Error("Enlistment", "Error returning from leave", ex);
            }
        }

        /// <summary>
        ///     Checks whether the player is currently on leave cooldown.
        /// </summary>
        public bool IsLeaveOnCooldown(out int daysRemaining)
        {
            daysRemaining = 0;

            if (_leaveCooldownEnds == CampaignTime.Zero || _leaveCooldownEnds <= CampaignTime.Now)
            {
                return false;
            }

            daysRemaining = (int)Math.Ceiling((_leaveCooldownEnds - CampaignTime.Now).ToDays);
            return true;
        }

        /// <summary>
        ///     Transfer service to a different lord in the same faction.
        ///     Preserves all progression (tier, XP, kills, service date).
        ///     Used when player is on leave or in grace period and wants to serve under a new commander.
        /// </summary>
        /// <param name="newLord">The new lord to serve under (must be in same faction).</param>
        public void TransferServiceToLord(Hero newLord)
        {
            if (newLord == null)
            {
                ModLogger.Error("Enlistment", "TransferServiceToLord called with null lord");
                return;
            }

            try
            {
                var previousLord = _enlistedLord;
                var previousLordName = previousLord?.Name?.ToString() ?? "Unknown";
                var newLordName = newLord.Name?.ToString() ?? "Unknown";

                // If transferring from grace period, restore saved progression values first
                // During grace, StopEnlist() clears _enlistmentTier/_enlistmentXP but saves them to grace state
                if (IsInDesertionGracePeriod && _savedGraceTier > 0)
                {
                    _enlistmentTier = _savedGraceTier;
                    _enlistmentXP = _savedGraceXP;
                    _enlistmentDate = _savedGraceEnlistmentDate != CampaignTime.Zero
                        ? _savedGraceEnlistmentDate
                        : CampaignTime.Now;
                    ModLogger.Info("Enlistment",
                        $"Restored grace progression for transfer: Tier={_enlistmentTier}, XP={_enlistmentXP}");
                }

                ModLogger.Info("Enlistment", $"Transferring service from {previousLordName} to {newLordName}");
                ModLogger.Info("Enlistment",
                    $"Preserving: Tier={_enlistmentTier}, XP={_enlistmentXP}, Kills={_currentTermKills}, Date={_enlistmentDate}");

                // Clear leave state if on leave
                if (_isOnLeave)
                {
                    if (previousLord != null)
                    {
                        Campaign.Current.VisualTrackerManager.RemoveTrackedObject(previousLord);
                    }

                    _isOnLeave = false;
                    _leaveStartDate = CampaignTime.Zero;
                    ModLogger.Debug("Enlistment", "Cleared leave state for transfer");
                }

                // Clear grace period if in grace (this is also a valid transfer path)
                if (IsInDesertionGracePeriod)
                {
                    ClearDesertionGracePeriod();
                    ModLogger.Debug("Enlistment", "Cleared grace period for transfer");
                }

                // Update the enlisted lord (progression stays the same)
                _enlistedLord = newLord;

                // Handle equipment for transfer
                // During grace period, player keeps their enlisted equipment (already backed up)
                // Only need to handle case where equipment wasn't backed up (shouldn't happen normally)
                if (!_hasBackedUpEquipment)
                {
                    // Edge case: equipment not backed up - back it up now
                    BackupPlayerEquipment();
                    _hasBackedUpEquipment = true;
                    ModLogger.Info("Enlistment", "Backed up personal equipment during service transfer");

                    // Apply enlisted equipment for new lord
                    var appliedGraceEquipment = TryApplyGraceEquipment(true, _savedGraceTroopId);
                    if (!appliedGraceEquipment)
                    {
                        AssignInitialEquipment();
                        SetInitialFormation();
                        ModLogger.Info("Enlistment", "Applied enlisted equipment during service transfer");
                    }
                }
                else
                {
                    // Normal case: player already has enlisted equipment from grace period
                    // Keep current equipment - they're still a soldier in the same kingdom
                    ModLogger.Info("Enlistment", "Keeping enlisted equipment during service transfer (same kingdom)");
                }

                // Transfer any companions/troops to new lord's party
                TransferPlayerTroopsToLord();

                // Finish any active encounter first
                if (PlayerEncounter.Current != null)
                {
                    if (PlayerEncounter.InsideSettlement)
                    {
                        PlayerEncounter.LeaveSettlement();
                    }

                    PlayerEncounter.Finish(true);
                }

                // Re-attach to new lord's party
                EncounterGuard.TryAttachOrEscort(newLord);

                // Configure party state for enlistment
                var main = MobileParty.MainParty;
                var newLordParty = newLord.PartyBelongedTo;
                if (main != null)
                {
                    main.IsVisible = false;
                    main.IsActive = true;
                    main.IgnoreByOtherPartiesTill(CampaignTime.Now + CampaignTime.Days(365f));

                    if (newLordParty != null)
                    {
                        main.SetMoveEscortParty(newLordParty, MobileParty.NavigationType.Default, false);
                        newLordParty.Party.SetAsCameraFollowParty();
                        ModLogger.Debug("Enlistment", $"Set escort AI to follow {newLordName}");
                    }

                    TrySetShouldJoinPlayerBattles(main, true);
                }

                // Activate enlisted status menu
                EnlistedMenuBehavior.SafeActivateEnlistedMenu();

                ModLogger.Info("Enlistment", $"Service transfer complete: {previousLordName} -> {newLordName}");

                // Log state for diagnostics
                SessionDiagnostics.LogStateTransition("Enlistment", "OnLeave/Grace", "Enlisted",
                    $"Transfer from {previousLordName} to {newLordName}, Tier: {_enlistmentTier}, XP: {_enlistmentXP}");
            }
            catch (Exception ex)
            {
                ModLogger.Error("Enlistment", $"Error transferring service to {newLord?.Name}: {ex.Message}", ex);
            }
        }

        /// <summary>
        ///     Transfer player companions and troops to lord's party.
        ///     Called when returning from leave or initial enlistment.
        ///     At Tier 4+, companions stay with the player to form their retinue squad.
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

                // At Tier 4+, companions stay with player to form the retinue squad
                // This enables the Command Tent's Personal Retinue system
                var keepCompanions = _enlistmentTier >= 4;

                var transferCount = 0;
                var companionCount = 0;
                var companionsKept = 0;

                // Transfer non-player troops to lord's party (with tier-based companion exception)
                var troopsToTransfer = new List<TroopRosterElement>();
                foreach (var troop in main.MemberRoster.GetTroopRoster())
                {
                    // Skip the player character
                    if (troop.Character == CharacterObject.PlayerCharacter)
                    {
                        continue;
                    }

                    // At Tier 4+, keep companions with player for retinue system
                    if (keepCompanions && troop.Character.IsHero &&
                        troop.Character.HeroObject?.IsPlayerCompanion == true)
                    {
                        companionsKept++;
                        ModLogger.Debug("Enlistment",
                            $"Keeping companion {troop.Character.Name} with player (Tier {_enlistmentTier}+)");
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

                if (transferCount > 0 || companionsKept > 0)
                {
                    if (transferCount > 0)
                    {
                        var message =
                            new TextObject(
                                "Your {TROOP_COUNT} troops{COMPANION_INFO} have joined your lord's party for the duration of service.");
                        message.SetTextVariable("TROOP_COUNT", transferCount.ToString());
                        message.SetTextVariable("COMPANION_INFO",
                            companionCount > 0 ? $" (including {companionCount} companions)" : "");
                        InformationManager.DisplayMessage(new InformationMessage(message.ToString()));
                    }

                    if (companionsKept > 0)
                    {
                        var keptMessage = new TextObject(
                            "{=ct_companions_retained}Your {COUNT} companion(s) remain under your direct command.");
                        keptMessage.SetTextVariable("COUNT", companionsKept.ToString());
                        InformationManager.DisplayMessage(new InformationMessage(keptMessage.ToString()));
                    }

                    ModLogger.Info("Enlistment",
                        $"Transfer complete: {transferCount} troops ({companionCount} companions) to lord, " +
                        $"{companionsKept} companions kept with player (Tier {_enlistmentTier})");
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("Enlistment", "Error transferring troops to lord", ex);
            }
        }

        /// <summary>
        ///     Restore companions to player party on retirement (regular troops stay with lord).
        ///     Companions should be available for the player again after service ends.
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
                    lordParty.MemberRoster.AddToCounts(companion.Character, -1 * companion.Number, false, 0, 0, true,
                        -1);
                    // Add back to player party
                    main.MemberRoster.AddToCounts(companion.Character, companion.Number, false, 0, 0, true, -1);
                }

                if (companionsRestored > 0)
                {
                    var message =
                        new TextObject("{=enlist_companions_rejoined}Your {COMPANION_COUNT} companions have rejoined your party upon retirement.");
                    message.SetTextVariable("COMPANION_COUNT", companionsRestored.ToString());
                    InformationManager.DisplayMessage(new InformationMessage(message.ToString()));

                    ModLogger.Info("Enlistment",
                        $"Restored {companionsRestored} companions to player party on retirement");
                }

                // Note: Regular troops stay with the lord as they've become part of the military unit
            }
            catch (Exception ex)
            {
                ModLogger.Error("Enlistment", "Error restoring companions on retirement", ex);
            }
        }

        /// <summary>
        ///     Reclaims companions from the lord's party back to the player's MainParty.
        ///     Called when the player reaches Tier 4 (via promotion or save load) to enable the
        ///     Companion Assignments feature. At Tier 4+, companions should fight alongside the player.
        /// </summary>
        private void ReclaimCompanionsFromLord()
        {
            try
            {
                // Only reclaim at Tier 4+ (when companion assignments become available)
                if (_enlistmentTier < 4)
                {
                    return;
                }

                var main = MobileParty.MainParty;
                var lordParty = _enlistedLord?.PartyBelongedTo;

                if (main == null || lordParty == null)
                {
                    return;
                }

                var companionsReclaimed = 0;
                var companionsToReclaim = new List<TroopRosterElement>();

                // Find player's companions in lord's party
                foreach (var troop in lordParty.MemberRoster.GetTroopRoster())
                {
                    // Only reclaim hero companions that belong to the player's clan
                    if (troop.Character.IsHero && troop.Character != CharacterObject.PlayerCharacter)
                    {
                        var hero = troop.Character.HeroObject;
                        if (hero != null && hero.IsPlayerCompanion && hero.Clan == Clan.PlayerClan)
                        {
                            companionsToReclaim.Add(troop);
                            companionsReclaimed += troop.Number;
                        }
                    }
                }

                // Transfer companions back to player's MainParty
                foreach (var companion in companionsToReclaim)
                {
                    // Remove from lord's party
                    lordParty.MemberRoster.AddToCounts(companion.Character, -1 * companion.Number, false, 0, 0, true, -1);
                    // Add to player's party
                    main.MemberRoster.AddToCounts(companion.Character, companion.Number, false, 0, 0, true, -1);
                }

                if (companionsReclaimed > 0)
                {
                    var message = new TextObject(
                        "{=ct_companions_reclaimed}Your {COMPANION_COUNT} companion(s) have joined your personal retinue.");
                    message.SetTextVariable("COMPANION_COUNT", companionsReclaimed.ToString());
                    InformationManager.DisplayMessage(new InformationMessage(message.ToString(), Colors.Cyan));

                    ModLogger.Info("Enlistment",
                        $"Reclaimed {companionsReclaimed} companions from lord's party (Tier {_enlistmentTier} promotion)");
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("Enlistment", "Error reclaiming companions from lord", ex);
            }
        }

        /// <summary>
        ///     Army-aware battle participation logic.
        ///     Handles both individual lord battles and army battles with proper state management.
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
                        ModLogger.Info("Battle",
                            $"ARMY BATTLE DETECTED - Army Leader: {armyLeader?.LeaderHero?.Name}, Lord: {_enlistedLord.Name}");
                        ModLogger.Debug("Battle",
                            $"Army details - Army size: {lordArmy.Parties.Count}, Battle party: {battleParty.LeaderHero?.Name}");
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
        ///     Handle army battle participation.
        ///     Uses army membership for battle participation, keeps player party inactive to avoid conflicts.
        /// </summary>
        private void HandleArmyBattle(MobileParty main, MobileParty lordParty, Army lordArmy)
        {
            try
            {
                // Don't activate player party - use army membership for battle participation
                // The player participates through being part of the lord's army, not as independent party
                ModLogger.Info("Battle", "Army battle detected, player participates through army membership");

                // Ensure player is positioned with army for battle camera/interface
                // Position is handled by Escort AI (SetMoveEscortParty)
            }
            catch (Exception ex)
            {
                ModLogger.Error("Battle", $"Error handling army battle: {ex.Message}");
            }
        }

        /// <summary>
        ///     Handle individual lord battle participation.
        ///     Creates temporary army for individual battles when needed.
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
                // Position is handled by Escort AI (SetMoveEscortParty)
            }
            catch (Exception ex)
            {
                ModLogger.Error("Battle", $"Error handling individual battle: {ex.Message}");
            }
        }

        /// <summary>
        ///     Clean up after battle ends.
        ///     Disbands temporary armies if we created them.
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
        ///     MINIMAL: Settlement entry detection for menu refresh only.
        ///     Just refreshes the menu when lord enters settlements - no complex logic.
        /// </summary>
        private void OnSettlementEntered(MobileParty party, Settlement settlement, Hero hero)
        {
            try
            {
                // CRITICAL: Skip all processing when player is a prisoner
                // The native PlayerCaptivity system handles everything during captivity
                // Interfering with party state or menus while captured causes crashes
                // (e.g., when captor enters a settlement with the player as prisoner)
                if (!IsEnlisted || _isOnLeave || Hero.MainHero.IsPrisoner)
                {
                    if (Hero.MainHero.IsPrisoner && IsEnlisted)
                    {
                        ModLogger.Debug("Settlement",
                            $"Skipping settlement entry handling - player is prisoner (captor entering {settlement?.Name})");
                    }

                    return;
                }

                var mainParty = MobileParty.MainParty;
                if (party == mainParty)
                {
                    ModLogger.Info("Settlement",
                        $"Player entered {settlement?.Name?.ToString() ?? "unknown"} ({settlement?.StringId ?? "unknown"})");

                    if (PlayerEncounter.Current != null && !PlayerEncounter.InsideSettlement)
                    {
                        try
                        {
                            ModLogger.Info("Settlement",
                                "Finishing PlayerEncounter before entering settlement to prevent InsideSettlement assertion");
                            PlayerEncounter.Finish(true);
                        }
                        catch (Exception ex)
                        {
                            ModLogger.Error("Settlement",
                                $"Error finishing PlayerEncounter before settlement entry: {ex.Message}");
                        }
                    }

                    if (mainParty != null)
                    {
                        if (!mainParty.IsActive)
                        {
                            mainParty.IsActive = true;
                        }

                        mainParty.IgnoreByOtherPartiesTill(CampaignTime.Now);
                    }

                    return;
                }

                // Only react when OUR enlisted lord enters a settlement
                if (hero == _enlistedLord)
                {
                    ModLogger.Info("Settlement", $"Lord {hero.Name} entered {settlement.Name} ({settlement.StringId})");

                    // NOTE: We used to call EnterSettlementAction.ApplyForParty() here to immediately
                    // pull the player into the settlement, but this causes EncounterMenuOverlayVM assertion
                    // failures ("Encounter overlay is open but MapEvent AND SiegeEvent is null").
                    // The escort AI naturally follows the lord into settlements - just a half-second delay.
                    // A small delay is better than a crash.

                    // CRITICAL: Finish any active PlayerEncounter before entering settlement
                    // This prevents InsideSettlement assertion failures when the army enters a settlement
                    // Party encounters cannot exist while inside settlements
                    // According to API: StartPartyEncounter cannot be called when InsideSettlement is true
                    // We should finish party encounters when entering settlements to prevent assertion at line 2080
                    if (PlayerEncounter.Current != null && !PlayerEncounter.InsideSettlement)
                    {
                        try
                        {
                            ModLogger.Info("Settlement",
                                "Finishing PlayerEncounter before entering settlement to prevent InsideSettlement assertion");
                            PlayerEncounter.Finish(true);
                        }
                        catch (Exception ex)
                        {
                            ModLogger.Error("Settlement",
                                $"Error finishing PlayerEncounter before settlement entry: {ex.Message}");
                        }
                    }

                    // Only pause/activate menu for towns and castles, not villages
                    // Villages should allow continuous time flow while following the lord
                    if (settlement.IsTown || settlement.IsCastle)
                    {
                        // CRITICAL GUARD: Don't interfere with battle/siege encounter menus
                        // CORRECT API: Use Party.MapEvent (not direct on MobileParty)
                        var inBattleOrSiege = mainParty?.Party.MapEvent != null ||
                                              PlayerEncounter.Current != null ||
                                              _enlistedLord?.PartyBelongedTo?.BesiegedSettlement != null;

                        if (!inBattleOrSiege)
                        {
                            // Trigger settlement entered event for player (MainParty) to activate perks
                            // This allows perks like "Show Your Scars" to trigger automatically when the lord enters a town
                            if (MobileParty.MainParty != null && Hero.MainHero != null &&
                                CampaignEventDispatcher.Instance != null)
                            {
                                try
                                {
                                    CampaignEventDispatcher.Instance.OnSettlementEntered(MobileParty.MainParty,
                                        settlement, Hero.MainHero);
                                    ModLogger.Info("Settlement",
                                        $"Triggered synthetic SettlementEntered for {settlement.Name} to activate perks");
                                }
                                catch (Exception ex)
                                {
                                    // Safely ignore errors from native listeners (e.g. due to unusual enlisted party state)
                                    ModLogger.Warn("Settlement",
                                        $"Synthetic perk trigger suppressed error: {ex.Message}");
                                }
                            }

                            // Safe to show enlisted menu - no battles/sieges active
                            EnlistedMenuBehavior.SafeActivateEnlistedMenu();
                            ModLogger.Debug("Settlement",
                                $"Activated enlisted menu for {settlement.Name} (town/castle)");
                        }
                        else
                        {
                            ModLogger.Info("Settlement",
                                $"GUARDED: Skipped enlisted menu activation - battle/siege active ({settlement.Name})");
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

        private void OnSettlementLeft(MobileParty party, Settlement settlement)
        {
            try
            {
                if (!IsEnlisted || _isOnLeave)
                {
                    return;
                }

                var mainParty = MobileParty.MainParty;
                if (party == mainParty)
                {
                    ModLogger.Info("Settlement",
                        $"Player left {settlement?.Name?.ToString() ?? "unknown"} ({settlement?.StringId ?? "unknown"})");

                    // Re-hide the party if we're not immediately entering a battle or siege.
                    bool inBattle = mainParty?.Party.MapEvent != null;
                    var inSiege = IsPartyInActiveSiege(mainParty);
                    if (!inBattle && !inSiege)
                    {
                        mainParty.IsVisible = false;
                        mainParty.IsActive = false;
                        mainParty.IgnoreByOtherPartiesTill(CampaignTime.Now + CampaignTime.Hours(0.5f));
                    }

                    if (_enlistedLord != null)
                    {
                        EncounterGuard.TryAttachOrEscort(_enlistedLord);
                    }

                    return;
                }

                if (party == _enlistedLord?.PartyBelongedTo)
                {
                    ModLogger.Info("Settlement",
                        $"Lord {_enlistedLord.Name} left {settlement?.Name?.ToString() ?? "unknown"} - pulling player to follow");

                    // LORD LEFT - pull the player out to follow!
                    // Check if player is still in this settlement
                    if (mainParty?.CurrentSettlement == settlement)
                    {
                        ModLogger.Info("Settlement", "Player still in settlement - forcing exit to follow lord");

                        // CRITICAL: Enable force hidden mode BEFORE exit to block visibility during transition
                        // This prevents race conditions where native code sets IsVisible=true before our hiding code runs
                        VisibilityEnforcementPatch.BeginForceHidden();

                        // Force leave the settlement
                        NextFrameDispatcher.RunNextFrame(() =>
                        {
                            try
                            {
                                if (PlayerEncounter.Current != null)
                                {
                                    if (PlayerEncounter.InsideSettlement)
                                    {
                                        PlayerEncounter.LeaveSettlement();
                                    }

                                    PlayerEncounter.Finish(true);
                                }

                                // CRITICAL: Set up escort FIRST so IsEmbeddedWithLord() returns true
                                // This ensures the VisibilityEnforcementPatch blocks visibility correctly
                                var main = MobileParty.MainParty;
                                if (main != null)
                                {
                                    main.IsActive = true; // Keep active for escort AI
                                    main.IgnoreByOtherPartiesTill(CampaignTime.Now + CampaignTime.Hours(1f));

                                    // Resume escort BEFORE setting visibility so TargetParty is set
                                    if (_enlistedLord != null)
                                    {
                                        EncounterGuard.TryAttachOrEscort(_enlistedLord);
                                    }

                                    // NOW hide the party - IsEmbeddedWithLord() will return true
                                    main.IsVisible = false;

                                    // Also hide the 3D visual entity (separate from nameplate VM)
                                    EncounterGuard.HidePlayerPartyVisual();
                                }

                                // Force hidden mode served its purpose - can disable now
                                // (It will auto-expire anyway, but clean up for clarity)
                                VisibilityEnforcementPatch.EndForceHidden();

                                // Activate enlisted menu
                                EnlistedMenuBehavior.SafeActivateEnlistedMenu();
                                ModLogger.Info("Settlement", "Player pulled from settlement to follow lord (hidden)");
                            }
                            catch (Exception ex)
                            {
                                // Make sure to end force hidden even on error
                                VisibilityEnforcementPatch.EndForceHidden();
                                ModLogger.Error("Settlement", $"Error pulling player from settlement: {ex.Message}");
                            }
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("Settlement", $"Error in settlement exit detection: {ex.Message}");
            }
        }

        /// <summary>
        ///     REVERT: Handle battle start events to inject player into lord's battles.
        ///     Using MapEventStarted for battle detection (BattleStarted is not available).
        /// </summary>
        private void OnMapEventStarted(MapEvent mapEvent, PartyBase attackerParty, PartyBase defenderParty)
        {
            try
            {
                if (!IsEnlisted || _isOnLeave || _enlistedLord == null)
                {
                    return;
                }

                // SAFETY: Never process enlistment battle flow while the player is a prisoner or
                // while capture cleanup is pending. The native captivity system owns the state during this time,
                // and forcing battle prep (teleport/army join) can crash when captors are attacked.
                if (Hero.MainHero.IsPrisoner || _playerCaptureCleanupScheduled)
                {
                    ModLogger.Info("EventSafety",
                        $"Skipping MapEventStarted - player prisoner or cleanup pending (IsPrisoner={Hero.MainHero.IsPrisoner}, CaptureCleanupScheduled={_playerCaptureCleanupScheduled})");
                    return;
                }

                // BUGFIX: Skip battle processing if player is already waiting in reserve.
                // When player was wounded and chose "Wait in Reserve", they intend to sit out ALL battles
                // in the current series. Without this check, each new MapEvent triggers menu switching,
                // creating a loop where the encounter menu keeps appearing and awarding duplicate XP.
                if (EnlistedEncounterBehavior.IsWaitingInReserve)
                {
                    ModLogger.Debug("Battle",
                        "Skipping MapEventStarted - player is waiting in reserve (already chose to sit out battles)");
                    return;
                }

                var lordParty = _enlistedLord.PartyBelongedTo;
                if (lordParty == null || mapEvent == null)
                {
                    return;
                }

                var main = MobileParty.MainParty;
                if (main == null)
                {
                    return;
                }

                // Determine whether this event matters to our enlisted service
                var lordIsAttacker = attackerParty?.MobileParty == lordParty || attackerParty == lordParty?.Party;
                var lordIsDefender = defenderParty?.MobileParty == lordParty || defenderParty == lordParty?.Party;
                var inArmy = lordParty.Army != null && main.Army == lordParty.Army;
                var armyLeaderInvolved = lordParty.Army?.LeaderParty?.Party == attackerParty ||
                                         lordParty.Army?.LeaderParty?.Party == defenderParty;

                // Check if lord is in the MapEvent's involved parties (most reliable check)
                var lordInvolvedInMapEvent = mapEvent.InvolvedParties?.Any(p => p?.MobileParty == lordParty) == true;

                // FIX: Only consider battle relevant if our LORD is actually involved, not just army members
                // Being in the same army doesn't mean we should join every random skirmish around us
                // During sieges, many small battles happen (looters vs villagers) that we should ignore
                var isRelevantBattle = lordIsAttacker || lordIsDefender || armyLeaderInvolved || lordInvolvedInMapEvent;

                // Early exit for unrelated battles - don't log to avoid spam from all map battles
                if (!isRelevantBattle)
                {
                    return;
                }

                // Only log detailed info for battles that actually involve our lord
                ModLogger.Info("Battle", "=== MapEventStarted - LORD INVOLVED ===");
                ModLogger.Info("Battle",
                    $"Lord: {lordParty.LeaderHero?.Name?.ToString() ?? "unknown"}, LordParty ID: {lordParty.StringId}");
                ModLogger.Info("Battle",
                    $"Attacker: {attackerParty?.MobileParty?.LeaderHero?.Name?.ToString() ?? attackerParty?.Name?.ToString() ?? "unknown"}");
                ModLogger.Info("Battle",
                    $"Defender: {defenderParty?.MobileParty?.LeaderHero?.Name?.ToString() ?? defenderParty?.Name?.ToString() ?? "unknown"}");
                ModLogger.Debug("Battle",
                    $"lordIsAttacker={lordIsAttacker}, lordIsDefender={lordIsDefender}, inArmy={inArmy}, armyLeaderInvolved={armyLeaderInvolved}");

                // Reset tracking flags for new battle
                _battleXPAwardedThisBattle = false;
                _playerEncounterCreatedForBattle = false;

                var isSiegeBattle = mapEvent.IsSiegeAssault || mapEvent.EventType == MapEvent.BattleTypes.Siege;

                ModLogger.Info("Battle",
                    $"Native battle detected (Siege: {isSiegeBattle}, InArmy: {inArmy}) - preparing player for vanilla flow");

                // Exit custom enlisted menus so the native system can push its own encounter/army menus
                var currentMenu = Campaign.Current?.CurrentMenuContext?.GameMenu?.StringId;
                
                if (!string.IsNullOrEmpty(currentMenu) && currentMenu.StartsWith("enlisted_") && !isSiegeBattle)
                {
                    var encounterMenuModel = Campaign.Current?.Models?.EncounterGameMenuModel;
                    var desiredMenu = encounterMenuModel?.GetGenericStateMenu();

                    if (!string.IsNullOrEmpty(desiredMenu) && desiredMenu != currentMenu)
                    {
                        ModLogger.Info("Battle",
                            $"Switching from enlisted menu to native menu '{desiredMenu}' for battle");
                        NextFrameDispatcher.RunNextFrame(() =>
                        {
                            try
                            {
                                GameMenu.SwitchToMenu(desiredMenu);
                            }
                            catch (Exception ex)
                            {
                                ModLogger.Error("Battle",
                                    $"Failed to switch to native menu '{desiredMenu}': {ex.Message}");
                            }
                        });
                    }
                    else
                    {
                        ModLogger.Debug("Battle",
                            "Native battle menu not ready yet - keeping enlisted menu until engine pushes one");
                    }
                }

                // CRITICAL: Teleport player party to lord's position IMMEDIATELY so encounter triggers instantly
                // Without this, the player's invisible party has to "travel" to the battle, causing a pause
                // while the game waits for the party to arrive before showing the encounter menu
                if (main.Position != lordParty.Position)
                {
                    var oldPos = main.GetPosition2D;
                    main.Position = lordParty.Position;
                    ModLogger.Info("Battle",
                        $"Teleported player party to battle location (from {oldPos} to {lordParty.GetPosition2D})");
                }

                EnsurePlayerSharesArmy(lordParty);
                PreparePartyForNativeBattle(main);

                // Keep escort behaviour hooked so we stay attached to the lord while the encounter progresses
                EncounterGuard.TryAttachOrEscort(_enlistedLord);
                TryReleaseEscort(main);

                // Force unpause the game so encounter appears immediately without manual intervention
                if (Campaign.Current?.TimeControlMode == CampaignTimeControlMode.Stop)
                {
                    Campaign.Current.TimeControlMode = CampaignTimeControlMode.StoppablePlay;
                    ModLogger.Debug("Battle", "Auto-unpaused game for seamless battle entry");
                }

                if (mapEvent != null)
                {
                    _cachedLordMapEvent = mapEvent;
                }

                LogPartyState("OnMapEventStarted");
            }
            catch (Exception ex)
            {
                ModLogger.Error("Battle", $"Error in BattleStarted handler: {ex.Message}");
            }
        }

        /// <summary>
        ///     Handle battle end events to return player to hidden state.
        /// </summary>
        private void OnMapEventEnded(MapEvent mapEvent)
        {
            try
            {
                var main = MobileParty.MainParty;

                // Reset PlayerEncounter guard flag - battle is ending
                _playerEncounterCreatedForBattle = false;
                
                // SAFETY: If the player is still a prisoner or capture cleanup is queued, avoid any post-battle
                // enlistment cleanup (army/visibility/menu) and let native captivity flow finalize first.
                if (Hero.MainHero.IsPrisoner || _playerCaptureCleanupScheduled)
                {
                    ModLogger.Info("EventSafety",
                        $"Skipping MapEventEnded - player prisoner or cleanup pending (IsPrisoner={Hero.MainHero.IsPrisoner}, CaptureCleanupScheduled={_playerCaptureCleanupScheduled})");
                    _cachedLordMapEvent = null;
                    return;
                }

                var effectiveMapEvent = mapEvent ?? _cachedLordMapEvent;

                var lordParty = _enlistedLord?.PartyBelongedTo;

                var playerParticipated = main?.Party.MapEvent == mapEvent ||
                                         effectiveMapEvent?.InvolvedParties?.Any(p => p?.MobileParty == main) == true;

                var lordParticipated = false;
                if (lordParty != null && effectiveMapEvent?.InvolvedParties != null)
                {
                    lordParticipated = effectiveMapEvent.InvolvedParties.Any(p => p?.MobileParty == lordParty);

                    if (!lordParticipated && lordParty.Army != null)
                    {
                        var army = lordParty.Army;
                        lordParticipated = effectiveMapEvent.InvolvedParties.Any(p =>
                            p?.MobileParty == army.LeaderParty ||
                            (army.Parties != null && army.Parties.Contains(p?.MobileParty)));
                    }
                }

                var attachedToLord = lordParty != null && main?.AttachedTo == lordParty;
                var shareArmyWithLord = lordParty?.Army != null && main?.Army == lordParty.Army;

                // Early exit for unrelated battles - don't log to avoid spam from all map battles
                if (!playerParticipated && !lordParticipated)
                {
                    _cachedLordMapEvent = null;
                    return;
                }

                // Only log diagnostics for battles that actually involved us
                LogPartyState("OnMapEventEnded-Start");

                // FIX: Proactive army state cleanup after naval battles
                // Native PlayerArmyWaitBehavior crashes when Army.LeaderParty is null but Army is not
                // This can happen after naval battles when the army state becomes inconsistent
                // We proactively clean up any invalid army references to prevent the crash
                ValidateAndCleanupArmyState(main, effectiveMapEvent?.IsNavalMapEvent == true);

                // CRITICAL: Check if enlistment ended during the battle (lord captured/army defeated)
                // Don't activate immediately - keep party inactive to prevent new encounters
                if (!IsEnlisted || _isOnLeave)
                {
                    // FIX: Only perform cleanup if we were actually enlisted when the battle started (tracked by _cachedLordMapEvent)
                    // If _cachedLordMapEvent is null, it means we were never enlisted for this battle (normal player scenario),
                    // so we must NOT interfere with the native encounter flow.
                    if (_cachedLordMapEvent == null)
                    {
                        return;
                    }

                    ModLogger.Info("Battle",
                        $"OnMapEventEnded early exit: IsEnlisted={IsEnlisted}, OnLeave={_isOnLeave}, main.HasMapEvent={main?.Party.MapEvent != null}, lordHasMapEvent={lordParty?.Party.MapEvent != null}, MapEventHasWinner={mapEvent?.HasWinner}");

                    // Enlistment ended during battle - cleanup army reference only
                    // StopEnlist already handled party activation state based on whether player was in battle
                    // Do NOT override IsActive/IsVisible here - that would revert StopEnlist's correct activation
                    // for wounded players who weren't in the battle
                    if (main?.Army != null)
                    {
                        main.Army = null;
                    }

                    if (PlayerEncounter.Current != null)
                    {
                        try
                        {
                            if (PlayerEncounter.InsideSettlement)
                            {
                                PlayerEncounter.LeaveSettlement();
                            }

                            PlayerEncounter.Finish(true);
                            ModLogger.Info("Battle", "Finished PlayerEncounter after enlistment ended");
                        }
                        catch (Exception ex)
                        {
                            ModLogger.Error("Battle",
                                $"Error finishing PlayerEncounter after enlistment ended: {ex.Message}");
                        }
                    }

                    ResetSiegePreparationLatch();
                    _cachedLordMapEvent = null;
                    if (effectiveMapEvent != null)
                    {
                        RestoreCampaignFlowAfterBattle();
                    }

                    LogPartyState("OnMapEventEnded-EarlyExit");
                    return;
                }

                if (_enlistedLord == null)
                {
                    return;
                }

                LogPartyState("OnMapEventEnded-After");

                lordParty = _enlistedLord.PartyBelongedTo;
                if (lordParty == null)
                {
                    return;
                }

                // NULL-SAFE: Check if this was our lord's battle that ended
                if (effectiveMapEvent?.InvolvedParties != null &&
                    effectiveMapEvent.InvolvedParties.Any(p => p?.MobileParty == lordParty ||
                                                               (p?.MobileParty?.Army != null &&
                                                                p.MobileParty.Army == lordParty.Army)))
                {
                    ModLogger.Info("Battle", "Lord battle ended, returning to hidden state");
                    _cachedLordMapEvent = null;

                    // Award battle XP only if player actually participated (not waiting in reserve)
                    // Players who chose "Wait in Reserve" opted out of combat and don't earn XP
                    if (!EnlistedEncounterBehavior.IsWaitingInReserve)
                    {
                        AwardBattleXP(playerParticipated);
                    }
                    else
                    {
                        ModLogger.Debug("Battle", "Skipping battle XP - player waited in reserve");
                    }

                    // CRITICAL: Clear siege encounter creation timestamp when battle ends
                    // This allows new encounters to be created if needed after the battle
                    _lastSiegeEncounterCreation = CampaignTime.Zero;

                    // CRITICAL: Defer finishing PlayerEncounter to next frame to avoid timing issues
                    // The game may still be using the encounter for score screens or settlement entry
                    // Finishing immediately after battle ends can cause crashes
                    if (TaleWorlds.CampaignSystem.Encounters.PlayerEncounter.Current != null)
                    {
                        NextFrameDispatcher.RunNextFrame(() =>
                        {
                            try
                            {
                                if (TaleWorlds.CampaignSystem.Encounters.PlayerEncounter.Current != null)
                                {
                                    if (TaleWorlds.CampaignSystem.Encounters.PlayerEncounter.InsideSettlement)
                                    {
                                        TaleWorlds.CampaignSystem.Encounters.PlayerEncounter.LeaveSettlement();
                                    }

                                    TaleWorlds.CampaignSystem.Encounters.PlayerEncounter.Finish(true);
                                    ModLogger.Info("Battle",
                                        "Finished PlayerEncounter after battle ended (deferred to next frame)");
                                }

                                // Return to hidden enlisted state after encounter is finished
                                var mainParty = MobileParty.MainParty;
                                if (mainParty != null && IsEnlisted)
                                {
                                    mainParty.IsActive = false;
                                    mainParty.IsVisible = false;
                                    ModLogger.Info("Battle",
                                        "Player returned to hidden enlisted state after encounter cleanup");
                                }

                                ResetSiegePreparationLatch();
                                RestoreCampaignFlowAfterBattle();
                            }
                            catch (Exception ex)
                            {
                                ModLogger.Error("Battle",
                                    $"Error finishing PlayerEncounter after battle: {ex.Message}");
                            }
                        });
                    }
                    else
                    {
                        // No encounter to finish - return to hidden enlisted state immediately
                        main.IsActive = false;
                        main.IsVisible = false;
                        ModLogger.Info("Battle", "Player returned to hidden enlisted state (no encounter to finish)");
                        ResetSiegePreparationLatch();
                        RestoreCampaignFlowAfterBattle();
                    }

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
        ///     Handles battle completion - awards battle XP, tracks kills, and updates term kill count.
        ///     This is the primary integration point for the kill tracker system.
        ///     CRITICAL: Only awards XP when battle actually ends with a winner - not when entering reserve mode.
        /// </summary>
        private void OnPlayerBattleEnd(MapEvent mapEvent)
        {
            try
            {
                ModLogger.Info("Battle", "Player battle ended");
                ModLogger.Info("Battle", $"Battle Type: {mapEvent?.EventType}, Was Siege: {mapEvent?.IsSiegeAssault}");

                // CRITICAL: Don't award XP if battle hasn't actually finished (no winner yet)
                // This prevents XP from being awarded when player clicks "Wait in Reserve"
                // which calls PlayerEncounter.Finish() but the battle is still ongoing
                if (mapEvent != null && !mapEvent.HasWinner)
                {
                    ModLogger.Debug("Battle", "Skipping battle rewards - battle still ongoing (no winner yet)");
                    return;
                }

                // CRITICAL: Don't award XP if player is entering "Wait in Reserve" mode
                // PlayerEncounter.Finish() triggers this event, but we're just entering reserve, not ending battle
                if (EnlistedEncounterBehavior.IsWaitingInReserve)
                {
                    ModLogger.Debug("Battle",
                        "Skipping battle rewards - player is entering reserve mode, not ending battle");
                    return;
                }

                // Only process if enlisted
                if (!IsEnlisted || _isOnLeave)
                {
                    ModLogger.Debug("Battle", "Skipping battle rewards - not enlisted or on leave");
                    return;
                }

                // Get kill count from tracker (if available)
                var killsThisBattle = 0;
                var participated = false;

                var killTracker = EnlistedKillTrackerBehavior.Instance;
                if (killTracker != null)
                {
                    killsThisBattle = killTracker.GetAndResetKillCount();
                    participated = killTracker.GetAndResetParticipation();
                    ModLogger.Info("Battle", $"Kill tracker: {killsThisBattle} kills, participated: {participated}");
                }
                else
                {
                    // Fallback: assume participation if we got to this event
                    // BUGFIX: Don't assume participation if morale is too low to fight (prevents XP spam loop)
                    if (MobileParty.MainParty.Morale <= 1f)
                    {
                        participated = false;
                        ModLogger.Debug("Battle", "Player morale too low for combat - participation denied");
                    }
                    else
                    {
                        participated = true;
                        ModLogger.Debug("Battle", "Kill tracker not available - assuming participation");
                    }
                }

                // Award battle XP if player participated
                if (participated)
                {
                    AwardBattleXP(killsThisBattle);
                }

                // Add kills to current term total (persists to faction record on retirement)
                if (killsThisBattle > 0)
                {
                    _currentTermKills += killsThisBattle;
                    ModLogger.Info("Battle",
                        $"Term kills updated: +{killsThisBattle} = {_currentTermKills} total this term");
                    
                    // Update Service Records for Command Tent display
                    ServiceRecordManager.Instance?.OnKillsRecorded(killsThisBattle);
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("Battle", $"Error in OnPlayerBattleEnd: {ex.Message}");
            }
        }

        /// <summary>
        ///     Awards XP for battle participation and kills (called from OnPlayerBattleEnd).
        ///     XP values are loaded from progression_config.json.
        /// </summary>
        private void AwardBattleXP(int kills)
        {
            try
            {
                // Don't award XP if player is waiting in reserve - they're sitting out all battles
                if (EnlistedEncounterBehavior.IsWaitingInReserve)
                {
                    ModLogger.Debug("Battle", "Skipping XP award (kills) - player is waiting in reserve");
                    return;
                }

                // Prevent double XP awards
                if (_battleXPAwardedThisBattle)
                {
                    ModLogger.Debug("Battle", "Skipping XP award - already awarded this battle");
                    return;
                }

                // Load XP values from config
                var battleParticipationXP = EnlistedConfig.GetBattleParticipationXp();
                var xpPerKill = EnlistedConfig.GetXpPerKill();

                // Award participation XP (flat bonus for being in battle)
                if (battleParticipationXP > 0)
                {
                    AddEnlistmentXP(battleParticipationXP, "Battle Participation");
                }

                // Award kill XP (bonus per enemy killed)
                var killXP = kills * xpPerKill;
                if (killXP > 0)
                {
                    AddEnlistmentXP(killXP, $"Combat Kills ({kills})");
                }

                // Mark XP as awarded to prevent double awards from OnMapEventEnded
                _battleXPAwardedThisBattle = true;

                ModLogger.Info("Battle",
                    $"Battle XP awarded: {battleParticipationXP} (participation) + {killXP} (kills) = {battleParticipationXP + killXP} total");
            }
            catch (Exception ex)
            {
                ModLogger.Error("Battle", $"Error awarding battle XP: {ex.Message}");
            }
        }

        /// <summary>
        ///     Siege watchdog to prepare party for vanilla encounter creation when siege begins.
        ///     This is the missing piece - making party encounter-eligible so vanilla siege menus can appear.
        /// </summary>
        private void SiegeWatchdogTick(MobileParty mainParty, MobileParty lordParty)
        {
            try
            {
                if (mainParty == null || lordParty == null)
                {
                    return;
                }

                // This is the pre-assault state: arriving / laying siege
                var currentSiegeSettlement =
                    lordParty.BesiegedSettlement ?? lordParty.Party.SiegeEvent?.BesiegedSettlement;
                bool siegeForming = currentSiegeSettlement != null;

                if (!siegeForming)
                {
                    ResetSiegePreparationLatch();
                    return;
                }

                // If we're already in an encounter/menu, do nothing
                if (PlayerEncounter.Current != null || mainParty.Party.MapEvent != null)
                {
                    return;
                }

                // If we've already prepared for this siege target, skip further work until the siege state changes.
                if (_isSiegePreparationLatched && _latchedSiegeSettlement == currentSiegeSettlement)
                {
                    return;
                }

                // CRITICAL: Throttle this watchdog to prevent rapid execution that causes zero-delta-time assertions
                // The realtime tick runs every frame, so we need to limit how often this runs
                // Only run once per 2 seconds to prevent assertion failures (increased from 1 second)
                bool enoughTimePassed = CampaignTime.Now - _lastSiegeEncounterCreation >= CampaignTime.Seconds(2L);
                if (!enoughTimePassed)
                {
                    return; // Skip if called too recently
                }

                ModLogger.Info("Siege",
                    $"Siege detected at {lordParty.BesiegedSettlement?.Name?.ToString() ?? "unknown"}");
                LogPartyState("SiegeWatchdog-Triggered");

                // Prepare for vanilla to collect us
                TryReleaseEscort(mainParty);
                mainParty.IgnoreByOtherPartiesTill(CampaignTime.Now); // clear ignore window
                // CRITICAL: Do NOT set position directly - causes teleportation and assertion failures
                // Escort AI (SetMoveEscortParty) handles position syncing
                // mainParty.Position2D = lordParty.Position2D;  // REMOVED - causes teleportation

                // Note: IsVisible and IsActive are already set by the siege handling code above
                // We just need to ensure the party can be encountered by vanilla
                TrySetShouldJoinPlayerBattles(mainParty, true);

                // Update timestamp to prevent rapid execution
                _lastSiegeEncounterCreation = CampaignTime.Now;
                _isSiegePreparationLatched = true;
                _latchedSiegeSettlement = currentSiegeSettlement;

                // IMPORTANT: do NOT push our own menu here. Let vanilla push army_wait / siege menu.
                ModLogger.Info("Siege",
                    "Prepared player for siege encounter (active/visible & co-located) - vanilla should create encounter menu");
            }
            catch (Exception ex)
            {
                ModLogger.Error("Siege", $"Error in siege watchdog: {ex.Message}");
            }
        }

        private void ResetSiegePreparationLatch()
        {
            _isSiegePreparationLatched = false;
            _latchedSiegeSettlement = null;
        }

        private bool TryCapturePlayerAlongsideLord(PartyBase capturingParty)
        {
            if (!IsEnlisted || _isOnLeave || Hero.MainHero.IsPrisoner)
            {
                return false;
            }

            if (capturingParty == null)
            {
                ModLogger.Info("Battle",
                    "Lord capture detected but no captor party was provided to mirror capture for player");
                return false;
            }

            if (PlayerEncounter.Current != null)
            {
                // Player is already in an encounter (e.g., choosing Surrender). Native flow will capture them.
                ModLogger.Info("Battle",
                    "Encounter active during lord capture - letting native surrender capture handle the player.");
                return false;
            }

            try
            {
                TakePrisonerAction.Apply(capturingParty, Hero.MainHero);
                ModLogger.Info("Battle", $"Mirrored player capture after lord capture - captor: {capturingParty.Name}");
                return true;
            }
            catch (Exception ex)
            {
                ModLogger.Error("Battle", $"Error mirroring capture after lord capture: {ex.Message}");
                return false;
            }
        }

        private void SchedulePostEncounterVisibilityRestore()
        {
            if (_pendingVisibilityRestore)
            {
                return;
            }

            _pendingVisibilityRestore = true;
            NextFrameDispatcher.RunNextFrame(RestoreVisibilityAfterEncounter, true);
        }

        private void RestoreVisibilityAfterEncounter()
        {
            if (!_pendingVisibilityRestore)
            {
                return;
            }

            var mainParty = MobileParty.MainParty;
            if (mainParty == null)
            {
                _pendingVisibilityRestore = false;
                return;
            }

            if (PlayerEncounter.Current != null || mainParty.Party?.MapEvent != null)
            {
                NextFrameDispatcher.RunNextFrame(RestoreVisibilityAfterEncounter, true);
                return;
            }

            _pendingVisibilityRestore = false;

            // CRITICAL: If in grace period, apply protection BEFORE making visible
            // This prevents enemies from immediately attacking the player after discharge
            if (IsInDesertionGracePeriod)
            {
                var protectionUntil = CampaignTime.Now + CampaignTime.Days(1f);
                mainParty.IgnoreByOtherPartiesTill(protectionUntil);
                _graceProtectionEnds = protectionUntil;
                ModLogger.Info("Enlistment",
                    $"Applied grace protection during visibility restore (until {protectionUntil})");
            }

            if (!mainParty.IsActive)
            {
                mainParty.IsActive = true;
            }

            if (!mainParty.IsVisible)
            {
                mainParty.IsVisible = true;
            }

            ModLogger.Info("Enlistment", "Party visibility restored after encounter cleanup");
            ForceFinishLingeringEncounter("VisibilityRestore");
        }

        private void SchedulePlayerCaptureCleanup(Kingdom lordKingdom)
        {
            _pendingPlayerCaptureKingdom = lordKingdom;
            _pendingPlayerCaptureReason = lordKingdom != null ? "Player captured" : "Player captured (No Kingdom)";

            if (_playerCaptureCleanupScheduled)
            {
                return;
            }

            _playerCaptureCleanupScheduled = true;
            NextFrameDispatcher.RunNextFrame(FinalizePlayerCaptureCleanup, true);
        }

        private void FinalizePlayerCaptureCleanup()
        {
            if (!_playerCaptureCleanupScheduled)
            {
                return;
            }

            var mainParty = MobileParty.MainParty;
            if (PlayerEncounter.Current != null || mainParty?.Party?.MapEvent != null)
            {
                NextFrameDispatcher.RunNextFrame(FinalizePlayerCaptureCleanup, true);
                return;
            }

            _playerCaptureCleanupScheduled = false;

            var reason = _pendingPlayerCaptureReason ?? "Player captured";
            var kingdom = _pendingPlayerCaptureKingdom;
            _pendingPlayerCaptureReason = null;
            _pendingPlayerCaptureKingdom = null;

            if (!IsEnlisted && !_isOnLeave && !IsInDesertionGracePeriod)
            {
                return;
            }

            ModLogger.Info("EventSafety", "Finalizing deferred capture cleanup for player");

            if (kingdom != null)
            {
                StopEnlist(reason, false, true);
                StartDesertionGracePeriod(kingdom);
                GrantGracePeriodInteractionWindow();
                ForceFinishLingeringEncounter("CaptureCleanup");
            }
            else
            {
                StopEnlist("Player captured (No Kingdom)");
                GrantGracePeriodInteractionWindow();
                ForceFinishLingeringEncounter("CaptureCleanup");
            }
        }

        /// <summary>
        ///     Gives the player a short invulnerability window after discharge/capture so they can interact with NPCs.
        ///     Keeps the party active/visible but ignored by hostile AI for one in-game hour.
        /// </summary>
        private void GrantGracePeriodInteractionWindow()
        {
            try
            {
                var main = MobileParty.MainParty;
                if (main == null)
                {
                    return;
                }

                // Skip if the game still has an encounter/battle running
                if (main.Party?.MapEvent != null || PlayerEncounter.Current != null)
                {
                    ModLogger.Debug("GraceProtection", "Delaying interaction window - still in encounter/battle state");
                    NextFrameDispatcher.RunNextFrame(GrantGracePeriodInteractionWindow, true);
                    return;
                }

                var protectionUntil = CampaignTime.Now + CampaignTime.Days(1f);
                main.IgnoreByOtherPartiesTill(protectionUntil);
                _graceProtectionEnds = protectionUntil;

                if (!main.IsActive)
                {
                    main.IsActive = true;
                }

                main.IsVisible = true;
                ModLogger.Info("GraceProtection",
                    $"Granted 1-day protection window after discharge (ignored until {protectionUntil})");
                ForceFinishLingeringEncounter("GraceProtection");
            }
            catch (Exception ex)
            {
                ModLogger.Error("GraceProtection", $"Failed to grant grace period interaction window: {ex.Message}");
            }
        }

        private void RestoreCampaignFlowAfterBattle()
        {
            try
            {
                var campaign = Campaign.Current;
                if (campaign == null)
                {
                    return;
                }

                if (campaign.TimeControlMode == CampaignTimeControlMode.Stop)
                {
                    campaign.TimeControlMode = CampaignTimeControlMode.StoppablePlay;
                    ModLogger.Debug("Battle", "Restored campaign time control to StoppablePlay");
                }

                var menuContext = campaign.CurrentMenuContext;
                var currentMenuId = menuContext?.GameMenu?.StringId;
                if (!string.IsNullOrEmpty(currentMenuId) &&
                    (currentMenuId == "army_wait" ||
                     currentMenuId.Contains("siege") ||
                     currentMenuId.Contains("encounter")))
                {
                    GameMenu.ExitToLast();
                    ModLogger.Debug("Battle", $"Exited lingering menu after battle ({currentMenuId})");
                }

                // Handle menu restoration after battle ends
                // Use NextFrameDispatcher to avoid race conditions with menu state
                NextFrameDispatcher.RunNextFrame(() =>
                {
                    if (IsEnlisted && !_isOnLeave)
                    {
                        EnlistedMenuBehavior.SafeActivateEnlistedMenu();
                        ModLogger.Debug("Battle", "Activated enlisted_status menu after battle");
                    }
                    else
                    {
                        // Discharged during battle - ensure party is properly activated
                        // StopEnlist handles this, but as a safety net verify state is correct
                        var mainParty = MobileParty.MainParty;
                        if (mainParty != null && !mainParty.IsActive)
                        {
                            mainParty.IsActive = true;
                            mainParty.IsVisible = true;
                            ModLogger.Info("Battle", "Safety recovery: activated party after discharge-during-battle");
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                ModLogger.Error("Battle", $"Error restoring campaign flow after battle: {ex.Message}");
            }
        }

        private void ForceFinishLingeringEncounter(string context)
        {
            try
            {
                if (PlayerEncounter.Current == null)
                {
                    return;
                }

                PlayerEncounter.LeaveEncounter = true;

                if (PlayerEncounter.InsideSettlement)
                {
                    PlayerEncounter.LeaveSettlement();
                }

                var menuContext = Campaign.Current?.CurrentMenuContext;
                var currentMenuId = menuContext?.GameMenu?.StringId;
                if (!string.IsNullOrEmpty(currentMenuId) && currentMenuId.Contains("encounter"))
                {
                    GameMenu.ExitToLast();
                }

                ModLogger.Info("EncounterCleanup", $"Requested encounter exit after {context}");
            }
            catch (Exception ex)
            {
                ModLogger.Error("EncounterCleanup",
                    $"Failed to finish lingering PlayerEncounter after {context}: {ex.Message}");
            }
        }

        #endregion

        #region Diagnostics

        public bool IsEmbeddedWithLord()
        {
            if (!IsEnlisted || _enlistedLord == null)
            {
                return false;
            }

            var main = MobileParty.MainParty;
            var lordParty = _enlistedLord.PartyBelongedTo;
            if (main == null || lordParty == null)
            {
                return false;
            }

            // Check if in same army or actively following via Escort AI
            // NOTE: We use TargetParty instead of AttachedTo because AttachedTo crashes
            // GetGenericStateMenu() when the player isn't in an army
            var sameArmy = main.Army != null && lordParty.Army != null && main.Army == lordParty.Army;
            bool following = main.TargetParty == lordParty;
            return sameArmy || following;
        }

        public bool IsPartyInActiveSiege(MobileParty party)
        {
            if (party == null)
            {
                return false;
            }

            try
            {
                if (party.Party?.SiegeEvent != null)
                {
                    return true;
                }

                if (party.BesiegerCamp != null && party.BesiegerCamp.SiegeEvent != null)
                {
                    return true;
                }

                var siegeEvent = party.BesiegedSettlement?.SiegeEvent;
                if (siegeEvent != null && party.Party != null)
                {
                    return siegeEvent.IsPartyInvolved(party.Party);
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("Diagnostics",
                    $"Error evaluating siege state for {party?.LeaderHero?.Name?.ToString() ?? "unknown"}: {ex.Message}");
            }

            return false;
        }

        /// <summary>
        /// Validates and cleans up potentially invalid army state to prevent native game crashes.
        /// The native PlayerArmyWaitBehavior crashes when Army is not null but LeaderParty is null,
        /// which can happen after naval battles when the army state becomes inconsistent.
        /// This proactive cleanup prevents the crash by removing invalid army references.
        /// </summary>
        /// <param name="mainParty">The player's main party to check.</param>
        /// <param name="wasNavalBattle">Whether the battle that just ended was a naval battle.</param>
        private void ValidateAndCleanupArmyState(MobileParty mainParty, bool wasNavalBattle)
        {
            if (mainParty == null)
            {
                return;
            }

            try
            {
                var army = mainParty.Army;
                if (army == null)
                {
                    return;
                }

                // Check for invalid army state: Army exists but LeaderParty is null or invalid
                // This is the exact crash condition in PlayerArmyWaitBehavior
                var leaderParty = army.LeaderParty;
                if (leaderParty == null)
                {
                    // Invalid state - remove the army reference to prevent crash
                    mainParty.Army = null;
                    ModLogger.Info("Naval",
                        "Cleaned up invalid army state after battle (LeaderParty was null). " +
                        "This prevents a native game crash in PlayerArmyWaitBehavior.");
                    return;
                }

                // Additional validation for naval battles - check if the army is still coherent
                if (wasNavalBattle)
                {
                    // After naval battles, verify the army leader is still valid and alive
                    var leaderHero = leaderParty.LeaderHero;
                    if (leaderHero == null || !leaderHero.IsAlive)
                    {
                        // Leader is gone - army should have been disbanded but wasn't
                        mainParty.Army = null;
                        ModLogger.Info("Naval",
                            "Cleaned up army reference after naval battle (leader deceased or missing). " +
                            "This prevents potential crashes.");
                        return;
                    }

                    // Check if the army is at sea but has no sea capability
                    // This can cause issues with the army_wait menu trying to access sea state
                    if (leaderParty.IsCurrentlyAtSea && !leaderParty.HasNavalNavigationCapability)
                    {
                        ModLogger.Warn("Naval",
                            "Army leader is at sea without naval capability after battle - " +
                            "native game may have state issues");
                        // Don't remove army here - just log for diagnostics
                    }

                    ModLogger.Debug("Naval",
                        $"Post-naval battle army state validated: Leader={leaderHero.Name}, " +
                        $"AtSea={leaderParty.IsCurrentlyAtSea}, ArmySize={army.Parties?.Count ?? 0}");
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("Naval", $"Error validating army state: {ex.Message}");
                
                // On error, try to clean up anyway to prevent cascading crashes
                try
                {
                    mainParty.Army = null;
                    ModLogger.Info("Naval", "Forcibly cleared army reference after validation error");
                }
                catch
                {
                    // Can't even clear - leave it as is
                }
            }
        }

        /// <summary>
        ///     Log party state for diagnostics (Debug level only - not verbose in production).
        ///     Only logs on critical state transitions, not every frame.
        /// </summary>
        private void LogPartyState(string context)
        {
            try
            {
                var main = MobileParty.MainParty;
                var lordParty = _enlistedLord?.PartyBelongedTo;
                if (main == null)
                {
                    ModLogger.Debug("Diagnostics", $"{context}: Main party null");
                    return;
                }

                // Lightweight state summary - only key flags, not full details
                var info =
                    $"{context}: Active={main.IsActive}, Visible={main.IsVisible}, " +
                    $"InBattle={main.Party.MapEvent != null}, InSiege={main.Party.SiegeEvent != null}, " +
                    $"LordInBattle={lordParty?.Party.MapEvent != null}";
                ModLogger.Debug("Diagnostics", info);
            }
            catch (Exception ex)
            {
                ModLogger.Error("Diagnostics", $"Error logging party state: {ex.Message}");
            }
        }

        /// <summary>
        ///     Called after a mission (battle) starts.
        ///     Note: We no longer add mission behaviors dynamically as this can cause crashes.
        ///     Kill tracking is now handled via campaign events instead.
        /// </summary>
        private void OnAfterMissionStarted(IMission mission)
        {
            try
            {
                // Only process if player is enlisted and not on leave
                if (!IsEnlisted || _isOnLeave)
                {
                    return;
                }

                // DISABLED: Adding MissionBehaviors dynamically during AfterMissionStarted
                // causes crashes because the mission lifecycle methods get called in an unstable state.
                // Instead, we track participation via campaign events (OnMapEventEnded, OnPlayerBattleEnd).
                // Kill XP is calculated based on the native kill counter when available.

                ModLogger.Debug("Battle", "Mission started while enlisted - using campaign event tracking");
            }
            catch (Exception ex)
            {
                ModLogger.Error("Battle", $"Error in OnAfterMissionStarted: {ex.Message}");
            }
        }

        #endregion
    }

    /// <summary>
    ///     Tracks veteran service history for a specific kingdom/faction.
    ///     Persists across multiple enlistment terms and cooldown periods.
    /// </summary>
    [Serializable]
    public class FactionVeteranRecord
    {
        /// <summary>
        ///     Whether the player has completed the initial 3-year (252 day) term with this faction.
        ///     First term completion unlocks full retirement benefits.
        /// </summary>
        public bool FirstTermCompleted { get; set; }

        /// <summary>
        ///     Preserved military tier from last service. Restored on re-enlistment after cooldown.
        /// </summary>
        public int PreservedTier { get; set; } = 1;

        /// <summary>
        ///     Total kills accumulated across all service terms with this faction.
        /// </summary>
        public int TotalKills { get; set; }

        /// <summary>
        ///     Campaign time when the 6-month cooldown period ends.
        ///     Player can re-enlist after this time.
        /// </summary>
        public CampaignTime CooldownEnds { get; set; } = CampaignTime.Zero;

        /// <summary>
        ///     Campaign time when the current service term ends.
        ///     For first term: 252 days. For renewal terms: 84 days (1 Bannerlord year).
        /// </summary>
        public CampaignTime CurrentTermEnd { get; set; } = CampaignTime.Zero;

        /// <summary>
        ///     Whether the player is currently in a renewal term (post-first-term service).
        ///     Renewal terms are 1 Bannerlord year (84 days) and offer 5,000g bonus/discharge.
        /// </summary>
        public bool IsInRenewalTerm { get; set; }

        /// <summary>
        ///     Number of completed renewal terms after the first full term.
        /// </summary>
        public int RenewalTermsCompleted { get; set; }
    }
}
