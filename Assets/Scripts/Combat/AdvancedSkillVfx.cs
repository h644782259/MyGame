using System.Collections.Generic;
using UnityEngine;

namespace Emberfall
{
    // Each effect has bounded geometry, two shared materials and no per-frame allocations.
    // Solid shards, shockwaves and falling weapons make the silhouette legible in motion.
    internal sealed class AdvancedSkillVfx : MonoBehaviour
    {
        private const int MaximumEffects = 36;
        private const int MaximumSolidParts = 20;
        private static int activeEffects;
        private readonly List<LineRenderer> lines = new List<LineRenderer>();
        private readonly List<Transform> rotors = new List<Transform>();
        private readonly List<SolidPart> solids = new List<SolidPart>();
        private readonly List<Mesh> meshes = new List<Mesh>();
        private MaterialPropertyBlock properties;
        private PlayerController owner;
        private int epoch;
        private Material material, solidMaterial;
        private Transform fallingWeapon;
        private float fallingHeight;
        private Color tint;
        private float age, duration, radius;
        private bool follow;

        private enum Motion { Still, Burst, Orbit, Wave, Pulse }

        private void Awake() { properties = new MaterialPropertyBlock(); }

        private sealed class SolidPart
        {
            public Transform Transform;
            public Renderer Renderer;
            public Vector3 Origin, Scale, Velocity, Spin;
            public Quaternion Rotation;
            public float Delay, Life, Phase;
            public Motion Movement;
        }

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
            fx.solidMaterial = CombatFx.NewGlow();
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
            fx.SolidRune(size, Mathf.Clamp(detail, 1, 3));
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
            float length = delta.magnitude;
            if (length > .01f)
            {
                SolidPart core = fx.Primitive("Luminous Beam Core", PrimitiveType.Cylinder, delta * .5f,
                    new Vector3(width * .65f, length * .5f, width * .65f), Motion.Pulse, lifetime);
                core.Transform.localRotation = Quaternion.FromToRotation(Vector3.up, delta);
                core.Rotation = core.Transform.localRotation;
                fx.Primitive("Beam Impact", PrimitiveType.Sphere, delta, Vector3.one * width * 3.2f, Motion.Pulse, lifetime);
                for (int i = 0; i < 6; i++)
                {
                    float a = i * Mathf.PI / 3f;
                    Vector3 direction = new Vector3(Mathf.Cos(a), .65f, Mathf.Sin(a));
                    SolidPart spark = fx.Primitive("Beam Splinter", PrimitiveType.Cube, delta,
                        new Vector3(.06f, .22f, .06f), Motion.Burst, Mathf.Min(lifetime, .55f));
                    spark.Velocity = direction * 2.1f;
                    spark.Spin = new Vector3(120f, 70f, 180f);
                }
            }
        }

        public static void FallingBlade(PlayerController hero, Vector3 at, Color color, float scale = 1f)
        {
            AdvancedSkillVfx fx = Create(hero,at,color,.86f);
            if (fx == null) return;
            fx.fallingHeight = 6f * scale;
            fx.fallingWeapon = new GameObject("Descending Celestial Sword").transform;
            fx.fallingWeapon.SetParent(fx.transform, false);
            fx.fallingWeapon.localPosition = Vector3.up * fx.fallingHeight;
            fx.fallingWeapon.localRotation = Quaternion.Euler(0, 32f, -7f);
            Mesh blade = fx.CrystalMesh();
            fx.MeshSolid("Celestial Blade", blade, new Vector3(0, 1.85f * scale, 0),
                new Vector3(.48f, 3.7f, .16f) * scale, Motion.Still, .86f, 0, fx.fallingWeapon);
            fx.Primitive("Celestial Crossguard", PrimitiveType.Cube, new Vector3(0, 3.25f * scale, 0),
                new Vector3(1.6f, .19f, .25f) * scale, Motion.Still, .86f, 0, fx.fallingWeapon);
            fx.Primitive("Celestial Hilt", PrimitiveType.Cylinder, new Vector3(0, 3.65f * scale, 0),
                new Vector3(.14f, .42f, .14f) * scale, Motion.Still, .86f, 0, fx.fallingWeapon);
            fx.Wave(1.7f * scale, .12f, .27f, .55f);
            fx.Wave(2.4f * scale, .06f, .36f, .5f);
            for (int i = 0; i < 10; i++)
            {
                float a = i * Mathf.PI * .2f;
                Vector3 radial = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                SolidPart shard = fx.MeshSolid("Impact Shard", blade, radial * .15f + Vector3.up * .16f,
                    new Vector3(.11f, .5f, .13f) * scale, Motion.Burst, .52f, .27f);
                shard.Velocity = (radial * 4.5f + Vector3.up * (2.4f + i % 3)) * scale;
                shard.Spin = new Vector3(150 + i * 13, 190, 120);
            }
        }

