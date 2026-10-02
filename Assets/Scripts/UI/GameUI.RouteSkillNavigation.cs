using UnityEngine;
namespace Emberfall
{
    public sealed partial class GameUI
    {
        private ProgressionService routeSkillOwner;
        private string routeSkillSlot;
        private int routeSkillTab;
        private bool RouteSkillReturnAvailable
        {get{return panel==Panel.Skills&&routeSkillOwner==session.Progression&&routeSkillSlot==session.Progression.CurrentSlotId;}}
        private void OpenRouteSkill(int skill)
        {
            if(!session.IsInCamp||skill<0||skill>=GameBalance.SkillCount)return;
            routeSkillOwner=session.Progression;routeSkillSlot=routeSkillOwner.CurrentSlotId;routeSkillTab=campTab;
            ReconcileMobileSkillOwner();
            if(selectedSkill!=skill){mobileSkillStatus=null;mobileSkillStatusFailed=false;}
            selectedSkill=skill;mobileSkillDetail=true;mobileSkillDetailScroll=Vector2.zero;
            desktopDetailScroll=Vector2.zero;desktopDetailSkill=skill;panel=Panel.Skills;
            CancelMobileScroll();session.SetUIBlocking(true);BlockUITransition();
        }
        private bool CloseRouteSkill()
        {
            if(!RouteSkillReturnAvailable)return false;
            routeSkillOwner=null;routeSkillSlot=null;panel=Panel.Camp;campTab=routeSkillTab;
            CancelMobileScroll();session.SetUIBlocking(true);BlockUITransition();return true;
        }
    }
}
