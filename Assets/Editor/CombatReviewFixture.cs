using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Emberfall.Editor
{
    // Opt-in manual Play Mode fixture. Logs actual callbacks and labelled sampled observations.
    [InitializeOnLoad]
    public static class CombatReviewFixture
    {
        const string Key="Emberfall.CombatReview.", SaveKey="Emberfall.ValidationSaveDirectory";
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        static StreamWriter writer;
        static GameSession game;
        static double started, nextSample;
        static int rows;
        static string lastHoldState;
        static double nextPose;
        static bool ending;
        static readonly Dictionary<int,float> health=new Dictionary<int,float>();
        static readonly Dictionary<int,Vector3> petPositions=new Dictionary<int,Vector3>();
        static readonly Dictionary<int,int> petTargets=new Dictionary<int,int>();
        static readonly FieldInfo petTarget=typeof(SummonedCompanion).GetField("target",Private);
        [Serializable] sealed class Row
        {
            public string kind,detail; public int frame,actorId,targetId,skill;
            public float time,amount,x,y,z;
        }
        static CombatReviewFixture()
        {
            EditorApplication.update+=Tick;
            EditorApplication.playModeStateChanged+=StateChanged;
            AssemblyReloadEvents.beforeAssemblyReload+=Close;
        }
        // CLI: -executeMethod Emberfall.Editor.CombatReviewFixture.Run -reviewBuild ranger -reviewScenario moving
        // Do not pass -quit. Default manual session ends after 180 seconds or when Play Mode stops.
        [MenuItem("Emberfall/Review/Start level 50 fixture")]
        public static void Run()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode || SessionState.GetBool(Key+"Active",false)) throw new InvalidOperationException("Stop current Play Mode first.");
            if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("Save the current scene first.");
            string build=Arg("-reviewBuild","vanguard"), scenario=Arg("-reviewScenario","stationary");
            if(Array.Find(CombatReviewConfigurations.Create(),c=>c.id==build)==null)throw new ArgumentException("Unknown review build: "+build);
            if(scenario!="stationary"&&scenario!="moving"&&scenario!="formation")throw new ArgumentException("Scenario must be stationary, moving or formation.");
            string output=Path.GetFullPath(Path.Combine(Application.dataPath,"../Tests/TestResults/CombatReview-"+Guid.NewGuid().ToString("N")));
            ending=false;Directory.CreateDirectory(Path.Combine(output,"IsolatedSave"));
            SessionState.SetString(Key+"Output",output);SessionState.SetString(Key+"Build",build);SessionState.SetString(Key+"Scenario",scenario);
            SessionState.SetString(Key+"PreviousSave",SessionState.GetString(SaveKey,""));
            SessionState.SetString(SaveKey,Path.Combine(output,"IsolatedSave"));SessionState.SetBool(Key+"Active",true);
            EditorSceneManager.OpenScene("Assets/Scenes/Main.unity",OpenSceneMode.Single);
            EditorApplication.isPlaying=true;
        }
        static string Arg(string key,string fallback)
        {string[] args=Environment.GetCommandLineArgs();for(int i=0;i+1<args.Length;i++)if(args[i]==key)return args[i+1];return fallback;}
        static void StateChanged(PlayModeStateChange state)
        {
            if(state!=PlayModeStateChange.EnteredEditMode||!SessionState.GetBool(Key+"Active",false))return;
            Close();SessionState.SetString(SaveKey,SessionState.GetString(Key+"PreviousSave",""));SessionState.SetBool(Key+"Active",false);
        }
        static void StartFixture()
        {
            string output=SessionState.GetString(Key+"Output","");
            string expected=Path.GetFullPath(Path.Combine(output,"IsolatedSave"));
            if(Path.GetFullPath(SessionState.GetString(SaveKey,""))!=expected)throw new InvalidOperationException("Isolated save override lost.");
            var config=Array.Find(CombatReviewConfigurations.Create(),c=>c.id==SessionState.GetString(Key+"Build",""));
            UnityEngine.Random.InitState(config.seed);
            game.StartNew(config.hero);
            if(!game.HasStarted||game.Player==null)throw new InvalidOperationException("Review character creation failed.");
            if(!Path.GetFullPath(game.Progression.SaveFilePath).StartsWith(expected+Path.DirectorySeparatorChar,StringComparison.Ordinal))throw new InvalidOperationException("Review save escaped isolation.");
            var p=game.Progression.Profile;p.level=config.level;p.xp=0;p.specialization=config.specialization;
            p.skillRanks=(int[])config.skillRanks.Clone();p.masteryRanks=new int[4];p.masteryCore=-1;p.summonerRoute=SummonerRoute.Bonded;
            p.tutorialMask=int.MaxValue;game.Progression.Save();
            if(!string.IsNullOrEmpty(game.Progression.LastError))throw new InvalidOperationException(game.Progression.LastError);
            p=game.Progression.Profile;game.Player.RefreshStats(true);game.SetPaused(false);
            foreach(var e in game.Enemies)if(e!=null){e.gameObject.SetActive(false);UnityEngine.Object.Destroy(e.gameObject);}game.Enemies.Clear();
            // Keep the actual wilderness terrain/collisions, suppress only unrelated ambient respawns.
            typeof(GameSession).GetField("respawnTimer",Private).SetValue(game,100000f);
            game.Player.Teleport(WorldTraversal.NearestWalkable(new Vector3(0,0,-3),.45f));
            game.Player.ResetCooldownsForDungeonEntry();
            string scenario=SessionState.GetString(Key+"Scenario","");
            if(scenario=="stationary")Spawn(EnemyKind.Goblin,new Vector3(0,0,1),false);
            else if(scenario=="moving")Spawn(EnemyKind.Goblin,new Vector3(3,0,3),true);
            else {Spawn(EnemyKind.Guardian,new Vector3(-1,0,1),true);Spawn(EnemyKind.Goblin,new Vector3(2,0,1),true);Spawn(EnemyKind.Wisp,new Vector3(0,0,7),true);}
            if(Camera.main!=null)Camera.main.GetComponent<AdventureCamera>().Snap();
            File.WriteAllText(Path.Combine(output,"configuration.json"),JsonUtility.ToJson(config,true));
            File.WriteAllText(Path.Combine(output,"profile.json"),JsonUtility.ToJson(p,true));
            writer=new StreamWriter(Path.Combine(output,"events.jsonl"),false);rows=0;health.Clear();petPositions.Clear();petTargets.Clear();
            started=EditorApplication.timeSinceStartup;nextSample=started;nextPose=started;lastHoldState=null;
            CombatReviewEvents.Observed+=Record;
            Write("session_start",0,0,0,-1,"Actual Unity editor session; unity="+Application.unityVersion+"; scenario="+scenario+"; width="+Screen.width+"; height="+Screen.height+"; quality="+QualitySettings.GetQualityLevel()+"; observed HP deltas are sampled, not individual hit attribution");
            foreach(var e in game.Enemies){health[e.GetInstanceID()]=e.Health;Write("fixture_enemy",e.GetInstanceID(),0,e.Health,-1,e.Kind+"; ai="+e.enabled,e.transform.position);}
            Debug.Log("Combat review ready; real event log: "+output);
        }
        static void Spawn(EnemyKind kind,Vector3 at,bool ai)
        {
            at=WorldTraversal.NearestWalkable(at,kind==EnemyKind.Guardian?.6f:.45f);
            typeof(GameSession).GetMethod("SpawnEnemy",Private).Invoke(game,new object[]{kind,50,at,false});
            game.Enemies[game.Enemies.Count-1].enabled=ai;
        }
        static void Tick()
        {
            if(!SessionState.GetBool(Key+"Active",false)||!EditorApplication.isPlaying)return;
            try
            {
                if(ending)return;
                if(writer==null){game=GameSession.Instance;if(game==null||game.Progression==null)return;StartFixture();}
                if(EditorApplication.timeSinceStartup-started>=180||rows>=100000){Write("session_end",0,0,0,-1,"bounded duration/event cap; no acceptance assertion");Close();EditorApplication.isPlaying=false;return;}
                if(EditorApplication.timeSinceStartup<nextSample)return;nextSample=EditorApplication.timeSinceStartup+.1;
                foreach(var e in game.Enemies)
                {
                    if(e==null)continue;int id=e.GetInstanceID();float old;
                    if(health.TryGetValue(id,out old)&&old>e.Health)Write("observed_enemy_hp_delta",id,0,old-e.Health,-1,"100ms sample; may aggregate several hits",e.transform.position);
                    health[id]=e.Health;
                }
                foreach(var pet in SummonedCompanion.Snapshot(game.Player))
                {
                    if(pet==null)continue;int id=pet.GetInstanceID();var target=petTarget.GetValue(pet) as EnemyController;
                    Vector3 old;int oldTarget;int targetId=target==null?0:target.GetInstanceID();
                    if(targetId!=0&&petPositions.TryGetValue(id,out old)&&(pet.transform.position-old).sqrMagnitude>.0001f)
                        Write("petchase",id,targetId,Vector3.Distance(pet.transform.position,target.transform.position),-1,"sampled movement with live target; not path-success proof",pet.transform.position);
                    if(!petTargets.TryGetValue(id,out oldTarget)||oldTarget!=targetId)Write("pet_target",id,targetId,0,-1,pet.Form.ToString());
                    petPositions[id]=pet.transform.position;petTargets[id]=targetId;
                }
                if(EditorApplication.timeSinceStartup>=nextPose)
                {
                    nextPose=EditorApplication.timeSinceStartup+1;
                    Write("player_pose",game.Player.GetInstanceID(),0,game.Player.Health,-1,"energy="+game.Player.Energy,game.Player.transform.position);
                    if(Camera.main!=null)Write("camera_pose",Camera.main.GetInstanceID(),0,Camera.main.fieldOfView,-1,"rotation="+Camera.main.transform.eulerAngles,Camera.main.transform.position);
                }
                if(game.ModeRun!=null)
                {
                    // Optional 02 integration, discovered without coupling to an unmerged rule type.
                    var mode=game.ModeRun;var type=mode.GetType();
                    var hold=type.GetProperty("HoldState");var idle=type.GetProperty("HoldIdleWaitSeconds");
                    if(hold!=null&&idle!=null)
                    {
                        string state=Convert.ToString(hold.GetValue(mode,null));
                        if(state!=lastHoldState){Write("hold_state",0,0,0,-1,state);lastHoldState=state;}
                        Write("hold_idle_wait",0,0,Convert.ToSingle(idle.GetValue(mode,null)),-1,"cumulative combat-active wait seconds");
                    }
                }
                writer.Flush();
            }
            catch(Exception error){Debug.LogException(error);if(writer!=null)Write("fixture_error",0,0,0,-1,error.ToString());Close();EditorApplication.isPlaying=false;}
        }
        static void Record(CombatReviewEvents.Entry e){Write(e.kind,e.actorId,e.targetId,e.amount,e.skill,e.detail);}
        static void Write(string kind,int actor,int target,float amount,int skill,string detail,Vector3 position=default(Vector3))
        {
            if(writer==null||rows>=100001)return;
            writer.WriteLine(JsonUtility.ToJson(new Row{kind=kind,frame=Time.frameCount,time=Time.time,actorId=actor,targetId=target,amount=amount,skill=skill,detail=detail,x=position.x,y=position.y,z=position.z}));rows++;
        }
        static void Close(){ending=true;CombatReviewEvents.Observed-=Record;if(writer!=null){Write("recorder_closed",0,0,0,-1,"Recorder stopped; no acceptance verdict");writer.Flush();writer.Dispose();writer=null;}game=null;}
    }
}
