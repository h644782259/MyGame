using UnityEngine;
namespace Emberfall
{
    public static partial class WorldBuilder
    {
        private static void BuildWaterSurface(Transform parent,WorldResources r,string name,Vector3[] path,float width,float height,WaterEnvironment environment)
        {
            if(path==null||path.Length<2)return;
            var profile=new WaterPresentation(environment,width);
            // Opaque depth bands are independent of framebuffer/refraction support on mobile.
            // All layers remain INSIDE the existing traversal river width.
            Material shallows=r.Material(new Color(.16f,.38f,.40f),false,VisualSurface.Water);
            Material depth=r.Material(new Color(.045f,.18f,.24f),false,VisualSurface.Water);
            Material current=r.Material(new Color(.26f,.48f,.49f)*profile.CurrentBrightness,false,VisualSurface.Water);
            Ribbon(parent,r,name+" shallow banks",path,profile.Width,height,shallows);
            Ribbon(parent,r,name+" deep bed",path,profile.DeepWidth,height+.0015f,depth);
            Vector3[] flow=new Vector3[path.Length];
            for(int i=0;i<path.Length;i++)
            {
                Vector3 tangent=(path[Mathf.Min(path.Length-1,i+1)]-path[Mathf.Max(0,i-1)]).normalized;
                flow[i]=path[i]+new Vector3(-tangent.z,0,tangent.x)*profile.CurrentOffset;
            }
            Ribbon(parent,r,name+" broad reflected current",flow,profile.CurrentWidth,height+.003f,current);
            WaterFlowBands.Create(parent,flow,profile.CurrentWidth*.72f,height+.004f,environment,
                r.Material(new Color(.31f,.55f,.55f)*profile.CurrentBrightness,false,VisualSurface.Water));
        }
        private static void BuildBridgeWaterContact(Transform parent,WorldResources r,Rect footprint,float deckTop)
        {
            // Wet contact/wear seams sit inside the authored bridge deck, never new obstacles.
            Material damp=r.Material(new Color(.14f,.20f,.20f),false,VisualSurface.Wood);
            float x=footprint.center.x,z=footprint.center.y;
            for(int side=-1;side<=1;side+=2)
            {
                Primitive(parent,"Bridge damp bank contact",PrimitiveType.Cube,new Vector3(x,deckTop+.002f,z+side*(footprint.height*.5f-.11f)),new Vector3(footprint.width-.08f,.004f,.16f),damp);
                Primitive(parent,"Bridge deck edge wear",PrimitiveType.Cube,new Vector3(x+side*(footprint.width*.5f-.08f),deckTop+.002f,z),new Vector3(.08f,.004f,footprint.height-.12f),damp);
            }
        }
    }
}
