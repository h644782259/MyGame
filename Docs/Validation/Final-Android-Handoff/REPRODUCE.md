# v4 candidate: final full acceptance pending

Frozen Windows `bc6a572a86fe4376f6f346b0fe52f20d9678aec3`, iOS `30f748b1bf6da7def9d91118aa756cd225c03e79`. The parent's233-check full run is in progress. The preceding231/233 result and historical v1–v3 packages are retained; none substitutes for this new frozen candidate's final report.

Production Scripts/Resources are unchanged from v2/v3. This candidate includes fixture compile-boundary repairs and test-output isolation; runtime-only archive SHA-256 is therefore unchanged. All v1–v3 files remain intact. No Android checkout, remote/network/login, branch, APK, patch application, upload or repository commit was attempted. The Windows-based patch's applicability to the unavailable Android baseline remains unknown.

Full source/authoring/evidence archive bytes are not Android package cost. Runtime-only is a changed shared-source overlay, not an APK, complete Unity project, or complete authoring/test bundle. See manifest.json for precise scopes, target blob hashes and cumulative new runtime resource payload bytes.

Reproduce offline into a NEW output directory:

```sh
python3 generate_handoff.py \
  --win-repo /workspace/emberfall_win \
  --ios-repo /workspace/emberfall_ios \
  --win-base 25096ba7dbf6e9d7ba9463ec29104c2c462da426 \
  --win-head bc6a572a86fe4376f6f346b0fe52f20d9678aec3 \
  --ios-base 1cd7ce36c77064757899788e23520fb796266888 \
  --ios-head 30f748b1bf6da7def9d91118aa756cd225c03e79 \
  --output /workspace/scratch/final-android-handoff-v4-reproduced
```

Docs-ready contains small manifest/README/generator/audit/log/checksum files only, for parent archival. Large tar archives and binary patch remain outside Git. Run `sha256sum -c SHA256SUMS` at the output root for artifacts, or inside Docs-ready for copied documentation records. The root generator reads immutable refs rather than working-tree files and refuses existing output directories.
