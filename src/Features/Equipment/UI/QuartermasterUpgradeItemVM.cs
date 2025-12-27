using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using Enlisted.Features.Equipment.Behaviors;
using Enlisted.Mod.Core.Logging;

namespace Enlisted.Features.Equipment.UI
{
    /// <summary>
    /// Individual upgradeable equipment item ViewModel.
    /// Represents a single equipped item with its available upgrade tiers.
    /// </summary>
    public class QuartermasterUpgradeItemVm : ViewModel
    {
        [DataSourceProperty]
        public string ItemName { get; private set; }

        [DataSourceProperty]
        public string CurrentQualityText { get; private set; }

        [DataSourceProperty]
        public string CurrentQualityColor { get; private set; }

        [DataSourceProperty]
        public string SlotTypeText { get; private set; }

        [DataSourceProperty]
        public ItemImageIdentifierVM Image { get; private set; }

        [DataSourceProperty]
        public MBBindingList<UpgradeOptionVm> UpgradeOptions { get; }

        [DataSourceProperty]
        public bool HasUpgrades { get; private set; }

        [DataSourceProperty]
        public string NoUpgradeReason { get; private set; }

        private readonly EquipmentIndex _slot;
        private readonly EquipmentElement _currentElement;
        private readonly QuartermasterUpgradeVm _parent;

        /// <summary>
        /// Initialize upgrade item with current equipment.
        /// </summary>
        public QuartermasterUpgradeItemVm(EquipmentIndex slot, EquipmentElement element, QuartermasterUpgradeVm parent)
        {
            _slot = slot;
            _currentElement = element;
            _parent = parent ?? throw new ArgumentNullException(nameof(parent));

            UpgradeOptions = new MBBindingList<UpgradeOptionVm>();
        }

        /// <summary>
        /// Refresh display values when upgrade data changes.
        /// </summary>
        public override void RefreshValues()
        {
            base.RefreshValues();

            try
            {
                if (_currentElement.IsEmpty || _currentElement.Item == null)
                {
                    SetEmptyValues();
                    return;
                }

                var item = _currentElement.Item;
                var currentModifier = _currentElement.ItemModifier;
                var currentQuality = QuartermasterManager.GetModifierQuality(item, currentModifier);

                // Set item display
                ItemName = item.Name?.ToString() ?? "Unknown Item";
                SlotTypeText = GetSlotTypeName(_slot);
                Image = new ItemImageIdentifierVM(item);

                // Set current quality display
                SetCurrentQualityDisplay(currentQuality);

                // Build available upgrades
                UpgradeOptions.Clear();
                BuildUpgradeOptions(item, currentModifier, currentQuality);

                HasUpgrades = UpgradeOptions.Count > 0;

                if (!HasUpgrades)
                {
                    // Determine why no upgrades are available
                    var modGroup = item.ItemComponent?.ItemModifierGroup;
                    if (modGroup == null)
                    {
                        NoUpgradeReason = new TextObject("{=qm_upgrade_no_modifier_group}This item cannot be improved.").ToString();
                    }
                    else if (currentQuality == ItemQuality.Legendary)
                    {
                        NoUpgradeReason = new TextObject("{=qm_upgrade_max_quality}Already the best it can be.").ToString();
                    }
                    else
                    {
                        NoUpgradeReason = new TextObject("{=qm_upgrade_no_items}No upgrade tiers available for this item.").ToString();
                    }
                }
                else
                {
                    NoUpgradeReason = "";
                }

                // Notify UI of property changes
                OnPropertyChanged(nameof(ItemName));
                OnPropertyChanged(nameof(CurrentQualityText));
                OnPropertyChanged(nameof(CurrentQualityColor));
                OnPropertyChanged(nameof(SlotTypeText));
                OnPropertyChanged(nameof(Image));
                OnPropertyChanged(nameof(HasUpgrades));
                OnPropertyChanged(nameof(NoUpgradeReason));
            }
            catch (Exception ex)
            {
                ModLogger.Error("QuartermasterUI", "Error refreshing upgrade item values", ex);
                SetEmptyValues();
            }
        }

