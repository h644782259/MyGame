using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_ANDROID
using UnityEditor.Android;
#endif
namespace Emberfall.Editor
{
    // Dedicated Android clone. Standard Unity debug signing; no manual key, licence or store operations.
    public static class AndroidDevelopmentBuild
    {
        public const string RequiredVersion="6000.6.3f1";
        public const string ApplicationId="com.h644782259.emberfall.android.dev";
        public const int MinimumApi=26,TargetApi=36;
        const string MainScene="Assets/Scenes/Main.unity";

        [MenuItem("Emberfall/Android/Configure development APK")]
        public static void Configure()
        {
            RequireAndroidEditor();
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android,ApplicationId);
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android,ScriptingImplementation.IL2CPP);
            // Minimal preserves runtime reflection/serialization better than a first unverified aggressive strip.
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Android,ManagedStrippingLevel.Minimal);
            PlayerSettings.Android.targetArchitectures=AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion=(AndroidSdkVersions)MinimumApi;
            PlayerSettings.Android.targetSdkVersion=(AndroidSdkVersions)TargetApi;
            PlayerSettings.Android.useCustomKeystore=false;
            PlayerSettings.Android.forceInternetPermission=false;
            PlayerSettings.Android.forceSDCardPermission=false;
            PlayerSettings.Android.useAPKExpansionFiles=false;
            PlayerSettings.Android.buildApkPerCpuArchitecture=false;
            PlayerSettings.Android.renderOutsideSafeArea=true;
            PlayerSettings.defaultInterfaceOrientation=UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait=false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown=false;
            PlayerSettings.allowedAutorotateToLandscapeLeft=true;
            PlayerSettings.allowedAutorotateToLandscapeRight=true;
            PlayerSettings.runInBackground=false;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android,false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android,new[]{GraphicsDeviceType.OpenGLES3});
            PlayerSettings.openGLRequireES31=true;
            EditorUserBuildSettings.buildAppBundle=false;
            EditorUserBuildSettings.exportAsGoogleAndroidProject=false;
            // Existing Android Medium quality selection is retained; do not rewrite shared quality levels.
            AssetDatabase.SaveAssets();
        }
        static void RequireAndroidEditor()
        {
            if(Application.unityVersion!=RequiredVersion)throw new BuildFailedException("Use exact Unity "+RequiredVersion+"; got "+Application.unityVersion);
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new BuildFailedException("Stop Play Mode first.");
            if(!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android,BuildTarget.Android))throw new BuildFailedException("Install Android Build Support with SDK/NDK/OpenJDK through the official Unity Hub.");
            if(EditorUserBuildSettings.activeBuildTarget!=BuildTarget.Android)throw new BuildFailedException("Launch with -buildTarget Android (or switch target in Build Profiles first).");
        }
        [MenuItem("Emberfall/Android/Build development APK")]
        public static void BuildDebugApk()
        {
            RequireAndroidEditor();
#if UNITY_ANDROID
            string module=Path.Combine(EditorApplication.applicationContentsPath,"PlaybackEngines","AndroidPlayer");
            RequireBundled(AndroidExternalToolsSettings.sdkRootPath,Path.Combine(module,"SDK"),"SDK");
            RequireBundled(AndroidExternalToolsSettings.ndkRootPath,Path.Combine(module,"NDK"),"NDK");
            RequireBundled(AndroidExternalToolsSettings.jdkRootPath,Path.Combine(module,"OpenJDK"),"JDK");
            RequireBundled(AndroidExternalToolsSettings.gradlePath,Path.Combine(module,"Tools","gradle"),"Gradle");
            RequireFile(Path.Combine(module,"SDK","platforms","android-36","android.jar"));
            RequireFile(Path.Combine(module,"SDK","licenses","android-sdk-license"));
            string output=OutputPath();
            if(File.Exists(output))throw new BuildFailedException("Refusing an existing APK path; use a fresh output so stale binaries cannot pass: "+output);
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            Configure();
            BuildReport report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{
                scenes=new[]{MainScene},target=BuildTarget.Android,locationPathName=output,
                options=BuildOptions.Development|BuildOptions.CompressWithLz4
            });
            if(report==null||report.summary.result!=BuildResult.Succeeded||!File.Exists(output)||new FileInfo(output).Length==0)
                throw new BuildFailedException("Android APK build failed; inspect the Unity/Gradle log. No successful artifact claimed.");
            File.WriteAllText(output+".build.json",JsonUtility.ToJson(new Receipt{
                unityVersion=Application.unityVersion,applicationId=ApplicationId,minApi=MinimumApi,targetApi=TargetApi,
                apkPath=output,bytes=new FileInfo(output).Length,utc=DateTime.UtcNow.ToString("o"),
                result="Unity BuildPipeline succeeded; APK audit/install/device test still required"
            },true));
            Debug.Log("Development APK built (not installed or device-verified): "+output);
#else
            throw new BuildFailedException("Android target must be active when this entrypoint is compiled.");
#endif
        }
        static void RequireBundled(string actual,string expected,string tool)
        {
            var comparison=Application.platform==RuntimePlatform.WindowsEditor?StringComparison.OrdinalIgnoreCase:StringComparison.Ordinal;
            if(string.IsNullOrWhiteSpace(actual)||!string.Equals(Path.GetFullPath(actual).TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar),Path.GetFullPath(expected).TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar),comparison))
                throw new BuildFailedException("Select Installed with Unity for "+tool+" in Preferences > External Tools. This build does not change your machine tool preferences.");
        }
        static void RequireFile(string path){if(!File.Exists(path)||new FileInfo(path).Length==0)throw new BuildFailedException("Missing required Android dependency or accepted SDK licence: "+path);}
        static string OutputPath()
        {
            string project=Path.GetFullPath(Path.Combine(Application.dataPath,".."));
            string value=null;string[] args=Environment.GetCommandLineArgs();
            for(int i=0;i+1<args.Length;i++)if(args[i]=="-emberfallAndroidOutput")value=args[i+1];
            string output=Path.GetFullPath(value??Path.Combine(project,"Builds","Android",DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")+"-"+Guid.NewGuid().ToString("N").Substring(0,8),"Emberfall-Android-dev.apk"));
            string root=Path.GetFullPath(Path.Combine(project,"Builds","Android"))+Path.DirectorySeparatorChar;
            if(!output.StartsWith(root,Application.platform==RuntimePlatform.WindowsEditor?StringComparison.OrdinalIgnoreCase:StringComparison.Ordinal)||Path.GetExtension(output).ToLowerInvariant()!=".apk")
                throw new BuildFailedException("Output must be an .apk inside this project's Builds/Android directory.");
            return output;
        }
        [Serializable]sealed class Receipt{public string unityVersion,applicationId,apkPath,utc,result;public int minApi,targetApi;public long bytes;}
    }
}
