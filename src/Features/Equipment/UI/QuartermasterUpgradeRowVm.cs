using TaleWorlds.Library;

namespace Enlisted.Features.Equipment.UI
{
    /// <summary>
    /// Represents a row of upgrade cards in the upgrade grid (up to 4 cards per row).
    /// </summary>
    public class QuartermasterUpgradeRowVm : ViewModel
    {
        [DataSourceProperty]
        public MBBindingList<QuartermasterUpgradeCardVm> Cards { get; }

        public QuartermasterUpgradeRowVm()
        {
            Cards = new MBBindingList<QuartermasterUpgradeCardVm>();
        }
    }
}
