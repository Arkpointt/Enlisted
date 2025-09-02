using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.Localization;
using Enlisted.Features.Enlistment.Behaviors;

namespace Enlisted.Features.Conversations.Behaviors
{
	/// <summary>
	/// Phase 2 dialog behavior. Adds the "join army" entry point under hero_main_options.
	/// </summary>
	public sealed class LordDialogBehavior : CampaignBehaviorBase
	{
		public override void RegisterEvents()
		{
			CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
		}

		public override void SyncData(IDataStore dataStore)
		{
			// No persistent data yet.
		}

		private void OnSessionLaunched(CampaignGameStarter starter)
		{
			AddDialogs(starter);
		}

		private void AddDialogs(CampaignGameStarter starter)
		{
			// Enlist flow (party-only)
			starter.AddPlayerLine(
				"enlisted_enlist_party",
				"hero_main_options",
				"enlisted_enlist_party_query",
				EnlistPrompt().ToString(),
				EnlistCondition,
				JoinArmyConsequence,
				110);

			starter.AddDialogLine(
				"enlisted_enlist_party_confirm",
				"enlisted_enlist_party_query",
				"enlisted_enlist_party_choice",
				ConfirmResponse().ToString(),
				null,
				null,
				110);

			starter.AddPlayerLine(
				"enlisted_enlist_party_accept",
				"enlisted_enlist_party_choice",
				"close_window",
				AcceptResponse().ToString(),
				enlist_accept_condition,
				OnAcceptConsequence,
				110);

			starter.AddPlayerLine(
				"enlisted_enlist_party_decline",
				"enlisted_enlist_party_choice",
				"hero_main_options",
				DeclineResponse().ToString(),
				null,
				null,
				110);

			// Retire flow
			starter.AddPlayerLine(
				"enlisted_retire",
				"hero_main_options",
				"close_window",
				RetirePrompt().ToString(),
				RetireCondition,
				OnRetireConsequence,
				110);
		}

		private bool EnlistCondition()
		{
			var lord = Hero.OneToOneConversationHero;
			if (lord == null || !lord.IsLord)
			{
				return false;
			}
			TextObject reason;
			return EnlistmentBehavior.Instance != null && EnlistmentBehavior.Instance.CanEnlistWithParty(lord, out reason);
		}
		private bool enlist_accept_condition() => true;

		private void JoinArmyConsequence()
		{
			// No direct action here; the accept line will call OnAcceptConsequence.
		}

		private void OnAcceptConsequence()
		{
			var lord = Hero.OneToOneConversationHero;
			if (lord == null)
			{
				return;
			}
			EnlistmentBehavior.Instance?.StartEnlist(lord);
		}

		private bool RetireCondition()
		{
			return EnlistmentBehavior.Instance != null && EnlistmentBehavior.Instance.IsEnlisted;
		}

		private void OnRetireConsequence()
		{
			EnlistmentBehavior.Instance?.StopEnlist("Retired by player");
		}

		private static TextObject EnlistPrompt() => new TextObject("I wish to serve in your warband.");
		private static TextObject ConfirmResponse() => new TextObject("Very well. Keep pace and heed my orders.");
		private static TextObject AcceptResponse() => new TextObject("We march together.");
		private static TextObject DeclineResponse() => new TextObject("Another time, perhaps.");
		private static TextObject RetirePrompt() => new TextObject("I'd like to retire from your service.");
	}
}


