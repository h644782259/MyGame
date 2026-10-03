# G03 — 冻结、霜痕、易伤持续标识

独立于包2的后续提交。`EnemyStatusVisual`逐敌绑定真实`EnemyStatusEffects`，不持有第二套计时器，不改变任何控制/伤害数值。

- `IsFrozen`控制脚踝局部晶体；`HasFrostMark`单独控制侧身小菱片。Boss即使有霜痕也不显示冻结晶体，不暗示不可移动。
- `IsMarked`显示琥珀双斜纹；不使用锁定准星、地面危险圈或全身遮挡特效。
- 目标/精英/普通的晶体数为3/2/1，低档为1；菱片强调为1.35/1.15/1倍。按目标变化只调本敌强调，不转移状态。
- 真实施加、消费、过期通过`VisualStateChanged`同步通知；消费立即隐藏、同帧重施复用对象，不排队Destroy。死亡/停用隐藏，销毁退订与释放材质。
- 材质使用现有Standard不透明深度测试，禁投影。每敌最多6个复用立方体（72三角面）、2个按需创建自有材质、0纹理；普通/低档最多4体。没有粒子、发光背景或跨敌共享可变状态。

## 原始检查

`python3 Tests/EnemyStatusVisualProductionTests.py /workspace/shared/emberfall-tools/dotnet/dotnet`

10项完整生产视觉组件生命周期检查、6项真实EnemyStatusEffects通知检查通过。移除Boss保护、移除同步订阅、移除消费通知三个负对照均编译成功并按预期失败。`production.log`保留完整输出，异常栈为已标记负对照的预期结果。

`api-compile.log`记录Win/iOS/Android宏下所有运行源对固定Unity2021 API编译成功。初次API缺方法错误原样留档，已改使用项目既有Standard材质构造方式；没有未处理编译错误。

尚未运行Unity原生渲染、实际遮挡摄像机、不同敌人形体上的屏幕可读性或Windows/iOS设备验收。托管测试使用Unity对象/渲染替身，不能作为画面验收。源状态持续时间、Boss控制规则不变。
