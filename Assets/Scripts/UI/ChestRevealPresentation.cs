using System;
namespace Emberfall
{
    public enum ChestResultKind { Gold, FirstCollection, Duplicate }
    // Presentation reads the committed receipt; progress/skip never grant a reward.
    public static class ChestRevealPresentation
    {
        public static float Progress(float elapsed,float duration)
        {
            if(float.IsNaN(elapsed)||float.IsNaN(duration)||duration<=0)return 1;
            return Math.Max(0,Math.Min(1,elapsed/duration));
        }
        public static float UnselectedOpacity(float progress){return Math.Max(0,1-Math.Max(0,progress)/.4f);}
        public static ChestResultKind Kind(ChestReward reward)
        {return reward==null||!reward.Rarity.HasValue?ChestResultKind.Gold:reward.Duplicate?ChestResultKind.Duplicate:ChestResultKind.FirstCollection;}
        public static string Outcome(ChestReward reward)
        {
            switch(Kind(reward))
            {case ChestResultKind.FirstCollection:return "首次收藏 · 外观已入藏";case ChestResultKind.Duplicate:return "重复收藏 · 已转金币与星纹";default:return "金币奖励 · 已入账";}
        }
        public static string Result(ChestReward reward,int threads)
        {
            if(reward==null)return "正在读取已保存的奖励";
            string identity=reward.Rarity.HasValue?GameBalance.RarityName(reward.Rarity.Value)+" · "+reward.Name+"\n":"";
            return identity+Outcome(reward)+"\n\n+"+reward.Gold+" 金币\n星纹余额 "+threads;
        }
        public static float DesktopArtSize(float bodyHeight){return Math.Max(0,Math.Min(320,bodyHeight));}
    }
}
