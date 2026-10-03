#!/usr/bin/env python3
"""Real practice dispatch with ordinary Enemy AI and projectile traversal, managed recipients."""
from pathlib import Path
import os,sys,tempfile,subprocess
root=Path(__file__).resolve().parents[1]
ns={'__file__':str(root/'Tests/ThreatAdmissionFairnessProductionTests.py')}
exec((root/'Tests/ThreatAdmissionFairnessProductionTests.py').read_text().split('with tempfile.TemporaryDirectory')[0],ns)
fixture=ns['fixture'].split('class FairnessProgram')[0];body=ns['body'];math=ns['math'];sdk=sys.argv[1] if len(sys.argv)>1 else 'dotnet'
def member(s,k):
 a=s.index(k);b=s.index('{',a)+1;n=1
 while n:n+=(s[b]=='{')-(s[b]=='}');b+=1
 return s[a:b]
fixture=fixture.replace('public enum CampPracticeScenario {Stationary,Moving,FrontAndSupplier}','').replace('public class PracticeRecordBoundary {public CampPracticeScenario Scenario;}','').replace('public class GameSession{','public partial class GameSession{').replace('PracticeRecordBoundary PracticeRecord','CampPracticeRecord PracticeRecord')
fixture=fixture.replace('public bool MovePracticeTarget(EnemyController enemy){throw new Exception("practice outside blocked-combat fixture scope");}','')
fixture=fixture.replace('public class Status{','public class Status{public bool IsFrozen,KnockedDown,IsAirborne;')
fixture=fixture.replace('public bool IsAggro=>aggro;','public bool IsStunned=>stunTime>0;public bool IsAggro=>aggro;')
fixture=fixture.replace('string DisplayName{get{return "guardian";}}','public string DisplayName="guardian";public float MaxHealth=73;')
fixture=fixture.replace('private void Update(){throw new Exception("projectile Update outside fairness fixture; lifecycle has separate suite");}','')
fixture=fixture.replace('if(onEnded!=null)onEnded();','var bolt=new CombatProjectile(s);bolt.transform.position=p;bolt.direction=dir;bolt.damage=new CombatDamage(dmg);bolt.speed=speed;Spawned.Add(bolt);if(onEnded!=null)onEnded();')
fixture+='''namespace Emberfall{public partial class CombatProjectile{public static List<CombatProjectile> Spawned=new List<CombatProjectile>();}public partial class EnemyController{public void SetPressure(EnemyKind k){Kind=k;preparing=false;aggro=true;attackCooldown=0;Health=MaxHealth;}public bool WarnPressure(){attackForward=(session.Player.transform.position-transform.position).normalized;return PrepareAttack(Kind==EnemyKind.Wisp?AttackType.Bolt:AttackType.Slam,false,session.Player.transform.position);}}}'''
session=(root/'Assets/Scripts/Core/GameSession.Practice.cs').read_text();enemy=(root/'Assets/Scripts/Combat/EnemyController.cs').read_text();fx=(root/'Assets/Scripts/Combat/CombatEffects.cs').read_text()
body+='usingPLACEHOLDER'.replace('usingPLACEHOLDER','namespace Emberfall{public partial class GameSession{'+member(session,'public bool MovePracticeTarget(')+'}public partial class EnemyController{'+member(enemy,'public void ConfigurePracticeTarget()')+'}public partial class CombatProjectile{'+member(fx[fx.index('internal sealed class CombatProjectile'):],'private void Update()')+'}}')
program='''using System;using UnityEngine;using Emberfall;class Program{static int count;static void C(bool v,string s){count++;if(!v)throw new Exception(s);}static void Main(){
 foreach(var scenario in new[]{CampPracticeScenario.GuardAndWispPressure,CampPracticeScenario.SupplierPressure}){
 WorldTraversal.Reset(ZoneKind.Dungeon);Time.deltaTime=.1f;var game=new GameSession{PracticeActive=true,PracticeRecord=new CampPracticeRecord(scenario,60,"actual")};game.PracticeRecord.Prepare();game.InputBlocked=true;
 var guard=new EnemyController(game);game.Enemies.Add(guard);guard.SetPressure(EnemyKind.Guardian);guard.ConfigurePracticeTarget();C(guard.Health==73&&guard.MaxHealth==73,"practice retains ordinary initialized health");
 C(guard.WarnPressure(),"real ordinary guard warning starts");float remaining=guard.Windup;for(int i=0;i<20;i++)guard.Tick();C(guard.Windup==remaining&&game.Player.DamageCalls==0&&game.PracticeRecord.Elapsed==0,"preparation cannot advance warning damage or time");
 game.PracticeRecord.Start();game.InputBlocked=false;for(int i=0;i<8;i++)guard.Tick();C(game.Player.DamageCalls==0&&guard.Windup>0,"pressure keeps full ordinary warning window");guard.Tick();C(game.Player.DamageCalls==1&&game.Player.Health<100,"real pressure ordinary AI resolves actual damage dispatch");
 WorldTraversal.Reset(ZoneKind.Dungeon);game.Player.Health=100;game.Player.DamageCalls=0;var blocked=new EnemyController(game);blocked.SetPressure(EnemyKind.Guardian);game.Enemies.Add(blocked);blocked.WarnPressure();WorldTraversal.AddBox(new Vector3(0,0,-.5f),new Vector2(3,.2f));for(int i=0;i<9;i++)blocked.Tick();C(game.Player.DamageCalls==0,"real pressure impact path respects obstruction");
 WorldTraversal.Reset(ZoneKind.Dungeon);var wisp=new EnemyController(game);wisp.SetPressure(EnemyKind.Wisp);wisp.transform.position=new Vector3(0,0,-4);game.Enemies.Add(wisp);CombatProjectile.Spawned.Clear();wisp.WarnPressure();for(int i=0;i<7;i++)wisp.Tick();C(CombatProjectile.Spawned.Count==0,"wisp keeps .72 second warning");wisp.Tick();C(CombatProjectile.Spawned.Count==2,"actual wisp resolver emits normal two projectiles");
 foreach(var bolt in CombatProjectile.Spawned)for(int i=0;i<30;i++)bolt.Tick();C(game.Player.DamageCalls>0,"actual hostile projectile sweep reaches recipient");
 }
 foreach(var scene in new[]{CampPracticeScenario.Stationary,CampPracticeScenario.Moving,CampPracticeScenario.FrontAndSupplier}){WorldTraversal.Reset(ZoneKind.Dungeon);var game=new GameSession{PracticeActive=true,PracticeRecord=new CampPracticeRecord(scene,10,"")};var e=new EnemyController(game);game.Enemies.Add(e);for(int i=0;i<100;i++)e.Tick();C(game.Player.DamageCalls==0,"original passive scenes never invoke hostile AI");}
 Console.WriteLine("PASS "+count+" actual Enemy Update/Prepare/Resolve/WorldTraversal/Projectile pressure assertions; rendering and final recipient are managed boundaries");}}'''
