# Bounded Vanguard and Redrock replay refinement

This round starts from merged Windows main `c5a734ebe43925eb46fee4c236bb863a1a0d5884` and iOS main `81ff46e28549b019638e5a190f3677f7cabab97e`. It refines one authored character and one existing chapter room, rather than introducing another challenge system. The prior pilot delivery remains the baseline.

## Redrock: a route that changes movement and sight

The original second room always requires travel around a continuous central retaining wall. Mirroring that wall changes handedness but retains the same tactical topology. Completed Redrock replays on Hard or Heroic now alternate between that layout and a split retaining wall with a four-metre central cross passage. The two hoist columns remain on solid wall pieces, and the suspended beam stays overhead. Both rendering and navigation consume the same `ChapterRoomPlan` obstacles.

The passage creates a direct side-to-side movement and ranged sight connection. Crossing can shorten a reposition but also opens crossfire; the continuous wall retains cover and requires a detour. This is the intended tradeoff, not a claim that simulation establishes enjoyable or balanced play. The existing six-enemy crossfire roster, escape objective, four-second objective requirement, rewards and statistics remain unchanged. The first room, Normal difficulty, first-clear progression and other chapters keep their existing layout.

The seed encodes the route independently from mirror and Forest formation bits, with a version marker outside the old generated seed range. `Plan` and `FromLayout` reproduce the geometry from the seed alone. The first eligible admission in a fresh host uses the split route, then successful admissions alternate. Failed save, receipt, world build, path or spawn admission does not advance the choice. The bounded history is for the most recently admitted save owner in the current session; it is not persisted across app restarts or remembered for multiple inactive owners. No save fields or migration are added.

Validation executes the real `WorldTraversal` code for 64 raw seeds, both route choices and actor radii .45, .65, .9 and 1.3. Checks cover actual direct-path and sight differences, reachable entrances/objectives/exits/spawns, sampled usable floor connectivity, spacing, six-enemy cap, legacy seed behavior, unchanged other-room footprints and the existing Heroic heat envelope. A compiled mutant closes the passage and must fail the actual crossing assertion. Host tests execute real progression/admission logic with scene-boundary doubles, including failed entries, consecutive runs, same-save reload and save-owner changes. These are source/navigation checks, not device playtests.

Run `python3 Tests/RedrockReplayGeometryTests.py DOTNET_EXECUTABLE` and `python3 Tests/ChapterHostProductionTests.py DOTNET_EXECUTABLE` for scoped verification. The ordinary cloud suite also executes the extended geometry and host checks. First-clear Hard/Heroic is rejected by actual normalized progression, so tests preserve that rejection rather than inventing an admissible first-clear receipt.

## Vanguard: connected joints and a shaped mantle

The default-off authored Vanguard pilot retains its starter-equipment/action gate and fallback behavior. The asset refinement connects sleeve cloth across the existing shoulder/elbow/wrist bones and shapes the mantle with a closed folded surface and bowed profile. It does not change skeleton, action timing, attachment sockets, shared atlas/material or props. See [the authored source and evidence contract](../ArtSource/BlenderPilotRefinement/README.md) for exact geometry counts and reproduction commands.

## Remaining acceptance

No Unity 6 Editor, engine import/material/binding, real JsonUtility/PlayMode, touchscreen/device, platform build or performance acceptance was available. The overhead hoist's camera fading and the passage's practical readability still need engine review. Native Blender portraits and skin/socket roundtrips support asset review; they do not establish in-game visual quality. The VFX comparison from the preceding batch remains a proposal and is not installed by this round. Platform and font settings, including iOS iPad settings, must stay unchanged during synchronization. Parent owns review, merge and release.
