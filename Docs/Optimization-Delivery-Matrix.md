# 优化交付矩阵：实现与验收边界

本表记录 2026-10-02 的 B01–B10 / 策划 A–F，不以登记清单中的“待实施”代替当前源码状态。独立整合分支基于经济审查 `d29b8d8`，叠加 B06 `ddd922c` / `99e05ac`、B10 `7467480` / `05a9890`、F `5be2c87`。当前整合已沿环境→经济→最终分支继承 `c149965` 全部父级修复及B05生产矩阵；默认注册包含158项（含补充API编译），该数字仅为入口清点，最终统一聚合待执行。未修改 PR14–16 冻结头。各项“通过”仅指列出的源码或受管生产测试，不代表 Unity 画面、完整战斗 trace 或设备验收。

## 美术与交互

| 项 | 当前实现 / 复用边界 | 实际验证证据 | 未验证事项 |
|---|---|---|---|
| B01 姿势提交 | 出手同步提交最终姿势后采样挂点；取消全身恢复与衔接次级恢复分开；同帧不重复推进动作/惯性。复用原战斗恢复参数，不改伤害频率。 | `HeroPoseCommitTests.py` 执行真实提交、释放、恢复和姿势方法；即时 pose 与旧蓄力覆盖负对照。B10 扩展仍保留原断言。 | Unity Update/LateUpdate 画面节奏、实际角色骨架表现。 |
| B02 挥击身份 | 真实 `WeaponSlash` 与装饰 `Slash` 分离；每次物理动作仅申请一条轨迹，换动作/取消停止采样，旧轨迹淡出。 | `WeaponSwingIdentityTests.py`：生产分派、重复申请、旧动作冻结；三个编译后精确失败负对照。 | 实际拖影观感、镜头尺度与透明排序。 |
| B03 箭雨与预算 | 非毒箭雨独立配方；万箭批次共享生命周期；全局装饰预算保主形/落点/终段优先，硬上限不变。 | `CombatReadabilityProductionTests.py`、`FilledVfxAllocationTests.py`：真实组件生命周期/批次/优先预算及负对照。 | 密集战斗 GPU 开销、特效节奏与箭形辨认度。 |
| B04 危险/命中边界 | 圆形敌预警复用判伤路径/LOS裁轮廓；主命中特效锚定真实落点，仅装饰可找空地。 | `EnemyImpactContourTests.py`、`CombatReadabilityProductionTests.py`：墙、河桥、真实判定与全圆旧行为负对照。 | Unity 地形、透明材质和视角下的轮廓可读性。 |
| B05 装备保留区 | 杖下端固定安全长度，成长移上段；圣物实际零件约束在腰扣区。复用四职业装备/时装构建链。 | `EquipmentAttachmentTests`、`EquipmentAttachmentSourceTests.py`；独立追加提交 `2b3aeae` 的 `EquipmentCompositionProductionTests.py` 已执行 7,358 断言与三个负对照：四职业、T4三件、强化0/3/7/10、最高时装、两装配顺序及资源循环。该追加提交现已继承，并注册 `equipment-composition-production` 默认入口。 | 只验证静态变换顶点/挂点及资源；不是通用三角面碰撞或动态穿插验收。 |
| B06 战术/伙伴反馈 | 对象绑定供能/受援/争夺标志；断援短反馈；真实捕获进度分段；守卫35%正面减伤条件共源展示开闭甲。伙伴常驻饰与成长结构、到期/战败/替换收束分开；召回仍是活体导航。 | 本批 `TacticalLiveVisualTests.py` 24 项+两负对照；`CompanionAppearanceTests.py` 184 项，真实组件与 Dismiss 路径。复用既有减伤、目标和导航规则，无新伤害增益。 | 伙伴动态轮廓、守卫提示是否易辨、动画和真实触控反馈。 |
| B07 宝箱/试穿 | 单次组合插值与稳定跳过；试穿、观看分离；结果位置/增量/传说自选进度复用奖励与展示状态。 | 已交 `ChestCompositeProductionTests.py`、`ChestTrialProductionTests.py`、`RewardViewingRulesTests`、`ChestCurrencyDeltaTests`；保存失败与忽略保存结果负对照。见 `Reward-Viewing-B07.md`。 | 实际画面过渡、试穿取景与中文排版。 |
| B08 成长导航 | 路线直达技能详情并保留返回位置；机制收益/代价与评分并列；目标状态/行动固定、候选独立滚动。经济报价接线继续使用同一目标身份。 | 已交 `RouteSkillNavigationProductionTests.py`、`ProgressionGoalLayoutTests`、`GrowthNavigationSourceTests.py`；经济整合 `MilestoneGoalSurfaceTests.py` 保留固定 header。 | 真机最小字号、长文本阅读和滚动操作。 |
| B09 环境/文字 | 水流坐标和低频流光、河院不同速度；稳定岸桥接触；遮挡原因分组；世界字按HUD逻辑尺度、距离与优先级调度。 | 已交 `WaterFlowProductionTests.py`、`OcclusionCauseProductionTests.py`、`WorldLabelProductionTests.py` 与环境源码契约。 | 水/熔流实际材质、镜头遮挡、中文实渲和设备帧时。 |
| B10 施法/首领 | 元素/唤灵已提交技能使用定向、地面、自护、契约姿势族；首领导能槽跟随活锚，纯模型停机演出不推迟战斗死亡/奖励。Tier轮廓和强化工艺**复用原装备结构与B05**，没有新增一套Tier系统。 | 本批强化 `HeroPoseCommitTests.py` 执行真实 `ApplyCasterSkillPose`；`LargeBossShutdownTests.py` 检查真实三通道构建、分件收束、时间冻结时清理及旧时间步负对照。 | **generic蓄力姿势仍未分族**；已分族的是提交后的技能姿势。没有完整动画融合/动态穿插、Tier观感或首领演出实机验收。 |

