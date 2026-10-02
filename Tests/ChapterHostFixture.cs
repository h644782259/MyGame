// Managed scene doubles only. The companion script compiles the actual Chapter host,
// ChangeZone/SaveBeforeLeaving/OnEnemyKilled and real progression/filesystem transactions.
using System;using System.Collections;using System.Collections.Generic;using System.IO;using Emberfall;using UnityEngine;
namespace UnityEngine
{
 public class Object {public static void Destroy(Object o){if(o is GameObject g)g.SetActive(false);}}
 public class MonoBehaviour:Object {public GameObject gameObject;public Transform transform=>gameObject.transform;public MonoBehaviour(){gameObject=new GameObject();}}
 public class GameObject:Object {public string name;public bool activeInHierarchy=true;public readonly Transform transform;readonly Dictionary<Type,object> components=new Dictionary<Type,object>();public GameObject(string n=""){name=n;transform=new Transform{gameObject=this};}public void SetActive(bool a){activeInHierarchy=a;}public T AddComponent<T>()where T:new(){var v=new T();if(v is MonoBehaviour m)m.gameObject=this;components[typeof(T)]=v;return v;}public T GetComponent<T>()where T:new(){return components.TryGetValue(typeof(T),out var v)?(T)v:new T();}}
 public class Transform {public Vector3 position;public GameObject gameObject;public void SetParent(Transform p,bool world){}}
 public struct Vector3 {public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}public static Vector3 up=>new Vector3(0,1,0);public static Vector3 zero=>new Vector3();public float magnitude=>(float)Math.Sqrt(sqrMagnitude);public float sqrMagnitude=>x*x+y*y+z*z;public static float Distance(Vector3 a,Vector3 b)=>(float)Math.Sqrt((a-b).sqrMagnitude);public static Vector3 operator-(Vector3 a,Vector3 b)=>new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);public static Vector3 operator+(Vector3 a,Vector3 b)=>new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);public static Vector3 operator*(Vector3 a,float b)=>new Vector3(a.x*b,a.y*b,a.z*b);}
 public struct Vector2 {public float x,y;public Vector2(float x,float y){this.x=x;this.y=y;}}
 public static class Mathf {public static int FloorToInt(float value)=>(int)Math.Floor(value);public const float PI=(float)Math.PI;public static float Sin(float v)=>(float)Math.Sin(v);public static float Cos(float v)=>(float)Math.Cos(v);public static int Min(int a,int b)=>Math.Min(a,b);public static float Max(float a,float b)=>Math.Max(a,b);public static int Max(int a,int b)=>Math.Max(a,b);public static int Clamp(int v,int a,int b)=>Math.Max(a,Math.Min(b,v));public static float Clamp(float v,float a,float b)=>Math.Max(a,Math.Min(b,v));public static int RoundToInt(float v)=>(int)Math.Round(v);}
 public static class Random {public static int Range(int a,int b)=>a+1;public static float value=>.9f;}
 public static class Time {public static int frameCount;public static float deltaTime=.25f,time;}
 public class Camera:MonoBehaviour {public static Camera main=new Camera();public T GetComponent<T>()where T:new()=>new T();}
}
namespace Emberfall
{
 public sealed class PlayerController:MonoBehaviour {public int CombatEpoch=1,CooldownResets,Retirements;public float Health=100,MaxHealth=100;public void RetireCombatForWorldTransition(){CombatEpoch++;Retirements++;}public void Teleport(Vector3 p){transform.position=p;}public void RefreshStats(bool full){}public void ResetCooldownsForDungeonEntry(){CooldownResets++;}public void Heal(float n){Health=Math.Min(MaxHealth,Health+n);}}
 public sealed partial class EnemyController:MonoBehaviour {public bool IsBoss,IsDead;public EscapeRole PostRole;public bool isActiveAndEnabled=>gameObject.activeInHierarchy;public EnemyKind Kind;public float NavigationRadius=>IsBoss?1.3f:.65f;public int PostCalls;public void Initialize(GameSession s,EnemyKind k,int l,bool b){Kind=k;IsBoss=b;ApplySpawnStats(s,l,b);}public void ConfigureEscapePost(EscapeRole role,Vector3 p){PostRole=role;PostCalls++;}public void ConfigureMobileSupport(EnemyController supplier,EnemyController first,EnemyController second){}public void BeginDeath(){IsDead=true;}}
 public static class WorldTraversal {public static bool SpawnBlocked,Occluded;public static int Rays;public static void AddCircle(Vector3 p,float r){}public static void AddBox(Vector3 p,Vector2 s){}public static bool IsWalkable(Vector3 p,float r)=>!SpawnBlocked;public static Vector3 NearestWalkable(Vector3 p,float r)=>p;public static bool CanReach(Vector3 a,Vector3 b,float r)=>true;public static bool HasLineOfSight(Vector3 a,Vector3 b){Rays++;return !Occluded;}}
 public static class WorldBuilder {public static int Builds;public static GameObject Build(ZoneKind z,int layout=0,int tier=0,int hub=0,int chapterSeed=0){Builds++;return new GameObject("World");}public static GameObject MakeLootBeacon(Vector3 p,Color c)=>new GameObject();public static GameObject MakeRoomObjective(Vector3 p,bool chapterSeal=false){var g=new GameObject();g.transform.position=p;return g;}public static void ApplyChapterLandmark(GameObject w,int mask){}}
 // Visual components are separately executed by TacticalLiveVisualTests; host keeps scene boundary doubles.
 public static class LargeBossShutdownVisual {public static GameSession Owner;public static bool Active;public static bool IsPresenting(GameSession s)=>Active&&ReferenceEquals(Owner,s);public static void Skip(GameSession s){if(ReferenceEquals(Owner,s))Active=false;}}
 public static class TacticalCaptureVisual {public static void AttachChapter(GameObject o,GameSession s,int index){}public static void Attach(GameObject objective,GameSession session){}}
 public static class TacticalEnemyVisual {public static void Attach(EnemyController enemy,GameSession session){}}
 public static class TacticalRoomGeometry {public static Vector3 Entrance=>new Vector3(0,0,-12);}
 public static class CombatFx {public static Vector3 Flat(Vector3 p)=>new Vector3(p.x,0,p.z);public static void Ring(Vector3 p,float r,Color c,float a,float b){}}
 public static class LargeExpeditionBoss {public static int Configures;public static void ConfigureChapter(EnemyController e,int t,int s,ChapterDifficulty d){Configures++;}}
 public static class ChapterHazards {public static void Configure(GameSession g,ChapterNode n,ChapterDifficulty d,int r,int s,ChapterRoomPlan p){}}
 public class AdventureCamera {public void Snap(){}}
 public enum SoundCue {Victory,Death}public static class GameAudio {public static void Play(SoundCue s){}}
 public static class MobileControls {public static bool Active;}
 public class FakeChoices {public void Reset(){}public void Cancel(){}}
 public sealed partial class GameSession:MonoBehaviour
 {
  public ProgressionService Progression;public PlayerController Player=new PlayerController();public bool HasStarted=true,InDungeon,IsDead,Paused,BackgroundPaused,IsInCamp=true;
  public bool InputBlocked=>Paused||BackgroundPaused||IsDead||ChapterFinished;public bool CombatEnded=>ChapterFinished||DungeonCleared;
  public int CurrentHub,DungeonTier=1,DungeonWave,DungeonLayout,DungeonEntryLevel=2,SelectedDungeonTier=1,HealingCharges;public int MaximumDungeonTier=>100;
  public bool DungeonCleared,ChallengeRun;public string LastRunSummary,Notice;public List<EnemyController> Enemies=new List<EnemyController>();
  ExpeditionModeState ModeRun;RoomChainState RoomChainRun;bool loadingSaveSnapshot,changingZone;object waveRoutine;GameObject world=new GameObject("Camp");readonly List<GameObject> transientObjects=new List<GameObject>();
  int runSeed,wavePopulation,recapGoldLost;float nextReinforcementAt,lastDamageAmount,lastInterruptAt,respawnTimer,runDamageTaken,runHealingReceived;string lastDamageSource;bool objectiveHealedThisWave,DungeonSelectionOpen;Queue<object> reinforcementQueue=new Queue<object>();Dictionary<string,int> combatActions=new Dictionary<string,int>();RunChoices RunChoices=new RunChoices();FakeChoices pendingRoomChoice=new FakeChoices();
  public int LastEnemyExperience;public int OldWaveCalls,OldBuildCalls;public bool DungeonRewardPending=>false;public bool ModeRewardPending=>ChapterRewardPending;public bool WorldAlive=>world.activeInHierarchy;
  public void Notify(string s){Notice=s;}void SuspendInputs(){}void UpdateTimeScale(){}void AbandonSideEvent(){}void RetireWorldLootReceipts(int epoch){}string BuildRunSummary(bool success,string failure=null)=>"summary";
  void RecordRecapGoldLoss(int amount){recapGoldLost+=amount;}
  void ClearDungeonSettlement(){}void ResetArenaMode(bool dungeon){ModeRun=null;RoomChainRun=null;}bool TrySettleSideEventRewards()=>true;bool TrySettleDungeonReward()=>true;bool TrySettleArenaReward()=>TrySettleChapterReward();bool PreserveWorldLoot()=>true;
  void StopCoroutine(object routine){}void BeginRoomChainScene(){OldBuildCalls++;}void BeginArenaScene(){OldBuildCalls++;}void SpawnDungeonWave(){OldBuildCalls++;}void BuildSideEvent(){}void SpawnWildernessEnemy(){}
  void RecordArenaDefeat(EnemyController e){}void RecordRoomDefeat(EnemyController e){}void OnExpeditionEnemyKilled(EnemyController e){}public void LogSystem(string s){}void SpawnFloatingText(Vector3 p,string s,Color c){if(s.Contains(" XP  +"))LastEnemyExperience=int.Parse(s.Substring(1,s.IndexOf(" XP")-1));}void DeliverEnemyLoot(ItemData item,Vector3 p){}void FinalizeRoomChain(){}void FinalizeArenaResult(){}void TrySpawnReinforcements(){}object StartCoroutine(IEnumerator x){OldWaveCalls++;return x;}IEnumerator NextWave(){yield break;}
  public Vector3 FixtureObjective=>ChapterNextObjectivePoint;public void Tick()=>TickChapterRun();public void Camp(){ChangeZone(false);}public void FailForTest(){FailChapter("dead");}public ChapterRunReceipt Receipt=>chapterReceipt;
  public static string VerifyChapterResult(string folder)
  {
   int n=0;Action<bool,string> check=(ok,why)=>{n++;if(!ok)throw new Exception(why);};
   foreach(int first in new[]{0,1})
   {
    var hud=new GameSession{Progression=new ProgressionService(Path.Combine(folder,"seal-hud-"+first))};check(hud.Progression.CreateNewSlot(HeroClass.Vanguard)&&hud.ConfirmChapterEnter(),"seal HUD real host entry");
    foreach(var enemy in hud.Enemies)enemy.transform.position=new Vector3(99,0,99);
    hud.Player.transform.position=hud.chapterPlan.Objectives[first];for(int i=0;i<6;i++)hud.Tick();
    check(hud.ChapterSealView(first).Occupied&&!hud.ChapterSealView(1-first).Occupied&&hud.ChapterSealView(first).Seconds==1.5f,"HUD occupancy follows actual player on either ring");
    hud.Player.transform.position=hud.chapterPlan.Objectives[1-first];for(int i=0;i<6;i++)hud.Tick();
    hud.Enemies[0].transform.position=hud.chapterPlan.Objectives[1-first];hud.Tick();
    check(hud.ChapterSealView(1-first).Contested&&!hud.ChapterSealView(first).Contested&&hud.ChapterSealView(1-first).Seconds==1.5f,"HUD contest uses actual enemy region and pauses only occupied contested ring");
    hud.Enemies[0].transform.position=new Vector3(99,0,99);
    hud.Player.transform.position=hud.chapterPlan.Objectives[first];for(int i=0;i<6;i++)hud.Tick();
    check(hud.ChapterSealView(first).Complete&&hud.ChapterSealView(1-first).Seconds==1.5f,"HUD return preserves other half while chosen seal completes");
    hud.Player.transform.position=hud.chapterPlan.Objectives[1-first];for(int i=0;i<6;i++)hud.Tick();
    check(hud.ChapterSealView(0).Complete&&hud.ChapterSealView(1).Complete&&hud.ChapterRun.DoorUnlocked,"HUD both orders complete real host rings");
   }
   Func<string,GameSession> create=name=>{var game=new GameSession{Progression=new ProgressionService(Path.Combine(folder,name))};check(game.Progression.CreateNewSlot(HeroClass.Vanguard),"result fixture real save");game.FixtureUnlock();game.SelectedChapterNode=ChapterNode.StarPlatform;check(game.ConfirmChapterEnter(),"result fixture direct boss entry");return game;};
   var s=create("boss-first");Time.frameCount=10;LargeBossShutdownVisual.Owner=s;LargeBossShutdownVisual.Active=true;
   int xp=s.Progression.Profile.xp,gold=s.Progression.Profile.gold;var fake=new EnemyController();s.Enemies.Add(fake);s.OnEnemyKilled(fake);check(s.Progression.Profile.xp==xp&&s.Progression.Profile.gold==gold&&s.Enemies.Contains(fake),"unregistered chapter enemy cannot award or advance even when listed");s.Enemies.Remove(fake);
   var firstBoss=s.Enemies[0];s.OnEnemyKilled(firstBoss);check(s.LastEnemyExperience==33,"profile level one boss receives frozen 33 XP even at dungeon level two");xp=s.Progression.Profile.xp;gold=s.Progression.Profile.gold;s.OnEnemyKilled(firstBoss);check(s.Progression.Profile.xp==xp&&s.Progression.Profile.gold==gold,"repeated registered kill callback cannot pay twice");check(!s.ChapterFinished,"boss death alone does not settle live guards");
   Time.frameCount++;foreach(var e in new List<EnemyController>(s.Enemies))s.OnEnemyKilled(e);
   check(s.ChapterFinished&&s.ChapterRun.RewardClaimed&&s.ChapterResult.Saved,"reward is durably settled before presentation ends");
   check(!s.ChapterResultReady,"ACTIVE_BOSS_EXIT must keep opaque result hidden");
   LargeBossShutdownVisual.Active=false;check(s.ChapterResultReady,"finished boss exit allows result without a new guard-death delay");
   int paid=s.Progression.Profile.mechanicMaterials;s.TrySettleChapterReward();check(s.Progression.Profile.mechanicMaterials==paid,"presentation does not duplicate settled reward");
   s=create("boss-last");Time.frameCount=20;var boss=s.Enemies[0];s.OnEnemyKilled(s.Enemies[2]);s.OnEnemyKilled(s.Enemies[1]);s.OnEnemyKilled(boss);
   check(!s.ChapterResultReady,"boss death frame waits for visual lifecycle admission");Time.frameCount++;LargeBossShutdownVisual.Owner=s;LargeBossShutdownVisual.Active=true;check(!s.ChapterResultReady,"boss-last waits for actual visual rather than scaled combat time");s.ContinueChapterResult();check(s.ChapterResultReady&&!LargeBossShutdownVisual.Active,"explicit continue skips only presentation");
   s=create("failed-write");Directory.CreateDirectory(s.Progression.SaveFilePath+".tmp");foreach(var e in new List<EnemyController>(s.Enemies))s.OnEnemyKilled(e);
   check(s.ChapterRewardPending&&!s.ChapterResult.Saved&&s.ChapterResult.Materials==0&&s.ChapterResult.UnlockedNode==-1,"failed write publishes no saved reward or unlock snapshot");Directory.Delete(s.Progression.SaveFilePath+".tmp");check(s.TrySettleChapterReward()&&s.ChapterResult.Saved&&s.ChapterResult.SharedAfter==1,"same pending receipt retry captures committed shared tier");
   s=new GameSession{Progression=new ProgressionService(Path.Combine(folder,"failed-seal"))};check(s.Progression.CreateNewSlot(HeroClass.Vanguard)&&s.ConfirmChapterEnter(),"failure fixture enters forest");foreach(var e in s.Enemies)e.transform.position=new Vector3(99,0,99);var killed=s.Enemies[0];s.OnEnemyKilled(killed);check(s.LastEnemyExperience==7,"profile level one ordinary enemy receives frozen 7 XP");s.Player.transform.position=s.chapterPlan.Objectives[1];for(int i=0;i<5;i++)s.Tick();s.lastDamageSource="guard slam";s.lastDamageAmount=17;s.FailChapter("fixture generation path #4");
   check(s.ChapterResult.Failed&&s.ChapterResult.FirstSealSeconds==0&&s.ChapterResult.SecondSealSeconds==1.25f,"failure snapshot preserves independently chosen seal progress");check(s.ChapterResult.LastHit=="guard slam"&&s.ChapterResult.LastHitAmount==17&&s.ChapterResult.Failure.Contains("#4"),"failure snapshot retains actual last hit and specific path evidence");check(!s.ChapterResult.Saved&&s.ChapterResult.UnlockedNode==-1,"failure never invents saved unlock");
   s=new GameSession{Progression=new ProgressionService(Path.Combine(folder,"actual-death"))};check(s.Progression.CreateNewSlot(HeroClass.Vanguard)&&s.ConfirmChapterEnter(),"actual death enters forest");foreach(var enemy in s.Enemies)enemy.transform.position=new Vector3(99,0,99);s.OnEnemyKilled(s.Enemies[0]);s.Player.transform.position=s.chapterPlan.Objectives[1];for(int i=0;i<5;i++)s.Tick();s.lastDamageSource="guardian final slam";s.lastDamageAmount=23;int deathGold=s.Progression.Profile.gold;
   Directory.CreateDirectory(s.Progression.SaveFilePath+".tmp");s.OnPlayerDied();check(s.ChapterResult!=null,"DEATH_RESULT_CAPTURE must exist before XP budget cancellation");
   check(s.IsDead&&s.ChapterFinished&&s.ChapterResult.Failed&&s.ChapterResult.KillExperience==7&&s.Progression.ChapterKillExperienceEarned==0,"real death retains earned XP before canceling budget");
   check(s.ChapterResult.FirstSealSeconds==0&&s.ChapterResult.SecondSealSeconds==1.25f&&s.ChapterResult.LastHit=="guardian final slam"&&s.ChapterResult.LastHitAmount==23,"real death retains actual independent seal and last-hit evidence");
   string deathCopy=ChapterEntryPresentation.Result(s.ChapterResult);check(deathCopy.Contains("角色倒下")&&deathCopy.Contains("guardian final slam")&&deathCopy.Contains("本次击杀经验 +7")&&deathCopy.Contains("二 1.3s")&&!deathCopy.Contains("奖励已保存"),"actual death UI copy includes reason partial seal last hit and earned XP despite save failure");
   int remainingGold=s.Progression.Profile.gold;s.OnPlayerDied();check(s.Progression.Profile.gold==remainingGold&&remainingGold==deathGold-(int)Math.Floor(deathGold*.1f),"duplicate death preserves original single gold penalty");Directory.Delete(s.Progression.SaveFilePath+".tmp");
   var ordinary=new GameSession{Progression=new ProgressionService(Path.Combine(folder,"ordinary-death"))};check(ordinary.Progression.CreateNewSlot(HeroClass.Vanguard),"ordinary death profile");int ordinaryGold=ordinary.Progression.Profile.gold;ordinary.OnPlayerDied();check(ordinary.IsDead&&ordinary.ChapterResult==null&&ordinary.Progression.Profile.gold==ordinaryGold-(int)Math.Floor(ordinaryGold*.1f),"nonchapter death retains original penalty and no invented chapter result");
   s=new GameSession{Progression=new ProgressionService(Path.Combine(folder,"revisit"))};check(s.Progression.CreateNewSlot(HeroClass.Vanguard),"revisit slot");s.FixtureUnlock();s.SelectedChapterTactic=2;s.SelectedChapterDifficulty=ChapterDifficulty.Hard;
   check(s.ConfirmChapterEnter()&&s.RunChoices.Has(RunBlessing.IronSkin)&&!s.ForestMobileLineup,"C tactic applies after entry reset; first Hard lineup A");
   foreach(var enemy in s.Enemies)enemy.transform.position=new Vector3(99,0,99);
   for(int seal=0;seal<2;seal++){s.Player.transform.position=s.chapterPlan.Objectives[seal];for(int i=0;i<12;i++)s.Tick();}
   s.Player.transform.position=s.chapterPlan.Exit;check(s.EnterNextChapterRoom()&&s.RunChoices.Has(RunBlessing.IronSkin),"C tactic survives actual room retirement");s.FailForTest();check(!s.RunChoices.Has(RunBlessing.IronSkin),"C failure clears tactic");s.Camp();
   check(s.SelectedForestLineupB&&s.ConfirmChapterEnter()&&s.ForestMobileLineup&&s.Enemies.Count==6,"D repeated Hard attempt selects B independently of mirror");
   check(s.Enemies[1].PostRole==EscapeRole.GateGuard&&s.Enemies[4].PostRole==EscapeRole.GateGuard,"D B has two altar guards");
   check(s.Enemies[2].Kind==EnemyKind.Goblin&&s.Enemies[5].Kind==EnemyKind.Goblin&&s.Enemies[3].Kind==EnemyKind.Slime,"D B preserves exact six enemy kinds");
   s.FailForTest();s.Camp();check(!s.SelectedForestLineupB&&s.ConfirmChapterEnter()&&!s.ForestMobileLineup,"D third Hard attempt returns A");s.Camp();check(!s.RunChoices.Has(RunBlessing.IronSkin),"C camp clears tactic");
   foreach(HeroClass hero in Enum.GetValues(typeof(HeroClass)))foreach(bool mobile in new[]{false,true})for(int skill=0;skill<10;skill++)
   {
    var p=new GameProfile{heroClass=hero,chapterCompletedMask=7,equippedSkills=new[]{-1,-1,-1,-1,-1,-1,-1,-1,-1,-1}};p.skillRanks[skill]=1;
    bool eligible=mobile&&!GameBalance.IsPassive(skill)&&EnemyControlPolicy.IsInterruptSkill(hero,skill);
    check(RunChoices.ChapterTactic(p,mobile,1)==(eligible?RunBlessing.InterruptFlow:RunBlessing.SwiftHands),"C exact reachable interrupt eligibility");
    var choices=new RunChoices();check(choices.ChooseChapterTactic(p,ChapterNode.ForestCourt,mobile,0)&&!choices.ChooseChapterTactic(p,ChapterNode.ForestCourt,mobile,2),"C at most one tactic");
    p.chapterCompletedMask=3;choices.Reset();check(!choices.ChooseChapterTactic(p,ChapterNode.ForestCourt,mobile,0),"C first story has no tactic");
   }
   return "PASS: "+n+" actual chapter result persistence, independent seal evidence and boss presentation-gate checks";
  }
  public void FixtureUnlock(){Progression.Profile.chapterCompletedMask=7;Progression.Profile.chapterHighestDifficulties=new[]{3,3,3};Progression.Save();}
 }
}
public static class ChapterHostProductionTests
{
 static int checks;static void Check(bool v,string text){checks++;if(!v)throw new Exception(text);}
 static GameSession Fresh(string path){var s=new GameSession{Progression=new ProgressionService(path)};Check(s.Progression.CreateNewSlot(HeroClass.Vanguard),"fresh fixture save");return s;}
 static void Capture(GameSession s){s.Player.transform.position=s.FixtureObjective;for(int i=0;i<16;i++)s.Tick();}
 static void Exit(GameSession s){s.Player.transform.position=new Vector3(0,0,14);Check(s.EnterNextChapterRoom(),"real chapter exit succeeds");}
 static void Formation(GameSession s)
 {
  Check(s.Enemies.Count==6&&s.Enemies.FindAll(e=>e.Kind==EnemyKind.Wisp).Count==2&&s.Enemies.FindAll(e=>e.Kind==EnemyKind.Guardian).Count==2,"crossfire roster uses two wisps and two guardians within six-enemy cap");
  Check(s.Enemies[0].Kind==EnemyKind.Wisp&&s.Enemies[0].PostRole==EscapeRole.GateSupplier&&s.Enemies[1].PostRole==EscapeRole.SideFlanker,"hunt target remains index zero while second caster holds its lane");
  Check(s.Enemies[0].transform.position.x*s.Enemies[1].transform.position.x<0,"production crossfire spawn candidates occupy opposite fork lanes");
  for(int i=2;i<4;i++)Check(s.Enemies[i].PostRole==EscapeRole.GateGuard&&Vector3.Distance(s.Enemies[i].transform.position,new Vector3(0,0,11))<3,"both real guardian posts protect the exit capture ring");
  Check(s.Enemies[4].PostRole==EscapeRole.Pursuer&&s.Enemies[5].PostRole==EscapeRole.SideFlanker,"remaining melee retain pursuit and side-lane decisions");
 }
 public static string Run(string folder)
 {
  checks=0;var s=Fresh(Path.Combine(folder,"entry"));var oldWorldBuilds=WorldBuilder.Builds;int oldEpoch=s.Player.CombatEpoch;
  s.SelectedChapterDifficulty=ChapterDifficulty.Hard;Check(!s.ConfirmChapterEnter()&&!s.InDungeon&&s.WorldAlive&&s.Player.CombatEpoch==oldEpoch,"locked difficulty cannot destroy camp");s.SelectedChapterDifficulty=ChapterDifficulty.Normal;
  s.Progression.Profile.gold++;Directory.CreateDirectory(s.Progression.SaveFilePath+".tmp");Check(!s.ConfirmChapterEnter()&&!s.ChapterActive&&WorldBuilder.Builds==oldWorldBuilds&&s.Player.CombatEpoch==oldEpoch,"entry save failure leaves old world and epoch intact");Directory.Delete(s.Progression.SaveFilePath+".tmp");
  Check(s.ConfirmChapterEnter()&&s.Enemies.Count==6&&s.OldBuildCalls==0&&s.Player.CooldownResets==1,"chapter enters six-enemy host without old mode spawning");var first=s.ChapterRun;var stale=s.Enemies[0];Check(s.ChapterSupportMultiplier(s.Enemies[1])==1,"normal forest never grants support");Check(Math.Abs(stale.MaxHealth-CombatBalance.EnemyHealth(s.DungeonEntryLevel,1,false,EnemyKind.Wisp))<.001f,"normal chapter stats retain tier baseline");
  var originalPositions=s.Enemies.ConvertAll(e=>e.transform.position);foreach(var e in s.Enemies)e.transform.position=new Vector3(99,0,99);WorldTraversal.Rays=0;s.Tick();Check(WorldTraversal.Rays==0,"far capture contestants skip LOS query");for(int i=0;i<s.Enemies.Count;i++)s.Enemies[i].transform.position=originalPositions[i];
  foreach(var enemy in s.Enemies)enemy.transform.position=new Vector3(0,0,-8);
  Capture(s);Check(s.ChapterRun.Seals==1&&!s.ChapterRun.DoorUnlocked,"first seal alone cannot exit");Capture(s);Check(s.ChapterRun.DoorUnlocked,"two actual host captures open first exit");
  s.Player.transform.position=new Vector3(0,0,14);oldEpoch=s.Player.CombatEpoch;s.Progression.Profile.gold++;Directory.CreateDirectory(s.Progression.SaveFilePath+".tmp");Check(!s.EnterNextChapterRoom()&&s.ChapterRoomIndex==0&&s.Player.CombatEpoch==oldEpoch&&stale.gameObject.activeInHierarchy,"room save failure preserves living old room");Directory.Delete(s.Progression.SaveFilePath+".tmp");
  Exit(s);Check(s.ChapterRoomIndex==1&&s.Enemies.Count==6&&s.Player.CombatEpoch==oldEpoch+1&&s.Player.CooldownResets==1&&!stale.gameObject.activeInHierarchy,"second room retires epoch/enemies while retaining cooldowns");int gold=s.Progression.Profile.gold;s.OnEnemyKilled(stale);Check(s.Progression.Profile.gold==gold&&!s.ChapterRun.DoorUnlocked,"stale previous-room kill cannot award or advance new room");
  foreach(var enemy in new List<EnemyController>(s.Enemies))s.OnEnemyKilled(enemy);Check(s.OldWaveCalls==0&&!s.ChapterFinished,"chapter clear never schedules legacy NextWave or skips capture");Capture(s);Check(s.ChapterRun.DoorUnlocked&&!s.ChapterFinished,"second capture still requires actual exit");
  s.Player.transform.position=new Vector3(0,0,14);s.Progression.Profile.gold++;s.Progression.Save();
  Exit(s);Check(s.ChapterFinished&&!s.ChapterRewardPending&&s.Progression.Profile.chapterCompletedMask==1&&s.Progression.Profile.highestAdventureTier==0,"forest durable node completion changes story only");int paid=s.Progression.Profile.mechanicMaterials;Check(s.TrySettleChapterReward()&&s.Progression.Profile.mechanicMaterials==paid&&!s.EnterNextChapterRoom(),"repeat finish/exit cannot double pay");
  Check(!s.Progression.Profile.pendingFirstClearReward&&!s.ChapterResult.FirstCoreAvailable,"FOREST_CORE_BOUNDARY forest host completion must not enable shared core");
  s.Camp();Check(!s.ChapterActive&&!s.InDungeon,"return to camp clears chapter state");
  s.FixtureUnlock();s.SelectedChapterNode=ChapterNode.Redrock;s.SelectedChapterDifficulty=ChapterDifficulty.Hard;Check(s.ConfirmChapterEnter(),"hard redrock entry");Check(s.Enemies.TrueForAll(e=>e.PostCalls==1),"hard redrock configures existing guard roles once");Formation(s);Check(Math.Abs(s.Enemies[0].MaxHealth-CombatBalance.EnemyHealth(s.DungeonEntryLevel,1,false,EnemyKind.Wisp)*1.2f)<.001f&&Math.Abs(s.Enemies[0].damage-CombatBalance.EnemyDamage(s.DungeonEntryLevel,1,false)*1.15f)<.001f,"chapter difficulty multiplies already tier-scaled stats exactly once");s.OnEnemyKilled(s.Enemies[0]);Check(s.ChapterRun.DoorUnlocked&&s.Enemies.Count==5,"hunt prioritizes designated target rather than all enemies");Exit(s);Formation(s);s.FailForTest();var abandoned=s.Receipt;s.Camp();Check(!s.Progression.TryCompleteChapterNode(abandoned),"failed and abandoned attempt receipt is invalid");
  s.SelectedChapterNode=ChapterNode.StarPlatform;s.SelectedChapterDifficulty=ChapterDifficulty.Normal;Check(s.ConfirmChapterEnter()&&s.Enemies.Count==3&&s.ChapterRoomIndex==0&&!s.ChapterRun.DoorUnlocked&&s.ChapterObjectiveStatus.Contains("1 / 1"),"star directly enters its single boss room");Check(s.Enemies.Count==3&&LargeExpeditionBoss.Configures==1,"star creates one anchor boss plus two guards");
  s.OnEnemyKilled(s.Enemies[0]);Check(!s.ChapterFinished,"boss kill alone does not ignore guards");s.OnEnemyKilled(s.Enemies[0]);
  // Kill callback's normal enemy save is allowed to fail; chapter receipt must remain retryable.
  Directory.CreateDirectory(s.Progression.SaveFilePath+".tmp");s.OnEnemyKilled(s.Enemies[0]);Check(s.ChapterFinished&&s.ChapterRewardPending&&s.Progression.Profile.highestAdventureTier==0,"real completion save failure publishes no shared tier and remains pending");Directory.Delete(s.Progression.SaveFilePath+".tmp");Check(s.TrySettleChapterReward()&&!s.ChapterRewardPending&&s.Progression.Profile.highestAdventureTier==1,"same terminal receipt can retry durable star tier completion");Check(s.Progression.Profile.pendingFirstClearReward&&!s.Progression.Profile.pendingFashionChest,"first successful star uses shared one-time core entitlement without relic chest");
  s.Camp();s.SelectedChapterNode=ChapterNode.ForestCourt;s.SelectedChapterDifficulty=ChapterDifficulty.Hard;Check(s.ConfirmChapterEnter(),"hard forest entry");var supplier=s.Enemies[0];var friend=s.Enemies[1];friend.transform.position=supplier.transform.position+new Vector3(1,0,0);Check(s.ChapterSupportMultiplier(friend)==.7f&&s.ChapterSupportMultiplier(supplier)==1,"hard forest gives actual support to others only");WorldTraversal.Occluded=true;Check(s.ChapterSupportMultiplier(friend)==1,"wall severs chapter support");WorldTraversal.Occluded=false;friend.transform.position=new Vector3(99,0,99);WorldTraversal.Rays=0;Check(s.ChapterSupportMultiplier(friend)==1&&WorldTraversal.Rays==0,"distant support skips LOS query");s.Camp();
  WorldTraversal.SpawnBlocked=true;Check(s.ConfirmChapterEnter()&&s.ChapterFinished&&s.ChapterRun.Failed,"unavailable spawn fails safely without legacy fallback enemies");WorldTraversal.SpawnBlocked=false;s.Camp();
  var state=new ChapterCombatRun(ChapterNode.ForestCourt,ChapterDifficulty.Normal,42);Check(!state.Register(0,-1,0),"unbound room cannot accept spawn");state.BindRoom(7);
  for(int i=0;i<6;i++)Check(state.Register(0,7,i)&&!state.Register(0,7,i),"registration is bounded and idempotent");
  Check(!state.Register(0,7,6)&&!state.Defeat(0,6,0)&&!state.Defeat(1,7,0),"cap and stale epoch/room rejected");
  state.Advance(200,true,true,false);Check(state.Progress==.25f,"large frame cannot skip objective interaction");state.Advance(float.NaN,true,true,false);state.Advance(1,false,true,false);state.Advance(1,true,true,true);Check(state.Progress==.25f,"pause contest invalid delta retain progress");
  Check(!state.Exit(true,false)&&!state.FinishBoss(),"no early room exit or boss completion");state.Fail();Check(!state.Defeat(0,7,0)&&!state.Exit(true,false),"failed state rejects late callbacks");
  s.SelectedChapterDifficulty=ChapterDifficulty.Heroic;Check(s.ConfirmChapterEnter(),"heroic fixture entry");Check(Math.Abs(s.Enemies[0].MaxHealth-CombatBalance.EnemyHealth(s.DungeonEntryLevel,1,false,EnemyKind.Wisp)*1.35f)<.001f&&Math.Abs(s.Enemies[0].damage-CombatBalance.EnemyDamage(s.DungeonEntryLevel,1,false)*1.25f)<.001f,"heroic difficulty uses stated stat multipliers");s.Camp();
  s.SelectedChapterNode=ChapterNode.Redrock;Check(s.ConfirmChapterEnter(),"heroic redrock entry");Formation(s);s.OnEnemyKilled(s.Enemies[0]);Exit(s);Formation(s);s.FailForTest();s.Camp();
  // Independent real Redrock -> Star progression, without FixtureUnlock's synthetic Star-complete mask.
  var boundary=Fresh(Path.Combine(folder,"core-boundary"));boundary.Progression.Profile.chapterCompletedMask=1;boundary.Progression.Profile.chapterHighestDifficulties=new[]{1,0,0};boundary.Progression.Save();Check(!boundary.Progression.Profile.pendingFirstClearReward,"forest migration retains no core entitlement");
  boundary.SelectedChapterNode=ChapterNode.Redrock;Check(boundary.ConfirmChapterEnter(),"core boundary enters redrock");boundary.OnEnemyKilled(boundary.Enemies[0]);Exit(boundary);foreach(var enemy in new List<EnemyController>(boundary.Enemies))boundary.OnEnemyKilled(enemy);Capture(boundary);Exit(boundary);
  Check(boundary.ChapterResult.Saved&&!boundary.ChapterResult.FirstCoreAvailable&&!boundary.Progression.Profile.pendingFirstClearReward,"REDROCK_CORE_BOUNDARY actual redrock completion grants no shared core");boundary.Camp();Check(boundary.Progression.LoadSlot(boundary.Progression.CurrentSlotId)&&!boundary.Progression.Profile.pendingFirstClearReward,"redrock save reload cannot manufacture core entitlement");
  boundary.SelectedChapterNode=ChapterNode.StarPlatform;Check(boundary.ConfirmChapterEnter(),"core boundary enters star after earlier nodes");foreach(var enemy in new List<EnemyController>(boundary.Enemies))boundary.OnEnemyKilled(enemy);
  Check(boundary.ChapterResult.Saved&&boundary.ChapterResult.FirstCoreAvailable&&boundary.Progression.Profile.pendingFirstClearReward,"STAR_CORE_BOUNDARY only actual saved star completion newly enables shared core");int coreMaterials=boundary.Progression.Profile.mechanicMaterials;Check(boundary.TrySettleChapterReward()&&boundary.Progression.Profile.mechanicMaterials==coreMaterials,"star retry cannot repeat materials or entitlement");boundary.Camp();Check(boundary.Progression.LoadSlot(boundary.Progression.CurrentSlotId)&&boundary.Progression.Profile.pendingFirstClearReward,"star entitlement survives real save reload");
  return "PASS: "+checks+" actual chapter host/save/transition/kill assertions (managed scene doubles, no Unity gameplay)";
 }
}
