\# ForgeCare v1.1 — Sprint 21: Evidence Explorer



\## Status



PLANNED



\## Sprint



Sprint 21



\## Feature



Evidence Explorer



\## Depends On



Sprint 20 — Evidence Foundation



Sprint 20 must remain stable throughout this sprint.



\---



\# 1. PURPOSE



Sprint 21 turns ForgeCare's persisted Evidence foundation into a technician-facing investigation surface.



Sprint 20 established the underlying Evidence architecture:



\- structured Evidence records

\- stable Evidence IDs

\- report-session ownership

\- UTC timestamps

\- category taxonomy

\- source taxonomy

\- severity

\- confidence

\- structured values and units

\- bounded metadata

\- correlation keys

\- schema-versioned persistence

\- System Scan Evidence

\- Deep Analysis Evidence

\- read-only health inspection

\- regression coverage

\- debug-bundle inclusion

\- restart persistence

\- local developer inspection



Sprint 21 does NOT redesign that architecture.



Sprint 21 exposes it.



The technician should be able to answer:



\- What did ForgeCare observe?

\- When was it observed?

\- Which diagnostic produced it?

\- How severe was the observation?

\- How confident is ForgeCare in the observation?

\- What structured measurement supports it?

\- What additional metadata exists?

\- Which observations refer to the same diagnostic concept?

\- Which observations belong to the current technician/report session?



Evidence Explorer is an investigation tool.



It is NOT an optimization engine.



It is NOT a recommendation engine.



It is NOT an automatic remediation system.



\---



\# 2. PRODUCT PRINCIPLE



Evidence must remain distinguishable from interpretation and action.



The UI should reinforce the conceptual chain:



Observation

&#x20;   ↓

Evidence

&#x20;   ↓

Interpretation

&#x20;   ↓

Recommendation

&#x20;   ↓

Technician decision

&#x20;   ↓

Action



Sprint 21 operates primarily at the Evidence layer.



It may display existing classifications already attached to Evidence.



It must not invent new diagnoses or recommendations.



\---



\# 3. PRIMARY USER



The primary user is a technician investigating the current ForgeCare diagnostic session.



Evidence Explorer should optimize for:



\- fast scanning

\- traceability

\- technical clarity

\- low cognitive overhead

\- confidence in where information came from

\- ability to move from summary to detail

\- ability to distinguish observation from recommendation



The interface should feel like a professional diagnostic cockpit rather than a raw JSON viewer.



\---



\# 4. SPRINT OBJECTIVE



Build a read-only Evidence Explorer that allows the technician to:



1\. inspect Evidence for the active Forge Report session

2\. filter Evidence by category

3\. filter Evidence by source

4\. search Evidence

5\. select an Evidence record

6\. inspect its full details

7\. understand severity and confidence independently

8\. inspect structured values and metadata

9\. inspect correlation information

10\. refresh the view after new diagnostics produce Evidence



The feature must operate entirely on the existing Sprint 20 Evidence foundation.



\---



\# 5. NON-GOALS



Sprint 21 must NOT implement:



\- Evidence editing

\- Evidence deletion

\- Evidence repair

\- Evidence schema migration

\- Evidence retention policies

\- automatic cleanup

\- recommendation generation

\- automatic remediation

\- process termination

\- service control

\- startup modification

\- registry changes

\- installer execution

\- privilege elevation

\- AI-generated diagnosis

\- cloud synchronization

\- telemetry

\- cross-device Evidence

\- remote Evidence

\- correlation engine

\- causal inference

\- process enrichment

\- executable path inspection

\- signature/publisher inspection

\- command-line capture

\- Evidence comparison across machines

\- full historical session browser

\- Forge Plan 2.0



These belong to later work unless separately specified.



\---



\# 6. DATA AUTHORITY



Evidence Explorer must consume the existing persisted Evidence records.



The existing Evidence domain remains authoritative.



Primary model:



EvidenceRecord



Expected existing fields include:



\- Id

\- SessionId

\- TimestampUtc

\- Category

\- Source

