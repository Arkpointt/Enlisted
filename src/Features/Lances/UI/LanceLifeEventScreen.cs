using System;
using Enlisted.Features.Assignments.Core;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Features.Lances.Events;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Engine.Screens;
using TaleWorlds.GauntletUI.Data;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;

namespace Enlisted.Features.Lances.UI
{
    /// <summary>
    /// Custom Gauntlet screen for modern Lance Life event presentation.
    /// Replaces basic inquiry popups with rich, visual event screens.
    /// </summary>
    public class LanceLifeEventScreen : ScreenBase
    {
        private readonly LanceLifeEventDefinition _event;
        private readonly EnlistmentBehavior _enlistment;
        private readonly Action _onClosed;

        private GauntletLayer _gauntletLayer;
        private GauntletMovieIdentifier _gauntletMovie;
        private LanceLifeEventVM _dataSource;

        private bool _closing;

        public LanceLifeEventScreen(LanceLifeEventDefinition eventDef, EnlistmentBehavior enlistment, Action onClosed = null)
        {
            _event = eventDef;
            _enlistment = enlistment;
            _onClosed = onClosed;
        }

        protected override void OnInitialize()
        {
            base.OnInitialize();

            // Create the ViewModel
            _dataSource = new LanceLifeEventVM(_event, _enlistment, CloseScreen);

            // Create Gauntlet layer
            _gauntletLayer = new GauntletLayer("GauntletLayer", 200);

            // Load the UI prefab
            _gauntletMovie = _gauntletLayer.LoadMovie("LanceLifeEventScreen", _dataSource);

            // Add layer to screen
            AddLayer(_gauntletLayer);

            // Set input restrictions (pause game, capture input)
            _gauntletLayer.InputRestrictions.SetInputRestrictions(
                isMouseVisible: true,
                InputUsageMask.All);

            // Pause game time when screen is open
            if (TaleWorlds.CampaignSystem.Campaign.Current != null)
            {
                TaleWorlds.CampaignSystem.Campaign.Current.TimeControlMode = 
                    TaleWorlds.CampaignSystem.CampaignTimeControlMode.Stop;
            }
        }

        protected override void OnFrameTick(float dt)
        {
            base.OnFrameTick(dt);

            // Handle ESC key to close (if allowed)
            if (_gauntletLayer.Input.IsKeyReleased(InputKey.Escape) && _dataSource.CanClose && !_closing)
            {
                CloseScreen();
            }
        }

        private void CloseScreen()
        {
            if (_closing)
                return;

            _closing = true;

            // Clean up
            if (_gauntletMovie != null)
            {
                _gauntletLayer?.ReleaseMovie(_gauntletMovie);
                _gauntletMovie = null;
            }

            if (_gauntletLayer != null)
            {
                RemoveLayer(_gauntletLayer);
                _gauntletLayer = null;
            }

            _dataSource?.OnFinalize();
            _dataSource = null;

            // Close the screen
            ScreenManager.PopScreen();

            // Notify caller
            _onClosed?.Invoke();
        }

        protected override void OnFinalize()
        {
            base.OnFinalize();

            // Cleanup if not already done
            if (_dataSource != null)
            {
                _dataSource.OnFinalize();
                _dataSource = null;
            }

            if (_gauntletMovie != null)
            {
                _gauntletLayer?.ReleaseMovie(_gauntletMovie);
                _gauntletMovie = null;
            }
        }

        /// <summary>
        /// Static helper to open the event screen from anywhere.
        /// Replaces ShowInquiry calls for a modern experience.
        /// </summary>
        public static void Open(LanceLifeEventDefinition eventDef, EnlistmentBehavior enlistment, Action onClosed = null)
        {
            if (eventDef == null)
            {
                Enlisted.Mod.Core.Logging.ModLogger.Warn("LanceLifeUI", "Cannot open event screen - event is null");
                return;
            }

            var screen = new LanceLifeEventScreen(eventDef, enlistment, onClosed);
            ScreenManager.PushScreen(screen);
        }
    }
}
