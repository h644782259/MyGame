# Fixed goal identity and actual camp context

Reforge candidates represent the current character level. Their selected check now compares that level with the persisted target as well as the goal kind and item ID. A selected 10→20 target remains 20 / 510 gold after the character reaches 50; the new 50 candidate is unselected until explicitly chosen. The service quotation and atomic payment implementation are unchanged.

All goal status surfaces use `CurrentProgressionGoalStatus`, which passes the actual session camp state into the service formatter. The optional formatter context defaults to false for callers without a world context; it no longer invents arrival at camp. Workshop, current-goal header, recap drawing and recap height measurement share the same path.

`python3 Tests/EconomyGoalUiTests.py /path/to/dotnet` executes the actual goal UI partial and actual progression service, plus the exact formatter call expressions extracted from recap drawing and measurement. It checks candidate clicks, level changes, fixed header price, field/camp contexts, recap gains and read-only presentation. Both old behaviors must compile and fail their named assertion. It also retains the original milestone and identity tests. GUI shells and extracted recap calls are not a full recap render or device input simulation.

Validation: 15 new production UI/context assertions, 15 existing milestone assertions, 52 existing identity/transaction assertions; two compiled negative controls. Original EconomyGrowthTests and its regression groups/negative controls pass. AdventureProgressionSourceTests and GrowthNavigationSourceTests pass; the fixed header and action layout remain intact. Executed with .NET 8 and DOTNET_TieredCompilation=0. No Unity execution, device rendering or platform build.

Default aggregate registers `('economy-goal-ui','EconomyGoalUiTests.py')` in the Python checks list. No new core dependencies are required by other harnesses.

Follow-up registration audit: all 65 `*SourceTests.py` passed. All runtime C# sources compiled for Windows/default, `UNITY_ANDROID`, and `UNITY_IOS` against the cached Unity 2021.3.33 references, with zero warnings/errors. These are supplementary old-reference checks, not Unity 6000.6 validation. All five production status call sites route through the actual-context helper; standalone goal harnesses already include the complete partial and require no new dependency. Evidence: `/workspace/scratch/economy-goal-ui-validation/`. No full aggregate or push was performed.
