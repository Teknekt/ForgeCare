# ForgeCare 12A.2 — Report Layout Fix

Cause:
The original FORGE REPORT card and the newer PROFESSIONAL REPORT DETAILS card
were occupying the same Grid position. The newer card was visually covering the
old card, so the HTML export controls existed but were literally hidden behind it.

Fix:
The report action controls are moved into the visible PROFESSIONAL REPORT DETAILS
card underneath SAVE REPORT DETAILS.

Visible controls:
- REFRESH REPORT
- EXPORT HTML REPORT
- EXPORT DIRECT TO DESKTOP
- OPEN LAST EXPORTED REPORT
- LAST EXPORT PATH
- START NEW SESSION
- report status text

No report-export C# logic changed in this hotfix.
