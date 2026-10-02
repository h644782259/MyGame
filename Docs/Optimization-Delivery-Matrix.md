# 优化交付矩阵：实现与验收边界

本表记录 2026-10-02 的 B01–B10 / 策划 A–F 和首章三节点，以实际生产接线区分新实现、复用与验收边界。当前候选继承近墙特效修复 `9a34011`、环境修复 `92e3631` 和经济界面修复 `8c9d143`；已发布 PR19 冻结头 `e672e26` / `10ad25e` 的独立复审已判自身差异GO；当前候选继承PR16–18汇总修复。当前候选尚未完成最终统一聚合，不能引用旧头通过数作为当前头结果。默认入口预计159项（单套补充API编译时160项），数字仅为清点。下列托管/几何证据不代表 Unity 画面、完整战斗 trace 或设备验收。

## 美术与交互

| 项 | 当前实现 / 复用边界 | 实际验证证据 | 未验证事项 |
|---|---|---|---|
| B01 姿势提交 | 出手同步提交最终姿势后采样挂点；取消全身恢复与衔接次级恢复分开；同帧不重复推进动作/惯性。复用原战斗恢复参数，不改伤害频率。 | `HeroPoseCommitTests.py` 执行真实提交、释放、恢复和姿势方法；即时 pose 与旧蓄力覆盖负对照。B10 扩展仍保留原断言。 | Unity Update/LateUpdate 画面节奏、实际角色骨架表现。 |
| B02 挥击身份 | 真实 `WeaponSlash` 与装饰 `Slash` 分离；每次物理动作仅申请一条轨迹，换动作/取消停止采样，旧轨迹淡出。 | `WeaponSwingIdentityTests.py`：生产分派、重复申请、旧动作冻结；三个编译后精确失败负对照。 | 实际拖影观感、镜头尺度与透明排序。 |
| B03 箭雨与预算 | 非毒箭雨独立配方；万箭批次共享生命周期；全局装饰预算保主形/落点/终段优先，硬上限不变。 | `CombatReadabilityProductionTests.py`、`FilledVfxAllocationTests.py`：真实组件生命周期/批次/优先预算及负对照。 | 密集战斗 GPU 开销、特效节奏与箭形辨认度。 |
| B04 危险/命中边界 | 圆形敌预警复用判伤路径/LOS裁轮廓；主命中特效锚定真实落点，仅装饰可找空地。近墙证书使用完整原点—面可见区域与真实障碍精确相交，避免旧原点膨胀圆将合法主形全部裁空。 | `EnemyImpactContourTests.py`、`CombatReadabilityProductionTests.py`：墙、河桥、真实判定与全圆旧行为负对照。补强后旧套件6751项、五负对照，逐个要求主形/落点/接触正面积后再查边界；新近墙矩阵144部件均有面积，19,763,189次真实LOS检查及三个精准负对照。 | Unity 地形、透明材质和视角下的轮廓可读性。 |
| B05 装备保留区 | 杖下端固定安全长度，成长移上段；圣物实际零件约束在腰扣区。复用四职业装备/时装构建链。 | `EquipmentAttachmentTests`、`EquipmentAttachmentSourceTests.py`；独立追加提交 `2b3aeae` 的 `EquipmentCompositionProductionTests.py` 已执行 7,358 断言与三个负对照：四职业、T4三件、强化0/3/7/10、最高时装、两装配顺序及资源循环。该追加提交现已继承，并注册 `equipment-composition-production` 默认入口。 | 只验证静态变换顶点/挂点及资源；不是通用三角面碰撞或动态穿插验收。 |
| B06 战术/伙伴反馈 | 对象绑定供能/受援/争夺标志；断援短反馈；真实捕获进度分段；守卫35%正面减伤条件共源展示开闭甲。伙伴常驻饰与成长结构、到期/战败/替换收束分开；召回仍是活体导航。 | 本批 `TacticalLiveVisualTests.py` 24 项+两负对照；`CompanionAppearanceTests.py` 184 项，真实组件与 Dismiss 路径。复用既有减伤、目标和导航规则，无新伤害增益。 | 伙伴动态轮廓、守卫提示是否易辨、动画和真实触控反馈。 |
| B07 宝箱/试穿 | 单次组合插值与稳定跳过；试穿、观看分离；结果位置/增量/传说自选进度复用奖励与展示状态。 | 已交 `ChestCompositeProductionTests.py`、`ChestTrialProductionTests.py`、`RewardViewingRulesTests`、`ChestCurrencyDeltaTests`；保存失败与忽略保存结果负对照。见 `Reward-Viewing-B07.md`。 | 实际画面过渡、试穿取景与中文排版。 |
| B08 成长导航 | 路线直达技能详情并保留返回位置；切换技能清除上一技能成功/失败状态；机制收益/代价与评分并列；目标状态/行动固定、候选独立滚动。经济报价接线继续使用同一目标身份。 | 已交 `RouteSkillNavigationProductionTests.py`、`ProgressionGoalLayoutTests`、`GrowthNavigationSourceTests.py`；经济整合 `MilestoneGoalSurfaceTests.py` 保留固定 header。 | 真机最小字号、长文本阅读和滚动操作。 |
| B09 环境/文字 | 水流坐标和低频流光、河院不同速度；真实河流/水院/战术水域入口创建静态湿痕带与垂直水线，桥底保留接触；遮挡原因分组；世界字按HUD逻辑尺度、距离与优先级调度。 | 已交 `WaterFlowProductionTests.py`、`OcclusionCauseProductionTests.py`、`WorldLabelProductionTests.py` 与环境源码契约；`ShoreContactProductionTests.py` 174项和缺接线负对照。每水域≤64段、两个静态网格、共享材质，无Update/碰撞/导航修改。 | 水/熔流实际材质、镜头遮挡、中文实渲和设备帧时。 |
| B10 施法/首领 | 元素/唤灵已提交技能使用定向、地面、自护、契约姿势族；首领导能槽跟随活锚，纯模型停机演出不推迟战斗死亡/奖励。Tier轮廓和强化工艺**复用原装备结构与B05**，没有新增一套Tier系统。 | 本批强化 `HeroPoseCommitTests.py` 执行真实 `ApplyCasterSkillPose`；`LargeBossShutdownTests.py` 检查真实三通道构建、分件收束、时间冻结时清理及旧时间步负对照。 | **generic蓄力姿势仍未分族**；已分族的是提交后的技能姿势。没有完整动画融合/动态穿插、Tier观感或首领演出实机验收。 |

