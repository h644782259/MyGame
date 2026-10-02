namespace Emberfall
{
    public sealed partial class GameUI
    {
        private bool TryGrowthHudHint(out string title,out string detail)
        {
            return ProgressionHudHint.TryGet(session.Progression,session.IsInCamp?Attention:null,
                session.InDungeon||session.SpecialAdventure||session.IsDead,session.IsInCamp,ProgressionHudHint.TutorialUsable(session.Progression.Profile,MobileControls.Active,session.ClassTutorialVisible),out title,out detail);
        }
    }
}
