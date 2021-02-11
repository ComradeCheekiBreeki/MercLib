using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

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

        public static float GetPercentageOfNonNativeGarrisonTroops(Town town) // i don't condone being racist but it's the middle ages
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

        public static int GetNumberOfDetachmentsFormable(Town town)
        {
            switch(town.GetProsperityLevel())
            {
                case SettlementComponent.ProsperityLevel.Low:
                    return 2;
                case SettlementComponent.ProsperityLevel.Mid:
                    return 3;
                case SettlementComponent.ProsperityLevel.High:
                    return 5;
            }
            return 1;
        }

        /* public static int GetNumberOfGarrisonersWillingToJoinAsMercenary(Town town, int midTier, int officerTier, int numOfficerTroops)
        {
            float satisfaction = CalculateGarrisonSatsifaction(town);
            float factor = (satisfaction * 0.12f) / 100f;
            int maxBase = (int)(factor * town.GarrisonParty.MemberRoster.TotalHealthyCount);

            int charCount = 0;
            int remainder = 0;
            foreach (TroopRosterElement t in town.GarrisonParty.MemberRoster.ToList())
            {
                if(t.Character.Tier == midTier)
                {

                }
            }
            return charCount;
        } does not work */
    }
}
