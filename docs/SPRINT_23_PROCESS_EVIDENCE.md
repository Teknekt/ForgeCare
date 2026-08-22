ForgeCare v1.1 — Sprint 23

Process Evidence Intelligence



Status: PLANNED

Target: v1.1 development branch

Depends on: Sprint 20 — Evidence Foundation, Sprint 21 — Evidence Explorer, Sprint 22 — Startup Evidence Intelligence

Primary principle: Observe processes as applications and instances. Aggregate what is equivalent. Preserve what is distinct. Never confuse resource use with danger.



1\. PURPOSE



Sprint 23 turns ForgeCare process inspection from a primarily resource-oriented process snapshot into a technician-grade process evidence source.



The goal is not to turn ForgeCare into:



Task Manager replacement

EDR

antivirus

process killer

permanent monitoring agent

application reputation service



The goal is to answer, using locally observable evidence:



What processes were observed?

Which process instances belong to the same executable/application?

What executable is each process running?

Where is that executable located?

What publisher metadata is available?

Is Authenticode information available?

How many instances belong to one application identity?

What is their combined CPU usage?

What is their combined memory usage?

Which instances contribute most to the total?

What parent-process information is available?

What resource pressure was observed?

What facts support ForgeCare's interpretation?

How confident is ForgeCare in that interpretation?

What remains unknown?



Sprint 23 must preserve the v1.1 product philosophy:



Evidence before recommendation.



Unknown is safer than guessing.



Observation and execution remain separate.



ForgeCare should become substantially better at explaining process behavior without gaining additional power to alter running processes. These principles are explicit v1.1 requirements.



2\. PRODUCT OUTCOME



After Sprint 23, ForgeCare should be capable of representing process observations as an inspectable evidence chain.



Conceptually:



Process Sampling

↓

Process Instance

↓

Executable Identity

↓

File Presence / Metadata

↓

Publisher / Authenticode

↓

Application Grouping

↓

Aggregated Resource Usage

↓

Observed Pressure

↓

Classification

↓

Confidence

↓

Rationale

↓

Process Evidence

↓

Evidence Explorer



A technician should be able to inspect both:



an aggregated application-level observation

the individual process instances supporting that aggregate



Example target:



Chrome



Instances: 14

Total Memory: 2.4 GB

CPU: 3.8 %

Publisher: Google LLC

Signature: Verified

Pressure: LOW



with the individual chrome.exe instances still inspectable separately.



This behavior is directly aligned with the v1.1 specification's Process Analysis and Resource Consumer Aggregation targets.



3\. NON-GOALS



Sprint 23 must NOT introduce:



automatic process termination

manual process termination from new Process Intelligence UI

process suspension

process priority changes

process affinity changes

executable quarantine

malware detection

antivirus replacement

behavioral threat detection

cloud reputation lookup

VirusTotal or similar APIs

executable upload

hash reputation lookup

continuous background monitoring

always-on telemetry

Windows service intelligence beyond what is necessary to describe parent/process context

Startup Intelligence redesign

Forge Plan 2.0

cross-source recommendation logic

automatic remediation

application-wide MVVM migration

application-wide dependency injection

Evidence schema version 2 unless objectively required

redesign of Evidence Explorer

unrelated Analysis redesign

unrelated cleanup/refactoring



Sprint 23 is an observational process-intelligence sprint.



The v1.1 specification explicitly excludes turning the lightweight sampling model into a permanent background telemetry agent.



4\. CORE SAFETY CONTRACT



Process Evidence collection must be read-only with respect to Windows state.



Allowed operations may include:



consuming already-completed Deep Analysis results

enumerating or inspecting process metadata where separately approved

reading process ID

reading process name

reading executable path when accessible

reading parent-process identity where accessible

reading resource metrics

reading local executable metadata

reading Authenticode/signature information

grouping equivalent process identities

calculating aggregate metrics from observed values

creating in-memory Process Intelligence results

writing Evidence through the existing Evidence subsystem

logging bounded diagnostic failures



Process Intelligence must NOT:



call Process.Kill

terminate a process through native APIs

suspend a process

change process priority

change processor affinity

inject into a process

open a process with mutation-oriented permissions unnecessarily

modify executables

delete executables

change ACLs

modify registry configuration

modify services

launch inspected executables

execute commands associated with processes

request elevation merely for intelligence collection

upload executable data

contact external reputation services



Failure to collect Process Intelligence must not invalidate an otherwise successful Deep Analysis.



5\. SOURCE-OF-TRUTH PRINCIPLE



Sprint 23 must reuse existing process observations wherever possible.



The existing Deep Analysis flow already produces ResourceAnalysisResult and ResourceProcessInfo values.



Preferred flow:



Existing Deep Analysis Result

