using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;

namespace MercLib.Patrols
{
    class TownPatrolData
    {
        public TownPatrolData(string pName, string pSize, MobileParty party)
        {
            name = pName;
            size = pSize;
            associatedParty = party;
        }

        public string name { get; set; }
        public string size { get; set; }
        public MobileParty associatedParty { get; set; }
    }
}
