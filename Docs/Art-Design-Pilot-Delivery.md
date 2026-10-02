# Art and design pilot delivery matrix

Baseline Windows main: `03422ab83d0ab6a8f84f2147a81d8aeb63a6cd5e`. This matrix covers the complete requested round; preview work does not replace the gameplay/UI work. Source implementation is distinct from Unity/device or visual acceptance.

| Request | Implemented source and bounded behavior | Remaining acceptance |
|---|---|---|
| D01 finale readability | Main-impact priority; actual-contact cash feedback; confirmed elemental, arrow and generic player finale producers retain a visual-only tail up to 0.65 s | Empty/immune finale contact does not qualify; actual dense GPU presentation pending |
| D02 boss sweep | Shared warning/damage capsule, body compensation and full-width obstruction clipping | Conservative entire-beam clipping; Unity visual/feel pending |
| D03 paired seals | Independent A/B time, completion, contest and matching glyphs | Touch and combat readability pending |
| D04 target rejection | Bound target identity, per-action/per-skill rejection state | Actual input devices pending |
| D05 equipment comparison | One reused preview, current/candidate, inherited reinforcement, real fashion and fixed framing | Native RenderTexture/gear/cosmetic combinations pending |
| D06 rear silhouettes | Class-specific cape/robe/quiver/tail and wing attachment/open angles | Large wings still occlude some upper back; animated clipping pending |
| D07 Blender pilot | Original source/FBX/atlas, five clips, explicit opt-in adapter, starter-kit gate, fail-closed readiness and procedural fallback for moving attacks/death | First visual sample remains provisional; Unity 6 import/deformation, full motion set and visual quality acceptance pending |
| D08 scenery | Authored tree branches/open fans/variants and roof pitch/eaves/chimney | Increased geometry/renderers require device profiling |
| Three small fixes | Independent entry/result scroll; stable 32px desktop icons; preview ranger arrow pull/release/reload | Aspect ratios and native rendering pending |
| Design A | Rank-one first active for four classes; shared budget/migration preserving legal legacy allocations | Device/new-save playthrough pending |
| Design B | Pack reinforcement wolves prefer unoccupied legal targets, bounded hold; free 出击 does not refresh paid effects | Dynamic-body navigation/gameplay feel pending |
| Design C | Completed-node revisit entry tactic, zero/one of three eligible choices, room persistence and run cleanup | Choice clarity and balance pending |
| Design D | Hard Forest first-room same-count A/B, mobile provider and shared aggro with actual support range/LOS | Alternation is session-local, not persisted across app restarts; gameplay feel pending |
| Design E | Distinct generated/effective CinderTrail and FrostEcho instances, actual HP loss before lethal callbacks, factual result evidence | UI/gameplay observation pending |
| Design F | Successful-transaction-only cosmetic mastery for Forest/Redrock/Star with attribution and no fallback grants | Badge/title display only, no equipped-title system; device playthrough pending |
| VFX requested comparison | Pinned production geometry/time export plus native Blender before/after preview, same stage/camera/fps | Reconstruction is not Unity footage; optimized VFX is a visual proposal and is not installed into combat |

## Merged source delivery — 2026-10-02

Windows PR #21 merged as `c5a734ebe43925eb46fee4c236bb863a1a0d5884` (tree `4a106eb08c61a7c5a2ae8bf03227e444e414e3cd`). iOS PR #21 merged as `81ff46e28549b019638e5a190f3677f7cabab97e` (tree `337604ef6acef0cf996bea03b0a88188ebde4923`). Both merged trees exactly match their reviewed candidates. This Android delivery is a documentation-only descendant of source candidate `a5d78d03ce3ae84c3d90b39cfb72e60661aab0fb`; runtime, assets and Android configuration are unchanged.

One full Windows/shared-source run passed **189/189 checks** at `bce7b1f4e8ad65e270e78c0e2b9a44484cef9191`, whose tree exactly matches merged Windows main. The run ended at 2026-10-02T16:08:53.977973+00:00 with `sourceChangedDuringRun=[]`. This is not three full platform test runs. Separate Windows/iOS/Android conditional C# compilations passed with zero warnings/errors against cached Unity 2021 references, not Unity 6. The 256 runtime C# files and 16 pilot resources match across the three candidates. Protected platform/font settings remain unchanged, including iOS font/iPad settings.

Validation report SHA-256: `c293612316333de3828ba59a402c33131cda6a4e9eac507f558eb30f858ecf70`. Review evidence: https://github.com/h644782259/emberfall_win/tree/0daabb037ac1db2550c9490774fef2b456e2ed20/ArtSource/Review/Validation-5244a98 . The tested commit is retained on remote branch `codex/pilot-validation-bce7b1f`.

The authored Vanguard remains default-off and limited to the documented starter equipment/action set, with procedural fallback. The VFX comparison is a native Blender visual proposal, not installed combat VFX or Unity footage. Static/source checks, seed/navigation simulations and FBX roundtrip checks are not gameplay or visual acceptance.

No Unity 6 Editor, import/shader validation, real JsonUtility/PlayMode, device run, APK, Windows/iOS build, or performance acceptance was available. Android is delivered as source only; use the authorized local Unity workflow in [Android development](Android-Development.md). No license or environment restriction was bypassed. Parent controls further releases.
