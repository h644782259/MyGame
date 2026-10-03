# Blender 美术剩余有限清单（PR35 后实施）

盘点起点：Windows `0daa0f2dec8b53f695888032e3972b24a8145773`，iOS `c6c230ca023ed2dcc4ebede44a6d516399b103fa`。已核对远端 main 仍为已审PR30–32基线；PR35独立冻结验证，后续在单独工作区与分支制作。范围止于下列对象及现有 4×10 技能槽，不增职业、敌种、时装系列、建筑种类、技能或新动画系统。不生成视频。

状态说明：`保留`表示已有有意设计的构造且不应重复制作，并不代表 Unity 实机验收；`制作`表示有清楚运行时入口的剩余候选；`核验后定`必须先看正确构造/姿态再决定，不以导出文件数凑完成率。每项关闭需要实际调用/回退证据，未用资产不得算接入。

## 已做内容：保留，不重做

- [x] 剑卫旋风/裂地预演资产已由 `PlayerController` 开篇技能分支 → `BlenderSkillVfx.TryPlay` 接入；旧 Ring 回退存在。
- [x] `AuthoredActorMeshes.Key/Apply` 已覆盖 torso/肩甲/头盔/鞋、武器护手/弓肢/法杖晶冠、狼、史莱姆/幽灵核心、Guardian 锤、星盘机身/开合甲。保留可靠部分，不能宣称这些模块等于全角色完成。
- [x] `FilledSkillVfx.EnsureAssets` 已加载 10 类 SpellBases 与 IcePrimary/FirePrimary；E 中 BladeSlice/ForkPulse/ContractSigil/ProtectionCage 已接入。沿用现有终击确认、优先级池、范围遮挡契约。
- [x] Tent/Campfire/Crate 已脱离角色试点开关接入；Pot/Rubble/OpenCanopyTree 由 `BlenderSceneryArt` 接入。火焰仍由原 WorldMotion 驱动；Tree 注册/遮挡已保留。
- [x] E02 真正清毒后立即撤持续毒光、同帧重施安全、Burn 普攻反馈按真实状态；不得把绿爆尾光当持续毒层恢复。
- [x] E04 小人形/Guardian 击倒适配已有实际工厂姿态/接地检查；不另建动画控制器。
- [x] 旧 ActorModules/Constructions 图未过滤 disabled renderer，已留档说明；后续仅用 EnabledReview 与修正 exporter。不能引用旧图认定装备遮挡缺陷。

## 批 F1：三职业及伙伴完整轮廓闭环（最多 5 个角色身份）

实际链：`PlayerController.Initialize` → `CombatModel.Hero` → `BuildHero/EnhanceHero/BuildClassCostume` → `ApplyEquipment/ApplyFashion`；藏品 `CollectionModelPreview` 同一 Hero 工厂。伙伴 `SummonedCompanion.Create` → `CombatModel.Companion`，并由 `.CompanionAppearance` 添加契约/等级结构。

- [ ] 元素师：**核验后定**。ClothTorso/肩披/法杖核心保留；检查 `BuildHero` 帽檐/帽冠及 `EnhanceHero` robe/stole/帽尖和 `Costumes.BuildClassCostume` 高领，必要时只做帽冠与袍片刚性模块。保留 neck/spine/mantle、TailoredCloth 运行时垂布，不能整角色启用五 clip Vanguard 试点。
- [ ] 游侠：**核验后定**。保留 Hood/羽毛/箭袋/弓腕骨和弓弦。优先检查现有 hood face aperture、quiver lip/fletching 在真实站姿/拉弓姿的识别；必要制作 hood/quiver 2 件，不先缩胸包。已测装备胸包为旧结构、远侧弓部分遮挡不能单凭灰模图定新 bug。
- [ ] 唤灵师：**制作候选**。保留木图腾与现有叶肩；冠角/分枝角、仪式短裙/叶披风组成一个可识别完整轮廓。对应 `SummonerCrown`、`.Costumes.BuildClassCostume`；只替换有名称的刚性件，保留法杖与灵珠动作。
- [ ] 星灵：**制作候选**。`CombatModel.Companion(Spirit)` 的 Star spirit 核心保留；Star crown、Spirit wing 形成完整月冠/双翼轮廓，绑定现有 `CompanionRigidParent`，核心软缩放不能缩坏冠翼。保留永久契约纹章/等级点缀。
- [ ] 树灵：**制作候选**。保留 BarkTorso、躯干—肩的实际交叠与现有关节；把 Leaf crown 的 3 球冠与通用袖/手轮廓做为树冠/树皮手臂/根足模块，最多一套左右复用。不得因为静态“悬挂感”重定位肩关节。现有左右肩有 4/2 唯一顶点进入 torso；需新增移动/攻击/召回姿态核验。

