using UnityEngine;
using UnityEngine.Rendering;

namespace Emberfall
{
    /// <summary>Visible dungeon loot remains session-owned until collected or settled on exit.</summary>
    public sealed class GroundLootPickup : MonoBehaviour
    {
        public const float PickupRadius = 2f;
        public const float LandingProtection = .6f;
        public string ItemId { get; private set; }
        public bool ReadyToCollect { get { return age >= LandingProtection && !retired; } }
        private GameSession session;
        private float age;
        private bool retired;
        private Transform model;
        private Transform label;
        private Material bodyMaterial;
        private Material glowMaterial;
        private Font font;

        public void Initialize(GameSession owner, ItemData item)
        {
            session = owner;
            ItemId = item.id;
            Color color = GameBalance.RarityColor(item.rarity);
            bodyMaterial = new Material(Shader.Find("Standard")) { color = Color.Lerp(color, Color.white, .2f), hideFlags = HideFlags.HideAndDontSave };
            bodyMaterial.EnableKeyword("_EMISSION");
            bodyMaterial.SetColor("_EmissionColor", color * .55f);
            Shader glowShader = Shader.Find("Sprites/Default");
            if (glowShader == null) glowShader = Shader.Find("Unlit/Color");
            glowMaterial = new Material(glowShader) { color = color, hideFlags = HideFlags.HideAndDontSave };
            model = new GameObject("Equipment miniature").transform;
            model.SetParent(transform, false);
            model.localPosition = Vector3.up * .4f;
            if (item.slot == ItemSlot.Weapon)
            {
                Part("Blade", PrimitiveType.Cube, new Vector3(0, .23f, 0), new Vector3(.11f, .67f, .075f));
                Part("Guard", PrimitiveType.Cube, new Vector3(0, -.1f, 0), new Vector3(.43f, .085f, .12f));
                Part("Grip", PrimitiveType.Cube, new Vector3(0, -.23f, 0), new Vector3(.075f, .23f, .09f));
            }
            else if (item.slot == ItemSlot.Armor)
            {
                Part("Chest", PrimitiveType.Cube, Vector3.zero, new Vector3(.44f, .53f, .23f));
                Part("Shoulder left", PrimitiveType.Cube, new Vector3(-.27f, .17f, 0), new Vector3(.2f, .2f, .25f));
                Part("Shoulder right", PrimitiveType.Cube, new Vector3(.27f, .17f, 0), new Vector3(.2f, .2f, .25f));
            }
            else
            {
                Part("Relic", PrimitiveType.Sphere, Vector3.zero, new Vector3(.37f, .48f, .3f));
                Part("Relic clasp", PrimitiveType.Cube, new Vector3(0, .3f, 0), new Vector3(.12f, .15f, .1f));
            }
            LineRenderer beam = Line("Rarity beam", false, 2, .13f);
            beam.SetPosition(0, new Vector3(0, .12f, 0));
            beam.SetPosition(1, new Vector3(0, 2.6f, 0));
            beam.startColor = new Color(1, 1, 1, .8f);
            beam.endColor = new Color(1, 1, 1, 0);
            LineRenderer ring = Line("Rarity ring", true, 40, .035f);
            for (int i = 0; i < 40; i++)
            {
                float angle = i * Mathf.PI * 2 / 40;
                ring.SetPosition(i, new Vector3(Mathf.Cos(angle) * .43f, .105f, Mathf.Sin(angle) * .43f));
            }
            font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "PingFang SC", "PingFangSC-Regular", "Heiti SC", "SimHei", "Arial" }, 24);
            label = new GameObject("Item name").transform;
            label.SetParent(transform, false);
            label.localPosition = new Vector3(0, 1.25f, 0);
            TextMesh text = label.gameObject.AddComponent<TextMesh>();
            text.font = font;
            text.fontSize = 36;
            text.characterSize = .06f;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.color = Color.Lerp(color, Color.white, .35f);
            string itemName = string.IsNullOrEmpty(item.name) ? "未知装备" : item.name;
            text.text = itemName.Length > 10 ? itemName.Substring(0, 10) : itemName;
            if (font != null) label.GetComponent<Renderer>().sharedMaterial = font.material;
        }

        private void Part(string name, PrimitiveType shape, Vector3 position, Vector3 size)
        {
            GameObject part = GameObject.CreatePrimitive(shape);
            part.name = name;
            part.transform.SetParent(model, false);
            part.transform.localPosition = position;
            part.transform.localScale = size;
            Collider collider = part.GetComponent<Collider>();
            if (collider != null) { collider.enabled = false; Destroy(collider); }
            Renderer renderer = part.GetComponent<Renderer>();
            renderer.sharedMaterial = bodyMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        private LineRenderer Line(string name, bool loop, int count, float width)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(transform, false);
            LineRenderer line = child.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.loop = loop;
            line.positionCount = count;
            line.widthMultiplier = width;
            line.sharedMaterial = glowMaterial;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            return line;
        }

        private void Update()
        {
            if (retired || session == null || session.InputBlocked || !session.InDungeon) return;
            age += Time.deltaTime;
            if (model != null)
            {
                model.localPosition = Vector3.up * (.4f + Mathf.Sin(age * 2.5f) * .055f);
                model.localRotation = Quaternion.Euler(0, age * 35f, -15);
            }
            if (!ReadyToCollect || session.Player == null) return;
            Vector3 offset = session.Player.transform.position - transform.position;
            offset.y = 0;
            if (offset.sqrMagnitude <= PickupRadius * PickupRadius) session.TryCollectGroundLoot(ItemId);
        }

        private void LateUpdate()
        {
            if (label != null && Camera.main != null) label.rotation = Camera.main.transform.rotation;
        }

        public void Retire()
        {
            if (retired) return;
            retired = true;
            gameObject.SetActive(false);
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (bodyMaterial != null) Destroy(bodyMaterial);
            if (glowMaterial != null) Destroy(glowMaterial);
            if (font != null) Destroy(font);
        }
    }
}
