using UnityEngine;
namespace Emberfall
{
    public sealed class TacticalCaptureVisual:MonoBehaviour
    {
        private GameSession session;private LineRenderer[] segments;private Material material;private int epoch;
        public static void Attach(GameObject objective,GameSession session)
        {
            var root=new GameObject("Segmented capture progress");root.transform.SetParent(objective.transform,false);
            var visual=root.AddComponent<TacticalCaptureVisual>();visual.session=session;visual.epoch=session.Player.CombatEpoch;visual.material=CombatFx.NewGlow();visual.segments=new LineRenderer[12];
            for(int i=0;i<12;i++)
            {
                var part=new GameObject("Capture segment "+i);part.transform.SetParent(root.transform,false);
                var line=part.AddComponent<LineRenderer>();line.sharedMaterial=visual.material;line.useWorldSpace=false;line.widthMultiplier=.1f;line.positionCount=3;
                for(int j=0;j<3;j++){float a=(i+(j*.4f+.1f))*Mathf.PI/6;line.SetPosition(j,new Vector3(Mathf.Sin(a)*2.6f,.1f,Mathf.Cos(a)*2.6f));}
                visual.segments[i]=line;
            }
            visual.Refresh();
        }
        private void LateUpdate(){Refresh();}
        private void Refresh()
        {
            float fraction=0;bool contested=false;bool active=session!=null&&session.Player!=null&&session.Player.CombatEpoch==epoch&&session.TryGetTacticalCapture(out fraction,out contested);
            if(!active){Hide();return;}
            for(int i=0;i<segments.Length;i++){segments[i].enabled=true;Color tint=fraction*12>=i+1?(contested?new Color(1,.45f,.18f):new Color(.3f,1,.65f)):new Color(.22f,.28f,.32f);segments[i].startColor=segments[i].endColor=tint;}
        }
        private void Hide(){if(segments!=null)foreach(var line in segments)if(line!=null)line.enabled=false;}
        private void OnDisable(){Hide();}
        private void OnDestroy(){if(material!=null)Destroy(material);}
    }
}
