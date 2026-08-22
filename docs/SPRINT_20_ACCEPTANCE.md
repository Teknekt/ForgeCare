# ForgeCare Technician Edition

## Sprint 20 — Evidence Foundation Acceptance

**Release target:** v1.1-dev  
**Persistence schema:** 1  
**Status:** Implementation complete; final integrated manual acceptance pending  

## Sprint objective

Sprint 20 establishes a local-first, schema-versioned Evidence foundation so
ForgeCare can persist and reload factual diagnostic observations without
changing the existing diagnostic result models or introducing system-changing
behavior.

## Implemented phases

### Phase A — Evidence Foundation

- Evidence domain types and conservative taxonomies
- required-field and UTC validation
- schema-versioned JSON document
- atomic per-session persistence
- repository queries
- thin Evidence service
- pure System Scan adapter
- automated test project

### Phase B — Live System Scan Evidence

- best-effort capture after the existing successful System Scan workflow
- active Forge Report session association
- six factual System Scan observations
- isolated logging and persistence failure handling

### Phase C — Live Deep Analysis Evidence

- pure translation from existing `ResourceAnalysisResult`
- CPU, memory, process-count and overall-pressure observations
- one record per existing selected top process
- best-effort capture after report, history, UI and success-state processing
- same-session coexistence with System Scan evidence

### Phase D — Hardening and closeout

- read-only Evidence health and document validation
- Regression Suite integration
- Evidence persistence/restart field-test step
- Evidence inclusion in explicit debug-bundle export
- read-only Tools action for opening the Evidence folder
- final safety, privacy and Definition-of-Done review

## Resulting architecture

```text
SystemSnapshot ──→ SystemScanEvidenceAdapter ──┐
                                               │
ResourceAnalysisResult                        ├──→ EvidenceService
    └──→ DeepAnalysisEvidenceAdapter ──────────┘          │
                                                          ↓
                                                JsonEvidenceRepository
                                                          │
                                                          ↓
                                      %LOCALAPPDATA%\ForgeCare\Evidence
                                            └── <sessionId>.json
```

`ForgeReportService.Snapshot().SessionId` is the authoritative session
identity. Evidence does not use application-lifecycle, beta-test, machine-name
or independently generated session ownership.

## Persistence

- Root: `%LOCALAPPDATA%\ForgeCare\Evidence`
- Shape: one `<sessionId>.json` document per active Forge Report session
- Schema: `SchemaVersion = 1`, independent of the ForgeCare product version
- JSON: PascalCase, indented, string enums
- Writes: complete document to `.tmp`, then atomic destination replacement
- Ordering: `TimestampUtc` descending, then `Id` ascending
- Unsupported or malformed documents are reported and left untouched

## Live sources

Implemented:

- `SystemScan`
- `DeepAnalysis`

No other Evidence source is live in Sprint 20.

## Automated validation

**AUTOMATED — PASS**

- Full test suite: 46 passed, 0 failed, 0 skipped
- Full solution build: succeeded
- Phase D introduced no compiler warnings
- Three pre-existing nullable warnings remain in update-discovery code and are
  emitted twice by WPF temporary/final compilation

Automated coverage includes domain validation, repository behavior, malformed
and future-schema preservation, adapter translation, partial success,
same-session coexistence, failure isolation, safety boundaries, Evidence
health validation, debug-bundle inclusion/non-mutation and field-test checklist
content.

## Regression validation

The existing Regression Suite now derives a read-only Evidence health result.

- Missing Evidence directory: PASS; storage is created lazily
- Valid current-schema documents: PASS
- Unsupported future schema: WARN; document preserved
- Malformed or invalid current-schema data: FAIL; document preserved
- Session mismatch or non-UTC record: FAIL

The regression check does not add, rewrite, repair or delete Evidence.

**AUTOMATED VALIDATOR TESTS — PASS**  
**IN-APPLICATION REGRESSION RUN — MANUAL PENDING**

## Field-test validation

The existing beta field-test checklist includes `Evidence persistence / restart`:

1. Run System Scan.
2. Run Deep Analysis.
3. Confirm the same-session JSON contains both sources.
4. Restart ForgeCare.
5. Confirm the document remains readable.

**CHECKLIST CONSTRUCTION — AUTOMATED PASS**  
**TECHNICIAN FIELD STEP — MANUAL PENDING**

## Debug-bundle behavior

Explicit debug-bundle export includes the `Evidence` directory when present.
The source files are copied read-only and remain unchanged. A missing Evidence
directory is not an error. Individual copy failures are recorded in
`bundle-copy-warnings.txt` without failing the entire bundle.

