# ForgeCare External Beta Product Inventory

## Release under test

- Product: ForgeCare — Technician Edition
- Release: `v1.1.0-beta.1` — External Beta
- Windows target: Windows 11 x64
- Source commit: `3639515def54857aef29c2ab9b5e3cf378ff27a6`
- Distribution: unsigned per-user installer and self-contained portable ZIP
- Public release: <https://github.com/Teknekt/ForgeCare/releases/tag/v1.1.0-beta.1>

ForgeCare is local-first. It has no telemetry and does not automatically upload diagnostics, reports, logs, Evidence, or support packages. Windows SmartScreen or reputation warnings may appear because the beta is currently unsigned.

## Test classifications

- **[OBSERVE]** — reads and presents information without changing Windows.
- **[APP-STATE]** — changes ForgeCare configuration, session, history, or selection state.
- **[FILES]** — creates, exports, opens, recycles, or otherwise handles files.
- **[SYSTEM-REVERSIBLE]** — changes Windows through a ForgeCare recovery/undo path.
- **[SYSTEM-CAUTION]** — changes Windows and requires an informed, explicit choice.
- **[ENVIRONMENT]** — useful only when its prerequisite data or environment exists.
- **[NETWORK]** — communicates over the network.
- **[LEGACY]** — older but still accessible functionality retained for regression coverage.

## Navigation map

```text
MAIN WINDOW
├── DASHBOARD
│   ├── Run System Scan
│   ├── Health, system overview, startup inventory and recommendations
│   └── Guided next action / Continue Safe Flow
├── CLEANUP
│   ├── Analyze Cleanup
│   └── Review Cleanup → safety simulation → optional live cleanup
├── OPTIMIZE
│   ├── Analyze Optimization
│   ├── Startup impact analysis
│   └── Review Startup Changes → dry run → optional disable / restore
├── ANALYSIS
│   ├── Run Deep Analysis
│   ├── Resource/process insights
│   └── Resource baseline/history → Clear Baseline
├── EVIDENCE
│   ├── Current-session Evidence load/refresh
│   ├── Search and category/source facets
│   └── Generic detail inspector
├── SERVICES
│   └── Read-only Windows service analysis and insights
├── STORAGE
│   ├── Storage Deep Scan
│   ├── Large-file review → dry run → optional Recycle Bin cleanup
│   └── Exact duplicate scan → cancel/review → dry run → optional Recycle Bin cleanup
├── WORKFLOW
│   ├── Guided service progress / next step
│   └── Start New Service Workflow
├── FORGE PLAN
│   ├── Read-only Technician Attention
│   ├── View Supporting Evidence
│   └── Legacy Plan Actions / Open Next Selected Action
├── SAFETY
│   ├── Journal, snapshots and startup undo state
│   ├── Restore Startup State
│   └── Clear Journal
├── REPORTS
│   ├── Report details and session comparison
│   ├── Professional Service Report export
│   ├── Legacy HTML exports / open last export
│   ├── Export history
│   └── Start New Session
├── TOOLS
│   ├── Field Test Session and Issue Package
│   ├── Stability/recovery inspection and safe-transient cleanup
│   ├── Regression Suite
│   ├── Support diagnostics / Debug Bundle / data folders
│   └── External machine preflight and checklist
└── SETTINGS
    ├── Technician profile and report defaults
    ├── Local data and release identity
    ├── Local manifest check
    └── Technician-configured HTTPS update discovery, verified download and installer handoff

GLOBAL SHELL
├── COMMANDS (Ctrl+K)
├── persistent CONTINUE and WORKFLOW controls
└── remembered selected tab / UX state
```

## User-accessible feature inventory

The feature number is used only for coverage accounting. “Changes state” identifies the main side effect; prerequisites and expected results are expanded in the full field-test checklist.

