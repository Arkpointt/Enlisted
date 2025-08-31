using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ViewModelCollection;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.CampaignSystem.Party;

namespace Enlisted.Features.Enlistment.Domain
{
    [Serializable]
    public class EnlistmentState
    {
        private string _commanderId;
        private bool _isEnlisted;
        private CampaignTime _enlistTime;
        private int _enlistTier;
        private List<EquipmentElement> _storedEquipment;
        private List<int> _storedEquipmentSlots;
        private List<ItemObject> _storedItems;
        private Equipment _storedBattleEquipment;
        private Equipment _storedCivilianEquipment;
        private List<EquipmentElement> _storedRosterElements;
        private List<int> _storedRosterCounts;

        public EnlistmentState()
        {
            _isEnlisted = false;
            _enlistTier = 1;
            _storedEquipment = new List<EquipmentElement>();
            _storedEquipmentSlots = new List<int>();
            _storedItems = new List<ItemObject>();
            _storedBattleEquipment = null;
            _storedCivilianEquipment = null;
            _storedRosterElements = new List<EquipmentElement>();
            _storedRosterCounts = new List<int>();
        }

        public bool IsEnlisted => _isEnlisted;
        public Hero Commander => GetCommander();
        public CampaignTime EnlistTime => _enlistTime;
        public int EnlistTier => _enlistTier;

        private Hero GetCommander()
        {
            if (string.IsNullOrEmpty(_commanderId) || !_isEnlisted)
                return null;

            // Find the commander by their string ID
            foreach (Hero hero in Campaign.Current.AliveHeroes)
            {
                if (hero.StringId == _commanderId)
                    return hero;
            }

            // If commander not found, player is no longer enlisted
            _isEnlisted = false;
            return null;
        }

        public void Enlist(Hero commander)
        {
            if (commander == null)
                return;

            _commanderId = commander.StringId;
            _isEnlisted = true;
            _enlistTime = CampaignTime.Now;
            _enlistTier = 1;

            // Make the player's party escort the commander's party so we move with them
            try
            {
                if (MobileParty.MainParty != null && commander.PartyBelongedTo != null)
                {
                    MobileParty.MainParty.Ai.SetMoveEscortParty(commander.PartyBelongedTo);
                }
            }
            catch (Exception)
            {
                // Swallow to avoid hard crash; logging is handled at higher layers
            }
        }

        // Restore minimal state from save (used when SaveSystem field mapping is not present)
        public void RestoreFromSave(string commanderId, bool isEnlisted, CampaignTime enlistTime, int enlistTier)
        {
            _commanderId = commanderId;
            _isEnlisted = isEnlisted;
            _enlistTime = enlistTime;
            _enlistTier = enlistTier;
        }

        // Extract minimal state for saving
        public void ExtractForSave(out string commanderId, out bool isEnlisted, out CampaignTime enlistTime, out int enlistTier)
        {
            commanderId = _commanderId;
            isEnlisted = _isEnlisted;
            enlistTime = _enlistTime;
            enlistTier = _enlistTier;
        }

