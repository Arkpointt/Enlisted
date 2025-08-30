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
        private List<ItemObject> _storedItems;

        public EnlistmentState()
        {
            _isEnlisted = false;
            _enlistTier = 1;
            _storedEquipment = new List<EquipmentElement>();
            _storedItems = new List<ItemObject>();
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
            RestorePlayerEquipment();

            // Clear enlistment state
            _isEnlisted = false;
            _commanderId = null;
            _storedEquipment.Clear();
            _storedItems.Clear();

            // No explicit detach needed when escorting; player regains control

            // Show leave message
            InformationManager.DisplayMessage(new InformationMessage(
                EnlistmentDialogs.Messages.LeaveSuccess, 
                Color.FromUint(0xFFFF8000)));
        }

        public void StorePlayerEquipment()
        {
            if (Hero.MainHero == null)
                return;

            _storedEquipment.Clear();
            _storedItems.Clear();

            // Store current equipment
            Equipment equipment = Hero.MainHero.BattleEquipment;
            for (int i = 0; i < 12; i++) // Equipment slots
            {
                EquipmentElement element = equipment[i];
                if (!element.IsEmpty)
                {
                    _storedEquipment.Add(element);
                }
            }

            // Store inventory items
            foreach (ItemRosterElement item in Hero.MainHero.PartyBelongedTo.ItemRoster)
            {
                if (item.Amount > 0)
                {
                    _storedItems.Add(item.EquipmentElement.Item);
                }
            }
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

            // Restore stored equipment
            Equipment equipment = Hero.MainHero.BattleEquipment;
            for (int i = 0; i < 12; i++)
            {
                equipment[i] = EquipmentElement.Invalid;
            }

            // Restore equipment to slots (simplified - you might want more sophisticated logic)
            for (int i = 0; i < Math.Min(_storedEquipment.Count, 12); i++)
            {
                equipment[i] = _storedEquipment[i];
            }

            // Restore inventory items
            PartyBase.MainParty.ItemRoster.Clear();
            foreach (ItemObject item in _storedItems)
            {
                PartyBase.MainParty.ItemRoster.AddToCounts(item, 1);
            }
        }

        // Accessors for saving/restoring stored equipment/items across save/load
        public (List<EquipmentElement> equipment, List<ItemObject> items) GetStoredLoadout()
        {
            return (_storedEquipment, _storedItems);
        }

        public void RestoreStoredLoadout(List<EquipmentElement> equipment, List<ItemObject> items)
        {
            _storedEquipment = equipment ?? new List<EquipmentElement>();
            _storedItems = items ?? new List<ItemObject>();
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
