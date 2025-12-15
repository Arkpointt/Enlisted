using System;
using Enlisted.Mod.Core.Logging;
using Enlisted.Mod.Entry;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Engine.Screens;
using TaleWorlds.GauntletUI.Data;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;

namespace Enlisted.Features.Camp.UI.Hub
{
    /// <summary>
    /// Custom Gauntlet screen for the Camp Hub.
    /// Displays 6 location buttons for navigating to different camp areas.
    /// Phase 2: Camp Hub & Location System.
    /// </summary>
    public class CampHubScreen : ScreenBase
    {
        private readonly Action _onClosed;
        
        private GauntletLayer _gauntletLayer;
        private GauntletMovieIdentifier _gauntletMovie;
        private CampHubVM _dataSource;
        
        private bool _closing;
        
        public CampHubScreen(Action onClosed = null)
        {
            _onClosed = onClosed;
        }
        
        protected override void OnInitialize()
        {
            base.OnInitialize();
            
            try
            {
                // Create the ViewModel
                _dataSource = new CampHubVM(CloseScreen);
                
                // Create Gauntlet layer and load UI
                _gauntletLayer = new GauntletLayer("GauntletLayer", 200);
                _gauntletMovie = _gauntletLayer.LoadMovie("CampHubScreen", _dataSource);
                
                if (_gauntletMovie == null)
                {
                    throw new Exception("Failed to load Camp Hub UI - XML file may be missing or invalid");
                }
                
                // Add layer to screen
                AddLayer(_gauntletLayer);
                
                // Set input restrictions (pause game, capture input)
                _gauntletLayer.InputRestrictions.SetInputRestrictions(
                    isMouseVisible: true,
                    InputUsageMask.All);
                
                // Pause game time when screen is open
                if (Campaign.Current != null)
                {
                    Campaign.Current.TimeControlMode = CampaignTimeControlMode.Stop;
                }
                
                ModLogger.Info("CampHubUI", "Camp Hub screen displayed successfully");
            }
            catch (Exception ex)
            {
                ModLogger.Error("CampHubUI", $"Failed to display Camp Hub screen: {ex.Message}", ex);
                
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
                NextFrameDispatcher.RunNextFrame(() =>
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
            
            // Handle ESC key to close
            if (_gauntletLayer?.Input != null && 
                _gauntletLayer.Input.IsKeyReleased(InputKey.Escape) && 
                !_closing)
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
            
            _dataSource = null;
            
            // Close the screen
            ScreenManager.PopScreen();
            
            // Notify caller
            _onClosed?.Invoke();
            
            ModLogger.Debug("CampHubUI", "Camp Hub screen closed");
        }
        
        protected override void OnFinalize()
        {
            base.OnFinalize();
            
            // Cleanup if not already done
            if (_dataSource != null)
            {
                _dataSource = null;
            }
            
            if (_gauntletMovie != null)
            {
                _gauntletLayer?.ReleaseMovie(_gauntletMovie);
                _gauntletMovie = null;
            }
        }
        
        /// <summary>
        /// Static helper to open the Camp Hub screen from anywhere.
        /// Defers screen opening to next frame to avoid crashes during native visual updates.
        /// </summary>
        public static void Open(Action onClosed = null)
        {
            // Defer screen opening to next frame to prevent crashes during native visual updates
            NextFrameDispatcher.RunNextFrame(() =>
            {
                try
                {
                    // Validate campaign still exists when deferred action executes
                    if (Campaign.Current == null)
                    {
                        ModLogger.Warn("CampHubUI", "Cannot display screen - campaign session ended");
                        onClosed?.Invoke();
                        return;
                    }
                    
                    var screen = new CampHubScreen(onClosed);
                    ScreenManager.PushScreen(screen);
                }
                catch (Exception ex)
                {
                    ModLogger.Error("CampHubUI", $"Failed to display Camp Hub screen: {ex.Message}", ex);
                    // Ensure onClosed is called so the caller can clean up
                    onClosed?.Invoke();
                }
            });
        }
    }
}

