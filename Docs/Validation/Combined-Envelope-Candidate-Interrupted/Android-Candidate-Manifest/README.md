# Local shared-source candidate — NOT Android synchronization

Frozen Windows `b32543fc4867fec16f931ac44f5d571359d9f189`, iOS `b5a39f2c497a0e6e54a04ccb74230fe3dd500034`. Unified full tests pending.
Windows patch base `25096ba7dbf6e9d7ba9463ec29104c2c462da426`; reported Android base `8654c803eb29b87dea7d69f09678ec21e1955f6d` is unavailable/uninspected. Patch applicability UNKNOWN. No Android login/network/repository/branch/APK/upload/application occurs.

`shared-source-changes.tar.gz` contains all changed authoring assets, builders, tests, tools and evidence in the declared scope. `runtime-shared.tar.gz` contains only changed Scripts/Resources and metas, NOT a complete independent project or authoring bundle. The binary patch is explicitly Windows-baseline-relative. Keep Android native/platform settings unchanged; no target checkout is inspected here.

Sizes/hashes, per-file provenance, GUID audit and cumulative new runtime resource input are in manifest.json. Source archive bytes are NOT APK size or CPU/GPU memory. Editable sources and tests remain in the full bundle. Both archives are verified against Git blob content, not the working tree. No Unity import/render/device/performance acceptance is claimed.
