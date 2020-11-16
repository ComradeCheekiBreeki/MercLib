using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;

namespace MercLib.Utils
{
    class DiplomacyUtils
    {
        public static float GetAverageFactionStrengthToEnemies(IFaction f)
        {
            float totalStrength = 0f;
            int numEntitiesAtWar = 0;
            foreach (Clan c in Clan.All)
            {
                if (c.MapFaction.IsAtWarWith(f))
                {
                    numEntitiesAtWar++;
                    if(c.Kingdom != null)
                    {
                        totalStrength += c.Kingdom.TotalStrength;
                    }
                    else
                    {
                        totalStrength += c.TotalStrength;
                    }
                }
            }
            return totalStrength / numEntitiesAtWar;
        }

        public static bool IsFactionOverallAtDisadvantage(IFaction f)
        {
            float averageStrength = DiplomacyUtils.GetAverageFactionStrengthToEnemies(f);
            int numFactionsAtWarWith = 0;
            int numFactionsAtDisadvantageAgainst = 0;
            foreach (Clan c in Clan.All)
            {
                if (f.IsAtWarWith(c.MapFaction))
                {
                    numFactionsAtWarWith++;
                    if (averageStrength < (DiplomacyUtils.GetAverageFactionStrengthToEnemies(c.MapFaction) * 0.65))
                    {
                        numFactionsAtDisadvantageAgainst++;
                    }
                }
            }
            return (numFactionsAtWarWith / 2) < (numFactionsAtDisadvantageAgainst);
        }
    }
}
