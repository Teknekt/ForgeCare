\# ForgeCare Technician Edition

\## Sprint 20 — Evidence Foundation



\*\*Target:\*\* v1.1-dev  

\*\*Parent specification:\*\* `docs/V1.1-SPEC.md`  

\*\*Theme:\*\* Evidence Foundation  

\*\*Status:\*\* READY FOR IMPLEMENTATION



\---



\# 1. Sprint Objective



Build the foundational evidence architecture for ForgeCare v1.1.



This sprint introduces the internal structures required to represent,

persist, correlate, query and display diagnostic evidence without changing

the established ForgeCare v1.0 safety model.



The goal is not to build the complete Evidence Explorer.



The goal is to establish the trusted evidence substrate that later v1.1

features can consume.



At the end of this sprint ForgeCare should be capable of producing structured

evidence records from selected diagnostic operations and associating those

records with the active ForgeCare session.



\---



\# 2. Product Principle



ForgeCare v1.1 moves from:



&#x20;   diagnostic result



toward:



&#x20;   observation

&#x20;       ↓

&#x20;   evidence

&#x20;       ↓

&#x20;   finding

&#x20;       ↓

&#x20;   recommendation

&#x20;       ↓

&#x20;   technician decision

&#x20;       ↓

&#x20;   action

&#x20;       ↓

&#x20;   verification



Evidence must remain distinct from recommendations and actions.



An observation does not automatically imply that something is wrong.



A finding does not automatically authorize an action.



ForgeCare must continue to prefer uncertainty over unsupported conclusions.



\---



\# 3. Non-Negotiable Safety Rules



Sprint 20 MUST NOT weaken or bypass any existing ForgeCare safety control.



Existing v1.0 principles remain authoritative:



\- technician-controlled execution

\- explicit confirmation before system-changing actions

\- dry-run/review where currently required

\- recovery metadata before supported reversible actions

\- before/after verification

\- conservative classification

\- no automatic service disabling

\- no automatic startup modification

\- no automatic deletion

\- no silent privilege escalation

\- no invented certainty from incomplete evidence



Evidence collection is observational by default.



Collecting evidence MUST NOT itself modify the inspected system.



\---



\# 4. Scope



Sprint 20 includes:



1\. Evidence domain model

2\. Evidence identifiers

3\. Evidence source classification

4\. Evidence severity / confidence representation

5\. Evidence timestamps

6\. Session association

7\. Evidence persistence

8\. Evidence repository/query layer

9\. Evidence collection contracts

10\. Initial diagnostic integration

11\. Evidence summary model

12\. Basic internal validation

13\. Tests

14\. Developer documentation



\---



\# 5. Explicitly Out of Scope



Do NOT implement the following during Sprint 20:



\- full Evidence Explorer UI

\- global evidence search UI

\- advanced filtering UI

\- process relationship graphs

\- service dependency graphs

\- historical machine timelines

\- automatic remediation

\- recommendation execution

\- cloud synchronization

\- telemetry upload

\- remote evidence collection

\- AI-generated conclusions

\- Forge Plan 2.0 UI redesign

\- report redesign

\- unrelated v1.0 refactoring



These belong to later v1.1 phases.



\---



\# 6. Evidence Domain Model



Introduce a first-class evidence entity.



Suggested conceptual model:



&#x20;   EvidenceRecord

&#x20;   ├── Id

&#x20;   ├── SessionId

&#x20;   ├── TimestampUtc

&#x20;   ├── Category

&#x20;   ├── Source

&#x20;   ├── Subject

&#x20;   ├── Observation

&#x20;   ├── Value

&#x20;   ├── Unit

&#x20;   ├── Severity

&#x20;   ├── Confidence

&#x20;   ├── Collector

&#x20;   ├── Metadata

&#x20;   └── CorrelationKey



The exact implementation may adapt to the existing ForgeCare architecture,

but these concepts must remain represented.



\---



