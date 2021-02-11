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
    class MercenaryRecruitmentFromFactionBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnSessionLaunched));
            CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnAfterNewGameCreated));
            CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnAfterNewGameCreated));
        }

        public override void SyncData(IDataStore dataStore)
        {
        }

        public void OnSessionLaunched(CampaignGameStarter starter)
        {
            this.AddDialogs(starter);
        }

        public void OnAfterNewGameCreated(CampaignGameStarter starter)
        {
            _mlLastRecruitedFromMercPartyTime = new Dictionary<string, CampaignTime>();
        }

        protected void AddDialogs(CampaignGameStarter starter)
        {
            starter.AddPlayerLine("ml_mercfaction_question_start", "lord_talk_ask_something_2", "ml_mercfaction_ask_for_men", "{ML_PLAYER_ASK_MERCS}",
            delegate
            {
                return this.ml_ask_for_men_condition();
            }, null, 150, null, null);

            starter.AddDialogLine("ml_mercfaction_response_to_player", "ml_mercfaction_ask_for_men", "ml_mercfaction_player_consider_payment", "{ML_MERCENARY_LORD_RESPONSE}",
            delegate
            {
                bool keyPresent = false;
                if (_mlLastRecruitedFromMercPartyTime.ContainsKey(Hero.OneToOneConversationHero.StringId))
                {
                    keyPresent = true;
                    if (_mlLastRecruitedFromMercPartyTime[Hero.OneToOneConversationHero.StringId].ElapsedDaysUntilNow > 14)
                    {
                        _mlLastRecruitedFromMercPartyTime.Remove(Hero.OneToOneConversationHero.StringId);
                        keyPresent = false;
                    }
                }
                return this.ml_lord_response_to_player_condition(keyPresent);
            }, null, 100, null);
            starter.AddPlayerLine("ml_player_buy_small_group", "ml_mercfaction_player_consider_payment", "ml_player_buy_group", "{=ml.purchase.mercdetachment.sml}I'd like a group of {SM_TROOP_NUM} recruits and {SM_OFF_NUM} officers ({SM_GOLD_AMOUNT}{GOLD_ICON})",
            delegate
            {
                bool keyPresent = true;
                if (!_mlLastRecruitedFromMercPartyTime.ContainsKey(Hero.OneToOneConversationHero.StringId))
                {
                    keyPresent = false;
                }
                return this.ml_player_purchase_small_group_condition(keyPresent);
            },
            delegate
            {
                if (!_mlLastRecruitedFromMercPartyTime.ContainsKey(Hero.OneToOneConversationHero.StringId))
                {
                    _mlLastRecruitedFromMercPartyTime.Add(Hero.OneToOneConversationHero.StringId, CampaignTime.Now);
                }
                this.ml_player_purchase_small_group_consequence();
            }, 100, null, null);
            starter.AddPlayerLine("ml_player_buy_med_group", "ml_mercfaction_player_consider_payment", "ml_player_buy_group", "{=ml.purchase.mercdetachment.mdm}I'd like a group of {MD_TROOP_NUM} recruits and {MD_OFF_NUM} officers ({MD_GOLD_AMOUNT}{GOLD_ICON})",
            delegate
            {
                bool keyPresent = true;
                if (!_mlLastRecruitedFromMercPartyTime.ContainsKey(Hero.OneToOneConversationHero.StringId))
                {
                    keyPresent = false;
                }
                return this.ml_player_purchase_med_group_condition(keyPresent);
            },
            delegate
            {
                if (!_mlLastRecruitedFromMercPartyTime.ContainsKey(Hero.OneToOneConversationHero.StringId))
                {
                    _mlLastRecruitedFromMercPartyTime.Add(Hero.OneToOneConversationHero.StringId, CampaignTime.Now);
                }
                this.ml_player_purchase_medium_group_consequence();
            }, 100, null, null);
            starter.AddPlayerLine("ml_player_leave_buy_menu", "ml_mercfaction_player_consider_payment", "ml_player_leave_buy_tok", "{ML_EXIT_PLAYER_BUY_COMMENT}",
            delegate
            {
                bool keyPresent = true;
                if (!_mlLastRecruitedFromMercPartyTime.ContainsKey(Hero.OneToOneConversationHero.StringId))
                {
                    keyPresent = false;
                }
                return ml_player_leave_purchase_condition(keyPresent);
            }, null, 100, null, null);

            starter.AddDialogLine("ml_mercfaction_player_bought", "ml_player_buy_group", "hero_main_options", "Very well, it's all arranged. They'll be joining you shortly. Now... was there anything else?",
            delegate
            {
                return true;
            }, null, 100, null);
            starter.AddDialogLine("ml_mercfaction_player_didnt_buy", "ml_player_leave_buy_tok", "hero_main_options", "Well alright then. Was there anything else?",
            delegate
            {
                return true;
            }, null, 100, null);
        }

        #region Dialogue conditions

        private bool ml_ask_for_men_condition()
        {
            if (Hero.MainHero.MapFaction.IsAtWarWith(Hero.OneToOneConversationHero.MapFaction))
            {
                return false;
            }
            if (Hero.OneToOneConversationHero.Clan.IsClanTypeMercenary)
            {
                if (Hero.MainHero.MapFaction == Hero.OneToOneConversationHero.MapFaction)
                {
                    MBTextManager.SetTextVariable("ML_PLAYER_ASK_MERCS", "{=ml.player.ask.lord.for.men.1}Can you spare me any of your men?");
                    return true;
                }
                else
                {
                    MBTextManager.SetTextVariable("ML_PLAYER_ASK_MERCS", "{=ml.player.ask.lord.for.men.2}You're mercenary men, do you have any soldiers willing to join me?");
                    return true;
                }
            }
            return false;
        }

        private bool ml_lord_response_to_player_condition(bool keyPresent)
        {
            TextObject text;
            float pRelation = Hero.OneToOneConversationHero.GetRelationWithPlayer();
            bool overallDisadvantaged = DiplomacyUtils.IsFactionOverallAtDisadvantage(Hero.OneToOneConversationHero.MapFaction);
            if (keyPresent)
            {
                text = new TextObject("{=ml.merc.too.soon}Did we not just do business? Give it some time. If I sold you anything more, my army would be wiped out in an instant.", null);
            }
            else if (PlayerEncounter.InsideSettlement)
            {
                text = new TextObject("{=ml.merc.in.the.field}Let us do business in the field, eh? Easier to exchange men out there. Besides, now is my time to relax, and you certainly are intruding.", null);
            }
            else if (MobileParty.ConversationParty.MemberRoster.TotalHealthyCount < 60)
            {
                if (pRelation <= -10)
                    text = new TextObject("{=ml.merc.not.enough.troops.hate}If you can't tell, you fool, I'm not exactly in a posititon to start whoring out my troops. So go away.", null);
                else if (pRelation <= -3)
                    text = new TextObject("{=ml.merc.not.enough.troops.dislike}Well, I'd be compromising my own security if I sold any troops to you right now.", null);
                else
                {
                    text = new TextObject("{=ml.merc.not.enough.troops}I'm not in a position to do so right now. I'd be in a pickle if I had any fewer men than this. Perhaps another lord will be more generous.", null);
                }
            }
            else if ((Hero.MainHero.MapFaction == Hero.OneToOneConversationHero.MapFaction) && overallDisadvantaged)
            {
                if (pRelation <= -10)
                    text = new TextObject("{=ml.merc.in.kindgom.bad.war.hate}You have the audacity to ask me for my own men when we're already knee deep in bodies? Begone from my sight!", null);
                else if (pRelation <= -3)
                    text = new TextObject("{=ml.merc.in.kingdom.bad.war.dislike}Even if I could spare some in the midst of this conflict, I wouldn't trust you to handle them. So the answer is no.", null);
                else
                {
                    text = new TextObject("{=ml.merc.in.kingdom.bad.war}You know as well as I do how strenuous this war is. I'm sorry, but I can't.", null);
                }
            }
            else if (Hero.MainHero.MapFaction == Hero.OneToOneConversationHero.MapFaction)
            {
                if (pRelation <= -10)
                    text = new TextObject("{=ml.merc.in.kindgom.hate}Go bother someone else for these things. I need my men, and you need yours.", null);
                else if (pRelation <= -3)
                    text = new TextObject("{=ml.merc.in.kingdom.dislike}Very well... though, I can't promise you I will have much.", null);
                else
                {
                    text = new TextObject("{=ml.merc.in.kingdom}Of course, friend. How many do you need?", null);
                }
            }
            else if (Hero.OneToOneConversationHero.MapFaction.IsKingdomFaction)
            {
                if (pRelation <= -10)
                    text = new TextObject("{=ml.merc.in.other.kindgom.hate}Not only do I despise you, but you're asking me to betray my contractor's interest. How utterly stupid.", null);
                else if (pRelation <= -3)
                    text = new TextObject("{=ml.merc.in.other.kingdom.dislike}Well, I can't be sure of their purpose, and I don't want to help the enemy. So no.", null);
                else
                {
                    text = new TextObject("{=ml.merc.in.other.kingdom}I would, but my contractual duty binds me to this kingdom. Maybe in the future, perhaps.", null);
                }
            }
            else
            {
                if (pRelation <= -10)
                    text = new TextObject("{=ml.merc.in.other.kindgom.hate}I wouldn't entrust domestic duties to you, let alone a detachment of my own men. So no.", null);
                else if (pRelation <= -3)
                    text = new TextObject("{=ml.merc.in.other.kingdom.dislike}Very well, but I must send you with a retainer. He will ensure my men are treated fairly.", null);
                else
                {
                    text = new TextObject("{=ml.merc.in.other.kingdom}We could strike a deal. What forces do you require?", null);
                }
            }
            MBTextManager.SetTextVariable("ML_MERCENARY_LORD_RESPONSE", text);
            return true;
        }

        private bool ml_player_purchase_small_group_condition(bool keyPresent)
        {
            float pRelation = Hero.OneToOneConversationHero.GetRelationWithPlayer();
            bool overallDisadvantaged = DiplomacyUtils.IsFactionOverallAtDisadvantage(Hero.OneToOneConversationHero.MapFaction);
            if (keyPresent)
            {
                return false;
            }
            else if (PlayerEncounter.InsideSettlement)
            {
                return false;
            }
            else if (MobileParty.ConversationParty.MemberRoster.TotalHealthyCount < 60)
            {
                return false;
            }
            else if ((Hero.MainHero.MapFaction == Hero.OneToOneConversationHero.MapFaction) && overallDisadvantaged)
            {
                return false;
            }
            else if ((Hero.MainHero.MapFaction == Hero.OneToOneConversationHero.MapFaction) && (pRelation <= -10))
            {
                return false;
            }
            else if (Hero.OneToOneConversationHero.MapFaction.IsKingdomFaction)
            {
                return false;
            }
            else if (pRelation <= -10)
            {
                return false;
            }
            else
            {
                int num = (int)(MobileParty.ConversationParty.MemberRoster.TotalHealthyCount * 0.06f);
                MBTextManager.SetTextVariable("SM_TROOP_NUM", num);

                if ((int)(num * 0.1f) >= 1)
                    MBTextManager.SetTextVariable("SM_OFF_NUM", (int)(num * 0.1f));
                else
                    MBTextManager.SetTextVariable("SM_OFF_NUM", "no");

                MBTextManager.SetTextVariable("SM_GOLD_AMOUNT", num * 45);
                return (num > 0) && (Hero.MainHero.Gold > num * 45);
            }
        }

        private bool ml_player_purchase_med_group_condition(bool keyPresent)
        {
            float pRelation = Hero.OneToOneConversationHero.GetRelationWithPlayer();
            bool overallDisadvantaged = DiplomacyUtils.IsFactionOverallAtDisadvantage(Hero.OneToOneConversationHero.MapFaction);
            if (keyPresent)
            {
                return false;
            }
            else if (PlayerEncounter.InsideSettlement)
            {
                return false;
            }
            else if (MobileParty.ConversationParty.MemberRoster.TotalHealthyCount < 60)
            {
                return false;
            }
            else if ((Hero.MainHero.MapFaction == Hero.OneToOneConversationHero.MapFaction) && overallDisadvantaged)
            {
                return false;
            }
            else if ((Hero.MainHero.MapFaction == Hero.OneToOneConversationHero.MapFaction) && (pRelation <= -10))
            {
                return false;
            }
            else if (Hero.OneToOneConversationHero.MapFaction.IsKingdomFaction)
            {
                return false;
            }
            else if (pRelation <= -10)
            {
                return false;
            }
            else
            {
                int num = (int)(MobileParty.ConversationParty.MemberRoster.TotalHealthyCount * 0.12f);
                MBTextManager.SetTextVariable("MD_TROOP_NUM", num);

                if ((int)(num * 0.1f) >= 1)
                    MBTextManager.SetTextVariable("MD_OFF_NUM", (int)(num * 0.1f));
                else
                    MBTextManager.SetTextVariable("MD_OFF_NUM", "no");

                MBTextManager.SetTextVariable("MD_GOLD_AMOUNT", num * 45);
                return (num > 0) && (Hero.MainHero.Gold > num * 45);
            }
        }

        private bool ml_player_leave_purchase_condition(bool keyPresent)
        {
            float pRelation = Hero.OneToOneConversationHero.GetRelationWithPlayer();
            bool overallDisadvantaged = DiplomacyUtils.IsFactionOverallAtDisadvantage(Hero.OneToOneConversationHero.MapFaction);
            if (keyPresent)
            {
                MBTextManager.SetTextVariable("ML_EXIT_PLAYER_BUY_COMMENT", "Another time, then.");
            }
            else if(PlayerEncounter.InsideSettlement)
            {
                MBTextManager.SetTextVariable("ML_EXIT_PLAYER_BUY_COMMENT", "See you in the field, then.");
            }
            else if (MobileParty.ConversationParty.MemberRoster.TotalHealthyCount < 60)
            {
                MBTextManager.SetTextVariable("ML_EXIT_PLAYER_BUY_COMMENT", "Very well.");
            }
            else if ((Hero.MainHero.MapFaction == Hero.OneToOneConversationHero.MapFaction) && overallDisadvantaged)
            {
                MBTextManager.SetTextVariable("ML_EXIT_PLAYER_BUY_COMMENT", "These are bad times.");
            }
            else if ((Hero.MainHero.MapFaction == Hero.OneToOneConversationHero.MapFaction) && (pRelation <= -10))
            {
                MBTextManager.SetTextVariable("ML_EXIT_PLAYER_BUY_COMMENT", "No need for the attitude.");
            }
            else if (Hero.OneToOneConversationHero.MapFaction.IsKingdomFaction)
            {
                MBTextManager.SetTextVariable("ML_EXIT_PLAYER_BUY_COMMENT", "If the secrets of the state are so important...");
            }
            else if (pRelation <= -10)
            {
                MBTextManager.SetTextVariable("ML_EXIT_PLAYER_BUY_COMMENT", "No need for the attitude..");
            }
            else
            {
                MBTextManager.SetTextVariable("ML_EXIT_PLAYER_BUY_COMMENT", "On second thought, I think I will get by.");
            }
            return true;
        }

        #endregion Dialogue conditions

        #region Dialogue consequences

        private void ml_player_purchase_small_group_consequence()
        {
            int highestTier = 1;
            int lowestTier = 15;
            TroopRoster roster = new TroopRoster(PartyBase.MainParty);
            List<CharacterObject> baseChars = new List<CharacterObject>();
            List<CharacterObject> offChars = new List<CharacterObject>();
            int num = (int)(MobileParty.ConversationParty.MemberRoster.TotalHealthyCount * 0.06f);
            int numOfficers = (int)(num * 0.1f);

            foreach(PartyTemplateStack s in Hero.OneToOneConversationHero.Clan.DefaultPartyTemplate.Stacks)
            {
                if(s.Character.Tier < lowestTier)
                    lowestTier = s.Character.Tier;
                if (s.Character.Tier > highestTier)
                    highestTier = s.Character.Tier;
            }

            foreach (PartyTemplateStack s in Hero.OneToOneConversationHero.Clan.DefaultPartyTemplate.Stacks)
            {
                if (s.Character.Tier == lowestTier)
                    baseChars.Add(s.Character);
                else if (s.Character.Tier == highestTier)
                    offChars.Add(s.Character);
            }

            MobileParty.MainParty.AddElementToMemberRoster(baseChars.GetRandomElement(), num, false);
            MobileParty.MainParty.AddElementToMemberRoster(offChars.GetRandomElement(), numOfficers, false);

            Dictionary<CharacterObject, int> toRemove = new Dictionary<CharacterObject, int>();
            int i = 0;
            foreach (TroopRosterElement c in MobileParty.ConversationParty.MemberRoster.ToList())
            {
                if (i < num)
                {
                    if (c.Character.Tier == lowestTier)
                    {
                        if(c.Number < (num + i))
                        {
                            toRemove.Add(c.Character, c.Number);
                            i = i + c.Number;
                        }
                        else
                        {
                            toRemove.Add(c.Character, num - i);
                            i = num + 1;
                        }
                    }
                }
                else
                {
                    break;
                }
            }
            foreach (KeyValuePair<CharacterObject, int> k in toRemove)
            {
                MobileParty.ConversationParty.MemberRoster.RemoveTroop(k.Key, k.Value);
            }

            GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, Hero.OneToOneConversationHero, num * 45, false);
        }

        private void ml_player_purchase_medium_group_consequence()
        {
            int highestTier = 1;
            int lowestTier = 15;
            List<CharacterObject> baseChars = new List<CharacterObject>();
            List<CharacterObject> offChars = new List<CharacterObject>();
            int num = (int)(MobileParty.ConversationParty.MemberRoster.TotalHealthyCount * 0.12f);
            int numOfficers = (int)(num * 0.1f);

            foreach (PartyTemplateStack s in Hero.OneToOneConversationHero.Clan.DefaultPartyTemplate.Stacks)
            {
                if (s.Character.Tier < lowestTier)
                    lowestTier = s.Character.Tier;
                if (s.Character.Tier > highestTier)
                    highestTier = s.Character.Tier;
            }

            foreach (PartyTemplateStack s in Hero.OneToOneConversationHero.Clan.DefaultPartyTemplate.Stacks)
            {
                if (s.Character.Tier == lowestTier)
                    baseChars.Add(s.Character);
                else if (s.Character.Tier == highestTier)
                    offChars.Add(s.Character);
            }

            MobileParty.MainParty.AddElementToMemberRoster(baseChars.GetRandomElement(), num, false);
            MobileParty.MainParty.AddElementToMemberRoster(offChars.GetRandomElement(), numOfficers, false);

            Dictionary<CharacterObject, int> toRemove = new Dictionary<CharacterObject, int>();
            int i = 0;
            foreach (TroopRosterElement c in MobileParty.ConversationParty.MemberRoster.ToList())
            {
                if (i < num)
                {
                    if (c.Character.Tier == lowestTier)
                    {
                        if (c.Number < (num + i))
                        {
                            toRemove.Add(c.Character, c.Number);
                            i = i + c.Number;
                        }
                        else
                        {
                            toRemove.Add(c.Character, num - i);
                            i = num + 1;
                        }
                    }
                }
                else
                {
                    break;
                }
            }
            foreach (KeyValuePair<CharacterObject, int> k in toRemove)
            {
                MobileParty.ConversationParty.MemberRoster.RemoveTroop(k.Key, k.Value);
            }

            GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, Hero.OneToOneConversationHero, num * 45, false);
        }

        #endregion Dialogue consequences

        private Dictionary<string, CampaignTime> _mlLastRecruitedFromMercPartyTime;
    }
}