| # | Area | User-visible feature | Class | Changes state | Prerequisite / expected result |
|---:|---|---|---|---|---|
| 1 | Distribution | Per-user installer install and launch | FILES | Installs under the current user | Windows 11 x64; app launches as Technician Edition |
| 2 | Distribution | Installed identity and uninstall | FILES | Installs/removes binaries; app data is retained | Installed build; version is beta.1 and uninstall is controlled |
| 3 | Distribution | Portable extract and launch | FILES | Writes normal local app data, not installation state | Extracted ZIP; no installation required |
| 4 | Distribution | Genuine v1.0 in-place upgrade | ENVIRONMENT | Replaces installed application binaries | Existing supported v1.0 installation |
| 5 | Shell | Main tab navigation | OBSERVE | Remembers selected tab | Main window; all 13 tabs reachable |
| 6 | Shell | Command palette (`Ctrl+K`) | APP-STATE | Changes current view | Main window focused; 12 listed destinations filter and route correctly; Cleanup is reached through its tab, not the current palette |
| 7 | Shell | Persistent Continue / Workflow controls | APP-STATE | Changes current view | Active workflow; routes to current next step |
| 8 | Shell | Window sizing, scrolling and keyboard focus | OBSERVE | None | 100–200% DPI or smaller supported window |
| 9 | Dashboard | Start/current Forge Report session | APP-STATE | Creates or resumes report session | Launch; session identity is visible in Reports/Evidence |
| 10 | Dashboard | Run System Scan | OBSERVE | Persists report and Evidence observations | Normal Windows access; health and baseline populate |
| 11 | Dashboard | System overview, startup items and recommendations | OBSERVE | None beyond scan results | Completed System Scan; values are plausible against Windows |
| 12 | Dashboard | Guided next action / Continue Safe Flow | APP-STATE | Navigates only | Scan/workflow state; next route is understandable |
| 13 | Cleanup | Analyze Cleanup | OBSERVE | Stores analysis in current report state | Accessible temporary locations; candidates/sources shown |
| 14 | Cleanup | Cleanup selection and review | APP-STATE | Selection only | Cleanup analysis; review starts with safe explicit selection |
| 15 | Cleanup | Safety simulation and optional live cleanup | SYSTEM-CAUTION | Deletes allowlisted temporary files | Selected expendable candidates; dry run and confirmation required |
| 16 | Optimize | Analyze Optimization | OBSERVE | Stores recommendations | System profile from scan; recommendations and impact appear |
| 17 | Optimize | Startup impact inventory | OBSERVE | None | Startup entries present; impact/candidate labels are conservative |
| 18 | Optimize | Startup dry run and optional disable | SYSTEM-REVERSIBLE | Disables supported current-user startup entries | Technician-selected expendable item; dry run and confirmation required |
| 19 | Optimize | Restore startup changes | SYSTEM-REVERSIBLE | Restores recorded startup entries | ForgeCare startup undo record exists |
| 20 | Analysis | Run Deep Analysis | OBSERVE | Persists history, report and Evidence | Normal process access; resource results populate |
| 21 | Analysis | System insights and process consumers | OBSERVE | None beyond analysis | Completed Deep Analysis; protected processes may be incomplete |
| 22 | Analysis | Resource baseline/history and recurring patterns | APP-STATE | Appends local resource history | One or more Deep Analysis runs |
| 23 | Analysis | Clear Resource Baseline | APP-STATE | Deletes ForgeCare resource-history data | Existing baseline; confirmation required; Windows unchanged |
| 24 | Evidence | Load/refresh current-session Evidence | OBSERVE | Reads persisted Evidence only | Current report session; typed empty/error states are possible |
| 25 | Evidence | Category/source facets and search | OBSERVE | In-memory filter state | Evidence exists; combined filters and clear work without disk reload |
| 26 | Evidence | Record selection and detail inspector | OBSERVE | Selection only | Evidence exists; provenance, severity, confidence and metadata shown |
| 27 | Evidence | Session/error/empty-state handling | OBSERVE | None | New session, filtered empty, or unavailable/malformed document |
| 28 | Services | Analyze Services | OBSERVE | Stores service analysis in report | Windows Service Control Manager readable |
| 29 | Services | Service classifications and insights | OBSERVE | None | Completed service analysis; no service control is offered |
| 30 | Storage | Run Storage Deep Scan | OBSERVE | Stores storage analysis in report | Selected built-in scan roots accessible |
| 31 | Storage | Storage locations and large-file findings | OBSERVE | None | Completed storage scan; inaccessible folders are counted/skipped |
| 32 | Storage | Large-file review and selection | APP-STATE | Selection only | Cleanup candidates exist; nothing preselected |
| 33 | Storage | Large-file dry run and optional recycle | SYSTEM-REVERSIBLE | Moves selected files to Recycle Bin | Tester-owned expendable candidate; dry run and confirmation required |
| 34 | Storage | Find Exact Duplicates / cancel | OBSERVE | Hashes files; cancellation state only | Duplicate candidates or sizeable data; scan can be cancelled |
| 35 | Storage | Duplicate group review and preservation guard | APP-STATE | Selection only | Exact duplicate groups; at least one copy per group must remain |
| 36 | Storage | Duplicate dry run and optional recycle | SYSTEM-REVERSIBLE | Moves redundant copies to Recycle Bin | Tester-owned duplicate; dry run and confirmation required |
| 37 | Workflow | Guided workflow state, refresh and next route | APP-STATE | Navigation and workflow view state | Active report workflow |
| 38 | Workflow | Start New Service Workflow | APP-STATE | Starts new report session; clears in-memory analysis | Confirmation; does not undo Windows changes |
| 39 | Workflow | Completion and report route | APP-STATE | Navigates to report | Required workflow complete |
| 40 | Forge Plan | Build/refresh legacy Plan Actions | OBSERVE | Rebuilds transient plan | Required scan/analysis results |
| 41 | Forge Plan | Evidence-derived Technician Attention | OBSERVE | Rebuilds transient Attention | Persisted current-session Evidence; advisory only |
| 42 | Forge Plan | View Supporting Evidence | OBSERVE | Navigates/selects Evidence | Attention item with support; no Evidence mutation |
| 43 | Forge Plan | Select and open next legacy Plan Action | LEGACY | Routes to established review UI | Executable legacy plan item; execution still requires review safeguards |
| 44 | Safety | Recovery state, journal and snapshots | OBSERVE | Reads local safety state | Prior or empty safety state |
| 45 | Safety | Restore Startup State | SYSTEM-REVERSIBLE | Restores all recorded startup undo entries | Undo records; confirmation required |
| 46 | Safety | Clear Action Journal | APP-STATE | Clears journal only | Confirmation; undo, snapshots, reports and Recycle Bin remain |
| 47 | Reports | View/refresh current Forge Report | OBSERVE | Refreshes projection | Current session; before/current and activity shown |
| 48 | Reports | Edit/save professional report details | APP-STATE | Persists metadata/notes | Current session; export also saves unsaved fields |
| 49 | Reports | Export Professional Service Report | FILES | Writes privacy-bounded HTML | Valid destination; may report partial-data notes |
| 50 | Reports | Export Legacy HTML with Save dialog | LEGACY | Writes legacy HTML | Valid destination |
| 51 | Reports | Export Legacy directly to Desktop / open last | LEGACY | Writes/opens HTML | Writable Desktop; last export exists for open action |
| 52 | Reports | Export history and before/current comparison | OBSERVE | Reads persisted report history | Recorded actions/checkpoints and repeated observations |
| 53 | Reports | Start New Session | APP-STATE | Replaces current report session | Export current report first if needed; old Windows changes remain |
| 54 | Tools | Start/score/complete Field Test Session | APP-STATE | Persists field-test checklist state | Local app data writable |
| 55 | Tools | Export Beta Issue Package | FILES | Writes ZIP containing entered issue context and projected diagnostics | Issue description and destination; review before sharing |
| 56 | Tools | Run Stability/Recovery Check | OBSERVE | Reads recovery/staging state | Local data root accessible |
| 57 | Tools | Clean Safe Transients | FILES | Deletes bounded stale staging/transient files | Recovery inspection; explicit confirmation |
| 58 | Tools | Run Regression Suite | OBSERVE | Produces local PASS/WARN/FAIL result | Normal application environment |
| 59 | Tools | Refresh Support Diagnostics | OBSERVE | Reads environment/crash/support state | Local diagnostics accessible |
| 60 | Tools | Export Debug Bundle | FILES | Writes support ZIP | Destination; projected crash diagnostics; review before sharing |
| 61 | Tools | Open Diagnostics/Evidence folders | FILES | Opens File Explorer only | Folders may be created/empty; no diagnostic run |
| 62 | Tools | External Machine Preflight | OBSERVE | Reads support readiness | Installed or portable environment |
| 63 | Tools | Open Field Test Checklist | FILES | Opens packaged checklist | Checklist file present |
| 64 | Settings | Technician profile and report defaults | APP-STATE | Writes local settings | Optional technician/customer/device text |
| 65 | Settings | Reset preferences | APP-STATE | Restores ForgeCare defaults | Confirmation; Windows unchanged |
| 66 | Settings | Open ForgeCare data root | FILES | Opens File Explorer | Local data root available |
| 67 | Settings | Release/distribution identity and release folder | OBSERVE | Reads identity; opens folder | Installed/portable build |
| 68 | Settings | Check Local Manifest | ENVIRONMENT | Reads selected/offline manifest | Compatible local manifest exists; ordinary testers may mark N/A |
| 69 | Settings | Configure/check remote update | NETWORK | Persists URL/channel and performs explicit HTTPS request | No production endpoint is configured; ordinary expected state is unavailable/not configured |
| 70 | Settings | Verified update download and installer handoff | NETWORK | Downloads verified installer; explicit launch may start installer and close ForgeCare | Approved HTTPS manifest/update only; ordinary external test is N/A/BLOCKED |

