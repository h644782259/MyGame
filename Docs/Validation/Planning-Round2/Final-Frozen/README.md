# Planning round 2 — final frozen integrated validation

## Exact validated source

- Windows: `4388d71defb8c65b0c29e08435d10f7144a01b8e`.
- iOS equivalent: `b187c72500bba1a883777076f958b1823ccdcc96`.
- Published branch: `codex/planning-round2` in both repositories. The later delivery commit adds evidence only; these are the actual tested source heads.
- Local Android source: `/workspace/scratch/planning-round2/android-source`, preserving original platform base `8654c803eb29b87dea7d69f09678ec21e1955f6d`. No new repository, Android commit/push, login, archive or Library operation.

The scope is the five planning packages plus G01–G07 described in the parent README. Each has a separate implementation commit and package-specific raw evidence. The parent reviewer controls merge; main has not been modified by this task.

## Verification

**253/253 full combined checks passed, process exit 0.** All 6043 tracked inputs and 204 external reference-package files were unchanged; source HEAD and SDK executable also stayed unchanged. Explicit Windows/iOS/Android API restores and builds all returned 0 for the 285 runtime C# files. See raw reports below.

`input-manifest.json` inventories all 6043 tracked input files (including all runners, fixtures, shaders, resources, ArtSource scripts and prior evidence), 204 external pinned-reference files, SDK version and executable hash. `input-stability.json` checks the same complete input set after the run. This deliberately exceeds the narrower C# source hash in the managed report.

`Managed/report.json`, `Managed/*.log` and `managed-raw.log` contain the full combined run. `Three-Platform-API/compile-report.json` and the three raw build logs cover all 285 runtime C# files with explicit Windows, iOS and Android macros against pinned Unity 2021.3.33 API references. These are managed tests/compilation, not Unity execution.

Commands executed in detached `/workspace/scratch/planning-round2/validation-v4`:

```sh
bash Tests/Run-CloudValidation.sh --dotnet /workspace/shared/emberfall-tools/dotnet/dotnet --compile --compile-android
python3 Docs/Validation/Final-Combined-Frozen/compile_runtime_at.py /workspace/scratch/planning-round2/validation-v4 /workspace/scratch/planning-round2/final-api-v4
```

`ManifestTools` retains the scripts used to inventory and compare inputs. Ignored generated outputs are not source inputs. The fixed-scenery runner now writes only `Tests/TestResults/FixedScenery-Latest`, preserving historical ArtSource snapshots.

## Parent-review corrections included

| Finding | Windows correction | Concrete regression evidence |
|---|---|---|
| Summoner no-target countdown conflicted with green action feedback | `c913c6e7c7178353f12dd02253726c1c5ef898bc` | Actual desktop/mobile draws use the same actionable window; old query and green-dot mutations rejected. `G01-SummonerSlotFix` |
| Paused result suppression leaked across owner/epoch with reused receipt number | `d4ba334ef4987ca9a453b6213a26aa2fec741859` | New owner/epoch receipt 1 displays after empty first observation; first-frame stale result stays suppressed. `G01-ReceiptScopeFix` |
| Draft rank effects ignored equipped mechanism replacement | `e3da940304e4535a089b8cac372334b04274953a` | Real collect/equip/unlock/toggle, venom B rank 2→3 and 3→2, other replacement modifiers and shared production coefficients; old descriptions rejected. `G04-Variant-Fix` |
| Low-FPS queue expiry caused starvation; update order shortened warning spacing | `dbb0bf5d3efbeda79c723f0c69f64653c7a0e7c6` | Actual enemy methods at 50/200/250/500ms and mixed spikes, both host orders, withdrawal/death/disable; old policy and lagged-clock mutations rejected. `Coordination-Fairness` |
| Failed chest draw lost on actual menu service replacement | `8364848e608050490807cd4e970f814927c8ea21` | Real SaveSlotTransition, repeated same-slot replacement, A→B→A, abandoned candidate map isolation; removed carry rejected. `RewardLoadTransition` |
| Practice cancelled nearby real enemies, retained real save path, and blocked window close | `7453280de88a88f93fae4ee1041a0e99b2e1a21f` | Safe-entry refusal before actual OnDisable; memory-only storage target; actual quit, background, destroy and failed-save paths. `PracticeIsolation` |
| Direct NewGame on memory practice reached a null slot key | `1567a40c8ec1a4c34cd5b3b748d00a4f06c56036` | Public API refusal preserves profile identity/fields/disk; exact removed guard rejected. `Practice-NewGame-Guard` |

The above paths are siblings of this directory. Raw outputs include expected negative-control failures; these are accepted only when compiled old/mutated behavior fails the intended assertion.

## Platform and history preservation

`platform-sync.json` records identical bytes for all 1615 files changed at the validated source heads on Windows, iOS and local Android. Seventy-two Android platform configuration/package/editor files are unchanged. `shared-scope-and-guids.json` additionally compares the complete tracked runtime/resource/test/ArtSource scope. Existing iOS Fonts.meta and four Android PowerShell-runner differences are preserved from the original baselines. Existing GUIDs and historical JSON evidence are unchanged; twelve new script metadata files have stable GUIDs.

`Integrated-Attempt-V1` retains the first full run (225/251), all failures and the broad input audit that detected the old scenery test rewriting two tracked gzip snapshots. Its narrower source hash did not catch that drift. `Integrated-Attempt-V2` retains the deliberately interrupted run after direct NewGame review exposed the remaining memory-service API boundary. `Integrated-Attempt-V3` retains the complete 241/253 run before the last shared test dependency repairs; its complete input inventory was stable. None of these attempts is counted as final acceptance. Runtime/ArtSource/Tools bytes in this final source are identical to 1567a40; the final changes are tests and evidence only. `Integration/Full-Suite-Fixture-Retry` preserves compatibility repairs and original negative controls.

## Remaining boundaries and publication blocker

No Unity Editor is installed. Unity shader/resource import, real JsonUtility, physics contacts, touch/input feel, same-camera visual capture, player builds and Windows/iOS/Android device execution remain unverified. Renderer inventory reductions do not establish GPU/draw-call/performance gains. Prior Blender/Unity recordings are not evidence of this round's new behavior. Optional `BlenderPilotArt=true` can switch from imported body to procedural body for unsupported movement; the default is off and that pilot does not provide a complete same-body action set.

Unpersisted failed chest draws are protected across in-process menu service replacements, but cannot be guaranteed across a process restart when the write never succeeded. Low-FPS warning admission retains the configured 0.35–0.6s scheduling interval/minimum separation; execution is frame-quantized and may occur later, not at impossible subframe times.

Draft PR creation was attempted once per repository and the original gh GraphQL request returned `Post "https://api.github.com/graphql": Forbidden` (exit 1). No retry, alternate endpoint or credential change was used. Branches are reviewable; PR creation and main merge remain with the parent reviewer. No automatic approval rejection was observed.
