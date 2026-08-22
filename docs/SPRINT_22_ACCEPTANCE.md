# ForgeCare Sprint 22 Acceptance

**Release target:** v1.1-dev  
**Evidence schema:** 1  
**Final status:** SPRINT 22 — ACCEPTED  
**Acceptance basis:** automated verification, structural safety guards, and completed live technician acceptance

## Objective

Sprint 22 adds read-only Startup Evidence Intelligence to ForgeCare. It enriches
startup entries already discovered by System Scan, records conservative and
privacy-safe observations in the existing per-session Evidence document, and
exposes those observations through the generic Evidence Explorer.

The feature is additive. Existing System Scan results, health scoring,
recommendations, Optimize behavior, and technician-controlled startup management
remain authoritative and unchanged.

## Delivered Architecture

```text
System Scan
    ↓
existing StartupScanner output in SystemSnapshot.StartupItems
    ↓
StartupIntelligenceService
    ├── StartupCommandParser
    ├── IStartupFileInspector / WindowsStartupFileInspector
    ├── IStartupSignatureInspector / WinVerifyTrustStartupSignatureInspector
    └── StartupClassificationPolicy
    ↓
StartupIntelligenceResult
    ↓
StartupIntelligenceEvidenceAdapter
    ↓
EvidenceService
    ↓
existing schema-1 JsonEvidenceRepository
    ↓
Evidence Explorer
```

The live integration runs after the accepted successful System Scan processing
and after existing System Scan Evidence capture. It reuses the active Forge
Report session ID, the completed `SystemSnapshot.StartupItems`, and the System
Scan observation timestamp. It does not rerun startup discovery.

Startup Intelligence is a guarded, best-effort enrichment. Its failure cannot
retroactively invalidate an otherwise successful System Scan.

## Phase B — Startup Intelligence Foundation

Phase B established an isolated read-only analysis foundation that consumes
constructed or already-collected `StartupItem` values. It introduced:

- conservative startup-command parsing;
- support for quoted direct executable paths;
- intentionally narrow handling of unquoted paths;
- environment-variable expansion for inspection identity;
- explicit ambiguous, malformed, launcher-mediated, and unresolved-shortcut
  states instead of guessed identities;
- direct local file/version metadata inspection;
- an offline Windows Authenticode inspection boundary;
- deterministic classification and explicit confidence;
- bounded sequential processing, per-run target caching, cancellation, and
  partial-success behavior.

Phase B does not execute commands, search `PATH`, search the disk for possible
executables, rerun `StartupScanner`, modify startup configuration, or persist
Evidence.

The classification taxonomy is:

- `Verified`
- `Known`
- `Unverified`
- `Broken`
- `Suspicious`
- `Unknown`

`Suspicious` exists in the taxonomy for forward compatibility, but Sprint 22
contains no rule that emits it. An existing unsigned executable maps
conservatively to `Known`, not `Suspicious`. Launcher-mediated and unresolved
shortcut entries map to `Unverified`. Malformed or ambiguous identity maps to
`Unknown` rather than `Broken`.

Classification describes available provenance and inspection state. It is not
a malware verdict, necessity decision, or remediation recommendation.

## Phase C — Evidence Projection

Phase C added `EvidenceSource.StartupIntelligence` and a pure
`StartupIntelligenceEvidenceAdapter`. It creates one `EvidenceRecord` for each
valid enriched startup entry.

Typical record semantics are:

- Category: `Startup`
- Source: `StartupIntelligence`
- Collector: `StartupIntelligenceEvidenceAdapter`
- Confidence: copied from the Startup Intelligence result
- Severity: conservative mapping from the existing classification
- Correlation: deterministic privacy-safe startup correlation key

The adapter preserves valid records during partial translation failures. It
does not inspect Windows, rerun analysis, mutate startup state, or write the
repository directly.

## Phase D — Live System Scan Integration

Phase D connected the approved analysis and projection chain to the live
successful System Scan path in `MainWindow`.

Execution order remains:

1. System Scan completes.
2. Existing snapshot, health, recommendations, report, workflow, and Dashboard
   behavior completes.
3. Existing System Scan Evidence is captured.
4. Startup Intelligence analyzes the already-completed startup snapshot.
5. Valid Startup Intelligence Evidence is appended through the existing
   `EvidenceService` and shared repository.

Session identity is sourced from `ForgeReportService.Snapshot().SessionId` and
validated using the established GUID-in-`N` contract. Evidence timestamps use
`snapshot.ScanTime.ToUniversalTime()`.

