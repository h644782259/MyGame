using UnityEngine;
namespace Emberfall
{
 public sealed partial class GameUI
 {
  private void DrawArenaSelection()
  {
   float u=MobileControls.Active?TouchRatio:1f;
   var layout=new AdventureSelectionLayout(width/u,height/u);
   float x=layout.X,y=layout.Y;
   Fill(new Rect(0,0,width,height),new Color(.012f,.025f,.04f,.92f));
   blockedRects.Add(new Rect(0,0,width,height));
   Text(new Rect(x*u,y*u,520*u,26*u),"选择冒险",Mathf.RoundToInt(22*u),pale,true);
   string[] names={"沉星遗迹","守望林庭","烬河突围","蚀星斗场","回廊远征"};
   for(int i=0;i<names.Length;i++)
   {
    var a=layout.Entry(i);Rect r=new Rect(a.X*u,a.Y*u,a.Width*u,a.Height*u);bool chosen=session.SelectedArenaMode==i-1;
    Fill(r,chosen?new Color(.11f,.2f,.21f):card);Border(r,chosen?gold:jade*.4f);
    Text(new Rect(r.x+10*u,r.y+3*u,r.width-20*u,23*u),names[i],Mathf.RoundToInt(17*u),chosen?gold:pale,true);
    Text(new Rect(r.x+10*u,r.y+25*u,r.width-20*u,12*u),AdventureEntryPresentation.RewardLine(i-1,session.SelectedDungeonTier),Mathf.RoundToInt(10*u),jade);
    Text(new Rect(r.x+10*u,r.y+37*u,r.width-20*u,12*u),AdventureEntryPresentation.EncounterLine(i-1),Mathf.RoundToInt(10*u),muted);
    if(GUI.Button(r,GUIContent.none,invisibleButton))session.SelectedArenaMode=i-1;
   }
   // The spare sixth cell carries the same selected goal as camp and results.
   Rect goal=new Rect((x+266)*u,(y+142)*u,254*u,50*u);
   Fill(goal,card);
   var selectedGoal=session.Progression.SelectedProgressionGoal();
   Text(new Rect(goal.x+8*u,goal.y+3*u,goal.width-16*u,16*u),selectedGoal.Title,Mathf.RoundToInt(10*u),jade,true);
   Text(new Rect(goal.x+8*u,goal.y+20*u,goal.width-16*u,28*u),AdventureEntryPresentation.GoalFit(session.Progression.Profile,selectedGoal,session.SelectedArenaMode,session.SelectedDungeonTier),Mathf.RoundToInt(10*u),pale,false,true);
   float options=layout.OptionsY;
   Text(new Rect(x*u,options*u,180*u,25*u),"第 "+session.SelectedDungeonTier+" 阶",Mathf.RoundToInt(18*u),gold,true,false,TextAnchor.MiddleLeft);
   
   Text(new Rect(x*u,(options+27)*u,180*u,18*u),"通关碎片 "+AdventureEntryPresentation.Materials(session.SelectedArenaMode,session.SelectedDungeonTier),Mathf.RoundToInt(12*u),jade);
   if(Button(new Rect((x+184)*u,options*u,48*u,48*u),"−",jade,session.SelectedDungeonTier>1))session.SelectedDungeonTier--;
   if(Button(new Rect((x+240)*u,options*u,48*u,48*u),"+",jade,session.SelectedDungeonTier<session.MaximumDungeonTier))session.SelectedDungeonTier++;
   if(Button(new Rect((x+300)*u,options*u,220*u,48*u),session.SelectedChallengeMode?"限疗挑战 ✓":"普通治疗",jade))session.SelectedChallengeMode=!session.SelectedChallengeMode;
   if(Button(new Rect(x*u,layout.FooterY*u,190*u,48*u),"返回",muted))session.CancelDungeonSelection();
   if(Button(new Rect((x+208)*u,layout.FooterY*u,312*u,48*u),"进入挑战",gold,true,null,true))session.ConfirmDungeonSelection();
  }
  private void DrawMobileModeStatus(Rect r)
  {
   blockedRects.Add(r);Box(r,jade,false);float u=TouchRatio;
   if(session.ChapterActive)
   {
    Text(new Rect(r.x+6*u,r.y+4*u,r.width-12*u,18*u),session.ChapterSealView(0)!=null&&session.ChapterRun.DoorUnlocked?"双印完成 · 前往出口":ChapterDefinition.Get(session.ActiveChapterNode).Name,TouchFont(13),gold,true,false,TextAnchor.MiddleCenter);
    if(session.ChapterSealView(0)!=null)DrawChapterSeals(new Rect(r.x+6*u,r.y+24*u,r.width-12*u,44*u),u);
    else Text(new Rect(r.x+6*u,r.y+24*u,r.width-12*u,r.height-28*u),session.ChapterObjectiveCompact,TouchFont(11),pale,false,true,TextAnchor.UpperCenter);
    return;
   }
   if(session.RoomChainRun!=null)
   {
    var objective=session.RoomObjectiveView;
    Text(new Rect(r.x+6*u,r.y+4*u,r.width-12*u,16*u),session.RoomSealView(0)!=null&&session.RoomChainRun.DoorUnlocked?"双印完成 · 前往北门":objective.Title,TouchFont(13),gold,true,false,TextAnchor.MiddleCenter);
    if(session.RoomSealView(0)!=null)
    {DrawRoomSeals(new Rect(r.x+6*u,r.y+21*u,r.width-12*u,50*u),u,true);return;}
    Text(new Rect(r.x+6*u,r.y+21*u,r.width-12*u,15*u),objective.ProgressText,TouchFont(11),pale,false,false,TextAnchor.MiddleCenter);
    Text(new Rect(r.x+6*u,r.y+37*u,r.width-12*u,14*u),objective.Hint,TouchFont(10),session.RoomCaptureContested?gold:jade,false,false,TextAnchor.MiddleCenter);
    Text(new Rect(r.x+6*u,r.y+52*u,r.width-12*u,14*u),objective.SupportHint,TouchFont(10),muted,false,false,TextAnchor.MiddleCenter);
    Bar(new Rect(r.x+8*u,r.yMax-7*u,r.width-16*u,3*u),objective.Fraction,jade);
    return;
   }
   Text(new Rect(r.x+6*u,r.y+5*u,r.width-12*u,20*u),session.ModeName,TouchFont(14),gold,true,false,TextAnchor.MiddleCenter);
   Text(new Rect(r.x+6*u,r.y+27*u,r.width-12*u,20*u),"阶段 "+session.DungeonWave+" / 3  ·  "+Mathf.CeilToInt(session.ModeRun.RemainingSeconds)+"秒",TouchFont(11),pale,false,false,TextAnchor.MiddleCenter);
   if(session.ModeRun.Mode==ExpeditionModeKind.HoldPoint)
    Text(new Rect(r.x+6*u,r.y+49*u,r.width-12*u,15*u),session.ModeRun.HoldStateLabel+" "+Mathf.RoundToInt(session.ModeRun.ObjectiveProgress*100)+"%",TouchFont(10),session.ModeRun.HoldState==HoldPointState.Contested?gold:jade,false,false,TextAnchor.MiddleCenter);
   Bar(new Rect(r.x+8*u,r.yMax-7*u,r.width-16*u,3*u),session.ModeRun.ObjectiveProgress,jade);
  }
 }
}
