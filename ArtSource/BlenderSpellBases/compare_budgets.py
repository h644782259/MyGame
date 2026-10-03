"""Compare actual procedural recipe outputs to authored data; no Unity claimed."""
from pathlib import Path
import tempfile,subprocess,sys,json,struct,os
root=Path(__file__).resolve().parents[2]
with tempfile.TemporaryDirectory(prefix='spell-budget-') as d:
 p=Path(d);(p/'Recipes.cs').write_text((root/'Assets/Scripts/Core/FilledVfxRecipes.cs').read_text())
 (p/'Program.cs').write_text('''using System;using System.Linq;using System.Text.Json;using Emberfall;class Program{static void Main(){foreach(var name in new[]{"Crescent","Crystal","Flame","Sword","Lightning","ArcaneShard","Rupture","Arrow","Vine"}){var method=typeof(FilledVfxRecipes).GetMethod(name);var pars=method.GetParameters().Select(p=>p.DefaultValue).ToArray();var m=(FilledMeshRecipe)method.Invoke(null,pars);Console.WriteLine(JsonSerializer.Serialize(new {name,vertices=m.Positions.Length/3,triangles=m.Triangles.Length/3,min=Enumerable.Range(0,3).Select(a=>Enumerable.Range(0,m.Positions.Length/3).Min(i=>m.Positions[i*3+a])).ToArray(),max=Enumerable.Range(0,3).Select(a=>Enumerable.Range(0,m.Positions.Length/3).Max(i=>m.Positions[i*3+a])).ToArray()}));}}}''')
 (p/'Test.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType></PropertyGroup></Project>');(p/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>')
 r=subprocess.run([sys.argv[1] if len(sys.argv)>1 else 'dotnet','run','--project',str(p/'Test.csproj')],capture_output=True,text=True,check=True,env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1'))
 records=[]
 for line in r.stdout.splitlines():
  if not line.startswith('{'):continue
  old=json.loads(line);b=(root/('Assets/Resources/BlenderSpellBases/'+old['name']+'.bytes')).read_bytes();_,n,k=struct.unpack_from('<Iii',b);v=[struct.unpack_from('<3f',b,12+i*32) for i in range(n)];new={'vertices':n,'triangles':k//3,'min':[min(x[a] for x in v) for a in range(3)],'max':[max(x[a] for x in v) for a in range(3)]};records.append({'name':old.pop('name'),'procedural':old,'authored':new})
 print(json.dumps({'evidence':'actual C# FilledVfxRecipes outputs vs committed authored bytes; not Unity','meshes':records},indent=2))
