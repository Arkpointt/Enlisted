using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.ScreenSystem;
using Enlisted.Features.Equipment.Behaviors;
using Enlisted.Mod.Core.Logging;

namespace Enlisted.Features.Equipment.UI
{
    /// <summary>
    /// Gauntlet equipment selector behavior providing individual clickable equipment selection.
    /// 
    /// Creates a custom UI overlay showing equipment variants as clickable buttons.
    /// Uses TaleWorlds Gauntlet UI system for custom overlay creation and management.
    /// </summary>
    public class QuartermasterEquipmentSelectorBehavior : CampaignBehaviorBase
    {
        public static QuartermasterEquipmentSelectorBehavior Instance { get; private set; }
        
        // Gauntlet UI components for custom overlay display
        private static GauntletLayer _gauntletLayer;
        // 1.3.4 API: LoadMovie now returns GauntletMovieIdentifier instead of IGauntletMovie
        private static GauntletMovieIdentifier _gauntletMovie;
        private static QuartermasterEquipmentSelectorVm _selectorViewModel;
        
        public QuartermasterEquipmentSelectorBehavior()
        {
            Instance = this;
        }
        
        public override void RegisterEvents()
        {
            // No events needed for UI behavior
        }
        
        public override void SyncData(IDataStore dataStore)
        {
            // No persistent data for UI behavior
        }
        
        /// <summary>
        /// Show equipment selector with proper grid UI using OFFICIAL module structure.
        /// Template now located in GUI/Prefabs/Equipment/ following Bannerlord standards.
        /// </summary>
        public static void ShowEquipmentSelector(List<EquipmentVariantOption> availableVariants, EquipmentIndex targetSlot, string equipmentType)
        {
            try
            {
                // Close any existing selector first
                if (_gauntletLayer != null)
                {
                    CloseEquipmentSelector();
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
        /// </summary>
        public static void CloseEquipmentSelector()
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
        }
        
    }
}
