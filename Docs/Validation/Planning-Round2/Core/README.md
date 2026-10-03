# Round 2 — Offense mastery core

Baseline: Windows `5d85e47b9fab2489d5b06963a0b896ec19112740`.

## Behavior

- First/enhanced payoff now 0.6/1.0 × CombatAttack, explicitly noncritical. The existing 6-second opportunity and 6/4-second internal cooldown remain unchanged.
- A single ready charge, using the existing monotonically bounded cast receipt: repeated hits/targets/ticks from one cast cannot rearm; first contact during cooldown is discarded. New legitimate skill hits can refresh the single six-second opportunity; no charge banking.
- Desktop basic-action heading and mobile attack-button bottom label show `连击就绪`. Existing attack failure presentation remains higher priority on desktop, separate from the mobile readiness bottom label.
- Confirmed payoff emits one restrained `连击兑现` text and `RecordCombatAction("破敌兑现")` only if enemy HP actually falls. It directly calls noncritical `TakeDamage`, never skill/status/area reaction resolution.
- A target already killed by the triggering basic strike no longer consumes readiness without any payoff; readiness remains for the next living target within the original window. A living target rejecting damage consumes that attempt, but reports no actual payoff.
- Generic `HitArea` with no explicit cast identity (dodge shock / defensive passive) cannot count as a skill hit. Actual skill areas preserve their explicit cast identity. Damage, radius, and timings of those areas are unchanged.
- No profile fields/save migrations, paid assets, balance changes to other cores, or runtime asset changes.

## Reproduce

Use a writable `DOTNET_CLI_HOME`, e.g. `/tmp/core-dotnet-home`.

```
python3 Tests/MasteryComboRound2Tests.py /workspace/shared/emberfall-tools/dotnet/dotnet
python3 Tests/DesktopOpportunityHotbarProductionTests.py /workspace/shared/emberfall-tools/dotnet/dotnet
python3 Tests/StatusFeedbackProductionTests.py /workspace/shared/emberfall-tools/dotnet/dotnet
python3 Tests/PlayerFinaleTailProductionTests.py /workspace/shared/emberfall-tools/dotnet/dotnet
python3 Tests/ProgressionGrowthSourceTests.py
python3 Tests/CombatOpportunitySourceTests.py
python3 Tools/validate-meta-guids.py
```

`ProgressionGrowthTests.Run` was also compiled/run against the actual progression/core dependencies using the registered `progression-growth` source set and production `write_project` helper (2204 assertions). The full integration run should use the repository's existing cloud validation workflow.

Register `Tests/MasteryComboRound2Tests.py` with the standard dotnet argument in the integrator's cloud runner; this work package intentionally does not edit its shared registration file.

## Evidence and limits

- `mastery-combo.log`: 241 production state/payoff/HitArea assertions; compiled baseline coefficient and passive-area negative controls fail their intended assertions.
- `desktop.log`: 37 actual managed DrawHotbar observations, plus existing negative controls.
- `status-feedback.log`: existing actual poison/frost/burn chain and three negative controls. Its standalone non-core fixture supplies a no-op core settlement; the new dedicated suite exercises real settlement.
- `finale-tail.log`: 154 production finale/area lifecycle assertions and two negative controls.
- `progression-runtime.log`: 2204 mastery/refund/ascension/tier assertions.
- Source contracts and GUID audit pass in their named logs.
- `desktop-attempt1-environment.log` is the preserved first failure: .NET tried the read-only default `/home/agent/.dotnet`. Re-run with a writable temporary CLI home passed; no permission change.

These are managed production-method tests with explicit engine boundaries and source contracts, not Unity execution. Rendering, mobile label overlap/readability, physics, input/device behavior, actual Unity import and Windows/iOS/Android player builds remain unverified. The integrator owns synchronized platform commits and full API compilation.