\# 7. Evidence Identity



Every evidence record must have a stable unique identifier.



Preferred format:



&#x20;   Guid



Evidence IDs must:



\- be generated locally

\- not contain user-identifying information

\- survive persistence/reload

\- be usable by future findings and reports

\- not depend on UI state



\---



\# 8. Session Association



Every evidence record generated during an active ForgeCare session must be

associated with that session.



Required relationship:



&#x20;   ForgeCareSession

&#x20;       └── EvidenceRecords\[]



Evidence created outside a normal guided workflow may still be stored, but

its session state must be explicit rather than inferred.



Do not fabricate session ownership.



\---



\# 9. Evidence Categories



Introduce a conservative category taxonomy.



Initial categories should support at least:



\- System

\- CPU

\- Memory

\- Process

\- Startup

\- Storage

\- Service

\- Cleanup

\- OperatingSystem

\- Security

\- Network

\- Application

\- Other



The taxonomy must be extensible.



Do not hard-code UI assumptions into the domain representation.



\---



\# 10. Evidence Sources



Evidence must identify where the observation originated.



Examples:



\- SystemScan

\- DeepAnalysis

\- CleanupAnalyzer

\- OptimizationAnalyzer

\- ServiceAnalyzer

\- StorageAnalyzer

\- StartupAnalyzer

\- Workflow

\- Safety

\- Manual

\- Unknown



Unknown is a valid source.



ForgeCare must not guess when provenance cannot be established.



\---



\# 11. Evidence Severity



Evidence severity represents the significance of an observation.



It does NOT represent authorization to act.



Initial values:



&#x20;   Informational

&#x20;   Low

&#x20;   Medium

&#x20;   High

&#x20;   Critical

&#x20;   Unknown



Severity must be stored independently from confidence.



A high-severity observation may have low confidence.



A high-confidence observation may be informational.



\---



\# 12. Evidence Confidence



Introduce an explicit confidence representation.



Suggested initial values:



&#x20;   Unknown

&#x20;   Low

&#x20;   Medium

&#x20;   High



Confidence describes how strongly ForgeCare can support the observation from

available data.



It must not be silently converted into severity.



\---



\# 13. Observation Text



Every evidence record should contain a human-readable observation.



Examples:



&#x20;   "17 startup entries were discovered."



&#x20;   "51.0 GB of physical memory is currently available."



&#x20;   "The observed process used approximately 634 MB working-set memory."



Observation text must:



\- describe what was observed

\- avoid unsupported causal claims

\- avoid presenting recommendations as facts

\- remain understandable without the originating UI screen



\---



\# 14. Structured Values



Where applicable, evidence should preserve machine-readable values separately

from observation text.



Example:



&#x20;   Observation:

&#x20;   "51.0 GB of physical memory is currently available."



&#x20;   Value:

&#x20;   51.0



&#x20;   Unit:

&#x20;   GB



This enables future comparison, filtering, reporting and correlation.



Do not rely solely on formatted UI strings.



\---



\# 15. Subject



Evidence should identify what it describes.



Examples:



&#x20;   system

&#x20;   physical-memory

&#x20;   system-drive

&#x20;   chrome.exe

&#x20;   Cloudflare WARP

&#x20;   Windows Update

&#x20;   C:\\Users\\<user>\\Downloads



Subjects must be descriptive but must not unnecessarily duplicate sensitive

data.



Future privacy filtering should be possible without parsing observation text.



\---



\# 16. Metadata



Evidence may contain additional structured metadata.



Use a representation compatible with ForgeCare's existing persistence model.



Metadata examples:



&#x20;   processId

&#x20;   executableName

&#x20;   serviceName

&#x20;   startupSource

&#x20;   driveLetter

&#x20;   fileCount

&#x20;   sampleDuration

&#x20;   analyzerVersion



Metadata must not become an uncontrolled dumping ground.



Prefer explicit domain properties when a value becomes broadly important.



\---



