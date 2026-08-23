# ForgeCare Sprint 26 Acceptance

**Release target:** v1.1-dev

**Evidence schema:** 1 (unchanged)

**Final status:** SPRINT 26 — ACCEPTED

**Acceptance basis:** 477 passing automated tests, successful solution build,
clean-checkout reproducibility, structural privacy/safety guards, repository
review, and completed live technician acceptance

## 1. Sprint Objective

Sprint 26 delivers Reporting 2.0 / Service Traceability: a professional,
offline HTML service report built from the authoritative Forge Report session
and an optional current-session Evidence snapshot.

```text
ForgeReportSession
    +
current-session Evidence snapshot
    ↓
ForgeServiceReportBuilder
    ↓
immutable ForgeServiceReportModel
    ↓
ForgeServiceReportHtmlRenderer
    ↓
professional offline HTML
    ↓
existing export, history and local-open workflow
```

The report communicates recorded diagnostic activity, technician-controlled
startup actions, execution outcome, expected state, factual verification,
recovery-reference availability, unresolved work, compact Evidence references,
and recorded session checkpoints. It does not invent causal or historical
review relationships.

## 2. Strategic Context

Sprint 20 established Evidence persistence, Sprint 21 made Evidence
inspectable, Sprints 22–23 added provenance-aware diagnostic observations,
Sprint 24 added transient explainable Attention, and Sprint 25 introduced
action-linked Startup Verification. Sprint 26 turns that persisted truth into
a professional handoff artifact without changing execution or verification.

## 3. Phase A — Reconnaissance

Phase A reviewed the live report session, legacy exporter, archive/history,
typed Startup receipts and verification, Evidence, Workflow, Safety, privacy,
and restored-session behavior. It selected a pure projection and standalone
renderer followed by narrow integration into the existing Reports workflow.

## 4. Phase B — Professional Projection

`ForgeServiceReportBuilder` accepts only a supplied detached
`ForgeReportSession` and optional supplied Evidence records. It performs no
repository access, file I/O, WPF work, Windows inspection, correlation, network
access, or mutation. The result is an immutable/read-only
`ForgeServiceReportModel`; source collections and mutable domain objects are
not retained.

## 5. Report Domain Semantics

The model separates:

- diagnostic and legacy activity;
- typed technician-controlled actions;
- execution status;
- expected state;
- verification status and observations;
- recovery-reference availability and supersession;
- unresolved work;
- compact Evidence references;
- initial/latest recorded checkpoints;
- bounded data-quality notes.

These are recorded facts. Checkpoint differences do not prove action causality.

## 6. Diagnostic / Action Separation

Stable legacy categories such as System Scan and Deep Analysis project as
diagnostic activity. Cleanup, storage cleanup and recovery retain their
bounded legacy categories. Ambiguous legacy entries remain explicitly legacy
and are not upgraded into typed technician actions by title inference.

## 7. Typed Startup Action Traceability

Startup remains the only fully typed action family. Each projected action is
derived from Sprint 25 receipts and preserves operation, per-target execution
status, expected state, execution time, verification eligibility,
recovery-reference presence and supersession. Raw `StartupChangeItem`, command,
argument, registry and path material is not reconstructed.

## 8. Verification Traceability

Verification linkage is exact by `ReceiptId + TargetId`. There is no name-only,
fuzzy, Evidence-based or application-name matching. The report distinguishes:

- Pending
- Verified
- Not Verified (`ExpectedOutcomeNotObserved`)
- Inconclusive
- Not Eligible
- Superseded

Execution and verification remain independent. An expected outcome not
observed is never represented as successful verification.

## 9. Recovery and Supersession Semantics

The report exposes only whether a bounded recovery reference exists; it does
not expose the recovery identifier. Superseding relationships retain bounded
typed receipt identity. The report does not execute recovery or assert that a
recovery operation is safe or guaranteed.

## 10. Unresolved and Pending Semantics

