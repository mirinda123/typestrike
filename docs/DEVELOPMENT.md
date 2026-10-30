# 开发指南

先读 [架构说明](ARCHITECTURE.md)，再按功能进入src目录。当前使用.NET Framework 4.0目标、x86、WinForms，保留旧式C#编译器兼容；不用将项目迁移到新框架来改玩法。

## 构建

```powershell
.\build.ps1 -Test
```

产物位于 `artifacts/bin/Release/`，测试位于 `artifacts/tests/Release/`。测试引用生产程序集的内部类，不在生产程序中添加测试分支。普通回归不需要原版游戏或音乐，不启动网络。

没有4.0开发参考程序集时，可在本地目录准备微软的参考包（不安装到系统）：

```powershell
nuget install Microsoft.NETFramework.ReferenceAssemblies.net40 -Version 1.0.3 -OutputDirectory artifacts/build-deps -NonInteractive
.\build.ps1 -Test -ReferenceAssembliesPath artifacts/build-deps/Microsoft.NETFramework.ReferenceAssemblies.net40.1.0.3/build
```

来源：[微软参考程序集包](https://www.nuget.org/packages/Microsoft.NETFramework.ReferenceAssemblies.net40/1.0.3)。GitHub CI使用同一参数，原生生成和美术生成均不是常规构建前置步骤。

## 修改入口

| 内容 | 文件 |
| --- | --- |
| 弹速、弹丸、射程 | Gameplay/GameRules.cs、Ballistics.cs |
| 弹药、射速、供弹及换弹 | Gameplay/WeaponAmmo.cs |
| 生命值、受击间隔 | Gameplay/DuelHealth.cs |
| 能量、盾牌及瞬移 | Gameplay/RoleSkill.cs、Runtime/ModSession.Skills.cs、Interop/NativeSkillBridge.cs |
| 打字与键盘队列 | Runtime/TypingController.cs、Interop/NativeTypingBridge.cs |
| 枪口与射击起点 | Presentation/WeaponMuzzle.cs、Runtime/ModSession.Animation.cs、Gameplay/ShotOrigin.cs |
| 会话故障与线程关闭 | Runtime/SessionWorker.cs、Launcher/LauncherForm.cs |
| 地图边界 | assets/MapCover.json、Gameplay/MovementMap.cs |
| 动画与特效 | Runtime/ModSession.Animation.cs、Presentation/ |
| 联网权威及回合 | Networking/DuelSession.*.cs |
| 局域网网卡与房间搜索 | Networking/LanDiscovery.cs、Interop/LanInterfaces.cs、Runtime/ModSession.Discovery.cs、native/asm/lan_address.asm |
| 便携目录、启动模式 | Installation/GameBundle.cs、GameInstallation.cs、Launcher/LauncherOptions.cs |
| 发布文件选择 | build/game-files.json、tools/package.ps1 |

## 原生与地图生成

只有更改汇编／共享布局时才安装 `tools/requirements-native.txt`，运行 `python tools/build_native.py --write`，随后 `--check` 及完整回归。不要手改MemoryLayout.g.cs或生成二进制。

修改地图轮廓后，安装Pillow并运行 `python tools/build_map_cover.py`。挡弹图与人物图分离；人物图额外包含水面和细长花坛，并按脚底范围扩张。原版地图仅用于本地预览，不进入源码。

格式为四空格、Allman花括号、110列、LF。可按requirements-format.txt安装clang-format后运行tools/format.ps1。保留进程／COM／文件／网络边界检查，玩法中不重复兜底。

## 启动与验证

源码构建默认打开设置；完整包发现Classic目录后直接启动。`--launcher`只打开设置，`--game`显式选择原版路径，`--attach PID --instance lab`连接已有进程。显式参数覆盖随包发现，损坏的完整包不会静默转用本机另一份安装。

实机项目见 [手动清单](../tests/manual-checklist.md)。只操作自己创建并确认路径的进程，保留用户实例。联网前检查目标程序、当前网络配置文件及已有防火墙规则；没有规则时先做离线验证，不自动提权、安装系统功能或更改安全设置。消息模拟不能代替实际双机联测。

可用 `python tools/verify_refactor.py --baseline 旧版TypeGameWASD.exe` 做重构行为对照。工具保存两份执行记录及parity.json，包含弹药、技能、单人战斗、联网包、动画、技能HUD、枪口、资源与原生重定位；只有使用兼容规则及资源的基线时才应逐字节相等。测试进程只分配自己的内存，不连接游戏或网络。

发布步骤见 [RELEASING.md](RELEASING.md)。本机历史实验、日志和验证记录保留在开发工作区，不进入干净源码导出。
