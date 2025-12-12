using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using Enlisted.Features.Equipment.Behaviors;
using Enlisted.Mod.Core.Logging;
using Enlisted.Mod.Core.Util;

namespace Enlisted.Features.Equipment.UI
{
    /// <summary>
    /// Individual equipment item ViewModel for clickable selection.
    /// 
    /// Represents a single equipment variant option with proper data binding.
    /// Uses TaleWorlds ViewModel APIs for data binding and property change notifications.
    /// </summary>
    public class QuartermasterEquipmentItemVm : ViewModel
    {
        // DataSourceProperty attributes enable data binding with the UI layer
        
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
        [DataSourceProperty] public string CostText { get; private set; }
        
        /// <summary>
        /// Primary stats line showing key stats for weapons/armor (displayed below image).
        /// </summary>
        [DataSourceProperty]
        public string PrimaryStats { get; private set; }
        
        /// <summary>
        /// Secondary stats line for additional details (tier, material type, etc).
        /// </summary>
        [DataSourceProperty]
        public string SecondaryStats { get; private set; }
        
        /// <summary>
        /// Slot type indicator (Head, Body, Weapon, etc).
        /// </summary>
        [DataSourceProperty]
        public string SlotTypeText { get; private set; }
        
        // Legacy property for backwards compatibility
        [DataSourceProperty]
        public string WeaponDetails { get; private set; }
        
        // Internal data (readonly as set only in constructor)
        private readonly EquipmentVariantOption _variant;
        private readonly QuartermasterEquipmentSelectorVm _parentSelector;
        
        /// <summary>
        /// Get the underlying variant option for state updates.
        /// </summary>
        public EquipmentVariantOption GetVariant() => _variant;
        
        /// <summary>
        /// Initialize equipment item with variant data.
        /// Sets up the ViewModel with equipment variant information for display and selection.
        /// </summary>
        public QuartermasterEquipmentItemVm(EquipmentVariantOption variant, QuartermasterEquipmentSelectorVm parent)
        {
            _variant = variant; // Allow null for empty slot display
            _parentSelector = parent ?? throw new ArgumentNullException(nameof(parent));
            
            // Note: RefreshValues() should be called by the parent after construction
            // to avoid virtual member call in constructor (ReSharper warning)
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
                
                // Build item details for display, include price
                var item = _variant.Item;
                CostText = $"Price: {_variant.Cost} denars";
                
                // Set item name and basic properties
                ItemName = item.Name?.ToString() ?? "Unknown Item";
                IsCurrentEquipment = _variant.IsCurrent;
                CanAfford = _variant.CanAfford;
                
                // Determine slot type for enable/disable behavior (weapons can be bought repeatedly).
                var isWeaponSlot = _variant.Slot is >= EquipmentIndex.Weapon0 and <= EquipmentIndex.Weapon3;
                
                // Set item image using ItemImageIdentifierVM for proper image display (1.3.4 API)
                Image = new ItemImageIdentifierVM(item);
                
                // Build equipment stats based on item type (weapon, armor, or accessory)
                BuildEquipmentStats(item);
                
                // Legacy support - combine stats into WeaponDetails for backwards compatibility
                WeaponDetails = $"{PrimaryStats}\n{SecondaryStats}";
                
                // Purchase-based status (no issue limits / no accountability).
                if (_variant.IsCurrent)
                {
                    StatusText = new TextObject("{=qm_status_equipped}Equipped").ToString();
                }
                else if (!_variant.CanAfford)
                {
                    StatusText = "Insufficient funds";
                }
                else
                {
                    StatusText = new TextObject("{=qm_status_available}Available").ToString();
                }

                // Disable buying the currently-equipped non-weapon item (no-op purchase).
                // Weapons can be purchased repeatedly (another copy goes to an empty weapon slot or inventory).
                IsEnabled = _variant.CanAfford && (isWeaponSlot || !_variant.IsCurrent);
                
                // Notify UI of property changes for data binding updates
                OnPropertyChanged(nameof(ItemName));
                OnPropertyChanged(nameof(CostText));
                OnPropertyChanged(nameof(StatusText));
                OnPropertyChanged(nameof(IsCurrentEquipment));
                OnPropertyChanged(nameof(CanAfford));
                OnPropertyChanged(nameof(IsEnabled));
                OnPropertyChanged(nameof(Image));
                OnPropertyChanged(nameof(PrimaryStats));
                OnPropertyChanged(nameof(SecondaryStats));
                OnPropertyChanged(nameof(SlotTypeText));
                OnPropertyChanged(nameof(WeaponDetails));
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
                PrimaryStats = "";
                SecondaryStats = "";
                SlotTypeText = "";
                WeaponDetails = "";
                Image = new ItemImageIdentifierVM(null);
            }
            else
            {
                // Error case
                ItemName = "Error";
                CostText = "";
                StatusText = "Error loading item";
                PrimaryStats = "";
                SecondaryStats = "";
                SlotTypeText = "";
                WeaponDetails = "";
                Image = new ItemImageIdentifierVM(null);
            }
            IsCurrentEquipment = false;
            CanAfford = false;
            IsEnabled = false;
        }
        
