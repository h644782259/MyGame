# Reward revision — managed production validation

Baseline: Windows main `5d85e47b9fab2489d5b06963a0b896ec19112740`.

## Delivered behavior

- Ordinary ruin choices 0/1/2 are Weapon / Wings / Supply. Directional chests retain absolute fashion odds (Common22, Rare12, Epic5, Legendary1, none60 percent); a fashion drop is restricted to the selected slot. Duplicate collection/conversion stays unchanged.
- Supply skips fashion; multiply the original integer gold roll by 3 then divide by 2 (floor). The original currency cap applies after the reward; no other gold budget changes. Each chest grants one base fashion thread. Completion fragments remain in their original completion transaction.
- `ChestReward.rulesRevision`: missing/0 is the historical three-identical-box rule. New draws are1. Existing saved receipt contents are never reinterpreted/re-rolled, including historical choice2 fashion drops. Both rule panels explicitly label old results and scope current rules to newly opened chests.
- Failed in-process writes retain a private roll (id, choice, gold, rarity) for the same save path / clear count / pending tier. Retry, including a reload in the same service, reuses it; switching boxes is refused. NewGame and successful slot changes clear unpublished rolls; returning to a different role cannot inherit a choice lock. Rewards/UI/events remain unpublished until save succeeds. A process restart after a wholly failed write cannot preserve an unwritten draw; no reward was committed in that case.
- Independent `chapterDifficultyRewardMask` has two bits per node (Hard, Heroic). Each successful first difficulty transaction adds4; six bits cap these extra rewards at24. Base/repeat fragments and node-first1 stay independent.

## Historical migration decision and evidence

The original `ChapterProgression.CanEnter` required difficulty <= highest completed +1; `TryBeginChapterNode` and `TryCompleteChapterNode` both enforced it. Baseline `Tests/ChapterProgressionTests.cs` rejects early Heroic and proves per-node Hard→Heroic unlocking. Thus a valid highest Heroic record proves Hard+Heroic; Hard proves only Hard. A completed-node bit plus a legal recorded highest value is required. Missing arrays/entries, missing completed-node bits, and illegal values do not establish high-difficulty progress. Illegal values formerly clamped upward are now normalized to Normal rather than manufacturing Heroic evidence. Unknown slots can earn their first bonus on a later real completion.

`chapterDifficultyRewardRevision` gates migration. `LoadSlot` writes a detached migrated profile before selecting/publishing it; failure returns without changing the live profile, disk reward state or events. No migration runs while merely listing saves. New characters start revision1. Backup recovery follows the existing storage guards; migration uses the same atomic writer. Existing 999999 fragment cap remains in force.

## Reproduction and evidence

Run `python3 Tests/RewardRevisionTests.py /workspace/shared/emberfall-tools/dotnet/dotnet` (offline .NET8). This compiles copied production source with the established managed Unity/JSON fixture, writes only isolated temporary save directories, and includes:

- 1514 new production assertions: all directional probability inputs, all supply gold inputs, duplicates, failed-write frozen draw, old receipts/rules, each of six first clears, failures/retries/replays/reloads, legal legacy maxima, malformed/missing evidence, partial arrays and backup recovery.
- Existing chapter transaction212, chapter presentation917, adventure103, save idempotence149, upgrade480 assertions; existing mode rewards also execute.
- Exact baseline `OpenDungeonChest` from pinned Git commit substituted in isolation; the new supply assertion fails on its fashion output.
- Negative mutations: remove supply multiplier, remove difficulty bonus, remove the six-bit paid guard. Each is rejected with raw failure output in `reward-revision.log`.

Initial SDK attempts targeted its default read-only user home and failed before compilation; logs retained. Writable `DOTNET_CLI_HOME` fixes this without permission escalation. An initial duplicate fixture used legendary roll0 while expecting common; corrected fixture to common roll18, with original failure retained. Earlier passing logs are historical snapshots, not final acceptance.

No Unity editor/player, real Unity JsonUtility, rendering, touch, platform build, APK or device run is claimed. Desktop/mobile layout and long rules text still require visual acceptance. Root integration owns full-suite/Win-iOS-Android API compilation and cross-platform synchronization. Register `RewardRevisionTests.py` as one cloud production check; this branch intentionally does not modify `Tools/cloud-validation.py`.
