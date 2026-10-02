using UnityEngine;
using System.Collections.Generic;
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
            BuildShoreWaterContact(parent,r,name,path,profile.Width,height);
        }
        // Same miter as the rendered ribbon: the waterline must meet the real surface edge.
        private static void RibbonSection(Vector3[] path,int i,out Vector3 normal,out float miter)
        {
            Vector3 before=i==0?path[1]-path[0]:path[i]-path[i-1];
            Vector3 after=i==path.Length-1?before:path[i+1]-path[i];
            Vector3 direction=(before.normalized+after.normalized).normalized;
            normal=new Vector3(-direction.z,0,direction.x);
            Vector3 sectionNormal=new Vector3(-before.z,0,before.x).normalized;
            miter=1f/Mathf.Max(.72f,Vector3.Dot(normal,sectionNormal));
        }
        private static void BuildShoreWaterContact(Transform parent,WorldResources r,string name,Vector3[] path,float width,float height)
        {
            // Static, merged bank details; cap malformed/very long authored paths without runtime updates.
            int sections=Mathf.Min(path.Length,65);
            Material damp=r.Material(new Color(.12f,.22f,.215f),false,VisualSurface.Stone);
            var wet=new List<Vector3>();var line=new List<Vector3>();
            var topTriangles=new List<int>();var lineTriangles=new List<int>();
            for(int side=-1;side<=1;side+=2)
            {
                int start=wet.Count;
                for(int i=0;i<sections;i++)
                {
                    Vector3 normal;float miter;RibbonSection(path,i,out normal,out miter);
                    Vector3 edge=path[i]+normal*(side*width*.5f*miter);edge.y=height;
                    Vector3 inner=edge-normal*(side*.045f),outer=edge+normal*(side*.12f);
                    inner.y=outer.y=height+.002f;wet.Add(inner);wet.Add(outer);
                    line.Add(edge+Vector3.up*.004f);line.Add(edge-Vector3.up*.012f);
                    if(i==sections-1)continue;
                    int a=start+i*2,b=a+2;
                    if(side==1){topTriangles.Add(a);topTriangles.Add(a+1);topTriangles.Add(b);topTriangles.Add(a+1);topTriangles.Add(b+1);topTriangles.Add(b);}
                    else{topTriangles.Add(a);topTriangles.Add(b);topTriangles.Add(a+1);topTriangles.Add(a+1);topTriangles.Add(b);topTriangles.Add(b+1);}
                    // Back faces are added with independent vertices below to retain lit normals.
                    int[] face={a,b,a+1,a+1,b,b+1};lineTriangles.AddRange(face);
                }
            }
            int frontVertices=line.Count,frontIndices=lineTriangles.Count;
            for(int i=0;i<frontVertices;i++)line.Add(line[i]);
            for(int i=0;i<frontIndices;i+=3)
            {lineTriangles.Add(lineTriangles[i+2]+frontVertices);lineTriangles.Add(lineTriangles[i+1]+frontVertices);lineTriangles.Add(lineTriangles[i]+frontVertices);}
            Geometry(parent,r,name+" shore damp seam",wet,topTriangles,damp);
            Geometry(parent,r,name+" shore vertical waterline",line,lineTriangles,damp);
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
