"""Real progression persistence and immutable-quote regression replay."""
from pathlib import Path
import importlib.util,tempfile,os,subprocess,sys
root=Path(__file__).resolve().parents[1];dotnet=sys.argv[1] if len(sys.argv)>1 else 'dotnet'
s=importlib.util.spec_from_file_location('cv',root/'Tools/cloud-validation.py');cv=importlib.util.module_from_spec(s);s.loader.exec_module(cv)
core=['GameTypes','ProgressionService','ProgressionService.Chapter','ChapterProgression','RoomTactics','CombatBalance','HubTravelRules','MasteryCoreRuntime','TierRewardRules','TierRewardBand','ProgressionGoalState','ReforgeQuote']
with tempfile.TemporaryDirectory(prefix='economy-growth-') as t:
 o=Path(t);p=cv.write_project(o/'p',[root/'Assets/Scripts/Core'/f'{n}.cs' for n in core]+[root/'Tests/ProgressionTests.cs',root/'Tests/EconomyGrowthTests.cs'],program='using System;class Program{static void Main(string[] args){Console.WriteLine(EconomyGrowthTests.Run(args[0]));}}')
 c=o/'NuGet.Config';c.write_text('<configuration><packageSources><clear /></packageSources></configuration>');env=dict(os.environ,DOTNET_CLI_HOME=str(o/'cli'),DOTNET_NOLOGO='1')
 subprocess.run([dotnet,'build',str(p),'--configfile',str(c),'-v:q'],env=env,check=True);subprocess.run([dotnet,str(p.parent/'bin/Debug/net8.0/Validation.dll'),str(o/'saves')],env=env,check=True)