Pending verification, failed/not-executed actions, inconclusive observations,
expected states not observed, orphan verification and incomplete traceability
remain visible as unresolved work. Missing truth is not converted into a
successful or healthy state.

## 11. Evidence Reference Contract

Evidence is included only as compact session-scoped references containing:

- shortened technical reference;
- UTC timestamp;
- source and category;
- bounded subject;
- severity and confidence.

Observation bodies, metadata, correlation keys and full GUIDs are excluded.
Evidence references are contextual observations and do not imply that Evidence
automatically justified a technician action.

## 12. Attention Exclusion Rationale

Forge Plan Attention remains transient derived state. Reporting 2.0 does not
invoke `EvidenceCorrelationEngine`, Attention rules, the presenter or live
Forge Plan state, and does not claim that an Attention Item was reviewed,
accepted or dismissed historically.

## 13. Checkpoint and Final-State Semantics

Initial and latest session checkpoints provide recorded state context such as
health score, storage, memory and startup count. They are observations at
specific times, not proof that a reported action caused a difference.

## 14. Partial-Data Behavior

Fresh, diagnostic-only, legacy, Evidence-free, action-free and unverified
sessions produce valid reports without fabricated data. Invalid optional
records are omitted with bounded notes. An Evidence repository failure is
explicitly reported as unavailable references, not represented as an
authoritatively empty Evidence set.

## 15. Privacy Contract

The projection, renderer and live path do not newly expose usernames,
user-profile paths, startup commands, arguments, raw registry values,
disabled-storage paths, tokens, API keys, recovery identifiers, fingerprint
source material, Evidence observations/metadata/correlation keys or arbitrary
exception messages. Only the approved bounded professional fields and compact
technical references enter HTML.

## 16. Phase C — Standalone HTML Renderer

`ForgeServiceReportHtmlRenderer` accepts only the immutable report model and
returns an HTML string. It performs no file I/O, service/repository lookup,
Windows inspection, network access or mutation. It contains no JavaScript and
uses no remote fonts, scripts, styles or images.

## 17. Professional Report Structure

The offline document contains coherent report header, service session,
diagnostic activity, technician actions, verification, unresolved/attention
required, Evidence references, checkpoints/final state, data notes and footer
sections. Empty sections render truthful neutral states.

## 18. HTML Escaping and Security

All dynamic content enters encoded text contexts. Automated adversarial tests
cover script tags, image `onerror`, ampersands, angle brackets, and single and
double quotes. Dynamic values do not enter script, style, URL or unsafe
attribute contexts.

## 19. Offline, Print and Accessibility Behavior

The report is a self-contained local HTML artifact with print CSS, semantic
headings, tables and readable text labels. Browser Print / Save as PDF remains
the PDF path. No WebView, native PDF library, cloud renderer or remote asset is
required. Manual visual acceptance was successful; it is not a formal
pixel-perfect accessibility certification.

## 20. Deterministic Output

For an identical model, the renderer returns identical HTML. It injects no
export timestamp, random identifier, UI time or generated GUID into report
content. The user-selected filename may contain a bounded session reference
and export timestamp without affecting HTML determinism.

## 21. Phase D — Live Integration

The Reports screen constructs `ForgeServiceReportLiveService` with the existing
shared Evidence repository plus the approved builder and renderer. An explicit
professional export request loads Evidence, builds and renders once, then sends
the resulting HTML through the existing trusted writer/history mechanism.

## 22. Authoritative Session Identity

The detached session comes from `ForgeReportService.Snapshot()`. Its SessionId
must satisfy the existing GUID-in-N contract. No fallback identity is created.
Before writing, `ForgeReportService` confirms that the active session still
matches the generated report session.

## 23. One-Load Evidence Behavior

Each explicit professional generation request performs exactly one
`IEvidenceRepository.GetBySessionAsync` call for the authoritative SessionId.
The builder and renderer perform no repository access. No Evidence is loaded
per section or per rule, and no diagnostics or Attention are rerun.

