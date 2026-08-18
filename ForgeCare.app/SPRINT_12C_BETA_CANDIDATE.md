# ForgeCare Sprint 12C — Polish & Beta Candidate

Version: v0.0.30-beta

## Focus
12C intentionally avoids adding new system-changing functionality. It freezes the
feature surface and polishes the shell for external-machine testing.

## Added
- Beta Candidate identity in the shell.
- Persistent bottom status bar.
- Safe keyboard navigation:
  - Ctrl+1 Dashboard
  - Ctrl+2 Analysis
  - Ctrl+3 Workflow
  - Ctrl+4 Reports
  - Ctrl+5 Tools
  - Ctrl+, Settings
- Navigation status feedback.
- Release metadata moved from alpha to beta candidate.

## Safety
Keyboard shortcuts only navigate between views. They never trigger scans,
cleanup, startup changes, storage actions, recovery, or export.

## Acceptance test
1. Clean + Rebuild.
2. F5.
3. Confirm header shows v0.0.30-beta and BETA CANDIDATE.
4. Test all six keyboard shortcuts.
5. Confirm shortcuts only navigate.
6. Run System Scan → Guided Action → Analysis.
7. Re-test Report HTML export.
8. Re-test Tools → Export Debug Bundle.
9. Re-test Demo Session.
10. Publish portable win-x64 and run outside Visual Studio.

If all ten pass, this build is a strong first external-machine beta candidate.
