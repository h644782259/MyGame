#!/usr/bin/env python3
"""Compile runtime against local pinned references; never launch Unity or download."""
from pathlib import Path
import tempfile,subprocess,importlib.util,os,sys
r=Path(sys.argv[1]);references=Path(sys.argv[2]);sdk=sys.argv[3]
s=importlib.util.spec_from_file_location('cv',r/'Tools/cloud-validation.py');cv=importlib.util.module_from_spec(s);s.loader.exec_module(cv)
with tempfile.TemporaryDirectory(prefix='protection-api-') as t:
 p=Path(t);config=p/'NuGet.Config';config.write_text('<configuration><packageSources><clear/></packageSources></configuration>');env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1')
 for platform,define in [('win','UNITY_STANDALONE;UNITY_STANDALONE_WIN'),('ios','UNITY_IOS'),('android','UNITY_ANDROID')]:
  project=cv.write_project(p/platform,sorted((r/'Assets/Scripts').rglob('*.cs')),references=list(references.glob('*.dll')),defines=define)
  print(platform,flush=True);subprocess.run([sdk,'build',str(project),'--configfile',str(config),'-v:q'],env=env,check=True)
