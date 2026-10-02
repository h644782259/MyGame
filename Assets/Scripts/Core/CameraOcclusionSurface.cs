using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace Emberfall
{
    // Explicit world-only registration. Telegraphs, effects and actor materials
    // are never collected or modified by this system.
    public sealed class CameraOcclusionSurface : MonoBehaviour
    {
        private static readonly List<CameraOcclusionSurface> surfaces=new List<CameraOcclusionSurface>(256);
        private static int fadedCount;
        private Renderer visual;
        private Material original,fade;
        private float alpha=1;
        private BuildingOcclusionGroup group;
        private bool requested;
        private static readonly HashSet<BuildingOcclusionGroup> hitGroups=new HashSet<BuildingOcclusionGroup>(),admittedGroups=new HashSet<BuildingOcclusionGroup>();
        public static int LastOccluders {get;private set;}
        public static void Mark(GameObject root)
        {if(root!=null&&root.GetComponent<CameraOcclusionSurface>()==null)root.AddComponent<CameraOcclusionSurface>();}
        internal void AssignGroup(BuildingOcclusionGroup value){group=value;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRegistry(){surfaces.Clear();hitGroups.Clear();admittedGroups.Clear();fadedCount=0;LastOccluders=0;}
        private void OnEnable()
        {visual=GetComponent<Renderer>();if(visual!=null&&surfaces.Count<CameraVisibilityRules.MaximumSurfaces&&!surfaces.Contains(this))surfaces.Add(this);}
        public static float Nearest(Vector3 point)
        {
            float nearest=float.PositiveInfinity;
            foreach(var surface in surfaces)
                if(surface!=null&&surface.visual!=null&&surface.visual.enabled)
                {Vector3 closest=surface.visual.bounds.ClosestPoint(point);nearest=Mathf.Min(nearest,Vector3.Distance(point,closest));}
            return nearest;
        }
        public static void Advance(Vector3 camera,Vector3 hero,float deltaTime)
        {Advance(camera,hero,hero,hero,false,deltaTime);}
        public static void Advance(Vector3 camera,Vector3 torso,Vector3 feet,Vector3 target,bool protectTarget,float deltaTime)
        {
            LastOccluders=0;hitGroups.Clear();admittedGroups.Clear();
            foreach(var surface in surfaces)
            {
                if(surface==null||surface.visual==null)continue;
                Bounds bounds=surface.visual.bounds;bounds.Expand(.7f);
                surface.requested=surface.visual.enabled&&(Protects(bounds,camera,torso)||Protects(bounds,camera,feet)||(protectTarget&&Protects(bounds,camera,target)));
                if(surface.requested)LastOccluders++;
                if(surface.requested&&surface.group!=null)hitGroups.Add(surface.group);
            }
            // Reserve whole groups first. Never show half a roof because the material cap ran out.
            int available=CameraVisibilityRules.MaximumFaded-fadedCount;
            foreach(var group in hitGroups)
            {
                int needed=0;foreach(var surface in surfaces)if(surface!=null&&surface.group==group&&surface.fade==null&&surface.CanFade)needed++;
                if(CameraVisibilityRules.ReserveGroup(ref available,needed))admittedGroups.Add(group);
            }
            foreach(var surface in surfaces)
            {
                if(surface==null||surface.visual==null||surface.group==null)continue;
                bool hit=admittedGroups.Contains(surface.group);
                surface.SetFade(hit,deltaTime);surface.group.SetOccluded(hit||surface.fade!=null);
            }
            foreach(var surface in surfaces)
            {
                if(surface==null||surface.visual==null||surface.group!=null)continue;
                surface.SetFade(surface.requested,deltaTime);
            }
        }
        private bool CanFade {get{return visual!=null&&visual.sharedMaterial!=null&&visual.sharedMaterial.HasProperty("_Color");}}
        private static bool Protects(Bounds bounds,Vector3 camera,Vector3 point)
        {
            Vector3 min=bounds.min,max=bounds.max;
            return CameraVisibilityRules.ProtectsBox(camera.x,camera.y,camera.z,point.x,point.y,point.z,
                min.x,min.y,min.z,max.x,max.y,max.z);
        }
        private void SetFade(bool occluded,float delta)
        {
            if(occluded&&fade==null)
            {
                if(fadedCount>=CameraVisibilityRules.MaximumFaded)return;
                original=visual.sharedMaterial;if(original==null||!original.HasProperty("_Color"))return;
                fade=new Material(original){name="Camera fade (owned)"};
                fade.SetInt("_SrcBlend",(int)BlendMode.SrcAlpha);fade.SetInt("_DstBlend",(int)BlendMode.OneMinusSrcAlpha);
                fade.SetInt("_ZWrite",0);if(fade.HasProperty("_Mode"))fade.SetFloat("_Mode",2);
                fade.DisableKeyword("_ALPHATEST_ON");fade.DisableKeyword("_ALPHAPREMULTIPLY_ON");fade.EnableKeyword("_ALPHABLEND_ON");fade.renderQueue=3000;
                visual.sharedMaterial=fade;fadedCount++;
            }
            alpha=CameraVisibilityRules.FadeStep(alpha,occluded,delta);
            if(fade!=null){Color color=original.color;color.a*=alpha;fade.color=color;}
            if(!occluded&&alpha>.995f)Restore();
        }
        private void Restore()
        {
            if(fade!=null)
            {if(visual!=null&&visual.sharedMaterial==fade)visual.sharedMaterial=original;Destroy(fade);fade=null;fadedCount=Mathf.Max(0,fadedCount-1);}
            alpha=1;
        }
        public static void RestoreAll(){foreach(var surface in surfaces)if(surface!=null){surface.Restore();if(surface.group!=null)surface.group.SetOccluded(false);}LastOccluders=0;}
        private void OnDisable(){surfaces.Remove(this);Restore();}
        private void OnDestroy(){surfaces.Remove(this);Restore();}
    }
}
