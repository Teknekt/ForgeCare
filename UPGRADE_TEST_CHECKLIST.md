# ForgeCare 13C — Installer Upgrade Test

Build: **v0.0.33-beta**

Use a non-critical Windows x64 test account.

## Fresh install
1. Build `scripts\forge-installer.cmd`.
2. Run `ForgeCare-v0.0.33-beta-Setup.exe`.
3. Accept the default per-user install path.
4. Launch ForgeCare.
5. Settings → Distribution & Update should show `INSTALLER MANAGED`.
6. Save a technician/company profile.
7. Create a field-test session.

## Upgrade-in-place simulation
The installer AppId is intentionally stable:

`{0F34D1F2-0B94-4F4F-A63D-F0A15E7D11C7}`

For the next ForgeCare beta build, build a newer Setup package with the same AppId.
Run the newer installer while this version is installed.

Verify:
- The existing installation directory is reused.
- ForgeCare application files are replaced by the newer build.
- `%LOCALAPPDATA%\ForgeCare` remains intact.
- Technician settings survive.
- Field-test/report/safety state survives as appropriate.
- Windows Apps / uninstall entry shows the newer version.

## Uninstall
1. Uninstall ForgeCare from Windows Settings.
2. Confirm the program files under `%LOCALAPPDATA%\Programs\ForgeCare` are removed.
3. Confirm `%LOCALAPPDATA%\ForgeCare` remains preserved intentionally.

## Release integrity
After `forge-release.cmd`, inspect:

`artifacts\release-manifest.json`

Confirm it contains:
- version
- channel
- stable AppId
- portable SHA-256
- installer SHA-256 when installer was built

Remote update discovery remains disabled in 13C.
