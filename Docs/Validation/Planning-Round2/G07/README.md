# G07 现有场景构图与职业识别

基线 `dc0861d92879072b6dae912dd8250478d402f530`（美术内容等于本轮起始 main）。只复用已存在模块和材质，资源 GUID、网格文件、纹理和来源没有更换。

- 出口：门中心留白，周边晶体8→4、浮点11→4，保留玉色交互提示与原灯光；六个平面引导块加宽，以原朝向指向门。
- 商人：帽檐加宽；货架3→2、货品12→6，大件陈列取代密集小件。
- 铁匠：上身、肩袖、围裙和锤头形成更宽轮廓；管理者台面加宽、保留星盘。
- 三名 NPC 初始化立即建立工作姿态，暂停首次入场也能看到各自工具姿态。
- 四设施使用原底座的不同长宽比例；目标晶冠从0.6米抬到0.85米、尺寸0.35→0.45。捕获环半径、判定、NPC中心/交互、注册碰撞导航不变。

`factory-before.json` / `factory-after.json` 是7个完整受影响工厂输出：柱、门、采石城、观星城、四设施、三NPC、目标。实际执行原生产创建方法和现有 `.bytes` 解码、材质与遮挡分组；Unity API/导航存储/文字标签是测试边界，标签渲染未计入。不是仅统计新增零件。`factory-comparison.json` 校验全导航登记和遮挡成员/组逐项不变。

| 工厂 | GameObject 前→后 | Renderer 前→后 | 独立材质 前→后 |
|---|---:|---:|---:|
| 门 |49→27|38→21|3→3|
| 采石城 |267→238|241→217|23→23|
| 观星城 |258→229|230→206|24→24|
| 三NPC |66→59|56→49|8→8|

其余对象数不增。JSON 分开记录独立源三角面与实例网格三角面；LineRenderer 的引擎生成几何不在网格三角面中。Renderer 数不是 draw call，未测批次、GPU、FrameDebugger或移动端性能。

验证：`SceneryPresentationProductionTests.py` 276断言及18个编译负对照、2个行为保持对照；`FixedSceneryEnabledIntegrationTests.py` 76778断言、116次完整六组工厂运行、27资源缺失/损坏及4个负对照；原始日志保存同目录。首次 `scenery-production.log` 是更新后的新尺寸遇到旧断言失败，已修正规格，未掩盖历史失败。生产脚本 Unity2021 API 编译通过，不代表 Unity 执行。

重现库存：设置 `DOTNET`，用 `G07_INVENTORY=/absolute/output.json python3 Tests/G07FactoryInventoryTests.py`。通过 `EMBERFALL_TEST_SOURCE_ROOT=/baseline/worktree` 读取同一测试驱动下的旧生产源码/资源。再运行 `python3 Tests/G07CompareFactoryInventory.py before.json after.json`。

尚未验收：缩小视角识别、同机位截图/实录、NPC工具遮挡、真实字体标签、Unity光照与动态淡出视觉、设备帧率。没有以离线图或托管工厂快照替代 Unity 实录。
