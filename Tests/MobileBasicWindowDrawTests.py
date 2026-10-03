#!/usr/bin/env python3
"""Execute production DrawAvailability + LabelControl; GUI records text/color/geometry."""
from pathlib import Path
import os,sys,tempfile,subprocess
root=Path(__file__).resolve().parents[1];dotnet=sys.argv[1] if len(sys.argv)>1 else 'dotnet'
shell=r'''
using System;using System.Collections.Generic;using System.Linq;using UnityEngine;
namespace UnityEngine {
 public struct Vector2{public float x,y;public Vector2(float x,float y){this.x=x;this.y=y;}}
 public struct Vector3{public float x,y,z;public static Vector3 up=>new Vector3();public static Vector3 operator+(Vector3 a,Vector3 b)=>a;public static Vector3 operator*(Vector3 a,float b)=>a;}
 public struct Rect{public float x,y,width,height;public float yMax=>y+height;public Vector2 center=>new Vector2(x+width/2,y+height/2);public Rect(float x,float y,float w,float h){this.x=x;this.y=y;width=w;height=h;}}
 public struct Color{public float r,g,b,a;public Color(float r,float g,float b,float a=1){this.r=r;this.g=g;this.b=b;this.a=a;}public static Color white=>new Color(1,1,1);}
 public enum TextAnchor{MiddleCenter}public enum FontStyle{Bold}public class Font{}
 public class GUIStyleState{public Color textColor;}public class GUIStyle{public GUIStyle(){}public GUIStyle(GUIStyle s){}public TextAnchor alignment;public int fontSize;public FontStyle fontStyle;public Font font;public GUIStyleState normal=new GUIStyleState();}
 public class Skin{public GUIStyle label=new GUIStyle();}public class Texture2D{public static Texture2D whiteTexture=new Texture2D();}
 public static class GUI{public struct Draw{public Rect Rect;public string Text;public Color Color;}public static List<Draw> Draws=new List<Draw>();public static Skin skin=new Skin();public static Color color;public static void DrawTexture(Rect r,Texture2D t){}public static void Label(Rect r,string text,GUIStyle style){Draws.Add(new Draw{Rect=r,Text=text,Color=style.normal.textColor});}}
 public class Camera{public static Camera main;public float nearClipPlane;public Vector3 WorldToScreenPoint(Vector3 p)=>p;}public static class Mathf{public static float Clamp(float v,float a,float b)=>Math.Max(a,Math.Min(v,b));}
 public class Transform{public Vector3 position;}
}
namespace Emberfall {
 public static class EffectPreferences{public static float TouchVisualScale=1,TouchOpacity=1;}public static class GameFont{public static Font Shared=new Font();}
 public static class MobileCombatPresentation{public static string Potion(int n,bool l,bool full)=>"";public static string Dodge(float cd,bool jumping)=>"";}
 public class SkillChargeController{public bool IsCharging;}public class EnemyController{public Transform transform=new Transform();public string DisplayName;}
 public class PlayerController{
 public float Health=50,MaxHealth=100,DodgeCooldown;public bool IsJumping;public EnemyController MobilePinnedTarget;public CombatOpportunityState Counter,Combo;public int LegacyQueries;public string Reason="";
 public CombatOpportunityState BasicOpportunityWindow(bool mastery=false)=>mastery?Combo:Counter;
 // Reproduces the old actor contract: readiness ignores absent/remote/occluded targets.
 public CombatOpportunityState BasicOpportunity(){LegacyQueries++;return new CombatOpportunityState(CombatOpportunityKind.Counter,1.2f);}
 public string MobilePinnedActionReason(int skill)=>Reason;public T GetComponent<T>()where T:class=>null;
 }
 public class Profile{public int potions=3;}public class Progression{public Profile Profile=new Profile();}public class GameSession{public bool ChallengeRun,InDungeon;public int HealingCharges;public PlayerController Player=new PlayerController();public Progression Progression=new Progression();public string Failure="";public string ControlFailure(string key)=>key=="attack"?Failure:"";public void ReportControlFailure(string key,string reason){}}
 public sealed partial class MobileControls{
 GameSession session=new GameSession();Rect Potion=new Rect(0,0,40,40),Dodge=new Rect(50,0,40,40),Attack=new Rect(100,100,84,84);public class LayoutType{public float Width=568,Height=320;}LayoutType Layout=new LayoutType();Vector2 ToUI(Vector2 p)=>p;void Circle(Rect r,Color c,string text){}
 static int n;static void C(bool b,string s){n++;if(!b)throw new Exception(s);}
 public static void Run(){var v=new MobileControls();var hero=v.session.Player;
 foreach(var reason in new[]{"无目标","距离不足","被遮挡"})foreach(float scale in new[]{.86f,1f}){
 EffectPreferences.TouchVisualScale=scale;hero.Counter=new CombatOpportunityState(CombatOpportunityKind.Counter,1.2f,blockReason:reason);hero.Reason=reason=="无目标"?"":reason;v.session.Failure=reason;hero.Combo=new CombatOpportunityState(CombatOpportunityKind.MasteryCombo,4.3f,blockReason:reason);GUI.Draws.Clear();v.DrawAvailability();
 var clocks=GUI.Draws.Where(d=>d.Text=="反击 1.2").ToArray();C(clocks.Length==1,"one authoritative counter clock for blocked target");C(clocks[0].Color.r<.7f&&clocks[0].Color.g<.7f,"blocked counter is muted with no white executable duplicate");C(clocks[0].Rect.center.y<125,"counter clock stays above center rejection");C(GUI.Draws.Any(d=>d.Text==reason)&&GUI.Draws.Any(d=>d.Text=="连击 4.3"),"rejection and mastery clock coexist");C(hero.LegacyQueries==0,"legacy counter readiness is never queried");}
 hero.Combo=default;hero.Counter=new CombatOpportunityState(CombatOpportunityKind.Counter,.5f);hero.Reason="";v.session.Failure="";GUI.Draws.Clear();v.DrawAvailability();var ready=GUI.Draws.Where(d=>d.Text=="反击 0.5").ToArray();C(ready.Length==1&&ready[0].Color.r==1,"legal target gives one bright clock");hero.Counter=default;GUI.Draws.Clear();v.DrawAvailability();C(!GUI.Draws.Any(d=>d.Text.StartsWith("反击")),"expiry removes every counter caption");Console.WriteLine("PASS "+n+" actual DrawAvailability/LabelControl color and geometry assertions (managed GUI boundary)");}
 }
}
class Program{static void Main(){Emberfall.MobileControls.Run();}}
'''
with tempfile.TemporaryDirectory(prefix='mobile-basic-clock-') as tmp:
 p=Path(tmp);(p/'State.cs').write_text((root/'Assets/Scripts/Core/CombatOpportunityState.cs').read_text());method=p/'Draw.cs';original=(root/'Assets/Scripts/UI/MobileControls.Feedback.cs').read_text();method.write_text(original);(p/'Fixture.cs').write_text(shell);(p/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>');proj=p/'Test.csproj';proj.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType></PropertyGroup></Project>');env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1')
 for mode in ['current','old-duplicate-ready-counter']:
  method.write_text(original if mode=='current' else original.replace('            string basicReason=', '            var opportunity=hero.BasicOpportunity();if(opportunity.Actionable)LabelControl(Attack,opportunity.Caption,true);\n            string basicReason='))
  q=subprocess.run([dotnet,'build',str(proj),'--configfile',str(p/'NuGet.Config'),'-v:q'],env=env,capture_output=True,text=True);print(mode,'BUILD',q.stdout,q.stderr);assert q.returncode==0
  q=subprocess.run([dotnet,str(p/'bin/Debug/net8.0/Test.dll')],env=env,capture_output=True,text=True);print(mode,'RUN',q.stdout,q.stderr)
  if mode=='current':assert q.returncode==0
  else:assert q.returncode!=0 and 'one authoritative counter clock for blocked target' in q.stderr;print('PASS compiled old duplicate caption fails actual GUI draw count')
