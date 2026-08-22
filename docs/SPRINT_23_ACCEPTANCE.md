# ForgeCare Sprint 23 Acceptance

**Release target:** v1.1-dev

**Evidence schema:** 1

**Final status:** SPRINT 23 — ACCEPTED

**Acceptance basis:** automated verification, structural safety guards, and completed live technician acceptance

## 1. Sprint Objective

Sprint 23 adds read-only Process Intelligence to ForgeCare. It enriches the
complete process observations already collected during Deep Analysis, groups
related instances into application-level observations, records privacy-safe
Process Evidence, and exposes it through the existing generic Evidence
Explorer.

The feature is additive. The existing Deep Analysis result, Analysis UI,
reporting, Resource History, workflow, baseline behavior, `TopProcesses`, and
Deep Analysis Evidence remain authoritative.

## 2. Scope

Sprint 23 delivered:

- an immutable process-observation contract without live `Process` references;
- local executable provenance inspection using the established offline
  Authenticode boundary;
- deterministic application identity, grouping, classification, and confidence;
- privacy-safe schema-1 Process Evidence;
- best-effort live integration after successful Deep Analysis;
- generic Evidence Explorer, persistence, Regression Suite, and Debug Bundle
  compatibility.

It did not add process control, remediation, continuous monitoring, a new
Analysis-page UI, or a new Evidence document shape.

## 3. Architecture Delivered

```text
ResourceAnalyzerService.AnalyzeAsync
    ↓
ResourceAnalysisResult
    ├── existing TopProcesses contract
    └── immutable complete ProcessObservations
    ↓
existing Analysis UI / Forge Report / Resource History / workflow
    ↓
existing successful Deep Analysis state
    ↓
existing DeepAnalysis Evidence
    ↓
ProcessIntelligenceService
    ├── IProcessExecutableInspector
    ├── WindowsProcessExecutableInspector
    ├── Startup file/signature inspection primitives
    ├── ProcessClassificationPolicy
    └── deterministic application aggregation
    ↓
ProcessIntelligenceResult
    ↓
ProcessIntelligenceEvidenceAdapter
    ↓
shared EvidenceService / schema-1 JsonEvidenceRepository
    ↓
generic Evidence Explorer
```

Process Intelligence runs only after the existing successful Deep Analysis
workflow and existing Deep Analysis Evidence capture. It is guarded and
best-effort: its failure cannot retroactively invalidate a successful Deep
Analysis.

## 4. Phase B — Process Intelligence Foundation

Phase B introduced an isolated, read-only Process Intelligence foundation. It
consumes constructed `ProcessInstanceObservation` values and produces immutable
`ProcessIntelligenceResult` data.

The foundation provides:

- strong identity from normalized executable path where available;
- isolated provisional identity for pathless or inaccessible processes;
- same-path aggregation and same-name/different-path separation;
- cached executable inspection for duplicate targets;
- bounded asynchronous inspection concurrency;
- deterministic ordering and grouping;
- local file metadata and offline Authenticode provenance;
- explicit provenance classification and independent confidence;
- partial-success and cancellation behavior.

It does not enumerate processes, reacquire PIDs, inspect command lines or
owners, execute commands, or change the system.

## 5. Phase C — Process Evidence Projection

Phase C added `EvidenceSource.ProcessIntelligence` and a pure
`ProcessIntelligenceEvidenceAdapter`. It maps valid application aggregates to
privacy-safe `EvidenceRecord` values.

Typical record semantics are:

- Category: `Process`
- Source: `ProcessIntelligence`
- Collector: `ProcessIntelligenceEvidenceAdapter`
- Subject: application-oriented process identity
- Severity: conservative mapping from observed resource pressure
- Confidence: copied from Process Intelligence provenance confidence
- Correlation: deterministic, hashed, privacy-safe application identity
- Metadata: bounded resource and provenance fields

The adapter does not inspect Windows, mutate analysis results, or write the
repository directly. Valid aggregates survive unrelated projection failures.

## 6. Phase D — Live Integration

Phase D connected the approved foundation and adapter to the successful live
Deep Analysis path.

The accepted execution order is:

