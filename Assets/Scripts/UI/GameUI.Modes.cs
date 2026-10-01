using UnityEngine;
namespace Emberfall
{
 public sealed partial class GameUI
 {
  private Vector2 arenaSelectionScroll;
  private void DrawArenaSelection()
  {
   float u=MobileControls.Active?TouchRatio:1f,w=width/u,h=height/u;float x=(w-520)*.5f,y=(h-304)*.5f;
   Fill(new Rect(0,0,width,height),new Color(.012f,.025f,.04f,.92f));
   Text(new Rect(x*u,y*u,520*u,30*u),"选择冒险",Mathf.RoundToInt(23*u),pale,true);
   string[] names={"沉星遗迹","守望林庭","烬河突围","蚀星斗场","回廊远征"};
   string[] types={"小型 · 三波探索与宝箱","小型 · 守点解围与荆棘","小型 · 限时突围与窄桥","小型 · 三首领轮替连战","大型 · 五房 / 支线 / 星环首领"};
   Rect viewport=new Rect(x*u,(y+40)*u,520*u,132*u);
   arenaSelectionScroll=BeginTouchScroll("arena-entry",viewport,arenaSelectionScroll,new Rect(0,0,502*u,204*u));
   for(int i=0;i<names.Length;i++)
   {
    Rect r=new Rect((i%2)*253*u,(i/2)*70*u,242*u,62*u);bool chosen=session.SelectedArenaMode==i-1;
    Fill(r,chosen?new Color(.11f,.2f,.21f):card);Border(r,chosen?gold:jade*.4f);
    Text(new Rect(r.x+12*u,r.y+6*u,r.width-24*u,26*u),names[i],Mathf.RoundToInt(18*u),chosen?gold:pale,true);
    Text(new Rect(r.x+12*u,r.y+36*u,r.width-24*u,19*u),types[i],Mathf.RoundToInt(12*u),muted);
    if(GUI.Button(r,GUIContent.none,invisibleButton))session.SelectedArenaMode=i-1;
   }
   EndTouchScroll();
   Text(new Rect(x*u,(y+187)*u,190*u,26*u),"第 "+session.SelectedDungeonTier+" 阶",Mathf.RoundToInt(20*u),gold,true,false,TextAnchor.MiddleLeft);
   int materialBase=session.SelectedArenaMode<0?3:session.SelectedArenaMode==3?4:session.SelectedArenaMode+1;
   Text(new Rect(x*u,(y+216)*u,190*u,18*u),"通关碎片 "+TierRewardBand.Materials(materialBase,session.SelectedDungeonTier),Mathf.RoundToInt(12*u),jade);
   if(Button(new Rect((x+192)*u,(y+189)*u,48*u,44*u),"−",jade,session.SelectedDungeonTier>1))session.SelectedDungeonTier--;
   if(Button(new Rect((x+249)*u,(y+189)*u,48*u,44*u),"+",jade,session.SelectedDungeonTier<session.MaximumDungeonTier))session.SelectedDungeonTier++;
   if(Button(new Rect((x+315)*u,(y+189)*u,205*u,44*u),session.SelectedChallengeMode?"限疗挑战 ✓":"普通治疗",jade))session.SelectedChallengeMode=!session.SelectedChallengeMode;
   if(Button(new Rect(x*u,(y+248)*u,190*u,48*u),"返回",muted))session.CancelDungeonSelection();
   if(Button(new Rect((x+208)*u,(y+248)*u,312*u,48*u),"进入挑战",gold,true,null,true))session.ConfirmDungeonSelection();
  }
  private void DrawMobileModeStatus(Rect r)
  {
   blockedRects.Add(r);Box(r,jade,false);float u=TouchRatio;
   Text(new Rect(r.x+6*u,r.y+5*u,r.width-12*u,20*u),session.ModeName,TouchFont(14),gold,true,false,TextAnchor.MiddleCenter);
   Text(new Rect(r.x+6*u,r.y+27*u,r.width-12*u,20*u),session.RoomChainRun!=null?"房间 "+session.DungeonWave+" / 5  ·  "+(session.RoomChainRun.DoorUnlocked?"北门已开":"探索中"):"阶段 "+session.DungeonWave+" / 3  ·  "+Mathf.CeilToInt(session.ModeRun.RemainingSeconds)+"秒",TouchFont(11),pale,false,false,TextAnchor.MiddleCenter);
   Bar(new Rect(r.x+8*u,r.yMax-7*u,r.width-16*u,3*u),session.RoomChainRun!=null?(session.DungeonWave-1)/4f:session.ModeRun.ObjectiveProgress,jade);
  }
 }
}
