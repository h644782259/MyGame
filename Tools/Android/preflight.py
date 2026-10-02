#!/usr/bin/env python3
"""Read-only checks. Never installs tools, accepts licences, or reads signing contents."""
import argparse
import json
import os
from pathlib import Path
import platform
import re
import shutil
import subprocess
from datetime import datetime, timezone

ROOT = Path(__file__).resolve().parents[2]
POLICY = json.loads((Path(__file__).parent / 'policy.json').read_text())

def module_root(editor):
    if editor.parent.name == 'MacOS':
        return editor.parent.parent / 'PlaybackEngines/AndroidPlayer'
    return editor.parent / 'Data/PlaybackEngines/AndroidPlayer'

def probe(editor=None, home=None):
    checks = []
    def check(name, passed, detail):
        checks.append(dict(name=name, passed=bool(passed), detail=detail))
    supplied = editor or os.environ.get('UNITY_EDITOR') or shutil.which('Unity') or shutil.which('unity-editor')
    exe = Path(supplied).expanduser().resolve() if supplied else None
    check('unity_editor', exe is not None and exe.is_file(), 'Exact Unity 6000.6.3f1 executable required')
    if exe and exe.is_file():
        try:
            version = subprocess.run([str(exe), '-version'], capture_output=True, text=True, timeout=30)
            check('unity_version', version.returncode == 0 and re.search(r'(?<!\d)6000\.6\.3f1(?!\w)', version.stdout + version.stderr), 'Exact version probe; licence/build status remains unverified')
        except (OSError, subprocess.TimeoutExpired):
            check('unity_version', False, 'Version probe failed or timed out')
    module = module_root(exe) if exe else None
    def exists(relative):
        return module is not None and (module / relative).is_file() and (module / relative).stat().st_size > 0
    def read(relative):
        return (module / relative).read_text(errors='replace') if exists(relative) else ''
    suffix = '.exe' if platform.system() == 'Windows' else ''
    for name, relative in [
        ('android_module', 'UnityEditor.Android.Extensions.dll'),
        ('sdk_platform_36', 'SDK/platforms/android-36/android.jar'),
        ('aapt2', 'SDK/build-tools/36.0.0/aapt2' + suffix),
        ('zipalign', 'SDK/build-tools/36.0.0/zipalign' + suffix),
        ('apksigner', 'SDK/build-tools/36.0.0/lib/apksigner.jar'),
        ('adb', 'SDK/platform-tools/adb' + suffix),
        ('bundled_java', 'OpenJDK/bin/java' + suffix),
        ('gradle_9_3_1', 'Tools/gradle/lib/gradle-launcher-9.3.1.jar'),
        ('sdk_licence_record', 'SDK/licenses/android-sdk-license')]:
        check(name, exists(relative), 'Bundled dependency or existing licence record required; never installed/accepted here')
    check('ndk_r27c', bool(re.search(r'Pkg.Revision\s*=\s*27\.2\.12479018\b', read('NDK/source.properties'))), 'Exact bundled NDK revision')
    check('jdk_17', bool(re.search(r'JAVA_VERSION="17(?:[.\"]|$)', read('OpenJDK/release'))), 'System Java is not a substitute for bundled JDK 17')
    redirects = ('ANDROID_USER_HOME', 'ANDROID_PREFS_ROOT', 'ANDROID_SDK_HOME', 'JAVA_TOOL_OPTIONS', '_JAVA_OPTIONS', 'JDK_JAVA_OPTIONS', 'GRADLE_OPTS')
    check('standard_signing_environment', not any(os.environ.get(name) for name in redirects), 'Signing/home/JVM overrides must be absent; values are never logged')
    # Do not return/log this path or read its contents. No key generation fallback.
    signing = (Path(home) if home else Path.home()) / '.android/debug.keystore'
    check('existing_debug_signing', signing.is_file() and signing.stat().st_size > 0, 'Existing standard local debug signing material required; contents never read; automatic generation forbidden')
    free = shutil.disk_usage(ROOT).free / 1024**3
    check('disk_reserve', free >= POLICY['minimum_free_gib'], f'{free:.1f} GiB free; 12 GiB reserve is a preflight floor, not a build size guarantee')
    return dict(utc=datetime.now(timezone.utc).isoformat(), host=platform.platform(),
        status='TOOLS_PRESENT_UNVERIFIED' if all(c['passed'] for c in checks) else 'BLOCKED',
        checks=checks, unity_licence='Not inspected or activated; valid user-provided Editor entitlement required',
        apk_produced=False, editor_compile_verified=False, device_verified=False)

def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--unity'); parser.add_argument('--report', type=Path)
    args=parser.parse_args(); result=probe(args.unity)
    value=json.dumps(result, indent=2)
    if args.report:
        args.report.parent.mkdir(parents=True, exist_ok=True); args.report.write_text(value+'\n')
    print(value)
    return 0 if result['status']=='TOOLS_PRESENT_UNVERIFIED' else 2
if __name__ == '__main__':
    raise SystemExit(main())
