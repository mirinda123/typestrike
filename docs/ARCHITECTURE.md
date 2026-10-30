# 当前架构 · 1.19.2

这是一个 x86 WinForms 启动器。C# 负责输入、规则、联网状态和画面数据，原生 x86 桥接负责必须在原游戏 UI 线程完成的绘制、移动及场景回调。游戏原文件不直接修改；玩法类可以脱离游戏运行测试。

## 从哪里开始读

| 目录 | 负责什么 |
| --- | --- |
| `Launcher/`、`Installation/` | 选择安装位置、准备独立副本、启动／连接游戏 |
| `Runtime/` | 连接原游戏内存与下列模块，拥有运行时状态和资源生命周期 |
| `Gameplay/` | 弹药、弹道、血量、能量、文章进度与地图碰撞；不访问进程内存 |
| `Networking/` | 原版 DirectPlay 上的消息格式、会话、可靠动作及房主权威状态 |
| `Presentation/` | 像素 HUD、角色动画时钟与开火／打字效果 |
| `Audio/` | 短音效混音、压缩音乐播放和音量策略 |
| `Interop/`、`native/` | Windows 边界、挂钩、共享内存布局与游戏线程桥接 |

## 每帧执行顺序

`Runtime/ModSession.Loop.cs` 的 `Poll` 约每 8 毫秒运行一次，模拟步长上限 50 毫秒。它只编排步骤：

1. `ReadGameFrame` 读取场景和角色；`KeyboardInput` 生成一次输入，区分持续按住与按下边沿。
2. `PublishMovement` 发布移动意图；单人由 `TickOffline`、联网由 `TickNetwork` 处理。
3. `UpdateAnimations` 读取双方视觉状态，推进动画时钟并合成特效；`UpdateMusic` 更新播放策略。
4. `Publish` 通过序号保护发布一份完整快照，再写入武器外观和心跳。
5. 原游戏线程读取稳定快照，绘制精灵／HUD，执行移动、瞬移及胜负场景回调。

`ModSession.Frames.cs` 集中读帧和发布。`ModSession.Animation.cs` 集中动画调度，`CharacterVisualState` 表达一个角色本帧的武器、移动、举盾和实际射击计数。警察与小偷使用各自的时钟。每帧由FireEffectBuffer建立空的特效列表，两种角色只追加自己的火光与烟雾，最后统一发布数量。角色之间没有清空／追加的先后依赖。

## 玩法与碰撞

`OfflineMatch` 负责单人投射物、命中和胜负；`DuelSession` 在房主运行相同规则。`GameRules` 集中武器编号、弹丸数、速度、散布、射程和世界尺寸；`GameScene` 标注原游戏的场景值。

`Ballistics.FirstImpact` 比较人物相对运动的扫掠接触和静态掩体接触，距离相同时由掩体挡住；只移除当前弹丸。联网把本步命中按接触先后及射手角色排序，再应用 `DuelHealth` 的三颗血和半秒无敌。保留单人与联网原有的 double／float 精度。

`TypingController`拥有打字模式、文章进度、键盘释放门控及反馈；ControlsAllowed只查询状态，键盘物理状态每帧采样一次。`NativeTypingBridge`读取文章和有序键盘队列，`NativeSkillBridge`拥有瞬移邮箱确认及盾牌资源指针。`SkillHud`接收一份SkillView编码画面，完全不访问进程内存。

`WeaponMuzzle`只根据角色、武器、方向、是否移动和当前动画帧查找枪口偏移。运行层把它转为世界坐标ShotOrigin；联网会话通过构造参数接收定位函数，不再保存动画帧或读取美术数据。`Ballistics`负责弹道与接触计算。

`RoleSkill` 管理最多六点能量：一个完整正确单词加一点，瞬移扣一点，按住盾牌每秒扣一点，E补弹扣三点。原版文章的接受光标由 `ArticleProgress` 计数；满格仍能打字。`WeaponAmmo` 独立管理两种枪的弹匣、备弹、换弹截止时间及射击间隔。

`assets/MapCover.json` 是地图轮廓的源数据。`build_map_cover.py` 分别生成挡弹图和人物图；人物图额外加入湖水、喷泉池、景观水池及中央细长灌木花坛，再按脚底半宽10、半高5像素扩张。建筑、车辆、树干及树冠同时挡人挡弹；水面和低矮花坛只挡人。`MovementMap` 的分步移动支持贴墙滑动，瞬移在障碍前停止。原生 `MapMove` 执行同一规则，锚点为人物帧中的(40,85)。