↓

Process Intelligence Enrichment

↓

Process Intelligence Result

↓

Process Evidence Adapter

↓

EvidenceService



Not:



Process Evidence Adapter

↓

Perform an entirely separate resource analysis



The Evidence adapter must remain a translator.



Windows/file/process inspection belongs in a dedicated read-only Process Intelligence component.



Before implementation, reconnaissance must establish exactly which required process fields already exist in the completed Deep Analysis result and which genuinely require additional inspection.



6\. REQUIRED RECONNAISSANCE BEFORE IMPLEMENTATION



Before modifying production code, inspect the real repository and document:



current ResourceAnalysisResult

current ResourceProcessInfo

current ResourceAnalyzerService

current process sampling model

current process enumeration behavior

current CPU calculation

current memory calculation

current Top Processes selection behavior

current process count behavior

existing per-process pressure fields

existing PrimaryResource

current Deep Analysis Evidence adapter

current Resource History behavior

current Analysis UI

current process ListView/data templates

existing process-specific actions, if any

whether executable paths are currently available

whether parent process IDs are currently available

whether process start time is currently available

whether Windows user/session identity is currently available

whether publisher/file metadata helpers can be reused from Sprint 22

whether Authenticode inspection from Sprint 22 can be safely generalized or reused

whether Startup Intelligence file/signature boundaries are process-independent enough to reuse

existing path privacy helpers

existing process identity/correlation logic

current Evidence metadata bounds

current Evidence source/category taxonomy

current Explorer formatting behavior

existing tests

Regression Suite expectations

Debug Bundle behavior

performance implications of enriching multiple process instances



Do not assume conceptual names in this specification exist.



The reconnaissance must identify the smallest architecture compatible with the actual repository.



No implementation should occur during Phase A.



7\. PROCESS IDENTITY MODEL



A Windows PID is an observation identifier, not a durable application identity.



PID reuse means Sprint 23 must distinguish:



Evidence record identity

Process instance identity

Executable/application identity

Correlation identity



Evidence ID remains the existing Evidence GUID.



A process instance may be described by available values such as:



PID

executable identity

process start time, if available

parent PID, if available



An application/executable grouping identity should prefer:



confidently resolved executable path



with conservative fallback when path is unavailable.



Do not treat process name alone as universally equivalent to executable identity.



For example:



helper.exe



at two different executable paths must not automatically be grouped as one application.



8\. PROCESS INSTANCE MODEL



Each observed process instance should retain, where available:



PID

process name

executable path

executable filename

CPU usage

working set / memory

pressure score

pressure level

primary resource

parent PID

parent process name

start time

executable metadata

signature state

signer identity

application-group identity

inspection warnings

observation timestamp



Fields must remain optional when Windows access restrictions prevent collection.



An inaccessible process is not an application failure.



An exited process during inspection is not an Analysis failure.



9\. APPLICATION AGGREGATION MODEL



Sprint 23 should aggregate process instances that can be confidently attributed to the same executable/application identity.



Recommended grouping strength:



Strong identity



Normalized resolved executable path.



Partial identity



When path is inaccessible:



process name may support a provisional group

the group must explicitly indicate reduced identity confidence



Do not merge ambiguous groups as though they were strongly verified.



Aggregation should produce:



display/application name

executable identity

instance count

combined CPU usage

combined memory usage

maximum single-instance CPU

maximum single-instance memory

member process IDs

publisher metadata where coherent

signature state where coherent

pressure classification

identity confidence

aggregation warnings



Individual instances must remain inspectable.



10\. AGGREGATE RESOURCE METRICS



At minimum, application groups should support:



instance count

total observed CPU

total observed memory

highest instance CPU

highest instance memory



If the real analyzer already supports multiple samples and can safely expose them, reconnaissance should evaluate:



average CPU

peak CPU

average memory

peak memory



Do not fabricate metrics that the current sampling model cannot support.



The specification allows improved short-window sampling but explicitly rejects permanent monitoring.



11\. CPU SEMANTICS



CPU values must preserve the semantics of the existing analyzer.



Do not silently change:



sampling interval

processor normalization

percentage interpretation

Top Process ranking



unless explicitly approved after reconnaissance.



When aggregated CPU is calculated, document precisely whether it means:



sum of member instance percentages

average across samples

another existing analyzer-defined metric



Do not present aggregate CPU with greater precision than the underlying observations justify.



12\. MEMORY SEMANTICS



Memory aggregation must use one clearly defined existing metric.



Likely candidates include:



Working Set

Private Working Set

another current ResourceProcessInfo field



Do not sum unlike memory metrics.



UI and Evidence must use the same defined unit.



Recommended technician display:



MB for smaller groups

GB for large totals where formatting already supports it



