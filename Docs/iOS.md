# 在 Mac 上安装到自己的 iPhone

这是 Unity 游戏源码的 iPhone 构建流程。独立仓库为 https://github.com/h644782259/emberfall_ios ，推荐在 Mac 克隆后导出 Xcode 工程，再由 Xcode 直接安装到自己的设备。Windows 发行包不能转换成 iPhone 安装包。

当前 Windows 开发环境安装了 Unity 6000.6.3f1，但只有 Windows/WebGL 模块，没有 iOS Build Support；因此此环境没有生成 Xcode 工程、签名 IPA，也没有完成 iPhone 真机测试。提交构建脚本不代表已经完成这些步骤。

## 1. 准备 Mac 和 iPhone

- 在 Mac 安装支持所连 iPhone iOS 版本的完整 Xcode，并启动一次完成其要求的首次配置和 iOS 平台支持下载；仅 Command Line Tools 不足以安装到 iPhone。
- 用 Unity Hub 安装 `ProjectSettings/ProjectVersion.txt` 指定的 Unity **6000.6.3f1**，勾选该编辑器的 **iOS Build Support**。脚本不会替你安装或更换模块。
- 用 USB 连接 iPhone，解锁设备并按提示信任这台 Mac。较新 iOS 若提示需要开发者模式，在 iPhone 的「设置 → 隐私与安全性 → 开发者模式」中按设备提示启用。
- 在 Xcode → Settings → Accounts 添加自己的 Apple Account。安装到自己的设备可以使用免费的 **Personal Team**，不要求先加入付费 Apple Developer Program，也不需要 TestFlight。

Personal Team 的开发签名有 Apple 的期限及设备/应用数量限制，通常需要每隔数天重新用 Xcode 部署；Xcode 显示的当前规则为准。此流程适合自己的 iPhone 调试，不是公开分发安装包。

## 2. 克隆和导入 Unity

```bash
git clone https://github.com/h644782259/emberfall_ios.git
cd emberfall_ios
```

Unity Hub → Add project from disk → 选择包含 `Assets`、`Packages`、`ProjectSettings` 的仓库根目录。等待导入和编译结束。场景由 `Assets/Scenes/Main.unity` 启动，运行时生成模型和世界，无需另找美术包。

图形配置为 Built-in/Metal，iPhone ARM64、IL2CPP，横屏双方向，最低目标 iOS 15.0。开发和首轮真机测试需关注发热、帧率、刘海/灵动岛安全区、手指遮挡与触屏施法；这些设置不保证所有设备已经达到目标帧率。

## 3. 导出 Xcode 源工程

可在 Unity 使用 **Emberfall → 导出 iPhone Xcode 工程 Export iOS**。输出目录会自动包含时间戳。也可先关闭此项目的 Unity 编辑器，在 Mac Terminal 运行：

```bash
# 只检查工具和模块，不启动 Unity 或构建
bash Tools/Export-iOS.sh --check-only

# 导出；将 Bundle Identifier 改成属于自己且唯一的反向域名
bash Tools/Export-iOS.sh --bundle-id com.yourname.emberfall
```

若编辑器不在默认安装位置：

```bash
bash Tools/Export-iOS.sh \
  --unity "/Applications/Unity/Hub/Editor/6000.6.3f1/Unity.app/Contents/MacOS/Unity" \
  --bundle-id com.yourname.emberfall \
  --output Builds/iOS/MyFirstExport
```

`--team-id ABCDE12345` 是可选项；不知道 Team ID 就省略，稍后在 Xcode 选择 Personal Team。不要把 Apple 账号、证书、私钥、描述文件或密码提交到仓库。脚本不读取 Apple 凭据，不会自动签名、安装、发布或上传。

输出必须位于 `Builds/iOS` 下的一个空子目录，已有工程不会被覆盖。成功后包含 `Unity-iPhone.xcodeproj`、Unity/IL2CPP 源代码和游戏数据；日志位于 `Logs/ios-export-*.log`。复制到另一台 Mac 时带上**整个输出目录**。

Windows 可执行 `pwsh -File Tools/Export-iOS.ps1 -CheckOnly` 检查模块。缺少 iOS 模块时脚本明确报错并停止，不创建伪造工程；这台开发机的当前状态正是如此。

## 4. Xcode 选择签名和设备

1. 如果导出目录存在 `Unity-iPhone.xcworkspace`，打开它；否则打开 `Unity-iPhone.xcodeproj`。
2. 选择蓝色工程 → **TARGETS → Unity-iPhone → Signing & Capabilities**，勾选 **Automatically manage signing**。
3. 在 **Team** 选择自己的 **Personal Team** 或已有开发团队。确认 **Bundle Identifier** 与导出时填写的一致且唯一；若提示标识被占用，换一个唯一标识。保留 UnityFramework 的 Unity 生成配置，不要给它另加无关能力。
4. 顶部 Scheme 选择 **Unity-iPhone**，运行目标选择已连接的真实 iPhone，而不是 Simulator 或 Any iOS Device。首次配对或准备调试支持时等待 Xcode 完成。
5. 点击 **Run ▶**（Cmd+R）。Xcode 构建、开发签名并安装到所选设备。若 iPhone 要求信任开发者或开启开发者模式，按其设置提示完成后再运行。

若失败，先读 Xcode 的具体签名/设备错误。常见原因是尚未选择 Team、Bundle ID 已被占用、手机没有信任电脑、Xcode 不支持手机的 iOS 版本或开发签名过期。免费 Team 不需要通过 Archive/TestFlight 流程才能在自己的设备运行。

## 5. 真机验收与存档

- 检查横屏切换、文字、安全区、双指移动与攻击、技能选点/取消、蓄力、背包/技能树、第四职业召唤、副本掉落和返回营地。
- 检查锁屏、切到后台、来电/音频中断后，游戏暂停与声音恢复是否正常；保存、退出、重启后继续同一角色。
- 连续战斗与多召唤物场景检查性能和温度，按 `Tests/PLAYTEST.md` 验证核心玩法。桌面通过不能替代 iPhone 真机测试。
- iPhone 存档在应用自己的沙盒中，Windows 的 `%USERPROFILE%` 路径不适用。目前没有云存档或专门的 iOS 文件导入界面。删除 App 或更换 Bundle Identifier 可能导致无法读取原沙盒进度；换号/删除前先从 Xcode 的设备容器工具备份。

构建日志、`Builds/iOS`、Xcode 中间输出和个人签名材料应保持为本地产物，不提交 Git。升级源码后导出新的空目录，再在 Xcode 为新工程选择同一 Bundle Identifier 和 Team，可以避免覆盖旧的手动配置文件。
