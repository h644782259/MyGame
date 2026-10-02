using System;using System.Reflection;using System.Linq;using System.Collections.Generic;using Emberfall;using UnityEngine;
namespace UnityEngine
{
    public class Object {public bool Destroyed;public static void Destroy(Object o){if(o!=null)o.Destroyed=true;}}
    public class MonoBehaviour:Object{public GameObject gameObject=new GameObject();public Transform transform=>gameObject.transform;}
    public class GameObject:Object{public Transform transform=new Transform();public string name;public GameObject(string n=""){name=n;}public T AddComponent<T>()where T:new(){var c=new T();if(c is MonoBehaviour mb)mb.gameObject=this;return c;}}
    public class Transform{public Vector3 position;public void SetParent(Transform parent,bool world){}}
    public class Material:Object{}
    public class Mesh:Object{public string name;public Vector3[] vertices;public Color[] colors;public int[] triangles;public void RecalculateBounds(){}}
    public class MeshFilter:MonoBehaviour{public Mesh sharedMesh;}
    public class MeshRenderer:MonoBehaviour{public Material sharedMaterial;public Rendering.ShadowCastingMode shadowCastingMode;public int sortingOrder;}
    public class LineRenderer:MonoBehaviour{public bool useWorldSpace,loop,enabled;public int positionCount,sortingOrder;public float widthMultiplier;public Material sharedMaterial;public Color startColor,endColor;public Vector3[] Points=new Vector3[128];public void SetPosition(int i,Vector3 p){Points[i]=p;}}
    public struct Color{public float r,g,b,a;public Color(float r,float g,float b,float a=1){this.r=r;this.g=g;this.b=b;this.a=a;}}
    public partial struct Vector3{public static Vector3 up=>new Vector3(0,1,0);public static Vector3 operator -(Vector3 a)=>a*-1;public static Vector3 Cross(Vector3 a,Vector3 b)=>new Vector3(a.y*b.z-a.z*b.y,a.z*b.x-a.x*b.z,a.x*b.y-a.y*b.x);}
    public static partial class Mathf{public static float Clamp01(float f)=>Clamp(f,0,1);}
}
namespace UnityEngine.Rendering{public enum ShadowCastingMode{Off}}
namespace Emberfall
{
    public static class ThreatVisualStyle{public static readonly Color Danger=new Color(1,.22f,.1f);public static Material Material()=>new Material();}
}
public static class EnemyImpactContourTests
{
    static int checks;
    static void Check(bool okay,string why){checks++;if(!okay)throw new Exception(why);}
    static T Field<T>(object o,string name)=>(T)o.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(o);
    static void Call(object o,string name)=>o.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(o,null);
    public static string Run()
    {
        checks=0;WorldTraversal.Reset(ZoneKind.Dungeon);
        var attacker=new Transform{position=new Vector3(-2,0,0)};var center=new Vector3(.4f,0,0);float radius=2;
        WorldTraversal.AddBox(Vector3.zero,new Vector2(.2f,7));
        Check(!EnemyImpactRegion.Contains(attacker.position,center,center,radius),"impact center can be behind wall while near lobe still hits");
        var warning=EnemyAttackTelegraph.Circle(center,radius,attacker);Mesh mesh=Field<Mesh>(warning,"impactMesh");
        Check(mesh.vertices.Length>0,"reachable near lobe still has warning when center is blocked");
        Check(mesh.vertices.All(v=>v.x<.001f),"enemy warning must not cover the wall-hidden safe lobe");
        var outline=EnemyImpactRegion.Outline(attacker.position,center,radius);
        foreach(var point in outline)Check(EnemyImpactRegion.Contains(attacker.position,center,point,radius),"every contour bracket stays on actual attacker-reachable damage side");
        foreach(float x in new[]{-1.4f,-.3f,.4f,1f,2f})
        {Vector3 point=new Vector3(x,0,0),offset=point-center;Check(EnemyImpactRegion.Contains(attacker.position,center,point,radius)==PlayerUpgradeRules.IsInsideArea(offset.x,offset.z,radius,WorldTraversal.HasGroundPath(attacker.position,point,.12f)),"shared hit predicate preserves exact pre-change damage result");}
        Call(warning,"LateUpdate");Check(ReferenceEquals(mesh,Field<Mesh>(warning,"impactMesh")),"unchanged warning does not rebuild geometry each frame");
        WorldTraversal.Reset(ZoneKind.Dungeon);Call(warning,"LateUpdate");var rebuilt=Field<Mesh>(warning,"impactMesh");
        Check(mesh.Destroyed&&!ReferenceEquals(mesh,rebuilt)&&rebuilt.vertices.Any(v=>v.x>1),"cover revision replaces and releases mesh with newly reachable arc");
        attacker.position=new Vector3(2,0,0);WorldTraversal.AddBox(Vector3.zero,new Vector2(.2f,7));Call(warning,"LateUpdate");
        Check(Field<Mesh>(warning,"impactMesh").vertices.All(v=>v.x>-.001f),"actual moved attacker changes safe side without moving impact center");
        var last=Field<Mesh>(warning,"impactMesh");Call(warning,"OnDestroy");Check(last.Destroyed&&Field<Material>(warning,"material").Destroyed,"warning destruction releases mesh and material");
        WorldTraversal.Reset(ZoneKind.Wilderness);WorldTraversal.SetRiver(new[]{new Vector3(-15,0,0),new Vector3(15,0,0)},2,new Rect(-1,-2,2,4));
        attacker.position=new Vector3(4,0,-2);center=new Vector3(4,0,0);
        Check(!EnemyImpactRegion.Contains(attacker.position,center,new Vector3(4,0,1.5f),3),"opposite river bank stays safe without a ground crossing");
        var bank=EnemyImpactRegion.Outline(attacker.position,center,3);Check(bank.Count>0&&bank.All(p=>p.z<-.99f),"river warning contour retains only reachable bank");
        attacker.position=new Vector3(0,0,-2);center=Vector3.zero;
        Check(EnemyImpactRegion.Contains(attacker.position,center,new Vector3(0,0,1.5f),3),"bridge mouth retains its actual reachable damage lane");
        return "PASS: "+checks+" actual enemy contour/traversal/cache/disposal checks (managed, not Unity rendering)";
    }
}
