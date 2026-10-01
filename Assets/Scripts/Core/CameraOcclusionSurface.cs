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
        public static int LastOccluders {get;private set;}
        public static void Mark(GameObject root)
        {if(root!=null&&root.GetComponent<CameraOcclusionSurface>()==null)root.AddComponent<CameraOcclusionSurface>();}
        private void OnEnable()
        {visual=GetComponent<Renderer>();if(visual!=null&&surfaces.Count<CameraVisibilityRules.MaximumSurfaces)surfaces.Add(this);}
        public static float Nearest(Vector3 point)
        {
            float nearest=float.PositiveInfinity;
            foreach(var surface in surfaces)
                if(surface!=null&&surface.visual!=null&&surface.visual.enabled)
                {Vector3 closest=surface.visual.bounds.ClosestPoint(point);nearest=Mathf.Min(nearest,Vector3.Distance(point,closest));}
            return nearest;
        }
        public static void Advance(Vector3 camera,Vector3 hero,float deltaTime)
        {
            LastOccluders=0;Vector3 direction=hero-camera;float distance=direction.magnitude;
            Ray ray=new Ray(camera,direction.normalized);
            foreach(var surface in surfaces)
            {
                if(surface==null||surface.visual==null)continue;
                Bounds bounds=surface.visual.bounds;bounds.Expand(.7f);float enter;
                bool hit=surface.visual.enabled&&distance>.01f&&bounds.IntersectRay(ray,out enter)&&enter<distance-.25f;
                if(hit)LastOccluders++;
                surface.SetFade(hit,deltaTime);
            }
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
        public static void RestoreAll(){foreach(var surface in surfaces)if(surface!=null)surface.Restore();LastOccluders=0;}
        private void OnDisable(){surfaces.Remove(this);Restore();}
        private void OnDestroy(){surfaces.Remove(this);Restore();}
    }
}
