using UnityEngine;

namespace Emberfall
{
    internal static class ElementalCombatVfx
    {
        internal enum Element { Fire, Lightning, Poison }

        public static void Area(Transform parent, float radius, Element element)
        {
            // The readable field owns its own lease; optional particles must not gate it.
            ElementalFieldVisual.Spawn(parent, element, radius, false,CombatVisualPriority.SustainedBackground);
            ParticleSystem particles = Create(parent, element == Element.Fire ? "Rising Flames" :
                element == Element.Poison ? "Poison Bubbles" : "Storm Sparks", element,
                Mathf.Clamp(radius * radius * 1.8f, 8f, 45f), radius);
            if(particles==null)return;
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = radius * .82f;
            shape.radiusThickness = 1f;
            particles.transform.localPosition = Vector3.up * .16f;
            particles.gameObject.AddComponent<CoveredAreaParticles>();
            particles.Play();
        }

        public static void OnEnemy(EnemyController enemy, Element element, float duration)
        {
            if (enemy == null || enemy.IsDead) return;
            ElementalEnemyAura aura = enemy.GetComponent<ElementalEnemyAura>();
            if (aura == null) aura = enemy.gameObject.AddComponent<ElementalEnemyAura>();
            aura.Refresh(element, duration, enemy.IsBoss ? 1.7f : enemy.Kind == EnemyKind.Slime ? .65f : 1.2f);
        }

        internal static void ClearFire(EnemyController enemy)
        {var aura=enemy==null?null:enemy.GetComponent<ElementalEnemyAura>();if(aura!=null)aura.ClearFire();}

        internal static void ClearPoison(EnemyController enemy)
        {var aura=enemy==null?null:enemy.GetComponent<ElementalEnemyAura>();if(aura!=null)aura.ClearPoison();}

        internal static ParticleSystem Create(Transform parent, string name, Element element, float rate, float radius)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            if(!DecorationLease.Attach(obj,0)){Object.Destroy(obj);return null;}
            ParticleSystem particles = obj.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.playOnAwake = false;
            main.loop = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = element == Element.Lightning ? .16f : element == Element.Poison ? .85f : .48f;
            main.startSpeed = element == Element.Lightning ? 2.4f : element == Element.Poison ? .8f : 1.7f;
            main.startSize = element == Element.Poison ? .2f : element == Element.Lightning ? .11f : .27f;
            main.maxParticles = DecorationBudget.Particles(EffectPreferences.EffectsScale);
            main.gravityModifier = -.08f;
            main.startColor = element == Element.Fire ? new Color(1f, .46f, .08f, .85f) :
                element == Element.Poison ? new Color(.48f, 1f, .22f, .7f) : new Color(.56f, .88f, 1f, .9f);
            var emission = particles.emission;
            emission.rateOverTime = rate * EffectPreferences.EffectsScale;
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = radius;
            var size = particles.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, .35f), new Keyframe(.3f, 1f), new Keyframe(1f, 0f)));
            ParticleSystemRenderer renderer = obj.GetComponent<ParticleSystemRenderer>();
            Shader shader = Shader.Find("Particles/Standard Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            renderer.sharedMaterial = new Material(shader);
            obj.AddComponent<OwnedParticleMaterial>().Value = renderer.sharedMaterial;
            return particles;
        }

        public static void Lightning(Vector3 from, Vector3 to)
        {
            GameObject obj = new GameObject("Lightning Fork");
            if(!DecorationLease.Attach(obj,1)){Object.Destroy(obj);return;}
            LineRenderer line = obj.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            int points=EffectPreferences.ReducedEffects?4:7;
            line.positionCount = points;
            line.widthMultiplier = .095f;
            line.sharedMaterial = CombatFx.NewGlow();
            Color color = new Color(.55f, .89f, 1f, .95f);
            line.startColor = line.endColor = color;
            Vector3 tangent = Vector3.Cross((to - from).normalized, Vector3.up);
            if (tangent.sqrMagnitude < .01f) tangent = Vector3.right;
            for (int i = 0; i < points; i++)
            {
                float t = i / (float)(points-1);
                line.SetPosition(i, Vector3.Lerp(from, to, t) + tangent * (i == 0 || i == points-1 ? 0 : Random.Range(-.3f, .3f)));
            }
            obj.AddComponent<FadingCombatEffect>().Setup(line, color, 1f, .18f, false);
        }
    }

    internal sealed class ElementalEnemyAura : MonoBehaviour
    {
        private ParticleSystem fire, poison;
        private ElementalFieldVisual fireShape, poisonShape;
        private float fireUntil, poisonUntil;

        public void Refresh(ElementalCombatVfx.Element element, float duration, float height)
        {
            if (element == ElementalCombatVfx.Element.Lightning) return;
            bool burning = element == ElementalCombatVfx.Element.Fire;
            // Siblings under the enemy: retiring a decoration can never retire its main shape.
            ElementalFieldVisual activeShape = burning ? fireShape : poisonShape;
            if (activeShape == null)
            {
                activeShape = ElementalFieldVisual.Spawn(transform, element, .48f, true,CombatVisualPriority.SustainedBackground);
                if (activeShape != null) activeShape.transform.localPosition = Vector3.up * height;
                if (burning) fireShape = activeShape; else poisonShape = activeShape;
            }
            if (activeShape != null) activeShape.gameObject.SetActive(true);
            if (burning) fireUntil = Mathf.Max(fireUntil, Time.time + duration);
            else poisonUntil = Mathf.Max(poisonUntil, Time.time + duration);
            ParticleSystem particles = burning ? fire : poison;
            if (particles == null)
            {
                particles = ElementalCombatVfx.Create(transform, burning ? "Burning Body" : "Poisoned Body",
                    element, burning ? 27f : 12f, .35f);
                if (particles == null) return;
                particles.transform.localPosition = Vector3.up * height;
                if (burning) fire = particles; else poison = particles;
            }
            particles.gameObject.SetActive(true);
            if (particles.gameObject.activeInHierarchy && !particles.isPlaying) particles.Play();
        }

        internal void ClearFire()
        {fireUntil=Time.time;if(fire!=null){fire.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);fire.gameObject.SetActive(false);}if(fireShape!=null)fireShape.gameObject.SetActive(false);}

        // Retire only the sustained poison channel synchronously. Keep its reusable
        // components alive so same-frame reapplication cannot inherit a queued Destroy.
        internal void ClearPoison()
        {poisonUntil=Time.time;if(poison!=null){poison.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);poison.gameObject.SetActive(false);}if(poisonShape!=null)poisonShape.gameObject.SetActive(false);}

        private void Update()
        {
            if (fire != null && Time.time >= fireUntil && fire.isEmitting) fire.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (poison != null && Time.time >= poisonUntil && poison.isEmitting) poison.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if(fire!=null&&Time.time>=fireUntil)fire.gameObject.SetActive(false);
            if(poison!=null&&Time.time>=poisonUntil)poison.gameObject.SetActive(false);
            if (fireShape != null && Time.time >= fireUntil) fireShape.gameObject.SetActive(false);
            if (poisonShape != null && Time.time >= poisonUntil) poisonShape.gameObject.SetActive(false);
        }
    }

    internal sealed class OwnedParticleMaterial : MonoBehaviour
    {
        public Material Value;
        private void OnDestroy() { if (Value != null) Destroy(Value); }
    }
}
