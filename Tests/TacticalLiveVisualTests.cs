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
  Time.time=1;Camera.main=new GameObject("camera").AddComponent<Camera>();Camera.main.transform.rotation=Quaternion.Euler(20,45,0);var player=new GameObject("Player").AddComponent<PlayerController>();var game=new GameSession{Player=player};var enemy=new GameObject("Enemy").AddComponent<EnemyController>();enemy.session=game;
  game.Supplier=true;game.Supported=true;game.Contested=true;TacticalEnemyVisual.Attach(enemy,game);var live=enemy.GetComponentInChildren<TacticalEnemyVisual>(true);
  Check(Line("Supplier crown").enabled&&Line("Supported ward").enabled&&Line("Contender feet").enabled,"actual live visual initial supplier/support/contender states");
  var parent=Line("Supplier crown").transform.parent.parent.parent;Check(parent==enemy.transform,"marker follows enemy object rather than detached world position");
  var before=Line("Supplier crown").transform.TransformPoint(Line("Supplier crown").Positions[0]);enemy.transform.position=new Vector3(3,0,0);var moved=Line("Supplier crown").transform.TransformPoint(Line("Supplier crown").Positions[0]);Check(Math.Abs(moved.x-before.x-3)<.0001f,"live supplier crown follows actual owner transform");
  Check(Line("Supplier crown").transform.parent.rotation.Equals(Camera.main.transform.rotation),"head marker faces actual camera while foot marker remains ground bound");
  enemy.gameObject.Call("LateUpdate");enemy.gameObject.Call("LateUpdate");Check(WorldBuilder.LegacyRings==0,"enemy update never allocates the redundant legacy contest rings");
  game.RoomChainRun=new RoomChainState();live.gameObject.Call("LateUpdate");Check(Line("Hunt target crosshair").enabled,"legacy hunt provider also carries distinct hunt-target marker");game.RoomChainRun=null;
  game.Supplier=false;game.Hunt=true;live.gameObject.Call("LateUpdate");Check(Line("Hunt target crosshair").enabled&&!Line("Supplier crown").enabled,"hunt target has a distinct crosshair rather than provider crown");game.Hunt=false;game.Supplier=true;
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
  game.ChapterSeal=true;game.SealFractions[0]=.5f;game.SealFractions[1]=0;game.SealContested[1]=true;
  var sealA=new GameObject("seal A");var sealB=new GameObject("seal B");sealB.transform.position=new Vector3(8,0,1);TacticalCaptureVisual.AttachChapter(sealA,game,0);TacticalCaptureVisual.AttachChapter(sealB,game,1);
  var roots=GameObject.All.Where(o=>o.name=="Segmented capture progress"&&!o.Destroyed).ToArray();var aRoot=roots[roots.Length-2];var bRoot=roots[roots.Length-1];
  Func<GameObject,string,LineRenderer> local=(root,name)=>GameObject.All.Single(o=>o.transform.parent==root.transform&&o.name==name).GetComponent<LineRenderer>();
  Check(GameObject.All.Count(o=>o.transform.parent==aRoot.transform&&o.name.StartsWith("Capture segment ")&&o.GetComponent<LineRenderer>().startColor.g==1)==6&&local(bRoot,"Capture segment 0").startColor.g!=1,"two seals render independent local progress");
  Check(local(bRoot,"Capture contested flag").enabled,"zero-progress contested seal still shows visible flag");
  game.SealCompleted[0]=true;game.SealFractions[0]=1;game.NextObjective=sealB.transform.position;aRoot.Call("LateUpdate");Check(local(aRoot,"Capture segment 0").enabled&&local(aRoot,"Capture segment 0").startColor.r==.16f&&local(aRoot,"Capture next direction").enabled,"completed seal retains dark circle and points toward remaining objective");
  game.SealCompleted[1]=true;game.NextObjective=new Vector3(0,0,14);bRoot.Call("LateUpdate");Check(!local(bRoot,"Capture contested flag").enabled&&local(bRoot,"Capture next direction").enabled,"completed contested seal switches to exit direction without hiding ground record");
  game.ChapterSeal=false;game.RoomSeal=true;game.RoomChainRun=new RoomChainState();game.SealFractions[0]=0;game.SealFractions[1]=.5f;game.SealCompleted[0]=game.SealCompleted[1]=false;game.SealContested[0]=true;game.SealContested[1]=false;
  var roomA=new GameObject("room A");var roomB=new GameObject("room B");TacticalCaptureVisual.AttachRoomSeal(roomA,game,0);TacticalCaptureVisual.AttachRoomSeal(roomB,game,1);
  var roomARoot=GameObject.All.Single(o=>o.transform.parent==roomA.transform&&o.name=="Segmented capture progress");var roomBRoot=GameObject.All.Single(o=>o.transform.parent==roomB.transform&&o.name=="Segmented capture progress");
  Check(local(roomARoot,"Seal A").enabled&&local(roomBRoot,"Seal B").enabled&&local(roomARoot,"Capture contested flag").enabled&&!local(roomBRoot,"Capture contested flag").enabled,"room owner mode renders stable identities and independent pressure without chapter adapter");
  Check(GameObject.All.Count(o=>o.transform.parent==roomBRoot.transform&&o.name.StartsWith("Capture segment ")&&o.GetComponent<LineRenderer>().startColor.g==1)==6,"room B first progress renders on B");
  game.SealCompleted[1]=true;game.SealFractions[1]=1;game.NextObjective=new Vector3(-8,0,-6);roomBRoot.Call("LateUpdate");Check(local(roomBRoot,"Capture next direction").enabled&&local(roomBRoot,"Capture segment 0").startColor.r==.16f,"room completed B retains dark record and points to A");
  game.Player=new GameObject("replacement same epoch").AddComponent<PlayerController>();game.Player.CombatEpoch=player.CombatEpoch;roomARoot.Call("LateUpdate");roomBRoot.Call("LateUpdate");Check(!local(roomARoot,"Seal A").enabled&&!local(roomBRoot,"Capture segment 0").enabled,"replacement player same epoch cannot revive old room visuals");game.Player=player;
  var oldRoom=game.RoomChainRun.Room;game.RoomChainRun.Room=new RoomPlan();roomARoot.Call("LateUpdate");Check(!local(roomARoot,"Seal A").enabled,"replacement room same epoch cannot revive old room visuals");game.RoomChainRun.Room=oldRoom;
  var oldRun=game.RoomChainRun;game.RoomChainRun=new RoomChainState{Room=oldRoom};roomBRoot.Call("LateUpdate");Check(!local(roomBRoot,"Capture segment 0").enabled,"replacement run with same plan cannot revive old room visuals");game.RoomChainRun=oldRun;
  player.CombatEpoch++;aRoot.Call("LateUpdate");bRoot.Call("LateUpdate");roomARoot.Call("LateUpdate");roomBRoot.Call("LateUpdate");Check(!local(roomARoot,"Seal A").enabled&&!local(roomBRoot,"Capture segment 0").enabled,"room owner old epoch hides both seals");Check(!local(aRoot,"Capture next direction").enabled&&!local(bRoot,"Capture segment 0").enabled,"old epoch hides both retained seal presentations");
  return "PASS: "+checks+" actual tactical/guard/capture component lifecycle and damage assertions";
 }
}
namespace Emberfall
{
 public static class WorldBuilder{public static int LegacyRings;public static GameObject MakeRoomContestMarker(Transform owner,float radius){LegacyRings++;return new GameObject("legacy rings");}}
 public enum RoomObjective{Hunt,Escape}public class RoomChainState{public RoomPlan Room=new RoomPlan();}public class RoomPlan{public RoomObjective Objective;}
 public enum EnemyKind {Guardian,Wisp}
 public class PlayerController:MonoBehaviour {public int CombatEpoch;}
 public class GameSession
 {public RoomChainState RoomChainRun;public PlayerController Player;public bool CombatEnded,HasStarted=true,Supplier,Supported,Contested,Capture,Hunt,ChapterSeal;public float[] SealFractions=new float[2];public bool[] SealContested=new bool[2],SealCompleted=new bool[2];public Vector3 NextObjective;public Vector3 ChapterNextObjectivePoint=>NextObjective;public Vector3 RoomNextObjectivePoint=>NextObjective;public bool RoomSeal;
 public bool TryGetRoomSeal(int index,out float fraction,out bool contested,out bool complete){fraction=SealFractions[index];contested=SealContested[index];complete=SealCompleted[index];return RoomSeal;}
 public bool TryGetChapterSeal(int index,out float fraction,out bool contested,out bool complete){fraction=SealFractions[index];contested=SealContested[index];complete=SealCompleted[index];return ChapterSeal;}public float Fraction;
 public bool IsRoomSupplier(EnemyController e)=>Supplier;public bool IsChapterSupplier(EnemyController e)=>false;public bool IsChapterHuntTarget(EnemyController e)=>Hunt;
 public float RoomSupportMultiplier(EnemyController e)=>Supported?.7f:1;public float ChapterSupportMultiplier(EnemyController e)=>1;
 public bool IsRoomContesting(EnemyController e)=>Contested;public bool IsChapterContesting(EnemyController e)=>false;
 public bool TryGetTacticalCapture(out float fraction,out bool contested){fraction=Fraction;contested=Contested;return Capture;}}
 public class Status {public float DamageMultiplier=1;}
 public class Boss {public BossState State=new BossState();}public class BossState {public float IncomingMultiplier=1;}
 public partial class EnemyController:MonoBehaviour
 {
  public GameObject roomContestMarker;public Transform healthRoot,healthFill;public bool aggro;public float MaxHealth=100;public GameSession session;public bool IsBoss,preparing;public bool IsDead=>Health<=0;public bool isActiveAndEnabled=>gameObject.activeInHierarchy;public float Health=100,NavigationRadius=.6f;public EnemyKind Kind;public Status StatusEffects;public Boss largeBoss;public GuardArmorVisual guardArmorVisual;
  public T GetComponentInChildren<T>(bool inactive)where T:Component=>GameObject.All.Where(o=>o.transform.parent==transform).Select(o=>o.GetComponent<T>()).FirstOrDefault(c=>c!=null);
 }
 public static class AdventureResultPolicy {public static bool AcceptsDamage(bool started,bool ended)=>started&&!ended;}
 public static class CombatFx {public static Material NewGlow()=>new Material(new Shader());public static Vector3 Flat(Vector3 v)=>new Vector3(v.x,0,v.z);}
}

namespace UnityEngine{public sealed class Camera:Component{public static Camera main;}}
