using SandBox;
using System;
using System.Collections.Generic;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Overlay;
using TaleWorlds.CampaignSystem.SandBox.CampaignBehaviors;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace MBFastDialogue
{
	public class ReflectionUtil
	{
		public static T ForceGet<T>(object obj, string field)
		{
			return (T)obj.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(obj);
		}

		public static T ForceCall<T>(object obj, string methodName, object[] args)
		{
			MethodInfo method = obj.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
			if (method.ReturnType == typeof(void))
			{
				method.Invoke(obj, args);
				return default;
			}
			return (T)method.Invoke(obj, args);
		}
	}

	public class SubModule : MBSubModuleBase
	{
		private List<ConversationCharacterData> cached_otherSidePartners;
		private List<ConversationCharacterData> cached_playerSidePartners;
		private ConversationCharacterData cached_firstCharacterToTalk;

		private GameState prevState;

		private long pausedTicks = 0;

		protected override void OnBeforeInitialModuleScreenSetAsRoot()
		{
			InformationManager.DisplayMessage(new InformationMessage("Loaded MBFastDialogue.", Color.FromUint(4282569842U)));
		}

		protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
		{
			var campStarter = gameStarterObject as CampaignGameStarter;
			if(campStarter == null)
			{
				return;
			}

			campStarter.AddGameMenu("fast_combat_menu", "{=!}{ENCOUNTER_TEXT}", 
				new OnInitDelegate((args) => {
					ReflectionUtil.ForceCall<object>(GetGlobalCampaignBehaviorManager(), "game_menu_encounter_on_init", new object[] { args });
				}), GameOverlays.MenuOverlayType.Encounter, GameMenu.MenuFlags.none, null);
			campStarter.AddGameMenuOption("fast_combat_menu", "fast_combat_menu_attack", "{=o1pZHZOF}{ATTACK_TEXT}!",
				new GameMenuOption.OnConditionDelegate((args) => {
					bool atWar = cached_otherSidePartners[0].Party != null && cached_playerSidePartners[0].Party.MapFaction.IsAtWarWith(cached_otherSidePartners[0].Party.MapFaction);
					return atWar && ReflectionUtil.ForceCall<bool>(GetGlobalCampaignBehaviorManager(), "game_menu_encounter_attack_on_condition", new object[] { args });
				}),
				CampaignManagerConsequenceOf("game_menu_encounter_attack_on_consequence"),
				false, -1, false);
			campStarter.AddGameMenuOption("fast_combat_menu", "fast_combat_menu_send_troops", "{=rxSz5dY1}Send troops.",
				new GameMenuOption.OnConditionDelegate((args) => {
					bool atWar = cached_otherSidePartners[0].Party != null && cached_playerSidePartners[0].Party.MapFaction.IsAtWarWith(cached_otherSidePartners[0].Party.MapFaction);
					return atWar && ReflectionUtil.ForceCall<bool>(GetGlobalCampaignBehaviorManager(), "game_menu_encounter_order_attack_on_condition", new object[] { args });
				}),
				CampaignManagerConsequenceOf("game_menu_encounter_order_attack_on_consequence"), 
				false, -1, false);
			campStarter.AddGameMenuOption("fast_combat_menu", "fast_combat_menu_getaway", "{=qNgGoqmI}Try to get away.",
				CampaignManagerConditionOf("game_menu_encounter_leave_your_soldiers_behind_on_condition"),
				new GameMenuOption.OnConsequenceDelegate((args) => GameMenu.SwitchToMenu("try_to_get_away")), false, -1, false);
			campStarter.AddGameMenuOption("fast_combat_menu", "fast_combat_menu_talk", "{=qNgGoqmI}Converse.",
				new GameMenuOption.OnConditionDelegate((args) =>
				{
					args.optionLeaveType = GameMenuOption.LeaveType.Conversation;
					return true;
				}),
				new GameMenuOption.OnConsequenceDelegate((args) =>
				{
					CampaignMission.OpenConversationMission(cached_playerSidePartners, cached_otherSidePartners, cached_firstCharacterToTalk);
				}), false, -1, false);
			campStarter.AddGameMenuOption("fast_combat_menu", "fast_combat_menu_surrend", "{=3nT5wWzb}Surrender.",
				CampaignManagerConditionOf("game_menu_encounter_surrender_on_condition"),
				new GameMenuOption.OnConsequenceDelegate((args) =>
				{
					PlayerEncounter.PlayerSurrender = true;
					PlayerEncounter.Update();
				}), false, -1, false);
			campStarter.AddGameMenuOption("fast_combat_menu", "fast_combat_menu_leave", "{=2YYRyrOO}Leave...",
				CampaignManagerConditionOf("game_menu_encounter_leave_on_condition"),
				CampaignManagerConsequenceOf("game_menu_encounter_leave_on_consequence"), true, -1, false);
		}

		protected override void OnApplicationTick(float dt)
		{
			if(GameStateManager.Current.ActiveState is MapState mapState)
			{
				if(Campaign.Current.TimeControlMode == CampaignTimeControlMode.Stop)
				{
					pausedTicks++;
				}
				else
				{
					pausedTicks = 0;
				}
			}

			if (GameStateManager.Current != null && GameStateManager.Current.ActiveState != prevState)
			{
				OnStateChange();
				prevState = GameStateManager.Current.ActiveState;
			}

		}

		private EncounterGameMenuBehavior GetGlobalCampaignBehaviorManager()
		{
			return Campaign.Current.GetCampaignBehavior<EncounterGameMenuBehavior>();
		}

		private GameMenuOption.OnConditionDelegate CampaignManagerConditionOf(string name)
		{
			return new GameMenuOption.OnConditionDelegate((args) =>
			{
				return ReflectionUtil.ForceCall<bool>(GetGlobalCampaignBehaviorManager(), name, new object[] { args });
			});
		}

		private GameMenuOption.OnConsequenceDelegate CampaignManagerConsequenceOf(string name)
		{
			return new GameMenuOption.OnConsequenceDelegate((args) =>
			{
				ReflectionUtil.ForceCall<object>(GetGlobalCampaignBehaviorManager(), name, new object[] { args });
			});
		}

		private void OnStateChange()
		{
			if (GameStateManager.Current.ActiveState is MissionState missionState && prevState is MapState prevMapState)
			{
				// Only open if this is a random meeting on the map
				if(Campaign.Current.MapStateData.GameMenuId != "encounter_meeting")
				{
					return;
				}

				//only open if another menu wasn't just open
				if (pausedTicks > 2)
				{
					return;
				}

				var mission = missionState.CurrentMission;
				var convoLogic = mission.GetMissionBehaviour<ConversationMissionLogic>();
				if (convoLogic == null)
				{
					return;
				}
				var _otherSidePartners = ReflectionUtil.ForceGet<List<ConversationCharacterData>>(convoLogic, "_otherSidePartners");
				var _playerSidePartners = ReflectionUtil.ForceGet<List<ConversationCharacterData>>(convoLogic, "_playerSidePartners");
				var _firstCharacterToTalk = ReflectionUtil.ForceGet<ConversationCharacterData>(convoLogic, "_firstCharacterToTalk");

				cached_playerSidePartners = _playerSidePartners;
				cached_otherSidePartners = _otherSidePartners;
				cached_firstCharacterToTalk = _firstCharacterToTalk;
				GameStateManager.Current.PopState();
				GameMenu.ActivateGameMenu("fast_combat_menu");
			}
		}
	}
}