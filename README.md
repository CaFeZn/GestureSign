# GestureSign

GestureSign is a gesture recognition software for Windows tablet. You can automate repetitive tasks by simply drawing a gesture with your fingers or mouse.

[中文说明](README.zh-CN.md)

[![Release](https://img.shields.io/github/release/CaFeZn/GestureSign.svg?style=flat-square)](https://github.com/CaFeZn/GestureSign/releases)

## Project Status

This repository is a community fork of [TransposonY/GestureSign](https://github.com/TransposonY/GestureSign). It carries selected maintenance fixes, documentation updates, and build/release automation, but it is not the upstream maintainer's official continuation and does not promise a full roadmap.

The current codebase remains a Windows desktop app targeting `.NET Framework 4.8`. The documented build path uses Visual Studio 2022 and the Windows 11 notes in this README describe known behavior and limitations, but this fork has not migrated the app to .NET 6/.NET 8.

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

- Upstream installer package: `winget install --id TransposonY.GestureSign -e`
- Update upstream installer package: `winget upgrade --id TransposonY.GestureSign -e`
- Fork installer: download `GestureSign-<tag>-setup-win-anycpu.exe` from the [releases page](https://github.com/CaFeZn/GestureSign/releases) and run it. The installer shows a destination-folder page; silent installs can pass `/DIR="D:\Tools\GestureSign"`.
- Fork portable edition: download `GestureSign-<tag>-portable-win-anycpu.zip`, extract it to the folder you want, and run `GestureSign.ControlPanel.exe`.
- Latest manual-test build: open the rolling `continuous` prerelease on the releases page and download `GestureSign-continuous-setup-win-anycpu.exe` for a normal installer or `GestureSign-continuous-portable-win-anycpu.zip` for a portable run.
- If this fork has no published release asset yet, use the upstream installer package above or build locally with `.\scripts\build.ps1 -Configuration Portable`.
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
- For startup without opening the settings window, start `GestureSign.exe`, not `GestureSign.ControlPanel.exe`; the normal startup option targets the daemon for this reason.

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
- Mouse drawing buttons are trigger buttons, not output actions. `Mouse Actions` can send wheel input after a gesture matches. Wheel rotation by itself does not draw a mouse gesture, but it can be used as a conditioned standalone mouse trigger as described below.
- Multi-finger gestures require simultaneous contacts. A three-finger gesture is not the same as drawing one line three times.
- Point and tap gestures are recorded as point samples, so their preview can look like dots instead of strokes.
- A one-finger touchscreen tap can be recorded as a gesture sample. Mouse and touchpad training still require movement to avoid capturing ordinary clicks as gestures.
- Double-clicking a gesture in the `Gestures` tab opens it for editing; use the edit window to redraw or rename the gesture.
- Gesture trail color is configured in `Options` > `Gesture Trail`. Use `Pick Color` for a fixed trail color, or `Follow System Color` to use the current Windows DWM theme color.

Common command examples:

- `Hot Key` sends one shortcut such as `Ctrl+Alt+T`.
- For side-specific modifiers such as `Left Alt` or `Right Shift`, use the `Hot Key` extra-key dropdown instead of typing the modifier directly.
- `Send Keystrokes` types a key sequence or text.
- `Key Down/Up` can hold or release keys such as `Left Shift`; pair one hold action with another release action when you need to select text or keep a modifier down temporarily.
- `Launch Windows Store App`, `Open Default Browser`, `Open File`, and `Run Command or Program` start applications or files.
- `Search or Open Clipboard Text` opens URL-like clipboard text directly, or searches other clipboard text in the default browser. To use it with selected text, chain `Hot Key` (`Ctrl+C`), a short `Delay`, then this command.
- `Mouse Actions` can send clicks, wheel actions, and other mouse input.
- `Mouse Actions` > `Hold Down` can be paired with a later `Release`. When a hold-down command runs from a continuous gesture while capture is still active, GestureSign now releases the held mouse button automatically when that capture ends or is canceled, reducing stuck-button risk.
- `Add Current Application to Ignored List` adds the current foreground application to the ignored-app list, which is useful when a gesture conflicts with one app's native input.
- `Show Notification` displays a short Windows tray notification with configurable title, message, and duration.
- `Open GestureSign Control Panel` opens the settings window from a gesture.
- `Repeat Last Command` replays the last command that completed successfully.
- `Next Virtual Desktop` and `Previous Virtual Desktop` switch Windows virtual desktops.

Multiple gestures for the same behavior:

- An action has one assigned gesture, but you can bind several gestures to the same command sequence by duplicating the action.
- In the `Actions` tab, copy the existing action, use `Paste To New Action`, then edit the pasted action and choose a different gesture.
- Commands inside each action run from top to bottom, so keep the duplicated command order identical when both gestures should do exactly the same thing.
- To disable one global gesture only in a specific app, create or copy an action with the same gesture under that app and turn all commands in that app-specific action off. The app-specific action blocks fallback to the global action, while other global gestures still work in that app.
- If an application uses `Match Activated Window`, capture-start rules now follow that activated window from the beginning of capture as well, not only at final gesture recognition. This keeps whitelist checks, finger-count limits, block-touch thresholds, and guarded one-finger touchpad capture aligned with the app that will actually receive the gesture.
- The same activated-window alignment now carries through command execution as well, so window-targeting plugins and `%GS_WindowHandle%` use the same non-shell foreground target instead of drifting back to a point-based window lookup mid-action.

Continuous gestures:

- `Continuous Gesture` is an action setting, not a GitHub release tag. It means GestureSign runs the action repeatedly while the current finger, pen, touchpad, or mouse capture is still moving.
- Implementation summary: `PointCapture` collects raw input points and raises `PointCaptured` on accepted movement. `ContinuousGestureTrigger` listens during active capture, compares the latest points with the previous points, chooses `Up`, `Down`, `Left`, or `Right`, checks the DPI-scaled `Options` > `Continuous Gesture Distance`, and fires matching continuous actions immediately. If a continuous action fired, the normal finger-up gesture match for that same capture is canceled to avoid duplicate commands.
- To configure one, edit or create an action, turn on `Continuous Gesture`, set the contact count and direction, choose commands, then tune `Options` > `Continuous Gesture Distance`. Smaller distances trigger more often; larger distances trigger less often.
- Continuous commands are good for repeated behaviors such as scrolling, volume, brightness, virtual-desktop switching, or repeated hotkeys. Avoid commands that should run exactly once unless you intentionally want repeats.
- `Mouse Actions` > `Hold Down` is automatically released when the continuous capture ends or is canceled, reducing stuck-button risk for continuous drag-like workflows.

One-finger touchpad edge workflows:

- One-finger precision-touchpad capture is protected because ordinary one-finger touchpad movement controls the pointer. GestureSign captures a one-finger touchpad action only when the action allows `TouchPad`, uses one contact, has at least one enabled command, and has a trigger condition that is already true at touch start. Capture gating checks the current application's matching one-finger touchpad actions first, then falls back to global one-finger touchpad actions only when normal global fallback would apply, so an unrelated app-specific one-finger action does not block a global edge gesture from starting.
- For a right-edge scrollbar-like area, create two actions: one `Continuous Gesture` with one finger `Up`, and one with one finger `Down`. Enable `TouchPad` for both actions and add `Mouse Actions` > `Vertical Scroll` commands with opposite scroll directions.
- Use a start-zone condition so normal touchpad movement is not captured. Example middle strip on the right edge: `finger_1_start_X%>=95 AND finger_1_start_Y%>=10 AND finger_1_start_Y%<=90`.
- If you want a modifier guard, add it to the same condition, for example `key_is_alt_down AND finger_1_start_X%>=95 AND finger_1_start_Y%>=10 AND finger_1_start_Y%<=90`.
- The scroll is dynamic: movement events trigger while the finger slides. You do not need to lift the finger before scrolling begins.
- `start_*` variables decide whether one-finger touchpad capture may begin. `end_*` variables update during execution, but they cannot be the only gate for starting a protected one-finger touchpad capture.
- Percent coordinates use Windows virtual-screen bounds, not the touchpad's physical hardware percentage. On multi-monitor or unusual scaling setups, adjust the edge percentage after real-device testing.

Standalone wheel triggers:

- `Wheel Forward` and `Wheel Backward` can be selected in an action's `Mouse HotKey` field.
- Standalone wheel triggers must have a non-empty trigger condition and at least one enabled command. This keeps ordinary scrolling from being captured globally.
- Standalone wheel triggers also respect `Match Activated Window`, so app-specific conditioned wheel actions follow the same activated-window matching path as gesture capture.
- Use them for deliberate edge or corner workflows, for example only when the cursor or gesture condition is in a configured zone. Do not configure unconditioned global wheel actions.

Default browser matching:

- New bundled defaults match common browser executable names exactly: `msedge`, `chrome`, `firefox`, `iexplore`, and legacy `MicrosoftEdge`, with or without `.exe`.
- Existing user configurations are not auto-migrated. If an old browser group fails to match Chromium Edge, edit that application rule and use an executable/process match such as `^(MicrosoftEdge|firefox|chrome|iexplore|msedge)(\.exe)?$`.

Gesture and trigger notes:

- Touchscreen, touchpad, pen, and mouse input are separate source devices. Check the action's ignored-device settings if a gesture works from one device but not another.
- A single action can allow both touchscreen and touchpad input by leaving both source devices enabled. If the same gesture should run different commands per device, duplicate the action and disable the unwanted source on each copy.
- Pen gestures require `Options` > `Pen Gesture`, one HID pen activation state (barrel/right-click button or inverted pen), and one draw mode (tip or hover). If a stylus driver exposes its button as a normal mouse right-click instead of HID pen input, try `Mouse Gesture` with the right mouse button.
- Continuous gestures run while fingers are still moving, but only after movement crosses `Options` > `Continuous Gesture Distance`. That distance is scaled by the DPI of the screen where the gesture is captured. If a short swipe releases before that distance, the matching normal gesture can still run first.
- One-finger precision-touchpad gestures are guarded by default. GestureSign only captures a one-finger touchpad action when the action allows `TouchPad`, uses one contact, has at least one enabled command, and has a trigger condition that is already true at touch start. This avoids hijacking normal pointer movement.
- For a right-edge touchpad scrollbar, create separate one-finger continuous `Up` and `Down` actions, enable `TouchPad`, add `Mouse Actions` > `Vertical Scroll` commands, and use a start-zone condition such as `key_is_alt_down AND finger_1_start_X%>=95 AND finger_1_start_Y%>=10 AND finger_1_start_Y%<=90`. The scroll command repeats dynamically while the finger is moving, not only after finger-up.
- `Drawing Start Timeout` cancels only gestures that do not start drawing quickly enough. `Whole Gesture Timeout` cancels the full capture after the configured duration and is exclusive with drawing-start timeout. `Composite Gesture Timeout` only controls how long GestureSign waits for the next segment of a composite gesture.
- Precision touchpad gestures depend on the touchpad driver exposing raw HID touchpad input. If a touchpad or third-party driver is not detected, first disable conflicting Windows touchpad gestures and confirm the device appears as a precision touchpad, not only as mouse wheel or vendor-specific input.
- Standard HID precision-touchpad drivers are no longer filtered only because their raw device path contains `ROOT` or `VIRTUAL_DIGITIZER`. If a third-party driver still does not work, confirm it really exposes `TouchPadUsage`, not only mouse, pen, or vendor-specific HID reports.
- Different raw HID touchpad devices are now tracked by device handle as well as source type, so one touchpad's active packet stream is less likely to be mistaken for another touchpad of the same `TouchPadUsage` class.
- Virtual-display or remote-touch tools such as spacedesk must expose compatible Windows digitizer/touchscreen input. If Windows built-in touch gestures do not work through the virtual display, GestureSign usually cannot capture that input either.
- `Block Touch Input` is available only in UIAccess builds and is configured per matched application. It starts blocking after GestureSign has enough touch contacts to identify a gesture, so very early touch frames may still reach the target app. It redirects touch pointer frames only; it is not a complete system-event blocker for precision-touchpad, pen, mouse, keyboard, or app-specific native input.
- If the touch keyboard or a browser's native touch behavior breaks while touch blocking is enabled, lower that app's block threshold or disable blocking for that app first.
- Trigger conditions can use `finger_1_start_X`, `finger_1_start_Y`, `finger_1_end_X`, `finger_1_end_Y`, their percent variants such as `finger_1_start_X%`, and `finger_1_ID`.
- Edge gestures can be approximated with percent trigger conditions, for example `finger_1_start_X%<5`, `finger_1_start_X%>95`, `finger_1_start_Y%<5`, or `finger_1_start_Y%>95`.
- Trigger conditions can route the same gesture to different commands by finger position, window state, or held modifier keys. Zone workflows can be approximated with coordinate ranges such as `finger_1_start_X%>=25 AND finger_1_start_X%<50`, but this is not a separate Android-style edge gesture or floating-ball feature.
- For one-finger touchpad protection, the capture gate is evaluated at touch start. Use `start_*`, modifier-key, and window-state variables for the edge guard; `end_*` variables still reflect the latest captured point during action execution, but cannot be the only reason to start capturing a one-finger touchpad stroke.
- True BetterTouchTool-style tip-tap is not implemented. A one-shot approximation can use trigger conditions such as `finger_1_ID<finger_2_ID` or `finger_1_ID>finger_2_ID`, but both contacts must be captured together and some touchpad drivers do not expose stable contact IDs. Separate tap-vs-tip-tap recognition and left/right tip-tap require recognizer/model work.
- Window conditions can use `window_is_maximized`, `window_is_minimized`, and `window_is_fullscreen`.
- Modifier-key conditions can use `key_is_shift_down`, `key_is_ctrl_down`, `key_is_alt_down`, and `key_is_win_down`. Arbitrary virtual-key conditions can use `key_<key-name>_down`, for example `key_space_down`, `key_a_down`, or `key_page_up_down`; the condition editor can insert these by focusing the key box and pressing the desired key.
- Use SQL-style operators such as `AND`, `OR`, and parentheses for multiple trigger conditions, for example `finger_1_start_X<500 AND finger_1_end_X<450`.
- Conditions are evaluated with Windows virtual screen coordinates. Percent variants use Win32 virtual-screen pixel bounds, matching captured touch coordinates across mixed-DPI displays and monitors placed left or above the primary display.
- GestureSign cannot send a standalone `Fn` key. On most keyboards, `Fn` is handled by firmware and is not exposed to Windows as a normal virtual key. Use the real key generated by the `Fn` combination instead, such as `F1`-`F24`, volume, brightness, or media keys when the keyboard driver exposes them.

## Troubleshooting

Windows 11 touch/gesture conflicts:

- Windows 11, browsers, and some apps can still handle native touch or precision-touchpad gestures before or alongside GestureSign. Disabling Windows three- and four-finger gestures may not stop every app-specific zoom, scroll, or two-finger behavior.
- Prefer gesture shapes and finger counts that do not conflict with the target app's native input, or add app-specific actions/ignored applications as described above. `Block Touch Input` can help only in UIAccess builds and has the initial-frame limits documented in the gesture notes.

General troubleshooting:

- If no gestures run, confirm the tray daemon is running and restart GestureSign from the control panel.
- If gestures work only after manually opening GestureSign, enable `Options` > `Start GestureSign on Windows Startup` or add a startup shortcut/task for `GestureSign.exe`, not the control panel executable.
- If mouse drawing does not start, confirm `Options` > `Mouse Gesture` is on and at least one drawing button is selected.
- If browser actions do not match Chromium Edge, add or edit an application rule using executable filename or process matching for `msedge.exe`. The bundled browser group matches exact browser executable names including `msedge`, `chrome`, `firefox`, `iexplore`, and legacy `MicrosoftEdge`, with or without `.exe`. Detached Firefox tab windows should still match by executable, but failures need per-window validation because browser window classes and tab-tearoff behavior vary by version.
- If Chrome ignores mouse actions while a pen is hovering or its side button is pressed, test whether the pen driver exposes the input as HID pen state or as normal mouse input. Use `Pen Gesture` for HID pen input and `Mouse Gesture` for drivers that emit mouse right-clicks.
- If GestureSign exits or stops responding after an external monitor is disconnected, restart the tray daemon and check the log. Display-change handling now releases active input, clears cached touch-screen mappings, re-registers raw input on the UI message context, and treats temporarily unavailable monitor bounds as a skipped input frame instead of a fatal error.
- If battery use is high, disable unused input sources and avoid low-threshold one-finger gestures during long tablet sessions. GestureSign listens for raw digitizer input while the daemon is running, so device-specific power impact must be profiled on the affected hardware.
- If gestures fail only in Task Manager, Device Manager, installers, or other administrator windows, see the administrator-window notes below.
- If a configured action does not run in one app, check whether that app is in the ignored list or whether the action is configured only for a different application.
- If GestureSign interferes with one app, bind `Add Current Application to Ignored List` to a gesture and run it while that app is active.
- To avoid interference by default, enable `Options` > `Whitelist Mode`. In this mode GestureSign captures gestures and hotkeys only when the target foreground/capture window matches a configured application; unmatched apps are ignored, and matched apps may still fall back to global actions.
- If touchpad gestures are delayed or dropped, increase the drawing-start timeout in `Options`.
- If touchscreen input temporarily stops touchpad gestures until the old source goes idle, update to a build that includes the raw-input source-staleness fix. Touchscreen/touchpad source ownership is now refreshed by completed output frames instead of every partial raw packet, so a noisy or incomplete touchscreen packet stream is less likely to keep the touchpad blocked after touchscreen input stops.
- Mouse wheel rotation can be used as a standalone mouse trigger by selecting `Wheel Forward` or `Wheel Backward` in an action's mouse hotkey field. Standalone wheel triggers require a trigger condition, for example a corner or edge condition, so ordinary scrolling is not intercepted globally.
- Single-finger touchpad gestures are ignored unless the action has a trigger condition that already matches at touch start. For a right-edge middle strip, use a condition such as `finger_1_start_X%>=95 AND finger_1_start_Y%>=10 AND finger_1_start_Y%<=90`; combine it with `Continuous Gesture` one-finger `Up`/`Down` and a mouse vertical-scroll command for scrollbar-like fast paging while the finger moves. If one application has its own one-finger touchpad action, unrelated app-specific conditions no longer prevent a global guarded one-finger action from starting when normal global fallback should still apply.
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
| [#134](https://github.com/TransposonY/GestureSign/issues/134), [#122](https://github.com/TransposonY/GestureSign/issues/122) | Continuous gesture distance is configurable, documented, threshold-edge firing no longer drops the first trigger, and distance thresholds scale with the gesture screen DPI. |
| [#133](https://github.com/TransposonY/GestureSign/issues/133), [#111](https://github.com/TransposonY/GestureSign/issues/111) | Build scripts and docs cover Windows 11 on Arm64 and optional native Arm64 output. |
| [#132](https://github.com/TransposonY/GestureSign/issues/132), [#125](https://github.com/TransposonY/GestureSign/issues/125) | The `Fn` limitation is documented, and F1-F24/media/function keys are easier to choose from the Hot Key UI. |
| [#117](https://github.com/TransposonY/GestureSign/issues/117) | Installer installation and update commands are documented for the current winget package. |
| [#121](https://github.com/TransposonY/GestureSign/issues/121) | The setup `.exe` now exposes the destination-folder page, and silent installs can use Inno Setup's `/DIR=` override. |
| [#115](https://github.com/TransposonY/GestureSign/issues/115), [#40](https://github.com/TransposonY/GestureSign/issues/40) | Virtual desktop switching actions are available and can be assigned to normal or continuous gestures. |
| [#113](https://github.com/TransposonY/GestureSign/issues/113) | App-specific actions with the same gesture now block fallback to global actions even when their commands are disabled, so one global gesture can be excluded in one app without disabling other global gestures there. `Match Activated Window` app rules are also applied consistently from capture start, through command execution, and for standalone conditioned wheel triggers, keeping per-app limits, `%GS_WindowHandle%`, and guarded touchpad capture aligned with the final matched app. |
| [#109](https://github.com/TransposonY/GestureSign/issues/109), [#131](https://github.com/TransposonY/GestureSign/issues/131) | Shell/taskbar activation and modifier-key cleanup around switch-window and switch-desktop actions have been hardened. |
| [#97](https://github.com/TransposonY/GestureSign/issues/97), [#31](https://github.com/TransposonY/GestureSign/issues/31) | Control Panel startup tolerates unavailable Windows Application Event Log access, and configuration writes are flushed, serialized, and stored under package local state for Desktop Bridge builds. |
| [#104](https://github.com/TransposonY/GestureSign/issues/104) | `Add Current Application to Ignored List` is available and documented. |
| [#93](https://github.com/TransposonY/GestureSign/issues/93), [#92](https://github.com/TransposonY/GestureSign/issues/92) | `Key Down/Up` supports held modifiers, and side-specific Hot Key modifiers are documented in the UI and README. |
| [#84](https://github.com/TransposonY/GestureSign/issues/84), [#65](https://github.com/TransposonY/GestureSign/issues/65), [#118](https://github.com/TransposonY/GestureSign/issues/118) | This README now replaces the unavailable external guide for setup, usage, troubleshooting, and build notes; Control Panel help buttons link here. |
| [#76](https://github.com/TransposonY/GestureSign/issues/76) | Composite gesture timeout is configurable. |
| [#30](https://github.com/TransposonY/GestureSign/issues/30) | Added an optional whole-gesture timeout that cancels a capture after the configured duration and is mutually exclusive with drawing-start timeout. |
| [#75](https://github.com/TransposonY/GestureSign/issues/75), [#32](https://github.com/TransposonY/GestureSign/issues/32) | Trigger conditions support touch-coordinate variables, multi-condition expressions, and percent coordinates aligned with Win32 virtual-screen pixel bounds. |
| [#66](https://github.com/TransposonY/GestureSign/issues/66), [#24](https://github.com/TransposonY/GestureSign/issues/24), [#52](https://github.com/TransposonY/GestureSign/issues/52) | Trigger conditions can test arbitrary held virtual keys such as `key_space_down`, `key_a_down`, and `key_page_up_down`, and the condition editor can capture a key name for insertion. |
| [#74](https://github.com/TransposonY/GestureSign/issues/74) | Re-injected touch `UP` frames preserve `INRANGE` for the final contact. |
| [#73](https://github.com/TransposonY/GestureSign/issues/73), [#56](https://github.com/TransposonY/GestureSign/issues/56) | Touch Keyboard command behavior and TabTip path lookup have been hardened for Windows 10/11. |
| [#72](https://github.com/TransposonY/GestureSign/issues/72) | Touchscreen gestures can be disabled from `Options`. |
| [#70](https://github.com/TransposonY/GestureSign/issues/70) | Touchscreen monitor selection no longer relies only on cursor position. |
| [#57](https://github.com/TransposonY/GestureSign/issues/57), [#9](https://github.com/TransposonY/GestureSign/issues/9) | Touch-blocking behavior has been hardened, pointer capture-loss/canceled frames release internal injected-touch IDs, and UIAccess/per-app/initial-frame limits are documented. |
| [#51](https://github.com/TransposonY/GestureSign/issues/51) | Drawing-start timeout is honored for precision touchpad gestures. |
| [#83](https://github.com/TransposonY/GestureSign/issues/83), [#89](https://github.com/TransposonY/GestureSign/issues/89), [#69](https://github.com/TransposonY/GestureSign/issues/69) | Conditioned one-finger precision-touchpad capture supports guarded edge/zone workflows, including right-edge continuous scrolling, without intercepting ordinary one-finger touchpad movement. |
| [#50](https://github.com/TransposonY/GestureSign/issues/50) | Gesture trail color can be fixed with `Pick Color` or reset to follow the Windows DWM theme color with `Follow System Color`. |
| [#49](https://github.com/TransposonY/GestureSign/issues/49) | Backup/settings restore accepts current backups and legacy action/gesture exports. |
| [#46](https://github.com/TransposonY/GestureSign/issues/46) | Added optional whitelist mode so unmatched applications are ignored by default; configured user applications still work and can fall back to global actions. |
| [#48](https://github.com/TransposonY/GestureSign/issues/48), [#27](https://github.com/TransposonY/GestureSign/issues/27) | Administrator-window, startup, silent daemon launch, and portable-mode guidance is documented. |
| [#60](https://github.com/TransposonY/GestureSign/issues/60) | Startup guidance now clarifies that unattended startup should target `GestureSign.exe`, while `GestureSign.ControlPanel.exe` is the settings UI. |
| [#44](https://github.com/TransposonY/GestureSign/issues/44), [#37](https://github.com/TransposonY/GestureSign/issues/37) | `Repeat Last Command` and `Open GestureSign Control Panel` actions are available. |
| [#110](https://github.com/TransposonY/GestureSign/issues/110) | Added a `Show Notification` action for short task-complete popups using the daemon tray icon. |
| [#43](https://github.com/TransposonY/GestureSign/issues/43) | Added `Search or Open Clipboard Text` for browser search/open-URL workflows after selected text is copied to the clipboard. |
| [#33](https://github.com/TransposonY/GestureSign/issues/33) | Mouse gestures can use multiple configured drawing buttons, such as right and middle mouse buttons. |
| [#38](https://github.com/TransposonY/GestureSign/issues/38) | Control Panel touchpad scrolling uses fractional wheel-delta handling instead of treating every small delta as a full wheel tick. |
| [#41](https://github.com/TransposonY/GestureSign/issues/41) | Mouse wheel rotation can be used as a standalone conditioned mouse trigger, so edge/corner wheel workflows do not need a held drawing button. |
| [#19](https://github.com/TransposonY/GestureSign/issues/19), [#126](https://github.com/TransposonY/GestureSign/issues/126) | Hot Key and built-in window commands cover common accessibility shortcuts and hide-window workflows. |

Improved but not fully closed without hardware validation or larger feature design:

| Issue | Current status |
| --- | --- |
| [#129](https://github.com/TransposonY/GestureSign/issues/129), [#124](https://github.com/TransposonY/GestureSign/issues/124), [#54](https://github.com/TransposonY/GestureSign/issues/54) | Project status is documented for this fork: it is a community fork with selected maintenance fixes, docs, and build/release automation, not an official upstream continuation or promised roadmap. |
| [#107](https://github.com/TransposonY/GestureSign/issues/107), [#106](https://github.com/TransposonY/GestureSign/issues/106) | Current tech status is documented: this fork targets `.NET Framework 4.8`, builds with Visual Studio 2022, and has optional native Arm64 .NET Framework output on Windows 11 24H2/VS 2022 17.11+; it has not migrated to .NET 6/.NET 8. |
| [#105](https://github.com/TransposonY/GestureSign/issues/105) | Windows 11 native gesture and app touch conflicts are documented as limitations; mitigation is configuration/app-specific behavior, not a complete OS-level override. |
| [#67](https://github.com/TransposonY/GestureSign/issues/67) | Release executable expectations are documented and the workflow builds the true `Portable` configuration, validates the control panel and daemon entrypoints, and uploads a zip once a release is created. |
| [#82](https://github.com/TransposonY/GestureSign/issues/82), [#54](https://github.com/TransposonY/GestureSign/issues/54) | Donation/support status is documented: this fork has no donation or sponsorship link; support is testing, reports, docs, or focused PRs. |
| [#77](https://github.com/TransposonY/GestureSign/issues/77), [#78](https://github.com/TransposonY/GestureSign/issues/78), [#80](https://github.com/TransposonY/GestureSign/issues/80), [#100](https://github.com/TransposonY/GestureSign/issues/100) | Gesture capture is limited by Windows integrity levels, shell/secure UI, and apps that capture input before GestureSign. Administrator-window guidance covers Task Manager; Timeline/Win+Tab and Parsec still require OS/app-specific validation. |
| [#79](https://github.com/TransposonY/GestureSign/issues/79), [#81](https://github.com/TransposonY/GestureSign/issues/81) | Windows Store/UWP launch and matching paths exist, including `Launch Windows Store App` and `ApplicationFrameWindow` unwrapping to `Windows.UI.Core.CoreWindow`, but Microsoft app failures still need per-app/package-state validation. |
| [#85](https://github.com/TransposonY/GestureSign/issues/85) | Touchpad delay, duplicate, and jagged-path reports are treated as driver/device-specific. Touchpad/raw-input handling has improved, but affected Synaptics or vendor-driver hardware is still required for closure. |
| [#103](https://github.com/TransposonY/GestureSign/issues/103) | Display-change handling now releases active raw input, clears touchscreen screen caches, re-registers raw input on the UI message context, resets the gesture-trail surface, and skips input frames when monitor bounds are temporarily unavailable; external-monitor hot-unplug still needs validation on the reported laptop/external-display topology. |
| [#128](https://github.com/TransposonY/GestureSign/issues/128), [#120](https://github.com/TransposonY/GestureSign/issues/120) | Win11 tablet/touchscreen parsing is more tolerant of HID reports without standard contact-count or coordinate-range data, initializes raw-device info buffers per Win32 requirements, handles unordered tip usages, improves raw-input diagnostics, and makes source-device stale-timeout recovery depend on completed output frames so a stuck touchscreen packet stream is less likely to keep later touchpad input blocked; device-specific validation is still required. |
| [#123](https://github.com/TransposonY/GestureSign/issues/123) | Rapid tap handling no longer drops a new active-contact frame when it replaces a stale contact set, and now splits combined release/next-press raw frames so the previous tap can finish before the next tap starts; high-frequency touchscreen validation is still required. |
| [#127](https://github.com/TransposonY/GestureSign/issues/127), [#119](https://github.com/TransposonY/GestureSign/issues/119), [#112](https://github.com/TransposonY/GestureSign/issues/112), [#94](https://github.com/TransposonY/GestureSign/issues/94), [#55](https://github.com/TransposonY/GestureSign/issues/55) | Pen settings and docs are clearer, but Wacom/passive pen support still depends on whether the driver exposes HID pen/touchpad input. |
| [#135](https://github.com/TransposonY/GestureSign/issues/135), [#116](https://github.com/TransposonY/GestureSign/issues/116), [#114](https://github.com/TransposonY/GestureSign/issues/114), [#59](https://github.com/TransposonY/GestureSign/issues/59) | Precision touchpad handling is improved, including safer one-finger capture gating that checks app-specific conditioned touchpad actions before allowed global fallback, standard `TouchPadUsage` devices are no longer rejected only because their raw HID device path contains `ROOT` or `VIRTUAL_DIGITIZER`, and same-class touchpad sources are tracked by device handle so multiple touchpads are less likely to interfere with each other; third-party/vendor driver support still needs per-device validation. |
| [#130](https://github.com/TransposonY/GestureSign/issues/130), [#87](https://github.com/TransposonY/GestureSign/issues/87), [#45](https://github.com/TransposonY/GestureSign/issues/45) | Mouse hold-down actions triggered during continuous capture are now auto-released on capture end/cancel to reduce stuck-button risk, but full OS-level drag-window/file-drag workflows still need hardware/UI validation. |
| [#108](https://github.com/TransposonY/GestureSign/issues/108) | spacedesk and similar virtual-display touch paths depend on whether Windows exposes compatible raw HID digitizer/touchscreen input; this needs spacedesk-device validation. |
| [#64](https://github.com/TransposonY/GestureSign/issues/64), [#63](https://github.com/TransposonY/GestureSign/issues/63), [#47](https://github.com/TransposonY/GestureSign/issues/47), [#26](https://github.com/TransposonY/GestureSign/issues/26) | Tip-tap can only be approximated with contact-ID conditions; separate tap-vs-tip-tap, left/right tip-tap, and rotation-insensitive five-finger pinch need recognizer/model work rather than a small configuration change. |
| [#61](https://github.com/TransposonY/GestureSign/issues/61) | Native/system event suppression is limited to UIAccess touch-pointer redirection after enough contacts are captured; GestureSign cannot fully block precision-touchpad, pen, mouse, keyboard, or all app-native events. |
| [#58](https://github.com/TransposonY/GestureSign/issues/58) | Battery-drain reports need profiling on affected touchscreen/tablet hardware; users can reduce risk by disabling unused sources and avoiding low-threshold one-finger capture during long sessions. |
| [#34](https://github.com/TransposonY/GestureSign/issues/34) | Chrome plus hovering pen/side-button behavior depends on whether the driver exposes HID pen state or mouse input, and needs validation with the affected browser and stylus driver. |
| [#28](https://github.com/TransposonY/GestureSign/issues/28) | Firefox detached-tab windows should match by exact browser executable/default browser rules, but remaining failures require per-window validation on the affected Firefox version. |
| [#91](https://github.com/TransposonY/GestureSign/issues/91) | Actions can be scoped to multiple source devices, including touchscreen and touchpad, but compatible raw HID touchpad input is still driver-dependent. |
| [#53](https://github.com/TransposonY/GestureSign/issues/53) | Browser matching guidance now calls out Chromium Edge `msedge.exe`; new default browser rules use exact executable matching, but existing user configs and Edge-specific failures still need per-rule and per-window validation. |

## Build

- Open `GestureSign.sln` in Visual Studio 2022, or run `.\scripts\build.ps1`
- The solution now targets `.NET Framework 4.8`
- NuGet packages are restored with `packages.config`, so restore is required before the first build
- This fork intentionally keeps the production build on .NET Framework 4.8 for compatibility with the existing WPF/Win32/HID code. .NET 6 migration is not treated as complete here.
- Windows 11 on Arm64 uses the `Any CPU` build; do not add an `ARM64` solution platform. For native Arm64 .NET Framework output, build on Windows 11 24H2 with VS 2022 17.11 or newer and pass `.\scripts\build.ps1 -Configuration Release -PreferNativeArm64`.

## GitHub Releases

- Push a semver-like tag such as `v8.1.0` or `v8.1.0-beta.1` to run the release workflow automatically.
- Every push to `master` also updates the `continuous` prerelease with the latest portable build for manual testing.
- The GitHub release named `continuous` is a rolling prerelease build tag. It is separate from the app feature named `Continuous Gesture`.
- The workflow builds `Release|Any CPU` for the installer and `Portable|Any CPU` for the zip, creates or updates the GitHub Release, and uploads both `GestureSign-<tag>-setup-win-anycpu.exe` and `GestureSign-<tag>-portable-win-anycpu.zip`.
- The release workflow uses Node 24-compatible checkout, artifact upload, and release actions to avoid GitHub Actions Node 20 deprecation warnings.
- You can also run the `Release` workflow manually from GitHub Actions. Provide `tag_name`; use `continuous` for a prerelease test build, or a semver-like tag for a formal release. By default the workflow checks out the same ref as `tag_name`, or you can provide `build_ref` to build a specific branch, commit, or tag. If `tag_name` has not been pushed yet, provide `build_ref`.
- Use the setup `.exe` for normal installation. Use the portable `.zip` for manual testing or a no-install run; after extracting it, start `GestureSign.ControlPanel.exe`. `GestureSign.exe` is the background daemon.
- The releases page may be empty until the first `master` push, matching tag push, or manual workflow run completes successfully.

## Donations and Support

This fork does not publish a donation or sponsorship link. Please do not assume any third-party payment account is official unless it is linked from this repository. The most useful support is testing, reproducible bug reports, documentation fixes, and focused pull requests.

## Administrator Windows

Windows blocks normal processes from sending input to elevated applications. If gestures do not work while Task Manager, Device Manager, or another administrator window is active, run the GestureSign daemon with administrator privileges too.

- Recommended: open Options, enable `Start GestureSign on Windows Startup`, then enable `Run GestureSign As Administrator At Startup`
- One-time troubleshooting: right-click `GestureSign.ControlPanel.exe` and choose `Run as administrator`; the control panel now starts the daemon elevated as well
- Do not use the executable file's Compatibility tab option `Run this program as an administrator`; GestureSign warns about this because it can break startup and touch-blocking features
