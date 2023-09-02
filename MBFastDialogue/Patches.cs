using HarmonyLib;
//using StoryMode.GameModels;
using System;
using System.Reflection;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Library;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem;

namespace MBFastDialogue.Patches
{
    /// <summary>
    /// Hook the menu setup method to ensure the fast encounter method is hooked correctly
    /// </summary>
    [HarmonyPatch(typeof(GameMenuCallbackManager), "InitializeState")]
	public class GameMenuCallbackManagerPatch1
	{
		private static Type DefaultEncounterType { get; }
			= typeof(GameMenu).Assembly.GetType("TaleWorlds.CampaignSystem.GameMenus.GameMenuInitializationHandlers.DefaultEncounter");
		private static MethodInfo game_menu_encounter_on_initMethod { get; }
			= DefaultEncounterType.GetMethod("game_menu_encounter_on_init", BindingFlags.Static | BindingFlags.NonPublic);

		private static void Postfix(GameMenuCallbackManager __instance, string menuId, MenuContext state)
		{
            try
			{
				if (menuId == FastDialogueSubModule.FastEncounterMenu)
				{
					MenuCallbackArgs args = new MenuCallbackArgs(state, null);
					game_menu_encounter_on_initMethod.Invoke(null, new object[] { args });
				}
			}
			catch (Exception ex)
			{
				InformationManager.DisplayMessage(new InformationMessage("Fast Dialogue failed to init menu", Color.FromUint(4282569842U)));
			}
		}
	}

    // For other mods compatibility
    // When a non-native menu option is added to menu ID "encounter", it is added as a Fast Dialogue menu option   
    [HarmonyPatch(typeof(CampaignGameStarter), "AddGameMenuOption")]
	public class CampaignGameStarterPatch1
	{
        private static void Postfix(CampaignGameStarter __instance, string menuId, string optionId, string optionText, GameMenuOption.OnConditionDelegate condition, GameMenuOption.OnConsequenceDelegate consequence, bool isLeave = false, int index = -1, bool isRepeatable = false, object relatedObject = null)
        {
			try
			{
				if (menuId == "encounter" && optionId != "continue_preparations" && optionId != "village_raid_action" && optionId != "village_force_volunteer_action" && optionId != "village_force_supplies_action" && optionId != "attack" && optionId != "capture_the_enemy" && optionId != "str_order_attack" && optionId != "leave_soldiers_behind" && optionId != "surrender" && optionId != "leave" && optionId != "go_back_to_settlement")
				{
					__instance.AddGameMenuOption(FastDialogueSubModule.FastEncounterMenu, optionId, optionText, condition, consequence, isLeave, index, isRepeatable, relatedObject);
				}
			}
			catch(Exception ex)
			{

			}
        }
    }

    /// <summary>
    /// Catches game trying to setup a new map menu and subs in the fast encounter menu when appropriate
    /// </summary>
    [HarmonyPatch(typeof(DefaultEncounterGameMenuModel), "GetEncounterMenu")]
	public class StoryModeEncounterGameMenuModelPatch1
	{
        private static MethodInfo GetEncounteredPartyBaseMethod { get; }
			= typeof(DefaultEncounterGameMenuModel).GetMethod("GetEncounteredPartyBase", BindingFlags.Instance | BindingFlags.NonPublic);

		private static void Postfix(DefaultEncounterGameMenuModel __instance, ref string __result, PartyBase attackerParty, PartyBase defenderParty, bool startBattle, bool joinBattle)
		{
			try
			{
				var encounteredPartyBase = (PartyBase)GetEncounteredPartyBaseMethod.Invoke(__instance, new object[] { attackerParty, defenderParty });
                //InformationManager.DisplayMessage(new InformationMessage($"{encounteredPartyBase.Id}", Color.FromUint(4282569842U)));
                var result = GetEncounterMenu(attackerParty, defenderParty, encounteredPartyBase);

				if (result != null)
				{
					__result = result;
				}
			}
			catch (Exception ex)
			{ 
				InformationManager.DisplayMessage(new InformationMessage($"Fast Dialogue failed to handle interaction", Color.FromUint(4282569842U)));
			}
		}

		private static string? GetEncounterMenu(PartyBase attackerParty, PartyBase defenderParty, PartyBase encounteredPartyBase)
		{
            // Debug
            /*InformationManager.DisplayMessage(new InformationMessage($"Party ID : {encounteredPartyBase.Id}", Color.FromUint(4282569842U)));
            if(encounteredPartyBase.MobileParty != null)
            {
                InformationManager.DisplayMessage(new InformationMessage($"MobileParty StringId : {encounteredPartyBase.MobileParty.StringId}", Color.FromUint(4282569842U)));
            }*/

            if (!FastDialogueSubModule.Instance.running)
			{
				return null;
			}

            if(encounteredPartyBase.Id.Contains("locate_and_rescue_traveller_quest_raider_party"))
            {
                return null;
            }

            if (encounteredPartyBase.IsSettlement || encounteredPartyBase.MapEvent != null)
			{
				return null;
			}

            if ((encounteredPartyBase.MapFaction.IsClan || (encounteredPartyBase.MobileParty != null && encounteredPartyBase.MobileParty.IsLordParty) || encounteredPartyBase.MapFaction.IsMinorFaction) && !PartyBase.MainParty.MapFaction.IsAtWarWith(encounteredPartyBase.MapFaction))
            {
                return null;
            }

			if (!FastDialogueSubModule.Instance.IsPatternWhitelisted(encounteredPartyBase.Id))
			{
				return null;
			}

            bool inOwnedKingdom = encounteredPartyBase.MapFaction == PartyBase.MainParty.MapFaction && PartyBase.MainParty.MapFaction.Leader.CharacterObject == PartyBase.MainParty.LeaderHero.CharacterObject;
			if (inOwnedKingdom)
			{
				return null;
			}

            if (encounteredPartyBase.MobileParty?.IsCurrentlyUsedByAQuest == true && encounteredPartyBase.Id.Contains("villager"))
			{
				return null;
			}

			if (!encounteredPartyBase.IsMobile)
			{
				return FastDialogueSubModule.FastEncounterMenu;
			}

			var notGarrisonOrSiege = !encounteredPartyBase.MobileParty.IsGarrison || MobileParty.MainParty.BesiegedSettlement == null;
			var notOwnSettlementOrNotOwnBesiegedSettlement = MobileParty.MainParty.CurrentSettlement == null || encounteredPartyBase.MobileParty.BesiegedSettlement != MobileParty.MainParty.CurrentSettlement;
			if (notGarrisonOrSiege && notOwnSettlementOrNotOwnBesiegedSettlement)
			{
				return FastDialogueSubModule.FastEncounterMenu;
			}
			return null;
		}
	}
}
