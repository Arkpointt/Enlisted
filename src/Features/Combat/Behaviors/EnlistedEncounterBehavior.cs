using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Overlay;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.Library;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Mod.Core.Logging;

namespace Enlisted.Features.Combat.Behaviors
{
    /// <summary>
    /// Handles encounter menu extensions for enlisted soldiers during battles.
    /// Adds military options like "Wait in reserve" similar to SAS mod.
    /// </summary>
    public sealed class EnlistedEncounterBehavior : CampaignBehaviorBase
    {
        public static EnlistedEncounterBehavior Instance { get; private set; }

        public EnlistedEncounterBehavior()
        {
            Instance = this;
        }

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        }

        public override void SyncData(IDataStore dataStore)
        {
            // No persistent state needed
        }

        private void OnSessionLaunched(CampaignGameStarter campaignStarter)
        {
            AddEnlistedEncounterOptions(campaignStarter);
            ModLogger.Info("Combat", "Enlisted encounter menu options initialized");
        }

        /// <summary>
        /// Add military encounter options following SAS pattern.
        /// </summary>
        private void AddEnlistedEncounterOptions(CampaignGameStarter starter)
        {
            // SAS PATTERN: "Wait in reserve" option for large battles
            var waitInReserveText = new TextObject("Wait in reserve");
            var waitInReserveTooltip = new TextObject("Stay back from the main fighting and wait for orders");
            
            starter.AddGameMenuOption("encounter", "enlisted_wait_reserve", 
                waitInReserveText.ToString(),
                IsWaitInReserveAvailable,
                OnWaitInReserveSelected,
                false, 1);

            // Add battle wait menu (SAS pattern)
            starter.AddWaitGameMenu("enlisted_battle_wait", 
                "Waiting in Reserve: {BATTLE_STATUS}",
                OnBattleWaitInit,
                OnBattleWaitCondition,
                null,
                OnBattleWaitTick,
                GameMenu.MenuAndOptionType.WaitMenuHideProgressAndHoursOption,
                GameOverlays.MenuOverlayType.None,
                0f,
                GameMenu.MenuFlags.None,
                null);

            // Return to battle option
            var rejoinBattleText = new TextObject("Rejoin the battle");
            starter.AddGameMenuOption("enlisted_battle_wait", "enlisted_rejoin_battle",
                rejoinBattleText.ToString(),
                args => true,
                OnRejoinBattleSelected,
                false, 1);
        }

        /// <summary>
        /// Check if Wait in Reserve option should be available.
        /// SAS PATTERN: Only for enlisted soldiers in large battles.
        /// </summary>
        private bool IsWaitInReserveAvailable(MenuCallbackArgs args)
        {
            var enlistment = EnlistmentBehavior.Instance;
            if (!enlistment?.IsEnlisted == true)
            {
                return false;
            }

            var lord = enlistment.CurrentLord;
            var lordParty = lord?.PartyBelongedTo;
            
            if (lordParty?.MapEvent == null)
            {
                return false;
            }

            // SAS PATTERN: Only available in large battles (>= 100 troops)
            var battle = lordParty.MapEvent;
            
            // SAS API PATTERN: Check which side the lord's party is on
            bool isOnAttackerSide = ContainsParty(battle.PartiesOnSide(BattleSideEnum.Attacker), lordParty);
            var lordSide = isOnAttackerSide ? battle.AttackerSide : battle.DefenderSide;
            var troopCount = lordSide?.TroopCount ?? 0;

            if (troopCount < 100)
            {
                args.IsEnabled = false;
                args.Tooltip = new TextObject("You can't wait in reserve if there are less than 100 healthy troops in the army");
                return false;
            }

            args.optionLeaveType = GameMenuOption.LeaveType.Wait;
            return true;
        }

        /// <summary>
        /// Handle Wait in Reserve selection.
        /// </summary>
        private void OnWaitInReserveSelected(MenuCallbackArgs args)
        {
            try
            {
                // SAS PATTERN: Exit encounter and switch to wait menu
                if (PlayerEncounter.Current != null)
                {
                    PlayerEncounter.Finish(true);
                }

                GameMenu.ActivateGameMenu("enlisted_battle_wait");
                ModLogger.Info("Battle", "Player waiting in reserve");
            }
            catch (Exception ex)
            {
                ModLogger.Error("Battle", $"Error entering reserve mode: {ex.Message}");
            }
        }

