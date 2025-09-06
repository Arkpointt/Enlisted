using TaleWorlds.Library;

namespace Enlisted.Features.Equipment.UI
{
    /// <summary>
    /// Row container for equipment cards (EXACT SAS pattern: 4 cards per row).
    /// 
    /// Based on SASEquipmentCardRowVM from ServeasaSoldier.
    /// Used by ListPanel with ItemTemplate in main UI.
    /// </summary>
    public class QuartermasterEquipmentRowVM : ViewModel
    {
        [DataSourceProperty]
        public MBBindingList<QuartermasterEquipmentItemVM> Cards { get; private set; }
        
        /// <summary>
        /// Initialize equipment row with cards (EXACT SAS pattern).
        /// </summary>
        public QuartermasterEquipmentRowVM(MBBindingList<QuartermasterEquipmentItemVM> cards)
        {
            Cards = cards;
        }
    }
}
