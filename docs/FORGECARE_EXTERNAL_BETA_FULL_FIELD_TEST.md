# ForgeCare External Beta Full Field Test

Target time: approximately 45–90+ minutes. Optional system-changing, upgrade, update and environment-dependent tests may require additional time or a separate tester.

## Tester record

- Tester:
- Date:
- ForgeCare version: `v1.1.0-beta.1`
- Distribution: ☐ Installer ☐ Portable
- Windows edition/version/build:
- Standard user or elevated:
- Machine purpose: ☐ Non-critical test machine ☐ Other: __________
- Existing ForgeCare state: ☐ Fresh ☐ v1.0 upgrade ☐ Existing beta session
- Network: ☐ Online ☐ Offline

Status values: `NOT TESTED`, `PASS`, `FAIL`, `PARTIAL`, `N/A`, `BLOCKED`.

## Safety rules

- Do not deliberately damage Windows or create malicious/corrupt files for this checklist.
- Do not disable Windows security controls.
- Use tester-owned, expendable files for optional cleanup tests.
- Only disable a startup entry you understand and can safely restore.
- A dangerous or unavailable test should be recorded as `N/A` or `BLOCKED`, not forced.
- Review all reports and support packages before sharing them.

## Installation and distribution

| Done | ID | Feature | Class | Risk | Prerequisites | Steps | Expected Result | Actual Result | Status | Notes / Evidence |
|---|---|---|---|---|---|---|---|---|---|---|
| ☐ | FC-BETA-INSTALL-001 | Per-user install and launch | FILES | MEDIUM | Fresh Windows 11 x64 user | Run the approved installer; review choices; launch ForgeCare. | Installs for the current user and opens Technician Edition without requiring a source checkout. |  | NOT TESTED |  |
| ☐ | FC-BETA-INSTALL-002 | Installed identity and uninstall | FILES | MEDIUM | Installed beta | Check Settings/About identity and installed-app entry; close ForgeCare; uninstall if this machine permits. | Version is beta.1. Uninstall removes application binaries without claiming to erase operational app data. |  | NOT TESTED |  |
| ☐ | FC-BETA-INSTALL-003 | Portable launch | FILES | LOW | Extracted portable ZIP | Extract to a writable folder; run `ForgeCare.exe`; inspect Settings → Local Data. | Runs without installation. Data still uses normal ForgeCare local-data locations; “portable” does not mean isolated data. |  | NOT TESTED |  |
| ☐ | FC-BETA-INSTALL-004 | v1.0 in-place upgrade | ENVIRONMENT | MEDIUM | **Optional/environment-dependent:** supported v1.0 installation | Record existing supported settings/session state; run beta installer; launch and inspect retained state. | Stable AppId upgrades in place and supported state remains available. Record precisely what was retained. |  | NOT TESTED |  |

## Shell and navigation

| Done | ID | Feature | Class | Risk | Prerequisites | Steps | Expected Result | Actual Result | Status | Notes / Evidence |
|---|---|---|---|---|---|---|---|---|---|---|
| ☐ | FC-BETA-NAV-001 | Main tab navigation | OBSERVE | LOW | App launched | Visit all 13 tabs and return to Dashboard. | Every tab opens, selected state is clear, and no tab is clipped or unreachable. |  | NOT TESTED |  |
| ☐ | FC-BETA-NAV-002 | Command palette | APP-STATE | LOW | Main window focused | Press `Ctrl+K`; filter the 12 listed destinations; use keyboard and mouse; press Escape; reach Cleanup through its tab. | Listed commands route correctly and the palette closes predictably. Cleanup is not listed in the released palette but remains reachable from the tab strip. |  | NOT TESTED |  |
| ☐ | FC-BETA-NAV-003 | Persistent Continue / Workflow | APP-STATE | LOW | Current workflow/session | Use the top Continue and Workflow controls at different workflow stages. | Controls route to the displayed next stage and never execute a system change themselves. |  | NOT TESTED |  |
| ☐ | FC-BETA-NAV-004 | Window, DPI, scrolling and focus | OBSERVE | LOW | **Optional/environment-dependent:** one or more 100/125/150/200% DPI settings | Resize around minimum usable width; tab through controls; inspect scrollable pages. | Content remains readable; focus is visible; essential controls remain reachable. |  | NOT TESTED |  |

