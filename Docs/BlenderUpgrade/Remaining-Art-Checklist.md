# 有限美术清单：F1–F6 集成收口

集成源码快照：Windows `bc6a572a86fe4376f6f346b0fe52f20d9678aec3`。原始请求基线 Windows `25096ba7dbf6e9d7ba9463ec29104c2c462da426`、iOS `1cd7ce36c77064757899788e23520fb796266888`；PR35 后的有限制作起点为 `0daa0f2`。PR35 已由父任务合并至 main：Windows `4214b885b5346b4e4987e9d50a3abf27f329db5c`、iOS `c1a49acd52cac28d1408a6d9bb25af971f7e3343`；父任务确认同步合入不改变该集成树的 Assets/Tests/Tools。当前 iOS 资源对照快照为 `30f748b1bf6da7def9d91118aa756cd225c03e79`。

以下勾选表示有限对象已经获得“实际接入新资产”或“有依据保留现状”的明确结论，不表示 Unity 实机或艺术终审通过。F1–F6 已共同进入上述 Windows 集成树；**整合后的233项全量检查及三平台API编译已通过，1,785项输入文件运行前后不变；报告见`Docs/Validation/Final-Combined-Frozen`**。各批独立日志不能代替该最终联合结果。此次仅文档/清单审计，不新增生产修改、视频、Android 包或远端操作。

## 已有成果：保留

- [x] 用户认可的旋风/裂地技能方向已在真实 `PlayerController` 释放点 → `BlenderSkillVfx.TryPlay` 接入，保留原伤害与失败回退。源和日志：`ArtSource/BlenderVfx/`。
- [x] 既有 ActorModules、狼轮廓、Vanguard 动作、TacticalAttachments、E02 真状态反馈、E04 击倒、E06 池与箭批 generation 修复保留；不重做动作控制器、不启用不完整角色 FBX 试点。证据：`ActorModules/WolfV2`、`VanguardActions`、`TacticalAttachments`、`EnemyKnockdown` 及对应 Tests。
- [x] 已接入的 tent/fire/crate、pot/rubble/open-canopy、SpellBases 与四类技能身份继续使用。旧 PR30–32 未过滤 disabled renderer 的组装图仅作历史；新判断使用 enabled-filtered 实际工厂输出。

## F1：三职业及两伙伴（5 项已分类关闭）

运行链：`CombatModel.Hero/Companion` → 既有构造/装备/动作 → `CombatModel.Part` → `ActorSilhouetteF1.Apply`。通用 Head/Sleeve/Boot 等键仅在真实 `treantCompanion` 为 true 时替换；不误改其它角色或敌人。

- [x] 元素师：保留已合格尖帽/帽檐、高领、法杖关节、TailoredCloth；新增折面袍片/刺绣裙摆，不另造帽模型。
- [x] 游侠：新增开脸兜帽、空心箭袋口、三个原插槽的羽箭；保留胸包、腕骨/拉弓/弓弦。未将旧胸包或远侧弓遮挡武断定为缺陷。
- [x] 唤灵师：角与枝角改曲线锥形；保留叶肩、短仪式服、图腾、灵珠、披风及成长结构。
- [x] 星灵：新增月冠和三瓣双翼；双翼绑定 `CompanionRigidParent`，保留核心软缩放、契约/等级结构。
- [x] 树灵：新增四瓣树冠、树皮脸/四肢/手、根足；保留 BarkTorso、肩原点/关节及契约结构。实际动作双向交叠检查保留，不依据静态图随意移关节。

证据：`ArtSource/ActorSilhouettes/F1/Review/{Arcanist,Ranger,Summoner,Spirit,Treant}-{Before,After}.png`；`tests.log`、`attachments.log`、`render-equivalence.log`。44 个实际构造/姿态组合；12 模块/1,042 唯一面/112,680 源字节。缺失、坏数据或 `Enabled=false` 保留旧网格。未测 Unity 实时布料/弓弦着色。

## F2：主要敌人及首领（5 项已分类关闭）

运行链：`CombatModel.Enemy` → `EnemySilhouetteArt.ApplyEnemy`；大型遭遇 `LargeExpeditionBoss.CreateAnchors` → `ApplyAnchors`。