共同关闭条件：每身份一张可见 renderer 的彩色组装图 + 一张关键动作姿态图（Blender actual factory/TRS 明确标注）；base 与代表装备都能辨认。保留形体可判合格关闭，不能强制每身份新增 FBX。移动端共用缓存 mesh/既有 palette，不增加独立 Animator/碰撞体。

## 批 F2：主要敌人 / 首领完整轮廓（4 敌种 + 2 首领形态）

实际链：`EnemyController.Initialize: CombatModel.Enemy(kind,boss)` → `BuildEnemy/EnhanceEnemy`；大型遭遇 `InitializeLargeExpedition` → `CombatModel.LargeExpedition` → `LargeBossRig.Build`。

- [ ] Slime：**核验后定，倾向保留**。Slime 已有扁底模组+眼白/瞳孔/嘴/高光；检查缩放动画下面部跟随与正/背读形，合格即关闭，不加盔甲等新身份。
- [ ] Goblin：**制作候选**。精修实际 Long Ear/Leather Cap/Goblin Knife/鼻眉组合；现有四肢、spine/headRig 与击倒恢复保持。最多耳、刀、帽沿 3 种复用模块。
- [ ] Wisp：**核验后定，倾向保留主体**。SpiritCore 已替换，原 swept horn / Spirit Crown / tail / eyes 可决定保留或仅补尾焰剖面；不把它误做星灵伙伴配色版本。
- [ ] Guardian 普通与 boss：**制作候选**。保留 Hammer、胸块、既有 boss 比例/冠刺差别；改 Guardian chest plate、layered pauldron、Iron Crown 的同套石甲模块。普通/boss 必须共用动画轴，保留冠晶伤害/控制反馈。
- [ ] 大型星盘：**保留机身，补局部候选**。BossHousing/BossPlate/核心/爪足已有完整组装；仅 Power anchor plinth/Breakable crystal/Anchor claw 可共用精修锚点组件。`LargeExpeditionBoss.BuildAnchors` 的锚点血量、存活 mask、交互距离、阶段不得变化。环轨/光束炮口朝向/暴露开合/停机连接仍由真实 rig 驱动。

关闭条件：敌种/普通-vs-boss可辨认，面部/重锤/星核在真实镜头下可读；小人形沿用 E04 接地与动作回退检查。不得给每个章节刷怪重复导出同模型。

## 批 F3：武器 T1/T4 与时装结构（3 武器族 + 现有时装）

实际链：`CombatModel.ApplyEquipment` → `BuildEquipmentWeapon`、`BuildEquipmentArmor/BuildClassEquipmentArmor`、`BuildClassTierGeometry/BuildClassUpgradeGeometry`；`ApplyFashion` → `.Costumes.BuildFashionWingShape`、`.CostumeLayers.BuildWeaponFashionShape`。握点/轨迹用 `WeaponStructure` + `CombatModel.WeaponRig`，不按 FBX bounds 推断攻击长度。

