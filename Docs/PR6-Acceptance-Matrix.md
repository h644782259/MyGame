# PR6 逐项实现与验收矩阵

PR5 已由父任务合并；PR6 基于 Windows main `abe5ef06` / iOS main `f83b7d28`，保持 draft。本表按可独立验收的18组美术项归类，再逐项列策划00–09；同一功能跨两表只实现一次。

“已实现”表示下列生产入口已接通，不代表画面或玩法体验已验收。所有列出的检查都是本环境实际运行的托管规则、生产几何、源码契约或参考API编译。Unity Editor/Player、渲染、输入回放、设备性能与新手体验均未执行。最终聚合报告必须与固定源码哈希一并阅读。

## 美术18组

| 项 | 原有基础 / 本次实现 | 真实入口与调用 | 实际检查 | 未完成体验验收 |
|---|---|---|---|---|
| A01 普攻接触与放箭 | 原有即时伤害保留；本次从接触帧开始恢复，箭显隐同源，关键技能姿势和闪避优先 | `PlayerController.BasicAttack`→`CombatModel.PlayAction`→`BasicActionTimeline`；`TraversalStartedThisFrame`双入口门禁 | `BasicActionTimelineTests`、`CombatPrioritySourceTests`、`CombatReviewRulesTests` | 连续高速输入、取消/闪避的真实观感 |
| A02 明确材质 | 原程序化模型保留；护甲刀刃金属、衣服/皮肤/木弓/叶冠及NPC显式材质 | `CombatModel.Part/MeshPart`、`CombatModel.Costumes`、`WorldBuilder.Hubs`传`VisualSurface` | `VisualIdentitySourceTests`、运行源码编译 | 光照下金属/皮肤/布料辨识 |
| A03 技能图标 | 复用共享atlas；修正唤灵0震荡、1荆棘、2狼及关联图标 | `UIIconAtlas`由桌面/手机技能按钮共同读取 | `VisualIdentitySourceTests` | 小尺寸图标辨识 |
| A04 危险与打断 | 原预警/打断逻辑保留；固定橙红危险边界，独立青色符号和计时弧 | `EnemyAttackTelegraph`；`EnemyController.CreateWarning`；`LargeExpeditionBoss.DrawBeam` | `HazardEffectsSourceTests`、`LargeBossMotionSourceTests` | 降特效/拥挤时可读性 |
| A05 释放后墙边范围 | 复用生产`CombatSight`/`WorldTraversal`；地面环尊重遮挡，大冰火形体保守裁剪 | `CombatArea.Spawn`的`respectCover`；`FilledSkillVfx`；`CoveredAreaParticles.LateUpdate` | `CombatSightTraversalTests`94项生产几何检查 | 贴墙轮廓与伤害感知是否一致；保守隐藏是否过稀 |
| A06 显式技能配方 | 移除RGB推断元素；区域技能传实际视觉配方 | `PlayerController`各`CombatArea.Spawn`调用→`SkillVisualRecipe`；`AdvancedSkillSequence` | `SkillVisualRecipeTests`376项、`VisualIdentitySourceTests` | 各职业技能外观识别 |
| A07 降特效预算 | 原偏好保留；扩展粒子、闪电、死亡碎光；禁用释放租约；机制提示独立容量 | `DecorationLease`、`DecorationBudget`；特效工厂准入与`OnDisable`；`SpawnMechanismText` | `DecorationBudgetTests`3002项、特效源码契约 | 极端战斗性能、警示始终可见 |
| A08 移动战斗状态 | 补HP数值、药剂/限疗、闪避CD、十技能可用/CD/缺能/蓄力/锁定/被动和拒绝反馈 | `GameUI.MobileFeedback`、`MobileControls.Feedback`；`PlayerController`拒绝分支→`GameSession.ReportControlFailure` | `MobileCombatFeedbackTests`、机会/拒绝源码契约 | 真机触控、文字密度 |
| A09 目标与锁点 | 原目标选择不改；增加自动目标、显式集火和蓄力固定落点反馈 | `GameUI.Initialize`挂`CombatTargetFeedback`；只读`AimTarget`/`ExplicitFocus`/charge，按owner/epoch清理 | `FeedbackSourceTests`、`CombatOpportunitySourceTests` | 标记在地形和特效上的辨识 |
| A10 飘字 | 替换固定粗略通道为字体测量、renderer复核、屏幕避让；机制4席独立 | `FloatingNumber`→`CombatTextLayout`，实际创建前准入 | `CombatTextLayoutTests`11735项、反馈源码契约 | 1.8倍/高DPI中文、暴击重叠 |
| A11 菜单可达与长文 | 保留工坊/冒险入口；修日志避让、五入口、滚动保持和长技能测量 | `GameUI.DesktopSkills`、`AdventureSelectionLayout`、`GameUI.MobileWorkshop` | `PanelReadabilityLayoutTests`345项、旧mobile workshop/layout检查 | Unity字体布局与滚动手感 |
| A12 英雄/守卫动作 | 原骨架保留；步相读正常导航实际位移，侧后步/跳跃/起停/披风惯性；守卫明确阶段 | `PlayerController`导航采样→`LocomotionPoseState`→`CombatModel.Motion`；`EnemyController.AnimateModel` | `LocomotionPoseTests`4030项、10接线契约；旧画面fixture参考编译 | 剑卫/守卫/游侠镜头样板、动作质感录像 |
| A13 伙伴动作 | 原伙伴攻击数值保留；狼咬合/局部扑身、星灵核心收缩、树灵双臂回收 | `SummonedCompanion.Update`真实释放后再`Animate`；`CombatModel`三伙伴分支 | 运行源码编译、契约/伤害预算旧回归 | 动作与真实投射/接触的画面同步 |
| A14 大首领动作 | 原阶段规则保留；四阶段姿态，星环积分角速度，发射口跟真实扫射角 | `LargeExpeditionBoss.BeamWorldAngle`→`LargeBossRig`→`LargeBossMotion`；二轮蓄力重置旧角 | `LargeBossPhaseTests`45项、`LargeBossMotionTests`30007项 | 预警/扫射/暴露/恢复的实际辨识 |
| A15 职业轮廓与翅膀 | 原职业/时装ID、属性不变；重甲/袍/单肩皮甲/叶披肩，羽/晶体/机械三形 | `CombatModel.Hero/ApplyEquipment/ApplyFashion`→`CombatModel.Costumes`→`CostumeRecipes` | `CostumeRecipeTests`4624项拓扑/边界/身份 | 各阶装备剪影、翅膀轮廓和穿插 |
| A16 三营地和环境 | 复用三营地/NPC；布局、地标、货架/砧锤/交换星盘、轻量idle、道路水岸 | `WorldBuilder.Hubs`和导航共读`HubSettlementPlan`；`GameSession.HubNpcPosition`同源；`HubNpcIdle` | `HubSettlementGeometryTests`587项真实导航，含多体型入口/NPC/传送门 | 环境生活感、地标识别与实走 |
| A17 比较/收藏/开箱 | 原授奖与最高收藏属性保留；机制得失对照、真模型旋转试穿、持久奖励实物展示 | `DrawItemDetail`→`EquipmentComparisonPresentation`；`DrawFashion`→`DrawDesktopCollection`/`DrawMobileCollectionPreview`→`CollectionModelPreview`；奖励receipt选预览 | `EquipmentComparisonPresentationTests`178项、`CollectionPreviewSourceTests`；随机状态/资源释放契约 | RenderTexture渲染、手机旋转操作、真实翼/武器画面 |
| A18 前景与十格构图 | 原相机和触控体系保留；标记世界遮挡、淡化/位置兜底、建筑缩距、有限投影偏移、位置/透明度/视觉大小设置 | `AdventureCamera.UpdateVisibility`→`CameraOcclusionSurface`；暂停设置→`DrawMobileControlPreferences`→`EffectPreferences`→`MobileControls.Layout`与实际绘制/点击 | `CameraVisibilityTests`2013项；三布局`MobileControlLayoutTests`10181项、相机生命周期源码检查 | 568×320中央窗口的真实近战观感、透明shader、帧率、iPad |

