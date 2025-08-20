using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.CampaignSystem;

namespace Enlisted
{
    /// <summary>
    /// Main mod entry point for the Enlisted modification.
    /// Handles Harmony patching and behavior registration.
    /// </summary>
    public class SubModule : MBSubModuleBase
    {
        private Harmony _harmony;

        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            _harmony = new Harmony("Enlisted");
            _harmony.PatchAll();
        }

        protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
        {
            base.OnGameStart(game, gameStarterObject);

            // Register campaign behaviors
            if (gameStarterObject is CampaignGameStarter campaignStarter)
            {
                campaignStarter.AddBehavior(new Enlisted.Behaviors.EnlistmentBehavior());
                campaignStarter.AddBehavior(new Enlisted.Behaviors.WageBehavior());
                // Register promotion behavior for Phase 1 progression
                campaignStarter.AddBehavior(new Enlisted.Behaviors.PromotionBehavior());
            }
        }

        protected override void OnSubModuleUnloaded()
        {
            base.OnSubModuleUnloaded();
            _harmony?.UnpatchAll("Enlisted");
        }
    }
}
