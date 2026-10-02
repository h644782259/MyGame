# 战斗可读性与真实行动条件

本批组合有直接接线依赖的B01–B04、祝福B、策划C/D/E，以及晶核标记需要的世界文字生命周期。宝箱B07已单独PR，不包含重铸经济、伙伴成长、后续首领演出或燃烧终击试验。

- 出手先提交最终姿态再采样武器锚点；取消恢复与攻击衔接分开。恢复时长和伤害时序不改。
- 真实剑挥击使用动作身份，装饰斩光不生成玩家手持轨迹；换动作、取消、闪避、传送断采样，旧残影正常淡出。
- 无毒箭雨使用独立箭形；万箭使用共享有界生命周期。全局装饰池按主形/真实落点/终段优先回收旧装饰，所有高优先占满时仍守硬上限。危险边界不参与装饰竞争。
- 主命中特效不寻找远处空地，保持真实落点并裁剪几何。敌圆预警采用与判伤同源的当前攻击者地面路径判定，安全缺口不画成完整危险圈；实际判伤始终实时。
- 碎冰状态与可施行动分开：技能学习、CD、能量、蓄力、落点、范围、LOS和命中足迹来自生产解析路径。当前可用不保证延迟落地时条件仍成立。
- 守卫追击只累计真实移动时间；预算耗尽不取消已承诺预警，攻击结束先返岗。晶核双敌有对象绑定剩余2/1标记，源晶体销毁不影响标记。
- 房间失败保存死亡/超时/主动放弃/生成或路径异常原因，死亡保留最后来源，建议读取实际伤害、治疗与目标进度。原房间无截止计时，本批不新增时限。
- 祝福试验为战意10%、鹰眼+20个百分点、锋芒215%暴击倍率（基础165%不变）；伙伴只继承一次战意，暴击卡仅玩家直接伤害。固定假设收益是规则预算，不是实战DPS。

新增默认生产回放与旧行为负对照覆盖姿势顺序、剑轨身份、守岗预警、实际碎冰HUD、失败证据、祝福组合/伙伴路径、箭雨批次/优先池、墙与河桥预警轮廓、对象文字生命周期。最终聚合结果在PR冻结记录中报告。托管引擎替身、文字bounds和数值规则不能替代Unity6000.6、GPU/材质、真机触控、中文实渲、画面节奏或帧时验收；本环境无Editor，不报告这些通过。


## Independent review corrections

Successful world transitions reconcile the complete session pause state, including chapter return; save failure does not release the terminal gate. The default runner includes the real UI/session return chain and two compiled old-behavior negative controls.

Elemental main shapes acquire their priority lease before optional particles. Aura shapes and particles have independent sibling ownership, so evicting a decorative particle cannot destroy the sustained shape. The default priority regression runs actual Area/OnEnemy/component lifecycles at full mobile capacity (111 assertions plus two compiled negative controls).

Anchored spell meshes certify the entire horizontal triangle footprint conservatively, subdivide only within bounded depth/work, and omit uncertified pieces. Horizontal animation contracts radially on the original visibility rays. Actual WorldTraversal/CombatSight coverage tests sample triangle interiors and animation vertices independently, retain unobstructed shapes and nonempty narrow-corridor shapes, and compile two exact old-behavior controls. The managed matrix checked 27,608,071 LOS assertions and 240 initially visible Lightning cases at age 0.2.

Clipping has a per-mesh ceiling of 4,096 certification queries, 4,096 output faces, and depth 6; budgets are shared fairly among source faces. Three main pieces together are bounded by 12,288 certification queries/faces, with source-vertex BoundaryPoint LOS work additional. In the managed creation matrix the largest actual Impact used 12,047 WorldTraversal segment calls, 65,031 solid probes and 1,674 output faces. These are creation-work counts, not device frame times; mobile rendering/performance remains unverified.

Five pre-existing fixtures were aligned with actual production dependencies and the explicitly changed blessing values without dropping assertions. Original aggregate failures are retained separately; final passing counts are reported only after the new full run completes.
