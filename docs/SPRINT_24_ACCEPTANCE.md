# ForgeCare Sprint 24 Acceptance

**Release target:** v1.1-dev

**Evidence schema:** 1

**Final status:** SPRINT 24 — ACCEPTED

**Acceptance basis:** automated verification, structural safety guards, repository review, and completed live technician acceptance

## 1. Sprint Objective

Sprint 24 adds deterministic, read-only Evidence correlation and presents its
results as technician Attention within Forge Plan 2.0. It identifies narrowly
defined relationships between already-persisted observations without changing
Evidence, executing an action, or replacing the existing Forge Plan action
workflow.

## 2. Strategic Context

ForgeCare now distinguishes two product concepts:

- **Attention:** persisted observations appear related and deserve technician
  review.
- **Plan Actions:** existing subsystem findings may route into established,
  technician-controlled review and action workflows.

Correlation is advisory. It is not execution, remediation, or a replacement
for the authoritative diagnostic and safety subsystems.

## 3. Phase A — Reconnaissance

Phase A inspected the real Evidence, session, Forge Plan, workflow, safety,
report, and UI architecture. It established an additive design that consumes
Sprint 20–23 Evidence while preserving legacy Forge Plan semantics.

## 4. Phase B — Correlation Foundation

Phase B introduced the pure correlation domain:

```text
persisted current-session Evidence
    ↓
defensive EvidenceCorrelationRecord snapshots
    ↓
EvidenceCorrelationEngine
    ↓
explicit deterministic rules
    ↓
TechnicianAttentionItem[]
```

The foundation performs no repository access, Windows inspection, network
activity, persistence, or system mutation.

## 5. TechnicianAttentionItem Contract

`TechnicianAttentionItem` is an immutable correlation result containing a
stable ID, session and rule identity, factual title and summary, category,
attention priority, confidence, rationale, suggested investigation, entity
key, supporting Evidence IDs and correlation keys, and latest Evidence UTC
timestamp.

It deliberately contains no `CanExecute`, route, mutation target, action
command, or automatic-selection state.

## 6. EvidenceCorrelationEngine

The engine validates and snapshots supplied Evidence, enforces the requested
session, orders records deterministically, evaluates explicitly registered
rules, bounds warnings/errors, isolates rule failures, and deduplicates only
identical rule/entity/support sets. Valid results survive another rule's
failure.

## 7. startup-process-resource-v1

The first rule correlates compatible Startup Intelligence and Process
Intelligence observations for the same strong executable identity when the
current Process observation has meaningful resource pressure. Matching is
exact and privacy-conscious; it does not use fuzzy names or ambiguous paths.

## 8. Latest-State Evidence Semantics

Correlation selects the latest relevant observation per source/entity using
timestamp descending and Evidence ID ascending tie-breaking. Current low or
ineligible Evidence suppresses stale historical attention. Correlation keys
are not treated as unique persistence keys.

## 9. Identity and Privacy Matching

The Startup/Process rule accepts only compatible strong identities under
approved normalized roots. `%USERPROFILE%`, redacted, custom-root,
provisional, and ambiguous identities remain excluded. Exact matching favors
safe false negatives over risky association.

## 10. Confidence Design

Confidence represents certainty in the correlation and is derived
conservatively from the weakest supporting source. It remains independent of
priority, resource pressure, and Evidence severity.

## 11. Attention-Priority Design

Attention priority represents technician-review significance. It is explicit
and separate from legacy Forge Plan priority/risk. Current rules emit
restrained Low or Medium priorities and do not create execution authority.

## 12. Phase C — Presentation Contract

Phase C added a stateless presentation boundary:

```text
TechnicianAttentionItem[]
    + optional supporting Evidence lookup
    ↓
ForgePlanAttentionPresenter
    ↓
immutable ForgePlanAttentionItem[]
```

## 13. ForgePlanAttentionItem

The immutable UI projection includes formatted category, priority,
confidence, source support, Evidence count, timestamp, rationale, and
investigation guidance while retaining stable supporting Evidence IDs. It
does not expose mutable Evidence or action-routing fields.

## 14. ForgePlanAttentionPresenter

The presenter performs no repository access or correlation. It formats and
orders items by priority descending, timestamp descending, title, and ID. Its
optional Evidence lookup is used only for readable supporting-source labels.

## 15. process-system-cpu-pressure-v1

