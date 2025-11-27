using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Core.ViewModelCollection.Generic;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using Enlisted.Features.Equipment.Behaviors;
using Enlisted.Mod.Core.Logging;

namespace Enlisted.Features.Equipment.UI
{
    /// <summary>
    /// Individual equipment item ViewModel for clickable selection.
    /// 
    /// Represents a single equipment variant option with proper data binding.
    /// Uses TaleWorlds ViewModel APIs for data binding and property change notifications.
    /// </summary>
    public class QuartermasterEquipmentItemVM : ViewModel
    {
        // DataSourceProperty attributes enable data binding with the UI layer
        
        [DataSourceProperty] 
        public string CostText { get; private set; }
        
        [DataSourceProperty]
        public string StatusText { get; private set; }
        
        [DataSourceProperty]
        public bool IsCurrentEquipment { get; private set; }
        
        [DataSourceProperty]
        public bool CanAfford { get; private set; }
        
        [DataSourceProperty]
        public bool IsEnabled { get; private set; }
        
        [DataSourceProperty]
        public ItemImageIdentifierVM Image { get; private set; }
        
        [DataSourceProperty]
        public string ItemName { get; private set; }
        
        [DataSourceProperty]
        public string WeaponDetails { get; private set; }
        
        // Internal data
        private EquipmentVariantOption _variant;
        private QuartermasterEquipmentSelectorVM _parentSelector;
        
        /// <summary>
        /// Initialize equipment item with variant data.
        /// Sets up the ViewModel with equipment variant information for display and selection.
        /// </summary>
        public QuartermasterEquipmentItemVM(EquipmentVariantOption variant, QuartermasterEquipmentSelectorVM parent)
        {
            _variant = variant; // Allow null for empty slot display
            _parentSelector = parent ?? throw new ArgumentNullException(nameof(parent));
            
            // Initialize basic properties
            
            RefreshValues();
        }
        
        /// <summary>
        /// Refresh display values when equipment variant data changes.
        /// </summary>
        public override void RefreshValues()
        {
            base.RefreshValues();
            
            try
            {
                if (_variant?.Item == null)
                {
                    SetEmptyValues();
                    return;
                }
                
                // Build item details for display
                var item = _variant.Item;
                
                // Set item name and basic properties
                ItemName = item.Name?.ToString() ?? "Unknown Item";
                IsCurrentEquipment = _variant.IsCurrent;
                CanAfford = _variant.CanAfford;
                IsEnabled = !_variant.IsCurrent && _variant.CanAfford;
                
                // Set item image using ItemImageIdentifierVM for proper image display (1.3.4 API)
                Image = new ItemImageIdentifierVM(item, "");
                
                // Debug logging to diagnose image loading issues
                ModLogger.Debug("QuartermasterUI", $"Item image created - Name: {item.Name}, StringId: {item.StringId}, Image.Id: {Image.Id}, Image.TextureProviderName: {Image.TextureProviderName}");
                
                // Build simplified weapon details
                WeaponDetails = BuildSimpleWeaponDetails(item);
                
                // Set cost and status
                if (_variant.IsCurrent)
                {
                    CostText = "(Current)";
                    StatusText = "Currently Equipped";
                }
                else if (_variant.CanAfford)
                {
                    CostText = $"{_variant.Cost} denars";
                    StatusText = "Available";
                }
                else
                {
                    CostText = $"{_variant.Cost} denars";
                    StatusText = "Insufficient Funds";
                }
                
                // Notify UI of property changes for data binding updates
                OnPropertyChangedWithValue(ItemName, "ItemName");
                OnPropertyChangedWithValue(CostText, "CostText");
                OnPropertyChangedWithValue(StatusText, "StatusText");
                OnPropertyChangedWithValue(IsCurrentEquipment, "IsCurrentEquipment");
                OnPropertyChangedWithValue(CanAfford, "CanAfford");
                OnPropertyChangedWithValue(IsEnabled, "IsEnabled");
                OnPropertyChangedWithValue(Image, "Image");
                OnPropertyChangedWithValue(WeaponDetails, "WeaponDetails");
            }
            catch (Exception ex)
            {
                ModLogger.Error("QuartermasterUI", "Error refreshing equipment item values", ex);
                SetEmptyValues();
            }
        }
        
