# B01/B02 与守门追击预算

本批保留伤害事件、技能冷却、蓄力提交和普攻恢复参数。渲染姿态不决定这些战斗时钟。

- Player 在技能/普攻前提交实际步行位移；CombatModel.PlayAction / ReleaseCharge 随即零时间步求最终姿态。发力臂和武器直接进入该攻击阶段，攻击衔接只允许头部/披风收势；取消仍保留有界的全身恢复。每帧惯性仅推进一次，同帧重新求姿态不会推进动作年龄。蓄力在原 LateUpdate 提交后也立即更新武器，不等下一次 Player.Update。
- 普通 CombatFx.Slash 只产生装饰新月。玩家真实持剑挥击显式调用 WeaponSlash，并以模型动作身份领取一次端点剑带。换动作、取消或异常端点跳跃停止采样，既有几何在原 0.22 秒寿命内淡出；死亡/换场旧 epoch 直接清理。区域每跳、伙伴与召唤冲击不再领取玩家剑带。
- 守门者的 3.5 秒预算由实际 WalkForAnimation 位移累计，碰墙不算追击时间，站桩预警不消耗。越界/超预算的返岗请求等待已开始的预警/冲锋释放完毕；下一次 AI 选择先返岗，不能借机再开招。硬控、死亡与既有有效性处理保持原优先级。

验证入口（参数为 dotnet 可执行路径）：

```
python3 Tests/HeroPoseCommitTests.py <dotnet>
python3 Tests/WeaponSwingIdentityTests.py <dotnet>
python3 Tests/GuardReturnTests.py <dotnet>
```

这些脚本执行实际生产姿态/恢复方法、实际 Slash 分派与 WeaponSlashRibbon 组件、实际 ReturnToEscapePost / WalkForAnimation 方法及 EscapePostPolicy；Unity 的对象、变换和最终场景边界由受控替身提供。每条负对照必须先成功编译，再在指定行为断言失败：旧姿态未提交、衔接错误套取消恢复、重复剑带、旧动作继续采样、区域装饰附带剑带、预警前强制返岗。

PlayerController 同批作为单一文件 owner 接入其他负责人的只读可碎冰查询、实际治疗证据和本局攻击倍率接口；各项业务测试由对应负责人维护。晶核双目标/剩余计数由结算与标记负责人交付。

本批没有 Unity 6 原生运行、截图、录像、设备帧时或真机操作证据；托管几何/方法检查不等同于画面质感验收。
