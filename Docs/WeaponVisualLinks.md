# Weapon visual connections

This follow-up consumes `CombatModel.TryGetWeaponVisualAnchor` and `WeaponSwingSide` from the weapon-structure change. It follows A04's primary-volume change. It does not replace a safe projectile origin with an animated weapon coordinate.

`CombatProjectile` retains its simulation transform, scaling, velocity, radius, target and collision processing. Its existing mesh and trail now live on a renderer child. For player shots, that child initially uses BowArrowRest or StaffCore when the anchor is finite, nearby and on a clear side of cover. It samples the completed release pose before the first render, then detaches and smoothly rejoins the unchanged projectile trajectory in 0.09 seconds. Invalid/covered anchors fall back to the simulation origin. Companion and hostile shots retain their original visual origin. The trail starts after the visual origin is assigned, avoiding a line from the temporary creation position.

Sword slashes also create a bounded, short-lived filled ribbon using both live SwordRoot/SwordTip endpoints. Its trailing edge and the broad crescent share the model's alternating swing side. Pause freezes its age; owner/epoch/result changes retire it. This is an additional visual mesh, not a hitbox or target query.

Executed validation:

- The production visual components ran with explicit managed transform and anchor substitutes: 28 assertions check actual child placement, first-frame pose recapture, timed convergence, blocked-anchor fallback, unchanged simulation transforms under nonuniform scale/rotation, both sword endpoints, left/right mirroring, alpha and epoch retirement.
- A mandatory negative control injects a write to the simulation root and must fail the root-invariance assertion. A04's mandatory old-order allocation mutation and its existing 26,386 component/vertex checks plus 22,234 geometry checks still pass normally.
- Wiring contracts, basic timing contracts, combat-review contracts and weapon-structure contracts pass.
- Full runtime sources compile against locally available **2021.3.33** references in ordinary and `UNITY_ANDROID` variants, zero warnings/errors. This does not establish Unity 6 compatibility.
- Compared with A04 commit `bf409274`, the actual `CombatProjectile.Update`, `CanLaunchFromMuzzle`, `RegisterDodge` and `AlignBodyFlight` method bodies are byte-identical. The primary Update body SHA-256 is `009347037aa41a592907daa2b28682cfae16c51e7a464787e090e2c513158ed3`.

Run `DOTNET=/path/to/dotnet python3 Tests/FilledVfxAllocationTests.py` for both production-component suites and both mutations. No additional runner registration is needed beyond A04's Python entrypoint. `python3 Tests/WeaponVisualLinksSourceTests.py` provides separate wiring checks.

The anchor provider is an explicit test substitute; these tests do not establish the appearance of the real weapon meshes or GPU trails. Exact Unity 6 API compilation, LateUpdate/render ordering in the engine, actual hit-frame footage, transparent sorting and mobile performance remain for the parent environment/device validation. No engine execution or screenshots were produced here.