## Dashboard and System Scan

| Done | ID | Feature | Class | Risk | Prerequisites | Steps | Expected Result | Actual Result | Status | Notes / Evidence |
|---|---|---|---|---|---|---|---|---|---|---|
| ☐ | FC-BETA-SESSION-001 | Current report session | APP-STATE | LOW | App launched | Open Reports and Evidence; note the session identity; close/reopen ForgeCare. | Normal product behavior reconnects to the current persisted session where supported. |  | NOT TESTED |  |
| ☐ | FC-BETA-SCAN-001 | Run System Scan | OBSERVE | LOW | Normal Windows access | Select **RUN SYSTEM SCAN** and wait for completion; repeat once if time permits. | Health/system/startup results populate; repeat creates new observations without corrupting prior session Evidence. |  | NOT TESTED |  |
| ☐ | FC-BETA-SCAN-002 | Overview, startup and recommendations | OBSERVE | LOW | Completed scan | Compare OS, CPU, memory, drive and visible startup facts with Windows Settings, Task Manager and Startup Apps. | Results are plausible; recommendations remain advisory and do not mutate Windows. |  | NOT TESTED |  |
| ☐ | FC-BETA-SCAN-003 | Guided next action | APP-STATE | LOW | Scan/workflow state | Use **NEXT BEST ACTION** / **CONTINUE SAFE FLOW**. | ForgeCare explains and opens the next view; it does not silently execute it. |  | NOT TESTED |  |

## Cleanup

| Done | ID | Feature | Class | Risk | Prerequisites | Steps | Expected Result | Actual Result | Status | Notes / Evidence |
|---|---|---|---|---|---|---|---|---|---|---|
| ☐ | FC-BETA-CLEANUP-001 | Analyze Cleanup | OBSERVE | LOW | Accessible temporary sources | Select **ANALYZE CLEANUP**; review sources, paths, sizes and file counts; run again. | Analysis is read-only, handles inaccessible items, and produces understandable candidates or an honest empty state. |  | NOT TESTED |  |
| ☐ | FC-BETA-CLEANUP-002 | Cleanup review and selection | APP-STATE | LOW | Cleanup candidates | Open **REVIEW CLEANUP**; inspect selection and safety text; close without simulation. | Review alone changes nothing; selected totals are accurate; live cleanup starts locked. |  | NOT TESTED |  |
| ☐ | FC-BETA-CLEANUP-003 | Simulation and optional live cleanup | SYSTEM-CAUTION | HIGH | **Optional/environment-dependent:** only clearly expendable temporary candidates | Select safe candidates; run safety simulation; verify no file change; optionally confirm live cleanup; independently check candidate paths. | Dry run changes nothing. Live cleanup requires explicit Yes confirmation, revalidates safety and reports deleted/skipped/blocked/errors. Cleanup deletion has no general ForgeCare undo. |  | NOT TESTED | Record what changed and why it was safe. |

## Optimize and startup management

| Done | ID | Feature | Class | Risk | Prerequisites | Steps | Expected Result | Actual Result | Status | Notes / Evidence |
|---|---|---|---|---|---|---|---|---|---|---|
| ☐ | FC-BETA-OPTIMIZE-001 | Analyze Optimization | OBSERVE | LOW | Completed System Scan | Select **ANALYZE OPTIMIZATION**; inspect recommendations, impact and plan. | Analysis is advisory; no startup entry is changed. |  | NOT TESTED |  |
| ☐ | FC-BETA-STARTUP-001 | Startup impact inventory | OBSERVE | LOW | **Optional/environment-dependent:** machine has startup entries | Compare names/sources with Task Manager → Startup Apps or Windows Settings. | Inventory and impact labels are understandable; unsupported/machine-wide items are not presented as freely mutable. |  | NOT TESTED |  |
| ☐ | FC-BETA-STARTUP-002 | Dry run and optional disable | SYSTEM-REVERSIBLE | HIGH | **Optional/environment-dependent:** one understood, non-essential current-user startup entry | Open **REVIEW STARTUP CHANGES**; select only that item; run dry run; optionally confirm live disable; verify with Startup Apps and restart ForgeCare. | Nothing is preselected; live disable remains locked until dry run; confirmation names the action; undo record, Safety entry, report action and receipt are created for successful changes. |  | NOT TESTED | Record item and original state. |
| ☐ | FC-BETA-STARTUP-003 | Restore startup entry | SYSTEM-REVERSIBLE | HIGH | **Optional/environment-dependent:** undo record from prior ForgeCare change | Use **RESTORE ALL** in Startup Review or **RESTORE STARTUP STATE** in Safety; confirm; verify independently. | Recorded entry is restored where still valid; result and verification are recorded; errors do not erase remaining recovery information. |  | NOT TESTED | Verify after ForgeCare and, if relevant, Windows restart. |

