# Frozen E01–E06 and wolf acceptance evidence

All215 registered checks passed in the isolated worktree at Windows `c959e5cae8f0a87d0adee75bad5152d6cab21a7f`, with matching iOS inputs at `343ea11b5857b34cd0597f5dfcaaa3786adce9c1`. Started 20261003T014141056029Z; completed 2026-10-03T01:54:19.615728+00:00. sourceChangedDuringRun is empty; every recorded source hash was rechecked against both the tested worktree and final development tree before publication.

Command: `bash Tests/Run-CloudValidation.sh --dotnet /workspace/shared/emberfall-tools/dotnet/dotnet --compile --compile-android`. Cloud/report.json and all per-check logs plus full-run.log are included. Expected compiled mutation failures inside passing logs are negative controls. The failed preliminary run is separately retained in Extended-Preliminary and does not count toward this pass.

Separate Windows/iOS/Android define compilations against pinned Unity2021.3.33 API references pass, with zero warnings/errors and sourceStable=true. All265 runtime C# files match across Windows/iOS. source-manifest.json also covers runtime resources, tests, tools and authoring/export inputs. The inherited Fonts.meta difference and iOS-only font/exporter files remain unchanged. Original GUIDs: Windows306/306 preserved, current372 unique; iOS309/309 preserved, current375 unique.

After parent approval merged PR30–32 into main, both feature branches integrated that main without conflicts or any file/tree changes. main-integration.json binds those graph-only merge heads to exactly the same tested trees. Later evidence commits change documentation only. The full frozen215-check report therefore validates the integrated source tree, not a substitute older stack.

Cumulative resource inputs:50 files,480,568 bytes. ResourceBudget.json is the latest per-file table; the separate PR32-Frozen report still correctly describes its38-file338,792-byte snapshot. Android archive identity/location are recorded in android-handoff.json; its source/art inputs match the tested head. Android repository access remains404 and applicability to its inaccessible branch is unverified.

These are managed/API boundary checks and Blender constructions, not Unity Editor, real rendering/JsonUtility, FrameDebugger/FPS or native device build acceptance. Unity is unavailable; the official editor download was blocked by proxy403. No engine acceptance is claimed.
