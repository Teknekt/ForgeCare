# ForgeCare Sprint 14A — Update Discovery Foundation

Version: **v0.0.34-beta**

## Hotfix carried into 14A
The 13C patch is superseded by this package. `MainWindow.xaml` and
`MainWindow.xaml.cs` are shipped as a matched pair so all named controls from
13B/13C are present in the generated WPF namescope.

This specifically covers the missing-control build cascade involving:
- ShellStatusText
- BetaField* controls
- BetaIssue* controls
- ExternalTest* controls
- GuidedAction* controls
- LastResult* controls

Do not combine an older MainWindow.xaml with the 14A code-behind.

## 14A added
- Offline update discovery.
- Local `release-manifest.json` picker in Settings.
- Product/AppId validation before version comparison.
- Current / Update Available / Older Build / Invalid Manifest states.
- No download.
- No automatic execution.
- No silent update.
- Stable ForgeCare installer AppId remains unchanged.

## Test
1. Clean Solution.
2. Delete `bin` and `obj` if Visual Studio still shows stale generated-XAML errors.
3. Rebuild.
4. Run ForgeCare.
5. Settings -> Distribution & Update should render.
6. Settings -> Update Discovery -> CHECK LOCAL MANIFEST.
7. Select a release-manifest.json from an older/equal/newer ForgeCare release.
8. Confirm the state is reported without changing the machine.
