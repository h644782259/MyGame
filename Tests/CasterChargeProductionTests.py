#!/usr/bin/env python3
"""Actual charge, commit and family methods; same managed transform shell as pose regression."""
from pathlib import Path
exec((Path(__file__).with_name('HeroPoseCommitTests.py')).read_text().split('with tempfile.TemporaryDirectory')[0])
with tempfile.TemporaryDirectory(prefix='caster-charge-') as directory:
 p=Path(directory)
 for name in ['GameTypes','CombatBalance','SkillDamageBudgets','BasicActionTimeline','VisualMotionEnvelope','CasterPoseRecipe']:(p/(name+'.cs')).write_text((root/'Assets/Scripts/Core'/(name+'.cs')).read_text())
 for f in ['Assets/Scripts/Combat/CombatModel.Recovery.cs','Assets/Scripts/Combat/CombatModel.CastPoses.cs','Tests/CasterChargeProductionTests.cs']:(p/Path(f).name).write_text((root/f).read_text())
 fixture=(root/'Tests/HeroPoseCommitFixture.cs').read_text().replace('public static float Clamp01','public static float Lerp(float a,float b,float t)=>a+(b-a)*Clamp01(t);public static float Clamp01')
 (p/'Fixture.cs').write_text(fixture)
 signatures=['public bool SwordActionActive','public bool TryClaimSwordRibbon(', 'public void PlayAction(','public void CancelAction(','public void ReleaseCharge(','private void CommitActionPose(','private static Quaternion Pose(','private void AnimateHero(','public void AnimateCharge(float progress)','public void AnimateCharge(float progress,int skill)']
 body='using UnityEngine;namespace Emberfall {public sealed partial class CombatModel {'+'\n'.join(method(x) for x in signatures)+'}}'
 production=p/'Model.cs';production.write_text(body);(p/'Program.cs').write_text('System.Console.WriteLine(CasterChargeProductionTests.Run());')
 project=p/'Test.csproj';project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><NoWarn>0649;0169</NoWarn></PropertyGroup></Project>');(p/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>')
 env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1');cmd=[dotnet,'run','--project',str(project),'--no-restore'];subprocess.run([dotnet,'restore',str(project),'--configfile',str(p/'NuGet.Config'),'-v:q'],env=env,check=True);subprocess.run(cmd,env=env,check=True)
 for replacement in ['ApplyCasterSkillPose(poseTime,0);','rightArm.localRotation=Quaternion.Euler(-105,-12,25);staffRig.localRotation=Quaternion.Euler(80,0,-15);']:
  assert body.count('ApplyCasterSkillPose(poseTime,skill);')==1;production.write_text(body.replace('ApplyCasterSkillPose(poseTime,skill);',replacement));subprocess.run([dotnet,'build',str(project),'--no-restore','-v:q'],env=env,check=True,stdout=subprocess.DEVNULL)
  result=subprocess.run(cmd+['--no-build'],env=env,capture_output=True,text=True);assert result.returncode and 'System.Exception: actual skill charge converges to its committed family pose' in result.stdout+result.stderr,result.stdout+result.stderr
 print('PASS: wrong-skill and old generic charge compiled negative controls fail exact handoff assertion')
