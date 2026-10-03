# v2 local candidate: waiting for final full acceptance

Frozen pair: Windows `b32543fc4867fec16f931ac44f5d571359d9f189`, iOS `b5a39f2c497a0e6e54a04ccb74230fe3dd500034`. Parent full validation is in progress; this handoff is a candidate, not an accepted/applied Android build. No Android remote, branch, login, package build or patch application occurred.

The original 5dd/f620 archive remains intact and superseded. This directory is the new candidate, generated entirely from the requested frozen Git refs. Full authoring/evidence archive size is not Android package cost; runtime-only is a shared source overlay, not an APK or independent Unity project. `manifest.json` distinguishes exact file scopes and new runtime resource payload bytes.

Reproduce with the included unmodified generator into a NEW directory (it refuses existing output):

```sh
python3 generate_handoff.py \
  --win-repo /workspace/emberfall_win \
  --ios-repo /workspace/emberfall_ios \
  --win-base 25096ba7dbf6e9d7ba9463ec29104c2c462da426 \
  --win-head b32543fc4867fec16f931ac44f5d571359d9f189 \
  --ios-base 1cd7ce36c77064757899788e23520fb796266888 \
  --ios-head b5a39f2c497a0e6e54a04ccb74230fe3dd500034 \
  --output /workspace/scratch/final-android-handoff-v2-reproduced
```

`Docs-ready/` holds only the small manifest, README, generator, audit and verification records for parent archival. It deliberately excludes both archives and the large binary patch. No repository has been modified or committed by this handoff generation. Run `sha256sum -c SHA256SUMS` in this output directory to verify its root artifacts, or in Docs-ready to verify only its small copied records.
