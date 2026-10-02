#!/usr/bin/env python3
"""Read-only clean-room Unity source ZIP audit. No Editor/import/build acceptance claim."""
import argparse,contextlib,collections,datetime,hashlib,json,re,stat,subprocess,tempfile,zipfile
from pathlib import Path,PurePosixPath
p=argparse.ArgumentParser();p.add_argument('--zip',required=True,type=Path);p.add_argument('--manifest',required=True,type=Path);p.add_argument('--inventory',required=True,type=Path);p.add_argument('--repo',required=True,type=Path);p.add_argument('--ledger',required=True,type=Path);p.add_argument('--output',required=True,type=Path);p.add_argument('--extract-to',type=Path,help='Optional new empty extraction directory retained for original-entry test execution');a=p.parse_args();a.output.mkdir(parents=True,exist_ok=True)
def sha(b):return hashlib.sha256(b).hexdigest()
def git(*args):return subprocess.check_output(['git','-C',str(a.repo),*args])
report={'startedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'archive':str(a.zip),'scriptSha256':sha(Path(__file__).read_bytes()),'packagingFailures':[],'sourceFindings':[],'limitations':['No Unity Editor, importer, asset deserialization, subasset/fileID resolution, font rendering, package download, compilation, APK or device test.','Resources and GUID reachability is a conservative static inclusion graph, not proof of runtime execution. Dynamic resource expressions are listed, not guessed.','Git blob identities and inventory SHA256 prove archive fidelity, not source correctness.']}
def failure(kind,detail):report['packagingFailures'].append({'kind':kind,'detail':detail})
def finding(kind,detail):report['sourceFindings'].append({'kind':kind,'detail':detail})
manifest=json.loads(a.manifest.read_text());inventory=json.loads(a.inventory.read_text());commit=manifest['sourceCommit'];report['sourceCommit']=commit;report['sourceTree']=git('rev-parse',commit+'^{tree}').decode().strip();report['archiveSha256']=sha(a.zip.read_bytes())
if manifest.get('sha256')!=report['archiveSha256']:failure('archive_sha256','manifest mismatch')
if manifest.get('bytes')!=a.zip.stat().st_size:failure('archive_size','manifest mismatch')
tracked={}
for row in git('ls-tree','-rz',commit).split(b'\0'):
 if row:
  info,name=row.split(b'\t',1);mode,typ,oid=info.decode().split();tracked[name.decode()]={'mode':mode,'type':typ,'oid':oid}
report['trackedCount']=len(tracked)
if set(tracked)!=set(inventory):failure('inventory_paths',{'missing':sorted(set(tracked)-set(inventory)),'extra':sorted(set(inventory)-set(tracked))})
if a.extract_to:
 a.extract_to.mkdir(parents=True,exist_ok=False)
extraction=contextlib.nullcontext(str(a.extract_to)) if a.extract_to else tempfile.TemporaryDirectory(prefix='emberfall-zip-cleanroom-')
with extraction as tmp:
 root=Path(tmp);data={};fold={};members=[]
 with zipfile.ZipFile(a.zip) as z:
  infos=z.infolist();report['zipEntries']=len(infos)
  if manifest.get('entries')!=len(infos):failure('entry_count','manifest mismatch')
  roots={PurePosixPath(i.filename).parts[0] for i in infos if PurePosixPath(i.filename).parts}
  if len(roots)!=1:raise ValueError('Archive must have one project root')
  prefix=next(iter(roots))+'/'
  if sum(i.file_size for i in infos)>1024**3:raise ValueError('expanded archive exceeds 1GiB audit bound')
  for i in infos:
   name=i.filename;pp=PurePosixPath(name);mode=i.external_attr>>16
   if '\\' in name or ':' in name or pp.is_absolute() or '..' in pp.parts or '\x00' in name or name!=pp.as_posix()+('/' if i.is_dir() else ''):raise ValueError('Unsafe ZIP path: '+name)
   if stat.S_ISLNK(mode) or (stat.S_IFMT(mode) not in (0,stat.S_IFREG,stat.S_IFDIR)):raise ValueError('Nonregular ZIP member: '+name)
   if name in members:raise ValueError('Duplicate ZIP member: '+name)
   members.append(name)
   if name.casefold() in fold and fold[name.casefold()]!=name:failure('casefold_member_collision',[fold[name.casefold()],name])
   fold[name.casefold()]=name
   if i.is_dir():continue
   rel=name[len(prefix):];content=z.read(i);dest=root/rel;dest.parent.mkdir(parents=True,exist_ok=True);dest.write_bytes(content);data[rel]=content
  bad=z.testzip()
  if bad:failure('zip_crc',bad)
 extras=set(data)-set(tracked)
 if extras!={'SOURCE-DELIVERY.json'}:failure('archive_paths',{'missing':sorted(set(tracked)-set(data)),'extras':sorted(extras)})
 delivery=json.loads(data.get('SOURCE-DELIVERY.json',b'{}'))
 if delivery.get('sourceCommit')!=commit or delivery.get('trackedFiles')!=len(tracked):failure('embedded_delivery_identity','sourceCommit/trackedFiles mismatch')
 verified=0
 report["gitNormalizationExceptions"]=[]
 for name,blob in tracked.items():
  if name not in data:continue
  b=(root/name).read_bytes();inv=inventory.get(name,{})
  if inv.get('bytes')!=len(b) or inv.get('sha256')!=sha(b):failure('inventory_bytes',name)
  oid=hashlib.sha1(b'blob '+str(len(b)).encode()+b'\0'+b).hexdigest()
  if blob['type']!='blob' or blob['mode'] not in ('100644','100755'):failure('git_blob_type',name)
  elif oid!=blob['oid']:
   normalized=b.replace(b'\r\n',b'\n');normalized_oid=hashlib.sha1(b'blob '+str(len(normalized)).encode()+b'\0'+normalized).hexdigest()
   authorized=name.endswith('.ps1') and b'*.ps1 text eol=crlf' in data.get('.gitattributes',b'') and normalized_oid==blob['oid']
   if authorized:report['gitNormalizationExceptions'].append({'path':name,'kind':'CRLF checkout bytes match committed LF blob after declared eol conversion','archiveSha256':sha(b),'gitBlob':blob['oid']})
   else:failure('git_blob_bytes',name)
  verified+=1
 report['verifiedSourceFiles']=verified
 # Compare canonical paths, including implicit directories, on case-insensitive platforms.
 paths=set(data)
 for f in data:paths.update(str(x) for x in PurePosixPath(f).parents if str(x)!='.')
 casegroups=collections.defaultdict(list)
 for f in paths:casegroups[f.casefold()].append(f)
 report['casefoldCollisions']=[v for v in casegroups.values() if len(v)>1]
 for c in report['casefoldCollisions']:failure('casefold_path_collision',c)
 assets={f for f in data if f.startswith('Assets/')};folders={str(q) for f in assets for q in PurePosixPath(f).parents if str(q) not in ('.','Assets')}
 required={f for f in assets if not f.endswith('.meta')}|folders
 missing=sorted(f+'.meta' for f in required if f+'.meta' not in data);orphans=sorted(f for f in assets if f.endswith('.meta') and f[:-5] not in required)
 report['metadata']={'assetFiles':len(assets),'folders':len(folders),'missingMeta':missing,'orphanMeta':orphans}
 for f in missing:finding('missing_meta',f)
 for f in orphans:finding('orphan_meta',f)
 guids=collections.defaultdict(list);badguid=[]
 for f in sorted(assets):
  if not f.endswith('.meta'):continue
  m=re.search(rb'^guid:\s*(\S+)',data[f],re.M)
  if not m:finding('missing_guid',f);continue
  g=m[1].decode();guids[g.lower()].append(f[:-5])
  if not re.fullmatch('[0-9a-fA-F]{32}',g):badguid.append({'meta':f,'guid':g,'length':len(g)})
 duplicates={g:v for g,v in guids.items() if len(v)>1};report['metadata'].update(invalidGuids=badguid,duplicateGuids=duplicates)
 for x in badguid:finding('invalid_guid',x)
 for g,v in duplicates.items():finding('duplicate_guid',{'guid':g,'assets':v})
 builtin={'0'*32:'null','0000000000000000e000000000000000':'Unity built-in extra','0000000000000000f000000000000000':'Unity built-in resources'}
 refs=[];edges=collections.defaultdict(set)
 yaml_files=[f for f,b in data.items() if f.startswith(('Assets/','ProjectSettings/','Packages/')) and (b.startswith(b'%YAML') or f.endswith('.meta'))]
 for f in yaml_files:
  for line_n,line in enumerate(data[f].decode('utf-8','replace').splitlines(),1):
   if f.endswith('.meta') and re.match(r'^guid:',line):continue
   for match in re.finditer(r'\bguid:\s*([0-9a-fA-F]+)',line):
    g=match[1].lower();kind='built-in' if g in builtin else 'resolved' if g in guids else 'unresolved'
    row={'source':f,'line':line_n,'guid':g,'classification':kind,'context':line.strip()}
    if kind=='resolved':row['targets']=guids[g];edges[f.removesuffix('.meta')].update(guids[g])
    if kind=='built-in':row['builtinKind']=builtin[g]
    refs.append(row)
 # Resources keys remove final asset extension, retain directories and dots in basename.
 resources=collections.defaultdict(list)
 for f in sorted(assets):
  if f.endswith('.meta') or '/Resources/' not in f:continue
  key=f.split('/Resources/',1)[1];key=key.rsplit('.',1)[0] if '.' in PurePosixPath(key).name else key;resources[key].append(f)
 loads=[]
 global_constants={}
 for filename,content in data.items():
  if filename.startswith('Assets/') and filename.endswith('.cs'):
   code=content.decode('utf-8','replace');cls=PurePosixPath(filename).stem
   if re.search(r'\bclass\s+'+re.escape(cls)+r'\b',code):
    for k,v in re.findall(r'const\s+string\s+(\w+)\s*=\s*"([^"\\]*)"',code):global_constants[cls+'.'+k]=v
 for f in sorted(assets):
  if not f.endswith('.cs'):continue
  text=data[f].decode('utf-8');text=re.sub(r'//[^\n]*|/\*.*?\*/|"(?:\\.|[^"\\])*"',lambda m:' '*(len(m[0])) if m[0].startswith(('/',)) else m[0],text,flags=re.S)
  constants=dict(re.findall(r'const\s+string\s+(\w+)\s*=\s*"([^"\\]*)"',text))
  for m in re.finditer(r'Resources\.(LoadAll|Load)(?:<[^>]+>)?\s*\(\s*([^)]*)\)',text):
   arg=m[2].strip();key=arg[1:-1] if re.fullmatch(r'"[^"\\]*"',arg) else constants.get(arg,global_constants.get(arg))
   row={'source':f,'line':text[:m.start()].count('\n')+1,'method':m[1],'expression':arg}
   if key is None:row['classification']='dynamic-unresolved'
   else:
    found=list(resources.get(key,[]))
    if m[1]=='LoadAll':found+= [path for k,values in resources.items() if k.startswith(key.rstrip('/')+'/') for path in values]
    row.update(key=key,targets=sorted(set(found)),classification='path-exists' if found else 'missing-static-path')
    if not found:finding('missing_static_resource',row)
   loads.append(row)
 report['resources']={'keys':dict(resources),'loads':loads,'duplicateKeys':{k:v for k,v in resources.items() if len(v)>1},'limits':'Only literal arguments and local const string values resolved; LoadAll FBX subassets/type compatibility require Unity import.'}
 for k,v in report['resources']['duplicateKeys'].items():finding('duplicate_resource_key',{'key':k,'paths':v})
 scene_text=data.get('ProjectSettings/EditorBuildSettings.asset',b'').decode();scenes=[]
 for m in re.finditer(r'- enabled:\s*(\d+)\s+path:\s*([^\n]+)\s+guid:\s*(\S+)',scene_text):
  enabled,path,g=m.groups();path=path.strip();ok=path in data and path in guids.get(g,[]);scenes.append({'enabled':enabled=='1','path':path,'guid':g,'pathAndGuidValid':ok})
  if not ok:finding('build_scene_missing_or_wrong_guid',scenes[-1])
 if not any(s['enabled'] for s in scenes):finding('no_enabled_build_scene',scenes)
 seeds={s['path'] for s in scenes if s['enabled']}|{f for v in resources.values() for f in v};reachable=set(seeds);pending=list(seeds)
 while pending:
  for target in edges[pending.pop()]:
   if target not in reachable:reachable.add(target);pending.append(target)
 for row in refs:row['reachableFromEnabledSceneOrResources']=row['source'].removesuffix('.meta') in reachable
 unresolved=[x for x in refs if x['classification']=='unresolved'];report['guidReferences']={'references':refs,'counts':dict(collections.Counter(x['classification'] for x in refs)),'unresolved':unresolved,'roots':sorted(seeds),'reachableAssets':sorted(reachable)}
 for x in unresolved:finding('unresolved_yaml_guid',x)
 report['buildScenes']=scenes
 report['projectVersion']=data['ProjectSettings/ProjectVersion.txt'].decode().strip();report['packageManifest']=json.loads(data['Packages/manifest.json']);report['packageLockPresent']='Packages/packages-lock.json' in data
 report['pins']={f:sha(data[f]) for f in ['ProjectSettings/ProjectVersion.txt','Packages/manifest.json','ProjectSettings/EditorBuildSettings.asset']}
 protected=json.loads(a.ledger.read_text());entries=protected['platforms'];android=[x for x in entries if 'android' in x['path'].lower()];assert len(android)==1
 protected_results=[]
 for f,h in android[0]['protectedSha256'].items():
  ok=f in data and sha((root/f).read_bytes())==h;protected_results.append({'path':f,'sha256':sha(data[f]) if f in data else None,'matchesLedger':ok})
  if not ok:failure('protected_file',f)
 report['protectedFiles']=protected_results
 fonts=[]
 for f,b in data.items():
  if f.startswith('Assets/') and f.lower().endswith(('.otf','.ttf')):fonts.append({'path':f,'bytes':len(b),'sha256':sha(b),'sfntHeader':b[:4].hex(),'validSfntMagic':b[:4] in (b'OTTO',b'\x00\x01\x00\x00',b'ttcf')})
 report['fonts']=fonts
 for item in badguid:item['references']=[x for x in refs if x['guid']==item['guid'].lower()];item['asset']=item['meta'][:-5];item['csharpSource']=item['asset'].endswith('.cs')
 report['cleanRoom']={'usedFreshTemporaryRoot':True,'extractedFiles':len(data),'extractionDeletedAfterAudit':a.extract_to is None,'retainedRoot':str(a.extract_to) if a.extract_to else None}
report['rawGitBlobBytesExact']=not report['gitNormalizationExceptions'] and not any(x['kind'].startswith('git_blob') for x in report['packagingFailures']);report['packagingPassed']=not report['packagingFailures'];report['staticSourceClean']=not report['sourceFindings'];report['completedUtc']=datetime.datetime.now(datetime.timezone.utc).isoformat();(a.output/'report.json').write_text(json.dumps(report,indent=2,ensure_ascii=False)+'\n');print(json.dumps({k:report[k] for k in ['sourceCommit','sourceTree','verifiedSourceFiles','packagingPassed','staticSourceClean','sourceFindings','completedUtc']},ensure_ascii=False,indent=2))
raise SystemExit(0 if report['packagingPassed'] else 1)
