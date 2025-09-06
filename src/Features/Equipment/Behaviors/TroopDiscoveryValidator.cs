using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.ObjectSystem;
using Enlisted.Mod.Core.Logging;

namespace Enlisted.Features.Equipment.Behaviors
{
    /// <summary>
    /// Validates troop discovery across all cultures to ensure complete faction coverage.
    /// 
    /// This utility class tests our troop selection system against all 6 Bannerlord cultures
    /// to identify potential gaps in troop availability and ensure robust faction support.
    /// </summary>
    public static class TroopDiscoveryValidator
    {
        /// <summary>
        /// Validate troop availability across all cultures and tiers.
        /// Call this during development to ensure complete coverage.
        /// </summary>
        public static void ValidateAllCulturesAndTiers()
        {
            try
            {
                ModLogger.Info("TroopDiscovery", "=== FACTION TROOP COVERAGE VALIDATION ===");
                
                var cultures = new[] { "empire", "aserai", "khuzait", "vlandia", "sturgia", "battania" };
                var allTroops = MBObjectManager.Instance.GetObjectTypeList<CharacterObject>();
                
                foreach (var cultureId in cultures)
                {
                    ValidateCultureCoverage(cultureId, allTroops);
                }
                
                ModLogger.Info("TroopDiscovery", "=== VALIDATION COMPLETE ===");
            }
            catch (Exception ex)
            {
                ModLogger.Error("TroopDiscovery", "Error during faction validation", ex);
            }
        }
        
        /// <summary>
        /// Validate troop coverage for a specific culture.
        /// </summary>
        private static void ValidateCultureCoverage(string cultureId, IEnumerable<CharacterObject> allTroops)
        {
            try
            {
                var culture = MBObjectManager.Instance.GetObject<CultureObject>(cultureId);
                if (culture == null)
                {
                    ModLogger.Error("TroopDiscovery", $"❌ CULTURE MISSING: {cultureId} not found");
                    return;
                }
                
                ModLogger.Info("TroopDiscovery", $"=== {cultureId.ToUpper()} CULTURE VALIDATION ===");
                
                // Check each tier (1-7)
                for (int tier = 1; tier <= 7; tier++)
                {
                    var troopsAtTier = allTroops.Where(troop => 
                        troop.Culture == culture && 
                        troop.Tier == tier &&
                        troop.IsSoldier &&
                        troop.BattleEquipments.Any()).ToList();
                    
                    if (troopsAtTier.Count > 0)
                    {
                        ModLogger.Info("TroopDiscovery", $"✅ T{tier}: {troopsAtTier.Count} troops found");
                        
                        // Show sample troop names
                        var sampleTroops = troopsAtTier.Take(3).Select(t => t.Name?.ToString() ?? "Unnamed").ToArray();
                        ModLogger.Info("TroopDiscovery", $"   Samples: {string.Join(", ", sampleTroops)}");
                        
                        // Check formation coverage
                        ValidateFormationCoverage(troopsAtTier, tier);
                    }
                    else
                    {
                        ModLogger.Error("TroopDiscovery", $"❌ T{tier}: NO TROOPS FOUND - troop selection will fail!");
                    }
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("TroopDiscovery", $"Error validating culture {cultureId}", ex);
            }
        }
        
        /// <summary>
        /// Check formation coverage for troops at a specific tier.
        /// </summary>
        private static void ValidateFormationCoverage(List<CharacterObject> troopsAtTier, int tier)
        {
            try
            {
                var formationCounts = new Dictionary<FormationType, int>
                {
                    { FormationType.Infantry, 0 },
                    { FormationType.Archer, 0 },
                    { FormationType.Cavalry, 0 },
                    { FormationType.HorseArcher, 0 }
                };
                
                foreach (var troop in troopsAtTier)
                {
                    var formation = DetectTroopFormation(troop);
                    formationCounts[formation]++;
                }
                
                var formationsWithTroops = formationCounts.Count(kvp => kvp.Value > 0);
                var totalFormations = formationCounts.Count;
                
                if (formationsWithTroops == totalFormations)
                {
                    ModLogger.Info("TroopDiscovery", $"   ✅ All 4 formations covered");
                }
                else
                {
                    ModLogger.Error("TroopDiscovery", $"   ⚠️ Only {formationsWithTroops}/{totalFormations} formations available");
                    
                    // Report missing formations
                    foreach (var kvp in formationCounts.Where(x => x.Value == 0))
                    {
                        ModLogger.Error("TroopDiscovery", $"   ❌ NO {kvp.Key} troops at T{tier}");
                    }
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("TroopDiscovery", $"Error validating formations at tier {tier}", ex);
            }
        }
        
        /// <summary>
        /// Detect formation type from troop properties (same logic as TroopSelectionManager).
        /// </summary>
        private static FormationType DetectTroopFormation(CharacterObject troop)
        {
            try
            {
                if (troop.IsRanged && troop.IsMounted)
                    return FormationType.HorseArcher;
                else if (troop.IsMounted)
                    return FormationType.Cavalry;
                else if (troop.IsRanged)
                    return FormationType.Archer;
                else
                    return FormationType.Infantry;
            }
            catch
            {
                return FormationType.Infantry;
            }
        }
        
        /// <summary>
        /// Test specific culture and tier to verify troop availability.
        /// Call this during development to debug specific issues.
        /// </summary>
        public static void TestSpecificCultureTier(string cultureId, int tier)
        {
            try
            {
                ModLogger.Info("TroopDiscovery", $"=== TESTING {cultureId.ToUpper()} TIER {tier} ===");
                
                var culture = MBObjectManager.Instance.GetObject<CultureObject>(cultureId);
                var allTroops = MBObjectManager.Instance.GetObjectTypeList<CharacterObject>();
                
                var availableTroops = allTroops.Where(troop => 
                    troop.Culture == culture && 
                    troop.Tier == tier &&
                    troop.IsSoldier &&
                    troop.BattleEquipments.Any()).ToList();
                
                if (availableTroops.Count > 0)
                {
                    ModLogger.Info("TroopDiscovery", $"✅ SUCCESS: {availableTroops.Count} troops found");
                    
                    foreach (var troop in availableTroops.Take(5))
                    {
                        var formation = DetectTroopFormation(troop);
                        var equipmentCount = troop.BattleEquipments.Count();
                        ModLogger.Info("TroopDiscovery", $"   • {troop.Name} ({formation}) - {equipmentCount} equipment sets");
                    }
                }
                else
                {
                    ModLogger.Error("TroopDiscovery", $"❌ FAILURE: NO TROOPS FOUND for {cultureId} T{tier}");
                    
                    // Debug info
                    var totalCultureTroops = allTroops.Count(t => t.Culture == culture);
                    var totalTierTroops = allTroops.Count(t => t.Tier == tier);
                    var totalSoldiers = allTroops.Count(t => t.IsSoldier);
                    
                    ModLogger.Info("TroopDiscovery", $"   Debug: {totalCultureTroops} culture troops, {totalTierTroops} tier troops, {totalSoldiers} soldiers total");
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("TroopDiscovery", $"Error testing {cultureId} T{tier}", ex);
            }
        }
    }
}
