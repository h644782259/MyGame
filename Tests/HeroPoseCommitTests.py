#!/usr/bin/env python3
import os,sys,tempfile,subprocess,argparse
from pathlib import Path
root=Path(__file__).resolve().parents[1]
parser=argparse.ArgumentParser();parser.add_argument('dotnet',nargs='?',default=os.environ.get('DOTNET','dotnet'));parser.add_argument('--combat-model',type=Path);args=parser.parse_args();dotnet=args.dotnet
source=(args.combat_model or root/'Assets/Scripts/Combat/CombatModel.cs').read_text()
pilot_hook='if (SampleBlenderPilot(acting,t,hurt)) return;' in source
def method(signature):
 start=source.index(signature);i=source.index('{',start)+1;depth=1
 while depth:
  if source[i]=='{':depth+=1
  elif source[i]=='}':depth-=1
  i+=1
 return source[start:i]
with tempfile.TemporaryDirectory(prefix='hero-pose-commit-') as path:
 p=Path(path)
 for name in ['HeroMotionStyle','GameTypes','CombatBalance','SkillDamageBudgets','BasicActionTimeline','VisualMotionEnvelope','CasterPoseRecipe']:(p/(name+'.cs')).write_text((root/'Assets/Scripts/Core'/(name+'.cs')).read_text())
 for file in ['Assets/Scripts/Combat/CombatModel.Recovery.cs','Assets/Scripts/Combat/CombatModel.CastPoses.cs','Tests/HeroPoseCommitFixture.cs']:(p/Path(file).name).write_text((root/file).read_text())
 body='\n'.join(method(x) for x in ['public bool SwordActionActive','public bool TryClaimSwordRibbon(', 'public void PlayAction(','public void CancelAction(','public void ReleaseCharge(','private void CommitActionPose(','private static Quaternion Pose(','private void AnimateHero('])
 production=p/'HeroPose.cs';production.write_text('using UnityEngine;namespace Emberfall {public sealed partial class CombatModel {'+body+'}}')
 (p/'Program.cs').write_text('HeroPoseCommitTests.RequirePilotSample='+str(pilot_hook).lower()+';System.Console.WriteLine(HeroPoseCommitTests.Run());')
 project=p/'Test.csproj';project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><NoWarn>0649;0169</NoWarn></PropertyGroup></Project>')
 (p/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>')
 env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1',DOTNET_CLI_TELEMETRY_OPTOUT='1')
 subprocess.run([dotnet,'restore',str(project),'--configfile',str(p/'NuGet.Config'),'-v:q'],env=env,check=True)
 cmd=[dotnet,'run','--project',str(project),'--no-restore'];subprocess.run(cmd,env=env,check=True)
 original=production.read_text()
 for before,after,expected in [('CommitActionPose();','', 'committed action writes its release arm before any later Update'),('BeginVisualRecovery(false);','BeginVisualRecovery();','LateUpdate charge release replaces old charge arm and weapon immediately')]:
  production.write_text(original.replace(before,after))
  subprocess.run([dotnet,'build',str(project),'--no-restore','-v:q'],env=env,check=True,stdout=subprocess.DEVNULL)
  result=subprocess.run(cmd+['--no-build'],env=env,capture_output=True,text=True)
  if result.returncode==0 or 'System.Exception: '+expected not in result.stdout+result.stderr:raise AssertionError(result.stdout+result.stderr)
 production.write_text(original)
 if pilot_hook:
  production.write_text(original.replace('if (SampleBlenderPilot(acting,t,hurt)) return;','if (SampleBlenderPilot(acting,0,hurt)) return;'))
  subprocess.run([dotnet,'build',str(project),'--no-restore','-v:q'],env=env,check=True,stdout=subprocess.DEVNULL)
  result=subprocess.run(cmd+['--no-build'],env=env,capture_output=True,text=True)
  assert result.returncode and 'System.Exception: pilot receives contact phase synchronously at basic commit' in result.stdout+result.stderr,result.stdout+result.stderr
  production.write_text(original)
  print('PASS: compiled zero-phase pilot mutation fails synchronous contact forwarding assertion')
 print('PASS: 2 compiled old-pose/handoff negative controls hit exact runtime assertions')
