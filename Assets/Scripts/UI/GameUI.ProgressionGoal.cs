using UnityEngine;
namespace Emberfall
{
    public sealed partial class GameUI
    {
        private bool progressionGoalsOpen;
        private Vector2 progressionGoalScroll;
        private ProgressionService progressionGoalOwner;
        private string progressionGoalCharacter;
        private void OpenProgressionGoals()
        {
            progressionGoalsOpen=true;progressionGoalOwner=session.Progression;
            progressionGoalCharacter=session.Progression.CurrentSlotId;progressionGoalScroll=Vector2.zero;
            CancelMobileScroll();BlockUITransition();
        }
        private void ReconcileProgressionGoalSurface()
        {
            if(progressionGoalsOpen&&(panel!=Panel.Camp||progressionGoalOwner!=session.Progression||progressionGoalCharacter!=session.Progression.CurrentSlotId))
                progressionGoalsOpen=false;
        }
        private bool CloseProgressionGoalSurface()
        {
            if(!progressionGoalsOpen)return false;
            progressionGoalsOpen=false;CancelMobileScroll();BlockUITransition();return true;
        }
        private bool DrawProgressionGoalSurface()
        {
            ReconcileProgressionGoalSurface();if(!progressionGoalsOpen)return false;
            float u=MobileControls.Active?TouchRatio:1;
            var l=new MobileDialogLayout(width/u,height/u);
            Fill(new Rect(0,0,width,height),new Color(.018f,.031f,.048f,1));blockedRects.Add(new Rect(0,0,width,height));
            Box(BuildPlanRect(l.Frame,u),jade,false);
            Text(BuildPlanRect(l.Header,u),"成长目标 · 一次追踪一个",Mathf.RoundToInt(21*u),pale,true);
            float h=DrawProgressionGoalOptions(l.Body.Width-18,u,false);
            progressionGoalScroll=BeginTouchScroll("progression-goals",BuildPlanRect(l.Body,u),progressionGoalScroll,new Rect(0,0,(l.Body.Width-18)*u,Mathf.Max(l.Body.Height,h)*u));
            DrawProgressionGoalOptions(l.Body.Width-18,u,true);EndTouchScroll();
            if(Button(BuildPlanRect(l.FooterButton(0,1),u),"返回工坊",jade))CloseProgressionGoalSurface();
            return true;
        }
        private float DrawProgressionGoalOptions(float w,float u,bool draw)
        {
            var p=session.Progression;float y=8;
            string status=string.IsNullOrEmpty(p.LastError)?p.ProgressionGoalStatus():p.LastError;
            float h=Mathf.Ceil(Style(Mathf.RoundToInt(13*u),false,true).CalcHeight(new GUIContent(status),(w-16)*u)/u)+4;
            if(draw)Text(new Rect(8*u,y*u,(w-16)*u,h*u),status,Mathf.RoundToInt(13*u),jade,false,true);y+=h+12;
            GoalOption(ref y,w,u,"首件机制装备",ProgressionGoalKind.Core,null,0,draw);
            GoalOption(ref y,w,u,"保存第二套配装",ProgressionGoalKind.SecondPreset,null,0,draw);
            GoalOption(ref y,w,u,"通关第 "+p.HighestUnlockedAdventureTier+" 阶",ProgressionGoalKind.Tier,null,p.HighestUnlockedAdventureTier,draw);
            bool variants=false,ascensions=false;
            foreach(ItemData item in p.Profile.inventory)
            {
                if(item==null||item.mechanic==EquipmentMechanic.None||BuildCatalog.MechanicClass(item.mechanic)!=p.Profile.heroClass)continue;
                if(item.mechanic==EquipmentMechanic.FrostEcho||item.mechanic==EquipmentMechanic.CinderTrail)
                {variants=true;GoalOption(ref y,w,u,"解锁变体 · "+item.name,ProgressionGoalKind.Variant,item.id,0,draw);}
                ascensions=true;GoalOption(ref y,w,u,"传说升华 · "+item.name,ProgressionGoalKind.Ascension,item.id,0,draw);
            }
            if(!variants)GoalUnavailable(ref y,w,u,"变体目标 · 先获得元素机制装备",draw);
            if(!ascensions)GoalUnavailable(ref y,w,u,"升华目标 · 先获得机制装备",draw);
            GoalOption(ref y,w,u,"取消追踪",ProgressionGoalKind.None,null,0,draw);return y;
        }
        private void GoalOption(ref float y,float w,float u,string text,ProgressionGoalKind kind,string id,int tier,bool draw)
        {
            bool selected=session.Progression.Profile.progressionGoal==kind && (id==null||session.Progression.Profile.progressionGoalItemId==id) &&
                (kind!=ProgressionGoalKind.Tier||session.Progression.Profile.progressionGoalTier==tier);
            if(draw&&Button(new Rect(8*u,y*u,(w-16)*u,48*u),text+(selected?" ✓":""),selected?gold:jade))
            {Feedback(session.Progression.SelectProgressionGoal(kind,id,tier),"成长目标已保存");CancelMobileScroll();BlockUITransition();}
            y+=56;
        }
        private void GoalUnavailable(ref float y,float w,float u,string text,bool draw)
        {if(draw)Text(new Rect(8*u,y*u,(w-16)*u,40*u),text,Mathf.RoundToInt(13*u),muted,false,true);y+=48;}
    }
}
