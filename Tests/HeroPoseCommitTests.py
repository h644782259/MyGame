#!/usr/bin/env python3
import os,sys,tempfile,subprocess
from pathlib import Path
root=Path(__file__).resolve().parents[1]
dotnet=sys.argv[1] if len(sys.argv)>1 else os.environ.get('DOTNET','dotnet')
source=(root/'Assets/Scripts/Combat/CombatModel.cs').read_text()
def method(signature):
 start=source.index(signature);i=source.index('{',start)+1;depth=1
 while depth:
  if source[i]=='{':depth+=1
  elif source[i]=='}':depth-=1
  i+=1
 return source[start:i]
with tempfile.TemporaryDirectory(prefix='hero-pose-commit-') as path:
 p=Path(path)
 for name in ['GameTypes','CombatBalance','SkillDamageBudgets','BasicActionTimeline','VisualMotionEnvelope']:(p/(name+'.cs')).write_text((root/'Assets/Scripts/Core'/(name+'.cs')).read_text())
 for file in ['Assets/Scripts/Combat/CombatModel.Recovery.cs','Tests/HeroPoseCommitFixture.cs']:(p/Path(file).name).write_text((root/file).read_text())
 body='\n'.join(method(x) for x in ['public bool SwordActionActive','public bool TryClaimSwordRibbon(', 'public void PlayAction(','public void CancelAction(','public void ReleaseCharge(','private void CommitActionPose(','private static Quaternion Pose(','private void AnimateHero('])
 production=p/'HeroPose.cs';production.write_text('using UnityEngine;namespace Emberfall {public sealed partial class CombatModel {'+body+'}}')
 (p/'Program.cs').write_text('System.Console.WriteLine(HeroPoseCommitTests.Run());')
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
 print('PASS: 2 compiled old-pose/handoff negative controls hit exact runtime assertions')
