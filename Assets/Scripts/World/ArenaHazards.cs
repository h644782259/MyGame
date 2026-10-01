using UnityEngine;
namespace Emberfall
{
 /// <summary>Two telegraphed environmental pulses; advances only from combat-time host ticks.</summary>
 public sealed class ArenaHazards:MonoBehaviour
 {
  private GameSession session;private int mode;private float age;private Material material;
  private readonly LineRenderer[] rings=new LineRenderer[3];private readonly Vector3[] centers=new Vector3[3];private readonly int[] hitCycle={-1,-1,-1};
  public void Initialize(GameSession owner,int selectedMode)
  {
   session=owner;mode=selectedMode;material=CombatFx.NewGlow();material.renderQueue=3105;
   centers[0]=mode==0?new Vector3(-8,0,0):mode==1?new Vector3(-5,0,-5):new Vector3(-5,0,3);
   centers[1]=mode==0?new Vector3(8,0,0):mode==1?new Vector3(5,0,5):new Vector3(5,0,-3);
   centers[2]=Vector3.zero;
   for(int i=0;i<rings.Length;i++)
   {
    var go=new GameObject(i==2?"Hold objective boundary":"Arena hazard warning");go.transform.SetParent(transform,false);
    var line=go.AddComponent<LineRenderer>();rings[i]=line;line.useWorldSpace=true;line.loop=true;line.positionCount=48;line.widthMultiplier=i==2?.07f:.10f;
    line.sharedMaterial=material;line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;line.sortingOrder=35;
    float radius=i==2?3.2f:2.1f;for(int p=0;p<48;p++){float a=p*Mathf.PI*2/48;line.SetPosition(p,centers[i]+new Vector3(Mathf.Cos(a)*radius,.08f,Mathf.Sin(a)*radius));}
    line.enabled=i==2&&mode==0;line.startColor=line.endColor=new Color(.3f,1,.62f,.6f);
   }
  }
  public void Advance(float dt)
  {
   if(session==null||session.Player==null||session.InputBlocked||dt<=0)return;age+=dt;
   for(int i=0;i<2;i++)
   {
    float clock=age+i*2.7f;int cycle=Mathf.FloorToInt(clock/6.5f);float phase=clock-cycle*6.5f;
    bool warning=phase>=4.9f,active=phase>=6.1f;rings[i].enabled=warning;
    Color color=active?new Color(1,.25f,.1f,.9f):new Color(1,.68f,.2f,.72f);rings[i].startColor=rings[i].endColor=color;
    if(active&&hitCycle[i]!=cycle&&CombatFx.Flat(session.Player.transform.position-centers[i]).sqrMagnitude<=4.41f)
    {hitCycle[i]=cycle;session.Player.TakeDamageFrom(Mathf.Max(3,session.Player.MaxHealth*.065f),mode==0?"林庭荆棘脉冲":mode==1?"烬河热涌":"蚀星脉冲");}
   }
  }
  private void OnDestroy(){if(material!=null)Destroy(material);}
 }
}
