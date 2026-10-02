#!/usr/bin/env python3
"""Actual preview host lifecycle and clock; native/render/model stand-ins explicitly scoped."""
import os,sys,subprocess,tempfile
from pathlib import Path
root=Path(__file__).resolve().parents[1];dotnet=sys.argv[1] if len(sys.argv)>1 else os.environ.get('DOTNET','dotnet')
with tempfile.TemporaryDirectory(prefix='preview-presentation-') as directory:
 p=Path(directory)
 for name in ['CollectionModelPreview','CollectionPreviewState','CollectionPreviewComposition']:(p/(name+'.cs')).write_text((root/'Assets/Scripts/UI'/(name+'.cs')).read_text())
 for name in ['CollectionRenderLifecycleTests','CollectionPreviewCompositionTests']:(p/(name+'.cs')).write_text((root/'Tests'/(name+'.cs')).read_text())
 (p/'Program.cs').write_text('System.Console.WriteLine(CollectionRenderLifecycleTests.Run());System.Console.WriteLine(CollectionPreviewCompositionTests.Run());');project=p/'Test.csproj';project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType></PropertyGroup></Project>');(p/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>')
 env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1');cmd=[dotnet,'run','--project',str(project),'--no-restore'];subprocess.run([dotnet,'restore',str(project),'--configfile',str(p/'NuGet.Config'),'-v:q'],env=env,check=True);subprocess.run(cmd,env=env,check=True)
 source=p/'CollectionModelPreview.cs';original=source.read_text()
 for before,after,expected in [('model.SamplePreview(motion.Time,motion.Action,motion.Progress);','model.SamplePreview(0,CollectionPreviewAction.Idle,1);','actual preview host samples selected action on its local clock'),('if(framingDirty){FrameModel();framingDirty=false;}','if(true){FrameModel();framingDirty=false;}','local pose motion reuses model texture and cached framing')]:
  assert original.count(before)==1;source.write_text(original.replace(before,after));subprocess.run([dotnet,'build',str(project),'--no-restore','-v:q'],env=env,check=True,stdout=subprocess.DEVNULL)
  result=subprocess.run(cmd+['--no-build'],env=env,capture_output=True,text=True);assert result.returncode and 'System.Exception: '+expected in result.stdout+result.stderr,result.stdout+result.stderr
 print('PASS: ignored presentation action and uncached pose envelope compiled negative controls fail exact host assertions')
