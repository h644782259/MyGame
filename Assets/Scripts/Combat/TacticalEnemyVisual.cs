using UnityEngine;
namespace Emberfall
{
    // One live child per enemy; no world-space history rings survive a state change.
    public sealed class TacticalEnemyVisual:MonoBehaviour
    {
        private EnemyController enemy;private GameSession session;private int epoch;
        private Material material;private LineRenderer crown,ward,feet;private bool supported;private float severedUntil;
        public static void Attach(EnemyController enemy,GameSession session)
        {
            if(enemy==null||session==null||enemy.GetComponentInChildren<TacticalEnemyVisual>(true)!=null)return;
            var root=new GameObject("Live tactical relations");root.transform.SetParent(enemy.transform,false);
            var visual=root.AddComponent<TacticalEnemyVisual>();visual.enemy=enemy;visual.session=session;visual.epoch=session.Player.CombatEpoch;
            visual.material=CombatFx.NewGlow();visual.crown=visual.Line("Supplier crown",new Color(1,.8f,.2f),6);
            visual.ward=visual.Line("Supported ward",new Color(.2f,.85f,1),5);visual.feet=visual.Line("Contender feet",new Color(1,.4f,.18f),17);
            for(int i=0;i<6;i++)visual.crown.SetPosition(i,new Vector3((i-2.5f)*.18f,2.25f+(i%2==0?.12f:.38f),0));
            Vector3[] diamond={new Vector3(0,1.8f,0),new Vector3(.24f,2.03f,0),new Vector3(0,2.26f,0),new Vector3(-.24f,2.03f,0),new Vector3(0,1.8f,0)};
            for(int i=0;i<5;i++)visual.ward.SetPosition(i,diamond[i]);
            for(int i=0;i<17;i++){float a=i*Mathf.PI/8;visual.feet.SetPosition(i,new Vector3(Mathf.Cos(a),.08f,Mathf.Sin(a))*(enemy.NavigationRadius+.16f));}
            visual.Refresh();
        }
        private LineRenderer Line(string name,Color color,int points)
        {var obj=new GameObject(name);obj.transform.SetParent(transform,false);var line=obj.AddComponent<LineRenderer>();line.useWorldSpace=false;line.sharedMaterial=material;line.positionCount=points;line.widthMultiplier=.07f;line.startColor=line.endColor=color;return line;}
        private void LateUpdate(){Refresh();}
        private void Refresh()
        {
            if(enemy==null||enemy.IsDead||!enemy.isActiveAndEnabled||session==null||session.Player==null||session.Player.CombatEpoch!=epoch||session.CombatEnded)
            {Hide();return;}
            crown.enabled=session.IsRoomSupplier(enemy)||session.IsChapterSupplier(enemy)||session.IsChapterHuntTarget(enemy);
            bool next=session.RoomSupportMultiplier(enemy)<1||session.ChapterSupportMultiplier(enemy)<1;
            if(supported&&!next)severedUntil=Time.time+.24f;
            supported=next;ward.enabled=next||Time.time<severedUntil;
            ward.startColor=ward.endColor=next?new Color(.2f,.85f,1):new Color(.55f,.55f,.55f);
            ward.widthMultiplier=next?.07f:.025f;
            feet.enabled=session.IsRoomContesting(enemy)||session.IsChapterContesting(enemy);
        }
        private void Hide(){if(crown!=null)crown.enabled=false;if(ward!=null)ward.enabled=false;if(feet!=null)feet.enabled=false;}
        private void OnDisable(){Hide();supported=false;severedUntil=0;}
        private void OnDestroy(){if(material!=null)Destroy(material);}
    }
}
