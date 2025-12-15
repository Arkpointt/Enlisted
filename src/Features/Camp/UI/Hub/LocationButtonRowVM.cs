using TaleWorlds.Library;

namespace Enlisted.Features.Camp.UI.Hub
{
    /// <summary>
    /// Helper ViewModel for organizing location buttons into rows of 3.
    /// Used by the Camp Hub to create a clean grid layout (2 rows × 3 columns = 6 locations).
    /// </summary>
    public class LocationButtonRowVM : ViewModel
    {
        private const int CardsPerRow = 3;
        
        private MBBindingList<LocationButtonVM> _buttons;
        
        public LocationButtonRowVM()
        {
            Buttons = new MBBindingList<LocationButtonVM>();
        }
        
        [DataSourceProperty]
        public MBBindingList<LocationButtonVM> Buttons
        {
            get => _buttons;
            set
            {
                if (_buttons != value)
                {
                    _buttons = value;
                    OnPropertyChangedWithValue(value, nameof(Buttons));
                }
            }
        }
        
        /// <summary>
        /// Whether this row is full (has 3 buttons).
        /// </summary>
        public bool IsFull => Buttons.Count >= CardsPerRow;
        
        /// <summary>
        /// Add a location button to this row.
        /// </summary>
        public void AddButton(LocationButtonVM button)
        {
            if (!IsFull)
            {
                Buttons.Add(button);
            }
        }
    }
}

