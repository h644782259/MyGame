"""Run real route navigation, owner reconciliation and ClosePanel methods; engine shell only."""
from pathlib import Path
import tempfile,os,subprocess,sys
r=Path(__file__).resolve().parents[1]
def member(file,sig):
 s=(r/'Assets/Scripts/UI'/file).read_text();a=s.index(sig);b=s.index('{',a)+1;d=1
 while d:d+=(s[b]=='{')-(s[b]=='}');b+=1
 return s[a:b]
route=(r/'Assets/Scripts/UI/GameUI.RouteSkillNavigation.cs').read_text()
close=member('GameUI.cs','private void ClosePanel()')
reconcile=member('GameUI.MobileSkills.cs','private void ReconcileMobileSkillOwner()')
shell=r'''
using System;using UnityEngine;
namespace UnityEngine{public struct Vector2{public float x,y;public Vector2(float a,float b){x=a;y=b;}public static Vector2 zero=>new Vector2();}public static class Time{public static float unscaledTime;}}
namespace Emberfall{
public static class GameBalance{public const int SkillCount=10;}
public class Profile{public bool pendingFashionChest,pendingChestReveal;}
public class ProgressionService{public string CurrentSlotId="slot-a";public Profile Profile=new Profile();}
public class Session{public bool IsInCamp=true,HasStarted=true,Blocked=true;public ProgressionService Progression=new ProgressionService();public void SetUIBlocking(bool b){Blocked=b;}public void SetPaused(bool b){}}
public partial class GameUI{
enum Panel{None,Skills,Camp,Inventory,SaveSelection,Chests,Fashion,PotionAssignment,Bindings,SaveLocation,Controls}
Panel panel=Panel.Camp,bindingReturnPanel;Session session=new Session();int campTab=2,selectedSkill,desktopDetailSkill=-1,rebindingSlot,blocks;
bool mobileSkillDetail,saveSelectionFromPause,chestDetails,bindingReturnPause,saveReturnPause,controlsReturnPause;bool ChestAnimationDone=true;float chestRevealedAt,ChestDuration=1;
ProgressionService mobileSkillsService;string mobileSkillsSlot,mobileSkillStatus;Vector2 mobileSkillListScroll=new Vector2(0,173),mobileSkillDetailScroll=new Vector2(0,300),desktopDetailScroll=new Vector2(0,260);Vector2[] mobileWorkshopScroll={new Vector2(0,93),new Vector2(0,143),new Vector2(0,271),new Vector2(0,12)};
bool CloseChapterSelection()=>false;bool CloseMobileInventoryDetail()=>false;bool CloseMobileSkillDetail(){if(!mobileSkillDetail)return false;mobileSkillDetail=false;return true;}bool CloseProgressionGoalSurface()=>false;bool CloseBuildPlanSurface()=>false;bool CloseTravelMap()=>false;bool CancelSaveDeletion()=>false;bool CancelActiveSaveFlow()=>false;
void CancelMobileScroll(){}void BlockUITransition(){blocks++;}void FinishChestReveal(){}void ReturnToInventory(){}
RECONCILE
CLOSE
public static void Verify(){int n=0;Action<bool,string> check=(b,s)=>{n++;if(!b)throw new Exception(s);};
foreach(bool visited in new[]{false,true}){
var ui=new GameUI();if(visited){ui.mobileSkillsService=ui.session.Progression;ui.mobileSkillsSlot="slot-a";}ui.OpenRouteSkill(5);ui.ReconcileMobileSkillOwner();
check(ui.panel==Panel.Skills&&ui.selectedSkill==5&&ui.mobileSkillDetail&&ui.mobileSkillDetailScroll.y==0&&ui.desktopDetailScroll.y==0,"route must reach selected detail at top even on first visit");
check(!visited||ui.mobileSkillListScroll.y==173,"existing skill list scroll preserved");ui.ClosePanel();
check(ui.panel==Panel.Camp&&ui.campTab==2&&ui.mobileWorkshopScroll[2].y==271&&ui.session.Blocked,"route Back must restore workshop position while gameplay remains blocked");
check(!ui.RouteSkillReturnAvailable,"route return consumed");}
var stale=new GameUI();stale.OpenRouteSkill(1);stale.session.Progression=new ProgressionService();check(!stale.CloseRouteSkill(),"another profile owner cannot receive stale route");
var slot=new GameUI();slot.OpenRouteSkill(1);slot.session.Progression.CurrentSlotId="slot-b";check(!slot.CloseRouteSkill(),"another slot cannot receive stale route");
var outside=new GameUI();outside.session.IsInCamp=false;outside.OpenRouteSkill(2);check(outside.panel==Panel.Camp,"outside camp cannot initiate directed route navigation");
Console.WriteLine("PASS: "+n+" actual route navigation/ClosePanel owner and scroll checks");}
}}
class Program{static void Main(){Emberfall.GameUI.Verify();}}
'''.replace('CLOSE',close).replace('RECONCILE',reconcile)
with tempfile.TemporaryDirectory(prefix='route-skill-') as t:
 p=Path(t);(p/'Route.cs').write_text(route);(p/'Program.cs').write_text(shell);(p/'Test.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType></PropertyGroup></Project>');(p/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
 env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1');cmd=[sys.argv[1] if len(sys.argv)>1 else 'dotnet','run','--project',str(p/'Test.csproj')]
 subprocess.run(cmd,env=env,check=True)
 for changed,expected in [(route.replace('mobileSkillDetail=true;',''),'route must reach selected detail'),(route,'route Back must restore workshop')]:
  (p/'Route.cs').write_text(changed);(p/'Program.cs').write_text(shell if changed!=route else shell.replace('if(CloseRouteSkill())return;',''))
  result=subprocess.run(cmd,env=env,capture_output=True,text=True)
  assert result.returncode and expected in result.stdout+result.stderr,'old directed navigation mutation must fail specific oracle'
 print('PASS: old list-only and missing-route-Back mutations fail their exact navigation oracles')
