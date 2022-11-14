using Helpers;
using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Overlay;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Party;
using StoryMode;

namespace MBFastDialogue.CampaignBehaviors
{
    public class FastDialogueCampaignBehaviorBase : EncounterGameMenuBehavior
    {
        private EncounterGameMenuBehavior GetGlobalCampaignBehaviorManager() => Campaign.Current.GetCampaignBehavior<EncounterGameMenuBehavior>();
        private void Init(MenuCallbackArgs args) =>
             ReflectionUtils.ForceCall<object>(GetGlobalCampaignBehaviorManager(), "game_menu_encounter_on_init", new object[] { args });
        private GameMenuOption.OnConditionDelegate ConditionOf(string name) =>
            (MenuCallbackArgs args) => ReflectionUtils.ForceCall<bool>(GetGlobalCampaignBehaviorManager(), name, new object[] { args });
        private GameMenuOption.OnConsequenceDelegate ConsequenceOf(string name) =>
            (MenuCallbackArgs args) => ReflectionUtils.ForceCall<object>(GetGlobalCampaignBehaviorManager(), name, new object[] { args });

        private bool ShouldShowWarOptions()
        {
            try
            {
                if(PlayerEncounter.EncounteredParty != null && PlayerEncounter.EncounteredParty.Id.Contains("quest_party_template"))
                {
                    return true;
                }
                
                if(PlayerEncounter.EncounteredParty != null && (PlayerEncounter.EncounteredMobileParty != null && (PlayerEncounter.EncounteredMobileParty.StringId.Contains("conspiracy") || PlayerEncounter.EncounteredMobileParty.StringId.Contains("conspirator"))))
                {
                    return true;
                }
                
                if(PlayerEncounter.EncounteredParty != null && PlayerEncounter.EncounteredMobileParty != null && (PlayerEncounter.EncounteredMobileParty.IsCaravan || PlayerEncounter.EncounteredMobileParty.IsVillager) && (PartyBase.MainParty.MapFaction != PlayerEncounter.EncounteredParty.MapFaction))
                {
                    return true;
                }
                return PlayerEncounter.EncounteredParty != null && PartyBase.MainParty.MapFaction.IsAtWarWith(PlayerEncounter.EncounteredParty.MapFaction);
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage("MBFastDialogue generated an exception " + ex.Message, Color.Black));
            }
            return false;
        }

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        }

        private void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
        {
            campaignGameStarter.AddGameMenu(
                FastDialogueSubModule.FastEncounterMenu,
                "{=!}{ENCOUNTER_TEXT}",
                Init,
                GameOverlays.MenuOverlayType.Encounter,
                GameMenu.MenuFlags.None,
                null);
            campaignGameStarter.AddGameMenuOption(
                FastDialogueSubModule.FastEncounterMenu,
                $"{FastDialogueSubModule.FastEncounterMenu}_attack",
                "{=o1pZHZOF}Attack!",
                args =>
                {
                    return ShouldShowWarOptions() && ReflectionUtils.ForceCall<bool>(GetGlobalCampaignBehaviorManager(), "game_menu_encounter_attack_on_condition", new object[] { args });
                },
                ConsequenceOf("game_menu_encounter_attack_on_consequence"),
                false,
                -1,
                false);
            campaignGameStarter.AddGameMenuOption(
                FastDialogueSubModule.FastEncounterMenu,
                $"{FastDialogueSubModule.FastEncounterMenu}_troops",
                "{=QfMeoKOm}Send troops.",
                (args) =>
                {
                    return ShouldShowWarOptions() && ReflectionUtils.ForceCall<bool>(GetGlobalCampaignBehaviorManager(), "game_menu_encounter_order_attack_on_condition", new object[] { args });
                },
                ConsequenceOf("game_menu_encounter_order_attack_on_consequence"),
                false,
                -1,
                false);
            campaignGameStarter.AddGameMenuOption(
                FastDialogueSubModule.FastEncounterMenu,
                $"{FastDialogueSubModule.FastEncounterMenu}_getaway",
                "{=qNgGoqmI}Try to get away.",
                ConditionOf("game_menu_encounter_leave_your_soldiers_behind_on_condition"),
                ConsequenceOf("game_menu_encounter_leave_your_soldiers_behind_accept_on_consequence"),
                false,
                -1,
                false);
            campaignGameStarter.AddGameMenuOption(
                FastDialogueSubModule.FastEncounterMenu,
                $"{FastDialogueSubModule.FastEncounterMenu}_talk",
                "{=OPhlqUVl}Talk",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Conversation;
                    return true;
                },
                args =>
                {
                    PlayerEncounter.DoMeeting();
                },
                false,
                -1,
                false);
            campaignGameStarter.AddGameMenuOption(
                FastDialogueSubModule.FastEncounterMenu,
                $"{FastDialogueSubModule.FastEncounterMenu}_surrend",
                "{=3nT5wWzb}Surrender.",
                ConditionOf("game_menu_encounter_surrender_on_condition"),
                args =>
                {
                    PlayerEncounter.PlayerSurrender = true;
                    PlayerEncounter.Update();
                },
                false,
                -1,
                false);
            campaignGameStarter.AddGameMenuOption(
                FastDialogueSubModule.FastEncounterMenu,
                $"{FastDialogueSubModule.FastEncounterMenu}_leave",
                "{=2YYRyrOO}Leave...",
                ConditionOf("game_menu_encounter_leave_on_condition"),
                (args) =>
                {
                    MenuHelper.EncounterLeaveConsequence(args);
                    if (PartyBase.MainParty.IsMobile && PartyBase.MainParty.MobileParty != null)
                    {
                        PartyBase.MainParty.MobileParty.SetIsDisorganized(false);
                    }
                },
                true,
                -1,
                false);
        }
    }
}