## Conditional and legacy areas

Conditional/environment-dependent areas include genuine v1.0 upgrade, startup mutation/restore, cleanup candidates, duplicate files, protected process/service access, local manifests, and HTTPS update download/handoff. A tester may record `N/A` or `BLOCKED`; they must not manufacture dangerous system state.

Accessible legacy functionality:

- Legacy Forge Plan actions remain the route into established review windows.
- Legacy HTML report export, direct-to-Desktop export, and “open last report” remain visible.

Internal or not directly user-accessible:

- Evidence repositories, adapters, correlation rules, action receipts, verification evaluators, and privacy projectors operate behind the visible workflows.
- Startup and Process Intelligence have no separate mutation UI; their results appear through Evidence and Attention.

Dead/development-only UI found:

- `LOAD SAMPLE SESSION` is collapsed in the released Dashboard. Its demo window and handler are not part of external field-test coverage.

Visible navigation constraint:

- The command palette lists 12 destinations and does not list Cleanup. Cleanup remains fully reachable from the main tab strip. This is documented product behavior for beta testing, not a reason to invent an unavailable palette command.

## Test environments

| Code | Environment | Suitable coverage |
|---|---|---|
| E1 | Fresh Windows 11 x64 user, non-critical machine | Installer, first launch, scan, analysis, reports, basic tools |
| E2 | Existing supported ForgeCare v1.0 installation | Optional upgrade preservation |
| E3 | Installed v1.1 beta | Full installed workflow, restart, uninstall |
| E4 | Extracted portable v1.1 beta | Portable launch and local-data semantics |
| E5 | Standard/non-elevated user | Default execution, access-denied handling, safety boundaries |
| E6 | Machine with meaningful startup/process/storage data | Intelligence, Attention and optional safe reversible action |
| E7 | Offline/no-network | Normal local behavior and honest update-unavailable state |
| E8 | Existing persisted ForgeCare session/state | Restart, Evidence, history, receipts, recovery and report persistence |

