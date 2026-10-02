#!/usr/bin/env python3
import os,sys,tempfile,subprocess
from pathlib import Path
root=Path(__file__).resolve().parents[1];dotnet=sys.argv[1] if len(sys.argv)>1 else os.environ.get('DOTNET','dotnet')
source=(root/'Assets/Scripts/Combat/EnemyController.cs').read_text()
start=source.index('            if (session == null || !AdventureResultPolicy.AcceptsDamage',source.index('public void TakeDamage('));end=source.index('\n            if (CombatReviewEvents.Enabled)',start)
damage=source[start:end]
prop=source[source.index('public bool GuardArmorClosed'):source.index('\n',source.index('public bool GuardArmorClosed'))]
with tempfile.TemporaryDirectory(prefix='live-tactics-') as directory:
 path=Path(directory)
 for name in ['Combat/TacticalEnemyVisual','Combat/TacticalCaptureVisual','Combat/GuardArmorVisual','Core/GuardArmorRules']:(path/(name.split('/')[-1]+'.cs')).write_text((root/'Assets/Scripts'/(name+'.cs')).read_text())
 (path/'Fixture.cs').write_text((root/'Tests/TacticalLiveVisualTests.cs').read_text())
 unity=(root/'Tests/FilledVfxAllocationTests.cs').read_text().split('namespace UnityEngine',1)[1]
 # Reuse the existing explicit managed Unity substitutes, not any combat implementation.
 unity=unity.replace('public Vector3 right=>','public Vector3 forward=>localRotation.Rotate(new Vector3(0,0,1));public Vector3 right=>').replace('public float sqrMagnitude=>','public static float Dot(Vector3 a,Vector3 b)=>a.x*b.x+a.y*b.y+a.z*b.z;public float sqrMagnitude=>')
 unity=unity.replace('public struct Color{','public struct Color{public static Color white=>new Color(1,1,1);').replace('public float sqrMagnitude=>','public static Vector3 operator-(Vector3 a)=>new Vector3(-a.x,-a.y,-a.z);public float sqrMagnitude=>')
 unity=unity.replace('public int positionCount;','public int positionCount{get=>Positions.Length;set=>Array.Resize(ref Positions,value);}') .replace('public readonly Vector3[] Positions=new Vector3[4];','public Vector3[] Positions=new Vector3[0];')
 (path/'Unity.cs').write_text('using System;using System.Linq;using System.Reflection;using System.Collections.Generic;namespace UnityEngine'+unity)
 (path/'Enemy.cs').write_text('using UnityEngine;namespace Emberfall{public partial class EnemyController{'+prop+'public void TakeDamagePrefix(float amount,Vector3 direction){'+damage+'}}}')
 (path/'Program.cs').write_text('System.Console.WriteLine(TacticalLiveVisualTests.Run());')
 project=path/'Test.csproj';project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType></PropertyGroup></Project>');config=path/'NuGet.Config';config.write_text('<configuration><packageSources><clear /></packageSources></configuration>')
 subprocess.run([dotnet,'restore',str(project),'--configfile',str(config)],check=True)
 command=[dotnet,'run','--project',str(project),'--no-restore'];subprocess.run(command,check=True)
 visual=path/'TacticalEnemyVisual.cs';original=visual.read_text();visual.write_text(original.replace('supported=next;ward.enabled=next||Time.time<severedUntil;','if(supported&&!next)return; supported=next;ward.enabled=next||Time.time<severedUntil;'))
 result=subprocess.run(command,capture_output=True,text=True)
 assert result.returncode and 'support removal immediately extinguishes supported color' in result.stdout+result.stderr,result.stdout+result.stderr
 visual.write_text(original)
 guard=path/'GuardArmorVisual.cs';original=guard.read_text();guard.write_text(original.replace('bool closed=enemy.GuardArmorClosed;','bool closed=true;'))
 result=subprocess.run(command,capture_output=True,text=True)
 assert result.returncode and 'windup opens real weakpoint state' in result.stdout+result.stderr,result.stdout+result.stderr
 print('PASS: stale support visual and permanently closed armor mutations fail actual production components')
for file in ['GameSession.RoomTactics.cs','GameSession.Chapter.cs']:
 body=(root/'Assets/Scripts/Core'/file).read_text();assert 'CombatFx.Ring(' not in body
assert 'guardArmorVisual=GuardArmorVisual.Attach(this)' in source
print('PASS: tactical heartbeat rings removed and guard initialization connected; managed engine substitutes, no GPU acceptance')
