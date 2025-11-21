using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using static TaleWorlds.Core.ViewModelCollection.CharacterViewModel;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using Enlisted.Features.Equipment.Behaviors;
using Enlisted.Mod.Core.Logging;

namespace Enlisted.Features.Equipment.UI
{
    /// <summary>
    /// Equipment selector main ViewModel for grid-based equipment selection.
    /// 
    /// Provides individual clickable equipment items displayed in a grid layout.
    /// Uses TaleWorlds ViewModel patterns for data binding and UI updates.
    /// </summary>
    public class QuartermasterEquipmentSelectorVM : ViewModel
    {
        // Row-based organization for ListPanel with ItemTemplate in grid layout
        [DataSourceProperty]
        public MBBindingList<QuartermasterEquipmentRowVM> EquipmentRows { get; private set; }
        
        [DataSourceProperty]
        public string HeaderText { get; private set; }
        
        [DataSourceProperty]
        public string PlayerGoldText { get; private set; }
        
        [DataSourceProperty]
        public string CurrentEquipmentText { get; private set; }
        
        // CharacterViewModel for displaying character preview with equipment
        [DataSourceProperty]
        public CharacterViewModel UnitCharacter { get; private set; }
        
        // Internal state
        private EquipmentIndex _targetSlot;
        private string _equipmentType;
        private List<EquipmentVariantOption> _availableVariants;
        
        /// <summary>
        /// Initialize equipment selector with available variants.
        /// Sets up the ViewModel with equipment data and organizes items into rows for grid display.
        /// </summary>
        public QuartermasterEquipmentSelectorVM(List<EquipmentVariantOption> availableVariants, EquipmentIndex targetSlot, string equipmentType)
        {
            _availableVariants = availableVariants ?? throw new ArgumentNullException(nameof(availableVariants));
            _targetSlot = targetSlot;
            _equipmentType = equipmentType;
            
            // Organize equipment items into rows with 4 cards per row for grid display
            EquipmentRows = new MBBindingList<QuartermasterEquipmentRowVM>();
            var currentCards = new MBBindingList<QuartermasterEquipmentItemVM>();
            
            // Add equipment variants organized into rows of 4 cards each
            foreach (var variant in availableVariants.Take(15)) // Reasonable limit for 4 rows
            {
                currentCards.Add(new QuartermasterEquipmentItemVM(variant, this));
                
                // Create new row when we reach 4 cards in the current row
                if (currentCards.Count == 4)
                {
                    EquipmentRows.Add(new QuartermasterEquipmentRowVM(currentCards));
                    currentCards = new MBBindingList<QuartermasterEquipmentItemVM>();
                }
            }
            
            // Add remaining cards as final row if any remain
            if (currentCards.Count > 0)
            {
                EquipmentRows.Add(new QuartermasterEquipmentRowVM(currentCards));
            }
            
            // Set up CharacterViewModel for character preview display
            try
            {
                UnitCharacter = new CharacterViewModel(StanceTypes.None);
                UnitCharacter.FillFrom(Hero.MainHero.CharacterObject, -1); // Fill character model for preview
            }
            catch (Exception ex)
            {
                ModLogger.Error("QuartermasterUI", "Error setting up character view model", ex);
                UnitCharacter = null; // Safe fallback
            }
            
            RefreshValues();
        }
        
        /// <summary>
        /// Refresh all display values when equipment data changes.
        /// </summary>
        public override void RefreshValues()
        {
            base.RefreshValues();
            
            try
            {
                var hero = Hero.MainHero;
                var currentEquipment = hero.BattleEquipment[_targetSlot]; // No null-conditional on struct
                var currentItem = currentEquipment.Item;
                
                HeaderText = "Quartermaster";
                PlayerGoldText = $"Your Gold: {hero.Gold} denars";
                CurrentEquipmentText = $"Current: {currentItem?.Name?.ToString() ?? "None"}";
                
                // Refresh all equipment rows and their cards to update display
                foreach (var row in EquipmentRows)
                {
                    foreach (var card in row.Cards)
                    {
                        card.RefreshValues();
                    }
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("QuartermasterUI", "Error refreshing equipment selector values", ex);
                
                // Safe fallback values
                HeaderText = "Quartermaster Equipment";
                PlayerGoldText = "Gold information unavailable";
                CurrentEquipmentText = "Current equipment unknown";
            }
        }
        
        /// <summary>
        /// Handle equipment item selection from child ViewModels.
        /// </summary>
        public void OnEquipmentItemSelected(EquipmentVariantOption selectedVariant)
        {
            try
            {
                if (selectedVariant?.Item == null)
                {
                    ModLogger.Error("QuartermasterUI", "Cannot select equipment - variant or item is null");
                    return;
                }
                
                // Apply equipment through existing QuartermasterManager
                var quartermasterManager = QuartermasterManager.Instance;
                if (quartermasterManager != null)
                {
                    quartermasterManager.RequestEquipmentVariant(selectedVariant.Item, selectedVariant.Slot);
                }
                
                // Close the selector
                ExecuteClose();
                
                ModLogger.Info("QuartermasterUI", $"Applied equipment variant: {selectedVariant.Item.Name?.ToString()}");
            }
            catch (Exception ex)
            {
                ModLogger.Error("QuartermasterUI", "Error applying selected equipment", ex);
            }
        }
        
        /// <summary>
        /// Close the equipment selector when player cancels or completes selection.
        /// </summary>
        public void ExecuteClose()
        {
            try
            {
                QuartermasterEquipmentSelectorBehavior.CloseEquipmentSelector();
            }
            catch (Exception ex)
            {
                ModLogger.Error("QuartermasterUI", "Error closing equipment selector", ex);
            }
        }
        
        /// <summary>
        /// Get display name for equipment slot.
        /// </summary>
        private string GetSlotDisplayName(EquipmentIndex slot)
        {
            return slot switch
            {
                EquipmentIndex.Weapon0 => "Primary Weapon",
                EquipmentIndex.Weapon1 => "Secondary Weapon",
                EquipmentIndex.Weapon2 => "Shield/Backup",
                EquipmentIndex.Weapon3 => "Throwing Weapon",
                EquipmentIndex.Head => "Helmet",
                EquipmentIndex.Body => "Armor",
                EquipmentIndex.Leg => "Boots",
                EquipmentIndex.Gloves => "Gloves",
                EquipmentIndex.Cape => "Cape/Shoulders",
                EquipmentIndex.Horse => "Mount",
                EquipmentIndex.HorseHarness => "Horse Armor",
                _ => "Equipment"
            };
        }
    }
}
