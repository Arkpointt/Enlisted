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

        private void OnTick(float dt)
        {
            // DISABLED: Hotkeys disabled per user request
            // Only process input during appropriate game states
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
        /// Handle promotion hotkey press. (DISABLED)
        /// </summary>
        private void OnPromotionHotkeyPressed()
        {
            // DISABLED: Hotkeys disabled per user request
        }

        /// <summary>
        /// Handle status menu hotkey press. (DISABLED)
        /// </summary>
        private void OnStatusMenuHotkeyPressed()
        {
            // DISABLED: Hotkeys disabled per user request
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
        /// Get XP requirement for next tier.
        /// </summary>
        private int GetNextTierXPRequirement(int currentTier)
        {
            var requirements = new int[] { 0, 500, 2000, 5000, 10000, 18000 };
            return currentTier < 6 ? requirements[currentTier] : 18000;
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
        /// Get rank name for tier.
        /// </summary>
        private string GetRankName(int tier)
        {
            var rankNames = new Dictionary<int, string>
            {
                {1, "Recruit"},
                {2, "Private"}, 
                {3, "Corporal"},
                {4, "Sergeant"},
                {5, "Staff Sergeant"},
                {6, "Master Sergeant"},
                {7, "Veteran"}
            };
            
            return rankNames.ContainsKey(tier) ? rankNames[tier] : $"Tier {tier}";
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
