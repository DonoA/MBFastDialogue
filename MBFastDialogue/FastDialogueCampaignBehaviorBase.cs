using Helpers;
using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Overlay;
using TaleWorlds.CampaignSystem.SandBox.CampaignBehaviors;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace MBFastDialogue.CampaignBehaviors
{
	/// <summary>
	/// Defines the fast encounter menu with special converse option
	/// </summary>
	public class FastDialogueCampaignBehaviorBase : EncounterGameMenuBehavior
	{
		private EncounterGameMenuBehavior GetGlobalCampaignBehaviorManager() => Campaign.Current.GetCampaignBehavior<EncounterGameMenuBehavior>();
		private void Init(MenuCallbackArgs args) =>
			 ReflectionUtils.ForceCall<object>(GetGlobalCampaignBehaviorManager(), "game_menu_encounter_on_init", new object[] { args });
		private GameMenuOption.OnConditionDelegate ConditionOf(string name) =>
			(MenuCallbackArgs args) => ReflectionUtils.ForceCall<bool>(GetGlobalCampaignBehaviorManager(), name, new object[] { args });
		private GameMenuOption.OnConsequenceDelegate ConsequenceOf(string name) =>
			(MenuCallbackArgs args) => ReflectionUtils.ForceCall<object>(GetGlobalCampaignBehaviorManager(), name, new object[] { args });

		private bool ShouldShowWarOptions()
		{
			try
			{
				return PlayerEncounter.EncounteredParty != null && PartyBase.MainParty.MapFaction.IsAtWarWith(PlayerEncounter.EncounteredParty.MapFaction);
			}
			catch (Exception ex)
			{
				InformationManager.DisplayMessage(new InformationMessage("MBFastDialogue generated an exception " + ex.Message, Color.Black));
			}
			return false;
		}

		public override void RegisterEvents()
		{
			CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
		}

		private void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
		{
			campaignGameStarter.AddGameMenu(
				FastDialogueSubModule.FastEncounterMenu,
				"{=!}{ENCOUNTER_TEXT}",
				Init,
				GameOverlays.MenuOverlayType.None,
				GameMenu.MenuFlags.none,
				null);
			campaignGameStarter.AddGameMenuOption(
				FastDialogueSubModule.FastEncounterMenu,
				"fast_encounter_attack",
				"{=o1pZHZOF}Attack!",
				args =>
				{
					return ShouldShowWarOptions() && ReflectionUtils.ForceCall<bool>(GetGlobalCampaignBehaviorManager(), "game_menu_encounter_attack_on_condition", new object[] { args });
				},
				ConsequenceOf("game_menu_encounter_attack_on_consequence"),
				false,
				-1,
				false);
			campaignGameStarter.AddGameMenuOption(
				FastDialogueSubModule.FastEncounterMenu,
				"fast_encounter_troops",
				"{=rxSz5dY1}Send troops.",
				(args) => {
					return ShouldShowWarOptions() && ReflectionUtils.ForceCall<bool>(GetGlobalCampaignBehaviorManager(), "game_menu_encounter_order_attack_on_condition", new object[] { args });
				},
				ConsequenceOf("game_menu_encounter_order_attack_on_consequence"),
				false,
				-1,
				false);
			campaignGameStarter.AddGameMenuOption(
				FastDialogueSubModule.FastEncounterMenu,
				"fast_encounter_getaway",
				"{=qNgGoqmI}Try to get away.",
				ConditionOf("game_menu_encounter_leave_your_soldiers_behind_on_condition"),
				ConsequenceOf("game_menu_encounter_leave_your_soldiers_behind_accept_on_consequence"),
				false,
				-1,
				false);
			campaignGameStarter.AddGameMenuOption(
				FastDialogueSubModule.FastEncounterMenu,
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
				},
				false,
				-1,
				false);
			campaignGameStarter.AddGameMenuOption(
				FastDialogueSubModule.FastEncounterMenu,
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
				FastDialogueSubModule.FastEncounterMenu,
				"fast_encounter_leave",
				"{=2YYRyrOO}Leave...",
				ConditionOf("game_menu_encounter_leave_on_condition"),
				(args) =>
				{
					MenuHelper.EncounterLeaveConsequence(args);
					if (PartyBase.MainParty.IsMobile && PartyBase.MainParty.MobileParty != null)
					{
						PartyBase.MainParty.MobileParty.IsDisorganized = false;
					}
				},
				true,
				-1,
				false);
		}
	}
}