1. `ResourceAnalyzerService.AnalyzeAsync` completes.
2. Existing result state and Analysis UI are updated.
3. Existing Forge Report and Resource History recording completes.
4. Existing workflow, baseline, and success behavior completes.
5. Existing Deep Analysis Evidence is captured.
6. Process Intelligence analyzes the completed process observations.
7. Valid Process Intelligence Evidence is appended through the shared
   `EvidenceService` and `JsonEvidenceRepository`.

The helper uses `ForgeReportService.Snapshot().SessionId`, validates the
established GUID-in-`N` session contract, and uses
`result.AnalysisTime.ToUniversalTime()` as the observation timestamp. Empty
observations and zero translated records are safe no-ops. Partial results are
preserved. Validation, analysis, adapter, persistence, cancellation, and
unexpected failures are logged in bounded form and do not enter the successful
Deep Analysis failure path.

## 7. Complete Process Observation Surface

`ResourceAnalysisResult.ProcessObservations` is an immutable/read-only snapshot
of all successfully inspected second-sample process observations, not only the
selected `TopProcesses` subset.

An observation may transiently contain:

- PID and process name;
- optional UTC process start time;
- optional executable path;
- CPU percentage;
- working-set MB and memory percentage;
- pressure score and level;
- primary resource classification.

The collection contains no live `Process` references. Assignment defensively
copies the input, the default is empty, and the collection is excluded from
unrelated JSON serialization. `TopProcesses` remains the established UI and
report contract.

## 8. CPU Baseline / PID Reuse Correctness Fix

Sprint 23 identified a narrow analyzer correctness risk: the original first CPU
sample was keyed only by PID. A process missing from the first sample, or a PID
reused between samples, could compare lifetime CPU against zero or a different
process lifetime and create a false spike.

The final conservative calculation is:

- valid first baseline plus matching PID and start time: calculate the delta;
- missing baseline: `0%` CPU;
- mismatched start time / reused PID: `0%` CPU;
- unavailable start time: `0%` CPU;
- negative delta: `0%` CPU;
- valid samples retain the existing clamping and rounding semantics.

Memory and other process observations remain available when CPU identity cannot
be matched defensibly. This is a correctness safeguard, not a sampling redesign.

## 9. Application Aggregation Semantics

Strong executable identity groups multiple observations for the same normalized
executable. Same-name processes with different strong paths remain separate.
Pathless or inaccessible processes receive isolated provisional identities and
are not broadly merged by name.

For an application group:

- `TotalCpuPercent` is the sum of member CPU logical-capacity shares from the
  same observation window and may exceed `100%`;
- `TotalMemoryMb` is the sum of current member working-set values;
- `MaximumInstanceCpuPercent` is the largest observed member CPU value;
- `MaximumInstanceMemoryMb` is the largest observed member working set;
- pressure retains ForgeCare's existing resource-ranking meaning.

CPU total is not a historical average or per-core utilization. Working-set
total is not private bytes, unique physical ownership, commit charge, a
historical average, or a historical peak.

## 10. Executable Provenance / Offline Authenticode

Executable path and optional start time are captured only from the same current
second-sample `Process` instance when accessible. There is no post-analysis
`Process.GetProcesses`, `Process.GetProcessById`, path search, or executable
guessing. Protected, inaccessible, or exiting processes may remain pathless and
receive provisional identity.

Process provenance reuses the Sprint 22 local/offline Authenticode architecture:

- Windows trust UI is disabled;
- revocation/network retrieval is not required;
- cache-only URL retrieval is configured;
- no PowerShell subprocess, cloud API, online reputation service, or required
  online certificate retrieval is introduced.

A `Valid` signature means valid under the local/offline Windows Authenticode
evaluation available at inspection time. It does not mean malware-free,
necessary, recommended, harmless, or currently globally unrevoked. This
acceptance does not claim packet-level proof of zero network activity elsewhere
in ForgeCare or Windows.

## 11. Classification and Confidence Semantics

The Sprint 23 provenance taxonomy is:

- `Verified`
- `Known`
- `Unverified`
- `Unknown`

Classification represents the available support for executable/application
provenance. It does not mean safe or unsafe, malicious or benign, necessary or
unnecessary, or recommended or not recommended.

Confidence represents certainty in the provenance classification. Evidence
Severity represents observed resource significance. Existing pressure,
provenance classification, Evidence severity, and confidence remain separate
dimensions.

## 12. Resource Metric Semantics