## Deep Analysis

| Done | ID | Feature | Class | Risk | Prerequisites | Steps | Expected Result | Actual Result | Status | Notes / Evidence |
|---|---|---|---|---|---|---|---|---|---|---|
| ☐ | FC-BETA-ANALYSIS-001 | Run Deep Analysis | OBSERVE | LOW | Normal process access | Run analysis and wait for the second sample; repeat once. | Resource pressure, insights and process observations populate; existing successful behavior remains usable. |  | NOT TESTED |  |
| ☐ | FC-BETA-ANALYSIS-002 | Process/resource correctness | OBSERVE | LOW | **Optional/environment-dependent:** active processes, including possibly protected ones | Compare top CPU/memory consumers with Task Manager near the sample time. | Values are directionally plausible; inaccessible/exiting processes become incomplete or omitted, not fabricated. |  | NOT TESTED | Sampling timing can differ. |
| ☐ | FC-BETA-ANALYSIS-003 | Baseline/history | APP-STATE | LOW | One or more analysis runs | Review sample count, confidence, averages and recurring patterns; restart and run again. | History persists locally and confidence evolves; observations remain diagnostic rather than causal claims. |  | NOT TESTED |  |
| ☐ | FC-BETA-ANALYSIS-004 | Clear Baseline | APP-STATE | MEDIUM | **Optional/environment-dependent:** existing resource history | Select **CLEAR BASELINE**; cancel once, then confirm if acceptable. | Cancel preserves data. Confirm clears ForgeCare history only and explicitly does not change processes, services or Windows settings. |  | NOT TESTED |  |

## Evidence Explorer

| Done | ID | Feature | Class | Risk | Prerequisites | Steps | Expected Result | Actual Result | Status | Notes / Evidence |
|---|---|---|---|---|---|---|---|---|---|---|
| ☐ | FC-BETA-EVIDENCE-001 | Load and refresh | OBSERVE | LOW | Current session, ideally after diagnostics | Open Evidence; refresh; run another diagnostic and refresh again. | Current-session records load once per refresh; new observations appear; Explorer never runs diagnostics or writes Evidence. |  | NOT TESTED |  |
| ☐ | FC-BETA-EVIDENCE-002 | Facets and search | OBSERVE | LOW | Evidence exists | Select category and source facets; combine them; search title/observation/company/classification/metadata; clear filters. | Filtering is immediate and in memory; All states restore the full session set. |  | NOT TESTED |  |
| ☐ | FC-BETA-EVIDENCE-003 | Selection and detail | OBSERVE | LOW | Evidence exists | Use mouse and keyboard to select rows; inspect IDs, subject, observation, measurement, provenance, assessment, correlation and metadata. | Full generic details render; identifiers are selectable; severity and confidence remain distinct; nothing is editable. |  | NOT TESTED |  |
| ☐ | FC-BETA-EVIDENCE-004 | Empty/error/session states | OBSERVE | LOW | New session or naturally unavailable data | Open Evidence before diagnostics; create a new session; use a search with no matches. | “No Evidence Yet” differs from “No Matching Evidence”; old-session records are not shown as current; errors are not mislabeled empty. |  | NOT TESTED | Do not corrupt files manually. |

## Services

