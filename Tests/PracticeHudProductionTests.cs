using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Emberfall;
namespace UnityEngine
{
 public struct Color{}
 public struct Rect{public float x,y,width,height;public float xMax=>x+width;public float yMax=>y+height;public Rect(float a,float b,float c,float d){x=a;y=b;width=c;height=d;}}
 public class GUIContent{public string text;public GUIContent(string value){text=value;}}
 public static class Mathf{public static float Max(float a,float b)=>Math.Max(a,b);public static int RoundToInt(float n)=>(int)Math.Round(n);public static float Ceil(float n)=>(float)Math.Ceiling(n);}
}
namespace Emberfall
{
 public static class MobileControls{public static bool Active;}
 public class ProgressionService{public class BuildDraft{}}
 public class PlayerController{public float Health=33,MaxHealth=100,Energy=44;}
 public class GameSession
 {
  public PlayerController Player=new PlayerController();public CampPracticeRecord PracticeRecord,PreviousPracticeRecord;public bool PracticeActive=true;public int Starts,Restarts,Ends,Pins;
  public bool BeginPractice(CampPracticeScenario scenario,int seconds,ProgressionService.BuildDraft draft){throw new Exception("entry from choices is covered by lifecycle replay");}
  public bool PinPracticeBaseline(){Pins++;return true;}public bool StartPractice(){Starts++;return PracticeRecord.Start();}public bool RestartPractice(){Restarts++;PracticeRecord=new CampPracticeRecord(PracticeRecord.Scenario,PracticeRecord.Duration,"");PracticeRecord.Prepare();return true;}public void EndPractice(string reason){Ends++;PracticeRecord.Finish(reason);PracticeActive=false;}
 }
 public sealed partial class GameUI
 {
  public enum Panel{None,Skills}private Panel panel=Panel.Skills;private float width=568,height=320;private float TouchRatio=1;private Color muted,pale,gold,jade;private List<Rect> blockedRects=new List<Rect>();public GameSession session;
  public readonly List<(Rect area,string text)> Labels=new List<(Rect,string)>();public readonly List<(Rect area,string text)> Buttons=new List<(Rect,string)>();public string Click;public int Hotbars,MobileHotbars,Companions,Charge,Targeting,Cancellations;
  private bool Button(Rect r,string text,Color c,bool enabled=true){Buttons.Add((r,text));if(enabled&&Click==text){Click=null;return true;}return false;}
  private void Text(Rect r,string text,int size,Color c,bool bold=false,bool wrap=false){Labels.Add((r,text));}
  // Text measuring is an explicit managed substitute; actual Unity font rendering is not claimed.
  private sealed class FontBoundary{public float CalcHeight(GUIContent c,float w){return Math.Max(1,c.text.Split('\n').Sum(s=>(int)Math.Ceiling(Math.Max(1,s.Length)*13f/Math.Max(1,w))))*17;}}
  private FontBoundary Style(int n,bool bold,bool wrap)=>new FontBoundary();private void CancelMobileScroll(){Cancellations++;}private void CancelMobileCast(){Cancellations++;}private void CancelHotbarPointer(){Cancellations++;}
  private void DrawMobileHotbar(){MobileHotbars++;}private void DrawHotbar(){Hotbars++;}private void DrawCompanionCommands(){Companions++;}private void DrawChargeProgress(){Charge++;}private void DrawTargetingHint(){Targeting++;}
  public void Frame(float w,float h){width=w;height=h;Labels.Clear();Buttons.Clear();blockedRects.Clear();DrawPracticeCombatHUD();DrawPracticeOverlay();}
  public float Results(bool draw){Labels.Clear();Buttons.Clear();float y=0;DrawPracticeChoices(ref y,568,1,draw,false,null);return y;}
  public bool OriginalPanel=>panel==Panel.Skills;public bool BattlePanel=>panel==Panel.None;
 }
}
class Program
{
 static int n;static void C(bool value,string s){n++;if(!value)throw new Exception(s);}static CampPracticeRecord Run(string config="A",int duration=10,int seed=7319,string hero="Vanguard",int level=20,int rules=2){return new CampPracticeRecord(CampPracticeScenario.GuardAndWispPressure,duration,config,seed,"human summary",hero,level,new[]{hero+" sword",hero+" guard"},rules);}
 static void Main()
 {
  var names=new[]{"Frozen original skill"};var labels=new CampPracticeRecord(CampPracticeScenario.Stationary,10,"names",skillNames:names);names[0]="Changed current hero skill";C(labels.SkillLabel(0)=="Frozen original skill","record freezes incoming skill labels");
  var a=Run();a.Advance(2);a.Cast(1,0);a.Cast(2,0);a.ConfirmedHealthLoss(100,1);a.ConfirmedHealthLoss(20,1);a.Energy(-30);a.Energy(5);a.IncomingDamage(9);a.Healing(4);a.Mechanism("剑卫反击");a.Defeat("Wisp",true,false);a.Defeat("Guardian",false,true);var frozen=a.FrozenCopy();C(!ReferenceEquals(a,frozen)&&frozen.Finished&&frozen.KillOrder.Count==2,"frozen copy deep snapshots terminal evidence");
  bool dictionaryRefused=false,listRefused=false;try{((IDictionary<int,int>)frozen.SkillCasts).Add(99,99);}catch(NotSupportedException){dictionaryRefused=true;}try{((IList<string>)frozen.KillOrder).Add("fake");}catch(NotSupportedException){listRefused=true;}C(dictionaryRefused&&listRefused,"baseline result collections cannot be externally mutated");
  a.Damage(500);a.Healing(100);a.Cast(10,0);frozen.Damage(600);frozen.Energy(600);frozen.Mechanism("fake");C(frozen.ActualDamage==120&&frozen.EnergyRestored==5&&frozen.Mechanisms.Count==2,"finished original and frozen baseline reject late mutations");
  var b=Run("B");b.Advance(4);b.Cast(20,0);b.ConfirmedHealthLoss(200,20);b.Defeat("Guardian",false,true);C(b.ComparableConditions(frozen)&&b.Comparison(frozen).Contains("配置不同"),"same objective conditions allow completed runs with explicit config distinction");
  var table=new PracticeResultPresentation(frozen,b);C(table.Rows.Single(r=>r.Label=="DPS（按实际秒数）").A=="60.0"&&table.Rows.Single(r=>r.Label=="DPS（按实际秒数）").B=="50.0","DPS uses actual completion time not configured cap");C(table.Rows.Single(r=>r.Label=="技能 1 有效 / 施放").A.Contains("1 / 2")&&table.Rows.Single(r=>r.Label=="技能 1 有效 / 施放").A.Contains("Vanguard sword"),"table reports distinct effective casts not arrow hit ratio");C(table.Rows.Any(r=>r.Label=="机制 · 剑卫反击"&&r.A=="1"&&r.B=="0"),"table preserves actual mechanism counts");
  foreach(var other in new[]{Run(seed:1),Run(hero:"Ranger"),Run(level:21),Run(duration:60)}){other.Advance(2);other.Defeat("Guardian",false,true);C(!frozen.ComparableConditions(other),"different seed hero level or limit cannot compare");}
  var rulesChanged=Run(rules:3);rulesChanged.Advance(2);rulesChanged.Defeat("Guardian",false,true);C(!frozen.ComparableConditions(rulesChanged),"changed target rules cannot compare");var early=Run();early.Advance(2);early.Finish("主动离开");C(!frozen.ComparableConditions(early),"early exit cannot compare");var dead=Run();dead.Advance(2);dead.PlayerDefeated();C(!frozen.ComparableConditions(dead),"death cannot compare");
  foreach(var shape in new[]{(568f,320f),(640f,360f),(844f,390f),(1024f,768f),(1366f,768f)})
  foreach(int preset in new[]{-1,0,1})
  {
   var touch=new MobileControlLayout(shape.Item1,shape.Item2,163,preset);var layout=new PracticeHudLayout(touch.Width);var areas=new[]{layout.Sidebar,layout.Header,layout.Primary,layout.Leave};
   for(int i=0;i<areas.Length;i++){var area=areas[i];C(area.X>=0&&area.Y>=0&&area.XMax<=touch.Width&&area.YMax<=touch.Height,"HUD fits safe touch viewport");for(int j=0;j<i;j++)C(!area.Overlaps(areas[j]),"HUD areas do not overlap each other");var r=new MobileControlLayout.Area(area.X,area.Y,area.Width,area.Height);foreach(var key in touch.Skills)C(!r.Overlaps(key),"HUD never obscures any of ten skill targets");C(!r.Overlaps(touch.CombatView)&&!r.Overlaps(touch.MoveZone)&&!r.Overlaps(touch.Potion)&&!r.Overlaps(touch.Interact),"HUD preserves feet camera view joystick potion and interaction");}
   C(layout.Primary.Width>=44&&layout.Primary.Height>=44&&layout.Leave.Height>=44,"actions retain touch target minimum");
  }
  foreach(bool mobile in new[]{false,true})
  {
   MobileControls.Active=mobile;var game=new GameSession{PracticeRecord=Run()};game.PracticeRecord.Prepare();var ui=new GameUI{session=game};ui.EnterPracticePanel();ui.EnterPracticePanel();C(ui.BattlePanel&&ui.Cancellations==3,"panel handoff cancels old pointers once");ui.Frame(568,320);C(ui.Buttons.Count==2&&ui.Buttons.Any(x=>x.text=="开始")&&ui.Buttons.Any(x=>x.text=="结束"),"actual preparation GUI emits only two top actions");C(ui.Labels.Count==2&&ui.Labels.All(x=>x.area.yMax<=70),"actual combat labels stay within compact top band");ui.Click="开始";ui.Frame(568,320);C(game.Starts==1&&game.PracticeRecord.Started,"actual GUI start routes to explicit lifecycle boundary");ui.Click="重开";ui.Frame(568,320);C(game.Restarts==1&&!game.PracticeRecord.Started,"actual GUI restart returns to preparation");ui.Click="结束";ui.Frame(568,320);ui.LeavePracticePanel();ui.LeavePracticePanel();C(game.Ends==1&&ui.OriginalPanel,"actual GUI exit restores prior draft panel once");C(mobile?ui.MobileHotbars==4&&ui.Hotbars==0:ui.Hotbars==4&&ui.MobileHotbars==0,"actual practice GUI reuses platform hotbar helper");
   game.PracticeRecord=b;game.PreviousPracticeRecord=frozen;float measured=ui.Results(false),drawn=ui.Results(true);C(measured==drawn&&drawn>0,"results table uses same measured and rendered row flow");C(ui.Labels.Any(x=>x.text=="固定基准 A")&&ui.Labels.Any(x=>x.text=="本轮 B")&&!ui.Labels.Any(x=>x.text.Contains("heroClass")),"actual GUI emits structured frozen result columns without profile JSON");
  }
  Console.WriteLine("PASS "+n+" actual practice HUD/presentation/frozen baseline/layout assertions; GUI/font boundaries managed, not Unity rendering");
 }
}
