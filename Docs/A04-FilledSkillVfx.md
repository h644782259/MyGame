# A04: primary skill volumes and visible-side allocation

This change is cosmetic. Damage events, targets, hit scheduling, line-of-sight policy and collision roots are unchanged.

The prior mobile budget admitted ten repeated ice/fire ornaments before the two ground bases were requested, so both bases were rejected by the actual ten-part cap. `Impact` now requests landing base, primary silhouette and contact flash before ornament requests. Mobile normal keeps at most ten parts; reduced keeps seven. Reusable closed meshes provide a ridged sword with guard/grip and rupture base, a zigzag lightning trunk with branches, and an arcane cubical lattice that compresses, expands, releases independent broken struts and fades. The falling-blade entry now uses the sword recipe; lightning/arcane no longer route to summon/ice. Existing charge envelopes still provide pre-hit preparation, and impact volumes start on the existing hit event; no decorative delayed arrival is substituted for hit timing.

For covered areas, a bounded deterministic search first tries the requested position and clear directions at full size, then progressively smaller footprints. The root remains at the original hit location; individual cosmetic volumes may move into a visible sector within that area. The center ray must be damage-visible, and the destination must fit the full animation envelope. Once placed, meshes retain stable visibility instead of toggling the entire renderer from a changing bounds circle each frame. Fully obstructed candidates are omitted. Newly emitted covered particles receive a visible-side position and constrained horizontal drift before rendering; old blocked particles expire instead of teleporting. These are cosmetic placement choices, not changes to damage coverage.

Validation executed on this machine:

- `DOTNET=/workspace/scratch/dotnet/dotnet python3 Tests/FilledVfxAllocationTests.py`: actual production `FilledSkillVfx` and `CoveredAreaParticles` execute with explicit managed scene/transform substitutes; 26,386 allocation/animated-vertex/coverage/phase assertions and 22,234 mesh/lifetime assertions passed. Mandatory negative control moves the real base/primary/contact `Add` requests after ornaments and must fail the retained-base assertion. Synthetic wall geometry and numerical transforms are not a Unity rendering test.
- `python3 Tests/FilledVfxSourceTests.py`: 18 wiring contracts passed.
- `SkillVisualRecipeTests.Run()`: 384 recipe identity assertions passed.
- All runtime sources compiled against the locally available **Unity 2021.3.33 reference package**, zero warnings/errors. This is only a legacy compatibility check; it is not exact Unity 6000.6 compilation.

Exact Unity 6 compilation, shader/GPU rendering, mobile draw-call/fill-rate behavior, visual phase readability, safe-area footage and actual device performance remain unverified here. The parent task owns exact-version compilation in its separate environment. No screenshots or gameplay observations are claimed.

Default runner registration (parent owns `Tools/cloud-validation.py`): add a `run_check` for `Tests/FilledVfxAllocationTests.py`, passing `dict(env, DOTNET=dotnet)`. The script compiles its production dependencies and runs its own old-order negative control. Existing `filled-vfx` and `skill-visual-recipe` registrations retain their dependencies.