Invalid sessions, cancellation, analysis/adapter partial failures, persistence
failures, and unexpected exceptions are contained by the additive helper.
Privacy-safe bounded counts or exception type names are logged rather than raw
commands, arguments, or arbitrary path-bearing warnings.

Empty startup collections and zero translated records are successful no-ops.
Repeated successful scans append distinct observations; correlation keys are
not treated as unique record identifiers.

## Offline Authenticode Policy

Startup signature inspection uses `WinVerifyTrust` with a local/offline,
non-interactive policy:

- `WTD_UI_NONE`
- `WTD_REVOKE_NONE`
- `WTD_REVOCATION_CHECK_NONE`
- `WTD_CACHE_ONLY_URL_RETRIEVAL`

The implementation does not launch PowerShell, use an online reputation
service, or require online certificate retrieval. Structural tests protect
these configured flags and the absence of network/command-execution APIs in
the Startup Intelligence foundation.

A `Valid` result means valid under the local/offline Windows Authenticode
evaluation available at inspection time. It does not prove current online
revocation freshness, that an application is harmless, or that a startup entry
is necessary. Signature state remains distinct from classification, severity,
optimization, and remediation advice.

This acceptance does not claim packet-level proof that no network traffic can
occur elsewhere in ForgeCare or Windows.

## Privacy Contract

The persisted Evidence boundary is deliberately narrower than transient
analysis state:

| Data | Treatment |
|---|---|
| Raw startup command | Transient only; never persisted to Evidence |
| Command arguments | Transient only; never persisted to Evidence |
| Sensitive full executable path | Transient where applicable |
| Persisted executable identity/path | Normalized/redacted form only |
| Certificate binary data | Not persisted |

Recognized environment-root tokens include:

- `%USERPROFILE%`
- `%LOCALAPPDATA%`
- `%APPDATA%`
- `%PROGRAMFILES%`
- `%PROGRAMFILES(X86)%`
- `%PROGRAMDATA%`
- `%WINDIR%`

Correlation keys are deterministic and bounded but exclude raw commands,
arguments, session IDs, timestamps, clear-text usernames, and secrets/tokens.
Debug Bundles inherit this privacy model by copying the persisted Evidence
documents rather than exporting a separate Startup Intelligence data store.

Automated privacy tests verify raw-command and argument exclusion, path
normalization/redaction, and correlation-key independence. Manual inspection of
both live and bundled Evidence found no raw startup commands, arguments,
obvious secrets/tokens, or unredacted user-profile paths.

## Evidence Schema Compatibility

Sprint 22 retains `EvidenceDocument.SchemaVersion = 1`. It adds a new string
enum member, `EvidenceSource.StartupIntelligence`, without changing the
document shape, repository architecture, per-session storage location, atomic
write behavior, or query contract.

`SystemScan`, `DeepAnalysis`, and `StartupIntelligence` records coexist in the
same current-session schema-1 JSON document. Existing Evidence inspection,
Regression Suite validation, restart persistence, and Debug Bundle inclusion
continue to use the Sprint 20 infrastructure.

Older ForgeCare builds that do not define the `StartupIntelligence` enum member
may not deserialize newer records containing that source value. This is a
forward-compatibility limitation of the string-enum contract, not a schema
shape migration.

## Evidence Explorer Integration

No dedicated Startup Intelligence UI was introduced. The Sprint 21 Evidence
Explorer consumes Startup records generically through immutable presentation
projections. Existing facets, search, deterministic ordering, stable-ID
selection, generic metadata rendering, and detail inspection support the new
source without source-specific Explorer architecture.

Manual acceptance verified:

- Startup category and Startup Intelligence source facets;
- combined filtering and Clear Filters;
- searches against persisted metadata, company, and classification values;
- selection and generic detail rendering;
- separate severity and confidence;
- correlation and metadata display;
- coexistence of all three live Evidence sources;
- stable loading after repeated scans and restart.

## Runtime Binding Defect and Fix

Live acceptance exposed a WPF runtime defect: display-only `TextBox.Text`
bindings defaulted to TwoWay binding against immutable/read-only Evidence
Explorer presentation properties. Selecting or loading real records could
therefore produce `InvalidOperationException` when WPF attempted to write back.

Affected display-only values included:

- `RawSubject`
- `Id`
- `SessionId`
- `Collector`
- `CorrelationKey`
- metadata `Value`
- `UnsupportedSchemaVersion`
- `SupportedSchemaVersion`

The separately reviewed and merged fix explicitly sets `Mode=OneWay` on these
display-only bindings. `SearchQuery` remains writable with immediate source
updates and was not converted to OneWay.

