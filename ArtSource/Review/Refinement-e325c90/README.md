# Refinement candidate evidence

Candidate commits: Windows `e325c904ab5ee1a45aaf354cbaf3556677b407d9`, iOS `44d24269470076010c626465c75630da18a58b51`, Android `57445429957bbc2c65a471e7ae5affeb9df841c3`. This independent review branch adds evidence to the Windows candidate; its tip is not a different runtime candidate or an additional engine-tested build.

Final result: **189/189 passed**, default JIT, with no source changes during execution; all three API compilations passed. Explicit Redrock checks passed: 3,232 spawn-isolation assertions, 51,209 geometry/navigation/spawn assertions and 780 formation-navigation assertions, with both compiled negative controls.

The authoritative aggregate result and source hashes are in `report.json`; `handoff.json` maps the frozen tree, final platform heads, API compilation inputs and resource parity. The aggregate is one shared-source run, not three independent full runs. Separate Windows/iOS/Android compilations use cached Unity 2021.3.33 reference APIs. They are not Unity 6 Editor or platform builds.

`redrock-explicit.log` records the explicitly executed, separately counted `Tests/RedrockReplayGeometryTests.py` on the final candidate: actual room-zero spawn isolation, production geometry/navigation and compiled wall/low-bit controls. The script is not registered in the aggregate runner. Earlier scoped geometry/host logs and the exact returned-route illustration are retained as supporting evidence, not additional unique aggregate checks. `redrock-export.log` belongs to the final source-hash refresh; the standalone JSON/SVG and reproducible exporter live in `../Redrock-Replay`.

The normal-distance images under `Combat-Distance` use identical native Blender camera, stage, lights, pose and unchanged material. The authored hero projects to approximately 44.2×62 pixels at the default camera distance. Joint/cloth details are not reliably distinguishable there, and these images do not establish a meaningful combat-readability improvement. Close-view source comparisons and exact rig/FBX checks are under `../../BlenderPilotRefinement/Review`. The optional authored model remains default-off and retains its restricted equipment/action support and procedural fallback.

No Unity 6 Editor import, shader rendering, real JsonUtility/PlayMode, device input, gameplay feel, platform build, memory residency or performance acceptance is claimed. The raster route map is an Inkscape rendering of the production-derived SVG, not a game screenshot. No source image retouching, camera zoom or cropping was used for the native combat-distance comparison.

The older interrupted `36eebf5` run and `951800d` run are historical diagnostics only. The latter reported 188/189 because an obsolete return-clock fixture injection no longer matched its shared fixture; the final candidate reuses the existing failure seam and retains both compiled negative controls. Those earlier outcomes must not be presented as complete passes.
