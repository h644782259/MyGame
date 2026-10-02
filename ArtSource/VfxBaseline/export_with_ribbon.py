# Run after existing exporter prepares build/, then extend it without changing pinned runtime sources.
from pathlib import Path
exec(Path(__file__).with_name('export_baseline.py').read_text().split('subprocess.run(')[0])
for n in ['GameTypes','CombatBalance','SkillDamageBudgets','BasicActionTimeline','VisualMotionEnvelope','CasterPoseRecipe','WeaponStructure']:(build/(n+'.cs')).write_text(source('Assets/Scripts/Core/'+n+'.cs'))
for n in ['CombatModel.Recovery','CombatModel.CastPoses','CombatModel.WeaponRig']:(build/(n+'.cs')).write_text(source('Assets/Scripts/Combat/'+n+'.cs'))
model=source('Assets/Scripts/Combat/CombatModel.cs');body='\n'.join(extract(model,x) for x in ['public bool SwordActionActive','public bool TryClaimSwordRibbon(', 'public void PlayAction(','public void CancelAction(','public void ReleaseCharge(','private void CommitActionPose(','private static Quaternion Pose(','private void AnimateHero(']);(build/'HeroPose.cs').write_text('using UnityEngine;namespace Emberfall {public sealed partial class CombatModel {'+body+'}}')
pose=source('Tests/HeroPoseCommitFixture.cs');pose=pose[pose.index('namespace Emberfall'):pose.index('public static class HeroPoseCommitTests')]
pose=pose.replace(' public enum WeaponVisualAnchor {BowGrip,BowNock}','').replace('private readonly Transform transform=new Transform();','public readonly Transform transform=new Transform();public int WeaponActionId=>weaponActionId;')
pose=pose.replace('  private int WeaponSwingSide=>swingCount%2==0?1:-1;private Vector3 WeaponAnchorLocal(WeaponVisualAnchor a)=>Vector3.zero;','')
pose=pose.replace('public CombatModel(HeroClass hero){heroClass=hero;}','''public CombatModel(HeroClass hero){heroClass=hero;phase=3.14f;
 spine.parent=transform;spine.localPosition=new Vector3(0,1.12f,0);
 rightArm.parent=spine;rightArm.localPosition=new Vector3(.47f*1.15f,.45f,0);
 leftArm.parent=spine;leftArm.localPosition=new Vector3(-.47f*1.15f,.45f,0);
 rightElbow.parent=rightArm;rightElbow.localPosition=new Vector3(0,-.30f,0);
 leftElbow.parent=leftArm;leftElbow.localPosition=new Vector3(0,-.30f,0);
 swordRig.parent=rightElbow;swordRig.localPosition=new Vector3(0,-.23f,.04f);
 } public void StartOrdinal(int ordinal){swingCount=ordinal-1;} public void SampleCastAge(float age){actionAge=.68f*.52f+age;CommitActionPose();}
 public object ExportSockets(){Vector3 root,tip;TryGetWeaponVisualAnchor(WeaponVisualAnchor.SwordRoot,out root);TryGetWeaponVisualAnchor(WeaponVisualAnchor.SwordTip,out tip);return new{root=new[]{root.x,root.y,root.z},tip=new[]{tip.x,tip.y,tip.z}};}''')
