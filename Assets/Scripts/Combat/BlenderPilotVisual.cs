using System;
using UnityEngine;

namespace Emberfall
{
    // Development-only opt-in, reset on every player/domain startup. Never stored in a save.
    public static class BlenderPilotArt
    {
        public static bool Enabled;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetForPlayerStartup() { Enabled=false; }
        internal static bool ApplyMaterial(GameObject root)
        {
            Material material=Resources.Load<Material>("BlenderPilot/Pilot_Atlas_Standard");
            if(material==null||material.shader==null||!material.shader.isSupported||material.shader.name!="Standard"||
                !material.HasProperty("_MainTex")||!material.HasProperty("_MetallicGlossMap")||!material.IsKeywordEnabled("_METALLICGLOSSMAP"))return false;
            var albedo=material.GetTexture("_MainTex") as Texture2D;
            var metal=material.GetTexture("_MetallicGlossMap") as Texture2D;
            if(albedo==null||metal==null||albedo.width<2||albedo.height<2||metal.width<2||metal.height<2)return false;
            Renderer[] renderers=root.GetComponentsInChildren<Renderer>(true);bool hasGeometry=false;
            foreach(Renderer renderer in renderers)
            {
                var skinned=renderer as SkinnedMeshRenderer;var filter=renderer.GetComponent<MeshFilter>();
                if(renderer.enabled&&renderer.gameObject.activeInHierarchy&&
                    ((skinned!=null&&skinned.sharedMesh!=null)||(filter!=null&&filter.sharedMesh!=null)))hasGeometry=true;
            }
            if(!hasGeometry)return false;
            foreach(Renderer renderer in renderers)renderer.sharedMaterial=material;
            return true;
        }
        public static GameObject CreateProp(string name, Transform parent, Vector3 position)
        {
            if (!Enabled) return null;
            GameObject source = Resources.Load<GameObject>("BlenderPilot/" + name);
            if (source == null) return null;
            GameObject instance = UnityEngine.Object.Instantiate(source, parent, false);
            instance.transform.localPosition = position;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
            if(!ApplyMaterial(instance)){instance.SetActive(false);UnityEngine.Object.Destroy(instance);return null;}
            return instance;
        }
    }

    // Sole writer of the imported skeleton. Explicit SampleAnimation means no automatic
    // Animator/Animation clock, root motion, animation events, or gameplay callbacks.
    public sealed class BlenderPilotVisual : MonoBehaviour
    {
        private AnimationClip idle, move, basic, hit, skill;
        private Transform[] sockets;
        public bool Ready { get; private set; }
        public static BlenderPilotVisual Create(Transform parent)
        {
            if (!BlenderPilotArt.Enabled) return null;
            GameObject source = Resources.Load<GameObject>("BlenderPilot/Vanguard");
            if (source == null) return null;
            GameObject root = Instantiate(source, parent, false);
            root.name = "Blender pilot visual (sampled skeleton)";
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;
            // Imported components can never compete with the manual sampler.
            foreach (Animator animator in root.GetComponentsInChildren<Animator>(true))
            { animator.applyRootMotion = false; animator.enabled = false; }
            foreach (Animation animation in root.GetComponentsInChildren<Animation>(true))
            { animation.playAutomatically = false; animation.enabled = false; }
            if(!BlenderPilotArt.ApplyMaterial(root)){root.SetActive(false);Destroy(root);return null;}
            var view = root.AddComponent<BlenderPilotVisual>();
            foreach (AnimationClip clip in Resources.LoadAll<AnimationClip>("BlenderPilot/Vanguard"))
            {
                if (clip.name.EndsWith("Pilot_Idle", StringComparison.Ordinal)) view.idle = clip;
                else if (clip.name.EndsWith("Pilot_Move", StringComparison.Ordinal)) view.move = clip;
                else if (clip.name.EndsWith("Pilot_Basic", StringComparison.Ordinal)) view.basic = clip;
                else if (clip.name.EndsWith("Pilot_Hit", StringComparison.Ordinal)) view.hit = clip;
                else if (clip.name.EndsWith("Pilot_Skill", StringComparison.Ordinal)) view.skill = clip;
            }
            view.sockets = new Transform[5];
            string[] names = { "Anchor_Pommel", "Anchor_Grip", "Anchor_Guard", "Anchor_BladeRoot", "Anchor_Tip" };
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                for (int i = 0; i < names.Length; i++) if (child.name == names[i]) view.sockets[i] = child;
            view.Ready = ValidClip(view.idle) && ValidClip(view.move) && ValidClip(view.basic) && ValidClip(view.hit) && ValidClip(view.skill);
            if(view.Ready)view.Ready=BindingsMove(root,new[]{view.idle,view.move,view.basic,view.hit,view.skill});
            foreach (Transform socket in view.sockets) view.Ready &= socket != null;
            if (!view.Ready) { root.SetActive(false); Destroy(root); return null; }
            root.SetActive(false); // first visible frame must already have a sampled pose
            return view;
        }
        private static bool ValidClip(AnimationClip clip)
        {return clip!=null&&!clip.empty&&clip.length>0&&!float.IsInfinity(clip.length)&&!float.IsNaN(clip.length);}
        private static bool BindingsMove(GameObject root,AnimationClip[] clips)
        {
            // Runtime smoke check for curves targeting this imported hierarchy. Exact
            // Unity import/deformation acceptance remains an Editor/device check.
            Transform[] parts=root.GetComponentsInChildren<Transform>(true);
            var positions=new Vector3[parts.Length];var rotations=new Quaternion[parts.Length];var scales=new Vector3[parts.Length];
            for(int i=0;i<parts.Length;i++){positions[i]=parts[i].localPosition;rotations[i]=parts[i].localRotation;scales[i]=parts[i].localScale;}
            bool valid=true;
            try
            {
                foreach(AnimationClip clip in clips)
                {
                    bool changed=false;
                    for(int sample=0;sample<4;sample++)
                    {
                        for(int i=0;i<parts.Length;i++){parts[i].localPosition=positions[i];parts[i].localRotation=rotations[i];parts[i].localScale=scales[i];}
                        clip.SampleAnimation(root,clip.length*sample*.25f);
                        for(int i=0;i<parts.Length;i++)if(parts[i].localPosition!=positions[i]||parts[i].localRotation!=rotations[i]||parts[i].localScale!=scales[i])changed=true;
                    }
                    valid &= changed;
                }
            }
            catch(Exception){valid=false;}
            finally {for(int i=0;i<parts.Length;i++){parts[i].localPosition=positions[i];parts[i].localRotation=rotations[i];parts[i].localScale=scales[i];}}
            return valid;
        }
        public void Sample(float time, float phase, float speed, bool acting, bool isBasic, float actionProgress, float hurtAge)
        {
            BlenderPilotSample sample=BlenderPilotPosePolicy.Select(time,idle.length,phase,speed,acting,isBasic,actionProgress,hurtAge);
            AnimationClip clip=sample.Clip==BlenderPilotClip.Basic?basic:sample.Clip==BlenderPilotClip.Skill?skill:
                sample.Clip==BlenderPilotClip.Hit?hit:sample.Clip==BlenderPilotClip.Move?move:idle;
            clip.SampleAnimation(gameObject, sample.NormalizedTime * clip.length);
        }
        public bool Anchor(WeaponVisualAnchor anchor, out Vector3 position)
        {
            int index = (int)anchor - (int)WeaponVisualAnchor.SwordPommel;
            if (index >= 0 && index < sockets.Length && sockets[index] != null)
            { position = sockets[index].position; return true; }
            position = transform.position; return false;
        }
    }
}
