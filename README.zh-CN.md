# GestureSign

GestureSign 是一款 Windows 平板手势识别软件。你可以用手指或鼠标画出手势来自动执行重复操作。

[English README](README.md)

[![Release](https://img.shields.io/github/release/CaFeZn/GestureSign.svg?style=flat-square)](https://github.com/CaFeZn/GestureSign/releases)

## 项目状态

本仓库是 [TransposonY/GestureSign](https://github.com/TransposonY/GestureSign) 的社区 fork，包含选择性的维护修复、文档更新以及构建/发布自动化；它不是上游维护者的官方延续，也不承诺完整路线图。

当前代码库仍是以 `.NET Framework 4.8` 为目标的 Windows 桌面应用。本文档中的构建路径使用 Visual Studio 2022，Windows 11 说明描述已知行为和限制；本 fork 尚未迁移到 .NET 6/.NET 8。

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

- 上游安装包：`winget install --id TransposonY.GestureSign -e`
- 更新上游安装包：`winget upgrade --id TransposonY.GestureSign -e`
- 本 fork 安装版：从 [releases 页面](https://github.com/CaFeZn/GestureSign/releases) 下载 `GestureSign-<tag>-setup-win-anycpu.exe` 并运行。安装向导会显示目标文件夹页面；静默安装可传入 `/DIR="D:\Tools\GestureSign"`。
- 本 fork 便携版：下载 `GestureSign-<tag>-portable-win-anycpu.zip`，解压到目标目录后运行 `GestureSign.ControlPanel.exe`。
- 最新手动测试构建：打开 releases 页面里的滚动 `continuous` 预发布版，常规安装下载 `GestureSign-continuous-setup-win-anycpu.exe`，免安装测试下载 `GestureSign-continuous-portable-win-anycpu.zip`。这里的 `continuous` 只是“持续滚动更新的测试构建”这个发布渠道名称，和下文动作设置里的 `Continuous Gesture` 不是一回事。
- 如果本 fork 暂无已发布的 release asset，请使用上面的上游安装包，或用 `.\scripts\build.ps1 -Configuration Portable` 本地构建。
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
- 如果要开机后不打开设置窗口，请启动 `GestureSign.exe`，不要启动 `GestureSign.ControlPanel.exe`；普通开机启动选项因此指向后台守护进程。

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
- 鼠标绘制按钮是触发按钮，不是输出动作。`Mouse Actions` 可以在手势匹配后发送滚轮输入。单独滚动滚轮不会绘制鼠标手势，但可以按下文说明作为带条件的独立鼠标触发器使用。
- 多指手势需要多个触点同时接触。三指手势不等同于把同一条线画三次。
- 点和点击手势会被记录为点样本，所以预览中可能看起来像点而不是线。
- 单指触摸屏点击可以录制为手势样本。鼠标和触摸板训练仍需要移动，以避免把普通点击误录成手势。
- 在 `Gestures` 标签页双击手势可打开编辑窗口，重新绘制或重命名手势。
- 手势轨迹颜色可在 `Options` > `Gesture Trail` 中配置。使用 `Pick Color` 可固定颜色，使用 `Follow System Color` 可跟随当前 Windows DWM 主题色。
- `Options` > `Gesture Trail` 宽度的有效范围是 `0..30 px`。超出这个范围的已保存配置值现在会统一归一到和控制面板 slider 一样的边界内。
- `Options` > `Opacity` 的有效范围是 `0..1`。超出这个范围的已保存配置值现在会统一归一到和控制面板 slider 一样的边界内。

常用命令示例：

- `Hot Key` 发送一个快捷键，例如 `Ctrl+Alt+T`。
- 对于 `Left Alt`、`Right Shift` 等区分左右的修饰键，请使用 `Hot Key` 的额外按键下拉框，不要直接按键盘录入修饰键。
- `Send Keystrokes` 输入一串按键或文本。
- `Key Down/Up` 可以按住或松开 `Left Shift` 等按键；需要临时选择文本或保持修饰键按下时，把一个按住动作和另一个松开动作配对使用。
- `Launch Windows Store App`、`Open Default Browser`、`Open File`、`Run Command or Program` 用于启动应用或文件。
- `Search or Open Clipboard Text` 会直接打开剪贴板里像网址的文本；其他剪贴板文本会用默认浏览器搜索。若要处理选中文本，可把 `Hot Key`（`Ctrl+C`）、短 `Delay` 和该命令串起来。
- `Mouse Actions` 可以发送点击、滚轮和其他鼠标输入。
- `Mouse Actions` > `Hold Down` 可以与后续 `Release` 配对使用。当 hold-down 命令在连续手势捕获仍进行中触发时，GestureSign 会在该次捕获结束或取消时自动释放被按住的鼠标按钮，降低鼠标键卡住风险。
- `Add Current Application to Ignored List` 会把当前前台应用加入忽略列表，适合某个应用的原生输入和手势冲突时使用。
- `Show Notification` 会显示短暂的 Windows 托盘通知，可配置标题、消息和显示时长。
- `Open GestureSign Control Panel` 可以通过手势打开设置窗口。
- `Repeat Last Command` 会重复上一次成功执行的命令。
- `Next Virtual Desktop` 和 `Previous Virtual Desktop` 用于切换 Windows 虚拟桌面。

为同一行为绑定多个手势：

- 一个动作只能分配一个手势，但可以通过复制动作，把多个手势绑定到同一组命令。
- 在 `Actions` 标签页复制现有动作，选择 `Paste To New Action`，再编辑粘贴出来的动作并选择另一个手势。
- 每个动作中的命令都会从上到下执行。如果多个手势应执行完全相同的行为，请保持复制动作的命令顺序一致。
- 如果只想在某个应用中禁用一个全局手势，在该应用下创建或复制一个使用相同手势的动作，并关闭该应用专属动作中的所有命令。这个应用专属动作会阻止回退到全局动作，但该应用中的其他全局手势仍可继续生效。
- 如果某个应用启用了 `匹配当前激活的窗口`，捕获开始阶段现在也会从一开始就按该激活窗口来应用规则，而不是只在最终手势识别时才生效。这样白名单检查、触点数量限制、block-touch 阈值，以及受保护的单指触摸板捕获都会和最终真正命中的应用保持一致。
- 这种激活窗口对齐现在也会继续贯穿到命令执行阶段，所以窗口类命令和 `%GS_WindowHandle%` 也会沿用同一个非 shell 前台窗口目标，不会在动作执行过程中又退回按坐标点重新找窗口。
- 对非正则的应用规则来说，Match String 现在会在保存/导入冲突检测时忽略首尾空格，与运行时已有的去空格比较语义保持一致。正则规则仍然要求完全相同的模式文本，并会在保存时先校验正则是否合法。

`Continuous Gesture`（连续捕获/连续触发）：

- GitHub Releases 里的 `continuous` 指的是“滚动预发布测试构建”这个发布渠道；动作编辑器里的 `Continuous Gesture` 指的是“同一段仍在进行中的输入捕获里重复触发动作”。两者只是英文单词相同，含义不同。
- 就行为来说，`Continuous Gesture` 不是等到抬手后才识别。只要 GestureSign 已经接受了当前这段输入捕获，手指、笔、触摸板或鼠标还在移动时，命令就可以开始重复触发。
- 实现路径：`PointCapture` 负责开始捕获，并在接受到有效移动时持续发出 `PointCaptured` 更新；`ContinuousGestureTrigger` 在捕获仍处于活动状态时监听这些更新，把最新点和上一次接受的点做比较，判断主方向是 `Up`、`Down`、`Left` 还是 `Right`，再根据移动距离、经过时间，以及按当前屏幕 DPI 缩放后的 `Options` > `Continuous Gesture Distance` 计算本次应该触发几次。
- 每跨过一次完整阈值，就会立刻触发一次匹配的连续动作。移动更快时，一次更新里也可能触发多次。如果这一段捕获里已经触发过连续动作，那么同一段输入在抬手后的普通手势匹配会被取消，避免同一段操作把命令执行两遍。
- 配置步骤：
  1. 创建或编辑一个动作。
  2. 打开 `Continuous Gesture`。
  3. 选择触点数量和一个方向。
  4. 添加适合重复执行的命令。
  5. 在 `Options` > `Continuous Gesture Distance` 调整触发距离。
- 连续手势适合滚动、音量、亮度、虚拟桌面切换、重复快捷键，或用重复 `Page Up` / `Page Down` 做“快速翻页”这类需要连续执行的行为。不希望重复执行的命令不要放在连续手势里，除非你明确需要重复。
- `Mouse Actions` > `Hold Down` 在连续捕获结束或取消时会自动释放，降低连续拖拽类工作流中鼠标按键卡住的风险。

单指触摸板边缘工作流：

- 单指精确触摸板捕获默认受保护，因为普通单指触摸板移动就是鼠标指针移动。只有动作允许 `TouchPad`、使用一个触点、至少有一个启用命令，并且触发条件在触摸开始时已经为真，GestureSign 才会捕获该一指触摸板动作。捕获开闸会先检查当前应用里真正命中的一指触摸板条件动作，只有在普通全局回退本来就允许时，才会继续回退到全局一指触摸板动作；因此，不相关的应用专属一指动作不会把全局边缘手势提前挡掉。
- 这种保护是否足够“不会误截普通移动”，取决于你的条件写得够不够窄。如果条件很宽，比如只写 `key_is_alt_down`，那么在该条件成立期间，普通单指指针移动仍可能被捕获。
- 要做右侧边缘类似滚动条的区域，创建两个动作：一个一指 `Up` 的 `Continuous Gesture`，一个一指 `Down` 的 `Continuous Gesture`。两个动作都启用 `TouchPad`，并添加方向相反的 `Mouse Actions` > `Vertical Scroll` 命令。
- 建议按这个顺序配置：
  1. 建一个 `Up` 动作，再建一个 `Down` 动作。
  2. 保留 `TouchPad`，如果你不希望别的输入来源触发它，就把其他 source device 关掉。
  3. 给两个动作添加可重复命令，例如 `Mouse Actions` > `Vertical Scroll`；如果你想要更接近“快速翻页”，也可以改成重复的 `Page Up` / `Page Down` 热键。
  4. 给两个动作都写上同一个起始区域条件，避免普通单指移动被截获。
- 用起始区域条件限制捕获范围，避免普通触摸板移动被截获。右侧边缘中段示例：`finger_1_start_X%>=95 AND finger_1_start_Y%>=10 AND finger_1_start_Y%<=90`。
- 如果还想用修饰键保护，把修饰键写进同一个条件，例如 `key_is_alt_down AND finger_1_start_X%>=95 AND finger_1_start_Y%>=10 AND finger_1_start_Y%<=90`。
- 滚动是动态触发的：手指滑动过程中的移动事件会触发命令，不需要等到抬手才开始滚动。
- `start_*` 变量决定是否允许开始捕获一指触摸板动作。`end_*` 变量会在执行过程中更新，但不能作为受保护一指触摸板捕获的唯一启动条件。
- 百分比坐标使用 Windows 虚拟屏幕边界，不是触摸板硬件表面的物理百分比。多显示器或特殊缩放环境下，需要按真实设备测试后调整边缘百分比。

独立滚轮触发：

- 可以在动作的 `Mouse HotKey` 字段选择 `Wheel Forward` 或 `Wheel Backward`。
- 独立滚轮触发必须有非空触发条件，并且至少有一个启用命令。这样可以避免普通滚动被全局截获。
- 独立滚轮触发现在也会遵守 `匹配当前激活的窗口`，所以应用专属的带条件滚轮动作会和手势捕获走同一套激活窗口匹配逻辑。
- 独立滚轮触发和 mouse hotkey 触发现在只有在“至少有一个匹配动作仍然有启用命令，并且它当前的触发条件求值为真”时，才会真正吞掉底层滚轮/按键输入。如果所有匹配动作都是空动作、已禁用，或当前条件为假，原始输入会继续透传。
- 它适合明确的边缘或角落工作流，例如只在光标或手势条件位于某个配置区域时触发。不要配置无条件全局滚轮动作。

默认浏览器匹配：

- 新内置默认配置会精确匹配常见浏览器可执行文件名：`msedge`、`chrome`、`firefox`、`iexplore` 和旧版 `MicrosoftEdge`，带不带 `.exe` 都可匹配。
- 已有用户配置不会自动迁移。如果旧浏览器组无法匹配 Chromium Edge，请编辑该应用规则，使用 executable/process match，例如 `^(MicrosoftEdge|firefox|chrome|iexplore|msedge)(\.exe)?$`。

手势和触发条件说明：

- 触摸屏、触摸板、触控笔和鼠标是不同输入来源。如果某个手势只在某类设备上生效，请检查动作的 ignored-device 设置。
- 单个动作可以同时允许触摸屏和触摸板输入，只要保持这两个来源启用即可。如果同一个手势要按设备执行不同命令，请复制动作，并在每个副本里禁用不需要的输入来源。
- 单指触摸屏动作现在也可以使用与受保护单指触摸板动作相同的“带条件显式开闸”模型。只要某个单指触摸屏动作有启用命令，并且它的触发条件在触摸开始时已经成立，GestureSign 就可以允许这次单指触摸屏捕获，而不必把该应用或全局的整体手指数限制一并降到 1。
- 触控笔手势需要 `Options` > `Pen Gesture`，并至少选择一种绘制模式（`Tip` 或 `Hover`）。笔侧键/右键按钮或翻转笔现在可以作为可选的附加激活门槛，而不再是强制要求；打开该选项时，默认会先启用简单的 `Tip` 绘制。如果触控笔驱动把按钮暴露成普通鼠标右键而不是 HID 笔输入，请尝试启用 `Mouse Gesture` 并使用鼠标右键。
- 对无源笔或没有可用侧键的笔，启用 `Pen Gesture` 后保留 `Tip`，并把可选激活条件的复选框都关掉即可。如果这样仍然画不出手势，剩余阻塞点通常是驱动是否向 Windows 暴露了兼容的 HID pen 输入。
- 在只使用 `Hover` 的触控笔模式下，如果之后笔尖又碰到屏幕，当前笔手势现在也会继续保持，而不会在中途把这段 stroke 状态丢掉。
- `Options` > `使用笔时忽略触摸输入` 现在只会在触控笔真正上报活跃 HID pen 状态时才抑制触摸，而不会再对每一个 pen raw-input 包都立即生效。这能降低部分 Win11 设备上被动悬停噪声或空闲笔报告误杀触摸的概率。
- 连续手势会在手指仍在移动时运行，但必须先超过 `Options` > `Continuous Gesture Distance`。该距离会按捕获手势所在屏幕的 DPI 缩放。如果短滑动在超过该距离前就松开，匹配的普通手势仍可能先运行。
- `Options` > `Minimum Point Distance` 的有效最小值是 `3 px`，`Options` > `Continuous Gesture Distance` 的有效最小值是 `5 px`。低于这些下限的已保存配置值现在会统一归一到和控制面板一致的最小值。
- 单指精确触摸板手势默认受保护。只有动作允许 `TouchPad`、手指数为 1、至少有一个已启用命令，并且触发条件在触摸开始时已经成立时，GestureSign 才会捕获该一指触摸板动作，避免劫持普通指针移动。
- 要做右边缘触摸板滚动条效果，可以分别创建一指连续 `Up` 和 `Down` 动作，启用 `TouchPad`，命令使用 `Mouse Actions` > `Vertical Scroll`，并填写起始区域条件，例如 `key_is_alt_down AND finger_1_start_X%>=95 AND finger_1_start_Y%>=10 AND finger_1_start_Y%<=90`。滚动命令会在手指移动过程中动态重复触发，不需要等到抬手。
- `Drawing Start Timeout` 只取消未及时开始绘制的手势；`Whole Gesture Timeout` 会在设定时长后取消整段捕获，并且与 drawing-start timeout 互斥；`Composite Gesture Timeout` 只控制组合手势等待下一段手势的时间。
- `Drawing Start Timeout` 的有效范围是 `0..2.0 s`，`Whole Gesture Timeout` 的有效范围是 `0..10.0 s`，`Composite Gesture Timeout` 的有效范围是 `0..2.0 s`。超出这些控制面板范围的已保存配置值现在会在运行时统一归一到相同边界内。
- 精确触摸板手势依赖驱动暴露 raw HID touchpad input。如果触摸板或第三方驱动无法检测，先关闭冲突的 Windows 触摸板手势，并确认设备显示为精确触摸板，而不是只暴露为鼠标滚轮或厂商私有输入。
- 如果某个第三方驱动确实以标准 `TouchPadUsage` 暴露 raw HID precision touchpad，它现在不会再仅因设备路径里带 `ROOT` 或 `VIRTUAL_DIGITIZER` 就被提前过滤；若仍然无法使用，需要继续确认该驱动是否实际上只暴露成鼠标、触控笔或厂商私有 HID 报告。
- daemon 日志现在也会记录一次性的 raw digitizer 设备校验信息，包括 usage page、usage、device path，以及该设备最终是被接受还是被忽略。对于 Win11 下的第三方触摸板驱动，例如 reWASD 或 Apple Magic Trackpad 驱动，这是确认 Windows 实际向 GestureSign 暴露了什么输入类型的最快方式。
- daemon 日志现在还会记录一次性的 raw input 设备清单，包括那些最终被 Windows 暴露成 mouse、keyboard，或非 digitizer HID 的路径。对于 Win11 第三方触摸板驱动，建议同时收集 `Enumerated raw input device:` 和 `Raw digitizer device validation:` 这两类日志行。
- 现在的构建会在 daemon 启动和后续设备状态刷新时自动输出这些 raw input 清单/校验日志，所以通常只需要复现一次问题，再去查看日志即可。
- 某些 precision touchpad 驱动也会只上报当前活动触点数，但真实触点仍放在后面的逻辑槽位里。现在 GestureSign 在这种情况下也会按完整触摸板逻辑槽位宽度解析，降低后面槽位里的有效触点被跳过的概率。
- 不同的 raw HID 触摸板设备现在也会按设备句柄区分，而不只是按 `TouchPadUsage` 这一类输入源统一看待，因此一个触摸板的活跃数据流不容易再被误认为另一个同类触摸板的后续输入。
- 触摸板 timeout/cancel 之后的“忽略直到抬手”状态现在也会绑定到同一个物理/raw 设备句柄，而不是把这一类所有触摸板一起静音。
- 当某个 touch 设备捕获 timeout 或取消时，GestureSign 现在也会立刻释放这个设备对应的当前 touch-source 所有权，而不是非得等它再来一帧 raw 输入，其他同类设备才能接管。
- 同一条 timeout/cancel 路径现在也会同步释放底层 raw-input touch-source 占用，因此其他 touch 设备不需要再傻等旧输入源的 stale timeout 过期才能接管。
- 如果某个 touch 设备在捕获刚开始时就被当前应用规则、白名单模式或手指数限制拒绝，现在也会走这条“立即释放”路径，所以一次短暂被拒绝的触摸屏起手不容易再把下一次触摸板 source 挡住。
- 如果一次 touch 手势在捕获结束后的识别阶段被取消，例如连续手势已经先触发并压掉了普通手势结果，现在也会走这条“立即释放”路径。
- 这条“立即释放”现在也覆盖到正常完成的 touch 设备手势，所以一次普通 finger-up 结束后，下一台触摸屏或触摸板设备也不需要再等旧 source ownership 过期才能接管。
- 这条即时释放路径现在也覆盖到触摸屏捕获，所以被取消或超时的触摸屏 stroke 不容易再把后续 touch 设备源拖住。
- spacedesk 这类虚拟显示或远程触摸工具必须向 Windows 暴露兼容的 digitizer/touchscreen input。如果 Windows 内置触摸手势无法通过虚拟显示工作，GestureSign 通常也无法捕获这路输入。
- `Block Touch Input` 仅在 UIAccess 构建中可用，并按匹配应用配置。GestureSign 需要足够触点来识别手势后才开始阻断，所以很早期的触摸帧仍可能传给目标应用。它只重定向 touch pointer 帧，不是对精确触摸板、触控笔、鼠标、键盘或应用原生输入的完整系统事件阻断器。
- `Block Touch Input` 小于 `2` 的值现在会一致地按 `Off` 处理，所以对话框、保存后的用户配置以及内置默认配置使用的是同一套阈值语义。
- 应用级 `Number Of Fingers` 现在会统一归一到 `1..10`，应用级 `Block Touch Input` 会统一归一到 `0..10`，其中小于 `2` 的值按 `Off` 处理，与应用设置对话框的边界保持一致。
- 如果 Windows 拒绝了某次 touch-blocking 反注册，GestureSign 现在也会先清理这一轮留在内存里的 pointer/frame 状态，降低旧的 touch-blocking 状态串到后续输入处理里的概率。
- 重新启用或关闭 `Block Touch Input` 时，现在也会先清理上一轮残留的 pointer-ID/frame 状态，降低应用切换或阈值变化后旧的注入触点状态串到下一轮拦截里的概率。
- 如果启用 touch blocking 后触摸键盘或浏览器原生触摸行为异常，先降低该应用的 block threshold，或为该应用关闭 touch blocking。
- 触发条件可以使用 `finger_1_start_X`、`finger_1_start_Y`、`finger_1_end_X`、`finger_1_end_Y`、百分比变量如 `finger_1_start_X%`，以及 `finger_1_ID`。
- 可以用百分比触发条件模拟边缘手势，例如 `finger_1_start_X%<5`、`finger_1_start_X%>95`、`finger_1_start_Y%<5`、`finger_1_start_Y%>95`。
- 触发条件可以按手指位置、窗口状态或按住的修饰键，把同一个手势路由到不同命令。区域工作流可用坐标范围近似，例如 `finger_1_start_X%>=25 AND finger_1_start_X%<50`，但这不是独立的 Android 风格边缘手势或悬浮球功能。
- 当同一作用域里同一个手势同时存在“带条件动作”和“无条件兜底动作”时，GestureSign 现在会优先执行实际命中的带条件动作，而不会再把无条件兜底动作一并执行。
- 导入或手工编辑得到的 `Match Using = All` 应用规则，现在在主运行时应用匹配路径里也会保持一致匹配。这个改动主要影响旧配置或手工改过的数据；内置的全局应用仍然是常规的兜底作用域。
- 组合手势等待和 tap 后续匹配现在也会判断“这个手势在当前设备、当前命令启用状态、当前触发条件下是否真的可执行”，而不再只是看有没有同名动作存在。
- 独立热键触发现在也会走与其他触发路径相同的“可执行动作”检查。只有当至少有一个匹配动作仍然有启用命令、对应插件可用，并且当前触发条件为真时，注册的热键才会真正触发。
- 对于抬手后才识别的普通手势，`key_is_alt_down`、`key_space_down` 这类按键条件现在会按“手势开始时”的按键快照求值，而不再随动作真正执行时刻的按键状态漂移。
- 对单指触摸板保护来说，捕获开闸条件会在触摸开始时判断。边缘保护应使用 `start_*`、修饰键和窗口状态变量；`end_*` 变量在动作执行时仍表示最新捕获点，但不能作为开始捕获一指触摸板 stroke 的唯一理由。
- 尚未实现真正类似 BetterTouchTool 的 tip-tap。可以用 `finger_1_ID<finger_2_ID` 或 `finger_1_ID>finger_2_ID` 这类触发条件做一次性近似，但两个触点必须同时被捕获，而且部分触摸板驱动不会暴露稳定触点 ID。区分普通 tap 与 tip-tap、区分左/右 tip-tap 需要改造识别器/模型。
- 窗口条件可以使用 `window_is_maximized`、`window_is_minimized`、`window_is_fullscreen`。
- 修饰键条件可以使用 `key_is_shift_down`、`key_is_ctrl_down`、`key_is_alt_down`、`key_is_win_down`。任意虚拟键条件可使用 `key_<按键名>_down`，例如 `key_space_down`、`key_a_down` 或 `key_page_up_down`；条件编辑器可以通过聚焦按键框并按下目标键来插入。
- 多个触发条件可使用 SQL 风格运算符，例如 `AND`、`OR` 和括号：`finger_1_start_X<500 AND finger_1_end_X<450`。
- 条件按 Windows 虚拟屏幕坐标计算。百分比变量使用 Win32 虚拟屏幕像素边界，与混合 DPI、多屏幕以及位于主屏左侧或上方显示器上的触摸坐标保持一致。
- GestureSign 不能发送独立的 `Fn` 键。大多数键盘的 `Fn` 由固件处理，不会作为普通虚拟键暴露给 Windows。请使用 `Fn` 组合实际生成的键，例如驱动暴露出的 `F1`-`F24`、音量、亮度或媒体键。

## 故障排查

Windows 11 触摸/手势冲突：

- Windows 11、浏览器和部分应用仍可能在 GestureSign 之前或同时处理原生触摸/精确触摸板手势。关闭 Windows 三指/四指手势不一定能阻止每个应用自己的缩放、滚动或双指行为。
- 优先使用不与目标应用原生输入冲突的手势形状和手指数，或按上文说明添加应用专属动作/忽略应用。`Block Touch Input` 只在 UIAccess 构建中有帮助，并且仍有手势说明中记录的初始帧限制。

通用排查：

- 如果没有任何手势运行，确认托盘守护进程正在运行，并从控制面板重启 GestureSign。
- 如果只有手动打开 GestureSign 后手势才工作，请启用 `Options` > `Start GestureSign on Windows Startup`，或为 `GestureSign.exe` 添加启动快捷方式/任务，而不是启动控制面板可执行文件。
- 如果鼠标无法开始绘制，确认 `Options` > `Mouse Gesture` 已启用，且至少选择了一个绘制按钮。
- 如果浏览器动作无法匹配 Chromium Edge，请用 executable filename 或 process matching 添加/编辑 `msedge.exe` 的应用规则。内置浏览器组会按完整可执行文件名匹配常见浏览器进程，包括 `msedge`、`chrome`、`firefox`、`iexplore` 和旧版 `MicrosoftEdge`，带不带 `.exe` 都可匹配。Firefox 分离出来的标签页窗口理论上仍应按可执行文件匹配，但浏览器窗口类和标签页分离行为会随版本变化，失败时需要按具体窗口验证。
- 如果触控笔悬停或按下侧键时 Chrome 忽略鼠标动作，请确认笔驱动把输入暴露为 HID pen state 还是普通鼠标输入。HID 笔输入使用 `Pen Gesture`，把侧键暴露为鼠标右键的驱动可尝试 `Mouse Gesture`。
- 如果断开外接显示器后 GestureSign 退出或无响应，请重启托盘守护进程并查看日志。显示器变化处理现在会释放当前输入、清理触摸屏映射缓存、在 UI 消息上下文重新注册 raw input，并在系统临时拿不到显示器边界时跳过该输入帧，而不是把它当成致命错误。
- 如果耗电偏高，关闭不用的输入来源，并避免在长时间平板使用场景里启用低阈值单指手势。后台守护进程运行时会监听 raw digitizer input，具体功耗影响需要在受影响硬件上 profiling。
- 如果手势只在 Task Manager、Device Manager、安装程序或其他管理员窗口中失败，请查看下方“管理员窗口”说明。
- 如果某个动作在一个应用里不运行，检查该应用是否在忽略列表中，或者该动作是否只配置给了其他应用。
- 某些 Win11 触摸屏驱动会把活动 `ContactCount` 报得比较小，但真实触点仍放在后面的逻辑槽位里。现在 GestureSign 在这种情况下会按完整触摸屏逻辑槽位宽度解析，降低后面槽位里的有效触点被跳过的概率。
- 触摸屏屏幕解析现在在多显示器场景下更保守：如果已缓存的 touchscreen -> screen 映射和当前前台窗口唯一匹配、或物理尺寸唯一匹配的结果相冲突，GestureSign 会刷新到新匹配的屏幕；如果缓存映射不可用且这些启发都失败，GestureSign 会直接跳过这次不可靠的触摸屏输入帧，而不是盲猜前台窗口屏幕或光标屏幕，避免把整段触摸错误地映射到别的显示器上。
- 对依赖光标的触控笔和触摸板 fallback 路径，屏幕选择现在也会优先使用光标当前所在的显示器，再回退到前台窗口显示器。这样它们的坐标会和基于光标的应用匹配、触摸板起始条件落在同一块屏幕上。
- 如果 GestureSign 干扰某个应用，把 `Add Current Application to Ignored List` 绑定到一个手势，在该应用激活时运行它。
- 如果希望默认不干扰任何未配置应用，请启用 `Options` > `白名单模式`。该模式下 GestureSign 只在目标前台/捕获窗口匹配已配置应用时捕获手势和热键；未匹配应用会被忽略，已匹配应用仍可按规则回退到全局动作。
- 如果触摸板手势延迟或丢失，提高 `Options` 中的 drawing-start timeout。
- 如果 Win11 下的第三方触摸板驱动仍然检测不到，先重启托盘 daemon，复现一次问题，再打开 `%LOCALAPPDATA%\GestureSign\GestureSign.log`，搜索 `Enumerated raw input device:` 和 `Raw digitizer device validation:`。这两类日志能直接看出 Windows 究竟把设备暴露成了 `TouchPad`、其他 HID usage，还是仅仅鼠标/键盘输入。
- 如果触摸屏输入结束后，触摸板手势还会暂时被上一种输入源卡住，请使用包含 raw-input source-staleness 修复的版本。现在触摸屏/触摸板输入源的超时刷新会基于“完整输出帧完成”而不是每个半截 raw 包，因此有噪声或不完整的触摸屏 raw 包流不容易在触摸停止后继续把触摸板挡住。
- 活跃中的 touch 捕获现在也不会再因为 stale timeout 被别的 touch 设备中途抢走，因此当前触点只是短暂停住、但手势还没真正结束时，不容易再被另一台 touch 设备打断。
- 鼠标滚轮旋转可以作为独立鼠标触发器使用：在动作的 mouse hotkey 字段选择 `向前滚动` 或 `向后滚动`。独立滚轮触发必须设置触发条件，例如角落或边缘条件，避免普通滚动被全局截获。
- 单指触摸板手势默认会被忽略，只有动作设置了触发条件并且触摸开始时已经满足条件才会捕获。右侧边缘中段可以使用 `finger_1_start_X%>=95 AND finger_1_start_Y%>=10 AND finger_1_start_Y%<=90`；再配合 `Continuous Gesture` 的单指 `Up`/`Down` 和鼠标垂直滚动命令，就能在手指移动过程中实现类似滚动条的快速翻页。如果某个应用自己也配置了一指触摸板动作，不相关的应用专属条件也不会再阻止原本应当允许全局回退的受保护一指动作启动。
- 测试大规模配置变更前，先使用 `Options` > `Backup User Data`。

## 上游 Issue 覆盖情况

本 fork 针对以下上游 `TransposonY/GestureSign` issues 做了修复、实现或文档覆盖。

已实现或已覆盖：

| Issue | 覆盖情况 |
| --- | --- |
| [#139](https://github.com/TransposonY/GestureSign/issues/139) | 文档说明了复制动作的流程，可把多个手势绑定到同一组命令。 |
| [#138](https://github.com/TransposonY/GestureSign/issues/138), [#95](https://github.com/TransposonY/GestureSign/issues/95), [#62](https://github.com/TransposonY/GestureSign/issues/62) | 支持单指触摸屏手势和单指触摸屏点击训练。现在带条件的单指触摸屏动作也可以显式启用单指捕获，而不需要把无关触摸手势的整体手指数限制一并降到 1。 |
| [#137](https://github.com/TransposonY/GestureSign/issues/137) | 添加未识别手势提示音，并支持自定义 `.wav` 文件。 |
| [#136](https://github.com/TransposonY/GestureSign/issues/136), [#90](https://github.com/TransposonY/GestureSign/issues/90) | 手势训练帮助会说明可用输入来源，以及何时需要先启用鼠标手势。训练界面在鼠标手势关闭时也可以直接一键启用默认的右键绘制配置。 |
| [#134](https://github.com/TransposonY/GestureSign/issues/134), [#122](https://github.com/TransposonY/GestureSign/issues/122) | 连续手势距离可配置、已文档化，修复了刚到阈值时首个触发被丢弃的问题，让距离阈值按手势所在屏幕 DPI 缩放，并且只有当前确实可执行的连续动作真正触发后，才会压掉同一段输入的普通手势。 |
| [#133](https://github.com/TransposonY/GestureSign/issues/133), [#111](https://github.com/TransposonY/GestureSign/issues/111) | 构建脚本和文档覆盖 Windows 11 on Arm64 以及可选 native Arm64 输出。 |
| [#132](https://github.com/TransposonY/GestureSign/issues/132), [#125](https://github.com/TransposonY/GestureSign/issues/125) | 文档说明 `Fn` 限制，Hot Key UI 更容易选择 F1-F24、媒体键和功能键。 |
| [#117](https://github.com/TransposonY/GestureSign/issues/117) | 文档说明了当前 winget 包的安装和更新命令。 |
| [#121](https://github.com/TransposonY/GestureSign/issues/121) | setup `.exe` 现在会显示安装目录选择页；静默安装可使用 Inno Setup 的 `/DIR=` 覆盖目标目录。 |
| [#115](https://github.com/TransposonY/GestureSign/issues/115), [#40](https://github.com/TransposonY/GestureSign/issues/40) | 虚拟桌面切换动作可用，可分配给普通手势或连续手势。 |
| [#113](https://github.com/TransposonY/GestureSign/issues/113) | 应用专属的同手势动作即使命令被禁用，也会阻止回退到全局动作，因此可以只在某个应用中排除一个全局手势，而不影响该应用中的其他全局手势。启用 `匹配当前激活的窗口` 的应用规则现在也会从捕获开始阶段、命令执行阶段以及独立带条件滚轮触发中一致生效，让按应用限制、`%GS_WindowHandle%` 和受保护触摸板捕获与最终命中的应用保持一致。 |
| [#109](https://github.com/TransposonY/GestureSign/issues/109), [#131](https://github.com/TransposonY/GestureSign/issues/131) | 对切换窗口/桌面动作的 shell/taskbar 激活和修饰键释放做了加固。内置的 Alt+Tab / Shift+Alt+Tab / Win+Ctrl+方向键动作，以及等价的 `Hot Key` 快捷键，现在在成功路径上也会强制复位相关修饰键，而不只是依赖普通的 key-up 注入。 |
| [#97](https://github.com/TransposonY/GestureSign/issues/97), [#31](https://github.com/TransposonY/GestureSign/issues/31) | 控制面板启动时会容忍 Windows Application Event Log 不可用；配置写入会 flush、串行化，并在 Desktop Bridge 构建中写入 package local state。 |
| [#104](https://github.com/TransposonY/GestureSign/issues/104) | `Add Current Application to Ignored List` 可用并已文档化。 |
| [#93](https://github.com/TransposonY/GestureSign/issues/93), [#92](https://github.com/TransposonY/GestureSign/issues/92) | `Key Down/Up` 支持保持修饰键按下，Hot Key 中区分左右的修饰键已在 UI 和 README 中说明。 |
| [#84](https://github.com/TransposonY/GestureSign/issues/84), [#65](https://github.com/TransposonY/GestureSign/issues/65), [#118](https://github.com/TransposonY/GestureSign/issues/118) | 本 README 替代不可用的外部指南，覆盖设置、使用、故障排查和构建说明；控制面板帮助按钮会跳转到这里。 |
| [#76](https://github.com/TransposonY/GestureSign/issues/76) | 组合手势等待时间可配置。 |
| [#30](https://github.com/TransposonY/GestureSign/issues/30) | 添加可选的整段手势超时，可在设定时长后取消捕获，并与手势起始超时互斥。 |
| [#75](https://github.com/TransposonY/GestureSign/issues/75), [#32](https://github.com/TransposonY/GestureSign/issues/32) | 触发条件支持触摸坐标变量、多个条件表达式，并让百分比坐标与 Win32 虚拟屏幕像素边界对齐。 |
| [#66](https://github.com/TransposonY/GestureSign/issues/66), [#24](https://github.com/TransposonY/GestureSign/issues/24), [#52](https://github.com/TransposonY/GestureSign/issues/52) | 触发条件可以检测任意按住的虚拟键，例如 `key_space_down`、`key_a_down` 和 `key_page_up_down`，条件编辑器也可以捕获按键名并插入；对普通手势来说，这些按键条件现在会按手势开始时的按键快照求值，而不是按更晚的动作执行时刻求值。 |
| [#74](https://github.com/TransposonY/GestureSign/issues/74) | 重新注入触摸 `UP` 帧时为最后一个触点保留 `INRANGE`。 |
| [#73](https://github.com/TransposonY/GestureSign/issues/73), [#56](https://github.com/TransposonY/GestureSign/issues/56) | 对 Windows 10/11 下触摸键盘命令行为和 TabTip 路径查找做了加固。 |
| [#72](https://github.com/TransposonY/GestureSign/issues/72) | 可在 `Options` 中关闭触摸屏手势。 |
| [#70](https://github.com/TransposonY/GestureSign/issues/70) | 触摸屏显示器选择不再只依赖光标位置：缓存映射可以按当前前台窗口唯一匹配结果更新，而无法可靠判定的输入帧会被直接跳过，不再盲映射到前台窗口屏幕或光标屏幕；依赖光标的触控笔/触摸板 fallback 也会优先使用光标所在显示器。 |
| [#57](https://github.com/TransposonY/GestureSign/issues/57), [#9](https://github.com/TransposonY/GestureSign/issues/9) | 对 touch blocking 做了加固，指针捕获丢失/取消帧会释放内部注入触点 ID，并文档化 UIAccess、按应用配置、初始帧等限制。 |
| [#51](https://github.com/TransposonY/GestureSign/issues/51) | 精确触摸板手势会遵守 drawing-start timeout。 |
| [#83](https://github.com/TransposonY/GestureSign/issues/83), [#89](https://github.com/TransposonY/GestureSign/issues/89), [#69](https://github.com/TransposonY/GestureSign/issues/69) | 带条件的一指精确触摸板捕获支持受保护的边缘/区域工作流，包括右边缘连续滚动；当起始条件写得足够窄时，可以避免截获普通一指触摸板移动；同一作用域里命中的带条件动作现在也会优先于同手势的无条件兜底动作执行。 |
| [#50](https://github.com/TransposonY/GestureSign/issues/50) | 手势轨迹颜色可用 `Pick Color` 固定，也可用 `Follow System Color` 重置为跟随 Windows DWM 主题色。 |
| [#49](https://github.com/TransposonY/GestureSign/issues/49) | 备份/设置恢复支持当前备份和旧版动作/手势导出。 |
| [#46](https://github.com/TransposonY/GestureSign/issues/46) | 添加可选白名单模式，未匹配应用默认被忽略；已配置用户应用仍可使用并可回退到全局动作。 |
| [#48](https://github.com/TransposonY/GestureSign/issues/48), [#27](https://github.com/TransposonY/GestureSign/issues/27) | 管理员窗口、开机启动、静默启动 daemon 和便携模式已文档化。 |
| [#60](https://github.com/TransposonY/GestureSign/issues/60) | 开机启动说明现在明确无人值守启动应指向 `GestureSign.exe`；`GestureSign.ControlPanel.exe` 是设置界面。 |
| [#44](https://github.com/TransposonY/GestureSign/issues/44), [#37](https://github.com/TransposonY/GestureSign/issues/37) | `Repeat Last Command` 和 `Open GestureSign Control Panel` 动作可用。 |
| [#110](https://github.com/TransposonY/GestureSign/issues/110) | 添加 `Show Notification` 动作，可通过 daemon 托盘图标显示短暂的任务完成弹窗。 |
| [#43](https://github.com/TransposonY/GestureSign/issues/43) | 添加 `Search or Open Clipboard Text`，可在选中文本复制到剪贴板后执行浏览器搜索或打开 URL。 |
| [#33](https://github.com/TransposonY/GestureSign/issues/33) | 鼠标手势可配置多个绘制按钮，例如同时使用右键和中键。 |
| [#38](https://github.com/TransposonY/GestureSign/issues/38) | 控制面板触摸板滚动使用小数滚轮增量处理，不再把每个小 delta 当成完整滚轮刻度。 |
| [#41](https://github.com/TransposonY/GestureSign/issues/41) | 鼠标滚轮旋转可作为带条件的独立鼠标触发器使用，边缘/角落滚轮工作流不再需要按住绘制按钮；空动作或当前条件为假的 wheel / mouse hotkey 触发器也不再吞掉原始输入。 |
| [#19](https://github.com/TransposonY/GestureSign/issues/19), [#126](https://github.com/TransposonY/GestureSign/issues/126) | Hot Key 和内置窗口命令可覆盖常见无障碍快捷键和隐藏窗口工作流。 |

已有改进但未在无硬件验证或无更大功能设计的情况下完全关闭：

| Issue | 当前状态 |
| --- | --- |
| [#129](https://github.com/TransposonY/GestureSign/issues/129), [#124](https://github.com/TransposonY/GestureSign/issues/124), [#54](https://github.com/TransposonY/GestureSign/issues/54) | 已文档化本 fork 的项目状态：社区 fork，包含选择性的维护修复、文档和构建/发布自动化，不是上游官方延续，也不承诺完整路线图。 |
| [#107](https://github.com/TransposonY/GestureSign/issues/107), [#106](https://github.com/TransposonY/GestureSign/issues/106) | 已文档化当前技术状态：本 fork 目标为 `.NET Framework 4.8`，使用 Visual Studio 2022 构建，并可在 Windows 11 24H2/VS 2022 17.11+ 上选择 native Arm64 .NET Framework 输出；尚未迁移到 .NET 6/.NET 8。 |
| [#105](https://github.com/TransposonY/GestureSign/issues/105) | 已文档化 Windows 11 原生手势和应用触摸冲突限制；缓解方式是配置/按应用行为，而不是完整 OS 级覆盖。 |
| [#67](https://github.com/TransposonY/GestureSign/issues/67) | 已文档化 release executable 预期；workflow 会构建真正的 `Portable` 配置，校验控制面板和后台守护进程入口，并在创建 release 后上传 zip。 |
| [#82](https://github.com/TransposonY/GestureSign/issues/82), [#54](https://github.com/TransposonY/GestureSign/issues/54) | 已文档化捐赠/支持状态：本 fork 没有捐赠或赞助链接；支持方式是测试、可复现报告、文档修复或聚焦 PR。 |
| [#77](https://github.com/TransposonY/GestureSign/issues/77), [#78](https://github.com/TransposonY/GestureSign/issues/78), [#80](https://github.com/TransposonY/GestureSign/issues/80), [#100](https://github.com/TransposonY/GestureSign/issues/100) | 手势捕获受 Windows 完整性级别、shell/安全界面，以及先于 GestureSign 捕获输入的应用限制。管理员窗口说明覆盖 Task Manager；Timeline/Win+Tab 和 Parsec 仍需要按系统/应用验证。 |
| [#79](https://github.com/TransposonY/GestureSign/issues/79), [#81](https://github.com/TransposonY/GestureSign/issues/81) | Windows Store/UWP 启动和匹配路径已存在，包括 `Launch Windows Store App` 以及把 `ApplicationFrameWindow` 展开到 `Windows.UI.Core.CoreWindow`；但 Microsoft 应用失败仍需要按应用和 package 状态验证。 |
| [#85](https://github.com/TransposonY/GestureSign/issues/85) | 触摸板延迟、重复和轨迹锯齿报告按驱动/设备相关问题处理。触摸板/raw-input 处理已有改进，但仍需要受影响的 Synaptics 或厂商驱动硬件才能关闭。 |
| [#103](https://github.com/TransposonY/GestureSign/issues/103) | 显示器变化处理现在会释放活跃 raw input、清理触摸屏幕缓存、在 UI 消息上下文重新注册 raw input、重置手势轨迹表面，并在系统临时拿不到显示器边界时跳过输入帧；外接显示器热拔仍需要在对应笔记本/外接显示拓扑上验证。 |
| [#128](https://github.com/TransposonY/GestureSign/issues/128), [#120](https://github.com/TransposonY/GestureSign/issues/120) | Win11 平板/触摸屏解析对缺少标准 contact-count 或坐标范围数据的 HID 报告更宽容，会按 Win32 要求初始化 raw-device info buffer，支持无序 tip usage，改进 raw-input 诊断，并把输入源 stale-timeout 的刷新改为基于完整输出帧，降低卡住的触摸屏 raw 包流持续阻塞后续触摸板输入的概率；但仍需要具体设备验证。 |
| [#123](https://github.com/TransposonY/GestureSign/issues/123) | 快速点击时，如果新的活跃触点帧替换了过期触点集合，现在不会再直接丢掉该帧；同时会拆分“上一轮释放 + 下一轮按下”合并在同一 raw 帧里的情况，让上一次 tap 先结束、下一次 tap 再开始；组合手势/tap 后续等待也只会在该手势对当前设备和条件确实可执行时才继续成立；但高频触摸屏场景仍需要真实硬件验证。 |
| [#94](https://github.com/TransposonY/GestureSign/issues/94) | 简单触控笔手势现在可以只依赖 `Tip` 或 `Hover`，不再强制要求笔侧键/右键按钮或翻转笔作为激活门槛；只使用 `Hover` 的笔手势在后续笔尖触屏时也不会再把当前 stroke 状态丢掉。 |
| [#127](https://github.com/TransposonY/GestureSign/issues/127), [#119](https://github.com/TransposonY/GestureSign/issues/119), [#112](https://github.com/TransposonY/GestureSign/issues/112), [#55](https://github.com/TransposonY/GestureSign/issues/55) | 触控笔设置和文档更清晰，简单笔手势也不再强制要求侧键，但 Wacom/无源笔支持仍取决于驱动是否暴露兼容的 HID pen 或 touchpad 输入。 |
| [#135](https://github.com/TransposonY/GestureSign/issues/135), [#116](https://github.com/TransposonY/GestureSign/issues/116), [#114](https://github.com/TransposonY/GestureSign/issues/114), [#59](https://github.com/TransposonY/GestureSign/issues/59) | 精确触摸板处理已有改进，包括更安全的一指捕获开闸：会先检查应用专属的带条件触摸板动作，再在允许时回退到全局动作；同时，标准 `TouchPadUsage` 设备不再仅因 raw HID 设备路径里带 `ROOT` 或 `VIRTUAL_DIGITIZER` 就被过滤掉，同类触摸板源也会按设备句柄区分，降低多触摸板之间互相干扰的概率；raw input 设备清单和 digitizer 校验现在都会把 usage/path 细节写入日志，加快 Win11 第三方驱动诊断，但第三方/厂商驱动支持仍必须按设备验证。 |
| [#130](https://github.com/TransposonY/GestureSign/issues/130), [#87](https://github.com/TransposonY/GestureSign/issues/87), [#45](https://github.com/TransposonY/GestureSign/issues/45) | 连续捕获期间触发的鼠标 hold-down 动作会在捕获结束/取消时自动释放，降低鼠标键卡住风险；完整窗口拖拽/文件拖拽工作流仍需要硬件和 UI 验证。 |
| [#108](https://github.com/TransposonY/GestureSign/issues/108) | spacedesk 以及类似虚拟显示触摸路径取决于 Windows 是否暴露兼容的 raw HID digitizer/touchscreen input；需要用 spacedesk 设备验证。 |
| [#64](https://github.com/TransposonY/GestureSign/issues/64), [#63](https://github.com/TransposonY/GestureSign/issues/63), [#47](https://github.com/TransposonY/GestureSign/issues/47), [#26](https://github.com/TransposonY/GestureSign/issues/26) | tip-tap 目前只能用触点 ID 条件近似；区分 tap/tip-tap、区分左/右 tip-tap、旋转不敏感的五指捏合都需要改造识别器/模型，不是小配置改动。 |
| [#61](https://github.com/TransposonY/GestureSign/issues/61) | 原生/系统事件抑制仅限 UIAccess touch-pointer 重定向，并且要等捕获到足够触点后才生效；GestureSign 不能完整阻断精确触摸板、触控笔、鼠标、键盘或所有应用原生事件。 |
| [#58](https://github.com/TransposonY/GestureSign/issues/58) | 耗电报告需要在受影响的触摸屏/平板硬件上 profiling；可通过关闭不用的输入来源、避免长时间使用低阈值单指捕获来降低风险。 |
| [#34](https://github.com/TransposonY/GestureSign/issues/34) | Chrome 与悬停笔/侧键组合的行为取决于驱动暴露 HID pen state 还是鼠标输入，需要结合受影响浏览器和触控笔驱动验证。 |
| [#28](https://github.com/TransposonY/GestureSign/issues/28) | Firefox 分离标签页窗口理论上应按完整浏览器可执行文件/默认浏览器规则匹配，但剩余失败需要在受影响 Firefox 版本上逐窗口验证。 |
| [#91](https://github.com/TransposonY/GestureSign/issues/91) | 动作可以限定到多个输入来源，包括触摸屏和触摸板，但兼容的 raw HID 触摸板输入仍取决于驱动。 |
| [#53](https://github.com/TransposonY/GestureSign/issues/53) | 浏览器匹配说明现在明确 Chromium Edge 的 `msedge.exe`；新的默认浏览器规则使用完整可执行文件名匹配，但既有用户配置和 Edge 特定失败仍需要按规则和窗口验证。 |

## 构建

- 使用 Visual Studio 2022 打开 `GestureSign.sln`，或运行 `.\scripts\build.ps1`。
- 解决方案现在目标为 `.NET Framework 4.8`。
- NuGet 包使用 `packages.config` 恢复，所以首次构建前需要 restore。
- 本 fork 有意将生产构建保留在 `.NET Framework 4.8`，以兼容现有 WPF/Win32/HID 代码；这里不把 .NET 6 迁移视为已完成。
- Windows 11 on Arm64 使用 `Any CPU` 构建；不要添加 `ARM64` solution platform。如需 native Arm64 .NET Framework 输出，请在 Windows 11 24H2 + VS 2022 17.11 或更新版本上运行 `.\scripts\build.ps1 -Configuration Release -PreferNativeArm64`。

## GitHub Releases

- 推送 `v8.1.0` 或 `v8.1.0-beta.1` 这类 semver 风格 tag 会自动运行 release workflow。
- 每次推送到 `master` 也会更新 `continuous` 预发布版，提供最新便携构建用于手动测试。
- release workflow 现在也为同一个 ref/event 启用了 GitHub Actions 并发控制，所以在短时间连续推送时，较早但仍在运行的 `master` 任务会被取消，不会再把较新的 `continuous` 预发布覆盖回旧提交。
- GitHub release 里的 `continuous` 是滚动预发布构建标签，和应用功能 `Continuous Gesture` 不是同一个概念。
- workflow 会为安装器构建 `Release|Any CPU`，为 zip 构建 `Portable|Any CPU`，创建或更新 GitHub Release，并同时上传 `GestureSign-<tag>-setup-win-anycpu.exe` 和 `GestureSign-<tag>-portable-win-anycpu.zip`。
- release workflow 使用兼容 Node 24 的 checkout、artifact upload 和 release action，以避免 GitHub Actions 的 Node 20 弃用警告。
- 也可以在 GitHub Actions 页面手动运行 `Release` workflow。填写 `tag_name`；测试预发布版可用 `continuous`，正式 release 使用 semver 风格 tag。默认会 checkout 与 `tag_name` 相同的 ref，也可以填写 `build_ref` 来构建指定 branch、commit 或 tag。如果 `tag_name` 还没有推送，请填写 `build_ref`。
- 常规安装请使用 setup `.exe`。手动测试或免安装运行请使用便携 `.zip`；解压后启动 `GestureSign.ControlPanel.exe`。`GestureSign.exe` 是后台守护进程。
- 在第一次 `master` 推送、匹配 tag 推送或手动 workflow 成功完成前，本 fork 的 releases 页面可能没有可下载 asset。

## 捐赠与支持

本 fork 不发布捐赠或赞助链接。除非某个第三方收款账号由本仓库明确链接，否则不要假定它是官方账号。最有用的支持方式是测试、可复现 bug 报告、文档修复和聚焦 PR。

## 管理员窗口

Windows 会阻止普通进程向提权应用发送输入。如果在 Task Manager、Device Manager 或其他管理员窗口激活时手势不工作，请让 GestureSign 守护进程也以管理员权限运行。

- 推荐：打开 Options，启用 `Start GestureSign on Windows Startup`，再启用 `Run GestureSign As Administrator At Startup`。
- 一次性排查：右键 `GestureSign.ControlPanel.exe`，选择 `Run as administrator`；控制面板也会以提权方式启动守护进程。
- 不要使用可执行文件 Compatibility 标签页中的 `Run this program as an administrator`。GestureSign 会对此发出警告，因为它可能破坏启动和 touch-blocking 功能。
