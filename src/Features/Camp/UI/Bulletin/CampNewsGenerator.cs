using System;
using System.Collections.Generic;
using System.Linq;
using Enlisted.Features.Schedule.Behaviors;
using Enlisted.Features.Schedule.Core;
using Enlisted.Features.Schedule.Models;
using TaleWorlds.CampaignSystem;

namespace Enlisted.Features.Camp.UI.Bulletin
{
    /// <summary>
    /// Generates news items for the Camp Bulletin feed from game events.
    /// Track A Phase 3: Richer news generation integrated with Track B schedule system.
    /// </summary>
    public static class CampNewsGenerator
    {
        private static int _newsIdCounter = 0;

        /// <summary>
        /// Generate welcome news (when no schedule system available).
        /// </summary>
        public static CampBulletinNewsItemVM GenerateWelcomeNews()
        {
            string newsId = $"welcome_{_newsIdCounter++}";
            string title = "Camp Bulletin";
            string description = "Welcome to the camp bulletin board. News and orders will be posted here.";

            return new CampBulletinNewsItemVM(
                newsId,
                title,
                description,
                CampaignTime.Now,
                "General",
                1 // Normal priority
            );
        }

        /// <summary>
        /// Generate general camp status news.
        /// </summary>
        public static CampBulletinNewsItemVM GenerateCampStatusNews()
        {
            string newsId = $"camp_status_{_newsIdCounter++}";
            string title = "Camp Status";
            
            var hour = (int)CampaignTime.Now.CurrentHourInDay;
            string timeOfDay = hour < 6 ? "Night" : hour < 12 ? "Morning" : hour < 18 ? "Afternoon" : "Evening";
            
            string description = $"{timeOfDay} in camp. All quiet. Check your duties and manage your lance.";

            return new CampBulletinNewsItemVM(
                newsId,
                title,
                description,
                CampaignTime.Now,
                "General",
                0 // Low priority (background info)
            );
        }

        /// <summary>
        /// Generate news item when a new schedule is created.
        /// </summary>
        public static CampBulletinNewsItemVM GenerateScheduleNews(DailySchedule schedule)
        {
            if (schedule == null)
                return null;

            string newsId = $"schedule_{_newsIdCounter++}";
            string title = $"Day {schedule.CycleDay}/12 Schedule";
            string description = $"New orders: {schedule.LordOrders}";

            return new CampBulletinNewsItemVM(
                newsId,
                title,
                description,
                CampaignTime.Now,
                "Lance",
                1 // Normal priority
            );
        }

        /// <summary>
        /// Generate news item for critical lance needs.
        /// </summary>
        public static List<CampBulletinNewsItemVM> GenerateLanceNeedsWarnings(LanceNeedsState needs)
        {
            var warnings = new List<CampBulletinNewsItemVM>();

            if (needs == null)
                return warnings;

            var criticalNeeds = LanceNeedsManager.CheckCriticalNeeds(needs);

            foreach (var warning in criticalNeeds)
            {
                string newsId = $"need_warning_{warning.Key}_{_newsIdCounter++}";
                string title = $"{warning.Key} Critical!";
                string description = warning.Value;

                var newsItem = new CampBulletinNewsItemVM(
                    newsId,
                    title,
                    description,
                    CampaignTime.Now,
                    "Lance",
                    3 // Critical priority
                );

                warnings.Add(newsItem);
            }

            return warnings;
        }

        /// <summary>
        /// Generate news item when schedule block starts.
        /// </summary>
        public static CampBulletinNewsItemVM GenerateBlockStartNews(ScheduledBlock block)
        {
            if (block == null)
                return null;

            string newsId = $"block_start_{block.TimeBlock}_{_newsIdCounter++}";
            string title = $"{block.TimeBlock}: {block.Title}";
            string description = $"Current duty: {block.Description}";

            return new CampBulletinNewsItemVM(
                newsId,
                title,
                description,
                CampaignTime.Now,
                "Lance",
                1 // Normal priority
            );
        }

        /// <summary>
        /// Generate news item when schedule block completes.
        /// </summary>
        public static CampBulletinNewsItemVM GenerateBlockCompleteNews(ScheduledBlock block)
        {
            if (block == null)
                return null;

            string newsId = $"block_complete_{block.TimeBlock}_{_newsIdCounter++}";
            string title = $"Completed: {block.Title}";
            
            string rewardsText = "";
            if (block.XPReward > 0)
                rewardsText += $"+{block.XPReward} XP";
            
            string description = $"Duty completed for {block.TimeBlock}.";
            if (!string.IsNullOrEmpty(rewardsText))
                description += $" Earned: {rewardsText}";

            return new CampBulletinNewsItemVM(
                newsId,
                title,
                description,
                CampaignTime.Now,
                "Lance",
                0 // Low priority
            );
        }