\# 17. Correlation Key



Evidence records may optionally expose a correlation key.



Purpose:



Allow later v1.1 components to recognize evidence concerning the same logical

subject.



Examples:



&#x20;   process:chrome.exe

&#x20;   service:wuauserv

&#x20;   drive:C

&#x20;   startup:cloudflare-warp



Correlation keys must be deterministic where practical.



They must not imply that two records are causally related.



\---



\# 18. Persistence



Evidence must survive application restart when associated with persisted

ForgeCare session data.



Persistence must remain local-first.



Preferred location should follow ForgeCare's existing local application data

strategy.



Do not introduce:



\- external databases

\- cloud dependencies

\- accounts

\- remote telemetry



for this sprint.



\---



\# 19. Repository Layer



Introduce an abstraction for storing and retrieving evidence.



Conceptually:



&#x20;   IEvidenceRepository



Expected responsibilities:



&#x20;   Add

&#x20;   AddRange

&#x20;   GetById

&#x20;   GetBySession

&#x20;   GetByCategory

&#x20;   GetByCorrelationKey



Exact method names may follow existing project conventions.



UI components must not directly manipulate evidence persistence files.



\---



\# 20. Collection Contract



Introduce a reusable contract for diagnostic components capable of producing

evidence.



Conceptually:



&#x20;   IEvidenceCollector



A collector should be able to return zero or more EvidenceRecord instances.



Collectors must be observational.



Collection failure must not crash the entire diagnostic workflow where safe

degradation is possible.



\---



\# 21. Collector Result



Prefer an explicit result model rather than throwing routine collection

failures into UI code.



Conceptually:



&#x20;   EvidenceCollectionResult

&#x20;   ├── Success

&#x20;   ├── Evidence

&#x20;   ├── Warnings

&#x20;   └── Errors



Partial success must be representable.



Example:



&#x20;   8 observations collected

&#x20;   1 source unavailable

&#x20;   0 fatal errors



ForgeCare should preserve useful evidence even when another source fails.



\---



\# 22. Initial Integration Targets



Do not convert every ForgeCare analyzer during this sprint.



Integrate a small representative vertical slice.



Required initial sources:



\### System Scan



Produce evidence for at least:



\- operating system

\- processor identity

\- installed physical memory

\- available physical memory

\- system drive free space

\- startup item count



\### Deep Analysis



Produce evidence for at least:



\- observed CPU pressure

\- observed memory pressure

\- process count

\- selected top resource consumers



This is sufficient to validate the architecture.



\---



\# 23. Existing Analyzer Compatibility



Existing analyzer output must continue working.



Evidence generation should initially be additive.



Do not replace established v1.0 result models unless necessary.



Preferred transition:



&#x20;   existing analyzer

&#x20;         │

&#x20;         ├── existing v1.0 result

&#x20;         │

&#x20;         └── evidence adapter / collector

&#x20;                   ↓

&#x20;             EvidenceRecord\[]



This minimizes regression risk.



\---



\# 24. Evidence Adapter Pattern



Where practical, prefer adapters over invasive rewrites.



Example:



&#x20;   SystemScanResult

&#x20;         ↓

&#x20;   SystemScanEvidenceAdapter

&#x20;         ↓

&#x20;   EvidenceRecord\[]



This keeps evidence architecture decoupled from the diagnostic implementation.



Adapters should contain translation logic only.



They must not perform system-changing operations.



\---



\# 25. Evidence Summary



Introduce an internal summary model suitable for later UI use.



Example:



&#x20;   EvidenceSummary

&#x20;   ├── TotalCount

&#x20;   ├── InformationalCount

&#x20;   ├── LowCount

&#x20;   ├── MediumCount

&#x20;   ├── HighCount

&#x20;   ├── CriticalCount

&#x20;   ├── UnknownCount

&#x20;   └── Categories



The summary must be derived from evidence records.



Do not persist redundant summary data unless the existing architecture clearly