        /// <summary>
        /// Handle clicking on this equipment item (main functionality).
        /// Executes when the player clicks on an equipment item in the UI.
        /// </summary>
        [UsedImplicitly("Bound via Gauntlet XML: Command.Click")]
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
                
                // Block purchase when the player can't afford it.
                if (!_variant.CanAfford)
                {
                    var msg = new TextObject("{=qm_cannot_afford}You can’t afford this. Cost: {COST} denars.");
                    msg.SetTextVariable("COST", _variant.Cost);
                    InformationManager.DisplayMessage(new InformationMessage(msg.ToString(), Colors.Red));
                    return;
                }
                
                // Apply selection through parent
                _parentSelector?.OnEquipmentItemSelected(_variant);
                
                ModLogger.Info("QuartermasterUI", $"Equipment purchased: {_variant.Item.Name} ({_variant.Cost} denars)");
            }
            catch (Exception ex)
            {
                ModLogger.Error("QuartermasterUI", "Error selecting equipment item", ex);
                InformationManager.DisplayMessage(new InformationMessage(
                    new TextObject("{=qm_error_selecting}Error selecting equipment. Please try again.").ToString()));
            }
        }
        
        /// <summary>
        /// Handle previewing this equipment item.
        /// </summary>
        [UsedImplicitly("Bound via Gauntlet XML: Command.Click")]
        public void ExecutePreviewItem()
        {
            try
            {
                if (_variant?.Item != null)
                {
                    var previewText = $"Preview: {_variant.Item.Name} - {_variant.Cost} denars";
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
        /// Build equipment stats based on item type (weapon, armor, shield, etc).
        /// Sets PrimaryStats, SecondaryStats, and SlotTypeText properties.
        /// </summary>
        private void BuildEquipmentStats(ItemObject item)
        {
            try
            {
                if (item == null)
                {
                    PrimaryStats = "";
                    SecondaryStats = "";
                    SlotTypeText = "";
                    return;
                }
                
                // Determine slot type display name
                SlotTypeText = GetSlotTypeName(_variant.Slot);
                
                // Build stats based on item component type
                if (item.ArmorComponent != null)
                {
                    BuildArmorStats(item);
                }
                else if (item.WeaponComponent != null)
                {
                    BuildWeaponStats(item);
                }
                else
                {
                    // Generic item (banner, horse, etc)
                    var tierNumber = ((int)item.Tier) + 1; // Tier enum is 0-based
                    PrimaryStats = $"Tier {tierNumber}";
                    SecondaryStats = item.Culture?.Name?.ToString() ?? "";
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("QuartermasterUI", "Error building equipment stats", ex);
                PrimaryStats = "";
                SecondaryStats = "Stats unavailable";
            }
        }
        
        /// <summary>
        /// Build armor-specific stats for display.
        /// Shows armor values and material type.
        /// </summary>
        private void BuildArmorStats(ItemObject item)
        {
            var armor = item.ArmorComponent;
            var statParts = new List<string>();
            
            // Show relevant armor values based on slot type
            switch (_variant.Slot)
            {
                case EquipmentIndex.Head:
                    if (armor.HeadArmor > 0)
                    {
                        statParts.Add($"Head: {armor.HeadArmor}");
                    }
                    break;
                    
                case EquipmentIndex.Body:
                    if (armor.BodyArmor > 0)
                    {
                        statParts.Add($"Body: {armor.BodyArmor}");
                    }
                    if (armor.ArmArmor > 0)
                    {
                        statParts.Add($"Arm: {armor.ArmArmor}");
                    }
                    if (armor.LegArmor > 0)
                    {
                        statParts.Add($"Leg: {armor.LegArmor}");
                    }
                    break;
                    
                case EquipmentIndex.Leg:
                    if (armor.LegArmor > 0)
                    {
                        statParts.Add($"Leg: {armor.LegArmor}");
                    }
                    break;
                    
                case EquipmentIndex.Gloves:
                    if (armor.ArmArmor > 0)
                    {
                        statParts.Add($"Arm: {armor.ArmArmor}");
                    }
                    break;
                    
                case EquipmentIndex.Cape:
                    // Capes typically have body armor bonus
                    if (armor.BodyArmor > 0)
                    {
                        statParts.Add($"Body: {armor.BodyArmor}");
                    }
                    break;
                    
                default:
                    // Show all non-zero armor values
                    if (armor.HeadArmor > 0)
                    {
                        statParts.Add($"H:{armor.HeadArmor}");
                    }
                    if (armor.BodyArmor > 0)
                    {
                        statParts.Add($"B:{armor.BodyArmor}");
                    }
                    if (armor.ArmArmor > 0)
                    {
                        statParts.Add($"A:{armor.ArmArmor}");
                    }
                    if (armor.LegArmor > 0)
                    {
                        statParts.Add($"L:{armor.LegArmor}");
                    }
                    break;
            }
            
            // Set primary stats (armor values)
            PrimaryStats = statParts.Count > 0 ? string.Join(" | ", statParts) : "No armor bonus";
            
            // Set secondary stats (material and tier)
            // Note: item.Tier is an enum (Tier1, Tier2, etc.) - extract just the number
            var tierNumber = ((int)item.Tier) + 1; // Tier enum is 0-based
            var materialName = armor.MaterialType.ToString();
            SecondaryStats = $"Tier {tierNumber} • {materialName}";
        }
        
        /// <summary>
        /// Build weapon-specific stats for display.
        /// Shows damage, speed, and handling.
        /// </summary>
        private void BuildWeaponStats(ItemObject item)
        {
            var weapon = item.WeaponComponent.PrimaryWeapon;
            var statParts = new List<string>();
            
            // Damage stats - show most relevant based on weapon type
            if (weapon.IsShield)
            {
                // Shield stats
                statParts.Add($"HP: {weapon.MaxDataValue}");
                if (weapon.BodyArmor > 0)
                {
                    statParts.Add($"Armor: {weapon.BodyArmor}");
                }
            }
            else if (weapon.IsRangedWeapon && weapon.IsConsumable)
            {
                // Throwing weapons or arrows
                if (weapon.MissileDamage > 0)
                {
                    statParts.Add($"Dmg: {weapon.MissileDamage}");
                }
                statParts.Add($"Stack: {weapon.MaxDataValue}");
            }
            else if (weapon.IsRangedWeapon)
            {
                // Bows/crossbows
                if (weapon.MissileSpeed > 0)
                {
                    statParts.Add($"Speed: {weapon.MissileSpeed}");
                }
                if (weapon.ThrustDamage > 0)
                {
                    statParts.Add($"Dmg: {weapon.ThrustDamage}");
                }
            }
            else
            {
                // Melee weapons - show primary damage type
                if (weapon.SwingDamage > 0)
                {
                    statParts.Add($"Swing: {weapon.SwingDamage}");
                }
                if (weapon.ThrustDamage > 0)
                {
                    statParts.Add($"Thrust: {weapon.ThrustDamage}");
                }
            }
            
            // Add length for melee weapons
            if (weapon.WeaponLength > 0 && !weapon.IsRangedWeapon)
            {
                statParts.Add($"Reach: {weapon.WeaponLength}");
            }
            
            PrimaryStats = statParts.Count > 0 ? string.Join(" | ", statParts) : "No stats";
            
            // Secondary stats: handling and tier
            // Note: item.Tier is an enum (Tier1, Tier2, etc.) - extract just the number
            var tierNumber = ((int)item.Tier) + 1; // Tier enum is 0-based
            var handlingText = weapon.Handling > 0 ? $"Handling: {weapon.Handling}" : "";
            SecondaryStats = $"Tier {tierNumber}" + (handlingText.Length > 0 ? $" • {handlingText}" : "");
        }
        
        /// <summary>
        /// Get human-readable slot type name for display.
        /// </summary>
        private static string GetSlotTypeName(EquipmentIndex slot)
        {
            return slot switch
            {
                EquipmentIndex.Head => "Helmet",
                EquipmentIndex.Body => "Body Armor",
                EquipmentIndex.Leg => "Leg Armor",
                EquipmentIndex.Gloves => "Gloves",
                EquipmentIndex.Cape => "Cape",
                EquipmentIndex.Weapon0 => "Weapon 1",
                EquipmentIndex.Weapon1 => "Weapon 2",
                EquipmentIndex.Weapon2 => "Weapon 3",
                EquipmentIndex.Weapon3 => "Weapon 4",
                EquipmentIndex.Horse => "Mount",
                EquipmentIndex.HorseHarness => "Horse Armor",
                _ => "Equipment"
            };
        }
    }
}
