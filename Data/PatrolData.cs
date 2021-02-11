
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Linq;
using System.Text;
using TaleWorlds.CampaignSystem;
using TaleWorlds.ObjectSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace MercLib.Data
{
    //"data manager" system partly taken from Anopey/CustomSpawns github repo
    class PatrolDataManager
    {
        static PatrolDataManager _instance;

        public static PatrolDataManager Instance
        {
            get
            {
                return _instance ?? new PatrolDataManager();
            }
            private set
            {
                _instance = value;
            }
        }

        private List<PatrolData> patrols = new List<PatrolData>();

        public IList<PatrolData> Patrols
        {
            get
            {
                return patrols.AsReadOnly();
            }
        }

        public static void ClearInstance(Main caller)
        {
            if (caller == null)
                return;
            _instance = null;
        }

        private PatrolDataManager()
        {
            string path = Path.Combine(BasePath.Name, "Modules", "MercLib", "MercLib", "DetachmentSignifiers.xml");
            Deserialize(path);
        }

        private void Deserialize(string path)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(path);
            foreach(XmlNode node in doc.DocumentElement)
            {
                if (node.NodeType == XmlNodeType.Comment)
                    continue;

                PatrolData dat = new PatrolData();
                MBObjectManager objManager = Game.Current.ObjectManager;

                if (node.Name == "DetachmentSignifier")
                {
                    dat.templateName = node.Attributes["template"] == null ? "" : node.Attributes["template"].InnerText;
                    dat.name = node.Attributes["name"] == null ? "" : node.Attributes["name"].InnerText;
                    dat.description = node.Attributes["desc"] == null ? "" : node.Attributes["desc"].InnerText;
                    dat.sizes = node.Attributes["sizes"] == null ? "small" : node.Attributes["sizes"].InnerText;

                    dat.basePrice = node.Attributes["base_price"] == null ? 0 : int.Parse(node.Attributes["base_price"].InnerText);
                    dat.priceStep = node.Attributes["price_step"] == null ? 1 : int.Parse(node.Attributes["price_step"].InnerText);
                    dat.culture = (CultureObject)objManager.ReadObjectReferenceFromXml("culture", typeof(CultureObject), node);
                }

                patrols.Add(dat);
            }
        }
    }

    class PatrolData
    {
        public string templateName;
        public string name;
        public string description;
        public string sizes;

        public int basePrice;
        public int priceStep;
        public CultureObject culture;
    }
}
