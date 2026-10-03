"""Evidence only: actual old/new PlayerController guard/passive entries, real Filled Update MPB.
python3 export_comparison.py /path/to/dotnet
"""
from pathlib import Path
import os,sys,tempfile,subprocess,json,re
root=Path(__file__).resolve().parents[2];out=Path(__file__).resolve().parent
sdk=sys.argv[1]
prefix=(root/'Tests/DefenseIdentityProductionTests.py').read_text().split('\nwith tempfile.TemporaryDirectory')[0]
base='0daa0f2dec8b53f695888032e3972b24a8145773'
all_samples=[]
for version in ['Before','After']:
 code=prefix
 if version=='Before':code=code.replace("player=(root/'Assets/Scripts/Combat/PlayerController.cs').read_text()",f"player=subprocess.check_output(['git','show','{base}:Assets/Scripts/Combat/PlayerController.cs'],cwd=root,text=True)")
 ns={'__file__':str(root/'Tests/DefenseIdentityProductionTests.py')};exec(code,ns)
 stubs=ns['stubs'].replace('public float Opacity,TintAlpha;','public Color Tint;public float Opacity,TintAlpha;').replace('Opacity=block.Opacity;TintAlpha=block.TintAlpha;','Opacity=block.Opacity;TintAlpha=block.TintAlpha;Tint=block.Tint;').replace('public float Opacity,TintAlpha=1;','public Color Tint;public float Opacity,TintAlpha=1;').replace('TintAlpha=value.a;','TintAlpha=value.a;Tint=value;')
 # Keep the fixture helper's class palette equal to actual production ClassColors.
 colors=re.search(r'public static readonly Color\[\] ClassColors = \{(.*?)\};',(root/'Assets/Scripts/Core/GameTypes.cs').read_text(),re.S).group(1)
 fixture=ns['fixture'].replace('public static Color ClassColor(HeroClass h)=>new Color(1,.8f,.3f,1);','private static readonly Color[] colors={'+colors+'};public static Color ClassColor(HeroClass h)=>colors[(int)h];')
 test=(root/'Tests/DefenseIdentityProductionTests.cs').read_text().split(' static void Main')[0]
 test=test.replace('Time.deltaTime=.12f','Time.deltaTime=.75f').replace('opacity=o.GetComponent<MeshRenderer>().Opacity','opacity=o.GetComponent<MeshRenderer>().Opacity,color=new[]{o.GetComponent<MeshRenderer>().Tint.r,o.GetComponent<MeshRenderer>().Tint.g,o.GetComponent<MeshRenderer>().Tint.b,o.GetComponent<MeshRenderer>().Tint.a}')
 test=test.replace('kind="ProtectionCage"','kind="'+version+'",age=.75f')
 test+=''' static void Main(string[] args){Resources.Root=args[0];var h=New();h.HeroClass=HeroClass.Vanguard;h.Guard1(1,1,1,GameBalance.ClassColor(h.HeroClass),23);Sample("Vanguard guard rank1",Anchor());h=New();h.HeroClass=HeroClass.Vanguard;GameSession.Instance.Progression.Profile.skillRanks[8]=1;h.TriggerPassive();Sample("Vanguard passive true trigger rank1",Anchor());System.IO.File.WriteAllText(args[1],JsonSerializer.Serialize(samples));Console.WriteLine("PASS actual entry at age .75 with renderer property-block tint/opacity");}}'''
 with tempfile.TemporaryDirectory(prefix='f6-compare-') as d:
  p=Path(d);(p/'Stubs.cs').write_text(stubs);(p/'Player.cs').write_text(fixture);(p/'Test.cs').write_text(test)
  for f in ['Core/FilledVfxRecipes','Core/FilledVfxPlacement','Core/CombatVisualBudget','Combat/CombatVisualLease','Combat/AnchoredImpactMesh','Combat/FilledSkillVfx','Combat/AuthoredActorMeshes','Combat/AuthoredSpellBases','Combat/AdvancedSkillVfx']:(p/(Path(f).name+'.cs')).write_text((root/('Assets/Scripts/'+f+'.cs')).read_text())
  (p/'Test.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType></PropertyGroup></Project>');(p/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>')
  dest=p/'sample.json';subprocess.run([sdk,'run','--project',str(p/'Test.csproj'),'--',str(root/'Assets/Resources'),str(dest)],env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1'),check=True)
  samples=json.loads(dest.read_text());assert all(any(part['color'][3]>0 and part['opacity']>0 for part in s['objects']) for s in samples)
  all_samples.extend(samples)
(out/'Comparison-Samples.json').write_text(json.dumps(all_samples))
print('PASS before baseline actual PlayerController entries vs current actual entries; nonzero actual MPB color/opacity')
