using UnityEngine;
namespace Emberfall
{
    public sealed partial class GameUI
    {
        private int opportunityEpoch=-1;
        private PlayerController opportunityOwner;
        private string commandStatus;
        private bool commandRecall;
        private float commandStatusUntil;
        private int commandEpoch;
        public bool CompanionCommandsVisible {get{return session!=null&&session.Player!=null&&session.Player.HeroClass==HeroClass.Summoner&&session.HasStarted&&!session.IsDead;}}
        public void ActivateFreeCommand(bool recall)
        {
            if(!CompanionCommandsVisible||session.InputBlocked||panel!=Panel.None)return;
            var hero=session.Player;var target=hero.AimTarget;
            bool success=recall?SummonedCompanion.FreeRecall(hero):SummonedCompanion.SetFreeFocus(hero,target);
            commandStatus=success?(recall?"已召回":"已集火"):!recall&&(target==null||target.IsDead)?"无目标":!recall&&CombatFx.Flat(target.transform.position-hero.transform.position).sqrMagnitude>196f?"太远":"不可用";
            commandRecall=recall;commandEpoch=hero.CombatEpoch;commandStatusUntil=Time.unscaledTime+1.1f;
        }
        private void DrawCompanionCommands()
        {
            if(!CompanionCommandsVisible)return;
            float previousOpacity=controlOpacity;if(MobileControls.Active)controlOpacity=EffectPreferences.TouchOpacity;
            for(int i=0;i<2;i++)
            {
                bool recall=i==1;
                Rect r=MobileControls.Active?TouchRect(recall?MobileControls.Layout.RecallCommand:MobileControls.Layout.FocusCommand):new Rect(hotbarBounds.x-76,hotbarBounds.y+27+i*50,66,46);
                blockedRects.Add(r);if(MobileControls.Active)r=MobileVisualRect(r);
                string caption=recall?"召回":"集火";
                if(!session.InputBlocked&&commandEpoch==session.Player.CombatEpoch&&Time.unscaledTime<commandStatusUntil&&commandRecall==recall)caption=commandStatus;
                Box(r,jade,false);
                Text(new Rect(r.x,r.y+4*(MobileControls.Active?TouchRatio:1),r.width,r.height*.5f),caption,MobileControls.Active?TouchFont(11):12,pale,true,false,TextAnchor.MiddleCenter);
                Text(new Rect(r.x,r.y+r.height*.55f,r.width,r.height*.35f),"免费",MobileControls.Active?TouchFont(9):10,jade,false,false,TextAnchor.MiddleCenter);
                // MobileControls owns all physical pointers; the IMGUI surface is
                // presentation-only on touch devices, preventing duplicate orders.
                if(!MobileControls.Active&&GUI.Button(r,GUIContent.none,invisibleButton))ActivateFreeCommand(recall);
            }
            controlOpacity=previousOpacity;
        }
        private string CurrentCombatOpportunity()
        {
            var hero=session.Player;
            if(hero==null||hero.IsDead||session.InputBlocked||!session.HasStarted)return "";
            if(opportunityOwner!=hero||opportunityEpoch!=hero.CombatEpoch)
            {opportunityOwner=hero;opportunityEpoch=hero.CombatEpoch;return "";}
            if(hero.HeroClass==HeroClass.Vanguard)return CombatOpportunityPresentation.Vanguard(hero.CounterOpportunityRemaining);
            if(hero.HeroClass==HeroClass.Summoner)
            {
                int count;float lifetime;SummonedCompanion.DescribeRoster(hero,out count,out lifetime);
                return CombatOpportunityPresentation.Summoner(count,lifetime,SummonedCompanion.CommandOpportunityRemaining(hero));
            }
            var target=hero.CurrentOpportunityTarget;
            if(target==null||!session.Enemies.Contains(target)||!target.gameObject.activeInHierarchy)return "";
            var status=target.StatusEffects;if(status==null)return "";
            if(hero.HeroClass==HeroClass.Arcanist)
            {
                var charge=hero.GetComponent<SkillChargeController>();
                bool castBlocked=hero.IsJumping||charge!=null&&(charge.IsCharging||charge.ConsumedThisFrame);
                bool ready=CombatOpportunityPresentation.MeteorReady(session.Progression.Profile.skillRanks[1]>0,
                    hero.SkillCooldownRemaining(1),hero.Energy,GameBalance.SkillEnergyCost(hero.HeroClass,1),castBlocked);
                return CombatOpportunityPresentation.Arcanist(status.HasFrostMark,status.IsBurning,
                    hero.Specialization==ElementalistSpecialization.Burn,ready);
            }
            return CombatOpportunityPresentation.Ranger(status.PoisonStacks,status.IsMarked);
        }
    }
}