The fix is confined to `EvidenceExplorerView.xaml`. It introduced no domain,
persistence, schema, ViewModel, or Explorer architecture redesign. The repaired
runtime behavior was manually verified with real persisted System Scan and
Startup Intelligence Evidence.

## Automated Verification

**AUTOMATED — PASS**

Final closeout verification:

- `dotnet restore .\ForgeCare.slnx`: passed
- `dotnet build .\ForgeCare.slnx --no-restore`: passed
- build warnings: 0
- build errors: 0
- `dotnet test .\ForgeCare.slnx --no-build --no-restore`: 190 passed,
  0 failed, 0 skipped
- `git diff --check`: passed

The current suite covers:

- conservative quoted/unquoted command parsing and environment expansion;
- launcher-mediated commands, unresolved shortcuts, and malformed/ambiguous
  identity;
- file metadata inspection and distinct missing/unsupported states;
- offline/cache-only, UI-free Authenticode configuration and signature result
  states;
- deterministic classification and confidence semantics;
- partial success, null inputs, cancellation, input snapshotting, bounded
  100-entry behavior, and duplicate-target caching;
- structural absence of command execution, network clients, scanner coupling,
  startup mutation, service control, and installer handoff from the new
  subsystem;
- privacy-safe path normalization and raw-command/argument exclusion;
- deterministic, bounded, privacy-safe correlation keys;
- schema-1 persistence, reload, inspection, and coexistence with System Scan
  and Deep Analysis;
- generic Evidence Explorer facets, search, metadata, and correlation
  compatibility;
- live System Scan ordering, active-session and UTC timestamp reuse, shared
  repository construction, empty-input handling, and guarded failure boundary.

Structural source/dependency tests are regression guards, not runtime proof of
every possible Windows behavior. The WPF binding correction is validated by
the completed live acceptance; a dedicated full WPF binding-engine test was
not added because reliable coverage would require disproportionate UI
orchestration.

## Manual Acceptance Results

**MANUAL — PASS**

### Initial live integration

- ForgeCare launched from the development build.
- A fresh Forge Report session was created.
- System Scan completed normally.
- System Scan and Startup Intelligence Evidence persisted in schema 1.
- Evidence Explorer loaded successfully after the binding fix.
- Startup facets, rows, detail inspector, severity, confidence, correlation,
  and metadata rendered correctly.
- One independently inspected session contained 23 total records: 6
  `SystemScan` and 17 `StartupIntelligence`.

### Search, facets, and detail privacy

- Startup category, Startup Intelligence source, combined filters, metadata
  search, company-name search, classification search, and Clear Filters passed.
- Multiple Startup Intelligence detail states were reviewed.
- Available name, source, classification, signature, company/product,
  normalized identity/path, correlation, severity, and confidence data rendered.
- No raw command, arguments, obvious secrets/tokens, or unredacted user-profile
  paths were observed.

### Coexistence, repeated scan, and restart

- Deep Analysis ran in the same report session and all three Evidence sources
  coexisted.
- A repeated System Scan appended System Scan and Startup Intelligence
  observations without replacing prior records or Deep Analysis Evidence.
- ForgeCare closed normally, restarted through the development build,
  reconnected using its supported session behavior, and loaded the existing
  persisted Evidence successfully.

### Regression Suite and Debug Bundle

- In-application Regression Suite: **PASS WITH WARNINGS**
  - PASS: 14
  - WARN: 1
  - FAIL: 0
- The warning count is retained in this acceptance record; zero failures were
  observed and the warning did not block Sprint 22 acceptance.
- A Debug Bundle was exported through the normal workflow.
- Bundled Evidence contained the expected persisted records and no observed
  raw commands, arguments, obvious secrets/tokens, or unredacted user-profile
  paths.

## Record Count Verification

| Source | Before repeated System Scan | After repeated System Scan | Delta |
|---|---:|---:|---:|
| Total Evidence | 62 | 83 | +21 |
| SystemScan | 12 | 18 | +6 |
| StartupIntelligence | 32 | 47 | +15 |
| DeepAnalysis | 18 | 18 | 0 |

This verifies append behavior: System Scan and Startup Intelligence added new
observations, Deep Analysis remained intact, previous records remained, and the
document continued to load normally. Exact future Startup Intelligence counts
are not invariant; they depend on the startup entries present during each scan.

## Safety / Read-Only Contract

Startup Intelligence does not:

