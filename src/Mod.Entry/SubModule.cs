using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Enlisted.Features.Assignments.Behaviors;
using Enlisted.Features.Combat.Behaviors;
using Enlisted.Features.CommandTent.Core;
using Enlisted.Features.CommandTent.Systems;
using Enlisted.Features.Activities;
using Enlisted.Features.Camp;
using Enlisted.Features.Conversations.Behaviors;
using Enlisted.Features.Escalation;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Features.Equipment.Behaviors;
using Enlisted.Features.Equipment.UI;
using Enlisted.Features.Interface.Behaviors;
using Enlisted.Features.Lances.Behaviors;
using Enlisted.Features.Lances.Events;
using Enlisted.Features.Lances.Personas;
using Enlisted.Features.Conditions;
using Enlisted.Features.Ranks.Behaviors;
using Enlisted.Mod.Core.Config;
using Enlisted.Mod.Core.Logging;
using Enlisted.Mod.Core;
using Enlisted.Mod.Core.Triggers;
using Enlisted.Mod.GameAdapters.Patches;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Enlisted.Mod.Entry
{
    /// <summary>
    ///     Defers action execution to the next game frame to prevent race conditions during game state transitions.
    ///     The RGL (Rendering Graphics Library) skeleton system performs time-based updates during encounter exits.
    ///     Executing menu transitions or state changes during these updates can cause assertion failures because
    ///     the system expects a non-zero time delta, but rapid re-entrant calls can create zero-delta situations.
    ///     By deferring actions, we ensure they execute after the critical update phase completes.
    /// </summary>
    public static class NextFrameDispatcher
    {
        private static readonly Queue<DeferredAction> NextFrameQueue = new Queue<DeferredAction>();

        /// <summary>
        ///     Queues an action to execute on the next game frame tick instead of immediately.
        ///     Used for menu activations, encounter finishing, and other state transitions that must occur
        ///     after the current frame's critical updates complete. This prevents timing conflicts with
        ///     the game's internal systems during state transitions.
        /// </summary>
        /// <param name="action">The action to execute on the next frame. Null actions are ignored.</param>
        /// <param name="requireNoEncounter">
        ///     When true, the action will be deferred until no player encounter is active.
        ///     This allows callers to delay sensitive transitions (like menu activation) until
        ///     the encounter lifecycle has fully completed.
        /// </param>
        public static void RunNextFrame(Action action, bool requireNoEncounter = false)
        {
            if (action != null)
            {
                NextFrameQueue.Enqueue(new DeferredAction(action, requireNoEncounter));
            }
        }

        /// <summary>
        ///     Executes queued deferred actions, processing them in FIFO order.
        ///     Called automatically by the Harmony patch on Campaign.Tick() after the native tick completes.
        ///     Actions that request "no encounter" will be re-queued until PlayerEncounter.Current is null.
        /// </summary>
        public static void ProcessNextFrame()
        {
            if (NextFrameQueue.Count == 0)
            {
                return;
            }

            var itemsToProcess = NextFrameQueue.Count;
            while (itemsToProcess-- > 0 && NextFrameQueue.Count > 0)
            {
                var deferred = NextFrameQueue.Dequeue();
                if (deferred?.Action == null)
                {
                    continue;
                }

                if (deferred.RequireNoEncounter && PlayerEncounter.Current != null)
                {
                    // Re-queue until the encounter fully finishes
                    NextFrameQueue.Enqueue(deferred);
                    continue;
                }

                try
                {
                    deferred.Action();
                }
                catch (Exception ex)
                {
                    ModLogger.Error("NextFrameDispatcher", $"Error processing next frame action: {ex.Message}");
                }
            }
        }

        private sealed class DeferredAction
        {
            public DeferredAction(Action action, bool requireNoEncounter)
            {
                Action = action;
                RequireNoEncounter = requireNoEncounter;
            }

            public Action Action { get; }
            public bool RequireNoEncounter { get; }
        }
    }

    public class SubModule : MBSubModuleBase
    {
        private Harmony _harmony;
        private ModSettings _settings;

        /// <summary>
        ///     Called when the mod module is first loaded by Bannerlord, before any game starts.
        ///     Initializes logging system and applies Harmony patches to game methods.
        ///     This happens once per game session, not per save game.
        /// </summary>
        protected override void OnSubModuleLoad()
        {
            try
            {
                // Initialize logging system - this clears old logs and starts fresh
                ModLogger.Initialize();
                ModLogger.Info("Bootstrap", "SubModule loading");

                // Create Harmony instance with a unique identifier to avoid patch collisions
                // PatchAll() automatically discovers and applies all [HarmonyPatch] attributes in the assembly
                _harmony = new Harmony("com.enlisted.mod");

                try
                {
                    // Reference patch types to satisfy static analysis (ReSharper)
                    // These are discovered via reflection by Harmony.PatchAll()
                    // Listed alphabetically by patch file for easy maintenance
                    _ = typeof(AbandonArmyBlockPatch);
                    _ = typeof(EncounterAbandonArmyBlockPatch);
                    _ = typeof(EncounterAbandonArmyBlockPatch2);
                    _ = typeof(ArmyCohesionExclusionPatch);
                    _ = typeof(ArmyDispersedMenuPatch);
                    _ = typeof(CheckFortificationAttackablePatch);
                    _ = typeof(CompanionCaptainBlockPatch);
                    _ = typeof(CompanionGeneralBlockPatch);
                    _ = typeof(DischargeRelationPenaltyPatch);
                    _ = typeof(DutiesEffectiveEngineerPatch);
                    _ = typeof(DutiesEffectiveScoutPatch);
                    _ = typeof(DutiesEffectiveQuartermasterPatch);
                    _ = typeof(DutiesEffectiveSurgeonPatch);
                    _ = typeof(EncounterSuppressionPatch);
                    _ = typeof(EndCaptivityCleanupPatch);
                    _ = typeof(EnlistedWaitingPatch);
                    _ = typeof(FoodSystemPatches);
                    _ = typeof(FoodSystemPatches.VirtualFoodLinkPatch);
                    _ = typeof(FoodSystemPatches.SharedFoodConsumptionPatch);
                    _ = typeof(FormationMessageSuppressionPatch);
                    _ = typeof(GenericStateMenuPatch);
                    _ = typeof(HidePartyNamePlatePatch);
                    _ = typeof(IncidentsSuppressionPatch);
                    _ = typeof(InfluenceMessageSuppressionPatch);
                    _ = typeof(JoinEncounterAutoSelectPatch);
                    _ = typeof(LootBlockPatch);
                    _ = typeof(LootBlockPatch.ItemLootPatch);
                    _ = typeof(LootBlockPatch.MemberLootPatch);
                    _ = typeof(LootBlockPatch.PrisonerLootPatch);
                    _ = typeof(LootBlockPatch.LootScreenPatch);
                    _ = typeof(NavalBattleArmyWaitCrashFix);
                    _ = typeof(NavalBattleShipAssignmentPatch);
                    _ = typeof(NavalShipDamageProtectionPatch);
                    _ = typeof(OrderOfBattleSuppressionPatch);
                    _ = typeof(PlayerIsAtSeaTagCrashFix);
                    _ = typeof(PostDischargeProtectionPatch);
                    _ = typeof(RaftStateSuppressionPatch);
                    _ = typeof(ReturnToArmySuppressionPatch);
                    _ = typeof(SettlementOutsideLeaveButtonPatch);
                    _ = typeof(SkillSuppressionPatch);
                    _ = typeof(TownLeaveButtonPatch);
                    _ = typeof(VisibilityEnforcementPatch);

                    // These patches are deferred until first Campaign.Tick() to avoid TypeInitializationException.
                    // Their target classes (DefaultArmyManagementCalculationModel, EncounterGameMenuBehavior)
                    // have static fields that call GameTexts.FindText() before the localization system is ready.
                    // This causes crashes on Proton/Linux and potential issues on Windows under some conditions.
                    var manualPatches = new HashSet<string>
                    {
                        "HidePartyNamePlatePatch",              // Uses manual patching via ApplyManualPatches()
                        "ArmyCohesionExclusionPatch",           // Target: DefaultArmyManagementCalculationModel
                        "SettlementOutsideLeaveButtonPatch",    // Target: EncounterGameMenuBehavior
                        "JoinEncounterAutoSelectPatch",         // Target: EncounterGameMenuBehavior
                        "EncounterAbandonArmyBlockPatch",       // Target: EncounterGameMenuBehavior (deferred)
                        "EncounterAbandonArmyBlockPatch2"       // Target: EncounterGameMenuBehavior (deferred)
                    };

                    var assembly = Assembly.GetExecutingAssembly();
                    var patchTypes = assembly.GetTypes()
                        .Where(t => t.GetCustomAttributes(typeof(HarmonyPatch), true).Length > 0 ||
                                    t.GetNestedTypes().Any(n =>
                                        n.GetCustomAttributes(typeof(HarmonyPatch), true).Length > 0));

                    var enabledCount = 0;
                    var skippedCount = 0;

                    foreach (var type in patchTypes)
                    {
                        // Skip patches that use manual patching
                        if (manualPatches.Contains(type.Name))
                        {
                            ModLogger.Info("Bootstrap", $"Skipping {type.Name} (uses manual patching)");
                            skippedCount++;
                            continue;
                        }

                        try
                        {
                            _harmony.CreateClassProcessor(type).Patch();
                            enabledCount++;
                        }
                        catch (Exception patchEx)
                        {
                            ModLogger.Error("Bootstrap", $"Failed to patch {type.Name}: {patchEx.Message}");
                            if (patchEx.InnerException != null)
                            {
                                ModLogger.Error("Bootstrap", $"  Inner exception: {patchEx.InnerException.Message}");
                            }
                            ModLogger.Debug("Bootstrap", $"  Stack trace: {patchEx.StackTrace}");
                        }
                    }

                    // Deferred patches will be applied later when campaign starts
                    // Applying during SubModule load causes TypeInitializationException on Proton/Linux
                    ModLogger.Info("Bootstrap", $"{skippedCount} patches deferred until campaign start");

                    ModLogger.Info("Bootstrap", $"Harmony patches: {enabledCount} auto, {skippedCount} manual");
                }
                catch (Exception ex)
                {
                    ModLogger.Error("Bootstrap", $"Harmony PatchAll failed: {ex.Message}\n{ex.StackTrace}");
                }

                // Log all patched methods for debugging
                var patchedMethods = _harmony.GetPatchedMethods();
                foreach (var method in patchedMethods)
                {
                    ModLogger.Info("Bootstrap", $"Patched method: {method.DeclaringType?.Name}.{method.Name}");
                }

                // Run mod conflict diagnostics and write to Debugging/conflicts.log
                // This helps users identify when other mods interfere with Enlisted
                ModConflictDiagnostics.RunStartupDiagnostics(_harmony);

                ModLogger.Info("Bootstrap", "Harmony patched");
            }
            catch (Exception ex)
            {
                // If patching fails, log the error but don't crash the game
                // The mod may still function partially without patches, and crashing would prevent the player from playing
                ModLogger.Error("Bootstrap", "Exception during OnSubModuleLoad", ex);
            }
        }

        /// <summary>
        ///     Called when a new game starts or when loading a save game.
        ///     Registers all campaign behaviors that manage the military service system throughout the campaign.
        ///     Campaign behaviors receive events from the game (hourly ticks, daily ticks, party encounters, etc.)
        ///     and can modify game state in response. They persist across save/load and are serialized automatically.
        /// </summary>
        /// <param name="game">The game instance being started.</param>
        /// <param name="gameStarterObject">Provides methods to register campaign behaviors and game menus.</param>
        protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
        {
            try
            {
                ModLogger.Info("Bootstrap", "Game start");
                EnlistedActivation.SetActive(false, "game_start");

                // Log startup diagnostics once per session for troubleshooting
                SessionDiagnostics.LogStartupDiagnostics();

                // Load mod settings from JSON configuration file in ModuleData folder
                // Settings control logging verbosity, encounter suppression, and feature toggles
                _settings = ModSettings.LoadFromModule();
                ModConfig.Settings = _settings;

                // Apply log level configuration from settings
                // This enables per-category verbosity control and message throttling
                _settings.ApplyLogLevels();

                // Log configuration values for verification
                SessionDiagnostics.LogConfigurationValues();

                if (gameStarterObject is CampaignGameStarter campaignStarter)
                {
                    // Save/load diagnostics: two marker behaviors registered first/last so we can log
                    // user-friendly "Saving..." / "Save finished" and "Loading..." / "Load finished" lines.
                    campaignStarter.AddBehavior(new SaveLoadDiagnosticsMarkerBehavior(SaveLoadDiagnosticsMarkerBehavior.Phase.Begin));

                    // Core enlistment system: tracks which lord the player serves, manages enlistment state,
                    // handles party following, battle participation, and leave/temporary absence
                    campaignStarter.AddBehavior(new EnlistmentBehavior());

                    // Incidents: registers enlistment-specific incidents (e.g., deferred bag check)
                    campaignStarter.AddBehavior(new EnlistedIncidentsBehavior());

                    // Conversation system: adds dialog options to talk with lords about enlistment,
                    // service status, promotions, and requesting leave
                    campaignStarter.AddBehavior(new EnlistedDialogManager());

                    // Duties system: manages military assignments (duties and professions) that provide
                    // daily skill XP, wage multipliers, and officer role assignments
                    campaignStarter.AddBehavior(new EnlistedDutiesBehavior());

                    // Menu system: provides the main enlisted status menu and duty/profession selection interface
                    // Handles menu state transitions, battle detection, and settlement access
                    campaignStarter.AddBehavior(new EnlistedMenuBehavior());

                    // Troop selection: allows players to choose which troop type to represent during service,
                    // which determines formation (Infantry/Cavalry/Archer/Horse Archer) and equipment access
                    campaignStarter.AddBehavior(new TroopSelectionManager());

                    // Equipment management: handles equipment backups and restoration when leaving service,
                    // ensures players get their personal gear back when they end their enlistment
                    campaignStarter.AddBehavior(new EquipmentManager());

                    // Promotion system: checks XP thresholds hourly and promotes players through military ranks
                    // Triggers formation selection at tier 2 and handles promotion notifications
                    campaignStarter.AddBehavior(new PromotionBehavior());

                    // Quartermaster system: manages equipment variant selection when players can choose
                    // between different equipment sets at their tier level
                    campaignStarter.AddBehavior(new QuartermasterManager());

                    // Quartermaster UI: provides the grid-based equipment selection interface where players
                    // can click on individual equipment pieces to see stats and select variants
                    campaignStarter.AddBehavior(new QuartermasterEquipmentSelectorBehavior());

                    // Phase 1 foundation: shared trigger vocabulary + minimal recent-history persistence (no scanning loops).
                    campaignStarter.AddBehavior(new CampaignTriggerTrackerBehavior());

                    // Phase 5 (optional): named lance role personas (text-only roster). Feature-flagged.
                    campaignStarter.AddBehavior(new LancePersonaBehavior());

                    // Phase 5: player conditions (injury/illness/exhaustion). Feature-flagged.
                    campaignStarter.AddBehavior(new PlayerConditionBehavior());

                    // Phase 2 (menu_system_update): data-driven camp activities menu. Feature-flagged.
                    campaignStarter.AddBehavior(new CampActivitiesBehavior());

                    // Lance Life (text events): Viking Conquest-style camp activities and stories tied to lance identity
                    campaignStarter.AddBehavior(new LanceStoryBehavior());

                    // Lance Life Events (shared state): persisted cooldowns + one-time fired ids.
                    campaignStarter.AddBehavior(new LanceLifeEventsStateBehavior());

                    // Lance Life Events (Phase 4): onboarding state machine (stage/track/variant), feature-flagged.
                    campaignStarter.AddBehavior(new LanceLifeOnboardingBehavior());

                    // Lance Life Events (Phase 2): automatic scheduler (tick evaluation + queueing), feature-flagged.
                    campaignStarter.AddBehavior(new LanceLifeEventsAutomaticBehavior());

                    // Lance Life Events (Phase 5b): incident channel delivery (MapState.NextIncident), feature-flagged.
                    campaignStarter.AddBehavior(new LanceLifeEventsIncidentBehavior());

                    // My Lance menu: roster view, relationships, wounded/fallen tracking
                    campaignStarter.AddBehavior(new EnlistedLanceMenuBehavior());

                    // Medical menu: treatment options when injured/ill/exhausted
                    campaignStarter.AddBehavior(new EnlistedMedicalMenuBehavior());

                    // Battle encounter system: detects when the lord enters battle and handles player participation,
                    // manages menu transitions during battles, and provides battle wait menu options
                    campaignStarter.AddBehavior(new EnlistedEncounterBehavior());

                    // Service records: tracks faction-specific and lifetime statistics
                    campaignStarter.AddBehavior(new ServiceRecordManager());

                    // Camp UI: provides menus for viewing service records (current posting,
                    // faction history, lifetime summary) and future retinue management
                    campaignStarter.AddBehavior(new CampMenuHandler());

                    // Camp Life Simulation (Phase 3): daily snapshot + Quartermaster/Pay integrations (gated by config).
                    campaignStarter.AddBehavior(new CampLifeBehavior());

                    // Phase 4: escalation tracks (heat/discipline/lance rep/medical risk). Feature-flagged.
                    campaignStarter.AddBehavior(new EscalationManager());

                    // Retinue trickle system: adds free soldiers over time (every 2-3 days)
                    campaignStarter.AddBehavior(new RetinueTrickleSystem());

                    // Retinue lifecycle handler: clears retinue on capture, discharge, lord death, army defeat
                    campaignStarter.AddBehavior(new RetinueLifecycleHandler());

                    // Retinue casualty tracker: reconciles troop counts after battles
                    campaignStarter.AddBehavior(new RetinueCasualtyTracker());

                    // Companion assignment manager: tracks which companions should fight vs stay back
                    // Companions marked "stay back" don't spawn in battle, keeping them safe
                    campaignStarter.AddBehavior(new CompanionAssignmentManager());

                    // Save/load diagnostics end marker: registered last so it runs after all other behaviors
                    // during save/load serialization passes.
                    campaignStarter.AddBehavior(new SaveLoadDiagnosticsMarkerBehavior(SaveLoadDiagnosticsMarkerBehavior.Phase.End));

                    // Encounter guard: utility system for managing player party attachment and encounter transitions
                    // Initializes static helper methods used throughout the enlistment system
                    EncounterGuard.Initialize();
                    ModLogger.Info("Bootstrap", "Military service behaviors registered successfully");
                    
                    // Log registered behaviors for conflict diagnostics
                    // This helps troubleshoot issues by showing exactly what was registered
                    // NOTE: Use classic collection initializer (ReSharper can choke on newer collection expressions).
                    ModConflictDiagnostics.LogRegisteredBehaviors(new List<string>
                    {
                        nameof(SaveLoadDiagnosticsMarkerBehavior),
                        nameof(EnlistmentBehavior),
                        nameof(EnlistedIncidentsBehavior),
                        nameof(EnlistedDialogManager),
                        nameof(EnlistedDutiesBehavior),
                        nameof(EnlistedMenuBehavior),
                        nameof(TroopSelectionManager),
                        nameof(EquipmentManager),
                        nameof(PromotionBehavior),
                        nameof(QuartermasterManager),
                        nameof(QuartermasterEquipmentSelectorBehavior),
                        nameof(CampaignTriggerTrackerBehavior),
                        nameof(LancePersonaBehavior),
                        nameof(PlayerConditionBehavior),
                        nameof(CampActivitiesBehavior),
                        nameof(LanceStoryBehavior),
                        nameof(LanceLifeEventsStateBehavior),
                        nameof(LanceLifeOnboardingBehavior),
                        nameof(LanceLifeEventsAutomaticBehavior),
                        nameof(LanceLifeEventsIncidentBehavior),
                        nameof(EnlistedEncounterBehavior),
                        nameof(ServiceRecordManager),
                        nameof(CampMenuHandler),
                        nameof(CampLifeBehavior),
                        nameof(EscalationManager),
                        nameof(RetinueTrickleSystem),
                        nameof(RetinueLifecycleHandler),
                        nameof(RetinueCasualtyTracker),
                        nameof(CompanionAssignmentManager)
                    });
                }
            }
            catch (Exception ex)
            {
                // If behavior registration fails, log the error but allow the game to continue
                // Some behaviors may still be registered if the failure occurs partway through
                ModLogger.Error("Bootstrap", "Exception during OnGameStart", ex);
            }
        }

        /// <summary>
        ///     Called when a mission (battle, siege, etc.) initializes its behaviors.
        ///     This is the proper place to add mission-specific behaviors like kill tracking.
        ///     Unlike dynamic addition during AfterMissionStarted, this runs before mission lifecycle begins.
        /// </summary>
        /// <param name="mission">The mission being initialized.</param>
        public override void OnMissionBehaviorInitialize(Mission mission)
        {
            try
            {
                base.OnMissionBehaviorInitialize(mission);

                // DIAGNOSTIC: Log that we entered the method
                ModLogger.Info("Mission", $"OnMissionBehaviorInitialize called (Mode: {mission.Mode})");

                var enlistment = EnlistmentBehavior.Instance;

                // DIAGNOSTIC: Log enlistment state before checking
                ModLogger.Info("Mission",
                    $"Enlistment check - Instance: {enlistment != null}, IsEnlisted: {enlistment?.IsEnlisted}, OnLeave: {enlistment?.IsOnLeave}");

                if (enlistment?.IsEnlisted != true)
                {
                    ModLogger.Info("Mission", "Skipping behavior add - not enlisted");
                    return;
                }

                // Check if this is a combat mission (field battle, siege, hideout, etc.)
                // Skip non-combat missions like conversations, tournaments, arenas
                // NOTE: Siege battles start in StartUp mode and transition to Battle mode later,
                // so we must include StartUp to catch them at initialization time.
                if (mission.Mode == MissionMode.Battle ||
                    mission.Mode == MissionMode.StartUp ||
                    mission.Mode == MissionMode.Stealth ||
                    mission.Mode == MissionMode.Deployment)
                {
                    mission.AddMissionBehavior(new EnlistedKillTrackerBehavior());
                    mission.AddMissionBehavior(new EnlistedFormationAssignmentBehavior());
                    ModLogger.Info("Mission", $"Enlisted behaviors added to mission (Mode: {mission.Mode})");
                }
                else
                {
                    ModLogger.Info("Mission", $"Skipped behaviors - mode {mission.Mode} not a combat mission");
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("Bootstrap", $"Error in OnMissionBehaviorInitialize: {ex.Message}");
            }
        }
    }

    /// <summary>
    ///     Harmony patch that hooks into Campaign.Tick() to process deferred actions on the next frame.
    ///     Campaign.Tick() runs every frame of the campaign map and handles all campaign-level updates.
    ///     By executing in Postfix, we run after the native tick completes, ensuring game state is stable.
    /// </summary>
    [HarmonyPatch(typeof(Campaign), "Tick")]
    public static class NextFrameDispatcherPatch
    {
        private static bool _deferredPatchesApplied;

        /// <summary>
        ///     Called after Campaign.Tick() completes each frame.
        ///     Processes any actions that were deferred to avoid timing conflicts during game state transitions.
        ///     Also applies deferred patches on first tick (after character creation is complete).
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("ReSharper", "UnusedMember.Local",
            Justification = "Called by Harmony via [HarmonyPatch] postfix")]
        private static void Postfix()
        {
            // Apply deferred patches only after the campaign is *fully* ready.
            // On some platforms (notably Proton/Linux), Campaign.Tick can run during character creation,
            // and touching certain menu/localization-heavy types at that time can crash the game.
            if (!_deferredPatchesApplied && CampaignSafetyGuard.IsCampaignReady)
            {
                try
                {
                    _deferredPatchesApplied = true;
                    var harmony = new Harmony("com.enlisted.mod.deferred");

                    // Apply manual patches
                    HidePartyNamePlatePatch.ApplyManualPatches(harmony);

                    // Apply patches that target types with early static initialization.
                    // These fail on Proton/Linux if applied during OnSubModuleLoad because
                    // their target classes call GameTexts.FindText() in static field initializers.
                    // By deferring until the campaign is ready, the localization system is fully initialized.
                    // Each patch is wrapped individually so one failure doesn't block others.
                    ApplyDeferredPatch(harmony, typeof(ArmyCohesionExclusionPatch));
                    ApplyDeferredPatch(harmony, typeof(SettlementOutsideLeaveButtonPatch));
                    ApplyDeferredPatch(harmony, typeof(JoinEncounterAutoSelectPatch));
                    ApplyDeferredPatch(harmony, typeof(EncounterAbandonArmyBlockPatch));
                    ApplyDeferredPatch(harmony, typeof(EncounterAbandonArmyBlockPatch2));

                    // Apply Naval DLC patches that use reflection to find types.
                    // These must be deferred because Naval DLC types aren't available during OnSubModuleLoad.
                    RaftStateSuppressionPatch.TryApplyPatch(harmony);
                    RaftStateSuppressionPatch.TryApplyOnPartyLeftArmyPatch(harmony);

                    ModLogger.Info("Bootstrap", "Deferred patches applied (campaign ready)");

                    // Update conflict diagnostics with deferred patch info
                    // This appends to the existing conflicts.log so users can see all patches
                    ModConflictDiagnostics.RefreshDeferredPatches(harmony);
                }
                catch (Exception ex)
                {
                    // Hard safety: never allow a failure during deferred patch application to crash the game.
                    // If something goes wrong here, we'll run without deferred patches.
                    ModLogger.Error("Bootstrap", $"Unexpected error applying deferred patches: {ex.Message}");
                }
            }

            NextFrameDispatcher.ProcessNextFrame();
        }

        /// <summary>
        /// Applies a deferred patch with detailed error logging.
        /// Isolates failures so one broken patch doesn't block others.
        /// </summary>
        private static void ApplyDeferredPatch(Harmony harmony, Type patchType)
        {
            try
            {
                harmony.CreateClassProcessor(patchType).Patch();
                ModLogger.Info("Bootstrap", $"Applied deferred patch: {patchType.Name}");
            }
            catch (Exception ex)
            {
                ModLogger.Error("Bootstrap", $"Failed to apply deferred patch {patchType.Name}: {ex.Message}");
                if (ex.InnerException != null)
                {
                    ModLogger.Error("Bootstrap", $"  Inner exception: {ex.InnerException.Message}");
                }
                ModLogger.Debug("Bootstrap", $"  Stack trace: {ex.StackTrace}");
            }
        }
    }
}
