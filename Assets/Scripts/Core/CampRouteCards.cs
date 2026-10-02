using System;
using System.Collections.Generic;
namespace Emberfall
{
    // Suggestions describe existing skills/equipment; they never unlock a second progression system.
    public sealed class CampRouteCard
    {
        public readonly string Name, Loop, Requirements;
        public readonly bool Ready;
        public CampRouteCard(string name,string loop,string requirements,bool ready)
        {Name=name;Loop=loop;Requirements=requirements;Ready=ready;}
    }
    public static class CampRouteCards
    {
        public static CampRouteCard Describe(GameProfile profile,bool mobile,int index)
        {
            if(profile==null||index<0||index>1)return new CampRouteCard("","","",false);
            string name,loop;int[] skills;EquipmentMechanic mechanic=EquipmentMechanic.None;
            switch(profile.heroClass)
            {
                case HeroClass.Vanguard:
                    name=index==0?"回刃反击":"破阵控场";
                    loop=index==0?"闪避 → 普攻反击 → 回刃连击":"裂地控敌 → 风暴清场 → 普攻回能";
                    skills=index==0?new int[0]:new[]{1,2};if(index==0)mechanic=EquipmentMechanic.ReturningBlade;break;
                case HeroClass.Arcanist:
                    name=index==0?"碎冰连锁":"灼燃留场";
                    loop=index==0?"新星冻敌 → 陨星碎冰":"陨星留火 → 护盾贴身留火";
                    skills=index==0?new[]{0,1}:new[]{1,5};if(index==1)mechanic=EquipmentMechanic.CinderTrail;break;
                case HeroClass.Ranger:
                    name=index==0?"叠毒引爆":"猎印追击";
                    loop=index==0?"普攻叠毒 → 扇形箭引爆":"狩猎标记 → 集火击杀 → 拉开距离";
                    skills=index==0?new[]{0}:new[]{7,4};if(index==0)mechanic=EquipmentMechanic.VenomSpread;break;
                default:
                    name=index==0?"双契协同":"群契围攻";
                    loop=index==0?"狼 / 星灵常驻 → 普攻集火 → 双契共鸣":"普攻集火 → 契约增援 → 轮换兽群";
                    skills=new[]{2,4};if(index==0)mechanic=EquipmentMechanic.TwinSummonResonance;break;
            }
            var missing=new List<string>();int[] usable=RunChoices.UsableRanks(profile,mobile);
            foreach(int skill in skills)if(usable[skill]<=0)
                missing.Add((profile.skillRanks!=null&&profile.skillRanks.Length>skill&&profile.skillRanks[skill]>0?"装入":"学习")+GameBalance.SkillName(profile.heroClass,skill));
            if(profile.heroClass==HeroClass.Arcanist&&profile.specialization!=(index==0?ElementalistSpecialization.Shatter:ElementalistSpecialization.Burn))missing.Add("切换"+(index==0?"碎冰":"灼燃"));
            if(profile.heroClass==HeroClass.Summoner&&(int)profile.summonerRoute!=index)missing.Add("切换"+(index==0?"双契":"群契"));
            if(profile.heroClass==HeroClass.Summoner&&index==1&&Equipped(profile,EquipmentMechanic.TwinSummonResonance))missing.Add("卸下双契共鸣以展开兽群");
            if(mechanic!=EquipmentMechanic.None&&!Equipped(profile,mechanic))missing.Add("穿戴"+BuildCatalog.MechanicName(mechanic));
            return new CampRouteCard(name,loop,missing.Count==0?"核心条件齐备":"缺："+string.Join(" / ",missing),missing.Count==0);
        }
        private static bool Equipped(GameProfile profile,EquipmentMechanic mechanic)
        {
            if(profile.inventory==null)return false;
            string id=BuildCatalog.MechanicSlot(mechanic)==ItemSlot.Weapon?profile.weaponId:profile.relicId;
            foreach(var item in profile.inventory)if(item!=null&&item.id==id&&item.mechanic==mechanic)return true;
            return false;
        }
    }
}
