import bpy,json,pathlib,sys
root=pathlib.Path(sys.argv[sys.argv.index('--')+1]); output=sys.argv[sys.argv.index('--')+2]; rows=[]
for f in sorted(root.rglob('*.blend')):
 bpy.ops.wm.open_mainfile(filepath=str(f),load_ui=False,use_scripts=False)
 refs=[]
 for kind in ('images','movieclips','sounds','libraries','fonts'):
  for x in getattr(bpy.data,kind):
   path=getattr(x,'filepath','')
   if path and path!='<builtin>':refs.append({'kind':kind,'path':path,'packed':bool(getattr(x,'packed_file',None)),'resolved':bpy.path.abspath(path)})
 rows.append({'blend':str(f.relative_to(root)),'references':refs})
pathlib.Path(output).write_text(json.dumps(rows,indent=2)+'\n')
