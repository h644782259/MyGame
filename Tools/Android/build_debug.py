#!/usr/bin/env python3
"""Build only a fresh development APK after read-only preflight, then audit actual output."""
import argparse
from datetime import datetime, timezone
import json
import hashlib
import os
from pathlib import Path
import re
import shutil
import stat
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



RECEIPT_RESULT='Unity BuildPipeline succeeded; APK audit/install/device test still required'


def require_output_location(project, output):
    """Refuse redirected output trees before creating directories or invoking Unity."""
    project=project.resolve()
    try:
        relative=output.relative_to(project)
    except ValueError:
        raise ValueError('Output is outside project')
    if relative.parts[:2]!=('Builds','Android') or '..' in relative.parts:
        raise ValueError('Output must be under Builds/Android')
    cursor=project
    for part in relative.parts:
        cursor=cursor/part
        # lstat exposes Windows reparse points without following junctions.
        # Only descendants of the canonical project root are inspected.
        attributes=getattr(cursor.lstat(),'st_file_attributes',0) if cursor.exists() or cursor.is_symlink() else 0
        if cursor.is_symlink() or attributes & getattr(stat,'FILE_ATTRIBUTE_REPARSE_POINT',0):
            raise ValueError('Linked output path is not permitted')
        if not cursor.resolve().is_relative_to(project):
            raise ValueError('Resolved output is outside project')


def validate_receipt(apk):
    receipt_path=Path(str(apk)+'.build.json')
    if apk.is_symlink() or receipt_path.is_symlink():
        raise ValueError('Linked build artifact is not permitted')
    if not apk.is_file() or apk.stat().st_size<=0:
        raise ValueError('Missing or empty APK')
    if not receipt_path.is_file() or not 0<receipt_path.stat().st_size<=65536:
        raise ValueError('Missing, empty or oversized receipt')
    receipt=json.loads(receipt_path.read_text(encoding='utf-8'))
    if not isinstance(receipt,dict):
        raise ValueError('Receipt must be an object')
    expected={'schemaVersion':1,'unityVersion':POLICY['unity_version'],
        'applicationId':POLICY['application_id'],'minApi':POLICY['min_api'],
        'targetApi':POLICY['target_api'],'bytes':apk.stat().st_size,'result':RECEIPT_RESULT}
    if any(type(receipt.get(key)) is not type(value) or receipt[key]!=value for key,value in expected.items()):
        raise ValueError('Receipt configuration or size mismatch')
    if not isinstance(receipt.get('apkPath'),str) or Path(receipt['apkPath']).resolve()!=apk.resolve():
        raise ValueError('Receipt APK path mismatch')
    digest=hashlib.sha256()
    with apk.open('rb') as stream:
        for chunk in iter(lambda:stream.read(1024*1024),b''):digest.update(chunk)
    if receipt.get('sha256')!=digest.hexdigest():
        raise ValueError('Receipt APK digest mismatch')
    return receipt


def finish(output, status, error=None, audit_report=None):
    result=dict(status=status,utc=datetime.now(timezone.utc).isoformat(),device_verified=False)
    if error:result['error']=error
    if audit_report is not None:result['audit']=audit_report
    (output/'build-status.json').write_text(json.dumps(result,indent=2)+'\n')
    print(json.dumps(dict(output=str(output),**result),indent=2))
    return 0 if status=='PASSED_ARTIFACT_AUDIT' else 2


def main():
    parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('--unity');args=parser.parse_args()
    report=probe(args.unity)
    if report['status']!='TOOLS_PRESENT_UNVERIFIED':
        print(json.dumps(report,indent=2));return 2
    exe=Path(args.unity or os.environ.get('UNITY_EDITOR') or shutil.which('Unity') or shutil.which('unity-editor')).expanduser().resolve()
    output=ROOT/'Builds/Android'/(datetime.now(timezone.utc).strftime('%Y%m%d-%H%M%S')+'-'+uuid.uuid4().hex[:8])
    try:
        require_output_location(ROOT,output)
        output.mkdir(parents=True,exist_ok=False)
    except (OSError,ValueError) as error:
        print('BLOCKED: output creation rejected: '+type(error).__name__);return 2
    (output/'preflight.json').write_text(json.dumps(report,indent=2)+'\n')
    apk=output/'Emberfall-Android-dev.apk'
    command=[str(exe),'-batchmode','-quit','-nographics','-buildTarget','Android','-projectPath',str(ROOT),
        '-executeMethod','Emberfall.Editor.AndroidDevelopmentBuild.BuildDebugApk',
        '-emberfallAndroidOutput',str(apk),'-logFile',str(output/'unity-build.log')]
    # No shell, package installation, licence acceptance, key generation, or upload commands.
    try:
        result=subprocess.run(command)
    except OSError as error:
        return finish(output,'BUILD_PROCESS_FAILED',type(error).__name__)
    if result.returncode!=0:
        return finish(output,'BUILD_PROCESS_FAILED','Unity exit code '+str(result.returncode))
    try:
        validate_receipt(apk)
    except (OSError,ValueError) as error:
        return finish(output,'RECEIPT_REJECTED',type(error).__name__)
    try:
        report=audit(apk,module_root(exe))
    except (OSError,subprocess.SubprocessError,zipfile.BadZipFile) as error:
        return finish(output,'AUDIT_FAILED',type(error).__name__)
    (output/'apk-audit.json').write_text(json.dumps(report,indent=2)+'\n')
    status='PASSED_ARTIFACT_AUDIT' if report['configuration_verified'] and not report['permissions_review_required'] else 'AUDIT_REVIEW_REQUIRED'
    return finish(output,status,audit_report=report)
if __name__=='__main__':
    raise SystemExit(main())
