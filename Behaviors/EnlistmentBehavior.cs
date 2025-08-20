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
using Enlisted.Models;
using Enlisted.Services;
using Enlisted.Utils;

namespace Enlisted.Behaviors
{
    /// <summary>
    /// Main behavior orchestrator for the Enlisted mod.
    /// Coordinates between services to provide seamless enlistment experience.
    /// </summary>
    public class EnlistmentBehavior : CampaignBehaviorBase
    {
        [SaveableField(1)] private EnlistmentState _state = new EnlistmentState();

        // Static instance for Harmony patches to access enlistment state
        public static EnlistmentBehavior Instance { get; private set; }

        // Public properties for backward compatibility and external access
        public bool IsEnlisted => _state.IsEnlisted;
        public Hero Commander => _state.Commander;

        public EnlistmentBehavior()
        {
            // Set the static instance when the behavior is created
            Instance = this;
        }

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.TickEvent.AddNonSerializedListener(this, OnTick);
            CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, OnHourlyTick);
            
            // Settlement following events
            CampaignEvents.SettlementEntered.AddNonSerializedListener(this, OnSettlementEntered);
            CampaignEvents.OnSettlementLeftEvent.AddNonSerializedListener(this, OnSettlementLeft);
            
            // 🆕 NEW: Battle participation events
            CampaignEvents.MapEventStarted.AddNonSerializedListener(this, OnMapEventStarted);
        }

        public override void SyncData(IDataStore data)
        {
            data.SyncData(nameof(_state), ref _state);

            // Sync the visibility state with the service after loading
            if (_state != null)
            {
                PartyIllusionService.SetOriginalVisibilityState(_state.PlayerPartyWasVisible);
            }
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            ConversationSentence.OnConditionDelegate canEnlist = () =>
                !_state.IsEnlisted &&
                Hero.OneToOneConversationHero != null &&
                Hero.OneToOneConversationHero != Hero.MainHero &&
                Hero.OneToOneConversationHero.PartyBelongedTo != null;

            ConversationSentence.OnConditionDelegate canLeave = () =>
                _state.IsEnlisted &&
                Hero.OneToOneConversationHero != null &&
                Hero.OneToOneConversationHero == _state.Commander;

            // Register dialogs using the service
            DialogService.RegisterDialogs(starter, Constants.MAIN_HUBS, canEnlist, canLeave, OnEnlist, OnLeave);
        }

        private void OnTick(float _)
        {
            if (_state.PendingDetach)
            {
                _state.CompletePendingDetach();
                PartyIllusionService.RestorePlayerPartyVisibility();
                _state.PlayerPartyWasVisible = PartyIllusionService.GetOriginalVisibilityState();
            }

            // Maintain the illusion while enlisted - ENHANCED VERSION
            if (_state.IsEnlisted && _state.Commander != null && _state.Commander.IsAlive)
            {
                var main = MobileParty.MainParty;
                var commanderParty = _state.Commander.PartyBelongedTo;

                // Validate commander is still in a party and active
                if (commanderParty == null || !commanderParty.IsActive)
                {
                    ArmyService.SafeDetach();
                    _state.ForceEndService();
                    PartyIllusionService.RestorePlayerPartyVisibility();
                    InformationManager.DisplayMessage(new InformationMessage(Constants.Messages.SERVICE_ENDED));
                    return;
                }

                // **CRITICAL: Original Mod's Party Control Approach**
                // This is the key insight from ServeAsSoldier mod - make main party untargetable
                if (main.CurrentSettlement == null) // Only when on campaign map
                {
                    // Set main party position to commander's position
                    main.Position2D = commanderParty.Position2D;
                    
                    // Make main party inactive - this prevents ALL encounter targeting
                    main.IsActive = false;
                    
                    // Ensure camera follows commander
                    commanderParty.Party.SetAsCameraFollowParty();
                }

                // Handle battle participation
                bool commanderInBattle = commanderParty.MapEvent != null;
                bool playerNotInBattle = main.MapEvent == null;
                bool notWaitingInReserve = !_state.WaitingInReserve; // Assuming you add this state

                if (commanderInBattle && playerNotInBattle && notWaitingInReserve)
                {
                    // Commander entered battle - join them
                    JoinCommandersBattle(commanderParty);
                }
                else if (!commanderInBattle && main.IsActive && main.CurrentSettlement == null)
                {
                    // Battle ended or no battle - ensure party is inactive again
                    main.IsActive = false;
                }

                // Maintain visual illusion
                PartyIllusionService.MaintainIllusion(_state.Commander);
                
                // Ensure escort behavior persists
                EnsureEscortBehavior();
            }
            else if (!_state.IsEnlisted)
            {
                // Not enlisted - ensure main party is active and visible
                var main = MobileParty.MainParty;
                if (!main.IsActive)
                {
                    main.IsActive = true;
                    main.Party.SetAsCameraFollowParty();
                }
            }
        }

        /// <summary>
        /// Joins the commander's battle by creating/joining army and activating main party.
        /// Based on the original ServeAsSoldier mod's battle participation logic.
        /// </summary>
        private void JoinCommandersBattle(MobileParty commanderParty)
        {
            var main = MobileParty.MainParty;
            
            try
            {
                // Exit any current menu context cleanly
                while (Campaign.Current.CurrentMenuContext != null)
                {
                    GameMenu.ExitToLast();
                }

                // Ensure commander has an army for the battle
                if (commanderParty.Army == null)
                {
                    // Create a temporary army for this battle
                    var kingdom = commanderParty.ActualClan?.Kingdom;
                    if (kingdom != null)
                    {
                        kingdom.CreateArmy(commanderParty.LeaderHero, commanderParty.HomeSettlement, Army.ArmyTypes.Patrolling);
                    }
                }

                // Join the commander's army for this battle
                if (commanderParty.Army != null)
                {
                    commanderParty.Army.AddPartyToMergedParties(main);
                    main.Army = commanderParty.Army;
                    main.IsActive = true;
                    main.Ai.SetMoveEngageParty(commanderParty);

                    InformationManager.DisplayMessage(new InformationMessage(
                        $"[Enlisted] Joining {_state.Commander.Name} in battle!"));
                }
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    $"[Enlisted] Error joining battle: {ex.Message}"));
            }
        }

        private void OnHourlyTick()
        {
            if (!_state.IsEnlisted) return;

            if (!_state.IsCommanderValid())
            {
                ArmyService.SafeDetach();
                _state.ForceEndService();
                PartyIllusionService.RestorePlayerPartyVisibility();
                InformationManager.DisplayMessage(new InformationMessage(Constants.Messages.SERVICE_ENDED));
            }
        }

        private void OnEnlist()
        {
            var commander = Hero.OneToOneConversationHero;
            _state.Enlist(commander);

            // Store original visibility state before hiding
            _state.PlayerPartyWasVisible = PartyIllusionService.GetOriginalVisibilityState();

            // Attempt to join army
            if (ArmyService.TryJoinCommandersArmy(commander))
            {
                // Create the party illusion
                PartyIllusionService.HidePlayerPartyAndFollowCommander(commander);
                InformationManager.DisplayMessage(new InformationMessage(string.Format(Constants.Messages.ENLISTED_SUCCESS, commander?.Name)));
                
                // Verify encounter protection is active
                DebugHelper.VerifyEncounterProtection(commander);
            }
        }

        private void OnLeave()
        {
            ArmyService.LeaveCurrentArmy();
            _state.Leave();
            InformationManager.DisplayMessage(new InformationMessage(Constants.Messages.LEFT_SERVICE));
        }

        /// <summary>
        /// Handles when any party enters a settlement.
        /// If it's our commander, we follow them in automatically.
        /// </summary>
        private void OnSettlementEntered(MobileParty party, Settlement settlement, Hero hero)
        {
            // Only process if we're enlisted and have a commander
            if (!_state.IsEnlisted || _state.Commander == null) return;

            // Check if our commander entered a settlement
            if (hero == _state.Commander && party == _state.Commander.PartyBelongedTo)
            {
                // Follow the commander into the settlement
                InformationManager.DisplayMessage(new InformationMessage(
                    string.Format(Constants.Messages.FOLLOWING_INTO_SETTLEMENT, 
                    _state.Commander.Name, settlement.Name)));

                // Enter the settlement if we're not already there
                if (MobileParty.MainParty.CurrentSettlement != settlement)
                {
                    // Don't force time control changes - let the game handle it naturally
                    EncounterManager.StartSettlementEncounter(MobileParty.MainParty, settlement);
                }
            }
        }

        /// <summary>
        /// Handles when any party leaves a settlement.
        /// If it's our commander, we follow them out seamlessly.
        /// </summary>
        private void OnSettlementLeft(MobileParty party, Settlement settlement)
        {
            // Only process if we're enlisted and have a commander
            if (!_state.IsEnlisted || _state.Commander == null) return;

            // Check if our commander left a settlement
            if (party == _state.Commander.PartyBelongedTo)
            {
                // Show a simple message
                InformationManager.DisplayMessage(new InformationMessage(
                    string.Format(Constants.Messages.FOLLOWING_COMMANDER)));

                // Handle different menu contexts properly
                var currentMenuContext = Campaign.Current.CurrentMenuContext;
                if (currentMenuContext != null)
                {
                    var gameMenu = currentMenuContext.GameMenu;
                    
                    // If we're in a wait menu, properly end the wait operation first
                    if (gameMenu != null && gameMenu.StringId != null && 
                        (gameMenu.StringId.Contains("wait") || gameMenu.StringId.Contains("settlement")))
                    {
                        // End any active wait operation before exiting
                        gameMenu.EndWait();
                    }
                    
                    // Use PlayerEncounter.Finish if we're in an encounter
                    if (PlayerEncounter.Current != null)
                    {
                        PlayerEncounter.Finish(true);
                    }
                    else
                    {
                        // Fallback to ExitToLast for non-encounter menus
                        GameMenu.ExitToLast();
                    }
                }

                // Ensure we properly leave the settlement and restore time control
                if (MobileParty.MainParty.CurrentSettlement != null)
                {
                    // Use LeaveSettlementAction for proper cleanup
                    LeaveSettlementAction.ApplyForParty(MobileParty.MainParty);
                }

                // Restore natural time control - don't force any specific mode
                // Let the game's natural flow handle time control
                if (Campaign.Current.TimeControlMode == CampaignTimeControlMode.Stop)
                {
                    Campaign.Current.TimeControlMode = Campaign.Current.LastTimeControlMode;
                }
            }
        }

        /// <summary>
        /// Handles battle start events to notify when commander enters combat.
        /// The actual battle joining is handled by the BattleParticipationPatch Harmony patch.
        /// </summary>
        private void OnMapEventStarted(MapEvent mapEvent, PartyBase attackerParty, PartyBase defenderParty)
        {
            // Only process if we're enlisted
            if (!_state.IsEnlisted || _state.Commander == null) return;

            var commanderParty = _state.Commander.PartyBelongedTo?.Party;
            if (commanderParty == null) return;

            // Check if our commander is involved in this battle
            bool commanderIsAttacker = (attackerParty == commanderParty);
            bool commanderIsDefender = (defenderParty == commanderParty);
            
            if (!commanderIsAttacker && !commanderIsDefender) return;

            // Just show a notification - the Harmony patch handles the actual joining
            string actionType = commanderIsAttacker ? "attacking" : "defending";
            InformationManager.DisplayMessage(new InformationMessage(
                $"[Enlisted] Commander {_state.Commander.Name} is {actionType} - prepare for battle!"));
        }

        /// <summary>
        /// Ensures the escort behavior is maintained throughout the service.
        /// This keeps the player following the commander without army integration issues.
        /// </summary>
        private void EnsureEscortBehavior()
        {
            var main = MobileParty.MainParty;
            var commanderParty = _state.Commander?.PartyBelongedTo;
            
            if (main == null || commanderParty == null) return;

            // Only maintain escort behavior when not in battle or settlement
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