Process Intelligence preserves the existing analysis observation window and
resource values. It does not infer causality, necessity, historical behavior,
or remediation advice from those measurements.

- CPU values are sampled logical-capacity shares using the existing two-pass
  analyzer.
- Working-set values are point-in-time memory observations.
- Aggregate values describe only the grouped members present in that completed
  observation surface.
- Pressure is the existing ForgeCare ranking signal, not provenance or trust.

## 13. Evidence Mapping

One Process Intelligence record represents one application aggregate for one
completed Deep Analysis observation. The record preserves bounded structured
measurements and generic provenance metadata without requiring a process-specific
Explorer UI.

Repeated analyses append distinct records. Correlation keys support grouping
and investigation but are not unique identifiers and do not cause
deduplication, replacement, or upsert behavior.

## 14. Schema Compatibility

Sprint 23 retains `EvidenceDocument.SchemaVersion = 1`. It adds the string enum
member `EvidenceSource.ProcessIntelligence` without changing the document shape,
per-session repository, atomic persistence behavior, or query contract.

All four current sources coexist in one session document:

- `SystemScan`
- `StartupIntelligence`
- `DeepAnalysis`
- `ProcessIntelligence`

Older ForgeCare builds lacking newer enum members may not deserialize schema-1
documents containing those source strings. This is an application-version
compatibility caveat, not a schema-shape migration.

## 15. Evidence Explorer Compatibility

No Process-specific Evidence Explorer production UI was required. The generic
Explorer supports:

- the Process category and Process Intelligence source facets;
- application subjects and generic metadata;
- provenance/classification, company/product, signature, and pressure search;
- correlation display;
- independent severity and confidence presentation.

Live acceptance verified these behaviors with real Process Intelligence data.

## 16. Privacy Contract

Process Intelligence deliberately excludes collection and persistence of:

- process command lines and arguments;
- process owner/account and SID;
- arbitrary environment values;
- document paths;
- certificate binary data;
- raw trust exception messages.

Full executable paths may exist transiently during local analysis. Persisted
Process Evidence passes through `ProcessEvidencePathProjector`; `MainWindow`
does not manually create executable-path Evidence metadata. Accepted persisted
forms include environment-root substitutions such as `%PROGRAMFILES(X86)%`,
`%LOCALAPPDATA%`, `%APPDATA%`, and bounded `%USERPROFILE%` identities, plus
`<redacted>` or `<custom-root>` forms where appropriate.

Correlation keys are deterministic and hashed. They do not expose raw path,
command-line, account, SID, session, or timestamp data. This is diagnostic-data
minimization and redaction, not a claim of cryptographic secrecy.

## 17. Safety / Read-Only Contract

Process Intelligence is observational. Sprint 23 adds no functionality to:

- launch, terminate, suspend, or resume processes;
- change priority or processor affinity;
- mutate services or startup state;
- write registry state;
- modify executable files;
- elevate privileges;
- invoke installer or remediation workflows.

The existing analyzer performs its normal process enumeration only. Process
Intelligence adds no extra process enumeration and no PID reacquisition.
Structural safety tests guard the new foundation, Evidence adapter, and live
integration path. These guards complement, rather than replace, live safety and
privacy acceptance.

## 18. Automated Verification Result

Final closeout verification on the `v1.1-dev` working tree:

| Verification | Result |
|---|---|
| Restore | PASS — all projects up to date |
| Build | PASS — 0 errors, 3 warnings |
| Automated tests | PASS — 260 passed, 0 failed, 0 skipped |
| `git diff --check` before documentation | PASS |

The three build warnings are pre-existing nullable-analysis warnings outside
Sprint 23 scope:

- `UpdateDiscoveryService.cs(76)` — `CS8602`
- `RemoteUpdateDiscoveryService.cs(46)` — `CS8604`
- `RemoteUpdateDiscoveryService.cs(164)` — `CS8602`

Automated coverage includes:

- immutable observation snapshotting and JSON exclusion;
- strong and provisional process identity;
- same-path grouping and same-name/different-path separation;
- duplicate-target inspection caching;
- bounded concurrency, partial success, input validation, and cancellation;
- deterministic classification and independent confidence;
- CPU and working-set aggregation;
- missing-baseline, unavailable-start-time, PID-reuse, negative-delta, clamping,
  and rounding behavior;
