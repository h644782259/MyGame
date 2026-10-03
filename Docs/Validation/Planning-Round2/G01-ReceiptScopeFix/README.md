# G01 review — scope result suppression to owner and combat epoch

The suppression receipt now resets when owner or epoch changes, before observing the new scope's first frame. Companion result sequences restart at one for a new bond/epoch, so a previous scope's suppressed `EmpoweredHit/1` must not suppress a new scope's genuine first event.

If a scope's first observed frame still carries a nonempty old result, the usual transition rule suppresses that result in the new scope. If the first frame is empty, a later real result numbered one remains visible. Target changes within the same scope retain suppression behavior.

Evidence:
- `receipt.log`: 29 actual production UI query/runtime receipt/channel assertions. Covers epoch1 receipt1 → pause → epoch2 empty frame → genuine receipt1 visible; different owner with reused number visible; old result carried on first epoch/owner frame remains suppressed; a later new receipt becomes visible. The earlier unseen-receipt → pause → resume cases still pass.
- Compiled negative controls separately restore the old last-rendered cache and remove scope reset. They fail the intended resume/reused-receipt assertions, respectively.
- `channels.log`: 224 production opportunity/result-channel assertions and three existing negative controls pass.

Only the presentation channel changes; no combat, persistence or lifecycle events are written. Existing registered tests were extended, so no new runner registration is needed. These are managed production-method checks, not Unity/device execution.