- [ ] 独立 StarcoreSword：**明确待接入**。`Resources/BlenderPilot/StarcoreSword.fbx` 当前没有独立运行时加载调用；不要把 Vanguard 内嵌剑算它已接入。优先沿原 Sword Wrist 将独立剑拆/适配成可沿 SwordRoot→SwordTip 缩放的 blade 与固定 grip/guard/pommel。只在精确 starter kit/base 路径启用首版；其它装备仍准确回退。若整把导入无法满足 tier 插槽，明确选择复用其源 mesh 分部导出，而非同时显示旧剑+新剑。
- [ ] 剑 T1/T4：**保留已优良刀刃/护手，核验成长**。T4 的 Guard wing/Blade fang/刃脊/符文必须不被独立剑盖住；剑长取 `weaponStructure.SwordTip-SwordRoot`，原 6 sockets/ribbon 仍指当前视觉。升级等级的 forging mark 保留。
- [ ] 弓 T1/T4：**制作候选**。弓肢已离线；检查 Bow tip/Bow horn/握把/箭台并按需要统一木金接头模块。保持弦三点与 BowReach；不替换动态弦或拉弓附件关系。
- [ ] 杖 T1/T4：**核验后定**。Focus crystal/晶冠已有模块；可补 Inlaid staff/collar 剖面，法杖上下端、caster 两职业身份和 tier prongs 必须保留。注意 Arcane Crystal 原始创建为 Cube 后又重设 scale，不能仅给 Sphere 名称映射便宣称覆盖。
- [ ] 翅膀和武器时装：**保留已独立结构，检查组合**。Common/Rare/Epic/Legendary 的实际 Feather/Crystal/Mechanical silhouettes 与低稀有武器几何已存在，先跑现有 128 结构与装备组合检查。只有遮住握点或 silhouette 相同且有组装证据才局部修；不新添时装目录。

最小矩阵：4 职业 × T1/T4 × 无时装/各职业最高现有时装，共16个组合；中间层级沿现有结构规则验证，不逐件造重复模型。源资产和真实启用路径都写明。

## 批 F4：固定场景模块（6 个对象组）

- [ ] 石柱：**制作候选**。`WorldBuilder.Pillar` 的 base/shaft/capital/collar/band 为一组可参数化高度的分件，不固定整柱高度；保留 `WorldTraversal.AddBox(1.4×1.4)` 与 Column/Capital 遮挡标记。野外/地城共几何不同现有 palette。
- [ ] 门户：**制作候选**。`WorldBuilder.Portal` 的 frame/plinth/固定晶座可离线；Gate light/WorldMotion/PortalFocus/motes 保留动态且交互 jade 色不改。入口位置、触达范围、portal ring 旋转不能由新 mesh 决定。
- [ ] 建筑屋顶：**制作候选**。`WorldBuilder.Hubs.BuildTown` → `.AuthoredScenery.BuildWorkshopRoof`；已有坡屋顶/屋檐/烟囱是程序细分，不是 Blender 模块。做坡瓦/檐脊/烟囱帽共用套件，另观星拱廊屋顶只做其已有结构。保持 BuildingWidth 导航 footprint、BuildingOcclusionGroup 与 Mark 标记。
- [ ] 城镇设施：**制作候选**。采石 forge/anvil/street furniture 与观星台 armillary 使用现有 BuildTown 分支；静态台座/炉体/书架可离线，旋臂/星图需保留 WorldMotion/HubNpcIdle 轴。与 NPC 工位共用砧/货架，不能当两个资产重复计完成。
- [ ] 营地 4 功能台：**制作候选**。`WorldBuilder.BuildCampFacilities` 中 STAR CORE / APPRENTICE / CODEX / CLASS TRIAL 共用台座并提供有限4顶饰。进度颜色/抬升、中文世界标签、导航圆 `.48` 原样；禁为美术增加交互功能。
- [ ] 城镇 NPC 3 人：**制作候选**。`WorldBuilder.Hubs.BuildHubNpcs` 的 merchant/smith/exchange，保留 `HubNpcIdle.Initialize(index,right,left,dial)`。共用一套躯干四肢模块，帽/围裙/卷轴或星图区分职业；保留脸朝向、手工具、工位/标签/导航。它们不走 CombatModel.Hero，不可只升级角色模块便声称 NPC 完成。

保留：当前 OpenCanopyTree、可破坏pot/rubble/crate、营地tent/fire、已风化岩体；场景水道/桥面/房间地形/章节路线/动态障碍/危险判定保持运行时，不整场景烘焙硬替换。新增场景文件建议增量 runtime 源预算≤1 MiB、复用≤8 palette slots、无新贴图；超预算先缩减模块而不是悄加 atlas。

## 批 F5：普通弹体、状态/领域与首领视觉（有限6项）

