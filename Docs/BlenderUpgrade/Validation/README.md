# Validation evidence index

The files directly in this directory and `Cloud/` are the **initial historical 205-check snapshot**. For the corrected PR30–32 stack, use `PR32-Frozen/` (207 checks and exact source identity). For the later E01–E06/wolf batch, use `Extended-Frozen/`. `Extended-Preliminary/` is a failed diagnostic run and must not be counted as acceptance.

## Historical initial snapshot

2026-10-03: 205 registered checks passed, no source change during the run; see `Cloud/report.json` and all original per-check `.log` files. Command:

```sh
bash Tests/Run-CloudValidation.sh --dotnet /workspace/shared/emberfall-tools/dotnet/dotnet --compile --compile-android
```

This includes the four new authored-art suites. `full-run.log` is the unedited aggregate output. Failures inside logs marked as expected compiled negative controls are test evidence, not an overall failed check.

`compile-report.json` and win/ios/android compile logs are a separate C# compile of the 260 shared runtime sources under each platform define using pinned UnityEngine.Modules **2021.3.33** API references. It is not Unity 6 compilation. `compile-command.py.txt` preserves the exact workspace command script used; its absolute paths are environment-specific.

`platform-parity.json` verifies all 260 runtime C# files match between Windows/iOS and all shared tests match. The one additional iOS font source test is retained and passed separately (`ios-font-source.log`). Changed resources/source files are also byte-mirrored via exact git blobs. Original platform project settings/exporters/README were not overwritten.

`meta-final.json` validates all 350 GUID identities; all 306 preexisting GUIDs were additionally compared with the pinned Windows main and preserved. The resource table has since advanced with later batches; use each frozen report and its source revision for that snapshot. The old GUID comparison checks identity rather than Unity subasset reference resolution.

The preliminary run during concurrent implementation was explicitly invalidated by its source-change guard and found missing new fixture boundaries; those were corrected. Its output is retained outside the repository at `/workspace/scratch/blender-upgrade/baseline-results`, not used as final passing evidence.

**Not executed:** Unity Editor import/PlayMode/shader rendering, actual JsonUtility, native platform builds, device input/performance or Unity gameplay capture. Official Unity download returned proxy403. Blender/managed checks and reference compilation do not remove that boundary.
