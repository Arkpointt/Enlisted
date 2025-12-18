using System;
using System.Linq;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Features.Ranks;
using Enlisted.Features.Schedule.Behaviors;
using Enlisted.Features.Schedule.Models;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace Enlisted.Features.Camp.UI.Management
{
    /// <summary>
    /// Schedule tab ViewModel - Policy-style interface for lance scheduling.
    /// Uses the same pattern as native KingdomPoliciesVM.
    /// Integrates with ScheduleBehavior authority system for tier/role-based permissions.
    /// </summary>
    public class CampScheduleVM : ViewModel
    {
        // Schedule lists (mirrors ActivePolicies/OtherPolicies)
        private MBBindingList<ScheduleBlockItemVM> _todaysSchedule;
        private MBBindingList<ActivityItemVM> _availableActivities;
        
        // Current selection
        private ScheduleBlockItemVM _currentSelectedBlock;
        private ActivityItemVM _currentSelectedActivity;
        private bool _isAcceptableItemSelected;
        
        // Generic selection display (shows either selected block or activity info)
        private string _selectedTitle;
        private string _selectedDescription;
        private MBBindingList<EffectItemVM> _selectedEffects;
        private bool _isBlockSelected;      // True if a scheduled block is selected
        private bool _isActivitySelected;   // True if an available activity is selected
        
        // Permission state (driven by ScheduleBehavior authority system)
        private ScheduleAuthorityLevel _authorityLevel;
        private bool _canViewOnly;           // T1-T2
        private bool _canRequestChange;      // T3-T4
        private bool _canSetWithGuidance;    // T5-T6 non-Leader
        private bool _canSetFully;           // Lance Leader
        private int _approvalLikelihood;
        private bool _showApprovalSlider;    // Only for T3-T4 requests
        private bool _isManualMode;          // Lance Leader manual control
        
        // Text strings
        private string _ordersText;
        private string _todaysScheduleText;
        private string _availableActivitiesText;
        private string _numScheduledBlocksText;
        private string _numAvailableActivitiesText;
        private string _noItemSelectedText;
        private string _approvalText;
        private string _approvalLikelihoodText;
        private string _actionExplanationText;
        private string _setScheduleText;
        private string _requestChangeText;
        private string _aiRecommendationText;
        private string _revertToAutoText;
        private string _authorityLevelText;
        
        // Phase 6: Cycle tracking display
        private string _cycleDayText;
        private string _daysUntilMusterText;
        private int _currentCycleDay;
        private int _daysUntilMuster;
        
        // Visibility
        private bool _show;

        // Hover tooltips for action buttons (used by Gauntlet HoverText).
        private string _setScheduleHoverText;
        private string _requestChangeHoverText;
        
        public CampScheduleVM()
        {
            TodaysSchedule = new MBBindingList<ScheduleBlockItemVM>();
            AvailableActivities = new MBBindingList<ActivityItemVM>();
            SelectedEffects = new MBBindingList<EffectItemVM>();
            RefreshValues();
        }
        
        public override void RefreshValues()
        {
            base.RefreshValues();
            
            // Get the player's culture-specific rank for the header
            var enlistment = EnlistmentBehavior.Instance;
            var playerRank = RankHelper.GetCurrentRank(enlistment);
            
            // Get lance name if available
            var lanceAssignment = Enlisted.Features.Assignments.Core.LanceRegistry.ResolveLanceById(enlistment?.CurrentLanceId);
            var lanceName = lanceAssignment?.Name ?? "your lance";
            
            // "As a Miles in The Red Chevron, your orders for today:"
            OrdersText = $"As a {playerRank} in {lanceName}, your orders:";
            
            TodaysScheduleText = "Schedule";
            AvailableActivitiesText = "Available Activities";
            NoItemSelectedText = "Select a time block to see details";
            ApprovalText = "Lord Relation";
            SetScheduleText = "Set Activity";
            RequestChangeText = "Request Change";
            
            RefreshScheduleList();
            RefreshPermissions();
            RefreshCycleInfo();
            RefreshActionButtonState();
            RefreshLordRelation();
        }

        /// <summary>
        /// Refresh the Lord Relation bar.
        /// Shows the player's relation with the enlisted lord (-100 to +100).
        /// </summary>
        private void RefreshLordRelation()
        {
            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment == null || enlistment.CurrentLord == null)
            {
                ApprovalLikelihood = 50; // Neutral center
                ApprovalLikelihoodText = "Unknown";
                return;
            }

            var lord = enlistment.CurrentLord;
            var relation = Hero.MainHero.GetRelation(lord);

            // Normalize -100..+100 to 0..100 for slider
            // -100 -> 0
            // 0 -> 50
            // +100 -> 100
            ApprovalLikelihood = (relation + 100) / 2;

            // Text display
            string status = relation switch
            {
                >= 80 => "Devoted",
                >= 50 => "Loyal",
                >= 20 => "Friendly",
                >= 5 => "Favorable",
                >= -5 => "Neutral",
                >= -20 => "Cold",
                >= -50 => "Resentful",
                _ => "Hostile"
            };

            ApprovalLikelihoodText = $"{relation} ({status})";
        }
        
        /// <summary>
        /// Refresh the schedule list from ScheduleBehavior.
        /// </summary>
        public void RefreshScheduleList()
        {
            TodaysSchedule.Clear();
            AvailableActivities.Clear();
            
            var scheduleBehavior = ScheduleBehavior.Instance;
            
            Enlisted.Mod.Core.Logging.ModLogger.Info("CampSchedule", $"RefreshScheduleList called - ScheduleBehavior: {(scheduleBehavior != null ? "Found" : "NULL")}");
            
            var currentSchedule = scheduleBehavior?.CurrentSchedule;
            
            Enlisted.Mod.Core.Logging.ModLogger.Info("CampSchedule", $"CurrentSchedule: {(currentSchedule != null ? "Found" : "NULL")}");
            
            if (currentSchedule != null && currentSchedule.Blocks != null)
            {
                Enlisted.Mod.Core.Logging.ModLogger.Info("CampSchedule", $"Schedule has {currentSchedule.Blocks.Count} blocks");
                
                // Add today's scheduled blocks in order
                var orderedBlocks = currentSchedule.Blocks
                    .OrderBy(b => (int)b.TimeBlock)
                    .ToList();
                
                foreach (var block in orderedBlocks)
                {
                    var item = new ScheduleBlockItemVM(block, OnBlockSelect);
                    TodaysSchedule.Add(item);
                    Enlisted.Mod.Core.Logging.ModLogger.Debug("CampSchedule", $"Added block: {block.TimeBlock} - {block.Title}");
                }
            }
            else
            {
                Enlisted.Mod.Core.Logging.ModLogger.Warn("CampSchedule", "No schedule blocks found - schedule may not be generated yet");
            }
            
            // Add available activities from config (filter by player tier/formation)
            var config = scheduleBehavior?.Config;
            var enlistment = EnlistmentBehavior.Instance;
            int playerTier = enlistment?.EnlistmentTier ?? 1;
            
            Enlisted.Mod.Core.Logging.ModLogger.Info("CampSchedule", $"Config: {(config != null ? "Found" : "NULL")}, Activities: {config?.Activities?.Count ?? 0}, PlayerTier: {playerTier}");
            
            if (config != null && config.Activities != null)
            {
                // Filter activities suitable for player
                var availableActivities = config.Activities
                    .Where(a => a.MinTier <= playerTier)
                    .Where(a => a.MaxTier == 0 || a.MaxTier >= playerTier)
                    .ToList();
                
                Enlisted.Mod.Core.Logging.ModLogger.Info("CampSchedule", $"Filtered activities for tier {playerTier}: {availableActivities.Count}");
                
                foreach (var activity in availableActivities)
                {
                    var item = new ActivityItemVM(activity, OnActivitySelect);
                    AvailableActivities.Add(item);
                    Enlisted.Mod.Core.Logging.ModLogger.Debug("CampSchedule", $"Added activity: {activity.Title ?? activity.Id}");
                }
            }
            else
            {
                Enlisted.Mod.Core.Logging.ModLogger.Warn("CampSchedule", "No config or activities available to load");
            }
            
            // Update counts
            NumScheduledBlocksText = $"({TodaysSchedule.Count})";
            NumAvailableActivitiesText = $"({AvailableActivities.Count})";
            
            // Select first block by default
            if (TodaysSchedule.Count > 0)
            {
                OnBlockSelect(TodaysSchedule[0]);
            }
            else
            {
                RefreshActionButtonState();
            }
        }
        
        /// <summary>
        /// Refresh permission state using ScheduleBehavior authority system.
        /// </summary>
        private void RefreshPermissions()
        {
            var scheduleBehavior = ScheduleBehavior.Instance;
            if (scheduleBehavior == null)
            {
                // Fallback if behavior not loaded
                CanViewOnly = true;
                CanRequestChange = false;
                CanSetWithGuidance = false;
                CanSetFully = false;
                AuthorityLevelText = "Recruit";
                ActionExplanationText = "Schedule not available.";
                return;
            }
            
            // Get authority level from ScheduleBehavior
            _authorityLevel = scheduleBehavior.GetPlayerScheduleAuthority();
            
            // Set UI flags based on authority level
            CanViewOnly = _authorityLevel == ScheduleAuthorityLevel.ViewOnly;
            CanRequestChange = _authorityLevel == ScheduleAuthorityLevel.CanRequest;
            CanSetWithGuidance = _authorityLevel == ScheduleAuthorityLevel.GuidedControl;
            CanSetFully = _authorityLevel == ScheduleAuthorityLevel.FullControl;
            
            // Check if in manual mode (Lance Leaders only)
            IsManualMode = scheduleBehavior.IsManualScheduleMode;
            
            // Show approval slider only for T3-T4 requests
            ShowApprovalSlider = CanRequestChange;
            
            // Set authority level text
            var enlistment = EnlistmentBehavior.Instance;
            int tier = enlistment?.EnlistmentTier ?? 1;
            bool isLanceLeader = scheduleBehavior.CanUseManualManagement();
            
            if (isLanceLeader)
            {
                AuthorityLevelText = "Lance Leader";
            }
            else
            {
                // Prefer the culture-appropriate rank string in UI (avoid "Tier" wording).
                AuthorityLevelText = RankHelper.GetCurrentRank(enlistment);
            }
            
            // Set explanation text based on authority level
            switch (_authorityLevel)
            {
                case ScheduleAuthorityLevel.ViewOnly:
                    ActionExplanationText = "Follow your assigned orders. Authority to modify schedule earned through service.";
                    SetScheduleText = "View Details";
                    break;
                    
                case ScheduleAuthorityLevel.CanRequest:
                    ActionExplanationText = "You may request schedule changes. Your lord will consider your request.";
                    RequestChangeText = "Request Change";
                    break;
                    
                case ScheduleAuthorityLevel.GuidedControl:
                    ActionExplanationText = "As a senior soldier, you help decide the schedule. AI recommends based on lance needs.";
                    SetScheduleText = "Set Activity";
                    AiRecommendationText = "AI Recommendation";
                    break;
                    
                case ScheduleAuthorityLevel.FullControl:
                    if (IsManualMode)
                    {
                        ActionExplanationText = "Lance Leader: You control the schedule. Set activities as you see fit.";
                    }
                    else
                    {
                        ActionExplanationText = "Lance Leader: Currently using AI schedule. You can take manual control.";
                    }
                    SetScheduleText = "Set Activity";
                    RevertToAutoText = "Revert to Auto";
                    break;
            }

            RefreshActionButtonState();
        }
        
        /// <summary>
        /// Handle schedule block selection (mirrors KingdomPoliciesVM.OnPolicySelect).
        /// Calculates approval likelihood if player can request changes.
        /// </summary>
        private void OnBlockSelect(ScheduleBlockItemVM block)
        {
            if (CurrentSelectedBlock == block)
                return;
            
            // Deselect previous block
            if (CurrentSelectedBlock != null)
            {
                CurrentSelectedBlock.IsSelected = false;
            }
            
            // Select new
            CurrentSelectedBlock = block;
            if (CurrentSelectedBlock != null)
            {
                CurrentSelectedBlock.IsSelected = true;
                
                // Update the generic display properties with block info
                UpdateSelectedDisplay(block.Title, block.Description, block.Effects);
                // Keep the activity selection intact (player needs to select block + activity).
                IsBlockSelected = CurrentSelectedBlock != null;
                IsActivitySelected = CurrentSelectedActivity != null;
                
                // Ensure relation is shown
                RefreshLordRelation();
            }
            
            IsAcceptableItemSelected = CurrentSelectedBlock != null || CurrentSelectedActivity != null;
            RefreshActionButtonState();
        }
        
        
        /// <summary>
        /// Handle activity selection for assignment.
        /// Updates approval likelihood if in request mode.
        /// </summary>
        private void OnActivitySelect(ActivityItemVM activity)
        {
            if (CurrentSelectedActivity == activity)
                return;
            
            // Deselect previous activity
            if (CurrentSelectedActivity != null)
            {
                CurrentSelectedActivity.IsSelected = false;
            }
            
            // Select new
            CurrentSelectedActivity = activity;
            if (CurrentSelectedActivity != null)
            {
                CurrentSelectedActivity.IsSelected = true;
                
                // Update the generic display properties with activity info
                UpdateSelectedDisplayForActivity(activity);
                // Keep the block selection intact (player needs to select block + activity).
                IsBlockSelected = CurrentSelectedBlock != null;
                IsActivitySelected = CurrentSelectedActivity != null;
                
                // Ensure relation is shown
                RefreshLordRelation();
            }
            
            IsAcceptableItemSelected = CurrentSelectedBlock != null || CurrentSelectedActivity != null;
            RefreshActionButtonState();
        }

        /// <summary>
        /// Centralized update for action button enable/disable states.
        /// Buttons should stay visible, but go grey + non-clickable unless:
        /// - The player has the authority, AND
        /// - Both a time block and an activity are selected.
        /// </summary>
        private void RefreshActionButtonState()
        {
            // Set Activity: only possible for Guided/Full control, and requires both selections.
            CanSetScheduleAction = (CanSetWithGuidance || CanSetFully)
                                   && CurrentSelectedBlock != null
                                   && CurrentSelectedActivity != null;

            // Request Change: only possible in request mode, and requires both selections.
            CanRequestChangeAction = CanRequestChange
                                     && CurrentSelectedBlock != null
                                     && CurrentSelectedActivity != null;

            // Update hover tooltips so disabled buttons explain *why*.
            SetScheduleHoverText = BuildSetScheduleHoverText();
            RequestChangeHoverText = BuildRequestChangeHoverText();
        }

        private string BuildSetScheduleHoverText()
        {
            // If player lacks authority, explain requirement.
            if (!(CanSetWithGuidance || CanSetFully))
            {
                return "Requires authority to set the schedule (senior soldier or lance leader).";
            }

            // If authority exists but selections are missing, explain what to do.
            if (CurrentSelectedBlock == null && CurrentSelectedActivity == null)
                return "Select a time block and an activity to set the schedule.";
            if (CurrentSelectedBlock == null)
                return "Select a time block (left list) first.";
            if (CurrentSelectedActivity == null)
                return "Select an available activity (left list) first.";

            return "Apply the selected activity to the selected time block.";
        }

        private string BuildRequestChangeHoverText()
        {
            // If player lacks authority, explain requirement.
            if (!CanRequestChange)
            {
                return "Requires authority to request schedule changes.";
            }

            // If authority exists but selections are missing, explain what to do.
            if (CurrentSelectedBlock == null && CurrentSelectedActivity == null)
                return "Select a time block and an activity to request a change.";
            if (CurrentSelectedBlock == null)
                return "Select a time block (left list) first.";
            if (CurrentSelectedActivity == null)
                return "Select an available activity (left list) first.";

            return "Request that your lord approve switching this time block to the selected activity.";
        }
        
        /// <summary>
        /// Execute: Set schedule (T5-T6+ with guidance or Lance Leader full control).
        /// </summary>
        public void ExecuteSetSchedule()
        {
            if ((!CanSetWithGuidance && !CanSetFully) || CurrentSelectedBlock == null || CurrentSelectedActivity == null)
                return;
            
            var scheduleBehavior = ScheduleBehavior.Instance;
            if (scheduleBehavior == null)
                return;
            
            // Lance Leaders can set directly, T5-T6 use guided control
            if (CanSetFully)
            {
                // Lance Leader: Set directly with full control
                var newSchedule = scheduleBehavior.CurrentSchedule;
                var block = newSchedule.Blocks.FirstOrDefault(b => b.TimeBlock == CurrentSelectedBlock.Block.TimeBlock);
                
                if (block != null)
                {
                    block.ActivityId = CurrentSelectedActivity.Activity.Id;
                    block.Title = CurrentSelectedActivity.Activity.Title ?? CurrentSelectedActivity.Activity.TitleKey;
                    block.Description = CurrentSelectedActivity.Activity.Description ?? CurrentSelectedActivity.Activity.DescriptionKey;
                    
                    scheduleBehavior.SetManualSchedule(newSchedule);
                    
                    InformationManager.DisplayMessage(new InformationMessage(
                        $"Schedule updated: {block.TimeBlock} set to {block.Title}"));
                    
                    RefreshScheduleList();
                }
            }
            else if (CanSetWithGuidance)
            {
                // T5-T6 Guided Control: Show AI recommendation and let player decide
                // For now, just apply directly (full guided UI would show comparison)
                var newSchedule = scheduleBehavior.CurrentSchedule;
                var block = newSchedule.Blocks.FirstOrDefault(b => b.TimeBlock == CurrentSelectedBlock.Block.TimeBlock);
                
                if (block != null)
                {
                    block.ActivityId = CurrentSelectedActivity.Activity.Id;
                    block.Title = CurrentSelectedActivity.Activity.Title ?? CurrentSelectedActivity.Activity.TitleKey;
                    block.Description = CurrentSelectedActivity.Activity.Description ?? CurrentSelectedActivity.Activity.DescriptionKey;
                    
                    // Note: In guided mode, AI can still override this if needs become critical
                    InformationManager.DisplayMessage(new InformationMessage(
                        $"Schedule updated (guided): {block.TimeBlock} set to {block.Title}"));
                    
                    RefreshScheduleList();
                }
            }
        }
        
        /// <summary>
        /// Execute: Request schedule change (T3-T4).
        /// Uses real approval system from ScheduleBehavior.
        /// </summary>
        public void ExecuteRequestChange()
        {
            if (!CanRequestChange || CurrentSelectedBlock == null || CurrentSelectedActivity == null)
                return;
            
            var scheduleBehavior = ScheduleBehavior.Instance;
            if (scheduleBehavior == null)
                return;
            
            // Use the real request system with approval roll
            bool approved = scheduleBehavior.RequestScheduleChange(
                CurrentSelectedBlock.Block,
                CurrentSelectedActivity.Activity,
                out int approvalChance
            );
            
            if (approved)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    $"Request approved! {CurrentSelectedBlock.Block.TimeBlock} changed to {CurrentSelectedActivity.Activity.Title ?? CurrentSelectedActivity.Activity.TitleKey}",
                    Colors.Green));
                RefreshScheduleList();
            }
            else
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    $"Request denied ({approvalChance}% chance). Your lord prefers the current schedule.",
                    Colors.Red));
            }
        }
        
        /// <summary>
        /// Execute: Revert to automatic scheduling (Lance Leader only).
        /// </summary>
        public void ExecuteRevertToAuto()
        {
            if (!CanSetFully)
                return;
            
            var scheduleBehavior = ScheduleBehavior.Instance;
            if (scheduleBehavior == null)
                return;
            
            scheduleBehavior.RevertToAutoSchedule();
            
            InformationManager.DisplayMessage(new InformationMessage(
                "Schedule control returned to AI. Daily orders will be generated automatically."));
            
            RefreshScheduleList();
            RefreshPermissions();
        }
        
        /// <summary>
        /// Phase 6: Refresh cycle information from ScheduleBehavior.
        /// </summary>
        private void RefreshCycleInfo()
        {
            var scheduleBehavior = ScheduleBehavior.Instance;
            if (scheduleBehavior == null)
            {
                CycleDayText = "Day 1 of 12";
                DaysUntilMusterText = "Next Muster: 12 days";
                CurrentCycleDay = 1;
                DaysUntilMuster = 12;
                return;
            }
            
            CurrentCycleDay = scheduleBehavior.CurrentCycleDay;
            DaysUntilMuster = scheduleBehavior.DaysUntilMuster;
            
            CycleDayText = $"Day {CurrentCycleDay} of 12";
            DaysUntilMusterText = DaysUntilMuster == 1 
                ? "Next Muster: Tomorrow" 
                : $"Next Muster: {DaysUntilMuster} days";
        }
        
        // ===== Helper Methods =====
        
        /// <summary>
        /// Update the generic display properties with block information.
        /// </summary>
        private void UpdateSelectedDisplay(string title, string description, MBBindingList<EffectItemVM> effects)
        {
            SelectedTitle = title ?? new TextObject("{=enl_schedule_unknown}Unknown").ToString();
            SelectedDescription = description ?? new TextObject("{=enl_schedule_no_description}No description available.").ToString();
            
            SelectedEffects.Clear();
            if (effects != null)
            {
                foreach (var effect in effects)
                {
                    SelectedEffects.Add(effect);
                }
            }
        }
        
        /// <summary>
        /// Update the generic display properties with activity information.
        /// </summary>
        private void UpdateSelectedDisplayForActivity(ActivityItemVM activity)
        {
            if (activity?.Activity == null)
            {
                SelectedTitle = new TextObject("{=enl_schedule_unknown_activity}Unknown Activity").ToString();
                SelectedDescription = new TextObject("{=enl_schedule_no_description}No description available.").ToString();
                SelectedEffects.Clear();
                return;
            }
            
            var def = activity.Activity;
            SelectedTitle = def.Title ?? def.TitleKey ?? def.Id ?? new TextObject("{=enl_schedule_activity_default_title}Activity").ToString();
            SelectedDescription = def.Description ?? def.DescriptionKey ?? new TextObject("{=enl_schedule_activity_default_desc}This activity can be assigned during Free Time.").ToString();
            
            // Build effects list from activity definition
            SelectedEffects.Clear();
            
            // Time block preferences
            if (def.PreferredTimeBlocks != null && def.PreferredTimeBlocks.Count > 0)
            {
                var timeBlocks = string.Join(", ", def.PreferredTimeBlocks);
                SelectedEffects.Add(new EffectItemVM($"Best Time: {timeBlocks}"));
            }
            
            // Fatigue cost
            if (def.FatigueCost > 0)
            {
                SelectedEffects.Add(new EffectItemVM($"Fatigue Cost: {def.FatigueCost}"));
            }
            
            // XP reward
            if (def.XPReward > 0)
            {
                SelectedEffects.Add(new EffectItemVM($"XP Reward: {def.XPReward}"));
            }
            
            // Need recovery effects
            if (def.NeedRecovery != null)
            {
                foreach (var kvp in def.NeedRecovery)
                {
                    if (kvp.Value > 0)
                    {
                        SelectedEffects.Add(new EffectItemVM($"+{kvp.Value} {kvp.Key}"));
                    }
                }
            }
            
            // Need degradation effects
            if (def.NeedDegradation != null)
            {
                foreach (var kvp in def.NeedDegradation)
                {
                    if (kvp.Value > 0)
                    {
                        SelectedEffects.Add(new EffectItemVM($"-{kvp.Value} {kvp.Key}"));
                    }
                }
            }
            
            // Tier requirements
            if (def.MinTier > 1 || def.MaxTier > 0)
            {
                string tierReq = def.MaxTier > 0 
                    ? $"Tier: {def.MinTier}-{def.MaxTier}" 
                    : $"Tier: {def.MinTier}+";
                SelectedEffects.Add(new EffectItemVM(tierReq));
            }
            
            // Formation requirements
            if (def.RequiredFormations != null && def.RequiredFormations.Count > 0)
            {
                var formations = string.Join(", ", def.RequiredFormations);
                SelectedEffects.Add(new EffectItemVM($"Formations: {formations}"));
            }
            
            // If no effects, show placeholder
            if (SelectedEffects.Count == 0)
            {
                SelectedEffects.Add(new EffectItemVM("Standard activity with no special effects."));
            }
        }
        
        // ===== Properties =====
        
        [DataSourceProperty]
        public bool Show
        {
            get => _show;
            set
            {
                if (value == _show) return;
                _show = value;
                OnPropertyChangedWithValue(value, nameof(Show));
                
                // Refresh when tab becomes visible
                if (value)
                {
                    RefreshScheduleList();
                    RefreshPermissions();
                }
            }
        }
        
        [DataSourceProperty]
        public string OrdersText
        {
            get => _ordersText;
            set
            {
                if (value == _ordersText) return;
                _ordersText = value;
                OnPropertyChangedWithValue(value, nameof(OrdersText));
            }
        }
        
        [DataSourceProperty]
        public string TodaysScheduleText
        {
            get => _todaysScheduleText;
            set
            {
                if (value == _todaysScheduleText) return;
                _todaysScheduleText = value;
                OnPropertyChangedWithValue(value, nameof(TodaysScheduleText));
            }
        }
        
        [DataSourceProperty]
        public string AvailableActivitiesText
        {
            get => _availableActivitiesText;
            set
            {
                if (value == _availableActivitiesText) return;
                _availableActivitiesText = value;
                OnPropertyChangedWithValue(value, nameof(AvailableActivitiesText));
            }
        }
        
        [DataSourceProperty]
        public string NumScheduledBlocksText
        {
            get => _numScheduledBlocksText;
            set
            {
                if (value == _numScheduledBlocksText) return;
                _numScheduledBlocksText = value;
                OnPropertyChangedWithValue(value, nameof(NumScheduledBlocksText));
            }
        }
        
        [DataSourceProperty]
        public string NumAvailableActivitiesText
        {
            get => _numAvailableActivitiesText;
            set
            {
                if (value == _numAvailableActivitiesText) return;
                _numAvailableActivitiesText = value;
                OnPropertyChangedWithValue(value, nameof(NumAvailableActivitiesText));
            }
        }
        
        [DataSourceProperty]
        public string NoItemSelectedText
        {
            get => _noItemSelectedText;
            set
            {
                if (value == _noItemSelectedText) return;
                _noItemSelectedText = value;
                OnPropertyChangedWithValue(value, nameof(NoItemSelectedText));
            }
        }
        
        [DataSourceProperty]
        public string ApprovalText
        {
            get => _approvalText;
            set
            {
                if (value == _approvalText) return;
                _approvalText = value;
                OnPropertyChangedWithValue(value, nameof(ApprovalText));
            }
        }
        
        [DataSourceProperty]
        public string ApprovalLikelihoodText
        {
            get => _approvalLikelihoodText;
            set
            {
                if (value == _approvalLikelihoodText) return;
                _approvalLikelihoodText = value;
                OnPropertyChangedWithValue(value, nameof(ApprovalLikelihoodText));
            }
        }
        
        [DataSourceProperty]
        public int ApprovalLikelihood
        {
            get => _approvalLikelihood;
            set
            {
                if (value == _approvalLikelihood) return;
                _approvalLikelihood = value;
                OnPropertyChangedWithValue(value, nameof(ApprovalLikelihood));
            }
        }
        
        [DataSourceProperty]
        public string ActionExplanationText
        {
            get => _actionExplanationText;
            set
            {
                if (value == _actionExplanationText) return;
                _actionExplanationText = value;
                OnPropertyChangedWithValue(value, nameof(ActionExplanationText));
            }
        }
        
        [DataSourceProperty]
        public string SetScheduleText
        {
            get => _setScheduleText;
            set
            {
                if (value == _setScheduleText) return;
                _setScheduleText = value;
                OnPropertyChangedWithValue(value, nameof(SetScheduleText));
            }
        }
        
        [DataSourceProperty]
        public string RequestChangeText
        {
            get => _requestChangeText;
            set
            {
                if (value == _requestChangeText) return;
                _requestChangeText = value;
                OnPropertyChangedWithValue(value, nameof(RequestChangeText));
            }
        }
        
        [DataSourceProperty]
        public bool CanViewOnly
        {
            get => _canViewOnly;
            set
            {
                if (value == _canViewOnly) return;
                _canViewOnly = value;
                OnPropertyChangedWithValue(value, nameof(CanViewOnly));
            }
        }
        
        [DataSourceProperty]
        public bool CanRequestChange
        {
            get => _canRequestChange;
            set
            {
                if (value == _canRequestChange) return;
                _canRequestChange = value;
                OnPropertyChangedWithValue(value, nameof(CanRequestChange));
            }
        }
        
        [DataSourceProperty]
        public bool CanSetWithGuidance
        {
            get => _canSetWithGuidance;
            set
            {
                if (value == _canSetWithGuidance) return;
                _canSetWithGuidance = value;
                OnPropertyChangedWithValue(value, nameof(CanSetWithGuidance));
            }
        }
        
        [DataSourceProperty]
        public bool CanSetFully
        {
            get => _canSetFully;
            set
            {
                if (value == _canSetFully) return;
                _canSetFully = value;
                OnPropertyChangedWithValue(value, nameof(CanSetFully));
            }
        }
        
        [DataSourceProperty]
        public bool ShowApprovalSlider
        {
            get => _showApprovalSlider;
            set
            {
                if (value == _showApprovalSlider) return;
                _showApprovalSlider = value;
                OnPropertyChangedWithValue(value, nameof(ShowApprovalSlider));
            }
        }
        
        [DataSourceProperty]
        public bool IsManualMode
        {
            get => _isManualMode;
            set
            {
                if (value == _isManualMode) return;
                _isManualMode = value;
                OnPropertyChangedWithValue(value, nameof(IsManualMode));
            }
        }
        
        [DataSourceProperty]
        public bool IsAcceptableItemSelected
        {
            get => _isAcceptableItemSelected;
            set
            {
                if (value == _isAcceptableItemSelected) return;
                _isAcceptableItemSelected = value;
                OnPropertyChangedWithValue(value, nameof(IsAcceptableItemSelected));
            }
        }
        
        [DataSourceProperty]
        public MBBindingList<ScheduleBlockItemVM> TodaysSchedule
        {
            get => _todaysSchedule;
            set
            {
                if (value == _todaysSchedule) return;
                _todaysSchedule = value;
                OnPropertyChangedWithValue(value, nameof(TodaysSchedule));
            }
        }
        
        [DataSourceProperty]
        public MBBindingList<ActivityItemVM> AvailableActivities
        {
            get => _availableActivities;
            set
            {
                if (value == _availableActivities) return;
                _availableActivities = value;
                OnPropertyChangedWithValue(value, nameof(AvailableActivities));
            }
        }
        
        [DataSourceProperty]
        public ScheduleBlockItemVM CurrentSelectedBlock
        {
            get => _currentSelectedBlock;
            set
            {
                if (value == _currentSelectedBlock) return;
                _currentSelectedBlock = value;
                OnPropertyChangedWithValue(value, nameof(CurrentSelectedBlock));
            }
        }
        
        [DataSourceProperty]
        public ActivityItemVM CurrentSelectedActivity
        {
            get => _currentSelectedActivity;
            set
            {
                if (value == _currentSelectedActivity) return;
                _currentSelectedActivity = value;
                OnPropertyChangedWithValue(value, nameof(CurrentSelectedActivity));
            }
        }
        
        [DataSourceProperty]
        public string AuthorityLevelText
        {
            get => _authorityLevelText;
            set
            {
                if (value == _authorityLevelText) return;
                _authorityLevelText = value;
                OnPropertyChangedWithValue(value, nameof(AuthorityLevelText));
            }
        }
        
        [DataSourceProperty]
        public string AiRecommendationText
        {
            get => _aiRecommendationText;
            set
            {
                if (value == _aiRecommendationText) return;
                _aiRecommendationText = value;
                OnPropertyChangedWithValue(value, nameof(AiRecommendationText));
            }
        }
        
        [DataSourceProperty]
        public string RevertToAutoText
        {
            get => _revertToAutoText;
            set
            {
                if (value == _revertToAutoText) return;
                _revertToAutoText = value;
                OnPropertyChangedWithValue(value, nameof(RevertToAutoText));
            }
        }
        
        // ===== Generic Selection Display Properties =====
        
        [DataSourceProperty]
        public string SelectedTitle
        {
            get => _selectedTitle;
            set
            {
                if (value == _selectedTitle) return;
                _selectedTitle = value;
                OnPropertyChangedWithValue(value, nameof(SelectedTitle));
            }
        }
        
        [DataSourceProperty]
        public string SelectedDescription
        {
            get => _selectedDescription;
            set
            {
                if (value == _selectedDescription) return;
                _selectedDescription = value;
                OnPropertyChangedWithValue(value, nameof(SelectedDescription));
            }
        }
        
        [DataSourceProperty]
        public MBBindingList<EffectItemVM> SelectedEffects
        {
            get => _selectedEffects;
            set
            {
                if (value == _selectedEffects) return;
                _selectedEffects = value;
                OnPropertyChangedWithValue(value, nameof(SelectedEffects));
            }
        }
        
        [DataSourceProperty]
        public bool IsBlockSelected
        {
            get => _isBlockSelected;
            set
            {
                if (value == _isBlockSelected) return;
                _isBlockSelected = value;
                OnPropertyChangedWithValue(value, nameof(IsBlockSelected));
            }
        }
        
        [DataSourceProperty]
        public bool IsActivitySelected
        {
            get => _isActivitySelected;
            set
            {
                if (value == _isActivitySelected) return;
                _isActivitySelected = value;
                OnPropertyChangedWithValue(value, nameof(IsActivitySelected));
            }
        }

        // ===== Action gating for Orders menu buttons =====
        // These are intentionally separate from authority flags:
        // - Authority says what you're allowed to do in general.
        // - Action gating says whether the button should be clickable right now.

        private bool _canSetScheduleAction;
        private bool _canRequestChangeAction;

        /// <summary>
        /// True only when player can set schedule (guided or full) AND has selected both a block and an activity.
        /// </summary>
        [DataSourceProperty]
        public bool CanSetScheduleAction
        {
            get => _canSetScheduleAction;
            set
            {
                if (value == _canSetScheduleAction) return;
                _canSetScheduleAction = value;
                OnPropertyChangedWithValue(value, nameof(CanSetScheduleAction));
            }
        }

        /// <summary>
        /// True only when player can request changes AND has selected both a block and an activity.
        /// </summary>
        [DataSourceProperty]
        public bool CanRequestChangeAction
        {
            get => _canRequestChangeAction;
            set
            {
                if (value == _canRequestChangeAction) return;
                _canRequestChangeAction = value;
                OnPropertyChangedWithValue(value, nameof(CanRequestChangeAction));
            }
        }

        [DataSourceProperty]
        public string SetScheduleHoverText
        {
            get => _setScheduleHoverText;
            set
            {
                if (value == _setScheduleHoverText) return;
                _setScheduleHoverText = value;
                OnPropertyChangedWithValue(value, nameof(SetScheduleHoverText));
            }
        }

        [DataSourceProperty]
        public string RequestChangeHoverText
        {
            get => _requestChangeHoverText;
            set
            {
                if (value == _requestChangeHoverText) return;
                _requestChangeHoverText = value;
                OnPropertyChangedWithValue(value, nameof(RequestChangeHoverText));
            }
        }

        // ===== Phase 6: Cycle Info Properties =====

        [DataSourceProperty]
        public string CycleDayText
        {
            get => _cycleDayText;
            set
            {
                if (value == _cycleDayText) return;
                _cycleDayText = value;
                OnPropertyChangedWithValue(value, nameof(CycleDayText));
            }
        }

        [DataSourceProperty]
        public string DaysUntilMusterText
        {
            get => _daysUntilMusterText;
            set
            {
                if (value == _daysUntilMusterText) return;
                _daysUntilMusterText = value;
                OnPropertyChangedWithValue(value, nameof(DaysUntilMusterText));
            }
        }

        [DataSourceProperty]
        public int CurrentCycleDay
        {
            get => _currentCycleDay;
            set
            {
                if (value == _currentCycleDay) return;
                _currentCycleDay = value;
                OnPropertyChangedWithValue(value, nameof(CurrentCycleDay));
            }
        }

        [DataSourceProperty]
        public int DaysUntilMuster
        {
            get => _daysUntilMuster;
            set
            {
                if (value == _daysUntilMuster) return;
                _daysUntilMuster = value;
                OnPropertyChangedWithValue(value, nameof(DaysUntilMuster));
            }
        }
    }
}

