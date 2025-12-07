using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using Helpers;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Features.Assignments.Behaviors;
using Enlisted.Features.Interface.Behaviors;
using Enlisted.Mod.Core.Logging;
using Enlisted.Mod.Entry;

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
        // ReSharper disable once NotAccessedField.Local - Field is assigned for future caching functionality
        private Dictionary<string, CharacterObject> _currentTroopCache;
        [System.Diagnostics.CodeAnalysis.SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "May be used for future cache invalidation")]
        private CampaignTime _lastCacheUpdate = CampaignTime.Zero;
        private static readonly HashSet<string> NonReturnableQuestItemIds =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "dragon_banner" // Main quest banner item should never be returned
            };

        // Quartermaster state
        private CharacterObject _selectedTroop;
        private Dictionary<EquipmentIndex, List<EquipmentVariantOption>> _availableVariants;
        private readonly EquipmentIndex _selectedSlot = EquipmentIndex.None;
        private readonly List<ReturnOption> _returnOptions = new List<ReturnOption>();
        
        // Conversation tracking for dynamic equipment selection
        [System.Diagnostics.CodeAnalysis.SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "May be used for future conversation-based equipment selection")]
        private Dictionary<int, EquipmentVariantOption> _conversationWeaponVariants = new Dictionary<int, EquipmentVariantOption>();
        // ReSharper disable once NotAccessedField.Local - Field is assigned for future conversation-based equipment selection
        private Dictionary<int, EquipmentVariantOption> _conversationEquipmentVariants = new Dictionary<int, EquipmentVariantOption>();
        // ReSharper disable once NotAccessedField.Local - Field is assigned for future conversation-based equipment selection
        private string _conversationEquipmentType = "";
        
        private sealed class ReturnOption
        {
            public ItemObject Item { get; set; }
            public int Count { get; set; }
        }
        
        public QuartermasterManager()
        {
            Instance = this;
            InitializeVariantCache();
        }
        
        #region Time-Preserving Menu Helpers
        
        // Captured time state for wait menu time restoration (allows spacebar to work)
        // Public so other menu behaviors can share the same captured state
        public static CampaignTimeControlMode? CapturedTimeMode { get; set; }
        
        /// <summary>
        /// Activate a game menu without changing the current time control mode.
        /// Vanilla ActivateGameMenu forcibly pauses time; this wrapper captures and restores the prior state.
        /// Also updates the captured time mode for the wait menu tick handler.
        /// </summary>
        private static void ActivateMenuPreserveTime(string menuId)
        {
            var previousMode = Campaign.Current?.TimeControlMode ?? CampaignTimeControlMode.Stop;
            CapturedTimeMode = previousMode; // Update for tick handler (shared across all Enlisted menus)
            GameMenu.ActivateGameMenu(menuId);
            if (Campaign.Current != null)
            {
                Campaign.Current.TimeControlMode = previousMode;
            }
        }
        
        /// <summary>
        /// Switch to a game menu without changing the current time control mode.
        /// Vanilla SwitchToMenu forcibly pauses time; this wrapper captures and restores the prior state.
        /// Also updates the captured time mode for the wait menu tick handler.
        /// </summary>
        private static void SwitchToMenuPreserveTime(string menuId)
        {
            var previousMode = Campaign.Current?.TimeControlMode ?? CampaignTimeControlMode.Stop;
            CapturedTimeMode = previousMode; // Update for tick handler (shared across all Enlisted menus)
            GameMenu.SwitchToMenu(menuId);
            if (Campaign.Current != null)
            {
                Campaign.Current.TimeControlMode = previousMode;
            }
        }
        
        /// <summary>
        /// Capture the current time control mode BEFORE activating a wait menu.
        /// Must be called from the calling code before ActivateGameMenu/SwitchToMenu,
        /// not from init handlers (which run after vanilla already sets Stop).
        /// This is a shared capture used by all Enlisted wait menus.
        /// </summary>
        public static void CaptureTimeStateBeforeMenuActivation()
        {
            CapturedTimeMode = Campaign.Current?.TimeControlMode;
            ModLogger.Info("Quartermaster", $"Captured time state: {CapturedTimeMode}");
        }
        
        /// <summary>
        /// Shared wait menu condition. Always returns true since we control exit via menu options.
        /// </summary>
        private static bool QuartermasterWaitCondition(MenuCallbackArgs args)
        {
            return true;
        }
        
        /// <summary>
        /// Shared wait menu consequence. Empty since we handle exit via menu options.
        /// </summary>
        private static void QuartermasterWaitConsequence(MenuCallbackArgs args)
        {
            // No consequence needed - we never let progress reach 100%
        }
        
        /// <summary>
        /// Wait tick handler for Quartermaster menus.
        /// NOTE: Time mode restoration is handled ONCE during menu init, not here.
        /// Previously this tick handler would restore CapturedTimeMode whenever it saw
        /// UnstoppableFastForward, but this fought with user input - when the user clicked
        /// fast forward, the next tick would immediately restore it. This caused x3 speed to pause.
        /// </summary>
        private static void QuartermasterWaitTick(MenuCallbackArgs args, CampaignTime dt)
        {
            // Intentionally empty - time mode is handled in menu init, not per-tick
            // The old code here fought with user speed input and caused pausing issues
        }
        
        #endregion
        
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
            // Set up global gold icon for inline currency display
            MBTextManager.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">");
            
            AddQuartermasterMenus(starter);
            ModLogger.Info("Quartermaster", "Quartermaster system initialized with modern UI styling");
        }
        
        /// <summary>
        /// Menu background initialization for quartermaster_equipment menu.
        /// Sets culture-appropriate background and ambient audio.
        /// </summary>
        [GameMenuInitializationHandler("quartermaster_equipment")]
        private static void OnQuartermasterEquipmentBackgroundInit(MenuCallbackArgs args)
        {
            var enlistment = EnlistmentBehavior.Instance;
            var backgroundMesh = "encounter_looter";
            
            if (enlistment?.CurrentLord?.Clan?.Kingdom?.Culture?.EncounterBackgroundMesh != null)
            {
                backgroundMesh = enlistment.CurrentLord.Clan.Kingdom.Culture.EncounterBackgroundMesh;
            }
            else if (enlistment?.CurrentLord?.Culture?.EncounterBackgroundMesh != null)
            {
                backgroundMesh = enlistment.CurrentLord.Culture.EncounterBackgroundMesh;
            }
            
            args.MenuContext.SetBackgroundMeshName(backgroundMesh);
            args.MenuContext.SetAmbientSound("event:/map/ambient/node/settlements/2d/camp_army");
            args.MenuContext.SetPanelSound("event:/ui/panels/settlement_camp");
        }
        
        /// <summary>
        /// Menu background initialization for quartermaster_variants menu.
        /// </summary>
        [GameMenuInitializationHandler("quartermaster_variants")]
        private static void OnQuartermasterVariantsBackgroundInit(MenuCallbackArgs args)
        {
            var enlistment = EnlistmentBehavior.Instance;
            var backgroundMesh = "encounter_looter";
            
            if (enlistment?.CurrentLord?.Clan?.Kingdom?.Culture?.EncounterBackgroundMesh != null)
            {
                backgroundMesh = enlistment.CurrentLord.Clan.Kingdom.Culture.EncounterBackgroundMesh;
            }
            else if (enlistment?.CurrentLord?.Culture?.EncounterBackgroundMesh != null)
            {
                backgroundMesh = enlistment.CurrentLord.Culture.EncounterBackgroundMesh;
            }
            
            args.MenuContext.SetBackgroundMeshName(backgroundMesh);
            args.MenuContext.SetAmbientSound("event:/map/ambient/node/settlements/2d/camp_army");
            args.MenuContext.SetPanelSound("event:/ui/panels/settlement_camp");
        }
        
        /// <summary>
        /// Menu background initialization for quartermaster_supplies menu.
        /// </summary>
        [GameMenuInitializationHandler("quartermaster_supplies")]
        private static void OnQuartermasterSuppliesBackgroundInit(MenuCallbackArgs args)
        {
            var enlistment = EnlistmentBehavior.Instance;
            var backgroundMesh = "encounter_looter";
            
            if (enlistment?.CurrentLord?.Clan?.Kingdom?.Culture?.EncounterBackgroundMesh != null)
            {
                backgroundMesh = enlistment.CurrentLord.Clan.Kingdom.Culture.EncounterBackgroundMesh;
            }
            else if (enlistment?.CurrentLord?.Culture?.EncounterBackgroundMesh != null)
            {
                backgroundMesh = enlistment.CurrentLord.Culture.EncounterBackgroundMesh;
            }
            
            args.MenuContext.SetBackgroundMeshName(backgroundMesh);
            args.MenuContext.SetAmbientSound("event:/map/ambient/node/settlements/2d/camp_army");
            args.MenuContext.SetPanelSound("event:/ui/panels/settlement_camp");
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
        /// Uses wait menus with hidden progress to allow spacebar time control passthrough.
        /// </summary>
        private void AddQuartermasterMenus(CampaignGameStarter starter)
        {
            // Main quartermaster equipment menu (wait menu with hidden progress for spacebar support)
            starter.AddWaitGameMenu(
                "quartermaster_equipment",
                "Army Quartermaster\n{QUARTERMASTER_TEXT}",
                OnQuartermasterEquipmentInit,
                QuartermasterWaitCondition,
                QuartermasterWaitConsequence,
                QuartermasterWaitTick,
                GameMenu.MenuAndOptionType.WaitMenuHideProgressAndHoursOption);
                
            // Equipment variant selection submenu (wait menu with hidden progress)
            starter.AddWaitGameMenu(
                "quartermaster_variants",
                "Equipment Variants\n{VARIANT_TEXT}",
                OnQuartermasterVariantsInit,
                QuartermasterWaitCondition,
                QuartermasterWaitConsequence,
                QuartermasterWaitTick,
                GameMenu.MenuAndOptionType.WaitMenuHideProgressAndHoursOption);
            
            // Return equipment submenu (wait menu with hidden progress)
            starter.AddWaitGameMenu(
                "quartermaster_returns",
                "Return Equipment\n{RETURN_TEXT}",
                OnQuartermasterReturnsInit,
                QuartermasterWaitCondition,
                QuartermasterWaitConsequence,
                QuartermasterWaitTick,
                GameMenu.MenuAndOptionType.WaitMenuHideProgressAndHoursOption);
                
            // Main equipment category options with modern icons

            // Request weapon variants (Trade icon for equipment exchange)
            starter.AddGameMenuOption("quartermaster_equipment", "quartermaster_weapons",
                "Request weapon variants",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Trade;
                    return IsWeaponVariantsAvailable(args);
                },
                OnWeaponVariantsSelected,
                false, 1);
                
            // Request armor variants (Trade icon)
            starter.AddGameMenuOption("quartermaster_equipment", "quartermaster_armor",
                "Request armor variants", 
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Trade;
                    return IsArmorVariantsAvailable(args);
                },
                OnArmorVariantsSelected,
                false, 2);
                
            // Helmets are handled through the armor slot selection system
                
            // Request accessory variants (Trade icon)
            starter.AddGameMenuOption("quartermaster_equipment", "quartermaster_accessories",
                "Request accessory variants",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Trade;
                    return IsAccessoryVariantsAvailable(args);
                },
                OnAccessoryVariantsSelected,
                false, 4);
                
            // Supply management options (Manage icon for inventory management)
            starter.AddGameMenuOption("quartermaster_equipment", "quartermaster_supplies",
                "Manage party supplies",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Manage;
                    var available = IsSupplyManagementAvailable(args);
                    return available;
                },
                OnSupplyManagementSelected,
                false, 5);
            
            // Return issued equipment (Manage icon)
            starter.AddGameMenuOption("quartermaster_equipment", "quartermaster_return",
                "Return issued equipment",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Manage;
                    return true;
                },
                _ => ActivateMenuPreserveTime("quartermaster_returns"),
                false, 6);
                
            // Return to enlisted status (Leave icon)
            starter.AddGameMenuOption("quartermaster_equipment", "quartermaster_back",
                "Return to enlisted status",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Leave;
                    return true;
                },
                _ =>
                {
                    NextFrameDispatcher.RunNextFrame(() =>
                    {
                        try
                        {
                            SwitchToMenuPreserveTime("enlisted_status");
                            ModLogger.Info("Quartermaster", "Returned from quartermaster to enlisted status menu");
                        }
                        catch (Exception ex)
                        {
                            ModLogger.Error("Quartermaster", $"Failed to switch back to enlisted status: {ex.Message}");
                            EnlistedMenuBehavior.SafeActivateEnlistedMenu();
                        }
                    });
                });
                
            // Variant selection options (dynamically populated)
            AddVariantSelectionOptions(starter);
            
            // Return equipment options
            AddReturnMenuOptions(starter);
            
            // Supply management menu for quartermaster officers
            AddSupplyManagementMenu(starter);
        }

        /// <summary>
        /// Add dynamic variant selection options for the variants submenu.
        /// </summary>
        private void AddVariantSelectionOptions(CampaignGameStarter starter)
        {
            // Generic variant selection options that will be populated dynamically
            for (var i = 1; i <= 6; i++) // Support up to 6 variants per slot
            {
                var index = i; // Capture local copy to avoid closure issue
                starter.AddGameMenuOption("quartermaster_variants", $"variant_option_{index}",
                    "", // Text will be set dynamically
                    args => IsVariantOptionAvailable(args, index),
                    args => OnVariantOptionSelected(args, index),
                    false, index);
            }
            
            // Return to quartermaster (Leave icon)
            starter.AddGameMenuOption("quartermaster_variants", "variants_back",
                "Return to quartermaster",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Leave;
                    return true;
                },
                _ => ActivateMenuPreserveTime("quartermaster_equipment"));
        }

        /// <summary>
        /// Add dynamic return options for the return submenu.
        /// </summary>
        private void AddReturnMenuOptions(CampaignGameStarter starter)
        {
            for (var i = 1; i <= MaxReturnOptions; i++)
            {
                var index = i;
                starter.AddGameMenuOption("quartermaster_returns", $"return_option_{index}",
                    $"{{RETURN_OPTION_{index}}}",
                    args => IsReturnOptionAvailable(args, index),
                    args => OnReturnOptionSelected(args, index),
                    false, index);
            }

            // Return to quartermaster (Leave icon)
            starter.AddGameMenuOption("quartermaster_returns", "returns_back",
                "Return to quartermaster",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Leave;
                    return true;
                },
                _ => ActivateMenuPreserveTime("quartermaster_equipment"));
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
                if (_troopEquipmentVariants.TryGetValue(cacheKey, out var cachedVariants))
                {
                    return cachedVariants;
                }
                
                var variants = new Dictionary<EquipmentIndex, List<ItemObject>>();
                var troopCulture = selectedTroop.Culture;
                
                // RUNTIME DISCOVERY: Extract all equipment variants from this troop's BattleEquipments
                foreach (var equipment in selectedTroop.BattleEquipments)
                {
                    for (var slot = EquipmentIndex.Weapon0; slot <= EquipmentIndex.HorseHarness; slot++)
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
                
                if (enlistment?.IsEnlisted != true || duties == null)
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
                    ModLogger.Info("Quartermaster", $"Player troop identified as: {selectedTroop.Name} ({formation} formation)");
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
        /// Get the item limit for a specific equipment slot.
        /// Armor slots (head, body, legs, gloves, cape, shields) = 1 per type.
        /// Weapon slots (weapons, bows, arrows, bolts, javelins, throwing) = 2 per type.
        /// </summary>
        private static int GetSlotItemLimit(EquipmentIndex slot)
        {
            // Weapon slots allow 2 of each type (multiple weapons, ammo stacks, etc.)
            if (slot is >= EquipmentIndex.Weapon0 and <= EquipmentIndex.Weapon3)
            {
                return 2;
            }
            // Armor/accessory slots only allow 1 of each type
            return 1;
        }
        
        /// <summary>
        /// Maximum return options shown at once in the return menu.
        /// </summary>
        private const int MaxReturnOptions = 6;
        
        /// <summary>
        /// Process equipment variant request and update player equipment.
        /// Equipment is FREE (cost = 0), but limited to 2 of each item type.
        /// For weapons and consumables, finds the next available slot if the requested slot is occupied.
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
                
                // ITEM LIMIT CHECK: Weapons allow 2, armor/accessories allow 1
                var currentCount = GetPlayerItemCount(hero, requestedItem.StringId);
                var itemLimit = GetSlotItemLimit(slot);
                
                ModLogger.Debug("Quartermaster", $"Limit check: {requestedItem.Name} in {slot} - owned={currentCount}, limit={itemLimit} ({(itemLimit == 1 ? "armor" : "weapon")})");
                
                if (currentCount >= itemLimit)
                {
                    var limitMsg = itemLimit == 1 
                        ? new TextObject("{=qm_item_limit_reached_1}You already have this equipped, soldier.")
                        : new TextObject("{=qm_item_limit_reached_2}Two's the limit, soldier. Army regs. You already have {COUNT} of {ITEM_NAME}.");
                    limitMsg.SetTextVariable("COUNT", currentCount);
                    limitMsg.SetTextVariable("ITEM_NAME", requestedItem.Name);
                    InformationManager.DisplayMessage(new InformationMessage(limitMsg.ToString(), Colors.Yellow));
                    ModLogger.Info("Quartermaster", $"Item limit reached: {requestedItem.Name} (count: {currentCount}, limit: {itemLimit})");
                    return;
                }
                
                // For weapons and consumables, find the best slot to place the item
                // This prevents overwriting the same slot when player wants multiple items
                var targetSlot = slot;
                var addedToInventory = false;
                
                if (IsWeaponSlot(slot) || IsConsumableItem(requestedItem))
                {
                    targetSlot = FindBestWeaponSlot(hero, requestedItem, slot);
                    
                    // If no empty slot available, add to inventory instead of blocking
                    if (targetSlot == EquipmentIndex.None)
                    {
                        // Add to party inventory since hands are full
                        var partyInventory = PartyBase.MainParty?.ItemRoster;
                        if (partyInventory != null)
                        {
                            partyInventory.AddToCounts(new EquipmentElement(requestedItem), 1);
                            addedToInventory = true;
                            
                            var inventoryMsg = new TextObject("{=qm_added_to_inventory}{ITEM_NAME} stowed in your pack. Hands full.");
                            inventoryMsg.SetTextVariable("ITEM_NAME", requestedItem.Name);
                            InformationManager.DisplayMessage(new InformationMessage(inventoryMsg.ToString(), Colors.Yellow));
                            ModLogger.Info("Quartermaster", $"Weapon slots full - {requestedItem.Name} added to inventory");
                        }
                        else
                        {
                            // Fallback if inventory unavailable (shouldn't happen)
                            var noSlotsMsg = new TextObject("{=qm_no_weapon_slots}Your hands are full, soldier. Return something to the armory first.");
                            InformationManager.DisplayMessage(new InformationMessage(noSlotsMsg.ToString(), Colors.Yellow));
                            ModLogger.Info("Quartermaster", $"No available weapon slot for {requestedItem.Name}");
                            return;
                        }
                    }
                }
                
                // If we added to inventory, record it and skip equipment slot change
                if (addedToInventory)
                {
                    // Record for accountability (item is now in inventory)
                    var troopSelection = TroopSelectionManager.Instance;
                    troopSelection?.RecordIssuedItem(requestedItem, EquipmentIndex.None);
                    return;
                }
                
                var currentItem = hero.BattleEquipment[targetSlot].Item;
                var previousItemName = currentItem?.Name?.ToString() ?? "empty";
                
                // Equipment is FREE for soldiers - no cost charged
                // (Accountability system charges for missing gear on troop change instead)
                
                // Apply the equipment change to the target slot
                ApplyEquipmentSlotChange(hero, requestedItem, targetSlot);
                
                // Record newly issued equipment for accountability tracking
                var troopSelectionMgr = TroopSelectionManager.Instance;
                troopSelectionMgr?.RecordIssuedItem(requestedItem, targetSlot);
                
                // Success notification
                var successMessage = new TextObject("{=qm_equipment_issued}Equipment issued: {ITEM_NAME}. Army provides.");
                successMessage.SetTextVariable("ITEM_NAME", requestedItem.Name);
                InformationManager.DisplayMessage(new InformationMessage(successMessage.ToString()));
                
                ModLogger.Info("Quartermaster", $"Equipment issued: {requestedItem.Name} to slot {targetSlot} (replaced {previousItemName})");
            }
            catch (Exception ex)
            {
                ModLogger.Error("Quartermaster", $"Error processing equipment variant request for {requestedItem?.Name?.ToString() ?? "null"} in slot {slot}: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// Find the best available weapon slot for an item.
        /// Only returns empty slots - does not replace existing items.
        /// When all slots are full, returns None so caller can add to inventory.
        /// </summary>
        private static EquipmentIndex FindBestWeaponSlot(Hero hero, ItemObject newItem, EquipmentIndex preferredSlot)
        {
            _ = newItem; // Item type not needed since we only look for empty slots
            
            // Weapon slots are Weapon0 through Weapon3
            var weaponSlots = new[] { EquipmentIndex.Weapon0, EquipmentIndex.Weapon1, EquipmentIndex.Weapon2, EquipmentIndex.Weapon3 };
            
            // Priority 1: Use the preferred slot if it's empty
            if (IsWeaponSlot(preferredSlot) && hero.BattleEquipment[preferredSlot].IsEmpty)
            {
                return preferredSlot;
            }
            
            // Priority 2: Find any empty weapon slot
            foreach (var weaponSlot in weaponSlots)
            {
                if (hero.BattleEquipment[weaponSlot].IsEmpty)
                {
                    return weaponSlot;
                }
            }
            
            // All weapon slots are occupied - return None so caller can add to inventory
            return EquipmentIndex.None;
        }
        
        /// <summary>
        /// Count how many of a specific item the player currently has in equipment and inventory.
        /// Used to enforce the 2-item-per-type limit to prevent abuse.
        /// </summary>
        private static int GetPlayerItemCount(Hero hero, string itemStringId)
        {
            if (hero == null || string.IsNullOrEmpty(itemStringId))
            {
                return 0;
            }
            
            var count = 0;
            
            // Check battle equipment (all slots including weapons, armor, horse)
            for (var slot = EquipmentIndex.Weapon0; slot <= EquipmentIndex.HorseHarness; slot++)
            {
                if (hero.BattleEquipment[slot].Item?.StringId == itemStringId)
                {
                    count++;
                }
            }
            
            // Also check civilian equipment
            for (var slot = EquipmentIndex.Weapon0; slot <= EquipmentIndex.HorseHarness; slot++)
            {
                if (hero.CivilianEquipment[slot].Item?.StringId == itemStringId)
                {
                    count++;
                }
            }
            
            // Check party inventory for the same item
            var partyInventory = PartyBase.MainParty?.ItemRoster;
            if (partyInventory != null)
            {
                var targetItem = MBObjectManager.Instance.GetObject<ItemObject>(itemStringId);
                if (targetItem != null)
                {
                    count += partyInventory.GetItemNumber(targetItem);
                }
            }
            
            return count;
        }

        /// <summary>
        /// Build a list of returnable items with their total counts.
        /// </summary>
        private List<ReturnOption> BuildReturnOptions()
        {
            var options = new List<ReturnOption>();
            var hero = Hero.MainHero;
            if (hero == null)
            {
                return options;
            }

            var seen = new HashSet<string>(StringComparer.Ordinal);

            void TryAddItem(ItemObject item)
            {
                if (!IsReturnableItem(item) || !seen.Add(item.StringId))
                {
                    return;
                }

                var total = GetPlayerItemCount(hero, item.StringId);
                if (total > 0)
                {
                    options.Add(new ReturnOption { Item = item, Count = total });
                }
            }

            // Battle equipment
            for (var slot = EquipmentIndex.Weapon0; slot <= EquipmentIndex.HorseHarness; slot++)
            {
                TryAddItem(hero.BattleEquipment[slot].Item);
            }

            // Civilian equipment
            for (var slot = EquipmentIndex.Weapon0; slot <= EquipmentIndex.HorseHarness; slot++)
            {
                TryAddItem(hero.CivilianEquipment[slot].Item);
            }

            // Party inventory
            var roster = PartyBase.MainParty?.ItemRoster;
            if (roster != null)
            {
                for (var i = 0; i < roster.Count; i++)
                {
                    var element = roster.GetElementCopyAtIndex(i);
                    if (element.Amount <= 0)
                    {
                        continue;
                    }

                    TryAddItem(element.EquipmentElement.Item);
                }
            }

            return options
                .OrderByDescending(o => o.Count)
                .ThenBy(o => o.Item.Name?.ToString())
                .Take(MaxReturnOptions)
                .ToList();
        }

        /// <summary>
        /// Update text variables for return options.
        /// </summary>
        private void SetReturnOptionTextVariables()
        {
            for (var i = 0; i < MaxReturnOptions; i++)
            {
                var variableName = $"RETURN_OPTION_{i + 1}";
                if (i < _returnOptions.Count)
                {
                    var option = _returnOptions[i];
                    var text = new TextObject("{=qm_return_option_label}{ITEM_NAME} (x{COUNT}) - Return one");
                    text.SetTextVariable("ITEM_NAME", option.Item.Name);
                    text.SetTextVariable("COUNT", option.Count);
                    MBTextManager.SetTextVariable(variableName, text.ToString());
                }
                else
                {
                    MBTextManager.SetTextVariable(variableName, "");
                }
            }
        }

        /// <summary>
        /// Determine if an item is valid for return (equipment/weapon/mount/consumable).
        /// </summary>
        private static bool IsReturnableItem(ItemObject item)
        {
            if (item == null)
            {
                return false;
            }

            // Never allow quest-critical items (e.g., Dragon Banner) to be returned
            if (item.IsQuestItem || NonReturnableQuestItemIds.Contains(item.StringId))
            {
                return false;
            }

            return item.PrimaryWeapon != null ||
                   item.ArmorComponent != null ||
                   item.HorseComponent != null;
        }

        private bool IsReturnOptionAvailable(MenuCallbackArgs args, int optionIndex)
        {
            try
            {
                if (optionIndex > _returnOptions.Count)
                {
                    return false;
                }

                var option = _returnOptions[optionIndex - 1];
                if (option == null || option.Count <= 0)
                {
                    return false;
                }

                args.optionLeaveType = GameMenuOption.LeaveType.Manage;
                args.Tooltip = new TextObject("{=qm_return_tooltip}Return one item to the armory.");
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void OnReturnOptionSelected(MenuCallbackArgs args, int optionIndex)
        {
            _ = args;
            try
            {
                if (optionIndex > _returnOptions.Count)
                {
                    return;
                }

                var option = _returnOptions[optionIndex - 1];
                var item = option.Item;

                var removed = TryReturnSingleItem(item);
                var remaining = GetPlayerItemCount(Hero.MainHero, item.StringId);

                var message = removed
                    ? new TextObject("{=qm_return_success}Returned {ITEM_NAME} to the armory.")
                    : new TextObject("{=qm_return_none_left}No {ITEM_NAME} remains to return.");
                message.SetTextVariable("ITEM_NAME", item.Name);

                InformationManager.DisplayMessage(new InformationMessage(message.ToString(), Colors.Yellow));

                ModLogger.Info("Quartermaster",
                    $"Return action: {item.Name}; removed={(removed ? "yes" : "no")}; remaining={remaining}");

                // Refresh options after removal
                _returnOptions.Clear();
                _returnOptions.AddRange(BuildReturnOptions());
                SetReturnOptionTextVariables();

                SwitchToMenuPreserveTime("quartermaster_returns");
            }
            catch (Exception ex)
            {
                ModLogger.Error("Quartermaster", "Error returning equipment", ex);
                InformationManager.DisplayMessage(new InformationMessage(
                    new TextObject("{=qm_return_error}Return processing unavailable.").ToString(), Colors.Red));
            }
        }

        /// <summary>
        /// Attempt to return a single item from inventory or equipment.
        /// </summary>
        private bool TryReturnSingleItem(ItemObject item)
        {
            if (item == null)
            {
                return false;
            }

            // Prefer removing from inventory first to avoid stripping equipped gear if possible
            if (TryRemoveFromInventory(item))
            {
                TroopSelectionManager.Instance?.MarkIssuedItemReturned(item.StringId);
                return true;
            }

            var hero = Hero.MainHero;
            if (hero != null && TryRemoveFromEquipment(hero, item))
            {
                TroopSelectionManager.Instance?.MarkIssuedItemReturned(item.StringId);
                return true;
            }

            return false;
        }

        private bool TryRemoveFromInventory(ItemObject item)
        {
            var roster = PartyBase.MainParty?.ItemRoster;
            if (roster == null)
            {
                return false;
            }

            if (roster.GetItemNumber(item) <= 0)
            {
                return false;
            }

            roster.AddToCounts(new EquipmentElement(item), -1);
            return true;
        }

        private bool TryRemoveFromEquipment(Hero hero, ItemObject item)
        {
            // Battle equipment first
            var battleEquipment = hero.BattleEquipment.Clone();
            for (var slot = EquipmentIndex.Weapon0; slot <= EquipmentIndex.HorseHarness; slot++)
            {
                if (battleEquipment[slot].Item?.StringId == item.StringId)
                {
                    battleEquipment[slot] = default;
                    EquipmentHelper.AssignHeroEquipmentFromEquipment(hero, battleEquipment);
                    return true;
                }
            }

            // Civilian equipment
            var civilianEquipment = hero.CivilianEquipment.Clone();
            for (var slot = EquipmentIndex.Weapon0; slot <= EquipmentIndex.HorseHarness; slot++)
            {
                if (civilianEquipment[slot].Item?.StringId == item.StringId)
                {
                    civilianEquipment[slot] = default;
                    hero.CivilianEquipment.FillFrom(civilianEquipment, false);
                    return true;
                }
            }

            return false;
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
                var newEquipment = hero.BattleEquipment.Clone(); // Default cloneWithoutWeapons=false is sufficient
                
                // Replace only the requested slot
                newEquipment[slot] = new EquipmentElement(newItem); // Default parameters are sufficient
                
                // Apply the updated equipment
                EquipmentHelper.AssignHeroEquipmentFromEquipment(hero, newEquipment);
                
                // Equipment change is applied via EquipmentHelper which handles visual refresh
                // The hero's equipment is updated immediately and visible in the game world
                
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
                // Time state is captured by caller before ActivateGameMenu (not here - too late)
                
                // 1.3.4+: Set proper menu background to avoid assertion failure
                var enlistment = EnlistmentBehavior.Instance;
                var backgroundMesh = "encounter_looter"; // Safe fallback
                
                if (enlistment?.CurrentLord?.Clan?.Kingdom?.Culture?.EncounterBackgroundMesh != null)
                {
                    backgroundMesh = enlistment.CurrentLord.Clan.Kingdom.Culture.EncounterBackgroundMesh;
                }
                else if (enlistment?.CurrentLord?.Culture?.EncounterBackgroundMesh != null)
                {
                    backgroundMesh = enlistment.CurrentLord.Culture.EncounterBackgroundMesh;
                }
                
                args.MenuContext.SetBackgroundMeshName(backgroundMesh);
                
                // Validate enlisted state
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
                    ModLogger.Info("Quartermaster", $"No equipment variants found for {_selectedTroop.Name}");
                    return;
                }
                
                _availableVariants = BuildVariantOptions(variants);
                
                // Build quartermaster display
                BuildQuartermasterStatusDisplay();
                
                // Create dynamic menu options for each equipment slot with variants
                CreateEquipmentSlotOptions(args);
                
                ModLogger.Info("Quartermaster", $"Quartermaster menu opened for {_selectedTroop.Name} with {_availableVariants?.Count ?? 0} variant slots");
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
        /// Provides full equipment variant access for quartermaster officers.
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
            
            // Convert to variant options - equipment is FREE but limited to 2 per item type
            var finalOptions = new Dictionary<EquipmentIndex, List<EquipmentVariantOption>>();
            
            foreach (var slotItems in options)
            {
                var slot = slotItems.Key;
                var items = slotItems.Value;
                
                // Include all slots with at least one option - this ensures players can see their
                // current equipment even if no alternatives exist (e.g., Tier 1 Levy with only one helmet type).
                // Previously filtered to Count > 1, which caused equipped items to be hidden from the store.
                if (items.Count > 0)
                {
                    var variantOptions = new List<EquipmentVariantOption>();
                    var currentItem = hero.BattleEquipment[slot].Item;
                    
                    foreach (var item in items)
                    {
                        var isCurrent = item == currentItem;
                        
                        // Check if player has hit the slot-specific limit (weapons=2, armor=1)
                        var itemCount = GetPlayerItemCount(hero, item.StringId);
                        var itemLimit = GetSlotItemLimit(slot);
                        var isAtLimit = itemCount >= itemLimit;
                        
                        // Determine if item allows duplicate purchases (weapons and consumables)
                        // Soldiers can carry multiple weapons in different slots or multiple stacks of ammo
                        var allowsDuplicate = IsWeaponSlot(slot) || IsConsumableItem(item);
                        
                        // Equipment is FREE - cost is 0 (accountability system handles missing gear charges)
                        // CanAfford is true unless at item limit
                        variantOptions.Add(new EquipmentVariantOption
                        {
                            Item = item,
                            Cost = 0, // Equipment is free for soldiers
                            IsCurrent = isCurrent,
                            CanAfford = !isAtLimit, // Can get if not at limit
                            Slot = slot,
                            IsOfficerExclusive = !variants.ContainsKey(slot) || !variants[slot].Contains(item),
                            AllowsDuplicatePurchase = allowsDuplicate,
                            IsAtLimit = isAtLimit
                        });
                    }
                    
                    // Sort: current item first, then by name
                    variantOptions = variantOptions.OrderBy(o => o.IsCurrent ? 0 : 1)
                                                  .ThenBy(o => o.Item.Name.ToString()).ToList();
                    
                    finalOptions[slot] = variantOptions;
                }
            }

            return finalOptions;
        }
        
        /// <summary>
        /// Get culture-wide equipment variants for quartermaster officers.
        /// Provides equipment variant access beyond standard troop equipment.
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
                        for (var slot = EquipmentIndex.Weapon0; slot <= EquipmentIndex.HorseHarness; slot++)
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
        /// Build quartermaster status display - clean, organized format matching other Enlisted menus.
        /// </summary>
        private void BuildQuartermasterStatusDisplay()
        {
            try
            {
                var sb = new StringBuilder();
                var duties = EnlistedDutiesBehavior.Instance;
                
                // In-character quartermaster dialogue
                var qmDialogue = new TextObject("{=qm_intro_dialogue}\"Take what you need, soldier. The army provides. Mind your kit - anything missing gets docked from your pay.\"");
                sb.AppendLine(qmDialogue.ToString());
                sb.AppendLine();
                
                // Current status section
                sb.AppendLine("— Current Status —");
                sb.AppendLine();
                sb.AppendLine($"Troop Type: {_selectedTroop?.Name?.ToString() ?? "Unknown"}");
                sb.AppendLine();
                
                // Issue limits section
                sb.AppendLine("— Issue Limits —");
                sb.AppendLine();
                sb.AppendLine("Armor & Shields: 1 each");
                sb.AppendLine("Weapons & Ammo: 2 each");
                sb.AppendLine();
                
                // Officer privileges if applicable
                var isProvisioner = duties?.ActiveDuties.Contains("provisioner") == true;
                var isQuartermaster = duties?.GetCurrentOfficerRole() == "Quartermaster";
                
                if (isProvisioner || isQuartermaster)
                {
                    sb.AppendLine("— Officer Access —");
                    sb.AppendLine();
                    sb.AppendLine("Extended equipment options available.");
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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "Required by menu callback signature")]
        private void CreateEquipmentSlotOptions(MenuCallbackArgs args)
        {
            // Create menu options for equipment slot selection
            // Focus on weapon variants as they are the most commonly changed equipment
            var weaponVariants = _availableVariants.Where(kvp => 
                kvp.Key is >= EquipmentIndex.Weapon0 and <= EquipmentIndex.Weapon3).ToList();
                
            if (weaponVariants.Count > 0)
            {
                // Add a generic "Request weapon variant" option
                // The actual variant selection would happen in a submenu or through conversation
                // Check if any variants are available (not at the 2-item limit)
                var hasAffordableVariants = weaponVariants.Any(kvp => 
                    kvp.Value.Any(opt => !opt.IsAtLimit));
                    
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
                // Time state is captured by caller before menu transition (not here - too late)
                
                // 1.3.4+: Set proper menu background to avoid assertion failure
                var enlistment = EnlistmentBehavior.Instance;
                var backgroundMesh = "encounter_looter"; // Safe fallback
                
                if (enlistment?.CurrentLord?.Clan?.Kingdom?.Culture?.EncounterBackgroundMesh != null)
                {
                    backgroundMesh = enlistment.CurrentLord.Clan.Kingdom.Culture.EncounterBackgroundMesh;
                }
                else if (enlistment?.CurrentLord?.Culture?.EncounterBackgroundMesh != null)
                {
                    backgroundMesh = enlistment.CurrentLord.Culture.EncounterBackgroundMesh;
                }
                
                args.MenuContext.SetBackgroundMeshName(backgroundMesh);
                
                if (_selectedSlot == EquipmentIndex.None || !_availableVariants.TryGetValue(_selectedSlot, out var options))
                {
                    MBTextManager.SetTextVariable("VARIANT_TEXT", "No equipment variants available for the selected slot.");
                    return;
                }
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
        /// Initialize return menu with current equipment/inventory counts.
        /// </summary>
        private void OnQuartermasterReturnsInit(MenuCallbackArgs args)
        {
            try
            {
                // Time state is captured by caller before menu transition (not here - too late)
                
                var enlistment = EnlistmentBehavior.Instance;
                var backgroundMesh = "encounter_looter"; // Safe fallback

                if (enlistment?.CurrentLord?.Clan?.Kingdom?.Culture?.EncounterBackgroundMesh != null)
                {
                    backgroundMesh = enlistment.CurrentLord.Clan.Kingdom.Culture.EncounterBackgroundMesh;
                }
                else if (enlistment?.CurrentLord?.Culture?.EncounterBackgroundMesh != null)
                {
                    backgroundMesh = enlistment.CurrentLord.Culture.EncounterBackgroundMesh;
                }

                args.MenuContext.SetBackgroundMeshName(backgroundMesh);

                _returnOptions.Clear();
                _returnOptions.AddRange(BuildReturnOptions());
                SetReturnOptionTextVariables();

                if (_returnOptions.Count == 0)
                {
                    MBTextManager.SetTextVariable("RETURN_TEXT",
                        new TextObject("{=qm_return_none}No equipment to return.").ToString());
                    return;
                }

                var sb = new StringBuilder();
                sb.AppendLine(new TextObject("{=qm_return_intro}Return issued gear to the armory. Select an item to hand back.").ToString());
                sb.AppendLine();

                foreach (var option in _returnOptions.Take(5))
                {
                    var line = new TextObject("{=qm_return_line}{ITEM_NAME} x{COUNT}");
                    line.SetTextVariable("ITEM_NAME", option.Item.Name);
                    line.SetTextVariable("COUNT", option.Count);
                    sb.AppendLine(line.ToString());
                }

                if (_returnOptions.Count > 5)
                {
                    sb.AppendLine($"... plus {_returnOptions.Count - 5} more items");
                }

                MBTextManager.SetTextVariable("RETURN_TEXT", sb.ToString());
            }
            catch (Exception ex)
            {
                ModLogger.Error("Quartermaster", "Error initializing return menu", ex);
                MBTextManager.SetTextVariable("RETURN_TEXT",
                    new TextObject("{=qm_return_error}Return processing unavailable.").ToString());
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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "May be called from menu system or other modules")]
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
                
                // Full access granted if player has quartermaster officer role or provisioner duty
                if (duties?.GetCurrentOfficerRole() == "Quartermaster" || 
                    duties?.ActiveDuties.Contains("provisioner") == true)
                {
                    return true; // Full access with officer privileges
                }
                
                // Standard access for all enlisted soldiers without special roles
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
        /// Uses branch-based collection to find all weapons in the troop upgrade tree.
        /// </summary>
        private bool IsWeaponVariantsAvailable(MenuCallbackArgs args)
        {
            _ = args; // Required by API contract
            try
            {
                // Use branch-based weapon collection (same pattern as armor)
                var weaponOptions = BuildWeaponOptionsFromCurrentTroop();
                
                // Available if there are any weapon slots with at least one purchasable option
                // Weapons allow duplicate purchases, so include current items that allow duplicates
                // Weapons available if any are not at the 2-item limit
                return weaponOptions.Any(kvp => 
                    kvp.Value != null && 
                    kvp.Value.Any(opt => !opt.IsAtLimit));
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// Handle weapon variant selection.
        /// Uses branch-based collection to show all weapons available in the troop upgrade tree.
        /// </summary>
        private void OnWeaponVariantsSelected(MenuCallbackArgs args)
        {
            try
            {
                // Get weapons from branch-based collection (same pattern as armor)
                var weaponOptions = BuildWeaponOptionsFromCurrentTroop();
                
                if (weaponOptions.Count == 0)
                {
                    InformationManager.DisplayMessage(new InformationMessage(
                        new TextObject("{=qm_no_weapon_variants}No weapon variants available for your troop tree.").ToString()));
                    return;
                }

                // Flatten all weapon sub-slot variants into one list and show a single scrollable grid
                // This matches the armor selection pattern for consistency
                var combined = weaponOptions.SelectMany(kvp => kvp.Value).ToList();
                ShowEquipmentVariantSelectionDialog(combined, "weapon");
            }
            catch (Exception ex)
            {
                ModLogger.Error("Quartermaster", "Error selecting weapon variants", ex);
            }
        }
        
        /// <summary>
        /// Show equipment variant selection with individual clickable items using custom Gauntlet UI.
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
                
                // Attempt to use custom Gauntlet UI for individual item clicking
                if (TryShowGauntletEquipmentSelector(variants, equipmentType))
                {
                    ModLogger.Info("Quartermaster", $"Opened Gauntlet equipment selector for {equipmentType} with {variants.Count} variants");
                }
                else
                {
                    // Fallback to automatic selection if custom UI is unavailable
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
                // Attempt to use custom Gauntlet UI for variant selection
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
        /// Automatic variant selection when custom Gauntlet UI is unavailable.
        /// Automatically selects the first affordable variant from the available options.
        /// </summary>
        private void ShowConversationBasedEquipmentSelection(List<EquipmentVariantOption> variants, string equipmentType)
        {
            try
            {
                // Filter to purchasable variants - only exclude items at the 2-item limit
                var availableVariants = variants
                    .Where(v => !v.IsAtLimit)
                    .Take(5).ToList(); // Limit to 5 for conversation
                
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
                        null)); // Default parameters are sufficient
                        
                    ModLogger.Info("Quartermaster", $"Opened equipment selection inquiry for {equipmentType}");
                }
                else
                {
                    // Use automatic selection when only one variant is available
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
                if (selectedElements is { Count: > 0 })
                {
                    if (selectedElements.First().Identifier is EquipmentVariantOption selectedVariant)
                    {
                        // Apply the selected equipment variant
                        RequestEquipmentVariant(selectedVariant.Item, selectedVariant.Slot);
                        
                        // Return to quartermaster menu
                        ActivateMenuPreserveTime("quartermaster_equipment");
                    }
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("Quartermaster", "Error processing equipment variant selection", ex);
            }
        }
        
        /// <summary>
        /// Automatic variant selection when custom UI is unavailable.
        /// Automatically selects the first affordable variant from the available options.
        /// </summary>
        private void ShowSimplifiedVariantSelection(List<EquipmentVariantOption> variants, string equipmentType)
        {
            try
            {
                // Find available variants that player hasn't hit the 2-item limit on
                var availableVariants = variants.Where(v => !v.IsAtLimit).ToList();
                
                if (availableVariants.Count > 0)
                {
                    var selectedVariant = availableVariants.First();
                    
                    // Show confirmation - requisitioned equipment is free
                    var confirmText = $"Requisitioned {selectedVariant.Item.Name}";
                    InformationManager.DisplayMessage(new InformationMessage(confirmText));
                    
                    // Apply the equipment variant
                    RequestEquipmentVariant(selectedVariant.Item, selectedVariant.Slot);
                    
                    // Return to main quartermaster menu to see updated equipment
                    ActivateMenuPreserveTime("quartermaster_equipment");
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
        /// Check if armor variants are available (body, head, leg, gloves - capes go in accessories).
        /// </summary>
        private bool IsArmorVariantsAvailable(MenuCallbackArgs args)
        {
            _ = args; // Required by API contract
            try
            {
                // Armor slots: Body, Head, Leg, Gloves (capes go in accessories with shields)
                var armorSlots = new[] { EquipmentIndex.Body, EquipmentIndex.Head, EquipmentIndex.Leg, EquipmentIndex.Gloves };
                var armorOptions = BuildArmorOptionsFromCurrentTroop();
                
                // Available if there are any armor slots with at least one option not at limit
                return armorSlots.Any(slot => 
                    armorOptions.ContainsKey(slot) &&
                    armorOptions[slot].Any(opt => !opt.IsAtLimit));
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// Handle armor variant selection (body, head, leg, gloves - capes go in accessories).
        /// </summary>
        private void OnArmorVariantsSelected(MenuCallbackArgs args)
        {
            try
            {
                // Armor slots: Body, Head, Leg, Gloves (capes go in accessories with shields)
                var armorSlots = new[] { EquipmentIndex.Body, EquipmentIndex.Head, EquipmentIndex.Leg, EquipmentIndex.Gloves };
                var armorOptions = BuildArmorOptionsFromCurrentTroop();
                
                // Filter to only armor slots (exclude capes)
                var filteredOptions = armorOptions
                    .Where(kvp => armorSlots.Contains(kvp.Key))
                    .ToDictionary(k => k.Key, v => v.Value);
                
                if (filteredOptions.Count == 0 || !filteredOptions.Any(kvp => kvp.Value.Any()))
                {
                    InformationManager.DisplayMessage(new InformationMessage(
                        new TextObject("{=qm_no_armor_variants}No armor variants available for your troop tree.").ToString()));
                    return;
                }

                // Flatten armor variants into one list
                var combined = filteredOptions.SelectMany(kvp => kvp.Value).ToList();
                ModLogger.Info("Quartermaster", $"Armor selection: {combined.Count} items (Body, Head, Leg, Gloves)");
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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "May be used for future helmet variant selection")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "Required by menu callback signature")]
        private bool IsHelmetVariantsAvailable(MenuCallbackArgs args)
        {
            try
            {
                // Helmets are part of armor collection - available if not at limit
                var armorOptions = BuildArmorOptionsFromCurrentTroop();
                return armorOptions.ContainsKey(EquipmentIndex.Head) &&
                       armorOptions[EquipmentIndex.Head].Any(opt => !opt.IsAtLimit);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Build armor options at runtime from the troop's upgrade branch (all troops leading to selected troop).
        /// This expands armor choices beyond just the current troop's single BattleEquipment loadout.
        /// At higher tiers (4+), we include armor from ALL tiers in the branch for more options.
        /// </summary>
        private Dictionary<EquipmentIndex, List<EquipmentVariantOption>> BuildArmorOptionsFromCurrentTroop()
        {
            var result = new Dictionary<EquipmentIndex, List<EquipmentVariantOption>>();
            try
            {
                var enlistment = EnlistmentBehavior.Instance;
                if (enlistment?.IsEnlisted != true)
                {
                    ModLogger.Debug("Quartermaster", "BuildArmorOptions: Not enlisted");
                    return result;
                }

                var selectedTroop = GetPlayerSelectedTroop();
                if (selectedTroop == null)
                {
                    ModLogger.Debug("Quartermaster", "BuildArmorOptions: No selected troop");
                    return result;
                }

                var culture = selectedTroop.Culture;
                var tier = enlistment.EnlistmentTier;
                
                ModLogger.Debug("Quartermaster", $"BuildArmorOptions: Tier={tier}, Culture={culture?.Name}, SelectedTroop={selectedTroop.Name}");

                // Build the troop branch (all troops leading to the selected troop)
                var branchNodes = BuildTroopBranchNodes(culture, selectedTroop, tier);
                
                // If no branch found, fall back to just the selected troop
                if (branchNodes.Count == 0)
                {
                    ModLogger.Debug("Quartermaster", "BuildArmorOptions: No branch nodes, using selected troop");
                    branchNodes.Add(selectedTroop);
                }
                
                ModLogger.Debug("Quartermaster", $"BuildArmorOptions: Branch has {branchNodes.Count} troops");
                foreach (var node in branchNodes)
                {
                    var loadoutCount = node.BattleEquipments?.Count() ?? 0;
                    ModLogger.Debug("Quartermaster", $"  Branch node: {node.Name} (Tier {SafeGetTier(node)}, Loadouts: {loadoutCount})");
                }

                // Collect armor variants - for tier 4+, use all tiers immediately for better selection
                Dictionary<EquipmentIndex, List<ItemObject>> armorVariants;
                if (tier >= 4)
                {
                    // Higher tiers should have access to more equipment variety
                    ModLogger.Debug("Quartermaster", $"BuildArmorOptions: Tier {tier} - using all branch tiers for variety");
                    armorVariants = CollectArmorVariantsFromAllTiers(branchNodes, culture);
                }
                else
                {
                    // Lower tiers start with exact tier, fall back to all tiers if needed
                    armorVariants = CollectArmorVariantsFromNodes(branchNodes, tier, culture);
                    if (armorVariants.Count == 0 || armorVariants.All(kvp => kvp.Value.Count == 0))
                    {
                        ModLogger.Debug("Quartermaster", $"BuildArmorOptions: No armor at exact tier {tier}, expanding to all branch tiers");
                        armorVariants = CollectArmorVariantsFromAllTiers(branchNodes, culture);
                    }
                }

                ModLogger.Info("Quartermaster", $"Collected armor variants from {branchNodes.Count} branch troops: " +
                    $"Body={GetSlotCount(armorVariants, EquipmentIndex.Body)}, " +
                    $"Head={GetSlotCount(armorVariants, EquipmentIndex.Head)}, " +
                    $"Gloves={GetSlotCount(armorVariants, EquipmentIndex.Gloves)}, " +
                    $"Leg={GetSlotCount(armorVariants, EquipmentIndex.Leg)}, " +
                    $"Cape={GetSlotCount(armorVariants, EquipmentIndex.Cape)}");

                // Convert to variant options (this version allows single items too)
                result = BuildVariantOptionsForArmor(armorVariants);

                // Keep only slots with choices (or current equipment)
                result = result.Where(kvp => kvp.Value is { Count: > 0 })
                               .ToDictionary(k => k.Key, v => v.Value);
                               
                ModLogger.Debug("Quartermaster", $"BuildArmorOptions: Final result has {result.Count} slots with options");
            }
            catch (Exception ex)
            {
                ModLogger.Error("Quartermaster", $"BuildArmorOptionsFromCurrentTroop failed: {ex.Message}");
            }
            return result;
        }

        /// <summary>
        /// Build weapon options at runtime from the troop's upgrade branch (all troops leading to selected troop).
        /// This expands weapon choices beyond just the current troop's single BattleEquipment loadout,
        /// ensuring players see all weapon variants available to their troop line (e.g., spears for spearmen).
        /// At higher tiers (4+), uses all tiers in the branch for maximum variety.
        /// </summary>
        private Dictionary<EquipmentIndex, List<EquipmentVariantOption>> BuildWeaponOptionsFromCurrentTroop()
        {
            var result = new Dictionary<EquipmentIndex, List<EquipmentVariantOption>>();
            try
            {
                var enlistment = EnlistmentBehavior.Instance;
                if (enlistment?.IsEnlisted != true)
                {
                    ModLogger.Debug("Quartermaster", "BuildWeaponOptions: Not enlisted");
                    return result;
                }

                var selectedTroop = GetPlayerSelectedTroop();
                if (selectedTroop == null)
                {
                    ModLogger.Debug("Quartermaster", "BuildWeaponOptions: No selected troop");
                    return result;
                }

                var culture = selectedTroop.Culture;
                var tier = enlistment.EnlistmentTier;
                
                ModLogger.Debug("Quartermaster", $"BuildWeaponOptions: Tier={tier}, Culture={culture?.Name}, SelectedTroop={selectedTroop.Name}");

                // Build the troop branch (all troops leading to the selected troop)
                var branchNodes = BuildTroopBranchNodes(culture, selectedTroop, tier);
                
                // If no branch found, fall back to just the selected troop
                if (branchNodes.Count == 0)
                {
                    ModLogger.Debug("Quartermaster", "BuildWeaponOptions: No branch nodes, using selected troop");
                    branchNodes.Add(selectedTroop);
                }
                
                ModLogger.Debug("Quartermaster", $"BuildWeaponOptions: Branch has {branchNodes.Count} troops");

                // Collect weapon variants - for tier 4+, use all tiers immediately for better selection
                Dictionary<EquipmentIndex, List<ItemObject>> weaponVariants;
                if (tier >= 4)
                {
                    // Higher tiers should have access to more weapon variety
                    ModLogger.Debug("Quartermaster", $"BuildWeaponOptions: Tier {tier} - using all branch tiers for variety");
                    weaponVariants = CollectWeaponVariantsFromAllTiers(branchNodes, culture);
                }
                else
                {
                    // Lower tiers start with exact tier, fall back to all tiers if needed
                    weaponVariants = CollectWeaponVariantsFromNodes(branchNodes, tier, culture);
                    if (weaponVariants.Count == 0 || weaponVariants.All(kvp => kvp.Value.Count == 0))
                    {
                        ModLogger.Debug("Quartermaster", $"BuildWeaponOptions: No weapons at exact tier {tier}, expanding to all branch tiers");
                        weaponVariants = CollectWeaponVariantsFromAllTiers(branchNodes, culture);
                    }
                }

                ModLogger.Info("Quartermaster", $"Collected weapon variants from {branchNodes.Count} branch troops: " +
                    $"Weapon0={GetSlotCount(weaponVariants, EquipmentIndex.Weapon0)}, " +
                    $"Weapon1={GetSlotCount(weaponVariants, EquipmentIndex.Weapon1)}, " +
                    $"Weapon2={GetSlotCount(weaponVariants, EquipmentIndex.Weapon2)}, " +
                    $"Weapon3={GetSlotCount(weaponVariants, EquipmentIndex.Weapon3)}");

                // Convert to variant options using the same pattern as armor
                result = BuildVariantOptionsForWeapons(weaponVariants);

                // Keep only slots with choices
                result = result.Where(kvp => kvp.Value is { Count: > 0 })
                               .ToDictionary(k => k.Key, v => v.Value);
                               
                ModLogger.Debug("Quartermaster", $"BuildWeaponOptions: Final result has {result.Count} slots with options");
            }
            catch (Exception ex)
            {
                ModLogger.Error("Quartermaster", $"BuildWeaponOptionsFromCurrentTroop failed: {ex.Message}");
            }
            return result;
        }

        /// <summary>
        /// Collect weapon variants from branch nodes at the exact tier.
        /// Weapons include all items in slots Weapon0 through Weapon3 (swords, spears, shields, bows, etc.).
        /// </summary>
        private Dictionary<EquipmentIndex, List<ItemObject>> CollectWeaponVariantsFromNodes(HashSet<CharacterObject> nodes, int exactTier, CultureObject culture)
        {
            var variants = new Dictionary<EquipmentIndex, List<ItemObject>>();
            try
            {
                var weaponSlots = new[] { EquipmentIndex.Weapon0, EquipmentIndex.Weapon1, EquipmentIndex.Weapon2, EquipmentIndex.Weapon3 };
                foreach (var troop in nodes)
                {
                    // Only gather from exact tier nodes to reflect the current tier's supply
                    if (SafeGetTier(troop) != exactTier)
                    {
                        continue;
                    }
                    foreach (var equipment in troop.BattleEquipments)
                    {
                        foreach (var slot in weaponSlots)
                        {
                            var item = equipment[slot].Item;
                            if (item == null)
                            {
                                continue;
                            }
                            // NOTE: Removed culture filter - items from a troop's own BattleEquipments are already
                            // validated by the game's troop data. Culture filter was incorrectly excluding valid items.
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
                ModLogger.Error("Quartermaster", $"CollectWeaponVariantsFromNodes failed: {ex.Message}");
            }
            return variants;
        }

        /// <summary>
        /// Collect weapon variants from all tiers in the branch (fallback when exact tier has no variants).
        /// This ensures players always see weapon options even if their exact tier has limited loadouts.
        /// </summary>
        private Dictionary<EquipmentIndex, List<ItemObject>> CollectWeaponVariantsFromAllTiers(HashSet<CharacterObject> nodes, CultureObject culture)
        {
            var variants = new Dictionary<EquipmentIndex, List<ItemObject>>();
            try
            {
                var weaponSlots = new[] { EquipmentIndex.Weapon0, EquipmentIndex.Weapon1, EquipmentIndex.Weapon2, EquipmentIndex.Weapon3 };
                foreach (var troop in nodes)
                {
                    if (troop.BattleEquipments == null)
                    {
                        continue;
                    }
                    
                    var loadoutCount = troop.BattleEquipments.Count();
                    ModLogger.Debug("Quartermaster", $"  Scanning weapons: {troop.Name?.ToString() ?? "Unknown"} ({loadoutCount} loadouts)");
                    
                    foreach (var equipment in troop.BattleEquipments)
                    {
                        foreach (var slot in weaponSlots)
                        {
                            var item = equipment[slot].Item;
                            if (item == null)
                            {
                                continue;
                            }
                            
                            // NOTE: Removed culture filter - items from a troop's own BattleEquipments are already
                            // validated by the game's troop data. Culture filter was incorrectly excluding valid items.
                            var isShield = item.WeaponComponent?.PrimaryWeapon?.IsShield == true;
                            var weaponType = item.WeaponComponent?.GetItemType().ToString() ?? "unknown";
                            
                            if (!variants.ContainsKey(slot))
                            {
                                variants[slot] = new List<ItemObject>();
                            }
                            
                            if (!variants[slot].Contains(item))
                            {
                                variants[slot].Add(item);
                                var shieldTag = isShield ? " [SHIELD]" : "";
                                ModLogger.Debug("Quartermaster", $"    {slot}: {item.Name} ({weaponType}){shieldTag} - ADDED");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("Quartermaster", $"CollectWeaponVariantsFromAllTiers failed: {ex.Message}");
            }
            return variants;
        }

        /// <summary>
        /// Build variant options for weapons - allows even single items to show (so player can see current equipment).
        /// Uses the same pattern as BuildVariantOptionsForArmor for consistency.
        /// </summary>
        private Dictionary<EquipmentIndex, List<EquipmentVariantOption>> BuildVariantOptionsForWeapons(
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
                    if (items == null || items.Count == 0)
                    {
                        continue;
                    }

                    var currentItem = hero.BattleEquipment[slot].Item;
                    var optionList = new List<EquipmentVariantOption>();
                    foreach (var item in items)
                    {
                        var cost = CalculateVariantCost(item, currentItem, slot);
                        
                        // Check if player has hit the slot-specific limit (weapons=2, armor=1)
                        var itemCount = GetPlayerItemCount(hero, item.StringId);
                        var itemLimit = GetSlotItemLimit(slot);
                        var isAtLimit = itemCount >= itemLimit;
                        
                        // Weapons always allow duplicate purchases (soldiers can carry multiple)
                        var allowsDuplicate = IsWeaponSlot(slot) || IsConsumableItem(item);
                        
                        optionList.Add(new EquipmentVariantOption
                        {
                            Item = item,
                            Cost = cost,
                            IsCurrent = item == currentItem,
                            CanAfford = !isAtLimit, // Can get if not at limit (equipment is free)
                            Slot = slot,
                            IsOfficerExclusive = false,
                            AllowsDuplicatePurchase = allowsDuplicate,
                            IsAtLimit = isAtLimit
                        });
                    }

                    // Sort: current item first, then by cost ascending
                    optionList = optionList.OrderBy(o => o.IsCurrent ? 0 : 1).ThenBy(o => o.Cost).ToList();
                    finalOptions[slot] = optionList;
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("Quartermaster", $"BuildVariantOptionsForWeapons failed: {ex.Message}");
            }
            return finalOptions;
        }

        /// <summary>
        /// Collect armor variants from all tiers in the branch (fallback when exact tier has no variants).
        /// </summary>
        private Dictionary<EquipmentIndex, List<ItemObject>> CollectArmorVariantsFromAllTiers(HashSet<CharacterObject> nodes, CultureObject culture)
        {
            var variants = new Dictionary<EquipmentIndex, List<ItemObject>>();
            try
            {
                var armorSlots = new[] { EquipmentIndex.Body, EquipmentIndex.Head, EquipmentIndex.Gloves, EquipmentIndex.Leg, EquipmentIndex.Cape };
                foreach (var troop in nodes)
                {
                    if (troop.BattleEquipments == null)
                    {
                        continue;
                    }
                    
                    var loadoutCount = troop.BattleEquipments.Count();
                    ModLogger.Debug("Quartermaster", $"  Scanning {troop.Name?.ToString() ?? "Unknown"} ({loadoutCount} loadouts)");
                    
                    foreach (var equipment in troop.BattleEquipments)
                    {
                        foreach (var slot in armorSlots)
                        {
                            var item = equipment[slot].Item;
                            if (item == null)
                            {
                                continue;
                            }
                            
                            // Log every item we find
                            var hasArmorComp = item.ArmorComponent != null;
                            var itemCulture = item.Culture?.Name?.ToString() ?? "none";
                            
                            if (!hasArmorComp)
                            {
                                ModLogger.Debug("Quartermaster", $"    {slot}: {item.Name} - SKIP (no ArmorComponent)");
                                continue;
                            }
                            
                            // NOTE: Removed culture filter - items from a troop's own BattleEquipments are already
                            // validated by the game's troop data. Culture filter was incorrectly excluding valid items.
                            
                            if (!variants.ContainsKey(slot))
                            {
                                variants[slot] = new List<ItemObject>();
                            }
                            
                            if (!variants[slot].Contains(item))
                            {
                                variants[slot].Add(item);
                                ModLogger.Debug("Quartermaster", $"    {slot}: {item.Name} - ADDED (culture: {itemCulture})");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("Quartermaster", $"CollectArmorVariantsFromAllTiers failed: {ex.Message}");
            }
            return variants;
        }

        /// <summary>
        /// Build variant options for armor - allows even single items to show (so player can see current equipment).
        /// Armor does not allow duplicates (you can only wear one piece per slot).
        /// </summary>
        private Dictionary<EquipmentIndex, List<EquipmentVariantOption>> BuildVariantOptionsForArmor(
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
                    if (items == null || items.Count == 0)
                    {
                        continue;
                    }

                    var currentItem = hero.BattleEquipment[slot].Item;
                    var optionList = new List<EquipmentVariantOption>();
                    foreach (var item in items)
                    {
                        var cost = CalculateVariantCost(item, currentItem, slot);
                        
                        // Check if player has hit the slot-specific limit (armor=1, weapons=2)
                        var itemCount = GetPlayerItemCount(hero, item.StringId);
                        var itemLimit = GetSlotItemLimit(slot);
                        var isAtLimit = itemCount >= itemLimit;
                        var isCurrent = item == currentItem;
                        
                        optionList.Add(new EquipmentVariantOption
                        {
                            Item = item,
                            Cost = cost,
                            IsCurrent = isCurrent,
                            CanAfford = !isAtLimit, // Can get if not at limit (equipment is free)
                            Slot = slot,
                            IsOfficerExclusive = false,
                            AllowsDuplicatePurchase = false, // Armor doesn't allow duplicates
                            IsAtLimit = isAtLimit
                        });
                    }

                    optionList = optionList.OrderBy(o => o.IsCurrent ? 0 : 1).ThenBy(o => o.Cost).ToList();
                    finalOptions[slot] = optionList;
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("Quartermaster", $"BuildVariantOptionsForArmor failed: {ex.Message}");
            }
            return finalOptions;
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
                    catch (Exception ex)
                    {
                        ModLogger.Debug("Quartermaster", $"Error in CollectArmorVariantsFromNodes: {ex.Message}");
                    }
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
                            // Safety filter: ensure true armor component exists
                            // NOTE: Removed culture filter here - items from a troop's own BattleEquipments are already
                            // validated by the game's troop data. The culture filter was incorrectly excluding items like
                            // leather_cap (empire culture) from vlandian_recruit even though the troop legitimately uses it.
                            if (item.ArmorComponent == null)
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

        [System.Diagnostics.CodeAnalysis.SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "May be used for future exact variant matching")]
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
                        
                        // Check if player has hit the slot-specific limit (weapons=2, armor=1)
                        var itemCount = GetPlayerItemCount(hero, item.StringId);
                        var itemLimit = GetSlotItemLimit(slot);
                        var isAtLimit = itemCount >= itemLimit;
                        
                        // Weapons and consumables allow duplicates
                        var allowsDuplicate = IsWeaponSlot(slot) || IsConsumableItem(item);
                        
                        optionList.Add(new EquipmentVariantOption
                        {
                            Item = item,
                            Cost = cost,
                            IsCurrent = item == currentItem,
                            CanAfford = !isAtLimit, // Can get if not at limit (equipment is free)
                            Slot = slot,
                            IsOfficerExclusive = false,
                            AllowsDuplicatePurchase = allowsDuplicate,
                            IsAtLimit = isAtLimit
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

        [System.Diagnostics.CodeAnalysis.SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "May be used for future armor slot selection UI")]
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

        private int GetSlotCount(Dictionary<EquipmentIndex, List<ItemObject>> dict, EquipmentIndex slot)
        {
            return dict.TryGetValue(slot, out var list) ? list?.Count ?? 0 : 0;
        }
        
        /// <summary>
        /// Handle helmet variant selection.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "May be used for future helmet variant selection")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "Required by menu callback signature")]
        private void OnHelmetVariantsSelected(MenuCallbackArgs args)
        {
            try
            {
                var armorOptions = BuildArmorOptionsFromCurrentTroop();
                if (armorOptions.ContainsKey(EquipmentIndex.Head) && armorOptions[EquipmentIndex.Head].Count > 0)
                {
                    ShowEquipmentVariantSelectionDialog(armorOptions[EquipmentIndex.Head], "helmet");
                }
                else
                {
                    InformationManager.DisplayMessage(new InformationMessage(
                        new TextObject("{=qm_no_helmet_variants}No helmet variants available for your troop type.").ToString()));
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("Quartermaster", "Error selecting helmet variants", ex);
            }
        }
        
        /// <summary>
        /// Check if accessory variants are available (capes and shields).
        /// Shields are technically weapons but logically fit in accessories for player convenience.
        /// </summary>
        private bool IsAccessoryVariantsAvailable(MenuCallbackArgs args)
        {
            _ = args; // Required by API contract
            try
            {
                // Capes from armor slots - available if not at limit
                var armorOptions = BuildArmorOptionsFromCurrentTroop();
                var hasCapes = armorOptions.ContainsKey(EquipmentIndex.Cape) &&
                               armorOptions[EquipmentIndex.Cape].Any(opt => !opt.IsAtLimit);
                
                // Shields from weapon slots - available if not at limit
                var shieldOptions = BuildShieldOptionsFromWeapons();
                var hasShields = shieldOptions.Any(opt => !opt.IsAtLimit);
                
                ModLogger.Debug("Quartermaster", $"Accessories check: Capes={hasCapes}, Shields={shieldOptions.Count}");
                
                return hasCapes || hasShields;
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// Handle accessory variant selection (capes and shields).
        /// </summary>
        private void OnAccessoryVariantsSelected(MenuCallbackArgs args)
        {
            try
            {
                var combined = new List<EquipmentVariantOption>();
                
                // Get capes from armor options
                var armorOptions = BuildArmorOptionsFromCurrentTroop();
                if (armorOptions.TryGetValue(EquipmentIndex.Cape, out var capeOptions))
                {
                    combined.AddRange(capeOptions);
                }
                
                // Add shields from weapon slots
                var shieldOptions = BuildShieldOptionsFromWeapons();
                combined.AddRange(shieldOptions);
                
                ModLogger.Info("Quartermaster", $"Accessory selection: {combined.Count} total items (Capes, Shields)");
                
                // Show dialog if any items are not at limit
                if (combined.Any(opt => !opt.IsAtLimit))
                {
                    ShowEquipmentVariantSelectionDialog(combined, "accessories");
                }
                else
                {
                    InformationManager.DisplayMessage(new InformationMessage(
                        new TextObject("{=qm_no_accessory_variants}No accessory variants available.").ToString()));
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("Quartermaster", "Error selecting accessory variants", ex);
            }
        }
        
        /// <summary>
        /// Build shield options from weapon slots.
        /// Shields are technically weapons but grouped with accessories for player convenience.
        /// </summary>
        private List<EquipmentVariantOption> BuildShieldOptionsFromWeapons()
        {
            var shields = new List<EquipmentVariantOption>();
            try
            {
                var weaponOptions = BuildWeaponOptionsFromCurrentTroop();
                
                // Check all weapon slots for shields
                foreach (var kvp in weaponOptions)
                {
                    foreach (var option in kvp.Value)
                    {
                        // Check if this item is a shield
                        if (option.Item?.WeaponComponent?.PrimaryWeapon?.IsShield == true)
                        {
                            shields.Add(option);
                            ModLogger.Debug("Quartermaster", $"Found shield: {option.Item.Name} in slot {kvp.Key}");
                        }
                    }
                }
                
                ModLogger.Debug("Quartermaster", $"BuildShieldOptions: Found {shields.Count} shields");
            }
            catch (Exception ex)
            {
                ModLogger.Error("Quartermaster", $"BuildShieldOptionsFromWeapons failed: {ex.Message}");
            }
            return shields;
        }
        
        /// <summary>
        /// Check if supply management is available (requires provisioner duty).
        /// </summary>
        private bool IsSupplyManagementAvailable(MenuCallbackArgs args)
        {
            _ = args; // Required by API contract
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
                ActivateMenuPreserveTime("quartermaster_supplies");
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
            // Supply management menu (wait menu with hidden progress for spacebar support)
            starter.AddWaitGameMenu(
                "quartermaster_supplies",
                "Supply Management\n{SUPPLY_TEXT}",
                OnSupplyManagementInit,
                QuartermasterWaitCondition,
                QuartermasterWaitConsequence,
                QuartermasterWaitTick,
                GameMenu.MenuAndOptionType.WaitMenuHideProgressAndHoursOption);
                
            // Food optimization option (Manage icon)
            starter.AddGameMenuOption("quartermaster_supplies", "optimize_food",
                "Optimize food supplies",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Manage;
                    return IsFoodOptimizationAvailable(args);
                },
                OnFoodOptimizationSelected,
                false, 1);
                
            // Inventory management option (Manage icon)
            starter.AddGameMenuOption("quartermaster_supplies", "manage_inventory",
                "Reorganize party inventory",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Manage;
                    return IsInventoryManagementAvailable(args);
                },
                OnInventoryManagementSelected,
                false, 2);
                
            // Supply purchase option (Trade icon)
            starter.AddGameMenuOption("quartermaster_supplies", "purchase_supplies",
                "Purchase additional supplies",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Trade;
                    return IsSupplyPurchaseAvailable(args);
                },
                OnSupplyPurchaseSelected,
                false, 3);
            
            // Return to quartermaster (Leave icon)
            starter.AddGameMenuOption("quartermaster_supplies", "supplies_back",
                "Return to quartermaster",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Leave;
                    return true;
                },
                _ => ActivateMenuPreserveTime("quartermaster_equipment"));
        }
        
        /// <summary>
        /// Initialize supply management menu display.
        /// </summary>
        private void OnSupplyManagementInit(MenuCallbackArgs args)
        {
            try
            {
                // Time state is captured by caller before menu transition (not here - too late)
                
                // Background is now set by GameMenuInitializationHandler
                var sb = new StringBuilder();
                var party = MobileParty.MainParty;
                var duties = EnlistedDutiesBehavior.Instance;
                
                sb.AppendLine("— Supply Status —");
                sb.AppendLine();
                
                // Current supply status with cleaner formatting
                sb.AppendLine($"Inventory: {party.TotalWeightCarried:F1} / {party.InventoryCapacity:F1} capacity");
                sb.AppendLine($"Food Supplies: {party.Food:F1} (consumption: {party.FoodChange:F2}/day)");
                sb.AppendLine($"Morale: {party.Morale:F1} / 100");
                sb.AppendLine();
                
                // Officer benefits display with modern formatting
                if (duties?.GetCurrentOfficerRole() == "Quartermaster")
                {
                    sb.AppendLine("— Officer Benefits —");
                    sb.AppendLine("• +50 party carry capacity");
                    sb.AppendLine("• +15% food efficiency");
                    sb.AppendLine("• Enhanced supply management options");
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
            _ = args; // Required by API contract
            var party = MobileParty.MainParty;
            return party is { Food: > 0, FoodChange: < 0 }; // Has food but consuming it
        }
        
        private void OnFoodOptimizationSelected(MenuCallbackArgs args)
        {
            // Quartermaster food efficiency bonus implementation
            var party = MobileParty.MainParty;
            var foodBonus = (int)(party.Food * 0.05f); // 5% food efficiency bonus
            
            if (foodBonus > 0)
            {
                party.ItemRoster.AddToCounts(MBObjectManager.Instance.GetObject<ItemObject>("grain"), foodBonus);
                
                var message = new TextObject("{=qm_optimized_food}Optimized food supplies: +{AMOUNT} grain from efficient rationing.");
                message.SetTextVariable("AMOUNT", foodBonus.ToString());
                InformationManager.DisplayMessage(new InformationMessage(message.ToString()));
                
                ModLogger.Info("Quartermaster", $"Food optimization applied: +{foodBonus} grain");
            }
        }
        
        private bool IsInventoryManagementAvailable(MenuCallbackArgs args)
        {
            _ = args; // Required by API contract
            var party = MobileParty.MainParty;
            // 1.3.4 API: TotalWeight moved from ItemRoster to MobileParty.TotalWeightCarried
            return party != null && party.TotalWeightCarried > party.InventoryCapacity * 0.8f; // Over 80% capacity
        }
        
        private void OnInventoryManagementSelected(MenuCallbackArgs args)
        {
            // Quartermaster inventory optimization
            var party = MobileParty.MainParty;
            var capacityBonus = party.InventoryCapacity * 0.05f; // 5% temporary capacity bonus
            
            var message = new TextObject("{=qm_reorganized_inventory}Reorganized inventory: +{AMOUNT} temporary carry capacity.");
            message.SetTextVariable("AMOUNT", capacityBonus.ToString("F0"));
            InformationManager.DisplayMessage(new InformationMessage(message.ToString()));
            
            ModLogger.Info("Quartermaster", $"Inventory optimization applied: +{capacityBonus:F0} capacity");
        }
        
        private bool IsSupplyPurchaseAvailable(MenuCallbackArgs args)
        {
            _ = args; // Required by API contract
            return Hero.MainHero.Gold >= 50; // Can afford basic supplies
        }
        
        private void OnSupplyPurchaseSelected(MenuCallbackArgs args)
        {
            // Basic supply purchase system
            var cost = 50;
            if (Hero.MainHero.Gold >= cost)
            {
                GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, cost); // Default disableNotification=false is sufficient
                
                // Add basic supplies
                var party = MobileParty.MainParty;
                party.ItemRoster.AddToCounts(MBObjectManager.Instance.GetObject<ItemObject>("grain"), 5);
                party.ItemRoster.AddToCounts(MBObjectManager.Instance.GetObject<ItemObject>("tools"), 2);
                
                InformationManager.DisplayMessage(new InformationMessage(
                    new TextObject("{=qm_purchased_supplies}Purchased basic supplies: 5 grain, 2 tools.").ToString()));
                    
                ModLogger.Info("Quartermaster", "Supply purchase completed");
            }
        }
        
        /// <summary>
        /// Check if a specific variant option is available and visible.
        /// </summary>
        private bool IsVariantOptionAvailable(MenuCallbackArgs _, int optionIndex)
        {
            try
            {
                if (_selectedSlot == EquipmentIndex.None || !_availableVariants.TryGetValue(_selectedSlot, out var options))
                {
                    return false;
                }
                if (optionIndex > options.Count)
                {
                    return false;
                }
                
                // Validate option index and check if option exists
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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "Required by menu callback signature - parameter name indicates it's intentionally unused")]
        private void OnVariantOptionSelected(MenuCallbackArgs _, int optionIndex)
        {
            try
            {
                if (_selectedSlot == EquipmentIndex.None || !_availableVariants.TryGetValue(_selectedSlot, out var options))
                {
                    return;
                }
                if (optionIndex > options.Count)
                {
                    return;
                }
                
                var selectedOption = options[optionIndex - 1];
                
                // Block purchase only for non-duplicate items that are already equipped
                if (selectedOption.IsCurrent && !selectedOption.AllowsDuplicatePurchase)
                {
                    InformationManager.DisplayMessage(new InformationMessage(
                        new TextObject("{=qm_already_have_variant}You already have this equipment variant.").ToString()));
                    return;
                }
                
                if (!selectedOption.CanAfford)
                {
                    var message = new TextObject("{=qm_insufficient_funds}Insufficient funds. You need {COST} denars.");
                    message.SetTextVariable("COST", selectedOption.Cost.ToString());
                    InformationManager.DisplayMessage(new InformationMessage(message.ToString()));
                    return;
                }
                
                // Process the equipment variant request
                RequestEquipmentVariant(selectedOption.Item, selectedOption.Slot);
                
                // Return to main quartermaster menu
                ActivateMenuPreserveTime("quartermaster_equipment");
            }
            catch (Exception ex)
            {
                ModLogger.Error("Quartermaster", "Error selecting variant option", ex);
            }
        }
        
        /// <summary>
        /// Build display text for a variant option.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "May be used for future variant text formatting")]
        private string BuildVariantOptionText(EquipmentVariantOption option)
        {
            var statusText = option.IsCurrent ? "(Current)" :
                           option.CanAfford ? $"({option.Cost} denars)" :
                           $"({option.Cost} denars - Can't afford)";
            
            var marker = option.IsCurrent ? "*" : "-"; // Use simple ASCII characters
            return $"{marker} {option.Item.Name} {statusText}";
        }
        
        /// <summary>
        /// Check if the given equipment slot is a weapon slot (Weapon0 through Weapon3).
        /// Soldiers can carry multiple weapons in different slots.
        /// </summary>
        private static bool IsWeaponSlot(EquipmentIndex slot)
        {
            return slot is >= EquipmentIndex.Weapon0 and <= EquipmentIndex.Weapon3;
        }
        
        /// <summary>
        /// Check if an item is consumable (arrows, bolts, throwing weapons, etc).
        /// Consumable items can be stacked and soldiers may want multiple stacks.
        /// </summary>
        private static bool IsConsumableItem(ItemObject item)
        {
            if (item?.WeaponComponent?.PrimaryWeapon == null)
            {
                return false;
            }
            
            return item.WeaponComponent.PrimaryWeapon.IsConsumable;
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
        
        /// <summary>
        /// True if this item allows duplicate purchases (weapons and consumables like arrows/bolts).
        /// Soldiers can carry multiple weapons in different slots or multiple stacks of consumables.
        /// </summary>
        public bool AllowsDuplicatePurchase { get; set; }
        
        /// <summary>
        /// True if the player has reached the item limit (2 per type) for this item.
        /// Prevents abuse of the free equipment system.
        /// </summary>
        public bool IsAtLimit { get; set; }
    }
}
