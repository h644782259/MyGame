#!/usr/bin/env python3
"""Generate isolated level-50 profiles and rule probes; never runs Unity/combat."""
import argparse
import csv
import json
import importlib.util
import os
from pathlib import Path
import subprocess
import tempfile
ROOT=Path(__file__).resolve().parents[1]
def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--dotnet',default=os.environ.get('DOTNET','dotnet'))
    parser.add_argument('--output',type=Path,required=True)
    parser.add_argument('--compare',type=Path,help='prior rule-probe CSV; compare named fields, never treat as combat evidence')
    args=parser.parse_args()
    output=args.output.resolve()
    if output.exists():parser.error('output must be a new directory, never a player save or previous sample')
    # Guard the few literal production proc delays until they become shared constants.
    player=(ROOT/'Assets/Scripts/Combat/PlayerController.cs').read_text()
    assert 'returningBladeProc.TryTrigger(1.5f)' in player and 'venomSpreadProc.TryTrigger(2f)' in player
    fixture=(ROOT/'Assets/Editor/CombatReviewFixture.cs').read_text()
    assert 'CombatReviewConfigurations.CreateAll()' in fixture and 'CombatReviewBuildSetup.Apply(game.Progression,config,expected)' in fixture
    spec=importlib.util.spec_from_file_location('validation',ROOT/'Tools/cloud-validation.py')
    validation=importlib.util.module_from_spec(spec);spec.loader.exec_module(validation)
    sources=['Assets/Scripts/Core/'+name+'.cs' for name in ['GameTypes','ProgressionGoalState','ProgressionService','CombatBalance','HubTravelRules','MasteryCoreRuntime','TierRewardRules','TierRewardBand','SkillRuntime','CombatReviewConfigurations']]
    sources+=['Assets/Scripts/Combat/'+name+'.cs' for name in ['PlayerUpgradeRules','CompanionRules']]
    sources+=['Assets/Editor/CombatReviewBuildSetup.cs','Tests/ProgressionTests.cs','Tests/GrowthReviewFixtureTests.cs']
    with tempfile.TemporaryDirectory(prefix='EmberfallGrowthReview-') as temp:
        temp=Path(temp);config=temp/'NuGet.Config';config.write_text('<configuration><packageSources><clear /></packageSources></configuration>')
        project=validation.write_project(temp/'project',[ROOT/s for s in sources],
            'using System;class Program{static void Main(string[] a){Console.WriteLine(GrowthReviewFixtureTests.Run(a[0]));}}')
        env=dict(os.environ,DOTNET_CLI_HOME=str(temp/'cli'),DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1',DOTNET_CLI_TELEMETRY_OPTOUT='1')
        subprocess.run([args.dotnet,'restore',str(project),'--configfile',str(config),'--verbosity','quiet'],env=env,check=True)
        subprocess.run([args.dotnet,'run','--project',str(project),'--no-restore','--configuration','Release','--',str(output)],env=env,check=True)
    if args.compare:
        def read_rows(path):
            with path.open(newline='') as f:return {row['fixture']:row for row in csv.DictReader(f)}
        prior=read_rows(args.compare);current=read_rows(output/'Growth-Review-Rule-Probes.csv')
        changes={key:{field:{'before':prior.get(key,{}).get(field),'after':current.get(key,{}).get(field)}
            for field in set(prior.get(key,{}))|set(current.get(key,{}))
            if prior.get(key,{}).get(field)!=current.get(key,{}).get(field)} for key in set(prior)|set(current)}
        changes={key:value for key,value in changes.items() if value}
        (output/'rule-comparison.json').write_text(json.dumps({'evidence':'managed_rule_probes_not_combat','changes':changes},indent=2,sort_keys=True)+'\n')
        print('Rule-probe comparison changed fixtures:',len(changes))
    print('Prepared managed profiles and synthetic eligible-event probes:',output)
if __name__=='__main__':main()