Raw stored numeric values should remain deterministic.



13\. PROCESS EXECUTABLE PATH



Where Windows exposes the executable path safely, Process Intelligence may capture it for transient inspection.



Possible access failures include:



access denied

protected/system process

process exited

insufficient information



These must produce explicit states.



Do not infer executable path from process name.



Do not search the disk for an executable matching the process name.



Do not inspect unrelated directories.



14\. FILE METADATA



For a confidently resolved existing executable, Process Intelligence may inspect:



file name

file description

product name

company name

file version

product version

original filename



Sprint 23 should preferentially reuse the Sprint 22 file-inspection boundary if reconnaissance confirms it is generic and process-independent.



Do not duplicate equivalent Windows metadata implementations without a clear reason.



Missing metadata is valid evidence.



Missing company metadata does not imply suspiciousness.



15\. AUTHENTICODE



Process executable Authenticode inspection should follow Sprint 22's accepted local/offline trust semantics wherever the same executable-level inspection is applicable.



Preferred reuse:



IStartupSignatureInspector should NOT necessarily be reused by name if doing so creates incorrect domain coupling.



Reconnaissance should determine whether:



the underlying implementation can be generalized

a process-neutral executable-signature abstraction already effectively exists

a narrowly shared executable inspection component is justified



Do not perform a broad refactor solely for naming purity.



Accepted trust semantics remain:



Valid

NotSigned

HashMismatch

Untrusted

Invalid

InspectionFailure

FileMissing

NotChecked

Unsupported



A valid signature is context.



It does not prove:



process safety

application necessity

absence of vulnerabilities

suitability for termination



Unsigned does not mean malicious.



16\. OFFLINE / NETWORK POLICY



Sprint 23 must remain local-first.



No:



HTTP lookup

online reputation

cloud model

external certificate reputation

required certificate download

process hash upload



If Sprint 22 Authenticode logic is reused, preserve its cache-only/offline trust configuration.



Do not claim packet-level proof of zero network traffic by Windows as a whole.



17\. PROCESS CLASSIFICATION



Classification must remain conservative and evidence-based.



Recommended initial taxonomy should be determined during reconnaissance.



Possible useful classifications might include:



VerifiedApplication

KnownApplication

SystemComponent

UnverifiedApplication

Unknown



However:



Do not implement this exact taxonomy automatically.



First inspect:



existing process pressure/status taxonomy

Startup Intelligence classification

Evidence categories

existing product language



Avoid creating a second incompatible classification vocabulary without need.



Classification should describe what ForgeCare can establish about the process/application identity.



It must NOT answer:



Should this process be terminated?



18\. RESOURCE PRESSURE VS IDENTITY CLASSIFICATION



Resource pressure and application identity are different concepts.



Example:



Chrome



Identity classification: Verified Application

Resource pressure: High

Confidence: High



means:



ForgeCare has strong provenance for the executable AND the observed aggregate resource use was high.



It does NOT mean Chrome is dangerous.



Likewise:



Unknown application identity

Resource pressure: Low



does not mean the process is suspicious.



UI and Evidence must not collapse these concepts.



19\. CONFIDENCE



Reuse existing EvidenceConfidence where appropriate.



Confidence describes certainty in the interpretation.



Examples:



Executable path resolved, valid signature, coherent metadata:

High



Path inaccessible but process name observed:

Medium or Low depending on classification claim



Process exited before enrichment:

Unknown/Low for enrichment fields



Aggregation based only on process name:

Reduced confidence compared with path-based aggregation



ForgeCare may be highly confident that identity remains Unknown.



Unknown classification does not require Low confidence.



20\. RATIONALE



Every non-trivial Process Intelligence interpretation should have factual rationale.



Good:



Four process instances resolved to the same executable path and were grouped as one application. The executable carries a valid locally evaluated Authenticode signature.



Good:



ForgeCare observed three processes named helper.exe, but executable paths were unavailable. They are presented as a provisional name-based group.



Good:



The application group used 1.8 GB of combined working set during the completed analysis sample.



Bad:



This application is heavy.



Bad:



This process should be killed.



Bad:



This looks suspicious.



Bad:



ForgeCare AI thinks Chrome is wasting memory.



Rationale must describe evidence.



21\. PARENT PROCESS EVIDENCE



Where practical and safely accessible, Sprint 23 may represent:



parent PID

parent process name



Parent identity is contextual evidence.



Do not infer malicious process trees.



Do not recursively build full process ancestry unless separately approved.



A missing or exited parent is normal.



If parent process inspection requires a disproportionately invasive/native implementation, defer it rather than expanding Sprint 23 unsafely.



22\. PARTIAL SUCCESS



Process Intelligence must support partial success at multiple levels.



Example:



120 processes observed



