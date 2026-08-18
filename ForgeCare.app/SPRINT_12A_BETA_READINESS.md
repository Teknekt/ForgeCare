# ForgeCare Sprint 12A — Beta Readiness

Version: v0.0.28-alpha

Includes 11C.2 hotfix:
- Restores persistent workflow methods.
- Keeps hardened HTML export.
- Verifies HTML exists and is non-empty.
- Offers to open exported report.
- Logs export exceptions.

Sprint 12A:
- App-level crash logging.
- Tools > Beta Diagnostics.
- Environment snapshot.
- Exportable debug bundle ZIP.
- Local diagnostics folder.

Acceptance:
1. Clean + Rebuild.
2. F5 and verify workflow bar.
3. Export HTML report.
4. Open Tools.
5. Export Debug Bundle.
6. Confirm environment.txt inside ZIP.
7. Repeat from portable build outside Visual Studio.