- local/offline provenance inspection boundaries;
- privacy-safe path projection and hashed correlation keys;
- raw command-line/account/SID exclusion;
- schema-1 persistence and four-source coexistence;
- generic Evidence Explorer compatibility;
- live Deep Analysis ordering, append semantics, shared persistence, failure
  isolation, and targeted structural safety boundaries.

Structural tests establish regression guards over source and dependency paths;
they do not claim to be full interactive WPF or operating-system proofs.

## 19. Manual Acceptance Result

The supplied completed technician acceptance is PASS for:

- launching the development build and creating a fresh Forge Report session;
- normal System Scan and Deep Analysis completion;
- preserved Analysis UI behavior;
- Process Intelligence execution after Deep Analysis;
- Process Intelligence visibility in the generic Evidence Explorer;
- Process category and Process Intelligence source facets;
- coexistence with System Scan, Startup Intelligence, and Deep Analysis records;
- aggregate records, generic detail metadata, correlation data, executable
  provenance, signature information, and privacy-safe identity;
- separate severity and confidence presentation.

No additional manual behavior beyond the supplied acceptance evidence is
claimed.

## 20. Multi-Process Copilot Acceptance Example

The live run verified a real five-instance Copilot application group:

| Field | Observed value |
|---|---|
| Application | Copilot |
| Classification | Verified |
| Company | Microsoft Corporation |
| Executable | `mscopilot.exe` |
| Identity strength | Strong |
| Instance count | 5 |
| Maximum instance CPU | 0.1 |
| Maximum instance memory | 129 MB |
| Maximum pressure score | 3 |
| Member PID count | 5 |
| Member PIDs | 10108, 17932, 19360, 31192, 32380 |
| Normalized executable path | `%PROGRAMFILES(X86)%\Microsoft\Copilot\Application\mscopilot.exe` |
| Pressure | MINIMAL |
| Product | Copilot |
| Signature | Valid |
| Signer | Microsoft Corporation |
| Total CPU | 0.1 |
| Total working-set memory | 259 MB |

This is live proof of aggregation by strong executable identity. The listed
PIDs are transient acceptance observations, not stable product identifiers.

## 21. Repeated-Analysis Count Result

Before a repeated Deep Analysis in the same report session:

| Source | Records |
|---|---:|
| Total | 108 |
| DeepAnalysis | 18 |
| ProcessIntelligence | 69 |
| StartupIntelligence | 15 |
| SystemScan | 6 |

After the second Deep Analysis and Explorer refresh:

| Source | Records | Delta |
|---|---:|---:|
| Total | 195 | +87 |
| DeepAnalysis | 36 | +18 |
| ProcessIntelligence | 138 | +69 |
| StartupIntelligence | 15 | 0 |
| SystemScan | 6 | 0 |

PASS: Deep Analysis and Process Intelligence observations appended, prior
records remained, unrelated sources were unchanged, matching correlation keys
did not replace observations, and the Explorer continued to load normally.

## 22. Restart Persistence Result

PASS: ForgeCare was closed normally and reopened through the development build.
The application's normal session behavior reconnected to the existing Forge
Report session, and the previously persisted Evidence remained readable in the
Explorer. No additional session-recovery capability is claimed.

## 23. Regression Suite Result

The in-application Regression Suite result was:

- PASS: 14
- WARN: 1
- FAIL: 0
- Overall: **PASS WITH WARNINGS**

The warning was non-blocking in the supplied acceptance run. It is recorded as
a warning and is not relabelled as a plain pass.

## 24. Debug Bundle Result

PASS: a normal Debug Bundle included the same schema-1 Evidence document seen
by the Explorer after the repeated analysis:

- Total: 195
- ProcessIntelligence: 138
- DeepAnalysis: 36
- StartupIntelligence: 15
- SystemScan: 6

Manual privacy inspection found no clear-text `C:\Users\<username>\...` paths,
command-line fields, command arguments, token-style arguments, API-key-like
metadata fields, or SID-like user identity values. The Debug Bundle copies the
persisted Evidence privacy surface; it does not introduce a separate Process
Intelligence export path.

## 25. Known Limitations

1. The analyzer retains its existing two-pass 900 ms sampling architecture.
2. First/second enumeration threading was not broadly redesigned.
3. Inaccessible process start time yields conservative `0%` CPU.
4. Inaccessible executable path yields provisional identity.
5. Protected or exiting processes may produce incomplete observations or be
   omitted under existing analyzer behavior.
