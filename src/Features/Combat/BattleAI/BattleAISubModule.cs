#if BATTLE_AI
using TaleWorlds.MountAndBlade;
using Enlisted.Mod.Core.Logging;

namespace Enlisted.Features.Combat.BattleAI
{
    /// <summary>
    /// Optional SubModule for Battle AI systems.
    /// Users can disable this SubModule in the Bannerlord launcher to disable all Battle AI features.
    /// When disabled, there is no performance cost as this SubModule never initializes.
    /// </summary>
    public class BattleAISubModule : MBSubModuleBase
    {
        private const string LogCategory = "BattleAI";

        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            ModLogger.Info(LogCategory, "Battle AI SubModule loaded - Advanced combat AI enabled");
        }

        protected override void OnBeforeInitialModuleScreenSetAsRoot()
        {
            base.OnBeforeInitialModuleScreenSetAsRoot();
            
            // Register Battle AI mission behaviors here when implemented
            // Example:
            // MissionManager.OnMissionBehaviourCreated += RegisterBattleAIBehaviors;
            
            ModLogger.Info(LogCategory, "Battle AI systems initialized");
        }

        protected override void OnSubModuleUnloaded()
        {
            base.OnSubModuleUnloaded();
            ModLogger.Info(LogCategory, "Battle AI SubModule unloaded");
        }

        /// <summary>
        /// Registers Battle AI mission behaviors for field battles.
        /// Called automatically when missions are created.
        /// </summary>
        private void RegisterBattleAIBehaviors(Mission mission)
        {
            // Only add Battle AI to field battles (not siege/naval)
            if (mission.CombatType != Mission.MissionCombatType.Combat)
            {
                return;
            }

            // Check if this is a field battle
            if (mission.Scene.GetName().Contains("siege") || mission.Scene.GetName().Contains("naval"))
            {
                return;
            }

            // TODO: Add Battle AI behaviors here when implemented
            // Example:
            // mission.AddMissionBehavior(new EnlistedBattleAIBehavior());
            
            ModLogger.Debug(LogCategory, $"Battle AI behaviors registered for mission: {mission.SceneName}");
        }
    }
}
#endif
