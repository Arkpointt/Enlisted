using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using Enlisted.Features.Enlistment.Behaviors;
using EnlistedConfig = Enlisted.Features.Assignments.Core.ConfigurationManager;
using Enlisted.Mod.Core.Logging;

namespace Enlisted.Features.Interface.Behaviors
{
    /// <summary>
    /// Handles keyboard input for enlisted military service system.
    /// 
    /// Provides hotkey support for:
    /// - 'P' key: Open promotion/advancement menu (when available)
    /// - 'N' key: Open enlisted status menu (when enlisted)
    /// </summary>
    public sealed class EnlistedInputHandler : CampaignBehaviorBase
    {
        public static EnlistedInputHandler Instance { get; private set; }

        // Hotkey definitions
        private const InputKey PROMOTION_HOTKEY = InputKey.P;
        private const InputKey STATUS_MENU_HOTKEY = InputKey.N;

        // Input state tracking
        private bool _lastPromotionKeyState = false;
        private bool _lastStatusKeyState = false;
        private CampaignTime _lastPromotionNotification = CampaignTime.Zero;

        public EnlistedInputHandler()
        {
            Instance = this;
        }

        public override void RegisterEvents()
        {
            CampaignEvents.TickEvent.AddNonSerializedListener(this, OnTick);
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        }

        public override void SyncData(IDataStore dataStore)
        {
            // Input handler has no persistent state
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            ModLogger.Info("Interface", "Enlisted input handler initialized");
        }

        /// <summary>
        /// Called every game frame to handle keyboard input for enlisted system.
        /// Checks for hotkey presses and triggers appropriate actions.
        /// Exits early if the player is not enlisted to avoid unnecessary processing.
        /// </summary>
        /// <param name="dt">Time elapsed since last frame, in seconds.</param>
        private void OnTick(float dt)
        {
            // Skip all processing if the player is not currently enlisted
            // This avoids unnecessary computation when the system isn't active
            var enlistmentBehavior = EnlistmentBehavior.Instance;
            if (enlistmentBehavior?.IsEnlisted != true)
            {
                return;
            }
            
            // Hotkeys are currently disabled per user request
            // The following code is commented out but can be re-enabled if needed:
            // if (!ShouldProcessInput())
            //     return;
            // HandlePromotionHotkey();
            // HandleStatusMenuHotkey();
        }

        /// <summary>
        /// Check if input should be processed based on current game state.
        /// </summary>
        private bool ShouldProcessInput()
        {
            try
            {
                // Don't process input if a conversation is active
                if (Campaign.Current?.ConversationManager?.IsConversationInProgress == true)
                    return false;

                // Basic check - only process during campaign
                if (Campaign.Current == null)
                    return false;

                return true;
            }
            catch
            {
                return false; // Safe fallback
            }
        }

        /// <summary>
        /// Handle 'P' key for promotion/advancement menu.
        /// </summary>
        private void HandlePromotionHotkey()
        {
            try
            {
                bool currentKeyState = Input.IsKeyPressed(PROMOTION_HOTKEY);
                
                // Detect key press (not held)
                if (currentKeyState && !_lastPromotionKeyState)
                {
                    OnPromotionHotkeyPressed();
                }
                
                _lastPromotionKeyState = currentKeyState;
            }
            catch (Exception ex)
            {
                ModLogger.Error("Interface", $"Error handling promotion hotkey: {ex.Message}");
            }
        }

        /// <summary>
        /// Handle 'N' key for enlisted status menu.
        /// </summary>
        private void HandleStatusMenuHotkey()
        {
            try
            {
                bool currentKeyState = Input.IsKeyPressed(STATUS_MENU_HOTKEY);
                
                // Detect key press (not held)
                if (currentKeyState && !_lastStatusKeyState)
                {
                    OnStatusMenuHotkeyPressed();
                }
                
                _lastStatusKeyState = currentKeyState;
            }
            catch (Exception ex)
            {
                ModLogger.Error("Interface", $"Error handling status menu hotkey: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles the 'P' key press to open the promotion/advancement menu.
        /// Currently disabled per user request, but can be re-enabled if needed.
        /// </summary>
        private void OnPromotionHotkeyPressed()
        {
            // Hotkeys are currently disabled per user request
            // This method would open the promotion menu when hotkeys are enabled
        }

        /// <summary>
        /// Handles the 'N' key press to open the enlisted status menu.
        /// Currently disabled per user request, but can be re-enabled if needed.
        /// </summary>
        private void OnStatusMenuHotkeyPressed()
        {
            // Hotkeys are currently disabled per user request
            // This method would open the enlisted status menu when hotkeys are enabled
        }

        /// <summary>
        /// Check if promotion is currently available.
        /// </summary>
        private bool IsPromotionAvailable()
        {
            var enlistment = EnlistmentBehavior.Instance;
            if (!enlistment?.IsEnlisted == true || enlistment.EnlistmentTier >= 6)
                return false;

            var nextTierXP = GetNextTierXPRequirement(enlistment.EnlistmentTier);
            return enlistment.EnlistmentXP >= nextTierXP;
        }

        /// <summary>
        /// Get XP requirement for next tier from progression_config.json.
        /// </summary>
        private int GetNextTierXPRequirement(int currentTier)
        {
            // Load from progression_config.json instead of hardcoded values
            return Enlisted.Features.Assignments.Core.ConfigurationManager.GetXPRequiredForTier(currentTier);
        }

        /// <summary>
        /// Show promotion notification when player reaches XP threshold.
        /// </summary>
        public void TriggerPromotionNotification()
        {
            // Prevent spam notifications
            if (CampaignTime.Now - _lastPromotionNotification < CampaignTime.Hours(1))
                return;

            var enlistment = EnlistmentBehavior.Instance;
            if (!enlistment?.IsEnlisted == true)
                return;

            var nextTier = enlistment.EnlistmentTier + 1;
            var nextRankName = GetRankName(nextTier);
            
            ShowNotification($"Promotion available! You can advance to {nextRankName} (Tier {nextTier}). Press 'P' to choose your advancement!", "event:/ui/notification/quest_update");
            
            _lastPromotionNotification = CampaignTime.Now;
            ModLogger.Info("Interface", $"Promotion notification triggered for Tier {nextTier}");
        }

        /// <summary>
        /// Get rank name for tier from configuration.
        /// Uses tier_names from enlisted_config.json via progression config.
        /// </summary>
        private string GetRankName(int tier)
        {
            return EnlistedConfig.GetTierName(tier);
        }

        /// <summary>
        /// Show notification to player with sound.
        /// </summary>
        private void ShowNotification(string text, string soundEvent = "")
        {
            try
            {
                var message = new TextObject(text);
                InformationManager.DisplayMessage(new InformationMessage(message.ToString()));
            }
            catch (Exception ex)
            {
                ModLogger.Error("Interface", $"Error showing notification: {ex.Message}");
            }
        }

        /// <summary>
        /// Public method for external systems to check promotion availability.
        /// </summary>
        public bool CheckPromotionAvailability()
        {
            return IsPromotionAvailable();
        }
    }
}
