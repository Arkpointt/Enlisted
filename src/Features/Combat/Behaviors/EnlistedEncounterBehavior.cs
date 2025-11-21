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
using Enlisted.Features.Interface.Behaviors;
using Enlisted.Mod.Core.Logging;
using Enlisted.Mod.Entry;
using Enlisted.Mod.GameAdapters.Patches;

namespace Enlisted.Features.Combat.Behaviors
{
    /// <summary>
    /// Handles encounter menu extensions and battle participation for enlisted soldiers.
    /// Adds military-specific menu options during battles and sieges, such as
    /// "Wait in reserve" for large battles and siege participation options.
    /// </summary>
    public sealed class EnlistedEncounterBehavior : CampaignBehaviorBase
    {
        /// <summary>
        /// Helper method to check if a party is in battle or siege.
        /// This prevents PlayerSiege assertion failures by ensuring we don't finish encounters during sieges.
        /// </summary>
        private static bool InBattleOrSiege(MobileParty party) =>
            party?.Party.MapEvent != null || party?.Party.SiegeEvent != null || party?.BesiegedSettlement != null;

        public static EnlistedEncounterBehavior Instance { get; private set; }

        public EnlistedEncounterBehavior()
        {
            Instance = this;
            ModLogger.Info("Combat", "=== ENLISTED ENCOUNTER BEHAVIOR CONSTRUCTOR CALLED ===");
        }

        public override void RegisterEvents()
        {
            ModLogger.Info("Combat", "=== ENLISTED ENCOUNTER BEHAVIOR - REGISTERING EVENTS ===");
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            ModLogger.Debug("Combat", "OnSessionLaunched event listener registered");
        }

        public override void SyncData(IDataStore dataStore)
        {
            // No persistent state needed
        }

        private void OnSessionLaunched(CampaignGameStarter campaignStarter)
        {
            try
            {
                AddEnlistedEncounterOptions(campaignStarter);
                ModLogger.Info("Combat", "=== ENLISTED ENCOUNTER BEHAVIOR INITIALIZED ===");
                ModLogger.Info("Combat", "Menu options registered for: encounter, menu_siege_strategies");
                ModLogger.Debug("Combat", "Combat behavior ready for battle and siege participation");
            }
            catch (Exception ex)
            {
                ModLogger.Error("Combat", $"Failed to initialize encounter behavior: {ex.Message}");
            }
        }

