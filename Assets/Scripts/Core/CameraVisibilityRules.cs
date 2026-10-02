using System;
namespace Emberfall
{
    public static class CameraVisibilityRules
    {
        public const int MaximumSurfaces=256,MaximumFaded=32;
        public static float FadeStep(float current,bool occluded,float seconds)
        {
            if(float.IsNaN(seconds)||float.IsInfinity(seconds)||seconds<=0)return current;
            float target=occluded?.2f:1f;
            float blend=1-(float)Math.Exp(-10*Math.Min(seconds,.1f));
            return Math.Max(.2f,Math.Min(1,current+(target-current)*blend));
        }
        public static float Zoom(float normal,float nearestBuilding)
        {float proximity=1-Math.Max(0,Math.Min(1,nearestBuilding/4));return Math.Max(13,normal*(1-.16f*proximity));}
        public static float Projection(float normalizedAnchor)
        {return 1-2*Math.Max(.25f,Math.Min(.75f,normalizedAnchor));}
    }
}