        /// <summary>
        /// Generate news item for lord objective changes.
        /// </summary>
        public static CampBulletinNewsItemVM GenerateLordObjectiveNews(LordObjective objective, string description)
        {
            string newsId = $"lord_objective_{objective}_{_newsIdCounter++}";
            string title = $"New Orders: {objective}";

            int priority = objective switch
            {
                LordObjective.PreparingBattle => 3,  // Critical
                LordObjective.Fleeing => 3,           // Critical
                LordObjective.Besieging => 2,         // High
                LordObjective.Defending => 2,         // High
                _ => 1                                // Normal
            };

            return new CampBulletinNewsItemVM(
                newsId,
                title,
                description,
                CampaignTime.Now,
                "Lance",
                priority
            );
        }

        /// <summary>
        /// Generate news item when needs recover significantly.
        /// </summary>
        public static CampBulletinNewsItemVM GenerateNeedRecoveryNews(LanceNeed need, int oldValue, int newValue)
        {
            string newsId = $"need_recovery_{need}_{_newsIdCounter++}";
            int recovery = newValue - oldValue;

            string title = $"{need} Improved";
            string description = $"{need} increased from {oldValue}% to {newValue}% (+{recovery}%)";

            return new CampBulletinNewsItemVM(
                newsId,
                title,
                description,
                CampaignTime.Now,
                "Lance",
                0 // Low priority (good news)
            );
        }

        /// <summary>
        /// Generate welcome news item for new day.
        /// </summary>
        public static CampBulletinNewsItemVM GenerateDailyWelcomeNews(int cycleDay)
        {
            string newsId = $"daily_welcome_{cycleDay}_{_newsIdCounter++}";
            string title = $"New Day (Day {cycleDay}/12)";
            string description = "Check your schedule and complete assigned duties.";

            return new CampBulletinNewsItemVM(
                newsId,
                title,
                description,
                CampaignTime.Now,
                "Lance",
                1 // Normal priority
            );
        }

        /// <summary>
        /// Generate news item for T5-T6 tier promotions.
        /// </summary>
        public static CampBulletinNewsItemVM GeneratePromotionNews(int newTier)
        {
            string newsId = $"promotion_t{newTier}_{_newsIdCounter++}";
            string title = $"Promoted to Tier {newTier}!";
            
            string description = newTier switch
            {
                5 => "You've been promoted to Lance Second! The Lance Leader will seek your input on schedule decisions.",
                6 => "You've been promoted to Lance Leader! You now have full control over schedule assignments.",
                _ => $"Congratulations on reaching Tier {newTier}!"
            };

            return new CampBulletinNewsItemVM(
                newsId,
                title,
                description,
                CampaignTime.Now,
                "Command",
                2 // High priority (promotion!)
            );
        }

        /// <summary>
        /// Generate news item for performance feedback (T6 leaders).
        /// </summary>
        public static CampBulletinNewsItemVM GeneratePerformanceFeedbackNews(int performanceScore, string rating)
        {
            string newsId = $"performance_{performanceScore}_{_newsIdCounter++}";
            string title = $"📊 Performance Review: {rating}";
            
            string description;
            if (performanceScore >= 75)
                description = $"Excellent leadership! Score: {performanceScore}/100. The lord is pleased with your performance.";
            else if (performanceScore >= 45)
                description = $"Adequate performance. Score: {performanceScore}/100. Continue balancing needs and orders.";
            else
                description = $"Poor performance. Score: {performanceScore}/100. Address critical issues or risk consequences.";

            int priority = performanceScore >= 75 ? 0 : (performanceScore < 45 ? 2 : 1);

            return new CampBulletinNewsItemVM(
                newsId,
                title,
                description,
                CampaignTime.Now,
                performanceScore >= 75 ? "Lance" : "Alert",
                priority
            );
        }

        /// <summary>
        /// Generate news item for 12-day cycle completion.
        /// </summary>
        public static CampBulletinNewsItemVM GenerateCycleCompleteNews()
        {
            string newsId = $"cycle_complete_{_newsIdCounter++}";
            string title = "✨ 12-Day Cycle Complete";
            string description = "Pay Muster complete. New orders will be issued for the next cycle.";

            return new CampBulletinNewsItemVM(
                newsId,
                title,
                description,
                CampaignTime.Now,
                "General",
                1 // Normal priority
            );
        }

