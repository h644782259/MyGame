# PR17 environment review corrections

`WorldBuilder.BuildWaterSurface` now calls `BuildShoreWaterContact` for the existing brook, courtyard and tactical-water entries. It shares `RibbonSection` with the actual water ribbon so the static two-bank damp seams and short vertical waterlines follow the rendered edge, including corners. Two merged geometry objects share one material, with at most 65 authored sections; there are no new Update components, colliders, lights or navigation writes. The existing three depth bands, moving current and bridge damp/wear strips remain connected. This adds shoreline contact geometry; it does not simulate changing water levels or prove its rendered appearance.

`ShoreContactProductionTests.py` executes the actual three entry slices and production water/ribbon/resource/traversal generators. It checks geometry and lifecycle with managed engine substitutes; deleting the shoreline call must compile and fail the exact entry assertion. `WaterFlowProductionTests.py` and existing water source contracts retain their separate behavior checks. The shoreline regression is now a default cloud-validation entry.

The companion route correction clears skill-specific success/failure feedback when selecting another route skill, while preserving the workshop return scroll. See `Growth-Navigation-B08.md` for the production replay and exact old-behavior controls.

These corrections are integrated in an independent branch from the held PR17 head. Full aggregate and platform synchronization are deferred to the parent task. Unity 6, rendered shoreline quality and physical touch remain unverified.
