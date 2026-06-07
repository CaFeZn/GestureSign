# GestureSign

GestureSign is a gesture recognition software for Windows tablet. You can automate repetitive tasks by simply drawing a gesture with your fingers or mouse.

[![Release](https://img.shields.io/github/release/TransposonY/GestureSign.svg?style=flat-square)](https://github.com/TransposonY/GestureSign/releases/latest)

## Feature

- Activate Window
- Window Control
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

## Build

- Open `GestureSign.sln` in Visual Studio 2022, or run `.\scripts\build.ps1`
- The solution now targets `.NET Framework 4.8`
- NuGet packages are restored with `packages.config`, so restore is required before the first build
- Windows 11 on Arm64 uses the `Any CPU` build; executable projects set `Prefer32Bit=false`, so they can run natively when the .NET Framework 4.8.1 Arm64 runtime is installed

## Administrator Windows

Windows blocks normal processes from sending input to elevated applications. If gestures do not work while Task Manager, Device Manager, or another administrator window is active, run the GestureSign daemon with administrator privileges too.

- Recommended: open Options, enable `Start GestureSign on Windows Startup`, then enable `Run GestureSign As Administrator At Startup`
- One-time troubleshooting: right-click `GestureSign.ControlPanel.exe` and choose `Run as administrator`; the control panel now starts the daemon elevated as well
- Do not use the executable file's Compatibility tab option `Run this program as an administrator`; GestureSign warns about this because it can break startup and touch-blocking features
