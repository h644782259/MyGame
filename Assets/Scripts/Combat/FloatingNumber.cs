using UnityEngine;

namespace Emberfall
{
    /// <summary>Bounded, screen-legible combat text with a dark four-corner outline.</summary>
    public sealed class FloatingNumber : MonoBehaviour
    {
        public static int ActiveCount { get; private set; }
        private static Font sharedFont;
        private static int sequence;
        private TextMesh textMesh;
        private MeshRenderer textRenderer;
        private readonly TextMesh[] outline = new TextMesh[4];
        private float life;
        private Color color;
        private bool critical, counted;
        private const float Lifetime = 1.3f;

        public void Initialize(string value, Color tint, bool isCritical = false)
        {
            if (counted) return;
            if (ActiveCount >= 48) { Destroy(gameObject); return; }
            ActiveCount++;
            counted = true;
            critical = isCritical;
            color = critical ? new Color(1f, .87f, .18f) : tint;
            if (sharedFont == null)
                sharedFont = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "微软雅黑", "PingFang SC", "PingFangSC-Regular", "Heiti SC", "SimHei", "Arial" }, 64);
            string display = critical && !string.IsNullOrEmpty(value) && !value.EndsWith("!", System.StringComparison.Ordinal) ? value + "!" : value;
            // Separate simultaneous hits slightly without randomness or extra critical rolls.
            Camera camera = Camera.main;
            if (camera != null) transform.position += camera.transform.right * ((sequence++ % 3 - 1) * .22f);
            for (int i = 0; i < outline.Length; i++)
            {
                var edge = new GameObject("Combat text outline");
                edge.transform.SetParent(transform, false);
                edge.transform.localPosition = new Vector3(i % 2 == 0 ? -.018f : .018f, i < 2 ? -.018f : .018f, .012f);
                outline[i] = Configure(edge, display, new Color(.045f, .025f, .035f, 1));
                edge.GetComponent<MeshRenderer>().sortingOrder = 100;
            }
            textMesh = Configure(gameObject, display, color);
            textRenderer = GetComponent<MeshRenderer>();
            textRenderer.sortingOrder = 101;
            FaceCamera();
        }

        private TextMesh Configure(GameObject obj, string value, Color tint)
        {
            TextMesh mesh = obj.AddComponent<TextMesh>();
            mesh.text = value;
            mesh.fontSize = 64;
            mesh.characterSize = .085f;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.fontStyle = FontStyle.Bold;
            mesh.color = tint;
            if (sharedFont != null)
            {
                mesh.font = sharedFont;
                obj.GetComponent<MeshRenderer>().sharedMaterial = sharedFont.material;
            }
            return mesh;
        }

        private void Update()
        {
            life += Time.deltaTime;
            transform.position += Vector3.up * Time.deltaTime * .65f * EffectPreferences.EffectsScale;
            float alpha = Mathf.Clamp01((Lifetime - life) / .35f);
            if (textMesh != null) textMesh.color = new Color(color.r, color.g, color.b, alpha);
            foreach (TextMesh edge in outline)
                if (edge != null) edge.color = new Color(.045f, .025f, .035f, alpha * .98f);
            if (life > Lifetime) Destroy(gameObject);
        }

        private void LateUpdate() { FaceCamera(); }

        private void FaceCamera()
        {
            Camera camera = Camera.main;
            if (camera == null || textRenderer == null) return;
            transform.rotation = camera.transform.rotation;
            float glyphHeight = textRenderer.localBounds.size.y;
            if (glyphHeight <= .001f || camera.pixelHeight <= 0) return;
            float depth = Mathf.Max(1, Vector3.Dot(transform.position - camera.transform.position, camera.transform.forward));
            float visibleHeight = camera.orthographic ? camera.orthographicSize * 2f : depth * 2f * Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * .5f);
            float pixels = 21f * EffectPreferences.CombatTextScale * (critical ? 1.28f : 1f);
            float pop = critical ? 1f + Mathf.Max(0, 1f - life / .16f) * .14f * EffectPreferences.EffectsScale : 1f;
            float scale = Mathf.Clamp(pixels * visibleHeight / (camera.pixelHeight * glyphHeight), .05f, 15f);
            transform.localScale = Vector3.one * scale * pop;
        }

        private void OnDestroy() { if (counted) ActiveCount = Mathf.Max(0, ActiveCount - 1); }
    }
}
