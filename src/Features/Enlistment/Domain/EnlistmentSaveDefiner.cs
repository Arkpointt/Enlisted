using TaleWorlds.SaveSystem;

namespace Enlisted.Features.Enlistment.Domain
{
	// Registers EnlistmentState for the Bannerlord save system (1.2.12)
	public sealed class EnlistmentSaveDefiner : SaveableTypeDefiner
	{
		// Use a unique ID for this assembly's type definer (arbitrary stable number)
		public EnlistmentSaveDefiner() : base(28745101) { }

		protected override void DefineClassTypes()
		{
			AddClassDefinition(typeof(EnlistmentState), 1);
		}

		protected override void DefineContainerDefinitions()
		{
			// Ensure common containers can be serialized if used
			ConstructContainerDefinition(typeof(System.Collections.Generic.List<TaleWorlds.Core.EquipmentElement>));
			ConstructContainerDefinition(typeof(System.Collections.Generic.List<TaleWorlds.Core.ItemObject>));
		}
	}
}


