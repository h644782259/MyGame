using System;using System.Linq;using System.Collections.Generic;using UnityEngine;using Emberfall;
namespace UnityEngine
{
 public static class ColorUtility{public static string ToHtmlStringRGBA(Color c)=>$"{c.r},{c.g},{c.b},{c.a}";}
 public struct Rect{public float xMin,xMax,yMin,yMax;public Rect(float x,float y,float w,float h){xMin=x;xMax=x+w;yMin=y;yMax=y+h;}public float width=>xMax-xMin;public float height=>yMax-yMin;public Vector2 center=>new Vector2((xMin+xMax)*.5f,(yMin+yMax)*.5f);}
 public partial struct Vector3{public void Normalize(){this=normalized;}public static float Dot(Vector3 a,Vector3 b)=>a.x*b.x+a.y*b.y+a.z*b.z;public static float Distance(Vector3 a,Vector3 b)=>(a-b).magnitude;public static Vector3 ClampMagnitude(Vector3 a,float m)=>a.magnitude>m?a.normalized*m:a;}
 public static partial class Mathf{public static int Min(int a,int b)=>Math.Min(a,b);public static int Clamp(int a,int b,int c)=>Math.Min(c,Math.Max(a,b));public static int CeilToInt(float v)=>(int)Math.Ceiling(v);public static int RoundToInt(float v)=>(int)Math.Round(v);}
}
namespace Emberfall
{
 public enum ZoneKind{Dungeon,Wilderness}internal enum VisualSurface{Stone,Water,Wood,Crystal}
 public static class ProceduralVisuals{internal static void ApplySurface(Material m,VisualSurface s){}}
 public static class PlayerUpgradeRules{public static float FindSafeBlinkDistance(float d,Func<float,bool>a,Func<float,bool>b){throw new NotSupportedException();}}
 public static partial class CombatFx{public static float SegmentDistance(Vector3 p,Vector3 a,Vector3 b){p.y=a.y=b.y=0;var d=b-a;return Vector3.Distance(p,a+d*(d.sqrMagnitude<.0001f?0:Mathf.Clamp01(Vector3.Dot(p-a,d)/d.sqrMagnitude)));}}
 public static class WaterFlowBands{public static void Create(Transform p,Vector3[] path,float width,float height,WaterEnvironment environment,Material material){}}
 public static partial class WorldBuilder
 {
  public static GameObject Primitive(Transform p,string name,PrimitiveType type,Vector3 at,Vector3 size,Material m){var o=new GameObject(name);o.transform.SetParent(p,false);o.transform.localPosition=at;o.transform.localScale=size;return o;}
  private static void WaterEntry(bool baseline,Transform p,WorldResources r,string name,Vector3[] path,float width,float height,WaterEnvironment env){if(!baseline)BuildWaterSurface(p,r,name,path,width,height,env);}
  public static void LongPath(Transform p,WorldResources r){var path=Enumerable.Range(0,200).Select(i=>new Vector3(i,0,0)).ToArray();BuildWaterSurface(p,r,"long",path,2,.02f,WaterEnvironment.Brook);}
 }
}
public static class ShoreContactProductionTests
{
 static int checks;static void Check(bool v,string why){checks++;if(!v)throw new Exception(why);}
 static void Clear(){foreach(var o in GameObject.All.ToArray())UnityEngine.Object.Destroy(o);GameObject.All.Clear();}
 static bool[] Nav(){var result=new List<bool>();for(int x=-15;x<=15;x++)for(int z=-10;z<=10;z++)result.Add(WorldTraversal.IsWalkable(new Vector3(x,0,z),.45f));return result.ToArray();}
 static void Entry(int scene,Transform p,WorldResources r,bool baseline){if(scene==0)WorldBuilder.Brook(p,r,22,baseline);else if(scene==1)WorldBuilder.Courtyard(p,r,22,baseline);else WorldBuilder.Tactical(p,r,scene==2?22:23,baseline);}
 static void ValidateNormals(Mesh mesh,string tag)
 {
  var sums=new Vector3[mesh.vertices.Length];var used=new int[sums.Length];
  for(int i=0;i<mesh.triangles.Length;i+=3){int a=mesh.triangles[i],b=mesh.triangles[i+1],c=mesh.triangles[i+2];var n=Vector3.Cross(mesh.vertices[b]-mesh.vertices[a],mesh.vertices[c]-mesh.vertices[a]);Check(n.sqrMagnitude>1e-12f,tag+" triangle must have nonzero area");foreach(int j in new[]{a,b,c}){sums[j]+=n;used[j]++;}}
  Check(mesh.normals!=null&&mesh.normals.Length==sums.Length,tag+" production Geometry must recalculate normals");
  for(int i=0;i<sums.Length;i++){
   Check(used[i]>0&&sums[i].sqrMagnitude>1e-12f,tag=="waterline"?"WATERLINE_NORMAL_ZERO":"wet seam normal must be nonzero");
   var normal=mesh.normals[i];Check(!float.IsNaN(normal.x)&&!float.IsNaN(normal.y)&&!float.IsNaN(normal.z)&&Math.Abs(normal.magnitude-1)<.00001f,tag+" normal must be finite and normalized");
   Check(Vector3.Dot(normal,sums[i].normalized)>.99999f,tag+" normal must follow actual incident triangle orientation");
   Check(tag=="waterline"?Math.Abs(normal.y)<.00001f:normal.y>.99999f,tag+" normal follows vertical line or upward wet band");
  }
 }
 public static string Run()
 {
  for(int scene=0;scene<4;scene++)
  {
   Clear();WorldTraversal.Reset(ZoneKind.Wilderness);var root=new GameObject("world");var resources=root.AddComponent<WorldResources>();Entry(scene,root.transform,resources,true);var original=Nav();float bx=scene<2?0:scene==2?7:-7;
   bool route=WorldTraversal.CanReach(new Vector3(bx,0,-5),new Vector3(bx,0,5),.45f);Check(route,"authored bridge has reachable crossing before visuals");
   Clear();WorldTraversal.Reset(ZoneKind.Wilderness);root=new GameObject("world");resources=root.AddComponent<WorldResources>();int revision=WorldTraversal.Revision;Entry(scene,root.transform,resources,false);
   Check(WorldTraversal.Revision==revision+1&&Nav().SequenceEqual(original)&&WorldTraversal.CanReach(new Vector3(bx,0,-5),new Vector3(bx,0,5),.45f)==route,"actual shoreline entry preserves traversal registration and reachability");
   var wet=GameObject.All.Where(o=>o.name.EndsWith("shore damp seam")).ToArray();var line=GameObject.All.Where(o=>o.name.EndsWith("shore vertical waterline")).ToArray();Check(wet.Length==1&&line.Length==1,"actual water entry must build both static shoreline layers");
   var water=GameObject.All.Single(o=>o.name.EndsWith("shallow banks")&&!o.name.StartsWith("Pebble"));var waterMesh=water.GetComponent<MeshFilter>().sharedMesh;var wm=wet[0].GetComponent<MeshFilter>().sharedMesh;var lm=line[0].GetComponent<MeshFilter>().sharedMesh;int sections=waterMesh.vertices.Length/2;
   ValidateNormals(lm,"waterline");ValidateNormals(wm,"wet");
   Check(wm.vertices.Length==4*sections&&lm.vertices.Length==8*sections&&wm.triangles.Length>0&&lm.triangles.Length>0,"both true banks are nonempty merged static geometry");
   Check(lm.triangles.Length==24*(sections-1)&&wm.triangles.Length==12*(sections-1),"splitting normals retains original waterline and wet-band triangle counts");
   int front=lm.vertices.Length/2,frontTriangles=lm.triangles.Length/2;
   for(int i=0;i<front;i++){Check((lm.vertices[i]-lm.vertices[i+front]).sqrMagnitude<1e-12f,"front/back waterline vertices coincide without sharing indices");Check(Vector3.Dot(lm.normals[i],lm.normals[i+front])<-.99999f,"paired waterline normals face opposite directions");}
   for(int i=0;i<frontTriangles;i+=3)for(int j=0;j<3;j++)Check(lm.triangles[frontTriangles+i+j]==lm.triangles[i+2-j]+front,"back triangles use reversed winding and independent vertices");
   Check(ReferenceEquals(wet[0].GetComponent<MeshRenderer>().sharedMaterial,line[0].GetComponent<MeshRenderer>().sharedMaterial),"wet seam and vertical line share world-owned material");
   for(int side=0;side<2;side++)for(int i=0;i<sections;i++)
   {
    var edge=waterMesh.vertices[i*2+(side==0?1:0)];var high=lm.vertices[side*sections*2+i*2];var low=lm.vertices[side*sections*2+i*2+1];
    Check(Math.Abs(edge.x-high.x)<.00001f&&Math.Abs(edge.z-high.z)<.00001f&&high.x==low.x&&high.z==low.z,"vertical shoreline meets actual mitered water edge");Check(Math.Abs(high.y-low.y-.016f)<.00001f&&low.y<edge.y&&high.y>edge.y,"waterline crosses actual water height vertically");
    var inner=wm.vertices[side*sections*2+i*2];var outer=wm.vertices[side*sections*2+i*2+1];Check(Math.Abs(CombatFx.Flat(outer-inner).magnitude-.165f)<.00001f,"wet bank width is bounded independently of path bends");
   }
   foreach(int k in Enumerable.Range(0,wm.triangles.Length/3)){Vector3 a=wm.vertices[wm.triangles[k*3]],b=wm.vertices[wm.triangles[k*3+1]],c=wm.vertices[wm.triangles[k*3+2]];Check(Vector3.Cross(b-a,c-a).y>0,"wet seam faces camera above water");}
   float deck=scene==0?.049f:scene==1?.07f:.14f;Check(lm.vertices.All(v=>v.y<deck)&&wm.vertices.All(v=>v.y<deck),"shore contact stays below existing bridge deck");
   var mat=line[0].GetComponent<MeshRenderer>().sharedMaterial;UnityEngine.Object.Destroy(root);Check(wm.Destroyed&&lm.Destroyed&&mat.Destroyed,"world teardown owns and releases static contact resources");
  }
  Clear();var longRoot=new GameObject("long");WorldBuilder.LongPath(longRoot.transform,longRoot.AddComponent<WorldResources>());foreach(var o in GameObject.All.Where(o=>o.name.Contains(" shore "))){var m=o.GetComponent<MeshFilter>().sharedMesh;ValidateNormals(m,o.name.EndsWith("waterline")?"waterline":"wet");Check(m.vertices.Length<=(o.name.EndsWith("waterline")?520:260)&&m.triangles.Length<=(o.name.EndsWith("waterline")?1536:768),"authored path overflow has bounded shoreline topology");}Check(GameObject.All.Where(o=>o.name.Contains(" shore ")).Sum(o=>o.GetComponent<MeshFilter>().sharedMesh.vertices.Length)<=780,"total static shore vertex budget is 260 wet plus 520 double-sided line");Clear();
  return "PASS: "+checks+" actual brook/courtyard/both tactical water-entry shoreline/mesh/ownership/traversal checks (managed, not Unity rendering)";
 }
}
