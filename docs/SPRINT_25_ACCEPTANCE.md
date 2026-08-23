# ForgeCare Sprint 25 Acceptance

**Release target:** v1.1-dev

**Evidence schema:** 1 (unchanged)

**Final status:** SPRINT 25 — ACCEPTED

**Acceptance basis:** 403 passing automated tests, successful solution build,
structural safety/privacy guards, repository review, and completed live
technician acceptance

## 1. Sprint Objective

Sprint 25 introduces Verification 2.0's first action-linked proof slice. It
distinguishes an executed startup operation from a later observation that the
intended startup configuration state exists.

```text
technician-controlled startup mutation
    ↓
privacy-safe per-target action receipt
    ↓
ForgeReportSession persistence
    ↓
later explicit System Scan
    ↓
source-complete startup observation
    ↓
StartupVerificationEvaluator
    ↓
persisted StartupVerificationResult
    ↓
truthful Workflow verification state
```

Verification proves configuration state only. It does not claim that the
startup change caused a performance improvement.

## 2. Strategic Motivation

Earlier workflow state could treat diagnostic activity and repeated scans as
if technician work had been executed and verified. Sprint 25 adds typed action
and verification facts so Workflow can distinguish observation, execution,
and later proof without interpreting legacy activity text.

## 3. Phase A — Reconnaissance

Phase A inspected the startup mutation, undo, report-session, System Scan,
Workflow, privacy, and safety boundaries. It selected startup disable/restore
as the smallest action family with deterministic configuration-state proof.

## 4. Phase B — Verification Domain

Phase B introduced immutable/read-only domain contracts for startup target
identity, action receipts, expected state, source observations, observation
snapshots, and verification results. The pure evaluator performs no scanner,
registry, filesystem, mutation, Evidence, network, or UI work.

## 5. Configuration-Target Identity

Verification identity is based on the exact startup source, bounded locator,
and optional configuration fingerprint. It does not reuse Sprint 24
application `EntityKey` identity and does not fall back to application names.

Supported target kinds are:

- HKCU Run
- Current-user Startup folder

Source remains part of identity, so the same name under different sources is
not treated as one configuration target.

## 6. Fingerprint and Privacy Model

Configuration values are normalized transiently and represented by lowercase
SHA-256 fingerprints. Locator and target IDs are deterministic, bounded, and
privacy-safe. Domain and persistence objects do not retain raw commands,
arguments, registry data, full user paths, usernames, or secrets.

Adversarial automated coverage exercises user-profile paths and token-like
values and verifies that sensitive source material does not survive in the
serialized receipt, observation, or verification result.

## 7. Verification State Model

- **Verified:** the later complete observation matches the expected state.
- **ExpectedOutcomeNotObserved:** a complete later observation factually shows
  that the expected state was not present.
- **Inconclusive:** completeness, identity, ordering, or configuration is not
  sufficient for a conclusion.
- **NotEligible:** the action/target/session cannot participate in this proof
  slice.

Execution outcome and verification state remain separate concepts.
`ExpectedOutcomeNotObserved` is terminal factual evaluation, not a successful
expected outcome.

## 8. Source Completeness Semantics

Supported source observations retain one of:

- Completed
- Failed
- Unavailable
- Unknown

An empty `Completed` source may prove absence. An empty Failed, Unavailable, or
Unknown source cannot. Duplicate locators, projection failures, or duplicate
source results are treated conservatively as ambiguous/unknown rather than as
proof of absence.

## 9. Phase C — Receipt and Action Linkage

Phase C connects actual technician-controlled startup outcomes to the domain.
`StartupActionReceiptBuilder` creates per-target receipt items from the real
mutation result and reviewed/undo context. A best-effort integration boundary
records receipts without changing the authoritative mutation result.

## 10. ForgeReportSession Persistence

`StartupActionReceipts` and `StartupVerificationResults` are additive optional
fields in the existing Forge Report session. No separate verification store
was created. Missing optional collections load as empty, while malformed or
conflicting optional entries are rejected without manufacturing proof or
discarding valid legacy report state.

Identical receipt/result identities are idempotent. Conflicting duplicates are
rejected deterministically. IDs and results survive normal session restart.

## 11. Old-Session Compatibility

Pre-Sprint-25 sessions load with empty receipt/result collections and preserve
their existing actions and checkpoints. No receipt is reconstructed from old
`ForgeReportAction` text, and old scans do not fabricate pending verification.

Older builds may ignore unknown optional fields and could discard them if they
load and resave a newer session. This is an application-version downgrade
caveat, not a report schema migration.

## 12. Receipt Eligibility and Outcomes

Eligibility is retained per target:

- Executed + supported source → eligible.
- DryRun, Failed, Blocked, Skipped, or unsupported source → not eligible.

Batch aggregate counts never substitute for per-target truth.

## 13. Disable Receipt Flow

The Startup Review disable path records the existing startup change first,
then best-effort creates a privacy-safe receipt from the actual per-target
outcomes and current undo records. Receipt failure does not rerun, roll back,
or invalidate the mutation.

