using System;
using Enlisted.Features.Enlistment.Behaviors;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.ViewModelCollection;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace Enlisted.Features.Camp.UI.Bulletin
{
    /// <summary>
    /// ViewModel for the lance leader portrait panel (like governor in settlement management).
    /// </summary>
    public class LanceLeaderVM : ViewModel
    {
        private string _titleText;
        private string _partySizeText;
        private HeroVM _hero;

        public LanceLeaderVM()
        {
            RefreshValues();
        }

        public override void RefreshValues()
        {
            base.RefreshValues();

            try
            {
                var enlistment = EnlistmentBehavior.Instance;
                var playerHero = TaleWorlds.CampaignSystem.Hero.MainHero;

                // Simplified: Show the lord you're enlisted to (no lance system yet)
                if (enlistment != null && playerHero != null && enlistment.EnlistedLord != null)
                {
                    var lord = enlistment.EnlistedLord;
                    
                    TitleText = "Your Commanding Officer";
                    
                    // Party info
                    var party = playerHero.PartyBelongedTo;
                    if (party != null)
                    {
                        var current = party.MemberRoster.TotalManCount;
                        var limit = party.Party.PartySizeLimit;
                        PartySizeText = $"Party: {current}/{limit}";
                    }
                    else
                    {
                        PartySizeText = "Not in active party";
                    }

                    // Create portrait - just show the lord for now
                    Hero = new HeroVM(lord, true);
                }
                else
                {
                    TitleText = "Join a Lord's Service";
                    PartySizeText = "";
                    
                    // Show player's own portrait as fallback
                    if (playerHero != null)
                    {
                        Hero = new HeroVM(playerHero, true);
                    }
                }
            }
            catch (Exception ex)
            {
                // Fallback to safe defaults
                InformationManager.DisplayMessage(new InformationMessage($"Error loading lance leader: {ex.Message}"));
                TitleText = "Camp Leader";
                PartySizeText = "";
                
                // Try to at least show the player
                var playerHero = TaleWorlds.CampaignSystem.Hero.MainHero;
                if (playerHero != null)
                {
                    Hero = new HeroVM(playerHero, true);
                }
            }
        }


        /// <summary>
        /// Command: Open party screen to see roster.
        /// </summary>
        public void ExecuteShowLanceInfo()
        {
            // For now, just show a message - can expand later to open party screen
            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment?.EnlistedLord != null)
            {
                InformationManager.DisplayMessage(new InformationMessage($"Serving under {enlistment.EnlistedLord.Name}"));
            }
            else
            {
                InformationManager.DisplayMessage(new InformationMessage("Not currently enlisted"));
            }
        }

        [DataSourceProperty]
        public string TitleText
        {
            get => _titleText;
            set
            {
                if (value == _titleText) return;
                _titleText = value;
                OnPropertyChangedWithValue(value, nameof(TitleText));
            }
        }

        [DataSourceProperty]
        public string PartySizeText
        {
            get => _partySizeText;
            set
            {
                if (value == _partySizeText) return;
                _partySizeText = value;
                OnPropertyChangedWithValue(value, nameof(PartySizeText));
            }
        }

        [DataSourceProperty]
        public HeroVM Hero
        {
            get => _hero;
            set
            {
                if (value == _hero) return;
                _hero = value;
                OnPropertyChangedWithValue(value, nameof(Hero));
            }
        }
    }
}

