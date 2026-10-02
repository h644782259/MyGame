#!/usr/bin/env python3
"""Replay actual IMGUI skill-slot method. Geometry/query tests live in ShatterAvailability;
engine drawing and host observation are recording boundaries, not a Unity render test."""
import os,sys,tempfile,subprocess
from pathlib import Path
root=Path(__file__).resolve().parents[1];dotnet=sys.argv[1] if len(sys.argv)>1 else os.environ.get('DOTNET','dotnet')
def member(s,sig):
 a=s.index(sig);b=s.index('{',a)+1;d=1
 while d:d+=(s[b]=='{')-(s[b]=='}');b+=1
 return s[a:b]
s=(root/'Assets/Scripts/UI/GameUI.MobileFeedback.cs').read_text()
methods=''.join(member(s,x) for x in ('private string MobileSkillState(', 'private void DrawMobileSkillAvailability('))
shell=r'''
using System;using System.Collections.Generic;using UnityEngine;
namespace UnityEngine {
 public struct Rect {public float x,y,width,height;public float xMax=>x+width;public float yMax=>y+height;public Rect(float a,float b,float c,float d){x=a;y=b;width=c;height=d;}}
 public struct Color{public Color(float r,float g,float b,float a=1){}}
 public enum TextAnchor{MiddleCenter}
}
namespace Emberfall {
 public enum HeroClass{Arcanist}
 public static class GameBalance {public static bool IsPassive(int skill)=>skill==3||skill==8;public static float SkillEnergyCost(HeroClass hero,int skill)=>10;}
 public class Profile {public int[] skillRanks={1,1,1,1,1,1,1,1,1,1};public HeroClass heroClass;}
 public class Progression {public Profile Profile=new Profile();}
 public class SkillChargeController {public bool IsCharging;public int SkillIndex;public float Progress=.5f;}
 public class PlayerController {public float Energy=100,Cooldown;public SkillChargeController Charge=new SkillChargeController();public CombatOpportunityState Observation;public int ObservedSkill=-1;public T GetComponent<T>()where T:class=>Charge as T;public float SkillCooldownRemaining(int skill)=>Cooldown;public CombatOpportunityState SkillOpportunity(int skill){ObservedSkill=skill;return Observation;}}
 public class GameSession {public Progression Progression=new Progression();public PlayerController Player=new PlayerController();public bool ChallengeRun,InDungeon;public int HealingCharges=1;public string Failure="";public string ControlFailure(string key)=>Failure;}
 public partial class GameUI {
  GameSession session=new GameSession();float TouchRatio=1;Color gold=new Color(),jade=new Color();List<string> labels=new List<string>();
  int TouchFont(int n)=>n;void Fill(Rect r,Color c){}void Bar(Rect r,float n,Color c){}void Text(Rect r,string s,int size,Color color,bool bold,bool wrap,TextAnchor anchor){labels.Add(s);}
  static void Check(bool ok,string why){if(!ok)throw new Exception(why);}
  public static void Run(){var ui=new GameUI();var p=ui.session.Player;var r=new Rect(0,0,48,48);int n=0;
   p.Observation=new CombatOpportunityState(CombatOpportunityKind.Shatter,1.25f);ui.DrawMobileSkillAvailability(r,1);Check(p.ObservedSkill==1&&ui.labels.Contains("碎冰 1.3"),"actual skill slot displays typed opportunity and actual expiry");n++;
   ui.labels.Clear();p.Energy=0;ui.DrawMobileSkillAvailability(r,1);Check(ui.labels.Contains("缺能")&&!ui.labels.Exists(x=>x.StartsWith("碎冰")),"no-energy slot cannot show opportunity even with stale observation");n++;
   p.Energy=100;p.Cooldown=1;ui.labels.Clear();ui.DrawMobileSkillAvailability(r,1);Check(!ui.labels.Exists(x=>x.StartsWith("碎冰")),"cooldown slot never advertises ready opportunity");n++;
   p.Cooldown=0;p.Charge.IsCharging=true;p.Charge.SkillIndex=1;ui.labels.Clear();ui.DrawMobileSkillAvailability(r,1);Check(ui.labels.Contains("蓄力")&&!ui.labels.Exists(x=>x.StartsWith("碎冰")),"captured charge remains charging instead of next-action opportunity");n++;
   p.Charge.IsCharging=false;ui.session.Failure="目标被遮挡";ui.labels.Clear();ui.DrawMobileSkillAvailability(r,1);Check(ui.labels[ui.labels.Count-1]=="目标被遮挡","explicit action rejection has final caption priority");n++;
   ui.session.Failure="";p.Observation=default;ui.labels.Clear();ui.DrawMobileSkillAvailability(r,1);Check(ui.labels.Count==0,"expired observation leaves ordinary slot ready marker");n++;
   p.Observation=new CombatOpportunityState(CombatOpportunityKind.EmpoweredContract,7);ui.labels.Clear();ui.DrawMobileSkillAvailability(r,4);Check(p.ObservedSkill==4&&ui.labels.Contains("强化 7.0"),"each actual slot queries its own skill identity");n++;
   ui.labels.Clear();ui.DrawMobileSkillAvailability(r,3);Check(ui.labels.Count==0,"passive slot retains icon without actionable caption");n++;
   Console.WriteLine("PASS: "+n+" actual skill-slot caption/priority observations (managed draw recorder)");
  }
 }
}
class Program {static void Main(){Emberfall.GameUI.Run();}}
'''
with tempfile.TemporaryDirectory(prefix='opportunity-slot-') as t:
 p=Path(t)
 for relative in ('Assets/Scripts/Core/CombatOpportunityState.cs','Assets/Scripts/UI/MobileCombatPresentation.cs'):(p/Path(relative).name).write_text((root/relative).read_text())
 (p/'Shell.cs').write_text(shell);method=p/'Methods.cs';original='using UnityEngine;namespace Emberfall{public partial class GameUI{'+methods+'}}';method.write_text(original)
 project=p/'Test.csproj';project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType></PropertyGroup></Project>');config=p/'NuGet.Config';config.write_text('<configuration><packageSources><clear/></packageSources></configuration>')
 def build():subprocess.run([dotnet,'build',str(project),'--configfile',str(config),'-v:q'],check=True,stdout=subprocess.DEVNULL)
 command=[dotnet,str(p/'bin/Debug/net8.0/Test.dll')];build();subprocess.run(command,check=True)
 old=original.replace('if(state.Length==0&&opportunity.Actionable)','if(false)');assert old!=original;method.write_text(old);build();r=subprocess.run(command,text=True,capture_output=True)
 assert r.returncode!=0 and 'System.Exception: actual skill slot displays typed opportunity and actual expiry' in r.stdout+r.stderr,r.stdout+r.stderr
 print('PASS: compiled old ready-dot-only slot fails precise caption assertion')
