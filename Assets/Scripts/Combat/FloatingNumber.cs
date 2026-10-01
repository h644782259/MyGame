using UnityEngine;

namespace Emberfall
{
    /// <summary>Bounded, screen-legible combat text with a dark four-corner outline.</summary>
    public sealed class FloatingNumber : MonoBehaviour
    {
        public static int ActiveCount { get; private set; }
        private static Font sharedFont;
        private static readonly System.Collections.Generic.List<FloatingNumber> visible=new System.Collections.Generic.List<FloatingNumber>();
        private Vector3 originAtSpawn;
        private int lane;
        private float Duration {get{return critical?1.12f:.88f;}}
        private TextMesh textMesh;
        private MeshRenderer textRenderer;
        private readonly TextMesh[] outline = new TextMesh[4];
        private float life;
        private Color color;
        private bool critical, counted;


        // Read-only admission is shared by session callers and initialization.
        // Reject crowded text before allocating a GameObject, without retiring a
        // potential critical replacement unless the new text can take its place.
        public static bool CanSpawn(Vector3 origin, bool isCritical = false)
        {
            FloatingNumber replacement; int selectedLane; Camera camera;
            return TrySelectAdmission(origin, isCritical, out replacement, out selectedLane, out camera);
        }

        public static FloatingNumber Spawn(Vector3 origin, string value, Color tint, bool isCritical = false,
            string objectName = "Combat Text")
        {
            FloatingNumber replacement; int selectedLane; Camera camera;
            if(!TrySelectAdmission(origin,isCritical,out replacement,out selectedLane,out camera))return null;
            // Consume the local admission immediately. No second camera/lane scan,
            // callbacks or saved plan can separate selection from registration.
            GameObject root=new GameObject(objectName);root.transform.position=origin;
            FloatingNumber number=root.AddComponent<FloatingNumber>();
            number.InitializeAdmitted(value,tint,isCritical,replacement,selectedLane,camera);
            return number;
        }

        private static bool TrySelectAdmission(Vector3 origin, bool isCritical, out FloatingNumber replacement,
            out int selectedLane, out Camera camera)
        {
            replacement = null; selectedLane = 0; camera = null;
            int globalLimit=MobileControls.Active?20:36;
            if(ActiveCount>=globalLimit)
            {
                if(!isCritical)return false;
                foreach(var number in visible)
                    if(number!=null&&!number.critical){replacement=number;break;}
                if(replacement==null)return false;
            }
            camera=Camera.main;
            Vector2 projectedOrigin=camera!=null?(Vector2)camera.WorldToScreenPoint(origin):Vector2.zero;
            int occupied=0,nearby=0;
            foreach(var number in visible)
            {
                if(number==null||number==replacement)continue;
                bool close=camera!=null?Vector2.Distance(projectedOrigin,camera.WorldToScreenPoint(number.originAtSpawn))<110:
                    Vector3.Distance(origin,number.originAtSpawn)<2f;
                if(close){occupied|=1<<number.lane;nearby++;}
            }
            if(nearby>=(isCritical?6:4))return false;
            while(selectedLane<6&&(occupied&(1<<selectedLane))!=0)selectedLane++;
            return true;
        }

        public void Initialize(string value, Color tint, bool isCritical = false)
        {
            if (counted) return;
            FloatingNumber replacement; Camera camera; int selectedLane;
            if(!TrySelectAdmission(transform.position,isCritical,out replacement,out selectedLane,out camera))
            {Destroy(gameObject);return;}
            InitializeAdmitted(value,tint,isCritical,replacement,selectedLane,camera);
        }

        private void InitializeAdmitted(string value, Color tint, bool isCritical, FloatingNumber replacement,
            int selectedLane, Camera camera)
        {
            if(replacement!=null)replacement.Retire();
            critical=isCritical;originAtSpawn=transform.position;lane=selectedLane;
            ActiveCount++;counted=true;visible.Add(this);
            color = critical ? new Color(1f, .87f, .18f) : tint;
            if (sharedFont == null)
                sharedFont = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "微软雅黑", "PingFang SC", "PingFangSC-Regular", "Heiti SC", "SimHei", "Arial" }, 64);
            string display = critical && !string.IsNullOrEmpty(value) && !value.EndsWith("!", System.StringComparison.Ordinal) ? value + "!" : value;
            // Six bounded screen-space lanes keep overlapping AoE hits out of the
            // character silhouette. No random crit roll or cumulative global offset.
            if(camera!=null)
            {
                float depth=Mathf.Max(1,Vector3.Dot(transform.position-camera.transform.position,camera.transform.forward));
                float worldHeight=camera.orthographic?camera.orthographicSize*2:depth*2*Mathf.Tan(camera.fieldOfView*Mathf.Deg2Rad*.5f);
                float units=worldHeight/Mathf.Max(1,camera.pixelHeight);
                transform.position+=camera.transform.right*((lane%2==0?-1:1)*35*units)+camera.transform.up*((lane/2)*34+14)*units;
            }
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
            float alpha = Mathf.Clamp01((Duration - life) / .25f);
            if (textMesh != null) textMesh.color = new Color(color.r, color.g, color.b, alpha);
            foreach (TextMesh edge in outline)
                if (edge != null) edge.color = new Color(.045f, .025f, .035f, alpha * .98f);
            if (life > Duration) Retire();
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

        private void Retire()
        { if(counted){counted=false;ActiveCount=Mathf.Max(0,ActiveCount-1);visible.Remove(this);}gameObject.SetActive(false);Destroy(gameObject); }
        private void OnDestroy() { visible.Remove(this);if (counted) ActiveCount = Mathf.Max(0, ActiveCount - 1); }
    }
}
