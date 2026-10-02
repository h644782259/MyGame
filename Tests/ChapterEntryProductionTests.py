"""Actual chapter UI partial + progression persistence; engine/host shells are explicit.
No Unity event delivery, font rendering, device touch or GameSession host behavior is claimed.
"""
from pathlib import Path
import importlib.util,os,subprocess,sys,tempfile
root=Path(__file__).resolve().parents[1];dotnet=sys.argv[1] if len(sys.argv)>1 else 'dotnet'
spec=importlib.util.spec_from_file_location('cv',root/'Tools/cloud-validation.py');cv=importlib.util.module_from_spec(spec);spec.loader.exec_module(cv)
def member(file,signature):
 s=(root/'Assets/Scripts/UI'/file).read_text();a=s.index(signature);b=s.index('{',a)+1;depth=1
 while depth:depth+=(s[b]=='{')-(s[b]=='}');b+=1
 return s[a:b]
shell=r'''
using System;using System.IO;using System.Collections.Generic;using UnityEngine;
namespace UnityEngine {
 public struct Vector2{public float x,y;public Vector2(float a,float b){x=a;y=b;}public static Vector2 zero=>new Vector2();}
 public struct Rect{public float x,y,width,height;public Rect(float a,float b,float w,float h){x=a;y=b;width=w;height=h;}public float yMax=>y+height;public float xMax=>x+width;}
 public enum TextAnchor{MiddleLeft}public static class Time{public static float unscaledTime;}
 public static class Mathf{public static float Max(float a,float b)=>Math.Max(a,b);public static float Min(float a,float b)=>Math.Min(a,b);public static int Clamp(int x,int a,int b)=>Math.Max(a,Math.Min(b,x));public static int RoundToInt(float x)=>(int)Math.Round(x);}
 public class GUIContent{public string text;public GUIContent(string s){text=s;}}
 public class GUIStyle{public static int Measurements;public float CalcHeight(GUIContent c,float width){Measurements++;return 20*(1+c.text.Length/Math.Max(1,(int)(width/10)));}}
}
namespace Emberfall {
 public static class MobileControls{public static bool Active=true;}
 public sealed class RunStub{public bool Failed;}
 public sealed class SessionStub {
  public ProgressionService Progression;public bool Paused,BackgroundPaused,IsDead,HasStarted=true,Blocked,AllowConfirm=true,ChapterFinished,ChapterRewardPending;
  public bool OpenChapterSelectionAllowed=>HasStarted&&!Paused&&!BackgroundPaused&&!IsDead&&!ChapterFinished;
  public ChapterNode SelectedChapterNode,ActiveChapterNode;public ChapterDifficulty SelectedChapterDifficulty;public int SelectedChapterTier=1;public bool SelectedChapterLimitedHealing;
  public RunStub ChapterRun=new RunStub();public ChapterRunReceipt Receipt;public int ChapterRewardMaterials=>Receipt==null?0:Receipt.Materials;public int ConfirmCalls,ReturnCalls;
  public bool ConfirmChapterEnter(){ConfirmCalls++;return AllowConfirm&&Progression.TryBeginChapterNode(SelectedChapterNode,SelectedChapterDifficulty,SelectedChapterTier,out Receipt);}
  public bool TrySettleChapterReward(){bool ok=Progression.TryCompleteChapterNode(Receipt);if(ok)ChapterRewardPending=false;return ok;}
  public void ReturnToCamp(){ReturnCalls++;}public void SetUIBlocking(bool b){Blocked=b;}public void SetPaused(bool b){Paused=b;}
 }
 public sealed partial class GameUI {
  enum Panel{None,Chapter,Camp,Inventory,Skills,Chests,Fashion,PotionAssignment,Bindings,SaveLocation,SaveSelection,Controls,TravelMap}
  Panel panel,bindingReturnPanel;SessionStub session;int campTab,rebindingSlot,blocks,cancels;
  bool UITransitionBlocked=false,saveSelectionFromPause,chestDetails,bindingReturnPause,saveReturnPause,controlsReturnPause;float chestRevealedAt;const float ChestDuration=1;bool ChestAnimationDone=>true;
  float width=568,height=320,TouchRatio=1;Color gold=new Color(),jade=new Color(),pale=new Color(),muted=new Color();string click;bool insideScroll;Rect viewport,content;
  List<(string text,Rect rect,bool scroll,bool enabled)> buttons=new List<(string,Rect,bool,bool)>();List<string> texts=new List<string>();
  void CancelHotbarPointer(){}void CancelMobileScroll(){cancels++;}void BlockUITransition(){blocks++;}
  void Fill(Rect r,Color c){}void Text(Rect r,string s,int size,Color c,bool bold=false,bool wrap=false,TextAnchor anchor=TextAnchor.MiddleLeft){texts.Add(s);}
  bool Button(Rect r,string s,Color c,bool enabled=true){buttons.Add((s,r,insideScroll,enabled));if(enabled&&click!=null&&s.StartsWith(click)){click=null;return true;}return false;}
  GUIStyle Style(int n,bool b,bool w)=>new GUIStyle();MobilePanelLayout MobilePanelGeometry()=>new MobilePanelLayout(width/TouchRatio,height/TouchRatio);
  Vector2 BeginTouchScroll(string key,Rect body,Vector2 p,Rect full){insideScroll=true;viewport=body;content=full;return p;}void EndTouchScroll(){insideScroll=false;}
  bool CloseMobileInventoryDetail()=>false;bool CloseMobileSkillDetail()=>false;bool CloseRouteSkill()=>false;bool CloseProgressionGoalSurface()=>false;bool CloseBuildPlanSurface()=>false;bool CloseTravelMap()=>false;bool CancelSaveDeletion()=>false;bool CancelActiveSaveFlow()=>false;
  void FinishChestReveal(){}void ReturnToInventory(){panel=Panel.Inventory;}
  CLOSE
  public static int Verify(string root){int n=0;Action<bool,string> check=(ok,why)=>{n++;if(!ok)throw new Exception(why);};
   var p=new ProgressionService(Path.Combine(root,"ui"));check(p.CreateNewSlot(HeroClass.Arcanist),"create persisted profile");p.Profile.highestAdventureTier=12;
   var ui=new GameUI{session=new SessionStub{Progression=p}};ui.session.SelectedChapterTier=7;
   string state=JsonUtility.ToJson(p.Profile,true),disk=File.ReadAllText(p.SaveFilePath);int events=0;p.Changed+=()=>events++;
   check(ui.OpenChapterSelection()&&ui.panel==Panel.Chapter&&ui.session.Blocked,"open actual chapter UI blocks combat");
   ui.ClosePanel();check(ui.panel==Panel.None&&ui.chapterSelectionOwner==null&&!ui.session.Blocked&&ui.cancels==2,"chapter Back must clear owner and cancel input without entering combat");
   check(ui.session.ConfirmCalls==0,"Back never enters chapter");
   check(ui.OpenChapterSelection(),"reopen chapter");
   check(!ui.SelectChapterNode(ChapterNode.Redrock)&&!ui.SelectChapterDifficulty(ChapterDifficulty.Heroic),"locked node/difficulty cannot be selected");
   ui.session.SelectedChapterDifficulty=ChapterDifficulty.Heroic;check(!ui.ConfirmSelectedChapter()&&ui.session.ConfirmCalls==0,"stale locked difficulty cannot reach host");
   ui.session.SelectedChapterDifficulty=ChapterDifficulty.Normal;
   p.Profile.chapterCompletedMask=1;p.Profile.chapterHighestDifficulties[0]=1;
   int tier=ui.session.SelectedChapterTier;check(ui.SelectChapterDifficulty(ChapterDifficulty.Hard)&&ui.session.SelectedChapterTier==tier,"difficulty selection never changes tier");
   ui.ChangeChapterTier(1);check(ui.session.SelectedChapterDifficulty==ChapterDifficulty.Hard&&ui.session.SelectedChapterTier==tier+1,"tier adjustment never changes difficulty");
   check(ui.SelectChapterNode(ChapterNode.Redrock)&&ui.session.SelectedChapterDifficulty==ChapterDifficulty.Normal,"node change resets only difficulty to valid normal");
   ui.session.AllowConfirm=false;check(!ui.ConfirmSelectedChapter()&&ui.panel==Panel.Chapter&&ui.session.Blocked&&!string.IsNullOrEmpty(ui.chapterEntryError),"host rejection retains selection and retry surface");
   foreach(int interruption in new[]{0,1,2}){
    ui.session.BackgroundPaused=interruption==0;ui.session.Paused=interruption==1;ui.session.IsDead=interruption==2;int calls=ui.session.ConfirmCalls;
    check(!ui.ConfirmSelectedChapter()&&ui.session.ConfirmCalls==calls,"interrupted UI cannot dispatch entry");
    ui.session.BackgroundPaused=ui.session.Paused=ui.session.IsDead=false;
   }
   // Read-only UI interactions above did not persist story/tier or touch the saved file.
   check(events==0&&File.ReadAllText(p.SaveFilePath)==disk,"browsing/selecting/rejected entry never writes progression");
   foreach(float logicalWidth in new[]{568,667,800,1024})foreach(float ratio in new[]{1f,1.8f,3f}){
    ui.width=logicalWidth*ratio;ui.height=320*ratio;ui.TouchRatio=ratio;ui.buttons.Clear();ui.texts.Clear();int measured=GUIStyle.Measurements;ui.DrawChapterSelection();
    check(GUIStyle.Measurements>measured&&ui.content.height>=ui.viewport.height,"body uses measured scroll content");
    int footer=0,nodeButtons=0;foreach(var b in ui.buttons){check(b.rect.height>=48*ratio-.01f,"all chapter choices keep 48-unit touch height");if(!b.scroll){footer++;check(b.rect.y>=ui.viewport.yMax&&b.rect.x>=0&&b.rect.xMax<=ui.width&&b.rect.yMax<=ui.height,"footer stays below body and inside viewport");}else if(b.text.StartsWith("林庭")||b.text.StartsWith("赤岩")||b.text.StartsWith("星台"))nodeButtons++;}
    check(footer==3&&nodeButtons==3,"three nodes and all fixed navigation actions remain reachable");
    check(ui.buttons.Exists(b=>b.text.StartsWith("星台")&&!b.enabled)&&ui.buttons.Exists(b=>b.text.StartsWith("英雄")&&!b.enabled),"locked node and heroic render disabled using shared core eligibility");
   }
   ui.session.AllowConfirm=true;check(ui.ConfirmSelectedChapter()&&ui.panel==Panel.None&&!ui.session.Blocked,"successful actual core Begin closes entry once");
   // Real filesystem rejection and actual core Complete, reached through production result retry method.
   ui.session.ChapterFinished=true;ui.session.ChapterRewardPending=true;ui.session.ActiveChapterNode=ChapterNode.Redrock;
   Directory.CreateDirectory(p.SaveFilePath+".tmp");check(!ui.RetryChapterSettlement()&&ui.session.ChapterRewardPending,"save failure preserves pending receipt");
   ui.texts.Clear();ui.buttons.Clear();ui.DrawChapterResult();check(ui.buttons.Exists(b=>b.text=="重试保存结算"&&b.enabled&&!b.scroll),"save failure keeps reachable fixed retry action");
   check(ui.texts.Exists(t=>t.Contains("结算待保存"))&&ui.texts.Exists(t=>t.Contains(p.LastError)),"result visibly distinguishes unsaved progress and actual error");
   int capturedMaterials=ui.session.Receipt.Materials;Directory.Delete(p.SaveFilePath+".tmp");check(ui.RetryChapterSettlement()&&!ui.session.ChapterRewardPending,"same receipt retries through actual UI method and real save");
   ui.texts.Clear();ui.DrawChapterResult();check(ui.texts.Exists(t=>t.Contains("奖励已保存 · +"+capturedMaterials+" 碎片")),"result displays original receipt amount including captured first-clear bonus");
   check(capturedMaterials==ChapterProgression.MaterialReward(ui.session.Receipt.Node,ui.session.Receipt.Tier)+1,"first-clear receipt retains bonus after completion mask changed");
   int after=events;check(!ui.RetryChapterSettlement()&&events==after,"completed UI retry cannot grant again");
   ui.ReturnFromChapter();check(ui.session.ReturnCalls==1,"result return delegates to host guarded leave path");
   ui.session.ChapterFinished=false;check(ui.OpenChapterSelection(),"open new selection after completed run");
   check(p.LoadSlot(p.CurrentSlotId),"replace profile through real slot load");int callsBefore=ui.session.ConfirmCalls;
   check(!ui.ConfirmSelectedChapter()&&ui.session.ConfirmCalls==callsBefore,"same character reloaded profile cannot use stale UI owner");
   ui.DrawChapterSelection();check(ui.panel==Panel.None&&ui.chapterSelectionOwner==null&&!ui.session.Blocked,"drawing stale selection closes safely without entry");
   return n;
  }
 }
}
class Program{static void Main(string[] args){Console.WriteLine("PASS: "+Emberfall.GameUI.Verify(args[0])+" chapter UI/core replay assertions");}}
'''
core=['GameTypes','ProgressionService','ProgressionService.Chapter','ChapterProgression','RoomTactics','CombatBalance','HubTravelRules','MasteryCoreRuntime','TierRewardRules','TierRewardBand','ProgressionGoalState']
close=member('GameUI.cs','private void ClosePanel()');hook='if(CloseChapterSelection())return;'
assert hook in close,'chapter ClosePanel hook must be integrated before replay'
files=[root/'Assets/Scripts/Core'/f'{name}.cs' for name in core]+[root/'Assets/Scripts/UI/GameUI.Chapter.cs',root/'Assets/Scripts/UI/ChapterEntryPresentation.cs',root/'Assets/Scripts/UI/MobilePanelLayout.cs',root/'Tests/ProgressionTests.cs']
with tempfile.TemporaryDirectory(prefix='chapter-entry-') as folder:
 out=Path(folder);config=out/'NuGet.Config';config.write_text('<configuration><packageSources><clear /></packageSources></configuration>');env=dict(os.environ,DOTNET_CLI_HOME=str(out/'cli'),DOTNET_NOLOGO='1')
 for legacy in [False,True]:
  p=cv.write_project(out/('legacy' if legacy else 'positive'),files,program=shell.replace('CLOSE',close.replace(hook,'') if legacy else close))
  subprocess.run([dotnet,'build',str(p),'--configfile',str(config),'-v:q'],env=env,check=True)
  run=[dotnet,str(p.parent/'bin/Debug/net8.0/Validation.dll'),str(out/('legacy-saves' if legacy else 'saves'))]
  if not legacy:subprocess.run(run,env=env,check=True)
  else:
   result=subprocess.run(run,env=env,capture_output=True,text=True);error=result.stdout+result.stderr
   expected='Unhandled exception. System.Exception: chapter Back must clear owner and cancel input without entering combat'
   if result.returncode==0 or not error.splitlines() or error.splitlines()[0]!=expected or any('Exception:' in line for line in error.splitlines()[1:]):raise RuntimeError('legacy hook removal did not fail intended assertion: '+error)
   print('PASS: old chapter Back hook negative control compiled and failed specified navigation assertion')
