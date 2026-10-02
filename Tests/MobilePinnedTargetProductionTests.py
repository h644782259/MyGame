"""Real pointer, aim, action, targeting and charge methods with managed Unity boundary doubles."""
from pathlib import Path
import os,sys,tempfile,subprocess
r=Path(__file__).resolve().parents[1];dotnet=sys.argv[1] if len(sys.argv)>1 else 'dotnet'
def extract(path,signature):
 s=(r/'Assets/Scripts'/path).read_text();a=s.index(signature);b=s.index('{',a)+1;d=1
 while d:d+=(s[b]=='{')-(s[b]=='}');b+=1
 return s[a:b]
with tempfile.TemporaryDirectory(prefix='mobile-pin-production-') as t:
 p=Path(t)
 for path in ['Core/GameTypes.cs','Core/CombatBalance.cs','Core/MobileCameraGesture.cs','Core/SkillDamageBudgets.cs','Combat/MobileSkillPolicy.cs','Combat/PlayerController.MobileFocus.cs','Combat/SkillChargeController.cs','UI/MobileControlLayout.cs']:(p/Path(path).name).write_text((r/'Assets/Scripts'/path).read_text())
 (p/'Fixture.cs').write_text((r/'Tests/MobilePinnedTargetProductionTests.cs').read_text())
 player=['private Vector3 ResolveMobileAim(','internal void PrepareMobileSkillAim(','internal void ResolveMobileSkillAim(','private static bool ValidAimTarget(','private static bool ProjectedBounds(','private void FaceAim(','private EnemyController MagicConeTarget(','private void BasicAttack(','internal bool CastImmediateSkill(','internal bool ConfirmTargetedSkill(','internal bool ExecuteChargedSkill(']
 controls=['public bool ProcessPointer(','public static void ResetInput()','private void Update()']
 target=['public enum Shape','public struct Preview','public static Preview Describe(','public static bool RequiresConfirmation(','public bool Begin(','public void Cancel()']
 (p/'PlayerMethods.cs').write_text('using System.Collections.Generic;using UnityEngine;namespace Emberfall{public sealed partial class PlayerController{'+''.join(extract('Combat/PlayerController.cs',x) for x in player)+'}}')
 (p/'PointerMethods.cs').write_text('using System.Collections.Generic;using UnityEngine;namespace Emberfall{public sealed partial class MobileControls{'+''.join(extract('UI/MobileControls.cs',x) for x in controls)+'}}')
 (p/'TargetMethods.cs').write_text('using UnityEngine;namespace Emberfall{public partial class SkillTargetingController{'+''.join(extract('Combat/SkillTargetingController.cs',x) for x in target)+'}}')
 (p/'Program.cs').write_text('System.Console.WriteLine(MobilePinnedTargetProductionTests.Run());')
 project=p/'Test.csproj';project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><NuGetAudit>false</NuGetAudit><NoWarn>0649;0414</NoWarn></PropertyGroup></Project>');(p/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
 env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1');build=[dotnet,'build',str(project),'--configfile',str(p/'NuGet.Config'),'-v:q'];run=[dotnet,str(p/'bin/Debug/net8.0/Test.dll')]
 subprocess.run(build,env=env,check=True);subprocess.run(run,env=env,check=True)
 mutations=[('MobileCameraGesture.cs','((x-startX)*(x-startX)+(y-startY)*(y-startY))/(density*density)>=Threshold*Threshold','Math.Abs(y-startY)/density>=Threshold','horizontal out-and-back never taps'),('PlayerMethods.cs','if(pinned!=null){AimTarget=pinned;return CombatFx.Flat(pinned.transform.position);}','if(false){AimTarget=pinned;return CombatFx.Flat(pinned.transform.position);}','movement and closer enemy cannot replace pin'),('TargetMethods.cs','if(!owner.MobilePinnedActionAllowed(index,true))return false;','','blocked pin rejects actual targeted skill without charge or resource spending')]
 for name,before,after,oracle in mutations:
  file=p/name;original=file.read_text();assert before in original;file.write_text(original.replace(before,after))
  # For the final mutant remove all new-action pin guards to reproduce old fallback behavior.
  playerfile=p/'PlayerMethods.cs';playerOriginal=playerfile.read_text()
  if name=='TargetMethods.cs':playerfile.write_text(playerOriginal.replace(' || !MobilePinnedActionAllowed(skill,true)',''))
  subprocess.run(build,env=env,check=True,stdout=subprocess.DEVNULL)
  failed=subprocess.run(run,env=env,capture_output=True,text=True)
  assert failed.returncode and 'System.Exception: '+oracle in failed.stdout+failed.stderr,failed.stdout+failed.stderr
  file.write_text(original)
  if name=='TargetMethods.cs':playerfile.write_text(playerOriginal)
 print('PASS: three compiled old gesture/automatic-selection/action-fallback mutations fail exact production assertions')
