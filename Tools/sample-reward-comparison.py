#!/usr/bin/env python3
"""Compare guaranteed material payouts only; no run-time, drop-rate or balance verdict."""
import argparse, importlib.util, os, subprocess, tempfile
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
parser=argparse.ArgumentParser(description=__doc__)
parser.add_argument('--dotnet',default=os.environ.get('DOTNET','dotnet'))
parser.add_argument('--output',type=Path,required=True)
a=parser.parse_args();output=a.output.resolve()
if output.exists():parser.error('output must be a new file')
spec=importlib.util.spec_from_file_location('validation',ROOT/'Tools/cloud-validation.py')
v=importlib.util.module_from_spec(spec);spec.loader.exec_module(v)
with tempfile.TemporaryDirectory(prefix='emberfall-reward-comparison-') as directory:
    directory=Path(directory)
    sources=[ROOT/('Assets/Scripts/Core/'+name+'.cs') for name in ['ExpeditionModeState','TierRewardBand']]
    program=r'''using System;using System.IO;using System.Text;using Emberfall;
class Program {static void Main(string[] args){
var csv=new StringBuilder("tier,mode,current_guaranteed_materials,proposal_hold_base_3,delta,interpretation\n");
foreach(int tier in new[]{1,5,10,20,40,100})foreach(ExpeditionModeKind mode in Enum.GetValues(typeof(ExpeditionModeKind))){
int current=new ExpeditionModeState(mode,tier,7).Reward.Materials;
int proposed=mode==ExpeditionModeKind.HoldPoint?TierRewardBand.Materials(3,tier):current;
csv.AppendLine(tier+","+mode+","+current+","+proposed+","+(proposed-current)+",proposal_only_no_game_change");}
using(var file=new FileStream(args[0],FileMode.CreateNew,FileAccess.Write))using(var writer=new StreamWriter(file))writer.Write(csv.ToString());
Console.WriteLine("Wrote 18 production reward comparisons; no duration or player-experience estimate.");}}
'''
    project=v.write_project(directory/'project',sources,program)
    config=directory/'NuGet.Config';config.write_text('<configuration><packageSources><clear /></packageSources></configuration>')
    env=dict(os.environ,DOTNET_CLI_HOME=str(directory/'cli'),DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1',DOTNET_CLI_TELEMETRY_OPTOUT='1')
    subprocess.run([a.dotnet,'restore',str(project),'--configfile',str(config),'--verbosity','quiet'],env=env,check=True)
    output.parent.mkdir(parents=True,exist_ok=True)
    subprocess.run([a.dotnet,'run','--project',str(project),'--no-restore','-c','Release','--',str(output)],env=env,check=True)
