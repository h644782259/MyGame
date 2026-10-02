// Runner compiles unchanged production Enemy/Projectile Update and enemy damage dispatch methods.
// Unity math/rendering and the final damage recipient are explicit managed substitutes.
// No Unity timing, rendering, physics or full PlayerController behavior is claimed.
using System;using System.Collections.Generic;using UnityEngine;using Emberfall;
namespace Emberfall
{
 public class GameSession{public PlayerController Player=new PlayerController();public bool HasStarted=true,Paused,IsDead,InputBlocked,CombatEnded;public bool InDungeon=true;public float ArenaRadius=18;public List<EnemyController> Enemies=new List<EnemyController>();}
 public partial class PlayerController:MonoBehaviour{public float Health=100;public int CombatEpoch=1,DamageCalls;public bool IsDead;public void TakeDamageFrom(float a,string s){DamageCalls++;Health-=a;}public void ApplySlow(float a,float b){}public void NotifyPerfectDodge(){} }
 public enum EnemyKind{Guardian,Goblin,Slime,Wisp}public enum ThreatTier{Normal,Elite}public enum LargeBossPhase{Active,Finished}
 public class BossState{public LargeBossPhase Phase;public bool Interruptible;}public class Boss{public BossState State=new BossState();public int TickCalls;public bool Tick(float dt){TickCalls++;return true;}}
 public class Status{public float MoveMultiplier=1;public void Mark(float a,float b){}}
 public class Control{public void Advance(float dt){}}
 public class Telegraph{public void SetProgress(float f){}public void SetInterruptible(bool v){}}
 public class AdvanceBudget{public bool FallbackActive;public bool Advance(float a,float b,bool c){return false;}}
 public class Route{public Vector3 Direction(Vector3 a,Vector3 b,float radius){return (b-a).normalized;}}
 public static class BossAttackPolicy{public enum Move{Slam}public const float ChargeSpeed=11,ChargeHalfWidth=.85f,ChargeRange=8,EngageRange=10,CloseRange=5,ComboGap=.4f;public static bool PreferredApproach(int a,float b,Move c,int d,bool e){return false;}public static float Recovery(bool b){return 1;}}
 public partial class SummonedCompanion:MonoBehaviour{public bool IsAlive=true;public static SummonedCompanion ThreatTarget(EnemyController e,Vector3 p){return null;}public void TakeDamage(float a,bool areaAttack=false){}public static List<SummonedCompanion> Snapshot(PlayerController p){return new List<SummonedCompanion>();}public static bool HitHostileProjectile(Vector3 a,Vector3 b,float c,float d){return false;}}
 public static partial class PlayerUpgradeRules{public static bool IsInsideArea(float x,float z,float r,bool path){return path&&x*x+z*z<=r*r;}}
 public static partial class CombatFx{public static void Ring(Vector3 p,float r,Color c,float life,float width=.1f){}}
 public partial class CombatProjectile:MonoBehaviour{public static void Hostile(GameSession s,Vector3 p,Vector3 dir,float dmg,float speed,string sourceName){throw new Exception("unexpected bolt spawn");}}
 public partial class EnemyController:MonoBehaviour
 {
  enum AttackType{Melee,Slam,Charge,Bolt,Fan}
  bool escapePost;float escapeChaseMovement;
  GameSession session;public bool IsDead,IsBoss;public EnemyKind Kind=EnemyKind.Guardian;ThreatTier Tier=ThreatTier.Elite;
  Boss largeBoss;Control controlPolicy=new Control();Telegraph telegraph;public Status StatusEffects=new Status();Material healthFillMaterial=new Material();
  AdvanceBudget advanceBudget=new AdvanceBudget();Route route=new Route();
  Vector3 walkingDisplacement,knockVelocity,attackOrigin,sidestepDirection,chargeEnd,origin,targetPoint,attackForward;
  float stunTime,hurtTime,attackAnimation,attackCooldown,chargeTime,flinchUntil,sidestepTime,windup,totalWindup,comboDelay,patrolPhase;
  float speed=2,damage=10;bool aggro,preparing,CanBeSkillInterrupted,chargeHit,dodgePending,activeChargePose,IsEnraged;int arenaBossPattern,repeatedMove,attackNumber,comboRemaining;BossAttackPolicy.Move previousMove;
  AttackType attackType=AttackType.Melee;SummonedCompanion companionTarget;
  public float NavigationRadius=.6f;float ImpactRadius{get{return 1.55f;}}string DisplayName{get{return "guardian";}}
  void CreateWarning(){}void ClearWarning(){}void AnimateModel(float a,float b,bool c){}void ClampPosition(){}void ConfirmChargeDodge(Vector3 a,Vector3 b){}void ConfirmImpactDodge(){}
  void BeginComboAttack(float a,Vector3 b){throw new Exception("unexpected combo");}void BeginAttack(){throw new Exception("unexpected begin");}
  bool CanUseBossAttack(BossAttackPolicy.Move a,Vector3 b){return false;}BossAttackPolicy.Move SelectBossMove(float a,Vector3 b){return BossAttackPolicy.Move.Slam;}
  Vector3 Separation(){return Vector3.zero;}Color ThreatColor(){return new Color();}bool ReturnToEscapePost(float a,float b,Vector3 c){return false;}
  public EnemyController(GameSession game){session=game;preparing=true;windup=.005f;totalWindup=.85f;transform.position=new Vector3(0,0,-1);targetPoint=game.Player.transform.position;attackOrigin=transform.position;}
  public void Tick(){Update();}public float Windup{get{return windup;}}
  public float ChargeRemaining{get{return chargeTime;}}
  public void StartCharge(){preparing=false;IsBoss=true;chargeTime=.2f;transform.position=new Vector3(0,0,-.7f);chargeEnd=new Vector3(0,0,1.5f);}
  public Boss AttachLargeBoss(){preparing=false;return largeBoss=new Boss();}
 }
}
namespace UnityEngine
{
 public class GameObject{public bool activeInHierarchy=true,Destroyed;}
 public class MonoBehaviour{public GameObject gameObject=new GameObject();public Transform transform=new Transform();protected static void Destroy(GameObject g){g.Destroyed=true;}}
 public class Transform{public Vector3 position;public Quaternion rotation;public Vector3 forward=Vector3.forward;}
 public struct Color{public float r,g,b,a;public Color(float r,float g,float b,float a=1){this.r=r;this.g=g;this.b=b;this.a=a;}}
 public class Material{public Color color;}
 public struct Quaternion{public static Quaternion identity{get{return new Quaternion();}}public static Quaternion Euler(float a,float b,float c){return identity;}public static Quaternion LookRotation(Vector3 v){return identity;}public static Quaternion Slerp(Quaternion a,Quaternion b,float t){return a;}public static Quaternion operator *(Quaternion a,Quaternion b){return a;}public static Vector3 operator *(Quaternion q,Vector3 v){return v;}}
 public static partial class Time{public static float deltaTime=.016f;}
 public partial struct Vector3
 {
  public static Vector3 up{get{return new Vector3(0,1,0);}}public static Vector3 forward{get{return new Vector3(0,0,1);}}
  public static Vector3 operator -(Vector3 a){return a*-1;}
  public static Vector3 MoveTowards(Vector3 a,Vector3 b,float max){return a+ClampMagnitude(b-a,max);}
  public static Vector3 Cross(Vector3 a,Vector3 b){return new Vector3(a.y*b.z-a.z*b.y,a.z*b.x-a.x*b.z,a.x*b.y-a.y*b.x);}
  public static Vector3 Slerp(Vector3 a,Vector3 b,float t){return Lerp(a,b,t);}
  public static float Angle(Vector3 a,Vector3 b){return 0;}public static Vector3 RotateTowards(Vector3 a,Vector3 b,float c,float d){return b;}
 }
 public static partial class Mathf{public const float Deg2Rad=.01745329f;public static float Lerp(float a,float b,float t){return a+(b-a)*t;}public static float Clamp01(float f){return Clamp(f,0,1);}}
}
namespace Emberfall
{
 public struct CombatDamage{public float Amount;public bool IsCritical;public float CriticalMultiplier;public CombatDamage(float amount){Amount=amount;IsCritical=false;CriticalMultiplier=1;}}
 public class Volley{public CombatDamage Apply(EnemyController enemy,CombatDamage damage,bool b){return damage;}}
 public class DestructibleProp:MonoBehaviour{public static void FindProjectileHit(PlayerController owner,Vector3 a,Vector3 b,float c,out DestructibleProp prop,out float f){prop=null;f=1;}public void Impact(PlayerController o,int cast,CombatDamage d){}}
 public static class PropImpactGeometry{public static float EntryFraction(params float[] args){throw new Exception("unused prop branch");}}
 public static class LockedImpactMarkPolicy{public static bool ShouldApply(object a,EnemyController b,bool c,float d,float e){return false;}}
 public static class CombatReviewEvents{public static bool Enabled;public static void Emit(params object[] args){}}
 public static class CombatReviewObjectId{public static int Get(object v){return 0;}}
 public static class CombatSight{public static bool Direct(Vector3 a,Vector3 b){return WorldTraversal.HasLineOfSight(a,b);}}
 public partial class CombatProjectile
 {
  GameSession session;PlayerController owner,playerGeneration;int epoch;bool hostile=true;string terminationReason="active",damageSource="enemy bolt";
  float age,lifetime=3,speed=10,radius=.32f,distanceTravelled,impactHeight,launchHeight,aimedDistance=1,dodgeDeadline,impactMarkStrength,explosionRadius;
  EnemyController homingTarget,basicAimTarget;bool basicAttack,arrowShape,bodyHeightFlight,pendingDodge,energyAwarded,pierce;Vector3 direction=Vector3.forward,dodgeOrigin;int dodgeEpoch,castId,skillIndex;
  Color color;CombatDamage damage=new CombatDamage(7),explosionDamage;Volley volley;SummonedCompanion companionSource;object impactMarkTarget;
  HashSet<EnemyController> hitTargets=new HashSet<EnemyController>();
  void AlignBodyFlight(){}
  public CombatProjectile(GameSession game){session=game;owner=playerGeneration=game.Player;epoch=game.Player.CombatEpoch;transform.position=new Vector3(0,0,-.5f);}
  public void Tick(){if(!gameObject.Destroyed)Update();}public void LoseOwner(){hostile=false;owner=null;}public float Age{get{return age;}}public string Reason{get{return terminationReason;}}
 }
}

