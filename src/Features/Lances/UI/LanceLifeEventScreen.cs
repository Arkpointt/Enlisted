using System;
using System.Threading;
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
        // Crash guard for Naval DLC worker-thread map visuals:
        // NavalDLC_View updates party visuals on background threads. Pushing a screen while those threads are in-flight
        // can trip native code (seen as crashes in NavalMobilePartyVisual.UpdateEntityPosition).
        // We expose a simple, thread-safe flag that Naval crash-guard patches can read without touching ScreenManager.
        // 0 = inactive, 1 = opening (scheduled), 2 = open (initialized).
        private static int _navalVisualCrashGuardState;

        /// <summary>
        /// True while this screen is opening or open. Safe to read from background threads.
        /// </summary>
        public static bool IsNavalVisualCrashGuardActive => Volatile.Read(ref _navalVisualCrashGuardState) != 0;

        private static void SetNavalVisualCrashGuardState(int state)
        {
            Interlocked.Exchange(ref _navalVisualCrashGuardState, state);
        }

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

            try
            {
                // Mark as open as early as possible so background-thread crash guards can react immediately.
                SetNavalVisualCrashGuardState(2);

                // Create the ViewModel
                _dataSource = new LanceLifeEventVM(_event, _enlistment, CloseScreen);

                // Create Gauntlet layer and load UI
                _gauntletLayer = new GauntletLayer("GauntletLayer", 200);
                _gauntletMovie = _gauntletLayer.LoadMovie("LanceLifeEventScreen", _dataSource);

                if (_gauntletMovie == null)
                {
                    throw new Exception("Failed to load event UI - XML file may be missing or invalid");
                }

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

                Enlisted.Mod.Core.Logging.ModLogger.Info("LanceLifeUI", $"Lance event '{_event?.TitleFallback}' displayed");
            }
            catch (Exception ex)
            {
                Enlisted.Mod.Core.Logging.ModLogger.Error("LanceLifeUI", $"Failed to display lance event: {ex.Message}", ex);

                // Ensure crash guard doesn't remain stuck on if initialization fails.
                SetNavalVisualCrashGuardState(0);
                
                // Clean up partial initialization
                if (_gauntletMovie != null && _gauntletLayer != null)
                {
                    _gauntletLayer.ReleaseMovie(_gauntletMovie);
                }
                _gauntletMovie = null;
                _gauntletLayer = null;
                _dataSource = null;

                // Close immediately and notify
                _onClosed?.Invoke();
                
                // Pop this broken screen off the stack
                Enlisted.Mod.Entry.NextFrameDispatcher.RunNextFrame(() =>
                {
                    try
                    {
                        ScreenManager.PopScreen();
                    }
                    catch { }
                });

                throw; // Re-throw to let the screen manager know initialization failed
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
            SetNavalVisualCrashGuardState(0);

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

            // Safety: if the screen is finalized unexpectedly, clear crash guard.
            SetNavalVisualCrashGuardState(0);

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
        /// Defers screen opening to next frame to avoid crashes during native visual updates.
        /// </summary>
        public static void Open(LanceLifeEventDefinition eventDef, EnlistmentBehavior enlistment, Action onClosed = null)
        {
            if (eventDef == null)
            {
                Enlisted.Mod.Core.Logging.ModLogger.Warn("LanceLifeUI", "Cannot display event - event definition is null");
                return;
            }

            // Activate crash guard immediately so background-thread Naval visuals can skip while we transition.
            SetNavalVisualCrashGuardState(1);

            // Defer screen opening to next frame to prevent crashes during native visual updates
            // (e.g., NavalMobilePartyVisual.UpdateEntityPosition crashes if we push screen mid-tick)
            Enlisted.Mod.Entry.NextFrameDispatcher.RunNextFrame(() =>
            {
                try
                {
                    // Validate campaign still exists when deferred action executes
                    if (TaleWorlds.CampaignSystem.Campaign.Current == null)
                    {
                        Enlisted.Mod.Core.Logging.ModLogger.Warn("LanceLifeUI", "Cannot display event - campaign session ended");
                        SetNavalVisualCrashGuardState(0);
                        onClosed?.Invoke();
                        return;
                    }

                    var screen = new LanceLifeEventScreen(eventDef, enlistment, onClosed);
                    ScreenManager.PushScreen(screen);
                }
                catch (Exception ex)
                {
                    Enlisted.Mod.Core.Logging.ModLogger.Error("LanceLifeUI", $"Failed to display event screen: {ex.Message}", ex);
                    SetNavalVisualCrashGuardState(0);
                    // Ensure onClosed is called so the presenter can clean up
                    onClosed?.Invoke();
                }
            });
        }
    }
}
