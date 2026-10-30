# 素材制作记录

## 1.14.16 烫金打字提示

用户确认的 `Typing-WordArt-Gilded-v2` 由本地 Blender 5.2.2 制作，Noto Serif SC 900字重，独立立体字、黑金侧面、金箔反射条带、扫光、钻石闪点及金色花饰。没有使用新的image_gen或fal图片。源.blend和288×112 PNG序列保留在私有工作区。

`tools/import_typing_prompt.py` 从已确认的95色硬透明PNG序列固定做一次最近邻适配至180×70，不裁切、不逐帧重新对齐、不重画。每帧保存32位顶向下的黑白掩码与BGRX颜色；透明颜色为零。`TypingPrompt.gz` 包含一个40字节BITMAPINFOHEADER、至4096字节的填充和41组双位图。原五帧出场和36帧循环均保留，坐标(48,440)。逐帧输入、掩码、颜色及源Blender工程哈希见 `TypingPrompt.json`。

调用示例：`python tools/import_typing_prompt.py <Typing-WordArt-Gilded-v2目录>`。普通运行和构建使用已嵌入数据，不需要Blender、Python或系统字体。

## 1.9.0 新素材制作记录

新增 `PoliceShield0.dib`、`PoliceShield1.dib` 和 `PoliceShieldIdle.dib`。使用内置 image_gen，以此前用户认可的自制警察素材作为角色参考，生成 8 个朝向、每方向 4 个行走帧和 1 个站立帧。未使用 fal，也没有另行调用收费 API。

保留长脸、机灵的成人形象、灰色高檐帽、蓝灰裤子、原有角色尺寸和脚底基准。盾牌实际握在人物手里，举盾时隐藏枪械。每帧保留向右下方的地面阴影。

生成结果先按透明行间隙分出 8 行，避免将邻行帽子误切进帧内；每帧以已接受角色的高度进行一次 nearest-neighbour 缩放，放入 92×99 单元。16 帧行走表、16 帧行走表、8 帧站立表分别打包为 276×594、8-bit DIB，透明色索引 0，每张 165008 字节。转换后重新解码并在真实游戏中核对八方向。

瞬移残影根据已接受小偷精灵的轮廓提取横线几何，在游戏自己的绘图线程短暂绘制青色残影和落点环；无需更换原来的小偷人物精灵。

原有 14 张人物精灵表与 1.8.4 逐文件哈希一致。完整新增提示词与文件哈希见 `assets/skills-generation.json`。


## 1.9.3 像素技能图标

新增 PoliceShieldIcon.png 和 ThiefBlinkIcon.png，均位于 assets/，64×64、最多64色、不透明银灰边框。通过内置 image_gen 生成，遵循用户指定的1990年代像素风；提示词为 assets/skill-icon-prompts.json。生成原稿保存在 assets/generated-icons/。未使用fal或外部API。

使用 universal-modder：um sprite pixelate source.png output.png --size 64x64 --colors 64。每个最终文件均从生成原稿直接转换一次，不反复缩放。像素图标与旧版17张人物精灵互相独立。

source/SkillIcons.cs 在启动时把两个PNG转换为顶向下的32位BI_RGB像素，每角色65个64×64帧。第0帧全亮，第N帧顶部N行改为28%亮度的灰度，第64帧全暗。原生绘图线程复用现有后台缓冲HDC，以SetDIBitsToDevice整块绘制所选帧，不新增持久GDI对象。图标坐标为800×600画面的(12,432)，文章栏不变。绘图API说明：https://learn.microsoft.com/en-us/windows/win32/api/wingdi/nf-wingdi-setdibitstodevice 。

素材内存新增区：BITMAPINFO在asset+0x300000，盾牌帧0x301000..0x405000，瞬移帧0x410000..0x514000，总分配0x520000。快照3048为本地角色，3052为暗行数。原有2256..3024的瞬移线段与3040/3044文章进度字段不重叠。真实画面按游戏原生RGB565量化核对，避免把正常颜色量化差异误判为素材错误。


## 1.9.4 图标位置

沿用 1.9.3 的两枚像素图标和全部 65 帧状态，将绘制坐标从 (12,432) 移到 (724,432)。图标右缘距 800×600 画面右边 12 像素，仍位于原版文章栏上方。素材没有重新生成。


## 1.9.5 图标缩小

