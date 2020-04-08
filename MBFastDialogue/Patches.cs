﻿using HarmonyLib;
using StoryMode.GameModels;
using System;
using System.Reflection;

using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;

namespace MBFastDialogue.Patches
{
	/// <summary>
	/// I guess patching it via postfix is better than directly adding out method to
	/// GameMenuCallbackManager._gameMenuInitializationHandlers
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
			if (menuId == FastDialogueSubModule.FastEncounterMenu)
			{
				MenuCallbackArgs args = new MenuCallbackArgs(state, null);
				game_menu_encounter_on_initMethod.Invoke(null, new object[] { args });
			}
		}
	}

	/// <summary>
	/// I prefer to patch the menthod instead of using override
	/// If we consider that multiple mods would want to override
	/// GetEncounterMenu, this is the safest approach.
	/// </summary>
	[HarmonyPatch(typeof(StoryModeEncounterGameMenuModel), "GetEncounterMenu")]
	public class StoryModeEncounterGameMenuModelPatch1
	{
		private static MethodInfo GetEncounteredPartyBaseMethod { get; }
			= typeof(StoryModeEncounterGameMenuModel).GetMethod("GetEncounteredPartyBase", BindingFlags.Instance | BindingFlags.NonPublic);

		private static void Postfix(StoryModeEncounterGameMenuModel __instance, ref string __result, PartyBase attackerParty, PartyBase defenderParty, bool startBattle, bool joinBattle)
		{
			if(__result != "encounter_meeting")
			{
				return;
			}
			var encounteredPartyBase = (PartyBase)GetEncounteredPartyBaseMethod.Invoke(__instance, new object[] { attackerParty, defenderParty });
			var result = GetEncounterMenu(attackerParty, defenderParty, encounteredPartyBase);

			if (result != null)
			{
				__result = result;
			}
		}

		private static string? GetEncounterMenu(PartyBase attackerParty, PartyBase defenderParty, PartyBase encounteredPartyBase)
		{
			try
			{
				var notEventSettlement = !encounteredPartyBase.IsSettlement && encounteredPartyBase.MapEvent == null; // not sure if naming is correct
				var notMobile = !encounteredPartyBase.IsMobile;
				var notGarrisonOrSiege = !encounteredPartyBase.MobileParty.IsGarrison || MobileParty.MainParty.BesiegedSettlement == null;
				var notOwnSettlementOrNotOwnBesiegedSettlement = MobileParty.MainParty.CurrentSettlement == null || encounteredPartyBase.MobileParty.BesiegedSettlement != MobileParty.MainParty.CurrentSettlement;
				var encounteredArmy = encounteredPartyBase.MobileParty.Army != null;

				if (notEventSettlement && (notMobile || (notGarrisonOrSiege && notOwnSettlementOrNotOwnBesiegedSettlement)))
				{
					return FastDialogueSubModule.FastEncounterMenu;
				}

				return null;
			}
			catch(Exception ex)
			{
				return null;
			}
		}
	}
}