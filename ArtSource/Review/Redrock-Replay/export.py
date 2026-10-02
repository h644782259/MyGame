#!/usr/bin/env python3
"""Export real chapter plans, host spawn placement and navigation; draw a review-only SVG.
Run: python3 ArtSource/Review/Redrock-Replay/export.py /path/to/dotnet
No Unity execution. Uses existing managed value/scene contract shims, not copied navigation.
"""
import hashlib, importlib.util, json, os, subprocess, sys, tempfile
from pathlib import Path
out=Path(__file__).resolve().parent
root=out.parents[2]
spec=importlib.util.spec_from_file_location('cv',root/'Tools/cloud-validation.py')
cv=importlib.util.module_from_spec(spec);spec.loader.exec_module(cv)
dotnet=sys.argv[1] if len(sys.argv)>1 else os.environ.get('DOTNET','dotnet')
source=root/'Assets/Scripts/Core/GameSession.Chapter.cs'
text=source.read_text();start=text.index('private bool TryChapterSpawn(');opening=text.index('{',start);end=opening+1;depth=1
while depth:
 depth+=(text[end]=='{')-(text[end]=='}');end+=1
method=text[start:end]
files=[root/p for p in ['Assets/Scripts/World/WorldTraversal.cs','Assets/Scripts/World/ChapterRoomGeometry.cs','Assets/Scripts/Core/ChapterProgression.cs','Assets/Scripts/Core/RoomTactics.cs','Tests/DestructibleTraversalTests.cs','Tests/ChapterGeometryFixture.cs']]
program=r'''
using System;using System.Collections.Generic;using System.Linq;using System.Text.Json;using UnityEngine;using Emberfall;
class Host {
 ChapterRoomPlan chapterPlan;bool ForestMobileLineup=>false;int ChapterSeed=>chapterPlan.Seed;int ChapterRoomIndex=>chapterPlan.Room;
 METHOD
 static float[] Point(Vector3 p)=>new[]{p.x,p.z};
 public static object Export(bool split){
  const int rawSeed=0;const float actorRadius=.65f;
  var plan=ChapterRoomGeometry.Plan(ChapterNode.Redrock,1,ChapterRoomGeometry.RedrockReplaySeed(rawSeed,split));
  WorldTraversal.Reset(ZoneKind.Dungeon);ChapterRoomGeometry.Register(plan);
  var host=new Host{chapterPlan=plan};var occupied=new List<Vector3>();var spawns=new List<object>();
  for(int i=0;i<6;i++){float radius=i==2||i==3?.65f:.5f;Vector3 at;
   if(!host.TryChapterSpawn(i,true,occupied,radius,out at))throw new Exception("spawn failed");
   occupied.Add(at);spawns.Add(new{index=i,position=Point(at),radius,reachable=WorldTraversal.CanReach(plan.Entrance,at,radius)});}
  var from=new Vector3(-4,0,-1);var to=new Vector3(4,0,-1);var path=WorldTraversal.FindPath(from,to,actorRadius);
  float length=0;var previous=from;foreach(var point in path){length+=Vector3.Distance(previous,point);previous=point;}
  return new{variant=split?"Split wall":"Continuous wall",rawSeed,seed=plan.Seed,mirrorBit=plan.Seed&1,roomIndex=plan.Room,layout=plan.Layout,actorRadius,
   entrance=Point(plan.Entrance),exit=Point(plan.Exit),objectives=plan.Objectives.Select(Point),
   obstacles=plan.Obstacles.Select(o=>new{center=Point(o.Center),size=new[]{o.Size.x,o.Size.y},radius=o.Radius,height=o.Height}),
   spawns,probe=new{from=Point(from),to=Point(to),waypoints=path.Select(Point),length,lineOfSight=WorldTraversal.HasLineOfSight(from,to),directMovement=WorldTraversal.HasGroundPath(from,to,actorRadius)},
   rangedBranchLineOfSight=WorldTraversal.HasLineOfSight(occupied[0],occupied[1])};
 }
 static void Main(){Console.WriteLine(JsonSerializer.Serialize(new[]{Export(false),Export(true)},new JsonSerializerOptions{WriteIndented=true}));}
}
'''.replace('METHOD',method)
with tempfile.TemporaryDirectory(prefix='redrock-evidence-') as temp:
 folder=Path(temp);config=folder/'NuGet.Config';config.write_text('<configuration><packageSources><clear /></packageSources></configuration>')
 project=cv.write_project(folder/'build',files,program=program)
 env=dict(os.environ,DOTNET_CLI_HOME=str(folder/'cli'),DOTNET_PROCESSOR_COUNT='2',DOTNET_NOLOGO='1')
 subprocess.run([dotnet,'build',str(project),'--configfile',str(config),'-v:q'],check=True,env=env)
 result=subprocess.run([dotnet,str(project.parent/'bin/Debug/net8.0/Validation.dll')],check=True,env=env,capture_output=True,text=True)
 data=json.loads(result.stdout)
