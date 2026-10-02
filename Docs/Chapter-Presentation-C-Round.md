# C round: direct star entry and saved chapter results

Star preparation is part of the existing entry screen. `ChapterDefinition.RoomCount` gives Star one boss room; Forest and Redrock retain two rooms. The host uses the same count for status, while the shared host wave getter and chapter geometry/state consume the agreed definition. The old Star rest-room transition is removed. Difficulty, tier, carried potions and limited healing remain distinct attempt settings.

The host captures an attempt-local `ChapterResultSnapshot`: independent seal progress, last actual damage source/amount, and specific generation/reachability failure context. It is retained when leaving or respawning cannot save; it is not a new persistent history schema. Successful settlement records the actual capped material delta, newly available node/difficulty/shared tier and shared first-core eligibility only for a completed StarPlatform node (the full chapter), and only after the progression transaction succeeds. Repeat completion displays its actual grant and does not replay a first-clear story reveal. The next-node action returns through the existing guarded leave path, then selects the unlocked node in camp; entering still requires a separate confirmation.

`RecordChapterDefeat` admits only registered run/room/epoch enemies before the shared kill path grants anything. Chapter XP comes from the entry-level budget receipt, with explicit registration/claim failure paths. Legacy enemy reward/death/save ordering remains owned by the shared host. Forest seals have two simultaneously created, independently addressed markers; the host advances and contests each point separately and preserves completed dark rings.

Result visibility alone waits on `LargeBossShutdownVisual.IsPresenting` for the actual session/player/epoch lifecycle, with death-frame admission protection. Settlement, death and combat pause happen immediately. A small saved/pending badge preserves the battlefield; Continue invokes the pure visual Skip API and opens the result. A boss killed before its guards does not restart the presentation clock. Chapter deaths route to the chapter evidence screen and retry the real Respawn save path; ordinary deaths retain the original screen.

Validation entry points (no new aggregate registration required): `ChapterHostProductionTests.py` includes real host reward/receipt/lifecycle-gate regressions and six compiled negative controls; `ChapterEntryProductionTests.py` includes actual UI navigation/result code, real progression persistence and Back/opaque-first controls; `ChapterReturnTimeScaleTests.py` exercises real UI Return/Respawn through filesystem rejection and retry. `ChapterUIWiringTests.py` retains ordinary-mode routing contracts. `ChapterResultSnapshot.cs` is an added compile dependency wherever `ChapterEntryPresentation.cs` or `GameSession.Chapter.cs` is included; the default `chapter-presentation` pure check also needs it.

Managed engine substitutes are explicit. These tests do not establish Unity 6 rendering, screen typography, actual touch feel or device frame timing. The C08 entry layout revision is a separate follow-up.


## C08 入口布局（独立跟进）

- 桌面章节内容按 960×660 上限居中；手机/平板三节点卡固定在详情滚动区上方，返回、兑换、确认仍固定底部。568×320 下卡片与操作按钮均保留 48 逻辑单位触控高度。
- 林庭树形、赤岩阶石、星台十字星使用共用程序图形，不依赖额外字体。当前选择文字与“最高通关”分开，三难度标记直接读取实际最高完成难度。
- 当前难度、阶数、治疗摘要相邻；目标、实际机制和共源材料奖励默认显示，故事手动展开。共享首核说明仅由星台通关（完整章节通关）启用，林庭/赤岩不授予资格；同时遵循 pending/claimed 状态，不再错误承诺“无全局首核”。
- 实际 `GameUI.Chapter` 托管回放覆盖 568/667/800/1024 × 1/1.8/3 缩放、固定节点/底栏、故事展开、宽屏居中，以及原有保存失败、Back、结果演出和下一节点路径。旧节点随滚动及旧桌面原点布局分别编译并精确击中负对照。
- 此为生产方法/矩形与状态验证；未运行 Unity 6 字体渲染、设备触控或画面评审。

PR20 review correction: Forest/Redrock completion and save reload must not create shared first-core eligibility. Real host completion of StarPlatform must create the one-time pending entitlement after successful persistence; UI previews distinguish these boundaries and retain pending/claimed deduplication.

The actual `OnPlayerDied` chapter branch now delegates to `FailChapter` before the budget is canceled. Host replay executes that real method, verifies independent partial seals, last-hit details and earned XP survive a rejected death save, and passes the retained snapshot through the production result formatter. A compiled old direct-Fail/Cancel branch must fail `DEATH_RESULT_CAPTURE`; ordinary death and duplicate-call penalties remain covered.