        /// <summary>
        /// Set safe fallback values for error cases or empty slots.
        /// </summary>
        private void SetEmptyValues()
        {
            if (_variant == null)
            {
                // Empty slot display
                ItemName = "Empty";
                CostText = "";
                StatusText = "Empty Slot";
                WeaponDetails = "";
                Image = new ItemImageIdentifierVM(null, ""); // Empty image identifier (1.3.4 API)
            }
            else
            {
                // Error case
                ItemName = "Error";
                CostText = "";
                StatusText = "Error loading item";
                WeaponDetails = "";
                Image = new ItemImageIdentifierVM(null, ""); // Empty image identifier for error case (1.3.4 API)
            }
            IsCurrentEquipment = false;
            CanAfford = false;
            IsEnabled = false;
        }
        
        /// <summary>
        /// Handle clicking on this equipment item (main functionality).
        /// Executes when the player clicks on an equipment item in the UI.
        /// </summary>
        public void ExecuteSelectItem()
        {
            try
            {
                if (_variant == null)
                {
                    // Empty slot - unequip current equipment
                    InformationManager.DisplayMessage(new InformationMessage("Equipment unequipping not yet supported."));
                    return;
                }
                
                if (_variant.Item == null)
                {
                    ModLogger.Error("QuartermasterUI", "Cannot select item - variant item is null");
                    return;
                }
                
                if (_variant.IsCurrent)
                {
                    InformationManager.DisplayMessage(new InformationMessage(
                        new TextObject("You already have this equipment equipped.").ToString()));
                    return;
                }
                
                if (!_variant.CanAfford)
                {
                    var message = new TextObject("Insufficient funds. You need {COST} denars for this equipment.");
                    message.SetTextVariable("COST", _variant.Cost.ToString());
                    InformationManager.DisplayMessage(new InformationMessage(message.ToString()));
                    return;
                }
                
                // Show confirmation
                var confirmText = $"Equipping {_variant.Item.Name?.ToString()} for {_variant.Cost} denars...";
                InformationManager.DisplayMessage(new InformationMessage(confirmText));
                
                // Apply selection through parent
                _parentSelector?.OnEquipmentItemSelected(_variant);
                
                ModLogger.Info("QuartermasterUI", $"Equipment item selected: {_variant.Item.Name?.ToString()} for {_variant.Cost} denars");
            }
            catch (Exception ex)
            {
                ModLogger.Error("QuartermasterUI", "Error selecting equipment item", ex);
                InformationManager.DisplayMessage(new InformationMessage(
                    new TextObject("Error selecting equipment. Please try again.").ToString()));
            }
        }
        
        /// <summary>
        /// Handle previewing this equipment item.
        /// </summary>
        public void ExecutePreviewItem()
        {
            try
            {
                if (_variant?.Item != null)
                {
                    var previewText = $"Preview: {_variant.Item.Name?.ToString()} - {_variant.Cost} denars";
                    InformationManager.DisplayMessage(new InformationMessage(previewText));
                }
                else
                {
                    // Empty slot or null item
                    InformationManager.DisplayMessage(new InformationMessage("Empty equipment slot"));
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("QuartermasterUI", "Error previewing equipment item", ex);
            }
        }
        
        /// <summary>
        /// Build simplified weapon details with key stats.
        /// </summary>
        private string BuildSimpleWeaponDetails(ItemObject item)
        {
            try
            {
                if (item == null) return "";
                
                var details = new List<string>();
                
                // Culture info
                if (item.Culture?.Name != null)
                {
                    details.Add($"Culture: {item.Culture.Name}");
                }
                else
                {
                    details.Add("Culture: No Culture");
                }
                
                // Weapon class and tier
                if (item.WeaponComponent != null)
                {
                    var weaponClass = item.WeaponComponent.GetItemType().ToString();
                    details.Add($"Class: {weaponClass}");
                    details.Add($"Weapon Tier: {item.Tier}");
                    
                    // Basic weapon stats
                    var weapon = item.WeaponComponent.PrimaryWeapon;
                    if (weapon.ThrustDamage > 0)
                        details.Add($"Thrust Speed: {weapon.ThrustSpeed}");
                    if (weapon.SwingDamage > 0)
                        details.Add($"Swing Speed: {weapon.SwingSpeed}");
                    if (weapon.ThrustDamage > 0)
                        details.Add($"Thrust Damage: {weapon.ThrustDamage} {weapon.ThrustDamageType}");
                    if (weapon.SwingDamage > 0)
                        details.Add($"Swing Damage: {weapon.SwingDamage} {weapon.SwingDamageType}");
                    details.Add($"Length: {weapon.WeaponLength}");
                    details.Add($"Handling: {weapon.Handling}");
                }
                
                return string.Join("\n", details);
            }
            catch (Exception ex)
            {
                ModLogger.Error("QuartermasterUI", "Error building weapon details", ex);
                return "Details unavailable";
            }
        }
    }
}