## 14. Restore Receipt Flow

Both the supported Startup Review restore path and Safety Center restore path
snapshot the original undo records, execute the established restore, record
the existing report action, and then best-effort create restore receipts.

## 15. Undo and Recovery Linkage

New undo records receive stable GUID-in-N IDs. Receipt items use bounded
`undo:<id>` references where available. A deterministic bounded legacy
reference supports older undo records without persisting disabled paths or raw
configuration data. Startup undo/mutation semantics themselves are unchanged.

## 16. Phase D — Source-Complete Scanning

`StartupScanner` still reads the existing four discovery source families and
retains the existing UI-facing deduplicated `StartupItems`. It now also returns
per-source read outcomes and source-specific items. `SystemScanner` carries
that diagnostic completeness surface in `SystemSnapshot` without changing
Dashboard, recommendations, Optimize, Startup Intelligence, or Evidence.

Only HKCU Run and current-user Startup folder are eligible for live
verification. Machine-wide sources remain observational and unsupported for
mutation proof.

## 17. Observation Snapshot

After a successful explicit System Scan,
`StartupObservationSnapshotBuilder` creates an immutable observation using:

- the active `ForgeReportService.Snapshot().SessionId`;
- the System Scan timestamp converted to UTC;
- exact supported-source completeness;
- privacy-safe target identities built from the scan's existing data.

It retains no mutable `StartupItem` reference and no raw command/path value.

## 18. Live Disable Verification

For an eligible executed disable:

- Later completed exact source + locator absent + no ambiguity → Verified.
- Later completed exact source + locator present →
  ExpectedOutcomeNotObserved.
- Incomplete source, ambiguity, conflict, or invalid time ordering →
  Inconclusive.

## 19. Live Restore Verification

For an eligible executed restore:

- Later completed exact source + locator present + matching fingerprint →
  Verified.
- Later completed exact source + locator absent or changed fingerprint →
  ExpectedOutcomeNotObserved.
- Incomplete source, ambiguity, missing required fingerprint, or invalid time
  ordering → Inconclusive.

No name-only, cross-source, or application-identity fallback exists.

## 20. Supersession Behavior

A later opposite operation supersedes an older target only when its execution
timestamp is later and its exact locator matches. Supersession is per target:
a restore of one target does not suppress unrelated targets from an earlier
disable batch.

## 21. Result Persistence

Only actual evaluator results are recorded through
`ForgeReportService.RecordStartupVerificationResult`. Verified,
ExpectedOutcomeNotObserved, and NotEligible are terminal for pending selection;
Inconclusive remains pending for a later explicit observation. Distinct scan
observations append; identical identities are not duplicated.

## 22. Workflow Correction

Workflow no longer uses generic diagnostic action count or two scans alone as
proof of executed work. Eligible executed startup receipts establish typed
startup work. VERIFY completes only when all currently relevant eligible
startup targets have a terminal factual result of Verified or
ExpectedOutcomeNotObserved. Inconclusive remains pending.

Recognized existing Cleanup and Storage Cleanup work retain their legacy
checkpoint-based behavior so Sprint 25 does not break those action families.
Action-linked proof for them is deferred.

## 23. Restart and Session Behavior

Automated and manual acceptance establish:

```text
action → receipt persisted → application restart → session restored
       → later System Scan → pending receipt evaluated → result persisted
```

No fallback session ID is generated and no historical action-text inference is
performed.

## 24. Failure Isolation

Receipt creation and result persistence are best-effort additions. Receipt
failure cannot invalidate or repeat an already-completed mutation.
Verification projection, query, evaluation, or persistence failure cannot
invalidate a successful System Scan, its UI, report checkpoint, or Evidence.
Per-receipt/per-result handling allows other valid work to continue.

## 25. Privacy Contract

Sprint 25 report/session state and verification diagnostics exclude:

- raw startup commands and arguments;
- raw registry value data;
- full user Startup-folder or disabled-storage paths;
- usernames, accounts, and SIDs;
- tokens, API keys, and arbitrary secret-like values;
- raw exception bodies containing machine paths.

Only bounded display names, source tokens, deterministic IDs, hashes,
timestamps, state vocabulary, and factual bounded rationale are retained.

## 26. CrashLog Privacy-Safe Logging

Phase D adds the narrow `CrashLogService.RecordPrivacySafe` entry point because
the general crash record intentionally contains broader machine/user context.
The verification path records only UTC time, bounded subsystem context, and
exception type. It omits username, machine details, exception body, stack, and
path-heavy text.

## 27. Read-Only Verification and Safety Contract

Verification consumes the completed `SystemSnapshot` only. It contains no call
path to startup mutation, registry writes, file write/move/delete, process
start/termination, service mutation, installer/elevation, Evidence writes,
network clients, or automatic scanner reruns.

The original startup disable/restore remains explicit, reviewed,
technician-controlled work with existing Dry Run, confirmation, Safety Journal,
and undo behavior.

## 28. Evidence, Forge Plan, and Reporting Boundaries

