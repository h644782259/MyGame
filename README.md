# 星烬纪元 / Emberfall — Android 开发分支

Unity 3D 即时战斗 RPG 单机原型，使用 **Unity 6000.6.3f1 / Built-in Render Pipeline**。本分支准备 Android ARM64 development APK，与 [Windows](https://github.com/h644782259/emberfall_win) 和 [iOS](https://github.com/h644782259/emberfall_ios) 共享玩法源码。

**当前只交付源码和构建入口，没有 APK、Android 安装、真机画面或性能验收。** 本次云环境缺少 Unity 和 Android SDK/NDK，实际构建预检返回 BLOCKED。没有启动 Gradle、生成签名材料或发布到应用商店。

## 构建入口

完整步骤、官方工具版本和限制见 [Android development APK 说明](Docs/Android-Development.md)，当前机器证据见 [工具链预检报告](Docs/Android-Toolchain-Report.json)。

1. 在有权限的机器通过 Unity Hub 安装准确版本及 Android Build Support、SDK/NDK/OpenJDK，并完成用户自己的许可配置。
2. 导入项目并选择 Android 目标。开发包名为 `com.h644782259.emberfall.android.dev`，API 26–36，ARM64/IL2CPP，支持两个横屏方向。
3. 先执行预检，再构建开发 APK：

```bash
python3 Tools/Android/preflight.py --unity /path/to/Editor/Unity
python3 Tools/Android/build_debug.py --unity /path/to/Editor/Unity
```

工具不安装依赖、不接受许可、不收集发布 keystore 或上传密钥。使用标准 Unity debug 签名流程，发布签名不属于本轮。构建成功后仍需实际 APK 权限/ABI/签名审计及设备验证；预检通过本身不等于构建成功。

## 本轮内容

- 四职业、十格移动技能、装备与独立存档；三营地、三试炼、五房远征及阶段首领。
- 地形/目标与敌人组合、房间祝福和路线取舍；种子规则及旧副本回归保留。
- 完整打包中文字体、后台/旋转输入清理、Back 同帧归属与退出确认。见 [移动生命周期说明](Docs/Android-Lifecycle.md)。
- 收藏预览按变化重绘，飘字按字体版本缓存测量。见 [性能改动及测量边界](Docs/UI-Performance-Iteration.md)。托管计数不代表 Unity 帧率或真机体验。

## 可运行的源码检查

```bash
bash Tests/Run-CloudValidation.sh --dotnet /path/to/dotnet --compile --compile-android
python3 Tests/AndroidPlatformTests.py
python3 Tests/FontCoverageAudit.py --font Assets/Resources/Fonts/NotoSansSC-Regular.otf --strict
python3 Tools/validate-world-labels.py
```

引用程序集编译不会启动 Unity，也不会编译 Shader、运行 IL2CPP 或生成 APK。当前仍需补充准确 Unity 6 引用验证、真实构建和 API 26/API 36 ARM64 设备上的触控、中文、旋转/安全区、后台恢复、存档升级及长战斗测试。

中文字体许可证随资产保留在 [字体目录](Assets/Resources/Fonts/LICENSE.txt)。Windows 历史安装器和桌面开发步骤请查看 Windows 仓库，不适用于本 Android 分支。
