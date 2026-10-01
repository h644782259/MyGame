using System;
namespace Emberfall
{
    // Logical units shared by desktop and safe-area touch canvases. Five entries are always visible.
    public sealed class AdventureSelectionLayout
    {
        public readonly float X,Y,OptionsY,FooterY;
        public AdventureSelectionLayout(float width,float height)
        {
            X=(width-520)*.5f;Y=(height-302)*.5f;
            OptionsY=Y+198;FooterY=Y+254;
        }
        public MobilePanelLayout.Area Entry(int index)
        {
            if(index<0||index>=5)throw new ArgumentOutOfRangeException(nameof(index));
            return new MobilePanelLayout.Area(X+(index%2)*266,Y+30+(index/2)*56,254,50);
        }
        public static float LogHeight(int messages,bool expanded)
        {return messages<=0?0:Math.Min(expanded?8:3,messages)*39+34;}
        public static float WorkshopY(float height,int messages,bool expanded)
        {return Math.Min(height-223,height-16-LogHeight(messages,expanded)-44);}
    }
}