        /// <summary>
        /// Registers military-specific menu options for battles and sieges.
        /// Adds options like "Wait in reserve" for large battles and siege participation options.
        /// These options appear in the encounter menu when the player is enlisted and their lord is in battle.
        /// </summary>
        private void AddEnlistedEncounterOptions(CampaignGameStarter starter)
        {
            // Add "Wait in reserve" option for large battles (100+ troops)
            // This allows players to stay out of the initial fighting in large battles
            var waitInReserveText = new TextObject("Wait in reserve");
            var waitInReserveTooltip = new TextObject("Stay back from the main fighting and wait for orders");
            
            // Add to the encounter menu for both field battles and siege encounters
            starter.AddGameMenuOption("encounter", "enlisted_wait_reserve", 
                waitInReserveText.ToString(),
                IsWaitInReserveAvailable,
                OnWaitInReserveSelected,
                false, 1);
                
            // Add siege participation options to the encounter menu
            // These allow players to join siege assaults or wait in siege reserve
            starter.AddGameMenuOption("encounter", "enlisted_join_siege_encounter", 
                "Join the siege assault",
                IsSiegeEncounterAvailable,
                OnJoinSiegeEncounterSelected,
                false, 2);
                
            starter.AddGameMenuOption("encounter", "enlisted_siege_wait_reserve", 
                "Wait in siege reserve",
                IsSiegeEncounterAvailable,
                OnSiegeWaitReserveSelected,
                false, 3);
                
            // Let the native system handle the army_wait menu
            // Adding custom options to army_wait was interfering with native battle flow
            ModLogger.Info("Combat", "Native army_wait menu will handle battle options automatically");
                
            // Add siege options to the siege strategies menu
            // This provides siege participation options during siege planning
            starter.AddGameMenuOption("menu_siege_strategies", "enlisted_join_siege_assault", 
                "Join the assault as enlisted soldier",
                IsSiegeJoinAvailable,
                OnJoinSiegeSelected,
                false, 1);
                
            starter.AddGameMenuOption("menu_siege_strategies", "enlisted_siege_reserve", 
                "Wait in siege reserve",
                IsEnlistedSiegeAvailable,
                OnWaitInReserveSelected,
                false, 2);

            // Add a custom "wait in reserve" menu for battles
            // This menu shows while the player is waiting in reserve and allows them to rejoin
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
        /// Checks if the "Wait in Reserve" option should be available in the encounter menu.
        /// This option is only available for enlisted soldiers in large battles (100+ troops),
        /// allowing players to stay out of the initial fighting when the army is large enough.
        /// </summary>
        /// <param name="args">Menu callback arguments containing menu state and context.</param>
        /// <returns>True if the option should be available, false otherwise.</returns>
        private bool IsWaitInReserveAvailable(MenuCallbackArgs args)
        {
            var enlistment = EnlistmentBehavior.Instance;
            if (!enlistment?.IsEnlisted == true)
            {
                return false;
            }

            var lord = enlistment.CurrentLord;
            var lordParty = lord?.PartyBelongedTo;
            
            // The MapEvent property exists on Party, not directly on MobileParty
            // This is the correct API structure for checking battle state
            if (lordParty?.Party.MapEvent == null)
            {
                return false;
            }

            // Only available in large battles with 100+ troops
            // Small battles don't have enough troops to support a reserve
            // The MapEvent property exists on Party, not directly on MobileParty
            var battle = lordParty.Party.MapEvent;
            
            // Determine which side the lord's party is fighting on
            // This is needed to check the troop count on their side
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
        /// Handles the player selecting "Wait in Reserve" from the encounter menu.
        /// Exits the current encounter and switches to the battle wait menu where
        /// the player can monitor the battle and rejoin when ready.
        /// </summary>
        /// <param name="args">Menu callback arguments containing menu state and context.</param>
        private void OnWaitInReserveSelected(MenuCallbackArgs args)
        {
            try
            {
                // Exit the current encounter and switch to the wait menu
                // This allows the player to wait out the battle without participating
                if (PlayerEncounter.Current != null)
                {
                    var enlistmentBehavior = EnlistmentBehavior.Instance;
                    var lordParty = enlistmentBehavior?.CurrentLord?.PartyBelongedTo;
                    
                    // Don't finish the encounter if the lord is in battle or siege
                    // This prevents assertion failures that can occur during battle state transitions
                    if (!InBattleOrSiege(lordParty))
                    {
                        PlayerEncounter.Finish(true);
                    }
                    else
                    {
                        ModLogger.Debug("Combat", "Skipped finishing encounter - lord in battle/siege, preserving vanilla battle menu");
                    }
                }

                // Switch to the battle wait menu where the player can monitor the battle
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
            // CORRECT API: Use Party.MapEvent (not direct on MobileParty)
            if (lordParty?.Party.MapEvent != null)
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
        /// Tick handler for the battle wait menu that runs while the player is waiting in reserve.
        /// Monitors battle state, checks if the native system wants to show a different menu,
        /// and automatically returns the player to battle if troop count drops below 100.
        /// Includes time delta validation to prevent assertion failures.
        /// </summary>
        /// <param name="args">Menu callback arguments containing menu state and context.</param>
        /// <param name="dt">Time elapsed since last tick, in campaign time. Must be positive.</param>
        private void OnBattleWaitTick(MenuCallbackArgs args, CampaignTime dt)
        {
            try
            {
                // Validate time delta to prevent assertion failures
                // Zero-delta-time updates can cause assertion failures in the rendering system
                if (dt.ToSeconds <= 0)
                {
                    return;
                }
                
                // Check what menu the native game system wants to show based on current state
                // The native system may want to show army_wait, menu_siege_strategies, or other menus
                // We should respect this and switch to the native menu to avoid blocking battle flow
                string genericStateMenu = Campaign.Current.Models.EncounterGameMenuModel.GetGenericStateMenu();
                
                // If the native system wants a different menu, switch to it immediately
                // This prevents our custom battle wait menu from blocking native battle menus
                if (!string.IsNullOrEmpty(genericStateMenu) && genericStateMenu != "enlisted_battle_wait")
                {
                    args.MenuContext.GameMenu.EndWait();
                    ModLogger.Info("Battle", $"Native system wants menu '{genericStateMenu}' - switching from enlisted_battle_wait");
                    GameMenu.SwitchToMenu(genericStateMenu);
                    return;
                }
                
                // Check if the battle has ended
                // If so, let the native system decide what menu to show next
                // Don't force a return to the enlisted menu - let GetGenericStateMenu() determine it
                var lord = EnlistmentBehavior.Instance?.CurrentLord;
                var lordParty = lord?.PartyBelongedTo;

                // The MapEvent property exists on Party, not directly on MobileParty
                // This is the correct API structure for checking battle state
                if (lordParty?.Party.MapEvent == null && string.IsNullOrEmpty(genericStateMenu))
                {
                    // Battle ended and native system doesn't want a specific menu
                    // Exit and let the menu tick handler or native system decide the next menu
                    args.MenuContext.GameMenu.EndWait();
                    GameMenu.ExitToLast();
                    ModLogger.Info("Battle", "Battle ended - exiting battle wait menu, native system will show appropriate menu");
                }
                else
                {
                    // Check troop count to see if we should auto-rejoin the battle
                    // When troop count drops below 100, there aren't enough troops for a reserve
                    // and the player should automatically rejoin to help
                    var battle = lordParty.MapEvent;
                    
                    // Determine which side the lord's party is fighting on
                    // This is needed to check the troop count on their side
                    bool isOnAttackerSide = ContainsParty(battle.PartiesOnSide(BattleSideEnum.Attacker), lordParty);
                    var lordSide = isOnAttackerSide ? battle.AttackerSide : battle.DefenderSide;
                    var troopCount = lordSide?.TroopCount ?? 0;

                    if (troopCount < 100)
                    {
                        // Not enough troops left for a reserve - automatically rejoin the battle
                        // Defer the menu transition to the next frame to avoid timing conflicts
                        NextFrameDispatcher.RunNextFrame(() =>
                        {
                            if (Campaign.Current.CurrentMenuContext != null)
                            {
                                GameMenu.ExitToLast();
                            }
                        });
                        
                        // Return to the enlisted status menu after exiting the wait menu
                        EnlistedMenuBehavior.SafeActivateEnlistedMenu();
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
                // Exit wait menu and return to encounter (deferred to next frame)
                NextFrameDispatcher.RunNextFrame(() =>
                {
                    if (Campaign.Current.CurrentMenuContext != null)
                    {
                        GameMenu.ExitToLast();
                    }
                });

                // This should return player to the encounter menu
                ModLogger.Info("Battle", "Player rejoining battle from reserve");
            }
            catch (Exception ex)
            {
                ModLogger.Error("Battle", $"Error rejoining battle: {ex.Message}");
            }
        }

        /// <summary>
        /// Checks if siege encounter options should be available in the encounter menu.
        /// These options appear when the player is enlisted and their lord is involved in a siege.
        /// </summary>
        /// <param name="args">Menu callback arguments containing menu state and context.</param>
        /// <returns>True if siege options should be available, false otherwise.</returns>
        private bool IsSiegeEncounterAvailable(MenuCallbackArgs args)
        {
            var enlistment = EnlistmentBehavior.Instance;
            if (!enlistment?.IsEnlisted == true)
            {
                return false;
            }

            var lord = enlistment.CurrentLord;
            var lordParty = lord?.PartyBelongedTo;
            
            // Enhanced siege detection - check multiple siege conditions
            // CORRECT API: Use Party.MapEvent and Party.SiegeEvent (not direct on MobileParty)
            bool lordInSiege = lordParty?.Party.SiegeEvent != null || lordParty?.BesiegedSettlement != null;
            bool lordInSiegeBattle = lordParty?.Party.MapEvent != null && IsSiegeRelatedBattleForEncounter(MobileParty.MainParty, lordParty);
            
            // Menu condition - no logging to prevent spam
            
            return lordInSiege || lordInSiegeBattle;
        }
        
        /// <summary>
        /// Detects siege-related battles for encounter behavior.
        /// </summary>
        private static bool IsSiegeRelatedBattleForEncounter(MobileParty main, MobileParty lord)
        {
            try
            {
                // Check if current battle has siege-related types
                var mapEvent = main?.MapEvent ?? lord?.MapEvent;
                if (mapEvent != null)
                {
                    // Check for siege battle types: SiegeOutside, SiegeAssault, etc.
                    string battleType = mapEvent.EventType.ToString();
                    string mapEventString = mapEvent.ToString() ?? "";
                    
                    bool isSiegeType = battleType.Contains("Siege") || 
                                      mapEventString.Contains("Siege") ||
                                      mapEventString.Contains("SiegeOutside");
                        
                    if (isSiegeType)
                    {
                        ModLogger.Info("Combat", $"SIEGE BATTLE DETECTED: Type='{battleType}', Event='{mapEventString}'");
                        return true;
                    }
                }
                
                return false;
            }
            catch (Exception ex)
            {
                ModLogger.Error("Combat", $"Error in siege battle detection: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Handles the player selecting "Join the siege assault" from the encounter menu.
        /// Verifies that the player is in the lord's army and lets the native encounter system
        /// handle the actual siege participation. The player should already be in the army
        /// if they've been following the lord during the siege.
        /// </summary>
        /// <param name="args">Menu callback arguments containing menu state and context.</param>
        private void OnJoinSiegeEncounterSelected(MenuCallbackArgs args)
        {
            try
            {
                ModLogger.Info("Battle", "Player joining siege through encounter menu");
                
                // Verify the player is in the lord's army for siege participation
                // If they're already in the army, the native encounter system will handle participation
                var enlistment = EnlistmentBehavior.Instance;
                var lord = enlistment?.CurrentLord;
                var lordParty = lord?.PartyBelongedTo;
                var mainParty = MobileParty.MainParty;
                
                if (lordParty?.Army != null && mainParty?.Army == lordParty.Army)
                {
                    ModLogger.Info("Battle", "Player already in siege army - participation should work automatically");
                    // Let the native encounter system handle the rest
                }
                else
                {
                    ModLogger.Error("Battle", "Player not properly in siege army for participation");
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("Battle", $"Error in siege encounter participation: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Handles the player selecting "Wait in siege reserve" from the encounter menu.
        /// Exits the encounter and switches to the battle wait menu where the player can
        /// monitor the siege and rejoin when ready.
        /// </summary>
        /// <param name="args">Menu callback arguments containing menu state and context.</param>
        private void OnSiegeWaitReserveSelected(MenuCallbackArgs args)
        {
            try
            {
                ModLogger.Info("Battle", "Player waiting in siege reserve");
                
                // Exit the current encounter and switch to the wait menu
                var enlistmentBehavior = EnlistmentBehavior.Instance;
                var lordParty = enlistmentBehavior?.CurrentLord?.PartyBelongedTo;
                
                // Don't finish the encounter if the lord is in battle or siege
                // This prevents assertion failures during battle state transitions
                if (!InBattleOrSiege(lordParty))
                {
                    PlayerEncounter.Finish(true);
                }
                else
                {
                    ModLogger.Debug("Combat", "Skipped finishing encounter - lord in battle/siege, preserving vanilla battle menu");
                }
                
                // Switch to the battle wait menu where the player can monitor the siege
                GameMenu.ActivateGameMenu("enlisted_battle_wait");
                
                ModLogger.Info("Battle", "Switched to siege reserve wait menu");
            }
            catch (Exception ex)
            {
                ModLogger.Error("Battle", $"Error entering siege reserve: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Check if siege join option should be available.
        /// </summary>
        private bool IsSiegeJoinAvailable(MenuCallbackArgs args)
        {
            var enlistment = EnlistmentBehavior.Instance;
            // Menu condition - no logging to prevent spam
            
            if (!enlistment?.IsEnlisted == true)
            {
                ModLogger.Debug("Combat", "Siege option hidden - not enlisted");
                return false;
            }

            var lord = enlistment.CurrentLord;
            var lordParty = lord?.PartyBelongedTo;
            
            // Only show for siege battles where we can assault
            bool result = lordParty?.BesiegedSettlement != null || 
                         (lordParty?.MapEvent?.IsSiegeAssault == true);
                         
            ModLogger.Info("Combat", $"SIEGE OPTION RESULT: {result} (BesiegedSettlement={lordParty?.BesiegedSettlement?.Name}, IsSiegeAssault={lordParty?.MapEvent?.IsSiegeAssault})");
            return result;
        }
        
        /// <summary>
        /// Check if enlisted army battle options should be available.
        /// </summary>
        private bool IsEnlistedArmyBattleAvailable(MenuCallbackArgs args)
        {
            var enlistmentBehavior = EnlistmentBehavior.Instance;
            if (enlistmentBehavior?.IsEnlisted != true)
                return false;
                
            var lord = enlistmentBehavior.CurrentLord;
            var lordParty = lord?.PartyBelongedTo;
            
            // Available if lord is in an army and has a MapEvent (battle)
            return lordParty?.Army != null && lordParty.Party.MapEvent != null;
        }
        
        /// <summary>
        /// Handles the player selecting to join an army battle.
        /// Ensures the player is properly configured for battle participation by verifying
        /// army membership and activating the party, then lets the native system handle
        /// the actual battle start without interfering with menu state.
        /// </summary>
        /// <param name="args">Menu callback arguments containing menu state and context.</param>
        private void OnJoinArmyBattleSelected(MenuCallbackArgs args)
        {
            ModLogger.Info("Combat", "Player selected to join army battle");
            
            try
            {
                var enlistmentBehavior = EnlistmentBehavior.Instance;
                var lord = enlistmentBehavior?.CurrentLord;
                var lordParty = lord?.PartyBelongedTo;
                var mainParty = MobileParty.MainParty;
                
                if (lordParty?.Party.MapEvent == null)
                {
                    ModLogger.Error("Combat", "Cannot join battle - lord not in battle");
                    return;
                }
                
                ModLogger.Info("Combat", $"Joining army battle: {lordParty.Party.MapEvent.EventType}");
                
                // Don't clear menus or interfere with the native system
                // The native system needs to handle the battle start sequence itself
                // Interfering with menus can prevent battles from starting properly
                ModLogger.Info("Combat", "Letting native system handle battle start");
                
                // Ensure the player is properly configured for battle participation
                // Add them to the lord's army if they're not already in it
                if (mainParty.Army != lordParty.Army)
                {
                    ModLogger.Info("Combat", "Adding player to lord's army for battle");
                    lordParty.Army.AddPartyToMergedParties(mainParty);
                    mainParty.Army = lordParty.Army;
                }
                
                // Activate the player's party so they can participate in the battle
                mainParty.IsActive = true;
                mainParty.IsVisible = true;
                mainParty.IgnoreByOtherPartiesTill(CampaignTime.Now);
                
                ModLogger.Info("Combat", "Player configured for battle - native system should handle battle start");
                ModLogger.Info("Combat", $"Player party state: IsActive={mainParty.IsActive}, IsVisible={mainParty.IsVisible}, Army={mainParty.Army?.LeaderParty?.LeaderHero?.Name}");
            }
            catch (Exception ex)
            {
                ModLogger.Error("Combat", $"Error joining army battle: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Handle waiting in army reserve.
        /// </summary>
        private void OnArmyReserveSelected(MenuCallbackArgs args)
        {
            ModLogger.Info("Combat", "Player selected to wait in army reserve");
            
            // Exit current menu and activate reserve menu
            GameMenu.ExitToLast();
            GameMenu.ActivateGameMenu("enlisted_battle_wait");
        }
        
        
        /// <summary>
        /// Check if enlisted siege options should be available.
        /// </summary>
        private bool IsEnlistedSiegeAvailable(MenuCallbackArgs args)
        {
            var enlistment = EnlistmentBehavior.Instance;
            var result = enlistment?.IsEnlisted == true;
            // Menu condition - no logging to prevent spam
            return result;
        }
        
        /// <summary>
        /// Handle siege participation selection.
        /// </summary>
        private void OnJoinSiegeSelected(MenuCallbackArgs args)
        {
            try
            {
                ModLogger.Info("Battle", "Player chose to join siege assault");
                
                // This should trigger the actual siege battle mission
                // The army membership should automatically include the player
            }
            catch (Exception ex)
            {
                ModLogger.Error("Battle", $"Error joining siege: {ex.Message}");
            }
        }

        /// <summary>
        /// Helper method that checks if a specific party is included in a list of battle parties.
        /// Used to determine which side of a battle a party is fighting on by checking
        /// if the party appears in the attacker or defender side's party list.
        /// </summary>
        /// <param name="parties">The list of battle parties to check against.</param>
        /// <param name="party">The party to search for in the list.</param>
        /// <returns>True if the party is found in the list, false otherwise.</returns>
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

