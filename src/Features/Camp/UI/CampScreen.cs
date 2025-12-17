using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;

namespace Enlisted.Features.Camp.UI
{
    /// <summary>
    /// The main Camp screen using a tab-based interface.
    /// Follows native Clan/Kingdom screen patterns with Q/E tab switching.
    /// </summary>
    public class CampScreen : ScreenBase
    {
        private GauntletLayer _gauntletLayer;
        private GauntletMovieIdentifier _movie;
        private CampScreenVM _dataSource;
        private readonly Action _onClosed;

        public CampScreen(Action onClosed = null)
        {
            _onClosed = onClosed;
        }

        protected override void OnInitialize()
        {
            base.OnInitialize();

            _dataSource = new CampScreenVM(CloseScreen);

            _gauntletLayer = new GauntletLayer("GauntletLayer", 100);
            _movie = _gauntletLayer.LoadMovie("CampScreen", _dataSource);

            _gauntletLayer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.All);
            _gauntletLayer.Input.RegisterHotKeyCategory(
                HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
            _gauntletLayer.Input.RegisterHotKeyCategory(
                HotKeyManager.GetCategory("GenericCampaignPanelsGameKeyCategory"));

            AddLayer(_gauntletLayer);
            _gauntletLayer.IsFocusLayer = true;
            ScreenManager.TrySetFocus(_gauntletLayer);

            if (Campaign.Current != null)
            {
                Campaign.Current.TimeControlMode = CampaignTimeControlMode.Stop;
            }
        }

        protected override void OnFrameTick(float dt)
        {
            base.OnFrameTick(dt);

            if (_gauntletLayer == null || _dataSource == null)
            {
                return;
            }

            // Tab switching (Q/E keys)
            if (_dataSource.CanSwitchTabs)
            {
                if (_gauntletLayer.Input.IsHotKeyReleased("SwitchToPreviousTab"))
                {
                    _dataSource.SelectPreviousCategory();
                }
                else if (_gauntletLayer.Input.IsHotKeyReleased("SwitchToNextTab"))
                {
                    _dataSource.SelectNextCategory();
                }
            }

            // ESC or Enter to close
            if (_gauntletLayer.Input.IsHotKeyReleased("Exit") ||
                _gauntletLayer.Input.IsHotKeyReleased("Confirm"))
            {
                CloseScreen();
            }
        }

        protected override void OnFinalize()
        {
            base.OnFinalize();

            if (Campaign.Current != null)
            {
                Campaign.Current.TimeControlMode = CampaignTimeControlMode.UnstoppablePlay;
            }

            if (_movie != null && _gauntletLayer != null)
            {
                _gauntletLayer.ReleaseMovie(_movie);
            }
            
            RemoveLayer(_gauntletLayer);

            _dataSource?.OnFinalize();
            _gauntletLayer = null;
            _dataSource = null;
            _movie = null;
        }

        private void CloseScreen()
        {
            ScreenManager.PopScreen();
            _onClosed?.Invoke();
        }

        /// <summary>
        /// Opens the Camp screen.
        /// </summary>
        public static void Open(Action onClosed = null)
        {
            // Use next frame to avoid issues with menu transitions
            Campaign.Current?.SetTimeSpeed(0);

            var screen = new CampScreen(onClosed);
            ScreenManager.PushScreen(screen);
        }
    }
}
