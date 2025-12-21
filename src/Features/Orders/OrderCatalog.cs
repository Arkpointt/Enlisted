using System;
using System.Collections.Generic;
using System.Linq;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Features.Identity;
using Enlisted.Features.Orders.Models;
using Enlisted.Features.Ranks;
using Enlisted.Mod.Core.Logging;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.Core;

namespace Enlisted.Features.Orders
{
    /// <summary>
    /// Catalog of military orders available based on rank, role, and campaign context.
    /// Orders are hardcoded for Phase 4, can be externalized to JSON in future phases.
    /// </summary>
    public static class OrderCatalog
    {
        private const string LogCategory = "OrderCatalog";
        private static List<Order> _orders = [];

        /// <summary>
        /// Initializes the order catalog with all available orders.
        /// </summary>
        public static void Initialize()
        {
            _orders =
            [
                // T1-T3: Basic soldier orders
                CreateGuardDutyOrder(),
                CreatePatrolCampOrder(),
                CreateEquipmentCheckOrder(),
                CreateFirewoodCollectionOrder(),
                CreateSentryPostOrder(),
                CreateMusterInspectionOrder(),

                // T4-T6: Specialist orders
                CreateScoutRouteOrder(),
                CreateTreatWoundedOrder(),
                CreateRepairEquipmentOrder(),
                CreateForageSuppliesOrder(),
                CreateLeadPatrolOrder(),
                CreateInspectDefensesOrder(),
                CreateTrainRecruitsOrder(),

                // T7-T9: Leadership orders
                CreateLeadSquadOrder(),
                CreatePlanStrategyOrder(),
                CreateCoordinateSupplyLineOrder(),
                CreateInterrogatePrisonerOrder(),
                CreateInspectCompanyReadinessOrder(),
                CreateNegotiateTruceOrder()
            ];

            ModLogger.Info(LogCategory, $"Initialized {_orders.Count} orders");
        }

        /// <summary>
        /// Selects an appropriate order based on player rank, role, and campaign context.
        /// </summary>
        public static Order SelectOrder()
        {
            if (_orders.Count == 0)
            {
                Initialize();
            }

            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment == null || !enlistment.IsEnlisted)
            {
                return null;
            }

            var tier = enlistment.EnlistmentTier;
            var context = GetCampaignContext();
            var role = EnlistedStatusManager.Instance?.GetPrimaryRole() ?? "Soldier";

            // Filter by tier eligibility
            var eligibleOrders = _orders
                .Where(o => tier >= o.Requirements.TierMin && tier <= o.Requirements.TierMax)
                .ToList();

            if (eligibleOrders.Count == 0)
            {
                ModLogger.Debug(LogCategory, $"No orders for tier {tier}");
                return null;
            }

            // Filter by skill/trait requirements
            eligibleOrders = eligibleOrders
                .Where(o => MeetsSkillRequirements(o) && MeetsTraitRequirements(o))
                .ToList();

            if (eligibleOrders.Count == 0)
            {
                ModLogger.Debug(LogCategory, $"No orders matching skill/trait requirements for tier {tier}");
                return null;
            }

            // Prefer orders matching context + role
            var contextOrders = eligibleOrders
                .Where(o => o.Tags.Contains(context.ToLower()))
                .ToList();

            var roleOrders = eligibleOrders
                .Where(o => o.Tags.Contains(role.ToLower()))
                .ToList();

            var roleContextOrders = contextOrders
                .Where(o => o.Tags.Contains(role.ToLower()))
                .ToList();

            // Priority: Role + Context > Context > Role > Any Eligible
            if (roleContextOrders.Count > 0)
            {
                return roleContextOrders.GetRandomElement();
            }

            if (contextOrders.Count > 0)
            {
                return contextOrders.GetRandomElement();
            }

            if (roleOrders.Count > 0)
            {
                return roleOrders.GetRandomElement();
            }

            return eligibleOrders.GetRandomElement();
        }

        /// <summary>
        /// Determines who issues the order based on player rank and order type.
        /// Typically issuer is 2-3 tiers above player.
        /// </summary>
        public static string DetermineOrderIssuer(int playerTier, Order order)
        {
            var enlistment = EnlistmentBehavior.Instance;
            var lord = enlistment?.CurrentLord;
            var culture = lord?.Culture?.StringId ?? "empire";

            int issuerTier;
            if (playerTier <= 2)
            {
                issuerTier = 4; // NCO
            }
            else if (playerTier <= 4)
            {
                issuerTier = 6; // Officer
            }
            else if (playerTier <= 6)
            {
                issuerTier = 8; // Commander
            }
            else
            {
                issuerTier = 9; // Lord-level
            }

            // Strategic orders from high ranks come directly from lord
            if (playerTier >= 7 && lord != null && order.Tags.Contains("strategic"))
            {
                return lord.Name?.ToString() ?? "Your Lord";
            }

            // Get culture-specific rank title for issuer
            var issuerRankTitle = RankHelper.GetRankTitle(issuerTier, culture);
            return issuerRankTitle;
        }

