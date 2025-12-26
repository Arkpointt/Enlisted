using System;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Mod.Core.Logging;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace Enlisted.Features.Logistics
{
    /// <summary>
    /// Manages company supply calculation using a hybrid 40/60 model.
    /// The 40% "food component" observes the lord's party food status (read-only, vanilla system).
    /// The 60% "non-food component" simulates consumption of ammunition, repair materials, and camp supplies.
    /// This keeps supply responsive to gameplay without modifying vanilla food mechanics.
    /// </summary>
    public class CompanySupplyManager
    {
        private const string LogCategory = "Supply";

        /// <summary>
        /// Singleton instance for global access. Set when EnlistmentBehavior initializes.
        /// </summary>
        public static CompanySupplyManager Instance { get; private set; }

        /// <summary>
        /// The non-food supply component (0-60%). We manage consumption and resupply.
        /// Represents ammunition, repair materials, medical supplies, and camp gear.
        /// </summary>
        private float _nonFoodSupply = 60.0f;

        /// <summary>
        /// Cached food component from the last calculation. Used for logging changes.
        /// </summary>
        private float _lastFoodComponent = 40.0f;

        /// <summary>
        /// The enlisted lord whose party we observe for food status.
        /// </summary>
        private Hero _enlistedLord;

        /// <summary>
        /// Total supply percentage (0-100), combining food observation (0-40%) and non-food simulation (0-60%).
        /// Food component reflects the lord's party food days; non-food degrades/recovers based on activity.
        /// </summary>
        public int TotalSupply
        {
            get
            {
                float food = CalculateFoodComponent();
                float nonFood = _nonFoodSupply;
                return (int)MathF.Clamp(food + nonFood, 0f, 100f);
            }
        }

        /// <summary>
        /// Current non-food supply level (0-60). Exposed for save/load and debugging.
        /// </summary>
        public float NonFoodSupply => _nonFoodSupply;

        /// <summary>
        /// Initializes the supply manager singleton and sets the enlisted lord.
        /// Called when player enlists with a lord.
        /// </summary>
        /// <param name="enlistedLord">The lord the player is enlisting with.</param>
        /// <param name="preserveSupply">If true, preserves existing non-food supply level (for grace period re-enlistment).</param>
        public static void Initialize(Hero enlistedLord, bool preserveSupply = false)
        {
            float existingSupply = Instance?._nonFoodSupply ?? 60.0f;

            Instance = new CompanySupplyManager
            {
                _enlistedLord = enlistedLord,
                _nonFoodSupply = preserveSupply ? MathF.Clamp(existingSupply, 0f, 60f) : 60.0f
            };

            ModLogger.Info(LogCategory, preserveSupply
                ? $"CompanySupplyManager transferred to lord: {enlistedLord?.Name}, preserved supply: {Instance._nonFoodSupply:F1}%"
                : $"CompanySupplyManager initialized for lord: {enlistedLord?.Name}");
        }

        /// <summary>
        /// Clears the singleton when player leaves service.
        /// </summary>
        public static void Shutdown()
        {
            if (Instance != null)
            {
                ModLogger.Info(LogCategory, "CompanySupplyManager shutdown");
                Instance = null;
            }
        }

        /// <summary>
        /// Restores supply manager state after a save load.
        /// </summary>
        public static void RestoreFromSave(Hero enlistedLord, float nonFoodSupply)
        {
            Instance = new CompanySupplyManager
            {
                _enlistedLord = enlistedLord,
                _nonFoodSupply = MathF.Clamp(nonFoodSupply, 0f, 60f)
            };
            ModLogger.Info(LogCategory, $"CompanySupplyManager restored: NonFood={nonFoodSupply:F1}%, Lord={enlistedLord?.Name}");
        }

        /// <summary>
        /// Observes the lord's party food situation and maps it to a 0-40% contribution.
        /// Does NOT modify vanilla food - purely observational.
        /// </summary>
        private float CalculateFoodComponent()
        {
            MobileParty lordParty = GetLordParty();
            if (lordParty == null)
            {
                return 40.0f; // Full food component if not attached to a party
            }

            try
            {
                // Check starvation first (vanilla tracks this via Party.IsStarving)
                if (lordParty.Party.IsStarving)
                {
                    return 0.0f;
                }

                // Observe vanilla food days calculation
                // GetNumDaysForFoodToLast divides by -FoodChange; if FoodChange >= 0 (recovering), result is invalid
                float foodChange = lordParty.FoodChange;
                if (foodChange >= 0f)
                {
                    // Party is gaining or stable on food - treat as well-supplied
                    return 40.0f;
                }

                int daysOfFood = lordParty.GetNumDaysForFoodToLast();

                // Guard against negative or extreme values from edge cases
                if (daysOfFood < 0 || daysOfFood > 1000)
                {
                    return 40.0f;
                }

                // Map food days to supply contribution (0-40%)
                // 10+ days = excellent supply, diminishing returns below that
                if (daysOfFood >= 10)
                {
                    return 40.0f;
                }
                if (daysOfFood >= 7)
                {
                    return 35.0f;
                }
                if (daysOfFood >= 5)
                {
                    return 30.0f;
                }
                if (daysOfFood >= 3)
                {
                    return 20.0f;
                }
                if (daysOfFood >= 1)
                {
                    return 10.0f;
                }
                return 5.0f; // Less than 1 day but not starving yet
            }
            catch (Exception ex)
            {
                ModLogger.Debug(LogCategory, $"Error calculating food component: {ex.Message}");
                return 40.0f; // Fail-safe: assume well-supplied
            }
        }

        /// <summary>
        /// Processes daily supply updates: consumption and resupply of non-food supplies.
        /// Called from EnlistmentBehavior's daily tick while enlisted.
        /// </summary>
        public void DailyUpdate()
        {
            try
            {
                float oldNonFood = _nonFoodSupply;
                float oldFood = _lastFoodComponent;

                float consumption = CalculateNonFoodConsumption();
                float resupply = CalculateNonFoodResupply();

                _nonFoodSupply = MathF.Clamp(_nonFoodSupply - consumption + resupply, 0f, 60f);
                _lastFoodComponent = CalculateFoodComponent();

                // Log significant changes
                float nonFoodChange = _nonFoodSupply - oldNonFood;
                float foodChange = _lastFoodComponent - oldFood;
                int totalSupply = TotalSupply;

                if (MathF.Abs(nonFoodChange) > 0.5f || MathF.Abs(foodChange) > 2f)
                {
                    ModLogger.Debug(LogCategory,
                        $"Daily supply update: NonFood {oldNonFood:F1}->{_nonFoodSupply:F1} ({nonFoodChange:+0.0;-0.0}), " +
                        $"Food {oldFood:F0}->{_lastFoodComponent:F0}, Total={totalSupply}%");
                }

                // Log warnings at thresholds
                if (totalSupply < 30 && (oldNonFood + oldFood) >= 30)
                {
                    ModLogger.Warn(LogCategory, $"Supply CRITICAL: {totalSupply}% - equipment access blocked");
                }
                else if (totalSupply < 50 && (oldNonFood + oldFood) >= 50)
                {
                    ModLogger.Info(LogCategory, $"Supply LOW: {totalSupply}% - resupply recommended");
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, "Error in DailyUpdate", ex);
            }
        }

        /// <summary>
        /// Calculates daily non-food supply consumption based on activity and conditions.
        /// Base rate is 1.5% per day, modified by company size, activity, and terrain.
        /// </summary>
        private float CalculateNonFoodConsumption()
        {
            MobileParty lordParty = GetLordParty();
            if (lordParty == null)
            {
                return 0f;
            }

            // Base consumption rate: 1.5% per day for non-food supplies
            float baseRate = 1.5f;

            // Size multiplier: scales with party size relative to 100-troop baseline
            int partySize = lordParty.Party.NumberOfAllMembers;
            float sizeMultiplier = MathF.Max(0.3f, partySize / 100f);

            // Activity multiplier based on what the party is doing
            float activityMult = GetActivityMultiplier(lordParty);

            // Terrain multiplier based on current terrain type
            float terrainMult = GetTerrainMultiplier(lordParty);

            float consumption = baseRate * sizeMultiplier * activityMult * terrainMult;

            ModLogger.Debug(LogCategory,
                $"Consumption calc: base={baseRate}, size={sizeMultiplier:F2} (troops={partySize}), " +
                $"activity={activityMult:F1}, terrain={terrainMult:F1} => {consumption:F2}%");

            return consumption;
        }

        /// <summary>
        /// Determines activity multiplier for non-food consumption.
        /// Higher multipliers for intense activities (siege, raiding).
        /// </summary>
        private float GetActivityMultiplier(MobileParty party)
        {
            if (party == null)
            {
                return 1.0f;
            }

            // Check siege states first (before general settlement check)
            // Active siege = high consumption (siege equipment, repairs)
            if (party.BesiegerCamp != null)
            {
                return 2.5f;
            }

            // In settlement - check if being besieged first
            if (party.CurrentSettlement != null)
            {
                // Being besieged = elevated consumption (defending, rationing)
                if (party.CurrentSettlement.SiegeEvent != null)
                {
                    return 1.8f;
                }

                // Normal settlement rest = minimal consumption
                return 0.3f;
            }

            // In combat = extra consumption handled by battle events, not daily tick
            if (party.Party?.MapEvent != null)
            {
                return 1.5f;
            }

            // Patrolling or raiding behavior - check short-term AI
            var behavior = party.ShortTermBehavior;
            if (behavior == AiBehavior.RaidSettlement)
            {
                return 1.5f;
            }
            if (behavior == AiBehavior.PatrolAroundPoint)
            {
                return 1.2f;
            }

            // Default traveling
            return 1.0f;
        }

        /// <summary>
        /// Determines terrain multiplier for non-food consumption.
        /// Harsh terrain (desert, mountains, snow) increases wear on equipment.
        /// </summary>
        private float GetTerrainMultiplier(MobileParty party)
        {
            if (party == null || !party.IsActive)
            {
                return 1.0f;
            }

            try
            {
                // Safety check for campaign state
                if (Campaign.Current?.MapSceneWrapper == null)
                {
                    return 1.0f;
                }

                var navFace = party.CurrentNavigationFace;
                if (!navFace.IsValid())
                {
                    return 1.0f;
                }

                TerrainType terrain = Campaign.Current.MapSceneWrapper.GetFaceTerrainType(navFace);

                return terrain switch
                {
                    TerrainType.Desert => 1.2f,   // Sand damages equipment
                    TerrainType.Mountain => 1.3f, // Rough terrain wear
                    TerrainType.Snow => 1.4f,     // Cold damages gear, extra fuel needed
                    TerrainType.Forest => 1.0f,   // Standard
                    TerrainType.Plain => 1.0f,    // Standard
                    TerrainType.Steppe => 1.0f,   // Standard
                    TerrainType.Fording => 1.1f,  // Water damage
                    TerrainType.Lake => 1.1f,     // Water damage
                    TerrainType.Water => 1.1f,    // Naval/water damage
                    _ => 1.0f
                };
            }
            catch (Exception ex)
            {
                ModLogger.Debug(LogCategory, $"Error getting terrain type: {ex.Message}");
                return 1.0f;
            }
        }

        /// <summary>
        /// Calculates daily non-food resupply when in a settlement.
        /// Base rate is +3% per day in towns/castles, with bonuses for prosperity and ownership.
        /// </summary>
        private float CalculateNonFoodResupply()
        {
            MobileParty lordParty = GetLordParty();
            if (lordParty?.CurrentSettlement == null)
            {
                return 0f;
            }

            Settlement settlement = lordParty.CurrentSettlement;

            // Only resupply in towns and castles (not villages)
            if (!settlement.IsTown && !settlement.IsCastle)
            {
                return 0f;
            }

            // Base resupply: +3% per day
            float resupply = 3.0f;

            // Wealthy settlement bonus: +1% if prosperity > 5000 (Town has Prosperity property)
            float prosperity = settlement.Town?.Prosperity ?? 0f;
            if (prosperity > 5000)
            {
                resupply += 1.0f;
            }

            // Owned settlement bonus: +1% if lord owns the settlement
            if (settlement.OwnerClan == lordParty.LeaderHero?.Clan)
            {
                resupply += 1.0f;
            }

            ModLogger.Debug(LogCategory,
                $"Resupply in {settlement.Name}: base=3, prosperity={prosperity:F0} ({(prosperity > 5000 ? "+1" : "0")}), " +
                $"owned={settlement.OwnerClan == lordParty.LeaderHero?.Clan} => {resupply:F1}%");

            return resupply;
        }

        /// <summary>
        /// Applies supply changes after a battle based on casualties and outcome.
        /// Called from EnlistmentBehavior.OnPlayerBattleEnd or OnMapEventEnded.
        /// </summary>
        /// <param name="troopsLost">Number of friendly troops lost in battle.</param>
        /// <param name="enemiesKilled">Number of enemy troops killed.</param>
        /// <param name="playerWon">Whether the player's side won the battle.</param>
        /// <param name="wasSiege">Whether this was a siege assault.</param>
        public void ProcessBattleSupplyChanges(int troopsLost, int enemiesKilled, bool playerWon, bool wasSiege)
        {
            try
            {
                float oldNonFood = _nonFoodSupply;

                // Casualty loss: -1% per 10 troops lost (supplies abandoned/consumed treating wounded)
                float casualtyLoss = troopsLost / 10f;

                // Defeat penalty: -5% for lost battles (abandoned supplies during retreat)
                float defeatPenalty = playerWon ? 0f : 5f;

                // Siege assault penalty: -3% for siege equipment usage
                float siegePenalty = wasSiege ? 3f : 0f;

                // Total loss
                float totalLoss = casualtyLoss + defeatPenalty + siegePenalty;

                // Battle loot: +1% per 25 enemies killed (salvaged supplies from enemy)
                float lootGain = CalculateBattleLoot(enemiesKilled);

                // Apply changes
                _nonFoodSupply = MathF.Clamp(_nonFoodSupply - totalLoss + lootGain, 0f, 60f);

                float netChange = _nonFoodSupply - oldNonFood;

                ModLogger.Info(LogCategory,
                    $"Battle supply: casualties={troopsLost} (-{casualtyLoss:F1}%), " +
                    $"defeat={!playerWon} (-{defeatPenalty:F0}%), siege={wasSiege} (-{siegePenalty:F0}%), " +
                    $"loot={enemiesKilled} kills (+{lootGain:F1}%) => net {netChange:+0.0;-0.0}%, now {_nonFoodSupply:F1}%");
            }
            catch (Exception ex)
            {
                ModLogger.Error(LogCategory, "Error processing battle supply changes", ex);
            }
        }

        /// <summary>
        /// Calculates supply loot from defeated enemies.
        /// Base is +1% per 25 kills, modified by enemy faction quality.
        /// </summary>
        private float CalculateBattleLoot(int enemiesKilled)
        {
            if (enemiesKilled <= 0)
            {
                return 0f;
            }

            // Base loot: 1% per 25 enemies killed
            float baseLoot = enemiesKilled / 25f;

            // Faction multiplier could be added here based on MapEvent data
            // For now, use a standard 1.0x multiplier
            float factionMult = 1.0f;

            return MathF.Min(baseLoot * factionMult, 6f); // Cap at 6% per battle
        }

        /// <summary>
        /// Adds non-food supplies from player actions (foraging duty, purchases, etc.).
        /// </summary>
        /// <param name="amount">Percentage to add (0-60 range clamped).</param>
        /// <param name="source">Description of the source for logging.</param>
        public void AddNonFoodSupplies(float amount, string source)
        {
            if (amount <= 0)
            {
                return;
            }

            float oldSupply = _nonFoodSupply;
            _nonFoodSupply = MathF.Clamp(_nonFoodSupply + amount, 0f, 60f);
            float actualGain = _nonFoodSupply - oldSupply;

            if (actualGain > 0)
            {
                ModLogger.Info(LogCategory, $"Added {actualGain:F1}% non-food supplies from {source}. Now: {_nonFoodSupply:F1}%");
            }
        }

        /// <summary>
        /// Gets the lord's mobile party for observation.
        /// Returns null if lord has no party or is not available.
        /// Note: Uses EnlistedLord directly (not IsEnlisted) so we track supply even during leave.
        /// </summary>
        private MobileParty GetLordParty()
        {
            try
            {
                // First try to get from EnlistmentBehavior for most current state
                // Note: We check EnlistedLord directly, not IsEnlisted, because IsEnlisted
                // returns false during leave but we still want to observe the lord's party
                var behavior = EnlistmentBehavior.Instance;
                var lord = behavior?.EnlistedLord ?? _enlistedLord;

                if (lord == null)
                {
                    return null;
                }

                // Verify lord is still alive and has a party
                if (!lord.IsAlive)
                {
                    return null;
                }

                var party = lord.PartyBelongedTo;

                // Verify party is valid
                if (party == null || !party.IsActive)
                {
                    return null;
                }

                return party;
            }
            catch (Exception ex)
            {
                ModLogger.Debug(LogCategory, $"Error getting lord party: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Sets the non-food supply value directly. Used for save/load restoration.
        /// </summary>
        internal void SetNonFoodSupply(float value)
        {
            _nonFoodSupply = MathF.Clamp(value, 0f, 60f);
        }

        /// <summary>
        /// Updates the enlisted lord reference. Called when lord changes.
        /// </summary>
        internal void SetEnlistedLord(Hero lord)
        {
            _enlistedLord = lord;
        }
    }
}

