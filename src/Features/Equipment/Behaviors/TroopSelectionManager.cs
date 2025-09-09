using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Overlay;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using Helpers;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Features.Assignments.Behaviors;
using Enlisted.Mod.Core.Logging;
using Enlisted.Features.Equipment.UI;

namespace Enlisted.Features.Equipment.Behaviors
{
    /// <summary>
    /// SAS-style troop selection system using real Bannerlord troop templates.
    /// 
    /// This system allows players to choose from actual game troops during promotion,
    /// replacing the equipment kit approach with authentic military progression.
    /// Equipment is REPLACED (not accumulated) for realistic military service.
    /// </summary>
    public sealed class TroopSelectionManager : CampaignBehaviorBase
    {
        public static TroopSelectionManager Instance { get; private set; }
        
        // Promotion state tracking
        private bool _promotionPending = false;
        private int _pendingTier = 1;
        private List<CharacterObject> _availableTroops = new List<CharacterObject>();
        private string _lastSelectedTroopId;
        
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
                if (!enlistment?.IsEnlisted == true)
                {
                    InformationManager.DisplayMessage(new InformationMessage(
                        new TextObject("You must be enlisted to use Master at Arms.").ToString()));
                    return;
                }

                var cultureId = enlistment.CurrentLord?.Culture?.StringId;
                if (string.IsNullOrEmpty(cultureId))
                {
                    ModLogger.Error("Equipment", "Master at Arms: Missing culture on current lord");
                    return;
                }

                var unlocked = GetUnlockedTroopsForCurrentTier(cultureId, enlistment.EnlistmentTier);
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
                    var portrait = new ImageIdentifier(CharacterCode.CreateFrom(troop));
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
                                ApplySelectedTroopEquipment(Hero.MainHero, chosen);
                                _lastSelectedTroopId = chosen.StringId;
                                if (Campaign.Current?.CurrentMenuContext != null)
                                {
                                    GameMenu.ActivateGameMenu("enlisted_status");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            ModLogger.Error("Equipment", $"Master at Arms apply failed: {ex.Message}");
                        }
                    },
                    _ =>
                    {
                        try
                        {
                            if (Campaign.Current?.CurrentMenuContext != null)
                            {
                                GameMenu.ActivateGameMenu("enlisted_status");
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
        private List<CharacterObject> GetUnlockedTroopsForCurrentTier(string cultureId, int currentTier)
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
            starter.AddGameMenu("enlisted_troop_selection",
                "Military Advancement\n{TROOP_SELECTION_TEXT}",
                OnTroopSelectionInit,
                GameOverlays.MenuOverlayType.None,
                GameMenu.MenuFlags.None,
                null);
                
            // Dynamic troop options will be added based on available troops
            
            // Back to enlisted status option
            starter.AddGameMenuOption("enlisted_troop_selection", "troop_selection_back",
                "Return to enlisted status",
                args => true,
                args => GameMenu.ActivateGameMenu("enlisted_status"),
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
                if (!enlistment?.IsEnlisted == true)
                {
                    ModLogger.Error("TroopSelection", "Cannot show troop selection - player not enlisted");
                    return;
                }

                var cultureId = enlistment.CurrentLord.Culture.StringId;
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
                if (!_promotionPending || _availableTroops.Count == 0)
                {
                    // Initialize menu context
                    MBTextManager.SetTextVariable("TROOP_SELECTION_TEXT", "No promotion available at this time.");
                    return;
                }

                // Build troop selection display
                var statusText = $"Promotion to Tier {_pendingTier} Available!\n\n";
                statusText += $"Select your military specialization from {_availableTroops.Count} available troops:\n\n";
                
                // Add troop information display
                foreach (var troop in _availableTroops.Take(6)) // Limit display for readability
                {
                    var formation = DetectTroopFormation(troop);
                    statusText += $"• {troop.Name} ({formation.ToString()})\n";
                }
                
                if (_availableTroops.Count > 6)
                {
                    statusText += $"... and {_availableTroops.Count - 6} more options\n";
                }
                
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
                var currentMenu = args.MenuContext.GameMenu;
                
                // Add option for each available troop (limit to first 8 for UI reasons)
                for (int i = 0; i < Math.Min(_availableTroops.Count, 8); i++)
                {
                    var troop = _availableTroops[i];
                    var formation = DetectTroopFormation(troop);
                    var optionText = $"Select {troop.Name} ({formation.ToString()})";
                    
                    // Note: Dynamic menu option creation requires different approach
                    // For now, we'll use a simplified selection through text input
                    // This can be enhanced later with custom Gauntlet UI
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
                // CRITICAL: Equipment REPLACEMENT system (not accumulation)
                // Player turns in old equipment, receives new equipment
                var troopEquipment = selectedTroop.BattleEquipments.FirstOrDefault();
                if (troopEquipment == null)
                {
                    ModLogger.Error("TroopSelection", $"No equipment found for {selectedTroop.Name}");
                    return;
                }
                
                // Replace all equipment with new troop's gear
                EquipmentHelper.AssignHeroEquipmentFromEquipment(hero, troopEquipment);
                
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
                
                ModLogger.Info("TroopSelection", $"Equipment replaced with {selectedTroop.Name} gear");
            }
            catch (Exception ex)
            {
                ModLogger.Error("TroopSelection", "Failed to apply selected troop equipment", ex);
            }
        }
        
        /// <summary>
        /// Detect formation type from troop properties.
        /// Uses SAS-style formation detection logic.
        /// </summary>
        private FormationType DetectTroopFormation(CharacterObject troop)
        {
            try
            {
                // SAS formation detection logic
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
        /// Handle troop selection from menu option (simplified approach).
        /// For now, select first troop of each formation type.
        /// Can be enhanced later with custom UI.
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
                    GameMenu.ActivateGameMenu("enlisted_status");
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
