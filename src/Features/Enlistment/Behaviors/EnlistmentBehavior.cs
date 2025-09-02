using System;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Localization;

namespace Enlisted.Features.Enlistment.Behaviors
{
	/// <summary>
	/// Party-first enlistment behavior: attach to a lord's party and mirror/follow.
	/// No army membership is read or modified by this behavior.
	/// </summary>
	public sealed class EnlistmentBehavior : CampaignBehaviorBase
	{
		public static EnlistmentBehavior Instance { get; private set; }

		private Hero _enlistedLord;
		public bool IsEnlisted => _enlistedLord != null;
		public Hero CurrentLord => _enlistedLord;

		public EnlistmentBehavior()
		{
			// Singleton-style access for dialog behaviors.
			Instance = this;
		}

		public override void RegisterEvents()
		{
			// Maintain escort/visibility while enlisted.
			CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, OnHourlyTick);
		}

		public override void SyncData(IDataStore dataStore)
		{
			// No persistent data in Phase 2.
		}

		public bool CanEnlistWithParty(Hero lord, out TextObject reason)
		{
			reason = TextObject.Empty;
			if (IsEnlisted)
			{
				reason = new TextObject("You are already in service.");
				return false;
			}
			if (lord == null || !lord.IsLord)
			{
				reason = new TextObject("We must speak to a noble to enlist.");
				return false;
			}
			var main = MobileParty.MainParty;
			if (main == null)
			{
				reason = new TextObject("No main party found.");
				return false;
			}
			var counterpartParty = MobileParty.ConversationParty ?? lord.PartyBelongedTo;
			if (counterpartParty == null)
			{
				reason = new TextObject("The lord has no party at present.");
				return false;
			}
			return true;
		}

		public void StartEnlist(Hero lord)
		{
			if (lord == null)
			{
				return;
			}
			_enlistedLord = lord;
			EncounterGuard.TryAttachOrEscort(lord);
			var main = MobileParty.MainParty;
			if (main != null)
			{
				main.IsVisible = false;
				TrySetShouldJoinPlayerBattles(main, true);
			}
		}

		public void StopEnlist(string reason)
		{
			var main = MobileParty.MainParty;
			if (main != null)
			{
				main.IsVisible = true;
				TrySetShouldJoinPlayerBattles(main, false);
				TryReleaseEscort(main);
			}
			_enlistedLord = null;
		}

		private void OnHourlyTick()
		{
			var main = MobileParty.MainParty;
			if (main == null)
			{
				return;
			}
			if (!IsEnlisted)
			{
				return;
			}

			var counterpartParty = _enlistedLord?.PartyBelongedTo;
			if (counterpartParty == null)
			{
				// Auto-stop if target party became invalid.
				StopEnlist("Target party invalid");
				return;
			}

			// Maintain attach/escort and stealth while enlisted.
			EncounterGuard.TryAttachOrEscort(_enlistedLord);
			main.IsVisible = false;
			TrySetShouldJoinPlayerBattles(main, true);
			main.IgnoreByOtherPartiesTill(CampaignTime.Now + CampaignTime.Hours(0.5f));
		}

		internal static void ApplyEscort(Hero lord)
		{
			var main = MobileParty.MainParty;
			var counterpartParty = MobileParty.ConversationParty ?? lord?.PartyBelongedTo;
			if (main == null || counterpartParty == null)
			{
				return;
			}
			main.Ai.SetMoveEscortParty(counterpartParty);
		}

		internal static void TryReleaseEscort(MobileParty main)
		{
			try
			{
				var ai = main.Ai;
				var hold = ai.GetType().GetMethod("SetMoveModeHold", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
				hold?.Invoke(ai, null);
			}
			catch
			{
				// Best-effort: if we can't find a clear/hold method, do nothing.
			}
		}

		private static void TrySetShouldJoinPlayerBattles(MobileParty party, bool value)
		{
			try
			{
				var prop = party.GetType().GetProperty("ShouldJoinPlayerBattles", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				prop?.SetValue(party, value, null);
			}
			catch
			{
				// Best-effort: property may not exist in some versions; ignore failures.
			}
		}
	}
}