\- Subject

\- Observation

\- Value

\- Unit

\- Severity

\- Confidence

\- Collector

\- Metadata

\- CorrelationKey



Sprint 21 must not create a second competing Evidence model merely for UI convenience.



A UI projection/view model may be introduced where appropriate.



\---



\# 7. SESSION SCOPE



The default Explorer scope is:



CURRENT FORGE REPORT SESSION



Session identity must come from the same source used by Sprint 20:



ForgeReportService.Snapshot().SessionId



Evidence Explorer must not generate its own session identity.



It must not use:



\- lifecycle session IDs

\- beta field-test IDs

\- machine name

\- timestamps

\- arbitrary UI-generated IDs



If the current report session changes, Evidence Explorer must be able to load the new session's Evidence.



Historical session browsing is explicitly deferred unless the existing repository architecture makes a minimal read-only selector essentially free.



The sprint must not grow into a session-management feature.



\---



\# 8. READ-ONLY CONTRACT



Evidence Explorer is strictly read-only.



The UI must not expose:



\- edit

\- delete

\- repair

\- normalize

\- rewrite

\- merge

\- deduplicate

\- cleanup



Opening or refreshing Evidence Explorer must not modify the underlying JSON document.



Malformed or unsupported documents must not be rewritten.



\---



\# 9. PRIMARY UX



Preferred desktop layout:



┌────────────────────────────────────────────────────────────────────────────┐

│ EVIDENCE EXPLORER                                         REFRESH           │

│ Current Forge session · 18 observations                                    │

├────────────────────┬──────────────────────────────┬────────────────────────┤

│ FILTERS            │ EVIDENCE                     │ DETAILS                │

│                    │                              │                        │

│ ALL            18  │ MEMORY PRESSURE              │ MEMORY PRESSURE        │

│ SYSTEM          5  │ 31.4%                        │                        │

│ CPU             4  │ Deep Analysis · 14:31 UTC   │ 31.4 %                 │

│ MEMORY          3  │                              │                        │

│ PROCESS         6  │ CPU PRESSURE                 │ SOURCE                 │

│                    │ 18.4%                        │ Deep Analysis          │

│ SOURCES            │ Deep Analysis · 14:31 UTC   │                        │

│ System Scan     6  │                              │ OBSERVED               │

│ Deep Analysis  12  │ PROCESS: CHROME              │ 14:31:04 UTC           │

│                    │ 634 MB                       │                        │

│ SEARCH             │ Deep Analysis · 14:31 UTC   │ SEVERITY               │

│ \[\_\_\_\_\_\_\_\_\_\_\_\_\_\_]   │                              │ Informational          │

│                    │                              │                        │

│                    │                              │ CONFIDENCE             │

│                    │                              │ High                   │

│                    │                              │                        │

│                    │                              │ OBSERVATION            │

│                    │                              │ 31.4% of physical...   │

│                    │                              │                        │

│                    │                              │ CORRELATION            │

│                    │                              │ memory:pressure        │

└────────────────────┴──────────────────────────────┴────────────────────────┘



This is a conceptual layout, not a pixel-perfect mandate.



The implementation should adapt to the established ForgeCare design language.



\---



\# 10. VISUAL DESIGN PRINCIPLES



Evidence Explorer should look native to the existing ForgeCare UI.



Reuse existing:



\- typography

\- spacing system

\- panel/card language

\- border treatment

\- status chips

\- button styles

\- section headers

\- dark/light visual conventions if applicable

\- navigation behavior

\- scrolling behavior



Do not introduce a visually unrelated design system.



The Explorer should feel denser than the Dashboard because it is a technician investigation surface, but it must remain readable.



Prefer:



\- clear hierarchy

\- compact information density

\- restrained use of accent colors

\- strong selected-state indication

\- readable technical metadata

\- predictable alignment



Avoid:



\- oversized decorative cards

\- excessive gradients

\- unnecessary animation

\- giant empty areas

\- raw DataGrid aesthetics where a styled list is more appropriate

\- color as the only carrier of meaning



\---



