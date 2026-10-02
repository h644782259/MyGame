namespace Emberfall
{
    public sealed partial class GameUI
    {
        private bool ReturnToMobilePauseRoot()
        {
            if (!MobileControls.Active || session == null || !session.Paused || panel != Panel.None || mobilePausePage <= 0) return false;
            mobilePausePage = 0;
            BlockUITransition();
            return true;
        }
    }
}
