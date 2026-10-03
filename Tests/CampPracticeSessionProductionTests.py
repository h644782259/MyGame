#!/usr/bin/env python3
import importlib.util,os,sys,tempfile,subprocess
from pathlib import Path
root=Path(__file__).resolve().parents[1];spec=importlib.util.spec_from_file_location('v',root/'Tools/cloud-validation.py');m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)
with tempfile.TemporaryDirectory(prefix='practice-session-') as directory:
 p=Path(directory);production=p/'Session.cs';production.write_text((root/'Assets/Scripts/Core/GameSession.Practice.cs').read_text());original=production.read_text()
 player=(root/'Assets/Scripts/Combat/PlayerController.cs').read_text();a=player.index('public void Initialize(GameSession game, HeroClass heroClass)');b=player.index('{',a)+1;depth=1
 while depth:depth+=(player[b]=='{')-(player[b]=='}');b+=1
 actual=p/'Initialize.cs';actual.write_text('using UnityEngine;namespace Emberfall{public partial class PlayerController{'+player[a:b]+'}}')
 project=m.write_project(p/'project',[production,actual,root/'Assets/Scripts/Core/CampPracticeRecord.cs',root/'Tests/CampPracticeSessionBoundary.cs'],'using System;class Program{static void Main(){Console.WriteLine(CampPracticeSessionTests.Run());}}')
 config=p/'NuGet.Config';config.write_text('<configuration><packageSources><clear/></packageSources></configuration>');env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1');dotnet=sys.argv[1] if len(sys.argv)>1 else 'dotnet'
 def run(args):
  q=subprocess.run([dotnet]+args,env=env,capture_output=True,text=True);print(q.stdout+q.stderr);return q
 assert run(['restore',str(project),'--configfile',str(config),'-v:q']).returncode==0
 assert run(['run','--project',str(project),'--no-restore']).returncode==0
 for old,new,message in [('Player=practiceOriginalPlayer','Player=Player','timed finish restores exact references'),('practiceRandom=UnityEngine.Random.state','practiceRandom=default(UnityEngine.Random.State)','random state restored')]:
  assert old in original;production.write_text(original.replace(old,new));assert run(['build',str(project),'--no-restore','-v:q']).returncode==0
  q=run([str(project.parent/'bin/Debug/net8.0/Validation.dll')]);assert q.returncode!=0 and message in q.stdout+q.stderr;print('PASS compiled negative control:',message);production.write_text(original)
