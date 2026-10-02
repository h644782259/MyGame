// Compile with CollectionModelPreview.cs, CollectionPreviewState.cs and CollectionPreviewComposition.cs.
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
    static int Live<T>()where T:UnityEngine.Object {int n=0;foreach(var item in UnityEngine.Object.Registry)if(item is T&&!item.Destroyed)n++;return n;}
    public static string Run()
    {
        checks=0;Time.unscaledDeltaTime=0;Time.frameCount=10;Event.current=new Event{type=EventType.Repaint};
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
        Check(Live<RenderTexture>()==0&&Live<Material>()==0&&Live<Texture2D>()==0&&Live<Light>()==0,"all initial preview resources disposed");
        var world=new GameObject("World light").AddComponent<Light>();world.cullingMask=-1;
        var originalAmbient=new Color(.1f,.2f,.3f);RenderSettings.ambientLight=originalAmbient;RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Skybox;RenderSettings.fog=true;
        SystemInfo.SupportedSamples=1;Time.unscaledDeltaTime=1f/60;Camera.Renders=CombatModel.Builds=0;
        preview=new CollectionModelPreview();preview.SetViewport(500,800,true);allocations=RenderTexture.Instances;
        RenderTexture animated=null;
        for(int frame=100;frame<700;frame++)
        {
            Time.frameCount=frame;Event.current.type=EventType.Layout;int layoutAllocations=RenderTexture.Instances;Draw(preview);
            Check(RenderTexture.Instances==layoutAllocations,"layout never creates native surfaces");
            Event.current.type=EventType.Repaint;animated=Draw(preview);int rendered=Camera.Renders;Draw(preview);
            Check(Camera.Renders==rendered,"repeated repaint advances neither motion nor render count");
        }
        Check(Camera.Renders>=199&&Camera.Renders<=201&&CombatModel.Builds==1&&RenderTexture.Instances==allocations+1,"ten seconds of motion remain20Hz with stable one-model/one-RT cache");
        Check(animated.width==320&&animated.height==512&&animated.samples==1,"physical viewport cap and unsupported MSAA fallback use production allocation");
        Check(Live<Material>()==2&&Live<Texture2D>()==1&&Live<Light>()==4,"motion does not accumulate owned materials/textures/lights");
        preview.SetComposition(CollectionPreviewComposition.Back);Time.frameCount++;var prior=animated;Draw(preview);
        Check(CombatModel.Builds==1&&RenderTexture.Instances==allocations+1,"composition reuses mannequin and surface");
        preview.SetViewport(900,1200,false);Time.frameCount++;animated=Draw(preview);
        Check(prior.Destroyed&&!prior.IsCreated()&&animated.width==576&&animated.height==768,"resize releases old RT before bounded replacement");
        int native=RenderTexture.Instances;preview.SetViewport(200,200,true);Draw(preview);
        Check(RenderTexture.Instances==native,"multiple viewport requests allocate at most one surface per frame");
        Time.frameCount++;animated=Draw(preview);Check(animated.width==208&&animated.height==208,"deferred viewport applies next frame");
        preview.Invalidate();Time.frameCount++;
        bool observedIsolation=false;
        Camera.DuringRender=()=>observedIsolation=(world.cullingMask&(1<<31))==0&&!RenderSettings.fog&&RenderSettings.ambientMode==UnityEngine.Rendering.AmbientMode.Flat;
        Camera.ThrowOnRender=true;bool threw=false;try{Draw(preview);}catch(Exception){threw=true;}
        Check(observedIsolation,"external light/fog isolated only during render");
        Check(threw&&world.cullingMask==-1&&RenderSettings.fog&&RenderSettings.ambientMode==UnityEngine.Rendering.AmbientMode.Skybox&&RenderSettings.ambientLight.r==originalAmbient.r,"render failure restores masks and world ambient/fog in finally");
        Camera.ThrowOnRender=false;Camera.DuringRender=null;Draw(preview);Check(animated.Populated,"failed render remains retryable");
        int samples=CombatModel.Samples;int builds=CombatModel.Builds;int surfaces=RenderTexture.Instances;
        preview.Play(CollectionPreviewAction.Cast);float framing=0;Time.unscaledDeltaTime=.05f;
        for(int frame=1000;frame<1030;frame++)
        {
            Time.frameCount=frame;Draw(preview);var lens=UnityEngine.Object.FindObjectsOfType<Camera>()[0];if(frame==1000)framing=lens.orthographicSize;
            Check(lens.orthographicSize==framing,"camera envelope remains stable throughout one-shot cast");
            int calls=CombatModel.Samples;Draw(preview);Check(CombatModel.Samples==calls,"duplicate GUI repaint cannot resample presentation time");
        }
        Check(preview.PreviewAction==CollectionPreviewAction.Idle,"short preview action returns to idle without gameplay dispatch");
        Check(CombatModel.Samples-samples<=31&&CombatModel.Builds==builds&&RenderTexture.Instances==surfaces,"local pose motion reuses model texture and cached framing");
        preview.Play(CollectionPreviewAction.Attack);Time.frameCount++;Draw(preview);Check(CombatModel.LastAction==CollectionPreviewAction.Attack&&CombatModel.LastTime>0,"actual preview host samples selected action on its local clock");
        preview.Dispose();Check(Live<Material>()==0&&Live<Texture2D>()==0&&Live<RenderTexture>()==0&&Live<Light>()==1,"dispose after animation/resize/render exception returns all owned resource counts to zero");
        for(int i=0;i<20;i++)
        {
            Time.frameCount++;Draw(preview);preview.Dispose();
            Check(Live<Material>()==0&&Live<Texture2D>()==0&&Live<RenderTexture>()==0&&Live<Light>()==1,"repeated reopen/close never retains native stand-ins");
        }
        // Fail both the first and second contact material allocation, including consecutive retries.
        // The second failure occurs after the first material/quad are already owned by the stage.
        for(int materialIndex=0;materialIndex<2;materialIndex++)
        {
            for(int attempt=0;attempt<3;attempt++)
            {
                Time.frameCount++;Material.FailAfterSuccessfulCreates=materialIndex;bool stageFailed=false;
                try{Draw(preview);}catch(InvalidOperationException error){stageFailed=ReferenceEquals(error,Material.AllocationFailure);}
                Check(stageFailed,"stage creation preserves the original allocation exception");
                Check(Live<Material>()==0&&Live<Texture2D>()==0&&Live<RenderTexture>()==0&&Live<Light>()==1,
                    "failed stage creation releases partial lights, materials and contact texture immediately");
            }
            Time.frameCount++;var afterStageFailure=Draw(preview);
            Check(afterStageFailure!=null&&afterStageFailure.Populated,
                "next repaint rebuilds and renders after consecutive stage allocation failures");
            preview.Dispose();preview.Dispose();
            Check(Live<Material>()==0&&Live<Texture2D>()==0&&Live<RenderTexture>()==0&&Live<Light>()==1,
                "recovered stage has normal idempotent disposal ownership");
        }
        UnityEngine.Object.Destroy(world.gameObject);Time.unscaledDeltaTime=0;SystemInfo.SupportedSamples=4;
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
        bool configured;public static int Samples;public static float LastTime,LastProgress,PoseExtent;public static CollectionPreviewAction LastAction;
        public void ConfigurePreview(){configured=true;}
        public void SamplePreview(float time,CollectionPreviewAction action,float progress){if(!configured)throw new Exception("preview must configure isolation before sampling");Samples++;LastTime=time;LastAction=action;LastProgress=progress;PoseExtent=action==CollectionPreviewAction.Idle?.01f*(float)Math.Sin(time):.5f*(float)Math.Sin(progress*Math.PI);}
        public void Animate(float a,float b,bool c){}
    }
}
namespace UnityEngine
{
    public class Object
    {
        public static readonly List<Object> Registry=new List<Object>();
        public string name;public HideFlags hideFlags;public bool Destroyed;
        public Object(){Registry.Add(this);}
        public static void Destroy(Object value)
        {
            if(value==null||value.Destroyed)return;value.Destroyed=true;
            if(value is GameObject go){foreach(var child in go.transform.children.ToArray())Destroy(child.gameObject);foreach(var c in go.Components)Destroy(c);}
        }
        public static T[] FindObjectsOfType<T>()where T:Object
        {var list=new List<T>();foreach(var item in Registry)if(item is T t&&!t.Destroyed&&(!(t is Component c)||c.gameObject.activeInHierarchy))list.Add(t);return list.ToArray();}
        public static T[] FindObjectsByType<T>(FindObjectsSortMode mode)where T:Object{return FindObjectsOfType<T>();}
    }
    public enum FindObjectsSortMode{None}
    public class Component:Object{public GameObject gameObject;public Transform transform=>gameObject.transform;}
    public class MonoBehaviour:Component{public bool enabled=true;}
    public class GameObject:Object
    {
        public readonly List<Component> Components=new List<Component>();public Transform transform;public int layer;bool active=true;
        public bool activeInHierarchy=>active&&!Destroyed&&(transform.parent==null||transform.parent.gameObject.activeInHierarchy);
        public GameObject(string name=""){this.name=name;transform=new Transform{gameObject=this};Components.Add(transform);}
        public T AddComponent<T>()where T:Component,new(){var c=new T{gameObject=this};Components.Add(c);return c;}
        public T GetComponent<T>()where T:class{foreach(var c in Components)if(c is T t)return t;return null;}
        public T[] GetComponentsInChildren<T>(bool all)where T:class
        {var result=new List<T>();foreach(var c in Components)if(c is T t)result.Add(t);foreach(var child in transform.children)if(!child.gameObject.Destroyed)result.AddRange(child.gameObject.GetComponentsInChildren<T>(all));return result.ToArray();}
        public static GameObject CreatePrimitive(PrimitiveType type){var go=new GameObject();go.AddComponent<Renderer>();go.AddComponent<Collider>();return go;}
        public void SetActive(bool value){active=value;}
    }
    public class Transform:Component
    {
        public new string name=>gameObject.name;
        public List<Transform> children=new List<Transform>();public Transform parent;public Vector3 localPosition,localScale=Vector3.one;public Quaternion localRotation;
        public Vector3 position{get{return parent==null?localPosition:parent.position+localPosition;}set{localPosition=parent==null?value:value-parent.position;}}
        public void SetParent(Transform p,bool world){parent=p;p.children.Add(this);}
        public bool IsChildOf(Transform root){for(var p=parent;p!=null;p=p.parent)if(p==root)return true;return false;}
        public void LookAt(Vector3 point){}
    }
    public class Texture:Object{}
    public class Texture2D:Texture
    {
        public TextureWrapMode wrapMode;public Texture2D(int w,int h,TextureFormat f,bool m){}
        public void SetPixels(Color[] c){}public void Apply(bool m,bool unreadable){}
    }
    public class Shader:Object{public static Shader Find(string name){return new Shader();}}
    public class Material:Object{public Color color;public Texture mainTexture;public static int FailAfterSuccessfulCreates=-1;
        public static readonly InvalidOperationException AllocationFailure=new InvalidOperationException("simulated material allocation failure");
        public Material(Shader s){if(FailAfterSuccessfulCreates==0){FailAfterSuccessfulCreates=-1;Destroyed=true;throw AllocationFailure;}if(FailAfterSuccessfulCreates>0)FailAfterSuccessfulCreates--;}}
    public struct RenderTextureDescriptor
    {public int width,height,msaaSamples;public RenderTextureDescriptor(int w,int h,RenderTextureFormat f,int d){width=w;height=h;msaaSamples=1;}}
    public static class SystemInfo{public static int SupportedSamples=4;public static int GetRenderTextureSupportedMSAASampleCount(RenderTextureDescriptor d)=>Math.Min(d.msaaSamples,SupportedSamples);}
    public class RenderTexture:Texture
    {
        bool created;public int width,height,samples;public bool Populated;public static bool FailCreate;public static int Instances;
        public RenderTexture(RenderTextureDescriptor d){width=d.width;height=d.height;samples=d.msaaSamples;Instances++;}
        public bool IsCreated()=>created;
        public bool Create(){if(created)return true;Populated=false;return created=!FailCreate;}
        public void Release(){created=false;Populated=false;}
    }
    public class Camera:MonoBehaviour
    {
        public static int Renders;public static bool ThrowOnRender;public static Action DuringRender;
        public bool orthographic,allowHDR,allowMSAA;public float aspect,orthographicSize,nearClipPlane,farClipPlane;
        public CameraClearFlags clearFlags;public Color backgroundColor;public int cullingMask;public RenderTexture targetTexture;
        public void Render(){if(!targetTexture.IsCreated())throw new Exception("render to uncreated texture");DuringRender?.Invoke();if(ThrowOnRender)throw new Exception("simulated native render failure");Renders++;targetTexture.Populated=true;}
    }
    public class Light:Component{public LightType type;public float intensity;public Color color;public int cullingMask;public LightShadows shadows;}
    public class Collider:Component{public bool enabled;}
    public class Renderer:Component{public bool enabled=true,receiveShadows;public Material sharedMaterial;public Rendering.ShadowCastingMode shadowCastingMode;public Bounds bounds=>new Bounds(transform.position+Vector3.up,Vector3.one*(1+CombatModel.PoseExtent));}
    public class LineRenderer:Renderer{public bool useWorldSpace;public int positionCount;public float startWidth,endWidth;public void SetPosition(int i,Vector3 v){}}
    public static class RenderSettings{public static Rendering.AmbientMode ambientMode;public static Color ambientLight;public static bool fog;}
    public enum HideFlags{HideAndDontSave}public enum CameraClearFlags{SolidColor}public enum LightType{Directional}public enum LightShadows{None}
    public enum TextureFormat{RGBA32}public enum TextureWrapMode{Clamp}public enum PrimitiveType{Quad}public enum RenderTextureFormat{ARGB32}
    public static class Random{public static int state;}
    public enum EventType{Layout,Repaint}public class Event{public static Event current;public EventType type;}
    public static class Time{public static int frameCount;public static float unscaledDeltaTime;}
    public struct Quaternion{public static Quaternion Euler(float x,float y,float z)=>new Quaternion();public static Quaternion Euler(Vector3 v)=>new Quaternion();}
    public struct Color{public float r,g,b,a;public Color(float r,float g,float b,float a=1){this.r=r;this.g=g;this.b=b;this.a=a;}}
    public struct Vector2{public float x,y;public Vector2(float x,float y){this.x=x;this.y=y;}public float magnitude=>(float)Math.Sqrt(x*x+y*y);}
    public struct Vector3
    {
        public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}
        public static Vector3 zero=>new Vector3();public static Vector3 one=>new Vector3(1,1,1);public static Vector3 up=>new Vector3(0,1,0);public static Vector3 forward=>new Vector3(0,0,1);
        public static Vector3 operator+(Vector3 a,Vector3 b)=>new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);
        public static Vector3 operator-(Vector3 a,Vector3 b)=>new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);
        public static Vector3 operator*(Vector3 a,float b)=>new Vector3(a.x*b,a.y*b,a.z*b);
    }
    public struct Bounds{public Vector3 center,extents;public Bounds(Vector3 c,Vector3 size){center=c;extents=size*.5f;}public void Encapsulate(Bounds bounds){}}
    public static class Mathf
    {public const float PI=(float)Math.PI;public static float Sin(float a)=>(float)Math.Sin(a);public static float Cos(float a)=>(float)Math.Cos(a);public static float Clamp01(float a)=>Math.Max(0,Math.Min(1,a));public static int Clamp(int a,int l,int h)=>Math.Max(l,Math.Min(h,a));public static float Abs(float v)=>Math.Abs(v);public static float Pow(float a,float b)=>(float)Math.Pow(a,b);public static float Sqrt(float v)=>(float)Math.Sqrt(v);public static float Max(params float[] args){float m=args[0];foreach(float v in args)m=Math.Max(m,v);return m;}}
}
namespace UnityEngine.Rendering{public enum ShadowCastingMode{Off}public enum AmbientMode{Flat,Skybox}}