\# 11. INFORMATION ARCHITECTURE



The Explorer has three conceptual areas:



\## A. Filter rail



Used to reduce the visible Evidence set.



\## B. Evidence list



Used to scan and select observations.



\## C. Detail inspector



Used to inspect the complete selected record.



The layout may collapse responsively if required by the existing WPF window dimensions.



\---



\# 12. FILTERING — CATEGORY



Category filters should be generated from the actual Evidence present in the current session.



Expected categories may include:



\- OperatingSystem

\- Cpu

\- Memory

\- Storage

\- Startup

\- Process

\- System



Future categories must not break the Explorer.



Do not hard-code the UI such that unknown/new enum values crash rendering.



Each category filter should preferably show a count.



Example:



ALL        18

CPU         4

MEMORY      3

PROCESS     6



Selecting a category filters the Evidence list.



Only one category filter needs to be active at a time for Sprint 21 unless the existing UI architecture makes multi-select trivial.



Default:



ALL



\---



\# 13. FILTERING — SOURCE



The Explorer should support filtering by EvidenceSource.



Current live sources:



\- SystemScan

\- DeepAnalysis



Future sources must remain representable.



Preferred display names:



System Scan

Deep Analysis



Source counts should be derived from the currently loaded session.



Default:



ALL SOURCES



Category and source filters should combine.



Example:



Category = Process

Source = Deep Analysis



shows only process Evidence produced by Deep Analysis.



\---



\# 14. SEARCH



Provide local in-memory search over the currently loaded Evidence set.



Search should be case-insensitive.



At minimum search:



\- Subject

\- Observation

\- Category display name

\- Source display name

\- CorrelationKey

\- relevant metadata values



Search must not modify persisted Evidence.



Search should combine with category and source filters.



Empty search restores the normal filtered result set.



No fuzzy-search engine or external indexing dependency is required.



\---



\# 15. SORTING



Default order should match the Evidence repository contract:



1\. TimestampUtc descending

2\. Id ascending



Newest observations appear first.



Sprint 21 does not require a full user-configurable sort system.



If sorting controls are introduced, keep them minimal.



\---



\# 16. EVIDENCE LIST ITEM



Each Evidence list item should expose enough information to scan quickly without opening details.



Recommended content:



\- human-readable subject/title

\- primary structured value where available

\- unit where available

\- source

\- timestamp

\- severity indicator

\- confidence indicator where visually appropriate



Examples:



MEMORY PRESSURE

31.4 %

Deep Analysis · 14:31 UTC

Informational · High confidence



PROCESS: CHROME

634 MB

Deep Analysis · 14:31 UTC

Medium · High confidence



OPERATING SYSTEM

Microsoft Windows ...

System Scan · 14:29 UTC



Do not display raw enum names when a clean human-readable label can be derived.



\---



\# 17. SUBJECT DISPLAY



Evidence Subject is a stable technical identifier.



Examples:



cpu-pressure

memory-pressure

process-count

operating-system

process:chrome



The UI should derive a human-readable title without modifying the stored Subject.



Examples:



cpu-pressure

→ CPU PRESSURE



available-memory

→ AVAILABLE MEMORY



process-count

→ PROCESS COUNT



process:chrome

→ PROCESS: CHROME



The raw Subject should remain available in the detail inspector if useful for technical inspection.



\---



\# 18. STRUCTURED VALUE DISPLAY



If EvidenceRecord.Value is present:



display:



Value + Unit



Examples:



18.4 %

43.7 GB

17 items

247 processes

634 MB



Do not invent units.



If Value is absent, the UI may use a useful metadata value or observation preview, but must not fabricate a structured value.



Numeric formatting should be readable and stable.



Avoid unnecessary decimal noise.



\---



\# 19. DETAIL INSPECTOR



Selecting a record opens/populates the detail inspector.



The inspector should expose:



\## Identity



\- human-readable title

\- raw Subject

\- Evidence ID where useful



\## Observation



Full Observation text.



\## Measurement



\- Value

\- Unit



when present.



\## Provenance