Sprint 25 does not change Evidence schema 1, Evidence repositories, Evidence
Explorer, Startup/Process Intelligence, or Forge Plan Attention/correlation.
No new Evidence source was added.

Typed receipts/results now exist for future Reporting 2.0 traceability, but
Sprint 25 does not change report HTML, report UX, export layout, or
customer-facing verification presentation.

## 29. Automated Verification

Final verification on the Sprint 25 working tree:

- `dotnet restore .\ForgeCare.slnx` — PASS
- `dotnet build .\ForgeCare.slnx --no-restore` — PASS
- Build warnings: 0
- Build errors: 0
- `dotnet test .\ForgeCare.slnx --no-build --no-restore` — PASS
- Tests passed: 403
- Tests failed: 0
- Tests skipped: 0
- `git diff --check` — PASS

Automated coverage includes target identity, fingerprints, evaluator states,
immutability, privacy, receipt construction, per-target outcomes, old-session
loading, duplicate/conflict behavior, live source completeness, observation
projection, cross-source retention, disable/restore verification,
supersession, pending behavior, persistence/restart, failure isolation,
Workflow semantics, and structural read-only guards.

## 30. Manual Acceptance Results

The user completed the requested live acceptance and reported PASS for:

- fresh-session and no-action Workflow truthfulness;
- normal supported startup disable with Dry Run and confirmation intact;
- startup undo availability;
- later explicit scan and disable-absence verification;
- restore through the supported path and later matching verification;
- restart between action and verification;
- pending receipt survival and later result persistence;
- scans not masquerading as executed technician work;
- existing Safety/undo behavior;
- no verification-triggered mutation.

No destructive source-failure injection was performed. Failed, Unavailable,
Unknown, and mixed-source behavior is verified deterministically by automated
tests; omission of destructive manual injection is intentional and non-blocking.

## 31. Regression Suite Status

**User-reported Regression Suite: PASS**

No exact PASS/WARN/FAIL counts were supplied, so none are invented here.

## 32. Debug Bundle and Privacy Status

**User-reported Debug Bundle/privacy inspection: PASS**

No record counts are claimed. The accepted review found no Sprint 25 privacy
boundary violation.

## 33. Known Limitations

- Verification 2.0 currently supports startup disable/restore only.
- Only HKCU Run and current-user Startup folder targets are eligible.
- Machine-wide sources remain unsupported for action verification.
- No performance causality or improvement claim is made.
- Inconclusive receipts may remain pending until a later valid observation.
- There is no automatic scan, polling, watcher, or background verification.
- Exact configuration identity may create conservative false negatives.
- Cleanup, storage, service, and other action-linked verification are deferred.
- Professional report traceability rendering is deferred to Sprint 26.
- Pre-Sprint-25 actions do not receive synthetic receipts.
- No destructive source-failure manual injection was required.
- No Verification-specific WPF detail page exists.
- Older builds may discard unknown optional receipt/result fields when resaving
  a newer report session.

## 34. Explicit Non-Goals

Sprint 25 does not add Reporting 2.0, new correlation rules, new Evidence
sources, performance-improvement claims, automatic rescanning, background
verification, telemetry, service/cleanup/storage Verification 2.0, process
control, automatic remediation, startup discovery expansion, broad Workflow or
Forge Plan redesign, version changes, release packaging, or Sprint 26 work.

## 35. Sprint 25 Definition of Done

- **PASS** — Action execution and verification are distinct.
- **PASS** — Stable privacy-safe source/locator/configuration identity exists.
- **PASS** — Configuration values are represented by deterministic hashes.
- **PASS** — Source completeness is explicit.
- **PASS** — Absence requires a successfully completed exact-source observation.
- **PASS** — Disable verification works.
- **PASS** — Restore verification works.
- **PASS** — Configuration mismatch is detected.
- **PASS** — Ambiguity and incomplete sources become Inconclusive.
- **PASS** — DryRun/Failed/Blocked/Skipped/unsupported targets are not eligible.
- **PASS** — Receipts and verification results persist in ForgeReportSession.
- **PASS** — Restart and pending retrieval work.
- **PASS** — Old sessions load without fabricated state.
- **PASS** — Per-target supersession prevents stale conclusions.
- **PASS** — Workflow uses typed work and factual terminal verification.
- **PASS** — Scans alone do not complete verification.
- **PASS** — Verification performs no mutation or automatic scan.
- **PASS** — Privacy and structural safety tests pass.
- **PASS** — Existing startup Safety/undo behavior remains intact.
- **PASS** — Full solution build and 403-test suite are green.
- **PASS** — Live manual acceptance is green.
- **PASS** — User-reported Regression Suite is green.
- **PASS** — User-reported Debug Bundle/privacy review is green.
- **PASS** — Evidence, Forge Plan, report rendering, release, installer, and
  version boundaries remain unchanged.

## 36. Final Acceptance Status

**SPRINT 25 — ACCEPTED**

Sprint 25 delivers the first restart-safe, action-linked, source-complete
verification loop while preserving explicit technician control and the
existing startup safety model. Sprint 26 Reporting 2.0 remains deferred and
has not begun.
