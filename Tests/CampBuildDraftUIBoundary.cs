using System;using System.Collections.Generic;using UnityEngine;
namespace UnityEngine {
 public struct Vector2 {public static Vector2 zero=>new Vector2();}public struct Rect {public Rect(float x,float y,float w,float h){}}
 public static class Mathf {public static int RoundToInt(float f)=>(int)Math.Round(f);public static float Ceil(float f)=>(float)Math.Ceiling(f);public static float Max(float a,float b)=>Math.Max(a,b);public static float Clamp(float f,float a,float b)=>Math.Min(b,Math.Max(a,f));}
 public class GUIContent {public GUIContent(string s){}}public class GUIStyle {public float CalcHeight(GUIContent c,float w)=>24;}
}
namespace Emberfall {
 public static class MobileControls {public static bool Active;}
 public class CombatModel {public void ApplyFashion(FashionData a,FashionData b){}public void ApplyEquipment(ItemData a,ItemData b,ItemData c){}public void SetBlenderPilotOwnerAlive(bool a){}}
 public static class SummonedCompanion {public static void RefreshBuild(PlayerController p){}}
 public partial class PlayerController {
  GameSession session;StatBlock stats;CombatModel model;MasteryCoreRuntime masteryCore=new MasteryCoreRuntime();float coreWardTime;
  public float Health=37,MaxHealth=100;public bool IsDead=>Health<=0;public SkillRuntime skillRuntime=new SkillRuntime(HeroClass.Vanguard);
  public void Bind(GameSession s){session=s;RefreshStats(false);skillRuntime.TryConsume(0,3);}
 }

 public partial class GameSession {public bool HasStarted=true;string equipmentFingerprint;void RecordCombatAction(string value){}public void Bind(){Player.Bind(this);Progression.Changed+=OnProgressChanged;}public ProgressionService Progression;public PlayerController Player=new PlayerController();public bool IsInCamp=true;public void Notify(string value){}}
 public sealed partial class GameUI {
  enum Panel{Camp}Panel panel;GameSession session;float width=1000,height=700,TouchRatio=1;Color gold,jade,pale,muted;List<Rect> blockedRects=new List<Rect>();string click;int clickIndex,seen;List<string> labels=new List<string>();
  bool Button(Rect r,string s,Color c,bool enabled=true,string reason=null){labels.Add(s);if(s!=click)return false;if(seen++!=clickIndex)return false;return enabled;}
  void Text(Rect r,string s,int z,Color c,bool b=false,bool wrap=false){labels.Add(s);}void Fill(Rect r,Color c){}void Box(Rect r,Color c,bool b){}GUIStyle Style(int s,bool b,bool w)=>new GUIStyle();
  void CancelMobileScroll(){}void BlockUITransition(){}Vector2 BeginTouchScroll(string s,Rect r,Vector2 v,Rect body)=>v;void EndTouchScroll(){}
  void Click(string value,int index=0){click=value;clickIndex=index;seen=0;labels.Clear();DrawBuildPlanSurface();click=null;}
  public static void TestDraftFlow(ProgressionService p,bool mobile,Action<bool,string> check)
  {
   MobileControls.Active=mobile;var ui=new GameUI{session=new GameSession{Progression=p}};ui.session.Bind();float health=ui.session.Player.Health,energy=ui.session.Player.skillRuntime.Energy,cooldown=ui.session.Player.skillRuntime.Remaining(0);string before=JsonUtility.ToJson(p.Profile,true);ui.OpenBuildPlans();ui.Click("局部调整配点 · 临时草稿");check(ui.allocationDraft!=null,"actual camp entry opens draft");ui.Click("− 1点",0);check(ui.allocationDraft.SkillRank(0)==2&&JsonUtility.ToJson(p.Profile,true)==before,"actual skill minus edits only draft");ui.Click("撤销上一步");check(ui.allocationDraft.SkillRank(0)==3,"actual undo button restores rank");ui.Click("− 1点",0);ui.Click("取消草稿");check(ui.allocationDraft==null&&JsonUtility.ToJson(p.Profile,true)==before,"actual cancel leaves live profile unchanged");
   ui.Click("局部调整配点 · 临时草稿");ui.Click("− 1点",0);System.IO.Directory.CreateDirectory(p.SaveFilePath+".tmp");ui.Click("应用并保存方案 A");check(ui.allocationDraft!=null&&ui.allocationDraft.Error!=null&&JsonUtility.ToJson(p.Profile,true)==before&&!p.HasBuildPreset(0),"actual apply-save UI retains retryable draft on storage failure");System.IO.Directory.Delete(p.SaveFilePath+".tmp");ui.Click("应用并保存方案 A");check(ui.allocationDraft==null&&p.HasBuildPreset(0)&&p.Profile.skillRanks[0]==2,"actual apply-save A commits exact draft");
   ui.Click("局部调整配点 · 临时草稿");ui.Click("− 1点",0);ui.Click("应用并保存方案 B");check(p.HasBuildPreset(1)&&p.Profile.skillRanks[0]==1,"actual B button targets second preset");ui.Click("局部调整配点 · 临时草稿");ui.Click("+ 1点",0);ui.Click("应用配点");check(ui.allocationDraft==null&&p.Profile.skillRanks[0]==2&&p.Profile.buildPresets[1].skillRanks[0]==1,"apply-only does not rewrite saved preset");
   check(ui.session.Player.Health==health&&ui.session.Player.skillRuntime.Energy==energy&&ui.session.Player.skillRuntime.Remaining(0)==cooldown,"actual Changed -> session -> RefreshStats preserves HP/energy/CD across applies");ui.Click("局部调整配点 · 临时草稿");ui.Click("− 1点",0);check(ui.CloseBuildPlanSurface()&&ui.allocationDraft==null&&p.Profile.skillRanks[0]==2,"Back uses actual cancel path");ui.Click("局部调整配点 · 临时草稿");ui.session.Player=new PlayerController();ui.ReconcileBuildPlanSurface();check(ui.allocationDraft==null&&!ui.buildPlansOpen,"hero replacement clears draft");
  }
 }
}
