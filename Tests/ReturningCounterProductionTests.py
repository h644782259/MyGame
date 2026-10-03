"""Production basic/melee/perfect-dodge + ground geometry; managed engine recipients."""
from pathlib import Path
import sys,subprocess,tempfile,os
r=Path(__file__).resolve().parents[1];dotnet=sys.argv[1]
def member(s,key):
 a=s.index(key);b=s.index('{',a)+1;d=1
 while d:d+=(s[b]=='{')-(s[b]=='}');b+=1
 return s[a:b]
s=(r/'Assets/Scripts/Combat/PlayerController.cs').read_text()
methods=''.join(member(s,k) for k in ['private void BasicAttack(','private bool Melee(','public void NotifyPerfectDodge(','private bool ReturningCounterVariant'])
# Execute the unchanged complete proc branch separately; surrounding on-hit features are independent.
branch=member(s,'if (HeroClass == HeroClass.Vanguard && HasMechanic(EquipmentMechanic.ReturningBlade) && !ReturningCounterVariant)')
math=(r/'Tests/DestructibleTraversalTests.cs').read_text();math=math[math.index('namespace UnityEngine'):]
math=math.replace('public static Vector3 zero=>new Vector3();','public static Vector3 zero=>new Vector3();public static Vector3 up=>new Vector3(0,1,0);public static float Angle(Vector3 a,Vector3 b)=>(float)(Math.Acos(Math.Max(-1,Math.Min(1,Dot(a.normalized,b.normalized))))*180/Math.PI);')
with tempfile.TemporaryDirectory(prefix='return-counter-') as t:
 p=Path(t)
 for f in ['Core/GameTypes','Core/CombatBalance','Core/SkillDamageBudgets','Combat/PlayerUpgradeRules','Combat/ReturningCounterRules','World/WorldTraversal','Core/CombatSightRules','Combat/CombatSight']:(p/(Path(f).name+'.cs')).write_text((r/('Assets/Scripts/'+f+'.cs')).read_text())
 (p/'Math.cs').write_text('using System;'+math)
 (p/'Fixture.cs').write_text((r/'Tests/ReturningCounterFixture.cs').read_text())
 (p/'Player.cs').write_text('using UnityEngine;namespace Emberfall{public partial class PlayerController{'+methods+'void Proc(EnemyController enemy){Vector3 position=enemy.transform.position;'+branch+'}}}')
 (p/'Test.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><NoWarn>0649;0414</NoWarn></PropertyGroup></Project>');(p/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
 env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1');build=[dotnet,'build',str(p/'Test.csproj'),'--configfile',str(p/'NuGet.Config'),'-v:q'];run=[dotnet,str(p/'bin/Debug/net8.0/Test.dll')]
 subprocess.run(build,env=env,check=True);subprocess.run(run,env=env,check=True)
 for old,new,expected in [('&& !ReturningCounterVariant)','&& true)','B no bounce or return grant'),('ReturningCounterVariant ? 3f','ReturningCounterVariant ? 2f','B actual three-second window'),('<= .35f+enemy.HitFootprintBonus','<= 3.5f+enemy.HitFootprintBonus','B narrow side rejection')]:
  f=p/'Player.cs';original=f.read_text();assert old in original;f.write_text(original.replace(old,new));subprocess.run(build,env=env,check=True);result=subprocess.run(run,env=env,capture_output=True,text=True);assert result.returncode and expected in result.stdout+result.stderr,result.stdout+result.stderr;f.write_text(original);print('PASS compiled negative control: '+expected)
