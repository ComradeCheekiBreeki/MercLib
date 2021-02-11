using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Localization;
using MercLib.Data;

namespace MercLib.Utils
{
    class MenuUtils
    {
        // all of this is really really bad but idc
        public static List<InquiryElement> AssemblePatrolSizes(PatrolData dat)
        {
            MBObjectManager objManager = Game.Current.ObjectManager;
            List<InquiryElement> list = new List<InquiryElement>();

            CharacterObject ch1 = objManager.GetObject<CharacterObject>("looter");
            CharacterObject ch2 = objManager.GetObject<CharacterObject>("imperial_recruit");
            CharacterObject ch3 = objManager.GetObject<CharacterObject>("imperial_infantryman");
            CharacterObject ch4 = objManager.GetObject<CharacterObject>("imperial_veteran_infantryman");

            if (dat.sizes.Contains("small"))
                list.Add(new InquiryElement(dat, "Small " + "(" + dat.basePrice.ToString() + " gold)", new ImageIdentifier(CharacterCode.CreateFrom(ch1))));
            if(dat.sizes.Contains("medium"))
                if(Hero.MainHero.Gold >= dat.basePrice + dat.priceStep)
                    list.Add(new InquiryElement(dat, "Medium " + "(" + (dat.basePrice + dat.priceStep).ToString() + " gold)", new ImageIdentifier(CharacterCode.CreateFrom(ch2))));
                else
                    list.Add(new InquiryElement(dat, "Medium " + "(" + (dat.basePrice + dat.priceStep).ToString() + " gold)", new ImageIdentifier(CharacterCode.CreateFrom(ch2)), false, "You do not have enough gold to purchase this size"));
            if (dat.sizes.Contains("large"))
                if (Hero.MainHero.Gold >= dat.basePrice + (dat.priceStep * 2))
                    list.Add(new InquiryElement(dat, "Large " + "(" + (dat.basePrice + (dat.priceStep * 2)).ToString() + " gold)", new ImageIdentifier(CharacterCode.CreateFrom(ch3))));
                else
                    list.Add(new InquiryElement(dat, "Large " + "(" + (dat.basePrice + (dat.priceStep * 2)).ToString() + " gold)", new ImageIdentifier(CharacterCode.CreateFrom(ch3)), false, "You do not have enough gold to purchase this size"));
            if (dat.sizes.Contains("huge"))
                if (Hero.MainHero.Gold >= dat.basePrice + (dat.priceStep * 3))
                    list.Add(new InquiryElement(dat, "Large " + "(" + (dat.basePrice + (dat.priceStep * 3)).ToString() + " gold)", new ImageIdentifier(CharacterCode.CreateFrom(ch4))));
                else
                    list.Add(new InquiryElement(dat, "Large " + "(" + (dat.basePrice + (dat.priceStep * 3)).ToString() + " gold)", new ImageIdentifier(CharacterCode.CreateFrom(ch4)), false, "You do not have enough gold to purchase this size"));

            if (list.Count == 0)
            {
                throw new Exception("Assembling sizes of patrol {" + dat.templateName + "} failed");
            }
            else
            {
                return list;
            }
        }
    }
}
