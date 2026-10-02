#!/usr/bin/env python3
"""Build only a fresh development APK after read-only preflight, then audit actual output."""
import argparse
from datetime import datetime, timezone
import json
import os
from pathlib import Path
import re
import shutil
import subprocess
import uuid
import zipfile
from preflight import ROOT, POLICY, probe, module_root


def inspect_badging(text):
    def field(pattern):
        match=re.search(pattern,text)
        return match.group(1) if match else None
    permissions=sorted(set(re.findall(r"uses-permission(?:-sdk-\d+)?: name='([^']+)'",text)))
    return dict(application_id=field(r"package: name='([^']+)'"),
        min_api=field(r"sdkVersion:'(\d+)'"),target_api=field(r"targetSdkVersion:'(\d+)'"),
        debuggable='application-debuggable' in text, permissions=permissions)


def inspect_zip(apk):
    with zipfile.ZipFile(apk) as archive:
        names=archive.namelist()
    abis=sorted(set(n.split('/')[1] for n in names if n.startswith('lib/') and n.count('/')>=2))
    return dict(abis=abis,il2cpp='lib/arm64-v8a/libil2cpp.so' in names,
        unity='lib/arm64-v8a/libunity.so' in names)


def audit(apk,module):
    suffix='.exe' if os.name=='nt' else ''
    buildtools=module/'SDK/build-tools/36.0.0'
    badging=subprocess.run([str(buildtools/('aapt2'+suffix)),'dump','badging',str(apk)],capture_output=True,text=True,timeout=60,check=True)
    result=inspect_badging(badging.stdout); result.update(inspect_zip(apk))
    verify=subprocess.run([str(module/('OpenJDK/bin/java'+suffix)),'-jar',str(buildtools/'lib/apksigner.jar'),'verify','--verbose',str(apk)],capture_output=True,text=True,timeout=60)
    result['signature_verified']=verify.returncode==0
    result['configuration_verified']=(result['application_id']==POLICY['application_id'] and
        result['min_api']==str(POLICY['min_api']) and result['target_api']==str(POLICY['target_api']) and
        result['debuggable'] and result['abis']==['arm64-v8a'] and result['il2cpp'] and result['unity'] and result['signature_verified'])
    # This offline game has no permission allowlist. Even normal permissions need explicit review.
    result['permissions_review_required']=bool(result['permissions'])
    result['device_verified']=False
    return result


def main():
    parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('--unity');args=parser.parse_args()
    report=probe(args.unity)
    if report['status']!='TOOLS_PRESENT_UNVERIFIED':
        print(json.dumps(report,indent=2));return 2
    exe=Path(args.unity or os.environ.get('UNITY_EDITOR') or shutil.which('Unity') or shutil.which('unity-editor')).expanduser().resolve()
    output=ROOT/'Builds/Android'/(datetime.now(timezone.utc).strftime('%Y%m%d-%H%M%S')+'-'+uuid.uuid4().hex[:8])
    output.mkdir(parents=True,exist_ok=False)
    (output/'preflight.json').write_text(json.dumps(report,indent=2)+'\n')
    apk=output/'Emberfall-Android-dev.apk'
    command=[str(exe),'-batchmode','-quit','-nographics','-buildTarget','Android','-projectPath',str(ROOT),
        '-executeMethod','Emberfall.Editor.AndroidDevelopmentBuild.BuildDebugApk',
        '-emberfallAndroidOutput',str(apk),'-logFile',str(output/'unity-build.log')]
    # No shell, package installation, licence acceptance, key generation, or upload commands.
    result=subprocess.run(command)
    if result.returncode!=0 or not apk.is_file() or not Path(str(apk)+'.build.json').is_file():
        print('BLOCKED: Unity did not return a confirmed APK and receipt. Inspect '+str(output/'unity-build.log'));return 2
    try:
        report=audit(apk,module_root(exe))
    except (OSError,subprocess.SubprocessError,zipfile.BadZipFile) as error:
        print('APK exists but audit failed: '+type(error).__name__);return 2
    (output/'apk-audit.json').write_text(json.dumps(report,indent=2)+'\n')
    print(json.dumps(dict(apk=str(apk),audit=report),indent=2))
    return 0 if report['configuration_verified'] and not report['permissions_review_required'] else 2
if __name__=='__main__':
    raise SystemExit(main())