保留已接受的两张 64×64 图标 PNG；启动时从原图做一次最近邻采样，生成 48×48 显示帧，不使用平滑插值。仍为 65 个计时状态，第 N 帧顶部 floor(N×48/64) 行变暗，每帧 9216 字节。绘制位置 (740,448)，右边距 12 像素、底边 496 与上一版一致，原版文章栏不变。原始素材没有重新生成或覆盖。


## 1.10.0 生命值像素 HUD

用户要求类似 MC 的红心，左上角同时显示双方血量。本版在 source/HealthHud.cs 中原创绘制9×9像素心形及警帽／蒙面头像，使用2倍整数像素；红心18×16，间距22像素。上排警察 y12，下排小偷 y38，头像 x12、三颗心 x36/58/80。满心红色、空心暗灰、受伤无敌时满心短暂闪白。不是从 Minecraft 提取的图像，也没有使用新的 AI 图片。

按角色×0至3颗心×闪亮状态生成16组透明像素游程，每组4096字节、最多200条矩形记录。银行位于已有资产内存4b0000..4c0000，与两种技能图标和人物资源不重叠。游戏原有UI线程使用借用的 DC_BRUSH + SetDCBrushColor/FillRect 绘制这些像素，透明间隙不覆盖地图，不增加GDI对象。顶端原有 TextOut 提示已移除。既有48×48技能图标保持不变。


## 1.10.1 武器 HUD 与滚轮

source/WeaponHud.cs 原创绘制24×24像素的拳头、木托弯弹匣 AK47、木质护木泵动霰弹枪及深灰金属框，按2倍整数像素输出48×48。图标位于800×600画面(12,448)，底边496，与右下角技能图标一致；三组4096字节游程位于既有资产内存4c0000..4c3000，不修改原图或人物精灵。继续借用 DC_BRUSH 在游戏原有UI线程绘制，不增加 GDI 对象。