        public void LeaveArmy()
        {
            if (!_isEnlisted)
                return;

            // Restore player's original equipment
            try { RestorePlayerEquipment(); } catch { }

            // Clear enlistment state
            _isEnlisted = false;
            _commanderId = null;
            _storedEquipment?.Clear();
            _storedEquipmentSlots?.Clear();
            _storedItems?.Clear();
            _storedBattleEquipment = null;
            _storedCivilianEquipment = null;
            _storedRosterElements?.Clear();
            _storedRosterCounts?.Clear();

            // No explicit detach needed when escorting; player regains control

            // Show leave message
            try
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    EnlistmentDialogs.Messages.LeaveSuccess, 
                    Color.FromUint(0xFFFF8000)));
            }
            catch { }
        }

        public void StorePlayerEquipment()
        {
            if (Hero.MainHero == null)
                return;

            // Ensure lists exist
            _storedEquipment = _storedEquipment ?? new List<EquipmentElement>();
            _storedEquipmentSlots = _storedEquipmentSlots ?? new List<int>();
            _storedItems = _storedItems ?? new List<ItemObject>();
            _storedRosterElements = _storedRosterElements ?? new List<EquipmentElement>();
            _storedRosterCounts = _storedRosterCounts ?? new List<int>();

            _storedEquipment.Clear();
            _storedEquipmentSlots.Clear();
            _storedItems.Clear();
            _storedRosterElements.Clear();
            _storedRosterCounts.Clear();

            // Snapshot full battle equipment for safe restoration
            try { _storedBattleEquipment = Hero.MainHero.BattleEquipment.Clone(false); } catch { _storedBattleEquipment = null; }
            try { _storedCivilianEquipment = Hero.MainHero.CivilianEquipment.Clone(false); } catch { _storedCivilianEquipment = null; }

            // Snapshot inventory exactly (element + count)
            try
            {
                var roster = PartyBase.MainParty?.ItemRoster;
                if (roster != null)
                {
                    foreach (ItemRosterElement item in roster)
                    {
                        if (item.Amount > 0)
                        {
                            _storedRosterElements.Add(item.EquipmentElement);
                            _storedRosterCounts.Add(item.Amount);
                        }
                    }
                }
            }
            catch { }

            // Store slot-indexed elements as a fallback representation
            try
            {
                Equipment equipment = Hero.MainHero.BattleEquipment;
                for (int i = 0; i < 12; i++) // Equipment slots
                {
                    EquipmentElement element = equipment[i];
                    if (!element.IsEmpty)
                    {
                        _storedEquipment.Add(element);
                        _storedEquipmentSlots.Add(i);
                    }
                }
            }
            catch { }

            // Legacy list of items fallback (kept for compatibility)
            try
            {
                var partyRoster = Hero.MainHero?.PartyBelongedTo?.ItemRoster;
                if (partyRoster != null)
                {
                    foreach (ItemRosterElement item in partyRoster)
                    {
                        if (item.Amount > 0)
                        {
                            _storedItems.Add(item.EquipmentElement.Item);
                        }
                    }
                }
            }
            catch { }
        }

        public void ApplySoldierEquipment()
        {
            if (Hero.MainHero == null)
                return;

            // Clear current equipment
            Equipment equipment = Hero.MainHero.BattleEquipment;
            for (int i = 0; i < 12; i++)
            {
                equipment[i] = EquipmentElement.Invalid;
            }

            // Apply basic soldier equipment based on culture
            CultureObject culture = Hero.MainHero.Culture;
            if (culture != null)
            {
                // Apply basic soldier gear - this would need to be customized based on your mod's needs
                // For now, we'll just clear the equipment and let the player manage it
                // You can add specific equipment assignment logic here
            }
        }

        public void RestorePlayerEquipment()
        {
            if (Hero.MainHero == null)
                return;

            bool restoredViaSnapshot = false;
            try
            {
                if (Hero.MainHero != null && _storedBattleEquipment != null)
                {
                    Hero.MainHero.BattleEquipment.FillFrom(_storedBattleEquipment, true);
                    restoredViaSnapshot = true;
                }
            }
            catch { restoredViaSnapshot = false; }

            // Restore civilian snapshot independently if available
            try { if (Hero.MainHero != null && _storedCivilianEquipment != null) Hero.MainHero.CivilianEquipment.FillFrom(_storedCivilianEquipment, true); } catch { }

            if (!restoredViaSnapshot)
            {
                // Fallback: clear and restore to original slots
                try
                {
                    Equipment equipment = Hero.MainHero.BattleEquipment;
                    for (int i = 0; i < 12; i++)
                    {
                        equipment[i] = EquipmentElement.Invalid;
                    }

                    int restoreCount = Math.Min(_storedEquipment.Count, _storedEquipmentSlots.Count);
                    for (int k = 0; k < restoreCount; k++)
                    {
                        int slotIndex = _storedEquipmentSlots[k];
                        if (slotIndex >= 0 && slotIndex < 12)
                        {
                            var el = _storedEquipment[k];
                            try { equipment[slotIndex] = el; } catch { }
                        }
                    }
                }
                catch { }
            }

            // Restore inventory exactly (guard nulls)
            try
            {
                var roster = PartyBase.MainParty?.ItemRoster;
                if (roster != null)
                {
                    roster.Clear();
                    int count = Math.Min(_storedRosterElements?.Count ?? 0, _storedRosterCounts?.Count ?? 0);
                    for (int i = 0; i < count; i++)
                    {
                        var elem = _storedRosterElements[i];
                        var amt = _storedRosterCounts[i];
                        if (amt > 0)
                        {
                            roster.AddToCounts(elem, amt);
                        }
                    }

                    // Legacy fallback if snapshot empty
                    if (count == 0 && _storedItems != null)
                    {
                        foreach (ItemObject item in _storedItems)
                        {
                            roster.AddToCounts(item, 1);
                        }
                    }
                }
            }
            catch { }
        }

        // Accessors for saving/restoring stored data across save/load
        public (List<EquipmentElement> equipment, List<ItemObject> items, List<int> slots) GetStoredLoadout()
        {
            return (_storedEquipment, _storedItems, _storedEquipmentSlots);
        }

        public void RestoreStoredLoadout(List<EquipmentElement> equipment, List<ItemObject> items, List<int> slots)
        {
            _storedEquipment = equipment ?? new List<EquipmentElement>();
            _storedItems = items ?? new List<ItemObject>();
            _storedEquipmentSlots = slots ?? new List<int>();
        }

        public Equipment GetStoredEquipmentSnapshot()
        {
            return _storedBattleEquipment;
        }

        public void SetStoredEquipmentSnapshot(Equipment equipment)
        {
            _storedBattleEquipment = equipment;
        }

        public Equipment GetStoredCivilianEquipmentSnapshot()
        {
            return _storedCivilianEquipment;
        }

        public void SetStoredCivilianEquipmentSnapshot(Equipment equipment)
        {
            _storedCivilianEquipment = equipment;
        }

        public (List<EquipmentElement> elements, List<int> counts) GetStoredRosterSnapshot()
        {
            return (_storedRosterElements, _storedRosterCounts);
        }

        public void SetStoredRosterSnapshot(List<EquipmentElement> elements, List<int> counts)
        {
            _storedRosterElements = elements ?? new List<EquipmentElement>();
            _storedRosterCounts = counts ?? new List<int>();
        }

        public void Promote()
        {
            if (_isEnlisted && _enlistTier < 5) // Max tier 5
            {
                _enlistTier++;
                InformationManager.DisplayMessage(new InformationMessage(
                    EnlistmentDialogs.GetPromotionMessage(_enlistTier), 
                    Color.FromUint(0xFF00FF00)));
            }
        }

        public int GetDaysServed()
        {
            if (!_isEnlisted)
                return 0;

            return (int)(CampaignTime.Now - _enlistTime).ToDays;
        }

        public string GetServiceDescription()
        {
            if (!_isEnlisted)
                return "Not enlisted";

            int daysServed = GetDaysServed();
            return $"Serving for {daysServed} days as a tier {_enlistTier} soldier";
        }
    }
}
