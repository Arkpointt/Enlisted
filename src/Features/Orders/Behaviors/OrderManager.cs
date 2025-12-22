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
        private CampaignTime _lastOrderTime = CampaignTime.Never;
        private int _declineCount;

        public OrderManager()
        {
            Instance = this;
        }

        public override void RegisterEvents()
        {
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
        }

        public override void SyncData(IDataStore dataStore)
        {
            // Persist order state across save/load
            dataStore.SyncData("_currentOrder", ref _currentOrder);
            dataStore.SyncData("_lastOrderTime", ref _lastOrderTime);
            dataStore.SyncData("_declineCount", ref _declineCount);
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
        /// </summary>
        private void TryIssueOrder()
        {
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

            _currentOrder = order;
            _currentOrder.IssuedTime = CampaignTime.Now;
            _currentOrder.ExpirationTime = CampaignTime.DaysFromNow(3f);
            _lastOrderTime = CampaignTime.Now;

            ModLogger.Info(LogCategory, $"Issued order: {order.Title} from {order.Issuer}");

            ShowOrderNotification(order);
        }

        /// <summary>
        /// Player accepts the current order. Executes immediately with success/failure determination.
        /// </summary>
        public void AcceptOrder()
        {
            if (_currentOrder == null)
            {
                return;
            }

            ExecuteOrder(_currentOrder);
            _currentOrder = null;
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

            // Show decline result
            ShowOrderResult(_currentOrder, false, _currentOrder.Consequences.Decline, isDecline: true);

            // Check for discharge after multiple declines
            if (_declineCount >= 5)
            {
                TriggerDischargeWarning();
            }

            _currentOrder = null;
        }

        /// <summary>
        /// Executes the order, determining success or failure based on skills and traits.
        /// </summary>
        private void ExecuteOrder(Order order)
        {
            var success = EvaluateOrderSuccess(order);

            var outcome = success ? order.Consequences.Success : order.Consequences.Failure;
            ApplyOrderOutcome(outcome);

            ModLogger.Info(LogCategory, $"Order {(success ? "succeeded" : "failed")}: {order.Title}");

            // Report to news system
            ReportOrderOutcome(order, success, outcome);

            ShowOrderResult(order, success, outcome);
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
        /// Evaluates whether the order succeeds based on player skills and traits.
        /// Base 60% success chance, modified by skill/trait levels relative to requirements.
        /// </summary>
        private bool EvaluateOrderSuccess(Order order)
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

            return MBRandom.RandomFloat < successChance;
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

            // Apply skill XP
            if (outcome.SkillXp != null)
            {
                foreach (var xp in outcome.SkillXp)
                {
                    var skill = GetSkillByName(xp.Key);
                    if (skill != null)
                    {
                        Hero.MainHero.AddSkillXp(skill, xp.Value);
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
                }
                if (outcome.Reputation.TryGetValue("officer", out var officerRep))
                {
                    escalation.ModifyOfficerReputation(officerRep);
                }
                if (outcome.Reputation.TryGetValue("soldier", out var soldierRep))
                {
                    escalation.ModifySoldierReputation(soldierRep);
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
                }
                if (outcome.Escalation.TryGetValue("discipline", out var disciplineEsc))
                {
                    escalation.ModifyDiscipline(disciplineEsc);
                }
            }

            // Apply denars/renown
            if (outcome.Denars != 0)
            {
                Hero.MainHero.ChangeHeroGold(outcome.Denars);
            }
            if (outcome.Renown != 0)
            {
                Hero.MainHero.Clan.AddRenown(outcome.Renown);
            }
        }

        /// <summary>
        /// Shows a notification that a new order has been issued.
        /// </summary>
        private void ShowOrderNotification(Order order)
        {
            InformationManager.DisplayMessage(new InformationMessage(
                $"New Order from {order.Issuer}: {order.Title}",
                Color.FromUint(4282569842u))); // Gold color
        }

        /// <summary>
        /// Shows the result of an order execution.
        /// </summary>
        private void ShowOrderResult(Order order, bool success, OrderOutcome outcome, bool isDecline = false)
        {
            var title = isDecline ? "Order Declined" : (success ? "Order Succeeded" : "Order Failed");
            var text = outcome.Text;

            if (string.IsNullOrEmpty(text))
            {
                text = isDecline
                    ? $"You declined the order from {order.Issuer}. Your reputation has suffered."
                    : success
                        ? $"You successfully completed the order: {order.Title}"
                        : $"You failed to complete the order: {order.Title}";
            }

            InformationManager.ShowInquiry(new InquiryData(
                titleText: title,
                text: text,
                isAffirmativeOptionShown: true,
                isNegativeOptionShown: false,
                affirmativeText: "Continue",
                negativeText: null,
                affirmativeAction: null,
                negativeAction: null
            ), true);
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
        /// Gets the count of declined orders.
        /// </summary>
        public int GetDeclineCount()
        {
            return _declineCount;
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

