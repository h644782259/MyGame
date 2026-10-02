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
        private Renderer[] hierarchy;
        private Material[] hierarchyOriginals;
        private readonly Dictionary<Material,Material> hierarchyFades=new Dictionary<Material,Material>();

        private Material original,fade;
        private float alpha=1;
        private BuildingOcclusionGroup group;
        private bool requested,heroRequested,targetRequested;
        private static readonly HashSet<BuildingOcclusionGroup> hitGroups=new HashSet<BuildingOcclusionGroup>(),admittedGroups=new HashSet<BuildingOcclusionGroup>();
        public static int LastOccluders {get;private set;}
        public static int LastHeroOccluders {get;private set;}
        public static int LastTargetOccluders {get;private set;}
        public static void Mark(GameObject root)
        {if(root!=null&&root.GetComponent<CameraOcclusionSurface>()==null)root.AddComponent<CameraOcclusionSurface>();}
        // One bounded registry entry for a complete authored prop. Shared bark/leaf
        // materials are cloned once per prop, with all-or-none material admission.
        public static void MarkHierarchy(GameObject root)
        {
            if(root==null)return;
            var surface=root.GetComponent<CameraOcclusionSurface>();
            if(surface==null)surface=root.AddComponent<CameraOcclusionSurface>();
            surface.Restore();surface.hierarchy=root.GetComponentsInChildren<Renderer>();
            surface.visual=surface.hierarchy.Length==0?null:surface.hierarchy[0];
            if(surface.visual!=null&&root.activeInHierarchy&&surface.enabled&&surfaces.Count<CameraVisibilityRules.MaximumSurfaces&&!surfaces.Contains(surface))surfaces.Add(surface);
        }
        internal void AssignGroup(BuildingOcclusionGroup value){group=value;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRegistry(){surfaces.Clear();hitGroups.Clear();admittedGroups.Clear();fadedCount=0;LastOccluders=0;LastHeroOccluders=0;LastTargetOccluders=0;}
        private void OnEnable()
        {visual=hierarchy!=null&&hierarchy.Length>0?hierarchy[0]:GetComponent<Renderer>();if(visual!=null&&surfaces.Count<CameraVisibilityRules.MaximumSurfaces&&!surfaces.Contains(this))surfaces.Add(this);}
        public static float Nearest(Vector3 point)
        {
            float nearest=float.PositiveInfinity;
            foreach(var surface in surfaces)
                if(surface!=null&&surface.visual!=null&&(surface.hierarchy!=null||surface.visual.enabled))
                {
                    if(surface.hierarchy!=null){foreach(var renderer in surface.hierarchy)if(renderer!=null&&renderer.enabled)nearest=Mathf.Min(nearest,Vector3.Distance(point,renderer.bounds.ClosestPoint(point)));}
                    else {Vector3 closest=surface.visual.bounds.ClosestPoint(point);nearest=Mathf.Min(nearest,Vector3.Distance(point,closest));}
                }
            return nearest;
        }
        public static void Advance(Vector3 camera,Vector3 hero,float deltaTime)
        {Advance(camera,hero,hero,hero,false,deltaTime);}
        public static void Advance(Vector3 camera,Vector3 torso,Vector3 feet,Vector3 target,bool protectTarget,float deltaTime)
        {
            LastOccluders=0;LastHeroOccluders=0;LastTargetOccluders=0;hitGroups.Clear();admittedGroups.Clear();
            foreach(var surface in surfaces)
            {
                if(surface==null||surface.visual==null)continue;
                Bounds bounds=surface.visual.bounds;bounds.Expand(.7f);
                surface.heroRequested=surface.hierarchy!=null?(surface.HierarchyProtects(camera,torso)||surface.HierarchyProtects(camera,feet)):surface.visual.enabled&&(Protects(bounds,camera,torso)||Protects(bounds,camera,feet));
                surface.targetRequested=surface.hierarchy!=null?protectTarget&&surface.HierarchyProtects(camera,target):surface.visual.enabled&&protectTarget&&Protects(bounds,camera,target);
                surface.requested=surface.heroRequested||surface.targetRequested;
                if(surface.heroRequested)LastHeroOccluders++;
                if(surface.targetRequested)LastTargetOccluders++;
                if(surface.requested)LastOccluders++;
                if(surface.requested&&surface.group!=null)hitGroups.Add(surface.group);
            }
            // Reserve whole groups first. Never show half a roof because the material cap ran out.
            int available=CameraVisibilityRules.MaximumFaded-fadedCount;
            foreach(var group in hitGroups)
            {
                int needed=0;foreach(var surface in surfaces)if(surface!=null&&surface.group==group&&surface.CanFade)needed+=surface.RequiredFadeSlots;
                if(CameraVisibilityRules.ReserveGroup(ref available,needed))admittedGroups.Add(group);
            }
            foreach(var surface in surfaces)
            {
                if(surface==null||surface.visual==null||surface.group==null)continue;
                bool hit=admittedGroups.Contains(surface.group);
                surface.SetFade(hit,deltaTime);surface.group.SetOccluded(hit||surface.fade!=null||surface.hierarchyFades.Count>0);
            }
            foreach(var surface in surfaces)
            {
                if(surface==null||surface.visual==null||surface.group!=null)continue;
                surface.SetFade(surface.requested,deltaTime);
            }
        }
        private bool HierarchyProtects(Vector3 camera,Vector3 point)
        {foreach(var renderer in hierarchy)if(renderer!=null&&renderer.enabled){Bounds bounds=renderer.bounds;bounds.Expand(.7f);if(Protects(bounds,camera,point))return true;}return false;}
        private int RequiredFadeSlots
        {
            get
            {
                if(hierarchy==null)return fade==null?1:0;
                if(hierarchyFades.Count>0)return 0;
                var materials=new HashSet<Material>();
                foreach(var renderer in hierarchy)if(renderer!=null&&renderer.sharedMaterial!=null&&renderer.sharedMaterial.HasProperty("_Color"))materials.Add(renderer.sharedMaterial);
                return materials.Count;
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
            if(hierarchy!=null){SetHierarchyFade(occluded,delta);return;}
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
        private void SetHierarchyFade(bool occluded,float delta)
        {
            if(occluded&&hierarchyFades.Count==0)
            {
                int needed=RequiredFadeSlots;
                if(needed==0||needed>CameraVisibilityRules.MaximumFaded-fadedCount)return;
                // Refuse an unsupported material rather than fading only part of a tree.
                foreach(var renderer in hierarchy)if(renderer!=null&&(renderer.sharedMaterial==null||!renderer.sharedMaterial.HasProperty("_Color")))return;
                hierarchyOriginals=new Material[hierarchy.Length];
                for(int i=0;i<hierarchy.Length;i++)
                {
                    var renderer=hierarchy[i];if(renderer==null)continue;
                    Material source=renderer.sharedMaterial;hierarchyOriginals[i]=source;
                    if(!hierarchyFades.ContainsKey(source))
                    {
                        Material owned=new Material(source){name="Camera hierarchy fade (owned)"};
                        owned.SetInt("_SrcBlend",(int)BlendMode.SrcAlpha);owned.SetInt("_DstBlend",(int)BlendMode.OneMinusSrcAlpha);
                        owned.SetInt("_ZWrite",0);if(owned.HasProperty("_Mode"))owned.SetFloat("_Mode",2);
                        owned.DisableKeyword("_ALPHATEST_ON");owned.DisableKeyword("_ALPHAPREMULTIPLY_ON");owned.EnableKeyword("_ALPHABLEND_ON");owned.renderQueue=3000;
                        hierarchyFades.Add(source,owned);
                    }
                }
                fadedCount+=hierarchyFades.Count;
                for(int i=0;i<hierarchy.Length;i++)if(hierarchy[i]!=null)hierarchy[i].sharedMaterial=hierarchyFades[hierarchyOriginals[i]];
            }
            alpha=CameraVisibilityRules.FadeStep(alpha,occluded,delta);
            foreach(var pair in hierarchyFades){Color color=pair.Key.color;color.a*=alpha;pair.Value.color=color;}
            if(!occluded&&alpha>.995f)Restore();
        }
        private void Restore()
        {
            if(hierarchyFades.Count>0)
            {
                for(int i=0;i<hierarchy.Length;i++)
                {
                    Material owned;
                    if(hierarchy[i]!=null&&hierarchyOriginals[i]!=null&&hierarchyFades.TryGetValue(hierarchyOriginals[i],out owned)&&hierarchy[i].sharedMaterial==owned)
                        hierarchy[i].sharedMaterial=hierarchyOriginals[i];
                }
                fadedCount=Mathf.Max(0,fadedCount-hierarchyFades.Count);
                foreach(var owned in hierarchyFades.Values)Destroy(owned);
                hierarchyFades.Clear();hierarchyOriginals=null;
            }
            if(fade!=null)
            {if(visual!=null&&visual.sharedMaterial==fade)visual.sharedMaterial=original;Destroy(fade);fade=null;fadedCount=Mathf.Max(0,fadedCount-1);}
            alpha=1;
        }
        public static void RestoreAll(){foreach(var surface in surfaces)if(surface!=null){surface.Restore();if(surface.group!=null)surface.group.SetOccluded(false);}LastOccluders=0;LastHeroOccluders=0;LastTargetOccluders=0;}
        private void OnDisable(){surfaces.Remove(this);Restore();}
        private void OnDestroy(){surfaces.Remove(this);Restore();}
    }
}
