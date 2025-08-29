using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.CampaignSystem;
using HarmonyLib;
using Enlisted.Features.Enlistment.Application;
using Enlisted.Mod.Core.Logging;

namespace Enlisted.Entry
{
    public class SubModule : MBSubModuleBase
    {
        private Harmony _harmony;

        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            
            // Initialize logging service first
            LoggingService.Initialize();
            LoggingService.Info("SubModule", "Enlisted mod loading...");
            
            // Initialize Harmony for potential future patches
            _harmony = new Harmony("com.enlisted.mod");
            _harmony.PatchAll();
            
            LoggingService.Info("SubModule", "Harmony initialized successfully");
        }

        protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
        {
            base.OnGameStart(game, gameStarterObject);
            
            LoggingService.Info("SubModule", "Game started, registering behaviors...");
            
            if (gameStarterObject is CampaignGameStarter campaignStarter)
            {
                // Register the EnlistmentBehavior
                campaignStarter.AddBehavior(new EnlistmentBehavior());
                LoggingService.Info("SubModule", "EnlistmentBehavior registered successfully");
            }
            else
            {
                LoggingService.Warning("SubModule", "GameStarter is not CampaignGameStarter, behaviors not registered");
            }
        }

        protected override void OnSubModuleUnloaded()
        {
            base.OnSubModuleUnloaded();
        }
    }
}
