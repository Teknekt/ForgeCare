# Sprint 21 — Evidence Explorer Acceptance

## Scope

Sprint 21 turns the Sprint 20 Evidence Foundation into a read-only technician
investigation surface for the active Forge Report session.

The sprint delivered a tested presentation layer, a dedicated WPF
`EvidenceExplorerView`, live ForgeCare navigation and current-session
integration, explicit refresh, typed failure presentation, and deterministic
search/filter/selection behavior.

Sprint 21 does not change the meaning, generation, schema or persistence of
Evidence established by Sprint 20.

## Architecture

```text
ForgeReportService
    ↓ Snapshot().SessionId
EvidenceExplorerViewModel
    ↓ read-only session query
shared IEvidenceRepository / JsonEvidenceRepository
    ↓
%LOCALAPPDATA%\ForgeCare\Evidence\<sessionId>.json
    ↓
immutable EvidenceExplorerItem projections
    ↓
EvidenceExplorerView
```

MainWindow constructs one `JsonEvidenceRepository` and shares that instance
between the existing Evidence writer service and the Explorer ViewModel. This
preserves the repository synchronization boundary without introducing a
dependency-injection framework.

The ViewModel receives its session ID from the host. It does not own session
identity and does not depend on `ForgeReportService` directly.

The Explorer calls only `IEvidenceRepository.GetBySessionAsync`. It never calls
repository write methods, adapters, scanners or analyzers. Search, facets,
selection and detail inspection operate only on immutable in-memory
projections.

## Delivered Capabilities

- Dedicated `EVIDENCE` navigation after Analysis and before Services
- Command-palette navigation
- Current Forge Report session loading
- System Scan Evidence inspection
- Deep Analysis Evidence inspection
- Category facets with stable full-session counts
- Source facets with stable full-session counts
- Explicit visual `ALL` and `ALL SOURCES` selections
- Case-insensitive in-memory search across Evidence and metadata
- Combined category, source and search filtering
- Deterministic timestamp-descending, ID-ascending ordering
- Stable-ID selection and refresh preservation
- Generic detail inspector
- Generic immutable metadata projection
- Structured value/unit presentation
- Separate severity and confidence presentation
- Explicit UTC timestamps
- Correlation and provenance inspection
- Explicit read-only refresh
- Automatic active-session switching on navigation/refresh
- Distinct NotLoaded, Loading, Empty, FilteredEmpty, MalformedDocument,
  UnsupportedSchema and LoadError presentation
- Ctrl+F search focus and Escape search clearing
- Native WPF list and facet keyboard navigation
- Accessible names, visible focus, text-plus-color states and selectable
  read-only technical identifiers
- Virtualized/recycling Evidence list suitable for the 500-record sprint target

## Explicit Non-Goals

Sprint 21 does not provide:

- Evidence mutation
- repair, normalization, migration or deletion
- Evidence export
- cross-session or historical browsing
- session comparison
- automatic refresh, timers or file watching
- diagnostic execution from Explorer
- recommendation generation
- Forge Plan integration
- new Evidence sources
- process, cleanup, startup, service or configuration actions
- database, cloud or telemetry functionality

## Safety

Evidence Explorer remains observational.

The Explorer presentation and View do not reference system scanners, resource
analyzers, Evidence adapters, cleanup executors, startup managers, service
control, registry mutation, process termination, installer execution,
elevation or Windows configuration mutation.

Refresh obtains the authoritative current report-session ID and reloads the
persisted session document. It does not run diagnostics or generate Evidence.

Malformed and future-schema documents are reported and left byte-for-byte
untouched. No repair action is offered.

MainWindow hosts other pre-existing ForgeCare actions, so this safety statement
applies specifically to the Explorer integration path rather than MainWindow as
a whole.

## Persistence Contract

Sprint 21 consumes the existing Sprint 20 persistence contract unchanged:

- Root: `%LOCALAPPDATA%\ForgeCare\Evidence`
- One document per Forge Report session
- Filename: `<sessionId>.json`
- Current schema: `1`
- PascalCase JSON properties
- String enum values
- UTC Evidence timestamps
- Atomic temporary-file replacement for writes performed by the existing
  Sprint 20 subsystem

Explorer performs no persistence writes. System Scan and Deep Analysis remain
the only live Evidence sources.

## Automated Acceptance

**AUTOMATED — PASS**

- Restore: succeeded
- Full solution build: succeeded
- Test suite: 97 passed, 0 failed, 0 skipped
- Phase D introduced one local detail-inspector polish so long metadata keys
  wrap instead of relying on tooltip-only inspection; no new warnings
- Three pre-existing nullable warnings remain in `UpdateDiscoveryService` and
  `RemoteUpdateDiscoveryService`
