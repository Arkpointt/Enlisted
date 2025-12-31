using System;
using System.Collections.Generic;
using System.Linq;
using Enlisted.Features.Company;
using Enlisted.Features.Context;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Features.Escalation;
using Enlisted.Features.Identity;
using Enlisted.Features.Orders.Models;
using Enlisted.Mod.Core.Logging;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace Enlisted.Features.Orders.Behaviors
{
    /// <summary>
    /// Manages the orders system, issuing orders from the chain of command, tracking acceptance/decline,
    /// and applying consequences. Orders are issued every ~3 days based on rank, role, and campaign context.
    /// </summary>
    public class OrderManager : CampaignBehaviorBase
    {
        private const string LogCategory = "Orders";

        public static OrderManager Instance { get; private set; }

        private Order _currentOrder;
        private bool _orderAccepted = false; // Tracks if current order is accepted (active) vs offered (waiting)
        private CampaignTime _lastOrderTime = CampaignTime.Never;
        private int _declineCount;

        public OrderManager()
        {
            Instance = this;
        }

        public override void RegisterEvents()
        {
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
            CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, OnHourlyTick);
        }

        public override void SyncData(IDataStore dataStore)
        {
            // Orders are transient (3-day expiry) so we don't persist _currentOrder.
            // On load, _currentOrder will be null and a new order will be issued
            // on the next eligible daily tick. This avoids save system issues with
            // complex Order objects that lack SaveableProperty attributes.
            dataStore.SyncData("_lastOrderTime", ref _lastOrderTime);
            dataStore.SyncData("_declineCount", ref _declineCount);

            // Clear any stale order reference on load to ensure clean state
            if (dataStore.IsLoading)
            {
                _currentOrder = null;
                _orderAccepted = false;
            }
        }

        /// <summary>
        /// Daily tick checks if it's time to issue a new order.
        /// </summary>
        private void OnDailyTick()
        {
            if (!EnlistmentBehavior.Instance.IsEnlisted)
            {
                return;
            }

            if (_currentOrder == null && ShouldIssueOrder())
            {
                TryIssueOrder();
            }
        }

        /// <summary>
        /// Hourly tick checks for order state transitions (IMMINENT → PENDING).
        /// </summary>
        private void OnHourlyTick()
        {
            if (!EnlistmentBehavior.Instance.IsEnlisted)
            {
                return;
            }

            UpdateOrderState();
        }

        /// <summary>
        /// Determines if enough time has passed since the last order to issue a new one.
        /// Base frequency is ~3 days, modified by campaign context and player rank.
        /// </summary>
        private bool ShouldIssueOrder()
        {
            if (_lastOrderTime == CampaignTime.Never)
            {
                return true;
            }

            var daysSinceLastOrder = CampaignTime.Now.ToDays - _lastOrderTime.ToDays;
            var tier = EnlistmentBehavior.Instance.EnlistmentTier;
            var context = GetCampaignContext();

            // Base: 3 days
            var targetDays = 3;

            // Context modifiers: high tempo operations = more frequent orders
            if (context == "Siege" || context == "Battle")
            {
                targetDays = 1;
            }
            else if (context == "War")
            {
                targetDays = 2;
            }
            else if (context == "Peace" || context == "Town")
            {
                targetDays = 4;
            }

            // Rank modifiers: lower ranks get more frequent orders
            if (tier <= 3)
            {
                targetDays = Math.Max(2, targetDays - 1);
            }
            else if (tier >= 7)
            {
                targetDays = Math.Min(5, targetDays + 1);
            }

            return daysSinceLastOrder >= targetDays;
        }

        /// <summary>
        /// Attempts to select and issue a new order from the catalog.
        /// Phase 10: Creates order in IMMINENT state with 4-8 hour advance warning.
        /// </summary>
        private void TryIssueOrder()
        {
            // Phase 4: Check with orchestrator before issuing
            if (!(Content.ContentOrchestrator.Instance?.CanIssueOrderNow() ?? true))
            {
                ModLogger.Debug(LogCategory, "Order issuance blocked by ContentOrchestrator timing");
                return;
            }

            var order = OrderCatalog.SelectOrder();
            if (order == null)
            {
                ModLogger.Debug(LogCategory, "No eligible orders found for current context");
                return;
            }

            // Determine issuer dynamically if set to "auto"
            if (order.Issuer == "auto")
            {
                var playerTier = EnlistmentBehavior.Instance.EnlistmentTier;
                order.Issuer = OrderCatalog.DetermineOrderIssuer(playerTier, order);
            }

            // Create order in IMMINENT state with 4-8 hour warning
            CreateImminentOrder(order);
        }

        /// <summary>
        /// Creates an order in IMMINENT state with 4-8 hour advance warning.
        /// The order will transition to PENDING when IssueTime arrives.
        /// </summary>
        private void CreateImminentOrder(Order order)
        {
            // Calculate issue time (4-8 hours from now)
            var hoursUntilIssue = MBRandom.RandomFloatRanged(4f, 8f);
            
            _currentOrder = order;
            _currentOrder.State = OrderState.Imminent;
            _currentOrder.ImminentTime = CampaignTime.Now;
            _currentOrder.IssueTime = CampaignTime.HoursFromNow(hoursUntilIssue);
            _currentOrder.ExpirationTime = CampaignTime.HoursFromNow(hoursUntilIssue + 72f); // 3 days after issue
            _lastOrderTime = CampaignTime.Now;
            _orderAccepted = false; // Not yet issued

            ModLogger.Info(LogCategory, 
                $"Order imminent: {order.Title} from {order.Issuer} (will issue in {hoursUntilIssue:F1} hours)");

            // Refresh the enlisted status menu so the [IMMINENT] marker appears immediately
            Interface.Behaviors.EnlistedMenuBehavior.Instance?.RefreshEnlistedStatusMenuUi();
        }

        /// <summary>
        /// Updates order state, checking for IMMINENT → PENDING transitions.
        /// Called every hour to check if forecast orders should be issued.
        /// </summary>
        private void UpdateOrderState()
        {
            if (_currentOrder == null)
            {
                return;
            }

            // Check for IMMINENT → PENDING transition
            if (_currentOrder.State == OrderState.Imminent && 
                CampaignTime.Now >= _currentOrder.IssueTime)
            {
                TransitionToPending(_currentOrder);
            }
        }

        /// <summary>
        /// Transitions an order from IMMINENT to PENDING state.
        /// Issues the order to the player for accept/decline (or auto-accepts if mandatory).
        /// </summary>
        private void TransitionToPending(Order order)
        {
            order.State = OrderState.Pending;
            order.IssuedTime = CampaignTime.Now;

            // Check if order is mandatory (auto-accept)
            if (order.Mandatory)
            {
                _orderAccepted = true; // Automatically accepted
                order.State = OrderState.Active;
                ModLogger.Info(LogCategory, $"Mandatory order assigned: {order.Title} from {order.Issuer}");

                // Show notification that duty has been assigned
                InformationManager.DisplayMessage(new InformationMessage(
                    $"Duty Assigned: {order.Title}",
                    Colors.Cyan));
            }
            else
            {
                _orderAccepted = false; // Order is offered, not yet accepted
                ModLogger.Info(LogCategory, $"Optional order issued: {order.Title} from {order.Issuer}");
                ShowOrderNotification(order);
            }

            // Refresh the enlisted status menu so the Orders accordion auto-expands for the new order
            Interface.Behaviors.EnlistedMenuBehavior.Instance?.RefreshEnlistedStatusMenuUi();
        }

        /// <summary>
        /// Player accepts the current order. Order becomes active and will progress through phases automatically.
        /// Order progression is handled by OrderProgressionBehavior.
        /// </summary>
        public void AcceptOrder()
        {
            if (_currentOrder == null || _currentOrder.State != OrderState.Pending)
            {
                return;
            }

            _orderAccepted = true;
            _currentOrder.State = OrderState.Active;
            ModLogger.Info(LogCategory, $"Order accepted: {_currentOrder.Title}");

            // Show combat log notification that order has started
            InformationManager.DisplayMessage(new InformationMessage(
                $"Order Started: {_currentOrder.Title}",
                Colors.Cyan));

            // Note: Order now progresses automatically via OrderProgressionBehavior.
            // It will fire events, update Recent Activity, and complete after phases run.
        }

        /// <summary>
        /// Player declines the current order. Applies reputation penalties and checks discharge threshold.
        /// </summary>
        public void DeclineOrder()
        {
            if (_currentOrder == null)
            {
                return;
            }

            _declineCount++;

            ApplyOrderOutcome(_currentOrder.Consequences.Decline);

            ModLogger.Info(LogCategory, $"Order declined: {_currentOrder.Title} ({_declineCount} total declines)");

            // Show combat log notification
            InformationManager.DisplayMessage(new InformationMessage(
                $"Order Declined: {_currentOrder.Title}",
                Colors.Red));

            // Report decline to news system
            if (Interface.Behaviors.EnlistedNewsBehavior.Instance != null)
            {
                var briefSummary = $"Declined: {_currentOrder.Title}";
                var detailedSummary = _currentOrder.Consequences.Decline.Text;

                Interface.Behaviors.EnlistedNewsBehavior.Instance.AddOrderOutcome(
                    orderTitle: _currentOrder.Title,
                    success: false,
                    briefSummary: briefSummary,
                    detailedSummary: detailedSummary ?? "Order declined",
                    issuer: _currentOrder.Issuer,
                    dayNumber: (int)CampaignTime.Now.ToDays
                );
            }

            // Decline results now shown in Recent Activities instead of popup to reduce UI interruption
            // ShowOrderResult(_currentOrder, false, _currentOrder.Consequences.Decline, isDecline: true);

            // Check for discharge after multiple declines
            if (_declineCount >= 5)
            {
                TriggerDischargeWarning();
            }

            _currentOrder = null;
            _orderAccepted = false;
        }

        /// <summary>
        /// Executes the order, determining success, failure, or critical failure based on skills and traits.
        /// Critical failure occurs when the roll is exceptionally bad (below 15% of success threshold).
        /// </summary>
        private void ExecuteOrder(Order order)
        {
            var result = EvaluateOrderResult(order);

            var outcome = result switch
            {
                OrderResult.Success => order.Consequences.Success,
                OrderResult.CriticalFailure => order.Consequences.CriticalFailure ?? order.Consequences.Failure,
                _ => order.Consequences.Failure
            };

            ApplyOrderOutcome(outcome);

            var resultName = result switch
            {
                OrderResult.Success => "succeeded",
                OrderResult.CriticalFailure => "critically failed",
                _ => "failed"
            };
            ModLogger.Info(LogCategory, $"Order {resultName}: {order.Title}");

            // Report to news system
            var success = result == OrderResult.Success;
            ReportOrderOutcome(order, success, outcome);

            // Order results now shown in Recent Activities instead of popup to reduce UI interruption
            // ShowOrderResult(order, success, outcome);
        }

        /// <summary>
        /// Result of an order execution roll.
        /// </summary>
        private enum OrderResult
        {
            Success,
            Failure,
            CriticalFailure
        }

        /// <summary>
        /// Reports the order outcome to the news system for display in daily brief and reports.
        /// </summary>
        private void ReportOrderOutcome(Order order, bool success, OrderOutcome outcome)
        {
            try
            {
                if (Interface.Behaviors.EnlistedNewsBehavior.Instance == null)
                {
                    return;
                }

                // Create brief summary for daily report
                var briefSummary = success
                    ? $"{order.Title} completed successfully"
                    : $"{order.Title} - mission failed";

                // Create detailed summary for full report
                var detailedSummary = outcome.Text ?? (success ? "Order completed." : "Order failed.");

                // Add reputation context if significant
                if (outcome.Reputation != null)
                {
                    var repEffects = new List<string>();
                    if (outcome.Reputation.ContainsKey("lord") && Math.Abs(outcome.Reputation["lord"]) >= 10)
                    {
                        repEffects.Add($"Lord reputation {outcome.Reputation["lord"]:+#;-#;0}");
                    }
                    if (outcome.Reputation.ContainsKey("officer") && Math.Abs(outcome.Reputation["officer"]) >= 10)
                    {
                        repEffects.Add($"Officer reputation {outcome.Reputation["officer"]:+#;-#;0}");
                    }
                    if (outcome.Reputation.ContainsKey("soldier") && Math.Abs(outcome.Reputation["soldier"]) >= 10)
                    {
                        repEffects.Add($"Soldier reputation {outcome.Reputation["soldier"]:+#;-#;0}");
                    }

                    if (repEffects.Count > 0)
                    {
                        detailedSummary += $"\n({string.Join(", ", repEffects)})";
                    }
                }

                Interface.Behaviors.EnlistedNewsBehavior.Instance.AddOrderOutcome(
                    orderTitle: order.Title,
                    success: success,
                    briefSummary: briefSummary,
                    detailedSummary: detailedSummary,
                    issuer: order.Issuer,
                    dayNumber: (int)CampaignTime.Now.ToDays
                );
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, "Failed to report order outcome", ex);
            }
        }

        /// <summary>
        /// Evaluates order outcome based on player skills and traits.
        /// Base 60% success chance, modified by skill/trait levels relative to requirements.
        /// Critical failure threshold is 15% of the failure zone (very bad roll when already failing).
        /// </summary>
        private OrderResult EvaluateOrderResult(Order order)
        {
            var successChance = 0.6f;

            var hero = Hero.MainHero;

            // Check skill requirements
            if (order.Requirements?.MinSkills != null)
            {
                foreach (var skillReq in order.Requirements.MinSkills)
                {
                    var skill = GetSkillByName(skillReq.Key);
                    if (skill == null)
                    {
                        continue;
                    }

                    var playerSkill = hero.GetSkillValue(skill);
                    var required = skillReq.Value;

                    // +1% per skill point above requirement, -2% per point below
                    if (playerSkill >= required)
                    {
                        successChance += (playerSkill - required) * 0.01f;
                    }
                    else
                    {
                        successChance -= (required - playerSkill) * 0.02f;
                    }
                }
            }

            // Check trait requirements
            if (order.Requirements?.MinTraits != null)
            {
                foreach (var traitReq in order.Requirements.MinTraits)
                {
                    var trait = GetTraitByName(traitReq.Key);
                    if (trait == null)
                    {
                        continue;
                    }

                    var playerTrait = hero.GetTraitLevel(trait);
                    var required = traitReq.Value;

                    // +2% per trait level above requirement, -3% per level below
                    if (playerTrait >= required)
                    {
                        successChance += (playerTrait - required) * 0.02f;
                    }
                    else
                    {
                        successChance -= (required - playerTrait) * 0.03f;
                    }
                }
            }

            // Clamp to 10-95% range
            successChance = MathF.Clamp(successChance, 0.1f, 0.95f);

            var roll = MBRandom.RandomFloat;
            if (roll < successChance)
            {
                return OrderResult.Success;
            }

            // Failed. Check if critical failure (bottom 15% of failure zone).
            // Critical failure threshold = 15% of (1 - successChance)
            var failureZone = 1f - successChance;
            var criticalThreshold = successChance + (failureZone * 0.85f);

            if (roll >= criticalThreshold)
            {
                return OrderResult.CriticalFailure;
            }

            return OrderResult.Failure;
        }

        /// <summary>
        /// Applies the consequences of an order outcome: skills, traits, reputation, company needs, etc.
        /// </summary>
        private void ApplyOrderOutcome(OrderOutcome outcome)
        {
            if (outcome == null)
            {
                return;
            }

            // Track applied effects for player feedback
            var feedbackMessages = new List<string>();

            // Apply skill XP
            if (outcome.SkillXp != null)
            {
                foreach (var xp in outcome.SkillXp)
                {
                    var skill = GetSkillByName(xp.Key);
                    if (skill != null)
                    {
                        Hero.MainHero.AddSkillXp(skill, xp.Value);
                        feedbackMessages.Add($"+{xp.Value} {skill.Name} XP");
                        ModLogger.Debug(LogCategory, $"Awarded {xp.Value} XP to {skill.Name}");
                    }
                }
            }

            // Apply trait XP
            if (outcome.TraitXp != null)
            {
                foreach (var xp in outcome.TraitXp)
                {
                    var trait = GetTraitByName(xp.Key);
                    if (trait != null)
                    {
                        TraitHelper.AwardTraitXp(Hero.MainHero, trait, xp.Value);
                        feedbackMessages.Add($"+{xp.Value} {trait.Name} trait XP");
                        ModLogger.Debug(LogCategory, $"Awarded {xp.Value} trait XP to {trait.Name}");
                    }
                }
            }

            // Apply reputation changes
            if (outcome.Reputation != null)
            {
                var escalation = EscalationManager.Instance;

                if (outcome.Reputation.TryGetValue("lord", out var lordRep))
                {
                    escalation.ModifyLordReputation(lordRep);
                    feedbackMessages.Add($"{(lordRep > 0 ? "+" : "")}{lordRep} Lord Reputation");
                }
                if (outcome.Reputation.TryGetValue("officer", out var officerRep))
                {
                    escalation.ModifyOfficerReputation(officerRep);
                    feedbackMessages.Add($"{(officerRep > 0 ? "+" : "")}{officerRep} Officer Reputation");
                }
                if (outcome.Reputation.TryGetValue("soldier", out var soldierRep))
                {
                    escalation.ModifySoldierReputation(soldierRep);
                    feedbackMessages.Add($"{(soldierRep > 0 ? "+" : "")}{soldierRep} Soldier Reputation");
                }
            }

            // Apply company need changes
            if (outcome.CompanyNeeds is { Count: > 0 })
            {
                if (EnlistmentBehavior.Instance is { IsEnlisted: true, CompanyNeeds: not null } enlistment)
                {
                    var state = enlistment.CompanyNeeds;

                    foreach (var needChange in outcome.CompanyNeeds)
                    {
                        // Parse the need name to enum
                        if (Enum.TryParse<CompanyNeed>(needChange.Key, true, out var need))
                        {
                            var oldValue = state.GetNeed(need);
                            var newValue = oldValue + needChange.Value;
                            state.SetNeed(need, newValue);

                            // Log the change with before/after values
                            var sign = needChange.Value >= 0 ? "+" : "";
                            ModLogger.Info(LogCategory,
                                $"Company need changed: {need}: {oldValue} -> {newValue} ({sign}{needChange.Value})");

                            // Report significant changes to news system
                            if (ShouldReportNeedChange(need, needChange.Value, oldValue, newValue))
                            {
                                var message = GetCompanyNeedChangeMessage(need, needChange.Value, newValue);
                                Interface.Behaviors.EnlistedNewsBehavior.Instance?.AddCompanyNeedChange(
                                    need: need.ToString(),
                                    delta: needChange.Value,
                                    oldValue: oldValue,
                                    newValue: newValue,
                                    message: message,
                                    dayNumber: (int)CampaignTime.Now.ToDays
                                );
                            }
                        }
                        else
                        {
                            ModLogger.Warn(LogCategory,
                                $"Unknown company need in order outcome: {needChange.Key}");
                        }
                    }

                    // Check for critical needs and warn player
                    var warnings = CompanyNeedsManager.CheckCriticalNeeds(state);
                    if (warnings.Count > 0)
                    {
                        foreach (var warning in warnings)
                        {
                            InformationManager.DisplayMessage(new InformationMessage(
                                $"Warning: {warning.Value}",
                                Color.FromUint(0xFFFF4444u))); // Red color for warnings
                        }
                    }
                }
            }

            // Apply escalation changes
            if (outcome.Escalation != null)
            {
                var escalation = EscalationManager.Instance;

                if (outcome.Escalation.TryGetValue("scrutiny", out var scrutinyEsc))
                {
                    escalation.ModifyScrutiny(scrutinyEsc);
                    feedbackMessages.Add($"{(scrutinyEsc > 0 ? "+" : "")}{scrutinyEsc} Scrutiny");
                }
                if (outcome.Escalation.TryGetValue("discipline", out var disciplineEsc))
                {
                    escalation.ModifyDiscipline(disciplineEsc);
                    feedbackMessages.Add($"{(disciplineEsc > 0 ? "+" : "")}{disciplineEsc} Discipline");
                }
            }

            // Apply medical risk (from spoiled food, disease exposure, etc.)
            if (outcome.MedicalRisk.HasValue && outcome.MedicalRisk.Value != 0)
            {
                EscalationManager.Instance.ModifyMedicalRisk(outcome.MedicalRisk.Value, "order outcome");
                feedbackMessages.Add($"{(outcome.MedicalRisk.Value > 0 ? "+" : "")}{outcome.MedicalRisk.Value} Medical Risk");
            }

            // Apply denars/renown
            if (outcome.Denars.HasValue && outcome.Denars.Value != 0)
            {
                Hero.MainHero.ChangeHeroGold(outcome.Denars.Value);
                feedbackMessages.Add($"{(outcome.Denars.Value > 0 ? "+" : "")}{outcome.Denars.Value} gold");
            }
            if (outcome.Renown.HasValue && outcome.Renown.Value != 0)
            {
                Hero.MainHero.Clan.AddRenown(outcome.Renown.Value);
                feedbackMessages.Add($"{(outcome.Renown.Value > 0 ? "+" : "")}{outcome.Renown.Value} Renown");
            }

            // Apply player HP loss (wounds from dangerous orders)
            if (outcome.HpLoss.HasValue && outcome.HpLoss.Value > 0)
            {
                var hero = Hero.MainHero;
                var oldHp = hero.HitPoints;
                var newHp = Math.Max(1, oldHp - outcome.HpLoss.Value);
                hero.HitPoints = newHp;

                feedbackMessages.Add($"-{outcome.HpLoss.Value} HP");

                ModLogger.Info(LogCategory, $"Player took {outcome.HpLoss.Value} HP damage from order outcome ({oldHp} -> {newHp})");

                if (newHp <= hero.CharacterObject.MaxHitPoints() * 0.25f)
                {
                    InformationManager.DisplayMessage(new InformationMessage(
                        "You are badly wounded!",
                        Color.FromUint(0xFFFF4444u)));
                }
            }

            // Apply troop casualties (losses from dangerous orders)
            if (outcome.TroopLossMin.HasValue || outcome.TroopLossMax.HasValue)
            {
                var minLoss = outcome.TroopLossMin ?? 0;
                var maxLoss = outcome.TroopLossMax ?? minLoss;

                if (maxLoss > 0)
                {
                    var actualLoss = minLoss == maxLoss ? minLoss : MBRandom.RandomInt(minLoss, maxLoss + 1);
                    ApplyTroopCasualties(actualLoss);
                    // Troop casualties already show their own message, don't duplicate
                }
            }

            // Display feedback to player if any effects were applied
            if (feedbackMessages.Count > 0)
            {
                var message = "Order: " + string.Join(", ", feedbackMessages);
                InformationManager.DisplayMessage(new InformationMessage(message, Colors.Green));
                ModLogger.Info(LogCategory, $"Order effects applied: {message}");
            }
        }

        /// <summary>
        /// Applies troop casualties to the enlisted lord's party as a result of order failure.
        /// Kills random non-hero troops from the roster.
        /// </summary>
        private void ApplyTroopCasualties(int count)
        {
            if (count <= 0)
            {
                return;
            }

            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment?.IsEnlisted != true || enlistment.EnlistedLord?.PartyBelongedTo == null)
            {
                ModLogger.Warn(LogCategory, "Cannot apply troop casualties - not enlisted or lord party unavailable");
                return;
            }

            var lordParty = enlistment.EnlistedLord.PartyBelongedTo;
            var roster = lordParty.MemberRoster;
            var killed = 0;

            // Kill random non-hero troops
            for (var i = 0; i < count && roster.TotalRegulars > 0; i++)
            {
                // Find a random non-hero troop to kill
                var regularCount = roster.TotalRegulars;
                if (regularCount <= 0)
                {
                    break;
                }

                var targetIndex = MBRandom.RandomInt(regularCount);
                var currentIndex = 0;

                for (var j = 0; j < roster.Count; j++)
                {
                    var element = roster.GetElementCopyAtIndex(j);
                    if (element.Character.IsHero)
                    {
                        continue;
                    }

                    var troopCount = element.Number - element.WoundedNumber;
                    if (troopCount <= 0)
                    {
                        continue;
                    }

                    if (currentIndex + troopCount > targetIndex)
                    {
                        roster.AddToCounts(element.Character, -1);
                        killed++;
                        break;
                    }

                    currentIndex += troopCount;
                }
            }

            if (killed > 0)
            {
                ModLogger.Info(LogCategory, $"Order outcome caused {killed} troop casualties in lord's party");
                InformationManager.DisplayMessage(new InformationMessage(
                    $"{killed} soldier{(killed > 1 ? "s" : "")} lost during the mission.",
                    Color.FromUint(0xFFFF4444u)));
            }
        }

        /// <summary>
        /// Shows a notification that a new order has been issued.
        /// </summary>
        private void ShowOrderNotification(Order order)
        {
            InformationManager.DisplayMessage(new InformationMessage(
                $"New Order Available: {order.Title}",
                Color.FromUint(4282569842u))); // Gold color
        }


        /// <summary>
        /// Warns the player after multiple order declines about potential discharge.
        /// </summary>
        private void TriggerDischargeWarning()
        {
            InformationManager.ShowInquiry(new InquiryData(
                titleText: "Warning: Insubordination",
                text: "Your repeated refusal of orders has not gone unnoticed. " +
                      "Continued insubordination may result in discharge from service.",
                isAffirmativeOptionShown: true,
                isNegativeOptionShown: false,
                affirmativeText: "Understood",
                negativeText: null,
                affirmativeAction: null,
                negativeAction: null
            ), true);
        }

        /// <summary>
        /// Gets the current campaign context for order selection.
        /// Now uses strategic context awareness from ArmyContextAnalyzer.
        /// </summary>
        private string GetCampaignContext()
        {
            var enlistment = EnlistmentBehavior.Instance;
            if (!enlistment.IsEnlisted)
            {
                return "Peace";
            }

            var lord = enlistment.CurrentLord;
            if (lord?.PartyBelongedTo == null)
            {
                return "Peace";
            }

            var party = lord.PartyBelongedTo;

            // Use strategic context detection for more nuanced context awareness
            try
            {
                var strategicContext = ArmyContextAnalyzer.GetLordStrategicContext(party);

                // Map strategic contexts to legacy context tags for backward compatibility
                // This allows existing hardcoded orders to still work while supporting new strategic tags
                return strategicContext switch
                {
                    "siege_operation" => "Siege",
                    "desperate_defense" => "Battle",
                    "coordinated_offensive" => "War",
                    "raid_operation" => "War",
                    "garrison_duty" => "Town",
                    "winter_camp" => "Peace",
                    "patrol_peacetime" => "Peace",
                    "recruitment_drive" => "Town",
                    _ => "Peace"
                };
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, "Error getting strategic context, falling back to simple detection", ex);
            }

            // Fallback to simple detection if strategic context fails
            if (party.BesiegerCamp != null || party.SiegeEvent != null)
            {
                return "Siege";
            }

            if (party.MapEvent != null)
            {
                return "Battle";
            }

            if (lord.MapFaction != null)
            {
                foreach (var kingdom in Kingdom.All)
                {
                    if (kingdom != lord.MapFaction && FactionManager.IsAtWarAgainstFaction(lord.MapFaction, kingdom))
                    {
                        return "War";
                    }
                }
            }

            if (party.CurrentSettlement != null)
            {
                return "Town";
            }

            return "Peace";
        }

        /// <summary>
        /// Resolves a skill object by name string.
        /// </summary>
        private SkillObject GetSkillByName(string skillName)
        {
            return Skills.All.FirstOrDefault(s => s.StringId.Equals(skillName, StringComparison.OrdinalIgnoreCase) ||
                                                  s.Name.ToString().Equals(skillName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Resolves a trait object by name string.
        /// </summary>
        private TraitObject GetTraitByName(string traitName)
        {
            // Check common traits by string ID match
            if (traitName.Equals("Surgery", StringComparison.OrdinalIgnoreCase))
            {
                return DefaultTraits.Surgery;
            }
            if (traitName.Equals("ScoutSkills", StringComparison.OrdinalIgnoreCase))
            {
                return DefaultTraits.ScoutSkills;
            }
            if (traitName.Equals("RogueSkills", StringComparison.OrdinalIgnoreCase))
            {
                return DefaultTraits.RogueSkills;
            }
            if (traitName.Equals("Siegecraft", StringComparison.OrdinalIgnoreCase))
            {
                return DefaultTraits.Siegecraft;
            }
            if (traitName.Equals("Commander", StringComparison.OrdinalIgnoreCase))
            {
                return DefaultTraits.Commander;
            }
            if (traitName.Equals("SergeantCommandSkills", StringComparison.OrdinalIgnoreCase))
            {
                return DefaultTraits.SergeantCommandSkills;
            }

            // Try personality traits
            foreach (var trait in DefaultTraits.Personality)
            {
                if (trait.StringId.Equals(traitName, StringComparison.OrdinalIgnoreCase))
                {
                    return trait;
                }
            }

            return null;
        }

        /// <summary>
        /// Gets the currently active order, if any.
        /// </summary>
        public Order GetCurrentOrder()
        {
            return _currentOrder;
        }

        /// <summary>
        /// Returns true if there is a current order that has been accepted by the player.
        /// Used by OrderProgressionBehavior to determine if order should progress through phases.
        /// </summary>
        public bool IsOrderActive()
        {
            return _currentOrder != null && _orderAccepted;
        }

        /// <summary>
        /// Completes the current active order and applies outcome consequences.
        /// Called by OrderProgressionBehavior when order phases are finished.
        /// </summary>
        public void CompleteOrder(bool success)
        {
            if (_currentOrder == null || !_orderAccepted)
            {
                return;
            }

            var outcome = success ? _currentOrder.Consequences.Success : _currentOrder.Consequences.Failure;
            
            ApplyOrderOutcome(outcome);

            ModLogger.Info(LogCategory, $"Order completed: {_currentOrder.Title} (Success: {success})");

            // Show combat log notification
            InformationManager.DisplayMessage(new InformationMessage(
                $"Order Completed: {_currentOrder.Title}",
                success ? Colors.Green : Colors.Yellow));

            // Report to news system
            ReportOrderOutcome(_currentOrder, success, outcome);

            // Clear the order
            _currentOrder = null;
            _orderAccepted = false;
        }

        /// <summary>
        /// Gets the count of declined orders.
        /// </summary>
        public int GetDeclineCount()
        {
            return _declineCount;
        }

        /// <summary>
        /// Gets forecast text for the current order if it's in IMMINENT state.
        /// Returns appropriate text based on order tags (strategic vs company-level).
        /// </summary>
        public string GetImminentWarningText()
        {
            if (_currentOrder == null || _currentOrder.State != OrderState.Imminent)
            {
                return string.Empty;
            }

            // Strategic orders (high-level, issued by lord/captain)
            if (_currentOrder.Tags.Contains("strategic"))
            {
                return "Expect strategic orders from command soon.";
            }

            // Company-level orders (issued by sergeant/officer)
            // Vary text based on order type
            if (_currentOrder.Tags.Contains("patrol"))
            {
                return "Sergeant looking for patrol volunteers.";
            }
            if (_currentOrder.Tags.Contains("guard"))
            {
                return "Sergeant organizing guard rotations.";
            }
            if (_currentOrder.Tags.Contains("scout"))
            {
                return "Officers discussing reconnaissance needs.";
            }
            if (_currentOrder.Tags.Contains("medical"))
            {
                return "Surgeons calling for aid with the wounded.";
            }

            // Generic company-level forecast
            return "Sergeant will call for you soon.";
        }

        /// <summary>
        /// Gets hours remaining until an imminent order is issued.
        /// Returns -1 if no imminent order.
        /// </summary>
        public float GetHoursUntilIssue()
        {
            if (_currentOrder == null || _currentOrder.State != OrderState.Imminent)
            {
                return -1f;
            }

            var hoursRemaining = (float)(_currentOrder.IssueTime - CampaignTime.Now).ToHours;
            return MathF.Max(0f, hoursRemaining);
        }

        /// <summary>
        /// Checks if the current order is imminent (forecast active).
        /// </summary>
        public bool IsOrderImminent()
        {
            return _currentOrder != null && _currentOrder.State == OrderState.Imminent;
        }

        /// <summary>
        /// Determines if a company need change should be reported to the news system.
        /// Reports changes that are significant (±10+) or cross critical thresholds.
        /// </summary>
        private static bool ShouldReportNeedChange(CompanyNeed need, int delta, int oldValue, int newValue)
        {
            _ = need;
            // Report if change is significant (±10+)
            if (Math.Abs(delta) >= 10)
            {
                return true;
            }

            // Report if crossing critical threshold (30%)
            if (oldValue >= 30 && newValue < 30)
            {
                return true;
            }
            if (oldValue < 30 && newValue >= 30)
            {
                return true;
            }

            // Report if crossing excellent threshold (80%)
            if (oldValue < 80 && newValue >= 80)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Generates a contextual message for a company need change based on severity and direction.
        /// </summary>
        private static string GetCompanyNeedChangeMessage(CompanyNeed need, int delta, int newValue)
        {
            _ = need;
            var needStr = need.ToString();
            // Critical thresholds
            if (newValue < 30)
            {
                switch (need)
                {
                    case CompanyNeed.Supplies:
                        return "Supplies critically low - Equipment access restricted";
                    case CompanyNeed.Readiness:
                        return "Unit readiness critical - Combat effectiveness severely reduced";
                    case CompanyNeed.Morale:
                        return "Morale breaking - Risk of desertion";
                    case CompanyNeed.Rest:
                        return "Men exhausted - Need rest urgently";
                    case CompanyNeed.Equipment:
                        return "Equipment in poor condition - Combat capability compromised";
                    default:
                        return $"{needStr} is critically low";
                }
            }

            // Positive changes
            if (delta >= 15)
            {
                switch (need)
                {
                    case CompanyNeed.Supplies:
                        return "Company well-supplied after resupply";
                    case CompanyNeed.Readiness:
                        return "Unit readiness greatly improved";
                    case CompanyNeed.Morale:
                        return "Morale lifted significantly";
                    case CompanyNeed.Rest:
                        return "Men well-rested and ready";
                    case CompanyNeed.Equipment:
                        return "Equipment in good condition";
                    default:
                        return $"{needStr} significantly improved";
                }
            }
            else if (delta >= 10)
            {
                switch (need)
                {
                    case CompanyNeed.Supplies:
                        return "Supplies replenished";
                    case CompanyNeed.Readiness:
                        return "Unit readiness improving";
                    case CompanyNeed.Morale:
                        return "Morale improving";
                    case CompanyNeed.Rest:
                        return "Men recovering from fatigue";
                    case CompanyNeed.Equipment:
                        return "Equipment condition improved";
                    default:
                        return $"{needStr} improved";
                }
            }

            // Negative changes
            if (delta <= -15)
            {
                switch (need)
                {
                    case CompanyNeed.Supplies:
                        return "Supplies depleted significantly";
                    case CompanyNeed.Readiness:
                        return "Unit readiness declining sharply";
                    case CompanyNeed.Morale:
                        return "Morale declining";
                    case CompanyNeed.Rest:
                        return "Men growing exhausted";
                    case CompanyNeed.Equipment:
                        return "Equipment wearing down";
                    default:
                        return $"{needStr} declined significantly";
                }
            }
            else if (delta <= -10)
            {
                switch (need)
                {
                    case CompanyNeed.Supplies:
                        return "Supplies running low";
                    case CompanyNeed.Readiness:
                        return "Unit readiness declining";
                    case CompanyNeed.Morale:
                        return "Morale slipping";
                    case CompanyNeed.Rest:
                        return "Men growing tired";
                    case CompanyNeed.Equipment:
                        return "Equipment condition declining";
                    default:
                        return $"{needStr} declined";
                }
            }

            return $"{needStr} changed by {delta:+#;-#;0}";
        }
    }
}

