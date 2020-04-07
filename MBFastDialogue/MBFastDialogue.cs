using Helpers;
using SandBox;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml;
using System.Xml.Serialization;
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

	public class Settings
	{
		[XmlElement("pattern_whitelist")]
		public Whitelist whitelist { get; set; } = new Whitelist();
	}

	public class Whitelist
	{
		[XmlElement("pattern")]
		public List<string> whitelistPatterns { get; set; } = new List<string>();
	}

	public static class XmlUtil
	{
		public static T SettingsFor<T>(string moduleName)
		{
			string settingsPath = Path.Combine(BasePath.Name, "Modules", moduleName, "settings.xml");
			try
			{
				using (XmlReader reader = XmlReader.Create(settingsPath))
				{
					XmlRootAttribute root = new XmlRootAttribute();
					root.ElementName = moduleName + ".Settings";
					root.IsNullable = true;

					if (reader.MoveToContent() != XmlNodeType.Element)
					{
						return default;
					}

					if (reader.Name != root.ElementName)
					{
						return default;
					}

					XmlSerializer serialiser = new XmlSerializer(typeof(T), root);
					var loaded = (T)serialiser.Deserialize(reader);
					return loaded;
				}
			}
			catch (Exception ex)
			{
				return default;
			}
		}
	}

	public class SubModule : MBSubModuleBase
	{
		private GameState prevState;

		private long pausedTicks = 0;

		private Settings settings = new Settings();

		protected override void OnSubModuleLoad()
		{
			try
			{
				var newSettings = XmlUtil.SettingsFor<Settings>("MBFastDialogue");
				if(newSettings != null)
				{
					settings = newSettings;
				}
			}
			catch (Exception ex)
			{
				InformationManager.DisplayMessage(new InformationMessage("Failed to load config.", Color.FromUint(4282569842U)));
			}
		}

		protected override void OnBeforeInitialModuleScreenSetAsRoot()
		{
			InformationManager.DisplayMessage(new InformationMessage("Loaded MBFastDialogue.", Color.FromUint(4282569842U)));
		}

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
			campStarter.AddGameMenuOption("fast_combat_menu", "fast_combat_menu_attack", "{=o1pZHZOF}Attack!",
				new GameMenuOption.OnConditionDelegate((args) => {
					return ShouldShowWarOptions() && ReflectionUtil.ForceCall<bool>(GetGlobalCampaignBehaviorManager(), "game_menu_encounter_attack_on_condition", new object[] { args });
				}),
				CampaignManagerConsequenceOf("game_menu_encounter_attack_on_consequence"),
				false, -1, false);
			campStarter.AddGameMenuOption("fast_combat_menu", "fast_combat_menu_send_troops", "{=rxSz5dY1}Send troops.",
				new GameMenuOption.OnConditionDelegate((args) => {
					return ShouldShowWarOptions() && ReflectionUtil.ForceCall<bool>(GetGlobalCampaignBehaviorManager(), "game_menu_encounter_order_attack_on_condition", new object[] { args });
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
					var encountered = PlayerEncounter.EncounteredParty;
					MenuHelper.EncounterLeaveConsequence(args);
					Campaign.Current.HandlePartyEncounter(PartyBase.MainParty, encountered);
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
				new GameMenuOption.OnConsequenceDelegate((args) =>
				{
					MenuHelper.EncounterLeaveConsequence(args);
					if (PartyBase.MainParty.IsMobile && PartyBase.MainParty.MobileParty != null)
					{
						PartyBase.MainParty.MobileParty.IsDisorganized = false;
					}
				}), true, -1, false);
		}

		protected override void OnApplicationTick(float dt)
		{
			try
			{
				if (GameStateManager.Current.ActiveState is MapState mapState)
				{
					if (Campaign.Current.TimeControlMode == CampaignTimeControlMode.Stop)
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
			catch(Exception ex)
			{
				InformationManager.DisplayMessage(new InformationMessage("MBFastDialogue generated an exception " + ex.Message, Color.Black));
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

		private bool matchesPattern(string name)
		{
			if(settings.whitelist.whitelistPatterns.Count == 0)
			{
				return true;
			}

			foreach(var pattern in settings.whitelist.whitelistPatterns)
			{
				if(name.Contains(pattern))
				{
					return true;
				}
			}

			return false;
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

				if(!matchesPattern(PlayerEncounter.EncounteredParty.Leader.OriginCharacterStringId))
				{
					return;
				}

				GameStateManager.Current.PopState();
				GameMenu.ActivateGameMenu("fast_combat_menu");
			}
		}
	}
}