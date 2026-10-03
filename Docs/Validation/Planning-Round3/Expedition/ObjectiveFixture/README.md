# 冻结 v2 两项房间 UI fixture 兼容修复

基线 `b853f84`，未改 runtime 或中央注册。原失败日志从只读 validation-v2/Tests/TestResults/Cloud-Latest 原样保留。

- mobile-room-objective：旧测试在第二房完成后直接调用 Next，忽略 false；新规则要求明确选择第三房。下一轮仍在第二房，对已死亡敌人再次 Defeat，在旧第59行失败。现在通过真实 OpenBranchChoice / SelectBranch 选择，并断言每次 Next 成功；两条分支各执行完整房间显示、首领与护卫、最终胜利断言。
- room-blessing-preview：旧第38行同样忽略两次 Next 的返回值；由于第二房未选路，旧第39行并未到休憩房。现在两条分支各覆盖原全部种子集合，明确选路并验证第三房身份和进入休憩房。原所有预览、只读重复查询、首领护卫和终局检查保持。

专项构建使用 Tools/cloud-validation.py 的 write_project 与注册表中原生产源码集合，dotnet=/workspace/shared/emberfall-tools/dotnet/dotnet，临时项目与 CLI home，离线 NuGet config。

结果：mobile-room-objective-fixed.log 171 断言；room-blessing-preview-fixed.log 27,504 断言。两份日志亦分别执行 `git show b853f84:Tests/<原fixture>.cs` 与完全相同现行生产源码，精确重现冻结同一异常，证明是旧 fixture 未履行新分支契约，未放宽 runtime 推进条件。

仍可用已登记的 `Tests/Run-CloudValidation.sh --dotnet /workspace/shared/emberfall-tools/dotnet/dotnet` 重跑。这里是文本/状态/布局的 managed 检查，不是 Unity 渲染或实机验收。未修改根工作树和冻结目录。
