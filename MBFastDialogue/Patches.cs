using HarmonyLib;
using StoryMode.GameComponents;
using SandBox.GameComponents;
//using StoryMode.GameModels;
using System;
using System.Reflection;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.CampaignSystem.GameComponents;

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

            if ((encounteredPartyBase.Id.Contains("lord") || encounteredPartyBase.MapFaction.IsMinorFaction) && !PartyBase.MainParty.MapFaction.IsAtWarWith(encounteredPartyBase.MapFaction))
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
