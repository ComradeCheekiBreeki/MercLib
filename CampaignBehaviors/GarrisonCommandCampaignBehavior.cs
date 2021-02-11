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
using MercLib.Patrols;
using MercLib.Data;

namespace MercLib.CampaignBehaviors
{
    public class GarrisonCommandCampaignBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.AddMenuOptions));
            CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnAfterNewGameCreated));
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData<List<Settlement>>("_mlBribedTowns", ref _mlBribedTowns);
            dataStore.SyncData<Dictionary<string, List<TownPatrolData>>>("_mlTownPatrols", ref _mlTownPatrols);
            // dataStore.SyncData<Dictionary<string, CampaignTime>>("_mlLastRecruitedTownsTime", ref _mlLastRecruitedTownsTime);
        }

        public void OnAfterNewGameCreated(CampaignGameStarter starter)
        {
            this._mlTownPatrols = new Dictionary<string, List<TownPatrolData>>();
        }

        private PatrolDataManager manager;

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
            starter.AddGameMenuOption("ml_garrison_menu", "ml_garrison_leave", "{=ml.leave}Leave.",
                new GameMenuOption.OnConditionDelegate(GarrisonCommandCampaignBehavior.ml_leave_condition),
                (MenuCallbackArgs args) =>
                {
                    GameMenu.SwitchToMenu("town");
                }, true, -1, false);

            #endregion Base

            #region Talking to garrison commander

            starter.AddGameMenu("ml_garrison_talk_commander_menu", "{ML_COMMANDER_INTRO}",
                new OnInitDelegate(GarrisonCommandCampaignBehavior.ml_garrison_commander_intro_menu_init), TaleWorlds.CampaignSystem.Overlay.GameOverlays.MenuOverlayType.SettlementWithBoth, GameMenu.MenuFlags.none, null);
            starter.AddGameMenuOption("ml_garrison_talk_commander_menu", "ml_garrison_commander_buy_patrols", "{=ml.talk.commander.about.patrols}Form a garrison detachment.",
                (MenuCallbackArgs args) =>
                {
                    bool isFull = false;
                    if(!_mlTownPatrols.ContainsKey(Settlement.CurrentSettlement.StringId))
                    {
                        _mlTownPatrols.Add(Settlement.CurrentSettlement.StringId, new List<TownPatrolData>());
                    }
                    else if(_mlTownPatrols[Settlement.CurrentSettlement.StringId].Count() >= TownUtils.GetNumberOfDetachmentsFormable(Settlement.CurrentSettlement.Town))
                    {
                        isFull = true;
                    }
                    return GarrisonCommandCampaignBehavior.ml_ask_garrison_detachment_condition(args, isFull);
                },
                (MenuCallbackArgs args) =>
                {
                    GarrisonCommandCampaignBehavior.ml_ask_garrison_detachment_consequence(args);
                }, false, -1, false);

            #endregion Talking to garrison commander

            #region Buying patrols menu

            starter.AddGameMenu("ml_patrol_buying_menu", "Below is a list of the available types of patrols you can purchase for this settlement. \n \n Settlement patrols: ({ML_GARRISON_PATROL_NUM}/{ML_GARRISON_PATROL_MAX})",
                (MenuCallbackArgs args) =>
                {
                    ml_garrison_buy_patrol_menu_init(args, _mlTownPatrols);
                }, 
                TaleWorlds.CampaignSystem.Overlay.GameOverlays.MenuOverlayType.SettlementWithBoth, GameMenu.MenuFlags.none, null);

            //here we repetedly define menu options in the foreach loop- can lead to probelms down the line if patrols are changed or something, for now it's fine
            foreach(PatrolData dat in manager.Patrols)
            {
                starter.AddGameMenuOption("ml_patrol_buying_menu", "ml_garrison_buy_patrol_" + dat.templateName, "{=ml.buy.text}Buy " + dat.name + " (starting at " + dat.basePrice + "{GOLD_ICON})",
                    (MenuCallbackArgs args) =>
                    {
                        args.optionLeaveType = GameMenuOption.LeaveType.Recruit;
                        args.Tooltip = new TextObject(dat.description);
                        if(Settlement.CurrentSettlement.Culture == dat.culture)
                        {
                            if(Hero.MainHero.Gold >= dat.basePrice)
                            {
                                return true;
                            }
                            args.IsEnabled = false;
                            args.Tooltip = new TextObject("You do not have enough money to buy this patrol.");
                            return true;
                        }
                        return false;
                    },
                    (MenuCallbackArgs args) =>
                    {
                        return;
                    }, false, -1, false);
            }


            #endregion Buying patrols menu

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

        private static bool ml_ask_garrison_detachment_condition(MenuCallbackArgs args, bool isFull)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Manage;
            if(!(Settlement.CurrentSettlement.MapFaction == Hero.MainHero.MapFaction))
            {
                bool playerOwnsWorkshop = false;
                foreach(Workshop w in Settlement.CurrentSettlement.Town.Workshops)
                {
                    if(w.Owner == Hero.MainHero)
                    {
                        playerOwnsWorkshop = true;
                    }
                }
                if(!playerOwnsWorkshop)
                {
                    args.IsEnabled = false;
                    args.Tooltip = new TextObject("You are not affiliated with this settlement.");
                }
            }
            else if(isFull)
            {
                args.IsEnabled = false;
                args.Tooltip = new TextObject("This settlement has reached the maximum number of patrols.");
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

        private static void ml_ask_garrison_detachment_consequence(MenuCallbackArgs args)
        {
            GameMenu.ActivateGameMenu("ml_patrol_buying_menu");
        }

        private void ml_form_garrison_detachment_consequence(MenuCallbackArgs args, PatrolData dat)
        {
            List<InquiryElement> inq = MenuUtils.AssemblePatrolSizes(dat);
            InformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(new TextObject("{=ml.patrol.size.select}Select Size").ToString(), new TextObject("{=ml.patrol.size.select.desc}Select the size of the patrol you want.").ToString(), inq, true, 1, "Name and purchase", "Cancel",
                delegate(List<InquiryElement> selection)
                {
                    if(selection != null)
                    {
                        if(selection.Count != 0)
                        {
                            InformationManager.ShowTextInquiry(new TextInquiryData(new TextObject("{=ml.name.patrol}Select your patrol's name: ", null).ToString(), string.Empty, true, false, GameTexts.FindText("str_done", null).ToString(), null,
                                delegate(string str)
                                {
                                    TownPatrolData newPatrol = SpawnTownPatrol(str, selection[0].Title.ToLower(), dat, true);
                                    this._mlTownPatrols[Settlement.CurrentSettlement.StringId].Add(newPatrol);
                                    InformationManager.HideInquiry();
                                }, null, false, null, "", Settlement.CurrentSettlement.Name.ToString() + " " + dat.name ));
                        }
                    }
                }, 
                delegate(List<InquiryElement> selection)
                {
                    InformationManager.HideInquiry();
                } ));
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
                    MBTextManager.SetTextVariable("ML_GARRISON_SATISFACTION", "of typical military attitude. One or two men curse the commander under their breath, and some look on at the town with displeasure, but generally everyone is adquately fed and well enough off for their status.");
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
                MBTextManager.SetTextVariable("ML_GARRISON_FLAVOR", "a smattering of armed men lounging about, some eating or catching up on sleep.");
        }

        private static void ml_garrison_enter_bribe_menu_init(MenuCallbackArgs args, List<Settlement> bribedSettlements)
        {
            args.MenuTitle = new TextObject("{=ml.garrison.courtyard}Courtyard", null);
            if ((bribedSettlements.Contains(Settlement.CurrentSettlement)) || Hero.MainHero.Clan.Tier > 1)
            {
                GameMenu.SwitchToMenu("ml_garrison_menu");
            }
        }

        private static void ml_garrison_commander_intro_menu_init(MenuCallbackArgs args)
        {
            args.MenuTitle = new TextObject("Commander's Office", null);
            TextObject comment = new TextObject();
            if(Settlement.CurrentSettlement.OwnerClan == Hero.MainHero.Clan)
                comment = new TextObject("{=ml.commander.town.clan}You knock on the door that says 'Commander,' and after a short wait a stout man with misadjusted spectacles comes and quickly hails you before speaking. \n \n 'Nothing to report, your {ML_PLAYER_G_PREFIX}ship. Is there something you need?'", null);
            else if(Settlement.CurrentSettlement.MapFaction == Hero.MainHero.MapFaction)
                comment = new TextObject("{=ml.commander.town.faction}You knock on the door that says 'Commander,' and after a short wait a stout man with misadjusted spectacles steps out and cordially greets you before speaking. \n \n 'Hello, my {ML_PLAYER_G_PREFIX}. I'll forward your business to the steward of this town when he returns. Is there something you need?'", null);
            else
                comment = new TextObject("{=ml.commander.town}You knock on the door that says 'Commander,' and after a short wait a stout man pushes open the door and greets you. \n \n 'Hmm? What is it? I'm a busy man.'", null);
            
            if(Hero.MainHero.IsFemale)
                comment.SetTextVariable("ML_PLAYER_G_PREFIX", "lady");
            else
                comment.SetTextVariable("ML_PLAYER_G_PREFIX", "lord");

            MBTextManager.SetTextVariable("ML_COMMANDER_INTRO", comment);
        }
        private static void ml_garrison_buy_patrol_menu_init(MenuCallbackArgs args, Dictionary<string, List<TownPatrolData>> dict)
        {
            args.MenuTitle = new TextObject("Commander's Office", null);

            MBTextManager.SetTextVariable("ML_GARRISON_PATROL_NUM", dict[Settlement.CurrentSettlement.StringId].Count());
            MBTextManager.SetTextVariable("ML_GARRISON_PATROL_MAX", TownUtils.GetNumberOfDetachmentsFormable(Settlement.CurrentSettlement.Town));
        }

        #endregion Menu inits

        private static TownPatrolData SpawnTownPatrol(string name, string size, PatrolData dat, bool isPlayerSpawn, Settlement spawnSettlement = null)
        {
            MBObjectManager objManager = Game.Current.ObjectManager;
            TextObject pName = new TextObject(name);

            PartyTemplateObject templateObject = (PartyTemplateObject)objManager.GetObject<PartyTemplateObject>(dat.templateName + "_" + size);
            spawnSettlement = isPlayerSpawn ? Settlement.CurrentSettlement : spawnSettlement;

            MobileParty patrol = objManager.CreateObject<MobileParty>(dat.templateName + "_" + size + "_" + 1);
            patrol.InitializeMobileParty(MenuUtils.ConstructTroopRoster(templateObject, patrol.Party), new TroopRoster(patrol.Party), isPlayerSpawn ? Settlement.CurrentSettlement.GatePosition : spawnSettlement.GatePosition, 0);

            patrol.SetCustomName(pName);
            patrol.Party.Owner = spawnSettlement.MapFaction.Leader == null ? spawnSettlement.OwnerClan.Heroes.ToList().First() : spawnSettlement.OwnerClan.Leader;
            patrol.Party.Visuals.SetMapIconAsDirty();
            patrol.ActualClan = spawnSettlement.OwnerClan;
            patrol.HomeSettlement = spawnSettlement;
            MenuUtils.CreatePartyTrade(patrol);

            foreach(ItemObject obj in ItemObject.All)
            {
                if(obj.IsFood)
                {
                    int num = MBRandom.RandomInt(patrol.MemberRoster.TotalManCount / 3, patrol.MemberRoster.TotalManCount);
                    if (num > 0)
                    {
                        patrol.ItemRoster.AddToCounts(obj, num);
                    }
                }
            }

            patrol.SetMovePatrolAroundSettlement(spawnSettlement);

            return new TownPatrolData(pName.ToString(), size, patrol);
        }

        private void GetData()
        {
            manager = PatrolDataManager.Instance;
        }

        private Dictionary<string, List<TownPatrolData>> _mlTownPatrols;

        private List<Settlement> _mlBribedTowns = new List<Settlement>();
    }
}