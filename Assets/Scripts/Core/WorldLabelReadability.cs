using System;
namespace Emberfall
{
    public static class WorldLabelReadability
    {
        public static float Scale(float projectedPixels,int lines)
        {
            if(float.IsNaN(projectedPixels)||float.IsInfinity(projectedPixels)||projectedPixels<=.01f)return 1;
            int count=Math.Max(1,Math.Min(8,lines));
            return Math.Max(12*count,Math.Min(22*count,projectedPixels))/projectedPixels;
        }
    }
}