## 策划与数值

| 项 | 当前实现 / 复用边界 | 实际验证证据 | 未验证事项 |
|---|---|---|---|
| A 成长经济 | 分等级精通cap、10/20点核心门槛、旧投入保留；重铸只收递增金币，固定目标报价/前置/扣费共源并保身份机制强化。 | 继承经济生产事务及旧行为负对照；服务测试壳保留 `ProgressionService.Reforge` / `ReforgeQuote` 依赖。详见 `Economy-Integration-Review.md`。 | 真实成长节奏、UI理解与经济平衡。 |
| B 祝福 | 战意10%、鹰眼+20pp、锋芒215%，基础165%不变；伙伴仅乘一次战意，暴击卡限主人直伤。 | `BlessingDamageProductionTests.py`、组合预算与可达性检查；主人/伙伴重复乘负对照。 | 预算按固定命中假设，不是实战DPS或最优配装结论。 |
| C 可碎冰 | 区分霜痕与当前可执行动作；复用实际瞄准、确认落点、范围、LOS和起手许可；蓄力期间保守不显示可碎冰。 | `ShatterAvailabilityTests.py` 真实 Player/HUD 查询及旧霜痕快捷判断负对照；包含8/10/13米、近无霜远有霜、隔墙、选点。 | 延迟落地时目标是否仍有霜痕无法由当前提示保证。 |
| D 守门/晶核 | 3.5秒只累计真实移动，返岗不打断已承诺预警；晶核两敌对象标记及2/1剩余。复用已有攻击/控制中断规则。 | `GuardReturnTests.py` 132项+旧计时负对照；晶核真实击杀回调与对象文字生命周期测试。 | 真实地图追击体感、标记密集时辨识。 |
| E 失败原因 | 保留死亡/超时/主动退出/生成或路径失败；建议读取真实受伤、治疗、目标状态证据。无截止计时房间不额外加入时限。 | `RoomFailureEvidenceTests.py`、章host实际状态/summary接线及失败被错误归为放弃的负对照。 | 设备交互与建议是否足够有用仍需用户试玩。 |
| F 灼燃终击 | 保直伤；先结算到期离散burn事件，再按己方既有burn、最高强度刷新规则兑现未来≤3秒事件并清对应未来；每cast/target一次，不加暴击/回能/proc。 | 本批 `BurnFinaleProductionTests.py`：真实 `EnemyStatusEffects`、实际 `ElementalAdvancedArea` 与四个精确负对照。**三级尾场仅验证生产发射/CombatArea接线不含兑换入口，未模拟完整尾场伤害流程。** | 无完整真实战斗trace、实际尾场播放或与祝福组合的实战平衡结论。 |

