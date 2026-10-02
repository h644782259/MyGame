# Finale retention and boss sweep pilot

D01 fixes the priority inversion where a cast's per-target burn contacts used the same priority as its main finale. Cash contacts now remain real-contact priority; newer main finales can still replace older mains. Elemental ultimate impact meshes register their cast and epoch; actual direct/cash HP loss confirms that impact. Confirmed elemental finales may finish a visual-only tail for at most 0.65 unscaled seconds after ModeFinished (also bounded by original lifetime). Other effects retain their existing terminal cleanup. Owner death/change, epoch/scene change, title exit and explicit boss-result skip retire immediately. No damage or reward callback exists in the terminal visual update.

D02 represents the danger to a player's center as the existing 0.55 beam half-width plus 0.40 body compensation. Both capped warning boundary and damage query use that 0.95 capsule. Occlusion clips the whole capsule before a solid, including near-tangent pillars and endcaps; it intentionally stops the whole beam at the first full-width obstruction rather than painting independently translated center-ray edges. Exact circle/rectangle segment clearance supplements the existing ground/water policy. Ordinary movement and navigation queries are unchanged. Fully obstructed origins suppress warning and damage.

## Executed evidence

- `Tests/DenseFinaleProductionTests.py`: production ElementalAdvancedArea, EnemyStatusEffects, burn receipts, FilledSkillVfx and shared leases execute together; 40 targets under cap 12, live boss / final cash kill / boss first with guards alive; 15 checks plus compiled old-priority negative control. Enemy HP/terminal callback and Unity objects are managed substitutes.
- `Tests/EffectPriorityProductionTests.py`: 164 actual emitter/lease/lifecycle checks, including the production RecordBurnCash bridge; 5 compiled negative controls.
- `Tests/BossSweepCapsuleProductionTests.py`: 231 production clipping/containment/contour/traversal assertions; compiled old center-ray control fails capsule safety.
- Existing burn-finalization regression: 118 checks (including actual direct-lethal visual confirmation), 2337 scheduled-tick checks and 7 compiled negative controls.
- Existing boss shutdown: 41 managed lifecycle/channel cases and scaled-clock negative control.
- Runtime C# build against cached Unity 2021.3.33 API references: zero warnings/errors; supplemental compatibility evidence, not the required Unity 6 Editor.

No Unity Editor/PlayMode, real render, touchscreen or device acceptance was available. Geometry assertions establish conservative safe boundaries, not perceived warning readability or frame-time performance. Final art/device acceptance remains pending.