95 fully enriched

14 enriched without executable path

7 exited during enrichment

4 inaccessible



This should not fail Deep Analysis.



Per-instance failures should produce bounded inspection states/warnings.



Aggregation should continue for usable instances.



Global failure should occur only when no meaningful analysis result can be constructed.



23\. PROCESS EXIT RACE CONDITIONS



Processes are inherently ephemeral.



Between sampling and enrichment, a process may:



exit

restart

reuse a PID later

change resource usage



Sprint 23 must treat this as normal operating behavior.



A process disappearing during enrichment must not be treated as corruption.



Do not automatically substitute a new process with the same PID as though it were the original observation.



If start-time or other identity validation is required to avoid PID reuse ambiguity, evaluate it during reconnaissance.



24\. EVIDENCE SOURCE



Sprint 23 should evaluate whether Process Intelligence requires a dedicated Evidence source.



Preferred conceptual source:



ProcessIntelligence



Before modifying EvidenceSource, confirm:



current enum

JSON serialization

schema-1 compatibility

older source behavior

Explorer generic formatting



Preferred outcome:



EvidenceDocument.SchemaVersion = 1



remains unchanged if the document contract supports the additive source value.



Do not increment the schema merely because a new Evidence source is added.



25\. EVIDENCE RECORD STRATEGY



Avoid Evidence spam.



Recommended first strategy:



Application aggregate record



One primary Evidence record per application/executable group.



Potential contents:



application identity

instance count

combined CPU

combined memory

publisher

signature

classification

confidence

pressure

rationale

Instance records



Do NOT automatically persist one complete Evidence record for every process instance unless reconnaissance shows clear technician value and acceptable volume.



Instead evaluate one of:



aggregate primary Evidence + bounded instance metadata

aggregate primary Evidence + instance Evidence only for selected/top processes

aggregate and separate lightweight observations



The final projection must balance:



inspectability

persistence size

Evidence Explorer usability

repeated Deep Analysis append behavior



Do not increase existing metadata limits merely to fit a poor projection.

26. EVIDENCE SUBJECTS



Possible subjects:



process-application:<normalized-name>



or equivalent.



Do not finalize naming before reviewing existing subject conventions.



Subjects must be:



bounded

deterministic enough for technician use

privacy-safe

generic Explorer friendly

27\. CORRELATION KEYS



Process Intelligence should establish deterministic correlation keys suitable for later cross-source reasoning.



Potential conceptual forms:



process-app:<identity-hash>



process-instance:<identity-hash>:<pid-context>



Requirements:



deterministic

bounded

privacy-safe

no timestamp

no raw command line

no username

no secret-bearing arguments

no claim of universal uniqueness

no random GUID as the correlation identity



Application-level correlation should preferentially derive from normalized executable identity when available.



This becomes important later when Forge Plan correlates process, startup, service and resource observations. Sprint 22 explicitly prepared for this future cross-source model without implementing the correlation itself.



28\. PATH PRIVACY



Reuse the privacy lessons from Sprint 22.



Persisted executable paths must be reviewed carefully.



Prefer environment-root normalization where useful:



%USERPROFILE%

%LOCALAPPDATA%

%APPDATA%

%PROGRAMFILES%

%PROGRAMFILES(X86)%

%PROGRAMDATA%

%WINDIR%



Do not persist unnecessary personal path segments.



Do not strip so much path information that process identity becomes meaningless.



The final persistence decision must be documented.



29\. COMMAND-LINE PRIVACY



Process command lines may contain:



usernames

URLs

filenames

account identifiers

access tokens

API keys

application arguments

document paths



Therefore:



Sprint 23 should not collect or persist process command lines by default.



If reconnaissance finds an existing command-line field, it must not automatically flow into Process Intelligence Evidence.



Any later command-line feature requires separate privacy review.



30\. USER / SESSION PRIVACY



Do not add process owner/user identity merely because Windows exposes it.



If process owner/session information is proposed during reconnaissance, document:



technician value

privacy impact

persistence behavior

Debug Bundle implications



Default Sprint 23 position:



Do not persist Windows username/account identity unless required by an explicitly approved diagnostic feature.



31\. EVIDENCE METADATA BUDGET



Respect the existing Evidence metadata limit.



Candidate aggregate metadata may include:



processName

executableName

normalizedExecutablePath

instanceCount

totalCpuPercent

totalMemoryMb

maxInstanceCpuPercent

maxInstanceMemoryMb

companyName

productName

fileVersion

signatureStatus

signerName

pressureLevel

classification

aggregationConfidence



Do not blindly persist every field.



Do not include raw command line.



Do not include arbitrary exception text.



32\. EVIDENCE SEVERITY



Severity should represent significance of the observation.



It must not represent:



kill desirability

application trustworthiness alone

whether an application is third party

whether an executable is unsigned



Resource pressure may influence severity when the observation explicitly concerns resource pressure.



Identity uncertainty should not automatically produce High severity.



Unknown must remain possible.



No process evidence should become Critical without a separately defined deterministic rule.



33\. EVIDENCE ADAPTER



Introduce a pure adapter conceptually similar to:



ProcessIntelligenceEvidenceAdapter



Input:



completed ProcessIntelligenceResult

explicit report-session ID

explicit UTC observation timestamp



Output:



EvidenceCollectionResult



The adapter must perform:



no process enumeration

no process handle inspection

no filesystem inspection

no signature inspection

no registry inspection

no process termination

no service control

no network call



It translates completed intelligence only.



34\. LIVE INTEGRATION



Preferred eventual flow:



Deep Analysis succeeds

↓

existing Analysis UI/report/history behavior completes

↓

existing Deep Analysis Evidence capture completes

↓

best-effort Process Intelligence enrichment

↓

Process Intelligence Evidence capture



Failure must not:



make Deep Analysis appear failed

clear existing Deep Analysis result

clear UI

invalidate history/report data

display the existing fatal analysis error path

trigger process changes



Use the smallest additive integration point discovered in reconnaissance.



Do not restructure ResourceAnalyzerService solely for conceptual purity.



35\. EXISTING DEEP ANALYSIS EVIDENCE



Sprint 20/21/22 behavior must remain valid.



Existing Deep Analysis Evidence currently provides global CPU/memory/process-count/pressure observations plus selected top-process observations.



Sprint 23 must decide how richer Process Intelligence coexists with that existing source.



Do NOT silently remove existing Deep Analysis Evidence.



Possible approach:



DeepAnalysis



continues to represent the raw resource-analysis observation.



ProcessIntelligence



represents enriched application/process provenance and aggregation.



This separation is preferable if it prevents one adapter from becoming overloaded.



Final design requires reconnaissance approval.



36\. RESOURCE HISTORY



Existing Resource History behavior must be inspected before deciding whether Process Intelligence participates in it.



Default position:



Do not change Resource History persistence during Sprint 23 unless necessary.



Process Intelligence Evidence already provides session-persistent observations.



Avoid creating redundant persistence systems.



37\. ANALYSIS UI



The v1.1 specification ultimately wants process aggregation and per-instance drill-down in the Analysis experience.



However Sprint 23 should not begin with a large UI redesign.



Preferred sequencing:



prove Process Intelligence model

prove aggregation

prove Evidence projection

prove live enrichment

only then evaluate minimal Analysis presentation



A minimal read-only application aggregation surface may be approved in Phase D if it materially improves technician workflow.



Evidence Explorer already provides generic inspection.



Do not duplicate Explorer inside Analysis.



38\. EVIDENCE EXPLORER



The existing generic Explorer should be reused.



Sprint 23 should prove:



Process Intelligence source facet

Process category behavior

application aggregate subject formatting

search by application/process name

search by publisher

search by signature

search by classification

search by pressure

metadata detail rendering

correlation visibility



No Process-specific Explorer redesign should be required unless generic rendering proves inadequate.



39\. TECHNICIAN-GRADE NAVIGATION



Search/filter/sort across Analysis is a broader v1.1 pillar.



Sprint 23 may add only navigation required to make Process Intelligence usable.



Do not turn Sprint 23 into the full Technician-grade Navigation sprint.



That broader work should remain separate.



40\. PERFORMANCE TARGET



Typical scale:



0–300 processes



The architecture should also remain reasonable on machines with more.



Avoid:



one expensive signature verification per duplicate executable

repeated file metadata inspection for the same executable

unbounded Task.WhenAll

continuous timers

recursive filesystem search

online lookups



Use per-run caches keyed by normalized executable identity where practical.



Potential bounded concurrency should be evaluated during reconnaissance.



Do not assume the Startup Intelligence sequential model is automatically appropriate for process counts.



41\. THREADING



Process enrichment must not block the WPF UI thread for a noticeable period.



Requirements:



async orchestration

cancellation support where architecture permits

process/file/native inspection off UI thread

bounded concurrency

no fire-and-forget mutation

clear partial success



Do not introduce a permanent worker/background service.



42\. SAMPLING SCOPE



The current Deep Analysis sampling model remains authoritative unless separately approved.



Sprint 23 may inspect whether modest enhancements are necessary for meaningful aggregation.



Potential future enhancements from the v1.1 specification include:



configurable short sampling window

average CPU

peak CPU

average memory

peak memory

process appearance/disappearance during sampling



Do not implement these automatically in Phase B.



Reconnaissance must determine whether they are necessary for the first Process Intelligence slice.