- [x] Slime：保留扁底、脸与缩放动画；前后实际几何/配色及四动作相等核验，无多余新甲片。
- [x] Goblin：新增折耳、皮帽/帽沿、弯刀；保留脸、关节与 E04 动作。
- [x] Wisp：保留核心、角冠、尾与脸；真实几何/配色及四动作相等，不改成伙伴星灵。
- [x] Guardian 普通/boss：新增胸甲、层叠肩甲、开口齿冠；保留锤、boss 比例/角/冠晶与控制/死亡所有权。真实击倒检查肩支撑接地。
- [x] 大型星盘：完整机身保留；新增锚座、破坏晶体、笼爪，五件实际锚点构造从 1,644 面到 360 面。锚位置/血量/mask、阶段/炮口/停机保持原逻辑。

证据：`ArtSource/EnemySilhouettes/{Before,After}/Enemies-{front,back}.png`、`Assemblies/Anchor-pair.png`、`Astrolabe-retained.png`（位于 Assemblies）、动作对照和 `Down/`；`production-validation.log`、`review-validation.log`、`support-evidence.json`。9 模块/832 面/89,964 字节；各资源坏数据回退，工厂开关保留旧构造。

## F3：武器与既有时装（5 项已分类关闭）

运行链：`CombatModel.Hero` 与 `ApplyEquipment` → `ApplyWeaponArt`；仍通过原 `WeaponStructure`/`WeaponRig` 握点与轨迹驱动。

- [x] 独立 StarcoreSword：读取原 `Emberfall-Pilot-Props.blend` 同名源，拆 blade/guard/grip/pommel 四件实际接入 base 或精确未升级 common level1 初行长剑；其它装备回退对应 tier 结构。没有将未加载的整把 FBX 算作接入，也不重复显示旧剑。
- [x] 剑 T1/T4：保留 T4 刃/刃脊/护手/牙/锻印；剑长仍由 SwordRoot→SwordTip 决定。原5公开锚点、ribbon root/tip不变；第6源 emission socket只作来源记录。
- [x] 弓 T1/T4：统一弓接头、杆件/小饰件低模，保留握点、弦三点、箭台/nock与 BowReach。
- [x] 杖 T1/T4：低模杆/晶体小饰件；保留两施法职业核心、晶冠、tier prongs与原腕轴。
- [x] 翅膀/武器时装：保留现有时装目录和背翼；武器机械环等沿原结构降面。4职业×T1/T4×无/最高时装16组，含真实最高 upgrade10，不新增系列。

证据：`ArtSource/WeaponModules/Factory16-{Before,After}.png`、`WeaponDetail-{Before,After}.png`、`production.log`、`contact.log`、`instance-budgets.json`。可见武器集116–718面（含持箭/武器时装，不含背翼），8模块/476唯一面/51,504字节。源blend/FBX/atlas未变；4源件任一缺失则整把旧剑回退。

## F4：固定场景（6 项已分类关闭）

运行链：`WorldBuilder.Primitive` → `AuthoredFixedScenery.Apply`，`Ring("Gate frame")` → `Frame`，既有设施构造 → `Crest`。原导航、材质所有权、遮挡登记不变。

- [x] 石柱：base/柱身/柱头/领环/束带分件，仍参数化高度，1.4导航箱和两遮挡标记不变。
- [x] 门户：离线石框/基座，保留 jade 内光、晶体、motes、PortalFocus与交互/移动轴。
- [x] 屋顶：工坊坡瓦/檐/脊/烟囱帽；观星 dome/spire 读形已合格保留，不硬换整个建筑。
- [x] 城镇设施：货架、砧/木墩、讲台、观星台；forge gantry和运行时星冠保留。NPC工位共用资源，不重复计数。
- [x] 四功能台：共用台座+4小顶饰，保留进度晶体/中文标签/.48导航圈，不新增功能。
- [x] 三 NPC：共用衣身/袖/靴/头，商人帽、铁匠围裙区分；原工具、表情、右左臂/星盘 idle轴、工位和标签保留。