| Done | ID | Feature | Class | Risk | Prerequisites | Steps | Expected Result | Actual Result | Status | Notes / Evidence |
|---|---|---|---|---|---|---|---|---|---|---|
| ☐ | FC-BETA-SERVICES-001 | Analyze Services | OBSERVE | LOW | Service inventory readable | Select **ANALYZE SERVICES**; compare several states/startup types with the Windows Services app. | Counts and sampled facts are plausible; inaccessible entries are handled safely. |  | NOT TESTED |  |
| ☐ | FC-BETA-SERVICES-002 | Service insights/read-only boundary | OBSERVE | LOW | Completed service analysis | Inspect classifications and contextual insights; look for action controls. | UI clearly states read-only and offers no start/stop/disable operation. |  | NOT TESTED |  |

## Storage and duplicates

| Done | ID | Feature | Class | Risk | Prerequisites | Steps | Expected Result | Actual Result | Status | Notes / Evidence |
|---|---|---|---|---|---|---|---|---|---|---|
| ☐ | FC-BETA-STORAGE-001 | Storage Deep Scan | OBSERVE | LOW | **Optional/environment-dependent:** accessible built-in scan roots | Select **RUN STORAGE DEEP SCAN**; observe progress and completion. | Scan remains read-only and reports inspected files, skipped directories and large data. |  | NOT TESTED |  |
| ☐ | FC-BETA-STORAGE-002 | Locations and large files | OBSERVE | LOW | Completed storage scan | Review location summaries and findings; compare a few sizes/modified dates in File Explorer. | Values match the files closely; inaccessible paths are skipped rather than guessed. |  | NOT TESTED |  |
| ☐ | FC-BETA-STORAGE-003 | Large-file review | APP-STATE | LOW | Large-file candidates | Open review; verify no item is preselected; select/deselect tester-owned candidates; close. | Selection totals update and no file changes during review. |  | NOT TESTED |  |
| ☐ | FC-BETA-STORAGE-004 | Large-file dry run/recycle | SYSTEM-REVERSIBLE | HIGH | **Optional/environment-dependent:** expendable tester-owned file offered by scan | Run dry run; optionally confirm Recycle Bin action; verify in File Explorer/Recycle Bin; restore from Recycle Bin if desired. | No permanent-delete fallback; live action requires confirmation; result reports recycled/blocked/skipped/errors. |  | NOT TESTED | Record restoration result. |
| ☐ | FC-BETA-DUPLICATES-001 | Exact duplicate scan/cancel | OBSERVE | MEDIUM | **Optional/environment-dependent:** scan roots contain suitable data | Start **FIND EXACT DUPLICATES**; cancel one run; rerun to completion if practical. | Progress/cancel work; results require exact SHA-256 confirmation and show groups/copies/reclaimable data. |  | NOT TESTED |  |
| ☐ | FC-BETA-DUPLICATES-002 | Group review/preservation | APP-STATE | LOW | Duplicate groups | Open review; select redundant tester-owned copies; attempt to select every copy in a group. | Nothing preselected; ForgeCare blocks removal of every copy in a group. |  | NOT TESTED |  |
| ☐ | FC-BETA-DUPLICATES-003 | Duplicate dry run/recycle | SYSTEM-REVERSIBLE | HIGH | **Optional/environment-dependent:** expendable duplicate copies | Run dry run; optionally confirm recycle; verify one source copy remains and recycled copies are in Recycle Bin. | Live action remains locked until safe dry run; confirmation is explicit; no permanent-delete fallback. |  | NOT TESTED | Restore from Recycle Bin if needed. |

## Workflow

| Done | ID | Feature | Class | Risk | Prerequisites | Steps | Expected Result | Actual Result | Status | Notes / Evidence |
|---|---|---|---|---|---|---|---|---|---|---|
| ☐ | FC-BETA-WORKFLOW-001 | Workflow state/next route | APP-STATE | LOW | Active session | Open Workflow; refresh; compare completed/required steps; select **OPEN NEXT STEP**. | State matches actual completed work and routes without executing the step. |  | NOT TESTED |  |
| ☐ | FC-BETA-WORKFLOW-002 | Start New Service Workflow | APP-STATE | MEDIUM | Existing session | Cancel the confirmation once; export anything needed; then start a new workflow if acceptable. | New report session and in-memory state start fresh; Windows changes are not undone and old Evidence is not shown as current. |  | NOT TESTED |  |
| ☐ | FC-BETA-WORKFLOW-003 | Completion/report route | APP-STATE | LOW | **Optional/environment-dependent:** required workflow steps complete | Reach completion and use the report route. | Completion is based on established workflow semantics, not merely on Attention count; report opens. |  | NOT TESTED |  |

