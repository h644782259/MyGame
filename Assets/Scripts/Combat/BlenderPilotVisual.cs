using System;
using UnityEngine;

namespace Emberfall
{
    // Development-only opt-in, reset on every player/domain startup. Never stored in a save.
    public static class BlenderPilotArt
    {
        public static bool Enabled;
        internal static void ApplyMaterial(GameObject root)
        {
            Material material=Resources.Load<Material>("BlenderPilot/Pilot_Atlas_Standard");
            if(material==null)return;
            foreach(Renderer renderer in root.GetComponentsInChildren<Renderer>(true))renderer.sharedMaterial=material;
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
            ApplyMaterial(instance);
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
            BlenderPilotArt.ApplyMaterial(root);
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
            view.Ready = view.idle != null && view.move != null && view.basic != null && view.hit != null && view.skill != null;
            foreach (Transform socket in view.sockets) view.Ready &= socket != null;
            if (!view.Ready) { root.SetActive(false); Destroy(root); return null; }
            return view;
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