43\. NEGATIVE EVIDENCE



Process Intelligence should preserve healthy/low-pressure observations.



Examples:



no high-pressure application groups observed

no process group exceeded the current high-pressure classification

resource pressure remained low during the sample



Do not generate artificial problems merely because a process list exists.



Negative Evidence becomes useful later for Forge Plan prioritization, where the v1.1 specification explicitly expects healthy conditions to reduce unnecessary recommendations.



Do not implement Forge Plan correlation during Sprint 23.



44\. STARTUP CORRELATION — FUTURE ONLY



Sprint 22 now produces deterministic Startup Intelligence correlation keys.



Sprint 23 should design Process Intelligence identities so a future sprint can potentially correlate:



startup entry

↕

running executable/application

↕

service

↕

resource pressure



Do NOT implement this cross-source reasoning now.



Preserve future compatibility only.



45\. PROCESS TERMINATION BOUNDARY



If ForgeCare currently has or later gains process termination elsewhere, Process Intelligence must remain behaviorally separate.



Process Intelligence:



OBSERVE

INSPECT

AGGREGATE

CLASSIFY

EXPLAIN



Process management:



TERMINATE

SUSPEND

CHANGE PRIORITY

CHANGE SYSTEM STATE



Sprint 23 authorizes only the first group.



46\. DEBUG BUNDLE PRIVACY



Process Intelligence Evidence will naturally enter existing Evidence debug-bundle export if persisted there.



Before final acceptance inspect for:



full user-profile paths

process command lines

usernames

document paths

URLs

tokens

secrets

excessive process-instance detail

raw exception messages



Do not create a second Process-specific debug bundle without need.



47\. REGRESSION SUITE



Sprint 23 should not make the Regression Suite manipulate processes.



Safe checks may include:



Process Intelligence type availability

Evidence source/schema compatibility

constructed aggregation smoke test

privacy-safe persistence validation

existing Evidence health



Do not:



launch fake processes solely for Regression Suite

kill a process

modify services

force protected-process access



Use deterministic automated tests for deeper behavior.



48\. TEST STRATEGY



Use the existing MSTest project.



Likely test areas:



process instance projection

executable identity

path availability/inaccessibility

process exit race

aggregation by executable

same-name/different-path separation

provisional name grouping

instance count

total CPU

total memory

max CPU/memory

deterministic ordering

parent process metadata where implemented

file metadata reuse

signature status reuse

classification

confidence

rationale

partial success

cancellation

per-run executable cache

0 processes

1 process

100 processes

300 processes

privacy-safe paths

no command-line persistence

Evidence mapping

schema-1 compatibility

Explorer compatibility

same-session coexistence

live Deep Analysis failure isolation



Do not depend on installed third-party applications for core tests.



49\. SAFETY TESTS



Add structural and behavioral guards.



Process Intelligence production code must not reference/invoke:



Process.Kill

native termination APIs

process suspension APIs

process priority mutation

process affinity mutation

command execution

shell execution

service-control writes

registry writes

file delete/move/write

installer handoff

elevation

Startup Manager mutation



Tests should distinguish:



Process object inspection



from



process mutation.



A blanket ban on System.Diagnostics.Process is incorrect because process inspection may legitimately use it.



50\. PRIVACY TESTS



Explicitly verify Evidence does not contain:



command line

command-line arguments

known injected secret test token

clear-text username

unredacted test profile path



Test normalized paths where used.



Test correlation keys for privacy independence.



Test metadata bounds.



51\. AGGREGATION TESTS



At minimum prove:



three instances same executable → one application group

instance count = 3

CPU aggregation correct

memory aggregation correct

max instance metrics correct

two identical process names with different executable paths remain separate

inaccessible path behavior does not falsely merge strong identities

aggregation ordering deterministic

source instances remain inspectable

52\. EVIDENCE VERTICAL SLICE



Smallest useful Evidence slice:



Constructed Process Intelligence result

↓

ProcessIntelligenceEvidenceAdapter

↓

validated Evidence records

↓

EvidenceService

↓

schema-1 repository

↓

fresh repository reload

↓

generic Evidence Explorer projection



Prove:



correct session

UTC timestamp

aggregate metrics survive

privacy-safe path survives

source/classification/confidence survive

correlation survives

existing SystemScan / DeepAnalysis / StartupIntelligence remain readable

53\. LIVE VERTICAL SLICE



Smallest live slice after lower layers are approved:



existing completed ResourceAnalysisResult

↓

Process Intelligence enrichment

↓

Process Intelligence Evidence

↓

same Forge Report session



Do not make Analysis UI dependent on enrichment success.



54\. MANUAL ACCEPTANCE PLAN



Final acceptance should include at least:



A. Baseline