## Forge Plan 2.0

| Done | ID | Feature | Class | Risk | Prerequisites | Steps | Expected Result | Actual Result | Status | Notes / Evidence |
|---|---|---|---|---|---|---|---|---|---|---|
| ☐ | FC-BETA-PLAN-001 | Build legacy Plan Actions | OBSERVE | LOW | Scan/analysis results | Select **BUILD / REFRESH FORGE PLAN**; inspect priorities, selection and available route. | Legacy actions remain separate and no action executes during build. |  | NOT TESTED |  |
| ☐ | FC-BETA-PLAN-002 | Technician Attention | OBSERVE | LOW | Persisted Evidence | Review Attention priority, confidence, rationale, investigation and supporting sources; rebuild. | Attention is deterministic, advisory/read-only and clearly not malware detection or AI reasoning. |  | NOT TESTED | Empty is valid. |
| ☐ | FC-BETA-PLAN-003 | View Supporting Evidence | OBSERVE | LOW | Attention item exists | Select **VIEW EVIDENCE**, then return and repeat. | Existing Evidence route opens a canonical supporting record; hidden filters are cleared if needed; no Evidence is changed. |  | NOT TESTED | N/A if no Attention. |
| ☐ | FC-BETA-PLAN-004 | Open selected Plan Action | LEGACY | LOW | Executable Plan Action | Select an action and choose **OPEN NEXT SELECTED ACTION**; stop at the review window. | Correct established review window opens; no mutation occurs until its independent dry-run/confirmation gates. |  | NOT TESTED |  |

## Safety

| Done | ID | Feature | Class | Risk | Prerequisites | Steps | Expected Result | Actual Result | Status | Notes / Evidence |
|---|---|---|---|---|---|---|---|---|---|---|
| ☐ | FC-BETA-SAFETY-001 | Journal/snapshots/undo state | OBSERVE | LOW | Empty or prior safety state | Open Safety; refresh; compare counts and entries after any optional action. | Entries accurately describe action/result/reversibility; empty state is understandable. |  | NOT TESTED |  |
| ☐ | FC-BETA-SAFETY-002 | Restore Startup State | SYSTEM-REVERSIBLE | HIGH | **Optional/environment-dependent:** startup undo records | Review count; cancel once; confirm only if restoration is appropriate; verify in Startup Apps. | Explicit confirmation, bounded restore, result recording and retained errors/recovery information. |  | NOT TESTED |  |
| ☐ | FC-BETA-SAFETY-003 | Clear Action Journal | APP-STATE | MEDIUM | Journal entries | Cancel once; confirm; refresh Safety and inspect startup undo/snapshots/report history. | Journal clears only; startup undo, snapshots, reports and Recycle Bin data remain. |  | NOT TESTED |  |

## Reports