- enable or disable startup items;
- create, delete, or change registry values;
- move or delete startup files;
- start or terminate processes;
- control Windows services;
- elevate privileges or invoke installers;
- mutate Windows startup configuration;
- change `StartupImpactService` recommendations;
- replace `StartupManagerService`;
- change Optimize behavior;
- execute startup commands or launcher payloads;
- generate startup recommendations or remediation actions.

The existing technician-controlled startup mutation, safety journal, undo, and
recovery workflows remain separate and authoritative.

## Explicit Non-Goals

Sprint 22 intentionally does not add:

- discovery beyond the existing four startup source families;
- RunOnce, scheduled-task, AppX startup-task, browser-startup,
  services-as-startup, Winlogon/policy, or disabled Task Manager startup
  discovery;
- `.lnk` target resolution;
- launcher payload interpretation;
- disk-wide or `PATH` executable discovery;
- online reputation, required online certificate lookup, or cloud services;
- malware detection or safety verdicts;
- startup necessity decisions, recommendations, remediation, or automatic
  disabling;
- a new Startup Intelligence UI or Evidence schema migration;
- changes to startup management, Optimize, report sessions, release pipeline,
  installer, or product version.

## Known Limitations

1. Startup discovery remains limited to the existing four source families.
2. `.lnk` target resolution remains deferred.
3. Launcher-mediated commands are not interpreted to discover their payload.
4. Unquoted ambiguous commands remain unresolved by design.
5. Authenticode is evaluated locally/offline and does not guarantee current
   online revocation status.
6. Classification provides provenance/trust context, not a malware verdict.
7. Startup Intelligence does not decide whether an entry is necessary.
8. Startup Intelligence does not change Optimize or startup-management
   recommendations.
9. Repeated scans append observations rather than deduplicating them.
10. Schema remains 1, but older builds lacking the `StartupIntelligence` enum
    member may not understand newer records.
11. Runtime WPF binding behavior is manually validated; no dedicated full WPF
    binding-engine regression test exists because stable coverage would require
    disproportionate UI orchestration.
12. Nullable warnings in update-discovery code have appeared in prior builds
    and remain outside Sprint 22 scope. The final closeout build emitted zero
    warnings.

## Sprint 22 Definition of Done

| Requirement | Status | Verification |
|---|---|---|
| Read-only Startup Intelligence foundation | PASS | Architecture and automated dependency guards |
| Conservative command parsing | PASS | Automated parser matrix |
| Direct file metadata inspection | PASS | Automated file-inspector tests |
| Offline Authenticode inspection | PASS | Configuration tests and code review |
| Deterministic classification | PASS | Automated policy matrix |
| Confidence separate from classification | PASS | Model/policy and adapter tests |
| Partial-success behavior | PASS | Service and adapter tests |
| No command execution | PASS | Architecture plus structural guards |
| No startup mutation | PASS | Architecture plus structural guards |
| Privacy-safe Evidence projection | PASS | Automated and manual privacy inspection |
| Schema-1 compatibility | PASS | Persistence/reload tests and live JSON inspection |
| Deterministic correlation keys | PASS | Automated privacy/determinism tests |
| Generic Explorer compatibility | PASS | Automated projection test and live Explorer acceptance |
| Live integration after successful System Scan | PASS | Structural ordering test and live acceptance |
| Best-effort failure isolation | PASS | Guarded integration and structural regression test |
| System Scan coexistence | PASS | Automated and manual verification |
| Deep Analysis coexistence | PASS | Automated and manual verification |
| Repeated-scan append behavior | PASS | Manual record-count verification |
| Restart persistence | PASS | Manual close/restart/reload verification |
| Search and facet usability | PASS | Automated Explorer tests and manual acceptance |
| Detail inspection | PASS | Manual acceptance |
| Privacy inspection | PASS | Automated, live, and Debug Bundle inspection |
| Regression Suite acceptance | PASS WITH WARNINGS | 14 PASS, 1 WARN, 0 FAIL |
| Debug Bundle privacy acceptance | PASS | Manual bundle inspection |
| Full solution build | PASS | 0 warnings, 0 errors |
| Full automated suite | PASS | 190 passed, 0 failed, 0 skipped |
| No unrelated release/version changes | PASS | Final Git scope audit |

## Final Acceptance Status

**SPRINT 22 — ACCEPTED**

The read-only Startup Intelligence foundation, privacy-safe Evidence projection,
live best-effort System Scan integration, schema-1 coexistence, generic Explorer
consumption, persistence, regression validation, and Debug Bundle privacy review
have been implemented and verified.

Sprint 22 introduces no startup mutation, remediation, recommendation engine,
online reputation system, schema migration, release/version change, or Sprint 23
feature work.
