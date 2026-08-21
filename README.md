# ForgeCare

**Technician Edition · v1.0.0 · Windows x64**

> **Know your system. Forge it better.**

ForgeCare is a technician-focused Windows diagnostic, optimization, safety, verification and reporting application from MindForge Studio.

It is designed around a deliberate workflow rather than one-click “PC cleaner” behavior:

**Scan → Analyze → Plan → Forge → Verify → Report**

## What ForgeCare is for

ForgeCare brings common technician workflows into one local-first desktop application:

- system and hardware diagnostics
- deep analysis and technician review
- cleanup review and dry-run workflows
- startup optimization
- Windows service intelligence
- guided Forge Plan orchestration
- before/current verification
- professional HTML reporting
- safety journaling and diagnostic bundles

ForgeCare is intended for technicians and technically confident Windows users who want visibility and review before system-changing actions.

## Safety philosophy

ForgeCare is intentionally conservative.

- Diagnostic and analysis workflows are read-only unless an action is explicitly selected.
- Guided navigation does not silently execute system-changing actions.
- Changes are presented for technician review and confirmation.
- Supported workflows include dry-run/review stages where appropriate.
- Safety journal and verification workflows help make actions inspectable.
- ForgeCare does not silently elevate permissions.

ForgeCare is a technician tool, not a guarantee that every recommendation is appropriate for every machine. Review proposed actions and use normal backup/change-control practices on important systems.

## Privacy and data handling

ForgeCare is designed as a **local-first Windows application**. Operational diagnostics, settings, reports and safety information are handled locally by the application rather than requiring a MindForge cloud account.

Exported reports and Debug Bundles can contain machine, report, settings or local ForgeCare metadata. **Review exported material before sharing it with another person or attaching it to an issue.**

See [SECURITY.md](SECURITY.md) for responsible security reporting.

## Download v1.0.0

The current public release is **ForgeCare Technician Edition v1.0.0 for Windows x64**.

**Installer:** [ForgeCare-v1.0.0-Setup.exe](https://github.com/Teknekt/ForgeCare/releases/download/v1.0.0/ForgeCare-v1.0.0-Setup.exe)

You can also browse the [v1.0.0 release](https://github.com/Teknekt/ForgeCare/releases/tag/v1.0.0) for available release assets and notes.

### Installation expectations

1. Download the v1.0.0 installer from the GitHub release above.
2. Run the installer on a Windows x64 machine.
3. Windows may display reputation/SmartScreen messaging for software from a new independent publisher. Verify that you downloaded ForgeCare from this repository or the official MindForge Studio product page before continuing.
4. ForgeCare does not silently elevate privileges. Operations that require additional Windows permissions should remain explicit to the technician.

For first evaluation, avoid starting on an irreplaceable or production-critical system. Learn the workflow and review proposed actions before applying changes.

## Typical workflow

### 1. Scan
Establish the current machine state.

### 2. Analyze
Use deeper diagnostics, service intelligence, storage analysis and optimization analysis to understand what deserves attention.

### 3. Plan
Review findings and use Forge Plan / guided workflow to decide what should actually change.

### 4. Forge
Apply only reviewed and confirmed actions.

### 5. Verify
Re-scan and compare the resulting state instead of assuming an operation helped.

### 6. Report
Create a professional HTML report with technician, device and before/after information.

## Feedback and bugs

ForgeCare is an actively developed independent product. Real-world technician feedback is especially valuable as we shape the next release.

- Found a bug? [Open a bug report](https://github.com/Teknekt/ForgeCare/issues/new?template=bug_report.yml)
- Have a workflow or product suggestion? [Open a feature/feedback request](https://github.com/Teknekt/ForgeCare/issues/new?template=feedback.yml)

When reporting a problem, include the Windows version, ForgeCare version, what you expected, what happened and reproducible steps where possible. If you create a Debug Bundle, review its contents before sharing it.

## Current status and roadmap

**Current:** v1.0.0 — first public Technician Edition release.

Near-term work is intentionally focused on product quality rather than feature count:

- real-world technician feedback and bug fixing
- installation/reputation experience
- workflow clarity and safety
- reporting quality
- diagnostics accuracy and performance
- v1.1 improvements based on validated use

See [ROADMAP.md](ROADMAP.md) for the concise public roadmap.

## Historical test documentation

Some files in the source tree document pre-1.0 beta and external-machine validation. They are retained as engineering history and test evidence; **they do not describe the current public release version**. Current public release identity is v1.0.0.

## About MindForge Studio

ForgeCare is built by **MindForge Studio**, an independent technology studio building practical software, AI systems and experimental hardware for technicians, developers and intelligent computing workflows.

**Practical technology for people who work with technology.**

**FORGE. BUILD. IMAGINE.**
