# GestureSign

GestureSign is a gesture recognition software for Windows tablet. You can automate repetitive tasks by simply drawing a gesture with your fingers or mouse.

[中文说明](README.zh-CN.md)

[![Release](https://img.shields.io/github/release/CaFeZn/GestureSign.svg?style=flat-square)](https://github.com/CaFeZn/GestureSign/releases/latest)

## Feature

- Activate Window
- Window Control
- Virtual Desktop Switching
- Touch Keyboard Control
- Keyboard simulation
- Key Down/Up
- Mouse Simulation
- Send Keystrokes
- Open Default Browser
- Screen Brightness
- Volume Adjustment
- Run Command or Program
- Launch Windows Store App
- Send Message
- Toggle Window Topmost

## Install

- Installer edition: `winget install --id TransposonY.GestureSign -e`
- Update installer edition: `winget upgrade --id TransposonY.GestureSign -e`
- Portable edition: download the portable package from this fork's [releases page](https://github.com/CaFeZn/GestureSign/releases/latest), extract it to the folder you want, and run it from there.
- Portable builds write configuration and backups under the program folder's `AppData` directory. Installer builds write user data under `%APPDATA%\GestureSign`.

## Usage

This README is the current English getting-started guide when the old external documentation site is unavailable.

1. Start `GestureSign.ControlPanel.exe`. The control panel starts the background daemon (`GestureSign.exe`) and the tray icon.
2. Open the `Actions` tab. Add actions under `(Global Actions)` for every app, or add a specific application when an action should only run in matching windows.
3. Pick or create a gesture for the action. Use the `Gestures` tab to teach new gesture shapes before assigning them to actions.
4. Add one or more commands to the action. Commands run from top to bottom, so put window activation or delay commands before commands that depend on the foreground window.
5. Save the configuration. GestureSign writes user data under `%APPDATA%\GestureSign` unless you run a portable build.

Startup notes:

- `GestureSign.ControlPanel.exe` opens the settings window and starts the daemon when needed.
- `GestureSign.exe` starts only the background daemon and tray icon. Use it when you want GestureSign to start without opening the control panel.
- The normal startup option creates a shortcut to `GestureSign.exe`. The administrator startup option creates a startup task for `GestureSign.exe`.

Gesture names and defaults:

- The old external documentation site is no longer required for basic setup; the control panel is the supported guide for creating gestures, assigning actions, and changing options.
- Gesture names are user-editable labels. In the bundled defaults, a leading number usually means the number of fingers, for example `2Left` is a two-finger left swipe and `3Down` is a three-finger down swipe.
- Names without a leading number can still be multi-finger gestures. Open the `Gestures` tab to see the recorded sample and finger count for any gesture.
- Tap gestures are represented by points instead of long stroke paths. `3 Finger Double Tap` is a bundled example made from two three-finger tap samples.
- Default browser gestures include `Open Web Browser` on `ee`, browser back/forward gestures, and tab gestures under the browser application group. You can edit or delete these in the `Actions` tab.

Default gesture examples:

| Action | Gesture | Fingers | Scope |
| --- | --- | --- | --- |
| Open Web Browser | `ee` | 2 | Global actions |
| Show/Hide Touch Keyboard | `3 Finger Double Tap` | 3 | Global actions |
| Close Tab | `3Down` | 3 | Browsers |
| Next Tab | `3Left` | 3 | Browsers |

Gesture creation notes:

- Create or edit gestures from the `Gestures` tab or from the gesture picker in an action, then draw the gesture on the screen when the gesture definition window is open.
- Mouse gestures are disabled by default. To draw gestures with a mouse, open `Options`, turn on `Mouse Gesture`, and select one or more drawing buttons. The default when enabling this option is the right mouse button.
- When a mouse drawing button is configured, hold that button, move the mouse to draw the gesture, then release the button to finish the sample or run the gesture.
- Multi-finger gestures require simultaneous contacts. A three-finger gesture is not the same as drawing one line three times.
- Point and tap gestures are recorded as point samples, so their preview can look like dots instead of strokes.
- A one-finger touchscreen tap can be recorded as a gesture sample. Mouse and touchpad training still require movement to avoid capturing ordinary clicks as gestures.
- Double-clicking a gesture in the `Gestures` tab opens it for editing; use the edit window to redraw or rename the gesture.

Common command examples:

- `Hot Key` sends one shortcut such as `Ctrl+Alt+T`.
- For side-specific modifiers such as `Left Alt` or `Right Shift`, use the `Hot Key` extra-key dropdown instead of typing the modifier directly.
- `Send Keystrokes` types a key sequence or text.
- `Key Down/Up` can hold or release keys such as `Left Shift`; pair one hold action with another release action when you need to select text or keep a modifier down temporarily.
- `Launch Windows Store App`, `Open Default Browser`, `Open File`, and `Run Command or Program` start applications or files.
- `Mouse Actions` can send clicks, wheel actions, and other mouse input.
- `Add Current Application to Ignored List` adds the current foreground application to the ignored-app list, which is useful when a gesture conflicts with one app's native input.
- `Open GestureSign Control Panel` opens the settings window from a gesture.
- `Repeat Last Command` replays the last command that completed successfully.
- `Next Virtual Desktop` and `Previous Virtual Desktop` switch Windows virtual desktops.

Multiple gestures for the same behavior:

- An action has one assigned gesture, but you can bind several gestures to the same command sequence by duplicating the action.
- In the `Actions` tab, copy the existing action, use `Paste To New Action`, then edit the pasted action and choose a different gesture.
- Commands inside each action run from top to bottom, so keep the duplicated command order identical when both gestures should do exactly the same thing.

Gesture and trigger notes:

- Touchscreen, touchpad, pen, and mouse input are separate source devices. Check the action's ignored-device settings if a gesture works from one device but not another.
- Pen gestures require `Options` > `Pen Gesture`, one HID pen activation state (barrel/right-click button or inverted pen), and one draw mode (tip or hover). If a stylus driver exposes its button as a normal mouse right-click instead of HID pen input, try `Mouse Gesture` with the right mouse button.
- Continuous gestures run while fingers are still moving, but only after movement crosses `Options` > `Continuous Gesture Distance`. If a short swipe releases before that distance, the matching normal gesture can still run first.
- `Drawing Start Timeout` cancels only gestures that do not start drawing quickly enough. `Whole Gesture Timeout` cancels the full capture after the configured duration and is exclusive with drawing-start timeout. `Composite Gesture Timeout` only controls how long GestureSign waits for the next segment of a composite gesture.
- Precision touchpad gestures depend on the touchpad driver exposing raw HID touchpad input. If a touchpad or third-party driver is not detected, first disable conflicting Windows touchpad gestures and confirm the device appears as a precision touchpad, not only as mouse wheel or vendor-specific input.
- `Block Touch Input` is available only in UIAccess builds and is configured per matched application. It starts blocking after GestureSign has enough touch contacts to identify a gesture, so very early touch frames may still reach the target app.
- If the touch keyboard or a browser's native touch behavior breaks while touch blocking is enabled, lower that app's block threshold or disable blocking for that app first.
- Trigger conditions can use `finger_1_start_X`, `finger_1_start_Y`, `finger_1_end_X`, `finger_1_end_Y`, their percent variants such as `finger_1_start_X%`, and `finger_1_ID`.
- Edge gestures can be approximated with percent trigger conditions, for example `finger_1_start_X%<5`, `finger_1_start_X%>95`, `finger_1_start_Y%<5`, or `finger_1_start_Y%>95`.
- Window conditions can use `window_is_maximized`, `window_is_minimized`, and `window_is_fullscreen`.
- Modifier-key conditions can use `key_is_shift_down`, `key_is_ctrl_down`, `key_is_alt_down`, and `key_is_win_down`.
- Use SQL-style operators such as `AND`, `OR`, and parentheses for multiple trigger conditions, for example `finger_1_start_X<500 AND finger_1_end_X<450`.
- Conditions are evaluated with Windows virtual screen coordinates. Percent variants are relative to the full virtual desktop area, including monitors placed left or above the primary display, and are usually better when the same configuration must work across displays with different resolutions.
- GestureSign cannot send a standalone `Fn` key. On most keyboards, `Fn` is handled by firmware and is not exposed to Windows as a normal virtual key. Use the real key generated by the `Fn` combination instead, such as `F1`-`F24`, volume, brightness, or media keys when the keyboard driver exposes them.

## Troubleshooting

- If no gestures run, confirm the tray daemon is running and restart GestureSign from the control panel.
- If mouse drawing does not start, confirm `Options` > `Mouse Gesture` is on and at least one drawing button is selected.
- If gestures fail only in Task Manager, Device Manager, installers, or other administrator windows, see the administrator-window notes below.
- If a configured action does not run in one app, check whether that app is in the ignored list or whether the action is configured only for a different application.
- If GestureSign interferes with one app, bind `Add Current Application to Ignored List` to a gesture and run it while that app is active.
- If touchpad gestures are delayed or dropped, increase the drawing-start timeout in `Options`.
- Use `Options` > `Backup User Data` before testing large configuration changes.

## Upstream Issue Coverage

This fork has targeted fixes, implemented features, or documented workflows for these upstream `TransposonY/GestureSign` issues.

Implemented or covered:

| Issue | Coverage |
| --- | --- |
| [#139](https://github.com/TransposonY/GestureSign/issues/139) | Documented the duplicate-action workflow for binding several gestures to the same command sequence. |
| [#138](https://github.com/TransposonY/GestureSign/issues/138), [#95](https://github.com/TransposonY/GestureSign/issues/95), [#62](https://github.com/TransposonY/GestureSign/issues/62) | Single-finger touchscreen gestures and one-finger touchscreen tap training are supported. |
| [#137](https://github.com/TransposonY/GestureSign/issues/137) | Added optional sound playback for unrecognized gestures, including a custom `.wav` choice. |
| [#136](https://github.com/TransposonY/GestureSign/issues/136), [#90](https://github.com/TransposonY/GestureSign/issues/90) | Gesture training help now explains which input sources can draw and when mouse gestures must be enabled first. |
| [#134](https://github.com/TransposonY/GestureSign/issues/134), [#122](https://github.com/TransposonY/GestureSign/issues/122) | Continuous gesture distance is configurable, documented, and threshold-edge firing no longer drops the first trigger. |
| [#133](https://github.com/TransposonY/GestureSign/issues/133), [#111](https://github.com/TransposonY/GestureSign/issues/111) | Build scripts and docs cover Windows 11 on Arm64 and optional native Arm64 output. |
| [#132](https://github.com/TransposonY/GestureSign/issues/132), [#125](https://github.com/TransposonY/GestureSign/issues/125) | The `Fn` limitation is documented, and F1-F24/media/function keys are easier to choose from the Hot Key UI. |
| [#117](https://github.com/TransposonY/GestureSign/issues/117) | Installer installation and update commands are documented for the current winget package. |
| [#115](https://github.com/TransposonY/GestureSign/issues/115), [#40](https://github.com/TransposonY/GestureSign/issues/40) | Virtual desktop switching actions are available and can be assigned to normal or continuous gestures. |
| [#109](https://github.com/TransposonY/GestureSign/issues/109), [#131](https://github.com/TransposonY/GestureSign/issues/131) | Shell/taskbar activation and modifier-key cleanup around switch-window and switch-desktop actions have been hardened. |
| [#97](https://github.com/TransposonY/GestureSign/issues/97), [#31](https://github.com/TransposonY/GestureSign/issues/31) | Control Panel startup tolerates unavailable Windows Application Event Log access, and configuration writes are flushed, serialized, and stored under package local state for Desktop Bridge builds. |
| [#104](https://github.com/TransposonY/GestureSign/issues/104) | `Add Current Application to Ignored List` is available and documented. |
| [#93](https://github.com/TransposonY/GestureSign/issues/93), [#92](https://github.com/TransposonY/GestureSign/issues/92) | `Key Down/Up` supports held modifiers, and side-specific Hot Key modifiers are documented in the UI and README. |
| [#84](https://github.com/TransposonY/GestureSign/issues/84), [#65](https://github.com/TransposonY/GestureSign/issues/65), [#118](https://github.com/TransposonY/GestureSign/issues/118) | This README now replaces the unavailable external guide for setup, usage, troubleshooting, and build notes. |
| [#76](https://github.com/TransposonY/GestureSign/issues/76) | Composite gesture timeout is configurable. |
| [#30](https://github.com/TransposonY/GestureSign/issues/30) | Added an optional whole-gesture timeout that cancels a capture after the configured duration and is mutually exclusive with drawing-start timeout. |
| [#75](https://github.com/TransposonY/GestureSign/issues/75), [#32](https://github.com/TransposonY/GestureSign/issues/32) | Trigger conditions support touch-coordinate variables and multi-condition expressions. |
| [#74](https://github.com/TransposonY/GestureSign/issues/74) | Re-injected touch `UP` frames preserve `INRANGE` for the final contact. |
| [#73](https://github.com/TransposonY/GestureSign/issues/73), [#56](https://github.com/TransposonY/GestureSign/issues/56) | Touch Keyboard command behavior and TabTip path lookup have been hardened for Windows 10/11. |
| [#72](https://github.com/TransposonY/GestureSign/issues/72) | Touchscreen gestures can be disabled from `Options`. |
| [#70](https://github.com/TransposonY/GestureSign/issues/70) | Touchscreen monitor selection no longer relies only on cursor position. |
| [#57](https://github.com/TransposonY/GestureSign/issues/57), [#9](https://github.com/TransposonY/GestureSign/issues/9) | Touch-blocking behavior has been hardened and its UIAccess/per-app/initial-frame limits are documented. |
| [#51](https://github.com/TransposonY/GestureSign/issues/51) | Drawing-start timeout is honored for precision touchpad gestures. |
| [#49](https://github.com/TransposonY/GestureSign/issues/49) | Backup/settings restore accepts current backups and legacy action/gesture exports. |
| [#48](https://github.com/TransposonY/GestureSign/issues/48), [#27](https://github.com/TransposonY/GestureSign/issues/27) | Administrator-window, startup, silent daemon launch, and portable-mode guidance is documented. |
| [#44](https://github.com/TransposonY/GestureSign/issues/44), [#37](https://github.com/TransposonY/GestureSign/issues/37) | `Repeat Last Command` and `Open GestureSign Control Panel` actions are available. |
| [#33](https://github.com/TransposonY/GestureSign/issues/33) | Mouse gestures can use multiple configured drawing buttons, such as right and middle mouse buttons. |
| [#38](https://github.com/TransposonY/GestureSign/issues/38) | Control Panel touchpad scrolling uses fractional wheel-delta handling instead of treating every small delta as a full wheel tick. |
| [#19](https://github.com/TransposonY/GestureSign/issues/19), [#126](https://github.com/TransposonY/GestureSign/issues/126) | Hot Key and built-in window commands cover common accessibility shortcuts and hide-window workflows. |

Improved but not fully closed without hardware validation or larger feature design:

| Issue | Current status |
| --- | --- |
| [#128](https://github.com/TransposonY/GestureSign/issues/128), [#120](https://github.com/TransposonY/GestureSign/issues/120) | Win11 tablet/touchscreen reliability has several fixes, but device-specific validation is still required. |
| [#123](https://github.com/TransposonY/GestureSign/issues/123) | Rapid tap handling no longer drops a new active-contact frame when it replaces a stale contact set, but high-frequency touchscreen validation is still required. |
| [#127](https://github.com/TransposonY/GestureSign/issues/127), [#119](https://github.com/TransposonY/GestureSign/issues/119), [#112](https://github.com/TransposonY/GestureSign/issues/112), [#94](https://github.com/TransposonY/GestureSign/issues/94), [#55](https://github.com/TransposonY/GestureSign/issues/55) | Pen settings and docs are clearer, but Wacom/passive pen support still depends on whether the driver exposes HID pen/touchpad input. |
| [#135](https://github.com/TransposonY/GestureSign/issues/135), [#116](https://github.com/TransposonY/GestureSign/issues/116), [#114](https://github.com/TransposonY/GestureSign/issues/114), [#59](https://github.com/TransposonY/GestureSign/issues/59) | Precision touchpad handling is improved, but third-party/vendor driver support must be validated per device. |
| [#130](https://github.com/TransposonY/GestureSign/issues/130), [#87](https://github.com/TransposonY/GestureSign/issues/87), [#45](https://github.com/TransposonY/GestureSign/issues/45) | Hold-to-drag workflows need an explicit hold/release feature to avoid stuck mouse buttons. |
| [#66](https://github.com/TransposonY/GestureSign/issues/66), [#24](https://github.com/TransposonY/GestureSign/issues/24), [#52](https://github.com/TransposonY/GestureSign/issues/52) | Modifier-key conditions and held-key actions are partly supported, but arbitrary-key gesture conditions and keyboard-triggered mouse drawing are not implemented. |
| [#121](https://github.com/TransposonY/GestureSign/issues/121) | Portable builds can run from a chosen folder, but installer-directory selection is not implemented in this repository. |

## Build

- Open `GestureSign.sln` in Visual Studio 2022, or run `.\scripts\build.ps1`
- The solution now targets `.NET Framework 4.8`
- NuGet packages are restored with `packages.config`, so restore is required before the first build
- Windows 11 on Arm64 uses the `Any CPU` build; do not add an `ARM64` solution platform. For native Arm64 .NET Framework output, build on Windows 11 24H2 with VS 2022 17.11 or newer and pass `.\scripts\build.ps1 -Configuration Release -PreferNativeArm64`.

## GitHub Releases

- Push a semver-like tag such as `v8.1.0` or `v8.1.0-beta.1` to run the release workflow automatically.
- The workflow builds `Release|Any CPU`, packages `bin\Release`, creates or updates the GitHub Release, and uploads `GestureSign-<tag>-win-anycpu.zip`.
- You can also run the `Release` workflow manually from GitHub Actions. Provide `tag_name`; by default the workflow checks out the same ref as `tag_name`, or you can provide `build_ref` to build a specific branch, commit, or tag. If `tag_name` has not been pushed yet, provide `build_ref`.

## Administrator Windows

Windows blocks normal processes from sending input to elevated applications. If gestures do not work while Task Manager, Device Manager, or another administrator window is active, run the GestureSign daemon with administrator privileges too.

- Recommended: open Options, enable `Start GestureSign on Windows Startup`, then enable `Run GestureSign As Administrator At Startup`
- One-time troubleshooting: right-click `GestureSign.ControlPanel.exe` and choose `Run as administrator`; the control panel now starts the daemon elevated as well
- Do not use the executable file's Compatibility tab option `Run this program as an administrator`; GestureSign warns about this because it can break startup and touch-blocking features
