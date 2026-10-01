using UnityEngine;
namespace Emberfall
{
    public sealed partial class GameUI
    {
        private string MobileSkillState(int skill)
        {
            var p=session.Progression.Profile;var hero=session.Player;
            return MobileCombatPresentation.Skill(p.skillRanks[skill]>0,GameBalance.IsPassive(skill),
                hero==null?0:hero.SkillCooldownRemaining(skill),hero==null?0:hero.Energy,
                GameBalance.SkillEnergyCost(p.heroClass,skill),skill==6&&session.ChallengeRun&&session.InDungeon,session.HealingCharges);
        }
        private void DrawMobileSkillAvailability(Rect r,int skill)
        {
            string state=MobileSkillState(skill);
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
