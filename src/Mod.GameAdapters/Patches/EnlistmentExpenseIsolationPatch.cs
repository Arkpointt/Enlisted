using System;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using Enlisted.Features.Enlistment.Behaviors;
using Enlisted.Mod.Core.Logging;

// ExplainedNumber is in TaleWorlds.CampaignSystem namespace, not TaleWorlds.Localization
// Confirmed from decompiled DefaultClanFinanceModel.cs line 655:
// private int AddPartyExpense(MobileParty party, Clan clan, ExplainedNumber goldChange, bool applyWithdrawals)

namespace Enlisted.Mod.GameAdapters.Patches
{
	/// <summary>
	/// Prevents expense sharing when the player's party is attached to a lord's party during enlistment.
	/// This allows natural attachment behavior (AttachedTo) while isolating financial expenses.
	/// When enlisted, the player receives wages from the enlistment system and doesn't pay expenses
	/// based on the lord's financial situation.
	/// </summary>
	[HarmonyPatch]
	public class EnlistmentExpenseIsolationPatch
	{
		private static bool _loggedFirstInvocation;

		/// <summary>
		/// Finds the AddPartyExpense method in DefaultClanFinanceModel to patch.
		/// Uses reflection because the method is private.
		/// </summary>
		static MethodBase TargetMethod()
		{
			try
			{
				// Find DefaultClanFinanceModel class
				var modelType = AccessTools.TypeByName("TaleWorlds.CampaignSystem.GameComponents.DefaultClanFinanceModel");
				if (modelType == null)
				{
					ModLogger.Error("ExpenseIsolation", "Could not find DefaultClanFinanceModel type");
					return null;
				}

				// Find ExplainedNumber type from TaleWorlds.CampaignSystem
				// ExplainedNumber is a struct in TaleWorlds.CampaignSystem namespace
				// Based on decompiled DefaultClanFinanceModel.AddPartyExpense signature:
				// private int AddPartyExpense(MobileParty party, Clan clan, ExplainedNumber goldChange, bool applyWithdrawals)
				var explainedNumberType = AccessTools.TypeByName("TaleWorlds.CampaignSystem.ExplainedNumber");
				if (explainedNumberType == null)
				{
					// Fallback: Try to find in CampaignSystem assembly
					explainedNumberType = typeof(Clan).Assembly.GetType("TaleWorlds.CampaignSystem.ExplainedNumber");
				}

				if (explainedNumberType != null)
				{
					// Find the AddPartyExpense method with signature:
					// private int AddPartyExpense(MobileParty party, Clan clan, ExplainedNumber goldChange, bool applyWithdrawals)
					var method = AccessTools.Method(modelType, "AddPartyExpense", 
						new[] { typeof(MobileParty), typeof(Clan), explainedNumberType, typeof(bool) });
					if (method != null)
					{
						ModLogger.Info("ExpenseIsolation", "Found AddPartyExpense method for patching");
						return method;
					}
				}

				// Fallback: Try to find by name only (less reliable but more compatible)
				var allMethods = AccessTools.GetDeclaredMethods(modelType);
				foreach (var method in allMethods)
				{
					if (method.Name == "AddPartyExpense" && method.GetParameters().Length == 4)
					{
						var parameters = method.GetParameters();
						if (parameters[0].ParameterType == typeof(MobileParty) &&
						    parameters[1].ParameterType == typeof(Clan) &&
						    parameters[3].ParameterType == typeof(bool))
						{
							ModLogger.Info("ExpenseIsolation", "Found AddPartyExpense method (by name) for patching");
							return method;
						}
					}
				}

				ModLogger.Error("ExpenseIsolation", "Could not find AddPartyExpense method to patch");
				return null;
			}
			catch (Exception ex)
			{
				ModLogger.Error("ExpenseIsolation", $"Exception finding AddPartyExpense method: {ex.Message}");
				return null;
			}
		}

		/// <summary>
		/// Prefix that intercepts party expense calculations and isolates expenses for enlisted players.
		/// If the player is enlisted and attached to a lord's party, returns 0 expenses to prevent
		/// the lord's financial situation from affecting the player's expenses.
		/// </summary>
		/// <param name="party">The mobile party to calculate expenses for.</param>
		/// <param name="clan">The clan that owns the party.</param>
		/// <param name="goldChange">The explained number tracking gold changes (ExplainedNumber type).</param>
		/// <param name="applyWithdrawals">Whether to actually withdraw gold or just calculate.</param>
		/// <param name="__result">The result expense amount to return.</param>
		static bool Prefix(MobileParty party, Clan clan, object goldChange, bool applyWithdrawals, ref int __result)
		{
			try
			{
				// Only intercept for the player's main party
				if (party != MobileParty.MainParty)
				{
					return true; // Allow normal expense calculation for other parties
				}

				var enlistment = EnlistmentBehavior.Instance;
				bool isEnlisted = enlistment?.IsEnlisted == true;
				bool isInGracePeriod = enlistment?.IsInDesertionGracePeriod == true;
				var attachedTo = party.AttachedTo;
				var mainParty = MobileParty.MainParty;
				var lordParty = enlistment?.CurrentLord?.PartyBelongedTo;
				bool sameArmy = mainParty?.Army != null && lordParty?.Army != null && mainParty.Army == lordParty.Army;
				bool playerCaptured = Hero.MainHero?.IsPrisoner == true;
				
				if (!_loggedFirstInvocation)
				{
					_loggedFirstInvocation = true;
					ModLogger.Info("ExpenseIsolation", $"Patch active - AddPartyExpense intercepted (applyWithdrawals={applyWithdrawals})");
				}

				// Always isolate expenses when player is captured - they shouldn't pay captor's army expenses
				if (playerCaptured)
				{
					__result = 0;
					ModLogger.Debug("ExpenseIsolation", "Isolated player expenses - player is captured");
					return false;
				}

				ModLogger.Debug("ExpenseIsolation",
					$"Expense check - enlisted={isEnlisted}, grace={isInGracePeriod}, attachedTo={(attachedTo?.LeaderHero?.Name?.ToString() ?? "null")}, lordParty={(lordParty?.LeaderHero?.Name?.ToString() ?? "null")}, inSameArmy={sameArmy}, applyWithdrawals={applyWithdrawals}");
				
				if ((isEnlisted || isInGracePeriod) && (sameArmy || (attachedTo != null && attachedTo == lordParty)))
				{
					// Isolate expenses - return 0 to prevent expense sharing
					__result = 0;
					ModLogger.Debug("ExpenseIsolation", "Isolated player expenses from lord's party");
					return false;
				}
				
				ModLogger.Debug("ExpenseIsolation", "Allowed normal expense calculation (not embedded with lord)");
				return true; // Allow normal expense calculation
			}
			catch (Exception ex)
			{
				ModLogger.Error("ExpenseIsolation", $"Error in expense isolation patch: {ex.Message}");
				return true; // Fail open - allow normal behavior on error
			}
		}
	}
}

