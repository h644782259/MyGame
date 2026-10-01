using UnityEngine;

namespace Emberfall
{
    // Local hit reactions never alter global time, player movement or skill timers.
    public sealed class HitFeedback : MonoBehaviour
    {
        private static float cameraStarted = -10f, cameraStrength, nextCameraTime;
        private static int activeCount;
        private LineRenderer[] streaks;
        private Vector3[] directions;
        private Material material;
        private float age, strength, effectsScale;
        private bool critical;
        private Color coreColor;
        private const float Duration = .19f;

        public static Vector3 CameraOffset
        {
            get
            {
                if (!EffectPreferences.CameraShake) return Vector3.zero;
                float elapsed = Time.time - cameraStarted;
                if (elapsed < 0 || elapsed >= .16f) return Vector3.zero;
                float envelope = Mathf.Sin(elapsed / .16f * Mathf.PI) * (1f - elapsed / .16f);
                return Vector3.up * (envelope * cameraStrength * EffectPreferences.EffectsScale);
            }
        }

        public static void ClearCamera() { cameraStarted = -10f; nextCameraTime = 0; cameraStrength = 0; }

        public static void Spawn(Vector3 point, Vector3 direction, float intensity, bool isCritical = false)
        {
            if (activeCount >= 24) return;
            var obj = new GameObject(isCritical ? "Critical hit impact" : "Hit impact");
            obj.transform.position = point;
            var impact = obj.AddComponent<HitFeedback>();
            impact.critical = isCritical;
            impact.effectsScale = EffectPreferences.EffectsScale;
            impact.strength = Mathf.Clamp(intensity, isCritical ? 1.6f : .55f, isCritical ? 2.5f : 2f);
            impact.Build(direction);
            if (EffectPreferences.CameraShake && intensity >= 1.5f && Time.time >= nextCameraTime)
            {
                cameraStarted = Time.time;
                cameraStrength = Mathf.Min(isCritical ? .055f : .045f, .022f * intensity);
                nextCameraTime = Time.time + .18f;
            }
        }

        private void Build(Vector3 incoming)
        {
            activeCount++;
            material = CombatFx.NewGlow();
            int count = Mathf.Max(3, Mathf.RoundToInt((critical ? 12 : strength >= 1.5f ? 8 : 5) * effectsScale));
            streaks = new LineRenderer[count];
            directions = new Vector3[count];
            coreColor = critical ? new Color(1f, .92f, .23f) : new Color(1f, .96f, .74f);
            Vector3 forward = incoming.sqrMagnitude > .01f ? incoming.normalized : Vector3.forward;
            for (int i = 0; i < count; i++)
            {
                float angle = i * Mathf.PI * 2 / count;
                directions[i] = (new Vector3(Mathf.Cos(angle), .3f + Mathf.Sin(angle) * .65f, Mathf.Sin(angle)) + forward * .55f).normalized;
                var spark = new GameObject(critical ? "Critical golden spark" : "Impact spark");
                spark.transform.SetParent(transform, false);
                var line = spark.AddComponent<LineRenderer>();
                line.useWorldSpace = false;
                line.positionCount = 2;
                line.widthMultiplier = (critical ? .085f : .065f) * Mathf.Sqrt(strength);
                line.sharedMaterial = material;
                line.startColor = coreColor;
                line.endColor = critical ? new Color(1f, .35f, .08f, 0) : new Color(1f, .62f, .19f, 0);
                line.SetPosition(0, Vector3.zero);
                line.SetPosition(1, directions[i] * .14f);
                streaks[i] = line;
            }
        }

        private void Update()
        {
            if (GameSession.Instance == null || !GameSession.Instance.HasStarted) { Destroy(gameObject); return; }
            age += Time.deltaTime;
            if (age >= Duration) { Destroy(gameObject); return; }
            float t = age / Duration;
            for (int i = 0; i < streaks.Length; i++)
            {
                float reach = (.18f + t * (critical ? 1.2f : .85f) * effectsScale) * Mathf.Sqrt(strength);
                streaks[i].SetPosition(0, directions[i] * reach * t);
                streaks[i].SetPosition(1, directions[i] * reach);
                streaks[i].startColor = new Color(coreColor.r, coreColor.g, coreColor.b, 1f - t);
                streaks[i].widthMultiplier = (critical ? .085f : .065f) * Mathf.Sqrt(strength) * (1f - t);
            }
        }

        private void OnDestroy() { activeCount = Mathf.Max(0, activeCount - 1); if (material != null) Destroy(material); }
    }
}