## 策划与数值

| 项 | 当前实现 / 复用边界 | 实际验证证据 | 未验证事项 |
|---|---|---|---|
| A 成长经济 | 分等级精通cap、10/20点核心门槛、旧投入保留；重铸只收递增金币，固定目标报价/前置/扣费共源并保身份机制强化。目标选中态包含目标等级，营地状态读取真实会话；精通提示由同一实际分档规则生成。 | 继承经济生产事务及旧行为负对照；服务测试壳保留 `ProgressionService.Reforge` / `ReforgeQuote` 依赖。新增 `EconomyGoalUiTests.py` 121项与三个旧行为负对照，覆盖固定20级目标/510金币、非营地状态和真实cap文案。详见 `Economy-Integration-Review.md`。 | 真实成长节奏、UI理解与经济平衡。 |
| B 祝福 | 战意10%、鹰眼+20pp、锋芒215%，基础165%不变；伙伴仅乘一次战意，暴击卡限主人直伤。 | `BlessingDamageProductionTests.py`、组合预算与可达性检查；主人/伙伴重复乘负对照。 | 预算按固定命中假设，不是实战DPS或最优配装结论。 |
| C 可碎冰 | 区分霜痕与当前可执行动作；复用实际瞄准、确认落点、范围、LOS和起手许可；蓄力期间保守不显示可碎冰。 | `ShatterAvailabilityTests.py` 真实 Player/HUD 查询及旧霜痕快捷判断负对照；包含8/10/13米、近无霜远有霜、隔墙、选点。 | 延迟落地时目标是否仍有霜痕无法由当前提示保证。 |
| D 守门/晶核 | 3.5秒只累计真实移动，返岗不打断已承诺预警；晶核两敌对象标记及2/1剩余。复用已有攻击/控制中断规则。 | `GuardReturnTests.py` 132项+旧计时负对照；晶核真实击杀回调与对象文字生命周期测试。 | 真实地图追击体感、标记密集时辨识。 |
| E 失败原因 | 保留死亡/超时/主动退出/生成或路径失败；建议读取真实受伤、治疗、目标状态证据。无截止计时房间不额外加入时限。 | `RoomFailureEvidenceTests.py`、章host实际状态/summary接线及失败被错误归为放弃的负对照。 | 设备交互与建议是否足够有用仍需用户试玩。 |
| F 灼燃终击 | 保直伤；先结算到期离散burn事件，再按己方既有burn、最高强度刷新规则兑现未来≤3秒事件并清对应未来；每cast/target一次，不加暴击/回能/proc。 | 本批 `BurnFinaleProductionTests.py`：真实 `EnemyStatusEffects`、实际 `ElementalAdvancedArea` 与四个精确负对照。**三级尾场仅验证生产发射/CombatArea接线不含兑换入口，未模拟完整尾场伤害流程。** | 无完整真实战斗trace、实际尾场播放或与祝福组合的实战平衡结论。 |

