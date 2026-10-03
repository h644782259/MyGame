# Local shared-source candidate — NOT Android synchronization

Frozen Windows `bc6a572a86fe4376f6f346b0fe52f20d9678aec3`, iOS `30f748b1bf6da7def9d91118aa756cd225c03e79`. Unified full tests pending.
Windows patch base `25096ba7dbf6e9d7ba9463ec29104c2c462da426`; reported Android base `8654c803eb29b87dea7d69f09678ec21e1955f6d` is unavailable/uninspected. Patch applicability UNKNOWN. No Android login/network/repository/branch/APK/upload/application occurs.

`shared-source-changes.tar.gz` contains all changed authoring assets, builders, tests, tools and evidence in the declared scope. `runtime-shared.tar.gz` contains only changed Scripts/Resources and metas, NOT a complete independent project or authoring bundle. The binary patch is explicitly Windows-baseline-relative. Keep Android native/platform settings unchanged; no target checkout is inspected here.

Sizes/hashes, per-file provenance, GUID audit and cumulative new runtime resource input are in manifest.json. Source archive bytes are NOT APK size or CPU/GPU memory. Editable sources and tests remain in the full bundle. Both archives are verified against Git blob content, not the working tree. No Unity import/render/device/performance acceptance is claimed.
