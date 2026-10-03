from pathlib import Path
import subprocess,hashlib,json,sys
win=Path('/workspace/emberfall_win');ios=Path('/workspace/emberfall_ios');out=Path(sys.argv[3]);out.mkdir(parents=True,exist_ok=True)
refs={'win':sys.argv[1],'ios':sys.argv[2]}
def git(repo,*args):return subprocess.check_output(['git',*args],cwd=repo)
def manifest(repo,rev,folders):
 names=git(repo,'ls-tree','-r','--name-only','-z',rev,*folders).decode().split('\0')[:-1]
 return {n:hashlib.sha256(git(repo,'show',rev+':'+n)).hexdigest() for n in names}
w=manifest(win,refs['win'],['Assets','Tests','Tools','ArtSource']);i=manifest(ios,refs['ios'],['Assets','Tests','Tools','ArtSource'])
runtime=[p for p in w if p.startswith(('Assets/Scripts/','Assets/Resources/'))];different=[p for p in runtime if i.get(p)!=w[p]]
baseline={'win':'25096ba7dbf6e9d7ba9463ec29104c2c462da426','ios':'1cd7ce36c77064757899788e23520fb796266888'}
for path in different:
 assert w[path]==hashlib.sha256(git(win,'show',baseline['win']+':'+path)).hexdigest() and i[path]==hashlib.sha256(git(ios,'show',baseline['ios']+':'+path)).hexdigest(),path
common=sorted(p for p in w.keys()&i.keys() if w[p]!=i[p]);onlyw=sorted(w.keys()-i.keys());onlyi=sorted(i.keys()-w.keys())
# Platform-only inputs must already exist unchanged at the accepted initial baseline.
for repo,now,base,paths in [(win,w,baseline['win'],onlyw),(ios,i,baseline['ios'],onlyi)]:
 for path in paths:assert now[path]==hashlib.sha256(git(repo,'show',base+':'+path)).hexdigest(),path
report={'scope':'Frozen complete Assets, Tests, Tools and editable ArtSource input identity; not an engine/platform build','commits':refs,'trees':{k:git(win if k=='win' else ios,'rev-parse',v+'^{tree}').decode().strip() for k,v in refs.items()},'runtimeFiles':len(runtime),'runtimeCsFiles':sum(p.endswith('.cs') for p in runtime),'preservedPlatformRuntimeDifferences':different,'newRuntimeDifferences':[],'sameNamedInputDifferences':common,'iosOnlyInputs':onlyi,'winOnlyInputs':onlyw,'winManifest':w,'iosManifest':i}
(out/'source-manifest.json').write_text(json.dumps(report,indent=2)+'\n')
print('Compared runtime files:',len(runtime),'C#:',report['runtimeCsFiles'],'preserved baseline differences:',different,'allAssets/tests/tools/art:',len(w),len(i))
