# Final overnight source delivery

Selected freeze: Windows main `ace3109c23485d7f145d372b8cb3c24bd79abe89`, iOS main `a9caf3776d80439c7c559202b531dc4a3df4518e`, Android synchronized source `b1104bbd05e4f7f360ace83ec6d8ccc16ed1b0e1`. The main trees equal reviewed PR27 candidates. No source changes are made on this delivery branch.

`Emberfall-Android-Source-b1104bb.zip` is **29,504,810 bytes**, SHA-256 `798871e7592c640323377b14c69f0f74729468f402341058409789984603d012`, 1,153 entries: every one of 1,152 tracked Android files plus `SOURCE-DELIVERY.json`. It is source only, not an APK/AAB or Unity-validated build. No tracked files were omitted to meet a size limit; build caches and separate evidence branches are not in the source tree.

Read `PR21-27更新说明.txt` for concise player-facing changes and `交付与验收说明.txt` for remaining acceptance. `source-inventory.json` covers every packaged source byte; `Platform-Inventories/` records all three complete tracked trees and Git-attribute conversions. Eight PowerShell files use the declared CRLF archive conversion; raw Git blob bytes differ as expected and normalize to their recorded blob identities.

`asset-preview-manifest.json` distinguishes historical authoring pictures/videos, the default-off pilot, uninstalled VFX proposal and current discrete FBX precision evidence. Existing native Library identities are preserved. Hashes identify retained local originals; this task refreshed native metadata without downloading those media again. Older PR25 review material retains its old FBX FAIL; the PR27 4,923-sample discrete PASS is separate. No continuous-time, Unity rendering, gameplay-feel or device-performance claim is made.

## Verification status

Frozen PR27 registered198 and all three API compilations passed; exact reports are linked in the source delivery manifest. Static clean-room archive audit passes file/inventory integrity, metadata presence, GUID references, static Resources paths, fonts, scenes and protected Android configuration. Two pre-existing 33-hex script meta GUIDs are reported as source findings, with no serialized references found; native Unity behavior has not been tested. This package reproduces the frozen source rather than silently repairing those identities.

The actual packaged source is currently running its unchanged cloud-validation entry with `--compile` in a new extraction. An external pinned Unity2021 reference-cache symlink and generated test reports are recorded separately; no packaged source file is edited. Terminal full-suite and post-run ZIP/source hashes will be appended. This checkpoint does not claim that archive execution is complete.

Library replacement remains parent-owned and has not been performed here. The existing source identity was freshly observed at version5, but the parent must re-read its current version before replacement rather than reuse a stale guard.
