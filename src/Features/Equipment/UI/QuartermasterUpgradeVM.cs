using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using Enlisted.Features.Equipment.Behaviors;
using Enlisted.Mod.Core.Logging;

namespace Enlisted.Features.Equipment.UI
{
    /// <summary>
    /// Main upgrade screen ViewModel showing all equipped items with upgrade options.
    /// </summary>
    public class QuartermasterUpgradeVm : ViewModel
    {
        [DataSourceProperty]
        public string HeaderText { get; private set; }

        [DataSourceProperty]
        public string PlayerGoldText { get; private set; }

        [DataSourceProperty]
        public MBBindingList<QuartermasterUpgradeItemVm> UpgradeableItems { get; }

        [DataSourceProperty]
        public bool HasUpgradeableItems { get; private set; }

        [DataSourceProperty]
        public string NoItemsMessage { get; private set; }

        /// <summary>
        /// Initialize upgrade screen with player's equipped items.
        /// </summary>
        public QuartermasterUpgradeVm()
        {
            UpgradeableItems = new MBBindingList<QuartermasterUpgradeItemVm>();
        }

        /// <summary>
        /// Refresh all display values.
        /// </summary>
        public override void RefreshValues()
        {
            base.RefreshValues();

            try
            {
                var hero = Hero.MainHero;
                if (hero == null)
                {
                    SetEmptyValues();
                    return;
                }

                HeaderText = new TextObject("{=qm_upgrade_title}Improve Equipment").ToString();
                PlayerGoldText = $"Your Gold: {hero.Gold} denars";

                // Build list of equipped items that can be upgraded
                UpgradeableItems.Clear();
                BuildUpgradeableItemsList(hero);

                HasUpgradeableItems = UpgradeableItems.Count > 0;

                if (!HasUpgradeableItems)
                {
                    NoItemsMessage = new TextObject("{=qm_upgrade_no_items}You've nothing that can be improved.").ToString();
                }
                else
                {
                    NoItemsMessage = "";
                }

                // Refresh all upgrade items
                foreach (var item in UpgradeableItems)
                {
                    item.RefreshValues();
                }

                // Notify UI
                OnPropertyChanged(nameof(HeaderText));
                OnPropertyChanged(nameof(PlayerGoldText));
                OnPropertyChanged(nameof(HasUpgradeableItems));
                OnPropertyChanged(nameof(NoItemsMessage));
            }
            catch (Exception ex)
            {
                ModLogger.Error("QuartermasterUI", "Error refreshing upgrade screen values", ex);
                SetEmptyValues();
            }
        }

