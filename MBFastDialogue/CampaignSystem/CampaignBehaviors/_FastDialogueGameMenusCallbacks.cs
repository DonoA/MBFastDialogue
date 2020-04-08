using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.SandBox;

namespace MBFastDialogue.CampaignSystem.CampaignBehaviors
{
    /// <summary>
    /// As the actual menu will be using instance data, not bothering to will with actual logic for now
    /// </summary>
    public class FastDialogueGameMenusCallbacks : GameMenusCallbacks
	{
		public static bool fast_encounter_attack_on_condition(MenuCallbackArgs args)
		{
			return true;
		}
		public static void fast_encounter_attack_on_consequence(MenuCallbackArgs args)
		{

		}

		public static bool fast_encounter_attack_troops_on_condition(MenuCallbackArgs args)
		{
			return true;
		}
		public static void fast_encounter_attack_troops_on_consequence(MenuCallbackArgs args)
		{

		}

		public static bool fast_encounter_getaway_on_condition(MenuCallbackArgs args)
		{
			return true;
		}
		public static void fast_encounter_getaway_on_consequence(MenuCallbackArgs args)
		{

		}

		public static bool fast_encounter_talk_on_condition(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Conversation;
			return true;
		}
		public static void fast_encounter_talk_on_consequence(MenuCallbackArgs args)
		{
			PlayerEncounter.DoMeeting();
		}

		public static bool fast_encounter_surrender_on_condition(MenuCallbackArgs args)
		{
			return true;
		}
		public static void fast_encounter_surrender_on_consequence(MenuCallbackArgs args)
		{
			PlayerEncounter.PlayerSurrender = true;
			PlayerEncounter.Update();
		}

		public static bool fast_encounter_leave_on_condition(MenuCallbackArgs args)
		{
			return true;
		}
		public static void fast_encounter_leave_on_consequence(MenuCallbackArgs args)
		{

		}
	}
}