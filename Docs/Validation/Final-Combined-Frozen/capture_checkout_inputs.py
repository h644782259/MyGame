from pathlib import Path
import json,hashlib,subprocess,sys
root=Path(sys.argv[1]);canonical=json.loads(Path(sys.argv[2]).read_text())['winManifest'];out=Path(sys.argv[3]);rows={};normalized=[]
for path,expected in canonical.items():
 raw=(root/path).read_bytes();actual=hashlib.sha256(raw).hexdigest();rows[path]=actual
 if actual!=expected:
  attr=subprocess.check_output(['git','-C',str(root),'check-attr','eol','--',path]).decode().strip()
  assert attr.endswith(': crlf') and hashlib.sha256(raw.replace(b'\r\n',b'\n')).hexdigest()==expected,path
  normalized.append(path)
out.write_text(json.dumps({'scope':'Exact checkout bytes before the full run; canonical Git blobs are separately recorded. Only declared CRLF conversion may differ initially.','gitHead':subprocess.check_output(['git','-C',str(root),'rev-parse','HEAD']).decode().strip(),'expectedCRLFPaths':normalized,'inputSha256':rows},indent=2)+'\n')
print('Captured',len(rows),'actual checkout hashes;',len(normalized),'declared CRLF files')
