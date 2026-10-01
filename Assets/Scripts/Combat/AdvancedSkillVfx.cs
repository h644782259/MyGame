using System.Collections.Generic;
using UnityEngine;

namespace Emberfall
{
    // A bounded family of ornate spell effects. Geometry and one shared material
    // are created once per effect; animation only changes transforms and tint.
    internal sealed class AdvancedSkillVfx : MonoBehaviour
    {
        private const int MaximumEffects = 36;
        private static int activeEffects;
        private readonly List<LineRenderer> lines = new List<LineRenderer>();
        private readonly List<Transform> rotors = new List<Transform>();
        private PlayerController owner;
        private int epoch;
        private Material material;
        private Color tint;
        private float age, duration, radius;
        private bool follow;

        private static AdvancedSkillVfx Create(PlayerController hero, Vector3 at, Color color, float lifetime)
        {
            if (activeEffects >= MaximumEffects || hero == null) return null;
            GameObject obj = new GameObject("Ornate Skill Effect");
            obj.transform.position = at;
            AdvancedSkillVfx fx = obj.AddComponent<AdvancedSkillVfx>();
            activeEffects++;
            fx.owner = hero;
            fx.epoch = hero.CombatEpoch;
            fx.material = CombatFx.NewGlow();
            fx.tint = color;
            fx.duration = Mathf.Max(.12f,lifetime);
            return fx;
        }

        public static void Rune(PlayerController hero, Vector3 at, float size, Color color, float lifetime, int detail, bool followHero = false)
        {
            AdvancedSkillVfx fx = Create(hero,at,color,lifetime);
            if (fx == null) return;
            fx.radius = size;
            fx.follow = followHero;
            int rings = detail >= 2 ? 3 : 2;
            for (int r = 0; r < rings; r++)
            {
                Transform rotor = fx.Rotor();
                float ringRadius = size * (1f - r * .23f);
                Vector3[] circle = new Vector3[64];
                for (int i = 0; i < circle.Length; i++)
                {
                    float a = i * Mathf.PI * 2 / circle.Length;
                    circle[i] = new Vector3(Mathf.Cos(a)*ringRadius,.09f+r*.04f,Mathf.Sin(a)*ringRadius);
                }
                fx.Line(circle,.055f+r*.016f,true,rotor);
                int glyphs = detail >= 2 ? 8 : 6;
                for (int i = 0; i < glyphs; i++)
                {
                    float a = i * Mathf.PI * 2 / glyphs + r*.3f;
                    Vector3 radial = new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));
                    Vector3 tangent = new Vector3(-radial.z,0,radial.x);
                    Vector3 center = radial*ringRadius+Vector3.up*.13f;
                    float glyphSize = size*.075f;
                    fx.Line(new[] { center+radial*glyphSize,center+tangent*glyphSize*.5f,center-radial*glyphSize,center-tangent*glyphSize*.5f },.05f,true,rotor);
                }
            }
            if (detail >= 2)
            {
                Transform rotor = fx.Rotor();
                for (int i = 0; i < 6; i++)
                {
                    float a = i*Mathf.PI/3;
                    Vector3 ground = new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*size*.84f;
                    float height = detail >= 3 ? 3.8f : 2.6f;
                    Vector3 top = ground+Vector3.up*height;
                    fx.Line(new[] { ground+Vector3.up*.15f,top,top+Vector3.right*.12f,top+Vector3.up*.38f,top-Vector3.right*.12f,top,ground+Vector3.up*.15f },.075f,false,rotor);
                }
            }
            if (detail >= 3)
            {
                Transform rotor = fx.Rotor();
                Vector3[] spiral = new Vector3[96];
                for (int i = 0; i < spiral.Length; i++)
                {
                    float f = i/(float)(spiral.Length-1);
                    float a = f*Mathf.PI*6;
                    spiral[i] = new Vector3(Mathf.Cos(a)*size*.64f,.25f+f*3.2f,Mathf.Sin(a)*size*.64f);
                }
                fx.Line(spiral,.045f,false,rotor);
            }
        }

        public static void Beam(PlayerController hero, Vector3 start, Vector3 end, Color color, float lifetime, float width = .18f)
        {
            AdvancedSkillVfx fx = Create(hero,start,color,lifetime);
            if (fx == null) return;
            Vector3 delta = end-start;
            Vector3 side = Vector3.Cross(delta.normalized,Vector3.up);
            if (side.sqrMagnitude < .01f) side = Vector3.right;
            Vector3[] points = new Vector3[7];
            for (int i = 0; i < points.Length; i++)
            {
                float f = i/(float)(points.Length-1);
                points[i] = delta*f + side * (i==0||i==points.Length-1 ? 0 : (i%2==0?-.2f:.2f));
            }
            fx.Line(points,width,false,fx.transform);
            fx.Line(new[] { Vector3.zero,delta },width*.24f,false,fx.transform);
        }

        public static void FallingBlade(PlayerController hero, Vector3 at, Color color, float scale = 1f)
        {
            AdvancedSkillVfx fx = Create(hero,at,color,.7f);
            if (fx == null) return;
            float h = 6f*scale;
            fx.Line(new[] { new Vector3(0,h,0),new Vector3(.18f*scale,1.1f,0),Vector3.zero,new Vector3(-.18f*scale,1.1f,0),new Vector3(0,h,0) },.16f,false,fx.transform);
            fx.Line(new[] { new Vector3(-.8f*scale,h*.72f,0),new Vector3(.8f*scale,h*.72f,0) },.2f,false,fx.transform);
            fx.Line(new[] { Vector3.up*.15f,Vector3.up*(h+2f) },.065f,false,fx.transform);
        }

        private Transform Rotor()
        {
            GameObject obj = new GameObject("Runic Ring");
            obj.transform.SetParent(transform,false);
            rotors.Add(obj.transform);
            return obj.transform;
        }

        private void Line(Vector3[] points, float width, bool closed, Transform parent)
        {
            GameObject obj = new GameObject("Spell Light");
            obj.transform.SetParent(parent,false);
            LineRenderer line = obj.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.loop = closed;
            line.positionCount = points.Length;
            for (int i = 0; i < points.Length; i++) line.SetPosition(i,points[i]);
            line.widthMultiplier = width;
            line.sharedMaterial = material;
            line.startColor = line.endColor = tint;
            lines.Add(line);
        }

        private void Update()
        {
            if (owner == null || owner.IsDead || owner.CombatEpoch != epoch) { Destroy(gameObject); return; }
            age += Time.deltaTime;
            if (age >= duration) { Destroy(gameObject); return; }
            if (follow) transform.position = owner.transform.position;
            for (int i = 0; i < rotors.Count; i++) rotors[i].Rotate(0,Time.deltaTime*(i%2==0?32f:-24f),0,Space.Self);
            float opacity = Mathf.Min(1f,age*9f)*Mathf.Min(1f,(duration-age)*4f);
            Color faded = new Color(tint.r,tint.g,tint.b,tint.a*opacity);
            for (int i = 0; i < lines.Count; i++) lines[i].startColor = lines[i].endColor = faded;
            if (radius > 0) transform.localScale = Vector3.one*(1f+Mathf.Sin(age*5f)*.013f);
        }

        private void OnDestroy()
        {
            activeEffects = Mathf.Max(0,activeEffects-1);
            if (material != null) Destroy(material);
        }
    }
}