## 24. Professional / Legacy Coexistence

The Reports UI exposes distinct professional, legacy HTML and legacy direct-to-
desktop actions. Legacy rendering remains available and retains its semantics.
Professional reporting did not silently replace the v1.0 report path.

## 25. Export and File-Write Behavior

Professional export reuses the established UTF-8 HTML writer, `.html`
extension handling, directory creation, non-empty artifact verification,
save-dialog behavior and approved local-file opening helper. No native PDF,
cloud export, external rendering service or new temp-directory strategy was
added.

## 26. Archive / History Compatibility

`ForgeReportArchiveEntry.ReportKind` distinguishes `Professional` and `Legacy`
artifacts in existing history. Missing `ReportKind` in older archive JSON
defaults to Legacy. No separate archive database or persisted professional
projection was introduced.

## 27. Session-Change Isolation

If report generation begins for session A and the active report changes to
session B before export, the professional artifact/history write is rejected.
No stale history entry is created and session B remains unchanged.

## 28. Failure Isolation and Logging

Invalid session, generation and write failures use bounded technician-facing
messages. Evidence-load failure can produce a truthful report marked
`GENERATED WITH DATA NOTES`. Logging contains only bounded stage, sanitized
result/exception type and fixed subsystem context; HTML, Evidence, metadata,
paths and raw exception messages are not logged.

## 29. Workflow / Verification Regression Boundary

Generating or exporting a professional report does not mark VERIFY complete,
create receipts or verification results, mark actions executed, alter Forge
Plan, change Workflow, reevaluate startup state or modify recovery state.
Sprint 25 remains authoritative.

## 30. Safety Contract

Reporting 2.0 is observational except for the explicit requested HTML artifact
and existing report-history entry. It does not invoke startup/registry/service
mutation, cleanup, process control, scanners/analyzers, installers, elevation,
network clients, Evidence writes, verification reevaluation or automatic
diagnostics. The existing local HTML opener remains the only approved shell
boundary in this flow.

## 31. Automated Verification

Final repository verification on 2026-08-23:

- Restore: PASS; all projects up to date.
- Build: PASS; 0 errors.
- Automated tests: PASS — 477 passed, 0 failed, 0 skipped.
- `git diff --check`: PASS.
- Clean-checkout intended-scope build/test: PASS.

The build reported three pre-existing nullable warnings:

- `UpdateDiscoveryService.cs(76)` — CS8602
- `RemoteUpdateDiscoveryService.cs(46)` — CS8604
- `RemoteUpdateDiscoveryService.cs(164)` — CS8602

No Sprint 26 compiler warning was introduced.

Automated coverage includes immutable projection, diagnostic/action separation,
typed receipt and exact verification linkage, unresolved states, compact
Evidence projection, legacy/partial behavior, deterministic rendering,
adversarial HTML escaping, privacy/safety boundaries, one-load live composition,
Evidence failure, cancellation, session-change isolation, file writing, archive
kind/backward compatibility and legacy export coexistence.

## 32. Manual Acceptance

User-verified manual acceptance: **PASS**.

- ForgeCare launched normally.
- Fresh/low-data and diagnostic professional reports rendered successfully.
- Diagnostic activity and technician actions were visually distinct.
- A Startup action showed execution and Pending verification before the later
  explicit System Scan.
- A later report reflected persisted verification truth with execution and
  verification visibly separate.
- Compact Evidence references rendered without observation bodies or metadata
  and without implied action justification.
- Professional and Legacy exports, history and open behavior worked.
- Restart/restored-session generation worked.
- Exported HTML privacy inspection was green: no raw user-profile paths,
  command-line material, API-key/token-like content or raw registry values were
  observed.
- No system/startup mutation, diagnostic rerun or Attention rebuild occurred.
- Overall live layout and professional presentation were accepted.