        /// <summary>
        /// Generate army status news (where is the army, what are they doing).
        /// </summary>
        public static CampBulletinNewsItemVM GenerateArmyStatusNews(TaleWorlds.CampaignSystem.Party.MobileParty army)
        {
            if (army == null || army.LeaderHero == null)
            {
                return GenerateWelcomeNews();
            }

            string newsId = $"army_status_{_newsIdCounter++}";
            string title = $"Army Status - {army.Name.ToString()}";
            
            string description;
            var settlement = army.CurrentSettlement;
            var armyCount = army.Army != null ? army.Army.Parties.Count : 1;
            int armyStrength = (int)army.Party.NumberOfAllMembers;
            
            if (settlement != null)
            {
                if (army.BesiegerCamp != null)
                {
                    description = $"Army is besieging {settlement.Name}. Lord {army.LeaderHero.Name} commands {armyCount} parties with ~{armyStrength} troops.";
                }
                else
                {
                    description = $"Army stationed at {settlement.Name}. Lord {army.LeaderHero.Name} commands {armyCount} parties with ~{armyStrength} troops.";
                }
            }
            else if (army.TargetSettlement != null)
            {
                description = $"Army marching toward {army.TargetSettlement.Name}. Lord {army.LeaderHero.Name} commands {armyCount} parties with ~{armyStrength} troops.";
            }
            else
            {
                description = $"Army on patrol. Lord {army.LeaderHero.Name} commands {armyCount} parties with ~{armyStrength} troops.";
            }

            return new CampBulletinNewsItemVM(
                newsId,
                title,
                description,
                CampaignTime.Now,
                "Army",
                2 // High priority
            );
        }

        /// <summary>
        /// Generate siege news.
        /// </summary>
        public static CampBulletinNewsItemVM GenerateSiegeNews(TaleWorlds.CampaignSystem.Siege.BesiegerCamp siege)
        {
            if (siege == null || siege.SiegeEvent == null)
            {
                return GenerateWelcomeNews();
            }

            string newsId = $"siege_{_newsIdCounter++}";
            var settlement = siege.SiegeEvent.BesiegedSettlement;
            string settlementName = settlement?.Name?.ToString() ?? "Unknown";
            string title = $"Siege of {settlementName}";
            
            string description;
            var siegeEvent = siege.SiegeEvent;
            if (siegeEvent.BesiegerCamp?.LeaderParty != null)
            {
                int attackerStrength = (int)siegeEvent.BesiegerCamp.LeaderParty.Party.NumberOfAllMembers;
                int defenderStrength = settlement != null && settlement.Town != null ? 
                    (int)settlement.Town.GarrisonParty.Party.NumberOfAllMembers : 0;
                
                description = $"Our army is besieging {settlementName}. Attackers: ~{attackerStrength} troops. Defenders: ~{defenderStrength} troops. Prepare for battle orders.";
            }
            else
            {
                description = $"Siege underway at {settlementName}. Stand ready for combat deployment.";
            }

            return new CampBulletinNewsItemVM(
                newsId,
                title,
                description,
                CampaignTime.Now,
                "Army",
                3 // Urgent priority
            );
        }

        /// <summary>
        /// Generate enlistment status news (always shows when enlisted).
        /// </summary>
        public static CampBulletinNewsItemVM GenerateEnlistmentStatusNews(Enlistment.Behaviors.EnlistmentBehavior enlistment)
        {
            if (enlistment == null || !enlistment.IsEnlisted)
            {
                return GenerateWelcomeNews();
            }

            string newsId = $"enlistment_{_newsIdCounter++}";
            string title = $"Enlistment Status - {enlistment.CurrentLanceName}";
            
            int tier = enlistment.EnlistmentTier;
            string tierName = tier switch
            {
                1 => "Recruit",
                2 => "Soldier",
                3 => "Veteran",
                4 => "Sergeant",
                5 => "Lance Corporal",
                6 => "Lance Leader",
                _ => "Unknown"
            };

            string description = $"You are a T{tier} {tierName} serving with {enlistment.CurrentLanceName}. Continue your duties to earn promotion and pay.";

            return new CampBulletinNewsItemVM(
                newsId,
                title,
                description,
                CampaignTime.Now,
                "General",
                2 // High priority
            );
        }

