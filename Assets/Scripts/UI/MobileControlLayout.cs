using System;

namespace Emberfall
{
    /// <summary>Pure geometry in density-independent touch units. No gameplay state or device APIs.</summary>
    public sealed class MobileControlLayout
    {
        public struct Area
        {
            public float X, Y, Width, Height;
            public Area(float x, float y, float w, float h) { X=x;Y=y;Width=w;Height=h; }
            public bool Contains(float x,float y) { return x>=X&&x<X+Width&&y>=Y&&y<Y+Height; }
            public bool Overlaps(Area r) { return X<r.X+r.Width&&X+Width>r.X&&Y<r.Y+r.Height&&Y+Height>r.Y; }
        }
        public readonly float Scale, Width, Height;
        public readonly bool Tablet;
        public readonly Area Joystick, MoveZone, Attack, Dodge, Potion, Jump, Cancel, Menu, Inventory, SkillsMenu, Interact;
        public readonly Area EncounterText, BossHealth, Notice, AdventureStatus;
        public readonly Area[] Skills = new Area[10];
        public MobileControlLayout(float pixelWidth,float pixelHeight,float dpi)
        {
            pixelWidth=Math.Max(1,pixelWidth);pixelHeight=Math.Max(1,pixelHeight);
            Tablet=pixelWidth/pixelHeight<1.65f;
            float fallback=pixelHeight/(Tablet?768f:390f);
            // Screen.dpi is advisory; reject missing/implausible values and constrain
            // density so 320-point compact phones still fit the full combat cluster.
            float density=dpi>=120&&dpi<=700?dpi/163f:fallback;
            Scale=Math.Max(.25f,Math.Min(density,Math.Min(pixelHeight/320f,pixelWidth/568f)));
            Width=pixelWidth/Scale;Height=pixelHeight/Scale;
            Joystick=Centered(90,Height-86,128);
            MoveZone=new Area(12,Height-172,175,160);
            Attack=Centered(Width-61,Height-57,84);
            Dodge=Centered(Width-152,Height-56,62);
            Jump=Centered(Width-224,Height-56,56);
            Cancel=Jump; // Same thumb position, mutually exclusive with jump.
            Potion=Centered(50,Height-211,54);
            Interact=new Area(84,Height-235,120,48);
            for(int i=0;i<Skills.Length;i++)
                Skills[i]=Centered(Width-286+(i%5)*62,Height-(i<5?201:137),54);
            Menu=Centered(Width-32,32,48);
            Inventory=Centered(Width-91,32,48);
            SkillsMenu=Centered(Width-150,32,48);
            // Keep encounter feedback below the menu row, above the skill strip.
            // A centered bar at y=98 crosses the first skill row on 320-unit phones.
            EncounterText=new Area(Width-184,62,172,18);
            BossHealth=new Area(Width-184,83,172,5);
            // Four short objective lines + a real capture bar. At 568x320 the
            // card ends at y=84, above interaction and the first skill row.
            float objectiveWidth=Math.Min(236,Width-380);
            AdventureStatus=new Area((Width-objectiveWidth)/2,8,objectiveWidth,76);
            // The short feedback card uses the gap between the movement zone and
            // jump button, below the skill strip, never covering an action target.
            Notice=new Area(198,Height-106,Math.Min(320,Jump.X-210),94);
        }
        private static Area Centered(float x,float y,float size) { return new Area(x-size/2,y-size/2,size,size); }
        public static float DeadZone(float value,float dead=.14f)
        { float magnitude=Math.Abs(value);return magnitude<=dead?0:Math.Sign(value)*Math.Min(1,(magnitude-dead)/(1-dead)); }
    }
}