## 实际生产调用链

路径均相对 `Assets/Scripts`；方法名作为稳定定位，不将审计旧头行号冒充当前行号。

| 项 | 入口 → 行为 / 退出 |
|---|---|
| B01 | `PlayerController.BasicAttack` → `HeroModel.PlayAction/CommitActionPose` → 同步姿势 → 采样挂点；技能释放先退出蓄力再执行；取消与动作衔接分别恢复。 |
| B02 | Player真实挥击 → `CombatEffects.WeaponSlash` → 物理动作身份/取消；`Slash`只作装饰，区域tick不申请物理轨迹。 |
| B03 | Ranger箭雨 → `CombatArea`箭雨分支；高阶万箭 → `AdvancedSkillSequence`共享批次；毒域显式走毒配方；预算组件约束总数并先保主形。 |
| B04 | `EnemyAttackTelegraph` → `EnemyImpactRegion` → 同判伤GroundPath；实际Impact三部件 → `AnchoredImpactMesh` → `CombatSight.VisualTriangle` → `WorldTraversal.HasClearVisualTriangle`。 |
| B05 | `PlayerController.RefreshStats` → `HeroModel.ApplyEquipment` → 实际Weapon/Armor/Relic builder；杖底固定-.94，圣物受腰部保留区限制。 |
| B06 | RoomChain/Chapter实际房态 → `TacticalEnemyVisual`、`TacticalCaptureVisual`；GuardArmor同一35%规则连真实Health反馈；伙伴RefreshPower → 外形，Dismiss按Expired/Defeated/Replaced收束。 |
| B07 | 实际奖励事务 → receipt真实封顶后币差 → 宝箱单次组合绘制；选中框Travel→目标框；CollectionViewingState独立试穿/观看、两槽/角度；持久化确认后才自动试穿。 |
| B08 | 路线点击 → `OpenRouteSkill` → 详情顶部/清旧状态 → 返回原滚动；目标固定header调用同一ActionIdentity，候选独立滚动。 |
| B09 | `WorldBuilder`、LinkedRooms、TacticalRooms → `BuildWaterSurface` → 流光/湿痕/水线；遮挡独立记录英雄/目标原因；世界标签走HUD尺度/距离/优先级。 |
| B10 | 提交技能 → `CastPoseRecipe` → `ApplyCasterSkillPose`四族；Progression.Changed → OnProgressChanged → RefreshStats → ApplyEquipment复用Tier/强化构建；首领LiveAnchorMask三通道，死亡即退战斗、独立模型停机后销毁。 |
| A | 桌面/手机 → LearnMastery/SelectMasteryCore → 同MasteryProgressionRules → CalculateStats；重铸Quote绑定item/from/to/savePath → 验证 → candidate → CommitCandidate；目标等级不随英雄升级漂移。 |
| B | 祝福UI → RunChoices → Player单次直伤倍率/主人暴击；伙伴仅一次战意倍率，持续伤害不重复乘卡。 |
| C | HUD → `CanShatterNow` → 当前瞄准/确认落点/起手许可/范围/LOS → 同真实施法入口；不把霜痕等同可执行。 |
| D | Enemy实际位移 → 3.5秒EscapePostPolicy → 已承诺攻击完成后返岗；真实侧事件敌成员/击杀回调 → 晶核2/1对象标记与既有receipt。 |
| E | 实际减后伤害/治疗/房态 → BuildRunSummary → RunFailureEvidence → RunRecapPresentation；旧计时模式TimeExpired有真实超时入口，五房RoomChain没有新加时限。 |
| F | AdvancedSkillSequence终击 → ElementalAdvancedArea → 到期burn事件排空 → 己方既有burn快照 → 同击刷新 → 未来≤3秒离散事件领取 → 原直伤 → 单次无暴击/proc兑换；尾场普通CombatArea不进入兑换。 |

## 首章三节点与关卡策略