        private void SolidRune(float size, int detail)
        {
            float burstLife = Mathf.Min(duration, .8f);
            Wave(size, .10f, 0, burstLife);
            if (detail >= 2) Wave(size * .82f, .055f, .14f, Mathf.Min(duration - .14f, .7f));
            Mesh crystal = CrystalMesh();
            bool lingering = duration > 2f;
            int count = detail >= 3 ? 10 : 6;
            for (int i = 0; i < count; i++)
            {
                float a = i * Mathf.PI * 2f / count;
                Vector3 radial = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                Vector3 dimensions = owner.HeroClass == HeroClass.Vanguard ? new Vector3(.17f, .8f, .13f) :
                    owner.HeroClass == HeroClass.Arcanist ? new Vector3(.27f, .58f, .27f) : new Vector3(.13f, .6f, .09f);
                SolidPart shard = MeshSolid(owner.HeroClass == HeroClass.Ranger ? "Verdant Arrowleaf" : "Spell Crystal",
                    crystal, radial * size * (lingering ? .86f : .27f) + Vector3.up * .25f,
                    dimensions * Mathf.Clamp(size * .43f, .65f, 1.6f), lingering ? Motion.Orbit : Motion.Burst,
                    lingering ? duration : burstLife, 0);
                shard.Phase = a;
                shard.Velocity = radial * size * 1.35f + Vector3.up * (1.4f + (i % 3) * .4f);
                shard.Spin = new Vector3(45f + i * 9f, 65f, 70f);
                shard.Transform.localRotation = Quaternion.Euler(15f, -a * Mathf.Rad2Deg, 25f);
                shard.Rotation = shard.Transform.localRotation;
            }
            if (detail >= 2)
            {
                for (int i = 0; i < 4; i++)
                {
                    float a = i * Mathf.PI * .5f + .3f;
                    Vector3 at = new Vector3(Mathf.Cos(a) * size * .76f, .65f, Mathf.Sin(a) * size * .76f);
                    SolidPart sigil = MeshSolid("Rising Runic Lance", crystal, at, new Vector3(.18f, 1.9f, .18f),
                        Motion.Pulse, Mathf.Min(duration, 1.4f));
                    sigil.Velocity = Vector3.up * .5f;
                }
            }
            if (owner.HeroClass == HeroClass.Arcanist && detail >= 3)
                Primitive("Arcane Nova Core", PrimitiveType.Sphere, Vector3.up * .6f,
                    Vector3.one * Mathf.Min(size * .4f, 1.25f), Motion.Pulse, burstLife);
        }

        private SolidPart Primitive(string name, PrimitiveType type, Vector3 at, Vector3 scale,
            Motion motion, float life, float delay = 0, Transform parent = null)
        {
            if (solids.Count >= MaximumSolidParts) return null;
            GameObject obj = GameObject.CreatePrimitive(type);
            Collider collider = obj.GetComponent<Collider>();
            if (collider != null) { collider.enabled = false; Destroy(collider); }
            return RegisterSolid(obj, name, at, scale, motion, life, delay, parent);
        }

        private SolidPart MeshSolid(string name, Mesh mesh, Vector3 at, Vector3 scale,
            Motion motion, float life, float delay = 0, Transform parent = null)
        {
            if (solids.Count >= MaximumSolidParts) return null;
            GameObject obj = new GameObject(name);
            obj.AddComponent<MeshFilter>().sharedMesh = mesh;
            obj.AddComponent<MeshRenderer>();
            return RegisterSolid(obj, name, at, scale, motion, life, delay, parent);
        }

        private SolidPart RegisterSolid(GameObject obj, string name, Vector3 at, Vector3 scale,
            Motion motion, float life, float delay, Transform parent)
        {
            obj.name = name;
            obj.transform.SetParent(parent != null ? parent : transform, false);
            obj.transform.localPosition = at;
            obj.transform.localScale = scale;
            Renderer renderer = obj.GetComponent<Renderer>();
            renderer.sharedMaterial = solidMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            properties.SetColor("_Color", tint);
            renderer.SetPropertyBlock(properties);
            SolidPart part = new SolidPart { Transform = obj.transform, Renderer = renderer, Origin = at, Scale = scale,
                Movement = motion, Life = Mathf.Max(.06f, life), Delay = delay, Rotation = Quaternion.identity };
            solids.Add(part);
            if (delay > 0) obj.SetActive(false);
            return part;
        }