\- Source

\- Collector

\- TimestampUtc



\## Assessment



\- Severity

\- Confidence



These must remain visually and conceptually separate.



\## Correlation



\- CorrelationKey



when present.



\## Metadata



Render bounded metadata as key/value rows.



Do not assume specific metadata keys.



The UI must gracefully render future metadata.



\---



\# 20. SEVERITY DISPLAY



Severity is an Evidence classification.



Expected values:



\- Informational

\- Low

\- Medium

\- High

\- Critical

\- Unknown



Severity should be represented by:



\- text

\- optionally icon/badge

\- optionally restrained color



Color must not be the only signal.



Do not recalculate severity in the UI.



Use the persisted Evidence severity.



\---



\# 21. CONFIDENCE DISPLAY



Confidence is independent from severity.



Expected values:



\- Unknown

\- Low

\- Medium

\- High



The UI must not visually merge severity and confidence into one ambiguous score.



Example:



Severity

MEDIUM



Confidence

HIGH



This distinction is important to the Evidence architecture.



Do not calculate confidence in the UI.



\---



\# 22. SOURCE / PROVENANCE DISPLAY



Source should be easy to identify.



Current sources:



System Scan

Deep Analysis



The technician should be able to answer:



"Where did this observation come from?"



without opening the raw JSON.



Collector may be displayed in the detail inspector as a more technical provenance field.



Example:



Source:

Deep Analysis



Collector:

DeepAnalysisEvidenceAdapter



\---



\# 23. TIMESTAMPS



Persisted Evidence timestamps are UTC.



The UI should clearly communicate timestamp semantics.



Preferred behavior:



\- display a readable technician-facing time

\- retain UTC indication where appropriate

\- detail inspector may show full UTC timestamp



Do not silently relabel UTC as local time.



If local-time conversion is introduced, clearly label the resulting timezone or provide the UTC value in details.



Consistency is more important than sophistication.



\---



\# 24. CORRELATION



CorrelationKey should be visible in the detail inspector when present.



Examples:



cpu:pressure

memory:pressure

startup:count

process:chrome:1234



Sprint 21 does NOT implement a correlation engine.



Do not imply that matching correlation keys establish causation.



A future sprint may use these keys to group or compare observations.



For Sprint 21 they are inspectable technical provenance.



\---



\# 25. METADATA



Metadata must be rendered generically.



Example:



PROCESS METADATA



Process Name      chrome

Process ID        1234

CPU Percent       8.4

Memory MB         634

Pressure Score    42

Pressure Level    MEDIUM

Primary Resource  Memory



The UI must not depend on these exact keys existing.



Unknown metadata keys should still render safely.



Metadata values are strings in the current Evidence contract.



No metadata editing.



\---



\# 26. SELECTION BEHAVIOR



When the Explorer loads:



Preferred behavior:



\- select the newest visible Evidence record automatically



If no records exist:



\- no selection

\- show empty state



If filters/search remove the selected record:



\- select the newest remaining visible record



If no filtered records remain:



\- show filtered-empty state



Selection must not modify Evidence.



\---



\# 27. REFRESH BEHAVIOR



Provide an explicit:



REFRESH



action.



Refresh should:



1\. obtain the current Forge Report SessionId

2\. query Evidence for that session

3\. rebuild filter counts

4\. reapply active filters/search

5\. preserve selection when possible

6\. otherwise select the newest visible record



Refresh must not:



\- rerun System Scan

\- rerun Deep Analysis

\- generate Evidence

\- rewrite JSON



It only reloads persisted Evidence.



\---



\# 28. LIVE UPDATE EXPECTATION



Automatic filesystem watching is NOT required in Sprint 21.



The technician may:



1\. run System Scan

2\. open Evidence Explorer

3\. run Deep Analysis

4\. return to Explorer

5\. press REFRESH



A future sprint may implement automatic updates.



Do not add FileSystemWatcher complexity unless the current architecture already makes it trivial and safe.



\---



\# 29. EMPTY STATE



If the current session has no Evidence:



