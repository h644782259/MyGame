# 低帧率准入公平性修复

基线 `608e65914d3fece96fe8fe886022e53887a758a3`。不改变试点（仅困难赤岩第一房）、人数/数值、两项威胁上限或固定种子0.35–0.6秒调度间隔。

旧实现每0.2秒删除未刷新请求。在0.25/0.5秒固定帧中，仍持续请求的敌人也会每帧重新排队，固定 Update 顺序使先执行者反复抢先。

改为每次活动房间 Update 的 simulationStep：保留连续请求者的队列身份，只清除漏掉完整请求轮次的成员。远离/视线失效仍由实际 Enemy.Update 主动 Withdraw；死亡/禁用仍由 CancelAttack/OnDisable 退出。没有删除失活清理，也没有无限保留失效请求。

同时修复不同脚本 Update 顺序下的间隔时钟：生产 Request 显式读取当前帧 `Time.time`（缩放时间），而非可能尚未运行的 GameSession.Update 累加钟。混合200/500ms尖峰时仍不允许短于0.35秒的起手。配置的随机等待为0.35–0.6秒；极低帧率下实际发起只能落在可用帧上，上界会因帧量化变长，不伪称能在500ms帧上精确执行所有子帧时点。暂停时间仍冻结。

`ThreatAdmissionFairnessProductionTests.py` 提取实际 Enemy.Update、BeginAttack、PrepareAttack、FinishAttack、CancelAttack、OnDisable，编译实际策略。50/200/250/500ms固定帧、混合尖峰、host先/后两序均检查固定幽灵先更新顺序下四成员服务；另执行真实Update出范围退出、死亡/禁用退出、漏轮清理。物理/渲染是替身，射出的弹体在此公平性测试中立即结束；在途真实生命周期仍由原 ThreatAdmissionLifecycleTests 覆盖。

原始 `production.log` 含通过结果和两个成功编译后的失效对照：原608e659策略（仅加入忽略时间参数的API转发以接新调用，原Request/Advance函数体未改）饥饿断言失败；去掉当前帧时间参数的变体混合帧间隔失败。`policy.log` / `lifecycle.log` 为原策略/在途回归通过记录。

`fixture-first.log` 为初次补充fixture缺方法的编译失败；`fixture-short-sample.log` 为测试采样时间不足导致门槛失败；`mixed-frame-clock-failure.log` 为修复过程中实际识别出的间隔时钟问题。保留这些历史，不把它们计作通过证据。

未执行Unity引擎或设备；没有修改根集成工作树或冻结验证副本。根任务需注册新增Python测试并完成联合验证。
