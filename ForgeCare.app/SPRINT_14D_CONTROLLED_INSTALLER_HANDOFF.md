# ForgeCare Sprint 14D — Controlled Installer Handoff

Version: **v0.0.37-beta**

## Added

- Explicit PREPARE VERIFIED INSTALL stage.
- SHA-256 re-verification before installation becomes available.
- Mandatory technician confirmation checkbox.
- Final SHA-256 verification immediately before launch.
- Windows Shell installer handoff.
- No silent-install switches.
- No Windows security/UAC bypass.
- ForgeCare exits only after Windows successfully receives the installer launch request.

## Update chain

Remote HTTPS manifest
→ validate ForgeCare identity / AppId / channel / version
→ explicit download
→ SHA-256 verification
→ staged installer
→ PREPARE VERIFIED INSTALL
→ SHA-256 re-verification
→ technician confirmation
→ final SHA-256 verification
→ Windows Shell handoff
→ ForgeCare closes

## Important

The installer itself remains responsible for the actual in-place upgrade behavior.
Sprint 14D does not suppress installer UI, force elevation, or silently modify the installed application.

## Next phase

With Sprint 14D, the core update lifecycle is feature-complete.

Recommended next phase:
**Sprint 15 — Stability & Recovery**

Focus:
- crash/recovery hardening
- cancellation and partial-operation recovery
- permission/path/network edge cases
- state consistency after interrupted operations
- targeted regression tests before UX freeze
