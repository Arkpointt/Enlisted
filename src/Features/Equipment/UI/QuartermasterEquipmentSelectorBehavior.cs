using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.ScreenSystem;
using Enlisted.Features.Conversations.Behaviors;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Features.Equipment.Behaviors;
using Enlisted.Mod.Core.Logging;

namespace Enlisted.Features.Equipment.UI
{
    /// <summary>
    /// Gauntlet equipment selector behavior providing individual clickable equipment selection.
    /// 
    /// Creates a custom UI overlay showing equipment variants as clickable buttons.
    /// Uses TaleWorlds Gauntlet UI system for custom overlay creation and management.
    /// After closing, the player returns to the quartermaster conversation hub for more shopping.
    /// Registers for campaign events to force-close on battle/capture interruptions.
    /// </summary>
    public class QuartermasterEquipmentSelectorBehavior : CampaignBehaviorBase
    {
        public static QuartermasterEquipmentSelectorBehavior Instance { get; private set; }
        
        // Gauntlet UI components for custom overlay display
        private static GauntletLayer _gauntletLayer;
        // 1.3.4 API: LoadMovie now returns GauntletMovieIdentifier instead of IGauntletMovie
        private static GauntletMovieIdentifier _gauntletMovie;
        private static QuartermasterEquipmentSelectorVm _selectorViewModel;
        
        // Track whether to return to conversation after closing
        private static bool _returnToConversationOnClose = true;
        
        /// <summary>
        /// Returns true if the equipment selector UI is currently open.
        /// </summary>
        public static bool IsOpen => _gauntletLayer != null;
        
        public QuartermasterEquipmentSelectorBehavior()
        {
            Instance = this;
        }
        
        public override void RegisterEvents()
        {
            // Force-close Gauntlet on events that interrupt the QM interaction
            CampaignEvents.OnPlayerBattleEndEvent.AddNonSerializedListener(this, OnBattleEnd);
            CampaignEvents.OnSettlementLeftEvent.AddNonSerializedListener(this, OnSettlementLeft);
            CampaignEvents.HeroPrisonerTaken.AddNonSerializedListener(this, OnHeroPrisonerTaken);
            CampaignEvents.MapEventEnded.AddNonSerializedListener(this, OnMapEventEnded);
        }
        
        public override void SyncData(IDataStore dataStore)
        {
            // No persistent data for UI behavior
            // Force-close on save/load to prevent stale UI
            if (dataStore.IsLoading)
            {
                if (IsOpen)
                {
                    ForceCloseOnInterruption("save/load");
                }
                if (IsUpgradeScreenOpen)
                {
                    CloseUpgradeScreen(false);
                    ModLogger.Info("QuartermasterUI", "Force-closed upgrade screen due to save load");
                }
            }
        }
        
        private void OnBattleEnd(MapEvent mapEvent)
        {
            if (IsOpen)
            {
                ForceCloseOnInterruption("battle end");
            }
            if (IsUpgradeScreenOpen)
            {
                CloseUpgradeScreen(false);
                ModLogger.Info("QuartermasterUI", "Force-closed upgrade screen due to battle end");
            }
        }
        
        private void OnSettlementLeft(MobileParty party, Settlement settlement)
        {
            if (party == MobileParty.MainParty)
            {
                if (IsOpen)
                {
                    ForceCloseOnInterruption("settlement left");
                }
                if (IsUpgradeScreenOpen)
                {
                    CloseUpgradeScreen(false);
                    ModLogger.Info("QuartermasterUI", "Force-closed upgrade screen due to settlement departure");
                }
            }
        }
        
        private void OnHeroPrisonerTaken(PartyBase capturer, Hero prisoner)
        {
            if (prisoner == Hero.MainHero)
            {
                if (IsOpen)
                {
                    ForceCloseOnInterruption("player captured");
                }
                if (IsUpgradeScreenOpen)
                {
                    CloseUpgradeScreen(false);
                    ModLogger.Info("QuartermasterUI", "Force-closed upgrade screen due to player capture");
                }
            }
        }
        
        private void OnMapEventEnded(MapEvent mapEvent)
        {
            // Only close UI for actual combat events, not conversations.
            // Conversations also trigger MapEventEnded, but we want the upgrade/selector screens
            // to remain open after the conversation ends (opened via "open_upgrade" action).
            bool isCombatEvent = mapEvent?.EventType != MapEvent.BattleTypes.None;
            
            if (isCombatEvent && IsOpen)
            {
                ForceCloseOnInterruption("map event ended");
            }
            
            if (isCombatEvent && IsUpgradeScreenOpen)
            {
                CloseUpgradeScreen(false);
                ModLogger.Info("QuartermasterUI", "Force-closed upgrade screen due to map event end");
            }
        }
        
        /// <summary>
        /// Force-close the selector due to external interruption. Does not return to conversation.
        /// </summary>
        private static void ForceCloseOnInterruption(string reason)
        {
            ModLogger.Info("QuartermasterUI", $"Force-closing equipment selector due to: {reason}");
            CloseEquipmentSelector(false);
        }
        
