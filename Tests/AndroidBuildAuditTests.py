"""Synthetic host-only negative tests; no Unity, Gradle, signing or real APK is executed."""
import hashlib
import json
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest
from unittest.mock import patch
sys.path.insert(0,str(Path(__file__).resolve().parents[1]/'Tools/Android'))
import build_debug as build


def fixture_receipt(apk):
    apk.write_bytes(b'synthetic parser fixture, not an APK')
    data=dict(schemaVersion=1,unityVersion=build.POLICY['unity_version'],
        applicationId=build.POLICY['application_id'],minApi=26,targetApi=36,
        bytes=apk.stat().st_size,result=build.RECEIPT_RESULT,apkPath=str(apk.resolve()),
        sha256=hashlib.sha256(apk.read_bytes()).hexdigest())
    Path(str(apk)+'.build.json').write_text(json.dumps(data))
    return data


class AndroidBuildAuditTests(unittest.TestCase):
    def test_normal_output_under_real_project(self):
        with tempfile.TemporaryDirectory() as directory:
            project=Path(directory).resolve()
            build.require_output_location(project,project/'Builds/Android/fresh')
            self.assertFalse((project/'Builds').exists())

    def test_reject_output_escape_and_linked_parent(self):
        with tempfile.TemporaryDirectory() as directory:
            project=Path(directory).resolve()/'project';project.mkdir()
            outside=Path(directory)/'outside';outside.mkdir()
            for output in [outside/'fresh',project/'Builds/Android/../../outside']:
                with self.assertRaises(ValueError):build.require_output_location(project,output)
            (project/'Builds').symlink_to(outside,target_is_directory=True)
            with self.assertRaises(ValueError):build.require_output_location(project,project/'Builds/Android/fresh')
            self.assertEqual(list(outside.iterdir()),[])

    def test_linked_receipt_is_rejected_without_reading_target(self):
        with tempfile.TemporaryDirectory() as directory:
            apk=Path(directory)/'fixture.apk';fixture_receipt(apk)
            receipt=Path(str(apk)+'.build.json');receipt.unlink()
            target=Path(directory)/'unrelated';target.write_text('not a receipt')
            receipt.symlink_to(target)
            with patch.object(Path,'read_text',side_effect=AssertionError('must not read link target')):
                with self.assertRaisesRegex(ValueError,'Linked'):build.validate_receipt(apk)

    def test_project_root_may_be_legitimate_link(self):
        with tempfile.TemporaryDirectory() as directory:
            actual=Path(directory)/'actual';actual.mkdir()
            alias=Path(directory)/'alias';alias.symlink_to(actual,target_is_directory=True)
            # ROOT is resolved when the module loads; project ancestors are not rejected.
            build.require_output_location(alias,alias.resolve()/'Builds/Android/fresh')

    def test_matching_receipt_is_not_apk_audit(self):
        with tempfile.TemporaryDirectory() as directory:
            apk=Path(directory)/'fixture.apk';fixture_receipt(apk)
            receipt=build.validate_receipt(apk)
            self.assertEqual(receipt['bytes'],apk.stat().st_size)
            self.assertNotIn('configuration_verified',receipt)

    def test_empty_malformed_and_nonobject_receipts_rejected(self):
        with tempfile.TemporaryDirectory() as directory:
            apk=Path(directory)/'fixture.apk';fixture_receipt(apk)
            for invalid in ['', '{broken', '[]', 'null', '{}']:
                Path(str(apk)+'.build.json').write_text(invalid)
                with self.subTest(invalid=invalid),self.assertRaises(ValueError):build.validate_receipt(apk)

    def test_wrong_receipt_identity_path_and_bytes_rejected(self):
        with tempfile.TemporaryDirectory() as directory:
            apk=Path(directory)/'fixture.apk';valid=fixture_receipt(apk)
            for key,value in [('schemaVersion',True),('unityVersion','6000.0.1f1'),
                ('applicationId','unrelated'),('minApi',25),('targetApi',35),
                ('apkPath',str(apk.parent/'other.apk')),('bytes',1),('result','Succeeded'),('sha256','0'*64)]:
                receipt=dict(valid);receipt[key]=value
                Path(str(apk)+'.build.json').write_text(json.dumps(receipt))
                with self.subTest(field=key),self.assertRaises(ValueError):build.validate_receipt(apk)

    def test_stale_receipt_rejects_same_size_apk_replacement(self):
        with tempfile.TemporaryDirectory() as directory:
            apk=Path(directory)/'fixture.apk';fixture_receipt(apk)
            apk.write_bytes(b'X'*apk.stat().st_size)
            with self.assertRaisesRegex(ValueError,'digest'):build.validate_receipt(apk)

    def test_audit_failure_persists_failed_stage(self):
        with tempfile.TemporaryDirectory() as directory:
            project=Path(directory).resolve()
            def fake_unity(command):
                # Mock process produces only synthetic test bytes; no subprocess is launched.
                apk=Path(command[command.index('-emberfallAndroidOutput')+1]);fixture_receipt(apk)
                return subprocess.CompletedProcess(command,0)
            with patch.object(build,'ROOT',project),patch.object(sys,'argv',['build_debug.py','--unity','/synthetic/Unity']),\
                patch.object(build,'probe',return_value={'status':'TOOLS_PRESENT_UNVERIFIED'}),\
                patch.object(build.subprocess,'run',side_effect=fake_unity),\
                patch.object(build,'audit',side_effect=subprocess.TimeoutExpired('synthetic-aapt2',1)),patch('builtins.print'):
                self.assertEqual(build.main(),2)
            statuses=list(project.glob('Builds/Android/*/build-status.json'))
            self.assertEqual(len(statuses),1)
            status=json.loads(statuses[0].read_text())
            self.assertEqual(status['status'],'AUDIT_FAILED')
            self.assertEqual(status['error'],'TimeoutExpired')
            self.assertFalse(status['device_verified'])

if __name__=='__main__':unittest.main()
