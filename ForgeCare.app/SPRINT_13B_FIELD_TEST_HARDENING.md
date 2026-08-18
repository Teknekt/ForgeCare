# ForgeCare Sprint 13B — Field Test & Beta Hardening

Version: v0.0.32-beta

Added:
- Persistent Field Test Session in TOOLS
- Per-step PASS / WARN / FAIL marking
- Overall field-test completion result
- Built-in REPORT BETA ISSUE capture
- Exportable issue ZIP with issue.json, issue.txt, environment.txt,
  crash.log when available, and current field-test-session.json
- Build/machine identity embedded in issue data

Feature freeze remains in place:
13B adds no new system-changing optimization functionality.

Suggested external-machine use:
1. TOOLS -> START NEW FIELD TEST
2. Follow the beta checklist
3. Mark each step PASS / WARN / FAIL
4. If something behaves incorrectly, complete REPORT BETA ISSUE
5. Export the issue package before altering the environment
6. Complete the field test