requires it.



\---



\# 26. Ordering



Evidence queries should use deterministic ordering.



Default preferred ordering:



&#x20;   TimestampUtc descending



Where records share the same timestamp, use a stable secondary ordering.



UI must not receive randomly ordered evidence between refreshes.



\---



\# 27. Timestamp Rules



Store timestamps in UTC.



Formatting into local time belongs at the presentation boundary.



Do not persist locale-formatted timestamps as the authoritative timestamp.



\---



\# 28. Failure Handling



Evidence architecture must degrade safely.



Examples:



If a collector cannot access one data source:



&#x20;   preserve successful evidence

&#x20;   record warning

&#x20;   continue where safe



If persistence fails:



&#x20;   surface the failure

&#x20;   do not falsely claim evidence was saved



If evidence parsing fails:



&#x20;   preserve the original analyzer result

&#x20;   avoid blocking unrelated diagnostics



\---



\# 29. Logging



Important evidence subsystem failures should integrate with the existing

ForgeCare logging approach.



Do not log unnecessary sensitive values.



Logging should distinguish:



\- collection failure

\- persistence failure

\- validation failure

\- unsupported evidence source



Routine successful evidence creation should not flood logs.



\---



\# 30. Validation



Evidence records must be validated before persistence.



At minimum validate:



\- Id is valid

\- Timestamp exists

\- Category exists

\- Source exists

\- Observation is not empty

\- Severity exists

\- Confidence exists



Optional values must remain optional.



Do not manufacture placeholder values merely to satisfy validation.



Use Unknown where Unknown is semantically correct.



\---



\# 31. Privacy



Evidence collection must remain technician-focused and local-first.



Avoid storing unnecessary:



\- document contents

\- browser history

\- credentials

\- tokens

\- secrets

\- personal message content

\- clipboard contents



File paths and usernames should be handled conservatively.



Evidence architecture should make later redaction possible.



\---



\# 32. Performance



Evidence collection must not materially degrade normal ForgeCare diagnostics.



Avoid:



\- repeated expensive WMI/CIM calls where existing results already contain data

\- rescanning the filesystem solely to create evidence

\- duplicate process enumeration

\- synchronous persistence loops for large record sets where batching is possible



Reuse already-collected diagnostic data whenever possible.



\---



\# 33. Threading



Follow existing ForgeCare async/threading conventions.



Evidence persistence and translation must not unnecessarily block the UI

thread.



Do not introduce concurrency complexity without measurable need.



\---



\# 34. UI Impact



Sprint 20 requires minimal UI impact.



A full Evidence Explorer is explicitly deferred.



Permitted UI additions:



\- development/debug evidence count

\- small evidence summary indicator

\- internal diagnostic view

\- temporary developer inspection mechanism



Any temporary UI must not compromise the approved ForgeCare visual language.



\---



\# 35. Developer Inspection



Developers need a way to verify generated evidence before the Explorer exists.



Implement at least one practical inspection path.



Preferred options:



1\. structured local JSON file

2\. debug/developer panel

3\. structured log output



JSON persistence is preferred because it simultaneously validates the

persistence format.



\---



\# 36. Suggested Storage Shape



Conceptual JSON:



&#x20;   {

&#x20;     "schemaVersion": 1,

&#x20;     "sessionId": "...",

&#x20;     "evidence": \[

&#x20;       {

&#x20;         "id": "...",

&#x20;         "timestampUtc": "...",

&#x20;         "category": "Memory",

&#x20;         "source": "SystemScan",

&#x20;         "subject": "physical-memory",

&#x20;         "observation": "51.0 GB of physical memory is currently available.",

&#x20;         "value": 51.0,

&#x20;         "unit": "GB",

&#x20;         "severity": "Informational",

&#x20;         "confidence": "High",

&#x20;         "collector": "SystemScanEvidenceAdapter",

&#x20;         "correlationKey": "memory:physical",

