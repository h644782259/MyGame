using System;
using System.Collections.Generic;
namespace Emberfall
{
    public enum CampPracticeScenario { Stationary, Moving, FrontAndSupplier }
    // Captures observed events only; no stat-derived DPS or synthetic aggregate score.
    public sealed class CampPracticeRecord
    {
        public readonly CampPracticeScenario Scenario;
        public readonly int Duration, Seed;
        public readonly string Configuration;
        public float Elapsed { get; private set; }
        public float ActualDamage { get; private set; }
        public float EnergySpent { get; private set; }
        public float EnergyRestored { get; private set; }
        public bool Finished { get; private set; }
        public string EndReason { get; private set; }
        public readonly Dictionary<string,int> Mechanisms=new Dictionary<string,int>();
        public readonly Dictionary<int,int> SkillCasts=new Dictionary<int,int>(), EffectiveSkillCasts=new Dictionary<int,int>();
        private readonly Dictionary<int,int> casts=new Dictionary<int,int>();
        private readonly HashSet<int> hitCasts=new HashSet<int>();
        public CampPracticeRecord(CampPracticeScenario scenario,int duration,string configuration,int seed=7319)
        {if(duration!=10&&duration!=60)throw new ArgumentOutOfRangeException("duration");Scenario=scenario;Duration=duration;Configuration=configuration;Seed=seed;}
        private static bool Valid(float n){return n>0&&!float.IsNaN(n)&&!float.IsInfinity(n);}
        public void Advance(float dt){if(!Finished&&Valid(dt)){Elapsed=Math.Min(Duration,Elapsed+dt);if(Elapsed>=Duration)Finish("计时完成");}}
        public void Damage(float amount){if(!Finished&&Valid(amount))ActualDamage+=amount;}
        public void Energy(float delta){if(Finished||float.IsNaN(delta)||float.IsInfinity(delta))return;if(delta<0)EnergySpent-=delta;else EnergyRestored+=delta;}
        private static void Count<T>(Dictionary<T,int> counts,T key){int n;counts.TryGetValue(key,out n);counts[key]=n+1;}
        public void Cast(int id,int skill){if(Finished||id<=0||skill<0||casts.ContainsKey(id))return;casts[id]=skill;Count(SkillCasts,skill);}
        public void Hit(int id){int skill;if(!Finished&&casts.TryGetValue(id,out skill)&&hitCasts.Add(id))Count(EffectiveSkillCasts,skill);}
        public void Mechanism(string key){if(!Finished&&!string.IsNullOrEmpty(key))Count(Mechanisms,key);}
        public void Finish(string reason){if(Finished)return;Finished=true;EndReason=reason;}
        public bool ComparableConditions(CampPracticeRecord other){return other!=null&&Finished&&other.Finished&&Elapsed==Duration&&other.Elapsed==other.Duration&&Scenario==other.Scenario&&Duration==other.Duration&&Seed==other.Seed;}
        public string Comparison(CampPracticeRecord other){return !ComparableConditions(other)?"条件或时长不同，不能直接比较":Configuration==other.Configuration?"同配置、同场景实测；操作差异仍影响结果":"配置不同的 A/B 实测；同场景时长，详见各自固定快照，操作差异仍影响结果";}
    }
}
