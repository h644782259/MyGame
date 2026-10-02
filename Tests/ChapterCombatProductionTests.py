#!/usr/bin/env python3
"""Execute full chapter hazard/boss components with managed Unity object/render substitutes.

Actual phase policy, world geometry, traversal, cover and component methods are compiled.
Default legacy phase tests and precise compiled negative controls remain mandatory.
"""
import argparse
import os
from pathlib import Path
import re
import subprocess
import tempfile
ROOT=Path(__file__).resolve().parents[1]
def once(source,old,new):
    assert source.count(old)==1, 'expected unique seam: '+old
    return source.replace(old,new,1)
def main():
    parser=argparse.ArgumentParser();parser.add_argument('dotnet_path',nargs='?');parser.add_argument('--dotnet');args=parser.parse_args()
    dotnet=args.dotnet or args.dotnet_path or os.environ.get('DOTNET','dotnet')
    fixture=(ROOT/'Tests/DestructibleTraversalTests.cs').read_text();fixture='using System;using UnityEngine;'+fixture[fixture.index('namespace Emberfall'):]
    for declaration in ['public static class CombatFx','public struct Vector2','public struct Vector3','public static class Time','public static class Mathf']:
        fixture=fixture.replace(declaration,declaration.replace('class ','partial class ').replace('struct ','partial struct '))
    enums=(ROOT/'Assets/Scripts/Core/ChapterProgression.cs').read_text()
    enums='namespace Emberfall{'+''.join(re.findall(r'public enum Chapter(?:Node|Difficulty)\s*\{[^}]*\}',enums))+'}'
    cases=[('current',None),('no-body-activation','physical hazard grows during warning before active damage'),('no-anchor-reconcile','same attack last anchor break wins over interrupt in both target orders'),('immediate-interrupt','same attack last anchor break wins over interrupt in both target orders'),('old-interrupt-exposure','followup interrupt is recovery not exposure'),('long-followup','followup never chains indefinitely'),('no-followup','hard component must create exactly one full warned followup'),('no-hazard-pause','blocked chapter hazard must preserve clock and health'),('late-owner-cleanup','finished chapter retires hazard at zero delta while blocked')]
    with tempfile.TemporaryDirectory(prefix='chapter-combat-') as temporary:
        for title,expected in cases:
            directory=Path(temporary)/title;directory.mkdir()
            (directory/'FixtureMath.cs').write_text(fixture);(directory/'ChapterEnums.cs').write_text(enums)
            files=['Core/CombatImpactBatch','Core/LargeBossPhaseState','Core/ChapterBossPattern','Core/ArenaPulseRules','Core/CombatSightRules','World/ChapterRoomGeometry','World/WorldTraversal','World/ChapterHazards','World/ChapterHazardGeometry','Combat/LargeExpeditionBoss','Combat/CombatSight','Combat/ThreatVisualStyle']
            for name in files:
                source=(ROOT/('Assets/Scripts/'+name+'.cs')).read_text()
                if title=='no-anchor-reconcile' and name=='Combat/LargeExpeditionBoss':source=once(source,'            ReconcileAnchors();\n            if(State.Phase!=LargeBossPhase.Exposed', '            if(State.Phase!=LargeBossPhase.Exposed')
                if title=='immediate-interrupt' and name=='Combat/LargeExpeditionBoss':source=once(source,'CombatImpactBatch.Resolve(ResolveInterrupt);','ResolveInterrupt();')
                if title=='old-interrupt-exposure' and name=='Core/LargeBossPhaseState':source=once(source,'if (chapterFollowup) { followupPending=false;', 'if (false) { followupPending=false;')
                if title=='long-followup' and name=='Core/LargeBossPhaseState':source=once(source,'chapterFollowup && IsFollowup ? 4f : BeamSeconds','BeamSeconds')
                if title=='no-followup' and name=='Combat/LargeExpeditionBoss':source=once(source,'new LargeBossPhaseState(difficulty!=ChapterDifficulty.Normal)','new LargeBossPhaseState()')
                if title=='no-body-activation' and name=='World/ChapterHazards':source=once(source,'body.localScale=new Vector3(.24f,Mathf.Max(.025f,height),.24f)','body.localScale=new Vector3(.24f,.025f,.24f)')
                if title=='no-hazard-pause' and name=='World/ChapterHazards':source=once(source,'            if(game.InputBlocked)return;\n','')
                if title=='late-owner-cleanup' and name=='World/ChapterHazards':source=once(source,'            if(!ValidOwner){Retire();return;}\n            if(game.InputBlocked)return;','            if(game.InputBlocked)return;\n            if(!ValidOwner){Retire();return;}')
                (directory/(Path(name).name+'.cs')).write_text(source)
            for name in ['ChapterCombatProductionTests','LargeBossPhaseTests']:(directory/(name+'.cs')).write_text((ROOT/('Tests/'+name+'.cs')).read_text())
            (directory/'Program.cs').write_text('System.Console.WriteLine(LargeBossPhaseTests.Run());System.Console.WriteLine(ChapterCombatProductionTests.Run());')
            (directory/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>')
            project=directory/'Validation.csproj';project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType></PropertyGroup></Project>')
            subprocess.run([dotnet,'restore',str(project),'--configfile',str(directory/'NuGet.Config')],check=True,stdout=subprocess.DEVNULL)
            build=subprocess.run([dotnet,'build',str(project),'--no-restore','-c','Release'],text=True,stdout=subprocess.PIPE,stderr=subprocess.STDOUT)
            if build.returncode:print(build.stdout);build.check_returncode()
            result=subprocess.run([dotnet,str(directory/'bin/Release/net8.0/Validation.dll')],text=True,stdout=subprocess.PIPE,stderr=subprocess.STDOUT)
            if expected:
                if result.returncode==0 or ('System.Exception: '+expected) not in result.stdout:raise RuntimeError(title+' did not fail exact assertion:\n'+result.stdout)
                print('PASS: '+title+' compiled and failed exact runtime assertion: '+expected)
            else:print(result.stdout,end='');result.check_returncode()
if __name__=='__main__':main()