        /// <summary>
        /// Initialize battle wait menu.
        /// </summary>
        private void OnBattleWaitInit(MenuCallbackArgs args)
        {
            try
            {
                args.MenuContext.GameMenu.StartWait();
                Campaign.Current.TimeControlMode = CampaignTimeControlMode.StoppablePlay;
                
                // Set background from lord's culture
                var lord = EnlistmentBehavior.Instance?.CurrentLord;
                if (lord?.MapFaction?.Culture?.EncounterBackgroundMesh != null)
                {
                    args.MenuContext.SetBackgroundMeshName(lord.MapFaction.Culture.EncounterBackgroundMesh);
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("Battle", $"Error initializing battle wait menu: {ex.Message}");
            }
        }

        /// <summary>
        /// Condition for battle wait menu.
        /// </summary>
        private bool OnBattleWaitCondition(MenuCallbackArgs args)
        {
            var enlistment = EnlistmentBehavior.Instance;
            var lord = enlistment?.CurrentLord;
            var lordParty = lord?.PartyBelongedTo;

            // Update battle status text
            if (lordParty?.MapEvent != null)
            {
                var battleStatus = "Your lord is engaged in battle";
                MBTextManager.SetTextVariable("BATTLE_STATUS", battleStatus);
            }
            else
            {
                MBTextManager.SetTextVariable("BATTLE_STATUS", "Battle has ended");
            }

            return enlistment?.IsEnlisted == true;
        }

        /// <summary>
        /// Tick handler for battle wait menu.
        /// </summary>
        private void OnBattleWaitTick(MenuCallbackArgs args, CampaignTime dt)
        {
            try
            {
                var lord = EnlistmentBehavior.Instance?.CurrentLord;
                var lordParty = lord?.PartyBelongedTo;

                // SAS PATTERN: Check if battle ended, auto-return
                if (lordParty?.MapEvent == null)
                {
                    // Battle ended, return to normal enlisted state
                    while (Campaign.Current.CurrentMenuContext != null)
                    {
                        GameMenu.ExitToLast();
                    }
                    
                    // Return to enlisted menu
                    GameMenu.ActivateGameMenu("enlisted_status");
                    ModLogger.Info("Battle", "Battle ended, returning to enlisted status");
                }
                else
                {
                    // Check troop count for auto-return (SAS pattern)
                    var battle = lordParty.MapEvent;
                    
                    // SAS API PATTERN: Check which side the lord's party is on
                    bool isOnAttackerSide = ContainsParty(battle.PartiesOnSide(BattleSideEnum.Attacker), lordParty);
                    var lordSide = isOnAttackerSide ? battle.AttackerSide : battle.DefenderSide;
                    var troopCount = lordSide?.TroopCount ?? 0;

                    if (troopCount < 100)
                    {
                        // Not enough troops left, auto-rejoin
                        while (Campaign.Current.CurrentMenuContext != null)
                        {
                            GameMenu.ExitToLast();
                        }
                        
                        GameMenu.ActivateGameMenu("enlisted_status");
                        ModLogger.Info("Battle", "Low troop count, automatically rejoining battle");
                    }
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("Battle", $"Error in battle wait tick: {ex.Message}");
            }
        }

        /// <summary>
        /// Handle rejoin battle selection.
        /// </summary>
        private void OnRejoinBattleSelected(MenuCallbackArgs args)
        {
            try
            {
                // Exit wait menu and return to encounter
                while (Campaign.Current.CurrentMenuContext != null)
                {
                    GameMenu.ExitToLast();
                }

                // This should return player to the encounter menu
                ModLogger.Info("Battle", "Player rejoining battle from reserve");
            }
            catch (Exception ex)
            {
                ModLogger.Error("Battle", $"Error rejoining battle: {ex.Message}");
            }
        }

        /// <summary>
        /// SAS HELPER: Check if a party is in a list of battle parties.
        /// </summary>
        private bool ContainsParty(IReadOnlyList<MapEventParty> parties, MobileParty party)
        {
            foreach (var p in parties)
            {
                if (p.Party.MobileParty == party)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
