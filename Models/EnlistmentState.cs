using TaleWorlds.CampaignSystem;
using TaleWorlds.SaveSystem;

namespace Enlisted.Models
{
    /// <summary>
    /// Data persisted for enlistment lifecycle. Accessed only by behaviors/services.
    /// </summary>
    public class EnlistmentState
    {
        [SaveableField(1)] private bool _isEnlisted;
        [SaveableField(2)] private Hero _commander;
        [SaveableField(3)] private bool _pendingDetach;
        [SaveableField(4)] private bool _playerPartyWasVisible;
        [SaveableField(5)] private bool _waitingInReserve;

        public bool IsEnlisted { get => _isEnlisted; set => _isEnlisted = value; }
        public Hero Commander { get => _commander; set => _commander = value; }
        public bool PendingDetach { get => _pendingDetach; set => _pendingDetach = value; }
        public bool PlayerPartyWasVisible { get => _playerPartyWasVisible; set => _playerPartyWasVisible = value; }
        public bool WaitingInReserve { get => _waitingInReserve; set => _waitingInReserve = value; }

        public EnlistmentState()
        {
            _isEnlisted = false;
            _commander = null;
            _pendingDetach = false;
            _playerPartyWasVisible = true;
            _waitingInReserve = false;
        }

        /// <summary>Start service under a commander.</summary>
        public void Enlist(Hero commander)
        {
            _isEnlisted = true;
            _commander = commander;
            _pendingDetach = false;
            _waitingInReserve = false;
        }

        /// <summary>Initiate leaving service; behavior performs cleanup then clears flag.</summary>
        public void Leave()
        {
            _isEnlisted = false;
            _commander = null;
            _pendingDetach = true;
            _waitingInReserve = false;
        }

        public void CompletePendingDetach() => _pendingDetach = false;

        /// <summary>Commander still valid and in a party.</summary>
        public bool IsCommanderValid() => _commander != null && !_commander.IsDead && _commander.PartyBelongedTo != null;

        /// <summary>Emergency termination of service due to invalid commander or errors.</summary>
        public void ForceEndService()
        {
            _isEnlisted = false;
            _commander = null;
            _pendingDetach = true;
            _waitingInReserve = false;
        }
    }
    
    /// <summary>
    /// Save-type definer required by Bannerlord for custom classes.
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
