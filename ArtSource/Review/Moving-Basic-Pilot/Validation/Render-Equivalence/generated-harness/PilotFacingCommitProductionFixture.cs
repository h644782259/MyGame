using System;using System.Linq;using UnityEngine;using Emberfall;
namespace UnityEngine{
 public struct Vector2{public float x,y;}
 public enum KeyCode{J}
 public static class Input{public static Vector2 mousePosition;public static bool Held=true;public static bool GetMouseButton(int n)=>Held;public static bool GetKey(KeyCode k)=>false;public static bool GetKeyDown(KeyCode k)=>false;}
 public class Camera{public static Camera main;}
}
namespace Emberfall{
 public class GameProfile{public int[] hotbarKeys;}
 public static class GameBalance{public const int HotbarSize=4,HotbarPotion=-3,SkillCount=10;}
 public static class MobileControls{public static bool AttackHeld=true;}
 public partial class GameSession{public float ArenaRadius=100;public bool PointerOverUI;public void UseHotbarConsumable(){throw new Exception("unexpected hotbar");}}
 public class EnemyController:MonoBehaviour{}
 public sealed partial class CombatModel{
  private class RecoveryBoundary{public void Reset(){}}private RecoveryBoundary visualMotion=new RecoveryBoundary();private bool visualYawReady;private int visualMotionFrame;
  public void Animate(float speed,float attack,bool hurt)=>Tick(Time.deltaTime,hurt);
  public void ForgetWalking()=>ResetLocomotion();
 }
 public partial class PlayerController{
  public Vector3 Aim;public float attackCooldown,mobilityTime,attackAnimation,jumpAge;private bool suppressBasicUntilReleased;public bool TraversalStartedThisFrame;private Vector3 aimPoint;private float MovementMultiplier=>1;
  private bool ValidAimTarget(EnemyController e)=>e!=null;private int HotbarSkill(int slot)=>-1;
  private Vector3 ResolveMobileAim(Vector3 movement)=>Aim;private Vector3 ResolveAim(Camera c,Vector2 screen)=>Aim;
  private bool MobilePinnedActionAllowed(int skill,bool basic)=>true;private EnemyController MagicConeTarget()=>null;
  public void Setup(){model.ForgetWalking();}
 }
}
static class PilotFacingFixture{
 static int n;static void C(bool value,string label){n++;if(!value)throw new Exception(label);}
 static PlayerController Create(){
  var asset=new GameObject("asset");asset.AddComponent<Renderer>();asset.AddComponent<MeshFilter>().sharedMesh=new Mesh();LayerFixture.Build(asset);
  Resources.Items["BlenderPilot/Vanguard"]=asset;Resources.Items["BlenderPilot/Pilot_Atlas_Standard"]=new Material();Resources.Clips=new[]{"Pilot_Idle","Pilot_Move","Pilot_Basic","Pilot_Hit","Pilot_Skill"}.Select(s=>new AnimationClip{name=s,length=2}).ToArray();BlenderPilotArt.Enabled=true;
  var root=new GameObject("player");var player=root.AddComponent<PlayerController>();var model=new GameObject("model");model.transform.SetParent(root.transform);player.model=model.AddComponent<CombatModel>();player.model.heroClass=HeroClass.Vanguard;player.model.Init();player.Setup();return player;
 }
 static void Frame(PlayerController p,Vector3 walk,Vector3 aim){Time.frameCount++;Time.deltaTime=.02f;Time.time+=.02f;p.attackCooldown=0;p.Aim=p.transform.position+walk*.12f+aim*10;p.FacingFrame(walk,.02f);}
 
