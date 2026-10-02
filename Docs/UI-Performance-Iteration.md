# 有界 UI 重复工作优化（PR6 之后）

本轮只处理收藏预览和战斗飘字。没有 Unity Profiler、GPU 时间、帧率、Android/iOS 真机或画面验收结论。

## 生产依据与范围

- `CollectionModelPreview.Render` 原先在每次 IMGUI 调用拼接三个装备和两件时装的身份字符串。预览模型仅创建时调用 `Animate(0,0,false)`，且关闭所有行为脚本；即使外观和角度不变，仍按 30 Hz 时间门槛请求相机渲染。
- 现在使用单个值快照，覆盖角色、装备 ID/等级/稀有度/强化/机制/变体、时装 ID/部位/稀有度；包括同一物品对象的就地变化。静止预览不定时重画，只在改变外观、角度、纹理丢失或应用恢复后重画。模型创建和相机渲染都发生于 Repaint，每帧至多一次；暂停/关闭仍释放模型与 RT。RT 固定为 384×480。
- `FloatingNumber.LateUpdate` 原先对每个存活文本、每次集体重排重新请求字体字符并逐字读取度量。现在每个文本持有一个字形纵横比和字体版本值，缩放/DPI 仍逐帧换算；碰撞重排、真实 `localBounds` 修正和可见数量上限保持原有规则。无全局字符串缓存。
- 字体由 `GameFont` 唯一持有。飘字仅在有存活文本时订阅一份 `Font.textureRebuilt`，最后一个文本禁用/销毁时取消订阅。相关字体图集重建和字体替换失效缓存；更换字体同时更新文字及描边绑定，不销毁解析器持有的字体。

## 可复现的托管指标

基准源码为 Windows `c022a46`。旧身份表达式直接从该生产文件提取，并使用真实 `GameTypes` 在 .NET 8 Release 热运行：20,000 次调用分配 24,641,504 字节（约 1,232 字节/次）。新 `CollectionPreviewAppearance` 的三件装备/两件时装快照及比较同样热运行 20,000 次，测得 0 托管字节。该数字不包含 IMGUI 的其他分配，也不是 Unity GC Profiler 数据。

按原时间条件模拟十秒静止 Repaint 流，30/60/120 Hz 分别准入 159/213/248 次渲染。新状态机首次 Repaint 后，600 次静止帧不再请求渲染；这验证的是调度，不是 GPU 耗时。

旧字形度量方法的计数桩中，24 个四字文本、48 次重排请求字符 1,152 次、查询字形 4,608 次。新缓存测试中相同稳定样本仅需要 24 次度量；直接运行生产 `FloatingNumber` 的托管生命周期桩另证实：一个文本在 admission 度量一次后，48 次 reflow 不增加字形查询。字体重建/替换会重新测量，实际游戏中次数取决于真实字体事件。

## 验证与集成

- `UiRenderCacheTests.Run()`：692 项状态、失效、缩放和分配检查；依赖 `GameTypes` 及其常规 progression 测试支架、`CollectionPreviewState`、`CombatTextMetrics`、`CombatTextLayout`。
- `UiRenderCacheLifecycleTests.Run()`：21 项生产飘字调用/生命周期检查；**单独**编译 `FloatingNumber`、`CombatTextMetrics`、`CombatTextLayout` 和该文件，使用其中的计数/几何桩，不与其他 Unity 桩或 Unity DLL 混用。
- 43 组已有源码契约通过；旧“定时 30 Hz”契约更新为脏状态/纹理恢复契约。字形 admission 的无堆对象检查仅放行新的值类型 `CombatTextMetrics`，其他准入约束保留。
- 全部运行时源码在 `UNITY_ANDROID` 与 `UNITY_STANDALONE_WIN` 下参考 API 编译各零警告、零错误。字体事件、RT 丢失和应用切前后台仍需 Unity 及真机验收。
