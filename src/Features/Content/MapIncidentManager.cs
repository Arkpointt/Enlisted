using System;
using System.Linq;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Features.Escalation;
using Enlisted.Mod.Core.Logging;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace Enlisted.Features.Content
{
    /// <summary>
    /// Delivers map incidents during campaign travel based on context.
    /// Incidents fire on battle end, settlement entry/exit, and during siege operations.
    /// Uses existing event delivery system with context-specific filtering and pacing.
    /// </summary>
    public class MapIncidentManager : CampaignBehaviorBase
    {
        private const string LogCategory = "MapIncidents";

        // Cooldown durations for different incident types
        private const float BattleIncidentCooldownHours = 1f; // 1 battle per battle (effectively no time cooldown)
        private const float SettlementIncidentCooldownHours = 12f; // 12 hours between settlement incidents
        private const float SiegeIncidentCooldownHours = 4f; // 4 hours between siege incidents
        private const float WaitingIncidentCooldownHours = 8f; // 8 hours between waiting incidents
        private const float SiegeIncidentChancePerHour = 0.10f; // 10% chance per hour during siege
        private const float WaitingIncidentChancePerHour = 0.15f; // 15% chance per hour while waiting in settlement

        public static MapIncidentManager Instance { get; private set; }

        // Tracks last incident fire time by context to enforce cooldowns
        private CampaignTime _lastBattleIncidentTime = CampaignTime.Zero;
        private CampaignTime _lastSettlementIncidentTime = CampaignTime.Zero;
        private CampaignTime _lastSiegeIncidentTime = CampaignTime.Zero;
        private CampaignTime _lastWaitingIncidentTime = CampaignTime.Zero;

        public MapIncidentManager()
        {
            Instance = this;
        }

        public override void RegisterEvents()
        {
            CampaignEvents.OnPlayerBattleEndEvent.AddNonSerializedListener(this, OnBattleEnd);
            CampaignEvents.SettlementEntered.AddNonSerializedListener(this, OnSettlementEntered);
            CampaignEvents.OnSettlementLeftEvent.AddNonSerializedListener(this, OnSettlementLeft);
            CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, OnHourlyTick);
            
            ModLogger.Info(LogCategory, "Map incident manager registered for battle, settlement, and siege events");
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("_lastBattleIncidentTime", ref _lastBattleIncidentTime);
            dataStore.SyncData("_lastSettlementIncidentTime", ref _lastSettlementIncidentTime);
            dataStore.SyncData("_lastSiegeIncidentTime", ref _lastSiegeIncidentTime);
            dataStore.SyncData("_lastWaitingIncidentTime", ref _lastWaitingIncidentTime);
        }

        /// <summary>
        /// Fires after player participates in a battle.
        /// Triggers "leaving_battle" context incidents like looting, wounded comrades, first kill reflections.
        /// </summary>
        private void OnBattleEnd(MapEvent mapEvent)
        {
            try
            {
                if (!IsEnlistedAndValid())
                {
                    return;
                }

                // Only fire incidents for battles involving the player
                if (mapEvent == null || !mapEvent.IsPlayerMapEvent)
                {
                    return;
                }

                // Check cooldown (battle incidents: 1 per battle, but track time to prevent spam)
                if (!CheckCooldown(_lastBattleIncidentTime, BattleIncidentCooldownHours))
                {
                    ModLogger.Debug(LogCategory, "Battle incident on cooldown, skipping");
                    return;
                }

                ModLogger.Info(LogCategory, $"Battle ended, attempting to deliver leaving_battle incident");
                
                if (TryDeliverIncident("leaving_battle"))
                {
                    _lastBattleIncidentTime = CampaignTime.Now;
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, "Error handling battle end", ex);
            }
        }

        /// <summary>
        /// Fires when player enters a settlement.
        /// Triggers "entering_town" or "entering_village" context incidents.
        /// </summary>
        private void OnSettlementEntered(MobileParty party, Settlement settlement, Hero hero)
        {
            try
            {
                // Only trigger for main party
                if (party != MobileParty.MainParty)
                {
                    return;
                }

                if (!IsEnlistedAndValid())
                {
                    return;
                }

                if (settlement == null)
                {
                    return;
                }

                // Check cooldown
                if (!CheckCooldown(_lastSettlementIncidentTime, SettlementIncidentCooldownHours))
                {
                    ModLogger.Debug(LogCategory, "Settlement incident on cooldown, skipping");
                    return;
                }

                // Determine context based on settlement type
                var context = settlement.IsTown || settlement.IsCastle ? "entering_town" : "entering_village";

                ModLogger.Info(LogCategory, $"Entered {settlement.Name}, attempting to deliver {context} incident");

                if (TryDeliverIncident(context))
                {
                    _lastSettlementIncidentTime = CampaignTime.Now;
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, "Error handling settlement entry", ex);
            }
        }

        /// <summary>
        /// Fires when player leaves a settlement.
        /// Triggers "leaving_settlement" context incidents like hangovers, farewells, missing items.
        /// </summary>
        private void OnSettlementLeft(MobileParty party, Settlement settlement)
        {
            try
            {
                // Only trigger for main party
                if (party != MobileParty.MainParty)
                {
                    return;
                }

                if (!IsEnlistedAndValid())
                {
                    return;
                }

                if (settlement == null)
                {
                    return;
                }

                // Check cooldown (use same cooldown as entering)
                if (!CheckCooldown(_lastSettlementIncidentTime, SettlementIncidentCooldownHours))
                {
                    ModLogger.Debug(LogCategory, "Settlement incident on cooldown, skipping");
                    return;
                }

                ModLogger.Info(LogCategory, $"Left {settlement.Name}, attempting to deliver leaving_settlement incident");

                if (TryDeliverIncident("leaving_settlement"))
                {
                    _lastSettlementIncidentTime = CampaignTime.Now;
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, "Error handling settlement exit", ex);
            }
        }

        /// <summary>
        /// Fires every campaign hour.
        /// Checks for siege incidents when besieging, or waiting incidents when stationed in settlement.
        /// </summary>
        private void OnHourlyTick()
        {
            try
            {
                if (!IsEnlistedAndValid())
                {
                    return;
                }

                var lordParty = GetLordParty();
                if (lordParty == null)
                {
                    return;
                }

                // If besieging, check for siege incidents
                if (lordParty.BesiegerCamp != null)
                {
                    CheckSiegeIncident();
                    return;
                }

                // If stationed in a settlement, check for waiting incidents
                CheckWaitingInSettlement(lordParty);
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, "Error in hourly tick check", ex);
            }
        }

        /// <summary>
        /// Attempts to deliver a siege incident. Called when lord is besieging a settlement.
        /// </summary>
        private void CheckSiegeIncident()
        {
            // Check cooldown
            if (!CheckCooldown(_lastSiegeIncidentTime, SiegeIncidentCooldownHours))
            {
                return;
            }

            // 10% chance per hour to trigger siege incident
            if (MBRandom.RandomFloat >= SiegeIncidentChancePerHour)
            {
                return;
            }

            ModLogger.Info(LogCategory, "Siege detected, attempting to deliver during_siege incident");

            if (TryDeliverIncident("during_siege"))
            {
                _lastSiegeIncidentTime = CampaignTime.Now;
            }
        }

        /// <summary>
        /// Checks if player is waiting in a settlement and triggers waiting_in_settlement incidents.
        /// Fires when player's lord is stationed in a town or castle without active siege.
        /// </summary>
        private void CheckWaitingInSettlement(MobileParty lordParty)
        {
            // Check if lord is currently in a settlement (not just passing through)
            var currentSettlement = lordParty.CurrentSettlement;
            if (currentSettlement == null)
            {
                return;
            }

            // Only trigger for towns and castles (garrison situations)
            if (!currentSettlement.IsTown && !currentSettlement.IsCastle)
            {
                return;
            }

            // Check cooldown
            if (!CheckCooldown(_lastWaitingIncidentTime, WaitingIncidentCooldownHours))
            {
                return;
            }

            // 15% chance per hour to trigger waiting incident
            if (MBRandom.RandomFloat >= WaitingIncidentChancePerHour)
            {
                return;
            }

            ModLogger.Info(LogCategory, $"Waiting in {currentSettlement.Name}, attempting to deliver waiting_in_settlement incident");

            if (TryDeliverIncident("waiting_in_settlement"))
            {
                _lastWaitingIncidentTime = CampaignTime.Now;
            }
        }

        /// <summary>
        /// Attempts to deliver an incident for the specified context.
        /// Filters events by context, checks requirements, selects weighted, and queues for delivery.
        /// Returns true if an incident was successfully queued.
        /// </summary>
        private bool TryDeliverIncident(string context)
        {
            try
            {
                // Get all events for this context
                var candidates = EventCatalog.GetEventsForContext(context).ToList();

                if (candidates.Count == 0)
                {
                    ModLogger.Debug(LogCategory, $"No events found for context: {context}");
                    return false;
                }

                ModLogger.Debug(LogCategory, $"Found {candidates.Count} candidate events for context: {context}");

                // Filter by requirements and cooldowns
                var eligible = candidates.Where(e => IsEventEligible(e)).ToList();

                if (eligible.Count == 0)
                {
                    ModLogger.Debug(LogCategory, $"No eligible events after filtering for context: {context}");
                    return false;
                }

                // Use weighted selection to choose event
                var selected = SelectWeightedEvent(eligible, context);

                if (selected == null)
                {
                    ModLogger.Debug(LogCategory, $"Weighted selection returned null for context: {context}");
                    return false;
                }

                // Queue for delivery via EventDeliveryManager
                var deliveryManager = EventDeliveryManager.Instance;
                if (deliveryManager == null)
                {
                    ModLogger.Warn(LogCategory, "EventDeliveryManager not available, cannot deliver incident");
                    return false;
                }

                deliveryManager.QueueEvent(selected);

                // Record event fired for cooldown tracking
                var escalationState = EscalationManager.Instance?.State;
                if (escalationState != null)
                {
                    escalationState.RecordEventFired(selected.Id);
                    if (selected.Timing.OneTime)
                    {
                        escalationState.RecordOneTimeEventFired(selected.Id);
                    }
                }

                ModLogger.Info(LogCategory, $"Queued map incident: {selected.Id} (context: {context})");
                return true;
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, $"Error delivering incident for context: {context}", ex);
                return false;
            }
        }

        /// <summary>
        /// Checks if an event is eligible to fire based on requirements and cooldowns.
        /// </summary>
        private bool IsEventEligible(EventDefinition evt)
        {
            var escalationState = EscalationManager.Instance?.State;
            if (escalationState == null)
            {
                return false;
            }

            // Skip one-time events that have already fired
            if (evt.Timing.OneTime && escalationState.HasOneTimeEventFired(evt.Id))
            {
                return false;
            }

            // Skip events on cooldown
            if (escalationState.IsEventOnCooldown(evt.Id, evt.Timing.CooldownDays))
            {
                return false;
            }

            // Check all requirements
            if (!EventRequirementChecker.MeetsRequirements(evt.Requirements))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Selects an event from eligible candidates using weighted random selection.
        /// Context-matching events get a higher weight multiplier.
        /// </summary>
        private EventDefinition SelectWeightedEvent(System.Collections.Generic.List<EventDefinition> eligible, string context)
        {
            if (eligible.Count == 0)
            {
                return null;
            }

            if (eligible.Count == 1)
            {
                return eligible[0];
            }

            // Apply weights based on priority and context match
            var weighted = new System.Collections.Generic.List<WeightedEvent>();

            foreach (var evt in eligible)
            {
                var weight = 1.0f;

                // Context match bonus (already filtered to this context, so all should match)
                // But events with explicit context requirement get bonus over "Any"
                if (!string.IsNullOrEmpty(evt.Requirements.Context) &&
                    !evt.Requirements.Context.Equals("Any", StringComparison.OrdinalIgnoreCase))
                {
                    weight *= 1.5f;
                }

                // Priority bonus
                weight *= GetPriorityMultiplier(evt.Timing.Priority);

                weighted.Add(new WeightedEvent { Event = evt, Weight = weight });
            }

            // Weighted random selection
            var totalWeight = weighted.Sum(w => w.Weight);
            if (totalWeight <= 0)
            {
                return eligible[0];
            }

            var roll = MBRandom.RandomFloat * totalWeight;
            var cumulative = 0f;

            foreach (var w in weighted)
            {
                cumulative += w.Weight;
                if (roll <= cumulative)
                {
                    return w.Event;
                }
            }

            // Fallback (should not reach here normally)
            return weighted[weighted.Count - 1].Event;
        }

        /// <summary>
        /// Gets the weight multiplier for an event's priority level.
        /// </summary>
        private float GetPriorityMultiplier(string priority)
        {
            return priority?.ToLowerInvariant() switch
            {
                "high" => 1.5f,
                "critical" => 2.0f,
                "low" => 0.5f,
                _ => 1.0f // "normal" or unspecified
            };
        }

        /// <summary>
        /// Checks if enough time has passed since the last incident of this type.
        /// </summary>
        private bool CheckCooldown(CampaignTime lastFireTime, float cooldownHours)
        {
            if (lastFireTime == CampaignTime.Zero)
            {
                return true; // Never fired before
            }

            var hoursSince = (CampaignTime.Now - lastFireTime).ToHours;
            return hoursSince >= cooldownHours;
        }

        /// <summary>
        /// Checks if player is enlisted and in a valid state for incidents.
        /// </summary>
        private bool IsEnlistedAndValid()
        {
            var enlistment = EnlistmentBehavior.Instance;
            return enlistment?.IsEnlisted == true;
        }

        /// <summary>
        /// Gets the lord's party if the player is enlisted.
        /// </summary>
        private MobileParty GetLordParty()
        {
            var enlistment = EnlistmentBehavior.Instance;
            return enlistment?.CurrentLord?.PartyBelongedTo;
        }

        /// <summary>
        /// Internal class for tracking event weights during selection.
        /// </summary>
        private class WeightedEvent
        {
            public EventDefinition Event { get; set; }
            public float Weight { get; set; }
        }
    }
}