The second rule correlates a strong Process Intelligence application CPU
observation with a global Deep Analysis CPU-pressure observation. It requires
structured numeric data and authoritative Medium/High/Critical classifications
rather than parsing observation text or inventing thresholds.

## 16. Exact Same-Analysis Matching

Process/System CPU correlation requires exact UTC timestamp equality between
the Process Intelligence observation and the Deep Analysis `cpu-pressure`
observation. No time-window guessing or nearest-record matching is performed.

## 17. Phase D — Live Integration

```text
explicit BUILD / REFRESH FORGE PLAN
    ├── unchanged ForgePlanService.Build(...)
    │       ↓
    │   legacy ForgePlanItem[]
    └── ForgePlanAttentionLiveService
            ↓ one current-session Evidence load
        EvidenceCorrelationEngine
            ├── startup-process-resource-v1
            └── process-system-cpu-pressure-v1
            ↓
        ForgePlanAttentionPresenter
            ↓
        read-only ATTENTION UI
```

## 18. Shared Repository and One-Load Behavior

MainWindow constructs one `JsonEvidenceRepository`. The same instance remains
shared by Evidence writers, Evidence Explorer, and Forge Plan Attention.
Every explicit Attention build performs exactly one `GetBySessionAsync` call;
rules and presenter perform no additional repository access.

## 19. Attention Versus Plan Actions

The Forge Plan page presents a dedicated `ATTENTION` section above the existing
workspace and labels the legacy collection `PLAN ACTIONS`. Attention cards
contain no selection checkbox or execution control. Legacy plan selection,
risk, ordering, `CanExecute`, routing, and safety workflows remain unchanged.

## 20. Evidence Drill-Down

`VIEW EVIDENCE` uses the first supporting Evidence ID in deterministic order.
The host ensures the current Forge Report session is loaded, then the Explorer
selects the canonical immutable projection. Incompatible filters are cleared
only when required to reveal the record. The operation only loads, selects,
and navigates.

## 21. Session and Reset Behavior

The authoritative ID is always
`ForgeReportService.Snapshot().SessionId`; no fallback is invented. Explicit
Build/Refresh reloads current-session Evidence. Ordinary tab switching does
not rebuild. Starting a new report session clears old transient Attention
state and restores the deliberate not-built message.

## 22. Failure and Partial Success

Evidence load or correlation failure produces a neutral unavailable Attention
state while the already-built legacy plan remains usable. Partial success
retains valid Attention Items and shows a restrained warning. Raw internal
errors are not presented in the UI.

## 23. Workflow Relationship

Sprint 24 does not change `ForgeWorkflowService`. Attention existence or count
does not mark Forge Plan reviewed, complete a workflow step, or replace the
legacy `_latestForgePlanResult` authority.

## 24. Report Relationship

Attention is derived, transient state. It is not written to
`ForgeReportSession`, checkpoints, or report schemas. Source diagnostic runs,
actions, and execution outcomes remain the report's authoritative records.

## 25. Safety Relationship

Viewing Attention or supporting Evidence creates no Safety Journal entry,
recovery snapshot, dry run, confirmation, or action. Existing safety services
remain associated only with established concrete review/action routes.

## 26. Privacy Contract

The live correlation path does not log or persist raw Evidence JSON,
observation bodies, arbitrary metadata bags, executable-path source material,
user names, command lines, startup commands, arguments, tokens, or secrets.
New failure logging is bounded to subsystem context and sanitized exception
type rather than arbitrary source exception messages.

## 27. Read-Only and No-Execution Contract

Correlation and Attention do not call Evidence write APIs, inspect Windows,
use the network, launch or terminate processes, mutate registry/services or
startup state, execute cleanup/storage operations, write/move/delete files,
invoke installers, or elevate privileges. `VIEW EVIDENCE` only navigates and
selects.

## 28. Evidence Schema Compatibility

Evidence remains schema version 1. Sprint 24 adds no Evidence source, record
shape, persistence store, migration, or rewrite. Existing System Scan, Startup
Intelligence, Deep Analysis, and Process Intelligence capture remains
unchanged.

## 29. Automated Verification

Final verification on `v1.1-dev`:

- Restore: PASS
- Build: PASS
- Build errors: 0
- Build warnings: 3 pre-existing nullable warnings
- Automated tests: 324 passed, 0 failed, 0 skipped
- `git diff --check`: PASS

The warnings remain outside Sprint 24 scope:

- `UpdateDiscoveryService.cs(76)`: CS8602
- `RemoteUpdateDiscoveryService.cs(46)`: CS8604
- `RemoteUpdateDiscoveryService.cs(164)`: CS8602

