using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Overlay;
using TaleWorlds.CampaignSystem.SandBox.CampaignBehaviors;

namespace MBFastDialogue.CampaignSystem.CampaignBehaviors
{
	public class FastDialogueCampaignBehaviorBase : EncounterGameMenuBehavior
	{
		private EncounterGameMenuBehavior GetGlobalCampaignBehaviorManager() => Campaign.Current.GetCampaignBehavior<EncounterGameMenuBehavior>();
		private void Init(MenuCallbackArgs args) =>
			 ReflectionUtils.ForceCall<object>(GetGlobalCampaignBehaviorManager(), "game_menu_encounter_on_init", new object[] { args });
		private GameMenuOption.OnConditionDelegate ConditionOf(string name) =>
			(MenuCallbackArgs args) => ReflectionUtils.ForceCall<bool>(GetGlobalCampaignBehaviorManager(), name, new object[] { args });
		private GameMenuOption.OnConsequenceDelegate ConsequenceOf(string name) =>
			(MenuCallbackArgs args) => ReflectionUtils.ForceCall<object>(GetGlobalCampaignBehaviorManager(), name, new object[] { args });


		public override void RegisterEvents()
		{
			CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
		}
		private void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
		{
			campaignGameStarter.AddGameMenu(
				"fast_encounter",
				"{=!}.",
				Init,
				GameOverlays.MenuOverlayType.None,
				GameMenu.MenuFlags.none,
				null);
			campaignGameStarter.AddGameMenuOption(
				"fast_encounter",
				"fast_encounter_attack",
				"{=o1pZHZOF}Attack!",
				ConditionOf("game_menu_encounter_attack_on_condition"),
				ConsequenceOf("game_menu_encounter_attack_on_consequence"),
				false,
				-1,
				false);
			campaignGameStarter.AddGameMenuOption(
				"fast_encounter",
				"fast_encounter_troops",
				"{=rxSz5dY1}Send troops.",
				ConditionOf("game_menu_encounter_order_attack_on_condition"),
				ConsequenceOf("game_menu_encounter_order_attack_on_consequence"),
				false,
				-1,
				false);
			campaignGameStarter.AddGameMenuOption(
				"fast_encounter",
				"fast_encounter_getaway",
				"{=qNgGoqmI}Try to get away.",
				ConditionOf("game_menu_encounter_leave_your_soldiers_behind_on_condition"),
				ConsequenceOf("game_menu_encounter_leave_your_soldiers_behind_accept_on_consequence"),
				//args => GameMenu.SwitchToMenu("try_to_get_away"),
				false,
				-1,
				false);
			campaignGameStarter.AddGameMenuOption(
				"fast_encounter",
				"fast_encounter_talk",
				"{=qNgGoqmI}Converse.",
				args =>
				{
					args.optionLeaveType = GameMenuOption.LeaveType.Conversation;
					return true;
				},
				args =>
				{
					PlayerEncounter.DoMeeting();
					//PartyBase encounteredParty = PlayerEncounter.EncounteredParty;
					//MenuHelper.EncounterLeaveConsequence(args);
					//Campaign.Current.HandlePartyEncounter(PartyBase.MainParty, encounteredParty);
				},
				false,
				-1,
				false);
			campaignGameStarter.AddGameMenuOption(
				"fast_encounter",
				"fast_encounter_surrend",
				"{=3nT5wWzb}Surrender.",
				ConditionOf("game_menu_encounter_surrender_on_condition"),
				args =>
				{
					PlayerEncounter.PlayerSurrender = true;
					PlayerEncounter.Update();
				},
				false,
				-1,
				false);
			campaignGameStarter.AddGameMenuOption(
				"fast_encounter",
				"fast_encounter_leave",
				"{=2YYRyrOO}Leave...",
				ConditionOf("game_menu_encounter_leave_on_condition"),
				ConsequenceOf("game_menu_encounter_leave_on_consequence"),
				true,
				-1,
				false);
		}

		public override void SyncData(IDataStore dataStore) { }
	}
}