| Done | ID | Feature | Class | Risk | Prerequisites | Steps | Expected Result | Actual Result | Status | Notes / Evidence |
|---|---|---|---|---|---|---|---|---|---|---|
| ☐ | FC-BETA-REPORT-001 | View/refresh Forge Report | OBSERVE | LOW | Current session | Open Reports; refresh; inspect events, checkpoints, before/current and review summaries. | Display matches work performed and does not claim an unobserved after-state. |  | NOT TESTED |  |
| ☐ | FC-BETA-REPORT-002 | Save report details | APP-STATE | LOW | Current session | Enter harmless technician/customer/device/summary/notes; save; restart and reopen. | Details persist and can auto-fill from profile only when configured. |  | NOT TESTED | Avoid real sensitive customer data in beta. |
| ☐ | FC-BETA-REPORT-003 | Professional Service Report | FILES | MEDIUM | Current session; writable destination | Export HTML; optionally open; inspect diagnostics/actions/receipts/verification/checkpoints/unresolved items and Evidence references. | Deterministic readable report; observations, action execution and verification are distinct; partial data is explicit. |  | NOT TESTED | Privacy-inspect before sharing. |
| ☐ | FC-BETA-REPORT-004 | Legacy HTML export | LEGACY | MEDIUM | Current session | Select **EXPORT LEGACY HTML REPORT**; cancel once; export to a test folder. | Legacy HTML remains usable and does not disturb the professional report path. |  | NOT TESTED |  |
| ☐ | FC-BETA-REPORT-005 | Direct Desktop/open last | LEGACY | MEDIUM | Writable Desktop | Export direct to Desktop; select **OPEN LAST EXPORTED REPORT**; then move/delete it and retry. | Export opens when present; missing last file is handled clearly. |  | NOT TESTED |  |
| ☐ | FC-BETA-REPORT-006 | History and before/current | OBSERVE | LOW | Recorded actions/checkpoints | Inspect export history and comparisons before and after re-observation. | History persists; successful execution alone is not presented as proof of intended outcome. |  | NOT TESTED |  |
| ☐ | FC-BETA-REPORT-007 | Start New Session | APP-STATE | MEDIUM | Existing report | Cancel once; export if needed; start new session; inspect Reports and Evidence. | New session is distinct. UI warns that unexported current report may no longer be directly exportable after reset. |  | NOT TESTED |  |

## Tools and support

| Done | ID | Feature | Class | Risk | Prerequisites | Steps | Expected Result | Actual Result | Status | Notes / Evidence |
|---|---|---|---|---|---|---|---|---|---|---|
| ☐ | FC-BETA-TOOLS-001 | Field Test Session | APP-STATE | LOW | Local app data writable | Start a field test; mark steps PASS/WARN/FAIL with notes; restart; complete it. | State and notes persist and overall completion is understandable. |  | NOT TESTED |  |
| ☐ | FC-BETA-TOOLS-002 | Beta Issue Package | FILES | MEDIUM | Issue description and writable destination | Enter harmless issue/reproduction/expected/actual text; export ZIP; inspect contents and source crash history if present. | User-entered context is included intentionally; crash diagnostics are privacy-projected; source crash history is unchanged. |  | NOT TESTED | Review before sharing. |
| ☐ | FC-BETA-RECOVERY-001 | Recovery inspection | OBSERVE | LOW | Local data root | Select **RUN RECOVERY CHECK**; inspect previous-session/stale-staging/recoverable results. | Inspection is bounded and does not mutate state. |  | NOT TESTED |  |
| ☐ | FC-BETA-RECOVERY-002 | Clean Safe Transients | FILES | MEDIUM | Recovery inspection; naturally stale safe transient if available | Review scope; cancel once; confirm only if offered and appropriate; rerun inspection. | Only bounded safe transient/staging data is removed; reports, Evidence, settings and recovery state remain. |  | NOT TESTED | N/A if nothing safe to clean. |
| ☐ | FC-BETA-TOOLS-003 | Regression Suite | OBSERVE | LOW | Normal environment | Select **RUN REGRESSION SUITE**; record exact PASS/WARN/FAIL counts and warning details. | Suite completes without crashing and distinguishes warning from failure. |  | NOT TESTED |  |
| ☐ | FC-BETA-SUPPORT-001 | Support diagnostics refresh | OBSERVE | LOW | Local diagnostics | Refresh; inspect environment, crash-log status and local data root. | Useful bounded state appears; no upload occurs. |  | NOT TESTED |  |
| ☐ | FC-BETA-SUPPORT-002 | Debug Bundle | FILES | MEDIUM | Writable destination; historical crash data if naturally available | Export ZIP; inspect environment, Evidence/reports/settings/safety and crash diagnostic entries. | Crash history is privacy-projected; raw local crash log is not copied; source files remain unchanged. |  | NOT TESTED | Review before sharing. |
| ☐ | FC-BETA-SUPPORT-003 | Open support folders | FILES | LOW | App launched | Open Diagnostics folder and Evidence folder. | File Explorer opens the authoritative paths; no diagnostics rerun and no file is deleted. |  | NOT TESTED |  |
| ☐ | FC-BETA-SUPPORT-004 | External machine preflight | OBSERVE | LOW | Installed or portable beta | Run preflight and record every PASS/WARN/FAIL result. | Preflight is honest about environment and support readiness; warnings remain distinct from failures. |  | NOT TESTED |  |
| ☐ | FC-BETA-SUPPORT-005 | Open packaged field-test checklist | FILES | LOW | Installed or portable beta | Select **OPEN FIELD TEST CHECKLIST**. | The packaged checklist opens in the normal associated application; a missing file is reported clearly. |  | NOT TESTED |  |

