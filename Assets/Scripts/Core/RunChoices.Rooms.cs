using System;
using System.Collections.Generic;
namespace Emberfall
{
    public sealed partial class RunChoices
    {
        // Desktop pages can reach either equipped page; mobile has fixed learned active buttons.
        public static int[] UsableRanks(GameProfile profile, bool mobile)
        {
            var result = new int[GameBalance.SkillCount];
            if (profile == null || profile.skillRanks == null) return result;
            for (int skill=0; skill<result.Length && skill<profile.skillRanks.Length; skill++)
                if (!GameBalance.IsPassive(skill) && profile.skillRanks[skill]>0 &&
                    (mobile || profile.equippedSkills!=null && Array.IndexOf(profile.equippedSkills,skill)>=0))
                    result[skill]=profile.skillRanks[skill];
            return result;
        }
        public void PrepareRoomChoice(int stage, GameProfile profile, bool mobile, int seed)
        {
            if (profile==null || stage<1 || stage>2 || AwaitingChoice || stage!=CompletedWave+1 || active.Count!=stage-1) return;
            int[] usable=UsableRanks(profile,mobile);
            var style=new List<RunBlessing>();
            foreach(var item in new[]{RunBlessing.DodgeShock,RunBlessing.ChargedWard,RunBlessing.InterruptFlow,RunBlessing.MarkedPursuit})
                if(!active.Contains(item)&&IsCompatible(item,profile.heroClass,usable))style.Add(item);
            if(stage==2)
            {
                if(Has(RunBlessing.KeenSight))style.Insert(0,RunBlessing.DeadlyEdge);
                else if(Has(RunBlessing.DeadlyEdge))style.Insert(0,RunBlessing.KeenSight);
                else if(Has(RunBlessing.SwiftHands)||Has(RunBlessing.DodgeShock))style.Insert(0,RunBlessing.BattleFervor);
                else if(Has(RunBlessing.ChargedWard)||Has(RunBlessing.InterruptFlow)||Has(RunBlessing.QuickRecovery))style.Insert(0,RunBlessing.FlowingEssence);
            }
            if(style.Count==0)style.Add(RunBlessing.SwiftHands);
            var random=new Random(unchecked(seed+stage*577+(int)profile.heroClass*103+(int)profile.specialization*71+(int)profile.summonerRoute*43));
            var result=new List<RunBlessing>();
            // The second checkpoint first reinforces the selected route; the other cards patch gaps.
            AddRoomCard(result,style,random,stage==2);
            AddRoomCard(result,new List<RunBlessing>{RunBlessing.IronSkin,RunBlessing.LastStand,RunBlessing.ExecutionMend},random,false);
            AddRoomCard(result,new List<RunBlessing>{RunBlessing.FlowingEssence,RunBlessing.QuickRecovery,RunBlessing.SwiftHands},random,false);
            CompletedWave=stage;offer=result.ToArray();AwaitingChoice=true;
        }
        private void AddRoomCard(List<RunBlessing> result,List<RunBlessing> pool,Random random,bool preferFirst)
        {
            pool.RemoveAll(item=>active.Contains(item)||result.Contains(item));
            if(pool.Count==0)return;
            result.Add(pool[preferFirst?0:random.Next(pool.Count)]);
        }
        public static string Association(RunBlessing item,GameProfile profile,bool mobile)
        {
            if(profile==null)return "当前配置可用";
            int[] ranks=UsableRanks(profile,mobile);
            if(!IsCompatible(item,profile.heroClass,ranks))return "需可用的对应技能";
            if(item==RunBlessing.DodgeShock)return "闪避 → 普攻反击";
            if(item==RunBlessing.MarkedPursuit)return GameBalance.SkillName(profile.heroClass,7)+" → 击杀追击";
            if(item==RunBlessing.ChargedWard)
            {
                int preferred=profile.heroClass==HeroClass.Arcanist?1:profile.heroClass==HeroClass.Vanguard?7:profile.heroClass==HeroClass.Summoner?4:9;
                return GameBalance.SkillName(profile.heroClass,ranks[preferred]>0?preferred:9)+" → 蓄力护佑";
            }
            if(item==RunBlessing.InterruptFlow)return "控制打断 → 回能";
            if(item==RunBlessing.FlowingEssence)return "补资源 · 持续回能";
            if(item==RunBlessing.QuickRecovery)return "补节奏 · 新施法冷却更短";
            if(item==RunBlessing.IronSkin||item==RunBlessing.LastStand||item==RunBlessing.ExecutionMend)return "补生存 · 留容错";
            if(item==RunBlessing.SwiftHands)return "普攻 → 更快回能";
            if(item==RunBlessing.KeenSight||item==RunBlessing.DeadlyEdge)return "强化直伤暴击";
            return item==RunBlessing.RiskContract?"承受更多伤害换金币":"强化普攻与技能伤害";
        }
    }
}
