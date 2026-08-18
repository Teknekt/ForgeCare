# ForgeCare 12A.1 — HTML Export Diagnostic Fix

This is deliberately a diagnostic hardening pass rather than another feature sprint.

Reports now have TWO independent export paths:

1. EXPORT HTML REPORT
   - Uses the Windows SaveFileDialog.
   - Reports the dialog result when cancelled/no result is returned.

2. EXPORT DIRECT TO DESKTOP
   - Completely bypasses SaveFileDialog.
   - Writes straight to the current user's Desktop.

Both paths use the exact same ExportReportToPathAsync helper and verify:
- .html extension
- file exists after write
- file length > 0 bytes

The Reports UI also shows the exact last export path and can open the last exported report.

Interpretation:
- If Direct-to-Desktop works but Save Dialog export does not, the issue is SaveFileDialog/UI-related.
- If both fail, the error dialog and crash.log identify the actual filesystem/HTML generator exception.