三营地复用观星员（Exchange）入口打开章节选择，**不是三位专属剧情NPC分别绑定节点**。选择节点/难度 → ConfirmChapterEnter → TryBegin资格/存档检查 → BuildWorld/BeginChapterRoom → 真实目标/击杀回调 → 原子结算 → 地标变化/返回。

| 节点 | 实际目标与空间 | 难度/取舍 |
|---|---|---|
| 林庭复明 | 净化两处各3秒，继而4秒撤离；争夺停计，中央障碍与环路改变站位。 | 困难护援迫使判断支援源/目标优先级；英雄危险圈压缩安全站位。 |
| 赤岩断供 | 猎杀指定index0开出口，随后撤离；分叉地形可选择推进路线，不必清全敌。 | 困难及英雄真实两侧Wisp交火、两Guardian守出口；英雄热涌按墙截断。 |
| 星台封印 | 休整补25%生命（限疗模式补次数），随后首领与两护卫；三者全部死亡才结算。 | 困难首领顺序追加、英雄种子方向/锚候选；休整后集中处理首领/护卫，未另叠独立光束。 |

共6房（5战1休），三种拓扑。普通/困难/英雄门禁独立；只有星台推进共享层级。材料基础1/1/2随层段，首次+1，没有难度乘奖。完成mask、首次标记、材料与receipt在同一candidate保存；失败可重试，重复结算幂等，version1旧存档缺字段默认0，保留合法旧副本资格证据。房间检查owner/room/epoch、入口至目标/出口可达性与刷怪上限。返回走SaveBeforeLeaving → ChangeZone → UpdateTimeScale，失败不拆旧世界、不强制timeScale=1。

章节种子有镜像/奇偶与255种子回退，但**未加入章节近期多局拒重，普通敌初始仍有全局Random影响**；不能称完整战斗轨迹可重现。旧五房的前两局种子拒重仍保留。种子/几何统计证明布局输入差异，不能当作真实试玩已证实策略体验。

## 验证记录与边界

- 首次冻结PR19三平台157/157属于 `e672e26` / `10ad25e` / `3954724`，显式设置 `DOTNET_TieredCompilation=0`；不能替代当前候选测试。首次PR16/17/18通过也没有捕获后来复审揭示的合法贴墙空主形、跨技能旧提示和目标/精通文案问题。
- 新PR16/17/18三平台完整聚合正在各自冻结修正头以默认JIT运行；保留任何原始失败，不以关闭tiering或后续重跑覆盖。旧equipment-lookups出现24624字节分配的失败记录与诊断均保留，未把统计噪声直接当作生产无分配证明。
- PR19自身独立复审完成，无新增B06/B10/F阻塞；当前修正候选仍待统一最终聚合。定向历史证据包括战术24+2负对照、伙伴184、首领35+1负对照、burn106+2337调度及4负对照、姿势45+2、章host77+4、返营126+2；这些不是当前候选整套通过数。
- 近墙测试以144个必要部件正面积为前置，修复原套件700→67条断言因空顶点循环而虚通过的问题。可见性几何证明与每次生成工作量统计不是GPU性能或帧时结论。
- B05的7358项使用真实Hero/装备/时装构建与网格recipe，覆盖T4三件、强化0/3/7/10、最高时装、两装配顺序与资源循环；材质/cloth/引擎生命周期使用替身，不等同动态三角面穿插验收。
- B10分族的是提交后技能姿势；generic蓄力仍通用，原需求未明确要求蓄力分族。Tier/强化复用现有结构，未另造Tier系统。本地F三级尾场测试只验证发射接线/兑换隔离；父独立审查另执行真实tail动态312断言。该补充仍不是Unity场景完整战斗trace。
- 本环境没有Unity Editor。缓存Unity2021.3.33参考API编译仅作补充，不代表项目Unity6000.6、实际JsonUtility、平台构建、GPU、中文渲染、真机触控或帧时。六场真实试玩、截图/录像与完整战斗trace未执行。

保护范围：三端共享玩法源码保持一致；iOS平台、字体、iPad配置及各平台ProjectSettings/Packages保持原配置。Android仅本地同步，GitHub/main合并与统一发布由父任务处理。

父任务PR19独立复审提供的补充证据（针对旧冻结头，不是当前候选）：burn 26,279断言、真实tail动态312、活标记/伙伴/boss/caster 446，20负对照有效；默认iOS157/157，Windows156/157仅既有equipment-lookups间歇24624字节，14套现代引用编译通过。保留原失败，不将其改写成默认聚合全绿。
