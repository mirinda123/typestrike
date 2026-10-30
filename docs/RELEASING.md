# 发布与GitHub仓库

## 仓库边界

维护工程保存在src、tests、native、assets、audio/weapons、compat、build、tools、docs和.github。Classic原版游戏、Paris文件、日志、IDA数据库、构建目录和本机验证档案不提交Git；完整玩家包在打包阶段从本地输入生成。

```powershell
.\tools\export-source.ps1
```

输出的 `TypeGameMod-版本-src/` 是可独立构建的干净目录，不继承旧工作区的Git历史。审阅后可在这个目录初始化Git并添加自己的GitHub远程：

```powershell
git init -b main
git add .
git commit -m "Initial project import"
```

脚本不会替用户创建仓库、push或上传Release，也不会重写原工作区历史。

## 完整玩家包

```powershell
.\tools\package.ps1 -GameDirectory 'C:\Games\TypeEasyGame\Classic' -SoundtrackPath '.\audio\MOON-Paris.m4a'
```

打包先构建并测试，再检查游戏EXE与资源哈希，按build/game-files.json复制必要运行文件。游戏设置重置为空，保留原版许可和文章，不复制卸载器、更新器、服务程序、IDA数据库及运行日志。DirectDraw组件与免注册清单一起放入Classic。Paris仅在显式提供SoundtrackPath时加入。

```text
TypeGameWASD-版本/
  TypeGameWASD.exe       玩家双击的入口
  Settings.cmd          需要时打开设置
  Classic/              原版游戏、文章、资源及本地组件
  audio/                枪声与可选Paris
  compat/               兼容组件、清单和许可
  README.md             玩家说明
  LICENSE.txt           原创MOD代码许可
  manifest.json         相对路径、长度、SHA-256
```

完整包不再依赖choice1131、本机AppData安装位置或PowerShell试玩脚本。玩家只需完整解压并运行EXE。程序按清单加载本地DirectX与CRT，不注册DLL，不自动改Windows功能或防火墙。

完整ZIP名为 `TypeGameWASD-版本.zip`，仅MOD包为 `TypeGameWASD-版本-mod.zip`，源码包为 `TypeGameMod-版本-src.zip`。ZIP内直接放上述目录的内容，不再嵌套同名顶层目录，避免资源管理器默认解压时重复加长路径。发布检查同时验证默认解压位置的文件路径小于260个字符；不要再给压缩包添加冗长的目录前缀。

省略GameDirectory可生成仅MOD包；它需要玩家自己的原版安装。CI自动构建的是这种包，不会把游戏或个人音乐放进公共构建。CI采用[官方checkout](https://github.com/actions/checkout)和[artifact上传](https://github.com/actions/upload-artifact)，只保留只读仓库权限。

## 上传前检查

1. 核对版本、CHANGELOG及协议兼容说明；运行完整回归。
2. 运行tools/check-release.ps1，检查manifest和目录；ZIP旁的sha256文件用于下载校验。
3. 在另一目录完整解压，检查入口和组件；对未验证的Windows或双机环境不要写已通过。
4. 原创代码、原版游戏、Tripo／AI素材、DDrawCompat、枪声和Paris的许可分别保留。完整包包含第三方内容并不意味着它们获得MIT许可；公开分发应按对应授权范围进行。
5. 在自己的GitHub Release页面手动上传完整ZIP及sha256文件。GitHub自动生成的Source code ZIP是源码，不是完整玩家包。

本工程保留Windows原游戏的运行依赖；它不是对原游戏引擎的独立重写。免注册加载依据见[微软说明](https://learn.microsoft.com/en-us/dotnet/framework/interop/registration-free-com-interop)。
