# ForgeCare Sprint 13C — Installer & Update Foundation

Version: **v0.0.33-beta**

## Added

- Stable upgrade-safe Inno Setup identity.
- Per-user installer remains the default.
- Newer installers using the same ForgeCare AppId upgrade the installed build in place.
- Installer closes/restarts affected app processes through Windows Restart Manager behavior.
- ForgeCare local operational data remains outside the install directory and is deliberately preserved during uninstall.
- Settings now shows **Distribution & Update** state:
  - installed vs portable/development
  - build/channel
  - update policy
  - executable path
  - local data path
  - abbreviated SHA-256 release fingerprint
- Release pipeline now creates SHA-256 fingerprints.
- `artifacts\release-manifest.json` becomes the contract for future update discovery.
- `release-info.json` is embedded in portable/installer payloads.
- `scripts\forge-installer.cmd` added.
- Upgrade test checklist added.

## Update policy

13C does **not** silently download or execute updates.

Current policy:

`MANUAL BETA · SAME APP ID UPGRADES IN PLACE`

This gives ForgeCare a deterministic installer/upgrade contract before network update discovery is introduced.

## Stable installer AppId

`{0F34D1F2-0B94-4F4F-A63D-F0A15E7D11C7}`

Do not change this identifier for normal ForgeCare upgrades. Changing it would create a separate Windows application identity.

## Build

Portable + optional installer:

`scripts\forge-release.cmd`

Installer-focused launcher:

`scripts\forge-installer.cmd`

If Inno Setup 6 is unavailable, the portable build still succeeds and the installer step is skipped.