Start ForgeCare

Start fresh Forge Report session

Run System Scan if required by current workflow

Run Deep Analysis

Confirm existing Analysis UI behaves normally

B. Process Intelligence



Inspect multiple applications/process groups:



single-instance process

multi-instance application such as browser

Microsoft/Windows executable

third-party signed executable

unsigned executable if naturally available

inaccessible/system process where naturally available



Verify:



grouping

instance count

CPU/memory totals

publisher metadata

signature state

classification

confidence

rationale

C. Individual instances



Confirm members remain inspectable.



Verify PID and existing per-instance resource fields remain accurate.



D. Evidence



Open Evidence Explorer.



Confirm:



existing Evidence remains

Process Intelligence source appears

aggregate records render

search works

publisher/signature/classification metadata works

correlation is inspectable

E. Repeated analysis



Run Deep Analysis again.



Confirm:



newer observations append

previous evidence remains

no corruption

no unintended deduplication

F. Restart



Close/reopen ForgeCare.



Confirm Evidence persistence.



G. Safety



Confirm:



no process was terminated

no process was launched

no process priority changed

no unexpected elevation

no network dependency required

H. Regression / Debug Bundle



Run Regression Suite.



Export Debug Bundle.



Inspect Process Intelligence Evidence for sensitive values.



55\. PERFORMANCE ACCEPTANCE



Record:



number of observed process instances

number of aggregated application groups

approximate Process Intelligence enrichment duration

whether Analysis UI remained responsive

whether any unusually expensive executable/signature inspection occurred



Do not define a fake universal millisecond SLA before reconnaissance.



The goal is technician-acceptable responsiveness, not benchmark theater.

56. RISK REGISTER

Risk	Rank	Mitigation

Process Intelligence accidentally terminates process	CRITICAL	No mutation APIs; structural/behavioral guards

PID reused between observation and enrichment	HIGH	Conservative instance identity; validate where practical

Same process name grouped across different executables	HIGH	Prefer normalized executable path

Inaccessible paths falsely treated as strong identity	HIGH	Explicit partial/provisional identity

Process exits during inspection and fails full analysis	HIGH	Per-instance partial success

Signature inspection repeated for every instance	HIGH	Per-run executable cache

Enrichment blocks UI	HIGH	async/off-thread + bounded concurrency

Resource aggregation changes existing metric semantics	HIGH	reuse/document current analyzer semantics

Process pressure interpreted as dangerous process	HIGH	separate pressure, identity and recommendation

Unsigned executable treated as malicious	HIGH	conservative classification

Command line leaks secrets	CRITICAL	do not collect/persist by default

User-profile path leaks into Evidence	HIGH	normalize/redact

Evidence volume explodes	MEDIUM/HIGH	aggregate-first projection

Deep Analysis Evidence overwritten	HIGH	additive coexistence

Existing Deep Analysis behavior regresses	HIGH	minimal guarded integration

Schema compatibility breaks	HIGH	prefer schema 1

Explorer requires special process UI	LOW/MEDIUM	generic metadata-first records

Process aggregation becomes Forge Plan correlation	MEDIUM	defer cross-source reasoning

Sprint becomes permanent monitoring system	HIGH	explicit telemetry non-goal

Native parent-process inspection expands scope	MEDIUM	defer if disproportionately complex

57\. IMPLEMENTATION PHASES

Phase A — Repository Reconnaissance and Architecture



Read-only.



Deliver:



actual Deep Analysis architecture

exact process models

exact metrics

process enumeration behavior

existing executable-path availability

existing metadata/signature helpers

Sprint 22 reuse opportunities

identity/grouping design

parent-process feasibility

persistence/privacy decisions

Evidence schema compatibility

live integration point

proposed files

proposed modifications

test strategy

performance strategy

risk register

decisions requiring approval



No implementation.



Stop for approval.



Phase B — Process Intelligence Foundation



Implement only the isolated read-only foundation.



Potential scope:



process instance intelligence models

application aggregation model

process identity resolver

aggregation service

executable metadata/signature enrichment reuse

classification policy

confidence

rationale

partial-success behavior

per-run executable cache

tests



No Evidence adapter.



No live MainWindow integration.



No Analysis UI changes.



No process mutation.



Prove:



constructed existing process observations

↓

read-only enrichment

↓

deterministic aggregation

↓

ProcessIntelligenceResult



Stop for approval.



Phase C — Process Evidence Integration



Implement:



Process Intelligence Evidence source if required

pure Process Intelligence Evidence adapter

privacy-safe path projection

deterministic correlation

bounded metadata

schema-1 persistence

coexistence tests

Explorer compatibility tests



No live Deep Analysis integration until lower-layer slice is green.



No process UI redesign.



No process actions.