        /// <summary>
        /// Build list of equipped items that can be upgraded.
        /// Only includes items with modifier groups and available upgrade tiers.
        /// </summary>
        private void BuildUpgradeableItemsList(Hero hero)
        {
            try
            {
                // Check all equipment slots (iterate valid indices only, not NumEquipmentSetSlots)
                for (int i = 0; i < (int)EquipmentIndex.NumEquipmentSetSlots; i++)
                {
                    var slot = (EquipmentIndex)i;
                    var element = hero.BattleEquipment[slot];

                    // Skip empty slots
                    if (element.IsEmpty || element.Item == null)
                    {
                        continue;
                    }

                    // Skip items without modifier groups (cannot be upgraded)
                    var modGroup = element.Item.ItemComponent?.ItemModifierGroup;
                    if (modGroup == null)
                    {
                        continue;
                    }

                    // Skip items already at Legendary quality
                    var currentQuality = QuartermasterManager.GetModifierQuality(element.Item, element.ItemModifier);
                    if (currentQuality == ItemQuality.Legendary)
                    {
                        continue;
                    }

                    // Check if any upgrade tiers are available for this item
                    var availableTiers = QuartermasterManager.Instance?.GetAvailableUpgradeTiers()
                                       ?? new System.Collections.Generic.List<ItemQuality>();

                    bool hasAvailableUpgrade = false;
                    foreach (var tier in availableTiers)
                    {
                        if (tier > currentQuality)
                        {
                            var modifiers = modGroup.GetModifiersBasedOnQuality(tier);
                            if (modifiers != null && modifiers.Count > 0)
                            {
                                hasAvailableUpgrade = true;
                                break;
                            }
                        }
                    }

                    // Add to list if upgrades are available
                    if (hasAvailableUpgrade)
                    {
                        var upgradeItem = new QuartermasterUpgradeItemVm(slot, element, this);
                        UpgradeableItems.Add(upgradeItem);
                    }
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("QuartermasterUI", "Error building upgradeable items list", ex);
            }
        }

        /// <summary>
        /// Handle upgrade execution for a specific item.
        /// Called by child QuartermasterUpgradeItemVm when player selects an upgrade.
        /// </summary>
        public void OnUpgradePerformed(EquipmentIndex slot, ItemQuality targetQuality)
        {
            try
            {
                var qm = QuartermasterManager.Instance;
                if (qm == null)
                {
                    ModLogger.ErrorCode("QuartermasterUI", "E-QM-020", "Upgrade failed: QuartermasterManager instance not found");
                    return;
                }

                // Perform the upgrade
                bool success = qm.PerformUpgrade(slot, targetQuality, out string errorMessage);

                if (success)
                {
                    // Show success message
                    var hero = Hero.MainHero;
                    var item = hero?.BattleEquipment[slot].Item;
                    var qualityName = GetQualityName(targetQuality);

                    var msg = new TextObject("{=qm_upgrade_success}Your {ITEM} has been improved to {QUALITY} quality.");
                    msg.SetTextVariable("ITEM", item?.Name?.ToString() ?? "equipment");
                    msg.SetTextVariable("QUALITY", qualityName);

                    InformationManager.DisplayMessage(new InformationMessage(msg.ToString(), Colors.Green));

                    // Refresh the screen to show updated equipment and gold
                    RefreshValues();
                }
                else
                {
                    // Show error message
                    InformationManager.DisplayMessage(new InformationMessage(errorMessage ?? "Upgrade failed.", Colors.Red));
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("QuartermasterUI", "Error performing upgrade", ex);
                InformationManager.DisplayMessage(new InformationMessage(
                    new TextObject("{=qm_error_upgrade}Error performing upgrade. Please try again.").ToString(), Colors.Red));
            }
        }

        /// <summary>
        /// Handle closing the upgrade screen.
        /// </summary>
        public void ExecuteClose()
        {
            try
            {
                // Close the upgrade screen and return to conversation
                QuartermasterEquipmentSelectorBehavior.CloseUpgradeScreen();
            }
            catch (Exception ex)
            {
                ModLogger.Error("QuartermasterUI", "Error closing upgrade screen", ex);
            }
        }

        /// <summary>
        /// Get localized quality name for display.
        /// </summary>
        private static string GetQualityName(ItemQuality quality)
        {
            return quality switch
            {
                ItemQuality.Poor => new TextObject("{=qm_quality_poor}Poor").ToString(),
                ItemQuality.Inferior => new TextObject("{=qm_quality_inferior}Worn").ToString(),
                ItemQuality.Common => new TextObject("{=qm_quality_common}Standard").ToString(),
                ItemQuality.Fine => new TextObject("{=qm_quality_fine}Fine").ToString(),
                ItemQuality.Masterwork => new TextObject("{=qm_quality_masterwork}Masterwork").ToString(),
                ItemQuality.Legendary => new TextObject("{=qm_quality_legendary}Legendary").ToString(),
                _ => quality.ToString()
            };
        }

        /// <summary>
        /// Set safe fallback values for error cases.
        /// </summary>
        private void SetEmptyValues()
        {
            HeaderText = new TextObject("{=qm_upgrade_title}Improve Equipment").ToString();
            PlayerGoldText = "Gold information unavailable";
            UpgradeableItems.Clear();
            HasUpgradeableItems = false;
            NoItemsMessage = new TextObject("{=qm_upgrade_no_items}You've nothing that can be improved.").ToString();
        }
    }
}

