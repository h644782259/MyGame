# 职业背影与场景小样

本补丁针对 ART D06 / D08 与游侠预览搭箭周期。只改变外观构建与独立预览；不改变伤害、攻击提交时机、装备属性、存档格式或地图通行占位。

- 剑卫保留短而宽的厚披风；元素师使用长的分离后摆与高领；游侠披布偏向左侧，为右侧箭筒留空间；唤灵师两片仪式布向两侧展开，露出中间及枝状头饰。
- 法师/唤灵师的披布中心确实不连接三角面。每件披布固定 112 个双面顶点，普通轮廓 168 三角面、分离轮廓 144 三角面。四职业调整翅膀根部，羽片/晶翼按职业采用不同展开和后掠角；机械星环保持原轨道形状，只移动根部。
- Hero 初始化先构建衣服，再在 EnhanceHero 中设置 heroClass，因此 Cape 必须接收 BuildHero 的显式职业参数；不能读取尚未设置的字段。实际网格对照发现了这个问题，完整 Hero 工厂测试现在覆盖它。
- 树木使用 5 个错层叶簇，每个 14 顶点 / 24 三角面，10 个可见分枝与一根主干。三种确定性轮廓由种子模 3 选择，无新增随机源。主干原有半径 .22 的导航圆与 22 单位范围条件保持原样。叶簇归 WorldResources 管理并参与摄像机遮挡。
- 矿镇使用双坡屋面、檐口、屋脊和烟囱帽模块。房屋原有通行宽度与 BuildingOcclusionGroup 保留；新增部件标记摄像机遮挡，不创建额外碰撞体。
- 游侠普通攻击预览从搭箭、拉弦开始，在 .32 释放，.83 重新搭箭；真实普通攻击仍从提交接触帧播放，游戏内攻击逻辑未改。

## 验证与重现

`Tests/AuthoredSceneryProductionTests.py DOTNET` 执行实际树/屋顶构建，检查网格拓扑、种子变化、摄像机标记与导航保持；`SCENERY_EXPORT=/absolute/path/scenery.json` 可导出构建网格用于渲染。基线代码固定于 03422ab，位于 Tools/ArtSourceEvidence，只作对照，不参与运行时编译。

`Tests/PreviewClothProductionTests.py`、`CollectionPoseIsolationProductionTests.py`、`EquipmentCompositionProductionTests.py`、`ClassTierStructureProductionTests.py` 覆盖真实几何/动作/装备路径与编译后负对照。测试使用托管 Unity 替身，不能证明 Unity 引擎渲染或真机性能。

```
python Tools/ArtSourceEvidence/export_silhouettes.py REPOSITORY OUTPUT DOTNET
blender -b --threads 1 --python Tools/ArtSourceEvidence/render_silhouettes.py -- OUTPUT/geometry.json OUTPUT/renders
python Tools/ArtSourceEvidence/contact_sheet.py OUTPUT/renders "SOURCE GEOMETRY / not Unity"
```

比较 T1/T4、无翼/大型晶翼使用同一机位、构图、灰材质与光照。导出执行真实 Hero、装备、时装及网格配方；世界变换由托管 TRS 替身重建（欧拉角组合并非 Unity 引擎实现），Blender 原生 CPU 渲染。因此这些图是源码几何审阅证据，不是 Unity 截图。动态穿插、角色移动时的布摆、所有稀有度翅膀、摄像机遮挡过渡及移动设备性能仍需 Unity 6 / 真机验收。

视觉审阅结果：无翼四职业背影可区分；大型翼会遮盖上背部分服饰，披布尾部、职业头饰和武器仍露出。游侠箭筒在无翼图中清晰，带翼时部分遮挡，未宣称所有动画中的完全无穿插。先修正了职业初始化参数，之后扩大统一取景范围以纳入全部翼尖。

场景源码导出实测：旧树 672 三角面/4 渲染器；新树每株 1176 三角面/16 渲染器（5 个叶簇共 120 三角面），三种变体预算相同。旧屋顶模块 600 三角面/2 渲染器；新模块 2100 三角面/7 渲染器。使用现有缓存基础网格与共享材质；没有声称新增细节免费或已经通过移动端性能验收。原生灰模对照可见枝间空隙、三种冠幅方向和双坡/檐口/烟囱帽。
