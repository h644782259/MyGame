using System;
using System.Collections.Generic;
using System.Linq;
namespace UnityEngine
{
 public class MonoBehaviour:Component { public static void Destroy(GameObject go){GameObject.Roots.Remove(go);} }
 public class Component { public GameObject gameObject;public Transform transform{get{return gameObject.transform;}} public T GetComponent<T>() where T:class{return gameObject.GetComponent<T>();} }
 public class Transform {public Vector3 position;}
 public class GameObject
 {
  public static List<GameObject> Roots=new List<GameObject>();readonly List<object> components=new List<object>();public bool activeSelf=true;public string name;public Transform transform=new Transform();public Scene scene=new Scene();
  public GameObject(string n){name=n;Roots.Add(this);}public void SetActive(bool value){activeSelf=value;}
  public T AddComponent<T>() where T:Component,new(){var c=new T{gameObject=this};components.Add(c);return c;}
  public T GetComponent<T>() where T:class{return components.OfType<T>().FirstOrDefault();}public T GetComponentInChildren<T>() where T:class{return GetComponent<T>();}
 }
 public class Scene{public static bool FailSnapshot;public GameObject[] GetRootGameObjects(){if(FailSnapshot)throw new Exception("snapshot failure");return GameObject.Roots.ToArray();}}
 public class Camera:Component{}public class Light:Component{}
 public struct Vector3 {public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}public float sqrMagnitude{get{return x*x+y*y+z*z;}}public static Vector3 forward{get{return new Vector3(0,0,1);}}public static Vector3 operator *(Vector3 a,float b){return new Vector3(a.x*b,a.y*b,a.z*b);}public static Vector3 operator +(Vector3 a,Vector3 b){return new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);}public static Vector3 operator -(Vector3 a,Vector3 b){return new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);}public static Vector3 ClampMagnitude(Vector3 a,float b){return a;}}
 public static class Random{public struct State{public int value;}public static State state;public static void InitState(int n){state=new State{value=n};}}
 public static class Mathf{public static float Sin(float n){return(float)Math.Sin(n);}}
 public static class Time{public static float deltaTime;}
 public enum KeyCode{H}public static class Input{public static bool GetKeyDown(KeyCode code){return false;}}
 public static class Debug{public static void LogException(Exception e){}}
 public static class JsonUtility{public static string ToJson(object o,bool p){return "snapshot";}}
}
namespace Emberfall
{
 using UnityEngine;
 public enum HeroClass{Vanguard}public enum EnemyKind{Guardian,Wisp}
 public class GameProfile{public HeroClass heroClass;public int level=10;}
 public class ProgressionService
 {public string PracticeConfigurationSummary(){return "Human summary";}public bool IsPracticeOnly;public GameProfile Profile=new GameProfile();public ProgressionService CreatePracticeCopy(){return new ProgressionService{IsPracticeOnly=true};}public class BuildDraft{public bool valid=true;public ProgressionService CreatePracticeCopy(){return valid?new ProgressionService{IsPracticeOnly=true}:null;}}}
 public class GameUI{public void EnterPracticePanel(){}public void LeavePracticePanel(){}}
 public class SkillChargeController:Component{public bool IsCharging;public void Initialize(PlayerController p,GameSession s){}}
 public class SkillTargetingController:Component{public void Initialize(PlayerController p,GameSession s){}}
 public class SkillRuntime{public HeroClass HeroClass;public Action<float> EnergyChanged;public SkillRuntime(HeroClass h){HeroClass=h;}}
 public static class GameBalance{public static string ClassName(HeroClass h){return "Hero";}}
 public class CombatModel:Component{public static CombatModel Hero(Transform t,HeroClass h){if(PlayerController.ThrowOnInitialize)throw new Exception("injected model creation failure");return new GameObject("model").AddComponent<CombatModel>();}}
 public partial class PlayerController:MonoBehaviour{public static bool ThrowOnInitialize;public float Health=37,Energy=23,Cooldown=8;GameSession session;GameUI inputUI;SkillRuntime skillRuntime;public HeroClass HeroClass;CombatModel model;SkillTargetingController targeting;SkillChargeController charge;Vector3 aimPoint;void RefreshStats(bool heal){Health=100;Energy=100;Cooldown=0;}public void Teleport(Vector3 p){transform.position=p;}}

