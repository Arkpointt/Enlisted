using TaleWorlds.Library;

namespace Enlisted.Features.Camp.UI
{
    /// <summary>
    /// ViewModel for a row of activity cards (3 cards per row).
    /// Used to create a responsive grid layout.
    /// </summary>
    public class ActivityCardRowVM : ViewModel
    {
        private MBBindingList<ActivityCardVM> _cards;

        public ActivityCardRowVM()
        {
            Cards = new MBBindingList<ActivityCardVM>();
        }

        [DataSourceProperty]
        public MBBindingList<ActivityCardVM> Cards
        {
            get => _cards;
            set
            {
                if (_cards != value)
                {
                    _cards = value;
                    OnPropertyChangedWithValue(value, nameof(Cards));
                }
            }
        }

        public void AddCard(ActivityCardVM card)
        {
            Cards.Add(card);
        }

        public bool IsFull => Cards.Count >= 3;
    }
}
