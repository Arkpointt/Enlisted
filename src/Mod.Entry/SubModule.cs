using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.CampaignSystem;
using HarmonyLib;
using Enlisted.Features.Enlistment.Application;
using Enlisted.Features.EnlistedMenu.Application;
using Enlisted.Mod.Core.Logging;

namespace Enlisted.Entry
{
    public class SubModule : MBSubModuleBase
    {
        private Harmony _harmony;
        private const bool EnableHarmonyPatches = false; // temporarily disabled for debugging visuals

        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            
            // Initialize logging service first
            LoggingService.Initialize();
            LoggingService.Info("SubModule", "Enlisted mod loading...");
            
            // Initialize Harmony for potential future patches
            _harmony = new Harmony("com.enlisted.mod");
            if (EnableHarmonyPatches)
            {
                _harmony.PatchAll();
                LoggingService.Info("SubModule", "Harmony initialized successfully (patches enabled)");
            }
            else
            {
                LoggingService.Info("SubModule", "Harmony patches are disabled by flag for debugging");
            }
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
                // Register the EnlistedMenuBehavior (menus/status panel)
                campaignStarter.AddBehavior(new EnlistedMenuBehavior());
                LoggingService.Info("SubModule", "EnlistedMenuBehavior registered successfully");
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
