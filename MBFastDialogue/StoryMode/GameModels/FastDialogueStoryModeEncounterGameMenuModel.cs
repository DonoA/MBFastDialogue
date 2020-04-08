using StoryMode.GameModels;

using TaleWorlds.CampaignSystem;

namespace MBFastDialogue.StoryMode.GameModels
{
	public class FastDialogueStoryModeEncounterGameMenuModel : StoryModeEncounterGameMenuModel
	{
		public override string? GetEncounterMenu(PartyBase attackerParty, PartyBase defenderParty, out bool startBattle, out bool joinBattle)
		{
			var encounteredPartyBase = GetEncounteredPartyBase(attackerParty, defenderParty);
			var result = Utils.GetEncounterMenu(attackerParty, defenderParty, encounteredPartyBase, out startBattle, out joinBattle);
			return result ?? base.GetEncounterMenu(attackerParty, defenderParty, out startBattle, out joinBattle);
		}
	}
}
