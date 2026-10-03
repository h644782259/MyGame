# 困难赤岩单房协调试点

基线 Windows main `5d85e47b9fab2489d5b06963a0b896ec19112740`。

唯一作用域：ChapterNode.Redrock、ChapterDifficulty.Hard、RoomIndex=0，第一间猎杀断供房。整房一个区域，既有六名敌人、生命、伤害、弹速、射程、重击半径、技能数值不变。既有 roster 0/1 幽灵封路双弹与 2/3 守卫重击接入；哥布林、史莱姆、首领、其他房间和难度不接入。

`ThreatAdmissionPolicy` 配置：最大两项进行中攻击；固定 ChapterSeed 的 LCG 产生 0.35–0.6 秒相邻准入间隔。FIFO 等待队列，持续有资格者在稳定 Unity 更新顺序下也轮换；超过 0.2 秒未请求者离队，恢复资格后排尾。移动、回岗、避让继续运行；距离/视线失效退出等待，已开始预警不受排队影响。真打断/死亡/禁用释放尚未发射的资格，普通受击不释放。重击结算释放；双弹共享一个独立资格，来源死亡不清除在途危险，最后弹体真实 OnDisable/OnDestroy 才释放。不再猜测固定寿命；不会改弹道或伤害。暂停撤销等待并冻结时钟，保留预警，恢复后重新排队；新房、重试、离开清空旧实例。

复现：`DOTNET=/path/to/dotnet python3 Tests/ThreatAdmissionTests.py`。295 个生产策略断言涵盖限定范围、容量、种子、间隔、同帧拒绝、轮换、公平、取消、投射物保留、暂停和清理；另有生产接线源码契约（未把 GameSession/完整 Update/真实 Unity 生命周期作为托管验收）。`final-policy.log` 为最终原始结果；`lifecycle.log` 执行真实 PrepareAttack/CancelAttack/FinishAttack/弹体 OnDisable 方法，11断言通过；`mutation-no-admission.log` 去掉生产起手准入后实测失败。早期 `tests.log`/`tests-v2.log` 是修订前历史，不作最终证据；`baseline-negative.log` 在原基线重放接线契约，因缺失准入失败，未伪造旧数值性能结论。`environment-first-attempt.log` 保留首次 SDK 默认只读 HOME 失败，后续用临时 DOTNET_CLI_HOME 解决；没有改权限。

尚未验收：Unity 真实 Update 顺序、物理、同机位画面、敌人移动/岗位手感及 Win/iOS/Android 设备。托管策略检查不代表引擎或设备测试。根任务负责最终联合 API 编译和平台同步。