- [ ] 普通箭：`CombatProjectile.Friendly/BasicShot` → `Make(arrow=true)` 当前 Spectral Arrow capsule；接入已有 Arrow 羽翎/箭头 mesh 或小型新 mesh。必须只替换 visual body，保留 radius/speed/lifetime/tracking/volley/castId/VisualOrigin、实体碰撞时点。不要用已升级 ArrowRain 声称普通箭已升级。
- [ ] 普通法弹/星灵弹/敌弹：同一 `Make(arrow=false)` 当前 Arcane Bolt sphere；在 Friendly/Hostile 分派后给 ≤3 个**装饰身份**（元素/契约/敌方），保留共享动态运动与红色敌我提示，禁止依 tint 猜真实伤害元素。
- [ ] 陨星实体：`CombatArea.Spawn` 的 Falling Meteor 已将初始 sphere 替换为 WeatheredRock，是独立视觉实体；评估切面/熔沟石核是否有实质改进，只动落体外观，警示/落点/延迟来自原 area schedule。
- [ ] 持续状态：`EnemyStatusEffects` → `ElementalCombatVfx.OnEnemy` → `ElementalEnemyAura`。Fire/Poison已有粒子与FieldVisual主体，默认保留；Freeze/FrostMark/Mark 的增补仅在真状态存在时创建，复用撤除与同帧重施语义，不能把装饰寿命当状态计时器。
- [ ] 领域：`CombatArea` → `ElementalFieldVisual`、FilledSkillVfx，按 Ice/Fire/Lightning/Poison/ArrowRain/Spirit/Steel 核查已有离线主体；默认复用。边界、覆盖裁切、末击确认不能变成烘焙贴图尺寸。最多修缺失 recipe 的主体，不每技能各造一套相同圆环。
- [ ] Boss 身份：普通 Guardian 的已有扇形/冲锋/投射身份与大型星盘的 anchor/core/rotating beam 保持差别。可补锚点破裂/核心暴露接触装饰；`EnemyAttackTelegraph` 与 `LargeExpeditionBoss.DrawBeam/ClipBeam/BeamContains` 的地面危险线、interrupt symbol、timing arc全部仍为运行时真实形状，不替换。

## 全40技能逐槽核验表（不等于40套新资产）

关闭一行须查看实际开篇/持续/真实命中/结束状态；rank1/rank3覆盖新增段落，rank2用相同逻辑规则验证。`保留`也需要对应调用明确；若被动没有动作，不人为加施法效果。

