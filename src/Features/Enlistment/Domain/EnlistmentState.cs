using System;
using TaleWorlds.SaveSystem;
using TaleWorlds.CampaignSystem;

namespace Enlisted.Core.Models
{
    /// <summary>
    /// Represents the current state of a player's enlistment.
    /// Handles contract lifecycle, commander tracking, and save-game persistence.
    /// </summary>
    public class EnlistmentState
    {
        [SaveableField(1)] private bool _isEnlisted;
        [SaveableField(2)] private Hero _commander;
        [SaveableField(3)] private bool _pendingDetach;
        [SaveableField(4)] private bool _playerPartyWasVisible = true;
        [SaveableField(5)] private bool _waitingInReserve;

        // Public read-only properties
        public bool IsEnlisted => _isEnlisted;
        public Hero Commander => _commander;
        public bool PendingDetach => _pendingDetach;
        public bool PlayerPartyWasVisible => _playerPartyWasVisible;
        public bool WaitingInReserve => _waitingInReserve;

        /// <summary>Begin enlistment with the specified commander.</summary>
        public void Enlist(Hero commander)
        {
            _isEnlisted = true;
            _commander = commander;
            _pendingDetach = false;
            _waitingInReserve = false;
        }

        /// <summary>End enlistment and mark for cleanup.</summary>
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

        /// <summary>Set visibility state for save/load persistence.</summary>
        public void SetPlayerPartyVisibility(bool wasVisible) => _playerPartyWasVisible = wasVisible;
    }
}