with tempfile.TemporaryDirectory(prefix='practice-pressure-') as d:
 p=Path(d);actual=p/'Actual.cs';actual.write_text(body);(p/'Fixture.cs').write_text(fixture);(p/'Math.cs').write_text(math);(p/'Program.cs').write_text(program)
 catalog=(root/'Assets/Scripts/Core/GameTypes.cs').read_text();(p/'Budget.cs').write_text('namespace Emberfall{public static class BuildCatalog{'+member(catalog,'public static float ConcentratedVenomCoefficient(')+'}}')
 for rel in ['Core/CampPracticeRecord','Core/ThreatAdmissionPolicy','Core/DestructiblePropRules','Combat/ConcentratedVenomRules','World/WorldTraversal','Combat/EnemyImpactRegion']:(p/(Path(rel).name+'.cs')).write_text((root/'Assets/Scripts'/(rel+'.cs')).read_text())
 project=p/'Test.csproj';project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><NoWarn>0649;0414;0169</NoWarn></PropertyGroup></Project>');(p/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>');env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1')
 def run(args):
  q=subprocess.run([sdk]+args,env=env,text=True,capture_output=True);print(q.stdout+q.stderr);return q
 assert run(['run','--project',str(project)]).returncode==0
 for old,new,message in [('if(PracticeRecord.UsesEnemyAI&&PracticeRecord.Started&&!PracticeRecord.Finished)return false;','','real pressure ordinary AI resolves actual damage dispatch'),('DisplayName=session.PracticeRecord.HasSupplier','MaxHealth=Health=1000000;DisplayName=session.PracticeRecord.HasSupplier','practice retains ordinary initialized health')]:
  assert old in body;actual.write_text(body.replace(old,new));assert run(['build',str(project),'--no-restore','-v:q']).returncode==0;q=run([str(p/'bin/Debug/net8.0/Test.dll')]);assert q.returncode!=0 and message in q.stdout+q.stderr;print('PASS compiled old behavior rejected:',message);actual.write_text(body)
