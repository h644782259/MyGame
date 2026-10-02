# Explicit first-room choice integration probe

Passed 90 actual host/state/RunChoices/pause/transition assertions across six layouts, including B-first completion, same-frame deferral, next-frame offer, blocked exit before and during choice, valid/invalid/paused/duplicate confirmation, and one saved epoch transition. Both compiled negative controls fail their intended runtime assertion. This separate probe is not included in the 191 aggregate checks.

The probe uses actual full RunChoices/Rooms/ApplicationPauseState and extracted production InputBlocked, UpdateTimeScale, ConfirmRoomInterlude, room host and SaveBeforeLeaving. Unity scene and save services remain explicit doubles. This is not device gameplay or real save IO.

`report.json` records every source hash, exact tested commit and equivalent rebased delivery commit. `frozen` preserves the inputs. To rerun `probe.py`, point its `repo` variable to the repository checkout containing the pinned commit and its `dotnet` variable to an installed .NET 8 SDK; execute in a writable directory. The generated projects and CLI cache are intentionally excluded from review artifacts.
