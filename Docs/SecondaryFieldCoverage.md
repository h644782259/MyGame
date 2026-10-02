# Stable secondary elemental fields

`ElementalFieldVisual` previously rotated every ground-field emission point over time, then switched each entire bubble/strand on or off using a swept `.55` radius from the field center. When the center was closer than `.55` to a wall, that sweep could reject every piece even when a clear sector remained.

Ground fields now choose deterministic visible-side placements once, reserving each piece's maximum sway/jitter and width. Coverage checks use the existing center line-of-sight and a full footprint at the destination. The field origin no longer orbits over time. Poison still rises/pulses, fire still sways and lightning still branches, within those local envelopes. Lightning's top remains above its placed footprint rather than drawing a diagonal back toward the potentially obstructed center. The original on-body aura behavior is preserved.

The cached plan refreshes when the field transform or `WorldTraversal.Revision` changes. During refresh, each still-valid placed envelope retains its local position and scale: removing nearby cover or changing unrelated traversal geometry no longer teleports a safe piece back to its original preferred point. Invalid placements are replanned; previously omitted pieces may appear when space becomes available. A static field makes no repeated coverage queries merely because its animation advances. Replanning after an actual geometry/transform change can still add, remove or relocate cosmetic pieces; this change does **not** promise smooth visibility under every dynamic obstacle configuration. Fully blocked fields remain omitted. Damage and LOS rules are unchanged.

Validation executed:

- `FilledVfxAllocationTests.py` now executes the actual `ElementalFieldVisual` component with managed Unity substitutes: 6,378 assertions covering all three field elements, 120 animation samples per element, actual strand points plus width, growing bubble extents, stable visibility, no repeated queries on static frames, revision refresh, fully blocked fallback and on-body regression.
- A mandatory negative control reinstates the former rotating `.55` sweep/toggle. The production near-wall test then fails with `wall-adjacent field must keep visible secondary pieces`.
- The existing A04 allocation/geometry suite, weapon-child invariants, and both earlier mutations still pass. Hazard/effects and filled-effect source contracts pass.
- Full runtime legacy compatibility compile against Unity 2021.3.33 references: zero warnings/errors. No exact Unity 6, GPU, shader, mobile-performance or screenshot result is claimed.

The parent runner already invokes `Tests/FilledVfxAllocationTests.py`; no registration change is required. A04 coverage remains limited to its impact-placement path. Crescent, Charge and Thrust retain their prior unreserved cosmetic footprints; this secondary-field fix does not extend coverage guarantees to them.

## Safe-placement continuity regression

`Tests/ElementalFieldContinuityTests.py [dotnet-path]` executes the production component using the existing managed Unity substitutes. With animation time fixed, a half-plane wall at x=.25 first forces alternative placements. Removing that wall previously teleported Fire piece 0; the unmodified production component failed the new rendered-point assertion before the fix. The suite now passes 139 assertions across fire, lightning and poison, including retained strand points/bubble positions and scales, unrelated revisions, zero extra static queries, transform-only invalidation/restoration, newly blocking cover and later reopening. Its mandatory negative control removes the retention branch and must fail that same rendered-point assertion.

The prior allocation suite also passes (22,234 recipe, 26,386 allocation/vertex, 28 weapon-link and 6,378 secondary-field assertions), including its three existing negative controls and on-body aura regression. Tests model analytic wall coverage; they do not constitute Unity 6 engine, real physics, GPU or device acceptance. This fix deliberately adds no interpolation: a newly obstructed existing position can still move or disappear immediately.

Parent runner registration: add a Python check named `elemental-field-continuity` invoking `Tests/ElementalFieldContinuityTests.py` with the dotnet path as its first argument (or `DOTNET` environment variable). No runner file is changed in this commit.
