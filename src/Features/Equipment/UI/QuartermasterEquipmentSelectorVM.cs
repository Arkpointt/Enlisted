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
    /// Equipment selector main ViewModel using VERIFIED current TaleWorlds APIs.
    /// 
    /// Provides individual clickable equipment items similar to SAS weaponsmith.
    /// Based on current ViewModel patterns from BannerEditorVM and InventoryVM.
    /// </summary>
    public class QuartermasterEquipmentSelectorVM : ViewModel
    {
        // EXACT SAS PATTERN: Row-based organization for ListPanel with ItemTemplate
        [DataSourceProperty]
        public MBBindingList<QuartermasterEquipmentRowVM> EquipmentRows { get; private set; }
        
        [DataSourceProperty]
        public string HeaderText { get; private set; }
        
        [DataSourceProperty]
        public string PlayerGoldText { get; private set; }
        
        [DataSourceProperty]
        public string CurrentEquipmentText { get; private set; }
        
        // VERIFIED: CharacterViewModel usage from BannerEditorVM
        [DataSourceProperty]
        public CharacterViewModel UnitCharacter { get; private set; }
        
        // Internal state
        private EquipmentIndex _targetSlot;
        private string _equipmentType;
        private List<EquipmentVariantOption> _availableVariants;
        
        /// <summary>
        /// Initialize equipment selector with available variants.
        /// Using VERIFIED constructor pattern from current ViewModels.
        /// </summary>
        public QuartermasterEquipmentSelectorVM(List<EquipmentVariantOption> availableVariants, EquipmentIndex targetSlot, string equipmentType)
        {
            _availableVariants = availableVariants ?? throw new ArgumentNullException(nameof(availableVariants));
            _targetSlot = targetSlot;
            _equipmentType = equipmentType;
            
            // EXACT SAS PATTERN: Row-based organization (4 cards per row)
            EquipmentRows = new MBBindingList<QuartermasterEquipmentRowVM>();
            var currentCards = new MBBindingList<QuartermasterEquipmentItemVM>();
            
            // Add equipment variants using SAS 4-cards-per-row pattern (no empty slot for Quartermaster)
            foreach (var variant in availableVariants.Take(15)) // Reasonable limit for 4 rows
            {
                currentCards.Add(new QuartermasterEquipmentItemVM(variant, this));
                
                // Create new row when we reach 4 cards (EXACT SAS pattern)
                if (currentCards.Count == 4)
                {
                    EquipmentRows.Add(new QuartermasterEquipmentRowVM(currentCards));
                    currentCards = new MBBindingList<QuartermasterEquipmentItemVM>();
                }
            }
            
            // Add remaining cards as final row (SAS pattern)
            if (currentCards.Count > 0)
            {
                EquipmentRows.Add(new QuartermasterEquipmentRowVM(currentCards));
            }
            
            // VERIFIED: CharacterViewModel setup from BannerEditorView.cs
            try
            {
                UnitCharacter = new CharacterViewModel(StanceTypes.None);
                UnitCharacter.FillFrom(Hero.MainHero.CharacterObject, -1); // VERIFIED method
            }
            catch (Exception ex)
            {
                ModLogger.Error("QuartermasterUI", "Error setting up character view model", ex);
                UnitCharacter = null; // Safe fallback
            }
            
            RefreshValues();
        }
        
        /// <summary>
        /// Refresh all display values - VERIFIED override pattern.
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
                
                // Refresh all equipment rows and their cards (EXACT SAS pattern)
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
        /// Close the equipment selector - VERIFIED command pattern.
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
