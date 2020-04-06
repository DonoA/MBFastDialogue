using HarmonyLib;
using Helpers;
using SandBox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Overlay;
using TaleWorlds.CampaignSystem.SandBox.CampaignBehaviors;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
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
		//private List<ConversationCharacterData> cached_otherSidePartners;
		//private List<ConversationCharacterData> cached_playerSidePartners;
		//private ConversationCharacterData cached_firstCharacterToTalk;

		//private GameState prevState;

		private long pausedTicks = 0;

		public static SubModule Instance { get; private set; }

		protected override void OnSubModuleLoad()
		{
			Harmony harmony = new Harmony("io.dallen.mb.fastdialogue");
			harmony.PatchAll();
			Instance = this;
		}

		protected override void OnBeforeInitialModuleScreenSetAsRoot()
		{
			InformationManager.DisplayMessage(new InformationMessage("Loaded MBFastDialogue.", Color.FromUint(4282569842U)));
		}

		private bool ShouldShowWarOptions()
		{
			try
			{
				return PlayerEncounter.EncounteredParty != null && Campaign.Current.MainParty.Party.MapFaction.IsAtWarWith(PlayerEncounter.EncounteredParty.MapFaction);
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
					EncounterMenuInit(args);
					ReflectionUtil.ForceCall<object>(GetGlobalCampaignBehaviorManager(), "game_menu_encounter_on_init", new object[] { args });
					
				}), GameOverlays.MenuOverlayType.Encounter, GameMenu.MenuFlags.none, null);
			campStarter.AddGameMenuOption("fast_combat_menu", "fast_combat_menu_attack", "{=o1pZHZOF}{ATTACK_TEXT}!",
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
					Campaign.Current.HandlePartyEncounter(Campaign.Current.MainParty.Party, encountered);
					//CampaignMission.OpenConversationMission(cached_playerSidePartners, cached_otherSidePartners, cached_firstCharacterToTalk);
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

				//if (GameStateManager.Current.ActiveState != prevState)
				//{
				//	//OnStateChange();
				//	prevState = GameStateManager.Current.ActiveState;
				//}
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

		public bool OnStateChange(GameState prev, GameState next)
		{
			if (prev is MapState prevMapState && next is MissionState missionState)
			{
				// Only open if this is a random meeting on the map
				if(Campaign.Current.MapStateData == null || Campaign.Current.MapStateData.GameMenuId != "encounter_meeting")
				{
					return true;
				}

				//only open if another menu wasn't just open
				if (pausedTicks > 2)
				{
					return true;
				}

				//var mission = missionState.CurrentMission;
				//var convoLogic = mission.GetMissionBehaviour<ConversationMissionLogic>();
				//if (convoLogic == null)
				//{
				//	return true;
				//}
				//var _otherSidePartners = ReflectionUtil.ForceGet<List<ConversationCharacterData>>(convoLogic, "_otherSidePartners");
				//var _playerSidePartners = ReflectionUtil.ForceGet<List<ConversationCharacterData>>(convoLogic, "_playerSidePartners");
				//var _firstCharacterToTalk = ReflectionUtil.ForceGet<ConversationCharacterData>(convoLogic, "_firstCharacterToTalk");

				//cached_playerSidePartners = _playerSidePartners;
				//cached_otherSidePartners = _otherSidePartners;
				//cached_firstCharacterToTalk = _firstCharacterToTalk;
				//GameStateManager.Current.PopState();
				//var menus = ReflectionUtil.ForceGet<Dictionary<string, GameMenu>>(Campaign.Current.GameMenuManager, "_gameMenus");
				//GameMenu menu;
				//menus.TryGetValue("fast_combat_menu", out menu);
				//menu.RunOnInit()

				GameMenu.ActivateGameMenu("fast_combat_menu");
				//Campaign.Current.CurrentMenuContext.Refresh();

				//GameMenu.SwitchToMenu("fast_combat_menu");
				return false;
			}

			return true;
		}


		private static void EncounterMenuInit(MenuCallbackArgs args)
		{
			Settlement currentSettlement = Settlement.CurrentSettlement;
			bool flag = false;
			if (PlayerEncounter.Battle != null && PlayerEncounter.Current.FirstInit)
			{
				PlayerEncounter.Current.FirstInit = false;
			}
			if (currentSettlement != null && currentSettlement.IsVillage && PlayerEncounter.Battle != null)
			{
				args.MenuContext.SetBackgroundMeshName("wait_ambush");
			}
			else if (PlayerEncounter.EncounteredParty != null && PlayerEncounter.EncounteredParty.IsMobile)
			{
				if (PlayerEncounter.EncounteredParty.MobileParty.IsVillager)
				{
					args.MenuContext.SetBackgroundMeshName("encounter_peasant");
				}
				else if (PlayerEncounter.EncounteredParty.MobileParty.IsCaravan)
				{
					args.MenuContext.SetBackgroundMeshName("encounter_caravan");
				}
				else
				{
					args.MenuContext.SetBackgroundMeshName(PlayerEncounter.EncounteredParty.MapFaction.Culture.EncounterBackgroundMesh);
				}
			}
			if (PartyBase.MainParty.Side == BattleSideEnum.Defender && PartyBase.MainParty.NumberOfHealthyMembers == 0)
			{
				int num = 0;
				foreach (PartyBase partyBase in PartyBase.MainParty.MapEvent.PartiesOnSide(PartyBase.MainParty.Side))
				{
					if (partyBase != PartyBase.MainParty)
					{
						num += partyBase.NumberOfHealthyMembers;
					}
				}
				if (num > 0)
				{
					MBTextManager.SetTextVariable("ENCOUNTER_TEXT", GameTexts.FindText("str_you_have_encountered_no_health_men_but_allies_has", null), true);
				}
				else
				{
					MBTextManager.SetTextVariable("ENCOUNTER_TEXT", (PartyBase.MainParty.MemberRoster.Count == 1) ? GameTexts.FindText("str_you_have_encountered_no_health_alone", null) : GameTexts.FindText("str_you_have_encountered_no_health_men", null), true);
				}
			}
			else if (currentSettlement != null)
			{
				if (currentSettlement.IsAmbush())
				{
					MBTextManager.SetTextVariable("ENCOUNTER_TEXT", GameTexts.FindText("str_you_trapped_enemies", null), true);
				}
				else if (currentSettlement.IsHideout())
				{
					MBTextManager.SetTextVariable("ENCOUNTER_TEXT", GameTexts.FindText("str_there_are_bandits_inside", null), true);
				}
				else if (currentSettlement.IsUnderRebellionAttack())
				{
					MBTextManager.SetTextVariable("ENCOUNTER_TEXT", GameTexts.FindText("str_there_are_rebels_inside", null), true);
				}
				else if (currentSettlement.IsVillage && PlayerEncounter.Battle != null)
				{
					int num2 = (from party in PlayerEncounter.Battle.InvolvedParties
								where party.Side != PartyBase.MainParty.Side
								select party).Sum((PartyBase party) => party.NumberOfHealthyMembers);
					MBTextManager.SetTextVariable("SETTLEMENT", currentSettlement.Name, false);
					TextObject textObject;
					if (PlayerEncounter.Battle.IsRaid && num2 == 0)
					{
						if (!MobileParty.MainParty.MapFaction.IsAtWarWith(currentSettlement.MapFaction))
						{
							textObject = GameTexts.FindText("str_you_have_encountered_settlement_to_raid_no_resisting_on_peace", null);
							textObject.SetTextVariable("KINGDOM", currentSettlement.MapFaction.IsKingdomFaction ? ((Kingdom)currentSettlement.MapFaction).EncyclopediaTitle : currentSettlement.MapFaction.Name);
						}
						else
						{
							textObject = GameTexts.FindText("str_you_have_encountered_settlement_to_raid_no_resisting_on_war", null);
						}
					}
					else if (PlayerEncounter.Battle.IsForcingSupplies)
					{
						if (!MobileParty.MainParty.MapFaction.IsAtWarWith(currentSettlement.MapFaction))
						{
							textObject = GameTexts.FindText("str_you_have_encountered_settlement_to_force_supplies_with_resisting_on_peace", null);
							textObject.SetTextVariable("KINGDOM", currentSettlement.MapFaction.IsKingdomFaction ? ((Kingdom)currentSettlement.MapFaction).EncyclopediaTitle : currentSettlement.MapFaction.Name);
						}
						else
						{
							textObject = GameTexts.FindText("str_you_have_encountered_settlement_to_force_supplies_with_resisting_on_war", null);
						}
					}
					else if (PlayerEncounter.Battle.IsForcingVolunteers)
					{
						if (!MobileParty.MainParty.MapFaction.IsAtWarWith(currentSettlement.MapFaction))
						{
							textObject = GameTexts.FindText("str_you_have_encountered_settlement_to_force_volunteers_with_resisting_on_peace", null);
							textObject.SetTextVariable("KINGDOM", currentSettlement.MapFaction.IsKingdomFaction ? ((Kingdom)currentSettlement.MapFaction).EncyclopediaTitle : currentSettlement.MapFaction.Name);
						}
						else
						{
							textObject = GameTexts.FindText("str_you_have_encountered_settlement_to_force_volunteers_with_resisting_on_war", null);
						}
					}
					else if (!MobileParty.MainParty.MapFaction.IsAtWarWith(currentSettlement.MapFaction))
					{
						textObject = GameTexts.FindText("str_you_have_encountered_settlement_to_raid_with_resisting_on_peace", null);
						textObject.SetTextVariable("KINGDOM", currentSettlement.MapFaction.IsKingdomFaction ? ((Kingdom)currentSettlement.MapFaction).EncyclopediaTitle : currentSettlement.MapFaction.Name);
					}
					else
					{
						textObject = GameTexts.FindText("str_you_have_encountered_settlement_to_raid_with_resisting_on_war", null);
					}
					textObject.SetTextVariable("SETTLEMENT", currentSettlement.Name);
					MBTextManager.SetTextVariable("ENCOUNTER_TEXT", textObject, true);
				}
				else if (currentSettlement.IsFortification)
				{
					if (PlayerEncounter.Battle != null)
					{
						if (PlayerEncounter.Battle.IsSiege)
						{
							TextObject textObject2 = GameTexts.FindText("str_you_have_encountered_settlement_to_siege", null);
							textObject2.SetTextVariable("SETTLEMENT", currentSettlement.Name);
							MBTextManager.SetTextVariable("ENCOUNTER_TEXT", textObject2, true);
						}
						else if (PlayerEncounter.Battle.IsSiegeOutside)
						{
							TextObject textObject3 = GameTexts.FindText("str_you_have_encountered_PARTY", null);
							textObject3.SetTextVariable("PARTY", PlayerEncounter.Battle.GetLeaderParty(PartyBase.MainParty.OpponentSide).Name);
							MBTextManager.SetTextVariable("ENCOUNTER_TEXT", textObject3, true);
						}
						else if (PlayerEncounter.EncounteredMobileParty.IsGarrison && MobileParty.MainParty.BesiegedSettlement != null)
						{
							TextObject textObject4 = new TextObject("{=xYeMbApi}{PARTY} has sallied out to attack you!", null);
							textObject4.SetTextVariable("PARTY", PlayerEncounter.Battle.GetLeaderParty(PartyBase.MainParty.OpponentSide).Name);
							MBTextManager.SetTextVariable("ENCOUNTER_TEXT", textObject4, true);
						}
						else
						{
							TextObject textObject5 = GameTexts.FindText("str_you_have_encountered_PARTY", null);
							textObject5.SetTextVariable("PARTY", PlayerEncounter.Battle.GetLeaderParty(PartyBase.MainParty.OpponentSide).Name);
							MBTextManager.SetTextVariable("ENCOUNTER_TEXT", textObject5, true);
						}
					}
					else
					{
						TextObject textObject6 = GameTexts.FindText("str_you_have_encountered_settlement_to_siege", null);
						textObject6.SetTextVariable("SETTLEMENT", currentSettlement.Name);
						MBTextManager.SetTextVariable("ENCOUNTER_TEXT", textObject6, true);
					}
				}
				else
				{
					MBTextManager.SetTextVariable("ENCOUNTER_TEXT", GameTexts.FindText("str_you_are_trapped_by_enemies", null), true);
				}
			}
			else if (MobileParty.MainParty.MapEvent != null && PlayerEncounter.CheckIfLeadingAvaliable() && PlayerEncounter.GetLeadingHero() != Hero.MainHero)
			{
				Hero leadingHero = PlayerEncounter.GetLeadingHero();
				TextObject textObject7 = GameTexts.FindText("str_army_leader_encounter", null);
				textObject7.SetTextVariable("PARTY", leadingHero.PartyBelongedTo.Name);
				textObject7.SetTextVariable("ARMY_COMMANDER_GENDER", leadingHero.IsFemale ? 1 : 0);
				MBTextManager.SetTextVariable("ENCOUNTER_TEXT", textObject7, true);
			}
			else
			{
				TextObject textObject8;
				if (MobileParty.MainParty.CurrentSettlement != null && PlayerEncounter.EncounteredMobileParty.BesiegedSettlement == MobileParty.MainParty.CurrentSettlement)
				{
					textObject8 = new TextObject("{=dGoEWaeX}The enemy has begun their assault!", null);
					flag = true;
				}
				else if (PlayerEncounter.EncounteredMobileParty.Army != null)
				{
					if (PlayerEncounter.EncounteredMobileParty.Army.LeaderParty == PlayerEncounter.EncounteredMobileParty)
					{
						textObject8 = GameTexts.FindText("str_you_have_encountered_ARMY", null);
						textObject8.SetTextVariable("ARMY", PlayerEncounter.EncounteredMobileParty.Army.Name);
					}
					else
					{
						textObject8 = GameTexts.FindText("str_you_have_encountered_PARTY", null);
						textObject8.SetTextVariable("PARTY", MapEvent.PlayerMapEvent.GetLeaderParty(PartyBase.MainParty.OpponentSide).Name);
					}
				}
				else
				{
					if (!MobileParty.MainParty.MapFaction.IsAtWarWith(PlayerEncounter.EncounteredMobileParty.MapFaction))
					{
						textObject8 = GameTexts.FindText("str_you_have_encountered_PARTY_on_peace", null);
						IFaction mapFaction = PlayerEncounter.EncounteredMobileParty.MapFaction;
						textObject8.SetTextVariable("KINGDOM", mapFaction.IsKingdomFaction ? ((Kingdom)mapFaction).EncyclopediaTitle : mapFaction.Name);
					}
					else
					{
						textObject8 = GameTexts.FindText("str_you_have_encountered_PARTY", null);
					}
					textObject8.SetTextVariable("PARTY", MapEvent.PlayerMapEvent.GetLeaderParty(PartyBase.MainParty.OpponentSide).Name);
				}
				MBTextManager.SetTextVariable("ENCOUNTER_TEXT", textObject8, true);
			}
			if (Settlement.CurrentSettlement != null)
			{
				args.MenuContext.SetBackgroundMeshName(Settlement.CurrentSettlement.GetComponent<SettlementComponent>().WaitMeshName);
			}
			MBTextManager.SetTextVariable("ATTACK_TEXT", flag ? new TextObject("{=Ky03jg94}Fight", null) : new TextObject("{=zxMOqlhs}Attack", null), false);
		}

	}

	[HarmonyPatch(typeof(GameStateManager), "PushStateRPC")]
	public class SkipStatePushPatch
	{
		static bool Prefix(GameStateManager __instance, GameState gameState, int level)
		{
			try
			{
				//InformationManager.DisplayMessage(new InformationMessage("Trying to change state", Color.White));
				return SubModule.Instance.OnStateChange(GameStateManager.Current.ActiveState, gameState);
			}
			catch (Exception ex)
			{
				InformationManager.DisplayMessage(new InformationMessage("MBFastDialogue generated a patch exception " + ex.Message, Color.White));
			}
			return true;
		}
	}
}