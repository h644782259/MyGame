using System;
namespace Emberfall
{
    public enum CombatOpportunityKind { None, Counter, Shatter, Reignite, BurnFinale, PoisonDetonation, EmpoweredContract, BurnCash, EmpoweredHit }
    // A read-only observation, never a promise that a future projectile will hit.
    public readonly struct CombatOpportunityState
    {
        public readonly CombatOpportunityKind Kind;
        public readonly float Remaining;
        public readonly int Count;
        public bool Actionable {get{return Kind>=CombatOpportunityKind.Counter&&Kind<=CombatOpportunityKind.EmpoweredContract&&Remaining>0;}}
        public CombatOpportunityState(CombatOpportunityKind kind,float remaining,int count=0){Kind=remaining>0?kind:CombatOpportunityKind.None;Remaining=Math.Max(0,remaining);Count=count;}
        public string Caption
        {
            get
            {
                string title=Kind==CombatOpportunityKind.Counter?"反击":Kind==CombatOpportunityKind.Shatter?"碎冰":Kind==CombatOpportunityKind.Reignite?"续燃":Kind==CombatOpportunityKind.BurnFinale?"兑燃":Kind==CombatOpportunityKind.PoisonDetonation?"三毒":Kind==CombatOpportunityKind.EmpoweredContract?"强化":Kind==CombatOpportunityKind.BurnCash?"兑燃 ×"+Count:Kind==CombatOpportunityKind.EmpoweredHit?"强化命中":"";
                return title.Length==0?"":Actionable?title+" "+Remaining.ToString("0.0"):title;
            }
        }
    }
}
