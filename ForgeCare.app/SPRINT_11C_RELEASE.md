# ForgeCare Sprint 11C — Real Distribution

Version: **v0.0.27-alpha**

## What this sprint adds

- ForgeCare application icon (`Assets\Icons\ForgeCare.ico`)
- Windows manifest with:
  - `asInvoker`
  - PerMonitorV2 DPI awareness
  - long-path awareness
- Release metadata bumped to v0.0.27-alpha
- Self-contained Windows x64 publish profile
- Repeatable release pipeline:
  - Release build
  - self-contained single-file publish
  - portable ZIP
  - optional Inno Setup installer
- Per-user Inno Setup installer definition
- No silent admin elevation
- No fake code-signing step

## Fastest test

From the project root, double-click:

`scripts\forge-portable-only.cmd`

Expected output:

`artifacts\publish\win-x64\ForgeCare.exe`

and:

`artifacts\ForgeCare-v0.0.27-alpha-win-x64-portable.zip`

## Full installer build

Install **Inno Setup 6** on the development PC, then run:

`scripts\forge-release.cmd`

If Inno Setup is found, ForgeCare also creates:

`artifacts\installer\ForgeCare-v0.0.27-alpha-Setup.exe`

The installer is per-user by default:

`%LOCALAPPDATA%\Programs\ForgeCare`

This keeps the alpha installer aligned with ForgeCare's current-user / no-silent-elevation safety model.

## About "Windows protected your PC"

The generated alpha EXE/installer is not automatically trusted by SmartScreen.
A public trusted release requires a legitimate code-signing certificate and a
signing process. Sprint 11C deliberately does not pretend otherwise.

## Recommended 11C acceptance test

1. Clean Solution.
2. Rebuild Solution.
3. F5 and verify normal ForgeCare operation.
4. Run `scripts\forge-portable-only.cmd`.
5. Close Visual Studio.
6. Run the published `ForgeCare.exe` from `artifacts\publish\win-x64`.
7. Verify Settings persistence.
8. Run a System Scan.
9. Export a test report.
10. Copy the portable ZIP to a second Windows x64 machine and test there.
11. If Inno Setup is installed, build and test the installer/uninstaller.
