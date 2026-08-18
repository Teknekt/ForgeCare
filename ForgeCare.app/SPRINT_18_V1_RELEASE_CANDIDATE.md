# ForgeCare Sprint 18 — Final Visual Polish & v1 Release Candidate

Version: **v1.0.0-rc1**

## Visual system
- Premium ForgeCare gold gradient accent.
- Raised dark-surface card system with subtle border/shadow hierarchy.
- Gradient ForgeCare wordmark.
- Accent rule in the product header.
- Refined technician-edition typography.
- Glowing gradient System Health ring.
- Unified primary action gradient.
- Polished persistent workflow surface.
- Existing navigation, safety and technician workflows remain functionally unchanged.

## Release posture
This is the first ForgeCare **v1.0 release candidate**.

No new system-changing features are added.

## Final release gate
Before renaming this build to v1.0.0:
1. Run Regression Suite with zero unexplained FAIL items.
2. Complete one clean installer test on a fresh Windows x64 machine.
3. Complete one upgrade-in-place test.
4. Complete one uninstall/reinstall persistence test.
5. Run Scan → Analysis → Services → Storage → Plan → Safe Action → Verify → HTML Report.
6. Export Debug Bundle.
7. Verify update discovery/download/handoff with a test manifest/artifact.
8. Verify command palette and last-view persistence.
9. Confirm installer and portable SHA-256 values in release-manifest.json.
10. Freeze code except release blockers.

Once those pass, the same release pipeline can produce **ForgeCare-v1.0.0-Setup.exe** for the public website.
