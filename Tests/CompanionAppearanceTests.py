#!/usr/bin/env python3
import os,sys,tempfile,subprocess
from pathlib import Path
r=Path(__file__).resolve().parents[1];source=(r/'Assets/Scripts/Combat/SummonedCompanion.cs').read_text()
def method(signature):
 start=source.index(signature);i=source.index('{',start)+1;depth=1
 while depth:depth+=(source[i]=='{')-(source[i]=='}');i+=1
 return source[start:i]
with tempfile.TemporaryDirectory(prefix='companion-appearance-') as d:
 p=Path(d)
 for f in ['Assets/Scripts/Combat/CombatModel.CompanionAppearance.cs','Assets/Scripts/Combat/CompanionRetirementVisual.cs','Assets/Scripts/Core/CompanionRetirementRules.cs','Tests/CompanionAppearanceFixture.cs']:(p/Path(f).name).write_text((r/f).read_text())
 (p/'Host.cs').write_text('namespace Emberfall {public sealed partial class SummonedCompanion {'+method('public void Dismiss()')+method('private void Dismiss(CompanionRetirementReason reason)')+'}}')
 (p/'Test.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType></PropertyGroup></Project>');(p/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>')
 env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1');dotnet=sys.argv[1] if len(sys.argv)>1 else os.environ.get('DOTNET','dotnet');subprocess.run([dotnet,'restore',str(p/'Test.csproj'),'--configfile',str(p/'NuGet.Config'),'-v:q'],env=env,check=True);subprocess.run([dotnet,'run','--project',str(p/'Test.csproj'),'--no-restore'],env=env,check=True)
 assert 'CompanionRetirementReason.Defeated' in method('public void TakeDamage(') and 'CompanionRetirementReason.Expired' in method('private void Update()')
 recall=method('public static bool FreeRecall(');assert 'recallVisualPending=true' in recall and 'Dismiss' not in recall and 'TakeDamage' not in recall
 assert 'transform.position-followAnchor' in method('private void Update()') and 'model.SetCompanionRecall(' in source
 assert 'SetCompanionAppearance(Form,rank,IsPermanent)' in method('private void RefreshPower(')
 print('PASS expiry/defeat/recall and real rank refresh hooks; recall remains a living navigation command')
