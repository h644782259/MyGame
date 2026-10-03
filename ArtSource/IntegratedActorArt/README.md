# Combined authored actor integration evidence

Baseline: `7f99a05`. This batch adds tests/export/evidence only; no runtime or resource changes. The single `Combined-Factory.png` is a Blender reconstruction of eight actual enabled factories, using actual palettes, visible meshes, numerical transform hierarchy and bowstring line positions. It is **not Unity rendering or device verification**. No generated or edited promotional image stands in for the exported meshes.

## One shared production path

`Tests/IntegratedActorArtProductionTests.py <dotnet> [output-directory]` prepares one managed numerical Unity fixture and compiles the actual:

- `AuthoredActorMeshes`, `ActorSilhouetteF1`, `EnemySilhouetteArt`, `WeaponModules` and `CombatModel.WeaponArt` loaders/adapters; bytes come from this checkout's Resources.
- Hero, Companion and Enemy factories, all equipment/fashion builders, weapon anchors and actual class/rarity palette.
- `AnimateHero`, `AimArm`, caster poses, `ApplyHeroLocomotion`, `PlayAction`, `CancelAction`, `CommitActionPose`, and the **entire** `CombatModel.Recovery.cs` with real visual-motion advancement.
- `VanguardActionLibrary` and `CombatModel.VanguardArt`; actual companion appearance/preparation/recall and full `CombatModel.Animate` for companions and regular enemies.

The isolated F1 exporter is reused only for constructing the fixture. Its ReviewPose helper and recovery/Vanguard placeholders are removed; the matching optional weapon/Vanguard construction stubs are removed before compilation. F2 runs on Guardian and Goblin, rather than merely being present as an unused class. Existing independent whole-body BlenderPilot import remains explicitly disabled; it is a separate previously optional path. Large encounter animation is out of scope and throws if entered. Game controllers, physics, Unity lifecycle and shaders remain fixture boundaries.

## Checks and finite scope

Four classes walk with their actual authored layers, then reset cosmetic locomotion/inertia to a controlled contact-comparison baseline. On each original actor, sequential T1 → T4 → T4 plus highest existing wings/weapon fashion → unequip preserves the body joints and Vanguard library. Each stage executes basic contact, zero-time cancel, half/full recovery, skill cancellation and replacement basic attack. Body/equipment mesh identity is checked across the complete hierarchy; temporary arrow/orb visibility changes are allowed. Contact clocks retain production values.

The tests measure actual world-space contact continuity, loaded blade root/tip against weapon anchors, bow grip socket and string/nock alignment. Spirit/Treant and Guardian/Goblin execute full regular Animate paths with stable mesh identity. The exported tree's shoulder/body intersection samples are [12, 6], and eye/head samples [77, 3]; this measures final assembled contact, not a detached module. Hero sample: T4/highest fashion/basic contact. Companions: rank-3 permanent/preparation/attack. Enemies: actual windup poses.

Missing **and** malformed Hood, GuardianChest, StarGuard, Swordguard motion library and original Cuirass each retain fallback geometry and unrelated loaded layers. A partial starter sword falls back atomically. Five compiled negative controls must fail exact assertions: skip recovery, skip authored Vanguard pose, disable F1, disable F2, skip F3 starter application. Raw failures and passing run are retained. `tests.log` records 281 logical checks plus finite geometry validation and tree contact checks; it is not a Unity test count.

`exploratory-expectation-failure.log` reproduces an early fixture error: the expected neutral Vanguard contact did not include residual cosmetic inertia from the added walking exercise. Resetting both production locomotion and visual-motion state before that isolated comparison corrects the expectation. No production motion or asset changed. The final exported geometry is byte-identical to the earlier render input.

## Reproduce

```sh
python3 Tests/IntegratedActorArtProductionTests.py /path/to/dotnet /tmp/integrated-actor-art
blender -b -t 2 --factory-startup --python ArtSource/IntegratedActorArt/render.py -- /tmp/integrated-actor-art/actors.json ArtSource/IntegratedActorArt/Combined-Factory.png
```

The renderer also accepts retained `actors.json.gz`, limits itself to two threads and 16 samples, and creates fixed camera-facing labels and an explicit NOT Unity caption. It renders one image. `source-hashes.json` records the source and resource inputs; no new mesh/material/texture resources enter the game.