## Settings and updates

| Done | ID | Feature | Class | Risk | Prerequisites | Steps | Expected Result | Actual Result | Status | Notes / Evidence |
|---|---|---|---|---|---|---|---|---|---|---|
| ☐ | FC-BETA-SETTINGS-001 | Technician profile/defaults | APP-STATE | LOW | App launched | Enter harmless profile/default values; toggle auto-fill and recovery confirmation; save; restart; inspect Reports defaults. | Preferences persist locally; report auto-fill follows the checkbox; no Windows setting changes. |  | NOT TESTED |  |
| ☐ | FC-BETA-SETTINGS-002 | Reset preferences | APP-STATE | MEDIUM | Changed preferences | Cancel once; confirm reset; restart. | ForgeCare defaults return; Windows and diagnostic data are unchanged. |  | NOT TESTED |  |
| ☐ | FC-BETA-SETTINGS-003 | Open local data | FILES | LOW | Local data root | Select **OPEN FORGECARE DATA** and inspect top-level folders without editing them. | File Explorer opens LocalAppData-backed ForgeCare state. |  | NOT TESTED |  |
| ☐ | FC-BETA-SETTINGS-004 | Release/distribution identity | OBSERVE | LOW | Installed or portable beta | Refresh identity; compare version/channel/runtime/executable/fingerprint; open release folder. | Identity is beta.1/1.1.0.0/beta and reflects actual executable/distribution. |  | NOT TESTED |  |
| ☐ | FC-BETA-UPDATE-001 | Local manifest check | ENVIRONMENT | LOW | **Optional/environment-dependent:** naturally available compatible manifest | Select **CHECK LOCAL MANIFEST**; otherwise record N/A. | Missing/not configured state is clear; valid manifest comparison is observational. Do not tamper with manifests for this test. |  | NOT TESTED |  |
| ☐ | FC-BETA-UPDATE-002 | Remote update config/check | NETWORK | MEDIUM | **Optional/environment-dependent:** no endpoint for ordinary beta; internet as applicable | Confirm URL is empty/not production-configured; try check without URL; optionally save a technician-authorized HTTPS URL only. | No claim of automatic updates. Missing configuration is handled clearly; checks are explicit and HTTPS-only. |  | NOT TESTED | Ordinary result may be N/A/BLOCKED. |
| ☐ | FC-BETA-UPDATE-003 | Verified download/handoff | NETWORK | MEDIUM | **Optional/environment-dependent:** approved HTTPS manifest offering newer installer | Ordinary testers record N/A/BLOCKED. If an authorized fixture exists, download/verify, prepare, confirm and stop before or consciously launch installer. | SHA-256 must match before readiness; handoff requires explicit checkbox/button; ForgeCare never silently installs. |  | NOT TESTED | Positive handoff was internally accepted; do not create a fixture. |

## Privacy review of shareable artifacts

Complete this review for each artifact actually produced:

| Artifact | Produced? | Reviewed before sharing? | Unexpected username/machine/path? | Raw exception/stack? | Credential/token-like data? | Result/notes |
|---|---|---|---|---|---|---|
| Professional Service Report |  |  |  |  |  |  |
| Legacy HTML report |  |  |  |  |  |  |
| Debug Bundle |  |  |  |  |  |  |
| Beta Issue Package |  |  |  |  |  |  |

Do not promise anonymity. Reports intentionally contain technician-entered service context. Issue Packages intentionally contain user-entered issue text. Review every artifact for unrelated personal/customer data before sharing it.

## End-to-end scenarios

These scenarios reuse the case IDs above; they do not add duplicate test cases.

