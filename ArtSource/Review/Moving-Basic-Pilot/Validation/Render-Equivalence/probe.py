from pathlib import Path
import subprocess,hashlib,json,os
root=Path('/workspace/emberfall_moving_pilot');out=Path(__file__).parent;dotnet='/workspace/scratch/dotnet/dotnet';old='49a356eb2df9951fc07f9758f3eea4055b0b9c30';new='dcd75ac8a96806788868696f05cffcf4a4d583f4'
def git(*args):return subprocess.check_output(['git',*args],cwd=root)
assert git('rev-parse','HEAD').decode().strip()==new, 'probe requires frozen candidate HEAD'
paths=git('ls-tree','-r','--name-only',old,'--','Assets/Resources/BlenderPilot','ArtSource').decode().splitlines();assert len(paths)==69
samepaths=git('ls-tree','-r','--name-only',new,'--','Assets/Resources/BlenderPilot','ArtSource').decode().splitlines();assert paths==samepaths
paths+=['Assets/Scripts/Combat/BlenderPilotVisual.cs','Assets/Scripts/Core/BlenderPilotPosePolicy.cs']
report={'renderedCommit':old,'newCommit':new,'artPaths':69,'files':{}}
for p in paths:
 a=git('show',old+':'+p);b=git('show',new+':'+p);assert a==b,p;report['files'][p]={'equal':True,'sha256':hashlib.sha256(a).hexdigest(),'bytes':len(a)}
(out/'byte-equivalence.json').write_text(json.dumps(report,indent=2))
# Preserve the frozen actual production facing harness; replace its scenario only.
s=(root/'Tests/PilotFacingCommitProductionTests.py').read_text()
s=s.replace("r=Path(__file__).resolve().parents[1];dotnet=sys.argv[1] if len(sys.argv)>1 else 'dotnet'", "r=Path('/workspace/emberfall_moving_pilot');dotnet='/workspace/scratch/dotnet/dotnet'")
s=s.replace('static void Main(){PilotFacingFixture.Run();}', 'static void Main(){PilotFacingFixture.RunLocked();}')
# Add a controlled input-held bit, instead of the fixture's always-held mouse.
s=s.replace("(p/name).write_text((r/'Tests'/name).read_text())", "(p/name).write_text((r/'Tests'/name).read_text().replace('public static Vector2 mousePosition;', 'public static Vector2 mousePosition;public static bool Held=true;').replace('GetMouseButton(int n)=>true', 'GetMouseButton(int n)=>Held').replace('public static void Run(){',EXTRA+'public static void Run(){') if name=='PilotFacingCommitProductionFixture.cs' else (r/'Tests'/name).read_text())")
s=s.replace("ns['methods']+member(motion","ns['methods']+member(model,'public int WeaponActionId')+member(motion")
a=s.index(" for name,old,new in [('production'");s=s[:a]+s[a:].replace("for name,old,new in [('production',None,None),('missing-raw-facing','locomotion.Speed<=.05f || PilotMovingBasicFacing()','true'),('stale-basis','owner.InverseTransformDirection(pilotAcceptedWalkingWorld)','pilotAcceptedWalkingWorld')]:", "for name,old,new in [('production',None,None)]:",1)
s=s.replace("with tempfile.TemporaryDirectory(prefix='pilot-facing-') as tmp:", "tmp='/workspace/scratch/render-equivalence-dcd75ac/build'\nPath(tmp).mkdir(exist_ok=True)\nif True:")
extra=r'''
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
'''
s='EXTRA='+repr(extra)+'\n'+s
(out/'run_locked.py').write_text(s)
env=dict(os.environ,DOTNET_CLI_HOME=str(out/'cli'));subprocess.run(['python3',str(out/'run_locked.py')],env=env,check=True)
