using TaleWorlds.CampaignSystem;

namespace MBFastDialogue
{
    public static class Utils
    {
		public static string? GetEncounterMenu(PartyBase attackerParty, PartyBase defenderParty, PartyBase encounteredPartyBase, out bool startBattle, out bool joinBattle)
		{
			joinBattle = false;
			startBattle = false;

			var notEventSettlement = !encounteredPartyBase.IsSettlement && encounteredPartyBase.MapEvent == null; // not sure if naming is correct
			var notMobile = !encounteredPartyBase.IsMobile;
			var notGarrisonOrSiege = !encounteredPartyBase.MobileParty.IsGarrison || MobileParty.MainParty.BesiegedSettlement == null;
			var notOwnSettlementOrNotOwnBesiegedSettlement = MobileParty.MainParty.CurrentSettlement == null || encounteredPartyBase.MobileParty.BesiegedSettlement != MobileParty.MainParty.CurrentSettlement;

			if (notEventSettlement && (notMobile || (notGarrisonOrSiege && notOwnSettlementOrNotOwnBesiegedSettlement)))
				return "fastdialogue_encounter";

			return null;
		}
	}
}