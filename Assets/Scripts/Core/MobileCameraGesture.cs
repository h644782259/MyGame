using System;
namespace Emberfall
{
 public sealed class MobileCameraGesture
 {
  public const float MinimumPitch=25,MaximumPitch=65,Threshold=9;
  public int Finger {get;private set;}=-1000;private float startY,lastY;private bool dragging;
  public bool Begin(int finger,float y){if(Finger!=-1000)return false;Finger=finger;startY=lastY=y;dragging=false;return true;}
  public float Move(int finger,float y,float density,bool ended,bool cancelled)
  {
   if(Finger!=finger)return 0;if(cancelled){Cancel();return 0;}
   density=float.IsNaN(density)||density<=0?1:density;float delta=0;
   if(!dragging&&Math.Abs(y-startY)/density>=Threshold){dragging=true;delta=(y-startY)/density;}
   else if(dragging)delta=(y-lastY)/density;
   lastY=y;if(ended)Cancel();return float.IsNaN(delta)||float.IsInfinity(delta)?0:Math.Max(-18,Math.Min(18,delta*.18f));
  }
  public void Cancel(){Finger=-1000;dragging=false;}
  public static float Apply(float pitch,float delta){if(float.IsNaN(delta)||float.IsInfinity(delta))return pitch;return Math.Max(MinimumPitch,Math.Min(MaximumPitch,pitch+delta));}
 }
}
