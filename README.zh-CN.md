# GestureSign

GestureSign 是一款 Windows 平板手势识别软件。你可以用手指或鼠标画出手势来自动执行重复操作。

[English README](README.md)

[![Release](https://img.shields.io/github/release/CaFeZn/GestureSign.svg?style=flat-square)](https://github.com/CaFeZn/GestureSign/releases/latest)

## 功能

- 激活窗口
- 窗口控制
- 虚拟桌面切换
- 触摸键盘控制
- 键盘模拟
- Key Down/Up
- 鼠标模拟
- 发送按键序列
- 打开默认浏览器
- 屏幕亮度
- 音量调节
- 运行命令或程序
- 启动 Windows Store 应用
- 发送消息
- 切换窗口置顶

## 安装

- 安装版：`winget install --id TransposonY.GestureSign -e`
- 更新安装版：`winget upgrade --id TransposonY.GestureSign -e`
- 便携版：从本 fork 的 [releases 页面](https://github.com/CaFeZn/GestureSign/releases/latest)下载便携包，解压到目标目录后运行。
- 便携版会把配置和备份写到程序目录下的 `AppData`。安装版会把用户数据写到 `%APPDATA%\GestureSign`。

## 使用方法

当旧的外部文档站不可用时，本 README 是当前的中文入门说明。

1. 启动 `GestureSign.ControlPanel.exe`。控制面板会启动后台守护进程 `GestureSign.exe` 和托盘图标。
2. 打开 `Actions` 标签页。在 `(Global Actions)` 下添加全局动作，或者添加指定应用，让动作只在匹配窗口中运行。
3. 为动作选择或创建手势。先在 `Gestures` 标签页训练新的手势形状，再把它分配给动作。
4. 给动作添加一个或多个命令。命令会从上到下执行，所以窗口激活、延迟等前置命令应放在依赖前台窗口的命令之前。
5. 保存配置。除非运行便携版，否则 GestureSign 会把用户数据写到 `%APPDATA%\GestureSign`。

启动说明：

- `GestureSign.ControlPanel.exe` 会打开设置窗口，并在需要时启动后台守护进程。
- `GestureSign.exe` 只启动后台守护进程和托盘图标。需要开机静默运行时使用它。
- 普通开机启动选项会创建指向 `GestureSign.exe` 的快捷方式。管理员开机启动选项会为 `GestureSign.exe` 创建启动任务。

手势名称和默认值：

- 旧的外部文档站不再是基础设置的必需条件；控制面板是创建手势、分配动作和修改选项的主要入口。
- 手势名称只是可编辑标签。内置默认手势中，开头数字通常表示手指数，例如 `2Left` 是双指左滑，`3Down` 是三指下滑。
- 没有开头数字的名称也可能是多指手势。打开 `Gestures` 标签页可以查看任意手势的训练样本和手指数。
- 点击类手势用点样本表示，而不是长轨迹。`3 Finger Double Tap` 是由两个三指点击样本组成的内置示例。
- 默认浏览器手势包括 `ee` 上的 `Open Web Browser`、浏览器前进/后退，以及浏览器应用组下的标签页手势。你可以在 `Actions` 标签页中编辑或删除它们。

默认手势示例：

| 动作 | 手势 | 手指数 | 作用范围 |
| --- | --- | --- | --- |
| Open Web Browser | `ee` | 2 | Global actions |
| Show/Hide Touch Keyboard | `3 Finger Double Tap` | 3 | Global actions |
| Close Tab | `3Down` | 3 | Browsers |
| Next Tab | `3Left` | 3 | Browsers |

创建手势说明：

- 可以从 `Gestures` 标签页或动作中的手势选择器创建/编辑手势；打开手势定义窗口后，在屏幕上画出手势。
- 鼠标手势默认关闭。如需用鼠标画手势，打开 `Options`，启用 `Mouse Gesture`，并选择一个或多个绘制按钮。启用该选项时默认使用鼠标右键。
- 配置鼠标绘制按钮后，按住该按钮移动鼠标画手势，松开后完成样本或执行手势。
- 多指手势需要多个触点同时接触。三指手势不等同于把同一条线画三次。
- 点和点击手势会被记录为点样本，所以预览中可能看起来像点而不是线。
- 单指触摸屏点击可以录制为手势样本。鼠标和触摸板训练仍需要移动，以避免把普通点击误录成手势。
- 在 `Gestures` 标签页双击手势可打开编辑窗口，重新绘制或重命名手势。

常用命令示例：

- `Hot Key` 发送一个快捷键，例如 `Ctrl+Alt+T`。
- 对于 `Left Alt`、`Right Shift` 等区分左右的修饰键，请使用 `Hot Key` 的额外按键下拉框，不要直接按键盘录入修饰键。
- `Send Keystrokes` 输入一串按键或文本。
- `Key Down/Up` 可以按住或松开 `Left Shift` 等按键；需要临时选择文本或保持修饰键按下时，把一个按住动作和另一个松开动作配对使用。
- `Launch Windows Store App`、`Open Default Browser`、`Open File`、`Run Command or Program` 用于启动应用或文件。
- `Search or Open Clipboard Text` 会直接打开剪贴板里像网址的文本；其他剪贴板文本会用默认浏览器搜索。若要处理选中文本，可把 `Hot Key`（`Ctrl+C`）、短 `Delay` 和该命令串起来。
- `Mouse Actions` 可以发送点击、滚轮和其他鼠标输入。
- `Add Current Application to Ignored List` 会把当前前台应用加入忽略列表，适合某个应用的原生输入和手势冲突时使用。
- `Open GestureSign Control Panel` 可以通过手势打开设置窗口。
- `Repeat Last Command` 会重复上一次成功执行的命令。
- `Next Virtual Desktop` 和 `Previous Virtual Desktop` 用于切换 Windows 虚拟桌面。

为同一行为绑定多个手势：

- 一个动作只能分配一个手势，但可以通过复制动作，把多个手势绑定到同一组命令。
- 在 `Actions` 标签页复制现有动作，选择 `Paste To New Action`，再编辑粘贴出来的动作并选择另一个手势。
- 每个动作中的命令都会从上到下执行。如果多个手势应执行完全相同的行为，请保持复制动作的命令顺序一致。

手势和触发条件说明：

- 触摸屏、触摸板、触控笔和鼠标是不同输入来源。如果某个手势只在某类设备上生效，请检查动作的 ignored-device 设置。
- 触控笔手势需要 `Options` > `Pen Gesture`，并同时具备一个 HID 笔启动状态（笔侧键/右键按钮或翻转笔）和一个绘制模式（笔尖或悬停）。如果触控笔驱动把按钮暴露成普通鼠标右键而不是 HID 笔输入，请尝试启用 `Mouse Gesture` 并使用鼠标右键。
- 连续手势会在手指仍在移动时运行，但必须先超过 `Options` > `Continuous Gesture Distance`。如果短滑动在超过该距离前就松开，匹配的普通手势仍可能先运行。
- `Drawing Start Timeout` 只取消未及时开始绘制的手势；`Whole Gesture Timeout` 会在设定时长后取消整段捕获，并且与 drawing-start timeout 互斥；`Composite Gesture Timeout` 只控制组合手势等待下一段手势的时间。
- 精确触摸板手势依赖驱动暴露 raw HID touchpad input。如果触摸板或第三方驱动无法检测，先关闭冲突的 Windows 触摸板手势，并确认设备显示为精确触摸板，而不是只暴露为鼠标滚轮或厂商私有输入。
- `Block Touch Input` 仅在 UIAccess 构建中可用，并按匹配应用配置。GestureSign 需要足够触点来识别手势后才开始阻断，所以很早期的触摸帧仍可能传给目标应用。
- 如果启用 touch blocking 后触摸键盘或浏览器原生触摸行为异常，先降低该应用的 block threshold，或为该应用关闭 touch blocking。
- 触发条件可以使用 `finger_1_start_X`、`finger_1_start_Y`、`finger_1_end_X`、`finger_1_end_Y`、百分比变量如 `finger_1_start_X%`，以及 `finger_1_ID`。
- 可以用百分比触发条件模拟边缘手势，例如 `finger_1_start_X%<5`、`finger_1_start_X%>95`、`finger_1_start_Y%<5`、`finger_1_start_Y%>95`。
- 窗口条件可以使用 `window_is_maximized`、`window_is_minimized`、`window_is_fullscreen`。
- 修饰键条件可以使用 `key_is_shift_down`、`key_is_ctrl_down`、`key_is_alt_down`、`key_is_win_down`。
- 多个触发条件可使用 SQL 风格运算符，例如 `AND`、`OR` 和括号：`finger_1_start_X<500 AND finger_1_end_X<450`。
- 条件按 Windows 虚拟屏幕坐标计算。百分比变量相对于完整虚拟桌面区域，包括位于主屏左侧或上方的显示器；跨不同分辨率显示器复用配置时通常更可靠。
- GestureSign 不能发送独立的 `Fn` 键。大多数键盘的 `Fn` 由固件处理，不会作为普通虚拟键暴露给 Windows。请使用 `Fn` 组合实际生成的键，例如驱动暴露出的 `F1`-`F24`、音量、亮度或媒体键。

## 故障排查

- 如果没有任何手势运行，确认托盘守护进程正在运行，并从控制面板重启 GestureSign。
- 如果鼠标无法开始绘制，确认 `Options` > `Mouse Gesture` 已启用，且至少选择了一个绘制按钮。
- 如果手势只在 Task Manager、Device Manager、安装程序或其他管理员窗口中失败，请查看下方“管理员窗口”说明。
- 如果某个动作在一个应用里不运行，检查该应用是否在忽略列表中，或者该动作是否只配置给了其他应用。
- 如果 GestureSign 干扰某个应用，把 `Add Current Application to Ignored List` 绑定到一个手势，在该应用激活时运行它。
- 如果触摸板手势延迟或丢失，提高 `Options` 中的 drawing-start timeout。
- 测试大规模配置变更前，先使用 `Options` > `Backup User Data`。

## 上游 Issue 覆盖情况

本 fork 针对以下上游 `TransposonY/GestureSign` issues 做了修复、实现或文档覆盖。

已实现或已覆盖：

| Issue | 覆盖情况 |
| --- | --- |
| [#139](https://github.com/TransposonY/GestureSign/issues/139) | 文档说明了复制动作的流程，可把多个手势绑定到同一组命令。 |
| [#138](https://github.com/TransposonY/GestureSign/issues/138), [#95](https://github.com/TransposonY/GestureSign/issues/95), [#62](https://github.com/TransposonY/GestureSign/issues/62) | 支持单指触摸屏手势和单指触摸屏点击训练。 |
| [#137](https://github.com/TransposonY/GestureSign/issues/137) | 添加未识别手势提示音，并支持自定义 `.wav` 文件。 |
| [#136](https://github.com/TransposonY/GestureSign/issues/136), [#90](https://github.com/TransposonY/GestureSign/issues/90) | 手势训练帮助会说明可用输入来源，以及何时需要先启用鼠标手势。 |
| [#134](https://github.com/TransposonY/GestureSign/issues/134), [#122](https://github.com/TransposonY/GestureSign/issues/122) | 连续手势距离可配置、已文档化，并修复了刚到阈值时首个触发被丢弃的问题。 |
| [#133](https://github.com/TransposonY/GestureSign/issues/133), [#111](https://github.com/TransposonY/GestureSign/issues/111) | 构建脚本和文档覆盖 Windows 11 on Arm64 以及可选 native Arm64 输出。 |
| [#132](https://github.com/TransposonY/GestureSign/issues/132), [#125](https://github.com/TransposonY/GestureSign/issues/125) | 文档说明 `Fn` 限制，Hot Key UI 更容易选择 F1-F24、媒体键和功能键。 |
| [#117](https://github.com/TransposonY/GestureSign/issues/117) | 文档说明了当前 winget 包的安装和更新命令。 |
| [#115](https://github.com/TransposonY/GestureSign/issues/115), [#40](https://github.com/TransposonY/GestureSign/issues/40) | 虚拟桌面切换动作可用，可分配给普通手势或连续手势。 |
| [#109](https://github.com/TransposonY/GestureSign/issues/109), [#131](https://github.com/TransposonY/GestureSign/issues/131) | 对切换窗口/桌面动作的 shell/taskbar 激活和修饰键释放做了加固。 |
| [#97](https://github.com/TransposonY/GestureSign/issues/97), [#31](https://github.com/TransposonY/GestureSign/issues/31) | 控制面板启动时会容忍 Windows Application Event Log 不可用；配置写入会 flush、串行化，并在 Desktop Bridge 构建中写入 package local state。 |
| [#104](https://github.com/TransposonY/GestureSign/issues/104) | `Add Current Application to Ignored List` 可用并已文档化。 |
| [#93](https://github.com/TransposonY/GestureSign/issues/93), [#92](https://github.com/TransposonY/GestureSign/issues/92) | `Key Down/Up` 支持保持修饰键按下，Hot Key 中区分左右的修饰键已在 UI 和 README 中说明。 |
| [#84](https://github.com/TransposonY/GestureSign/issues/84), [#65](https://github.com/TransposonY/GestureSign/issues/65), [#118](https://github.com/TransposonY/GestureSign/issues/118) | 本 README 替代不可用的外部指南，覆盖设置、使用、故障排查和构建说明；控制面板帮助按钮会跳转到这里。 |
| [#76](https://github.com/TransposonY/GestureSign/issues/76) | 组合手势等待时间可配置。 |
| [#30](https://github.com/TransposonY/GestureSign/issues/30) | 添加可选的整段手势超时，可在设定时长后取消捕获，并与手势起始超时互斥。 |
| [#75](https://github.com/TransposonY/GestureSign/issues/75), [#32](https://github.com/TransposonY/GestureSign/issues/32) | 触发条件支持触摸坐标变量和多个条件表达式。 |
| [#74](https://github.com/TransposonY/GestureSign/issues/74) | 重新注入触摸 `UP` 帧时为最后一个触点保留 `INRANGE`。 |
| [#73](https://github.com/TransposonY/GestureSign/issues/73), [#56](https://github.com/TransposonY/GestureSign/issues/56) | 对 Windows 10/11 下触摸键盘命令行为和 TabTip 路径查找做了加固。 |
| [#72](https://github.com/TransposonY/GestureSign/issues/72) | 可在 `Options` 中关闭触摸屏手势。 |
| [#70](https://github.com/TransposonY/GestureSign/issues/70) | 触摸屏显示器选择不再只依赖光标位置。 |
| [#57](https://github.com/TransposonY/GestureSign/issues/57), [#9](https://github.com/TransposonY/GestureSign/issues/9) | 对 touch blocking 做了加固，并文档化 UIAccess、按应用配置、初始帧等限制。 |
| [#51](https://github.com/TransposonY/GestureSign/issues/51) | 精确触摸板手势会遵守 drawing-start timeout。 |
| [#49](https://github.com/TransposonY/GestureSign/issues/49) | 备份/设置恢复支持当前备份和旧版动作/手势导出。 |
| [#48](https://github.com/TransposonY/GestureSign/issues/48), [#27](https://github.com/TransposonY/GestureSign/issues/27) | 管理员窗口、开机启动、静默启动 daemon 和便携模式已文档化。 |
| [#44](https://github.com/TransposonY/GestureSign/issues/44), [#37](https://github.com/TransposonY/GestureSign/issues/37) | `Repeat Last Command` 和 `Open GestureSign Control Panel` 动作可用。 |
| [#43](https://github.com/TransposonY/GestureSign/issues/43) | 添加 `Search or Open Clipboard Text`，可在选中文本复制到剪贴板后执行浏览器搜索或打开 URL。 |
| [#33](https://github.com/TransposonY/GestureSign/issues/33) | 鼠标手势可配置多个绘制按钮，例如同时使用右键和中键。 |
| [#38](https://github.com/TransposonY/GestureSign/issues/38) | 控制面板触摸板滚动使用小数滚轮增量处理，不再把每个小 delta 当成完整滚轮刻度。 |
| [#19](https://github.com/TransposonY/GestureSign/issues/19), [#126](https://github.com/TransposonY/GestureSign/issues/126) | Hot Key 和内置窗口命令可覆盖常见无障碍快捷键和隐藏窗口工作流。 |

已有改进但未在无硬件验证或无更大功能设计的情况下完全关闭：

| Issue | 当前状态 |
| --- | --- |
| [#128](https://github.com/TransposonY/GestureSign/issues/128), [#120](https://github.com/TransposonY/GestureSign/issues/120) | Win11 平板/触摸屏解析对缺少标准 contact-count 或坐标范围数据的 HID 报告更宽容，会按 Win32 要求初始化 raw-device info buffer，支持无序 tip usage，并改进 raw-input 诊断；但仍需要具体设备验证。 |
| [#123](https://github.com/TransposonY/GestureSign/issues/123) | 快速点击时，如果新的活跃触点帧替换了过期触点集合，现在不会再直接丢掉该帧；但高频触摸屏场景仍需要真实硬件验证。 |
| [#127](https://github.com/TransposonY/GestureSign/issues/127), [#119](https://github.com/TransposonY/GestureSign/issues/119), [#112](https://github.com/TransposonY/GestureSign/issues/112), [#94](https://github.com/TransposonY/GestureSign/issues/94), [#55](https://github.com/TransposonY/GestureSign/issues/55) | 触控笔设置和文档更清晰，但 Wacom/无源笔支持仍取决于驱动是否暴露 HID pen/touchpad input。 |
| [#135](https://github.com/TransposonY/GestureSign/issues/135), [#116](https://github.com/TransposonY/GestureSign/issues/116), [#114](https://github.com/TransposonY/GestureSign/issues/114), [#59](https://github.com/TransposonY/GestureSign/issues/59) | 精确触摸板处理已有改进，但第三方/厂商驱动支持必须按设备验证。 |
| [#130](https://github.com/TransposonY/GestureSign/issues/130), [#87](https://github.com/TransposonY/GestureSign/issues/87), [#45](https://github.com/TransposonY/GestureSign/issues/45) | hold-to-drag 工作流需要显式 hold/release 功能，避免鼠标按钮卡住。 |
| [#66](https://github.com/TransposonY/GestureSign/issues/66), [#24](https://github.com/TransposonY/GestureSign/issues/24), [#52](https://github.com/TransposonY/GestureSign/issues/52) | 修饰键条件和 held-key 动作已有部分支持，但任意按键手势条件和键盘触发鼠标绘制尚未实现。 |
| [#121](https://github.com/TransposonY/GestureSign/issues/121) | 便携版可放在指定文件夹运行，但本仓库尚未实现安装包安装目录选择。 |

## 构建

- 使用 Visual Studio 2022 打开 `GestureSign.sln`，或运行 `.\scripts\build.ps1`。
- 解决方案现在目标为 `.NET Framework 4.8`。
- NuGet 包使用 `packages.config` 恢复，所以首次构建前需要 restore。
- Windows 11 on Arm64 使用 `Any CPU` 构建；不要添加 `ARM64` solution platform。如需 native Arm64 .NET Framework 输出，请在 Windows 11 24H2 + VS 2022 17.11 或更新版本上运行 `.\scripts\build.ps1 -Configuration Release -PreferNativeArm64`。

## GitHub Releases

- 推送 `v8.1.0` 或 `v8.1.0-beta.1` 这类 semver 风格 tag 会自动运行 release workflow。
- workflow 会构建 `Release|Any CPU`，打包 `bin\Release`，创建或更新 GitHub Release，并上传 `GestureSign-<tag>-win-anycpu.zip`。
- 也可以在 GitHub Actions 页面手动运行 `Release` workflow。填写 `tag_name`；默认会 checkout 与 `tag_name` 相同的 ref，也可以填写 `build_ref` 来构建指定 branch、commit 或 tag。如果 `tag_name` 还没有推送，请填写 `build_ref`。

## 管理员窗口

Windows 会阻止普通进程向提权应用发送输入。如果在 Task Manager、Device Manager 或其他管理员窗口激活时手势不工作，请让 GestureSign 守护进程也以管理员权限运行。

- 推荐：打开 Options，启用 `Start GestureSign on Windows Startup`，再启用 `Run GestureSign As Administrator At Startup`。
- 一次性排查：右键 `GestureSign.ControlPanel.exe`，选择 `Run as administrator`；控制面板也会以提权方式启动守护进程。
- 不要使用可执行文件 Compatibility 标签页中的 `Run this program as an administrator`。GestureSign 会对此发出警告，因为它可能破坏启动和 touch-blocking 功能。
