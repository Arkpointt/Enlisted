using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.GauntletUI.Data;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;
using Enlisted.Features.Equipment.Behaviors;
using Enlisted.Mod.Core.Logging;

namespace Enlisted.Features.Equipment.UI
{
    /// <summary>
    /// Gauntlet equipment selector behavior providing individual clickable equipment selection.
    /// 
    /// Based on SAS weaponsmith approach but using verified current TaleWorlds APIs.
    /// Creates a custom UI overlay showing equipment variants as clickable buttons.
    /// </summary>
    public class QuartermasterEquipmentSelectorBehavior : CampaignBehaviorBase
    {
        public static QuartermasterEquipmentSelectorBehavior Instance { get; private set; }
        
        // Gauntlet UI components (using VERIFIED current API types)
        private static GauntletLayer _gauntletLayer;
        private static IGauntletMovie _gauntletMovie;  // Current API returns interface
        private static QuartermasterEquipmentSelectorVM _selectorViewModel;
        
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
                
                // VERIFIED API: Create Gauntlet layer (exact SAS pattern)
                _gauntletLayer = new GauntletLayer(1001, "GauntletLayer", false);
                
                // Create ViewModel with flat collection (CORRECT modern approach)
                _selectorViewModel = new QuartermasterEquipmentSelectorVM(availableVariants, targetSlot, equipmentType);
                _selectorViewModel.RefreshValues();
                
                // FIXED: Load template from official module structure GUI/Prefabs/Equipment/
                _gauntletMovie = _gauntletLayer.LoadMovie("QuartermasterEquipmentGrid", _selectorViewModel);
                
                // EXACT SAS PATTERN: Register hotkeys first, then input restrictions
                _gauntletLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
                _gauntletLayer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.All);
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
        /// Fallback to conversation-based selection (proven to work).
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
                    method?.Invoke(quartermasterManager, new object[] { availableVariants, equipmentType });
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("QuartermasterUI", "Both grid and conversation UI failed", ex);
            }
        }
        
        /// <summary>
        /// Close equipment selector using VERIFIED cleanup pattern.
        /// </summary>
        public static void CloseEquipmentSelector()
        {
            try
            {
                if (_gauntletLayer != null)
                {
                    // VERIFIED API: Cleanup pattern from current TaleWorlds code
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
        
        /// <summary>
        /// Check if equipment selector is currently active.
        /// </summary>
        public static bool IsActive => _gauntletLayer != null;
    }
}