## 策划00–09

| 项 | 已存在 / 本次实现 | 生产入口和事件调用 | 实际检查 | 未完成事项 |
|---|---|---|---|---|
| 00 50级实战基线 | 原场景/敌AI保留；新增五套基础配置×静止/移动/前近后远三fixture、隔离存档、受限JSONL | Editor菜单/`CombatReviewFixture.Run`；`CombatReviewEvents`接实际伤害/命中/取消/缺能/受击/死亡，fixture记录伙伴追击观察；汇总脚本保留事件类别 | `CombatReviewEventsTests`、fixture隔离源码契约、`CombatReviewSummaryTests.py` | **只交脚本配置；真实50级三场景基线、录像均未完成**。配置全技能1阶/初行装备，不是满配。Editor新增脚本未获得Editor程序集编译；miss仅明确已观测事件，非统一全技能命中率 |
| 01 追击/幻影易伤 | 复用首领策略与投射；共享1.8s接近/1.5s停滞预算，合法冲锋/齐射后备；仅真实击中原锁定目标先加易伤后伤害 | `EnemyController.Update`→`BossAttackPolicy.PreferredApproach/LegalFallback`；`CombatProjectile`锁定命中→`LockedImpactMarkPolicy`→伤害 | `CombatReviewRulesTests`5677项含6m/10m/近身规则；相关接线检查 | 退让、绕柱的真实AI录像；穿透/墙边实际命中 |
| 02 占点 | 原击杀+计时要求保留；3.2m同源半径，敌`NavigationRadius`相交才争夺，保存进度 | `GameSession.TickArenaRun`→`ExpeditionModeState`；可见圈/`HoldStateLabel`/手机卡；idle wait供记录器 | `HoldPointStateTests`41项、旧模式7765项、源码契约 | 推出圈后恢复、清怪等待的真实战斗记录 |
| 03 职业机会/HUD | 复用实际技能状态；至多一条职业机会和独立免费指令，不缓存跨epoch状态 | `CurrentCombatOpportunity`读取counter/当前目标status/伙伴数量寿命；`MobileSkillState`/charge读取实际可用状态 | `CombatOpportunityTests`、状态/布局/机会接线检查 | 真机是否一眼理解；短提示优先级观感 |
| 04 动作/主裁决/音频 | 复用伤害预算；接触帧、pose优先级、裁决首事件约.15s、关键声道保留、hit节流 | `BasicAttack`→timeline；`AdvancedSkillSequence`裁决step0；`ProceduralAudio`→`AudioVoicePolicy`；dodge/cancel门禁 | 时序/预算/音频规则、`CombatPrioritySourceTests`、旧技能总预算检查 | 实际音画同步、高速/短窗口/贴墙试玩 |
| 05 免费持久集火 | 原契约及短时buff保留；新增独立行为指令，显式目标优先，不按buff到期清除 | 桌面`DrawCompanionCommands`按钮/手机pointer分派→`ActivateFreeCommand`→`SetFreeFocus/FreeRecall`→`CompanionDirective`；`AcquireTarget`先读显式目标；死亡/超距/epoch清除 | `CompanionDirectiveTests`604项；免费入口与不消费契约/机会接线；charged目标死亡固定点修复 | A3m/B8m连续6秒真实录像；无命令刷收益的真机操作 |
| 06 灼烧/路线/方案 | 较强DPS保留逻辑已存在且保留；新增有效刷新事件、两路线缺口卡、方案variant快照 | `EnemyStatusEffects.Burn` max DPS且结束清零；`CampRouteCards`→桌面/手机营地；`SavePreset/ApplyPreset`仅恢复已解锁变体 | `AdventureProgressionTests`103项、`BuildPresetTests`136项、原存档回归/源码复核 | A-B-A真实UI操作、强弱灼烧实战 |
| 07 两次祝福 | 复用`RunChoices`；首房后一次、房4休息一次，推荐读实际可用技能、两次不重复 | `OpenRoomGate`排`DeferredRoomChoice`→下次有效tick `PrepareRoomChoice(1)`；`GameSession.RoomChain`休息 `PrepareRoomChoice(2)`；`UsableRanks`桌面equipped/手机learned主动 | `RoomBlessingRouteTests`50404项；deferred18项；CD/暂停/epoch接线 | 连锁伤害→选择→恢复的Unity复现，选项是否形成取舍 |
| 08 共享推进/目标 | 复用五模式奖励、幂等收据；增加shared最高阶+bestFloor迁移、共享首核/5阶升华、单一保存目标 | 普通终局`ProgressionService`结算；模式`FinalizeModeRun`/远征终局→`TryGrantModeReward(...,DungeonTier)`；`CommitCandidate`成功后发布；`ProgressionGoalStatus`供camp/entry/results，picker→`SelectProgressionGoal` | 新103项、模式奖励14项、成长2204项、存档失败/重启/receipt旧回归；独立调用链审查 | 五模式完整通关/失败/退出/恢复真机流程；未重设10/20/40奖励 |
| 09 真事件教学 | 旧cast进度不再当完成；新增持久化class evidence、可用后显示，保留早达成，首通导航 | `PlayerController`完美闪避来源反击造成敌HP损失/碎冰消费/三毒消费；`EnemyStatusEffects.Burn`有效refresh；`SummonedCompanion.OnConfirmedHit`显式目标→`RecordClassTutorial`→持久化；首通core/route/preset/goal按钮 | 新成长/教学状态、`CombatOpportunityHookSourceTests`、独立事件链审查 | 四职业真实完成录像；新手接受度未验收 |

## 可复现命令与证据口径

两仓分别执行 `bash Tests/Run-CloudValidation.sh --dotnet /workspace/scratch/dotnet/dotnet --compile`，再逐个执行 `Tests/*SourceTests.py` 与 `Tests/CombatReviewSummaryTests.py`，最后 `git diff --check`。最终报告位于各仓 `Tests/TestResults/Cloud-Latest/report.json`，含每组结果与实际生产源码SHA-256；阶段报告另留固定文件，不用失败或源码变化期间的报告替代最终报告。

本环境编译引用为Unity2021.3.33，不能算Unity6 Editor运行或四项Unity6准确引用编译。官方Unity下载被代理CONNECT 403阻止，未绕过。iOS使用共享GameFont且相机不销毁共享字体；ProjectSettings、iPad设置和字体资源未变。
