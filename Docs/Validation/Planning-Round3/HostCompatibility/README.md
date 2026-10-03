# Frozen v2 UI host compatibility

Base: b853f84. The frozen `/workspace/scratch/planning-round3/validation-v2` tree was read only. `*-original-failure.log` preserves its raw full-suite failures.

All three failures were C# compilation failures in narrow test hosts, not observed runtime failures:

- ChestPauseBackProduction extracts the whole actual GameUI.Update. Its SessionStub lacked the RoomBranchChoiceOpen/CancelRoomBranchChoice session boundary newly referenced by that method. The fixture now exposes that boundary, and additionally verifies that actual Update cancels the branch before chest navigation without clearing the chest or its input release gate.
- SkillPanelNavigationProduction and MobileInventoryBackProduction extract actual ClosePanel. Their partial GameUI fixtures lacked presetSaleOpen and CancelPresetSale. Both now compile the actual production CancelPresetSale method from GameUI.BuildPlans.cs, and verify that its first Back cancels the sale while preserving the detail/blocking, followed by the ordinary detail-to-list Back.

Only three test files changed; production UI code and game behavior were not changed. Existing assertions remain, including the mandatory compiled old inventory-entry negative control.

Commands use `python3 Tests/<suite>.py /workspace/shared/emberfall-tools/dotnet/dotnet`. Final raw logs are `chest-fixed.log`, `skill-fixed.log`, and `inventory-fixed.log`. All three exit 0. These are managed source-host tests with explicit engine/session boundaries, not Unity execution, touch input, rendering, or device acceptance.
