using System;
using System.Linq;
using Enlisted.Features.Schedule.Behaviors;
using Enlisted.Features.Schedule.Models;
using Enlisted.Mod.Core.Triggers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace Enlisted.Features.Camp.UI.Bulletin
{
    /// <summary>
    /// Small "Queue" panel showing today's schedule blocks.
    /// Track A Phase 3: Now integrated with Track B (AI Camp Schedule).
    /// </summary>
    public class CampDailyScheduleVM : ViewModel
    {
        private string _headerText;
        private MBBindingList<CampScheduleQueueItemVM> _items;

        public CampDailyScheduleVM()
        {
            Items = new MBBindingList<CampScheduleQueueItemVM>();
            RefreshValues();
        }

        public override void RefreshValues()
        {
            base.RefreshValues();
            HeaderText = "Today's Schedule";

            Items.Clear();

            // Track A Phase 3: Pull real schedule data from Track B
            var scheduleBehavior = ScheduleBehavior.Instance;
            var currentSchedule = scheduleBehavior?.CurrentSchedule;

            if (currentSchedule == null || currentSchedule.Blocks == null || currentSchedule.Blocks.Count == 0)
            {
                // Fallback: No schedule available
                Items.Add(new CampScheduleQueueItemVM("--", "No schedule", false, false));
                return;
            }

            // Get current active block for highlighting
            var activeBlock = scheduleBehavior.GetCurrentActiveBlock();

            // Display all 6 time blocks in order
            var orderedBlocks = currentSchedule.Blocks
                .OrderBy(b => (int)b.TimeBlock)
                .ToList();

            foreach (var block in orderedBlocks)
            {
                string timeText = GetTimeBlockShortName(block.TimeBlock);
                string taskText = GetBlockTaskName(block);
                bool isActive = activeBlock != null && activeBlock.TimeBlock == block.TimeBlock;
                bool isCompleted = block.IsCompleted;

                Items.Add(new CampScheduleQueueItemVM(timeText, taskText, isActive, isCompleted));
            }
        }

        /// <summary>
        /// Get short time block name for UI display.
        /// Simplified to 4 blocks.
        /// </summary>
        private string GetTimeBlockShortName(TimeBlock timeBlock)
        {
            return timeBlock switch
            {
                TimeBlock.Morning => "Morn",
                TimeBlock.Afternoon => "Aft",
                TimeBlock.Dusk => "Dusk",
                TimeBlock.Night => "Night",
                _ => "???"
            };
        }

        /// <summary>
        /// Get task name from scheduled block.
        /// Converts from localization key or uses block type as fallback.
        /// </summary>
        private string GetBlockTaskName(ScheduledBlock block)
        {
            if (!string.IsNullOrEmpty(block.Title))
            {
                // Use title if available (might be localization key)
                // For now, return as-is. Future: resolve localization
                return block.Title;
            }

            // Fallback: Use block type with friendly formatting
            return block.BlockType switch
            {
                ScheduleBlockType.Rest => "Rest",
                ScheduleBlockType.FreeTime => "Free Time",
                ScheduleBlockType.SentryDuty => "Sentry",
                ScheduleBlockType.PatrolDuty => "Patrol",
                ScheduleBlockType.ScoutingMission => "Scouting",
                ScheduleBlockType.ForagingDuty => "Foraging",
                ScheduleBlockType.TrainingDrill => "Training",
                ScheduleBlockType.WorkDetail => "Work",
                ScheduleBlockType.WatchDuty => "Watch",
                _ => "Duty"
            };
        }

        [DataSourceProperty]
        public string HeaderText
        {
            get => _headerText;
            set
            {
                if (value == _headerText) return;
                _headerText = value;
                OnPropertyChangedWithValue(value, nameof(HeaderText));
            }
        }

        [DataSourceProperty]
        public MBBindingList<CampScheduleQueueItemVM> Items
        {
            get => _items;
            set
            {
                if (value == _items) return;
                _items = value;
                OnPropertyChangedWithValue(value, nameof(Items));
            }
        }
    }
}


