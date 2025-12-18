using TaleWorlds.Library;

namespace Enlisted.Features.Camp.UI.Management
{
    /// <summary>
    /// Army tab ViewModel - Other lance statuses.
    /// For v1.0.0 Army Lance Activity Simulation.
    /// Stub for now - T5+ only feature.
    /// </summary>
    public class CampArmyVM : ViewModel
    {
        private bool _show;
        
        public CampArmyVM()
        {
        }
        
        public override void RefreshValues()
        {
            base.RefreshValues();
        }
        
        [DataSourceProperty]
        public bool Show
        {
            get => _show;
            set
            {
                if (value == _show) return;
                _show = value;
                OnPropertyChangedWithValue(value, nameof(Show));
            }
        }
    }
}

