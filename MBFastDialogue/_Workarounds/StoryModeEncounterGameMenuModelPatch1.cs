using HarmonyLib;

using StoryMode.GameModels;

using System.Reflection;

using TaleWorlds.CampaignSystem;

namespace MBFastDialogue._Workarounds
{
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

		private static void Postfix(StoryModeEncounterGameMenuModel __instance, ref string __result, PartyBase attackerParty, PartyBase defenderParty, out bool startBattle, out bool joinBattle)
		{
			var encounteredPartyBase = (PartyBase) GetEncounteredPartyBaseMethod.Invoke(__instance, new object[] { attackerParty, defenderParty });
			var result = Utils.GetEncounterMenu(attackerParty, defenderParty, encounteredPartyBase, out startBattle, out joinBattle);
			if (result != null)
				__result = result;
		}
	}
}
