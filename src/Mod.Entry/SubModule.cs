using System;
using HarmonyLib;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Core;
using TaleWorlds.CampaignSystem;
using Enlisted.Mod.Core.Logging;
using Enlisted.Mod.Core.Config;
using Enlisted.Features.Conversations.Behaviors;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Features.Assignments.Behaviors;
using Enlisted.Features.Interface.Behaviors;
using Enlisted.Features.Equipment.Behaviors;
using Enlisted.Features.Ranks.Behaviors;
using Enlisted.Features.Combat.Behaviors;

namespace Enlisted.Mod.Entry
{
	public class SubModule : MBSubModuleBase
	{
		private Harmony _harmony;
		private ModSettings _settings;

		protected override void OnSubModuleLoad()
		{
			try
			{
				ModLogger.Initialize();
				ModLogger.Info("Bootstrap", "SubModule loading");

				_harmony = new Harmony("com.enlisted.mod");
				_harmony.PatchAll();
				ModLogger.Info("Bootstrap", "Harmony patched");
			}
			catch (Exception ex)
			{
				ModLogger.Error("Bootstrap", "Exception during OnSubModuleLoad", ex);
				// Fail closed: do not crash the game on load; continue without patches.
			}
		}

		protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
		{
			try
			{
				ModLogger.Info("Bootstrap", "Game start");
				_settings = ModSettings.LoadFromModule();
				ModConfig.Settings = _settings;

				if (gameStarterObject is CampaignGameStarter campaignStarter)
				{
					// Core military service behaviors
					campaignStarter.AddBehavior(new EnlistmentBehavior());
					campaignStarter.AddBehavior(new EnlistedDialogManager());
					campaignStarter.AddBehavior(new EnlistedDutiesBehavior());
					
					// Enhanced menu and input system
					campaignStarter.AddBehavior(new EnlistedMenuBehavior());
					campaignStarter.AddBehavior(new EnlistedInputHandler());
					
					// Phase 2B: Troop selection and equipment replacement system
					campaignStarter.AddBehavior(new TroopSelectionManager());
					campaignStarter.AddBehavior(new EquipmentManager());
					campaignStarter.AddBehavior(new PromotionBehavior());
					
					// Quartermaster system for equipment variant management
					campaignStarter.AddBehavior(new QuartermasterManager());
					campaignStarter.AddBehavior(new Features.Equipment.UI.QuartermasterEquipmentSelectorBehavior());
					
					// Battle integration system
					campaignStarter.AddBehavior(new EnlistedEncounterBehavior());
					
					EncounterGuard.Initialize();
					ModLogger.Info("Bootstrap", "Military service behaviors registered (with Phase 2B troop selection system)");
				}
			}
			catch (Exception ex)
			{
				ModLogger.Error("Bootstrap", "Exception during OnGameStart", ex);
				// Fail closed: avoid crashing startup; behaviors may be partially unavailable.
			}
		}
		
	}
}


