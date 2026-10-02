using System;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using Emberfall.EditorTools;
namespace UnityEngine {
 public class Material {} public class Renderer {}
 public enum WrapMode { Default,ClampForever }
 public enum RuntimeInitializeLoadType { BeforeSceneLoad }
 public class RuntimeInitializeOnLoadMethodAttribute:Attribute { public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType t){} }
}
namespace Emberfall { public static class BlenderPilotArt { public static bool Enabled; } }
namespace UnityEditor {
 public class AssetImporter {} public class AssetPostprocessor { public string assetPath;public AssetImporter assetImporter; }
 public enum ModelImporterNormals { Calculate,Import } public enum ModelImporterTangents { None,CalculateMikk }
 public enum ModelImporterAnimationType { Generic,Legacy } public enum ModelImporterMaterialImportMode { None,ImportStandard }
 public class ModelImporter:AssetImporter { public float globalScale=17;public bool useFileScale,addCollider=true,isReadable=true,optimizeGameObjects=true,importAnimation;public ModelImporterNormals importNormals;public ModelImporterTangents importTangents;public ModelImporterAnimationType animationType;public WrapMode animationWrapMode;public ModelImporterMaterialImportMode materialImportMode; }
 public enum TextureImporterType { Other,Default } public enum TextureImporterAlphaSource { None,FromInput } public enum TextureImporterCompression { Uncompressed,Compressed }
 public class TextureImporter:AssetImporter { public TextureImporterType textureType;public bool sRGBTexture,alphaIsTransparency=true,mipmapEnabled,isReadable=true;public TextureImporterAlphaSource alphaSource;public int maxTextureSize=3;public TextureImporterCompression textureCompression; }
 public static class AssetDatabase { public static string Last;public static int Calls;public static Material Shared=new Material();public static T LoadAssetAtPath<T>(string p)where T:class {Last=p;Calls++;return p=="Assets/Resources/BlenderPilot/Pilot_Atlas_Standard.mat"?Shared as T:null;} }
 public class InitializeOnLoadAttribute:Attribute {} public class MenuItem:Attribute {public MenuItem(string s){} }
 public static class SessionState {public static bool Value;public static bool GetBool(string k,bool fallback)=>Value;public static void SetBool(string k,bool v){Value=v;} }
}
class Program {
 static int checks;static void C(bool b,string m){checks++;if(!b)throw new Exception(m);}
 static object Invoke(BlenderPilotImport p,string method,params object[] args)=>typeof(BlenderPilotImport).GetMethod(method,BindingFlags.NonPublic|BindingFlags.Instance).Invoke(p,args);
 static BlenderPilotImport P(string path,AssetImporter i)=>new BlenderPilotImport{assetPath=path,assetImporter=i};
 static string Snapshot(object o){string s="";foreach(var f in o.GetType().GetFields())s+=f.Name+"="+f.GetValue(o)+";";return s;}
 static void Main(){
  string root="Assets/Resources/BlenderPilot/";
  foreach(string path in new[]{"Assets/Resources/Other/Vanguard.fbx","Assets/Resources/BlenderPilotElse/Vanguard.fbx","Assets/Resources/BlenderPilotBackup/Vanguard.fbx","Assets/Resources/blenderpilot/Vanguard.fbx"}){
   var m=new ModelImporter();string before=Snapshot(m);Invoke(P(path,m),"OnPreprocessModel");C(before==Snapshot(m),"outside model path untouched");
   var t=new TextureImporter();before=Snapshot(t);Invoke(P(path,t),"OnPreprocessTexture");C(before==Snapshot(t),"outside texture path untouched");
   int loads=AssetDatabase.Calls;var original=new Material();C(Invoke(P(path,m),"OnAssignMaterialModel",original,new Renderer())==null&&AssetDatabase.Calls==loads,"outside material path untouched");
  }
  foreach(string name in new[]{"Vanguard.fbx","StarcoreSword.fbx","SupplyCrate.fbx","WayfarerTent.fbx","StarEmberCampfire.fbx"}){
   var m=new ModelImporter();Invoke(P(root+name,m),"OnPreprocessModel");
   C(m.globalScale==1&&m.useFileScale,"model scale policy");C(!m.addCollider&&!m.isReadable,"model no collider and nonreadable");
   C(m.importNormals==ModelImporterNormals.Import&&m.importTangents==ModelImporterTangents.CalculateMikk,"model normals tangents");
   C(!m.optimizeGameObjects,"sockets hierarchy retained");C(m.importAnimation==(name=="Vanguard.fbx"),"only hero imports animation");
   C(m.animationType==ModelImporterAnimationType.Legacy&&m.animationWrapMode==WrapMode.ClampForever,"legacy manual clip policy");C(m.materialImportMode==ModelImporterMaterialImportMode.ImportStandard,"standard model material policy");
   var existing=new Material();var result=Invoke(P(root+name,m),"OnAssignMaterialModel",existing,new Renderer());C(ReferenceEquals(result,AssetDatabase.Shared)&&AssetDatabase.Last==root+"Pilot_Atlas_Standard.mat","shared material exact path and identity");
  }
  foreach(string name in new[]{"EmberfallPilot_Atlas.png","EmberfallPilot_MetallicSmoothness.png"}){
   var t=new TextureImporter();Invoke(P(root+name,t),"OnPreprocessTexture");C(t.sRGBTexture==(name=="EmberfallPilot_Atlas.png"),"atlas sRGB and metallic linear");
   C(t.textureType==TextureImporterType.Default&&t.alphaSource==TextureImporterAlphaSource.FromInput&&!t.alphaIsTransparency,"texture alpha policy");C(t.mipmapEnabled&&t.maxTextureSize==1024&&t.textureCompression==TextureImporterCompression.Compressed&&!t.isReadable,"bounded compressed texture policy");
  }
  var saved=AssetDatabase.Shared;AssetDatabase.Shared=null;C(Invoke(P(root+"Vanguard.fbx",new ModelImporter()),"OnAssignMaterialModel",new Material(),new Renderer())==null,"missing shared material remains absent for runtime readiness fallback");AssetDatabase.Shared=saved;
  Console.WriteLine("PASS: "+checks+" real importer callback assertions; UnityEditor/asset database are managed doubles, no actual FBX/texture import");
 }
}
