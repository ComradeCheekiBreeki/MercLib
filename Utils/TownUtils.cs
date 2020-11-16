using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;

namespace MercLib.Utils
{
    class TownUtils
    {
        public static float CalculateGarrisonSatsifaction(Town town)
        {
            float generalSatsifaction = town.Loyalty;
            float soldierSatisfaction = 85f;

            float percentOfNonNatives = GetPercentageOfNonNativeGarrisonTroops(town);
            switch (town.GetProsperityLevel())
            {
                case SettlementComponent.ProsperityLevel.Low:
                    soldierSatisfaction -= 40f;
                    break;
                case SettlementComponent.ProsperityLevel.Mid:
                    soldierSatisfaction -= 15f;
                    break;
                case SettlementComponent.ProsperityLevel.High:
                    soldierSatisfaction += 10f;
                    break;
            }

            if (percentOfNonNatives >= 0.66)
            {
                soldierSatisfaction -= 15f;
            }
            else if (percentOfNonNatives >= 0.33)
            {
                soldierSatisfaction -= 5f;
            }
            else
            {
                soldierSatisfaction += 5f;
            }

            if (town.FoodChange <= -40)
            {
                soldierSatisfaction -= 25f;
            }
            else if (town.FoodChange <= -20)
            {
                soldierSatisfaction -= 15f;
            }
            else if (town.FoodChange >= 0)
            {
                soldierSatisfaction -= 0f;
            }
            else
            {
                soldierSatisfaction += 5f;
            }

            return (generalSatsifaction + soldierSatisfaction) / 2;
        }

        public static float GetPercentageOfNonNativeGarrisonTroops(Town town) // ok i know this looks really sus, i don't condone being racist. but people in the middle ages unfortunately were
        {
            int nonNativeTroops = 0;
            foreach(CharacterObject soldier in town.GarrisonParty.MemberRoster.Troops)
            {
                if (soldier.Culture != town.Culture)
                {
                    nonNativeTroops += 1;
                }
            }
            return nonNativeTroops / town.GarrisonParty.MemberRoster.Troops.Count();
        }

        public static int GetNumberOfGarrisonersWillingToJoinAsMercenary(Town town, int midTier, int officerTier, int numOfficerTroops)
        {
            float satisfaction = CalculateGarrisonSatsifaction(town);
            float factor = (satisfaction * 0.12f) / 100f;
            int maxBase = (int)(factor * town.GarrisonParty.MemberRoster.TotalHealthyCount);
            int charCount = 0;
            foreach (CharacterObject c in town.GarrisonParty.MemberRoster.Troops)
            {
                if (charCount < (maxBase - numOfficerTroops))
                {
                    if (c.Tier <= 3)
                    {
                        charCount++;
                    }
                }
                else if (charCount < maxBase)
                {
                    if (c.Tier <= 4)
                    {
                        charCount++;
                    }
                }
                else
                {
                    break;
                }
            }
            return charCount;
        }
    }
}
