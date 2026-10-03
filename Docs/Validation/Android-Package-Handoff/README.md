# Android package handoff for native artifact worker

This evidence-only handoff publishes packaging rules and frozen metadata, not ZIP archives or runtime changes. Use immutable source commits below, never the current tip of this evidence branch as the game source. No Android commit/push, Unity build or device acceptance occurred. The prior Library save failed before prepare/transfer/finalize because the cloud helper could not perform hosted tools/list (network); neither original item was changed and the chat item was not created.

## Exact sources and published validation

- Android baseline, available as an object through the authorized Windows repository: `8654c803eb29b87dea7d69f09678ec21e1955f6d`.
- Windows original baseline used to identify Android differences: `25096ba7dbf6e9d7ba9463ec29104c2c462da426`.
- Windows final main: `5d85e47b9fab2489d5b06963a0b896ec19112740`; tree `d8a472e7f594a28988b2aa5fd9f73672e97518da`.
- iOS final main: `d3a4aab185eb368f5a4a7aba67b43678bfea508f`; tree `ff00beb4e3a004e6e235f717bd445b61a43bd854`.
- Previously published final delivery: Windows final-main `Docs/BlenderUpgrade/Final-Delivery.md`; frozen validation manifest `Docs/Validation/Final-Combined-Frozen/source-manifest.json`; source evidence was committed at `7e02b91b7ba7dc63cb32620257f2650bb99ed6f1` before merge. The 233/233 managed checks and three pinned API compilations are not a Unity engine/device test.
- Package final manifests: `full-compact/manifest.json`, `chat/manifest.json` in this handoff. Entry-level authoritative recovery map: `source-selection.json` (4564 paths, exact Git blob + source commit or frozen generated payload, SHA256, mode, size and compact inclusion).

## Full project selection

Start with every exact Android baseline Git blob. Compute platform-different paths as the union of Android and original-Windows filenames whose byte contents differ. Overlay final Windows paths ONLY in `Assets/Scripts/`, `Assets/Resources/`, `ArtSource/`, `Tests/`, `Tools/`, `Docs/`, EXCEPT platform-different paths, `Assets/Resources/Fonts.meta`, and everything under `Assets/Resources/Fonts/`. Do not delete original paths. Packages, ProjectSettings, Editor/native files, root configuration and fonts remain Android baseline. Save original Android README as `Docs/Android-Original-README.md`, replace root README with the frozen generated payload, and include the entire frozen `Docs/Android-Integrated-Package/` metadata directory.

The 20 platform-different paths are enumerated with original/final/integrated hashes in `full-compact/platform-differences.json`: `.gitignore`; `Assets/Editor/AndroidDevelopmentBuild.cs` and `.meta`; `Assets/Editor/ProjectTools.cs`; font license and OTF with metas; `Docs/Android-Development.md`, `Android-Toolchain-Report.json`, `Art-Design-Pilot-Delivery.md`, `Moving-Basic-Pilot.md`, `Release-2026-10-03.md`; `ProjectSettings/ProjectSettings.asset`; `README.md`; `Tests/AndroidBuildAuditTests.py`, `AndroidPlatformTests.py`; `Tools/Android/build_debug.py`, `policy.json`, `preflight.py`. Fonts.meta is protected in addition to those 20.

Use the original Library baseline only after verifying its entries against required baseline SHA256 values. A local git-archive ZIP had eight PowerShell files converted to CRLF (see original-baseline-zip-audit.json); assembled packages used exact Git blobs. Do not assume a ZIP container or extracted checkout preserves original bytes. Missing/mismatched bytes must be resolved from the cited authorized Git source, not guessed. `source-selection.json` supersedes the generic origin label in full-compact/file-manifest.json for generated metadata.

## Compact selection and Blender dependencies

