using UnityEngine;
namespace Emberfall
{
    public sealed class GuardArmorVisual:MonoBehaviour
    {
        private EnemyController enemy;private LineRenderer plates,core;private Material material;private float hitUntil;private bool scraped;
        public static GuardArmorVisual Attach(EnemyController enemy)
        {
            if(enemy==null||enemy.Kind!=EnemyKind.Guardian||enemy.IsBoss)return null;
            var root=new GameObject("Guardian live armor");root.transform.SetParent(enemy.transform,false);
            var visual=root.AddComponent<GuardArmorVisual>();visual.enemy=enemy;visual.material=CombatFx.NewGlow();
            visual.plates=visual.Line("Front armor seams",8);visual.core=visual.Line("Windup exposed core",5);visual.Refresh();return visual;
        }
        private LineRenderer Line(string name,int count)
        {var child=new GameObject(name);child.transform.SetParent(transform,false);var line=child.AddComponent<LineRenderer>();line.sharedMaterial=material;line.useWorldSpace=false;line.positionCount=count;line.widthMultiplier=.065f;return line;}
        public void RecordImpact(bool armorReduced,bool weakpointOpen)
        {if(!armorReduced&&!weakpointOpen)return;scraped=armorReduced;hitUntil=Time.time+.16f;Refresh();}
        private void LateUpdate(){Refresh();}
        private void Refresh()
        {
            if(enemy==null||enemy.IsDead||!enemy.isActiveAndEnabled){Hide();return;}
            bool closed=enemy.GuardArmorClosed;bool flash=Time.time<hitUntil;
            plates.enabled=true;core.enabled=!closed;
            Color plate=flash&&scraped?new Color(1,.85f,.45f):new Color(.6f,.72f,.78f);plates.startColor=plates.endColor=plate;
            float gap=closed?.04f:.3f;
            for(int side=0;side<2;side++){float sign=side==0?-1:1;int i=side*4;plates.SetPosition(i,new Vector3(sign*gap,1.65f,.48f));plates.SetPosition(i+1,new Vector3(sign*(gap+.26f),1.5f,.48f));plates.SetPosition(i+2,new Vector3(sign*(gap+.26f),1.15f,.48f));plates.SetPosition(i+3,new Vector3(sign*gap,1.0f,.48f));}
            core.startColor=core.endColor=flash&&!scraped?Color.white:new Color(1,.45f,.15f);
            for(int i=0;i<5;i++){float a=i*Mathf.PI*.5f;core.SetPosition(i,new Vector3(Mathf.Sin(a)*.17f,1.34f+Mathf.Cos(a)*.22f,.5f));}
        }
        private void Hide(){if(plates!=null)plates.enabled=false;if(core!=null)core.enabled=false;}
        private void OnDisable(){Hide();hitUntil=0;}
        private void OnDestroy(){if(material!=null)Destroy(material);}
    }
}