        private void Wave(float size, float thickness, float delay, float life)
        {
            const int segments = 48;
            Vector3[] vertices = new Vector3[segments * 2];
            int[] triangles = new int[segments * 12];
            for (int i = 0; i < segments; i++)
            {
                float a = i * Mathf.PI * 2f / segments;
                Vector3 radial = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                vertices[i * 2] = radial * size;
                vertices[i * 2 + 1] = radial * size * (1f - thickness);
                int a0 = i * 2, b = ((i + 1) % segments) * 2, offset = i * 12;
                triangles[offset] = a0; triangles[offset + 1] = a0 + 1; triangles[offset + 2] = b;
                triangles[offset + 3] = b; triangles[offset + 4] = a0 + 1; triangles[offset + 5] = b + 1;
                triangles[offset + 6] = a0; triangles[offset + 7] = b; triangles[offset + 8] = a0 + 1;
                triangles[offset + 9] = b; triangles[offset + 10] = b + 1; triangles[offset + 11] = a0 + 1;
            }
            Mesh mesh = new Mesh { name = "Solid Expanding Shockwave", vertices = vertices, triangles = triangles };
            mesh.RecalculateNormals();
            meshes.Add(mesh);
            MeshSolid("Expanding Shockwave", mesh, Vector3.up * .075f, Vector3.one, Motion.Wave, life, delay);
        }

        private Mesh CrystalMesh()
        {
            // A pointed eight-sided solid, reused by every shard belonging to this effect.
            Mesh mesh = new Mesh { name = "Faceted Spell Crystal" };
            mesh.vertices = new[] { new Vector3(0,-.5f,0), new Vector3(-.5f,0,0), new Vector3(0,0,.5f),
                new Vector3(.5f,0,0), new Vector3(0,0,-.5f), new Vector3(0,.5f,0) };
            mesh.triangles = new[] { 0,2,1,0,3,2,0,4,3,0,1,4,5,1,2,5,2,3,5,3,4,5,4,1 };
            mesh.RecalculateNormals();
            meshes.Add(mesh);
            return mesh;
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
            if (fallingWeapon != null)
            {
                float descent = Mathf.Clamp01(age / .27f);
                fallingWeapon.localPosition = Vector3.up * (fallingHeight * (1f - descent * descent));
            }
            for (int i = 0; i < solids.Count; i++) AnimateSolid(solids[i]);
        }

        private void AnimateSolid(SolidPart part)
        {
            float localAge = age - part.Delay;
            bool visible = localAge >= 0 && localAge < part.Life;
            if (part.Transform.gameObject.activeSelf != visible) part.Transform.gameObject.SetActive(visible);
            if (!visible) return;
            float t = Mathf.Clamp01(localAge / part.Life);
            float fade = Mathf.Min(1f, localAge * 22f) * Mathf.Min(1f, (1f - t) * 4f);
            Vector3 at = part.Origin;
            Vector3 scale = part.Scale;
            switch (part.Movement)
            {
                case Motion.Burst:
                    at += part.Velocity * localAge + Vector3.down * localAge * localAge * 2.4f;
                    scale *= 1f - t * .55f;
                    break;
                case Motion.Orbit:
                    float a = part.Phase + localAge * .85f;
                    float orbitRadius = new Vector2(part.Origin.x, part.Origin.z).magnitude;
                    at = new Vector3(Mathf.Cos(a) * orbitRadius, .65f + Mathf.Sin(a * 2f) * .23f, Mathf.Sin(a) * orbitRadius);
                    break;
                case Motion.Wave:
                    scale *= .18f + Mathf.Sqrt(t) * 1.04f;
                    fade *= .7f;
                    break;
                case Motion.Pulse:
                    scale *= .72f + Mathf.Sin(t * Mathf.PI) * .4f;
                    at += part.Velocity * localAge;
                    break;
            }
            part.Transform.localPosition = at;
            part.Transform.localScale = scale;
            part.Transform.localRotation = part.Rotation * Quaternion.Euler(part.Spin * localAge);
            properties.SetColor("_Color", new Color(Mathf.Lerp(tint.r, 1, .16f), Mathf.Lerp(tint.g, 1, .16f),
                Mathf.Lerp(tint.b, 1, .16f), tint.a * fade * .9f));
            part.Renderer.SetPropertyBlock(properties);
        }

        private void OnDestroy()
        {
            activeEffects = Mathf.Max(0,activeEffects-1);
            if (material != null) Destroy(material);
            if (solidMaterial != null) Destroy(solidMaterial);
            for (int i = 0; i < meshes.Count; i++) if (meshes[i] != null) Destroy(meshes[i]);
        }
    }
}