namespace Emberfall{
public partial class PlayerController {
public Vector3 EnemyBodyPoint(EnemyController e){return e.transform.position;}
public void RegisterSkillHit(int cast){}public float ResolveSkillImpact(EnemyController e,int skill,int cast,float amount,bool crit,float mult){return amount;}
public void OnBasicAttackHitTarget(Vector3 at,EnemyController e,bool b){}public void HitArea(Vector3 at,float radius,CombatDamage d,float a,float b,int cast,Volley volley){}
}
public partial class EnemyController {public float Health=100,ProjectileHitRadius=.6f;public void TakeDamage(float d,Vector3 p,float stun,bool critical=false){Health-=d;}}
public partial class SummonedCompanion{public void OnConfirmedHit(EnemyController e){}}
}

public static class BlockedCombatUpdateProductionTests
{
 static int checks;
 static void Check(bool condition,string message){checks++;if(!condition)throw new Exception(message);}
 public static string Run()
 {
  WorldTraversal.Reset(ZoneKind.Dungeon);Time.deltaTime=.016f;
  var game=new GameSession{InputBlocked=true,Paused=false};var guard=new EnemyController(game);
  float windup=guard.Windup;Vector3 at=guard.transform.position;
  for(int i=0;i<8;i++)guard.Tick();
  Check(guard.Windup==windup&&game.Player.Health==100&&game.Player.DamageCalls==0,"blocked ordinary windup must stay unchanged");
  Check((guard.transform.position-at).sqrMagnitude==0,"blocked ordinary enemy position stays unchanged");
  game.InputBlocked=false;guard.Tick();guard.Tick();
  Check(game.Player.Health==90&&game.Player.DamageCalls==1,"unblocked ordinary windup resolves exactly once");
  game=new GameSession{InputBlocked=true,Paused=false};guard=new EnemyController(game);guard.StartCharge();at=guard.transform.position;
  float charge=guard.ChargeRemaining;guard.Tick();
  Check(guard.ChargeRemaining==charge&&(guard.transform.position-at).sqrMagnitude==0&&game.Player.DamageCalls==0,"blocked Guardian boss charge retains corridor time and position");
  game.InputBlocked=false;guard.Tick();guard.Tick();
  Check(game.Player.DamageCalls==1&&game.Player.Health==86.5f,"unblocked Guardian boss charge hits once");
  game=new GameSession{InputBlocked=true};guard=new EnemyController(game);var large=guard.AttachLargeBoss();guard.Tick();
  Check(large.TickCalls==0,"existing large-boss gate remains blocked");
  game.InputBlocked=false;guard.Tick();Check(large.TickCalls==1,"large-boss delegate resumes once when unblocked");
  large.State.Phase=LargeBossPhase.Finished;guard.Tick();Check(large.TickCalls==1,"large-boss finished guard remains intact");
  game=new GameSession{InputBlocked=true,Paused=false};var projectile=new CombatProjectile(game);at=projectile.transform.position;
  for(int i=0;i<8;i++)projectile.Tick();
  Check(projectile.Age==0&&(projectile.transform.position-at).sqrMagnitude==0&&game.Player.Health==100&&game.Player.DamageCalls==0,"blocked projectile must keep position and lifetime");
  Check(!projectile.gameObject.Destroyed,"blocked current projectile is retained for resumption");
  game.InputBlocked=false;projectile.Tick();projectile.Tick();
  Check(game.Player.Health==93&&game.Player.DamageCalls==1&&projectile.gameObject.Destroyed,"unblocked existing projectile hits once and retires");
  game=new GameSession{InputBlocked=true};projectile=new CombatProjectile(game);game.Player.CombatEpoch++;projectile.Tick();
  Check(projectile.gameObject.Destroyed&&projectile.Reason=="retired"&&projectile.Age==0,"blocked old-epoch projectile still retires before simulation gate");
  game=new GameSession{InputBlocked=true};projectile=new CombatProjectile(game);game.Player=new PlayerController();projectile.Tick();
  Check(projectile.gameObject.Destroyed&&game.Player.DamageCalls==0,"blocked projectile with replaced owner generation retires");
  game=new GameSession{InputBlocked=true};projectile=new CombatProjectile(game);projectile.LoseOwner();projectile.Tick();
  Check(projectile.gameObject.Destroyed&&projectile.Reason=="retired","blocked friendly projectile with lost owner still retires");
  game=new GameSession{InputBlocked=true};projectile=new CombatProjectile(game);game.IsDead=true;projectile.Tick();
  Check(projectile.gameObject.Destroyed&&game.Player.DamageCalls==0,"blocked projectile still retires on death");
  return "PASS: "+checks+" actual Enemy/Projectile Update pause-boundary checks (managed substitutes, not Unity execution)";
 }
}