        /// <summary>
        /// Show equipment selector with proper grid UI using OFFICIAL module structure.
        /// Template now located in GUI/Prefabs/Equipment/ following Bannerlord standards.
        /// </summary>
        public static void ShowEquipmentSelector(List<EquipmentVariantOption> availableVariants, EquipmentIndex targetSlot, string equipmentType)
        {
            try
            {
                // Prevent double-open - close existing first without returning to conversation
                if (IsOpen)
                {
                    ModLogger.Debug("QuartermasterUI", "Closing existing selector before opening new one");
                    CloseEquipmentSelector(false);
                }
                
                if (availableVariants == null || availableVariants.Count <= 1)
                {
                    ModLogger.Info("QuartermasterUI", "No equipment variants available for grid UI - using conversation fallback");
                    ShowConversationFallback(availableVariants, equipmentType);
                    return;
                }
                
                // Create Gauntlet layer for custom UI overlay
                // 1.3.4 API: GauntletLayer constructor with name and localOrder (omit shouldClear as it defaults to false)
                _gauntletLayer = new GauntletLayer("QuartermasterEquipmentGrid", 1001);
                
                // Create ViewModel with equipment variant collection
                _selectorViewModel = new QuartermasterEquipmentSelectorVm(availableVariants, targetSlot, equipmentType);
                _selectorViewModel.RefreshValues();
                
                // FIXED: Load template from official module structure GUI/Prefabs/Equipment/
                _gauntletMovie = _gauntletLayer.LoadMovie("QuartermasterEquipmentGrid", _selectorViewModel);
                
                // Register hotkeys and set input restrictions for UI interaction
                _gauntletLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
                // Omit default parameter values for cleaner code
                _gauntletLayer.InputRestrictions.SetInputRestrictions();
                ScreenManager.TopScreen.AddLayer(_gauntletLayer);
                _gauntletLayer.IsFocusLayer = true;
                ScreenManager.TrySetFocus(_gauntletLayer);
                
                ModLogger.Info("QuartermasterUI", $"Grid UI opened successfully with {availableVariants.Count} variants for {equipmentType}");
            }
            catch (Exception ex)
            {
                ModLogger.Error("QuartermasterUI", "Grid UI failed, using conversation fallback", ex);
                CloseEquipmentSelector();
                ShowConversationFallback(availableVariants, equipmentType);
            }
        }
        
