#!/usr/bin/env python3
"""Reproduce archived FBX fidelity checks without changing archive or candidate.
Python stdlib wrapper; requires the verified Blender 4.3.2 executable.
"""
import argparse,hashlib,json,os,shutil,subprocess,time,traceback
from pathlib import Path

def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def require(test,message):
 if not test:raise RuntimeError(message)
def main():
 parser=argparse.ArgumentParser(description=__doc__)
 for name in ('source-root','evidence-root','output','blender'):parser.add_argument('--'+name,required=True)
 a=parser.parse_args();source=Path(a.source_root).resolve();evidence=Path(a.evidence_root).resolve();out=Path(a.output).resolve();blender=shutil.which(a.blender) or a.blender
 require(not out.exists(),'Output must be a fresh, nonexistent directory')
 require(not out.is_relative_to(source) and not out.is_relative_to(evidence),'Output must be outside source/evidence trees')
 out.mkdir(parents=True);summary={'completed':False,'pass':False,'sourceRoot':str(source),'evidenceRoot':str(evidence),'output':str(out),'blender':blender,'commands':[],'replacements':{}};summarypath=out/'run-summary.json'
 def save():summarypath.write_text(json.dumps(summary,indent=2)+'\n')
 save()
 try:
  archive=json.loads((evidence/'artifact-manifest.json').read_text());exp=evidence/'Experiment';historical=json.loads((exp/'hardened-report.json').read_text())
  # Archive checksum audit precedes all copying. Originals remain unchanged.
  for rel,record in archive['files'].items():
   p=evidence/rel;require(p.is_file() and sha(p)==record['sha256'] and p.stat().st_size==record['bytes'],'Archive mismatch: '+rel)
  summary['archiveFilesVerified']=len(archive['files']);summary['archiveManifestSHA256']=sha(evidence/'artifact-manifest.json')
  sourcefiles={'blend':source/'ArtSource/BlenderPilot/Emberfall-Pilot-Vanguard.blend','candidateFBX':source/'Assets/Resources/BlenderPilot/Vanguard.fbx','helper':source/'ArtSource/BlenderPilot/export_vanguard_fbx.py'}
  sourcefiles.update({name:source/'Assets/Resources/BlenderPilot'/name for name in ['EmberfallPilot_Atlas.png','EmberfallPilot_MetallicSmoothness.png']})
  before={k:sha(p) for k,p in sourcefiles.items()};summary['candidateInputHashes']=before
  require(before['blend']==historical['sourceBlendSha256'],'Candidate blend differs from locked authored source')
  require(before['candidateFBX']==sha(exp/'helper-output.fbx'),'Candidate FBX differs from archived final helper output')
  require(before['helper']==sha(exp/'export_vanguard_fbx.py'),'Candidate helper differs from verified archived helper')
  for n in ['EmberfallPilot_Atlas.png','EmberfallPilot_MetallicSmoothness.png']:require(before[n]==sha(exp/n),'Candidate texture differs: '+n)
  originals=out/'OriginalScripts';originals.mkdir();stage=out/'Stage';stage.mkdir();packed=stage/'Source';baselinedir=stage/'Baseline';baselinedir.mkdir()
  for p in sorted(exp.glob('*.py')):shutil.copyfile(p,originals/p.name)
  blend=packed/'ArtSource/BlenderPilot/Emberfall-Pilot-Vanguard.blend';blend.parent.mkdir(parents=True);shutil.copyfile(sourcefiles['blend'],blend)
  baseline=baselinedir/'Vanguard.fbx';shutil.copyfile(evidence/'Inputs/Baseline-Vanguard.fbx',baseline);require(sha(baseline)==historical['baselineFBXSha256'],'Baseline archive hash mismatch')
  for n in ['candidate-samples.json','candidate-stop-samples.json']:
   shutil.copyfile(evidence/'Inputs'/n,stage/n);require(sha(stage/n)==historical['inputs'][n],'Locked policy input mismatch: '+n)
  for n in ['EmberfallPilot_Atlas.png','EmberfallPilot_MetallicSmoothness.png']:
   for folder in [stage,baselinedir,packed/'Assets/Resources/BlenderPilot']:
    folder.mkdir(parents=True,exist_ok=True);shutil.copyfile(sourcefiles[n],folder/n)
  replacements={
   'hardened_probe.py':[("repo=Path('/workspace/emberfall_android_player_journey')",'repo=Path('+repr(str(packed))+')'),("original=repo/'Assets/Resources/BlenderPilot/Vanguard.fbx'",'original=Path('+repr(str(baseline))+')')],
   'helper_controls.py':[("Path('/workspace/emberfall_android_player_journey/ArtSource/BlenderPilot/Emberfall-Pilot-Vanguard.blend')",'Path('+repr(str(blend))+')')],
   'hardened_static_invariants.py':[("Path('/workspace/emberfall_android_player_journey/Assets/Resources/BlenderPilot/Vanguard.fbx')",'Path('+repr(str(baseline))+')')],
   'raw_key_counts.py':[("Path('/workspace/emberfall_android_player_journey/Assets/Resources/BlenderPilot/Vanguard.fbx')",'Path('+repr(str(baseline))+')')]}
  scripts=['hardened_probe.py','helper_controls.py','hardened_static_invariants.py','helper_equivalence.py','raw_key_counts.py','export_vanguard_fbx.py','layered_pose.py']
  for name in scripts:
   text=(originals/name).read_text();changes=[]
   for old,new in replacements.get(name,[]):
    require(text.count(old)==1,'Unexpected path token occurrence: '+name+' '+old);text=text.replace(old,new);changes.append({'from':old,'to':new})
   (stage/name).write_text(text);summary['replacements'][name]={'originalSHA256':sha(originals/name),'stagedSHA256':sha(stage/name),'changes':changes}
  summary['stagedInputHashes']={'blend':sha(blend),'baselineFBX':sha(baseline)};save()
  env=dict(os.environ,BLENDER_USER_CONFIG=str(out/'BlenderConfig'))
  groups=[['hardened_probe.py'],['helper_controls.py'],['hardened_static_invariants.py'],['helper_equivalence.py','raw_key_counts.py']]
  for index,names in enumerate(groups):
   cmd=[blender,'-b','--threads','1','--python-exit-code','1']
   for name in names:cmd+=['--python',str(stage/name)]
   log=out/('%02d-%s.log'%(index,names[0][:-3]));start=time.monotonic()
   with log.open('w') as stream:result=subprocess.run(cmd,cwd=stage,env=env,stdout=stream,stderr=subprocess.STDOUT)
   summary['commands'].append({'argv':cmd,'exitCode':result.returncode,'elapsedSeconds':time.monotonic()-start,'log':log.name});save();require(result.returncode==0,'Command failed: '+names[0]);print('COMPLETE',names,flush=True)
  dense=json.loads((stage/'hardened-report.json').read_text());static=json.loads((stage/'hardened-static-invariants.json').read_text());controls=json.loads((stage/'helper-controls.json').read_text());equiv=json.loads((stage/'helper-equivalence.json').read_text());raw=json.loads((stage/'raw-key-counts.json').read_text())
  require(dense['completed'] and dense['sourceFilesUnchanged'],'Dense run not complete or changed input')
  require(dense['thresholdM']==1e-4 and dense['blender']=='4.3.2','Wrong test threshold or Blender version')
  require(dense['steps']['baseline']['strictPass'] is False and dense['steps']['hardened-mixed']['strictPass'] is True,'Expected baseline FAIL + candidate PASS')
  require(sum(dense['sampleCounts'].values())==4923 and dense['sampleCounts']['layered']==288,'Unexpected sample domain')
  require(dense['finiteGuardNegativeControls']==[{'case':'meshNaN','rejected':True},{'case':'meshInfinity','rejected':True},{'case':'socketNaN','rejected':True}],'Finite negative control manifest mismatch')
  require(controls['pass'] and len(controls['negativeControls'])==3 and all(c['rejected'] and c['exportFunctionRestored'] for c in controls['negativeControls']),'Helper fail-closed controls failed')
  require(set(static['candidates'])=={'hardened-mixed.fbx','helper-output.fbx'} and all(v['staticEqual'] and not v['differentEntries'] for v in static['candidates'].values()),'Static invariants failed')
  require(equiv['allFiveImportedCurvesExactlyEqual'],'Final helper curves differ from measured candidate')
  archivedequiv=json.loads((exp/'helper-equivalence.json').read_text())
  require(equiv['helper-output.fbx']['curveHashes']==archivedequiv['helper-output.fbx']['curveHashes'],'Portable output curves differ from archived candidate')
  archivedstatic=json.loads((exp/'hardened-static-invariants.json').read_text())
  require(static['candidates']['helper-output.fbx']['sha256ByEntry']==archivedstatic['candidates']['helper-output.fbx']['sha256ByEntry'],'Portable output static data differ from archived candidate')
  require(raw['Vanguard.fbx']['rawKeyTimeCount']==43200 and raw['helper-output.fbx']['rawKeyTimeCount']==123300,'Unexpected raw FBX key counts')
  require({k:sha(p) for k,p in sourcefiles.items()}==before,'Candidate input changed during reproduction')
  for rel,record in archive['files'].items():require(sha(evidence/rel)==record['sha256'],'Archive changed during reproduction: '+rel)
  outputsha=sha(stage/'helper-output.fbx');summary.update({'completed':True,'pass':True,'baselineStrictPass':False,'candidateStrictPass':True,'samples':4923,'finiteNegativeControls':3,'helperNegativeControls':3,'candidateInputsUnchanged':True,'archiveUnchanged':True,'reproducedFBXSHA256':outputsha,'inputCandidateFBXSHA256':before['candidateFBX'],'exportByteIdentical':outputsha==before['candidateFBX'],'curvesAndStaticDataExactlyMatchArchivedCandidate':True,'timestampBoundary':'FBX exporter headers may include timestamps. If file bytes differ, exact imported animation curves and all verified static data above remain the acceptance comparison; this wrapper does not label the binary byte-deterministic.','UnityValidated':False})
  summary['resultHashes']={str(p.relative_to(out)):sha(p) for p in sorted(out.rglob('*')) if p.is_file() and p!=summarypath};save();print('PASS: portable archive reproduction, baseline FAIL / candidate PASS,4923 samples',flush=True)
 except Exception:
  summary['error']=traceback.format_exc();save();raise
if __name__=='__main__':main()
