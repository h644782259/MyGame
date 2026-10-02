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
methods+=member((root/'Assets/Scripts/UI/GameUI.ControlPreferences.cs').read_text(),'private Rect MobileVisualRect(')
methods+=member((root/'Assets/Scripts/UI/GameUI.Mobile.cs').read_text(),'private int TouchFont(')
shell=r'''
using System;using System.Collections.Generic;using UnityEngine;
namespace UnityEngine {
 public struct Vector2{public float x,y;public Vector2(float a,float b){x=a;y=b;}}
 public static class Mathf{public static int RoundToInt(float x)=>(int)Math.Round(x);}
 public struct Rect {public Vector2 center=>new Vector2(x+width*.5f,y+height*.5f);public float x,y,width,height;public float xMax=>x+width;public float yMax=>y+height;public Rect(float a,float b,float c,float d){x=a;y=b;width=c;height=d;}}
 public struct Color{public Color(float r,float g,float b,float a=1){}}
 public enum TextAnchor{MiddleCenter}
}
namespace Emberfall {
 public static class EffectPreferences{public static float TouchVisualScale=.86f;}
 public enum HeroClass{Arcanist}
 public static class GameBalance {public static bool IsPassive(int skill)=>skill==3||skill==8;public static float SkillEnergyCost(HeroClass hero,int skill)=>10;}
 public class Profile {public int[] skillRanks={1,1,1,1,1,1,1,1,1,1};public HeroClass heroClass;}
 public class Progression {public Profile Profile=new Profile();}
 public class SkillChargeController {public bool IsCharging;public int SkillIndex;public float Progress=.5f;}
 public class PlayerController {public string TargetReason="";public int TargetSkill=-1;public string MobilePinnedActionReason(int skill){TargetSkill=skill;return TargetReason;}public float Energy=100,Cooldown;public SkillChargeController Charge=new SkillChargeController();public CombatOpportunityState Observation;public int ObservedSkill=-1;public T GetComponent<T>()where T:class=>Charge as T;public float SkillCooldownRemaining(int skill)=>Cooldown;public CombatOpportunityState SkillOpportunity(int skill){ObservedSkill=skill;return Observation;}}
 public class GameSession {public Progression Progression=new Progression();public PlayerController Player=new PlayerController();public bool ChallengeRun,InDungeon;public int HealingCharges=1;public string Failure="";public string FailureKey;public string ControlFailure(string key){FailureKey=key;return Failure;}}
 public partial class GameUI {
  GameSession session=new GameSession();float TouchRatio=1;Color gold=new Color(),jade=new Color();List<string> labels=new List<string>();
  struct Drawn {public Rect Rect;public string Text;public int Font;public bool Wrap;}List<Drawn> drawn=new List<Drawn>();
  void Fill(Rect r,Color c){}void Bar(Rect r,float n,Color c){}void Text(Rect r,string s,int size,Color color,bool bold,bool wrap,TextAnchor anchor){labels.Add(s);drawn.Add(new Drawn{Rect=r,Text=s,Font=size,Wrap=wrap});}
  static void Check(bool ok,string why){if(!ok)throw new Exception(why);}
  public static void Run(){var ui=new GameUI();var p=ui.session.Player;var r=new Rect(0,0,48,48);int n=0;
   var minimum=new MobileControlLayout(568,320,163);Check(minimum.Skills[1].Width==48&&minimum.Scale==1,"actual minimum touch layout fixture");n++;
   var area=minimum.Skills[1];var compact=ui.MobileVisualRect(new Rect(area.X,area.Y,area.Width,area.Height));
   Check(Math.Abs(compact.width-41.28f)<.001f,"actual compact visual rectangle uses .86 preference");n++;
   p.Observation=default;ui.DrawMobileSkillAvailability(r,1);Check(ui.labels.Count==0,"inactive observation cannot draw an opportunity caption");n++;ui.drawn.Clear();ui.labels.Clear();
   foreach(string reason in new[]{"目标被遮挡","距离不足"})foreach(bool explicitFailure in new[]{false,true}) {
    p.Observation=default;p.TargetReason=explicitFailure?"":reason;ui.session.Failure=explicitFailure?reason:"";ui.drawn.Clear();ui.labels.Clear();
    ui.DrawMobileSkillAvailability(compact,1);Check(ui.drawn.Count==1,"each rejection path draws one slot caption");n++;
    var text=ui.drawn[0];float conservativeWidth=text.Text.Length*text.Font*1.1f+2;
    Check(!text.Wrap&&conservativeWidth<=text.Rect.width,"actual rejection caption fits minimum compact CJK width");n++;
    Check(text.Text==(reason=="目标被遮挡"?"被遮挡":"太远"),"only display reason is compacted");n++;
    Check((explicitFailure?ui.session.Failure:p.TargetReason)==reason&&p.TargetSkill==1&&ui.session.FailureKey=="skill1","full semantic rejection and skill identity remain intact");n++;
   }
   p.TargetReason="";ui.session.Failure="";ui.labels.Clear();
   p.Observation=new CombatOpportunityState(CombatOpportunityKind.Shatter,1.25f);ui.DrawMobileSkillAvailability(r,1);Check(p.ObservedSkill==1&&ui.labels.Contains("碎冰 1.3"),"actual skill slot displays typed opportunity and actual expiry");n++;
   p.TargetReason="距离不足";ui.labels.Clear();ui.DrawMobileSkillAvailability(r,1);Check(p.TargetSkill==1&&ui.labels.Contains("太远")&&!ui.labels.Exists(x=>x.StartsWith("碎冰")),"pinned rejection is queried for actual skill and suppresses opportunity");n++;p.TargetReason="";
   ui.labels.Clear();p.Energy=0;ui.DrawMobileSkillAvailability(r,1);Check(ui.labels.Contains("缺能")&&!ui.labels.Exists(x=>x.StartsWith("碎冰")),"no-energy slot cannot show opportunity even with stale observation");n++;
   p.Energy=100;p.Cooldown=1;ui.labels.Clear();ui.DrawMobileSkillAvailability(r,1);Check(!ui.labels.Exists(x=>x.StartsWith("碎冰")),"cooldown slot never advertises ready opportunity");n++;
   p.Cooldown=0;p.Charge.IsCharging=true;p.Charge.SkillIndex=1;ui.labels.Clear();ui.DrawMobileSkillAvailability(r,1);Check(ui.labels.Contains("蓄力")&&!ui.labels.Exists(x=>x.StartsWith("碎冰")),"captured charge remains charging instead of next-action opportunity");n++;
   p.Charge.IsCharging=false;ui.session.Failure="目标被遮挡";ui.labels.Clear();ui.DrawMobileSkillAvailability(r,1);Check(ui.labels[ui.labels.Count-1]=="被遮挡","explicit action rejection has final caption priority");n++;
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
 for relative in ('Assets/Scripts/Core/CombatOpportunityState.cs','Assets/Scripts/UI/MobileCombatPresentation.cs','Assets/Scripts/UI/MobileControlLayout.cs'):(p/Path(relative).name).write_text((root/relative).read_text())
 (p/'Shell.cs').write_text(shell);method=p/'Methods.cs';original='using UnityEngine;namespace Emberfall{public partial class GameUI{'+methods+'}}';method.write_text(original)
 project=p/'Test.csproj';project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType></PropertyGroup></Project>');config=p/'NuGet.Config';config.write_text('<configuration><packageSources><clear/></packageSources></configuration>')
 def build():
  result=subprocess.run([dotnet,'build',str(project),'--configfile',str(config),'-v:q'],capture_output=True,text=True)
  if result.returncode:print(result.stdout+result.stderr);result.check_returncode()
 command=[dotnet,str(p/'bin/Debug/net8.0/Test.dll')];build();subprocess.run(command,check=True)
 old=original.replace('if(state.Length==0&&targetReason.Length==0&&opportunity.Actionable)','if(false)');assert old!=original;method.write_text(old);build();r=subprocess.run(command,text=True,capture_output=True)
 assert r.returncode!=0 and 'System.Exception: actual skill slot displays typed opportunity and actual expiry' in r.stdout+r.stderr,r.stdout+r.stderr
 print('PASS: compiled old ready-dot-only slot fails precise caption assertion')

 method.write_text(original)
 for before,after,expected in [
  ('if(state.Length==0&&targetReason.Length==0&&opportunity.Actionable)','if(state.Length==0&&opportunity.Actionable)','pinned rejection is queried for actual skill and suppresses opportunity'),
  ('if(state.Length==0&&targetReason.Length==0&&opportunity.Actionable)','if(state.Length==0&&targetReason.Length==0)','inactive observation cannot draw an opportunity caption')]:
  mutated=original.replace(before,after);assert mutated!=original;method.write_text(mutated);build();r=subprocess.run(command,text=True,capture_output=True)
  assert r.returncode and 'System.Exception: '+expected in r.stdout+r.stderr,r.stdout+r.stderr
  print('PASS: compiled missing target/actionability gate rejected:',expected)
 method.write_text(original)
 presentation=p/'MobileCombatPresentation.cs';current=presentation.read_text()
 for before,after in [('if(reason=="目标被遮挡")return "被遮挡";','if(reason=="目标被遮挡")return reason;'),('if(reason=="距离不足")return "太远";','if(reason=="距离不足")return reason;')]:
  assert before in current;presentation.write_text(current.replace(before,after));build();r=subprocess.run(command,text=True,capture_output=True)
  assert r.returncode!=0 and 'actual rejection caption fits minimum compact CJK width' in r.stdout+r.stderr,r.stdout+r.stderr
  print('PASS: compiled full-reason caption fails actual minimum compact CJK width assertion')
 presentation.write_text(current)

# Retain the complete free-command, lifecycle and ownership wiring contract alongside actual draw tests.
subprocess.run([sys.executable,str(root/"Tests/CombatOpportunitySourceTests.py")],check=True)
