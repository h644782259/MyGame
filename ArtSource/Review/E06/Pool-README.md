# Bounded short spell reuse

FilledSkillVfx now reuses at most eight inactive roots and their existing renderer/filter/Piece slots (maximum fourteen per root). Active effects remain governed by the existing 12/20/32 global lease caps. This targets repeated short impacts, slices, contacts and beam bodies; an externally destroyed charge still destroys its own object normally. It does not claim every VFX emitter is pooled.

Retirement removes finale registration, releases the lease, hides all children, clears owner/session/epoch/time/tint/priority/parent and each part's alpha, mesh, material, timing and motion fields. Reuse starts with a new lease and current session. A changed session invalidates a live effect even if the same player object is retained. Subsystem reset destroys active and cached roots before shared source assets.

Immutable authored/procedural meshes and the material retain their cache lifetime. Per-cast cover-clipped meshes are destroyed when their cast retires and never returned as shared geometry. Reusing a root does not imply reusing stale clipping.

`Tests/FilledVfxPoolProductionTests.py` executes the actual loader, decoder, effect, cover clipping and lease with managed Unity API boundaries. It runs 90 post-warmup cycles across quality levels and priorities, plus ownership/session/epoch/cap/reset cases; 4,353 assertions and three compiled negative controls passed at the recorded snapshot. Object/renderer counts stay constant after warming all exercised compositions. This is not Unity allocation profiling, Frame Debugger, GPU/FPS or device evidence. Per-cast clipping still allocates and retires meshes; no zero-allocation claim is made.