证据：`ArtSource/FixedScenery/factory-{0..5}{,-before}.png`、`role-{0..2}-{before,after}.png`，`comparison-production.log`、`role-render.log`、`factory-snapshots.json.gz`。27模块/322,596字节、单模块最多384面、0新增纹理；full-town材质数是已有聚合palette，不误报为新增材质。缺失/禁用回退、坏数据/导航/遮挡组相等有实际工厂检查。

## F5/F6：弹体、真实状态、领域、Boss（6 项已分类关闭）

- [x] 普通箭：`Friendly/BasicShot → Make → AuthoredProjectileMeshes.Load("ArrowBody")`，416→48面；未将 ArrowRain 当作普通箭。
- [x] 法弹/契约弹/敌弹：显式源身份选 `CasterBolt/ContractBolt/HostileBolt`，不是依颜色推断伤害类型；原轨迹、红敌色、trail、碰撞/volley不变。
- [x] 陨星：`CombatArea.Spawn` 使用 MeteorRock，WeatheredRock仍为回退；192→116面，原落点/延迟/二次下降曲线保留。
- [x] 持续状态：Fire/Poison绑定实际 EnemyStatusEffects；清毒/消费、epoch/死亡/状态源消失即撤，暂停保留真实存续，同帧重施安全。Freeze/FrostMark/Mark/Slow保留真实状态HUD及接触反馈，无虚构常驻光效或被动施法。
- [x] 领域：现有 Steel/Ice/Fire/Lightning/Spirit/ArrowRain/Poison主体保留；Neutral首次陷阱准备新增clipped TrapCore，原age>=delay退场，Decoration lease 32/20/12限额，原rank2尾段不重复假陷阱。动态边界/末击确认不改。
- [x] Boss身份：Guardian真实冲锋/扇形等预警与星盘core/anchor/beam/interrupt/shutdown保留动态；F2锚点、F5 hostile弹体现已接入。危险形状不离线替换。

证据：`ArtSource/BlenderProjectiles/Factory-Review.png`、`factory-samples.json`、`validation/`；6源网格/344面/37,224字节。F6状态与防护原日志、新实际MPB对比图和body-clearance在 `ArtSource/DefenseIdentity/`。缺失坏资源回退原primitive/rock，缺失trap只保留原marker。

## F6：全40槽完成分类

- [x] [SkillCoverage.md](SkillCoverage.md) 保留完整40行：实际准备、命中/持续、结束与保留/新接入结论；F1/F2/F5依赖已更新为集成调用。
- [x] 3类主动防护与4职业slot8只在真实触发时调用 ProtectionCage，按独立owner/channel与实际timer撤除，普通技能castId不误取消护盾。
- [x] 持续 Protection 专属身体包络，普通Charge/治疗/反击默认尺度不变；同一网格/低档1part。最终真实F1/F3装配与动作59姿态×28触发/时点三角交叉检查通过；Y缩放下限2.6、XZ缩放下限仍2.6（不是世界空间米数），专属motion20从出生保持身体包络，旧高度和旧出生缩放负控均失败。证据见`ArtSource/FinalBodyEnvelope/`；原state/epoch/暂停/立即释放/同帧复租/刷新专项保留。源半径、伤害、时序不变。
- [x] slot3/8无主动施法；动态路线、布局、碰撞、危险/目标边界、beam/ribbon、LOS裁切继续运行时生成。

## 有限制作终点与未验收项

本清单没有未决制作候选。保留项是明确审查结论，不为提高模型数量重复离线制作。源预算和GUID以 [Cumulative-Budget.md](Cumulative-Budget.md)、[ResourceBudget.json](ResourceBudget.json)、[GuidPreservationAudit.json](GuidPreservationAudit.json) 为准；不把源字节当包体、唯一库面数当同屏实例成本、材质引用当drawcall。

源码已同步且联合专项／全量检查已通过；仍待父任务完成draft PR审查及合并。Unity importer/shader/实时物理、游戏机位动态可读性、Windows/iOS设备帧时/包体/内存未验收；managed与API编译不能代替。Android仅同步源码，不新建仓库、不制作包、不处理登录。该验收边界不会触发无限增加美术范围。