        /// <summary>
        /// Fallback to conversation-based selection when grid UI is unavailable.
        /// </summary>
        private static void ShowConversationFallback(List<EquipmentVariantOption> availableVariants, string equipmentType)
        {
            try
            {
                var quartermasterManager = QuartermasterManager.Instance;
                if (quartermasterManager != null)
                {
                    var method = typeof(QuartermasterManager).GetMethod("ShowConversationBasedEquipmentSelection", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    method?.Invoke(quartermasterManager, [availableVariants, equipmentType]);
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("QuartermasterUI", "Both grid and conversation UI failed", ex);
            }
        }
        
        /// <summary>
        /// Close equipment selector and clean up UI resources.
        /// By default, returns to the quartermaster conversation hub for continued shopping.
        /// </summary>
        /// <param name="returnToConversation">If true, restarts the QM conversation after closing.</param>
        public static void CloseEquipmentSelector(bool returnToConversation = true)
        {
            try
            {
                if (_gauntletLayer != null)
                {
                    // Reset input restrictions and remove focus
                    _gauntletLayer.InputRestrictions.ResetInputRestrictions();
                    _gauntletLayer.IsFocusLayer = false;
                    
                    if (_gauntletMovie != null)
                    {
                        _gauntletLayer.ReleaseMovie(_gauntletMovie);
                    }
                    
                    var topScreen = ScreenManager.TopScreen;
                    if (topScreen != null)
                    {
                        topScreen.RemoveLayer(_gauntletLayer);
                    }
                    
                    ModLogger.Info("QuartermasterUI", "Equipment selector closed");
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("QuartermasterUI", "Error closing equipment selector", ex);
            }
            finally
            {
                // Always cleanup references
                _gauntletLayer = null;
                _gauntletMovie = null;
                _selectorViewModel = null;
            }
            
            // Return to quartermaster conversation after closing (only if still enlisted with valid QM)
            if (returnToConversation && _returnToConversationOnClose)
            {
                var enlistment = EnlistmentBehavior.Instance;
                var qmHero = enlistment?.QuartermasterHero;
                
                if (enlistment?.IsEnlisted != true)
                {
                    ModLogger.Debug("QuartermasterUI", "Not returning to conversation - player no longer enlisted");
                }
                else if (qmHero == null || !qmHero.IsAlive)
                {
                    ModLogger.Debug("QuartermasterUI", "Not returning to conversation - QM hero unavailable");
                }
                else
                {
                    EnlistedDialogManager.RestartQuartermasterConversation();
                }
            }
        }
        
        /// <summary>
        /// Close the selector without returning to conversation.
        /// Used when the player explicitly exits via the "Done" button or external interruption.
        /// </summary>
        public static void CloseWithoutConversation()
        {
            _returnToConversationOnClose = false;
            CloseEquipmentSelector(false);
            _returnToConversationOnClose = true;
        }
        
        #region Upgrade Screen Support (Phase 3)
        
        // Upgrade screen UI components
        private static GauntletLayer _upgradeLayer;
        private static GauntletMovieIdentifier _upgradeMovie;
        private static QuartermasterUpgradeVm _upgradeViewModel;
        
        /// <summary>
        /// Check if upgrade screen is currently open.
        /// </summary>
        public static bool IsUpgradeScreenOpen => _upgradeLayer != null;
        
        /// <summary>
        /// Show upgrade screen with player's equipped items.
        /// </summary>
        public static void ShowUpgradeScreen()
        {
            try
            {
                ModLogger.Info("QuartermasterUI", "ShowUpgradeScreen called");
                
                // Prevent double-open
                if (IsUpgradeScreenOpen)
                {
                    ModLogger.Debug("QuartermasterUI", "Closing existing upgrade screen before opening new one");
                    CloseUpgradeScreen(false);
                }
                
                ModLogger.Debug("QuartermasterUI", "Creating upgrade ViewModel");
                
                // Create ViewModel
                _upgradeViewModel = new QuartermasterUpgradeVm();
                _upgradeViewModel.RefreshValues();
                
                ModLogger.Debug("QuartermasterUI", $"ViewModel created, HasUpgradeableItems={_upgradeViewModel.HasUpgradeableItems}");
                
                // Create Gauntlet layer for upgrade screen overlay
                _upgradeLayer = new GauntletLayer("QuartermasterUpgradeScreen", 4000);
                ModLogger.Debug("QuartermasterUI", "Gauntlet layer created");
                
                // Load upgrade screen movie from GUI/Prefabs/Equipment/
                ModLogger.Debug("QuartermasterUI", "Loading movie: QuartermasterUpgradeScreen");
                _upgradeMovie = _upgradeLayer.LoadMovie("QuartermasterUpgradeScreen", _upgradeViewModel);
                ModLogger.Debug("QuartermasterUI", "Movie loaded successfully");
                
                // Apply input restrictions and add layer to screen
                _upgradeLayer.InputRestrictions.SetInputRestrictions();
                ScreenManager.TopScreen.AddLayer(_upgradeLayer);
                _upgradeLayer.IsFocusLayer = true;
                ScreenManager.TrySetFocus(_upgradeLayer);
                
                ModLogger.Debug("QuartermasterUI", "Layer added to screen and focused");
                
                // Handle ESC key to close upgrade screen
                _upgradeLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericCampaignPanelsGameKeyCategory"));
                
                ModLogger.Info("QuartermasterUI", "Upgrade screen opened successfully");
            }
            catch (Exception ex)
            {
                ModLogger.Error("QuartermasterUI", "Failed to open upgrade screen", ex);
                CloseUpgradeScreen();
            }
        }
        
        /// <summary>
        /// Close upgrade screen and clean up UI resources.
        /// By default, returns to the quartermaster conversation hub.
        /// </summary>
        /// <param name="returnToConversation">If true, restarts the QM conversation after closing.</param>
        public static void CloseUpgradeScreen(bool returnToConversation = true)
        {
            try
            {
                if (_upgradeLayer != null)
                {
                    // Reset input restrictions and remove focus
                    _upgradeLayer.InputRestrictions.ResetInputRestrictions();
                    _upgradeLayer.IsFocusLayer = false;
                    
                    if (_upgradeMovie != null)
                    {
                        _upgradeLayer.ReleaseMovie(_upgradeMovie);
                        _upgradeMovie = null;
                    }
                    
                    // Remove layer from screen
                    ScreenManager.TopScreen?.RemoveLayer(_upgradeLayer);
                    _upgradeLayer = null;
                    
                    ModLogger.Debug("QuartermasterUI", "Upgrade screen closed successfully");
                }
                
                // Clean up ViewModel (OnFinalize should cascade to child ViewModels in MBBindingList)
                if (_upgradeViewModel != null)
                {
                    _upgradeViewModel.OnFinalize();
                    _upgradeViewModel = null;
                }
                
                // Note: Child ViewModels (QuartermasterUpgradeItemVm and UpgradeOptionVm) in MBBindingList
                // should be automatically finalized by parent OnFinalize() call. Monitor for memory leaks
                // if upgrade screen is used frequently.
                
                // Return to quartermaster conversation if requested
                if (returnToConversation && _returnToConversationOnClose)
                {
                    EnlistedDialogManager.RestartQuartermasterConversation();
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("QuartermasterUI", "Error closing upgrade screen", ex);
            }
        }
        
        #endregion
    }
}