## 联网

仍使用游戏自带 DirectPlay 的消息7，不增加监听程序或端口。当前线协议是 v15；1.18.4 拒绝 v14 及更早的地形边界，联网双方需要相同规则。

`LanInterfaces`只读枚举网络接口与默认接口；`LanDiscovery`选择已连接的实际以太网／Wi-Fi IPv4 子网并计算广播地址。连接时`PrepareLanDiscovery`写入一份不可变BSTR；`native/asm/lan_address.asm`只替换原版`CreateInternetAddress`的空地址参数。原生COM调用顺序、房间枚举／建房／加入和战斗消息保持原样。新挂钩与其他挂钩一并验证原字节、安装和卸载，停用后恢复原版空地址。没有可用接口时保留原参数，不影响单人。程序不调用系统路由写入API。

| 文件 | 负责什么 |
| --- | --- |
| `DuelProtocol.cs`、`DuelPacket.cs` | 完整解析并验证外部数据；不执行业务动作 |
| `DuelSession.cs`、`.Rounds.cs` | 握手、连接状态、单步调度、回合及重新开始 |
| `DuelSession.Skills.cs` | 按序应用单词、举盾／收盾、瞬移和补给动作 |
| `DuelSession.Combat.cs` | 本地动作请求、房主发射、弹道与伤害 |
| `DuelSession.Receive.cs` | 校验身份与序号，再分流为房主处理动作／客户端应用快照 |
| `DuelSession.Serialize.cs` | 按固定顺序写出协议 |
| `ModSession.Lan.cs` | 将当前输入与对局连接起来，处理开始、退出和断线 |
| `ModSession.NetworkTransport.cs` | 原生收发邮箱、姿势发布及场景请求 |
| `ModSession.NetworkView.cs` | 从对局状态写出 HUD、弹道及音频事件 |

房主决定弹药、技能消耗、伤害、胜负及速度；动作编号和确认防止重传重复消费。客户端可以预测打字及瞬移，E供弹等待房主确认；未确认补给会限制后续预测。举盾／收盾与单词共用可靠队列，保证松盾后打字的顺序。

## 资源、音频与生命周期

`ModSession.Sprites.cs` 在连接时上传资源；十组八向双银行共用一个上传函数。既有指针表、上传顺序、画面尺寸和资源地址保持不变。当前角色的 Blender 像素渲染已经保存为只读资源，常规构建不需要运行 Blender。旧版分层待机仍保留在资源重建流程中，不能把旧方案的帧数当成当前所有角色的帧数。具体时序见各动画类与测试。

`CombatAudio` 在独立 waveOut 线程混合短 PCM 音效，`WeaponMusic` 用 MFPlay 播放压缩 AAC。`MusicPolicy` 决定对局播放及菜单／结算／失焦暂停，`MusicEnvelope` 和 `CombatLimiter` 负责压低音乐与混音限幅。

`SessionWorker`拥有主循环线程并保留故障。启动器分别检查游戏进程和工作线程故障；循环异常会停用会话、恢复挂钩并显示原因。卸载必须等工作线程真正结束，随后才释放音频或关闭进程句柄。

连接前检查游戏哈希、图形就绪状态与挂钩原字节。卸载先停工作线程，再由游戏线程释放 MOD 自己的表面／引用，恢复挂钩和资源。桥接分配保留到进程退出，避免原生调用仍持有返回地址。

进程、文件、COM、资源格式和网络解析处保留边界验证；玩法内部复用已经验证的状态。没有新增通用框架、静默兜底或吞异常后继续运行的路径。

本机实验档案不进入干净公开仓库，当前构建所需的代码与资源均在工程目录。开发入口见 [DEVELOPMENT.md](DEVELOPMENT.md)。


## 便携发行边界

GameIdentity拥有文件哈希校验；安装模块只接收明确传入的实例名，不读取窗体静态状态。GameBundle定义随包Classic目录并验证游戏身份；LauncherOptions决定自动启动、设置、显式游戏路径和连接已有进程。GameInstallation在便携模式直接准备随包副本，在外部安装模式继续准备独立运行副本。

tools/package.ps1按build/game-files.json组织完整包；tools/export-source.ps1按维护清单导出干净仓库，二者共用Release.Common.ps1的版本、校验和压缩函数。历史日志与分析数据不混入玩家包。发布流程不改变玩法；1.19.1沿用v15，与1.18.4兼容。
