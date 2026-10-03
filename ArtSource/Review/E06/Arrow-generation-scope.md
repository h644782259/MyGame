# Arrow batch rental-generation regression

Production fix: `f41b547`; test commit: `d2937d526aba3b89ff7b394fed0fb3104721f66e`.

`ArrowBatchGenerationProductionTests.py` extracts the actual `AdvancedSkillSequence` retained fields, `Configure`, `Update`, Ranger skill-9 event body and `OnDisable` unchanged. A persistent fixture supplies the omitted scheduler context and unused skill branches. The actual `FilledSkillVfx`, `ArrowBatchHandle`, resource decoder, authored buffers, anchored clipping, visual leases and pool execute. Unity scene/API behavior and damage recipients are managed doubles; this is not Unity rendering, frame performance or device acceptance.

A real priority lease eviction retires an old sequence's batch, then the identical pooled component is rented to a new arrow/fire cast. Coverage includes matching owner **and cast ID**, different owners, room epochs and sessions. Advancing/disabling the retained old sequence and invoking its old handle or lease callback must leave the new rental's snapshot unchanged. Snapshot coverage includes component state, generation, ownership references, part state, mesh contents/bindings/destruction, transforms, renderer alpha/material/activation, lease state and finale membership. Current handles still support normal Retire/Dispose; invalid room/session handles cannot mutate an effect before normal Update cleanup.

The 114 assertions pass. Removing only the handle's generation condition reproduces corruption via the actual old sequence Update despite matching owner/cast ID. Removing only the lease-callback generation condition also fails the new-rental snapshot. The three adapted existing suites pass with their existing mutation controls.

Raw outputs (including compiler warnings about unused extracted sequence fields) are retained unchanged:

- `Arrow-generation.log`: new production-chain test and both generation negative controls.
- `Arrow-generation-filled-allocation.log`: allocation, animated bounds/coverage and three negative controls.
- `Arrow-generation-combat-readability.log`: visual batch/anchor/priority coverage and five negative controls.
- `Arrow-generation-finale-tail.log`: actual finale producers and two negative controls.

These logs precede the parent's final full-suite freeze; they support this focused fix rather than replacing that validation.
