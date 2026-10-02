# Optional 10 / 20 / 40 goal nodes

These labels organize the existing goal picker; they do not unlock content, reward items, or certify a completed build. All three groups remain optional, and merely opening or reopening the surface does not replace the saved goal.

- 10: choose a concrete class mechanism with its existing benefit/tradeoff description, or practise the class without spending mechanism materials.
- 20: explicitly track the existing A/B preset goal or an eligible, stable-ID element variant. Saving a preset does not grant its equipment.
- 40: explicitly track a particular ascension/epic prerequisite/reforge, or track tier 40 independently. The current unlocked tier remains selectable.

Current actions continue to use SelectedProgressionGoal, CanAct and ActionIdentity; selection uses SelectCoreGoal / SelectProgressionGoal. Completion and all costs remain in the existing progression service. No save schema, reward or combat rule changed.

`python Tests/MilestoneGoalSurfaceTests.py <dotnet>` compiles and executes the actual GameUI.ProgressionGoal partial with the real persistence service and GUI shells: read-only opening/reopening, explicit candidate selection, preset action, tier-vs-equipment completion, stable item identity and material rejection. It also runs existing goal-identity/transaction regressions. This is not Unity GUI execution; wrapping, touch interaction and device rendering still require integration/device validation.