### E2E 1 — Evidence-driven technician session

1. `FC-BETA-SESSION-001` — establish the current session.
2. `FC-BETA-SCAN-001` — run System Scan.
3. `FC-BETA-ANALYSIS-001` — run Deep Analysis.
4. `FC-BETA-EVIDENCE-001` through `003` — inspect Evidence.
5. `FC-BETA-PLAN-001` through `003` — inspect Attention and supporting Evidence.
6. `FC-BETA-PLAN-004` — open, but do not automatically execute, an appropriate Plan Action.
7. If and only if safe, perform one optional reversible startup action using `FC-BETA-STARTUP-002`.
8. Re-run System Scan, then inspect receipt/verification through `FC-BETA-REPORT-001` and `006`.
9. Export `FC-BETA-REPORT-003` and verify factual wording.
10. Close/relaunch and recheck `FC-BETA-SESSION-001`, Evidence and report persistence.

### E2E 2 — Maintenance without mutation

Run Cleanup analysis, Storage Deep Scan, duplicate scan (cancel allowed), Services analysis and Regression Suite. Review all results but stop before live cleanup/recycle. Expected: broad useful diagnostics with no Windows mutation.

### E2E 3 — Optional safe file recovery path

Using tester-owned expendable files only, complete either `FC-BETA-STORAGE-004` or `FC-BETA-DUPLICATES-003`, verify the file enters Recycle Bin, then restore it through Windows. Confirm ForgeCare’s report records the operation without claiming permanent deletion.

### E2E 4 — Support and privacy

Create a harmless issue, export an Issue Package and Debug Bundle, export both report types, inspect all artifacts using the privacy table, and confirm ForgeCare made no upload.

### E2E 5 — Offline and restart

Disconnect normally or use an already-offline machine; run local scan/analysis/report workflows; inspect update UI’s unavailable/not-configured behavior; restart ForgeCare and verify expected persisted versus rebuilt state.

## Restart and persistence record

| State | Expected | ForgeCare restart | Windows restart if relevant | Result/notes |
|---|---|---|---|---|
| Current report session/history | Persist |  |  |  |
| Per-session Evidence | Persist |  |  |  |
| Technician settings/update config | Persist |  |  |  |
| Resource baseline/history | Persist until cleared |  |  |  |
| Safety journal/snapshots/startup undo | Persist |  |  |  |
| Startup receipts/verification | Persist in report state |  |  |  |
| Field-test session | Persist |  |  |  |
| Technician Attention | Rebuild from Evidence |  |  |  |
| Forge Plan presentation | Rebuild from current state |  |  |  |
| Evidence filters/search | May reset on session change |  |  |  |
| Last-export shortcut | Current app run; file remains |  |  |  |

## Human review

1. Which feature was hardest to understand?
2. Which feature felt most useful?
3. Which feature felt unnecessary?
4. Did any result look incorrect or misleading?
5. Did ForgeCare ever appear to claim more certainty than the Evidence justified?
6. Was it always clear when ForgeCare was observing versus changing something?
7. Did any system-changing action surprise you?
8. Did you trust the verification results? Why or why not?
9. Was the Professional Service Report useful?
10. Was there anything you expected ForgeCare to do that you could not find?
11. Which tab/page would you remove or redesign first?
12. Would you use ForgeCare again on another machine?
13. If not, what is the main reason?
14. What would ForgeCare need before you would use it professionally?

Ratings:

- Overall stability (1–5): ___
- Ease of understanding (1–5): ___
- Usefulness (1–5): ___
- Trust/confidence (1–5): ___
- Likelihood of using again (1–5): ___

## Completion summary

| Status | Count |
|---|---:|
| PASS |  |
| FAIL |  |
| PARTIAL |  |
| N/A |  |
| BLOCKED |  |
| NOT TESTED |  |

- Total unique full-field test cases: **70**
- LOW risk: **47**
- MEDIUM risk: **17**
- HIGH risk: **6**
- Optional/environment-dependent: **17**

Every case must receive a disposition before calling a machine’s full field test complete. A `FAIL`, `PARTIAL`, `N/A` or `BLOCKED` result is valid evidence; explain it in Notes / Evidence.
