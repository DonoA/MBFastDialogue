using HarmonyLib;

using MBFastDialogue.CampaignSystem.CampaignBehaviors;
using MBFastDialogue.StoryMode.GameModels;

using System;

using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace MBFastDialogue
{
	public class FastDialogueSubModule : MBSubModuleBase
	{
		public FastDialogueSubModule()
		{
			try
			{
				var harmony = new Harmony("io.dallen.bannerlord.fastdialogue");
				harmony.PatchAll(typeof(FastDialogueSubModule).Assembly);
			}
			catch (Exception ex)
			{
				// TODO: Find a logger
			}
		}

		protected override void OnGameStart(Game game, IGameStarter gameStarter)
		{
			if (game.GameType is Campaign campaign && gameStarter is CampaignGameStarter campaignGameStarter)
			{
				campaignGameStarter.AddBehavior(new FastDialogueCampaignBehaviorBase());

				// Patching used instead.
				//campaignGameStarter.AddModel(new FastDialogStoryModeEncounterGameMenuModel());
			}
		}
	}
}