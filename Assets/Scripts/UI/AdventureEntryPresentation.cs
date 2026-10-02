using System;
namespace Emberfall
{
    // Guaranteed clear rewards are separate from probabilistic enemy equipment.
    public static class AdventureEntryPresentation
    {
        public static int Materials(int mode,int tier)
        {
            if(mode < -1 || mode > 3)throw new ArgumentOutOfRangeException(nameof(mode));
            return TierRewardBand.Materials(mode==-1?3:mode==3?4:mode+1,tier);
        }
        public static string RewardLine(int mode,int tier)
        {return "通关 "+Materials(mode,tier)+"碎片 · "+(mode==-1?"外观宝箱":"无外观宝箱");}
        public static string EncounterLine(int mode)
        {
            switch(mode)
            {
                case -1:return "三波 / 终局首领 · 敌人随机装备";
                case 0:return "守点清敌 / 无首领 · 随机装备";
                case 1:return "限时窄桥 / 无首领 · 随机装备";
                case 2:return "三首领连战 · 首领必掉随机装备";
                case 3:return "五房 / 阶段首领 · 敌人随机装备";
                default:throw new ArgumentOutOfRangeException(nameof(mode));
            }
        }
    }
}
