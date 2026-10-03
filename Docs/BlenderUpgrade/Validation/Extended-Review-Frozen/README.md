# PR35 corrected-tree acceptance snapshot

Source inputs: Windows `0daa0f2dec8b53f695888032e3972b24a8145773`; iOS `c6c230ca023ed2dcc4ebede44a6d516399b103fa`. This snapshot supersedes the earlier 215-check report for acceptance of PR35 review corrections. Historical evidence remains under Extended-Frozen.

The full aggregate runs in a detached worktree at the Windows source commit. Source identity includes runtime files, resources, test/build code, Blender sources and exporters. The separate iOS manifest preserves its original Fonts.meta identity and iOS-only inputs. The only runtime platform difference remains the pre-existing Fonts.meta.

**Result: 218/218 checks pass, sourceChangedDuringRun=[]**. The preceding run and allocation investigation are retained in Extended-Review-Preliminary. The repeat uses a fixed .NET tiering configuration; no gameplay source or test threshold changed.

Run command: `DOTNET_TieredCompilation=0 bash Tests/Run-CloudValidation.sh --dotnet /workspace/shared/emberfall-tools/dotnet/dotnet --compile --compile-android`. Full raw output and per-check logs are retained. The source-change report and Git tree identities are the acceptance authority; individual earlier agent logs are supporting evidence only.

Compile scope: 265 runtime C# files, pinned Unity 2021.3.33 API references, Windows/iOS/Android defines. This is not Unity6 import, Editor execution, packaged build, GPU/overdraw measurement or mobile device validation. No Unity Editor is available in this environment; the recorded official download access failure was not bypassed.

Android: no repository creation, login, permission or credential changes. Corrected shared source is present in the Windows/iOS tree; the parent will integrate it with the parallel planning work before the final authorized unified source handoff.
