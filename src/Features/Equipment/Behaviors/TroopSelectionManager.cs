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
        private bool _promotionPending = false;
        private int _pendingTier = 1;
        private List<CharacterObject> _availableTroops = new List<CharacterObject>();
        private string _lastSelectedTroopId;

        public string LastSelectedTroopId => _lastSelectedTroopId;
        
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
        }
        
        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            AddTroopSelectionMenus(starter);
            
            // DEVELOPMENT: Validate troop coverage across all factions
            TroopDiscoveryValidator.ValidateAllCulturesAndTiers();
            
            ModLogger.Info("TroopSelection", "Troop selection system initialized");
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
                        new TextObject("You must be enlisted to use Master at Arms.").ToString()));
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
                        new TextObject("No eligible troops found for your rank and culture.").ToString()));
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
                    string.Empty,
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
                            var chosen = selected?.FirstOrDefault()?.Identifier as CharacterObject;
                            if (chosen != null)
                            {
                                                ModLogger.Info("TroopSelection", $"Player selected troop: {chosen.Name} (ID: {chosen.StringId}, Tier: {SafeGetTier(chosen)}, Formation: {DetectTroopFormation(chosen)})");
                                ApplySelectedTroopEquipment(Hero.MainHero, chosen);
                                _lastSelectedTroopId = chosen.StringId;
                                if (Campaign.Current?.CurrentMenuContext != null)
                                {
                                    EnlistedMenuBehavior.SafeActivateEnlistedMenu();
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
                        try
                        {
                            if (Campaign.Current?.CurrentMenuContext != null)
                            {
                                EnlistedMenuBehavior.SafeActivateEnlistedMenu();
                            }
                        }
                        catch (Exception ex)
                        {
                            ModLogger.Error("TroopSelection", $"Master at Arms cancel failed: {ex.Message}");
                        }
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
                    .OrderBy(t => SafeGetTier(t))
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
                    for (EquipmentIndex slot = EquipmentIndex.Weapon0; slot <= EquipmentIndex.HorseHarness; slot++)
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
                OnTroopSelectionInit,
                GameMenu.MenuOverlayType.None,
                GameMenu.MenuFlags.None,
                null);
                
            // "Collect equipment now" button - opens Master at Arms popup for troop selection
            starter.AddGameMenuOption("enlisted_troop_selection", "troop_selection_collect_now",
                "Collect equipment now",
                _ => _promotionPending && _availableTroops.Count > 0,
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
            
            // "Return to camp" button - player declines immediate equipment collection
            starter.AddGameMenuOption("enlisted_troop_selection", "troop_selection_back",
                "Return to camp",
                _ => true,
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
                },
                true, -1);
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
                string backgroundMesh = "encounter_looter"; // Safe fallback
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
                statusText += "Do you want to complete the paperwork and collect your new equipment now, or later?\n\n";
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
        private void CreateTroopSelectionOptions(MenuCallbackArgs args)
        {
            try
            {
                // Clear any existing troop options
                // Note: Menu context available via args.MenuContext.GameMenu if needed
                
                // Add option for each available troop (limit to first 8 for UI reasons)
                for (int i = 0; i < Math.Min(_availableTroops.Count, 8); i++)
                {
                    var troop = _availableTroops[i];
                    // Note: Formation detection and option text would be set dynamically if needed
                    
                    // Troop selection is handled through the popup dialog system
                    // Players choose from available troops displayed in a selection dialog
                }
                
                ModLogger.Info("TroopSelection", $"Created {Math.Min(_availableTroops.Count, 8)} troop selection options");
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
                                if (next != null && next.Culture == culture && !next.IsHero && !visited.Contains(next.StringId))
                                {
                                    visited.Add(next.StringId);
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
        /// </summary>
        public void ApplySelectedTroopEquipment(Hero hero, CharacterObject selectedTroop)
        {
            try
            {
                // Equipment is replaced (not accumulated) for realistic military service
                // This ensures players get the equipment appropriate to their tier and troop type
                // Player turns in old equipment, receives new equipment
                var troopEquipment = selectedTroop.BattleEquipments.FirstOrDefault();
                if (troopEquipment == null)
                {
                    ModLogger.Error("TroopSelection", $"No equipment found for {selectedTroop.Name}");
                    return;
                }
                
                // Replace all equipment with new troop's gear
                EquipmentHelper.AssignHeroEquipmentFromEquipment(hero, troopEquipment);
                _lastSelectedTroopId = selectedTroop.StringId;
                
                // Update formation based on selected troop
                var formation = DetectTroopFormation(selectedTroop);
                var duties = EnlistedDutiesBehavior.Instance;
                duties?.SetPlayerFormation(formation.ToString().ToLower());
                
                // Show promotion notification
                var message = new TextObject("Promoted to {TROOP_NAME}! New equipment issued.");
                message.SetTextVariable("TROOP_NAME", selectedTroop.Name);
                InformationManager.DisplayMessage(new InformationMessage(message.ToString()));
                
                // Clear promotion state
                _promotionPending = false;
                _availableTroops.Clear();
                
                ModLogger.Info("TroopSelection", $"Equipment replaced with {selectedTroop.Name} gear (Formation: {formation})");
            }
            catch (Exception ex)
            {
                ModLogger.Error("TroopSelection", $"Failed to apply selected troop equipment for {selectedTroop?.Name?.ToString() ?? "null"}: {ex.Message}", ex);
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
                    ApplySelectedTroopEquipment(Hero.MainHero, troopOfType);
                    
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
}