&#x20;         "metadata": {}

&#x20;       }

&#x20;     ]

&#x20;   }



This is conceptual.



Adapt serialization conventions to the existing codebase.



\---



\# 37. Schema Version



Persisted evidence must include a schema version from the beginning.



Initial:



&#x20;   schemaVersion = 1



Do not tie persistence schema directly to the ForgeCare product version.



Future v1.2/v2.0 releases must be able to migrate evidence independently.



\---



\# 38. Compatibility



Malformed or future-version evidence data must not make ForgeCare unusable.



Expected behavior:



\- detect unsupported schema

\- avoid destructive rewriting

\- surface a meaningful warning

\- continue normal ForgeCare operation where possible



\---



\# 39. Tests — Domain



Add tests covering:



\- valid evidence construction

\- required-field validation

\- Unknown values

\- optional structured value

\- optional metadata

\- correlation key behavior

\- timestamp behavior



\---



\# 40. Tests — Persistence



Add tests covering:



\- save evidence

\- load evidence

\- multiple records

\- session filtering

\- category filtering

\- correlation filtering

\- schema version

\- malformed persistence data

\- missing persistence file

\- unsupported schema version



\---



\# 41. Tests — Adapters



Add tests for the initial adapters.



System Scan adapter:



\- generates expected categories

\- preserves numeric values

\- produces conservative observation text

\- uses correct source

\- associates session



Deep Analysis adapter:



\- produces CPU/memory evidence

\- produces process-count evidence

\- produces selected resource-consumer evidence

\- does not invent unsupported conclusions



\---



\# 42. Tests — Safety



Verify that evidence collection:



\- does not delete files

\- does not disable services

\- does not modify startup entries

\- does not terminate processes

\- does not alter system settings

\- does not bypass confirmation workflows



\---



\# 43. Regression Requirement



All existing relevant v1.0 tests must continue to pass.



Existing behavior should remain unchanged unless explicitly required by the

v1.1 specification.



If a regression is discovered:



&#x20;   fix the regression



Do not weaken the test merely to make the suite green.



\---



\# 44. Suggested Code Organization



Adapt to the actual repository structure rather than forcing this exact tree.



Conceptually:



&#x20;   ForgeCare.Core/

&#x20;       Evidence/

&#x20;           EvidenceRecord.cs

&#x20;           EvidenceCategory.cs

&#x20;           EvidenceSource.cs

&#x20;           EvidenceSeverity.cs

&#x20;           EvidenceConfidence.cs

&#x20;           EvidenceCollectionResult.cs

&#x20;           IEvidenceCollector.cs

&#x20;           IEvidenceRepository.cs



&#x20;           Persistence/

&#x20;               JsonEvidenceRepository.cs

&#x20;               EvidenceDocument.cs



&#x20;           Adapters/

&#x20;               SystemScanEvidenceAdapter.cs

&#x20;               DeepAnalysisEvidenceAdapter.cs



&#x20;           Services/

&#x20;               EvidenceService.cs



If equivalent project boundaries already exist, follow them.



Do not create unnecessary projects solely to match this document.



\---



\# 45. Evidence Service



Introduce a thin orchestration layer if appropriate.



Conceptually:



&#x20;   EvidenceService



Responsibilities may include:



\- validation

\- repository interaction

\- summary generation

\- query coordination



It must not become a god object.



Analyzer-specific translation belongs in adapters.



Persistence belongs in repositories.



\---



\# 46. No Premature Correlation Engine



CorrelationKey support is foundational only.



Do NOT build a sophisticated correlation engine during Sprint 20.



This sprint should enable:



&#x20;   GetByCorrelationKey(...)



Later sprints can build:



&#x20;   evidence relationships

&#x20;   temporal correlation

&#x20;   finding synthesis

&#x20;   process/service relationships



Keep Sprint 20 boring and reliable.



That is a feature.



\---



\# 47. No AI Dependency



