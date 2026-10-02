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
                "解锁 · 普通通关解锁困难，困难通关解锁英雄；阶数和治疗规则独立。\n"+
                (limited?"限疗：初始3次治疗充能。":"普通治疗：使用携带药剂。")+"\n"+
                "完成奖励 "+total+" 碎片（重复 "+repeat+(total>repeat?" + 首次1":"")+"）；难度不加乘，不发旧副本宝箱。\n"+
                (!profile.firstClearRewardClaimed?(profile.pendingFirstClearReward?"共享一次首通核心已待领取；本次不重复。\n":(node==ChapterNode.StarPlatform?"星台通关完成整章，可领取共享一次首通核心。\n":"首通核心需完成整章：通关星台；本节点不授予资格。\n")):"")+TierEffect(node);
        }
        public static string Result(ChapterResultSnapshot result)
        {
            if(result==null)return "章节记录暂不可用";
            string text=ChapterDefinition.Get(result.Node).Name+" · "+DifficultyName(result.Difficulty)+" · 第 "+result.Tier+" 阶\n"+
                "携带药剂 "+result.EntryPotions+" · "+(result.LimitedHealing?"限疗规则":"普通治疗")+"\n\n";
            if(result.Failed)return text+"止步房间 "+(result.Room+1)+" / "+ChapterDefinition.RoomCount(result.Node)+"\n"+
                (result.Node==ChapterNode.ForestCourt?"封印 "+result.Seals+"/2 · 一 "+result.FirstSealSeconds.ToString("0.0")+"s · 二 "+result.SecondSealSeconds.ToString("0.0")+"s\n":"")+
                "最后受击："+(string.IsNullOrEmpty(result.LastHit)?"未记录":result.LastHit)+" · "+result.LastHitAmount.ToString("0.#")+"\n"+
                (string.IsNullOrEmpty(result.Failure)?"本次挑战未完成。":result.Failure)+"\n本次击杀经验 +"+result.KillExperience+"；未发通关经验。\n节点与难度未解锁；回营重试。";
            if(!result.Saved)return text+"节点完成 · 结算尚未保存\n奖励与解锁尚未提交，重试保存后再继续。";
            text+="奖励已保存 · +"+result.Materials+" 碎片\n击杀经验 +"+result.KillExperience+" · 通关经验 +"+result.CompletionExperience;
            if(result.FirstCompletion)text+="\n"+ChapterDefinition.Get(result.Node).Outcome;
            if(result.FirstCoreAvailable)text+="\n首通核心已可领取（共享一次）";
            if(result.UnlockedNode>=0)text+="\n新节点："+ChapterDefinition.Get((ChapterNode)result.UnlockedNode).Name;
            if(result.UnlockedDifficulty>=0)text+="\n本节点新难度："+DifficultyName((ChapterDifficulty)result.UnlockedDifficulty);
            text+=result.SharedAfter>result.SharedBefore?"\n共享最高阶 "+result.SharedBefore+" → "+result.SharedAfter:"\n共享最高阶未变化（"+result.SharedAfter+"）";
            if(result.FirstCompletion)text+="\n下一线索 · "+ChapterDefinition.Get(result.Node).NextClue;
            return text;
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
