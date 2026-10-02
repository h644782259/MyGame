from pathlib import Path
import subprocess,hashlib,json,os
base=Path(__file__).resolve().parent;repo=Path('/workspace/emberfall_free_seals_integration');ref='1e61f116';dotnet='/workspace/scratch/dotnet/dotnet';hashes={}
def source(path):
 data=subprocess.check_output(['git','show',ref+':'+path],cwd=repo);hashes[path]=hashlib.sha256(data).hexdigest();dest=base/'frozen'/path;dest.parent.mkdir(parents=True,exist_ok=True);dest.write_bytes(data);return data.decode()
def member(s,sig):
 a=s.index(sig);b=s.index('{',a)+1;d=1
 while d:d+=(s[b]=='{')-(s[b]=='}');b+=1
 return s[a:b]
room=source('Assets/Scripts/Core/GameSession.RoomChain.cs');session=source('Assets/Scripts/Core/GameSession.cs')
methods='\n'.join(member(room,s) for s in ['public bool NearRoomExit','private void BeginRoomChainScene()','private void RecordRoomDefeat(','public bool EnterNextRoom()','private bool ConfirmRoomInterlude('])+'\n'+'\n'.join(member(session,s) for s in ['public bool SaveBeforeLeaving()','public bool InputBlocked','private void UpdateTimeScale()'])
fixture=source('Tests/RoomFreeSealHostTests.cs').split('public static class RoomFreeSealHostTests')[0]
fixture=fixture.replace(' public enum EnemyKind{Wisp,Guardian,Goblin,Slime}\n','')
fixture=fixture.replace('public Color(float r,float g,float b)','public Color(float r,float g,float b,float a=1)')
start=fixture.index(' public class ChoiceDouble');end=fixture.index(' public static class MobileControls',start);fixture=fixture[:start]+fixture[end:]
fixture=fixture.replace('public object Profile=new object();','public GameProfile Profile=new GameProfile{heroClass=HeroClass.Vanguard,skillRanks=new[]{1,1,1,0,1,1,1,1,0,1},equippedSkills=new[]{0,1,2,4,5}};')
fixture=fixture.replace('public ChoiceDouble RunChoices=new ChoiceDouble();','public RunChoices RunChoices=new RunChoices();')
fixture=fixture.replace('public bool InputBlocked=>Paused||BackgroundPaused||IsDead;','private ApplicationPauseState pauseState=new ApplicationPauseState();private bool uiBlocking,DungeonSelectionOpen;private bool ModeFinished=>RoomChainRun!=null&&RoomChainRun.Finished;')
fixture=fixture.replace('void UpdateTimeScale(){}','')
math=source('Tests/DestructibleTraversalTests.cs');math='using System;using UnityEngine;'+math[math.index('namespace Emberfall'):]
math=math.replace('    public enum ZoneKind{Wilderness,Dungeon}\n','').replace('public static float time=0;','public static float time=0,deltaTime=.25f,timeScale=1;public static int frameCount;')
files=['Core/RoomChainState','Core/RoomTactics','Core/RoomTacticalRegion','Core/DeferredRoomChoice','Core/EscapePostPolicy','Core/GameSession.RoomTactics','World/WorldTraversal','World/TacticalRoomGeometry','World/EscapeRoomFormation','UI/ChapterSealPresentation','UI/RoomObjectivePresentation','Core/GameTypes','Core/CombatBalance','Core/RunChoices','Core/RunChoices.Rooms','Core/ApplicationPauseState']
originals={Path(f).name+'.cs':source('Assets/Scripts/'+f+'.cs') for f in files}
probe=r'''
using System;using System.Linq;using UnityEngine;
namespace Emberfall {public sealed partial class GameSession {
static int checks;static void C(bool ok,string message){checks++;if(!ok)throw new Exception(message);}
public static void Probe(){
 foreach(int seed in new[]{0,3,6,9,12,15}){
  Time.timeScale=1;var s=new GameSession(seed);var plan=s.RoomChainRun.Room;int epoch=s.Player.CombatEpoch;
  C(s.RunChoices.CompletedWave==0&&!s.RunChoices.AwaitingChoice&&!s.pendingRoomChoice.Pending,"first room begins without any chosen blessing");
  foreach(var e in s.Enemies)e.transform.position=new Vector3(0,0,-10);
  s.Player.Teleport(s.Markers[1].transform.position);for(int i=0;i<12;i++)s.Tick();
  C(s.RoomChainRun.SealComplete(1)&&!s.RoomChainRun.SealComplete(0)&&!s.pendingRoomChoice.Pending,"B-first completion alone does not request blessing");
  s.Player.Teleport(s.Markers[0].transform.position);for(int i=0;i<12;i++)s.Tick();
  C(s.RoomChainRun.DoorUnlocked&&s.RoomChainRun.Seals==2&&s.pendingRoomChoice.Pending&&!s.RunChoices.AwaitingChoice,"both seals request deferred blessing without synchronous UI");
  C(s.Enemies.Count==6&&s.RunChoices.CompletedWave==0,"completion bypasses live guards without inventing selected wave");
  s.Player.Teleport(new Vector3(0,0,14));C(!s.EnterNextRoom()&&ReferenceEquals(s.RoomChainRun.Room,plan)&&s.Progression.Saves==0,"pending blessing blocks actual EnterNextRoom before save or transition");
  s.TickRoomTactics();C(s.pendingRoomChoice.Pending&&!s.RunChoices.AwaitingChoice,"same frame cannot claim deferred request");
  s.Tick();C(!s.pendingRoomChoice.Pending&&s.RunChoices.AwaitingChoice&&s.RunChoices.CompletedWave==1&&s.RunChoices.Offer.Length==3,"next frame claims deferred blessing through real offer generation");
  C(s.InputBlocked&&Time.timeScale==0,"real InputBlocked and UpdateTimeScale pause during blessing choice");
  C(!s.EnterNextRoom()&&ReferenceEquals(s.RoomChainRun.Room,plan)&&s.Player.CombatEpoch==epoch,"awaiting choice blocks room exit even after request is claimed");
  C(!s.ConfirmRoomInterlude(-1)&&s.RunChoices.AwaitingChoice,"invalid choice cannot release gate");
  s.Paused=true;C(!s.ConfirmRoomInterlude(0),"manual pause blocks blessing confirmation");s.Paused=false;
  C(s.ConfirmRoomInterlude(0)&&!s.RunChoices.AwaitingChoice&&s.RunChoices.Active.Count()==1&&!s.InputBlocked&&Time.timeScale==1,"real confirm selects one offered blessing and resumes combat");
  C(!s.ConfirmRoomInterlude(0)&&s.RunChoices.Active.Count()==1,"duplicate confirmation cannot choose twice");
  C(s.EnterNextRoom()&&s.RoomChainRun.Room.Index==1&&s.Player.CombatEpoch==epoch+1&&s.Progression.Saves==1,"only completed choice permits saved epoch-retiring transition");
  s.Tick();C(!s.pendingRoomChoice.Pending&&!s.RunChoices.AwaitingChoice&&s.RunChoices.Active.Count()==1,"new room cannot reclaim old first-room choice");
 }
 Console.WriteLine("PASS: "+checks+" actual first-room B-first deferred-choice/real RunChoices/pause/transition assertions; managed scene and save-service doubles");
}}}
class Program {static void Main(){Emberfall.GameSession.Probe();}}
'''
env=dict(os.environ,DOTNET_CLI_HOME=str(base/'cli'),DOTNET_NOLOGO='1')
results=[]
for mode,expected in [('current',None),('remove-pending-block','pending blessing blocks actual EnterNextRoom before save or transition'),('remove-deferred-claim','next frame claims deferred blessing through real offer generation')]:
 p=base/mode;p.mkdir(exist_ok=True)
 for name,s in originals.items():
  if mode=='remove-deferred-claim' and name=='GameSession.RoomTactics.cs':s=s.replace('if(TryOpenPendingRoomChoice())return;','')
  (p/name).write_text(s)
 body=methods.replace('||pendingRoomChoice.Pending','') if mode=='remove-pending-block' else methods
 (p/'Methods.cs').write_text('using UnityEngine;using System.Collections.Generic;namespace Emberfall{public sealed partial class GameSession{'+body+'}}')
 (p/'Fixture.cs').write_text(fixture);(p/'Math.cs').write_text(math);(p/'Probe.cs').write_text(probe)
 project=p/'Probe.csproj';project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><NuGetAudit>false</NuGetAudit><NoWarn>0649;0414</NoWarn></PropertyGroup></Project>');(p/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>')
 build=subprocess.run([dotnet,'build',str(project),'--configfile',str(p/'NuGet.Config'),'-v:q'],env=env,capture_output=True,text=True);(p/'build.log').write_text(build.stdout+build.stderr)
 if build.returncode:print(build.stdout+build.stderr);build.check_returncode()
 run=subprocess.run([dotnet,str(p/'bin/Debug/net8.0/Probe.dll')],env=env,capture_output=True,text=True);(p/'run.log').write_text(run.stdout+run.stderr)
 if expected:assert run.returncode and 'System.Exception: '+expected in run.stdout+run.stderr,run.stdout+run.stderr;print('PASS compiled negative:',mode)
 else:print(run.stdout+run.stderr);run.check_returncode()
 results.append({'mode':mode,'exit':run.returncode,'expected_failure':expected})
report={'source_ref':ref,'source_commit':subprocess.check_output(['git','rev-parse',ref],cwd=repo,text=True).strip(),'tree':subprocess.check_output(['git','rev-parse',ref+'^{tree}'],cwd=repo,text=True).strip(),'equivalent_commit':'3d2c33ea3a4262d1186e0bede073d2041cc551cb','sha256':hashes,'results':results,'limits':['Managed scene and save-service doubles; no Unity or real filesystem save','Real full RunChoices/RunChoices.Rooms/ApplicationPauseState and extracted actual InputBlocked/UpdateTimeScale/ConfirmRoomInterlude used','Actual full RoomTactics and state/navigation; actual room begin, gate, enter, save-before-leaving methods','Standalone scratch validation, not part of frozen repository or191aggregate']}
(base/'report.json').write_text(json.dumps(report,indent=2))
