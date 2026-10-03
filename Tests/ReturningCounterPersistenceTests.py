from pathlib import Path
import sys,importlib.util,tempfile,subprocess,os
r=Path(__file__).resolve().parents[1];dotnet=sys.argv[1]
s=importlib.util.spec_from_file_location('cv',r/'Tools/cloud-validation.py');cv=importlib.util.module_from_spec(s);s.loader.exec_module(cv)
core=['GameTypes','ProgressionService','ProgressionService.Chapter','ChapterProgression','RoomTactics','ProgressionService.Reforge','ReforgeQuote','CombatBalance','HubTravelRules','MasteryCoreRuntime','TierRewardRules','TierRewardBand','ProgressionGoalState']
files=[r/'Assets/Scripts/Core'/f'{x}.cs' for x in core]+[r/'Tests'/f'{x}.cs' for x in ['ProgressionTests','ProgressionGrowthTests','ReturningCounterPersistenceTests']]
with tempfile.TemporaryDirectory(prefix='returning-save-') as t:
 p=Path(t);project=cv.write_project(p/'p',files,program='using System;class Program{static void Main(string[] args){Console.WriteLine(ReturningCounterPersistenceTests.Run(args[0]));Console.WriteLine(ProgressionGrowthTests.Run(args[0]));}}');c=p/'NuGet.Config';c.write_text('<configuration><packageSources><clear/></packageSources></configuration>');env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1');subprocess.run([dotnet,'build',str(project),'--configfile',str(c),'-v:q'],env=env,check=True);subprocess.run([dotnet,str(project.parent/'bin/Debug/net8.0/Validation.dll'),str(p/'save')],env=env,check=True)
