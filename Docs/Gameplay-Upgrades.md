# Gameplay upgrades — development branch

This source update is not a new Windows installer or iPhone build. Shared runtime
changes are synchronized between the two repositories. Existing platform-specific
export/build tools and project settings remain separate.

Current second-round changes and save migration: [Combat-Progression-V2.md](Combat-Progression-V2.md). Numerical budgets: [Combat-Budget.md](Combat-Budget.md).

## Playable changes

- Distance-aware Boss slam/charge/fan, half-health combinations, readable stable
  footprints and wind-up progress; real imminent-attack perfect-dodge rewards
- Shortened safe blink landing, small input buffer, obstacle/path bounds retained
- Four opening class signatures; mutually exclusive elemental Shatter/Burn builds;
  frost/meteor shatter, poison detonation, starter wolf and short summon focus
- Five mechanic equipment affixes with tradeoffs, internal proc limits, a source
  codex, first-clear selection and targeted material exchange
- Two dungeon layouts, four seeded encounter styles, environment-dependent
  wilderness populations, tier/wave-scaled groups and bounded reinforcements
- Three waves, explicit three-card blessing choices after waves one/two, six
  possible blessings with at least two useful to the current loadout
- Tier selection including cleared tiers; optional limited-healing challenge
  (three shared charges; normal mode keeps its existing healing rules)
- Optional crystal event with extra enemies, material and recovery reward
- Mastery opens at levels50/65/80/95 across four competing tracks, with one active
  core; old three-track mastery is refunded once and existing skills remain valid
- Locked items, configurable common/rare auto-sale, protected bulk sale, pending
  valuable loot and a durable recovery mailbox for full-bag exits
- Permanent weapon/armor/relic slot reinforcement, up to +10 per slot. Replacing
  equipment automatically applies that slot's rank using the new item's own base
  stats; selling or replacing an item never removes the slot's investment. Manual
  enhancement transfer is removed, and sale prices do not include reinforcement
- Legacy saves migrate once to the highest existing rank per slot across worn,
  inventory, pending and recovery items. Ranks are not summed. Original stat bases
  are retained and previously upgraded legacy items are locked for protection

## Requested presentation changes

- Compact chest cards with metal trim, gem lock, staged lock/lid/light animation,
  actual-reward-rarity colors/sounds and a skip control; identical unopened odds
- Odds/rules are optional details. Opening grants once only after a durable save;
  a persisted receipt survives restart, animation skips and repeated clicks
- Current equipped and candidate scores shown side by side with net change;
  both use the same slot rank, calculated from each item's own base stats.
  Attack/armor/health comparisons and mechanism tradeoffs remain visible
- Larger outlined combat text and real critical-hit feedback; saved text-size,
  camera-shake and reduced-effects controls in the manual pause menu
- Losing OS focus suspends safely without opening the pause dialog. Returning
  preserves manual pause/modal state and does not replay held attacks
- Pickup messages appear in the bottom-left system history rather than top-center
- Minimap terrain/river/bridge/heading/north cues and portal color match the world;
  camp facilities, practical tutorial progress, and optional battle recap

## Validation scope

`Tests/Run-CloudValidation.sh` executes independent production-logic suites and
source/API compilation, with source hashes ensuring no mid-run edits invalidate
results. `Tools/cloud-validation-environment.md` records the exact installed
Unity version and current local-IPC startup blocker.

These checks do **not** validate Unity engine import, real JsonUtility behavior,
Play Mode, render/GUI/animation/audio quality, Windows packaging, iOS signing,
or device performance. Those remain release gates. No installer/IPA is included.

## Engine acceptance checklist (not yet run in this cloud executor)

1. New/legacy saves for all classes: starter ability, rank-three skills, multiple
   slots, camera rotation, jump, hotbar moves and touch controls
2. Boss attacks at close/middle/far range, blocked charge paths, half-health
   combos, interruption, true dodge rewards once and no empty-ground farming
3. Every layout/seed: reachable spawns, spacing, safe arrival radius, caps,
   wave reinforcements and optional crystal reward only after both guards die
4. Blessing selection: open/cancel inputs/repeated confirmation/zone exit/death;
   next wave must require explicit confirmation and only one choice applies
5. Chest UI on 1080p and small/mobile layouts: open every rarity, gold-only and
   duplicate fashion; details click-through, skip, Esc, reload and write failure;
   no extra roll or grant on a repeated event
6. Slot reinforcement and equipment comparison: empty/identical/equal/worse/
   level-locked/locked/mechanic items; preview must exactly match automatic
   reinforcement after equip. Repeated swaps cannot compound stats or heal;
   upgrading from an unequipped selection trains the slot and currently worn item
7. Full inventory + full pending queue: pickup, death, respawn, title, app quit,
   recovery claim, save failure and retry; no silently lost valuable equipment
8. Alt-Tab and app suspension during combat, manual pause, reward and blessing
   dialogs; no automatic pause menu or accidental unpause
9. Actual critical and normal hits, delayed skill/projectile hits, DOT and summons;
   readability, density cap, large numbers and all accessibility controls
10. Limited-healing mode at zero inventory potions/zero charges; targeted skills
    canceled before casting; wave/objective refill caps. Normal healing unchanged
11. Reward totals/materials/mastery caps; migrate differing old reinforcement ranks
    from inventory/pending/recovery, preserve the highest per slot and original
    bases, then save/reload repeatedly without re-importing item ranks. Verify
    real Unity JsonUtility and build each platform before distribution
