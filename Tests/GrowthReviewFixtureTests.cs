using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Emberfall;
using Emberfall.Editor;

public static class GrowthReviewFixtureTests
{
    private static int checks;
    private static void Check(bool value,string reason){checks++;if(!value)throw new Exception(reason);}
    private static string Times(List<float> times){return string.Join(";",times.Select(t=>t.ToString("0.00",CultureInfo.InvariantCulture)));}
    public static string Run(string output)
    {
        checks=0;Directory.CreateDirectory(output);
        var rows=new List<string>{"fixture,hero,level,skill_points_spent,mastery_points_spent,points_remaining,specialization,route,mechanism,core,window_seconds,mastery_probe_times,equipment_probe_times,equipment_probe_semantics"};
        var configs=CombatReviewConfigurations.CreateGrowthBuilds();Check(configs.Length==6,"six complete skill/mastery/mechanism fixture definitions");
        Check(CombatReviewConfigurations.CreateAll().Length==11,"introductory baselines remain available");
        foreach(var c in configs)
        {
            string dir=Path.Combine(output,"isolated",c.id);var p=new ProgressionService(dir);
            Check(p.CreateNewSlot(c.hero),"new isolated role");
            CombatReviewBuildSetup.Apply(p,c,dir);
            Check(p.Profile.level==50&&p.Profile.skillRanks.SequenceEqual(c.skillRanks),"production save accepts exact requested level/ranks without repairing them");
            Check(p.Profile.masteryRanks.SequenceEqual(c.masteryRanks)&&p.Profile.masteryCore==c.masteryCore,"production save accepts exact mastery and exclusive core");
            Check(p.Profile.skillRanks.Sum()==30&&p.Profile.masteryRanks.Sum()==10&&p.Profile.skillPoints==9,"40 of 49 earned points invested legally");
            Check(p.Profile.summonerRoute==c.summonerRoute&&p.Profile.specialization==c.specialization,"route and specialization apply");
            for(int i=0;i<c.skillRanks.Length;i++)Check(GameBalance.SkillRankRequiredLevel(i,c.skillRanks[i])<=50,"rank level gate");
            string gearId=null;
            if(c.mechanism!=EquipmentMechanic.None)
            {
                var item=p.Equipped(BuildCatalog.MechanicSlot(c.mechanism));gearId=item.id;
                Check(item.mechanic==c.mechanism&&item.level==50&&item.rarity==Rarity.Epic&&item.upgradeLevel==0,"production mechanism equipped at declared level/rarity/rank");
                Check(!item.mechanicVariantUnlocked&&item.mechanicVariant==0,"no hidden unlocked variant");
            }
            Check(p.LoadSlot(p.CurrentSlotId)&&p.Profile.skillRanks.SequenceEqual(c.skillRanks)&&p.Profile.masteryRanks.SequenceEqual(c.masteryRanks),"build survives restart without normalization loss");
            if(gearId!=null)Check(p.Equipped(BuildCatalog.MechanicSlot(c.mechanism)).id==gearId,"mechanism stable identity survives restart");
            File.WriteAllText(Path.Combine(output,c.id+".profile.json"),UnityEngine.JsonUtility.ToJson(p.Profile,true));
            var core=new MasteryCoreRuntime();core.Configure(c.masteryCore,10);var proc=new CombatProcCooldown();
            var twin=new CompanionCooperationTracker<object>();var target=new object();var skill=new SkillRuntime(c.hero);
            var coreTimes=new List<float>();var gearTimes=new List<float>();string semantics="none";
            for(int step=0;step<=144;step++)
            {
                float t=step*.25f;if(step>0){core.Advance(.25f);proc.Advance(.25f);skill.Advance(.25f);}
                // Eligible-event probes isolate cooldown/rearm rules. They are not
                // player input, available energy, hits or a combat damage trace.
                bool fired=false;
                if(c.masteryCore==(int)MasteryType.Offense){core.SkillHit(step+1);fired=core.BasicHit()>0;}
                if(c.masteryCore==(int)MasteryType.Vitality)fired=core.DamageTaken(.4f)>0;
                if(c.masteryCore==(int)MasteryType.Guard)fired=core.PerfectDodge()>0;
                if(c.masteryCore==(int)MasteryType.Technique)fired=core.SkillSpent(60).Energy>0;
                if(fired)coreTimes.Add(t);
                if(c.mechanism==EquipmentMechanic.ReturningBlade||c.mechanism==EquipmentMechanic.VenomSpread)
                {
                    float cooldown=c.mechanism==EquipmentMechanic.ReturningBlade?1.5f:2f;
                    semantics="eligible_proc_gate_"+cooldown.ToString("0.0",CultureInfo.InvariantCulture)+"s";
                    if(proc.TryTrigger(cooldown))gearTimes.Add(t);
                }
                else if(c.mechanism==EquipmentMechanic.TwinSummonResonance)
                {
                    semantics="same_target_two_forms_1.5s_window_3s_gate";
                    bool first=twin.RegisterHit(target,0,t),second=twin.RegisterHit(target,1,t);
                    if(first||second)gearTimes.Add(t);
                }
                else if(c.mechanism==EquipmentMechanic.FrostEcho||c.mechanism==EquipmentMechanic.CinderTrail)
                {
                    int index=c.mechanism==EquipmentMechanic.FrostEcho?0:1;
                    semantics="per_successful_cast_no_extra_gate_skill_"+index;
                    skill.RestoreEnergy(100); // isolates readiness; not an energy-sustainable rotation
                    if(skill.TryConsume(index,3))gearTimes.Add(t);
                }
            }
            Check(coreTimes.Count>=3&&coreTimes[2]<=36,"start plus at least two real mastery cooldown re-arms");
            float coreCooldown=c.masteryCore==(int)MasteryType.Offense?6:c.masteryCore==(int)MasteryType.Vitality?12:8;
            Check(coreTimes[0]==0&&coreTimes[1]==coreCooldown&&coreTimes[2]==coreCooldown*2,"configured tier-one mastery cooldown is exact at two re-arms");
            for(int i=1;i<coreTimes.Count;i++)Check(coreTimes[i]-coreTimes[i-1]>=coreCooldown,"no repeated eligible probe bypasses mastery cooldown");
            if(c.mechanism!=EquipmentMechanic.None)Check(gearTimes.Count>=3&&gearTimes[2]<=36,"start plus at least two equipment gate or skill cooldown re-arms");
            bool twinBuild=c.mechanism==EquipmentMechanic.TwinSummonResonance;
            if(c.hero==HeroClass.Summoner)
            {
                Check(CompanionRules.NormalCapacity(twinBuild)==(twinBuild?2:4),"declared summon capacity");
                Check(CompanionRules.PermanentPartner(false,1,c.summonerRoute==SummonerRoute.Pack)==!c.id.EndsWith("pack"),"bonded spirit permanence differs from timed pack");
            }
            rows.Add(string.Join(",",c.id,c.hero,50,30,10,9,c.specialization,c.summonerRoute,c.mechanism,(MasteryType)c.masteryCore,36,Times(coreTimes),Times(gearTimes),semantics));
            bool rejected=false;try{CombatReviewBuildSetup.Apply(p,c,dir+"-wrong");}catch(InvalidOperationException){rejected=true;}
            Check(rejected,"fixture refuses mismatched isolation directory");
        }
        configs[0].skillRanks[0]=0;configs[0].masteryRanks[0]=0;
        Check(CombatReviewConfigurations.CreateGrowthBuilds()[0].skillRanks[0]==3&&CombatReviewConfigurations.CreateGrowthBuilds()[0].masteryRanks[0]==10,"new fixture arrays independent");
        File.WriteAllLines(Path.Combine(output,"Growth-Review-Rule-Probes.csv"),rows);
        return "PASS: "+checks+" legal-build/save-roundtrip/core-rule probes (managed fixtures, not combat traces)";
    }
}
