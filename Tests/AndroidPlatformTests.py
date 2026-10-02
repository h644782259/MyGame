"""Host-only policy/parser/negative preflight tests; never a Unity/APK/device validation."""
import importlib.util
import json
from pathlib import Path
import sys
import tempfile
import unittest
from unittest.mock import patch
import zipfile
ROOT=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(ROOT/'Tools/Android'))
import preflight
import build_debug

class AndroidPlatformTests(unittest.TestCase):
    def test_missing_editor_blocks_without_generating_signing(self):
        with tempfile.TemporaryDirectory() as home:
            report=preflight.probe('/nonexistent/Unity',home)
            self.assertEqual(report['status'],'BLOCKED')
            self.assertFalse(report['apk_produced'])
            self.assertFalse((Path(home)/'.android').exists())
            self.assertNotIn(home,json.dumps(report))
    def test_policy_matches_serialized_android(self):
        p=preflight.POLICY;s=(ROOT/'ProjectSettings/ProjectSettings.asset').read_text()
        for entry in [f'Android: {p["application_id"]}',f'AndroidMinSdkVersion: {p["min_api"]}',
            f'AndroidTargetSdkVersion: {p["target_api"]}','AndroidTargetArchitectures: 2',
            'scriptingBackend:\n    Standalone: 0\n    Android: 1','managedStrippingLevel:\n    Android: 4',
            'allowedAutorotateToPortrait: 0','allowedAutorotateToPortraitUpsideDown: 0',
            'androidUseCustomKeystore: 0','ForceInternetPermission: 0','ForceSDCardPermission: 0']:
            self.assertIn(entry,s)
    def test_badging_is_actual_evidence_not_assumed_permission_set(self):
        data=build_debug.inspect_badging("package: name='com.example' versionCode='1'\nsdkVersion:'26'\ntargetSdkVersion:'36'\napplication-debuggable\nuses-permission: name='android.permission.INTERNET'\nuses-permission-sdk-23: name='android.permission.CAMERA'\n")
        self.assertEqual(data['application_id'],'com.example')
        self.assertEqual(data['permissions'],['android.permission.CAMERA','android.permission.INTERNET'])
        self.assertTrue(data['debuggable'])
        self.assertIsNone(build_debug.inspect_badging('')['min_api'])
    def test_abi_parser_detects_extra_abi(self):
        with tempfile.TemporaryDirectory() as directory:
            apk=Path(directory)/'synthetic.zip'
            with zipfile.ZipFile(apk,'w') as archive:
                for path in ['lib/arm64-v8a/libil2cpp.so','lib/arm64-v8a/libunity.so','lib/armeabi-v7a/libunity.so']:archive.writestr(path,b'test-fixture-not-a-binary')
            result=build_debug.inspect_zip(apk)
            self.assertEqual(result['abis'],['arm64-v8a','armeabi-v7a'])
    def test_android_bootstrap_keeps_quality_but_initializes_assets(self):
        # Static branch contract only: Unity import/Play execution remains unverified.
        source=(ROOT/'Assets/Editor/ProjectTools.cs').read_text()
        ensure=source.split('public static void EnsureSettings()',1)[1].split('private static void IncludeRuntimeShaders()',1)[0]
        guard='if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)'
        before, guarded=ensure.split(guard,1)
        desktop, after=guarded.split('}',1)
        self.assertNotIn('QualitySettings.',before)
        self.assertNotIn('QualitySettings.',after)
        self.assertEqual(desktop.count('QualitySettings.'),5)
        self.assertIn('QualitySettings.antiAliasing = 4;',desktop)
        self.assertIn('QualitySettings.shadowDistance = 65;',desktop)
        self.assertIn('IncludeRuntimeShaders();',after)
        self.assertIn('AppIconSetup.Apply();',after)
        self.assertIn('EditorBuildSettings.scenes =',before)
        builder=(ROOT/'Assets/Editor/AndroidDevelopmentBuild.cs').read_text()
        self.assertNotIn('QualitySettings.',builder)
        self.assertIn('PlayerSettings.colorSpace = ColorSpace.Linear;',before)
        self.assertIn('NamedBuildTarget.Android,ScriptingImplementation.IL2CPP',builder)

    def test_module_layout(self):
        self.assertEqual(str(preflight.module_root(Path('/opt/unity/Editor/Unity'))),'/opt/unity/Editor/Data/PlaybackEngines/AndroidPlayer')
        self.assertEqual(str(preflight.module_root(Path('/Unity.app/Contents/MacOS/Unity'))),'/Unity.app/Contents/PlaybackEngines/AndroidPlayer')
if __name__=='__main__':unittest.main()
