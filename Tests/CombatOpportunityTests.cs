using System;
using Emberfall;
public static class CombatOpportunityTests
{
    static int n;static void Check(bool value,string reason){n++;if(!value)throw new Exception(reason);}
    public static string Run()
    {
        n=0;Check(CombatOpportunityPresentation.Vanguard(0)=="","Expired counter has no stale notice");
        Check(CombatOpportunityPresentation.Vanguard(.5f).Contains("0.5"),"Actual remaining counter");
        Check(CombatOpportunityPresentation.Arcanist(false,false,false)=="","No target effects means no opportunity");
        Check(CombatOpportunityPresentation.Arcanist(true,true,true)=="霜痕 · 可碎冰","Only one prioritized message");
        Check(CombatOpportunityPresentation.Arcanist(true,false,false)=="目标霜痕","Locked or cooldown shatter does not claim cast ready");
        Check(CombatOpportunityPresentation.Arcanist(false,true,false)=="目标灼烧","Burn is real current target state");
        Check(CombatOpportunityPresentation.Ranger(0,false)=="","No poison no notice");
        Check(CombatOpportunityPresentation.Ranger(3,true)=="三层毒 · 易伤","Vulnerable and three stacks on one line");
        Check(CombatOpportunityPresentation.Summoner(0,float.PositiveInfinity,0)=="暂无伙伴","No stale roster");
        Check(CombatOpportunityPresentation.Summoner(2,float.PositiveInfinity,0)=="伙伴 2 · 常驻","Permanent roster no fake lifetime");
        Check(CombatOpportunityPresentation.Summoner(3,4.2f,0).Contains("5秒"),"Shortest live contract rounded up");
        Check(CombatOpportunityPresentation.Summoner(3,4.2f,6).Contains("契机"),"Command opportunity takes single line priority");
        Check(CombatOpportunityPresentation.Summoner(3,4.2f,6).Contains("5秒"),"Contract lifetime stays visible beside command opportunity");
        return n+" combat opportunity presentation assertions passed";
    }
}
