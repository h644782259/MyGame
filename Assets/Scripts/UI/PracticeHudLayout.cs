using System;
namespace Emberfall
{
    // Same touch-unit top band reserved by MobileControlLayout. No combat-centre card.
    public sealed class PracticeHudLayout
    {
        public readonly MobilePanelLayout.Area Header,Sidebar,Primary,Leave;
        public PracticeHudLayout(float width)
        {
            width=float.IsNaN(width)||float.IsInfinity(width)?568:Math.Max(568,width);
            Sidebar=new MobilePanelLayout.Area(12,8,172,62);
            Header=new MobilePanelLayout.Area(190,8,width-372,48);
            Primary=new MobilePanelLayout.Area(width-176,8,80,48);
            Leave=new MobilePanelLayout.Area(width-88,8,80,48);
        }
    }
}
