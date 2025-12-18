using System.Collections.Generic;
using TaleWorlds.SaveSystem;

namespace Enlisted.Features.Schedule.Models
{
	/// <summary>
	/// Save definitions for Schedule model types.
	///
	/// This is required because `ScheduleBehavior.SyncData()` persists `_currentSchedule` and `_performanceTracker`.
	/// Those objects include custom Enlisted types (e.g. `DailySchedule`, `ScheduledBlock`) which must be registered
	/// with Bannerlord's SaveSystem via a `SaveableTypeDefiner`. If they aren't registered, saving can fail once the
	/// values become non-null (often showing up in-game as "cannot create saved data").
	///
	/// Native reference pattern:
	/// - See `WorkshopsCampaignBehaviorTypeDefiner` (CampaignSystem) in the decompile: it defines class types and any
	///   needed container definitions.
	/// </summary>
	public sealed class ScheduleModelsSaveDefiner : SaveableTypeDefiner
	{
		// Keep this base id stable forever once released; changing it will break old saves.
		public ScheduleModelsSaveDefiner() : base(735400) { }

		protected override void DefineEnumTypes()
		{
			// Keep ids unique within this definer (shared id space across enums/classes/etc).
			AddEnumDefinition(typeof(TimeBlock), 1);
			AddEnumDefinition(typeof(ScheduleBlockType), 2);
		}

		protected override void DefineClassTypes()
		{
			// Keep ids unique within this definer (shared id space across enums/classes/etc).
			AddClassDefinition(typeof(DailySchedule), 10);
			AddClassDefinition(typeof(ScheduledBlock), 11);
			AddClassDefinition(typeof(SchedulePerformanceTracker), 12);
			AddClassDefinition(typeof(LanceNeedsSnapshot), 13);
		}

		protected override void DefineContainerDefinitions()
		{
			ConstructContainerDefinition(typeof(List<ScheduledBlock>));
			ConstructContainerDefinition(typeof(List<LanceNeedsSnapshot>));
		}
	}
}


