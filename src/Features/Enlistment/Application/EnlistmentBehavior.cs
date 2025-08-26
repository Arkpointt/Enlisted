using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.SaveSystem;
using Enlisted.Core.Models;
using Enlisted.Core.Constants;
using Enlisted.Core.Logging;
using Enlisted.Core.DependencyInjection;
using Enlisted.Features.Enlistment.Infrastructure;

namespace Enlisted.Features.Enlistment.Application
{
    /// <summary>
    /// Application orchestrator for enlistment feature implementing dependency injection patterns.
    /// Coordinates between domain models and infrastructure services.
    /// Handles campaign integration and event orchestration per blueprint.
    /// 
    /// Implements IEnlistmentService to replace static Instance pattern (ADR-004).
    /// </summary>
    public class EnlistmentBehavior : CampaignBehaviorBase, IEnlistmentService
    {
        [SaveableField(1)] private EnlistmentState _state = new EnlistmentState();
        private ILoggingService _logger;

        // DEPRECATED: Static instance for backward compatibility during transition
        // TODO: Remove when all GameAdapters use dependency injection
        public static EnlistmentBehavior Instance { get; private set; }

        // IEnlistmentService implementation
        public bool IsEnlisted => _state.IsEnlisted;
        public Hero Commander => _state.Commander;
        public bool IsCommanderValid() => _state.IsCommanderValid();

        public EnlistmentBehavior()
        {
            // Temporary: Set static instance for GameAdapters during transition
            Instance = this;
        }

        public override void RegisterEvents()
        {
            // Initialize logging service
            if (ServiceLocator.TryGetService<ILoggingService>(out _logger))
            {
                _logger.LogInfo(LogCategories.Enlistment, "EnlistmentBehavior initialized with dependency injection");
            }
            else
            {
                // Fallback for transition period
                Debug.Print("[Enlisted] EnlistmentBehavior: Warning - Logging service not available");
            }

            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.TickEvent.AddNonSerializedListener(this, OnTick);
            CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, OnHourlyTick);
            
            // Settlement following events
            CampaignEvents.SettlementEntered.AddNonSerializedListener(this, OnSettlementEntered);
            CampaignEvents.OnSettlementLeftEvent.AddNonSerializedListener(this, OnSettlementLeft);
            
            // Battle participation events
            CampaignEvents.MapEventStarted.AddNonSerializedListener(this, OnMapEventStarted);

            _logger?.LogInfo(LogCategories.Enlistment, "Event listeners registered successfully");
        }

        public override void SyncData(IDataStore data)
        {
            data.SyncData(nameof(_state), ref _state);

            // Sync visibility state after loading
            if (_state != null)
            {
                PartyIllusionService.SetOriginalVisibilityState(_state.PlayerPartyWasVisible);
                _logger?.LogInfo(LogCategories.Persistence, "Enlistment state loaded from save game. IsEnlisted: {0}", _state.IsEnlisted);
            }
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            _logger?.LogInfo(LogCategories.Enlistment, "Campaign session launched, registering dialogs");

            ConversationSentence.OnConditionDelegate canEnlist = () =>
                !_state.IsEnlisted &&
                Hero.OneToOneConversationHero != null &&
                Hero.OneToOneConversationHero != Hero.MainHero &&
                Hero.OneToOneConversationHero.PartyBelongedTo != null;

            ConversationSentence.OnConditionDelegate canLeave = () =>
                _state.IsEnlisted &&
                Hero.OneToOneConversationHero != null &&
                Hero.OneToOneConversationHero == _state.Commander;

            // Register dialogs through presentation layer
            DialogService.RegisterDialogs(starter, Constants.MAIN_HUBS, canEnlist, canLeave, OnEnlist, OnLeave);
        }

