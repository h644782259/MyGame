using System.Globalization;
namespace Emberfall
{
    public static class ChapterEntryPresentation
    {
        public static string DifficultyName(ChapterDifficulty difficulty)
        {return difficulty==ChapterDifficulty.Hard?"困难":difficulty==ChapterDifficulty.Heroic?"英雄":"普通";}
        public static string Story(ChapterNode node)
        {
            var definition=ChapterDefinition.Get(node);
            return "观星员的线索 · "+definition.StoryIntro+"\n目标 · "+definition.Mechanic+"\n完成后 · "+definition.Outcome+"\n下一线索 · "+definition.NextClue;
        }
        public static string TierEffect(ChapterNode node)
        {return node==ChapterNode.StarPlatform?"星台通关推进共享最高阶，解锁下一阶。":"此节点推进故事与本节点难度，不推进共享最高阶。";}
        public static string Preview(GameProfile profile,ChapterNode node,ChapterDifficulty difficulty,int tier,bool limited)
        {
            string health=ChapterDefinition.HealthMultiplier(difficulty).ToString("0.##",CultureInfo.InvariantCulture);
            string damage=ChapterDefinition.DamageMultiplier(difficulty).ToString("0.##",CultureInfo.InvariantCulture);
            int repeat=ChapterProgression.MaterialReward(node,tier),total=ChapterProgression.CompletionMaterials(profile,node,tier);
            return DifficultyName(difficulty)+" · 敌人生命 ×"+health+" / 伤害 ×"+damage+"\n"+
                ChapterDefinition.DifficultyMechanic(node,difficulty)+"\n"+
                "普通通关解锁困难，困难通关解锁英雄；阶数和治疗规则独立。\n"+
                (limited?"限疗：初始3次治疗充能。":"普通治疗：使用携带药剂。")+"\n"+
                "完成奖励 "+total+" 碎片（重复 "+repeat+(total>repeat?" + 首次1":"")+"）；难度不加乘，无全局首通核心或宝箱。\n"+TierEffect(node);
        }
        public static string Result(ChapterNode node,bool failed,bool pending)
        {
            if(failed)return "本次没有完成节点，故事与难度进度未推进。\n可以回营整备，再次挑战。";
            if(pending)return "节点战斗已完成，结算尚未保存。\n故事、解锁与奖励尚未提交；重试保存后再继续。";
            var definition=ChapterDefinition.Get(node);
            return definition.Outcome+"\n\n下一线索 · "+definition.NextClue+"\n\n"+TierEffect(node)+"\n已解锁节点可回营后独立重玩。";
        }
    }
}
