from pathlib import Path
import subprocess,os,json,hashlib,sys
REPO=Path(sys.argv[1]).resolve();OUT=Path(sys.argv[2]).resolve();OUT.mkdir(parents=True,exist_ok=True);DOTNET=sys.argv[3];PIN='03422ab83d0ab6a8f84f2147a81d8aeb63a6cd5e';build=OUT/'build';build.mkdir(exist_ok=True)
def source(path):return subprocess.check_output(['git','-C',str(REPO),'show',PIN+':'+path],text=True)
def extract(s,signature):
 a=s.index(signature);i=s.index('{',a)+1;depth=1
 while depth:depth+=(s[i]=='{')-(s[i]=='}');i+=1
 return s[a:i]
paths=['Assets/Scripts/Core/FilledVfxRecipes.cs','Assets/Scripts/Core/CombatVisualBudget.cs','Assets/Scripts/Core/FilledVfxPlacement.cs','Assets/Scripts/Combat/FilledSkillVfx.cs','Assets/Scripts/Combat/CombatVisualLease.cs','Assets/Scripts/Combat/AnchoredImpactMesh.cs']
for path in paths:(build/Path(path).name).write_text(source(path))
f=source('Tests/FilledVfxAllocationTests.cs');f='using System;using System.Collections.Generic;using System.Linq;using System.Reflection;\n'+f[f.index('namespace Emberfall'):]
f=f.replace('public static class CombatFx{','public static partial class CombatFx{').replace('public bool HasStarted,ModeFinished,InputBlocked;','public bool HasStarted,ModeFinished,InputBlocked,IsDead;')
f=f.replace('public static float deltaTime,time;','public static float deltaTime,time,unscaledDeltaTime;')
f=f.replace('public sealed class LineRenderer:Renderer{','public sealed class LineRenderer:Renderer{public bool loop;public int numCornerVertices,numCapVertices;public void SetPositions(Vector3[] values){Array.Copy(values,Positions,values.Length);}')
f=f.replace('new Vector3[4]','new Vector3[64]')
f=f.replace('public int renderQueue;public Color color;','public int renderQueue;public bool enableInstancing;public Color color;')
f=f.replace('public float Opacity;public void SetPropertyBlock(MaterialPropertyBlock block){Opacity=block.Opacity;}','public float Opacity,Progress,Style;public Color Tint;public void SetPropertyBlock(MaterialPropertyBlock block){Opacity=block.Opacity;Progress=block.Progress;Style=block.Style;Tint=block.Tint;}')
f=f.replace('public float Opacity;public void SetColor(string name,Color value){}public void SetFloat(string name,float value){if(name=="_Opacity")Opacity=value;}','public float Opacity,Progress,Style;public Color Tint;public void SetColor(string name,Color value){Tint=value;}public void SetFloat(string name,float value){if(name=="_Opacity")Opacity=value;if(name=="_Progress")Progress=value;if(name=="_Style")Style=value;}')
f=f.replace('public static class CombatSight\n    {','public static class WorldTraversal{public static int Revision;}\n    public static class CombatSight\n    {public static void FillAreaBoundary(Vector3[] p,Vector3 c,float r){}')
f=f.replace('public sealed class Transform\n    {','public enum Space{Self}\n    public sealed class Transform\n    {public void Rotate(float x,float y,float z,Space space){localRotation*=Quaternion.Euler(x,y,z);}')
f=f.replace('public const float PI=(float)Math.PI,Rad2Deg=180/PI;','public const float PI=(float)Math.PI,Rad2Deg=180/PI;public static float Pow(float x,float y)=>(float)Math.Pow(x,y);')
(build/'Fixture.cs').write_text('using UnityEngine;\n'+f)
effects=source('Assets/Scripts/Combat/CombatEffects.cs');(build/'Rings.cs').write_text('using UnityEngine;namespace Emberfall {public static partial class CombatFx{'+extract(effects,'public static GameObject Ring(')+'}\n'+extract(effects,'internal sealed class FadingCombatEffect')+'}')
(build/'Export.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><NoWarn>0649</NoWarn></PropertyGroup></Project>');(build/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>')
(build/'Program.cs').write_text(Path(__file__).with_name('Program.cs').read_text())
subprocess.run([DOTNET,'run','--project',str(build/'Export.csproj')],env=dict(os.environ,DOTNET_CLI_HOME=str(OUT/'cli'),EMBERFALL_VFX_OUTPUT=str(OUT/'baseline-vfx-03422ab.json')),check=True)
paths+=['Assets/Scripts/Combat/CombatEffects.cs','Assets/Scripts/Combat/PlayerController.cs','Assets/Scripts/Combat/WeaponVisualLinks.cs','Assets/Scripts/Combat/CombatModel.cs','Assets/Scripts/Combat/CombatModel.WeaponRig.cs','Tests/FilledVfxAllocationTests.cs','Assets/Resources/FilledSpell.shader','Assets/Scripts/Core/GameTypes.cs']
(OUT/'source-manifest.json').write_text(json.dumps({'pin':PIN,'sources':{x:hashlib.sha256(source(x).encode()).hexdigest() for x in paths}},indent=2))