No single tester needs every environment. System-changing tests are optional and must use tester-owned, expendable or explicitly approved targets.

## Persistence semantics

### Expected to persist

- current Forge Report session and report history;
- schema-1 per-session Evidence;
- technician preferences and remote-update configuration;
- selected-tab UX state;
- Deep Analysis resource history/baseline until explicitly cleared;
- Safety Journal, safety snapshots and startup undo data;
- Startup action receipts and verification results recorded in the report session;
- field-test session state;
- downloaded/staged update files until normal cleanup;
- exported reports and support packages at their chosen paths.

### Expected to rebuild or reset

- Technician Attention and Forge Plan Attention presentation are rebuilt from current-session Evidence;
- Forge Plan action presentation is rebuilt from current scan/analysis state;
- in-memory Dashboard, Cleanup, Optimization, Storage and Service projections reset with a new workflow/session or application process as implemented;
- search, filters and selection are presentation state and may reset on session change;
- “last exported report” open shortcut is tied to the current application run, while export history remains recorded;
- transient staging/recovery warnings may disappear after safe cleanup.

Starting a new session does not undo Windows changes and does not copy old Evidence into the new session.

## Coverage summary

| Area | Feature Count | Test Case Count | OBSERVE | APP-STATE | FILES | SYSTEM-REVERSIBLE | SYSTEM-CAUTION | ENVIRONMENT | NETWORK | LEGACY | Covered? | Notes |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|---|
| Distribution | 4 | 4 | 0 | 0 | 3 | 0 | 0 | 1 | 0 | 0 | Yes | Upgrade optional |
| Shell/navigation | 4 | 4 | 2 | 2 | 0 | 0 | 0 | 0 | 0 | 0 | Yes | Includes keyboard/DPI |
| Dashboard | 4 | 4 | 2 | 2 | 0 | 0 | 0 | 0 | 0 | 0 | Yes | Current session and scan |
| Cleanup | 3 | 3 | 1 | 1 | 0 | 0 | 1 | 0 | 0 | 0 | Yes | Live execution optional |
| Optimize | 4 | 4 | 2 | 0 | 0 | 2 | 0 | 0 | 0 | 0 | Yes | Disable/restore optional |
| Analysis | 4 | 4 | 2 | 2 | 0 | 0 | 0 | 0 | 0 | 0 | Yes | Local baseline included |
| Evidence | 4 | 4 | 4 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | Yes | Generic Explorer states |
| Services | 2 | 2 | 2 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | Yes | Read-only |
| Storage | 7 | 7 | 3 | 2 | 0 | 2 | 0 | 0 | 0 | 0 | Yes | Two optional recycle paths |
| Workflow | 3 | 3 | 0 | 3 | 0 | 0 | 0 | 0 | 0 | 0 | Yes | New-session boundary |
| Forge Plan | 4 | 4 | 3 | 0 | 0 | 0 | 0 | 0 | 0 | 1 | Yes | Attention separate from actions |
| Safety | 3 | 3 | 1 | 1 | 0 | 1 | 0 | 0 | 0 | 0 | Yes | Journal clear is app-state only |
| Reports | 7 | 7 | 2 | 2 | 1 | 0 | 0 | 0 | 0 | 2 | Yes | Professional and legacy |
| Tools | 10 | 10 | 4 | 1 | 5 | 0 | 0 | 0 | 0 | 0 | Yes | Support/privacy included |
| Settings/updates | 7 | 7 | 1 | 2 | 1 | 0 | 0 | 1 | 2 | 0 | Yes | No production endpoint |
| **Total** | **70** | **70** | **29** | **18** | **10** | **5** | **1** | **2** | **2** | **3** | **Yes** | Primary classifications only |

Risk and optional-test totals are maintained in the full checklist:

- LOW-risk tests: 47
- MEDIUM-risk tests: 17
- HIGH-risk tests: 6
- Optional/environment-dependent tests: 17

**Does every meaningful user-accessible ForgeCare v1.1.0-beta.1 feature have a test or recorded disposition? YES.**