        private void OnTick(float _)
        {
            if (_state.PendingDetach)
            {
                _state.CompletePendingDetach();
                PartyIllusionService.RestorePlayerPartyVisibility();
                _state.SetPlayerPartyVisibility(PartyIllusionService.GetOriginalVisibilityState());
                _logger?.LogDebug(LogCategories.Enlistment, "Completed pending detach from service");
            }

            // Maintain enlistment state while active
            if (_state.IsEnlisted && _state.Commander != null && _state.Commander.IsAlive)
            {
                var main = MobileParty.MainParty;
                var commanderParty = _state.Commander.PartyBelongedTo;

                // Validate commander state
                if (commanderParty == null || !commanderParty.IsActive)
                {
                    _logger?.LogWarning(LogCategories.Enlistment, "Commander {0} is no longer valid, ending service", 
                        _state.Commander.Name?.ToString() ?? "Unknown");
                    
                    ArmyIntegrationService.SafeDetach();
                    _state.ForceEndService();
                    PartyIllusionService.RestorePlayerPartyVisibility();
                    _logger?.ShowPlayerMessage(Constants.Messages.SERVICE_ENDED);
                    return;
                }

                // Maintain party control mechanics
                if (main.CurrentSettlement == null) // Only on campaign map
                {
                    main.Position2D = commanderParty.Position2D;
                    main.IsActive = false; // Prevents encounter targeting
                    commanderParty.Party.SetAsCameraFollowParty();
                }

                // Handle battle participation
                HandleBattleParticipation(main, commanderParty);

                // Maintain visual illusion and escort behavior
                PartyIllusionService.MaintainIllusion(_state.Commander);
                EnsureEscortBehavior();
            }
            else if (!_state.IsEnlisted)
            {
                // Ensure main party is active when not enlisted
                var main = MobileParty.MainParty;
                if (!main.IsActive)
                {
                    main.IsActive = true;
                    main.Party.SetAsCameraFollowParty();
                }
            }
        }

        private void HandleBattleParticipation(MobileParty main, MobileParty commanderParty)
        {
            bool commanderInBattle = commanderParty.MapEvent != null;
            bool playerNotInBattle = main.MapEvent == null;
            bool notWaitingInReserve = !_state.WaitingInReserve;

            if (commanderInBattle && playerNotInBattle && notWaitingInReserve)
            {
                _logger?.LogInfo(LogCategories.Enlistment, "Commander entered battle, joining...");
                JoinCommandersBattle(commanderParty);
            }
            else if (!commanderInBattle && main.IsActive && main.CurrentSettlement == null)
            {
                main.IsActive = false; // Return to inactive state
            }
        }

