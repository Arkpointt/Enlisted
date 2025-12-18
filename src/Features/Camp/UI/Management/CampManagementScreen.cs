using System;
using Enlisted.Mod.Core.Logging;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ScreenSystem;
using TaleWorlds.TwoDimension;

namespace Enlisted.Features.Camp.UI.Management
{
    /// <summary>
    /// Kingdom-style full-screen for Camp Management.
    /// Uses layer-based pattern similar to CampBulletinScreen.
    /// </summary>
    public static class CampManagementScreen
    {
        private const string LogCategory = "CampManagement";
        
        private static bool _isOpen;
        private static GauntletLayer _gauntletLayer;
        private static GauntletMovieIdentifier _movie;
        private static CampManagementVM _dataSource;
        private static Action _onClosed;
        private static SpriteCategory _kingdomCategory;
        private static SpriteCategory _clanCategory;
        private static SpriteCategory _townManagementCategory;
        private static int _initialTab;
        
        /// <summary>
        /// Static helper to open the Camp Management screen.
        /// </summary>
        /// <param name="initialTab">Tab to open (0=Lance, 1=Schedule, 2=Activities, 3=Reports, 4=Army)</param>
        public static void Open(int initialTab = 1, Action onClosed = null)
        {
            if (_isOpen)
            {
                return;
            }
            
            ModLogger.Info(LogCategory, "Open called");
            
            _isOpen = true;
            _onClosed = onClosed;
            _initialTab = initialTab;
            
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
                    
                    // Create Gauntlet layer with high priority
                    _gauntletLayer = new GauntletLayer("CampManagementLayer", 1001);
                    
                    // Load sprite categories (Kingdom screen resources)
                    _kingdomCategory = UIResourceManager.LoadSpriteCategory("ui_kingdom");
                    if (_kingdomCategory != null && !_kingdomCategory.IsLoaded)
                    {
                        _kingdomCategory.Load();
                    }
                    
                    _clanCategory = UIResourceManager.LoadSpriteCategory("ui_clan");
                    if (_clanCategory != null && !_clanCategory.IsLoaded)
                    {
                        _clanCategory.Load();
                    }
                    
                    _townManagementCategory = UIResourceManager.LoadSpriteCategory("ui_town_management");
                    if (_townManagementCategory != null && !_townManagementCategory.IsLoaded)
                    {
                        _townManagementCategory.Load();
                    }
                    
                    // Create ViewModel
                    _dataSource = new CampManagementVM(Close);
                    _dataSource.RefreshValues();
                    
                    // Set initial tab
                    if (_initialTab >= 0)
                    {
                        _dataSource.SetSelectedCategory(_initialTab);
                    }
                    
                    // Load the Camp Management UI
                    _movie = _gauntletLayer.LoadMovie("CampManagement", _dataSource);
                    if (_movie == null)
                    {
                        ModLogger.Error(LogCategory, "LoadMovie returned null for 'CampManagement'. Check prefab.");
                        InformationManager.DisplayMessage(new InformationMessage(
                            new TextObject("{=enl_camp_ui_failed_to_load}Enlisted: Camp Management UI failed to load.").ToString()));
                        Close();
                        return;
                    }
                    
                    // Register hotkeys and set input restrictions
                    var genericPanel = HotKeyManager.GetCategory("GenericPanelGameKeyCategory");
                    if (!_gauntletLayer.Input.IsCategoryRegistered(genericPanel))
                    {
                        _gauntletLayer.Input.RegisterHotKeyCategory(genericPanel);
                    }
                    
                    var campaignPanel = HotKeyManager.GetCategory("GenericCampaignPanelsGameKeyCategory");
                    if (!_gauntletLayer.Input.IsCategoryRegistered(campaignPanel))
                    {
                        _gauntletLayer.Input.RegisterHotKeyCategory(campaignPanel);
                    }
                    
                    _gauntletLayer.InputRestrictions.SetInputRestrictions();
                    
                    // Set input keys
                    _dataSource.SetDoneInputKey(genericPanel.GetHotKey("Confirm"));
                    _dataSource.SetPreviousTabInputKey(genericPanel.GetHotKey("SwitchToPreviousTab"));
                    _dataSource.SetNextTabInputKey(genericPanel.GetHotKey("SwitchToNextTab"));
                    
                    // Add layer to TopScreen
                    ScreenManager.TopScreen.AddLayer(_gauntletLayer);
                    
                    _gauntletLayer.IsFocusLayer = true;
                    ScreenManager.TrySetFocus(_gauntletLayer);
                    
                    // Pause the game
                    if (Campaign.Current != null)
                    {
                        Campaign.Current.TimeControlMode = CampaignTimeControlMode.Stop;
                    }
                    
                    ModLogger.Info(LogCategory, "Camp Management layer opened successfully.");
                }
                catch (Exception ex)
                {
                    ModLogger.ErrorCode(LogCategory, "E-CAMPUI-060", "Exception while opening Camp Management UI.", ex);
                    InformationManager.DisplayMessage(new InformationMessage(
                        new TextObject("{=enl_camp_ui_failed_to_open}Enlisted: Camp Management UI failed to open.").ToString()));
                    Close();
                }
            });
        }
        
        /// <summary>
        /// Called each frame to handle input (ESC to close, tab switching).
        /// </summary>
        public static void Tick()
        {
            if (!_isOpen || _gauntletLayer == null || _dataSource == null)
            {
                return;
            }
            
            // Handle tab switching with hotkeys
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
            
            // Handle ESC key to close
            if (_gauntletLayer.Input.IsKeyReleased(InputKey.Escape))
            {
                Close();
            }
            
            _dataSource?.OnFrameTick();
        }
        
        public static void Close()
        {
            if (!_isOpen && _gauntletLayer == null)
            {
                return;
            }
            
            ModLogger.Debug(LogCategory, "Closing Camp Management layer");
            
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
                
                // Finalize ViewModel
                _dataSource?.OnFinalize();
                _dataSource = null;
                
                // Unload sprite categories
                _kingdomCategory?.Unload();
                _kingdomCategory = null;
                
                _clanCategory?.Unload();
                _clanCategory = null;
                
                _townManagementCategory?.Unload();
                _townManagementCategory = null;
                
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