6. Parent-process identity is not collected.
7. Command lines are not inspected.
8. Owner/account identity is not inspected.
9. Continuous monitoring is not provided.
10. The Analysis page has no application-aggregation UI.
11. Evidence observations append and are not deduplicated by correlation key.
12. Offline Authenticode does not provide fresh online revocation or reputation.
13. Older builds may reject newer string enum source values.
14. No full WPF end-to-end automation covers the complete live workflow.
15. The existing three update-discovery nullable warnings remain outside Sprint
    23 scope.

## 26. Explicit Non-Goals

Sprint 23 intentionally does not implement:

- process termination or suspension;
- priority or affinity management;
- malware detection or network reputation;
- a parent-process graph;
- command-line, owner, or SID collection;
- continuous process monitoring or kernel/ETW telemetry;
- Analysis-page application grouping UI;
- Resource History migration;
- Evidence schema 2;
- Evidence deduplication or upsert semantics;
- remediation or recommendation engine changes.

## 27. Sprint 23 Definition of Done

| Requirement | Status | Evidence |
|---|---|---|
| Immutable process observations | PASS | Defensive read-only `ProcessObservations` snapshot |
| No live `Process` references in result | PASS | Value-only observation model and tests |
| Strong executable identity | PASS | Normalized path identity and live Copilot grouping |
| Isolated provisional identity | PASS | Pathless/inaccessible observations remain isolated |
| Cached executable inspection | PASS | Duplicate-target cache tests |
| Offline Authenticode reuse | PASS | Existing local/cache-only inspector boundary |
| Deterministic provenance classification | PASS | Policy tests |
| Independent confidence | PASS | Model/policy/Evidence tests |
| Strong-path application aggregation | PASS | Aggregation tests and live Copilot example |
| Same-name/different-path separation | PASS | Service tests |
| CPU and working-set aggregation | PASS | Aggregate tests and documented semantics |
| Bounded concurrency | PASS | Maximum-concurrency test |
| Partial success | PASS | Service and adapter tests |
| Cancellation support | PASS | Foundation cancellation test |
| No command-line collection | PASS | Contract and safety/privacy guards |
| No account/SID collection | PASS | Contract and safety/privacy guards |
| Privacy-safe Process Evidence | PASS | Projection/privacy tests and bundle inspection |
| Hashed correlation | PASS | Correlation-key tests |
| Schema-1 persistence | PASS | Vertical-slice tests and live document |
| Four Evidence sources coexist | PASS | Automated and live acceptance |
| Generic Explorer compatibility | PASS | Presentation test and live acceptance |
| Live integration after Deep Analysis | PASS | Ordering test and live acceptance |
| No extra process enumeration | PASS | Targeted structural guard |
| No PID reacquisition | PASS | Targeted structural guard |
| Missing-baseline CPU safeguard | PASS | Calculator tests |
| PID-reuse safeguard | PASS | Start-time identity tests |
| Existing `TopProcesses` preserved | PASS | Analyzer contract tests and live Analysis acceptance |
| Existing DeepAnalysis Evidence preserved | PASS | Integration tests and live counts |
| Repeated-analysis append semantics | PASS | Automated repository test and live count delta |
| Restart persistence | PASS | Manual acceptance |
| Regression Suite | PASS WITH WARNINGS | 14 PASS, 1 WARN, 0 FAIL |
| Debug Bundle inclusion | PASS | Manual bundle inspection |
| Privacy acceptance | PASS | Live and bundled Evidence inspection |
| No system-changing Process action | PASS | Architecture, structural guards, and live review |
| Automated suite green | PASS | 260 passed, 0 failed, 0 skipped |
| Final build green | PASS | 0 errors; 3 pre-existing warnings |
| No unrelated version/release changes | PASS | Final repository scope audit |

## 28. Final Acceptance Status

**SPRINT 23 — ACCEPTED**

Sprint 23 satisfies its approved Process Intelligence and Process Evidence
scope. Automated verification, structural guards, supplied live acceptance,
restart persistence, Regression Suite validation, and Debug Bundle inspection
are complete. Known limitations and compatibility caveats are explicitly
recorded and do not block acceptance.
