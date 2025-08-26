using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.CampaignSystem;
using Enlisted.Core.Config;
using Enlisted.Core.Logging;
using Enlisted.Core.DependencyInjection;
using Enlisted.Features.Enlistment.Application;

namespace Enlisted.Entry
{
    /// <summary>
    /// Main mod entry point for the Enlisted modification.
    /// Handles Harmony patching, service initialization, and behavior registration.
    /// Thin entry layer that routes to feature modules as per blueprint.
    /// 
    /// Implements ADR-004 dependency injection initialization.
    /// </summary>
    public class SubModule : MBSubModuleBase
    {
        private Harmony _harmony;
        private IServiceContainer _serviceContainer;
        private ILoggingService _logger;

        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            
            // Initialize dependency injection container
            InitializeServices();
            
            // Apply Harmony patches
            _harmony = new Harmony("Enlisted");
            _harmony.PatchAll();
            
            _logger?.LogInfo(LogCategories.Initialization, "Enlisted mod loaded successfully with dependency injection");
        }

        protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
        {
            base.OnGameStart(game, gameStarterObject);

            _logger?.LogInfo(LogCategories.Initialization, "Game started, registering campaign behaviors");

            // Register campaign behaviors
            if (gameStarterObject is CampaignGameStarter campaignStarter)
            {
                // Create behaviors with dependency injection
                var enlistmentBehavior = new EnlistmentBehavior();
                var wageBehavior = new Features.Wages.Application.WageBehavior();
                var promotionBehavior = new Features.Promotion.Application.PromotionBehavior();

                // Register behaviors with campaign system
                campaignStarter.AddBehavior(enlistmentBehavior);
                campaignStarter.AddBehavior(wageBehavior);
                campaignStarter.AddBehavior(promotionBehavior);

                // Register enlistment service for dependency injection
                _serviceContainer.RegisterSingleton<IEnlistmentService, EnlistmentBehavior>(enlistmentBehavior);

                _logger?.LogInfo(LogCategories.Initialization, "Campaign behaviors registered successfully");
            }
        }

        protected override void OnSubModuleUnloaded()
        {
            base.OnSubModuleUnloaded();
            
            _logger?.LogInfo(LogCategories.Initialization, "Enlisted mod unloading");
            
            _harmony?.UnpatchAll("Enlisted");
        }

        /// <summary>
        /// Initialize dependency injection container and core services.
        /// Implements ADR-004 service registration pattern.
        /// </summary>
        private void InitializeServices()
        {
            try
            {
                // Create service container
                _serviceContainer = new ServiceContainer();

                // Load configuration
                var settings = ModSettings.Instance;

                // Register core services
                _logger = new LoggingService(settings);
                _serviceContainer.RegisterSingleton<ILoggingService, LoggingService>(_logger);
                _serviceContainer.RegisterSingleton<ModSettings, ModSettings>(settings);

                // Initialize global service locator for transition period
                ServiceLocator.Initialize(_serviceContainer);

                _logger.LogInfo(LogCategories.Initialization, "Dependency injection container initialized");
                _logger.LogInfo(LogCategories.Configuration, "Settings loaded - Debug Logging: {0}, Daily Wage: {1}", 
                    settings.EnableDebugLogging, settings.DailyWage);
            }
            catch (System.Exception ex)
            {
                // Fallback logging if service initialization fails
                Debug.Print($"[Enlisted] CRITICAL: Failed to initialize services: {ex.Message}");
            }
        }
    }
}