payload={'scope':'Geometry/navigation illustration from production code with managed shims; not Unity footage or gameplay testing.',
 'view':'Orthographic X/Z top-down, identical scale and bounds; x right, z up; metres.',
 'sourceSha256':{str(p.relative_to(root)):hashlib.sha256(p.read_bytes()).hexdigest() for p in files+[source]},
 'fixtures':'Redrock roomIndex 1, Hard/Heroic crossfire host placement; probe radius 0.65 m; spawn markers use admission-clearance radii, not runtime enemy radii; no transient hazard active.',
 'variants':data}
(out/'routes.json').write_text(json.dumps(payload,indent=2)+'\n')
# All spatial marks below come from the export. Frame, colours and label offsets are presentation only.
svg=['<svg xmlns="http://www.w3.org/2000/svg" width="1100" height="760" viewBox="0 0 1100 760">',
 '<rect width="1100" height="760" fill="#101822"/>',
 '<style>text{font-family:DejaVu Sans,Arial,sans-serif;fill:#e8edf2;font-size:14px}.small{font-size:12px}.title{font-size:23px;font-weight:bold}</style>',
 '<text x="28" y="38" class="title">Redrock replay: central cross passage</text>',
 '<text x="28" y="64">Geometry/navigation illustration — production outputs, not Unity footage or gameplay testing</text>']
for index,variant in enumerate(data):
 cx=280+index*540;cy=365;scale=13
 def xy(p):return (cx+p[0]*scale,cy-p[1]*scale)
 def label(p,text,dy=-12,color='#e8edf2'):
  x,y=xy(p);svg.append(f'<text x="{x+8:.2f}" y="{y+dy:.2f}" style="fill:{color}" class="small">{text}</text>')
 svg.append(f'<text x="{cx-230}" y="99" font-weight="bold">{variant["variant"]} · seed {variant["seed"]}</text>')
 for axis in range(-15,16,5):
  a,b=xy([axis,-18]);c,d=xy([axis,18]);svg.append(f'<path d="M{a},{b} L{c},{d}" stroke="#293645"/>')
  a,b=xy([-18,axis]);c,d=xy([18,axis]);svg.append(f'<path d="M{a},{b} L{c},{d}" stroke="#293645"/>')
 for obstacle in variant['obstacles']:
  x,y=xy(obstacle['center']);w,h=[v*scale for v in obstacle['size']]
  svg.append(f'<rect x="{x-w/2:.2f}" y="{y-h/2:.2f}" width="{w}" height="{h}" fill="#9f7357" stroke="#d8b292"/>')
 for key,color in [('entrance','#74d0ff'),('exit','#6cf0be')]:
  x,y=xy(variant[key]);svg.append(f'<circle cx="{x}" cy="{y}" r="6" fill="{color}"/>');label(variant[key],key.capitalize())
 for point in variant['objectives']:
  x,y=xy(point);svg.append(f'<circle cx="{x}" cy="{y}" r="12" fill="none" stroke="#ffe27a" stroke-width="2"/>');label(point,'Escape objective',dy=22)
 for spawn in variant['spawns']:
  x,y=xy(spawn['position']);r=spawn['radius']*scale;svg.append(f'<circle cx="{x}" cy="{y}" r="{r}" fill="#dd757b"/>');label(spawn['position'],str(spawn['index']),dy=-8)
 probe=variant['probe'];a,b=xy(probe['from']);c,d=xy(probe['to'])
 svg.append(f'<path d="M{a},{b} L{c},{d}" stroke="#ffd76b" stroke-width="2" stroke-dasharray="5 5"/>')
 points=' '.join(f'{xy(p)[0]:.2f},{xy(p)[1]:.2f}' for p in [probe['from']]+probe['waypoints'])
 svg.append(f'<polyline points="{points}" fill="none" stroke="#71e4fa" stroke-width="3"/>')
 for point in [probe['from'],probe['to']]:
  x,y=xy(point);svg.append(f'<circle cx="{x}" cy="{y}" r="{variant["actorRadius"]*scale}" fill="none" stroke="#71e4fa" stroke-width="2"/>')
 label(probe['from'],'A',dy=24);label(probe['to'],'B',dy=24)
 svg.append(f'<text x="{cx-230}" y="622">A → B: {probe["length"]:.2f} m returned route · LOS {"open" if probe["lineOfSight"] else "blocked"}</text>')
 svg.append(f'<text x="{cx-230}" y="646">Existing ranged branches (0 ↔ 1): LOS {"open" if variant["rangedBranchLineOfSight"] else "blocked"}</text>')
svg+=['<text x="28" y="688">Same raw seed 0 / mirror 0 · same X/Z scale · navigation probe radius 0.65 m · cyan = actual path</text>',
 '<text x="28" y="710">Yellow dashed = sight probe · pink = six actual host spawn positions (circles show spawn-clearance radii)</text>',
 '<text x="28" y="732">Four-metre wall opening; props, overhead hoist, camera, animations and transient heat are not rendered.</text>','</svg>']
(out/'routes.svg').write_text('\n'.join(svg)+'\n')
print('Exported routes.json + routes.svg from production Plan, TryChapterSpawn and WorldTraversal.')
