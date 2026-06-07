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
