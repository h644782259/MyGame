namespace Emberfall
{
    public sealed partial class GameUI
    {
        private void FollowCampRouteStep(CampRouteCard route,int index)
        {
            if(!session.IsInCamp)return;
            var p=session.Progression;
            switch(route.NextAction)
            {
                case CampRouteAction.Skill:panel=Panel.Skills;selectedSkill=route.NextSkill;break;
                case CampRouteAction.Specialization:Feedback(p.SetSpecialization(index==0?ElementalistSpecialization.Shatter:ElementalistSpecialization.Burn,true),"专精已切换");break;
                case CampRouteAction.SummonerRoute:Feedback(p.SetSummonerRoute((SummonerRoute)index,true),"契约已切换");break;
                case CampRouteAction.Inventory:panel=Panel.Inventory;break;
                case CampRouteAction.TrackCore:
                    if(p.SelectCoreGoal(route.Mechanic))OpenProgressionGoals();else Feedback(false,p.LastError);break;
            }
            CancelMobileScroll();BlockUITransition();
        }
    }
}
