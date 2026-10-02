using UnityEngine;
namespace Emberfall
{
    // Attach only to floating navigation labels. Floor inscriptions keep their authored pose.
    public sealed class WorldLabelPresentation:MonoBehaviour
    {
        private TextMesh label;private Renderer visual;private Vector3 originalScale;private int lines;
        public void Initialize(TextMesh text)
        {label=text;GameFont.Apply(label);visual=GetComponent<Renderer>();originalScale=transform.localScale;lines=1;foreach(char c in label.text)if(c=='\n')lines++;}
        private void LateUpdate()
        {
            Camera camera=Camera.main;if(camera==null||label==null||visual==null)return;
            transform.rotation=camera.transform.rotation;transform.localScale=originalScale;
            Vector3 point=camera.WorldToScreenPoint(transform.position);if(point.z<=0)return;
            // Renderer local glyph bounds reflect the actual font rather than a guessed Chinese width.
            float height=visual.localBounds.size.y*transform.lossyScale.y;
            Vector3 top=camera.WorldToScreenPoint(transform.position+camera.transform.up*height);
            float pixels=Mathf.Abs(top.y-point.y);
            transform.localScale=originalScale*WorldLabelReadability.Scale(pixels,lines);
        }
    }
}
