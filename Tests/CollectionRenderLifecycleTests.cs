// Compile alone with CollectionModelPreview.cs and CollectionPreviewState.cs.
// Runs production preview lifecycle against controlled native-resource stand-ins;
// no GPU, Unity frame timing, pixel correctness or real CombatModel execution.
using System;
using System.Collections.Generic;
using Emberfall;
using UnityEngine;
public static class CollectionRenderLifecycleTests
{
    static int checks;
    static void Check(bool ok,string why){checks++;if(!ok)throw new Exception(why);}
    static RenderTexture Draw(CollectionModelPreview preview)
    {return preview.Render(HeroClass.Arcanist,null,null,null,null,null) as RenderTexture;}
    public static string Run()
    {
        checks=0;Time.frameCount=10;Event.current=new Event{type=EventType.Repaint};
        Camera.Renders=CombatModel.Builds=0;RenderTexture.FailCreate=false;
        var preview=new CollectionModelPreview();var first=Draw(preview);
        Check(first!=null&&first.Populated&&Camera.Renders==1&&CombatModel.Builds==1,"initial repaint builds and populates one texture");
        Draw(preview);Time.frameCount++;Draw(preview);
        Check(Camera.Renders==1,"unchanged texture remains cached across repaint calls/frames");
        preview.Rotate(45);Draw(preview);
        Check(Camera.Renders==2&&CombatModel.Builds==1,"yaw rerenders without rebuilding");
        first.Release();var recovered=Draw(preview);
        Check(recovered!=null&&recovered.Populated&&Camera.Renders==3,"RT loss after a render in the same frame must populate the recreated texture before returning it");
        Draw(preview);Check(Camera.Renders==3,"same-frame recovered texture is reused");
        first.Release();Event.current.type=EventType.Layout;Draw(preview);
        Check(Camera.Renders==3,"layout after texture loss does not render");
        Event.current.type=EventType.Repaint;Draw(preview);
        Check(first.Populated&&Camera.Renders==4,"layout recovery cannot consume the pending repaint");
        first.Release();RenderTexture.FailCreate=true;Time.frameCount++;
        Check(Draw(preview)==null&&Camera.Renders==4,"failed native allocation never exposes an uncreated texture");
        int allocations=RenderTexture.Instances;
        Event.current.type=EventType.Layout;Check(Draw(preview)==null,"failed allocation stays hidden during layout");
        Event.current.type=EventType.Repaint;Check(Draw(preview)==null&&RenderTexture.Instances==allocations,"retries retain the existing managed RT instead of allocating objects on every GUI event");
        RenderTexture.FailCreate=false;Draw(preview);
        Check(first.Populated&&Camera.Renders==5,"allocation retry paints once when texture becomes available");
        preview.Invalidate();Time.frameCount++;Draw(preview);
        Check(Camera.Renders==6&&CombatModel.Builds==1,"resume invalidation repaints the existing model");
        preview.Dispose();Check(!first.IsCreated()&&first.Destroyed,"dispose releases and destroys owned texture");
        preview.Dispose();var next=Draw(preview);
        Check(next!=first&&next.Populated&&CombatModel.Builds==2,"repeated dispose is safe and later render recreates resources");
        preview.Dispose();
        return "PASS: "+checks+" production preview lifecycle checks (managed resource fixture, not Unity rendering)";
    }
}
namespace Emberfall
{
    public enum HeroClass{Arcanist}public enum Rarity{Common}public enum EquipmentMechanic{None}public enum FashionSlot{Wings,Weapon}
    public class ItemData{public string id;public int level,upgradeLevel,mechanicVariant;public Rarity rarity;public EquipmentMechanic mechanic;}
    public class FashionData{public string id;public FashionSlot slot;public Rarity rarity;}
    public class CombatModel:MonoBehaviour
    {
        public static int Builds;
        public static CombatModel Hero(Transform parent,HeroClass hero){Builds++;var go=new GameObject();go.transform.SetParent(parent,false);go.AddComponent<Renderer>();return go.AddComponent<CombatModel>();}
        public void ApplyEquipment(ItemData weapon,ItemData armor,ItemData relic){}
        public void ApplyFashion(FashionData wings,FashionData weapon){}
        public void Animate(float a,float b,bool c){}
    }
}
namespace UnityEngine
{
    public class Object
    {
        public string name;public HideFlags hideFlags;public bool Destroyed;
        public static void Destroy(Object value){if(value!=null)value.Destroyed=true;}
    }
    public class Component:Object{public GameObject gameObject;public Transform transform=>gameObject.transform;}
    public class MonoBehaviour:Component{public bool enabled=true;}
    public class GameObject:Object
    {
        readonly List<Component> components=new List<Component>();public Transform transform;public int layer;
        public GameObject(string name=""){this.name=name;transform=new Transform{gameObject=this};components.Add(transform);}
        public T AddComponent<T>()where T:Component,new(){var c=new T{gameObject=this};components.Add(c);return c;}
        public T[] GetComponentsInChildren<T>(bool all)where T:class
        {var result=new List<T>();foreach(var c in components)if(c is T t)result.Add(t);foreach(var child in transform.children)result.AddRange(child.gameObject.GetComponentsInChildren<T>(all));return result.ToArray();}
        public void SetActive(bool value){}
    }
    public class Transform:Component
    {
        public List<Transform> children=new List<Transform>();public Vector3 position,localPosition;public Quaternion localRotation;
        public void SetParent(Transform parent,bool world){parent.children.Add(this);position=parent.position;}
        public void LookAt(Vector3 point){}
    }
    public class Texture:Object{}
    public class RenderTexture:Texture
    {
        bool created;public int width,height;public bool Populated;public static bool FailCreate;public static int Instances;
        public RenderTexture(int w,int h,int depth){width=w;height=h;Instances++;}
        public bool IsCreated()=>created;
        public bool Create(){if(created)return true;Populated=false;return created=!FailCreate;}
        public void Release(){created=false;Populated=false;}
    }
    public class Camera:MonoBehaviour
    {
        public static int Renders;public bool orthographic,allowHDR,allowMSAA;public float aspect,orthographicSize,nearClipPlane,farClipPlane;
        public CameraClearFlags clearFlags;public Color backgroundColor;public int cullingMask;public RenderTexture targetTexture;
        public void Render(){if(!targetTexture.IsCreated())throw new Exception("render to uncreated texture");Renders++;targetTexture.Populated=true;}
    }
    public class Light:Component{public LightType type;public float intensity;public int cullingMask;public LightShadows shadows;}
    public class Collider:Component{public bool enabled;}
    public class Renderer:Component{public Rendering.ShadowCastingMode shadowCastingMode;public Bounds bounds=new Bounds(Vector3.zero,Vector3.one);}
    public enum HideFlags{HideAndDontSave}public enum CameraClearFlags{SolidColor}public enum LightType{Directional}public enum LightShadows{None}
    public static class Random{public static int state;}
    public enum EventType{Layout,Repaint}public class Event{public static Event current;public EventType type;}
    public static class Time{public static int frameCount;}
    public struct Quaternion{public static Quaternion Euler(float x,float y,float z)=>new Quaternion();}
    public struct Color{public Color(float r,float g,float b){}}
    public struct Vector3
    {
        public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}
        public static Vector3 zero=>new Vector3();public static Vector3 one=>new Vector3(1,1,1);public static Vector3 up=>new Vector3(0,1,0);public static Vector3 forward=>new Vector3(0,0,1);
        public static Vector3 operator+(Vector3 a,Vector3 b)=>new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);
        public static Vector3 operator*(Vector3 a,float b)=>new Vector3(a.x*b,a.y*b,a.z*b);
    }
    public struct Bounds{public Vector3 center,extents;public Bounds(Vector3 c,Vector3 size){center=c;extents=size*.5f;}public void Encapsulate(Bounds bounds){}}
    public static class Mathf
    {public static float Abs(float v)=>Math.Abs(v);public static float Pow(float a,float b)=>(float)Math.Pow(a,b);public static float Sqrt(float v)=>(float)Math.Sqrt(v);public static float Max(params float[] args){float m=args[0];foreach(float v in args)m=Math.Max(m,v);return m;}}
}
namespace UnityEngine.Rendering{public enum ShadowCastingMode{Off}}