        /// <summary>
        /// Build available upgrade options for this item.
        /// Filters by reputation requirements and modifier group availability.
        /// </summary>
        private void BuildUpgradeOptions(ItemObject item, ItemModifier currentModifier, ItemQuality currentQuality)
        {
            try
            {
                var modGroup = item.ItemComponent?.ItemModifierGroup;
                if (modGroup == null)
                {
                    // No modifier group - cannot upgrade
                    return;
                }

                // Get available upgrade tiers based on QM reputation
                var availableTiers = QuartermasterManager.Instance?.GetAvailableUpgradeTiers()
                                   ?? new List<ItemQuality>();

                // Build upgrade options for each tier higher than current quality
                foreach (var targetQuality in availableTiers)
                {
                    // Skip if not an upgrade from current quality
                    if (targetQuality <= currentQuality)
                    {
                        continue;
                    }

                    // Check if this quality tier exists for this item
                    var modifiers = modGroup.GetModifiersBasedOnQuality(targetQuality);
                    if (modifiers == null || modifiers.Count == 0)
                    {
                        // This quality tier doesn't exist for this item
                        continue;
                    }

                    // Calculate upgrade cost
                    int cost = QuartermasterManager.Instance?.CalculateUpgradeCost(_currentElement, targetQuality) ?? 0;
                    bool canAfford = TaleWorlds.CampaignSystem.Hero.MainHero?.Gold >= cost;

                    // Add upgrade option
                    var upgradeOption = new UpgradeOptionVm(
                        _slot,
                        targetQuality,
                        cost,
                        canAfford,
                        this);

                    UpgradeOptions.Add(upgradeOption);

                    // Refresh the upgrade option to populate its display values
                    upgradeOption.RefreshValues();
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("QuartermasterUI", "Error building upgrade options", ex);
            }
        }

        /// <summary>
        /// Handle upgrade selection (called by child UpgradeOptionVm).
        /// </summary>
        public void OnUpgradeSelected(EquipmentIndex slot, ItemQuality targetQuality)
        {
            _parent?.OnUpgradePerformed(slot, targetQuality);
        }

        /// <summary>
        /// Set display for current quality tier.
        /// </summary>
        private void SetCurrentQualityDisplay(ItemQuality quality)
        {
            CurrentQualityText = quality switch
            {
                ItemQuality.Poor => new TextObject("{=qm_quality_poor}Poor").ToString(),
                ItemQuality.Inferior => new TextObject("{=qm_quality_inferior}Worn").ToString(),
                ItemQuality.Common => new TextObject("{=qm_quality_common}Standard").ToString(),
                ItemQuality.Fine => new TextObject("{=qm_quality_fine}Fine").ToString(),
                ItemQuality.Masterwork => new TextObject("{=qm_quality_masterwork}Masterwork").ToString(),
                ItemQuality.Legendary => new TextObject("{=qm_quality_legendary}Legendary").ToString(),
                _ => new TextObject("{=qm_quality_common}Standard").ToString()
            };

            CurrentQualityColor = quality switch
            {
                ItemQuality.Poor => "#909090FF",        // Light gray
                ItemQuality.Inferior => "#CD853FFF",    // Peru/tan
                ItemQuality.Common => "#E8E8E8FF",      // Off-white
                ItemQuality.Fine => "#90EE90FF",        // Light green
                ItemQuality.Masterwork => "#6495EDFF",  // Cornflower blue
                ItemQuality.Legendary => "#FFD700FF",   // Gold
                _ => "#E8E8E8FF"
            };
        }

        /// <summary>
        /// Set safe fallback values for error cases or empty slots.
        /// </summary>
        private void SetEmptyValues()
        {
            ItemName = new TextObject("{=qm_ui_empty}Empty").ToString();
            CurrentQualityText = "";
            CurrentQualityColor = "";
            SlotTypeText = "";
            Image = new ItemImageIdentifierVM(null);
            HasUpgrades = false;
            NoUpgradeReason = new TextObject("{=qm_ui_empty_slot_label}Empty Slot").ToString();
            UpgradeOptions.Clear();
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

    /// <summary>
    /// Individual upgrade tier option for an item.
    /// </summary>
    public class UpgradeOptionVm : ViewModel
    {
        [DataSourceProperty]
        public string QualityText { get; private set; }

        [DataSourceProperty]
        public string QualityColor { get; private set; }

        [DataSourceProperty]
        public string CostText { get; private set; }

        [DataSourceProperty]
        public bool CanAfford { get; private set; }

        [DataSourceProperty]
        public bool IsEnabled { get; private set; }

        [DataSourceProperty]
        public string DisabledReason { get; private set; }

        private readonly EquipmentIndex _slot;
        private readonly ItemQuality _targetQuality;
        private readonly int _cost;
        private readonly QuartermasterUpgradeItemVm _parent;

        public UpgradeOptionVm(
            EquipmentIndex slot,
            ItemQuality targetQuality,
            int cost,
            bool canAfford,
            QuartermasterUpgradeItemVm parent)
        {
            _slot = slot;
            _targetQuality = targetQuality;
            _cost = cost;
            CanAfford = canAfford;
            _parent = parent ?? throw new ArgumentNullException(nameof(parent));
        }

        public override void RefreshValues()
        {
            base.RefreshValues();

            try
            {
                // Set quality display
                QualityText = _targetQuality switch
                {
                    ItemQuality.Fine => new TextObject("{=qm_quality_fine}Fine").ToString(),
                    ItemQuality.Masterwork => new TextObject("{=qm_quality_masterwork}Masterwork").ToString(),
                    ItemQuality.Legendary => new TextObject("{=qm_quality_legendary}Legendary").ToString(),
                    _ => _targetQuality.ToString()
                };

                QualityColor = _targetQuality switch
                {
                    ItemQuality.Fine => "#90EE90FF",        // Light green
                    ItemQuality.Masterwork => "#6495EDFF",  // Cornflower blue
                    ItemQuality.Legendary => "#FFD700FF",   // Gold
                    _ => "#E8E8E8FF"
                };

                // Set cost display
                CostText = $"{_cost} denars";

                // Determine if this upgrade is available
                IsEnabled = CanAfford;

                if (!CanAfford)
                {
                    DisabledReason = new TextObject("{=qm_upgrade_cannot_afford}You can't afford this upgrade.").ToString();
                }
                else
                {
                    DisabledReason = "";
                }

                // Notify UI
                OnPropertyChanged(nameof(QualityText));
                OnPropertyChanged(nameof(QualityColor));
                OnPropertyChanged(nameof(CostText));
                OnPropertyChanged(nameof(CanAfford));
                OnPropertyChanged(nameof(IsEnabled));
                OnPropertyChanged(nameof(DisabledReason));
            }
            catch (Exception ex)
            {
                ModLogger.Error("QuartermasterUI", "Error refreshing upgrade option values", ex);
            }
        }

        /// <summary>
        /// Handle clicking on this upgrade option.
        /// </summary>
        public void ExecuteUpgrade()
        {
            try
            {
                if (!IsEnabled)
                {
                    if (!CanAfford)
                    {
                        InformationManager.DisplayMessage(
                            new InformationMessage(DisabledReason, Colors.Red));
                    }
                    return;
                }

                _parent?.OnUpgradeSelected(_slot, _targetQuality);
            }
            catch (Exception ex)
            {
                ModLogger.Error("QuartermasterUI", "Error executing upgrade", ex);
            }
        }
    }
}

