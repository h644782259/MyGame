# 收束毒矢 B — 独立工作包

基线 Windows origin/main `5d85e47b9fab2489d5b06963a0b896ec19112740`。只改共享源码；合并与双平台同步由集成负责人完成。

A 为未解锁/旧档默认：保留原扇形、三阶群爆、毒爆 80% 加成和传播。B 经既有4碎片解锁、营地免费互斥切换：技能0在既有TryConsume能量/CD成功后走单独CastConcentratedVenom并return，不进入(arrows-1)扇形循环。一次Friendly调用，非穿透、无爆炸、无volley，直伤2.4/3.6/4.8A；碰撞半径0.14米不随范围放大，速度沿原20×range。

捕获施法时AimTarget；0.18秒内、18度锥、70度/秒有限修正，此后直飞，1.15秒寿命，不改目标、不保证送达。B在当前运动段按真实敌人碰撞圆EntryFraction选首个拦截；墙体仍用原地形截断，阻挡道具优先比较。普通三层毒爆为原完整非暴击预算，与直接伤害暴击分开；ConsumePoison原cast gate保证同cast不能重兑。B不传播；实际消费仅记录“收束毒爆”，A记录“毒层引爆”。

HasMechanicVariant扩入VenomSpread，复用现有存档规范化、解锁事务、重铸、升华和预设；无新存档schema。装备比较与工坊文案显示两种取舍。

## 可复现检查

使用.NET路径 `/workspace/shared/emberfall-tools/dotnet/dotnet`；三个新runner均接受该路径为第一个参数：
- `Tests/ConcentratedVenomProductionTests.py`：16项实际contact+15项真实持久化事务。列表顺序旧行为、旧目录各编译通过并按预期断言失败。
- `Tests/ConcentratedVenomLaunchTests.py`：18项实际Cast helper/Friendly/Make：各rank一发、无穿透/群爆、准确预算、窄半径、锁定身份/castId；穿透负例被拒。
- `Tests/ConcentratedVenomPoisonTests.py`：8项实际Ranger反应分支及完整EnemyStatusEffects：126完整毒爆、重复/补毒同cast不二次消费、暴击只影响直伤、A旧传播、epoch隔离；旧spread负例被拒。
- `Tests/BlockedCombatUpdateProductionTests.py`：完整生产Update含B暂停、过期、移动脱靶、换代、重试和真实WorldTraversal墙体高速截断；原两项暂停负例仍失败。独立运行需 `DOTNET_CLI_HOME` 指向可写临时目录。
- 原艺术factory857项及contact事件11项回归通过。原Update严格SHA在移除仅B独占三段后仍等于历史值（非B行为未改）。
- `api-compile.log`：Win/iOS/Android三个宏的全部runtime源对固定Unity2021 API编译通过。脚本与原始日志在本目录。

日志中的Unhandled exception是标注负对照的预期失败；initial-*保留初次fixture缺参数、嵌套脚本分割错误及CLI默认目录不可写记录，修复后以其余日志为准。

## 尚未验收

以上为托管生产源码检查、引擎API编译，非Unity运行、非真实JsonUtility、非设备构建。尚需Unity实录检查箭体/反馈、原生场景墙体和运动目标手感，以及Windows/iOS设备验收。没有改数值资产/伤害接收规则，未声称Android已构建或推送。