        private void JoinCommandersBattle(MobileParty commanderParty)
        {
            var main = MobileParty.MainParty;
            
            try
            {
                _logger?.LogDebug(LogCategories.Enlistment, "Attempting to join commander's battle");

                // Clean exit from any current menu context
                while (Campaign.Current.CurrentMenuContext != null)
                {
                    GameMenu.ExitToLast();
                }

                // Ensure commander has army for battle
                if (commanderParty.Army == null)
                {
                    var kingdom = commanderParty.ActualClan?.Kingdom;
                    if (kingdom != null)
                    {
                        kingdom.CreateArmy(commanderParty.LeaderHero, commanderParty.HomeSettlement, Army.ArmyTypes.Patrolling);
                        _logger?.LogDebug(LogCategories.Enlistment, "Created temporary army for battle");
                    }
                }

                // Join army for battle
                if (commanderParty.Army != null)
                {
                    commanderParty.Army.AddPartyToMergedParties(main);
                    main.Army = commanderParty.Army;
                    main.IsActive = true;
                    main.Ai.SetMoveEngageParty(commanderParty);

                    var message = $"[Enlisted] Joining {_state.Commander.Name} in battle!";
                    _logger?.ShowPlayerMessage(message);
                    _logger?.LogInfo(LogCategories.Enlistment, "Successfully joined commander's battle");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(LogCategories.Enlistment, "Failed to join commander's battle: {0}", ex);
            }
        }

        private void OnHourlyTick()
        {
            if (!_state.IsEnlisted) return;

            if (!_state.IsCommanderValid())
            {
                _logger?.LogWarning(LogCategories.Enlistment, "Commander validation failed during hourly tick, ending service");
                
                ArmyIntegrationService.SafeDetach();
                _state.ForceEndService();
                PartyIllusionService.RestorePlayerPartyVisibility();
                _logger?.ShowPlayerMessage(Constants.Messages.SERVICE_ENDED);
            }
        }

        private void OnEnlist()
        {
            var commander = Hero.OneToOneConversationHero;
            _logger?.LogInfo(LogCategories.Enlistment, "Player enlisting under {0}", commander?.Name?.ToString() ?? "Unknown");
            
            _state.Enlist(commander);

            // Store original visibility state
            _state.SetPlayerPartyVisibility(PartyIllusionService.GetOriginalVisibilityState());

            // Attempt to join army
            if (ArmyIntegrationService.TryJoinCommandersArmy(commander))
            {
                PartyIllusionService.HidePlayerPartyAndFollowCommander(commander);
                _logger?.ShowPlayerMessage(string.Format(Constants.Messages.ENLISTED_SUCCESS, commander?.Name));
                _logger?.LogInfo(LogCategories.Enlistment, "Enlistment completed successfully");
            }
            else
            {
                _logger?.LogError(LogCategories.Enlistment, "Failed to join commander's army during enlistment");
            }
        }

        private void OnLeave()
        {
            _logger?.LogInfo(LogCategories.Enlistment, "Player leaving service");
            
            ArmyIntegrationService.LeaveCurrentArmy();
            _state.Leave();
            _logger?.ShowPlayerMessage(Constants.Messages.LEFT_SERVICE);
        }

        private void OnSettlementEntered(MobileParty party, Settlement settlement, Hero hero)
        {
            if (!_state.IsEnlisted || _state.Commander == null) return;

            if (hero == _state.Commander && party == _state.Commander.PartyBelongedTo)
            {
                _logger?.LogDebug(LogCategories.Enlistment, "Following commander {0} into settlement {1}", 
                    _state.Commander.Name, settlement.Name);

                _logger?.ShowPlayerMessage(string.Format(Constants.Messages.FOLLOWING_INTO_SETTLEMENT, 
                    _state.Commander.Name, settlement.Name));

                if (MobileParty.MainParty.CurrentSettlement != settlement)
                {
                    EncounterManager.StartSettlementEncounter(MobileParty.MainParty, settlement);
                }
            }
        }

        private void OnSettlementLeft(MobileParty party, Settlement settlement)
        {
            if (!_state.IsEnlisted || _state.Commander == null) return;

            if (party == _state.Commander.PartyBelongedTo)
            {
                _logger?.LogDebug(LogCategories.Enlistment, "Following commander out of settlement {0}", settlement.Name);
                _logger?.ShowPlayerMessage(Constants.Messages.FOLLOWING_COMMANDER);

                var currentMenuContext = Campaign.Current.CurrentMenuContext;
                if (currentMenuContext != null)
                {
                    var gameMenu = currentMenuContext.GameMenu;
                    
                    if (gameMenu != null && gameMenu.StringId != null && 
                        (gameMenu.StringId.Contains("wait") || gameMenu.StringId.Contains("settlement")))
                    {
                        gameMenu.EndWait();
                    }
                    
                    if (PlayerEncounter.Current != null)
                    {
                        PlayerEncounter.Finish(true);
                    }
                    else
                    {
                        GameMenu.ExitToLast();
                    }
                }

                if (MobileParty.MainParty.CurrentSettlement != null)
                {
                    LeaveSettlementAction.ApplyForParty(MobileParty.MainParty);
                }

                if (Campaign.Current.TimeControlMode == CampaignTimeControlMode.Stop)
                {
                    Campaign.Current.TimeControlMode = Campaign.Current.LastTimeControlMode;
                }
            }
        }

        private void OnMapEventStarted(MapEvent mapEvent, PartyBase attackerParty, PartyBase defenderParty)
        {
            if (!_state.IsEnlisted || _state.Commander == null) return;

            var commanderParty = _state.Commander.PartyBelongedTo?.Party;
            if (commanderParty == null) return;

            bool commanderIsAttacker = (attackerParty == commanderParty);
            bool commanderIsDefender = (defenderParty == commanderParty);
            
            if (!commanderIsAttacker && !commanderIsDefender) return;

            string actionType = commanderIsAttacker ? "attacking" : "defending";
            var message = $"[Enlisted] Commander {_state.Commander.Name} is {actionType} - prepare for battle!";
            _logger?.ShowPlayerMessage(message);
            _logger?.LogInfo(LogCategories.Enlistment, "Commander entered battle as {0}", actionType);
        }

        private void EnsureEscortBehavior()
        {
            var main = MobileParty.MainParty;
            var commanderParty = _state.Commander?.PartyBelongedTo;
            
            if (main == null || commanderParty == null) return;

            if (main.MapEvent == null && main.CurrentSettlement == null && commanderParty.MapEvent == null)
            {
                if (main.TargetParty != commanderParty)
                {
                    main.Ai.SetMoveEscortParty(commanderParty);
                }
            }
        }
    }
}