Display a useful empty state.



Example:



NO EVIDENCE YET



Run System Scan or Deep Analysis to create diagnostic observations for the

current Forge session.



Do not treat this as an application error.



Do not create placeholder Evidence.



\---



\# 30. FILTERED EMPTY STATE



If Evidence exists but filters/search return no results:



Example:



NO MATCHING EVIDENCE



Adjust the active filters or search query.



This is different from a session with no Evidence.



\---



\# 31. LOAD ERROR STATE



If Evidence cannot be loaded because the current session document is malformed,

unsupported, or unreadable:



The Explorer should fail safely.



Requirements:



\- do not rewrite the document

\- do not delete the document

\- do not repair it

\- preserve application stability

\- provide a useful technician-facing message

\- log technical details through existing diagnostics conventions



Where useful, offer:



OPEN EVIDENCE FOLDER



or existing diagnostics access.



Do not expose raw exception stack traces in the normal UI.



\---



\# 32. FUTURE SCHEMA STATE



If the Evidence document uses a future unsupported schema:



Do not attempt migration.



Display an explicit compatibility message.



Example:



THIS EVIDENCE DOCUMENT USES A NEWER SCHEMA VERSION.



The document has been preserved unchanged.



The current ForgeCare version cannot inspect it.



This should not crash ForgeCare.



\---



\# 33. NAVIGATION



Evidence Explorer should be reachable through the existing ForgeCare navigation

model.



Preferred result:



A dedicated Evidence / Evidence Explorer destination.



However, the exact placement must be determined from repository reconnaissance.



Do not force a navigation redesign.



The implementation should identify the smallest native integration point.



The existing Tools "OPEN EVIDENCE FOLDER" action remains useful and should not

be removed.



\---



\# 34. UI ARCHITECTURE



Do not place all filtering, projection, selection, and formatting logic directly

inside XAML event handlers if a small UI-facing model/service provides a cleaner

boundary.



A lightweight presentation layer is encouraged.



Possible concepts:



EvidenceExplorerItem

EvidenceExplorerState

EvidenceExplorerService

EvidenceExplorerViewModel



These names are conceptual.



Do not introduce a full MVVM framework solely for Sprint 21.



Do not refactor the entire existing MainWindow to MVVM.



Follow the real ForgeCare architecture.



\---



\# 35. QUERY STRATEGY



The existing Evidence repository should remain the persistence authority.



Preferred load flow:



current report SessionId

&#x20;   ↓

EvidenceService / IEvidenceRepository

&#x20;   ↓

GetBySession

&#x20;   ↓

in-memory Explorer state

&#x20;   ↓

filter/search/select locally



Do not repeatedly deserialize the Evidence file for every filter click.



Load once per refresh.



Filter in memory.



\---



\# 36. PERFORMANCE



Evidence Explorer should remain responsive for normal technician sessions.



Sprint 21 should comfortably handle at least:



500 Evidence records in one session



without obvious UI stalls during:



\- load

\- filter

\- search

\- selection



No database is required.



No pagination is required for the initial implementation unless actual

repository inspection demonstrates a need.



Avoid expensive disk I/O during every UI interaction.



\---



\# 37. ACCESSIBILITY / USABILITY



Requirements:



\- selected item must be visually obvious

\- severity must include text, not color only

\- confidence must include text

\- controls should be keyboard reachable where existing ForgeCare controls are

\- search should have a clear label or placeholder

\- empty/error states should be understandable

\- technical text should remain selectable/readable where practical

\- avoid tiny metadata typography



Follow existing ForgeCare accessibility conventions.



\---



\# 38. ERROR LOGGING



Evidence Explorer failures should use the existing ForgeCare diagnostics/logging

mechanism.



Do not create a separate log system.



Log technical details useful to support/debugging.



UI messages should remain concise and technician-friendly.



\---



\# 39. SAFETY



Evidence Explorer must remain observational.



It must not invoke:



\- CleanupExecutor

\- StartupManagerService

\- StorageCleanupService

\- service control

\- process termination

\- registry modification