Evidence may contain machine diagnostic information including OS and processor
identity, memory and storage values, startup counts, process names, PIDs and
resource measurements. Technicians should review bundles before sharing them.

Current Sprint 20 Evidence does not contain document contents, browser history,
credentials, tokens, secrets, command lines, executable paths or personal file
contents.

**BUNDLE AUTOMATION — PASS**  
**TOOLS EXPORT AND ZIP INSPECTION — MANUAL PENDING**

## Inspection path

The Tools area provides `OPEN EVIDENCE FOLDER`. It opens the existing Evidence
root without modifying Evidence. If the directory does not yet exist, ForgeCare
explains that it is created after a successful diagnostic.

The structured JSON files remain the Sprint 20 inspection surface. A full
Evidence Explorer is deferred.

## Manual acceptance procedure

**MANUAL — PENDING**

1. Start ForgeCare.
2. Run System Scan.
3. Run Deep Analysis.
4. Use Tools → Open Evidence Folder.
5. Confirm one current-session JSON contains `SystemScan` and `DeepAnalysis`.
6. Confirm schema 1, matching session IDs and UTC timestamps.
7. Run Regression Suite and confirm the Evidence result is sensible.
8. Export a Debug Bundle and confirm the Evidence files are included.
9. Close and reopen ForgeCare.
10. Confirm the Evidence document remains readable.
11. Complete the Evidence field-test checklist step.

## Safety and privacy review

**AUTOMATED — PASS**

Evidence generation, validation and inspection do not:

- delete or clean files
- write registry values
- disable startup entries
- control services
- terminate processes
- change Windows settings
- execute installers
- elevate privileges
- transmit telemetry or require cloud services

Evidence validation is read-only. There is no repair-on-read, schema rewrite,
normalization rewrite, retention deletion or technician-session mutation.

## Known limitations

- JSON files are the only Evidence detail view in Sprint 20.
- Only System Scan and Deep Analysis generate Evidence.
- Process correlation keys include PID and are session observations, not stable
  cross-restart process identities.
- Process aggregation, enrichment, executable paths, publishers and signatures
  are not implemented.
- There is no retention policy or schema migration engine yet.
- Final integrated Phase D manual acceptance remains pending.

## Deferred v1.1 work

- Evidence Explorer UI
- richer startup, service, storage and cleanup evidence
- process enrichment and aggregation
- Forge Plan 2.0 correlation
- evidence relationships and temporal correlation
- privacy/redaction controls for future richer evidence
- schema migrations when a later schema requires them

## Sprint 20 Definition of Done

| Requirement | Status | Evidence |
|---|---|---|
| EvidenceRecord domain model exists | PASS | Phase A domain model and tests |
| Evidence category taxonomy exists | PASS | `EvidenceCategory` |
| Evidence source taxonomy exists | PASS | `EvidenceSource` |
| Severity is represented | PASS | `EvidenceSeverity` |
| Confidence is represented separately | PASS | `EvidenceConfidence` and independence tests |
| Evidence IDs are stable | PASS | GUID persistence/reload tests |
| UTC timestamps are used | PASS | validation and adapter tests |
| Session association works | PASS | active report-session integration and coexistence test |
| Evidence persistence works | PASS | atomic repository save/reload tests |
| Schema version 1 exists | PASS | document and JSON tests |
| Repository queries work | PASS | ID/session/category/correlation tests |
| Collection result supports partial success | PASS | malformed-process partial-success test |
| System Scan produces evidence | PASS | live Phase B integration and adapter tests |
| Deep Analysis produces evidence | PASS | live Phase C integration and adapter tests |
| Existing analyzer behavior remains functional | PASS | additive integration; analyzers unchanged; build/tests pass |
| Evidence can be inspected locally | PASS | JSON plus Open Evidence Folder action |
| Domain tests pass | PASS | automated suite |
| Persistence tests pass | PASS | automated suite |
| Adapter tests pass | PASS | automated suite |
| Safety tests pass | PASS | automated structural and behavioral checks |
| Existing relevant regression tests pass | PASS | full automated suite; in-app Phase D run pending manual |
| No new cloud dependency exists | PASS | local filesystem only |
| No system-changing operation occurs during collection | PASS | pure adapters and safety review |

## Closeout status

All required Sprint 20 implementation and automated Definition-of-Done items
are complete.

Final integrated technician acceptance is **MANUAL — PENDING** and must be
recorded before the sprint is marked fully accepted for release progression.
