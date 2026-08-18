# ForgeCare Technician Edition — External Machine Beta Test

Build: **v0.0.34-beta**

## Scope

This test is for a non-critical Windows x64 machine. Do not begin with a production-critical,
medical, industrial, kiosk, domain-controller, or irreplaceable customer system.

## 1. Launch

- Extract the portable ZIP to a normal user-writable folder.
- Launch `ForgeCare.exe`.
- Confirm the app starts without Visual Studio or a separate .NET install.
- Open **TOOLS** and confirm External Machine Preflight has no FAIL items.

## 2. Identity / Persistence

- Open **SETTINGS**.
- Enter a temporary technician/company identity.
- Save.
- Close ForgeCare completely.
- Reopen ForgeCare.
- Confirm the saved values persist.

## 3. Read-only diagnostics

Run:

1. System Scan
2. Deep Analysis
3. Service Intelligence
4. Storage Deep Scan
5. Optimization analysis

Confirm the UI remains responsive and no unexpected Windows changes occur.

## 4. Guided workflow

- Use NEXT BEST ACTION.
- Use CONTINUE SAFE FLOW.
- Confirm guided actions navigate only.
- Confirm no system-changing action executes without review/confirmation.

## 5. Safe-action test

Use only low-risk disposable test candidates.

- Cleanup Review → Dry Run → Confirm
- Optional: one harmless current-user Startup candidate with recovery available
- Verify Safety journal records the operation.

## 6. Verify

- Run a second System Scan.
- Confirm Before → Current comparison appears.
- Confirm Forge Complete / report readiness when required workflow is complete.

## 7. Report

- Fill Professional Report Details.
- Export HTML report.
- Open exported report.
- Confirm customer/device/technician data and before/after values are correct.

## 8. Diagnostics bundle

- Open TOOLS.
- Export Debug Bundle.
- Open ZIP and confirm `environment.txt` exists.
- Review the ZIP before sharing because it can contain local ForgeCare report/settings/safety metadata.

## 9. Restart test

- Close ForgeCare.
- Reopen it.
- Confirm Settings survive.
- Confirm the app opens without requiring Visual Studio.

## 10. Result

Record:

- Machine / Windows version
- PASS / WARN / FAIL preflight counts
- What worked
- What failed
- Whether HTML export worked
- Whether Debug Bundle worked
- Any crash-log entry
- Screenshots of unexpected UI state

If a bug occurs, stop the affected operation and export a Debug Bundle before changing the environment.