| 职业/槽 | 技能 | 真正入口/身份 | 剩余决策 |
|---|---|---|---|
|剑0|旋风斩|PlayerController → BlenderSkillVfx.TryPlay(false)|保留，rank追加范围仍原CombatArea|
|剑1|裂地冲击|PlayerController → TryPlay(true)/Melee|保留，核验击倒与裂地不混淆|
|剑2|剑刃风暴|PlayerController → CombatArea Steel|复用已离线blade族，核验整段/收束|
|剑3|剑术精研|被动成长|无新效果|
|剑4|圣盾反击|PlayerController guard + Rune/反击|现为通用Rune；按真实guard状态接入ProtectionCage并核验提前结束|
|剑5|破军突进|AdvancedSkillSequence.Vanguard case5|保留动态路径，复用blade contact|
|剑6|生命战旗|AdvancedSkillSequence.Healing|现保护身份保留，旧Beam fallback保留|
|剑7|大地崩裂|AdvancedSkillSequence.Vanguard case7|保留每段真实落点，复用rupture主体|
|剑8|不屈意志|受伤触发被动|核验真实触发，无独立新演出|
|剑9|终焉裁决|AdvancedSkillSequence.Vanguard case9|保留主裁决/剑阵/终击确认|
|元0|冰霜新星|PlayerController + CombatArea Ice|保留IcePrimary；rank3弹片归普通弹体任务|
|元1|陨星术|PlayerController + CombatArea Fire|落体实体归F5，其余保留|
|元2|奥术风暴|PlayerController + CombatArea Lightning|核验雷体持续/收束，复用ForkPulse|
|元3|奥能亲和|被动成长|无新效果|
|元4|雷霆锁链|AdvancedSkillSequence.ChainLightning|E03 ForkPulse端点保留，连线仍动态|
|元5|冰晶护体|PlayerController guard|按Burn/Shatter真实状态区分，不固定冰蓝|
|元6|奥术回流|AdvancedSkillSequence.Healing|复用保护/回流身份，核验治疗时序|
|元7|虚空漩涡|AdvancedSkillSequence.Arcanist case7|检查Arcane lattice与吸附中心，动态聚怪保持|
|元8|法力屏障|受伤触发被动|现通用Rune需按真实触发与passive状态核验，复用Cage|
|元9|天灾终章|AdvancedSkillSequence.Arcanist case9|现火冰交替/实际终击确认保留|
|游0|扇形箭|PlayerController → CombatProjectile.Friendly arrow|普通箭F5；保留扇形方向/同目标预算|
|游1|震荡陷阱|PlayerController → CombatArea Neutral|核验陷阱主体身份，若仍无辨识只补小型机关芯|
|游2|天幕箭雨|CombatArea ArrowRain|E01同步实体箭保留|
|游3|弱点洞察|被动成长|无新效果|
|游4|逐风步|AdvancedSkillSequence.Ranger case4|普通箭复用，运动轨迹/加成计时保留|
|游5|毒蔓牢笼|AdvancedSkillSequence.Ranger case5|Vine已有离线主体，E02清层语义保留|
|游6|森林祈愿|AdvancedSkillSequence.Healing|现保护/恢复身份核验，复用|
|游7|幻影连射|AdvancedSkillSequence.Ranger case7|普通箭F5，真实目标/节拍保留|
|游8|灵风庇佑|受伤/规避被动|只真触发反馈核验|
|游9|万箭归星|AdvancedSkillSequence.Ranger case9|同步箭批/末爆保留，普通放射箭复用F5|
|唤0|灵能冲击|SummonerSpell.Cast skill0|E03 ContractSigil contact保留|
|唤1|荆棘牢笼|SummonerSpell.Cast/Update|核验灵契与毒蔓视觉区分，复用枝形不改变控制|
|唤2|灵狼契约|SummonerSpell → CastContract Wolf|狼已升级；只检验召唤主体/契约色|
|唤3|灵魂共鸣|被动成长|无新效果|
|唤4|星灵契约|SummonerSpell → CastContract Spirit|完整伙伴轮廓F1，召唤环复用|
|唤5|灵魂护盾|PlayerController guard|现guard Rune需接入ProtectionCage，伙伴分摊仍按真实反馈核验|
|唤6|回春共鸣|SummonerSpell|核验玩家/伙伴实际受治疗时刻，复用|
|唤7|引力印记|SummonerSpell|核验真实标记目标与拉扯中心，动态目标不烘焙|
|唤8|灵体庇护|被动触发|只真触发反馈核验|
|唤9|远古树灵|SummonerSpell → CastContract Treant|完整伙伴轮廓F1，保留已存在契约主体|

## 执行及预算终点

建议顺序 F1→F3→F2→F4→F5（弹体可与F4独立）。每批 draft PR、精确头提交、父任务审查；Windows/iOS同步，Android只同步源码，不创建仓库/处理登录。每批只围绕以上身份，禁止延伸到未列内容。

预算初值：新增 actor rigid module≤36种、每种≤512 triangles；单个新增完整静态NPC视觉目标≤2k triangles；武器每套可见 mesh≤800 triangles；场景每模块≤2k triangles；普通弹体≤128 triangles；VFX主体每实例新增≤256 triangles且服从现有pool/低档上限。以上是审查预算，不是已测数据；实际替代若超原面数需写明收益，不能以总离线资产数代替实例成本。复用既有材质/调色板，默认0新增贴图；任何新 atlas须明确内存/包体实测缺口。

统一交付：可编辑blend、rebuild脚本、来源、GUID不重建、实际工厂接入与关闭回退、资源source字节与单实例tri/material计数、新图（无需视频）、原检查日志；Blender/managed/Unity边界逐项写清。运行时碰撞/范围/时序/成长数值不得改变。通过资产接入/回退与完整有限清单分类后结束；Unity设备未有工具只能如实列未验收，不继续无限制作来回避验收缺口。
