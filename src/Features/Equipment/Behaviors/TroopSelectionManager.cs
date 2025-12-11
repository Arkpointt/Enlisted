using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.Core;
using TaleWorlds.Core.ImageIdentifiers; // 1.3.4 API: ImageIdentifier moved here
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
    /// Troop selection system allowing players to choose from real Bannerlord troop templates.
    /// 
    /// This system allows players to select actual game troops during promotion, providing
    /// authentic military progression where equipment is replaced (not accumulated) for
    /// realistic military service. Players can choose troops up to their current tier level.
    /// </summary>
    public sealed class TroopSelectionManager : CampaignBehaviorBase
    {
        public static TroopSelectionManager Instance { get; private set; }
        
        // Promotion state tracking
        private bool _promotionPending;
        private int _pendingTier = 1;
        private List<CharacterObject> _availableTroops = new List<CharacterObject>();
        private string _lastSelectedTroopId;
        
        // Equipment accountability tracking - tracks what gear has been issued to the soldier
        // Used to charge for missing equipment when changing troop types
        private Dictionary<int, IssuedItemRecord> _issuedEquipment = new Dictionary<int, IssuedItemRecord>();

        public string LastSelectedTroopId => _lastSelectedTroopId;
        
        /// <summary>
        /// Gets the currently issued equipment for accountability tracking.
        /// </summary>
        public IReadOnlyDictionary<int, IssuedItemRecord> IssuedEquipment => _issuedEquipment;
        
        public TroopSelectionManager()
        {
            Instance = this;
        }
        
        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        }
        
        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("_promotionPending", ref _promotionPending);
            dataStore.SyncData("_pendingTier", ref _pendingTier);
            dataStore.SyncData("_lastSelectedTroopId", ref _lastSelectedTroopId);
            SerializeIssuedEquipment(dataStore);
        }
        
        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            AddTroopSelectionMenus(starter);
            
            // DEVELOPMENT: Validate troop coverage across all factions
            TroopDiscoveryValidator.ValidateAllCulturesAndTiers();
            
            ModLogger.Info("TroopSelection", "Troop selection system initialized with modern UI styling");
        }
        
        /// <summary>
        /// Menu background initialization for enlisted_troop_selection menu.
        /// Sets culture-appropriate background and ambient audio.
        /// </summary>
        [GameMenuInitializationHandler("enlisted_troop_selection")]
        private static void OnTroopSelectionBackgroundInit(MenuCallbackArgs args)
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
        /// Show Master at Arms popup allowing player to select among unlocked troops (tiers ≤ current tier).
        /// Keeps tier unchanged; only equipment/role expression is changed.
        /// </summary>
        public void ShowMasterAtArmsPopup()
        {
            try
            {
                var enlistment = EnlistmentBehavior.Instance;
                if (enlistment?.IsEnlisted != true)
                {
                    InformationManager.DisplayMessage(new InformationMessage(
                        new TextObject("{=eq_must_be_enlisted}You must be enlisted to use Master at Arms.").ToString()));
                    return;
                }

                var cultureId = enlistment?.CurrentLord?.Culture?.StringId;
                if (string.IsNullOrEmpty(cultureId))
                {
                    ModLogger.Error("Equipment", "Master at Arms: Missing culture on current lord");
                    return;
                }

                // Use pending tier if promotion is active, otherwise current tier
                var effectiveTier = _promotionPending ? _pendingTier : enlistment.EnlistmentTier;
                var unlocked = GetUnlockedTroopsForCurrentTier(cultureId, effectiveTier);
                if (unlocked.Count == 0)
                {
                    InformationManager.DisplayMessage(new InformationMessage(
                        new TextObject("{=eq_no_eligible_troops}No eligible troops found for your rank and culture.").ToString()));
                    return;
                }

                var options = new List<InquiryElement>();
                foreach (var troop in unlocked)
                {
                    var hint = BuildTroopLoadoutHint(troop);
                    var name = troop.Name?.ToString() ?? "Unknown";
                    // 1.3.4 API: ImageIdentifier is now abstract, use CharacterImageIdentifier
                    var portrait = new CharacterImageIdentifier(CharacterCode.CreateFrom(troop));
                    options.Add(new InquiryElement(troop, name, portrait, true, hint));
                }

                var data = new MultiSelectionInquiryData(
                    "Select equipment to use",
                    "Gear will not be auto-issued after Tier 1. Your chosen kit becomes purchasable at the Quartermaster.",
                    options,
                    true, // Enable close button (X) like lord selection dialog
                    1,
                    1,
                    "Continue",
                    "Cancel",
                    selected =>
                    {
                        try
                        {
                            if (selected?.FirstOrDefault()?.Identifier is CharacterObject chosen)
                            {
                                ModLogger.Info("TroopSelection", $"Player selected troop: {chosen.Name} (ID: {chosen.StringId}, Tier: {SafeGetTier(chosen)}, Formation: {DetectTroopFormation(chosen)})");
                                var autoIssue = SafeGetTier(chosen) <= 1; // Tier 1 keeps auto-issue; higher tiers unlock for purchase
                                ApplySelectedTroopEquipment(Hero.MainHero, chosen, autoIssue);
                                _lastSelectedTroopId = chosen.StringId;
                                // Don't re-capture time - preserve the time state from when the button was clicked
                                // Just refresh the menu without affecting time control
                                if (Campaign.Current?.CurrentMenuContext != null)
                                {
                                    Campaign.Current.GameMenuManager.RefreshMenuOptions(Campaign.Current.CurrentMenuContext);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            ModLogger.Error("Equipment", $"Master at Arms apply failed: {ex.Message}", ex);
                        }
                    },
                    _ =>
                    {
                        // Cancel action - just close popup, don't affect menu or time state
                        // The enlisted_status menu is already active underneath
                    },
                    string.Empty);

                MBInformationManager.ShowMultiSelectionInquiry(data);
            }
            catch (Exception ex)
            {
                ModLogger.Error("Equipment", $"Master at Arms popup failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Build unlocked troop list for culture across tiers ≤ current tier.
        /// </summary>
        public List<CharacterObject> GetUnlockedTroopsForCurrentTier(string cultureId, int currentTier)
        {
            try
            {
                var culture = MBObjectManager.Instance.GetObject<CultureObject>(cultureId);
                var tree = BuildCultureTroopTree(culture);
                var troops = tree.Where(t =>
                        !t.IsHero &&
                        t.BattleEquipments.Any() &&
                        SafeGetTier(t) <= currentTier)
                    .OrderBy(SafeGetTier)
                    .ThenBy(t => t.Name?.ToString())
                    .ToList();

                if (troops.Count == 0 && culture != null)
                {
                    var fallback = new List<CharacterObject>();
                    if (culture.BasicTroop != null)
                    {
                        fallback.Add(culture.BasicTroop);
                    }
                    if (culture.EliteBasicTroop != null)
                    {
                        fallback.Add(culture.EliteBasicTroop);
                    }
                    if (culture.MeleeMilitiaTroop != null)
                    {
                        fallback.Add(culture.MeleeMilitiaTroop);
                    }
                    if (culture.RangedMilitiaTroop != null)
                    {
                        fallback.Add(culture.RangedMilitiaTroop);
                    }
                    troops = fallback.Where(t => t != null && t.BattleEquipments.Any()).ToList();
                }

                return troops;
            }
            catch (Exception ex)
            {
                ModLogger.Error("Equipment", $"GetUnlockedTroops failed: {ex.Message}");
                return new List<CharacterObject>();
            }
        }

        private int SafeGetTier(CharacterObject troop)
        {
            try { return troop.GetBattleTier(); } catch { return 1; }
        }

        private List<InquiryElement> BuildInquiryElements(List<CharacterObject> troops)
        {
            var elements = new List<InquiryElement>();
            foreach (var troop in troops)
            {
                try
                {
                    var title = troop?.Name?.ToString() ?? "Unknown";
                    var element = new InquiryElement(
                        troop,
                        title,
                        null,
                        true,
                        null);
                    elements.Add(element);
                }
                catch
                {
                    // Best effort; skip this troop if element creation fails
                }
            }
            return elements;
        }

        private string BuildTroopLoadoutHint(CharacterObject troop)
        {
            try
            {
                var lines = new List<string>();
                var best = troop.BattleEquipments?.FirstOrDefault();
                if (best != null)
                {
                    for (var slot = EquipmentIndex.Weapon0; slot <= EquipmentIndex.HorseHarness; slot++)
                    {
                        var item = best[slot].Item;
                        if (item == null)
                        {
                            continue;
                        }
                        lines.Add(item.Name?.ToString() ?? item.StringId);
                    }
                }
                return string.Join("\n", lines);
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        /// Add troop selection menus for promotion system.
        /// </summary>
        private void AddTroopSelectionMenus(CampaignGameStarter starter)
        {
            // Main troop selection menu
            // NOTE: Use MenuOverlayType.None to avoid showing empty battle bar
            starter.AddGameMenu("enlisted_troop_selection",
                "Master at Arms\n{TROOP_SELECTION_TEXT}",
                OnTroopSelectionInit);
                
            // "Collect equipment now" button - opens Master at Arms popup for troop selection (TroopSelection icon)
            starter.AddGameMenuOption("enlisted_troop_selection", "troop_selection_collect_now",
                "Collect equipment now",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.TroopSelection;
                    return _promotionPending && _availableTroops.Count > 0;
                },
                _ =>
                {
                    NextFrameDispatcher.RunNextFrame(() =>
                    {
                        try
                        {
                            ShowMasterAtArmsPopup();
                            ModLogger.Info("TroopSelection", "Player chose to collect equipment now - opening Master at Arms");
                        }
                        catch (Exception ex)
                        {
                            ModLogger.Error("TroopSelection", $"Failed to open Master at Arms popup: {ex.Message}");
                        }
                    });
                },
                false, 0);
            
            // "Return to camp" button - player declines immediate equipment collection (Leave icon)
            starter.AddGameMenuOption("enlisted_troop_selection", "troop_selection_back",
                "Return to camp",
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
                            GameMenu.SwitchToMenu("enlisted_status");
                            ModLogger.Info("TroopSelection", "Player declined equipment - returned to camp");
                        }
                        catch (Exception ex)
                        {
                            ModLogger.Error("TroopSelection", $"Failed to switch back to enlisted status: {ex.Message}");
                            // Fallback to safe activation if direct switch failed
                            EnlistedMenuBehavior.SafeActivateEnlistedMenu();
                        }
                    });
                });
        }
        
        /// <summary>
        /// Show troop selection menu for a specific tier.
        /// Called when player reaches promotion XP threshold.
        /// </summary>
        public void ShowTroopSelectionMenu(int newTier)
        {
            try
            {
                var enlistment = EnlistmentBehavior.Instance;
                if (enlistment?.IsEnlisted != true)
                {
                    ModLogger.Error("TroopSelection", "Cannot show troop selection - player not enlisted");
                    return;
                }

                var cultureId = enlistment.CurrentLord?.Culture?.StringId;
                if (string.IsNullOrEmpty(cultureId))
                {
                    ModLogger.Error("TroopSelection", "Cannot show troop selection - missing culture on current lord");
                    return;
                }
                _availableTroops = GetTroopsForCultureAndTier(cultureId, newTier);
                _pendingTier = newTier;
                _promotionPending = true;
                
                if (_availableTroops.Count == 0)
                {
                    ModLogger.Error("TroopSelection", $"No troops found for culture {cultureId} tier {newTier}");
                    
                    // Fallback - apply basic tier progression without equipment change
                    enlistment.ApplyBasicPromotion(newTier);
                    return;
                }
                
                // Activate troop selection menu
                GameMenu.ActivateGameMenu("enlisted_troop_selection");
                
                ModLogger.Info("TroopSelection", $"Troop selection menu opened - {_availableTroops.Count} troops available");
            }
            catch (Exception ex)
            {
                ModLogger.Error("TroopSelection", "Failed to show troop selection menu", ex);
            }
        }
        
        /// <summary>
        /// Initialize troop selection menu with available troop choices.
        /// </summary>
        private void OnTroopSelectionInit(MenuCallbackArgs args)
        {
            try
            {
                // 1.3.4+: Set proper menu background to avoid assertion failure
                var backgroundMesh = "encounter_looter"; // Safe fallback
                var enlistment = EnlistmentBehavior.Instance;
                
                if (enlistment?.CurrentLord?.Clan?.Kingdom?.Culture?.EncounterBackgroundMesh != null)
                {
                    backgroundMesh = enlistment.CurrentLord.Clan.Kingdom.Culture.EncounterBackgroundMesh;
                }
                else if (enlistment?.CurrentLord?.Culture?.EncounterBackgroundMesh != null)
                {
                    backgroundMesh = enlistment.CurrentLord.Culture.EncounterBackgroundMesh;
                }
                
                args.MenuContext.SetBackgroundMeshName(backgroundMesh);
                
                if (!_promotionPending || _availableTroops.Count == 0)
                {
                    // Initialize menu context
                    MBTextManager.SetTextVariable("TROOP_SELECTION_TEXT", "No promotion available at this time.");
                    return;
                }

                // Build roleplay-friendly promotion display
                var enlistmentRef = EnlistmentBehavior.Instance;
                var rankName = enlistmentRef?.GetRankName(_pendingTier) ?? $"Tier {_pendingTier}";
                
                var statusText = "You've been summoned before the Master at Arms.\n\n";
                statusText += $"\"Soldier, your service has not gone unnoticed. You've earned promotion to {rankName}.\"\n\n";
                statusText += "After Tier 1, gear is not auto-issued. Your chosen kit becomes purchasable from the Quartermaster.\n\n";
                statusText += $"({_availableTroops.Count} troop specializations available)";
                
                MBTextManager.SetTextVariable("TROOP_SELECTION_TEXT", statusText);
                
                // Dynamically create menu options for each troop
                CreateTroopSelectionOptions(args);
            }
            catch (Exception ex)
            {
                ModLogger.Error("TroopSelection", "Error initializing troop selection menu", ex);
                MBTextManager.SetTextVariable("TROOP_SELECTION_TEXT", "Error loading troop selection. Please report this issue.");
            }
        }
        
        /// <summary>
        /// Create dynamic menu options for each available troop.
        /// </summary>
        private void CreateTroopSelectionOptions(MenuCallbackArgs _)
        {
            try
            {
                // Troop selection is handled through the popup dialog system
                // Players choose from available troops displayed in a selection dialog
                var troopCount = Math.Min(_availableTroops.Count, 8);
                ModLogger.Info("TroopSelection", $"Created {troopCount} troop selection options");
            }
            catch (Exception ex)
            {
                ModLogger.Error("TroopSelection", "Error creating troop selection options", ex);
            }
        }
        
        /// <summary>
        /// Get available troops for specific culture and tier.
        /// Uses real Bannerlord CharacterObject templates.
        /// </summary>
        public List<CharacterObject> GetTroopsForCultureAndTier(string cultureId, int tier)
        {
            try
            {
                var culture = MBObjectManager.Instance.GetObject<CultureObject>(cultureId);
                var tree = BuildCultureTroopTree(culture);
                
                var availableTroops = tree.Where(troop => 
                    SafeGetTier(troop) == tier &&
                    !troop.IsHero &&
                    troop.BattleEquipments.Any()).ToList();
                
                // Add culture fallback troops if no specific tier troops found
                if (availableTroops.Count == 0 && culture != null)
                {
                    ModLogger.Info("TroopSelection", $"No troops found at tier {tier}, using culture fallbacks");
                    
                    // Use guaranteed culture troop templates as fallbacks
                    var fallbackTroops = new List<CharacterObject>();
                    if (culture.BasicTroop != null)
                    {
                        fallbackTroops.Add(culture.BasicTroop);
                    }
                    if (culture.EliteBasicTroop != null)
                    {
                        fallbackTroops.Add(culture.EliteBasicTroop);
                    }
                    if (culture.MeleeMilitiaTroop != null)
                    {
                        fallbackTroops.Add(culture.MeleeMilitiaTroop);
                    }
                    if (culture.RangedMilitiaTroop != null)
                    {
                        fallbackTroops.Add(culture.RangedMilitiaTroop);
                    }
                    
                    availableTroops = fallbackTroops.Where(t => t.BattleEquipments.Any()).ToList();
                    ModLogger.Info("TroopSelection", $"Using {availableTroops.Count} culture fallback troops");
                }
                
                ModLogger.Info("TroopSelection", $"Found {availableTroops.Count} tree troops for {cultureId} tier {tier}");
                return availableTroops;
            }
            catch (Exception ex)
            {
                ModLogger.Error("TroopSelection", "Failed to get troops for culture/tier", ex);
                return new List<CharacterObject>();
            }
        }

        /// <summary>
        /// Build the culture's troop tree by traversing upgrade paths from BasicTroop and EliteBasicTroop.
        /// </summary>
        private List<CharacterObject> BuildCultureTroopTree(CultureObject culture)
        {
            var results = new List<CharacterObject>();
            try
            {
                if (culture == null)
                {
                    return results;
                }

                var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var queue = new Queue<CharacterObject>();

                void EnqueueIfValid(CharacterObject start)
                {
                    if (start == null)
                    {
                        return;
                    }
                    if (start.Culture != culture)
                    {
                        return;
                    }
                    if (start.IsHero)
                    {
                        return;
                    }
                    if (!visited.Add(start.StringId))
                    {
                        return;
                    }
                    queue.Enqueue(start);
                }

                EnqueueIfValid(culture.BasicTroop);
                EnqueueIfValid(culture.EliteBasicTroop);

                while (queue.Count > 0)
                {
                    var node = queue.Dequeue();
                    results.Add(node);

                    try
                    {
                        var upgrades = node.UpgradeTargets; // MBReadOnlyList<CharacterObject>
                        if (upgrades != null)
                        {
                            foreach (var next in upgrades)
                            {
                                if (next != null && next.Culture == culture && !next.IsHero && visited.Add(next.StringId))
                                {
                                    queue.Enqueue(next);
                                }
                            }
                        }
                    }
                    catch
                    {
                        // best-effort; continue on any API differences
                    }
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("TroopSelection", $"BuildCultureTroopTree failed: {ex.Message}");
            }
            return results;
        }
        
        /// <summary>
        /// Apply equipment from selected troop to hero.
        /// Implements equipment REPLACEMENT system (not accumulation).
        /// Includes accountability check - soldier is charged for missing equipment when auto-issuing.
        /// </summary>
        public void ApplySelectedTroopEquipment(Hero hero, CharacterObject selectedTroop, bool autoIssueEquipment)
        {
            try
            {
                _lastSelectedTroopId = selectedTroop.StringId;

                // Update formation based on selected troop (always)
                var formation = DetectTroopFormation(selectedTroop);
                var duties = EnlistedDutiesBehavior.Instance;
                duties?.SetPlayerFormation(formation.ToString().ToLower());
                
                if (autoIssueEquipment)
                {
                    // Equipment is replaced (not accumulated) for realistic military service
                    var troopEquipment = selectedTroop.BattleEquipments.FirstOrDefault();
                    if (troopEquipment == null)
                    {
                        ModLogger.Error("TroopSelection", $"No equipment found for {selectedTroop.Name}");
                        return;
                    }

                    // ACCOUNTABILITY CHECK: Charge for any missing equipment before issuing new gear
                    ProcessEquipmentAccountability(hero);

                    // Replace all equipment with new troop's gear
                    EquipmentHelper.AssignHeroEquipmentFromEquipment(hero, troopEquipment);

                    // Record newly issued equipment for future accountability
                    RecordIssuedEquipment(hero.BattleEquipment);

                    // Show promotion notification
                    var message = new TextObject("{=eq_promoted_new_equipment}Promoted to {TROOP_NAME}! New equipment issued.");
                    message.SetTextVariable("TROOP_NAME", selectedTroop.Name);
                    InformationManager.DisplayMessage(new InformationMessage(message.ToString()));
                }
                else
                {
                    // No auto-issue: inform player gear is purchasable at Quartermaster
                    var message = new TextObject("{=eq_purchasable_qm}Promotion recorded. Gear for {TROOP_NAME} is now available at the Quartermaster.");
                    message.SetTextVariable("TROOP_NAME", selectedTroop.Name);
                    InformationManager.DisplayMessage(new InformationMessage(message.ToString()));
                }
                
                // Clear promotion state
                _promotionPending = false;
                _availableTroops.Clear();
                
                ModLogger.Info("TroopSelection", autoIssueEquipment
                    ? $"Equipment replaced with {selectedTroop.Name} gear (Formation: {formation})"
                    : $"Troop selection recorded without auto-issue (Formation: {formation}); gear available at Quartermaster");
            }
            catch (Exception ex)
            {
                ModLogger.Error("TroopSelection", $"Failed to apply selected troop equipment for {selectedTroop?.Name?.ToString() ?? "null"}: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// Process equipment accountability - check for missing gear and charge the soldier.
        /// Called before issuing new equipment when changing troop type.
        /// </summary>
        private void ProcessEquipmentAccountability(Hero hero)
        {
            try
            {
                // Check for missing equipment
                var (missingItems, totalDebt) = CheckMissingEquipment();
                
                if (missingItems.Count == 0)
                {
                    // All equipment accounted for - clear tracking for fresh start
                    ClearIssuedEquipment();
                    return;
                }
                
                // Deduct the cost of missing equipment from soldier's pay
                if (totalDebt > 0)
                {
                    hero.Gold = Math.Max(0, hero.Gold - totalDebt);
                    
                    // Build notification message listing missing items
                    var sb = new System.Text.StringBuilder();
                    var headerText = new TextObject("{=qm_missing_equipment_header}Missing equipment deducted from pay:");
                    sb.AppendLine(headerText.ToString());
                    foreach (var item in missingItems)
                    {
                        sb.AppendLine($"  • {item.ItemName} ({item.ItemValue} denars)");
                    }
                    var totalText = new TextObject("{=qm_missing_equipment_total}Total deducted: {AMOUNT} denars");
                    totalText.SetTextVariable("AMOUNT", totalDebt);
                    sb.AppendLine(totalText.ToString());
                    
                    // Show notification to player
                    var chargeMsg = new TextObject("{=qm_missing_equipment_charge}Missing equipment charge: {AMOUNT} denars deducted from pay.");
                    chargeMsg.SetTextVariable("AMOUNT", totalDebt);
                    InformationManager.DisplayMessage(new InformationMessage(chargeMsg.ToString(), Colors.Red));
                    
                    // Show detailed popup if significant amount
                    if (totalDebt >= 100)
                    {
                        var titleText = new TextObject("{=qm_missing_equipment_title}Equipment Accountability");
                        var btnText = new TextObject("{=qm_btn_understood}Understood");
                        InformationManager.ShowInquiry(new InquiryData(
                            titleText.ToString(),
                            sb.ToString(),
                            true,
                            false,
                            btnText.ToString(),
                            string.Empty,
                            null,
                            null));
                    }
                    
                    ModLogger.Info("TroopSelection", $"Charged {totalDebt} denars for {missingItems.Count} missing items");
                }
                
                // Clear tracking after processing
                ClearIssuedEquipment();
            }
            catch (Exception ex)
            {
                ModLogger.Error("TroopSelection", "Error processing equipment accountability", ex);
            }
        }
        
        /// <summary>
        /// Detect formation type from troop properties.
        /// Detects the player's military formation based on equipment.
        /// </summary>
        private FormationType DetectTroopFormation(CharacterObject troop)
        {
            try
            {
                // Detect formation based on equipment characteristics
                if (troop.IsRanged && troop.IsMounted)
                {
                    return FormationType.HorseArcher;   // Bow + Horse
                }
                else if (troop.IsMounted)
                {
                    return FormationType.Cavalry;       // Sword + Horse  
                }
                else if (troop.IsRanged)
                {
                    return FormationType.Archer;        // Bow + No Horse
                }
                else
                {
                    return FormationType.Infantry;      // Sword + No Horse (default)
                }
            }
            catch
            {
                return FormationType.Infantry; // Safe fallback
            }
        }
        
        /// <summary>
        /// Handle troop selection by formation type.
        /// Selects the first available troop matching the specified formation type.
        /// </summary>
        public void SelectTroopByFormation(FormationType formationType)
        {
            try
            {
                var troopOfType = _availableTroops.FirstOrDefault(t => DetectTroopFormation(t) == formationType);
                if (troopOfType != null)
                {
                    ApplySelectedTroopEquipment(Hero.MainHero, troopOfType, autoIssueEquipment: SafeGetTier(troopOfType) <= 1);
                    
                    // Return to main enlisted menu
                    EnlistedMenuBehavior.SafeActivateEnlistedMenu();
                }
                else
                {
                    ModLogger.Error("TroopSelection", $"No {formationType} troop available");
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("TroopSelection", "Error in troop selection", ex);
            }
        }
        
        /// <summary>
        /// Check if promotion is currently pending.
        /// </summary>
        public bool IsPromotionPending => _promotionPending;
        
        /// <summary>
        /// Get pending promotion tier.
        /// </summary>
        public int PendingTier => _pendingTier;
        
        #region Equipment Accountability
        
        /// <summary>
        /// Record equipment as issued to the soldier for accountability tracking.
        /// Called when equipment is given via promotion, enlistment, or quartermaster.
        /// </summary>
        public void RecordIssuedEquipment(TaleWorlds.Core.Equipment equipment)
        {
            try
            {
                if (equipment == null)
                {
                    return;
                }
                
                _issuedEquipment.Clear();
                
                // Record all equipped items
                for (var slot = EquipmentIndex.Weapon0; slot <= EquipmentIndex.HorseHarness; slot++)
                {
                    var item = equipment[slot].Item;
                    if (item != null)
                    {
                        _issuedEquipment[(int)slot] = new IssuedItemRecord(item, slot);
                    }
                }
                
                ModLogger.Info("TroopSelection", $"Recorded {_issuedEquipment.Count} issued equipment items for accountability");
            }
            catch (Exception ex)
            {
                ModLogger.Error("TroopSelection", "Error recording issued equipment", ex);
            }
        }
        
        /// <summary>
        /// Record a single item as issued (for quartermaster acquisitions).
        /// </summary>
        public void RecordIssuedItem(ItemObject item, EquipmentIndex slot)
        {
            try
            {
                if (item == null)
                {
                    return;
                }
                
                _issuedEquipment[(int)slot] = new IssuedItemRecord(item, slot);
                ModLogger.Info("TroopSelection", $"Recorded issued item: {item.Name} in slot {slot}");
            }
            catch (Exception ex)
            {
                ModLogger.Error("TroopSelection", "Error recording issued item", ex);
            }
        }

        /// <summary>
        /// Determine if an item (by stringId) is currently tracked as issued.
        /// Used to filter quartermaster returns so only issued items are returnable.
        /// </summary>
        public bool IsIssuedItem(string itemStringId)
        {
            try
            {
                if (string.IsNullOrEmpty(itemStringId) || _issuedEquipment.Count == 0)
                {
                    return false;
                }

                return _issuedEquipment.Values.Any(v =>
                    v != null && v.ItemStringId == itemStringId);
            }
            catch (Exception ex)
            {
                ModLogger.Error("TroopSelection", $"Error checking issued item: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Mark one issued item as returned, removing it from accountability tracking.
        /// Matches by ItemStringId; removes a single record.
        /// </summary>
        public bool MarkIssuedItemReturned(string itemStringId)
        {
            try
            {
                if (string.IsNullOrEmpty(itemStringId) || _issuedEquipment.Count == 0)
                {
                    return false;
                }

                var kvp = _issuedEquipment.FirstOrDefault(x =>
                    x.Value != null && x.Value.ItemStringId == itemStringId);

                if (kvp.Value == null)
                {
                    return false;
                }

                _issuedEquipment.Remove(kvp.Key);
                ModLogger.Info("TroopSelection", $"Marked issued item returned: {kvp.Value.ItemName} (slot {kvp.Key})");
                return true;
            }
            catch (Exception ex)
            {
                ModLogger.Error("TroopSelection", $"Error marking issued item returned: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Check for missing equipment and calculate the debt owed.
        /// Returns a list of missing items and the total value to charge.
        /// </summary>
        public (List<IssuedItemRecord> MissingItems, int TotalDebt) CheckMissingEquipment()
        {
            var missingItems = new List<IssuedItemRecord>();
            var totalDebt = 0;
            
            try
            {
                var hero = Hero.MainHero;
                if (hero == null || _issuedEquipment.Count == 0)
                {
                    return (missingItems, totalDebt);
                }
                
                foreach (var issued in _issuedEquipment.Values)
                {
                    if (string.IsNullOrEmpty(issued.ItemStringId))
                    {
                        continue;
                    }
                    
                    // Check if the player still has this item equipped anywhere
                    var hasItem = false;
                    for (var slot = EquipmentIndex.Weapon0; slot <= EquipmentIndex.HorseHarness; slot++)
                    {
                        var equippedItem = hero.BattleEquipment[slot].Item;
                        if (equippedItem?.StringId == issued.ItemStringId)
                        {
                            hasItem = true;
                            break;
                        }
                    }
                    
                    // Also check civilian equipment
                    if (!hasItem)
                    {
                        for (var slot = EquipmentIndex.Weapon0; slot <= EquipmentIndex.HorseHarness; slot++)
                        {
                            var equippedItem = hero.CivilianEquipment[slot].Item;
                            if (equippedItem?.StringId == issued.ItemStringId)
                            {
                                hasItem = true;
                                break;
                            }
                        }
                    }
                    
                    if (!hasItem)
                    {
                        missingItems.Add(issued);
                        totalDebt += issued.ItemValue;
                    }
                }
                
                if (missingItems.Count > 0)
                {
                    ModLogger.Info("TroopSelection", $"Found {missingItems.Count} missing items worth {totalDebt} denars");
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("TroopSelection", "Error checking missing equipment", ex);
            }
            
            return (missingItems, totalDebt);
        }
        
        /// <summary>
        /// Clear the issued equipment tracking (called after accountability check).
        /// </summary>
        public void ClearIssuedEquipment()
        {
            _issuedEquipment.Clear();
            ModLogger.Info("TroopSelection", "Cleared issued equipment tracking");
        }

        /// <summary>
        ///     Manually serialize issued equipment so the save system only touches primitives.
        ///     Avoids missing type definitions for IssuedItemRecord dictionaries during save.
        /// </summary>
        private void SerializeIssuedEquipment(IDataStore dataStore)
        {
            try
            {
                var count = _issuedEquipment?.Count ?? 0;
                dataStore.SyncData("_issued_count", ref count);

                if (!dataStore.IsLoading)
                {
                    var issuedEquipment = _issuedEquipment ?? new Dictionary<int, IssuedItemRecord>();
                    var index = 0;
                    const int unsetSlotIndex = -1;
                    foreach (var kvp in issuedEquipment)
                    {
                        var slotKey = kvp.Key;
                        var record = kvp.Value ?? new IssuedItemRecord();
                        var slotIndex = record.SlotIndex >= 0 ? record.SlotIndex : unsetSlotIndex;
                        var itemId = record.ItemStringId ?? string.Empty;
                        var itemName = record.ItemName ?? string.Empty;
                        var itemValue = record.ItemValue;

                        dataStore.SyncData($"_issued_{index}_slotKey", ref slotKey);
                        dataStore.SyncData($"_issued_{index}_slotIndex", ref slotIndex);
                        dataStore.SyncData($"_issued_{index}_itemId", ref itemId);
                        dataStore.SyncData($"_issued_{index}_itemName", ref itemName);
                        dataStore.SyncData($"_issued_{index}_itemValue", ref itemValue);
                        index++;
                    }
                }
                else
                {
                    _issuedEquipment = new Dictionary<int, IssuedItemRecord>();
                    const int unsetSlotIndex = -1;
                    for (var i = 0; i < count; i++)
                    {
                        var slotKey = 0;
                        var slotIndex = 0;
                        var itemId = string.Empty;
                        var itemName = string.Empty;
                        var itemValue = 0;

                        dataStore.SyncData($"_issued_{i}_slotKey", ref slotKey);
                        dataStore.SyncData($"_issued_{i}_slotIndex", ref slotIndex);
                        dataStore.SyncData($"_issued_{i}_itemId", ref itemId);
                        dataStore.SyncData($"_issued_{i}_itemName", ref itemName);
                        dataStore.SyncData($"_issued_{i}_itemValue", ref itemValue);

                        var resolvedSlotIndex = slotIndex == unsetSlotIndex ? slotKey : slotIndex;
                        _issuedEquipment[slotKey] = new IssuedItemRecord
                        {
                            ItemStringId = itemId,
                            ItemName = itemName,
                            ItemValue = itemValue,
                            SlotIndex = resolvedSlotIndex
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("TroopSelection", $"Error serializing issued equipment: {ex.Message}");
            }
        }
        
        #endregion
    }
    
    /// <summary>
    /// Formation types for troop selection and duties system integration.
    /// </summary>
    public enum FormationType
    {
        None,
        Infantry,
        Archer, 
        Cavalry,
        HorseArcher
    }
    
    /// <summary>
    /// Record of an item issued to the soldier for accountability tracking.
    /// When the soldier changes troop type, missing items will be charged to their pay.
    /// </summary>
    [Serializable]
    public class IssuedItemRecord
    {
        /// <summary>
        /// The StringId of the issued item (used to look up the item).
        /// </summary>
        public string ItemStringId { get; set; }
        
        /// <summary>
        /// The display name of the item (for notifications).
        /// </summary>
        public string ItemName { get; set; }
        
        /// <summary>
        /// The value of the item in denars (charged if missing).
        /// </summary>
        public int ItemValue { get; set; }
        
        /// <summary>
        /// The equipment slot this item was issued to.
        /// </summary>
        public int SlotIndex { get; set; }
        
        public IssuedItemRecord()
        {
            // Parameterless constructor for serialization
        }
        
        public IssuedItemRecord(ItemObject item, EquipmentIndex slot)
        {
            if (item == null)
            {
                return;
            }
            
            ItemStringId = item.StringId;
            ItemName = item.Name?.ToString() ?? "Unknown Item";
            ItemValue = item.Value;
            SlotIndex = (int)slot;
        }
    }
}
