using TaleWorlds.CampaignSystem;
using TaleWorlds.SaveSystem;

namespace Enlisted.Models
{
    /// <summary>
    /// Data model representing the enlistment state.
    /// Encapsulates all saveable data for the enlistment system.
    /// Properly integrated with Bannerlord's save system.
    /// </summary>
    public class EnlistmentState
    {
        [SaveableField(1)] 
        private bool _isEnlisted;
        
        [SaveableField(2)] 
        private Hero _commander;
        
        [SaveableField(3)] 
        private bool _pendingDetach;
        
        [SaveableField(4)] 
        private bool _playerPartyWasVisible;

        [SaveableField(5)] 
        private bool _waitingInReserve;

        public bool IsEnlisted 
        { 
            get => _isEnlisted; 
            set => _isEnlisted = value; 
        }
        
        public Hero Commander 
        { 
            get => _commander; 
            set => _commander = value; 
        }
        
        public bool PendingDetach 
        { 
            get => _pendingDetach; 
            set => _pendingDetach = value; 
        }
        
        public bool PlayerPartyWasVisible 
        { 
            get => _playerPartyWasVisible; 
            set => _playerPartyWasVisible = value; 
        }

        public bool WaitingInReserve 
        { 
            get => _waitingInReserve; 
            set => _waitingInReserve = value; 
        }

        public EnlistmentState()
        {
            _isEnlisted = false;
            _commander = null;
            _pendingDetach = false;
            _playerPartyWasVisible = true;
            _waitingInReserve = false;
        }

        /// <summary>
        /// Initiates enlistment with the specified commander.
        /// </summary>
        public void Enlist(Hero commander)
        {
            _isEnlisted = true;
            _commander = commander;
            _pendingDetach = false;
            _waitingInReserve = false;
        }

        /// <summary>
        /// Initiates the leave service process.
        /// </summary>
        public void Leave()
        {
            _isEnlisted = false;
            _commander = null;
            _pendingDetach = true;
            _waitingInReserve = false;
        }

        /// <summary>
        /// Clears the pending detach flag after cleanup is complete.
        /// </summary>
        public void CompletePendingDetach()
        {
            _pendingDetach = false;
        }

        /// <summary>
        /// Checks if the commander is still valid for continued service.
        /// </summary>
        public bool IsCommanderValid()
        {
            return _commander != null && 
                   !_commander.IsDead && 
                   _commander.PartyBelongedTo != null;
        }

        /// <summary>
        /// Forces end of service (used when commander becomes unavailable).
        /// </summary>
        public void ForceEndService()
        {
            _isEnlisted = false;
            _commander = null;
            _pendingDetach = true;
            _waitingInReserve = false;
        }
    }
    
    /// <summary>
    /// Save type definer for the Enlisted mod.
    /// Required for proper serialization of custom types.
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