Coverage spans defensive snapshots, latest-state selection, identity matching,
both rules, deterministic IDs/order, confidence/priority, historical behavior,
partial success, scale, presentation, source traceability, one-load live
composition, shared repository, failure isolation, Explorer selection,
filter-reveal behavior, and structural privacy/safety boundaries.

## 30. Manual Acceptance Result

**User-verified: PASS.**

The development build launched and the live Forge Plan layout rendered with a
clear, usable separation between Attention and Plan Actions. Attention Items
populated from current-session Evidence. Priority and confidence rendered
separately; supporting source/count information and advisory investigation
text rendered correctly; no Attention execution control was present.

A real `process-system-cpu-pressure-v1` correlation triggered naturally for
Microsoft Visual Studio, supported by Deep Analysis and Process Intelligence
Evidence. `VIEW EVIDENCE` navigated to Evidence Explorer and selected a
supporting record. Legacy Plan Actions remained separate and usable.
Same-session rebuild, new-session reset, restart/session behavior, and absence
of duplicate/stale Attention behavior were accepted. No system mutation was
observed from Attention functionality.

This is functional manual acceptance, not pixel-perfect accessibility
certification.

## 31. Regression Suite Result

The user reports the in-application Regression Suite as **PASS** during the
Sprint 24 acceptance run. Exact PASS/WARN/FAIL counts were not supplied and
are intentionally not invented here.

## 32. Debug Bundle and Privacy Result

The user reports Debug Bundle and privacy verification as **PASS**. No
fabricated bundle record or correlation counts are recorded.

## 33. Known Limitations

- Only two explicitly reviewed correlation rules exist.
- Correlation is deterministic and rule-based, not AI reasoning.
- Exact identity matching intentionally permits false negatives.
- `%USERPROFILE%`, redacted, custom-root, provisional, and ambiguous identities
  remain excluded from Startup/Process matching.
- CPU correlation requires an exact same-analysis timestamp.
- Attention is transient and rebuilt from Evidence.
- Attention reviewed/accepted/skipped state is not persisted.
- Drill-down initially selects one deterministic supporting Evidence record.
- Legacy Plan Actions and Attention remain separate collections.
- No generic negative-Evidence suppression framework exists.
- No continuous or background correlation exists.
- Future rules require explicit implementation and review.
- Existing older-build enum compatibility limitations remain; Sprint 24 adds no
  Evidence enum or schema member.

## 34. Explicit Non-Goals

Sprint 24 did not add AI/LLM reasoning, automatic execution, startup disable,
process termination, service control, cleanup/storage execution, remediation,
a new Evidence schema/store, Attention persistence, report-schema changes,
workflow/safety semantic redesign, universal entity identity, fuzzy matching,
background correlation, or Sprint 25 functionality.

## 35. Sprint 24 Definition of Done

- **PASS** — deterministic correlation domain exists.
- **PASS** — two useful multi-source rules exist.
- **PASS** — supporting Evidence IDs and correlation traceability are retained.
- **PASS** — confidence and attention priority are explicit and independent.
- **PASS** — rationale is factual and investigation guidance is advisory.
- **PASS** — latest Evidence selection and historical handling are deterministic.
- **PASS** — individual rule failures are isolated and partial success survives.
- **PASS** — correlation performs no Evidence writes or system inspection.
- **PASS** — correlation performs no mutation, execution, network, or persistence.
- **PASS** — generic Evidence drill-down works against canonical Explorer items.
- **PASS** — Forge Plan presents live Attention separately from Plan Actions.
- **PASS** — legacy Forge Plan remains authoritative for actions and workflow.
- **PASS** — shared repository and one-load-per-build behavior are verified.
- **PASS** — session reset prevents stale Attention from appearing current.
- **PASS** — automated suite and solution build are green.
- **PASS** — live manual acceptance is green.
- **PASS** — Regression Suite and Debug Bundle/privacy were user-accepted.
- **PASS** — repository scope contains only intended Phase D, test, and acceptance changes plus the known excluded unrelated path.

## 36. Final Acceptance Status

**SPRINT 24 — ACCEPTED**

Sprint 24 delivers deterministic, explainable, multi-source technician
Attention inside Forge Plan 2.0 while preserving Evidence immutability, legacy
Forge Plan action authority, report/workflow/safety semantics, and ForgeCare's
read-only diagnostic privacy boundary.
