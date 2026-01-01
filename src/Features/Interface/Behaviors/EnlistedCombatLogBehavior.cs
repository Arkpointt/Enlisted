using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Engine.Screens;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Features.Interface.ViewModels;
using Enlisted.Features.Interface.Utils;
using Enlisted.Mod.Core.Logging;

namespace Enlisted.Features.Interface.Behaviors
{
    /// <summary>
    /// Manages the enlisted combat log UI layer lifecycle and message routing.
    /// Creates the Gauntlet layer when campaign starts and handles visibility
    /// based on enlistment state. Provides singleton access for Harmony patch.
    /// </summary>
    public class EnlistedCombatLogBehavior : CampaignBehaviorBase
    {
        public static EnlistedCombatLogBehavior Instance { get; private set; }
        
        private GauntletLayer _layer;
        private EnlistedCombatLogVM _dataSource;
        private GauntletMovieIdentifier _movie;
        private ScrollablePanel _scrollablePanel;
        private ListPanel _messagesListPanel;
        private bool _isInitialized;
        private float _lastScrollPosition;
        private float _timeSinceLastManualScroll;
        private bool _shouldAutoScroll = true;
        private const float AutoScrollResumeDelay = 6f;
        
        public EnlistedCombatLogBehavior()
        {
            Instance = this;
        }
        
        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.TickEvent.AddNonSerializedListener(this, OnTick);
            CampaignEvents.OnGameOverEvent.AddNonSerializedListener(this, OnGameOver);
        }
        
