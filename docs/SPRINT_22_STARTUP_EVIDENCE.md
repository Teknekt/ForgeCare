\# ForgeCare v1.1 — Sprint 22

\# Startup Evidence Intelligence



\*\*Status:\*\* PLANNED  

\*\*Target:\*\* v1.1 development branch  

\*\*Depends on:\*\* Sprint 20 — Evidence Foundation, Sprint 21 — Evidence Explorer  

\*\*Primary principle:\*\* Observe first. Explain what is known. Preserve what is unknown. Never infer certainty from incomplete evidence.



\---



\# 1. PURPOSE



Sprint 22 turns ForgeCare startup inspection from a simple inventory and management surface into a technician-grade evidence source.



The goal is not to make ForgeCare automatically decide which startup applications are "good" or "bad".



The goal is to answer, as reliably as the local machine allows:



\- What startup entry was observed?

\- Where was the entry discovered?

\- What does it attempt to launch?

\- Can ForgeCare resolve the executable?

\- Does the executable currently exist?

\- What publisher information is available?

\- Is an Authenticode signature present?

\- What is the signature state?

\- Which observations are directly verified?

\- Which observations are incomplete or unknown?

\- How confident can ForgeCare be in the resulting classification?

\- Why was that classification assigned?



Sprint 22 must preserve the core ForgeCare safety philosophy:



> Unknown is safer than guessing.



Startup Evidence must describe observed facts and explicitly bounded interpretation.



It must not silently convert uncertainty into recommendations.



\---



\# 2. PRODUCT OUTCOME



After Sprint 22, ForgeCare should be capable of representing a startup entry as an inspectable evidence chain.



Conceptually:



