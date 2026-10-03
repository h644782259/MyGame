# Status and healing identity fixture inputs

Base: `13ffda9` frozen combined candidate. Only Tests/StatusFeedbackProductionTests.py and Tests/SkillIdentityCallsiteProductionTests.py changed; production and registry are untouched.

Original full-run errors were copied unchanged from complete-final-v2-frozen/Tests/TestResults/Cloud-Latest into status-before.log and identity-before.log. They report missing ReturningCounterVariant and restrictedHealing in extracted-method hosts, respectively. The full run was not stopped or modified.

Status repair: ReturningCounterVariant, mechanic ownership and nearest bounce recipient are explicit fixture inputs. Actual OnBasicAttackHitTarget executes for both variant values. Ordinary returning blade still dispatches 110 damage and grants 1.8 seconds of heavy counter; the counter variant dispatches neither legacy bounce nor legacy counter grant. Existing actual EnemyStatusEffects, scheduled ticks, poison consumption, aura retirement/reapplication, burn/frost feedback, pause, epoch and death assertions remain. All pass; both original compiled negative controls plus a new removed-variant-exclusion control fail at their intended assertions.

Identity repair: HealingProbe receives a restricted-mode boolean. The actual production Healing body executes with real authored ProtectionCage resource loading for four classes × ordinary/restricted mode. Rank-three self pulse is 11 / 16 health at max100, final energy return remains8, Summoner companion fraction remains11%, other classes do not heal companions. Actual charge lifecycle and actual lightning/contract damage confirmation tests remain unchanged, as do their three negative controls. Full admission and five-second timing remain covered by RestrictedHealingProductionTests; this probe tests the event callsite and identity.

Targeted commands:

```sh
python3 Tests/StatusFeedbackProductionTests.py /workspace/shared/emberfall-tools/dotnet/dotnet
python3 Tests/SkillIdentityCallsiteProductionTests.py /workspace/shared/emberfall-tools/dotnet/dotnet
```

Both exited successfully; raw outputs are status-after.log and identity-after.log. Existing fixture unused-field warnings are retained. Source hashes accompany the logs. No aggregate or platform build was launched; managed source/geometry checks do not represent Unity runtime or device validation.
