using System;
using System.Collections.Generic;
using System.Linq;
using Enlisted.Mod.Core.Logging;
using TaleWorlds.CampaignSystem;

namespace Enlisted.Features.Lances.Events
{
    /// <summary>
    /// Shared persisted state for Lance Life Events (automatic + player-initiated).
    ///
    /// Safe persistence:
    /// - strings, ints only (count + indexed keys).
    /// </summary>
    public sealed class LanceLifeEventsStateBehavior : CampaignBehaviorBase
    {
        private const string LogCategory = "LanceLifeEvents";

        public static LanceLifeEventsStateBehavior Instance { get; private set; }

        private readonly Dictionary<string, int> _eventLastFiredDay = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _oneTimeFired = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Phase 5a: optional tag state driven by event effects (safe strings).
        private readonly HashSet<string> _characterTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _loyaltyTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private string _formationTag = string.Empty;

        public LanceLifeEventsStateBehavior()
        {
            Instance = this;
        }

        public override void RegisterEvents()
        {
            // State only.
        }

        public override void SyncData(IDataStore dataStore)
        {
            // Map<string,int>
            var firedCount = dataStore.IsLoading ? 0 : _eventLastFiredDay.Count;
            dataStore.SyncData("ll_evt_state_firedCount", ref firedCount);
            if (!dataStore.IsLoading)
            {
                var keys = _eventLastFiredDay.Keys.ToList();
                keys.Sort(StringComparer.OrdinalIgnoreCase);
                firedCount = keys.Count;
                for (var i = 0; i < keys.Count; i++)
                {
                    var k = keys[i];
                    var v = _eventLastFiredDay.TryGetValue(k, out var day) ? day : -1;
                    dataStore.SyncData($"ll_evt_state_fired_{i}_id", ref k);
                    dataStore.SyncData($"ll_evt_state_fired_{i}_day", ref v);
                }
            }
            else
            {
                _eventLastFiredDay.Clear();
                for (var i = 0; i < firedCount; i++)
                {
                    var k = string.Empty;
                    var v = -1;
                    dataStore.SyncData($"ll_evt_state_fired_{i}_id", ref k);
                    dataStore.SyncData($"ll_evt_state_fired_{i}_day", ref v);
                    if (!string.IsNullOrWhiteSpace(k) && v >= 0)
                    {
                        _eventLastFiredDay[k] = v;
                    }
                }
            }

            // Set<string>
            var oneTimeCount = dataStore.IsLoading ? 0 : _oneTimeFired.Count;
            dataStore.SyncData("ll_evt_state_oneTimeCount", ref oneTimeCount);
            if (!dataStore.IsLoading)
            {
                var ids = _oneTimeFired.ToList();
                ids.Sort(StringComparer.OrdinalIgnoreCase);
                oneTimeCount = ids.Count;
                for (var i = 0; i < ids.Count; i++)
                {
                    var id = ids[i];
                    dataStore.SyncData($"ll_evt_state_oneTime_{i}_id", ref id);
                }
            }
            else
            {
                _oneTimeFired.Clear();
                for (var i = 0; i < oneTimeCount; i++)
                {
                    var id = string.Empty;
                    dataStore.SyncData($"ll_evt_state_oneTime_{i}_id", ref id);
                    if (!string.IsNullOrWhiteSpace(id))
                    {
                        _oneTimeFired.Add(id);
                    }
                }
            }

            // Character tags
            var charCount = dataStore.IsLoading ? 0 : _characterTags.Count;
            dataStore.SyncData("ll_evt_state_charTagCount", ref charCount);
            if (!dataStore.IsLoading)
            {
                var ids = _characterTags.ToList();
                ids.Sort(StringComparer.OrdinalIgnoreCase);
                charCount = ids.Count;
                for (var i = 0; i < ids.Count; i++)
                {
                    var id = ids[i];
                    dataStore.SyncData($"ll_evt_state_charTag_{i}", ref id);
                }
            }
            else
            {
                _characterTags.Clear();
                for (var i = 0; i < charCount; i++)
                {
                    var id = string.Empty;
                    dataStore.SyncData($"ll_evt_state_charTag_{i}", ref id);
                    if (!string.IsNullOrWhiteSpace(id))
                    {
                        _characterTags.Add(id);
                    }
                }
            }

            // Loyalty tags
            var loyCount = dataStore.IsLoading ? 0 : _loyaltyTags.Count;
            dataStore.SyncData("ll_evt_state_loyTagCount", ref loyCount);
            if (!dataStore.IsLoading)
            {
                var ids = _loyaltyTags.ToList();
                ids.Sort(StringComparer.OrdinalIgnoreCase);
                loyCount = ids.Count;
                for (var i = 0; i < ids.Count; i++)
                {
                    var id = ids[i];
                    dataStore.SyncData($"ll_evt_state_loyTag_{i}", ref id);
                }
            }
            else
            {
                _loyaltyTags.Clear();
                for (var i = 0; i < loyCount; i++)
                {
                    var id = string.Empty;
                    dataStore.SyncData($"ll_evt_state_loyTag_{i}", ref id);
                    if (!string.IsNullOrWhiteSpace(id))
                    {
                        _loyaltyTags.Add(id);
                    }
                }
            }

            // Formation tag
            dataStore.SyncData("ll_evt_state_formationTag", ref _formationTag);
        }