 public class EnemyStatusEffects{public bool IsFrozen,KnockedDown,IsAirborne;public float MoveMultiplier=1;}
 public class EnemyController:Component{public EnemyStatusEffects StatusEffects;public bool IsStunned,IsDead;public float NavigationRadius=.5f;public void ConfigurePracticeTarget(){}}
 public static class WorldTraversal{public static Vector3 Move(Vector3 a,Vector3 b,float radius){return new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);}public static bool HasLineOfSight(Vector3 a,Vector3 b){return true;}}
 public sealed partial class GameSession:MonoBehaviour
 {
  public ProgressionService Progression=new ProgressionService();public PlayerController Player;public List<EnemyController> Enemies=new List<EnemyController>();
  public bool IsInCamp{get{return !PracticeActive;}}public bool IsDead,Paused,uiBlocking=true;public bool InputBlocked{get{return Paused||uiBlocking;}}public GameObject world;public GameUI ui;public void Notify(string s){}void UpdateTimeScale(){}
  void SpawnEnemy(EnemyKind kind,int level,Vector3 point,bool boss){var go=new GameObject("enemy");go.transform.position=point;Enemies.Add(go.AddComponent<EnemyController>());}
  public void TestTick(){TickPractice();}
 }
}
public static class CampPracticeSessionTests
{
 static int n;static void Check(bool value,string text){n++;if(!value)throw new Exception(text);}
 public static string Run()
 {
  UnityEngine.GameObject.Roots.Clear();var root=new UnityEngine.GameObject("session");var s=root.AddComponent<Emberfall.GameSession>();s.world=new UnityEngine.GameObject("world");s.Player=new UnityEngine.GameObject("original").AddComponent<Emberfall.PlayerController>();var original=s.Player;var owner=s.Progression;var enemies=s.Enemies;var draft=new Emberfall.ProgressionService.BuildDraft();UnityEngine.Random.InitState(42);
  foreach(var scene in new[]{Emberfall.CampPracticeScenario.Stationary,Emberfall.CampPracticeScenario.Moving,Emberfall.CampPracticeScenario.FrontAndSupplier})
  {
   Check(s.BeginPractice(scene,10,draft),"real BeginPractice accepts legal scene");Check(s.PracticeActive&&s.Player!=original&&s.Progression!=owner&&!original.gameObject.activeSelf,"real ownership handoff");Check(!s.BeginPractice(scene,10,draft),"duplicate start rejected");Check(s.Enemies.Count==(scene==Emberfall.CampPracticeScenario.FrontAndSupplier?2:1),"scene target cardinality");
   if(s.Enemies.Count==2)Check(s.PracticeSupportMultiplier(s.Enemies[0])==.7f&&s.PracticeSupportMultiplier(s.Enemies[1])==1,"actual support routing");
   s.RecordPracticeCast(1,0);s.RecordPracticeSkillHit(1);s.RecordPracticeEnergy(-15);Check(s.RestartPractice()&&s.PracticeRecord.ActualDamage==0&&s.PracticeRecord.EnergySpent==0,"refresh only temporary actors and new record");
   UnityEngine.Time.deltaTime=10;s.TestTick();Check(!s.PracticeActive&&s.Player==original&&s.Progression==owner&&s.Enemies==enemies,"timed finish restores exact references");Check(original.Health==37&&original.Energy==23&&original.Cooldown==8&&original.gameObject.activeSelf,"no original vitals or cooldown refresh");Check(UnityEngine.Random.state.value==42,"random state restored");s.EndPractice("duplicate");
  }
  int roots=UnityEngine.GameObject.Roots.Count;UnityEngine.Scene.FailSnapshot=true;Check(!s.BeginPractice(Emberfall.CampPracticeScenario.Stationary,10)&&!s.PracticeActive&&s.Player==original&&UnityEngine.GameObject.Roots.Count==roots&&original.gameObject.activeSelf,"snapshot failure cannot retire original roots");UnityEngine.Scene.FailSnapshot=false;
  Emberfall.PlayerController.ThrowOnInitialize=true;Check(!s.BeginPractice(Emberfall.CampPracticeScenario.Moving,10,draft)&&s.Player==original&&s.Progression==owner&&!s.PracticeActive&&original.gameObject.activeSelf,"injected creation failure rolls ownership back");Emberfall.PlayerController.ThrowOnInitialize=false;
  draft.valid=false;Check(!s.BeginPractice(Emberfall.CampPracticeScenario.Moving,10,draft)&&s.Player==original,"stale draft cannot start");
  Check(s.BeginPractice(Emberfall.CampPracticeScenario.Moving,60),"current build sixty second scenario");s.EndPractice("death");Check(s.Player==original&&s.PracticeRecord.EndReason=="death","early exit restores original");
  return "PASS "+n+" actual GameSession.Practice lifecycle assertions (Unity boundary doubles, no engine execution)";
 }
}
