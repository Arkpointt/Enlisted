using System;
using Enlisted.Mod.Core.Logging;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;
using TaleWorlds.TwoDimension;

namespace Enlisted.Features.Camp.UI.Bulletin
{
    /// <summary>
    /// Layer-based UI for the Camp Bulletin Board.
    /// Uses the same pattern as QuartermasterEquipmentSelectorBehavior and native TownManagement:
    /// adds a GauntletLayer to the TopScreen instead of pushing a new screen.
    /// </summary>
    public static class CampBulletinScreen
    {
        private const string LogCategory = "CampBulletinUI";
        private static bool _isOpen = false;
        private static GauntletLayer _gauntletLayer;
        private static GauntletMovieIdentifier _movie;
        private static CampBulletinVM _dataSource;
        private static Action _onClosed;
        private static bool _tickLoggedThisOpen;
        private static SpriteCategory _townManagementSpriteCategory;
        private static SpriteCategory _encyclopediaSpriteCategory;

        public static void Open(Action onClosed = null)
        {
            // Guard against multiple simultaneous opens
            if (_isOpen)
            {
                return;
            }

            ModLogger.Info(LogCategory, "Open called");
            
            _isOpen = true;
            _onClosed = onClosed;
            _tickLoggedThisOpen = false;
            
            // Defer to next frame to avoid crashes during native visual updates
            Enlisted.Mod.Entry.NextFrameDispatcher.RunNextFrame(() =>
            {
                if (Campaign.Current == null || ScreenManager.TopScreen == null)
                {
                    ModLogger.Warn(LogCategory, "Open aborted: Campaign.Current or ScreenManager.TopScreen was null.");
                    _isOpen = false;
                    onClosed?.Invoke();
                    return;
                }

                try
                {
                    // Close any existing layer first
                    if (_gauntletLayer != null)
                    {
                        Close();
                    }

                    // Create Gauntlet layer with high priority (like Quartermaster uses 1001)
                    _gauntletLayer = new GauntletLayer("CampBulletinLayer", 1001);

                    // Load the same sprite categories used by the native Town Management screen.
                    // Without this, many of the TownManagement/Encyclopedia sprites + brushes can appear blank.
                    _townManagementSpriteCategory = UIResourceManager.LoadSpriteCategory("ui_town_management");
                    if (_townManagementSpriteCategory != null && !_townManagementSpriteCategory.IsLoaded)
                    {
                        _townManagementSpriteCategory.Load();
                    }

                    _encyclopediaSpriteCategory = UIResourceManager.LoadSpriteCategory("ui_encyclopedia");
                    if (_encyclopediaSpriteCategory != null && !_encyclopediaSpriteCategory.IsLoaded)
                    {
                        _encyclopediaSpriteCategory.Load();
                    }

                    // Create ViewModel
                    _dataSource = new CampBulletinVM();
                    _dataSource.RefreshValues();

                    // Track A Phase 3: Register bulletin for news integration
                    CampBulletinIntegration.RegisterActiveBulletin(_dataSource);

                    // Load the Camp Bulletin UI
                    _movie = _gauntletLayer.LoadMovie("CampBulletin", _dataSource);
                    if (_movie == null)
                    {
                        ModLogger.Error(LogCategory, "LoadMovie returned null for 'CampBulletin'. This usually means the prefab failed to load/bind.");
                        InformationManager.DisplayMessage(new InformationMessage("Enlisted: Camp UI failed to load. Check Modules/Enlisted/Debugging for the log."));
                        Close();
                        return;
                    }
                    
                    // Register hotkeys and set input restrictions
                    var genericPanel = HotKeyManager.GetCategory("GenericPanelGameKeyCategory");
                    if (!_gauntletLayer.Input.IsCategoryRegistered(genericPanel))
                    {
                        _gauntletLayer.Input.RegisterHotKeyCategory(genericPanel);
                    }
                    _gauntletLayer.InputRestrictions.SetInputRestrictions();

                    // Provide the Done button input key (native TownManagement uses "Confirm").
                    _dataSource.SetDoneInputKey(genericPanel.GetHotKey("Confirm"));
                    
                    // KEY DIFFERENCE: Add layer to TopScreen instead of pushing a new screen
                    ScreenManager.TopScreen.AddLayer(_gauntletLayer);
                    
                    _gauntletLayer.IsFocusLayer = true;
                    ScreenManager.TrySetFocus(_gauntletLayer);
                    
                    // Show the UI
                    _dataSource.Show = true;
                    
                    // Pause the game
                    if (Campaign.Current != null)
                    {
                        Campaign.Current.TimeControlMode = CampaignTimeControlMode.Stop;
                    }

                    ModLogger.Info(LogCategory, "Camp Bulletin layer opened successfully.");
                }
                catch (Exception ex)
                {
                    // Bannerlord menu callbacks often fail "quietly" when exceptions occur.
                    // Log + show an in-game hint so we can diagnose quickly without guessing.
                    ModLogger.Error(LogCategory, "Exception while opening Camp Bulletin UI.", ex);
                    InformationManager.DisplayMessage(new InformationMessage("Enlisted: Camp UI failed to open. Check Modules/Enlisted/Debugging for details."));
                    Close();
                }
            });
        }

        /// <summary>
        /// Called each frame to handle input (ESC to close).
        /// This needs to be called from somewhere that ticks - we'll use a behavior.
        /// </summary>
        public static void Tick()
        {
            if (!_isOpen || _gauntletLayer == null)
            {
                return;
            }

            if (!_tickLoggedThisOpen)
            {
                _tickLoggedThisOpen = true;
            }

            // TownManagement pattern: close when VM requests close (Show=false)
            if (_dataSource != null && !_dataSource.Show)
            {
                Close();
                return;
            }

            // Handle ESC key to close
            if (_gauntletLayer.Input.IsKeyReleased(InputKey.Escape))
            {
                Close();
            }
        }

        public static void Close()
        {
            if (!_isOpen && _gauntletLayer == null)
            {
                return;
            }

            ModLogger.Debug(LogCategory, "Closing Camp Bulletin layer");

            try
            {
                // Release movie
                if (_movie != null && _gauntletLayer != null)
                {
                    _gauntletLayer.ReleaseMovie(_movie);
                }
                _movie = null;

                // Remove layer from TopScreen
                if (_gauntletLayer != null && ScreenManager.TopScreen != null)
                {
                    _gauntletLayer.IsFocusLayer = false;
                    ScreenManager.TryLoseFocus(_gauntletLayer);
                    ScreenManager.TopScreen.RemoveLayer(_gauntletLayer);
                }
                _gauntletLayer = null;

                // Track A Phase 3: Unregister bulletin from news integration
                CampBulletinIntegration.UnregisterActiveBulletin();

                // Finalize ViewModel
                _dataSource?.OnFinalize();
                _dataSource = null;

                // Unload sprite categories (mirrors native GauntletMenuTownManagementView cleanup).
                _townManagementSpriteCategory?.Unload();
                _townManagementSpriteCategory = null;

                _encyclopediaSpriteCategory?.Unload();
                _encyclopediaSpriteCategory = null;

                // Unpause the game
                if (Campaign.Current != null)
                {
                    Campaign.Current.TimeControlMode = CampaignTimeControlMode.UnstoppablePlay;
                }
            }
            finally
            {
                _isOpen = false;
                _onClosed?.Invoke();
                _onClosed = null;
            }
        }

        public static bool IsOpen => _isOpen;
    }
}