        public override void SyncData(IDataStore dataStore)
        {
            // No save data needed - messages are transient
        }
        
        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            // Delay initialization until we're sure MapScreen exists
            _isInitialized = false;
        }
        
        private void OnTick(float dt)
        {
            // Initialize UI on first tick when MapScreen is available
            if (!_isInitialized && Campaign.Current != null)
            {
                InitializeUI();
                _isInitialized = true;
            }
            
            // Update ViewModel tick
            if (_dataSource != null)
            {
                _dataSource.Tick(dt);
            }
            
            // Detect if party screen or inventory is open and adjust positioning
            // Only reposition for Gauntlet UI screens that have panels at the bottom,
            // not for text-based GameMenus (like enlisted_status) which don't obstruct the log
            if (_dataSource != null)
            {
                // Check if party screen is open by checking active game state
                bool isPartyScreenOpen = Game.Current?.GameStateManager?.ActiveState is TaleWorlds.CampaignSystem.GameState.PartyState;
                
                UpdatePositioning(isPartyScreenOpen);
            }
            
            // Handle manual scroll detection and auto-scroll behavior
            if (_scrollablePanel != null && _scrollablePanel.VerticalScrollbar != null)
            {
                var scrollbar = _scrollablePanel.VerticalScrollbar;
                float currentScrollPosition = scrollbar.ValueFloat;
                
                // Detect manual scroll (user interaction)
                if (MathF.Abs(currentScrollPosition - _lastScrollPosition) > 0.01f)
                {
                    // User scrolled manually - pause auto-scroll
                    _shouldAutoScroll = false;
                    _timeSinceLastManualScroll = 0f;
                    _lastScrollPosition = currentScrollPosition;
                }
                
                // Update timer if auto-scroll is paused
                if (!_shouldAutoScroll)
                {
                    _timeSinceLastManualScroll += dt;
                    
                    // Resume auto-scroll after delay
                    if (_timeSinceLastManualScroll >= AutoScrollResumeDelay)
                    {
                        _shouldAutoScroll = true;
                    }
                }
                
                // Auto-scroll to bottom if enabled
                if (_shouldAutoScroll)
                {
                    scrollbar.ValueFloat = scrollbar.MaxValue;
                    _lastScrollPosition = scrollbar.ValueFloat;
                }
            }
        }
        
        private void InitializeUI()
        {
            try
            {
                // Find the map screen
                var mapScreen = ScreenManager.TopScreen;
                if (mapScreen == null)
                {
                    ModLogger.Warn("Interface", "Cannot initialize combat log: MapScreen not found");
                    return;
                }
                
                // Create Gauntlet layer with priority 1000 (above map, below menus)
                _layer = new GauntletLayer("EnlistedCombatLog", 1000);
                
                // Configure input: allow mouse input but don't restrict other inputs
                _layer.InputRestrictions.SetInputRestrictions(false);
                
                // Create ViewModel
                _dataSource = new EnlistedCombatLogVM();
                
                // Load prefab
                _movie = _layer.LoadMovie("EnlistedCombatLog", _dataSource);
                
                // Get reference to ScrollablePanel widget for manual scroll control
                var rootWidget = _movie.Movie.RootWidget;
                _scrollablePanel = rootWidget.FindChild("ScrollablePanel", true) as ScrollablePanel;
                if (_scrollablePanel != null && _scrollablePanel.VerticalScrollbar != null)
                {
                    _lastScrollPosition = _scrollablePanel.VerticalScrollbar.ValueFloat;
                    
                    // Subscribe to scroll events for immediate detection
                    _scrollablePanel.OnScroll += OnUserScroll;
                }
                
                // Get reference to the messages ListPanel to subscribe to link clicks
                _messagesListPanel = rootWidget.FindChild("InnerPanel", true) as ListPanel;
                
                // Subscribe to ViewModel Messages collection changes to handle new message widgets
                _dataSource.Messages.ListChanged += OnMessagesListChanged;
                
                // Add layer to screen
                mapScreen.AddLayer(_layer);
                
                ModLogger.Info("Interface", "Enlisted combat log initialized successfully");
            }
            catch (System.Exception ex)
            {
                ModLogger.Error("Interface", $"Failed to initialize combat log: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// Called when user scrolls the panel with mouse wheel.
        /// Immediately pauses auto-scroll and resets timer.
        /// </summary>
        private void OnUserScroll(float scrollAmount)
        {
            // User is actively scrolling - pause auto-scroll and reset timer
            _shouldAutoScroll = false;
            _timeSinceLastManualScroll = 0f;
            
            if (_scrollablePanel?.VerticalScrollbar != null)
            {
                _lastScrollPosition = _scrollablePanel.VerticalScrollbar.ValueFloat;
            }
        }
        
        /// <summary>
        /// Called when the Messages collection changes (items added/removed).
        /// Subscribes to RichTextWidget EventFire events for new messages.
        /// </summary>
        private void OnMessagesListChanged(object sender, TaleWorlds.Library.ListChangedEventArgs e)
        {
            // Wait one frame for the UI to create the widget, then subscribe
            if (_messagesListPanel != null && e.ListChangedType == TaleWorlds.Library.ListChangedType.ItemAdded)
            {
                // Schedule subscription for next frame when widget exists
                _messagesListPanel.EventManager.AddLateUpdateAction(_messagesListPanel, dt =>
                {
                    SubscribeToMessageWidget(e.NewIndex);
                }, 1);
            }
        }
        
        /// <summary>
        /// Subscribes to the RichTextWidget EventFire event for a specific message at the given index.
        /// </summary>
        private void SubscribeToMessageWidget(int messageIndex)
        {
            if (_messagesListPanel == null || messageIndex < 0 || messageIndex >= _messagesListPanel.ChildCount)
                return;
            
            var messageWidget = _messagesListPanel.GetChild(messageIndex);
            
            // ItemTemplate widgets may not preserve ID attribute, so search by type instead
            // The message widget should have exactly one RichTextWidget child
            RichTextWidget richTextWidget = null;
            if (messageWidget != null)
            {
                for (int i = 0; i < messageWidget.ChildCount; i++)
                {
                    var child = messageWidget.GetChild(i);
                    if (child is RichTextWidget rtw)
                    {
                        richTextWidget = rtw;
                        break;
                    }
                }
            }
            
            if (richTextWidget != null)
            {
                richTextWidget.EventFire += OnRichTextWidgetEvent;
            }
        }
        
        /// <summary>
        /// Handles EventFire events from RichTextWidget instances.
        /// Processes LinkClick events and opens encyclopedia pages.
        /// </summary>
        private void OnRichTextWidgetEvent(Widget widget, string eventName, object[] args)
        {
            if (eventName != "LinkClick") return;
            
            if (args == null || args.Length == 0) return;
            
            // The RichTextWidget passes the href value (already stripped of "event:" prefix) as the first argument
            string encyclopediaLink = args[0] as string;
            
            if (string.IsNullOrEmpty(encyclopediaLink)) return;
            
            try
            {
                // Open the encyclopedia page
                Campaign.Current.EncyclopediaManager.GoToLink(encyclopediaLink);
                
                // Reset inactivity timer on interaction
                _dataSource?.OnUserInteraction();
                
                ModLogger.Debug("Interface", $"Opened encyclopedia link from combat log: {encyclopediaLink}");
            }
            catch (System.Exception ex)
            {
                ModLogger.Error("Interface", $"Failed to open encyclopedia link: {encyclopediaLink}", ex);
            }
        }
        
        /// <summary>
        /// Called by Harmony patch to add messages to the log.
        /// Colorizes kingdom links with faction-specific colors, preserves all other formatting.
        /// </summary>
        public void AddMessage(InformationMessage message)
        {
            if (_dataSource == null || message == null)
            {
                return;
            }
            
            // Log ALL messages to see what's actually coming through
            if (message.Information.Contains("<a "))
            {
                ModLogger.Info("Interface", $"Message with link: {message.Information}");
            }
            
            // Replace generic Link.Kingdom styles with faction-specific colors
            // This makes each kingdom appear in its native banner color
            var colorizedText = FactionLinkColorizer.ColorizeFactionLinks(message.Information);
            
            // Create new message with colorized text
            var colorizedMessage = new InformationMessage(
                colorizedText,
                message.Color,
                message.SoundEventPath
            );
            
            _dataSource.AddMessage(colorizedMessage);
            
            // Don't force auto-scroll here - let the timer-based logic in OnTick handle it
            // This respects the user's manual scroll and the 6-second pause delay
        }
        
        /// <summary>
        /// Updates positioning to avoid menu overlap (party screen, inventory, etc.).
        /// Automatically called each tick based on MapScreen.IsInMenu detection.
        /// </summary>
        public void UpdatePositioning(bool isMenuOpen)
        {
            _dataSource?.UpdatePositioning(isMenuOpen);
        }
        
        private void OnGameOver()
        {
            CleanUp();
        }
        
        private void CleanUp()
        {
            try
            {
                // Unsubscribe from scroll events
                if (_scrollablePanel != null)
                {
                    _scrollablePanel.OnScroll -= OnUserScroll;
                    _scrollablePanel = null;
                }
                
                // Unsubscribe from message list events
                if (_dataSource != null)
                {
                    _dataSource.Messages.ListChanged -= OnMessagesListChanged;
                }
                
                // Unsubscribe from all existing RichTextWidget EventFire events
                if (_messagesListPanel != null)
                {
                    for (int i = 0; i < _messagesListPanel.ChildCount; i++)
                    {
                        var messageWidget = _messagesListPanel.GetChild(i);
                        
                        // Find RichTextWidget by type (same as subscription logic)
                        RichTextWidget richTextWidget = null;
                        if (messageWidget != null)
                        {
                            for (int j = 0; j < messageWidget.ChildCount; j++)
                            {
                                var child = messageWidget.GetChild(j);
                                if (child is RichTextWidget rtw)
                                {
                                    richTextWidget = rtw;
                                    break;
                                }
                            }
                        }
                        
                        if (richTextWidget != null)
                        {
                            richTextWidget.EventFire -= OnRichTextWidgetEvent;
                        }
                    }
                    
                    _messagesListPanel = null;
                }
                
                if (_layer != null)
                {
                    // Release movie first
                    _layer.ReleaseMovie(_movie);
                    
                    // Remove layer from screen
                    var mapScreen = ScreenManager.TopScreen;
                    if (mapScreen != null)
                    {
                        mapScreen.RemoveLayer(_layer);
                    }
                    _layer = null;
                }
                
                // Clean up ViewModel
                if (_dataSource != null)
                {
                    _dataSource.OnFinalize();
                    _dataSource = null;
                }
                
                Instance = null;
                _isInitialized = false;
                
                ModLogger.Info("Interface", "Enlisted combat log cleaned up");
            }
            catch (System.Exception ex)
            {
                ModLogger.Error("Interface", $"Error during combat log cleanup: {ex.Message}");
            }
        }
    }
}
