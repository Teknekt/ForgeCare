# ForgeCare Sprint 19 — v1.0 Release

Version: **v1.0.0**

## Purpose
Sprint 19 is the release gate, not a feature sprint.

## Included
- WPF `CharacterSpacing` hotfix from Sprint 18.
- Final version identity: `v1.0.0`.
- Final installer naming: `ForgeCare-v1.0.0-Setup.exe`.
- Release channel moved from RC/beta identity to stable release metadata.
- Existing installer AppId remains unchanged for upgrade continuity.

## Release gate
Before publishing the Setup file:
1. Clean Solution.
2. Delete `bin` and `obj`.
3. Rebuild.
4. Run Regression Suite.
5. Resolve every unexplained FAIL.
6. Fresh-install on a second Windows x64 machine.
7. Run the complete technician workflow.
8. Export HTML report.
9. Export Debug Bundle.
10. Test update discovery/download/handoff against a controlled test manifest.
11. Test uninstall/reinstall persistence.
12. Run `scripts\forge-release.cmd`.
13. Confirm `artifacts\release-manifest.json`.
14. Confirm SHA-256 for the Setup file.
15. Publish the Setup file to the website.

## Public artifact
Recommended public download:

`ForgeCare-v1.0.0-Setup.exe`

Optional advanced artifact:

`ForgeCare-v1.0.0-win-x64-portable.zip`
