using System;
using System.Collections.Generic;
using System.Linq;
using Enlisted.Features.CommandTent.Data;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Mod.Core.Logging;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace Enlisted.Features.CommandTent.Core
{
    /// <summary>
    /// Manages the player's personal retinue of soldiers. Handles soldier addition/removal,
    /// tier capacity checks, faction availability, and party size safeguards.
    /// </summary>
    public sealed class RetinueManager
    {
        private const string LogCategory = "Retinue";

        // Tier unlock thresholds
        public const int LanceTier = 4;
        public const int SquadTier = 5;
        public const int RetinueTier = 6;

        // Capacity by tier
        public const int LanceCapacity = 5;
        public const int SquadCapacity = 10;
        public const int RetinueCapacity = 20;

        // Factions that don't have horse archers
        private static readonly HashSet<string> NoHorseArcherFactions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "vlandia", "battania", "sturgia"
        };

        // Formation class mapping for soldier types
        private static readonly Dictionary<string, FormationClass> TypeToFormation =
            new Dictionary<string, FormationClass>(StringComparer.OrdinalIgnoreCase)
            {
                { "infantry", FormationClass.Infantry },
                { "archers", FormationClass.Ranged },
                { "cavalry", FormationClass.Cavalry },
                { "horse_archers", FormationClass.HorseArcher }
            };

        public static RetinueManager Instance { get; private set; }

        private readonly RetinueState _state;

        public RetinueManager(RetinueState state)
        {
            _state = state ?? new RetinueState();
            Instance = this;
            ModLogger.Debug(LogCategory, "RetinueManager initialized");
        }

        /// <summary>
        /// Gets the current retinue state for serialization or UI access.
        /// </summary>
        public RetinueState State => _state;

        #region Tier Capacity

        /// <summary>
        /// Gets the maximum soldier capacity for a given tier.
        /// </summary>
        /// <param name="tier">Player's enlistment tier (1-6)</param>
        /// <returns>Max soldiers allowed: 0 for tier less than 4, 5/10/20 for tier 4/5/6</returns>
        public static int GetTierCapacity(int tier)
        {
            return tier switch
            {
                >= RetinueTier => RetinueCapacity,
                SquadTier => SquadCapacity,
                LanceTier => LanceCapacity,
                _ => 0
            };
        }

        /// <summary>
        /// Gets the unit name for a tier (Lance, Squad, Retinue).
        /// </summary>
        public static string GetUnitName(int tier)
        {
            return tier switch
            {
                >= RetinueTier => "Retinue",
                SquadTier => "Squad",
                LanceTier => "Lance",
                _ => "None"
            };
        }

        /// <summary>
        /// Checks if the player can have a retinue at their current tier.
        /// </summary>
        public bool CanHaveRetinue(out string reason)
        {
            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment == null || !enlistment.IsEnlisted)
            {
                reason = "You must be enlisted to command soldiers.";
                return false;
            }

            var currentTier = enlistment.EnlistmentTier;
            if (currentTier < LanceTier)
            {
                reason = $"Requires Tier {LanceTier} or higher. You are Tier {currentTier}.";
                return false;
            }

            var party = PartyBase.MainParty;
            if (party == null)
            {
                reason = "No party found.";
                return false;
            }

            var availableSpace = party.PartySizeLimit - party.NumberOfAllMembers;
            if (availableSpace <= 0)
            {
                reason = "No party space available. Your companions fill your party.";
                return false;
            }

            reason = null;
            return true;
        }

        #endregion

        #region Faction Availability

        /// <summary>
        /// Checks if a soldier type is available for the given culture.
        /// Horse archers are not available for Vlandia, Battania, and Sturgia.
        /// </summary>
        /// <param name="typeId">Soldier type ID (infantry, archers, cavalry, horse_archers)</param>
        /// <param name="culture">The faction's culture</param>
        /// <returns>True if the type is available for this culture</returns>
        public static bool IsSoldierTypeAvailable(string typeId, CultureObject culture)
        {
            if (string.IsNullOrEmpty(typeId) || culture == null)
            {
                return false;
            }

            // Horse archers are faction-restricted
            if (typeId.Equals("horse_archers", StringComparison.OrdinalIgnoreCase))
            {
                var cultureId = culture.StringId?.ToLowerInvariant() ?? "";
                return !NoHorseArcherFactions.Contains(cultureId);
            }

            // All other types available for all factions
            return true;
        }

        /// <summary>
        /// Gets the formation class for a soldier type ID.
        /// </summary>
        public static FormationClass GetFormationClass(string typeId)
        {
            if (string.IsNullOrEmpty(typeId))
            {
                return FormationClass.Infantry;
            }

            return TypeToFormation.TryGetValue(typeId, out var formation)
                ? formation
                : FormationClass.Infantry;
        }

        #endregion

        #region Troop Retrieval

        /// <summary>
        /// Gets available troops of a specific type and tier range from a culture's troop tree.
        /// Uses CharacterHelper.GetTroopTree to traverse the faction's upgrade tree.
        /// </summary>
        /// <param name="typeId">Soldier type (infantry, archers, cavalry, horse_archers)</param>
        /// <param name="culture">The faction's culture</param>
        /// <param name="playerTier">Player's tier for determining troop quality range</param>
        /// <returns>List of CharacterObjects matching the criteria</returns>
        public static List<CharacterObject> GetAvailableTroops(string typeId, CultureObject culture, int playerTier)
        {
            if (culture?.BasicTroop == null)
            {
                ModLogger.Warn(LogCategory, "GetAvailableTroops: Invalid culture or no basic troop");
                return new List<CharacterObject>();
            }

            var targetFormation = GetFormationClass(typeId);

            // Determine tier range based on player tier
            int minTier, maxTier;
            if (playerTier >= RetinueTier)
            {
                minTier = 3;
                maxTier = 4;
            }
            else
            {
                minTier = 2;
                maxTier = 3;
            }

            // Get all troops from the culture's troop tree within tier range
            // Materialize to list immediately to avoid multiple enumeration
            var allTroops = CharacterHelper.GetTroopTree(culture.BasicTroop, minTier, maxTier).ToList();

            // Filter by formation class
            var filtered = allTroops
                .Where(t => t != null && t.DefaultFormationClass == targetFormation)
                .ToList();

            ModLogger.Debug(LogCategory,
                $"GetAvailableTroops: type={typeId}, culture={culture.StringId}, " +
                $"tierRange={minTier}-{maxTier}, found={filtered.Count} troops");

            // If no troops found in the exact formation, try finding any troops in tier range
            // This handles edge cases where a faction might not have troops in a specific formation
            if (filtered.Count == 0)
            {
                ModLogger.Warn(LogCategory,
                    $"No {typeId} troops found for {culture.StringId} in tier {minTier}-{maxTier}, " +
                    "falling back to any available troop");

                filtered = allTroops.Take(1).ToList();
            }

            return filtered;
        }

        /// <summary>
        /// Gets a random troop of the specified type appropriate for the player's tier.
        /// </summary>
        public static CharacterObject GetRandomTroop(string typeId, CultureObject culture, int playerTier)
        {
            var troops = GetAvailableTroops(typeId, culture, playerTier);
            if (troops.Count == 0)
            {
                return null;
            }

            return troops.GetRandomElement();
        }

        #endregion

        #region Add/Remove Soldiers

        /// <summary>
        /// Attempts to add soldiers to the player's party with tier and party size safeguards.
        /// </summary>
        /// <param name="count">Requested number of soldiers</param>
        /// <param name="typeId">Soldier type ID</param>
        /// <param name="actuallyAdded">Out: actual number added</param>
        /// <param name="message">Out: result message for UI</param>
        /// <returns>True if any soldiers were added</returns>
        public bool TryAddSoldiers(int count, string typeId, out int actuallyAdded, out string message)
        {
            actuallyAdded = 0;
            message = string.Empty;

            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment == null || !enlistment.IsEnlisted)
            {
                message = "You must be enlisted to command soldiers.";
                ModLogger.Warn(LogCategory, "AddSoldiers blocked: not enlisted");
                return false;
            }

            var party = MobileParty.MainParty;
            if (party == null)
            {
                message = "No party available.";
                ModLogger.Warn(LogCategory, "AddSoldiers blocked: no party");
                return false;
            }

            var currentTier = enlistment.EnlistmentTier;
            var culture = enlistment.CurrentLord?.Culture;

            if (culture == null)
            {
                message = "Cannot determine faction culture.";
                ModLogger.Warn(LogCategory, "AddSoldiers blocked: no culture");
                return false;
            }

            // Calculate available space
            var tierCapacity = GetTierCapacity(currentTier);
            var currentSoldiers = _state.TotalSoldiers;
            var tierAvailable = tierCapacity - currentSoldiers;

            var partyLimit = PartyBase.MainParty.PartySizeLimit;
            var currentMembers = PartyBase.MainParty.NumberOfAllMembers;
            var partyAvailable = partyLimit - currentMembers;

            // Take the more restrictive limit
            var maxCanAdd = Math.Min(tierAvailable, partyAvailable);
            actuallyAdded = Math.Min(count, maxCanAdd);

            if (actuallyAdded <= 0)
            {
                if (partyAvailable <= 0)
                {
                    message = "Party is full. Dismiss troops or increase party size.";
                }
                else if (tierAvailable <= 0)
                {
                    message = "Retinue is at full capacity for your rank.";
                }
                else
                {
                    message = "Cannot add soldiers.";
                }

                ModLogger.Warn(LogCategory, $"AddSoldiers blocked: {message}");
                return false;
            }

            // Set the type if not already set
            if (string.IsNullOrEmpty(_state.SelectedTypeId))
            {
                _state.SelectedTypeId = typeId;
            }

            // Add soldiers to roster
            var added = 0;
            for (var i = 0; i < actuallyAdded; i++)
            {
                var troop = GetRandomTroop(typeId, culture, currentTier);
                if (troop == null)
                {
                    ModLogger.Warn(LogCategory, $"Failed to get troop for type={typeId}");
                    continue;
                }

                party.MemberRoster.AddToCounts(troop, 1);
                _state.UpdateTroopCount(troop.StringId, 1);
                added++;

                ModLogger.Debug(LogCategory, $"Added {troop.Name} to retinue");
            }

            actuallyAdded = added;

            if (actuallyAdded < count)
            {
                message = $"Party limit reached. Only {actuallyAdded} soldiers assigned.";
                ModLogger.Info(LogCategory, message);
            }
            else
            {
                message = $"{actuallyAdded} soldiers assigned to your retinue.";
            }

            ModLogger.Info(LogCategory, $"AddSoldiers: added {actuallyAdded}/{count}, total={_state.TotalSoldiers}");
            return actuallyAdded > 0;
        }

        /// <summary>
        /// Removes all retinue troops from the player's roster and clears state.
        /// Called on capture, enlistment end, army defeat, or type change.
        /// </summary>
        /// <param name="reason">Logging reason for debugging</param>
        public void ClearRetinueTroops(string reason)
        {
            var main = MobileParty.MainParty;
            if (main == null)
            {
                _state.Clear();
                ModLogger.Debug(LogCategory, $"ClearRetinueTroops({reason}): no party, state cleared");
                return;
            }

            var totalCleared = 0;

            foreach (var kvp in _state.TroopCounts.ToList())
            {
                var troopId = kvp.Key;
                var count = kvp.Value;

                if (count <= 0)
                {
                    continue;
                }

                var character = CharacterObject.Find(troopId);
                if (character != null)
                {
                    // Remove from roster (clamp to actual roster count to prevent negative)
                    var actualCount = main.MemberRoster.GetTroopCount(character);
                    var toRemove = Math.Min(count, actualCount);

                    if (toRemove > 0)
                    {
                        main.MemberRoster.AddToCounts(character, -toRemove);
                        totalCleared += toRemove;
                        ModLogger.Debug(LogCategory, $"Cleared {toRemove}x {character.Name}");
                    }
                }
            }

            _state.Clear();
            ModLogger.Info(LogCategory, $"Cleared {totalCleared} retinue troops (reason: {reason})");
        }

        #endregion

        #region Companion Tracking

        /// <summary>
        /// Checks if a character is a player companion (not retinue).
        /// </summary>
        public static bool IsCompanion(CharacterObject character)
        {
            if (character == null || !character.IsHero)
            {
                return false;
            }

            return character.HeroObject?.IsPlayerCompanion == true;
        }

        /// <summary>
        /// Checks if a character is a retinue soldier (tracked in our state).
        /// </summary>
        public bool IsRetinueSoldier(CharacterObject character)
        {
            if (character == null || string.IsNullOrEmpty(character.StringId))
            {
                return false;
            }

            return _state.TroopCounts?.ContainsKey(character.StringId) == true;
        }

        /// <summary>
        /// Gets the count of companions in the player's party.
        /// </summary>
        public static int GetCompanionCount()
        {
            var roster = MobileParty.MainParty?.MemberRoster;
            if (roster == null)
            {
                return 0;
            }

            return roster.GetTroopRoster()
                .Count(e => e.Character.IsHero &&
                            e.Character.HeroObject?.IsPlayerCompanion == true);
        }

        /// <summary>
        /// Logs a breakdown of the player's party for debugging.
        /// </summary>
        public void LogPartyBreakdown()
        {
            var roster = MobileParty.MainParty?.MemberRoster;
            if (roster == null)
            {
                return;
            }

            var total = roster.TotalManCount;
            var companions = GetCompanionCount();
            var retinue = _state.TotalSoldiers;
            var other = total - 1 - companions - retinue; // -1 for player

            ModLogger.Debug("Party",
                $"Breakdown: Player=1, Companions={companions}, " +
                $"Retinue={retinue}, Other={other}, Total={total}");
        }

        #endregion

        #region Requisition System

        // Requisition configuration constants
        public const int RequisitionCooldownDays = 14;

        /// <summary>
        /// Checks if the player can perform an instant requisition.
        /// Validates: enlisted status, tier, cooldown, capacity, and gold.
        /// </summary>
        /// <param name="reason">Out: reason if cannot requisition</param>
        /// <returns>True if requisition is allowed</returns>
        public bool CanRequisition(out string reason)
        {
            reason = null;

            // Must be enlisted at Tier 4+
            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment == null || !enlistment.IsEnlisted)
            {
                reason = "You must be enlisted to requisition soldiers.";
                return false;
            }

            if (enlistment.EnlistmentTier < LanceTier)
            {
                reason = $"Requires Tier {LanceTier} or higher.";
                return false;
            }

            // Must have a retinue type selected
            if (!_state.HasTypeSelected)
            {
                reason = "You must select a soldier type first.";
                return false;
            }

            // Check cooldown
            if (!_state.IsRequisitionAvailable())
            {
                var daysRemaining = _state.GetRequisitionCooldownDays();
                reason = $"Requisition on cooldown: {daysRemaining} days remaining.";
                return false;
            }

            // Must have room for more soldiers
            var missing = GetMissingSoldierCount();
            if (missing <= 0)
            {
                reason = "Your retinue is at full strength.";
                return false;
            }

            // Must have enough gold
            var cost = CalculateRequisitionCost();
            var playerGold = Hero.MainHero?.Gold ?? 0;
            if (playerGold < cost)
            {
                reason = $"Not enough gold. Need {cost} denars, have {playerGold}.";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Calculates the total gold cost to requisition all missing soldiers.
        /// Uses native GetTroopRecruitmentCost formula multiplied by missing count.
        /// </summary>
        /// <returns>Total gold cost for requisition</returns>
        public int CalculateRequisitionCost()
        {
            var missing = GetMissingSoldierCount();
            if (missing <= 0)
            {
                return 0;
            }

            var costPerSoldier = GetAverageRecruitmentCost();
            return costPerSoldier * missing;
        }

        /// <summary>
        /// Gets the number of soldiers that can be added (respects tier and party limits).
        /// Uses the shared capacity check: Math.Min(tierCapacity - current, partyLimit - partyMembers)
        /// </summary>
        /// <returns>Number of soldiers that can be added</returns>
        public int GetMissingSoldierCount()
        {
            var enlistment = EnlistmentBehavior.Instance;
            if (enlistment == null || !enlistment.IsEnlisted)
            {
                return 0;
            }

            // Tier capacity check
            var tierCapacity = GetTierCapacity(enlistment.EnlistmentTier);
            var currentSoldiers = _state.TotalSoldiers;
            var tierAvailable = tierCapacity - currentSoldiers;

            // Party size check
            var party = PartyBase.MainParty;
            if (party == null)
            {
                return 0;
            }

            var partySpace = party.PartySizeLimit - party.NumberOfAllMembers;

            // Take the more restrictive limit
            return Math.Max(0, Math.Min(tierAvailable, partySpace));
        }

        /// <summary>
        /// Gets the average recruitment cost per soldier for the selected type.
        /// </summary>
        private int GetAverageRecruitmentCost()
        {
            var enlistment = EnlistmentBehavior.Instance;
            var culture = enlistment?.CurrentLord?.Culture;
            var playerTier = enlistment?.EnlistmentTier ?? 4;

            if (culture == null || string.IsNullOrEmpty(_state.SelectedTypeId))
            {
                // Fallback cost based on type
                return _state.SelectedTypeId switch
                {
                    "cavalry" or "horse_archers" => 200,
                    _ => 100
                };
            }

            var troops = GetAvailableTroops(_state.SelectedTypeId, culture, playerTier);
            if (troops == null || troops.Count == 0)
            {
                return 100;
            }

            // Calculate average cost across available troops
            var totalCost = 0;
            var count = 0;

            foreach (var troop in troops)
            {
                var cost = Campaign.Current?.Models?.PartyWageModel?
                    .GetTroopRecruitmentCost(troop, Hero.MainHero)
                    .RoundedResultNumber ?? 100;
                totalCost += cost;
                count++;
            }

            return count > 0 ? totalCost / count : 100;
        }

        /// <summary>
        /// Attempts to instantly fill all missing soldier slots for gold.
        /// Applies a 14-day cooldown on success.
        /// </summary>
        /// <param name="message">Out: result message for UI</param>
        /// <returns>True if requisition succeeded</returns>
        public bool TryRequisition(out string message)
        {
            const string reqCategory = "Requisition";

            if (!CanRequisition(out message))
            {
                ModLogger.Warn(reqCategory, $"Requisition blocked: {message}");
                return false;
            }

            var cost = CalculateRequisitionCost();
            var toAdd = GetMissingSoldierCount();

            if (toAdd <= 0)
            {
                message = "No soldiers needed.";
                ModLogger.Debug(reqCategory, message);
                return false;
            }

            // Deduct gold
            Hero.MainHero?.ChangeHeroGold(-cost);

            // Add soldiers
            if (TryAddSoldiers(toAdd, _state.SelectedTypeId, out var actuallyAdded, out var addMessage))
            {
                // Start cooldown
                _state.RequisitionCooldownEnd = CampaignTime.Now + CampaignTime.Days(RequisitionCooldownDays);

                message = $"{actuallyAdded} soldiers have reported for duty.";
                ModLogger.ActionResult(reqCategory, "Requisition", true,
                    $"Added {actuallyAdded} soldiers for {cost} gold, cooldown={RequisitionCooldownDays}d");

                return true;
            }

            // Refund gold on failure
            Hero.MainHero?.ChangeHeroGold(cost);
            message = addMessage;
            ModLogger.ActionResult(reqCategory, "Requisition", false, addMessage);
            return false;
        }

        /// <summary>
        /// Gets remaining cooldown days for requisition.
        /// </summary>
        public int GetRequisitionCooldownDays()
        {
            return _state.GetRequisitionCooldownDays();
        }

        /// <summary>
        /// Checks if requisition is off cooldown.
        /// </summary>
        public bool IsRequisitionAvailable()
        {
            return _state.IsRequisitionAvailable();
        }

        #endregion

        #region Leadership Notification

        /// <summary>
        /// Shows the Tier 4 leadership unlock notification dialog.
        /// Called when player reaches Tier 4 for the first time in a session.
        /// </summary>
        public static void ShowLeadershipNotification()
        {
            var title = new TextObject("{=ct_leadership_title}Promotion to Leadership");
            var message = new TextObject("{=ct_leadership_message}Your service has not gone unnoticed. " +
                "You've been granted the authority to command a small lance of soldiers in battle.\n\n" +
                "Visit the Command Tent to request men be assigned to your command. " +
                "Know that you'll be responsible for their welfare—each soldier in your care will cost 2 denars per day in upkeep.");

            InformationManager.ShowInquiry(
                new InquiryData(
                    title.ToString(),
                    message.ToString(),
                    true,
                    false,
                    new TextObject("{=ct_leadership_acknowledge}Understood").ToString(),
                    string.Empty,
                    null,
                    null),
                true);

            ModLogger.Info(LogCategory, "Showed Tier 4 leadership notification");
        }

        #endregion
    }
}

