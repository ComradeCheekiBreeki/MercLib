using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.Localization;
using MercLib.Utils;

namespace MercLib.CampaignBehaviors
{
    public class GarrisonCommandCampaignBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.AddMenuOptions));
            CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnAfterNewGameCreated));
            CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnAfterNewGameCreated));
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData<List<Settlement>>("_mlBribedTowns", ref _mlBribedTowns);
            dataStore.SyncData<Dictionary<string, CampaignTime>>("_mlLastRecruitedTownsTime", ref _mlLastRecruitedTownsTime);
        }

        public void OnAfterNewGameCreated(CampaignGameStarter starter)
        {
            this._mlLastRecruitedTownsTime = new Dictionary<string, CampaignTime>();
        }

        public void AddMenuOptions(CampaignGameStarter starter)
        {

            #region Entering garrison headquarters

            starter.AddGameMenuOption("town", "ml_go_to_garrison", "{=ml.garrison.start}Go to the garrison headquarters", 
                new GameMenuOption.OnConditionDelegate(GarrisonCommandCampaignBehavior.ml_menu_enter_garrison_condition),
                (MenuCallbackArgs args) =>
                {
                    GarrisonCommandCampaignBehavior.ml_menu_start_enter_garrison_consequence(args, _mlBribedTowns);
                }, false, 4, false);

            #region Bribing

            starter.AddGameMenu("ml_garrison_bribe_enter", "{=ml.garrison.guards.refuse}The guards inform you that you're not allowed to enter and waive you off.",
                (MenuCallbackArgs args) =>
                {
                    GarrisonCommandCampaignBehavior.ml_garrison_enter_bribe_menu_init(args, _mlBribedTowns);
                }, 
                TaleWorlds.CampaignSystem.Overlay.GameOverlays.MenuOverlayType.SettlementWithBoth, GameMenu.MenuFlags.none, null);
            starter.AddGameMenuOption("ml_garrison_bribe_enter", "ml_bribe_guards_into_garrison", "{=ml.bribe.guards}Try to bribe the guards with {GOLD_ICON}{ML_BRIBE_AMOUNT}.",
                new GameMenuOption.OnConditionDelegate(GarrisonCommandCampaignBehavior.ml_menu_bribe_guards_condition),
                (MenuCallbackArgs args) =>
                {
                    GarrisonCommandCampaignBehavior.ml_menu_bribe_guards_consequence(args);
                    _mlBribedTowns.Add(Settlement.CurrentSettlement);
                }, false, -1, false);
            starter.AddGameMenuOption("ml_garrison_bribe_enter", "ml_bribe_guards_leave", "{=ml.leave}Leave.",
                new GameMenuOption.OnConditionDelegate(GarrisonCommandCampaignBehavior.ml_leave_condition),
                (MenuCallbackArgs args) =>
                {
                    GameMenu.SwitchToMenu("town");
                }, true, -1, false);

            #endregion Bribing

            #endregion Entering garrison headquarters

            #region Garrison menu

            #region Base

            starter.AddGameMenu("ml_garrison_menu", "{=ml.garrison.flavortext}You step into the garrison headquarters of {ML_GARRISON_TOWN_NAME}, a {ML_GARRISON_DESC} adjacent to the keep. Inside you can see {ML_GARRISON_FLAVOR} \n \n You can gauge the air in the room is one {ML_GARRISON_SATISFACTION}",
                new OnInitDelegate(GarrisonCommandCampaignBehavior.ml_town_init_garrison_menu), TaleWorlds.CampaignSystem.Overlay.GameOverlays.MenuOverlayType.SettlementWithBoth, GameMenu.MenuFlags.none, null);
            starter.AddGameMenuOption("ml_garrison_menu", "ml_garrison_ask_to_recruit", "{=ml.ask.garrison.recruits}Ask for mercenary volunteers.",
                (MenuCallbackArgs args) =>
                {
                    if (_mlLastRecruitedTownsTime.ContainsKey(Settlement.CurrentSettlement.StringId))
                    {
                        if (_mlLastRecruitedTownsTime[Settlement.CurrentSettlement.StringId].ElapsedDaysUntilNow > 12)
                        {
                            _mlLastRecruitedTownsTime.Remove(Settlement.CurrentSettlement.StringId);
                        }
                    }
                    return GarrisonCommandCampaignBehavior.ml_garrison_recruitment_condition(args, _mlLastRecruitedTownsTime);
                },
                (MenuCallbackArgs args) =>
                {
                    GarrisonCommandCampaignBehavior.ml_menu_ask_recruits_garrison_consequence(args);
                }, false, -1, false);
            starter.AddGameMenuOption("ml_garrison_menu", "ml_garrison_leave", "{=ml.leave}Leave.",
                new GameMenuOption.OnConditionDelegate(GarrisonCommandCampaignBehavior.ml_leave_condition),
                (MenuCallbackArgs args) =>
                {
                    GameMenu.SwitchToMenu("town");
                }, true, -1, false);

            #endregion Base

            #region Mercenary recruitment

            starter.AddGameMenu("ml_garrison_trooptalk_menu", "{=ml.garrison.recruitmentarea}You approach a group of mercenary men chatting in the corner. When you inquire, their captain, {ML_OFFICER}, greets you and says: \n \n '{CAPTAIN_COMMENT}'",
                new OnInitDelegate(GarrisonCommandCampaignBehavior.ml_garrison_trooptalk_menu_init), TaleWorlds.CampaignSystem.Overlay.GameOverlays.MenuOverlayType.SettlementWithBoth, GameMenu.MenuFlags.none, null);
            starter.AddGameMenuOption("ml_garrison_trooptalk_menu", "ml_garrison_trooptalk_agree_pay", "{=ml.pay.garrison.troops}Agree and pay {GOLD_ICON}{ML_TROOPS_COST}.",
                new GameMenuOption.OnConditionDelegate(GarrisonCommandCampaignBehavior.ml_agree_pay_troop_cost_condition),
                (MenuCallbackArgs args) =>
                {
                    GarrisonCommandCampaignBehavior.ml_menu_accept_recruits_pay_consequence(args);
                    if (!this._mlLastRecruitedTownsTime.ContainsKey(Settlement.CurrentSettlement.StringId))
                    {
                        this._mlLastRecruitedTownsTime.Add(Settlement.CurrentSettlement.StringId, CampaignTime.Now);
                    }
                }, false, -1, false);
            starter.AddGameMenuOption("ml_garrison_trooptalk_menu", "ml_garrison_trooptalk_leave", "{=ml.leave}Leave.",
                new GameMenuOption.OnConditionDelegate(GarrisonCommandCampaignBehavior.ml_leave_condition),
                (MenuCallbackArgs args) =>
                {
                    GameMenu.SwitchToMenu("town");
                }, true, -1, false);

            #endregion Mercenary recruitment

            #endregion Garrison menu
        }

        #region Menu option conditions

        private static bool ml_menu_enter_garrison_condition(MenuCallbackArgs args)
        {
            SettlementAccessModel.AccessDetails access;
            Campaign.Current.Models.SettlementAccessModel.CanMainHeroEnterKeep(Settlement.CurrentSettlement, out access);
            if (access.AccessLevel == SettlementAccessModel.AccessLevel.NoAccess)
            {
                SettlementAccessModel.AccessLimitationReason limitReason = access.AccessLimitationReason;
                if (limitReason == SettlementAccessModel.AccessLimitationReason.HostileFaction)
                {
                    args.IsEnabled = false;
                    if (limitReason == SettlementAccessModel.AccessLimitationReason.Disguised)
                        args.Tooltip = new TextObject("{=ml.garrison.noentry.disguised}Entering the garrison headquarters while disguised would draw unnecessary suspicion.", null);
                    else
                        args.Tooltip = new TextObject("{=ml.garrison.cannot.enter}You cannot enter an enemy garrison headquarters.", null);
                }
            }
            if (Settlement.CurrentSettlement.IsUnderSiege)
            {
                args.IsEnabled = false;
                args.Tooltip = new TextObject("{ml.garrison.under.siege}The settlement is under siege.");
            }
            args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
            return true;
        }

        private static bool ml_menu_bribe_guards_condition(MenuCallbackArgs args)
        {
            int bribe = Campaign.Current.Models.BribeCalculationModel.GetBribeToEnterLordsHall(Settlement.CurrentSettlement);
            if(bribe == 0)
            {
                bribe = 500;
            }
            MBTextManager.SetTextVariable("ML_BRIBE_AMOUNT", bribe);
            if(Hero.MainHero.Gold < bribe)
            {
                args.IsEnabled = false;
                args.Tooltip = new TextObject("{=ml.not.enough.gold}You don't have enough gold.", null);
            }
            args.optionLeaveType = GameMenuOption.LeaveType.Mission;
            return true;
        }

        private static bool ml_garrison_recruitment_condition(MenuCallbackArgs args, Dictionary<string, CampaignTime> dict)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Recruit;
            if(Settlement.CurrentSettlement.Town.Security <= 40)
            {
                args.IsEnabled = false;
                args.Tooltip = new TextObject("{=ml.tooltip.low.security}The garrison cannot spare troops because security is too low.", null);
            }
            else if (dict.ContainsKey(Settlement.CurrentSettlement.StringId))
            {
                if(dict[Settlement.CurrentSettlement.StringId].ElapsedDaysUntilNow <= 12)
                {
                    args.IsEnabled = false;
                    args.Tooltip = new TextObject("{=ml.recruited.too.recently}You have recently conscripted troops from this garrison.");
                }
            }
            return Settlement.CurrentSettlement.OwnerClan != Clan.PlayerClan;
        }

        private static bool ml_agree_pay_troop_cost_condition(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.RansomAndBribe;
            int numWillingToJoin = TownUtils.GetNumberOfGarrisonersWillingToJoinAsMercenary(Settlement.CurrentSettlement.Town, 3, 4, 2);
            MBTextManager.SetTextVariable("ML_TROOPS_COST", (numWillingToJoin * 80));
            if(Hero.MainHero.Gold < (numWillingToJoin * 80))
            {
                args.IsEnabled = false;
                args.Tooltip = new TextObject("{=ml.not.enough.gold}You don't have enough gold.", null);
            }
            return true;
        }

        private static bool ml_leave_condition(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Leave;
            return true;
        }

        #endregion Menu option conditions

        #region Menu option consequences

        private static void ml_menu_start_enter_garrison_consequence(MenuCallbackArgs args, List<Settlement> bribedSettlements)
        {
            if ((Hero.MainHero.Clan.Tier <= 1) && !(bribedSettlements.Contains(Settlement.CurrentSettlement)))
            {
                GameMenu.ActivateGameMenu("ml_garrison_bribe_enter");
            }
            else
            {
                GameMenu.ActivateGameMenu("ml_garrison_menu");
            }
        }

        private static void ml_menu_bribe_guards_consequence(MenuCallbackArgs args)
        {
            int bribe = Campaign.Current.Models.BribeCalculationModel.GetBribeToEnterLordsHall(Settlement.CurrentSettlement);
            if (bribe == 0)
            {
                bribe = 500;
            }
            GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, bribe, false);
            GameMenu.ActivateGameMenu("ml_garrison_menu");
        }

        private static void ml_menu_ask_recruits_garrison_consequence(MenuCallbackArgs args)
        {
            GameMenu.ActivateGameMenu("ml_garrison_trooptalk_menu");
        }

        private static void ml_menu_accept_recruits_pay_consequence(MenuCallbackArgs args)
        {
            TroopRoster roster = new TroopRoster(PartyBase.MainParty);
            int numWillingToJoin = TownUtils.GetNumberOfGarrisonersWillingToJoinAsMercenary(Settlement.CurrentSettlement.Town, 3, 4, 2);
            List<CharacterObject> chars = new List<CharacterObject>();
            foreach (CharacterObject c in Settlement.CurrentSettlement.Town.GarrisonParty.MemberRoster.Troops)
            {
                if(chars.Count < (numWillingToJoin - 2))
                {
                    if (c.Tier <= 3)
                    {
                        chars.Add(c);
                    }
                }
                else if(chars.Count < numWillingToJoin)
                {
                    if (c.Tier <= 4)
                    {
                        chars.Add(c);
                    }
                }
                else
                {
                    break;
                }
            }
            foreach(CharacterObject c in chars)
            {
                roster.AddToCounts(c, 1, false);
                Settlement.CurrentSettlement.Town.GarrisonParty.MemberRoster.RemoveTroop(c);
            }
            MobileParty.MainParty.MemberRoster.Add(roster);
            GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, numWillingToJoin * 80, false);
            GameMenu.SwitchToMenu("town");
        }

        #endregion Menu option consequences

        #region Menu inits

        private static void ml_town_init_garrison_menu(MenuCallbackArgs args)
        {
            args.MenuTitle = new TextObject("{ml.garrison.title}Garrison Headquarters", null);
            MBTextManager.SetTextVariable("ML_GARRISON_TOWN_NAME", Settlement.CurrentSettlement.Name, false);
            float garrisonSatsifaction = TownUtils.CalculateGarrisonSatsifaction(Settlement.CurrentSettlement.Town);
            switch(Settlement.CurrentSettlement.Town.GetProsperityLevel())
            {
                case SettlementComponent.ProsperityLevel.Low:
                    MBTextManager.SetTextVariable("ML_GARRISON_DESC", "modest wooden stockade");
                    break;
                case SettlementComponent.ProsperityLevel.Mid:
                    MBTextManager.SetTextVariable("ML_GARRISON_DESC", "stout wooden blockhouse");
                    break;
                case SettlementComponent.ProsperityLevel.High:
                    MBTextManager.SetTextVariable("ML_GARRISON_DESC", "large stone towerhouse");
                    break;
            }

            switch(Settlement.CurrentSettlement.Town.GetProsperityLevel())
            {
                case SettlementComponent.ProsperityLevel.Low:
                    MBTextManager.SetTextVariable("ML_GARRISON_SATISFACTION", "of destitution. The soldiers look underfed and glance longingly at their empty coin pouches between swigs of cheap alcohol. Some men curse out their commanders and lords in the corner.");
                    break;
                case SettlementComponent.ProsperityLevel.Mid:
                    MBTextManager.SetTextVariable("ML_GARRISON_SATISFACTION", "of typical military attitude. One or two men curse the commander under their breath, and some look on at the town with displeasure, but generally everyone is adquately fed and well enough off.");
                    break;
                case SettlementComponent.ProsperityLevel.High:
                    MBTextManager.SetTextVariable("ML_GARRISON_SATISFACTION", "of general happiness and camraderie. The men look content and well-fed, and they sling jokes back and forth. Everyone seems like friends.");
                    break;
            }

            float num = MBRandom.RandomFloatRanged(0, 1f);

            if (num <= 0.25)
                MBTextManager.SetTextVariable("ML_GARRISON_FLAVOR", "soldiers milling about, playing board games and catching up on the latest gossip.");
            else if (num <= 0.5)
                MBTextManager.SetTextVariable("ML_GARRISON_FLAVOR", "soldiers and stewards moving about on duty. On the officer's desk, there's a colossal stack of parchments.");
            else if (num <= 0.75)
                MBTextManager.SetTextVariable("ML_GARRISON_FLAVOR", "a handful of guards chatting in a corner, their comrades diligently watching the ramparts just outside.");
            else
                MBTextManager.SetTextVariable("ML_GARRISON_FLAVOR", "a smattering of armed men lounging about, drinking and eating, sleeping and even smoking.");
        }

        private static void ml_garrison_enter_bribe_menu_init(MenuCallbackArgs args, List<Settlement> bribedSettlements)
        {
            args.MenuTitle = new TextObject("{=ml.garrison.courtyard}Courtyard", null);
            if ((bribedSettlements.Contains(Settlement.CurrentSettlement)) || Hero.MainHero.Clan.Tier > 1)
            {
                GameMenu.SwitchToMenu("ml_garrison_menu");
            }
        }

        private static void ml_garrison_trooptalk_menu_init(MenuCallbackArgs args)
        {
            TextObject text = new TextObject("", null);
            CharacterObject captain;
            List<CharacterObject> officerChars = new List<CharacterObject>();
            args.MenuTitle = new TextObject("{ml.garrison.title}Garrison Headquarters", null);
            foreach (CharacterObject obj in Settlement.CurrentSettlement.Town.GarrisonParty.MemberRoster.Troops)
            {
                if (obj.Tier >= 5)
                {
                    officerChars.Add(obj);
                }
            }
            if(officerChars.Count == 0)
            {
                officerChars.Add(MBObjectManager.Instance.GetObject<CharacterObject>("imperial_menavliaton"));
            }
            captain = officerChars.GetRandomElement();
            MBTextManager.SetTextVariable("ML_OFFICER", captain.EncyclopediaLinkWithName);
            int numWillingToJoin = TownUtils.GetNumberOfGarrisonersWillingToJoinAsMercenary(Settlement.CurrentSettlement.Town, 3, 4, 2);
            MBTextManager.SetTextVariable("ML_TROOP_COUNT", numWillingToJoin);

            float num = MBRandom.RandomFloatRanged(0f, 1.0f);
            if (numWillingToJoin > 0)
            {
                if (num <= 0.25)
                {
                    text = new TextObject("{=ml.officer.comment.1}Yes, me and my boys' tenures are almost up. Us {ML_COUNT} would be happy to join you, for a fee.");
                }
                else if (num <= 0.5)
                {
                    text = new TextObject("{=ml.officer.comment.2}Looking for men, sir? We're {ML_COUNT} all together, I bet we could do you well as fighters.");
                }
                else if (num <= 0.75)
                {
                    text = new TextObject("{=ml.officer.comment.3}Lucky you caught us, our contract is just about expired. We number {ML_COUNT} in all, how about you take us on?");
                }
                else
                {
                    text = new TextObject("{=ml.officer.comment.4}Right, well me and {ML_COUNT} of my men would like to join you. How about it?");
                }
                text.SetTextVariable("ML_COUNT", numWillingToJoin);
                MBTextManager.SetTextVariable("CAPTAIN_COMMENT", text);
            }
            else
            {
                TextObject text2 = new TextObject("{= ml.no.troops.available }Well, we're fighting men alright, but we can't spare our contracts. Besides, we wouldn't dare disobey the {RULER_TITLE}.");
                string rtext = Settlement.CurrentSettlement.MapFaction.Culture.StringId;
                if(Settlement.CurrentSettlement.MapFaction.Leader.IsFemale)
                {
                    rtext += "_f";
                }
                text2.SetTextVariable("RULER_TITLE", GameTexts.FindText("str_faction_ruler", rtext));
                MBTextManager.SetTextVariable("CAPTAIN_COMMENT", text2);
            }
        }

        #endregion Menu inits

        private Dictionary<string, CampaignTime> _mlLastRecruitedTownsTime;

        private List<Settlement> _mlBribedTowns = new List<Settlement>();
    }
}