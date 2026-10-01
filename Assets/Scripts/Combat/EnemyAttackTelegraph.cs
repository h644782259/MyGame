using System.Collections.Generic;
using UnityEngine;

namespace Emberfall
{
    /// <summary>Controller-owned warnings: fixed footprints live for the entire windup, including stun.</summary>
    internal sealed class EnemyAttackTelegraph : MonoBehaviour
    {
        private readonly List<LineRenderer> lines = new List<LineRenderer>();
        private Material material;
        private float progress;
        private bool interruptible;

        public static EnemyAttackTelegraph Circle(Vector3 center, float radius)
        {
            EnemyAttackTelegraph warning = Create("Enemy Warning - Impact Circle");
            var points = new Vector3[72];
            for (int i = 0; i < points.Length; i++)
            {
                float angle = i * Mathf.PI * 2f / points.Length;
                points[i] = center + new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle)) * radius;
            }
            warning.Line(points, true, .11f);
            return warning;
        }

        public static EnemyAttackTelegraph Charge(Vector3 start, Vector3 end, float halfWidth)
        {
            EnemyAttackTelegraph warning = Create("Enemy Warning - Charge Corridor");
            warning.Lane(start, end, halfWidth, .1f);
            return warning;
        }

        public static EnemyAttackTelegraph Fan(Vector3 muzzle, Vector3[] directions, float[] lengths, float halfWidth)
        {
            EnemyAttackTelegraph warning = Create("Enemy Warning - Projectile Lanes");
            for (int i = 0; i < directions.Length; i++)
                if (lengths[i] > .05f) warning.Lane(muzzle, muzzle + directions[i] * lengths[i], halfWidth, .055f);
            return warning;
        }

        private static EnemyAttackTelegraph Create(string title)
        {
            var root = new GameObject(title);
            var warning = root.AddComponent<EnemyAttackTelegraph>();
            Shader shader = Resources.Load<Shader>("ThreatBoundary");
            warning.material = shader == null ? CombatFx.NewGlow() : new Material(shader);
            warning.material.renderQueue = 3900;
            return warning;
        }

        private void Lane(Vector3 start, Vector3 end, float halfWidth, float width)
        {
            Vector3 forward = CombatFx.Flat(end - start).normalized;
            if (forward.sqrMagnitude < .01f) return;
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            // Rounded ends match the swept-circle collision used by charge and projectiles.
            var outline = new Vector3[34];
            for (int i = 0; i <= 16; i++)
            {
                float angle = i * Mathf.PI / 16f;
                outline[i] = end + (right * Mathf.Cos(angle) + forward * Mathf.Sin(angle)) * halfWidth;
                outline[i + 17] = start + (-right * Mathf.Cos(angle) - forward * Mathf.Sin(angle)) * halfWidth;
            }
            Line(outline, true, width);
            Line(new[] { start, end }, false, width * .7f);
            float arrow = Mathf.Min(.65f, Vector3.Distance(start, end) * .25f);
            Line(new[] { end - forward * arrow + right * arrow * .6f, end, end - forward * arrow - right * arrow * .6f }, false, width * 1.4f);
        }

        private void Line(Vector3[] points, bool loop, float width)
        {
            var obj = new GameObject("Warning Geometry");
            obj.transform.SetParent(transform, false);
            LineRenderer line = obj.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.loop = loop;
            line.positionCount = points.Length;
            for (int i = 0; i < points.Length; i++)
            {
                Vector3 point = CombatFx.Flat(points[i]);
                point.y = .16f;
                line.SetPosition(i, point);
            }
            line.widthMultiplier = Mathf.Max(.07f, width);
            line.sortingOrder = 120;
            line.sharedMaterial = material;
            line.startColor = line.endColor = new Color(1f, .32f, .12f, .85f);
            lines.Add(line);
        }

        public void SetInterruptible(bool value)
        {
            if (interruptible == value) return;
            interruptible = value;
            SetProgress(progress);
        }

        public void SetProgress(float value)
        {
            progress = Mathf.Clamp01(value);
            Color color = interruptible
                ? Color.Lerp(new Color(.15f, .72f, .85f, .8f), new Color(.45f, 1f, .8f, 1f), progress)
                : Color.Lerp(new Color(1f, .46f, .12f, .75f), new Color(1f, .08f, .13f, 1f), progress);
            foreach (LineRenderer line in lines) if (line != null) line.startColor = line.endColor = color;
        }

        private void OnDestroy() { if (material != null) Destroy(material); }
    }
}