 public static void RunLocked(){
  string folder="/workspace/scratch/vanguard-moving-basic-baseline";
  var outputs=new System.Collections.Generic.List<object>();
  foreach(bool stopping in new[]{false,true}){
   var expected=System.Text.Json.JsonDocument.Parse(System.IO.File.ReadAllText(folder+(stopping?"/candidate-stop-samples.json":"/candidate-samples.json"))).RootElement.GetProperty("samples");
   var p=Create();WorldTraversal.Accepted=null;const float dt=1f/120f;int visible=0,recorded=0;float phaseError=0,speedError=0,progressError=0,layerError=0;var contacts=new System.Collections.Generic.List<float>();int identity=p.model.WeaponActionId;
   for(int tick=0;tick<720;tick++){
    Time.frameCount=tick;Time.time=tick*dt;Time.deltaTime=dt;p.attackCooldown-=dt;Input.Held=tick>=120&&tick<576;
    bool walking=!stopping||Time.time<1.4f||Time.time>=2.4f;
    // Controlled coordinate-origin rebasing preserves the exact accepted delta of the locked exporter.
    p.transform.position=Vector3.zero;
    var move=walking?new Vector3(0,0,1):Vector3.zero;p.Aim=p.transform.position+move*(6*dt)+new Vector3(0,0,10);
    p.FacingFrame(move,dt);C(p.model.Visible,"locked fixed-facing recorded frame remains pilot admitted");visible++;C((bool)typeof(CombatModel).GetMethod("PilotMovingBasicFacing",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(p.model,null),"locked raw facing guard itself admits input");
    if(p.model.WeaponActionId!=identity){contacts.Add(Time.time);identity=p.model.WeaponActionId;}
    if(tick%5!=0)continue;recorded++;
    var f=expected[tick/5];float progress=p.model.actionDuration>0?p.model.actionAge/p.model.actionDuration:1;
    phaseError=Math.Max(phaseError,Math.Abs(p.model.locomotion.Phase-f.GetProperty("phase").GetSingle()));speedError=Math.Max(speedError,Math.Abs(p.model.locomotion.Speed-f.GetProperty("speed").GetSingle()));progressError=Math.Max(progressError,Math.Abs(progress-f.GetProperty("progress").GetSingle()));
    var layer=BlenderPilotPosePolicy.Layers(Time.time,2,p.model.locomotion.Phase,p.model.locomotion.Speed,progress);
    layerError=Math.Max(layerError,Math.Max(Math.Max(Math.Abs(layer.IdleTime-f.GetProperty("idle").GetSingle()),Math.Abs(layer.MoveTime-f.GetProperty("move").GetSingle())),Math.Max(Math.Abs(layer.SpeedWeight-f.GetProperty("speedWeight").GetSingle()),Math.Abs(layer.UpperWeight-f.GetProperty("upperWeight").GetSingle()))));
    C(Math.Abs(layer.IdleTime-f.GetProperty("idle").GetSingle())<.000001f&&Math.Abs(layer.MoveTime-f.GetProperty("move").GetSingle())<.000001f&&Math.Abs(layer.SpeedWeight-f.GetProperty("speedWeight").GetSingle())<.000001f&&Math.Abs(layer.UpperWeight-f.GetProperty("upperWeight").GetSingle())<.000001f,"actual locked layer inputs unchanged");
   }
   C(phaseError<.000001f&&speedError<.000001f&&progressError<.000001f,"locked actual walking/action clocks unchanged");C(contacts.Count==9,"locked nine actual visual basic commits");
   outputs.Add(new{scenario=stopping?"stop1.4-restart2.4":"continuous-forward",ticks=720,visible,recorded,phaseError,speedError,progressError,layerError,contacts});
  }
  System.IO.File.WriteAllText("/workspace/scratch/render-equivalence-dcd75ac/trajectory-equivalence.json",System.Text.Json.JsonSerializer.Serialize(outputs,new System.Text.Json.JsonSerializerOptions{WriteIndented=true}));
  Console.WriteLine("PASS: "+n+" locked production Update-tail/FaceAim/BasicAttack-visual/adapter assertions;1440 admitted ticks,288 recorded layer states;Unity/damage dispatch excluded");
 }
public static void Run(){
  foreach(float angle in new[]{90f,180f,-90f}){
   var p=Create();Vector3 forward=new Vector3(0,0,1);for(int i=0;i<20;i++)Frame(p,forward,forward);
   C(p.model.Visible,"steady forward pilot supported");float speed=p.model.locomotion.Speed;int advances=p.model.locomotion.Advances;float expectedPhase=p.model.locomotion.Phase;
   Vector3 turned=Quaternion.Euler(0,angle,0)*forward;
   for(int i=0;i<4;i++){
    Frame(p,forward,turned);C(!p.model.Visible,"current-facing moving basic rejects turn "+angle+" frame "+i);
    expectedPhase=(expectedPhase+.12f*2.6f)%(2*(float)Math.PI);C(Math.Abs(p.model.locomotion.Phase-expectedPhase)<.00002f,"raw facing guard preserves accepted gait phase");
    C(p.model.locomotion.Advances==advances+i+1,"raw facing guard never advances locomotion twice");
    C(p.model.actionAge==p.model.actionDuration*.52f,"turned fallback preserves synchronous contact");
   }
   C(p.model.locomotion.Speed>=speed,"raw facing does not reset smoothed speed");
   for(int i=0;i<40;i++)Frame(p,forward,forward);C(p.model.Visible,"turning back reenters only when existing smoothed gates also permit");
  }
  var ownerBasis=Create();ownerBasis.model.transform.localRotation=Quaternion.Euler(30,65,-20);Frame(ownerBasis,new Vector3(0,0,1),new Vector3(0,0,1));C(ownerBasis.model.Visible,"raw walking basis is Player owner not procedural model pose");
  var stop=Create();var z=new Vector3(0,0,1);for(int i=0;i<20;i++)Frame(stop,z,z);
  for(int i=0;i<4;i++){Frame(stop,Vector3.zero,new Vector3(1,0,0));C(stop.model.Visible&&stop.model.locomotion.Speed>.05f,"legal zero accepted walking preserves decaying gait basic");}
  stop.model.SetLocomotion(Vector3.zero,0,6,true);stop.model.PlayAction(-1,true,.46f);C(stop.model.Visible,"paused legal zero does not invalidate decaying gait basis");
  // Collision accepts zero despite nonzero requested movement; guard uses accepted displacement.
  WorldTraversal.Accepted=Vector3.zero;Frame(stop,z,new Vector3(-1,0,0));C(stop.model.Visible,"blocked actual movement clears previous raw direction");WorldTraversal.Accepted=null;
  var unknown=Create();unknown.model.locomotion.Speed=1;unknown.model.PlayAction(-1,true,.46f);C(!unknown.model.Visible,"unknown walking basis fails closed");
  for(int i=0;i<20;i++)Frame(unknown,z,z);
  unknown.model.SetLocomotion(new Vector3(float.NaN,0,1),.02f,6,true);unknown.model.PlayAction(-1,true,.46f);C(!unknown.model.Visible,"invalid raw displacement fails closed");
  Console.WriteLine("PASS: "+n+" actual Update movement/aim tail + FaceAim + BasicAttack visual-commit facing assertions; damage dispatch and Unity frame loop excluded");
 }
}
