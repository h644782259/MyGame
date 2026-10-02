from pathlib import Path
import hashlib,json,subprocess
out=Path(__file__).parent;snapshot=out/'snapshot'
fixture=(snapshot/'Tests/PilotFacingCommitProductionFixture.cs').read_text()
fixture=fixture.replace('Vector3 aim){Time.frameCount++','Vector3 aim,bool allowCommit=true){Time.frameCount++').replace('p.attackCooldown=0;p.Aim=', 'p.attackCooldown=allowCommit?0:Math.Max(.001f,p.attackCooldown-.02f);p.Aim=')
fixture=fixture.replace('   for(int i=0;i<4;i++){\n    Frame(p,forward,turned);', '   int previousAction=p.model.weaponActionId,commitFrame=Time.frameCount+1;\n   for(int i=0;i<4;i++){\n    Frame(p,forward,turned,i==0);')
fixture=fixture.replace('    C(p.model.actionAge==p.model.actionDuration*.52f,"turned fallback preserves synchronous contact");', '''    C(Math.Abs(p.model.actionAge-(p.model.actionDuration*.52f+i*.02f))<.000001f,"single turn commit advances existing recovery by dt");
    C(p.model.weaponActionId==previousAction+1&&p.model.actionStartedFrame==commitFrame,"positive cooldown skips recommit while FaceAim and Animate continue");
    C(p.attackCooldown>0,"normal cooldown remains positive after initial commit");
    Console.WriteLine("TRACE angle="+angle+" postTurnFrame="+i+" cooldown="+p.attackCooldown+" age="+p.model.actionAge+" phase="+p.model.locomotion.Phase+" action="+p.model.weaponActionId+" visible="+p.model.Visible);''')
fixture='// SCRATCH SUPPLEMENT: cooldown expiry/decrement is a host boundary; exact Update tail remains production.\n'+fixture
(out/'PilotFacingCooldownFixture.cs').write_text(fixture)
runner=(snapshot/'Tests/PilotFacingCommitProductionTests.py').read_text()
runner=runner.replace('r=Path(__file__).resolve().parents[1];', 'r=Path('+repr(str(snapshot))+');')
runner=runner.replace("(p/name).write_text((r/'Tests'/name).read_text())", "(p/name).write_text((Path("+repr(str(out/'PilotFacingCooldownFixture.cs'))+") if name=='PilotFacingCommitProductionFixture.cs' else r/'Tests'/name).read_text())")
(out/'run_cooldown_followup.py').write_text(runner)
revision=subprocess.check_output(['git','-C','/workspace/emberfall_art_pilot','rev-parse','dcd75ac'],text=True).strip()
paths=list((snapshot/'Assets/Scripts').rglob('*.cs'))+[snapshot/'Tests'/s for s in ['BlenderPilotAdapterProductionTests.py','BlenderPilotAdapterProductionFixture.cs','BlenderPilotLayerProductionFixture.cs','BlenderPilotReadinessTests.cs','PilotFacingCommitProductionTests.py','PilotFacingCommitProductionFixture.cs']]
manifest={'frozenRevision':revision,'scope':'Scratch-only cooldown continuation supplement; production Update walking block and aim tail, FaceAim, BasicAttack visual prefix, actual AnimateHero prefix and pilot adapter. Cooldown decrement remains explicit host input boundary; no gameplay damage, full Update, or Unity frame loop claim.','snapshotHashes':{str(p.relative_to(snapshot)):hashlib.sha256(p.read_bytes()).hexdigest() for p in paths},'variantHashes':{p.name:hashlib.sha256(p.read_bytes()).hexdigest() for p in [out/'PilotFacingCooldownFixture.cs',out/'run_cooldown_followup.py']},'command':'DOTNET_CLI_HOME=/workspace/scratch/chapter-root-dotnet python3 -u /workspace/scratch/moving-pilot-facing-followup/run_cooldown_followup.py /workspace/scratch/dotnet/dotnet','trackedEdits':False}
(out/'manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')
