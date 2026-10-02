# Read-only reward/save/return audit

Source candidate: `d1e1606ccddec402862141346f6fefa364f77d87`. No reproducible player-facing regression was identified in this bounded source trace, and no fix branch or runtime edit was made for it.

- Chapter mastery evidence, rewards and receipt sequence enter the same candidate save in TryCompleteChapterNode; only successful commit publishes that profile. Failed settlement retains the original attempt and does not mark reward claimed.
- EnterNextRoom checks the deferred blessing and input gate before SaveBeforeLeaving. Successful save precedes room advancement and combat epoch retirement. ChangeZone also saves before replacing world state.
- Side-event reward failures remain queued; transitions and SaveAs first retry pending settlement. Runtime supply application also requires the originating player and combat epoch.
- Expedition SaveAs intentionally continues the same live adventure: service/player/room ownership remains, and the save path switches only after the new snapshot is written. Completed receipts are copied in that snapshot. An unfinished or reward-pending chapter explicitly rejects SaveAs because its chapter receipt binds SavePath.
- PlayerController.TakeDamageFrom records incoming attribution before subtracting HP and invoking OnPlayerDied, so the result capture sees the current lethal hit. A nearby older comment was not treated as behavior evidence.

This is a source-call-chain audit, not newly executed Unity, device or real disk-failure verification. The prior PR23 scratch probe is not reused as exact PR24 execution evidence; the maintained first-room production test is now registered in the PR24 aggregate run.
