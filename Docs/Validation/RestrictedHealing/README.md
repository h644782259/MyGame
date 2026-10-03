# Restricted healing skill budget

Scope: `ChallengeRun && InDungeon` at skill release. Slot 6 heals the player for 60% / 70% / 80% maximum health at ranks 1 / 2 / 3 over the existing five one-second pulses (first pulse at one second, last at five seconds). Mode and rank belong to the released sequence; no instant heal was added. Per-pulse maximum health still uses current stats, as before: RefreshStats does not restart the sequence, and Heal clamps at current maximum health.

Normal mode self healing and Summoner companion healing retain 30% / 42% / 55%. Potions retain immediate 50% healing. Skill energy, cooldown, one healing charge, higher-rank defense and rank-three final energy return retain their original rules. No additional healing stack, refresh or charge mechanic was introduced.

Restricted mode rank one refuses a cast before charge/energy/cooldown commitment if the player is full and there is no eligible injured companion that this spell can heal. Only Summoner slot 6 heals companions. The readiness scan uses precisely the original HealAll eligibility: non-null, IsAlive and same Owner. Actual IsAlive excludes dead, withdrawn, inactive owner, wrong current session player and stale combat epoch. High ranks can still spend at full health to acquire their existing defense. Normal-mode full-health cast behavior is unchanged.

Changes:

- AdvancedSkillSequence: snapshot restricted-mode flag; use increased self-healing fraction only.
- PlayerController: restricted rank-one empty-benefit rejection before payment.
- SummonedCompanion: read-only injured-target query sharing HealAll qualification; HealAll unchanged.
- GameTypes: skill evolution text names both ordinary and restricted budgets and unchanged companion amount.
- Existing mobile managed fixture receives harmless default full-health / ordinary-mode fields for the new production dependencies. Its original tests still execute.

Verification from this checkout:

```sh
python3 Tests/RestrictedHealingProductionTests.py /path/to/dotnet
python3 Tests/MobilePinnedTargetProductionTests.py /path/to/dotnet
python3 Tests/BalanceIntegrationSourceTests.py
python3 Tests/CombatTimingSourceTests.py
```

`production.log`: 612 assertions executing full CastSkillCore and SkillRuntime, production sequence Spawn/Configure/Update/Healing, Heal, RefreshStats, HealingProtection, charge spending, DrinkPotion and companion IsAlive/HasHealingTarget/HealAll. Covers all four hero classes, all ranks, both modes, five-second timing, no instant healing, original companion budget, cooldown retry, insufficient resources, full/partial health, legal/illegal pets, higher-rank pre-use, pause, zero/negative dt, death, epoch, owner replacement, combat end, session stop, rank/mode/stat refresh and potion payment. Four compiled mutations deliberately fail for old self budget, missing empty-cast guard, increased companion budget and missing death retirement. Expected exception traces are retained in the raw log.

The managed fixture reuses the mobile cast harness. VFX, non-healing attacks, terrain queries, equipment appearance and companion build refresh are explicit boundaries. SummonerSpell dispatch is a boundary forwarding slot 6 to the real sequence, matching the unchanged production dispatch. The actual RefreshStats method executes; appearance/mastery setup are lightweight doubles. No Unity scene, shader, physics or device execution is claimed.

`mobile-regression.log`: original 56 pointer/aim/targeting/charge assertions plus compiled negative controls. `source-regression.log`: 32 balance integration +29 timing source contracts. `api-compile.log`: all runtime C# sources compiled with pinned Unity 2021.3.33 reference APIs for Windows, iOS and Android defines; zero warnings/errors. This does not build platform players or validate Unity rendering. The full aggregate is intentionally deferred to parent integration; this suite is registered in Tools/cloud-validation.py.

No repository AGENTS.md, .instructions or local skill files were present in this authorized checkout. No remote operation was performed.
