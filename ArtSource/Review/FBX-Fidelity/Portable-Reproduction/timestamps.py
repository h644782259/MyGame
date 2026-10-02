"""Read only FBXHeaderExtension/CreationTimeStamp; no animation/array parsing."""
import json,struct,sys
from pathlib import Path
def timestamp(path):
 data=Path(path).read_bytes();assert data[:23]==b'Kaydara FBX Binary  \x00\x1a\x00';version=struct.unpack_from('<I',data,23)[0];fmt='<QQQB' if version>=7500 else '<IIIB';size=struct.calcsize(fmt)
 def nodes(start,end):
  while start+size<=end:
   stop,count,length,n=struct.unpack_from(fmt,data,start)
   if stop==0:break
   name=data[start+size:start+size+n].decode();p=start+size+n;yield name,p,count,length,stop
   start=stop
 for name,p,count,length,end in nodes(27,len(data)):
  if name!='FBXHeaderExtension':continue
  for name,p,count,length,end in nodes(p+length,end):
   if name!='CreationTimeStamp':continue
   result={}
   for name,p,count,length,end in nodes(p+length,end):
    assert count==1 and data[p:p+1]==b'I';result[name]=struct.unpack_from('<i',data,p+1)[0]
   return result
 raise ValueError('Missing timestamp')
if __name__=='__main__':print(json.dumps({p:timestamp(p) for p in sys.argv[1:]},indent=2))
