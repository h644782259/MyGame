# Frozen corrected PR30–32 stack

Windows tested runtime head: `cda8a9045b3ea6e11b352cff40af8a39b5939e4c`; iOS matching shared runtime: `fbd3b7011fc702b6d9921ec8138ebced1efc3041`. Complete commit trees and file SHA-256 are in source-manifest.json. This directory excludes all E01–E06 and wolf follow-up changes.

The independent detached worktree ran `bash Tests/Run-CloudValidation.sh --dotnet /workspace/shared/emberfall-tools/dotnet/dotnet --compile --compile-android`. All 207 registered checks passed, sourceChangedDuringRun is empty, and every recorded source hash was checked again after completion. Started 20261003T011448960439Z; ended 2026-10-03T01:29:32.294351+00:00. Cloud/report.json is the machine-readable authority; every per-check log and full console output is included.

Separate Windows/iOS/Android define compilations all passed against pinned Unity 2021.3.33 API references, with sourceStable=true and zero warnings/errors. All 260 runtime C# files are identical between the named heads. The original platform-specific Fonts.meta difference and iOS-only font files/exporters are retained, listed explicitly in source-manifest.json. No Unity Editor or device build/rendering execution occurred.

Budget corrected: 38 new runtime resource inputs, 338,792 bytes. The nine base buffers plus two dedicated Ice/Fire primary buffers total34,004 bytes. ResourceBudget.json at the parent documentation directory records each resource's bytes/hash/GUID. These are input bytes, not compressed player size or resident memory.

Later commits on the evidence branch add only documentation, static review thumbnails and raw evidence. They do not alter the tested Assets, Tests or Tools inputs. Original 205-check reports remain historical and are superseded for this corrected stack by this report.
