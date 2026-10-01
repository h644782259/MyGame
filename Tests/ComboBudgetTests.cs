using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Emberfall;

public static class ComboBudgetTests
{
    private static int checks;
    private static void Check(bool value,string message){checks++;if(!value)throw new Exception(message);}
    private static bool Near(float a,float b,float tolerance=.003f){return Math.Abs(a-b)<=tolerance;}
    public static string Run(string artifactDirectory=null)
    {
        checks=0;
        var rows=ComboBudgetSimulation.StandardRows();Check(rows.Count==48,"four classes, three levels, two recipes and two horizons");
        foreach(var r in rows)
        {
            var recipe=ComboBudgetSimulation.Recipes(r.Hero).Single(p=>p.Name==r.Recipe);
            Check(r.Signature==ComboBudgetSimulation.Run(r.Hero,r.Level,recipe,r.Seconds).Signature,"identical replay produces identical ordered event ledger");
            Check(r.Ranks.Sum()<=r.Level-1,"legal shared learned-point budget");
            for(int skill=0;skill<r.Ranks.Length;skill++)
            {
                if(r.Ranks[skill]>0)Check(r.Level>=GameBalance.SkillRankRequiredLevel(skill,r.Ranks[skill])&&GameBalance.SkillPrerequisites[skill].All(p=>r.Ranks[p]>0),"rank level and prerequisite legality");
            }
            foreach(var cast in r.Casts)
            {
                Check(r.Ranks[cast.Skill]>0&&!GameBalance.IsPassive(cast.Skill),"locked/passive skills never committed");
                Check(Near(cast.Cost,GameBalance.SkillEnergyCost(r.Hero,cast.Skill)),"each committed cast pays production cost once");
                Check(cast.Committed-cast.Started+.011f>=SkillDamageBudgets.ChargeSeconds(r.Hero,cast.Skill),"charge commitment precedes cost and damage");
                var prior=r.Casts.LastOrDefault(c=>c.Skill==cast.Skill&&c.Id<cast.Id);
                if(prior!=null)Check(cast.Committed-prior.Committed+.02f>=GameBalance.EffectiveCooldown(r.Hero,cast.Skill,cast.Rank),"same skill observes actual cooldown");
            }
            Check(r.EnergySamples.All(e=>e>=0&&e<=SkillRuntime.MaximumEnergy)&&r.MinimumEnergy>=0&&r.MaximumEnergy<=100,"energy never negative or over cap");
            Check(Near(r.EnergySpent,r.Casts.Sum(c=>c.Cost))&&Near(r.EnergyRemaining,100-r.EnergySpent+r.EnergyRestored,.02f),"resource ledger reconciles with actual SkillRuntime energy");
            Check(r.Hits.All(h=>h.Time>=0&&h.Time<=r.Seconds+.0001f&&h.Coefficient>0&&!float.IsNaN(h.Coefficient)),"only delivered finite positive damage enters window");
            Check(Near(r.Category("basic"),r.BasicHits*SkillDamageBudgets.BasicCoefficient(r.Hero),.005f),"basic damage equals confirmed hits times production coefficient");
            Check(Near(r.Damage,r.Category("basic")+r.Category("skill")+r.Category("reaction")+r.Category("poison")+r.Hits.Where(h=>h.Category.StartsWith("pet_")).Sum(h=>h.Coefficient),.01f),"damage categories reconcile to total");
            if(r.Hero==HeroClass.Arcanist)Check(r.Hits.Count(h=>h.Category=="reaction")<=r.Casts.Count(c=>c.Skill==1),"one shatter at most per meteor cast including its echo");
            if(r.Hero==HeroClass.Ranger)Check(r.Hits.Count(h=>h.Category=="reaction")<=r.Casts.Count(c=>c.Skill==0),"one poison detonation at most per shared fan cast");
            if(r.Hero!=HeroClass.Summoner)Check(r.MaximumPets==0,"non-summoners cannot acquire phantom pets");
            else Check(r.MaximumPets<=3&&r.PetCommands==r.Casts.Count(c=>c.Skill==2||c.Skill==4||c.Skill==9),"bonded partner count and command events bounded by real commits");
        }
        foreach(HeroClass hero in new[]{HeroClass.Vanguard,HeroClass.Arcanist,HeroClass.Ranger})
        for(int rank=1;rank<=3;rank++)
        {
            var field=ComboBudgetSimulation.SkillTimeline(hero,2,rank);var budget=SkillDamageBudgets.EarlyField(hero,rank);
            Check(field.Count==budget.Ticks+(budget.FinisherCoefficient>0?1:0)&&Near(field.Sum(h=>h.Coefficient),budget.TotalCoefficient),"full early field exactly reconciles to production tick and finisher budget");
            var ultimate=ComboBudgetSimulation.SkillTimeline(hero,9,rank);int steps=SkillDamageBudgets.AdvancedSteps(hero,9,rank);
            float scale=CombatBalance.RankPower(rank)*SkillDamageBudgets.AdvancedScale(hero,9);
            float total=Enumerable.Range(0,steps).Sum(step=>SkillDamageBudgets.AdvancedImpact(hero,9,rank,step));
            total+=SkillDamageBudgets.AdvancedTail(hero,9,rank).TotalCoefficient;
            total+=SkillDamageBudgets.RadialArrowCount(hero,9,rank)*SkillDamageBudgets.RadialArrowCoefficient;
            Check(Near(ultimate.Sum(h=>h.Coefficient),total*scale),"full ultimate schedule reconciles main, tail and radial production budget");
        }
        for(int rank=1;rank<=3;rank++)
        {
            var thorn=ComboBudgetSimulation.SkillTimeline(HeroClass.Summoner,1,rank);
            Check(thorn.Count==SummonerDamageRules.ThornTicks(rank)&&Near(thorn.Sum(h=>h.Coefficient),CombatBalance.RankPower(rank)*SummonerDamageRules.ThornTickCoefficient*SummonerDamageRules.ThornTicks(rank)),"summoner thorn uses actual finite tick schedule");
            var mark=ComboBudgetSimulation.SkillTimeline(HeroClass.Summoner,7,rank);
            Check(mark.Count==SummonerDamageRules.MarkTicks+1&&Near(mark.Sum(h=>h.Coefficient),CombatBalance.RankPower(rank)*(SummonerDamageRules.MarkTicks*SummonerDamageRules.MarkTickCoefficient+SummonerDamageRules.MarkFinisherCoefficient)),"summoner mark uses finite ticks plus separate finisher");
            var fan=ComboBudgetSimulation.SkillTimeline(HeroClass.Ranger,0,rank);
            Check(fan.Sum(h=>h.Coefficient)<=SkillDamageBudgets.FanTargetCap(rank)+.0001f,"all fan arrows and explosions share target cap");
        }
        foreach(int skill in new[]{1,7,9})
        {
            float lifetime=ComboBudgetSimulation.SummonerFullLifetime(skill);
            var single=ComboBudgetSimulation.StandaloneSummoner(skill,lifetime);
            Check(single.Casts.Count==1&&single.BasicHits==0&&single.EnergySpent==GameBalance.SkillEnergyCost(HeroClass.Summoner,skill),"standalone coefficient isolates one paid cast without basics");
            if(skill==1||skill==7)
            {
                Check(Near(single.Category("skill"),ComboBudgetSimulation.SkillTimeline(HeroClass.Summoner,skill,3).Sum(h=>h.Coefficient)),"standalone full lifetime drains every production spell event");
                Check(single.MaximumPets==1&&single.PetCommands==0&&single.Category("pet_2")==0,"foundation wolf damage remains separate from standalone spell output");
            }
            if(skill==1)Check(single.Category("poison")>0&&single.Hits.Where(h=>h.Category=="poison").Max(h=>h.Time)>SummonerDamageRules.ThornDuration(3),"thorn analysis includes refreshed poison after final field tick");
            if(skill==7)Check(single.Category("poison")==0&&single.Hits.Count(h=>h.Category=="skill")==SummonerDamageRules.MarkTicks+1,"gravity does not inherit unrelated thorn poison");
            if(skill==9)
            {
                float total=0;for(float at=0;at<CompanionRules.ContractLifetime(2,3,false);at+=CompanionRules.AttackInterval(2))
                    total+=CompanionRules.RankPower(3)*CompanionRules.AttackCoefficient(2)*(at<CompanionRules.CommandDuration(false)?CompanionRules.CommandMultiplier(3):1);
                Check(Near(single.Category("pet_2"),total)&&single.PetCommands==1,"treant full lifetime reconciles real form cadence and finite command buff");
            }
        }
        var chargeRecipe=new ComboBudgetSimulation.Recipe("charge-only",false,1);
        var before=ComboBudgetSimulation.Run(HeroClass.Arcanist,50,chargeRecipe,.4f,false);
        Check(before.ChargesStarted==1&&before.PendingCharge==1&&before.Casts.Count==0&&before.EnergySpent==0&&before.Damage==0,"precommit charged cast has no cost or damage");
        var committed=ComboBudgetSimulation.Run(HeroClass.Arcanist,50,chargeRecipe,.6f,false);
        Check(committed.Casts.Count==1&&committed.EnergySpent==GameBalance.SkillEnergyCost(HeroClass.Arcanist,1)&&committed.Damage==0,"commit pays once but delayed meteor has not yet landed");
        var cancelled=ComboBudgetSimulation.Run(HeroClass.Arcanist,50,chargeRecipe,.4f,false,.2f);
        Check(cancelled.ChargesCancelled==1&&cancelled.Casts.Count==0&&cancelled.EnergySpent==0,"cancelled charge never pays twice or schedules hits");
        var locked=ComboBudgetSimulation.Run(HeroClass.Vanguard,20,new ComboBudgetSimulation.Recipe("locked",false,9),10,false);
        Check(locked.Casts.Count==0&&locked.ChargesStarted==0&&locked.Damage==0,"level20 cannot cast level30 ultimate");
        var delayed=ComboBudgetSimulation.Run(HeroClass.Ranger,50,new ComboBudgetSimulation.Recipe("field",false,2),.2f,false);
        Check(delayed.Casts.Count==1&&delayed.Damage==0&&delayed.PendingSkillCoefficient>0,"field startup not delivered prematurely");
        foreach(var r in rows.Where(r=>r.Hero==HeroClass.Arcanist&&r.Recipe=="frost_meteor"||r.Hero==HeroClass.Ranger&&r.Recipe=="poison_fan"))
            Check(r.Category("reaction")>0,"actual frost-to-meteor and three-poison-to-fan combo appears");
        var pets=ComboBudgetSimulation.Run(HeroClass.Summoner,100,new ComboBudgetSimulation.Recipe("treant-lifetime",false,9),35,false);
        Check(pets.Casts.Count==1&&pets.MaximumPets==2,"one actual treant plus starter, no invented recasts or extra pets");
        float end=pets.Casts[0].Committed+CompanionRules.ContractLifetime(2,pets.Ranks[9],false);
        Check(pets.Hits.Where(h=>h.Category=="pet_2").All(h=>h.Time<end)&&pets.Hits.Any(h=>h.Category=="pet_0"&&h.Time>end),"timed treant expires while legitimate foundation partner remains");
        var starvation=ComboBudgetSimulation.Run(HeroClass.Arcanist,100,new ComboBudgetSimulation.Recipe("starvation",false,9,2,1,0),20,false);
        Check(starvation.MinimumEnergy>=0&&starvation.EnergySpent<=100+20*SkillRuntime.EnergyPerSecond+.01f,"skill-only rotation cannot spend nonexistent energy");
        if(artifactDirectory!=null){Directory.CreateDirectory(artifactDirectory);File.WriteAllText(Path.Combine(artifactDirectory,"Combo-Budget.csv"),ComboBudgetSimulation.Csv(rows));}
        return checks+" deterministic combo/resource/pet budget checks passed across "+rows.Count+" scenarios";
    }
}
