# ForgeCare Sprint 10C — Release & Distribution Foundation

Version: **v0.0.23-alpha**

## Included

- Fix for missing `SanitizeReportFileName`.
- `SeverityBrushConverter.cs` included explicitly.
- Stale `StartupPreviewWindow.xaml` is excluded from build; ForgeCare uses `StartupReviewWindow`.
- Product/company/version metadata in the project file.
- Windows application manifest:
  - `asInvoker`
  - Per-monitor V2 DPI awareness
  - long-path awareness
- Visual Studio publish profile:
  - Windows x64
  - self-contained
  - single-file publish
- `scripts/publish-win-x64.ps1` for repeatable Release builds.
- Portable ZIP packaging into `artifacts/`.

## Visual Studio test

1. Close any running ForgeCare instance.
2. Clean Solution.
3. Rebuild Solution.
4. Run with F5.
5. Verify Dashboard, Cleanup, Optimize, Analysis, Services, Reports and Safety.

## Visual Studio publish

Right-click **ForgeCare.app** → **Publish** → select/import
`WinX64SelfContained`.

Or run from PowerShell in the project directory:

```powershell
.\scripts\publish-win-x64.ps1
```

The script creates:

- `artifacts\ForgeCare-win-x64\`
- `artifacts\ForgeCare-v0.0.23-alpha-win-x64.zip`

## Deliberately not included yet

A signed MSI/MSIX installer and code-signing certificate are intentionally deferred.
The 10C goal is a repeatable, portable release pipeline first.
