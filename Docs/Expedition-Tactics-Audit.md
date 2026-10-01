# 远征交付补充复核

## 聚合计数对账

基线为 Windows `e0f0846c1f410a157082c483ec9d3da65c5ce5a8`、iOS `d12ceac6b2f2a0102a5de6a596e761903e28d77f`。父任务提供的上一轮 56 项与基线 runner 的注册表一致；旧环境的原始 56 项报告不在当前工作区，以下依据基线 runner 和本轮报告逐项对账。

| 类别 | 旧版 | 本轮 | 说明 |
| --- | ---: | ---: | --- |
| 生产逻辑/几何独立套件 | 51 | 52 | 全部保留，新增 `tactical-room-geometry` |
| Unity 2021.3 参考 API 编译 | 1 | 1 | `runtime-compile`，两端分别执行 |
| 精确 Unity 6 Windows runtime 编译 | 1 | 0 | `exact-unity-runtime-compile` 未执行 |
| 精确 Unity 6 iOS runtime 编译 | 1 | 0 | `exact-unity-ios-runtime-compile` 未执行 |
| 精确 Unity 6 Editor 编译 | 1 | 0 | `exact-unity-editor-compile` 未执行 |
| 精确 Unity 6 验证 Player 编译 | 1 | 0 | `exact-unity-visual-validation-compile` 未执行 |
| 合计 | 56 | 53 | 56 − 4 + 1 = 53 |
| 单列 Python 源码契约 | 22 | 23 | 原 22 组保留，新增 `RoomTacticsSourceTests.py` |

四项精确编译仍保留在 runner 中，仅在提供 `--unity-editor` 时执行。本环境没有编辑器及配套 Unity 6 DLL，因此没有传该参数，也没有把这些未执行项算成通过。不能把 53 项通过理解成通过了上一轮全部编译配置。

Git 文件清单确认没有删除 Tests 文件。原 `room-chain` 套件适配了新的目标开门条件，继续覆盖身份、登记、重复/过期回调、门锁、失败和结算；两处 Editor 夹具增加净化前置，但没有在当前环境运行。

## 当前机器的 Unity 状态

当前环境中 `/workspace/shared/emberfall-tools` 不存在；PATH、`/usr/local`、`/opt`、`/home/agent`、`/workspace` 未发现 Unity 可执行文件。找到的 UnityEngine DLL 仅来自仓库的 `Tools/ReferenceAssemblies/UnityEngine` 编译参考包。没有尝试启动编辑器、激活许可或绕过沙箱。

因此本轮的原因是**当前机器未安装 Unity，也没有运行尝试**，不是本轮复现了许可错误或 AF_UNIX/NETLINK/IPC 错误。`Tools/cloud-validation-environment.md` 是先前环境记录，其安装路径和失败原因不能当作本机证据。

本轮使用 `/workspace/scratch/dotnet/dotnet`，SDK 8.0.408；独立测试可正常运行。

## 复核矩阵

| 边界 | 已核实的规则/接线 | 验证限制 |
| --- | --- | --- |
| 净化/突围暂停 | `InputBlocked` 覆盖菜单、UI、后台、死亡和终态；`Advance` 拒绝 inactive/非法时间，争夺或离开保留部分进度 | 状态测试和源码检查；未在设备切后台 |
| 护援暂停/死亡 | 护援是即时距离+视线倍率，无独立计时；暂停停止 VFX tick；供能者死亡或远征终态立即返回倍率 1；既有终态伤害门禁继续生效 | 源码检查；未执行带 Unity 对象的伤害/VFX 场景 |
| 死亡后抢门 | `Fail` 关闭已开门，禁用推进和结算；角色死亡入口有重复调用保护 | 新增“门开后死亡”状态断言；未执行死亡动画/触控 |
| 重复进门 | `Next` 替换房间后立即锁门，旧计划不能登记击杀；新房入口距离北门足够远 | 状态/接线测试；未执行真实多触点输入 |
| 撤离/晚到回调 | 先保存，再禁用/销毁旧敌人并清空回执；没有为剩余敌人调用击杀。Dispose 重复执行后仍拒绝推进、击杀和奖励 | 状态、持久化/拾取套件与源码检查；Editor 场景夹具未运行 |
| 奖励幂等 | 最终成功才结算；唯一 modeReceipt 持久化后才 ClaimReward。写入失败保留旧存档，可重试；重载后同收据不重复发奖 | `ModeRewardTests`、独立存档/回执套件通过；JsonUtility 用测试替身 |
| 地图与近战 | 六布局、两印/北门/支线、三种半径和所有 97 档刷怪角度；所有玩家共用 .45m 半径可步行到敌人，.7m 近战伙伴可找到 1.4m 内攻击站位 | 执行生产 WorldTraversal，Unity 数学用 managed shim；不是 AI/四职业实际试玩 |

