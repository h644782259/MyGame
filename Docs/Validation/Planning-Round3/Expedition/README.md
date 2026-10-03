# 第三房分支：实施与验证边界

基线：`fce5efc4361614aed3eca070ed2574b7d7204a8e`。仅五房回廊远征（SelectedArenaMode=3）；章节、训练、其它竞技模式不进入分支入口。

## 实际接线

第二房目标完成后，北门按钮 / T / 移动交互打开两选一面板；未确认不创建第三房对象。面板说明目标、敌人和地形，不使用简单/困难标签。后退或 Esc 关闭面板，保留第二房现场。暂停和后台禁止确认；保存失败保持未决定；重复点击不能跳房。

- 守印侧廊：双印净化、4 名近战守点者（2 守卫、2 哥布林）、中央墙和两侧印记。无供能者。沿用实际敌人岗位控制接口。
- 断供侧廊：击杀金环供能者、6 敌（2 守卫、1 哥布林、3 魔灵），交错墙前后排；只有一个供能源。沿用供能视线与范围规则。

布局和位置取原种子镜像，使用实际 WorldTraversal 验证可走与可达；只创建所选房间。第四房休憩、第五房首领不变；首房和第四房仍各一次祝福。第三房目标种类现在由选择决定，故可能与前两房重复；这是对旧“三种目标每种一次”约束的明确替换。

原基础奖励方法逐字保持（reward-baseline.log）；分支不增材料奖励。旧种子支线事件仍保留原规则，不额外创建新事件。

失败复盘增加原条件重试：保存前置通过后，实际 ChangeZone → ResetExpedition → ResetRoomChain 重建 State、epoch 和新的奖励 GUID。保留 seed、所选分支、阶数和限疗条件；跳过 RNG 与 NextSeed 历史轮换。未选分支前失败则重试仍待选择。回营后新远征清空选择。房间构建抛异常时结束为 GenerationOrPathFailure，finally 释放切房锁；保留确认分支用于重试。

## 已执行

命令均在本 worktree，SDK `/workspace/shared/emberfall-tools/dotnet/dotnet`。

| 日志 | 结果 |
|---|---|
| branch-final-v3.log | 3,785 断言，54 种子 × 2 分支、4 阶及限疗开关；真实 Confirm / EnterNext / Begin / ChangeZone / ResetExpedition / ResetRoomChain / Retry / RunChoices / traversal；旧第三房和重试换种子两个编译负控均准确失败 |
| free-seal-final.log | 229 既有真实印记 host 断言 + 51,061 状态断言 + 强制印记顺序负控 |
| first-choice-final.log | 90 真实首房延迟祝福断言 + 2 编译负控 |
| chapter-host.log | 460 章节持久化断言 + 12 负控 |
| chapter-return.log | 126 章节切回时钟断言 + 2 负控 |
| journey.log | 3,684 集成旅程断言 + 6 负控 |
| practice.log | 61 原练习生命周期断言 + 4 负控 |
| vanguard.log | 原恢复/反击/死亡暂停生产链与负控 |
| api-final.log | 全 Assets/Scripts：Windows/iOS/Android 宏 API 编译，均 0 错误/警告 |
| source.log | 既有战术/祝福接线源契约 |
| reward-baseline.log | 奖励方法与基线逐字一致及 SHA256 |

`branch-first.log` 是最初测试未通过真实 Tick 解锁祝福的 fixture 缺陷；`branch-second.log` 是新远征测试未重选模式的 fixture 缺陷。其余非 final 命名日志为中间步骤保留的原始记录，不用它们替代最终结果。负控日志中的异常是预期的断言失败，runner 验证其具体文本；不是编译失败冒充行为证明。

测试的 WorldBuilder 对象、保存 IO、Player/Enemy 外壳与无关章节/竞技入口是显式替身；实际生产房间/状态/几何/选择/重试方法参与运行。岗位 AI 沿用已有实测 EscapePostPolicy，新增调用接入；本新 suite 不声称执行完整 EnemyController.Update。未运行 Unity、真实物理/GUI/设备、GPU、录像或性能测量。移动窄屏面板、守点实际观感、遮挡和操作舒适度仍需 Unity 人工验收。

## 消费者审计 / 集成

- 新 runner `Tests/RoomBranchProductionTests.py` 未修改中央 Tools 注册表，由根统一注册。
- RoomFreeSealHost / RoomFirstChoice runner 加实际 RoomBranchGeometry；FirstChoice 加实际 RoomBranchChoiceOpen 属性提取。
- RoomChainStateTests 保留原 capture/death/reward 等断言，显式选择第三房；旧“3 种目标/地形全覆盖”调整为前两房轮换、第三房由用户选，新增确定性检查。
- RoomTacticsSourceTests 更新 new State 构造器契约，不取消检查。
- ChapterHostFixture（ChapterHost/ChapterReturn/IntegratedJourney 消费）补非远征 retryingRoomChain=false、roomRetrySeed=0、RoomBranchChoiceOpen=false。
- CampPracticeSessionBoundary、VanguardRecoveryIntegrationTests 补非远征 RoomBranchChoiceOpen=false。未改其生产练习/恢复代码和原断言。
- 共享 runtime 小触点：GameSession.cs 的 InputBlocked / UpdateTimeScale；GameSession.Expedition.cs 的种子初始化；GameUI.cs 的面板和 Esc；GameUI.RunRecap.cs 的重试按钮。其余逻辑在 RoomChainState / GameSession.RoomChain / 新 RoomBranch partial 与 geometry。
- 新 .cs 保留一次生成的稳定 .meta GUID；无资源包、ZIP、Library、PR、推送或主分支写入。
