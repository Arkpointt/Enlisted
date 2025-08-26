using TaleWorlds.SaveSystem;
using Enlisted.Core.Models;

namespace Enlisted.Core.Persistence
{
    /// <summary>
    /// Save-type definer required by Bannerlord for custom classes.
    /// Centralizes save game schema definitions following blueprint.
    /// </summary>
    public class EnlistedSaveDefiner : SaveableTypeDefiner
    {
        public EnlistedSaveDefiner() : base(580669) { }

        protected override void DefineClassTypes()
        {
            AddClassDefinition(typeof(EnlistmentState), 1);
            AddClassDefinition(typeof(PromotionState), 2);
        }
    }
}
