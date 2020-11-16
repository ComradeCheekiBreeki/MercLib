using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using HarmonyLib;

namespace MercLib
{
    public class Main : MBSubModuleBase
    {

        #region TW Submod integration

        protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
        {
            if (!(game.GameType is Campaign))
                return;
            InitializeGame(game, gameStarterObject);
        }

        #endregion TW Submod integration

        private void InitializeGame(Game game, IGameStarter gameStarter)
        {
            AddBehaviors(gameStarter as CampaignGameStarter);
        }

        private void AddBehaviors(CampaignGameStarter starter)
        {
            starter.AddBehavior(new CampaignBehaviors.GarrisonCommandCampaignBehavior());
            starter.AddBehavior(new CampaignBehaviors.MercenaryRecruitmentFromFactionBehavior());
        }
    }
}
