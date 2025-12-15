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

namespace Enlisted.Features.Camp.UI.Areas
{
    /// <summary>
    /// Custom Gauntlet screen for viewing activities at a specific camp location.
    /// Phase 2: Refactored from CampActivitiesScreen to support location-based filtering.
    /// </summary>
    public class CampAreaScreen : ScreenBase
    {
        private readonly string _locationId;
        private readonly Action _onClosed;
        
        private GauntletLayer _gauntletLayer;
        private GauntletMovieIdentifier _gauntletMovie;
        private CampAreaVM _dataSource;
        
        private bool _closing;
        
        public CampAreaScreen(string locationId, Action onClosed = null)
        {
            _locationId = locationId;
            _onClosed = onClosed;
        }
        
        protected override void OnInitialize()
        {
            base.OnInitialize();
            
            try
            {
                // Create the ViewModel with location filter
                _dataSource = new CampAreaVM(_locationId, CloseScreen);
                
                // Create Gauntlet layer and load UI
                // Note: Still uses CampActivitiesScreen.xml for layout (same structure)
                _gauntletLayer = new GauntletLayer("GauntletLayer", 200);
                _gauntletMovie = _gauntletLayer.LoadMovie("CampActivitiesScreen", _dataSource);
                
                if (_gauntletMovie == null)
                {
                    throw new Exception("Failed to load Camp Area UI - XML file may be missing or invalid");
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
                
                ModLogger.Info("CampAreaUI", $"Camp Area screen displayed for location: {_locationId}");
            }
            catch (Exception ex)
            {
                ModLogger.Error("CampAreaUI", $"Failed to display Camp Area screen: {ex.Message}", ex);
                
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
            
            _dataSource?.OnFinalize();
            _dataSource = null;
            
            // Close the screen
            ScreenManager.PopScreen();
            
            // Notify caller
            _onClosed?.Invoke();
            
            ModLogger.Debug("CampAreaUI", "Camp Area screen closed");
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
        /// Static helper to open the Camp Area screen for a specific location.
        /// Defers screen opening to next frame to avoid crashes during native visual updates.
        /// </summary>
        public static void Open(string locationId, Action onClosed = null)
        {
            // Defer screen opening to next frame to prevent crashes during native visual updates
            NextFrameDispatcher.RunNextFrame(() =>
            {
                try
                {
                    // Validate campaign still exists when deferred action executes
                    if (Campaign.Current == null)
                    {
                        ModLogger.Warn("CampAreaUI", "Cannot display screen - campaign session ended");
                        onClosed?.Invoke();
                        return;
                    }
                    
                    var screen = new CampAreaScreen(locationId, onClosed);
                    ScreenManager.PushScreen(screen);
                }
                catch (Exception ex)
                {
                    ModLogger.Error("CampAreaUI", $"Failed to display Camp Area screen: {ex.Message}", ex);
                    // Ensure onClosed is called so the caller can clean up
                    onClosed?.Invoke();
                }
            });
        }
    }
}

