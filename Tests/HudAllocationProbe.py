#!/usr/bin/env python3
"""Managed .NET 8 allocation probe of actual HUD draw bodies and presentation constructors.
UI/scene boundaries are no-allocation doubles: excludes Unity IMGUI, fonts, native memory and frame timing.
"""
import argparse,os,subprocess,tempfile,json,hashlib
from pathlib import Path
root=Path(__file__).resolve().parents[1]
p=argparse.ArgumentParser();p.add_argument('dotnet',nargs='?',default=os.environ.get('DOTNET','dotnet'));p.add_argument('--output',type=Path);p.add_argument('--revision');args=p.parse_args()
def read(relative):
 if args.revision:return subprocess.check_output(['git','show',args.revision+':'+relative],cwd=root,text=True)
 return (root/relative).read_text()
def member(s,k):
 a=s.index(k);b=s.index('{',a)+1;n=1
 while n:n+=(s[b]=='{')-(s[b]=='}');b+=1
 return s[a:b]
paths=['Core/RoomTactics','Core/RoomTacticalRegion','Core/RoomChainState','UI/RoomObjectivePresentation','UI/ChapterSealPresentation','UI/ObjectiveCardLayout','UI/MobileControlLayout']
modes=read('Assets/Scripts/UI/GameUI.Modes.cs');desktop=read('Assets/Scripts/UI/GameUI.cs');a=desktop.index('            string objectiveText =');b=desktop.index('            DrawMinimap();',a)
fixture=(root/'Tests/RoomSealHudProductionTests.cs').read_text()
fixture=fixture.replace('public static ChapterDefinition Get(int node)=>new ChapterDefinition();','static readonly ChapterDefinition cached=new ChapterDefinition();public static ChapterDefinition Get(int node)=>cached;')
for sig,body in [('void Text(', 'void Text(Rect r,string s,int size,Color c,bool bold=false,bool wrap=false,TextAnchor anchor=TextAnchor.MiddleCenter){sink+=s.Length;}'),('void Fill(', 'void Fill(Rect r,Color c){}'),('void Bar(', 'void Bar(Rect r,float value,Color c){sink+=(int)(value*10);}'),('MeasuredStyle Style(', 'readonly MeasuredStyle measuredStyle=new MeasuredStyle();MeasuredStyle Style(int size,bool bold,bool wrap)=>measuredStyle;')]:
 if sig=='MeasuredStyle Style(':fixture=fixture.replace('MeasuredStyle Style(int size,bool bold,bool wrap)=>new MeasuredStyle();',body)
 else:fixture=fixture.replace(member(fixture,sig),body)
probe='''
  static long sink;
  static double[] Measure(Action callback)
  {
   for(int i=0;i<3000;i++)callback();
   var samples=new double[5];
   for(int sample=0;sample<samples.Length;sample++){long before=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<10000;i++)callback();samples[sample]=(GC.GetAllocatedBytesForCurrentThread()-before)/10000.0;}
   return samples;
  }
  public static void Probe()
  {
   var view=new GameUI();var run=new RoomChainState(0);Register(run);for(int i=0;i<6;i++){run.AdvanceSeal(0,.25f,true,true,false);run.AdvanceSeal(1,.25f,true,true,false);}view.session.RoomChainRun=run;view.session.Occupied=1;view.session.Contested=0;view.TouchRatio=.86f;
   var results=new Dictionary<string,double[]>();
   results["seal_snapshot_bytes"]=Measure(()=>{var seal=new ChapterSealPresentation(1,1.5f,true,false,false,false);sink+=seal.Label.Length;});
   results["room_mobile_draw_bytes"]=Measure(()=>{view.Clear();view.DrawMobileModeStatus(new Rect(0,0,188*.86f,76*.86f));});
   results["room_desktop_draw_bytes"]=Measure(()=>{view.Clear();view.Desktop();});
   results["room_mobile_two_event_frame_bytes"]=Measure(()=>{for(int e=0;e<2;e++){view.Clear();view.DrawMobileModeStatus(new Rect(0,0,188*.86f,76*.86f));}});
   results["room_mobile_four_event_frame_bytes"]=Measure(()=>{for(int e=0;e<4;e++){view.Clear();view.DrawMobileModeStatus(new Rect(0,0,188*.86f,76*.86f));}});
   view.session.ChapterActive=view.session.ChapterOpen=true;
   results["chapter_mobile_draw_bytes"]=Measure(()=>{view.Clear();view.DrawMobileModeStatus(new Rect(0,0,188*.86f,76*.86f));});
   results["chapter_desktop_draw_bytes"]=Measure(()=>{view.Clear();view.Desktop();});
   Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(results));GC.KeepAlive(sink);
  }
'''
fixture=fixture.replace('  public static void Run()',probe+'  public static void Run()')
with tempfile.TemporaryDirectory(prefix='hud-allocation-') as temporary:
 t=Path(temporary)
 for name in paths:(t/(Path(name).name+'.cs')).write_text(read('Assets/Scripts/'+name+'.cs'))
 (t/'Rows.cs').write_text(read('Assets/Scripts/UI/GameUI.ChapterSeals.cs'));(t/'Draw.cs').write_text('using UnityEngine;namespace Emberfall{public partial class GameUI{'+member(modes,'private void DrawMobileModeStatus(')+'public void Desktop(){var p=session.Profile;'+desktop[a:b]+'}}}')
 (t/'Fixture.cs').write_text(fixture);(t/'Program.cs').write_text('Emberfall.GameUI.Probe();')
 project=t/'Probe.csproj';project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><NoWarn>0649;0414</NoWarn></PropertyGroup></Project>');(t/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
 env=dict(os.environ,DOTNET_CLI_HOME=str(t/'cli'),DOTNET_NOLOGO='1');env.pop('DOTNET_TieredCompilation',None)
 q=subprocess.run([args.dotnet,'build',str(project),'-c','Release','--configfile',str(t/'NuGet.Config'),'-v:q'],env=env,capture_output=True,text=True);assert q.returncode==0,q.stdout+q.stderr
 q=subprocess.run([args.dotnet,str(t/'bin/Release/net8.0/Probe.dll')],env=env,capture_output=True,text=True);assert q.returncode==0,q.stdout+q.stderr
 result={'scope':'managed .NET8 Release default JIT; real constructor/draw bodies; no-allocation UI doubles; host snapshot getters managed doubles; excludes Unity/native/fonts/device timing','warmupCalls':3000,'callsPerSample':10000,'sampleCount':5,'statistic':'bytes/call for each of five samples','allocations':json.loads(q.stdout),'sourceRevision':subprocess.check_output(['git','rev-parse',args.revision or 'HEAD'],cwd=root,text=True).strip(),'workingTreeSources':not bool(args.revision),'sourceSha256':{str(f.relative_to(root)):hashlib.sha256(read(str(f.relative_to(root))).encode()).hexdigest() for f in [root/'Assets/Scripts/UI/GameUI.cs',root/'Assets/Scripts/UI/GameUI.Modes.cs',root/'Assets/Scripts/UI/GameUI.ChapterSeals.cs',root/'Assets/Scripts/UI/ChapterSealPresentation.cs',root/'Assets/Scripts/UI/RoomObjectivePresentation.cs']}}
 if args.output:args.output.parent.mkdir(parents=True,exist_ok=True);args.output.write_text(json.dumps(result,indent=2))
 print(json.dumps(result,indent=2))