No screenshot was retained; this is not an acceptance blocker.

## 33. Regression Suite Result

User-reported in-application Regression Suite: **PASS**. Exact PASS/WARN/FAIL
counts were not supplied and are not inferred.

## 34. Debug Bundle / Privacy Result

User-reported Debug Bundle/privacy review: **PASS**. Reporting 2.0 did not add
professional HTML to Debug Bundle scope or weaken existing diagnostic privacy
boundaries.

## 35. Known Limitations

1. Startup is the only fully typed and verified action family.
2. Cleanup, storage and other action families remain legacy/unverified activity.
3. There is no persisted historical Attention review state.
4. There is no generic Finding-to-Action linkage.
5. Evidence references are session observations, not action justification.
6. There is no native PDF renderer; browser Print / Save as PDF is the path.
7. There are no customer, technician or ticket identity fields.
8. Evidence-load failures produce explicit data notes in otherwise valid partial reports.
9. Old sessions do not gain fabricated Verification 2.0 truth.
10. Legacy reporting remains available during acceptance/release evaluation.
11. The professional projection is transient and rebuilt from persisted truth.
12. There is no cloud reporting or telemetry.
13. Manual visual acceptance exists, but no screenshot was retained.
14. Pre-existing update-discovery nullable warnings remain outside Sprint 26 scope.

## 36. Explicit Non-Goals

Sprint 26 did not add native PDF or Office export, cloud reporting, telemetry,
AI-written narrative, a customer portal, electronic signatures, a new Evidence
source/rule, another Verification action family, persisted Attention review,
fuzzy finding/action matching, a report database, report-driven mutation, a
product version change, release/installer redesign or Sprint 27 work.

## 37. Definition of Done

| Requirement | Status | Verification |
|---|---|---|
| Immutable professional projection | PASS | Automated projection/immutability tests |
| Diagnostics and actions distinguished | PASS | Automated tests and manual report review |
| Typed Startup actions and execution status | PASS | Builder/traceability tests |
| Verification independent from execution | PASS | Exact-linkage tests and manual acceptance |
| Pending, unresolved and inconclusive truth visible | PASS | Builder/renderer tests |
| Recovery-reference and supersession semantics factual | PASS | Traceability tests |
| Compact Evidence references | PASS | Projection, renderer and privacy tests |
| No Evidence-to-Action claim | PASS | Domain review and manual acceptance |
| No historical Attention fabrication | PASS | Structural safety review |
| Checkpoints do not claim causality | PASS | Projection semantics review |
| Offline deterministic renderer | PASS | Determinism and structural tests |
| Dynamic HTML safely escaped | PASS | Adversarial escaping tests |
| Empty, partial and legacy reports render | PASS | Automated and manual acceptance |
| One Evidence load per live request | PASS | Live-service tests |
| Authoritative session identity | PASS | Live integration tests |
| Stale-session export rejected | PASS | Export integration test |
| Existing writer/history reused | PASS | Export integration test |
| Professional/Legacy history distinction | PASS | Archive compatibility and UI tests |
| No Workflow/Verification mutation | PASS | Structural review and manual acceptance |
| Privacy and safety boundaries | PASS | Automated guards and manual inspection |
| Build and automated suite green | PASS | 477 passed; build 0 errors |
| Manual acceptance | PASS | User-supplied live verification |
| Regression Suite | PASS | User-reported |
| Debug Bundle/privacy | PASS | User-reported |
| Clean-checkout reproducibility | PASS | Isolated intended-scope worktree build/test |
| Complete repository inventory | PASS | Explicit B/C/D inventory and Git audit |

## 38. Final Acceptance Status

**SPRINT 26 — ACCEPTED**

Sprint 26 provides a truthful, privacy-conscious, deterministic professional
service report and integrates it into ForgeCare's existing explicit export,
history and local-open workflow while preserving legacy reports and all
technician-controlled safety boundaries.
