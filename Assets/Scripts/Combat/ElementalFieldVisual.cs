using UnityEngine;

namespace Emberfall
{
    // Large, camera-readable shapes supplement the smaller particle details.
    internal sealed class ElementalFieldVisual : MonoBehaviour
    {
        private ElementalCombatVfx.Element element;
        private float radius;
        private bool onBody;
        private LineRenderer[] strands;
        private Transform[] bubbles;
        private Material material;

        public static ElementalFieldVisual Spawn(Transform parent, ElementalCombatVfx.Element type, float size, bool body)
        {
            GameObject obj = new GameObject(type + " Silhouette");
            obj.transform.SetParent(parent, false);
            ElementalFieldVisual effect = obj.AddComponent<ElementalFieldVisual>();
            effect.Initialize(type, size, body);
            return effect;
        }

        private void Initialize(ElementalCombatVfx.Element type, float size, bool body)
        {
            element = type;
            radius = size;
            onBody = body;
            int count = onBody ? 7 : type == ElementalCombatVfx.Element.Poison ? 14 : 12;
            if (type == ElementalCombatVfx.Element.Poison)
            {
                material = new Material(Shader.Find("Unlit/Color"));
                material.color = new Color(.47f, 1f, .2f);
                bubbles = new Transform[count];
                for (int i = 0; i < count; i++)
                {
                    GameObject bubble = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    bubble.name = "Venom Bubble";
                    bubble.transform.SetParent(transform, false);
                    Collider collider = bubble.GetComponent<Collider>();
                    if (collider != null) Destroy(collider);
                    bubble.GetComponent<Renderer>().sharedMaterial = material;
                    bubbles[i] = bubble.transform;
                }
            }
            else
            {
                material = CombatFx.NewGlow();
                strands = new LineRenderer[count];
                for (int i = 0; i < count; i++)
                {
                    GameObject strand = new GameObject(type == ElementalCombatVfx.Element.Fire ? "Flame Tongue" : "Electric Branch");
                    strand.transform.SetParent(transform, false);
                    LineRenderer line = strand.AddComponent<LineRenderer>();
                    line.useWorldSpace = false;
                    line.positionCount = 4;
                    line.sharedMaterial = material;
                    line.widthMultiplier = onBody ? .25f : type == ElementalCombatVfx.Element.Fire ? .3f : .12f;
                    line.widthCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(.55f, .78f), new Keyframe(1f, 0f));
                    line.startColor = type == ElementalCombatVfx.Element.Fire ? new Color(1f, .85f, .2f) : new Color(.78f, .97f, 1f);
                    line.endColor = type == ElementalCombatVfx.Element.Fire ? new Color(1f, .25f, .06f) : new Color(.3f, .58f, 1f);
                    strands[i] = line;
                }
            }
        }

        private void Update()
        {
            float time = Time.time;
            int count = bubbles != null ? bubbles.Length : strands.Length;
            for (int i = 0; i < count; i++)
            {
                float angle = i * 2.39996f + time * (onBody ? .45f : .2f);
                float distance = radius * (onBody ? .75f : .28f + (i % 4) * .19f);
                Vector3 origin = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * distance;
                if (bubbles != null)
                {
                    float rise = Mathf.Repeat(time * .65f + i * .173f, 1f);
                    float height = onBody ? 1.05f : 1.35f;
                    bubbles[i].localPosition = origin + Vector3.up * ((onBody ? -.35f : .1f) + rise * height);
                    float size = (onBody ? .13f : .18f) * (1f + .35f * Mathf.Sin(time * 5f + i));
                    bubbles[i].localScale = Vector3.one * size;
                }
                else if (element == ElementalCombatVfx.Element.Fire)
                {
                    float height = (onBody ? .9f : 1.25f) * (.75f + .25f * Mathf.Sin(time * 8f + i));
                    float bottom = onBody ? -.42f : .08f;
                    Vector3 sway = new Vector3(Mathf.Sin(time * 6f + i * 2f), 0f, Mathf.Cos(time * 5f + i)) * .18f;
                    strands[i].SetPosition(0, origin + Vector3.up * bottom);
                    strands[i].SetPosition(1, origin + sway * .4f + Vector3.up * (bottom + height * .3f));
                    strands[i].SetPosition(2, origin + sway + Vector3.up * (bottom + height * .7f));
                    strands[i].SetPosition(3, origin + sway * 1.3f + Vector3.up * (bottom + height));
                }
                else
                {
                    float height = onBody ? 1f : 2.35f;
                    Vector3 top = origin * .25f + Vector3.up * height;
                    strands[i].SetPosition(0, top);
                    strands[i].SetPosition(1, Vector3.Lerp(top, origin, .33f) + new Vector3(Mathf.Sin(time * 39f + i), 0f, Mathf.Cos(time * 31f + i)) * .28f);
                    strands[i].SetPosition(2, Vector3.Lerp(top, origin, .68f) + new Vector3(Mathf.Cos(time * 37f + i), 0f, Mathf.Sin(time * 29f + i)) * .25f);
                    strands[i].SetPosition(3, origin + Vector3.up * .08f);
                }
            }
        }

        private void OnDestroy() { if (material != null) Destroy(material); }
    }
}
