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

Anchored spell meshes certify the complete visibility hull from the true impact origin to each triangle against the same registered circles, rounded boxes, arena and .04 LOS clearance. Unsafe faces are subdivided within bounded work; the certificate does not impose the face width as a disk around the impact origin. Horizontal animation still contracts on the original rays. This preserves valid wall-adjacent hits without moving their center.

The real GroundPoint regression casts from (-3,0,0) toward (3,0,0) against a .2 by 6 box and retains the returned x=-.14025879. Six impact families across this input and seven near-wall clearances retain 144 positive-area anchored pieces; required ground-contact pieces also retain positive horizontal area. The coverage suite ran 19,763,189 real LOS checks over 106,304 faces and 1,489,560 animated vertices. Three compiled controls reject the old origin-disk certificate, sparse face test and anisotropic animation.

The older combat-readability suite now also compiles actual CombatSight and WorldTraversal, resolves the same GroundPoint, and checks all six impact families. It first requires a positive-area primary, landing and contact mesh, then checks their vertices against real LOS. The all-empty old implementation fails EMPTY_REQUIRED_SHAPE before any vertex loop. Its former 700-to-67 assertion drop was vacuous coverage, not equivalent validation; the revised suite executes 6,751 checks and five compiled controls. Changing vertex counts are not used as a substitute for these explicit requirements.

The work ceilings remain 4,096 certificates/output faces per mesh and depth 6, with fair source-face budgets. Three pieces together have a 12,288 certificate/face ceiling; each certificate examines the registered obstacles, and source-vertex BoundaryPoint work is additional. For the same finite-circle matrix, creation changed from 12,047 sampled segments / 65,031 solid probes / 1,674 faces to 1,847 / 7,447 / 549. Near-wall scenarios used at most 514 certificates and 1,524 obstacle-fan checks. These are separate work counts, not a device performance benchmark; Unity/mobile rendering and frame times remain unverified.

Five pre-existing fixtures were aligned with actual production dependencies and the explicitly changed blessing values without dropping assertions. Original aggregate failures are retained separately; final passing counts are reported only after the new full run completes.