Evidence Foundation must not require an LLM or external AI service.



All initial evidence should come from deterministic diagnostic data already

available to ForgeCare.



Future intelligent interpretation may consume the evidence layer.



The evidence layer itself must remain useful without AI.



\---



\# 48. Definition of Done



Sprint 20 is complete when:



\- \[ ] EvidenceRecord domain model exists

\- \[ ] Evidence category taxonomy exists

\- \[ ] Evidence source taxonomy exists

\- \[ ] Severity is represented

\- \[ ] Confidence is represented separately

\- \[ ] Evidence IDs are stable

\- \[ ] UTC timestamps are used

\- \[ ] Session association works

\- \[ ] Evidence persistence works

\- \[ ] Schema version 1 exists

\- \[ ] Repository queries work

\- \[ ] Collection result supports partial success

\- \[ ] System Scan produces evidence

\- \[ ] Deep Analysis produces evidence

\- \[ ] Existing analyzer behavior remains functional

\- \[ ] Evidence can be inspected locally

\- \[ ] Domain tests pass

\- \[ ] Persistence tests pass

\- \[ ] Adapter tests pass

\- \[ ] Safety tests pass

\- \[ ] Existing relevant regression tests pass

\- \[ ] No new cloud dependency exists

\- \[ ] No system-changing operation occurs during evidence collection



\---



\# 49. Acceptance Scenario



A technician launches ForgeCare and starts a normal diagnostic session.



ForgeCare runs System Scan.



The existing Dashboard continues to display its normal v1.0 information.



In addition, ForgeCare internally creates structured evidence records such as:



&#x20;   Operating system identified

&#x20;   Processor identified

&#x20;   Physical memory measured

&#x20;   Available memory measured

&#x20;   System drive free space measured

&#x20;   Startup entries counted



The technician then runs Deep Analysis.



Additional evidence is generated for:



&#x20;   CPU pressure

&#x20;   Memory pressure

&#x20;   Process count

&#x20;   Selected resource consumers



The evidence is associated with the current session and persisted locally.



ForgeCare is restarted.



The evidence can be loaded again.



No system configuration was changed as a result of evidence collection.



This scenario must work before Sprint 20 is considered complete.



\---



\# 50. Implementation Strategy



Implement in this order:



&#x20;   1. Domain enums/models

&#x20;   2. Validation

&#x20;   3. Persistence document

&#x20;   4. Repository

&#x20;   5. Repository tests

&#x20;   6. System Scan adapter

&#x20;   7. Adapter tests

&#x20;   8. Deep Analysis adapter

&#x20;   9. Adapter tests

&#x20;   10. Evidence service / orchestration

&#x20;   11. Session integration

&#x20;   12. Developer inspection

&#x20;   13. Regression verification

&#x20;   14. Documentation



Do not start with UI.



Do not start with broad analyzer rewrites.



Prove the evidence pipeline end-to-end first.



\---



\# 51. Architectural Boundary



The intended dependency direction is:



&#x20;   Diagnostic Data

&#x20;         ↓

&#x20;   Evidence Adapter

&#x20;         ↓

&#x20;   Evidence Domain

&#x20;         ↓

&#x20;   Evidence Service

&#x20;         ↓

&#x20;   Evidence Repository

&#x20;         ↓

&#x20;   Local Persistence



Future consumers:



&#x20;   Evidence Explorer

&#x20;   Findings Engine

&#x20;   Forge Plan 2.0

&#x20;   Reports

&#x20;   Verification

&#x20;   Historical Comparison



Future consumers depend on Evidence.



Evidence must not depend on those future consumers.



\---



\# 52. Sprint Deliverable



Sprint 20 should leave ForgeCare with an invisible but functional new

capability:



\*\*ForgeCare can remember what it actually observed.\*\*



That capability becomes the foundation for the rest of v1.1.



\---



MindForge Studio  

ForgeCare Technician Edition  

Sprint 20 — Evidence Foundation

