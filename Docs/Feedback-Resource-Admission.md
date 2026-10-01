# Combat text admission and log views

Both session text callers previously checked a count of 48 even though the text
renderer accepts only 20 mobile or 36 desktop numbers. At the actual limit, an
ordinary hit therefore created a GameObject and component that initialization
immediately rejected and destroyed.

The shared `FloatingNumber.Spawn` factory now evaluates global capacity and local
lanes before creating anything. One read-only scan chooses the oldest ordinary
replacement for an incoming critical, excludes it from prospective local
occupancy, and keeps the existing four ordinary / six critical local thresholds.
Only a fully accepted spawn retires the replacement. In particular, a critical
rejected because its origin is crowded cannot erase unrelated ordinary text.
The selected replacement, lane and camera stay local to the immediate call; the
factory does not repeat the scan or retain a plan across frames. Public
`Initialize` remains supported and uses the same policy. Registration, retirement,
count decrement and destruction semantics remain centralized in their existing
paths.

`SystemMessages` now caches its read-only collection wrapper once per session.
Readers keep the same live view as messages append or the oldest entry is evicted
at the existing 32-entry limit. Collection mutation remains disallowed.

`FeedbackAllocationSourceTests.py` checks the creation/admission order, shared
callers, local limits, replacement semantics and cached wrapper. The isolated
`FeedbackValidation` fixture prepares actual engine assertions for both display
limits, critical-only saturation, rejected-spawn object counts, local lane reuse,
non-destructive failed replacement and the read-only live log. It requires a quiet
scene and restores its mobile simulation and log changes. The dense-rejection
case starts with six critical entries together, then adds ordinary entries far
away; replacing a distant ordinary entry cannot free a local lane. This is
separate from the mixed ordinary/critical lane-limit case. These are prepared
checks, not executed Unity memory measurements or PlayMode results. Source/API
validation cannot establish frame-time or native allocation performance.
