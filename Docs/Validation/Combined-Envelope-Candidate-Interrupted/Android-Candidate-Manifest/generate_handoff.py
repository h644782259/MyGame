#!/usr/bin/env python3
"""Offline shared-source candidate generator. Reads Git objects; never contacts Android.
Existing output directories are refused to protect previous candidate artifacts.
"""
import argparse
import collections
import gzip
import hashlib
import io
import json
from pathlib import Path
import re
import subprocess
import tarfile

SCOPE = ['Assets/Scripts', 'Assets/Resources', 'ArtSource', 'Tests', 'Tools']
RUNTIME = ('Assets/Scripts/', 'Assets/Resources/')

def sha(data):
    return hashlib.sha256(data).hexdigest()

def file_sha(path):
    h = hashlib.sha256()
    with path.open('rb') as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b''):
            h.update(block)
    return h.hexdigest()

def git(repo, *args):
    # Only local rev-parse, ls-tree, cat-file and diff are used. No network/auth commands.
    return subprocess.check_output(['git', '-C', str(repo), *args])

def commit(repo, ref):
    value = git(repo, 'rev-parse', '--verify', '--end-of-options', ref + '^{commit}').decode().strip()
    if not re.fullmatch('[0-9a-f]{40,64}', value):
        raise ValueError('Not a resolved commit: ' + value)
    return value

def tree(repo, ref, *paths):
    result = {}
    for row in git(repo, 'ls-tree', '-r', '-l', '-z', ref, *paths).split(b'\0'):
        if not row:
            continue
        info, path = row.split(b'\t', 1)
        mode, kind, oid, size = info.decode().split()
        if kind != 'blob' or mode not in ('100644', '100755'):
            raise ValueError('Unsupported tracked entry (no symlink/submodule conversion): ' + path.decode())
        name = path.decode('utf-8')
        if name.startswith('/') or '..' in Path(name).parts:
            raise ValueError('Unsafe tracked relative path: ' + name)
        result[name] = {'gitBlob': oid, 'bytes': int(size), 'mode': mode}
    return result

def blob(repo, row):
    value = git(repo, 'cat-file', 'blob', row['gitBlob'])
    if len(value) != row['bytes']:
        raise ValueError('Git blob length mismatch: ' + row['gitBlob'])
    return value

def write_json(path, value):
    path.write_text(json.dumps(value, indent=2, ensure_ascii=False) + '\n', encoding='utf-8')

def guid_audit(repo, baseline, frozen, platform):
    before = tree(repo, baseline, 'Assets')
    after = tree(repo, frozen, 'Assets')
    def values(files):
        guids, invalid = {}, []
        for path, row in files.items():
            if path.endswith('.meta'):
                match = re.search(rb'^guid:\s*([a-fA-F0-9]{32})\s*$', blob(repo, row), re.M)
                if match:
                    guids[path] = match.group(1).decode().lower()
                else:
                    invalid.append(path)
        return guids, invalid
    old, old_invalid = values(before)
    new, new_invalid = values(after)
    inverted = collections.defaultdict(list)
    for path, guid in new.items():
        inverted[guid].append(path)
    cs = sorted(path for path in after if path.startswith('Assets/Scripts/') and path.endswith('.cs'))
    return {
        'platform': platform, 'baseline': baseline, 'frozen': frozen,
        'runtimeCsFiles': len(cs), 'originalGuidCount': len(old),
        'preservedGuidCount': sum(new.get(path) == guid for path, guid in old.items()),
        'changedGuids': [{'path': path, 'old': guid, 'new': new[path]} for path, guid in old.items() if path in new and new[path] != guid],
        'missingOriginalMetaPaths': [path for path in old if path not in new],
        'newGuidPaths': {path: guid for path, guid in new.items() if path not in old},
        'duplicateGuidPaths': {guid: paths for guid, paths in inverted.items() if len(paths) > 1},
        'runtimeCsMissingMetas': [path for path in cs if path + '.meta' not in after],
        'invalidBaselineMetaPaths': old_invalid, 'invalidCurrentMetaPaths': new_invalid,
        'originalGuids': old,
    }

