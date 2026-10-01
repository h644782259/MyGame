using UnityEngine;
namespace Emberfall
{
    public sealed partial class GameUI
    {
        private string MobileSkillState(int skill)
        {
            var p=session.Progression.Profile;var hero=session.Player;
            var charge=hero==null?null:hero.GetComponent<SkillChargeController>();
            return MobileCombatPresentation.Skill(p.skillRanks[skill]>0,GameBalance.IsPassive(skill),
                hero==null?0:hero.SkillCooldownRemaining(skill),hero==null?0:hero.Energy,
                GameBalance.SkillEnergyCost(p.heroClass,skill),skill==6&&session.ChallengeRun&&session.InDungeon,session.HealingCharges,charge!=null&&charge.IsCharging&&charge.SkillIndex==skill);
        }
        private void DrawMobileSkillAvailability(Rect r,int skill)
        {
            string state=MobileSkillState(skill);
            if(state=="蓄力")
            {
                var charge=session.Player.GetComponent<SkillChargeController>();
                Fill(r,new Color(.10f,.07f,.025f,.75f));Text(r,"蓄力",TouchFont(12),gold,true,false,TextAnchor.MiddleCenter);
                Bar(new Rect(r.x+3*TouchRatio,r.yMax-6*TouchRatio,r.width-6*TouchRatio,3*TouchRatio),charge.Progress,gold);
            }
            else if(state.Length==0)Fill(new Rect(r.xMax-7*TouchRatio,r.yMax-7*TouchRatio,4*TouchRatio,4*TouchRatio),jade);
            if(state=="缺能"||state=="限疗空")
            {
                Fill(r,new Color(.035f,.06f,.12f,.65f));
                Text(r,state,TouchFont(12),state=="缺能"?new Color(.5f,.72f,1):gold,true,false,TextAnchor.MiddleCenter);
            }
            string rejected=session.ControlFailure("skill"+skill);
            if(!string.IsNullOrEmpty(rejected))
            {Fill(r,new Color(.08f,.025f,.015f,.92f));Text(r,rejected,TouchFont(11),gold,true,false,TextAnchor.MiddleCenter);}
        }
    }
}