```text

Startup Entry

&#x20;   ↓

Startup Source

&#x20;   ↓

Configured Command

&#x20;   ↓

Executable Resolution

&#x20;   ↓

File Presence

&#x20;   ↓

Publisher / Version Metadata

&#x20;   ↓

Authenticode Inspection

&#x20;   ↓

Structured Startup Classification

&#x20;   ↓

Confidence

&#x20;   ↓

Rationale

&#x20;   ↓

Evidence Records

&#x20;   ↓

Evidence Explorer

A technician should be able to inspect a startup entry and understand both:

what ForgeCare actually observed, and
what ForgeCare was unable to establish.

The resulting Evidence should naturally appear inside the existing Sprint 21 Evidence Explorer without requiring a Startup-specific Explorer.

3. NON-GOALS

Sprint 22 must NOT introduce:

automatic startup disabling
automatic startup enabling
automatic startup removal
automatic registry modification
automatic file deletion
process termination
service modification
scheduled-task modification unless explicitly approved by later scope
automatic remediation
automatic optimization
cloud reputation lookup
VirusTotal or third-party reputation APIs
telemetry
online publisher lookup
executable upload
hash reputation services
malware classification
antivirus replacement behavior
AI-generated security verdicts
Forge Plan 2.0
process intelligence beyond what is required for startup inspection
service intelligence
Evidence schema version 2 unless proven absolutely necessary
Evidence Explorer redesign
application-wide MVVM migration
application-wide dependency injection
unrelated Startup UI redesign
unrelated cleanup or refactoring

Sprint 22 is an observational intelligence sprint.

Existing startup-changing functionality must remain behaviorally separate from Evidence collection.

4. CORE SAFETY CONTRACT

Startup Evidence collection must be read-only with respect to Windows state.

Allowed operations include:

reading already-collected startup models
reading startup configuration required for approved inspection
parsing startup command strings
resolving local paths
checking file existence
reading file version information
reading Authenticode signature information
reading certificate metadata exposed by the local signed file
creating in-memory classification results
writing Evidence through the existing Evidence subsystem
logging ForgeCare diagnostic failures

Startup Evidence collection must NOT:

enable a startup entry
disable a startup entry
delete a startup entry
modify a registry value
create a registry value
modify Startup folders
delete Startup-folder shortcuts
modify shortcuts
execute startup commands
launch inspected executables
terminate processes
stop or start services
change file ACLs
change Windows security settings
request elevation merely for evidence collection
download anything
upload anything
contact external reputation services

Evidence collection must never become a prerequisite for existing Startup functionality.

Failure to collect Startup Evidence must not make an otherwise successful Startup scan or System Scan appear failed.

5. SOURCE-OF-TRUTH PRINCIPLE

Sprint 22 must reuse existing ForgeCare startup observations wherever possible.

The implementation must NOT independently rediscover information that an existing completed startup scan already provides unless the additional inspection is explicitly part of Startup Intelligence.

Preferred flow:

Existing Startup Result
    ↓
Startup Intelligence Enrichment
    ↓
Startup Intelligence Result
    ↓
Startup Evidence Adapter
    ↓
EvidenceService

Not:

Evidence Adapter
    ↓
Perform an entirely separate Startup scan

The Evidence adapter must remain a translator.

Windows inspection belongs in a dedicated read-only Startup Intelligence component.

6. REQUIRED RECONNAISSANCE BEFORE IMPLEMENTATION

Before modifying production code, inspect the real repository and document:

current Startup models
current Startup scanner/service
current Startup enable/disable behavior
current registry locations inspected
current Startup-folder handling
whether shortcuts are currently resolved
how startup commands are represented
whether command paths and arguments are already separated
existing publisher/file metadata helpers
existing Authenticode/signature helpers
existing file-path normalization helpers
existing safety/journal integration
existing Startup UI
current System Scan relationship to startup entries
current Evidence System Scan startup-count generation
current test coverage
current regression checks
current debug-bundle behavior

Do not assume conceptual names from this specification exist in the repository.

The reconnaissance must identify the smallest architecture compatible with the actual codebase.

No implementation should occur during reconnaissance.

7. STARTUP ENTRY IDENTITY

A startup observation needs stable descriptive identity within the diagnostic result, but Sprint 22 must not invent false long-term identity guarantees.

Startup entries may change because:

registry values are renamed
executable paths change
applications update
arguments change
shortcuts are recreated
users move files
entries are removed and re-added

The intelligence model should therefore distinguish:

Evidence record identity
current startup-entry identity
correlation identity

Evidence Id remains the existing stable Evidence record GUID.

A startup correlation key should be deterministic from the strongest locally available identity fields.

Candidate components may include:

startup source
registry hive/location or Startup-folder origin
entry name
normalized executable path

Exact construction must be determined during implementation reconnaissance.

Do not include volatile values such as timestamps in the correlation identity.

Do not claim the correlation key uniquely identifies an application across machines or across all future software versions.

8. STARTUP SOURCE MODEL

ForgeCare must preserve where the startup entry was observed.

At minimum, the intelligence layer should be able to represent source kinds already supported by the current scanner.

Possible source kinds may include:

Current User Run
Local Machine Run
Current User RunOnce
Local Machine RunOnce
Current User Startup Folder
All Users Startup Folder
Unknown

Only source types actually supported by the real ForgeCare scanner should be implemented in the first slice.

If the existing scanner does not inspect one of the conceptual sources above, Sprint 22 must not silently add it without explicit review.

The persisted Evidence should expose a technician-readable source while preserving a machine-readable source value in metadata when appropriate.

9. COMMAND REPRESENTATION

Startup commands are potentially ambiguous.

Examples include:

"C:\Program Files\Example\App.exe" --background
C:\Tools\Agent.exe /startup
%LOCALAPPDATA%\Vendor\App.exe --silent
rundll32.exe something.dll,EntryPoint
cmd.exe /c ...
powershell.exe ...
"C:\Path With Spaces\App.exe"

Sprint 22 must not assume the entire configured startup value is an executable path.

The intelligence model should preserve:

raw configured command
resolved executable path, when confidently resolvable
arguments, when confidently separable
resolution status
resolution confidence or equivalent state

The raw configured command is evidence.

The parsed executable path is an interpretation of that evidence.

Those concepts must remain distinguishable.

10. COMMAND PARSING SAFETY

Command parsing must be conservative.

The parser must:

preserve the original command unchanged
support quoted executable paths
support ordinary unquoted executable paths where resolution is unambiguous
expand environment variables only for inspection
never execute the command
never invoke a shell to determine its meaning
never call the startup executable
tolerate malformed strings
represent unresolved commands explicitly

Special launcher forms such as:

cmd.exe
powershell.exe
pwsh.exe
rundll32.exe
regsvr32.exe
wscript.exe
cscript.exe

must not be interpreted as though the launched payload is automatically equivalent to the launcher executable.

The initial implementation may classify such commands as launcher-mediated and preserve the command without attempting deep payload interpretation.

Unknown is preferable to unsafe parsing.

11. PATH RESOLUTION

When a direct executable path can be safely resolved, Startup Intelligence may determine:

normalized inspection path
whether the path is rooted
whether environment expansion succeeded
whether the target file exists
file extension
executable filename

Path resolution must not:

execute the file
create the file
modify the file
change ACLs
traverse unrelated directories
search the entire disk for similarly named executables
guess replacement locations for missing files

If the configured executable cannot be resolved, record that state.

If a resolved path does not exist, record that state.

Do not silently substitute another executable.

12. FILE METADATA

For an existing resolved executable, ForgeCare may inspect locally available file metadata.

Desired fields, where available:

File name
File description
Product name
Company name
Product version
File version
Original filename

Use local Windows/file metadata only.

Missing metadata is normal and must not automatically lower a startup entry into a dangerous classification.

Metadata absence means metadata absence.

It does not mean maliciousness.

13. AUTHENTICODE INSPECTION

For an existing resolved executable, ForgeCare should inspect Windows Authenticode information using local operating-system facilities.

The implementation should distinguish states such as:

Valid
NotSigned
HashMismatch
NotTrusted
UnknownError
NotChecked
FileMissing
Unsupported

Exact names should follow the actual API and implementation design.

The important contract is semantic:

ForgeCare must distinguish:

VALID SIGNATURE

from:

NO SIGNATURE

from:

SIGNATURE PRESENT BUT INVALID / UNTRUSTED

from:

SIGNATURE COULD NOT BE DETERMINED

These states must never collapse into a single boolean IsSigned.

14. SIGNER / PUBLISHER DATA

Where the local signature exposes appropriate signer information, Startup Intelligence may capture bounded metadata such as:

signer subject/display name
certificate issuer
certificate validity dates
signature inspection state

Do not persist:

private keys
certificate binary blobs
complete certificate chains
unnecessary certificate extensions
unrelated personal certificate-store information

ForgeCare should inspect the certificate associated with the executable being analyzed, not enumerate the user's certificate stores broadly.

Publisher information from file version metadata and signer identity from Authenticode must remain conceptually separate.

They may disagree.

ForgeCare must not silently choose one and discard the other.

15. MICROSOFT / WINDOWS RELATIONSHIP

If Sprint 22 exposes a concept such as "Microsoft-signed", it must be based on explicit locally observed signer information.

Do not classify an executable as Microsoft software merely because:

it is located under C:\Windows
its filename sounds like Windows
its company metadata contains an approximate string
it is commonly known to be a Windows executable

Location and filename are supporting observations, not cryptographic identity.

If signer identity cannot establish the relationship, use Unknown.

16. STARTUP INTELLIGENCE RESULT

Introduce a read-only result model representing enriched startup observations.

Conceptually:

StartupIntelligenceResult
    Entry identity
    Startup source
    Raw command
    Command resolution state
    Resolved executable path?
    Arguments?
    File exists?
    File metadata
    Signature state
    Signer metadata
    Classification
    Confidence
    Rationale
    Warnings

Exact field names should follow existing ForgeCare conventions.

The result must contain enough structured information that:

Evidence generation requires no further Windows inspection
UI presentation requires no further Windows inspection
tests can construct deterministic results without touching the real registry or filesystem where avoidable
17. CLASSIFICATION MODEL

Sprint 22 may classify startup observations, but classifications must remain conservative and explainable.

Recommended classification taxonomy:

Verified
Known
Unverified
Broken
Suspicious
Unknown

These names are conceptual and may be refined during reconnaissance if an existing ForgeCare taxonomy already serves the purpose.

Intended semantics:

Verified

Strong local evidence supports the executable identity.

Typical supporting observations:

executable resolved
file exists
valid Authenticode signature
coherent signer/publisher metadata

This does NOT mean:

safe forever
vulnerability-free
recommended
necessary
performance-friendly
Known

ForgeCare can resolve and inspect the startup target, but the evidence does not justify Verified.

Examples may include:

existing executable
useful file metadata
unsigned executable
insufficient signer information

This is not inherently negative.

Unverified

The entry exists but important identity/provenance properties cannot be established.

Examples:

executable exists but publisher/signature identity is unavailable
launcher-mediated command cannot be confidently attributed to its payload
incomplete inspection

This is uncertainty, not accusation.

Broken

The startup configuration points to something ForgeCare can confidently determine is unavailable or structurally unusable.

Examples may include:

confidently resolved direct executable path does not exist

Do not classify as Broken merely because a complex command could not be parsed.

Suspicious

Use extremely conservatively.

Sprint 22 should only emit this classification when explicit locally observed conditions justify it.

A suspicious classification must NEVER be produced solely because:

an executable is unsigned
publisher metadata is missing
a file lives outside Program Files
the application is unfamiliar
the filename is unusual
the process consumes resources
ForgeCare lacks reputation information

If no strong deterministic suspicious rule is approved during implementation, Sprint 22 may ship without ever producing Suspicious.

Unknown

Use when the available evidence does not safely support another classification.

Unknown is a valid and expected result.

18. CLASSIFICATION MUST NOT BECOME RECOMMENDATION

Classification answers:

What can ForgeCare establish about this startup entry?

It does NOT answer:

Should this startup entry be disabled?

Examples:

Verified ≠ Keep enabled
Known ≠ Keep enabled
Unverified ≠ Disable
Broken ≠ Automatically remove
Suspicious ≠ Automatically disable
Unknown ≠ Bad

Startup management decisions remain technician-controlled.

Sprint 22 must not introduce automatic action based on classification.

19. CONFIDENCE MODEL

Confidence must remain independent from classification and Evidence severity.

Recommended levels should reuse the existing Evidence confidence taxonomy where practical:

High
Medium
Low
Unknown

Examples:

Classification: Verified
Confidence: High
Classification: Unknown
Confidence: High

The second example is valid.

ForgeCare can be highly confident that it lacks enough evidence to identify the startup target.

Do not force low confidence simply because the classification is Unknown.

20. RATIONALE

Every non-trivial startup classification must have human-readable rationale.

Good rationale:

The startup target was resolved and exists. Windows reports a valid Authenticode signature issued to Example Corporation.

Good rationale:

The startup entry was observed, but the configured command uses a launcher form that ForgeCare did not resolve to a payload executable.

Good rationale:

The configured direct executable path was resolved, but the target file was not present at inspection time.

Bad rationale:

This app looks safe.

Bad rationale:

Probably unnecessary.

Bad rationale:

Suspicious startup program.

Rationale must describe evidence, not intuition.

21. PARTIAL SUCCESS

Startup Intelligence must support partial success.

Example:

17 startup entries discovered


14 fully inspected
2 partially inspected
1 could not be enriched

One malformed or inaccessible startup entry must not discard all other observations.

Per-entry failures should produce:

a usable partial result when possible
bounded warnings
Unknown states where appropriate

Global failure should occur only when the underlying startup result itself cannot be meaningfully processed.

22. EVIDENCE SOURCE TAXONOMY

Sprint 22 requires a dedicated Evidence source for Startup Intelligence.

Preferred conceptual source:

StartupIntelligence

Before changing EvidenceSource, confirm the current enum and serialization behavior.

Adding the new enum member must remain compatible with schema version 1 if the schema contract permits additive enum values.

Do NOT increment the Evidence schema merely because a new source enum value is introduced unless the persisted contract genuinely requires it.

Existing Sprint 20 documents must remain readable.

23. EVIDENCE CATEGORIES

Reuse existing categories where appropriate.

Expected categories may include:

Startup
Application
Security

Do not add redundant categories solely for Sprint 22 if the existing taxonomy can represent the observations.

The primary startup record should normally remain Startup.

Signature-specific evidence may use Security only when doing so improves inspection without fragmenting one entry into excessive records.

Avoid evidence spam.

24. EVIDENCE RECORD DESIGN

Each enriched startup entry should produce a bounded, useful Evidence representation.

Preferred initial strategy:

One primary Evidence record per startup entry.

Subject concept:

startup:<normalized-entry-name>

Observation example:

Startup entry 'Example Agent' launches a locally resolved executable with a valid Authenticode signature.

Structured metadata may include bounded fields such as:

entryName
startupSource
rawCommand
resolvedExecutable
fileExists
fileDescription
productName
companyName
fileVersion
productVersion
signatureStatus
signerName
classification
classificationRationale
resolutionStatus

Do not blindly persist every available field.

Respect the existing Evidence metadata bound.

If required data cannot fit clearly inside the bounded metadata model, reconsider the projection rather than increasing limits automatically.

25. RAW COMMAND PRIVACY

Startup commands can contain sensitive information.

Potential examples include:

usernames inside paths
user-profile paths
application arguments
tokens passed incorrectly by third-party software
URLs
account identifiers

Therefore the reconnaissance must explicitly review whether rawCommand should be persisted directly into Evidence.

Preferred privacy hierarchy:

Preserve the raw command inside the transient Startup Intelligence result when needed for diagnosis.
Persist a normalized/bounded representation only when safe.
Avoid persisting command arguments that may contain secrets unless clearly justified.
Prefer executable identity over full argument persistence.

Do not assume that because startup commands are locally available they are automatically appropriate for debug bundles.

The final persistence decision must be documented.

26. EXECUTABLE PATH PRIVACY

Executable paths may expose usernames or custom directory names.

The implementation must explicitly review whether full paths should be persisted.

Possible approaches:

full path locally
environment-tokenized path
profile-relative normalized path
filename plus source metadata
selectively redacted user-profile prefix

The chosen approach must balance technician usefulness and privacy.

Do not silently remove path information if doing so prevents meaningful diagnosis.

Do not silently persist unnecessary personal path segments either.

The decision must be recorded in Sprint 22 acceptance documentation.

27. CORRELATION KEY

Startup Intelligence records should receive deterministic correlation keys.

Conceptual form:

startup:<source>:<normalized-entry-identity>

The final implementation may include a normalized executable identity where useful.

Requirements:

deterministic
bounded
no random GUID
no timestamp
no claim of universal uniqueness
stable for equivalent observations when reasonable
does not contain secrets
does not require executing the target

Correlation should support future Forge Plan reasoning without becoming a primary database key.

28. EVIDENCE SEVERITY

Evidence severity must describe the significance of the observation, not the desirability of disabling startup.

Recommended conservative mapping:

Verified / Known
    → Informational


Unverified
    → Low or Informational depending on rationale


Broken
    → Medium


Suspicious
    → High only when strong deterministic evidence exists


Unknown
    → Unknown or Informational

The exact mapping must be explicitly tested.

Do not map:

Unsigned → High
Unknown publisher → High
Unknown → Critical

No startup intelligence observation should become Critical without an explicit, separately approved deterministic rule.

29. STARTUP EVIDENCE ADAPTER

Introduce a pure adapter conceptually similar to:

StartupIntelligenceEvidenceAdapter

Input:

StartupIntelligenceResult
reportSessionId

Output:

EvidenceCollectionResult

The adapter must:

perform no registry inspection
perform no filesystem inspection
perform no signature inspection
execute no command
modify no startup state
generate no new startup result
translate only completed intelligence data

It should be testable entirely with constructed models.

30. LIVE INTEGRATION

The safest integration point must be identified during reconnaissance.

Preferred behavior:

Existing successful Startup/System Scan
    ↓
existing behavior completes
    ↓
Startup Intelligence enrichment
    ↓
existing UI remains successful
    ↓
best-effort Startup Evidence capture

Evidence or enrichment failure must not turn the original diagnostic into a failure.

If Startup inspection is currently embedded inside System Scan, do not restructure the entire scanner merely to satisfy this conceptual flow.

Use the smallest additive integration point supported by the real repository.

31. EXISTING STARTUP MANAGEMENT

ForgeCare already contains startup management behavior.

Sprint 22 must maintain a hard conceptual boundary:

STARTUP INTELLIGENCE
    OBSERVE
    INSPECT
    CLASSIFY
    EXPLAIN

versus:

STARTUP MANAGEMENT
    ENABLE
    DISABLE
    RESTORE
    CHANGE SYSTEM STATE

Startup Intelligence must not directly invoke management operations.

Management may eventually consume Intelligence in a later sprint, but Sprint 22 does not authorize that integration.

32. EXISTING STARTUP COUNT EVIDENCE

Sprint 20 currently produces System Scan Evidence for:

startup-entries

That record must remain.

Sprint 22 adds per-entry intelligence.

Therefore a session may contain:

Source: SystemScan
Subject: startup-entries
Value: 17 items

plus:

Source: StartupIntelligence
Subject: startup:example-agent

for individual entries.

Do not replace or reinterpret the existing startup-count record.

33. EVIDENCE EXPLORER COMPATIBILITY

Sprint 21 Evidence Explorer is intentionally source-neutral.

Startup Intelligence must fit the existing Explorer without requiring special-case rendering.

Expected behavior:

New source automatically appears in Source facets.
Startup category counts update naturally.
Startup records appear in deterministic timestamp ordering.
Search can find entry name, classification, publisher, signer, and metadata.
Generic metadata rendering remains sufficient.
Detail inspector exposes correlation and provenance.
Severity and confidence remain separate.

Do not add Startup-specific Explorer XAML unless generic rendering proves genuinely insufficient and explicit approval is obtained.

34. OPTIONAL STARTUP UI ENRICHMENT

Sprint 22 does not require a redesign of the existing Startup UI.

If the existing Startup page can accept a very small read-only enrichment without architectural risk, possible future presentation may include:

VERIFIED
KNOWN
UNVERIFIED
BROKEN
UNKNOWN

with confidence/rationale available.

However:

The first vertical slice must prove the intelligence and Evidence pipeline before modifying Startup presentation.

Evidence Explorer is the required inspection surface for Sprint 22.

Startup-page badges are optional and require separate approval after the core slice is proven.

35. FAILURE HANDLING

Expected failure conditions include:

malformed startup command
unresolved environment variable
missing executable
inaccessible executable
file metadata unavailable
signature inspection unavailable
certificate parsing failure
unsupported launcher command
unexpected filesystem error

These must be represented as safely as possible.

Rules:

Do not crash the complete startup scan because one entry fails.
Do not discard valid entries.
Do not silently convert inspection failure into NotSigned.
Do not silently convert parse failure into Broken.
Do not silently convert missing metadata into Suspicious.
Log meaningful unexpected failures.
Preserve the original startup observation.

36. CANCELLATION / THREADING

File and signature inspection may be slower than simple model translation.

The implementation should:

support cancellation where existing workflows support it
avoid blocking the WPF UI thread with bulk file inspection
avoid unnecessary parallel fan-out across dozens of executables
use bounded/conservative concurrency if concurrency is introduced

For ordinary startup counts, sequential or lightly bounded asynchronous inspection is likely sufficient.

Do not introduce a complex worker framework for Sprint 22.

37. CACHING

Do not add persistent caching in the first implementation slice.

A short-lived in-memory cache may be considered only if repeated inspection during one completed workflow causes measurable duplicate work.

Any cache key must account for enough file identity to avoid reusing stale signature information after executable replacement.

Unless performance testing demonstrates a need:

NO CACHE

is preferred.

38. FILE HASHING

Cryptographic file hashing is NOT required for Sprint 22.

Do not add SHA-256 merely because security metadata is being inspected.

Hashing may become useful for later:

local change detection
reputation integration
artifact identity

but it creates additional cost and future semantic expectations.

Sprint 22 should establish provenance first.

39. NETWORK BEHAVIOR

Startup Intelligence must operate with:

0 required network requests

It must work offline.

No:

HTTP
DNS reputation
cloud certificate lookup
package registry
vendor API
search engine
VirusTotal
Microsoft reputation API
OpenAI API

is required or authorized.

Any Windows signature API behavior that may implicitly attempt online revocation checks must be reviewed during implementation.

Prefer inspection behavior that does not unexpectedly create network dependency.

The final implementation must document this decision.

40. PROPOSED PRODUCTION TYPES

Exact names may adapt to the real repository after reconnaissance.

Likely new types:

Models
StartupIntelligenceResult
StartupIntelligenceEntry
StartupCommandResolution
StartupCommandResolutionStatus
StartupSignatureInfo
StartupSignatureStatus
StartupClassification

Potentially:

StartupFileMetadata

if separating file metadata improves clarity.

Avoid creating tiny one-property models without architectural value.

Services
StartupIntelligenceService
StartupCommandParser
StartupSignatureInspector
StartupIntelligenceEvidenceAdapter

Potentially:

IStartupSignatureInspector
IStartupFileInspector

when abstraction materially improves deterministic testing.

Do not create interfaces automatically for every class.

Use interfaces at OS/file inspection boundaries where fakes make safety and error-state testing substantially cleaner.

41. DEPENDENCY BOUNDARIES

Preferred dependency flow:

Existing Startup Models
        ↓
StartupIntelligenceService
   ├─ command parser
   ├─ file inspector
   └─ signature inspector
        ↓
StartupIntelligenceResult
        ↓
StartupIntelligenceEvidenceAdapter
        ↓
EvidenceService

Forbidden dependency direction:

Evidence Adapter
    ↓
StartupManagerService
    ↓
Disable / Enable

Also forbidden:

Evidence Explorer
    ↓
StartupIntelligenceService

The Explorer reads persisted Evidence only.

42. TESTABILITY CONTRACT

Windows-specific inspection must be isolated sufficiently that classification and Evidence behavior can be tested deterministically.

Tests should not depend on:

whichever startup programs happen to exist on the developer machine
installed third-party applications
internet access
administrator rights
modifying real startup registry keys
modifying real Startup folders

Where real Authenticode integration tests are useful, use a stable Windows-supplied signed executable only if such a test can remain deterministic across supported environments.

Otherwise test the signature abstraction with constructed results/fakes.

43. COMMAND PARSER TEST PLAN

Cover at minimum:

quoted executable path
quoted path with arguments
ordinary direct executable path
environment-variable path
path containing spaces
empty command
whitespace-only command
malformed quotes
launcher-mediated command
unsupported command
unresolved environment variable
arguments preserved separately where supported
original command preserved exactly
parser performs no execution

Test cases must explicitly verify that ambiguous commands remain unresolved instead of being guessed.

44. FILE INSPECTION TEST PLAN

Cover:

resolved file exists
resolved file missing
metadata available
metadata partially missing
access denied
unexpected I/O failure
directory supplied instead of executable
non-executable extension where relevant
path normalization
no file mutation

If the implementation exposes file timestamps, they must not become identity/security claims without justification.

45. SIGNATURE TEST PLAN

Cover semantic states:

valid signature
unsigned
invalid/hash mismatch
untrusted
inspection failure
file missing
unsupported/not checked

Verify:

Unsigned != Invalid
Invalid != Unknown
Unknown != Valid

Also verify:

signer metadata preserved when available
missing signer does not crash inspection
no private certificate material persisted
no certificate-store enumeration required
46. CLASSIFICATION TEST PLAN

Cover deterministic combinations such as:

Valid signed executable

Expected:

Classification: Verified
Confidence: High

provided the approved classification rules support it.

Existing unsigned executable

Must NOT automatically become Suspicious.

Expected may be:

Known

or:

Unverified

depending on final approved rules.

Missing direct executable

Expected candidate:

Broken

with rationale explaining that the configured target was resolved but absent.

Launcher-mediated unresolved command

Expected:

Unverified

or:

Unknown

not Broken.

Metadata missing

Must not automatically become Suspicious.

Signature inspection error

Must not become unsigned.

Unknown input

Must remain safely representable.

Every classification rule must have a corresponding rationale assertion.

47. EVIDENCE ADAPTER TEST PLAN

Verify:

correct session ID
StartupIntelligence source
appropriate category
deterministic subject
deterministic correlation key
classification represented
confidence represented independently
rationale preserved
signature state preserved
bounded metadata
UTC timestamp
partial-success behavior
malformed individual entry does not discard valid entries
no Windows inspection
no startup-management dependency
no system-changing call
48. INTEGRATION TEST PLAN

Prove a vertical slice:

Constructed existing startup entry
    ↓
Fake file/signature inspection
    ↓
StartupIntelligenceService
    ↓
StartupIntelligenceResult
    ↓
StartupIntelligenceEvidenceAdapter
    ↓
EvidenceService
    ↓
JsonEvidenceRepository
    ↓
fresh repository instance
    ↓
GetBySessionAsync
    ↓
Startup Evidence verified

Then prove coexistence:

SystemScan Evidence
DeepAnalysis Evidence
StartupIntelligence Evidence
        ↓
same report session document

Existing records must remain intact.

49. EXPLORER INTEGRATION TEST

Because the Explorer is generic, Sprint 22 should prove presentation compatibility without adding special UI logic.

Construct Startup Intelligence Evidence and verify:

Source facet contains Startup Intelligence.
Startup category count includes records.
Search by startup entry name works.
Search by publisher works when persisted.
Search by classification works.
Search by signature state works.
Detail projection exposes metadata.
Correlation key remains inspectable.
Existing System Scan and Deep Analysis presentation tests remain green.
50. SAFETY TESTS

Extend structural/behavioral safety coverage.

Startup Intelligence collection must not reference or invoke system-changing methods from:

startup enable/disable services
cleanup executors
storage cleanup
service-control execution
process termination
installer handoff
elevation
registry write APIs

Registry READ access may be part of the existing scanner.

Registry WRITE access is forbidden inside the new intelligence subsystem.

Tests should distinguish those concepts accurately.

51. REGRESSION SUITE

Sprint 22 should not make the Regression Suite perform live startup modifications.

Potential safe additions:

Startup Intelligence type availability
Evidence source/schema compatibility
read-only inspection smoke behavior using safe inputs
Evidence persistence compatibility

Do not make the Regression Suite:

add startup entries
disable startup entries
delete startup entries
modify the registry to manufacture test data

Use automated unit/integration tests for deterministic behavior.

52. DEBUG BUNDLE

Sprint 20 already includes Evidence documents in explicit debug bundles.

Startup Intelligence Evidence will therefore naturally be included.

Before final acceptance, review persisted Startup metadata for privacy.

Specifically inspect:

executable paths
startup commands
arguments
usernames
URLs
tokens
publisher information

Do not create a second Startup-specific debug export unless a real need is identified.

53. EVIDENCE SCHEMA COMPATIBILITY

Preferred outcome:

Evidence SchemaVersion = 1

remains unchanged.

Sprint 22 should add:

a new Evidence source enum value
new record subjects
new metadata keys

without changing the document shape.

Before implementation, verify existing deserialization behavior for additive enum values.

Existing Sprint 20/21 Evidence documents must remain readable.

Sprint 22 must not rewrite old documents merely to introduce Startup Intelligence.

54. PERFORMANCE TARGET

Startup Intelligence should remain reasonable for normal technician machines.

Target scale:

0–100 startup entries

The implementation should avoid:

process launches
recursive filesystem searches
repeated certificate-store scans
repeated inspection of the same target without need
network reputation checks

A typical machine with approximately 10–30 entries should not make System Scan feel stalled for an excessive period.

If signature inspection proves expensive, architecture should allow enrichment to remain additive and safely isolated.

Performance observations must be recorded during manual acceptance.

55. USER-FACING LANGUAGE

ForgeCare language must remain factual and technician-oriented.

Prefer:

Valid Authenticode signature observed.

Publisher metadata was not available.

The configured executable could not be resolved.

The resolved executable was not present at inspection time.

ForgeCare could not determine signature status.

Avoid:

Safe application.

Dangerous application.

Virus.

Malware.

You should disable this.

This is unnecessary.

unless a later feature has explicit evidence and product authorization for such claims.

56. OBSERVATION VS INTERPRETATION

The implementation must keep these conceptually distinct.

Observation:

SignatureStatus = NotSigned

Interpretation:

Classification = Unverified

Observation:

FileExists = false

Interpretation:

Classification = Broken

Observation:

CommandResolution = LauncherMediated

Interpretation:

Classification = Unknown

The interpretation must be traceable back to observations through rationale.

57. FUTURE COMPATIBILITY

Sprint 22 should prepare, but not implement, future reasoning such as:

Startup entry
    ↕ correlation
Running process
    ↕ correlation
Service
    ↕ correlation
Resource pressure
    ↕ correlation
Forge Plan

Therefore:

preserve deterministic correlation
preserve provenance
preserve classification rationale
preserve raw technical observations where privacy permits

Do not implement cross-source correlation yet.

That belongs to a later sprint.

58. IMPLEMENTATION PHASES
Phase A — Repository reconnaissance and architecture

Read-only.

Deliver:

actual Startup architecture
exact existing models/services
exact mutation boundaries
available metadata/signature helpers
integration point
proposed files
proposed modifications
privacy decisions requiring approval
Evidence schema compatibility review
test strategy
risk register

No implementation.

Phase B — Startup Intelligence foundation

Implement only:

command-resolution model
conservative parser
file metadata inspection boundary
Authenticode inspection boundary
Startup Intelligence result model
deterministic classification/rationale
unit tests

No Evidence integration.

No MainWindow integration.

No Startup UI changes.

Prove:

existing/constructed startup entry
    ↓
read-only enrichment
    ↓
deterministic StartupIntelligenceResult
Phase C — Startup Evidence integration

Implement:

StartupIntelligence Evidence source
pure Startup Intelligence Evidence adapter
session association
bounded metadata
correlation keys
best-effort live persistence
same-session coexistence tests
Explorer compatibility tests

Do not redesign Explorer.

Do not change Startup management behavior.

Phase D — Product integration and closeout

Only after Phase C is approved.

Perform:

final live workflow integration
optional minimal Startup read-only presentation only if separately approved
Regression/support review
debug-bundle privacy review
performance review
keyboard/visual regression if UI changed
full automated suite
full build
manual technician acceptance
acceptance documentation

Create:

docs/SPRINT_22_ACCEPTANCE.md

Do not begin Sprint 23 work.

59. MANUAL ACCEPTANCE PLAN

Final acceptance should include at least the following.

A. Baseline regression
Start ForgeCare.
Start a new Forge Report session.
Run System Scan.
Confirm existing Dashboard behavior remains normal.
Confirm existing startup count remains correct.
Confirm existing startup management remains unchanged.
B. Startup Intelligence
Inspect Startup Intelligence output for several entries.
Confirm direct executable entries resolve correctly where possible.
Confirm file existence state matches reality.
Confirm publisher/file metadata appears only when available.
Confirm signed executables show a meaningful signature state.
Confirm unsigned executables are not labelled dangerous merely for being unsigned.
Confirm unresolved commands remain Unknown/Unverified rather than guessed.
Confirm missing targets are explained factually.
C. Evidence
Open Evidence Explorer.
Confirm existing System Scan records remain.
Confirm Startup Intelligence appears as a Source.
Filter to Startup Intelligence.
Confirm per-entry records appear.
Select several records.
Confirm classification, confidence, rationale, signature state, and bounded provenance are inspectable.
Confirm correlation keys are present where designed.
Search by entry name.
Search by publisher/signature metadata where available.
D. Deep Analysis coexistence
Run Deep Analysis.
Refresh Evidence Explorer.
Confirm System Scan, Deep Analysis, and Startup Intelligence coexist in the same report session.
Confirm no previous Evidence was overwritten.
E. Persistence
Close ForgeCare.
Reopen ForgeCare.
Return to Evidence.
Confirm Startup Intelligence Evidence remains readable.
F. Safety
Confirm no startup state changed during inspection.
Confirm no application was launched by Startup Intelligence.
Confirm no unexpected elevation prompt occurred.
Confirm no network dependency was required.
G. Regression/support
Run Regression Suite.
Confirm Evidence persistence remains healthy.
Export Debug Bundle.
Review Startup Evidence for unexpected sensitive values.
Confirm source Evidence remains unchanged.
60. RISK REGISTER
Risk	Rank	Mitigation
Command parser incorrectly treats arguments as executable path	HIGH	Conservative parser; unresolved beats guessed
Intelligence accidentally executes configured command	CRITICAL	Never shell/launch commands; structural and behavioral safety tests
Startup inspection becomes coupled to enable/disable behavior	HIGH	Separate read-only intelligence service and mutation services
Unsigned executable is treated as malicious	HIGH	Explicit classification contract and tests
Signature inspection failure is treated as unsigned	HIGH	Distinct signature states
Missing executable is inferred from an unparseable command	HIGH	Broken only after confident direct resolution
Raw command leaks tokens/secrets into Evidence/debug bundles	HIGH	Explicit persistence/privacy review and bounded metadata
Full executable paths expose personal directory information	MEDIUM/HIGH	Explicit path persistence decision and acceptance review
Authenticode inspection unexpectedly requires network	MEDIUM/HIGH	Review API behavior; preserve offline operation
Evidence schema compatibility breaks older documents	HIGH	Prefer schema 1; compatibility tests
One inaccessible entry fails complete scan	MEDIUM	Per-entry partial success
Signature/file inspection blocks WPF UI	MEDIUM	Async/off-thread inspection and conservative concurrency
Evidence record count becomes excessive	MEDIUM	Prefer one primary record per startup entry
Correlation key leaks sensitive data	MEDIUM	Normalize/bound identity and exclude secrets
Explorer requires Startup-specific rendering	LOW	Generic metadata-first design
Performance degrades on machines with many startup entries	MEDIUM	No network, no recursive search, bounded inspection
Existing startup management regresses	HIGH	Do not modify mutation path; regression/manual acceptance
Sprint expands into malware/reputation product	HIGH	Explicit non-goals
Sprint expands into Forge Plan correlation prematurely	MEDIUM	Preserve future compatibility only

61. DEFINITION OF DONE

Sprint 22 is complete only when all applicable items below are satisfied.

Architecture
 Real Startup architecture has been inspected before implementation.
 Existing mutation boundaries are documented.
 Startup Intelligence is read-only.
 Intelligence and management remain separate.
 Evidence adapter performs translation only.
Command inspection
 Original configured command is preserved in the transient model.
 Direct executable paths are conservatively resolved.
 Arguments are not mistaken for executable identity.
 Environment-variable expansion is inspection-only.
 Launcher-mediated commands are represented safely.
 Malformed commands do not crash the complete workflow.
 No command is executed.
File provenance
 File existence is represented.
 Available local file metadata is represented.
 Missing metadata remains a valid state.
 No filesystem mutation occurs.
 No disk-wide guessing/search is used.
Signature provenance
 Valid signature is distinguishable.
 Unsigned is distinguishable.
 Invalid/untrusted is distinguishable.
 Unknown/inspection failure is distinguishable.
 Missing file is distinguishable.
 Signer metadata is bounded.
 Publisher metadata and signer identity remain conceptually separate.
Classification
 Classification taxonomy exists.
 Unknown is first-class.
 Confidence is independent.
 Rationale exists.
 Unsigned alone does not produce Suspicious.
 Missing publisher alone does not produce Suspicious.
 Unknown does not imply bad.
 Classification does not trigger action.
Evidence
 StartupIntelligence source exists.
 Existing schema compatibility is preserved.
 Existing startup-count Evidence remains.
 Per-entry Startup Evidence exists.
 Session ID uses the active Forge Report session.
 UTC timestamps are preserved.
 Correlation keys are deterministic and bounded.
 Metadata remains within existing limits.
 Evidence adapter performs no Windows inspection.
 Partial success is supported.
 Evidence persistence failure does not invalidate successful diagnostics.
Explorer
 Startup Intelligence appears through generic Explorer behavior.
 Source facet works.
 Category filtering works.
 Search finds relevant Startup metadata.
 Generic detail rendering is sufficient.
 No Startup-specific Explorer redesign was required unless separately approved.
Privacy
 Raw-command persistence decision is documented.
 Argument persistence is reviewed for secrets.
 Executable-path persistence decision is documented.
 Debug Bundle contents are reviewed.
 No credentials/tokens are intentionally persisted.
 Certificate data is bounded.
Safety
 No startup enable/disable occurs during intelligence collection.
 No registry write occurs.
 No file deletion/modification occurs.
 No process termination occurs.
 No service modification occurs.
 No installer execution occurs.
 No elevation is required.
 No required network request occurs.
 No automatic remediation occurs.
Testing
 Command parser tests pass.
 File inspection tests pass.
 Signature-state tests pass.
 Classification tests pass.
 Evidence adapter tests pass.
 Vertical-slice persistence tests pass.
 Explorer compatibility tests pass.
 Safety regression tests pass.
 Existing Sprint 20 tests remain green.
 Existing Sprint 21 tests remain green.
 Full automated suite passes.
 Solution builds successfully.
 git diff --check passes.
Manual acceptance
 Real startup entries inspected.
 Existing startup management still works normally.
 Startup Evidence visible in Explorer.
 System Scan / Deep Analysis / Startup Intelligence coexist.
 Restart persistence verified.
 Regression Suite healthy.
 Debug Bundle reviewed.
 No unexpected system changes observed.
 No unexpected network dependency observed.
 Performance acceptable.
Closeout
 docs/SPRINT_22_ACCEPTANCE.md exists.
 Privacy decisions are recorded.
 Signature/network behavior is recorded.
 Known limitations are recorded.
 No unrelated version/release changes exist.
 No Sprint 23 work was introduced.
62. SMALLEST SAFE VERTICAL SLICE

The first implementation slice after reconnaissance should be:

Constructed Startup Entry
        ↓
Conservative Command Parser
        ↓
Fake File Inspector
        ↓
Fake Signature Inspector
        ↓
StartupIntelligenceService
        ↓
StartupIntelligenceResult

Prove:

direct executable resolution
malformed/unresolved behavior
file-present/file-missing distinction
valid/unsigned/invalid/unknown signature distinction
classification
confidence
rationale
partial success
zero system mutation

Do NOT add Evidence persistence in the first slice.

Do NOT modify MainWindow in the first slice.

Do NOT modify Startup management in the first slice.

Do NOT modify Evidence Explorer in the first slice.

Once this vertical slice is deterministic and fully tested, Startup Evidence becomes an additive translation problem rather than a Windows-inspection problem.

63. PRODUCT PRINCIPLE

Sprint 22 establishes the next major ForgeCare capability:

ForgeCare should not tell a technician what to believe about a startup entry.
ForgeCare should show the technician what it observed, what it verified, what it could not verify, and why.

The desired evolution is:

v1.0
"What starts with Windows?"


        ↓


Sprint 20
"We can preserve diagnostic observations."


        ↓


Sprint 21
"The technician can inspect those observations."


        ↓


Sprint 22
"We can explain the local provenance of a startup entry."


        ↓


Future
"We can correlate provenance, behavior, resource pressure,
services, and system state into an explainable Forge Plan."

That future reasoning must be built on evidence, not guesses.

64. STOP CONDITION

After the initial repository reconnaissance:

STOP.

Do not implement Phase B automatically.

Return:

Current Startup architecture
Existing startup models
Existing scanner/read path
Existing mutation path
Registry and Startup-folder coverage
Existing command representation
Existing path/shortcut handling
Existing file metadata/signature capabilities
Exact proposed architecture
Files to create
Existing files to modify
Command-resolution design
Authenticode design
Classification rules
Confidence rules
Evidence mapping
Privacy decision for commands/paths
Network/revocation behavior
Test plan
Safety review
Risk register
Implementation order
Deviations required by the real repository
Smallest safe Phase B vertical slice

Wait for explicit approval before changing repository files.