# ForgeCare Sprint 15 — Stability & Recovery

Version: **v0.0.38-beta**

## Added

- Clean-shutdown lifecycle marker.
- Detection of a previous ForgeCare session that did not record a clean shutdown.
- Recovery inspection in TOOLS.
- Detection of stale ForgeCare-owned update `.partial` files.
- Detection of stale diagnostic bundle staging directories.
- Safe transient cleanup restricted to ForgeCare-owned transient data older than 30 minutes.
- Recovery findings and reclaimable transient-size summary.

## Safety boundaries

Recovery cleanup does NOT remove:
- technician settings,
- Forge reports,
- safety journal data,
- field-test state,
- verified update installers,
- arbitrary Windows temp files.

The cleaner only targets:
- `%LOCALAPPDATA%\ForgeCare\Updates\*.partial`
- `%LOCALAPPDATA%\ForgeCare\Diagnostics\bundle-*`

and only when the item is older than 30 minutes.

## Unclean session behavior

At startup ForgeCare writes a running-session marker. On normal application exit the marker is removed.
If a marker already exists at the next launch, ForgeCare flags the previous session as potentially interrupted.

This does not automatically roll back actions. It tells the technician to review crash diagnostics and the safety journal before continuing system-changing work.

## Acceptance test

1. Clean + Rebuild.
2. Launch ForgeCare normally.
3. TOOLS → Stability & Recovery should report HEALTHY on a clean environment.
4. Exit ForgeCare normally and reopen; previous session should remain clean.
5. Create an old dummy `.partial` file under the ForgeCare Updates folder and rerun recovery inspection.
6. Confirm RECOVERY AVAILABLE.
7. CLEAN SAFE TRANSIENTS and confirm only the stale dummy transient is removed.
8. Re-test update discovery/download/handoff UI.
9. Re-test HTML report export and Debug Bundle.
