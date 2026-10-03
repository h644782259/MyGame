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
            var window=session.Player==null?default(CombatOpportunityState):session.Player.SkillOpportunityWindow(skill);
            if(window.Window)
            {
                Rect clock=new Rect(r.x,r.yMax+2*TouchRatio,r.width,11*TouchRatio);
                Fill(clock,new Color(.025f,.045f,.06f,.95f));
                Text(clock,window.Caption,TouchFont(8),window.Actionable?jade:new Color(.58f,.61f,.65f),true,false,TextAnchor.MiddleCenter);
            }

            string targetReason=state.Length==0&&session.Player!=null?session.Player.MobilePinnedActionReason(skill):"";
            Rect caption=new Rect(r.x,r.yMax-15*TouchRatio,r.width,15*TouchRatio);
            if(state=="蓄力")
            {
                var charge=session.Player.GetComponent<SkillChargeController>();
                Fill(caption,new Color(.10f,.07f,.025f,.75f));Text(caption,"蓄力",TouchFont(12),gold,true,false,TextAnchor.MiddleCenter);
                Bar(new Rect(r.x+3*TouchRatio,r.yMax-6*TouchRatio,r.width-6*TouchRatio,3*TouchRatio),charge.Progress,gold);
            }
            else if(state.Length==0&&targetReason.Length==0&&(!window.Window||window.Actionable))Fill(new Rect(r.xMax-7*TouchRatio,r.yMax-7*TouchRatio,4*TouchRatio,4*TouchRatio),jade);
            if(state=="缺能"||state=="限疗空")
            {
                Fill(caption,new Color(.035f,.06f,.12f,.65f));
                Text(caption,state,TouchFont(12),state=="缺能"?new Color(.5f,.72f,1):gold,true,false,TextAnchor.MiddleCenter);
            }
            var opportunity=window;
            if(state.Length==0&&targetReason.Length==0&&opportunity.Actionable)
            {Fill(caption,new Color(.055f,.16f,.12f,.95f));Text(caption,opportunity.Caption,TouchFont(10),jade,true,false,TextAnchor.MiddleCenter);}
            if(targetReason.Length>0)
            {Fill(caption,new Color(.08f,.025f,.015f,.92f));Text(caption,MobileCombatPresentation.SkillRejectionCaption(targetReason),TouchFont(11),gold,true,false,TextAnchor.MiddleCenter);}
            string rejected=session.ControlFailure("skill"+skill);
            if(!string.IsNullOrEmpty(rejected))
            {Fill(caption,new Color(.08f,.025f,.015f,.92f));Text(caption,MobileCombatPresentation.SkillRejectionCaption(rejected),TouchFont(11),gold,true,false,TextAnchor.MiddleCenter);}
        }
    }
}
