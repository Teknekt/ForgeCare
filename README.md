# ForgeCare

**Know your system. Forge it better.**

ForgeCare Technician Edition is a local-first Windows diagnostic, optimization, safety, verification, and reporting application built by **MindForge Studio** for technicians and people who work directly with Windows systems.

The project is designed around a technician-controlled workflow rather than one-click “optimizer” behavior: inspect the machine, understand what is happening, review proposed actions, apply controlled changes, verify the result, and preserve a useful record of the work performed.

## Release

**Current release:** v1.0.0  
**Platform:** Windows x64  
**Application:** WPF / .NET 10

[Download ForgeCare v1.0.0 for Windows](https://github.com/Teknekt/ForgeCare/releases/download/v1.0.0/ForgeCare-v1.0.0-Setup.exe)

## Workflow

ForgeCare follows a simple operational loop:

**Scan → Analyze → Plan → Forge → Verify → Report**

The goal is not to hide system changes behind an automatic cleanup button. ForgeCare keeps the technician in the loop so findings and recommendations can be reviewed before actions are applied.

## Core capabilities

- **System dashboard** — a quick operational overview of the Windows machine.
- **Deep analysis** — inspect system state and surface findings that deserve technician attention.
- **Cleanup review** — review cleanup candidates before making changes.
- **Startup optimization** — inspect startup behavior and review optimization opportunities.
- **Service intelligence** — provide technician-oriented context around Windows services.
- **Forge Plan** — collect findings and proposed actions into a controlled execution plan.
- **Verification** — check system state after work has been performed instead of assuming an action succeeded.
- **Professional reports** — preserve useful session and technician information for reporting and follow-up.

## Safety philosophy

ForgeCare is intentionally conservative.

The application is built around:

- technician review before meaningful changes;
- controlled actions rather than indiscriminate optimization;
- local operational data;
- verification after changes;
- preserving technician, field-test, report, and safety state where appropriate;
- transparent workflows that make it possible to understand what the tool is proposing.

ForgeCare should support technical judgment, not replace it.

## Local-first

ForgeCare is designed as a local Windows application. Operational state and technician data are kept locally, including persistent application data under `%LOCALAPPDATA%\ForgeCare`.

The installer uses a per-user installation model and preserves application data independently from the installed program files so technician settings and relevant working state can survive upgrades or uninstall/reinstall workflows where intended.

## Repository structure

The solution currently contains the main application project:

```text
ForgeCare.slnx
└── ForgeCare.app/
    └── ForgeCare.app.csproj
```

The application targets `net10.0-windows`, uses WPF, and currently publishes for `win-x64`.

## Building from source

### Requirements

- Windows x64
- .NET 10 SDK

Clone the repository and build the solution:

```powershell
git clone https://github.com/Teknekt/ForgeCare.git
cd ForgeCare
dotnet build ForgeCare.slnx
```

For release/distribution work, use the release and installer scripts included in the repository rather than treating a normal debug build as a distributable release.

## Release artifacts

ForgeCare's release process supports a Windows installer and portable distribution. Release builds can produce a release manifest containing version/channel information and SHA-256 hashes for generated artifacts.

The stable v1.0.0 installer is available from the GitHub release linked above.

## Project status

ForgeCare Technician Edition **v1.0.0** is the first official MindForge Studio release.

Development continues with a focus on making the application more useful as a practical technician workstation tool while keeping its review-first and safety-oriented behavior intact.

## MindForge Studio

ForgeCare is built by **MindForge Studio** — an independent technology studio building practical software, AI systems, and experimental hardware for technicians, developers, and intelligent computing workflows.

**FORGE. BUILD. IMAGINE.**
