from pathlib import Path
import os,subprocess,sys
r=Path(sys.argv[1]).resolve();o=Path(sys.argv[2]).resolve();o.mkdir(parents=True,exist_ok=True);p=o/'export';dotnet=sys.argv[3] if len(sys.argv)>3 else 'dotnet'
def member(s,k):
 a=s.index(k);b=s.index('{',a)+1;n=1
 while n:n+=(s[b]=='{')-(s[b]=='}');b+=1
 return s[a:b]
import shutil
if p.exists():shutil.rmtree(p)
subprocess.run(['python3',str(r/'Tools/ArtSourceEvidence/export_silhouettes.py'),str(r),str(o),dotnet],check=True)
m=(r/'Assets/Scripts/Combat/CombatModel.cs').read_text()
import re
(p/'EnemyEnum.cs').write_text('namespace Emberfall{'+re.search(r'public enum EnemyKind\s*\{[^}]+\}',(r/'Assets/Scripts/Core/GameTypes.cs').read_text())[0]+'}')
for folder,names in [('Core',['LocomotionPoseState','VisualMotionEnvelope','BasicActionTimeline','SkillDamageBudgets','CombatBalance','CasterPoseRecipe']),('Combat',['CombatModel.Motion','CombatModel.Recovery','CombatModel.CastPoses'])]:
 for n in names:(p/(n+'.cs')).write_text((r/'Assets/Scripts'/folder/(n+'.cs')).read_text())
methods='\n'.join(member(m,k) for k in ['public void PlayAction(', 'private void CommitActionPose(', 'private static Quaternion Pose(', 'private void AnimateHero('])
(p/'MotionExport.cs').write_text('''using UnityEngine;namespace Emberfall {public sealed partial class CombatModel {
private float actionAge,actionDuration,gaitPhase,previewTime;private int actionStartedFrame;private bool isolatedPreview,pilotCharging,pilotAirborne;
private static void AimArm(Transform a,Transform b,Vector3 c,Vector3 d){throw new System.Exception("Non-Vanguard boundary");}
public void ExportTick(float dt){AnimateHero(locomotion.Speed,1,false,dt);if(tailoredCloth!=null)typeof(TailoredCloth).GetMethod("LateUpdate",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(tailoredCloth,null);}
public float ExportPhase=>locomotion.Phase;public float ExportSpeed=>locomotion.Speed;public float ExportProgress=>actionDuration>0?actionAge/actionDuration:1;public bool ExportPilot=>pilotVisible;
private bool SampleBlenderPilot(bool acting,float progress,bool hurt){pilotVisible=!pilotHasGear&&!pilotHasFashion&&(!acting||locomotion.Speed<=.05f);if(pilotVisible){transform.localPosition=Vector3.zero;transform.localRotation=Quaternion.identity;}return pilotVisible;}
'''+methods+'}}')
f=(p/'Fixture.cs').read_text().replace('public struct Vector3 {','public struct Vector3 {public static Vector3 Lerp(Vector3 a,Vector3 b,float t)=>a+(b-a)*Mathf.Clamp01(t);')
f=f.replace('public Quaternion rotation=>parent==null?localRotation:parent.rotation*localRotation;','public Quaternion rotation{get=>parent==null?localRotation:parent.rotation*localRotation;set=>localRotation=parent==null?value:Quaternion.Inverse(parent.rotation)*value;}public Vector3 eulerAngles;public void Rotate(float x,float y,float z,Space s){localRotation*=Quaternion.Euler(x,y,z);}')
f=f.replace('public struct Quaternion {','public enum Space {Self,World} public struct Quaternion {public static Quaternion Euler(Vector3 v)=>Euler(v.x,v.y,v.z);public static Quaternion Slerp(Quaternion a,Quaternion b,float t)=>new Quaternion{q=System.Numerics.Quaternion.Slerp(a.q,b.q,t)};')
f=f.replace('public static float deltaTime=.016f,time;','public static float deltaTime=.016f,time;public static int frameCount;')
f=f.replace('public static class Mathf{','public static class Mathf{public static float Min(float a,float b)=>Math.Min(a,b);public static float Max(float a,float b)=>Math.Max(a,b);public static float Pow(float a,float b)=>(float)Math.Pow(a,b);public static float SmoothStep(float a,float b,float t){t=Clamp01(t);return a+(b-a)*t*t*(3-2*t);}public static float DeltaAngle(float a,float b)=>b-a;')
(p/'Fixture.cs').write_text(f)
# Only imported visual boundary is substituted. Actual production eligibility below is
# narrowed to the stated forward, grounded, unhurt, starter/no-gear basic scenario.
e=(p/'OptionalPilotBoundary.cs').read_text().replace('private void ConfigureBlenderPilot() { blenderPilot=null;pilotVisible=false; }','private void ConfigureBlenderPilot() { blenderPilot=null;pilotVisible=false; }')
pilot=(r/'Assets/Scripts/Combat/CombatModel.BlenderPilot.cs').read_text()
e=e.replace('private static bool PilotStarterCompatible(ItemData item,ItemSlot slot) { return false; }',member(pilot,'private static bool PilotStarterCompatible('))
(p/'OptionalPilotBoundary.cs').write_text(e)
(p/'Exporter.cs').write_text(r'''using System;using System.Linq;using System.Collections.Generic;using System.Text.Json;using UnityEngine;using Emberfall;
class Exporter {
static float[] V(Vector3 v)=>new[]{v.x,v.y,v.z};
static void Main(){var host=new GameObject("baseline");var model=CombatModel.Hero(host.transform,HeroClass.Vanguard);model.ApplyEquipment(new ItemData{id="starter-w",name="初行长剑",slot=ItemSlot.Weapon,level=1,rarity=Rarity.Common},new ItemData{id="starter-a",name="初行战衣",slot=ItemSlot.Armor,level=1,rarity=Rarity.Common},new ItemData{id="starter-r",name="初行护符",slot=ItemSlot.Relic,level=1,rarity=Rarity.Common});UnityEngine.Object.Flush();var frames=new List<object>();var contacts=new List<float>();float cooldown=0;const float dt=1f/120f;for(int tick=0;tick<720;tick++){
Time.time=tick*dt;Time.deltaTime=dt;Time.frameCount=tick;cooldown-=dt;
model.SetLocomotion(new Vector3(0,0,6*dt),dt,6,true);
if(tick>=120&&tick<576&&cooldown<=0){cooldown=SkillDamageBudgets.BasicInterval(HeroClass.Vanguard);model.PlayAction(-1,true,cooldown);contacts.Add(Time.time);}
model.ExportTick(dt);
if(tick%5!=0)continue;
var parts=model.GetComponentsInChildren<MeshFilter>(false).Where(x=>x.sharedMesh?.vertices!=null).Select(x=>new{name=x.transform.name,vertices=x.sharedMesh.vertices.Select(v=>V(x.transform.TransformPoint(v))).ToArray(),triangles=x.sharedMesh.triangles}).ToArray();
frames.Add(new{index=tick/5,time=Time.time,phase=model.ExportPhase,speed=model.ExportSpeed,progress=model.ExportProgress,pilot=model.ExportPilot,parts});
}
System.IO.File.WriteAllText("OUTPUT",JsonSerializer.Serialize(new{fps=24,duration=6,simulationHz=120,walkSpeed=6,contacts,frames}));}}
'''.replace('OUTPUT',str(o/'motion.json')))
subprocess.run([dotnet,'run','--project',str(p/'Export.csproj')],env=dict(os.environ,DOTNET_CLI_HOME=str(o/'cli')),check=True)
