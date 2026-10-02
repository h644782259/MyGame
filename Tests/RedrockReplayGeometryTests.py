#!/usr/bin/env python3
"""Run actual chapter/navigation geometry and a compiled continuous-wall negative control."""
import importlib.util, os, subprocess, sys, tempfile
from pathlib import Path
root=Path(__file__).resolve().parents[1]
spec=importlib.util.spec_from_file_location('cv',root/'Tools/cloud-validation.py')
cv=importlib.util.module_from_spec(spec);spec.loader.exec_module(cv)
dotnet=sys.argv[1] if len(sys.argv)>1 else os.environ.get('DOTNET','dotnet')
files=[root/p for p in ['Assets/Scripts/World/WorldTraversal.cs','Assets/Scripts/World/ChapterRoomGeometry.cs','Assets/Scripts/Core/ChapterProgression.cs','Assets/Scripts/Core/RoomTactics.cs','Assets/Scripts/Core/ArenaPulseRules.cs','Assets/Scripts/World/ChapterHazardGeometry.cs','Assets/Scripts/World/TacticalRoomGeometry.cs','Tests/DestructibleTraversalTests.cs','Tests/ChapterGeometryFixture.cs','Tests/ChapterRoomGeometryTests.cs','Tests/ChapterFormationGeometryTests.cs']]
program='System.Console.WriteLine(ChapterRoomGeometryTests.Run());System.Console.WriteLine(ChapterFormationGeometryTests.Run());'
with tempfile.TemporaryDirectory(prefix='redrock-route-') as folder:
 out=Path(folder);config=out/'NuGet.Config';config.write_text('<configuration><packageSources><clear /></packageSources></configuration>')
 env=dict(os.environ,DOTNET_CLI_HOME=str(out/'cli'),DOTNET_NOLOGO='1')
 for mutant in [False,True]:
  source=root/'Assets/Scripts/World/ChapterRoomGeometry.cs';inputs=files
  if mutant:
   changed=out/'ContinuousWall.cs';text=source.read_text();before='if(room==1&&RedrockSplitRoute(seed))';assert before in text
   # Retain four obstacles/seed encoding but close the gap: identity-only tests cannot catch this.
   text=text.replace('new Vector3(0,0,-5),new Vector2(4,4)','new Vector3(0,0,-4),new Vector2(4,6)').replace('new Vector3(0,0,3),new Vector2(4,4)','new Vector3(0,0,2),new Vector2(4,6)')
   changed.write_text(text);inputs=[changed if path==source else path for path in files]
  project=cv.write_project(out/('negative' if mutant else 'positive'),inputs,program=program)
  subprocess.run([dotnet,'build',str(project),'--configfile',str(config),'-v:q'],env=env,check=True)
  run=subprocess.run([dotnet,str(project.parent/'bin/Debug/net8.0/Validation.dll')],env=env,capture_output=True,text=True)
  if mutant:
   assert run.returncode and 'System.Exception: SPLIT_CROSSING' in run.stdout+run.stderr,run.stdout+run.stderr
   print('PASS: compiled continuous-wall control fails actual navigation/LOS crossing oracle')
  else:
   print(run.stdout,end='');assert not run.returncode,run.stderr
