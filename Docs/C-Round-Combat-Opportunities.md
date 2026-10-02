# C03：真实机会与兑现反馈

动作槽读取 `PlayerController.SkillOpportunity(skill)`，不写目标、资源或技能冷却。霜痕状态仍可留在 HUD；只有技能已学、能量/冷却/动作门禁通过，当前实际陨星落点覆盖霜痕且无遮挡，才给陨星槽显示“碎冰 + 状态剩余秒数”。手动固定敌人被遮挡或技能超距时，不把其他目标的状态作为可用承诺。

燃烧路线把陨星“续燃”和终裁决“兑燃”分开。后者必须覆盖当前角色、当前 epoch 的既存燃烧。倒计时读实际状态剩余量，不表示延迟技能未来必定命中或能保留这份状态。游侠三毒只读自己的三层毒，提示对应扇射；飞行箭仍按真实碰撞结算。反击提示对应普攻；强化契约窗口只对应 2/4/9 三个契约技能，读取原 16 秒窗口，不把免费集火/召回当施法。

终裁决维持原先顺序：结算到期旧燃烧 → 本次命中增益 → 合并/领取未来燃烧 → 直伤 → 尝试兑现。`BurnFinaleSettlement.Apply()` 只有实际生命值下降才返回成功。直伤先杀死或拒绝伤害不会产生成功计数/接触效果。原每目标每 cast 账本仍负责去重；玩家只保留最近一个 cast 的成功目标数，2 秒游戏时钟后失效，换房 epoch/退役角色不可继承。真实成功才调用 `CombatFx.BurnContact`，沿用全局效果容量。强化伙伴反馈读取伙伴生产事件，一次命中显示一次事件，不冒称整次指令的目标总数。

玩家 Melee/HitArea/ElementalAdvancedArea 全解析（包括可破坏物）进入 `CombatImpactBatch`；正常、提前返回和异常都在 finally 结束。模型蓄力调用传真实 `charge.SkillIndex`。召唤实体出现使用 ActionBody；阵地周期落点提示使用 SustainedBackground，因为此时不保证命中。

验证入口：
- `BurnFinaleProductionTests.py`：真实状态组件与玩家终裁决入口，117 项 + 2337 原离散 tick 检查，7 个先编译、再要求精确失败的旧行为对照。
- `ShatterAvailabilityTests.py`：真实准备/落点/可用性/HUD/typed query，43 项，4 个编译负对照（实际旧落点、旧 HUD、伪造到期时间、绕过 readiness）。
- `CombatOpportunitySlotProductionTests.py`：真实动作槽绘制方法，8 项，旧 ready-dot-only 精确负对照。绘制 API 是记录器。
- 原锁敌 36 项 + 3 负对照、触控生命周期 26 项、BlockedCombatUpdate、ContractSnapshot、HeroPoseCommit、SkillReadability 和相关源码契约继续通过。

以上是托管生产方法回放；未执行 Unity/真机触控、GPU 画面或手感验收。当前环境没有 Unity 6000.6 精确引用，不把旧引用兼容性代替 Unity 6 验收。没有新增控制配置、玩法费用、伤害倍率、粒子系统或空间索引。