        public bool IsOneTimeFired(string eventId)
        {
            if (string.IsNullOrWhiteSpace(eventId))
            {
                return false;
            }

            return _oneTimeFired.Contains(eventId);
        }

        public bool TryGetCooldownDaysRemaining(string eventId, int cooldownDays, int currentDay, out int remainingDays)
        {
            remainingDays = 0;
            if (string.IsNullOrWhiteSpace(eventId) || cooldownDays <= 0 || currentDay < 0)
            {
                return false;
            }

            if (!_eventLastFiredDay.TryGetValue(eventId, out var lastDay) || lastDay < 0)
            {
                return false;
            }

            var nextDay = lastDay + cooldownDays;
            if (currentDay < nextDay)
            {
                remainingDays = nextDay - currentDay;
                return true;
            }

            return false;
        }

        internal void MarkFired(LanceLifeEventDefinition evt)
        {
            if (evt == null || string.IsNullOrWhiteSpace(evt.Id))
            {
                return;
            }

            var today = (int)Math.Floor(CampaignTime.Now.ToDays);
            _eventLastFiredDay[evt.Id] = today;

            if (evt.Timing?.OneTime == true)
            {
                _oneTimeFired.Add(evt.Id);
            }

            ModLogger.Debug(LogCategory, $"Marked event fired: {evt.Id} (day={today}, oneTime={evt.Timing?.OneTime == true})");
        }

        internal void ApplySchemaEffectTags(LanceLifeEventEscalationEffects effects, string reason)
        {
            if (effects == null)
            {
                return;
            }

            // Phase 7: Apply formation choice to EnlistedDutiesBehavior (not just state tag)
            if (!string.IsNullOrWhiteSpace(effects.Formation))
            {
                _formationTag = effects.Formation.Trim();
                ModLogger.Debug(LogCategory, $"Formation tag set: {_formationTag} (why={reason})");

                // Actually set the player's formation in the duties system
                var duties = Assignments.Behaviors.EnlistedDutiesBehavior.Instance;
                if (duties != null)
                {
                    duties.SetPlayerFormation(_formationTag);
                    ModLogger.Info(LogCategory, $"Player formation set to {_formationTag} via event effect (why={reason})");

                    // Show formation assignment message
                    var formationName = _formationTag.Substring(0, 1).ToUpper() + _formationTag.Substring(1);
                    var msg = new TaleWorlds.Localization.TextObject("{=evt_formation_assigned}You have been assigned to the {FORMATION} formation.");
                    msg.SetTextVariable("FORMATION", formationName);
                    TaleWorlds.Library.InformationManager.DisplayMessage(
                        new TaleWorlds.Library.InformationMessage(msg.ToString(), TaleWorlds.Library.Colors.Cyan));
                }
            }

            // Phase 7: Apply starter duty assignment
            if (!string.IsNullOrWhiteSpace(effects.StarterDuty))
            {
                var duties = Assignments.Behaviors.EnlistedDutiesBehavior.Instance;
                var enlistment = Enlistment.Behaviors.EnlistmentBehavior.Instance;
                if (duties != null && enlistment != null)
                {
                    enlistment.SetSelectedDuty(effects.StarterDuty.Trim());
                    ModLogger.Info(LogCategory, $"Starter duty assigned: {effects.StarterDuty} (why={reason})");

                    var msg = new TaleWorlds.Localization.TextObject("{=evt_duty_assigned}You have been assigned to {DUTY} duty.");
                    msg.SetTextVariable("DUTY", effects.StarterDuty.Trim());
                    TaleWorlds.Library.InformationManager.DisplayMessage(
                        new TaleWorlds.Library.InformationMessage(msg.ToString(), TaleWorlds.Library.Colors.Cyan));
                }
            }

            // Phase 7: Apply tier promotion
            if (effects.Promotes)
            {
                var enlistment = Enlistment.Behaviors.EnlistmentBehavior.Instance;
                if (enlistment != null)
                {
                    var newTier = enlistment.EnlistmentTier + 1;
                    enlistment.SetTier(newTier);
                    ModLogger.Info(LogCategory, $"Player promoted to tier {newTier} via event effect (why={reason})");

                    // Update Quartermaster's "new item" tracking
                    Equipment.Behaviors.QuartermasterManager.Instance?.UpdateNewlyUnlockedItems();

                    // Clear the pending promotion flag in PromotionBehavior
                    Ranks.Behaviors.PromotionBehavior.Instance?.ClearPendingPromotion();

                    // Show QM prompt
                    var qmMsg = new TaleWorlds.Localization.TextObject("{=evt_promotion_qm}Report to the Quartermaster for your new kit.");
                    TaleWorlds.Library.InformationManager.DisplayMessage(
                        new TaleWorlds.Library.InformationMessage(qmMsg.ToString(), TaleWorlds.Library.Colors.Cyan));
                }
            }

            if (!string.IsNullOrWhiteSpace(effects.CharacterTag))
            {
                _characterTags.Add(effects.CharacterTag.Trim());
                ModLogger.Debug(LogCategory, $"Character tag added: {effects.CharacterTag} (why={reason})");
            }

            if (!string.IsNullOrWhiteSpace(effects.LoyaltyTag))
            {
                _loyaltyTags.Add(effects.LoyaltyTag.Trim());
                ModLogger.Debug(LogCategory, $"Loyalty tag added: {effects.LoyaltyTag} (why={reason})");
            }
        }
    }
}


