import importlib.util, sys, pathlib, subprocess, tempfile, os, json, hashlib
root=pathlib.Path(sys.argv[1]).resolve()
out=pathlib.Path(sys.argv[2]).resolve();out.mkdir(parents=True,exist_ok=True)
spec=importlib.util.spec_from_file_location('cv',root/'Tools/cloud-validation.py')
cv=importlib.util.module_from_spec(spec);spec.loader.exec_module(cv)
refs=list(pathlib.Path('/workspace/emberfall_win/Tools/ReferenceAssemblies/UnityEngine/lib/netstandard2.0').glob('*.dll'))
assert refs
sdk='/workspace/shared/emberfall-tools/dotnet/dotnet'
sources=sorted((root/'Assets/Scripts').rglob('*.cs'))
hashes=lambda:{str(p.relative_to(root)):hashlib.sha256(p.read_bytes()).hexdigest() for p in sources}
report={'head':subprocess.check_output(['git','rev-parse','HEAD'],cwd=root,text=True).strip(),'scope':'Managed C#9 compile against pinned Unity 2021.3.33 APIs; no Unity Editor, build, rendering or device execution','sourceHashes':hashes(),'checks':[]}
with tempfile.TemporaryDirectory() as td:
    work=pathlib.Path(td)
    config=work/'NuGet.Config';config.write_text('<configuration><packageSources><clear /></packageSources></configuration>')
    env=dict(os.environ,DOTNET_CLI_TELEMETRY_OPTOUT='1',DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1',DOTNET_NOLOGO='1',DOTNET_CLI_HOME=str(work/'dotnet-home'),NUGET_PACKAGES=str(work/'nuget'))
    for name,defines in [('windows','UNITY_STANDALONE;UNITY_STANDALONE_WIN'),('ios','UNITY_IOS'),('android','UNITY_ANDROID')]:
        proj=cv.write_project(work/name,sources,references=refs,defines=defines)
        log='';rc=0
        for args in [['restore',str(proj),'--configfile',str(config),'--verbosity','quiet'],['build',str(proj),'--no-restore','--configuration','Release','--verbosity','minimal']]:
            p=subprocess.run([sdk,*args],env=env,text=True,stdout=subprocess.PIPE,stderr=subprocess.STDOUT)
            log+=p.stdout;rc=p.returncode
            if rc:break
        (out/(name+'.log')).write_text(log)
        report['checks'].append({'platform':name,'defines':defines,'passed':rc==0,'sourceCount':len(sources),'log':name+'.log'})
        print(name,rc,flush=True)
report['sourceUnchanged']=report['sourceHashes']==hashes()
report['passed']=report['sourceUnchanged'] and all(x['passed'] for x in report['checks'])
(out/'report.json').write_text(json.dumps(report,indent=2)+'\n')
sys.exit(0 if report['passed'] else 1)