        /// <summary>
        /// Gets the current campaign context for order selection.
        /// </summary>
        private static string GetCampaignContext()
        {
            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment == null || !enlistment.IsEnlisted)
            {
                return "Peace";
            }

            var lord = enlistment.CurrentLord;
            if (lord?.PartyBelongedTo == null)
            {
                return "Peace";
            }

            var party = lord.PartyBelongedTo;

            if (party.BesiegerCamp != null || party.SiegeEvent != null)
            {
                return "Siege";
            }

            if (party.MapEvent != null)
            {
                return "Battle";
            }

            // Check for war state - check if at war with any faction
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
        /// Checks if the player meets the skill requirements for an order.
        /// </summary>
        private static bool MeetsSkillRequirements(Order order)
        {
            if (order.Requirements?.MinSkills == null || order.Requirements.MinSkills.Count == 0)
            {
                return true;
            }

            var hero = Hero.MainHero;
            foreach (var skillReq in order.Requirements.MinSkills)
            {
                var skill = GetSkillByName(skillReq.Key);
                if (skill == null)
                {
                    continue;
                }

                if (hero.GetSkillValue(skill) < skillReq.Value)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Checks if the player meets the trait requirements for an order.
        /// </summary>
        private static bool MeetsTraitRequirements(Order order)
        {
            if (order.Requirements?.MinTraits == null || order.Requirements.MinTraits.Count == 0)
            {
                return true;
            }

            var hero = Hero.MainHero;
            foreach (var traitReq in order.Requirements.MinTraits)
            {
                var trait = GetTraitByName(traitReq.Key);
                if (trait == null)
                {
                    continue;
                }

                if (hero.GetTraitLevel(trait) < traitReq.Value)
                {
                    return false;
                }
            }

            return true;
        }

        private static SkillObject GetSkillByName(string skillName)
        {
            return Skills.All.FirstOrDefault(s =>
                s.StringId.Equals(skillName, StringComparison.OrdinalIgnoreCase) ||
                s.Name.ToString().Equals(skillName, StringComparison.OrdinalIgnoreCase));
        }

        private static TraitObject GetTraitByName(string traitName)
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

        #region Order Definitions - T1-T3 (Basic Soldiers)

        private static Order CreateGuardDutyOrder()
        {
            return new Order
            {
                Id = "t1_guard_duty",
                Title = "Guard Duty",
                Description = "Stand watch at the camp perimeter. Report any suspicious activity.",
                Issuer = "auto",
                Requirements = new OrderRequirement { TierMin = 1, TierMax = 3 },
                Consequences = new OrderConsequence
                {
                    Success = new OrderOutcome
                    {
                        SkillXp = new Dictionary<string, int> { { "Tactics", 20 } },
                        Reputation = new Dictionary<string, int> { { "officer", 3 } },
                        CompanyNeeds = new Dictionary<string, int> { { "Readiness", 2 } },
                        Text = "Your vigilance is noted. Nothing gets past your watch."
                    },
                    Failure = new OrderOutcome
                    {
                        Reputation = new Dictionary<string, int> { { "officer", -5 } },
                        CompanyNeeds = new Dictionary<string, int> { { "Readiness", -3 } },
                        Text = "You dozed off. The officer catches you sleeping on watch."
                    },
                    Decline = new OrderOutcome
                    {
                        Reputation = new Dictionary<string, int> { { "officer", -10 } },
                        Text = "Refusing guard duty? The sergeant is not pleased."
                    }
                },
                Tags = ["soldier", "camp", "peace"]
            };
        }

        private static Order CreatePatrolCampOrder()
        {
            return new Order
            {
                Id = "t2_patrol_camp",
                Title = "Camp Patrol",
                Description = "Patrol the camp perimeter and check security.",
                Issuer = "auto",
                Requirements = new OrderRequirement { TierMin = 1, TierMax = 3 },
                Consequences = new OrderConsequence
                {
                    Success = new OrderOutcome
                    {
                        SkillXp = new Dictionary<string, int> { { "Tactics", 25 }, { "Scouting", 15 } },
                        Reputation = new Dictionary<string, int> { { "officer", 5 }, { "soldier", 2 } },
                        CompanyNeeds = new Dictionary<string, int> { { "Readiness", 3 }, { "Morale", 2 } },
                        Text = "Your patrol is thorough and professional. The men feel safer."
                    },
                    Failure = new OrderOutcome
                    {
                        Reputation = new Dictionary<string, int> { { "officer", -8 } },
                        CompanyNeeds = new Dictionary<string, int> { { "Readiness", -5 }, { "Morale", -3 } },
                        Text = "You missed a breach in the perimeter. The sergeant reprimands you sharply."
                    },
                    Decline = new OrderOutcome
                    {
                        Reputation = new Dictionary<string, int> { { "officer", -12 }, { "soldier", -3 } },
                        Text = "The men see you shirking duties. Your reputation suffers."
                    }
                },
                Tags = ["soldier", "camp", "peace", "war"]
            };
        }

        private static Order CreateEquipmentCheckOrder()
        {
            return new Order
            {
                Id = "t2_equipment_check",
                Title = "Equipment Inspection",
                Description = "Inspect and maintain your squad's equipment.",
                Issuer = "auto",
                Requirements = new OrderRequirement { TierMin = 2, TierMax = 4 },
                Consequences = new OrderConsequence
                {
                    Success = new OrderOutcome
                    {
                        SkillXp = new Dictionary<string, int> { { "Crafting", 30 } },
                        Reputation = new Dictionary<string, int> { { "officer", 5 }, { "soldier", 5 } },
                        CompanyNeeds = new Dictionary<string, int> { { "Equipment", 5 }, { "Readiness", 2 } },
                        Text = "Equipment is spotless and battle-ready. The men appreciate your diligence."
                    },
                    Failure = new OrderOutcome
                    {
                        Reputation = new Dictionary<string, int> { { "officer", -10 } },
                        CompanyNeeds = new Dictionary<string, int> { { "Equipment", -8 }, { "Readiness", -3 } },
                        Text = "You missed several damaged items. The officer is disappointed."
                    },
                    Decline = new OrderOutcome
                    {
                        Reputation = new Dictionary<string, int> { { "officer", -15 } },
                        CompanyNeeds = new Dictionary<string, int> { { "Equipment", -5 } },
                        Text = "Equipment checks are mandatory. Your refusal is noted."
                    }
                },
                Tags = ["soldier", "camp", "peace", "town"]
            };
        }

        private static Order CreateFirewoodCollectionOrder()
        {
            return new Order
            {
                Id = "t1_firewood_collection",
                Title = "Gather Firewood",
                Description = "Collect firewood for the camp's evening fires.",
                Issuer = "auto",
                Requirements = new OrderRequirement { TierMin = 1, TierMax = 2 },
                Consequences = new OrderConsequence
                {
                    Success = new OrderOutcome
                    {
                        SkillXp = new Dictionary<string, int> { { "Athletics", 15 } },
                        Reputation = new Dictionary<string, int> { { "soldier", 3 } },
                        CompanyNeeds = new Dictionary<string, int> { { "Morale", 3 }, { "Rest", 2 } },
                        Text = "The fires burn bright tonight. The men are warm and grateful."
                    },
                    Failure = new OrderOutcome
                    {
                        CompanyNeeds = new Dictionary<string, int> { { "Morale", -5 }, { "Rest", -3 } },
                        Text = "Not enough firewood. Cold, miserable night for everyone."
                    },
                    Decline = new OrderOutcome
                    {
                        Reputation = new Dictionary<string, int> { { "soldier", -8 } },
                        Text = "The men freeze while you shirk basic duties."
                    }
                },
                Tags = ["soldier", "camp", "peace"]
            };
        }

        private static Order CreateSentryPostOrder()
        {
            return new Order
            {
                Id = "t3_sentry_post",
                Title = "Man the Sentry Post",
                Description = "Take position at the forward sentry post through the night.",
                Issuer = "auto",
                Requirements = new OrderRequirement { TierMin = 2, TierMax = 4 },
                Consequences = new OrderConsequence
                {
                    Success = new OrderOutcome
                    {
                        SkillXp = new Dictionary<string, int> { { "Scouting", 35 }, { "Tactics", 20 } },
                        Reputation = new Dictionary<string, int> { { "officer", 8 } },
                        CompanyNeeds = new Dictionary<string, int> { { "Readiness", 5 }, { "Rest", -2 } },
                        Text = "Your sharp eyes spot movement in the darkness. Alert raised in time."
                    },
                    Failure = new OrderOutcome
                    {
                        Reputation = new Dictionary<string, int> { { "officer", -12 } },
                        CompanyNeeds = new Dictionary<string, int> { { "Readiness", -8 }, { "Morale", -5 } },
                        Text = "You failed to spot the enemy scouts. The camp was nearly compromised."
                    },
                    Decline = new OrderOutcome
                    {
                        Reputation = new Dictionary<string, int> { { "officer", -18 } },
                        Escalation = new Dictionary<string, int> { { "discipline", 1 } },
                        Text = "Refusing sentry duty is a serious offense."
                    }
                },
                Tags = ["soldier", "war", "scout"]
            };
        }

        private static Order CreateMusterInspectionOrder()
        {
            return new Order
            {
                Id = "t3_muster_inspection",
                Title = "Muster Inspection",
                Description = "Present yourself for formal inspection by the officers.",
                Issuer = "auto",
                Requirements = new OrderRequirement { TierMin = 1, TierMax = 4 },
                Consequences = new OrderConsequence
                {
                    Success = new OrderOutcome
                    {
                        Reputation = new Dictionary<string, int> { { "officer", 6 }, { "lord", 2 } },
                        CompanyNeeds = new Dictionary<string, int> { { "Readiness", 3 }, { "Morale", 2 } },
                        Text = "Your bearing and equipment impress the inspecting officer."
                    },
                    Failure = new OrderOutcome
                    {
                        Reputation = new Dictionary<string, int> { { "officer", -10 } },
                        CompanyNeeds = new Dictionary<string, int> { { "Readiness", -4 } },
                        Text = "Sloppy appearance. The officer makes an example of you."
                    },
                    Decline = new OrderOutcome
                    {
                        Reputation = new Dictionary<string, int> { { "officer", -20 }, { "lord", -5 } },
                        Escalation = new Dictionary<string, int> { { "discipline", 2 } },
                        Text = "Missing inspection is unacceptable. You're confined to camp."
                    }
                },
                Tags = ["soldier", "camp", "town"]
            };
        }

        #endregion

        #region Order Definitions - T4-T6 (Specialists)

        private static Order CreateScoutRouteOrder()
        {
            return new Order
            {
                Id = "t4_scout_route",
                Title = "Reconnaissance Patrol",
                Description = "Scout the route ahead and report enemy positions.",
                Issuer = "auto",
                Requirements = new OrderRequirement
                {
                    TierMin = 4,
                    TierMax = 6,
                    MinSkills = new Dictionary<string, int> { { "Scouting", 40 } }
                },
                Consequences = new OrderConsequence
                {
                    Success = new OrderOutcome
                    {
                        SkillXp = new Dictionary<string, int> { { "Scouting", 80 }, { "Tactics", 40 } },
                        TraitXp = new Dictionary<string, int> { { "ScoutSkills", 100 } },
                        Reputation = new Dictionary<string, int> { { "lord", 5 }, { "officer", 15 } },
                        CompanyNeeds = new Dictionary<string, int> { { "Readiness", 8 }, { "Morale", 3 } },
                        Denars = 50,
                        Text = "Excellent intel. Your report helps the commander plan the march."
                    },
                    Failure = new OrderOutcome
                    {
                        Reputation = new Dictionary<string, int> { { "officer", -15 } },
                        CompanyNeeds = new Dictionary<string, int> { { "Readiness", -10 }, { "Morale", -5 } },
                        Text = "You were spotted and had to retreat. No intelligence gathered."
                    },
                    Decline = new OrderOutcome
                    {
                        Reputation = new Dictionary<string, int> { { "officer", -20 } },
                        Escalation = new Dictionary<string, int> { { "discipline", 2 } },
                        Text = "The captain's jaw tightens. 'Find someone else who can handle it.'"
                    }
                },
                Tags = ["scout", "war", "outdoor"]
            };
        }

        private static Order CreateTreatWoundedOrder()
        {
            return new Order
            {
                Id = "t5_treat_wounded",
                Title = "Treat the Wounded",
                Description = "Assist the surgeon with treating battle casualties.",
                Issuer = "auto",
                Requirements = new OrderRequirement
                {
                    TierMin = 4,
                    TierMax = 6,
                    MinSkills = new Dictionary<string, int> { { "Medicine", 40 } }
                },
                Consequences = new OrderConsequence
                {
                    Success = new OrderOutcome
                    {
                        SkillXp = new Dictionary<string, int> { { "Medicine", 100 } },
                        TraitXp = new Dictionary<string, int> { { "Surgery", 120 } },
                        Reputation = new Dictionary<string, int> { { "officer", 10 }, { "soldier", 15 } },
                        CompanyNeeds = new Dictionary<string, int> { { "Morale", 10 }, { "Readiness", 5 } },
                        Text = "Your skill saves several lives. The men are deeply grateful."
                    },
                    Failure = new OrderOutcome
                    {
                        Reputation = new Dictionary<string, int> { { "soldier", -15 } },
                        CompanyNeeds = new Dictionary<string, int> { { "Morale", -12 }, { "Readiness", -8 } },
                        Text = "Several men die under your care. You're haunted by their screams."
                    },
                    Decline = new OrderOutcome
                    {
                        Reputation = new Dictionary<string, int> { { "soldier", -25 } },
                        CompanyNeeds = new Dictionary<string, int> { { "Morale", -10 } },
                        Text = "Men die while you refuse to help. Your comrades won't forget this."
                    }
                },
                Tags = ["medic", "camp", "battle", "siege"]
            };
        }

        private static Order CreateRepairEquipmentOrder()
        {
            return new Order
            {
                Id = "t4_repair_equipment",
                Title = "Equipment Repair",
                Description = "Repair damaged weapons and armor for the company.",
                Issuer = "auto",
                Requirements = new OrderRequirement
                {
                    TierMin = 4,
                    TierMax = 6,
                    MinSkills = new Dictionary<string, int> { { "Crafting", 50 } }
                },
                Consequences = new OrderConsequence
                {
                    Success = new OrderOutcome
                    {
                        SkillXp = new Dictionary<string, int> { { "Crafting", 90 } },
                        TraitXp = new Dictionary<string, int> { { "Siegecraft", 80 } },
                        Reputation = new Dictionary<string, int> { { "officer", 12 }, { "soldier", 8 } },
                        CompanyNeeds = new Dictionary<string, int> { { "Equipment", 15 }, { "Readiness", 5 } },
                        Text = "Weapons shine, armor holds. The company is battle-ready again."
                    },
                    Failure = new OrderOutcome
                    {
                        CompanyNeeds = new Dictionary<string, int> { { "Equipment", -10 }, { "Readiness", -5 } },
                        Denars = -30,
                        Text = "You damage several pieces beyond repair. Cost deducted from pay."
                    },
                    Decline = new OrderOutcome
                    {
                        Reputation = new Dictionary<string, int> { { "officer", -18 } },
                        CompanyNeeds = new Dictionary<string, int> { { "Equipment", -8 } },
                        Text = "Equipment remains damaged. The company's readiness suffers."
                    }
                },
                Tags = ["engineer", "camp", "peace"]
            };
        }

        private static Order CreateForageSuppliesOrder()
        {
            return new Order
            {
                Id = "t4_forage_supplies",
                Title = "Forage for Supplies",
                Description = "Lead a foraging party to gather food and materials.",
                Issuer = "auto",
                Requirements = new OrderRequirement
                {
                    TierMin = 4,
                    TierMax = 6,
                    MinSkills = new Dictionary<string, int> { { "Scouting", 30 } }
                },
                Consequences = new OrderConsequence
                {
                    Success = new OrderOutcome
                    {
                        SkillXp = new Dictionary<string, int> { { "Scouting", 60 }, { "Tactics", 30 } },
                        Reputation = new Dictionary<string, int> { { "officer", 10 } },
                        CompanyNeeds = new Dictionary<string, int> { { "Supplies", 12 }, { "Morale", 5 } },
                        Text = "You return with wagons full. The company eats well tonight."
                    },
                    Failure = new OrderOutcome
                    {
                        Reputation = new Dictionary<string, int> { { "officer", -12 } },
                        CompanyNeeds = new Dictionary<string, int> { { "Supplies", -5 }, { "Morale", -8 } },
                        Text = "Ambushed by bandits. Lost men and supplies."
                    },
                    Decline = new OrderOutcome
                    {
                        Reputation = new Dictionary<string, int> { { "officer", -15 } },
                        CompanyNeeds = new Dictionary<string, int> { { "Supplies", -8 } },
                        Text = "The company goes hungry while you refuse duty."
                    }
                },
                Tags = ["soldier", "outdoor", "war", "peace"]
            };
        }

        private static Order CreateLeadPatrolOrder()
        {
            return new Order
            {
                Id = "t5_lead_patrol",
                Title = "Lead Patrol",
                Description = "Lead a combat patrol to clear the area of threats.",
                Issuer = "auto",
                Requirements = new OrderRequirement
                {
                    TierMin = 5,
                    TierMax = 6,
                    MinSkills = new Dictionary<string, int> { { "Tactics", 50 }, { "Leadership", 30 } }
                },
                Consequences = new OrderConsequence
                {
                    Success = new OrderOutcome
                    {
                        SkillXp = new Dictionary<string, int> { { "Tactics", 70 }, { "Leadership", 60 } },
                        TraitXp = new Dictionary<string, int> { { "SergeantCommandSkills", 100 } },
                        Reputation = new Dictionary<string, int> { { "lord", 8 }, { "officer", 15 }, { "soldier", 10 } },
                        CompanyNeeds = new Dictionary<string, int> { { "Readiness", 8 }, { "Morale", 10 } },
                        Denars = 75,
                        Text = "Your leadership is solid. The patrol returns safely with intel."
                    },
                    Failure = new OrderOutcome
                    {
                        Reputation = new Dictionary<string, int> { { "officer", -20 }, { "soldier", -15 } },
                        CompanyNeeds = new Dictionary<string, int> { { "Readiness", -15 }, { "Morale", -15 } },
                        Text = "Poor decisions cost lives. The men question your leadership."
                    },
                    Decline = new OrderOutcome
                    {
                        Reputation = new Dictionary<string, int> { { "officer", -25 }, { "soldier", -10 } },
                        Escalation = new Dictionary<string, int> { { "discipline", 2 } },
                        Text = "Refusing command? You'll never lead men again."
                    }
                },
                Tags = ["nco", "war", "outdoor"]
            };
        }

        private static Order CreateInspectDefensesOrder()
        {
            return new Order
            {
                Id = "t5_inspect_defenses",
                Title = "Inspect Defenses",
                Description = "Assess camp fortifications and recommend improvements.",
                Issuer = "auto",
                Requirements = new OrderRequirement
                {
                    TierMin = 5,
                    TierMax = 7,
                    MinSkills = new Dictionary<string, int> { { "Engineering", 40 } }
                },
                Consequences = new OrderConsequence
                {
                    Success = new OrderOutcome
                    {
                        SkillXp = new Dictionary<string, int> { { "Engineering", 80 }, { "Tactics", 40 } },
                        TraitXp = new Dictionary<string, int> { { "Siegecraft", 100 } },
                        Reputation = new Dictionary<string, int> { { "lord", 6 }, { "officer", 12 } },
                        CompanyNeeds = new Dictionary<string, int> { { "Readiness", 10 } },
                        Text = "Your improvements significantly strengthen the camp's defenses."
                    },
                    Failure = new OrderOutcome
                    {
                        Reputation = new Dictionary<string, int> { { "officer", -12 } },
                        CompanyNeeds = new Dictionary<string, int> { { "Readiness", -5 } },
                        Text = "You miss critical weaknesses. The officer is not impressed."
                    },
                    Decline = new OrderOutcome
                    {
                        Reputation = new Dictionary<string, int> { { "officer", -18 } },
                        Text = "The captain expected better from someone of your rank."
                    }
                },
                Tags = ["engineer", "camp", "siege", "war"]
            };
        }

        private static Order CreateTrainRecruitsOrder()
        {
            return new Order
            {
                Id = "t6_train_recruits",
                Title = "Train Recruits",
                Description = "Drill the newest soldiers in combat fundamentals.",
                Issuer = "auto",
                Requirements = new OrderRequirement
                {
                    TierMin = 5,
                    TierMax = 7,
                    MinSkills = new Dictionary<string, int> { { "Leadership", 40 } }
                },
                Consequences = new OrderConsequence
                {
                    Success = new OrderOutcome
                    {
                        SkillXp = new Dictionary<string, int> { { "Leadership", 90 } },
                        TraitXp = new Dictionary<string, int> { { "SergeantCommandSkills", 120 } },
                        Reputation = new Dictionary<string, int> { { "officer", 15 }, { "soldier", 12 } },
                        CompanyNeeds = new Dictionary<string, int> { { "Readiness", 12 }, { "Morale", 8 } },
                        Text = "The recruits improve rapidly under your instruction."
                    },
                    Failure = new OrderOutcome
                    {
                        Reputation = new Dictionary<string, int> { { "soldier", -10 } },
                        CompanyNeeds = new Dictionary<string, int> { { "Morale", -8 } },
                        Text = "Your harsh methods demoralize the recruits without improving them."
                    },
                    Decline = new OrderOutcome
                    {
                        Reputation = new Dictionary<string, int> { { "officer", -20 } },
                        CompanyNeeds = new Dictionary<string, int> { { "Readiness", -10 } },
                        Text = "The recruits remain green. Your refusal is noted."
                    }
                },
                Tags = ["nco", "camp", "peace"]
            };
        }

        #endregion

        #region Order Definitions - T7-T9 (Leadership)

        private static Order CreateLeadSquadOrder()
        {
            return new Order
            {
                Id = "t7_lead_squad",
                Title = "Command Squad in Battle",
                Description = "Take command of a squad during the upcoming engagement.",
                Issuer = "auto",
                Requirements = new OrderRequirement
                {
                    TierMin = 7,
                    TierMax = 9,
                    MinSkills = new Dictionary<string, int> { { "Leadership", 80 }, { "Tactics", 70 } }
                },
                Consequences = new OrderConsequence
                {
                    Success = new OrderOutcome
                    {
                        SkillXp = new Dictionary<string, int> { { "Leadership", 120 }, { "Tactics", 100 } },
                        TraitXp = new Dictionary<string, int> { { "Commander", 150 } },
                        Reputation = new Dictionary<string, int> { { "lord", 15 }, { "officer", 20 }, { "soldier", 20 } },
                        CompanyNeeds = new Dictionary<string, int> { { "Readiness", 15 }, { "Morale", 20 } },
                        Denars = 150,
                        Renown = 5,
                        Text = "Your tactical brilliance wins the day. The lord himself commends you."
                    },
                    Failure = new OrderOutcome
                    {
                        Reputation = new Dictionary<string, int> { { "lord", -15 }, { "officer", -25 }, { "soldier", -30 } },
                        CompanyNeeds = new Dictionary<string, int> { { "Readiness", -25 }, { "Morale", -30 } },
                        Renown = -3,
                        Text = "Heavy casualties under your command. Your reputation is damaged."
                    },
                    Decline = new OrderOutcome
                    {
                        Reputation = new Dictionary<string, int> { { "lord", -30 }, { "officer", -40 } },
                        Escalation = new Dictionary<string, int> { { "discipline", 3 } },
                        Text = "Cowardice from an officer? You're stripped of command."
                    }
                },
                Tags = ["officer", "battle", "war", "strategic"]
            };
        }

        private static Order CreatePlanStrategyOrder()
        {
            return new Order
            {
                Id = "t8_plan_strategy",
                Title = "Strategic Planning",
                Description = "Attend the war council and provide tactical recommendations.",
                Issuer = "auto",
                Requirements = new OrderRequirement
                {
                    TierMin = 7,
                    TierMax = 9,
                    MinSkills = new Dictionary<string, int> { { "Tactics", 100 }, { "Leadership", 80 } }
                },
                Consequences = new OrderConsequence
                {
                    Success = new OrderOutcome
                    {
                        SkillXp = new Dictionary<string, int> { { "Tactics", 150 }, { "Leadership", 100 } },
                        TraitXp = new Dictionary<string, int> { { "Commander", 200 } },
                        Reputation = new Dictionary<string, int> { { "lord", 25 } },
                        CompanyNeeds = new Dictionary<string, int> { { "Readiness", 20 } },
                        Denars = 200,
                        Renown = 10,
                        Text = "Your strategic insight impresses the war council. The lord values your counsel."
                    },
                    Failure = new OrderOutcome
                    {
                        Reputation = new Dictionary<string, int> { { "lord", -20 } },
                        Text = "Your suggestions are dismissed. Perhaps you're not ready for this."
                    },
                    Decline = new OrderOutcome
                    {
                        Reputation = new Dictionary<string, int> { { "lord", -35 } },
                        Text = "The lord's eyes narrow. 'I don't forget who refuses my summons.'"
                    }
                },
                Tags = ["officer", "strategic", "war", "camp"]
            };
        }

        private static Order CreateCoordinateSupplyLineOrder()
        {
            return new Order
            {
                Id = "t7_coordinate_supply",
                Title = "Coordinate Supply Line",
                Description = "Organize logistics for the army's supply train.",
                Issuer = "auto",
                Requirements = new OrderRequirement
                {
                    TierMin = 7,
                    TierMax = 9,
                    MinSkills = new Dictionary<string, int> { { "Steward", 80 }, { "Leadership", 60 } }
                },
                Consequences = new OrderConsequence
                {
                    Success = new OrderOutcome
                    {
                        SkillXp = new Dictionary<string, int> { { "Steward", 130 }, { "Leadership", 80 } },
                        Reputation = new Dictionary<string, int> { { "lord", 18 }, { "officer", 15 } },
                        CompanyNeeds = new Dictionary<string, int> { { "Supplies", 25 }, { "Morale", 15 } },
                        Denars = 120,
                        Text = "The supply line flows smoothly. The army is well-provisioned."
                    },
                    Failure = new OrderOutcome
                    {
                        Reputation = new Dictionary<string, int> { { "lord", -18 } },
                        CompanyNeeds = new Dictionary<string, int> { { "Supplies", -20 }, { "Morale", -15 } },
                        Text = "Supply shortages cripple the campaign. You're held responsible."
                    },
                    Decline = new OrderOutcome
                    {
                        Reputation = new Dictionary<string, int> { { "lord", -28 }, { "officer", -20 } },
                        Text = "Logistics are critical. Your refusal is a serious misstep."
                    }
                },
                Tags = ["officer", "war", "strategic"]
            };
        }

        private static Order CreateInterrogatePrisonerOrder()
        {
            return new Order
            {
                Id = "t7_interrogate_prisoner",
                Title = "Interrogate Prisoner",
                Description = "Question a captured enemy officer for intelligence.",
                Issuer = "auto",
                Requirements = new OrderRequirement
                {
                    TierMin = 7,
                    TierMax = 9,
                    MinSkills = new Dictionary<string, int> { { "Charm", 60 } }
                },
                Consequences = new OrderConsequence
                {
                    Success = new OrderOutcome
                    {
                        SkillXp = new Dictionary<string, int> { { "Charm", 100 }, { "Tactics", 80 } },
                        TraitXp = new Dictionary<string, int> { { "RogueSkills", 150 } },
                        Reputation = new Dictionary<string, int> { { "lord", 20 }, { "officer", 18 } },
                        CompanyNeeds = new Dictionary<string, int> { { "Readiness", 18 } },
                        Denars = 100,
                        Text = "You extract valuable intelligence. The lord is very pleased."
                    },
                    Failure = new OrderOutcome
                    {
                        Reputation = new Dictionary<string, int> { { "lord", -12 } },
                        Text = "The prisoner reveals nothing. A wasted opportunity."
                    },
                    Decline = new OrderOutcome
                    {
                        Reputation = new Dictionary<string, int> { { "lord", -25 } },
                        Text = "Intelligence is vital. Your squeamishness is disappointing."
                    }
                },
                Tags = ["operative", "officer", "war", "camp"]
            };
        }

        private static Order CreateInspectCompanyReadinessOrder()
        {
            return new Order
            {
                Id = "t8_inspect_readiness",
                Title = "Inspect Company Readiness",
                Description = "Conduct a comprehensive readiness assessment of the company.",
                Issuer = "auto",
                Requirements = new OrderRequirement
                {
                    TierMin = 8,
                    TierMax = 9,
                    MinSkills = new Dictionary<string, int> { { "Leadership", 100 }, { "Tactics", 80 } }
                },
                Consequences = new OrderConsequence
                {
                    Success = new OrderOutcome
                    {
                        SkillXp = new Dictionary<string, int> { { "Leadership", 140 }, { "Tactics", 100 } },
                        TraitXp = new Dictionary<string, int> { { "Commander", 180 } },
                        Reputation = new Dictionary<string, int> { { "lord", 22 }, { "officer", 18 }, { "soldier", 15 } },
                        CompanyNeeds = new Dictionary<string, int> { { "Readiness", 25 }, { "Morale", 12 } },
                        Text = "Your inspection identifies and corrects numerous issues. The company is battle-ready."
                    },
                    Failure = new OrderOutcome
                    {
                        Reputation = new Dictionary<string, int> { { "lord", -15 } },
                        CompanyNeeds = new Dictionary<string, int> { { "Readiness", -10 } },
                        Text = "You miss critical readiness issues. The lord is not impressed."
                    },
                    Decline = new OrderOutcome
                    {
                        Reputation = new Dictionary<string, int> { { "lord", -30 } },
                        Text = "This is a direct order from your lord. Unacceptable."
                    }
                },
                Tags = ["officer", "camp", "war", "strategic"]
            };
        }

        private static Order CreateNegotiateTruceOrder()
        {
            return new Order
            {
                Id = "t9_negotiate_truce",
                Title = "Negotiate Truce",
                Description = "Represent your lord in truce negotiations with the enemy.",
                Issuer = "auto",
                Requirements = new OrderRequirement
                {
                    TierMin = 8,
                    TierMax = 9,
                    MinSkills = new Dictionary<string, int> { { "Charm", 100 }, { "Leadership", 100 } }
                },
                Consequences = new OrderConsequence
                {
                    Success = new OrderOutcome
                    {
                        SkillXp = new Dictionary<string, int> { { "Charm", 180 }, { "Leadership", 150 } },
                        TraitXp = new Dictionary<string, int> { { "Commander", 250 } },
                        Reputation = new Dictionary<string, int> { { "lord", 35 } },
                        CompanyNeeds = new Dictionary<string, int> { { "Morale", 25 }, { "Rest", 15 } },
                        Denars = 300,
                        Renown = 15,
                        Text = "Your diplomatic skill secures favorable terms. Your lord is deeply grateful."
                    },
                    Failure = new OrderOutcome
                    {
                        Reputation = new Dictionary<string, int> { { "lord", -25 } },
                        Renown = -5,
                        Text = "Negotiations collapse. The war continues with no resolution."
                    },
                    Decline = new OrderOutcome
                    {
                        Reputation = new Dictionary<string, int> { { "lord", -50 } },
                        Text = "The lord's face darkens. 'Perhaps I've placed my trust unwisely.'"
                    }
                },
                Tags = ["officer", "strategic", "war"]
            };
        }

        #endregion
    }
}

