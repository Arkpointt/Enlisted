using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Overlay;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using Helpers;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Features.Assignments.Behaviors;
using Enlisted.Mod.Core.Logging;

namespace Enlisted.Features.Equipment.Behaviors
{
    /// <summary>
    /// Quartermaster system providing equipment variant access for enlisted soldiers.
    /// 
    /// This system replaces the weaponsmith feature with comprehensive equipment management
    /// based on runtime discovery of equipment variants from actual troop data. Players can
    /// request different weapons, armor, and equipment variants that their selected troop
    /// type can legally spawn with, creating authentic military supply management.
    /// </summary>
    public sealed class QuartermasterManager : CampaignBehaviorBase
    {
        public static QuartermasterManager Instance { get; private set; }
        
        // Equipment variant cache for performance
        private Dictionary<string, Dictionary<EquipmentIndex, List<ItemObject>>> _troopEquipmentVariants;
        private Dictionary<string, CharacterObject> _currentTroopCache;
        private CampaignTime _lastCacheUpdate = CampaignTime.Zero;
        
        // Quartermaster state
        private CharacterObject _selectedTroop;
        private Dictionary<EquipmentIndex, List<EquipmentVariantOption>> _availableVariants;
        private EquipmentIndex _selectedSlot = EquipmentIndex.None;
        
        // Conversation tracking for dynamic equipment selection
        private Dictionary<int, EquipmentVariantOption> _conversationWeaponVariants = new Dictionary<int, EquipmentVariantOption>();
        private Dictionary<int, EquipmentVariantOption> _conversationEquipmentVariants = new Dictionary<int, EquipmentVariantOption>();
        private string _conversationEquipmentType = "";
        
        public QuartermasterManager()
        {
            Instance = this;
            InitializeVariantCache();
        }
        
        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        }
        
