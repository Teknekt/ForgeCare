# ForgeCare Sprint 13A — First External Machine Test

Version: v0.0.31-beta

## Added
- External Machine Preflight in TOOLS
- Pass / Warn / Fail preflight summary
- Checks for Windows, x64 process, executable path, local-data write access,
  Desktop path, beta build identity and crash-diagnostic readiness
- External beta test checklist
- Beta tester README
- `scripts\forge-beta-kit.cmd`
- Release pipeline copies beta test documentation into portable package

## Feature freeze
13A adds no system-changing optimization features.

## Build beta kit
Run:

`scripts\forge-beta-kit.cmd`

Then take the generated portable ZIP from `artifacts` to a non-critical Windows x64 machine.

## First action on test machine
Open ForgeCare → TOOLS → External Machine Preflight.

Do not continue with system-changing tests if preflight shows FAIL items.