def write_archive(path, repo, rows):
    with path.open('wb') as raw, gzip.GzipFile(fileobj=raw, mode='wb', filename='', mtime=0) as gz, tarfile.open(fileobj=gz, mode='w|', format=tarfile.PAX_FORMAT) as tar:
        for row in rows:
            if row['after'] is None:
                continue
            data = blob(repo, row['after'])
            item = tarfile.TarInfo(row['path'])
            item.size = len(data)
            item.mode = int(row['after']['mode'], 8) & 0o777
            item.uid = item.gid = item.mtime = 0
            item.uname = item.gname = ''
            tar.addfile(item, io.BytesIO(data))
    expected = {row['path']: row['after'] for row in rows if row['after'] is not None}
    with tarfile.open(path, 'r:gz') as tar:
        members = tar.getmembers()
        if len(members) != len(expected) or len({m.name for m in members}) != len(expected):
            raise ValueError('Archive member count or uniqueness failed')
        for item in members:
            record = expected.get(item.name)
            if not item.isfile() or record is None or item.mode != int(record['mode'], 8) & 0o777:
                raise ValueError('Archive entry type/path/mode mismatch: ' + item.name)
            data = tar.extractfile(item).read()
            if len(data) != record['bytes'] or sha(data) != record['sha256']:
                raise ValueError('Archive entry payload mismatch: ' + item.name)
    return len(expected), sum(record['bytes'] for record in expected.values())

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ['win-repo', 'ios-repo', 'win-base', 'win-head', 'ios-base', 'ios-head', 'output']:
        parser.add_argument('--' + name, required=True)
    parser.add_argument('--android-base', default='8654c803eb29b87dea7d69f09678ec21e1955f6d', help='Reported only; never accessed or validated')
    parser.add_argument('--check-inputs-only', action='store_true', help='Resolve local refs and inventory paths; write nothing and produce no archive')
    args = parser.parse_args()
    win, ios, output = Path(args.win_repo).resolve(), Path(args.ios_repo).resolve(), Path(args.output).resolve()
    refs = {'winBase': commit(win, args.win_base), 'winHead': commit(win, args.win_head), 'iosBase': commit(ios, args.ios_base), 'iosHead': commit(ios, args.ios_head)}
    old = tree(win, refs['winBase'], *SCOPE)
    new = tree(win, refs['winHead'], *SCOPE)
    paths = sorted(path for path in set(old) | set(new) if old.get(path) != new.get(path))
    if args.check_inputs_only:
        print(json.dumps({'resolvedRefs': refs, 'changedSharedPaths': len(paths), 'outputWillBeNewDirectory': not output.exists(), 'writes': 0, 'networkActions': 0}, indent=2))
        return
    if output.exists():
        raise FileExistsError('Refusing existing output directory; choose a new candidate directory: ' + str(output))
    # Validate audit BEFORE producing bundles. Never silently label a broken audit as preserved.
    audits = [guid_audit(win, refs['winBase'], refs['winHead'], 'Windows'), guid_audit(ios, refs['iosBase'], refs['iosHead'], 'iOS')]
    for audit in audits:
        for field in ['changedGuids', 'missingOriginalMetaPaths', 'duplicateGuidPaths', 'runtimeCsMissingMetas', 'invalidCurrentMetaPaths']:
            if audit[field]:
                raise ValueError('Audit failed: ' + audit['platform'] + '/' + field + ': ' + json.dumps(audit[field]))
    wc = tree(win, refs['winHead'], 'Assets/Scripts')
    ic = tree(ios, refs['iosHead'], 'Assets/Scripts')
    csdiff = sorted(path for path in set(wc) | set(ic) if path.endswith('.cs') and wc.get(path) != ic.get(path))
    new_guid_parity = audits[0]['newGuidPaths'] == audits[1]['newGuidPaths']
    if csdiff or not new_guid_parity:
        raise ValueError('Frozen source/GUID parity differs: ' + json.dumps({'cs': csdiff, 'newGuidParity': new_guid_parity}))
    changes = []
    for path in paths:
        before, after = old.get(path), new.get(path)
        row = {'path': path, 'status': 'A' if before is None else 'D' if after is None else 'M'}
        for label, record in [('before', before), ('after', after)]:
            row[label] = None if record is None else dict(record, sha256=sha(blob(win, record)))
        changes.append(row)
    output.mkdir(parents=True, exist_ok=False)
    full = output / 'shared-source-changes.tar.gz'
    small = output / 'runtime-shared.tar.gz'
    full_count, full_bytes = write_archive(full, win, changes)
    small_rows = [row for row in changes if row['path'].startswith(RUNTIME)]
    small_count, small_bytes = write_archive(small, win, small_rows)
    patch = output / 'windows-baseline-to-frozen-shared.binary.patch'
    with patch.open('wb') as stream:
        subprocess.run(['git', '-C', str(win), 'diff', '--no-ext-diff', '--no-textconv', '--binary', '--full-index', '--no-renames', refs['winBase'], refs['winHead'], '--', *SCOPE], stdout=stream, check=True)
    audit_path = output / 'frozen-guid-audit.json'
    write_json(audit_path, {'status': 'Frozen Git-ref audit; no working-tree reads; not Unity importer validation', 'audits': audits, 'runtimeCsParityDifferentPaths': csdiff, 'newGuidParityIdentical': new_guid_parity})
    resources = [row for row in changes if row['path'].startswith('Assets/Resources/') and not row['path'].endswith('.meta')]
    artifacts = {}
    for path, scope in [(full, SCOPE), (small, ['Assets/Scripts', 'Assets/Resources']), (patch, SCOPE), (audit_path, ['All tracked Assets meta GUIDs and runtime C# meta presence/parity'])]:
        artifacts[path.name] = {'bytes': path.stat().st_size, 'sha256': file_sha(path), 'scope': scope}
    artifacts[full.name].update(fileCount=full_count, uncompressedChangedSourceBytes=full_bytes, purpose='Changed shared authoring/source/tests/builders/evidence; NOT runtime player package size')
    artifacts[small.name].update(fileCount=small_count, uncompressedChangedSourceBytes=small_bytes, purpose='Changed runtime shared source including metas; NOT APK, independent Unity project, or complete authoring/test bundle')
    manifest = {
        'status': 'LOCAL CANDIDATE — unified full tests pending; not applied to Android',
        'windowsPatchBase': refs['winBase'], 'windowsFrozenTarget': refs['winHead'], 'iosOriginalBase': refs['iosBase'], 'iosFrozenAuditTarget': refs['iosHead'],
        'reportedAndroidBase': args.android_base, 'androidBaseInspected': False,
        'patchApplicabilityToAndroid': 'UNKNOWN; patch is based on Windows, not Android. No application or synchronization claimed.',
        'scope': SCOPE, 'excluded': 'ProjectSettings, Packages, native platform projects/settings, authentication/repository setup. No APK/Android branch/remote access/upload.',
        'source': 'All content from resolved immutable Git refs/blob IDs, not working trees.',
        'archiveSemantics': 'Changed target files only at repository-relative paths. Deletions recorded in manifest/patch. Unchanged Android platform files intended to remain intact but never inspected.',
        'changedPathCounts': dict(collections.Counter(row['status'] for row in changes)), 'archiveFileCount': full_count, 'uncompressedChangedSourceBytes': full_bytes,
        'changedFiles': changes, 'artifacts': artifacts,
        'runtimeResourceIncrement': {'addedPayloadFiles': sum(row['status'] == 'A' for row in resources), 'addedSourceBytes': sum(row['after']['bytes'] for row in resources if row['status'] == 'A'), 'netSourceByteDelta': sum((row['after']['bytes'] if row['after'] else 0) - (row['before']['bytes'] if row['before'] else 0) for row in resources), 'scope': 'Non-meta Assets/Resources payload. Not script/archive/package or resident memory size.'},
        'verification': 'Both archives read back member-by-member against target blob SHA-256/bytes/mode, with exact unique safe member paths. Full-index binary diff generated locally; no patch application attempted.',
        'generator': {'name': Path(__file__).name, 'sha256': file_sha(Path(__file__)), 'networkActions': 0},
    }
    write_json(output / 'manifest.json', manifest)
    (output / 'README.md').write_text(f'''# Local shared-source candidate — NOT Android synchronization

Frozen Windows `{refs['winHead']}`, iOS `{refs['iosHead']}`. Unified full tests pending.
Windows patch base `{refs['winBase']}`; reported Android base `{args.android_base}` is unavailable/uninspected. Patch applicability UNKNOWN. No Android login/network/repository/branch/APK/upload/application occurs.

`shared-source-changes.tar.gz` contains all changed authoring assets, builders, tests, tools and evidence in the declared scope. `runtime-shared.tar.gz` contains only changed Scripts/Resources and metas, NOT a complete independent project or authoring bundle. The binary patch is explicitly Windows-baseline-relative. Keep Android native/platform settings unchanged; no target checkout is inspected here.

Sizes/hashes, per-file provenance, GUID audit and cumulative new runtime resource input are in manifest.json. Source archive bytes are NOT APK size or CPU/GPU memory. Editable sources and tests remain in the full bundle. Both archives are verified against Git blob content, not the working tree. No Unity import/render/device/performance acceptance is claimed.
''', encoding='utf-8')
    lines = ['LOCAL CANDIDATE; unified full tests pending; no Android application/APK.', f'PASS full archive: {full_count} changed target files; exact target hashes, sizes, modes and safe unique member paths.', f'PASS runtime-only archive: {small_count} changed target files; exact target hashes, sizes and modes.']
    for audit in audits:
        lines.append(f"PASS {audit['platform']}: {audit['preservedGuidCount']}/{audit['originalGuidCount']} original GUIDs preserved; {len(audit['newGuidPaths'])} new GUIDs, no collisions; {audit['runtimeCsFiles']} runtime C# files all have metas.")
    lines += ['PASS runtime C# contents and new GUID paths identical between frozen platforms.', 'NOT CHECKED: Android baseline, patch applicability, Unity execution or device validation.']
    (output / 'verification.log').write_text('\n'.join(lines) + '\n', encoding='utf-8')
    (output / 'SHA256SUMS').write_text(''.join(file_sha(path) + '  ' + path.name + '\n' for path in sorted(output.iterdir()) if path.is_file() and path.name != 'SHA256SUMS'), encoding='utf-8')
    print(json.dumps({'output': str(output), 'status': manifest['status'], 'artifacts': artifacts, 'manifestSha256': file_sha(output / 'manifest.json')}, indent=2))

if __name__ == '__main__':
    main()