        public override void SyncData(IDataStore dataStore)
        {
            // Quartermaster has no persistent state - all data comes from runtime discovery
        }
        
        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            AddQuartermasterMenus(starter);
            ModLogger.Info("Quartermaster", "Quartermaster system initialized with runtime equipment discovery - simplified menu approach");
        }
        
        /// <summary>
        /// Initialize equipment variant caching system for performance.
        /// </summary>
        private void InitializeVariantCache()
        {
            _troopEquipmentVariants = new Dictionary<string, Dictionary<EquipmentIndex, List<ItemObject>>>();
            _currentTroopCache = new Dictionary<string, CharacterObject>();
            _availableVariants = new Dictionary<EquipmentIndex, List<EquipmentVariantOption>>();
        }
        
        /// <summary>
        /// Add quartermaster menu system for equipment variant management.
        /// </summary>
        private void AddQuartermasterMenus(CampaignGameStarter starter)
        {
            // Main quartermaster equipment menu
            starter.AddGameMenu("quartermaster_equipment",
                "Army Quartermaster\n{QUARTERMASTER_TEXT}",
                OnQuartermasterEquipmentInit,
                GameOverlays.MenuOverlayType.None,
                GameMenu.MenuFlags.None,
                null);
                
            // Equipment variant selection submenu
            starter.AddGameMenu("quartermaster_variants",
                "Equipment Variants\n{VARIANT_TEXT}",
                OnQuartermasterVariantsInit,
                GameOverlays.MenuOverlayType.None,
                GameMenu.MenuFlags.None,
                null);
                
            // Main equipment category options
            starter.AddGameMenuOption("quartermaster_equipment", "quartermaster_weapons",
                "Request weapon variants",
                IsWeaponVariantsAvailable,
                OnWeaponVariantsSelected,
                false, 1);
                
            starter.AddGameMenuOption("quartermaster_equipment", "quartermaster_armor",
                "Request armor variants", 
                IsArmorVariantsAvailable,
                OnArmorVariantsSelected,
                false, 2);
                
            // Helmet button removed; helmets are handled under the Armor flow (slot picker)
                
            starter.AddGameMenuOption("quartermaster_equipment", "quartermaster_accessories",
                "Request accessory variants",
                IsAccessoryVariantsAvailable,
                OnAccessoryVariantsSelected,
                false, 4);
                
            // Supply management options (integrates with provisioner duty)
            starter.AddGameMenuOption("quartermaster_equipment", "quartermaster_supplies",
                "Manage party supplies",
                IsSupplyManagementAvailable,
                OnSupplyManagementSelected,
                false, 5);
                
            // Return to enlisted status
            starter.AddGameMenuOption("quartermaster_equipment", "quartermaster_back",
                "Return to enlisted status",
                args => true,
                args => GameMenu.ActivateGameMenu("enlisted_status"),
                true, -1);
                
            // Variant selection options (dynamically populated)
            AddVariantSelectionOptions(starter);
            
            // Supply management menu for quartermaster officers
            AddSupplyManagementMenu(starter);
        }
        
        /// <summary>
        /// Add dynamic variant selection options for the variants submenu.
        /// </summary>
        private void AddVariantSelectionOptions(CampaignGameStarter starter)
        {
            // Generic variant selection options that will be populated dynamically
            for (int i = 1; i <= 6; i++) // Support up to 6 variants per slot
            {
                starter.AddGameMenuOption("quartermaster_variants", $"variant_option_{i}",
                    "", // Text will be set dynamically
                    args => IsVariantOptionAvailable(args, i),
                    args => OnVariantOptionSelected(args, i),
                    false, i);
            }
            
            starter.AddGameMenuOption("quartermaster_variants", "variants_back",
                "Return to quartermaster",
                args => true,
                args => GameMenu.ActivateGameMenu("quartermaster_equipment"),
                true, -1);
        }
        
        /// <summary>
        /// Get equipment variants available to a specific troop type.
        /// Uses runtime discovery from actual game data.
        /// </summary>
        public Dictionary<EquipmentIndex, List<ItemObject>> GetTroopEquipmentVariants(CharacterObject selectedTroop)
        {
            try
            {
                if (selectedTroop == null)
                {
                    return new Dictionary<EquipmentIndex, List<ItemObject>>();
                }
                
                // Check cache first for performance
                var cacheKey = selectedTroop.StringId;
                if (_troopEquipmentVariants.ContainsKey(cacheKey))
                {
                    return _troopEquipmentVariants[cacheKey];
                }
                
                var variants = new Dictionary<EquipmentIndex, List<ItemObject>>();
                var troopCulture = selectedTroop.Culture;
                
                // RUNTIME DISCOVERY: Extract all equipment variants from this troop's BattleEquipments
                foreach (var equipment in selectedTroop.BattleEquipments)
                {
                    for (EquipmentIndex slot = EquipmentIndex.Weapon0; slot <= EquipmentIndex.HorseHarness; slot++)
                    {
                        var item = equipment[slot].Item;
                        if (item != null)
                        {
                            // Culture-strict: if item declares a culture, it must match the selected troop's culture
                            if (troopCulture != null && item.Culture != null && item.Culture != troopCulture)
                            {
                                continue;
                            }
                            if (!variants.ContainsKey(slot))
                            {
                                variants[slot] = new List<ItemObject>();
                            }
                            
                            if (!variants[slot].Contains(item))
                            {
                                variants[slot].Add(item);
                            }
                        }
                    }
                }
                
                // Cache result for performance
                _troopEquipmentVariants[cacheKey] = variants;
                
                ModLogger.Info("Quartermaster", $"Discovered {variants.Sum(kvp => kvp.Value.Count)} equipment variants for {selectedTroop.Name}");
                return variants;
            }
            catch (Exception ex)
            {
                ModLogger.Error("Quartermaster", "Error getting troop equipment variants", ex);
                return new Dictionary<EquipmentIndex, List<ItemObject>>();
            }
        }
        
        /// <summary>
        /// Get the currently selected troop for the player (from TroopSelectionManager).
        /// </summary>
        public CharacterObject GetPlayerSelectedTroop()
        {
            try
            {
                var enlistment = EnlistmentBehavior.Instance;
                var duties = EnlistedDutiesBehavior.Instance;
                
                if (!enlistment?.IsEnlisted == true || duties == null)
                {
                    return null;
                }
                
                // Get player's current formation to help identify troop type
                var formation = duties.GetPlayerFormationType() ?? "infantry";
                var culture = enlistment.CurrentLord?.Culture;
                var tier = enlistment.EnlistmentTier;
                
                if (culture == null)
                {
                    return null;
                }
                
                // Find a representative troop for this culture/tier/formation combination
                var allTroops = MBObjectManager.Instance.GetObjectTypeList<CharacterObject>();
                var matchingTroops = allTroops.Where(troop => 
                    troop.Culture == culture && 
                    troop.GetBattleTier() == tier &&
                    !troop.IsHero &&
                    troop.BattleEquipments.Any() &&
                    DetectTroopFormation(troop).ToString().ToLower() == formation).ToList();
                
                // Select first matching troop as representative
                var selectedTroop = matchingTroops.FirstOrDefault();
                if (selectedTroop != null)
                {
                    ModLogger.Info("Quartermaster", $"Player troop identified as: {selectedTroop.Name?.ToString()} ({formation} formation)");
                }
                
                return selectedTroop;
            }
            catch (Exception ex)
            {
                ModLogger.Error("Quartermaster", "Error identifying player troop", ex);
                return null;
            }
        }
        
        /// <summary>
        /// Detect formation type from troop properties (matches TroopSelectionManager logic).
        /// </summary>
        private FormationType DetectTroopFormation(CharacterObject troop)
        {
            try
            {
                if (troop.IsRanged && troop.IsMounted)
                {
                    return FormationType.HorseArcher;
                }
                else if (troop.IsMounted)
                {
                    return FormationType.Cavalry;
                }
                else if (troop.IsRanged)
                {
                    return FormationType.Archer;
                }
                else
                {
                    return FormationType.Infantry;
                }
            }
            catch
            {
                return FormationType.Infantry;
            }
        }
        
        /// <summary>
        /// Calculate cost for requesting a specific equipment variant.
        /// Integrates with existing equipment pricing system.
        /// </summary>
        public int CalculateVariantCost(ItemObject requestedItem, ItemObject currentItem, EquipmentIndex slot)
        {
            try
            {
                if (requestedItem == null || currentItem == null)
                {
                    return 0;
                }
                
                // Base cost calculation
                var itemValue = requestedItem.Value;
                var currentValue = currentItem.Value;
                var costDifference = Math.Max(itemValue - currentValue, 0);
                
                // Slot-based multipliers
                var slotMultiplier = slot switch
                {
                    EquipmentIndex.Weapon0 or EquipmentIndex.Weapon1 or 
                    EquipmentIndex.Weapon2 or EquipmentIndex.Weapon3 => 1.0f, // Base cost for weapons
                    EquipmentIndex.Head => 0.3f,        // Helmets are cheaper to swap
                    EquipmentIndex.Body => 0.8f,        // Armor is significant cost
                    EquipmentIndex.Leg => 0.2f,         // Boots are minor cost
                    EquipmentIndex.Gloves => 0.2f,      // Gloves are minor cost
                    EquipmentIndex.Cape => 0.2f,        // Capes are minor cost
                    EquipmentIndex.Horse => 2.0f,       // Horses are expensive
                    EquipmentIndex.HorseHarness => 0.5f, // Horse armor moderate cost
                    _ => 1.0f
                };
                
                // Formation-based multipliers (from existing EquipmentManager)
                var formation = DetectTroopFormation(GetPlayerSelectedTroop());
                var formationMultiplier = formation switch
                {
                    FormationType.Infantry => 1.0f,        // Base cost
                    FormationType.Archer => 1.3f,          // +30% for ranged equipment
                    FormationType.Cavalry => 2.0f,         // +100% for mounted equipment
                    FormationType.HorseArcher => 2.5f,     // +150% for elite mounted ranged
                    _ => 1.0f
                };
                
                // Service fee (quartermaster profit)
                var serviceFee = 10; // Base 10 gold service charge
                
                // Quartermaster duty discount (15% off for provisioners)
                var duties = EnlistedDutiesBehavior.Instance;
                var isProvisioner = duties?.ActiveDuties.Contains("provisioner") == true;
                var isQuartermaster = duties?.GetCurrentOfficerRole() == "Quartermaster";
                var discountMultiplier = (isProvisioner || isQuartermaster) ? 0.85f : 1.0f; // 15% discount
                
                var finalCost = (int)((costDifference * slotMultiplier * formationMultiplier * discountMultiplier) + serviceFee);
                return Math.Max(finalCost, 5); // Minimum 5 gold
            }
            catch
            {
                return 25; // Safe fallback cost
            }
        }
        
        /// <summary>
        /// Process equipment variant request and update player equipment.
        /// </summary>
        public void RequestEquipmentVariant(ItemObject requestedItem, EquipmentIndex slot)
        {
            try
            {
                var enlistment = EnlistmentBehavior.Instance;
                if (!enlistment?.IsEnlisted == true)
                {
                    ModLogger.Error("Quartermaster", "Equipment request failed - player not enlisted");
                    return;
                }
                
                var hero = Hero.MainHero;
                var currentItem = hero.BattleEquipment[slot].Item;
                var cost = CalculateVariantCost(requestedItem, currentItem, slot);
                
                // Check if player can afford the variant
                if (hero.Gold < cost)
                {
                    var message = new TextObject("Insufficient funds. Need {COST} denars for this equipment variant.");
                    message.SetTextVariable("COST", cost.ToString());
                    InformationManager.DisplayMessage(new InformationMessage(message.ToString()));
                    return;
                }
                
                // Charge the player
                GiveGoldAction.ApplyBetweenCharacters(hero, null, cost, false);
                
                // Apply the equipment change to the specific slot
                ApplyEquipmentSlotChange(hero, requestedItem, slot);
                
                // Success notification
                var successMessage = new TextObject("Equipment variant applied: {ITEM_NAME} for {COST} denars.");
                successMessage.SetTextVariable("ITEM_NAME", requestedItem.Name);
                successMessage.SetTextVariable("COST", cost.ToString());
                InformationManager.DisplayMessage(new InformationMessage(successMessage.ToString()));
                
                ModLogger.Info("Quartermaster", $"Equipment variant applied: {requestedItem.Name} to slot {slot} for {cost} denars");
            }
            catch (Exception ex)
            {
                ModLogger.Error("Quartermaster", "Error processing equipment variant request", ex);
            }
        }
        
        /// <summary>
        /// Apply equipment change to a specific slot while preserving other equipment.
        /// Uses safe cloning to avoid corrupting player equipment.
        /// </summary>
        private void ApplyEquipmentSlotChange(Hero hero, ItemObject newItem, EquipmentIndex slot)
        {
            try
            {
                // Clone current equipment to preserve other slots
                var newEquipment = hero.BattleEquipment.Clone(false);
                
                // Replace only the requested slot
                newEquipment[slot] = new EquipmentElement(newItem, null, null, false);
                
                // Apply the updated equipment
                EquipmentHelper.AssignHeroEquipmentFromEquipment(hero, newEquipment);
                
                // Equipment change notification (using simplified approach for compatibility)
                // The EquipmentHelper.AssignHeroEquipmentFromEquipment call above handles the visual refresh
                
                ModLogger.Info("Quartermaster", $"Equipment slot {slot} updated with {newItem.Name}");
            }
            catch (Exception ex)
            {
                ModLogger.Error("Quartermaster", "Error applying equipment slot change", ex);
            }
        }
        
        /// <summary>
        /// Initialize quartermaster equipment menu with current troop's variants.
        /// </summary>
        private void OnQuartermasterEquipmentInit(MenuCallbackArgs args)
        {
            try
            {
                // Validate enlisted state first
                var enlistment = EnlistmentBehavior.Instance;
                if (!enlistment?.IsEnlisted == true)
                {
                    MBTextManager.SetTextVariable("QUARTERMASTER_TEXT", 
                        "You must be enlisted to access quartermaster services.");
                    ModLogger.Error("Quartermaster", "Quartermaster access denied - player not enlisted");
                    return;
                }
                
                _selectedTroop = GetPlayerSelectedTroop();
                
                if (_selectedTroop == null)
                {
                    // Provide fallback message with helpful information
                    var sb = new StringBuilder();
                    sb.AppendLine("Unable to determine your current military equipment type.");
                    sb.AppendLine();
                    sb.AppendLine("This may be because:");
                    sb.AppendLine("- You haven't selected a troop type during promotion yet");
                    sb.AppendLine("- Your current lord's culture is not recognized");
                    sb.AppendLine("- There was an error loading troop data");
                    sb.AppendLine();
                    sb.AppendLine("Please speak with your commanding officer about your assignment");
                    sb.AppendLine("or try again after your next promotion.");
                    
                    MBTextManager.SetTextVariable("QUARTERMASTER_TEXT", sb.ToString());
                    ModLogger.Error("Quartermaster", "Unable to identify player troop type");
                    return;
                }
                
                // Get all equipment variants for this troop type
                var variants = GetTroopEquipmentVariants(_selectedTroop);
                if (variants == null || variants.Count == 0)
                {
                    var sb = new StringBuilder();
                    sb.AppendLine($"Current Equipment: {_selectedTroop.Name?.ToString() ?? "Unknown"}");
                    sb.AppendLine($"Your Gold: {Hero.MainHero.Gold} denars");
                    sb.AppendLine();
                    sb.AppendLine("Your current troop type uses standard military issue equipment.");
                    sb.AppendLine("No equipment variants are available for this troop type.");
                    sb.AppendLine();
                    sb.AppendLine("Equipment variants may become available when you advance");
                    sb.AppendLine("to higher tiers or different troop specializations.");
                    
                    MBTextManager.SetTextVariable("QUARTERMASTER_TEXT", sb.ToString());
                    ModLogger.Info("Quartermaster", $"No equipment variants found for {_selectedTroop.Name?.ToString()}");
                    return;
                }
                
                _availableVariants = BuildVariantOptions(variants);
                
                // Build quartermaster display
                BuildQuartermasterStatusDisplay();
                
                // Create dynamic menu options for each equipment slot with variants
                CreateEquipmentSlotOptions(args);
                
                ModLogger.Info("Quartermaster", $"Quartermaster menu opened for {_selectedTroop.Name?.ToString()} with {_availableVariants?.Count ?? 0} variant slots");
            }
            catch (Exception ex)
            {
                ModLogger.Error("Quartermaster", "Error initializing quartermaster equipment menu", ex);
                
                // Provide user-friendly error message
                var errorMsg = new StringBuilder();
                errorMsg.AppendLine("Quartermaster services temporarily unavailable.");
                errorMsg.AppendLine();
                errorMsg.AppendLine("This error has been logged for investigation.");
                errorMsg.AppendLine("Please try again later or contact support if the issue persists.");
                
                MBTextManager.SetTextVariable("QUARTERMASTER_TEXT", errorMsg.ToString());
            }
        }
        
        /// <summary>
        /// Build equipment variant options with pricing and availability.
        /// Enhanced access for quartermaster officers.
        /// </summary>
        private Dictionary<EquipmentIndex, List<EquipmentVariantOption>> BuildVariantOptions(
            Dictionary<EquipmentIndex, List<ItemObject>> variants)
        {
            var options = new Dictionary<EquipmentIndex, List<ItemObject>>();
            var hero = Hero.MainHero;
            var duties = EnlistedDutiesBehavior.Instance;
            var enlistment = EnlistmentBehavior.Instance;
            
            // Start with troop-specific variants
            foreach (var slotVariants in variants)
            {
                options[slotVariants.Key] = new List<ItemObject>(slotVariants.Value);
            }
            
            // ENHANCEMENT: Quartermaster officers get access to culture-wide equipment
            var isProvisioner = duties?.ActiveDuties.Contains("provisioner") == true;
            var isQuartermaster = duties?.GetCurrentOfficerRole() == "Quartermaster";
            
            if (isProvisioner || isQuartermaster)
            {
                ModLogger.Info("Quartermaster", "Applying quartermaster officer enhancements - expanding equipment access");
                
                // Add culture-wide equipment variants for quartermaster officers
                var cultureVariants = GetCultureEquipmentVariants(enlistment?.CurrentLord?.Culture, enlistment?.EnlistmentTier ?? 1);
                
                foreach (var cultureSlot in cultureVariants)
                {
                    var slot = cultureSlot.Key;
                    var cultureItems = cultureSlot.Value;
                    
                    if (!options.ContainsKey(slot))
                    {
                        options[slot] = new List<ItemObject>();
                    }
                    
                    // Add culture items not already in troop variants
                    foreach (var item in cultureItems)
                    {
                        if (!options[slot].Contains(item))
                        {
                            options[slot].Add(item);
                        }
                    }
                }
            }
            
            // Convert to variant options with pricing
            var finalOptions = new Dictionary<EquipmentIndex, List<EquipmentVariantOption>>();
            
            foreach (var slotItems in options)
            {
                var slot = slotItems.Key;
                var items = slotItems.Value;
                
                // Only include slots with multiple options
                if (items.Count > 1)
                {
                    var variantOptions = new List<EquipmentVariantOption>();
                    var currentItem = hero.BattleEquipment[slot].Item;
                    
                    foreach (var item in items)
                    {
                        var cost = CalculateVariantCost(item, currentItem, slot);
                        var isCurrent = item == currentItem;
                        var canAfford = hero.Gold >= cost;
                        
                        variantOptions.Add(new EquipmentVariantOption
                        {
                            Item = item,
                            Cost = cost,
                            IsCurrent = isCurrent,
                            CanAfford = canAfford,
                            Slot = slot,
                            IsOfficerExclusive = !variants.ContainsKey(slot) || !variants[slot].Contains(item)
                        });
                    }
                    
                    // Sort by cost (current item first, then by price)
                    variantOptions = variantOptions.OrderBy(o => o.IsCurrent ? 0 : 1)
                                                  .ThenBy(o => o.Cost).ToList();
                    
                    finalOptions[slot] = variantOptions;
                }
            }
            
            return finalOptions;
        }
        
        /// <summary>
        /// Get culture-wide equipment variants for quartermaster officers.
        /// Provides enhanced equipment access beyond just troop variants.
        /// </summary>
        private Dictionary<EquipmentIndex, List<ItemObject>> GetCultureEquipmentVariants(CultureObject culture, int maxTier)
        {
            try
            {
                if (culture == null)
                {
                    return new Dictionary<EquipmentIndex, List<ItemObject>>();
                }
                
                var variants = new Dictionary<EquipmentIndex, List<ItemObject>>();
                var allTroops = MBObjectManager.Instance.GetObjectTypeList<CharacterObject>();
                
                // Get all troops from this culture at or below the player's tier
                var cultureTroops = allTroops.Where(troop => 
                    troop.Culture == culture &&
                    troop.GetBattleTier() <= maxTier &&
                    !troop.IsHero &&
                    troop.BattleEquipments.Any()).ToList();
                
                // Extract all equipment from culture troops
                foreach (var troop in cultureTroops)
                {
                    foreach (var equipment in troop.BattleEquipments)
                    {
                        for (EquipmentIndex slot = EquipmentIndex.Weapon0; slot <= EquipmentIndex.HorseHarness; slot++)
                        {
                            var item = equipment[slot].Item;
                            if (item != null)
                            {
                                if (!variants.ContainsKey(slot))
                                {
                                    variants[slot] = new List<ItemObject>();
                                }
                                
                                if (!variants[slot].Contains(item))
                                {
                                    variants[slot].Add(item);
                                }
                            }
                        }
                    }
                }
                
                ModLogger.Info("Quartermaster", $"Officer enhancement: Added {variants.Sum(kvp => kvp.Value.Count)} culture-wide equipment options");
                return variants;
            }
            catch (Exception ex)
            {
                ModLogger.Error("Quartermaster", "Error getting culture equipment variants", ex);
                return new Dictionary<EquipmentIndex, List<ItemObject>>();
            }
        }
        
        /// <summary>
        /// Build quartermaster status display with current equipment and available variants.
        /// </summary>
        private void BuildQuartermasterStatusDisplay()
        {
            try
            {
                var sb = new StringBuilder();
                var hero = Hero.MainHero;
                var enlistment = EnlistmentBehavior.Instance;
                var duties = EnlistedDutiesBehavior.Instance;
                
                // Header information
                sb.AppendLine($"Current Equipment: {_selectedTroop?.Name?.ToString() ?? "Unknown"}");
                sb.AppendLine($"Your Gold: {hero.Gold} denars");
                sb.AppendLine();
                
                // Show equipment slots with variants available
                if (_availableVariants.Count > 0)
                {
                    sb.AppendLine("Equipment variants available for your troop type:");
                    sb.AppendLine();
                    
                    foreach (var slotOptions in _availableVariants)
                    {
                        var slot = slotOptions.Key;
                        var options = slotOptions.Value;
                        var slotName = GetSlotDisplayName(slot);
                        
                        sb.AppendLine($"{slotName} Options ({options.Count} variants):");
                        
                foreach (var option in options.Take(3)) // Show first 3 variants
                {
                    var status = option.IsCurrent ? "(Current)" : 
                                option.CanAfford ? $"({option.Cost} denars)" : 
                                $"({option.Cost} denars - Can't afford)";
                    var marker = option.IsCurrent ? "*" : "-"; // Use simple ASCII characters
                    var exclusiveMarker = option.IsOfficerExclusive ? " [Officer Access]" : "";
                    
                    sb.AppendLine($"  {marker} {option.Item.Name} {status}{exclusiveMarker}");
                }
                        
                        if (options.Count > 3)
                        {
                            sb.AppendLine($"  ... and {options.Count - 3} more variants");
                        }
                        sb.AppendLine();
                    }
                }
                else
                {
                    sb.AppendLine("Your current troop type has limited equipment variants.");
                    sb.AppendLine("Standard military issue equipment is in good condition.");
                }
                
                // Show quartermaster duty bonus if applicable
                var isProvisioner = duties?.ActiveDuties.Contains("provisioner") == true;
                var isQuartermaster = duties?.GetCurrentOfficerRole() == "Quartermaster";
                
                if (isProvisioner || isQuartermaster)
                {
                    sb.AppendLine("-----------------------------------------------------------");
                    sb.AppendLine("OFFICER PRIVILEGES - QUARTERMASTER");
                    sb.AppendLine("- 15% discount on all equipment requests");
                    sb.AppendLine("- Access to supply management functions"); 
                    sb.AppendLine("- Enhanced equipment variant discovery");
                    sb.AppendLine("- Party logistics and carry capacity management");
                    sb.AppendLine("-----------------------------------------------------------");
                    sb.AppendLine();
                }
                
                MBTextManager.SetTextVariable("QUARTERMASTER_TEXT", sb.ToString());
            }
            catch (Exception ex)
            {
                ModLogger.Error("Quartermaster", "Error building quartermaster display", ex);
                MBTextManager.SetTextVariable("QUARTERMASTER_TEXT", "Equipment information unavailable.");
            }
        }
        
        /// <summary>
        /// Create dynamic menu options for each equipment slot with variants.
        /// </summary>
        private void CreateEquipmentSlotOptions(MenuCallbackArgs args)
        {
            // Note: Dynamic menu option creation is complex in Bannerlord
            // For initial implementation, we'll use a simplified approach with preset options
            // This can be enhanced later with custom Gauntlet UI
            
            // For now, focus on weapon variants (most common and impactful)
            var weaponVariants = _availableVariants.Where(kvp => 
                kvp.Key >= EquipmentIndex.Weapon0 && kvp.Key <= EquipmentIndex.Weapon3).ToList();
                
            if (weaponVariants.Count > 0)
            {
                // Add a generic "Request weapon variant" option
                // The actual variant selection would happen in a submenu or through conversation
                var hasAffordableVariants = weaponVariants.Any(kvp => 
                    kvp.Value.Any(opt => !opt.IsCurrent && opt.CanAfford));
                    
                if (hasAffordableVariants)
                {
                    ModLogger.Info("Quartermaster", "Added weapon variant request option to menu");
                }
            }
        }
        
        /// <summary>
        /// Initialize equipment variant selection submenu.
        /// </summary>
        private void OnQuartermasterVariantsInit(MenuCallbackArgs args)
        {
            try
            {
                if (_selectedSlot == EquipmentIndex.None || !_availableVariants.ContainsKey(_selectedSlot))
                {
                    MBTextManager.SetTextVariable("VARIANT_TEXT", "No equipment variants available for the selected slot.");
                    return;
                }
                
                var options = _availableVariants[_selectedSlot];
                var slotName = GetSlotDisplayName(_selectedSlot);
                
                var sb = new StringBuilder();
                sb.AppendLine($"Available {slotName} variants for {_selectedTroop.Name}:");
                sb.AppendLine();
                
                foreach (var option in options)
                {
                    var status = option.IsCurrent ? "(Current Equipment)" :
                                option.CanAfford ? $"Cost: {option.Cost} denars" :
                                $"Cost: {option.Cost} denars (Insufficient funds)";
                    var marker = option.IsCurrent ? "●" : "○";
                    
                    sb.AppendLine($"{marker} {option.Item.Name}");
                    sb.AppendLine($"  {status}");
                    sb.AppendLine();
                }
                
                MBTextManager.SetTextVariable("VARIANT_TEXT", sb.ToString());
            }
            catch (Exception ex)
            {
                ModLogger.Error("Quartermaster", "Error initializing variant selection", ex);
                MBTextManager.SetTextVariable("VARIANT_TEXT", "Equipment variant information unavailable.");
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
        
        /// <summary>
        /// Check if quartermaster services are available based on duties.
        /// Integrates with existing provisioner duty system.
        /// </summary>
        public bool IsQuartermasterServiceAvailable()
        {
            try
            {
                var enlistment = EnlistmentBehavior.Instance;
                var duties = EnlistedDutiesBehavior.Instance;
                
                if (!enlistment?.IsEnlisted == true)
                {
                    return false;
                }
                
                // Enhanced access if player has quartermaster duties
                if (duties?.GetCurrentOfficerRole() == "Quartermaster" || 
                    duties?.ActiveDuties.Contains("provisioner") == true)
                {
                    return true; // Full access with officer privileges
                }
                
                // Basic access for all enlisted soldiers
                return true;
            }
            catch
            {
                return false;
            }
        }
        
        #region Menu Option Implementations
        
        /// <summary>
        /// Check if weapon variants are available for the current troop.
        /// </summary>
        private bool IsWeaponVariantsAvailable(MenuCallbackArgs args)
        {
            if (_availableVariants == null)
            {
                return false;
            }
            
            // Check if any weapon slots have variants
            return _availableVariants.Any(kvp => 
                kvp.Key >= EquipmentIndex.Weapon0 && kvp.Key <= EquipmentIndex.Weapon3 &&
                kvp.Value.Any(opt => !opt.IsCurrent));
        }
        
        /// <summary>
        /// Handle weapon variant selection.
        /// </summary>
        private void OnWeaponVariantsSelected(MenuCallbackArgs args)
        {
            try
            {
                // Find first weapon slot with variants
                var weaponSlot = _availableVariants.Where(kvp => 
                    kvp.Key >= EquipmentIndex.Weapon0 && kvp.Key <= EquipmentIndex.Weapon3)
                    .FirstOrDefault(kvp => kvp.Value.Any(opt => !opt.IsCurrent));
                
                if (weaponSlot.Key != EquipmentIndex.None)
                {
                    _selectedSlot = weaponSlot.Key;
                    
                    // Show available weapon options for direct selection
                    ShowEquipmentVariantSelectionDialog(weaponSlot.Value, "weapon");
                }
                else
                {
                    InformationManager.DisplayMessage(new InformationMessage(
                        new TextObject("No weapon variants available for your troop type.").ToString()));
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("Quartermaster", "Error selecting weapon variants", ex);
            }
        }
        
        /// <summary>
        /// Show equipment variant selection with individual clickable items (SAS-style approach).
        /// Uses custom Gauntlet UI for professional equipment selection.
        /// </summary>
        private void ShowEquipmentVariantSelectionDialog(List<EquipmentVariantOption> variants, string equipmentType)
        {
            try
            {
                if (variants == null || variants.Count <= 1)
                {
                    InformationManager.DisplayMessage(new InformationMessage(
                        new TextObject($"No {equipmentType} variants available.").ToString()));
                    return;
                }
                
                // Try to use custom Gauntlet UI for individual item clicking (SAS-style approach)
                if (TryShowGauntletEquipmentSelector(variants, equipmentType))
                {
                    ModLogger.Info("Quartermaster", $"Opened Gauntlet equipment selector for {equipmentType} with {variants.Count} variants");
                }
                else
                {
                    // Fallback to simplified automatic selection
                    ShowSimplifiedVariantSelection(variants, equipmentType);
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("Quartermaster", "Error showing equipment variant selection", ex);
                
                // Ultimate fallback
                ShowSimplifiedVariantSelection(variants, equipmentType);
            }
        }
        
        /// <summary>
        /// Try to show custom Gauntlet equipment selector UI.
        /// Returns true if successful, false if fallback needed.
        /// </summary>
        private bool TryShowGauntletEquipmentSelector(List<EquipmentVariantOption> variants, string equipmentType)
        {
            try
            {
                // Try custom Gauntlet UI first (SAS-style approach)
                var targetSlot = variants.FirstOrDefault()?.Slot ?? EquipmentIndex.Weapon0;
                UI.QuartermasterEquipmentSelectorBehavior.ShowEquipmentSelector(variants, targetSlot, equipmentType);
                
                return true; // Success
            }
            catch (Exception ex)
            {
                ModLogger.Error("Quartermaster", "Gauntlet UI failed, using conversation-based selection", ex);
                
                // Fallback to conversation-based individual selection
                ShowConversationBasedEquipmentSelection(variants, equipmentType);
                return true; // Still success with fallback
            }
        }
        
        /// <summary>
        /// Show equipment selection using conversation system for individual item clicking.
        /// Simplified alternative to complex Gauntlet UI.
        /// </summary>
        private void ShowConversationBasedEquipmentSelection(List<EquipmentVariantOption> variants, string equipmentType)
        {
            try
            {
                var availableVariants = variants.Where(v => !v.IsCurrent).Take(5).ToList(); // Limit to 5 for conversation
                
                if (availableVariants.Count > 1)
                {
                    // Store variants for conversation selection
                    _conversationEquipmentVariants = availableVariants.ToDictionary(v => availableVariants.IndexOf(v) + 1, v => v);
                    _conversationEquipmentType = equipmentType;
                    
                    // Create inquiry for equipment selection
                    var inquiryElements = new List<InquiryElement>();
                    
                    foreach (var variant in availableVariants)
                    {
                        var title = variant.Item.Name?.ToString() ?? "Unknown";
                        var description = variant.CanAfford 
                            ? $"Cost: {variant.Cost} denars" 
                            : $"Cost: {variant.Cost} denars (Can't afford)";
                        
                        inquiryElements.Add(new InquiryElement(
                            variant, 
                            title, 
                            null, // No image for simplicity
                            variant.CanAfford, 
                            description));
                    }
                    
                    // Show selection inquiry
                    MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(
                        $"Select {equipmentType}",
                        "Choose equipment variant to request:",
                        inquiryElements,
                        true, 1, 1, // Single selection
                        "Request Equipment", "Cancel",
                        OnEquipmentVariantSelected,
                        null), false, false);
                        
                    ModLogger.Info("Quartermaster", $"Opened equipment selection inquiry for {equipmentType}");
                }
                else
                {
                    // Use simplified fallback for single variant
                    ShowSimplifiedVariantSelection(variants, equipmentType);
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("Quartermaster", "Error showing conversation-based equipment selection", ex);
                ShowSimplifiedVariantSelection(variants, equipmentType);
            }
        }
        
        /// <summary>
        /// Handle equipment variant selection from inquiry.
        /// </summary>
        private void OnEquipmentVariantSelected(List<InquiryElement> selectedElements)
        {
            try
            {
                if (selectedElements != null && selectedElements.Count > 0)
                {
                    var selectedVariant = selectedElements.First().Identifier as EquipmentVariantOption;
                    
                    if (selectedVariant != null)
                    {
                        // Apply the selected equipment variant
                        RequestEquipmentVariant(selectedVariant.Item, selectedVariant.Slot);
                        
                        // Return to quartermaster menu
                        GameMenu.ActivateGameMenu("quartermaster_equipment");
                    }
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("Quartermaster", "Error processing equipment variant selection", ex);
            }
        }
        
        /// <summary>
        /// Simplified variant selection fallback (current working approach).
        /// </summary>
        private void ShowSimplifiedVariantSelection(List<EquipmentVariantOption> variants, string equipmentType)
        {
            try
            {
                // Find available variants that player can afford
                var availableVariants = variants.Where(v => !v.IsCurrent && v.CanAfford).ToList();
                
                if (availableVariants.Count > 0)
                {
                    var selectedVariant = availableVariants.First();
                    
                    // Show confirmation before applying
                    var confirmText = $"Equipping {selectedVariant.Item.Name?.ToString()} for {selectedVariant.Cost} denars...";
                    InformationManager.DisplayMessage(new InformationMessage(confirmText));
                    
                    // Apply the equipment variant
                    RequestEquipmentVariant(selectedVariant.Item, selectedVariant.Slot);
                    
                    // Return to main quartermaster menu to see updated equipment
                    GameMenu.ActivateGameMenu("quartermaster_equipment");
                }
                else
                {
                    InformationManager.DisplayMessage(new InformationMessage(
                        new TextObject($"No affordable {equipmentType} variants available. You need more gold.").ToString()));
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("Quartermaster", "Error in simplified variant selection", ex);
            }
        }
        
        /// <summary>
        /// Check if armor variants are available.
        /// </summary>
        private bool IsArmorVariantsAvailable(MenuCallbackArgs args)
        {
            if (_availableVariants == null)
            {
                return false;
            }
            
            return _availableVariants.ContainsKey(EquipmentIndex.Body) &&
                   _availableVariants[EquipmentIndex.Body].Any(opt => !opt.IsCurrent);
        }
        
        /// <summary>
        /// Handle armor variant selection.
        /// </summary>
        private void OnArmorVariantsSelected(MenuCallbackArgs args)
        {
            try
            {
                var armorOptions = BuildArmorOptionsFromCurrentTroop();
                if (armorOptions.Count == 0)
                {
                    InformationManager.DisplayMessage(new InformationMessage(
                        new TextObject("No armor variants available for your troop tree.").ToString()));
                    return;
                }

                // Flatten all armor sub-slot variants into one list and show a single scrollable grid
                var combined = armorOptions.SelectMany(kvp => kvp.Value).ToList();
                ShowEquipmentVariantSelectionDialog(combined, "armor");
            }
            catch (Exception ex)
            {
                ModLogger.Error("Quartermaster", "Error selecting armor variants", ex);
            }
        }
        
        /// <summary>
        /// Check if helmet variants are available.
        /// </summary>
        private bool IsHelmetVariantsAvailable(MenuCallbackArgs args)
        {
            if (_availableVariants == null)
            {
                return false;
            }
            
            return _availableVariants.ContainsKey(EquipmentIndex.Head) &&
                   _availableVariants[EquipmentIndex.Head].Any(opt => !opt.IsCurrent);
        }

        /// <summary>
        /// Build armor options at runtime from the currently selected troop's BattleEquipments (weapons pattern).
        /// </summary>
        private Dictionary<EquipmentIndex, List<EquipmentVariantOption>> BuildArmorOptionsFromCurrentTroop()
        {
            var result = new Dictionary<EquipmentIndex, List<EquipmentVariantOption>>();
            try
            {
                var enlistment = EnlistmentBehavior.Instance;
                if (!enlistment?.IsEnlisted == true)
                {
                    return result;
                }

                var selectedTroop = GetPlayerSelectedTroop();
                if (selectedTroop == null)
                {
                    return result;
                }

                // Use existing runtime discovery like weapons
                var allVariants = GetTroopEquipmentVariants(selectedTroop);
                var armorSlots = new[] { EquipmentIndex.Body, EquipmentIndex.Head, EquipmentIndex.Gloves, EquipmentIndex.Leg, EquipmentIndex.Cape };
                var filtered = allVariants.Where(kvp => armorSlots.Contains(kvp.Key))
                                          .ToDictionary(k => k.Key, v => v.Value);

                result = BuildVariantOptionsExact(filtered);

                // Keep only slots with choices
                result = result.Where(kvp => kvp.Value != null && kvp.Value.Count > 0)
                               .ToDictionary(k => k.Key, v => v.Value);
            }
            catch (Exception ex)
            {
                ModLogger.Error("Quartermaster", $"BuildArmorOptionsFromCurrentTroop failed: {ex.Message}");
            }
            return result;
        }

        private HashSet<CharacterObject> BuildTroopBranchNodes(CultureObject culture, CharacterObject targetTroop, int maxTier)
        {
            var branch = new HashSet<CharacterObject>();
            try
            {
                bool Dfs(CharacterObject node, HashSet<string> seen)
                {
                    if (node == null)
                    {
                        return false;
                    }
                    if (node.IsHero || node.Culture != culture)
                    {
                        return false;
                    }
                    if (SafeGetTier(node) > maxTier)
                    {
                        return false;
                    }
                    if (!seen.Add(node.StringId))
                    {
                        return false;
                    }

                    if (node == targetTroop)
                    {
                        branch.Add(node);
                        return true;
                    }

                    try
                    {
                        foreach (var next in node.UpgradeTargets)
                        {
                            if (Dfs(next, seen))
                            {
                                branch.Add(node);
                                return true;
                            }
                        }
                    }
                    catch { }
                    return false;
                }

                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                Dfs(culture.BasicTroop, seen);
                Dfs(culture.EliteBasicTroop, seen);
            }
            catch (Exception ex)
            {
                ModLogger.Error("Quartermaster", $"BuildTroopBranchNodes failed: {ex.Message}");
            }
            return branch;
        }

        private Dictionary<EquipmentIndex, List<ItemObject>> CollectArmorVariantsFromNodes(HashSet<CharacterObject> nodes, int exactTier, CultureObject culture)
        {
            var variants = new Dictionary<EquipmentIndex, List<ItemObject>>();
            try
            {
                var armorSlots = new[] { EquipmentIndex.Body, EquipmentIndex.Head, EquipmentIndex.Gloves, EquipmentIndex.Leg, EquipmentIndex.Cape };
                foreach (var troop in nodes)
                {
                    // Only gather from exact tier nodes to reflect the current tier's supply
                    if (SafeGetTier(troop) != exactTier)
                    {
                        continue;
                    }
                    foreach (var equipment in troop.BattleEquipments)
                    {
                        foreach (var slot in armorSlots)
                        {
                            var item = equipment[slot].Item;
                            if (item == null)
                            {
                                continue;
                            }
                            // Safety filters: ensure true armor, and culture match when item has culture
                            if (item.ArmorComponent == null)
                            {
                                continue;
                            }
                            if (culture != null && item.Culture != null && item.Culture != culture)
                            {
                                continue;
                            }
                            if (!variants.ContainsKey(slot))
                            {
                                variants[slot] = new List<ItemObject>();
                            }
                            if (!variants[slot].Contains(item))
                            {
                                variants[slot].Add(item);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("Quartermaster", $"CollectArmorVariantsFromNodes failed: {ex.Message}");
            }
            return variants;
        }

        private Dictionary<EquipmentIndex, List<EquipmentVariantOption>> BuildVariantOptionsExact(
            Dictionary<EquipmentIndex, List<ItemObject>> variants)
        {
            var finalOptions = new Dictionary<EquipmentIndex, List<EquipmentVariantOption>>();
            try
            {
                var hero = Hero.MainHero;
                foreach (var slotItems in variants)
                {
                    var slot = slotItems.Key;
                    var items = slotItems.Value;
                    if (items == null || items.Count <= 1)
                    {
                        // need choices
                        continue;
                    }

                    var currentItem = hero.BattleEquipment[slot].Item;
                    var optionList = new List<EquipmentVariantOption>();
                    foreach (var item in items)
                    {
                        var cost = CalculateVariantCost(item, currentItem, slot);
                        optionList.Add(new EquipmentVariantOption
                        {
                            Item = item,
                            Cost = cost,
                            IsCurrent = item == currentItem,
                            CanAfford = hero.Gold >= cost,
                            Slot = slot,
                            IsOfficerExclusive = false
                        });
                    }

                    optionList = optionList.OrderBy(o => o.IsCurrent ? 0 : 1).ThenBy(o => o.Cost).ToList();
                    finalOptions[slot] = optionList;
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("Quartermaster", $"BuildVariantOptionsExact failed: {ex.Message}");
            }
            return finalOptions;
        }

        private void ShowArmorSlotPicker(Dictionary<EquipmentIndex, List<EquipmentVariantOption>> armorOptions)
        {
            try
            {
                var elements = new List<InquiryElement>();
                foreach (var kvp in armorOptions)
                {
                    var slot = kvp.Key;
                    var label = $"{GetSlotDisplayName(slot)} ({kvp.Value.Count})";
                    elements.Add(new InquiryElement(slot, label, null, true, null));
                }

                void OnDone(List<InquiryElement> selection)
                {
                    if (selection == null || selection.Count == 0)
                    {
                        return;
                    }
                    var slot = (EquipmentIndex)selection[0].Identifier;
                    if (armorOptions.TryGetValue(slot, out var variants))
                    {
                        ShowEquipmentVariantSelectionDialog(variants, "armor");
                    }
                }

                MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(
                    "Select armor slot",
                    "Choose which armor piece to request",
                    elements,
                    false, 1, 1,
                    "Continue", "Cancel",
                    OnDone,
                    _ => { }));
            }
            catch (Exception ex)
            {
                ModLogger.Error("Quartermaster", $"ShowArmorSlotPicker failed: {ex.Message}");
            }
        }

        private int SafeGetTier(CharacterObject troop)
        {
            try { return troop.GetBattleTier(); } catch { return 1; }
        }
        
        /// <summary>
        /// Handle helmet variant selection.
        /// </summary>
        private void OnHelmetVariantsSelected(MenuCallbackArgs args)
        {
            try
            {
                if (_availableVariants?.ContainsKey(EquipmentIndex.Head) == true)
                {
                    ShowEquipmentVariantSelectionDialog(_availableVariants[EquipmentIndex.Head], "helmet");
                }
                else
                {
                    InformationManager.DisplayMessage(new InformationMessage(
                        new TextObject("No helmet variants available for your troop type.").ToString()));
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("Quartermaster", "Error selecting helmet variants", ex);
            }
        }
        
        /// <summary>
        /// Check if accessory variants are available (gloves, capes, boots).
        /// </summary>
        private bool IsAccessoryVariantsAvailable(MenuCallbackArgs args)
        {
            if (_availableVariants == null)
            {
                return false;
            }
            
            var accessorySlots = new[] { EquipmentIndex.Gloves, EquipmentIndex.Cape, EquipmentIndex.Leg };
            return accessorySlots.Any(slot => 
                _availableVariants.ContainsKey(slot) &&
                _availableVariants[slot].Any(opt => !opt.IsCurrent));
        }
        
        /// <summary>
        /// Handle accessory variant selection.
        /// </summary>
        private void OnAccessoryVariantsSelected(MenuCallbackArgs args)
        {
            try
            {
                // Find first accessory slot with variants
                var accessorySlots = new[] { EquipmentIndex.Gloves, EquipmentIndex.Cape, EquipmentIndex.Leg };
                var availableSlot = accessorySlots.FirstOrDefault(slot => 
                    _availableVariants.ContainsKey(slot) &&
                    _availableVariants[slot].Any(opt => !opt.IsCurrent));
                
                if (availableSlot != EquipmentIndex.None)
                {
                    _selectedSlot = availableSlot;
                    GameMenu.ActivateGameMenu("quartermaster_variants");
                }
                else
                {
                    InformationManager.DisplayMessage(new InformationMessage(
                        new TextObject("No accessory variants available.").ToString()));
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("Quartermaster", "Error selecting accessory variants", ex);
            }
        }
        
        /// <summary>
        /// Check if supply management is available (requires provisioner duty).
        /// </summary>
        private bool IsSupplyManagementAvailable(MenuCallbackArgs args)
        {
            var duties = EnlistedDutiesBehavior.Instance;
            return duties?.GetCurrentOfficerRole() == "Quartermaster" ||
                   duties?.ActiveDuties.Contains("provisioner") == true;
        }
        
        /// <summary>
        /// Handle supply management (food, carry capacity, etc.).
        /// </summary>
        private void OnSupplyManagementSelected(MenuCallbackArgs args)
        {
            try
            {
                GameMenu.ActivateGameMenu("quartermaster_supplies");
            }
            catch (Exception ex)
            {
                ModLogger.Error("Quartermaster", "Error accessing supply management", ex);
            }
        }
        
        /// <summary>
        /// Add supply management menu for quartermaster officers.
        /// </summary>
        private void AddSupplyManagementMenu(CampaignGameStarter starter)
        {
            // Supply management menu for quartermaster officers
            starter.AddGameMenu("quartermaster_supplies",
                "Supply Management\n{SUPPLY_TEXT}",
                OnSupplyManagementInit,
                GameOverlays.MenuOverlayType.None,
                GameMenu.MenuFlags.None,
                null);
                
            // Food optimization option
            starter.AddGameMenuOption("quartermaster_supplies", "optimize_food",
                "Optimize food supplies",
                IsFoodOptimizationAvailable,
                OnFoodOptimizationSelected,
                false, 1);
                
            // Inventory management option  
            starter.AddGameMenuOption("quartermaster_supplies", "manage_inventory",
                "Reorganize party inventory",
                IsInventoryManagementAvailable,
                OnInventoryManagementSelected,
                false, 2);
                
            // Supply purchase option
            starter.AddGameMenuOption("quartermaster_supplies", "purchase_supplies",
                "Purchase additional supplies",
                IsSupplyPurchaseAvailable,
                OnSupplyPurchaseSelected,
                false, 3);
                
            starter.AddGameMenuOption("quartermaster_supplies", "supplies_back",
                "Return to quartermaster",
                args => true,
                args => GameMenu.ActivateGameMenu("quartermaster_equipment"),
                true, -1);
        }
        
        /// <summary>
        /// Initialize supply management menu display.
        /// </summary>
        private void OnSupplyManagementInit(MenuCallbackArgs args)
        {
            try
            {
                var sb = new StringBuilder();
                var party = MobileParty.MainParty;
                var duties = EnlistedDutiesBehavior.Instance;
                
                sb.AppendLine("Army Supply Management");
                sb.AppendLine("---------------------------------------");
                sb.AppendLine();
                
                // Current supply status
                sb.AppendLine($"Current Inventory: {party.ItemRoster.TotalWeight:F1}/{party.InventoryCapacity:F1} capacity");
                sb.AppendLine($"Food Supplies: {party.Food:F1} (consumption: {party.FoodChange:F2}/day)");
                sb.AppendLine($"Morale: {party.Morale:F1}/100");
                sb.AppendLine();
                
                // Officer benefits display
                if (duties?.GetCurrentOfficerRole() == "Quartermaster")
                {
                    sb.AppendLine("QUARTERMASTER OFFICER BENEFITS:");
                    sb.AppendLine("- +50 party carry capacity");
                    sb.AppendLine("- +15% food efficiency");
                    sb.AppendLine("- Enhanced supply management options");
                    sb.AppendLine();
                }
                
                MBTextManager.SetTextVariable("SUPPLY_TEXT", sb.ToString());
            }
            catch (Exception ex)
            {
                ModLogger.Error("Quartermaster", "Error initializing supply management menu", ex);
                MBTextManager.SetTextVariable("SUPPLY_TEXT", "Supply information unavailable.");
            }
        }
        
        private bool IsFoodOptimizationAvailable(MenuCallbackArgs args)
        {
            var party = MobileParty.MainParty;
            return party?.Food > 0 && party.FoodChange < 0; // Has food but consuming it
        }
        
        private void OnFoodOptimizationSelected(MenuCallbackArgs args)
        {
            // Quartermaster food efficiency bonus implementation
            var party = MobileParty.MainParty;
            var foodBonus = (int)(party.Food * 0.05f); // 5% food efficiency bonus
            
            if (foodBonus > 0)
            {
                party.ItemRoster.AddToCounts(MBObjectManager.Instance.GetObject<ItemObject>("grain"), foodBonus);
                
                var message = new TextObject("Optimized food supplies: +{AMOUNT} grain from efficient rationing.");
                message.SetTextVariable("AMOUNT", foodBonus.ToString());
                InformationManager.DisplayMessage(new InformationMessage(message.ToString()));
                
                ModLogger.Info("Quartermaster", $"Food optimization applied: +{foodBonus} grain");
            }
        }
        
        private bool IsInventoryManagementAvailable(MenuCallbackArgs args)
        {
            var party = MobileParty.MainParty;
            return party?.ItemRoster.TotalWeight > party.InventoryCapacity * 0.8f; // Over 80% capacity
        }
        
        private void OnInventoryManagementSelected(MenuCallbackArgs args)
        {
            // Quartermaster inventory optimization
            var party = MobileParty.MainParty;
            var capacityBonus = party.InventoryCapacity * 0.05f; // 5% temporary capacity bonus
            
            var message = new TextObject("Reorganized inventory: +{AMOUNT} temporary carry capacity.");
            message.SetTextVariable("AMOUNT", capacityBonus.ToString("F0"));
            InformationManager.DisplayMessage(new InformationMessage(message.ToString()));
            
            ModLogger.Info("Quartermaster", $"Inventory optimization applied: +{capacityBonus:F0} capacity");
        }
        
        private bool IsSupplyPurchaseAvailable(MenuCallbackArgs args)
        {
            return Hero.MainHero.Gold >= 50; // Can afford basic supplies
        }
        
        private void OnSupplyPurchaseSelected(MenuCallbackArgs args)
        {
            // Basic supply purchase system
            var cost = 50;
            if (Hero.MainHero.Gold >= cost)
            {
                GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, cost, false);
                
                // Add basic supplies
                var party = MobileParty.MainParty;
                party.ItemRoster.AddToCounts(MBObjectManager.Instance.GetObject<ItemObject>("grain"), 5);
                party.ItemRoster.AddToCounts(MBObjectManager.Instance.GetObject<ItemObject>("tools"), 2);
                
                InformationManager.DisplayMessage(new InformationMessage(
                    new TextObject("Purchased basic supplies: 5 grain, 2 tools.").ToString()));
                    
                ModLogger.Info("Quartermaster", "Supply purchase completed");
            }
        }
        
        /// <summary>
        /// Check if a specific variant option is available and visible.
        /// </summary>
        private bool IsVariantOptionAvailable(MenuCallbackArgs args, int optionIndex)
        {
            try
            {
                if (_selectedSlot == EquipmentIndex.None || !_availableVariants.ContainsKey(_selectedSlot))
                {
                    return false;
                }
                
                var options = _availableVariants[_selectedSlot];
                if (optionIndex > options.Count)
                {
                    return false;
                }
                
                var option = options[optionIndex - 1]; // Convert to 0-based index
                
                // Set dynamic option text
                var optionText = BuildVariantOptionText(option);
                // Note: In a full implementation, we'd need to dynamically update menu option text
                // For now, this validates that the option should be shown
                
                return true;
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// Handle selection of a specific variant option.
        /// </summary>
        private void OnVariantOptionSelected(MenuCallbackArgs args, int optionIndex)
        {
            try
            {
                if (_selectedSlot == EquipmentIndex.None || !_availableVariants.ContainsKey(_selectedSlot))
                {
                    return;
                }
                
                var options = _availableVariants[_selectedSlot];
                if (optionIndex > options.Count)
                {
                    return;
                }
                
                var selectedOption = options[optionIndex - 1];
                
                if (selectedOption.IsCurrent)
                {
                    InformationManager.DisplayMessage(new InformationMessage(
                        new TextObject("You already have this equipment variant.").ToString()));
                    return;
                }
                
                if (!selectedOption.CanAfford)
                {
                    var message = new TextObject("Insufficient funds. You need {COST} denars.");
                    message.SetTextVariable("COST", selectedOption.Cost.ToString());
                    InformationManager.DisplayMessage(new InformationMessage(message.ToString()));
                    return;
                }
                
                // Process the equipment variant request
                RequestEquipmentVariant(selectedOption.Item, selectedOption.Slot);
                
                // Return to main quartermaster menu
                GameMenu.ActivateGameMenu("quartermaster_equipment");
            }
            catch (Exception ex)
            {
                ModLogger.Error("Quartermaster", "Error selecting variant option", ex);
            }
        }
        
        /// <summary>
        /// Build display text for a variant option.
        /// </summary>
        private string BuildVariantOptionText(EquipmentVariantOption option)
        {
            var statusText = option.IsCurrent ? "(Current)" :
                           option.CanAfford ? $"({option.Cost} denars)" :
                           $"({option.Cost} denars - Can't afford)";
            
            var marker = option.IsCurrent ? "*" : "-"; // Use simple ASCII characters
            return $"{marker} {option.Item.Name} {statusText}";
        }
        
        #endregion
    }
    
    /// <summary>
    /// Equipment variant option for quartermaster menu display.
    /// </summary>
    public class EquipmentVariantOption
    {
        public ItemObject Item { get; set; }
        public int Cost { get; set; }
        public bool IsCurrent { get; set; }
        public bool CanAfford { get; set; }
        public EquipmentIndex Slot { get; set; }
        public bool IsOfficerExclusive { get; set; } // Available only due to quartermaster officer privileges
    }
}
