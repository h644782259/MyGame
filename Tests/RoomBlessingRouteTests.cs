using System;
using System.Collections.Generic;
using Emberfall;
public static class RoomBlessingRouteTests
{
    public static string Run()
    {
        int checks=0;Action<bool,string> check=(ok,message)=>{checks++;if(!ok)throw new Exception(message);};
        for(int seed=0;seed<150;seed++)for(int hero=0;hero<4;hero++)foreach(bool mobile in new[]{false,true})
        {
            var p=new GameProfile{heroClass=(HeroClass)hero,specialization=(ElementalistSpecialization)(seed%3),summonerRoute=(SummonerRoute)(seed%2),equippedSkills=new[]{0,2,5}};
            for(int i=0;i<10;i++)p.skillRanks[i]=(seed+i)%4;
            int[] usable=RunChoices.UsableRanks(p,mobile);
            for(int i=0;i<10;i++)check((usable[i]>0)==(p.skillRanks[i]>0&&!GameBalance.IsPassive(i)&&(mobile||Array.IndexOf(p.equippedSkills,i)>=0)),"only actually reachable skills recommend mechanics");
            var a=new RunChoices();var b=new RunChoices();
            a.PrepareRoomChoice(2,p,mobile,seed);check(!a.AwaitingChoice,"rest cannot skip first checkpoint");
            for(int stage=1;stage<=2;stage++)
            {
                a.PrepareRoomChoice(stage,p,mobile,seed);b.PrepareRoomChoice(stage,p,mobile,seed);
                var cards=a.Offer;check(cards.Length==3&&new HashSet<RunBlessing>(cards).Count==3,"three distinct offers");
                check(string.Join(",",cards)==string.Join(",",b.Offer),"seed/config/path deterministic");
                bool survival=false,resource=false;
                foreach(var card in cards)
                {
                    check(!a.Has(card)&&RunChoices.IsCompatible(card,p.heroClass,usable),"eligible and unowned");
                    check(!string.IsNullOrEmpty(RunChoices.Association(card,p,mobile)),"short actionable association");
                    survival|=card==RunBlessing.IronSkin||card==RunBlessing.LastStand||card==RunBlessing.ExecutionMend;
                    resource|=card==RunBlessing.FlowingEssence||card==RunBlessing.QuickRecovery||card==RunBlessing.SwiftHands;
                }
                check(survival&&resource,"every checkpoint includes survival and resource patch");
                a.PrepareRoomChoice(stage,p,!mobile,seed+77);check(string.Join(",",cards)==string.Join(",",a.Offer),"pending choice cannot reroll");
                check(!a.Choose(-1)&&a.AwaitingChoice,"invalid confirm preserves modal");
                check(a.Choose(seed%3)&&b.Choose(seed%3)&&!a.Choose(0),"choice exactly once");
                a.PrepareRoomChoice(stage,p,mobile,seed);check(!a.AwaitingChoice,"completed checkpoint cannot repeat");
            }
            int count=0;foreach(var item in a.Active)count++;check(count==2,"exactly two run blessings");
            a.PrepareRoomChoice(3,p,mobile,seed);check(!a.AwaitingChoice,"no third choice");
            a.Reset();a.PrepareRoomChoice(1,p,mobile,seed);check(a.AwaitingChoice,"fresh run grants first checkpoint");
            for(int route=0;route<2;route++)
            {var info=CampRouteCards.Describe(p,mobile,route);check(info.Name.Length>0&&info.Loop.Length>0&&info.Requirements.Length>0,"all four classes have two concrete route cards");}
        }
        var ranger=new GameProfile{heroClass=HeroClass.Ranger,equippedSkills=new[]{0}};ranger.skillRanks[7]=1;ranger.skillRanks[4]=1;
        check(CampRouteCards.Describe(ranger,false,1).Requirements.Contains("装入"),"desktop learned but unequipped requires equip");
        check(CampRouteCards.Describe(ranger,true,1).Ready,"mobile learned fixed buttons need no desktop equip");
        var vanguard=new GameProfile{heroClass=HeroClass.Vanguard};
        check(CampRouteCards.Describe(vanguard,true,0).Requirements.Contains("回刃长剑"),"missing mechanism shown");
        vanguard.inventory.Add(new ItemData{id="sword",slot=ItemSlot.Weapon,mechanic=EquipmentMechanic.ReturningBlade});vanguard.weaponId="sword";
        check(CampRouteCards.Describe(vanguard,true,0).Ready,"equipped route mechanism recognized");
        return "PASS: "+checks+" room blessing/route assertions (no Unity execution)";
    }
}
