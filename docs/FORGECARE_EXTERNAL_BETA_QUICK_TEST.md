# ForgeCare External Beta Quick Test

Target time: 15–20 minutes. This path validates first launch and the central evidence-driven workflow without changing Windows configuration.

## Before you start

- Use ForgeCare `v1.1.0-beta.1` on Windows 11 x64.
- The beta is unsigned. Windows SmartScreen or reputation warnings may appear.
- Use a non-critical machine and run as your normal Windows user.
- Do not disable security software or elevate solely for this test.
- Record each result as `PASS`, `FAIL`, `PARTIAL`, `N/A`, or `BLOCKED`.

Build type: ☐ Installer ☐ Portable

Windows version/build: ____________________

Machine type/use: ____________________

## Quick checklist

### 1. Launch and orientation — `FC-BETA-INSTALL-001`, `FC-BETA-NAV-001`

- [ ] Launch ForgeCare and confirm the title identifies Technician Edition.
- [ ] Confirm these tabs are visible: Dashboard, Cleanup, Optimize, Analysis, Evidence, Services, Storage, Workflow, Forge Plan, Safety, Reports, Tools, Settings.
- [ ] Open several tabs and confirm navigation and scrolling remain responsive.

Expected: the application opens without a fatal dialog; all major areas are discoverable.

Status: __________  Notes: ________________________________________________

### 2. System Scan — `FC-BETA-SCAN-001`

- [ ] On **Dashboard**, select **RUN SYSTEM SCAN**.
- [ ] Wait for completion.
- [ ] Review health, storage, memory, startup items, system overview and recommendations.
- [ ] Compare a few facts with Windows Settings or Task Manager.

Expected: the scan completes, results are plausible, and no system settings are changed.

Status: __________  Notes: ________________________________________________

### 3. Deep Analysis — `FC-BETA-ANALYSIS-001`

- [ ] Open **Analysis** and select **RUN DEEP ANALYSIS**.
- [ ] Review system pressure, process consumers and insights.
- [ ] Compare the most visible CPU/memory consumers with Task Manager, allowing for sampling-time differences.

Expected: analysis completes; inaccessible processes are handled conservatively rather than causing total failure.

Status: __________  Notes: ________________________________________________

### 4. Evidence Explorer — `FC-BETA-EVIDENCE-001`, `FC-BETA-EVIDENCE-002`

- [ ] Open **Evidence** and select **REFRESH** if needed.
- [ ] Confirm System Scan, Startup Intelligence, Deep Analysis or Process Intelligence records appear as available.
- [ ] Try a category facet, a source facet and a search term.
- [ ] Select a record and inspect provenance, observation, severity, confidence, correlation and metadata.
- [ ] Select **CLEAR FILTERS**.

Expected: records remain read-only; filters/search work without rerunning diagnostics; severity and confidence remain separate.

Status: __________  Notes: ________________________________________________

### 5. Forge Plan and Attention — `FC-BETA-PLAN-001`, `FC-BETA-PLAN-002`

- [ ] Open **Forge Plan** and select **BUILD / REFRESH FORGE PLAN**.
- [ ] Confirm **ATTENTION** is visually separate from **PLAN ACTIONS**.
- [ ] If an Attention Item exists, read its rationale and suggested investigation.
- [ ] Do not execute a Plan Action during the quick test.

Expected: Attention is advisory/read-only and does not look like automatic remediation.

Status: __________  Notes: ________________________________________________

### 6. Supporting Evidence — `FC-BETA-PLAN-003`

- [ ] If an Attention Item is available, select **VIEW EVIDENCE**.

Expected: ForgeCare navigates to Evidence and selects a supporting record without changing it. Record `N/A` if no Attention Item exists.

Status: __________  Notes: ________________________________________________

### 7. Professional Service Report — `FC-BETA-REPORT-003`

- [ ] Open **Reports**.
- [ ] Enter harmless test report details if desired.
- [ ] Select **EXPORT PROFESSIONAL SERVICE REPORT** and save it to a test folder.
- [ ] Open the HTML file and confirm it reflects this session and distinguishes observations, actions and verification.
- [ ] Check it for unexpected username, machine name, personal paths, tokens or unrelated personal information before sharing.

Expected: a readable local HTML report is created; partial or unavailable data is described honestly.

Status: __________  Notes: ________________________________________________

### 8. General UX review

- [ ] Press `Ctrl+K`, search for a listed destination such as Evidence, and navigate using the command palette. Cleanup is reached from the tab strip in this release.
- [ ] Confirm it was clear when ForgeCare was observing versus offering a system-changing review path.
- [ ] Close ForgeCare normally.

Expected: navigation is understandable and no system mutation occurred during this quick path.

Status: __________  Notes: ________________________________________________

## Quick feedback

- Overall stability (1–5): ___
- Ease of understanding (1–5): ___
- Usefulness (1–5): ___
- Trust/confidence (1–5): ___
- Most useful feature: _________________________________________________
- Hardest feature to understand: _______________________________________
- Any incorrect or misleading result: __________________________________

If reporting a bug, use `FORGECARE_EXTERNAL_BETA_BUG_REPORT.md`. Review every support package before sharing it, and do not put private information in a public issue.
