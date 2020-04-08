using HarmonyLib;
using MBFastDialogue.CampaignBehaviors;
using System;

using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace MBFastDialogue
{
	public class FastDialogueSubModule : MBSubModuleBase
	{
		public static string FastEncounterMenu = "fast_encounter";

		public static string ModuleName = "MBFastDialogue";

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

		protected override void OnBeforeInitialModuleScreenSetAsRoot()
		{
			InformationManager.DisplayMessage(new InformationMessage("Loaded " + ModuleName, Color.FromUint(4282569842U)));
		}

		protected override void OnGameStart(Game game, IGameStarter gameStarter)
		{
			if (game.GameType is Campaign campaign && gameStarter is CampaignGameStarter campaignGameStarter)
			{
				campaignGameStarter.AddBehavior(new FastDialogueCampaignBehaviorBase());
			}
		}
	}
}