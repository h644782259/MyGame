#!/usr/bin/env python3
"""Compile real scenery resource adapter and real pilot atlas gate against managed Unity boundaries."""
from pathlib import Path
import os,subprocess,tempfile,sys
root=Path(__file__).resolve().parents[1];dotnet=sys.argv[1] if len(sys.argv)>1 else os.environ.get('DOTNET','dotnet')
fixture=(root/'Tests/BlenderPilotAdapterProductionFixture.cs').read_text().split('namespace Emberfall{')[0]
fixture=fixture.replace('if(Bound)LayerFixture.ApplyClip(g,name,t/length);','')
fixture=fixture.replace('var v=g.AddComponent<Renderer>();v.enabled=r.enabled;v.sharedMaterial=r.sharedMaterial;','var v=c is MeshRenderer?g.AddComponent<MeshRenderer>():g.AddComponent<Renderer>();v.enabled=r.enabled;v.sharedMaterial=r.sharedMaterial;v.sharedMaterials=(Material[])r.sharedMaterials.Clone();')
fixture=fixture.replace('public Material sharedMaterial;','public Material sharedMaterial;public Material[] sharedMaterials=new Material[0];')
fixture+='\nnamespace UnityEngine {public class Collider:Component{}public class MeshRenderer:Renderer{}}\nnamespace Emberfall {public enum VisualSurface{Stone,Wood,Foliage}public class WorldResources{public Material Material(Color c,bool e,VisualSurface s)=>new Material{name=s.ToString()};}}\n'
pilot=(root/'Assets/Scripts/Combat/BlenderPilotVisual.cs').read_text().split('    // Sole writer of the imported skeleton.')[0]+'}\n'
checks='''
class Test {
 static int count;static void C(bool b,string s){count++;if(!b)throw new Exception(s);}
 static GameObject Source(string slot="Clay"){var g=new GameObject("resource");g.AddComponent<MeshFilter>().sharedMesh=new Mesh();var r=g.AddComponent<MeshRenderer>();r.sharedMaterials=new[]{new Material{name=slot}};return g;}
 static void Main(){var owner=new GameObject("owner");var r=new WorldResources();
 C(BlenderSceneryArt.Create("missing",owner.transform,Vector3.zero,r)==null,"missing falls back");
 var source=Source();Resources.Items["BlenderScenery/pot"]=source;
 BlenderSceneryArt.Enabled=false;int loads=Resources.Loads;C(BlenderSceneryArt.Create("pot",owner.transform,Vector3.zero,r)==null&&Resources.Loads==loads,"opt out avoids load");BlenderSceneryArt.Enabled=true;
 var made=BlenderSceneryArt.Create("pot",owner.transform,new Vector3(1,2,3),r);C(made!=null&&made.transform.parent==owner.transform&&made.transform.localPosition==new Vector3(1,2,3),"preserves parent and authored placement");C(made.GetComponentsInChildren<MeshRenderer>(true)[0].sharedMaterials[0].name=="Stone","binds shared world surface");C(source.GetComponentsInChildren<MeshRenderer>(true)[0].sharedMaterials[0].name=="Clay","never mutates resource material slots");
 source.AddComponent<Collider>();C(BlenderSceneryArt.Create("pot",owner.transform,Vector3.zero,r)==null,"collider resource rejected");
 Resources.Items["BlenderScenery/pot"]=Source("unknown");int before=GameObject.All.Count;C(BlenderSceneryArt.Create("pot",owner.transform,Vector3.zero,r)==null,"unknown imported material rejected");C(GameObject.All.Skip(before).All(g=>!g.activeSelf&&g.destroyed),"rejected partial instance hidden and destroyed");
 foreach(string slot in new[]{"Clay","Ochre","Stone","Bark","Leaf"}){Resources.Items["BlenderScenery/pot"]=Source(slot);C(BlenderSceneryArt.Create("pot",owner.transform,Vector3.zero,r)!=null,"accept palette "+slot);}
 Resources.Items["BlenderPilot/SupplyCrate"]=Source();BlenderPilotArt.Enabled=false;C(BlenderSceneryArt.CreatePilotProp("SupplyCrate",owner.transform,Vector3.zero)==null,"missing atlas falls back");Resources.Items["BlenderPilot/Pilot_Atlas_Standard"]=new Material();C(BlenderSceneryArt.CreatePilotProp("SupplyCrate",owner.transform,Vector3.zero)!=null&&!BlenderPilotArt.Enabled,"scenery independent of hero pilot opt in");
 Console.WriteLine("PASS: "+count+" real scenery adapter checks; managed boundaries, no Unity import/render claim");
 }
}
'''
with tempfile.TemporaryDirectory(prefix='scenery-loader-') as tmp:
 p=Path(tmp);(p/'Fixture.cs').write_text(fixture+checks);(p/'Pilot.cs').write_text(pilot);(p/'Scenery.cs').write_text((root/'Assets/Scripts/World/BlenderSceneryArt.cs').read_text());(p/'Test.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType></PropertyGroup></Project>');(p/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>')
 env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1')
 subprocess.run([dotnet,'restore',str(p/'Test.csproj'),'--configfile',str(p/'NuGet.Config'),'-v:q'],check=True,env=env)
 subprocess.run([dotnet,'run','--project',str(p/'Test.csproj'),'--no-restore'],check=True,env=env)