\- installer execution

\- elevation

\- Windows configuration mutation



Viewing an Evidence record must never trigger an action against the observed

resource.



A process Evidence record is information about a process, not permission to

terminate it.



\---



\# 40. PRIVACY



Evidence Explorer displays only already-persisted Evidence.



Sprint 21 must not expand collection merely to make the UI richer.



Current Evidence may include:



\- OS identity

\- processor identity

\- memory/storage measurements

\- startup count

\- process names

\- PIDs

\- process resource measurements

\- diagnostic classifications



It must not begin collecting:



\- credentials

\- secrets

\- document contents

\- browser history

\- clipboard contents

\- command lines

\- personal file contents



Process executable paths and publishers remain deferred.



\---



\# 41. TESTING STRATEGY



Sprint 21 should add automated tests below the WPF click layer.



Prefer testing:



\- Explorer state construction

\- current-session loading

\- category counts

\- source counts

\- category filtering

\- source filtering

\- combined filtering

\- search

\- sorting

\- title formatting

\- value/unit formatting

\- severity display mapping

\- confidence display mapping

\- metadata projection

\- selection behavior

\- refresh behavior

\- empty state

\- filtered-empty state

\- malformed-document handling

\- unsupported-schema handling

\- 500-record behavior



Avoid brittle pixel tests.



Avoid requiring interactive WPF automation unless an existing framework already

supports it cleanly.



\---



\# 42. MANUAL ACCEPTANCE



A technician should be able to perform:



1\. Start ForgeCare.

2\. Start or use a Forge Report session.

3\. Run System Scan.

4\. Run Deep Analysis.

5\. Open Evidence Explorer.

6\. Confirm both System Scan and Deep Analysis observations are visible.

7\. Select CPU Evidence.

8\. Confirm value, source, timestamp, severity and confidence.

9\. Select Memory Evidence.

10\. Confirm structured value and metadata.

11\. Select Process Evidence.

12\. Confirm process metadata and correlation key.

13\. Filter by Process.

14\. Confirm only Process Evidence remains.

15\. Filter by Deep Analysis.

16\. Confirm source filtering combines correctly.

17\. Search for a known process.

18\. Confirm matching Evidence remains.

19\. Clear filters/search.

20\. Confirm full Evidence set returns.

21\. Run another Deep Analysis.

22\. Return to Evidence Explorer.

23\. Press Refresh.

24\. Confirm new Evidence appears.

25\. Close ForgeCare.

26\. Reopen ForgeCare.

27\. Open Evidence Explorer.

28\. Confirm persisted Evidence remains inspectable.

29\. Run Regression Suite.

30\. Confirm Evidence persistence remains healthy.



\---



\# 43. FAILURE ACCEPTANCE



Automated tests should prove malformed and unsupported documents remain

untouched.



Manual destructive corruption of technician Evidence is NOT required.



If safe isolated test data can be used in automated tests, verify:



\- malformed JSON produces a safe Explorer error state

\- future schema produces compatibility state

\- source file remains byte-for-byte unchanged



\---



\# 44. EXPECTED IMPLEMENTATION PHASES



Sprint 21 should be implemented incrementally.



Recommended phases:



\## Phase A — Explorer Domain / Presentation Foundation



\- repository reconnaissance

\- Explorer state model

\- projections

\- filtering

\- search

\- sorting

\- formatting

\- selection rules

\- tests



No major UI integration.



\## Phase B — Explorer UI Shell



\- navigation integration

\- three-area layout

\- list

\- detail inspector

\- empty/loading/error states

\- bind to constructed/test state where useful



Minimal live persistence integration.



\## Phase C — Live Evidence Integration



\- active report SessionId

\- repository load

\- refresh

\- real System Scan / Deep Analysis Evidence

\- persistence/error states



\## Phase D — Polish / Hardening / Acceptance



\- UX polish

\- keyboard/accessibility

\- performance

\- regression coverage

\- debug-support review

\- manual acceptance

\- Sprint 21 acceptance document



Do not implement the entire sprint in one uncontrolled change.



