using System;
using System.Linq;
using Emberfall;
using UnityEngine;
public static class TacticalLiveVisualTests
{
 static int checks;static void Check(bool value,string why){checks++;if(!value)throw new Exception(why);}
 static LineRenderer Line(string name)=>GameObject.All.Last(o=>o.name==name).GetComponent<LineRenderer>();
 public static string Run()
 {
  Time.time=1;var player=new GameObject("Player").AddComponent<PlayerController>();var game=new GameSession{Player=player};var enemy=new GameObject("Enemy").AddComponent<EnemyController>();enemy.session=game;
  game.Supplier=true;game.Supported=true;game.Contested=true;TacticalEnemyVisual.Attach(enemy,game);var live=enemy.GetComponentInChildren<TacticalEnemyVisual>(true);
  Check(Line("Supplier crown").enabled&&Line("Supported ward").enabled&&Line("Contender feet").enabled,"actual live visual initial supplier/support/contender states");
  var parent=Line("Supplier crown").transform.parent.parent;Check(parent==enemy.transform,"marker follows enemy object rather than detached world position");
  var before=Line("Supplier crown").transform.TransformPoint(Line("Supplier crown").Positions[0]);enemy.transform.position=new Vector3(3,0,0);var moved=Line("Supplier crown").transform.TransformPoint(Line("Supplier crown").Positions[0]);Check(Math.Abs(moved.x-before.x-3)<.0001f,"live supplier crown follows actual owner transform");
  int count=GameObject.All.Count;TacticalEnemyVisual.Attach(enemy,game);Check(GameObject.All.Count==count,"no duplicate marker component allocations");
  game.Supported=false;game.Supplier=false;game.Contested=false;live.gameObject.Call("LateUpdate");
  Check(!Line("Supplier crown").enabled&&!Line("Contender feet").enabled&&Line("Supported ward").startColor.r==.55f,"support removal immediately extinguishes supported color");
  Time.time+=.25f;live.gameObject.Call("LateUpdate");Check(!Line("Supported ward").enabled,"severed feedback expires without stale support ring");
  game.Supported=true;live.gameObject.Call("LateUpdate");Check(Line("Supported ward").enabled,"new support relights same owned object");player.CombatEpoch++;live.gameObject.Call("LateUpdate");Check(!Line("Supported ward").enabled,"old combat epoch hides all relations");
  var objective=new GameObject("Objective");game.Capture=true;game.Fraction=.5f;TacticalCaptureVisual.Attach(objective,game);
  Check(GameObject.All.Count(o=>o.name.StartsWith("Capture segment ")&&o.GetComponent<LineRenderer>().startColor.g==1)==6,"actual capture renderer shows six of twelve complete segments");
  var capture=GameObject.All.Last(o=>o.name=="Segmented capture progress");game.Capture=false;capture.Call("LateUpdate");Check(GameObject.All.Where(o=>o.name.StartsWith("Capture segment ")).All(o=>!o.GetComponent<LineRenderer>().enabled),"completed capture hides segments");
  game.Supported=false;enemy.Kind=EnemyKind.Guardian;enemy.Health=100;var armor=GuardArmorVisual.Attach(enemy);enemy.guardArmorVisual=armor;
  enemy.TakeDamagePrefix(100,new Vector3(0,0,-1));Check(Math.Abs(enemy.Health-35)<.001f&&enemy.GuardArmorClosed,"actual closed-front hit deducts65 and shows closed armor");
  enemy.Health=100;enemy.StatusEffects=new Status{DamageMultiplier=0};Time.time+=1;armor.gameObject.Call("LateUpdate");enemy.TakeDamagePrefix(100,new Vector3(0,0,-1));Check(enemy.Health==100&&Line("Front armor seams").startColor.r==.6f,"zero actual health loss does not emit armor-hit flash");enemy.StatusEffects=null;
  enemy.Health=100;enemy.preparing=true;armor.gameObject.Call("LateUpdate");Check(Line("Windup exposed core").enabled&&!enemy.GuardArmorClosed,"windup opens real weakpoint state");
  enemy.TakeDamagePrefix(100,new Vector3(0,0,-1));Check(enemy.Health==0,"actual windup hit has no front mitigation");
  enemy.Health=100;enemy.preparing=false;armor.gameObject.Call("LateUpdate");Check(!Line("Windup exposed core").enabled,"recovery closes weakpoint immediately");
  enemy.TakeDamagePrefix(100,new Vector3(0,0,1));Check(enemy.Health==0,"rear damage remains unmitigated");
  enemy.Health=100;enemy.IsBoss=true;enemy.TakeDamagePrefix(100,new Vector3(0,0,-1));Check(enemy.Health==0,"boss excluded from ordinary guard armor");
  armor.gameObject.SetActive(false);Check(!Line("Front armor seams").enabled&&!Line("Windup exposed core").enabled,"disable never leaves stale armor visuals");
  var material=Line("Front armor seams").sharedMaterial;UnityEngine.Object.Destroy(armor.gameObject);Check(material.Destroyed,"owned armor material released on destroy");
  Check(GuardArmorRules.Multiplier(true,false,false,.01f,1)==1&&GuardArmorRules.Multiplier(true,false,false,1,.45f)==1,"original strict direction and frontal thresholds preserved");
  Check(GuardArmorRules.Multiplier(true,false,false,1,.451f)==.65f&&GuardArmorRules.Multiplier(true,false,true,1,1)==1,"existing closed-front reduction and windup exemption preserved");
  live.gameObject.SetActive(false);Check(!Line("Supplier crown").enabled&&!Line("Supported ward").enabled&&!Line("Contender feet").enabled,"disable clears all relation visuals immediately");
  var tacticalMaterial=Line("Supplier crown").sharedMaterial;UnityEngine.Object.Destroy(live.gameObject);Check(tacticalMaterial.Destroyed,"relation material released");
  var captureMaterial=Line("Capture segment 0").sharedMaterial;UnityEngine.Object.Destroy(capture);Check(captureMaterial.Destroyed,"capture material released");
  return "PASS: "+checks+" actual tactical/guard/capture component lifecycle and damage assertions";
 }
}
namespace Emberfall
{
 public enum EnemyKind {Guardian,Wisp}
 public class PlayerController:MonoBehaviour {public int CombatEpoch;}
 public class GameSession
 {public PlayerController Player;public bool CombatEnded,HasStarted=true,Supplier,Supported,Contested,Capture;public float Fraction;
 public bool IsRoomSupplier(EnemyController e)=>Supplier;public bool IsChapterSupplier(EnemyController e)=>false;public bool IsChapterHuntTarget(EnemyController e)=>false;
 public float RoomSupportMultiplier(EnemyController e)=>Supported?.7f:1;public float ChapterSupportMultiplier(EnemyController e)=>1;
 public bool IsRoomContesting(EnemyController e)=>Contested;public bool IsChapterContesting(EnemyController e)=>false;
 public bool TryGetTacticalCapture(out float fraction,out bool contested){fraction=Fraction;contested=Contested;return Capture;}}
 public class Status {public float DamageMultiplier=1;}
 public class Boss {public BossState State=new BossState();}public class BossState {public float IncomingMultiplier=1;}
 public partial class EnemyController:MonoBehaviour
 {
  public GameSession session;public bool IsBoss,preparing;public bool IsDead=>Health<=0;public bool isActiveAndEnabled=>gameObject.activeInHierarchy;public float Health=100,NavigationRadius=.6f;public EnemyKind Kind;public Status StatusEffects;public Boss largeBoss;public GuardArmorVisual guardArmorVisual;
  public T GetComponentInChildren<T>(bool inactive)where T:Component=>GameObject.All.Where(o=>o.transform.parent==transform).Select(o=>o.GetComponent<T>()).FirstOrDefault(c=>c!=null);
 }
 public static class AdventureResultPolicy {public static bool AcceptsDamage(bool started,bool ended)=>started&&!ended;}
 public static class CombatFx {public static Material NewGlow()=>new Material(new Shader());public static Vector3 Flat(Vector3 v)=>new Vector3(v.x,0,v.z);}
}
