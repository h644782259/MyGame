# v3 local candidate: waiting for final full acceptance

Frozen pair: Windows `13ffda9bc711a51a16261dd77bdf55234b21610e`, iOS `f1aa2787458bc343550d024e1d303bd082b12404`. Parent full validation is in progress; this handoff is a candidate, not an accepted/applied Android build. No Android remote, branch, login, package build or patch application occurred.

The original v1 (5dd/f620) and v2 (b325/b5a) archives remain intact as historical candidates. This v3 includes the VanguardRecoveryIntegrationTests fixture compile fix; production Scripts/Resources remain byte-identical to v2. This directory is the new candidate, generated entirely from the requested frozen Git refs. Full authoring/evidence archive size is not Android package cost; runtime-only is a shared source overlay, not an APK or independent Unity project. `manifest.json` distinguishes exact file scopes and new runtime resource payload bytes.

Reproduce with the included unmodified generator into a NEW directory (it refuses existing output):

```sh
python3 generate_handoff.py \
  --win-repo /workspace/emberfall_win \
  --ios-repo /workspace/emberfall_ios \
  --win-base 25096ba7dbf6e9d7ba9463ec29104c2c462da426 \
  --win-head 13ffda9bc711a51a16261dd77bdf55234b21610e \
  --ios-base 1cd7ce36c77064757899788e23520fb796266888 \
  --ios-head f1aa2787458bc343550d024e1d303bd082b12404 \
  --output /workspace/scratch/final-android-handoff-v3-reproduced
```

`Docs-ready/` holds only the small manifest, README, generator, audit and verification records for parent archival. It deliberately excludes both archives and the large binary patch. No repository has been modified or committed by this handoff generation. Run `sha256sum -c SHA256SUMS` in this output directory to verify its root artifacts, or in Docs-ready to verify only its small copied records.
