using HarmonyLib;

using System;
using System.Reflection;

using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;

namespace MBFastDialogue
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
			if (menuId == "fastdialogue_encounter")
			{
				MenuCallbackArgs args = new MenuCallbackArgs(state, null);
				game_menu_encounter_on_initMethod.Invoke(null, new object[] { args });
			}
		}
	}
}