## 本整合批执行范围

新增默认入口：`TacticalLiveVisualTests.py`、`CompanionAppearanceTests.py`、`LargeBossShutdownTests.py`、`BurnFinaleProductionTests.py`。均接受 dotnet 可执行文件路径；不复制玩法实现。

先执行全部65项 `*SourceTests.py` 与全部runtime缓存Unity2021.3.33参考API编译；然后只跑新增与直接受影响的生产fixture。没有在本批跑全aggregate、push或三端构建。PR14返营修复、PR16测试alignment与预算/遮挡审查修复统一继承后，再由父任务运行完整聚合。当前参考编译不代表项目Unity6000.6、Editor执行、平台包、GPU/截图、录像或设备性能验证。

整合提交 `7325777` 时的定向结果（非最新合并头的完整aggregate）：战术视觉24项+2负对照，伙伴结构/收束184项，首领通道/停机35项+时间步负对照，燃烧终击106项+离散调度2337项+4负对照；强化后的姿势提交45项+2负对照，暂停模拟15项+2负对照，守岗132项+1负对照，伙伴意图27项，章节宿主77项+4负对照。章节宿主仅补视觉Attach场景壳并抽取真实 `LiveRoomEnemy`，保留原断言与经济重铸依赖。Windows默认、Android、iOS三种符号的缓存2021参考API编译均零错误/零警告。


## 最终审阅：修复已继承，待统一聚合与实机证据

B01–B10、策划A–F共16项均有上表对应的生产实现或明确复用路径，未漏登记项；这不表示审查修复与实机验收全部结束。以下事项尚不能标为最终完成：

- **PR14返营冻结修复已继承**：`f17ff0b` 在成功世界切换及角色丢弃后重算模拟时间；`ChapterReturnTimeScaleTests.py` 已注册默认入口。几何合并前，本最终树完成126项生产链断言及两个精确负对照，章节host另完成77项及四个负对照；已保留经济类型与真实 `LiveRoomEnemy` 测试依赖。当前最终合并头仍待统一聚合。
- **PR16审查修复已继承**：五个受影响fixture的对齐、`29cac20` 主元素形态先于可选粒子申请预算，以及 `4062bad` 遮挡三角/Lightning动画修复均已通过 `c149965` 继承。`ElementalPriorityProductionTests.py` 与 `AnchoredImpactCoverageTests.py` 均在默认入口；预算定向111项及两个精确负对照此前通过。最后几何合并仅核对入口并集，不将父提交证据代称当前头整套通过。
- **B05结构矩阵已补齐**：7,358项检查与三个编译后精确失败负对照来自实际Hero/装备/时装构建和网格recipe，包含T4三件、+10及最高时装；不是只测另一份尺寸公式。材质/cloth及引擎生命周期用受管替身，未检验所有动画瞬间的相交或实际画面。
- **B10的保留范围**：提交后技能姿势已分族；generic蓄力未分族，Tier轮廓/强化工艺复用原结构+B05而非新增。Unity真机施法观感、动态穿插、导能槽/分件停机画面均无验收证据。
- **F尾场证据不足以称完整回放**：三级尾场只验证实际生产接线不能再次兑现burn；完整尾场伤害时间线、与真实怪物/设备帧率共同运行未执行。

尚未执行的共性验收包括Unity6000.6 Editor/平台包、真实截图录像、真机触控/中文显示、六场真实试玩与帧时。修复已实际继承，最终统一聚合仍待父任务执行；不将旧测试总数包装成最新HEAD的全套通过。本次合并保留默认入口完整并集，未跑full aggregate、未push。
