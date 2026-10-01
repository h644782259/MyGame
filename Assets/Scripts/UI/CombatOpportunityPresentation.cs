using System;
namespace Emberfall
{
    public static class CombatOpportunityPresentation
    {
        public static string Vanguard(float counter){return counter>0?"反击窗口 "+counter.ToString("0.0")+"秒":"";}
        public static string Arcanist(bool frost,bool burn,bool canShatter)
        {return frost?(canShatter?"霜痕 · 可碎冰":"目标霜痕"):burn?"目标灼烧":"";}
        public static string Ranger(int poison,bool vulnerable)
        {return poison>=3?(vulnerable?"三层毒 · 易伤":"目标三层毒"):vulnerable?"目标易伤":poison>0?"目标中毒 ×"+poison:"";}
        public static string Summoner(int count,float lifetime,float opportunity)
        {
            if(opportunity>0)return "契机"+Math.Ceiling(opportunity)+"秒·"+count+"伴·"+(float.IsPositiveInfinity(lifetime)?"常驻":Math.Ceiling(Math.Max(0,lifetime))+"秒");
            if(count<=0)return "暂无伙伴";
            return "伙伴 "+count+ (float.IsPositiveInfinity(lifetime)?" · 常驻":" · 最短 "+Math.Ceiling(Math.Max(0,lifetime))+"秒");
        }
    }
}