启动器在自己的窗口注册 Raw Input 鼠标，过滤游戏前台及移动模式，仅取有符号垂直滚轮增量。每120单位换一格，精细增量累计，退出允许状态或按数字键时清除余量，停用时注销。没有修改游戏窗口过程、系统鼠标配置或添加全局钩子。与既有pose.Weapon字段共用切枪逻辑，wire3不变。API参考：[RegisterRawInputDevices](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-registerrawinputdevices)、[GetRawInputData](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getrawinputdata)、[RAWMOUSE](https://learn.microsoft.com/en-us/windows/win32/api/winuser/ns-winuser-rawmouse)。


## 1.10.2 透明斜向武器素材

用户要求从左下角方框图标改成右上角、类似 GTA 的自由斜向造型，同时贴合游戏画风。使用内置 image_gen 对旧的 AK47／霰弹枪／拳头三张图分别重绘，以已有游戏截图与已接受的人物精灵作为画风参考；第二、三次还使用新 AK 图统一风格。透明背景，暖木、蓝黑钢、自然肤色，光线来自左上，不添加底板、方框、文字或地面阴影。完整提示词见 assets/generated-weapons/PROMPTS.json，三个原始生成 PNG 原样保留于同目录。

各图先用 um sprite hard-alpha --thresh128 清理近透明边缘，再用 um sprite pixelate --size96x72 --colors96 做一次面积缩小和色板限制，保留96×72透明画布；游戏不再缩放。成品 WeaponUnarmed.png／WeaponAK47.png／WeaponShotgun.png 分别有2821／1317／1060不透明像素。C#启动时将同色横向游程纵向合并为2439／1169／940个矩形，完全跳过透明像素，复用现有原生绘图线程的 stock DC_BRUSH，不增加GDI对象或改变窗口过程。

右上角视口(692,12)..(788,84)。三状态步长从4096增至0x20000，银行4c0000..520000，仍在原有0x520000资产分配内，与人物／文字／技能／红心不重叠。原生442字节绘图代码改为17位状态偏移和6000矩形上限，27块代码和10处钩子数量不变。实机逐像素误差小于1色阶，透明区域与不绘制图标时的底图100%一致，绘制调用约52.7次／秒且GDI句柄不增长。


## 1.11.0 缩小武器、弹药与音效

保留 1.10.2 的三个原始生成 PNG 与提示词，各自从原图 hard-alpha128，再一次 um sprite pixelate --size64x48 --colors96；不从旧96×72成品二次缩放。位置(648,12)..(712,60)，相邻像素数字位于(724,17)，同色横向游程纵向合并为矩形，透明像素不绘制。武器矩形数1130/537/435。数字、R与金色换弹进度使用原创3×5像素字形和不可变矩形银行，借用原有 stock DC_BRUSH。数字62×4096位于520000..55e000，进度65×1024位于55e000..56e400，总资产分配570000；与其他资产不重叠。原生绘制块616字节，27块代码、10处钩子不变。

AK枪声取 Free Firearm Sound Library 的 AK-47/C_28P.wav 单发录音，霰弹取 Benelli Nova/O_21P.wav 单发录音；换弹为 SpringySpringo 的空气枪录音，均为CC0。用FFmpeg定位第一枪瞬态，剪掉环境前导，保留短尾、65Hz高通、调低音量与尾部淡出，转44.1kHz单声道16bit PCM。原始库留在私有work测试目录，不把194MB下载归档塞进MOD。来源页、作者、源文件哈希、精确剪辑起点/时长/增益/命令均记录在audio/weapons/。

启动器的独立waveOut工作线程混合最多16路短声音，4个20ms缓冲，队列有界；只在实际接受开火/换弹时触发。联网依照房主各角色、各武器的事件计数播放，重复快照不重复发声；失焦时清空待播并静音，恢复后不补播旧音效。没有阻塞游戏UI线程，既有MFPlay压缩音乐保持不变。


## 1.11.1 音效重混与音乐持续

角色与HUD美术逐文件保留。新的AK枪响将Michel Baradari的rifle.wav前280ms与CC0真实AK近场瞬态叠加；霰弹枪叠加其shotgun.wav前460ms、CC0真实枪响与延后180ms的泵动声。均衡/动态压缩/调音/短尾淡出后44.1kHz mono PCM，最终峰值0.577/0.699，前80ms RMS .264/.321，对比旧版提升15.0/16.1dB。派生两张枪声文件按CC BY3.0分发，原作者署名、源地址、许可、变化与哈希在audio/weapons/。独立预听通过与运行时相同的混音限幅生成；无游戏提取的CS或HotlineMiami素材。

MusicPolicy只在首次启用且装备枪时触发，跨收枪/结果/菜单/重开不清零。既有原生UI线程的静音代码依据共享SoundEngine当前BGM缓冲工作，不再限制角色/scene3；切换缓冲时恢复/释放旧引用并取得新引用，没有原BGM时也允许外部音乐继续。普通绘制回调与既有设备回调均执行静音协调。MFPlay正常56%音量，开火触发20%/110ms后180ms恢复，临时切出只暂停。新的软限幅限制多声叠音，4x10ms缓冲缩短响应。停止时恢复原BGM音量/引用，离开小游戏后不访问已释放的角色对象。


## 1.13.0 补弹图标

新增 `ResupplyIcon.png`，使用内置 image_gen，以已有 `PoliceShieldIcon.png` 作风格参考生成弹药箱、三枚铜色弹药及绿色回转箭头。保留生成原稿 `generated-resupply/original.png` 和生成说明 `PROVENANCE.json`；原有角色与武器图像逐字节保留。

生成原图仅做一次最近邻缩至 64×64 RGB；运行时按已有技能管线取样至 48×48。`SkillIconFrames.BuildResupply` 生成 0–4 单词五帧，由暗到亮，叠加原创像素 E 标记和四格进度；坐标 (684,448)，在现有 Shift 图标左侧，原版文章栏上方。资产区使用原有空隙 0xD0000..0xDB400，不占用人物精灵。储备数字银行位于 0x570000..0x5CB000，总资产分配 0x5D0000。
# 1.14.0 Blender 分层待机

沿用用户认可的原版像素分层样片，不重新生成人脸或重建几何人物。警察、小偷空手／AK47／霰弹枪共 48 个方向组合：其中 8 个空手斜向继续播放原版资源，40 个新增方向由 Blender 制作；警察盾牌另外增加 8 个方向。

每个新循环为 18 帧、15 FPS、92×99 单元，打包到 276×594 DIB。上身、头部、下身／阴影、持枪层各有独立网格和骨骼；原版斜向头部提供姿态变化，已有持枪像素提供身体和枪械。警察右下沿用已确认样片的原版 9 帧身体及两臂 IK。小偷原版头部做一次 90% 最近邻适配、去除随头部区域截入的布袋像素，并按颈部稳定位置；枪和上身随动，脚底／原投影固定。

`tools/idle/prepare.py` 从用户自己的 GameRes.dll 和现有 MOD 图集准备私有分层输入；`bake.py` 在 Blender 5.2.2 LTS 中求解网格、UV 与骨骼，再按像素中心栅格化；`pack.py` 生成 `IdleMaps.gz`、映射哈希清单及枪口地标。映射记录每个输出像素来自哪个本地原版帧或 MOD 像素，没有原版 RGB 图像。Blender 工程、提取 PNG 和彩色预览只保存在私有工作目录，不进入源码与发行包。

运行时 `IdleAnimation.Build` 读取本机资源，把映射转换成 indexed DIB。新增头部原版颜色优先无损保留，再分配／匹配武器颜色，输出黑色透明索引 0。现有全部 `.dib`、原始生成 PNG 和提示词逐字节保留。实际输出可用 `TypeGameMod.Tests.exe --export-idle <目录> <GameRes.dll 路径>` 导出检查。


# 1.14.17 警察空手八向3D待机

输入为用户提供的 cartoon+police+officer+3d+model.zip，自带Tripo模型、贴图与Mixamo骨骼。沿用用户确认的双手裤侧姿势及柔和高光；在Blender中将相机和灯光绕人物统一旋转，渲染八方向各九帧。作者工程与审阅GIF保留在私有 outputs/Police-Idle-Eight-Directions，未放入运行包。

原生帧61×85、硬透明，颜色沿用获批SE调色板，所有方向共享同一像素／阴影处理。tools/idle/import_police_idle.py 仅将整帧平移(14,5)进入92×99图格，不缩放、不裁人物／阴影。每向以18格DIB容器存储九帧两次；运行时仅播放0..8，周期0.5179694999969797秒。PoliceIdle.json记录源图与各银行哈希。以前的DIB、PNG、压缩图形及其元数据逐字节保留。

# 1.14.18 警察AK47八向3D待机

获批作者工程与GIF为私有 outputs/Police-AK47-Idle-Eight-Directions。tools/idle/import_police_ak_idle.py --source <审阅目录> --landmarks <muzzle-landmarks.json> --output <记录目录> 将每向九张92×99硬透明PNG直接编码为276×594 indexed DIB，九帧在后九格重复；不缩放、不拼接肢体、不裁切像素。颜色精确保留，原有资源文件不修改。

枪口地标由 work/police-model-idle/export_police_ak_landmarks.py 从获批Blender工程的实际枪管前端按同一相机投影导出；运行包嵌入图集和生成的二维坐标，不包含作者3D工程。PoliceAkIdle.json 记录源工程、各PNG与DIB的哈希。

# 1.14.19 警察AK47八向跑步

私有作者工程与获批GIF位于outputs/Police-AK47-Eight-Direction-Run。tools/idle/import_police_ak_run.py --source <审阅目录> --output <记录目录> 直接编码八向各24张92×99硬透明PNG；每向18+6有效格，第二表余12格为中立帧且不播放。无缩放、裁切或图块拼接。camera.json中的枪口投影同时生成PoliceAkRun.g.cs，PoliceAkRun.json记录源项目、图格和DIB哈希。

火光和淡烟使用原AkFireEffect点阵矩形及配色，警察只增加独立实例和追加绘制，未烘焙进人物图集。所有旧DIB、PNG、压缩资源及元数据逐字节保留。

# 1.14.20 警察持霰弹枪八向待机

作者工程、审阅GIF及render-manifest.json保存在私有outputs/Police-Shotgun-Idle-Eight-Directions。输入为用户确认的加长13.34%、枪管横向加粗32%／纵向28%的模型与原握姿。每向九张92×99硬透明PNG使用原警察SE调色板和固定阴影图案，东南像素与单向获批版完全一致。

`tools/idle/import_police_shotgun_idle.py --source <审阅目录> --landmarks <muzzle-landmarks.json> --output <记录目录>` 直接输出八个276×594 indexed DIB，后九格重复前九格、仅播放0..8。源图、DIB及作者工程哈希在PoliceShotgunIdle.json。西向图格平移+2、绘制补偿-2；枪口由同一相机和姿势在Blender中投影，再加绘制补偿。除目标坐标偏移外，无图像重采样或人物形变。所有旧资产数据保留。


# 1.14.21 警察霰弹枪八向跑步

获批作者工程及GIF保存在私有outputs/Police-Shotgun-Eight-Direction-Run。tools/idle/import_police_shotgun_run.py --source <审阅目录> --output <记录目录> 直接编码八方向各24张92×99硬透明PNG，每向两个18格DIB，第二表余12格填中立帧且不播放。无重采样、肢体拼接或裁切，既有模型、纹理、枪身大小和待机素材保留。

camera.json的逐姿势枪口投影减去西向3像素打包偏移，生成PoliceShotgunRun.g.cs；PoliceShotgunRun.json记录源工程及PNG／DIB哈希。火光和淡烟沿用ShotgunFireEffect矩形及配色，独立于人物图集；所有旧资产数据逐字节保留。


# 1.14.22 警察空手跑步

源为私有outputs/Police-Unarmed-Eight-Direction-Run-v2/Police-unarmed-eight-direction-run.blend及获批GIF。肩关节FK摆臂、肘部约70±2.5度，手腕按拇指朝上、手心向内对齐前臂；保持原模型、手掌、权重、材质、光照、腿部和躯干动画。

tools/idle/import_police_run.py --source <审阅目录> --output <记录目录> 将八向各24张92×99硬透明PNG直接编码为16个276×594 indexed DIB；每向18+6有效格，第二银行余12格填中立帧而不播放。原生图无重采样、裁切或肢体拼接，绘制原点不另加补偿。源项目及PNG/DIB哈希存入PoliceRun.json，全部旧资产数据保留。


# 1.14.23 警察加长枪身

源为用户确认的Police-Weapons-Length-Review：AK47总长比原警察版1.249628976、霰弹枪1.102514964。四个作者片段位于私有outputs/Police-Long-Weapons-Eight-Directions。各片段使用同一获批枪网格，只复制前段顶点变化；人物网格、骨骼与双手握姿、枪托和握把、枪宽高、材质及光照保持原样。

`tools/idle/import_police_long_weapons.py --source <四片段目录> --output <记录目录> --palette-helper <work/police-long-weapons>` 使用相同原版警察调色板和固定阴影，原生92×99图格无重采样或裁切。相机按完整片段逐方向选择整数X留边，绘制和枪口使用相反补偿；四个g.cs的DrawOffsetX和Muzzle来自同一camera.json。待机后9格重复，跑步第二银行余12格填中立帧且不播放。

旧1.14.22运行包和接入前完整源码ZIP保留。仅四组枪械DIB和元数据替换，其他177个资产数据文件逐字节保留；新36帧E／SE待机与获批预览完全一致。


# 1.14.24 警察加厚盾牌待机

源为私有outputs/Police-Shield-Thicker-Eight-Directions的已确认像素帧及Blender工程。盾牌来自用户的riot+shield+3d+model.zip，沿用已确认的警察模型和骨骼待机；盾体只向外增加厚度，后部握把和前臂握持位置保留。各方向均通过相机／灯光一同旋转生成，SE直接复用已认可帧。

`tools/idle/import_police_shield_idle.py --source <八向审阅目录> --output <记录目录>` 将72张92×99硬透明PNG直接编码为八个PoliceShieldIdle{0..7}.dib。无缩放、裁切、像素重绘或分层重建；后九格重复原九帧。PoliceShieldIdle.json记录源模型和各帧哈希。

原有231个资产文件逐字节保留。编译包经IdleAnimation.Build导出后，144个图格的可见RGB／透明度与确认图一致。


# 1.14.25 警察持盾移动

源为私有outputs/Police-Shield-Eight-Direction-Run的获批GIF、原生PNG和Blender工程。沿用已认可的加厚盾牌和警察模型，持盾手臂固定在胸部握持框架，腿部和身体起伏使用已确认的空手跑步步法；实际鞋底接地／腾空高度、膝盖朝向、左手握点和循环接缝已检查。

`tools/idle/import_police_shield_run.py --source <八向跑步目录> --output <记录目录>` 将192张92×99硬透明PNG直接编码为16个PoliceShieldRun{heading}_{bank}.dib，每向18+6有效格，第二银行余12格填第0帧且不播放。无缩放、裁切或肢体分层重绘，源工程和帧哈希写入PoliceShieldRun.json。运行时资源导出后，288个图格的可见RGB和透明度逐像素核对。