(build/'PoseFixture.cs').write_text('using System;using UnityEngine;'+pose)
ribbon=extract(source('Assets/Scripts/Combat/WeaponVisualLinks.cs'),'internal sealed class WeaponSlashRibbon');(build/'Ribbon.cs').write_text('using UnityEngine;namespace Emberfall {'+ribbon+'}')
f=(build/'Fixture.cs').read_text().replace('public Vector3 localPosition,localScale=Vector3.one;','public Vector3 localPosition,localScale=Vector3.one,eulerAngles;')
f=f.replace('public static Quaternion identity=>','public static Quaternion Slerp(Quaternion a,Quaternion b,float t)=>new Quaternion{value=System.Numerics.Quaternion.Slerp(a.value,b.value,t)};public static Quaternion Euler(Vector3 v)=>Euler(v.x,v.y,v.z);public static Quaternion identity=>')
f=f.replace('public static float Abs(float value)', 'public static float SmoothStep(float a,float b,float t){t=Clamp01(t);return a+(b-a)*t*t*(3-2*t);}public static float DeltaAngle(float a,float b)=>b-a;public static float Abs(float value)')
(build/'Fixture.cs').write_text(f)
p=(build/'Program.cs').read_text();p=p.replace('if(casts[cast].slot==1&&age<.34f){','''if(casts[cast].slot==1&&age<.22f){
    var model=new CombatModel(HeroClass.Vanguard);model.StartOrdinal(cast+1);Time.time=casts[cast].time;Time.frameCount=cast;model.PlayAction(1,false);
    WeaponSlashRibbon.Spawn(hero,model,new Color(1,.85f,.4f));var trail=GameObject.All.Last(o=>o.GetComponent<WeaponSlashRibbon>()!=null);
    int ticks=(int)Math.Round(age*24);for(int tick=1;tick<=ticks;tick++){Time.time=casts[cast].time+tick/24f;model.SampleCastAge(tick/24f);Time.deltaTime=1/24f;trail.Call("LateUpdate");}
    if(trail.activeInHierarchy){var mesh=trail.GetComponent<MeshFilter>().sharedMesh;string key=mesh.name;if(!meshes.ContainsKey(key))meshes[key]=new{vertices=mesh.vertices.Select(V).ToArray(),triangles=mesh.triangles,uv=mesh.vertices.Select(v=>new[]{0f,0f}).ToArray()};objects.Add(new{id=$"cast{cast}/swordRibbon",mesh=key,vertices=mesh.vertices.Select(V).ToArray(),color=C(trail.GetComponent<MeshRenderer>().sharedMaterial.color),opacity=1f,progress=0f,style=0f,sockets=model.ExportSockets()});}
   }
   if(casts[cast].slot==1&&age<.34f){''')
p=p.replace('"Dynamic sword root-tip ribbon not exported: needs actual animated weapon sockets; do not represent as absent from baseline game."','"Sword ribbon executes pinned Spawn/LateUpdate/Sample using pinned PlayAction/AnimateHero/Pose and WeaponRig anchors, original unit-scale chain, zero locomotion, breathphase3.14 and24Hz sample history. Headless managed TRS, not Unity playback."')
p=p.replace('baseline-vfx-03422ab.json','baseline-vfx-with-ribbon-03422ab.json')
(build/'Program.cs').write_text(p)
subprocess.run([DOTNET,'run','--project',str(build/'Export.csproj')],env=dict(os.environ,DOTNET_CLI_HOME=str(OUT/'cli'),EMBERFALL_VFX_OUTPUT=str(OUT/'baseline-vfx-with-ribbon-03422ab.json')),check=True)
paths+=['Assets/Scripts/Combat/CombatEffects.cs','Assets/Scripts/Combat/CombatModel.cs','Assets/Scripts/Combat/CombatModel.WeaponRig.cs','Assets/Scripts/Combat/CombatModel.Recovery.cs','Assets/Scripts/Combat/CombatModel.CastPoses.cs','Assets/Scripts/Combat/WeaponVisualLinks.cs','Assets/Scripts/Core/SkillDamageBudgets.cs','Assets/Scripts/Core/WeaponStructure.cs','Tests/HeroPoseCommitFixture.cs','Tests/FilledVfxAllocationTests.cs','Assets/Resources/FilledSpell.shader']
(OUT/'source-manifest.json').write_text(json.dumps({'pin':PIN,'sources':{x:hashlib.sha256(source(x).encode()).hexdigest() for x in paths}},indent=2))
