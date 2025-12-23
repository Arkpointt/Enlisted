using System;
using Enlisted.Features.Context;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Features.Escalation;
using Enlisted.Features.Identity;
using Enlisted.Mod.Core.Logging;
using TaleWorlds.CampaignSystem;

namespace Enlisted.Features.Content
{
    /// <summary>
    /// Validates whether the player meets the requirements for an event to fire.
    /// Checks tier, role, context, skills, traits, and escalation thresholds.
    /// Used by EventSelector to filter eligible events before weighted selection.
    /// </summary>
    public static class EventRequirementChecker
    {
        private const string LogCategory = "EventRequirements";

        /// <summary>
        /// Checks if the player meets all requirements for an event.
        /// </summary>
        /// <param name="requirements">The event's requirements to check against.</param>
        /// <returns>True if all requirements are met, false otherwise.</returns>
        public static bool MeetsRequirements(EventRequirements requirements)
        {
            if (requirements == null)
            {
                return true; // No requirements means always eligible
            }

            try
            {
                // Check tier requirements
                if (!MeetsTierRequirement(requirements))
                {
                    return false;
                }

                // Check role requirement
                if (!MeetsRoleRequirement(requirements))
                {
                    return false;
                }

                // Check context requirement
                if (!MeetsContextRequirement(requirements))
                {
                    return false;
                }

                // Check skill requirements
                if (!MeetsSkillRequirements(requirements))
                {
                    return false;
                }

                // Check trait requirements
                if (!MeetsTraitRequirements(requirements))
                {
                    return false;
                }

                // Check escalation requirements
                if (!MeetsEscalationRequirements(requirements))
                {
                    return false;
                }
                
                // Check onboarding requirements (stage and track match)
                if (!MeetsOnboardingRequirements(requirements))
                {
                    return false;
                }
                
                // Check HP requirements (for decisions like Seek Treatment)
                if (!MeetsHpRequirement(requirements))
                {
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, "Error checking event requirements", ex);
                return false;
            }
        }

