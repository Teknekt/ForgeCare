# ForgeCare Sprint 17 — Regression & Field-Test Hardening

Version: **v0.0.40-rc1**

ForgeCare is now treated as a release candidate.

## Added
- Read-only Regression Suite in TOOLS.
- PASS / WARN / FAIL summary.
- Checks for:
  - Windows / x64 environment
  - executable and version identity
  - local ForgeCare write access
  - Settings / Reports / Safety / Diagnostics / Recovery directories
  - release fingerprint
  - external-machine preflight
  - previous-session cleanliness
  - stale ForgeCare transient recovery state

## Feature freeze
No new system-changing functionality is introduced in Sprint 17.

## RC1 acceptance
1. Clean + Rebuild.
2. Launch.
3. TOOLS → RUN REGRESSION SUITE.
4. Investigate every FAIL.
5. Treat WARN items as explicit review points.
6. Run the full field-test checklist on a second Windows x64 machine.
7. Verify installer install, upgrade, uninstall, settings persistence.
8. Verify HTML report export.
9. Verify Debug Bundle and Issue Package.
10. Verify update discovery/download/installer handoff.
11. Reboot between at least one test cycle.
12. Do not add features until RC defects are resolved.

If all critical flows pass, proceed to Sprint 18 release packaging and final visual polish.