        /// <summary>
        /// Generate comprehensive kingdom status news.
        /// </summary>
        public static CampBulletinNewsItemVM GenerateKingdomStatusNews(TaleWorlds.CampaignSystem.Kingdom kingdom)
        {
            if (kingdom == null)
            {
                return GenerateWelcomeNews();
            }

            string newsId = $"kingdom_{_newsIdCounter++}";
            
            // Count active wars and track enemy names
            int activeWars = 0;
            string enemyNames = "";
            foreach (var otherKingdom in TaleWorlds.CampaignSystem.Kingdom.All)
            {
                if (otherKingdom != kingdom && kingdom.IsAtWarWith(otherKingdom))
                {
                    activeWars++;
                    if (activeWars == 1)
                    {
                        enemyNames = otherKingdom.Name.ToString();
                    }
                    else if (activeWars == 2)
                    {
                        enemyNames += " and " + otherKingdom.Name.ToString();
                    }
                }
            }

            string title = $"{kingdom.Name} - Kingdom Report";
            string description;

            if (activeWars == 0)
            {
                description = $"{kingdom.Name} is at peace. {kingdom.Fiefs.Count} settlements under our control. Maintain readiness and await orders.";
            }
            else if (activeWars == 1)
            {
                description = $"{kingdom.Name} is at war with {enemyNames}. {kingdom.Fiefs.Count} settlements under our control. Stand ready for deployment.";
            }
            else if (activeWars == 2)
            {
                description = $"{kingdom.Name} is at war with {enemyNames}. {kingdom.Fiefs.Count} settlements defended. Multiple fronts active.";
            }
            else
            {
                description = $"{kingdom.Name} is at war with {activeWars} factions. {kingdom.Fiefs.Count} settlements defended. The realm is embattled on all sides.";
            }

            return new CampBulletinNewsItemVM(
                newsId,
                title,
                description,
                CampaignTime.Now,
                "Kingdom",
                2 // High priority
            );
        }

        /// <summary>
        /// Generate lord status news (where is your commanding lord).
        /// </summary>
        public static CampBulletinNewsItemVM GenerateLordStatusNews(TaleWorlds.CampaignSystem.Hero lord)
        {
            if (lord == null || lord.PartyBelongedTo == null)
            {
                return GenerateWelcomeNews();
            }

            string newsId = $"lord_{_newsIdCounter++}";
            string title = $"Lord {lord.Name} - Commander Status";
            
            var party = lord.PartyBelongedTo;
            string description;

            if (party.CurrentSettlement != null)
            {
                description = $"Lord {lord.Name} is stationed at {party.CurrentSettlement.Name}. Party strength: {(int)party.Party.NumberOfAllMembers} troops.";
            }
            else if (party.TargetSettlement != null)
            {
                description = $"Lord {lord.Name} is traveling to {party.TargetSettlement.Name}. Party strength: {(int)party.Party.NumberOfAllMembers} troops.";
            }
            else if (party.Army != null)
            {
                description = $"Lord {lord.Name} is part of an army led by {party.Army.LeaderParty?.LeaderHero?.Name?.ToString() ?? "Unknown"}. Stand ready for orders.";
            }
            else
            {
                description = $"Lord {lord.Name} is on patrol. Party strength: {(int)party.Party.NumberOfAllMembers} troops. Await further orders.";
            }

            return new CampBulletinNewsItemVM(
                newsId,
                title,
                description,
                CampaignTime.Now,
                "Lord",
                2 // High priority
            );
        }

        /// <summary>
        /// Generate news about other parties in the lord's army.
        /// </summary>
        public static CampBulletinNewsItemVM GenerateArmyPartiesNews(TaleWorlds.CampaignSystem.Army army)
        {
            if (army == null || army.Parties == null || army.Parties.Count <= 1)
            {
                return null; // No other parties to report
            }

            string newsId = $"army_parties_{_newsIdCounter++}";
            string title = $"Army Composition";
            
            int totalParties = army.Parties.Count;
            int totalTroops = 0;
            foreach (var p in army.Parties)
            {
                if (p != null)
                {
                    totalTroops += (int)p.Party.NumberOfAllMembers;
                }
            }

            string description = $"Our army consists of {totalParties} parties with approximately {totalTroops} troops total. Multiple lances are deployed and ready for battle.";

            return new CampBulletinNewsItemVM(
                newsId,
                title,
                description,
                CampaignTime.Now,
                "Army",
                1 // Normal priority
            );
        }

    }
}