Compact equals full minus exactly the 126 entries in `full-compact/compact-excluded-review-media.json`. The builder selects offline png/jpg/jpeg/mp4 under ArtSource or Docs/BlenderUpgrade/ReviewThumbnails, excluding dependency basenames and any path component sources/textures/atlas (case-insensitive). No Assets or .blend are excluded. Use the explicit frozen list rather than broad extension removal.

All 17 .blend files were opened with Blender 4.3.2, factory startup and autoexecution disabled. Inspector enumerated images, movieclips, sounds, libraries and fonts. `full-compact/blend-dependencies.json` records 6 image references in PilotProps, PilotVanguard and WeaponModules, all packed=true; other 14 files have no such references. Older //Assets/Art image path strings remain in some files but packed image bytes are present. All runtime textures/fonts remain in both archives. The inspector and raw scan log are supplied.

## Chat project selection

Derive from the exact full archive. Remove all `ArtSource/` paths and all `Docs/` except `Docs/Android-Development.md`, `Docs/Android-Lifecycle.md`, `Docs/Android-Toolchain-Report.json` (retain these if present). Preserve ALL Assets, ProjectSettings, Packages, Tools and Tests and every other root path. Replace README and add frozen Docs/Android-Chat-Package metadata. Exact excluded list is `chat/excluded-files.json` (3234 paths). Chat is runtime-complete project source, not editable Blender source and not an APK. All runtime paths must match the original full project byte for byte. Do not remove runtime files if size changes; report instead.

## Reproduction boundaries and validation

Original builders are published unchanged: `full-compact/build_android_integrated.py`, companion inspector, and `chat/build_chat_package.py`. The full builder performs fresh Blender inspection; its resolved absolute paths vary by output directory. To recover the exact frozen package content without cloud filesystem access, use source-selection.json plus frozen-generated-payload/full (all generated bytes supplied), rather than regenerating metadata with new paths. Chat's original builder intentionally guards the exact full ZIP hash. Frozen chat payload is also provided so a native worker can preserve entry bytes while using its own archive transport. Report any different container hash honestly, and verify all content hashes.

ZIP order is sorted, timestamps 1980-01-01 00:00:00, Unix mode in external_attr, DEFLATED via the original Python zipfile builders. No archive comments or path prefix. Byte-identical compressed ZIPs also depend on Python/zlib behavior; unchanged entry hashes are the authoritative source-content evidence. No ZIP was rebuilt during this handoff.

Required validation: safe unique paths, CRC, all entry SHA256, original 309 GUID preservation / 451 final GUID paths, no duplicate GUID or missing Assets/folder meta, 273 shared C# bytes identical to both final Windows and iOS, full/compact Assets identical, 17 editable blends retained in both, chat Assets/ProjectSettings/Packages byte-identical (901 files), Tools/Tests retained, and chat under 20 MiB. Supplied independent audit records passed these applicable checks.

## Frozen archive receipts and Library mutations

| Archive | Bytes | SHA256 |
| --- | ---: | --- |
| Emberfall-Android-Integrated-Source-base8654c803.zip | 200620131 | c58a7542ecebfca4e03880d305cf77f527906bb92c143d9bb28a0089fcbb4e94 |
| Emberfall-Android-Integrated-Compact-base8654c803.zip | 42147129 | fa63150e84e8254b094ad4b6518ebedca6636bcfbbc517660c83a80ccf170669 |
| Emberfall-Android-Runtime-Project-base8654c803.zip | 12121456 | 4982e0e727937ba467f5019ef8226acd8d2aa0cd4efed035f40f4aa0845ad50d |

Preserve requested mutation order: full replacement of `libfile_29b2c7a771bc8191a6593d8035cfd597` with expected current version 6; compact replacement of `libfile_ecf26b1c9934819189f3dbc8a1a69e6e` with expected current version 0; chat create, artifact type other. Re-read if needed for concurrent changes, retain guards, and follow the official Library workflow. No credentials, signed URLs or Library upload helper copies are published here.