Stop for approval.



Phase D — Live Integration and Product Closeout



Only after Phase C approval.



Perform:



guarded live integration after successful Deep Analysis

current-session association

correct observation timestamp

failure isolation

repeated analysis behavior

optional minimal read-only Analysis aggregation presentation only if separately approved

regression/support review

Debug Bundle privacy review

performance review

full automated suite

full build

manual technician acceptance

acceptance documentation



Create:



docs/SPRINT\_23\_ACCEPTANCE.md



Do not begin Sprint 24.



58\. PROTECTED AREAS



Until reconnaissance proves otherwise, treat these as protected:



existing Deep Analysis behavior

ResourceAnalyzerService

current ResourceAnalysisResult

current ResourceProcessInfo

existing Deep Analysis Evidence adapter

Evidence schema

JsonEvidenceRepository architecture

Evidence Explorer architecture

Startup Intelligence behavior

Startup management

service control

cleanup execution

release pipeline

installer

product version



Phase A must identify the minimum actual changes required.



59\. DEFINITION OF DONE — PROCESS INTELLIGENCE



Sprint 23 is complete only when applicable requirements are satisfied.



Architecture

Real Deep Analysis architecture inspected first

Existing metrics documented

Process Intelligence remains read-only

Process inspection and process mutation are separate

Evidence adapter translates completed results only

Process Identity

PID is not treated as durable application identity

executable identity preferred where available

same-name/different-path executables remain separate

inaccessible identity remains explicitly incomplete

process-exit races handled safely

Aggregation

equivalent process instances can aggregate

instance count correct

CPU aggregation deterministic

memory aggregation deterministic

highest-instance metrics available where designed

individual instances remain inspectable

aggregation confidence represented

File Provenance

executable path represented where available

available metadata represented

missing metadata valid

no filesystem mutation

no disk-wide guessing/search

Signature Provenance

valid signature distinguishable

unsigned distinguishable

invalid/untrusted distinguishable

inspection failure distinguishable

signer metadata bounded

no online reputation dependency

Classification

identity classification exists if justified by reconnaissance

confidence independent

rationale factual

resource pressure separate from identity classification

unsigned does not imply dangerous

unknown does not imply dangerous

classification triggers no action

Evidence

Process Intelligence Evidence exists

schema compatibility preserved

current session reused

UTC timestamps preserved

correlation deterministic/bounded

metadata bounded

raw command line excluded

partial success supported

persistence failure does not invalidate successful Deep Analysis

Explorer

generic source facet works

category/filter/search works

aggregate process evidence inspectable

metadata generic rendering sufficient

no redesign unless separately approved

Privacy

command-line persistence reviewed

user-profile path persistence reviewed

username persistence reviewed

Debug Bundle reviewed

no known secret-bearing test values persist

Safety

no process termination

no suspension

no priority/affinity mutation

no command execution

no executable modification

no registry mutation

no service mutation

no elevation for inspection

no cloud/reputation dependency

Performance

normal machine process counts remain practical

UI remains responsive

repeated executable inspection cached where appropriate

no unbounded concurrency

no permanent monitoring

Regression

Sprint 20 tests green

Sprint 21 tests green

Sprint 22 tests green

Sprint 23 tests green

solution builds

Regression Suite acceptable

Debug Bundle functional

existing Deep Analysis behavior preserved

Startup Intelligence preserved

no unrelated version/release changes

60\. DEFINITION OF DONE — PRODUCT CONTRIBUTION



Sprint 23 must materially advance the v1.1 release objective.



The feature is not done merely because Process Intelligence classes exist.



It must make ForgeCare better able to answer:



What is consuming resources?



Is this one application or many unrelated processes?



What executable and publisher are behind it?



What evidence supports that identity?



How confident is ForgeCare?



What remains unknown?



The broader v1.1 feature-completion standard requires correct Evidence, partial-failure handling, explicit unknown states, responsive UI, understandable rationale, preserved safety boundaries, testability, no silent behavior changes and updated documentation.



Sprint 23 should move ForgeCare substantially closer to the v1.1 release requirement:



process analysis supports meaningful aggregation.



61\. SUCCESS CRITERIA



Sprint 23 succeeds if a technician can inspect a multi-process application and understand:



how many instances ForgeCare observed

how much CPU they used together

how much memory they used together

which executable they belong to

which local publisher/signature evidence is available

how strongly ForgeCare can establish the identity

why ForgeCare grouped them

which underlying process instances support the aggregate

what ForgeCare could not determine



without ForgeCare making an automatic process-management decision.



Sprint 23 succeeds if ForgeCare becomes more informative about running software without becoming more dangerous.



STOP.



Do not implement Sprint 23 until Phase A reconnaissance has been explicitly approved.