        /// <summary>
        /// Checks if player's tier is within the required range.
        /// </summary>
        private static bool MeetsTierRequirement(EventRequirements requirements)
        {
            var playerTier = GetPlayerTier();

            if (requirements.MinTier.HasValue && playerTier < requirements.MinTier.Value)
            {
                return false;
            }

            if (requirements.MaxTier.HasValue && playerTier > requirements.MaxTier.Value)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Checks if player's primary role matches the required role.
        /// "Any" means no restriction.
        /// </summary>
        private static bool MeetsRoleRequirement(EventRequirements requirements)
        {
            if (string.IsNullOrEmpty(requirements.Role) ||
                requirements.Role.Equals("Any", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            var playerRole = GetPlayerRole();
            return playerRole.Equals(requirements.Role, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Checks if current campaign context matches the required context.
        /// "Any" means no restriction.
        /// </summary>
        private static bool MeetsContextRequirement(EventRequirements requirements)
        {
            if (string.IsNullOrEmpty(requirements.Context) ||
                requirements.Context.Equals("Any", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            var currentContext = GetCurrentContext();

            // Handle both simple context names and strategic context mapping
            return ContextMatches(currentContext, requirements.Context);
        }

        /// <summary>
        /// Checks if player meets all minimum skill requirements.
        /// </summary>
        private static bool MeetsSkillRequirements(EventRequirements requirements)
        {
            if (requirements.MinSkills == null || requirements.MinSkills.Count == 0)
            {
                return true;
            }

            var hero = Hero.MainHero;
            if (hero == null)
            {
                return false;
            }

            foreach (var skillReq in requirements.MinSkills)
            {
                var skill = SkillCheckHelper.GetSkillByName(skillReq.Key);
                if (skill == null)
                {
                    continue; // Skip unknown skills
                }

                if (!SkillCheckHelper.MeetsSkillThreshold(hero, skill, skillReq.Value))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Checks if player meets all minimum trait requirements.
        /// </summary>
        private static bool MeetsTraitRequirements(EventRequirements requirements)
        {
            if (requirements.MinTraits == null || requirements.MinTraits.Count == 0)
            {
                return true;
            }

            var hero = Hero.MainHero;
            if (hero == null)
            {
                return false;
            }

            foreach (var traitReq in requirements.MinTraits)
            {
                var trait = SkillCheckHelper.GetTraitByName(traitReq.Key);
                if (trait == null)
                {
                    continue; // Skip unknown traits
                }

                if (!SkillCheckHelper.MeetsTraitThreshold(hero, trait, traitReq.Value))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Checks if escalation tracks meet minimum thresholds.
        /// </summary>
        private static bool MeetsEscalationRequirements(EventRequirements requirements)
        {
            if (requirements.MinEscalation == null || requirements.MinEscalation.Count == 0)
            {
                return true;
            }

            var escalation = EscalationManager.Instance?.State;
            if (escalation == null)
            {
                return false;
            }

            foreach (var escReq in requirements.MinEscalation)
            {
                var trackValue = GetEscalationTrackValue(escalation, escReq.Key);
                if (trackValue < escReq.Value)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Gets the value of an escalation track by name.
        /// </summary>
        private static int GetEscalationTrackValue(EscalationState state, string trackName)
        {
            return trackName.ToLowerInvariant() switch
            {
                "scrutiny" => state.Scrutiny,
                "discipline" => state.Discipline,
                "soldierreputation" or "soldier_reputation" or "soldierrep" => state.SoldierReputation,
                "lordreputation" or "lord_reputation" or "lordrep" => state.LordReputation,
                "officerreputation" or "officer_reputation" or "officerrep" => state.OfficerReputation,
                "medicalrisk" or "medical_risk" => state.MedicalRisk,
                _ => 0
            };
        }
        
        /// <summary>
        /// Checks if the current onboarding state matches the event's requirements.
        /// Events with OnboardingStage set are only eligible when the player is at that stage.
        /// Events without OnboardingStage set are eligible regardless of onboarding state.
        /// </summary>
        private static bool MeetsOnboardingRequirements(EventRequirements requirements)
        {
            // No onboarding requirement = always eligible (not an onboarding event)
            if (!requirements.OnboardingStage.HasValue)
            {
                return true;
            }
            
            var escalation = EscalationManager.Instance?.State;
            if (escalation == null)
            {
                return false;
            }
            
            // Onboarding events require player to be actively enlisted
            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment == null || !enlistment.IsEnlisted)
            {
                return false;
            }
            
            // Check if player is at the required onboarding stage
            if (!escalation.IsOnboardingStage(requirements.OnboardingStage.Value))
            {
                return false;
            }
            
            // Stale state check: onboarding active but track is empty = corrupted state, skip
            if (escalation.IsOnboardingActive && string.IsNullOrEmpty(escalation.OnboardingTrack))
            {
                return false;
            }
            
            // If a specific track is required, check that too
            if (!string.IsNullOrEmpty(requirements.OnboardingTrack))
            {
                if (!string.Equals(escalation.OnboardingTrack, requirements.OnboardingTrack, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }
            
            return true;
        }

        /// <summary>
        /// Checks if the player's HP is below the required threshold.
        /// Used for decisions like Seek Treatment which only appear when wounded.
        /// </summary>
        private static bool MeetsHpRequirement(EventRequirements requirements)
        {
            if (!requirements.HpBelow.HasValue)
            {
                return true; // No HP requirement
            }

            var hero = Hero.MainHero;
            if (hero == null)
            {
                return false;
            }

            var maxHp = hero.CharacterObject.MaxHitPoints();
            if (maxHp <= 0)
            {
                return false;
            }

            var hpPercent = (hero.HitPoints * 100) / maxHp;
            return hpPercent < requirements.HpBelow.Value;
        }

        /// <summary>
        /// Gets the player's current enlistment tier.
        /// </summary>
        public static int GetPlayerTier()
        {
            return EnlistmentBehavior.Instance?.EnlistmentTier ?? 1;
        }

        /// <summary>
        /// Gets the player's primary role based on trait levels.
        /// </summary>
        public static string GetPlayerRole()
        {
            return EnlistedStatusManager.Instance?.GetPrimaryRole() ?? "Soldier";
        }

        /// <summary>
        /// Gets the current strategic context for the lord's party.
        /// Returns a simplified context for event matching.
        /// </summary>
        public static string GetCurrentContext()
        {
            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment?.CurrentLord?.PartyBelongedTo == null)
            {
                return "Peace";
            }

            var party = enlistment.CurrentLord.PartyBelongedTo;
            var strategicContext = ArmyContextAnalyzer.GetLordStrategicContext(party);

            // Map strategic context to simplified event context
            return MapStrategicToEventContext(strategicContext);
        }

        /// <summary>
        /// Maps detailed strategic context to simplified event context categories.
        /// </summary>
        private static string MapStrategicToEventContext(string strategicContext)
        {
            return strategicContext.ToLowerInvariant() switch
            {
                "siege_operation" => "Siege",
                "desperate_defense" => "War",
                "coordinated_offensive" => "War",
                "raid_operation" => "War",
                "recruitment_drive" => "War",
                "patrol_peacetime" => "Peace",
                "garrison_duty" => "Town",
                "winter_camp" => "Camp",
                _ => "Peace"
            };
        }

        /// <summary>
        /// Checks if the current context matches the required context.
        /// Handles hierarchical matching (War includes Battle, Siege includes War, etc.).
        /// </summary>
        private static bool ContextMatches(string current, string required)
        {
            if (string.IsNullOrEmpty(required))
            {
                return true;
            }

            var reqLower = required.ToLowerInvariant();
            var curLower = current.ToLowerInvariant();

            // Exact match
            if (reqLower == curLower)
            {
                return true;
            }

            // War context includes multiple sub-contexts
            if (reqLower == "war" && (curLower == "siege" || curLower == "battle"))
            {
                return true;
            }

            // Camp context matches peace
            if (reqLower == "camp" && curLower == "peace")
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if a trigger condition string is met.
        /// Supports flag: and has_flag: prefixes for flag checking.
        /// </summary>
        public static bool CheckTriggerCondition(string condition)
        {
            if (string.IsNullOrEmpty(condition))
            {
                return true;
            }

            // Check for flag: prefix (e.g., "flag:qm_owes_favor")
            if (condition.StartsWith("flag:", StringComparison.OrdinalIgnoreCase))
            {
                var flagName = condition.Substring(5).Trim();
                return EscalationManager.Instance?.State?.HasFlag(flagName) ?? false;
            }

            // Check for has_flag: prefix (e.g., "has_flag:mutiny_joined")
            if (condition.StartsWith("has_flag:", StringComparison.OrdinalIgnoreCase))
            {
                var flagName = condition.Substring(9).Trim();
                return EscalationManager.Instance?.State?.HasFlag(flagName) ?? false;
            }

            // For other conditions, return true (handled elsewhere or not implemented yet)
            return true;
        }
    }
}

