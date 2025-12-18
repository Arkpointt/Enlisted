using System;
using System.Linq;
using Enlisted.Features.Assignments.Behaviors;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Mod.Core.Logging;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace Enlisted.Features.CommandTent.Core
{
    /// <summary>
    /// Handles automatic granting of raw recruits when player reaches commander tiers (T7-T9).
    /// Part of Retinue System V2.0.
    /// 
    /// Grant amounts:
    /// - T6→T7: 15 soldiers (initial grant)
    /// - T7→T8: 10 soldiers (expansion)
    /// - T8→T9: 10 soldiers (final expansion)
    /// 
    /// Recruits match player's formation type and enlisted lord's culture.
    /// </summary>
    public static class RetinueRecruitmentGrant
    {
        private const string LogCategory = "RecruitGrant";

        /// <summary>
        /// Grants recruits when player promotes to commander tier.
        /// Called from PromotionBehavior after promotion completes.
        /// </summary>
        /// <param name="newTier">New tier after promotion</param>
        /// <param name="previousTier">Tier before promotion</param>
        public static void GrantCommanderRetinue(int newTier, int previousTier)
        {
            try
            {
                // Only grant on promotion to T7/T8/T9
                if (newTier < RetinueManager.CommanderTier1 || previousTier >= newTier)
                {
                    ModLogger.Debug(LogCategory,
                        $"No grant: newTier={newTier}, previousTier={previousTier}");
                    return;
                }

                var count = CalculateGrantCount(newTier, previousTier);
                if (count <= 0)
                {
                    ModLogger.Debug(LogCategory, "No soldiers to grant");
                    return;
                }

                // Get player's formation and lord's culture
                var formation = GetPlayerFormationType();
                var culture = GetEnlistedLordCulture();

                ModLogger.Info(LogCategory,
                    $"Granting {count} recruits (tier={newTier}, formation={formation}, culture={culture?.StringId ?? "null"})");

                GrantRawRecruits(count, formation, culture);
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory,
                    $"Failed to grant commander retinue: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Calculates how many recruits to grant based on tier transition.
        /// T6→T7: 15 (initial grant)
        /// T7→T8: 10 (expansion)
        /// T8→T9: 10 (final expansion)
        /// </summary>
        public static int CalculateGrantCount(int newTier, int previousTier)
        {
            // Initial grant at T7
            if (newTier >= RetinueManager.CommanderTier1 && previousTier < RetinueManager.CommanderTier1)
            {
                return RetinueManager.CommanderCapacity1; // 15
            }

            // Expansion grants
            if (newTier >= RetinueManager.CommanderTier2 && previousTier < RetinueManager.CommanderTier2)
            {
                return RetinueManager.CommanderCapacity2 - RetinueManager.CommanderCapacity1; // 10
            }

            if (newTier >= RetinueManager.CommanderTier3 && previousTier < RetinueManager.CommanderTier3)
            {
                return RetinueManager.CommanderCapacity3 - RetinueManager.CommanderCapacity2; // 10
            }

            return 0;
        }

        /// <summary>
        /// Grants raw recruits to player's party.
        /// Handles party size limits and capacity checks.
        /// </summary>
        private static void GrantRawRecruits(int count, FormationClass formation, CultureObject culture)
        {
            var recruitTroop = FindCultureRecruit(culture, formation);
            if (recruitTroop == null)
            {
                ModLogger.Error(LogCategory,
                    $"Could not find recruit for culture={culture?.StringId}, formation={formation}");
                ShowRecruitGrantFailedMessage(count);
                return;
            }

            // Check party size limit
            var mobileParty = MobileParty.MainParty;
            if (mobileParty == null)
            {
                ModLogger.Error(LogCategory, "Player party is null");
                return;
            }

            var partyBase = mobileParty.Party;
            var partySpace = partyBase.PartySizeLimit - partyBase.NumberOfAllMembers;
            if (partySpace < count)
            {
                ModLogger.Warn(LogCategory,
                    $"Party size limit: requested {count}, space available {partySpace}");
                count = Math.Max(0, partySpace);
            }

            if (count <= 0)
            {
                ModLogger.Warn(LogCategory, "No party space for recruits");
                ShowPartyFullMessage();
                return;
            }

            // Add recruits to party roster
            mobileParty.MemberRoster.AddToCounts(recruitTroop, count);

            // Update retinue state tracking
            var manager = RetinueManager.Instance;
            if (manager != null)
            {
                manager.State.UpdateTroopCount(recruitTroop.StringId, count);
                manager.State.SelectedTypeId = GetFormationTypeId(formation);
            }

            ModLogger.Info(LogCategory,
                $"Granted {count}x {recruitTroop.Name} (ID: {recruitTroop.StringId})");

            ShowRecruitGrantNotification(count, recruitTroop.Name);
        }

        /// <summary>
        /// Finds the appropriate raw recruit troop for given culture and formation.
        /// Returns lowest-tier troop matching formation class.
        /// </summary>
        public static CharacterObject FindCultureRecruit(CultureObject culture, FormationClass formation)
        {
            if (culture?.BasicTroop == null)
            {
                ModLogger.Warn(LogCategory, "No culture or basic troop - using fallback");
                return FindFallbackRecruit(formation);
            }

            // Get all troops from the culture's basic troop tree
            var allTroops = Helpers.CharacterHelper.GetTroopTree(culture.BasicTroop, 1, 3).ToList();

            // Find lowest-level troop matching formation
            var candidates = allTroops
                .Where(t => t != null && IsValidRecruit(t, culture.StringId, formation))
                .OrderBy(t => t.Level)
                .ThenBy(t => t.Tier)
                .ToList();

            var recruit = candidates.FirstOrDefault();

            if (recruit != null)
            {
                ModLogger.Debug(LogCategory,
                    $"Found recruit: {recruit.Name} (Level {recruit.Level}, Tier {recruit.Tier})");
            }
            else
            {
                ModLogger.Warn(LogCategory,
                    $"No recruit found for culture={culture.StringId}, formation={formation}");

                // Fallback: try to find any basic troop of right formation
                recruit = FindFallbackRecruit(formation);
            }

            return recruit;
        }

        /// <summary>
        /// Validates if a troop is a valid recruit for granting.
        /// </summary>
        private static bool IsValidRecruit(CharacterObject troop, string cultureId, FormationClass formation)
        {
            // Must match culture
            if (!string.Equals(troop.Culture?.StringId, cultureId, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // Must be basic troop (not hero, not noble)
            if (!troop.IsBasicTroop || troop.IsHero)
            {
                return false;
            }

            // Must match formation
            if (troop.DefaultFormationClass != formation)
            {
                return false;
            }

            // Must be low level (raw recruit - level 10 or below)
            if (troop.Level > 15)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Fallback recruit finder if culture-specific search fails.
        /// Finds any basic low-level troop matching formation.
        /// </summary>
        private static CharacterObject FindFallbackRecruit(FormationClass formation)
        {
            var fallback = CharacterObject.All
                .Where(t => t != null && t.IsBasicTroop && !t.IsHero)
                .Where(t => t.DefaultFormationClass == formation)
                .Where(t => t.Level <= 10)
                .Where(t => t.UpgradeTargets != null && t.UpgradeTargets.Length > 0)
                .OrderBy(t => t.Level)
                .FirstOrDefault();

            if (fallback != null)
            {
                ModLogger.Warn(LogCategory,
                    $"Using fallback recruit: {fallback.Name} (Culture: {fallback.Culture?.StringId})");
            }

            return fallback;
        }

        /// <summary>
        /// Gets player's formation type from enlisted duties.
        /// </summary>
        private static FormationClass GetPlayerFormationType()
        {
            var duties = EnlistedDutiesBehavior.Instance;
            if (duties != null)
            {
                var formationStr = duties.PlayerFormation;
                if (!string.IsNullOrEmpty(formationStr))
                {
                    return ParseFormationClass(formationStr);
                }
            }

            // Fallback to infantry if no formation set
            ModLogger.Warn(LogCategory, "No player formation set, defaulting to infantry");
            return FormationClass.Infantry;
        }

        /// <summary>
        /// Gets enlisted lord's culture.
        /// </summary>
        private static CultureObject GetEnlistedLordCulture()
        {
            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment?.CurrentLord?.Culture != null)
            {
                return enlistment.CurrentLord.Culture;
            }

            // Fallback to empire if no lord culture
            ModLogger.Warn(LogCategory, "No lord culture found, using player culture fallback");
            return Hero.MainHero?.Culture;
        }

        /// <summary>
        /// Parses formation string to FormationClass enum.
        /// </summary>
        private static FormationClass ParseFormationClass(string formation)
        {
            if (string.IsNullOrEmpty(formation))
            {
                return FormationClass.Infantry;
            }

            return formation.ToLowerInvariant() switch
            {
                "infantry" => FormationClass.Infantry,
                "archers" or "archer" or "ranged" => FormationClass.Ranged,
                "cavalry" => FormationClass.Cavalry,
                "horse_archers" or "horsearcher" or "horsearchers" => FormationClass.HorseArcher,
                _ => FormationClass.Infantry
            };
        }

        /// <summary>
        /// Gets the type ID string for a formation class.
        /// </summary>
        private static string GetFormationTypeId(FormationClass formation)
        {
            return formation switch
            {
                FormationClass.Infantry => "infantry",
                FormationClass.Ranged => "archers",
                FormationClass.Cavalry => "cavalry",
                FormationClass.HorseArcher => "horse_archers",
                _ => "infantry"
            };
        }

        #region Notifications

        private static void ShowRecruitGrantNotification(int count, TextObject troopName)
        {
            var msg = new TextObject("{=retinue_grant_success}{COUNT} {TROOP_NAME} have been assigned to your command.")
                .SetTextVariable("COUNT", count)
                .SetTextVariable("TROOP_NAME", troopName);

            InformationManager.DisplayMessage(new InformationMessage(msg.ToString(), Colors.Green));

            // Also show a brief banner notification
            MBInformationManager.AddQuickInformation(msg,
                announcerCharacter: Hero.MainHero?.CharacterObject);
        }

        private static void ShowRecruitGrantFailedMessage(int count)
        {
            var msg = new TextObject("{=retinue_grant_failed}Failed to assign {COUNT} recruits. Please report this issue.")
                .SetTextVariable("COUNT", count);

            InformationManager.DisplayMessage(new InformationMessage(msg.ToString(), Colors.Red));
        }

        private static void ShowPartyFullMessage()
        {
            var msg = new TextObject("{=retinue_party_full}Your party is full. Dismiss soldiers or increase party size to receive recruits.");

            InformationManager.DisplayMessage(new InformationMessage(msg.ToString(), Colors.Yellow));
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Checks if a troop is a raw recruit (lowest tier in upgrade tree).
        /// </summary>
        public static bool IsRawRecruit(CharacterObject troop)
        {
            if (troop == null || troop.IsHero)
            {
                return false;
            }

            // Check level
            if (troop.Level > 10)
            {
                return false;
            }

            // Check if has upgrade targets (not dead-end)
            if (troop.UpgradeTargets == null || troop.UpgradeTargets.Length == 0)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Gets the tier/quality level of a troop for display.
        /// </summary>
        public static string GetRecruitTierName(CharacterObject troop)
        {
            if (troop == null) return "Unknown";

            if (troop.Level <= 10)
            {
                return "Recruit";
            }
            else if (troop.Level <= 20)
            {
                return "Regular";
            }
            else
            {
                return "Veteran";
            }
        }

        #endregion
    }
}
