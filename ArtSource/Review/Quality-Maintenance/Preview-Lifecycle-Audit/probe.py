from pathlib import Path
import subprocess,os,hashlib
root=Path('/workspace/emberfall_quality_integration'); p=Path(__file__).resolve().parent
source=root/'Assets/Scripts/UI/GameUI.CollectionPreview.cs'; s=source.read_text()
def member(k):
 a=s.index(k);b=s.index('{',a)+1;n=1
 while n:n+=(s[b]=='{')-(s[b]=='}');b+=1
 return s[a:b]
methods='\n'.join(member(k) for k in ['private void ReconcileCollectionPreview(', 'private void ReleaseCollectionModel(', 'private void ReleaseCollectionPreview(', 'private void OnDisable(', 'private void OnApplicationFocus(', 'private void OnApplicationPause('])
fixture=r'''
using System;
namespace Emberfall {
class PlayerController {}
class Preview {public int Disposals,Invalidations;public void Dispose(){Disposals++;}public void Invalidate(){Invalidations++;}}
class Viewing {public int Resets;public void Reset(){Resets++;}}
class Choices {public bool AwaitingChoice;}
class Session {public bool BackgroundPaused,Paused,HasStarted=true,IsDead,ModeFinished,DungeonSelectionOpen; public PlayerController Player=new PlayerController(); public Choices RunChoices=new Choices();}
class GameUI {
enum Panel {None,Fashion,Chests,Inventory,Other}
Session session=new Session();Panel panel=Panel.Fashion;Preview collectionModel;Viewing collectionViewing=new Viewing();PlayerController collectionOwner;
object collectionTrial,collectionNotice,collectionReceiptKey,equipmentAppearanceItem;float collectionPreviewYaw;bool mobileFashionPreview,equipmentAppearanceOpen,equipmentAppearanceCandidate;
METHODS
static int assertions;static void Check(bool b,string m){assertions++;if(!b)throw new Exception(m);}
void Seed(){collectionOwner=session.Player;collectionModel=new Preview();collectionTrial=new object();collectionNotice=new object();collectionReceiptKey=new object();equipmentAppearanceItem=new object();equipmentAppearanceCandidate=true;collectionPreviewYaw=160;}
static void Main(){
 var u=new GameUI();u.Seed();var live=u.collectionModel;var trial=u.collectionTrial;u.ReconcileCollectionPreview();Check(u.collectionModel==live&&u.collectionTrial==trial,"eligible owner retains preview");
 u.session.Paused=true;u.ReconcileCollectionPreview();Check(live.Disposals==1&&u.collectionModel==null,"pause disposes model");Check(u.collectionTrial==trial&&u.collectionOwner==u.session.Player,"pause retains viewing intent");u.ReconcileCollectionPreview();Check(live.Disposals==1,"paused repeated reconcile does not redispose");
 u.session.Player=new PlayerController();u.ReconcileCollectionPreview();Check(u.collectionModel==null,"paused owner change cannot leave old model alive");u.session.Paused=false;u.ReconcileCollectionPreview();Check(u.collectionTrial==null&&u.collectionOwner==u.session.Player,"resume resets old-owner trial");
 u.Seed();live=u.collectionModel;u.session.BackgroundPaused=true;u.ReconcileCollectionPreview();Check(live.Disposals==1&&u.collectionModel==null,"background pause disposes model");u.session.BackgroundPaused=false;
 foreach(var scenario in new[]{"title","dead","finished","dungeon","choice","other-panel","inventory-closed"}){
  u=new GameUI();u.Seed();live=u.collectionModel;
  switch(scenario){case "title":u.session.HasStarted=false;break;case "dead":u.session.IsDead=true;break;case "finished":u.session.ModeFinished=true;break;case "dungeon":u.session.DungeonSelectionOpen=true;break;case "choice":u.session.RunChoices.AwaitingChoice=true;break;case "other-panel":u.panel=Panel.Other;break;case "inventory-closed":u.panel=Panel.Inventory;break;}
  u.ReconcileCollectionPreview();Check(live.Disposals==1&&u.collectionModel==null,scenario+" disposes model");Check(u.collectionOwner==null&&u.collectionTrial==null&&u.collectionReceiptKey==null&&u.collectionNotice==null&&!u.equipmentAppearanceCandidate&&u.equipmentAppearanceItem==null&&u.collectionPreviewYaw==20&&u.mobileFashionPreview,scenario+" clears detached state");
 }
 u=new GameUI();u.Seed();u.panel=Panel.Inventory;u.equipmentAppearanceOpen=true;live=u.collectionModel;u.ReconcileCollectionPreview();Check(u.collectionModel==live,"open equipment view retained");u.session.Player=new PlayerController();u.ReconcileCollectionPreview();Check(live.Disposals==1&&!u.equipmentAppearanceOpen&&u.collectionOwner==u.session.Player,"regenerated player closes previous equipment candidate");
 u=new GameUI();u.Seed();u.panel=Panel.Chests;live=u.collectionModel;u.ReconcileCollectionPreview();Check(u.collectionModel==live,"reward panel retains preview");u.panel=Panel.None;u.ReconcileCollectionPreview();Check(live.Disposals==1&&u.collectionReceiptKey==null,"leaving reward panel releases receipt preview");
 u=new GameUI();u.Seed();live=u.collectionModel;u.OnApplicationFocus(false);u.OnApplicationPause(true);Check(live.Invalidations==0,"loss notifications do not render");u.OnApplicationFocus(true);u.OnApplicationPause(false);Check(live.Invalidations==2,"resume invalidates cached scene lights/frame");u.OnDisable();u.OnDisable();Check(live.Disposals==1&&u.collectionModel==null,"disable releases model idempotently");
 Console.WriteLine("PASS: "+assertions+" actual GameUI ownership/disposal assertions; Unity resource and model boundaries are doubles");
}
}}
'''
(p/'Program.cs').write_text(fixture.replace('METHODS',methods))
(p/'Probe.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType></PropertyGroup></Project>')
(p/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>')
env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1')
dotnet='/workspace/scratch/dotnet/dotnet'
subprocess.run([dotnet,'restore',str(p/'Probe.csproj'),'--configfile',str(p/'NuGet.Config'),'-v:q'],env=env,check=True)
subprocess.run([dotnet,'run','--project',str(p/'Probe.csproj'),'--no-restore'],env=env,check=True)
print('Source SHA256',hashlib.sha256(source.read_bytes()).hexdigest())
