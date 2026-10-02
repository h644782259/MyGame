#!/usr/bin/env python3
"""Production-component allocation/geometry tests with a mandatory old-order negative control."""
import os
from pathlib import Path
import subprocess
import tempfile
root=Path(__file__).resolve().parents[1]
dotnet=os.environ.get('DOTNET','dotnet')
source=(root/'Assets/Scripts/Combat/FilledSkillVfx.cs').read_text()
with tempfile.TemporaryDirectory(prefix='emberfall-filled-vfx-') as temp:
    temp=Path(temp)
    for path in ['Assets/Scripts/Core/FilledVfxRecipes.cs','Assets/Scripts/Combat/CoveredAreaParticles.cs','Assets/Scripts/Core/FilledVfxPlacement.cs','Tests/FilledVfxAllocationTests.cs','Tests/FilledVfxRecipeTests.cs','Assets/Scripts/Combat/WeaponVisualLinks.cs','Assets/Scripts/Core/WeaponStructure.cs','Tests/WeaponVisualLinkTests.cs']:
        (temp/Path(path).name).write_text((root/path).read_text())
    production=temp/'FilledSkillVfx.cs';production.write_text(source)
    (temp/'Program.cs').write_text('System.Console.WriteLine(FilledVfxRecipeTests.Run());System.Console.WriteLine(FilledVfxAllocationTests.Run());System.Console.WriteLine(WeaponVisualLinkTests.Run());')
    project=temp/'Validation.csproj';project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework></PropertyGroup></Project>')
    config=temp/'NuGet.Config';config.write_text('<configuration><packageSources><clear /></packageSources></configuration>')
    subprocess.run([dotnet,'restore',str(project),'--configfile',str(config)],check=True)
    command=[dotnet,'run','--project',str(project),'--no-restore','-c','Release']
    subprocess.run(command,check=True)
    # Move actual primary Add calls after ornaments. This restores the defective mobile allocation order.
    start=source.index('        public static void Impact(');end=source.index('        public static void Charge(',start)
    impact=source[start:end];calls=[]
    for marker in ['fx.Add(rupture,Vector3.up*.07f','fx.Add(main,Vector3.zero','fx.Add(rupture,Vector3.up*.11f']:
        begin=impact.index(marker);finish=impact.index(';',begin)+1;calls.append(impact[begin:finish]);impact=impact[:begin]+impact[finish:]
    close=impact.rfind('        }');impact=impact[:close]+'\n'.join(calls)+'\n'+impact[close:]
    production.write_text(source[:start]+impact+source[end:])
    failed=subprocess.run(command,capture_output=True,text=True)
    if failed.returncode==0 or 'landing base must survive actual allocation' not in failed.stdout+failed.stderr:
        raise AssertionError('Old-order negative control did not fail for the allocation regression: '+failed.stdout+failed.stderr)
    print('PASS: old-order mutation fails actual mobile retained-base allocation; no Unity/GPU execution')

    production.write_text(source)
    bridge=temp/'WeaponVisualLinks.cs';original=bridge.read_text()
    bridge.write_text(original.replace('            transform.position=candidate;', '            simulation.position=candidate; transform.position=candidate;'))
    failed=subprocess.run(command,capture_output=True,text=True)
    if failed.returncode==0 or 'visual convergence never changes trajectory root' not in failed.stdout+failed.stderr:
        raise AssertionError('Logical-root mutation did not fail: '+failed.stdout+failed.stderr)
    print('PASS: visual-writing-logical-root negative control fails the production transform invariant')
