using System;using System.Linq;using UnityEngine;using Emberfall;
namespace Emberfall
{
 public enum ZoneKind{Dungeon,Wilderness}
 public static class PlayerUpgradeRules{public static float FindSafeBlinkDistance(float d,Func<float,bool>a,Func<float,bool>b){throw new NotSupportedException();}}
 public static partial class CombatFx{public static float SegmentDistance(Vector3 p,Vector3 a,Vector3 b){p.y=a.y=b.y=0;var d=b-a;return Vector3.Distance(p,a+d*(d.sqrMagnitude<.0001f?0:Mathf.Clamp01(Vector3.Dot(p-a,d)/d.sqrMagnitude)));}}
}
namespace UnityEngine
{
 public struct Rect{public float xMin,xMax,yMin,yMax;public Rect(float x,float y,float w,float h){xMin=x;xMax=x+w;yMin=y;yMax=y+h;}}
 public partial struct Vector3{public void Normalize(){this=normalized;}public static float Dot(Vector3 a,Vector3 b)=>a.x*b.x+a.y*b.y+a.z*b.z;public static float Distance(Vector3 a,Vector3 b)=>(a-b).magnitude;public static Vector3 ClampMagnitude(Vector3 a,float m)=>a.magnitude>m?a.normalized*m:a;}
 public static partial class Mathf{public static int Min(int a,int b)=>Math.Min(a,b);public static int Clamp(int a,int b,int c)=>Math.Min(c,Math.Max(a,b));public static int CeilToInt(float v)=>(int)Math.Ceiling(v);public static int RoundToInt(float v)=>(int)Math.Round(v);}
}
public static class AnchoredImpactCoverageTests
{
 static int checks,faces,vertices,maxSegments,maxProbes,maxTriangles;static void Check(bool v,string why){checks++;if(!v)throw new Exception(why);}
 static PlayerController Reset(){foreach(var o in GameObject.All.ToArray())UnityEngine.Object.Destroy(o);GameObject.All.Clear();Check(CombatVisualLease.Active==0,"mesh cleanup returns leases");WorldTraversal.Reset(ZoneKind.Dungeon);Application.isMobilePlatform=true;EffectPreferences.ReducedEffects=false;var hero=new GameObject("hero").AddComponent<PlayerController>();GameSession.Instance=new GameSession{HasStarted=true,Player=hero};return hero;}
 static void Verify(GameObject part,Vector3 origin,bool dense)
 {
  var mesh=part.GetComponent<MeshFilter>().sharedMesh;Check(mesh.vertices.Length<=12288,"clipping vertex budget is bounded");var points=mesh.vertices.Select(v=>part.transform.TransformPoint(v)).ToArray();
  if(dense)for(int i=0;i<mesh.triangles.Length;i+=3)
  {
   faces++;Vector3 a=points[mesh.triangles[i]],b=points[mesh.triangles[i+1]],c=points[mesh.triangles[i+2]];
   // Independent 1/17 barycentric grid, deliberately not production centroid/edge-midpoint samples.
   for(int u=0;u<=17;u++)for(int v=0;v<=17-u;v++)Check(CombatSight.Area(origin,a*(u/17f)+b*(v/17f)+c*((17-u-v)/17f)),"retained anchored triangle crosses actual finite-cover LOS");
  }
  foreach(var v in points){vertices++;Check(CombatSight.Area(origin,v),"animated anchored vertex crosses actual finite-cover LOS");}
 }
 public static string RunMotion()
 {
  int witnessed=0;
  foreach(float radius in new[]{.12f,.28f,.48f})foreach(float distance in new[]{.35f,.65f,1f,1.5f})for(int angle=0;angle<24;angle++)
  {
   if(distance<=radius+.05f)continue;
   var hero=Reset();float a=angle*Mathf.PI/12+.13f;WorldTraversal.AddCircle(new Vector3(Mathf.Cos(a)*distance,0,Mathf.Sin(a)*distance),radius);
   FilledSkillVfx.Impact(hero,Vector3.zero,4,FilledVfxKind.Lightning,new Color(1,1,1));var effect=GameObject.All.Single(o=>o.GetComponent<FilledSkillVfx>()!=null);var part=GameObject.All.Single(o=>o.name.Contains("Primary "));var mesh=part.GetComponent<MeshFilter>().sharedMesh;
   if(mesh.vertices.Length==0||!mesh.vertices.All(v=>CombatSight.Area(Vector3.zero,part.transform.TransformPoint(v))))continue;
   witnessed++;Time.deltaTime=.2f;effect.Call("Update");
   foreach(var v in mesh.vertices)Check(CombatSight.Area(Vector3.zero,part.transform.TransformPoint(v)),"animated anchored vertex crosses actual finite-cover LOS");
  }
  Check(witnessed>0,"animation oracle observes initially visible lightning");Reset();return "PASS: "+witnessed+" initially visible finite-cover lightning animations";
 }
 public static string Run()
 {
  Console.WriteLine(RunMotion());
  foreach(var type in new[]{FilledVfxKind.Arcane,FilledVfxKind.Lightning})
  {
   var hero=Reset();FilledSkillVfx.Impact(hero,Vector3.zero,4,type,new Color(1,1,1));
   var primary=GameObject.All.Single(o=>o.name.Contains("Primary "));var original=(Mesh)typeof(FilledSkillVfx).GetField(type==FilledVfxKind.Arcane?"arcane":"lightning",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).GetValue(null);
   Check(primary.GetComponent<MeshFilter>().sharedMesh.triangles.Length==original.triangles.Length,"unobstructed primary retains every original identity face");
   hero=Reset();WorldTraversal.AddBox(new Vector3(-.65f,0,0),new Vector2(.2f,6));WorldTraversal.AddBox(new Vector3(.65f,0,0),new Vector2(.2f,6));FilledSkillVfx.Impact(hero,Vector3.zero,4,type,new Color(1,1,1));primary=GameObject.All.Single(o=>o.name.Contains("Primary "));
   Check(primary.GetComponent<MeshFilter>().sharedMesh.triangles.Length>0,"narrow corridor retains actual primary silhouette faces");Verify(primary,Vector3.zero,true);
  }
  foreach(var type in new[]{FilledVfxKind.Arcane,FilledVfxKind.Lightning})
  foreach(float distance in new[]{.65f,1f,1.5f,2f})for(int angle=0;angle<12;angle++)
  {
   var hero=Reset();float a=angle*Mathf.PI/6+.13f;WorldTraversal.AddCircle(new Vector3(Mathf.Cos(a)*distance,0,Mathf.Sin(a)*distance),.28f);
   WorldTraversal.TestSegmentCalls=WorldTraversal.TestSolidProbes=0;
   FilledSkillVfx.Impact(hero,Vector3.zero,4,type,new Color(1,1,1));maxSegments=Math.Max(maxSegments,WorldTraversal.TestSegmentCalls);maxProbes=Math.Max(maxProbes,WorldTraversal.TestSolidProbes);var effect=GameObject.All.Single(o=>o.GetComponent<FilledSkillVfx>()!=null);var parts=GameObject.All.Where(o=>o.name.Contains("Primary ")||o.name.Contains("Landing base")||o.name.Contains("Contact flash")).ToArray();
   maxTriangles=Math.Max(maxTriangles,parts.Sum(o=>o.GetComponent<MeshFilter>().sharedMesh.triangles.Length/3));
   Check(parts.Length==3&&parts.All(o=>o.GetComponent<MeshFilter>().sharedMesh.triangles.Length>0),"finite cover preserves reachable anchored feedback");Check(effect.transform.position.sqrMagnitude==0,"real impact center cannot relocate");
   foreach(var part in parts)Verify(part,Vector3.zero,true);
   for(int frame=1;frame<=9;frame++){Time.deltaTime=.1f;effect.Call("Update");foreach(var part in parts)Verify(part,Vector3.zero,frame==2);}
   var owned=parts.Select(o=>o.GetComponent<MeshFilter>().sharedMesh).ToArray();UnityEngine.Object.Destroy(effect);Check(owned.All(m=>m.Destroyed),"all subdivided mesh resources released with effect");
  }
  Reset();Console.WriteLine("Creation maxima in tested cases: "+maxTriangles+" anchored triangles summed across 3 parts; "+maxSegments+" real traversal segment calls; "+maxProbes+" solid probes per Impact (includes ornaments, excludes test oracle)");return "PASS: "+checks+" anchored real LOS checks; "+faces+" faces, "+vertices+" animated vertices (managed, not Unity rendering)";
 }
}
