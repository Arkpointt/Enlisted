using System;
using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using Enlisted.Features.Assignments.Behaviors;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Mod.Core.Logging;

namespace Enlisted.Mod.GameAdapters.Patches
{
    /// <summary>
    /// Automatic formation-based battle command filtering for enlisted soldiers.
    /// 
    /// This patch provides immersive command filtering where enlisted soldiers only hear
    /// battle commands relevant to their assigned formation type (Infantry, Archer, Cavalry, Horse Archer).
    /// No menu toggles required - it works automatically based on troop selection.
    /// </summary>
    
    // Harmony Patch
    // Target: TaleWorlds.MountAndBlade.BehaviorComponent.InformSergeantPlayer()
    // Why: Filter battle commands to only show orders relevant to player's assigned formation type
    // Safety: Campaign-only; checks enlisted state; validates formation assignment; only affects enlisted soldiers
    // Notes: Postfix patch; works with the existing formation detection system; includes audio cues for immersion
    [HarmonyPatch(typeof(BehaviorComponent), "InformSergeantPlayer")]
    [HarmonyPriority(999)] // Run before other Harmony layers
    public class BattleCommandsFilterPatch
    {
        static void Postfix(BehaviorComponent __instance)
        {
            try
            {
                // Guard: Only process if player is enlisted
                if (EnlistmentBehavior.Instance?.IsEnlisted != true)
                {
                    return; // Not enlisted - let original behavior handle it
                }
                
                // Log initialization once per session
                ModLogger.LogOnce("battle_commands_init", "Patch", "Battle commands filter active - showing formation-relevant orders only");
                
                // Guard: Must have valid formation assignment
                var playerFormation = EnlistedDutiesBehavior.Instance?.PlayerFormation;
                if (string.IsNullOrEmpty(playerFormation))
                {
                    return; // No formation assigned yet
                }
                
                // Guard: Must have valid behavior component and formation
                if (__instance?.Formation?.PhysicalClass == null)
                {
                    return; // Invalid formation data
                }
                
                // Guard: Only show commands for player team or allies
                bool isPlayerTeamOrAlly = __instance.Formation.Team.IsPlayerTeam || __instance.Formation.Team.IsPlayerAlly;
                if (!isPlayerTeamOrAlly)
                {
                    return; // Not our team's commands
                }
                
                // Get command's target formation type
                var commandFormationType = GetFormationTypeFromClass(__instance.Formation.PhysicalClass);
                
                // Only show commands if they match player's assigned formation
                if (commandFormationType.ToLower() == playerFormation.ToLower())
                {
                    var behaviorString = __instance.GetBehaviorString();
                    if (behaviorString != null && ShouldShowCommand(__instance))
                    {
                        // Get enlisted lord for command attribution
                        var enlistedLord = EnlistmentBehavior.Instance.CurrentLord;
                        
                        // Format command message with formation context
                        var formationName = GetFormationDisplayName(playerFormation);
                        var commandText = $"{formationName} {behaviorString.ToString().ToLower()}";
                        
                        // Display command with lord's portrait
                        // 1.3.4 API: Added Equipment parameter (4th param)
                        MBInformationManager.AddQuickInformation(
                            new TextObject(commandText, null), 
                            4000, 
                            enlistedLord?.CharacterObject, 
                            null,  // Equipment parameter (1.3.4 API)
                            ""
                        );
                        
                        // Play appropriate horn sound for immersion
                        PlayCommandSound(__instance);
                    }
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error("BattleCommands", $"Battle commands filter error: {ex.Message}");
                // Fail safe - let original behavior continue
            }
        }
        
        /// <summary>
        /// Convert FormationClass to formation type string matching our system.
        /// </summary>
        private static string GetFormationTypeFromClass(FormationClass formationClass)
        {
            return formationClass switch
            {
                FormationClass.Infantry => "infantry",
                FormationClass.Ranged => "archer", 
                FormationClass.Cavalry => "cavalry",
                FormationClass.HorseArcher => "horsearcher",
                FormationClass.Skirmisher => "archer", // Treat as archer variant
                FormationClass.HeavyInfantry => "infantry", // Treat as infantry variant
                FormationClass.LightCavalry => "cavalry", // Treat as cavalry variant  
                FormationClass.HeavyCavalry => "cavalry", // Treat as cavalry variant
                _ => "infantry" // Safe fallback
            };
        }
        
        /// <summary>
        /// Get display-friendly formation name for command messages.
        /// </summary>
        private static string GetFormationDisplayName(string formationType)
        {
            return formationType.ToLower() switch
            {
                "infantry" => "Infantry",
                "archer" => "Archers", 
                "cavalry" => "Cavalry",
                "horsearcher" => "Horse archers",
                _ => "Infantry" // Safe fallback
            };
        }
        
        /// <summary>
        /// Determine if this command should be shown to enlisted soldiers.
        /// Filters out generic behaviors that aren't tactical commands.
        /// </summary>
        private static bool ShouldShowCommand(BehaviorComponent behavior)
        {
            if (behavior == null) return false;
            
            // Don't show generic or protection behaviors - these aren't tactical commands
            var behaviorType = behavior.GetType();
            return behaviorType != typeof(BehaviorGeneral) && 
                   behaviorType != typeof(BehaviorProtectGeneral);
        }
        
        /// <summary>
        /// Play appropriate horn sound based on command type for immersion.
        /// Attack commands get attack horn, movement commands get move horn.
        /// </summary>
        private static void PlayCommandSound(BehaviorComponent behavior)
        {
            try
            {
                var behaviorType = behavior.GetType();
                
                // Attack commands - more aggressive horn
                if (behaviorType == typeof(BehaviorHorseArcherSkirmish) ||
                    behaviorType == typeof(BehaviorAssaultWalls) ||
                    behaviorType == typeof(BehaviorCharge) ||
                    behaviorType == typeof(BehaviorSkirmish) ||
                    behaviorType == typeof(BehaviorTacticalCharge))
                {
                    SoundEvent.PlaySound2D(SoundEvent.GetEventIdFromString("event:/ui/mission/horns/attack"));
                }
                else
                {
                    // Movement/defensive commands - standard horn
                    SoundEvent.PlaySound2D(SoundEvent.GetEventIdFromString("event:/ui/mission/horns/move"));
                }
            }
            catch
            {
                // Audio is optional - don't break functionality if sound fails
            }
        }
    }
}