- `git diff --check`: passed

Automated coverage includes formatting, immutable projection, generic metadata,
session loading, deterministic ordering, facets, combined search/filtering,
selection, refresh, session switching, typed load failures, file preservation,
500-record behavior, no repository writes, structural safety and canonical
`ALL` / `ALL SOURCES` synchronization.

## Manual Acceptance

The following results are recorded from the user-reported, approved Phase C
manual acceptance run.

**MANUAL PHASE C — PASS**

- Empty current session: PASS
- System Scan Evidence visibility: PASS
- Deep Analysis Evidence visibility: PASS
- Search: PASS
- Category filtering: PASS
- Source filtering: PASS
- Combined filtering: PASS
- Detail inspector: PASS
- Generic process metadata and correlation inspection: PASS
- Explicit refresh without diagnostic execution: PASS
- Same-session state preservation: PASS
- New-session switching: PASS
- `ALL` / `ALL SOURCES` visual synchronization: PASS
- Restart persistence: PASS
- EVIDENCE tab placement: PASS
- Command-palette navigation: PASS
- 1120-pixel navigation check: PASS
- Existing System Scan and Deep Analysis workflows: PASS
- Regression Suite / Evidence persistence health: PASS

No additional in-application manual run was performed during Phase D.

## Definition of Done

| Sprint 21 requirement | Status | Verification |
|---|---|---|
| Evidence Explorer is reachable through existing navigation | PASS | EVIDENCE tab and approved manual run |
| Current Forge Report session Evidence loads successfully | PASS | Host integration, tests and manual run |
| System Scan Evidence is visible | PASS | Approved manual run |
| Deep Analysis Evidence is visible | PASS | Approved manual run |
| Category filtering works | PASS | Automated and manual validation |
| Source filtering works | PASS | Automated and manual validation |
| Combined filtering works | PASS | Automated and manual validation |
| Search works | PASS | Automated and manual validation |
| Default ordering is deterministic | PASS | Timestamp/ID ordering tests |
| Newest Evidence is selected by default | PASS | ViewModel tests |
| Selection populates the detail inspector | PASS | Binding and manual validation |
| Structured Value and Unit are displayed correctly | PASS | Formatter tests and manual validation |
| Observation is displayed | PASS | List/detail binding and manual validation |
| Source is displayed | PASS | List/detail binding and manual validation |
| Collector is inspectable | PASS | Read-only provenance field |
| UTC timestamp semantics are clear | PASS | Formatter tests and explicit UTC UI |
| Severity is displayed | PASS | Text badge and detail assessment |
| Confidence is displayed separately | PASS | Separate labelled confidence presentation |
| CorrelationKey is inspectable | PASS | Conditional read-only detail field |
| Metadata renders generically | PASS | Immutable generic projection and manual run |
| Empty session state works | PASS | Automated state and approved manual run |
| Filtered-empty state works | PASS | Automated state and UI binding |
| Malformed-document state is safe | PASS | Typed state and file-preservation test |
| Future-schema state is safe | PASS | Typed state/version and preservation test |
| Refresh loads newly persisted Evidence | PASS | Refresh tests and approved manual run |
| Refresh does not rerun diagnostics | PASS | Integration audit and safety boundary |
| Explorer performs no Evidence writes | PASS | Behavioral and structural safety tests |
| Explorer performs no system-changing actions | PASS | Dependency audit and safety tests |
| Sprint 20 tests remain green | PASS | Full 97-test suite |
| Sprint 21 automated tests pass | PASS | 97 passed, 0 failed |
| Normal System Scan behavior remains unchanged | PASS | No scanner/handler change; manual run |
| Normal Deep Analysis behavior remains unchanged | PASS | No analyzer/handler change; manual run |
| Regression Suite remains healthy | PASS | Approved manual run and existing contracts |
| Debug Bundle remains functional | PASS | Existing automated bundle tests remain green |
| Manual technician acceptance passes | PASS | User-reported approved Phase C run |
| No unrelated release/version changes occur | PASS | Phase D Git audit |

## Known Limitations / Future Candidates

Potential future work, not implemented by Sprint 21:

- Historical session browser
- Evidence comparison and timelines
- Correlation grouping
- Finding/recommendation traceability
- Evidence-driven Forge Plan work
- Process enrichment and trust/signature information
- Additional Evidence sources
- Automatic live refresh
- Cross-session or cross-machine trends
- Evidence export/report presentation

These candidates require separate product specifications and architectural
approval.

## Closeout Status

Sprint 21 implementation, automated verification and the approved manual
acceptance are complete. Evidence Explorer remains a read-only consumer of the
accepted Sprint 20 Evidence Foundation.

**SPRINT 21 ACCEPTED — READY FOR SPRINT 22 PLANNING**
