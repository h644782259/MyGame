// Managed scene doubles only. The companion script compiles the actual Chapter host,
// ChangeZone/SaveBeforeLeaving/OnEnemyKilled and real progression/filesystem transactions.
using System;using System.Collections;using System.Collections.Generic;using System.IO;using Emberfall;using UnityEngine;
namespace UnityEngine
{
 public class Object {public static void Destroy(Object o){if(o is GameObject g)g.SetActive(false);}}
 public class MonoBehaviour:Object {public GameObject gameObject;public Transform transform=>gameObject.transform;public MonoBehaviour(){gameObject=new GameObject();}}
 public class GameObject:Object {public string name;public bool activeInHierarchy=true;public readonly Transform transform;readonly Dictionary<Type,object> components=new Dictionary<Type,object>();public GameObject(string n=""){name=n;transform=new Transform{gameObject=this};}public void SetActive(bool a){activeInHierarchy=a;}public T AddComponent<T>()where T:new(){var v=new T();if(v is MonoBehaviour m)m.gameObject=this;components[typeof(T)]=v;return v;}public T GetComponent<T>()where T:new(){return components.TryGetValue(typeof(T),out var v)?(T)v:new T();}}
 public class Transform {public Vector3 position;public GameObject gameObject;public void SetParent(Transform p,bool world){}}
 public struct Vector3 {public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}public static Vector3 up=>new Vector3(0,1,0);public float sqrMagnitude=>x*x+y*y+z*z;public static float Distance(Vector3 a,Vector3 b)=>(float)Math.Sqrt((a-b).sqrMagnitude);public static Vector3 operator-(Vector3 a,Vector3 b)=>new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);public static Vector3 operator+(Vector3 a,Vector3 b)=>new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);public static Vector3 operator*(Vector3 a,float b)=>new Vector3(a.x*b,a.y*b,a.z*b);}
 public static class Mathf {public static int Min(int a,int b)=>Math.Min(a,b);public static float Max(float a,float b)=>Math.Max(a,b);public static int Max(int a,int b)=>Math.Max(a,b);public static int Clamp(int v,int a,int b)=>Math.Max(a,Math.Min(b,v));public static float Clamp(float v,float a,float b)=>Math.Max(a,Math.Min(b,v));public static int RoundToInt(float v)=>(int)Math.Round(v);}
 public static class Random {public static int Range(int a,int b)=>a+1;public static float value=>.9f;}
 public static class Time {public static float deltaTime=.25f,time;}
 public class Camera:MonoBehaviour {public static Camera main=new Camera();public T GetComponent<T>()where T:new()=>new T();}
}
namespace Emberfall
{
 public sealed class PlayerController:MonoBehaviour {public int CombatEpoch=1,CooldownResets,Retirements;public float Health=100,MaxHealth=100;public void RetireCombatForWorldTransition(){CombatEpoch++;Retirements++;}public void Teleport(Vector3 p){transform.position=p;}public void RefreshStats(bool full){}public void ResetCooldownsForDungeonEntry(){CooldownResets++;}public void Heal(float n){Health=Math.Min(MaxHealth,Health+n);}}
 public sealed partial class EnemyController:MonoBehaviour {public bool IsBoss,IsDead;public bool isActiveAndEnabled=>gameObject.activeInHierarchy;public EnemyKind Kind;public float NavigationRadius=>IsBoss?1.3f:.65f;public int PostCalls;public void Initialize(GameSession s,EnemyKind k,int l,bool b){Kind=k;IsBoss=b;ApplySpawnStats(s,l,b);}public void ConfigureEscapePost(object role,Vector3 p){PostCalls++;}public void BeginDeath(){IsDead=true;}}
 public static class EscapeRoomFormation {public static object Role(int i)=>i;}
 public sealed class ChapterRoomPlan {public int Layout=100;public Vector3 Entrance=new Vector3(0,0,-12),Exit=new Vector3(0,0,14);public Vector3[] Objectives={new Vector3(-8,0,-5),new Vector3(8,0,7)};}
 public static class ChapterRoomGeometry {public static ChapterRoomPlan Plan(ChapterNode n,int r,int s)=>new ChapterRoomPlan{Layout=100+(int)n*2+r};public static bool TrySpawn(ChapterRoomPlan p,int i,List<Vector3> occupied,float radius,out Vector3 point){point=new Vector3(i*2,0,6);return !WorldTraversal.SpawnBlocked;}}
 public static class WorldTraversal {public static bool SpawnBlocked,Occluded;public static int Rays;public static Vector3 NearestWalkable(Vector3 p,float r)=>p;public static bool CanReach(Vector3 a,Vector3 b,float r)=>true;public static bool HasLineOfSight(Vector3 a,Vector3 b){Rays++;return !Occluded;}}
 public static class WorldBuilder {public static int Builds;public static GameObject Build(ZoneKind z,int layout=0,int tier=0,int hub=0,int chapterSeed=0){Builds++;return new GameObject("World");}public static GameObject MakeLootBeacon(Vector3 p,Color c)=>new GameObject();public static GameObject MakeRoomObjective(Vector3 p)=>new GameObject();public static void ApplyChapterLandmark(GameObject w,int mask){}}
 public static class TacticalRoomGeometry {public static Vector3 Entrance=>new Vector3(0,0,-12);}
 public static class CombatFx {public static Vector3 Flat(Vector3 p)=>new Vector3(p.x,0,p.z);public static void Ring(Vector3 p,float r,Color c,float a,float b){}}
 public static class LargeExpeditionBoss {public static int Configures;public static void ConfigureChapter(EnemyController e,int t,int s,ChapterDifficulty d){Configures++;}}
 public static class ChapterHazards {public static void Configure(GameSession g,ChapterNode n,ChapterDifficulty d,int r,int s,ChapterRoomPlan p){}}
 public class AdventureCamera {public void Snap(){}}
 public enum SoundCue {Victory}public static class GameAudio {public static void Play(SoundCue s){}}
 public class FakeChoices {public void Reset(){}}
 public sealed partial class GameSession:MonoBehaviour
 {
  public ProgressionService Progression;public PlayerController Player=new PlayerController();public bool HasStarted=true,InDungeon,IsDead,Paused,BackgroundPaused,IsInCamp=true;
  public bool InputBlocked=>Paused||BackgroundPaused||IsDead||ChapterFinished;public bool CombatEnded=>ChapterFinished||DungeonCleared;
  public int CurrentHub,DungeonTier=1,DungeonWave,DungeonLayout,DungeonEntryLevel=2,SelectedDungeonTier=1,HealingCharges;public int MaximumDungeonTier=>100;
  public bool DungeonCleared,ChallengeRun;public string LastRunSummary,Notice;public List<EnemyController> Enemies=new List<EnemyController>();
  object ModeRun,RoomChainRun;bool loadingSaveSnapshot,changingZone;object waveRoutine;GameObject world=new GameObject("Camp");readonly List<GameObject> transientObjects=new List<GameObject>();
  int runSeed,wavePopulation,recapGoldLost;float nextReinforcementAt,lastDamageAmount,lastInterruptAt,respawnTimer;string lastDamageSource;bool objectiveHealedThisWave,DungeonSelectionOpen;Queue<object> reinforcementQueue=new Queue<object>();Dictionary<string,int> combatActions=new Dictionary<string,int>();FakeChoices RunChoices=new FakeChoices();
  public int OldWaveCalls,OldBuildCalls;public bool DungeonRewardPending=>false;public bool ModeRewardPending=>ChapterRewardPending;public bool WorldAlive=>world.activeInHierarchy;
  public void Notify(string s){Notice=s;}void SuspendInputs(){}void UpdateTimeScale(){}void AbandonSideEvent(){}void RetireWorldLootReceipts(int epoch){}string BuildRunSummary(bool success)=>"summary";
  void ClearDungeonSettlement(){}void ResetArenaMode(bool dungeon){ModeRun=RoomChainRun=null;}bool TrySettleSideEventRewards()=>true;bool TrySettleDungeonReward()=>true;bool TrySettleArenaReward()=>TrySettleChapterReward();bool PreserveWorldLoot()=>true;
  void StopCoroutine(object routine){}void BeginRoomChainScene(){OldBuildCalls++;}void BeginArenaScene(){OldBuildCalls++;}void SpawnDungeonWave(){OldBuildCalls++;}void BuildSideEvent(){}void SpawnWildernessEnemy(){}
  void RecordArenaDefeat(EnemyController e){}void RecordRoomDefeat(EnemyController e){}void OnExpeditionEnemyKilled(EnemyController e){}public void LogSystem(string s){}void SpawnFloatingText(Vector3 p,string s,Color c){}void DeliverEnemyLoot(ItemData item,Vector3 p){}void FinalizeRoomChain(){}void FinalizeArenaResult(){}void TrySpawnReinforcements(){}object StartCoroutine(IEnumerator x){OldWaveCalls++;return x;}IEnumerator NextWave(){yield break;}
  public void Tick()=>TickChapterRun();public void Camp(){ChangeZone(false);}public void FailForTest(){FailChapter("dead");}public ChapterRunReceipt Receipt=>chapterReceipt;
  public void FixtureUnlock(){Progression.Profile.chapterCompletedMask=7;Progression.Profile.chapterHighestDifficulties=new[]{3,3,3};Progression.Save();}
 }
}
public static class ChapterHostProductionTests
{
 static int checks;static void Check(bool v,string text){checks++;if(!v)throw new Exception(text);}
 static GameSession Fresh(string path){var s=new GameSession{Progression=new ProgressionService(path)};Check(s.Progression.CreateNewSlot(HeroClass.Vanguard),"fresh fixture save");return s;}
 static void Capture(GameSession s){s.Player.transform.position=s.ChapterRun.Seals==0?new Vector3(-8,0,-5):new Vector3(8,0,7);for(int i=0;i<16;i++)s.Tick();}
 static void Exit(GameSession s){s.Player.transform.position=new Vector3(0,0,14);Check(s.EnterNextChapterRoom(),"real chapter exit succeeds");}
 public static string Run(string folder)
 {
  checks=0;var s=Fresh(Path.Combine(folder,"entry"));var oldWorldBuilds=WorldBuilder.Builds;int oldEpoch=s.Player.CombatEpoch;
  s.SelectedChapterDifficulty=ChapterDifficulty.Hard;Check(!s.ConfirmChapterEnter()&&!s.InDungeon&&s.WorldAlive&&s.Player.CombatEpoch==oldEpoch,"locked difficulty cannot destroy camp");s.SelectedChapterDifficulty=ChapterDifficulty.Normal;
  s.Progression.Profile.gold++;Directory.CreateDirectory(s.Progression.SaveFilePath+".tmp");Check(!s.ConfirmChapterEnter()&&!s.ChapterActive&&WorldBuilder.Builds==oldWorldBuilds&&s.Player.CombatEpoch==oldEpoch,"entry save failure leaves old world and epoch intact");Directory.Delete(s.Progression.SaveFilePath+".tmp");
  Check(s.ConfirmChapterEnter()&&s.Enemies.Count==6&&s.OldBuildCalls==0&&s.Player.CooldownResets==1,"chapter enters six-enemy host without old mode spawning");var first=s.ChapterRun;var stale=s.Enemies[0];Check(s.ChapterSupportMultiplier(s.Enemies[1])==1,"normal forest never grants support");Check(Math.Abs(stale.MaxHealth-CombatBalance.EnemyHealth(s.DungeonEntryLevel,1,false,EnemyKind.Wisp))<.001f,"normal chapter stats retain tier baseline");
  WorldTraversal.Rays=0;s.Tick();Check(WorldTraversal.Rays==0,"far capture contestants skip LOS query");
  foreach(var enemy in s.Enemies)enemy.transform.position=new Vector3(0,0,-8);
  Capture(s);Check(s.ChapterRun.Seals==1&&!s.ChapterRun.DoorUnlocked,"first seal alone cannot exit");Capture(s);Check(s.ChapterRun.DoorUnlocked,"two actual host captures open first exit");
  s.Player.transform.position=new Vector3(0,0,14);oldEpoch=s.Player.CombatEpoch;s.Progression.Profile.gold++;Directory.CreateDirectory(s.Progression.SaveFilePath+".tmp");Check(!s.EnterNextChapterRoom()&&s.ChapterRoomIndex==0&&s.Player.CombatEpoch==oldEpoch&&stale.gameObject.activeInHierarchy,"room save failure preserves living old room");Directory.Delete(s.Progression.SaveFilePath+".tmp");
  Exit(s);Check(s.ChapterRoomIndex==1&&s.Enemies.Count==6&&s.Player.CombatEpoch==oldEpoch+1&&s.Player.CooldownResets==1&&!stale.gameObject.activeInHierarchy,"second room retires epoch/enemies while retaining cooldowns");int gold=s.Progression.Profile.gold;s.OnEnemyKilled(stale);Check(s.Progression.Profile.gold==gold&&!s.ChapterRun.DoorUnlocked,"stale previous-room kill cannot award or advance new room");
  foreach(var enemy in new List<EnemyController>(s.Enemies))s.OnEnemyKilled(enemy);Check(s.OldWaveCalls==0&&!s.ChapterFinished,"chapter clear never schedules legacy NextWave or skips capture");Capture(s);Check(s.ChapterRun.DoorUnlocked&&!s.ChapterFinished,"second capture still requires actual exit");
  s.Player.transform.position=new Vector3(0,0,14);s.Progression.Profile.gold++;s.Progression.Save();
  Exit(s);Check(s.ChapterFinished&&!s.ChapterRewardPending&&s.Progression.Profile.chapterCompletedMask==1&&s.Progression.Profile.highestAdventureTier==0,"forest durable node completion changes story only");int paid=s.Progression.Profile.mechanicMaterials;Check(s.TrySettleChapterReward()&&s.Progression.Profile.mechanicMaterials==paid&&!s.EnterNextChapterRoom(),"repeat finish/exit cannot double pay");
  s.Camp();Check(!s.ChapterActive&&!s.InDungeon,"return to camp clears chapter state");
  s.FixtureUnlock();s.SelectedChapterNode=ChapterNode.Redrock;s.SelectedChapterDifficulty=ChapterDifficulty.Hard;Check(s.ConfirmChapterEnter(),"hard redrock entry");Check(s.Enemies.TrueForAll(e=>e.PostCalls==1),"hard redrock configures existing guard roles once");Check(Math.Abs(s.Enemies[0].MaxHealth-CombatBalance.EnemyHealth(s.DungeonEntryLevel,1,false,EnemyKind.Wisp)*1.2f)<.001f&&Math.Abs(s.Enemies[0].damage-CombatBalance.EnemyDamage(s.DungeonEntryLevel,1,false)*1.15f)<.001f,"chapter difficulty multiplies already tier-scaled stats exactly once");s.OnEnemyKilled(s.Enemies[0]);Check(s.ChapterRun.DoorUnlocked&&s.Enemies.Count==5,"hunt prioritizes designated target rather than all enemies");Exit(s);s.FailForTest();var abandoned=s.Receipt;s.Camp();Check(!s.Progression.TryCompleteChapterNode(abandoned),"failed and abandoned attempt receipt is invalid");
  s.SelectedChapterNode=ChapterNode.StarPlatform;s.SelectedChapterDifficulty=ChapterDifficulty.Normal;Check(s.ConfirmChapterEnter()&&s.Enemies.Count==0&&s.NearChapterExit==false&&s.ChapterRun.DoorUnlocked,"star rest starts safe and requires reaching exit");Exit(s);Check(s.Enemies.Count==3&&LargeExpeditionBoss.Configures==1,"star creates one anchor boss plus two guards");
  s.OnEnemyKilled(s.Enemies[0]);Check(!s.ChapterFinished,"boss kill alone does not ignore guards");s.OnEnemyKilled(s.Enemies[0]);
  // Kill callback's normal enemy save is allowed to fail; chapter receipt must remain retryable.
  Directory.CreateDirectory(s.Progression.SaveFilePath+".tmp");s.OnEnemyKilled(s.Enemies[0]);Check(s.ChapterFinished&&s.ChapterRewardPending&&s.Progression.Profile.highestAdventureTier==0,"real completion save failure publishes no shared tier and remains pending");Directory.Delete(s.Progression.SaveFilePath+".tmp");Check(s.TrySettleChapterReward()&&!s.ChapterRewardPending&&s.Progression.Profile.highestAdventureTier==1,"same terminal receipt can retry durable star tier completion");Check(!s.Progression.Profile.pendingFirstClearReward&&!s.Progression.Profile.pendingFashionChest,"chapter never creates old first-core or relic chest");
  s.Camp();s.SelectedChapterNode=ChapterNode.ForestCourt;s.SelectedChapterDifficulty=ChapterDifficulty.Hard;Check(s.ConfirmChapterEnter(),"hard forest entry");var supplier=s.Enemies[0];var friend=s.Enemies[1];friend.transform.position=supplier.transform.position+new Vector3(1,0,0);Check(s.ChapterSupportMultiplier(friend)==.7f&&s.ChapterSupportMultiplier(supplier)==1,"hard forest gives actual support to others only");WorldTraversal.Occluded=true;Check(s.ChapterSupportMultiplier(friend)==1,"wall severs chapter support");WorldTraversal.Occluded=false;friend.transform.position=new Vector3(99,0,99);WorldTraversal.Rays=0;Check(s.ChapterSupportMultiplier(friend)==1&&WorldTraversal.Rays==0,"distant support skips LOS query");s.Camp();
  WorldTraversal.SpawnBlocked=true;Check(s.ConfirmChapterEnter()&&s.ChapterFinished&&s.ChapterRun.Failed,"unavailable spawn fails safely without legacy fallback enemies");WorldTraversal.SpawnBlocked=false;s.Camp();
  var state=new ChapterCombatRun(ChapterNode.ForestCourt,ChapterDifficulty.Normal,42);Check(!state.Register(0,-1,0),"unbound room cannot accept spawn");state.BindRoom(7);
  for(int i=0;i<6;i++)Check(state.Register(0,7,i)&&!state.Register(0,7,i),"registration is bounded and idempotent");
  Check(!state.Register(0,7,6)&&!state.Defeat(0,6,0)&&!state.Defeat(1,7,0),"cap and stale epoch/room rejected");
  state.Advance(200,true,true,false);Check(state.Progress==.25f,"large frame cannot skip objective interaction");state.Advance(float.NaN,true,true,false);state.Advance(1,false,true,false);state.Advance(1,true,true,true);Check(state.Progress==.25f,"pause contest invalid delta retain progress");
  Check(!state.Exit(true,false)&&!state.FinishBoss(),"no early room exit or boss completion");state.Fail();Check(!state.Defeat(0,7,0)&&!state.Exit(true,false),"failed state rejects late callbacks");
  s.SelectedChapterDifficulty=ChapterDifficulty.Heroic;Check(s.ConfirmChapterEnter(),"heroic fixture entry");Check(Math.Abs(s.Enemies[0].MaxHealth-CombatBalance.EnemyHealth(s.DungeonEntryLevel,1,false,EnemyKind.Wisp)*1.35f)<.001f&&Math.Abs(s.Enemies[0].damage-CombatBalance.EnemyDamage(s.DungeonEntryLevel,1,false)*1.25f)<.001f,"heroic difficulty uses stated stat multipliers");s.Camp();
  return "PASS: "+checks+" actual chapter host/save/transition/kill assertions (managed scene doubles, no Unity gameplay)";
 }
}
