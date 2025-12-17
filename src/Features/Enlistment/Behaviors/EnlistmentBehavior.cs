using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Enlisted.Features.Assignments.Behaviors;
using Enlisted.Features.CommandTent.Core;
using Enlisted.Features.Assignments.Core;
using Enlisted.Features.Combat.Behaviors;
using Enlisted.Features.Escalation;
using Enlisted.Features.Equipment.Behaviors;
using Enlisted.Features.Interface.Behaviors;
using Enlisted.Features.Lances.Leaders;
using Enlisted.Mod.Core;
using Enlisted.Mod.Core.Logging;
using Enlisted.Mod.Entry;
using Enlisted.Mod.GameAdapters.Patches;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
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
using Helpers;
using TaleWorlds.ObjectSystem;
using EnlistedConfig = Enlisted.Features.Assignments.Core.ConfigurationManager;
using EnlistedIncidentsBehavior = Enlisted.Features.Enlistment.Behaviors.EnlistedIncidentsBehavior;

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
        private const string SaveKeyPrefix = "Enlisted.EnlistmentBehavior.";

        private static string Key(string legacyKey) => $"{SaveKeyPrefix}{legacyKey}";

        private static void SyncKey<T>(IDataStore dataStore, string legacyKey, ref T value)
        {
            // On load: try legacy key first (for backwards compatibility), then prefixed key.
            // On save: only write prefixed keys to avoid collisions with other behaviors.
            if (dataStore == null || string.IsNullOrWhiteSpace(legacyKey))
            {
                return;
            }

            if (dataStore.IsLoading)
            {
                dataStore.SyncData(legacyKey, ref value);
            }

            dataStore.SyncData(Key(legacyKey), ref value);
        }
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

        /// <summary>
        ///     Phase 7: Date of last promotion (or enlistment if T1). Used to calculate days in rank.
        /// </summary>
        private CampaignTime _lastPromotionDate = CampaignTime.Zero;

        /// <summary>
        ///     Phase 7: Number of battles survived since enlistment. Used for promotion requirements.
        /// </summary>
        private int _battlesSurvived;

        /// <summary>
        ///     Phase 7: Number of Lance Life events completed. Used for promotion requirements.
        /// </summary>
        private int _eventsCompleted;

        // Phase 4: promotion can be temporarily blocked at very high discipline risk.
        // This is rate-limited to avoid message spam when earning XP while blocked.
        private CampaignTime _lastPromotionBlockedMessageTime = CampaignTime.Zero;

        // UI anti-spam: wage breakdown is used by tooltips and can be queried frequently.
        // If it ever throws, log it once per session and fall back to a safe default breakdown.
        private bool _loggedWageBreakdownFailure;

        private CampaignTime _graceProtectionEnds = CampaignTime.Zero;

        /// <summary>
        ///     Whether the player's personal equipment has been backed up before enlistment.
        ///     Equipment is backed up once at the start of service and restored when service ends.
        /// </summary>
        private bool _hasBackedUpEquipment;

        // Deferred bag check scheduling (avoids blocking enlistment flow)
        private bool _bagCheckScheduled;
        private CampaignTime _bagCheckDueTime = CampaignTime.Zero;

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
        ///     Tracks whether the player was a prisoner on the previous tick.
        ///     Used to detect captivity release and apply protection when transitioning
        ///     from prisoner to free during the grace period.
        /// </summary>
        private bool _wasPrisonerLastTick;

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
        ///     Guard flag to indicate we're processing an intentional discharge (retirement/honorable discharge).
        ///     Prevents OnClanChangedKingdom from treating the kingdom removal as desertion when the player
        ///     is legitimately ending their service through proper channels like retirement.
        /// </summary>
        private bool _isProcessingDischarge;

        // Captivity settlement log throttle to avoid spamming every hop
        private static readonly HashSet<string> _captivitySettlementsLogged = new HashSet<string>();

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
        private CampaignTime _pendingVisibilityRestoreStartTime = CampaignTime.Zero;

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

        // Baggage train stash for enlistment storage
        private ItemRoster _baggageStash = new ItemRoster();
        private bool _bagCheckCompleted;
        private bool _bagCheckInProgress;
        private bool _bagCheckEverCompleted;
        private Hero _pendingBagCheckLord;

        // Lance assignment state
        private string _currentLanceId;
        private string _currentLanceName;
        private string _currentLanceStyle;
        private string _currentLanceStoryId;
        private bool _isLanceProvisional;
        private string _manualLanceStyleId;
        private bool _isLanceLegacy;
        private bool _savedGraceLanceLegacy;

        // Camp/Fatigue flavor hooks (Phase 6)
        private int _fatigueCurrent = 24;
        private int _fatigueMax = 24;

        // Enhanced Fatigue System (Phase 1 - 24-point budget with health penalties)
        // _fatigueCurrent represents REMAINING fatigue points (24 = fresh, 0 = exhausted)
        // Health penalties trigger at LOW fatigue (8 or less remaining)
        private CampaignTime _lastFatigueRecoveryTime = CampaignTime.Zero;
        private float _healthBeforeExhaustion = -1f;
        private float _accumulatedFatigueRecovery = 0f; // Fractional recovery accumulator

        private bool _playerCaptureCleanupScheduled;
        private CampaignTime _savedGraceEnlistmentDate = CampaignTime.Zero;
        private Hero _savedGraceLord;
        private int _savedGraceTier = -1;
        private string _savedGraceTroopId;
        private int _savedGraceXP;

        // Lance state preserved during grace period transfers
        private string _savedGraceLanceId;
        private string _savedGraceLanceName;
        private string _savedGraceLanceStyle;
        private string _savedGraceLanceStoryId;
        private bool _savedGraceLanceProvisional;
        private string _savedGraceManualLanceStyleId;

        // Ledger-based pay scheduling (custom muster system)
        private int _pendingMusterPay;
        private CampaignTime _nextPayday = CampaignTime.Zero;
        private bool _payMusterPending;
        private string _lastPayOutcome;
        private int _pensionAmountPerDay;
        private string _pensionFactionId;
        private bool _isPensionPaused;
        private bool _isPendingDischarge;
        private string _lastDischargeBand;
        private bool _isOnProbation;
        private CampaignTime _probationEnds = CampaignTime.Zero;

        // Pay tension and backpay tracking (Phase 1 Pay System)
        // PayTension escalates when pay is delayed; triggers events at thresholds
        private int _payTension;
        private int _owedBackpay;
        private CampaignTime _lastPayDate = CampaignTime.Zero;
        private int _consecutiveDelays;
        private int _lanceFundBalance;

        /// <summary>
        ///     Currently selected duty assignment ID (e.g., "runner", "scout", "field_medic").
        ///     Duties provide daily skill XP bonuses and may include wage multipliers.
        ///     Changed via the duty selection menu.
        /// </summary>
        private string _selectedDuty = "runner";

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

        // ========================================================================
        // QUARTERMASTER HERO SYSTEM (Phase 3)
        // Persistent NPC quartermaster with personality archetype
        // ========================================================================

        /// <summary>
        ///     The quartermaster Hero assigned to the current enlisted lord.
        ///     Created on first quartermaster access, persists for duration of service.
        ///     Each lord has their own unique quartermaster.
        /// </summary>
        private Hero _quartermasterHero;

        /// <summary>
        ///     Personality archetype for the quartermaster.
        ///     Determines dialogue style and special interactions.
        ///     Values: "veteran", "merchant", "bookkeeper", "scoundrel", "believer", "eccentric"
        /// </summary>
        private string _quartermasterArchetype;

        /// <summary>
        ///     Relationship level with quartermaster (0-100).
        ///     Affects discounts, dialogue options, and special favors.
        ///     Separate from lord relationship.
        /// </summary>
        private int _quartermasterRelationship;

        /// <summary>
        ///     Whether this is the first meeting with the current quartermaster.
        ///     Used to trigger introduction dialogue on first visit.
        /// </summary>
        private bool _hasMetQuartermaster;

        // ========================================================================
        // FOOD/RATIONS SYSTEM (Phase 5)
        // Allows player to purchase better rations for morale and fatigue bonuses
        // ========================================================================

        /// <summary>
        ///     Current food quality tier affecting morale and fatigue recovery.
        ///     Standard (0) = no bonus, Supplemental (1) = +2 morale, Officer (2) = +4 morale +2 fatigue,
        ///     Commander (3) = +8 morale +5 fatigue.
        /// </summary>
        private int _currentFoodQuality;

        /// <summary>
        ///     When the current food quality bonus expires.
        ///     Rations are typically purchased for 1-3 days at a time.
        /// </summary>
        private CampaignTime _foodQualityExpires = CampaignTime.Zero;

        // ========================================================================
        // RETINUE PROVISIONING SYSTEM (Phase 6)
        // T7-T9 commanders must provision their retinue weekly
        // ========================================================================

        /// <summary>
        ///     Current provisioning tier for the retinue.
        ///     Affects retinue morale and combat effectiveness.
        /// </summary>
        private int _retinueProvisioningTier;

        /// <summary>
        ///     When the current retinue provisioning expires.
        ///     Provisioning is purchased for 7 days (1 week) at a time.
        /// </summary>
        private CampaignTime _retinueProvisioningExpires = CampaignTime.Zero;

        /// <summary>
        ///     Whether the player has been warned about low provisions (2 days remaining).
        ///     Resets when new provisions are purchased.
        /// </summary>
        private bool _retinueProvisioningWarningShown;

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
        ///     Phase 7: Days since last promotion (or enlistment for T1).
        /// </summary>
        public int DaysInRank
        {
            get
            {
                if (_lastPromotionDate == CampaignTime.Zero)
                {
                    // Fall back to enlistment date if no promotion recorded
                    return _enlistmentDate != CampaignTime.Zero 
                        ? (int)(CampaignTime.Now - _enlistmentDate).ToDays 
                        : 0;
                }
                return (int)(CampaignTime.Now - _lastPromotionDate).ToDays;
            }
        }

        /// <summary>
        ///     Phase 7: Number of battles survived since enlistment.
        /// </summary>
        public int BattlesSurvived => _battlesSurvived;

        /// <summary>
        ///     Phase 7: Number of Lance Life events completed since enlistment.
        /// </summary>
        public int EventsCompleted => _eventsCompleted;

        /// <summary>
        ///     Currently assigned duty (e.g., "runner", "scout", "field_medic").
        /// </summary>
        public string SelectedDuty => _selectedDuty;

        /// <summary>
        ///     Current lance identifiers for UI/story systems.
        /// </summary>
        public string CurrentLanceId => _currentLanceId ?? string.Empty;

        public string CurrentLanceName => _currentLanceName ?? string.Empty;

        public string CurrentLanceStyle => _currentLanceStyle ?? string.Empty;

        public string CurrentLanceStoryId => _currentLanceStoryId ?? string.Empty;

        public string ManualLanceStyleId => _manualLanceStyleId ?? string.Empty;

        public bool IsLanceProvisional => _isLanceProvisional;
        public bool IsLanceLegacy => _isLanceLegacy;
        public int PendingMusterPay => _pendingMusterPay;
        public CampaignTime NextPaydaySafe => _nextPayday != CampaignTime.Zero ? _nextPayday : (_nextPayday = ComputeNextPayday());
        public string LastPayOutcome => _lastPayOutcome ?? "none";
        public bool IsPayMusterPending => _payMusterPending;
        
        // Pay tension properties (Phase 1 Pay System)
        /// <summary>Pay tension level (0-100). Escalates when pay is delayed, triggers events at thresholds.</summary>
        public int PayTension => _payTension;
        /// <summary>Accumulated unpaid wages when lord can't afford to pay.</summary>
        public int OwedBackpay => _owedBackpay;
        /// <summary>Days since last successful pay. Used for pay delay detection.</summary>
        public int DaysSincePay => _lastPayDate == CampaignTime.Zero ? 0 : (int)(CampaignTime.Now - _lastPayDate).ToDays;
        /// <summary>True if pay is overdue (more than 7 days since last pay).</summary>
        public bool IsPayOverdue => DaysSincePay > 7 && IsEnlisted;
        /// <summary>Lance communal fund balance (5% deduction from pay).</summary>
        public int LanceFundBalance => _lanceFundBalance;
        
        public int PensionAmountPerDay => _pensionAmountPerDay;
        public bool IsPensionPaused => _isPensionPaused;
        public bool IsPendingDischarge => _isPendingDischarge;
        public string LastDischargeBand => _lastDischargeBand ?? "none";
        public bool IsOnProbation => _isOnProbation;

        /// <summary>
        ///     Current remaining fatigue points for camp actions.
        ///     24 = fully rested, 0 = completely exhausted.
        ///     Health penalties apply at 8 or below (exhausted) and 0 (severe exhaustion).
        /// </summary>
        public int FatigueCurrent => _fatigueCurrent;

        /// <summary>
        ///     Maximum fatigue points available (may be reduced by probation).
        /// </summary>
        public int FatigueMax => _fatigueMax;

        /// <summary>
        ///     Returns true if the player is in an exhausted state (low fatigue, health penalties active).
        /// </summary>
        public bool IsExhausted => _fatigueCurrent <= 8;

        /// <summary>
        ///     Returns true if the player is severely exhausted (critical fatigue level).
        /// </summary>
        public bool IsSeverelyExhausted => _fatigueCurrent <= 0;

        /// <summary>
        ///     Gets the fatigue percentage remaining (100% = fresh, 0% = exhausted).
        ///     Useful for UI progress bars.
        /// </summary>
        public float FatiguePercentage => _fatigueMax > 0 ? (float)_fatigueCurrent / _fatigueMax * 100f : 0f;

        /// <summary>
        ///     Gets a localized fatigue status string for UI display.
        /// </summary>
        public string FatigueStatusText
        {
            get
            {
                if (_fatigueCurrent <= 0) return "Severely Exhausted";
                if (_fatigueCurrent <= 8) return "Exhausted";
                if (_fatigueCurrent <= 16) return "Tired";
                return "Rested";
            }
        }

        // ========================================================================
        // QUARTERMASTER HERO PROPERTIES (Phase 3)
        // ========================================================================

        /// <summary>
        ///     The current quartermaster Hero for dialog interaction.
        ///     Returns null if not enlisted or hero hasn't been created.
        /// </summary>
        public Hero QuartermasterHero => _quartermasterHero;

        /// <summary>
        ///     Personality archetype of the quartermaster.
        ///     Values: "veteran", "merchant", "bookkeeper", "scoundrel", "believer", "eccentric"
        /// </summary>
        public string QuartermasterArchetype => _quartermasterArchetype ?? "veteran";

        /// <summary>
        ///     Relationship level with quartermaster (0-100).
        /// </summary>
        public int QuartermasterRelationship => _quartermasterRelationship;

        /// <summary>
        ///     Whether this is the first meeting with the quartermaster.
        /// </summary>
        public bool HasMetQuartermaster => _hasMetQuartermaster;

        /// <summary>
        ///     Returns true if a bag check is scheduled or in progress.
        ///     Used to defer other events until bag check completes.
        /// </summary>
        public bool IsBagCheckPending => (_bagCheckScheduled || _bagCheckInProgress) && !_bagCheckCompleted;

        private bool ShouldRunBagCheck()
        {
            // Only on first enlist or after retirement/desertion reset, and only below Tier 7
            return !_bagCheckCompleted && !_bagCheckInProgress && !_bagCheckEverCompleted && _enlistmentTier < 7;
        }

        /// <summary>
        ///     Open baggage train stash with fatigue gating by rank.
        /// </summary>
        public bool TryOpenBaggageTrain()
        {
            try
            {
                var cost = GetBaggageFatigueCost();
                if (cost > 0 && !TryConsumeFatigue(cost, "baggage_train"))
                {
                    var msg = new TextObject("{=qm_baggage_no_fatigue}You are too exhausted to rummage through the baggage train.");
                    InformationManager.DisplayMessage(new InformationMessage(msg.ToString(), Colors.Red));
                    return false;
                }

                InventoryScreenHelper.OpenScreenAsStash(_baggageStash);
                return true;
            }
            catch (Exception ex)
            {
                // End-user support: stable code + full exception detail (stack trace written once per unique exception).
                ModLogger.ErrorCode("Enlistment", "E-ENLIST-001", "Error opening baggage train stash", ex);
                return false;
            }
        }

        private int GetBaggageFatigueCost()
        {
            if (_enlistmentTier <= 2)
            {
                return 4;
            }
            if (_enlistmentTier <= 4)
            {
                return 2;
            }
            return 0;
        }

        /// <summary>
        ///     Reduce fatigue for camp actions. Clamped to 0..FatigueMax. Returns true if applied.
        ///     Safe to call by camp actions when they later consume fatigue.
        ///     Now includes health penalty checks for exhaustion (Enhanced Fatigue System).
        /// </summary>
        public bool TryConsumeFatigue(int amount, string reason = null)
        {
            if (!IsEnlisted || amount <= 0)
            {
                return true;
            }

            var newValue = Math.Max(0, _fatigueCurrent - amount);
            if (newValue == _fatigueCurrent)
            {
                return false;
            }

            _fatigueCurrent = newValue;
            SessionDiagnostics.LogEvent("Fatigue", "Consumed",
                $"amount={amount}, now={_fatigueCurrent}/{_fatigueMax}, reason={reason ?? "camp_action"}");

            // Check for health penalties when fatigue gets critically low
            CheckFatigueHealthPenalty();

            return true;
        }

        private void ActivateProbation()
        {
            var config = EnlistedConfig.LoadRetirementConfig();
            var days = Math.Max(1, config?.ProbationDays ?? 12);
            _isOnProbation = true;
            _probationEnds = CampaignTime.Now + CampaignTime.Days(days);

            var fatigueCap = Math.Max(1, config?.ProbationFatigueCap ?? 18);
            _fatigueMax = Math.Min(_fatigueMax, fatigueCap);
            _fatigueCurrent = Math.Min(_fatigueCurrent, _fatigueMax);

            ModLogger.Info("Probation", $"Probation activated for {days} days (fatigue cap {_fatigueMax})");
        }

        private void ClearProbation(string reason)
        {
            if (!_isOnProbation)
            {
                return;
            }

            _isOnProbation = false;
            _probationEnds = CampaignTime.Zero;
            _fatigueMax = 24;
            _fatigueCurrent = Math.Min(_fatigueCurrent, _fatigueMax);

            ModLogger.Info("Probation", $"Probation cleared ({reason})");
        }

        private void TryApplyReservistReentryBoost(IFaction faction)
        {
            try
            {
                var svc = ServiceRecordManager.Instance;
                if (svc == null || faction == null)
                {
                    return;
                }

                if (svc.TryConsumeReservistForFaction(faction, out var targetTier, out var bonusXp,
                        out var relationBonus, out var band, out var probation))
                {
                    if (probation)
                    {
                        ActivateProbation();
                    }

                    if (targetTier > 0)
                    {
                        _enlistmentTier = Math.Max(_enlistmentTier, targetTier);
                    }

                    if (bonusXp > 0)
                    {
                        AddEnlistmentXP(bonusXp, "Reservist Re-entry");
                    }

                    if (relationBonus > 0)
                    {
                        if (_enlistedLord != null)
                        {
                            ChangeRelationAction.ApplyPlayerRelation(_enlistedLord, relationBonus, true, true);
                        }

                        var factionLeader = faction.Leader;
                        if (factionLeader != null && factionLeader != _enlistedLord)
                        {
                            ChangeRelationAction.ApplyPlayerRelation(factionLeader, relationBonus, true, true);
                        }
                    }

                    if (targetTier >= 7)
                    {
                        var fine = Math.Max(0, EnlistedConfig.LoadRetirementConfig().CommanderReentryFineGold);
                        if (fine > 0)
                        {
                            var hero = Hero.MainHero;
                            if (hero != null)
                            {
                                var partyGold = hero.PartyBelongedTo?.PartyTradeGold ?? 0;
                                var paid = Math.Min(partyGold, fine);
                                if (paid > 0)
                                {
                                    GiveGoldAction.ApplyBetweenCharacters(hero, null, paid);
                                }
                                ModLogger.Info("Enlistment",
                                    $"Commander re-entry fine paid: {paid} (configured {fine})");
                            }
                        }
                    }

                    _lastDischargeBand = band;
                }
            }
            catch (Exception ex)
            {
                // This should never fail, but if it does we want the full stack trace for support.
                ModLogger.ErrorCode("Enlistment", "E-ENLIST-002", "Reservist re-entry boost failed", ex);
            }
        }

        /// <summary>
        ///     Restore fatigue by amount (or full if amount <= 0). Clamped to max.
        /// </summary>
        public void RestoreFatigue(int amount = 0, string reason = null)
        {
            var target = amount > 0 ? Math.Min(_fatigueMax, _fatigueCurrent + amount) : _fatigueMax;
            if (target == _fatigueCurrent)
            {
                return;
            }

            _fatigueCurrent = target;
            SessionDiagnostics.LogEvent("Fatigue", "Restored",
                $"amount={amount}, now={_fatigueCurrent}/{_fatigueMax}, reason={reason ?? "rest"}");

            // Check if we've recovered enough to remove health penalties
            CheckFatigueHealthRecovery();
        }

        /// <summary>
        ///     Gets the hourly fatigue recovery rate based on military rank/tier.
        ///     Higher ranks recover faster due to better accommodations and rest privileges.
        ///     See: docs/research/IMPLEMENTATION_PLAN.md lines 138-145
        /// </summary>
        /// <returns>Fatigue points recovered per hour during rest periods.</returns>
        public float GetFatigueRecoveryRate()
        {
            // Based on tier (see IMPLEMENTATION_PLAN.md)
            // T1-T2: 0.5/hour (8 hours to full recovery from exhausted)
            // T3-T4: 0.75/hour (~5.3 hours to full recovery)
            // T5-T6: 1.0/hour (4 hours to full recovery)
            // T7+: 1.25/hour (3.2 hours to full recovery)
            if (_enlistmentTier <= 2) return 0.5f;
            if (_enlistmentTier <= 4) return 0.75f;
            if (_enlistmentTier <= 6) return 1.0f;
            return 1.25f;
        }

        /// <summary>
        ///     Checks and applies health penalties based on current fatigue level.
        ///     Called when fatigue is consumed. Low fatigue (high exhaustion) triggers penalties.
        ///     - At 8 or less remaining: 15% health reduction
        ///     - At 0 remaining: 70% health reduction (drops to 30%)
        /// </summary>
        private void CheckFatigueHealthPenalty()
        {
            var hero = Hero.MainHero;
            if (hero == null) return;

            // Fatigue thresholds (remaining points):
            // 0 = completely exhausted (severe penalty)
            // 8 or less = exhausted (moderate penalty)
            // 9+ = fine (no penalty)
            const int SevereThreshold = 0;
            const int ModerateThreshold = 8;

            if (_fatigueCurrent <= SevereThreshold)
            {
                // Severe exhaustion: Drop to 30% health
                if (_healthBeforeExhaustion < 0)
                    _healthBeforeExhaustion = hero.HitPoints;

                var targetHealth = Math.Max(1, (int)(hero.MaxHitPoints * 0.30f));
                if (hero.HitPoints > targetHealth)
                {
                    hero.HitPoints = targetHealth;
                    InformationManager.DisplayMessage(new InformationMessage(
                        "You collapse from exhaustion! Health dropped to 30%.",
                        Colors.Red));
                    ModLogger.Warn("Fatigue", $"Severe exhaustion penalty applied: HP={hero.HitPoints}/{hero.MaxHitPoints}");
                }
            }
            else if (_fatigueCurrent <= ModerateThreshold)
            {
                // Moderate exhaustion: Reduce to 85% max health
                if (_healthBeforeExhaustion < 0)
                    _healthBeforeExhaustion = hero.HitPoints;

                var targetHealth = Math.Max(1, (int)(hero.MaxHitPoints * 0.85f));
                if (hero.HitPoints > targetHealth)
                {
                    hero.HitPoints = targetHealth;
                    InformationManager.DisplayMessage(new InformationMessage(
                        "You're exhausted. Health reduced.",
                        Colors.Yellow));
                    ModLogger.Info("Fatigue", $"Moderate exhaustion penalty applied: HP={hero.HitPoints}/{hero.MaxHitPoints}");
                }
            }
        }

        /// <summary>
        ///     Checks if health should be restored after fatigue recovery.
        ///     Called when fatigue is restored. Removes health penalties when fatigue rises above danger zones.
        /// </summary>
        private void CheckFatigueHealthRecovery()
        {
            var hero = Hero.MainHero;
            if (hero == null || _healthBeforeExhaustion < 0) return;

            const int ModerateThreshold = 8;

            // Restore health if fatigue is now above the danger zone
            if (_fatigueCurrent > ModerateThreshold)
            {
                var restoredHealth = (int)_healthBeforeExhaustion;
                if (hero.HitPoints < restoredHealth)
                {
                    hero.HitPoints = Math.Min(restoredHealth, hero.MaxHitPoints);
                    InformationManager.DisplayMessage(new InformationMessage(
                        "You feel rested. Health restored.",
                        Colors.Green));
                    ModLogger.Info("Fatigue", $"Health restored after rest: HP={hero.HitPoints}/{hero.MaxHitPoints}");
                }
                _healthBeforeExhaustion = -1f;
            }
        }

        /// <summary>
        ///     Processes hourly fatigue recovery during rest periods.
        ///     Called from OnHourlyTick. Recovery only occurs during night hours (dusk/night/dawn).
        ///     Settlement bonus provides +2 fatigue recovery per hour.
        /// </summary>
        public void ProcessFatigueRecovery()
        {
            if (!IsEnlisted) return;
            if (_fatigueCurrent >= _fatigueMax) return; // Already at max

            // Only recover during rest hours (dusk, night, dawn)
            // Use Campaign.Current.IsNight as a simple check, or check time of day
            var currentHour = CampaignTime.Now.CurrentHourInDay;
            bool isRestPeriod = currentHour >= 20 || currentHour <= 6; // 8 PM to 6 AM

            if (!isRestPeriod)
            {
                return;
            }

            // Get rank-based recovery rate
            float recoveryRate = GetFatigueRecoveryRate();

            // Settlement bonus: +2/hour when in a town
            if (Settlement.CurrentSettlement?.IsTown == true)
            {
                recoveryRate += 2.0f;
            }

            // Accumulate fractional recovery
            _accumulatedFatigueRecovery += recoveryRate;

            // Apply whole number recovery
            if (_accumulatedFatigueRecovery >= 1.0f)
            {
                int wholeRecovery = (int)_accumulatedFatigueRecovery;
                _accumulatedFatigueRecovery -= wholeRecovery;

                RestoreFatigue(wholeRecovery, "night_rest");
                ModLogger.Debug("Fatigue",
                    $"Hourly recovery: +{wholeRecovery} (rate={recoveryRate}/hr, tier={_enlistmentTier}, now={_fatigueCurrent}/{_fatigueMax})");
            }

            _lastFatigueRecoveryTime = CampaignTime.Now;
        }

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
        ///     Immediately joins the lord's battle when MapEvent starts so the fight cannot auto-resolve
        ///     before the player is collected. Used as a fast-path guard for tiny skirmishes that end in
        ///     the same frame they begin.
        /// </summary>
        private void ForceImmediateBattleJoin(MapEvent mapEvent, MobileParty main, MobileParty lordParty)
        {
            try
            {
                if (mapEvent == null || main == null || lordParty == null)
                {
                    return;
                }

                // Respect reserve choice; don't force-join if the player opted to sit out
                if (EnlistedEncounterBehavior.IsWaitingInReserve)
                {
                    ModLogger.Debug("Battle", "Skipping immediate join - player is waiting in reserve");
                    return;
                }

                // Already joined
                if (main.Party?.MapEvent == mapEvent)
                {
                    return;
                }

                var attackerSide = mapEvent.AttackerSide;
                var defenderSide = mapEvent.DefenderSide;
                bool lordIsAttacker = attackerSide?.Parties?.Any(p => p?.Party == lordParty.Party) == true;
                var targetSide = lordIsAttacker ? attackerSide : defenderSide;

                if (targetSide == null)
                {
                    ModLogger.Warn("Battle", "MapEventStarted: could not resolve lord side for immediate join");
                    return;
                }

                // Make sure the player is eligible for collection and hidden to avoid helper menus
                main.IsActive = true;
                main.IsVisible = false;
                main.IgnoreByOtherPartiesTill(CampaignTime.Now);
                TrySetShouldJoinPlayerBattles(main, true);

                // NAVAL BATTLE FIX: Sync player's sea state with lord before joining naval battles
                // The game requires IsCurrentlyAtSea to match the MapEvent type for encounters to work.
                // Without this sync, PlayerEncounter.Init() crashes because the player party state
                // doesn't match the naval battle expectations (ship handling, position validation, etc.)
                if (mapEvent.IsNavalMapEvent && main.IsCurrentlyAtSea != lordParty.IsCurrentlyAtSea)
                {
                    main.IsCurrentlyAtSea = lordParty.IsCurrentlyAtSea;
                    main.Position = lordParty.Position;
                    ModLogger.Debug("Battle",
                        $"Synced player sea state to {lordParty.IsCurrentlyAtSea} for naval battle join");
                }

                var playerSideLabel = lordIsAttacker ? "Attacker" : "Defender";

                // Join the MapEvent on the lord's side right away to block auto-sim resolution
                main.Party.MapEventSide = targetSide;
                ModLogger.Info("Battle",
                    $"Immediate battle join on {playerSideLabel} side (MapEventStarted guard, naval={mapEvent.IsNavalMapEvent})");

                if (PlayerEncounter.Current == null && !_playerEncounterCreatedForBattle)
                {
                    _playerEncounterCreatedForBattle = true;
                    PlayerEncounter.Start();
                    PlayerEncounter.Init();
                    ModLogger.Info("Battle",
                        "PlayerEncounter initialized at battle start to prevent instant auto-resolve");
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("Battle",
                    $"Failed immediate battle join on MapEventStarted: {ex.Message}", ex);
            }
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
            SaveLoadDiagnostics.SafeSyncData(this, dataStore, () =>
            {
                // Serialize all enlistment state variables
                // These values are saved to the game's save file and restored when loading
                SyncKey(dataStore, "_enlistedLord", ref _enlistedLord);
            SyncKey(dataStore, "_enlistmentTier", ref _enlistmentTier);
            SyncKey(dataStore, "_enlistmentXP", ref _enlistmentXP);
            SyncKey(dataStore, "_lastPromotionBlockedMessageTime", ref _lastPromotionBlockedMessageTime);
            SyncKey(dataStore, "_hasBackedUpEquipment", ref _hasBackedUpEquipment);
            SyncKey(dataStore, "_personalBattleEquipment", ref _personalBattleEquipment);
            SyncKey(dataStore, "_personalCivilianEquipment", ref _personalCivilianEquipment);
            SyncKey(dataStore, "_personalInventory", ref _personalInventory);
            SyncKey(dataStore, "_baggageStash", ref _baggageStash);
            SyncKey(dataStore, "_bagCheckCompleted", ref _bagCheckCompleted);
            SyncKey(dataStore, "_bagCheckEverCompleted", ref _bagCheckEverCompleted);
            SyncKey(dataStore, "_disbandArmyAfterBattle", ref _disbandArmyAfterBattle);
            SyncKey(dataStore, "_enlistmentDate", ref _enlistmentDate);
            
            // Phase 7: Promotion requirements tracking
            SyncKey(dataStore, "_lastPromotionDate", ref _lastPromotionDate);
            SyncKey(dataStore, "_battlesSurvived", ref _battlesSurvived);
            SyncKey(dataStore, "_eventsCompleted", ref _eventsCompleted);

            SyncKey(dataStore, "_isOnLeave", ref _isOnLeave);
            SyncKey(dataStore, "_leaveStartDate", ref _leaveStartDate);
            SyncKey(dataStore, "_leaveCooldownEnds", ref _leaveCooldownEnds);
            SyncKey(dataStore, "_desertionGracePeriodEnd", ref _desertionGracePeriodEnd);
            SyncKey(dataStore, "_pendingDesertionKingdom", ref _pendingDesertionKingdom);
            SyncKey(dataStore, "_savedGraceTier", ref _savedGraceTier);
            SyncKey(dataStore, "_savedGraceLord", ref _savedGraceLord);
            SyncKey(dataStore, "_savedGraceXP", ref _savedGraceXP);
            SyncKey(dataStore, "_savedGraceTroopId", ref _savedGraceTroopId);
            SyncKey(dataStore, "_pendingMusterPay", ref _pendingMusterPay);
            SyncKey(dataStore, "_nextPayday", ref _nextPayday);
            SyncKey(dataStore, "_payMusterPending", ref _payMusterPending);
            SyncKey(dataStore, "_lastPayOutcome", ref _lastPayOutcome);
            
            // Pay tension state (Phase 1 Pay System)
            SyncKey(dataStore, "_payTension", ref _payTension);
            SyncKey(dataStore, "_owedBackpay", ref _owedBackpay);
            SyncKey(dataStore, "_lastPayDate", ref _lastPayDate);
            SyncKey(dataStore, "_consecutiveDelays", ref _consecutiveDelays);
            SyncKey(dataStore, "_lanceFundBalance", ref _lanceFundBalance);
            
            SyncKey(dataStore, "_pensionAmountPerDay", ref _pensionAmountPerDay);
            SyncKey(dataStore, "_pensionFactionId", ref _pensionFactionId);
            SyncKey(dataStore, "_isPensionPaused", ref _isPensionPaused);
            SyncKey(dataStore, "_isPendingDischarge", ref _isPendingDischarge);
            SyncKey(dataStore, "_lastDischargeBand", ref _lastDischargeBand);
            SyncKey(dataStore, "_isOnProbation", ref _isOnProbation);
            SyncKey(dataStore, "_probationEnds", ref _probationEnds);
            SyncKey(dataStore, "_savedGraceEnlistmentDate", ref _savedGraceEnlistmentDate);
            SyncKey(dataStore, "_savedGraceLanceId", ref _savedGraceLanceId);
            SyncKey(dataStore, "_savedGraceLanceName", ref _savedGraceLanceName);
            SyncKey(dataStore, "_savedGraceLanceStyle", ref _savedGraceLanceStyle);
            SyncKey(dataStore, "_savedGraceLanceStoryId", ref _savedGraceLanceStoryId);
            SyncKey(dataStore, "_savedGraceLanceProvisional", ref _savedGraceLanceProvisional);
            SyncKey(dataStore, "_savedGraceManualLanceStyleId", ref _savedGraceManualLanceStyleId);
            SyncKey(dataStore, "_savedGraceLanceLegacy", ref _savedGraceLanceLegacy);
            SyncKey(dataStore, "_graceProtectionEnds", ref _graceProtectionEnds);
            SyncKey(dataStore, "_selectedDuty", ref _selectedDuty);
            SyncKey(dataStore, "_currentLanceId", ref _currentLanceId);
            SyncKey(dataStore, "_currentLanceName", ref _currentLanceName);
            SyncKey(dataStore, "_currentLanceStyle", ref _currentLanceStyle);
            SyncKey(dataStore, "_currentLanceStoryId", ref _currentLanceStoryId);
            SyncKey(dataStore, "_isLanceProvisional", ref _isLanceProvisional);
            SyncKey(dataStore, "_manualLanceStyleId", ref _manualLanceStyleId);
            SyncKey(dataStore, "_isLanceLegacy", ref _isLanceLegacy);
            SyncKey(dataStore, "_fatigueCurrent", ref _fatigueCurrent);
            SyncKey(dataStore, "_fatigueMax", ref _fatigueMax);

            // Enhanced Fatigue System (Phase 1)
            SyncKey(dataStore, "_lastFatigueRecoveryTime", ref _lastFatigueRecoveryTime);
            SyncKey(dataStore, "_healthBeforeExhaustion", ref _healthBeforeExhaustion);
            SyncKey(dataStore, "_accumulatedFatigueRecovery", ref _accumulatedFatigueRecovery);

            // Quartermaster Hero System (Phase 3)
            SyncKey(dataStore, "_quartermasterHero", ref _quartermasterHero);
            SyncKey(dataStore, "_quartermasterArchetype", ref _quartermasterArchetype);
            SyncKey(dataStore, "_quartermasterRelationship", ref _quartermasterRelationship);
            SyncKey(dataStore, "_hasMetQuartermaster", ref _hasMetQuartermaster);

            // Food/Rations System (Phase 5)
            SyncKey(dataStore, "_currentFoodQuality", ref _currentFoodQuality);
            SyncKey(dataStore, "_foodQualityExpires", ref _foodQualityExpires);

            // Retinue Provisioning System (Phase 6)
            SyncKey(dataStore, "_retinueProvisioningTier", ref _retinueProvisioningTier);
            SyncKey(dataStore, "_retinueProvisioningExpires", ref _retinueProvisioningExpires);
            SyncKey(dataStore, "_retinueProvisioningWarningShown", ref _retinueProvisioningWarningShown);

            // Serialize kingdom state so we can restore the player's original kingdom/clan status
            SyncKey(dataStore, "_originalKingdom", ref _originalKingdom);
            SyncKey(dataStore, "_wasIndependentClan", ref _wasIndependentClan);

            // Serialize minor faction war relations - these are created when enlisting with non-Kingdom lords
            // and need to be restored to neutral when service ends
            SyncKey(dataStore, "_minorFactionWarRelations", ref _minorFactionWarRelations);

            // Serialize minor faction desertion cooldowns - manual serialization for Dictionary<string, CampaignTime>
            // Bannerlord's save system can serialize this dictionary directly since both types are primitives
            SerializeMinorFactionDesertionCooldowns(dataStore);

            // Veteran retirement system state - manual serialization for dictionary
            // Bannerlord's save system can't serialize custom class dictionaries directly
            SyncKey(dataStore, "_retirementNotificationShown", ref _retirementNotificationShown);
            SyncKey(dataStore, "_currentTermKills", ref _currentTermKills);

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
                
                // Clean up any duplicate hero entries in rosters (can occur after escape from captivity)
                DeduplicateRosterHeroes();
                
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
            });
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
                SyncKey(dataStore, "_vetRec_count", ref recordCount);

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

                        SyncKey(dataStore, $"_vetRec_{index}_id", ref kingdomId);
                        SyncKey(dataStore, $"_vetRec_{index}_firstTerm", ref firstTerm);
                        SyncKey(dataStore, $"_vetRec_{index}_tier", ref tier);
                        SyncKey(dataStore, $"_vetRec_{index}_kills", ref kills);
                        SyncKey(dataStore, $"_vetRec_{index}_cooldown", ref cooldown);
                        SyncKey(dataStore, $"_vetRec_{index}_termEnd", ref termEnd);
                        SyncKey(dataStore, $"_vetRec_{index}_renewal", ref renewal);
                        SyncKey(dataStore, $"_vetRec_{index}_renewalCount", ref renewalCount);
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

                        SyncKey(dataStore, $"_vetRec_{i}_id", ref kingdomId);
                        SyncKey(dataStore, $"_vetRec_{i}_firstTerm", ref firstTerm);
                        SyncKey(dataStore, $"_vetRec_{i}_tier", ref tier);
                        SyncKey(dataStore, $"_vetRec_{i}_kills", ref kills);
                        SyncKey(dataStore, $"_vetRec_{i}_cooldown", ref cooldown);
                        SyncKey(dataStore, $"_vetRec_{i}_termEnd", ref termEnd);
                        SyncKey(dataStore, $"_vetRec_{i}_renewal", ref renewal);
                        SyncKey(dataStore, $"_vetRec_{i}_renewalCount", ref renewalCount);

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
                ModLogger.ErrorCode("SaveLoad", "E-SAVELOAD-001", "Error serializing veteran records", ex);
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
                SyncKey(dataStore, "_minorFacDesertion_count", ref cooldownCount);

                if (!dataStore.IsLoading)
                {
                    // Saving: serialize each cooldown entry individually
                    var index = 0;
                    foreach (var kvp in _minorFactionDesertionCooldowns)
                    {
                        var factionId = kvp.Key;
                        var cooldownEnd = kvp.Value;

                        SyncKey(dataStore, $"_minorFacDesertion_{index}_id", ref factionId);
                        SyncKey(dataStore, $"_minorFacDesertion_{index}_end", ref cooldownEnd);
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

                        SyncKey(dataStore, $"_minorFacDesertion_{i}_id", ref factionId);
                        SyncKey(dataStore, $"_minorFacDesertion_{i}_end", ref cooldownEnd);

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
                ModLogger.ErrorCode("SaveLoad", "E-SAVELOAD-002", "Error serializing minor faction desertion cooldowns", ex);
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
            if (lord.MapFaction != null && !(lord.MapFaction is Kingdom) && IsBlockedFromMinorFaction(lord.MapFaction, out var remainingDays))
            {
                reason = new TextObject(
                    "{=Enlisted_Message_MinorFactionCooldown}{FACTION} will not accept you back for another {DAYS} days due to your past desertion.");
                reason.SetTextVariable("FACTION", lord.MapFaction.Name);
                reason.SetTextVariable("DAYS", remainingDays);
                return false;
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

            // Run enlistment bag check (stash/sell/smuggle) before proceeding
            // If the incidents behavior is unavailable, fail open so enlistment can continue.
            // For stability, schedule the bag check 1 in-game hour later so it never blocks enlistment.
            if (ShouldRunBagCheck() && !_bagCheckScheduled)
            {
                _pendingBagCheckLord = lord;
                _bagCheckScheduled = true;
                _bagCheckDueTime = CampaignTime.Now + CampaignTime.Hours(1f);
                ModLogger.Info("Enlistment", $"Deferred bag check scheduled for {_bagCheckDueTime}");
            }

            ContinueStartEnlistInternal(lord);
        }

        private void ShowBagCheckInquiry(Hero lord)
        {
            try
            {
                _bagCheckInProgress = true;

                var options = new List<InquiryElement>
                {
                    new InquiryElement("stash",
                        new TextObject("{=qm_stow_all}\"Stow it all\" (50g)").ToString(),
                        null,
                        true,
                        new TextObject("{=qm_stow_hint}Move every scrap into the baggage wagon and pay the clerk his fee.").ToString()),
                    new InquiryElement("sell",
                        new TextObject("{=qm_sell_all}\"Sell it all\" (60%)").ToString(),
                        null,
                        true,
                        new TextObject("{=qm_sell_hint}Liquidate the lot at a battlefield rate and march off heavier in coin.").ToString()),
                    new InquiryElement("smuggle",
                        new TextObject("{=qm_smuggle_one}\"I'm keeping one thing\" (Roguery 30+)").ToString(),
                        null,
                        true,
                        new TextObject("{=qm_smuggle_hint}Slip one prized piece past the ledger; if caught, it’s gone.").ToString())
                };

                var inquiry = new MultiSelectionInquiryData(
                    new TextObject("{=qm_bagcheck_title}Enlistment Bag Check").ToString(),
                    new TextObject("{=qm_bagcheck_body}The quartermaster lifts his quill. \"You can’t march in that finery. Regimental rules. Everything goes in the wagons or my ledger. If the wagons burn, so does your past life. How do you want this written, soldier?\"").ToString(),
                    options,
                    false,
                    1,
                    1,
                    new TextObject("{=qm_continue}Continue").ToString(),
                    new TextObject("{=str_cancel}Cancel").ToString(),
                    selection =>
                    {
                        try
                        {
                            var choice = selection?.FirstOrDefault()?.Identifier as string;
                            HandleBagCheckChoice(choice);
                        }
                        finally
                        {
                            _bagCheckInProgress = false;
                        }
                    },
                    _ =>
                    {
                        _bagCheckInProgress = false;
                        ModLogger.Warn("Enlistment", "Bag check cancelled; enlistment halted.");
                    });

                MBInformationManager.ShowMultiSelectionInquiry(inquiry);
            }
            catch (Exception ex)
            {
                _bagCheckInProgress = false;
                ModLogger.ErrorCode("Enlistment", "E-ENLIST-003", "Bag check prompt failed", ex);
            }
        }

        // Fallback inquiry used by incidents behavior when invoked manually
        public void ShowBagCheckInquiryFallback()
        {
            ShowBagCheckInquiry(_pendingBagCheckLord);
        }

        public void HandleBagCheckChoice(string choice)
        {
            EnsureBaggageStash();
            switch (choice)
            {
                case "stash":
                    StashAllBelongings(Hero.MainHero, chargeFee: 50);
                    break;
                case "sell":
                    LiquidateAllBelongings(Hero.MainHero, 0.60f);
                    break;
                case "smuggle":
                    SmuggleOneItem(Hero.MainHero);
                    break;
                default:
                    // Default to stow to avoid enlistment without cleanup
                    StashAllBelongings(Hero.MainHero, chargeFee: 50);
                    break;
            }

            _bagCheckCompleted = true;
            _bagCheckEverCompleted = true;
            _pendingBagCheckLord = null;
            _bagCheckScheduled = false;
            _bagCheckDueTime = CampaignTime.Zero;
            _bagCheckInProgress = false;
            
            // NOTE: Do NOT call ContinueStartEnlistInternal here!
            // The player is already enlisted from StartEnlist() → ContinueStartEnlistInternal() (first call).
            // Calling it again here causes:
            // 1. OnEnlisted event to fire twice
            // 2. Onboarding state to be re-initialized
            // 3. Queued onboarding events to be dropped as "no longer eligible"
            // The bag check should ONLY process equipment, not re-run the enlistment flow.
            ModLogger.Info("Enlistment", "Bag check completed successfully");
        }

        /// <summary>
        ///     Processes a deferred enlistment bag check after a cooldown so enlistment flow is never blocked.
        /// </summary>
        private void ProcessDeferredBagCheck()
        {
            if (!_bagCheckScheduled || _bagCheckCompleted || _bagCheckInProgress)
            {
                return;
            }

            if (CampaignTime.Now < _bagCheckDueTime)
            {
                return;
            }

            var main = MobileParty.MainParty;
            if (main == null)
            {
                return;
            }

            // Only fire when safe: not in battle, encounter, or captivity
            bool inBattle = main.Party?.MapEvent != null;
            bool inEncounter = PlayerEncounter.Current != null;
            bool isPrisoner = Hero.MainHero?.IsPrisoner == true;

            if (inBattle || inEncounter || isPrisoner)
            {
                // Retry next hour
                return;
            }

            var incidents = EnlistedIncidentsBehavior.Instance;
            if (incidents != null)
            {
                _bagCheckInProgress = true;
                ModLogger.Info("Enlistment", $"Triggering deferred bag check (scheduled at {_bagCheckDueTime})");
                incidents.TriggerBagCheckIncident();
                return;
            }

            // Fail open if incidents behavior is unavailable
            ModLogger.Warn("Enlistment",
                "Deferred bag check behavior unavailable; marking bag check complete to avoid blocking enlistment");
            _bagCheckCompleted = true;
            _bagCheckEverCompleted = true;
            _bagCheckScheduled = false;
            _bagCheckDueTime = CampaignTime.Zero;
            _pendingBagCheckLord = null;
        }

        private void EnsureBaggageStash()
        {
            _baggageStash ??= new ItemRoster();
        }

        private void StashAllBelongings(Hero hero, int chargeFee)
        {
            try
            {
                EnsureBaggageStash();

                var partyRoster = MobileParty.MainParty?.ItemRoster;
                if (partyRoster != null)
                {
                    foreach (var element in partyRoster.ToList())
                    {
                        if (element.EquipmentElement.Item != null && element.Amount > 0)
                        {
                            _baggageStash.AddToCounts(element.EquipmentElement.Item, element.Amount);
                        }
                    }
                    partyRoster.Clear();
                }

                MoveEquipmentToStash(hero.BattleEquipment);
                MoveEquipmentToStash(hero.CivilianEquipment);

            if (chargeFee > 0)
            {
                // Use party gold (what the player sees in UI) not hero personal gold
                var partyGold = Hero.MainHero?.PartyBelongedTo?.PartyTradeGold ?? 0;
                var fee = Math.Min(chargeFee, partyGold);
                if (fee > 0)
                {
                    // GiveGoldAction properly deducts from party treasury and updates UI
                    GiveGoldAction.ApplyBetweenCharacters(hero, null, fee);
                    InformationManager.DisplayMessage(new InformationMessage(
                        new TextObject("{=qm_fee_paid}You pay {FEE} denars for the wagon fee.")
                            .SetTextVariable("FEE", fee).ToString()));
                    ModLogger.Info("Gold", $"Wagon fee paid: {fee} denars");
                }
                else
                {
                    // Player has no gold - still proceed but log it
                    ModLogger.Info("Gold", "No gold available for wagon fee - proceeding without charge.");
                }
            }

                ModLogger.Info("Enlistment", $"Stashed belongings; stash now {_baggageStash.Count} entries.");
            }
            catch (Exception ex)
            {
                ModLogger.ErrorCode("Enlistment", "E-ENLIST-004", "Error stashing belongings", ex);
            }
        }

        private void LiquidateAllBelongings(Hero hero, float rate)
        {
            try
            {
                var totalValue = 0f;
                var partyRoster = MobileParty.MainParty?.ItemRoster;
                if (partyRoster != null)
                {
                    foreach (var element in partyRoster.ToList())
                    {
                        if (element.EquipmentElement.Item != null && element.Amount > 0)
                        {
                            totalValue += element.EquipmentElement.Item.Value * element.Amount * rate;
                        }
                    }
                    partyRoster.Clear();
                }

                totalValue += ExtractEquipmentValue(hero.BattleEquipment, rate);
                totalValue += ExtractEquipmentValue(hero.CivilianEquipment, rate);

                var gain = (int)Math.Floor(totalValue);
                if (gain > 0)
                {
                    // GiveGoldAction properly adds to party treasury and updates UI
                    GiveGoldAction.ApplyBetweenCharacters(null, hero, gain);
                    InformationManager.DisplayMessage(new InformationMessage(
                        new TextObject("{=qm_liquidate_gain}You receive {GOLD} denars from liquidating your possessions.")
                            .SetTextVariable("GOLD", gain).ToString()));
                }

                ModLogger.Info("Enlistment", $"Liquidated belongings for {gain} denars at rate {rate}");
            }
            catch (Exception ex)
            {
                ModLogger.ErrorCode("Enlistment", "E-ENLIST-005", "Error liquidating belongings", ex);
            }
        }

        private void SmuggleOneItem(Hero hero)
        {
            try
            {
                EnsureBaggageStash();

                var allItems = new List<ItemRosterElement>();

                var partyRoster = MobileParty.MainParty?.ItemRoster;
                if (partyRoster != null)
                {
                    allItems.AddRange(partyRoster.ToList());
                }

                allItems.AddRange(GetEquipmentElements(hero.BattleEquipment));
                allItems.AddRange(GetEquipmentElements(hero.CivilianEquipment));

                var best = allItems
                    .Where(e => e.EquipmentElement.Item != null && e.Amount > 0)
                    .OrderByDescending(e => e.EquipmentElement.Item.Value)
                    .FirstOrDefault();

                // If nothing to smuggle, just stash everything
                if (best.EquipmentElement.Item == null)
                {
                    StashAllBelongings(hero, 0);
                    return;
                }

                // Clear current rosters/equipment
                StashAllBelongings(hero, 0);

                var roguery = hero.GetSkillValue(DefaultSkills.Roguery);
                var success = roguery >= 30;

                if (success)
                {
                    MobileParty.MainParty?.ItemRoster.AddToCounts(best.EquipmentElement.Item, 1);
                    InformationManager.DisplayMessage(new InformationMessage(
                        new TextObject("{=qm_smuggle_success}You tuck away your {ITEM} without being caught.")
                            .SetTextVariable("ITEM", best.EquipmentElement.Item.Name).ToString()));
                }
                else
                {
                    // Failure: item confiscated
                    InformationManager.DisplayMessage(new InformationMessage(
                        new TextObject("{=qm_smuggle_fail}Caught smuggling. Your {ITEM} is confiscated.")
                            .SetTextVariable("ITEM", best.EquipmentElement.Item.Name).ToString(), Colors.Red));
                }

                ModLogger.Info("Enlistment",
                    $"Smuggle attempt {(success ? "succeeded" : "failed")} for {best.EquipmentElement.Item.StringId}");
            }
            catch (Exception ex)
            {
                ModLogger.ErrorCode("Enlistment", "E-ENLIST-006", "Error smuggling item", ex);
            }
        }

        private void MoveEquipmentToStash(TaleWorlds.Core.Equipment equipment)
        {
            if (equipment == null)
            {
                return;
            }

            // Use numeric iteration to avoid invalid enum values like NumEquipmentSetSlots
            // which would cause IndexOutOfRangeException when used as an array index.
            for (int i = 0; i < (int)EquipmentIndex.NumEquipmentSetSlots; i++)
            {
                var slot = (EquipmentIndex)i;
                var elem = equipment[slot];
                if (elem.Item != null)
                {
                    _baggageStash.AddToCounts(elem.Item, 1);
                    equipment[slot] = default;
                }
            }
        }

        private float ExtractEquipmentValue(TaleWorlds.Core.Equipment equipment, float rate)
        {
            var total = 0f;
            if (equipment == null)
            {
                return total;
            }

            // Use numeric iteration to avoid invalid enum values like NumEquipmentSetSlots
            for (int i = 0; i < (int)EquipmentIndex.NumEquipmentSetSlots; i++)
            {
                var slot = (EquipmentIndex)i;
                var elem = equipment[slot];
                if (elem.Item != null)
                {
                    total += elem.Item.Value * rate;
                    equipment[slot] = default;
                }
            }

            return total;
        }

        private List<ItemRosterElement> GetEquipmentElements(TaleWorlds.Core.Equipment equipment)
        {
            var list = new List<ItemRosterElement>();
            if (equipment == null)
            {
                return list;
            }

            // Use numeric iteration to avoid invalid enum values like NumEquipmentSetSlots
            for (int i = 0; i < (int)EquipmentIndex.NumEquipmentSetSlots; i++)
            {
                var slot = (EquipmentIndex)i;
                var elem = equipment[slot];
                if (elem.Item != null)
                {
                    list.Add(new ItemRosterElement(elem, 1));
                }
            }

            return list;
        }

        private void ContinueStartEnlistInternal(Hero lord)
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

            if (!_bagCheckEverCompleted)
            {
                _bagCheckCompleted = false;
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
                _selectedDuty = "runner"; // Default to runner duty for new recruits
                _fatigueMax = 24;
                _fatigueCurrent = _fatigueMax;
                _pendingMusterPay = 0;
                _payMusterPending = false;
                _nextPayday = CampaignTime.Zero;
                _lastPayOutcome = null;
                _isPensionPaused = true; // Pension payments pause while enlisted
                _isPendingDischarge = false;
                _lastDischargeBand = null;
                _isOnProbation = false;
                _probationEnds = CampaignTime.Zero;
                
                // Reset term-based notification state (used only for informational prompts)
                _retirementNotificationShown = false;
                _currentTermKills = 0;

                TryApplyReservistReentryBoost(_enlistedLord?.MapFaction);

                // Assign provisional lance on enlist (or restore from grace)
                // If culture is unknown, prompt for style before assigning provisional lance
                TryPromptUnknownCultureStyle();
                AssignProvisionalLance(resumedFromGrace);

                // Register the formation-appropriate starter duty with the duties system
                // This ensures daily XP processing begins immediately for the basic duty
                var dutiesBehavior = EnlistedDutiesBehavior.Instance;
                if (dutiesBehavior != null)
                {
                    var formation = dutiesBehavior.PlayerFormation ?? "infantry";
                    var starterDuty = EnlistedDutiesBehavior.GetStarterDutyForFormation(formation);
                    dutiesBehavior.AssignDuty(starterDuty);
                    _selectedDuty = starterDuty;
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
                            ModLogger.ErrorCode("Enlistment", "E-ENLIST-007", "Error joining lord's kingdom", ex);
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
                        ModLogger.ErrorCode("Enlistment", "E-ENLIST-008",
                            "Error finishing PlayerEncounter before enlistment", ex);
                    }
                }

                // Attach the player's party to the lord's party using natural attachment system
                // This ensures the player follows the lord during travel and integrates with armies
                // IMPORTANT: Do this AFTER finishing any active encounters to avoid creating
                // an encounter with the lord's party itself
                // Escort into lord's party when nearby; clan expenses now native
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

                // Persistent Lance Leaders: connect/create the current leader now that we are enlisted.
                // Without this, the leader system only reconnects on session launch and never records state for new enlistments.
                try
                {
                    PersistentLanceLeadersBehavior.Instance?.OnPlayerEnlisted(lord);
                }
                catch (Exception ex)
                {
                    ModLogger.ErrorCode("LanceLeaders", "E-LANCELEAD-001",
                        "Failed to notify persistent lance leader system on enlist", ex);
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("Enlistment",
                    $"Failed to start enlistment with {lord?.Name?.ToString() ?? "null"} - {ex.Message}", ex);
                
                // CRITICAL: Reset enlistment state to prevent partial enlistment
                // Without this, IsEnlisted would return true and hide the enlistment dialog
                _enlistedLord = null;
                _enlistmentTier = 1;
                _enlistmentXP = 0;
                _enlistmentDate = CampaignTime.Zero;
                _selectedDuty = "runner";
                _isOnLeave = false;
                
                // Restore equipment if backup was created
                if (_hasBackedUpEquipment)
                {
                    RestorePersonalEquipment();
                    _hasBackedUpEquipment = false;
                }
                
                // Deactivate enlisted mode
                SyncActivationState("enlistment_failed");
                
                // Restore party visibility
                var main = MobileParty.MainParty;
                if (main != null)
                {
                    main.IsVisible = true;
                    main.IsActive = true;
                }
                
                ModLogger.Info("Enlistment", "Enlistment state reset after failure - dialog should be available again");
            }
        }

        public void TryPromptLanceSelection(int? overrideSelectionCount = null)
        {
            try
            {
                if (!LanceRegistry.IsFeatureEnabled() || !_isLanceProvisional || !IsEnlisted || _enlistedLord == null)
                {
                    return;
                }

                var config = Assignments.Core.ConfigurationManager.LoadLancesConfig();
                var selectionCount = overrideSelectionCount ?? config?.LanceSelectionCount ?? 3;
                selectionCount = Math.Max(1, Math.Min(selectionCount, 5)); // plan: 3–5 options

                var duties = EnlistedDutiesBehavior.Instance;
                var formation = duties?.PlayerFormation;
                var candidates = LanceRegistry.GetCandidateLances(_enlistedLord, formation, selectionCount,
                    _manualLanceStyleId);

                if (candidates.Count == 0)
                {
                    ModLogger.Warn("Lance", "No lance candidates available for selection prompt");
                    return;
                }

                var elements = candidates.Select(c =>
                    new InquiryElement(c, c.Name, null,
                        true, $"{c.Name} ({c.StyleId})")).ToList();

                var inquiry = new MultiSelectionInquiryData(
                    new TextObject("{=Lance_Select_Title}Select a Lance in need of recruits").ToString(),
                    new TextObject("{=Lance_Select_Body}Choose a Lance to formally join at Tier 2.").ToString(),
                    elements,
                    false,
                    1,
                    1,
                    new TextObject("{=Lance_Select_Confirm}Join").ToString(),
                    new TextObject("{=Lance_Select_Cancel}Keep provisional").ToString(),
                    OnLanceSelectionConfirmed,
                    OnLanceSelectionCancelled);

                MBInformationManager.ShowMultiSelectionInquiry(inquiry);
                ModLogger.Info("Lance", $"Presented lance selection with {elements.Count} options");
            }
            catch (Exception ex)
            {
                ModLogger.ErrorCode("Lance", "E-LANCE-004", "Error prompting lance selection", ex);
            }
        }

        private void OnLanceSelectionConfirmed(List<InquiryElement> selections)
        {
            try
            {
                var chosen = selections?.FirstOrDefault()?.Identifier as LanceAssignment;
                if (chosen == null)
                {
                    ModLogger.Warn("Lance", "Lance selection confirm without choice");
                    return;
                }

                FinalizeLanceSelection(chosen);
            }
            catch (Exception ex)
            {
                ModLogger.ErrorCode("Lance", "E-LANCE-005", "Error finalizing lance selection", ex);
            }
        }

        private void OnLanceSelectionCancelled(List<InquiryElement> _)
        {
            ModLogger.Info("Lance", "Player kept provisional lance (selection cancelled)");
        }

        private void FinalizeLanceSelection(LanceAssignment chosen)
        {
            _currentLanceId = chosen.Id;
            _currentLanceName = chosen.Name;
            _currentLanceStyle = chosen.StyleId;
            _currentLanceStoryId = chosen.StoryId;
            _isLanceProvisional = false;
            _manualLanceStyleId = null;
            _isLanceLegacy = false;

            var message = new TextObject("{=Lance_Select_Finalized}You have joined the {LANCE}.");
            message.SetTextVariable("LANCE", _currentLanceName);
            InformationManager.DisplayMessage(new InformationMessage(message.ToString(), Colors.Cyan));

            ModLogger.Info("Lance",
                $"Lance finalized: {_currentLanceName} [{_currentLanceStyle}] story={_currentLanceStoryId}");

            // Raise event for story/camp systems
            OnLanceFinalized?.Invoke(_currentLanceId, _currentLanceStyle, _currentLanceStoryId);

            // Lightweight session log for diagnostics
            SessionDiagnostics.LogEvent("Lance", "LanceFinalized",
                $"id={_currentLanceId}, style={_currentLanceStyle}, story={_currentLanceStoryId}");
        }

        private void AssignProvisionalLance(bool resumedFromGrace)
        {
            if (!LanceRegistry.IsFeatureEnabled())
            {
                ClearLanceState();
                return;
            }

            if (resumedFromGrace && !string.IsNullOrWhiteSpace(_savedGraceLanceId))
            {
                _currentLanceId = _savedGraceLanceId;
                _currentLanceName = _savedGraceLanceName;
                _currentLanceStyle = _savedGraceLanceStyle;
                _currentLanceStoryId = _savedGraceLanceStoryId;
                _isLanceProvisional = _savedGraceLanceProvisional;
                _manualLanceStyleId = _savedGraceManualLanceStyleId;
                ClearSavedGraceLance();
                ModLogger.Info("Lance",
                    $"Restored grace lance: {_currentLanceName} ({_currentLanceStyle})");
                return;
            }

            var duties = EnlistedDutiesBehavior.Instance;
            var formation = duties?.PlayerFormation;
            var assignment = LanceRegistry.GenerateProvisionalLance(_enlistedLord, formation, _manualLanceStyleId);

            if (assignment == null)
            {
                ClearLanceState();
                return;
            }

            _currentLanceId = assignment.Id;
            _currentLanceName = assignment.Name;
            _currentLanceStyle = assignment.StyleId;
            _currentLanceStoryId = assignment.StoryId;
            _isLanceProvisional = true;
            _isLanceLegacy = false;

            var cultureTag = string.IsNullOrWhiteSpace(assignment.SourceCultureId)
                ? "unknown"
                : assignment.SourceCultureId;
            var fallbackTag = assignment.UsedFallback ? " (fallback)" : string.Empty;
            ModLogger.Info("Lance",
                $"Provisional lance assigned: {_currentLanceName} [{_currentLanceStyle}] culture={cultureTag}{fallbackTag}");

            // One-line session log for diagnostics
            SessionDiagnostics.LogEvent("Lance", "LanceProvisional",
                $"id={_currentLanceId}, style={_currentLanceStyle}, story={_currentLanceStoryId}, culture={cultureTag}, fallback={assignment.UsedFallback}");
        }

        private void ClearLanceState()
        {
            _currentLanceId = null;
            _currentLanceName = null;
            _currentLanceStyle = null;
            _currentLanceStoryId = null;
            _isLanceProvisional = false;
            _manualLanceStyleId = null;
            _fatigueCurrent = _fatigueMax;
        }

        private void ClearSavedGraceLance()
        {
            _savedGraceLanceId = null;
            _savedGraceLanceName = null;
            _savedGraceLanceStyle = null;
            _savedGraceLanceStoryId = null;
            _savedGraceLanceProvisional = false;
            _savedGraceManualLanceStyleId = null;
            _savedGraceLanceLegacy = false;
        }

        private void TryPromptUnknownCultureStyle()
        {
            if (!LanceRegistry.IsFeatureEnabled() || _enlistedLord == null)
            {
                return;
            }

                var catalog = Assignments.Core.ConfigurationManager.LoadLanceCatalog();
            if (catalog?.StyleDefinitions == null || catalog.StyleDefinitions.Count == 0)
            {
                return;
            }

            var cultureId = _enlistedLord.MapFaction?.Culture?.StringId ?? _enlistedLord.Culture?.StringId;
            var mapped = catalog.CultureMap != null &&
                         !string.IsNullOrWhiteSpace(cultureId) &&
                         catalog.CultureMap.ContainsKey(cultureId);

            if (mapped)
            {
                _manualLanceStyleId = null;
                return;
            }

            var options = catalog.StyleDefinitions.Select(s =>
                new InquiryElement(
                    s.Id,
                    FormatStyleDisplayName(s.Id),
                    null,
                    true,
                    s.Id)).ToList();

            if (options.Count == 0)
            {
                return;
            }

            var inquiry = new MultiSelectionInquiryData(
                new TextObject("{=Lance_StylePrompt_Title}How does this army fight?").ToString(),
                new TextObject("{=Lance_StylePrompt_Body}Choose a tradition for your lance.").ToString(),
                options,
                false,
                1,
                1,
                new TextObject("{=Lance_StylePrompt_Confirm}Choose").ToString(),
                new TextObject("{=Lance_StylePrompt_Cancel}Cancel").ToString(),
                OnUnknownCultureStyleChosen,
                OnUnknownCultureStyleCancelled);

            MBInformationManager.ShowMultiSelectionInquiry(inquiry);
        }

        private void OnUnknownCultureStyleChosen(List<InquiryElement> selections)
        {
            try
            {
                var chosen = selections?.FirstOrDefault()?.Identifier as string;
                if (string.IsNullOrWhiteSpace(chosen))
                {
                    ModLogger.Warn("Lance", "Style selection confirmed with no choice");
                    return;
                }

                _manualLanceStyleId = chosen;
                ModLogger.Info("Lance", $"Manual style selected for unknown culture: {_manualLanceStyleId}");
                AssignProvisionalLance(false);
            }
            catch (Exception ex)
            {
                ModLogger.ErrorCode("Lance", "E-LANCE-006", "Error applying unknown-culture style selection", ex);
            }
        }

        private void OnUnknownCultureStyleCancelled(List<InquiryElement> _)
        {
            ModLogger.Info("Lance", "Unknown-culture style selection cancelled; using fallback (mercenary)");
            _manualLanceStyleId = null;

            SessionDiagnostics.LogEvent("Lance", "UnknownCultureStyleCancelled", "fallback=mercenary");
        }

        private string FormatStyleDisplayName(string styleId)
        {
            if (string.IsNullOrWhiteSpace(styleId))
            {
                return "Mercenary";
            }

            var trimmed = styleId.Replace("style_", "").Replace("_", " ");
            return char.ToUpper(trimmed[0]) + trimmed.Substring(1);
        }

        /// <summary>
        ///     Discharge the player from enlistment. By default, returns the player to independent status.
        ///     Exception: If honorably discharged after completing the configured full service period,
        ///     the player is returned to their original kingdom.
        /// </summary>
        /// <param name="reason">Reason for discharge (for logging).</param>
        /// <param name="isHonorableDischarge">Whether this is an honorable discharge (e.g., lord died). Defaults to false.</param>
        public void StopEnlist(string reason, bool isHonorableDischarge = false, bool retainKingdomDuringGrace = false)
        {
            try
            {
                ModLogger.Info("Enlistment", $"Service ended: {reason} (Honorable: {isHonorableDischarge})");

                // Persistent Lance Leaders: record discharge memory and clear cached leader reference.
                // Best effort only: enlistment discharge must not fail due to auxiliary systems.
                try
                {
                    PersistentLanceLeadersBehavior.Instance?.OnPlayerDischarged();
                }
                catch (Exception ex)
                {
                    // Best-effort only, but include the full exception detail for support.
                    ModLogger.ErrorCode("LanceLeaders", "E-LANCELEAD-002",
                        "Failed to notify persistent lance leader system on discharge", ex);
                }
                var retirementConfig = EnlistedConfig.LoadRetirementConfig();
                var firstTermDays = retirementConfig?.FirstTermDays > 0 ? retirementConfig.FirstTermDays : 252;
                
                // Set guard flag to prevent OnClanChangedKingdom from treating the upcoming
                // kingdom removal as desertion. This is an intentional discharge, not abandonment.
                _isProcessingDischarge = true;
                _bagCheckCompleted = false;
                _bagCheckInProgress = false;
                // Allow re-prompt on next enlistment if this was retirement or desertion
                var reasonLower = reason?.ToLowerInvariant() ?? string.Empty;
                if (isHonorableDischarge || reasonLower.Contains("desert"))
                {
                    _bagCheckEverCompleted = false;
                }
                // Otherwise keep the ever-completed flag so normal re-enlists skip the prompt
                
                // Clear reserve state immediately when enlistment ends for ANY reason
                // This prevents player getting stuck invisible if lord's party disbands while in reserve
                if (Combat.Behaviors.EnlistedEncounterBehavior.IsWaitingInReserve)
                {
                    ModLogger.Info("Battle", "Clearing reserve state during service end");
                    Combat.Behaviors.EnlistedEncounterBehavior.ClearReserveState();
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
                            ModLogger.ErrorCode("Enlistment", "E-ENLIST-011", "Error removing player from army", ex);
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

                    // CRITICAL: Check if player is still in a MapEvent, PlayerEncounter, or is a prisoner.
                    // If the player is in any of these states, we must NOT activate to prevent crashes.
                    // This is especially important when the lord is captured/defeated while the player
                    // is in a surrender menu - activating while in an encounter causes assertion failures.
                    // Also check prisoner state because native PlayerCaptivity deactivates the party;
                    // reactivating it here would fight with the native captivity system.
                    bool playerInMapEvent = main.Party.MapEvent != null;
                    bool playerInEncounter = PlayerEncounter.Current != null;
                    bool playerIsPrisoner = Hero.MainHero?.IsPrisoner == true;
                    playerInBattleState = playerInMapEvent || playerInEncounter || playerIsPrisoner;

                    if (playerInBattleState)
                    {
                        ModLogger.Info("Enlistment",
                            $"Player in vulnerable state when ending service (MapEvent: {playerInMapEvent}, Encounter: {playerInEncounter}, Prisoner: {playerIsPrisoner}) - deferring activation");
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
                    // This can happen when the army disbands at sea and service ends (e.g., lord captured at sea, lord dies at sea)
                    // Without this fix, the player becomes permanently stranded on the water
                    // EXCEPTION: If player is a PRISONER, do NOT teleport - let native's player_raft_state_after_prisoner
                    // menu handle stranding after release. Teleporting while prisoner causes confusing "washed ashore"
                    // notifications while still in captivity.
                    if (main.IsCurrentlyAtSea && !main.HasNavalNavigationCapability && !playerIsPrisoner)
                    {
                        ModLogger.Info("Naval",
                            "Player stranded at sea after service ended - teleporting to nearest port");
                        TryTeleportToNearestPort(main);
                    }
                    else if (main.IsCurrentlyAtSea && !main.HasNavalNavigationCapability && playerIsPrisoner)
                    {
                        ModLogger.Info("Naval",
                            "Player stranded at sea but is prisoner - letting native handle stranding after release");
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

                // Determine if player completed full enlistment period (configurable Bannerlord years)
                // This is required for honorable discharge to restore original kingdom
                var completedFullService = false;
                var daysServedInt = 0;
                if (_enlistmentDate != CampaignTime.Zero)
                {
                    var serviceDuration = CampaignTime.Now - _enlistmentDate;
                    var fullServicePeriod = CampaignTime.Days(firstTermDays);
                    completedFullService = serviceDuration >= fullServicePeriod;

                    var daysServed = serviceDuration.ToDays;
                    daysServedInt = (int)Math.Floor(daysServed);
                    ModLogger.Info("Enlistment",
                        $"Service duration: {daysServed:F1} days (Full period: {firstTermDays} days, Completed: {completedFullService})");
                }

                // Pension determination happens on discharge (honorable only)
                ApplyPensionOnDischarge(isHonorableDischarge, daysServedInt);

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
                        ModLogger.ErrorCode("Enlistment", "E-ENLIST-012", "Error restoring kingdom during discharge", ex);
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
                // BUG FIX: Removed _enlistedLord != null check - lord may be dead but we still need
                // to save progression state so player can resume at their previous rank
                if (retainKingdomDuringGrace)
                {
                    _savedGraceTier = _enlistmentTier;
                    _savedGraceXP = _enlistmentXP;
                    _savedGraceTroopId = TroopSelectionManager.Instance?.LastSelectedTroopId;
                    _savedGraceEnlistmentDate = _enlistmentDate;
                    _savedGraceLanceId = _currentLanceId;
                    _savedGraceLanceName = _currentLanceName;
                    _savedGraceLanceStyle = _currentLanceStyle;
                    _savedGraceLanceStoryId = _currentLanceStoryId;
                    _savedGraceLanceProvisional = _isLanceProvisional;
                    _savedGraceManualLanceStyleId = _manualLanceStyleId;
                    _savedGraceLanceLegacy = _isLanceLegacy;
                    ModLogger.Info("Enlistment",
                        $"Saved grace progression state: Tier={_savedGraceTier}, XP={_savedGraceXP}, Lord={((_enlistedLord?.Name?.ToString()) ?? "deceased")}");
                }
                else
                {
                    ClearSavedGraceLance();
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

                // Clean up Quartermaster Hero reference (Phase 3)
                CleanupQuartermasterOnServiceEnd();

                // Clear food quality bonus (Phase 5)
                ClearFoodQuality();

                // Clear retinue provisioning (Phase 6)
                ClearRetinueProvisioning();

                _enlistedLord = null;
                _enlistmentTier = 1;
                _enlistmentXP = 0;
                _enlistmentDate = CampaignTime.Zero;
                _disbandArmyAfterBattle = false; // Clear any pending army operations
                ClearPayState("service_ended");
                ClearLanceState();
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
            finally
            {
                // Always clear the discharge guard flag, even if an exception occurred.
                // This ensures OnClanChangedKingdom returns to normal behavior after discharge completes.
                _isProcessingDischarge = false;
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
                    ModLogger.ErrorCode("Desertion", "E-DESERT-004", "Cannot start grace period - kingdom is null",
                        new InvalidOperationException("Generated diagnostic exception to capture stack trace."));
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
                
                // BUG FIX: Also record reservist data when starting grace period
                // This ensures player can resume service later even if grace period expires
                // Uses "grace" band to give better treatment than deserter on re-enlistment
                var daysServed = _enlistmentDate != CampaignTime.Zero 
                    ? (int)(CampaignTime.Now - _enlistmentDate).ToDays 
                    : 0;
                ServiceRecordManager.Instance?.RecordReservist(
                    "grace", // New band type for grace period exits
                    daysServed,
                    _savedGraceTier > 0 ? _savedGraceTier : _enlistmentTier,
                    _savedGraceXP > 0 ? _savedGraceXP : _enlistmentXP,
                    _savedGraceLord ?? _enlistedLord);

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
                    ModLogger.ErrorCode("Desertion", "E-DESERT-005", "Cannot apply penalties - player clan is null",
                        new InvalidOperationException("Generated diagnostic exception to capture stack trace."));
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
                ModLogger.ErrorCode("Desertion", "E-DESERT-001", "Error applying minor faction desertion penalties", ex);
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
                    ModLogger.ErrorCode("Desertion", "E-DESERT-006", "Cannot desert - player clan is null",
                        new InvalidOperationException("Generated diagnostic exception to capture stack trace."));
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
                            ModLogger.ErrorCode("Desertion", "E-DESERT-002", "Error removing from army", ex);
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
                ClearPayState("desertion");

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
                        ModLogger.ErrorCode("Desertion", "E-DESERT-003", "Error leaving kingdom", ex);
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
                ModLogger.ErrorCode("Enlistment", "E-ENLIST-009",
                    "TroopSelectionManager unavailable - cannot restore saved equipment", null);
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

            manager.ApplySelectedTroopEquipment(Hero.MainHero, selectedTroop, autoIssueEquipment: true);
            return true;
        }

        /// <summary>
        ///     Hourly tick handler that runs once per in-game hour while the player is enlisted.
        ///     Maintains party following, visibility, and encounter prevention state.
        ///     Also processes hourly fatigue recovery during rest periods.
        ///     Called automatically by the game every hour.
        /// </summary>
        private void OnHourlyTick()
        {
            // Check if grace period expired (even if not enlisted)
            // BUG FIX: Previous check used IsInDesertionGracePeriod which requires CampaignTime.Now < _desertionGracePeriodEnd
            // This was contradictory with the >= check, meaning expiration NEVER triggered.
            // Fixed: Check if grace period WAS active (kingdom set, deadline set) but has now expired
            if (_pendingDesertionKingdom != null && 
                _desertionGracePeriodEnd != CampaignTime.Zero && 
                CampaignTime.Now >= _desertionGracePeriodEnd)
            {
                ModLogger.Info("Desertion", $"Grace period expired - {(CampaignTime.Now - _desertionGracePeriodEnd).ToDays:F1} days overdue");
                ApplyDesertionPenalties();
                return;
            }

            ProcessDeferredBagCheck();

            // Process hourly fatigue recovery (Enhanced Fatigue System - Phase 1)
            // This runs for enlisted players during rest periods (night hours)
            ProcessFatigueRecovery();

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
                var partyState = main?.Party;
                var inMapEvent = partyState?.MapEvent != null;
                var isPrisoner = Hero.MainHero?.IsPrisoner == true;
                var inEncounter = Campaign.Current?.PlayerEncounter != null;
                var isActive = main.IsActive;
                var isVisible = main.IsVisible;

                // Only skip visibility enforcement when in a MEANINGFUL encounter state:
                // - Actively in a MapEvent (battle in progress)
                // - Player is a prisoner (captivity system owns the player)
                // NOTE: We do NOT skip just because PlayerEncounter.Current exists - it can
                // be stale after battle ends (LeaveEncounter=true set but not yet processed)
                if (inMapEvent || isPrisoner)
                {
                    ModLogger.LogOnce(
                        "hourly_skip_vanilla_capture",
                        "Hourly",
                        $"Skipping visibility enforcement - vanilla owns player (MapEvent:{inMapEvent}, Prisoner:{isPrisoner})");
                    return;
                }

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

                    // If player was in reserve during disbandment, teleport to safety first
                    // This prevents spawning surrounded by enemies at the battle location
                    var wasInReserve = Combat.Behaviors.EnlistedEncounterBehavior.IsWaitingInReserve;
                    if (wasInReserve)
                    {
                        ModLogger.Info("Battle", "Player was in reserve when party disbanded - teleporting to safety");
                        Combat.Behaviors.EnlistedEncounterBehavior.ClearReserveState();
                        
                        // Clear any lingering encounter state
                        if (PlayerEncounter.Current != null)
                        {
                            PlayerEncounter.Current.IsPlayerWaiting = false;
                            PlayerEncounter.LeaveEncounter = true;
                            PlayerEncounter.Finish(true);
                        }
                        
                        // Apply protection and teleport
                        var mainParty = MobileParty.MainParty;
                        if (mainParty != null)
                        {
                            var protectionDuration = CampaignTime.Hours(12f);
                            mainParty.IgnoreByOtherPartiesTill(CampaignTime.Now + protectionDuration);
                            TryTeleportToSafety(mainParty);
                            
                            // Reactivate and make the party visible after the safety teleport
                            mainParty.IsActive = true;
                            mainParty.IsVisible = true;
                            mainParty.SetMoveModeHold(); // stop any phantom movement after disband
                        }
                        
                        // Normalize time control to a stoppable mode so reserve wait state does not leave unstoppable speed
                        if (Campaign.Current != null)
                        {
                            var normalized = Equipment.Behaviors.QuartermasterManager.NormalizeToStoppable(
                                Campaign.Current.TimeControlMode);
                            Campaign.Current.TimeControlMode = normalized;
                        }
                        
                        GameMenu.ExitToLast();
                    }

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
            // Escort into lord's party when nearby; clan expenses now native
            EncounterGuard.TryAttachOrEscort(_enlistedLord);

            // Keep the player party invisible on the map (they're part of the lord's force)
            main.IsVisible = false;

            // Ensure the player can join battles when the lord fights
            TrySetShouldJoinPlayerBattles(main, true);

            // Ignore other parties briefly to suppress stray encounters while enlisted
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
                ModLogger.ErrorCode("Following", "E-FOLLOW-001", "Error releasing escort (SetMoveModeHold)", ex);
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
                        ModLogger.ErrorCode("Battle", "E-BATTLE-001",
                            "ShouldJoinPlayerBattles property not found via reflection", null);
                    }
                }
                catch (Exception ex2)
                {
                    // Capture both failure stacks: direct set + reflection fallback.
                    var agg = new AggregateException(ex1, ex2);
                    ModLogger.ErrorCode("Battle", "E-BATTLE-002",
                        "Failed to set ShouldJoinPlayerBattles (direct + reflection)", agg);
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
            CheckProbationExpiration();

            // Check for leave expiration (14 days max)
            // This runs even when "not enlisted" because IsOnLeave makes IsEnlisted return false
            CheckLeaveExpiration();

            // Pensions are paid daily while NOT enlisted (and paused while enlisted).
            // This must run even when EnlistedActivation is inactive.
            ProcessDailyPension();

            SyncActivationState("daily_tick");
            if (!EnlistedActivation.IsActive)
            {
                return;
            }

            if (!IsEnlisted || _enlistedLord?.IsAlive != true)
            {
                return;
            }

            // Pause accrual while on leave or captive
            if (_isOnLeave || Hero.MainHero?.IsPrisoner == true)
            {
                return;
            }

            if (_nextPayday == CampaignTime.Zero)
            {
                _nextPayday = ComputeNextPayday();
            }

            try
            {
                // Accrue daily wage into muster ledger (no clan finance involvement)
                var wage = CalculateDailyWage();
                if (wage > 0 && !_payMusterPending)
                {
                    _pendingMusterPay += wage;
                }

                // Schedule pay muster when due; gate to one active incident at a time
                if (!_payMusterPending && CampaignTime.Now >= _nextPayday)
                {
                    _payMusterPending = true;
                    ModLogger.Info("Gold", $"Pay muster queued. Pending={_pendingMusterPay}, NextPayday={_nextPayday}");
                    EnlistedIncidentsBehavior.Instance?.TriggerPayMusterIncident();
                }

                // Award daily XP for military tier progression (config-driven)
                // This is separate from skill XP, which is handled by formation training and duties
                var dailyXP = Math.Max(0, EnlistedConfig.GetDailyBaseXp());
                AddEnlistmentXP(dailyXP, "Daily Service");

                ModLogger.Debug("Enlistment", $"Daily service completed: {wage} gold paid, {dailyXP} XP gained");

                // Check for retirement eligibility notification (first term complete)
                CheckRetirementEligibility();
                
                // Phase 5: Check for NPC soldier desertion when pay tension is high
                CheckNpcDesertionFromPayTension();

                // Phase 6: Check retinue provisioning status (T7+ only)
                CheckRetinueProvisioningStatus();
            }
            catch (Exception ex)
            {
                ModLogger.Error("Enlistment", "Daily service processing failed", ex);
            }
        }

        private void CheckProbationExpiration()
        {
            if (!_isOnProbation)
            {
                return;
            }

            if (_probationEnds != CampaignTime.Zero && CampaignTime.Now >= _probationEnds)
            {
                ClearProbation("duration_elapsed");
            }
        }

        private CampaignTime ComputeNextPayday()
        {
            var finance = EnlistedConfig.LoadFinanceConfig();
            var interval = finance?.PaydayIntervalDays > 0 ? finance.PaydayIntervalDays : 12;
            var jitter = finance?.PaydayJitterDays ?? 0f;
            var jitterOffset = jitter > 0f ? MBRandom.RandomFloatRanged(-jitter, jitter) : 0f;
            var days = Math.Max(1f, interval + jitterOffset);
            return CampaignTime.Now + CampaignTime.Days(days);
        }

        internal void ResolvePayMusterStandard()
        {
            if (_isPendingDischarge)
            {
                ModLogger.Info("Pay", "Resolving pay muster: final muster (pending discharge)");
                FinalizePendingDischarge();
                return;
            }

            var payout = _pendingMusterPay;
            var totalWithBackpay = payout + _owedBackpay;
            var canPayFull = CanLordAffordPay(totalWithBackpay);
            var canPayPartial = !canPayFull && CanLordAffordPartialPay(totalWithBackpay);
            var wealthStatus = GetLordWealthStatus();
            
            ModLogger.Info("Pay", $"Resolving pay muster: PendingPay={payout}, Backpay={_owedBackpay}, " +
                $"CanPayFull={canPayFull}, CanPayPartial={canPayPartial}, LordWealth={wealthStatus}");

            if (canPayFull && totalWithBackpay > 0)
            {
                // Full payment including any backpay
                ProcessFullPayment(totalWithBackpay);
            }
            else if (canPayPartial && totalWithBackpay > 0)
            {
                // Partial payment - lord pays 50% of what's owed
                ProcessPartialPayment(payout, totalWithBackpay);
            }
            else if (payout > 0)
            {
                // Lord can't afford to pay anything - accumulate backpay and tension
                ProcessPayDelay(payout);
            }
            
            _pendingMusterPay = 0;
            _nextPayday = ComputeNextPayday();
            _payMusterPending = false;
            ClearProbation("pay_muster_resolved");
            ModLogger.Info("Pay", $"Pay muster resolved: {_lastPayOutcome} (NextPayday={_nextPayday}, Tension={_payTension})");
            
            // Phase 6: Notify Schedule system to reset 12-day cycle
            Schedule.Behaviors.ScheduleBehavior.Instance?.OnPayMusterCompleted();
        }

        /// <summary>
        /// Process full payment including any backpay. Reduces tension by 30.
        /// </summary>
        private void ProcessFullPayment(int grossAmount)
        {
            // Deduct lance fund (5%)
            var lanceFundDeduction = (int)(grossAmount * 0.05f);
            _lanceFundBalance += lanceFundDeduction;
            var netPayout = grossAmount - lanceFundDeduction;
            
            GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, netPayout);
            OnWagePaid?.Invoke(netPayout);
            
            // Update pay tension state - successful full payment reduces tension by 30
            _lastPayDate = CampaignTime.Now;
            var hadBackpay = _owedBackpay > 0;
            _payTension = Math.Max(0, _payTension - 30);
            _owedBackpay = 0;
            _consecutiveDelays = 0;
            
            _lastPayOutcome = hadBackpay ? $"backpay:{netPayout}" : $"standard:{netPayout}";
            ModLogger.Info("Gold", $"Full payment: {netPayout} (gross={grossAmount}, lanceFund={lanceFundDeduction})");
            
            if (hadBackpay)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    $"Back pay received! {netPayout} denars (including owed wages).",
                    Colors.Green));
            }
        }

        /// <summary>
        /// Process partial payment - lord pays current week + 50% of backpay. Reduces tension by 10.
        /// </summary>
        private void ProcessPartialPayment(int currentPay, int totalOwed)
        {
            // Pay current week + half of backpay
            var backpayPortion = _owedBackpay / 2;
            var grossAmount = currentPay + backpayPortion;
            
            // Deduct lance fund (5%)
            var lanceFundDeduction = (int)(grossAmount * 0.05f);
            _lanceFundBalance += lanceFundDeduction;
            var netPayout = grossAmount - lanceFundDeduction;
            
            GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, netPayout);
            OnWagePaid?.Invoke(netPayout);
            
            // Update state - partial payment reduces tension by 10, halves remaining backpay
            _lastPayDate = CampaignTime.Now;
            _owedBackpay = _owedBackpay - backpayPortion;
            _payTension = Math.Max(0, _payTension - 10);
            // Don't reset consecutive delays for partial payment
            
            _lastPayOutcome = $"partial_backpay:{netPayout}";
            ModLogger.Info("Gold", $"Partial payment: {netPayout} (stillOwed={_owedBackpay})");
            
            InformationManager.DisplayMessage(new InformationMessage(
                $"Partial pay received: {netPayout} denars. Still owed: {_owedBackpay}.",
                Colors.Yellow));
        }

        /// <summary>
        /// Process pay delay - accumulate backpay and increase tension.
        /// </summary>
        private void ProcessPayDelay(int unpaidAmount)
        {
            _owedBackpay += unpaidAmount;
            _consecutiveDelays++;
            
            // Tension escalates: 10 + 5 per week overdue
            var tensionIncrease = GetPayTensionIncrease();
            _payTension = Math.Min(100, _payTension + tensionIncrease);
            
            _lastPayOutcome = $"delayed:{unpaidAmount}";
            ModLogger.Warn("Pay", $"Pay delayed: Owed={_owedBackpay}, Tension={_payTension}, Consecutive={_consecutiveDelays}");
            
            InformationManager.DisplayMessage(new InformationMessage(
                $"Pay delayed! Lord {_enlistedLord?.Name} cannot afford wages. Owed: {_owedBackpay} denars.",
                Colors.Red));
        }

        /// <summary>
        /// Write off backpay debt (used when transferring lords or lord goes completely broke).
        /// Clears owed amount and tension, but damages lord relation.
        /// </summary>
        internal void WriteOffBackpay(string reason)
        {
            if (_owedBackpay <= 0) return;
            
            var writtenOff = _owedBackpay;
            _owedBackpay = 0;
            _payTension = 0;
            _consecutiveDelays = 0;
            
            // Damage relation with lord for stiffing us
            if (_enlistedLord != null)
            {
                ChangeRelationAction.ApplyPlayerRelation(_enlistedLord, -10, false, true);
            }
            
            _lastPayOutcome = $"written_off:{writtenOff}";
            ModLogger.Warn("Pay", $"Backpay written off: {writtenOff} denars ({reason})");
            
            InformationManager.DisplayMessage(new InformationMessage(
                $"Your back pay of {writtenOff} denars has been written off. {reason}",
                Colors.Red));
        }

        internal void ResolveCorruptionMuster()
        {
            try
            {
                if (_isPendingDischarge)
                {
                    ModLogger.Info("Pay", "Resolving pay muster: final muster (pending discharge)");
                    FinalizePendingDischarge();
                    return;
                }

                ModLogger.Info("Pay", $"Resolving pay muster: corruption attempt (PendingPay={_pendingMusterPay})");

                // Fatigue cost
                if (!TryConsumeFatigue(10, "corruption"))
                {
                    _lastPayOutcome = "corruption_blocked_fatigue";
                    _payMusterPending = false;
                    _nextPayday = ComputeNextPayday();
                    ModLogger.Warn("Pay", $"Corruption muster blocked by fatigue (NextPayday={_nextPayday})");
                    return;
                }

                var roguery = Hero.MainHero.GetSkillValue(DefaultSkills.Roguery);
                var charm = Hero.MainHero.GetSkillValue(DefaultSkills.Charm);
                var best = Math.Max(roguery, charm);

                // Requirement: Roguery > 20 OR Charm > 20
                if (best <= 20)
                {
                    FailCorruption();
                }
                else
                {
                    var baseChance = 0.70f;
                    var bonus = (best - 20) * 0.005f;
                    var chance = Math.Min(0.90f, baseChance + bonus);
                    var roll = MBRandom.RandomFloat;

                    if (roll <= chance)
                    {
                        // Success: 1.20x
                        var payout = (int)(_pendingMusterPay * 1.2f);
                        if (payout > 0)
                        {
                            GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, payout);
                            OnWagePaid?.Invoke(payout);
                        }
                        _lastPayOutcome = $"corruption_success:{payout}";
                        ModLogger.Info("Pay", $"Corruption muster success (Payout={payout}, Chance={chance:0.00}, Roll={roll:0.00})");
                        if (_enlistedLord != null)
                        {
                            ChangeRelationAction.ApplyPlayerRelation(_enlistedLord, 1, true, true);
                        }
                    }
                    else
                    {
                        FailCorruption();
                        ModLogger.Info("Pay", $"Corruption muster failed (Outcome={_lastPayOutcome}, Chance={chance:0.00}, Roll={roll:0.00})");
                    }
                }

                _pendingMusterPay = 0;
                _nextPayday = ComputeNextPayday();
                _payMusterPending = false;
                ClearProbation("pay_muster_resolved");
                ModLogger.Info("Pay", $"Pay muster resolved: corruption (Outcome={_lastPayOutcome}, NextPayday={_nextPayday})");
            }
            catch (Exception ex)
            {
                ModLogger.Warn("Pay", $"Corruption muster failed: {ex.Message}");
                _payMusterPending = false;
                _nextPayday = ComputeNextPayday();
            }
        }

        private void FailCorruption()
        {
            var payout = (int)(_pendingMusterPay * 0.95f);
            if (payout > 0)
            {
                GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, payout);
                OnWagePaid?.Invoke(payout);
            }

            if (_enlistedLord != null)
            {
                ChangeRelationAction.ApplyPlayerRelation(_enlistedLord, -5, true, true);
            }

            _lastPayOutcome = $"corruption_fail:{payout}";
        }

        internal void ResolveSideDealMuster()
        {
            try
            {
                if (_isPendingDischarge)
                {
                    ModLogger.Info("Pay", "Resolving pay muster: final muster (pending discharge)");
                    FinalizePendingDischarge();
                    return;
                }

                ModLogger.Info("Pay", $"Resolving pay muster: side deal attempt (PendingPay={_pendingMusterPay})");

                // Fatigue cost
                if (!TryConsumeFatigue(6, "side_deal"))
                {
                    _lastPayOutcome = "side_deal_blocked_fatigue";
                    _payMusterPending = false;
                    _nextPayday = ComputeNextPayday();
                    ModLogger.Warn("Pay", $"Side deal muster blocked by fatigue (NextPayday={_nextPayday})");
                    return;
                }

                var dailyWage = CalculateDailyWage();
                var payout = Math.Max(0, (int)(_pendingMusterPay * 0.4f));

                // Side deal only makes sense if it produces at least ~one day's wage.
                // Otherwise, fall back to standard pay.
                if (payout <= 0 || payout < dailyWage)
                {
                    ModLogger.Info("Pay",
                        $"Side deal not worthwhile; falling back to standard (SideDeal={payout}, DailyWage={dailyWage})");
                    ResolvePayMusterStandard();
                    return;
                }

                GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, payout);
                OnWagePaid?.Invoke(payout);

                // Placeholder: if no gear picker available, just log outcome.
                _lastPayOutcome = $"side_deal:{payout}";

                _pendingMusterPay = 0;
                _nextPayday = ComputeNextPayday();
                _payMusterPending = false;
                ClearProbation("pay_muster_resolved");
                ModLogger.Info("Pay", $"Pay muster resolved: side deal (Outcome={_lastPayOutcome}, NextPayday={_nextPayday})");
            }
            catch (Exception ex)
            {
                ModLogger.Warn("Pay", $"Side deal muster failed: {ex.Message}");
                _payMusterPending = false;
                _nextPayday = ComputeNextPayday();
            }
        }

        internal void DeferPayMuster()
        {
            _payMusterPending = false;
            // Player cancelled the pay muster prompt; do NOT push the schedule out by a full interval.
            // Instead, retry soon so the player can resolve pay without losing track of it.
            _nextPayday = CampaignTime.Now + CampaignTime.Days(1f);
            ModLogger.Info("Pay", $"Pay muster deferred; retry scheduled for {_nextPayday} (Pending={_pendingMusterPay}).");
        }

        internal void ResolvePromissoryMuster()
        {
            try
            {
                if (_isPendingDischarge)
                {
                    // Promissory notes aren't used for final muster; resolve normally.
                    ResolvePayMusterStandard();
                    return;
                }

                // Keep this internal and simple:
                // - do not pay out today
                // - keep pending pay in the ledger
                // - retry soon so the player can resolve when conditions improve
                _lastPayOutcome = $"promissory:{_pendingMusterPay}";
                _payMusterPending = false;
                _nextPayday = CampaignTime.Now + CampaignTime.Days(3f);
                ModLogger.Info("Pay",
                    $"Pay muster resolved: promissory note accepted (PendingStillOwed={_pendingMusterPay}, NextRetry={_nextPayday})");
            }
            catch (Exception ex)
            {
                ModLogger.Warn("Pay", $"Promissory muster failed: {ex.Message}");
                _payMusterPending = false;
                _nextPayday = CampaignTime.Now + CampaignTime.Days(1f);
            }
        }

        public bool RequestDischarge()
        {
            if (!IsEnlisted)
            {
                return false;
            }

            _isPendingDischarge = true;
            _lastPayOutcome = "pending_discharge";
            ModLogger.Info("Retirement", "Pending discharge requested; will resolve at next pay muster");
            return true;
        }

        public bool CancelDischarge()
        {
            if (!_isPendingDischarge)
            {
                return false;
            }

            _isPendingDischarge = false;
            _lastPayOutcome = "pending_discharge_cancelled";
            ModLogger.Info("Retirement", "Pending discharge cancelled");
            return true;
        }

        private void FinalizePendingDischarge()
        {
            try
            {
                var daysServed = (int)(CampaignTime.Now - _enlistmentDate).ToDays;
                var config = EnlistedConfig.LoadRetirementConfig();
                var band = daysServed >= 200
                    ? (_enlistmentTier >= 4 ? "heroic" : "veteran")
                    : daysServed >= 100
                        ? "honorable"
                        : "washout";

                // Honorable/Veteran discharge requires at least neutral relation with the enlisted lord.
                // If relations are negative at exit, treat as washout even if days-served threshold is met.
                if ((band == "honorable" || band == "veteran") &&
                    _enlistedLord != null &&
                    Hero.MainHero.GetRelation(_enlistedLord) < 0)
                {
                    band = "washout";
                }

                var lordRelation = 0;
                var factionRelation = 0;
                var severance = 0;
                var isHonorable = false;

                switch (band)
                {
                    case "heroic":
                    case "veteran":
                        lordRelation = 30;
                        factionRelation = 15;
                        severance = Math.Max(0, config?.SeveranceVeteran ?? 3000);
                        isHonorable = true;
                        _lastDischargeBand = band;
                        break;
                    case "honorable":
                        lordRelation = 10;
                        factionRelation = 5;
                        severance = Math.Max(0, config?.SeveranceHonorable ?? 3000);
                        isHonorable = true;
                        _lastDischargeBand = "honorable";
                        break;
                    default:
                        lordRelation = -10;
                        factionRelation = -10;
                        severance = 0;
                        isHonorable = false;
                        _lastDischargeBand = "washout";
                        break;
                }

                // Apply relations
                if (_enlistedLord != null)
                {
                    ChangeRelationAction.ApplyPlayerRelation(_enlistedLord, lordRelation, true, true);
                }

                var faction = _enlistedLord?.MapFaction;
                if (faction is Kingdom kingdom && kingdom.Leader != null && kingdom.Leader != _enlistedLord)
                {
                    ChangeRelationAction.ApplyPlayerRelation(kingdom.Leader, factionRelation, true, true);
                }

                // Payout: pending muster + severance
                var payout = Math.Max(0, _pendingMusterPay) + Math.Max(0, severance);
                if (payout > 0)
                {
                    GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, payout);
                }

                ModLogger.Info("Pay",
                    $"Final muster resolved (Band={band}, DaysServed={daysServed}, PendingPay={_pendingMusterPay}, Severance={severance}, Payout={payout})");

                _pendingMusterPay = 0;
                _payMusterPending = false;
                _isPendingDischarge = false;
                _nextPayday = CampaignTime.Zero;
                _lastPayOutcome = $"final_muster:{band}:{payout}";
                _lastDischargeBand = band;

                // Snapshot reservist data for re-entry flows
                ServiceRecordManager.Instance?.RecordReservist(
                    band,
                    daysServed,
                    _enlistmentTier,
                    _enlistmentXP,
                    _enlistedLord);

                HandleGearOnDischarge(band);

                // Pension assignment handled inside StopEnlist via ApplyPensionOnDischarge
                StopEnlist($"Final muster ({band})", isHonorable);
            }
            catch (Exception ex)
            {
                ModLogger.ErrorCode("Retirement", "E-RETIRE-001", "Error finalizing discharge", ex);
                _isPendingDischarge = false;
                _payMusterPending = false;
                _pendingMusterPay = 0;
                _nextPayday = CampaignTime.Zero;
            }
        }

        internal void ResolveSmuggleDischarge()
        {
            try
            {
                if (!IsEnlisted || !_isPendingDischarge)
                {
                    return;
                }

                ModLogger.Info("Pay", "Resolving final muster: smuggle discharge (deserter)");

                var faction = _enlistedLord?.MapFaction as Kingdom;
                if (faction != null)
                {
                    ChangeCrimeRatingAction.Apply(faction, 30f, true);
                    if (faction.Leader != null)
                    {
                        ChangeRelationAction.ApplyPlayerRelation(faction.Leader, -50, true, true);
                    }
                }

                if (_enlistedLord != null)
                {
                    ChangeRelationAction.ApplyPlayerRelation(_enlistedLord, -50, true, true);
                }

                // Deserter outcome: keep all gear, clear pension
                _pensionAmountPerDay = 0;
                _pensionFactionId = null;
                _isPensionPaused = true;

                _pendingMusterPay = 0;
                _payMusterPending = false;
                _isPendingDischarge = false;
                _nextPayday = CampaignTime.Zero;
                _lastDischargeBand = "deserter";
                _lastPayOutcome = "final_muster:deserter:smuggle";

                ServiceRecordManager.Instance?.RecordReservist(
                    "deserter",
                    (int)(CampaignTime.Now - _enlistmentDate).ToDays,
                    _enlistmentTier,
                    _enlistmentXP,
                    _enlistedLord);

                StopEnlist("Smuggle discharge (deserter)", false);
            }
            catch (Exception ex)
            {
                ModLogger.ErrorCode("Pay", "E-PAY-001", "Error resolving smuggle discharge", ex);
                _isPendingDischarge = false;
                _payMusterPending = false;
                _pendingMusterPay = 0;
                _nextPayday = CampaignTime.Zero;
            }
        }

        private void ProcessDailyPension()
        {
            // Pension is only paid while NOT enlisted (no double-dipping).
            if (IsEnlisted)
            {
                return;
            }

            if (_pensionAmountPerDay <= 0 || _isPensionPaused)
            {
                return;
            }

            try
            {
                var faction = ResolvePensionFaction();
                if (faction != null)
                {
                    // Stop if at war with pension faction
                    if (Hero.MainHero?.MapFaction != null && faction.IsAtWarWith(Hero.MainHero.MapFaction))
                    {
                        _isPensionPaused = true;
                        _lastPayOutcome = "pension_paused_war";
                        return;
                    }

                    // Stop if crime rating positive (best-effort)
                    if (HasCrimeAgainstFaction(faction))
                    {
                        _isPensionPaused = true;
                        _lastPayOutcome = "pension_paused_crime";
                        return;
                    }

                    // Stop if relation below threshold
                    var config = EnlistedConfig.LoadRetirementConfig();
                    var relationStop = config?.PensionRelationStopThreshold ?? 0;
                    if (faction.Leader != null && Hero.MainHero.GetRelation(faction.Leader) < relationStop)
                    {
                        _isPensionPaused = true;
                        _lastPayOutcome = "pension_paused_relation";
                        return;
                    }
                }

                if (_pensionAmountPerDay > 0)
                {
                    GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, _pensionAmountPerDay);
                }
                _lastPayOutcome = $"pension:{_pensionAmountPerDay}";
                ModLogger.Info("Gold", $"Pension paid: {_pensionAmountPerDay} denars");
            }
            catch (Exception ex)
            {
                // Pension is processed daily while not enlisted. If this throws, pause pension to avoid log spam
                // and prevent repeating a potentially bad/partial payout.
                _isPensionPaused = true;
                _lastPayOutcome = "pension_paused_error";
                ModLogger.ErrorCode("Gold", "E-GOLD-001", "Pension processing failed; pension paused", ex);
            }
        }

        private IFaction ResolvePensionFaction()
        {
            if (string.IsNullOrWhiteSpace(_pensionFactionId))
            {
                return null;
            }

            try
            {
                // Search kingdoms first
                var kingdom = Campaign.Current?.Kingdoms?.FirstOrDefault(k => k.StringId == _pensionFactionId);
                if (kingdom != null)
                {
                    return kingdom;
                }
                // Then search clans
                var clan = Campaign.Current?.Clans?.FirstOrDefault(c => c.StringId == _pensionFactionId);
                return clan;
            }
            catch
            {
                return null;
            }
        }

        private bool HasCrimeAgainstFaction(IFaction faction)
        {
            try
            {
                if (faction == null)
                {
                    return false;
                }

                // Verified via decompile: crime rating is stored per-faction on IFaction.MainHeroCrimeRating
                return faction.MainHeroCrimeRating > 0f;
            }
            catch
            {
                // best-effort
            }

            return false;
        }

        private void HandleGearOnDischarge(string band)
        {
            try
            {
                var hero = Hero.MainHero;
                if (hero == null)
                {
                    return;
                }

                var retireConfig = EnlistedConfig.LoadRetirementConfig();
                if (retireConfig?.DebugSkipGearStripping == true)
                {
                    ModLogger.Debug("Equipment", "Gear stripping skipped (debug flag)");
                    return;
                }

                switch (band)
                {
                    case "washout":
                        ClearAllEquipment(hero);
                        break;
                    case "honorable":
                    case "veteran":
                    case "heroic":
                        // Keep armor (slots 6–9), clear weapons (0–3) and mounts (10–11) to inventory
                        MoveSlotsToInventory(hero, new[] { 0, 1, 2, 3, 10, 11 });
                        break;
                }
            }
            catch (Exception ex)
            {
                ModLogger.ErrorCode("Equipment", "E-EQUIP-004", "Gear handling on discharge failed", ex);
            }
        }

        private void MoveSlotsToInventory(Hero hero, int[] slotIndices)
        {
            if (hero?.BattleEquipment == null || slotIndices == null)
            {
                return;
            }

            var roster = hero.PartyBelongedTo?.ItemRoster ?? MobileParty.MainParty?.ItemRoster;
            var updated = hero.BattleEquipment.Clone();
            foreach (var idx in slotIndices)
            {
                if (idx < 0 || idx >= (int)EquipmentIndex.NumEquipmentSetSlots)
                {
                    continue;
                }

                var slot = (EquipmentIndex)idx;
                var elem = updated[slot];
                if (elem.Item != null)
                {
                    // Preserve modifiers/quest flags by moving the full EquipmentElement into inventory.
                    roster?.AddToCounts(elem, 1);
                    updated[slot] = default;
                }
            }

            // Apply equipment changes safely (ensures visuals refresh).
            Helpers.EquipmentHelper.AssignHeroEquipmentFromEquipment(hero, updated);
        }

        private void ClearAllEquipment(Hero hero)
        {
            try
            {
                if (hero?.BattleEquipment != null)
                {
                    for (int i = 0; i < (int)EquipmentIndex.NumEquipmentSetSlots; i++)
                    {
                        hero.BattleEquipment[(EquipmentIndex)i] = default;
                    }
                }

                if (hero?.CivilianEquipment != null)
                {
                    for (int i = 0; i < (int)EquipmentIndex.NumEquipmentSetSlots; i++)
                    {
                        hero.CivilianEquipment[(EquipmentIndex)i] = default;
                    }
                }
            }
            catch (Exception ex)
            {
                ModLogger.ErrorCode("Equipment", "E-EQUIP-005", "Failed to clear equipment", ex);
            }
        }

        private void ApplyPensionOnDischarge(bool isHonorableDischarge, int daysServed)
        {
            try
            {
                // Default: clear/paused
                _pensionAmountPerDay = 0;
                _pensionFactionId = null;
                _isPensionPaused = true;

                if (!isHonorableDischarge || daysServed <= 0)
                {
                    return;
                }

                var faction = _enlistedLord?.MapFaction;
                if (faction == null)
                {
                    ModLogger.Debug("Retirement", "No faction for pension determination");
                    return;
                }

                var config = EnlistedConfig.LoadRetirementConfig();
                var honorable = config?.PensionHonorableDaily ?? 50;
                var veteran = config?.PensionVeteranDaily ?? 100;
                var relationStop = config?.PensionRelationStopThreshold ?? 0;

                // Determine band
                var amount = daysServed >= 200 ? veteran : (daysServed >= 100 ? honorable : 0);
                if (amount <= 0)
                {
                    ModLogger.Debug("Retirement", $"No pension awarded (days served: {daysServed})");
                    return;
                }

                // Relation gate at award time
                var leader = faction.Leader;
                if (leader != null && Hero.MainHero.GetRelation(leader) < relationStop)
                {
                    ModLogger.Info("Retirement",
                        $"Pension blocked at award time due to relation {Hero.MainHero.GetRelation(leader)} < {relationStop}");
                    _lastPayOutcome = "pension_blocked_relation";
                    return;
                }

                _pensionAmountPerDay = amount;
                _pensionFactionId = faction.StringId;
                _isPensionPaused = false;
                _lastPayOutcome = $"pension_start:{amount}";

                ModLogger.Info("Retirement",
                    $"Pension started: {amount}/day (daysServed={daysServed}, faction={faction.Name})");
            }
            catch (Exception ex)
            {
                ModLogger.ErrorCode("Retirement", "E-RETIRE-002", "Error applying pension on discharge", ex);
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
            var probationMult = _isOnProbation ? Math.Max(0.01f, EnlistedConfig.LoadRetirementConfig().ProbationWageMultiplier) : 1f;
            var finalWage = Math.Min((int)(baseWage * armyMultiplier * dutiesMultiplier * probationMult), 150);
            return Math.Max(finalWage, 24); // Minimum 24 gold/day
        }

        #region Pay System (Phase 1-2)

        /// <summary>
        /// Lord's financial status affects pay reliability and amount.
        /// </summary>
        internal enum LordWealthStatus { Wealthy, Comfortable, Struggling, Poor, Broke }

        /// <summary>
        /// Get base daily pay by tier per pay_system.md spec.
        /// T1 (Levy): 3, T2 (Recruit): 6, T3 (Soldier): 10, T4 (Veteran): 16,
        /// T5 (Elite): 25, T6 (Household): 40, T7 (Lieutenant): 60, T8 (Captain): 85, T9 (Commander): 120
        /// </summary>
        private int GetBasePayByTier(int tier)
        {
            return tier switch
            {
                1 => 3,
                2 => 6,
                3 => 10,
                4 => 16,
                5 => 25,
                6 => 40,
                7 => 60,
                8 => 85,
                9 => 120,
                _ => 3
            };
        }

        /// <summary>
        /// Get culture modifier for pay. Some cultures pay better than others.
        /// Empire/Aserai: 1.1, Vlandia: 1.0, Sturgia: 0.9, Battania: 0.85, Khuzait: 0.8
        /// </summary>
        private float GetCulturePayModifier()
        {
            var cultureId = _enlistedLord?.Culture?.StringId?.ToLowerInvariant() ?? string.Empty;
            return cultureId switch
            {
                "empire" => 1.1f,
                "aserai" => 1.1f,
                "vlandia" => 1.0f,
                "sturgia" => 0.9f,
                "battania" => 0.85f,
                "khuzait" => 0.8f,
                _ => 1.0f
            };
        }

        /// <summary>
        /// Get wartime modifier for pay. Active conflict = hazard pay.
        /// Peacetime: 1.0, Active War: 1.15, Siege (defending): 1.25, Siege (attacking): 1.2
        /// </summary>
        private float GetWartimePayModifier()
        {
            var lordFaction = _enlistedLord?.MapFaction;
            if (lordFaction == null) return 1.0f;

            // Check if we're in a siege
            var settlement = Settlement.CurrentSettlement;
            if (settlement?.IsUnderSiege == true)
            {
                // Check if we're defending or attacking
                var siegeEvent = settlement.SiegeEvent;
                if (siegeEvent != null)
                {
                    var isDefending = siegeEvent.BesiegerCamp?.LeaderParty?.MapFaction != lordFaction;
                    return isDefending ? 1.25f : 1.2f;
                }
            }

            // Check if faction is at war with anyone
            foreach (var otherFaction in Campaign.Current.Factions)
            {
                if (otherFaction != lordFaction && FactionManager.IsAtWarAgainstFaction(lordFaction, otherFaction))
                {
                    return 1.15f; // Active war hazard pay
                }
            }

            return 1.0f; // Peacetime
        }

        /// <summary>
        /// Get lord wealth pay modifier. Wealthy lords pay more, poor lords pay less.
        /// Wealthy (>50k): 1.1, Comfortable: 1.0, Struggling: 0.9, Poor: 0.75, Broke: 0.5
        /// </summary>
        private float GetLordWealthPayModifier()
        {
            var status = GetLordWealthStatus();
            return status switch
            {
                LordWealthStatus.Wealthy => 1.1f,
                LordWealthStatus.Comfortable => 1.0f,
                LordWealthStatus.Struggling => 0.9f,
                LordWealthStatus.Poor => 0.75f,
                LordWealthStatus.Broke => 0.5f,
                _ => 1.0f
            };
        }

        /// <summary>
        /// Check if the lord has enough gold to pay the player.
        /// Lords keep a buffer so we don't drain them completely.
        /// </summary>
        private bool CanLordAffordPay(int amount)
        {
            var lord = _enlistedLord;
            if (lord == null) return false;

            // Lord needs to keep a buffer - don't drain them to zero
            const int minimumBuffer = 500;
            return lord.Gold >= amount + minimumBuffer;
        }

        /// <summary>
        /// Check if lord can afford partial pay (at least 50%).
        /// </summary>
        private bool CanLordAffordPartialPay(int amount)
        {
            var lord = _enlistedLord;
            if (lord == null) return false;

            const int minimumBuffer = 200;
            return lord.Gold >= (amount / 2) + minimumBuffer;
        }

        /// <summary>
        /// Get lord's financial status based on gold thresholds.
        /// Used for pay reliability and potential pay reduction.
        /// </summary>
        private LordWealthStatus GetLordWealthStatus()
        {
            var gold = _enlistedLord?.Gold ?? 0;
            if (gold > 50000) return LordWealthStatus.Wealthy;
            if (gold > 20000) return LordWealthStatus.Comfortable;
            if (gold > 5000) return LordWealthStatus.Struggling;
            if (gold > 1000) return LordWealthStatus.Poor;
            return LordWealthStatus.Broke;
        }

        /// <summary>
        /// Calculate fully-modified daily pay using tier base + all modifiers.
        /// </summary>
        internal int CalculateModifiedDailyPay()
        {
            var basePay = GetBasePayByTier(_enlistmentTier);
            var dutyMod = GetDutiesWageMultiplier();
            var cultureMod = GetCulturePayModifier();
            var wealthMod = GetLordWealthPayModifier();
            var wartimeMod = GetWartimePayModifier();

            var totalPay = basePay * dutyMod * cultureMod * wealthMod * wartimeMod;
            return Math.Max(1, (int)Math.Round(totalPay));
        }

        /// <summary>
        /// Clear pay tension state when service ends or transfers.
        /// </summary>
        private void ClearPayState(string reason)
        {
            _payTension = 0;
            _owedBackpay = 0;
            _lastPayDate = CampaignTime.Zero;
            _consecutiveDelays = 0;
            // Note: Lance fund is NOT cleared - it belongs to the lance, not the player
            ModLogger.Debug("Pay", $"Pay state cleared: {reason}");
        }

        /// <summary>
        /// Calculate pay tension increase based on weeks overdue.
        /// Base 10 + 5 per week overdue, per pay_tension_events.md spec.
        /// </summary>
        private int GetPayTensionIncrease()
        {
            if (_lastPayDate == CampaignTime.Zero) return 10;
            var weeksOverdue = Math.Max(0, (DaysSincePay - 7) / 7);
            return 10 + (weeksOverdue * 5);
        }

        #endregion

        #region Battle Loot Share (Phase 4 Pay System)

        /// <summary>
        /// Get gold share percentage based on tier per pay_system.md spec.
        /// T1-T6: Percentage of troop pool (after lord's 50% take)
        /// T7-T9: Percentage of total loot (before lord's take)
        /// </summary>
        private float GetLootSharePercent(int tier)
        {
            return tier switch
            {
                1 => 0.05f,  // 5%
                2 => 0.10f,  // 10%
                3 => 0.10f,  // 10%
                4 => 0.15f,  // 15%
                5 => 0.15f,  // 15%
                6 => 0.15f,  // 15%
                7 => 0.10f,  // 10% of total (commander)
                8 => 0.15f,  // 15% of total (commander)
                9 => 0.20f,  // 20% of total (commander)
                _ => 0.05f
            };
        }

        /// <summary>
        /// Calculate and award battle loot share based on battle outcome.
        /// Called from OnMapEventEnded when player participated in a victory.
        /// </summary>
        internal void AwardBattleLootShare(MapEvent mapEvent, bool playerWon)
        {
            try
            {
                if (!IsEnlisted || _enlistedLord == null)
                {
                    return;
                }

                if (!playerWon)
                {
                    ModLogger.Debug("Pay", "No loot share - battle lost");
                    return;
                }

                // Estimate battle loot value based on enemy casualties
                // Native doesn't expose loot value directly, so we estimate based on killed troops
                var enemySide = mapEvent.PlayerSide == BattleSideEnum.Defender 
                    ? BattleSideEnum.Attacker 
                    : BattleSideEnum.Defender;
                    
                var enemyCasualties = mapEvent.GetMapEventSide(enemySide)?.TroopCasualties ?? 0;
                
                // Estimate loot value: ~20 denars per enemy killed (rough average)
                var estimatedTotalLoot = enemyCasualties * 20;
                
                if (estimatedTotalLoot <= 0)
                {
                    ModLogger.Debug("Pay", "No loot share - no enemy casualties");
                    return;
                }

                var tier = _enlistmentTier;
                var sharePercent = GetLootSharePercent(tier);
                
                // T1-T6 get share of troop pool (50% of total after lord's take)
                // T7-T9 get share of total loot (commander privilege)
                var lootPool = tier >= 7 ? estimatedTotalLoot : (int)(estimatedTotalLoot * 0.5f);
                var goldEarned = (int)(lootPool * sharePercent);
                
                // Battle bonuses: Victory bonus (5-20 based on battle size)
                var battleSizeBonus = Math.Min(20, 5 + (enemyCasualties / 10));
                
                var totalReward = goldEarned + battleSizeBonus;
                
                if (totalReward <= 0)
                {
                    return;
                }

                // Award gold
                GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, totalReward);
                
                ModLogger.Info("Pay", $"Battle loot share: {goldEarned} (share) + {battleSizeBonus} (victory) = {totalReward} gold (T{tier}, {enemyCasualties} casualties)");
                
                // Notify player
                InformationManager.DisplayMessage(new InformationMessage(
                    $"Battle spoils: {totalReward} denars ({goldEarned} loot share + {battleSizeBonus} victory bonus)",
                    Colors.Green));
            }
            catch (Exception ex)
            {
                ModLogger.ErrorCode("Pay", "E-PAY-002", "Error awarding battle loot share", ex);
            }
        }

        #endregion

        #region PayTension Effects (Phase 5 Pay System)

        /// <summary>
        /// Get morale penalty based on PayTension level.
        /// Higher tension = lower morale as soldiers get desperate.
        /// </summary>
        public int GetPayTensionMoralePenalty()
        {
            if (_payTension < 20) return 0;
            if (_payTension < 40) return -3;
            if (_payTension < 60) return -6;
            if (_payTension < 80) return -10;
            return -15;
        }

        /// <summary>
        /// Get discipline incident chance modifier based on PayTension.
        /// Higher tension = more likely discipline problems.
        /// </summary>
        public float GetPayTensionDisciplineModifier()
        {
            if (_payTension < 40) return 0f;
            if (_payTension < 60) return 0.05f;  // +5% chance
            if (_payTension < 80) return 0.10f;  // +10% chance
            return 0.20f;  // +20% chance
        }

        /// <summary>
        /// Check if free desertion is available (no penalty at 60+ tension).
        /// When pay is severely delayed, the lord understands if soldiers leave.
        /// </summary>
        public bool IsFreeDesertionAvailable => _payTension >= 60 && IsEnlisted;

        /// <summary>
        /// Reduces PayTension by the specified amount.
        /// Used when the player helps the lord (Phase 8).
        /// </summary>
        /// <param name="amount">Amount to reduce (positive value)</param>
        public void ReducePayTension(int amount)
        {
            if (amount <= 0 || !IsEnlisted)
            {
                return;
            }

            var oldTension = _payTension;
            _payTension = Math.Max(0, _payTension - amount);

            ModLogger.Info("Pay", $"PayTension reduced: {oldTension} -> {_payTension} (by {amount})");

            if (_payTension < 40 && oldTension >= 40)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    new TextObject("{=hlm_tension_stabilized}The lord's financial situation has stabilized.").ToString(),
                    Colors.Green));
            }
        }

        /// <summary>
        /// Process free desertion when PayTension is 60+.
        /// Minimal penalties compared to normal desertion.
        /// </summary>
        public void ProcessFreeDesertion()
        {
            if (!IsFreeDesertionAvailable)
            {
                ModLogger.Warn("Pay", "Free desertion not available - tension below 60");
                return;
            }

            try
            {
                // Minimal relation penalty - lord understands
                if (_enlistedLord != null)
                {
                    ChangeRelationAction.ApplyPlayerRelation(_enlistedLord, -5, false, true);
                }

                // No bounty, no faction penalty
                ModLogger.Info("Pay", $"Processing free desertion (PayTension={_payTension})");

                // End enlistment cleanly
                StopEnlist("free_desertion_pay_crisis", isHonorableDischarge: false);

                InformationManager.DisplayMessage(new InformationMessage(
                    "You leave quietly. No one blames you — you weren't paid.",
                    Colors.Yellow));
            }
            catch (Exception ex)
            {
                ModLogger.ErrorCode("Pay", "E-PAY-003", "Error processing free desertion", ex);
            }
        }

        /// <summary>
        /// Check for NPC soldier desertion due to high pay tension.
        /// Only applies to commanders (T7+) with retinue.
        /// Called daily when tension is 60+.
        /// </summary>
        internal void CheckNpcDesertionFromPayTension()
        {
            try
            {
                // Only commanders have troops to desert
                if (_enlistmentTier < 7 || _payTension < 60)
                {
                    return;
                }

                var mainParty = MobileParty.MainParty;
                if (mainParty?.MemberRoster == null || mainParty.MemberRoster.TotalManCount <= 1)
                {
                    return;
                }

                // Desertion chance scales with tension
                float desertionChance = _payTension switch
                {
                    >= 90 => 0.05f,  // 5% per soldier
                    >= 80 => 0.03f,  // 3% per soldier
                    >= 60 => 0.01f,  // 1% per soldier
                    _ => 0f
                };

                var roster = mainParty.MemberRoster;
                var desertersTotal = 0;

                // Check each troop type (not heroes)
                for (int i = roster.Count - 1; i >= 0; i--)
                {
                    var element = roster.GetElementCopyAtIndex(i);
                    if (element.Character?.IsHero == true) continue;

                    var count = element.Number;
                    var deserters = 0;

                    for (int j = 0; j < count; j++)
                    {
                        if (MBRandom.RandomFloat < desertionChance)
                        {
                            deserters++;
                        }
                    }

                    if (deserters > 0)
                    {
                        roster.AddToCounts(element.Character, -deserters);
                        desertersTotal += deserters;
                    }
                }

                if (desertersTotal > 0)
                {
                    ModLogger.Info("Pay", $"NPC desertion: {desertersTotal} soldiers left due to unpaid wages (tension={_payTension})");
                    InformationManager.DisplayMessage(new InformationMessage(
                        $"{desertersTotal} soldier(s) deserted due to unpaid wages!",
                        Colors.Red));
                }
            }
            catch (Exception ex)
            {
                ModLogger.ErrorCode("Pay", "E-PAY-004", "Error checking NPC desertion from pay tension", ex);
            }
        }

        #endregion

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
                if (!_loggedWageBreakdownFailure)
                {
                    _loggedWageBreakdownFailure = true;
                    ModLogger.ErrorCode("Wage", "E-WAGE-001", "Failed to calculate wage breakdown (using fallback values)", ex);
                }
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
                    // Return the first active duty for display
                    foreach (var duty in activeDuties)
                    {
                        return FormatDutyName(duty);
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
                // Phase 4: temporary promotion block at very high discipline risk.
                // This is a relief-valve-friendly block: XP keeps accumulating and promotion will resume
                // once discipline improves below the threshold.
                var escalation = EscalationManager.Instance;
                if (escalation?.IsEnabled() == true &&
                    escalation.State != null &&
                    escalation.State.Discipline >= EscalationThresholds.DisciplineBlocked)
                {
                    var show = _lastPromotionBlockedMessageTime == CampaignTime.Zero ||
                               (CampaignTime.Now - _lastPromotionBlockedMessageTime).ToDays >= 1f;
                    if (show)
                    {
                        _lastPromotionBlockedMessageTime = CampaignTime.Now;
                        InformationManager.DisplayMessage(new InformationMessage(
                            new TextObject("{=enlist_promotion_blocked_discipline}Promotion blocked: your discipline is under review. Reduce discipline risk before advancing.").ToString(),
                            Colors.Yellow));
                    }
                    break;
                }

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
            if (_baggageStash == null)
            {
                _baggageStash = new ItemRoster();
            }
            _bagCheckInProgress = false;

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

            ValidateLanceStateOnLoad();
            
            // Phase 7: Migrate tracking fields for existing saves
            MigratePhase7TrackingFields();

            ModLogger.Info("SaveLoad", $"Validated enlistment state - Tier: {_enlistmentTier}, XP: {_enlistmentXP}");
        }
        
        /// <summary>
        ///     Phase 7: Migrate promotion tracking fields for existing saves.
        ///     Initializes days in rank, events completed, and battles survived with reasonable estimates.
        /// </summary>
        private void MigratePhase7TrackingFields()
        {
            if (!IsEnlisted)
            {
                return;
            }
            
            try
            {
                var migrated = false;

                // If lastPromotionDate is not set but player is T2+, estimate from enlistment date
                if (_lastPromotionDate == CampaignTime.Zero && _enlistmentTier > 1)
                {
                    // Assume player was promoted fairly recently (within tier * 7 days)
                    var estimatedDaysSincePromotion = Math.Max(7, (_enlistmentTier - 1) * 7);
                    _lastPromotionDate = CampaignTime.Now - CampaignTime.Days(estimatedDaysSincePromotion);
                    ModLogger.Info("SaveLoad", $"Migration: Estimated lastPromotionDate to {estimatedDaysSincePromotion} days ago");
                    migrated = true;
                }
                
                // If T1 and lastPromotionDate not set, use enlistment date
                if (_lastPromotionDate == CampaignTime.Zero && _enlistmentTier == 1)
                {
                    _lastPromotionDate = _enlistmentDate != CampaignTime.Zero ? _enlistmentDate : CampaignTime.Now;
                    ModLogger.Info("SaveLoad", "Migration: Set lastPromotionDate to enlistment date for T1 player");
                    migrated = true;
                }
                
                // If events/battles are 0 but player is T2+, estimate based on tier
                // Players typically complete 2-3 events and survive 1-2 battles per tier
                if (_eventsCompleted == 0 && _enlistmentTier > 1)
                {
                    _eventsCompleted = (_enlistmentTier - 1) * 2; // 2 events per tier
                    ModLogger.Info("SaveLoad", $"Migration: Estimated eventsCompleted to {_eventsCompleted}");
                    migrated = true;
                }
                
                if (_battlesSurvived == 0 && _enlistmentTier > 1)
                {
                    _battlesSurvived = (_enlistmentTier - 1); // 1 battle per tier
                    ModLogger.Info("SaveLoad", $"Migration: Estimated battlesSurvived to {_battlesSurvived}");
                    migrated = true;
                }

                if (migrated)
                {
                    // Non-spammy summary marker for support: confirms a migration occurred in this session.
                    ModLogger.LogOnce("enlistment_migrate_phase7_tracking", "SaveLoad",
                        "[E-SAVELOAD-002] Applied Phase 7 tracking migration for an existing save.");
                }
            }
            catch (Exception ex)
            {
                ModLogger.ErrorCode("SaveLoad", "E-SAVELOAD-003", "Phase 7 tracking migration failed", ex);
            }
        }

        private void TryPauseForSettlementEntry()
        {
            try
            {
                if (Campaign.Current != null && Campaign.Current.TimeControlMode != CampaignTimeControlMode.Stop)
                {
                    Campaign.Current.TimeControlMode = CampaignTimeControlMode.Stop;
                }
            }
            catch (Exception ex)
            {
                ModLogger.ErrorCode("Settlement", "E-SETTLEMENT-001", "Failed to pause time on settlement entry", ex);
            }
        }

        private void ValidateLanceStateOnLoad()
        {
            _currentLanceId ??= string.Empty;
            _currentLanceName ??= string.Empty;
            _currentLanceStyle ??= string.Empty;
            _currentLanceStoryId ??= string.Empty;
            _manualLanceStyleId ??= string.Empty;
            _savedGraceLanceId ??= string.Empty;
            _savedGraceLanceName ??= string.Empty;
            _savedGraceLanceStyle ??= string.Empty;
            _savedGraceLanceStoryId ??= string.Empty;
            _savedGraceManualLanceStyleId ??= string.Empty;
            if (_fatigueMax <= 0)
            {
                _fatigueMax = 24;
            }
            if (_fatigueCurrent <= 0 || _fatigueCurrent > _fatigueMax)
            {
                _fatigueCurrent = _fatigueMax;
            }

            // Resolve legacy lance ids against current config to refresh name/story when possible
            if (Assignments.Core.ConfigurationManager.LoadLancesConfig()?.LancesEnabled == true &&
                !string.IsNullOrWhiteSpace(_currentLanceId))
            {
                var resolved = LanceRegistry.ResolveLanceById(_currentLanceId);
                if (resolved != null)
                {
                    _currentLanceName = resolved.Name ?? _currentLanceName;
                    _currentLanceStyle = resolved.StyleId ?? _currentLanceStyle;
                    _currentLanceStoryId = resolved.StoryId ?? _currentLanceStoryId;
                    _isLanceLegacy = false;
                }
                else
                {
                    // Mark as legacy but keep saved name for display
                    _isLanceLegacy = true;
                }
            }
        }

        /// <summary>
        ///     Removes duplicate hero entries from player's roster that can occur after escape from captivity.
        ///     Also cleans up stale references to player companions in other parties (e.g., lord's party).
        /// </summary>
        private void DeduplicateRosterHeroes()
        {
            try
            {
                var main = MobileParty.MainParty;
                if (main == null)
                {
                    return;
                }

                var mainRoster = main.MemberRoster;
                var duplicatesRemoved = 0;
                var staleRefsRemoved = 0;

                // Track heroes we've seen in main roster to detect duplicates
                var seenHeroes = new HashSet<CharacterObject>();
                var heroesToRemove = new List<(CharacterObject character, int excess)>();

                foreach (var troop in mainRoster.GetTroopRoster())
                {
                    if (!troop.Character.IsHero)
                    {
                        continue;
                    }

                    // Heroes should only appear once in roster (count = 1)
                    if (troop.Number > 1)
                    {
                        heroesToRemove.Add((troop.Character, troop.Number - 1));
                    }
                    else if (seenHeroes.Contains(troop.Character))
                    {
                        // Duplicate entry - shouldn't happen but clean up
                        heroesToRemove.Add((troop.Character, troop.Number));
                    }
                    else
                    {
                        seenHeroes.Add(troop.Character);
                    }
                }

                // Remove duplicate hero entries
                foreach (var (character, excess) in heroesToRemove)
                {
                    mainRoster.AddToCounts(character, -excess, false, 0, 0, true, -1);
                    duplicatesRemoved += excess;
                    ModLogger.Warn("SaveLoad", $"Removed {excess} duplicate entry for hero {character.Name}");
                }

                // Clean up stale companion references in lord's party (if enlisted)
                if (_enlistedLord?.PartyBelongedTo != null)
                {
                    var lordRoster = _enlistedLord.PartyBelongedTo.MemberRoster;
                    var staleCompanions = new List<TroopRosterElement>();

                    foreach (var troop in lordRoster.GetTroopRoster())
                    {
                        if (!troop.Character.IsHero || troop.Character == CharacterObject.PlayerCharacter)
                        {
                            continue;
                        }

                        var hero = troop.Character.HeroObject;
                        if (hero != null && hero.Clan == Clan.PlayerClan)
                        {
                            // Companion is in lord's party - check if also in player's party (stale ref)
                            if (mainRoster.GetTroopCount(troop.Character) > 0)
                            {
                                staleCompanions.Add(troop);
                            }
                        }
                    }

                    foreach (var stale in staleCompanions)
                    {
                        lordRoster.AddToCounts(stale.Character, -stale.Number, false, 0, 0, true, -1);
                        staleRefsRemoved++;
                        ModLogger.Warn("SaveLoad", $"Removed stale companion ref {stale.Character.Name} from lord's party");
                    }
                }

                if (duplicatesRemoved > 0 || staleRefsRemoved > 0)
                {
                    ModLogger.Info("SaveLoad",
                        $"Roster cleanup: removed {duplicatesRemoved} duplicates, {staleRefsRemoved} stale refs");
                }
            }
            catch (Exception ex)
            {
                ModLogger.ErrorCode("SaveLoad", "E-SAVELOAD-003", "Error during roster deduplication", ex);
            }
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

                    // Activate enlisted menu after state restoration (deferred to next frame)
                    // This ensures the player sees their status menu when loading a save while enlisted
                    NextFrameDispatcher.RunNextFrame(() =>
                    {
                        if (IsEnlisted && !_isOnLeave && !Hero.MainHero.IsPrisoner)
                        {
                            EnlistedMenuBehavior.SafeActivateEnlistedMenu();
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                ModLogger.ErrorCode("SaveLoad", "E-SAVELOAD-004", "Error in RestorePartyStateAfterLoad", ex);
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
                            ModLogger.ErrorCode("EventSafety", "E-EVENTSAFE-001",
                                "Could not find EndCaptivityAction.ApplyByEscape", null);
                        }
                    }
                    catch (Exception ex)
                    {
                        ModLogger.ErrorCode("EventSafety", "E-EVENTSAFE-002", "Error forcing player escape", ex);
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
                if (mainParty != null && mainHero != null)
                {
                    var partyState = mainParty.Party;
                    var inMapEvent = partyState?.MapEvent != null;
                    var isPrisoner = mainHero.IsPrisoner;
                    var inEncounter = Campaign.Current?.PlayerEncounter != null;
                    var isActive = mainParty.IsActive;
                    var isVisible = mainParty.IsVisible;

                    // CAPTIVITY TRACKING: Detect when player is released from captivity
                    // This helps debug issues where players get attacked immediately after release
                    if (_wasPrisonerLastTick && !isPrisoner)
                    {
                        // Player just released from captivity!
                        ModLogger.Info("Captivity", 
                            $"Player RELEASED from captivity - IsActive:{isActive}, IsVisible:{isVisible}, InMapEvent:{inMapEvent}, InEncounter:{inEncounter}, InGrace:{IsInDesertionGracePeriod}");
                        
                        // ALWAYS apply protection on captivity release - player shouldn't be attacked immediately
                        // This applies regardless of whether we're in grace period or not
                        if (mainParty != null)
                        {
                            var protectionDuration = CampaignTime.Hours(12f);
                            var protectionUntil = CampaignTime.Now + protectionDuration;
                            
                            // Native protection - prevents parties from targeting us
                            mainParty.IgnoreByOtherPartiesTill(protectionUntil);
                            
                            // Mod-level protection - our EncounterSuppressionPatch will block encounters
                            _graceProtectionEnds = protectionUntil;
                            
                            ModLogger.Info("Captivity", 
                                $"Applied 1-day protection after captivity release (until {protectionUntil})");
                        }
                    }
                    _wasPrisonerLastTick = isPrisoner;
                    
                    // Only skip visibility enforcement when in a MEANINGFUL encounter state:
                    // - Actively in a MapEvent (battle in progress)
                    // - Player is a prisoner (captivity system owns the player)
                    // NOTE: We do NOT skip just because PlayerEncounter.Current exists - it can
                    // be stale after battle ends (LeaveEncounter=true set but not yet processed)
                    if (inMapEvent || isPrisoner)
                    {
                        ModLogger.LogOnce(
                            "realtime_skip_vanilla_capture",
                            "Realtime",
                            $"Skipping visibility enforcement - vanilla owns player (MapEvent:{inMapEvent}, Prisoner:{isPrisoner})");
                        return;
                    }

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
                                    ModLogger.ErrorCode("Battle", "E-BATTLE-003", "Error joining lord's army", ex);
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
                                        var mapFaction = _enlistedLord?.MapFaction;
                                        bool isNonKingdomFactionLord = mapFaction != null &&
                                                                       !(mapFaction is Kingdom) &&
                                                                       (mapFaction.IsMinorFaction || mapFaction.IsBanditFaction);
                                        
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
                                            // Naval battles - join MapEventSide so Init() works
                                            // NavalBattleShipAssignmentPatch assigns ship from lord's fleet (enlisted players have no ships)
                                            mainParty.Party.MapEventSide = targetSide;
                                            ModLogger.Info("Naval",
                                                $"Naval battle detected - joined MapEventSide on {lordSide} side");
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
                                ModLogger.ErrorCode("Battle", "E-BATTLE-004", "Error in battle participation setup", ex);
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
                                    ModLogger.ErrorCode("Siege", "E-SIEGE-001",
                                        "Error finishing PlayerEncounter when entering settlement", ex);
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
                ModLogger.ErrorCode("Battle", "E-BATTLE-005", "Failed to sync besieger camp", ex);
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
                ModLogger.ErrorCode("PositionSync", "E-POSSYNC-001", "Error in aggressive position sync", ex);
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
                ModLogger.ErrorCode("Naval", "E-NAVAL-001", "Failed to sync naval position", ex);
                return false;
            }
        }


        /// <summary>
        ///     Teleports the player to a safe location just outside engagement range.
        ///     Uses the game's navigation mesh to ensure the destination is valid terrain (not water/mountains).
        ///     For naval scenarios, falls back to finding the nearest friendly/neutral port.
        ///     Called when lord is captured while player is in reserve, so player doesn't spawn
        ///     surrounded by enemies at the battle location.
        /// </summary>
        /// <param name="mainParty">The player's party to teleport.</param>
        /// <param name="threatParty">Optional threat party to escape from.</param>
        private void TryTeleportToSafety(MobileParty mainParty, PartyBase threatParty = null)
        {
            try
            {
                if (mainParty == null)
                {
                    return;
                }

                // Determine reference point (threat or lord position) to escape from
                CampaignVec2 referencePosition;
                var threatName = "battle";

                if (threatParty?.MobileParty != null)
                {
                    referencePosition = threatParty.MobileParty.Position;
                    threatName = threatParty.Name?.ToString() ?? "enemy";
                }
                else if (_enlistedLord?.PartyBelongedTo != null)
                {
                    referencePosition = _enlistedLord.PartyBelongedTo.Position;
                    threatName = _enlistedLord.Name?.ToString() ?? "lord";
                }
                else
                {
                    // No reference - use player's current position as center
                    referencePosition = mainParty.Position;
                }

                // Find a safe, reachable point just outside engagement range (12-20 map units)
                // NavigationHelper validates the position is on valid terrain (not water/mountains)
                // and that there's a navigable path to reach it
                var safePosition = Helpers.NavigationHelper.FindPointAroundPosition(
                    referencePosition,
                    MobileParty.NavigationType.Default, // Land navigation
                    maxDistance: 20f,
                    minDistance: 12f,
                    requirePath: true,
                    useUniformDistribution: false
                );

                // Verify we got a valid different position
                if (safePosition.IsValid() && safePosition != referencePosition)
                {
                    mainParty.Position = safePosition;
                    mainParty.SetMoveModeHold();

                    ModLogger.Info("Battle",
                        $"Teleported player to safe distance from {threatName} (validated terrain)");

                    var message = new TextObject("{=Enlisted_Message_EscapedToSafety}You have escaped to a safe distance.");
                    InformationManager.DisplayMessage(new InformationMessage(message.ToString()));
                }
                else
                {
                    // Fallback for naval scenarios or when no nearby land: find nearest friendly/neutral port
                    var port = FindNearestFriendlyPort(mainParty.Position);
                    if (port != null)
                    {
                        mainParty.Position = port.GatePosition;
                        mainParty.SetMoveModeHold();

                        ModLogger.Info("Battle",
                            $"Teleported player to {port.Name} after naval defeat (no nearby land found)");

                        var message = new TextObject("{=Enlisted_Message_EscapedToPort}You have washed ashore at {PORT}.");
                        message.SetTextVariable("PORT", port.Name);
                        InformationManager.DisplayMessage(new InformationMessage(message.ToString()));
                    }
                    else
                    {
                        ModLogger.Warn("Battle",
                            $"Could not find valid terrain or port for safety teleport near {threatName} - player stays in place");
                    }
                }
            }
            catch (Exception ex)
            {
                ModLogger.ErrorCode("Battle", "E-BATTLE-006", "Failed to teleport player to safety", ex);
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
                ModLogger.ErrorCode("Naval", "E-NAVAL-002", "Failed to teleport stranded player to nearest port", ex);

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

        /// <summary>
        ///     Finds the nearest friendly or neutral settlement with a port.
        ///     Used for naval defeat scenarios where the player needs to "wash ashore" somewhere safe.
        /// </summary>
        /// <param name="fromPosition">The position to search from.</param>
        /// <returns>The nearest friendly/neutral port settlement, or null if none found.</returns>
        private Settlement FindNearestFriendlyPort(CampaignVec2 fromPosition)
        {
            var playerFaction = Hero.MainHero?.MapFaction;
            Settlement bestPort = null;
            var bestDistance = float.MaxValue;

            foreach (var settlement in Settlement.All)
            {
                if (settlement == null || settlement.IsHideout)
                {
                    continue;
                }

                // Skip settlements without ports (for naval scenarios)
                // Also accept any town/castle for land-based fallback
                var isPort = settlement.HasPort;
                var isSafeSettlement = settlement.IsTown || settlement.IsCastle;
                
                if (!isPort && !isSafeSettlement)
                {
                    continue;
                }

                // Skip enemy settlements
                if (playerFaction != null && settlement.MapFaction != null)
                {
                    if (FactionManager.IsAtWarAgainstFaction(playerFaction, settlement.MapFaction))
                    {
                        continue;
                    }
                }

                var targetPosition = isPort ? settlement.PortPosition : settlement.GatePosition;
                var distance = fromPosition.Distance(targetPosition);

                // Prefer ports for naval scenarios
                if (isPort)
                {
                    distance *= 0.8f; // 20% distance bonus for ports
                }

                // Prefer towns over castles (more services)
                if (settlement.IsTown)
                {
                    distance *= 0.9f; // 10% distance bonus for towns
                }

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestPort = settlement;
                }
            }

            return bestPort;
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
        ///     Fired when a lance is finalized (non-provisional). Args: lanceId, styleId, storyId.
        /// </summary>
        public static event Action<string, string, string> OnLanceFinalized;

        /// <summary>
        ///     Fired when player is promoted. Passes the new tier number.
        /// </summary>
        public static event Action<int> OnPromoted;

        /// <summary>
        ///     Current rank title for story systems (culture-specific from progression_config).
        ///     Returns the rank name appropriate for the current lord's culture.
        /// </summary>
        public string CurrentRankTitle
        {
            get
            {
                var cultureId = CurrentLord?.Culture?.StringId ?? 
                               CurrentLord?.Clan?.Kingdom?.Culture?.StringId ?? 
                               "mercenary";
                return Assignments.Core.ConfigurationManager.GetCultureRankTitle(_enlistmentTier, cultureId);
            }
        }

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
        ///     Whether the player has served the minimum configured days required for first-term retirement.
        /// </summary>
        public bool IsEligibleForRetirement
        {
            get
            {
                var config = EnlistedConfig.LoadRetirementConfig();
                var requiredDays = config?.FirstTermDays > 0 ? config.FirstTermDays : 252;
                return IsEnlisted && DaysServed >= requiredDays && !IsInRenewalTerm;
            }
        }

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
        ///     Checks if the player has reached first-term retirement eligibility (configurable days).
        ///     Shows a one-time notification when first eligible.
        /// </summary>
        private void CheckRetirementEligibility()
        {
            if (_retirementNotificationShown || !IsEnlisted || _isPendingDischarge)
            {
                return;
            }

            var config = EnlistedConfig.LoadRetirementConfig();
            if (DaysServed >= config.FirstTermDays)
            {
                _retirementNotificationShown = true;

                var message =
                    new TextObject(
                        "{=Enlisted_Message_TermCompleted}You have completed your term of service! Use the Camp menu to request a managed discharge (Final Muster).");
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
                ModLogger.ErrorCode("Retirement", "E-RETIRE-005", "Cannot process first-term retirement - not eligible",
                    new InvalidOperationException("Generated diagnostic exception to capture stack trace."));
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
                ModLogger.ErrorCode("Retirement", "E-RETIRE-006", "Cannot process renewal retirement - not in renewal term",
                    new InvalidOperationException("Generated diagnostic exception to capture stack trace."));
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
                ModLogger.ErrorCode("Retirement", "E-RETIRE-007", "Cannot start renewal term - not enlisted",
                    new InvalidOperationException("Generated diagnostic exception to capture stack trace."));
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
                ModLogger.ErrorCode("Retirement", "E-RETIRE-008", "Cannot re-enlist - not eligible or wrong faction",
                    new InvalidOperationException("Generated diagnostic exception to capture stack trace."));
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
                    ModLogger.ErrorCode("Retirement", "E-RETIRE-009", "Cannot apply relation bonuses - no faction",
                        new InvalidOperationException("Generated diagnostic exception to capture stack trace."));
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
                ModLogger.ErrorCode("Retirement", "E-RETIRE-003", "Error applying relation bonuses", ex);
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
                    ModLogger.ErrorCode("Retirement", "E-RETIRE-010", "Cannot apply subsequent bonuses - no faction",
                        new InvalidOperationException("Generated diagnostic exception to capture stack trace."));
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
                ModLogger.ErrorCode("Retirement", "E-RETIRE-004", "Error applying subsequent re-enlistment bonuses", ex);
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
                var captorName = capturingParty?.LeaderHero?.Name?.ToString() ?? capturingParty?.Name?.ToString() ?? "unknown";
                ModLogger.Info("Captivity", $"Captured by {captorName} - service ended, grace period started");
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

                // Handle player state cleanup (reserve mode, encounters, etc.)
                // This doesn't capture the player - if player was fighting, native surrender flow handles capture
                // If player was in reserve, they escape to safety
                HandlePlayerStateOnLordCapture(capturingParty);

                // Start grace period instead of immediate discharge
                // Player has 14 days to rejoin another lord in the same kingdom
                var lordKingdom = _enlistedLord.MapFaction as Kingdom;
                if (lordKingdom != null)
                {
                    if (_playerCaptureCleanupScheduled)
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
                    if (_playerCaptureCleanupScheduled)
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
            // EXCEPTION: If _isProcessingDischarge is true, this is an intentional retirement/discharge,
            // not a player abandoning their post. Skip desertion logic to allow graceful retirement.
            if ((IsEnlisted || _isOnLeave) && newKingdom == null && oldKingdom != null && !_isProcessingDischarge)
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
            // NOTE: Skip all of this if _isProcessingDischarge is true - intentional discharge kingdom changes
            // (like restoring to original kingdom after full service) should not trigger any of this logic.
            if ((IsEnlisted || _isOnLeave) && newKingdom != null && !_isProcessingDischarge)
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
        ///     Phase 7: Increment the battles survived counter. Called after successfully completing a battle.
        /// </summary>
        public void IncrementBattlesSurvived()
        {
            if (!IsEnlisted)
            {
                return;
            }
            _battlesSurvived++;
            ModLogger.Debug("Enlistment", $"Battles survived: {_battlesSurvived}");
        }

        /// <summary>
        ///     Phase 7: Increment the events completed counter. Called after completing a Lance Life event.
        /// </summary>
        public void IncrementEventsCompleted()
        {
            if (!IsEnlisted)
            {
                return;
            }
            _eventsCompleted++;
            ModLogger.Debug("Enlistment", $"Events completed: {_eventsCompleted}");
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

                    // Phase 7: Track battles for promotion requirements
                    IncrementBattlesSurvived();

                    // Show notification to player
                    var message = $"Battle completed! +{battleXP} XP";
                    InformationManager.DisplayMessage(new InformationMessage(message));

                    ModLogger.Info("Battle", $"Battle XP awarded: {battleXP} (participation) via OnMapEventEnded");
                }
            }
            catch (Exception ex)
            {
                ModLogger.ErrorCode("Battle", "E-BATTLE-007", "Error awarding battle XP", ex);
            }
        }

        /// <summary>
        ///     Set tier directly (called by PromotionBehavior for immediate promotion).
        ///     V2.0: Companions now managed from T1. Commander retinue granted at T7/T8/T9.
        /// </summary>
        public void SetTier(int tier)
        {
            // V2.0: Support T1-T9 (was T1-T6)
            if (!IsEnlisted || tier < 1 || tier > 9)
            {
                ModLogger.Debug("Enlistment", $"SetTier rejected - Enlisted: {IsEnlisted}, Tier: {tier}");
                return;
            }

            var previousTier = _enlistmentTier;
            _enlistmentTier = tier;

            // Phase 7: Track promotion date for days-in-rank requirement
            if (tier > previousTier)
            {
                _lastPromotionDate = CampaignTime.Now;
            }

            ModLogger.Info("Enlistment", $"Tier changed: {previousTier} → {tier} (XP: {_enlistmentXP})");

            // V2.0: Companions are now managed from T1 (enlistment start), not T4
            // Legacy reclaim kept for backward compatibility with older saves
            if (previousTier < 4 && tier >= 4)
            {
                ReclaimCompanionsFromLord();
            }

            // V2.0: Grant commander retinue when crossing into T7/T8/T9
            // Recruits are auto-granted based on player's formation and lord's culture
            if (tier >= 7 && previousTier < tier)
            {
                try
                {
                    Features.CommandTent.Core.RetinueRecruitmentGrant.GrantCommanderRetinue(tier, previousTier);
                }
                catch (Exception ex)
                {
                    ModLogger.ErrorCode("Enlistment", "E-ENLIST-014", "Failed to grant commander retinue on tier change", ex);
                }
            }
        }

        // ========================================================================
        // QUARTERMASTER HERO METHODS (Phase 3)
        // ========================================================================

        /// <summary>
        ///     Get the current quartermaster Hero, creating one if necessary.
        ///     Returns null if not enlisted or if hero creation fails.
        /// </summary>
        public Hero GetOrCreateQuartermaster()
        {
            if (!IsEnlisted || _enlistedLord == null)
            {
                return null;
            }

            // Check if we need to create a new quartermaster
            if (_quartermasterHero == null || _quartermasterHero.IsDead)
            {
                _quartermasterHero = CreateQuartermasterForLord();
                
                if (_quartermasterHero != null)
                {
                    _hasMetQuartermaster = false; // Reset for first meeting
                }
            }

            return _quartermasterHero;
        }

        /// <summary>
        ///     Creates a new quartermaster Hero for the current enlisted lord.
        ///     The quartermaster has a culture-appropriate name, appearance, equipment,
        ///     and a randomly assigned personality archetype.
        /// </summary>
        private Hero CreateQuartermasterForLord()
        {
            if (_enlistedLord == null)
            {
                ModLogger.ErrorCode("Quartermaster", "E-QM-008", "Cannot create quartermaster: no enlisted lord",
                    new InvalidOperationException("Generated diagnostic exception to capture stack trace."));
                return null;
            }

            try
            {
                var culture = _enlistedLord.Culture;
                if (culture == null)
                {
                    ModLogger.ErrorCode("Quartermaster", "E-QM-009", "Cannot create quartermaster: lord has no culture",
                        new InvalidOperationException("Generated diagnostic exception to capture stack trace."));
                    return null;
                }

                // 1. Get culture-appropriate troop template (sergeant/veteran tier)
                var template = GetSergeantTierTroopTemplate(culture);
                if (template == null)
                {
                    ModLogger.Error("Quartermaster", $"Cannot find troop template for culture {culture.StringId}");
                    return null;
                }

                // 2. Get birth settlement (fallback chain for safety)
                var birthSettlement = _enlistedLord.HomeSettlement 
                    ?? _enlistedLord.BornSettlement 
                    ?? Settlement.All.FirstOrDefault(s => s.Culture == culture && s.IsTown);

                // 3. Create the Hero using native HeroCreator
                var qm = HeroCreator.CreateSpecialHero(
                    template,
                    birthSettlement,
                    _enlistedLord.Clan,
                    null, // No spouse
                    MBRandom.RandomInt(35, 55) // Experienced NCO age
                );

                if (qm == null)
                {
                    ModLogger.ErrorCode("Quartermaster", "E-QM-010", "HeroCreator.CreateSpecialHero returned null",
                        new InvalidOperationException("Generated diagnostic exception to capture stack trace."));
                    return null;
                }

                // 4. Set Hero properties
                // Use Wanderer occupation for generic camp NPCs (Quartermaster not in API)
                qm.SetNewOccupation(Occupation.Wanderer);
                qm.HiddenInEncyclopedia = true; // Don't clutter encyclopedia
                qm.IsKnownToPlayer = true;
                // Note: HasMet is read-only, but IsKnownToPlayer triggers the met status

                // 4.5. Visuals: quartermasters should look like a wealthy camp official, not a line soldier.
                // We keep battle equipment intact (safety: avoid spawning a helpless hero in rare edge cases),
                // but overwrite CIVILIAN equipment with the richest culture-appropriate civilian outfit we can find.
                TryApplyQuartermasterWealthyCulturalAttire(qm, culture);

                // 5. Place in lord's party (stays in baggage train)
                if (_enlistedLord.PartyBelongedTo != null)
                {
                    qm.ChangeHeroGold(-qm.Gold); // Remove default gold
                    EnterSettlementAction.ApplyForCharacterOnly(qm, birthSettlement);
                }

                // 6. Assign personality archetype based on culture
                _quartermasterArchetype = SelectArchetypeForCulture(culture.StringId);
                _quartermasterRelationship = 0; // Start neutral
                _hasMetQuartermaster = false;

                ModLogger.Info("Quartermaster",
                    $"Created quartermaster '{qm.Name}' ({_quartermasterArchetype}) for lord {_enlistedLord.Name}");

                return qm;
            }
            catch (Exception ex)
            {
                ModLogger.Error("Quartermaster", "Failed to create quartermaster Hero", ex);
                return null;
            }
        }

        private static void TryApplyQuartermasterWealthyCulturalAttire(Hero qm, CultureObject culture)
        {
            try
            {
                if (qm?.CivilianEquipment == null || culture == null)
                {
                    return;
                }

                // Candidate civilian templates: “wealthy” town/camp types.
                // Note: We do not rely on string ids (mod compatibility); we rely on Occupation + culture.
                var candidates = CharacterObject.All
                    .Where(t => t != null
                                && !t.IsHero
                                && t.Culture == culture
                                && (t.Occupation == Occupation.Merchant ||
                                    t.Occupation == Occupation.Artisan ||
                                    t.Occupation == Occupation.Preacher ||
                                    t.Occupation == Occupation.Gangster))
                    .ToList();

                if (candidates.Count == 0)
                {
                    // Fallback: any non-soldier civilian template of the culture.
                    candidates = CharacterObject.All
                        .Where(t => t != null
                                    && !t.IsHero
                                    && t.Culture == culture
                                    && t.Occupation != Occupation.Soldier)
                        .ToList();
                }

                if (candidates.Count == 0)
                {
                    return;
                }

                CharacterObject best = null;
                var bestScore = -1;
                foreach (var candidate in candidates)
                {
                    var score = ScoreCivilianOutfit(candidate?.FirstCivilianEquipment);
                    if (score > bestScore)
                    {
                        bestScore = score;
                        best = candidate;
                    }
                }

                if (best == null || bestScore <= 0)
                {
                    return;
                }

                // Apply the outfit.
                qm.CivilianEquipment.FillFrom(best.FirstCivilianEquipment, false);

                // Keep the look “official” (no visible civilian weapons unless the template insists).
                qm.CivilianEquipment[EquipmentIndex.Weapon0] = default;
                qm.CivilianEquipment[EquipmentIndex.Weapon1] = default;
                qm.CivilianEquipment[EquipmentIndex.Weapon2] = default;
                qm.CivilianEquipment[EquipmentIndex.Weapon3] = default;

                ModLogger.Info("Quartermaster",
                    $"Quartermaster attire: applied wealthy civilian outfit from '{best.StringId}' (score={bestScore}) for culture '{culture.StringId}'");
            }
            catch (Exception ex)
            {
                ModLogger.ErrorCode("Quartermaster", "E-QM-011", "Failed to apply wealthy quartermaster attire", ex);
            }
        }

        private static int ScoreCivilianOutfit(TaleWorlds.Core.Equipment equipment)
        {
            try
            {
                if (equipment == null)
                {
                    return 0;
                }

                // Focus on attire slots; treat item value as a decent “wealth” proxy.
                var slots = new[]
                {
                    EquipmentIndex.Head,
                    EquipmentIndex.Body,
                    EquipmentIndex.Leg,
                    EquipmentIndex.Gloves,
                    EquipmentIndex.Cape
                };

                var score = 0;
                foreach (var slot in slots)
                {
                    var item = equipment[slot].Item;
                    if (item != null)
                    {
                        score += Math.Max(0, item.Value);
                    }
                }

                return score;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        ///     Gets a sergeant/veteran tier troop template from the culture.
        ///     Used as the base for quartermaster appearance and skills.
        /// </summary>
        private CharacterObject GetSergeantTierTroopTemplate(CultureObject culture)
        {
            // Find tier 3-4 infantry troops from the lord's culture
            var eligibleTroops = CharacterObject.All
                .Where(t => t.Culture == culture
                         && !t.IsHero
                         && t.Tier >= 3 && t.Tier <= 4
                         && t.Occupation == Occupation.Soldier
                         && t.DefaultFormationClass == FormationClass.Infantry)
                .ToList();

            if (eligibleTroops.Count == 0)
            {
                // Fallback: any tier 3-4 troop
                eligibleTroops = CharacterObject.All
                    .Where(t => t.Culture == culture
                             && !t.IsHero
                             && t.Tier >= 3 && t.Tier <= 4)
                    .ToList();
            }

            if (eligibleTroops.Count == 0)
            {
                // Last resort: basic troop
                ModLogger.Warn("Quartermaster", $"No sergeant-tier troops found for {culture.StringId}, using basic troop");
                return culture.BasicTroop;
            }

            return eligibleTroops.GetRandomElement();
        }

        /// <summary>
        ///     Selects a personality archetype based on culture with weighted randomization.
        ///     Different cultures favor different personality types.
        /// </summary>
        private string SelectArchetypeForCulture(string cultureId)
        {
            var archetypes = new[] { "veteran", "merchant", "bookkeeper", "scoundrel", "believer", "eccentric" };

            // Culture-specific weights (from quartermaster_hero_update.md)
            // Format: veteran, merchant, bookkeeper, scoundrel, believer, eccentric
            var weights = cultureId?.ToLowerInvariant() switch
            {
                "empire" => new[] { 30, 20, 35, 3, 10, 2 },    // Bureaucratic empire favors bookkeepers
                "vlandia" => new[] { 25, 30, 20, 7, 15, 3 },   // Mercantile culture favors merchants
                "sturgia" => new[] { 40, 10, 5, 5, 25, 15 },   // Warrior culture favors veterans
                "battania" => new[] { 20, 7, 3, 30, 15, 25 },  // Tribal culture favors scoundrels/eccentrics
                "khuzait" => new[] { 20, 30, 3, 30, 7, 10 },   // Trade/raid culture
                "aserai" => new[] { 15, 35, 3, 15, 30, 2 },    // Merchant/faith culture
                _ => new[] { 30, 20, 15, 10, 15, 10 }          // Mercenary/default
            };

            // Weighted random selection
            var totalWeight = weights.Sum();
            var roll = MBRandom.RandomInt(0, totalWeight);
            var cumulative = 0;

            for (var i = 0; i < archetypes.Length; i++)
            {
                cumulative += weights[i];
                if (roll < cumulative)
                {
                    return archetypes[i];
                }
            }

            return "veteran"; // Fallback
        }

        /// <summary>
        ///     Modifies the relationship level with the quartermaster.
        /// </summary>
        /// <param name="change">Amount to change (positive or negative)</param>
        public void ModifyQuartermasterRelationship(int change)
        {
            _quartermasterRelationship = Math.Max(0, Math.Min(100, _quartermasterRelationship + change));
            ModLogger.Debug("Quartermaster", $"Relationship changed by {change}, now {_quartermasterRelationship}");
        }

        /// <summary>
        ///     Marks the quartermaster as met (after first conversation).
        /// </summary>
        public void MarkQuartermasterMet()
        {
            _hasMetQuartermaster = true;
            ModLogger.Debug("Quartermaster", "Player has met the quartermaster");
        }

        /// <summary>
        ///     Gets the relationship level name for UI display.
        /// </summary>
        public string GetQuartermasterRelationshipLevel()
        {
            return _quartermasterRelationship switch
            {
                >= 80 => "Battle Brother",
                >= 60 => "Respected",
                >= 40 => "Trusted",
                >= 20 => "Known",
                _ => "Stranger"
            };
        }

        /// <summary>
        ///     Gets the quartermaster discount percentage based on relationship.
        ///     Phase 8: High relationship provides discounts on equipment and rations.
        /// </summary>
        /// <returns>Discount percentage (0-15)</returns>
        public int GetQuartermasterDiscount()
        {
            return _quartermasterRelationship switch
            {
                >= 80 => 15, // 15% discount for Battle Brothers
                >= 60 => 10, // 10% discount for Respected
                >= 40 => 5,  // 5% discount for Trusted
                _ => 0       // No discount below Trusted
            };
        }

        /// <summary>
        ///     Applies the quartermaster discount to a price.
        /// </summary>
        /// <param name="basePrice">Original price</param>
        /// <returns>Discounted price</returns>
        public int ApplyQuartermasterDiscount(int basePrice)
        {
            var discount = GetQuartermasterDiscount();
            if (discount <= 0)
            {
                return basePrice;
            }

            var reduction = (int)(basePrice * discount / 100f);
            return Math.Max(1, basePrice - reduction);
        }

        /// <summary>
        ///     Gets relationship milestone info for display.
        ///     Returns (nextMilestone, pointsNeeded, reward) tuple.
        /// </summary>
        public (int NextMilestone, int PointsNeeded, string Reward) GetRelationshipMilestoneInfo()
        {
            var current = _quartermasterRelationship;

            if (current < 20)
            {
                return (20, 20 - current, "Unlocks: Chat option");
            }
            else if (current < 40)
            {
                return (40, 40 - current, "Unlocks: 5% discount, Black market access");
            }
            else if (current < 60)
            {
                return (60, 60 - current, "Unlocks: 10% discount");
            }
            else if (current < 80)
            {
                return (80, 80 - current, "Unlocks: 15% discount, Special items");
            }
            else
            {
                return (100, 100 - current, "Maximum relationship");
            }
        }

        /// <summary>
        ///     Cleans up quartermaster state when service ends.
        /// </summary>
        private void CleanupQuartermasterOnServiceEnd()
        {
            // Quartermaster stays with the lord's army when player leaves
            // Just null out our reference
            _quartermasterHero = null;
            _quartermasterArchetype = null;
            _quartermasterRelationship = 0;
            _hasMetQuartermaster = false;

            ModLogger.Info("Quartermaster", "Quartermaster reference cleared on service end");
        }

        // ========================================================================
        // FOOD/RATIONS SYSTEM METHODS (Phase 5)
        // ========================================================================

        /// <summary>
        ///     Food quality tier enumeration for rations system.
        ///     Values correspond to _currentFoodQuality field.
        /// </summary>
        public enum FoodQualityTier
        {
            /// <summary>Standard army rations - no bonus.</summary>
            Standard = 0,
            /// <summary>Supplemental rations - +2 morale for 1 day.</summary>
            Supplemental = 1,
            /// <summary>Officer's fare - +4 morale, +2 fatigue relief for 2 days.</summary>
            Officer = 2,
            /// <summary>Commander's feast - +8 morale, +5 fatigue relief for 3 days.</summary>
            Commander = 3
        }

        /// <summary>
        ///     Gets the current food quality tier.
        ///     Automatically returns Standard if the current quality has expired.
        /// </summary>
        public FoodQualityTier CurrentFoodQuality
        {
            get
            {
                // Check if expired
                if (_currentFoodQuality > 0 && CampaignTime.Now >= _foodQualityExpires)
                {
                    _currentFoodQuality = 0;
                    _foodQualityExpires = CampaignTime.Zero;
                    ModLogger.Debug("Food", "Food quality bonus expired");
                }
                return (FoodQualityTier)_currentFoodQuality;
            }
        }

        /// <summary>
        ///     Gets the time remaining on the current food quality bonus.
        ///     Returns zero if no bonus active or expired.
        /// </summary>
        public CampaignTime FoodQualityTimeRemaining
        {
            get
            {
                if (_currentFoodQuality <= 0 || CampaignTime.Now >= _foodQualityExpires)
                {
                    return CampaignTime.Zero;
                }
                return _foodQualityExpires - CampaignTime.Now;
            }
        }

        /// <summary>
        ///     Gets the morale bonus from current food quality.
        ///     Used by camp life behavior to adjust party morale.
        /// </summary>
        /// <returns>Morale bonus (0, 2, 4, or 8)</returns>
        public int GetFoodMoraleBonus()
        {
            var quality = CurrentFoodQuality;
            return quality switch
            {
                FoodQualityTier.Supplemental => 2,
                FoodQualityTier.Officer => 4,
                FoodQualityTier.Commander => 8,
                _ => 0
            };
        }

        /// <summary>
        ///     Gets the fatigue relief bonus from current food quality.
        ///     Applied when rations are purchased (immediate relief).
        /// </summary>
        /// <returns>Fatigue relief points (0, 0, 2, or 5)</returns>
        public int GetFoodFatigueBonus()
        {
            var quality = CurrentFoodQuality;
            return quality switch
            {
                FoodQualityTier.Officer => 2,
                FoodQualityTier.Commander => 5,
                _ => 0
            };
        }

        /// <summary>
        ///     Gets food quality information for display.
        /// </summary>
        public (string Name, int MoraleBonus, int FatigueBonus, float DaysRemaining) GetFoodQualityInfo()
        {
            var quality = CurrentFoodQuality;
            var remaining = FoodQualityTimeRemaining;
            var daysRemaining = (float)remaining.ToDays;

            var name = quality switch
            {
                FoodQualityTier.Supplemental => "Supplemental Rations",
                FoodQualityTier.Officer => "Officer's Fare",
                FoodQualityTier.Commander => "Commander's Feast",
                _ => "Standard Rations"
            };

            return (name, GetFoodMoraleBonus(), GetFoodFatigueBonus(), daysRemaining);
        }

        /// <summary>
        ///     Purchases rations of the specified quality tier.
        ///     Deducts gold, applies fatigue relief, sets morale bonus duration.
        /// </summary>
        /// <param name="tier">The food quality tier to purchase</param>
        /// <param name="cost">Gold cost (validated before calling)</param>
        /// <param name="durationDays">How many days the bonus lasts</param>
        /// <returns>True if purchase successful, false if insufficient funds</returns>
        public bool PurchaseRations(FoodQualityTier tier, int cost, int durationDays)
        {
            if (!IsEnlisted)
            {
                ModLogger.Warn("Food", "Cannot purchase rations: not enlisted");
                return false;
            }

            // Relationship discount is part of the Quartermaster Hero system.
            // Apply it here so *all* rations purchases respect the relationship tier.
            var effectiveCost = ApplyQuartermasterDiscount(cost);

            if (Hero.MainHero.Gold < effectiveCost)
            {
                ModLogger.Debug("Food", $"Cannot purchase rations: insufficient gold ({Hero.MainHero.Gold} < {effectiveCost})");
                return false;
            }

            // Deduct gold
            Hero.MainHero.ChangeHeroGold(-effectiveCost);

            // Set quality and duration
            _currentFoodQuality = (int)tier;
            _foodQualityExpires = CampaignTime.Now + CampaignTime.Days(durationDays);

            // Apply immediate fatigue relief for higher tiers
            var fatigueBonus = tier switch
            {
                FoodQualityTier.Officer => 2,
                FoodQualityTier.Commander => 5,
                _ => 0
            };

            if (fatigueBonus > 0)
            {
                RestoreFatigue(fatigueBonus, "rations_purchase");
            }

            // Increase quartermaster relationship
            ModifyQuartermasterRelationship(2);

            var tierName = tier switch
            {
                FoodQualityTier.Supplemental => "Supplemental Rations",
                FoodQualityTier.Officer => "Officer's Fare",
                FoodQualityTier.Commander => "Commander's Feast",
                _ => "Rations"
            };

            ModLogger.Info("Food",
                $"Purchased {tierName} for {effectiveCost}g (base {cost}g), duration {durationDays} days, fatigue relief +{fatigueBonus}");

            return true;
        }

        /// <summary>
        ///     Clears food quality bonus (used when service ends).
        /// </summary>
        private void ClearFoodQuality()
        {
            _currentFoodQuality = 0;
            _foodQualityExpires = CampaignTime.Zero;
        }

        // ========================================================================
        // RETINUE PROVISIONING SYSTEM METHODS (Phase 6)
        // ========================================================================

        /// <summary>
        ///     Retinue provisioning tier enumeration.
        ///     Higher tiers provide better morale but cost more per soldier.
        /// </summary>
        public enum RetinueProvisioningTier
        {
            /// <summary>No provisions - starvation penalties apply.</summary>
            None = 0,
            /// <summary>Bare minimum rations - -5 morale penalty.</summary>
            BareMinimum = 1,
            /// <summary>Standard army rations - no modifier.</summary>
            Standard = 2,
            /// <summary>Good fare - +5 morale bonus.</summary>
            GoodFare = 3,
            /// <summary>Officer quality - +10 morale bonus.</summary>
            OfficerQuality = 4
        }

        /// <summary>
        ///     Gets the current retinue provisioning tier.
        ///     Returns None if expired.
        /// </summary>
        public RetinueProvisioningTier CurrentRetinueProvisioning
        {
            get
            {
                // Check if expired
                if (_retinueProvisioningTier > 0 && CampaignTime.Now >= _retinueProvisioningExpires)
                {
                    _retinueProvisioningTier = 0;
                    _retinueProvisioningExpires = CampaignTime.Zero;
                    ModLogger.Debug("RetinueFood", "Retinue provisioning expired");
                }
                return (RetinueProvisioningTier)_retinueProvisioningTier;
            }
        }

        /// <summary>
        ///     Gets the time remaining on retinue provisions.
        /// </summary>
        public CampaignTime RetinueProvisioningTimeRemaining
        {
            get
            {
                if (_retinueProvisioningTier <= 0 || CampaignTime.Now >= _retinueProvisioningExpires)
                {
                    return CampaignTime.Zero;
                }
                return _retinueProvisioningExpires - CampaignTime.Now;
            }
        }

        /// <summary>
        ///     Gets the morale modifier from current retinue provisioning.
        /// </summary>
        /// <returns>Morale modifier (-10 to +10)</returns>
        public int GetRetinueMoraleModifier()
        {
            var tier = CurrentRetinueProvisioning;
            return tier switch
            {
                RetinueProvisioningTier.None => -10,       // Starvation
                RetinueProvisioningTier.BareMinimum => -5, // Grumbling
                RetinueProvisioningTier.Standard => 0,     // Neutral
                RetinueProvisioningTier.GoodFare => 5,     // Satisfied
                RetinueProvisioningTier.OfficerQuality => 10, // High morale
                _ => 0
            };
        }

        /// <summary>
        ///     Gets the cost per soldier for a provisioning tier.
        /// </summary>
        public static int GetProvisioningCostPerSoldier(RetinueProvisioningTier tier)
        {
            return tier switch
            {
                RetinueProvisioningTier.BareMinimum => 2,    // 2g per soldier per week
                RetinueProvisioningTier.Standard => 5,       // 5g per soldier per week
                RetinueProvisioningTier.GoodFare => 10,      // 10g per soldier per week
                RetinueProvisioningTier.OfficerQuality => 20, // 20g per soldier per week
                _ => 0
            };
        }

        /// <summary>
        ///     Gets the total cost to provision the retinue for one week.
        /// </summary>
        /// <param name="tier">Provisioning tier</param>
        /// <param name="soldierCount">Number of soldiers (from RetinueManager)</param>
        public static int GetRetinueProvisioningCost(RetinueProvisioningTier tier, int soldierCount)
        {
            return GetProvisioningCostPerSoldier(tier) * soldierCount;
        }

        /// <summary>
        ///     Gets retinue provisioning information for display.
        /// </summary>
        public (string Name, int MoraleModifier, float DaysRemaining) GetRetinueProvisioningInfo()
        {
            var tier = CurrentRetinueProvisioning;
            var remaining = RetinueProvisioningTimeRemaining;
            var daysRemaining = (float)remaining.ToDays;

            var name = tier switch
            {
                RetinueProvisioningTier.BareMinimum => "Bare Minimum",
                RetinueProvisioningTier.Standard => "Standard Rations",
                RetinueProvisioningTier.GoodFare => "Good Fare",
                RetinueProvisioningTier.OfficerQuality => "Officer Quality",
                _ => "Not Provisioned"
            };

            return (name, GetRetinueMoraleModifier(), daysRemaining);
        }

        /// <summary>
        ///     Purchases retinue provisions for one week.
        /// </summary>
        /// <param name="tier">The provisioning tier to purchase</param>
        /// <param name="soldierCount">Number of soldiers to provision</param>
        /// <returns>True if purchase successful</returns>
        public bool PurchaseRetinueProvisioning(RetinueProvisioningTier tier, int soldierCount)
        {
            if (!IsEnlisted || _enlistmentTier < 7)
            {
                ModLogger.Warn("RetinueFood", "Cannot provision retinue: not a commander (T7+)");
                return false;
            }

            var baseCost = GetRetinueProvisioningCost(tier, soldierCount);
            var cost = ApplyQuartermasterDiscount(baseCost);

            if (Hero.MainHero.Gold < cost)
            {
                ModLogger.Debug("RetinueFood", $"Cannot provision: insufficient gold ({Hero.MainHero.Gold} < {cost})");
                return false;
            }

            // Deduct gold
            Hero.MainHero.ChangeHeroGold(-cost);

            // Set provisioning tier and duration (7 days)
            _retinueProvisioningTier = (int)tier;
            _retinueProvisioningExpires = CampaignTime.Now + CampaignTime.Days(7);
            _retinueProvisioningWarningShown = false;

            // Increase quartermaster relationship
            ModifyQuartermasterRelationship(3);

            var tierName = tier switch
            {
                RetinueProvisioningTier.BareMinimum => "Bare Minimum",
                RetinueProvisioningTier.Standard => "Standard",
                RetinueProvisioningTier.GoodFare => "Good Fare",
                RetinueProvisioningTier.OfficerQuality => "Officer Quality",
                _ => "provisions"
            };

            ModLogger.Info("RetinueFood",
                $"Purchased {tierName} provisioning for {soldierCount} soldiers, cost {cost}g (base {baseCost}g), 7 days");

            return true;
        }

        /// <summary>
        ///     Checks retinue provisioning status and displays warnings.
        ///     Called from DailyTick.
        /// </summary>
        public void CheckRetinueProvisioningStatus()
        {
            // Only applies to T7+ commanders with retinue
            if (!IsEnlisted || _enlistmentTier < 7)
            {
                return;
            }

            var retinueManager = Features.CommandTent.Core.RetinueManager.Instance;
            if (retinueManager?.State == null || retinueManager.State.TotalSoldiers <= 0)
            {
                return;
            }

            var remaining = RetinueProvisioningTimeRemaining;
            var daysRemaining = remaining.ToDays;

            // Check for starvation (no provisions)
            if (_retinueProvisioningTier <= 0 || CampaignTime.Now >= _retinueProvisioningExpires)
            {
                // Starvation - apply penalties
                InformationManager.DisplayMessage(new InformationMessage(
                    "Your retinue is starving! Visit the Quartermaster to provision them.",
                    Colors.Red));
                return;
            }

            // Check for low provisions warning (2 days or less)
            if (daysRemaining <= 2 && !_retinueProvisioningWarningShown)
            {
                _retinueProvisioningWarningShown = true;
                InformationManager.DisplayMessage(new InformationMessage(
                    $"Warning: Retinue provisions will run out in {daysRemaining:F1} days!",
                    Colors.Yellow));
            }
        }

        /// <summary>
        ///     Clears retinue provisioning (used when service ends or retinue disbanded).
        /// </summary>
        public void ClearRetinueProvisioning()
        {
            _retinueProvisioningTier = 0;
            _retinueProvisioningExpires = CampaignTime.Zero;
            _retinueProvisioningWarningShown = false;
        }

        /// <summary>
        ///     Whether the player has an active retinue that needs provisioning.
        /// </summary>
        public bool HasRetinueToProvision()
        {
            if (!IsEnlisted || _enlistmentTier < 7)
            {
                return false;
            }

            var retinueManager = Features.CommandTent.Core.RetinueManager.Instance;
            return retinueManager?.State?.TotalSoldiers > 0;
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
                        // Phase 7: Troop selection menu removed - promotions handled via PromotionBehavior
                        // which triggers proving events and culture-specific notifications.
                        // Player visits Quartermaster manually for equipment after promotion.
                        // The PromotionBehavior.CheckForPromotion() handles the actual tier advancement.
                        ModLogger.Info("Progression", $"XP threshold crossed for tier {tier + 1} - PromotionBehavior will handle");

                        break; // Only notify for the first threshold crossed
                    }
                }
            }
            catch (Exception ex)
            {
                ModLogger.ErrorCode("Progression", "E-PROG-001", "Error checking promotion notification", ex);
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
                ModLogger.ErrorCode("Progression", "E-PROG-002", "Error showing promotion notification", ex);
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
                ModLogger.ErrorCode("FactionRelations", "E-FACTIONREL-001", "Error mirroring war relations", ex);
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
                ModLogger.ErrorCode("FactionRelations", "E-FACTIONREL-002", "Error restoring war relations", ex);
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
                if (playerFaction == null)
                {
                    return;
                }

                // Mark all visible parties from factions at war with player as dirty
                // This triggers nameplate color refresh (same pattern as DeclareWarAction.ApplyInternal)
                foreach (var party in MobileParty.All)
                {
                    if (!party.IsVisible)
                    {
                        continue;
                    }
                    
                    var partyFaction = party.MapFaction;
                    if (partyFaction == null)
                    {
                        continue;
                    }

                    // Check if this party's faction is at war with player
                    if (FactionManager.IsAtWarAgainstFaction(playerFaction, partyFaction))
                    {
                        party.Party.SetVisualAsDirty();
                    }
                }

                // Also mark settlements
                foreach (var settlement in Settlement.All)
                {
                    if (!settlement.IsVisible)
                    {
                        continue;
                    }
                    
                    var settlementFaction = settlement.MapFaction;
                    if (settlementFaction == null)
                    {
                        continue;
                    }

                    if (FactionManager.IsAtWarAgainstFaction(playerFaction, settlementFaction))
                    {
                        settlement.Party.SetVisualAsDirty();
                    }
                }

                ModLogger.Debug("FactionRelations", "Refreshed faction visuals after relation change");
            }
            catch (Exception ex)
            {
                ModLogger.ErrorCode("FactionRelations", "E-FACTIONREL-003", "Error refreshing faction visuals", ex);
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
                ModLogger.ErrorCode("Enlistment", "E-ENLIST-016", "TransferServiceToLord called with null lord",
                    new InvalidOperationException("Generated diagnostic exception to capture stack trace."));
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
                var newLordName = newLord?.Name?.ToString() ?? "unknown";
                ModLogger.ErrorCode("Enlistment", "E-ENLIST-015", $"Error transferring service to {newLordName}", ex);
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
        ///     Deduplicates against existing roster and cleans stale refs from lord party.
        /// </summary>
        private void RestoreCompanionsToPlayer()
        {
            try
            {
                var main = MobileParty.MainParty;
                var lordParty = _enlistedLord?.PartyBelongedTo;

                if (main == null)
                {
                    ModLogger.Warn("Enlistment", "RestoreCompanionsToPlayer skipped - main party missing");
                    return;
                }

                if (lordParty == null)
                {
                    ModLogger.Warn("Enlistment", "RestoreCompanionsToPlayer skipped - lord party missing");
                    return;
                }

                var mainRoster = main.MemberRoster;
                var lordRoster = lordParty.MemberRoster;
                var companionsToRestore = new List<TroopRosterElement>();
                var staleRefsToRemove = new List<TroopRosterElement>();

                // Find player's companions in lord's party
                foreach (var troop in lordRoster.GetTroopRoster())
                {
                    if (!troop.Character.IsHero || troop.Character == CharacterObject.PlayerCharacter)
                    {
                        continue;
                    }

                    var hero = troop.Character.HeroObject;
                    if (hero == null || hero.Clan != Clan.PlayerClan)
                    {
                        continue;
                    }

                    // Check if companion already in player's roster
                    if (mainRoster.GetTroopCount(troop.Character) > 0)
                    {
                        // Stale reference - companion escaped and is already with player
                        staleRefsToRemove.Add(troop);
                    }
                    else
                    {
                        // Need to restore this companion to player
                        companionsToRestore.Add(troop);
                    }
                }

                // Remove stale references from lord's party (companion already with player)
                foreach (var stale in staleRefsToRemove)
                {
                    lordRoster.AddToCounts(stale.Character, -stale.Number, false, 0, 0, true, -1);
                    ModLogger.Info("Enlistment", $"Cleaned stale ref for {stale.Character.Name} from lord's party");
                }

                // Transfer companions that need restoration
                foreach (var companion in companionsToRestore)
                {
                    lordRoster.AddToCounts(companion.Character, -companion.Number, false, 0, 0, true, -1);
                    mainRoster.AddToCounts(companion.Character, companion.Number, false, 0, 0, true, -1);
                }

                if (companionsToRestore.Count > 0)
                {
                    var message =
                        new TextObject("{=enlist_companions_rejoined}Your {COMPANION_COUNT} companions have rejoined your party upon retirement.");
                    message.SetTextVariable("COMPANION_COUNT", companionsToRestore.Count.ToString());
                    InformationManager.DisplayMessage(new InformationMessage(message.ToString()));

                    ModLogger.Info("Enlistment",
                        $"Restored {companionsToRestore.Count} companions to player party (cleaned {staleRefsToRemove.Count} stale refs)");
                }
                else if (staleRefsToRemove.Count > 0)
                {
                    ModLogger.Info("Enlistment",
                        $"Cleaned {staleRefsToRemove.Count} stale companion refs from lord's party (companions already with player)");
                }
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
                ModLogger.ErrorCode("Battle", "E-BATTLE-008", "Error in army battle participation", ex);
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
                ModLogger.ErrorCode("Battle", "E-BATTLE-009", "Error handling army battle", ex);
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
                ModLogger.ErrorCode("Battle", "E-BATTLE-010", "Error handling individual battle", ex);
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
                ModLogger.ErrorCode("Battle", "E-BATTLE-011", "Error in post-battle cleanup", ex);
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
                var playerIsPrisoner = Hero.MainHero.IsPrisoner;
                if (!IsEnlisted || _isOnLeave || playerIsPrisoner)
                {
                    if (playerIsPrisoner)
                    {
                        var settlementName = settlement?.Name?.ToString() ?? "unknown";
                        if (!_captivitySettlementsLogged.Contains(settlementName))
                        {
                            ModLogger.Info("Captivity",
                                $"Captor entered {settlementName} with player as prisoner");
                            _captivitySettlementsLogged.Add(settlementName);
                        }
                    }
                    else
                    {
                        // Clear throttle cache once not prisoner so next captivity run logs fresh
                        _captivitySettlementsLogged.Clear();
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
                            ModLogger.ErrorCode("Settlement", "E-SETTLEMENT-002",
                                "Error finishing PlayerEncounter before settlement entry (player)", ex);
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

                    TryPauseForSettlementEntry();

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
                            ModLogger.ErrorCode("Settlement", "E-SETTLEMENT-003",
                                "Error finishing PlayerEncounter before settlement entry (lord)", ex);
                        }
                    }

                    // Only pause/activate menu for towns and castles, not villages
                    // Villages should allow continuous time flow while following the lord
                    if (settlement.IsTown || settlement.IsCastle)
                    {
                            TryPauseForSettlementEntry();

                        // CRITICAL GUARD: Don't interfere with battle/siege encounter menus
                        // CORRECT API: Use Party.MapEvent (not direct on MobileParty)
                        // Allow settlement encounters (peaceful town/castle entry) but block battles
                        var isSettlementEncounter = PlayerEncounter.EncounterSettlement != null &&
                                                    mainParty?.Party.MapEvent == null;
                        var inBattleOrSiege = (mainParty?.Party.MapEvent != null) ||
                                              (PlayerEncounter.Current != null && !isSettlementEncounter) ||
                                              (_enlistedLord?.PartyBelongedTo?.BesiegedSettlement != null);

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
                ModLogger.ErrorCode("Settlement", "E-SETTLEMENT-004", "Error in settlement entry detection", ex);
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
                                ModLogger.ErrorCode("Settlement", "E-SETTLEMENT-005",
                                    "Error pulling player from settlement to follow lord", ex);
                            }
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                ModLogger.ErrorCode("Settlement", "E-SETTLEMENT-006", "Error in settlement exit detection", ex);
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
                                ModLogger.ErrorCode("Battle", "E-BATTLE-018",
                                    $"Failed to switch to native menu '{desiredMenu}'", ex);
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
                ForceImmediateBattleJoin(mapEvent, main, lordParty);

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
                ModLogger.ErrorCode("Battle", "E-BATTLE-014", "Error in MapEventStarted battle handler", ex);
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
                            ModLogger.ErrorCode("Battle", "E-BATTLE-019",
                                "Error finishing PlayerEncounter after enlistment ended", ex);
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

                    // If the player chose to wait in reserve, make sure we clear the suspended state
                    // once the battle (or battle series) is actually over. Without this, the party
                    // can stay inactive and flagged as waiting, which blocks health regen until the
                    // player hits another major state change (reload, level, new battle, leave).
                    if (EnlistedEncounterBehavior.IsWaitingInReserve)
                    {
                        var lordStillInBattle = lordParty?.Party.MapEvent != null;
                        var armyStillInBattle = lordParty?.Army?.Parties?.Any(p => p?.Party?.MapEvent != null) == true;

                        // Only clear reserve when no active battles remain for the lord/army
                        if (!lordStillInBattle && !armyStillInBattle)
                        {
                            EnlistedEncounterBehavior.ClearReserveState();

                            // Release any lingering wait flag on the encounter
                            var encounter = TaleWorlds.CampaignSystem.Encounters.PlayerEncounter.Current;
                            if (encounter != null)
                            {
                                encounter.IsPlayerWaiting = false;
                                PlayerEncounter.LeaveEncounter = true;
                            }

                            var mainParty = MobileParty.MainParty;
                            if (mainParty != null && Hero.MainHero?.IsPrisoner != true)
                            {
                                mainParty.IsActive = true;   // ensure ticks resume (health, wages, etc.)
                                // Keep them hidden; visibility is managed elsewhere
                                if (!mainParty.IsVisible)
                                {
                                    mainParty.IsVisible = false;
                                }
                            }

                            ModLogger.Info("Battle",
                                "Cleared reserve state after battle end - restoring party activity for regen");
                        }
                    }

                    // Award battle XP only if player actually participated (not waiting in reserve)
                    // Players who chose "Wait in Reserve" opted out of combat and don't earn XP
                    if (!EnlistedEncounterBehavior.IsWaitingInReserve)
                    {
                        AwardBattleXP(playerParticipated);
                        
                        // Phase 4: Award battle loot share if we won
                        var playerWon = effectiveMapEvent?.WinningSide == effectiveMapEvent?.PlayerSide;
                        if (playerParticipated && playerWon)
                        {
                            AwardBattleLootShare(effectiveMapEvent, true);
                        }
                    }
                    else
                    {
                        ModLogger.Debug("Battle", "Skipping battle XP - player waited in reserve");
                    }

                    // CRITICAL: Clear siege encounter creation timestamp when battle ends
                    // This allows new encounters to be created if needed after the battle
                    _lastSiegeEncounterCreation = CampaignTime.Zero;

                    // Determine if this was a siege battle - affects cleanup strategy
                    // For sieges, native AiPartyThinkBehavior.PartyHourlyAiTick also calls PlayerEncounter.Finish()
                    // when parties change behavior. We must avoid a race condition.
                    bool wasSiege = effectiveMapEvent?.IsSiegeAssault == true || 
                                   effectiveMapEvent?.IsSallyOut == true ||
                                   lordParty?.BesiegedSettlement != null ||
                                   main?.BesiegedSettlement != null;
                    
                    // FIX: For siege battles, finish the encounter immediately instead of deferring.
                    // The native AI will try to call Finish() in its next hourly tick after the battle ends.
                    // If we defer our Finish() call, both systems race to clean up the same encounter,
                    // causing NullReferenceException when internal state becomes inconsistent.
                    // PlayerEncounterFinishSafetyPatch provides additional crash protection.
                    if (TaleWorlds.CampaignSystem.Encounters.PlayerEncounter.Current != null)
                    {
                        if (wasSiege)
                        {
                            // SIEGE: Finish immediately to prevent race with native AI
                            ModLogger.Debug("Battle", 
                                "Siege battle ended - finishing encounter immediately to avoid native AI race");
                            try
                            {
                                if (TaleWorlds.CampaignSystem.Encounters.PlayerEncounter.InsideSettlement)
                                {
                                    TaleWorlds.CampaignSystem.Encounters.PlayerEncounter.LeaveSettlement();
                                }
                                TaleWorlds.CampaignSystem.Encounters.PlayerEncounter.Finish(true);
                                ModLogger.Info("Battle", "Finished PlayerEncounter after siege battle (immediate)");
                            }
                            catch (Exception ex)
                            {
                                // PlayerEncounterFinishSafetyPatch should catch most issues,
                                // but log if something still slips through
                                ModLogger.Warn("Battle", 
                                    $"Error finishing siege encounter (safety patch may have handled it): {ex.Message}");
                            }
                            
                            // Return to hidden state
                            if (main != null && IsEnlisted)
                            {
                                main.IsActive = false;
                                main.IsVisible = false;
                            }
                            ResetSiegePreparationLatch();
                            RestoreCampaignFlowAfterBattle();
                        }
                        else
                        {
                            // NON-SIEGE: Defer to next frame (safe for field battles)
                            // Field battles don't have the same AI race condition because parties
                            // don't change their besieging behavior on battle end.
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
                                            "Finished PlayerEncounter after battle ended (deferred)");
                                    }

                                    // Return to hidden enlisted state after encounter is finished
                                    var mainParty = MobileParty.MainParty;
                                    if (mainParty != null && IsEnlisted)
                                    {
                                        mainParty.IsActive = false;
                                        mainParty.IsVisible = false;
                                        ModLogger.Debug("Battle",
                                            "Player returned to hidden state after deferred cleanup");
                                    }

                                    ResetSiegePreparationLatch();
                                    RestoreCampaignFlowAfterBattle();
                                }
                                catch (Exception ex)
                                {
                                    ModLogger.ErrorCode("Battle", "E-BATTLE-020",
                                        "Error in deferred encounter cleanup", ex);
                                }
                            });
                        }
                    }
                    else
                    {
                        // No encounter to finish - return to hidden enlisted state immediately
                        main.IsActive = false;
                        main.IsVisible = false;
                        ModLogger.Debug("Battle", "No encounter to finish - returning to hidden state");
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

                    ModLogger.Info("Battle", "Battle cleanup complete");
                }
            }
            catch (Exception ex)
            {
                ModLogger.ErrorCode("Battle", "E-BATTLE-015", "Error in MapEvent end handler", ex);
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
                    
                    // Record battle for lance persona progression (promotions require 3+ battles)
                    var lancePersonas = Lances.Personas.LancePersonaBehavior.Instance;
                    if (lancePersonas != null && !string.IsNullOrEmpty(CurrentLanceId))
                    {
                        var lanceKey = $"{CurrentLord?.StringId}_{CurrentLanceId}";
                        lancePersonas.RecordBattleParticipation(lanceKey, 10);
                        ModLogger.Debug("Battle", $"Lance battle recorded for {lanceKey}");
                    }
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
                ModLogger.ErrorCode("Battle", "E-BATTLE-016", "Error in OnPlayerBattleEnd", ex);
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
                ModLogger.ErrorCode("Battle", "E-BATTLE-017", "Error awarding battle XP", ex);
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
                ModLogger.ErrorCode("Siege", "E-SIEGE-002", "Error in siege watchdog", ex);
            }
        }

        private void ResetSiegePreparationLatch()
        {
            _isSiegePreparationLatched = false;
            _latchedSiegeSettlement = null;
        }

        /// <summary>
        /// Handles player state cleanup when lord is captured.
        /// - If player was in reserve: clears reserve state, finishes encounter, teleports to safety
        /// - If player is in encounter: native surrender flow handles capture
        /// - Otherwise: just logs (edge case)
        /// Does NOT directly capture the player - native game handles actual capture via surrender.
        /// </summary>
        private void HandlePlayerStateOnLordCapture(PartyBase capturingParty)
        {
            if (!IsEnlisted || _isOnLeave || Hero.MainHero?.IsPrisoner == true)
            {
                return;
            }

            // Check if player was waiting in reserve - if so, clean up the reserve state
            // so the player can appear on the map after the battle ends
            var wasInReserve = Combat.Behaviors.EnlistedEncounterBehavior.IsWaitingInReserve;
            
            if (wasInReserve)
            {
                // Clean up reserve state and IMMEDIATELY finish the encounter
                // Setting LeaveEncounter = true doesn't work reliably because:
                // 1. It's only processed on future menu ticks
                // 2. GameMenu.ExitToLast() can interrupt normal tick processing
                // 3. If game time is paused (menus), the encounter stays stuck forever
                // 4. This caused players to be stuck invisible with no way to recover
                Combat.Behaviors.EnlistedEncounterBehavior.ClearReserveState();
                
                // CRITICAL: Immediately finish the encounter - don't rely on LeaveEncounter flag
                var encounter = PlayerEncounter.Current;
                if (encounter != null)
                {
                    try
                    {
                        encounter.IsPlayerWaiting = false;
                        PlayerEncounter.LeaveEncounter = true;
                        // Force immediate cleanup - this clears PlayerEncounter.Current
                        PlayerEncounter.Finish(true);
                        ModLogger.Info("Battle", "Force-finished PlayerEncounter to prevent stuck state");
                    }
                    catch (System.Exception ex)
                    {
                        ModLogger.Warn("Battle", $"Error finishing encounter: {ex.Message} - will rely on watchdog");
                    }
                }
                
                // Apply protection so enemies don't immediately attack when restored
                var mainParty = MobileParty.MainParty;
                if (mainParty != null)
                {
                    var protectionDuration = CampaignTime.Hours(12f);
                    mainParty.IgnoreByOtherPartiesTill(CampaignTime.Now + protectionDuration);
                    
                    // Move player to nearest friendly settlement to escape the battle
                    TryTeleportToSafety(mainParty, capturingParty);
                    
                    // Since we've finished the encounter, StopEnlist won't see it as "in battle state"
                    // and will activate the party directly - this is what we want
                }
                
                // Exit the menu so player returns to campaign map
                GameMenu.ExitToLast();
                
                ModLogger.Info("Battle", "Player escaped during lord capture (was in reserve) - teleported to safety");
                return;
            }
            
            if (PlayerEncounter.Current != null)
            {
                // Player is already in an encounter (e.g., choosing Surrender). Native flow will capture them.
                ModLogger.Info("Battle",
                    "Encounter active during lord capture - native surrender flow will handle player.");
                return;
            }

            // Player wasn't in reserve and has no active encounter - edge case, just log
            ModLogger.Info("Battle", "Lord captured but player not in reserve or encounter - no action needed");
        }

        private void SchedulePostEncounterVisibilityRestore()
        {
            if (_pendingVisibilityRestore)
            {
                return;
            }

            _pendingVisibilityRestore = true;
            _pendingVisibilityRestoreStartTime = CampaignTime.Now;
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

            var mainHero = Hero.MainHero;

            // Wait until encounter/MapEvent clears before restoring visibility
            if (PlayerEncounter.Current != null || mainParty.Party?.MapEvent != null)
            {
                // If enlistment already ended and the encounter persists without a MapEvent, force-finish after a short timeout.
                // This prevents the player from remaining invisible/inactive because PostDischargeProtection blocks reactivation.
                var encounter = PlayerEncounter.Current;
                if ((!IsEnlisted || _isOnLeave) && mainHero != null && !mainHero.IsPrisoner &&
                    encounter != null && mainParty.Party?.MapEvent == null &&
                    _pendingVisibilityRestoreStartTime != CampaignTime.Zero &&
                    CampaignTime.Now - _pendingVisibilityRestoreStartTime > CampaignTime.Seconds(5L))
                {
                    ModLogger.Warn("EncounterCleanup",
                        $"Force finishing lingering PlayerEncounter after discharge (requested at {_pendingVisibilityRestoreStartTime})");
                    ForceFinishLingeringEncounter("VisibilityRestoreTimeout");
                    _pendingVisibilityRestoreStartTime = CampaignTime.Now;
                }

                NextFrameDispatcher.RunNextFrame(RestoreVisibilityAfterEncounter, true);
                return;
            }

            // CRITICAL: Don't restore visibility while the player is a prisoner.
            // Native PlayerCaptivity deactivates the party; fighting it causes state corruption.
            // Keep waiting until captivity ends (native will restore party state on release).
            if (mainHero != null && mainHero.IsPrisoner)
            {
                NextFrameDispatcher.RunNextFrame(RestoreVisibilityAfterEncounter, true);
                return;
            }

            _pendingVisibilityRestore = false;
            _pendingVisibilityRestoreStartTime = CampaignTime.Zero;

            // CRITICAL: If in grace period, apply protection BEFORE making visible
            // This prevents enemies from immediately attacking the player after discharge
            if (IsInDesertionGracePeriod)
            {
                var protectionUntil = CampaignTime.Now + CampaignTime.Hours(12f);
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

            // If the lord party vanished (captor destroyed/captured), companion restore will skip safely.
            if (_enlistedLord?.PartyBelongedTo == null)
            {
                ModLogger.Warn("EventSafety", "Capture cleanup: lord party missing, companion restore may be skipped");
            }

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

                // CRITICAL: Do NOT activate party while prisoner - captivity system owns player state
                // Making the party active/visible while prisoner allows enemies to attack us in dungeon
                if (Hero.MainHero?.IsPrisoner == true)
                {
                    ModLogger.Info("Captivity", "Skipping grace protection activation - player is prisoner");
                    // Still set the protection timer so it's ready when released
                    _graceProtectionEnds = CampaignTime.Now + CampaignTime.Hours(12f);
                    return;
                }

                // Skip if the game still has an encounter/battle running
                if (main.Party?.MapEvent != null || PlayerEncounter.Current != null)
                {
                    ModLogger.Debug("GraceProtection", "Delaying interaction window - still in encounter/battle state");
                    NextFrameDispatcher.RunNextFrame(GrantGracePeriodInteractionWindow, true);
                    return;
                }

                var protectionUntil = CampaignTime.Now + CampaignTime.Hours(12f);
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
                ModLogger.ErrorCode("GraceProtection", "E-GRACE-001", "Failed to grant grace period interaction window", ex);
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
                ModLogger.ErrorCode("Battle", "E-BATTLE-012", "Error restoring campaign flow after battle", ex);
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
                ModLogger.ErrorCode("EncounterCleanup", "E-ENCOUNTER-002",
                    $"Failed to finish lingering PlayerEncounter after {context}", ex);
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
                ModLogger.ErrorCode("Naval", "E-NAVAL-003", "Error validating army state", ex);
                
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
                ModLogger.ErrorCode("Diagnostics", "E-DIAG-001", "Error logging party state", ex);
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
                ModLogger.ErrorCode("Battle", "E-BATTLE-013", "Error in OnAfterMissionStarted", ex);
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
        ///     Whether the player has completed the initial configured term with this faction.
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
        ///     For first term: uses configured FirstTermDays. For renewal terms: uses configured RenewalTermDays.
        /// </summary>
        public CampaignTime CurrentTermEnd { get; set; } = CampaignTime.Zero;

        /// <summary>
        ///     Whether the player is currently in a renewal term (post-first-term service).
        ///     Renewal terms use the configured RenewalTermDays value and offer bonus/discharge rewards.
        /// </summary>
        public bool IsInRenewalTerm { get; set; }

        /// <summary>
        ///     Number of completed renewal terms after the first full term.
        /// </summary>
        public int RenewalTermsCompleted { get; set; }
    }
}
