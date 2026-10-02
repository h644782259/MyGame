# Final overnight source delivery

Selected freeze: Windows main `ace3109c23485d7f145d372b8cb3c24bd79abe89`, iOS main `a9caf3776d80439c7c559202b531dc4a3df4518e`, Android synchronized source `b1104bbd05e4f7f360ace83ec6d8ccc16ed1b0e1`. The main trees equal reviewed PR27 candidates. No source changes are made on this delivery branch.

`Emberfall-Android-Source-b1104bb.zip` is **29,504,810 bytes**, SHA-256 `798871e7592c640323377b14c69f0f74729468f402341058409789984603d012`, 1,153 entries: every one of 1,152 tracked Android files plus `SOURCE-DELIVERY.json`. It is source only, not an APK/AAB or Unity-validated build. No tracked files were omitted to meet a size limit; build caches and separate evidence branches are not in the source tree.

Read `PR21-27更新说明.txt` for concise player-facing changes and `交付与验收说明.txt` for remaining acceptance. `source-inventory.json` covers every packaged source byte; `Platform-Inventories/` records all three complete tracked trees and Git-attribute conversions. Eight PowerShell files use the declared CRLF archive conversion; raw Git blob bytes differ as expected and normalize to their recorded blob identities.

`asset-preview-manifest.json` distinguishes historical authoring pictures/videos, the default-off pilot, uninstalled VFX proposal and current discrete FBX precision evidence. Existing native Library identities are preserved. Hashes identify retained local originals; this task refreshed native metadata without downloading those media again. Older PR25 review material retains its old FBX FAIL; the PR27 4,923-sample discrete PASS is separate. No continuous-time, Unity rendering, gameplay-feel or device-performance claim is made.

## Verification status

Frozen PR27 registered198 and all three API compilations passed; exact reports are linked in the source delivery manifest. Static clean-room archive audit passes file/inventory integrity, metadata presence, GUID references, static Resources paths, fonts, scenes and protected Android configuration. Two pre-existing 33-hex script meta GUIDs are confirmed malformed source identities: packagingPassed=true but staticSourceClean=false. No serialized references were found; this limits migration scope but does not establish import safety. Native Unity behavior has not been tested. See Archive-QA/guid-assessment.json for exact paths, values, introduction commits and reference-scan boundaries. This package reproduces the frozen source rather than silently repairing those identities.

The actual packaged source completed its unchanged `Tools/cloud-validation.py --compile` entry in a new extraction: **198/198 PASS**, 256 runtime source files compiled, default JIT, UTC 2026-10-02 21:36:35.926017 to 21:52:41.034844. Report SHA-256: `ffbd0aeaa14243d024c7e5470fc0f1238bc3ccfd2cf186de9a990f4f1fa26cb8`. `Packaged-Source-QA/Cloud-Latest/` contains the raw report and individual logs; `full.log` records the run. An external pinned Unity2021 reference-cache symlink and generated test reports are accounted for separately; no packaged source file was edited. This is managed production-logic and reference-API compilation, not Unity 6 execution or a platform build.

Library replacement remains parent-owned and has not been performed here. The existing source identity was freshly observed at version5, but the parent must re-read its current version before replacement rather than reuse a stale guard.

`Packaged-Binary-QA/` independently reads all five FBXs, two packed source blends, fifteen PNGs and both fonts from a separate extraction. Its terminal PASS is bounded binary readability, not Unity import or visual approval; all 1,153 packaged files and the ZIP retain their original hashes.

`Packaged-Source-QA/final-handoff.json` closes the execution audit: all 1,153 packaged files retained their hashes, no source changes, pinned external references unchanged, and 203 generated non-source outputs inventoried separately. The original ZIP SHA-256 is unchanged after execution and independent binary QA. Two malformed GUID findings remain unresolved; these results do not claim a clean Unity import.