\---



\# 45. ARCHITECTURAL CONSTRAINTS



Sprint 20 is a stable dependency.



Avoid modifying Sprint 20 core types unless a proven bug requires it.



Particularly avoid unnecessary changes to:



\- EvidenceRecord

\- JsonEvidenceRepository

\- EvidenceService

\- SystemScanEvidenceAdapter

\- DeepAnalysisEvidenceAdapter

\- EvidenceInspectionService

\- ForgeReportService

\- SystemScanner

\- ResourceAnalyzerService



If a Sprint 20 core change appears necessary:



STOP.



Document:



\- why

\- affected contract

\- regression risk

\- alternative approaches



and request approval before changing it.



\---



\# 46. PERSISTENCE CONTRACT



Sprint 21 must not change:



%LOCALAPPDATA%\\ForgeCare\\Evidence\\<sessionId>.json



or:



SchemaVersion = 1



without explicit architectural approval.



Explorer is a consumer of the persistence contract.



It does not own it.



\---



\# 47. RELEASE / VERSION CONSTRAINT



Sprint 21 implementation must not independently modify:



\- ForgeCare product version

\- release scripts

\- installer scripts

\- GitHub release workflow

\- website download links



Version/release activation occurs only when the larger v1.1 release plan calls

for it.



\---



\# 48. DEFINITION OF DONE



Sprint 21 is complete when:



\- \[ ] Evidence Explorer is reachable through ForgeCare's existing navigation

\- \[ ] current Forge Report session Evidence loads successfully

\- \[ ] System Scan Evidence is visible

\- \[ ] Deep Analysis Evidence is visible

\- \[ ] category filtering works

\- \[ ] source filtering works

\- \[ ] combined filtering works

\- \[ ] search works

\- \[ ] default ordering is deterministic

\- \[ ] newest Evidence is selected by default

\- \[ ] selection populates the detail inspector

\- \[ ] structured Value and Unit are displayed correctly

\- \[ ] Observation is displayed

\- \[ ] Source is displayed

\- \[ ] Collector is inspectable

\- \[ ] UTC timestamp semantics are clear

\- \[ ] Severity is displayed

\- \[ ] Confidence is separately displayed

\- \[ ] CorrelationKey is inspectable

\- \[ ] metadata renders generically

\- \[ ] empty session state works

\- \[ ] filtered-empty state works

\- \[ ] malformed-document state is safe

\- \[ ] future-schema state is safe

\- \[ ] Refresh loads newly persisted Evidence

\- \[ ] refresh does not rerun diagnostics

\- \[ ] Explorer performs no Evidence writes

\- \[ ] Explorer performs no system-changing actions

\- \[ ] Sprint 20 tests remain green

\- \[ ] Sprint 21 automated tests pass

\- \[ ] normal System Scan behavior remains unchanged

\- \[ ] normal Deep Analysis behavior remains unchanged

\- \[ ] Regression Suite remains healthy

\- \[ ] Debug Bundle remains functional

\- \[ ] manual technician acceptance passes

\- \[ ] no unrelated release/version changes occur



\---



\# 49. DEFERRED AFTER SPRINT 21



Potential future work includes:



\- historical session browser

\- Evidence comparison

\- correlation grouping

\- evidence timelines

\- finding-to-evidence traceability

\- recommendation-to-evidence traceability

\- Evidence-driven Forge Plan

\- process enrichment

\- executable trust/signature information

\- service Evidence

\- startup Evidence expansion

\- cleanup Evidence

\- storage Evidence

\- workflow Evidence

\- Evidence export/report presentation

\- automatic live refresh

\- cross-session trends

\- cross-machine diagnostics



These are not Sprint 21 requirements.



\---



\# 50. FINAL SPRINT PRINCIPLE



Sprint 20 answered:



"What did ForgeCare observe, and can it remember it safely?"



Sprint 21 answers:



"Can a technician investigate those observations quickly, clearly and

confidently?"



The Explorer should make persisted Evidence useful without changing what the

Evidence means.

