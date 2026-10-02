# Script metadata identity repair

Two script meta files inherited malformed 33-hex GUIDs. This revision replaces only those asset identity values, using newly generated UUID4 identities rather than truncating old values. Runtime C#, art bytes, importer options, platform/font/iPad configuration and save formats are unchanged.

| Script meta | Old malformed identity | New identity |
| --- | --- | --- |
| `Assets/Scripts/Combat/WeaponVisualLinks.cs.meta` | `68a6e6a74a54417a8f1162a6ff67f5dc4` | `e552c6894a804ab59e21b10545f8c637` |
| `Assets/Scripts/Core/FilledVfxPlacement.cs.meta` | `a61004eb35a84c3c821b82421b504a5cf` | `c0322347832b4025895339bcf3671693` |

Before editing, a case-insensitive byte scan over every tracked file found each old value only in its defining meta; neither new identity occurred anywhere. Therefore no serialized-reference migration is required in the tracked project. The same mapping is applied to Windows, iOS and Android. Old values remain in this historical mapping only after repair. This audit cannot account for external projects or untracked developer scenes.

Run `python3 Tools/validate-meta-guids.py` to check every authored Assets file/folder for a meta pair, exactly one root GUID, nonzero 32-hex format and case-insensitive uniqueness. Nested importer GUID references are not mistaken for identity declarations. Symlinks and nonregular entries fail. No asset extension is excluded. Valid folder metas may represent empty folders absent from Git: Unity recreates these directories; file metas without their asset fail. The Assets root itself needs no meta. The scanner does not edit source, parse the full importer schema, resolve package/builtin/subasset references or prove Unity import behavior.

`python3 Tests/MetaGuidAuditTests.py` executes the real scanner against 18 disposable positive/negative fixtures, checking exact diagnostics, deterministic results, exit status and unchanged bytes. Both scanner and controls are registered in `Tools/cloud-validation.py`, increasing the suite from 198 to 200 checks. The prior 198-check PR27 archive report is historical and does not cover these changed metadata values. New candidate and post-merge archive reports are separate evidence.

No Unity Editor is available in the execution environment. Passing this audit confirms authored metadata structure and pairing, not an actual AssetDatabase refresh, script import or device build.
