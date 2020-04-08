using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.SandBox.CampaignBehaviors;
using TaleWorlds.Library;

namespace MBFastDialogue.CampaignSystem.CampaignBehaviors
{
	/// <summary>
	/// It is possible to do everything in a XML file, but when I'm not sure, is this an alternative or legacy code?
	/// Keeping it for clarity
	/// </summary>
	public class FastDialogueXmlCampaignBehaviorBase : EncounterGameMenuBehavior
	{
		public override void RegisterEvents()
		{
			CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
		}
		private void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
		{
			campaignGameStarter.LoadGameMenus(typeof(FastDialogueGameMenusCallbacks), $"{BasePath.Name}Modules/Aragas.DialogSkipper/ModuleData/game_menus.xml");
		}

		public override void SyncData(IDataStore dataStore) { }
	}
}