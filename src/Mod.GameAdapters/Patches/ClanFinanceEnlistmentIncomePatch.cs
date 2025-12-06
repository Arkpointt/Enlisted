using System;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Mod.Core.Logging;
using Enlisted.Mod.Core;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace Enlisted.Mod.GameAdapters.Patches
{
    /// <summary>
    ///     Helper class to cache tooltip labels for wage breakdown display.
    /// </summary>
    internal static class WageTooltipLabels
    {
        public static TextObject BasePay;
        public static TextObject LevelBonus;
        public static TextObject TierBonus;
        public static TextObject ServiceBonus;
        public static TextObject ArmyBonus;
        public static TextObject DutyBonus;
        public static TextObject WorkshopIncome;

        public static void EnsureInitialized()
        {
            if (BasePay == null)
            {
                BasePay = new TextObject("{=enlisted_base_pay}Soldier's Pay");
                LevelBonus = new TextObject("{=enlisted_level_bonus}Combat Exp");
                TierBonus = new TextObject("{=enlisted_tier_bonus}Rank Pay");
                ServiceBonus = new TextObject("{=enlisted_service_bonus}Service Seniority");
                ArmyBonus = new TextObject("{=enlisted_army_bonus}Army Campaign Bonus");
                DutyBonus = new TextObject("{=enlisted_duty_bonus}Duty Assignment");
                WorkshopIncome = new TextObject("{=enlisted_workshop_income}Workshop Income");
            }
        }

        public static void ClearCache()
        {
            BasePay = null;
            LevelBonus = null;
            TierBonus = null;
            ServiceBonus = null;
            ArmyBonus = null;
            DutyBonus = null;
            WorkshopIncome = null;
        }
    }

    /// <summary>
    ///     Completely isolates the player clan's income calculation when enlisted.
    ///     When enlisted, the player should ONLY see their enlistment wages, not any
    ///     settlement income, party income, or other clan-based income that might leak
    ///     from the lord's/army's finances due to party attachment.
    ///     Shows a detailed breakdown of wage components in the tooltip.
    ///     Workshop income is processed separately as it's a personal asset that should
    ///     continue generating income regardless of enlistment status.
    /// </summary>
    // ReSharper disable once UnusedType.Local - Harmony patch class discovered via reflection
    [System.Diagnostics.CodeAnalysis.SuppressMessage("ReSharper", "UnusedType.Local", Justification = "Harmony patch class discovered via reflection")]
    [HarmonyPatch(typeof(DefaultClanFinanceModel), nameof(DefaultClanFinanceModel.CalculateClanIncome))]
    internal static class ClanFinanceEnlistmentIncomePatch
    {
        private static bool _hasLoggedFirstWage;
        private static bool _hasLoggedFirstWorkshop;

        /// <summary>
        ///     PREFIX: Intercept income calculation BEFORE native code runs.
        ///     Matches signature: public override ExplainedNumber CalculateClanIncome(Clan clan, bool includeDescriptions = false,
        ///     bool applyWithdrawals = false, bool includeDetails = false)
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Called by Harmony via reflection")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "Parameters required to match Harmony patch signature")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Harmony convention: __result is a special injected parameter")]
        private static bool Prefix(Clan clan, bool includeDescriptions, bool applyWithdrawals, bool includeDetails,
            ref ExplainedNumber __result)
        {
            try
            {
                if (!EnlistedActivation.EnsureActive())
                {
                    return true;
                }

                // Skip during character creation
                if (Campaign.Current == null || Clan.PlayerClan == null)
                {
                    return true; // Run native
                }

                // Only intercept for player clan
                if (clan != Clan.PlayerClan)
                {
                    return true; // Run native for other clans
                }

                var enlistment = EnlistmentBehavior.Instance;
                var isEnlisted = enlistment?.IsEnlisted == true;
                var isInGracePeriod = enlistment?.IsInDesertionGracePeriod == true;
                var mainHero = Hero.MainHero;
                var playerCaptured = mainHero?.IsPrisoner == true;

                // Only isolate finances when actively enlisted (not on leave) or captured
                if (!isEnlisted && !isInGracePeriod && !playerCaptured)
                {
                    return true; // Run native when not enlisted
                }

                // CRITICAL: Create a fresh ExplainedNumber with ONLY enlistment wages
                // If captured or in grace period, this starts at 0 and stays at 0 (no wages added)
                __result = new ExplainedNumber(0f, includeDescriptions);

                // Add detailed wage breakdown when actively enlisted
                if (isEnlisted)
                {
                    AddWageBreakdownToResult(enlistment, ref __result);
                }

                // Always process workshop income - workshops are personal assets that should
                // generate income regardless of enlistment or prisoner status
                AddWorkshopIncomeToResult(mainHero, ref __result, applyWithdrawals);

                return false; // Skip native
            }
            catch (Exception ex)
            {
                ModLogger.Error("Finance", $"Error in income isolation prefix: {ex.Message}");
                return true; // Fail open
            }
        }

        /// <summary>
        ///     Calculates and adds workshop income to the result.
        ///     When applyWithdrawals is true, also withdraws profits from workshop capital
        ///     to prevent accumulation/buffering of workshop income.
        ///     This mirrors the native CalculateHeroIncomeFromWorkshops logic but works
        ///     within our isolated income calculation.
        /// </summary>
        private static void AddWorkshopIncomeToResult(Hero hero, ref ExplainedNumber result, bool applyWithdrawals)
        {
            if (hero == null)
            {
                return;
            }

            var ownedWorkshops = hero.OwnedWorkshops;
            if (ownedWorkshops == null || ownedWorkshops.Count == 0)
            {
                return;
            }

            var totalWorkshopIncome = 0;

            foreach (var workshop in ownedWorkshops)
            {
                // Calculate income using the native model method
                var incomeFromWorkshop =
                    Campaign.Current.Models.ClanFinanceModel.CalculateOwnerIncomeFromWorkshop(workshop);
                totalWorkshopIncome += incomeFromWorkshop;

                // When applyWithdrawals is true, withdraw from workshop capital to player
                // This is the critical fix - prevents workshop income from buffering
                if (applyWithdrawals && incomeFromWorkshop > 0)
                {
                    workshop.ChangeGold(-incomeFromWorkshop);
                    // Fire the event so other systems know the player earned gold from workshop
                    CampaignEventDispatcher.Instance.OnPlayerEarnedGoldFromAsset(
                        DefaultClanFinanceModel.AssetIncomeType.Workshop, incomeFromWorkshop);
                }
            }

            // Add workshop income to the tooltip as a separate line item
            if (totalWorkshopIncome > 0)
            {
                WageTooltipLabels.EnsureInitialized();
                result.Add(totalWorkshopIncome, WageTooltipLabels.WorkshopIncome);

                if (!_hasLoggedFirstWorkshop)
                {
                    _hasLoggedFirstWorkshop = true;
                    ModLogger.Info("Finance",
                        $"Workshop income processed while enlisted: {totalWorkshopIncome} from {ownedWorkshops.Count} workshop(s)");
                }
            }
        }

        /// <summary>
        ///     Adds each wage component as a separate line in the tooltip.
        /// </summary>
        internal static void AddWageBreakdownToResult(EnlistmentBehavior enlistment, ref ExplainedNumber result)
        {
            var breakdown = enlistment.GetWageBreakdown();
            if (breakdown.Total <= 0)
            {
                return;
            }

            WageTooltipLabels.EnsureInitialized();

            // Add each component as a separate tooltip line
            if (breakdown.BasePay > 0)
            {
                result.Add(breakdown.BasePay, WageTooltipLabels.BasePay);
            }

            if (breakdown.LevelBonus > 0)
            {
                result.Add(breakdown.LevelBonus, WageTooltipLabels.LevelBonus);
            }

            if (breakdown.TierBonus > 0)
            {
                result.Add(breakdown.TierBonus, WageTooltipLabels.TierBonus);
            }

            if (breakdown.ServiceBonus > 0)
            {
                result.Add(breakdown.ServiceBonus, WageTooltipLabels.ServiceBonus);
            }

            if (breakdown.ArmyBonus > 0)
            {
                result.Add(breakdown.ArmyBonus, WageTooltipLabels.ArmyBonus);
            }

            if (breakdown.DutyBonus > 0)
            {
                // Show duty name if available
                if (!string.IsNullOrEmpty(breakdown.ActiveDuty))
                {
                    var dutyLabel = new TextObject("{=enlisted_duty_bonus_named}{DUTY} Bonus");
                    dutyLabel.SetTextVariable("DUTY", breakdown.ActiveDuty);
                    result.Add(breakdown.DutyBonus, dutyLabel);
                }
                else
                {
                    result.Add(breakdown.DutyBonus, WageTooltipLabels.DutyBonus);
                }
            }

            if (!_hasLoggedFirstWage)
            {
                _hasLoggedFirstWage = true;
                ModLogger.Info("Finance",
                    $"Wage breakdown - Base:{breakdown.BasePay} Level:{breakdown.LevelBonus} Tier:{breakdown.TierBonus} Service:{breakdown.ServiceBonus} Army:{breakdown.ArmyBonus} Duty:{breakdown.DutyBonus} = {breakdown.Total}");
            }
        }

        /// <summary>
        ///     Clear cached tooltip labels when config is reloaded.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "May be called for configuration reloading")]
        internal static void ClearCache()
        {
            WageTooltipLabels.ClearCache();
        }
    }

    /// <summary>
    ///     Completely isolates the player clan's expense calculation when enlisted.
    ///     When enlisted, the player should have ZERO expenses - the lord pays for everything.
    ///     This prevents any army/party/garrison expenses from appearing in the player's finances.
    /// </summary>
    // ReSharper disable once UnusedType.Local - Harmony patch class discovered via reflection
    [System.Diagnostics.CodeAnalysis.SuppressMessage("ReSharper", "UnusedType.Local", Justification = "Harmony patch class discovered via reflection")]
    [HarmonyPatch(typeof(DefaultClanFinanceModel), nameof(DefaultClanFinanceModel.CalculateClanExpenses))]
    internal static class ClanFinanceEnlistmentExpensePatch
    {
        private static bool _hasLoggedFirst;

        /// <summary>
        ///     PREFIX: Intercept expense calculation BEFORE native code runs.
        ///     Matches signature: public override ExplainedNumber CalculateClanExpenses(Clan clan, bool includeDescriptions =
        ///     false, bool applyWithdrawals = false, bool includeDetails = false)
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Called by Harmony via reflection")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "Parameters required to match Harmony patch signature")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Harmony convention: __result is a special injected parameter")]
        private static bool Prefix(Clan clan, bool includeDescriptions, bool applyWithdrawals, bool includeDetails,
            ref ExplainedNumber __result)
        {
            try
            {
                if (Campaign.Current == null || Clan.PlayerClan == null)
                {
                    return true;
                }

                if (clan != Clan.PlayerClan)
                {
                    return true;
                }

                var enlistment = EnlistmentBehavior.Instance;
                var isEnlisted = enlistment?.IsEnlisted == true;
                var isInGracePeriod = enlistment?.IsInDesertionGracePeriod == true;
                var mainHero = Hero.MainHero;
                var playerCaptured = mainHero?.IsPrisoner == true;

                // Isolate expenses when enlisted, in grace period, or captured
                if (!isEnlisted && !isInGracePeriod && !playerCaptured)
                {
                    return true;
                }

                // CRITICAL: Return zero expenses when enlisted
                __result = new ExplainedNumber(0f, includeDescriptions);

                if (!_hasLoggedFirst)
                {
                    _hasLoggedFirst = true;
                    ModLogger.Info("Finance", "Expenses isolated - showing zero expenses while enlisted");
                }

                return false; // Skip native
            }
            catch (Exception ex)
            {
                ModLogger.Error("Finance", $"Error in expense isolation prefix: {ex.Message}");
                return true;
            }
        }
    }

    /// <summary>
    ///     Forces the daily gold change calculation to use the public CalculateClanIncome/Expenses methods
    ///     instead of the private Internal methods. This is required because:
    ///     1. Native CalculateClanGoldChange bypasses public overrides (calling Internal directly), which ignores our
    ///     Income/Expense patches.
    ///     2. We need to combine Income + Expenses manually to preserve the detailed wage breakdown tooltips from Income.
    ///     This patch ensures both functionality (wage appears) and compatibility (other mods patching Income/Expenses will
    ///     work).
    /// </summary>
    // ReSharper disable once UnusedType.Local - Harmony patch class discovered via reflection
    [System.Diagnostics.CodeAnalysis.SuppressMessage("ReSharper", "UnusedType.Local", Justification = "Harmony patch class discovered via reflection")]
    [HarmonyPatch(typeof(DefaultClanFinanceModel), nameof(DefaultClanFinanceModel.CalculateClanGoldChange))]
    internal static class ClanFinanceEnlistmentGoldChangePatch
    {
        private static bool _hasLoggedFirst;
        private static TextObject _expensesText;

        private static void EnsureInitialized()
        {
            if (_expensesText == null)
            {
                _expensesText = GameTexts.FindText("str_expenses");
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Called by Harmony via reflection")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "Parameters required to match Harmony patch signature")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Harmony convention: __instance and __result are special injected parameters")]
        private static bool Prefix(DefaultClanFinanceModel __instance, Clan clan, bool includeDescriptions,
            bool applyWithdrawals, bool includeDetails, ref ExplainedNumber __result)
        {
            try
            {
                if (Campaign.Current == null || Clan.PlayerClan == null)
                {
                    return true;
                }

                if (clan != Clan.PlayerClan)
                {
                    return true;
                }

                var enlistment = EnlistmentBehavior.Instance;
                var isEnlisted = enlistment?.IsEnlisted == true;

                // Only redirect when enlisted. When not enlisted, use native logic.
                if (!isEnlisted)
                {
                    return true;
                }

                // 1. Calculate Income using the PUBLIC method (triggers our patch and others)
                // This returns an ExplainedNumber with the full wage breakdown (if enlisted)
                var income =
                    __instance.CalculateClanIncome(clan, includeDescriptions, applyWithdrawals, includeDetails);

                // 2. Calculate Expenses using the PUBLIC method (triggers our patch and others)
                // This returns 0 (if enlisted)
                var expenses =
                    __instance.CalculateClanExpenses(clan, includeDescriptions, applyWithdrawals, includeDetails);

                // 3. Combine them
                // We start with Income to preserve its tooltips.
                __result = income;

                // Add expenses (if any) to the result.
                if (Math.Abs(expenses.ResultNumber) > 0.001f)
                {
                    EnsureInitialized();
                    __result.Add(expenses.ResultNumber, _expensesText);
                }

                if (!_hasLoggedFirst)
                {
                    _hasLoggedFirst = true;
                    ModLogger.Info("Finance",
                        $"GoldChange redirected - Income:{income.ResultNumber} Expenses:{expenses.ResultNumber}");
                }

                return false; // Skip native implementation (which calls Internal)
            }
            catch (Exception ex)
            {
                ModLogger.Error("Finance", $"Error in GoldChange redirection: {ex.Message}");
                return true; // Fail open
            }
        }
    }
}
