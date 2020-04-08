using HarmonyLib;
using MBFastDialogue.CampaignBehaviors;
using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace MBFastDialogue
{
	/// <summary>
	/// The entry point for the submodule
	/// </summary>
	public class FastDialogueSubModule : MBSubModuleBase
	{
		public static string FastEncounterMenu = "fast_combat_menu";

		public static string ModuleName = "MBFastDialogue";

		public static FastDialogueSubModule? Instance { get; private set; }

		public Settings settings { get; set; } =  new Settings();

		public FastDialogueSubModule()
		{
			Instance = this;
			try
			{
				var harmony = new Harmony("io.dallen.bannerlord.fastdialogue");
				harmony.PatchAll(typeof(FastDialogueSubModule).Assembly);

				var newSettings = LoadSettingsFor<Settings>(ModuleName);
				if (newSettings != null)
				{
					settings = newSettings;
				}
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

		public bool IsPatternWhitelisted(string name)
		{
			if (settings.whitelist.whitelistPatterns.Count == 0)
			{
				return true;
			}

			foreach (var pattern in settings.whitelist.whitelistPatterns)
			{
				if (name.Contains(pattern))
				{
					return true;
				}
			}

			return false;
		}

		private static T? LoadSettingsFor<T>(string moduleName) where T : class
		{
			string settingsPath = Path.Combine(BasePath.Name, "Modules", moduleName, "settings.xml");
			try
			{
				using (XmlReader reader = XmlReader.Create(settingsPath))
				{
					XmlRootAttribute root = new XmlRootAttribute();
					root.ElementName = moduleName + ".Settings";
					root.IsNullable = true;

					if (reader.MoveToContent() != XmlNodeType.Element)
					{
						return default;
					}

					if (reader.Name != root.ElementName)
					{
						return default;
					}

					XmlSerializer serialiser = new XmlSerializer(typeof(T), root);
					var loaded = (T)serialiser.Deserialize(reader);
					return loaded;
				}
			}
			catch (Exception ex)
			{
				return default;
			}
		}
	}
}