# Independent metadata repair review — GO

Reviewed exact candidates:

- Windows `fcdd97bfbcbeca6b9cf022c86a30766de6c93597`
- iOS `887cbbe84bac1f9b26d6aa7ee3d725b742453c93`
- Android `8654c803eb29b87dea7d69f09678ec21e1955f6d`

The independent recorder enumerates every tracked Git blob at each candidate and its first parent with `git ls-tree -r -z`, reads those exact blobs using `git cat-file --batch`, and searches lowercased bytes for both malformed identities. Each old GUID occurs only in its respective defining meta before repair and only in `Docs/Metadata-Guid-Repair.md` afterward. Both replacements parse as UUID version 4; the scanner confirms no duplicate identity.

Exactly six paths change per platform. Hashes of all six candidate files match across Windows, iOS and Android, and each working file matches its committed blob. The two meta edits affect GUID values only; other files add the scanner, its fixtures, documentation and two validation registrations. No runtime, art, importer, save or platform settings change in these commits.

The scanner source distinguishes root declarations from nested references; accepts BOM/CRLF; detects malformed/zero GUIDs and case-insensitive identity duplicates; checks file/folder meta pairs; rejects symlinks/nonregular entries; and explicitly permits valid folder metas whose empty directory is absent from Git. Scope includes hidden authored entries and has no extension allowlist. It is not a full Unity YAML/schema or external/package/subasset reference resolver.

Independent execution passed the real scanner (294 asset files, 12 folders, 306 metas and 306 unique GUIDs, zero failures) and all 18 disposable positive/negative fixtures. Fixtures check exact failure kinds, deterministic repeated diagnostics, process exit codes and unchanged input bytes. Raw stdout/stderr are saved beside this note. The record-producing rerun does not run the full 200-check suite or modify any candidate file.

Reproduce evidence capture:

```sh
python3 /workspace/scratch/guid-fix/independent-review/record_review.py
```

`review.json` contains exact heads and parents, blob counts/matches, changed-file hashes, commands and execution status. `scanner.stdout.log` / `scanner.stderr.log` and `controls.stdout.log` / `controls.stderr.log` preserve tool output. `review.stdout.log` and `review.stderr.log` preserve recorder output. The recorder is independently authored, read-only toward all three candidates, and writes only this evidence directory.

No remaining blocker found. External projects and untracked developer scenes are outside the blob scan; Unity AssetDatabase refresh, actual script import and device builds remain unverified. The parent's baseline malformed-GUID run and full 200-check result are separate evidence, not represented as executions performed here.