没有地形目标要求闪避、跳跃、远程攻击或特定职业技能。守卫护援只是减伤，不是无敌；供能者不保护自己。上述测试覆盖当前生成器所有六地图 × 三房 × 97 档刷怪角度的几何输入（194 个种子重复验证两轮角度），因此不是仅抽取几个好种子。但真实追逐、避让拥堵、控制和设备操作仍须引擎验收。

## 发现并修复的真实缺陷

首次进入远征原本继承普通副本 `(0,0,-9)` 出生点，而新刷怪净空与后续房间入口均以 `(0,0,-12)` 为基准。这会使首次入场的实际敌人距离小于设计的 5.5m。

只修改远征首次入场为 `(0,0,-12)`；普通副本仍为 `-9`，营地仍为 `-10`。新增源码契约检查首次入场的分流表达式，另外补充重复进门、开门后死亡、重复放弃、全部玩家/近战伙伴接近路线断言。没有添加新的玩法系统或改变奖励数值。

## 可复现命令与报告

分别在 `/workspace/emberfall_win`、`/workspace/emberfall_ios` 根目录执行：

```bash
bash Tests/Run-CloudValidation.sh --dotnet /workspace/scratch/dotnet/dotnet --compile
for test_file in Tests/*SourceTests.py; do python "$test_file" || exit 1; done
git diff --check
```

首次参考包下载使用 `--download-references`（该参数隐含 `--compile`），runner 检查固定 SHA-512。最终重跑使用上面的 `--compile`，复用相同参考包。未执行 `--unity-editor`。

最终报告保存在各仓库忽略目录 `Tests/TestResults/Cloud-Latest/report.json`，包含每项名称、结果、日志、源码 SHA-256 和 `sourceChangedDuringRun`。提交不改变源码内容；交付时核对全部记录的源码哈希与提交工作树一致。

首次交付的两个 53 项报告也保留在忽略目录：

- Windows：`Tests/TestResults/Tactics-a28dedd30dec23145a3f922ba8d5cac74f13be02-report.json`
- iOS：`Tests/TestResults/Tactics-eaab91e1c5c2396a4cabf4c98aabe26032873c05-report.json`

这些旧报告对应入口修复前提交，不应作为修复后版本的验收证据。最终提交 SHA 与新报告时间在 PR 和交付消息中给出。


## 后续独立审查修复与计数变化

移动 HUD 原本只显示房间数与“探索中”，桌面目标说明没有到达移动分支。现为最小 188×76 逻辑单位的目标卡，固定显示目标/房间、3秒或4秒实际进度及封印数、争夺/暂停状态、护援短提示。进度条读取真实目标进度，不再用房间编号代替；568×320 下顶部 y=8、底部 y=84，避开十技能键、交互、药剂、Boss条和玩家状态。覆盖手机/iPad的矩形检查不等于渲染验收。

供能者 Notify 会被后续入场通知覆盖，故机制说明移入常驻卡片：`护援：敌群减伤；引开或击杀`，死亡后变为`护援已断`。敌人特性也显示6米、30%和视线条件；不再依赖瞬时通知。

入口由 `TacticalRoomGeometry.Entrance` 单独定义，首次 teleport、换房 teleport、可达性和刷怪过滤、几何测试全部引用它。seed63/layout21/第六敌人的旧2.7566m复现属于原 `-9` 出生点；新玩家使用共享 `-12` 入口，所有97档刷怪角度的净空检查都基于真实入口定义。

新增 `mobile-room-objective` 套件后，当前聚合数量由初交付53增加到**54**：原51套件 + 地图套件1 + 移动目标呈现套件1 + 参考API编译1。原四项Unity6精确编译在当前环境仍未执行；若另一个worker补齐，完整矩阵应是58项，而且必须注明所验证的提交。Python源码契约仍为23组，仅扩展既有RoomTactics接线断言。

## 官方安装尝试的补充结果（2026-10-01 22:57 UTC）

在获得已有安装授权证据后，独立环境任务检查了Unity官方6000.6.3f1发布页，并请求官方Linux安装包。代理拒绝CONNECT，返回HTTP403（curl退出56）；因此没有下载、安装或启动编辑器。AF_UNIX与AF_NETLINK基础创建测试成功，不存在可直接沿用的旧EPERM结论。没有DISPLAY、Xvfb或GPU设备；即使获准下载安装包，图形运行、Debian兼容性和许可证激活也仍须验证。

原始证据位于 `/workspace/shared/unity-environment/status.json`、`download-head.log`、`local-probe.json`。没有绕过代理、修改安全限制、索取凭据或代用户确认许可条款。该尝试不产生Unity运行、截图或录像通过结论。
